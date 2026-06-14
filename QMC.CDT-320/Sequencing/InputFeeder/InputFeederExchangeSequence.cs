using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederExchangeStep
    {
        Idle,
        CheckUnit,
        CheckExchangeReady,
        CheckNextWaferData,
        UnloadCurrentWaferToCassette,
        MoveCassetteToNextWaferSlot,
        LoadNextWaferFromCassette,
        Complete,
        Error
    }

    internal sealed class InputFeederExchangeSequence : InputFeederSequenceBase<InputFeederExchangeStep>
    {
        public InputFeederExchangeSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.Exchange, "InputFeederExchangeSequence")
        {
        }

        protected override InputFeederExchangeStep IdleStep { get { return InputFeederExchangeStep.Idle; } }
        protected override InputFeederExchangeStep InitialStep { get { return InputFeederExchangeStep.CheckUnit; } }
        protected override InputFeederExchangeStep CompleteStep { get { return InputFeederExchangeStep.Complete; } }
        protected override InputFeederExchangeStep ErrorStep { get { return InputFeederExchangeStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case InputFeederExchangeStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederExchangeStep.CheckExchangeReady));
                    case InputFeederExchangeStep.CheckExchangeReady:
                        return Task.FromResult(CheckExchangeReady());
                    case InputFeederExchangeStep.CheckNextWaferData:
                        return Task.FromResult(CheckNextWaferData());
                    case InputFeederExchangeStep.UnloadCurrentWaferToCassette:
                        return UnloadCurrentWaferToCassetteAsync(ct);
                    case InputFeederExchangeStep.MoveCassetteToNextWaferSlot:
                        return MoveCassetteToNextWaferSlotAsync(ct);
                    case InputFeederExchangeStep.LoadNextWaferFromCassette:
                        return LoadNextWaferFromCassetteAsync(ct);
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-FEEDER-EXCHANGE-STEP-EX", "InputFeederExchangeSequence", "Exchange step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckExchangeReady()
        {
            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-EXCHANGE-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            string cassetteReason;
            if (!IsHardwareBypass() && !cassette.CheckWaferCassetteTransferReady(TransferMode.Load, out cassetteReason))
                return Fail("IN-FEEDER-EXCHANGE-CST-SENSOR", cassette.Name, "Input cassette is not detected or not ready for exchange. " + cassetteReason);

            if (!cassette.CheckWaferCassetteMoveReady(out cassetteReason))
                return Fail("IN-FEEDER-EXCHANGE-CST-MOVE", cassette.Name, "Input cassette lifter is not move ready. " + cassetteReason);

            if (ResolveFeederWafer() == null)
                return Fail("IN-FEEDER-EXCHANGE-FEEDER-DATA", "Material", "InputFeeder wafer data was not found before exchange unload.");

            if (!Feeder.HasWaferOnFeeder())
                return Fail("IN-FEEDER-EXCHANGE-FEEDER-WAFER", Feeder.Name, "InputFeeder must have wafer before exchange.");

            CurrentStep = InputFeederExchangeStep.CheckNextWaferData;
            return 0;
        }

        private int CheckNextWaferData()
        {
            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-EXCHANGE-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            if (cassette.IsInputCassetteProcessComplete())
            {
                cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);
                return Fail("IN-FEEDER-EXCHANGE-CST-COMPLETE", cassette.Name,
                    "Input cassette processing is complete. Replace input cassette.");
            }

            int nextSlot = Options.NextSlotIndex;
            if (Options.RunMode == SequenceRunMode.Auto)
            {
                int currentNextSlot = cassette.FindNextProcessWaferSlot();
                if (currentNextSlot < 0)
                {
                    if (cassette.IsInputCassetteProcessComplete())
                        cassette.RaiseInputCassetteCompleteAlarm(cassette.Name);

                    return Fail("IN-FEEDER-EXCHANGE-NEXT-SLOT", cassette.Name, "Next cassette wafer slot was not found.");
                }

                if (nextSlot != currentNextSlot)
                    return Fail("IN-FEEDER-EXCHANGE-NEXT-SLOT-MISMATCH", cassette.Name,
                        "Selected next slot is not current process slot. selected=" + nextSlot + ", current=" + currentNextSlot);
            }

            if (!IsSelectedSlotProcessReady(cassette, nextSlot))
                return Fail("IN-FEEDER-EXCHANGE-NEXT-SLOT-NOT-READY", cassette.Name, "Next cassette slot is not ready. slot=" + nextSlot);

            WaferMaterial nextWafer = MaterialStateService.GetWaferInCassette(Options.CassetteRole, nextSlot);
            if (nextWafer == null)
                return Fail("IN-FEEDER-EXCHANGE-NEXT-WAFER-DATA", "Material", "Next cassette wafer data was not found. role=" + Options.CassetteRole + ", slot=" + nextSlot);

            CurrentStep = InputFeederExchangeStep.UnloadCurrentWaferToCassette;
            return 0;
        }

        private async Task<int> UnloadCurrentWaferToCassetteAsync(CancellationToken ct)
        {
            InputFeederSequenceOptions unloadOptions = CloneOptions();
            unloadOptions.SlotIndex = Options.SlotIndex;
            unloadOptions.StartMode = Options.StartMode;
            unloadOptions.PostUnloadMove = InputFeederPostUnloadMove.Exchange;
            unloadOptions.ReturnCassetteToUnloadSlotAfterUnload = false;

            int result = await new InputFeederUnloadToCassetteSequence(Context).RunAsync(ct, unloadOptions).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-EXCHANGE-UNLOAD", Feeder.Name,
                    "Exchange unload current wafer failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            if (!Feeder.IsWaferFeederInExchangePosition())
                return Fail("IN-FEEDER-EXCHANGE-POS", Feeder.Name,
                    "WaferFeeder is not in exchange position after unload. " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederExchangeStep.MoveCassetteToNextWaferSlot;
            return 0;
        }

        private async Task<int> MoveCassetteToNextWaferSlotAsync(CancellationToken ct)
        {
            InputCassetteUnit cassette = ResolveCassette();
            if (cassette == null)
                return Fail("IN-FEEDER-EXCHANGE-CST-MISSING", "InputCassette", "Input cassette unit is not available.");

            double target = cassette.CalculateWaferCassetteSlotTargetPosition(Options.NextSlotIndex);
            int result = await MoveCassetteZAndVerifyAsync(cassette, target, "next wafer slot", ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederExchangeStep.LoadNextWaferFromCassette;
            return 0;
        }

        private async Task<int> LoadNextWaferFromCassetteAsync(CancellationToken ct)
        {
            InputFeederSequenceOptions loadOptions = CloneOptions();
            loadOptions.SlotIndex = Options.NextSlotIndex;
            loadOptions.StartMode = Options.StartMode;
            loadOptions.PostUnloadMove = InputFeederPostUnloadMove.Avoid;
            loadOptions.ReturnCassetteToUnloadSlotAfterUnload = true;

            int result = await new InputFeederLoadFromCassetteSequence(Context).RunAsync(ct, loadOptions).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-EXCHANGE-LOAD", Feeder.Name,
                    "Exchange load next wafer failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            Context.Bus.Set("InputFeederExchanged");
            CurrentStep = InputFeederExchangeStep.Complete;
            return 0;
        }

        private async Task<int> MoveCassetteZAndVerifyAsync(InputCassetteUnit cassette, double target, string description, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(cassette.MoveWaferLifterZ(target, Options.FineMove), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-EXCHANGE-CST-Z-MOVE", cassette.Name,
                    description + " move failed. target=" + target + ", result=" + result + ". " + BuildCassetteZState(cassette, target));

            AxisMoveWaitResult waitResult = await AwaitStepWithCancellationAsync(
                cassette.WaitWaferLifterZMoveDoneInPosition(target, ResolveTimeout()),
                ct).ConfigureAwait(false);
            if (waitResult == null || !waitResult.Success)
                return Fail(ResolveAxisMoveWaitAlarmCode("IN-FEEDER-EXCHANGE-CST-Z", waitResult), cassette.Name,
                    description + " move/in-position wait failed. " +
                    FormatAxisMoveWaitResult(waitResult, BuildCassetteZState(cassette, target)));

            return 0;
        }

        private string BuildCassetteZState(InputCassetteUnit cassette, double target)
        {
            if (cassette == null || cassette.InputLifterZ == null)
                return "CassetteZ=null, target=" + target;

            double tolerance = cassette.ResolveWaferLifterZInPositionTolerance();
            return "CassetteZ name=" + cassette.InputLifterZ.Name +
                   ", servo=" + cassette.InputLifterZ.IsServoOn +
                   ", alarm=" + cassette.InputLifterZ.IsAlarm +
                   ", alarmCode=" + cassette.InputLifterZ.AlarmCode +
                   ", moving=" + cassette.InputLifterZ.IsMoving +
                   ", actual=" + cassette.InputLifterZ.ActualPosition +
                   ", command=" + cassette.InputLifterZ.CommandPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance +
                   ", inPosition=" + cassette.IsWaferLifterZInPosition(target, tolerance);
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

        private InputCassetteUnit ResolveCassette()
        {
            return Context.Machine != null ? Context.Machine.InputCassetteUnit : null;
        }

        private WaferMaterial ResolveFeederWafer()
        {
            return Feeder.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
        }

        private bool IsHardwareBypass()
        {
            AppSettings settings = AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (Context.Controller != null && Context.Controller.GlobalDryRun) ||
                   (Feeder.Setup != null && Feeder.Setup.IsSimulationMode) ||
                   (Feeder.Config != null && Feeder.Config.bDryRun);
        }

        private InputFeederSequenceOptions CloneOptions()
        {
            return new InputFeederSequenceOptions
            {
                SlotIndex = Options.SlotIndex,
                NextSlotIndex = Options.NextSlotIndex,
                CassetteRole = Options.CassetteRole,
                WaferSize = Options.WaferSize,
                MoveTimeoutMs = Options.MoveTimeoutMs,
                FineMove = Options.FineMove,
                UseBarcode = Options.UseBarcode,
                UseVacuum = Options.UseVacuum,
                StageLoadOffset = Options.StageLoadOffset,
                StageUnloadOffset = Options.StageUnloadOffset,
                PostUnloadMove = Options.PostUnloadMove,
                ReturnCassetteToUnloadSlotAfterUnload = Options.ReturnCassetteToUnloadSlotAfterUnload,
                RunMode = Options.RunMode,
                StartMode = Options.StartMode
            };
        }
    }
}
