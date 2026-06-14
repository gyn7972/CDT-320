using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum InputStageMoveAvoidStep
    {
        Idle,
        CheckUnit,
        MoveAvoidPosition,
        Complete,
        Error
    }

    internal sealed class InputStageMoveAvoidSequence : InputStageSequenceBase<InputStageMoveAvoidStep>
    {
        public InputStageMoveAvoidSequence(MachineSequenceContext context)
            : base(context, InputStageSequenceKind.MoveAvoid, "InputStageMoveAvoidSequence")
        {
        }

        protected override InputStageMoveAvoidStep IdleStep { get { return InputStageMoveAvoidStep.Idle; } }
        protected override InputStageMoveAvoidStep InitialStep { get { return InputStageMoveAvoidStep.CheckUnit; } }
        protected override InputStageMoveAvoidStep CompleteStep { get { return InputStageMoveAvoidStep.Complete; } }
        protected override InputStageMoveAvoidStep ErrorStep { get { return InputStageMoveAvoidStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputStageMoveAvoidStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputStageMoveAvoidStep.MoveAvoidPosition));
                    // 어보이드 위치 이동
                    case InputStageMoveAvoidStep.MoveAvoidPosition:
                        return MoveAvoidPositionAsync();
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-STAGE-AVOID-STEP-EX", "InputStageMoveAvoidSequence", "Move avoid step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}

