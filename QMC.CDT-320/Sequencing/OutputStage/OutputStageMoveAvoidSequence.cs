using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStageMoveAvoidStep
    {
        Idle,
        CheckUnit,
        MoveGoodStageZToAvoid,
        CheckGoodStageZAvoid,
        MoveGoodStageYToAvoid,
        CheckGoodStageYAvoid,
        MoveNgStageYToAvoid,
        CheckNgStageYAvoid,
        MoveVisionXToAvoid,
        CheckVisionXAvoid,
        Complete,
        Error
    }

    internal sealed class OutputStageMoveAvoidSequence : OutputStageSequenceBase<OutputStageMoveAvoidStep>
    {
        public OutputStageMoveAvoidSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.MoveAvoid, "OutputStageMoveAvoidSequence")
        {
        }

        protected override OutputStageMoveAvoidStep IdleStep { get { return OutputStageMoveAvoidStep.Idle; } }
        protected override OutputStageMoveAvoidStep InitialStep { get { return OutputStageMoveAvoidStep.CheckUnit; } }
        protected override OutputStageMoveAvoidStep CompleteStep { get { return OutputStageMoveAvoidStep.Complete; } }
        protected override OutputStageMoveAvoidStep ErrorStep { get { return OutputStageMoveAvoidStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    case OutputStageMoveAvoidStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStageMoveAvoidStep.MoveGoodStageZToAvoid));

                    case OutputStageMoveAvoidStep.MoveGoodStageZToAvoid:
                        return MoveAxisAsync(BinStageAxis.GoodBinZ, "Avoid", "Good Z avoid", OutputStageMoveAvoidStep.CheckGoodStageZAvoid, ct);

                    case OutputStageMoveAvoidStep.CheckGoodStageZAvoid:
                        return Task.FromResult(CheckAxis(BinStageAxis.GoodBinZ, "Avoid", "Good Z avoid", OutputStageMoveAvoidStep.MoveGoodStageYToAvoid));

                    case OutputStageMoveAvoidStep.MoveGoodStageYToAvoid:
                        return MoveAxisAsync(BinStageAxis.GoodBinY, "Avoid", "Good Y avoid", OutputStageMoveAvoidStep.CheckGoodStageYAvoid, ct);

                    case OutputStageMoveAvoidStep.CheckGoodStageYAvoid:
                        return Task.FromResult(CheckAxis(BinStageAxis.GoodBinY, "Avoid", "Good Y avoid", OutputStageMoveAvoidStep.MoveNgStageYToAvoid));

                    case OutputStageMoveAvoidStep.MoveNgStageYToAvoid:
                        return MoveAxisAsync(BinStageAxis.NgBinY, "Avoid", "NG Y avoid", OutputStageMoveAvoidStep.CheckNgStageYAvoid, ct);

                    case OutputStageMoveAvoidStep.CheckNgStageYAvoid:
                        return Task.FromResult(CheckAxis(BinStageAxis.NgBinY, "Avoid", "NG Y avoid", OutputStageMoveAvoidStep.MoveVisionXToAvoid));

                    case OutputStageMoveAvoidStep.MoveVisionXToAvoid:
                        return MoveAxisAsync(BinStageAxis.VisionX, "Avoid", "VisionX avoid", OutputStageMoveAvoidStep.CheckVisionXAvoid, ct);

                    case OutputStageMoveAvoidStep.CheckVisionXAvoid:
                        return Task.FromResult(CheckAxis(BinStageAxis.VisionX, "Avoid", "VisionX avoid", OutputStageMoveAvoidStep.Complete));

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-STAGE-AVOID-EX", Name, "Move avoid step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> MoveAxisAsync(
            BinStageAxis axis,
            string positionName,
            string description,
            OutputStageMoveAvoidStep nextStep,
            CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(
                    axis,
                    ResolveTarget(axis, positionName),
                    description,
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-AVOID-MOVE-EX", Name, description + " move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckAxis(
            BinStageAxis axis,
            string positionName,
            string description,
            OutputStageMoveAvoidStep nextStep)
        {
            try
            {
                double target = ResolveTarget(axis, positionName);
                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-AVOID-CHECK", Stage.Name, description + " final check failed. target=" + target);

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-AVOID-CHECK-EX", Name, description + " check failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}
