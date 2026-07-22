using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederLoadToStageStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckFeederBinData,
        CheckOutputStageEmpty,
        EnsureOutputVisionAvoid,
        EnsurePickerAvoidPosition,
        EnsureStageMutualInterlock,
        MoveOutputStageLoadPosition,
        EnsureOutputStageGuideUp,
        EnsureOutputStageClampLiftDown,
        EnsureOutputStageUnclamp,
        VerifyOutputStageReceiveReady,
        VerifyFeederHoldingBin,
        MoveFeederStageLoadPosition,
        UnclampFeederBin,
        MoveFeederStageLoadAvoidPosition,
        // To do: 수령 후 상태 규격(GUIDE DOWN -> CLAMP LIFT UP -> CLAMP)의 첫 단계.
        LowerOutputStageGuideAfterReceive,
        LiftOutputStageClamp,
        ClampOutputStageBin,
        MoveMaterialDataToStage,
        PrepareFeederLiftUp,
        MoveFeederAvoidPosition,
        PrepareFeederLiftDownAfterAvoid,
        MoveNgStageAvoidAfterLoad,
        LowerNgStageGuideAfterAvoid,
        VerifyBinTransferredToStage,
        LowerOutputStageGuideBeforeProcess,
        MoveOutputStageProcessPosition,
        UpdateFeederData,
        MoveOutputCassetteAvoidPosition,
        Complete,
        Error
    }

    internal sealed class OutputFeederLoadToStageSequence : OutputFeederSequenceBase<OutputFeederLoadToStageStep>
    {
        public OutputFeederLoadToStageSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.LoadToStage, "OutputFeederLoadToStageSequence")
        {
        }

        protected override OutputFeederLoadToStageStep IdleStep { get { return OutputFeederLoadToStageStep.Idle; } }
        protected override OutputFeederLoadToStageStep InitialStep { get { return OutputFeederLoadToStageStep.CheckUnit; } }
        protected override OutputFeederLoadToStageStep CompleteStep { get { return OutputFeederLoadToStageStep.Complete; } }
        protected override OutputFeederLoadToStageStep ErrorStep { get { return OutputFeederLoadToStageStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputFeederLoadToStageStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederLoadToStageStep.CheckTransferReady));

                    // 이송 준비 확인
                    case OutputFeederLoadToStageStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());

                    // 피더 BIN 데이터 확인
                    case OutputFeederLoadToStageStep.CheckFeederBinData:
                        return Task.FromResult(CheckFeederBinData());

                    // 아웃풋 스테이지 비어있음 확인
                    case OutputFeederLoadToStageStep.CheckOutputStageEmpty:
                        return Task.FromResult(CheckOutputStageEmpty());

                    // 아웃풋 비전 어보이드 확보
                    case OutputFeederLoadToStageStep.EnsureOutputVisionAvoid:
                        return EnsureOutputVisionAvoidAsync(ct);

                    // 피커 어보이드 위치 확보
                    case OutputFeederLoadToStageStep.EnsurePickerAvoidPosition:
                        return EnsurePickerAvoidPositionAsync(ct);

                    // 스테이지 상호 인터락 확보
                    case OutputFeederLoadToStageStep.EnsureStageMutualInterlock:
                        return EnsureStageMutualInterlockAsync(ct);

                    // 아웃풋 스테이지 로드 위치 이동
                    case OutputFeederLoadToStageStep.MoveOutputStageLoadPosition:
                        return MoveOutputStageLoadPositionAsync(ct);

                    // 아웃풋 스테이지 가이드 업 확보
                    case OutputFeederLoadToStageStep.EnsureOutputStageGuideUp:
                        return EnsureOutputStageGuideUpAsync(ct);

                    // 아웃풋 스테이지 클램프 리프트 다운 확보
                    case OutputFeederLoadToStageStep.EnsureOutputStageClampLiftDown:
                        return EnsureOutputStageClampLiftDownAsync(ct);

                    // 아웃풋 스테이지 언클램프 확보
                    case OutputFeederLoadToStageStep.EnsureOutputStageUnclamp:
                        return EnsureOutputStageUnclampAsync(ct);

                    // 아웃풋 스테이지 수령 준비 검증
                    case OutputFeederLoadToStageStep.VerifyOutputStageReceiveReady:
                        return VerifyOutputStageReceiveReadyAsync(ct);

                    // 피더 보유 BIN 검증
                    case OutputFeederLoadToStageStep.VerifyFeederHoldingBin:
                        return VerifyFeederHoldingBinAsync(ct);

                    // 피더 스테이지 로드 위치 이동
                    case OutputFeederLoadToStageStep.MoveFeederStageLoadPosition:
                        return MoveFeederStageLoadPositionAsync(ct);

                    // 피더 BIN 언클램프
                    case OutputFeederLoadToStageStep.UnclampFeederBin:
                        return UnclampFeederBinAsync(ct);

                    // 피더 스테이지 로드 어보이드 위치 이동
                    case OutputFeederLoadToStageStep.MoveFeederStageLoadAvoidPosition:
                        return MoveFeederStageLoadAvoidPositionAsync(ct);

                    // 수령 후 아웃풋 스테이지 가이드 다운
                    case OutputFeederLoadToStageStep.LowerOutputStageGuideAfterReceive:
                        return LowerOutputStageGuideAfterReceiveAsync(ct);

                    // 아웃풋 스테이지 클램프 리프트 업
                    case OutputFeederLoadToStageStep.LiftOutputStageClamp:
                        return LiftOutputStageClampAsync(ct);

                    // 아웃풋 스테이지 BIN 클램프
                    case OutputFeederLoadToStageStep.ClampOutputStageBin:
                        return ClampOutputStageBinAsync(ct);

                    // 자재 데이터를 스테이지로 이동
                    case OutputFeederLoadToStageStep.MoveMaterialDataToStage:
                        return Task.FromResult(MoveMaterialDataToStage());

                    // 피더 리프트 업 준비
                    case OutputFeederLoadToStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);

                    // 피더 어보이드 위치 이동
                    case OutputFeederLoadToStageStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);

                    // 피더 리프트 다운 후 어보이드 준비
                    case OutputFeederLoadToStageStep.PrepareFeederLiftDownAfterAvoid:
                        return PrepareFeederLiftDownAfterAvoidAsync(ct);

                    // NG Bin 교체 완료 후 NG 스테이지를 무조건 어보이드로 이동
                    case OutputFeederLoadToStageStep.MoveNgStageAvoidAfterLoad:
                        return MoveNgStageAvoidAfterLoadAsync(ct);

                    // NG 스테이지 어보이드 도착 후 가이드 다운
                    case OutputFeederLoadToStageStep.LowerNgStageGuideAfterAvoid:
                        return LowerNgStageGuideAfterAvoidAsync(ct);

                    // 스테이지 BIN 전달 검증
                    case OutputFeederLoadToStageStep.VerifyBinTransferredToStage:
                        return VerifyBinTransferredToStageAsync(ct);

                    // 프로세스 이동 전 아웃풋 스테이지 가이드 다운
                    case OutputFeederLoadToStageStep.LowerOutputStageGuideBeforeProcess:
                        return LowerOutputStageGuideBeforeProcessAsync(ct);

                    // 아웃풋 스테이지 프로세스 위치 이동
                    case OutputFeederLoadToStageStep.MoveOutputStageProcessPosition:
                        return MoveOutputStageProcessPositionAsync(ct);

                    // 피더 데이터 갱신
                    case OutputFeederLoadToStageStep.UpdateFeederData:
                        return Task.FromResult(UpdateFeederData());

                    // 아웃풋 카세트 어보이드 위치 이동
                    case OutputFeederLoadToStageStep.MoveOutputCassetteAvoidPosition:
                        return MoveOutputCassetteAvoidPositionAsync(ct);

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-STAGE-LOAD-EX", Name, "Load to stage step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            string readyReason;
            if (!Feeder.CheckFeederStageReady(Options.Side, TransferMode.Load, out readyReason))
                return Fail("OUT-FEEDER-STAGE-LOAD-READY", Feeder.Name, "Output feeder stage load is not ready. " + readyReason);

            CurrentStep = OutputFeederLoadToStageStep.CheckFeederBinData;
            return 0;
        }

        private int CheckFeederBinData()
        {
            return CheckFeederReadyForStageLoad(OutputFeederLoadToStageStep.CheckOutputStageEmpty);
        }

        private int CheckOutputStageEmpty()
        {
            if (ResolveStageWafer() != null)
                return Fail("OUT-STAGE-DATA-OCCUPIED", "Material", "Output stage data became occupied before feeder to stage load. side=" + Options.Side);

            if (Stage == null)
                return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available. side=" + Options.Side);

            CurrentStep = OutputFeederLoadToStageStep.EnsureOutputVisionAvoid;
            return 0;
        }

        private async Task<int> EnsureOutputVisionAvoidAsync(CancellationToken ct)
        {
            if (Stage == null ||
                Stage.OutputCameraX == null ||
                Stage.Recipe == null ||
                Stage.Recipe.VisionX == null)
                return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available. side=" + Options.Side);

            Stage.Recipe.EnsurePositionObjects();
            double target = Stage.Recipe.VisionX.AvoidPosition;
            if (!QMC.Common.Motion.AxisMoveWaiter.CanSkipMoveCommandAtTarget(Stage.OutputCameraX, target))
            {
                int result = await Stage.MoveVisionXToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-VISION-AVOID", Stage.Name, "OutputVisionX avoid move failed. side=" + Options.Side + ", result=" + result);
            }

            if (!Stage.IsVisionXInAvoidPosition())
                return Fail("OUT-STAGE-VISION-AVOID", Stage.Name, "OutputVisionX is not in avoid position before feeder to stage load. side=" + Options.Side);

            CurrentStep = OutputFeederLoadToStageStep.EnsurePickerAvoidPosition;
            return 0;
        }

        private async Task<int> EnsurePickerAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            int pickerClear = await WaitPickersClearForOutputTransportAsync("OutputStage Load 준비", ct).ConfigureAwait(false);
            if (pickerClear != 0)
                return pickerClear;

            CurrentStep = OutputFeederLoadToStageStep.EnsureStageMutualInterlock;
            return 0;
        }

        private async Task<int> EnsureStageMutualInterlockAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Stage 위치 이동은 OutputStagePrepareLoadSequence에서 Feeder가 카세트로 가기 전에 끝나야 한다.
            // 이 시점에는 Feeder가 Bin을 Clamp하고 있으므로 Stage 축을 보정 이동하지 않고 도착 상태만 검증한다.
            if (!Stage.IsStageInLoadPosition(Options.Side))
                return Fail("OUT-STAGE-LOAD-POS", Stage.Name,
                    "OutputStage가 Feeder 이송 시작 전에 Load 위치에 준비되지 않았습니다. side=" + Options.Side + ", " +
                    Stage.DescribeStageLoadMoveState(Options.Side));

            int result = await Stage.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name,
                    "NG stage clamp lift up failed before feeder transfer. side=" + Options.Side + ", result=" + result + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name, "NG stage clamp lift must be up before stage load movement. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (Options.Side == BinSide.Ng)
            {
                if (!Stage.IsGoodStageZInAvoidPosition())
                    return Fail("OUT-STAGE-GOOD-Z-AVOID", Stage.Name, "NG Stage Load 이동 전 GoodStageZ가 Avoid 위치가 아닙니다. " + Stage.DescribeOutputStageInterlockState(Options.Side));
            }
            if (Options.Side != BinSide.Ng && !Stage.IsNgStageInAvoidPosition())
                return Fail("OUT-STAGE-NG-AVOID", Stage.Name, "NG stage must be avoid before GOOD stage receives bin. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            // 기존 조건: Good은 UNCLAMP -> GUIDE UP -> CLAMP LIFT DOWN, NG는 GUIDE UP -> CLAMP LIFT DOWN -> UNCLAMP 순서였다.
            // CurrentStep = Options.Side == BinSide.Good
            //     ? OutputFeederLoadToStageStep.EnsureOutputStageUnclamp
            //     : OutputFeederLoadToStageStep.EnsureOutputStageGuideUp;
            // 현재 기준: 수령 최종 준비 상태는 양쪽 공통 UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP 순서로 만든다.
            // To do: 스테이지 수령 준비 실린더 순서 통일(UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP).
            CurrentStep = OutputFeederLoadToStageStep.EnsureOutputStageUnclamp;
            return 0;
        }

        private Task<int> MoveOutputStageLoadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!Stage.IsStageInLoadPosition(Options.Side))
                return Task.FromResult(Fail("OUT-STAGE-LOAD-POS", Stage.Name,
                    "OutputStage가 Feeder 이송 시작 전에 Load 위치에 준비되지 않았습니다. side=" + Options.Side +
                    ", " + Stage.DescribeStageLoadMoveState(Options.Side)));

            // 기존 조건: Good -> 수령 준비 검증, NG -> GUIDE UP으로 분기했다.
            // CurrentStep = Options.Side == BinSide.Good
            //     ? OutputFeederLoadToStageStep.VerifyOutputStageReceiveReady
            //     : OutputFeederLoadToStageStep.EnsureOutputStageGuideUp;
            // 현재 기준: 수령 준비 체인(UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP)의 시작으로 통일.
            CurrentStep = OutputFeederLoadToStageStep.EnsureOutputStageUnclamp;
            return Task.FromResult(0);
        }

        private async Task<int> EnsureOutputStageGuideUpAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideUpAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-GUIDE-UP", Stage.Name, "Output stage bin guide up failed. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUp(Options.Side))
                return Fail("OUT-STAGE-GUIDE-UP", Stage.Name, "Output stage bin guide is not up. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            // 기존 조건: GUIDE UP 다음 CLAMP LIFT DOWN으로 이어졌다.
            // CurrentStep = OutputFeederLoadToStageStep.EnsureOutputStageClampLiftDown;
            // 현재 기준: GUIDE UP이 수령 준비의 마지막 단계 - 바로 수령 준비 검증으로 넘어간다.
            CurrentStep = OutputFeederLoadToStageStep.VerifyOutputStageReceiveReady;
            return 0;
        }

        private async Task<int> EnsureOutputStageClampLiftDownAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideClampLiftDownAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift down failed. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift is not down. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            // 기존 조건: Good -> 수령 준비 검증, NG -> UNCLAMP로 분기했다.
            // CurrentStep = Options.Side == BinSide.Good
            //     ? OutputFeederLoadToStageStep.VerifyOutputStageReceiveReady
            //     : OutputFeederLoadToStageStep.EnsureOutputStageUnclamp;
            // 현재 기준: CLAMP LIFT DOWN 다음은 양쪽 공통 GUIDE UP.
            CurrentStep = OutputFeederLoadToStageStep.EnsureOutputStageGuideUp;
            return 0;
        }

        private async Task<int> EnsureOutputStageUnclampAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideUnclampedAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide unclamp failed. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide is not unclamped. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            // 기존 조건: Good -> GUIDE UP, NG -> 수령 준비 검증으로 분기했다.
            // CurrentStep = Options.Side == BinSide.Good
            //     ? OutputFeederLoadToStageStep.EnsureOutputStageGuideUp
            //     : OutputFeederLoadToStageStep.VerifyOutputStageReceiveReady;
            // 현재 기준: UNCLAMP 다음은 양쪽 공통 CLAMP LIFT DOWN.
            CurrentStep = OutputFeederLoadToStageStep.EnsureOutputStageClampLiftDown;
            return 0;
        }

        private async Task<int> VerifyOutputStageReceiveReadyAsync(CancellationToken ct)
        {
            // 기존 조건: GUIDE UP -> CLAMP LIFT DOWN -> UNCLAMP 순서로 확인했다.
            // 현재 기준: 수령 최종 준비 상태 규격(UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP) 순서로 확인한다.
            // To do: 수령 준비 최종 검증 순서를 상태 규격과 일치시킴.
            int result = await Stage.EnsureBinGuideUnclampedAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-RECEIVE-UNCLAMP", Stage.Name,
                    "OutputStage Bin 수령 전 Unclamp 구동 실패. side=" + Options.Side +
                    ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-RECEIVE-UNCLAMP", Stage.Name,
                    "OutputStage Bin 수령 전 Unclamp 상태가 아닙니다. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            result = await Stage.EnsureBinGuideClampLiftDownAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-RECEIVE-CLAMP-DOWN", Stage.Name,
                    "OutputStage Bin 수령 전 Clamp Lift Down 구동 실패. side=" + Options.Side +
                    ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-RECEIVE-CLAMP-DOWN", Stage.Name,
                    "OutputStage Bin 수령 전 Clamp Lift가 Down 상태가 아닙니다. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            result = await Stage.EnsureBinGuideUpAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-RECEIVE-GUIDE-UP", Stage.Name,
                    "OutputStage Bin 수령 전 Guide Up 구동 실패. side=" + Options.Side +
                    ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUp(Options.Side))
                return Fail("OUT-STAGE-RECEIVE-GUIDE-UP", Stage.Name,
                    "OutputStage Bin 수령 전 Guide가 Up 상태가 아닙니다. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.VerifyFeederHoldingBin;
            return 0;
        }

        private Task<int> VerifyFeederHoldingBinAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (Feeder.IsFeederUnclamped())
                return Task.FromResult(Fail("OUT-FEEDER-CLAMP-CHECK", Feeder.Name,
                    "OutputFeeder가 BIN을 Stage로 이송하기 전 Clamp 상태가 아닙니다. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState()));

            if (!Feeder.IsFeederDown())
                return Task.FromResult(Fail("OUT-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "OutputFeeder가 BIN을 Stage로 이송하기 전 Down 상태가 아닙니다. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState()));

            CurrentStep = OutputFeederLoadToStageStep.MoveFeederStageLoadPosition;
            return Task.FromResult(0);
        }

        private async Task<int> MoveFeederStageLoadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageLoadPosition(Options.Side, Options.FineMove), "stage load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageLoadPosition(Options.Side), "stage load", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederLoadToStageStep.UnclampFeederBin;
            return 0;
        }

        private async Task<int> UnclampFeederBinAsync(CancellationToken ct)
        {
            int result = await Feeder.SetFeederClampAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp command failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name, "Output feeder unclamp failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederLoadToStageStep.MoveFeederStageLoadAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageLoadAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageLoadAvoidPosition(Options.Side, Options.FineMove), "stage load avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageLoadAvoidPosition(Options.Side), "stage load avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            // 기존 조건: 피더 어보이드 직후 바로 CLAMP LIFT UP -> CLAMP 순서였고 GUIDE DOWN은 프로세스 이동 직전에 했다.
            // CurrentStep = OutputFeederLoadToStageStep.LiftOutputStageClamp;
            // 현재 기준: 수령 후 상태 규격 GUIDE DOWN -> CLAMP LIFT UP -> CLAMP 순서를 따른다.
            // To do: 수령 후 실린더 순서 변경(GUIDE DOWN 선행).
            CurrentStep = OutputFeederLoadToStageStep.LowerOutputStageGuideAfterReceive;
            return 0;
        }

        // To do: 수령 후 상태 규격(GUIDE DOWN -> CLAMP LIFT UP -> CLAMP)의 첫 단계 - 가이드 다운.
        private async Task<int> LowerOutputStageGuideAfterReceiveAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideDownAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-GUIDE-DOWN-AFTER-RECEIVE", Stage.Name,
                    "Output stage guide down failed after bin receive. side=" + Options.Side + ", result=" + result + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideDown(Options.Side))
                return Fail("OUT-STAGE-GUIDE-DOWN-AFTER-RECEIVE", Stage.Name,
                    "Output stage guide is not down after bin receive. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.LiftOutputStageClamp;
            return 0;
        }

        private async Task<int> LiftOutputStageClampAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideClampLiftUpAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-UP", Stage.Name, "Output stage bin clamp lift up failed. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftUp(Options.Side))
                return Fail("OUT-STAGE-CLAMP-UP", Stage.Name, "Output stage bin clamp lift is not up after feeder stage load avoid. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.ClampOutputStageBin;
            return 0;
        }

        private async Task<int> ClampOutputStageBinAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideClampedAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP", Stage.Name, "Output stage bin clamp failed after feeder stage load avoid. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClamped(Options.Side))
                return Fail("OUT-STAGE-CLAMP", Stage.Name, "Output stage bin clamp final check failed after feeder stage load avoid. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.MoveMaterialDataToStage;
            return 0;
        }

        private async Task<int> PrepareFeederLiftUpAsync(CancellationToken ct)
        {
            if (Feeder.IsFeederUp())
            {
                CurrentStep = OutputFeederLoadToStageStep.MoveFeederAvoidPosition;
                return 0;
            }

            int result = await Feeder.SetFeederUpDownAsync(true, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift up after stage load avoid failed. side=" + Options.Side + ", result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUp())
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift is not up after stage load avoid. side=" + Options.Side + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederLoadToStageStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederAvoidPosition(Options.FineMove), "stage load feeder avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederInAvoidPosition(), "stage load feeder avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederLoadToStageStep.PrepareFeederLiftDownAfterAvoid;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAfterAvoidAsync(CancellationToken ct)
        {
            if (Feeder.IsFeederDown())
            {
                CurrentStep = ResolveAfterFeederAvoidStep();
                return 0;
            }

            int result = await Feeder.SetFeederUpDownAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift down after feeder avoid failed. side=" + Options.Side + ", result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift is not down after feeder avoid. side=" + Options.Side + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = ResolveAfterFeederAvoidStep();
            return 0;
        }

        private OutputFeederLoadToStageStep ResolveAfterFeederAvoidStep()
        {
            return Options.Side == BinSide.Ng
                ? OutputFeederLoadToStageStep.MoveNgStageAvoidAfterLoad
                : OutputFeederLoadToStageStep.VerifyBinTransferredToStage;
        }

        private async Task<int> MoveNgStageAvoidAfterLoadAsync(CancellationToken ct)
        {
            int result = await Stage.MoveNgStageToAvoidAndVerifyAsync(ResolveTimeout(), Options.FineMove, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-NG-AVOID-AFTER-LOAD", Stage.Name,
                    "NG Bin 교체 후 NG Stage Avoid 이동 실패. result=" + result + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsNgStageInAvoidPosition())
                return Fail("OUT-STAGE-NG-AVOID-AFTER-LOAD", Stage.Name,
                    "NG Bin 교체 후 NG Stage가 Avoid 위치가 아닙니다. " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.LowerNgStageGuideAfterAvoid;
            return 0;
        }

        private async Task<int> LowerNgStageGuideAfterAvoidAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideDownAsync(BinSide.Ng, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-NG-GUIDE-DOWN", Stage.Name,
                    "NG Stage Avoid 이동 후 Guide Down 실패. result=" + result + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideDown(BinSide.Ng))
                return Fail("OUT-STAGE-NG-GUIDE-DOWN", Stage.Name,
                    "NG Stage Avoid 이동 후 Guide가 Down 상태가 아닙니다. " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.VerifyBinTransferredToStage;
            return 0;
        }

        private async Task<int> VerifyBinTransferredToStageAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("OUT-STAGE-DATA-MISSING", "Material", "Output stage data was not created before transfer verification. side=" + Options.Side);

            if (!IsHardwareBypass())
            {
                bool cleared = await Feeder.WaitFeederRingState(false, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("OUT-FEEDER-STAGE-RING", Feeder.Name, "Output feeder ring remained after stage load. waferId=" + wafer.WaferId);
            }

            CurrentStep = Options.Side == BinSide.Ng
                ? OutputFeederLoadToStageStep.UpdateFeederData
                : OutputFeederLoadToStageStep.LowerOutputStageGuideBeforeProcess;
            return 0;
        }

        // 현재 기준: 수령 직후 LowerOutputStageGuideAfterReceive에서 이미 GUIDE DOWN이 완료되므로
        //           이 스텝은 프로세스 이동 전 상태 재검증(멱등) 역할이다.
        private async Task<int> LowerOutputStageGuideBeforeProcessAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideDownAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-GUIDE-DOWN-BEFORE-PROCESS", Stage.Name,
                    "Output stage guide down failed before process move. side=" + Options.Side + ", result=" + result + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideDown(Options.Side))
                return Fail("OUT-STAGE-GUIDE-DOWN-BEFORE-PROCESS", Stage.Name,
                    "Output stage guide is not down before process move. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.MoveOutputStageProcessPosition;
            return 0;
        }

        private async Task<int> MoveOutputStageProcessPositionAsync(CancellationToken ct)
        {
            int pickerClear = await WaitPickersClearForOutputTransportAsync("OutputStage Process 이동 준비", ct).ConfigureAwait(false);
            if (pickerClear != 0)
                return pickerClear;

            var stageOptions = OutputStageSequenceOptions.Default();
            stageOptions.Side = Options.Side;
            stageOptions.Grade = Options.Side == BinSide.Ng ? DieGrade.Ng : DieGrade.Good;
            stageOptions.FineMove = Options.FineMove;
            stageOptions.MoveTimeoutMs = ResolveTimeout();
            stageOptions.RunMode = Options.RunMode;
            stageOptions.StartMode = SequenceStartMode.Restart;
            stageOptions.KeepVisionXAvoidOnProcessMove = true;

            int result = await new OutputStageSequence(Context)
                .RunMoveProcessAsync(ct, stageOptions)
                .ConfigureAwait(false);

            if (result != 0)
                return Fail("OUT-STAGE-PROCESS-AFTER-LOAD", "OutputStage",
                    "Output stage process move failed after bin load. side=" + Options.Side + ", result=" + result + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederLoadToStageStep.UpdateFeederData;
            return 0;
        }

        private int MoveMaterialDataToStage()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Output feeder wafer data was not found for stage material move.");
            if (ResolveStageWafer() != null)
                return Fail("OUT-STAGE-DATA-OCCUPIED", "Material", "Output stage data became occupied before feeder to stage material move. side=" + Options.Side);

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("OUT-STAGE-MATERIAL-WAFER", "Material", "물리 이송 후 Stage Material 갱신 직전에 Bin ID가 변경되었습니다. expected=" + Options.ExpectedWaferId + ", actual=" + wafer.WaferId);

            CassetteMaterialRole sourceRole = wafer.SourceCassetteRole;
            if ((Options.Side == BinSide.Ng && sourceRole != CassetteMaterialRole.Ng1) ||
                (Options.Side == BinSide.Good && sourceRole != CassetteMaterialRole.Good1 && sourceRole != CassetteMaterialRole.Good2))
                return Fail("OUT-STAGE-MATERIAL-SIDE", "Material", "Output side와 source cassette role이 일치하지 않습니다. wafer=" + wafer.WaferId + ", side=" + Options.Side + ", sourceRole=" + sourceRole);

            MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = ResolveOutputStageLocation() }, WaferMaterialState.Working);
            MaterialStateService.InitializeOutputStageReceivePlan(Options.Side);
            Feeder.ClearFeederMaterialState();
            CurrentStep = OutputFeederLoadToStageStep.PrepareFeederLiftUp;
            return 0;
        }

        private int UpdateFeederData()
        {
            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-CLEAR", "Material", "Output feeder data was not cleared after stage load. side=" + Options.Side);

            if (ResolveStageWafer() == null)
                return Fail("OUT-STAGE-DATA-MISSING", "Material", "Output stage data was not created after feeder to stage load. side=" + Options.Side);

            if (Options.Side == BinSide.Ng && !Stage.IsNgStageInAvoidPosition())
                return Fail("OUT-STAGE-NG-AVOID-FINAL", Stage.Name,
                    "NG Bin 교체 완료 시 NG Stage는 반드시 Avoid 위치여야 합니다. " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            Context.Bus.Set("OutputFeederEmpty");
            Context.Bus.Set("OutputStageOccupied");
            CurrentStep = OutputFeederLoadToStageStep.MoveOutputCassetteAvoidPosition;
            return 0;
        }
    }
}

