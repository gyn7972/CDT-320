using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStagePrepareUnloadStep
    {
        Idle,
        CheckUnit,
        CheckTargetSide,
        EnsureOutputFeederSafeBeforeStageMove,
        MoveOppositeStageToAvoid,
        CheckOppositeStageAvoid,
        MoveTargetStageZToAvoid,
        CheckTargetStageZAvoid,
        MoveTargetStageYToUnload,
        CheckTargetStageYUnload,
        MoveTargetStageZToUnload,
        CheckTargetStageZUnload,
        // To do: 언로드 위치 도착 후 배출 최종 준비 상태(UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP)까지 만든다.
        EnsureTargetStageUnloadReadyState,
        Complete,
        Error
    }

    internal sealed class OutputStagePrepareUnloadSequence : OutputStageSequenceBase<OutputStagePrepareUnloadStep>
    {
        public OutputStagePrepareUnloadSequence(MachineSequenceContext context)
            : base(context, OutputStageSequenceKind.PrepareUnload, "OutputStagePrepareUnloadSequence")
        {
        }

        protected override OutputStagePrepareUnloadStep IdleStep { get { return OutputStagePrepareUnloadStep.Idle; } }
        protected override OutputStagePrepareUnloadStep InitialStep { get { return OutputStagePrepareUnloadStep.CheckUnit; } }
        protected override OutputStagePrepareUnloadStep CompleteStep { get { return OutputStagePrepareUnloadStep.Complete; } }
        protected override OutputStagePrepareUnloadStep ErrorStep { get { return OutputStagePrepareUnloadStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputStagePrepareUnloadStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputStagePrepareUnloadStep.CheckTargetSide));

                    // 대상 사이드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetSide:
                        return CheckTargetSideAsync(ct);

                    // 메뉴얼 동작에서는 피더 Y를 움직이지 않고 어보이드 위치만 재확인
                    case OutputStagePrepareUnloadStep.EnsureOutputFeederSafeBeforeStageMove:
                        return Task.FromResult(EnsureOutputFeederSafeBeforeStageMove());

                    // 대상 스테이지 이동 전 반대쪽 스테이지를 어보이드로 이동
                    case OutputStagePrepareUnloadStep.MoveOppositeStageToAvoid:
                        return MoveOppositeStageToAvoidAsync(ct);

                    // 반대쪽 스테이지 어보이드 확인
                    case OutputStagePrepareUnloadStep.CheckOppositeStageAvoid:
                        return Task.FromResult(CheckOppositeStageAvoid());

                    // 대상 스테이지 Z로 어보이드 이동
                    case OutputStagePrepareUnloadStep.MoveTargetStageZToAvoid:
                        return MoveTargetStageZToAvoidAsync(ct);

                    // 대상 스테이지 Z 어보이드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetStageZAvoid:
                        return Task.FromResult(CheckTargetStageZAvoid());

                    // 대상 스테이지 Y로 언로드 이동
                    case OutputStagePrepareUnloadStep.MoveTargetStageYToUnload:
                        return MoveTargetStageYToUnloadAsync(ct);

                    // 대상 스테이지 Y 언로드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetStageYUnload:
                        return Task.FromResult(CheckTargetStageYUnload());

                    // 대상 스테이지 Z로 언로드 이동
                    case OutputStagePrepareUnloadStep.MoveTargetStageZToUnload:
                        return MoveTargetStageZToUnloadAsync(ct);

                    // 대상 스테이지 Z 언로드 확인
                    case OutputStagePrepareUnloadStep.CheckTargetStageZUnload:
                        return Task.FromResult(CheckTargetStageZUnload());

                    // 배출 최종 준비 상태 확보 (UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP)
                    case OutputStagePrepareUnloadStep.EnsureTargetStageUnloadReadyState:
                        return EnsureTargetStageUnloadReadyStateAsync(ct);

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-STAGE-PREP-UNLOAD-EX", Name, "Prepare unload step failed: " + ex.Message));
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

                int pickerReady = await WaitPickersClearForOutputTransportAsync("OutputStage Unload 준비", ct).ConfigureAwait(false);
                if (pickerReady != 0)
                    return pickerReady;

                CurrentStep = OutputStagePrepareUnloadStep.EnsureOutputFeederSafeBeforeStageMove;
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

        private int EnsureOutputFeederSafeBeforeStageMove()
        {
            try
            {
                if (Options.AllowOutputFeederActuation)
                {
                    CurrentStep = OutputStagePrepareUnloadStep.MoveOppositeStageToAvoid;
                    return 0;
                }

                if (OutputFeeder == null)
                    return Fail("OUT-STAGE-FEEDER-NO-UNIT", "OutputFeederUnit",
                        "OutputStage Unload 준비 중 OutputFeederUnit을 찾을 수 없습니다. side=" + Options.Side);

                if (!OutputFeeder.IsBinFeederYInAvoidPosition())
                    return Fail("OUT-STAGE-FEEDER-Y-MANUAL-POS", OutputFeeder.Name,
                        "메뉴얼 OutputStage Unload는 OutputFeederY를 이동하지 않습니다. 시작 전 FeederY를 Avoid 위치로 이동하십시오. side=" +
                        Options.Side + ", " + OutputFeeder.DescribeBinFeederYMoveDoneState());

                CurrentStep = OutputStagePrepareUnloadStep.MoveOppositeStageToAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-FEEDER-SAFE-EX", "OutputFeederUnit",
                    "OutputStage Unload 전 OutputFeeder 안전 위치 확인 중 예외가 발생했습니다. side=" +
                    Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOppositeStageToAvoidAsync(CancellationToken ct)
        {
            try
            {
                BinSide opposite = Options.Side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                BinStageAxis axis = opposite == BinSide.Ng
                    ? ResolveYAxis(BinSide.Ng)
                    : ResolveZAxis(BinSide.Good);
                double target = opposite == BinSide.Ng
                    ? ResolveSideTarget(BinSide.Ng, "Avoid")
                    : ResolveSideZTarget(BinSide.Good, "Avoid");

                int result = await MoveAxisAndVerifyAsync(
                    axis,
                    target,
                    opposite + " stage avoid before " + Options.Side + " unload",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckOppositeStageAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-UNLOAD-OPP-AVOID-EX", Name,
                    "Opposite stage avoid before unload failed. side=" + Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckOppositeStageAvoid()
        {
            try
            {
                BinSide opposite = Options.Side == BinSide.Ng ? BinSide.Good : BinSide.Ng;
                BinStageAxis axis = opposite == BinSide.Ng
                    ? ResolveYAxis(BinSide.Ng)
                    : ResolveZAxis(BinSide.Good);
                double target = opposite == BinSide.Ng
                    ? ResolveSideTarget(BinSide.Ng, "Avoid")
                    : ResolveSideZTarget(BinSide.Good, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-UNLOAD-OPP-AVOID-CHECK", Stage.Name,
                        "Opposite stage avoid final check failed before unload. side=" + Options.Side + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageZToAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-UNLOAD-OPP-AVOID-CHECK-EX", Name,
                    "Opposite stage avoid check before unload failed. side=" + Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToAvoidAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid before unload"))
                {
                    CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageYToUnload;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(Options.Side),
                    ResolveSideZTarget(Options.Side, "Avoid"),
                    Options.Side + " Z avoid before unload",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckTargetStageZAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-AVOID-EX", Name, "Target stage Z avoid move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZAvoid()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid final check before unload"))
                {
                    CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageYToUnload;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Z-AVOID-CHECK", Stage.Name,
                        Options.Side + " Z avoid final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageYToUnload;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-AVOID-CHECK-EX", Name, "Target stage Z avoid check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageYToUnloadAsync(CancellationToken ct)
        {
            try
            {
                int result = await MoveAxisAndVerifyAsync(
                    ResolveYAxis(Options.Side),
                    ResolveSideTarget(Options.Side, "Unload"),
                    Options.Side + " Y unload",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckTargetStageYUnload;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Y-UNLOAD-EX", Name, "Target stage Y unload move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageYUnload()
        {
            try
            {
                BinStageAxis axis = ResolveYAxis(Options.Side);
                double target = ResolveSideTarget(Options.Side, "Unload");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Y-UNLOAD-CHECK", Stage.Name,
                        Options.Side + " Y unload final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareUnloadStep.MoveTargetStageZToUnload;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Y-UNLOAD-CHECK-EX", Name, "Target stage Y unload check failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveTargetStageZToUnloadAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z unload"))
                {
                    // 기존 조건: Z축 없는 side(NG)는 바로 Complete로 건너뛰었다(실린더 준비 없음).
                    // CurrentStep = OutputStagePrepareUnloadStep.Complete;
                    // 현재 기준: Z 스킵 경로도 배출 최종 준비 상태(실린더)를 거친 뒤 종료한다.
                    CurrentStep = OutputStagePrepareUnloadStep.EnsureTargetStageUnloadReadyState;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(Options.Side),
                    ResolveSideZTarget(Options.Side, "Unload"),
                    Options.Side + " Z unload",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareUnloadStep.CheckTargetStageZUnload;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-UNLOAD-EX", Name,
                    "Target stage Z unload move failed. side=" + Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZUnload()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z unload final check"))
                {
                    // 기존 조건: 바로 Complete로 종료했다(실린더 준비 없음).
                    // CurrentStep = OutputStagePrepareUnloadStep.Complete;
                    // 현재 기준: 배출 최종 준비 상태(실린더)까지 만든 뒤 종료한다.
                    CurrentStep = OutputStagePrepareUnloadStep.EnsureTargetStageUnloadReadyState;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Unload");
                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Z-UNLOAD-CHECK", Stage.Name,
                        Options.Side + " Z unload final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                // 기존 조건: 위치 확인 후 바로 Complete로 종료했다(실린더 준비 없음).
                // CurrentStep = OutputStagePrepareUnloadStep.Complete;
                // 현재 기준: 배출 최종 준비 상태(실린더)까지 만든 뒤 종료한다.
                CurrentStep = OutputStagePrepareUnloadStep.EnsureTargetStageUnloadReadyState;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-Z-UNLOAD-CHECK-EX", Name,
                    "Target stage Z unload check failed. side=" + Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        // 기존 조건: PrepareUnload는 축 이동까지만 수행하고 실린더 준비는 하지 않았다.
        // 현재 기준: 언로드 위치 도착 후 배출 최종 준비 상태를 UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP 순서로 만든다.
        // To do: 스테이지 UNLOAD 준비에 실린더 최종 준비 상태 포함.
        private async Task<int> EnsureTargetStageUnloadReadyStateAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await Stage.EnsureBinGuideUnclampedAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-PREP-UNCLAMP", Stage.Name,
                        "OutputStage Unload 준비 중 Unclamp 구동 실패. side=" + Options.Side +
                        ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                if (!Stage.IsBinGuideUnclamped(Options.Side))
                    return Fail("OUT-STAGE-PREP-UNCLAMP", Stage.Name,
                        "OutputStage Unload 준비 중 Unclamp 상태 확인 실패. side=" + Options.Side +
                        ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                result = await Stage.EnsureBinGuideClampLiftDownAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-PREP-CLAMP-DOWN", Stage.Name,
                        "OutputStage Unload 준비 중 Clamp Lift Down 구동 실패. side=" + Options.Side +
                        ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                    return Fail("OUT-STAGE-PREP-CLAMP-DOWN", Stage.Name,
                        "OutputStage Unload 준비 중 Clamp Lift Down 상태 확인 실패. side=" + Options.Side +
                        ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                result = await Stage.EnsureBinGuideUpAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-PREP-GUIDE-UP", Stage.Name,
                        "OutputStage Unload 준비 중 Guide Up 구동 실패. side=" + Options.Side +
                        ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                if (!Stage.IsBinGuideUp(Options.Side))
                    return Fail("OUT-STAGE-PREP-GUIDE-UP", Stage.Name,
                        "OutputStage Unload 준비 중 Guide Up 상태 확인 실패. side=" + Options.Side +
                        ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                CurrentStep = OutputStagePrepareUnloadStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-PREP-READY-EX", Name, "Target stage unload ready state failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}

