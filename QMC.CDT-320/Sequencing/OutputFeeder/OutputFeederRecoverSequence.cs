using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederRecoverStep { Idle, CheckUnit, Unclamp, LiftDown, MoveAvoidPosition, Complete, Error }

    internal sealed class OutputFeederRecoverSequence : OutputFeederSequenceBase<OutputFeederRecoverStep>
    {
        public OutputFeederRecoverSequence(MachineSequenceContext context) : base(context, OutputFeederSequenceKind.Recover, "OutputFeederRecoverSequence") { }
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
                    case OutputFeederRecoverStep.CheckUnit: return Task.FromResult(CheckUnit(OutputFeederRecoverStep.Unclamp));
                    case OutputFeederRecoverStep.Unclamp: return UnclampAsync(ct);
                    case OutputFeederRecoverStep.LiftDown: return LiftDownAsync(ct);
                    case OutputFeederRecoverStep.MoveAvoidPosition: return MoveAvoidPositionAsync(ct);
                    default: return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex) { return Task.FromResult(Fail("OUT-FEEDER-RECOVER-EX", Name, "Recover step failed: " + ex.Message)); }
        }

        private async Task<int> UnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederClampAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsFeederUnclamped()) return Fail("OUT-FEEDER-RECOVER-UNCLAMP", Feeder.Name, "Output feeder recover unclamp failed. result=" + result);
            CurrentStep = OutputFeederRecoverStep.LiftDown;
            return 0;
        }

        private async Task<int> LiftDownAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(Feeder.SetFeederUpDownAsync(false, ResolveTimeout()), ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsFeederDown()) return Fail("OUT-FEEDER-RECOVER-DOWN", Feeder.Name, "Output feeder recover lift down failed. result=" + result);
            CurrentStep = OutputFeederRecoverStep.MoveAvoidPosition;
            return 0;
        }

        private async Task<int> MoveAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederAvoidPosition(Options.FineMove), "avoid", ct).ConfigureAwait(false);
            if (result != 0) return result;
            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederInAvoidPosition(), "avoid", ct).ConfigureAwait(false);
            if (result != 0) return result;
            CurrentStep = OutputFeederRecoverStep.Complete;
            return 0;
        }
    }
}
