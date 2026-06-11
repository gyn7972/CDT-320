using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum InputCassetteLoadingStep
    {
        Idle,
        CheckFeederPosition,
        MoveLoadingPosition,
        Complete,
        Error
    }

    internal sealed class InputCassetteLoadingSequence : InputCassetteSequenceBase<InputCassetteLoadingStep>
    {
        public InputCassetteLoadingSequence(MachineSequenceContext context)
            : base(context, InputCassetteSequenceKind.Loading, "InputCassetteLoadingSequence")
        {
        }

        protected override InputCassetteLoadingStep IdleStep { get { return InputCassetteLoadingStep.Idle; } }
        protected override InputCassetteLoadingStep InitialStep { get { return InputCassetteLoadingStep.CheckFeederPosition; } }
        protected override InputCassetteLoadingStep CompleteStep { get { return InputCassetteLoadingStep.Complete; } }
        protected override InputCassetteLoadingStep ErrorStep { get { return InputCassetteLoadingStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case InputCassetteLoadingStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(InputCassetteLoadingStep.MoveLoadingPosition));
                    case InputCassetteLoadingStep.MoveLoadingPosition:
                        return MoveLoadingPositionAsync();
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-CST-LOAD-STEP-EX", "InputCassetteLoadingSequence", "Loading step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}
