using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStagePrepareLoadStep { Idle, CheckUnit, MoveSideLoad, Complete, Error }

    internal sealed class OutputStagePrepareLoadSequence : OutputStageSequenceBase<OutputStagePrepareLoadStep>
    {
        public OutputStagePrepareLoadSequence(MachineSequenceContext context) : base(context, OutputStageSequenceKind.PrepareLoad, "OutputStagePrepareLoadSequence") { }
        protected override OutputStagePrepareLoadStep IdleStep { get { return OutputStagePrepareLoadStep.Idle; } }
        protected override OutputStagePrepareLoadStep InitialStep { get { return OutputStagePrepareLoadStep.CheckUnit; } }
        protected override OutputStagePrepareLoadStep CompleteStep { get { return OutputStagePrepareLoadStep.Complete; } }
        protected override OutputStagePrepareLoadStep ErrorStep { get { return OutputStagePrepareLoadStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputStagePrepareLoadStep.CheckUnit: return Task.FromResult(CheckUnit(OutputStagePrepareLoadStep.MoveSideLoad));
                    case OutputStagePrepareLoadStep.MoveSideLoad: return MoveSideAxesAsync("Load", OutputStagePrepareLoadStep.Complete, ct);
                    default: return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex) { return Task.FromResult(Fail("OUT-STAGE-PREP-LOAD-EX", Name, "Prepare load step failed: " + ex.Message)); }
        }
    }
}
