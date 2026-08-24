using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Barcode;
using QMC.CDT320.Materials;
using QMC.Common.Motion;

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
        RunBarcodeSequence,
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
            : this(context, false)
        {
        }

        // [P2 2026-08-22, 검토수정 2026-08-22 재시작 바코드 게이트] startAtStageBarcodeVerify=true면
        // Bin이 이미 OutputStage에 있는 재시작 상황용으로, CheckUnit(유닛/피더 이동 준비 검증)을 정상
        // 통과한 뒤 이적재 전반부를 건너뛰고 VerifyBinTransferredToStage(Ring 재확인) → 바코드 판독 →
        // Guide/공정 위치 → 데이터 검증 → 카세트 Avoid로 진행한다(UpdateFeederData는 검증+신호뿐이라
        // 재실행 안전). StartMode=Restart로 호출. InitialStep을 바꾸지 않는 이유와 재개 키 분리는
        // 인풋(InputFeederLoadToStageSequence) 주석과 동일 — 무검증 모션/재개 오염 방지.
        internal OutputFeederLoadToStageSequence(MachineSequenceContext context, bool startAtStageBarcodeVerify)
            : base(context, OutputFeederSequenceKind.LoadToStage, "OutputFeederLoadToStageSequence")
        {
            _startAtStageBarcodeVerify = startAtStageBarcodeVerify;
        }

        private readonly bool _startAtStageBarcodeVerify;

        protected override string SequenceStateNameSuffix
        {
            get { return _startAtStageBarcodeVerify ? ".BarcodeRecovery" : ""; }
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
                        // [검토수정 2026-08-22] 바코드 복구 진입도 CheckUnit 검증은 그대로 수행하고,
                        // 다음 스텝만 Ring 전달 재확인으로 건너뛴다(이적재 전반부 생략).
                        return Task.FromResult(CheckUnit(_startAtStageBarcodeVerify
                            ? OutputFeederLoadToStageStep.VerifyBinTransferredToStage
                            : OutputFeederLoadToStageStep.CheckTransferReady));

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
                        return MoveMaterialDataToStageAsync(ct);

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

                    // Stage 적재와 Feeder Avoid/Down 확인 후 OutputCameraX에서 Bin 바코드 판독
                    case OutputFeederLoadToStageStep.RunBarcodeSequence:
                        return RunBarcodeSequenceAsync(ct);

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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                // CYCLE STOP 정지는 제어 흐름이므로 고장으로 바꾸지 않고 상위 정지 처리로 전달한다.
                throw;
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
            if (!Stage.OutputCameraX.IsAtTargetPosition(target, 0.0))
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

            // 기존 조건: side와 무관하게 NG Clamp Lift Up을 강제/검증했다.
            //   문제 1 - NG 이송에서는 두 스텝 뒤 EnsureOutputStageClampLiftDown이 다시 Down으로 내리므로
            //            불필요한 Up<->Down 왕복이 발생한다.
            //   문제 2 - 이 Up 명령이 카세트 리프터 이동과 겹치면 인터락에 걸려 Critical 알람 + 전축 비상정지가
            //            발생한다. (실장비 2026-07-25 23:01:16
            //            "NGBinGuideClampLift move Fwd blocked. OutputLifterZ is moving.")
            // 현재 기준(사용자 확인 2026-07-25):
            //   - NG Clamp Lift Up이 필요한 조건은 "Stage 축(GOOD/NG)이 실제로 움직일 때"이며,
            //     그 강제는 축 이동 관문(OutputStageUnit.MoveStageAxis / EnsureNgStageYMoveClearAsync /
            //     MotionGuard VerifyNgClampSafeForStageMove)에서 이미 수행·검증된다.
            //   - 이 메서드는 위 주석대로 "Stage 축을 이동하지 않고 도착 상태만 검증"하는 구간이므로
            //     Up 조건 대상이 아니다. 피더가 Bin을 주고받는 동안 대상 side는 Clamp Lift Down이어야 한다.
            //   - 따라서 NG side 이송에서는 여기서 Up을 강제하지 않는다.
            //     GOOD side 이송에서는 NG가 유휴 상태이고 이후 GOOD 축이 움직이므로 기존대로 Up을 유지한다.
            if (Options.Side != BinSide.Ng)
            {
                int result = await Stage.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, ResolveTimeout(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name,
                        "NG stage clamp lift up failed before feeder transfer. side=" + Options.Side + ", result=" + result + ", " +
                        Stage.DescribeOutputStageInterlockState(Options.Side));

                if (!Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                    return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name, "NG stage clamp lift must be up before stage load movement. " + Stage.DescribeOutputStageInterlockState(Options.Side));
            }

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
            // Material 이동과 Ring 전달 확인이 끝나기 전에는 GOOD/NG StageY를 움직이지 않는다.
            return OutputFeederLoadToStageStep.VerifyBinTransferredToStage;
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

            // Ring 전달 검증과 바코드 판독은 NG Avoid 이동 전에 이미 완료된다.
            CurrentStep = OutputFeederLoadToStageStep.UpdateFeederData;
            return 0;
        }

        private async Task<int> VerifyBinTransferredToStageAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("OUT-STAGE-DATA-MISSING", "Material", "Output stage data was not created before transfer verification. side=" + Options.Side);

            var stageRingSensor =
                Options.Side == BinSide.Ng
                    ? Stage.NgBinRingSensor
                    : Stage.GoodBinRingSensor;
            bool controllerGlobalDryRun =
                Context != null &&
                Context.Controller != null &&
                Context.Controller.GlobalDryRun;
            bool virtualFeederRingState = ResolveFeederWafer() != null;
            bool virtualStageRingState = wafer != null;
            bool transferred = await Feeder.WaitTransportRingStatesConfirmedAsync(
                false,
                stageRingSensor,
                true,
                controllerGlobalDryRun,
                virtualFeederRingState,
                virtualStageRingState,
                ResolveTimeout(),
                ct).ConfigureAwait(false);
            if (!transferred)
                return Fail(
                    "OUT-FEEDER-STAGE-RING",
                    Feeder.Name,
                    "OutputFeeder→Stage 전달 후 Feeder Ring OFF + " +
                    Options.Side +
                    " Stage Ring ON 안정 확인에 실패했습니다. waferId=" +
                    wafer.WaferId + ", detail=" +
                    Feeder.LastTransportRingConfirmationFailure);

            CurrentStep = OutputFeederLoadToStageStep.RunBarcodeSequence;
            return 0;
        }

        private async Task<int> RunBarcodeSequenceAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options == null || !Options.UseBarcode)
                {
                    CurrentStep = ResolveAfterBarcodeStep();
                    return 0;
                }

                if (Stage == null || Stage.OutputCameraX == null || Stage.Recipe == null)
                    return Fail("OUT-BARCODE-STAGE-MISSING", "OutputStage",
                        "Output Bin barcode 판독에 필요한 OutputStage/OutputCameraX를 확인할 수 없습니다.");

                Stage.Recipe.EnsurePositionObjects();
                WaferMaterial wafer = ResolveStageWafer();
                if (wafer == null)
                    return Fail("OUT-BARCODE-MATERIAL-MISSING", "Material",
                        "Output Bin barcode 판독 전 선택 Stage의 Material 데이터가 없습니다. side=" + Options.Side);

                // 재개 시 동일 물리 Bin의 판독 완료 정보가 이미 저장되어 있으면 중복 판독하지 않는다.
                if (wafer.BarcodeConfirmed && IsUsableBarcode(wafer.BarcodeId))
                {
                    int resumeAvoid = await EnsureOutputVisionAvoidAfterBarcodeAsync(
                        "barcode-confirmed resume",
                        ct).ConfigureAwait(false);
                    if (resumeAvoid != 0)
                        return resumeAvoid;

                    Options.ExpectedWaferId = wafer.WaferId ?? string.Empty;
                    // [검토수정 2026-08-22] 생략도 수행으로 확정 — 인풋과 동일 사유(게이트 재발동 방지).
                    MaterialStateService.MarkWaferBarcodeSequencePerformed(wafer.WaferInstanceId, Name + ":ResumeSkip");
                    WriteLog(Name,
                        "Output Bin barcode already confirmed. scan skipped. side=" + Options.Side +
                        ", waferInstanceId=" + wafer.WaferInstanceId +
                        ", barcode=" + wafer.BarcodeId + " - Ok");
                    CurrentStep = ResolveAfterBarcodeStep();
                    return 0;
                }

                AppSettings settings = AppSettingsStore.Current ?? new AppSettings();
                int readTimeoutMs = settings.OutputBarcodeReadTimeoutMs > 0
                    ? settings.OutputBarcodeReadTimeoutMs
                    : 3000;
                int retryCount = Math.Max(0, settings.OutputBarcodeRetryCount);
                double retryStepMm = Math.Abs(settings.OutputBarcodeRetryStepMm);
                int totalAttempts = 0;
                string lastFailure = string.Empty;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    IBarcodeReader reader = Context != null && Context.Machine != null
                        ? Context.Machine.BinBarcodeReader
                        : null;
                    string readerFailure;
                    if (!TryEnsureBarcodeReaderReady(reader, out readerFailure))
                    {
                        BarcodeRecoveryResponse unavailableResponse = await RequestBarcodeRecoveryAsync(
                            wafer,
                            readerFailure,
                            retryCount,
                            retryStepMm,
                            ct).ConfigureAwait(false);

                        ApplyRecoveryRetryParameters(unavailableResponse, ref retryCount, ref retryStepMm);
                        if (unavailableResponse != null &&
                            unavailableResponse.Decision == BarcodeRecoveryDecision.ManualApply)
                        {
                            int manualResult = await ApplyBarcodeAndFinishAsync(
                                wafer,
                                unavailableResponse.ManualBarcode,
                                totalAttempts,
                                "Manual",
                                ct).ConfigureAwait(false);
                            return manualResult;
                        }

                        if (unavailableResponse == null ||
                            unavailableResponse.Decision == BarcodeRecoveryDecision.Cancelled)
                        {
                            ct.ThrowIfCancellationRequested();
                            return await FailBarcodeWithAvoidRecoveryAsync(
                                "OUT-BARCODE-RECOVERY-CANCELLED",
                                "Output Bin barcode Reader 연결 복구 Dialog를 완료하지 못했습니다. " +
                                readerFailure).ConfigureAwait(false);
                        }

                        // Dialog의 X/CLOSE도 Retry로 반환되므로 Reader 연결 확인부터 다시 수행한다.
                        continue;
                    }

                    int feederSafe = VerifyFeederSafeForBarcodeMotion();
                    if (feederSafe != 0)
                        return feederSafe;

                    double stageBaseTarget = Options.Side == BinSide.Ng
                        ? Stage.Recipe.NGStageY.BarcodePosition
                        : Stage.Recipe.GoodStageY.BarcodePosition;
                    string targetReason;
                    if (!ValidateOutputBarcodeStageYTargets(
                        Options.Side,
                        stageBaseTarget,
                        retryCount,
                        retryStepMm,
                        out targetReason))
                    {
                        return Fail(
                            "OUT-BARCODE-STAGE-Y-TEACH",
                            Stage.Name,
                            "Output Bin barcode StageY teaching/retry 범위가 유효하지 않습니다. side=" +
                            Options.Side + ", " + targetReason);
                    }

                    double visionTarget = Stage.Recipe.VisionX.BarcodePosition;
                    int visionMove = await Stage.MoveVisionXToTargetAndVerifyAsync(
                        visionTarget,
                        ResolveTimeout(),
                        Options.FineMove,
                        ct).ConfigureAwait(false);
                    if (visionMove != 0 || !IsOutputStageAxisReadyAt(BinStageAxis.VisionX, visionTarget))
                    {
                        return await FailBarcodeWithAvoidRecoveryAsync(
                            "OUT-BARCODE-VISION-X",
                            "OutputCameraX barcode 위치 이동/확인 실패. result=" + visionMove + ", " +
                            Stage.BuildStageAxisState(BinStageAxis.VisionX, visionTarget)).ConfigureAwait(false);
                    }

                    string barcode = string.Empty;
                    int scanCount = retryCount + 1;
                    for (int attemptIndex = 0; attemptIndex < scanCount; attemptIndex++)
                    {
                        ct.ThrowIfCancellationRequested();

                        double offset = ResolveBarcodeRetryOffset(attemptIndex, retryStepMm);
                        double stageTarget = stageBaseTarget + offset;
                        int stageMove = await Stage.MoveStageYToBarcodeAndVerifyAsync(
                            Options.Side,
                            stageTarget,
                            ResolveTimeout(),
                            Options.FineMove,
                            ct).ConfigureAwait(false);
                        if (stageMove != 0)
                        {
                            BinStageAxis yAxis = Options.Side == BinSide.Ng
                                ? BinStageAxis.NgBinY
                                : BinStageAxis.GoodBinY;
                            return await FailBarcodeWithAvoidRecoveryAsync(
                                "OUT-BARCODE-STAGE-Y",
                                "Output Bin barcode StageY 이동/확인 실패. side=" + Options.Side +
                                ", attempt=" + (attemptIndex + 1) +
                                ", offset=" + offset.ToString("F3") +
                                ", result=" + stageMove + ", " +
                                Stage.BuildStageAxisState(yAxis, stageTarget)).ConfigureAwait(false);
                        }

                        totalAttempts++;
                        try
                        {
                            string readValue = await reader.ReadAsync(readTimeoutMs).ConfigureAwait(false);
                            barcode = NormalizeBarcode(readValue);
                            if (IsUsableBarcode(barcode))
                                break;

                            lastFailure = "바코드가 검출되지 않았습니다. reader=" + reader.ReaderName +
                                ", attempt=" + totalAttempts +
                                ", stageYOffset=" + offset.ToString("F3");
                        }
                        catch (Exception ex)
                        {
                            lastFailure = "바코드 통신/판독 예외. reader=" + reader.ReaderName +
                                ", attempt=" + totalAttempts +
                                ", error=" + ex.Message;
                        }
                    }

                    if (IsUsableBarcode(barcode))
                    {
                        return await ApplyBarcodeAndFinishAsync(
                            wafer,
                            barcode,
                            totalAttempts,
                            "Reader",
                            ct).ConfigureAwait(false);
                    }

                    int promptAvoid = await EnsureOutputVisionAvoidAfterBarcodeAsync(
                        "barcode read failed before operator recovery",
                        ct).ConfigureAwait(false);
                    if (promptAvoid != 0)
                        return promptAvoid;

                    if (string.IsNullOrWhiteSpace(lastFailure))
                        lastFailure = "설정된 재시도 횟수 안에 Output Bin 바코드를 읽지 못했습니다.";

                    BarcodeRecoveryResponse response = await RequestBarcodeRecoveryAsync(
                        wafer,
                        lastFailure,
                        retryCount,
                        retryStepMm,
                        ct).ConfigureAwait(false);
                    ApplyRecoveryRetryParameters(response, ref retryCount, ref retryStepMm);

                    if (response != null && response.Decision == BarcodeRecoveryDecision.ManualApply)
                    {
                        return await ApplyBarcodeAndFinishAsync(
                            wafer,
                            response.ManualBarcode,
                            totalAttempts,
                            "Manual",
                            ct).ConfigureAwait(false);
                    }

                    if (response == null || response.Decision == BarcodeRecoveryDecision.Cancelled)
                    {
                        ct.ThrowIfCancellationRequested();

                        // CYCLE STOP으로 복구 Dialog가 닫힌 경우는 고장이 아니라 협조 정지다.
                        // Dialog 진입 전 이미 VisionX Avoid를 보장했으므로 안전 위치를 재확인한 뒤
                        // 경계 정지 경로로 합류시킨다. 복귀에 실패하면 그때는 복구 필요 상태로 알린다.
                        if (Context != null && Context.IsCycleStopRequested)
                        {
                            string cycleStopAvoidFailure = await TryRestoreOutputVisionAvoidBestEffortAsync(
                                "barcode cycle stop").ConfigureAwait(false);
                            if (!string.IsNullOrWhiteSpace(cycleStopAvoidFailure))
                            {
                                return Fail(
                                    "OUT-BARCODE-CANCEL-RECOVERY",
                                    Name,
                                    "Output Bin barcode CYCLE STOP 후 OutputCameraX Avoid 복귀에 실패했습니다. " +
                                    "RecoveryRequired. " + cycleStopAvoidFailure);
                            }

                            Context.StopIfCycleStopRequested(
                                "OutputFeederLoadToStageSequence.BarcodeRecoveryPrompt");
                        }

                        return await FailBarcodeWithAvoidRecoveryAsync(
                            "OUT-BARCODE-RECOVERY-CANCELLED",
                            "Output Bin barcode 판독 복구 Dialog를 완료하지 못했습니다. " +
                            lastFailure).ConfigureAwait(false);
                    }

                    // Retry는 base 위치부터 다시 판독한다.
                }
            }
            catch (OperationCanceledException)
            {
                string avoidFailure = await TryRestoreOutputVisionAvoidBestEffortAsync(
                    "barcode cancellation").ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(avoidFailure))
                {
                    return Fail(
                        "OUT-BARCODE-CANCEL-RECOVERY",
                        Name,
                        "Output Bin barcode 취소 후 OutputCameraX Avoid 복귀에 실패했습니다. " +
                        "RecoveryRequired. " + avoidFailure);
                }
                throw;
            }
            catch (SequenceStopException)
            {
                // CYCLE STOP 정지는 제어 흐름이므로 고장으로 바꾸지 않고 상위 정지 처리로 전달한다.
                throw;
            }
            catch (Exception ex)
            {
                return await FailBarcodeWithAvoidRecoveryAsync(
                    "OUT-BARCODE-EX",
                    "Output Bin barcode sequence exception. side=" + Options.Side +
                    ", error=" + ex.Message).ConfigureAwait(false);
            }
            finally
            {
            }
        }

        private OutputFeederLoadToStageStep ResolveAfterBarcodeStep()
        {
            return Options.Side == BinSide.Ng
                ? OutputFeederLoadToStageStep.MoveNgStageAvoidAfterLoad
                : OutputFeederLoadToStageStep.LowerOutputStageGuideBeforeProcess;
        }

        private int VerifyFeederSafeForBarcodeMotion()
        {
            if (Feeder == null || Feeder.FeederY == null ||
                Feeder.FeederY.IsMoving ||
                !Feeder.IsBinFeederAvoidPositionCheck() ||
                !Feeder.IsFeederDown())
            {
                return Fail("OUT-BARCODE-FEEDER-SAFE", Feeder != null ? Feeder.Name : "OutputFeeder",
                    "OutputCameraX/StageY barcode 이동 전 OutputFeeder가 정지된 Avoid/Lift Down 상태가 아닙니다. side=" +
                    Options.Side + ", " + (Feeder != null ? Feeder.DescribeFeederCylinderState() : "OutputFeeder=null"));
            }

            return 0;
        }

        private bool IsOutputStageAxisReadyAt(BinStageAxis axis, double target)
        {
            if (Stage == null || !Stage.HasStageAxis(axis))
                return false;

            QMC.Common.Motion.BaseAxis item = axis == BinStageAxis.VisionX
                ? Stage.OutputCameraX
                : (axis == BinStageAxis.NgBinY ? Stage.NgStage.StageY : Stage.GoodStage.StageY);
            if (item == null)
                return false;

            item.UpdateStatus();

            // ============================================================================
            // [INP 요구 제거 2026-08-05] OUT-BARCODE-VISION-AVOID 결정적 교착 수정.
            //
            // 기존 조건: ... && item.IsInPosition && Stage.IsStageAxisAtPosition(axis, target)
            //
            // IsInPosition 의 의미가 시뮬과 실보드에서 다르다.
            //   · 실보드(AjinAxis)  : AXM.GetInPositionValue / uMechSig 0x20 → 드라이브 INP 하드웨어 신호.
            //                        위치 편차가 INP 윈도우 안이면 상시 true. 이동 이력과 무관하다.
            //   · 노트북 시뮬(BaseAxis): WaitUntilMoveDone 완료 시에만 true 가 되고
            //                        Stop() 에서 false 로 지워진다. 초기값 false.
            //                        ★한 번도 이동 명령을 안 받은 축은 위치가 맞아도 영구 false★
            //
            // 그래서 시뮬에서 아래 교착이 100% 재현됐다(2026-08-05 22:06:30 실측):
            //   1) 바코드 리더(COM6) 부재 → 수동 입력 → OutputVisionX 이동이 발생하지 않음 → INP=false
            //   2) 이 판정이 INP 때문에 false → MoveVisionXToAvoidAndVerifyAsync 호출
            //   3) MoveStageAxis 는 IsAxisAtTarget(위치만 비교)로 이미 목표라 판단해 이동 생략, return 0
            //   4) 재판정 → INP 여전히 false → 알람. RUN 을 다시 눌러도 동일하게 실패.
            //   → 이동을 생략하는 기준(위치)과 도달을 인정하는 기준(INP)이 어긋난 것이 근본 원인이다.
            //
            // 현재 기준: 도달 판정은 위치 기준으로 통일한다.
            //   IsAtTargetPosition 이 이미 !IsMoving && !IsAlarm && IsServoOn 과
            //   ActualPosition/CommandPosition 양쪽 톨러런스를 모두 확인하므로 안전 강도는 유지된다.
            //   실보드에서는 정지·정착 상태면 INP 도 true 이므로 동작 변화가 없다.
            //
            // INP 는 버리지 않고 관측만 한다 — 실보드에서 위치는 맞는데 INP 가 false 라면
            // 드라이브 INP 설정(InPositionEnable/Level)이 빠졌다는 신호이므로 로그로 남긴다.
            // ============================================================================
            bool arrived = item.IsServoOn && !item.IsAlarm && !item.IsMoving &&
                Stage.IsStageAxisAtPosition(axis, target);

            if (arrived && !item.IsInPosition)
                LogAxisArrivedWithoutInPosition(item, axis.ToString(), target);

            return arrived;
        }

        /// <summary>
        /// [INP 요구 제거 2026-08-05] 위치는 도달했으나 INP 신호가 false 인 경우를 기록한다.
        /// 시뮬에서는 정상(이동 이력 없음)이고, 실보드에서 반복되면 드라이브 INP 설정 확인이 필요하다.
        /// 축마다 1회만 남겨 로그가 밀리지 않게 한다.
        /// </summary>
        private static readonly HashSet<string> InPositionMissingLogged =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal static void LogAxisArrivedWithoutInPosition(BaseAxis item, string axisLabel, double target)
        {
            try
            {
                if (item == null)
                    return;

                bool simulated = item.Config != null && item.Config.IsSimulationMode;
                string key = (item.Name ?? axisLabel) + "|" + (simulated ? "sim" : "real");
                lock (InPositionMissingLogged)
                {
                    if (!InPositionMissingLogged.Add(key))
                        return;
                }

                QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "AXIS-INP-MISSING",
                    "축이 목표 위치에 도달했지만 INP 신호가 false 입니다(위치 기준으로 도달 인정). axis=" + axisLabel +
                    ", name=" + item.Name +
                    ", actual=" + item.ActualPosition +
                    ", command=" + item.CommandPosition +
                    ", target=" + target +
                    ", simulation=" + simulated +
                    (simulated
                        ? ". 시뮬은 이동 이력이 없으면 INP 가 false 라 정상입니다."
                        : ". ★실보드입니다 — 드라이브 INP 설정(InPositionEnable/Level)을 확인하세요.★") +
                    " - Check");
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async Task<int> ApplyBarcodeAndFinishAsync(
            WaferMaterial expectedWafer,
            string barcode,
            int attempts,
            string sourceKind,
            CancellationToken ct)
        {
            string normalized = NormalizeBarcode(barcode);
            if (!IsUsableBarcode(normalized))
                return Fail("OUT-BARCODE-MANUAL-INVALID", "Barcode",
                    "입력된 Output Bin 바코드가 비어 있거나 유효하지 않습니다.");

            MaterialLocationKind expectedLocation = Options.Side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;
            string previousWaferId;
            string applyReason;
            bool applied = MaterialStateService.TryApplyWaferBarcode(
                expectedWafer.WaferInstanceId,
                expectedLocation,
                normalized,
                Name + ":" + Options.Side + ":" + sourceKind,
                attempts,
                out previousWaferId,
                out applyReason);
            if (!applied)
            {
                return await FailBarcodeWithAvoidRecoveryAsync(
                    "OUT-BARCODE-MATERIAL-APPLY",
                    "Output Bin barcode Material 갱신 실패. side=" + Options.Side +
                    ", waferInstanceId=" + expectedWafer.WaferInstanceId +
                    ", barcode=" + normalized +
                    ", reason=" + applyReason).ConfigureAwait(false);
            }

            WaferMaterial refreshed = ResolveStageWafer();
            if (refreshed == null ||
                !string.Equals(refreshed.WaferInstanceId, expectedWafer.WaferInstanceId, StringComparison.OrdinalIgnoreCase) ||
                !refreshed.BarcodeConfirmed ||
                !string.Equals(refreshed.WaferId, normalized, StringComparison.Ordinal))
            {
                return await FailBarcodeWithAvoidRecoveryAsync(
                    "OUT-BARCODE-MATERIAL-CHECK",
                    "Output Bin barcode Material 최종 확인 실패. side=" + Options.Side +
                    ", expectedInstanceId=" + expectedWafer.WaferInstanceId +
                    ", expectedBarcode=" + normalized +
                    ", actualWafer=" + (refreshed != null ? refreshed.WaferId : "null")).ConfigureAwait(false);
            }

            Options.ExpectedWaferId = refreshed.WaferId ?? string.Empty;
            int avoidResult = await EnsureOutputVisionAvoidAfterBarcodeAsync(
                "barcode apply completed",
                ct).ConfigureAwait(false);
            if (avoidResult != 0)
                return avoidResult;

            WriteLog(Name,
                "Output Bin barcode applied. side=" + Options.Side +
                ", waferInstanceId=" + refreshed.WaferInstanceId +
                ", previousWaferId=" + previousWaferId +
                ", barcode=" + refreshed.WaferId +
                ", attempts=" + attempts +
                ", source=" + sourceKind + " - Ok");
            CurrentStep = ResolveAfterBarcodeStep();
            return 0;
        }

        private async Task<int> EnsureOutputVisionAvoidAfterBarcodeAsync(string context, CancellationToken ct)
        {
            if (Stage == null || Stage.OutputCameraX == null || Stage.Recipe == null)
                return Fail("OUT-BARCODE-VISION-AVOID-MISSING", "OutputStage",
                    "Output Bin barcode 후 OutputCameraX Avoid 확인에 필요한 축/레시피가 없습니다. context=" + context);

            Stage.Recipe.EnsurePositionObjects();
            double avoidTarget = Stage.Recipe.VisionX.AvoidPosition;
            if (IsOutputStageAxisReadyAt(BinStageAxis.VisionX, avoidTarget))
                return 0;

            int result = await Stage.MoveVisionXToAvoidAndVerifyAsync(
                ResolveTimeout(),
                Options.FineMove,
                ct).ConfigureAwait(false);
            if (result != 0 || !IsOutputStageAxisReadyAt(BinStageAxis.VisionX, avoidTarget))
            {
                return Fail("OUT-BARCODE-VISION-AVOID", "OutputStage",
                    "Output Bin barcode 후 OutputCameraX Avoid 복귀/확인 실패. context=" + context +
                    ", result=" + result + ", " +
                    Stage.BuildStageAxisState(BinStageAxis.VisionX, avoidTarget));
            }

            return 0;
        }

        private async Task<int> FailBarcodeWithAvoidRecoveryAsync(string alarmCode, string message)
        {
            string avoidFailure = await TryRestoreOutputVisionAvoidBestEffortAsync(message).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(avoidFailure))
                message += " | RecoveryRequired: OutputCameraX Avoid 복귀 실패. " + avoidFailure;
            return Fail(alarmCode, Name, message);
        }

        private async Task<string> TryRestoreOutputVisionAvoidBestEffortAsync(string context)
        {
            try
            {
                if (Stage == null || Stage.OutputCameraX == null || Stage.Recipe == null)
                    return "OutputStage/OutputCameraX/Recipe unavailable.";

                Stage.Recipe.EnsurePositionObjects();
                double target = Stage.Recipe.VisionX.AvoidPosition;
                if (IsOutputStageAxisReadyAt(BinStageAxis.VisionX, target))
                    return string.Empty;

                int cleanupTimeoutMs = Math.Max(1000, Math.Min(ResolveTimeout(), 30000));
                using (var cleanupCts = new CancellationTokenSource())
                {
                    cleanupCts.CancelAfter(cleanupTimeoutMs);
                    int result = await Stage.MoveVisionXToAvoidAndVerifyAsync(
                        cleanupTimeoutMs,
                        Options != null && Options.FineMove,
                        cleanupCts.Token).ConfigureAwait(false);
                    if (result == 0 && IsOutputStageAxisReadyAt(BinStageAxis.VisionX, target))
                        return string.Empty;

                    return "context=" + context + ", result=" + result + ", " +
                        Stage.BuildStageAxisState(BinStageAxis.VisionX, target);
                }
            }
            catch (Exception ex)
            {
                return "context=" + context + ", error=" + ex.Message;
            }
            finally
            {
            }
        }

        private async Task<BarcodeRecoveryResponse> RequestBarcodeRecoveryAsync(
            WaferMaterial wafer,
            string failureMessage,
            int retryCount,
            double retryStepMm,
            CancellationToken ct)
        {
            var request = new BarcodeRecoveryRequest
            {
                Channel = BarcodeReaderChannel.OutputBin,
                MaterialId = wafer != null ? wafer.WaferId : string.Empty,
                MaterialInstanceId = wafer != null ? wafer.WaferInstanceId : string.Empty,
                FailureMessage = failureMessage ?? string.Empty,
                RetryCount = retryCount,
                RetryStepMm = retryStepMm
            };
            // 복구 Dialog는 작업자 응답이 없으면 무기한 대기한다. 경계 폴링으로는 깨울 수 없으므로
            // CYCLE STOP 토큰을 함께 관찰시켜 정지 요청 시 Cancelled로 즉시 닫히게 한다.
            using (CancellationTokenSource stoppable = Context.CreateCycleStopLinkedSource(ct))
            {
                return await BarcodeOperatorPromptService
                    .RequestAsync(request, stoppable.Token)
                    .ConfigureAwait(false);
            }
        }

        private static void ApplyRecoveryRetryParameters(
            BarcodeRecoveryResponse response,
            ref int retryCount,
            ref double retryStepMm)
        {
            if (response == null)
                return;

            retryCount = Math.Max(0, response.RetryCount);
            retryStepMm = Math.Abs(response.RetryStepMm);
        }

        private static bool TryEnsureBarcodeReaderReady(IBarcodeReader reader, out string reason)
        {
            reason = string.Empty;
            if (reader == null)
            {
                reason = "Output Bin barcode reader가 구성되지 않았습니다.";
                return false;
            }

            try
            {
                if (reader.IsConnected)
                    return true;

                if (reader.TryOpen() && reader.IsConnected)
                    return true;

                reason = "Output Bin barcode reader 연결에 실패했습니다. reader=" + reader.ReaderName;
                return false;
            }
            catch (Exception ex)
            {
                reason = "Output Bin barcode reader 연결 예외. reader=" + reader.ReaderName +
                    ", error=" + ex.Message;
                return false;
            }
        }

        private static double ResolveBarcodeRetryOffset(int attemptIndex, double retryStepMm)
        {
            if (attemptIndex <= 0 || retryStepMm <= 0.0)
                return 0.0;

            int distanceMultiplier = (attemptIndex + 1) / 2;
            double sign = attemptIndex % 2 == 1 ? 1.0 : -1.0;
            return sign * distanceMultiplier * retryStepMm;
        }

        private bool ValidateOutputBarcodeStageYTargets(
            BinSide side,
            double baseTarget,
            int retryCount,
            double retryStepMm,
            out string reason)
        {
            reason = string.Empty;
            QMC.Common.Motion.BaseAxis stageY = side == BinSide.Ng
                ? (Stage != null && Stage.NgStage != null ? Stage.NgStage.StageY : null)
                : (Stage != null && Stage.GoodStage != null ? Stage.GoodStage.StageY : null);
            if (stageY == null || stageY.Setup == null)
            {
                reason = "StageY axis/setup is unavailable.";
                return false;
            }

            int scanCount = Math.Max(0, retryCount) + 1;
            for (int attemptIndex = 0; attemptIndex < scanCount; attemptIndex++)
            {
                double offset = ResolveBarcodeRetryOffset(attemptIndex, Math.Abs(retryStepMm));
                double target = baseTarget + offset;
                if (stageY.Setup.SoftLimitEnabled &&
                    (target < stageY.Setup.SoftLimitMinus || target > stageY.Setup.SoftLimitPlus))
                {
                    reason = "attempt=" + (attemptIndex + 1) +
                        ", target=" + target.ToString("F3") +
                        ", softLimitMinus=" + stageY.Setup.SoftLimitMinus.ToString("F3") +
                        ", softLimitPlus=" + stageY.Setup.SoftLimitPlus.ToString("F3");
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeBarcode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var chars = new System.Collections.Generic.List<char>(value.Length);
            foreach (char ch in value)
            {
                if (ch == '\x02' || ch == '\x03' || char.IsWhiteSpace(ch))
                    continue;
                if (char.IsControl(ch))
                    return string.Empty;
                chars.Add(ch);
            }
            return new string(chars.ToArray());
        }

        // [검토수정 2026-08-22] 아웃풋 재시작 게이트(OutputSequence)가 인풋 규칙을 차용하지 않도록 공개.
        internal static bool IsUsableBarcode(string value)
        {
            string normalized = NormalizeBarcode(value);
            return !string.IsNullOrWhiteSpace(normalized) &&
                !string.Equals(normalized, "WAFER-NULL-ID", StringComparison.OrdinalIgnoreCase);
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

        private async Task<int> MoveMaterialDataToStageAsync(
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

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

            var stageRingSensor =
                Options.Side == BinSide.Ng
                    ? Stage.NgBinRingSensor
                    : Stage.GoodBinRingSensor;
            bool controllerGlobalDryRun =
                Context != null &&
                Context.Controller != null &&
                Context.Controller.GlobalDryRun;
            bool virtualTransferReady =
                wafer != null && ResolveStageWafer() == null;
            bool ringConfirmed = await Feeder.WaitTransportRingStatesConfirmedAsync(
                false,
                stageRingSensor,
                true,
                controllerGlobalDryRun,
                !virtualTransferReady,
                virtualTransferReady,
                ResolveTimeout(),
                ct).ConfigureAwait(false);
            if (!ringConfirmed)
            {
                return Fail(
                    "OUT-STAGE-MATERIAL-RING",
                    Feeder.Name,
                    "OutputFeeder→Stage Material 이동 직전 Feeder Ring OFF + " +
                    Options.Side +
                    " Stage Ring ON 안정 확인에 실패했습니다. wafer=" +
                    wafer.WaferId + ", detail=" +
                    Feeder.LastTransportRingConfirmationFailure);
            }

            wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Ring 확인 후 Output feeder wafer data가 사라졌습니다.");
            if (ResolveStageWafer() != null)
                return Fail("OUT-STAGE-DATA-OCCUPIED", "Material", "Ring 확인 후 Output stage data가 점유 상태로 변경되었습니다. side=" + Options.Side);

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("OUT-STAGE-MATERIAL-WAFER", "Material", "Ring 확인 후 Bin ID가 변경되었습니다. expected=" + Options.ExpectedWaferId + ", actual=" + wafer.WaferId);

            sourceRole = wafer.SourceCassetteRole;
            if ((Options.Side == BinSide.Ng && sourceRole != CassetteMaterialRole.Ng1) ||
                (Options.Side == BinSide.Good && sourceRole != CassetteMaterialRole.Good1 && sourceRole != CassetteMaterialRole.Good2))
                return Fail("OUT-STAGE-MATERIAL-SIDE", "Material", "Ring 확인 후 Output side와 source cassette role이 일치하지 않습니다. wafer=" + wafer.WaferId + ", side=" + Options.Side + ", sourceRole=" + sourceRole);

            MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = ResolveOutputStageLocation() }, WaferMaterialState.Working);

            // [사용자 승인 2026-08-17] 종전에는 반환값을 버렸다. 계획 생성이 실패하면 OutputReceiveSlots가
            // 0으로 남고 GOOD 배출 픽업 캡 allowance=0이 되어 Front/Rear 픽커가 알람 없이 무한 대기한다
            // (실측 13분 무언정지, 원인은 GoodBin 맵 FINAL APPLY 누락). 여기서 즉시 알람으로 세운다.
            // 복구는 Map Create에서 해당 역할 FINAL APPLY 후 재가동하면 OutputSequence의
            // EnsureOutputStageReadyForPlace가 계획을 재초기화한다.
            string receivePlanReason;
            if (!MaterialStateService.InitializeOutputStageReceivePlan(Options.Side, out receivePlanReason))
            {
                return Fail(
                    "OUT-STAGE-RECEIVE-PLAN",
                    "Material",
                    "Bin 로딩 후 배출 수령 계획을 생성하지 못했습니다. 이 상태로 가동하면 픽업 캡이 0이 되어 " +
                    "픽커가 알람 없이 대기만 합니다. side=" + Options.Side +
                    ", wafer=" + wafer.WaferId +
                    ", reason=" + receivePlanReason);
            }

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

            // [배포 전 최종점검 2026-08-23] 복구 모드도 OutputCassette Avoid 이동을 반드시 수행한다.
            // 한때 "카세트 무접촉" 원칙으로 생략했다가 인풋 측과 같은 확정 결함으로 되돌렸다:
            // 복구의 주 시나리오(원 로딩이 바코드 스텝에서 알람)는 리프터가 슬롯 높이에 남은 상태이고,
            // 픽커 X는 OutputLifterZ가 정지된 Avoid/Home이 아니면 무조건 차단된다
            // (PickerFront/RearInterlockRules) — 생략하면 Bin 준비 완료 후 픽커가 알람 정지한다.
            // 동시성 안전은 이 이동 자체의 인터락이 보장한다: 리프터 이동은 양픽커 X Avoid + 피더
            // 카세트 안전 + Bin 돌출 미감지 선확인(OutputCassetteInterlockRules), 픽커 X는 리프터
            // IsMoving/비Avoid 시 차단 — 양방향 하드 인터락이라 어느 쪽이 먼저든 fail-closed다.
            CurrentStep = OutputFeederLoadToStageStep.MoveOutputCassetteAvoidPosition;
            return 0;
        }
    }
}

