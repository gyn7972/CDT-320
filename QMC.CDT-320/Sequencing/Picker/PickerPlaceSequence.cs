using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Recipes;
using QMC.Common.Motion;
using QMC.Common;

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
        private bool _pickerZPlacedBySynchronizedArrival;
        private int _pendingSynchronizedRetreatPickerIndex = -1;
        private int _pendingSynchronizedRetreatPickerNo;

        public bool ForceSafeYBeforeFirstPlaceMove { get; set; }

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
                ClearPendingSynchronizedRetreat();
                ReleaseOutputPlaceArea();
                ReleaseOutputStageArea();
                ReleaseOutputFeederArea();
                EndOutputPostPlaceInspectionBatch();
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
                    int stepResult = await Context.Tact.StepAsync(this, CurrentStep, ct, () => ExecuteStepAsync(ct)).ConfigureAwait(false);
                    keepCurrentState = stepResult == 0 && CurrentStep != PickerPlaceStep.Complete;
                    return stepResult;
                }

                while (CurrentStep != PickerPlaceStep.Complete)
                {
                    ct.ThrowIfCancellationRequested();

                    int result = await Context.Tact.StepAsync(this, CurrentStep, ct, () => ExecuteStepAsync(ct)).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return 0;
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
                return Fail("PICKER-PLACE-EX", Name, "Picker place failed. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
                if (!keepCurrentState)
                {
                    ReleaseOutputPlaceArea();
                    ReleaseOutputStageArea();
                    ReleaseOutputFeederArea();
                    EndOutputPostPlaceInspectionBatch();
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
                    Log.Write("PickerPlaceSequence", Name + " Place 시작. side=" + Side + ", step=" + CurrentStep);
                    return Task.FromResult(CheckUnit());

                // 픽업 피커 목록 생성
                case PickerPlaceStep.BuildPickedPickerList:
                    Log.Write("PickerPlaceSequence", Name + " Place 피커 목록 생성 시작. side=" + Side + ", step=" + CurrentStep);
                    return Task.FromResult(BuildPickedPickerList());

                // 전체 피커 Z로 어보이드 이동
                case PickerPlaceStep.MoveAllPickerZToAvoid:
                    Log.Write("PickerPlaceSequence", Name + " Place 전체 피커 Z Avoid 이동 시작. side=" + Side + ", step=" + CurrentStep);
                    return MoveAllPickerZToAvoidAsync(ct);

                // 다음 피커 선택
                case PickerPlaceStep.SelectNextPicker:
                    Log.Write("PickerPlaceSequence", Name + " Place 다음 피커 선택 시작. side=" + Side + ", step=" + CurrentStep);
                    return Task.FromResult(SelectNextPicker());

                // 아웃풋 사이드 결정
                case PickerPlaceStep.ResolveOutputSide:
                    Log.Write("PickerPlaceSequence", Name + " Place 아웃풋 사이드 결정 시작. side=" + Side + ", step=" + CurrentStep);
                    return Task.FromResult(ResolveOutputSide());

                // 아웃풋 스테이지 수령 가능 확인
                case PickerPlaceStep.VerifyOutputStageReady:
                    Log.Write("PickerPlaceSequence", Name + " Place 아웃풋 스테이지 수령 준비 확인 시작. side=" + Side + ", step=" + CurrentStep);
                    return VerifyOutputStageReadyAsync(ct);

                // 아웃풋 스테이지 대상 예약
                case PickerPlaceStep.ReserveOutputStageTarget:
                    Log.Write("PickerPlaceSequence", Name + " Place 아웃풋 스테이지 대상 예약 시작. side=" + Side + ", step=" + CurrentStep);
                    return Task.FromResult(ReserveOutputStageTarget());

                // 아웃풋 스테이지 피커 진입용 어보이드 이동
                case PickerPlaceStep.MoveOutputStageAvoidPosition:
                    Log.Write("PickerPlaceSequence", Name + " Place 아웃풋 스테이지 피커 진입용 Avoid 이동 시작. side=" + Side + ", step=" + CurrentStep);
                    return MoveOutputStageAvoidPositionAsync(ct);

                // 아웃풋 스테이지 수령 위치 이동
                case PickerPlaceStep.MoveOutputStageReceivePosition:
                    Log.Write("PickerPlaceSequence", Name + " Place 아웃풋 스테이지 수령 위치 이동 시작. side=" + Side + ", step=" + CurrentStep);
                    return MoveOutputStageReceivePositionAsync(ct);

                // 플레이스 대상 계산
                case PickerPlaceStep.CalculatePlaceTarget:
                    Log.Write("PickerPlaceSequence", Name + " Place 대상 계산 시작. side=" + Side + ", step=" + CurrentStep);
                    return Task.FromResult(CalculatePlaceTarget());

                // 피커 X/Y/T 플레이스 티칭 위치 이동. Bin 내 Y 좌표는 OutputStageY가 담당한다.
                case PickerPlaceStep.MovePickerXYAndTToPlace:
                    Log.Write("PickerPlaceSequence", Name + " Place 피커 XY/T 플레이스 티칭 위치 이동 시작. side=" + Side + ", step=" + CurrentStep);
                    return MovePickerXYAndTToPlaceAsync(ct);

                // 플레이스 대상 검증
                case PickerPlaceStep.VerifyPlaceTarget:
                    Log.Write("PickerPlaceSequence", Name + " Place 대상 검증 시작. side=" + Side + ", step=" + CurrentStep);
                    return Task.FromResult(VerifyPlaceTarget());

                // 피커 Z 플레이스 이동
                case PickerPlaceStep.MovePickerZPlace:
                    Log.Write("PickerPlaceSequence", Name + " Place 피커 Z 플레이스 이동 시작. side=" + Side + ", step=" + CurrentStep);
                    return MovePickerZPlaceAsync(ct);

                // 진공 OFF 처리
                case PickerPlaceStep.VacuumOff:
                    Log.Write("PickerPlaceSequence", Name + " Place 진공 OFF 처리 시작. side=" + Side + ", step=" + CurrentStep);
                    return VacuumOffAsync(ct);

                // 블로우 OFF 처리
                case PickerPlaceStep.BlowOff:
                    Log.Write("PickerPlaceSequence", Name + " Place 블로우 OFF 처리 시작. side=" + Side + ", step=" + CurrentStep);
                    return BlowOffAsync(ct);

                // 피커 Z로 어보이드 이동
                case PickerPlaceStep.MovePickerZToAvoid:
                    Log.Write("PickerPlaceSequence", Name + " Place 피커 Z Avoid 이동 시작. side=" + Side + ", step=" + CurrentStep);
                    return MovePickerZToAvoidAsync(ct);

                // Flow OFF 최종 확인
                case PickerPlaceStep.VerifyFlowOff:
                    Log.Write("PickerPlaceSequence", Name + " Place Flow OFF 최종 확인 시작. side=" + Side + ", step=" + CurrentStep);
                    return VerifyFlowOffAsync(ct);

                // 자재로 아웃풋 스테이지 갱신
                case PickerPlaceStep.UpdateMaterialToOutputStage:
                    Log.Write("PickerPlaceSequence", Name + " Place 자재로 아웃풋 스테이지 갱신 시작. side=" + Side + ", step=" + CurrentStep);
                    return Task.FromResult(UpdateMaterialToOutputStage(ct));

                case PickerPlaceStep.RecoverOutputStageAfterPlace:
                    Log.Write("PickerPlaceSequence", Name + " Place 완료 후 아웃풋 스테이지 복귀 시작. side=" + Side + ", step=" + CurrentStep);
                    return RecoverOutputStageAfterPlaceAsync(ct);

                // 다음 피커 또는 완료 선택
                case PickerPlaceStep.SelectNextPickerOrComplete:
                    Log.Write("PickerPlaceSequence", Name + " Place 완료 후 다음 피커 또는 완료 선택 시작. side=" + Side + ", step=" + CurrentStep);
                    return Task.FromResult(SelectNextPickerOrComplete());

                // Place 완료 후 피커 전체 어보이드 복귀
                case PickerPlaceStep.MovePickerToAvoidAfterPlace:
                    Log.Write("PickerPlaceSequence", Name + " Place 완료 후 피커 전체 어보이드 복귀 시작. side=" + Side + ", step=" + CurrentStep);
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

                    if (IsInspectionFlowComplete(die) &&
                        (die.Result == DieResult.Good || die.Result == DieResult.NG))
                        continue;

                    unknownItems.Add(
                        "pickerNo=" + pickerNo +
                        ", pickerIndex=" + pickerIndex +
                        ", die=" + die.DieId +
                        ", result=" + die.Result +
                        ", bottomDone=" + HasInspectionResult(die, "Bottom") +
                        ", side0Done=" + HasInspectionResult(die, "Side0") +
                        ", side90Done=" + HasInspectionResult(die, "Side90"));
                }

                if (unknownItems.Count == 0)
                    return false;

                reason = "Place 전에 Bottom/Side 검사 흐름이 완료되지 않은 Die가 있습니다. 검사 NG는 정지 조건이 아니며, Bottom/Side 검사를 모두 완료한 뒤 Good/NG 판정에 따라 Place해야 합니다. " +
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

        private static bool IsInspectionFlowComplete(DieMaterial die)
        {
            return HasInspectionResult(die, "Bottom") &&
                   HasInspectionResult(die, "Side0") &&
                   HasInspectionResult(die, "Side90");
        }

        private static bool HasInspectionResult(DieMaterial die, string inspectionType)
        {
            try
            {
                if (die == null || die.Inspections == null || string.IsNullOrWhiteSpace(inspectionType))
                    return false;

                for (int i = 0; i < die.Inspections.Count; i++)
                {
                    DieInspectionRecord record = die.Inspections[i];
                    if (record == null)
                        continue;

                    if (!string.Equals(record.InspectionType, inspectionType, StringComparison.OrdinalIgnoreCase))
                        continue;

                    return record.Result != MaterialInspectionResult.Unknown;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<int> MoveAllPickerZToAvoidAsync(CancellationToken ct)
        {
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
            _pickerZPlacedBySynchronizedArrival = false;

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
            if (!IsInspectionFlowComplete(_currentDie))
            {
                return Fail("PICKER-PLACE-INSPECTION-INCOMPLETE", "Material",
                    "Place 전 Bottom/Side 검사 흐름이 완료되지 않았습니다. 검사 NG는 정지 조건이 아니며 모든 검사를 완료한 뒤 NG Stage로 배출해야 합니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", bottomDone=" + HasInspectionResult(_currentDie, "Bottom") +
                    ", side0Done=" + HasInspectionResult(_currentDie, "Side0") +
                    ", side90Done=" + HasInspectionResult(_currentDie, "Side90"));
            }

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
                bool safeWaitPositionPrepared = false;
                if (ForceSafeYBeforeFirstPlaceMove)
                {
                    int waitSafeResult = await MovePickerToSafeYBeforeOutputStageReadyWaitAsync(ct).ConfigureAwait(false);
                    if (waitSafeResult != 0)
                        return waitSafeResult;

                    safeWaitPositionPrepared = true;
                    WriteLog("PickerPlaceSequence",
                        Name + " Place 재시작 안전 진입: OutputStage 준비 확인 전 PickerY를 Avoid로 정리했습니다. " +
                        "side=" + Side +
                        ", outputSide=" + _currentOutputSide +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", pickerNo=" + _currentPickerNo + " - Ok");
                }

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

                    if (!safeWaitPositionPrepared)
                    {
                        int waitSafeResult = await MovePickerToSafeYBeforeOutputStageReadyWaitAsync(ct).ConfigureAwait(false);
                        if (waitSafeResult != 0)
                            return waitSafeResult;

                        safeWaitPositionPrepared = true;
                    }

                    WriteLog("PickerPlaceSequence", Name + " Place 대기: " + detail + " - Wait");
                    Context.StopIfCycleStopRequested("PickerPlaceSequence.WaitOutputStageReady");
                    await Task.Delay(1, ct).ConfigureAwait(false);
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

        private async Task<int> MovePickerToSafeYBeforeOutputStageReadyWaitAsync(CancellationToken ct)
        {
            try
            {
                int zResult = await MoveAllPickerZToAvoidAndVerifyAsync(
                    "Place 준비 대기 전 PickerZ 전체 Avoid",
                    ct).ConfigureAwait(false);
                if (zResult != 0)
                    return zResult;

                double yAvoid = GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                if (IsPickerAxisInPosition(PickerAxis.PickerY, yAvoid))
                    return 0;

                int yResult = await MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    yAvoid,
                    "Place 준비 대기 전 PickerY SafeY",
                    ct,
                    "AvoidPosition;PickerPhase=PlaceReadyWaitSafeY").ConfigureAwait(false);
                if (yResult != 0)
                    return yResult;

                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 준비 대기 전 PickerY를 SafeY/Avoid로 이동했습니다. " +
                    "side=" + Side +
                    ", outputSide=" + _currentOutputSide +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo + " - Ok");
                return 0;
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
                return Fail("PICKER-PLACE-WAIT-SAFE-Y-EX", Name,
                    "OutputStage 준비 대기 전 PickerY SafeY 이동 중 예외가 발생했습니다. side=" +
                    Side + ", outputSide=" + _currentOutputSide +
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

            // OutputFeeder/OutputStage 로딩이 1순위다.
            // Picker는 OutputPlace/Stage/Feeder 리소스와 Feeder 안전 위치가 확보된 뒤에만
            // Output work area를 점유해야 Feeder 로딩 중 Picker owner로 인한 인터락 오판이 생기지 않는다.
            EnsurePickerWorkAreaReserved(PickerWorkZone.Output, "Place");

            BinStageAxis yAxis = _currentOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;

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

            CalculatePlaceTargetValues();
            Log.Write("PickerPlaceSequence", Name + " Place 대상 좌표 계산 완료. side=" + Side + ", step=" + CurrentStep);

            int result = await MoveOutputStageYPickerXAndPickerZToPlaceByModeAsync(yAxis, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (ForceSafeYBeforeFirstPlaceMove)
                ForceSafeYBeforeFirstPlaceMove = false;

            Log.Write("PickerPlaceSequence", Name + " Place 대상 좌표 이동 완료. side=" + Side + ", step=" + CurrentStep);

            if (!_pickerZPlacedBySynchronizedArrival)
            {
                result = await EnsureOutputStageZReadyForPlaceAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
            }

            Log.Write("PickerPlaceSequence", Name + " Place 대상 좌표 이동 후 Z Ready 확인 완료. side=" + Side + ", step=" + CurrentStep);
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
            string dieId = _currentDie != null ? _currentDie.DieId : string.Empty;
            double outputStageBaseY = _currentOutputSide == BinSide.Ng
                ? OutputStage.Recipe.NGStageY.ProcessPosition
                : OutputStage.Recipe.GoodStageY.ProcessPosition;

            PlaceCoordinateResult coordinate = DieCoordinateTransformService.CalculatePlaceTarget(
                Name,
                Side,
                _currentPickerIndex,
                dieId,
                _currentOutputSide,
                outputStageBaseY,
                _receiveTarget != null ? _receiveTarget.TargetY : 0.0,
                OutputStage.Recipe.VisionX.ProcessPosition,
                _receiveTarget != null ? _receiveTarget.TargetX : 0.0,
                _outputVisionToPickerX,
                _outputVisionToPickerY,
                ResolvePickerAlignOffsetX(_currentPickerIndex),
                GetPickerTeachingPosition(PickerAxis.PickerY, "PlacePosition"),
                GetPickerTeachingPosition(GetPickerTAxis(_currentPickerIndex), "PlacePosition"),
                ResolvePickerAlignOffsetT(_currentPickerIndex),
                GetPickerTeachingPosition(GetPickerZAxis(_currentPickerIndex), "PlacePosition"));

            _targetOutputStageY = coordinate.OutputStageY;
            _targetPickerX = coordinate.PickerX;
            _targetPickerY = coordinate.PickerY;
            _targetPickerT = coordinate.PickerT;
            _targetPickerZ = coordinate.PickerZ;
        }

        private async Task<int> MovePickerXYAndTToPlaceAsync(CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _targetPickerX;
            targets[PickerAxis.PickerY] = _targetPickerY;

            AddLoadedPickerTPlaceTargets(targets);

            int result = await MovePickerXTThenYAndVerifyAsync(
                targets,
                "place picker X/Y/T",
                ct,
                BuildPlaceMoveTargetName()).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.VerifyPlaceTarget;
            return 0;
        }

        private async Task<int> MoveOutputStageYAndPickerXYTToPlaceAsync(BinStageAxis yAxis, CancellationToken ct)
        {
            var pickerTargets = new Dictionary<PickerAxis, double>();
            pickerTargets[PickerAxis.PickerX] = _targetPickerX;
            pickerTargets[PickerAxis.PickerY] = _targetPickerY;
            AddLoadedPickerTPlaceTargets(pickerTargets);

            Task<int> stageYMove = MoveOutputStageAxisAndVerifyAsync(
                yAxis,
                _targetOutputStageY,
                "output stage receive Y",
                ct,
                BuildOutputStagePlaceMoveTargetName("ReceiveY"));
            Task<int> pickerMove = MovePickerXTThenYAndVerifyAsync(
                pickerTargets,
                "place picker X/Y/T",
                ct,
                BuildPlaceMoveTargetName());

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

            //await MovePickerAxesAndVerifyAsync(
            //    pickerTargets,
            //    "place picker X/Y/T",
            //    ct,
            //    "DiePlacePosition[" + _currentPickerIndex + "]");

            //Log.Write("PickerPlaceSequence", Name + " MovePickerAxesAndVerifyAsync. side=" + Side + ", step=" + CurrentStep);
            //await MoveOutputStageAxisAndVerifyAsync(
            //    yAxis,
            //    _targetOutputStageY,
            //    "output stage receive Y",
            //    ct);
            //Log.Write("PickerPlaceSequence", Name + " MoveOutputStageAxisAndVerifyAsync. side=" + Side + ", step=" + CurrentStep);
            return 0;
        }

        private async Task<int> MoveOutputStageYAndPickerXTThenYToPlaceAsync(BinStageAxis yAxis, CancellationToken ct)
        {
            var pickerXAndTTargets = new Dictionary<PickerAxis, double>();
            pickerXAndTTargets[PickerAxis.PickerX] = _targetPickerX;
            AddLoadedPickerTPlaceTargets(pickerXAndTTargets);

            Task<int> stageYMove = MoveOutputStageAxisAndVerifyAsync(
                yAxis,
                _targetOutputStageY,
                "Place 재시작 OutputStageY 수령 위치",
                ct,
                BuildOutputStagePlaceMoveTargetName("ResumeReceiveY"));
            Task<int> pickerXAndTMove = MovePickerAxesAndVerifyAsync(
                pickerXAndTTargets,
                "Place 재시작 Picker X/T",
                ct,
                BuildPlaceMoveTargetName());

            int[] results = await Task.WhenAll(stageYMove, pickerXAndTMove).ConfigureAwait(false);
            if (results[0] != 0 || results[1] != 0)
            {
                return Fail("PICKER-PLACE-RESUME-XT-STAGE-MOVE", Name,
                    "Place 재시작 OutputStageY와 Picker X/T 선행 이동 실패. " +
                    "stageYResult=" + results[0] +
                    ", pickerXTResult=" + results[1] +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide);
            }

            if (IsPickerAxisAlreadyInPosition(PickerAxis.PickerY, _targetPickerY))
                return 0;

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                _targetPickerY,
                "Place 재시작 PickerY 전진",
                ct,
                BuildPlaceMoveTargetName()).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageYPickerXAndPickerZToPlaceByModeAsync(BinStageAxis yAxis, CancellationToken ct)
        {
            _pickerZPlacedBySynchronizedArrival = false;

            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            if (ForceSafeYBeforeFirstPlaceMove)
            {
                int pendingResult = await CompletePendingSynchronizedRetreatIfNeededAsync("Place 재시작 안전 진입으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                int safeYResult = await MovePickerToSafeYBeforeOutputStageReadyWaitAsync(ct).ConfigureAwait(false);
                if (safeYResult != 0)
                    return safeYResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place 재시작 첫 접근은 PickerY Avoid 상태에서 보간/선행 Y 전진을 사용하지 않고 X/T 이동 후 Y 전진 순서로 진행합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", pickerIndex=" + _currentPickerIndex +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXTThenYToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            if (placeConfig == null || !IsCoordinatedPlaceMotionMode(placeConfig.MotionMode))
            {
                int pendingResult = await CompletePendingSynchronizedRetreatIfNeededAsync("Place 보간 모드가 아니어서 이전 PickerZ를 먼저 Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            if (IsFirstPlaceMoveInBatch())
            {
                int pendingResult = await CompletePendingSynchronizedRetreatIfNeededAsync("Place 첫 번째 접근 전 이전 PickerZ 안전 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place 첫 번째 접근 이동은 보간을 사용하지 않고 기존 이동 방식으로 진행합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", pickerIndex=" + _currentPickerIndex +
                    ", cursor=" + _pickerCursor +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            BaseAxis stageY = ResolveOutputStageYAxis(yAxis);
            BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
            BaseAxis pickerZ = GetPickerAxis(GetPickerZAxis(_currentPickerIndex));
            BaseAxis previousPickerZ = HasPendingSynchronizedRetreat()
                ? GetPickerAxis(GetPickerZAxis(_pendingSynchronizedRetreatPickerIndex))
                : null;
            double previousPickerZAvoid = HasPendingSynchronizedRetreat()
                ? GetPickerTeachingPosition(GetPickerZAxis(_pendingSynchronizedRetreatPickerIndex), "AvoidPosition")
                : 0.0;

            if (placeConfig.MotionMode == PickerPlaceMotionMode.ContiSegmentedPlace)
            {
                return await MoveOutputStageYPickerXAndPickerZByContiSegmentedPlaceAsync(
                    yAxis,
                    stageY,
                    pickerX,
                    previousPickerZ,
                    previousPickerZAvoid,
                    pickerZ,
                    placeConfig,
                    ct).ConfigureAwait(false);
            }

            string guardReason;
            if (!CanUseSynchronizedArrivalFromCurrentPosition(stageY, pickerX, pickerZ, previousPickerZ, previousPickerZAvoid, placeConfig, out guardReason))
            {
                int pendingResult = await CompletePendingSynchronizedRetreatIfNeededAsync("Place 보간 조건 불만족으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place 보간 이동 조건 불만족으로 기존 이동 방식으로 접근합니다. " +
                    "reason=" + guardReason +
                    ", pickerNo=" + _currentPickerNo +
                    ", pickerIndex=" + _currentPickerIndex +
                    ", cursor=" + _pickerCursor +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide +
                    ", maxTravel=" + placeConfig.MaxSynchronizedTravelDistance.ToString("F3") +
                    ", stageYTravel=" + FormatTravel(stageY, _targetOutputStageY) +
                    ", pickerXTravel=" + FormatTravel(pickerX, _targetPickerX) +
                    ", pickerZTravel=" + FormatTravel(pickerZ, _targetPickerZ) +
                    ", previousPickerZTravel=" + FormatTravel(previousPickerZ, previousPickerZAvoid) +
                    " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            int preMove = await MovePickerYAndTToPlaceBeforeSynchronizedArrivalAsync(ct).ConfigureAwait(false);
            if (preMove != 0)
                return preMove;

            int zReady = await EnsureOutputStageZReadyForPlaceAsync(ct).ConfigureAwait(false);
            if (zReady != 0)
                return zReady;

            InterpolatedMotionMoveResult syncResult =
                await PickerPlaceSynchronizedArrivalMotion.MoveStageYPickerXAndPickerZToPlaceAsync(
                    stageY,
                    _targetOutputStageY,
                    pickerX,
                    _targetPickerX,
                    previousPickerZ,
                    previousPickerZAvoid,
                    pickerZ,
                    _targetPickerZ,
                    placeConfig,
                    ct).ConfigureAwait(false);

            if (syncResult != null && syncResult.Success)
            {
                int finalWait = await WaitSynchronizedArrivalFinalPositionAsync(
                    yAxis,
                    HasPendingSynchronizedRetreat() ? GetPickerZAxis(_pendingSynchronizedRetreatPickerIndex) : (PickerAxis?)null,
                    previousPickerZAvoid,
                    GetPickerZAxis(_currentPickerIndex),
                    Math.Max(placeConfig.SynchronizedTimeoutMs, ResolveTimeout()),
                    ct).ConfigureAwait(false);
                if (finalWait != 0)
                    return finalWait;

                WriteLog("PickerPlaceSequence",
                    Name + " Place 보간 이동 완료. die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", " + syncResult + " - Ok");
                ClearPendingSynchronizedRetreat();
                _pickerZPlacedBySynchronizedArrival = true;
                return 0;
            }

            if (syncResult != null && syncResult.CommandIssued)
            {
                _pickerZPlacedBySynchronizedArrival = false;
                return Fail("PICKER-PLACE-SYNC-MOVE", Name,
                    "Place 보간 이동 명령 후 완료 확인에 실패했습니다. 기존 이동 방식으로 전환하지 않고 정지합니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", " + syncResult);
            }

            WriteLog("PickerPlaceSequence",
                Name + " Place 보간 이동 실패로 기존 이동 방식으로 전환합니다. die=" +
                (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", result=" + (syncResult != null ? syncResult.ResultCode.ToString() : "-") +
                ", reason=" + (syncResult != null ? syncResult.Message : "결과 없음") +
                " - Check");

            int pendingFallbackResult = await CompletePendingSynchronizedRetreatIfNeededAsync("Place 보간 명령 전 실패로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
            if (pendingFallbackResult != 0)
                return pendingFallbackResult;

            _pickerZPlacedBySynchronizedArrival = false;
            return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
        }

        private static bool IsCoordinatedPlaceMotionMode(PickerPlaceMotionMode mode)
        {
            return mode == PickerPlaceMotionMode.SynchronizedArrival ||
                mode == PickerPlaceMotionMode.ContiSegmentedPlace;
        }

        private async Task<int> MoveOutputStageYPickerXAndPickerZByContiSegmentedPlaceAsync(
            BinStageAxis yAxis,
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis previousPickerZ,
            double previousPickerZAvoid,
            BaseAxis pickerZ,
            PickerPlaceMotionConfig placeConfig,
            CancellationToken ct)
        {
            string guardReason;
            if (!CanUseContiSegmentedPlaceFromCurrentPosition(stageY, pickerX, pickerZ, previousPickerZ, previousPickerZAvoid, placeConfig, out guardReason))
            {
                int pendingResult = await CompletePendingSynchronizedRetreatIfNeededAsync("Place ContiNode 조건 불만족으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 이동 조건 불만족으로 기존 이동 방식으로 접근합니다. " +
                    "reason=" + guardReason +
                    ", pickerNo=" + _currentPickerNo +
                    ", pickerIndex=" + _currentPickerIndex +
                    ", cursor=" + _pickerCursor +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide +
                    ", maxTravel=" + placeConfig.MaxSynchronizedTravelDistance.ToString("F3") +
                    ", stageYTravel=" + FormatTravel(stageY, _targetOutputStageY) +
                    ", pickerXTravel=" + FormatTravel(pickerX, _targetPickerX) +
                    ", previousPickerZTravel=" + FormatTravel(previousPickerZ, previousPickerZAvoid) +
                    " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            IList<PickerPlaceContiNode> nodes = BuildContiSegmentedPlaceNodes(stageY, pickerX, previousPickerZ, pickerZ, placeConfig);
            if (nodes == null || nodes.Count == 0)
            {
                int pendingResult = await CompletePendingSynchronizedRetreatIfNeededAsync("Place ContiNode 노드 생성 실패로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 노드 생성 실패로 기존 이동 방식으로 접근합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", pickerIndex=" + _currentPickerIndex +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            if (!CanUseContiSegmentedNodesFromCurrentPosition(stageY, pickerX, previousPickerZ, pickerZ, nodes, placeConfig, out guardReason))
            {
                int pendingResult = await CompletePendingSynchronizedRetreatIfNeededAsync("Place ContiNode 노드 거리 조건 불만족으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 노드 거리 조건 불만족으로 기존 이동 방식으로 접근합니다. " +
                    "reason=" + guardReason +
                    ", pickerNo=" + _currentPickerNo +
                    ", pickerIndex=" + _currentPickerIndex +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            int preMove = await MovePickerYAndTToPlaceBeforeSynchronizedArrivalAsync(ct).ConfigureAwait(false);
            if (preMove != 0)
                return preMove;

            int zReady = await EnsureOutputStageZReadyForPlaceAsync(ct).ConfigureAwait(false);
            if (zReady != 0)
                return zReady;

            double finalPickerZ = nodes[nodes.Count - 1].PickerZ;
            double originalPickerZTarget = _targetPickerZ;
            _targetPickerZ = finalPickerZ;

            InterpolatedMotionMoveResult contiResult =
                await PickerPlaceContiSegmentedMotion.MoveStageYPickerXAndPickerZByNodesAsync(
                    stageY,
                    pickerX,
                    previousPickerZ,
                    pickerZ,
                    nodes,
                    placeConfig,
                    ct).ConfigureAwait(false);

            if (contiResult != null && contiResult.Success)
            {
                int finalWait = await WaitSynchronizedArrivalFinalPositionAsync(
                    yAxis,
                    HasPendingSynchronizedRetreat() ? GetPickerZAxis(_pendingSynchronizedRetreatPickerIndex) : (PickerAxis?)null,
                    previousPickerZAvoid,
                    GetPickerZAxis(_currentPickerIndex),
                    Math.Max(placeConfig.SynchronizedTimeoutMs, ResolveTimeout()),
                    ct).ConfigureAwait(false);
                if (finalWait != 0)
                    return finalWait;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 이동 완료. die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", basePickerZ=" + originalPickerZTarget.ToString("F3") +
                    ", finalPickerZ=" + finalPickerZ.ToString("F3") +
                    ", " + contiResult + " - Ok");
                ClearPendingSynchronizedRetreat();
                _pickerZPlacedBySynchronizedArrival = true;
                return 0;
            }

            if (contiResult != null && contiResult.CommandIssued)
            {
                _pickerZPlacedBySynchronizedArrival = false;
                return Fail("PICKER-PLACE-CONTI-MOVE", Name,
                    "Place ContiNode 이동 명령 후 완료 확인에 실패했습니다. 기존 이동 방식으로 전환하지 않고 정지합니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", " + contiResult);
            }

            _targetPickerZ = originalPickerZTarget;
            WriteLog("PickerPlaceSequence",
                Name + " Place ContiNode 이동 명령 전 실패로 기존 이동 방식으로 전환합니다. die=" +
                (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", result=" + (contiResult != null ? contiResult.ResultCode.ToString() : "-") +
                ", reason=" + (contiResult != null ? contiResult.Message : "결과 없음") +
                " - Check");

            int pendingFallbackResult = await CompletePendingSynchronizedRetreatIfNeededAsync("Place ContiNode 명령 전 실패로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
            if (pendingFallbackResult != 0)
                return pendingFallbackResult;

            _pickerZPlacedBySynchronizedArrival = false;
            return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
        }

        private IList<PickerPlaceContiNode> BuildContiSegmentedPlaceNodes(
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis previousPickerZ,
            BaseAxis pickerZ,
            PickerPlaceMotionConfig placeConfig)
        {
            if (stageY == null || pickerX == null || previousPickerZ == null || pickerZ == null || placeConfig == null)
                return new List<PickerPlaceContiNode>();

            PickerAxis previousZAxis = GetPickerZAxis(_pendingSynchronizedRetreatPickerIndex);
            PickerAxis currentZAxis = GetPickerZAxis(_currentPickerIndex);

            double previousPlaceBase = GetPickerTeachingPosition(previousZAxis, "PlacePosition");
            double previousAvoid = GetPickerTeachingPosition(previousZAxis, "AvoidPosition");
            double currentPlaceBase = _targetPickerZ;
            double currentAvoid = GetPickerTeachingPosition(currentZAxis, "AvoidPosition");
            double tapeThickness = ResolveTapeThicknessMm(placeConfig);
            double dieThickness = ResolveDieThicknessMm(placeConfig);
            double materialOffset = tapeThickness + dieThickness;
            double previousMaterialBase = previousPlaceBase + materialOffset;
            double currentMaterialBase = currentPlaceBase + materialOffset;

            double previousZNode0 = previousMaterialBase + placeConfig.ContiZ1Step1Clearance;
            double previousZNode1 = previousMaterialBase + placeConfig.ContiZ1Step1Clearance + placeConfig.ContiZ1Step2Clearance;
            double previousZNearAvoid = ResolveNearAvoidPosition(previousAvoid, previousPlaceBase, placeConfig.ContiNearAvoidDistance);
            double currentZNearAvoid = ResolveNearAvoidPosition(currentAvoid, currentPlaceBase, placeConfig.ContiNearAvoidDistance);
            double currentZNode3 = currentMaterialBase + placeConfig.ContiZ1Step1Clearance + placeConfig.ContiZ1Step2Clearance;
            double currentZFinal = currentMaterialBase - placeConfig.ContiOverDrive;

            double ratio = placeConfig.ContiXYMidRatio;
            double stageYMid = stageY.ActualPosition + ((_targetOutputStageY - stageY.ActualPosition) * ratio);
            double pickerXMid = pickerX.ActualPosition + ((_targetPickerX - pickerX.ActualPosition) * ratio);

            var nodes = new List<PickerPlaceContiNode>();
            nodes.Add(new PickerPlaceContiNode(0, stageY.ActualPosition, pickerX.ActualPosition, previousZNode0, pickerZ.ActualPosition));
            nodes.Add(new PickerPlaceContiNode(1, stageY.ActualPosition, pickerX.ActualPosition, previousZNode1, pickerZ.ActualPosition));
            nodes.Add(new PickerPlaceContiNode(2, stageYMid, pickerXMid, previousZNearAvoid, currentZNearAvoid));
            nodes.Add(new PickerPlaceContiNode(3, _targetOutputStageY, _targetPickerX, previousZNearAvoid, currentZNode3));
            nodes.Add(new PickerPlaceContiNode(4, _targetOutputStageY, _targetPickerX, previousAvoid, currentZFinal));
            return nodes;
        }

        private bool CanUseContiSegmentedPlaceFromCurrentPosition(
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis pickerZ,
            BaseAxis previousPickerZ,
            double previousPickerZAvoid,
            PickerPlaceMotionConfig placeConfig,
            out string reason)
        {
            reason = string.Empty;

            if (!HasPendingSynchronizedRetreat())
            {
                reason = "이전 PickerZ 복귀 지연 대상이 없어 ContiNode에 포함할 Z1 축이 없습니다.";
                return false;
            }

            if (!CanUseSynchronizedArrivalFromCurrentPosition(stageY, pickerX, pickerZ, previousPickerZ, previousPickerZAvoid, placeConfig, out reason))
                return false;

            return true;
        }

        private bool CanUseContiSegmentedNodesFromCurrentPosition(
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis previousPickerZ,
            BaseAxis pickerZ,
            IList<PickerPlaceContiNode> nodes,
            PickerPlaceMotionConfig placeConfig,
            out string reason)
        {
            reason = string.Empty;

            if (nodes == null || nodes.Count == 0)
            {
                reason = "ContiNode 노드가 없습니다.";
                return false;
            }

            double maxTravel = placeConfig != null ? placeConfig.MaxSynchronizedTravelDistance : 37.0;
            if (maxTravel <= 0.0)
                maxTravel = 37.0;

            foreach (PickerPlaceContiNode node in nodes)
            {
                if (Math.Abs(node.StageY - stageY.ActualPosition) > maxTravel ||
                    Math.Abs(node.PickerX - pickerX.ActualPosition) > maxTravel ||
                    Math.Abs(node.PreviousPickerZ - previousPickerZ.ActualPosition) > maxTravel ||
                    Math.Abs(node.PickerZ - pickerZ.ActualPosition) > maxTravel)
                {
                    reason = "node" + node.Index + " 목표 위치가 연속 Place ContiNode 허용 거리 밖입니다.";
                    return false;
                }
            }

            return true;
        }

        private static double ResolveNearAvoidPosition(double avoidPosition, double placePosition, double distanceFromAvoid)
        {
            if (distanceFromAvoid <= 0.0)
                return avoidPosition;

            double directionToPlace = placePosition >= avoidPosition ? 1.0 : -1.0;
            return avoidPosition + (directionToPlace * distanceFromAvoid);
        }

        private double ResolveTapeThicknessMm(PickerPlaceMotionConfig placeConfig)
        {
            double fallback = placeConfig != null ? placeConfig.ContiTapeThicknessFallback : 0.10;

            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project != null)
                    return NormalizeThicknessMm(project.TapeThickness, fallback);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode Tape 두께 로드 실패. fallback=" + fallback.ToString("F3") +
                    ", error=" + ex.Message + " - Check");
            }

            return fallback;
        }

        private double ResolveDieThicknessMm(PickerPlaceMotionConfig placeConfig)
        {
            double fallback = placeConfig != null ? placeConfig.ContiDieThicknessFallback : 0.15;

            try
            {
                RecipeProject project = RecipeStore.LoadLastOrDefault();
                if (project != null)
                {
                    if (project.Die != null && project.Die.ThicknessMm > 0.0)
                        return NormalizeThicknessMm(project.Die.ThicknessMm, fallback);

                    return NormalizeThicknessMm(project.ChipThickness, fallback);
                }
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode Die 두께 로드 실패. fallback=" + fallback.ToString("F3") +
                    ", error=" + ex.Message + " - Check");
            }

            return fallback;
        }

        private static double NormalizeThicknessMm(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
                return fallback;

            if (value > 5.0)
                return value / 1000.0;

            return value;
        }

        private async Task<int> WaitSynchronizedArrivalFinalPositionAsync(
            BinStageAxis yAxis,
            PickerAxis? previousPickerZAxis,
            double previousPickerZAvoid,
            PickerAxis pickerZAxis,
            int timeoutMs,
            CancellationToken ct)
        {
            AxisMoveWaitResult stageYWait = await OutputStage.WaitStageAxisMoveDoneInPosition(
                yAxis,
                _targetOutputStageY,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (stageYWait == null || !stageYWait.Success)
            {
                return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-SYNC-STAGE-Y", stageYWait), "OutputStage",
                    "Place 보간 이동 후 OutputStageY 최종 위치 대기 실패. " +
                    FormatAxisMoveWaitResult(stageYWait, OutputStage.BuildStageAxisState(yAxis, _targetOutputStageY)));
            }

            AxisMoveWaitResult pickerXWait = await WaitPickerAxisMoveDoneAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerXWait == null || !pickerXWait.Success)
            {
                return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-SYNC-PICKER-X", pickerXWait), Name,
                    "Place 보간 이동 후 PickerX 최종 위치 대기 실패. " +
                    FormatAxisMoveWaitResult(pickerXWait, BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX)));
            }

            if (previousPickerZAxis.HasValue)
            {
                AxisMoveWaitResult previousPickerZWait = await WaitPickerAxisMoveDoneAsync(
                    previousPickerZAxis.Value,
                    previousPickerZAvoid,
                    timeoutMs,
                    ct).ConfigureAwait(false);
                if (previousPickerZWait == null || !previousPickerZWait.Success)
                {
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-SYNC-PREV-PICKER-Z", previousPickerZWait), Name,
                        "Place 보간 이동 후 이전 PickerZ Avoid 최종 위치 대기 실패. pickerNo=" + _pendingSynchronizedRetreatPickerNo +
                        ". " + FormatAxisMoveWaitResult(previousPickerZWait, BuildPickerAxisState(previousPickerZAxis.Value, previousPickerZAvoid)));
                }
            }

            AxisMoveWaitResult pickerZWait = await WaitPickerAxisMoveDoneAsync(
                pickerZAxis,
                _targetPickerZ,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerZWait == null || !pickerZWait.Success)
            {
                return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-SYNC-PICKER-Z", pickerZWait), Name,
                    "Place 보간 이동 후 PickerZ 최종 위치 대기 실패. pickerNo=" + _currentPickerNo +
                    ". " + FormatAxisMoveWaitResult(pickerZWait, BuildPickerAxisState(pickerZAxis, _targetPickerZ)));
            }

            return 0;
        }

        private bool IsFirstPlaceMoveInBatch()
        {
            return _pickerCursor <= 0;
        }

        private bool CanUseSynchronizedArrivalFromCurrentPosition(
            BaseAxis stageY,
            BaseAxis pickerX,
            BaseAxis pickerZ,
            BaseAxis previousPickerZ,
            double previousPickerZAvoid,
            PickerPlaceMotionConfig placeConfig,
            out string reason)
        {
            reason = "";

            if (stageY == null)
            {
                reason = "OutputStageY 축을 찾을 수 없습니다.";
                return false;
            }

            if (pickerX == null)
            {
                reason = "PickerX 축을 찾을 수 없습니다.";
                return false;
            }

            if (pickerZ == null)
            {
                reason = "PickerZ 축을 찾을 수 없습니다.";
                return false;
            }

            if (HasPendingSynchronizedRetreat() && previousPickerZ == null)
            {
                reason = "이전 PickerZ 복귀 대상 축을 찾을 수 없습니다.";
                return false;
            }

            double maxTravel = placeConfig != null ? placeConfig.MaxSynchronizedTravelDistance : 37.0;
            if (maxTravel <= 0.0)
                maxTravel = 37.0;

            double stageYTravel = Math.Abs(_targetOutputStageY - stageY.ActualPosition);
            double pickerXTravel = Math.Abs(_targetPickerX - pickerX.ActualPosition);
            double pickerZTravel = Math.Abs(_targetPickerZ - pickerZ.ActualPosition);
            double previousPickerZTravel = previousPickerZ != null ? Math.Abs(previousPickerZAvoid - previousPickerZ.ActualPosition) : 0.0;

            if (IsPickerXAxisAtAnyAvoidPosition())
            {
                reason = "PickerX가 Avoid 계열 위치에 있어 Place 첫 접근으로 판단됩니다.";
                return false;
            }

            if (stageYTravel > maxTravel || pickerXTravel > maxTravel || pickerZTravel > maxTravel || previousPickerZTravel > maxTravel)
            {
                reason = "현재 위치가 연속 Place 보간 허용 거리 밖입니다.";
                return false;
            }

            return true;
        }

        private bool IsPickerXAxisAtAnyAvoidPosition()
        {
            return IsPickerAxisAlreadyInPosition(PickerAxis.PickerX, GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition")) ||
                IsPickerAxisAlreadyInPosition(PickerAxis.PickerX, GetPickerTeachingPosition(PickerAxis.PickerX, "InputAvoidPosition")) ||
                IsPickerAxisAlreadyInPosition(PickerAxis.PickerX, GetPickerTeachingPosition(PickerAxis.PickerX, "OutputAvoidPosition"));
        }

        private bool ShouldDelayCurrentPickerZRetreatForNextSynchronizedPlace()
        {
            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            if (placeConfig == null || !IsCoordinatedPlaceMotionMode(placeConfig.MotionMode))
                return false;

            int nextCursor = _pickerCursor + 1;
            if (nextCursor >= _pickedPickerIndexes.Count)
                return false;

            int nextPickerIndex = _pickedPickerIndexes[nextCursor];
            int nextPickerNo = ToPickerNo(nextPickerIndex);
            DieMaterial nextDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, nextPickerNo);
            if (nextDie == null)
                return false;

            BinSide nextSide;
            if (!TryResolveOutputSide(nextDie, out nextSide))
                return false;

            return nextSide == _currentOutputSide;
        }

        private static bool TryResolveOutputSide(DieMaterial die, out BinSide side)
        {
            side = BinSide.Good;
            if (die == null)
                return false;

            if (die.Result == DieResult.Good)
            {
                side = BinSide.Good;
                return true;
            }

            if (die.Result == DieResult.NG)
            {
                side = BinSide.Ng;
                return true;
            }

            return false;
        }

        private bool HasPendingSynchronizedRetreat()
        {
            return _pendingSynchronizedRetreatPickerIndex >= 0;
        }

        private void SetPendingSynchronizedRetreat(int pickerIndex, int pickerNo)
        {
            _pendingSynchronizedRetreatPickerIndex = pickerIndex;
            _pendingSynchronizedRetreatPickerNo = pickerNo;
        }

        private void ClearPendingSynchronizedRetreat()
        {
            _pendingSynchronizedRetreatPickerIndex = -1;
            _pendingSynchronizedRetreatPickerNo = 0;
        }

        private async Task<int> CompletePendingSynchronizedRetreatIfNeededAsync(string description, CancellationToken ct)
        {
            if (!HasPendingSynchronizedRetreat())
                return 0;

            PickerAxis zAxis = GetPickerZAxis(_pendingSynchronizedRetreatPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(
                zAxis,
                avoid,
                description,
                ct,
                "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            ClearPendingSynchronizedRetreat();
            return 0;
        }

        private static string FormatTravel(BaseAxis axis, double target)
        {
            if (axis == null)
                return "axis=null";

            return Math.Abs(target - axis.ActualPosition).ToString("F3");
        }

        private async Task<int> MovePickerYAndTToPlaceBeforeSynchronizedArrivalAsync(CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerY] = _targetPickerY;
            AddLoadedPickerTPlaceTargets(targets);

            return await MovePickerAxesAndVerifyAsync(
                targets,
                "place picker Y/T before synchronized arrival",
                ct,
                BuildPlaceMoveTargetName()).ConfigureAwait(false);
        }

        private PickerPlaceMotionConfig ResolvePlaceMotionConfig()
        {
            PickerPlaceMotionConfig config = null;
            if (Side == PickerSequenceSide.Front && FrontPicker != null && FrontPicker.Config != null)
                config = FrontPicker.Config.Place;
            else if (Side == PickerSequenceSide.Rear && RearPicker != null && RearPicker.Config != null)
                config = RearPicker.Config.Place;

            if (config == null)
                config = new PickerPlaceMotionConfig();

            config.Ensure();
            return config;
        }

        private BaseAxis ResolveOutputStageYAxis(BinStageAxis yAxis)
        {
            if (OutputStage == null)
                return null;

            if (yAxis == BinStageAxis.GoodBinY)
                return OutputStage.GoodStage != null ? OutputStage.GoodStage.StageY : null;

            if (yAxis == BinStageAxis.NgBinY)
                return OutputStage.NgStage != null ? OutputStage.NgStage.StageY : null;

            return null;
        }

        private string BuildPlaceMoveTargetName()
        {
            return AppendAutoProcessCorrectionTargetTag("DiePlacePosition[" + _currentPickerIndex + "];PickerPhase=InspectionZHold;InspectionContinuous;From=Side;To=Place");
        }

        private async Task<int> EnsureOutputStageZReadyForPlaceAsync(CancellationToken ct)
        {
            if (_currentOutputSide != BinSide.Good)
                return 0;

            if (OutputStage == null || OutputStage.Recipe == null)
                return Fail("PICKER-PLACE-GOOD-Z-UNIT", "OutputStage",
                    "Good Stage Place 전 OutputStage 또는 Recipe를 찾을 수 없습니다. die=" +
                    (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo);

            OutputStage.Recipe.EnsurePositionObjects();

            double target = OutputStage.Recipe.GoodStageZ.ProcessPosition;
            int result = await MoveOutputStageAxisAndVerifyAsync(
                BinStageAxis.GoodBinZ,
                target,
                "Good Stage Z place process",
                ct,
                BuildOutputStagePlaceMoveTargetName("GoodZProcess")).ConfigureAwait(false);

            if (result != 0)
                return Fail("PICKER-PLACE-GOOD-Z-PROCESS", "OutputStage",
                    "Good Stage Place 전 Z축 Process 위치 이동 실패. result=" + result +
                    ", target=" + target +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            return 0;
        }

        private void AddLoadedPickerTPlaceTargets(IDictionary<PickerAxis, double> targets)
        {
            foreach (int pickerIndex in _pickedPickerIndexes)
            {
                PickerAxis tAxis = GetPickerTAxis(pickerIndex);
                double target = ResolvePickerZoneT("DiePlacePosition", pickerIndex);

                if (!IsPickerAxisAlreadyInPosition(tAxis, target))
                    targets[tAxis] = target;
            }
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

            if (_pickerZPlacedBySynchronizedArrival)
            {
                PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
                if (!IsPickerAxisInPosition(zAxis, _targetPickerZ))
                {
                    return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                        "Place 보간 이동 후 PickerZ 최종 위치 확인 실패. pickerNo=" + _currentPickerNo +
                        ", " + BuildPickerAxisState(zAxis, _targetPickerZ));
                }

                CurrentStep = PickerPlaceStep.VacuumOff;
                return 0;
            }

            CurrentStep = PickerPlaceStep.MovePickerZPlace;
            return 0;
        }

        private async Task<int> MovePickerZPlaceAsync(CancellationToken ct)
        {
            if (_pickerZPlacedBySynchronizedArrival)
            {
                CurrentStep = PickerPlaceStep.VacuumOff;
                return 0;
            }

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

                if (ShouldDelayCurrentPickerZRetreatForNextSynchronizedPlace())
                {
                    SetPendingSynchronizedRetreat(_currentPickerIndex, _currentPickerNo);
                    WriteLog("PickerPlaceSequence",
                        Name + " 다음 Place 보간 이동에 현재 PickerZ Avoid 복귀를 포함하기 위해 Z 복귀를 지연합니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", pickerIndex=" + _currentPickerIndex +
                        ", cursor=" + _pickerCursor +
                        ", outputSide=" + _currentOutputSide + " - Check");
                    CurrentStep = PickerPlaceStep.VerifyFlowOff;
                    return 0;
                }

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

            ClearPendingSynchronizedRetreat();
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
            {
                Context.Controller.RecordAutoDiePlacedForStats(_currentOutputSide);
                Context.Controller.RecordOutputStageProductReceivedForTact(
                    _currentOutputSide,
                    _currentDie.DieId,
                    _currentPickerNo,
                    _receiveTarget);
            }

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
                if (HasPendingSynchronizedRetreat())
                {
                    CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                    return 0;
                }

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

                int idleTimeoutMs = Options != null && Options.RunMode == SequenceRunMode.Auto
                    ? 0
                    : ResolveTimeout();

                return await Context.OutputPostPlaceInspections.WaitUntilIdleAsync(
                    Name + " Place 진입",
                    idleTimeoutMs,
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

        private string BuildOutputStagePlaceMoveTargetName(string outputStageStep)
        {
            return BuildPlaceMoveTargetName() + ";OutputStageStep=" + outputStageStep;
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct)
        {
            return await MoveOutputStageAxisAndVerifyAsync(axis, target, description, ct, null).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct, string targetName)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                bool logResumePlaceStageMove = description != null &&
                    description.IndexOf("Place 재시작", StringComparison.Ordinal) >= 0;
                if (logResumePlaceStageMove)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " " + description + " 이동 시작. axis=" + axis +
                        ", target=" + target +
                        ", targetName=" + (targetName ?? "-") +
                        ", " + OutputStage.BuildStageAxisState(axis, target) +
                        " - Start");
                }

                int result = await AwaitStepWithCancellationAsync(OutputStage.MoveStageAxis(axis, target, Options.FineMove, targetName), ct).ConfigureAwait(false);
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

                if (logResumePlaceStageMove)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " " + description + " 이동 완료. 이후 Picker X/T 완료 확인 후에만 PickerY 전진을 허용합니다. axis=" + axis +
                        ", target=" + target +
                        ", tolerance=" + tolerance +
                        ", targetName=" + (targetName ?? "-") +
                        ", " + OutputStage.BuildStageAxisState(axis, target) +
                        " - Ok");
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

            return 5; //100
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

