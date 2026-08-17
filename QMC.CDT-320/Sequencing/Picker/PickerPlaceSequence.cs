using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Recipes;
using QMC.Common.Motion;
using QMC.Common;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal sealed partial class PickerPlaceSequence : PickerSequenceBase<PickerPlaceStep>
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
        private BottomVisionOffset _currentBottomPlaceResult;
        private bool _resultRoutingModeCaptured;
        private OutputStageResultRoutingMode _resultRoutingModeSnapshot = OutputStageResultRoutingMode.ForceGoodStage;
        private bool _placeCorrectionConfigCaptured;
        private double _placeMechanicalOffsetXSnapshot;
        private double _placeMechanicalOffsetYSnapshot;
        private double _placeMechanicalOffsetTSnapshot;
        private double _bottomPlaceCorrectionLimitSnapshot = PickerPickUpMotionConfig.DefaultMechanicalOffsetLimitMm;
        // R4(follow-entry): 비동기 시작한 OutputVisionX 최소 회피 이동 Task와 확정 목표.
        // 피커 X 진입(MoveOutputStageReceivePosition) 완료 전에 반드시 join(결과 0 확인)한다.
        // 기존 조건(사용자 지시 2026-07-25, O-2): 이동 명령 발행을 피커 X 진입 직전까지 이연했다
        //   (_outputVisionRetreatDeferred) — 인풋 미러 실측에서 이연 설계가 홀드 손해로 확인됐다.
        // 현재 기준(사용자 지시 2026-07-26, 이연 폐지·인풋 미러): 후검사 큐가 배치 EPD 직후 시작한
        //   독립 회피(VisionIndependentRetreatCoordinator)를 인수하거나(목표 부족 시 연장 합성),
        //   외부 회피가 없으면 회피 좌표 확정 즉시 비동기 자체 기동한다.
        private Task<int> _outputVisionRetreatMoveTask;
        private double _outputVisionRetreatTarget;

        public bool ForceSafeYBeforeFirstPlaceMove { get; set; }
        public bool KeepPickerYForwardDuringPlaceReadyWait { get; set; }
        internal Func<string, bool> ReleaseParentOutputWorkZoneAfterSafeAvoid { get; set; }
        internal Func<BinSide, int, string, CancellationToken, Task<int>> WaitForOutputStageExchangeWithProcessHandoffAsync { get; set; }
        internal Func<int, string, double, CancellationToken, Task<int>> WaitBottomFinalBeforePlaceMoveAsync { get; set; }
        internal Func<int, string, BottomVisionOffset> GetValidatedBottomPlaceResult { get; set; }
        internal Func<int, string, CancellationToken, Task<int>> WaitInspectionResultsBeforePlaceDownAsync { get; set; }

        public PickerPlaceSequence(MachineSequenceContext context, PickerSequenceSide side)
            : base(context, side, PickerSequenceKind.UnloadToOutput, side == PickerSequenceSide.Front ? "FrontPickerPlaceSequence" : "RearPickerPlaceSequence")
        {
            CurrentStep = PickerPlaceStep.CheckUnit;
        }

        public bool IsComplete
        {
            get { return CurrentStep == PickerPlaceStep.Complete; }
        }

        // [사용자 지시 2026-07-27] 정지/종료 경계에서 백그라운드 Z Avoid 상승이 진행 중이면
        // 완주를 기다린 뒤 정지한다 — 상승이 도중에 잘리며 -5(이동 완료 후 command≠target)로
        // 유닛 PK-MOVE 알람이 승격되던 사고(실장비 2026-07-27 03:42, RearPickerZ0 -8.198) 차단.
        public async Task WaitPendingPickerZAvoidRiseBeforeStopAsync(int timeoutMs = 5000)
        {
            Task<int> rise = _pendingContiRetreatRiseTask;
            if (rise == null || rise.IsCompleted)
                return;

            WriteLog("PickerPlaceSequence",
                Name + " 정지 전 백그라운드 PickerZ Avoid 상승 완주를 대기합니다. timeoutMs=" + timeoutMs + " - Wait");
            await Task.WhenAny(rise, Task.Delay(timeoutMs)).ConfigureAwait(false);
        }

        public void Abort()
        {
            try
            {
                // [사용자 지시 2026-07-27] Abort 정리 전 상승 완주 대기(최대 5초) — 동기 경계라 Wait 사용.
                Task<int> rise = _pendingContiRetreatRiseTask;
                if (rise != null && !rise.IsCompleted)
                {
                    try { rise.Wait(5000); } catch { }
                }

                ObserveOutputVisionRetreatMoveTaskOnAbort();
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
            _currentBottomPlaceResult = null;
            _placeCorrectionConfigCaptured = false;

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
                OutputWaferInstanceId = source.OutputWaferInstanceId ?? "",
                SourceWaferId = source.SourceWaferId ?? "",
                SourceWaferInstanceId = source.SourceWaferInstanceId ?? "",
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

                // 현재 Place 대상 Picker의 Bottom FINAL만 먼저 확보합니다.
                // 다른 Picker의 Bottom/Side 결과는 기다리지 않으며 Side FINAL은 기존 Z 하강 게이트까지 병렬 수집합니다.
                case PickerPlaceStep.WaitBottomFinalBeforePlaceMove:
                    Log.Write("PickerPlaceSequence", Name + " Place 대상 Bottom FINAL 확인 시작. side=" + Side + ", step=" + CurrentStep);
                    return WaitCurrentBottomFinalBeforePlaceMoveAsync(ct);

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

            OutputStageResultRoutingMode routingMode = OutputStage.Config != null
                ? OutputStage.Config.ResultRoutingMode
                : OutputStageResultRoutingMode.ForceGoodStage;
            if (routingMode != OutputStageResultRoutingMode.ForceGoodStage &&
                routingMode != OutputStageResultRoutingMode.RouteByInspectionResult)
            {
                return Fail("PICKER-PLACE-ROUTING-MODE", "OutputStage",
                    "지원하지 않는 Die 결과 배출 모드이므로 Place를 시작하지 않습니다. mode=" + routingMode + ".");
            }

            // [NG 스킵 2026-07-27] 설정 조합 모순 방어.
            // RouteByInspectionResult 이면 NG Die가 NG Stage로 가는데, UseNgCassette=false 면
            // Ng1 카세트가 disabled 라 NG Stage는 영원히 공급되지 않는다. 그 상태로 진행하면
            // VerifyOutputStageReadyAsync 가 Die를 문 채 무한 대기한다(자동 모드는 timeout 없음).
            // 모션 시작 전에 명확한 알람으로 끊는다.
            var outputCassetteConfig = Context != null && Context.Machine != null && Context.Machine.OutputCassetteUnit != null
                ? Context.Machine.OutputCassetteUnit.Config
                : null;
            bool ngCassetteUsed = outputCassetteConfig == null || outputCassetteConfig.UseNgCassette;
            if (routingMode == OutputStageResultRoutingMode.RouteByInspectionResult && !ngCassetteUsed)
            {
                return Fail("PICKER-PLACE-NG-ROUTING-UNAVAILABLE", "OutputStage",
                    "설정이 서로 모순됩니다. 검사 결과별 배출(RouteByInspectionResult)은 NG Stage가 필요한데 " +
                    "NG 카세트가 미사용(UseNgCassette=false)이라 NG Stage를 공급할 수 없습니다. " +
                    "OutputStage 배출 모드를 ForceGoodStage로 바꾸거나 NG 카세트를 사용으로 설정하세요. " +
                    "mode=" + routingMode + ", useNgCassette=" + ngCassetteUsed + ".");
            }

            _resultRoutingModeSnapshot = routingMode;
            _resultRoutingModeCaptured = true;
            WriteLog("PickerPlaceSequence",
                Name + " Place batch의 Die 결과 배출 모드를 고정했습니다. " +
                "routingMode=" + _resultRoutingModeSnapshot + " - Check");

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
            {
                if (WaitInspectionResultsBeforePlaceDownAsync == null)
                    return Fail("PICKER-PLACE-DIE-RESULT-UNKNOWN", "Material", unknownReason);

                WriteLog("PickerPlaceSequence",
                    Name + " Bottom/Side 최종 RESULT가 아직 완료되지 않았지만 Place 접근을 시작합니다. " +
                    "각 PickerZ 최초 하강 직전에 해당 Picker 결과만 기다립니다. detail=" + unknownReason + " - Check");
            }

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
            _currentBottomPlaceResult = null;
            _placeCorrectionConfigCaptured = false;

            if (_currentDie == null)
            {
                CurrentStep = PickerPlaceStep.SelectNextPickerOrComplete;
                return 0;
            }

            CurrentStep = PickerPlaceStep.WaitBottomFinalBeforePlaceMove;
            return 0;
        }

        private async Task<int> WaitCurrentBottomFinalBeforePlaceMoveAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (Options != null && Options.RunMode != SequenceRunMode.Auto)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Manual Place 흐름은 기존 완료된 검사 결과를 사용하며 Auto 전용 Bottom FINAL 선행 게이트를 실행하지 않습니다. " +
                    "side=" + Side + ", pickerNo=" + _currentPickerNo + " - Check");
                CurrentStep = PickerPlaceStep.ResolveOutputSide;
                return 0;
            }

            if (_currentDie == null || string.IsNullOrWhiteSpace(_currentDie.DieId))
            {
                return await FailAfterPendingContiRetreatAsync(
                    "PICKER-PLACE-BOTTOM-GATE-DIE",
                    "Material",
                    "Place 이동 전에 Bottom FINAL과 연결할 현재 Picker 제품이 없습니다. " +
                    "side=" + Side + ", pickerNo=" + _currentPickerNo + ".",
                    ct).ConfigureAwait(false);
            }

            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            double correctionLimitMm = placeConfig != null
                ? placeConfig.BottomPlaceCorrectionLimitMm
                : PickerPickUpMotionConfig.DefaultMechanicalOffsetLimitMm;
            _placeMechanicalOffsetXSnapshot = placeConfig != null
                ? placeConfig.GetMechanicalOffsetX(_currentPickerIndex)
                : 0.0;
            _placeMechanicalOffsetYSnapshot = placeConfig != null
                ? placeConfig.GetMechanicalOffsetY(_currentPickerIndex)
                : 0.0;
            _placeMechanicalOffsetTSnapshot = placeConfig != null
                ? placeConfig.GetMechanicalOffsetT(_currentPickerIndex)
                : 0.0;
            _bottomPlaceCorrectionLimitSnapshot = correctionLimitMm;
            _placeCorrectionConfigCaptured = true;

            if (WaitBottomFinalBeforePlaceMoveAsync == null || GetValidatedBottomPlaceResult == null)
            {
                // [시뮬 통과 2026-07-28] 비전 미연결/시뮬 환경에서는 Bottom FINAL RESULT 자체가 생성되지 않는다.
                // 이때 Bottom/Side 검사 시퀀스가 완료 상태가 되지 못해 위 두 델리게이트가 주입되지 않고,
                // 저장된 FINAL 필드도 없어 PICKER-PLACE-BOTTOM-GATE-CALLBACK 으로 Place 가 막혔다.
                // 실장비 판정에는 영향이 없도록, 기존 IsPlaceProductCheckBypassed 와 동일한 조건
                // (BypassHardware / SimulationMode / DryRunMode / GlobalDryRun) 에서만 무보정으로 통과시킨다.
                // 보정값은 0 으로 두어 티칭 좌표 그대로 Place 한다(가짜 보정값을 만들지 않는다).
                if (IsPlaceProductCheckBypassed())
                {
                    _currentBottomPlaceResult = new BottomVisionOffset
                    {
                        PickerNo = _currentPickerNo,
                        BottomItemOffsetX = 0.0,
                        BottomItemOffsetY = 0.0,
                        HasBottomItemOffsetX = false,
                        HasBottomItemOffsetY = false
                    };

                    WriteLog("PickerPlaceSequence",
                        Name + " Place 이동 전 Bottom FINAL 보정 확인을 Simulation/DryRun 조건으로 통과합니다. " +
                        "보정값 0(무보정)으로 티칭 좌표를 그대로 사용합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") + " - Bypass");

                    CurrentStep = PickerPlaceStep.ResolveOutputSide;
                    return 0;
                }

                string storedReason = "Bottom/Side 저장 검사 흐름이 완료되지 않았습니다.";
                if (IsInspectionFlowComplete(_currentDie) &&
                    TryResolveStoredBottomPlaceResult(
                        _currentDie,
                        correctionLimitMm,
                        out _currentBottomPlaceResult,
                        out storedReason))
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Place 재개 시 저장된 Bottom FINAL 검사값으로 보정값을 복원했습니다. " +
                        "새 Vision RESULT를 기다리지 않으며 동일 절대좌표 목표를 다시 계산합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDie.DieId +
                        ", bottomItemOffsetX=" + _currentBottomPlaceResult.BottomItemOffsetX.ToString("F6") +
                        ", bottomItemOffsetY=" + _currentBottomPlaceResult.BottomItemOffsetY.ToString("F6") +
                        ", correctionLimitMm=±" + correctionLimitMm.ToString("F3") + " - Check");
                    CurrentStep = PickerPlaceStep.ResolveOutputSide;
                    return 0;
                }

                return await FailAfterPendingContiRetreatAsync(
                    "PICKER-PLACE-BOTTOM-GATE-CALLBACK",
                    "Vision",
                    "Place 이동 전 Bottom FINAL 보정 확인 경로가 연결되지 않았습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId +
                    ", storedResultReason=" + (storedReason ?? string.Empty) +
                    ". Bottom/Side 검사를 다시 수행한 뒤 Place를 재시도하세요.",
                    ct).ConfigureAwait(false);
            }

            WriteLog("PickerPlaceSequence",
                Name + " 현재 Place 대상 Picker의 Bottom FINAL만 먼저 기다립니다. " +
                "다른 Picker 결과는 기다리지 않습니다. side=" + Side +
                ", pickerNo=" + _currentPickerNo +
                ", die=" + _currentDie.DieId +
                ", correctionLimitMm=±" + correctionLimitMm.ToString("F3") +
                ", mechanicalOffsetX=" + _placeMechanicalOffsetXSnapshot.ToString("F3") +
                ", mechanicalOffsetY=" + _placeMechanicalOffsetYSnapshot.ToString("F3") +
                ", mechanicalOffsetT=" + _placeMechanicalOffsetTSnapshot.ToString("F3") + " - Start");

            int bottomResult;
            using (CancellationTokenSource bottomGateCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                Task<int> bottomWaitTask = WaitBottomFinalBeforePlaceMoveAsync(
                    _currentPickerNo,
                    _currentDie.DieId,
                    correctionLimitMm,
                    bottomGateCancellation.Token);

                // 이전 Place의 지연 Z 복귀가 남아 있고 Bottom RESULT가 아직 준비되지 않았다면
                // RESULT 대기와 안전 복귀를 겹쳐 수행한 뒤 다음 Stage/Picker 이동으로 진행합니다.
                if (!bottomWaitTask.IsCompleted && HasPendingContiRetreat())
                {
                    int retreatResult = await CompletePendingContiRetreatIfNeededAsync(
                        "다음 Picker Bottom FINAL 대기 중 이전 PickerZ 안전 복귀",
                        ct).ConfigureAwait(false);
                    if (retreatResult != 0)
                    {
                        bottomGateCancellation.Cancel();
                        await DrainGateTaskAfterCancellationAsync(
                            bottomWaitTask,
                            "Bottom FINAL 선행 게이트").ConfigureAwait(false);
                        return retreatResult;
                    }
                }

                bottomResult = await bottomWaitTask.ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();
            if (bottomResult != 0)
            {
                return await FailAfterPendingContiRetreatAsync(
                    "PICKER-PLACE-BOTTOM-GATE",
                    "Vision",
                    "Bottom FINAL 보정값이 준비되지 않아 Stage/Picker Place 이동을 차단합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId +
                    ", result=" + bottomResult + ".",
                    ct).ConfigureAwait(false);
            }

            _currentBottomPlaceResult = GetValidatedBottomPlaceResult(
                _currentPickerNo,
                _currentDie.DieId);
            if (_currentBottomPlaceResult == null)
            {
                return await FailAfterPendingContiRetreatAsync(
                    "PICKER-PLACE-BOTTOM-GATE-CACHE",
                    "Vision",
                    "Bottom FINAL 검증은 완료됐지만 현재 Place 대상의 보정값을 가져오지 못했습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId + ".",
                    ct).ConfigureAwait(false);
            }

            if (ResolveOutputStageResultRoutingMode() == OutputStageResultRoutingMode.RouteByInspectionResult)
            {
                if (WaitInspectionResultsBeforePlaceDownAsync == null)
                {
                    return await FailAfterPendingContiRetreatAsync(
                        "PICKER-PLACE-ROUTING-RESULT-CALLBACK",
                        "Vision",
                        "검사 결과별 출력 Stage 분기 모드이지만 Bottom/Side 최종 판정 경로가 연결되지 않았습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDie.DieId + ".",
                        ct).ConfigureAwait(false);
                }

                int inspectionResult;
                using (CancellationTokenSource inspectionGateCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    Task<int> inspectionWaitTask = WaitInspectionResultsBeforePlaceDownAsync(
                        _currentPickerNo,
                        _currentDie.DieId,
                        inspectionGateCancellation.Token);
                    if (!inspectionWaitTask.IsCompleted && HasPendingContiRetreat())
                    {
                        int retreatResult = await CompletePendingContiRetreatIfNeededAsync(
                            "검사 결과별 Stage 분기 대기 중 이전 PickerZ 안전 복귀",
                            ct).ConfigureAwait(false);
                        if (retreatResult != 0)
                        {
                            inspectionGateCancellation.Cancel();
                            await DrainGateTaskAfterCancellationAsync(
                                inspectionWaitTask,
                                "검사 결과별 출력 Stage 분기 게이트").ConfigureAwait(false);
                            return retreatResult;
                        }
                    }

                    inspectionResult = await inspectionWaitTask.ConfigureAwait(false);
                }
                ct.ThrowIfCancellationRequested();
                if (inspectionResult != 0)
                {
                    return await FailAfterPendingContiRetreatAsync(
                        "PICKER-PLACE-ROUTING-RESULT-GATE",
                        "Vision",
                        "검사 결과별 출력 Stage 분기에 필요한 Bottom/Side 최종 판정을 받지 못했습니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + _currentDie.DieId +
                        ", result=" + inspectionResult + ".",
                        ct).ConfigureAwait(false);
                }
            }

            WriteLog("PickerPlaceSequence",
                Name + " 현재 Place 대상 Bottom FINAL 보정 확보 완료. " +
                "pickerNo=" + _currentPickerNo +
                ", die=" + _currentDie.DieId +
                ", bottomItemOffsetX=" + _currentBottomPlaceResult.BottomItemOffsetX.ToString("F6") +
                ", bottomItemOffsetY=" + _currentBottomPlaceResult.BottomItemOffsetY.ToString("F6") +
                ", bottomOverallOk=" + _currentBottomPlaceResult.IsOk +
                ", routingMode=" + ResolveOutputStageResultRoutingMode() + " - Ok");

            CurrentStep = PickerPlaceStep.ResolveOutputSide;
            return 0;
        }

        private bool TryResolveStoredBottomPlaceResult(
            DieMaterial die,
            double correctionLimitMm,
            out BottomVisionOffset result,
            out string reason)
        {
            result = null;
            reason = string.Empty;
            if (die == null || die.Inspections == null)
            {
                reason = "Material 또는 검사 기록이 없습니다.";
                return false;
            }
            if (double.IsNaN(correctionLimitMm) ||
                double.IsInfinity(correctionLimitMm) ||
                correctionLimitMm < 0.0)
            {
                reason = "Place 보정 허용 범위가 올바르지 않습니다. limit=" + correctionLimitMm;
                return false;
            }

            DieInspectionRecord bottomRecord = null;
            for (int i = 0; i < die.Inspections.Count; i++)
            {
                DieInspectionRecord candidate = die.Inspections[i];
                if (candidate != null &&
                    string.Equals(candidate.InspectionType, "Bottom", StringComparison.OrdinalIgnoreCase))
                {
                    bottomRecord = candidate;
                    break;
                }
            }
            if (bottomRecord == null || bottomRecord.Measurements == null)
            {
                reason = "Bottom 검사 기록 또는 상세 측정값이 없습니다.";
                return false;
            }

            InspectionMeasurement offsetX = FindInspectionMeasurement(bottomRecord, "BottomItemOffsetX");
            InspectionMeasurement offsetY = FindInspectionMeasurement(bottomRecord, "BottomItemOffsetY");
            InspectionMeasurement hasOffsetX = FindInspectionMeasurement(bottomRecord, "BottomItemOffsetXPresent");
            InspectionMeasurement hasOffsetY = FindInspectionMeasurement(bottomRecord, "BottomItemOffsetYPresent");
            InspectionMeasurement hasOffsetXPass = FindInspectionMeasurement(bottomRecord, "BottomItemOffsetXPassPresent");
            InspectionMeasurement hasOffsetYPass = FindInspectionMeasurement(bottomRecord, "BottomItemOffsetYPassPresent");
            InspectionMeasurement offsetXPass = FindInspectionMeasurement(bottomRecord, "BottomItemOffsetXPass");
            InspectionMeasurement offsetYPass = FindInspectionMeasurement(bottomRecord, "BottomItemOffsetYPass");
            InspectionMeasurement hasMeasureValid = FindInspectionMeasurement(bottomRecord, "BottomMeasureValidPresent");
            InspectionMeasurement measureValid = FindInspectionMeasurement(bottomRecord, "BottomMeasureValid");
            InspectionMeasurement requestId = FindInspectionMeasurement(bottomRecord, "BottomRequestId");
            InspectionMeasurement groupId = FindInspectionMeasurement(bottomRecord, "BottomGroupId");
            InspectionMeasurement resultDieId = FindInspectionMeasurement(bottomRecord, "BottomDieId");
            InspectionMeasurement dieIndex = FindInspectionMeasurement(bottomRecord, "BottomDieIndex");
            InspectionMeasurement resultPickerNo = FindInspectionMeasurement(bottomRecord, "BottomPickerNo");
            InspectionMeasurement pickedAtTicks = FindInspectionMeasurement(bottomRecord, "BottomPickedAtTicks");
            if (offsetX == null || offsetY == null ||
                hasOffsetX == null || hasOffsetY == null ||
                hasOffsetXPass == null || hasOffsetYPass == null ||
                offsetXPass == null || offsetYPass == null ||
                hasMeasureValid == null || measureValid == null ||
                requestId == null || groupId == null ||
                resultDieId == null || dieIndex == null ||
                resultPickerNo == null || pickedAtTicks == null)
            {
                reason = "저장된 Bottom FINAL 보정/상관관계/Picker 세대 필드가 부족합니다.";
                return false;
            }

            bool flagsOk =
                hasOffsetX.Value > 0.5 &&
                hasOffsetY.Value > 0.5 &&
                hasOffsetXPass.Value > 0.5 &&
                hasOffsetYPass.Value > 0.5 &&
                offsetXPass.Value > 0.5 &&
                offsetYPass.Value > 0.5 &&
                hasMeasureValid.Value > 0.5 &&
                measureValid.Value > 0.5;
            if (!flagsOk)
            {
                reason = "저장된 Bottom FINAL measure_valid/item offset pass 조건이 유효하지 않습니다.";
                return false;
            }
            if (double.IsNaN(offsetX.Value) || double.IsInfinity(offsetX.Value) ||
                double.IsNaN(offsetY.Value) || double.IsInfinity(offsetY.Value) ||
                Math.Abs(offsetX.Value) > correctionLimitMm ||
                Math.Abs(offsetY.Value) > correctionLimitMm)
            {
                reason = "저장된 Bottom FINAL item offset이 유한값 또는 허용 범위를 만족하지 않습니다. " +
                         "offsetX=" + offsetX.Value.ToString("F6") +
                         ", offsetY=" + offsetY.Value.ToString("F6") +
                         ", limit=±" + correctionLimitMm.ToString("F3");
                return false;
            }

            string storedRequestId = requestId.RawValue ?? string.Empty;
            string storedGroupId = groupId.RawValue ?? string.Empty;
            string storedDieId = resultDieId.RawValue ?? string.Empty;
            int storedDieIndex = (int)Math.Round(dieIndex.Value, MidpointRounding.AwayFromZero);
            int storedPickerNo = (int)Math.Round(resultPickerNo.Value, MidpointRounding.AwayFromZero);
            long storedPickedAtTicks;
            bool hasStoredPickedAt = long.TryParse(
                pickedAtTicks.RawValue ?? string.Empty,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out storedPickedAtTicks);
            if (string.IsNullOrWhiteSpace(storedRequestId) ||
                string.IsNullOrWhiteSpace(storedGroupId) ||
                !string.Equals(storedDieId, die.DieId, StringComparison.Ordinal) ||
                Math.Abs(dieIndex.Value - storedDieIndex) > 0.000001 ||
                storedDieIndex != die.InputSequenceNo ||
                Math.Abs(resultPickerNo.Value - storedPickerNo) > 0.000001 ||
                storedPickerNo != _currentPickerNo ||
                die.PickedPickerLocation != PickerLocationKind ||
                die.PickedPickerNo != _currentPickerNo ||
                die.PickedAt <= new DateTime(1900, 1, 1, 23, 59, 59) ||
                !hasStoredPickedAt ||
                storedPickedAtTicks != die.PickedAt.Ticks ||
                bottomRecord.UpdatedAt < die.PickedAt)
            {
                reason = "저장된 Bottom FINAL 상관관계 또는 Picker/Pick 세대가 현재 Material과 일치하지 않습니다. " +
                         "currentDie=" + (die.DieId ?? string.Empty) +
                         ", storedDie=" + storedDieId +
                         ", currentDieIndex=" + die.InputSequenceNo +
                         ", storedDieIndex=" + dieIndex.Value.ToString("F6") +
                         ", currentPickerNo=" + _currentPickerNo +
                         ", storedPickerNo=" + resultPickerNo.Value.ToString("F6") +
                         ", materialPickedPickerNo=" + die.PickedPickerNo +
                         ", currentPickedAtTicks=" + die.PickedAt.Ticks +
                         ", storedPickedAtTicks=" + (hasStoredPickedAt ? storedPickedAtTicks.ToString() : "invalid") +
                         ", inspectionUpdatedAt=" + bottomRecord.UpdatedAt.ToString("O");
                return false;
            }

            InspectionMeasurement raw = FindInspectionMeasurement(bottomRecord, "BottomVisionRaw");
            result = new BottomVisionOffset
            {
                PickerNo = _currentPickerNo,
                BottomItemOffsetX = offsetX.Value,
                BottomItemOffsetY = offsetY.Value,
                HasBottomItemOffsetX = true,
                HasBottomItemOffsetY = true,
                BottomItemOffsetXPass = true,
                BottomItemOffsetYPass = true,
                HasBottomItemOffsetXPass = true,
                HasBottomItemOffsetYPass = true,
                MeasureValid = true,
                HasMeasureValid = true,
                RequestId = storedRequestId,
                GroupId = storedGroupId,
                DieId = storedDieId,
                DieIndex = storedDieIndex,
                IsOk = bottomRecord.Result == MaterialInspectionResult.Ok,
                Raw = raw != null ? raw.RawValue ?? string.Empty : string.Empty
            };
            return true;
        }

        private static InspectionMeasurement FindInspectionMeasurement(
            DieInspectionRecord record,
            string name)
        {
            if (record == null || record.Measurements == null || string.IsNullOrWhiteSpace(name))
                return null;

            for (int i = 0; i < record.Measurements.Count; i++)
            {
                InspectionMeasurement measurement = record.Measurements[i];
                if (measurement != null &&
                    string.Equals(measurement.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return measurement;
                }
            }

            return null;
        }

        private async Task<int> FailAfterPendingContiRetreatAsync(
            string alarmCode,
            string source,
            string message,
            CancellationToken ct)
        {
            if (HasPendingContiRetreat())
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place 선행 게이트 실패 전 이전 PickerZ를 먼저 안전 복귀합니다. " +
                    "reason=" + (message ?? string.Empty) + " - Start");
                int retreatResult = await CompletePendingContiRetreatIfNeededAsync(
                    "Place 선행 게이트 실패 전 이전 PickerZ 안전 복귀",
                    ct).ConfigureAwait(false);
                if (retreatResult != 0)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " 원래 Place 선행 게이트 실패와 함께 이전 PickerZ 안전 복귀도 실패했습니다. " +
                        "originalAlarmCode=" + (alarmCode ?? string.Empty) +
                        ", originalReason=" + (message ?? string.Empty) +
                        ", retreatResult=" + retreatResult + " - Failed");
                    return retreatResult;
                }
            }

            return Fail(alarmCode, source, message);
        }

        private async Task DrainGateTaskAfterCancellationAsync(
            Task<int> gateTask,
            string description)
        {
            if (gateTask == null)
                return;

            try
            {
                int result = await gateTask.ConfigureAwait(false);
                WriteLog("PickerPlaceSequence",
                    Name + " 취소 요청 후 이미 시작된 Vision 게이트 Task를 정리했습니다. " +
                    "description=" + (description ?? string.Empty) +
                    ", result=" + result + " - Check");
            }
            catch (OperationCanceledException)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " 취소 요청 후 Vision 게이트 Task가 취소 상태로 정리되었습니다. " +
                    "description=" + (description ?? string.Empty) + " - Check");
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " 취소 요청 후 Vision 게이트 Task 정리 중 예외가 발생했습니다. " +
                    "description=" + (description ?? string.Empty) +
                    ", error=" + ex.Message + " - Failed");
            }
        }

    }
}
