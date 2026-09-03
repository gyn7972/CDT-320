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
        private const string ReticleFinderName = VisionToolIds.BottomInspection.ReticleFinder;
        private const int ReticleFindRetryCount = 3;
        private const int ReticleMotionSettleDelayMs = 500;
        private const double CalibrationAxisTolerance = 0.01;
        private const double SimReticleMaxPixelOffset = 25.0;
        private const double SimReticleMaxAngleDeg = 0.08;
        private static readonly object SimReticleRandomLock = new object();
        private static readonly Random SimReticleRandom = new Random();
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
                return unit.Config.CalibrationData.Camera;
            }
        }

        private CalibrationMotionSettings ResolveMotionSettings()
        {
            VisionCameraCalibrationData data = CalibrationData;
            if (data != null)
            {
                data.EnsureObjects();
                return data.Motion.Clone();
            }

            var motion = new CalibrationMotionSettings();
            motion.EnsureDefaults();
            return motion;
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            return await RunAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> RunAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(runMode == SequenceRunMode.Auto, "VisionCameraCalibrationSequence.RunAsync:" + runMode))
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
                return Fail("VISION-CAMERA-CAL-PICKER-AVOID-STEP-EX", "PickerUnit", "Picker Output-side Avoid Step 예외 발생: " + ex.Message);
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
                return Fail("VISION-CAMERA-CAL-BOTTOM-FIND-STEP-EX", VisionModuleNames.BottomInspection, "Bottom Reticle Mark 측정 Step 예외 발생: " + ex.Message);
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
                    return Fail("VISION-CAMERA-CAL-BOTTOM-FIND", VisionModuleNames.BottomInspection, "Bottom 카메라 Reticle Mark 찾기 실패.");

                CalibrationData.BottomReticle = measurement;
                CalibrationData.Valid = false;
                PersistMeasuredCalibrationData(VisionCameraCalibrationTarget.Bottom, "Bottom 카메라 Reticle Mark 측정값");
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
                return Fail("VISION-CAMERA-CAL-BOTTOM-EX", VisionModuleNames.BottomInspection, "Bottom 카메라 Reticle Mark 찾기 예외 발생: " + ex.Message);
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
                    return Fail("VISION-CAMERA-CAL-INPUT-FIND", VisionModuleNames.Wafer, "Input 카메라 Reticle Mark 찾기 실패.");

                CalibrationData.InputReticle = measurement;
                CalibrationData.Valid = false;
                PersistMeasuredCalibrationData(VisionCameraCalibrationTarget.Input, "Input 카메라 Reticle Mark 측정값");
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
                return Fail("VISION-CAMERA-CAL-INPUT-EX", VisionModuleNames.Wafer, "Input 카메라 Reticle Mark 찾기 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> PrepareAndFindInputReticleAsync(CancellationToken ct)
        {
            return await PrepareAndFindInputReticleAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> PrepareAndFindInputReticleAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(runMode == SequenceRunMode.Auto, "VisionCameraCalibrationSequence.PrepareAndFindInputReticleAsync:" + runMode))
            {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = CheckUnit();
                if (result != 0)
                    return result;

                result = await EnsureReticleSafeBeforePickerMoveAsync(ct).ConfigureAwait(false);
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
                    return Fail("VISION-CAMERA-CAL-OUTPUT-FIND", VisionModuleNames.Bin, "Output 카메라 Reticle Mark 찾기 실패.");

                CalibrationData.OutputReticle = measurement;
                CalibrationData.Valid = false;
                PersistMeasuredCalibrationData(VisionCameraCalibrationTarget.Output, "Output 카메라 Reticle Mark 측정값");
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
                return Fail("VISION-CAMERA-CAL-OUTPUT-EX", VisionModuleNames.Bin, "Output 카메라 Reticle Mark 찾기 예외 발생: " + ex.Message);
            }
            finally
            {
            }
        }

        public async Task<int> PrepareAndFindOutputReticleAsync(CancellationToken ct)
        {
            return await PrepareAndFindOutputReticleAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> PrepareAndFindOutputReticleAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(runMode == SequenceRunMode.Auto, "VisionCameraCalibrationSequence.PrepareAndFindOutputReticleAsync:" + runMode))
            {
            try
            {
                ct.ThrowIfCancellationRequested();

                int result = CheckUnit();
                if (result != 0)
                    return result;

                result = await EnsureReticleSafeBeforePickerMoveAsync(ct).ConfigureAwait(false);
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

                TouchCalibrationData();
                LogCalibrationFormulaData(CalibrationData);
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

                // 시뮬레이션/bypass 측정이 섞인 계산 결과는 실 캘리브레이션/픽커 오프셋으로 적용하지 않는다.
                string simulatedCameras;
                if (HasSimulatedMeasurementInSession(out simulatedCameras))
                    return Fail("VISION-CAMERA-CAL-SAVE-SIM", "VisionUnit",
                        "Vision Camera Calibration 저장 불가: 이번 측정에 시뮬레이션/bypass 결과(" + simulatedCameras +
                        ")가 포함되어 있습니다. 실 Vision 연결 상태에서 다시 측정하세요.");

                string offsetSummary;
                PickerVisionOffsetCalibrationService.TryApplyAvailableOffsets(_machine, GetUserName(), out offsetSummary);
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-PICKER-OFFSET", offsetSummary);

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
                if (CanSkipAxisMoveCommand(stage.CameraX, target))
                    return 0;

                CalibrationMotionSettings motion = ResolveMotionSettings();
                int result = await stage.MoveInputStageAxisCommandWithMotion(
                    WaferStageAxis.VisionX,
                    target,
                    motion.MoveVelocity,
                    motion.MoveAcceleration,
                    motion.MoveDeceleration).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-CAMERA-CAL-INPUT-RETICLE-MOVE", "InputStageUnit", "InputVisionX Reticle 위치 이동 명령 실패. result=" + result + ", target=" + target.ToString("F3"));

                int wait = await stage.WaitInputStageAxisInPosition(WaferStageAxis.VisionX, target, motion.MoveTimeoutMs, ct).ConfigureAwait(false);
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
                if (CanSkipAxisMoveCommand(stage.OutputCameraX, target))
                    return 0;

                CalibrationMotionSettings motion = ResolveMotionSettings();
                int result = await stage.MoveStageAxisCommandWithMotion(
                    BinStageAxis.VisionX,
                    target,
                    motion.MoveVelocity,
                    motion.MoveAcceleration,
                    motion.MoveDeceleration,
                    "VisionCameraCalibrationSequence.OutputReticle").ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-CAMERA-CAL-OUTPUT-RETICLE-MOVE", "OutputStageUnit", "OutputVisionX Reticle 위치 이동 명령 실패. result=" + result + ", target=" + target.ToString("F3"));

                // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
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
                int reticleResult = await EnsureReticleSafeBeforePickerMoveAsync(ct).ConfigureAwait(false);
                if (reticleResult != 0)
                    return reticleResult;

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

                double target = stage.Recipe.VisionX.AvoidPosition;
                if (stage.CameraX.IsAtTargetPosition(target, 0.0))
                    return 0;

                CalibrationMotionSettings motion = ResolveMotionSettings();
                int result = await stage.MoveInputStageAxisCommandWithMotion(
                    WaferStageAxis.VisionX,
                    target,
                    motion.MoveVelocity,
                    motion.MoveAcceleration,
                    motion.MoveDeceleration).ConfigureAwait(false);
                if (result != 0)
                    return Fail("VISION-CAMERA-CAL-INPUT-VISION-AVOID", "InputStageUnit", "InputVisionX Avoid 이동 명령 실패. result=" + result + ", target=" + target.ToString("F3"));

                int wait = await stage.WaitInputStageAxisInPosition(WaferStageAxis.VisionX, target, motion.MoveTimeoutMs, ct).ConfigureAwait(false);
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
                if (stage == null ||
                    stage.OutputCameraX == null ||
                    stage.Recipe == null ||
                    stage.Recipe.VisionX == null)
                    return Fail("VISION-CAMERA-CAL-OUTPUT-VISION-MISSING", "OutputStageUnit", "OutputVisionX Avoid 이동을 위한 축 정보가 없습니다.");

                stage.Recipe.EnsurePositionObjects();
                double target = stage.Recipe.VisionX.AvoidPosition;
                if (stage.OutputCameraX.IsAtTargetPosition(target, 0.0))
                    return 0;

                CalibrationMotionSettings motion = ResolveMotionSettings();
                int result = await stage.MoveVisionXToAvoidAndVerifyAsync(
                    motion.MoveTimeoutMs,
                    motion.MoveVelocity,
                    motion.MoveAcceleration,
                    motion.MoveDeceleration,
                    ct).ConfigureAwait(false);
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

                int reticleResult = await EnsureReticleSafeBeforePickerMoveAsync(ct).ConfigureAwait(false);
                if (reticleResult != 0)
                    return reticleResult;

                CalibrationMotionSettings motion = ResolveMotionSettings();
                // 안전위치 이동은 SafeMovePercent(각 축 Default × %)를 적용한다. 미설정 시 기존 Custom 속도로 폴백.
                double safePercent = CalibrationSafeMoveMotion.ResolvePercent(_machine);
                Task<int> frontTask = _machine.PickerFrontUnit.MoveToOutputSideAvoidPositionSafeMove(safePercent, motion.MoveVelocity);
                Task<int> rearTask = _machine.PickerRearUnit.MoveToOutputSideAvoidPositionSafeMove(safePercent, motion.MoveVelocity);
                int[] results = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                    return Fail("VISION-CAMERA-CAL-PICKER-OUTPUT-AVOID", "PickerUnit", "Picker Output-side Avoid 이동 실패. frontResult=" + results[0] + ", rearResult=" + results[1]);

                ct.ThrowIfCancellationRequested();
                if (!_machine.PickerFrontUnit.IsPickerInOutputSideAvoidPosition() || !_machine.PickerRearUnit.IsPickerInOutputSideAvoidPosition())
                    return Fail("VISION-CAMERA-CAL-PICKER-OUTPUT-CHECK", "PickerUnit", "Picker Output-side Avoid 최종 위치 확인 실패. front=" + _machine.PickerFrontUnit.IsPickerInOutputSideAvoidPosition() + ", rear=" + _machine.PickerRearUnit.IsPickerInOutputSideAvoidPosition());

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

                int reticleResult = await EnsureReticleSafeBeforePickerMoveAsync(ct).ConfigureAwait(false);
                if (reticleResult != 0)
                    return reticleResult;

                CalibrationMotionSettings motion = ResolveMotionSettings();
                // 안전위치 이동은 SafeMovePercent(각 축 Default × %)를 적용한다. 미설정 시 기존 Custom 속도로 폴백.
                double safePercent = CalibrationSafeMoveMotion.ResolvePercent(_machine);
                Task<int> frontTask = _machine.PickerFrontUnit.MoveToInputSideAvoidPositionSafeMove(safePercent, motion.MoveVelocity);
                Task<int> rearTask = _machine.PickerRearUnit.MoveToInputSideAvoidPositionSafeMove(safePercent, motion.MoveVelocity);
                int[] results = await Task.WhenAll(frontTask, rearTask).ConfigureAwait(false);
                if (results[0] != 0 || results[1] != 0)
                    return Fail("VISION-CAMERA-CAL-PICKER-INPUT-AVOID", "PickerUnit", "Picker Input-side Avoid 이동 실패. frontResult=" + results[0] + ", rearResult=" + results[1]);

                ct.ThrowIfCancellationRequested();
                if (!_machine.PickerFrontUnit.IsPickerInInputSideAvoidPosition() || !_machine.PickerRearUnit.IsPickerInInputSideAvoidPosition())
                    return Fail("VISION-CAMERA-CAL-PICKER-INPUT-CHECK", "PickerUnit", "Picker Input-side Avoid 최종 위치 확인 실패. front=" + _machine.PickerFrontUnit.IsPickerInInputSideAvoidPosition() + ", rear=" + _machine.PickerRearUnit.IsPickerInInputSideAvoidPosition());

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

        private async Task<int> EnsureReticleSafeBeforePickerMoveAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                VisionUnit vision = _machine != null ? _machine.VisionUnit : null;
                if (vision == null)
                    return Fail("VISION-CAMERA-CAL-RETICLE-SAFE-NO-VISION", "VisionUnit", "Picker 이동 전 Reticle 안전 위치 확인을 위한 VisionUnit이 없습니다.");

                if (IsReticleRetracted(vision))
                    return 0;

                // 기존 조건: Picker 이동 전 Reticle을 항상 대기 위치로 복귀 —
                //           공정 위치(업+전진)에 올려 둔 Reticle이 FIND INPUT/OUTPUT 등 다른 동작마다 빠져 버렸다.
                // 현재 기준: 공정(촬영) 위치에 정상 배치된 Reticle은 그대로 유지한다.
                //           Bottom/Input/Output 카메라가 같은 Reticle Mark를 촬영해야 하므로 측정 사이에 빼면 안 되며,
                //           복귀는 RETICLE BACK 버튼(RetractReticleFromBottomCameraAsync)에서만 수행한다.
                //           업/전진 센서가 불일치하는 중간 상태일 때만 안전 복귀를 수행한다.
                if (IsReticleBottomReady(vision))
                {
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-RETICLE-KEEP",
                        "Reticle이 공정(촬영) 위치에 있어 그대로 유지합니다. RETICLE BACK 전까지 복귀하지 않습니다. up=" +
                        vision.IsVisionReticleUp() + ", rearFw=" + vision.IsVisionReticleRearSideForward());
                    return 0;
                }

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-RETICLE-SAFE-BEFORE-PICKER",
                    "Picker 이동 전 Reticle이 중간 상태여서 안전 위치로 복귀합니다. 순서=Rear Back -> Lift Down. Front Slide는 사용하지 않고 Rear Back으로 확인합니다.");

                return await RetractReticleFromBottomCameraAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-RETICLE-SAFE-EX", "VisionUnit", "Picker 이동 전 Reticle 안전 위치 복귀 예외 발생: " + ex.Message);
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

                if (IsReticleBottomReady(vision))
                {
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-RETICLE-READY-SKIP",
                        "Reticle이 이미 Bottom 촬영 위치입니다. 추가 동작 없이 현재 위치에서 촬영을 진행합니다. up=" + vision.IsVisionReticleUp() + ", rearFw=" + vision.IsVisionReticleRearSideForward());
                    return 0;
                }

                bool upReady = vision.IsVisionReticleUp();
                bool forwardReady = vision.IsVisionReticleRearSideForward();
                if (upReady && forwardReady)
                {
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-RETICLE-READY-SKIP",
                        "Reticle이 이미 Bottom 촬영 위치입니다. 추가 동작 없이 현재 위치에서 촬영을 진행합니다. up=" + upReady + ", rearFw=" + forwardReady);
                    return 0;
                }

                if (!upReady)
                {
                    int result = await vision.SetReticleLiftUpAsync(true, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    await DelayAfterReticleMotionAsync("Reticle Lift Up", ct).ConfigureAwait(false);
                }

                //Front 사용안함
                //result = await vision.SetReticleFrontSideForwardAsync(true, ct).ConfigureAwait(false);
                //if (result != 0)
                //    return result;

                if (!forwardReady)
                {
                    int result = await vision.SetReticleRearSideForwardAsync(true, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;

                    await DelayAfterReticleMotionAsync("Reticle Rear Slide Forward", ct).ConfigureAwait(false);
                }

                if (!IsReticleBottomReady(vision))
                    return Fail("VISION-CAMERA-CAL-RETICLE-CHECK", "VisionUnit", "Bottom 카메라 촬영 전 Reticle 위치 확인 실패. up=" + vision.IsVisionReticleUp() + ", rearFw=" + vision.IsVisionReticleRearSideForward() + ", frontFw=" + vision.IsVisionReticleFrontSideForward() + " (Front Slide 미사용, Rear Forward 기준)");

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

        private async Task DelayAfterReticleMotionAsync(string motionName, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-RETICLE-SETTLE",
                motionName + " 완료 후 안정화 대기. delayMs=" + ReticleMotionSettleDelayMs);
            await Task.Delay(ReticleMotionSettleDelayMs, ct).ConfigureAwait(false);
        }

        public async Task<int> RetractReticleFromBottomCameraAsync(CancellationToken ct)
        {
            return await RetractReticleFromBottomCameraAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> RetractReticleFromBottomCameraAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(runMode == SequenceRunMode.Auto, "VisionCameraCalibrationSequence.RetractReticleFromBottomCameraAsync:" + runMode))
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

                result = await vision.SetReticleLiftUpAsync(false, ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                if (!IsReticleRetracted(vision))
                    return Fail("VISION-CAMERA-CAL-RETICLE-RETRACT-CHECK", "VisionUnit", "Reticle 복귀 후 위치 확인 실패. down=" + vision.IsVisionReticleDown() + ", rearBw=" + vision.IsVisionReticleRearSideBackward() + ", frontBw=" + vision.IsVisionReticleFrontSideBackward() + " (Front Slide 미사용, Rear Back 기준)");

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-RETICLE-RETRACT", "Reticle이 Rear Back -> Lift Down 순서로 복귀했습니다. Front Slide는 Rear Back 기준으로 확인합니다.");
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

        private static bool CanSkipAxisMoveCommand(BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : CalibrationAxisTolerance;
            return axis.IsAtTargetPosition(target, tolerance);
        }

        private async Task<VisionReticleMeasurement> FindReticleWithRetryAsync(VisionCameraCalibrationTarget target, CancellationToken ct)
        {
            try
            {
                VisionReticleMeasurement lastMeasurement = null;
                for (int attempt = 1; attempt <= ReticleFindRetryCount; attempt++)
                {
                    ct.ThrowIfCancellationRequested();
                    _visionNotConnectedAborted = false;
                    lastMeasurement = await FindReticleAsync(target, ct).ConfigureAwait(false);
                    if (lastMeasurement != null && lastMeasurement.Valid && IsValidReticleMeasurement(lastMeasurement))
                        return lastMeasurement;

                    // Vision 미연결은 재시도해도 회복되지 않는다. 시도마다 Fail(알람)이 반복되므로 즉시 중단한다.
                    if (_visionNotConnectedAborted)
                        break;

                    EventLogger.Write(EventKind.Warning, "CAL", "VISION-CAMERA-CAL-RETICLE-RETRY",
                        ResolveCameraName(target) + " ReticleFinder 결과가 NG입니다. retry=" + attempt + "/" + ReticleFindRetryCount);
                }

                // 리트라이 소진: 비유한값(NaN/Infinity) 측정이 Valid=true로 저장 경로에 흘러가지 않도록 무효화한다.
                if (lastMeasurement != null && !IsValidReticleMeasurement(lastMeasurement))
                    lastMeasurement.Valid = false;

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

            // 비유한값(NaN/Infinity)은 카메라 스케일을 변형하기 전에 차단한다.
            // ApplyImageSize의 가드(<=0 검사)는 NaN을 통과시키고, 대상은 영속화되는 live 객체이므로
            // 여기서 막지 않으면 ImageWidthPixel/ImageCenterPixel이 NaN으로 저장돼 영구 손상이 된다.
            if (!IsFiniteMatchResult(match))
            {
                EventLogger.Write(EventKind.Warning, "CAL", "VISION-CAMERA-CAL-RETICLE-NONFINITE",
                    ResolveCameraName(target) + " ReticleFinder 결과에 비유한값이 있어 측정을 버립니다. " +
                    "x=" + match.X + ", y=" + match.Y + ", angle=" + match.AngleDeg + ", score=" + match.Score +
                    ", imageSize=" + match.ImageWidthPixel + "x" + match.ImageHeightPixel);
                return null;
            }

            // 시뮬/bypass 여부는 "이번 측정 결과"로 판정해 메모리에만 기록한다(영속화된 Raw로 판정하지 않는다).
            SetMeasurementSimulated(target, IsSimulatedMatchResult(match));

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

        // Vision 미연결로 측정 요청이 중단됐는지(리트라이 즉시 중단용).
        private bool _visionNotConnectedAborted;

        // 이번 세션에서 해당 카메라 측정이 시뮬/bypass 결과였는지(메모리 전용, 영속화 안 함).
        private bool _bottomMeasurementSimulated;
        private bool _inputMeasurementSimulated;
        private bool _outputMeasurementSimulated;

        private void SetMeasurementSimulated(VisionCameraCalibrationTarget target, bool simulated)
        {
            if (target == VisionCameraCalibrationTarget.Bottom)
                _bottomMeasurementSimulated = simulated;
            else if (target == VisionCameraCalibrationTarget.Input)
                _inputMeasurementSimulated = simulated;
            else
                _outputMeasurementSimulated = simulated;
        }

        private bool IsMeasurementSimulated(VisionCameraCalibrationTarget target)
        {
            if (target == VisionCameraCalibrationTarget.Bottom)
                return _bottomMeasurementSimulated;
            if (target == VisionCameraCalibrationTarget.Input)
                return _inputMeasurementSimulated;
            return _outputMeasurementSimulated;
        }

        /// <summary>이번 측정 결과가 시뮬/bypass로 생성된 값인지 판정한다(실측이 아니면 실 캘리브레이션에 저장하지 않는다).</summary>
        private static bool IsSimulatedMatchResult(MatchResultDto match)
        {
            if (match == null)
                return false;

            string raw = match.RawError ?? string.Empty;
            return raw.StartsWith("SIM:", StringComparison.OrdinalIgnoreCase) ||
                   raw.StartsWith("SIMULATION:", StringComparison.OrdinalIgnoreCase) ||
                   raw.StartsWith("BYPASS:", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFiniteMatchResult(MatchResultDto match)
        {
            if (match == null)
                return false;

            if (!IsFinite(match.X) || !IsFinite(match.Y) || !IsFinite(match.AngleDeg) || !IsFinite(match.Score))
                return false;

            if (match.HasImageSize &&
                (!IsFinite(match.ImageWidthPixel) || !IsFinite(match.ImageHeightPixel)))
                return false;

            return true;
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

            if (IsDryRunWithVisionConnected(channel))
            {
                await AutoVisionRequestService.GrabAsync(
                    channel,
                    0,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                return BuildSimulatedReticleMatch(target, channel, "DryRun 모드에서 Vision GRAB만 수행하고 시뮬레이션 결과를 사용합니다.");
            }

            EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-MATCHASYNC-REQ",
                cameraName + " Vision에 ReticleFinder INSPECT_SYNC 시작을 요청합니다.");

            bool started = await AutoVisionRequestService.StartMatchAsync(
                channel,
                ReticleFinderName,
                0,
                timeoutMs,
                ct).ConfigureAwait(false);

            if (started)
            {
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-MATCHASYNC-STARTED",
                    cameraName + " Vision ReticleFinder INSPECT_SYNC EPD 또는 bypass 허가를 받았습니다.");

                return await WaitReticleMatchResultAsync(cameraName, channel, timeoutMs, ct).ConfigureAwait(false);
            }

            if (IsVisionResultSimulationAllowed())
                return BuildSimulatedReticleMatch(target, channel, "INSPECT_SYNC 시작 실패 후 시뮬레이션 결과를 사용합니다.");

            if (VisionCommandService.IsConnected(channel))
            {
                return new MatchResultDto
                {
                    Success = false,
                    RawError = cameraName + " ReticleFinder INSPECT_SYNC EPD를 받지 못했습니다."
                };
            }

            // 미연결은 재시도 대상이 아니다 — 호출부(FindReticleWithRetryAsync)가 즉시 중단하도록 표시한다.
            _visionNotConnectedAborted = true;
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

                MatchResultDto result = await AutoVisionRequestService.WaitMatchResultAsync(
                    channel,
                    ReticleFinderName,
                    0,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                if (result != null && result.Success)
                {
                    EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-MATCHRESULT-DONE",
                        cameraName + " Vision ReticleFinder RESULT 완료. x=" + result.X.ToString("0.###") +
                        ", y=" + result.Y.ToString("0.###") +
                        ", r=" + result.AngleDeg.ToString("0.###") +
                        ", score=" + result.Score.ToString("0.###"));
                    return result;
                }

                return new MatchResultDto
                {
                    Success = false,
                    RawError = cameraName + " ReticleFinder RESULT 실패: " +
                               (result != null && !string.IsNullOrWhiteSpace(result.RawError)
                                   ? result.RawError
                                   : "응답이 없습니다.")
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
                    RawError = cameraName + " ReticleFinder RESULT 처리 중 예외 발생: " + ex.Message
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
                    return AutoVisionChannel.BottomInspection;
                case VisionCameraCalibrationTarget.Input:
                    return AutoVisionChannel.Wafer;
                case VisionCameraCalibrationTarget.Output:
                    return AutoVisionChannel.Bin;
                default:
                    return AutoVisionChannel.BottomInspection;
            }
        }

        private string ResolveCameraName(VisionCameraCalibrationTarget target)
        {
            switch (target)
            {
                case VisionCameraCalibrationTarget.Bottom:
                    return VisionModuleNames.BottomInspection;
                case VisionCameraCalibrationTarget.Input:
                    return VisionModuleNames.Wafer;
                case VisionCameraCalibrationTarget.Output:
                    return VisionModuleNames.Bin;
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

        private bool IsVisionResultSimulationAllowed()
        {
            if (IsSimulationMode())
                return true;

            AppSettings settings = AppSettingsStore.Current;
            return settings != null && !settings.UseVision;
        }

        private MatchResultDto BuildSimulatedReticleMatch(
            VisionCameraCalibrationTarget target,
            AutoVisionChannel channel,
            string reason)
        {
            VisionCameraCalibrationData data = CalibrationData;
            VisionCameraPixelCalibration camera = VisionCameraCalibrationTransform.ResolveCamera(data, channel);
            if (camera == null)
                camera = new VisionCameraPixelCalibration();

            camera.EnsureDefaults(320.0, 240.0, 0.001, 0.001);

            if (IsDryRunWithVisionDisabled())
            {
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-SIM-RETICLE-ZERO",
                    ResolveCameraName(target) + " Vision ReticleFinder 결과를 0 보정으로 처리합니다. " + reason +
                    " centerPixel=(" + camera.ImageCenterPixelX.ToString("F3") + ", " + camera.ImageCenterPixelY.ToString("F3") + ")" +
                    ", scale=(" + camera.PixelToMmX.ToString("F9") + ", " + camera.PixelToMmY.ToString("F9") + ") mm/px" +
                    ", image=(" + camera.ImageWidthPixel.ToString("F0") + "x" + camera.ImageHeightPixel.ToString("F0") + ")");

                return new MatchResultDto
                {
                    Success = true,
                    X = camera.ImageCenterPixelX,
                    Y = camera.ImageCenterPixelY,
                    AngleDeg = 0.0,
                    Score = 1.0,
                    HasImageSize = true,
                    ImageWidthPixel = camera.ImageWidthPixel,
                    ImageHeightPixel = camera.ImageHeightPixel,
                    RawError = "SIM:ReticleFinderPixelOffset:ZeroOffset"
                };
            }

            double pixelX = NextSimulatedReticlePixel(camera.ImageCenterPixelX, SimReticleMaxPixelOffset);
            double pixelY = NextSimulatedReticlePixel(camera.ImageCenterPixelY, SimReticleMaxPixelOffset);
            double angle = NextSimulatedReticlePixel(0.0, SimReticleMaxAngleDeg);
            double score = NextSimulatedReticleScore();

            EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-SIM-RETICLE",
                ResolveCameraName(target) + " Vision ReticleFinder 결과를 시뮬레이션합니다. " + reason +
                " centerPixel=(" + camera.ImageCenterPixelX.ToString("F3") + ", " + camera.ImageCenterPixelY.ToString("F3") + ")" +
                ", simulatedPixel=(" + pixelX.ToString("F3") + ", " + pixelY.ToString("F3") + ")" +
                ", scale=(" + camera.PixelToMmX.ToString("F9") + ", " + camera.PixelToMmY.ToString("F9") + ") mm/px" +
                ", image=(" + camera.ImageWidthPixel.ToString("F0") + "x" + camera.ImageHeightPixel.ToString("F0") + ")" +
                ", score=" + score.ToString("F6") +
                ", angle=" + angle.ToString("F6"));

            return new MatchResultDto
            {
                Success = true,
                X = pixelX,
                Y = pixelY,
                AngleDeg = angle,
                Score = score,
                HasImageSize = true,
                ImageWidthPixel = camera.ImageWidthPixel,
                ImageHeightPixel = camera.ImageHeightPixel,
                RawError = "SIM:ReticleFinderPixelOffset"
            };
        }

        private static bool IsDryRunWithVisionDisabled()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && settings.DryRunMode && !settings.UseVision;
        }

        private static bool IsDryRunWithVisionConnected(AutoVisionChannel channel)
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null || !settings.DryRunMode || !settings.UseVision)
                    return false;

                return VisionCommandService.IsConnected(channel);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static double NextSimulatedReticlePixel(double center, double maxAbsOffset)
        {
            lock (SimReticleRandomLock)
            {
                return center + ((SimReticleRandom.NextDouble() * 2.0) - 1.0) * maxAbsOffset;
            }
        }

        private static double NextSimulatedReticleScore()
        {
            lock (SimReticleRandomLock)
            {
                return 0.985 + (SimReticleRandom.NextDouble() * 0.014);
            }
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

        private void LogCalibrationFormulaData(VisionCameraCalibrationData data)
        {
            try
            {
                if (data == null)
                    return;

                data.EnsureObjects();

                VisionCameraPixelCalibration bottomCamera = VisionCameraCalibrationTransform.ResolveCamera(data, AutoVisionChannel.BottomInspection);
                VisionCameraPixelCalibration inputCamera = VisionCameraCalibrationTransform.ResolveCamera(data, AutoVisionChannel.Wafer);
                VisionCameraPixelCalibration outputCamera = VisionCameraCalibrationTransform.ResolveCamera(data, AutoVisionChannel.Bin);

                QMC.Common.Log.Write("Calibration", GetUserName(), "VisionCameraCalFormulaPixel",
                    "Vision Camera Calibration 계산 원본. " +
                    BuildMeasurementLog("Bottom", data.BottomReticle, bottomCamera) + " / " +
                    BuildMeasurementLog("Input", data.InputReticle, inputCamera) + " / " +
                    BuildMeasurementLog("Output", data.OutputReticle, outputCamera));

                QMC.Common.Log.Write("Calibration", GetUserName(), "VisionCameraCalFormulaMm",
                    "Vision Camera Calibration Pixel->mm 수식. " +
                    BuildPixelToMmFormulaLog("Bottom", data.BottomReticle, bottomCamera) + " / " +
                    BuildPixelToMmFormulaLog("Input", data.InputReticle, inputCamera) + " / " +
                    BuildPixelToMmFormulaLog("Output", data.OutputReticle, outputCamera));

                QMC.Common.Log.Write("Calibration", GetUserName(), "VisionCameraCalFormulaOffset",
                    "Vision Camera Calibration Offset 수식. " +
                    "Bottom-Input X = BottomMmX - InputMmX = (" + data.BottomReticle.MmX.ToString("F6") + " - " + data.InputReticle.MmX.ToString("F6") + ") = " + data.InputToBottomOffsetX.ToString("F6") + " mm(PickBridge), " +
                    "Bottom-Input Y = BottomMmY - InputMmY = (" + data.BottomReticle.MmY.ToString("F6") + " - " + data.InputReticle.MmY.ToString("F6") + ") = " + data.InputToBottomOffsetY.ToString("F6") + " mm(PickBridge), " +
                    "Bottom-Output X = -(BottomMmX + OutputMmX) = -(" + data.BottomReticle.MmX.ToString("F6") + " + " + data.OutputReticle.MmX.ToString("F6") + ") = " + data.OutputToBottomOffsetX.ToString("F6") + " mm(PlaceBridge), " +
                    "Bottom-Output Y = -(BottomMmY + OutputMmY) = -(" + data.BottomReticle.MmY.ToString("F6") + " + " + data.OutputReticle.MmY.ToString("F6") + ") = " + data.OutputToBottomOffsetY.ToString("F6") + " mm(PlaceBridge)");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "CAL", "VISION-CAMERA-CAL-CALC-FORMULA-LOG-FAIL",
                    "Vision Camera Calibration 계산 수식 로그 저장 실패: " + ex.Message);
            }
            finally
            {
            }
        }

        private static string BuildMeasurementLog(string name, VisionReticleMeasurement measurement, VisionCameraPixelCalibration camera)
        {
            if (measurement == null || camera == null)
                return name + "=null";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(name);
            sb.Append("[pixel=(").Append(measurement.PixelX.ToString("F3")).Append(", ").Append(measurement.PixelY.ToString("F3")).Append(")");
            sb.Append(", mm=(").Append(measurement.MmX.ToString("F6")).Append(", ").Append(measurement.MmY.ToString("F6")).Append(")");
            sb.Append(", angle=").Append(measurement.AngleDeg.ToString("F6"));
            sb.Append(", score=").Append(measurement.Score.ToString("F6"));
            sb.Append(", center=(").Append(camera.ImageCenterPixelX.ToString("F3")).Append(", ").Append(camera.ImageCenterPixelY.ToString("F3")).Append(")");
            sb.Append(", scale=(").Append(camera.PixelToMmX.ToString("F9")).Append(", ").Append(camera.PixelToMmY.ToString("F9")).Append(") mm/px");
            sb.Append(", image=(").Append(camera.ImageWidthPixel.ToString("F0")).Append("x").Append(camera.ImageHeightPixel.ToString("F0")).Append(")");
            if (measurement.HasVisionXPosition)
                sb.Append(", visionX=").Append(measurement.VisionXPosition.ToString("F6"));
            if (measurement.HasStageYPosition)
                sb.Append(", stageY=").Append(measurement.StageYPosition.ToString("F6"));
            sb.Append("]");
            return sb.ToString();
        }

        private static string BuildPixelToMmFormulaLog(string name, VisionReticleMeasurement measurement, VisionCameraPixelCalibration camera)
        {
            if (measurement == null || camera == null)
                return name + "=null";

            return name +
                   " MmX = (PixelX - CenterX) * ScaleX = (" + measurement.PixelX.ToString("F3") + " - " + camera.ImageCenterPixelX.ToString("F3") + ") * " + camera.PixelToMmX.ToString("F9") + " = " + measurement.MmX.ToString("F6") + " mm, " +
                   name +
                   " MmY = (CenterY - PixelY) * ScaleY = (" + camera.ImageCenterPixelY.ToString("F3") + " - " + measurement.PixelY.ToString("F3") + ") * " + camera.PixelToMmY.ToString("F9") + " = " + measurement.MmY.ToString("F6") + " mm";
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

        private void PersistMeasuredCalibrationData(VisionCameraCalibrationTarget target, string label)
        {
            try
            {
                // 시뮬레이션/bypass 측정은 실 캘리브레이션 파일을 덮어쓰지 않도록 저장을 생략한다.
                // 판정은 "이번에 측정한 그 카메라"만 본다 — 다른 카메라의 옛 값 때문에 정상 측정 저장이 막히면 안 된다.
                if (IsMeasurementSimulated(target))
                {
                    EventLogger.Write(EventKind.Warning, "CAL", "VISION-CAMERA-CAL-MEASURE-SIM-SKIP",
                        label + " 저장 생략: " + ResolveCameraName(target) +
                        " 측정이 시뮬레이션/bypass 결과입니다. 실 Vision 연결 상태에서 다시 측정하세요.");
                    return;
                }

                TouchCalibrationData();
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

        /// <summary>이번 세션 측정 중 시뮬/bypass 결과가 섞여 있는지(계산·저장 차단용). 영속화된 값이 아니라 메모리 플래그로 판정한다.</summary>
        private bool HasSimulatedMeasurementInSession(out string cameraNames)
        {
            List<string> simulated = new List<string>();
            if (_bottomMeasurementSimulated)
                simulated.Add("Bottom");
            if (_inputMeasurementSimulated)
                simulated.Add("Input");
            if (_outputMeasurementSimulated)
                simulated.Add("Output");

            cameraNames = string.Join(",", simulated.ToArray());
            return simulated.Count > 0;
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

        private void TouchCalibrationData()
        {
            try
            {
                if (_machine == null ||
                    _machine.VisionUnit == null ||
                    _machine.VisionUnit.Config == null ||
                    _machine.VisionUnit.Config.CalibrationData == null)
                    return;

                _machine.VisionUnit.Config.CalibrationData.Touch(GetUserName());
            }
            catch
            {
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
