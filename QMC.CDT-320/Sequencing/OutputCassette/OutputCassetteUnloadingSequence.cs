using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputCassetteUnloadingStep
    {
        Idle,
        CheckCassetteDetected,
        CheckCassetteMaterial,
        CheckFeederPosition,
        MoveUnloadingPosition,
        Complete,
        Error
    }

    internal sealed class OutputCassetteUnloadingSequence : OutputCassetteSequenceBase<OutputCassetteUnloadingStep>
    {
        public OutputCassetteUnloadingSequence(MachineSequenceContext context)
            : base(context, OutputCassetteSequenceKind.Unloading, "OutputCassetteUnloadingSequence")
        {
        }

        protected override OutputCassetteUnloadingStep IdleStep { get { return OutputCassetteUnloadingStep.Idle; } }
        protected override OutputCassetteUnloadingStep InitialStep { get { return OutputCassetteUnloadingStep.CheckCassetteDetected; } }
        protected override OutputCassetteUnloadingStep CompleteStep { get { return OutputCassetteUnloadingStep.Complete; } }
        protected override OutputCassetteUnloadingStep ErrorStep { get { return OutputCassetteUnloadingStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputCassetteUnloadingStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(OutputCassetteUnloadingStep.CheckCassetteMaterial));
                    case OutputCassetteUnloadingStep.CheckCassetteMaterial:
                        return Task.FromResult(CheckCassetteMaterial(OutputCassetteUnloadingStep.CheckFeederPosition));
                    case OutputCassetteUnloadingStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(OutputCassetteUnloadingStep.MoveUnloadingPosition));
                    case OutputCassetteUnloadingStep.MoveUnloadingPosition:
                        return MoveUnloadingPositionAsync(ct);
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-CST-UNLOAD-STEP-EX", "OutputCassetteUnloadingSequence", "Unloading step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}
