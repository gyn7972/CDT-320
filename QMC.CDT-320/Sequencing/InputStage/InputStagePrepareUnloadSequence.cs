using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum InputStagePrepareUnloadStep
    {
        Idle,
        CheckUnit,
        MoveUnloadPosition,
        Complete,
        Error
    }

    internal sealed class InputStagePrepareUnloadSequence : InputStageSequenceBase<InputStagePrepareUnloadStep>
    {
        public InputStagePrepareUnloadSequence(MachineSequenceContext context)
            : base(context, InputStageSequenceKind.PrepareUnload, "InputStagePrepareUnloadSequence")
        {
        }

        protected override InputStagePrepareUnloadStep IdleStep { get { return InputStagePrepareUnloadStep.Idle; } }
        protected override InputStagePrepareUnloadStep InitialStep { get { return InputStagePrepareUnloadStep.CheckUnit; } }
        protected override InputStagePrepareUnloadStep CompleteStep { get { return InputStagePrepareUnloadStep.Complete; } }
        protected override InputStagePrepareUnloadStep ErrorStep { get { return InputStagePrepareUnloadStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case InputStagePrepareUnloadStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputStagePrepareUnloadStep.MoveUnloadPosition));
                    case InputStagePrepareUnloadStep.MoveUnloadPosition:
                        return MoveUnloadPositionAsync();
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-STAGE-UNLOAD-PREP-STEP-EX", "InputStagePrepareUnloadSequence", "Prepare unload step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}
