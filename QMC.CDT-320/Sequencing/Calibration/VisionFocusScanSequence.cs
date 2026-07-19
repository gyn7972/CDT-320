using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.VisionComm;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    public enum VisionFocusScanStep
    {
        Idle,
        CheckUnit,
        PrepareFocusPosition,
        FocusStart,
        MoveAndMeasure,
        FocusBest,
        SaveBest,
        ReturnDefault,
        Complete,
        Error
    }

    public sealed class VisionFocusScanRequest
    {
        public VisionFocusScanKind Kind { get; set; }
        public VisionFocusPickerSide PickerSide { get; set; }
        public int PickerNo { get; set; } = 1;
        public double DefaultPosition { get; set; }
        public double MinusRange { get; set; } = 0.2;
        public double PlusRange { get; set; } = 0.2;
        public double Step { get; set; } = 0.02;
        public double FineMinusRange { get; set; } = 0.05;
        public double FinePlusRange { get; set; } = 0.05;
        public double FineStep { get; set; } = 0.01;
        public int RepeatCount { get; set; } = 1;
        public double MoveVelocity { get; set; } = 30.0;
        public double MoveAcceleration { get; set; } = 300.0;
        public double MoveDeceleration { get; set; } = 300.0;
        public int SettleDelayMs { get; set; } = 50;
        public int MotionTimeoutMs { get; set; } = 5000;
        public int VisionTimeoutMs { get; set; } = 5000;
        public int VisionBestTimeoutMs { get; set; } = 120000;
        public VisionFocusValueReceiveMode FocusValueReceiveMode { get; set; } = VisionFocusValueReceiveMode.AckOnly;
        public bool ReturnToDefaultAfterScan { get; set; } = true;
        public bool SkipPrepareFocusPosition { get; set; }
        // Side 스캔 전 Picker X/Y/Z/T를 DieSidePosition 기준으로 이동한다(다이얼로그 수동 실행 전용).
        // 자동(Collet Cal) 경로는 자체적으로 Picker를 위치시키므로 false를 유지한다.
        public bool PrepareSidePickerPosition { get; set; }
        public bool FineOnlyScan { get; set; }
        public string RuntimeReason { get; set; }
        public string UpdatedBy { get; set; }
    }

    public sealed class VisionFocusScanSample
    {
        public int No { get; set; }
        public double Position { get; set; }
        public double Score { get; set; }
        public bool Success { get; set; }
        public string Raw { get; set; }
    }

    public sealed class VisionFocusScanResult
    {
        public bool Success { get; set; }
        public double BestPosition { get; set; }
        public double BestScore { get; set; }
        public int SampleCount { get; set; }
        public string Message { get; set; }
        public List<VisionFocusScanSample> Samples { get; private set; } = new List<VisionFocusScanSample>();
    }

    public sealed class VisionFocusScanSequence
    {
        private const int MaxSampleCount = 1000;
        private const double ExactMoveSkipToleranceMm = 0.0;

        private readonly CDT320_Machine _machine;
        private readonly VisionFocusScanRequest _request;
        private readonly List<double> _scanPositions = new List<double>();
        private readonly Random _simRandom = new Random();
        private IDisposable _focusWorkAreaScope;
        private bool _useSimulatedVisionFocus;
        private int _scanPass;
        private int _currentPassStartSampleIndex;
        private double _roughBestPosition;
        private double _roughBestScore;

        public VisionFocusScanSequence(CDT320_Machine machine, VisionFocusScanRequest request)
        {
            _machine = machine;
            _request = request;
            Result = new VisionFocusScanResult();
        }

        public VisionFocusScanStep CurrentStep { get; private set; }
        public VisionFocusScanResult Result { get; private set; }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            // 현재 기준: runMode 없는 기존 호출은 Manual 스코프로 실행한다.
            return await RunAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> RunAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(runMode == SequenceRunMode.Auto, "VisionFocusScanSequence.RunAsync:" + runMode))
            {
            try
            {
                ct.ThrowIfCancellationRequested();
                CurrentStep = VisionFocusScanStep.CheckUnit;
                _useSimulatedVisionFocus = false;
                _scanPass = 0;
                _currentPassStartSampleIndex = 0;
                _roughBestPosition = 0.0;
                _roughBestScore = 0.0;

                while (CurrentStep != VisionFocusScanStep.Complete &&
                       CurrentStep != VisionFocusScanStep.Error)
                {
                    ct.ThrowIfCancellationRequested();
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-STEP",
                        "Vision Focus Cal 단계 시작: " + CurrentStep + ", 대상=" + BuildTargetLabel());

                    int result = await ExecuteCurrentStepAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        CurrentStep = VisionFocusScanStep.Error;
                        Result.Success = false;
                        return result;
                    }
                }

                Result.Success = true;
                Result.Message = "Vision Focus Cal 완료. 대상=" + BuildTargetLabel() +
                                 ", best=" + Result.BestPosition.ToString("F3") +
                                 ", score=" + Result.BestScore.ToString("F4") +
                                 ", sample=" + Result.SampleCount;
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-DONE", Result.Message);
                return 0;
            }
            catch (OperationCanceledException)
            {
                EventLogger.Write(EventKind.Warning, "CAL", "VISION-FOCUS-CAL-CANCEL", "Vision Focus Cal 작업이 취소되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-EX", "VisionFocusScanSequence", "Vision Focus Cal 예외 발생: " + ex.Message);
            }
            finally
            {
                ReleaseFocusWorkArea();
            }
            }
        }

        public async Task<int> MoveDefaultOnlyAsync(CancellationToken ct)
        {
            // 현재 기준: runMode 없는 기존 Default 이동 호출은 Manual 스코프로 실행한다.
            return await MoveDefaultOnlyAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> MoveDefaultOnlyAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(runMode == SequenceRunMode.Auto, "VisionFocusScanSequence.MoveDefaultOnlyAsync:" + runMode))
            {
            try
            {
                ct.ThrowIfCancellationRequested();
                int checkResult = CheckCommonCondition(false);
                if (checkResult != 0)
                    return checkResult;

                int prepareResult = await PrepareMoveDefaultReadyPositionAsync(ct).ConfigureAwait(false);
                if (prepareResult != 0)
                    return prepareResult;

                int result = await MoveScanAxisAndVerifyAsync(_request.DefaultPosition, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                Result.Success = true;
                Result.BestPosition = _request.DefaultPosition;
                Result.Message = "Default Position 이동 완료. 대상=" + BuildTargetLabel() +
                                 ", position=" + _request.DefaultPosition.ToString("F3");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-MOVE-DEFAULT-EX", "VisionFocusScanSequence",
                    "Default Position 이동 중 예외 발생: " + ex.Message);
            }
            finally
            {
                ReleaseFocusWorkArea();
            }
            }
        }

        private async Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                switch (CurrentStep)
                {
                    case VisionFocusScanStep.CheckUnit:
                        return CheckUnitStep();

                    case VisionFocusScanStep.PrepareFocusPosition:
                        return await PrepareFocusPositionStepAsync(ct).ConfigureAwait(false);

                    case VisionFocusScanStep.FocusStart:
                        return await FocusStartStepAsync(ct).ConfigureAwait(false);

                    case VisionFocusScanStep.MoveAndMeasure:
                        return await MoveAndMeasureStepAsync(ct).ConfigureAwait(false);

                    case VisionFocusScanStep.FocusBest:
                        return await FocusBestStepAsync(ct).ConfigureAwait(false);

                    case VisionFocusScanStep.SaveBest:
                        return SaveBestStep();

                    case VisionFocusScanStep.ReturnDefault:
                        return await ReturnDefaultStepAsync(ct).ConfigureAwait(false);

                    default:
                        CurrentStep = VisionFocusScanStep.Complete;
                        return 0;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-STEP-EX", "VisionFocusScanSequence",
                    "Vision Focus Cal 단계 예외 발생. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckUnitStep()
        {
            try
            {
                int checkResult = CheckCommonCondition(true);
                if (checkResult != 0)
                    return checkResult;

                Result.Samples.Clear();
                if (_request.FineOnlyScan)
                {
                    // Runtime AutoFocus는 현재 Bottom Z 기준으로 Fine 구간만 스캔한다.
                    _scanPass = 1;
                    BuildFineScanPositions(_request.DefaultPosition);
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-FINE-ONLY",
                        "Runtime Fine-only Focus Scan을 시작합니다. 대상=" + BuildTargetLabel() +
                        ", baseZ=" + _request.DefaultPosition.ToString("F3") +
                        ", fineMinus=" + _request.FineMinusRange +
                        ", finePlus=" + _request.FinePlusRange +
                        ", fineStep=" + _request.FineStep);
                }
                else
                {
                    _scanPass = 0;
                    BuildRoughScanPositions();
                }
                if (_scanPositions.Count == 0)
                    return Fail("VISION-FOCUS-CAL-NO-SAMPLE", "VisionFocusScanSequence", "Vision Focus Cal 스캔 위치가 없습니다.");

                _machine.VisionUnit.Config.EnsureCalibrationObjects();
                CurrentStep = VisionFocusScanStep.PrepareFocusPosition;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-CHECK-EX", "VisionFocusScanSequence",
                    "Vision Focus Cal 조건 확인 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckCommonCondition(bool requireScanRange)
        {
            if (_machine == null)
                return Fail("VISION-FOCUS-CAL-NO-MACHINE", "VisionFocusScanSequence", "장비 객체가 없어 Vision Focus Cal을 실행할 수 없습니다.");
            if (_machine.VisionUnit == null || _machine.VisionUnit.Config == null)
                return Fail("VISION-FOCUS-CAL-NO-VISION", "VisionFocusScanSequence", "VisionUnit이 없어 Vision Focus Cal을 실행할 수 없습니다.");
            if (_request == null)
                return Fail("VISION-FOCUS-CAL-NO-REQUEST", "VisionFocusScanSequence", "Vision Focus Cal 요청 정보가 없습니다.");

            if (requireScanRange)
            {
                if (_request.FineStep <= 0)
                    return Fail("VISION-FOCUS-CAL-BAD-FINE-STEP", "VisionFocusScanSequence", "Vision Focus Cal Fine Step 값은 0보다 커야 합니다. fineStep=" + _request.FineStep);
                if (!_request.FineOnlyScan && _request.Step <= 0)
                    return Fail("VISION-FOCUS-CAL-BAD-STEP", "VisionFocusScanSequence", "Vision Focus Cal Step 값은 0보다 커야 합니다. step=" + _request.Step);
                if (!_request.FineOnlyScan && (_request.MinusRange < 0 || _request.PlusRange < 0))
                    return Fail("VISION-FOCUS-CAL-BAD-RANGE", "VisionFocusScanSequence", "Vision Focus Cal 스캔 범위가 올바르지 않습니다. minus=" + _request.MinusRange + ", plus=" + _request.PlusRange);
                if (_request.FineMinusRange < 0 || _request.FinePlusRange < 0)
                    return Fail("VISION-FOCUS-CAL-BAD-FINE-RANGE", "VisionFocusScanSequence", "Vision Focus Cal Fine 스캔 범위가 올바르지 않습니다. minus=" + _request.FineMinusRange + ", plus=" + _request.FinePlusRange);
                if (_request.RepeatCount <= 0)
                    return Fail("VISION-FOCUS-CAL-BAD-REPEAT", "VisionFocusScanSequence", "Vision Focus Cal 반복 횟수는 1 이상이어야 합니다. repeat=" + _request.RepeatCount);
                if (_request.MoveVelocity <= 0)
                    return Fail("VISION-FOCUS-CAL-BAD-SPEED", "VisionFocusScanSequence", "Vision Focus Cal 이동 속도는 0보다 커야 합니다. velocity=" + _request.MoveVelocity);
                if (_request.MoveAcceleration <= 0 || _request.MoveDeceleration <= 0)
                    return Fail("VISION-FOCUS-CAL-BAD-ACCDEC", "VisionFocusScanSequence", "Vision Focus Cal 가감속 값은 0보다 커야 합니다. acc=" + _request.MoveAcceleration + ", dec=" + _request.MoveDeceleration);
            }

            if (_request.PickerNo < 1 || _request.PickerNo > 4)
                return Fail("VISION-FOCUS-CAL-BAD-PICKER", "VisionFocusScanSequence", "Collet 번호는 1~4 범위여야 합니다. pickerNo=" + _request.PickerNo);
            if (_machine.PickerFrontUnit == null)
                return Fail("VISION-FOCUS-CAL-NO-FRONT-PICKER", "VisionFocusScanSequence", "FrontPickerUnit이 없어 Vision Focus 준비 동작을 실행할 수 없습니다.");
            if (_machine.PickerRearUnit == null)
                return Fail("VISION-FOCUS-CAL-NO-REAR-PICKER", "VisionFocusScanSequence", "RearPickerUnit이 없어 Vision Focus 준비 동작을 실행할 수 없습니다.");
            if (_machine.InputStageUnit == null)
                return Fail("VISION-FOCUS-CAL-NO-INPUT-STAGE", "VisionFocusScanSequence", "InputStageUnit이 없어 InputCamera Avoid 확인을 실행할 수 없습니다.");
            if (_machine.OutputStageUnit == null)
                return Fail("VISION-FOCUS-CAL-NO-OUTPUT-STAGE", "VisionFocusScanSequence", "OutputStageUnit이 없어 OutputCamera Avoid 확인을 실행할 수 없습니다.");

            return 0;
        }

        private async Task<int> PrepareFocusPositionStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int result = await PrepareFocusReadyPositionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = VisionFocusScanStep.FocusStart;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-PREPARE-STEP-EX", "VisionFocusScanSequence", "Vision Focus 준비 Step 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareFocusReadyPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-PREPARE",
                    "Vision Focus 준비 동작을 시작합니다. 대상=" + BuildTargetLabel());

                if (_request.SkipPrepareFocusPosition)
                {
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-RUNTIME-Z-ONLY",
                        "생산 Runtime Focus Scan은 현재 Bottom 촬영 위치를 유지하고 Z축만 스캔합니다. 대상=" +
                        BuildTargetLabel() + ", reason=" + (_request.RuntimeReason ?? string.Empty));
                    if (!IsBottomFocusKind())
                        return Fail("VISION-FOCUS-RUNTIME-Z-ONLY-KIND", "VisionFocusScanSequence",
                            "Runtime Z-only Focus는 Bottom Focus 대상에서만 사용할 수 있습니다. 대상=" + BuildTargetLabel());

                    CurrentStep = VisionFocusScanStep.FocusStart;
                    return 0;
                }

                int result = await EnsureInputOutputVisionAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (IsBottomFocusKind())
                {
                    result = await PrepareBottomFocusPickerPositionAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = CheckFocusReadyPosition();
                    if (result != 0)
                        return result;

                    EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-PREPARE-DONE",
                        "Vision Focus 준비 동작이 완료되었습니다. 대상=" + BuildTargetLabel());
                    return 0;
                }

                if (_request.PrepareSidePickerPosition)
                {
                    result = await PrepareSideFocusPickerPositionAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-PREPARE-DONE",
                    "Vision Focus 준비 동작이 완료되었습니다. 대상=" + BuildTargetLabel());
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-PREPARE-EX", "VisionFocusScanSequence", "Vision Focus 준비 동작 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareBottomFocusPickerPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result;
                if (!CanSkipNonSelectedPickerOutputAvoidMove())
                {
                    result = await MoveSelectedPickerYAndZSafeForOppositePickerXAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveNonSelectedPickerOutputAvoidAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                result = ReserveFocusWorkArea();
                if (result != 0)
                    return result;

                if (!CanSkipSelectedPickerBottomMove())
                {
                    result = await MoveSelectedPickerBottomPositionAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-BOTTOM-PICKER-PREPARE-EX", "VisionFocusScanSequence",
                    "Bottom Focus Picker 위치 준비 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareSideFocusPickerPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                bool front = _request.Kind == VisionFocusScanKind.FrontSide0 ||
                             _request.Kind == VisionFocusScanKind.FrontSide90;
                bool angle90 = _request.Kind == VisionFocusScanKind.FrontSide90 ||
                               _request.Kind == VisionFocusScanKind.RearSide90;
                // Side kind에서는 Picker 측을 Kind 기준으로 강제해 표시/이동/저장 간 불일치를 방지한다.
                _request.PickerSide = front ? VisionFocusPickerSide.Front : VisionFocusPickerSide.Rear;
                int pickerIndex = NormalizePickerIndex(_request.PickerNo);
                PickerSideFocusReferenceTarget target = CalibrationCoordinateService.ResolveSideFocusReferenceTarget(
                    _machine,
                    front ? VisionFocusPickerSide.Front : VisionFocusPickerSide.Rear,
                    pickerIndex);
                if (target == null)
                    return Fail("VISION-FOCUS-CAL-SIDE-TARGET", "VisionFocusScanSequence",
                        "Side Focus 기준 좌표를 계산할 수 없습니다. 대상=" + BuildTargetLabel());

                double targetT = angle90 ? target.T + 90.0 : target.T;

                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-SIDE-PREPARE",
                    "Side Focus Picker 준비 이동을 시작합니다. 대상=" + BuildTargetLabel() +
                    ", x=" + target.X.ToString("F3") +
                    ", y=" + target.Y.ToString("F3") +
                    ", z=" + target.Z.ToString("F3") +
                    ", t=" + targetT.ToString("F3"));

                int result;
                if (!CanSkipNonSelectedPickerOutputAvoidMove())
                {
                    result = await MoveSelectedPickerYAndZSafeForOppositePickerXAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    result = await MoveNonSelectedPickerOutputAvoidAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }

                // 표준 존 진입 순서: Z 상승(Avoid) -> Y 후진(Avoid) -> X 이동 -> Y 전진 -> T 이동 -> Z 하강
                result = front
                    ? await MoveFrontPickerZGroupTeachingAsync("AvoidPosition", "Side Focus 준비 Z Avoid", ct).ConfigureAwait(false)
                    : await MoveRearPickerZGroupTeachingAsync("AvoidPosition", "Side Focus 준비 Z Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = front
                    ? await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerY, "AvoidPosition", ct).ConfigureAwait(false)
                    : await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerY, "AvoidPosition", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = ReserveFocusWorkArea(PickerWorkZone.Side, front);
                if (result != 0)
                    return result;

                result = front
                    ? await MoveFrontPickerAxisAndVerifyAsync(PickerAxis.PickerX, target.X, "VisionFocusCal;DieSidePosition;PickerPhase=SafeX", ct).ConfigureAwait(false)
                    : await MoveRearPickerAxisAndVerifyAsync(PickerAxis.PickerX, target.X, "VisionFocusCal;DieSidePosition;PickerPhase=SafeX", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = front
                    ? await MoveFrontPickerAxisAndVerifyAsync(PickerAxis.PickerY, target.Y, "VisionFocusCal;DieSidePosition;PickerPhase=SafeY", ct).ConfigureAwait(false)
                    : await MoveRearPickerAxisAndVerifyAsync(PickerAxis.PickerY, target.Y, "VisionFocusCal;DieSidePosition;PickerPhase=SafeY", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = front
                    ? await MoveFrontPickerAxisAndVerifyAsync(target.PickerTAxis, targetT, "VisionFocusCal;DieSidePosition;PickerPhase=SafeT", ct).ConfigureAwait(false)
                    : await MoveRearPickerAxisAndVerifyAsync(target.PickerTAxis, targetT, "VisionFocusCal;DieSidePosition;PickerPhase=SafeT", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = front
                    ? await MoveFrontPickerAxisAndVerifyAsync(target.PickerZAxis, target.Z, "VisionFocusCal;DieSidePosition;PickerPhase=SideZ", ct).ConfigureAwait(false)
                    : await MoveRearPickerAxisAndVerifyAsync(target.PickerZAxis, target.Z, "VisionFocusCal;DieSidePosition;PickerPhase=SideZ", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-SIDE-PREPARE-DONE",
                    "Side Focus Picker 준비 이동이 완료되었습니다. 대상=" + BuildTargetLabel());
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-SIDE-PICKER-PREPARE-EX", "VisionFocusScanSequence",
                    "Side Focus Picker 위치 준비 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> PrepareMoveDefaultReadyPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-MOVE-DEFAULT-PREPARE",
                    "Default Position 이동 준비를 시작합니다. 대상=" + BuildTargetLabel());

                int result = await EnsureInputOutputVisionAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (!IsBottomFocusKind())
                    return 0;

                result = await PrepareBottomFocusPickerPositionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = CheckFocusReadyPosition();
                if (result != 0)
                    return result;

                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-MOVE-DEFAULT-PREPARE-DONE",
                    "Default Position 이동 준비가 완료되었습니다. 대상=" + BuildTargetLabel());
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-MOVE-DEFAULT-PREPARE-EX", "VisionFocusScanSequence",
                    "Default Position 이동 준비 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureInputOutputVisionAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await EnsureInputVisionAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await EnsureOutputVisionAvoidAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-CAMERA-AVOID-EX", "VisionFocusScanSequence", "Input/Output Camera Avoid 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureInputVisionAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var stage = _machine != null ? _machine.InputStageUnit : null;
                if (stage == null || stage.CameraX == null || stage.Recipe == null || stage.Recipe.VisionX == null)
                    return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-MISSING", "InputStageUnit", "InputCamera Avoid \uC774\uB3D9\uC744 \uC704\uD55C \uCD95/Recipe \uC815\uBCF4\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4.");

                double target = stage.Recipe.VisionX.AvoidPosition;
                if (AxisMoveWaiter.CanSkipMoveCommandAtTarget(stage.CameraX, target))
                    return 0;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "VisionFocusCalStartSafe",
                    "Vision Focus Cal InputVisionX Avoid 이동. target=" + target.ToString("F6") +
                    ", velocity=" + _request.MoveVelocity.ToString("F6") +
                    ", acceleration=" + _request.MoveAcceleration.ToString("F6") +
                    ", deceleration=" + _request.MoveDeceleration.ToString("F6") +
                    ", timeoutMs=" + ResolveMotionTimeoutMs() +
                    ", speedScalePercent=" + MotionSpeedScale.ScalePercent.ToString("F3") +
                    ", effectiveScaleFactor=" + MotionSpeedScale.EffectiveScaleFactor.ToString("F6"));

                int result = await stage.MoveInputStageAxisCommandWithMotion(
                    WaferStageAxis.VisionX,
                    target,
                    _request.MoveVelocity,
                    _request.MoveAcceleration,
                    _request.MoveDeceleration).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-MOVE", "InputStageUnit", "InputCamera Avoid \uC774\uB3D9 \uBA85\uB839 \uC2E4\uD328. result=" + result + ", target=" + target.ToString("F3"));

                result = await stage.WaitInputStageAxisInPosition(WaferStageAxis.VisionX, target, ResolveMotionTimeoutMs(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-WAIT", "InputStageUnit", "InputCamera Avoid \uC774\uB3D9 \uC644\uB8CC \uD655\uC778 \uC2E4\uD328. result=" + result + ", target=" + target.ToString("F3"));

                if (!stage.IsVisionXInAvoidPosition())
                    return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-CHECK", "InputStageUnit", "InputCamera Avoid \uCD5C\uC885 \uC704\uCE58 \uD655\uC778 \uC2E4\uD328. actual=" + stage.CameraX.ActualPosition.ToString("F3") + ", target=" + target.ToString("F3"));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-EX", "InputStageUnit", "InputCamera Avoid 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureOutputVisionAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var stage = _machine != null ? _machine.OutputStageUnit : null;
                if (stage == null ||
                    stage.OutputCameraX == null ||
                    stage.Recipe == null ||
                    stage.Recipe.VisionX == null)
                    return Fail("VISION-FOCUS-CAL-OUTPUT-CAMERA-MISSING", "OutputStageUnit", "OutputCamera Avoid 이동을 위한 축 정보가 없습니다.");

                stage.Recipe.EnsurePositionObjects();
                double target = stage.Recipe.VisionX.AvoidPosition;
                if (AxisMoveWaiter.CanSkipMoveCommandAtTarget(stage.OutputCameraX, target))
                    return 0;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "VisionFocusCalStartSafe",
                    "Vision Focus Cal OutputVisionX Avoid 이동. actual=" + stage.OutputCameraX.ActualPosition.ToString("F6") +
                    ", velocity=" + _request.MoveVelocity.ToString("F6") +
                    ", acceleration=" + _request.MoveAcceleration.ToString("F6") +
                    ", deceleration=" + _request.MoveDeceleration.ToString("F6") +
                    ", timeoutMs=" + ResolveMotionTimeoutMs() +
                    ", speedScalePercent=" + MotionSpeedScale.ScalePercent.ToString("F3") +
                    ", effectiveScaleFactor=" + MotionSpeedScale.EffectiveScaleFactor.ToString("F6"));

                int result = await stage.MoveVisionXToAvoidAndVerifyAsync(
                    ResolveMotionTimeoutMs(),
                    _request.MoveVelocity,
                    _request.MoveAcceleration,
                    _request.MoveDeceleration,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-FOCUS-CAL-OUTPUT-CAMERA-MOVE", "OutputStageUnit", "OutputCamera Avoid 이동 실패. result=" + result);

                if (!stage.IsVisionXInAvoidPosition())
                    return Fail("VISION-FOCUS-CAL-OUTPUT-CAMERA-CHECK", "OutputStageUnit", "OutputCamera Avoid 최종 위치 확인 실패. actual=" + stage.OutputCameraX.ActualPosition.ToString("F3"));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-OUTPUT-CAMERA-EX", "OutputStageUnit", "OutputCamera Avoid 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveNonSelectedPickerOutputAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsSelectedFront())
                {
                    int result = await MoveRearPickerOutputAvoidSequentialAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-REAR-OUTPUT-AVOID", "PickerRearUnit", "선택되지 않은 RearPicker Output-side Avoid 이동 실패. result=" + result);

                    if (!_machine.PickerRearUnit.IsPickerInOutputSideAvoidPosition())
                        return Fail("VISION-FOCUS-CAL-REAR-OUTPUT-CHECK", "PickerRearUnit", "선택되지 않은 RearPicker Output-side Avoid 최종 위치 확인 실패.");
                }
                else
                {
                    int result = await MoveFrontPickerOutputAvoidSequentialAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-FRONT-OUTPUT-AVOID", "PickerFrontUnit", "선택되지 않은 FrontPicker Output-side Avoid 이동 실패. result=" + result);

                    if (!_machine.PickerFrontUnit.IsPickerInOutputSideAvoidPosition())
                        return Fail("VISION-FOCUS-CAL-FRONT-OUTPUT-CHECK", "PickerFrontUnit", "선택되지 않은 FrontPicker Output-side Avoid 최종 위치 확인 실패.");
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-NONSELECTED-PICKER-EX", "PickerUnit", "선택되지 않은 Picker Output-side Avoid 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveSelectedPickerOutputAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsSelectedFront())
                {
                    int result = await MoveFrontPickerOutputAvoidSequentialAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-FRONT-SELECTED-OUTPUT-AVOID", "PickerFrontUnit", "선택된 FrontPicker Output-side Avoid 이동 실패. result=" + result);

                    if (!_machine.PickerFrontUnit.IsPickerInOutputSideAvoidPosition())
                        return Fail("VISION-FOCUS-CAL-FRONT-SELECTED-OUTPUT-CHECK", "PickerFrontUnit", "선택된 FrontPicker Output-side Avoid 최종 위치 확인 실패.");
                }
                else
                {
                    int result = await MoveRearPickerOutputAvoidSequentialAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-REAR-SELECTED-OUTPUT-AVOID", "PickerRearUnit", "선택된 RearPicker Output-side Avoid 이동 실패. result=" + result);

                    if (!_machine.PickerRearUnit.IsPickerInOutputSideAvoidPosition())
                        return Fail("VISION-FOCUS-CAL-REAR-SELECTED-OUTPUT-CHECK", "PickerRearUnit", "선택된 RearPicker Output-side Avoid 최종 위치 확인 실패.");
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-SELECTED-PICKER-OUTPUT-EX", "PickerUnit", "선택된 Picker Output-side Avoid 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveSelectedPickerYAndZSafeForOppositePickerXAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsSelectedFront())
                {
                    int result = await MoveFrontPickerZGroupTeachingAsync("AvoidPosition", "상대 Picker X 이동 전 선택된 FrontPicker Z Avoid", ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-FRONT-SELECTED-Z-AVOID", "PickerFrontUnit", "상대 Picker X 이동 전 선택된 FrontPicker Z Avoid 실패. result=" + result);

                    result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerY, "AvoidPosition", ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-FRONT-SELECTED-Y-AVOID", "PickerFrontUnit", "상대 Picker X 이동 전 선택된 FrontPicker Y Avoid 실패. result=" + result);
                }
                else
                {
                    int result = await MoveRearPickerZGroupTeachingAsync("AvoidPosition", "상대 Picker X 이동 전 선택된 RearPicker Z Avoid", ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-REAR-SELECTED-Z-AVOID", "PickerRearUnit", "상대 Picker X 이동 전 선택된 RearPicker Z Avoid 실패. result=" + result);

                    result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerY, "AvoidPosition", ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-REAR-SELECTED-Y-AVOID", "PickerRearUnit", "상대 Picker X 이동 전 선택된 RearPicker Y Avoid 실패. result=" + result);
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-SELECTED-YZ-AVOID-EX", "PickerUnit",
                    "상대 Picker X 이동 전 선택 Picker Y/Z 안전 위치 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveSelectedPickerBottomPositionAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsSelectedFront())
                {
                    int result = await MoveFrontPickerBottomSequentialAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-FRONT-BOTTOM-MOVE", "PickerFrontUnit", "선택된 FrontPicker Bottom 위치 이동 실패. pickerNo=" + _request.PickerNo + ", result=" + result);

                    string detail;
                    if (!IsSelectedPickerBottomPosition(out detail))
                        return Fail("VISION-FOCUS-CAL-FRONT-BOTTOM-CHECK", "PickerFrontUnit",
                            "선택된 FrontPicker Bottom 위치 최종 확인 실패. pickerNo=" + _request.PickerNo +
                            ", " + detail);
                }
                else
                {
                    int result = await MoveRearPickerBottomSequentialAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-REAR-BOTTOM-MOVE", "PickerRearUnit", "선택된 RearPicker Bottom 위치 이동 실패. pickerNo=" + _request.PickerNo + ", result=" + result);

                    string detail;
                    if (!IsSelectedPickerBottomPosition(out detail))
                        return Fail("VISION-FOCUS-CAL-REAR-BOTTOM-CHECK", "PickerRearUnit",
                            "선택된 RearPicker Bottom 위치 최종 확인 실패. pickerNo=" + _request.PickerNo +
                            ", " + detail);
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-SELECTED-PICKER-BOTTOM-EX", "PickerUnit", "선택된 Picker Bottom 위치 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveFrontPickerOutputAvoidSequentialAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveFrontPickerZGroupTeachingAsync("AvoidPosition", "FrontPicker Output 이동 전 PickerZ Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerY, "AvoidPosition", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerX, "OutputAvoidPosition", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerY, "OutputAvoidPosition", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerTGroupTeachingAsync("OutputAvoidPosition", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await MoveFrontPickerZGroupTeachingAsync("OutputAvoidPosition", "FrontPicker Output-side Avoid PickerZ", ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-FRONT-OUTPUT-SEQ-EX", "PickerFrontUnit", "FrontPicker Output-side Avoid 순차 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveRearPickerOutputAvoidSequentialAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveRearPickerZGroupTeachingAsync("AvoidPosition", "RearPicker Output 이동 전 PickerZ Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerY, "AvoidPosition", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerX, "OutputAvoidPosition", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerY, "OutputAvoidPosition", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerTGroupTeachingAsync("OutputAvoidPosition", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await MoveRearPickerZGroupTeachingAsync("OutputAvoidPosition", "RearPicker Output-side Avoid PickerZ", ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-REAR-OUTPUT-SEQ-EX", "PickerRearUnit", "RearPicker Output-side Avoid 순차 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveFrontPickerBottomSequentialAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveFrontPickerZGroupTeachingAsync("AvoidPosition", "FrontPicker Bottom 이동 전 PickerZ Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerTeachingAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                int pickerIndex = NormalizePickerIndex(_request.PickerNo);
                var offset = _machine.PickerFrontUnit.GetRuntimePickerOffset(pickerIndex) ?? new PickerAlignOffset();
                PickerCalibratedZoneTarget bottomTarget = ResolveBottomZoneTarget(VisionFocusPickerSide.Front, pickerIndex, offset);

                result = await MoveFrontPickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    bottomTarget.X,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeX",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    bottomTarget.Y,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerAxisAndVerifyAsync(
                    bottomTarget.PickerTAxis,
                    bottomTarget.T,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeT",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await MoveFrontPickerAxisAndVerifyAsync(
                    bottomTarget.PickerZAxis,
                    ResolveBottomFocusStartZ(),
                    ResolveBottomMotionCommandTag() + ";PickerPhase=StartZ",
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-FRONT-BOTTOM-SEQ-EX", "PickerFrontUnit", "FrontPicker Bottom 순차 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveRearPickerBottomSequentialAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = await MoveRearPickerZGroupTeachingAsync("AvoidPosition", "RearPicker Bottom 이동 전 PickerZ Avoid", ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerTeachingAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    "AvoidPosition",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                int pickerIndex = NormalizePickerIndex(_request.PickerNo);
                var offset = _machine.PickerRearUnit.GetRuntimePickerOffset(pickerIndex) ?? new PickerAlignOffset();
                PickerCalibratedZoneTarget bottomTarget = ResolveBottomZoneTarget(VisionFocusPickerSide.Rear, pickerIndex, offset);

                result = await MoveRearPickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    bottomTarget.X,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeX",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    bottomTarget.Y,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerAxisAndVerifyAsync(
                    bottomTarget.PickerTAxis,
                    bottomTarget.T,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeT",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await MoveRearPickerAxisAndVerifyAsync(
                    bottomTarget.PickerZAxis,
                    ResolveBottomFocusStartZ(),
                    ResolveBottomMotionCommandTag() + ";PickerPhase=StartZ",
                    ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-REAR-BOTTOM-SEQ-EX", "PickerRearUnit", "RearPicker Bottom 순차 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveFrontPickerZGroupTeachingAsync(string positionName, string description, CancellationToken ct)
        {
            int result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ0, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-FRONT-Z-GROUP", "PickerFrontUnit", description + " 실패. axis=PickerZ1, result=" + result);
            result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ1, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-FRONT-Z-GROUP", "PickerFrontUnit", description + " 실패. axis=PickerZ2, result=" + result);
            result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ2, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-FRONT-Z-GROUP", "PickerFrontUnit", description + " 실패. axis=PickerZ3, result=" + result);
            result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ3, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-FRONT-Z-GROUP", "PickerFrontUnit", description + " 실패. axis=PickerZ4, result=" + result);
            return 0;
        }

        private async Task<int> MoveRearPickerZGroupTeachingAsync(string positionName, string description, CancellationToken ct)
        {
            int result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ0, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-REAR-Z-GROUP", "PickerRearUnit", description + " 실패. axis=PickerZ2, result=" + result);
            result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ1, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-REAR-Z-GROUP", "PickerRearUnit", description + " 실패. axis=PickerZ1, result=" + result);
            result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ2, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-REAR-Z-GROUP", "PickerRearUnit", description + " 실패. axis=PickerZ3, result=" + result);
            result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ3, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-REAR-Z-GROUP", "PickerRearUnit", description + " 실패. axis=PickerZ4, result=" + result);
            return 0;
        }

        private async Task<int> MoveFrontPickerTGroupTeachingAsync(string positionName, CancellationToken ct)
        {
            int result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerT0, positionName, ct).ConfigureAwait(false);
            if (result != 0) return result;
            result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerT1, positionName, ct).ConfigureAwait(false);
            if (result != 0) return result;
            result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerT2, positionName, ct).ConfigureAwait(false);
            if (result != 0) return result;
            return await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerT3, positionName, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveRearPickerTGroupTeachingAsync(string positionName, CancellationToken ct)
        {
            int result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerT0, positionName, ct).ConfigureAwait(false);
            if (result != 0) return result;
            result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerT1, positionName, ct).ConfigureAwait(false);
            if (result != 0) return result;
            result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerT2, positionName, ct).ConfigureAwait(false);
            if (result != 0) return result;
            return await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerT3, positionName, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis axis, string positionName, CancellationToken ct)
        {
            double target = _machine.PickerFrontUnit.GetPickerTeachingPosition(axis, positionName);
            return await MoveFrontPickerAxisAndVerifyAsync(axis, target, "VisionFocusCal;" + positionName, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis axis, string positionName, CancellationToken ct)
        {
            double target = _machine.PickerRearUnit.GetPickerTeachingPosition(axis, positionName);
            return await MoveRearPickerAxisAndVerifyAsync(axis, target, "VisionFocusCal;" + positionName, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveFrontPickerAxisAndVerifyAsync(PickerAxis axis, double target, string targetName, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            BaseAxis item = ResolveFrontPickerAxis(axis);
            if (IsAxisIdleAtExactPosition(item, target))
            {
                LogSkipMove("Picker", "Front", axis.ToString(), target, ExactMoveSkipToleranceMm);
                return 0;
            }

            int result = await _machine.PickerFrontUnit.MovePickerAxisCommandWithMotion(
                axis,
                target,
                _request.MoveVelocity,
                _request.MoveAcceleration,
                _request.MoveDeceleration,
                targetName,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            AxisMoveWaitResult wait = await WaitAxisMoveDoneInPositionAsync(item, target, ResolveMotionTimeoutMs(), ct).ConfigureAwait(false);
            if (!wait.Success)
                return Fail("VISION-FOCUS-CAL-FRONT-AXIS-WAIT", "PickerFrontUnit", "FrontPicker 축 이동 완료 확인 실패. axis=" + axis + ", target=" + target.ToString("F3") + ", reason=" + wait.Reason);

            double tolerance = ResolveAxisInPositionTolerance(item);
            if (!_machine.PickerFrontUnit.IsPickerAxisInPosition(axis, target, tolerance))
                return Fail("VISION-FOCUS-CAL-FRONT-AXIS-FINAL", "PickerFrontUnit",
                    "FrontPicker 축 최종 위치 확인 실패. axis=" + axis +
                    ", target=" + target.ToString("F3") +
                    ", actual=" + FormatAxisActual(item) +
                    ", tolerance=" + tolerance.ToString("0.######"));

            return 0;
        }

        private async Task<int> MoveRearPickerAxisAndVerifyAsync(PickerAxis axis, double target, string targetName, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            BaseAxis item = ResolveRearPickerAxis(axis);
            if (IsAxisIdleAtExactPosition(item, target))
            {
                LogSkipMove("Picker", "Rear", axis.ToString(), target, ExactMoveSkipToleranceMm);
                return 0;
            }

            int result = await _machine.PickerRearUnit.MovePickerAxisCommandWithMotion(
                axis,
                target,
                _request.MoveVelocity,
                _request.MoveAcceleration,
                _request.MoveDeceleration,
                targetName,
                true).ConfigureAwait(false);
            if (result != 0)
                return result;

            AxisMoveWaitResult wait = await WaitAxisMoveDoneInPositionAsync(item, target, ResolveMotionTimeoutMs(), ct).ConfigureAwait(false);
            if (!wait.Success)
                return Fail("VISION-FOCUS-CAL-REAR-AXIS-WAIT", "PickerRearUnit", "RearPicker 축 이동 완료 확인 실패. axis=" + axis + ", target=" + target.ToString("F3") + ", reason=" + wait.Reason);

            double tolerance = ResolveAxisInPositionTolerance(item);
            if (!_machine.PickerRearUnit.IsPickerAxisInPosition(axis, target, tolerance))
                return Fail("VISION-FOCUS-CAL-REAR-AXIS-FINAL", "PickerRearUnit",
                    "RearPicker 축 최종 위치 확인 실패. axis=" + axis +
                    ", target=" + target.ToString("F3") +
                    ", actual=" + FormatAxisActual(item) +
                    ", tolerance=" + tolerance.ToString("0.######"));

            return 0;
        }

        private BaseAxis ResolveFrontPickerAxis(PickerAxis axis)
        {
            if (_machine == null || _machine.PickerFrontUnit == null)
                return null;

            if (axis == PickerAxis.PickerX) return _machine.PickerFrontUnit.PickerX;
            if (axis == PickerAxis.PickerY) return _machine.PickerFrontUnit.PickerY;
            if (axis == PickerAxis.PickerT0) return _machine.PickerFrontUnit.PickerT0;
            if (axis == PickerAxis.PickerT1) return _machine.PickerFrontUnit.PickerT1;
            if (axis == PickerAxis.PickerT2) return _machine.PickerFrontUnit.PickerT2;
            if (axis == PickerAxis.PickerT3) return _machine.PickerFrontUnit.PickerT3;
            if (axis == PickerAxis.PickerZ0) return _machine.PickerFrontUnit.PickerZ0;
            if (axis == PickerAxis.PickerZ1) return _machine.PickerFrontUnit.PickerZ1;
            if (axis == PickerAxis.PickerZ2) return _machine.PickerFrontUnit.PickerZ2;
            if (axis == PickerAxis.PickerZ3) return _machine.PickerFrontUnit.PickerZ3;

            return null;
        }

        private BaseAxis ResolveRearPickerAxis(PickerAxis axis)
        {
            if (_machine == null || _machine.PickerRearUnit == null)
                return null;

            if (axis == PickerAxis.PickerX) return _machine.PickerRearUnit.PickerX;
            if (axis == PickerAxis.PickerY) return _machine.PickerRearUnit.PickerY;
            if (axis == PickerAxis.PickerT0) return _machine.PickerRearUnit.PickerT0;
            if (axis == PickerAxis.PickerT1) return _machine.PickerRearUnit.PickerT1;
            if (axis == PickerAxis.PickerT2) return _machine.PickerRearUnit.PickerT2;
            if (axis == PickerAxis.PickerT3) return _machine.PickerRearUnit.PickerT3;
            if (axis == PickerAxis.PickerZ0) return _machine.PickerRearUnit.PickerZ0;
            if (axis == PickerAxis.PickerZ1) return _machine.PickerRearUnit.PickerZ1;
            if (axis == PickerAxis.PickerZ2) return _machine.PickerRearUnit.PickerZ2;
            if (axis == PickerAxis.PickerZ3) return _machine.PickerRearUnit.PickerZ3;

            return null;
        }

        private static double ResolveAxisInPositionTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
        }

        private static bool IsAxisIdleAtExactPosition(BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return AxisMoveWaiter.CanSkipMoveCommandAtTarget(axis, target, tolerance);
        }

        private static Task<AxisMoveWaitResult> WaitAxisMoveDoneInPositionAsync(
            BaseAxis axis,
            double target,
            int timeoutMs,
            CancellationToken ct)
        {
            return AxisMoveWaiter.WaitMoveDoneInPositionAsync(
                axis,
                target,
                ResolveAxisInPositionTolerance(axis),
                timeoutMs,
                0,
                ct);
        }

        private static string FormatAxisActual(BaseAxis axis)
        {
            return axis != null ? axis.ActualPosition.ToString("0.######") : "<null>";
        }

        private int ReserveFocusWorkArea()
        {
            return ReserveFocusWorkArea(PickerWorkZone.Bottom, IsSelectedFront());
        }

        private int ReserveFocusWorkArea(PickerWorkZone zone, bool front)
        {
            try
            {
                ReleaseFocusWorkArea();
                string owner = ResolveFocusWorkAreaOwner();
                _focusWorkAreaScope = PickerZoneInterlockRules.BeginPickerWorkAreaUse(
                    front,
                    zone,
                    owner);
                EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-WORK-AREA",
                    "Vision Focus " + zone + " 작업 영역을 점유했습니다. 대상=" + BuildTargetLabel() +
                    ", owner=" + owner);
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-WORK-AREA-EX", "VisionFocusScanSequence", "Vision Focus " + zone + " 작업 영역 점유 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private string ResolveFocusWorkAreaOwner()
        {
            string updatedBy = _request != null ? _request.UpdatedBy : null;
            if (!string.IsNullOrWhiteSpace(updatedBy) &&
                updatedBy.IndexOf("ColletCalibration", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ColletCalibration";

            return "VisionFocusScanSequence";
        }

        private void ReleaseFocusWorkArea()
        {
            try
            {
                if (_focusWorkAreaScope != null)
                {
                    _focusWorkAreaScope.Dispose();
                    _focusWorkAreaScope = null;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "CAL", "VISION-FOCUS-CAL-WORK-AREA-RELEASE",
                    "Vision Focus 작업 영역 해제 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckFocusReadyPosition()
        {
            try
            {
                if (!_machine.InputStageUnit.IsVisionXInAvoidPosition())
                    return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-FINAL", "InputStageUnit", "Vision Focus 준비 최종 확인 실패: InputCamera가 Avoid 위치가 아닙니다.");
                if (!_machine.OutputStageUnit.IsVisionXInAvoidPosition())
                    return Fail("VISION-FOCUS-CAL-OUTPUT-CAMERA-FINAL", "OutputStageUnit", "Vision Focus 준비 최종 확인 실패: OutputCamera가 Avoid 위치가 아닙니다.");

                int result = CheckNonSelectedPickerOutputAvoid();
                if (result != 0)
                    return result;

                result = CheckSelectedPickerBottomPosition();
                if (result != 0)
                    return result;

                return CheckSelectedArmNonTargetPickerZSafe();
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-PREPARE-CHECK-EX", "VisionFocusScanSequence", "Vision Focus 준비 최종 확인 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckNonSelectedPickerOutputAvoid()
        {
            if (IsSelectedFront())
            {
                if (!_machine.PickerRearUnit.IsPickerInOutputSideAvoidPosition())
                    return Fail("VISION-FOCUS-CAL-REAR-OUTPUT-FINAL", "PickerRearUnit", "Vision Focus 준비 최종 확인 실패: RearPicker가 Output-side Avoid 위치가 아닙니다.");
            }
            else
            {
                if (!_machine.PickerFrontUnit.IsPickerInOutputSideAvoidPosition())
                    return Fail("VISION-FOCUS-CAL-FRONT-OUTPUT-FINAL", "PickerFrontUnit", "Vision Focus 준비 최종 확인 실패: FrontPicker가 Output-side Avoid 위치가 아닙니다.");
            }

            return 0;
        }

        private bool IsNonSelectedPickerOutputAvoid()
        {
            if (IsSelectedFront())
                return _machine != null && _machine.PickerRearUnit != null && _machine.PickerRearUnit.IsPickerInOutputSideAvoidPosition();

            return _machine != null && _machine.PickerFrontUnit != null && _machine.PickerFrontUnit.IsPickerInOutputSideAvoidPosition();
        }

        private bool CanSkipNonSelectedPickerOutputAvoidMove()
        {
            if (_machine == null)
                return false;

            if (IsSelectedFront())
            {
                if (_machine.PickerRearUnit == null)
                    return false;

                foreach (KeyValuePair<PickerAxis, BaseAxis> pair in _machine.PickerRearUnit.Axes)
                {
                    if (pair.Value == null)
                        return false;

                    pair.Value.UpdateStatus();
                    double target = _machine.PickerRearUnit.GetPickerTeachingPosition(pair.Key, "OutputAvoidPosition");
                    if (!AxisMoveWaiter.CanSkipMoveCommandAtTarget(pair.Value, target))
                        return false;
                }

                return true;
            }

            if (_machine.PickerFrontUnit == null)
                return false;

            foreach (KeyValuePair<PickerAxis, BaseAxis> pair in _machine.PickerFrontUnit.Axes)
            {
                if (pair.Value == null)
                    return false;

                pair.Value.UpdateStatus();
                double target = _machine.PickerFrontUnit.GetPickerTeachingPosition(pair.Key, "OutputAvoidPosition");
                if (!AxisMoveWaiter.CanSkipMoveCommandAtTarget(pair.Value, target))
                    return false;
            }

            return true;
        }

        private bool CanSkipSelectedPickerBottomMove()
        {
            if (_machine == null || _request == null)
                return false;

            int pickerIndex = NormalizePickerIndex(_request.PickerNo);
            double zTarget = ResolveBottomFocusStartZ();

            if (IsSelectedFront())
            {
                if (_machine.PickerFrontUnit == null)
                    return false;

                PickerAlignOffset offset = _machine.PickerFrontUnit.GetRuntimePickerOffset(pickerIndex) ?? new PickerAlignOffset();
                PickerCalibratedZoneTarget target = ResolveBottomZoneTarget(VisionFocusPickerSide.Front, pickerIndex, offset);
                UpdateFrontPickerBottomTargetStatus(target);
                return AxisMoveWaiter.CanSkipMoveCommandAtTarget(ResolveFrontPickerAxis(PickerAxis.PickerX), target.X) &&
                       AxisMoveWaiter.CanSkipMoveCommandAtTarget(ResolveFrontPickerAxis(PickerAxis.PickerY), target.Y) &&
                       AxisMoveWaiter.CanSkipMoveCommandAtTarget(ResolveFrontPickerAxis(target.PickerTAxis), target.T) &&
                       AxisMoveWaiter.CanSkipMoveCommandAtTarget(ResolveFrontPickerAxis(target.PickerZAxis), zTarget) &&
                       CanSkipFrontNonTargetPickerZAvoidMoves(target.PickerZAxis);
            }

            if (_machine.PickerRearUnit == null)
                return false;

            PickerAlignOffset rearOffset = _machine.PickerRearUnit.GetRuntimePickerOffset(pickerIndex) ?? new PickerAlignOffset();
            PickerCalibratedZoneTarget rearTarget = ResolveBottomZoneTarget(VisionFocusPickerSide.Rear, pickerIndex, rearOffset);
            UpdateRearPickerBottomTargetStatus(rearTarget);
            return AxisMoveWaiter.CanSkipMoveCommandAtTarget(ResolveRearPickerAxis(PickerAxis.PickerX), rearTarget.X) &&
                   AxisMoveWaiter.CanSkipMoveCommandAtTarget(ResolveRearPickerAxis(PickerAxis.PickerY), rearTarget.Y) &&
                   AxisMoveWaiter.CanSkipMoveCommandAtTarget(ResolveRearPickerAxis(rearTarget.PickerTAxis), rearTarget.T) &&
                   AxisMoveWaiter.CanSkipMoveCommandAtTarget(ResolveRearPickerAxis(rearTarget.PickerZAxis), zTarget) &&
                   CanSkipRearNonTargetPickerZAvoidMoves(rearTarget.PickerZAxis);
        }

        private bool CanSkipFrontNonTargetPickerZAvoidMoves(PickerAxis selectedZAxis)
        {
            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            foreach (PickerAxis zAxis in zAxes)
            {
                if (zAxis == selectedZAxis)
                    continue;

                BaseAxis item = ResolveFrontPickerAxis(zAxis);
                if (item == null)
                    return false;

                item.UpdateStatus();
                double avoidTarget = _machine.PickerFrontUnit.GetPickerTeachingPosition(zAxis, "AvoidPosition");
                if (!AxisMoveWaiter.CanSkipMoveCommandAtTarget(item, avoidTarget))
                    return false;
            }

            return true;
        }

        private bool CanSkipRearNonTargetPickerZAvoidMoves(PickerAxis selectedZAxis)
        {
            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            foreach (PickerAxis zAxis in zAxes)
            {
                if (zAxis == selectedZAxis)
                    continue;

                BaseAxis item = ResolveRearPickerAxis(zAxis);
                if (item == null)
                    return false;

                item.UpdateStatus();
                double avoidTarget = _machine.PickerRearUnit.GetPickerTeachingPosition(zAxis, "AvoidPosition");
                if (!AxisMoveWaiter.CanSkipMoveCommandAtTarget(item, avoidTarget))
                    return false;
            }

            return true;
        }

        private void UpdateFrontPickerBottomTargetStatus(PickerCalibratedZoneTarget target)
        {
            ResolveFrontPickerAxis(PickerAxis.PickerX)?.UpdateStatus();
            ResolveFrontPickerAxis(PickerAxis.PickerY)?.UpdateStatus();
            ResolveFrontPickerAxis(target.PickerTAxis)?.UpdateStatus();
            ResolveFrontPickerAxis(target.PickerZAxis)?.UpdateStatus();
        }

        private void UpdateRearPickerBottomTargetStatus(PickerCalibratedZoneTarget target)
        {
            ResolveRearPickerAxis(PickerAxis.PickerX)?.UpdateStatus();
            ResolveRearPickerAxis(PickerAxis.PickerY)?.UpdateStatus();
            ResolveRearPickerAxis(target.PickerTAxis)?.UpdateStatus();
            ResolveRearPickerAxis(target.PickerZAxis)?.UpdateStatus();
        }

        private bool IsSelectedPickerBottomPosition()
        {
            string detail;
            return IsSelectedPickerBottomPosition(out detail);
        }

        private bool IsSelectedPickerBottomPosition(out string detail)
        {
            detail = string.Empty;

            if (_machine == null || _request == null)
            {
                detail = "machine/request 정보가 없습니다.";
                return false;
            }

            if (IsSelectedFront())
                return IsFrontPickerBottomPosition(out detail);

            return IsRearPickerBottomPosition(out detail);
        }

        private bool IsFrontPickerBottomPosition(out string detail)
        {
            detail = string.Empty;
            if (_machine.PickerFrontUnit == null)
            {
                detail = "FrontPickerUnit이 없습니다.";
                return false;
            }

            int pickerIndex = NormalizePickerIndex(_request.PickerNo);
            PickerAlignOffset offset = _machine.PickerFrontUnit.GetRuntimePickerOffset(pickerIndex) ?? new PickerAlignOffset();
            PickerCalibratedZoneTarget bottomTarget = ResolveBottomZoneTarget(VisionFocusPickerSide.Front, pickerIndex, offset);

            double zTarget = ResolveBottomFocusStartZ();

            bool xOk = IsFrontPickerAxisInPosition(PickerAxis.PickerX, bottomTarget.X, out string xDetail);
            bool yOk = IsFrontPickerAxisInPosition(PickerAxis.PickerY, bottomTarget.Y, out string yDetail);
            bool tOk = IsFrontPickerAxisInPosition(bottomTarget.PickerTAxis, bottomTarget.T, out string tDetail);
            bool zOk = IsFrontPickerAxisInPosition(bottomTarget.PickerZAxis, zTarget, out string zDetail);

            detail = xDetail + ", " + yDetail + ", " + tDetail + ", " + zDetail;
            return xOk && yOk && tOk && zOk;
        }

        private bool IsRearPickerBottomPosition(out string detail)
        {
            detail = string.Empty;
            if (_machine.PickerRearUnit == null)
            {
                detail = "RearPickerUnit이 없습니다.";
                return false;
            }

            int pickerIndex = NormalizePickerIndex(_request.PickerNo);
            PickerAlignOffset offset = _machine.PickerRearUnit.GetRuntimePickerOffset(pickerIndex) ?? new PickerAlignOffset();
            PickerCalibratedZoneTarget bottomTarget = ResolveBottomZoneTarget(VisionFocusPickerSide.Rear, pickerIndex, offset);

            double zTarget = ResolveBottomFocusStartZ();

            bool xOk = IsRearPickerAxisInPosition(PickerAxis.PickerX, bottomTarget.X, out string xDetail);
            bool yOk = IsRearPickerAxisInPosition(PickerAxis.PickerY, bottomTarget.Y, out string yDetail);
            bool tOk = IsRearPickerAxisInPosition(bottomTarget.PickerTAxis, bottomTarget.T, out string tDetail);
            bool zOk = IsRearPickerAxisInPosition(bottomTarget.PickerZAxis, zTarget, out string zDetail);

            detail = xDetail + ", " + yDetail + ", " + tDetail + ", " + zDetail;
            return xOk && yOk && tOk && zOk;
        }

        private bool IsFrontPickerAxisInPosition(PickerAxis axis, double target, out string detail)
        {
            BaseAxis item = ResolveFrontPickerAxis(axis);
            double tolerance = ResolveAxisInPositionTolerance(item);
            bool ok = _machine.PickerFrontUnit.IsPickerAxisInPosition(axis, target, tolerance);
            detail = BuildAxisPositionDetail(axis, item, target, tolerance, ok);
            return ok;
        }

        private bool IsRearPickerAxisInPosition(PickerAxis axis, double target, out string detail)
        {
            BaseAxis item = ResolveRearPickerAxis(axis);
            double tolerance = ResolveAxisInPositionTolerance(item);
            bool ok = _machine.PickerRearUnit.IsPickerAxisInPosition(axis, target, tolerance);
            detail = BuildAxisPositionDetail(axis, item, target, tolerance, ok);
            return ok;
        }

        private static string BuildAxisPositionDetail(PickerAxis axis, BaseAxis item, double target, double tolerance, bool ok)
        {
            return axis + "(ok=" + ok +
                   ", actual=" + FormatAxisActual(item) +
                   ", target=" + target.ToString("0.###") +
                   ", tolerance=" + tolerance.ToString("0.######") +
                   ", alarm=" + (item != null && item.IsAlarm) +
                   ")";
        }

        private int CheckSelectedPickerBottomPosition()
        {
            if (IsSelectedFront())
            {
                string detail;
                if (!IsSelectedPickerBottomPosition(out detail))
                    return Fail("VISION-FOCUS-CAL-FRONT-BOTTOM-FINAL", "PickerFrontUnit",
                        "Vision Focus 준비 최종 확인 실패: 선택된 FrontPicker가 Bottom 위치가 아닙니다. pickerNo=" +
                        _request.PickerNo + ", " + detail);
            }
            else
            {
                string detail;
                if (!IsSelectedPickerBottomPosition(out detail))
                    return Fail("VISION-FOCUS-CAL-REAR-BOTTOM-FINAL", "PickerRearUnit",
                        "Vision Focus 준비 최종 확인 실패: 선택된 RearPicker가 Bottom 위치가 아닙니다. pickerNo=" +
                        _request.PickerNo + ", " + detail);
            }

            return 0;
        }

        private int CheckSelectedArmNonTargetPickerZSafe()
        {
            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (pickerNo == _request.PickerNo)
                    continue;

                PickerAxis axis = ResolvePickerZAxis(pickerNo);
                bool safe = IsSelectedFront()
                    ? _machine.PickerFrontUnit.IsPickerAxisInTeachingPosition(axis, "OutputAvoidPosition") || _machine.PickerFrontUnit.IsPickerAxisInTeachingPosition(axis, "AvoidPosition")
                    : _machine.PickerRearUnit.IsPickerAxisInTeachingPosition(axis, "OutputAvoidPosition") || _machine.PickerRearUnit.IsPickerAxisInTeachingPosition(axis, "AvoidPosition");
                if (!safe)
                {
                    string side = IsSelectedFront() ? "FrontPickerUnit" : "RearPickerUnit";
                    return Fail("VISION-FOCUS-CAL-SELECTED-ARM-Z-FINAL", side, "Vision Focus 준비 최종 확인 실패: 선택되지 않은 PickerZ가 안전 위치가 아닙니다. pickerNo=" + pickerNo);
                }
            }

            return 0;
        }

        private async Task<int> FocusStartStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                AutoVisionChannel channel = ResolveChannel();
                _useSimulatedVisionFocus = !VisionCommandService.IsConnected(channel);
                if (_useSimulatedVisionFocus)
                {
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-SIM",
                        "VisionPC가 연결되어 있지 않아 Focus Scan 점수를 시뮬레이션으로 생성합니다. channel=" +
                        channel + ", 대상=" + BuildTargetLabel());
                    CurrentStep = VisionFocusScanStep.MoveAndMeasure;
                    return 0;
                }

                VisionFocusStartResult result = await VisionCommandService.FocusStartAsync(
                    channel,
                    ResolveCameraName(),
                    ResolveTargetName(),
                    _request.VisionTimeoutMs,
                    ct).ConfigureAwait(false);

                if (result == null || !result.Success)
                    return Fail("VISION-FOCUS-CAL-FOCUS-START", "VisionFocusScanSequence",
                        "FOCUS_START 응답 실패. 대상=" + BuildTargetLabel() + ", raw=" + (result != null ? result.Raw : "null"));

                CurrentStep = VisionFocusScanStep.MoveAndMeasure;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-FOCUS-START-EX", "VisionFocusScanSequence", "FOCUS_START 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveAndMeasureStepAsync(CancellationToken ct)
        {
            try
            {
                _currentPassStartSampleIndex = Result.Samples.Count;

                // 첫 스캔 위치로 이동 + 안정화(측정 파이프라인 진입 전 1회). Fine 패스 재진입 시에도 동일.
                int moveResult = await MoveToScanPositionAsync(_scanPositions[0], ct).ConfigureAwait(false);
                if (moveResult != 0)
                    return moveResult;

                for (int i = 0; i < _scanPositions.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    double position = _scanPositions[i];

                    // EPD(노출 종료) 파이프라인 — 기본 모드는 FOCUS_VAL 그랩 ACK만 받고 점수는 FOCUS_BEST에서 회수한다.
                    // EPD 를 받으면 ACK 대기와 다음 위치 이동을 병렬로 시작해 '이동 ↔ 비전 처리' 가 겹치도록 한다.
                    // 경합 방지: EPD 대기 Task 는 명령 전송 '전'에 생성한다.
                    Task<bool> epdTask = _useSimulatedVisionFocus
                        ? null
                        : VisionCommandService.WaitExposureDoneAsync(ResolveChannel(), _request.VisionTimeoutMs);
                    Task<VisionFocusScanSample> ackTask = MeasureFocusValueAsync(Result.Samples.Count + 1, position, i == 0, ct);

                    // EPD 와 ACK 중 먼저 오는 쪽까지만 대기 — EPD 를 안 보내는 구버전 Vision 이어도 ACK 도착 즉시 진행된다.
                    Task<int> moveNextTask = null;
                    if (epdTask != null)
                    {
                        await Task.WhenAny(epdTask, ackTask).ConfigureAwait(false);
                        bool epdReceived = epdTask.Status == TaskStatus.RanToCompletion && epdTask.Result;
                        if (epdReceived && i + 1 < _scanPositions.Count)
                            moveNextTask = MoveToScanPositionAsync(_scanPositions[i + 1], ct);
                    }

                    VisionFocusScanSample sample = await ackTask.ConfigureAwait(false);
                    Result.Samples.Add(sample);
                    if (!sample.Success)
                    {
                        if (moveNextTask != null)
                        {
                            // 실패 보고는 FOCUS_VAL 기준 — 선행 이동은 완료만 기다려 축 상태를 정리한다.
                            try { await moveNextTask.ConfigureAwait(false); }
                            catch (Exception moveEx)
                            {
                                EventLogger.Write(EventKind.Warning, "CAL", "VISION-FOCUS-CAL-MOVE-NEXT",
                                    "FOCUS_VAL 실패 후 선행 이동 대기 중 예외: " + moveEx.Message);
                            }
                        }
                        return Fail("VISION-FOCUS-CAL-FOCUS-VAL", "VisionFocusScanSequence",
                            "FOCUS_VAL 응답 실패. 대상=" + BuildTargetLabel() +
                            ", position=" + position.ToString("F3") +
                            ", raw=" + sample.Raw);
                    }

                    // EPD 미수신(타임아웃/미연결/시뮬) 폴백 — ACK(그랩 완료) 후 순차 이동.
                    if (moveNextTask == null && i + 1 < _scanPositions.Count)
                        moveNextTask = MoveToScanPositionAsync(_scanPositions[i + 1], ct);

                    if (moveNextTask != null)
                    {
                        moveResult = await moveNextTask.ConfigureAwait(false);
                        if (moveResult != 0)
                            return moveResult;
                    }
                }

                CurrentStep = VisionFocusScanStep.FocusBest;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-MEASURE-EX", "VisionFocusScanSequence", "Focus 스캔 측정 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>스캔 위치 이동 + 완료 확인 + 안정화 대기(SettleDelayMs). 성공 0, 실패 결과코드.</summary>
        private async Task<int> MoveToScanPositionAsync(double position, CancellationToken ct)
        {
            try
            {
                int result = await MoveScanAxisAndVerifyAsync(position, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (_request.SettleDelayMs > 0)
                    await Task.Delay(_request.SettleDelayMs, ct).ConfigureAwait(false);
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-SCAN-MOVE-EX", "VisionFocusScanSequence",
                    "Focus 스캔 위치 이동 예외 발생. position=" + position.ToString("F3") + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> FocusBestStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_useSimulatedVisionFocus)
                {
                    ApplyBestFromSamples(_currentPassStartSampleIndex);
                    return MoveNextAfterBest();
                }

                VisionFocusBestResult best = await VisionCommandService.FocusBestAsync(
                    ResolveChannel(),
                    ResolveCameraName(),
                    ResolveTargetName(),
                    ResolvePickupNoForVision(),
                    ResolveVisionBestTimeoutMs(),
                    ct).ConfigureAwait(false);

                if (best == null || !best.Success)
                    return Fail("VISION-FOCUS-CAL-FOCUS-BEST", "VisionFocusScanSequence",
                        "FOCUS_BEST 응답 실패. 대상=" + BuildTargetLabel() + ", raw=" + (best != null ? best.Raw : "null"));

                Result.BestPosition = best.BestZ;
                Result.BestScore = best.BestScore;
                Result.SampleCount = Result.Samples.Count;
                return MoveNextAfterBest();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-FOCUS-BEST-EX", "VisionFocusScanSequence", "FOCUS_BEST 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private int MoveNextAfterBest()
        {
            try
            {
                if (_scanPass == 0 && IsFineScanEnabled())
                {
                    _roughBestPosition = Result.BestPosition;
                    _roughBestScore = Result.BestScore;
                    _scanPass = 1;
                    BuildFineScanPositions(_roughBestPosition);
                    if (_scanPositions.Count == 0)
                        return Fail("VISION-FOCUS-CAL-NO-FINE-SAMPLE", "VisionFocusScanSequence",
                            "Vision Focus Cal Fine 스캔 위치가 없습니다. roughBest=" + _roughBestPosition.ToString("F3"));

                    EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-FINE-START",
                        "Rough Best Focus 기준으로 Fine Scan을 시작합니다. 대상=" + BuildTargetLabel() +
                        ", roughBest=" + _roughBestPosition.ToString("F3") +
                        ", roughScore=" + _roughBestScore.ToString("F4") +
                        ", fineMinus=" + _request.FineMinusRange +
                        ", finePlus=" + _request.FinePlusRange +
                        ", fineStep=" + _request.FineStep);

                    CurrentStep = VisionFocusScanStep.FocusStart;
                    return 0;
                }

                Result.SampleCount = Result.Samples.Count;
                CurrentStep = VisionFocusScanStep.SaveBest;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-NEXT-BEST-EX", "VisionFocusScanSequence", "Vision Focus Cal Best 후 다음 단계 결정 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private int SaveBestStep()
        {
            try
            {
                VisionFocusCalibrationData data = _machine.VisionUnit.Config.FocusCalibration;
                data.EnsureObjects();
                VisionFocusPositionRecord record = ResolveSaveRecord(data);
                record.ApplyBest(
                    _request.DefaultPosition,
                    Result.BestPosition,
                    Result.BestScore,
                    Result.SampleCount,
                    _request.UpdatedBy);

                if (!IsBottomFocusKind())
                {
                    // Side AF는 촬영 당시 사용한 PickerZ 위치를 함께 저장해 이후 AF 시작 위치로 재사용한다.
                    // Best(BestPosition)는 해당 Side Vision Y의 초점 위치다.
                    // Picker 측 선택은 요청 PickerSide가 아니라 Kind 기준으로 결정한다(다이얼로그 측 선택 불일치 방지).
                    bool frontSideKind = _request.Kind == VisionFocusScanKind.FrontSide0 ||
                                         _request.Kind == VisionFocusScanKind.FrontSide90;
                    BaseAxis pickerZ = frontSideKind
                        ? ResolveFrontPickerAxis(ResolvePickerZAxis())
                        : ResolveRearPickerAxis(ResolvePickerZAxis());
                    // 초점 신호가 없는 스캔(score<=0)은 위치 저장에서 제외해 무효값이 다음 AF 시작 위치로 쓰이는 것을 막는다.
                    bool focusMeaningful = Result.BestScore > 0.0 && Result.SampleCount > 0;
                    if (pickerZ != null && focusMeaningful)
                    {
                        record.ApplyPickerZ(pickerZ.ActualPosition);
                        QMC.Common.Log.Write("Calibration", "SYSTEM", "VisionFocusSideSave",
                            "Side AF PickerZ/VisionY 저장. kind=" + _request.Kind +
                            ", pickerNo=" + _request.PickerNo +
                            ", pickerZ=" + record.PickerZPosition.ToString("F6") +
                            ", visionYBest=" + record.BestPosition.ToString("F6") +
                            ", score=" + record.BestScore.ToString("F6"));
                    }
                    else
                    {
                        QMC.Common.Log.Write("Calibration", "SYSTEM", "VisionFocusSideSave",
                            "Side AF PickerZ 저장을 건너뜁니다. 기존 저장값을 유지합니다. kind=" + _request.Kind +
                            ", pickerNo=" + _request.PickerNo +
                            ", reason=" + (pickerZ == null ? "PickerZ 축 확인 불가" : "초점 score/샘플 없음") +
                            ", score=" + Result.BestScore.ToString("F6") +
                            ", sample=" + Result.SampleCount);
                    }
                }

                CurrentStep = _request.ReturnToDefaultAfterScan
                    ? VisionFocusScanStep.ReturnDefault
                    : VisionFocusScanStep.Complete;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-SAVE-EX", "VisionFocusScanSequence", "Vision Focus Cal 결과 저장 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ReturnDefaultStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int result = await MoveScanAxisAndVerifyAsync(_request.DefaultPosition, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = VisionFocusScanStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-RETURN-EX", "VisionFocusScanSequence", "Focus 스캔 후 Default 위치 복귀 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveScanAxisAndVerifyAsync(double position, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsBottomFocusKind())
                    return await MovePickerZAndVerifyAsync(position, ct).ConfigureAwait(false);

                return await MoveSideVisionYAndVerifyAsync(position, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-MOVE-EX", "VisionFocusScanSequence",
                    "Focus 스캔 축 이동 예외 발생. position=" + position + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MovePickerZAndVerifyAsync(double position, CancellationToken ct)
        {
            PickerAxis axis = ResolvePickerZAxis();
            int result;
            AxisMoveWaitResult wait;
            if (_request.PickerSide == VisionFocusPickerSide.Front)
            {
                BaseAxis pickerZ = ResolveFrontPickerAxis(axis);
                double tolerance = ResolveAxisInPositionTolerance(pickerZ);
                if (IsAxisIdleAtExactPosition(pickerZ, position))
                {
                    LogSkipMove("PickerZ", "Front", axis.ToString(), position, ExactMoveSkipToleranceMm);
                    return 0;
                }

                result = await _machine.PickerFrontUnit.MovePickerAxisCommandWithMotion(
                    axis,
                    position,
                    _request.MoveVelocity,
                    _request.MoveAcceleration,
                    _request.MoveDeceleration,
                    ResolveBottomMotionCommandTag(),
                    true).ConfigureAwait(false);
                if (result != 0)
                    return FailPickerZCommand("Front", position, result);

                wait = await WaitAxisMoveDoneInPositionAsync(pickerZ, position, _request.MotionTimeoutMs, ct).ConfigureAwait(false);
                if (!wait.Success)
                    return FailPickerZWait("Front", position, wait);

                if (!_machine.PickerFrontUnit.IsPickerAxisInPosition(axis, position, tolerance))
                    return FailPickerZFinal("Front", position);
            }
            else
            {
                BaseAxis pickerZ = ResolveRearPickerAxis(axis);
                double tolerance = ResolveAxisInPositionTolerance(pickerZ);
                if (IsAxisIdleAtExactPosition(pickerZ, position))
                {
                    LogSkipMove("PickerZ", "Rear", axis.ToString(), position, ExactMoveSkipToleranceMm);
                    return 0;
                }

                result = await _machine.PickerRearUnit.MovePickerAxisCommandWithMotion(
                    axis,
                    position,
                    _request.MoveVelocity,
                    _request.MoveAcceleration,
                    _request.MoveDeceleration,
                    ResolveBottomMotionCommandTag(),
                    true).ConfigureAwait(false);
                if (result != 0)
                    return FailPickerZCommand("Rear", position, result);

                wait = await WaitAxisMoveDoneInPositionAsync(pickerZ, position, _request.MotionTimeoutMs, ct).ConfigureAwait(false);
                if (!wait.Success)
                    return FailPickerZWait("Rear", position, wait);

                if (!_machine.PickerRearUnit.IsPickerAxisInPosition(axis, position, tolerance))
                    return FailPickerZFinal("Rear", position);
            }

            return 0;
        }

        private async Task<int> MoveSideVisionYAndVerifyAsync(double position, CancellationToken ct)
        {
            VisionAxis axis = ResolveSideVisionAxis();
            BaseAxis visionAxis = _machine.VisionUnit.ResolveVisionAxis(axis);
            double tolerance = ResolveAxisInPositionTolerance(visionAxis);
            if (IsAxisIdleAtExactPosition(visionAxis, position))
            {
                LogSkipMove("SideVisionY", ResolveCameraName(), axis.ToString(), position, ExactMoveSkipToleranceMm);
                return 0;
            }

            int result = await _machine.VisionUnit.MoveVisionAxisCommandWithMotion(
                axis,
                position,
                _request.MoveVelocity,
                _request.MoveAcceleration,
                _request.MoveDeceleration,
                "VisionFocusCal;Side",
                true).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-SIDE-Y-MOVE", "VisionFocusScanSequence",
                    "Focus 스캔 SideVisionY 이동 명령 실패. axis=" + axis +
                    ", position=" + position +
                    ", velocity=" + _request.MoveVelocity +
                    ", acc=" + _request.MoveAcceleration +
                    ", dec=" + _request.MoveDeceleration +
                    ", result=" + result);

            AxisMoveWaitResult wait = await WaitAxisMoveDoneInPositionAsync(visionAxis, position, _request.MotionTimeoutMs, ct).ConfigureAwait(false);
            if (!wait.Success)
                return Fail("VISION-FOCUS-CAL-SIDE-Y-WAIT", "VisionFocusScanSequence",
                    "Focus 스캔 SideVisionY 이동 완료 확인 실패. axis=" + axis +
                    ", position=" + position + ", reason=" + wait.Reason);

            if (!_machine.VisionUnit.IsVisionAxisInPosition(axis, position, tolerance))
                return Fail("VISION-FOCUS-CAL-SIDE-Y-FINAL", "VisionFocusScanSequence",
                    "Focus 스캔 SideVisionY 최종 위치 확인 실패. axis=" + axis +
                    ", position=" + position +
                    ", tolerance=" + tolerance);

            return 0;
        }

        private static bool IsAxisIdleInPosition(BaseAxis axis)
        {
            return axis != null && !axis.IsMoving;
        }

        private void LogSkipMove(string motionKind, string side, string axisName, double position, double tolerance)
        {
            EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-MOVE-SKIP",
                "Focus 스캔 축이 이미 목표 위치에 있어 이동 명령을 생략합니다. kind=" + motionKind +
                ", side=" + side +
                ", axis=" + axisName +
                ", position=" + position.ToString("F6") +
                ", tolerance=" + tolerance.ToString("0.######") +
                ", 대상=" + BuildTargetLabel());
        }

        private int FailPickerZCommand(string side, double position, int result)
        {
            return Fail("VISION-FOCUS-CAL-PICKER-Z-MOVE", "VisionFocusScanSequence",
                "Focus 스캔 PickerZ 이동 명령 실패. side=" + side +
                ", pickerNo=" + _request.PickerNo +
                ", position=" + position +
                ", velocity=" + _request.MoveVelocity +
                ", acc=" + _request.MoveAcceleration +
                ", dec=" + _request.MoveDeceleration +
                ", result=" + result);
        }

        private int FailPickerZWait(string side, double position, AxisMoveWaitResult wait)
        {
            return Fail("VISION-FOCUS-CAL-PICKER-Z-WAIT", "VisionFocusScanSequence",
                "Focus 스캔 PickerZ 이동 완료 확인 실패. side=" + side +
                ", pickerNo=" + _request.PickerNo +
                ", position=" + position +
                ", reason=" + (wait != null ? wait.Reason : "wait result is null"));
        }

        private int FailPickerZFinal(string side, double position)
        {
            return Fail("VISION-FOCUS-CAL-PICKER-Z-FINAL", "VisionFocusScanSequence",
                "Focus 스캔 PickerZ 최종 위치 확인 실패. side=" + side +
                ", pickerNo=" + _request.PickerNo +
                ", position=" + position);
        }

        private async Task<VisionFocusScanSample> MeasureFocusValueAsync(int no, double position, bool initial, CancellationToken ct)
        {
            if (_useSimulatedVisionFocus)
            {
                double center = _scanPass == 0 ? _request.DefaultPosition : _roughBestPosition;
                double distance = position - center;
                double score = Math.Max(0.0, 1.0 - Math.Abs(distance) * 1.0 + _simRandom.NextDouble() * 0.01);
                score = Math.Min(1.0, score);
                return new VisionFocusScanSample
                {
                    No = no,
                    Position = position,
                    Score = score,
                    Success = true,
                    Raw = "Simulation"
                };
            }

            VisionFocusValueResult value = await VisionCommandService.FocusValueAsync(
                ResolveChannel(),
                position,
                ResolveCameraName(),
                ResolveTargetName(),
                ResolvePickupNoForVision(),
                initial,
                _request.VisionTimeoutMs,
                ct,
                _request.FocusValueReceiveMode == VisionFocusValueReceiveMode.WaitResultForTest).ConfigureAwait(false);

            return new VisionFocusScanSample
            {
                No = no,
                Position = position,
                Score = value != null ? value.Score : 0.0,
                Success = value != null && value.Success,
                Raw = value != null ? value.Raw : "null"
            };
        }

        private void BuildRoughScanPositions()
        {
            BuildScanPositions(_request.DefaultPosition, _request.MinusRange, _request.PlusRange, _request.Step);
        }

        private void BuildFineScanPositions(double centerPosition)
        {
            BuildScanPositions(centerPosition, _request.FineMinusRange, _request.FinePlusRange, _request.FineStep);
        }

        private void BuildScanPositions(double centerPosition, double minusRange, double plusRange, double stepValue)
        {
            _scanPositions.Clear();
            double start = centerPosition - minusRange;
            double end = centerPosition + plusRange;
            double step = Math.Abs(stepValue);
            int repeatCount = _request.RepeatCount <= 0 ? 1 : Math.Min(_request.RepeatCount, 100);
            int count = 0;

            for (int repeat = 0; repeat < repeatCount && count < MaxSampleCount; repeat++)
            {
                for (double pos = start; pos <= end + 0.000001 && count < MaxSampleCount; pos += step)
                {
                    _scanPositions.Add(Math.Round(pos, 6));
                    count++;
                }
            }
        }

        private bool IsFineScanEnabled()
        {
            return _request != null &&
                   (_request.FineMinusRange > 0.0 || _request.FinePlusRange > 0.0) &&
                   _request.FineStep > 0.0;
        }

        private void ApplyBestFromSamples(int startIndex)
        {
            VisionFocusScanSample best = null;
            int safeStartIndex = Math.Max(0, startIndex);
            for (int i = safeStartIndex; i < Result.Samples.Count; i++)
            {
                VisionFocusScanSample sample = Result.Samples[i];
                if (best == null || sample.Score > best.Score)
                    best = sample;
            }

            if (best != null)
            {
                Result.BestPosition = best.Position;
                Result.BestScore = best.Score;
                Result.SampleCount = Result.Samples.Count;
            }
        }

        private VisionFocusPositionRecord ResolveSaveRecord(VisionFocusCalibrationData data)
        {
            if (IsBottomFocusKind())
                return data.GetBottomRecord(_request.Kind, _request.PickerSide, _request.PickerNo);

            return data.GetSideRecord(_request.Kind, _request.PickerNo);
        }

        private AutoVisionChannel ResolveChannel()
        {
            if (IsBottomFocusKind())
                return AutoVisionChannel.BottomInspection;
            if (_request.Kind == VisionFocusScanKind.FrontSide0 || _request.Kind == VisionFocusScanKind.FrontSide90)
                return AutoVisionChannel.FrontSide;
            return AutoVisionChannel.RearSide;
        }

        private string ResolveCameraName()
        {
            if (IsBottomFocusKind())
                return "BOTTOM";
            if (_request.Kind == VisionFocusScanKind.FrontSide0 || _request.Kind == VisionFocusScanKind.FrontSide90)
                return "FRONT";
            return "BACK";
        }

        private string ResolveTargetName()
        {
            if (_request.Kind == VisionFocusScanKind.BottomCollet)
                return "COLLET";
            if (_request.Kind == VisionFocusScanKind.BottomDie)
                return "DIE";
            return "SIDE";
        }

        private int ResolvePickupNoForVision()
        {
            return IsBottomFocusKind() ? _request.PickerNo : 0;
        }

        private string ResolveBottomMotionCommandTag()
        {
            return _request.Kind == VisionFocusScanKind.BottomDie
                ? "VisionFocusCal;BottomDie"
                : "VisionFocusCal;BottomCollet";
        }

        private double ResolveBottomFocusStartZ()
        {
            return _request != null
                ? _request.DefaultPosition
                : 0.0;
        }

        private PickerAxis ResolvePickerZAxis()
        {
            return ResolvePickerZAxis(_request.PickerNo);
        }

        private PickerAxis ResolvePickerZAxis(int pickerNo)
        {
            switch (pickerNo)
            {
                case 1: return PickerAxis.PickerZ0;
                case 2: return PickerAxis.PickerZ1;
                case 3: return PickerAxis.PickerZ2;
                default: return PickerAxis.PickerZ3;
            }
        }

        private PickerAxis ResolvePickerTAxis(int pickerNo)
        {
            switch (pickerNo)
            {
                case 1: return PickerAxis.PickerT0;
                case 2: return PickerAxis.PickerT1;
                case 3: return PickerAxis.PickerT2;
                default: return PickerAxis.PickerT3;
            }
        }

        private int NormalizePickerIndex(int pickerNo)
        {
            if (pickerNo < 1)
                return 0;
            if (pickerNo > 4)
                return 3;
            return pickerNo - 1;
        }

        private PickerCalibratedZoneTarget ResolveBottomZoneTarget(
            VisionFocusPickerSide side,
            int pickerIndex,
            PickerAlignOffset offset)
        {
            return CalibrationCoordinateService.ResolvePickerZoneTarget(
                _machine,
                side,
                "DieBottomPosition",
                pickerIndex,
                offset,
                true,
                true);
        }

        private int ResolveMotionTimeoutMs()
        {
            return _request != null && _request.MotionTimeoutMs > 0 ? _request.MotionTimeoutMs : 5000;
        }

        private int ResolveVisionBestTimeoutMs()
        {
            return _request != null && _request.VisionBestTimeoutMs > 0 ? _request.VisionBestTimeoutMs : 120000;
        }

        private bool IsSelectedFront()
        {
            return _request == null || _request.PickerSide == VisionFocusPickerSide.Front;
        }

        private bool IsBottomFocusKind()
        {
            return _request != null &&
                   (_request.Kind == VisionFocusScanKind.BottomCollet ||
                    _request.Kind == VisionFocusScanKind.BottomDie);
        }

        private VisionAxis ResolveSideVisionAxis()
        {
            if (_request.Kind == VisionFocusScanKind.FrontSide0 || _request.Kind == VisionFocusScanKind.FrontSide90)
                return VisionAxis.FrontSideVisionY;
            return VisionAxis.RearSideVisionY;
        }

        private string BuildTargetLabel()
        {
            if (_request == null)
                return "-";
            if (_request.Kind == VisionFocusScanKind.BottomCollet)
                return _request.PickerSide + " Collet #" + _request.PickerNo;
            if (_request.Kind == VisionFocusScanKind.BottomDie)
                return _request.PickerSide + " Die #" + _request.PickerNo;
            return _request.Kind.ToString();
        }

        private int Fail(string code, string source, string message)
        {
            Result.Success = false;
            Result.Message = message;
            EventLogger.Write(EventKind.Alarm, "CAL", code, message);
            AlarmManager.Raise(AlarmSeverity.Error, code, source, message);
            return -1;
        }
    }
}
