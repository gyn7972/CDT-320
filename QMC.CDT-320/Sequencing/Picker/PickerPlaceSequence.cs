using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Recipes;
using QMC.Common.Motion;
using QMC.Common;
using QMC.CDT320.Motion.SharedRailX;

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
        // Formula from the central place target resolver; logged after XYT/Z final position checks.
        private string _targetFormula = "";
        private double _outputVisionToPickerX;
        private double _outputVisionToPickerY;
        private string _placedDieId = "";
        private BinSide _placedOutputSide;
        private OutputStageReceiveTarget _placedReceiveTarget;
        private SequenceResourceLease _outputPlaceLease;
        private SequenceResourceLease _outputStageLease;
        private SequenceResourceLease _outputFeederLease;
        private bool _outputInspectBatchOpen;
        private bool _pickerZPlacedByContiSegmentedPlace;
        private bool _currentPlaceZSafeReturnCompleted;
        private int _pendingContiRetreatPickerIndex = -1;
        private int _pendingContiRetreatPickerNo;
        private bool _suppressOutputPostPlaceInspection;
        private bool _placeBlowHoldUntilAvoid;
        private bool _placeTargetPrepared;
        private bool _parentOutputWorkZoneReleaseNotified;

        public bool ForceSafeYBeforeFirstPlaceMove { get; set; }
        public bool KeepPickerYForwardDuringPlaceReadyWait { get; set; }
        internal Func<string, bool> ReleaseParentOutputWorkZoneAfterSafeAvoid { get; set; }

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
                TurnPlaceBlowOff("Place 시퀀스 Abort");
                ClearPendingContiRetreat();
                ReleaseOutputPlaceArea();
                ReleaseOutputStageArea();
                ReleaseOutputFeederArea();
                CancelOutputPostPlaceInspectionBatch("Place 시퀀스 Abort");
                CurrentStep = PickerPlaceStep.Complete;
            }
            catch
            {
            }
            finally
            {
            }
        }

        internal async Task<int> RunManualSelectedOutputSlotPlaceAsync(
            BinSide targetSide,
            OutputStageReceiveTarget receiveTarget,
            int pickerNo,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            bool updatedMaterial = false;

            try
            {
                ct.ThrowIfCancellationRequested();
                _suppressOutputPostPlaceInspection = true;

                int result = PrepareManualSelectedOutputPlaceContext(targetSide, receiveTarget, pickerNo, options);
                if (result != 0)
                    return result;

                WriteLog("PickerPlaceManual",
                    Name + " manual selected output slot place start. die=" +
                    (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", order=" + (_receiveTarget != null ? _receiveTarget.OrderIndex.ToString() : "-") +
                    ", map=(" + (_receiveTarget != null ? _receiveTarget.DieMapX.ToString() : "-") +
                    "," + (_receiveTarget != null ? _receiveTarget.DieMapY.ToString() : "-") + ") - Start");

                result = await MoveAllPickerZToAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputStageAvoidPositionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputStageReceivePositionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = VerifyPlaceTarget();
                if (result != 0)
                    return result;

                result = await MovePickerZPlaceAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await VacuumOffAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await BlowOffAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerZToAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = UpdateMaterialToOutputStage(ct);
                if (result != 0)
                    return result;
                updatedMaterial = true;

                result = await RecoverOutputStageAfterPlaceAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MovePickerToAvoidAfterPlaceAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPlaceManual",
                    Name + " manual selected output slot place complete. die=" +
                    (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", order=" + (_receiveTarget != null ? _receiveTarget.OrderIndex.ToString() : "-") +
                    " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-MANUAL-EX", Name,
                    "Manual selected output slot place failed. pickerNo=" + pickerNo +
                    ", outputSide=" + targetSide +
                    ", error=" + ex.Message);
            }
            finally
            {
                if (!updatedMaterial)
                    CancelOutputPostPlaceInspectionBatch("Manual selected output slot place aborted before material update.");

                ReleaseOutputPlaceArea();
                ReleaseOutputStageArea();
                ReleaseOutputFeederArea();
                _suppressOutputPostPlaceInspection = false;
            }
        }

        internal async Task<int> RunManualSelectedOutputSlotPlaceStepAsync(
            BinSide targetSide,
            OutputStageReceiveTarget receiveTarget,
            int pickerNo,
            PickerPlaceManualStep step,
            CancellationToken ct,
            PickerSequenceOptions options)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                _suppressOutputPostPlaceInspection = true;

                int result = PrepareManualSelectedOutputPlaceContext(targetSide, receiveTarget, pickerNo, options);
                if (result != 0)
                    return result;

                switch (step)
                {
                    case PickerPlaceManualStep.PreparePlaceTarget:
                        WriteLog("PickerPlaceManual",
                            Name + " manual place target prepared. die=" +
                            (_currentDie != null ? _currentDie.DieId : "-") +
                            ", pickerNo=" + _currentPickerNo +
                            ", outputSide=" + _currentOutputSide +
                            ", formula=" + (_targetFormula ?? "") + " - Ok");
                        return 0;

                    case PickerPlaceManualStep.MoveStagePickerToPlace:
                        result = await MoveAllPickerZToAvoidAsync(ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                        result = await MoveOutputStageAvoidPositionAsync(ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                        return await MoveOutputStageReceivePositionAsync(ct).ConfigureAwait(false);

                    case PickerPlaceManualStep.VerifyPlaceTarget:
                        return VerifyPlaceTarget();

                    case PickerPlaceManualStep.MovePickerZPlace:
                        result = VerifyPlaceTarget();
                        if (result != 0)
                            return result;
                        return await MovePickerZPlaceAsync(ct).ConfigureAwait(false);

                    case PickerPlaceManualStep.VacuumOffBlow:
                        result = await VacuumOffAsync(ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                        return await BlowOffAsync(ct).ConfigureAwait(false);

                    case PickerPlaceManualStep.MovePickerZToAvoid:
                        return await MovePickerZToAvoidAsync(ct).ConfigureAwait(false);

                    case PickerPlaceManualStep.UpdateMaterialToOutputStage:
                        return UpdateMaterialToOutputStage(ct);

                    case PickerPlaceManualStep.RecoverAfterPlace:
                        result = await RecoverOutputStageAfterPlaceAsync(ct).ConfigureAwait(false);
                        if (result != 0)
                            return result;
                        return await MovePickerToAvoidAfterPlaceAsync(ct).ConfigureAwait(false);

                    default:
                        return Fail("PICKER-PLACE-MANUAL-STEP", Name,
                            "Unsupported manual place step. step=" + step);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-MANUAL-STEP-EX", Name,
                    "Manual place step failed. step=" + step +
                    ", pickerNo=" + pickerNo +
                    ", outputSide=" + targetSide +
                    ", error=" + ex.Message);
            }
            finally
            {
                ReleaseOutputPlaceArea();
                ReleaseOutputStageArea();
                ReleaseOutputFeederArea();
                _suppressOutputPostPlaceInspection = false;
            }
        }

        private int PrepareManualSelectedOutputPlaceContext(
            BinSide targetSide,
            OutputStageReceiveTarget receiveTarget,
            int pickerNo,
            PickerSequenceOptions options)
        {
            SetOptionsForManualOperation(options);
            ClearPendingContiRetreat();
            _pickedPickerIndexes.Clear();
            _pickerCursor = 0;
            _currentPickerIndex = -1;
            _currentPickerNo = 0;
            _currentDie = null;
            _receiveTarget = null;
            _placedDieId = "";
            _placedReceiveTarget = null;
            _pickerZPlacedByContiSegmentedPlace = false;
            _currentPlaceZSafeReturnCompleted = false;
            _placeBlowHoldUntilAvoid = false;

            if (OutputStage == null)
                return Fail("PICKER-PLACE-OUTPUT-STAGE-MISSING", "OutputStage", "OutputStageUnit is null.");

            string axisReason = BuildRequiredPickerAxesReason();
            if (!string.IsNullOrWhiteSpace(axisReason))
                return Fail("PICKER-PLACE-AXIS-NOT-READY", Name,
                    "Picker axis is not ready. side=" + Side + ", reason=" + axisReason);

            int normalizedPickerNo = pickerNo;
            if (normalizedPickerNo < 1)
                normalizedPickerNo = 1;
            if (normalizedPickerNo > 4)
                normalizedPickerNo = 4;

            _currentPickerNo = normalizedPickerNo;
            _currentPickerIndex = normalizedPickerNo - 1;
            _pickedPickerIndexes.Add(_currentPickerIndex);
            _currentDie = MaterialStateService.GetDieAtPicker(PickerLocationKind, _currentPickerNo);
            if (_currentDie == null)
            {
                return Fail("PICKER-PLACE-MANUAL-NO-DIE", "Material",
                    "Manual Place 대상 Picker에 Die가 없습니다. side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + targetSide);
            }

            if (receiveTarget == null)
            {
                return Fail("PICKER-PLACE-MANUAL-TARGET", "Material",
                    "Manual Place 대상 Output slot 정보가 없습니다. side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + targetSide);
            }

            _currentOutputSide = targetSide;
            _receiveTarget = CloneReceiveTarget(receiveTarget, targetSide);

            string offsetReason;
            if (!TryResolveOutputVisionToPickerOffsets(
                _currentOutputSide,
                _currentPickerIndex,
                out _outputVisionToPickerX,
                out _outputVisionToPickerY,
                out offsetReason))
            {
                return Fail("PICKER-PLACE-MANUAL-OFFSET", Name,
                    "Manual Place OutputVision 기준 Picker 좌표 보정값 계산 실패. side=" + Side +
                    ", outputSide=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo +
                    ", reason=" + offsetReason);
            }

            return CalculatePlaceTargetValues();
        }

        private static OutputStageReceiveTarget CloneReceiveTarget(OutputStageReceiveTarget source, BinSide side)
        {
            if (source == null)
                return null;

            return new OutputStageReceiveTarget
            {
                StageLocation = side == BinSide.Ng ? MaterialLocationKind.OutputStageNg : MaterialLocationKind.OutputStageGood,
                OutputWaferId = source.OutputWaferId ?? "",
                SourceWaferId = source.SourceWaferId ?? "",
                OrderIndex = source.OrderIndex,
                DieMapX = source.DieMapX,
                DieMapY = source.DieMapY,
                OffsetX = source.OffsetX,
                OffsetY = source.OffsetY,
                TargetX = source.TargetX,
                TargetY = source.TargetY
            };
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
                    if (CurrentStep == PickerPlaceStep.Complete)
                        EndOutputPostPlaceInspectionBatch();
                    else
                        CancelOutputPostPlaceInspectionBatch("Place 시퀀스가 완료되기 전에 종료됨. step=" + CurrentStep);
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

                case PickerPlaceStep.VerifyPickedPickerFlow:
                    Log.Write("PickerPlaceSequence", Name + " Place 시작 전 제품 흡착 Flow 확인 시작. side=" + Side + ", step=" + CurrentStep);
                    return VerifyPickedPickerFlowBeforePlaceAsync(ct);

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
            {
                return Fail("PICKER-PLACE-OUTPUT-STAGE-MISSING", "OutputStage", "OutputStageUnit is null.");
            }

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
            CurrentStep = PickerPlaceStep.VerifyPickedPickerFlow;
            return 0;
        }

        private async Task<int> VerifyPickedPickerFlowBeforePlaceAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (IsPlaceProductCheckBypassed())
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Place 시작 전 제품 흡착 Flow 확인은 Simulation/DryRun 조건으로 통과합니다. " +
                        "side=" + Side +
                        ", targetCount=" + _pickedPickerIndexes.Count + " - Bypass");
                    CurrentStep = PickerPlaceStep.MoveAllPickerZToAvoid;
                    return 0;
                }

                for (int i = 0; i < _pickedPickerIndexes.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    int pickerIndex = _pickedPickerIndexes[i];
                    int pickerNo = ToPickerNo(pickerIndex);
                    DieMaterial die = MaterialStateService.GetDieAtPicker(PickerLocationKind, pickerNo);
                    if (die == null)
                        continue;

                    int timeoutMs = ResolvePickerIoTimeoutMs(pickerNo);
                    DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
                    bool flowOn = ReadPickerFlowState(pickerNo);
                    while (!flowOn && DateTime.Now <= deadline)
                    {
                        ct.ThrowIfCancellationRequested();
                        await Task.Delay(1, ct).ConfigureAwait(false);
                        flowOn = ReadPickerFlowState(pickerNo);
                    }

                    if (!flowOn)
                    {
                        return Fail("PICKER-PLACE-FLOW-NOT-DETECTED", Name,
                            "Place 시작 전 제품 흡착 Flow 확인 실패. " +
                            "데이터상 Picker에 Die가 있지만 실제 Flow 신호가 ON이 아닙니다. " +
                            "side=" + Side +
                            ", pickerNo=" + pickerNo +
                            ", die=" + die.DieId +
                            ", timeoutMs=" + timeoutMs +
                            ", expectedFlow=ON, actualFlow=OFF");
                    }

                    WriteLog("PickerPlaceSequence",
                        Name + " Place 시작 전 제품 흡착 Flow 확인 완료. " +
                        "side=" + Side +
                        ", pickerNo=" + pickerNo +
                        ", die=" + die.DieId +
                        ", flow=ON - Ok");
                }

                CurrentStep = PickerPlaceStep.MoveAllPickerZToAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-FLOW-CHECK-EX", Name,
                    "Place 시작 전 제품 흡착 Flow 확인 중 예외가 발생했습니다. side=" + Side +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsPlaceProductCheckBypassed()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings != null && (settings.BypassHardware || settings.SimulationMode || settings.DryRunMode))
                    return true;

                return Context != null && Context.Controller != null && Context.Controller.GlobalDryRun;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
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
            _pickerZPlacedByContiSegmentedPlace = false;
            _currentPlaceZSafeReturnCompleted = false;
            _placeBlowHoldUntilAvoid = false;

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
                    "Place 전 Bottom/Side 검사 흐름이 완료되지 않았습니다. 검사 NG는 정지 조건이 아니며 현재 정책상 모든 검사를 완료한 뒤 Good Stage 순번으로 Place해야 합니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", bottomDone=" + HasInspectionResult(_currentDie, "Bottom") +
                    ", side0Done=" + HasInspectionResult(_currentDie, "Side0") +
                    ", side90Done=" + HasInspectionResult(_currentDie, "Side90"));
            }

            if (_currentDie.Result == DieResult.Good || _currentDie.Result == DieResult.NG)
            {
                _currentOutputSide = BinSide.Good;
                if (_currentDie.Result == DieResult.NG)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Die 결과가 NG이지만 현재 운전 정책에 따라 Good Stage 순번으로 Place합니다. " +
                        "나중에 NG Stage 배출로 복구하려면 이 분기에서 NG 결과를 BinSide.Ng로 되돌리면 됩니다. " +
                        "die=" + _currentDie.DieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", forcedOutputSide=" + _currentOutputSide + " - Check");
                }
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
                bool fullAvoidPrepared = false;
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
                    bool stageReceiveComplete = MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide);

                    if (materialReady && (!autoMode || signalReady))
                    {
                        BeginOutputPostPlaceInspectionBatch();
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

                    if (stageReceiveComplete && !fullAvoidPrepared)
                    {
                        int fullAvoidResult = await MovePickerToAvoidAfterPlaceFastAsync(
                            "OutputStage 수령 완료 대기 중 보유 Die Picker 전체 Avoid",
                            ct).ConfigureAwait(false);
                        if (fullAvoidResult != 0)
                            return fullAvoidResult;

                        ClearPendingContiRetreat();
                        ForceSafeYBeforeFirstPlaceMove = true;
                        KeepPickerYForwardDuringPlaceReadyWait = false;
                        ReleaseOutputPlaceArea();
                        ReleaseOutputStageArea();
                        ReleaseOutputFeederArea();
                        EndOutputPostPlaceInspectionBatch();
                        safeWaitPositionPrepared = true;
                        fullAvoidPrepared = true;
                        WriteLog("PickerPlaceSequence",
                            Name + " OutputStage가 완료되어 다음 Place 대상이 열릴 때까지 보유 Die 상태로 Picker 전체 Avoid 복귀를 완료했습니다. " +
                            "side=" + Side + ", outputSide=" + _currentOutputSide +
                            ", pickerNo=" + _currentPickerNo + " - Ok");
                    }
                    else if (!safeWaitPositionPrepared)
                    {
                        if (KeepPickerYForwardDuringPlaceReadyWait && !ForceSafeYBeforeFirstPlaceMove)
                        {
                            WriteLog("PickerPlaceSequence",
                                Name + " 정상 연속 Place 대기: Side 검사 종료 위치에서 PickerY Avoid 복귀를 생략합니다. " +
                                "OutputStage 준비가 열리면 현재 PickerY 위치에서 Place 목표 Y로 직접 이동합니다. " +
                                "side=" + Side +
                                ", outputSide=" + _currentOutputSide +
                                ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                                ", pickerNo=" + _currentPickerNo + " - Check");
                        }
                        else
                        {
                            int waitSafeResult = await MovePickerToSafeYBeforeOutputStageReadyWaitAsync(ct).ConfigureAwait(false);
                            if (waitSafeResult != 0)
                                return waitSafeResult;
                        }

                        safeWaitPositionPrepared = true;
                    }

                    WriteLog("PickerPlaceSequence", Name + " Place 대기: " + detail + " - Wait");
                    Context.StopIfCycleStopRequested(
                        "PickerPlaceSequence.WaitOutputStageReady",
                        ShouldDeferCycleStopForPickerDrain(),
                        "Picker Place drain");
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

            _placeTargetPrepared = false;

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

            bool outputFeederAlreadySafe = OutputFeeder != null &&
                                           OutputFeeder.IsFeederUnclamped() &&
                                           OutputFeeder.IsBinFeederYInAvoidPosition();
            if (!outputFeederAlreadySafe)
            {
                result = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveVisionXToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                    ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail("PICKER-PLACE-VISION-X-FEEDER-SAFE", "OutputStage",
                        "OutputFeeder 안전 위치 확보 전 OutputVisionX 전체 Avoid 이동 실패. " +
                        "side=" + _currentOutputSide + ", result=" + result + ", " +
                        OutputStage.DescribeStageLoadMoveState(_currentOutputSide));
                }

                result = await EnsureOutputFeederSafeBeforePlaceStageMoveAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                WriteLog("PickerPlaceSequence",
                    Name + " OutputFeeder가 안전 위치가 아니어서 OutputVisionX 전체 Avoid 후 Feeder를 먼저 정리했습니다. " +
                    "side=" + _currentOutputSide + ", die=" + (_currentDie != null ? _currentDie.DieId : "-") + " - Ok");
            }

            result = PreparePlaceTargetValues();
            if (result != 0)
                return result;

            double fullAvoid = OutputStage.Recipe.VisionX.AvoidPosition;
            double visionTarget = fullAvoid;
            string retreatDetail = "전체 Avoid 사용";
            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                Context != null ? Context.Machine : null);
            if (service != null)
            {
                var planned = new Dictionary<SharedRailXAxis, IList<double>>();
                SharedRailXAxis pickerRailAxis = Side == PickerSequenceSide.Front
                    ? SharedRailXAxis.FrontPickerX
                    : SharedRailXAxis.RearPickerX;
                planned[pickerRailAxis] = new List<double> { _targetPickerX };

                double dynamicTarget;
                string dynamicDetail;
                if (service.TryResolveNearestVisionRetreatTarget(
                    OutputStage.OutputCameraX,
                    fullAvoid,
                    -0.1,
                    planned,
                    1.0,
                    out dynamicTarget,
                    out dynamicDetail))
                {
                    visionTarget = dynamicTarget;
                    retreatDetail = dynamicDetail;
                }
                else
                {
                    retreatDetail = dynamicDetail + " 전체 Avoid로 대체합니다.";
                }
            }

            WriteLog("PickerPlaceSequence",
                Name + " OutputVisionX 피커 진입 회피 좌표를 확정했습니다. " +
                "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", pickerX=" + _targetPickerX.ToString("F6") +
                ", target=" + visionTarget.ToString("F6") +
                ", fullAvoid=" + fullAvoid.ToString("F6") +
                ", detail=" + retreatDetail + " - Check");

            result = await MoveOutputStageAxisAndVerifyAsync(
                BinStageAxis.VisionX,
                visionTarget,
                "OutputVisionX 최소 회피 위치 이동",
                ct).ConfigureAwait(false);

            if (result != 0)
                return Fail("PICKER-PLACE-VISION-X-AVOID", "OutputStage",
                    "OutputVisionX 피커 진입용 최소 회피 이동 실패. side=" + _currentOutputSide +
                    ", target=" + visionTarget.ToString("F6") +
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

            int prepareResult = PreparePlaceTargetValues();
            if (prepareResult != 0)
                return prepareResult;
            Log.Write("PickerPlaceSequence", Name + " Place 대상 좌표 계산 완료. side=" + Side + ", step=" + CurrentStep);

            int result = await MoveOutputStageYPickerXAndPickerZToPlaceByModeAsync(yAxis, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (ForceSafeYBeforeFirstPlaceMove)
                ForceSafeYBeforeFirstPlaceMove = false;

            Log.Write("PickerPlaceSequence", Name + " Place 대상 좌표 이동 완료. side=" + Side + ", step=" + CurrentStep);

            if (!_pickerZPlacedByContiSegmentedPlace)
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
            int result = PreparePlaceTargetValues();
            if (result != 0)
                return result;

            CurrentStep = PickerPlaceStep.MovePickerXYAndTToPlace;
            return 0;
        }

        private int PreparePlaceTargetValues()
        {
            if (_placeTargetPrepared)
                return 0;

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
                    ", reason=" + offsetReason);
            }

            int calculateResult = CalculatePlaceTargetValues();
            if (calculateResult != 0)
                return calculateResult;

            _placeTargetPrepared = true;
            return 0;
        }

        private int CalculatePlaceTargetValues()
        {
            string dieId = _currentDie != null ? _currentDie.DieId : string.Empty;
            VisionOffset bottomOffset;
            string bottomOffsetReason;
            if (!TryResolveBottomPlaceOffset(_currentDie, out bottomOffset, out bottomOffsetReason))
            {
                return Fail("PICKER-PLACE-BOTTOM-OFFSET", "Material",
                    "Place 좌표에 적용할 Bottom 검사 보정값을 찾을 수 없습니다. " +
                    "die=" + dieId +
                    ", pickerNo=" + _currentPickerNo +
                    ", reason=" + bottomOffsetReason);
            }

            double outputStageBaseY = _currentOutputSide == BinSide.Ng
                ? OutputStage.Recipe.NGStageY.ProcessPosition
                : OutputStage.Recipe.GoodStageY.ProcessPosition;

            PlaceCoordinateResult coordinate = PickerMotionTargetResolver.CalculateOutputPlaceTarget(
                Context != null ? Context.Machine : null,
                Side,
                _currentPickerIndex,
                Name,
                dieId,
                _currentOutputSide,
                outputStageBaseY,
                _receiveTarget != null ? _receiveTarget.TargetX : 0.0,
                _receiveTarget != null ? _receiveTarget.TargetY : 0.0,
                OutputStage.Recipe.VisionX.ProcessPosition,
                _outputVisionToPickerX,
                _outputVisionToPickerY,
                bottomOffset.X,
                bottomOffset.Y,
                bottomOffset.R);

            _targetOutputStageY = coordinate.OutputStageY;
            _targetPickerX = coordinate.PickerX;
            _targetPickerY = coordinate.PickerY;
            _targetPickerT = coordinate.PickerT;
            double placeZOverDrive = ResolvePlaceZOverDrive();
            _targetPickerZ = coordinate.PickerZ + placeZOverDrive;
            _targetFormula = coordinate.Formula +
                " / pickerZFinal = pickerZTeaching(" + coordinate.PickerZ.ToString("F6") +
                ") + placeZOverDrive(" + placeZOverDrive.ToString("F6") +
                ") = " + _targetPickerZ.ToString("F6");

            WriteLog("PickerPlaceSequence",
                Name + " calculated place target. die=" + dieId +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", outputStageY=" + _targetOutputStageY +
                ", pickerX=" + _targetPickerX +
                ", pickerY=" + _targetPickerY +
                ", pickerT=" + _targetPickerT +
                ", pickerZ=" + _targetPickerZ +
                ", pickerZTeaching=" + coordinate.PickerZ +
                ", placeZOverDrive=" + placeZOverDrive +
                ", outputStageBaseY=" + outputStageBaseY +
                ", receiveTargetX=" + (_receiveTarget != null ? _receiveTarget.TargetX.ToString() : "-") +
                ", receiveTargetY=" + (_receiveTarget != null ? _receiveTarget.TargetY.ToString() : "-") +
                ", outputVisionToPickerOffsetX=" + _outputVisionToPickerX +
                ", outputVisionToPickerOffsetY=" + _outputVisionToPickerY +
                ", bottomOffsetX=" + bottomOffset.X +
                ", bottomOffsetY=" + bottomOffset.Y +
                ", bottomOffsetT=" + bottomOffset.R +
                ", formula=" + _targetFormula + " - Ok");
            return 0;
        }

        private static bool TryResolveBottomPlaceOffset(
            DieMaterial die,
            out VisionOffset offset,
            out string reason)
        {
            offset = null;
            reason = string.Empty;
            try
            {
                if (die == null)
                {
                    reason = "Die Material 데이터가 없습니다.";
                    return false;
                }

                if (die.Inspections == null || die.Inspections.Count == 0)
                {
                    reason = "검사 기록이 없습니다.";
                    return false;
                }

                DieInspectionRecord latest = null;
                for (int i = 0; i < die.Inspections.Count; i++)
                {
                    DieInspectionRecord record = die.Inspections[i];
                    if (record == null ||
                        !string.Equals(record.InspectionType, "Bottom", StringComparison.OrdinalIgnoreCase) ||
                        record.Result == MaterialInspectionResult.Unknown ||
                        record.Offset == null ||
                        !record.Offset.IsValid)
                    {
                        continue;
                    }

                    if (latest == null || record.UpdatedAt >= latest.UpdatedAt)
                        latest = record;
                }

                if (latest == null)
                {
                    reason = "유효한 Bottom 검사 Offset 기록이 없습니다.";
                    return false;
                }

                if (double.IsNaN(latest.Offset.X) || double.IsInfinity(latest.Offset.X) ||
                    double.IsNaN(latest.Offset.Y) || double.IsInfinity(latest.Offset.Y) ||
                    double.IsNaN(latest.Offset.R) || double.IsInfinity(latest.Offset.R))
                {
                    reason = "Bottom 검사 Offset에 사용할 수 없는 숫자가 포함되어 있습니다.";
                    return false;
                }

                offset = new VisionOffset
                {
                    X = latest.Offset.X,
                    Y = latest.Offset.Y,
                    R = latest.Offset.R,
                    IsValid = true
                };
                return true;
            }
            catch (Exception ex)
            {
                reason = "Bottom 검사 Offset 확인 실패: " + ex.Message;
                return false;
            }
            finally
            {
            }
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
            //    BuildPickerTargetName("DiePlacePosition", _currentPickerIndex));

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
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place 재시작 Picker X/T 목표 이동 완료 후 PickerY가 이미 Place 위치임을 확인했습니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", targetY=" + _targetPickerY + " - Check");
                return 0;
            }

            WriteLog("PickerPlaceSequence",
                Name + " Place 재시작 Picker X/T 목표 이동 완료 후 PickerY 전진을 시작합니다. " +
                "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", targetY=" + _targetPickerY + " - Check");

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                _targetPickerY,
                "Place 재시작 PickerY 전진",
                ct,
                BuildPlaceMoveTargetName()).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageYPickerXAndPickerZToPlaceByModeAsync(BinStageAxis yAxis, CancellationToken ct)
        {
            _pickerZPlacedByContiSegmentedPlace = false;

            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            if (ForceSafeYBeforeFirstPlaceMove)
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place 재시작 안전 진입으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                int safeYResult = await MovePickerToSafeYBeforeOutputStageReadyWaitAsync(ct).ConfigureAwait(false);
                if (safeYResult != 0)
                    return safeYResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place 재시작 첫 접근은 PickerY Avoid 상태에서 ContiNode/선행 Y 전진을 사용하지 않고 X/T 이동 후 Y 전진 순서로 진행합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXTThenYToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            if (placeConfig == null || !IsCoordinatedPlaceMotionMode(placeConfig.MotionMode))
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place ContiNode 모드가 아니어서 이전 PickerZ를 먼저 Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            if (IsFirstPlaceMoveInBatch())
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place 첫 번째 접근 전 이전 PickerZ 안전 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place 첫 번째 접근 이동은 ContiNode를 사용하지 않고 기존 이동 방식으로 진행합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", cursor=" + _pickerCursor +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            BaseAxis stageY = ResolveOutputStageYAxis(yAxis);
            BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
            BaseAxis pickerZ = GetPickerAxis(GetPickerZAxis(_currentPickerIndex));
            BaseAxis previousPickerZ = HasPendingContiRetreat()
                ? GetPickerAxis(GetPickerZAxis(_pendingContiRetreatPickerIndex))
                : null;
            double previousPickerZAvoid = HasPendingContiRetreat()
                ? GetPickerTeachingPosition(GetPickerZAxis(_pendingContiRetreatPickerIndex), "AvoidPosition")
                : 0.0;

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

        private static bool IsCoordinatedPlaceMotionMode(PickerPlaceMotionMode mode)
        {
            return mode == PickerPlaceMotionMode.ContiSegmentedPlace;
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
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place ContiNode 조건 불만족으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 이동 조건 불만족으로 기존 이동 방식으로 접근합니다. " +
                    "reason=" + guardReason +
                    ", pickerNo=" + _currentPickerNo +
                    ", cursor=" + _pickerCursor +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide +
                    ", maxTravel=" + placeConfig.ContiMaxTravelDistance.ToString("F3") +
                    ", stageYTravel=" + FormatTravel(stageY, _targetOutputStageY) +
                    ", pickerXTravel=" + FormatTravel(pickerX, _targetPickerX) +
                    ", previousPickerZTravel=" + FormatTravel(previousPickerZ, previousPickerZAvoid) +
                    " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            IList<PickerPlaceContiNode> nodes = BuildContiSegmentedPlaceNodes(stageY, pickerX, previousPickerZ, pickerZ, placeConfig);
            if (nodes == null || nodes.Count < 2)
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place ContiNode 노드 생성 실패로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 노드 생성 실패로 기존 이동 방식으로 접근합니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            if (!CanUseContiSegmentedNodesFromCurrentPosition(stageY, pickerX, previousPickerZ, pickerZ, nodes, placeConfig, out guardReason))
            {
                int pendingResult = await CompletePendingContiRetreatIfNeededAsync("Place ContiNode 노드 거리 조건 불만족으로 기존 이동 전 이전 PickerZ Avoid 복귀", ct).ConfigureAwait(false);
                if (pendingResult != 0)
                    return pendingResult;

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode 노드 거리 조건 불만족으로 기존 이동 방식으로 접근합니다. " +
                    "reason=" + guardReason +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", outputSide=" + _currentOutputSide + " - Check");
                return await MoveOutputStageYAndPickerXYTToPlaceAsync(yAxis, ct).ConfigureAwait(false);
            }

            int preMove = await MovePickerYAndTToPlaceBeforeContiSegmentedPlaceAsync(ct).ConfigureAwait(false);
            if (preMove != 0)
                return preMove;

            int zReady = await EnsureOutputStageZReadyForPlaceAsync(ct).ConfigureAwait(false);
            if (zReady != 0)
                return zReady;

            double prePlacePickerZ = nodes[nodes.Count - 2].PickerZ;
            double finalPickerZ = nodes[nodes.Count - 1].PickerZ;
            double originalPickerZTarget = _targetPickerZ;
            _targetPickerZ = prePlacePickerZ;

            int previousRetreatResult = await CompletePendingContiRetreatIfNeededAsync(
                "Place 비동기 접근 전 이전 PickerZ Avoid 복귀",
                ct).ConfigureAwait(false);
            if (previousRetreatResult != 0)
            {
                _targetPickerZ = originalPickerZTarget;
                return previousRetreatResult;
            }

            double stageYStart = stageY != null ? stageY.ActualPosition : _targetOutputStageY;
            double pickerXStart = pickerX != null ? pickerX.ActualPosition : _targetPickerX;
            PickerAxis currentPickerZAxis = GetPickerZAxis(_currentPickerIndex);
            string placeTargetName = BuildPlaceMoveTargetName();

            Task<int> stageYMove = MoveOutputStageAxisAndVerifyAsync(
                yAxis,
                _targetOutputStageY,
                "Place ContiNode OutputStageY 비동기 이동",
                ct,
                BuildOutputStagePlaceMoveTargetName("AsyncReceiveY"));
            Task<int> pickerXMove = MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                "Place ContiNode PickerX 비동기 이동",
                ct,
                placeTargetName);
            Task<int> pickerZPrePlaceMove = MovePickerZPlaceAfterContiProgressAsync(
                yAxis,
                stageY,
                pickerX,
                stageYStart,
                pickerXStart,
                stageYMove,
                pickerXMove,
                currentPickerZAxis,
                prePlacePickerZ,
                placeConfig,
                ct);

            int[] moveResults = await Task.WhenAll(stageYMove, pickerXMove, pickerZPrePlaceMove).ConfigureAwait(false);
            if (moveResults[0] != 0 || moveResults[1] != 0 || moveResults[2] != 0)
            {
                _pickerZPlacedByContiSegmentedPlace = false;
                return Fail("PICKER-PLACE-CONTI-ASYNC-MOVE", Name,
                    "Place ContiNode 비동기 PrePlace 이동 실패. " +
                    "stageYResult=" + moveResults[0] +
                    ", pickerXResult=" + moveResults[1] +
                    ", pickerZPrePlaceResult=" + moveResults[2] +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide);
            }

            int finalWait = await WaitContiSegmentedPlaceFinalPositionAsync(
                yAxis,
                null,
                previousPickerZAvoid,
                currentPickerZAxis,
                Math.Max(placeConfig.ContiTimeoutMs, ResolveTimeout()),
                ct).ConfigureAwait(false);
            if (finalWait != 0)
                return finalWait;

            // StageY와 PickerX 최종 도착 확인 후에만 PickerZ를 최종 Place 접촉 위치로 이동합니다.
            _targetPickerZ = finalPickerZ;
            int finalPlaceZResult = await MovePickerAxisAndVerifyAsync(
                currentPickerZAxis,
                finalPickerZ,
                "Place ContiNode PickerZ PrePlace 후 최종 Place 하강",
                ct,
                BuildPickerTargetName("DiePlacePosition", _currentPickerIndex),
                skipFinalPositionCheck: true).ConfigureAwait(false);
            if (finalPlaceZResult != 0)
            {
                _pickerZPlacedByContiSegmentedPlace = false;
                return finalPlaceZResult;
            }

            WriteLog("PickerPlaceSequence",
                Name + " Place ContiNode 비동기 이동 완료. die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", basePickerZ=" + originalPickerZTarget.ToString("F3") +
                ", prePlacePickerZ=" + prePlacePickerZ.ToString("F3") +
                ", finalPickerZ=" + finalPickerZ.ToString("F3") +
                ", stageYStart=" + stageYStart.ToString("F3") +
                ", stageYTarget=" + _targetOutputStageY.ToString("F3") +
                ", pickerXStart=" + pickerXStart.ToString("F3") +
                ", pickerXTarget=" + _targetPickerX.ToString("F3") +
                ", triggerRatio=" + placeConfig.ContiXYMidRatio.ToString("F3") +
                " - Ok");
            _pickerZPlacedByContiSegmentedPlace = true;
            return 0;
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

            PickerAxis previousZAxis = GetPickerZAxis(_pendingContiRetreatPickerIndex);
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
            double currentZFinal = currentPlaceBase - placeConfig.ContiOverDrive;

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

            if (!HasPendingContiRetreat())
            {
                reason = "이전 PickerZ 복귀 지연 대상이 없어 ContiNode에 포함할 Z1 축이 없습니다.";
                return false;
            }

            if (!CanUseContiSegmentedPlaceBaseFromCurrentPosition(stageY, pickerX, pickerZ, previousPickerZ, previousPickerZAvoid, placeConfig, out reason))
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

            double maxTravel = placeConfig != null ? placeConfig.ContiMaxTravelDistance : 45.0;
            if (maxTravel <= 0.0)
                maxTravel = 45.0;

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

        private async Task<int> WaitContiSegmentedPlaceFinalPositionAsync(
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
                return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-CONTI-STAGE-Y", stageYWait), "OutputStage",
                    "Place ContiNode 이동 후 OutputStageY 최종 위치 대기 실패. " +
                    FormatAxisMoveWaitResult(stageYWait, OutputStage.BuildStageAxisState(yAxis, _targetOutputStageY)));
            }

            AxisMoveWaitResult pickerXWait = await WaitPickerAxisMoveDoneAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerXWait == null || !pickerXWait.Success)
            {
                return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-CONTI-PICKER-X", pickerXWait), Name,
                    "Place ContiNode 이동 후 PickerX 최종 위치 대기 실패. " +
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
                    return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-CONTI-PREV-PICKER-Z", previousPickerZWait), Name,
                        "Place ContiNode 이동 후 이전 PickerZ Avoid 최종 위치 대기 실패. pickerNo=" + _pendingContiRetreatPickerNo +
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
                return Fail(ResolveAxisMoveWaitAlarmCode("PICKER-PLACE-CONTI-PICKER-Z", pickerZWait), Name,
                    "Place ContiNode 이동 후 PickerZ 최종 위치 대기 실패. pickerNo=" + _currentPickerNo +
                    ". " + FormatAxisMoveWaitResult(pickerZWait, BuildPickerAxisState(pickerZAxis, _targetPickerZ)));
            }

            return 0;
        }

        private bool IsFirstPlaceMoveInBatch()
        {
            return _pickerCursor <= 0;
        }

        private bool CanUseContiSegmentedPlaceBaseFromCurrentPosition(
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

            if (HasPendingContiRetreat() && previousPickerZ == null)
            {
                reason = "이전 PickerZ 복귀 대상 축을 찾을 수 없습니다.";
                return false;
            }

            double maxTravel = placeConfig != null ? placeConfig.ContiMaxTravelDistance : 45.0;
            if (maxTravel <= 0.0)
                maxTravel = 45.0;

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
                reason = "현재 위치가 연속 Place ContiNode 허용 거리 밖입니다.";
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

        private bool ShouldDelayCurrentPickerZRetreatForNextContiPlace()
        {
            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            if (placeConfig == null || !IsCoordinatedPlaceMotionMode(placeConfig.MotionMode))
                return false;

            if (WillCurrentPlaceCompleteOutputStage())
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 마지막 Place이므로 PickerZ Avoid 복귀를 다음 Conti Place로 지연하지 않습니다. " +
                    "side=" + Side + ", outputSide=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo +
                    ", order=" + (_receiveTarget != null ? _receiveTarget.OrderIndex.ToString() : "-") + " - Check");
                return false;
            }

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

        private bool WillCurrentPlaceCompleteOutputStage()
        {
            try
            {
                if (_receiveTarget == null || _receiveTarget.OrderIndex < 0)
                    return false;

                WaferMaterial outputWafer = MaterialStateService.GetWaferAtLocation(_receiveTarget.StageLocation);
                if (outputWafer == null || outputWafer.OutputReceiveTotalCount <= 0)
                    return false;

                if (!string.IsNullOrWhiteSpace(_receiveTarget.OutputWaferId) &&
                    !string.Equals(outputWafer.WaferId, _receiveTarget.OutputWaferId, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (outputWafer.OutputReceiveSlots != null && outputWafer.OutputReceiveSlots.Count > 0)
                {
                    int pendingTargetCount = 0;
                    bool currentTargetIsPending = false;
                    for (int i = 0; i < outputWafer.OutputReceiveSlots.Count; i++)
                    {
                        OutputReceiveSlotMaterial slot = outputWafer.OutputReceiveSlots[i];
                        bool pending = slot != null &&
                                       slot.IsTarget &&
                                       slot.Result == DieResult.Unknown &&
                                       string.IsNullOrWhiteSpace(slot.DieUid);
                        if (!pending)
                            continue;

                        pendingTargetCount++;
                        if (slot.OrderIndex == _receiveTarget.OrderIndex)
                            currentTargetIsPending = true;
                    }

                    if (currentTargetIsPending)
                        return pendingTargetCount == 1;
                }

                return _receiveTarget.OrderIndex >= outputWafer.OutputReceiveTotalCount - 1;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 마지막 Place 판단 중 예외가 발생하여 Z 복귀 지연을 금지합니다. " +
                    "error=" + ex.Message + " - Check");
                return true;
            }
        }

        private static bool TryResolveOutputSide(DieMaterial die, out BinSide side)
        {
            side = BinSide.Good;
            if (die == null)
                return false;

            // GYN GOOD/NG Stage를 모두 사용하는 경우, NG 결과도 Good Stage로 내려놓는다.
            // 현재 운전 정책: NG 결과도 Good Stage 순번으로 내려놓는다.
            // 나중에 NG Stage 배출로 복구하려면 DieResult.NG는 BinSide.Ng를 반환하도록 되돌린다.
            if (die.Result == DieResult.Good || die.Result == DieResult.NG)
            {
                side = BinSide.Good;
                return true;
            }

            return false;
        }

        private bool HasPendingContiRetreat()
        {
            return _pendingContiRetreatPickerIndex >= 0;
        }

        private void SetPendingContiRetreat(int pickerIndex, int pickerNo)
        {
            _pendingContiRetreatPickerIndex = pickerIndex;
            _pendingContiRetreatPickerNo = pickerNo;
        }

        private void ClearPendingContiRetreat()
        {
            _pendingContiRetreatPickerIndex = -1;
            _pendingContiRetreatPickerNo = 0;
        }

        private async Task<int> CompletePendingContiRetreatIfNeededAsync(string description, CancellationToken ct)
        {
            if (!HasPendingContiRetreat())
                return 0;

            PickerAxis zAxis = GetPickerZAxis(_pendingContiRetreatPickerIndex);
            double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
            int result = await MovePickerAxisAndVerifyAsync(
                zAxis,
                avoid,
                description,
                ct,
                "AvoidPosition").ConfigureAwait(false);
            if (result != 0)
                return result;

            ClearPendingContiRetreat();
            return 0;
        }

        private static string FormatTravel(BaseAxis axis, double target)
        {
            if (axis == null)
                return "axis=null";

            return Math.Abs(target - axis.ActualPosition).ToString("F3");
        }

        private async Task<int> MovePickerYAndTToPlaceBeforeContiSegmentedPlaceAsync(CancellationToken ct)
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

        private static double ResolveContiAsyncPlaceTriggerPosition(double start, double target, PickerPlaceMotionConfig placeConfig)
        {
            double ratio = placeConfig != null ? placeConfig.ContiXYMidRatio : 0.5;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                ratio = 0.5;
            ratio = Math.Max(0.0, Math.Min(1.0, ratio));
            return start + ((target - start) * ratio);
        }

        private static bool IsContiAsyncPlaceTriggerReached(double start, double target, double trigger, double actual)
        {
            double travel = target - start;
            if (Math.Abs(travel) <= 0.000001)
                return true;

            return travel > 0.0
                ? actual >= trigger
                : actual <= trigger;
        }

        private async Task<int> MovePickerZPlaceAfterContiProgressAsync(
            BinStageAxis yAxis,
            BaseAxis stageY,
            BaseAxis pickerX,
            double stageYStart,
            double pickerXStart,
            Task<int> stageYMove,
            Task<int> pickerXMove,
            PickerAxis pickerZAxis,
            double pickerZTarget,
            PickerPlaceMotionConfig placeConfig,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                double stageYTrigger = ResolveContiAsyncPlaceTriggerPosition(stageYStart, _targetOutputStageY, placeConfig);
                double pickerXTrigger = ResolveContiAsyncPlaceTriggerPosition(pickerXStart, _targetPickerX, placeConfig);
                int timeoutMs = Math.Max(1000, Math.Max(placeConfig != null ? placeConfig.ContiTimeoutMs : 0, ResolveTimeout()));
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                DateTime stoppedCheckDeadline = DateTime.UtcNow.AddMilliseconds(200);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    if (stageYMove != null && stageYMove.IsCompleted)
                    {
                        int stageResult = await stageYMove.ConfigureAwait(false);
                        if (stageResult != 0)
                            return stageResult;
                    }

                    if (pickerXMove != null && pickerXMove.IsCompleted)
                    {
                        int pickerXResult = await pickerXMove.ConfigureAwait(false);
                        if (pickerXResult != 0)
                            return pickerXResult;
                    }

                    double stageYActual = stageY != null ? stageY.ActualPosition : stageYStart;
                    double pickerXActual = pickerX != null ? pickerX.ActualPosition : pickerXStart;
                    bool stageYReached = IsContiAsyncPlaceTriggerReached(stageYStart, _targetOutputStageY, stageYTrigger, stageYActual);
                    bool pickerXReached = IsContiAsyncPlaceTriggerReached(pickerXStart, _targetPickerX, pickerXTrigger, pickerXActual);
                    if (stageYReached && pickerXReached)
                        break;

                    if (stageY != null && stageY.IsAlarm)
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                            "Place ContiNode PickerZ 하강 트리거 대기 중 OutputStageY 알람이 발생했습니다. " +
                            OutputStage.BuildStageAxisState(yAxis, _targetOutputStageY));
                    }

                    if (pickerX != null && pickerX.IsAlarm)
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                            "Place ContiNode PickerZ 하강 트리거 대기 중 PickerX 알람이 발생했습니다. " +
                            BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX));
                    }

                    if (DateTime.UtcNow >= stoppedCheckDeadline)
                    {
                        if (stageY != null &&
                            !stageY.IsMoving &&
                            !OutputStage.IsStageAxisInPosition(yAxis, _targetOutputStageY, ResolveOutputStageAxisTolerance(yAxis)) &&
                            !stageYReached)
                        {
                            return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                                "Place ContiNode PickerZ 하강 트리거 대기 중 OutputStageY가 목표 전 정지했습니다. " +
                                "start=" + stageYStart.ToString("F6") +
                                ", trigger=" + stageYTrigger.ToString("F6") +
                                ", actual=" + stageYActual.ToString("F6") +
                                ", " + OutputStage.BuildStageAxisState(yAxis, _targetOutputStageY));
                        }

                        if (pickerX != null &&
                            !pickerX.IsMoving &&
                            !IsPickerAxisInPosition(PickerAxis.PickerX, _targetPickerX) &&
                            !pickerXReached)
                        {
                            return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                                "Place ContiNode PickerZ 하강 트리거 대기 중 PickerX가 목표 전 정지했습니다. " +
                                "start=" + pickerXStart.ToString("F6") +
                                ", trigger=" + pickerXTrigger.ToString("F6") +
                                ", actual=" + pickerXActual.ToString("F6") +
                                ", " + BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX));
                        }
                    }

                    if (DateTime.UtcNow >= deadline)
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER-TIMEOUT", Name,
                            "Place ContiNode PickerZ 하강 트리거 대기 시간이 초과되었습니다. " +
                            "timeoutMs=" + timeoutMs +
                            ", stageYStart=" + stageYStart.ToString("F6") +
                            ", stageYTrigger=" + stageYTrigger.ToString("F6") +
                            ", stageYActual=" + stageYActual.ToString("F6") +
                            ", pickerXStart=" + pickerXStart.ToString("F6") +
                            ", pickerXTrigger=" + pickerXTrigger.ToString("F6") +
                            ", pickerXActual=" + pickerXActual.ToString("F6"));
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }

                WriteLog("PickerPlaceSequence",
                    Name + " Place ContiNode PickerZ 하강 트리거 도달. " +
                    "pickerNo=" + _currentPickerNo +
                    ", outputSide=" + _currentOutputSide +
                    ", stageYStart=" + stageYStart.ToString("F6") +
                    ", stageYTrigger=" + stageYTrigger.ToString("F6") +
                    ", stageYActual=" + (stageY != null ? stageY.ActualPosition.ToString("F6") : "-") +
                    ", pickerXStart=" + pickerXStart.ToString("F6") +
                    ", pickerXTrigger=" + pickerXTrigger.ToString("F6") +
                    ", pickerXActual=" + (pickerX != null ? pickerX.ActualPosition.ToString("F6") : "-") +
                    ", pickerZTarget=" + pickerZTarget.ToString("F6") +
                    " - Start");

                return await MovePickerAxisAndVerifyAsync(
                    pickerZAxis,
                    pickerZTarget,
                    "Place ContiNode PickerZ 비동기 하강",
                    ct,
                    BuildPickerTargetName("DiePlacePosition", _currentPickerIndex),
                    skipFinalPositionCheck: true).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-CONTI-ASYNC-Z-EX", Name,
                    "Place ContiNode PickerZ 비동기 하강 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
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

        private double ResolvePlaceZOverDrive()
        {
            PickerPlaceMotionConfig config = ResolvePlaceMotionConfig();
            return config != null ? config.PlaceZOverDrive : 0.0;
        }

        private int ResolvePlaceReleaseDwellMs()
        {
            PickerPlaceMotionConfig config = ResolvePlaceMotionConfig();
            return config != null ? Math.Max(0, config.PlaceReleaseDwellMs) : 0;
        }

        private int ResolvePlaceBlowDelayMs()
        {
            PickerPlaceMotionConfig config = ResolvePlaceMotionConfig();
            return config != null ? Math.Max(0, config.PlaceBlowDelayMs) : 0;
        }

        private void TurnPlaceBlowOff(string reason, bool force = false)
        {
            if (!_placeBlowHoldUntilAvoid && !force)
                return;

            try
            {
                SetPickerBlow(_currentPickerNo, false);
                WriteLog("PickerPlaceSequence",
                    Name + " Place Blow OFF. " +
                    "pickerNo=" + _currentPickerNo +
                    ", reason=" + reason + " - Ok");
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place Blow OFF 정리 실패. " +
                    "pickerNo=" + _currentPickerNo +
                    ", reason=" + reason +
                    ", error=" + ex.Message + " - Failed");
            }
            finally
            {
                _placeBlowHoldUntilAvoid = false;
            }
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
            return AppendAutoProcessCorrectionTargetTag(BuildPickerTargetName("DiePlacePosition", _currentPickerIndex) + ";PickerPhase=InspectionZHold;InspectionContinuous;From=Side;To=Place");
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

            int ngAvoidResult = await AwaitStepWithCancellationAsync(
                OutputStage.MoveNgStageToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct),
                ct).ConfigureAwait(false);
            if (ngAvoidResult != 0)
                return Fail("PICKER-PLACE-GOOD-Z-NG-AVOID", "OutputStage",
                    "Good Stage Z Process 이동 전 NG Stage Avoid 이동 실패. result=" + ngAvoidResult +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

            if (!OutputStage.IsNgStageInAvoidPosition())
                return Fail("PICKER-PLACE-GOOD-Z-NG-AVOID-CHECK", "OutputStage",
                    "Good Stage Z Process 이동 전 NG Stage가 Avoid 위치가 아닙니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", " + OutputStage.DescribeOutputStageInterlockState(_currentOutputSide));

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
                double target = ResolvePlacePickerTTarget(pickerIndex);

                if (!IsPickerAxisAlreadyInPosition(tAxis, target))
                    targets[tAxis] = target;
            }
        }

        private double ResolvePlacePickerTTarget(int pickerIndex)
        {
            // 현재 Place 대상 PickerT에는 Bottom 검사 회전 보정값이 적용된 최종 목표를 사용한다.
            if (pickerIndex == _currentPickerIndex)
                return _targetPickerT;

            return GetPickerTeachingPosition(GetPickerTAxis(pickerIndex), "PlacePosition");
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

            PickerAxis currentTAxis = GetPickerTAxis(_currentPickerIndex);
            WriteLog("PickerPlaceTargetVerify",
                Name + " place target verified after XYT move. die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", formula=" + (_targetFormula ?? "") +
                ", outputStageYState=" + OutputStage.BuildStageAxisState(yAxis, _targetOutputStageY) +
                ", pickerXState=" + BuildPickerAxisState(PickerAxis.PickerX, _targetPickerX) +
                ", pickerYState=" + BuildPickerAxisState(PickerAxis.PickerY, _targetPickerY) +
                ", pickerTState=" + BuildPickerAxisState(currentTAxis, _targetPickerT) +
                ", pickerZTarget=" + _targetPickerZ.ToString("F6") +
                " - Ok");

            if (_pickerZPlacedByContiSegmentedPlace)
            {
                PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
                if (!IsPickerAxisInPosition(zAxis, _targetPickerZ))
                {
                    return Fail("PICKER-PLACE-POSITION-CHECK", Name,
                        "Place ContiNode 이동 후 PickerZ 최종 위치 확인 실패. pickerNo=" + _currentPickerNo +
                        ", " + BuildPickerAxisState(zAxis, _targetPickerZ));
                }

                WriteLog("PickerPlaceTargetVerify",
                    Name + " place synchronized Z target verified after move. die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", formula=" + (_targetFormula ?? "") +
                    ", pickerZState=" + BuildPickerAxisState(zAxis, _targetPickerZ) +
                    " - Ok");

                CurrentStep = PickerPlaceStep.VacuumOff;
                return 0;
            }

            CurrentStep = PickerPlaceStep.MovePickerZPlace;
            return 0;
        }

        private async Task<int> MovePickerZPlaceAsync(CancellationToken ct)
        {
            if (_pickerZPlacedByContiSegmentedPlace)
            {
                CurrentStep = PickerPlaceStep.VacuumOff;
                return 0;
            }

            int result = await MovePickerAxisAndVerifyAsync(
                GetPickerZAxis(_currentPickerIndex),
                _targetPickerZ,
                "place picker Z",
                ct,
                BuildPickerTargetName("DiePlacePosition", _currentPickerIndex),
                skipFinalPositionCheck: true).ConfigureAwait(false);
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
                int releaseDwellMs = ResolvePlaceReleaseDwellMs();
                int blowDelayMs = ResolvePlaceBlowDelayMs();
                int totalDwellMs = Math.Max(releaseDwellMs, blowDelayMs);

                if (blowDelayMs > 0)
                {
                    SetPickerBlow(_currentPickerNo, true);
                    _placeBlowHoldUntilAvoid = true;

                    WriteLog("PickerPlaceSequence",
                        Name + " Place Blow ON. Place 위치에서 Blow Delay 동안만 Blow를 유지합니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", outputSide=" + _currentOutputSide +
                        ", releaseDwellMs=" + releaseDwellMs +
                        ", blowDelayMs=" + blowDelayMs +
                        ", totalDwellMs=" + totalDwellMs + " - Start");

                    await Task.Delay(blowDelayMs, ct).ConfigureAwait(false);
                    TurnPlaceBlowOff("Place Blow Delay 완료");
                }
                else
                {
                    TurnPlaceBlowOff("Place Blow Delay 0ms", true);
                    WriteLog("PickerPlaceSequence",
                        Name + " Place Blow Delay가 0ms라 Blow ON을 생략합니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", outputSide=" + _currentOutputSide +
                        ", releaseDwellMs=" + releaseDwellMs +
                        ", blowDelayMs=" + blowDelayMs +
                        ", totalDwellMs=" + totalDwellMs + " - Check");
                }

                int remainDwellMs = totalDwellMs - blowDelayMs;
                if (remainDwellMs > 0)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Place Release Dwell 잔여 대기 시작. " +
                        "Blow는 OFF 상태로 유지합니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", releaseDwellMs=" + releaseDwellMs +
                        ", blowDelayMs=" + blowDelayMs +
                        ", remainDwellMs=" + remainDwellMs + " - Start");
                    await Task.Delay(remainDwellMs, ct).ConfigureAwait(false);
                }

                if (ShouldDelayCurrentPickerZRetreatForNextContiPlace())
                {
                    SetPendingContiRetreat(_currentPickerIndex, _currentPickerNo);
                    WriteLog("PickerPlaceSequence",
                        Name + " Place 완료 후 현재 PickerZ Avoid 복귀를 다음 Place ContiNode에 포함하도록 지연합니다. " +
                        "Blow는 이미 OFF 상태입니다. " +
                        "pickerNo=" + _currentPickerNo +
                        ", cursor=" + _pickerCursor +
                        ", outputSide=" + _currentOutputSide +
                        ", releaseDwellMs=" + releaseDwellMs +
                        ", blowDelayMs=" + blowDelayMs +
                        ", totalDwellMs=" + totalDwellMs + " - Check");
                    CurrentStep = PickerPlaceStep.UpdateMaterialToOutputStage;
                    return 0;
                }

                CurrentStep = PickerPlaceStep.MovePickerZToAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                TurnPlaceBlowOff("Place Blow 유지 중 취소");
                throw;
            }
            catch (Exception ex)
            {
                TurnPlaceBlowOff("Place Blow 유지 중 예외");
                return Fail("PICKER-PLACE-BLOW", Name,
                    "Place Blow 유지 동작 중 예외가 발생했습니다. pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZToAvoidAsync(CancellationToken ct)
        {
            try
            {
                PickerAxis zAxis = GetPickerZAxis(_currentPickerIndex);
                double avoid = GetPickerTeachingPosition(zAxis, "AvoidPosition");
                int result = await MovePickerAxisAndVerifyAsync(zAxis, avoid, "place picker Z avoid", ct, "AvoidPosition").ConfigureAwait(false);
                if (result != 0)
                {
                    TurnPlaceBlowOff("PickerZ Avoid 복귀 실패");
                    return result;
                }

                TurnPlaceBlowOff("PickerZ Avoid 복귀 완료");
                ClearPendingContiRetreat();
                _currentPlaceZSafeReturnCompleted = true;
                CurrentStep = PickerPlaceStep.UpdateMaterialToOutputStage;
                return 0;
            }
            catch (OperationCanceledException)
            {
                TurnPlaceBlowOff("PickerZ Avoid 복귀 중 취소");
                throw;
            }
            catch (Exception ex)
            {
                TurnPlaceBlowOff("PickerZ Avoid 복귀 중 예외");
                return Fail("PICKER-PLACE-Z-AVOID-EX", Name,
                    "Place 후 PickerZ Avoid 복귀 중 예외가 발생했습니다. side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
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

            _placedDieId = _currentDie.DieId;
            _placedOutputSide = _currentOutputSide;
            _placedReceiveTarget = _receiveTarget;

            if (_suppressOutputPostPlaceInspection)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " manual Place: Output camera 후검사 큐 등록을 생략합니다. die=" +
                    _placedDieId + ", side=" + _placedOutputSide +
                    ", pickerNo=" + _currentPickerNo + " - Check");
                CurrentStep = PickerPlaceStep.RecoverOutputStageAfterPlace;
                return 0;
            }

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
                if (MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide))
                {
                    int handoffResult = await CompleteOutputStageExchangeHandoffAsync(ct).ConfigureAwait(false);
                    if (handoffResult != 0)
                        return handoffResult;

                    CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                    return 0;
                }

                if (HasPendingContiRetreat())
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

        private async Task<int> CompleteOutputStageExchangeHandoffAsync(CancellationToken ct)
        {
            int avoidResult = await MovePickerToAvoidAfterPlaceFastAsync(
                "OutputStage 마지막 Place 후 교체 준비 Picker 전체 Avoid",
                ct).ConfigureAwait(false);
            if (avoidResult != 0)
                return avoidResult;

            _currentPlaceZSafeReturnCompleted = true;
            ClearPendingContiRetreat();
            ForceSafeYBeforeFirstPlaceMove = true;
            KeepPickerYForwardDuringPlaceReadyWait = false;

            ReleaseOutputPlaceArea();
            ReleaseOutputStageArea();
            ReleaseOutputFeederArea();
            EndOutputPostPlaceInspectionBatch();

            int workZoneReleaseResult = ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(
                "OutputStage 마지막 Place 후 교체 준비");
            if (workZoneReleaseResult != 0)
                return workZoneReleaseResult;

            return await PublishOutputStageExchangeReadyAfterSafeCompletionAsync(ct).ConfigureAwait(false);
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

        private async Task<int> PublishOutputStageExchangeReadyAfterSafeCompletionAsync(CancellationToken ct)
        {
            try
            {
                if (!MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide))
                    return 0;

                if (!_currentPlaceZSafeReturnCompleted || !IsCurrentPickerAtFullAvoidPosition())
                {
                    return Fail("PICKER-PLACE-STAGE-COMPLETE-UNSAFE", Name,
                        "OutputStage 마지막 Place 교체 준비 신호를 발행할 수 없습니다. " +
                        "마지막 Place Picker 전체 Avoid 복귀가 완료되지 않았습니다. " +
                        "side=" + Side + ", outputSide=" + _currentOutputSide +
                        ", pickerNo=" + _currentPickerNo);
                }

                if (Context == null || Context.Bus == null)
                {
                    return Fail("PICKER-PLACE-STAGE-COMPLETE-BUS", Name,
                        "OutputStage 마지막 Place 교체 준비 신호를 발행할 Bus가 없습니다. " +
                        "side=" + Side + ", outputSide=" + _currentOutputSide +
                        ", pickerNo=" + _currentPickerNo);
                }

                if (Context.OutputPostPlaceInspections != null)
                {
                    int idleResult = await Context.OutputPostPlaceInspections.WaitUntilIdleAsync(
                        "OutputStageExchangeReady:" + Side + ":" + _currentOutputSide,
                        0,
                        ct).ConfigureAwait(false);
                    if (idleResult != 0)
                    {
                        return Fail("PICKER-PLACE-STAGE-COMPLETE-INSPECTION", Name,
                            "OutputStage 마지막 Place 후검사 완료 대기 실패. " +
                            "side=" + Side + ", outputSide=" + _currentOutputSide +
                            ", pickerNo=" + _currentPickerNo + ", result=" + idleResult);
                    }
                }

                if (!MaterialStateService.IsOutputStageReceiveComplete(_currentOutputSide) ||
                    !IsCurrentPickerAtFullAvoidPosition())
                {
                    return Fail("PICKER-PLACE-STAGE-COMPLETE-FINAL-CHECK", Name,
                        "OutputStage 교체 준비 신호 직전 최종 안전 확인 실패. " +
                        "side=" + Side + ", outputSide=" + _currentOutputSide +
                        ", pickerNo=" + _currentPickerNo);
                }

                string signal = _currentOutputSide == BinSide.Ng
                    ? "OutputNgStageReceiveComplete"
                    : "OutputGoodStageReceiveComplete";

                Context.Bus.Set(signal);
                WriteLog("PickerPlaceSequence",
                    Name + " OutputStage 마지막 Place 후 Picker 전체 Avoid 및 후검사 완료를 확인하고 교체 준비 신호를 발행했습니다. " +
                    "side=" + _currentOutputSide + ", signal=" + signal +
                    ", pickerNo=" + _currentPickerNo + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-STAGE-COMPLETE-NOTIFY", Name,
                    "OutputStage 교체 준비 신호 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsCurrentPickerAtFullAvoidPosition()
        {
            PickerAxis[] axes =
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

            for (int i = 0; i < axes.Length; i++)
            {
                PickerAxis axis = axes[i];
                if (!IsPickerAxisInPosition(axis, GetPickerTeachingPosition(axis, "AvoidPosition")))
                    return false;
            }

            return true;
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

                int workZoneReleaseResult = ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(
                    "Place 완료 후 후검사 대기 전");
                if (workZoneReleaseResult != 0)
                    return workZoneReleaseResult;

                int completionResult = await PublishOutputStageExchangeReadyAfterSafeCompletionAsync(ct).ConfigureAwait(false);
                if (completionResult != 0)
                    return completionResult;

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

        private int ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(string description)
        {
            if (_parentOutputWorkZoneReleaseNotified)
                return 0;

            Func<string, bool> release = ReleaseParentOutputWorkZoneAfterSafeAvoid;
            if (release == null)
                return 0;

            try
            {
                bool released = release(description);
                if (!released)
                {
                    return Fail("PICKER-PLACE-PARENT-WORK-ZONE-RELEASE", Name,
                        "Place 완료 후 Output camera 후검사 대기 전 부모 Picker Output 작업영역을 해제하지 못했습니다. " +
                        "side=" + Side + ", description=" + description);
                }

                _parentOutputWorkZoneReleaseNotified = true;
                WriteLog("PickerPlaceSequence",
                    Name + " Picker 전체 Avoid 확인 후 Output camera 후검사 대기 전에 " +
                    "부모 Picker Output 작업영역을 해제했습니다. side=" + Side +
                    ", description=" + description + " - Ok");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("PICKER-PLACE-PARENT-WORK-ZONE-RELEASE-EX", Name,
                    "Place 완료 후 부모 Picker Output 작업영역 해제 중 예외가 발생했습니다. " +
                    "side=" + Side + ", description=" + description +
                    ", error=" + ex.Message);
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

        private void CancelOutputPostPlaceInspectionBatch(string reason)
        {
            try
            {
                if (!_outputInspectBatchOpen)
                    return;

                _outputInspectBatchOpen = false;

                if (Context == null || Context.OutputPostPlaceInspections == null)
                    return;

                Context.OutputPostPlaceInspections.CancelBatch(Name, reason);
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Output camera 후검사 묶음 취소 처리 중 예외가 발생했습니다. reason=" +
                    reason + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}

