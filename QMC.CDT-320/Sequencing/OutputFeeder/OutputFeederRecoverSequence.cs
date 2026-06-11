using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederRecoverStep
    {
        Idle,
        CheckUnit,
        CheckFeederEmpty,
        PrepareFeederUnclamp,
        PrepareFeederLiftDown,
        MoveFeederAvoidPosition,
        Complete,
        Error
    }

    internal sealed class OutputFeederRecoverSequence : OutputFeederSequenceBase<OutputFeederRecoverStep>
    {
        public OutputFeederRecoverSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.Recover, "OutputFeederRecoverSequence")
        {
        }

        protected override OutputFeederRecoverStep IdleStep { get { return OutputFeederRecoverStep.Idle; } }
        protected override OutputFeederRecoverStep InitialStep { get { return OutputFeederRecoverStep.CheckUnit; } }
        protected override OutputFeederRecoverStep CompleteStep { get { return OutputFeederRecoverStep.Complete; } }
        protected override OutputFeederRecoverStep ErrorStep { get { return OutputFeederRecoverStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputFeederRecoverStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederRecoverStep.CheckFeederEmpty));

                    case OutputFeederRecoverStep.CheckFeederEmpty:
                        return Task.FromResult(CheckFeederEmpty());

                    case OutputFeederRecoverStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);

                    case OutputFeederRecoverStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);

                    case OutputFeederRecoverStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-RECOVER-EX", Name, "Recover step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckFeederEmpty()
        {
            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-RECOVER-OCCUPIED", "Material", "Output feeder recover is not allowed while bin data exists. Use load-to-stage or unload-to-cassette sequence.");

            if (!IsHardwareBypass() && !Feeder.IsFeederEmpty())
                return Fail("OUT-FEEDER-RECOVER-SENSOR", Feeder.Name, "Output feeder recover is not allowed while feeder sensor is occupied.");

            CurrentStep = OutputFeederRecoverStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-RECOVER-UNCLAMP", Feeder.Name, "Output feeder recover unclamp command failed. result=" + result);

            if (!Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-RECOVER-UNCLAMP", Feeder.Name, "Output feeder recover unclamp failed. result=" + result);

            CurrentStep = OutputFeederRecoverStep.PrepareFeederLiftDown;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-RECOVER-DOWN", Feeder.Name, "Output feeder recover lift down command failed. result=" + result);

            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-RECOVER-DOWN", Feeder.Name, "Output feeder recover lift down failed. result=" + result);

            CurrentStep = OutputFeederRecoverStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederAvoidPosition(Options.FineMove), "avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederInAvoidPosition(), "avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederRecoverStep.Complete;
            return 0;
        }
    }
}
