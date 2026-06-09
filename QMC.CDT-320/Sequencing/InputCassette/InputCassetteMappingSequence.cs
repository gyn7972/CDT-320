using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum InputCassetteMappingStep
    {
        Idle,
        CheckLot,
        CheckCassetteDetected,
        CheckCassetteSize,
        CheckCassetteMaterial,
        CheckMappingStartCondition,
        CheckFeederPosition,
        MoveMappingStartPosition,
        MoveMappingEndPosition,
        ScanSlots,
        BuildWaferInfo,
        MoveFirstWaferSlot,
        Complete,
        Error
    }

    internal sealed class InputCassetteMappingSequence : InputCassetteSequenceBase<InputCassetteMappingStep>
    {
        public InputCassetteMappingSequence(MachineSequenceContext context)
            : base(context, InputCassetteSequenceKind.Mapping, "InputCassetteMappingSequence")
        {
        }

        protected override InputCassetteMappingStep IdleStep { get { return InputCassetteMappingStep.Idle; } }
        protected override InputCassetteMappingStep InitialStep { get { return InputCassetteMappingStep.CheckLot; } }
        protected override InputCassetteMappingStep CompleteStep { get { return InputCassetteMappingStep.Complete; } }
        protected override InputCassetteMappingStep ErrorStep { get { return InputCassetteMappingStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case InputCassetteMappingStep.CheckLot:
                        return Task.FromResult(CheckLot(InputCassetteMappingStep.CheckCassetteDetected));
                    case InputCassetteMappingStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(InputCassetteMappingStep.CheckCassetteSize));
                    case InputCassetteMappingStep.CheckCassetteSize:
                        return Task.FromResult(CheckCassetteSize(InputCassetteMappingStep.CheckCassetteMaterial, false));
                    case InputCassetteMappingStep.CheckCassetteMaterial:
                        return Task.FromResult(CheckCassetteMaterial(InputCassetteMappingStep.CheckMappingStartCondition));
                    case InputCassetteMappingStep.CheckMappingStartCondition:
                        return Task.FromResult(CheckMappingStartCondition(InputCassetteMappingStep.CheckFeederPosition));
                    case InputCassetteMappingStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(InputCassetteMappingStep.MoveMappingStartPosition));
                    case InputCassetteMappingStep.MoveMappingStartPosition:
                        return MoveMappingStartPositionAsync(InputCassetteMappingStep.MoveMappingEndPosition);
                    case InputCassetteMappingStep.MoveMappingEndPosition:
                        return MoveMappingEndPositionAsync(InputCassetteMappingStep.ScanSlots);
                    case InputCassetteMappingStep.ScanSlots:
                        return ScanSlotsAsync(InputCassetteMappingStep.BuildWaferInfo);
                    case InputCassetteMappingStep.BuildWaferInfo:
                        return Task.FromResult(BuildWaferInfo(InputCassetteMappingStep.MoveFirstWaferSlot));
                    case InputCassetteMappingStep.MoveFirstWaferSlot:
                        return MoveFirstWaferSlotAsync();
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("IN-CST-MAP-STEP-EX", "InputCassetteMappingSequence", "Mapping step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}
