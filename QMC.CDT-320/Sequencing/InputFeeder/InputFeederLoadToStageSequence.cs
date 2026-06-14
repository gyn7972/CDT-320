using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

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
                    case InputFeederLoadToStageStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederLoadToStageStep.CheckTransferReady));
                    case InputFeederLoadToStageStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());
                    case InputFeederLoadToStageStep.RunBarcodeSequence:
                        return Task.FromResult(RunBarcodeSequence());
                    case InputFeederLoadToStageStep.CheckStageLoadPosition:
                        return Task.FromResult(CheckStageLoadPosition());
                    case InputFeederLoadToStageStep.VerifyFeederHoldingWafer:
                        return Task.FromResult(VerifyFeederHoldingWafer());
                    case InputFeederLoadToStageStep.MoveFeederStageLoadPosition:
                        return MoveFeederStageLoadPositionAsync(ct);
                    case InputFeederLoadToStageStep.VerifyWaferBeforeTransfer:
                        return VerifyWaferBeforeTransferAsync(ct);
                    case InputFeederLoadToStageStep.StageVacuumOn:
                        return Task.FromResult(StageVacuumOn());
                    case InputFeederLoadToStageStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    case InputFeederLoadToStageStep.MoveFeederStageLoadAvoidPosition:
                        return MoveFeederStageLoadAvoidPositionAsync(ct);
                    case InputFeederLoadToStageStep.MoveMaterialDataToStage:
                        return Task.FromResult(MoveMaterialDataToStage());
                    case InputFeederLoadToStageStep.ClearFeederData:
                        return Task.FromResult(ClearFeederData());
                    case InputFeederLoadToStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);
                    case InputFeederLoadToStageStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);
                    case InputFeederLoadToStageStep.PrepareFeederLiftDownAfterAvoid:
                        return PrepareFeederLiftDownAsync(ct, InputFeederLoadToStageStep.VerifyInputStageData);
                    case InputFeederLoadToStageStep.VerifyInputStageData:
                        return Task.FromResult(VerifyInputStageData());
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

            CurrentStep = InputFeederLoadToStageStep.Complete;
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
                    description + " position check failed. Stage axis is checked only; no stage move is commanded in LoadToStage. " +
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
                case WaferStageAxis.WaferY: return stage.StageY;
                case WaferStageAxis.WaferT: return stage.StageT;
                case WaferStageAxis.WaferExpandingZ: return stage.ExpanderZ;
                case WaferStageAxis.VisionX: return stage.CameraX;
                case WaferStageAxis.NeedleX: return stage.NeedleBlockX;
                case WaferStageAxis.NeedleZ: return stage.NeedleZ;
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
