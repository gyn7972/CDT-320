using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederExchangeStep
    {
        Idle,
        CheckUnit,
        CheckExchangePlan,
        UnloadCurrentToCassette,
        LoadNextFromCassette,
        MoveExchangePosition,
        Complete,
        Error
    }

    internal sealed class OutputFeederExchangeSequence : OutputFeederSequenceBase<OutputFeederExchangeStep>
    {
        public OutputFeederExchangeSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.Exchange, "OutputFeederExchangeSequence")
        {
        }

        protected override OutputFeederExchangeStep IdleStep { get { return OutputFeederExchangeStep.Idle; } }
        protected override OutputFeederExchangeStep InitialStep { get { return OutputFeederExchangeStep.CheckUnit; } }
        protected override OutputFeederExchangeStep CompleteStep { get { return OutputFeederExchangeStep.Complete; } }
        protected override OutputFeederExchangeStep ErrorStep { get { return OutputFeederExchangeStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputFeederExchangeStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederExchangeStep.CheckExchangePlan));

                    case OutputFeederExchangeStep.CheckExchangePlan:
                        return Task.FromResult(CheckExchangePlan());

                    case OutputFeederExchangeStep.UnloadCurrentToCassette:
                        return UnloadCurrentToCassetteAsync(ct);

                    case OutputFeederExchangeStep.LoadNextFromCassette:
                        return LoadNextFromCassetteAsync(ct);

                    case OutputFeederExchangeStep.MoveExchangePosition:
                        return MoveExchangePositionAsync(ct);

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-EXCHANGE-EX", Name, "Exchange step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckExchangePlan()
        {
            if (Options.SlotIndex < 0)
                return Fail("OUT-FEEDER-EXCHANGE-SLOT", Feeder.Name, "Output feeder exchange current slot is invalid. slot=" + Options.SlotIndex);

            if (Options.NextSlotIndex < 0)
                return Fail("OUT-FEEDER-EXCHANGE-NEXT", Feeder.Name, "Output feeder exchange next slot is invalid. nextSlot=" + Options.NextSlotIndex);

            if (Options.SlotIndex == Options.NextSlotIndex)
                return Fail("OUT-FEEDER-EXCHANGE-SAME", Feeder.Name, "Output feeder exchange current/next slot must be different. slot=" + Options.SlotIndex);

            CurrentStep = OutputFeederExchangeStep.UnloadCurrentToCassette;
            return 0;
        }

        private async Task<int> UnloadCurrentToCassetteAsync(CancellationToken ct)
        {
            OutputFeederSequenceOptions unloadOptions = CloneOptions();
            unloadOptions.SlotIndex = Options.SlotIndex;
            unloadOptions.StartMode = Options.StartMode;
            int result = await new OutputFeederUnloadToCassetteSequence(Context).RunAsync(ct, unloadOptions).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-EXCHANGE-UNLOAD", Feeder.Name,
                    "Output feeder exchange unload failed. result=" + result + ", side=" + Options.Side + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederExchangeStep.LoadNextFromCassette;
            return 0;
        }

        private async Task<int> LoadNextFromCassetteAsync(CancellationToken ct)
        {
            OutputFeederSequenceOptions loadOptions = CloneOptions();
            loadOptions.SlotIndex = Options.NextSlotIndex;
            loadOptions.StartMode = Options.StartMode;
            int result = await new OutputFeederLoadFromCassetteSequence(Context).RunAsync(ct, loadOptions).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-EXCHANGE-LOAD", Feeder.Name,
                    "Output feeder exchange load failed. result=" + result + ", side=" + Options.Side + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederExchangeStep.MoveExchangePosition;
            return 0;
        }

        private async Task<int> MoveExchangePositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederExchangePosition(Options.Side, Options.FineMove), "exchange", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInExchangePosition(Options.Side), "exchange", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederExchangeStep.Complete;
            return 0;
        }

        private OutputFeederSequenceOptions CloneOptions()
        {
            return new OutputFeederSequenceOptions
            {
                SlotIndex = Options.SlotIndex,
                NextSlotIndex = Options.NextSlotIndex,
                Side = Options.Side,
                CassetteRole = Options.CassetteRole,
                MoveTimeoutMs = Options.MoveTimeoutMs,
                FineMove = Options.FineMove,
                UseBarcode = Options.UseBarcode,
                UseVacuum = Options.UseVacuum,
                RunMode = Options.RunMode,
                StartMode = Options.StartMode
            };
        }
    }
}
