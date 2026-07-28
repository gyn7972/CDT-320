using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederUnloadFromStageStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckOutputStageBinData,
        CheckFeederEmpty,
        EnsureOutputVisionAvoid,
        EnsurePickerAvoidPosition,
        EnsureStageMutualInterlock,
        MoveOutputStageUnloadPosition,
        EnsureOutputStageGuideUp,
        EnsureOutputStageUnclamp,
        EnsureOutputStageClampLiftDown,
        VerifyOutputStageUnloadReady,
        VerifyFeederReadyAtAvoid,
        PrepareFeederUnclamp,
        PrepareFeederLiftUp,
        MoveFeederStageUnloadAvoidPosition,
        PrepareFeederLiftDown,
        MoveFeederStageUnloadPosition,
        ClampFeederBin,
        UnclampOutputStageBin,
        LowerOutputStageClamp,
        VerifyBinDetected,
        MoveMaterialDataToFeeder,
        UpdateStageData,
        Complete,
        Error
    }

    internal sealed class OutputFeederUnloadFromStageSequence : OutputFeederSequenceBase<OutputFeederUnloadFromStageStep>
    {
        private bool _stageToFeederRingProofCompleted;

        public OutputFeederUnloadFromStageSequence(MachineSequenceContext context)
            : base(context, OutputFeederSequenceKind.UnloadFromStage, "OutputFeederUnloadFromStageSequence")
        {
        }

        protected override OutputFeederUnloadFromStageStep IdleStep { get { return OutputFeederUnloadFromStageStep.Idle; } }
        protected override OutputFeederUnloadFromStageStep InitialStep { get { return OutputFeederUnloadFromStageStep.CheckUnit; } }
        protected override OutputFeederUnloadFromStageStep CompleteStep { get { return OutputFeederUnloadFromStageStep.Complete; } }
        protected override OutputFeederUnloadFromStageStep ErrorStep { get { return OutputFeederUnloadFromStageStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputFeederUnloadFromStageStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederUnloadFromStageStep.CheckTransferReady));

                    // 이송 준비 확인
                    case OutputFeederUnloadFromStageStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());

                    // 아웃풋 스테이지 BIN 데이터 확인
                    case OutputFeederUnloadFromStageStep.CheckOutputStageBinData:
                        return Task.FromResult(CheckOutputStageBinData());

                    // 피더 비어있음 확인
                    case OutputFeederUnloadFromStageStep.CheckFeederEmpty:
                        return Task.FromResult(CheckFeederEmpty());

                    // 아웃풋 비전 어보이드 확보
                    case OutputFeederUnloadFromStageStep.EnsureOutputVisionAvoid:
                        return EnsureOutputVisionAvoidAsync(ct);

                    // 피커 어보이드 위치 확보
                    case OutputFeederUnloadFromStageStep.EnsurePickerAvoidPosition:
                        return EnsurePickerAvoidPositionAsync(ct);

                    // 스테이지 상호 인터락 확보
                    case OutputFeederUnloadFromStageStep.EnsureStageMutualInterlock:
                        return EnsureStageMutualInterlockAsync(ct);

                    // 아웃풋 스테이지 언로드 위치 이동
                    case OutputFeederUnloadFromStageStep.MoveOutputStageUnloadPosition:
                        return MoveOutputStageUnloadPositionAsync(ct);

                    // 아웃풋 스테이지 가이드 업 확보
                    case OutputFeederUnloadFromStageStep.EnsureOutputStageGuideUp:
                        return EnsureOutputStageGuideUpAsync(ct);

                    // 아웃풋 스테이지 언클램프 확보
                    case OutputFeederUnloadFromStageStep.EnsureOutputStageUnclamp:
                        return EnsureOutputStageUnclampAsync(ct);

                    // 아웃풋 스테이지 클램프 리프트 다운 확보
                    case OutputFeederUnloadFromStageStep.EnsureOutputStageClampLiftDown:
                        return EnsureOutputStageClampLiftDownAsync(ct);

                    // 아웃풋 스테이지 언로드 준비 검증
                    case OutputFeederUnloadFromStageStep.VerifyOutputStageUnloadReady:
                        return Task.FromResult(VerifyOutputStageUnloadReady());

                    // 피더 어보이드 준비 검증
                    case OutputFeederUnloadFromStageStep.VerifyFeederReadyAtAvoid:
                        return Task.FromResult(VerifyFeederReadyAtAvoid());

                    // 피더 언클램프 준비
                    case OutputFeederUnloadFromStageStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);

                    // 피더 리프트 업 준비
                    case OutputFeederUnloadFromStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);

                    // 피더 스테이지 언로드 어보이드 위치 이동
                    case OutputFeederUnloadFromStageStep.MoveFeederStageUnloadAvoidPosition:
                        return MoveFeederStageUnloadAvoidPositionAsync(ct);

                    // 피더 리프트 다운 준비
                    case OutputFeederUnloadFromStageStep.PrepareFeederLiftDown:
                        return PrepareFeederLiftDownAsync(ct);

                    // 피더 스테이지 언로드 위치 이동
                    case OutputFeederUnloadFromStageStep.MoveFeederStageUnloadPosition:
                        return MoveFeederStageUnloadPositionAsync(ct);

                    // 피더 BIN 클램프
                    case OutputFeederUnloadFromStageStep.ClampFeederBin:
                        return ClampFeederBinAsync(ct);

                    // 아웃풋 스테이지 BIN 언클램프
                    case OutputFeederUnloadFromStageStep.UnclampOutputStageBin:
                        return UnclampOutputStageBinAsync(ct);

                    // 아웃풋 스테이지 클램프 리프트 다운
                    case OutputFeederUnloadFromStageStep.LowerOutputStageClamp:
                        return LowerOutputStageClampAsync(ct);

                    // BIN 감지 검증
                    case OutputFeederUnloadFromStageStep.VerifyBinDetected:
                        return VerifyBinDetectedAsync(ct);

                    // 자재 데이터를 피더로 이동
                    case OutputFeederUnloadFromStageStep.MoveMaterialDataToFeeder:
                        return MoveMaterialDataToFeederAsync(ct);

                    // 스테이지 데이터 갱신
                    case OutputFeederUnloadFromStageStep.UpdateStageData:
                        return Task.FromResult(UpdateStageData());

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-STAGE-UNLOAD-EX", Name, "Unload from stage step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            string readyReason;
            if (!Feeder.CheckFeederStageReady(Options.Side, TransferMode.Unload, out readyReason))
                return Fail("OUT-FEEDER-STAGE-UNLOAD-READY", Feeder.Name, "Output feeder stage unload is not ready. " + readyReason);

            CurrentStep = OutputFeederUnloadFromStageStep.CheckOutputStageBinData;
            return 0;
        }

        private int CheckOutputStageBinData()
        {
            return CheckStageReadyForFeederUnload(OutputFeederUnloadFromStageStep.CheckFeederEmpty);
        }

        private int CheckFeederEmpty()
        {
            if (ResolveFeederWafer() != null)
            {
                WaferMaterial feederWafer = ResolveFeederWafer();
                return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material",
                    "Output feeder data became occupied before stage unload. side=" + Options.Side +
                    ", waferId=" + feederWafer.WaferId + ", state=" + feederWafer.State +
                    ", loc=" + feederWafer.CurrentLocation);
            }

            if (!IsHardwareBypass() && !Feeder.IsFeederEmpty())
                return Fail("OUT-FEEDER-SENSOR-OCCUPIED", Feeder.Name,
                    "Output feeder sensor must be empty before stage unload. sensorOccupied=" + Feeder.IsFeederOccupied() +
                    ", sensorEmpty=" + Feeder.IsFeederEmpty());

            CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputVisionAvoid;
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
                    return Fail("OUT-STAGE-VISION-AVOID", Stage.Name, "OutputVisionX avoid move failed before stage unload. side=" + Options.Side + ", result=" + result);
            }

            if (!Stage.IsVisionXInAvoidPosition())
                return Fail("OUT-STAGE-VISION-AVOID", Stage.Name, "OutputVisionX is not in avoid position before stage unload. side=" + Options.Side);

            CurrentStep = OutputFeederUnloadFromStageStep.EnsurePickerAvoidPosition;
            return 0;
        }

        private async Task<int> EnsurePickerAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            int pickerClear = await WaitPickersClearForOutputTransportAsync("OutputStage Unload 준비", ct).ConfigureAwait(false);
            if (pickerClear != 0)
                return pickerClear;

            CurrentStep = OutputFeederUnloadFromStageStep.EnsureStageMutualInterlock;
            return 0;
        }

        private async Task<int> EnsureStageMutualInterlockAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // Stage 위치 이동은 OutputStagePrepareUnloadSequence에서 Feeder 접근 전에 끝나야 한다.
            // Stage -> Feeder 이송 단계에서는 준비된 Unload 위치를 유지하고 위치만 검증한다.
            if (!Stage.IsStageInUnloadPosition(Options.Side))
                return Fail("OUT-STAGE-UNLOAD-POS", Stage.Name,
                    "OutputStage가 Feeder 이송 시작 전에 Unload 위치에 준비되지 않았습니다. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            // 기존 조건 / 현재 기준은 OutputFeederLoadToStageSequence.EnsureStageMutualInterlockAsync와 동일하다.
            //   - NG Clamp Lift Up은 "Stage 축이 실제로 움직일 때"만 필요하며 축 이동 관문에서 강제·검증된다.
            //   - 이 구간은 Stage 축을 움직이지 않고 도착 상태만 검증하며, 곧바로 대상 side를
            //     Clamp Lift Down으로 만들어 피더 이송을 수행한다.
            //   - 따라서 NG side 이송에서는 Up을 강제하지 않는다(불필요한 Up<->Down 왕복 및
            //     카세트 리프터 이동과의 인터락 충돌 방지). GOOD side 이송에서는 기존대로 유지한다.
            if (Options.Side != BinSide.Ng)
            {
                int result = await Stage.EnsureBinGuideClampLiftUpAsync(BinSide.Ng, ResolveTimeout(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name,
                        "NG stage clamp lift up failed before feeder transfer. side=" + Options.Side + ", result=" + result + ", " +
                        Stage.DescribeOutputStageInterlockState(Options.Side));

                if (!Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                    return Fail("OUT-STAGE-NG-CLAMP-UP", Stage.Name, "NG stage clamp lift must be up before stage unload movement. " + Stage.DescribeOutputStageInterlockState(Options.Side));
            }

            if (Options.Side == BinSide.Ng && !Stage.IsGoodStageZInAvoidPosition())
                return Fail("OUT-STAGE-GOOD-Z-AVOID", Stage.Name,
                    "NG Stage Unload 전 GoodStageZ가 Avoid 위치가 아닙니다. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (Options.Side != BinSide.Ng && !Stage.IsNgStageInAvoidPosition())
                return Fail("OUT-STAGE-NG-AVOID", Stage.Name, "NG stage must be avoid before GOOD stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            // 기존 조건: GUIDE UP -> UNCLAMP -> CLAMP LIFT DOWN 순서로 배출 준비를 했다.
            // CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageGuideUp;
            // 현재 기준: 배출 최종 준비 상태는 UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP 순서로 만든다.
            // To do: 스테이지 배출 준비 실린더 순서 통일(UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP).
            CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageUnclamp;
            return 0;
        }

        private Task<int> MoveOutputStageUnloadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!Stage.IsStageInUnloadPosition(Options.Side))
                return Task.FromResult(Fail("OUT-STAGE-UNLOAD-POS", Stage.Name,
                    "OutputStage가 Feeder 이송 시작 전에 Unload 위치에 준비되지 않았습니다. side=" + Options.Side));

            // 기존 조건: GUIDE UP -> UNCLAMP -> CLAMP LIFT DOWN 순서로 배출 준비를 했다.
            // CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageGuideUp;
            // 현재 기준: 배출 최종 준비 상태는 UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP 순서로 만든다.
            // To do: 스테이지 배출 준비 실린더 순서 통일(UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP).
            CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageUnclamp;
            return Task.FromResult(0);
        }

        private async Task<int> EnsureOutputStageGuideUpAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideUpAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-GUIDE-UP", Stage.Name, "Output stage bin guide up failed before stage unload. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUp(Options.Side))
                return Fail("OUT-STAGE-GUIDE-UP", Stage.Name, "Output stage bin guide is not up before stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            // 기존 조건: GUIDE UP 다음 UNCLAMP로 이어졌다.
            // CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageUnclamp;
            // 현재 기준: GUIDE UP이 배출 준비의 마지막 단계 - 바로 배출 준비 검증으로 넘어간다.
            CurrentStep = OutputFeederUnloadFromStageStep.VerifyOutputStageUnloadReady;
            return 0;
        }

        private async Task<int> EnsureOutputStageUnclampAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideUnclampedAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide unclamp failed before stage unload. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide is not unclamped before stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageClampLiftDown;
            return 0;
        }

        private async Task<int> EnsureOutputStageClampLiftDownAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideClampLiftDownAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift down failed before stage unload. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift is not down before stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            // 기존 조건: CLAMP LIFT DOWN 다음 바로 배출 준비 검증으로 넘어갔다.
            // CurrentStep = OutputFeederUnloadFromStageStep.VerifyOutputStageUnloadReady;
            // 현재 기준: CLAMP LIFT DOWN 다음은 GUIDE UP.
            CurrentStep = OutputFeederUnloadFromStageStep.EnsureOutputStageGuideUp;
            return 0;
        }

        private int VerifyOutputStageUnloadReady()
        {
            if (IsHardwareBypass() && ResolveStageWafer() != null)
            {
                CurrentStep = OutputFeederUnloadFromStageStep.VerifyFeederReadyAtAvoid;
                return 0;
            }

            // 기존 조건: GUIDE UP -> CLAMP LIFT DOWN -> UNCLAMP 순서로 확인했다.
            // 현재 기준: 배출 최종 준비 상태 규격(UNCLAMP -> CLAMP LIFT DOWN -> GUIDE UP) 순서로 확인한다.
            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-UNLOAD-UNCLAMP", Stage.Name,
                    "Output stage bin guide must be unclamped before feeder starts stage unload. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-UNLOAD-CLAMP-DOWN", Stage.Name,
                    "Output stage bin clamp lift must be down before feeder starts stage unload. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUp(Options.Side))
                return Fail("OUT-STAGE-UNLOAD-GUIDE-UP", Stage.Name,
                    "Output stage bin guide must be up before feeder starts stage unload. side=" + Options.Side + ", " +
                    Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.VerifyFeederReadyAtAvoid;
            return 0;
        }

        private int VerifyFeederReadyAtAvoid()
        {
            if (!IsFeederReadyForStageUnloadStart())
                return Fail("OUT-FEEDER-AVOID-CHECK", Feeder.Name,
                    "Output feeder must already be at Avoid or StageUnloadAvoid position before stage unload. side=" + Options.Side + ", " +
                    Feeder.DescribeBinFeederYMoveDoneState());

            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "Output feeder must already be down before stage unload. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState());

            CurrentStep = Feeder.IsFeederUnclamped()
                ? OutputFeederUnloadFromStageStep.PrepareFeederLiftUp
                : OutputFeederUnloadFromStageStep.PrepareFeederUnclamp;
            return 0;
        }

        private bool IsFeederReadyForStageUnloadStart()
        {
            return Feeder.IsBinFeederInAvoidPosition() ||
                   Feeder.IsBinFeederYInStageUnloadAvoidPosition(Options.Side);
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            int result = await Feeder.SetFeederClampAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name,
                    "Output feeder unclamp command failed before stage unload. result=" + result + ", " +
                    Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name,
                    "Output feeder unclamp failed before stage unload. result=" + result + ", " +
                    Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadFromStageStep.PrepareFeederLiftUp;
            return 0;
        }

        private async Task<int> PrepareFeederLiftUpAsync(CancellationToken ct)
        {
            int result = await Feeder.SetFeederUpDownAsync(true, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift up before stage unload avoid failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUp())
                return Fail("OUT-FEEDER-UP", Feeder.Name, "Output feeder lift is not up before stage unload avoid. " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadFromStageStep.MoveFeederStageUnloadAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageUnloadAvoidPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageUnloadAvoidPosition(Options.Side, Options.FineMove), "stage unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageUnloadAvoidPosition(Options.Side), "stage unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadFromStageStep.PrepareFeederLiftDown;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct)
        {
            int result = await Feeder.SetFeederUpDownAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift down before stage unload failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-DOWN", Feeder.Name, "Output feeder lift is not down before stage unload. " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadFromStageStep.MoveFeederStageUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageUnloadPositionAsync(CancellationToken ct)
        {
            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederStageUnloadPosition(Options.Side, Options.FineMove), "stage unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInStageUnloadPosition(Options.Side), "stage unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadFromStageStep.ClampFeederBin;
            return 0;
        }

        private async Task<int> ClampFeederBinAsync(CancellationToken ct)
        {
            int result = await Feeder.SetFeederClampAsync(true, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-CLAMP", Feeder.Name, "Output feeder clamp final check failed. side=" + Options.Side + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadFromStageStep.UnclampOutputStageBin;
            return 0;
        }

        // 현재 기준: 배출 준비 단계에서 이미 UNCLAMP가 완료되므로 이 스텝은 피더 클램프 후 상태 재검증(멱등) 역할이다.
        private async Task<int> UnclampOutputStageBinAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideUnclampedAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide unclamp failed after feeder clamped bin. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-UNCLAMP", Stage.Name, "Output stage bin guide unclamp final check failed after feeder clamped bin. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.LowerOutputStageClamp;
            return 0;
        }

        // 현재 기준: 배출 준비 단계에서 이미 CLAMP LIFT DOWN이 완료되므로 이 스텝은 상태 재검증(멱등) 역할이다.
        private async Task<int> LowerOutputStageClampAsync(CancellationToken ct)
        {
            int result = await Stage.EnsureBinGuideClampLiftDownAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift down failed after stage unload. side=" + Options.Side + ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftDown(Options.Side))
                return Fail("OUT-STAGE-CLAMP-DOWN", Stage.Name, "Output stage bin clamp lift down final check failed after stage unload. " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadFromStageStep.VerifyBinDetected;
            return 0;
        }

        private async Task<int> VerifyBinDetectedAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("OUT-STAGE-DATA-MISSING", "Material", "Output stage data disappeared before feeder material move. side=" + Options.Side);

            bool controllerGlobalDryRun =
                Context != null &&
                Context.Controller != null &&
                Context.Controller.GlobalDryRun;
            bool virtualTransferReady =
                wafer != null && ResolveFeederWafer() == null;
            bool detected = await Feeder.WaitTransportRingStatesConfirmedAsync(
                true,
                null,
                null,
                controllerGlobalDryRun,
                virtualTransferReady,
                null,
                ResolveTimeout(),
                ct).ConfigureAwait(false);
            if (!detected)
                return Fail(
                    "OUT-FEEDER-STAGE-UNLOAD-RING",
                    Feeder.Name,
                    "Stage→OutputFeeder 전달 후 Feeder Ring ON 안정 확인에 실패했습니다. waferId=" +
                    wafer.WaferId + ", detail=" +
                    Feeder.LastTransportRingConfirmationFailure);

            _stageToFeederRingProofCompleted = true;
            // Ring 증명과 Material cutover 사이에 Step 경계가 생기면 정지/재개 후
            // 오래된 증명을 재사용할 수 있다. 정상 흐름에서는 같은 호출 안에서 즉시
            // Material을 갱신하고, 구 ResumeStep로 직접 진입한 경우에는 아래 메서드가
            // Ring 상태를 다시 확인한다.
            return await MoveMaterialDataToFeederAsync(ct).ConfigureAwait(false);
        }

        private async Task<int> MoveMaterialDataToFeederAsync(
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            bool ringProofCompleted = _stageToFeederRingProofCompleted;
            _stageToFeederRingProofCompleted = false;

            WaferMaterial wafer = ResolveStageWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Output stage wafer data was not found for feeder material move. side=" + Options.Side);
            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material", "Output feeder data became occupied before stage to feeder material move.");

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("OUT-FEEDER-MATERIAL-WAFER", "Material", "물리 이송 후 Feeder Material 갱신 직전에 Bin ID가 변경되었습니다. expected=" + Options.ExpectedWaferId + ", actual=" + wafer.WaferId);

            CassetteMaterialRole sourceRole = wafer.SourceCassetteRole;
            if ((Options.Side == BinSide.Ng && sourceRole != CassetteMaterialRole.Ng1) ||
                (Options.Side == BinSide.Good && sourceRole != CassetteMaterialRole.Good1 && sourceRole != CassetteMaterialRole.Good2))
                return Fail("OUT-FEEDER-MATERIAL-SIDE", "Material", "Output side와 source cassette role이 일치하지 않습니다. wafer=" + wafer.WaferId + ", side=" + Options.Side + ", sourceRole=" + sourceRole);

            if (!ringProofCompleted)
            {
                bool controllerGlobalDryRun =
                    Context != null &&
                    Context.Controller != null &&
                    Context.Controller.GlobalDryRun;
                bool virtualTransferReady =
                    wafer != null && ResolveFeederWafer() == null;
                bool ringConfirmed =
                    await Feeder.WaitTransportRingStatesConfirmedAsync(
                        true,
                        null,
                        null,
                        controllerGlobalDryRun,
                        virtualTransferReady,
                        null,
                        ResolveTimeout(),
                        ct).ConfigureAwait(false);
                if (!ringConfirmed)
                {
                    return Fail(
                        "OUT-FEEDER-MATERIAL-RING",
                        Feeder.Name,
                        "Stage→OutputFeeder Material 재개 직전 Feeder Ring ON 안정 확인에 실패했습니다. wafer=" +
                        wafer.WaferId + ", detail=" +
                        Feeder.LastTransportRingConfirmationFailure);
                }

                wafer = ResolveStageWafer();
                if (wafer == null)
                    return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Ring 확인 후 Output stage wafer data가 사라졌습니다. side=" + Options.Side);
                if (ResolveFeederWafer() != null)
                    return Fail("OUT-FEEDER-DATA-OCCUPIED", "Material", "Ring 확인 후 Output feeder data가 점유 상태로 변경되었습니다.");
            }

            MaterialStateService.MoveWafer(wafer.WaferId, new MaterialLocation { Kind = MaterialLocationKind.OutputFeeder }, WaferMaterialState.WorkReady);
            Feeder.UpdateFeederMaterialState(MaterialState.Occupied);
            CurrentStep = OutputFeederUnloadFromStageStep.UpdateStageData;
            return 0;
        }

        private int UpdateStageData()
        {
            if (ResolveStageWafer() != null)
                return Fail("OUT-STAGE-DATA-CLEAR", "Material", "Output stage data was not cleared after stage unload. side=" + Options.Side);

            if (ResolveFeederWafer() == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data was not created after stage unload. side=" + Options.Side);

            Context.Bus.Set("OutputStageEmpty");
            Context.Bus.Set("OutputFeederOccupied");
            CurrentStep = OutputFeederUnloadFromStageStep.Complete;
            return 0;
        }
    }
}

