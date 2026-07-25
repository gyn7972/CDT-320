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
        private BottomVisionOffset _currentBottomPlaceResult;
        private bool _resultRoutingModeCaptured;
        private OutputStageResultRoutingMode _resultRoutingModeSnapshot = OutputStageResultRoutingMode.ForceGoodStage;
        private bool _placeCorrectionConfigCaptured;
        private double _placeMechanicalOffsetXSnapshot;
        private double _placeMechanicalOffsetYSnapshot;
        private double _bottomPlaceCorrectionLimitSnapshot = PickerPickUpMotionConfig.DefaultMechanicalOffsetLimitMm;
        // R4(follow-entry): 비동기 시작한 OutputVisionX 최소 회피 이동 Task와 확정 목표.
        // 피커 X 진입(MoveOutputStageReceivePosition) 완료 전에 반드시 join(결과 0 확인)한다.
        private Task<int> _outputVisionRetreatMoveTask;
        private double _outputVisionRetreatTarget;
        // 기존 조건(#R4): 회피 스텝에서 최소 회피 이동을 즉시 비동기 시작했다 — 피커 X 출발까지
        //   선행 모션(Feeder 안전/워크에어리어 점유/목표 계산, Conti 경로의 PickerY·T 선행 이동 등)이
        //   순차 await되는 사이에 짧은 최소 회피가 끝나버려, 진입 시점에는 OutputCameraX.IsMoving=false라
        //   오버랩이 성립하지 않았다.
        // 현재 기준(사용자 지시 2026-07-25, O-2): 회피 좌표 계산은 기존 위치에서 그대로 하고
        //   이동 명령 발행만 피커 X 진입 직전까지 이연한다. 두 축이 같은 순간에 기동되어
        //   FollowMoveAsync 포지션 오버라이드 추종이 실제로 성립한다.
        private bool _outputVisionRetreatDeferred;

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

        public void Abort()
        {
            try
            {
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
            _outputVisionRetreatDeferred = false;
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
            _outputVisionRetreatDeferred = false;
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
            _bottomPlaceCorrectionLimitSnapshot = correctionLimitMm;
            _placeCorrectionConfigCaptured = true;

            if (WaitBottomFinalBeforePlaceMoveAsync == null || GetValidatedBottomPlaceResult == null)
            {
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
                ", mechanicalOffsetY=" + _placeMechanicalOffsetYSnapshot.ToString("F3") + " - Start");

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

        private int ResolveOutputSide()
        {
            OutputStageResultRoutingMode routingMode = ResolveOutputStageResultRoutingMode();
            if (routingMode == OutputStageResultRoutingMode.ForceGoodStage)
            {
                _currentOutputSide = BinSide.Good;
                if (!IsInspectionFlowComplete(_currentDie))
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " GOOD 강제 배출 모드이므로 현재 Picker Bottom FINAL 확인 후 Good Stage 접근을 시작합니다. " +
                        "Side FINAL은 PickerZ 하강 게이트까지 병렬 수집합니다. " +
                        "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", pickerNo=" + _currentPickerNo + " - Check");
                }
                else if (_currentDie != null && _currentDie.Result == DieResult.NG)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " Die 결과가 NG이지만 설정에 따라 Good Stage 순번으로 Place합니다. " +
                        "die=" + _currentDie.DieId +
                        ", pickerNo=" + _currentPickerNo +
                        ", routingMode=" + routingMode +
                        ", forcedOutputSide=" + _currentOutputSide + " - Check");
                }

                CurrentStep = PickerPlaceStep.VerifyOutputStageReady;
                return 0;
            }

            if (routingMode != OutputStageResultRoutingMode.RouteByInspectionResult)
            {
                return Fail("PICKER-PLACE-ROUTING-MODE", "OutputStage",
                    "지원하지 않는 Die 결과 배출 모드입니다. mode=" + routingMode +
                    ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo + ".");
            }

            if (!IsInspectionFlowComplete(_currentDie))
            {
                return Fail("PICKER-PLACE-INSPECTION-INCOMPLETE", "Material",
                    "검사 결과별 출력 Stage 분기 전에 Bottom/Side 검사 흐름이 완료되지 않았습니다. " +
                    "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                    ", pickerNo=" + _currentPickerNo +
                    ", bottomDone=" + HasInspectionResult(_currentDie, "Bottom") +
                    ", side0Done=" + HasInspectionResult(_currentDie, "Side0") +
                    ", side90Done=" + HasInspectionResult(_currentDie, "Side90"));
            }

            if (_currentDie.Result == DieResult.Good)
                _currentOutputSide = BinSide.Good;
            else if (_currentDie.Result == DieResult.NG)
                _currentOutputSide = BinSide.Ng;
            else
                return Fail("PICKER-PLACE-DIE-RESULT-UNKNOWN", "Material",
                    "검사 결과별 출력 Stage 분기 전에 Die result가 확정되지 않았습니다. die=" +
                    _currentDie.DieId + ", pickerNo=" + _currentPickerNo + ".");

            WriteLog("PickerPlaceSequence",
                Name + " 검사 결과에 따라 출력 Stage를 결정했습니다. " +
                "die=" + _currentDie.DieId +
                ", pickerNo=" + _currentPickerNo +
                ", dieResult=" + _currentDie.Result +
                ", outputSide=" + _currentOutputSide + " - Ok");
            CurrentStep = PickerPlaceStep.VerifyOutputStageReady;
            return 0;
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
                        const string fullWaitDescription = "OutputStage 수령 완료 대기 중 보유 Die Picker 전체 Avoid";
                        int fullAvoidResult = await MovePickerToAvoidAfterPlaceFastAsync(
                            fullWaitDescription,
                            ct).ConfigureAwait(false);
                        if (fullAvoidResult != 0)
                            return fullAvoidResult;

                        _currentPlaceZSafeReturnCompleted = true;
                        ClearPendingContiRetreat();
                        ForceSafeYBeforeFirstPlaceMove = true;
                        KeepPickerYForwardDuringPlaceReadyWait = false;
                        ReleaseOutputPlaceArea();
                        ReleaseOutputStageArea();
                        ReleaseOutputFeederArea();
                        EndOutputPostPlaceInspectionBatch();

                        int workZoneReleaseResult = ReleaseParentOutputWorkZoneAfterSafeAvoidIfNeeded(
                            "OutputStage Full 교체 대기");
                        if (workZoneReleaseResult != 0)
                            return workZoneReleaseResult;

                        int publishResult = await PublishOutputStageExchangeReadyAfterSafeCompletionAsync(ct).ConfigureAwait(false);
                        if (publishResult != 0)
                            return publishResult;

                        safeWaitPositionPrepared = true;
                        fullAvoidPrepared = true;
                        WriteLog("PickerPlaceSequence",
                            Name + " OutputStage가 완료되어 다음 Place 대상이 열릴 때까지 보유 Die 상태로 Picker 전체 Avoid 복귀를 완료했습니다. " +
                            "side=" + Side + ", outputSide=" + _currentOutputSide +
                            ", pickerNo=" + _currentPickerNo + " - Ok");

                        Func<BinSide, int, string, CancellationToken, Task<int>> handoff =
                            WaitForOutputStageExchangeWithProcessHandoffAsync;
                        if (handoff == null)
                        {
                            return Fail("PICKER-PLACE-OUTPUT-HANDOFF-MISSING", Name,
                                "OutputStage Full 안전 대기 후 부모 Picker Process 리소스 양도 경로가 없습니다. " +
                                "side=" + Side + ", outputSide=" + _currentOutputSide +
                                ", pickerNo=" + _currentPickerNo);
                        }

                        int handoffResult = await handoff(
                            _currentOutputSide,
                            _currentPickerNo,
                            fullWaitDescription,
                            ct).ConfigureAwait(false);
                        if (handoffResult != 0)
                            return handoffResult;

                        // 부모가 Place phase와 Output work-zone을 다시 획득했으므로,
                        // 최종 Place 후 Avoid에서 후검사 전에 다시 정상 해제할 수 있도록 알림 상태를 복원한다.
                        _parentOutputWorkZoneReleaseNotified = false;
                        // 같은 Stage를 기다리던 다른 Picker가 먼저 새 Bin을 다시 Full로 만들 수 있다.
                        // handoff 1회가 끝날 때마다 새 교체 세대로 보고 다음 Full을 다시 양도할 수 있게 한다.
                        fullAvoidPrepared = false;
                        WriteLog("PickerPlaceSequence",
                            Name + " OutputStage 교체 후 부모 Picker Process/Place 작업영역 재점유가 완료되어 " +
                            "기존 보유 Die Place 준비 확인을 다시 시작합니다. side=" + Side +
                            ", outputSide=" + _currentOutputSide +
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
                if (CanSkipPickerMoveCommand(PickerAxis.PickerY, yAvoid))
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
                                           CanSkipOutputFeederMoveCommand(OutputFeeder, OutputFeeder.Recipe.AvoidPosition);
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
            {
                if (HasPendingContiRetreat())
                {
                    int retreatResult = await CompletePendingContiRetreatIfNeededAsync(
                        "Place 목표 계산 실패 후 이전 PickerZ 안전 복귀",
                        ct).ConfigureAwait(false);
                    if (retreatResult != 0)
                    {
                        WriteLog("PickerPlaceSequence",
                            Name + " Place 목표 계산 실패와 함께 이전 PickerZ 안전 복귀도 실패했습니다. " +
                            "targetResult=" + result +
                            ", retreatResult=" + retreatResult + " - Failed");
                        return retreatResult;
                    }
                }
                return result;
            }

            // O-2: 이전 픽커/중단에서 남은 이연 플래그를 좌표 재확정 전에 먼저 지운다.
            _outputVisionRetreatDeferred = false;

            double fullAvoid = OutputStage.Recipe.VisionX.AvoidPosition;
            double visionTarget = fullAvoid;
            string retreatDetail = "전체 Avoid 사용";
            // Conti 게이트: Auto + ContiSegmentedPlace면 부호 인지 최소 회피(Extra 포함).
            // 미충족이면 기존 경로(-0.1/1.0 — OutputVisionX는 사실상 전체 Avoid 폴백) 그대로 (동작 무변경).
            // planned는 배치 전체 목표 보관 필드가 없어 현재 아이템 _targetPickerX만 전달 —
            // 서비스가 피커 축 Actual/Command를 자동 포함하며, 다음 아이템 차례에 정확 좌표로 재계산된다.
            PickerPlaceMotionConfig retreatPlaceConfig = ResolvePlaceMotionConfig();
            bool useMinimalRetreat =
                Options != null && Options.RunMode == SequenceRunMode.Auto &&
                retreatPlaceConfig != null &&
                IsCoordinatedPlaceMotionMode(retreatPlaceConfig.MotionMode);
            string retreatMode = useMinimalRetreat ? "minimal" : "legacy";
            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                Context != null ? Context.Machine : null);
            if (service != null)
            {
                var planned = new Dictionary<SharedRailXAxis, IList<double>>();
                SharedRailXAxis pickerRailAxis = Side == PickerSequenceSide.Front
                    ? SharedRailXAxis.FrontPickerX
                    : SharedRailXAxis.RearPickerX;
                // 수정(2026-07-24): 현재 아이템 정확값 + 나머지 배치 아이템의 근사값으로 보강한다.
                // 근사 = 현재 PickerX − 현재 픽커 오프셋 + 해당 픽커 오프셋 (같은 빈 작업점 기준 피치 차).
                // 근사 실패 아이템은 생략 — 서비스가 피커 Actual/Command를 자동 포함해 안전.
                var plannedPickerTargets = new List<double> { _targetPickerX };
                double currentOffsetX;
                double currentOffsetY;
                string currentOffsetReason;
                if (_pickedPickerIndexes != null &&
                    TryResolveOutputVisionToPickerOffsets(_currentPickerIndex, out currentOffsetX, out currentOffsetY, out currentOffsetReason))
                {
                    for (int i = _pickerCursor + 1; i < _pickedPickerIndexes.Count; i++)
                    {
                        double itemOffsetX;
                        double itemOffsetY;
                        string itemOffsetReason;
                        if (TryResolveOutputVisionToPickerOffsets(_pickedPickerIndexes[i], out itemOffsetX, out itemOffsetY, out itemOffsetReason))
                            plannedPickerTargets.Add(_targetPickerX - currentOffsetX + itemOffsetX);
                    }
                }
                planned[pickerRailAxis] = plannedPickerTargets;

                double dynamicTarget;
                string dynamicDetail;
                bool resolved = useMinimalRetreat
                    ? service.TryResolveMinimalVisionRetreatTarget(
                        OutputStage.OutputCameraX,
                        fullAvoid,
                        planned,
                        service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0,
                        out dynamicTarget,
                        out dynamicDetail)
                    : service.TryResolveNearestVisionRetreatTarget(
                        OutputStage.OutputCameraX,
                        fullAvoid,
                        -0.1,
                        planned,
                        1.0,
                        out dynamicTarget,
                        out dynamicDetail);
                if (resolved)
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
                "mode=" + retreatMode +
                ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", pickerX=" + _targetPickerX.ToString("F6") +
                ", target=" + visionTarget.ToString("F6") +
                ", fullAvoid=" + fullAvoid.ToString("F6") +
                ", detail=" + retreatDetail + " - Check");

            // 기존 조건(R4 follow-entry): Auto Conti(minimal)면 회피 이동을 여기서 비동기 시작
            //   (Task 보관)하고 즉시 스텝 전이했다 — 피커 X 출발까지 남은 선행 모션들이 순차
            //   await되는 동안 짧은 최소 회피가 끝나 오버랩이 성립하지 않았다.
            // 현재 기준(사용자 지시 2026-07-25, O-2): 이동 명령을 발행하지 않고 이연 플래그만 세운다.
            //   실제 기동은 StartDeferredOutputVisionRetreatIfPendingAsync가 피커 X 진입 직전에
            //   수행하며, 이동 명령 경로(SharedRailX 중재 경유)와 join 지점은 기존과 동일하다.
            if (useMinimalRetreat)
            {
                await JoinOutputVisionRetreatMoveTaskAsync("이전 회피 Task 정리", ct).ConfigureAwait(false);
                _outputVisionRetreatTarget = visionTarget;
                _outputVisionRetreatDeferred = true;
                WriteLog("PickerPlaceSequence",
                    Name + " OutputVisionX 최소 회피 이동을 피커 X 진입 시점까지 이연합니다(팔로잉 오버랩). " +
                    "target=" + visionTarget.ToString("F6") + " - Check");
            }
            else
            {
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
            }

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
            {
                await JoinOutputVisionRetreatMoveTaskAsync("Feeder 안전 확보 실패 정리", ct).ConfigureAwait(false);
                return feederReady;
            }

            // OutputFeeder/OutputStage 로딩이 1순위다.
            // Picker는 OutputPlace/Stage/Feeder 리소스와 Feeder 안전 위치가 확보된 뒤에만
            // Output work area를 점유해야 Feeder 로딩 중 Picker owner로 인한 인터락 오판이 생기지 않는다.
            EnsurePickerWorkAreaReserved(PickerWorkZone.Output, "Place");

            BinStageAxis yAxis = _currentOutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;

            int prepareResult = PreparePlaceTargetValues();
            if (prepareResult != 0)
            {
                await JoinOutputVisionRetreatMoveTaskAsync("Place 목표 계산 실패 정리", ct).ConfigureAwait(false);
                return prepareResult;
            }
            Log.Write("PickerPlaceSequence", Name + " Place 대상 좌표 계산 완료. side=" + Side + ", step=" + CurrentStep);

            int result = await MoveOutputStageYPickerXAndPickerZToPlaceByModeAsync(yAxis, ct).ConfigureAwait(false);

            // O-2-G(방어적): 여기까지 이연이 소비되지 않았다면 회피가 아예 실행되지 않은 것이다.
            // 정상 흐름(follow 기동 또는 일반 이동 전 방어)에서는 발생하지 않아야 하므로 경고를
            // 남기고 지금이라도 기동해 아래 join이 완료를 확인하게 한다.
            if (_outputVisionRetreatDeferred)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputVisionX 이연 최소 회피가 피커 X 진입 완료 시점까지 소비되지 않아 지금 마무리합니다. " +
                    "target=" + _outputVisionRetreatTarget.ToString("F6") +
                    ", side=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo + " - Check");
                await StartDeferredOutputVisionRetreatIfPendingAsync(ct).ConfigureAwait(false);
            }

            // R4(follow-entry): 비동기 비전 회피 Task는 피커 X 진입 처리 완료 전에 반드시 join.
            // 진입 실패 시에도 drain(observe)하고, 진입 성공 후 회피 실패면 시퀀스 Fail.
            int retreatJoinResult = await JoinOutputVisionRetreatMoveTaskAsync("피커 X 진입 완료", ct).ConfigureAwait(false);
            if (result != 0)
                return result;
            if (retreatJoinResult != 0)
            {
                return Fail("PICKER-PLACE-VISION-X-AVOID", "OutputStage",
                    "OutputVisionX 비동기 최소 회피 이동이 실패했습니다. side=" + _currentOutputSide +
                    ", target=" + _outputVisionRetreatTarget.ToString("F6") +
                    ", result=" + retreatJoinResult + ", " + OutputStage.DescribeStageLoadMoveState(_currentOutputSide));
            }

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

                if (!CanSkipOutputFeederMoveCommand(feeder, feeder.Recipe.AvoidPosition))
                {
                    int moveResult = await feeder.MoveToFeederAvoidPosition(Options.FineMove).ConfigureAwait(false);
                    if (moveResult != 0)
                        return Fail("PICKER-PLACE-FEEDER-Y-AVOID", feeder.Name,
                            "Place 전 OutputFeederY Avoid 이동 명령 실패. result=" + moveResult +
                            ", side=" + _currentOutputSide + ", " + feeder.DescribeBinFeederYMoveDoneState() +
                            feeder.DescribeBinFeederYLastMotionFailure());

                    // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
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
            bool autoRun = Options != null && Options.RunMode == SequenceRunMode.Auto;
            if (autoRun && _currentBottomPlaceResult == null)
            {
                return Fail("PICKER-PLACE-BOTTOM-CORRECTION-MISSING", "Vision",
                    "Bottom FINAL 보정값을 확보하기 전에 Place 목표 계산이 요청되었습니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + dieId + ".");
            }

            // FINAL RESULT의 bottom_item_offset_x/y만 Place 보정에 정확히 한 번 사용합니다.
            // 기존 MRESULT OffsetY는 계속 Side Vision 전용이며 이 계산에 혼용하지 않습니다.
            VisionOffset bottomOffset = new VisionOffset
            {
                X = _currentBottomPlaceResult != null ? _currentBottomPlaceResult.BottomItemOffsetX : 0.0,
                Y = _currentBottomPlaceResult != null ? _currentBottomPlaceResult.BottomItemOffsetY : 0.0,
                R = 0.0,
                IsValid = true
            };
            string bottomOffsetReason = _currentBottomPlaceResult != null
                ? "BottomFinalItemOffsetAppliedOnce"
                : "ManualPlaceWithoutDeferredBottomCorrection";
            bool useBottomFinalItemOffsetYAsSoleColletYCorrection =
                autoRun && _currentBottomPlaceResult != null;

            double outputStageBaseY = _currentOutputSide == BinSide.Ng
                ? OutputStage.Recipe.NGStageY.ProcessPosition
                : OutputStage.Recipe.GoodStageY.ProcessPosition;
            PickerPlaceMotionConfig placeConfig = ResolvePlaceMotionConfig();
            double placeMechanicalOffsetX = _placeCorrectionConfigCaptured
                ? _placeMechanicalOffsetXSnapshot
                : placeConfig.GetMechanicalOffsetX(_currentPickerIndex);
            double placeMechanicalOffsetY = _placeCorrectionConfigCaptured
                ? _placeMechanicalOffsetYSnapshot
                : placeConfig.GetMechanicalOffsetY(_currentPickerIndex);
            double placeCorrectionX = -bottomOffset.X + placeMechanicalOffsetX;
            double placeCorrectionY = -bottomOffset.Y + placeMechanicalOffsetY;
            double placeCorrectionLimitMm = _placeCorrectionConfigCaptured
                ? _bottomPlaceCorrectionLimitSnapshot
                : placeConfig.BottomPlaceCorrectionLimitMm;
            if (Math.Abs(placeCorrectionX) > placeCorrectionLimitMm ||
                Math.Abs(placeCorrectionY) > placeCorrectionLimitMm)
            {
                return Fail("PICKER-PLACE-CORRECTION-RANGE", "Vision",
                    "Bottom FINAL과 기구 보정을 합산한 Place 보정값이 허용 범위를 벗어나 이동을 차단합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + dieId +
                    ", correctionX=-bottomX+mechanicalX=" + placeCorrectionX.ToString("F6") +
                    ", correctionY=-bottomY+mechanicalY=" + placeCorrectionY.ToString("F6") +
                    ", allowedAbsMaxMm=" + placeCorrectionLimitMm.ToString("F3") + ".");
            }

            // Place 런타임 보정: Enable일 때만 필터 상태를 적용하고, Disable이면 0을 전달한다
            // (Disable이어도 필터 학습·저장은 Bin 후검사 경로에서 계속된다).
            // 부호 반영(X:-, Y:+, T:-)은 DieCoordinateTransformService.CalculatePlaceTarget이 담당한다.
            bool placeRuntimeEnabled = PlaceRuntimeOffsetService.IsEnabled;
            double placeRuntimeOffsetX = 0.0;
            double placeRuntimeOffsetY = 0.0;
            double placeRuntimeOffsetT = 0.0;
            if (placeRuntimeEnabled)
            {
                PlaceRuntimeOffsetService.GetOffset(
                    Side,
                    _currentPickerNo,
                    out placeRuntimeOffsetX,
                    out placeRuntimeOffsetY,
                    out placeRuntimeOffsetT);
            }

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
                bottomOffset.R,
                placeRuntimeOffsetX,
                placeRuntimeOffsetY,
                placeRuntimeOffsetT,
                placeMechanicalOffsetX,
                placeMechanicalOffsetY,
                useBottomFinalItemOffsetYAsSoleColletYCorrection);

            _targetOutputStageY = coordinate.OutputStageY;
            _targetPickerX = coordinate.PickerX;
            _targetPickerY = coordinate.PickerY;
            _targetPickerT = coordinate.PickerT;
            // 공정 Place Z 유일한 생성점: Collet AF Z Offset을 여기서 1회만 가산한다(Conti 노드/하강/검증에 자동 전파).
            // 한계 초과는 fail-closed(알람 중단) — 확정 정책. PlaceZ 캘리브레이션은 이 함수를 지나지 않으므로 오염 없음.
            string placeAfZOffsetFailReason;
            double placeColletAfZOffset = ResolveColletAfZOffset(_currentPickerIndex, out placeAfZOffsetFailReason);
            if (placeAfZOffsetFailReason != null)
                return Fail("PICKER-PLACE-AF-ZOFFSET-LIMIT", Name, placeAfZOffsetFailReason);

            double placeZOverDrive = ResolvePlaceZOverDrive();
            _targetPickerZ = coordinate.PickerZ + placeZOverDrive + placeColletAfZOffset;
            _targetFormula = coordinate.Formula +
                " / pickerZFinal = pickerZTeaching(" + coordinate.PickerZ.ToString("F6") +
                ") + placeZOverDrive(" + placeZOverDrive.ToString("F6") +
                ") + colletAfZOffset(" + placeColletAfZOffset.ToString("F6") +
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
                ", colletAfZOffset=" + placeColletAfZOffset.ToString("F6") +
                ", outputStageBaseY=" + outputStageBaseY +
                ", receiveTargetX=" + (_receiveTarget != null ? _receiveTarget.TargetX.ToString() : "-") +
                ", receiveTargetY=" + (_receiveTarget != null ? _receiveTarget.TargetY.ToString() : "-") +
                ", outputVisionToPickerOffsetX=" + _outputVisionToPickerX +
                ", outputVisionToPickerOffsetY=" + _outputVisionToPickerY +
                ", outputVisionToPickerYAppliedToOutputStageY=" +
                (!useBottomFinalItemOffsetYAsSoleColletYCorrection) +
                ", bottomOffsetX=" + bottomOffset.X +
                ", bottomOffsetY=" + bottomOffset.Y +
                ", bottomOffsetT=" + bottomOffset.R +
                ", bottomOffsetMode=" + bottomOffsetReason +
                ", placeRuntimeEnabled=" + placeRuntimeEnabled +
                ", placeRuntimeOffsetX=" + placeRuntimeOffsetX.ToString("F6") +
                ", placeRuntimeOffsetY=" + placeRuntimeOffsetY.ToString("F6") +
                ", placeRuntimeOffsetT=" + placeRuntimeOffsetT.ToString("F6") +
                ", placeMechanicalOffsetX=" + placeMechanicalOffsetX.ToString("F3") +
                ", placeMechanicalOffsetXAppliedToPickerX=True" +
                ", placeMechanicalOffsetY=" + placeMechanicalOffsetY.ToString("F3") +
                ", placeMechanicalOffsetYAppliedToOutputStageY=True" +
                ", placeMechanicalOffsetYAppliedToPickerY=False" +
                ", combinedPlaceCorrectionX=" + placeCorrectionX.ToString("F6") +
                ", combinedPlaceCorrectionY=" + placeCorrectionY.ToString("F6") +
                ", combinedPlaceCorrectionLimitMm=" + placeCorrectionLimitMm.ToString("F3") +
                ", formula=" + _targetFormula + " - Ok");
            return 0;
        }

        private async Task<int> MovePickerXYAndTToPlaceAsync(CancellationToken ct)
        {
            var targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = _targetPickerX;
            targets[PickerAxis.PickerY] = _targetPickerY;

            AddLoadedPickerTPlaceTargets(targets);

            // O-2-F: 일반 이동 전 이연 회피 완료 보장(스텝 직접 진입 경로).
            int deferredJoin = await JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(ct).ConfigureAwait(false);
            if (deferredJoin != 0)
                return deferredJoin;

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

            // R4(follow-entry): 비전 회피가 진행/이연 중이면 X만 follow로 분리(T 병렬 → Y 전진 구조 유지),
            // 정지 상태(2번째 픽커 포함)면 기존 X/T→Y 이동 그대로.
            // O-2-F: follow를 타지 않을 때만, 그리고 어떤 이동 Task보다 먼저 이연 회피를 완료한다
            //        (StageY Task 생성 전에 판정해야 조기 반환 시 고아 Task가 생기지 않는다.
            //         StageY 이동은 게이트 입력에 영향을 주지 않으므로 판정 시점 이동은 무해하다).
            bool useVisionFollowEntry = ShouldFollowOutputVisionRetreatForPickerEntry();
            if (!useVisionFollowEntry)
            {
                int deferredJoin = await JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(ct).ConfigureAwait(false);
                if (deferredJoin != 0)
                    return deferredJoin;
            }

            Task<int> stageYMove = MoveOutputStageAxisAndVerifyAsync(
                yAxis,
                _targetOutputStageY,
                "output stage receive Y",
                ct,
                BuildOutputStagePlaceMoveTargetName("ReceiveY"));
            Task<int> pickerMove = useVisionFollowEntry
                ? MovePlacePickerXTThenYWithVisionFollowAsync(
                    pickerTargets,
                    BuildPlaceMoveTargetName(),
                    ct)
                : MovePickerXTThenYAndVerifyAsync(
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

            // O-1-B: 이 경로는 ForceSafeYBeforeFirstPlaceMove(재시작 첫 접근)로만 진입하는 안전 우선
            //        경로다. 오버랩보다 확정된 순서(X/T 완료 후 Y 전진)가 우선이므로 follow를 심지
            //        않는다. 대신 O-2-F 방어만 넣어 일반 이동 전에 이연 회피가 반드시 완료되게 한다.
            int deferredJoin = await JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(ct).ConfigureAwait(false);
            if (deferredJoin != 0)
                return deferredJoin;

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

            if (CanSkipPickerMoveCommand(PickerAxis.PickerY, _targetPickerY))
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

        // R4(follow-entry): 보관된 비동기 회피 Task를 join하고 필드를 비운다. 없으면 0.
        private async Task<int> JoinOutputVisionRetreatMoveTaskAsync(string context, CancellationToken ct)
        {
            Task<int> moveTask = _outputVisionRetreatMoveTask;
            if (moveTask == null)
                return 0;

            _outputVisionRetreatMoveTask = null;
            try
            {
                int result = await moveTask.ConfigureAwait(false);
                if (result != 0)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " OutputVisionX 비동기 최소 회피 이동이 실패로 종료되었습니다. " +
                        "context=" + (context ?? string.Empty) +
                        ", result=" + result + " - Check");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputVisionX 비동기 최소 회피 이동이 취소 상태로 정리되었습니다. " +
                    "context=" + (context ?? string.Empty) + " - Check");
                ct.ThrowIfCancellationRequested();
                return -1;
            }
            catch (Exception ex)
            {
                WriteLog("PickerPlaceSequence",
                    Name + " OutputVisionX 비동기 최소 회피 이동 정리 중 예외가 발생했습니다. " +
                    "context=" + (context ?? string.Empty) +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
        }

        // R4(follow-entry): Abort 등 동기 경로에서 회피 Task를 관찰(observe)만 하고 흘려보낸다.
        private void ObserveOutputVisionRetreatMoveTaskOnAbort()
        {
            _outputVisionRetreatDeferred = false;
            Task<int> moveTask = _outputVisionRetreatMoveTask;
            _outputVisionRetreatMoveTask = null;
            if (moveTask == null)
                return;

            moveTask.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                        t.Exception.Flatten();
                },
                TaskScheduler.Default);
        }

        // O-2: 미뤄둔 최소 회피 이동을 지금 비동기 시작한다. 피커 X 진입(팔로잉/일반) 직전에 호출해
        // "회피 명령 발행 → 즉시 피커 X 기동" 순서를 한 지점에서 보장한다.
        // (MoveOutputStageAxisAndVerifyAsync는 인포지션이면 자체적으로 즉시 완료되므로 별도
        //  인포지션 단락 검사를 두지 않는다 — 기존 비동기 시작 호출과 인자를 동일하게 유지한다.)
        private async Task StartDeferredOutputVisionRetreatIfPendingAsync(CancellationToken ct)
        {
            if (!_outputVisionRetreatDeferred)
                return;

            _outputVisionRetreatDeferred = false;
            await JoinOutputVisionRetreatMoveTaskAsync("이연 회피 시작 전 정리", ct).ConfigureAwait(false);

            _outputVisionRetreatMoveTask = MoveOutputStageAxisAndVerifyAsync(
                BinStageAxis.VisionX,
                _outputVisionRetreatTarget,
                "OutputVisionX 최소 회피 위치 이동",
                ct);
            WriteLog("PickerPlaceSequence",
                Name + " OutputVisionX 이연 최소 회피 이동을 비동기 시작했습니다(피커 진입 팔로잉 오버랩). " +
                "target=" + _outputVisionRetreatTarget.ToString("F6") + " - Start");
        }

        // O-2-F: follow를 타지 않는 일반 이동 분기 공통 방어 — 이연이 남아 있으면 회피를 기동하고
        // 완료(join)까지 확인한다. 이연 상태에서 비전이 아직 촬영/검사 위치에 있는데 피커 X가
        // 일반 이동으로 Output 존에 진입하면 인터락(-11)에 걸리기 때문이다. 이연이 없으면 0(무동작).
        private async Task<int> JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(CancellationToken ct)
        {
            if (!_outputVisionRetreatDeferred)
                return 0;

            await StartDeferredOutputVisionRetreatIfPendingAsync(ct).ConfigureAwait(false);
            return await JoinOutputVisionRetreatMoveTaskAsync("일반 이동 전 이연 회피 완료", ct).ConfigureAwait(false);
        }

        // 기존 조건(R4 follow-entry): 이미 시작된 회피 Task가 살아 있고 비전이 이동 중(IsMoving)일 때만 follow.
        // 현재 기준(사용자 지시 2026-07-25, O-2-D): 회피가 이연 대기 중이면 아직 출발 전이라
        //   IsMoving=false이므로 이연 플래그를 follow 조건에 포함한다. 이미 시작된 Task 기준의
        //   기존 조건은 그대로 유지한다(비전이 정지했고 이연도 없으면 기존 일반 이동).
        private bool ShouldFollowOutputVisionRetreatForPickerEntry()
        {
            return OutputStage != null &&
                   OutputStage.OutputCameraX != null &&
                   (_outputVisionRetreatDeferred ||
                    (_outputVisionRetreatMoveTask != null && OutputStage.OutputCameraX.IsMoving));
        }

        // R4(follow-entry): MovePickerXTThenYAndVerifyAsync 구조를 미러 — X만 follow(+R6 폴백)로
        // 분리하고 T는 병렬 단독 이동, X/T 완료 후 PickerY 전진(스킵 판정 동일).
        private async Task<int> MovePlacePickerXTThenYWithVisionFollowAsync(
            IDictionary<PickerAxis, double> targets,
            string targetName,
            CancellationToken ct)
        {
            if (targets == null || targets.Count == 0)
                return 0;

            double pickerYTarget;
            bool hasPickerY = targets.TryGetValue(PickerAxis.PickerY, out pickerYTarget);

            var tTargets = new Dictionary<PickerAxis, double>();
            foreach (KeyValuePair<PickerAxis, double> pair in targets)
            {
                if (pair.Key == PickerAxis.PickerX || pair.Key == PickerAxis.PickerY)
                    continue;

                tTargets[pair.Key] = pair.Value;
            }

            Task<int> pickerXEntryMove = MovePlacePickerXEntryByVisionFollowOrFallbackAsync(
                "place picker X 팔로잉 진입",
                targetName,
                ct);
            Task<int> pickerTMove = MovePickerAxesAndVerifyAsync(
                tTargets,
                "place picker T",
                ct,
                targetName);

            int[] results = await Task.WhenAll(pickerXEntryMove, pickerTMove).ConfigureAwait(false);
            int xtResult = results[0] != 0 ? results[0] : results[1];
            if (xtResult != 0)
                return xtResult;

            if (!hasPickerY || CanSkipPickerMoveCommand(PickerAxis.PickerY, pickerYTarget))
                return 0;

            WriteLog("PickerMove",
                Name + " place picker X(follow)/T 이동 완료 후 PickerY 전진을 시작합니다. " +
                "targetY=" + pickerYTarget +
                ", targetName=" + (targetName ?? "-") +
                " - Check");

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerY,
                pickerYTarget,
                "place picker Y",
                ct,
                targetName).ConfigureAwait(false);
        }

        // R6(follow-entry): follow 실패 시 — 함수가 피커 정지를 보장하므로 비전 회피 Task를
        // join(observe)한 뒤 기존 일반 이동(공유레일 대기 게이트 포함 순차 경로)으로 1회 재시도한다.
        private async Task<int> MovePlacePickerXEntryByVisionFollowOrFallbackAsync(
            string description,
            string targetName,
            CancellationToken ct)
        {
            // O-2-E: 이연해 둔 회피를 여기서 기동한다 — 명령 발행 직후 곧바로 FollowMoveAsync가
            // 시작되므로 두 축의 오버랩이 실제로 성립한다. 아래 R6 폴백(join + 일반 이동)은
            // 이 시점에 Task가 이미 시작돼 있어 기존대로 동작한다.
            await StartDeferredOutputVisionRetreatIfPendingAsync(ct).ConfigureAwait(false);

            int followResult = await TryFollowPickerXBehindOutputVisionRetreatAsync(ct).ConfigureAwait(false);
            if (followResult == 0)
                return 0;

            int retreatJoin = await JoinOutputVisionRetreatMoveTaskAsync(
                description + " follow 폴백",
                ct).ConfigureAwait(false);
            WriteLog("PickerPlaceSequence",
                Name + " " + description + " 팔로잉 진입이 실패해 기존 일반 이동으로 재시도합니다. " +
                "followResult=" + followResult +
                ", retreatJoin=" + retreatJoin + " - Check");

            return await MovePickerAxisAndVerifyAsync(
                PickerAxis.PickerX,
                _targetPickerX,
                description + " (follow 폴백)",
                ct,
                targetName).ConfigureAwait(false);
        }

        // R4/R5(follow-entry): 피커X(후행)가 회피 중인 OutputVisionX(선행)를 추종 진입한다.
        // homeGap/safetyGap/direction/timeout 전부 SharedRailX 설정에서 런타임 조회(하드코딩 금지).
        // 안전 근거: 팔로잉 유지 간격(safetyGap=SafetyDistance+OutputExtra, 기본 50) > 인터락 요구
        // (SafetyDistance, 기본 10)이므로 정상 추종 중 인터락 거부는 없다. 그럼에도 -11이면 R6 폴백.
        // 인터락 통과 체인: FollowMoveAsync 내부 MoveAbsoluteAsync→BaseAxis.VerifyMotionGuard→
        // MotionGuardRuntime.VerifyAxisMove(SharedRailX 포함) / TryOverridePosition→
        // MotionGuardRuntime.VerifyAxisTeachingMove — 우회 API 미사용.
        private async Task<int> TryFollowPickerXBehindOutputVisionRetreatAsync(CancellationToken ct)
        {
            BaseAxis pickerX = GetPickerAxis(PickerAxis.PickerX);
            AjinAxis followPickerX = pickerX as AjinAxis;
            BaseAxis visionX = OutputStage != null ? OutputStage.OutputCameraX : null;
            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                Context != null ? Context.Machine : null);
            if (followPickerX == null || visionX == null || service == null)
                return -1;

            int direction;
            double homeGap;
            double safetyGap;
            string gapDetail;
            if (!service.TryGetFollowGapParameters(
                pickerX,
                visionX,
                service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0,
                out direction,
                out homeGap,
                out safetyGap,
                out gapDetail))
            {
                WriteLog("PickerPlaceSequence",
                    Name + " Place 피커X 팔로잉 파라미터 조회에 실패해 일반 이동으로 진행합니다. " +
                    "detail=" + gapDetail + " - Check");
                return -1;
            }

            int timeoutMs = service.Config != null ? service.Config.VisionFollowEntryTimeoutMs : 15000;
            // 현재 기준: follow의 명령/오버라이드 경로는 축 레이어 자동 스케일이 없으므로 여기서 1회 스케일.
            double trailingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                pickerX.Config != null ? pickerX.Config.DefaultVelocity : 0.0);
            double trailingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                pickerX.Config != null ? pickerX.Config.Acceleration : 0.0);
            double trailingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                pickerX.Config != null ? pickerX.Config.Deceleration : 0.0);
            double leadingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                visionX.Config != null ? visionX.Config.DefaultVelocity : 0.0);
            double leadingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                visionX.Config != null ? visionX.Config.Acceleration : 0.0);
            double leadingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                visionX.Config != null ? visionX.Config.Deceleration : 0.0);

            WriteLog("PickerPlaceSequence",
                Name + " Place 피커X 팔로잉 진입을 시작합니다. leading=" + visionX.Name +
                ", visionTarget=" + _outputVisionRetreatTarget.ToString("F6") +
                ", pickerTarget=" + _targetPickerX.ToString("F6") +
                ", " + gapDetail +
                ", timeoutMs=" + timeoutMs + " - Start");

            return await followPickerX.FollowMoveAsync(
                visionX,
                _outputVisionRetreatTarget,
                leadingVelocity,
                leadingAcceleration,
                leadingDeceleration,
                _targetPickerX,
                trailingVelocity,
                trailingAcceleration,
                trailingDeceleration,
                direction,
                safetyGap,
                homeGap,
                timeoutMs,
                ct).ConfigureAwait(false);
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

            double stageYTarget = _targetOutputStageY;
            double pickerXTarget = _targetPickerX;
            if (stageY != null)
                stageY.UpdateStatus();
            if (pickerX != null)
                pickerX.UpdateStatus();

            if (stageY == null || pickerX == null ||
                !IsFinitePosition(stageYTarget) || !IsFinitePosition(pickerXTarget) ||
                !IsFinitePosition(stageY.ActualPosition) || !IsFinitePosition(stageY.CommandPosition) ||
                !IsFinitePosition(pickerX.ActualPosition) || !IsFinitePosition(pickerX.CommandPosition) ||
                !stageY.IsServoOn || stageY.IsAlarm || stageY.IsMoving ||
                !pickerX.IsServoOn || pickerX.IsAlarm || pickerX.IsMoving)
            {
                return Fail("PICKER-PLACE-CONTI-XY-START", Name,
                    "Place ContiNode XY 이동 시작 전 축 상태가 비정상입니다. " +
                    "stageY={" + BuildContiPlaceAxisDecision(stageY, stageYTarget, true) + "}" +
                    ", pickerX={" + BuildContiPlaceAxisDecision(pickerX, pickerXTarget, true) + "}");
            }

            bool stageYForceMove = RequiresContiPlaceForceMove(stageY, stageYTarget);
            bool pickerXForceMove = RequiresContiPlaceForceMove(pickerX, pickerXTarget);
            double stageYStart = stageY != null ? stageY.ActualPosition : stageYTarget;
            double pickerXStart = pickerX != null ? pickerX.ActualPosition : pickerXTarget;
            PickerAxis currentPickerZAxis = GetPickerZAxis(_currentPickerIndex);
            string placeTargetName = BuildPlaceMoveTargetName();

            WriteLog("PickerPlaceSequence",
                Name + " Place ContiNode XY 이동 명령 판정. " +
                "die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                ", pickerNo=" + _currentPickerNo +
                ", outputSide=" + _currentOutputSide +
                ", stageY={" + BuildContiPlaceAxisDecision(stageY, stageYTarget, stageYForceMove) + "}" +
                ", pickerX={" + BuildContiPlaceAxisDecision(pickerX, pickerXTarget, pickerXForceMove) + "}" +
                " - Check");

            // O-1-A(2026-07-25): 기존 조건 — Conti 본경로(배치 2번째 Place 이후)에는 follow 분기가
            //   아예 없어 Conti 정상 운전에서 팔로잉이 전혀 걸리지 않았다(XYT 폴백 경로에만 존재).
            //   현재 기준 — 비전 회피가 이연/진행 중이면 X만 follow로 진입한다(Input Conti 경로 미러).
            //   forceMove가 필요한 경우는 follow가 forceMove 의미를 보장하지 못하므로 기존 일반 이동.
            // O-2-F: follow를 쓰지 않는 쪽은 이동 Task 생성 전에 이연 회피를 완료한다.
            bool useContiVisionFollowEntry = !pickerXForceMove && ShouldFollowOutputVisionRetreatForPickerEntry();
            if (!useContiVisionFollowEntry)
            {
                int deferredJoin = await JoinDeferredOutputVisionRetreatBeforePlainMoveAsync(ct).ConfigureAwait(false);
                if (deferredJoin != 0)
                {
                    _targetPickerZ = originalPickerZTarget;
                    return deferredJoin;
                }
            }

            Task<int> stageYMove = MoveOutputStageAxisAndVerifyAsync(
                yAxis,
                stageYTarget,
                "Place ContiNode OutputStageY 비동기 이동",
                ct,
                BuildOutputStagePlaceMoveTargetName("AsyncReceiveY"),
                stageYForceMove);
            Task<int> pickerXMove = useContiVisionFollowEntry
                ? MovePlacePickerXEntryByVisionFollowOrFallbackAsync(
                    "Place ContiNode PickerX 팔로잉 진입",
                    placeTargetName,
                    ct)
                : MovePickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    pickerXTarget,
                    "Place ContiNode PickerX 비동기 이동",
                    ct,
                    placeTargetName,
                    pickerXForceMove);
            Task<int> pickerZPrePlaceMove = MovePickerZPlaceAfterContiProgressAsync(
                stageY,
                pickerX,
                stageYStart,
                stageYTarget,
                pickerXStart,
                pickerXTarget,
                stageYMove,
                pickerXMove,
                currentPickerZAxis,
                prePlacePickerZ,
                placeConfig,
                stageYForceMove,
                pickerXForceMove,
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
                stageYTarget,
                pickerXTarget,
                null,
                previousPickerZAvoid,
                currentPickerZAxis,
                Math.Max(placeConfig.ContiTimeoutMs, ResolveTimeout()),
                ct).ConfigureAwait(false);
            if (finalWait != 0)
                return finalWait;

            // StageY와 PickerX 최종 도착 확인 후에만 PickerZ를 최종 Place 접촉 위치로 이동합니다.
            int inspectionGateResult = await EnsureInspectionResultsReadyBeforePlaceDownAsync(ct).ConfigureAwait(false);
            if (inspectionGateResult != 0)
            {
                _pickerZPlacedByContiSegmentedPlace = false;
                return inspectionGateResult;
            }

            _targetPickerZ = finalPickerZ;
            int finalPlaceZResult = await MovePickerAxisAndVerifyAsync(
                currentPickerZAxis,
                finalPickerZ,
                "Place ContiNode PickerZ PrePlace 후 최종 Place 하강",
                ct,
                BuildPickerTargetName("DiePlacePosition", _currentPickerIndex)).ConfigureAwait(false);
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
                ", stageYTarget=" + stageYTarget.ToString("F3") +
                ", pickerXStart=" + pickerXStart.ToString("F3") +
                ", pickerXTarget=" + pickerXTarget.ToString("F3") +
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
            double stageYTarget,
            double pickerXTarget,
            PickerAxis? previousPickerZAxis,
            double previousPickerZAvoid,
            PickerAxis pickerZAxis,
            int timeoutMs,
            CancellationToken ct)
        {
            int stageYWait = await OutputStage.WaitStageAxisMoveDoneInPosition(
                yAxis,
                stageYTarget,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (stageYWait != 0)
            {
                return Fail("PICKER-PLACE-CONTI-STAGE-Y", "OutputStage",
                    "Place ContiNode 이동 후 OutputStageY 최종 위치 대기 실패. waitCode=" + stageYWait +
                    ". " + OutputStage.BuildStageAxisState(yAxis, stageYTarget));
            }

            int pickerXWait = await WaitPickerAxisMoveDoneAsync(
                PickerAxis.PickerX,
                pickerXTarget,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerXWait != 0)
            {
                return Fail("PICKER-PLACE-CONTI-PICKER-X", Name,
                    "Place ContiNode 이동 후 PickerX 최종 위치 대기 실패. waitCode=" + pickerXWait +
                    ". " + BuildPickerAxisState(PickerAxis.PickerX, pickerXTarget));
            }

            if (previousPickerZAxis.HasValue)
            {
                int previousPickerZWait = await WaitPickerAxisMoveDoneAsync(
                    previousPickerZAxis.Value,
                    previousPickerZAvoid,
                    timeoutMs,
                    ct).ConfigureAwait(false);
                if (previousPickerZWait != 0)
                {
                    return Fail("PICKER-PLACE-CONTI-PREV-PICKER-Z", Name,
                        "Place ContiNode 이동 후 이전 PickerZ Avoid 최종 위치 대기 실패. pickerNo=" + _pendingContiRetreatPickerNo +
                        ", waitCode=" + previousPickerZWait +
                        ". " + BuildPickerAxisState(previousPickerZAxis.Value, previousPickerZAvoid));
                }
            }

            int pickerZWait = await WaitPickerAxisMoveDoneAsync(
                pickerZAxis,
                _targetPickerZ,
                timeoutMs,
                ct).ConfigureAwait(false);
            if (pickerZWait != 0)
            {
                return Fail("PICKER-PLACE-CONTI-PICKER-Z", Name,
                    "Place ContiNode 이동 후 PickerZ 최종 위치 대기 실패. pickerNo=" + _currentPickerNo +
                    ", waitCode=" + pickerZWait +
                    ". " + BuildPickerAxisState(pickerZAxis, _targetPickerZ));
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

        private bool TryResolveOutputSide(DieMaterial die, out BinSide side)
        {
            side = BinSide.Good;
            if (die == null)
                return false;

            OutputStageResultRoutingMode routingMode = ResolveOutputStageResultRoutingMode();
            if (routingMode == OutputStageResultRoutingMode.ForceGoodStage)
            {
                side = BinSide.Good;
                return true;
            }

            if (routingMode != OutputStageResultRoutingMode.RouteByInspectionResult ||
                !IsInspectionFlowComplete(die))
            {
                return false;
            }

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

        private static bool RequiresContiPlaceForceMove(BaseAxis axis, double target)
        {
            if (axis == null)
                return true;

            axis.UpdateStatus();
            return !IsContiPlaceAxisSkipEligible(axis, target);
        }

        private static bool IsContiPlaceAxisSkipEligible(BaseAxis axis, double target)
        {
            if (!IsContiPlaceAxisStrongAtTarget(axis, target))
                return false;

            return IsSameF3(axis.ActualPosition, target) &&
                   IsSameF3(axis.CommandPosition, target);
        }

        private static bool IsContiPlaceAxisStrongAtTarget(BaseAxis axis, double target)
        {
            if (axis == null ||
                !IsFinitePosition(target) ||
                !IsFinitePosition(axis.ActualPosition) ||
                !IsFinitePosition(axis.CommandPosition))
            {
                return false;
            }

            double tolerance = ResolveContiPlaceAxisTolerance(axis);
            return axis.IsServoOn &&
                   !axis.IsAlarm &&
                   !axis.IsMoving &&
                   axis.IsInPosition &&
                   Math.Abs(axis.ActualPosition - target) <= tolerance &&
                   Math.Abs(axis.CommandPosition - target) <= tolerance;
        }

        private static bool IsSameF3(double left, double right)
        {
            if (!IsFinitePosition(left) || !IsFinitePosition(right))
                return false;

            return Math.Round(left, 3, MidpointRounding.AwayFromZero) ==
                   Math.Round(right, 3, MidpointRounding.AwayFromZero);
        }

        private static bool IsFinitePosition(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double ResolveContiPlaceAxisTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.001;
        }

        private static string BuildContiPlaceAxisDecision(BaseAxis axis, double target, bool forceMove)
        {
            if (axis == null)
                return "axis=null, target=" + target.ToString("F6") + ", forceMove=" + forceMove;

            double tolerance = ResolveContiPlaceAxisTolerance(axis);
            return "name=" + axis.Name +
                   ", actual=" + axis.ActualPosition.ToString("F6") +
                   ", command=" + axis.CommandPosition.ToString("F6") +
                   ", target=" + target.ToString("F6") +
                   ", actualF3=" + axis.ActualPosition.ToString("F3") +
                   ", commandF3=" + axis.CommandPosition.ToString("F3") +
                   ", targetF3=" + target.ToString("F3") +
                   ", actualF3Match=" + IsSameF3(axis.ActualPosition, target) +
                   ", commandF3Match=" + IsSameF3(axis.CommandPosition, target) +
                   ", tolerance=" + tolerance.ToString("F6") +
                   ", servo=" + axis.IsServoOn +
                   ", alarm=" + axis.IsAlarm +
                   ", moving=" + axis.IsMoving +
                   ", inPosition=" + axis.IsInPosition +
                   ", forceMove=" + forceMove;
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

        private static bool IsContiPlaceAxisThresholdReady(
            BaseAxis axis,
            double start,
            double target,
            double trigger,
            bool triggerReached,
            bool moveSucceeded)
        {
            if (!triggerReached ||
                axis == null ||
                !axis.IsServoOn ||
                axis.IsAlarm ||
                !IsFinitePosition(axis.ActualPosition) ||
                !IsFinitePosition(axis.CommandPosition) ||
                !IsFinitePosition(target))
            {
                return false;
            }

            // ratio=0 또는 실질 이동량이 없는 경우에는 명령 완료 전 즉시 Z 하강하지 않습니다.
            if (Math.Abs(trigger - start) <= 0.000001)
                return false;

            // 이 Task가 바로 위에서 해당 target으로 발행한 이동을 소유합니다.
            // 실장비 CommandPosition은 이동 중 궤적 위치이므로 target 일치 조건은 최종 완료 판정에서만 적용합니다.
            return axis.IsMoving ||
                   (moveSucceeded && IsContiPlaceAxisStrongAtTarget(axis, target));
        }

        private async Task<int> MovePickerZPlaceAfterContiProgressAsync(
            BaseAxis stageY,
            BaseAxis pickerX,
            double stageYStart,
            double stageYTarget,
            double pickerXStart,
            double pickerXTarget,
            Task<int> stageYMove,
            Task<int> pickerXMove,
            PickerAxis pickerZAxis,
            double pickerZTarget,
            PickerPlaceMotionConfig placeConfig,
            bool stageYForceMove,
            bool pickerXForceMove,
            CancellationToken ct)
        {
            CancellationTokenSource inspectionGateCancellation = null;
            Task<int> inspectionGateTask = null;
            bool inspectionGateObserved = false;

            try
            {
                ct.ThrowIfCancellationRequested();

                double stageYTrigger = ResolveContiAsyncPlaceTriggerPosition(stageYStart, stageYTarget, placeConfig);
                double pickerXTrigger = ResolveContiAsyncPlaceTriggerPosition(pickerXStart, pickerXTarget, placeConfig);
                int timeoutMs = Math.Max(1000, Math.Max(placeConfig != null ? placeConfig.ContiTimeoutMs : 0, ResolveTimeout()));
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                bool stageYMoveSucceeded = false;
                bool pickerXMoveSucceeded = false;
                bool stageYReachedByThreshold = false;
                bool pickerXReachedByThreshold = false;
                bool stageYReachedByCompletion = false;
                bool pickerXReachedByCompletion = false;
                bool inspectionGateSucceeded = false;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    if (stageYMove != null && stageYMove.IsCompleted)
                    {
                        int stageResult = await stageYMove.ConfigureAwait(false);
                        if (stageResult != 0)
                            return stageResult;
                        stageYMoveSucceeded = true;
                    }

                    if (pickerXMove != null && pickerXMove.IsCompleted)
                    {
                        int pickerXResult = await pickerXMove.ConfigureAwait(false);
                        if (pickerXResult != 0)
                            return pickerXResult;
                        pickerXMoveSucceeded = true;
                    }

                    if (stageY != null)
                        stageY.UpdateStatus();
                    if (pickerX != null)
                        pickerX.UpdateStatus();

                    if (stageY == null || pickerX == null)
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                            "Place ContiNode PickerZ 하강 트리거 확인 대상 축을 찾을 수 없습니다. " +
                            "stageY=" + (stageY != null) +
                            ", pickerX=" + (pickerX != null));
                    }

                    if (!stageY.IsServoOn || stageY.IsAlarm ||
                        !IsFinitePosition(stageY.ActualPosition) ||
                        !IsFinitePosition(stageY.CommandPosition))
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                            "Place ContiNode PickerZ 하강 트리거 대기 중 OutputStageY 상태가 비정상입니다. " +
                            BuildContiPlaceAxisDecision(stageY, stageYTarget, stageYForceMove));
                    }

                    if (!pickerX.IsServoOn || pickerX.IsAlarm ||
                        !IsFinitePosition(pickerX.ActualPosition) ||
                        !IsFinitePosition(pickerX.CommandPosition))
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER", Name,
                            "Place ContiNode PickerZ 하강 트리거 대기 중 PickerX 상태가 비정상입니다. " +
                            BuildContiPlaceAxisDecision(pickerX, pickerXTarget, pickerXForceMove));
                    }

                    double stageYActual = stageY != null ? stageY.ActualPosition : stageYStart;
                    double pickerXActual = pickerX != null ? pickerX.ActualPosition : pickerXStart;
                    bool stageYTriggerReached = IsContiAsyncPlaceTriggerReached(stageYStart, stageYTarget, stageYTrigger, stageYActual);
                    bool pickerXTriggerReached = IsContiAsyncPlaceTriggerReached(pickerXStart, pickerXTarget, pickerXTrigger, pickerXActual);
                    bool thresholdWindowOpen = DateTime.UtcNow < deadline;
                    stageYReachedByThreshold = thresholdWindowOpen && IsContiPlaceAxisThresholdReady(
                        stageY,
                        stageYStart,
                        stageYTarget,
                        stageYTrigger,
                        stageYTriggerReached,
                        stageYMoveSucceeded);
                    pickerXReachedByThreshold = thresholdWindowOpen && IsContiPlaceAxisThresholdReady(
                        pickerX,
                        pickerXStart,
                        pickerXTarget,
                        pickerXTrigger,
                        pickerXTriggerReached,
                        pickerXMoveSucceeded);
                    stageYReachedByCompletion = stageYMoveSucceeded && IsContiPlaceAxisStrongAtTarget(stageY, stageYTarget);
                    pickerXReachedByCompletion = pickerXMoveSucceeded && IsContiPlaceAxisStrongAtTarget(pickerX, pickerXTarget);
                    bool stageYReached = stageYReachedByThreshold || stageYReachedByCompletion;
                    bool pickerXReached = pickerXReachedByThreshold || pickerXReachedByCompletion;

                    if (stageYReached && pickerXReached && inspectionGateTask == null)
                    {
                        WriteLog("PickerPlaceSequence",
                            Name + " Place ContiNode XY 트리거 도달, 검사 결과 게이트 확인을 시작합니다. " +
                            "stageYReachedBy=" + (stageYReachedByThreshold ? "Threshold" : "MoveCompleteStrong") +
                            ", pickerXReachedBy=" + (pickerXReachedByThreshold ? "Threshold" : "MoveCompleteStrong") +
                            " - Check");
                        inspectionGateCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        inspectionGateTask = EnsureInspectionResultsReadyBeforePlaceDownAsync(
                            inspectionGateCancellation.Token);
                    }

                    if (!inspectionGateSucceeded &&
                        inspectionGateTask != null &&
                        inspectionGateTask.IsCompleted)
                    {
                        int inspectionGateResult;
                        try
                        {
                            inspectionGateResult = await inspectionGateTask.ConfigureAwait(false);
                        }
                        finally
                        {
                            inspectionGateObserved = true;
                        }

                        if (inspectionGateResult != 0)
                            return inspectionGateResult;
                        inspectionGateSucceeded = true;
                    }

                    // 검사 결과 대기 중에도 XY 상태를 매 주기 재확인하고,
                    // 검사 완료와 같은 상태 스냅샷에서 두 축이 모두 안전할 때만 Z 하강을 허용합니다.
                    if (stageYReached && pickerXReached && inspectionGateSucceeded)
                        break;

                    if (DateTime.UtcNow >= deadline && (!stageYReached || !pickerXReached))
                    {
                        return Fail("PICKER-PLACE-CONTI-ASYNC-Z-TRIGGER-TIMEOUT", Name,
                            "Place ContiNode PickerZ 하강 트리거 대기 시간이 초과되었습니다. " +
                            "timeoutMs=" + timeoutMs +
                            ", stageYStart=" + stageYStart.ToString("F6") +
                            ", stageYTrigger=" + stageYTrigger.ToString("F6") +
                            ", stageYActual=" + stageYActual.ToString("F6") +
                            ", stageYTarget=" + stageYTarget.ToString("F6") +
                            ", stageYMoveSucceeded=" + stageYMoveSucceeded +
                            ", stageYReachedByThreshold=" + stageYReachedByThreshold +
                            ", stageYReachedByCompletion=" + stageYReachedByCompletion +
                            ", stageYState={" + BuildContiPlaceAxisDecision(stageY, stageYTarget, stageYForceMove) + "}" +
                            ", pickerXStart=" + pickerXStart.ToString("F6") +
                            ", pickerXTrigger=" + pickerXTrigger.ToString("F6") +
                            ", pickerXActual=" + pickerXActual.ToString("F6") +
                            ", pickerXTarget=" + pickerXTarget.ToString("F6") +
                            ", pickerXMoveSucceeded=" + pickerXMoveSucceeded +
                            ", pickerXReachedByThreshold=" + pickerXReachedByThreshold +
                            ", pickerXReachedByCompletion=" + pickerXReachedByCompletion +
                            ", pickerXState={" + BuildContiPlaceAxisDecision(pickerX, pickerXTarget, pickerXForceMove) + "}");
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
                    ", stageYTarget=" + stageYTarget.ToString("F6") +
                    ", stageYReachedBy=" + (stageYReachedByThreshold ? "Threshold" : "MoveCompleteStrong") +
                    ", pickerXStart=" + pickerXStart.ToString("F6") +
                    ", pickerXTrigger=" + pickerXTrigger.ToString("F6") +
                    ", pickerXActual=" + (pickerX != null ? pickerX.ActualPosition.ToString("F6") : "-") +
                    ", pickerXTarget=" + pickerXTarget.ToString("F6") +
                    ", pickerXReachedBy=" + (pickerXReachedByThreshold ? "Threshold" : "MoveCompleteStrong") +
                    ", pickerZTarget=" + pickerZTarget.ToString("F6") +
                    " - Start");

                return await MovePickerAxisAndVerifyAsync(
                    pickerZAxis,
                    pickerZTarget,
                    "Place ContiNode PickerZ 비동기 하강",
                    ct,
                    BuildPickerTargetName("DiePlacePosition", _currentPickerIndex)).ConfigureAwait(false);
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
                if (inspectionGateCancellation != null)
                {
                    try
                    {
                        if (!inspectionGateObserved && inspectionGateTask != null)
                        {
                            inspectionGateCancellation.Cancel();
                            await DrainGateTaskAfterCancellationAsync(
                                inspectionGateTask,
                                "Place ContiNode PickerZ 하강 검사 결과 게이트").ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        inspectionGateCancellation.Dispose();
                    }
                }
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

        private OutputStageResultRoutingMode ResolveOutputStageResultRoutingMode()
        {
            if (_resultRoutingModeCaptured)
                return _resultRoutingModeSnapshot;

            if (OutputStage == null || OutputStage.Config == null)
                return OutputStageResultRoutingMode.ForceGoodStage;

            return OutputStage.Config.ResultRoutingMode;
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

                if (!CanSkipPickerMoveCommand(tAxis, target))
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

        private async Task<int> EnsureInspectionResultsReadyBeforePlaceDownAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (WaitInspectionResultsBeforePlaceDownAsync == null)
            {
                if (Options != null && Options.RunMode != SequenceRunMode.Auto)
                    return 0;

                if (_currentDie == null ||
                    !IsInspectionFlowComplete(_currentDie) ||
                    (_currentDie.Result != DieResult.Good && _currentDie.Result != DieResult.NG))
                {
                    return Fail("PICKER-PLACE-INSPECTION-RESUME-GATE", "Material",
                        "Place 재개 시 Vision RESULT Task는 없지만 저장된 Bottom/Side 판정이 완전하지 않아 PickerZ 하강을 차단합니다. " +
                        "side=" + Side +
                        ", pickerNo=" + _currentPickerNo +
                        ", die=" + (_currentDie != null ? _currentDie.DieId : "-") +
                        ", result=" + (_currentDie != null ? _currentDie.Result.ToString() : "null") +
                        ", bottomDone=" + HasInspectionResult(_currentDie, "Bottom") +
                        ", side0Done=" + HasInspectionResult(_currentDie, "Side0") +
                        ", side90Done=" + HasInspectionResult(_currentDie, "Side90") + ".");
                }

                WriteLog("PickerPlaceSequence",
                    Name + " Place 재개 PickerZ 하강 전 저장된 Bottom/Side 판정을 재확인했습니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId +
                    ", result=" + _currentDie.Result + " - Ok");
                return 0;
            }

            if (_currentDie == null || string.IsNullOrWhiteSpace(_currentDie.DieId))
            {
                return Fail("PICKER-PLACE-INSPECTION-GATE-DIE", "Material",
                    "PickerZ 하강 전 Bottom/Side 최종 RESULT와 연결할 Place 제품이 없습니다. " +
                    "side=" + Side + ", pickerNo=" + _currentPickerNo + ".");
            }

            WriteLog("PickerPlaceSequence",
                Name + " PickerZ 하강 직전 해당 Picker Bottom/Side 최종 RESULT를 기다립니다. " +
                "다른 Picker 결과는 기다리지 않습니다. pickerNo=" + _currentPickerNo +
                ", die=" + _currentDie.DieId + " - Start");

            int result = await WaitInspectionResultsBeforePlaceDownAsync(
                _currentPickerNo,
                _currentDie.DieId,
                ct).ConfigureAwait(false);
            if (result != 0)
            {
                return Fail("PICKER-PLACE-INSPECTION-GATE", "Vision",
                    "Bottom/Side 최종 RESULT 미수신 또는 불일치로 PickerZ 하강을 차단합니다. " +
                    "side=" + Side +
                    ", pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId +
                    ", result=" + result);
            }

            if (!IsInspectionFlowComplete(_currentDie) ||
                (_currentDie.Result != DieResult.Good && _currentDie.Result != DieResult.NG))
            {
                return Fail("PICKER-PLACE-INSPECTION-GATE-STATE", "Material",
                    "Bottom/Side 최종 RESULT Task는 완료됐지만 Material 판정 반영이 완료되지 않았습니다. " +
                    "pickerNo=" + _currentPickerNo +
                    ", die=" + _currentDie.DieId +
                    ", result=" + _currentDie.Result +
                    ", bottomDone=" + HasInspectionResult(_currentDie, "Bottom") +
                    ", side0Done=" + HasInspectionResult(_currentDie, "Side0") +
                    ", side90Done=" + HasInspectionResult(_currentDie, "Side90"));
            }

            WriteLog("PickerPlaceSequence",
                Name + " PickerZ 하강 직전 Bottom/Side 최종 RESULT 확인 완료. " +
                "pickerNo=" + _currentPickerNo +
                ", die=" + _currentDie.DieId +
                ", inspectionResult=" + _currentDie.Result + " - Ok");
            return 0;
        }

        private async Task<int> MovePickerZPlaceAsync(CancellationToken ct)
        {
            int inspectionGateResult = await EnsureInspectionResultsReadyBeforePlaceDownAsync(ct).ConfigureAwait(false);
            if (inspectionGateResult != 0)
                return inspectionGateResult;

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
                BuildPickerTargetName("DiePlacePosition", _currentPickerIndex)).ConfigureAwait(false);
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
            bool preserveInspectionResult = IsInspectionFlowComplete(_currentDie) &&
                                            (_currentDie.Result == DieResult.Good || _currentDie.Result == DieResult.NG);
            if (!MaterialStateService.MoveDieToOutputStage(
                _currentDie.DieId,
                _currentOutputSide,
                _receiveTarget,
                preserveInspectionResult))
            {
                return Fail("PICKER-PLACE-MATERIAL", "Material",
                    "Move die to output stage failed. die=" + _currentDie.DieId +
                    ", side=" + _currentOutputSide +
                    ", pickerNo=" + _currentPickerNo +
                    ", preserveInspectionResult=" + preserveInspectionResult);
            }

            WriteLog("PickerPlaceSequence",
                Name + " place complete. die=" + _currentDie.DieId +
                ", side=" + _currentOutputSide +
                ", pickerNo=" + _currentPickerNo +
                ", outputWafer=" + (_receiveTarget != null ? _receiveTarget.OutputWaferId : "-") +
                ", order=" + (_receiveTarget != null ? _receiveTarget.OrderIndex.ToString() : "-") +
                ", inspectionResult=" + _currentDie.Result +
                ", preserveInspectionResult=" + preserveInspectionResult + " - Ok");

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
                        HasPlacedDieCameraTarget = true,
                        PickerNo = _currentPickerNo,
                        PickerSide = Side,
                        HasPickerContext = true,
                        // Auto+Conti 플레이스 등록에서만 최소 회피 허용 (복원/기타 경로는 기본 false).
                        MinimalRetreatEligible =
                            Options != null && Options.RunMode == SequenceRunMode.Auto &&
                            IsCoordinatedPlaceMotionMode(ResolvePlaceMotionConfig().MotionMode),
                        PlacedStageY = _targetOutputStageY,
                        PlacedPickerY = _targetPickerY,
                        OutputVisionToPickerY = _outputVisionToPickerY,
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
                    ", pickerNo=" + _currentPickerNo +
                    ", placedStageY=" + _targetOutputStageY.ToString("F6") +
                    ", placedPickerY=" + _targetPickerY.ToString("F6") +
                    ", outputVisionToPickerY=" + _outputVisionToPickerY.ToString("F6") + " - Ok");
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
            return await MoveOutputStageAxisAndVerifyAsync(axis, target, description, ct, null, false).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(BinStageAxis axis, double target, string description, CancellationToken ct, string targetName)
        {
            return await MoveOutputStageAxisAndVerifyAsync(axis, target, description, ct, targetName, false).ConfigureAwait(false);
        }

        private async Task<int> MoveOutputStageAxisAndVerifyAsync(
            BinStageAxis axis,
            double target,
            string description,
            CancellationToken ct,
            string targetName,
            bool forceMove)
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
                        ", forceMove=" + forceMove +
                        ", " + OutputStage.BuildStageAxisState(axis, target) +
                        " - Start");
                }

                int result = await AwaitStepWithCancellationAsync(
                    OutputStage.MoveStageAxis(axis, target, Options.FineMove, targetName, forceMove),
                    ct).ConfigureAwait(false);
                if (result != 0)
                {
                    return Fail("PICKER-PLACE-STAGE-MOVE", "OutputStage",
                        description + " move command failed. result=" + result + ". " +
                        OutputStage.BuildStageAxisState(axis, target));
                }

                // 기존 조건: 이동 후 재대기 + 스냅샷 최종 확인 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3/R4).
                double tolerance = ResolveOutputStageAxisTolerance(axis);

                if (logResumePlaceStageMove)
                {
                    WriteLog("PickerPlaceSequence",
                        Name + " " + description + " 이동 완료. 이후 Picker X/T 완료 확인 후에만 PickerY 전진을 허용합니다. axis=" + axis +
                        ", target=" + target +
                        ", tolerance=" + tolerance +
                        ", targetName=" + (targetName ?? "-") +
                        ", forceMove=" + forceMove +
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

        private static bool CanSkipOutputFeederMoveCommand(OutputFeederUnit feeder, double target)
        {
            BaseAxis axis = feeder != null ? feeder.FeederY : null;
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return axis.IsAtTargetPosition(target, tolerance);
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

