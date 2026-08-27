using System;
using System.Threading;
using System.Threading.Tasks;

using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputStagePrepareLoadStep
    {
        Idle,
        CheckUnit,
        CheckTargetSide,
        EnsureOutputFeederSafeBeforeStageMove,
        EnsureNgClampLiftUpBeforeStageMove,
        MoveOppositeStageZToAvoid,
        CheckOppositeStageZAvoid,
        EnsureGoodGuideDownBeforeNgYMove,
        MoveTargetStageZToAvoidBeforeY,
        CheckTargetStageZAvoidBeforeY,
        MoveTargetStageYToLoad,
        CheckTargetStageYLoad,
        MoveTargetStageZToLoad,
        CheckTargetStageZLoad,
        // To do: 로드 위치 도착 후 수령 최종 준비 상태(UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP)까지 만든다.
        EnsureTargetStageReceiveReadyState,
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

                    // Stage Z/Y 이동 전 OutputFeeder 안전 위치 확보
                    case OutputStagePrepareLoadStep.EnsureOutputFeederSafeBeforeStageMove:
                        return EnsureOutputFeederSafeBeforeStageMoveAsync(ct);

                    // Good Z/NG Y 이동 전 NG Clamp Lift Up 확보
                    case OutputStagePrepareLoadStep.EnsureNgClampLiftUpBeforeStageMove:
                        return EnsureNgClampLiftUpBeforeStageMoveAsync(ct);

                    // 반대쪽 스테이지 Z로 어보이드 이동
                    case OutputStagePrepareLoadStep.MoveOppositeStageZToAvoid:
                        return MoveOppositeStageZToAvoidAsync(ct);

                    // 반대쪽 스테이지 Z 어보이드 확인
                    case OutputStagePrepareLoadStep.CheckOppositeStageZAvoid:
                        return Task.FromResult(CheckOppositeStageZAvoid());

                    // NG Y 이동 전 Good Guide Down 확보
                    case OutputStagePrepareLoadStep.EnsureGoodGuideDownBeforeNgYMove:
                        return EnsureGoodGuideDownBeforeNgYMoveAsync(ct);

                    // 대상 스테이지 Y 이동 전 대상 Z 어보이드 확보
                    case OutputStagePrepareLoadStep.MoveTargetStageZToAvoidBeforeY:
                        return MoveTargetStageZToAvoidBeforeYAsync(ct);

                    // 대상 스테이지 Y 이동 전 대상 Z 어보이드 확인
                    case OutputStagePrepareLoadStep.CheckTargetStageZAvoidBeforeY:
                        return Task.FromResult(CheckTargetStageZAvoidBeforeY());

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

                    // 수령 최종 준비 상태 확보 (UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP)
                    case OutputStagePrepareLoadStep.EnsureTargetStageReceiveReadyState:
                        return EnsureTargetStageReceiveReadyStateAsync(ct);

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

                CurrentStep = OutputStagePrepareLoadStep.EnsureOutputFeederSafeBeforeStageMove;
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

        private async Task<int> EnsureOutputFeederSafeBeforeStageMoveAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                OutputFeederUnit feeder = OutputFeeder;
                if (feeder == null)
                    return Fail("OUT-STAGE-FEEDER-NO-UNIT", "BinFeederUnit",
                        "OutputStage Load 준비 중 OutputFeederUnit을 찾을 수 없습니다. side=" + Options.Side);

                if (!Options.AllowOutputFeederActuation)
                {
                    if (!feeder.IsBinFeederYInAvoidPosition())
                        return Fail("OUT-STAGE-FEEDER-Y-MANUAL-POS", feeder.Name,
                            "메뉴얼 OutputStage Load는 OutputFeederY를 이동하지 않습니다. 시작 전 FeederY를 Avoid 위치로 이동하십시오. side=" +
                            Options.Side + ", " + feeder.DescribeBinFeederYMoveDoneState());

                    CurrentStep = OutputStagePrepareLoadStep.EnsureNgClampLiftUpBeforeStageMove;
                    return 0;
                }

                if (!feeder.IsFeederUnclamped())
                {
                    int result = await feeder.SetFeederClampAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("OUT-STAGE-FEEDER-UNCLAMP", feeder.Name,
                            "OutputStage Load 전 OutputFeeder Unclamp 명령 실패. result=" + result +
                            ", side=" + Options.Side + ", " + feeder.DescribeFeederCylinderState());
                }

                if (!feeder.IsFeederUnclamped())
                    return Fail("OUT-STAGE-FEEDER-UNCLAMP-CHECK", feeder.Name,
                        "OutputStage Load 전 OutputFeeder Unclamp 최종 확인 실패. side=" + Options.Side +
                        ", " + feeder.DescribeFeederCylinderState());

                if (!feeder.FeederY.IsAtTargetPosition(feeder.Recipe.AvoidPosition, 0.0))
                {
                    int moveResult = await feeder.MoveToFeederAvoidPosition(Options.FineMove).ConfigureAwait(false);
                    if (moveResult != 0)
                        return Fail("OUT-STAGE-FEEDER-Y-AVOID", feeder.Name,
                            "OutputStage Load 전 OutputFeederY Avoid 이동 명령 실패. result=" + moveResult +
                            ", side=" + Options.Side + ", " + feeder.DescribeBinFeederYMoveDoneState() +
                            feeder.DescribeBinFeederYLastMotionFailure());

                    // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
                }

                if (!feeder.IsBinFeederYInAvoidPosition())
                    return Fail("OUT-STAGE-FEEDER-Y-AVOID-CHECK", feeder.Name,
                        "OutputStage Load 전 OutputFeederY Avoid 최종 확인 실패. side=" + Options.Side +
                        ", " + feeder.DescribeBinFeederYMoveDoneState());

                CurrentStep = OutputStagePrepareLoadStep.EnsureNgClampLiftUpBeforeStageMove;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-FEEDER-SAFE-EX", "BinFeederUnit",
                    "OutputStage Load 전 OutputFeeder 안전 위치 확보 중 예외가 발생했습니다. side=" +
                    Options.Side + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureNgClampLiftUpBeforeStageMoveAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int liftResult = await Stage.EnsureBinGuideClampLiftUpAsync(
                    BinSide.Ng,
                    ResolveTimeout(),
                    ct).ConfigureAwait(false);
                if (liftResult != 0)
                {
                    return Fail("OUT-STAGE-NG-CLAMP-UP-BEFORE-MOVE", Stage.Name,
                        "OutputStage Load 이동 준비 중 NG Bin Clamp Lift Up 명령 실패. result=" +
                        liftResult + ", side=" + Options.Side + ", " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Ng));
                }

                if (!Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                {
                    return Fail("OUT-STAGE-NG-CLAMP-UP-CHECK-BEFORE-MOVE", Stage.Name,
                        "OutputStage Load 이동 준비 중 NG Bin Clamp Lift Up 최종 확인 실패. side=" +
                        Options.Side + ", " +
                        Stage.DescribeOutputStageInterlockState(BinSide.Ng));
                }

                CurrentStep = OutputStagePrepareLoadStep.MoveOppositeStageZToAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-NG-CLAMP-UP-PREP-EX", Name,
                    "OutputStage Load 전 NG Clamp Lift Up 확보 중 예외가 발생했습니다. side=" +
                    Options.Side + ", error=" + ex.Message);
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
                if (opposite == BinSide.Ng)
                {
                    int ngResult = await MoveAxisAndVerifyAsync(
                        ResolveYAxis(BinSide.Ng),
                        ResolveSideTarget(BinSide.Ng, "Avoid"),
                        "NG Y avoid before Good load",
                        ct).ConfigureAwait(false);

                    if (ngResult != 0)
                        return ngResult;

                    CurrentStep = OutputStagePrepareLoadStep.CheckOppositeStageZAvoid;
                    return 0;
                }

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
                if (opposite == BinSide.Ng)
                {
                    if (!Stage.IsNgStageInAvoidPosition())
                        return Fail("OUT-STAGE-OPP-NG-Y-CHECK", Stage.Name,
                            "NG Y avoid final check before Good load failed. " +
                            BuildAxisState(ResolveYAxis(BinSide.Ng), ResolveSideTarget(BinSide.Ng, "Avoid")));

                    CurrentStep = OutputStagePrepareLoadStep.EnsureGoodGuideDownBeforeNgYMove;
                    return 0;
                }

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
                    CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageZToAvoidBeforeY;
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

                CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageZToAvoidBeforeY;
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

        private async Task<int> MoveTargetStageZToAvoidBeforeYAsync(CancellationToken ct)
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid before Y load"))
                {
                    CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageYToLoad;
                    return 0;
                }

                int result = await MoveAxisAndVerifyAsync(
                    ResolveZAxis(Options.Side),
                    ResolveSideZTarget(Options.Side, "Avoid"),
                    Options.Side + " Z avoid before Y load",
                    ct).ConfigureAwait(false);

                if (result != 0)
                    return result;

                CurrentStep = OutputStagePrepareLoadStep.CheckTargetStageZAvoidBeforeY;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-TARGET-Z-AVOID-EX", Name, "Target stage Z avoid before Y load failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckTargetStageZAvoidBeforeY()
        {
            try
            {
                if (SkipMissingSideZAxis(Options.Side, Options.Side + " Z avoid final check before Y load"))
                {
                    CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageYToLoad;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Avoid");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-TARGET-Z-AVOID-CHECK", Stage.Name,
                        Options.Side + " Z avoid final check before Y load failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                CurrentStep = OutputStagePrepareLoadStep.MoveTargetStageYToLoad;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-TARGET-Z-AVOID-CHECK-EX", Name, "Target stage Z avoid check before Y load failed: " + ex.Message);
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
                    // 기존 조건: Z축 없는 side(NG)는 여기서 바로 Complete로 건너뛰어
                    //           실린더 준비 스텝(EnsureTargetStageReceiveReadyState)을 통과하지 못했다.
                    //           (NG CST->FEEDER에서 OUT-STAGE-LOAD-UNCLAMP 알람 발생 원인)
                    // CurrentStep = OutputStagePrepareLoadStep.Complete;
                    // 현재 기준: Z 스킵 경로도 수령 최종 준비 상태(실린더)를 거친 뒤 종료한다.
                    CurrentStep = OutputStagePrepareLoadStep.EnsureTargetStageReceiveReadyState;
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
                    // 기존 조건: 위치 확인 후 바로 Complete로 종료했다(실린더 준비 없음).
                    // CurrentStep = OutputStagePrepareLoadStep.Complete;
                    // 현재 기준: 수령 최종 준비 상태(실린더)까지 만든 뒤 종료한다.
                    CurrentStep = OutputStagePrepareLoadStep.EnsureTargetStageReceiveReadyState;
                    return 0;
                }

                BinStageAxis axis = ResolveZAxis(Options.Side);
                double target = ResolveSideZTarget(Options.Side, "Load");

                if (!Stage.IsStageAxisInPosition(axis, target, ResolveTolerance(axis)))
                    return Fail("OUT-STAGE-Z-LOAD-CHECK", Stage.Name,
                        Options.Side + " Z load final check failed. target=" + target + ". " +
                        BuildAxisState(axis, target));

                // 기존 조건: 위치 확인 후 바로 Complete로 종료했다(실린더 준비 없음).
                // CurrentStep = OutputStagePrepareLoadStep.Complete;
                // 현재 기준: 수령 최종 준비 상태(실린더)까지 만든 뒤 종료한다.
                CurrentStep = OutputStagePrepareLoadStep.EnsureTargetStageReceiveReadyState;
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

        // 기존 조건: PrepareLoad는 축 이동까지만 수행하고 실린더 준비는 하지 않아
        //           스테이지가 로드 위치에 있어도 GUIDE/CLAMP가 준비되지 않은 채 피더 픽이 진행됐다.
        // 현재 기준: 로드 위치 도착 후 수령 최종 준비 상태를 UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP 순서로 만든다.
        // To do: 스테이지 LOAD 준비에 실린더 최종 준비 상태 포함.
        private async Task<int> EnsureTargetStageReceiveReadyStateAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // 현재 기준(2026-08-27 팀장님 지시): 수령 준비 실린더 명령은 센서가 이미 목표 상태라도
                //           스킵하지 않고 무조건 정식 발행한다(클램프 UP 상태 로딩 재발 방지, 수동 로드 경로 포함).
                int result = await Stage.EnsureBinGuideUnclampedAsync(Options.Side, ResolveTimeout(), ct, forceCommand: true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-PREP-UNCLAMP", Stage.Name,
                        "OutputStage Load 준비 중 Unclamp 구동 실패. side=" + Options.Side +
                        ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                if (!Stage.IsBinGuideUnclamped(Options.Side))
                    return Fail("OUT-STAGE-PREP-UNCLAMP", Stage.Name,
                        "OutputStage Load 준비 중 Unclamp 상태 확인 실패. side=" + Options.Side +
                        ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                result = await Stage.EnsureBinGuideClampLiftDownAsync(Options.Side, ResolveTimeout(), ct, forceCommand: true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-PREP-CLAMP-DOWN", Stage.Name,
                        "OutputStage Load 준비 중 Clamp Lift Down 구동 실패. side=" + Options.Side +
                        ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                    return Fail("OUT-STAGE-PREP-CLAMP-DOWN", Stage.Name,
                        "OutputStage Load 준비 중 Clamp Lift Down 상태 확인 실패. side=" + Options.Side +
                        ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                result = await Stage.EnsureBinGuideUpAsync(Options.Side, ResolveTimeout(), ct, forceCommand: true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-PREP-GUIDE-UP", Stage.Name,
                        "OutputStage Load 준비 중 Guide Up 구동 실패. side=" + Options.Side +
                        ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                if (!Stage.IsBinGuideUp(Options.Side))
                    return Fail("OUT-STAGE-PREP-GUIDE-UP", Stage.Name,
                        "OutputStage Load 준비 중 Guide Up 상태 확인 실패. side=" + Options.Side +
                        ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

                CurrentStep = OutputStagePrepareLoadStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("OUT-STAGE-PREP-READY-EX", Name, "Target stage receive ready state failed: " + ex.Message);
            }
            finally
            {
            }
        }
    }
}

