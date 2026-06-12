using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederRecoverStep
    {
        Idle,
        CheckUnit,
        PrepareFeederUnclamp,
        PrepareFeederLiftDown,
        MoveFeederAvoidPosition,
        Complete,
        Error
    }

    internal sealed class InputFeederRecoverSequence : InputFeederSequenceBase<InputFeederRecoverStep>
    {
        public InputFeederRecoverSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.Recover, "InputFeederRecoverSequence")
        {
        }

        protected override InputFeederRecoverStep IdleStep { get { return InputFeederRecoverStep.Idle; } }
        protected override InputFeederRecoverStep InitialStep { get { return InputFeederRecoverStep.CheckUnit; } }
        protected override InputFeederRecoverStep CompleteStep { get { return InputFeederRecoverStep.Complete; } }
        protected override InputFeederRecoverStep ErrorStep { get { return InputFeederRecoverStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case InputFeederRecoverStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederRecoverStep.PrepareFeederUnclamp));
                    case InputFeederRecoverStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    case InputFeederRecoverStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);
                    case InputFeederRecoverStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-FEEDER-RECOVER-STEP-EX", "InputFeederRecoverSequence", "Recover step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederUnclamp())
                return Fail("IN-FEEDER-RECOVER-UNCLAMP", Feeder.Name,
                    "WaferFeeder recover unclamp failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederRecoverStep.PrepareFeederLiftDown;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederUpDownAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-RECOVER-DOWN", Feeder.Name,
                    "WaferFeeder recover lift down failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederRecoverStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederAvoidPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-RECOVER-AVOID", Feeder.Name,
                    "WaferFeeder recover avoid move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            bool done = await AwaitStepWithCancellationAsync(Feeder.WaitWaferFeederYMoveDone(ResolveTimeout()), ct).ConfigureAwait(false);
            if (!done || !Feeder.IsWaferFeederInAvoidPosition())
                return Fail("IN-FEEDER-RECOVER-AVOID-TIMEOUT", Feeder.Name,
                    "WaferFeeder recover avoid position timeout. done=" + done + ". " + Feeder.GetWaferFeederTransferState());

            Context.Bus.Set("InputFeederRecovered");
            CurrentStep = InputFeederRecoverStep.Complete;
            return 0;
        }
    }
}
