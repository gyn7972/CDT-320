using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputCassetteMappingStep
    {
        Idle,
        CheckLot,
        CheckCassetteDetected,
        CheckCassetteMaterial,
        CheckMappingStartCondition,
        CheckFeederPosition,
        MoveMappingStartPosition,
        MoveMappingEndPosition,
        ScanSlots,
        BuildBinInfo,
        MoveFirstEmptySlot,
        Complete,
        Error
    }

    internal sealed class OutputCassetteMappingSequence : OutputCassetteSequenceBase<OutputCassetteMappingStep>
    {
        public OutputCassetteMappingSequence(MachineSequenceContext context)
            : base(context, OutputCassetteSequenceKind.Mapping, "OutputCassetteMappingSequence")
        {
        }

        protected override OutputCassetteMappingStep IdleStep { get { return OutputCassetteMappingStep.Idle; } }
        protected override OutputCassetteMappingStep InitialStep { get { return OutputCassetteMappingStep.CheckLot; } }
        protected override OutputCassetteMappingStep CompleteStep { get { return OutputCassetteMappingStep.Complete; } }
        protected override OutputCassetteMappingStep ErrorStep { get { return OutputCassetteMappingStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case OutputCassetteMappingStep.CheckLot:
                        return Task.FromResult(CheckLot(OutputCassetteMappingStep.CheckCassetteDetected));
                    case OutputCassetteMappingStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(OutputCassetteMappingStep.CheckCassetteMaterial));
                    case OutputCassetteMappingStep.CheckCassetteMaterial:
                        return Task.FromResult(CheckCassetteMaterial(OutputCassetteMappingStep.CheckMappingStartCondition));
                    case OutputCassetteMappingStep.CheckMappingStartCondition:
                        return Task.FromResult(CheckMappingStartCondition(OutputCassetteMappingStep.CheckFeederPosition));
                    case OutputCassetteMappingStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(OutputCassetteMappingStep.MoveMappingStartPosition));
                    case OutputCassetteMappingStep.MoveMappingStartPosition:
                        return MoveMappingStartPositionAsync(OutputCassetteMappingStep.MoveMappingEndPosition, ct);
                    case OutputCassetteMappingStep.MoveMappingEndPosition:
                        return MoveMappingEndPositionAsync(OutputCassetteMappingStep.ScanSlots, ct);
                    case OutputCassetteMappingStep.ScanSlots:
                        return ScanSlotsAsync(OutputCassetteMappingStep.BuildBinInfo, ct);
                    case OutputCassetteMappingStep.BuildBinInfo:
                        return Task.FromResult(BuildBinInfo(OutputCassetteMappingStep.MoveFirstEmptySlot));
                    case OutputCassetteMappingStep.MoveFirstEmptySlot:
                        return MoveFirstEmptySlotAsync(ct);
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-CST-MAP-STEP-EX", "OutputCassetteMappingSequence", "Mapping step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}
