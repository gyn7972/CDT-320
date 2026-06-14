using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum InputStagePrepareLoadStep
    {
        Idle,
        CheckUnit,
        MoveLoadPosition,
        Complete,
        Error
    }

    internal sealed class InputStagePrepareLoadSequence : InputStageSequenceBase<InputStagePrepareLoadStep>
    {
        public InputStagePrepareLoadSequence(MachineSequenceContext context)
            : base(context, InputStageSequenceKind.PrepareLoad, "InputStagePrepareLoadSequence")
        {
        }

        protected override InputStagePrepareLoadStep IdleStep { get { return InputStagePrepareLoadStep.Idle; } }
        protected override InputStagePrepareLoadStep InitialStep { get { return InputStagePrepareLoadStep.CheckUnit; } }
        protected override InputStagePrepareLoadStep CompleteStep { get { return InputStagePrepareLoadStep.Complete; } }
        protected override InputStagePrepareLoadStep ErrorStep { get { return InputStagePrepareLoadStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputStagePrepareLoadStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputStagePrepareLoadStep.MoveLoadPosition));
                    // 로드 위치 이동
                    case InputStagePrepareLoadStep.MoveLoadPosition:
                        return MoveLoadPositionAsync();
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-STAGE-LOAD-PREP-STEP-EX", "InputStagePrepareLoadSequence", "Prepare load step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}

