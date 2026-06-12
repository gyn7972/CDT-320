using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederUnloadFromStageStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckStageWaferData,
        MoveStageToAvoidPosition,
        MoveStageToUnloadPosition,
        CheckPickerAvoidPosition,
        PrepareFeederLiftUp,
        PrepareFeederUnclamp,
        VerifyStageWaferBeforeTransfer,
        StageVacuumOff,
        MoveStageToUnloadOffsetPosition,
        PrepareFeederLiftDown,
        MoveFeederUnloadPosition,
        ClampFeederWafer,
        VerifyFeederWaferDetected,
        TransferStageToFeeder,
        MoveMaterialDataToFeeder,
        VerifyTransferData,
        Complete,
        Error
    }

    internal sealed class InputFeederUnloadFromStageSequence : InputFeederSequenceBase<InputFeederUnloadFromStageStep>
    {
        public InputFeederUnloadFromStageSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.UnloadFromStage, "InputFeederUnloadFromStageSequence")
        {
        }

        protected override InputFeederUnloadFromStageStep IdleStep { get { return InputFeederUnloadFromStageStep.Idle; } }
        protected override InputFeederUnloadFromStageStep InitialStep { get { return InputFeederUnloadFromStageStep.CheckUnit; } }
        protected override InputFeederUnloadFromStageStep CompleteStep { get { return InputFeederUnloadFromStageStep.Complete; } }
        protected override InputFeederUnloadFromStageStep ErrorStep { get { return InputFeederUnloadFromStageStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case InputFeederUnloadFromStageStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederUnloadFromStageStep.CheckTransferReady));
                    case InputFeederUnloadFromStageStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());
                    case InputFeederUnloadFromStageStep.CheckStageWaferData:
                        return Task.FromResult(CheckStageWaferData());
                    case InputFeederUnloadFromStageStep.MoveStageToAvoidPosition:
                        return MoveStageToAvoidPositionAsync(ct);
                    case InputFeederUnloadFromStageStep.MoveStageToUnloadPosition:
                        return MoveStageToUnloadPositionAsync(ct);
                    case InputFeederUnloadFromStageStep.CheckPickerAvoidPosition:
                        return CheckPickerAvoidPositionAsync(ct);
                    case InputFeederUnloadFromStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);
                    case InputFeederUnloadFromStageStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    case InputFeederUnloadFromStageStep.VerifyStageWaferBeforeTransfer:
                        return VerifyStageWaferBeforeTransferAsync(ct);
                    case InputFeederUnloadFromStageStep.StageVacuumOff:
                        return Task.FromResult(StageVacuumOff());
                    case InputFeederUnloadFromStageStep.MoveStageToUnloadOffsetPosition:
                        return MoveStageToUnloadOffsetPositionAsync(ct);
                    case InputFeederUnloadFromStageStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);
                    case InputFeederUnloadFromStageStep.MoveFeederUnloadPosition:
                        return MoveFeederUnloadPositionAsync(ct);
                    case InputFeederUnloadFromStageStep.ClampFeederWafer:
                        return ClampFeederWaferAsync(ct);
                    case InputFeederUnloadFromStageStep.VerifyFeederWaferDetected:
                        return VerifyFeederWaferDetectedAsync(ct);
                    case InputFeederUnloadFromStageStep.TransferStageToFeeder:
                        return Task.FromResult(TransferStageToFeeder());
                    case InputFeederUnloadFromStageStep.MoveMaterialDataToFeeder:
                        return Task.FromResult(MoveMaterialDataToFeeder());
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

            if (!Feeder.IsWaferFeederEmpty())
                return Fail("IN-FEEDER-OCCUPIED", Feeder.Name, "InputFeeder must be empty before stage to feeder unload.");

            InputStageUnit stage = ResolveStage();
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            if (ResolveStageWafer() == null)
                return Fail("IN-FEEDER-STAGE-WAFER-DATA", "Material", "InputStage wafer data was not found.");

            CurrentStep = InputFeederUnloadFromStageStep.CheckStageWaferData;
            return 0;
        }

        private int CheckStageWaferData()
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-STAGE-WAFER-DATA", "Material", "InputStage wafer data was not found.");

            InputStageUnit stage = ResolveStage();
            if (stage != null && stage.CurrentWaferMaterial == null)
                stage.SetCurrentWaferMaterial(wafer);

            CurrentStep = InputFeederUnloadFromStageStep.MoveStageToAvoidPosition;
            return 0;
        }

        private async Task<int> MoveStageToAvoidPositionAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.VisionX, stage.Recipe.VisionX.AvoidPosition, "VisionX avoid", ct).ConfigureAwait(false);
            if (result != 0) return result;

            Task<int> needleZMove = MoveStageAxisCommandAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, "NeedleZ avoid", ct);
            Task<int> ejectPinZMove = MoveStageAxisCommandAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, "EjectPinZ avoid", ct);
            int[] zMoveResults = await Task.WhenAll(needleZMove, ejectPinZMove).ConfigureAwait(false);
            if (zMoveResults[0] != 0) return zMoveResults[0];
            if (zMoveResults[1] != 0) return zMoveResults[1];

            //이거 필요한거 맞아?
            Task<int> needleZWait = WaitStageAxisInPositionResultAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, "NeedleZ avoid", ct);
            Task<int> ejectPinZWait = WaitStageAxisInPositionResultAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, "EjectPinZ avoid", ct);
            int[] zWaitResults = await Task.WhenAll(needleZWait, ejectPinZWait).ConfigureAwait(false);
            if (zWaitResults[0] != 0) return zWaitResults[0];
            if (zWaitResults[1] != 0) return zWaitResults[1];

            result = CheckStageAxisInPosition(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, "NeedleZ avoid");
            if (result != 0) return result;

            result = CheckStageAxisInPosition(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, "EjectPinZ avoid");
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

            CurrentStep = InputFeederUnloadFromStageStep.CheckPickerAvoidPosition;
            return 0;
        }

        private async Task<int> CheckPickerAvoidPositionAsync(CancellationToken ct)
        {
            PickerFrontUnit front = Context.Machine != null ? Context.Machine.PickerFrontUnit : null;
            if (front != null && !front.IsFrontPickerInAvoidPosition())
            {
                int result = await AwaitStepWithCancellationAsync(front.MoveToFrontPickerAvoidPosition(Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-FEEDER-FRONT-PICKER-AVOID", front.Name,
                        "FrontPicker avoid move command failed. result=" + result +
                        ", pickerX=" + BuildPickerXAxisState(front.PickerX));

                bool arrived = await WaitUntilAsync(() => front.IsFrontPickerInAvoidPosition(), ResolveTimeout(), ct).ConfigureAwait(false);
                if (!arrived)
                    return Fail("IN-FEEDER-FRONT-PICKER-AVOID-TIMEOUT", front.Name, "FrontPicker avoid position timeout.");
            }

            PickerRearUnit rear = Context.Machine != null ? Context.Machine.PickerRearUnit : null;
            if (rear != null && !rear.IsRearPickerInAvoidPosition())
            {
                int result = await AwaitStepWithCancellationAsync(rear.MoveToRearPickerAvoidPosition(Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-FEEDER-REAR-PICKER-AVOID", rear.Name,
                        "RearPicker avoid move command failed. result=" + result +
                        ", pickerX=" + BuildPickerXAxisState(rear.PickerX));

                bool arrived = await WaitUntilAsync(() => rear.IsRearPickerInAvoidPosition(), ResolveTimeout(), ct).ConfigureAwait(false);
                if (!arrived)
                    return Fail("IN-FEEDER-REAR-PICKER-AVOID-TIMEOUT", rear.Name, "RearPicker avoid position timeout.");
            }

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

            CurrentStep = InputFeederUnloadFromStageStep.PrepareFeederUnclamp;
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

            CurrentStep = InputFeederUnloadFromStageStep.VerifyStageWaferBeforeTransfer;
            return 0;
        }

        private async Task<int> VerifyStageWaferBeforeTransferAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-STAGE-WAFER-DATA-MISSING", "Material", "InputStage wafer data disappeared before stage to feeder transfer.");

            CurrentStep = InputFeederUnloadFromStageStep.StageVacuumOff;
            await Task.CompletedTask.ConfigureAwait(false);
            return 0;
        }

        private int StageVacuumOff()
        {
            InputStageUnit stage = ResolveStage();
            if (stage != null && stage.NeedleVacuum != null && Options.UseVacuum)
                stage.NeedleVacuum.Off();

            CurrentStep = InputFeederUnloadFromStageStep.MoveStageToUnloadOffsetPosition;
            return 0;
        }

        private async Task<int> MoveStageToUnloadOffsetPositionAsync(CancellationToken ct)
        {
            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            double target = stage.Recipe.WaferY.UnloadPosition + Options.StageUnloadOffset;
            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferY, target, "StageY unload offset", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederUnloadFromStageStep.PrepareFeederLiftDown;
            return 0;
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

            CurrentStep = InputFeederUnloadFromStageStep.MoveFeederUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederUnloadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederStageUnloadPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-UNLOAD-POS", Feeder.Name,
                    "WaferFeeder stage unload position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            bool done = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederYMoveDone(ResolveTimeout()), ct).ConfigureAwait(false);
            if (!done || !Feeder.IsWaferFeederInStageUnloadPosition())
                return Fail("IN-FEEDER-STAGE-UNLOAD-POS-TIMEOUT", Feeder.Name,
                    "WaferFeeder stage unload position timeout. done=" + done + ". " + Feeder.GetWaferFeederTransferState());

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

            CurrentStep = InputFeederUnloadFromStageStep.VerifyFeederWaferDetected;
            return 0;
        }

        private async Task<int> VerifyFeederWaferDetectedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-STAGE-WAFER-DATA-MISSING", "Material", "InputStage wafer data disappeared before feeder detection check.");

            if (!IsHardwareBypass())
            {
                bool detected = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederRingState(true, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("IN-FEEDER-STAGE-UNLOAD-RING", Feeder.Name, "WaferFeeder ring was not detected after stage unload. waferId=" + wafer.WaferId);
            }

            CurrentStep = InputFeederUnloadFromStageStep.TransferStageToFeeder;
            return 0;
        }

        private int TransferStageToFeeder()
        {
            if (ResolveStageWafer() == null)
                return Fail("IN-FEEDER-STAGE-WAFER-DATA-MISSING", "Material", "InputStage wafer data was not found at transfer step.");

            CurrentStep = InputFeederUnloadFromStageStep.MoveMaterialDataToFeeder;
            return 0;
        }

        private int MoveMaterialDataToFeeder()
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-MATERIAL-MOVE", "Material", "InputStage wafer data was not found for feeder material move.");

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

            result = await WaitStageAxisInPositionResultAsync(stage, axis, target, description, ct).ConfigureAwait(false);
            if (result != 0) return result;

            return CheckStageAxisInPosition(stage, axis, target, description);
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

            if (!IsStageAxisInPosition(item, target))
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
            if (item == null || item.IsMoving || item.IsAlarm)
                return false;

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(item.ActualPosition - target) <= tolerance;
        }

        private string BuildPickerXAxisState(QMC.Common.Motion.BaseAxis axis)
        {
            if (axis == null)
                return "axis=null";

            return "name=" + axis.Name +
                   ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (axis.IsMoving ? "Y" : "N") +
                   ", actual=" + axis.ActualPosition +
                   ", command=" + axis.CommandPosition;
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

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs, CancellationToken ct)
        {
            DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs > 0 ? timeoutMs : 10000);
            while (DateTime.Now <= deadline)
            {
                ct.ThrowIfCancellationRequested();
                if (condition != null && condition())
                    return true;

                await Task.Delay(20, ct).ConfigureAwait(false);
            }

            return condition != null && condition();
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
