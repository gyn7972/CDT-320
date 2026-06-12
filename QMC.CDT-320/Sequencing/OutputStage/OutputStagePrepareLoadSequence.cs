using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStagePrepareLoadStep
    {
        Idle,
        CheckUnit,
        CheckTargetSide,
        MoveOppositeStageZToAvoid,
        CheckOppositeStageZAvoid,
        MoveTargetStageYToLoad,
        CheckTargetStageYLoad,
        MoveTargetStageZToLoad,
        CheckTargetStageZLoad,
        Complete,
        Error
    }

    internal sealed class OutputStagePrepareLoadSequence : OutputStageSequenceBase<OutputStagePrepareLoadStep>
    {
        public OutputStagePrepareLoadSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.PrepareLoad, "OutputStagePrepareLoadSequence")
        {
        }

        protected override OutputStagePrepareLoadStep IdleStep { get { return OutputStagePrepareLoadStep.Idle; } }
        protected override OutputStagePrepareLoadStep InitialStep { get { return OutputStagePrepareLoadStep.CheckUnit; } }
        protected override OutputStagePrepareLoadStep CompleteStep { get { return OutputStagePrepareLoadStep.Complete; } }
        protected override OutputStagePrepareLoadStep ErrorStep { get { return OutputStagePrepareLoadStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    case OutputStagePrepareLoadStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStagePrepareLoadStep.CheckTargetSide));

                    case OutputStagePrepareLoadStep.CheckTargetSide:
                        return Task.FromResult(CheckTargetSide());

                    case OutputStagePrepareLoadStep.MoveOppositeStageZToAvoid:
                        return MoveOppositeStageZToAvoidAsync(ct);

                    case OutputStagePrepareLoadStep.CheckOppositeStageZAvoid:
                        return Task.FromResult(CheckOppositeStageZAvoid());

                    case OutputStagePrepareLoadStep.MoveTargetStageYToLoad:
                        return MoveTargetStageYToLoadAsync(ct);

                    case OutputStagePrepareLoadStep.CheckTargetStageYLoad:
                        return Task.FromResult(CheckTargetStageYLoad());

                    case OutputStagePrepareLoadStep.MoveTargetStageZToLoad:
                        return MoveTargetStageZToLoadAsync(ct);

                    case OutputStagePrepareLoadStep.CheckTargetStageZLoad:
                        return Task.FromResult(CheckTargetStageZLoad());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-STAGE-PREP-LOAD-EX", Name, "Prepare load step failed: " + ex.Message));
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

                CurrentStep = OutputStagePrepareLoadStep.MoveOppositeStageZToAvoid;
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
                if (SkipMissingSideZAxis(opposite, opposite + " Z avoid before load"))
                {
                    CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageYToLoad;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(opposite),
                    ResolveSideZTarget(opposite, "Avoid"),
                    opposite + " Z avoid before load",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareLoadStep.CheckOppositeStageZAvoid;
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
                if (SkipMissingSideZAxis(opposite, opposite + " Z avoid final check before load"))
                {
                    CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageYToLoad;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(opposite);
                double target = ResolveSideZTarget(opposite, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-OPP-Z-CHECK", Stage.Name, opposite + " Z avoid final check failed. target=" + target);

                CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageYToLoad;
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

        private async Task<int> MoveTargetStageYToLoadAsync(CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(
                    ResolveYAxis(Options.Side),
                    ResolveSideTarget(Options.Side, "Load"),
                    Options.Side + " Y load",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareLoadStep.CheckTargetStageYLoad;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Y-LOAD-EX", Name, "Target stage Y load move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageYLoad()
        {
            try
            {
                BinStageAxis axis = ResolveYAxis(Options.Side);
                double target = ResolveSideTarget(Options.Side, "Load");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Y-LOAD-CHECK", Stage.Name, Options.Side + " Y load final check failed. target=" + target);

                CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageZToLoad;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Y-LOAD-CHECK-EX", Name, "Target stage Y load check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToLoadAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z load"))
                {
                    CurrentStep = OutputStagePrepareLoadStep.Complete;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(Options.Side),
                    ResolveSideZTarget(Options.Side, "Load"),
                    Options.Side + " Z load",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareLoadStep.CheckTargetStageZLoad;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-LOAD-EX", Name, "Target stage Z load move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZLoad()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z load final check"))
                {
                    CurrentStep = OutputStagePrepareLoadStep.Complete;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Load");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Z-LOAD-CHECK", Stage.Name, Options.Side + " Z load final check failed. target=" + target);

                CurrentStep = OutputStagePrepareLoadStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-LOAD-CHECK-EX", Name, "Target stage Z load check failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}
