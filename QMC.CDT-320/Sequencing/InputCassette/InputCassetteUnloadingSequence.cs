using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum InputCassetteUnloadingStep
    {
        Idle,
        CheckCassetteDetected,
        CheckCassetteSize,
        CheckFeederPosition,
        MoveUnloadingPosition,
        Complete,
        Error
    }

    internal sealed class InputCassetteUnloadingSequence : InputCassetteSequenceBase<InputCassetteUnloadingStep>
    {
        public InputCassetteUnloadingSequence(MachineSequenceContext context)
            : base(context, InputCassetteSequenceKind.Unloading, "InputCassetteUnloadingSequence")
        {
        }

        protected override InputCassetteUnloadingStep IdleStep { get { return InputCassetteUnloadingStep.Idle; } }
        protected override InputCassetteUnloadingStep InitialStep { get { return InputCassetteUnloadingStep.CheckCassetteDetected; } }
        protected override InputCassetteUnloadingStep CompleteStep { get { return InputCassetteUnloadingStep.Complete; } }
        protected override InputCassetteUnloadingStep ErrorStep { get { return InputCassetteUnloadingStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case InputCassetteUnloadingStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(InputCassetteUnloadingStep.CheckCassetteSize));
                    case InputCassetteUnloadingStep.CheckCassetteSize:
                        return Task.FromResult(CheckCassetteSize(InputCassetteUnloadingStep.CheckFeederPosition, true));
                    case InputCassetteUnloadingStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(InputCassetteUnloadingStep.MoveUnloadingPosition));
                    case InputCassetteUnloadingStep.MoveUnloadingPosition:
                        return MoveUnloadingPositionAsync();
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-CST-UNLOAD-STEP-EX", "InputCassetteUnloadingSequence", "Unloading step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}
