using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederUnloadFromStageStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckStageWaferData,
        CheckStagePosition,
        MoveStageToAvoidPosition,
        MoveStageToUnloadPosition,
        VerifyFeederReadyAtAvoid,
        PrepareFeederUnclamp,
        PrepareFeederLiftUp,
        MoveFeederStageUnloadAvoidPosition,
        PrepareFeederLiftDown,
        MoveFeederStageUnloadPosition,
        VerifyStageWaferBeforeTransfer,
        ClampFeederWafer,
        StageVacuumOff,
        VerifyFeederWaferDetected,
        MoveMaterialDataToFeeder,
        VerifyTransferData,
        Complete,
        Error
    }



    internal sealed class InputFeederUnloadFromStageSequence : InputFeederSequenceBase<InputFeederUnloadFromStageStep>
    {
        // InputFeederSequenceBase.SequenceStateName과 반드시 같은 키를 사용한다.
        internal const string ResumeStateName = "InputFeederSequence.UnloadFromStage";

        public InputFeederUnloadFromStageSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.UnloadFromStage, "InputFeederUnloadFromStageSequence")
        {
        }

        protected override InputFeederUnloadFromStageStep IdleStep { get { return InputFeederUnloadFromStageStep.Idle; } }
        protected override InputFeederUnloadFromStageStep InitialStep { get { return InputFeederUnloadFromStageStep.CheckUnit; } }
        protected override InputFeederUnloadFromStageStep CompleteStep { get { return InputFeederUnloadFromStageStep.Complete; } }
        protected override InputFeederUnloadFromStageStep ErrorStep { get { return InputFeederUnloadFromStageStep.Error; } }

        protected override InputFeederUnloadFromStageStep ResolveStartStep(InputFeederUnloadFromStageStep initialStep)
        {
            InputFeederUnloadFromStageStep resolvedStep = base.ResolveStartStep(initialStep);
            if (object.Equals(resolvedStep, initialStep) || Feeder == null)
            {
                return resolvedStep;
            }

            // Ready/수동 복구가 실제 장비를 Avoid+Down 시작 자세로 되돌렸다면 저장된 후반
            // Step은 더 이상 물리 상태와 일치하지 않는다. 어떤 후반 Step이 저장됐더라도
            // CheckUnit부터 모든 자재/Teaching/Stage/Feeder 선행조건을 다시 확인한다.
            if (Feeder.IsWaferFeederInAvoidPosition() &&
                Feeder.IsWaferFeederDown() &&
                Feeder.IsWaferFeederEmpty() &&
                ResolveStageWafer() != null)
            {
                WriteLog(
                    "ResolveStartStep",
                    "Input feeder UnloadFromStage 저장 Step과 실제 상태가 달라 안전 Step으로 되돌립니다. " +
                    "savedStep=" + resolvedStep +
                    ", restartStep=" + initialStep +
                    ". " + Feeder.GetWaferFeederTransferState() +
                    " - Check");
                return initialStep;
            }

            return resolvedStep;
        }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputFeederUnloadFromStageStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederUnloadFromStageStep.CheckTransferReady));
                    // 이송 준비 확인
                    case InputFeederUnloadFromStageStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());
                    // 스테이지 웨이퍼 데이터 확인
                    case InputFeederUnloadFromStageStep.CheckStageWaferData:
                        return Task.FromResult(CheckStageWaferData());
                    // 스테이지 위치 확인
                    case InputFeederUnloadFromStageStep.CheckStagePosition:
                        return Task.FromResult(CheckStagePosition());
                    // 스테이지 어보이드 위치 이동
                    case InputFeederUnloadFromStageStep.MoveStageToAvoidPosition:
                        return MoveStageToAvoidPositionAsync(ct);
                    // 스테이지 언로드 위치 이동
                    case InputFeederUnloadFromStageStep.MoveStageToUnloadPosition:
                        return MoveStageToUnloadPositionAsync(ct);
                    // 피더 어보이드 준비 검증
                    case InputFeederUnloadFromStageStep.VerifyFeederReadyAtAvoid:
                        return Task.FromResult(VerifyFeederReadyAtAvoid());
                    // 피더 언클램프 준비
                    case InputFeederUnloadFromStageStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    // 피더 리프트 업 준비
                    case InputFeederUnloadFromStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);
                    // 피더 스테이지 언로드 어보이드 위치 이동
                    case InputFeederUnloadFromStageStep.MoveFeederStageUnloadAvoidPosition:
                        return MoveFeederStageUnloadAvoidPositionAsync(ct);
                    // 피더 리프트 다운 준비
                    case InputFeederUnloadFromStageStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);
                    // 피더 스테이지 언로드 위치 이동
                    case InputFeederUnloadFromStageStep.MoveFeederStageUnloadPosition:
                        return MoveFeederStageUnloadPositionAsync(ct);
                    // 스테이지 웨이퍼 전 이송 검증
                    case InputFeederUnloadFromStageStep.VerifyStageWaferBeforeTransfer:
                        return Task.FromResult(VerifyStageWaferBeforeTransfer());
                    // 피더 웨이퍼 클램프
                    case InputFeederUnloadFromStageStep.ClampFeederWafer:
                        return ClampFeederWaferAsync(ct);
                    // 스테이지 진공 OFF 처리
                    case InputFeederUnloadFromStageStep.StageVacuumOff:
                        return Task.FromResult(StageVacuumOff());
                    // 피더 웨이퍼 감지 검증
                    case InputFeederUnloadFromStageStep.VerifyFeederWaferDetected:
                        return VerifyFeederWaferDetectedAsync(ct);
                    // 자재 데이터를 피더로 이동
                    case InputFeederUnloadFromStageStep.MoveMaterialDataToFeeder:
                        return Task.FromResult(MoveMaterialDataToFeeder());
                    // 이송 데이터 검증
                    case InputFeederUnloadFromStageStep.VerifyTransferData:
                        return Task.FromResult(VerifyTransferData());
                    
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-FEEDER-STAGE-UNLOAD-STEP-EX", "InputFeederUnloadFromStageSequence", "Unload from stage step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            string readyReason;
            if (!Feeder.CheckWaferFeederMoveReady(out readyReason))
                return Fail("IN-FEEDER-STAGE-READY", Feeder.Name, "Input feeder is not move ready. " + readyReason);

            if (!CheckUnloadFromStageTeachingReady(out readyReason))
                return Fail("IN-FEEDER-STAGE-UNLOAD-TEACHING", Feeder.Name, "Input feeder unload from stage teaching data is not ready. " + readyReason);

            if (!Feeder.IsWaferFeederEmpty())
                return Fail("IN-FEEDER-OCCUPIED", Feeder.Name, "InputFeeder must be empty before stage to feeder unload. " + Feeder.GetWaferFeederTransferState());

            InputStageUnit stage = ResolveStage();
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            if (ResolveStageWafer() == null)
                return Fail("IN-FEEDER-STAGE-WAFER-DATA", "Material", "InputStage wafer data was not found.");

            CurrentStep = InputFeederUnloadFromStageStep.CheckStageWaferData;
            return 0;
        }

        private bool CheckUnloadFromStageTeachingReady(out string reason)
        {
            reason = string.Empty;
            if (Feeder == null || Feeder.Recipe == null)
            {
                reason = "Input feeder recipe is not available.";
                return false;
            }

            double stageUnload = Feeder.Recipe.WaferUnloadPosition;
            double stageUnloadAvoid = Feeder.Recipe.WaferUnloadAvoidPosition;
            double tolerance = Feeder.FeederY != null && Feeder.FeederY.Config != null && Feeder.FeederY.Config.InPositionTolerance > 0.0
                ? Feeder.FeederY.Config.InPositionTolerance
                : 0.01;

            if (Math.Abs(stageUnload - stageUnloadAvoid) <= tolerance)
            {
                reason = "WaferUnloadPosition equals WaferUnloadAvoidPosition. WaferUnload=" + stageUnload +
                         ", WaferUnloadAvoid=" + stageUnloadAvoid +
                         ", tolerance=" + tolerance;
                return false;
            }

            if (!IsFeederYTargetInSoftLimit(stageUnload))
            {
                reason = "WaferUnloadPosition is out of FeederY soft limit. target=" + stageUnload + ". " + BuildFeederYSoftLimitState();
                return false;
            }

            if (!IsFeederYTargetInSoftLimit(stageUnloadAvoid))
            {
                reason = "WaferUnloadAvoidPosition is out of FeederY soft limit. target=" + stageUnloadAvoid + ". " + BuildFeederYSoftLimitState();
                return false;
            }

            return true;
        }

        private int CheckStageWaferData()
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-STAGE-WAFER-DATA", "Material", "InputStage wafer data was not found.");

            InputStageUnit stage = ResolveStage();
            if (stage != null && stage.CurrentWaferMaterial == null)
                stage.SetCurrentWaferMaterial(wafer);

            CurrentStep = InputFeederUnloadFromStageStep.CheckStagePosition;
            return 0;
        }

        private int CheckStagePosition()
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            int result = CheckStageAxisReady(stage, WaferStageAxis.WaferY, "StageY");
            if (result != 0) return result;

            result = CheckStageAxisReady(stage, WaferStageAxis.WaferT, "StageT");
            if (result != 0) return result;

            result = CheckStageAxisReady(stage, WaferStageAxis.WaferExpandingZ, "StageZ");
            if (result != 0) return result;

            // Stage unload 위치 이동은 InputStagePrepareUnloadSequence에서 Feeder 접근 전에 완료한다.
            result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferY, stage.Recipe.WaferY.UnloadPosition, "StageY unload");
            if (result != 0) return result;

            result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferT, stage.Recipe.WaferT.UnloadPosition, "StageT unload");
            if (result != 0) return result;

            result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.UnloadPosition, "StageZ unload");
            if (result != 0) return result;

            CurrentStep = InputFeederUnloadFromStageStep.VerifyFeederReadyAtAvoid;
            return 0;
        }

        private async Task<int> MoveStageToAvoidPositionAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            Task<int> needleZMove = MoveStageAxisCommandAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, "NeedleZ avoid", ct);
            Task<int> ejectPinZMove = MoveStageAxisCommandAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, "EjectPinZ avoid", ct);
            int[] zMoveResults = await Task.WhenAll(needleZMove, ejectPinZMove).ConfigureAwait(false);
            if (zMoveResults[0] != 0) return zMoveResults[0];
            if (zMoveResults[1] != 0) return zMoveResults[1];

            Task<int> needleZWait = WaitStageAxisInPositionResultAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, "NeedleZ avoid", ct);
            Task<int> ejectPinZWait = WaitStageAxisInPositionResultAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, "EjectPinZ avoid", ct);
            int[] zWaitResults = await Task.WhenAll(needleZWait, ejectPinZWait).ConfigureAwait(false);
            if (zWaitResults[0] != 0) return zWaitResults[0];
            if (zWaitResults[1] != 0) return zWaitResults[1];

            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.AvoidPosition, "StageZ avoid", ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.VisionX, stage.Recipe.VisionX.AvoidPosition, "VisionX avoid", ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.NeedleX, stage.Recipe.NeedleX.AvoidPosition, "NeedleX avoid", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederUnloadFromStageStep.MoveStageToUnloadPosition;
            return 0;
        }

        private async Task<int> MoveStageToUnloadPositionAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferY, stage.Recipe.WaferY.UnloadPosition, "StageY unload", ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferT, stage.Recipe.WaferT.UnloadPosition, "StageT unload", ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.UnloadPosition, "StageZ unload", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederUnloadFromStageStep.VerifyFeederReadyAtAvoid;
            return 0;
        }

        private int VerifyFeederReadyAtAvoid()
        {
            if (!Feeder.IsWaferFeederInAvoidPosition())
                return Fail("IN-FEEDER-AVOID-CHECK", Feeder.Name,
                    "WaferFeeder must already be at avoid position before stage unload. " + Feeder.GetWaferFeederTransferState());

            if (!Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "WaferFeeder must already be down before stage unload starts. " + Feeder.GetWaferFeederTransferState());

            CurrentStep = Feeder.IsWaferFeederUnclamp()
                ? InputFeederUnloadFromStageStep.PrepareFeederLiftUp
                : InputFeederUnloadFromStageStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederUnclamp())
                return Fail("IN-FEEDER-UNCLAMP", Feeder.Name,
                    "WaferFeeder unclamp command failed before stage unload. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederUnloadFromStageStep.PrepareFeederLiftUp;
            return 0;
        }

        private async Task<int> PrepareFeederLiftUpAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederUpDownAsync(true, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederUp())
                return Fail("IN-FEEDER-LIFT-UP", Feeder.Name,
                    "WaferFeeder lift up command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederUnloadFromStageStep.MoveFeederStageUnloadAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageUnloadAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            string prerequisiteReason;
            if (!CheckFeederStageUnloadAvoidMovePrerequisites(out prerequisiteReason))
            {
                return Fail(
                    "IN-FEEDER-STAGE-UNLOAD-AVOID-PRECONDITION",
                    Feeder != null ? Feeder.Name : "InputFeeder",
                    "InputFeederY stage unload avoid 이동 전 선행조건 불일치로 이동을 차단합니다. " +
                    prerequisiteReason +
                    (Feeder != null ? " " + Feeder.GetWaferFeederTransferState() : ""));
            }

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederStageUnloadAvoidPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-UNLOAD-AVOID-MOVE", Feeder.Name,
                    "WaferFeeder stage unload avoid position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInStageUnloadAvoidPosition(),
                "WaferFeeder stage unload avoid position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederUnloadFromStageStep.PrepareFeederLiftDown;
            return 0;
        }

        private bool CheckFeederStageUnloadAvoidMovePrerequisites(out string reason)
        {
            reason = string.Empty;
            if (Feeder == null)
            {
                reason = "InputFeeder unit is not available.";
                return false;
            }

            if (!Feeder.IsWaferFeederSimulationOrDryRun())
            {
                int readError = -1;
                if (Feeder.WaferFeederUpSensor == null ||
                    !AjinIoScanService.TryReadHardwareInput(Feeder.WaferFeederUpSensor, out readError))
                {
                    reason = "Lift Up 센서 갱신 실패. error=" + readError + ".";
                    return false;
                }

                if (Feeder.WaferFeederDownSensor == null ||
                    !AjinIoScanService.TryReadHardwareInput(Feeder.WaferFeederDownSensor, out readError))
                {
                    reason = "Lift Down 센서 갱신 실패. error=" + readError + ".";
                    return false;
                }

                if (Feeder.WaferFeederClampSensor == null ||
                    !AjinIoScanService.TryReadHardwareInput(Feeder.WaferFeederClampSensor, out readError))
                {
                    reason = "Clamp/Unclamp 센서 갱신 실패. error=" + readError + ".";
                    return false;
                }
            }

            string axisReason;
            if (!Feeder.CheckWaferFeederYMoveReady(out axisReason))
            {
                reason = "InputFeederY 이동 준비 실패. " + axisReason;
                return false;
            }

            bool liftUp = Feeder.IsWaferFeederUp();
            bool liftDown = Feeder.IsWaferFeederDown();
            bool unclamp = Feeder.IsWaferFeederUnclamp();
            bool feederEmpty = Feeder.IsWaferFeederEmpty();
            bool stageWaferPresent = ResolveStageWafer() != null;
            if (!liftUp || liftDown || !unclamp || !feederEmpty || !stageWaferPresent)
            {
                reason =
                    "required=LiftUp+LiftDownOff+Unclamp+FeederEmpty+StageWafer, " +
                    "actual=LiftUp:" + liftUp +
                    ",LiftDown:" + liftDown +
                    ",Unclamp:" + unclamp +
                    ",FeederEmpty:" + feederEmpty +
                    ",StageWafer:" + stageWaferPresent + ".";
                return false;
            }

            return true;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederUpDownAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-LIFT-DOWN", Feeder.Name,
                    "WaferFeeder lift down command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederUnloadFromStageStep.MoveFeederStageUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageUnloadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederStageUnloadPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-UNLOAD-POS", Feeder.Name,
                    "WaferFeeder stage unload position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInStageUnloadPosition(),
                "WaferFeeder stage unload position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederUnloadFromStageStep.VerifyStageWaferBeforeTransfer;
            return 0;
        }

        private int VerifyStageWaferBeforeTransfer()
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-STAGE-WAFER-DATA-MISSING", "Material", "InputStage wafer data disappeared before stage to feeder transfer.");

            CurrentStep = InputFeederUnloadFromStageStep.ClampFeederWafer;
            return 0;
        }

        private async Task<int> ClampFeederWaferAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(true, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederClamp())
                return Fail("IN-FEEDER-CLAMP", Feeder.Name,
                    "WaferFeeder clamp command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederUnloadFromStageStep.StageVacuumOff;
            return 0;
        }

        private int StageVacuumOff()
        {
            InputStageUnit stage = ResolveStage();
            if (stage != null && stage.NeedleVacuum != null && Options.UseVacuum)
                stage.SetNeedleVacuum(false);

            CurrentStep = InputFeederUnloadFromStageStep.VerifyFeederWaferDetected;
            return 0;
        }

        private async Task<int> VerifyFeederWaferDetectedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-STAGE-WAFER-DATA-MISSING", "Material", "InputStage wafer data disappeared before feeder detection check.");

            int prerequisiteResult = VerifyFeederWaferDetectionPrerequisites();
            if (prerequisiteResult != 0)
                return prerequisiteResult;

            if (!IsHardwareBypass())
            {
                bool detected = await Feeder.WaitWaferFeederRingState(true, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("IN-FEEDER-STAGE-UNLOAD-RING", Feeder.Name, "WaferFeeder ring was not detected after stage unload. waferId=" + wafer.WaferId);
            }

            CurrentStep = InputFeederUnloadFromStageStep.MoveMaterialDataToFeeder;
            return 0;
        }

        private int VerifyFeederWaferDetectionPrerequisites()
        {
            if (Feeder == null || Feeder.FeederY == null || Feeder.Recipe == null)
            {
                return Fail(
                    "IN-FEEDER-STAGE-UNLOAD-RING-PRECONDITION",
                    Feeder != null ? Feeder.Name : "InputFeeder",
                    "Ring 확인 전 InputFeeder, FeederY 또는 Recipe를 확인할 수 없습니다.");
            }

            double target = Feeder.Recipe.WaferUnloadPosition;
            double tolerance = Feeder.FeederY.Config != null &&
                               Feeder.FeederY.Config.InPositionTolerance > 0.0
                ? Feeder.FeederY.Config.InPositionTolerance
                : 0.01;
            bool actualMatch = Math.Abs(Feeder.FeederY.ActualPosition - target) <= tolerance;
            bool commandMatch = Math.Abs(Feeder.FeederY.CommandPosition - target) <= tolerance;
            bool liftUp = Feeder.IsWaferFeederUp();
            bool liftDown = Feeder.IsWaferFeederDown();
            bool clamp = Feeder.IsWaferFeederClamp();
            bool unclamp = Feeder.IsWaferFeederUnclamp();
            bool transferDataEmpty = Feeder.IsWaferFeederTransferDataEmpty();
            bool overload = Feeder.IsWaferFeederOverload();

            if (!Feeder.FeederY.IsServoOn ||
                Feeder.FeederY.IsAlarm ||
                Feeder.FeederY.IsMoving ||
                !Feeder.FeederY.IsInPosition ||
                !actualMatch ||
                !commandMatch ||
                liftUp ||
                !liftDown ||
                !clamp ||
                unclamp ||
                !transferDataEmpty ||
                overload)
            {
                return Fail(
                    "IN-FEEDER-STAGE-UNLOAD-RING-PRECONDITION",
                    Feeder.Name,
                    "Ring 확인 전 InputFeeder의 Stage Unload 인계 자세가 일치하지 않습니다. " +
                    "required=ServoOn+AlarmOff+Stopped+InPosition+UnloadPosition+LiftDown+Clamp+DataEmpty+OverloadOff, " +
                    "target=" + target +
                    ", tolerance=" + tolerance +
                    ", actualMatch=" + actualMatch +
                    ", commandMatch=" + commandMatch +
                    ", liftUp=" + liftUp +
                    ", liftDown=" + liftDown +
                    ", clamp=" + clamp +
                    ", unclamp=" + unclamp +
                    ", transferDataEmpty=" + transferDataEmpty +
                    ", overload=" + overload +
                    ". " + Feeder.GetWaferFeederTransferState());
            }

            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
            {
                return Fail(
                    "IN-FEEDER-STAGE-UNLOAD-RING-PRECONDITION",
                    "InputStage",
                    "Ring 확인 전 InputStage 또는 Recipe를 확인할 수 없습니다.");
            }

            int result = CheckStageAxisReady(stage, WaferStageAxis.WaferY, "StageY");
            if (result != 0) return result;
            result = CheckStageAxisReady(stage, WaferStageAxis.WaferT, "StageT");
            if (result != 0) return result;
            result = CheckStageAxisReady(stage, WaferStageAxis.WaferExpandingZ, "StageZ");
            if (result != 0) return result;

            result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferY, stage.Recipe.WaferY.UnloadPosition, "StageY unload before ring check");
            if (result != 0) return result;
            result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferT, stage.Recipe.WaferT.UnloadPosition, "StageT unload before ring check");
            if (result != 0) return result;
            return CheckStageAxisInPosition(
                stage,
                WaferStageAxis.WaferExpandingZ,
                stage.Recipe.WaferZ.UnloadPosition,
                "StageZ unload before ring check");
        }

        private int MoveMaterialDataToFeeder()
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-MATERIAL-MOVE", "Material", "InputStage wafer data was not found for feeder material move.");

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("IN-FEEDER-MATERIAL-WAFER", "Material", "물리 이송 후 InputFeeder Material 갱신 직전에 Wafer ID가 변경되었습니다. expected=" + Options.ExpectedWaferId + ", actual=" + wafer.WaferId);

            if (wafer.SourceCassetteRole != Options.CassetteRole || wafer.SourceSlotNumber != Options.SlotIndex)
                return Fail("IN-FEEDER-MATERIAL-SOURCE", "Material", "InputStage wafer의 원본 cassette/slot 정보가 sequence option과 다릅니다. wafer=" + wafer.WaferId +
                    ", sourceRole=" + wafer.SourceCassetteRole + ", sourceSlot=" + (wafer.SourceSlotNumber + 1).ToString("00") +
                    ", optionRole=" + Options.CassetteRole + ", optionSlot=" + (Options.SlotIndex + 1).ToString("00"));

            // 현재 기준: Stage에서 wafer를 빼는 순간 input live map/cache도 같이 비운다.
            if (Context != null && Context.Controller != null)
                Context.Controller.ClearInputDieMap("InputFeederUnloadFromStageSequence.MoveMaterialDataToFeeder");
            else
                LotStorage.ActiveInputDieMap = null;

            MaterialStateService.MoveWaferToInputFeeder(wafer);
            Feeder.SetCurrentWaferMaterial(MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder));

            InputStageUnit stage = ResolveStage();
            if (stage != null)
                stage.ClearCurrentWaferMaterial();

            Context.Bus.Set("InputStageEmpty");
            Context.Bus.Set("InputFeederOccupied");
            CurrentStep = InputFeederUnloadFromStageStep.VerifyTransferData;
            return 0;
        }

        private int VerifyTransferData()
        {
            WaferMaterial feederWafer = Feeder.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
            if (feederWafer == null)
                return Fail("IN-FEEDER-DATA", "Material", "InputFeeder wafer data was not found after stage to feeder transfer.");

            InputStageUnit stage = ResolveStage();
            if ((stage != null && stage.CurrentWaferMaterial != null) ||
                MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage) != null)
            {
                return Fail("IN-FEEDER-STAGE-DATA-CLEAR", "Material", "InputStage wafer data remained after stage to feeder transfer.");
            }

            CurrentStep = InputFeederUnloadFromStageStep.Complete;
            return 0;
        }

        private async Task<int> MoveStageAxisAndVerifyAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            int result = await MoveStageAxisCommandAsync(stage, axis, target, description, ct).ConfigureAwait(false);
            if (result != 0) return result;

            return 0;
        }

        private async Task<int> MoveStageAxisCommandAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisMove(item, target, out interlockReason))
                    return Fail("IN-FEEDER-STAGE-INTERLOCK", stage != null ? stage.Name : "InputStage",
                        description + " 이동 인터락 차단. " + interlockReason + ". " +
                        BuildStageAxisState(stage, axis, target));

                int result = await AwaitStepWithCancellationAsync(stage.MoveInputStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-FEEDER-STAGE-MOVE", stage.Name, description + " 이동 명령 실패. result=" + result + ", " + BuildStageAxisState(stage, axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-MOVE-EX", stage != null ? stage.Name : "InputStage",
                    description + " 이동 명령 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitStageAxisInPositionResultAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int waitCode = await stage.WaitInputStageAxisInPositionResult(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitCode != 0)
                    return Fail("IN-FEEDER-STAGE-MOVE", stage.Name,
                        description + " 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ". " + BuildStageAxisState(stage, axis, target));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-WAIT-EX", stage != null ? stage.Name : "InputStage",
                    description + " 이동 완료 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckStageAxisReady(InputStageUnit stage, WaferStageAxis axis, string description)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return Fail("IN-FEEDER-STAGE-AXIS", stage != null ? stage.Name : "InputStage",
                    description + " axis is not available. axis=" + axis);

            if (item.IsMoving || item.IsAlarm)
                return Fail("IN-FEEDER-STAGE-READY", stage.Name,
                    description + " axis is not ready before unload from stage. " +
                    BuildStageAxisState(stage, axis, item.ActualPosition));

            return 0;
        }

        private int CheckStageAxisInPosition(InputStageUnit stage, WaferStageAxis axis, double target, string description)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return Fail("IN-FEEDER-STAGE-AXIS", stage != null ? stage.Name : "InputStage",
                    description + " axis is not available. " + BuildStageAxisState(stage, axis, target));

            if (item.IsMoving || item.IsAlarm || !IsStageAxisInPosition(item, target))
                return Fail("IN-FEEDER-STAGE-POSITION", stage.Name,
                    description + " position check failed. Stage axis is checked only; no stage move is commanded in UnloadFromStage. " +
                    BuildStageAxisState(stage, axis, target));

            return 0;
        }

        private static bool IsStageAxisInPosition(QMC.Common.Motion.BaseAxis item, double target)
        {
            if (item == null)
                return false;

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(item.ActualPosition - target) <= tolerance;
        }

        private QMC.Common.Motion.BaseAxis ResolveStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                // 웨이퍼 Y축 반환
                case WaferStageAxis.WaferY: return stage.StageY;
                // 웨이퍼 T축 반환
                case WaferStageAxis.WaferT: return stage.StageT;
                // 웨이퍼 확장 Z축 반환
                case WaferStageAxis.WaferExpandingZ: return stage.ExpanderZ;
                // 비전 X축 반환
                case WaferStageAxis.VisionX: return stage.CameraX;
                // 니들 X축 반환
                case WaferStageAxis.NeedleX: return stage.NeedleBlockX;
                // 니들 Z축 반환
                case WaferStageAxis.NeedleZ: return stage.NeedleZ;
                // 이젝트 핀 Z축 반환
                case WaferStageAxis.EjectPinZ: return stage.EjectPinZ;
                default: return null;
            }
        }

        private string BuildStageAxisState(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return "axis=" + axis + ", target=" + target + ", state=axis-not-found";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;

            return "axis=" + axis +
                   ", name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", actual=" + item.ActualPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance +
                   FormatAxisLastMotionFailure(item) +
                   FormatStageLastMoveFailure(stage);
        }

        private bool IsFeederYTargetInSoftLimit(double target)
        {
            if (Feeder == null || Feeder.FeederY == null || Feeder.FeederY.Setup == null)
                return false;

            if (!Feeder.FeederY.Setup.SoftLimitEnabled)
                return true;

            return target >= Feeder.FeederY.Setup.SoftLimitMinus &&
                   target <= Feeder.FeederY.Setup.SoftLimitPlus;
        }

        private string BuildFeederYSoftLimitState()
        {
            if (Feeder == null || Feeder.FeederY == null || Feeder.FeederY.Setup == null)
                return "FeederY setup is not available.";

            return "softLimitEnabled=" + Feeder.FeederY.Setup.SoftLimitEnabled +
                   ", softMinus=" + Feeder.FeederY.Setup.SoftLimitMinus +
                   ", softPlus=" + Feeder.FeederY.Setup.SoftLimitPlus +
                   ", actual=" + Feeder.FeederY.ActualPosition;
        }

        private static string FormatAxisLastMotionFailure(QMC.Common.Motion.BaseAxis item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.LastMotionFailureMessage))
                return string.Empty;

            return ", lastMotionFailure=" + item.LastMotionFailureMessage;
        }

        private static string FormatStageLastMoveFailure(InputStageUnit stage)
        {
            if (stage == null || string.IsNullOrWhiteSpace(stage.LastStageMoveFailureMessage))
                return string.Empty;

            return ", lastStageMoveFailure=" + stage.LastStageMoveFailureMessage;
        }

        private InputStageUnit ResolveStage()
        {
            return Context.Machine != null ? Context.Machine.InputStageUnit : null;
        }

        private WaferMaterial ResolveStageWafer()
        {
            InputStageUnit stage = ResolveStage();
            return (stage != null ? stage.CurrentWaferMaterial : null) ??
                   MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
        }

        private bool IsHardwareBypass()
        {
            AppSettings settings = AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (Context.Controller != null && Context.Controller.GlobalDryRun) ||
                   (Feeder.Setup != null && Feeder.Setup.IsSimulationMode) ||
                   (Feeder.Config != null && Feeder.Config.bDryRun);
        }
    }
}

