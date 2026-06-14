using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputCassetteLoadingStep
    {
        Idle,
        CheckCassetteDetected,
        CheckCassetteMaterial,
        CheckFeederPosition,
        MoveLoadingPosition,
        Complete,
        Error
    }

    internal sealed class OutputCassetteLoadingSequence : OutputCassetteSequenceBase<OutputCassetteLoadingStep>
    {
        public OutputCassetteLoadingSequence(MachineSequenceContext context)
            : base(context, OutputCassetteSequenceKind.Loading, "OutputCassetteLoadingSequence")
        {
        }

        protected override OutputCassetteLoadingStep IdleStep { get { return OutputCassetteLoadingStep.Idle; } }
        protected override OutputCassetteLoadingStep InitialStep { get { return OutputCassetteLoadingStep.CheckCassetteDetected; } }
        protected override OutputCassetteLoadingStep CompleteStep { get { return OutputCassetteLoadingStep.Complete; } }
        protected override OutputCassetteLoadingStep ErrorStep { get { return OutputCassetteLoadingStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 카세트 감지 확인
                    case OutputCassetteLoadingStep.CheckCassetteDetected:
                        return Task.FromResult(CheckCassetteDetected(OutputCassetteLoadingStep.CheckCassetteMaterial));
                    // 카세트 자재 확인
                    case OutputCassetteLoadingStep.CheckCassetteMaterial:
                        return Task.FromResult(CheckCassetteMaterial(OutputCassetteLoadingStep.CheckFeederPosition));
                    // 피더 위치 확인
                    case OutputCassetteLoadingStep.CheckFeederPosition:
                        return Task.FromResult(CheckFeederPosition(OutputCassetteLoadingStep.MoveLoadingPosition));
                    // 로딩 위치 이동
                    case OutputCassetteLoadingStep.MoveLoadingPosition:
                        return MoveLoadingPositionAsync(ct);
                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-CST-LOAD-STEP-EX", "OutputCassetteLoadingSequence", "Loading step failed: " + ex.Message));
            }
            finally
            {
            }
        }
    }
}

