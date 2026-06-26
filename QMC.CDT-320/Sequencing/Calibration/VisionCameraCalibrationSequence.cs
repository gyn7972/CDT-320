using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.VisionComm;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    public enum VisionCameraCalibrationTarget
    {
        Bottom,
        Input,
        Output
    }

    public enum VisionCameraCalibrationStep
    {
        Idle,
        CheckUnit,
        EnsureInputOutputVisionAvoid,
        EnsurePickersOutputAvoid,
        DeployReticleToBottomCamera,
        FindBottomReticle,
        Complete,
        Error
    }

    public sealed class VisionCameraCalibrationSequence
    {
        private const string ReticleFinderName = "ReticleFinder";
        private const int ReticleFindRetryCount = 3;
        private const int CalibrationMotionTimeoutMs = 10000;
        private const int ReticleMatchPollIntervalMs = 100;
        private const double CalibrationAxisTolerance = 0.01;
        private readonly CDT320_Machine _machine;
        private readonly Func<string> _userNameProvider;

        public VisionCameraCalibrationSequence(
            CDT320_Machine machine,
            Func<string> userNameProvider)
        {
            _machine = machine;
            _userNameProvider = userNameProvider;
        }

        public VisionCameraCalibrationStep CurrentStep { get; private set; }

        public VisionCameraCalibrationData CalibrationData
        {
            get
            {
                VisionUnit unit = _machine != null ? _machine.VisionUnit : null;
                if (unit == null || unit.Config == null)
                    return null;

                unit.Config.EnsureCalibrationObjects();
                return unit.Config.CameraCalibration;
            }
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                CurrentStep = VisionCameraCalibrationStep.CheckUnit;

                while (CurrentStep != VisionCameraCalibrationStep.Complete &&
                       CurrentStep != VisionCameraCalibrationStep.Error)
                {
                    ct.ThrowIfCancellationRequested();
                    VisionCameraCalibrationStep executingStep = CurrentStep;
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-STEP", "Vision Camera Calibration step 시작: " + executingStep);

                    int result = await ExecuteCurrentStepAsync(ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        CurrentStep = VisionCameraCalibrationStep.Error;
                        EventLogger.Write(EventKind.Alarm, "CAL", "VISION-CAMERA-CAL-STEP-FAIL", "Vision Camera Calibration step 실패: " + executingStep + ", result=" + result);
                        return result;
                    }

                    EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-STEP-DONE", "Vision Camera Calibration step 완료: " + executingStep + ", next=" + CurrentStep);
                }

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-RUN-DONE",
                    "Vision Camera Calibration Run Current 완료. Input/Output Reticle 측정과 계산/저장은 사용자가 별도 버튼으로 실행해야 합니다.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                EventLogger.Write(EventKind.Warning, "CAL", "VISION-CAMERA-CAL-CANCEL", "Vision Camera Calibration 작업이 취소되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-EX", "VisionCameraCalibrationSequence", "Vision Camera Calibration 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> ExecuteCurrentStepAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                switch (CurrentStep)
                {
                    case VisionCameraCalibrationStep.CheckUnit:
                        return CheckUnitStep();

                    case VisionCameraCalibrationStep.EnsureInputOutputVisionAvoid:
                        return await EnsureInputOutputVisionAvoidStepAsync(ct).ConfigureAwait(false);

                    case VisionCameraCalibrationStep.EnsurePickersOutputAvoid:
                        return await EnsurePickersOutputAvoidStepAsync(ct).ConfigureAwait(false);

                    case VisionCameraCalibrationStep.DeployReticleToBottomCamera:
                        return await DeployReticleToBottomCameraStepAsync(ct).ConfigureAwait(false);

                    case VisionCameraCalibrationStep.FindBottomReticle:
                        return await FindBottomReticleStepAsync(ct).ConfigureAwait(false);

                    default:
                        return Fail("VISION-CAMERA-CAL-UNSUPPORTED-STEP", "VisionCameraCalibrationSequence", "지원하지 않는 Vision Camera Calibration Step입니다. step=" + CurrentStep);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-STEP-EX", "VisionCameraCalibrationSequence", "Vision Camera Calibration Step 예외 발생. step=" + CurrentStep + ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckUnitStep()
        {
            try
            {
                int result = CheckUnit();
                if (result != 0)
                    return result;

                CurrentStep = VisionCameraCalibrationStep.EnsureInputOutputVisionAvoid;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-CHECK-STEP-EX", "VisionCameraCalibrationSequence", "Vision Camera Calibration 유닛 확인 Step 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> DeployReticleToBottomCameraStepAsync(CancellationToken ct)
        {
            try
            {
                int result = await DeployReticleToBottomCameraAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = VisionCameraCalibrationStep.FindBottomReticle;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-RETICLE-STEP-EX", "VisionUnit", "Reticle 측정 위치 이동 Step 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureInputOutputVisionAvoidStepAsync(CancellationToken ct)
        {
            try
            {
                int result = await EnsureInputOutputVisionAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = VisionCameraCalibrationStep.EnsurePickersOutputAvoid;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-VISION-AVOID-STEP-EX", "VisionCameraCalibrationSequence", "Input/Output VisionX Avoid Step 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsurePickersOutputAvoidStepAsync(CancellationToken ct)
        {
            try
            {
                int result = await EnsurePickersOutputAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = VisionCameraCalibrationStep.DeployReticleToBottomCamera;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-PICKER-AVOID-STEP-EX", "PickerUnit", "Picker Output Avoid Step 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> FindBottomReticleStepAsync(CancellationToken ct)
        {
            try
            {
                int result = await FindBottomReticleAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                CurrentStep = VisionCameraCalibrationStep.Complete;
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-BOTTOM-FIND-STEP-EX", "BottomInspection", "Bottom Reticle Mark 측정 Step 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> FindBottomReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int check = CheckUnit();
                if (check != 0)
                    return check;

                VisionReticleMeasurement measurement = await FindReticleWithRetryAsync(VisionCameraCalibrationTarget.Bottom, ct).ConfigureAwait(false);
                if (measurement == null || !measurement.Valid)
                    return Fail("VISION-CAMERA-CAL-BOTTOM-FIND", "BottomInspection", "Bottom 카메라 Reticle Mark 찾기 실패.");

                CalibrationData.BottomReticle = measurement;
                CalibrationData.Valid = false;
                PersistMeasuredCalibrationData("Bottom 카메라 Reticle Mark 측정값");
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-BOTTOM",
                    "Bottom 카메라 Reticle Mark 측정 완료. " +
                    "x=" + measurement.PixelX.ToString("F3") +
                    ", y=" + measurement.PixelY.ToString("F3") +
                    ", t=" + measurement.AngleDeg.ToString("F3") +
                    ", score=" + measurement.Score.ToString("F3"));
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-BOTTOM-EX", "BottomInspection", "Bottom 카메라 Reticle Mark 찾기 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> FindInputReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int check = CheckUnit();
                if (check != 0)
                    return check;

                VisionReticleMeasurement measurement = await FindReticleWithRetryAsync(VisionCameraCalibrationTarget.Input, ct).ConfigureAwait(false);
                if (measurement == null || !measurement.Valid)
                    return Fail("VISION-CAMERA-CAL-INPUT-FIND", "WaferVision", "Input 카메라 Reticle Mark 찾기 실패.");

                CalibrationData.InputReticle = measurement;
                CalibrationData.Valid = false;
                PersistMeasuredCalibrationData("Input 카메라 Reticle Mark 측정값");
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-INPUT",
                    "Input 카메라 Reticle Mark 측정 완료. x=" + measurement.PixelX.ToString("F3") +
                    ", y=" + measurement.PixelY.ToString("F3") +
                    ", t=" + measurement.AngleDeg.ToString("F3") +
                    ", score=" + measurement.Score.ToString("F3"));
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-INPUT-EX", "WaferVision", "Input 카메라 Reticle Mark 찾기 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> PrepareAndFindInputReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = CheckUnit();
                if (result != 0)
                    return result;

                result = await EnsureOutputVisionAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsurePickersOutputAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureReticleBottomReadyAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputCameraToReticleAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await FindInputReticleAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-INPUT-PREP-FIND-EX", "VisionCameraCalibrationSequence", "Input 카메라 Reticle 측정 준비/촬영 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> FindOutputReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int check = CheckUnit();
                if (check != 0)
                    return check;

                VisionReticleMeasurement measurement = await FindReticleWithRetryAsync(VisionCameraCalibrationTarget.Output, ct).ConfigureAwait(false);
                if (measurement == null || !measurement.Valid)
                    return Fail("VISION-CAMERA-CAL-OUTPUT-FIND", "BinVision", "Output 카메라 Reticle Mark 찾기 실패.");

                CalibrationData.OutputReticle = measurement;
                CalibrationData.Valid = false;
                PersistMeasuredCalibrationData("Output 카메라 Reticle Mark 측정값");
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-OUTPUT",
                    "Output 카메라 Reticle Mark 측정 완료. x=" + measurement.PixelX.ToString("F3") +
                    ", y=" + measurement.PixelY.ToString("F3") +
                    ", t=" + measurement.AngleDeg.ToString("F3") +
                    ", score=" + measurement.Score.ToString("F3"));
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-OUTPUT-EX", "BinVision", "Output 카메라 Reticle Mark 찾기 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> PrepareAndFindOutputReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = CheckUnit();
                if (result != 0)
                    return result;

                result = await EnsureInputVisionAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsurePickersInputAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await EnsureReticleBottomReadyAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputCameraToReticleAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                return await FindOutputReticleAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-OUTPUT-PREP-FIND-EX", "VisionCameraCalibrationSequence", "Output 카메라 Reticle 측정 준비/촬영 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public int CalculateCalibration()
        {
            try
            {
                int check = CheckUnit();
                if (check != 0)
                    return check;

                if (!CalibrationData.Calculate(GetUserName()))
                    return Fail("VISION-CAMERA-CAL-CALC", "VisionUnit", "Vision Camera Calibration 계산 실패. Bottom/Input/Output Reticle Mark 측정값이 모두 필요합니다.");

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-CALC",
                    "Vision Camera Calibration 계산 완료. Bottom-InputOffset=(" +
                    CalibrationData.InputToBottomOffsetX.ToString("F6") + "," +
                    CalibrationData.InputToBottomOffsetY.ToString("F6") + "), Bottom-OutputOffset=(" +
                    CalibrationData.OutputToBottomOffsetX.ToString("F6") + "," +
                    CalibrationData.OutputToBottomOffsetY.ToString("F6") + ")");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-CALC-EX", "VisionUnit", "Vision Camera Calibration 계산 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public int SaveCalibration()
        {
            try
            {
                int check = CheckUnit();
                if (check != 0)
                    return check;

                if (!CalibrationData.Valid)
                    return Fail("VISION-CAMERA-CAL-SAVE-NOT-VALID", "VisionUnit", "Vision Camera Calibration 저장 불가: 계산 완료된 유효 데이터가 없습니다.");

                if (!SaveMachineSettings())
                    return Fail("VISION-CAMERA-CAL-SAVE-FAIL", "VisionUnit", "Vision Camera Calibration 저장 실패: VisionUnit Config 파일 저장에 실패했습니다.");

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-SAVE", "Vision Camera Calibration 데이터를 VisionUnit Config에 저장했습니다.");
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-SAVE-EX", "VisionUnit", "Vision Camera Calibration 저장 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private int CheckUnit()
        {
            try
            {
                if (_machine == null)
                    return Fail("VISION-CAMERA-CAL-NO-MACHINE", "VisionCameraCalibrationSequence", "장비 객체가 없어 Vision Camera Calibration을 실행할 수 없습니다.");
                if (_machine.VisionUnit == null)
                    return Fail("VISION-CAMERA-CAL-NO-VISION", "VisionCameraCalibrationSequence", "VisionUnit이 없어 Vision Camera Calibration을 실행할 수 없습니다.");
                if (_machine.VisionUnit.Config == null)
                    return Fail("VISION-CAMERA-CAL-NO-CONFIG", "VisionCameraCalibrationSequence", "VisionUnit Config가 없어 Vision Camera Calibration을 실행할 수 없습니다.");

                _machine.VisionUnit.Config.EnsureCalibrationObjects();
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-CHECK-EX", "VisionCameraCalibrationSequence", "Vision Camera Calibration 유닛 확인 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private Task<int> PrepareReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-PREPARE",
                    "Reticle 자동 이동은 수행하지 않습니다. 작업자가 Reticle Mark를 각 카메라 시야 안에 준비한 상태에서 측정합니다.");
                return Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("VISION-CAMERA-CAL-PREPARE-EX", "VisionUnit", "Reticle 준비 확인 예외 발생: " + ex.Message));
            }
            finally
            {
            }
        }

        private async Task<int> MoveInputCameraToReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                InputStageUnit stage = _machine != null ? _machine.InputStageUnit : null;
                if (stage == null || stage.CameraX == null || stage.Recipe == null || stage.Recipe.VisionX == null)
                    return Fail("VISION-CAMERA-CAL-INPUT-RETICLE-MISSING", "InputStageUnit", "InputVisionX Reticle 위치 이동을 위한 축/Recipe 정보가 없습니다.");

                double target = stage.Recipe.VisionX.ReticlePosition;
                if (IsAxisInPosition(stage.CameraX, target))
                    return 0;

                int result = await stage.MoveInputStageAxis(WaferStageAxis.VisionX, target, true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-CAMERA-CAL-INPUT-RETICLE-MOVE", "InputStageUnit", "InputVisionX Reticle 위치 이동 명령 실패. result=" + result + ", target=" + target.ToString("F3"));

                int wait = await stage.WaitInputStageAxisInPosition(WaferStageAxis.VisionX, target, CalibrationMotionTimeoutMs, ct).ConfigureAwait(false);
                if (wait != 0)
                    return Fail("VISION-CAMERA-CAL-INPUT-RETICLE-WAIT", "InputStageUnit", "InputVisionX Reticle 위치 이동 완료 확인 실패. result=" + wait + ", target=" + target.ToString("F3"));

                if (!IsAxisInPosition(stage.CameraX, target))
                    return Fail("VISION-CAMERA-CAL-INPUT-RETICLE-CHECK", "InputStageUnit", "InputVisionX Reticle 최종 위치 확인 실패. actual=" + stage.CameraX.ActualPosition.ToString("F3") + ", target=" + target.ToString("F3"));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-INPUT-MOVE-EX", "InputStageUnit", "Input 카메라 Reticle 이동 준비 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveOutputCameraToReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                OutputStageUnit stage = _machine != null ? _machine.OutputStageUnit : null;
                if (stage == null || stage.OutputCameraX == null || stage.Recipe == null || stage.Recipe.VisionX == null)
                    return Fail("VISION-CAMERA-CAL-OUTPUT-RETICLE-MISSING", "OutputStageUnit", "OutputVisionX Reticle 위치 이동을 위한 축/Recipe 정보가 없습니다.");

                double target = stage.Recipe.VisionX.ReticlePosition;
                if (IsAxisInPosition(stage.OutputCameraX, target))
                    return 0;

                int result = await stage.MoveStageAxis(BinStageAxis.VisionX, target, true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-CAMERA-CAL-OUTPUT-RETICLE-MOVE", "OutputStageUnit", "OutputVisionX Reticle 위치 이동 명령 실패. result=" + result + ", target=" + target.ToString("F3"));

                AxisMoveWaitResult wait = await stage.WaitStageAxisMoveDoneInPosition(BinStageAxis.VisionX, target, CalibrationMotionTimeoutMs, ct).ConfigureAwait(false);
                if (wait == null || !wait.Success)
                    return Fail("VISION-CAMERA-CAL-OUTPUT-RETICLE-WAIT", "OutputStageUnit", "OutputVisionX Reticle 위치 이동 완료 확인 실패. target=" + target.ToString("F3"));

                if (!IsAxisInPosition(stage.OutputCameraX, target))
                    return Fail("VISION-CAMERA-CAL-OUTPUT-RETICLE-CHECK", "OutputStageUnit", "OutputVisionX Reticle 최종 위치 확인 실패. actual=" + stage.OutputCameraX.ActualPosition.ToString("F3") + ", target=" + target.ToString("F3"));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-OUTPUT-MOVE-EX", "OutputStageUnit", "Output 카메라 Reticle 이동 준비 예외 발생: " + ex.Message);
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
                int inputResult = await EnsureInputVisionAvoidAsync(ct).ConfigureAwait(false);
                if (inputResult != 0)
                    return inputResult;

                int outputResult = await EnsureOutputVisionAvoidAsync(ct).ConfigureAwait(false);
                if (outputResult != 0)
                    return outputResult;

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-VISION-AVOID-EX", "VisionCameraCalibrationSequence", "Input/Output VisionX Avoid 이동 예외 발생: " + ex.Message);
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
                InputStageUnit stage = _machine != null ? _machine.InputStageUnit : null;
                if (stage == null || stage.CameraX == null || stage.Recipe == null || stage.Recipe.VisionX == null)
                    return Fail("VISION-CAMERA-CAL-INPUT-VISION-MISSING", "InputStageUnit", "InputVisionX Avoid 이동을 위한 축/Recipe 정보가 없습니다.");

                if (stage.IsVisionXInAvoidPosition())
                    return 0;

                double target = stage.Recipe.VisionX.AvoidPosition;
                int result = await stage.MoveInputStageAxis(WaferStageAxis.VisionX, target, true).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-CAMERA-CAL-INPUT-VISION-AVOID", "InputStageUnit", "InputVisionX Avoid 이동 명령 실패. result=" + result + ", target=" + target.ToString("F3"));

                int wait = await stage.WaitInputStageAxisInPosition(WaferStageAxis.VisionX, target, CalibrationMotionTimeoutMs, ct).ConfigureAwait(false);
                if (wait != 0)
                    return Fail("VISION-CAMERA-CAL-INPUT-VISION-WAIT", "InputStageUnit", "InputVisionX Avoid 이동 완료 확인 실패. result=" + wait + ", target=" + target.ToString("F3"));

                if (!stage.IsVisionXInAvoidPosition())
                    return Fail("VISION-CAMERA-CAL-INPUT-VISION-CHECK", "InputStageUnit", "InputVisionX Avoid 최종 위치 확인 실패. actual=" + stage.CameraX.ActualPosition.ToString("F3") + ", target=" + target.ToString("F3"));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-INPUT-VISION-EX", "InputStageUnit", "InputVisionX Avoid 이동 예외 발생: " + ex.Message);
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
                OutputStageUnit stage = _machine != null ? _machine.OutputStageUnit : null;
                if (stage == null || stage.OutputCameraX == null)
                    return Fail("VISION-CAMERA-CAL-OUTPUT-VISION-MISSING", "OutputStageUnit", "OutputVisionX Avoid 이동을 위한 축 정보가 없습니다.");

                if (stage.IsVisionXInAvoidPosition())
                    return 0;

                int result = await stage.MoveVisionXToAvoidAndVerifyAsync(CalibrationMotionTimeoutMs, true, ct).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-CAMERA-CAL-OUTPUT-VISION-AVOID", "OutputStageUnit", "OutputVisionX Avoid 이동 실패. result=" + result);

                if (!stage.IsVisionXInAvoidPosition())
                    return Fail("VISION-CAMERA-CAL-OUTPUT-VISION-CHECK", "OutputStageUnit", "OutputVisionX Avoid 최종 위치 확인 실패. actual=" + stage.OutputCameraX.ActualPosition.ToString("F3"));

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-OUTPUT-VISION-EX", "OutputStageUnit", "OutputVisionX Avoid 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsurePickersOutputAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_machine == null || _machine.PickerFrontUnit == null || _machine.PickerRearUnit == null)
                    return Fail("VISION-CAMERA-CAL-PICKER-MISSING", "PickerUnit", "Picker Output-side Avoid 이동을 위한 Picker Unit이 없습니다.");

                Task<int> frontTask = _machine.PickerFrontUnit.MoveToPickerUnloadPosition(true);
                Task<int> rearTask = _machine.PickerRearUnit.MoveToPickerUnloadPosition(true);
                int[] results = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                    return Fail("VISION-CAMERA-CAL-PICKER-OUTPUT-AVOID", "PickerUnit", "Picker Output-side Avoid 이동 실패. frontResult=" + results[0] + ", rearResult=" + results[1]);

                ct.ThrowIfCancellationRequested();
                if (!_machine.PickerFrontUnit.IsPickerInUnloadPosition() || !_machine.PickerRearUnit.IsPickerInUnloadPosition())
                    return Fail("VISION-CAMERA-CAL-PICKER-OUTPUT-CHECK", "PickerUnit", "Picker Output-side Avoid 최종 위치 확인 실패. front=" + _machine.PickerFrontUnit.IsPickerInUnloadPosition() + ", rear=" + _machine.PickerRearUnit.IsPickerInUnloadPosition());

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-PICKER-OUTPUT-EX", "PickerUnit", "Picker Output-side Avoid 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsurePickersInputAvoidAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_machine == null || _machine.PickerFrontUnit == null || _machine.PickerRearUnit == null)
                    return Fail("VISION-CAMERA-CAL-PICKER-MISSING", "PickerUnit", "Picker Input-side Avoid 이동을 위한 Picker Unit이 없습니다.");

                Task<int> frontTask = _machine.PickerFrontUnit.MoveToPickerLoadPosition(true);
                Task<int> rearTask = _machine.PickerRearUnit.MoveToPickerLoadPosition(true);
                int[] results = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                    return Fail("VISION-CAMERA-CAL-PICKER-INPUT-AVOID", "PickerUnit", "Picker Input-side Avoid 이동 실패. frontResult=" + results[0] + ", rearResult=" + results[1]);

                ct.ThrowIfCancellationRequested();
                if (!_machine.PickerFrontUnit.IsPickerInLoadPosition() || !_machine.PickerRearUnit.IsPickerInLoadPosition())
                    return Fail("VISION-CAMERA-CAL-PICKER-INPUT-CHECK", "PickerUnit", "Picker Input-side Avoid 최종 위치 확인 실패. front=" + _machine.PickerFrontUnit.IsPickerInLoadPosition() + ", rear=" + _machine.PickerRearUnit.IsPickerInLoadPosition());

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-PICKER-INPUT-EX", "PickerUnit", "Picker Input-side Avoid 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureReticleBottomReadyAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                VisionUnit vision = _machine != null ? _machine.VisionUnit : null;
                if (vision == null)
                    return Fail("VISION-CAMERA-CAL-RETICLE-NO-VISION", "VisionUnit", "Reticle 준비 상태 확인을 위한 VisionUnit이 없습니다.");

                if (IsReticleBottomReady(vision))
                    return 0;

                return await DeployReticleToBottomCameraAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-RETICLE-READY-EX", "VisionUnit", "Reticle Bottom 준비 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> DeployReticleToBottomCameraAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                VisionUnit vision = _machine != null ? _machine.VisionUnit : null;
                if (vision == null)
                    return Fail("VISION-CAMERA-CAL-RETICLE-NO-VISION", "VisionUnit", "Reticle 동작을 위한 VisionUnit이 없습니다.");

                int result = await vision.SetReticleLiftUpAsync(true, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await vision.SetReticleFrontSideForwardAsync(true, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await vision.SetReticleRearSideForwardAsync(true, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (!IsReticleBottomReady(vision))
                    return Fail("VISION-CAMERA-CAL-RETICLE-CHECK", "VisionUnit", "Bottom 카메라 촬영 전 Reticle 위치 확인 실패. up=" + vision.IsVisionReticleUp() + ", frontFw=" + vision.IsVisionReticleFrontSideForward() + ", rearFw=" + vision.IsVisionReticleRearSideForward());

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-RETICLE-READY", "Reticle이 Bottom 카메라 측정 위치에 도착했습니다.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-RETICLE-DEPLOY-EX", "VisionUnit", "Reticle 측정 위치 이동 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> RetractReticleFromBottomCameraAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                VisionUnit vision = _machine != null ? _machine.VisionUnit : null;
                if (vision == null)
                    return Fail("VISION-CAMERA-CAL-RETICLE-RETRACT-NO-VISION", "VisionUnit", "Reticle 복귀를 위한 VisionUnit이 없습니다.");

                int result = await vision.SetReticleRearSideForwardAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await vision.SetReticleFrontSideForwardAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await vision.SetReticleLiftUpAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (!IsReticleRetracted(vision))
                    return Fail("VISION-CAMERA-CAL-RETICLE-RETRACT-CHECK", "VisionUnit", "Reticle 복귀 후 위치 확인 실패. down=" + vision.IsVisionReticleDown() + ", frontBw=" + vision.IsVisionReticleFrontSideBackward() + ", rearBw=" + vision.IsVisionReticleRearSideBackward());

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-RETICLE-RETRACT", "Reticle이 역순으로 복귀했습니다.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-RETICLE-RETRACT-EX", "VisionUnit", "Reticle 복귀 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool IsReticleRetracted(VisionUnit vision)
        {
            try
            {
                if (vision == null)
                    return false;

                if (IsSimulationMode())
                    return true;

                return vision.IsVisionReticleDown() &&
                       vision.IsVisionReticleFrontSideBackward() &&
                       vision.IsVisionReticleRearSideBackward();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsReticleBottomReady(VisionUnit vision)
        {
            try
            {
                if (vision == null)
                    return false;

                if (IsSimulationMode())
                    return true;

                return vision.IsVisionReticleUp() &&
                       vision.IsVisionReticleFrontSideForward() &&
                       vision.IsVisionReticleRearSideForward();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsAxisInPosition(BaseAxis axis, double target)
        {
            try
            {
                if (axis == null)
                    return false;

                return Math.Abs(axis.ActualPosition - target) <= CalibrationAxisTolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task<VisionReticleMeasurement> FindReticleWithRetryAsync(VisionCameraCalibrationTarget target, CancellationToken ct)
        {
            try
            {
                VisionReticleMeasurement lastMeasurement = null;
                for (int attempt = 1; attempt <= ReticleFindRetryCount; attempt++)
                {
                    ct.ThrowIfCancellationRequested();
                    lastMeasurement = await FindReticleAsync(target, ct).ConfigureAwait(false);
                    if (lastMeasurement != null && lastMeasurement.Valid && IsValidReticleMeasurement(lastMeasurement))
                        return lastMeasurement;

                    EventLogger.Write(EventKind.Warning, "CAL", "VISION-CAMERA-CAL-RETICLE-RETRY",
                        ResolveCameraName(target) + " ReticleFinder 결과가 NG입니다. retry=" + attempt + "/" + ReticleFindRetryCount);
                }

                return lastMeasurement;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Fail("VISION-CAMERA-CAL-RETICLE-RETRY-EX", ResolveCameraName(target), "ReticleFinder 리트라이 중 예외 발생: " + ex.Message);
                return null;
            }
            finally
            {
            }
        }

        private async Task<VisionReticleMeasurement> FindReticleAsync(VisionCameraCalibrationTarget target, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            VisionCameraCalibrationData data = CalibrationData;
            MatchResultDto match = await RequestReticleMatchAsync(target, ct).ConfigureAwait(false);
            if (match == null || !match.Success)
                return null;

            VisionReticleMeasurement measurement = new VisionReticleMeasurement();
            measurement.Valid = true;
            measurement.CameraName = ResolveCameraName(target);
            measurement.PixelX = match.X;
            measurement.PixelY = match.Y;
            VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(data, ResolveAutoVisionChannel(target));
            if (match.HasImageSize)
                camera.ApplyImageSize(match.ImageWidthPixel, match.ImageHeightPixel);
            measurement.MmX = camera.PixelToMmOffsetX(match.X);
            measurement.MmY = camera.PixelToMmOffsetY(match.Y);
            measurement.Score = match.Score;
            measurement.AngleDeg = match.AngleDeg;
            measurement.MeasuredAt = DateTime.Now;
            measurement.Raw = match.RawError ?? string.Empty;
            FillAxisPositions(target, measurement);
            return measurement;
        }

        private bool IsValidReticleMeasurement(VisionReticleMeasurement measurement)
        {
            try
            {
                if (measurement == null || !measurement.Valid)
                    return false;

                return IsFinite(measurement.PixelX) &&
                       IsFinite(measurement.PixelY) &&
                       IsFinite(measurement.AngleDeg) &&
                       IsFinite(measurement.Score);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private async Task<MatchResultDto> RequestReticleMatchAsync(VisionCameraCalibrationTarget target, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            string cameraName = ResolveCameraName(target);
            AutoVisionChannel channel = ResolveAutoVisionChannel(target);
            int timeoutMs = ResolveCaptureTimeoutMs();

            EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-MATCHASYNC-REQ",
                cameraName + " Vision에 ReticleFinder MATCHASYNC 시작을 요청합니다.");

            bool started = await AutoVisionRequestService.StartMatchAsync(
                channel,
                ReticleFinderName,
                0,
                timeoutMs,
                ct).ConfigureAwait(false);

            if (started)
            {
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-MATCHASYNC-STARTED",
                    cameraName + " Vision ReticleFinder MATCHASYNC STARTED 응답 또는 bypass 허가를 받았습니다.");

                return await WaitReticleMatchResultAsync(cameraName, channel, timeoutMs, ct).ConfigureAwait(false);
            }

            if (IsSimulationMode())
            {
                VisionCameraCalibrationData data = CalibrationData;
                VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(data, channel);
                return new MatchResultDto
                {
                    Success = true,
                    X = camera.ImageCenterPixelX,
                    Y = camera.ImageCenterPixelY,
                    AngleDeg = 0,
                    Score = 1.0,
                    HasImageSize = true,
                    ImageWidthPixel = camera.ImageWidthPixel,
                    ImageHeightPixel = camera.ImageHeightPixel,
                    RawError = "SIM:ReticleFinder"
                };
            }

            if (VisionCommandService.IsConnected(channel))
            {
                return new MatchResultDto
                {
                    Success = false,
                    RawError = cameraName + " ReticleFinder MATCHASYNC STARTED 응답을 받지 못했습니다."
                };
            }

            Fail("VISION-CAMERA-CAL-VISION-NOT-CONNECTED", cameraName, cameraName + " Vision이 연결되지 않아 ReticleFinder를 실행할 수 없습니다.");
            return null;
        }

        private async Task<MatchResultDto> WaitReticleMatchResultAsync(
            string cameraName,
            AutoVisionChannel channel,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                DateTime timeoutAt = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                while (DateTime.UtcNow < timeoutAt)
                {
                    ct.ThrowIfCancellationRequested();

                    int remainMs = (int)Math.Max(1, (timeoutAt - DateTime.UtcNow).TotalMilliseconds);
                    int pollTimeoutMs = Math.Min(1000, remainMs);
                    AsyncMatchPoll poll = await AutoVisionRequestService.PollMatchResultAsync(
                        channel,
                        ReticleFinderName,
                        0,
                        pollTimeoutMs,
                        ct).ConfigureAwait(false);

                    if (poll == null)
                    {
                        return new MatchResultDto
                        {
                            Success = false,
                            RawError = cameraName + " ReticleFinder MATCHRESULT 응답이 없습니다."
                        };
                    }

                    if (poll.Error)
                    {
                        return new MatchResultDto
                        {
                            Success = false,
                            RawError = cameraName + " ReticleFinder MATCHRESULT 실패: " + (poll.Raw ?? string.Empty)
                        };
                    }

                    if (poll.Done)
                    {
                        if (poll.Result != null && poll.Result.Success)
                        {
                            EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-MATCHRESULT-DONE",
                                cameraName + " Vision ReticleFinder MATCHRESULT 완료. x=" + poll.Result.X.ToString("0.###") +
                                ", y=" + poll.Result.Y.ToString("0.###") +
                                ", r=" + poll.Result.AngleDeg.ToString("0.###") +
                                ", score=" + poll.Result.Score.ToString("0.###"));
                            return poll.Result;
                        }

                        return new MatchResultDto
                        {
                            Success = false,
                            RawError = cameraName + " ReticleFinder MATCHRESULT 완료 응답 파싱 실패: " + (poll.Raw ?? string.Empty)
                        };
                    }

                    await Task.Delay(ReticleMatchPollIntervalMs, ct).ConfigureAwait(false);
                }

                return new MatchResultDto
                {
                    Success = false,
                    RawError = cameraName + " ReticleFinder MATCHRESULT 대기 시간이 초과되었습니다. timeoutMs=" + timeoutMs
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new MatchResultDto
                {
                    Success = false,
                    RawError = cameraName + " ReticleFinder MATCHRESULT 처리 중 예외 발생: " + ex.Message
                };
            }
            finally
            {
            }
        }

        private AutoVisionChannel ResolveAutoVisionChannel(VisionCameraCalibrationTarget target)
        {
            switch (target)
            {
                case VisionCameraCalibrationTarget.Bottom:
                    return AutoVisionChannel.Bottom;
                case VisionCameraCalibrationTarget.Input:
                    return AutoVisionChannel.Wafer;
                case VisionCameraCalibrationTarget.Output:
                    return AutoVisionChannel.Bin;
                default:
                    return AutoVisionChannel.Bottom;
            }
        }

        private string ResolveCameraName(VisionCameraCalibrationTarget target)
        {
            switch (target)
            {
                case VisionCameraCalibrationTarget.Bottom:
                    return "BottomInspection";
                case VisionCameraCalibrationTarget.Input:
                    return "WaferVision";
                case VisionCameraCalibrationTarget.Output:
                    return "BinVision";
                default:
                    return "UnknownVision";
            }
        }

        private int ResolveCaptureTimeoutMs()
        {
            VisionUnit unit = _machine != null ? _machine.VisionUnit : null;
            if (unit != null && unit.Recipe != null && unit.Recipe.CaptureTimeoutMs > 0)
                return unit.Recipe.CaptureTimeoutMs;

            return 5000;
        }

        private bool IsSimulationMode()
        {
            VisionUnit unit = _machine != null ? _machine.VisionUnit : null;
            if (unit != null &&
                ((unit.Setup != null && unit.Setup.IsSimulationMode) ||
                 (unit.Config != null && unit.Config.IsSimulationMode)))
                return true;

            return AppSettingsStore.Current != null &&
                   (AppSettingsStore.Current.SimulationMode || AppSettingsStore.Current.DryRunMode);
        }

        private void FillAxisPositions(VisionCameraCalibrationTarget target, VisionReticleMeasurement measurement)
        {
            try
            {
                if (target == VisionCameraCalibrationTarget.Input && _machine.InputStageUnit != null)
                {
                    FillAxis(measurement, _machine.InputStageUnit.CameraX, _machine.InputStageUnit.StageY);
                    return;
                }

                if (target == VisionCameraCalibrationTarget.Output && _machine.OutputStageUnit != null)
                {
                    BaseAxis y = _machine.OutputStageUnit.GoodStage != null ? _machine.OutputStageUnit.GoodStage.StageY : null;
                    FillAxis(measurement, _machine.OutputStageUnit.OutputCameraX, y);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void FillAxis(VisionReticleMeasurement measurement, BaseAxis visionX, BaseAxis stageY)
        {
            if (measurement == null)
                return;

            if (visionX != null)
            {
                measurement.VisionXPosition = visionX.ActualPosition;
                measurement.HasVisionXPosition = true;
            }

            if (stageY != null)
            {
                measurement.StageYPosition = stageY.ActualPosition;
                measurement.HasStageYPosition = true;
            }
        }

        private void PersistMeasuredCalibrationData(string label)
        {
            try
            {
                if (SaveMachineSettings())
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-MEASURE-SAVE", label + "을 VisionUnit Config에 저장했습니다.");
                else
                    EventLogger.Write(EventKind.Alarm, "CAL", "VISION-CAMERA-CAL-MEASURE-SAVE-FAIL", label + " 저장 실패: VisionUnit Config 파일 저장에 실패했습니다.");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "VISION-CAMERA-CAL-MEASURE-SAVE-EX", label + " 저장 중 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool SaveMachineSettings()
        {
            try
            {
                if (_machine == null)
                    return false;

                return _machine.SaveSettings();
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private string GetUserName()
        {
            try
            {
                if (_userNameProvider != null)
                    return _userNameProvider() ?? string.Empty;
            }
            catch
            {
            }

            return string.Empty;
        }

        private int Fail(string code, string source, string message)
        {
            EventLogger.Write(EventKind.Alarm, "CAL", code, message);
            AlarmManager.Raise(AlarmSeverity.Error, code, source, message);
            return -1;
        }
    }
}
