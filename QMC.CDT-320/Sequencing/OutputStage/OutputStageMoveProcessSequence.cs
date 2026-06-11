using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStageMoveProcessStep
    {
        Idle,
        CheckUnit,
        CheckTargetSide,
        MoveOppositeStageZToAvoid,
        CheckOppositeStageZAvoid,
        MoveTargetStageYToProcess,
        CheckTargetStageYProcess,
        MoveTargetStageZToProcess,
        CheckTargetStageZProcess,
        MoveVisionXToProcess,
        CheckVisionXProcess,
        Complete,
        Error
    }

    internal sealed class OutputStageMoveProcessSequence : OutputStageSequenceBase<OutputStageMoveProcessStep>
    {
        public OutputStageMoveProcessSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.MoveProcess, "OutputStageMoveProcessSequence")
        {
        }

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
                    case OutputStageMoveProcessStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStageMoveProcessStep.CheckTargetSide));

                    case OutputStageMoveProcessStep.CheckTargetSide:
                        return Task.FromResult(CheckTargetSide());

                    case OutputStageMoveProcessStep.MoveOppositeStageZToAvoid:
                        return MoveOppositeStageZToAvoidAsync(ct);

                    case OutputStageMoveProcessStep.CheckOppositeStageZAvoid:
                        return Task.FromResult(CheckOppositeStageZAvoid());

                    case OutputStageMoveProcessStep.MoveTargetStageYToProcess:
                        return MoveTargetAxisAsync(ResolveYAxis(Options.Side), "Process", Options.Side + " Y process", OutputStageMoveProcessStep.CheckTargetStageYProcess, ct);

                    case OutputStageMoveProcessStep.CheckTargetStageYProcess:
                        return Task.FromResult(CheckTargetAxis(ResolveYAxis(Options.Side), "Process", Options.Side + " Y process", OutputStageMoveProcessStep.MoveTargetStageZToProcess));

                    case OutputStageMoveProcessStep.MoveTargetStageZToProcess:
                        return MoveTargetAxisAsync(ResolveZAxis(Options.Side), "Process", Options.Side + " Z process", OutputStageMoveProcessStep.CheckTargetStageZProcess, ct);

                    case OutputStageMoveProcessStep.CheckTargetStageZProcess:
                        return Task.FromResult(CheckTargetAxis(ResolveZAxis(Options.Side), "Process", Options.Side + " Z process", OutputStageMoveProcessStep.MoveVisionXToProcess));

                    case OutputStageMoveProcessStep.MoveVisionXToProcess:
                        return MoveTargetAxisAsync(BinStageAxis.VisionX, "Process", "VisionX process", OutputStageMoveProcessStep.CheckVisionXProcess, ct);

                    case OutputStageMoveProcessStep.CheckVisionXProcess:
                        return Task.FromResult(CheckTargetAxis(BinStageAxis.VisionX, "Process", "VisionX process", OutputStageMoveProcessStep.Complete));

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-STAGE-PROCESS-EX", Name, "Move process step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTargetSide()
        {
            try
            {
                if (Options.Side != BinSide.Good && Options.Side != BinSide.Ng)
                    return Fail("OUT-STAGE-SIDE", Name, "Invalid output stage side: " + Options.Side);

                CurrentStep = OutputStageMoveProcessStep.MoveOppositeStageZToAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-SIDE-EX", Name, "Target side check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOppositeStageZToAvoidAsync(CancellationToken ct)
        {
            try
            {
                BinSide opposite = Options.Side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(opposite),
                    ResolveSideZTarget(opposite, "Avoid"),
                    opposite + " Z avoid before process",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStageMoveProcessStep.CheckOppositeStageZAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-OPP-Z-AVOID-EX", Name, "Opposite stage Z avoid failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckOppositeStageZAvoid()
        {
            try
            {
                BinSide opposite = Options.Side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                BinStageAxis axis = ResolveZAxis(opposite);
                double target = ResolveSideZTarget(opposite, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-OPP-Z-CHECK", Stage.Name, opposite + " Z avoid final check failed. target=" + target);

                CurrentStep = OutputStageMoveProcessStep.MoveTargetStageYToProcess;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-OPP-Z-CHECK-EX", Name, "Opposite stage Z avoid check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetAxisAsync(
            BinStageAxis axis,
            string positionName,
            string description,
            OutputStageMoveProcessStep nextStep,
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
                return Fail("OUT-STAGE-PROCESS-MOVE-EX", Name, description + " move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetAxis(
            BinStageAxis axis,
            string positionName,
            string description,
            OutputStageMoveProcessStep nextStep)
        {
            try
            {
                double target = ResolveTarget(axis, positionName);
                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-PROCESS-CHECK", Stage.Name, description + " final check failed. target=" + target);

                CurrentStep = nextStep;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-PROCESS-CHECK-EX", Name, description + " check failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}
