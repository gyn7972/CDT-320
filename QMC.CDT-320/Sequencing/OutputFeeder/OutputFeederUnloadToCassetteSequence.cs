using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common.IO;

namespace QMC.CDT320.Sequencing
{
    internal enum OutputFeederUnloadToCassetteStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckFeederBinData,
        CheckCassetteTargetSlot,
        VerifyFeederClamped,
        VerifyFeederLiftDown,
        VerifyBinDetected,
        MoveCassetteToBinSlot,
        MoveFeederCassetteUnloadPosition,
        PrepareFeederUnclamp,
        MoveFeederAvoidPosition,
        VerifyBinReleasedToCassette,
        VerifyPostUnloadStageRestoreReady,
        LowerTargetStageGuideAfterUnload,
        VerifyTargetStageUnclamped,
        LiftTargetStageClampAfterUnload,
        ClampTargetStageAfterUnload,
        VerifyPostUnloadStageFinalState,
        MoveMaterialDataToCassette,
        UpdateCassetteData,
        // To do: [언로드 오프셋] 피더 이탈 후 리프터를 정확한 슬롯 위치로 복귀시켜 bin을 안착시킨다.
        MoveCassetteToBinSlotFinalPosition,
        MoveOutputCassetteAvoidPosition,
        Complete,
        Error
    }

    internal sealed class OutputFeederUnloadToCassetteSequence : OutputFeederSequenceBase<OutputFeederUnloadToCassetteStep>
    {
        private readonly SequenceResourceLease _outputPlaceAreaLease;
        private readonly SequenceResourceLease _outputStageAreaLease;
        private bool _resumeEntryNormalized;

        public OutputFeederUnloadToCassetteSequence(MachineSequenceContext context)
            : this(context, null, null)
        {
        }

        internal OutputFeederUnloadToCassetteSequence(
            MachineSequenceContext context,
            SequenceResourceLease outputPlaceAreaLease,
            SequenceResourceLease outputStageAreaLease)
            : base(context, OutputFeederSequenceKind.UnloadToCassette, "OutputFeederUnloadToCassetteSequence")
        {
            _outputPlaceAreaLease = outputPlaceAreaLease;
            _outputStageAreaLease = outputStageAreaLease;
        }

        protected override OutputFeederUnloadToCassetteStep IdleStep { get { return OutputFeederUnloadToCassetteStep.Idle; } }
        protected override OutputFeederUnloadToCassetteStep InitialStep { get { return OutputFeederUnloadToCassetteStep.CheckUnit; } }
        protected override OutputFeederUnloadToCassetteStep CompleteStep { get { return OutputFeederUnloadToCassetteStep.Complete; } }
        protected override OutputFeederUnloadToCassetteStep ErrorStep { get { return OutputFeederUnloadToCassetteStep.Error; } }

        private int NormalizeResumeEntryStep()
        {
            WaferMaterial feederWafer = ResolveFeederWafer();
            WaferMaterial cassetteWafer = ResolveCassetteWafer();
            if (feederWafer != null && cassetteWafer != null)
            {
                return Fail("OUT-FEEDER-CST-RESUME-DUPLICATE", "Material",
                    "UnloadToCassette 재개 시 Feeder와 대상 Cassette slot에 Material이 동시에 존재합니다. " +
                    "feeder=" + feederWafer.WaferId + ", cassette=" + cassetteWafer.WaferId +
                    ", side=" + Options.Side + ", slot=" + Options.SlotIndex);
            }

            if (feederWafer != null)
            {
                int feederValidation = ValidateExpectedFeederWafer(feederWafer, "UnloadToCassette 재개 진입");
                if (feederValidation != 0)
                    return feederValidation;

                if (Feeder != null && Feeder.IsFeederUnclamped())
                {
                    WriteLog(Name,
                        "UnloadToCassette 중간 Step 재개를 제품 해제 후 안전 복귀 검증 Step으로 정규화합니다. " +
                        "savedStep=" + CurrentStep + ", wafer=" + feederWafer.WaferId +
                        ", side=" + Options.Side + " - Check");
                    CurrentStep = OutputFeederUnloadToCassetteStep.VerifyPostUnloadStageRestoreReady;
                    return 0;
                }

                WriteLog(Name,
                    "UnloadToCassette 중간 Step 재개를 제품 해제 전 전체 선행 검증 Step으로 정규화합니다. " +
                    "savedStep=" + CurrentStep + ", wafer=" + feederWafer.WaferId +
                    ", side=" + Options.Side + " - Check");
                CurrentStep = OutputFeederUnloadToCassetteStep.CheckTransferReady;
                return 0;
            }

            if (cassetteWafer != null)
            {
                int cassetteValidation = ValidateExpectedCassetteWafer(cassetteWafer, "UnloadToCassette 재개 진입");
                if (cassetteValidation != 0)
                    return cassetteValidation;

                WriteLog(Name,
                    "UnloadToCassette 중간 Step 재개를 이미 완료된 Material commit 검증 Step으로 정규화합니다. " +
                    "savedStep=" + CurrentStep + ", wafer=" + cassetteWafer.WaferId +
                    ", side=" + Options.Side + " - Check");
                CurrentStep = OutputFeederUnloadToCassetteStep.UpdateCassetteData;
                return 0;
            }

            return Fail("OUT-FEEDER-CST-RESUME-MATERIAL-MISSING", "Material",
                "UnloadToCassette 중간 Step 재개 시 Feeder와 대상 Cassette slot에서 Material을 찾을 수 없습니다. " +
                "savedStep=" + CurrentStep + ", side=" + Options.Side +
                ", cassetteRole=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);
        }

        private int ValidateExpectedFeederWafer(WaferMaterial wafer, string context)
        {
            if (wafer == null)
                return Fail("OUT-FEEDER-CST-WAFER-MISSING", "Material", context + " 중 Feeder Material이 없습니다.");

            CassetteMaterialRole targetRole = ResolveOutputCassetteRole();
            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
            {
                return Fail("OUT-FEEDER-CST-WAFER-EXPECTED", "Material",
                    context + " 중 Feeder Material이 실행 시작 시 예상 Bin과 다릅니다. expected=" +
                    Options.ExpectedWaferId + ", actual=" + wafer.WaferId + ", side=" + Options.Side);
            }

            if (wafer.SourceCassetteRole != targetRole || wafer.SourceSlotNumber != Options.SlotIndex)
            {
                return Fail("OUT-FEEDER-CST-WAFER-SOURCE", "Material",
                    context + " 중 Feeder Material의 원본 role/slot이 실행 대상과 다릅니다. wafer=" + wafer.WaferId +
                    ", sourceRole=" + wafer.SourceCassetteRole +
                    ", sourceSlot=" + (wafer.SourceSlotNumber + 1).ToString("00") +
                    ", targetRole=" + targetRole +
                    ", targetSlot=" + (Options.SlotIndex + 1).ToString("00"));
            }

            bool gradeMatches = Options.Side == BinSide.Ng
                ? wafer.OutputGrade == DieResult.NG && targetRole == CassetteMaterialRole.Ng1
                : wafer.OutputGrade == DieResult.Good &&
                  (targetRole == CassetteMaterialRole.Good1 || targetRole == CassetteMaterialRole.Good2);
            if (!gradeMatches)
            {
                return Fail("OUT-FEEDER-CST-WAFER-GRADE", "Material",
                    context + " 중 Feeder Material의 Output grade/side가 실행 대상과 정확히 일치하지 않습니다. wafer=" +
                    wafer.WaferId + ", side=" + Options.Side +
                    ", grade=" + wafer.OutputGrade + ", targetRole=" + targetRole);
            }

            return 0;
        }

        private int VerifyCassetteTransferAlignmentBeforeRelease(
            bool requireFeederAtCassetteUnload,
            bool requireFeederUnclamped,
            bool requireRingDetected,
            string context)
        {
            if (Feeder == null || Feeder.FeederY == null ||
                Cassette == null || Cassette.OutputLifterZ == null ||
                Stage == null)
            {
                return Fail("OUT-FEEDER-CST-ALIGN-UNIT", Name,
                    context + " 필요한 Feeder/Cassette/Stage 정보를 찾을 수 없습니다. side=" + Options.Side);
            }

            WaferMaterial wafer = ResolveFeederWafer();
            int waferValidation = ValidateExpectedFeederWafer(wafer, context);
            if (waferValidation != 0)
                return waferValidation;

            if (ResolveCassetteWafer() != null)
                return Fail("OUT-FEEDER-CST-ALIGN-SLOT", "Material",
                    context + " 대상 Cassette slot에 이미 Material이 있습니다. role=" +
                    ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            WaferMaterial stageWafer = ResolveStageWafer();
            if (stageWafer != null)
                return Fail("OUT-FEEDER-CST-ALIGN-STAGE-DATA", "Material",
                    context + " 대상 OutputStage에 Material 데이터가 남아 있습니다. side=" +
                    Options.Side + ", wafer=" + stageWafer.WaferId);

            string stageState;
            if (!IsTargetStageSafelyStoppedAtUnload(out stageState))
                return Fail("OUT-FEEDER-CST-ALIGN-STAGE", Stage.Name,
                    context + " 대상 OutputStage가 안전하게 정지된 정확한 Unload 위치가 아닙니다. " + stageState);

            Feeder.FeederY.UpdateStatus();
            if (!Feeder.FeederY.IsServoOn || Feeder.FeederY.IsAlarm || Feeder.FeederY.IsMoving)
                return Fail("OUT-FEEDER-CST-ALIGN-FEEDER-STATE", Feeder.Name,
                    context + " OutputFeederY가 안전하게 정지된 상태가 아닙니다. " +
                    Feeder.DescribeBinFeederYMoveDoneState());

            bool feederPositionReady = requireFeederAtCassetteUnload
                ? Feeder.IsBinFeederYInCassetteUnloadPosition(Options.Side)
                : Feeder.IsBinFeederYInAvoidPosition() && Feeder.IsBinFeederAvoidPositionCheck();
            if (!feederPositionReady)
                return Fail("OUT-FEEDER-CST-ALIGN-FEEDER-POS", Feeder.Name,
                    context + " OutputFeederY 정렬 위치가 정확하지 않습니다. required=" +
                    (requireFeederAtCassetteUnload ? "CassetteUnload" : "Avoid") +
                    ", side=" + Options.Side + ", " + Feeder.DescribeBinFeederYMoveDoneState());

            if (!Feeder.IsFeederDown() || Feeder.IsFeederUnclamped() != requireFeederUnclamped)
                return Fail("OUT-FEEDER-CST-ALIGN-FEEDER-POSTURE", Feeder.Name,
                    context + " OutputFeeder 실린더 자세가 올바르지 않습니다. required=" +
                    (requireFeederUnclamped ? "Unclamp+LiftDown" : "Clamp+LiftDown") +
                    ", " + Feeder.DescribeFeederCylinderState());

            TargetCassette targetCassette = ResolveOutputTargetCassette();
            double cassetteUnloadTarget = Cassette.CalculateBinCassetteSlotTargetPosition(targetCassette, Options.SlotIndex) +
                                           Cassette.ResolveUnloadingPositionOffset();
            Cassette.OutputLifterZ.UpdateStatus();
            if (!Cassette.OutputLifterZ.IsServoOn ||
                Cassette.OutputLifterZ.IsAlarm ||
                Cassette.OutputLifterZ.IsMoving ||
                !Cassette.IsBinLifterZInPosition(cassetteUnloadTarget))
            {
                return Fail("OUT-FEEDER-CST-ALIGN-CASSETTE", Cassette.Name,
                    context + " OutputCassette Lifter가 안전하게 정지된 정확한 Unload offset 위치가 아닙니다. " +
                    Cassette.DescribeOutputLifterZState(cassetteUnloadTarget));
            }

            if (requireRingDetected && IsStrictOutputHardwareMode())
            {
                if (Feeder.BinFeederRingCheckSensor == null)
                    return Fail("OUT-FEEDER-CST-ALIGN-RING-MISSING", Feeder.Name,
                        context + " OutputFeeder Ring 센서 정보를 찾을 수 없습니다.");

                if (Feeder.BinFeederRingCheckSensor.Config != null &&
                    (Feeder.BinFeederRingCheckSensor.Config.IsSimulationMode ||
                     Feeder.BinFeederRingCheckSensor.Config.IgnoreWaits))
                {
                    return Fail("OUT-FEEDER-CST-ALIGN-RING-MODE", Feeder.Name,
                        context + " 실기 운전 중 OutputFeeder Ring 센서가 Simulation/DryRun 설정입니다.");
                }

                int errorCode;
                if (!AjinIoScanService.TryReadHardwareInput(Feeder.BinFeederRingCheckSensor, out errorCode))
                    return Fail("OUT-FEEDER-CST-ALIGN-RING-READ", Feeder.Name,
                        context + " OutputFeeder Ring 센서 실제 입력 읽기 실패. errorCode=" + errorCode);

                if (!Feeder.BinFeederRingCheckSensor.IsOn)
                    return Fail("OUT-FEEDER-CST-ALIGN-RING-OFF", Feeder.Name,
                        context + " OutputFeeder에서 제품이 감지되지 않아 해제 동작을 차단합니다. wafer=" + wafer.WaferId);
            }

            return 0;
        }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // Resume이 어떤 중간 Step에서 시작해도 상위 호출자가 실제로 보유한 동일 Context의
                // Place/대상 Stage lease 없이는 물리 동작을 진행하지 않는다.
                int resourceReady = VerifyTargetStageAreaResourceOwned();
                if (resourceReady != 0)
                    return Task.FromResult(resourceReady);

                // 저장된 중간 Step을 그대로 실행하면 선행 정렬/센서 검증을 건너뛸 수 있다.
                // 첫 진입에서 실제 Material 및 Clamp 상태를 기준으로 안전 검증 Step으로 정규화한다.
                if (!_resumeEntryNormalized)
                {
                    _resumeEntryNormalized = true;
                    if (CurrentStep != OutputFeederUnloadToCassetteStep.CheckUnit)
                        return Task.FromResult(NormalizeResumeEntryStep());
                }

                switch (CurrentStep)
                {
                    // 유닛 확인
                    case OutputFeederUnloadToCassetteStep.CheckUnit:
                        return Task.FromResult(CheckUnit(OutputFeederUnloadToCassetteStep.CheckTransferReady));

                    // 이송 준비 확인
                    case OutputFeederUnloadToCassetteStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());

                    // 피더 BIN 데이터 확인
                    case OutputFeederUnloadToCassetteStep.CheckFeederBinData:
                        return Task.FromResult(CheckFeederBinData());

                    // 카세트 대상 슬롯 확인
                    case OutputFeederUnloadToCassetteStep.CheckCassetteTargetSlot:
                        return Task.FromResult(CheckCassetteTargetSlot());

                    // 피더 클램프 검증
                    case OutputFeederUnloadToCassetteStep.VerifyFeederClamped:
                        return Task.FromResult(VerifyFeederClamped());

                    // 피더 리프트 다운 검증
                    case OutputFeederUnloadToCassetteStep.VerifyFeederLiftDown:
                        return Task.FromResult(VerifyFeederLiftDown());

                    // BIN 감지 검증
                    case OutputFeederUnloadToCassetteStep.VerifyBinDetected:
                        return VerifyBinDetectedAsync(ct);

                    // 카세트 BIN 슬롯 이동
                    case OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlot:
                        return MoveCassetteToBinSlotAsync(ct);

                    // 피더 카세트 언로드 위치 이동
                    case OutputFeederUnloadToCassetteStep.MoveFeederCassetteUnloadPosition:
                        return MoveFeederCassetteUnloadPositionAsync(ct);

                    // 피더 언클램프 준비
                    case OutputFeederUnloadToCassetteStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);

                    // 피더 어보이드 위치 이동
                    case OutputFeederUnloadToCassetteStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);

                    // BIN 해제로 카세트 검증
                    case OutputFeederUnloadToCassetteStep.VerifyBinReleasedToCassette:
                        return VerifyBinReleasedToCassetteAsync(ct);

                    // 제품 인출 후 스테이지 실린더 복귀 시작 조건 확인
                    case OutputFeederUnloadToCassetteStep.VerifyPostUnloadStageRestoreReady:
                        return Task.FromResult(VerifyPostUnloadStageRestoreReady());

                    // 대상 스테이지 Guide Down
                    case OutputFeederUnloadToCassetteStep.LowerTargetStageGuideAfterUnload:
                        return LowerTargetStageGuideAfterUnloadAsync(ct);

                    // Clamp Lift 구동 전 Clamp Back/Unclamp 재확인
                    case OutputFeederUnloadToCassetteStep.VerifyTargetStageUnclamped:
                        return Task.FromResult(VerifyTargetStageUnclamped());

                    // 대상 스테이지 Clamp Lift Up
                    case OutputFeederUnloadToCassetteStep.LiftTargetStageClampAfterUnload:
                        return LiftTargetStageClampAfterUnloadAsync(ct);

                    // 대상 스테이지 Clamp Forward
                    case OutputFeederUnloadToCassetteStep.ClampTargetStageAfterUnload:
                        return ClampTargetStageAfterUnloadAsync(ct);

                    // Stage Y 이동 전 최종 안전 자세 확인
                    case OutputFeederUnloadToCassetteStep.VerifyPostUnloadStageFinalState:
                        return Task.FromResult(VerifyPostUnloadStageFinalState());

                    // 자재 데이터를 카세트로 이동
                    case OutputFeederUnloadToCassetteStep.MoveMaterialDataToCassette:
                        return Task.FromResult(MoveMaterialDataToCassette());

                    // 카세트 데이터 갱신
                    case OutputFeederUnloadToCassetteStep.UpdateCassetteData:
                        return Task.FromResult(UpdateCassetteData());

                    // 배출 후 카세트 슬롯 위치 복귀 (bin 안착)
                    case OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlotFinalPosition:
                        return MoveCassetteToBinSlotFinalPositionAsync(ct);

                    // 아웃풋 카세트 AVOID 이동
                    case OutputFeederUnloadToCassetteStep.MoveOutputCassetteAvoidPosition:
                        return MoveOutputCassetteAvoidAndVerifyStageRestoredAsync(ct);

                    default:
                        return Task.FromResult(FailUnsupportedStep());
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("OUT-FEEDER-CST-UNLOAD-EX", Name, "Unload to cassette step failed: " + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            string readyReason;
            if (!Feeder.CheckFeederCassetteReady(Options.Side, Options.SlotIndex, TransferMode.Unload, out readyReason))
                return Fail("OUT-FEEDER-CST-UNLOAD-READY", Feeder.Name, "Output feeder cassette unload is not ready. " + readyReason);

            // 기존 조건: 스테이지 상태를 확인하지 않고 카세트 배출을 진행했다.
            // 현재 기준: 스테이지에서 빼온 제품을 카세트로 보내는 흐름이므로, 배출 시작 전 대상 스테이지가
            //           언로드 위치(Good은 Y+Z, NG는 Y)를 유지하고 있어야 정상이다.
            // To do: 카세트 배출 전 대상 스테이지 언로드 위치 선행 검증.
            if (Stage == null)
                return Fail("OUT-STAGE-MISSING", "OutputStage", "Output stage unit is not available before cassette unload. side=" + Options.Side);

            string stageUnloadState;
            if (!IsTargetStageSafelyStoppedAtUnload(out stageUnloadState))
                return Fail("OUT-STAGE-UNLOAD-POS", Stage.Name,
                    "카세트 배출 전 OutputStage가 안전하게 정지된 정확한 Unload 위치에 준비되지 않았습니다. 먼저 " +
                    Options.Side + " 스테이지 UNLOAD 준비를 실행하세요. " + stageUnloadState);

            int stageResourceReady = VerifyTargetStageAreaResourceOwned();
            if (stageResourceReady != 0)
                return stageResourceReady;

            CurrentStep = OutputFeederUnloadToCassetteStep.CheckFeederBinData;
            return 0;
        }

        private int CheckFeederBinData()
        {
            return CheckCassetteSlotReadyForUnload(OutputFeederUnloadToCassetteStep.CheckCassetteTargetSlot);
        }

        private int CheckCassetteTargetSlot()
        {
            WaferMaterial cassetteWafer = ResolveCassetteWafer();
            if (cassetteWafer != null)
                return Fail("OUT-FEEDER-CST-SLOT-OCCUPIED", "Material",
                    "Output cassette target slot became occupied before unload. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", waferId=" + cassetteWafer.WaferId +
                    ", state=" + cassetteWafer.State + ", loc=" + cassetteWafer.CurrentLocation);

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyFeederClamped;
            return 0;
        }

        private int VerifyFeederClamped()
        {
            if (Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-CLAMP-CHECK", Feeder.Name,
                    "Output feeder must already be clamped before cassette unload move. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyFeederLiftDown;
            return 0;
        }

        private int VerifyFeederLiftDown()
        {
            if (!Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "Output feeder must already be down before cassette unload. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyBinDetected;
            return 0;
        }

        private async Task<int> VerifyBinDetectedAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data disappeared before cassette unload.");

            if (!IsHardwareBypass())
            {
                bool detected = await Feeder.WaitFeederRingState(true, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("OUT-FEEDER-CST-UNLOAD-RING-DETECT", Feeder.Name, "Output feeder ring was not detected before cassette unload. waferId=" + wafer.WaferId);
            }

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlot;
            return 0;
        }

        private async Task<int> MoveCassetteToBinSlotAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (Cassette == null)
                return Fail("OUT-FEEDER-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");

            TargetCassette targetCassette = ResolveOutputTargetCassette();
            // 기존 조건: 슬롯 위치로 바로 이동해 배출 준비를 했다 - 처진 bin이 셸프와 간섭해 제품을 받지 못했다.
            // double targetPosition = Cassette.CalculateBinCassetteSlotTargetPosition(targetCassette, Options.SlotIndex);
            // 현재 기준: Input UnloadToCassette와 동일하게 슬롯 위치 + UnloadingPositionOffset 로 이동해 진입 간섭을 피한다.
            // To do: [언로드 오프셋] 배출 준비 위치 = 슬롯 위치 + Config.UnloadingPositionOffset.
            double targetPosition = Cassette.CalculateBinCassetteSlotTargetPosition(targetCassette, Options.SlotIndex) +
                                    Cassette.ResolveUnloadingPositionOffset();

            string readyReason;
            if (!Cassette.CheckBinLifterZMoveReady(out readyReason))
                return Fail("OUT-FEEDER-CST-MOVE-READY", Cassette.Name,
                    "Output cassette is not ready to move before feeder unload. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", target=" + targetCassette +
                    ", targetPosition=" + targetPosition +
                    ". " + readyReason);

            try
            {
                // 기존 조건: PrepareBinCassetteForFeederLoad(슬롯 위치 이동) 사용.
                // int result = await AwaitStepWithCancellationAsync(
                //     Cassette.PrepareBinCassetteForFeederLoad(targetCassette, Options.SlotIndex, ResolveTimeout(), Options.FineMove),
                //     ct).ConfigureAwait(false);
                // 현재 기준: 배출 전용 오프셋 이동 사용.
                int result = await AwaitStepWithCancellationAsync(
                    Cassette.PrepareBinCassetteForFeederUnload(targetCassette, Options.SlotIndex, ResolveTimeout(), Options.FineMove),
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-FEEDER-CST-SLOT-MOVE", Cassette.Name,
                        "Output cassette slot move failed before feeder unload. role=" + ResolveOutputCassetteRole() +
                        ", slot=" + Options.SlotIndex + ", target=" + targetCassette +
                        ", targetPosition=" + targetPosition + ", result=" + result +
                        ". " + Cassette.DescribeOutputLifterZState(targetPosition));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-FEEDER-CST-SLOT-MOVE", Cassette.Name,
                    "Output cassette slot move exception before feeder unload. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", target=" + targetCassette +
                    ", targetPosition=" + targetPosition +
                    ", message=" + ex.Message + ". " + Cassette.DescribeOutputLifterZState(targetPosition));
            }

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveFeederCassetteUnloadPosition;
            return 0;
        }

        private async Task<int> MoveFeederCassetteUnloadPositionAsync(CancellationToken ct)
        {
            int alignment = VerifyCassetteTransferAlignmentBeforeRelease(
                false,
                false,
                true,
                "Feeder Cassette Unload 이동 전");
            if (alignment != 0)
                return alignment;

            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederCassetteUnloadPosition(Options.Side, Options.SlotIndex, Options.FineMove), "cassette unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederYInCassetteUnloadPosition(Options.Side), "cassette unload", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadToCassetteStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            int alignment = VerifyCassetteTransferAlignmentBeforeRelease(
                true,
                false,
                true,
                "Feeder Unclamp 직전");
            if (alignment != 0)
                return alignment;

            int result = await Feeder.SetFeederClampAsync(false, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name,
                    "Output feeder unclamp command failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            if (!Feeder.IsFeederUnclamped())
                return Fail("OUT-FEEDER-UNCLAMP", Feeder.Name,
                    "Output feeder unclamp failed. result=" + result + ", " + Feeder.DescribeFeederCylinderState());

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            int alignment = VerifyCassetteTransferAlignmentBeforeRelease(
                true,
                true,
                false,
                "제품 해제 후 Feeder Avoid 이동 전");
            if (alignment != 0)
                return alignment;

            int result = await MoveFeederYCommandAsync(Feeder.MoveToFeederAvoidPosition(Options.FineMove), "cassette unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            result = await WaitFeederYDoneAsync(() => Feeder.IsBinFeederInAvoidPosition(), "cassette unload avoid", ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyBinReleasedToCassette;
            return 0;
        }

        private async Task<int> VerifyBinReleasedToCassetteAsync(CancellationToken ct)
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("OUT-FEEDER-DATA-MISSING", "Material", "Output feeder data disappeared before cassette material move.");

            if (!IsHardwareBypass())
            {
                bool cleared = await Feeder.WaitFeederRingState(false, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("OUT-FEEDER-CST-RING", Feeder.Name, "Output feeder ring remained after cassette unload. waferId=" + wafer.WaferId);
            }

            // 물리적으로 Cassette에 제품이 인계되고 Feeder가 Avoid로 빠진 즉시 빈 Stage를 안전 자세로 복귀한다.
            // Material 데이터를 먼저 Cassette로 옮기면 이 구간의 알람/정지 후 Feeder 점유 재개 경로가 사라지므로
            // 반드시 Stage 복귀 완료 후에 Material 데이터를 갱신한다.
            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyPostUnloadStageRestoreReady;
            return 0;
        }

        private int MoveMaterialDataToCassette()
        {
            bool finalStepReady;
            int finalStepPreflight = VerifyPostUnloadFinalStepReady(out finalStepReady);
            if (!finalStepReady)
                return finalStepPreflight;

            bool cassetteAtAvoid;
            string cassetteState;
            if (!TryEvaluateOutputCassetteAvoidState(out cassetteAtAvoid, out cassetteState))
                return Fail("OUT-FEEDER-CST-LATE-STATE", Cassette != null ? Cassette.Name : "OutputCassette",
                    "Material 갱신 전 OutputCassette Lifter가 안전하게 정지된 상태가 아닙니다. " + cassetteState);

            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
            {
                WaferMaterial existingCassetteWafer = ResolveCassetteWafer();
                if (existingCassetteWafer == null)
                    return Fail("OUT-FEEDER-MATERIAL-MOVE", "Material", "Output feeder wafer data was not found for cassette material move.");

                int existingValidation = ValidateExpectedCassetteWafer(existingCassetteWafer, "Material 이동 재개");
                if (existingValidation != 0)
                    return existingValidation;

                CurrentStep = cassetteAtAvoid
                    ? OutputFeederUnloadToCassetteStep.UpdateCassetteData
                    : OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlotFinalPosition;
                return 0;
            }

            // Material을 Feeder에서 지우기 전에 모든 카세트 물리 이동을 끝낸다.
            // 이 조건을 지키면 슬롯 복귀/Avoid 중 정지되어도 Feeder 점유 재개 경로가 유지된다.
            if (!cassetteAtAvoid)
            {
                CurrentStep = OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlotFinalPosition;
                return 0;
            }

            CassetteMaterialRole targetRole = ResolveOutputCassetteRole();
            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
            {
                return Fail("OUT-FEEDER-MATERIAL-EXPECTED", "Material",
                    "Material commit 직전 Feeder Bin이 실행 시작 시 예상 Bin과 다릅니다. expected=" +
                    Options.ExpectedWaferId + ", actual=" + wafer.WaferId + ", side=" + Options.Side);
            }

            if (wafer.SourceCassetteRole != targetRole || wafer.SourceSlotNumber != Options.SlotIndex)
                return Fail("OUT-FEEDER-MATERIAL-SOURCE", "Material", "물리 배출 후 Material 갱신 직전에 원본 cassette/slot 불일치가 확인되었습니다. wafer=" + wafer.WaferId +
                    ", sourceRole=" + wafer.SourceCassetteRole + ", sourceSlot=" + (wafer.SourceSlotNumber + 1).ToString("00") +
                    ", targetRole=" + targetRole + ", targetSlot=" + (Options.SlotIndex + 1).ToString("00"));

            bool gradeMatches = Options.Side == BinSide.Ng
                ? wafer.OutputGrade == DieResult.NG && targetRole == CassetteMaterialRole.Ng1
                : wafer.OutputGrade == DieResult.Good &&
                  (targetRole == CassetteMaterialRole.Good1 || targetRole == CassetteMaterialRole.Good2);
            if (!gradeMatches)
            {
                return Fail("OUT-FEEDER-MATERIAL-GRADE", "Material", "Output grade/side와 복귀 cassette role이 정확히 일치하지 않습니다. wafer=" + wafer.WaferId +
                    ", side=" + Options.Side + ", grade=" + wafer.OutputGrade + ", targetRole=" + targetRole);
            }

            int physicalReleaseProof = VerifyPhysicalReleaseBeforeMaterialCommit(wafer);
            if (physicalReleaseProof != 0)
                return physicalReleaseProof;

            WaferMaterial targetWafer = ResolveCassetteWafer();
            if (targetWafer != null && !string.Equals(targetWafer.WaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("OUT-FEEDER-MATERIAL-TARGET", "Material", "물리 배출 후 대상 cassette slot에 다른 Material이 확인되어 데이터를 덮어쓰지 않습니다. movingWafer=" + wafer.WaferId +
                    ", targetWafer=" + targetWafer.WaferId + ", targetRole=" + targetRole + ", targetSlot=" + (Options.SlotIndex + 1).ToString("00"));

            MaterialStateService.PutWaferInCassette(
                wafer.WaferId,
                targetRole,
                Options.SlotIndex,
                wafer.CassetteLotId,
                wafer.SourceCassetteSlotPosition,
                WaferMaterialState.Finish);
            Feeder.ClearFeederMaterialState();
            PublishCassetteDataUpdateAfterCommit();
            CurrentStep = OutputFeederUnloadToCassetteStep.Complete;
            MarkResumeStateCompletedAtLogicalCutover();
            return 0;
        }

        private int UpdateCassetteData()
        {
            WaferMaterial cassetteWafer = ResolveCassetteWafer();
            if (cassetteWafer == null)
                return Fail("OUT-FEEDER-CST-DATA-MISSING", "Material", "Output cassette data was not created after feeder unload. role=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            if (ResolveFeederWafer() != null)
                return Fail("OUT-FEEDER-DATA-CLEAR", "Material", "Output feeder data was not cleared after cassette unload. waferId=" + cassetteWafer.WaferId);

            int validation = ValidateExpectedCassetteWafer(cassetteWafer, "Cassette 데이터 갱신");
            if (validation != 0)
                return validation;

            bool cassetteAtAvoid;
            string cassetteState;
            if (!TryEvaluateOutputCassetteAvoidState(out cassetteAtAvoid, out cassetteState))
                return Fail("OUT-FEEDER-CST-UPDATE-STATE", Cassette != null ? Cassette.Name : "OutputCassette",
                    "Cassette 데이터 완료 처리 전 OutputCassette Lifter가 안전하게 정지된 상태가 아닙니다. " + cassetteState);

            if (!cassetteAtAvoid)
            {
                bool finalStepReady;
                int finalStepPreflight = VerifyPostUnloadFinalStepReady(out finalStepReady);
                if (!finalStepReady)
                    return finalStepPreflight;

                CurrentStep = OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlotFinalPosition;
                return 0;
            }

            // 구버전 ResumeStep가 Material commit 이후의 UpdateCassetteData에 남아 있을 수 있다.
            // Cassette가 이미 Avoid여도 Stage/Feeder 위치, 실린더, 센서 및 lease를 모두 다시 증명한 뒤 완료한다.
            bool completionReady;
            int completionPreflight = VerifyPostUnloadFinalStepReady(out completionReady);
            if (!completionReady)
                return completionPreflight;

            PublishCassetteDataUpdateAfterCommit();
            CurrentStep = OutputFeederUnloadToCassetteStep.Complete;
            MarkResumeStateCompletedAtLogicalCutover();
            return 0;
        }

        private int VerifyPhysicalReleaseBeforeMaterialCommit(WaferMaterial wafer)
        {
            if (Feeder == null || !Feeder.IsFeederUnclamped() || !Feeder.IsFeederDown())
                return Fail("OUT-FEEDER-MATERIAL-RELEASE-POSTURE", Feeder != null ? Feeder.Name : "OutputFeeder",
                    "Material commit 직전 Feeder의 실제 배출 완료 자세가 아닙니다. " +
                    (Feeder != null ? Feeder.DescribeFeederCylinderState() : "OutputFeeder=null"));

            if (!IsStrictOutputHardwareMode())
                return 0;

            if (Feeder.BinFeederRingCheckSensor == null)
                return Fail("OUT-FEEDER-MATERIAL-RING-MISSING", Feeder.Name,
                    "Material commit 직전 OutputFeeder Ring 센서 정보를 찾을 수 없습니다. wafer=" + wafer.WaferId);

            if (Feeder.BinFeederRingCheckSensor.Config != null &&
                (Feeder.BinFeederRingCheckSensor.Config.IsSimulationMode ||
                 Feeder.BinFeederRingCheckSensor.Config.IgnoreWaits))
            {
                return Fail("OUT-FEEDER-MATERIAL-RING-MODE", Feeder.Name,
                    "실기 운전 중 OutputFeeder Ring 센서가 Simulation/DryRun 설정입니다. wafer=" + wafer.WaferId);
            }

            int errorCode;
            if (!AjinIoScanService.TryReadHardwareInput(Feeder.BinFeederRingCheckSensor, out errorCode))
                return Fail("OUT-FEEDER-MATERIAL-RING-READ", Feeder.Name,
                    "Material commit 직전 OutputFeeder Ring 센서 실제 입력 읽기 실패. wafer=" +
                    wafer.WaferId + ", errorCode=" + errorCode);

            if (Feeder.BinFeederRingCheckSensor.IsOn)
                return Fail("OUT-FEEDER-MATERIAL-RING-DETECTED", Feeder.Name,
                    "Material commit 직전 OutputFeeder에서 Ring이 계속 감지됩니다. wafer=" + wafer.WaferId);

            return 0;
        }

        private void PublishCassetteDataUpdateAfterCommit()
        {
            try
            {
                Context.Bus.Set("OutputFeederEmpty");
                Context.Bus.Set("OutputCassetteSlotUpdated");
            }
            catch (Exception ex)
            {
                WriteLog(Name,
                    "Material commit 후 Output signal 갱신 중 예외가 발생했지만 물리/Material 완료 상태를 유지합니다. " +
                    "error=" + ex.Message + " - Check");
            }

            try
            {
                NotifyOutputCassetteReplacementIfComplete();
            }
            catch (Exception ex)
            {
                WriteLog(Name,
                    "Material commit 후 Cassette 교체 알림 갱신 중 예외가 발생했지만 완료 상태를 유지합니다. " +
                    "error=" + ex.Message + " - Check");
            }
        }

        // To do: [언로드 오프셋] 피더 이탈 후 리프터를 정확한 슬롯 위치로 복귀시켜 bin을 안착시킨다.
        //        (Input UnloadToCassette의 MoveCassetteToSlotPosition과 동일 개념)
        private async Task<int> MoveCassetteToBinSlotFinalPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            bool finalStepReady;
            int finalStepPreflight = VerifyPostUnloadFinalStepReady(out finalStepReady);
            if (!finalStepReady)
                return finalStepPreflight;

            bool cassetteAtAvoid;
            string cassetteState;
            if (!TryEvaluateOutputCassetteAvoidState(out cassetteAtAvoid, out cassetteState))
                return Fail("OUT-FEEDER-CST-FINAL-SLOT-STATE", Cassette != null ? Cassette.Name : "OutputCassette",
                    "Output cassette final slot 이동 전 Lifter가 안전하게 정지된 상태가 아닙니다. " + cassetteState);

            if (Cassette == null)
                return Fail("OUT-FEEDER-CST-MISSING", "OutputCassette", "Output cassette unit is not available.");

            TargetCassette targetCassette = ResolveOutputTargetCassette();
            double targetPosition = Cassette.CalculateBinCassetteSlotTargetPosition(targetCassette, Options.SlotIndex);

            try
            {
                int result = await AwaitStepWithCancellationAsync(
                    Cassette.PrepareBinCassetteForFeederLoad(targetCassette, Options.SlotIndex, ResolveTimeout(), Options.FineMove),
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("OUT-FEEDER-CST-FINAL-SLOT-MOVE", Cassette.Name,
                        "Output cassette final slot move failed after feeder unload. role=" + ResolveOutputCassetteRole() +
                        ", slot=" + Options.SlotIndex + ", target=" + targetCassette +
                        ", targetPosition=" + targetPosition + ", result=" + result +
                        ". " + Cassette.DescribeOutputLifterZState(targetPosition));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("OUT-FEEDER-CST-FINAL-SLOT-MOVE", Cassette.Name,
                    "Output cassette final slot move exception after feeder unload. role=" + ResolveOutputCassetteRole() +
                    ", slot=" + Options.SlotIndex + ", target=" + targetCassette +
                    ", targetPosition=" + targetPosition +
                    ", message=" + ex.Message + ". " + Cassette.DescribeOutputLifterZState(targetPosition));
            }

            CurrentStep = OutputFeederUnloadToCassetteStep.MoveOutputCassetteAvoidPosition;
            return 0;
        }

        private async Task<int> MoveOutputCassetteAvoidAndVerifyStageRestoredAsync(CancellationToken ct)
        {
            bool finalStepReady;
            int finalStepPreflight = VerifyPostUnloadFinalStepReady(out finalStepReady);
            if (!finalStepReady)
                return finalStepPreflight;

            bool cassetteAtAvoid;
            string cassetteState;
            if (!TryEvaluateOutputCassetteAvoidState(out cassetteAtAvoid, out cassetteState))
                return Fail("OUT-FEEDER-CST-AVOID-STATE", Cassette != null ? Cassette.Name : "OutputCassette",
                    "Output cassette Avoid 이동 전 Lifter가 안전하게 정지된 상태가 아닙니다. " + cassetteState);

            if (!cassetteAtAvoid)
            {
                TargetCassette targetCassette = ResolveOutputTargetCassette();
                if (!Cassette.IsBinLifterZInSlotPosition(targetCassette, Options.SlotIndex))
                {
                    WriteLog(Name,
                        "Cassette Avoid 재개 전 정확한 final slot 안착 위치가 아니므로 final slot 이동부터 다시 실행합니다. " +
                        "role=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex +
                        ", " + cassetteState + " - Check");
                    CurrentStep = OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlotFinalPosition;
                    return 0;
                }
            }

            int result = await MoveOutputCassetteAvoidPositionAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            // 현재 정상 흐름에서는 Stage 복귀가 Material 갱신 전에 완료되어 있다.
            // 구버전 ResumeStep 또는 중간 상태 변경으로 안전 자세가 아니면 완료 처리하지 않고 복귀 Step으로 되돌린다.
            if (!Stage.IsBinGuideDown(Options.Side) ||
                !Stage.IsBinGuideClampLiftUp(Options.Side) ||
                !Stage.IsBinGuideClamped(Options.Side) ||
                !Stage.IsBinGuideDown(BinSide.Good) ||
                !Stage.IsBinGuideClampLiftUp(BinSide.Ng))
            {
                WriteLog(Name,
                    "Cassette Avoid 완료 후 OutputStage 안전 자세 재확인이 필요하여 복귀 Step으로 이동합니다. " +
                    "side=" + Options.Side + " - Check");
                CurrentStep = OutputFeederUnloadToCassetteStep.VerifyPostUnloadStageRestoreReady;
                return 0;
            }

            WaferMaterial feederWafer = ResolveFeederWafer();
            if (feederWafer != null)
            {
                CurrentStep = OutputFeederUnloadToCassetteStep.MoveMaterialDataToCassette;
                return 0;
            }

            WaferMaterial cassetteWafer = ResolveCassetteWafer();
            if (cassetteWafer != null)
            {
                int validation = ValidateExpectedCassetteWafer(cassetteWafer, "Cassette Avoid 완료 재개");
                if (validation != 0)
                    return validation;

                CurrentStep = OutputFeederUnloadToCassetteStep.UpdateCassetteData;
                return 0;
            }

            return Fail("OUT-FEEDER-CST-AVOID-MATERIAL-MISSING", "Material",
                "Cassette Avoid 완료 후 Feeder와 대상 Cassette에서 Material을 모두 찾을 수 없습니다. side=" +
                Options.Side + ", cassetteRole=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);
        }

        private int VerifyPostUnloadStageRestoreReady()
        {
            int ready = CheckPostUnloadStageRestorePrerequisites();
            if (ready != 0)
                return ready;

            CurrentStep = OutputFeederUnloadToCassetteStep.LowerTargetStageGuideAfterUnload;
            return 0;
        }

        private async Task<int> LowerTargetStageGuideAfterUnloadAsync(CancellationToken ct)
        {
            int ready = CheckPostUnloadStageRestorePrerequisites();
            if (ready != 0)
                return ready;

            int result = await Stage.EnsureBinGuideDownAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-GUIDE-DOWN-AFTER-UNLOAD", Stage.Name,
                    "제품 인출 후 OutputStage Guide Down 구동 실패. side=" + Options.Side +
                    ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideDown(Options.Side))
                return Fail("OUT-STAGE-GUIDE-DOWN-AFTER-UNLOAD", Stage.Name,
                    "제품 인출 후 OutputStage Guide Down 상태 확인 실패. side=" + Options.Side +
                    ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyTargetStageUnclamped;
            return 0;
        }

        private int VerifyTargetStageUnclamped()
        {
            int ready = CheckPostUnloadStageRestorePrerequisites();
            if (ready != 0)
                return ready;

            if (!Stage.IsBinGuideDown(Options.Side))
                return Fail("OUT-STAGE-GUIDE-DOWN-CHECK-AFTER-UNLOAD", Stage.Name,
                    "Clamp Lift Up 전 OutputStage Guide가 Down 상태가 아닙니다. side=" + Options.Side +
                    ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-UNCLAMP-CHECK-AFTER-UNLOAD", Stage.Name,
                    "Clamp Lift Up 전 OutputStage Clamp가 Back/Unclamp 상태가 아닙니다. side=" + Options.Side +
                    ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadToCassetteStep.LiftTargetStageClampAfterUnload;
            return 0;
        }

        private async Task<int> LiftTargetStageClampAfterUnloadAsync(CancellationToken ct)
        {
            int ready = CheckPostUnloadStageRestorePrerequisites();
            if (ready != 0)
                return ready;

            if (!Stage.IsBinGuideDown(Options.Side) || !Stage.IsBinGuideUnclamped(Options.Side))
                return Fail("OUT-STAGE-CLAMP-UP-READY-AFTER-UNLOAD", Stage.Name,
                    "제품 인출 후 Clamp Lift Up 선행 조건 불만족. Guide Down 및 Clamp Back이 필요합니다. side=" +
                    Options.Side + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            int result = await Stage.EnsureBinGuideClampLiftUpAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-UP-AFTER-UNLOAD", Stage.Name,
                    "제품 인출 후 OutputStage Clamp Lift Up 구동 실패. side=" + Options.Side +
                    ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClampLiftUp(Options.Side))
                return Fail("OUT-STAGE-CLAMP-UP-AFTER-UNLOAD", Stage.Name,
                    "제품 인출 후 OutputStage Clamp Lift Up 상태 확인 실패. side=" + Options.Side +
                    ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadToCassetteStep.ClampTargetStageAfterUnload;
            return 0;
        }

        private async Task<int> ClampTargetStageAfterUnloadAsync(CancellationToken ct)
        {
            int ready = CheckPostUnloadStageRestorePrerequisites();
            if (ready != 0)
                return ready;

            if (!Stage.IsBinGuideDown(Options.Side) || !Stage.IsBinGuideClampLiftUp(Options.Side))
                return Fail("OUT-STAGE-CLAMP-READY-AFTER-UNLOAD", Stage.Name,
                    "제품 인출 후 Clamp Forward 선행 조건 불만족. Guide Down 및 Clamp Lift Up이 필요합니다. side=" +
                    Options.Side + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            int result = await Stage.EnsureBinGuideClampedAsync(Options.Side, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OUT-STAGE-CLAMP-AFTER-UNLOAD", Stage.Name,
                    "제품 인출 후 OutputStage Clamp Forward 구동 실패. side=" + Options.Side +
                    ", result=" + result + ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            if (!Stage.IsBinGuideClamped(Options.Side))
                return Fail("OUT-STAGE-CLAMP-AFTER-UNLOAD", Stage.Name,
                    "제품 인출 후 OutputStage Clamp Forward 상태 확인 실패. side=" + Options.Side +
                    ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            CurrentStep = OutputFeederUnloadToCassetteStep.VerifyPostUnloadStageFinalState;
            return 0;
        }

        private int VerifyPostUnloadStageFinalState()
        {
            int ready = CheckPostUnloadStageRestorePrerequisites();
            if (ready != 0)
                return ready;

            if (!Stage.IsBinGuideDown(Options.Side) ||
                !Stage.IsBinGuideClampLiftUp(Options.Side) ||
                !Stage.IsBinGuideClamped(Options.Side))
            {
                return Fail("OUT-STAGE-POST-UNLOAD-STATE", Stage.Name,
                    "제품 인출 후 대상 OutputStage 최종 안전 자세가 아닙니다. " +
                    "필요 상태=Guide Down, Clamp Lift Up, Clamp Forward. side=" + Options.Side +
                    ", " + Stage.DescribeOutputStageInterlockState(Options.Side));
            }

            if (!Stage.IsBinGuideDown(BinSide.Good) || !Stage.IsBinGuideClampLiftUp(BinSide.Ng))
                return Fail("OUT-STAGE-POST-UNLOAD-GLOBAL-STATE", Stage.Name,
                    "OutputStage Y 이동 전 공통 안전 자세가 아닙니다. " +
                    "Good Guide Down 및 NG Clamp Lift Up이 필요합니다. side=" + Options.Side +
                    ", " + Stage.DescribeOutputStageInterlockState(Options.Side));

            WriteLog(Name,
                "제품 인출 후 OutputStage 안전 복귀 완료. 순서=Guide Down->Clamp Lift Up->Clamp Forward, side=" +
                Options.Side + " - Ok");

            bool cassetteAtAvoid;
            string cassetteState;
            if (!TryEvaluateOutputCassetteAvoidState(out cassetteAtAvoid, out cassetteState))
                return Fail("OUT-FEEDER-CST-POST-STAGE-STATE", Cassette != null ? Cassette.Name : "OutputCassette",
                    "OutputStage 복귀 후 다음 Step 결정 전 OutputCassette Lifter가 안전하게 정지된 상태가 아닙니다. " + cassetteState);

            if (ResolveFeederWafer() != null)
            {
                CurrentStep = cassetteAtAvoid
                    ? OutputFeederUnloadToCassetteStep.MoveMaterialDataToCassette
                    : OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlotFinalPosition;
                return 0;
            }

            // 구버전 흐름은 Material 갱신 및 Cassette Avoid 후에 Stage 복귀를 수행했을 수 있다.
            // 대상 Cassette에 자재가 이미 존재하면 중복 Material 이동 없이 완료한다.
            WaferMaterial cassetteWafer = ResolveCassetteWafer();
            if (cassetteWafer != null)
            {
                int validation = ValidateExpectedCassetteWafer(cassetteWafer, "구버전 Stage 복귀 재개");
                if (validation != 0)
                    return validation;

                CurrentStep = cassetteAtAvoid
                    ? OutputFeederUnloadToCassetteStep.UpdateCassetteData
                    : OutputFeederUnloadToCassetteStep.MoveCassetteToBinSlotFinalPosition;
                return 0;
            }

            return Fail("OUT-STAGE-POST-UNLOAD-MATERIAL-MISSING", "Material",
                "OutputStage 안전 복귀 후 Feeder와 대상 Cassette에서 Material을 모두 찾을 수 없습니다. side=" +
                Options.Side + ", cassetteRole=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);
        }

        private int ValidateExpectedCassetteWafer(WaferMaterial cassetteWafer, string resumeContext)
        {
            if (cassetteWafer == null)
                return Fail("OUT-STAGE-POST-UNLOAD-MATERIAL-MISSING", "Material",
                    resumeContext + " 중 대상 Cassette Material을 찾을 수 없습니다. cassetteRole=" +
                    ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, cassetteWafer.WaferId, StringComparison.OrdinalIgnoreCase))
            {
                return Fail("OUT-STAGE-POST-UNLOAD-MATERIAL-MISMATCH", "Material",
                    resumeContext + " 중 대상 Cassette Material이 예상 Bin과 다릅니다. expected=" +
                    Options.ExpectedWaferId + ", actual=" + cassetteWafer.WaferId +
                    ", cassetteRole=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);
            }

            CassetteMaterialRole targetRole = ResolveOutputCassetteRole();
            if (cassetteWafer.SourceCassetteRole != targetRole ||
                cassetteWafer.SourceSlotNumber != Options.SlotIndex)
            {
                return Fail("OUT-STAGE-POST-UNLOAD-MATERIAL-SOURCE", "Material",
                    resumeContext + " 중 Cassette Material의 원본 role/slot이 실행 대상과 다릅니다. wafer=" +
                    cassetteWafer.WaferId + ", sourceRole=" + cassetteWafer.SourceCassetteRole +
                    ", sourceSlot=" + (cassetteWafer.SourceSlotNumber + 1).ToString("00") +
                    ", targetRole=" + targetRole +
                    ", targetSlot=" + (Options.SlotIndex + 1).ToString("00"));
            }

            bool gradeMatches = Options.Side == BinSide.Ng
                ? cassetteWafer.OutputGrade == DieResult.NG && targetRole == CassetteMaterialRole.Ng1
                : cassetteWafer.OutputGrade == DieResult.Good &&
                  (targetRole == CassetteMaterialRole.Good1 || targetRole == CassetteMaterialRole.Good2);
            if (!gradeMatches)
            {
                return Fail("OUT-STAGE-POST-UNLOAD-MATERIAL-GRADE", "Material",
                    resumeContext + " 중 Cassette Material의 Output grade/side가 실행 대상과 정확히 일치하지 않습니다. wafer=" +
                    cassetteWafer.WaferId + ", side=" + Options.Side +
                    ", grade=" + cassetteWafer.OutputGrade + ", targetRole=" + targetRole);
            }

            WaferMaterialState state = WaferMaterialStateText.Normalize(cassetteWafer.State);
            MaterialLocation location = cassetteWafer.CurrentLocation;
            if (state != WaferMaterialState.Finish ||
                location == null ||
                location.Kind != MaterialLocationKind.OutputCassette ||
                location.CassetteRole != targetRole ||
                location.SlotNumber != Options.SlotIndex ||
                cassetteWafer.OutputCassetteRole != targetRole ||
                cassetteWafer.OutputSlotNumber != Options.SlotIndex)
            {
                return Fail("OUT-STAGE-POST-UNLOAD-MATERIAL-STATE", "Material",
                    resumeContext + " 중 Cassette Material이 정상 Output 완료 위치/상태가 아닙니다. wafer=" +
                    cassetteWafer.WaferId + ", state=" + state +
                    ", location=" + (location != null ? location.ToString() : "null") +
                    ", outputRole=" + cassetteWafer.OutputCassetteRole +
                    ", outputSlot=" + (cassetteWafer.OutputSlotNumber + 1).ToString("00") +
                    ", targetRole=" + targetRole +
                    ", targetSlot=" + (Options.SlotIndex + 1).ToString("00"));
            }

            return 0;
        }

        private bool IsPostUnloadStageFinalStateSatisfied()
        {
            return Stage != null &&
                   Stage.IsBinGuideDown(Options.Side) &&
                   Stage.IsBinGuideClampLiftUp(Options.Side) &&
                   Stage.IsBinGuideClamped(Options.Side) &&
                   Stage.IsBinGuideDown(BinSide.Good) &&
                   Stage.IsBinGuideClampLiftUp(BinSide.Ng);
        }

        private int VerifyPostUnloadFinalStepReady(out bool ready)
        {
            int prerequisites = CheckPostUnloadStageRestorePrerequisites();
            if (prerequisites != 0)
            {
                ready = false;
                return prerequisites;
            }

            if (!IsPostUnloadStageFinalStateSatisfied())
            {
                WriteLog(Name,
                    "후반 Resume/물리 이동 전 OutputStage 실린더 안전 자세를 다시 확보합니다. side=" +
                    Options.Side + " - Check");
                CurrentStep = OutputFeederUnloadToCassetteStep.VerifyPostUnloadStageRestoreReady;
                ready = false;
                return 0;
            }

            ready = true;
            return 0;
        }

        private bool TryEvaluateOutputCassetteAvoidState(out bool atAvoid, out string state)
        {
            atAvoid = false;
            if (Cassette == null || Cassette.OutputLifterZ == null)
            {
                state = "OutputCassette/OutputLifterZ=null";
                return false;
            }

            Cassette.OutputLifterZ.UpdateStatus();
            state = Cassette.DescribeOutputLifterZState(Cassette.Recipe.AvoidPosition);
            if (!Cassette.OutputLifterZ.IsServoOn ||
                Cassette.OutputLifterZ.IsAlarm ||
                Cassette.OutputLifterZ.IsMoving)
            {
                return false;
            }

            atAvoid = Cassette.IsBinLifterZInAvoidPosition();
            return true;
        }

        private int CheckPostUnloadStageRestorePrerequisites()
        {
            if (Stage == null)
                return Fail("OUT-STAGE-POST-UNLOAD-MISSING", "OutputStage",
                    "제품 인출 후 OutputStage 안전 복귀를 확인할 수 없습니다. side=" + Options.Side);

            int stageResourceReady = VerifyTargetStageAreaResourceOwned();
            if (stageResourceReady != 0)
                return stageResourceReady;

            if (Feeder == null || Feeder.FeederY == null)
                return Fail("OUT-STAGE-POST-UNLOAD-FEEDER-MISSING", "OutputFeeder",
                    "제품 인출 후 OutputFeeder 안전 위치를 확인할 수 없습니다. side=" + Options.Side);

            Feeder.FeederY.UpdateStatus();
            if (!Feeder.FeederY.IsServoOn ||
                Feeder.FeederY.IsAlarm ||
                Feeder.FeederY.IsMoving ||
                !Feeder.IsBinFeederYInAvoidPosition() ||
                !Feeder.IsBinFeederAvoidPositionCheck())
            {
                return Fail("OUT-STAGE-POST-UNLOAD-FEEDER-AVOID", Feeder.Name,
                    "OutputStage 실린더 복귀 전 OutputFeeder가 정지된 정확한 Avoid 위치가 아닙니다. side=" +
                    Options.Side + ", " + Feeder.DescribeBinFeederYMoveDoneState());
            }

            if (!Feeder.IsFeederUnclamped() || !Feeder.IsFeederDown())
            {
                return Fail("OUT-STAGE-POST-UNLOAD-FEEDER-POSTURE", Feeder.Name,
                    "OutputStage 실린더 복귀 전 OutputFeeder의 실제 배출 완료 자세가 아닙니다. " +
                    "필요 상태=Unclamp, Lift Down. side=" + Options.Side + ", " +
                    Feeder.DescribeFeederCylinderState());
            }

            string stageUnloadState;
            if (!IsTargetStageSafelyStoppedAtUnload(out stageUnloadState))
                return Fail("OUT-STAGE-POST-UNLOAD-POS", Stage.Name,
                    "제품 인출 후 실린더 복귀 전 대상 OutputStage가 안전하게 정지된 정확한 Unload 상태가 아닙니다. side=" +
                    Options.Side + ", " + stageUnloadState);

            WaferMaterial stageWafer = ResolveStageWafer();
            if (stageWafer != null)
                return Fail("OUT-STAGE-POST-UNLOAD-DATA-OCCUPIED", "Material",
                    "제품 인출 후 실린더 복귀 전에 대상 OutputStage Material이 남아 있습니다. side=" +
                    Options.Side + ", waferId=" + stageWafer.WaferId);

            if (IsStrictOutputHardwareMode())
            {
                if (Feeder.BinFeederRingCheckSensor == null)
                    return Fail("OUT-STAGE-POST-UNLOAD-FEEDER-RING-MISSING", Feeder.Name,
                        "제품 인출 후 OutputFeeder Ring 센서 정보를 찾을 수 없습니다. side=" + Options.Side);

                if (Feeder.BinFeederRingCheckSensor.Config != null &&
                    (Feeder.BinFeederRingCheckSensor.Config.IsSimulationMode ||
                     Feeder.BinFeederRingCheckSensor.Config.IgnoreWaits))
                {
                    return Fail("OUT-STAGE-POST-UNLOAD-FEEDER-RING-MODE", Feeder.Name,
                        "실기 운전 중 OutputFeeder Ring 센서가 Simulation/DryRun 설정입니다. side=" + Options.Side);
                }

                int feederRingErrorCode;
                if (!AjinIoScanService.TryReadHardwareInput(
                    Feeder.BinFeederRingCheckSensor,
                    out feederRingErrorCode))
                {
                    return Fail("OUT-STAGE-POST-UNLOAD-FEEDER-RING-READ", Feeder.Name,
                        "제품 인출 후 OutputFeeder Ring 센서 실제 입력 읽기 실패. side=" + Options.Side +
                        ", errorCode=" + feederRingErrorCode);
                }

                if (Feeder.BinFeederRingCheckSensor.IsOn)
                    return Fail("OUT-STAGE-POST-UNLOAD-FEEDER-RING-DETECTED", Feeder.Name,
                        "제품 인출 후 OutputFeeder에서 Ring이 계속 감지되어 Stage 복귀를 차단합니다. side=" +
                        Options.Side);

                var ringSensor = Options.Side == BinSide.Ng ? Stage.NgBinRingSensor : Stage.GoodBinRingSensor;
                if (ringSensor == null)
                    return Fail("OUT-STAGE-POST-UNLOAD-RING-MISSING", Stage.Name,
                        "제품 인출 후 대상 OutputStage Ring 센서 정보를 찾을 수 없습니다. side=" + Options.Side);

                if (ringSensor.Config != null &&
                    (ringSensor.Config.IsSimulationMode || ringSensor.Config.IgnoreWaits))
                {
                    return Fail("OUT-STAGE-POST-UNLOAD-RING-MODE", Stage.Name,
                        "실기 운전 중 대상 OutputStage Ring 센서가 Simulation/DryRun 설정입니다. " +
                        "DIO 설정을 실기 모드로 갱신한 뒤 다시 실행하십시오. side=" + Options.Side);
                }

                int errorCode;
                if (!AjinIoScanService.TryReadHardwareInput(ringSensor, out errorCode))
                    return Fail("OUT-STAGE-POST-UNLOAD-RING-READ", Stage.Name,
                        "제품 인출 후 대상 OutputStage Ring 센서 실제 입력 읽기 실패. side=" + Options.Side +
                        ", errorCode=" + errorCode);

                if (ringSensor.IsOn)
                    return Fail("OUT-STAGE-POST-UNLOAD-RING-DETECTED", Stage.Name,
                        "제품 인출 후 대상 OutputStage에서 Ring이 계속 감지되어 실린더 복귀를 차단합니다. side=" +
                        Options.Side);
            }

            return 0;
        }

        private bool IsTargetStageSafelyStoppedAtUnload(out string state)
        {
            state = "side=" + Options.Side;
            if (Stage == null)
            {
                state += ", OutputStage=null";
                return false;
            }

            if (Options.Side == BinSide.Good)
            {
                if (Stage.GoodStage == null ||
                    Stage.GoodStage.StageY == null ||
                    Stage.GoodStage.StageZ == null)
                {
                    state += ", GoodStageY/Z=null";
                    return false;
                }

                Stage.GoodStage.StageY.UpdateStatus();
                Stage.GoodStage.StageZ.UpdateStatus();
                state = Stage.DescribeOutputStageInterlockState(Options.Side);
                if (!Stage.GoodStage.StageY.IsServoOn ||
                    Stage.GoodStage.StageY.IsAlarm ||
                    Stage.GoodStage.StageY.IsMoving ||
                    !Stage.GoodStage.StageZ.IsServoOn ||
                    Stage.GoodStage.StageZ.IsAlarm ||
                    Stage.GoodStage.StageZ.IsMoving)
                {
                    return false;
                }
            }
            else
            {
                if (Stage.NgStage == null || Stage.NgStage.StageY == null)
                {
                    state += ", NgStageY=null";
                    return false;
                }

                Stage.NgStage.StageY.UpdateStatus();
                state = Stage.DescribeOutputStageInterlockState(Options.Side);
                if (!Stage.NgStage.StageY.IsServoOn ||
                    Stage.NgStage.StageY.IsAlarm ||
                    Stage.NgStage.StageY.IsMoving)
                {
                    return false;
                }
            }

            return Stage.IsStageInUnloadPosition(Options.Side);
        }

        private int VerifyTargetStageAreaResourceOwned()
        {
            if (Context == null || Context.Resources == null ||
                !Context.Resources.IsActiveLease(_outputPlaceAreaLease, SequenceResourceKind.OutputPlaceArea))
            {
                return Fail("OUT-STAGE-POST-UNLOAD-PLACE-RESOURCE", Name,
                    "OutputStage 실린더 복귀를 포함하는 UnloadToCassette는 현재 호출자가 동일 Context에서 발급받은 " +
                    "활성 Output Place Area lease를 전달해야 합니다. side=" + Options.Side);
            }

            SequenceResourceKind resource = Options.Side == BinSide.Ng
                ? SequenceResourceKind.OutputNgStageArea
                : SequenceResourceKind.OutputGoodStageArea;
            if (!Context.Resources.IsActiveLease(_outputStageAreaLease, resource))
            {
                return Fail("OUT-STAGE-POST-UNLOAD-RESOURCE", Name,
                    "OutputStage 실린더 복귀를 포함하는 UnloadToCassette는 현재 호출자가 동일 Context에서 발급받은 " +
                    "활성 대상 Stage Area lease를 전달해야 합니다. side=" + Options.Side + ", resource=" + resource);
            }

            return 0;
        }

        private bool IsStrictOutputHardwareMode()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                return settings != null &&
                       settings.UseAjin &&
                       !settings.SimulationMode &&
                       !settings.DryRunMode &&
                       !settings.BypassHardware &&
                       (Context == null || Context.Controller == null || !Context.Controller.GlobalDryRun) &&
                       !Stage.IsOutputStageSimulationOrDryRun();
            }
            catch
            {
                // 운전 모드를 판정하지 못하면 센서 검증을 생략하지 않고 실기 모드로 처리한다.
                return true;
            }
            finally
            {
            }
        }

        private void NotifyOutputCassetteReplacementIfComplete()
        {
            try
            {
                if (!IsOutputSideCassetteComplete())
                    return;

                OutputCassetteOperatorMessageHelper.RequestReplacement(
                    Context,
                    Options.Side,
                    ResolveOutputCassetteRole(),
                    "side=" + Options.Side + ", cassette=" + ResolveOutputCassetteRole() + ", slot=" + Options.SlotIndex);
            }
            catch (Exception ex)
            {
                WriteLog("OutputFeederUnloadToCassetteSequence",
                    "출력 카세트 교체 안내 상태 확인 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private bool IsOutputSideCassetteComplete()
        {
            try
            {
                string consistencyReason;
                if (!OutputSlotPlanner.ValidateSupplyCassetteConsistency(Options.Side, out consistencyReason))
                {
                    WriteLog("OutputFeederUnloadToCassetteSequence",
                        "출력 카세트 센서/Material 데이터 불일치로 교체 완료 상태를 확정하지 않습니다. side=" +
                        Options.Side + ", reason=" + consistencyReason + " - Failed");
                    return false;
                }

                OutputSlotPlan plan;
                if (OutputSlotPlanner.TryResolveNextSupplySlot(Options.Side, out plan))
                    return false;

                if (ResolveFeederWafer() != null)
                    return false;

                if (MaterialStateService.GetWaferAtLocation(ResolveOutputStageLocation()) != null)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                WriteLog("OutputFeederUnloadToCassetteSequence",
                    "출력 카세트 완료 상태 확인 중 예외가 발생했습니다. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }
    }
}
