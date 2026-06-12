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
        MoveStageToLoadPosition,
        PrepareFeederLiftDown,
        PrepareFeederUnclamp,
        VerifyWaferBeforeTransfer,
        StageVacuumOn,
        MoveStageToLoadOffsetPosition,
        MoveStageZToUnloadPosition,
        TransferFeederToStage,
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
                    case InputFeederLoadToStageStep.MoveStageToLoadPosition:
                        return MoveStageToLoadPositionAsync(ct);
                    case InputFeederLoadToStageStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct, InputFeederLoadToStageStep.PrepareFeederUnclamp);
                    case InputFeederLoadToStageStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    case InputFeederLoadToStageStep.VerifyWaferBeforeTransfer:
                        return VerifyWaferBeforeTransferAsync(ct);
                    case InputFeederLoadToStageStep.StageVacuumOn:
                        return Task.FromResult(StageVacuumOn());
                    case InputFeederLoadToStageStep.MoveStageToLoadOffsetPosition:
                        return MoveStageToLoadOffsetPositionAsync(ct);
                    case InputFeederLoadToStageStep.MoveStageZToUnloadPosition:
                        return MoveStageZToUnloadPositionAsync(ct);
                    case InputFeederLoadToStageStep.TransferFeederToStage:
                        return ExecuteFeederToStageTransferAsync(ct);
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

        private int RunBarcodeSequence()
        {
            if (Options.UseBarcode)
                WriteLog("InputFeederLoadToStageSequence", "Barcode sequence placeholder. Feeder to stage load will continue after barcode sequence hook. - Ok");

            CurrentStep = InputFeederLoadToStageStep.MoveStageToLoadPosition;
            return 0;
        }

        private async Task<int> MoveStageToLoadPositionAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferY, stage.Recipe.WaferY.LoadPosition, "StageY load", ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferT, stage.Recipe.WaferT.LoadPosition, "StageT load", ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.LoadPosition, "StageZ load", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederLoadToStageStep.PrepareFeederLiftDown;
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

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederUnclamp())
                return Fail("IN-FEEDER-UNCLAMP", Feeder.Name,
                    "WaferFeeder unclamp command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

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

            CurrentStep = InputFeederLoadToStageStep.MoveStageToLoadOffsetPosition;
            return 0;
        }

        private async Task<int> MoveStageToLoadOffsetPositionAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            double target = stage.Recipe.WaferY.LoadPosition + Options.StageLoadOffset;
            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferY, target, "StageY load offset", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederLoadToStageStep.MoveStageZToUnloadPosition;
            return 0;
        }

        private async Task<int> MoveStageZToUnloadPositionAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.UnloadPosition, "StageZ unload", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederLoadToStageStep.TransferFeederToStage;
            return 0;
        }

        private async Task<int> ExecuteFeederToStageTransferAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsHardwareBypass())
            {
                bool cleared = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederRingState(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("IN-FEEDER-STAGE-TRANSFER-SENSOR", Feeder.Name, "WaferFeeder ring remained after feeder to stage transfer.");
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

            bool done = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederYMoveDone(ResolveTimeout()), ct).ConfigureAwait(false);
            if (!done || !Feeder.IsWaferFeederInAvoidPosition())
                return Fail("IN-FEEDER-AVOID-TIMEOUT", Feeder.Name,
                    "WaferFeeder avoid position timeout. done=" + done + ". " + Feeder.GetWaferFeederTransferState());

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

        private async Task<int> MoveStageAxisCommandAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(stage.MoveInputStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-MOVE", stage.Name, description + " move failed. result=" + result + ", " + BuildStageAxisState(stage, axis, target));

            ct.ThrowIfCancellationRequested();
            return 0;
        }

        private async Task<int> MoveStageAxisAndVerifyAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            int result = await MoveStageAxisCommandAsync(stage, axis, target, description, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitStageAxisInPositionResultAsync(stage, axis, target, description, ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            return CheckStageAxisInPosition(stage, axis, target, description);
        }

        private async Task<int> WaitStageAxisInPositionResultAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(stage.WaitInputStageAxisInPosition(axis, target, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-MOVE-TIMEOUT", stage.Name, description + " move done timeout. waitResult=" + result + ", " + BuildStageAxisState(stage, axis, target));

            return 0;
        }

        private int CheckStageAxisInPosition(InputStageUnit stage, WaferStageAxis axis, double target, string description)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return Fail("IN-FEEDER-STAGE-AXIS", stage != null ? stage.Name : "InputStage", description + " axis is not available. " + BuildStageAxisState(stage, axis, target));

            if (item.IsMoving || item.IsAlarm || !IsStageAxisInPosition(item, target))
                return Fail("IN-FEEDER-STAGE-POSITION", stage.Name, description + " final position check failed. " + BuildStageAxisState(stage, axis, target));

            return 0;
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

        private static bool IsStageAxisInPosition(QMC.Common.Motion.BaseAxis item, double target)
        {
            if (item == null)
                return false;

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(item.ActualPosition - target) <= tolerance;
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
                   ", tolerance=" + tolerance;
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
