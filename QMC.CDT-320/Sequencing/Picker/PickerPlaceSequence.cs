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
        private string _placedDieId = "";
        private BinSide _placedOutputSide;
        private OutputStageReceiveTarget _placedReceiveTarget;
        private SequenceResourceLease _outputPlaceLease;
        private SequenceResourceLease _outputStageLease;
        private SequenceResourceLease _outputFeederLease;
        private bool _outputInspectBatchOpen;

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
                ReleaseOutputFeederArea();
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

        private OutputFeederUnit OutputFeeder
        {
            get { return Context != null && Context.Machine != null ? Context.Machine.OutputFeederUnit : null; }
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
                    ReleaseOutputFeederArea();
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
                    return VerifyOutputStageReadyAsync(ct);

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

                // 피커 X/Y/T 플레이스 티칭 위치 이동. Bin 내 Y 좌표는 OutputStageY가 담당한다.
                case PickerPlaceStep.MovePickerXYAndTToPlace:
                    return MovePickerXYAndTToPlaceAsync(ct);

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

                // Flow OFF 최종 확인
                case PickerPlaceStep.VerifyFlowOff:
                    return VerifyFlowOffAsync(ct);

                // 자재로 아웃풋 스테이지 갱신
                case PickerPlaceStep.UpdateMaterialToOutputStage:
                    return Task.FromResult(UpdateMaterialToOutputStage(ct));

                case PickerPlaceStep.RecoverOutputStageAfterPlace:
                    return RecoverOutputStageAfterPlaceAsync(ct);

                // 다음 피커 또는 완료 선택
                case PickerPlaceStep.SelectNextPickerOrComplete:
                    return Task.FromResult(SelectNextPickerOrComplete());

                // Place 완료 후 피커 전체 어보이드 복귀
                case PickerPlaceStep.MovePickerToAvoidAfterPlace:
                    return MovePickerToAvoidAfterPlaceAsync(ct);

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

            string unknownReason;
            if (HasUnknownResultDieBeforePlace(out unknownReason))
                return Fail("PICKER-PLACE-DIE-RESULT-UNKNOWN", "Material", unknownReason);

            BeginOutputPostPlaceInspectionBatch();
            CurrentStep = PickerPlaceStep.MoveAllPickerZToAvoid;
            return 0;
        }

        private bool HasUnknownResultDieBeforePlace(out string reason)
        {
            reason = string.Empty;

            try
            {
                var unknownItems = new List<string>();
                for (int i = 0; i < _pickedPickerIndexes.Count; i++)
                {
                    int pickerIndex = _pickedPickerIndexes[i];
                    int pickerNo = ToPickerNo(pickerIndex);
                    DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (die == null)
                        continue;

                    if (die.Result == DieResult.Good || die.Result == DieResult.NG)
                        continue;

                    unknownItems.Add("pickerNo=" + pickerNo + ", pickerIndex=" + pickerIndex + ", die=" + die.DieId);
                }

                if (unknownItems.Count == 0)
                    return false;

                reason = "Place 전에 검사 결과가 없는 Die가 있습니다. Bottom/Side 검사 또는 수동 Good/NG 판정 후 Place를 실행하세요. " +
                         string.Join("; ", unknownItems);
                return true;
            }
            catch (Exception ex)
            {
                reason = "Place 전 Die 검사 결과 확인 중 예외가 발생했습니다. error=" + ex.Message;
                return true;
            }
            finally
            {
            }
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
            _placedDieId = "";
            _placedReceiveTarget = null;

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

        private async Task<int> VerifyOutputStageReadyAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();

                    string reason;
                    bool materialReady = MaterialStateService.IsOutputStageReceiveAvailable(_currentOutputSide, out reason);
                    bool signalReady = IsOutputStageSideReadySignalSet(_currentOutputSide);
                    bool autoMode = Options != null && Options.RunMode == SequenceRunMode.Auto;

                    if (materialReady && (!autoMode || signalReady))
                    {
                        WriteLog("PickerPlaceSequence",
                            Name + " OutputStage 수령 준비 확인 완료. side=" + _currentOutputSide +
                            ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                            ", pickerNo=" + _currentPickerNo +
                            ", materialReady=" + materialReady +
                            ", signalReady=" + signalReady +
                            ", reason=" + reason + " - Ok");

                        CurrentStep = PickerPlaceStep.ReserveOutputStageTarget;
                        return 0;
                    }

                    string detail =
                        "OutputStage가 Die를 받을 준비가 되지 않았습니다. side=" + _currentOutputSide +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", pickerNo=" + _currentPickerNo +
                        ", materialReady=" + materialReady +
                        ", signalReady=" + signalReady +
                        ", reason=" + reason;

                    if (!autoMode)
                        return Fail("PICKER-PLACE-OUTPUT-STAGE-NOT-READY", "Material", detail);

                    WriteLog("PickerPlaceSequence", Name + " Place 대기: " + detail + " - Wait");
                    Context.StopIfCycleStopRequested("PickerPlaceSequence.WaitOutputStageReady");
                    await Task.Delay(100, ct).ConfigureAwait(false);
                }

                ct.ThrowIfCancellationRequested();
                return Fail("PICKER-PLACE-OUTPUT-STAGE-READY-CANCELED", "Material",
                    "OutputStage 수령 준비 대기가 취소되었습니다. side=" + _currentOutputSide +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-OUTPUT-STAGE-READY-EX", "Material",
                    "OutputStage 수령 준비 확인 중 예외가 발생했습니다. side=" + _currentOutputSide +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsOutputStageSideReadySignalSet(BinSide side)
        {
            try
            {
                if (Context == null || Context.Bus == null)
                    return false;

                if (side == BinSide.Good)
                    return Context.Bus.IsSet("OutputGoodStageReady");

                if (side == BinSide.Ng)
                    return Context.Bus.IsSet("OutputNgStageReady");

                return false;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 준비 신호 확인 실패. side=" + side +
                    ", error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
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
                int inspectWaitResult = await WaitOutputPostPlaceInspectionIdleAsync(ct).ConfigureAwait(false);
                if (inspectWaitResult != 0)
                    return inspectWaitResult;

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

            int result = await EnsureOutputStagePlaceEntryReadyAsync(ct).ConfigureAwait(false);

            if (result != 0)
                return result;

            result = await MoveOppositeOutputStageToAvoidForPlaceAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (IsPickerMotionOnlyTestMode())
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Picker Motion Only Test 모드: OutputVisionX 피커 진입용 Avoid 이동을 생략합니다. side=" +
                    _currentOutputSide + ", die=" + (_currentDie != null ? _currentDie.DieId : "-") + " - Check");
                CurrentStep = PickerPlaceStep.MoveOutputStageReceivePosition;
                return 0;
            }

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

        private async Task<int> EnsureOutputStagePlaceEntryReadyAsync(CancellationToken ct)
        {
            int timeout = ResolveTimeout();

            int result = await OutputStage.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, timeout, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!OutputStage.IsBinGuideClampLiftUp(BinSide.Ng))
                return Fail("PICKER-PLACE-NG-CLAMP-UP", "OutputStage",
                    "Place 진입 전 NG Bin Clamp Lift가 Up 상태가 아닙니다. " +
                    OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            result = await OutputStage.EnsureBinGuideDownAsync(BinSide.Good, timeout, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!OutputStage.IsBinGuideDown(BinSide.Good))
                return Fail("PICKER-PLACE-GOOD-GUIDE-DOWN", "OutputStage",
                    "Place 진입 전 Good Bin Guide가 Down 상태가 아닙니다. " +
                    OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

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
            int feederReady = await EnsureOutputFeederSafeBeforePlaceStageMoveAsync(ct).ConfigureAwait(false);
            if (feederReady != 0)
                return feederReady;

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

            CalculatePlaceTargetValues();

            int result = await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.VerifyPlaceTarget;
            return 0;
        }

        private async Task<int> EnsureOutputFeederSafeBeforePlaceStageMoveAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                OutputFeederUnit feeder = OutputFeeder;
                if (feeder == null)
                    return Fail("PICKER-PLACE-FEEDER-NO-UNIT", "BinFeederUnit",
                        "Place 전 OutputFeederUnit을 찾을 수 없습니다. side=" + _currentOutputSide +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-"));

                if (_outputFeederLease == null)
                {
                    _outputFeederLease = await AcquireResourceAsync(
                        SequenceResourceKind.OutputFeederArea,
                        Name + ":Place:OutputFeederAvoid:" + _currentOutputSide,
                        ct).ConfigureAwait(false);
                    if (_outputFeederLease == null)
                        return -1;
                }

                if (!feeder.IsFeederUnclamped())
                {
                    int unclampResult = await feeder.SetFeederClampAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
                    if (unclampResult != 0)
                        return Fail("PICKER-PLACE-FEEDER-UNCLAMP", feeder.Name,
                            "Place 전 OutputFeeder Unclamp 명령 실패. result=" + unclampResult +
                            ", side=" + _currentOutputSide + ", " + feeder.DescribeFeederCylinderState());
                }

                if (!feeder.IsFeederUnclamped())
                    return Fail("PICKER-PLACE-FEEDER-UNCLAMP-CHECK", feeder.Name,
                        "Place 전 OutputFeeder Unclamp 최종 확인 실패. side=" + _currentOutputSide +
                        ", " + feeder.DescribeFeederCylinderState());

                if (!feeder.IsBinFeederYInAvoidPosition())
                {
                    int moveResult = await feeder.MoveToFeederAvoidPosition(Options.FineMove).ConfigureAwait(false);
                    if (moveResult != 0)
                        return Fail("PICKER-PLACE-FEEDER-Y-AVOID", feeder.Name,
                            "Place 전 OutputFeederY Avoid 이동 명령 실패. result=" + moveResult +
                            ", side=" + _currentOutputSide + ", " + feeder.DescribeBinFeederYMoveDoneState() +
                            feeder.DescribeBinFeederYLastMotionFailure());

                    AxisMoveWaitResult waitResult = await feeder.WaitBinFeederYMoveDoneInPosition(
                        feeder.Recipe.AvoidPosition,
                        ResolveTimeout(),
                        ct).ConfigureAwait(false);
                    if (!waitResult.Success)
                        return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-FEEDER-Y-AVOID", waitResult), feeder.Name,
                            "Place 전 OutputFeederY Avoid 이동 완료 확인 실패. side=" + _currentOutputSide +
                            ", " + AxisMoveWaiter.FormatResult(waitResult, feeder.DescribeBinFeederYMoveDoneState()));
                }

                if (!feeder.IsBinFeederYInAvoidPosition())
                    return Fail("PICKER-PLACE-FEEDER-Y-AVOID-CHECK", feeder.Name,
                        "Place 전 OutputFeederY Avoid 최종 확인 실패. side=" + _currentOutputSide +
                        ", " + feeder.DescribeBinFeederYMoveDoneState());

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-FEEDER-SAFE-EX", "BinFeederUnit",
                    "Place 전 OutputFeeder 안전 위치 확보 중 예외가 발생했습니다. side=" +
                    _currentOutputSide + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CalculatePlaceTarget()
        {
            CalculatePlaceTargetValues();

            CurrentStep = PickerPlaceStep.MovePickerXYAndTToPlace;
            return 0;
        }

        private void CalculatePlaceTargetValues()
        {
            _targetPickerX = OutputStage.Recipe.VisionX.ProcessPosition +
                _receiveTarget.TargetX +
                _outputVisionToPickerX +
                ResolvePickerAlignOffsetX(_currentPickerIndex);
            _targetPickerY = GetPickerTeachingPosition(PickerAxis.PickerY, "PlacePosition");
            _targetPickerT = GetPickerTeachingPosition(GetPickerTAxis(_currentPickerIndex), "PlacePosition") +
                ResolvePickerAlignOffsetT(_currentPickerIndex);
            _targetPickerZ = GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "PlacePosition");
        }

        private async Task<int> MovePickerXYAndTToPlaceAsync(CancellationToken ct)
        {
            int result = await EnsurePickerYAtAvoidBeforePlaceMoveAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _targetPickerX;
            targets[PickerAxis.PickerY] = _targetPickerY;

            AddLoadedPickerTPlaceTargets(targets);

            result = await MovePickerAxesAndVerifyAsync(
                targets,
                "place picker X/Y/T",
                ct,
                "DiePlacePosition[" + _currentPickerIndex + "]").ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.VerifyPlaceTarget;
            return 0;
        }

        private async Task<int> MoveOutputStageYAndPickerXYTToPlaceAsync(BinStageAxis yAxis, CancellationToken ct)
        {
            int result = await EnsurePickerYAtAvoidBeforePlaceMoveAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            var pickerTargets = new Dictionary<PickerAxis, double>();
            pickerTargets[PickerAxis.PickerX] = _targetPickerX;
            pickerTargets[PickerAxis.PickerY] = _targetPickerY;
            AddLoadedPickerTPlaceTargets(pickerTargets);

            Task<int> stageYMove = MoveOutputStageAxisAndVerifyAsync(
                yAxis,
                _targetOutputStageY,
                "output stage receive Y",
                ct);
            Task<int> pickerMove = MovePickerAxesAndVerifyAsync(
                pickerTargets,
                "place picker X/Y/T",
                ct,
                "DiePlacePosition[" + _currentPickerIndex + "]");

            int[] results = await Task.WhenAll(stageYMove, pickerMove).ConfigureAwait(false);
            if (results[0] != 0 || results[1] != 0)
            {
                return Fail("PICKER-PLACE-PARALLEL-MOVE", Name,
                    "Place OutputStageY와 Picker X/Y/T 동시 이동 실패. " +
                    "stageYResult=" + results[0] +
                    ", pickerResult=" + results[1] +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide);
            }

            return 0;
        }

        private void AddLoadedPickerTPlaceTargets(IDictionary<PickerAxis, double> targets)
        {
            foreach (int pickerIndex in _pickedPickerIndexes)
            {
                PickerAxis tAxis = GetPickerTAxis(pickerIndex);
                double target = GetPickerTeachingPosition(tAxis, "PlacePosition") +
                    ResolvePickerAlignOffsetT(pickerIndex);

                if (!IsPickerAxisAlreadyInPosition(tAxis, target))
                    targets[tAxis] = target;
            }
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

            if (!IsPickerAxisInPosition(PickerAxis.PickerY, _targetPickerY))
            {
                return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                    "PickerY final teaching position check failed before place. " +
                    BuildPickerAxisState(PickerAxis.PickerY, _targetPickerY));
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
                int result = await PickerBlowAsync(_currentPickerNo, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

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

            CurrentStep = PickerPlaceStep.VerifyFlowOff;
            return 0;
        }

        private async Task<int> VerifyFlowOffAsync(CancellationToken ct)
        {
            int result = await VerifyPickerFlowStateAsync(
                _currentPickerNo,
                false,
                "Place 후 Vacuum OFF/Blow 완료 및 Picker Z Avoid 후 Flow OFF 확인",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.UpdateMaterialToOutputStage;
            return 0;
        }

        private int UpdateMaterialToOutputStage(CancellationToken ct)
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

            if (Context != null && Context.Controller != null)
                Context.Controller.RecordAutoDiePlacedForStats(_currentOutputSide);

            NotifySequenceProgressAfterPlace();

            _placedDieId = _currentDie.DieId;
            _placedOutputSide = _currentOutputSide;
            _placedReceiveTarget = _receiveTarget;

            int inspectQueueResult = RegisterOutputPostPlaceInspection(ct);
            if (inspectQueueResult != 0)
                return inspectQueueResult;

            CurrentStep = PickerPlaceStep.RecoverOutputStageAfterPlace;
            return 0;
        }

        private int RegisterOutputPostPlaceInspection(CancellationToken ct)
        {
            try
            {
                if (Context == null || Context.OutputPostPlaceInspections == null)
                {
                    return Fail("PICKER-PLACE-OUTPUT-INSPECT-QUEUE", Name,
                        "Output camera 후검사 큐가 없어 요청을 등록하지 못했습니다. die=" +
                        _placedDieId + ", side=" + _placedOutputSide);
                }

                int result = Context.OutputPostPlaceInspections.Enqueue(
                    new OutputPostPlaceInspectionRequest
                    {
                        DieId = _placedDieId,
                        OutputSide = _placedOutputSide,
                        ReceiveTarget = _placedReceiveTarget,
                        FineMove = Options != null && Options.FineMove,
                        MoveTimeoutMs = ResolveTimeout(),
                        Owner = Name,
                        SkipInspection = IsPickerMotionOnlyTestMode()
                    },
                    ct);
                if (result != 0)
                    return result;

                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 요청 등록 완료. die=" + _placedDieId +
                    ", side=" + _placedOutputSide +
                    ", pickerNo=" + _currentPickerNo + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-OUTPUT-INSPECT-QUEUE-EX", Name,
                    "Output camera 후검사 요청 등록 중 예외가 발생했습니다. die=" +
                    _placedDieId + ", side=" + _placedOutputSide +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> RecoverOutputStageAfterPlaceAsync(CancellationToken ct)
        {
            try
            {
                if (_currentOutputSide != BinSide.Ng)
                {
                    ReleaseOutputStageArea();
                    CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                    return 0;
                }

                int result = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveNgStageToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("PICKER-PLACE-NG-STAGE-AVOID-AFTER-PLACE", "OutputStage",
                        "NG Place 완료 후 NG Stage Avoid 이동 실패. result=" + result +
                        ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

                result = await MoveOutputStageAxisAndVerifyAsync(
                    BinStageAxis.GoodBinY,
                    OutputStage.Recipe.GoodStageY.ProcessPosition,
                    "NG Place 완료 후 Good Stage Y 다음 수령 기준 위치 복귀",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputStageAxisAndVerifyAsync(
                    BinStageAxis.GoodBinZ,
                    OutputStage.Recipe.GoodStageZ.ProcessPosition,
                    "NG Place 완료 후 Good Stage Z 다음 수령 높이 복귀",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ReleaseOutputStageArea();
                CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-STAGE-RECOVER-EX", "OutputStage",
                    "Place 완료 후 OutputStage 복귀 중 예외가 발생했습니다. side=" + _currentOutputSide +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitOutputPostPlaceInspectionIdleAsync(CancellationToken ct)
        {
            try
            {
                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return 0;

                return await Context.OutputPostPlaceInspections.WaitUntilIdleAsync(
                    Name + " Place 진입",
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-OUTPUT-INSPECT-WAIT-EX", Name,
                    "Place 진입 전 Output camera 후검사 완료 대기 중 예외가 발생했습니다. side=" +
                    _currentOutputSide + ", error=" + ex.Message);
            }
            finally
            {
            }
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
                CurrentStep = PickerPlaceStep.MovePickerToAvoidAfterPlace;
                return 0;
            }

            CurrentStep = PickerPlaceStep.SelectNextPicker;
            return 0;
        }

        private async Task<int> MovePickerToAvoidAfterPlaceAsync(CancellationToken ct)
        {
            try
            {
                int result = await MovePickerToAvoidAfterPlaceFastAsync(
                    "Place 완료 후 Picker 전체 Avoid 복귀",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                ReleaseOutputPlaceArea();
                ReleaseOutputStageArea();
                ReleaseOutputFeederArea();
                EndOutputPostPlaceInspectionBatch();
                CurrentStep = PickerPlaceStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-AVOID-EX", Name,
                    "Place 완료 후 Picker Avoid 복귀 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerToAvoidAfterPlaceFastAsync(string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveAllPickerZToAvoidAndVerifyAsync(
                    description + " Z축 Avoid",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition"),
                    description + " Y축 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=PlaceDoneSafeY").ConfigureAwait(false);
                if (result != 0)
                    return result;

                var tTargets = new Dictionary<PickerAxis, double>();
                tTargets[PickerAxis.PickerT0] = GetPickerTeachingPosition(PickerAxis.PickerT0, "AvoidPosition");
                tTargets[PickerAxis.PickerT1] = GetPickerTeachingPosition(PickerAxis.PickerT1, "AvoidPosition");
                tTargets[PickerAxis.PickerT2] = GetPickerTeachingPosition(PickerAxis.PickerT2, "AvoidPosition");
                tTargets[PickerAxis.PickerT3] = GetPickerTeachingPosition(PickerAxis.PickerT3, "AvoidPosition");
                tTargets[PickerAxis.PickerX] = GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition");

                result = await MovePickerAxesAndVerifyAsync(
                    tTargets,
                    description + " X/T축 병렬 Avoid",
                    ct,
                    "AvoidPosition;PickerPhase=PlaceDoneSafeXT").ConfigureAwait(false);
                if (result != 0)
                    return result;

                PickerAxis[] finalAxes =
                {
                    PickerAxis.PickerX,
                    PickerAxis.PickerY,
                    PickerAxis.PickerT0,
                    PickerAxis.PickerT1,
                    PickerAxis.PickerT2,
                    PickerAxis.PickerT3,
                    PickerAxis.PickerZ0,
                    PickerAxis.PickerZ1,
                    PickerAxis.PickerZ2,
                    PickerAxis.PickerZ3
                };

                foreach (PickerAxis axis in finalAxes)
                {
                    double target = GetPickerTeachingPosition(axis, "AvoidPosition");
                    if (!IsPickerAxisInPosition(axis, target))
                    {
                        return Fail("PICKER-PLACE-AVOID-FINAL-POS", Name,
                            description + " 최종 Avoid 위치 확인 실패. " +
                            BuildPickerAxisState(axis, target));
                    }
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-AVOID-SEQ-EX", Name,
                    description + " 안전 순서 Avoid 복귀 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
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

        private void ReleaseOutputFeederArea()
        {
            try
            {
                if (_outputFeederLease == null)
                    return;

                _outputFeederLease.Dispose();
                _outputFeederLease = null;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence", "OutputFeederArea lease release failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void BeginOutputPostPlaceInspectionBatch()
        {
            try
            {
                if (_outputInspectBatchOpen)
                    return;

                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return;

                Context.OutputPostPlaceInspections.BeginBatch(Name);
                _outputInspectBatchOpen = true;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 묶음 시작 처리 중 예외가 발생했습니다. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void EndOutputPostPlaceInspectionBatch()
        {
            try
            {
                if (!_outputInspectBatchOpen)
                    return;

                _outputInspectBatchOpen = false;

                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return;

                Context.OutputPostPlaceInspections.EndBatch(Name);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 묶음 종료 처리 중 예외가 발생했습니다. error=" +
                    ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}

