using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStageMoveProcessStep { Idle, CheckUnit, MoveSideProcess, MoveVisionProcess, Complete, Error }

    internal sealed class OutputStageMoveProcessSequence : OutputStageSequenceBase<OutputStageMoveProcessStep>
    {
        public OutputStageMoveProcessSequence(MachineSequenceContext context) : base(context, OutputStageSequenceKind.MoveProcess, "OutputStageMoveProcessSequence") { }
        protected override OutputStageMoveProcessStep IdleStep { get { return OutputStageMoveProcessStep.Idle; } }
        protected override OutputStageMoveProcessStep InitialStep { get { return OutputStageMoveProcessStep.CheckUnit; } }
        protected override OutputStageMoveProcessStep CompleteStep { get { return OutputStageMoveProcessStep.Complete; } }
        protected override OutputStageMoveProcessStep ErrorStep { get { return OutputStageMoveProcessStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputStageMoveProcessStep.CheckUnit: return Task.FromResult(CheckUnit(OutputStageMoveProcessStep.MoveSideProcess));
                    case OutputStageMoveProcessStep.MoveSideProcess: return MoveSideAxesAsync("Process", OutputStageMoveProcessStep.MoveVisionProcess, ct);
                    case OutputStageMoveProcessStep.MoveVisionProcess: return MoveVisionProcessAsync(OutputStageMoveProcessStep.Complete, ct);
                    default: return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex) { return Task.FromResult(Fail("OUT-STAGE-PROCESS-EX", Name, "Move process step failed: " + ex.Message)); }
        }
    }
}
