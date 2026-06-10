using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputCassetteMoveSlotStep
    {
        Idle,
        CheckCassetteDetected,
        CheckCassetteMaterial,
        CheckFeederPosition,
        MoveSlotPosition,
        Complete,
        Error
    }

    internal sealed class OutputCassetteMoveSlotSequence : OutputCassetteSequenceBase<OutputCassetteMoveSlotStep>
    {
        public OutputCassetteMoveSlotSequence(MachineSequenceContext context)
            : base(context, OutputCassetteSequenceKind.MoveSlot, "OutputCassetteMoveSlotSequence")
        {
        }

        protected override OutputCassetteMoveSlotStep IdleStep { get { return OutputCassetteMoveSlotStep.Idle; } }
        protected override OutputCassetteMoveSlotStep InitialStep { get { return OutputCassetteMoveSlotStep.CheckCassetteDetected; } }
        protected override OutputCassetteMoveSlotStep CompleteStep { get { return OutputCassetteMoveSlotStep.Complete; } }
        protected override OutputCassetteMoveSlotStep ErrorStep { get { return OutputCassetteMoveSlotStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            switch (CurrentStep)
            {
                case OutputCassetteMoveSlotStep.CheckCassetteDetected:
                    return Task.FromResult(CheckCassetteDetected(OutputCassetteMoveSlotStep.CheckCassetteMaterial));
                case OutputCassetteMoveSlotStep.CheckCassetteMaterial:
                    return Task.FromResult(CheckCassetteMaterial(OutputCassetteMoveSlotStep.CheckFeederPosition));
                case OutputCassetteMoveSlotStep.CheckFeederPosition:
                    return Task.FromResult(CheckFeederPosition(OutputCassetteMoveSlotStep.MoveSlotPosition));
                case OutputCassetteMoveSlotStep.MoveSlotPosition:
                    return MoveConfiguredSlotAsync(ct);
                default:
                    return Task.FromResult(FailUnsupportedStep());
            }
        }
    }
}
