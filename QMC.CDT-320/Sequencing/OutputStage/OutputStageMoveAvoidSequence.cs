using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStageMoveAvoidStep { Idle, CheckUnit, MoveAvoid, Complete, Error }

    internal sealed class OutputStageMoveAvoidSequence : OutputStageSequenceBase<OutputStageMoveAvoidStep>
    {
        public OutputStageMoveAvoidSequence(MachineSequenceContext context) : base(context, OutputStageSequenceKind.MoveAvoid, "OutputStageMoveAvoidSequence") { }
        protected override OutputStageMoveAvoidStep IdleStep { get { return OutputStageMoveAvoidStep.Idle; } }
        protected override OutputStageMoveAvoidStep InitialStep { get { return OutputStageMoveAvoidStep.CheckUnit; } }
        protected override OutputStageMoveAvoidStep CompleteStep { get { return OutputStageMoveAvoidStep.Complete; } }
        protected override OutputStageMoveAvoidStep ErrorStep { get { return OutputStageMoveAvoidStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputStageMoveAvoidStep.CheckUnit: return Task.FromResult(CheckUnit(OutputStageMoveAvoidStep.MoveAvoid));
                    case OutputStageMoveAvoidStep.MoveAvoid: return MoveAllAvoidAsync(OutputStageMoveAvoidStep.Complete, ct);
                    default: return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex) { return Task.FromResult(Fail("OUT-STAGE-AVOID-EX", Name, "Move avoid step failed: " + ex.Message)); }
        }
    }
}
