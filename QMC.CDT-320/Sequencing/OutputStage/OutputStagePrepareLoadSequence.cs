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
        EnsureGoodGuideDownBeforeNgYMove,
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
                    // 유닛 확인
                    case OutputStagePrepareLoadStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStagePrepareLoadStep.CheckTargetSide));

                    // 대상 사이드 확인
                    case OutputStagePrepareLoadStep.CheckTargetSide:
                        return CheckTargetSideAsync(ct);

                    // 반대쪽 스테이지 Z로 어보이드 이동
                    case OutputStagePrepareLoadStep.MoveOppositeStageZToAvoid:
                        return MoveOppositeStageZToAvoidAsync(ct);

                    // 반대쪽 스테이지 Z 어보이드 확인
                    case OutputStagePrepareLoadStep.CheckOppositeStageZAvoid:
                        return Task.FromResult(CheckOppositeStageZAvoid());

                    // NG Y 이동 전 Good Guide Down 확보
                    case OutputStagePrepareLoadStep.EnsureGoodGuideDownBeforeNgYMove:
                        return EnsureGoodGuideDownBeforeNgYMoveAsync(ct);

                    // 대상 스테이지 Y로 로드 이동
                    case OutputStagePrepareLoadStep.MoveTargetStageYToLoad:
                        return MoveTargetStageYToLoadAsync(ct);

                    // 대상 스테이지 Y 로드 확인
                    case OutputStagePrepareLoadStep.CheckTargetStageYLoad:
                        return Task.FromResult(CheckTargetStageYLoad());

                    // 대상 스테이지 Z로 로드 이동
                    case OutputStagePrepareLoadStep.MoveTargetStageZToLoad:
                        return MoveTargetStageZToLoadAsync(ct);

                    // 대상 스테이지 Z 로드 확인
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

        private async Task<int> CheckTargetSideAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options.Side != BinSide.Good && Options.Side != BinSide.Ng)
                    return Fail("OUT-STAGE-SIDE", Name, "Invalid output stage side: " + Options.Side);

                int pickerReady = await WaitPickersClearForOutputTransportAsync("OutputStage Load 준비", ct).ConfigureAwait(false);
                if (pickerReady != 0)
                    return pickerReady;

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
                    CurrentStep = OutputStagePrepareLoadStep.EnsureGoodGuideDownBeforeNgYMove;
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
                    CurrentStep = OutputStagePrepareLoadStep.EnsureGoodGuideDownBeforeNgYMove;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(opposite);
                double target = ResolveSideZTarget(opposite, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-OPP-Z-CHECK", Stage.Name,
                        opposite + " Z avoid final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareLoadStep.EnsureGoodGuideDownBeforeNgYMove;
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

        private async Task<int> EnsureGoodGuideDownBeforeNgYMoveAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options.Side != BinSide.Ng)
                {
                    CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageYToLoad;
                    return 0;
                }

                int result = await Stage.EnsureBinGuideDownAsync(BinSide.Good, ResolveTimeout(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-GOOD-GUIDE-DOWN", Stage.Name,
                        "NG Y 이동 전 Good Bin Guide Down 명령 실패. result=" + result + ", " +
                        Stage.DescribeOutputStageInterlockState(Options.Side));

                if (!Stage.IsBinGuideDown(BinSide.Good))
                    return Fail("OUT-STAGE-GOOD-GUIDE-DOWN", Stage.Name,
                        "NG Y 이동 전 Good Bin Guide Down 확인 실패. " +
                        Stage.DescribeOutputStageInterlockState(Options.Side));

                CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageYToLoad;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-GOOD-GUIDE-DOWN-EX", Name,
                    "NG Y 이동 전 Good Bin Guide Down 확보 중 예외가 발생했습니다: " + ex.Message);
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
                    return Fail("OUT-STAGE-Y-LOAD-CHECK", Stage.Name,
                        Options.Side + " Y load final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-LOAD-EX", Name, "OutputStage 대상 Z축 Load 위치 이동 중 예외가 발생했습니다. side=" + Options.Side + ", error=" + ex.Message);
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
                    return Fail("OUT-STAGE-Z-LOAD-CHECK", Stage.Name,
                        Options.Side + " Z load final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

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

