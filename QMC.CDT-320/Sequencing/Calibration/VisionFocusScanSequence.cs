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
        public int RepeatCount { get; set; } = 1;
        public double MoveVelocity { get; set; } = 30.0;
        public double MoveAcceleration { get; set; } = 300.0;
        public double MoveDeceleration { get; set; } = 300.0;
        public int SettleDelayMs { get; set; } = 50;
        public int MotionTimeoutMs { get; set; } = 5000;
        public int VisionTimeoutMs { get; set; } = 5000;
        public bool ReturnToDefaultAfterScan { get; set; } = true;
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

        private readonly CDT320_Machine _machine;
        private readonly VisionFocusScanRequest _request;
        private readonly List<double> _scanPositions = new List<double>();
        private readonly Random _simRandom = new Random();
        private IDisposable _focusWorkAreaScope;

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
            try
            {
                ct.ThrowIfCancellationRequested();
                CurrentStep = VisionFocusScanStep.CheckUnit;

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
                                 ", best=" + Result.BestPosition.ToString("F4") +
                                 ", score=" + Result.BestScore.ToString("F2") +
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

        public async Task<int> MoveDefaultOnlyAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int checkResult = CheckCommonCondition(false);
                if (checkResult != 0)
                    return checkResult;

                int prepareResult = await PrepareFocusReadyPositionAsync(ct).ConfigureAwait(false);
                if (prepareResult != 0)
                    return prepareResult;

                int result = await MoveScanAxisAndVerifyAsync(_request.DefaultPosition, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                Result.Success = true;
                Result.BestPosition = _request.DefaultPosition;
                Result.Message = "Default Position 이동 완료. 대상=" + BuildTargetLabel() +
                                 ", position=" + _request.DefaultPosition.ToString("F4");
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

                BuildScanPositions();
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
                if (_request.Step <= 0)
                    return Fail("VISION-FOCUS-CAL-BAD-STEP", "VisionFocusScanSequence", "Vision Focus Cal Step 값은 0보다 커야 합니다. step=" + _request.Step);
                if (_request.MinusRange < 0 || _request.PlusRange < 0)
                    return Fail("VISION-FOCUS-CAL-BAD-RANGE", "VisionFocusScanSequence", "Vision Focus Cal 스캔 범위가 올바르지 않습니다. minus=" + _request.MinusRange + ", plus=" + _request.PlusRange);
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

                int result = await EnsureInputOutputVisionAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveNonSelectedPickerOutputAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveSelectedPickerOutputAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = ReserveFocusWorkArea();
                if (result != 0)
                    return result;

                result = await MoveSelectedPickerBottomPositionAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = CheckFocusReadyPosition();
                if (result != 0)
                    return result;

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
                    return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-MISSING", "InputStageUnit", "InputCamera Avoid 이동을 위한 축/Recipe 정보가 없습니다.");

                double target = stage.Recipe.VisionX.AvoidPosition;
                if (stage.IsVisionXInAvoidPosition())
                    return 0;

                int result = await stage.MoveInputStageAxis(WaferStageAxis.VisionX, target, true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-MOVE", "InputStageUnit", "InputCamera Avoid 이동 명령 실패. result=" + result + ", target=" + target.ToString("F3"));

                result = await stage.WaitInputStageAxisInPosition(WaferStageAxis.VisionX, target, ResolveMotionTimeoutMs(), ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-WAIT", "InputStageUnit", "InputCamera Avoid 이동 완료 확인 실패. result=" + result + ", target=" + target.ToString("F3"));

                if (!stage.IsVisionXInAvoidPosition())
                    return Fail("VISION-FOCUS-CAL-INPUT-CAMERA-CHECK", "InputStageUnit", "InputCamera Avoid 최종 위치 확인 실패. actual=" + stage.CameraX.ActualPosition.ToString("F3") + ", target=" + target.ToString("F3"));

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
                if (stage == null || stage.OutputCameraX == null)
                    return Fail("VISION-FOCUS-CAL-OUTPUT-CAMERA-MISSING", "OutputStageUnit", "OutputCamera Avoid 이동을 위한 축 정보가 없습니다.");

                if (stage.IsVisionXInAvoidPosition())
                    return 0;

                int result = await stage.MoveVisionXToAvoidAndVerifyAsync(ResolveMotionTimeoutMs(), true, ct).ConfigureAwait(false);
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

                    if (!_machine.PickerRearUnit.IsPickerInUnloadPosition())
                        return Fail("VISION-FOCUS-CAL-REAR-OUTPUT-CHECK", "PickerRearUnit", "선택되지 않은 RearPicker Output-side Avoid 최종 위치 확인 실패.");
                }
                else
                {
                    int result = await MoveFrontPickerOutputAvoidSequentialAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-FRONT-OUTPUT-AVOID", "PickerFrontUnit", "선택되지 않은 FrontPicker Output-side Avoid 이동 실패. result=" + result);

                    if (!_machine.PickerFrontUnit.IsPickerInUnloadPosition())
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

                    if (!_machine.PickerFrontUnit.IsPickerInUnloadPosition())
                        return Fail("VISION-FOCUS-CAL-FRONT-SELECTED-OUTPUT-CHECK", "PickerFrontUnit", "선택된 FrontPicker Output-side Avoid 최종 위치 확인 실패.");
                }
                else
                {
                    int result = await MoveRearPickerOutputAvoidSequentialAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-REAR-SELECTED-OUTPUT-AVOID", "PickerRearUnit", "선택된 RearPicker Output-side Avoid 이동 실패. result=" + result);

                    if (!_machine.PickerRearUnit.IsPickerInUnloadPosition())
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

                    if (!_machine.PickerFrontUnit.IsPickerInDieProcessPosition(_request.PickerNo))
                        return Fail("VISION-FOCUS-CAL-FRONT-BOTTOM-CHECK", "PickerFrontUnit", "선택된 FrontPicker Bottom 위치 최종 확인 실패. pickerNo=" + _request.PickerNo);
                }
                else
                {
                    int result = await MoveRearPickerBottomSequentialAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                        return Fail("VISION-FOCUS-CAL-REAR-BOTTOM-MOVE", "PickerRearUnit", "선택된 RearPicker Bottom 위치 이동 실패. pickerNo=" + _request.PickerNo + ", result=" + result);

                    if (!_machine.PickerRearUnit.IsPickerInDieProcessPosition(_request.PickerNo))
                        return Fail("VISION-FOCUS-CAL-REAR-BOTTOM-CHECK", "PickerRearUnit", "선택된 RearPicker Bottom 위치 최종 확인 실패. pickerNo=" + _request.PickerNo);
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

                int pickerIndex = NormalizePickerIndex(_request.PickerNo);
                PickerAxis tAxis = ResolvePickerTAxis(_request.PickerNo);
                PickerAxis zAxis = ResolvePickerZAxis(_request.PickerNo);
                var offset = _machine.PickerFrontUnit.GetRuntimePickerOffset(pickerIndex) ?? new PickerAlignOffset();

                result = await MoveFrontPickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    ResolveFrontBottomX(pickerIndex, offset),
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeX",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    _machine.PickerFrontUnit.GetPickerTeachingPosition(PickerAxis.PickerY, "BottomPosition") + offset.AlignOffsetY,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveFrontPickerAxisAndVerifyAsync(
                    tAxis,
                    _machine.PickerFrontUnit.GetPickerTeachingPosition(tAxis, "BottomPosition") + offset.AlignOffsetT,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeT",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await MoveFrontPickerAxisAndVerifyAsync(
                    zAxis,
                    _machine.PickerFrontUnit.GetPickerTeachingPosition(zAxis, "BottomPosition"),
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeZ",
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

                int pickerIndex = NormalizePickerIndex(_request.PickerNo);
                PickerAxis tAxis = ResolvePickerTAxis(_request.PickerNo);
                PickerAxis zAxis = ResolvePickerZAxis(_request.PickerNo);
                var offset = _machine.PickerRearUnit.GetRuntimePickerOffset(pickerIndex) ?? new PickerAlignOffset();

                result = await MoveRearPickerAxisAndVerifyAsync(
                    PickerAxis.PickerX,
                    ResolveRearBottomX(pickerIndex, offset),
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeX",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerAxisAndVerifyAsync(
                    PickerAxis.PickerY,
                    _machine.PickerRearUnit.GetPickerTeachingPosition(PickerAxis.PickerY, "BottomPosition") + offset.AlignOffsetY,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeY",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveRearPickerAxisAndVerifyAsync(
                    tAxis,
                    _machine.PickerRearUnit.GetPickerTeachingPosition(tAxis, "BottomPosition") + offset.AlignOffsetT,
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeT",
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await MoveRearPickerAxisAndVerifyAsync(
                    zAxis,
                    _machine.PickerRearUnit.GetPickerTeachingPosition(zAxis, "BottomPosition"),
                    "VisionFocusCal;DieBottomPosition;PickerPhase=SafeZ",
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
                return Fail("VISION-FOCUS-CAL-FRONT-Z-GROUP", "PickerFrontUnit", description + " 실패. axis=PickerZ0, result=" + result);
            result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ1, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-FRONT-Z-GROUP", "PickerFrontUnit", description + " 실패. axis=PickerZ1, result=" + result);
            result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ2, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-FRONT-Z-GROUP", "PickerFrontUnit", description + " 실패. axis=PickerZ2, result=" + result);
            result = await MoveFrontPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ3, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-FRONT-Z-GROUP", "PickerFrontUnit", description + " 실패. axis=PickerZ3, result=" + result);
            return 0;
        }

        private async Task<int> MoveRearPickerZGroupTeachingAsync(string positionName, string description, CancellationToken ct)
        {
            int result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ0, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-REAR-Z-GROUP", "PickerRearUnit", description + " 실패. axis=PickerZ0, result=" + result);
            result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ1, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-REAR-Z-GROUP", "PickerRearUnit", description + " 실패. axis=PickerZ1, result=" + result);
            result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ2, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-REAR-Z-GROUP", "PickerRearUnit", description + " 실패. axis=PickerZ2, result=" + result);
            result = await MoveRearPickerTeachingAxisAndVerifyAsync(PickerAxis.PickerZ3, positionName, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-REAR-Z-GROUP", "PickerRearUnit", description + " 실패. axis=PickerZ3, result=" + result);
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
            int result = await _machine.PickerFrontUnit.MovePickerAxisCommand(axis, target, true, targetName).ConfigureAwait(false);
            if (result != 0)
                return result;

            AxisMoveWaitResult wait = await _machine.PickerFrontUnit.WaitPickerAxisMoveDoneInPosition(axis, target, ResolveMotionTimeoutMs(), ct).ConfigureAwait(false);
            if (!wait.Success)
                return Fail("VISION-FOCUS-CAL-FRONT-AXIS-WAIT", "PickerFrontUnit", "FrontPicker 축 이동 완료 확인 실패. axis=" + axis + ", target=" + target.ToString("F3") + ", reason=" + wait.Reason);

            if (!_machine.PickerFrontUnit.IsPickerAxisInPosition(axis, target, 0.01))
                return Fail("VISION-FOCUS-CAL-FRONT-AXIS-FINAL", "PickerFrontUnit", "FrontPicker 축 최종 위치 확인 실패. axis=" + axis + ", target=" + target.ToString("F3"));

            return 0;
        }

        private async Task<int> MoveRearPickerAxisAndVerifyAsync(PickerAxis axis, double target, string targetName, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            int result = await _machine.PickerRearUnit.MovePickerAxisCommand(axis, target, true, targetName).ConfigureAwait(false);
            if (result != 0)
                return result;

            AxisMoveWaitResult wait = await _machine.PickerRearUnit.WaitPickerAxisMoveDoneInPosition(axis, target, ResolveMotionTimeoutMs(), ct).ConfigureAwait(false);
            if (!wait.Success)
                return Fail("VISION-FOCUS-CAL-REAR-AXIS-WAIT", "PickerRearUnit", "RearPicker 축 이동 완료 확인 실패. axis=" + axis + ", target=" + target.ToString("F3") + ", reason=" + wait.Reason);

            if (!_machine.PickerRearUnit.IsPickerAxisInPosition(axis, target, 0.01))
                return Fail("VISION-FOCUS-CAL-REAR-AXIS-FINAL", "PickerRearUnit", "RearPicker 축 최종 위치 확인 실패. axis=" + axis + ", target=" + target.ToString("F3"));

            return 0;
        }

        private int ReserveFocusWorkArea()
        {
            try
            {
                ReleaseFocusWorkArea();
                _focusWorkAreaScope = PickerZoneInterlockRules.BeginPickerWorkAreaUse(
                    IsSelectedFront(),
                    PickerWorkZone.Bottom,
                    "VisionFocusScanSequence");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("VISION-FOCUS-CAL-WORK-AREA-EX", "VisionFocusScanSequence", "Vision Focus Bottom 작업 영역 점유 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
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
                    "Vision Focus Bottom 작업 영역 해제 중 예외 발생: " + ex.Message);
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
                if (!_machine.PickerRearUnit.IsPickerInUnloadPosition())
                    return Fail("VISION-FOCUS-CAL-REAR-OUTPUT-FINAL", "PickerRearUnit", "Vision Focus 준비 최종 확인 실패: RearPicker가 Output-side Avoid 위치가 아닙니다.");
            }
            else
            {
                if (!_machine.PickerFrontUnit.IsPickerInUnloadPosition())
                    return Fail("VISION-FOCUS-CAL-FRONT-OUTPUT-FINAL", "PickerFrontUnit", "Vision Focus 준비 최종 확인 실패: FrontPicker가 Output-side Avoid 위치가 아닙니다.");
            }

            return 0;
        }

        private int CheckSelectedPickerBottomPosition()
        {
            if (IsSelectedFront())
            {
                if (!_machine.PickerFrontUnit.IsPickerInDieProcessPosition(_request.PickerNo))
                    return Fail("VISION-FOCUS-CAL-FRONT-BOTTOM-FINAL", "PickerFrontUnit", "Vision Focus 준비 최종 확인 실패: 선택된 FrontPicker가 Bottom 위치가 아닙니다. pickerNo=" + _request.PickerNo);
            }
            else
            {
                if (!_machine.PickerRearUnit.IsPickerInDieProcessPosition(_request.PickerNo))
                    return Fail("VISION-FOCUS-CAL-REAR-BOTTOM-FINAL", "PickerRearUnit", "Vision Focus 준비 최종 확인 실패: 선택된 RearPicker가 Bottom 위치가 아닙니다. pickerNo=" + _request.PickerNo);
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
                if (IsVisionBypassed())
                {
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-FOCUS-CAL-BYPASS",
                        "Simulation/DryRun 상태라 FOCUS_START 통신을 생략합니다. 대상=" + BuildTargetLabel());
                    CurrentStep = VisionFocusScanStep.MoveAndMeasure;
                    return 0;
                }

                AutoVisionChannel channel = ResolveChannel();
                if (!VisionCommandService.IsConnected(channel))
                    return Fail("VISION-FOCUS-CAL-NOT-CONNECTED", "VisionFocusScanSequence",
                        "VisionPC가 연결되어 있지 않아 Focus 측정을 실행할 수 없습니다. channel=" + channel);

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
                Result.Samples.Clear();
                for (int i = 0; i < _scanPositions.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    double position = _scanPositions[i];
                    int moveResult = await MoveScanAxisAndVerifyAsync(position, ct).ConfigureAwait(false);
                    if (moveResult != 0)
                        return moveResult;

                    if (_request.SettleDelayMs > 0)
                        await Task.Delay(_request.SettleDelayMs, ct).ConfigureAwait(false);

                    VisionFocusScanSample sample = await MeasureFocusValueAsync(i + 1, position, i == 0, ct).ConfigureAwait(false);
                    Result.Samples.Add(sample);
                    if (!sample.Success)
                        return Fail("VISION-FOCUS-CAL-FOCUS-VAL", "VisionFocusScanSequence",
                            "FOCUS_VAL 응답 실패. 대상=" + BuildTargetLabel() +
                            ", position=" + position.ToString("F4") +
                            ", raw=" + sample.Raw);
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

        private async Task<int> FocusBestStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsVisionBypassed())
                {
                    ApplyBestFromSamples();
                    CurrentStep = VisionFocusScanStep.SaveBest;
                    return 0;
                }

                VisionFocusBestResult best = await VisionCommandService.FocusBestAsync(
                    ResolveChannel(),
                    ResolveCameraName(),
                    ResolveTargetName(),
                    ResolvePickupNoForVision(),
                    _request.VisionTimeoutMs,
                    ct).ConfigureAwait(false);

                if (best == null || !best.Success)
                    return Fail("VISION-FOCUS-CAL-FOCUS-BEST", "VisionFocusScanSequence",
                        "FOCUS_BEST 응답 실패. 대상=" + BuildTargetLabel() + ", raw=" + (best != null ? best.Raw : "null"));

                Result.BestPosition = best.BestZ;
                Result.BestScore = best.BestScore;
                Result.SampleCount = Result.Samples.Count;
                CurrentStep = VisionFocusScanStep.SaveBest;
                return 0;
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
                if (_request.Kind == VisionFocusScanKind.BottomCollet)
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
                result = await _machine.PickerFrontUnit.MovePickerAxisCommandWithMotion(
                    axis,
                    position,
                    _request.MoveVelocity,
                    _request.MoveAcceleration,
                    _request.MoveDeceleration,
                    "VisionFocusCal;BottomCollet").ConfigureAwait(false);
                if (result != 0)
                    return FailPickerZCommand("Front", position, result);

                wait = await _machine.PickerFrontUnit.WaitPickerAxisMoveDoneInPosition(axis, position, _request.MotionTimeoutMs, ct).ConfigureAwait(false);
                if (!wait.Success)
                    return FailPickerZWait("Front", position, wait);

                if (!_machine.PickerFrontUnit.IsPickerAxisInPosition(axis, position, 0.01))
                    return FailPickerZFinal("Front", position);
            }
            else
            {
                result = await _machine.PickerRearUnit.MovePickerAxisCommandWithMotion(
                    axis,
                    position,
                    _request.MoveVelocity,
                    _request.MoveAcceleration,
                    _request.MoveDeceleration,
                    "VisionFocusCal;BottomCollet").ConfigureAwait(false);
                if (result != 0)
                    return FailPickerZCommand("Rear", position, result);

                wait = await _machine.PickerRearUnit.WaitPickerAxisMoveDoneInPosition(axis, position, _request.MotionTimeoutMs, ct).ConfigureAwait(false);
                if (!wait.Success)
                    return FailPickerZWait("Rear", position, wait);

                if (!_machine.PickerRearUnit.IsPickerAxisInPosition(axis, position, 0.01))
                    return FailPickerZFinal("Rear", position);
            }

            return 0;
        }

        private async Task<int> MoveSideVisionYAndVerifyAsync(double position, CancellationToken ct)
        {
            VisionAxis axis = ResolveSideVisionAxis();
            int result = await _machine.VisionUnit.MoveVisionAxisCommandWithMotion(
                axis,
                position,
                _request.MoveVelocity,
                _request.MoveAcceleration,
                _request.MoveDeceleration,
                "VisionFocusCal;Side").ConfigureAwait(false);
            if (result != 0)
                return Fail("VISION-FOCUS-CAL-SIDE-Y-MOVE", "VisionFocusScanSequence",
                    "Focus 스캔 SideVisionY 이동 명령 실패. axis=" + axis +
                    ", position=" + position +
                    ", velocity=" + _request.MoveVelocity +
                    ", acc=" + _request.MoveAcceleration +
                    ", dec=" + _request.MoveDeceleration +
                    ", result=" + result);

            AxisMoveWaitResult wait = await _machine.VisionUnit.WaitVisionAxisMoveDoneInPosition(axis, position, _request.MotionTimeoutMs).ConfigureAwait(false);
            if (!wait.Success)
                return Fail("VISION-FOCUS-CAL-SIDE-Y-WAIT", "VisionFocusScanSequence",
                    "Focus 스캔 SideVisionY 이동 완료 확인 실패. axis=" + axis +
                    ", position=" + position + ", reason=" + wait.Reason);

            if (!_machine.VisionUnit.IsVisionAxisInPosition(axis, position, 0.01))
                return Fail("VISION-FOCUS-CAL-SIDE-Y-FINAL", "VisionFocusScanSequence",
                    "Focus 스캔 SideVisionY 최종 위치 확인 실패. axis=" + axis +
                    ", position=" + position);

            return 0;
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
            if (IsVisionBypassed())
            {
                double distance = position - _request.DefaultPosition;
                double score = 1000.0 - Math.Abs(distance) * 1000.0 + _simRandom.NextDouble() * 10.0;
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
                ct).ConfigureAwait(false);

            return new VisionFocusScanSample
            {
                No = no,
                Position = position,
                Score = value != null ? value.Score : 0.0,
                Success = value != null && value.Success,
                Raw = value != null ? value.Raw : "null"
            };
        }

        private void BuildScanPositions()
        {
            _scanPositions.Clear();
            double start = _request.DefaultPosition - _request.MinusRange;
            double end = _request.DefaultPosition + _request.PlusRange;
            double step = Math.Abs(_request.Step);
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

        private void ApplyBestFromSamples()
        {
            VisionFocusScanSample best = null;
            foreach (VisionFocusScanSample sample in Result.Samples)
            {
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
            if (_request.Kind == VisionFocusScanKind.BottomCollet)
                return data.GetColletRecord(_request.PickerSide, _request.PickerNo);

            return data.GetSideRecord(_request.Kind);
        }

        private bool IsVisionBypassed()
        {
            return _machine != null &&
                   _machine.VisionUnit != null &&
                   _machine.VisionUnit.Config != null &&
                   _machine.VisionUnit.Config.bDryRun;
        }

        private AutoVisionChannel ResolveChannel()
        {
            if (_request.Kind == VisionFocusScanKind.BottomCollet)
                return AutoVisionChannel.BottomInspection;
            if (_request.Kind == VisionFocusScanKind.FrontSide0 || _request.Kind == VisionFocusScanKind.FrontSide90)
                return AutoVisionChannel.FrontSide;
            return AutoVisionChannel.RearSide;
        }

        private string ResolveCameraName()
        {
            if (_request.Kind == VisionFocusScanKind.BottomCollet)
                return "BOTTOM";
            if (_request.Kind == VisionFocusScanKind.FrontSide0 || _request.Kind == VisionFocusScanKind.FrontSide90)
                return "FRONT";
            return "BACK";
        }

        private string ResolveTargetName()
        {
            return _request.Kind == VisionFocusScanKind.BottomCollet ? "COLLET" : "SIDE";
        }

        private int ResolvePickupNoForVision()
        {
            return _request.Kind == VisionFocusScanKind.BottomCollet ? _request.PickerNo : 0;
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

        private double ResolveFrontBottomX(int pickerIndex, PickerAlignOffset offset)
        {
            double baseX = _machine.PickerFrontUnit.GetPickerTeachingPosition(PickerAxis.PickerX, "BottomPosition");
            double pitch = _machine.PickerFrontUnit.Setup != null ? Math.Abs(_machine.PickerFrontUnit.Setup.PickerPitchX) : 0.0;
            double offsetX = offset != null ? offset.AlignOffsetX : 0.0;
            return baseX + pitch * Math.Max(0, 3 - pickerIndex) + offsetX;
        }

        private double ResolveRearBottomX(int pickerIndex, PickerAlignOffset offset)
        {
            double baseX = _machine.PickerRearUnit.GetPickerTeachingPosition(PickerAxis.PickerX, "BottomPosition");
            double pitch = _machine.PickerRearUnit.Setup != null ? Math.Abs(_machine.PickerRearUnit.Setup.PickerPitchX) : 0.0;
            double offsetX = offset != null ? offset.AlignOffsetX : 0.0;
            return baseX + pitch * Math.Max(0, 3 - pickerIndex) + offsetX;
        }

        private int ResolveMotionTimeoutMs()
        {
            return _request != null && _request.MotionTimeoutMs > 0 ? _request.MotionTimeoutMs : 5000;
        }

        private bool IsSelectedFront()
        {
            return _request == null || _request.PickerSide == VisionFocusPickerSide.Front;
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
