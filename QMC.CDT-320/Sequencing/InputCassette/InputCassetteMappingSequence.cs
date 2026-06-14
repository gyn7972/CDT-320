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
                    // LOT 확인
                    case InputCassetteMappingStep.CheckLot:
                        return Task.FromResult(CheckLot(InputCassetteMappingStep.CheckCassetteDetected));
                    // 카세트 감지 확인
                    case InputCassetteMappingStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(InputCassetteMappingStep.CheckCassetteSize));
                    // 카세트 사이즈 확인
                    case InputCassetteMappingStep.CheckCassetteSize:
                        return Task.FromResult(CheckCassetteSize(InputCassetteMappingStep.CheckCassetteMaterial, false));
                    // 카세트 자재 확인
                    case InputCassetteMappingStep.CheckCassetteMaterial:
                        return Task.FromResult(CheckCassetteMaterial(InputCassetteMappingStep.CheckMappingStartCondition));
                    // 맵핑 시작 조건 확인
                    case InputCassetteMappingStep.CheckMappingStartCondition:
                        return Task.FromResult(CheckMappingStartCondition(InputCassetteMappingStep.CheckFeederPosition));
                    // 피더 위치 확인
                    case InputCassetteMappingStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(InputCassetteMappingStep.MoveMappingStartPosition));
                    // 맵핑 시작 위치 이동
                    case InputCassetteMappingStep.MoveMappingStartPosition:
                        return MoveMappingStartPositionAsync(InputCassetteMappingStep.ScanSlots);
                    // 슬롯 스캔
                    case InputCassetteMappingStep.ScanSlots:
                        return ScanSlotsAsync(InputCassetteMappingStep.BuildWaferInfo);
                    // 웨이퍼 정보 생성
                    case InputCassetteMappingStep.BuildWaferInfo:
                        return Task.FromResult(BuildWaferInfo(InputCassetteMappingStep.MoveFirstWaferSlot));
                    // 첫번째 웨이퍼 슬롯 이동
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

