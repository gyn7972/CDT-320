using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStagePrepareUnloadStep { Idle, CheckUnit, MoveSideUnload, Complete, Error }

    internal sealed class OutputStagePrepareUnloadSequence : OutputStageSequenceBase<OutputStagePrepareUnloadStep>
    {
        public OutputStagePrepareUnloadSequence(MachineSequenceContext context) : base(context, OutputStageSequenceKind.PrepareUnload, "OutputStagePrepareUnloadSequence") { }
        protected override OutputStagePrepareUnloadStep IdleStep { get { return OutputStagePrepareUnloadStep.Idle; } }
        protected override OutputStagePrepareUnloadStep InitialStep { get { return OutputStagePrepareUnloadStep.CheckUnit; } }
        protected override OutputStagePrepareUnloadStep CompleteStep { get { return OutputStagePrepareUnloadStep.Complete; } }
        protected override OutputStagePrepareUnloadStep ErrorStep { get { return OutputStagePrepareUnloadStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputStagePrepareUnloadStep.CheckUnit: return Task.FromResult(CheckUnit(OutputStagePrepareUnloadStep.MoveSideUnload));
                    case OutputStagePrepareUnloadStep.MoveSideUnload: return MoveSideAxesAsync("Unload", OutputStagePrepareUnloadStep.Complete, ct);
                    default: return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex) { return Task.FromResult(Fail("OUT-STAGE-PREP-UNLOAD-EX", Name, "Prepare unload step failed: " + ex.Message)); }
        }
    }
}
