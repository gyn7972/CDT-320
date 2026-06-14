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
        CheckCassetteSize,
        CheckCassetteMaterial,
        CheckMappingStartCondition,
        CheckFeederPosition,
        MoveMappingStartPosition,
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
                    // LOT 확인
                    case OutputCassetteMappingStep.CheckLot:
                        return Task.FromResult(CheckLot(OutputCassetteMappingStep.CheckCassetteDetected));
                    // 카세트 감지 확인
                    case OutputCassetteMappingStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(OutputCassetteMappingStep.CheckCassetteSize));
                    // 카세트 사이즈 확인
                    case OutputCassetteMappingStep.CheckCassetteSize:
                        return Task.FromResult(CheckCassetteSize(OutputCassetteMappingStep.CheckCassetteMaterial, false));
                    // 카세트 자재 확인
                    case OutputCassetteMappingStep.CheckCassetteMaterial:
                        return Task.FromResult(CheckCassetteMaterial(OutputCassetteMappingStep.CheckMappingStartCondition));
                    // 맵핑 시작 조건 확인
                    case OutputCassetteMappingStep.CheckMappingStartCondition:
                        return Task.FromResult(CheckMappingStartCondition(OutputCassetteMappingStep.CheckFeederPosition));
                    // 피더 위치 확인
                    case OutputCassetteMappingStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(OutputCassetteMappingStep.MoveMappingStartPosition));
                    // 맵핑 시작 위치 이동
                    case OutputCassetteMappingStep.MoveMappingStartPosition:
                        return MoveMappingStartPositionAsync(OutputCassetteMappingStep.ScanSlots, ct);
                    // 슬롯 스캔
                    case OutputCassetteMappingStep.ScanSlots:
                        return ScanSlotsAsync(OutputCassetteMappingStep.BuildBinInfo, ct);
                    // BIN 정보 생성
                    case OutputCassetteMappingStep.BuildBinInfo:
                        return Task.FromResult(BuildBinInfo(OutputCassetteMappingStep.MoveFirstEmptySlot));
                    // 첫번째 비어있음 슬롯 이동
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

