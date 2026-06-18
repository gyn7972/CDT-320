using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.CDT320.Interlocks;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerPlaceSequence : PickerSequenceBase<PickerPlaceStep>
    {
        private readonly List<int> _pickedPickerIndexes = new List<int>();
        private int _pickerCursor;
        private int _currentPickerIndex = -1;
        private int _currentPickerNo;
        private DieMaterial _currentDie;
        private BinSide _currentOutputSide;
        private OutputStageReceiveTarget _receiveTarget;
        private double _targetPickerX;
        private double _targetPickerY;
        private double _targetPickerT;
        private double _targetPickerZ;
        private double _targetOutputStageY;
        private double _outputVisionToPickerX;
        private double _outputVisionToPickerY;
        private SequenceResourceLease _outputPlaceLease;
        private SequenceResourceLease _outputStageLease;

        public PickerPlaceSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.UnloadToOutput, side == PickerSequenceSide.Front ? "FrontPickerPlaceSequence" : "RearPickerPlaceSequence")
        {
            CurrentStep = PickerPlaceStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerPlaceStep.Complete; }
        }

        public void Abort()
        {
            try
            {
                ReleaseOutputPlaceArea();
                ReleaseOutputStageArea();
                CurrentStep = PickerPlaceStep.Complete;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private OutputStageUnit OutputStage
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputStageUnit : null; }
        }

        protected override async Task<int> ExecuteAsync(CancellationToken ct)
        {
            bool keepCurrentState = false;

            try
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                {
                    ct.ThrowIfCancellationRequested();
                    int stepResult = await ExecuteStepAsync(ct).ConfigureAwait(false);
                    keepCurrentState = stepResult == 0 && CurrentStep != PickerPlaceStep.Complete;
                    return stepResult;
                }

                while (CurrentStep != PickerPlaceStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();

                    int result = await ExecuteStepAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-EX", Name, "Picker place failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                if (!keepCurrentState)
                {
                    ReleaseOutputPlaceArea();
                    ReleaseOutputStageArea();
                }
            }
        }

        private Task<int> ExecuteStepAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            switch (CurrentStep)
            {
                // 유닛 확인
                case PickerPlaceStep.CheckUnit:
                    return Task.FromResult(CheckUnit());

                // 픽업 피커 목록 생성
                case PickerPlaceStep.BuildPickedPickerList:
                    return Task.FromResult(BuildPickedPickerList());

                // 전체 피커 Z로 어보이드 이동
                case PickerPlaceStep.MoveAllPickerZToAvoid:
                    return MoveAllPickerZToAvoidAsync(ct);

                // 다음 피커 선택
                case PickerPlaceStep.SelectNextPicker:
                    return Task.FromResult(SelectNextPicker());

                // 아웃풋 사이드 결정
                case PickerPlaceStep.ResolveOutputSide:
                    return Task.FromResult(ResolveOutputSide());

                // 아웃풋 스테이지 수령 가능 확인
                case PickerPlaceStep.VerifyOutputStageReady:
                    return Task.FromResult(VerifyOutputStageReady());

                // 아웃풋 스테이지 대상 예약
                case PickerPlaceStep.ReserveOutputStageTarget:
                    return Task.FromResult(ReserveOutputStageTarget());

                // 아웃풋 스테이지 피커 진입용 어보이드 이동
                case PickerPlaceStep.MoveOutputStageAvoidPosition:
                    return MoveOutputStageAvoidPositionAsync(ct);

                // 아웃풋 스테이지 수령 위치 이동
                case PickerPlaceStep.MoveOutputStageReceivePosition:
                    return MoveOutputStageReceivePositionAsync(ct);

                // 플레이스 대상 계산
                case PickerPlaceStep.CalculatePlaceTarget:
                    return Task.FromResult(CalculatePlaceTarget());

                // 피커 XY 픽업 T 이동
                case PickerPlaceStep.MovePickerXYPickT:
                    return MovePickerXYPickTAsync(ct);

                // 플레이스 대상 검증
                case PickerPlaceStep.VerifyPlaceTarget:
                    return Task.FromResult(VerifyPlaceTarget());

                // 피커 Z 플레이스 이동
                case PickerPlaceStep.MovePickerZPlace:
                    return MovePickerZPlaceAsync(ct);

                // 진공 OFF 처리
                case PickerPlaceStep.VacuumOff:
                    return VacuumOffAsync(ct);

                // 블로우 OFF 처리
                case PickerPlaceStep.BlowOff:
                    return BlowOffAsync(ct);

                // 피커 Z로 어보이드 이동
                case PickerPlaceStep.MovePickerZToAvoid:
                    return MovePickerZToAvoidAsync(ct);

                // 자재로 아웃풋 스테이지 갱신
                case PickerPlaceStep.UpdateMaterialToOutputStage:
                    return Task.FromResult(UpdateMaterialToOutputStage());

                // 다음 피커 또는 완료 선택
                case PickerPlaceStep.SelectNextPickerOrComplete:
                    return Task.FromResult(SelectNextPickerOrComplete());

                default:
                    return Task.FromResult(Fail("PICKER-PLACE-STEP", Name, "Unsupported picker place step. step=" + CurrentStep));
            }
        }

        private int CheckUnit()
        {
            if (!IsPickerSideEnabled())
            {
                WriteLog("PickerPlaceSequence", Name + " skipped because picker side is disabled. side=" + Side + " - Check");
                CurrentStep = PickerPlaceStep.Complete;
                return 0;
            }

            if (OutputStage == null)
                return Fail("PICKER-PLACE-OUTPUT-STAGE-MISSING", "OutputStage", "OutputStageUnit is null.");

            string axisReason = BuildRequiredPickerAxesReason();
            if (!string.IsNullOrWhiteSpace(axisReason))
                return Fail("PICKER-PLACE-AXIS-NOT-READY", Name, "Picker axis is not ready. side=" + Side + ", reason=" + axisReason);

            CurrentStep = PickerPlaceStep.BuildPickedPickerList;
            return 0;
        }

        private int BuildPickedPickerList()
        {
            _pickedPickerIndexes.Clear();
            _pickedPickerIndexes.AddRange(BuildLoadedPickerIndexesInRunOrder("PickerPlaceSequence"));

            _pickerCursor = 0;

            if (_pickedPickerIndexes.Count == 0)
            {
                WriteLog("PickerPlaceSequence", Name + " skipped because no die exists on picker. - Check");
                CurrentStep = PickerPlaceStep.Complete;
                return 0;
            }

            CurrentStep = PickerPlaceStep.MoveAllPickerZToAvoid;
            return 0;
        }

        private async Task<int> MoveAllPickerZToAvoidAsync(CancellationToken ct)
        {
            EnsurePickerWorkAreaReserved(PickerWorkZone.Output, "Place");

            int result = await MoveAllPickerZToAvoidAndVerifyAsync("place pre all picker Z avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.SelectNextPicker;
            return 0;
        }

        private int SelectNextPicker()
        {
            if (_pickerCursor >= _pickedPickerIndexes.Count)
            {
                CurrentStep = PickerPlaceStep.Complete;
                return 0;
            }

            _currentPickerIndex = _pickedPickerIndexes[_pickerCursor];
            _currentPickerNo = ToPickerNo(_currentPickerIndex);
            _currentDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo);
            _receiveTarget = null;

            if (_currentDie == null)
            {
                CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                return 0;
            }

            CurrentStep = PickerPlaceStep.ResolveOutputSide;
            return 0;
        }

        private int ResolveOutputSide()
        {
            if (_currentDie.Result == DieResult.Good)
            {
                _currentOutputSide = BinSide.Good;
                CurrentStep = PickerPlaceStep.VerifyOutputStageReady;
                return 0;
            }

            if (_currentDie.Result == DieResult.NG)
            {
                _currentOutputSide = BinSide.Ng;
                CurrentStep = PickerPlaceStep.VerifyOutputStageReady;
                return 0;
            }

            return Fail("PICKER-PLACE-DIE-RESULT-UNKNOWN", "Material",
                "Die result is unknown before place. die=" + _currentDie.DieId + ", pickerNo=" + _currentPickerNo);
        }

        private int VerifyOutputStageReady()
        {
            string reason;
            if (!MaterialStateService.IsOutputStageReceiveAvailable(_currentOutputSide, out reason))
            {
                return Fail("PICKER-PLACE-OUTPUT-STAGE-NOT-READY", "Material",
                    "Output stage is not ready to receive die. side=" + _currentOutputSide +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", reason=" + reason);
            }

            WriteLog("PickerPlaceSequence",
                Name + " output stage receive ready. side=" + _currentOutputSide +
                ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", reason=" + reason + " - Ok");

            CurrentStep = PickerPlaceStep.ReserveOutputStageTarget;
            return 0;
        }

        private int ReserveOutputStageTarget()
        {
            _receiveTarget = MaterialStateService.ReserveNextOutputStageReceiveTarget(_currentOutputSide);
            if (_receiveTarget == null)
            {
                return Fail("PICKER-PLACE-RECEIVE-TARGET-MISSING", "Material",
                    "Output stage receive target is missing. die=" + _currentDie.DieId +
                    ", side=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo);
            }

            CurrentStep = PickerPlaceStep.MoveOutputStageAvoidPosition;
            return 0;
        }

        private async Task<int> MoveOutputStageAvoidPositionAsync(CancellationToken ct)
        {
            if (_outputPlaceLease == null)
            {
                _outputPlaceLease = await AcquireResourceAsync(SequenceResourceKind.OutputPlaceArea, Name + ":Place", ct).ConfigureAwait(false);
                if (_outputPlaceLease == null)
                    return -1;
            }

            if (_outputStageLease == null)
            {
                SequenceResourceKind resource = _currentOutputSide == BinSide.Ng
                    ? SequenceResourceKind.OutputNgStageArea
                    : SequenceResourceKind.OutputGoodStageArea;

                _outputStageLease = await AcquireResourceAsync(resource, Name + ":Place:" + _currentOutputSide, ct).ConfigureAwait(false);
                if (_outputStageLease == null)
                    return -1;
            }

            int result = await AwaitStepWithCancellationAsync(
                OutputStage.EnsureStageMutualInterlockForLoadAsync(_currentOutputSide, ResolveTimeout(), Options.FineMove, ct),
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("PICKER-PLACE-STAGE-AVOID-INTERLOCK", "OutputStage",
                    "OutputStage 피커 진입용 Avoid 이동 전 인터락 준비 실패. side=" + _currentOutputSide +
                    ", result=" + result + ", " + OutputStage.DescribeStageLoadMoveState(_currentOutputSide));

            result = await MoveOppositeOutputStageToAvoidForPlaceAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await AwaitStepWithCancellationAsync(
                OutputStage.MoveVisionXToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("PICKER-PLACE-VISION-X-AVOID", "OutputStage",
                    "OutputVisionX 피커 진입용 Avoid 이동 실패. side=" + _currentOutputSide +
                    ", result=" + result + ", " + OutputStage.DescribeStageLoadMoveState(_currentOutputSide));

            CurrentStep = PickerPlaceStep.MoveOutputStageReceivePosition;
            return 0;
        }

        private async Task<int> MoveOppositeOutputStageToAvoidForPlaceAsync(CancellationToken ct)
        {
            if (_currentOutputSide == BinSide.Good)
            {
                int ngResult = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveNgStageToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                    ct).ConfigureAwait(false);
                if (ngResult != 0)
                    return Fail("PICKER-PLACE-OPP-STAGE-AVOID", "OutputStage",
                        "Place 전 상대 NG Stage Avoid 이동 실패. result=" + ngResult +
                        ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

                int zResult = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveGoodStageZToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                    ct).ConfigureAwait(false);
                if (zResult != 0)
                    return Fail("PICKER-PLACE-TARGET-Z-AVOID", "OutputStage",
                        "Place 전 Good Stage Z Avoid 이동 실패. result=" + zResult +
                        ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

                return 0;
            }

            int goodZResult = await AwaitStepWithCancellationAsync(
                OutputStage.MoveGoodStageZToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                ct).ConfigureAwait(false);
            if (goodZResult != 0)
                return Fail("PICKER-PLACE-OPP-STAGE-Z-AVOID", "OutputStage",
                    "Place 전 상대 Good Stage Z Avoid 이동 실패. result=" + goodZResult +
                    ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            double goodYAvoid = OutputStage.Recipe.GoodStageY.AvoidPosition;
            int goodYResult = await MoveOutputStageAxisAndVerifyAsync(
                BinStageAxis.GoodBinY,
                goodYAvoid,
                "opposite Good stage Y avoid before place",
                ct).ConfigureAwait(false);
            if (goodYResult != 0)
                return goodYResult;

            return 0;
        }

        private async Task<int> MoveOutputStageReceivePositionAsync(CancellationToken ct)
        {
            BinStageAxis yAxis = _currentOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
            double baseY = _currentOutputSide == BinSide.Ng
                ? OutputStage.Recipe.NGStageY.ProcessPosition
                : OutputStage.Recipe.GoodStageY.ProcessPosition;

            string offsetReason;
            if (!TryResolveOutputVisionToPickerOffsets(
                _currentOutputSide,
                _currentPickerIndex,
                out _outputVisionToPickerX,
                out _outputVisionToPickerY,
                out offsetReason))
            {
                return Fail("PICKER-PLACE-COORD-OFFSET", Name,
                    "OutputVision 기준 Picker 좌표 보정값 계산 실패. " +
                    "side=" + Side +
                    ", outputSide=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo +
                    ", pickerIndex=" + _currentPickerIndex +
                    ", reason=" + offsetReason);
            }

            _targetOutputStageY = baseY +
                _receiveTarget.TargetY +
                _outputVisionToPickerY;

            int result = await MoveOutputStageAxisAndVerifyAsync(yAxis, _targetOutputStageY, "output stage receive Y", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.CalculatePlaceTarget;
            return 0;
        }

        private int CalculatePlaceTarget()
        {
            _targetPickerX = OutputStage.Recipe.VisionX.ProcessPosition +
                _receiveTarget.TargetX +
                _outputVisionToPickerX +
                ResolvePickerAlignOffsetX(_currentPickerIndex);
            _targetPickerY = ResolvePickerZoneY("DiePlacePosition", _currentPickerIndex);
            _targetPickerT = GetPickerTeachingPosition(GetPickerTAxis(_currentPickerIndex), "PlacePosition") +
                ResolvePickerAlignOffsetT(_currentPickerIndex);
            _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "PlacePosition");

            CurrentStep = PickerPlaceStep.MovePickerXYPickT;
            return 0;
        }

        private async Task<int> MovePickerXYPickTAsync(CancellationToken ct)
        {
            int result = await EnsurePickerYAtAvoidBeforePlaceMoveAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _targetPickerX;
            targets[GetPickerTAxis(_currentPickerIndex)] = _targetPickerT;

            result = await MovePickerAxesAndVerifyAsync(
                targets,
                "place picker X/T",
                ct,
                "DiePlacePosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.VerifyPlaceTarget;
            return 0;
        }

        private async Task<int> EnsurePickerYAtAvoidBeforePlaceMoveAsync(CancellationToken ct)
        {
            double avoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
            if (IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, avoid))
                return 0;

            int result = await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                avoid,
                "place picker Y avoid before X/T",
                ct,
                "AvoidPosition;PickerPhase=SafeY").ConfigureAwait(false);
            if (result != 0)
                return result;

            return 0;
        }

        private int VerifyPlaceTarget()
        {
            if (!IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX))
            {
                return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                    "PickerX final position check failed before place. " +
                    BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX));
            }

            BinStageAxis yAxis = _currentOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
            if (!OutputStage.IsStageAxisInPosition(yAxis, _targetOutputStageY, ResolveOutputStageAxisTolerance(yAxis)))
            {
                return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                    "OutputStageY final position check failed before place. " +
                    OutputStage.BuildStageAxisState(yAxis, _targetOutputStageY));
            }

            if (!IsPickerAxisInPosition(GetPickerTAxis(_currentPickerIndex), _targetPickerT))
            {
                PickerAxis tAxis = GetPickerTAxis(_currentPickerIndex);
                return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                    "PickerT final position check failed before place. pickerNo=" + _currentPickerNo +
                    ", " + BuildPickerAxisState(tAxis, _targetPickerT));
            }

            CurrentStep = PickerPlaceStep.MovePickerZPlace;
            return 0;
        }

        private async Task<int> MovePickerZPlaceAsync(CancellationToken ct)
        {
            int result = await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(_currentPickerIndex),
                _targetPickerZ,
                "place picker Z",
                ct,
                "DiePlacePosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.VacuumOff;
            return 0;
        }

        private async Task<int> VacuumOffAsync(CancellationToken ct)
        {
            try
            {
                SetPickerVacuum(_currentPickerNo, false);
                await Task.Delay(ResolveVacuumSettleMs(), ct).ConfigureAwait(false);
                CurrentStep = PickerPlaceStep.BlowOff;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-VACUUM-OFF", Name, "Picker vacuum off failed. pickerNo=" + _currentPickerNo + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> BlowOffAsync(CancellationToken ct)
        {
            try
            {
                await PickerBlowAsync(_currentPickerNo, ct).ConfigureAwait(false);
                CurrentStep = PickerPlaceStep.MovePickerZToAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-BLOW", Name, "Picker blow failed. pickerNo=" + _currentPickerNo + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZToAvoidAsync(CancellationToken ct)
        {
            PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(zAxis, avoid, "place picker Z avoid", ct, "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.UpdateMaterialToOutputStage;
            return 0;
        }

        private int UpdateMaterialToOutputStage()
        {
            if (!MaterialStateService.MoveDieToOutputStage(_currentDie.DieId, _currentOutputSide, _receiveTarget))
            {
                return Fail("PICKER-PLACE-MATERIAL", "Material",
                    "Move die to output stage failed. die=" + _currentDie.DieId +
                    ", side=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo);
            }

            WriteLog("PickerPlaceSequence",
                Name + " place complete. die=" + _currentDie.DieId +
                ", side=" + _currentOutputSide +
                ", pickerNo=" + _currentPickerNo +
                ", outputWafer=" + (_receiveTarget != null ? _receiveTarget.OutputWaferId : "-") +
                ", order=" + (_receiveTarget != null ? _receiveTarget.OrderIndex.ToString() : "-") + " - Ok");

            NotifySequenceProgressAfterPlace();

            CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
            ReleaseOutputStageArea();
            return 0;
        }

        private void NotifySequenceProgressAfterPlace()
        {
            try
            {
                if (MaterialStateService.IsInputStagePickComplete())
                {
                    Context.Bus.Set("InputStageDieComplete");
                    WriteLog("PickerPlaceSequence",
                        Name + " input stage die complete signal set after place. - Ok");
                }

                if (MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide))
                {
                    string signal = _currentOutputSide == BinSide.Ng
                        ? "OutputNgStageReceiveComplete"
                        : "OutputGoodStageReceiveComplete";

                    Context.Bus.Set(signal);
                    WriteLog("PickerPlaceSequence",
                        Name + " output stage receive complete signal set. side=" +
                        _currentOutputSide + ", signal=" + signal + " - Ok");
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " sequence progress notify failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private int SelectNextPickerOrComplete()
        {
            _pickerCursor++;

            if (_pickerCursor >= _pickedPickerIndexes.Count)
            {
                CurrentStep = PickerPlaceStep.Complete;
                ReleaseOutputPlaceArea();
                ReleaseOutputStageArea();
                return 0;
            }

            CurrentStep = PickerPlaceStep.SelectNextPicker;
            return 0;
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await AwaitStepWithCancellationAsync(OutputStage.MoveStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail("PICKER-PLACE-STAGE-MOVE", "OutputStage",
                        description + " move command failed. result=" + result + ". " +
                        OutputStage.BuildStageAxisState(axis, target));
                }

                AxisMoveWaitResult waitResult = await OutputStage.WaitStageAxisMoveDoneInPosition(
                    axis,
                    target,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success)
                {
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-STAGE", waitResult), "OutputStage",
                        description + " move/in-position wait failed. " +
                        FormatAxisMoveWaitResult(waitResult, OutputStage.BuildStageAxisState(axis, target)));
                }

                double tolerance = ResolveOutputStageAxisTolerance(axis);
                if (!OutputStage.IsStageAxisInPosition(axis, target, tolerance))
                {
                    return Fail("PICKER-PLACE-STAGE-FINAL-POS", "OutputStage",
                        description + " final position check failed after move. " +
                        OutputStage.BuildStageAxisState(axis, target));
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-STAGE-EX", "OutputStage", description + " move exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private double ResolveOutputStageAxisTolerance(BinStageAxis axis)
        {
            try
            {
                BaseAxis item = null;

                switch (axis)
                {
                    case BinStageAxis.GoodBinY:
                        item = OutputStage != null && OutputStage.GoodStage != null ? OutputStage.GoodStage.StageY : null;
                        break;
                    case BinStageAxis.GoodBinZ:
                        item = OutputStage != null && OutputStage.GoodStage != null ? OutputStage.GoodStage.StageZ : null;
                        break;
                    case BinStageAxis.NgBinY:
                        item = OutputStage != null && OutputStage.NgStage != null ? OutputStage.NgStage.StageY : null;
                        break;
                    case BinStageAxis.VisionX:
                        item = OutputStage != null ? OutputStage.OutputCameraX : null;
                        break;
                }

                if (item != null && item.Config != null && item.Config.InPositionTolerance > 0.0)
                    return item.Config.InPositionTolerance;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.05;
        }

        private async Task<T> AwaitStepWithCancellationAsync<T>(Task<T> task, CancellationToken ct)
        {
            try
            {
                return await SequenceAwaiter.AwaitAsync(task, default(T), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }

        private int ResolveVacuumSettleMs()
        {
            if (Side == PickerSequenceSide.Front && FrontPicker != null)
                return FrontPicker.ResolvePickerVacuumSettleMs(_currentPickerNo);

            if (Side == PickerSequenceSide.Rear && RearPicker != null)
                return RearPicker.ResolvePickerVacuumSettleMs(_currentPickerNo);

            return 100;
        }

        private void ReleaseOutputStageArea()
        {
            try
            {
                if (_outputStageLease == null)
                    return;

                _outputStageLease.Dispose();
                _outputStageLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence", "OutputStageArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void ReleaseOutputPlaceArea()
        {
            try
            {
                ReleasePickerWorkArea();

                if (_outputPlaceLease == null)
                    return;

                _outputPlaceLease.Dispose();
                _outputPlaceLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence", "OutputPlaceArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}

