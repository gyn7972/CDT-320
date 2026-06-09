using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederLoadFromCassetteStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckCassetteWaferData,
        MoveCassetteToWaferSlot,
        MoveStageToAvoidPosition,
        MoveStageToLoadPosition,
        CheckPickerAvoidPosition,
        PrepareFeederUnclamp,
        PrepareFeederLiftDown,
        MoveFeederLoadPosition,
        VerifyWaferDetected,
        ClampFeederWafer,
        MoveMaterialDataToFeeder,
        UpdateCassetteData,
        Complete,
        Error
    }

    internal sealed class InputFeederLoadFromCassetteSequence : InputFeederSequenceBase<InputFeederLoadFromCassetteStep>
    {
        public InputFeederLoadFromCassetteSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.LoadFromCassette, "InputFeederLoadFromCassetteSequence")
        {
        }

        protected override InputFeederLoadFromCassetteStep IdleStep { get { return InputFeederLoadFromCassetteStep.Idle; } }
        protected override InputFeederLoadFromCassetteStep InitialStep { get { return InputFeederLoadFromCassetteStep.CheckUnit; } }
        protected override InputFeederLoadFromCassetteStep CompleteStep { get { return InputFeederLoadFromCassetteStep.Complete; } }
        protected override InputFeederLoadFromCassetteStep ErrorStep { get { return InputFeederLoadFromCassetteStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case InputFeederLoadFromCassetteStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederLoadFromCassetteStep.CheckTransferReady));
                    case InputFeederLoadFromCassetteStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());
                    case InputFeederLoadFromCassetteStep.CheckCassetteWaferData:
                        return Task.FromResult(CheckCassetteWaferData());
                    case InputFeederLoadFromCassetteStep.MoveCassetteToWaferSlot:
                        return MoveCassetteToWaferSlotAsync(ct);
                    case InputFeederLoadFromCassetteStep.MoveStageToAvoidPosition:
                        return MoveStageToAvoidPositionAsync(ct);
                    case InputFeederLoadFromCassetteStep.MoveStageToLoadPosition:
                        return MoveStageToLoadPositionAsync(ct);
                    case InputFeederLoadFromCassetteStep.CheckPickerAvoidPosition:
                        return CheckPickerAvoidPositionAsync(ct);
                    case InputFeederLoadFromCassetteStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    case InputFeederLoadFromCassetteStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);
                    case InputFeederLoadFromCassetteStep.MoveFeederLoadPosition:
                        return MoveFeederLoadPositionAsync(ct);
                    case InputFeederLoadFromCassetteStep.VerifyWaferDetected:
                        return VerifyWaferDetectedAsync(ct);
                    case InputFeederLoadFromCassetteStep.ClampFeederWafer:
                        return ClampFeederWaferAsync(ct);
                    case InputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder:
                        return Task.FromResult(MoveMaterialDataToFeeder());
                    case InputFeederLoadFromCassetteStep.UpdateCassetteData:
                        return Task.FromResult(UpdateCassetteData());
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-FEEDER-CST-LOAD-STEP-EX", "InputFeederLoadFromCassetteSequence", "Load from cassette step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            if (!Feeder.CheckWaferCassetteReady(Options.SlotIndex, TransferMode.Load))
                return Fail("IN-FEEDER-CST-READY", Feeder.Name, "Input feeder cassette load condition is not ready.");

            InputCassetteUnit cassette = Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            if (!IsHardwareBypass() && !cassette.CheckWaferCassetteTransferReady(TransferMode.Load))
                return Fail("IN-FEEDER-CST-SENSOR", cassette.Name, "Input cassette is not detected or not ready for transfer.");

            InputStageUnit stage = Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (!IsInputStageEmpty(stage))
                return Fail("IN-FEEDER-STAGE-OCCUPIED", "InputStage", "Input stage must be empty before cassette to feeder load.");

            CurrentStep = InputFeederLoadFromCassetteStep.CheckCassetteWaferData;
            return 0;
        }

        private int CheckCassetteWaferData()
        {
            InputCassetteUnit cassette = Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            int nextSlot = cassette.FindNextProcessWaferSlot();
            if (Options.RunMode == SequenceRunMode.Auto &&
                nextSlot >= 0 &&
                Options.SlotIndex != nextSlot)
            {
                return Fail("IN-FEEDER-CST-SLOT-MISMATCH", cassette.Name,
                    "Selected slot is not current process slot. selected=" + Options.SlotIndex + ", current=" + nextSlot);
            }

            if (!IsSelectedSlotProcessReady(cassette, Options.SlotIndex))
            {
                return Fail("IN-FEEDER-CST-SLOT-NOT-READY", cassette.Name,
                    "Selected cassette slot is not ready for wafer loading. slot=" + Options.SlotIndex);
            }

            WaferMaterial wafer = ResolveCassetteWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-CST-WAFER-DATA", "Material", "Mapped cassette wafer data was not found. role=" + Options.CassetteRole + ", slot=" + Options.SlotIndex);

            CurrentStep = InputFeederLoadFromCassetteStep.MoveCassetteToWaferSlot;
            return 0;
        }

        private async Task<int> MoveCassetteToWaferSlotAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            InputCassetteUnit cassette = Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            int result = await AwaitStepWithCancellationAsync(
                cassette.PrepareWaferCassetteForFeederLoad(Options.SlotIndex, ResolveTimeout(), Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-CST-SLOT-MOVE", cassette.Name, "Input cassette slot move failed. slot=" + Options.SlotIndex + ", result=" + result);

            CurrentStep = InputFeederLoadFromCassetteStep.MoveStageToAvoidPosition;
            return 0;
        }

        private async Task<int> MoveStageToAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            InputStageUnit stage = Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit is not available.");
            if (stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-RECIPE", stage.Name, "Input stage recipe is not available.");

            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.VisionX, stage.Recipe.VisionX.AvoidPosition, "VisionX avoid", ct).ConfigureAwait(false);
            if (result != 0) return result;

            Task<int> needleZMove = MoveStageAxisCommandAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, "NeedleZ avoid", ct);
            Task<int> ejectPinZMove = MoveStageAxisCommandAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, "EjectPinZ avoid", ct);
            int[] zResults = await Task.WhenAll(needleZMove, ejectPinZMove).ConfigureAwait(false);
            if (zResults[0] != 0) return zResults[0];
            if (zResults[1] != 0) return zResults[1];

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

            CurrentStep = InputFeederLoadFromCassetteStep.MoveStageToLoadPosition;
            return 0;
        }

        private async Task<int> MoveStageToLoadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            InputStageUnit stage = Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            int result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferY, stage.Recipe.WaferY.LoadPosition, "StageY load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferT, stage.Recipe.WaferT.LoadPosition, "StageT load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await MoveStageAxisAndVerifyAsync(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.LoadPosition, "StageZ load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederLoadFromCassetteStep.CheckPickerAvoidPosition;
            return 0;
        }

        private async Task<int> CheckPickerAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            PickerFrontUnit front = Context.Machine != null ? Context.Machine.PickerFrontUnit : null;
            if (front != null && !front.IsFrontPickerInAvoidPosition())
            {
                int result = await AwaitStepWithCancellationAsync(front.MoveToFrontPickerAvoidPosition(Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0 || !front.IsFrontPickerInAvoidPosition())
                    return Fail("IN-FEEDER-FRONT-PICKER-AVOID", front.Name, "FrontPickerX/FrontPicker avoid position check failed. result=" + result);
            }

            PickerRearUnit rear = Context.Machine != null ? Context.Machine.PickerRearUnit : null;
            if (rear != null && !rear.IsRearPickerInAvoidPosition())
            {
                int result = await AwaitStepWithCancellationAsync(rear.MoveToRearPickerAvoidPosition(Options.FineMove), ct).ConfigureAwait(false);
                if (result != 0 || !rear.IsRearPickerInAvoidPosition())
                    return Fail("IN-FEEDER-REAR-PICKER-AVOID", rear.Name, "RearPickerX/RearPicker avoid position check failed. result=" + result);
            }

            CurrentStep = InputFeederLoadFromCassetteStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederUnclamp())
                return Fail("IN-FEEDER-UNCLAMP", Feeder.Name, "WaferFeeder unclamp command failed. result=" + result);

            CurrentStep = InputFeederLoadFromCassetteStep.PrepareFeederLiftDown;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederUpDownAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-LIFT-DOWN", Feeder.Name, "WaferFeeder lift down command failed. result=" + result);

            CurrentStep = InputFeederLoadFromCassetteStep.MoveFeederLoadPosition;
            return 0;
        }

        private async Task<int> MoveFeederLoadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederCassetteLoadPosition(Options.SlotIndex, Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-LOAD-POS", Feeder.Name, "WaferFeeder cassette load position move command failed. result=" + result);

            bool done = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederYMoveDone(ResolveTimeout()), ct).ConfigureAwait(false);
            if (!done)
                return Fail("IN-FEEDER-LOAD-POS-TIMEOUT", Feeder.Name, "WaferFeeder cassette load position timeout.");

            CurrentStep = InputFeederLoadFromCassetteStep.VerifyWaferDetected;
            return 0;
        }

        private async Task<int> VerifyWaferDetectedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer = ResolveCassetteWafer();
            bool hasData = wafer != null;
            bool bypass = IsHardwareBypass();

            if (!hasData)
                return Fail("IN-FEEDER-WAFER-DATA-MISSING", "Material", "Wafer data disappeared before feeder clamp. role=" + Options.CassetteRole + ", slot=" + Options.SlotIndex);

            if (!bypass)
            {
                bool detected = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederRingState(true, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("IN-FEEDER-WAFER-SENSOR", Feeder.Name, "Wafer sensor timeout or data/sensor mismatch before clamp. waferId=" + wafer.WaferId);
            }

            CurrentStep = InputFeederLoadFromCassetteStep.ClampFeederWafer;
            return 0;
        }

        private async Task<int> ClampFeederWaferAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(true, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederClamp())
                return Fail("IN-FEEDER-CLAMP", Feeder.Name, "WaferFeeder clamp command failed. result=" + result);

            if (!IsHardwareBypass() && !Feeder.IsWaferFeederRingDetected(true))
                return Fail("IN-FEEDER-CLAMP-WAFER-SENSOR", Feeder.Name, "Wafer sensor is not detected after feeder clamp.");

            CurrentStep = InputFeederLoadFromCassetteStep.MoveMaterialDataToFeeder;
            return 0;
        }

        private int MoveMaterialDataToFeeder()
        {
            WaferMaterial wafer = ResolveCassetteWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-MATERIAL-MOVE", "Material", "Cassette wafer data was not found for feeder material move.");

            Feeder.SetCurrentWaferMaterial(wafer);
            MaterialStateService.MoveWaferToInputFeeder(wafer);

            CurrentStep = InputFeederLoadFromCassetteStep.UpdateCassetteData;
            return 0;
        }

        private int UpdateCassetteData()
        {
            InputCassetteUnit cassette = Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
            if (cassette != null)
                cassette.UpdateWaferCassetteSlotState(Options.SlotIndex, SlotPresence.Exist, ProcessState.Processing);

            Context.Bus.Set("InputFeederOccupied");
            CurrentStep = InputFeederLoadFromCassetteStep.Complete;
            return 0;
        }

        private async Task<int> MoveStageAxisCommandAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(stage.MoveInputStageAxis(axis, target, Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-MOVE", stage.Name, description + " move failed. result=" + result);

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
            bool arrived = await WaitStageAxisInPositionAsync(stage, axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
            if (!arrived)
                return Fail("IN-FEEDER-STAGE-MOVE-TIMEOUT", stage.Name, description + " move done timeout.");

            return 0;
        }

        private int CheckStageAxisInPosition(InputStageUnit stage, WaferStageAxis axis, double target, string description)
        {
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return Fail("IN-FEEDER-STAGE-AXIS", stage.Name, description + " axis is not available.");

            if (item.IsMoving || item.IsAlarm || !IsStageAxisInPosition(item, target))
                return Fail("IN-FEEDER-STAGE-POSITION", stage.Name, description + " position check failed.");

            return 0;
        }

        private async Task<bool> WaitStageAxisInPositionAsync(
            InputStageUnit stage,
            WaferStageAxis axis,
            double target,
            int timeoutMs,
            CancellationToken ct)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return false;

            DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs > 0 ? timeoutMs : 10000);
            while (DateTime.Now <= deadline)
            {
                ct.ThrowIfCancellationRequested();
                if (!item.IsMoving && IsStageAxisInPosition(item, target))
                    return true;

                await Task.Delay(20, ct).ConfigureAwait(false);
            }

            return !item.IsMoving && IsStageAxisInPosition(item, target);
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

        private WaferMaterial ResolveCassetteWafer()
        {
            return MaterialStateService.GetWaferInCassette(Options.CassetteRole, Options.SlotIndex);
        }

        private bool IsInputStageEmpty(InputStageUnit stage)
        {
            if (stage == null)
                return true;

            return stage.CurrentWaferMaterial == null &&
                   MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage) == null;
        }

        private bool IsSelectedSlotProcessReady(InputCassetteUnit cassette, int slotIndex)
        {
            if (cassette == null || slotIndex < 0)
                return false;

            WaferCassetteMaterial material = cassette.GetWaferMaterialCassette();
            if (material == null || material.Slots == null || slotIndex >= material.Slots.Count)
                return false;

            WaferSlotState state = material.Slots[slotIndex];
            if (state == null)
                return false;

            return state.Presence == SlotPresence.Exist &&
                   (state.Process == ProcessState.Ready || state.Process == ProcessState.Unknown);
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
