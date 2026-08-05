using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Barcode;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;

using QMC.CDT320.Interlocks;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal enum InputFeederLoadToStageStep
    {
        Idle,
        CheckUnit,
        CheckTransferReady,
        CheckStageLoadPosition,
        VerifyFeederHoldingWafer,
        MoveFeederStageLoadPosition,
        VerifyWaferBeforeTransfer,
        StageVacuumOn,
        PrepareFeederUnclamp,
        MoveFeederStageLoadAvoidPosition,
        MoveMaterialDataToStage,
        ClearFeederData,
        PrepareFeederLiftUp,
        MoveFeederAvoidPosition,
        PrepareFeederLiftDownAfterAvoid,
        VerifyInputStageData,
        RunBarcodeSequence,
        MoveInputStageProcessPosition,
        MoveInputCassetteAvoidPosition,
        Complete,
        Error
    }

    internal sealed class InputFeederLoadToStageSequence : InputFeederSequenceBase<InputFeederLoadToStageStep>
    {
        public InputFeederLoadToStageSequence(MachineSequenceContext context)
            : base(context, InputFeederSequenceKind.LoadToStage, "InputFeederLoadToStageSequence")
        {
        }

        protected override InputFeederLoadToStageStep IdleStep { get { return InputFeederLoadToStageStep.Idle; } }
        protected override InputFeederLoadToStageStep InitialStep { get { return InputFeederLoadToStageStep.CheckUnit; } }
        protected override InputFeederLoadToStageStep CompleteStep { get { return InputFeederLoadToStageStep.Complete; } }
        protected override InputFeederLoadToStageStep ErrorStep { get { return InputFeederLoadToStageStep.Error; } }

        protected override Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    // 유닛 확인
                    case InputFeederLoadToStageStep.CheckUnit:
                        return Task.FromResult(CheckUnit(InputFeederLoadToStageStep.CheckTransferReady));
                    // 이송 준비 확인
                    case InputFeederLoadToStageStep.CheckTransferReady:
                        return Task.FromResult(CheckTransferReady());
                    // 스테이지 로드 위치 확인
                    case InputFeederLoadToStageStep.CheckStageLoadPosition:
                        return CheckStageLoadPositionAsync(ct);
                    // 피더 보유 웨이퍼 검증
                    case InputFeederLoadToStageStep.VerifyFeederHoldingWafer:
                        return VerifyFeederHoldingWaferAsync(ct);
                    // 피더 스테이지 로드 위치 이동
                    case InputFeederLoadToStageStep.MoveFeederStageLoadPosition:
                        return MoveFeederStageLoadPositionAsync(ct);
                    // 웨이퍼 전 이송 검증
                    case InputFeederLoadToStageStep.VerifyWaferBeforeTransfer:
                        return VerifyWaferBeforeTransferAsync(ct);
                    // 스테이지 진공 ON 처리
                    case InputFeederLoadToStageStep.StageVacuumOn:
                        return Task.FromResult(StageVacuumOn());
                    // 피더 언클램프 준비
                    case InputFeederLoadToStageStep.PrepareFeederUnclamp:
                        return PrepareFeederUnclampAsync(ct);
                    // 피더 스테이지 로드 어보이드 위치 이동
                    case InputFeederLoadToStageStep.MoveFeederStageLoadAvoidPosition:
                        return MoveFeederStageLoadAvoidPositionAsync(ct);
                    // 자재 데이터를 스테이지로 이동
                    case InputFeederLoadToStageStep.MoveMaterialDataToStage:
                        return Task.FromResult(MoveMaterialDataToStage());
                    // 피더 데이터 클리어
                    case InputFeederLoadToStageStep.ClearFeederData:
                        return Task.FromResult(ClearFeederData());
                    // 피더 리프트 업 준비
                    case InputFeederLoadToStageStep.PrepareFeederLiftUp:
                        return PrepareFeederLiftUpAsync(ct);
                    // 피더 어보이드 위치 이동
                    case InputFeederLoadToStageStep.MoveFeederAvoidPosition:
                        return MoveFeederAvoidPositionAsync(ct);
                    // 피더 리프트 다운 후 어보이드 준비
                    case InputFeederLoadToStageStep.PrepareFeederLiftDownAfterAvoid:
                        return PrepareFeederLiftDownAsync(ct, InputFeederLoadToStageStep.VerifyInputStageData);
                    // 인풋 스테이지 데이터 검증
                    case InputFeederLoadToStageStep.VerifyInputStageData:
                        return Task.FromResult(VerifyInputStageData());
                    // Stage 적재와 Feeder Avoid/Down 확인 후 InputCameraX에서 바코드 판독
                    case InputFeederLoadToStageStep.RunBarcodeSequence:
                        return RunBarcodeSequenceAsync(ct);
                    // 인풋 스테이지 공정 위치 이동
                    case InputFeederLoadToStageStep.MoveInputStageProcessPosition:
                        return MoveInputStageProcessPositionAsync(ct);
                    // 인풋 카세트 AVOID 이동
                    case InputFeederLoadToStageStep.MoveInputCassetteAvoidPosition:
                        return MoveInputCassetteAvoidPositionAsync(ct);
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
                return Task.FromResult(Fail("IN-FEEDER-STAGE-LOAD-STEP-EX", "InputFeederLoadToStageSequence", "InputFeeder -> InputStage 로딩 스텝 예외. error=" + ex.Message));
            }
            finally
            {
            }
        }

        private int CheckTransferReady()
        {
            string readyReason;
            if (!Feeder.CheckWaferStageReady(Options.WaferSize, TransferMode.Load, out readyReason))
                return Fail("IN-FEEDER-STAGE-READY", Feeder.Name, "Input feeder to stage transfer condition is not ready. " + readyReason);

            if (!CheckLoadToStageTeachingReady(out readyReason))
                return Fail("IN-FEEDER-STAGE-TEACHING", Feeder.Name, "Input feeder to stage teaching data is not ready. " + readyReason);

            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-WAFER-DATA", "Material", "Input feeder wafer data was not found.");

            InputStageUnit stage = Context.Machine != null ? Context.Machine.InputStageUnit : null;
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit is not available.");

            if (!IsInputStageEmpty(stage))
                return Fail("IN-FEEDER-STAGE-OCCUPIED", stage.Name, "Input stage must be empty before feeder to stage load.");

            CurrentStep = InputFeederLoadToStageStep.CheckStageLoadPosition;
            return 0;
        }

        private bool CheckLoadToStageTeachingReady(out string reason)
        {
            reason = string.Empty;
            if (Feeder == null || Feeder.Recipe == null)
            {
                reason = "Input feeder recipe is not available.";
                return false;
            }

            double stageLoad = Feeder.Recipe.WaferLoadPosition;
            double stageLoadAvoid = Feeder.Recipe.WaferLoadAvoidPosition;
            double tolerance = Feeder.FeederY != null && Feeder.FeederY.Config != null && Feeder.FeederY.Config.InPositionTolerance > 0.0
                ? Feeder.FeederY.Config.InPositionTolerance
                : 0.01;

            if (Math.Abs(stageLoad - stageLoadAvoid) <= tolerance)
            {
                reason = "WaferLoadPosition equals WaferLoadAvoidPosition. WaferLoad=" + stageLoad +
                         ", WaferLoadAvoid=" + stageLoadAvoid +
                         ", tolerance=" + tolerance;
                return false;
            }

            if (!IsFeederYTargetInSoftLimit(stageLoad))
            {
                reason = "WaferLoadPosition is out of FeederY soft limit. target=" + stageLoad + ". " + BuildFeederYSoftLimitState();
                return false;
            }

            if (!IsFeederYTargetInSoftLimit(stageLoadAvoid))
            {
                reason = "WaferLoadAvoidPosition is out of FeederY soft limit. target=" + stageLoadAvoid + ". " + BuildFeederYSoftLimitState();
                return false;
            }

            return true;
        }

        private async Task<int> CheckStageLoadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            InputStageUnit stage = ResolveStage();
            if (stage == null || stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available.");

            int result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferY, stage.Recipe.WaferY.LoadPosition, "StageY load");
            if (result != 0) return result;

            result = CheckStageAxisInPosition(stage, WaferStageAxis.WaferT, stage.Recipe.WaferT.LoadPosition, "StageT load");
            if (result != 0) return result;

            // ExpanderZ(StageZ)는 로딩/언로딩 구간에서만 올라간다. Ready 안전 복구가 정지 중 ExpanderZ를
            // Avoid로 내린 뒤 로딩을 재개하는 경우가 있으므로, 확인 실패로 중단하는 대신 Load 위치로
            // 복원 이동한다. 안전 전제: 위에서 StageY/StageT가 Load(고정) 위치임을 확인했고,
            // Load 위치 상승은 LoadFromCassette의 원래 스테이지 준비 동작(StageZ load 이동)과 동일한
            // 티칭 지점 이동이다. 이미 Load 위치이면 이동 없이 통과한다.
            result = await MoveStageAxisAndVerifyAsync(
                stage,
                WaferStageAxis.WaferExpandingZ,
                stage.Recipe.WaferZ.LoadPosition,
                "StageZ load",
                ct).ConfigureAwait(false);
            if (result != 0) return result;

            CurrentStep = InputFeederLoadToStageStep.VerifyFeederHoldingWafer;
            return 0;
        }

        private async Task<int> VerifyFeederHoldingWaferAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (!Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-LIFT-DOWN-CHECK", Feeder.Name,
                    "InputFeeder -> InputStage 로딩 전 WaferFeeder Lift가 Down 상태여야 합니다. " + Feeder.GetWaferFeederTransferState());

            if (!Feeder.IsWaferFeederClamp())
            {
                WaferMaterial wafer = ResolveFeederWafer();
                if (wafer == null)
                {
                    return Fail("IN-FEEDER-CLAMP-CHECK", Feeder.Name,
                        "InputFeeder -> InputStage 로딩 전 WaferFeeder가 Clamp 상태여야 하지만, Feeder 웨이퍼 데이터가 없습니다. " +
                        Feeder.GetWaferFeederTransferState());
                }

                if (!IsHardwareBypass() && !Feeder.IsWaferFeederRingDetected(Options.WaferSize, true))
                {
                    return Fail("IN-FEEDER-CLAMP-CHECK", Feeder.Name,
                        "InputFeeder -> InputStage 로딩 전 WaferFeeder가 Clamp 상태여야 하지만, 웨이퍼 감지 신호가 없습니다. waferId=" +
                        wafer.WaferId + ". " + Feeder.GetWaferFeederTransferState());
                }

                int clampResult = await AwaitStepWithCancellationAsync(
                    Feeder.SetWaferFeederClampAsync(true, ResolveTimeout(), ct),
                    ct).ConfigureAwait(false);
                if (clampResult != 0 || !Feeder.IsWaferFeederClamp())
                {
                    return Fail("IN-FEEDER-CLAMP-CHECK", Feeder.Name,
                        "InputFeeder -> InputStage 로딩 전 WaferFeeder Clamp 복구 실패. result=" +
                        clampResult + ". " + Feeder.GetWaferFeederTransferState());
                }
            }

            CurrentStep = InputFeederLoadToStageStep.MoveFeederStageLoadPosition;
            return 0;
        }

        private async Task<int> PrepareFeederLiftDownAsync(CancellationToken ct, InputFeederLoadToStageStep nextStep)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederUpDownAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederDown())
                return Fail("IN-FEEDER-LIFT-DOWN", Feeder.Name,
                    "WaferFeeder lift down command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = nextStep;
            return 0;
        }

        private async Task<int> MoveFeederStageLoadPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederStageLoadPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-LOAD-MOVE", Feeder.Name,
                    "WaferFeeder stage load position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInStageLoadPosition(),
                "WaferFeeder stage load position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederLoadToStageStep.VerifyWaferBeforeTransfer;
            return 0;
        }

        private async Task<int> VerifyWaferBeforeTransferAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-WAFER-DATA-MISSING", "Material", "Input feeder wafer data disappeared before stage transfer.");

            if (!IsHardwareBypass() && !Feeder.IsWaferFeederRingDetected(Options.WaferSize, true))
            {
                bool detected = await Feeder.WaitWaferFeederRingState(true, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!detected)
                    return Fail("IN-FEEDER-WAFER-SENSOR", Feeder.Name, "Wafer sensor timeout or data/sensor mismatch before feeder to stage transfer. waferId=" + wafer.WaferId);
            }

            CurrentStep = InputFeederLoadToStageStep.StageVacuumOn;
            return 0;
        }

        private int StageVacuumOn()
        {
            InputStageUnit stage = ResolveStage();
            if (stage != null && stage.NeedleVacuum != null && Options.UseVacuum)
                stage.NeedleVacuum.On();

            CurrentStep = InputFeederLoadToStageStep.PrepareFeederUnclamp;
            return 0;
        }

        private async Task<int> PrepareFeederUnclampAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederClampAsync(false, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederUnclamp())
                return Fail("IN-FEEDER-UNCLAMP", Feeder.Name,
                    "WaferFeeder unclamp command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederLoadToStageStep.MoveFeederStageLoadAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederStageLoadAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederStageLoadAvoidPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-STAGE-LOAD-AVOID-MOVE", Feeder.Name,
                    "WaferFeeder stage load avoid position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInStageLoadAvoidPosition(),
                "WaferFeeder stage load avoid position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!IsHardwareBypass())
            {
                bool cleared = await Feeder.WaitWaferFeederRingState(false, ResolveTimeout(), ct).ConfigureAwait(false);
                if (!cleared)
                    return Fail("IN-FEEDER-STAGE-TRANSFER-SENSOR", Feeder.Name, "WaferFeeder ring remained after feeder stage load avoid move.");
            }

            CurrentStep = InputFeederLoadToStageStep.MoveMaterialDataToStage;
            return 0;
        }

        private int MoveMaterialDataToStage()
        {
            WaferMaterial wafer = ResolveFeederWafer();
            if (wafer == null)
                return Fail("IN-FEEDER-MATERIAL-MOVE", "Material", "Input feeder wafer data was not found for stage material move.");

            if (!string.IsNullOrWhiteSpace(Options.ExpectedWaferId) &&
                !string.Equals(Options.ExpectedWaferId, wafer.WaferId, StringComparison.OrdinalIgnoreCase))
                return Fail("IN-FEEDER-MATERIAL-WAFER", "Material", "물리 이송 후 InputStage Material 갱신 직전에 Wafer ID가 변경되었습니다. expected=" + Options.ExpectedWaferId + ", actual=" + wafer.WaferId);

            if (wafer.SourceCassetteRole != Options.CassetteRole || wafer.SourceSlotNumber != Options.SlotIndex)
                return Fail("IN-FEEDER-MATERIAL-SOURCE", "Material", "Input wafer의 원본 cassette/slot 정보가 sequence option과 다릅니다. wafer=" + wafer.WaferId +
                    ", sourceRole=" + wafer.SourceCassetteRole + ", sourceSlot=" + (wafer.SourceSlotNumber + 1).ToString("00") +
                    ", optionRole=" + Options.CassetteRole + ", optionSlot=" + (Options.SlotIndex + 1).ToString("00"));

            // 현재 기준: 새 wafer를 Stage에 올리기 전 이전 Input active map을 지워 stale map 표시/재사용을 막는다.
            if (Context != null && Context.Controller != null)
                Context.Controller.ClearInputDieMap("InputFeederLoadToStageSequence.MoveMaterialDataToStage");
            else
                LotStorage.ActiveInputDieMap = null;

            MaterialStateService.MoveWaferToInputStage(wafer);

            InputStageUnit stage = ResolveStage();
            if (stage != null)
                stage.SetCurrentWaferMaterial(MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage));

            CurrentStep = InputFeederLoadToStageStep.ClearFeederData;
            return 0;
        }

        private int ClearFeederData()
        {
            Feeder.ClearCurrentWaferMaterial();
            Context.Bus.Set("InputFeederEmpty");
            Context.Bus.Set("InputStageOccupied");
            CurrentStep = InputFeederLoadToStageStep.PrepareFeederLiftUp;
            return 0;
        }

        private async Task<int> PrepareFeederLiftUpAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.SetWaferFeederUpDownAsync(true, ResolveTimeout(), ct),
                ct).ConfigureAwait(false);
            if (result != 0 || !Feeder.IsWaferFeederUp())
                return Fail("IN-FEEDER-LIFT-UP", Feeder.Name,
                    "WaferFeeder lift up command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            CurrentStep = InputFeederLoadToStageStep.MoveFeederAvoidPosition;
            return 0;
        }

        private async Task<int> MoveFeederAvoidPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            int result = await AwaitStepWithCancellationAsync(
                Feeder.MoveToWaferFeederAvoidPosition(Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("IN-FEEDER-AVOID-MOVE", Feeder.Name,
                    "WaferFeeder avoid position move command failed. result=" + result + ". " + Feeder.GetWaferFeederTransferState());

            result = await WaitFeederYDoneAsync(
                () => Feeder.IsWaferFeederInAvoidPosition(),
                "WaferFeeder avoid position",
                ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            CurrentStep = InputFeederLoadToStageStep.PrepareFeederLiftDownAfterAvoid;
            return 0;
        }

        private int VerifyInputStageData()
        {
            InputStageUnit stage = ResolveStage();
            WaferMaterial stageWafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (stageWafer == null || stage == null || stage.CurrentWaferMaterial == null)
                return Fail("IN-FEEDER-STAGE-DATA", "Material", "InputStage wafer data was not found after feeder to stage transfer.");

            if (Feeder.CurrentWaferMaterial != null || MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder) != null)
                return Fail("IN-FEEDER-DATA-CLEAR", "Material", "InputFeeder wafer data remained after feeder to stage transfer.");

            CurrentStep = InputFeederLoadToStageStep.RunBarcodeSequence;
            return 0;
        }

        private async Task<int> RunBarcodeSequenceAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (Options == null || !Options.UseBarcode)
                {
                    CurrentStep = InputFeederLoadToStageStep.MoveInputStageProcessPosition;
                    return 0;
                }

                InputStageUnit stage = ResolveStage();
                if (stage == null || stage.Recipe == null || stage.CameraX == null ||
                    stage.StageY == null || stage.ExpanderZ == null)
                {
                    return Fail("IN-BARCODE-STAGE-MISSING", "InputStage",
                        "Input Wafer barcode 판독에 필요한 InputStage/CameraX/StageY/ExpanderZ를 확인할 수 없습니다.");
                }

                stage.Recipe.EnsurePositionObjects();
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
                if (wafer == null || stage.CurrentWaferMaterial == null ||
                    !MaterialStateService.IsSameWaferInstance(wafer, stage.CurrentWaferMaterial))
                {
                    return Fail("IN-BARCODE-MATERIAL-MISSING", "Material",
                        "Input Wafer barcode 판독 전 InputStage Material 데이터가 없거나 Unit 데이터와 일치하지 않습니다.");
                }

                // 재개 시 이미 같은 물리 Wafer에 판독값이 적용되어 있으면 다시 읽지 않는다.
                if (wafer.BarcodeConfirmed && IsUsableBarcode(wafer.BarcodeId))
                {
                    int resumeAvoid = await EnsureInputVisionAvoidAfterBarcodeAsync(
                        stage,
                        "barcode-confirmed resume",
                        ct).ConfigureAwait(false);
                    if (resumeAvoid != 0)
                        return resumeAvoid;

                    Options.ExpectedWaferId = wafer.WaferId ?? string.Empty;
                    WriteLog(Name,
                        "Input Wafer barcode already confirmed. scan skipped. waferInstanceId=" +
                        wafer.WaferInstanceId + ", barcode=" + wafer.BarcodeId + " - Ok");
                    CurrentStep = InputFeederLoadToStageStep.MoveInputStageProcessPosition;
                    return 0;
                }

                AppSettings settings = AppSettingsStore.Current ?? new AppSettings();
                int readTimeoutMs = settings.InputBarcodeReadTimeoutMs > 0
                    ? settings.InputBarcodeReadTimeoutMs
                    : 3000;
                int retryCount = Math.Max(0, settings.InputBarcodeRetryCount);
                double retryStepMm = Math.Abs(settings.InputBarcodeRetryStepMm);
                int totalAttempts = 0;
                string lastFailure = string.Empty;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    IBarcodeReader reader = Context != null && Context.Machine != null
                        ? Context.Machine.WaferBarcodeReader
                        : null;
                    string readerFailure;
                    if (!TryEnsureInputBarcodeReaderReady(reader, out readerFailure))
                    {
                        // Reader가 연결되지 않은 경우에는 어떤 축도 움직이지 않고 작업자 복구를 받는다.
                        BarcodeRecoveryResponse unavailableResponse = await RequestInputBarcodeRecoveryAsync(
                            wafer,
                            readerFailure,
                            retryCount,
                            retryStepMm,
                            ct).ConfigureAwait(false);
                        ApplyRecoveryRetryParameters(unavailableResponse, ref retryCount, ref retryStepMm);

                        if (unavailableResponse != null &&
                            unavailableResponse.Decision == BarcodeRecoveryDecision.ManualApply)
                        {
                            return await ApplyInputBarcodeAndFinishAsync(
                                stage,
                                wafer,
                                unavailableResponse.ManualBarcode,
                                totalAttempts,
                                "Manual",
                                ct).ConfigureAwait(false);
                        }

                        if (unavailableResponse == null ||
                            unavailableResponse.Decision == BarcodeRecoveryDecision.Cancelled)
                        {
                            ct.ThrowIfCancellationRequested();
                            return await FailInputBarcodeWithAvoidRecoveryAsync(
                                stage,
                                "IN-BARCODE-RECOVERY-CANCELLED",
                                "Input Wafer barcode Reader 연결 복구 Dialog를 완료하지 못했습니다. " + readerFailure).ConfigureAwait(false);
                        }

                        // Dialog X/CLOSE도 서비스에서 Retry로 변환되므로 Reader 연결 확인부터 반복한다.
                        continue;
                    }

                    int feederSafe = VerifyFeederSafeForInputBarcodeMotion();
                    if (feederSafe != 0)
                        return feederSafe;

                    double stageBaseTarget = stage.Recipe.WaferY.BarcodePosition;
                    string targetReason;
                    if (!ValidateInputBarcodeStageYTargets(
                        stage.StageY,
                        stageBaseTarget,
                        retryCount,
                        retryStepMm,
                        out targetReason))
                    {
                        return Fail("IN-BARCODE-STAGE-Y-TEACH", stage.Name,
                            "Input Wafer barcode StageY teaching/retry 범위가 유효하지 않습니다. " + targetReason);
                    }

                    // StageY/CameraX 평면 이동 인터락을 유지하기 위해 ExpanderZ를 기존 Process 높이로 먼저 내린다.
                    double zTarget = stage.Recipe.WaferZ.ProcessPosition;
                    if (!IsInputStageAxisReadyAt(stage, WaferStageAxis.WaferExpandingZ, zTarget))
                    {
                        int zMove = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxis(WaferStageAxis.WaferExpandingZ, zTarget, Options.FineMove),
                            ct).ConfigureAwait(false);
                        if (zMove != 0 || !IsInputStageAxisReadyAt(stage, WaferStageAxis.WaferExpandingZ, zTarget))
                        {
                            return await FailInputBarcodeWithAvoidRecoveryAsync(
                                stage,
                                "IN-BARCODE-STAGE-Z",
                                "Input Wafer barcode 전 ExpanderZ Process 위치 이동/확인 실패. result=" + zMove +
                                ", " + BuildStageAxisState(stage, WaferStageAxis.WaferExpandingZ, zTarget)).ConfigureAwait(false);
                        }
                    }

                    double visionTarget = stage.Recipe.VisionX.BarcodePosition;
                    if (!IsInputStageAxisReadyAt(stage, WaferStageAxis.VisionX, visionTarget))
                    {
                        int visionMove = await AwaitStepWithCancellationAsync(
                            stage.MoveInputStageAxis(WaferStageAxis.VisionX, visionTarget, Options.FineMove),
                            ct).ConfigureAwait(false);
                        if (visionMove != 0 || !IsInputStageAxisReadyAt(stage, WaferStageAxis.VisionX, visionTarget))
                        {
                            return await FailInputBarcodeWithAvoidRecoveryAsync(
                                stage,
                                "IN-BARCODE-VISION-X",
                                "InputCameraX barcode 위치 이동/확인 실패. result=" + visionMove +
                                ", " + BuildStageAxisState(stage, WaferStageAxis.VisionX, visionTarget)).ConfigureAwait(false);
                        }
                    }

                    string barcode = string.Empty;
                    int scanCount = retryCount + 1;
                    for (int attemptIndex = 0; attemptIndex < scanCount; attemptIndex++)
                    {
                        ct.ThrowIfCancellationRequested();

                        double offset = ResolveBarcodeRetryOffset(attemptIndex, retryStepMm);
                        double stageTarget = stageBaseTarget + offset;
                        if (!IsInputStageAxisReadyAt(stage, WaferStageAxis.WaferY, stageTarget))
                        {
                            int stageMove = await AwaitStepWithCancellationAsync(
                                stage.MoveInputStageAxis(WaferStageAxis.WaferY, stageTarget, Options.FineMove),
                                ct).ConfigureAwait(false);
                            if (stageMove != 0 || !IsInputStageAxisReadyAt(stage, WaferStageAxis.WaferY, stageTarget))
                            {
                                return await FailInputBarcodeWithAvoidRecoveryAsync(
                                    stage,
                                    "IN-BARCODE-STAGE-Y",
                                    "Input Wafer barcode StageY 이동/확인 실패. attempt=" + (attemptIndex + 1) +
                                    ", offset=" + offset.ToString("F3") +
                                    ", result=" + stageMove + ", " +
                                    BuildStageAxisState(stage, WaferStageAxis.WaferY, stageTarget)).ConfigureAwait(false);
                            }
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
                        return await ApplyInputBarcodeAndFinishAsync(
                            stage,
                            wafer,
                            barcode,
                            totalAttempts,
                            "Reader",
                            ct).ConfigureAwait(false);
                    }

                    // 작업자 응답을 기다리는 동안 CameraX가 작업 영역에 남지 않도록 먼저 실제 Avoid를 확인한다.
                    int promptAvoid = await EnsureInputVisionAvoidAfterBarcodeAsync(
                        stage,
                        "barcode read failed before operator recovery",
                        ct).ConfigureAwait(false);
                    if (promptAvoid != 0)
                        return promptAvoid;

                    if (string.IsNullOrWhiteSpace(lastFailure))
                        lastFailure = "설정된 재시도 횟수 안에 Input Wafer 바코드를 읽지 못했습니다.";

                    BarcodeRecoveryResponse response = await RequestInputBarcodeRecoveryAsync(
                        wafer,
                        lastFailure,
                        retryCount,
                        retryStepMm,
                        ct).ConfigureAwait(false);
                    ApplyRecoveryRetryParameters(response, ref retryCount, ref retryStepMm);

                    if (response != null && response.Decision == BarcodeRecoveryDecision.ManualApply)
                    {
                        return await ApplyInputBarcodeAndFinishAsync(
                            stage,
                            wafer,
                            response.ManualBarcode,
                            totalAttempts,
                            "Manual",
                            ct).ConfigureAwait(false);
                    }

                    if (response == null || response.Decision == BarcodeRecoveryDecision.Cancelled)
                    {
                        ct.ThrowIfCancellationRequested();
                        return await FailInputBarcodeWithAvoidRecoveryAsync(
                            stage,
                            "IN-BARCODE-RECOVERY-CANCELLED",
                            "Input Wafer barcode 판독 복구 Dialog를 완료하지 못했습니다. " + lastFailure).ConfigureAwait(false);
                    }

                    // Retry는 CameraX 진입 전 Reader 상태 확인부터 다시 수행한다.
                }
            }
            catch (OperationCanceledException)
            {
                InputStageUnit stage = ResolveStage();
                string avoidFailure = await TryRestoreInputVisionAvoidBestEffortAsync(
                    stage,
                    "barcode cancellation").ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(avoidFailure))
                {
                    return Fail("IN-BARCODE-CANCEL-RECOVERY", Name,
                        "Input Wafer barcode 취소 후 InputCameraX Avoid 복귀에 실패했습니다. " +
                        "RecoveryRequired. " + avoidFailure);
                }
                throw;
            }
            catch (Exception ex)
            {
                return await FailInputBarcodeWithAvoidRecoveryAsync(
                    ResolveStage(),
                    "IN-BARCODE-EX",
                    "Input Wafer barcode sequence exception. error=" + ex.Message).ConfigureAwait(false);
            }
            finally
            {
            }
        }

        private int VerifyFeederSafeForInputBarcodeMotion()
        {
            if (Feeder == null || Feeder.FeederY == null ||
                Feeder.FeederY.IsMoving ||
                !Feeder.IsWaferFeederAvoidPositionCheck() ||
                !Feeder.IsWaferFeederDown())
            {
                return Fail("IN-BARCODE-FEEDER-SAFE", Feeder != null ? Feeder.Name : "InputFeeder",
                    "InputCameraX/StageY barcode 이동 전 InputFeeder가 정지된 Avoid/Lift Down 상태가 아닙니다. " +
                    (Feeder != null ? Feeder.GetWaferFeederTransferState() : "InputFeeder=null"));
            }

            return 0;
        }

        private bool IsInputStageAxisReadyAt(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return false;

            item.UpdateStatus();

            // ============================================================================
            // [INP 요구 제거 2026-08-05] Output 측 OUT-BARCODE-VISION-AVOID 와 동일한 결함.
            //
            // 기존 조건: ... && item.IsInPosition && IsStageAxisInPosition(item, target)
            //
            // IsInPosition 의 의미가 시뮬과 실보드에서 다르다.
            //   · 실보드(AjinAxis)  : 드라이브 INP 하드웨어 신호(AXM.GetInPositionValue / uMechSig 0x20).
            //                        정지·정착 상태면 상시 true. 이동 이력과 무관하다.
            //   · 노트북 시뮬(BaseAxis): 이동 완료 시에만 true, Stop() 에서 false, 초기값 false.
            //                        ★한 번도 이동 명령을 안 받은 축은 위치가 맞아도 영구 false★
            //
            // 바코드 리더 부재/수동 입력으로 InputCameraX 가 이동하지 않으면 INP=false 이고,
            // MoveStageAxis 는 이미 목표라 이동을 생략(return 0)하므로 INP 가 세워질 기회가 없다.
            // → IN-BARCODE-VISION-AVOID 가 RUN 을 다시 눌러도 계속 발생한다.
            //
            // 현재 기준: 도달 판정은 위치 기준으로 통일한다(IsAtTargetPosition 이 servo/alarm/moving 과
            //           Actual/Command 양쪽 톨러런스를 이미 확인하므로 안전 강도는 유지된다).
            //           실보드에서는 INP 도 true 이므로 동작 변화가 없다.
            //           INP=false 인데 위치는 맞는 경우는 AXIS-INP-MISSING 로그로만 남긴다.
            // ============================================================================
            bool arrived = item.IsServoOn && !item.IsAlarm && !item.IsMoving &&
                IsStageAxisInPosition(item, target);

            if (arrived && !item.IsInPosition)
                OutputFeederLoadToStageSequence.LogAxisArrivedWithoutInPosition(
                    item, axis.ToString(), target);

            return arrived;
        }

        private async Task<int> ApplyInputBarcodeAndFinishAsync(
            InputStageUnit stage,
            WaferMaterial expectedWafer,
            string barcode,
            int attempts,
            string sourceKind,
            CancellationToken ct)
        {
            string normalized = NormalizeBarcode(barcode);
            if (!IsUsableBarcode(normalized))
                return Fail("IN-BARCODE-MANUAL-INVALID", "Barcode",
                    "입력된 Input Wafer 바코드가 비어 있거나 유효하지 않습니다.");

            string previousWaferId;
            string applyReason;
            bool applied = MaterialStateService.TryApplyWaferBarcode(
                expectedWafer.WaferInstanceId,
                MaterialLocationKind.InputStage,
                normalized,
                Name + ":" + sourceKind,
                attempts,
                out previousWaferId,
                out applyReason);
            if (!applied)
            {
                return await FailInputBarcodeWithAvoidRecoveryAsync(
                    stage,
                    "IN-BARCODE-MATERIAL-APPLY",
                    "Input Wafer barcode Material 갱신 실패. waferInstanceId=" + expectedWafer.WaferInstanceId +
                    ", barcode=" + normalized + ", reason=" + applyReason).ConfigureAwait(false);
            }

            WaferMaterial refreshed = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
            if (refreshed == null ||
                !string.Equals(refreshed.WaferInstanceId, expectedWafer.WaferInstanceId, StringComparison.OrdinalIgnoreCase) ||
                !refreshed.BarcodeConfirmed ||
                !string.Equals(refreshed.WaferId, normalized, StringComparison.Ordinal))
            {
                return await FailInputBarcodeWithAvoidRecoveryAsync(
                    stage,
                    "IN-BARCODE-MATERIAL-CHECK",
                    "Input Wafer barcode Material 최종 확인 실패. expectedInstanceId=" + expectedWafer.WaferInstanceId +
                    ", expectedBarcode=" + normalized +
                    ", actualWafer=" + (refreshed != null ? refreshed.WaferId : "null")).ConfigureAwait(false);
            }

            stage.SetCurrentWaferMaterial(refreshed);
            Options.ExpectedWaferId = refreshed.WaferId ?? string.Empty;
            int avoidResult = await EnsureInputVisionAvoidAfterBarcodeAsync(
                stage,
                "barcode apply completed",
                ct).ConfigureAwait(false);
            if (avoidResult != 0)
                return avoidResult;

            WriteLog(Name,
                "Input Wafer barcode applied. waferInstanceId=" + refreshed.WaferInstanceId +
                ", previousWaferId=" + previousWaferId +
                ", barcode=" + refreshed.WaferId +
                ", attempts=" + attempts +
                ", source=" + sourceKind + " - Ok");
            CurrentStep = InputFeederLoadToStageStep.MoveInputStageProcessPosition;
            return 0;
        }

        private async Task<int> EnsureInputVisionAvoidAfterBarcodeAsync(
            InputStageUnit stage,
            string context,
            CancellationToken ct)
        {
            if (stage == null || stage.CameraX == null || stage.Recipe == null)
                return Fail("IN-BARCODE-VISION-AVOID-MISSING", "InputStage",
                    "Input Wafer barcode 후 InputCameraX Avoid 확인에 필요한 축/레시피가 없습니다. context=" + context);

            stage.Recipe.EnsurePositionObjects();
            double target = stage.Recipe.VisionX.AvoidPosition;
            if (IsInputStageAxisReadyAt(stage, WaferStageAxis.VisionX, target) && stage.IsVisionXInAvoidPosition())
                return 0;

            int feederSafe = VerifyFeederSafeForInputBarcodeMotion();
            if (feederSafe != 0)
                return feederSafe;

            int result = await AwaitStepWithCancellationAsync(
                stage.MoveInputStageAxis(WaferStageAxis.VisionX, target, Options != null && Options.FineMove),
                ct).ConfigureAwait(false);
            if (result != 0 ||
                !IsInputStageAxisReadyAt(stage, WaferStageAxis.VisionX, target) ||
                !stage.IsVisionXInAvoidPosition())
            {
                return Fail("IN-BARCODE-VISION-AVOID", stage.Name,
                    "Input Wafer barcode 후 InputCameraX Avoid 복귀/확인 실패. context=" + context +
                    ", result=" + result + ", " + BuildStageAxisState(stage, WaferStageAxis.VisionX, target));
            }

            return 0;
        }

        private async Task<int> FailInputBarcodeWithAvoidRecoveryAsync(
            InputStageUnit stage,
            string alarmCode,
            string message)
        {
            string avoidFailure = await TryRestoreInputVisionAvoidBestEffortAsync(stage, message).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(avoidFailure))
                message += " | RecoveryRequired: InputCameraX Avoid 복귀 실패. " + avoidFailure;
            return Fail(alarmCode, Name, message);
        }

        private async Task<string> TryRestoreInputVisionAvoidBestEffortAsync(
            InputStageUnit stage,
            string context)
        {
            try
            {
                if (stage == null || stage.CameraX == null || stage.Recipe == null)
                    return "InputStage/InputCameraX/Recipe unavailable.";

                stage.Recipe.EnsurePositionObjects();
                double target = stage.Recipe.VisionX.AvoidPosition;
                if (IsInputStageAxisReadyAt(stage, WaferStageAxis.VisionX, target) && stage.IsVisionXInAvoidPosition())
                    return string.Empty;

                int cleanupTimeoutMs = Math.Max(1000, Math.Min(ResolveTimeout(), 30000));
                using (var cleanupCts = new CancellationTokenSource())
                {
                    cleanupCts.CancelAfter(cleanupTimeoutMs);
                    int result = await AwaitStepWithCancellationAsync(
                        stage.MoveInputStageAxis(
                            WaferStageAxis.VisionX,
                            target,
                            Options != null && Options.FineMove),
                        cleanupCts.Token).ConfigureAwait(false);
                    if (result == 0 &&
                        IsInputStageAxisReadyAt(stage, WaferStageAxis.VisionX, target) &&
                        stage.IsVisionXInAvoidPosition())
                    {
                        return string.Empty;
                    }

                    return "context=" + context + ", result=" + result + ", " +
                        BuildStageAxisState(stage, WaferStageAxis.VisionX, target);
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

        private async Task<BarcodeRecoveryResponse> RequestInputBarcodeRecoveryAsync(
            WaferMaterial wafer,
            string failureMessage,
            int retryCount,
            double retryStepMm,
            CancellationToken ct)
        {
            var request = new BarcodeRecoveryRequest
            {
                Channel = BarcodeReaderChannel.InputWafer,
                MaterialId = wafer != null ? wafer.WaferId : string.Empty,
                MaterialInstanceId = wafer != null ? wafer.WaferInstanceId : string.Empty,
                FailureMessage = failureMessage ?? string.Empty,
                RetryCount = retryCount,
                RetryStepMm = retryStepMm
            };
            return await BarcodeOperatorPromptService.RequestAsync(request, ct).ConfigureAwait(false);
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

        private static bool TryEnsureInputBarcodeReaderReady(IBarcodeReader reader, out string reason)
        {
            reason = string.Empty;
            if (reader == null)
            {
                reason = "Input Wafer barcode reader가 구성되지 않았습니다.";
                return false;
            }

            try
            {
                if (reader.IsConnected)
                    return true;
                if (reader.TryOpen() && reader.IsConnected)
                    return true;

                reason = "Input Wafer barcode reader 연결에 실패했습니다. reader=" + reader.ReaderName;
                return false;
            }
            catch (Exception ex)
            {
                reason = "Input Wafer barcode reader 연결 예외. reader=" + reader.ReaderName +
                    ", error=" + ex.Message;
                return false;
            }
        }

        private static bool ValidateInputBarcodeStageYTargets(
            BaseAxis stageY,
            double baseTarget,
            int retryCount,
            double retryStepMm,
            out string reason)
        {
            reason = string.Empty;
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

        private static double ResolveBarcodeRetryOffset(int attemptIndex, double retryStepMm)
        {
            if (attemptIndex <= 0 || retryStepMm <= 0.0)
                return 0.0;

            int distanceMultiplier = (attemptIndex + 1) / 2;
            double sign = attemptIndex % 2 == 1 ? 1.0 : -1.0;
            return sign * distanceMultiplier * retryStepMm;
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

        private static bool IsUsableBarcode(string value)
        {
            string normalized = NormalizeBarcode(value);
            return !string.IsNullOrWhiteSpace(normalized) &&
                !string.Equals(normalized, "WAFER-NULL-ID", StringComparison.OrdinalIgnoreCase);
        }

        //MoveInputCassetteAvoidPositionAsync
        private async Task<int> MoveInputCassetteAvoidPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputCassetteUnit cassette =
                    Context.Machine != null
                        ? Context.Machine.InputCassetteUnit
                        : null;

                if (cassette == null ||
                    cassette.InputLifterZ == null ||
                    cassette.Recipe == null)
                {
                    return Fail(
                        "IN-FEEDER-CST-MISSING",
                        "InputCassette",
                        "Input cassette unit, axis, or recipe is not available.");
                }

                // 기구 간섭 방지:
                // InputCassette 이동 전 InputFeeder가 정지된 실제 Avoid 위치여야 한다.
                if (Feeder == null ||
                    Feeder.FeederY == null ||
                    Feeder.FeederY.IsMoving ||
                    !Feeder.IsWaferFeederAvoidPositionCheck())
                {
                    return Fail(
                        "IN-FEEDER-CST-AVOID-INTERLOCK",
                        cassette.Name,
                        "InputCassette Avoid 이동 불가: " +
                        "InputFeeder가 정지된 Avoid 위치가 아닙니다. " +
                        (Feeder != null
                            ? Feeder.GetWaferFeederTransferState()
                            : "InputFeeder=null"));
                }

                double target = cassette.Recipe.AvoidPosition;
                int result = await AwaitStepWithCancellationAsync(
                    cassette.MoveWaferLifterZ(
                        target,
                        Options != null && Options.FineMove,
                        ct),
                    ct).ConfigureAwait(false);

                if (result != 0)
                {
                    return Fail(
                        "IN-FEEDER-CST-AVOID-MOVE",
                        cassette.Name,
                        "InputCassette Avoid 이동 명령 실패. result=" + result +
                        ", target=" + target +
                        ", actual=" + cassette.InputLifterZ.ActualPosition);
                }

                if (cassette.InputLifterZ.IsMoving ||
                    cassette.InputLifterZ.IsAlarm ||
                    !cassette.IsWaferLifterZInAvoidPosition())
                {
                    return Fail(
                        "IN-FEEDER-CST-AVOID-CHECK",
                        cassette.Name,
                        "InputCassette Avoid 도착 확인 실패. " +
                        "moving=" + cassette.InputLifterZ.IsMoving +
                        ", alarm=" + cassette.InputLifterZ.IsAlarm +
                        ", actual=" + cassette.InputLifterZ.ActualPosition +
                        ", target=" + target);
                }

                CurrentStep = InputFeederLoadToStageStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(
                    "IN-FEEDER-CST-AVOID-EX",
                    "InputFeederLoadToStageSequence",
                    "InputCassette Avoid 이동 중 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputStageProcessPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputStageUnit stage = ResolveStage();
                if (stage == null || stage.Recipe == null)
                    return Fail("IN-FEEDER-STAGE-MISSING", "InputStage", "Input stage unit or recipe is not available for process position move.");

                stage.Recipe.EnsurePositionObjects();

                int result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.WaferExpandingZ,
                    stage.Recipe.WaferZ.ProcessPosition,
                    "StageZ process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                ct.ThrowIfCancellationRequested();
                result = await stage.MoveNeedleWorkPointSafelyAsync(
                    stage.Recipe.NeedleX.ProcessPosition,
                    stage.Recipe.WaferY.ProcessPosition,
                    Options != null && Options.FineMove,
                    "InputFeederLoadToStageSequence.MoveInputStageProcessPositionAsync").ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.WaferT,
                    stage.Recipe.WaferT.ProcessPosition,
                    "StageT process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = VerifyNeedleXReadyForNeedleZProcess(stage);
                if (result != 0) return result;

                result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.NeedleZ,
                    stage.Recipe.NeedleZ.ProcessPosition,
                    "NeedleZ process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    WaferStageAxis.VisionX,
                    stage.Recipe.VisionX.ProcessPosition,
                    "VisionX process",
                    ct).ConfigureAwait(false);
                if (result != 0) return result;

                CurrentStep = InputFeederLoadToStageStep.MoveInputCassetteAvoidPosition;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-PROCESS-EX", "InputStage",
                    "InputStage process position move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private int VerifyNeedleXReadyForNeedleZProcess(InputStageUnit stage)
        {
            if (stage == null)
                return Fail("IN-FEEDER-STAGE-NEEDLE-X-CHECK", "InputStage",
                    "NeedleZ Process 이동 전 InputStageUnit을 찾을 수 없습니다.");

            if (stage.Recipe == null)
                return Fail("IN-FEEDER-STAGE-NEEDLE-X-CHECK", stage.Name,
                    "NeedleZ Process 이동 전 InputStage 레시피를 찾을 수 없습니다.");

            if (stage.NeedleBlockX == null || stage.StageY == null)
                return Fail("IN-FEEDER-STAGE-NEEDLE-X-CHECK", stage.Name,
                    "NeedleZ Process 이동 전 NeedleX 또는 StageY 축을 찾을 수 없습니다.");

            string areaReason;
            double needleX = stage.NeedleBlockX.ActualPosition;
            double stageY = stage.StageY.ActualPosition;
            if (stage.IsNeedleWorkPointInArea(needleX, stageY, out areaReason))
                return 0;

            return Fail("IN-FEEDER-STAGE-NEEDLE-X-TEACH", stage.Name,
                "NeedleZ Process 이동 전 NeedleX 위치가 작업 가능 영역을 벗어났습니다. " +
                "NeedleX Process 위치 티칭을 확인하세요. " +
                "needleX=" + needleX.ToString("F3") +
                ", stageY=" + stageY.ToString("F3") +
                ", needleXProcess=" + stage.Recipe.NeedleX.ProcessPosition.ToString("F3") +
                ", reason=" + areaReason);
        }

        private async Task<int> MoveStageAxisAndVerifyAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                int result = await MoveStageAxisCommandAsync(stage, axis, target, description, ct).ConfigureAwait(false);
                if (result != 0) return result;

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-PROCESS-MOVE-VERIFY-EX", stage != null ? stage.Name : "InputStage",
                    description + " 이동 확인 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveStageAxisCommandAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                BaseAxis item = ResolveStageAxis(stage, axis);
                if (item == null)
                    return Fail("IN-FEEDER-STAGE-AXIS", stage != null ? stage.Name : "InputStage",
                        description + " 축을 찾을 수 없습니다. " + BuildStageAxisState(stage, axis, target));

                string interlockReason;
                if (!MotionGuardRuntime.VerifyAxisMove(item, target, out interlockReason))
                    return Fail("IN-FEEDER-STAGE-PROCESS-INTERLOCK", stage != null ? stage.Name : "InputStage",
                        description + " 이동 인터락 차단. " + interlockReason + ". " +
                        BuildStageAxisState(stage, axis, target));

                int result = await AwaitStepWithCancellationAsync(
                    stage.MoveInputStageAxis(axis, target, Options.FineMove),
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("IN-FEEDER-STAGE-PROCESS-MOVE", stage.Name,
                        description + " 이동 명령 실패. result=" + result + ". " +
                        BuildStageAxisState(stage, axis, target));

                ct.ThrowIfCancellationRequested();
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-PROCESS-MOVE-EX", stage != null ? stage.Name : "InputStage",
                    description + " 이동 명령 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> WaitStageAxisInPositionResultAsync(InputStageUnit stage, WaferStageAxis axis, double target, string description, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int waitCode = await stage.WaitInputStageAxisInPositionResult(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
                if (waitCode != 0)
                    return Fail("IN-FEEDER-STAGE-PROCESS", stage.Name,
                        description + " 이동 완료/위치 확인 실패. waitCode=" + waitCode +
                        ". " + BuildStageAxisState(stage, axis, target));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("IN-FEEDER-STAGE-PROCESS-WAIT-EX", stage != null ? stage.Name : "InputStage",
                    description + " 이동 완료 대기 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckStageAxisInPosition(InputStageUnit stage, WaferStageAxis axis, double target, string description)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return Fail("IN-FEEDER-STAGE-AXIS", stage != null ? stage.Name : "InputStage",
                    description + " axis is not available. " + BuildStageAxisState(stage, axis, target));

            if (item.IsMoving || item.IsAlarm || !IsStageAxisInPosition(item, target))
                return Fail("IN-FEEDER-STAGE-POSITION", stage.Name,
                    description + " final position check failed after stage move/check step. " +
                    BuildStageAxisState(stage, axis, target));

            return 0;
        }

        private static bool IsStageAxisInPosition(QMC.Common.Motion.BaseAxis item, double target)
        {
            if (item == null)
                return false;

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;
            return Math.Abs(item.ActualPosition - target) <= tolerance;
        }

        private QMC.Common.Motion.BaseAxis ResolveStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                // 웨이퍼 Y축 반환
                case WaferStageAxis.WaferY: return stage.StageY;
                // 웨이퍼 T축 반환
                case WaferStageAxis.WaferT: return stage.StageT;
                // 웨이퍼 확장 Z축 반환
                case WaferStageAxis.WaferExpandingZ: return stage.ExpanderZ;
                // 비전 X축 반환
                case WaferStageAxis.VisionX: return stage.CameraX;
                // 니들 X축 반환
                case WaferStageAxis.NeedleX: return stage.NeedleBlockX;
                // 니들 Z축 반환
                case WaferStageAxis.NeedleZ: return stage.NeedleZ;
                // 이젝트 핀 Z축 반환
                case WaferStageAxis.EjectPinZ: return stage.EjectPinZ;
                default: return null;
            }
        }

        private string BuildStageAxisState(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            QMC.Common.Motion.BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return "axis=" + axis + ", target=" + target + ", state=axis-not-found";

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;

            // [진단 보강 2026-08-05] inpos/command/sim 추가 — 판정에 쓰는 값은 전부 메시지에 남긴다.
            // (Output 측 OUT-BARCODE-VISION-AVOID 가 INP 때문에 실패했는데 메시지에 INP 가 없어
            //  "전부 정상인데 실패"로 읽혔다. 같은 일이 Input 에서 반복되지 않게 한다.)
            return "axis=" + axis +
                   ", name=" + item.Name +
                   ", servo=" + (item.IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (item.IsAlarm ? "ON" : "OFF") +
                   ", moving=" + (item.IsMoving ? "Y" : "N") +
                   ", inpos=" + (item.IsInPosition ? "Y" : "N") +
                   ", actual=" + item.ActualPosition +
                   ", command=" + item.CommandPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance +
                   ", sim=" + (item.Config != null && item.Config.IsSimulationMode) +
                   FormatAxisLastMotionFailure(item) +
                   FormatStageLastMoveFailure(stage);
        }

        private bool IsFeederYTargetInSoftLimit(double target)
        {
            if (Feeder == null || Feeder.FeederY == null || Feeder.FeederY.Setup == null)
                return false;

            if (!Feeder.FeederY.Setup.SoftLimitEnabled)
                return true;

            return target >= Feeder.FeederY.Setup.SoftLimitMinus &&
                   target <= Feeder.FeederY.Setup.SoftLimitPlus;
        }

        private string BuildFeederYSoftLimitState()
        {
            if (Feeder == null || Feeder.FeederY == null || Feeder.FeederY.Setup == null)
                return "FeederY setup is not available.";

            return "softLimitEnabled=" + Feeder.FeederY.Setup.SoftLimitEnabled +
                   ", softMinus=" + Feeder.FeederY.Setup.SoftLimitMinus +
                   ", softPlus=" + Feeder.FeederY.Setup.SoftLimitPlus +
                   ", actual=" + Feeder.FeederY.ActualPosition;
        }

        private static string FormatAxisLastMotionFailure(QMC.Common.Motion.BaseAxis item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.LastMotionFailureMessage))
                return string.Empty;

            return ", lastMotionFailure=" + item.LastMotionFailureMessage;
        }

        private static string FormatStageLastMoveFailure(InputStageUnit stage)
        {
            if (stage == null || string.IsNullOrWhiteSpace(stage.LastStageMoveFailureMessage))
                return string.Empty;

            return ", lastStageMoveFailure=" + stage.LastStageMoveFailureMessage;
        }

        private InputStageUnit ResolveStage()
        {
            return Context.Machine != null ? Context.Machine.InputStageUnit : null;
        }

        private WaferMaterial ResolveFeederWafer()
        {
            return Feeder.CurrentWaferMaterial ?? MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
        }

        private bool IsInputStageEmpty(InputStageUnit stage)
        {
            if (stage == null)
                return true;

            return stage.CurrentWaferMaterial == null &&
                   MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage) == null;
        }

        private bool IsHardwareBypass()
        {
            AppSettings settings = AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (Context.Controller != null && Context.Controller.GlobalDryRun) ||
                   (Feeder.Setup != null && Feeder.Setup.IsSimulationMode) ||
                   (Feeder.Config != null && Feeder.Config.bDryRun);
        }
    }
}


