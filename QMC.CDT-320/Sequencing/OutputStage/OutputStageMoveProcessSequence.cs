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
        MoveNgStageYToAvoid,
        CheckNgStageYAvoid,
        EnsureNgClampLiftUp,
        MoveTargetStageZToAvoidBeforeY,
        CheckTargetStageZAvoidBeforeY,
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
                    // 유닛 확인
                    case OutputStageMoveProcessStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStageMoveProcessStep.CheckTargetSide));

                    // 대상 사이드 확인
                    case OutputStageMoveProcessStep.CheckTargetSide:
                        return Task.FromResult(CheckTargetSide());

                    // 반대쪽 스테이지 Z로 어보이드 이동
                    case OutputStageMoveProcessStep.MoveOppositeStageZToAvoid:
                        return MoveOppositeStageZToAvoidAsync(ct);

                    // 반대쪽 스테이지 Z 어보이드 확인
                    case OutputStageMoveProcessStep.CheckOppositeStageZAvoid:
                        return Task.FromResult(CheckOppositeStageZAvoid());

                    // NG 스테이지 Y로 어보이드 이동
                    case OutputStageMoveProcessStep.MoveNgStageYToAvoid:
                        return MoveNgStageYToAvoidAsync(ct);

                    // NG 스테이지 Y 어보이드 확인
                    case OutputStageMoveProcessStep.CheckNgStageYAvoid:
                        return Task.FromResult(CheckNgStageYAvoid());

                    // NG 클램프 리프트 업 확보
                    case OutputStageMoveProcessStep.EnsureNgClampLiftUp:
                        return EnsureNgClampLiftUpAsync(ct);

                    // 대상 스테이지 Y 이동 전 대상 Z 어보이드 확보
                    case OutputStageMoveProcessStep.MoveTargetStageZToAvoidBeforeY:
                        return MoveTargetStageZToAvoidBeforeYAsync(ct);

                    // 대상 스테이지 Y 이동 전 대상 Z 어보이드 확인
                    case OutputStageMoveProcessStep.CheckTargetStageZAvoidBeforeY:
                        return Task.FromResult(CheckTargetStageZAvoidBeforeY());

                    // 대상 스테이지 Y로 프로세스 이동
                    case OutputStageMoveProcessStep.MoveTargetStageYToProcess:
                        return MoveTargetAxisAsync(ResolveYAxis(Options.Side), "Process", Options.Side + " Y process", OutputStageMoveProcessStep.CheckTargetStageYProcess, ct);

                    // 대상 스테이지 Y 프로세스 확인
                    case OutputStageMoveProcessStep.CheckTargetStageYProcess:
                        return Task.FromResult(CheckTargetAxis(ResolveYAxis(Options.Side), "Process", Options.Side + " Y process", OutputStageMoveProcessStep.MoveTargetStageZToProcess));

                    // 대상 스테이지 Z로 프로세스 이동
                    case OutputStageMoveProcessStep.MoveTargetStageZToProcess:
                        return MoveTargetStageZToProcessAsync(ct);

                    // 대상 스테이지 Z 프로세스 확인
                    case OutputStageMoveProcessStep.CheckTargetStageZProcess:
                        return Task.FromResult(CheckTargetStageZProcess());

                    // 비전 X로 프로세스 이동
                    case OutputStageMoveProcessStep.MoveVisionXToProcess:
                        return MoveTargetAxisAsync(BinStageAxis.VisionX, "Process", "VisionX process", OutputStageMoveProcessStep.CheckVisionXProcess, ct);

                    // 비전 X 프로세스 확인
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
                if (SkipMissingSideZAxis(opposite, opposite + " Z avoid before process"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveNgStageYToAvoid;
                    return 0;
                }

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
                if (SkipMissingSideZAxis(opposite, opposite + " Z avoid final check before process"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveNgStageYToAvoid;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(opposite);
                double target = ResolveSideZTarget(opposite, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-OPP-Z-CHECK", Stage.Name,
                        opposite + " Z avoid final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStageMoveProcessStep.MoveNgStageYToAvoid;
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

        private async Task<int> MoveNgStageYToAvoidAsync(CancellationToken ct)
        {
            try
            {
                if (Options.Side != BinSide.Good || Stage.IsNgStageInAvoidPosition())
                {
                    CurrentStep = OutputStageMoveProcessStep.EnsureNgClampLiftUp;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    BinStageAxis.NgBinY,
                    ResolveTarget(BinStageAxis.NgBinY, "Avoid"),
                    "NG Y avoid before Good process",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStageMoveProcessStep.CheckNgStageYAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-NG-Y-AVOID-EX", Name, "NG stage Y avoid before Good process failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckNgStageYAvoid()
        {
            try
            {
                if (Options.Side != BinSide.Good)
                {
                    CurrentStep = OutputStageMoveProcessStep.EnsureNgClampLiftUp;
                    return 0;
                }

                if (!Stage.IsNgStageInAvoidPosition())
                    return Fail("OUT-STAGE-NG-Y-AVOID-CHECK", Stage.Name,
                        "NG stage must be in Avoid position before Good process. " +
                        BuildAxisState(BinStageAxis.NgBinY, ResolveTarget(BinStageAxis.NgBinY, "Avoid")));

                CurrentStep = OutputStageMoveProcessStep.EnsureNgClampLiftUp;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-NG-Y-AVOID-CHECK-EX", Name, "NG stage Y avoid check before Good process failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureNgClampLiftUpAsync(CancellationToken ct)
        {
            try
            {
                if (Options.Side != BinSide.Good || Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveTargetStageZToAvoidBeforeY;
                    return 0;
                }

                int result = await Stage.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, ResolveTimeout(), ct).ConfigureAwait(false);

                if (result != 0)
                    return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name,
                        "NG Bin Clamp Lift Up failed before Good process. result=" + result + ". " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Good));

                if (!Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                    return Fail("OUT-STAGE-NG-CLAMP-UP-CHECK", Stage.Name,
                        "NG Bin Clamp Lift Up final check failed before Good process. " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Good));

                CurrentStep = OutputStageMoveProcessStep.MoveTargetStageZToAvoidBeforeY;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-NG-CLAMP-UP-EX", Name,
                    "NG Bin Clamp Lift Up before Good process exception: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToAvoidBeforeYAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid before Y process"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveTargetStageYToProcess;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(Options.Side),
                    ResolveSideZTarget(Options.Side, "Avoid"),
                    Options.Side + " Z avoid before Y process",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStageMoveProcessStep.CheckTargetStageZAvoidBeforeY;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-TARGET-Z-AVOID-EX", Name, "Target stage Z avoid before Y process failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZAvoidBeforeY()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid final check before Y process"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveTargetStageYToProcess;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-TARGET-Z-AVOID-CHECK", Stage.Name,
                        Options.Side + " Z avoid final check failed before Y process. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStageMoveProcessStep.MoveTargetStageYToProcess;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-TARGET-Z-AVOID-CHECK-EX", Name, "Target stage Z avoid check before Y process failed: " + ex.Message);
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

        private async Task<int> MoveTargetStageZToProcessAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z process"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveVisionXToProcess;
                    return 0;
                }

                return await MoveTargetAxisAsync(
                    ResolveZAxis(Options.Side),
                    "Process",
                    Options.Side + " Z process",
                    OutputStageMoveProcessStep.CheckTargetStageZProcess,
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-PROCESS-Z-EX", Name, "Target stage Z process move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZProcess()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z process final check"))
                {
                    CurrentStep = OutputStageMoveProcessStep.MoveVisionXToProcess;
                    return 0;
                }

                return CheckTargetAxis(
                    ResolveZAxis(Options.Side),
                    "Process",
                    Options.Side + " Z process",
                    OutputStageMoveProcessStep.MoveVisionXToProcess);
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-PROCESS-Z-CHECK-EX", Name, "Target stage Z process check failed: " + ex.Message);
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
                    return Fail("OUT-STAGE-PROCESS-CHECK", Stage.Name,
                        description + " final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

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

