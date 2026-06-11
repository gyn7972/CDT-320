using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederUnloadToCassetteStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckFeederWaferData,
        CheckCassetteSlotEmpty,
        MoveCassetteToUnloadOffsetPosition,
        PrepareFeederClamp,
        VerifyFeederLiftDown,
        VerifyWaferDetected,
        MoveFeederUnloadPosition,
        PrepareFeederUnclamp,
        VerifyWaferCleared,
        MoveMaterialDataToCassette,
        UpdateCassetteData,
        MoveFeederPostUnloadPosition,
        MoveCassetteToSlotPosition,
        VerifyTransferData,
        Complete,
        Error
    }

    internal sealed class InputFeederUnloadToCassetteSequence : InputFeederSequenceBase<InputFeederUnloadToCassetteStep>
    {
        public InputFeederUnloadToCassetteSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.UnloadToCassette, "InputFeederUnloadToCassetteSequence")
        {
        }

        protected override InputFeederUnloadToCassetteStep IdleStep { get { return InputFeederUnloadToCassetteStep.Idle; } }
        protected override InputFeederUnloadToCassetteStep InitialStep { get { return InputFeederUnloadToCassetteStep.CheckUnit; } }
        protected override InputFeederUnloadToCassetteStep CompleteStep { get { return InputFeederUnloadToCassetteStep.Complete; } }
        protected override InputFeederUnloadToCassetteStep ErrorStep { get { return InputFeederUnloadToCassetteStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case InputFeederUnloadToCassetteStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederUnloadToCassetteStep.CheckTransferReady));
                    case InputFeederUnloadToCassetteStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());
                    case InputFeederUnloadToCassetteStep.CheckFeederWaferData:
                        return Task.FromResult(CheckFeederWaferData());
                    case InputFeederUnloadToCassetteStep.CheckCassetteSlotEmpty:
                        return Task.FromResult(CheckCassetteSlotEmpty());
                    case InputFeederUnloadToCassetteStep.MoveCassetteToUnloadOffsetPosition:
                        return MoveCassetteToUnloadOffsetPositionAsync(ct);
                    case InputFeederUnloadToCassetteStep.PrepareFeederClamp:
                        return PrepareFeederClampAsync(ct);
                    case InputFeederUnloadToCassetteStep.VerifyFeederLiftDown:
                        return VerifyFeederLiftDownAsync(ct);
                    case InputFeederUnloadToCassetteStep.VerifyWaferDetected:
                        return VerifyWaferDetectedAsync(ct);
                    case InputFeederUnloadToCassetteStep.MoveFeederUnloadPosition:
                        return MoveFeederUnloadPositionAsync(ct);
                    case InputFeederUnloadToCassetteStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    case InputFeederUnloadToCassetteStep.VerifyWaferCleared:
                        return VerifyWaferClearedAsync(ct);
                    case InputFeederUnloadToCassetteStep.MoveMaterialDataToCassette:
                        return Task.FromResult(MoveMaterialDataToCassette());
                    case InputFeederUnloadToCassetteStep.UpdateCassetteData:
                        return Task.FromResult(UpdateCassetteData());
                    case InputFeederUnloadToCassetteStep.MoveFeederPostUnloadPosition:
                        return MoveFeederPostUnloadPositionAsync(ct);
                    case InputFeederUnloadToCassetteStep.MoveCassetteToSlotPosition:
                        return MoveCassetteToSlotPositionAsync(ct);
                    case InputFeederUnloadToCassetteStep.VerifyTransferData:
                        return Task.FromResult(VerifyTransferData());
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-FEEDER-CST-UNLOAD-STEP-EX", "InputFeederUnloadToCassetteSequence", "Unload to cassette step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            if (!Feeder.CheckWaferFeederMoveReady())
                return Fail("IN-FEEDER-CST-UNLOAD-READY", Feeder.Name, "Input feeder is not move ready. state=" + Feeder.GetWaferFeederTransferState());

            if (!Feeder.HasWaferOnFeeder())
                return Fail("IN-FEEDER-WAFER-MISSING", Feeder.Name, "InputFeeder must have wafer before cassette unload.");

            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            if (!IsHardwareBypass() && !cassette.CheckWaferCassetteTransferReady(TransferMode.Unload))
                return Fail("IN-FEEDER-CST-SENSOR", cassette.Name, "Input cassette is not detected or not ready for unload.");

            if (!cassette.CheckWaferCassetteMoveReady())
                return Fail("IN-FEEDER-CST-MOVE-READY", cassette.Name, "Input cassette lifter is not move ready.");

            CurrentStep = InputFeederUnloadToCassetteStep.CheckFeederWaferData;
            return 0;
        }

        private int CheckFeederWaferData()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-WAFER-DATA", "Material", "InputFeeder wafer data was not found before cassette unload.");

            CurrentStep = InputFeederUnloadToCassetteStep.CheckCassetteSlotEmpty;
            return 0;
        }

        private int CheckCassetteSlotEmpty()
        {
            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            int unloadSlot = ResolveUnloadSlotIndex();
            if (!IsUnloadSlotEmpty(cassette, unloadSlot))
                return Fail("IN-FEEDER-CST-SLOT-OCCUPIED", cassette.Name, "Unload cassette slot must be empty. slot=" + unloadSlot);

            CurrentStep = InputFeederUnloadToCassetteStep.MoveCassetteToUnloadOffsetPosition;
            return 0;
        }

        private async Task<int> MoveCassetteToUnloadOffsetPositionAsync(CancellationToken ct)
        {
            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            int unloadSlot = ResolveUnloadSlotIndex();
            double target = cassette.CalculateWaferCassetteSlotTargetPosition(unloadSlot) + ResolveCassetteUnloadOffset(cassette);
            int result = await MoveCassetteZAndVerifyAsync(cassette, target, "cassette unload offset", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederUnloadToCassetteStep.PrepareFeederClamp;
            return 0;
        }

        private async Task<int> PrepareFeederClampAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(true, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederClamp())
                return Fail("IN-FEEDER-CLAMP", Feeder.Name, "WaferFeeder clamp command failed. result=" + result);

            CurrentStep = InputFeederUnloadToCassetteStep.VerifyFeederLiftDown;
            return 0;
        }

        private async Task<int> VerifyFeederLiftDownAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!Feeder.IsWaferFeederDown())
            {
                int result = await AwaitStepWithCancellationAsync(
                    Feeder.SetWaferFeederUpDownAsync(false, ResolveTimeout(), ct),
                    ct).ConfigureAwait(false);
                if (result != 0 || !Feeder.IsWaferFeederDown())
                    return Fail("IN-FEEDER-LIFT-DOWN", Feeder.Name, "WaferFeeder lift down command failed. result=" + result);
            }

            CurrentStep = InputFeederUnloadToCassetteStep.VerifyWaferDetected;
            return 0;
        }

        private async Task<int> VerifyWaferDetectedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-WAFER-DATA-MISSING", "Material", "InputFeeder wafer data disappeared before cassette unload.");

            if (!IsHardwareBypass())
            {
                bool detected = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederRingState(true, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("IN-FEEDER-CST-UNLOAD-WAFER-SENSOR", Feeder.Name, "Wafer sensor timeout or data/sensor mismatch before cassette unload. waferId=" + wafer.WaferId);
            }

            CurrentStep = InputFeederUnloadToCassetteStep.MoveFeederUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederUnloadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederCassetteUnloadPosition(ResolveUnloadSlotIndex(), Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-CST-UNLOAD-POS", Feeder.Name, "WaferFeeder cassette unload position move command failed. result=" + result);

            bool done = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederYMoveDone(ResolveTimeout()), ct).ConfigureAwait(false);
            int unloadSlot = ResolveUnloadSlotIndex();
            if (!done || !Feeder.IsWaferFeederInCassetteUnloadPosition(unloadSlot))
                return Fail("IN-FEEDER-CST-UNLOAD-POS-TIMEOUT", Feeder.Name, "WaferFeeder cassette unload position timeout. slot=" + unloadSlot);

            CurrentStep = InputFeederUnloadToCassetteStep.PrepareFeederUnclamp;
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

            CurrentStep = InputFeederUnloadToCassetteStep.VerifyWaferCleared;
            return 0;
        }

        private async Task<int> VerifyWaferClearedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsHardwareBypass())
            {
                bool cleared = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederRingState(false, ResolveTimeout()), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("IN-FEEDER-CST-UNLOAD-RING", Feeder.Name, "WaferFeeder ring remained after cassette unload.");
            }

            CurrentStep = InputFeederUnloadToCassetteStep.MoveMaterialDataToCassette;
            return 0;
        }

        private int MoveMaterialDataToCassette()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-MATERIAL-CST", "Material", "InputFeeder wafer data was not found for cassette material move.");

            InputCassetteUnit cassette = ResolveCassette();
            int unloadSlot = ResolveUnloadSlotIndex(wafer);
            double slotPosition = cassette != null ? cassette.CalculateWaferCassetteSlotTargetPosition(unloadSlot) : wafer.SourceCassetteSlotPosition;

            MaterialStateService.PutWaferInCassette(
                wafer.WaferId,
                Options.CassetteRole,
                unloadSlot,
                wafer.CassetteLotId,
                slotPosition,
                WaferMaterialState.Finish);

            Feeder.ClearCurrentWaferMaterial();
            Context.Bus.Set("InputFeederEmpty");
            CurrentStep = InputFeederUnloadToCassetteStep.UpdateCassetteData;
            return 0;
        }

        private int UpdateCassetteData()
        {
            InputCassetteUnit cassette = ResolveCassette();
            if (cassette != null)
            {
                cassette.UpdateWaferCassetteSlotState(ResolveUnloadSlotIndex(), SlotPresence.Exist, ProcessState.Done);
                if (cassette.IsInputCassetteProcessComplete())
                    cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);
            }

            CurrentStep = InputFeederUnloadToCassetteStep.MoveFeederPostUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederPostUnloadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            bool exchange = Options.PostUnloadMove == InputFeederPostUnloadMove.Exchange;
            int result = await AwaitStepWithCancellationAsync(
                exchange
                    ? Feeder.MoveToWaferFeederExchangePosition(Options.FineMove)
                    : Feeder.MoveToWaferFeederAvoidPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-POST-UNLOAD-MOVE", Feeder.Name, "WaferFeeder post unload position move command failed. result=" + result);

            bool done = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederYMoveDone(ResolveTimeout()), ct).ConfigureAwait(false);
            bool arrived = exchange ? Feeder.IsWaferFeederInExchangePosition() : Feeder.IsWaferFeederInAvoidPosition();
            if (!done || !arrived)
                return Fail("IN-FEEDER-POST-UNLOAD-TIMEOUT", Feeder.Name, "WaferFeeder post unload position timeout. target=" + Options.PostUnloadMove);

            CurrentStep = InputFeederUnloadToCassetteStep.MoveCassetteToSlotPosition;
            return 0;
        }

        private async Task<int> MoveCassetteToSlotPositionAsync(CancellationToken ct)
        {
            if (!Options.ReturnCassetteToUnloadSlotAfterUnload)
            {
                CurrentStep = InputFeederUnloadToCassetteStep.VerifyTransferData;
                await Task.CompletedTask.ConfigureAwait(false);
                return 0;
            }

            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            double target = cassette.CalculateWaferCassetteSlotTargetPosition(ResolveUnloadSlotIndex());
            int result = await MoveCassetteZAndVerifyAsync(cassette, target, "cassette final slot", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederUnloadToCassetteStep.VerifyTransferData;
            return 0;
        }

        private int VerifyTransferData()
        {
            if (Feeder.CurrentWaferMaterial != null || MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder) != null)
                return Fail("IN-FEEDER-DATA-CLEAR", "Material", "InputFeeder wafer data remained after cassette unload.");

            int unloadSlot = ResolveUnloadSlotIndex();
            WaferMaterial cassetteWafer = MaterialStateService.GetWaferInCassette(Options.CassetteRole, unloadSlot);
            if (cassetteWafer == null)
                return Fail("IN-FEEDER-CST-DATA", "Material", "Cassette wafer data was not found after feeder unload. slot=" + unloadSlot);

            Context.Bus.Set("InputCassetteSlotUpdated");
            CurrentStep = InputFeederUnloadToCassetteStep.Complete;
            return 0;
        }

        private async Task<int> MoveCassetteZAndVerifyAsync(InputCassetteUnit cassette, double target, string description, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(cassette.MoveWaferLifterZ(target, Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-CST-Z-MOVE", cassette.Name, description + " move failed. target=" + target + ", result=" + result);

            result = await AwaitStepWithCancellationAsync(cassette.WaitWaferLifterZMoveDone(ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-CST-Z-TIMEOUT", cassette.Name, description + " move done timeout. target=" + target);

            if (!cassette.IsWaferLifterZInPosition(target, cassette.ResolveWaferLifterZInPositionTolerance()))
                return Fail("IN-FEEDER-CST-Z-POSITION", cassette.Name, description + " final position check failed. target=" + target);

            return 0;
        }

        private bool IsUnloadSlotEmpty(InputCassetteUnit cassette, int slotIndex)
        {
            if (cassette == null || slotIndex < 0)
                return false;

            WaferMaterial feederWafer = ResolveFeederWafer();
            WaferMaterial cassetteWafer = MaterialStateService.GetWaferInCassette(Options.CassetteRole, slotIndex);
            if (cassetteWafer != null && feederWafer != null && string.Equals(cassetteWafer.WaferId, feederWafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return true;

            WaferCassetteMaterial material = cassette.GetWaferMaterialCassette();
            if (material == null || material.Slots == null || slotIndex >= material.Slots.Count)
                return false;

            WaferSlotState state = material.Slots[slotIndex];
            if (state == null)
                return false;

            return state.Presence == SlotPresence.Empty ||
                   (state.Presence == SlotPresence.Exist && state.Process == ProcessState.Processing && feederWafer != null) ||
                   cassetteWafer == null;
        }

        private double ResolveCassetteUnloadOffset(InputCassetteUnit cassette)
        {
            return cassette != null && cassette.Config != null ? cassette.Config.UnloadingPositionOffset : 0.0;
        }

        private InputCassetteUnit ResolveCassette()
        {
            return Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
        }

        private WaferMaterial ResolveFeederWafer()
        {
            return Feeder.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
        }

        private int ResolveUnloadSlotIndex()
        {
            return ResolveUnloadSlotIndex(ResolveFeederWafer());
        }

        private int ResolveUnloadSlotIndex(WaferMaterial wafer)
        {
            if (wafer != null &&
                wafer.SourceCassetteRole == Options.CassetteRole &&
                wafer.SourceSlotNumber >= 0)
            {
                return wafer.SourceSlotNumber;
            }

            return Options.SlotIndex;
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
