using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

using QMC.CDT320.Interlocks;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederLoadToStageStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        RunBarcodeSequence,
        CheckStageLoadPosition,
        VerifyFeederHoldingWafer,
        MoveFeederStageLoadPosition,
        VerifyWaferBeforeTransfer,
        StageVacuumOn,
        PrepareFeederUnclamp,
        MoveFeederStageLoadAvoidPosition,
        MoveMaterialDataToStage,
        ClearFeederData,
        PrepareFeederLiftUp,
        MoveFeederAvoidPosition,
        PrepareFeederLiftDownAfterAvoid,
        VerifyInputStageData,
        MoveInputStageProcessPosition,
        Complete,
        Error
    }

    internal sealed class InputFeederLoadToStageSequence : InputFeederSequenceBase<InputFeederLoadToStageStep>
    {
        public InputFeederLoadToStageSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.LoadToStage, "InputFeederLoadToStageSequence")
        {
        }

        protected override InputFeederLoadToStageStep IdleStep { get { return InputFeederLoadToStageStep.Idle; } }
        protected override InputFeederLoadToStageStep InitialStep { get { return InputFeederLoadToStageStep.CheckUnit; } }
        protected override InputFeederLoadToStageStep CompleteStep { get { return InputFeederLoadToStageStep.Complete; } }
        protected override InputFeederLoadToStageStep ErrorStep { get { return InputFeederLoadToStageStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputFeederLoadToStageStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederLoadToStageStep.CheckTransferReady));
                    // 이송 준비 확인
                    case InputFeederLoadToStageStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());
                    // 바코드 시퀀스 실행
                    case InputFeederLoadToStageStep.RunBarcodeSequence:
                        return Task.FromResult(RunBarcodeSequence());
                    // 스테이지 로드 위치 확인
                    case InputFeederLoadToStageStep.CheckStageLoadPosition:
                        return Task.FromResult(CheckStageLoadPosition());
                    // 피더 보유 웨이퍼 검증
                    case InputFeederLoadToStageStep.VerifyFeederHoldingWafer:
                        return Task.FromResult(VerifyFeederHoldingWafer());
                    // 피더 스테이지 로드 위치 이동
                    case InputFeederLoadToStageStep.MoveFeederStageLoadPosition:
                        return MoveFeederStageLoadPositionAsync(ct);
                    // 웨이퍼 전 이송 검증
                    case InputFeederLoadToStageStep.VerifyWaferBeforeTransfer:
                        return VerifyWaferBeforeTransferAsync(ct);
                    // 스테이지 진공 ON 처리
                    case InputFeederLoadToStageStep.StageVacuumOn:
                        return Task.FromResult(StageVacuumOn());
                    // 피더 언클램프 준비
                    case InputFeederLoadToStageStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    // 피더 스테이지 로드 어보이드 위치 이동
                    case InputFeederLoadToStageStep.MoveFeederStageLoadAvoidPosition:
                        return MoveFeederStageLoadAvoidPositionAsync(ct);
                    // 자재 데이터를 스테이지로 이동
                    case InputFeederLoadToStageStep.MoveMaterialDataToStage:
                        return Task.FromResult(MoveMaterialDataToStage());
                    // 피더 데이터 클리어
                    case InputFeederLoadToStageStep.ClearFeederData:
                        return Task.FromResult(ClearFeederData());
                    // 피더 리프트 업 준비
                    case InputFeederLoadToStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);
                    // 피더 어보이드 위치 이동
                    case InputFeederLoadToStageStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);
                    // 피더 리프트 다운 후 어보이드 준비
                    case InputFeederLoadToStageStep.PrepareFeederLiftDownAfterAvoid:
                        return PrepareFeederLiftDownAsync(ct, InputFeederLoadToStageStep.VerifyInputStageData);
                    // 인풋 스테이지 데이터 검증
                    case InputFeederLoadToStageStep.VerifyInputStageData:
                        return Task.FromResult(VerifyInputStageData());
                    // 인풋 스테이지 공정 위치 이동
                    case InputFeederLoadToStageStep.MoveInputStageProcessPosition:
                        return MoveInputStageProcessPositionAsync(ct);
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-FEEDER-STAGE-LOAD-STEP-EX", "InputFeederLoadToStageSequence", "Load to stage step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            string readyReason;
            if (!Feeder.CheckWaferStageReady(Options.WaferSize, TransferMode.Load, out readyReason))
                return Fail("IN-FEEDER-STAGE-READY", Feeder.Name, "Input feeder to stage transfer condition is not ready. " + readyReason);

            if (!CheckLoadToStageTeachingReady(out readyReason))
                return Fail("IN-FEEDER-STAGE-TEACHING", Feeder.Name, "Input feeder to stage teaching data is not ready. " + readyReason);

            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-WAFER-DATA", "Material", "Input feeder wafer data was not found.");

            InputStageUnit stage = Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            if (!IsInputStageEmpty(stage))
                return Fail("IN-FEEDER-STAGE-OCCUPIED", stage.Name, "Input stage must be empty before feeder to stage load.");

            CurrentStep = InputFeederLoadToStageStep.RunBarcodeSequence;
            return 0;
        }

        private bool CheckLoadToStageTeachingReady(out string reason)
        {
            reason = string.Empty;
            if (Feeder == null || Feeder.Recipe == null)
            {
                reason = "Input feeder recipe is not available.";
                return false;
            }

            double stageLoad = Feeder.Recipe.WaferLoadPosition;
            double stageLoadAvoid = Feeder.Recipe.WaferLoadAvoidPosition;
            double tolerance = Feeder.FeederY != null && Feeder.FeederY.Config != null && Feeder.FeederY.Config.InPositionTolerance > 0.0
                ? Feeder.FeederY.Config.InPositionTolerance
                : 0.01;

            if (Math.Abs(stageLoad - stageLoadAvoid) <= tolerance)
            {
                reason = "WaferLoadPosition equals WaferLoadAvoidPosition. WaferLoad=" + stageLoad +
                         ", WaferLoadAvoid=" + stageLoadAvoid +
                         ", tolerance=" + tolerance;
                return false;
            }

            if (!IsFeederYTargetInSoftLimit(stageLoad))
            {
                reason = "WaferLoadPosition is out of FeederY soft limit. target=" + stageLoad + ". " + BuildFeederYSoftLimitState();
                return false;
            }

            if (!IsFeederYTargetInSoftLimit(stageLoadAvoid))
            {
                reason = "WaferLoadAvoidPosition is out of FeederY soft limit. target=" + stageLoadAvoid + ". " + BuildFeederYSoftLimitState();
                return false;
            }

            return true;
        }

        private int RunBarcodeSequence()
        {
            if (Options.UseBarcode)
                WriteLog("InputFeederLoadToStageSequence", "Barcode sequence placeholder. Feeder to stage load will continue after barcode sequence hook. - Ok");

            CurrentStep = InputFeederLoadToStageStep.CheckStageLoadPosition;
            return 0;
        }

        private int CheckStageLoadPosition()
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            int result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferY, stage.Recipe.WaferY.LoadPosition, "StageY load");
            if (result != 0) return result;

            result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferT, stage.Recipe.WaferT.LoadPosition, "StageT load");
            if (result != 0) return result;

            result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.LoadPosition, "StageZ load");
            if (result != 0) return result;

            CurrentStep = InputFeederLoadToStageStep.VerifyFeederHoldingWafer;
            return 0;
        }

        private int VerifyFeederHoldingWafer()
        {
            if (!Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "WaferFeeder must already be down before feeder to stage load. " + Feeder.GetWaferFeederTransferState());

            if (!Feeder.IsWaferFeederClamp())
                return Fail("IN-FEEDER-CLAMP-CHECK", Feeder.Name,
                    "WaferFeeder must already be clamped before feeder to stage load. " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederLoadToStageStep.MoveFeederStageLoadPosition;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct, InputFeederLoadToStageStep nextStep)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederUpDownAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-LIFT-DOWN", Feeder.Name,
                    "WaferFeeder lift down command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = nextStep;
            return 0;
        }

        private async Task<int> MoveFeederStageLoadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederStageLoadPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-LOAD-MOVE", Feeder.Name,
                    "WaferFeeder stage load position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInStageLoadPosition(),
                "WaferFeeder stage load position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederLoadToStageStep.VerifyWaferBeforeTransfer;
            return 0;
        }

        private async Task<int> VerifyWaferBeforeTransferAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-WAFER-DATA-MISSING", "Material", "Input feeder wafer data disappeared before stage transfer.");

            if (!IsHardwareBypass() && !Feeder.IsWaferFeederRingDetected(Options.WaferSize, true))
            {
                bool detected = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederRingState(true, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("IN-FEEDER-WAFER-SENSOR", Feeder.Name, "Wafer sensor timeout or data/sensor mismatch before feeder to stage transfer. waferId=" + wafer.WaferId);
            }

            CurrentStep = InputFeederLoadToStageStep.StageVacuumOn;
            return 0;
        }

        private int StageVacuumOn()
        {
            InputStageUnit stage = ResolveStage();
            if (stage != null && stage.NeedleVacuum != null && Options.UseVacuum)
                stage.NeedleVacuum.On();

            CurrentStep = InputFeederLoadToStageStep.PrepareFeederUnclamp;
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
                    "WaferFeeder unclamp command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederLoadToStageStep.MoveFeederStageLoadAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageLoadAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederStageLoadAvoidPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-LOAD-AVOID-MOVE", Feeder.Name,
                    "WaferFeeder stage load avoid position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInStageLoadAvoidPosition(),
                "WaferFeeder stage load avoid position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!IsHardwareBypass())
            {
                bool cleared = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederRingState(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("IN-FEEDER-STAGE-TRANSFER-SENSOR", Feeder.Name, "WaferFeeder ring remained after feeder stage load avoid move.");
            }

            CurrentStep = InputFeederLoadToStageStep.MoveMaterialDataToStage;
            return 0;
        }

        private int MoveMaterialDataToStage()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-MATERIAL-MOVE", "Material", "Input feeder wafer data was not found for stage material move.");

            MaterialStateService.MoveWaferToInputStage(wafer);

            InputStageUnit stage = ResolveStage();
            if (stage != null)
                stage.SetCurrentWaferMaterial(MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage));

            CurrentStep = InputFeederLoadToStageStep.ClearFeederData;
            return 0;
        }

        private int ClearFeederData()
        {
            Feeder.ClearCurrentWaferMaterial();
            Context.Bus.Set("InputFeederEmpty");
            Context.Bus.Set("InputStageOccupied");
            CurrentStep = InputFeederLoadToStageStep.PrepareFeederLiftUp;
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

            CurrentStep = InputFeederLoadToStageStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederAvoidPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-AVOID-MOVE", Feeder.Name,
                    "WaferFeeder avoid position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInAvoidPosition(),
                "WaferFeeder avoid position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederLoadToStageStep.PrepareFeederLiftDownAfterAvoid;
            return 0;
        }

        private int VerifyInputStageData()
        {
            InputStageUnit stage = ResolveStage();
            WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (stageWafer == null || stage == null || stage.CurrentWaferMaterial == null)
                return Fail("IN-FEEDER-STAGE-DATA", "Material", "InputStage wafer data was not found after feeder to stage transfer.");

            if (Feeder.CurrentWaferMaterial != null || MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder) != null)
                return Fail("IN-FEEDER-DATA-CLEAR", "Material", "InputFeeder wafer data remained after feeder to stage transfer.");

            CurrentStep = InputFeederLoadToStageStep.MoveInputStageProcessPosition;
            return 0;
        }

        private async Task<int> MoveInputStageProcessPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputStageUnit stage = ResolveStage();
                if (stage == null || stage.Recipe == null)
                    return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available for process position move.");

                stage.Recipe.EnsurePositionObjects();

                int result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.WaferExpandingZ,
                    stage.Recipe.WaferZ.ProcessPosition,
                    "StageZ process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.WaferY,
                    stage.Recipe.WaferY.ProcessPosition,
                    "StageY process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.WaferT,
                    stage.Recipe.WaferT.ProcessPosition,
                    "StageT process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.NeedleX,
                    stage.Recipe.NeedleX.ProcessPosition,
                    "NeedleX process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    stage.Recipe.NeedleZ.ProcessPosition,
                    "NeedleZ process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.VisionX,
                    stage.Recipe.VisionX.ProcessPosition,
                    "VisionX process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                CurrentStep = InputFeederLoadToStageStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-PROCESS-EX", "InputStage",
                    "InputStage process position move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveStageAxisAndVerifyAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            int result = await MoveStageAxisCommandAsync(stage, axis, target, description, ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await WaitStageAxisInPositionResultAsync(stage, axis, target, description, ct).ConfigureAwait(false);
            if (result != 0) return result;

            return CheckStageAxisInPosition(stage, axis, target, description);
        }

        private async Task<int> MoveStageAxisCommandAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return Fail("IN-FEEDER-STAGE-AXIS", stage != null ? stage.Name : "InputStage",
                    description + " axis is not available. " + BuildStageAxisState(stage, axis, target));

            string interlockReason;
            if (!MotionGuardRuntime.VerifyAxisMove(item, target, out interlockReason))
                return Fail("IN-FEEDER-STAGE-PROCESS-INTERLOCK", stage != null ? stage.Name : "InputStage",
                    description + " move blocked by interlock. " + interlockReason + ". " +
                    BuildStageAxisState(stage, axis, target));

            int result = await AwaitStepWithCancellationAsync(
                stage.MoveInputStageAxis(axis, target, Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-PROCESS-MOVE", stage.Name,
                    description + " move command failed. result=" + result + ". " +
                    BuildStageAxisState(stage, axis, target));

            ct.ThrowIfCancellationRequested();
            return 0;
        }

        private async Task<int> WaitStageAxisInPositionResultAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            AxisMoveWaitResult waitResult = await AwaitStepWithCancellationAsync(
                stage.WaitInputStageAxisInPositionResult(axis, target, ResolveTimeout()),
                ct).ConfigureAwait(false);
            if (waitResult == null || !waitResult.Success)
                return Fail(ResolveAxisMoveWaitAlarmCode("IN-FEEDER-STAGE-PROCESS", waitResult), stage.Name,
                    description + " move/in-position wait failed. " +
                    FormatAxisMoveWaitResult(waitResult, BuildStageAxisState(stage, axis, target)));

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
                    description + " final position check failed after stage move/check step. " +
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

        private WaferMaterial ResolveFeederWafer()
        {
            return Feeder.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
        }

        private bool IsInputStageEmpty(InputStageUnit stage)
        {
            if (stage == null)
                return true;

            return stage.CurrentWaferMaterial == null &&
                   MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage) == null;
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


