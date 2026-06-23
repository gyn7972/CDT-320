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

    public sealed class VisionCameraCalibrationSequence
    {
        private const string ReticleFinderName = "ReticleFinder";
        private readonly CDT320_Machine _machine;
        private readonly Action _saveSettings;
        private readonly Func<string> _userNameProvider;

        public VisionCameraCalibrationSequence(
            CDT320_Machine machine,
            Action saveSettings,
            Func<string> userNameProvider)
        {
            _machine = machine;
            _saveSettings = saveSettings;
            _userNameProvider = userNameProvider;
        }

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
                int result = CheckUnit();
                if (result != 0)
                    return result;

                result = await PrepareReticleAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await FindBottomReticleAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveInputCameraToReticleAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await FindInputReticleAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await MoveOutputCameraToReticleAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = await FindOutputReticleAsync(ct).ConfigureAwait(false);
                if (result != 0)
                    return result;

                result = CalculateCalibration();
                if (result != 0)
                    return result;

                return SaveCalibration();
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

        public async Task<int> FindBottomReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                int check = CheckUnit();
                if (check != 0)
                    return check;

                VisionReticleMeasurement measurement = await FindReticleAsync(VisionCameraCalibrationTarget.Bottom, ct).ConfigureAwait(false);
                if (measurement == null || !measurement.Valid)
                    return Fail("VISION-CAMERA-CAL-BOTTOM-FIND", "BottomInspection", "Bottom 카메라 Reticle 찾기 실패.");

                CalibrationData.BottomReticle = measurement;
                CalibrationData.Valid = false;
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-BOTTOM",
                    "Bottom 카메라 Reticle 측정 완료. pixel=(" + measurement.PixelX.ToString("F3") + "," + measurement.PixelY.ToString("F3") +
                    "), mm=(" + measurement.MmX.ToString("F6") + "," + measurement.MmY.ToString("F6") + ")");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-BOTTOM-EX", "BottomInspection", "Bottom 카메라 Reticle 찾기 예외 발생: " + ex.Message);
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

                VisionReticleMeasurement measurement = await FindReticleAsync(VisionCameraCalibrationTarget.Input, ct).ConfigureAwait(false);
                if (measurement == null || !measurement.Valid)
                    return Fail("VISION-CAMERA-CAL-INPUT-FIND", "WaferVision", "Input 카메라 Reticle 찾기 실패.");

                CalibrationData.InputReticle = measurement;
                CalibrationData.Valid = false;
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-INPUT",
                    "Input 카메라 Reticle 측정 완료. pixel=(" + measurement.PixelX.ToString("F3") + "," + measurement.PixelY.ToString("F3") +
                    "), mm=(" + measurement.MmX.ToString("F6") + "," + measurement.MmY.ToString("F6") + ")");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-INPUT-EX", "WaferVision", "Input 카메라 Reticle 찾기 예외 발생: " + ex.Message);
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

                VisionReticleMeasurement measurement = await FindReticleAsync(VisionCameraCalibrationTarget.Output, ct).ConfigureAwait(false);
                if (measurement == null || !measurement.Valid)
                    return Fail("VISION-CAMERA-CAL-OUTPUT-FIND", "BinVision", "Output 카메라 Reticle 찾기 실패.");

                CalibrationData.OutputReticle = measurement;
                CalibrationData.Valid = false;
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-OUTPUT",
                    "Output 카메라 Reticle 측정 완료. pixel=(" + measurement.PixelX.ToString("F3") + "," + measurement.PixelY.ToString("F3") +
                    "), mm=(" + measurement.MmX.ToString("F6") + "," + measurement.MmY.ToString("F6") + ")");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("VISION-CAMERA-CAL-OUTPUT-EX", "BinVision", "Output 카메라 Reticle 찾기 예외 발생: " + ex.Message);
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
                    return Fail("VISION-CAMERA-CAL-CALC", "VisionUnit", "Vision Camera Calibration 계산 실패. Bottom/Input/Output Reticle 측정값이 모두 필요합니다.");

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-CALC",
                    "Vision Camera Calibration 계산 완료. InputOffset=(" +
                    CalibrationData.InputToBottomOffsetX.ToString("F6") + "," +
                    CalibrationData.InputToBottomOffsetY.ToString("F6") + "), OutputOffset=(" +
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

                if (_saveSettings != null)
                    _saveSettings();
                else
                    _machine.SaveSettings();

                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-SAVE", "Vision Camera Calibration 데이터를 저장했습니다.");
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
                    "Reticle 방향 자동 이동은 보류합니다. 작업자가 Reticle을 Bottom/Input/Output 카메라가 볼 수 있는 위치로 수동 준비한 상태에서 측정합니다.");
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

        private Task<int> MoveInputCameraToReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-INPUT-MOVE-SKIP",
                    "Input 카메라 Reticle 자동 이동은 보류합니다. InputStage RETICLE POSITION 티칭 위치 또는 수동 조그 위치에서 측정합니다.");
                return Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("VISION-CAMERA-CAL-INPUT-MOVE-EX", "InputStageUnit", "Input 카메라 Reticle 이동 준비 예외 발생: " + ex.Message));
            }
            finally
            {
            }
        }

        private Task<int> MoveOutputCameraToReticleAsync(CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                EventLogger.Write(EventKind.Event, "CAL", "VISION-CAMERA-CAL-OUTPUT-MOVE-SKIP",
                    "Output 카메라 Reticle 자동 이동은 보류합니다. OutputStage VISION RETICLE POSITION 티칭 위치 또는 수동 조그 위치에서 측정합니다.");
                return Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Task.FromResult(Fail("VISION-CAMERA-CAL-OUTPUT-MOVE-EX", "OutputStageUnit", "Output 카메라 Reticle 이동 준비 예외 발생: " + ex.Message));
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
            measurement.MmX = (match.X - data.ImageCenterPixelX) * data.PixelToMmX;
            measurement.MmY = (match.Y - data.ImageCenterPixelY) * data.PixelToMmY;
            measurement.Score = match.Score;
            measurement.AngleDeg = match.AngleDeg;
            measurement.MeasuredAt = DateTime.Now;
            measurement.Raw = match.RawError ?? string.Empty;
            FillAxisPositions(target, measurement);
            return measurement;
        }

        private async Task<MatchResultDto> RequestReticleMatchAsync(VisionCameraCalibrationTarget target, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            VisionTcpClient client = ResolveClient(target);
            if (client != null && client.IsConnected)
                return await client.MatchAsync(ReticleFinderName, 0, ResolveCaptureTimeoutMs(), ct).ConfigureAwait(false);

            if (IsSimulationMode())
            {
                VisionCameraCalibrationData data = CalibrationData;
                return new MatchResultDto
                {
                    Success = true,
                    X = data.ImageCenterPixelX,
                    Y = data.ImageCenterPixelY,
                    AngleDeg = 0,
                    Score = 1.0,
                    RawError = "SIM:ReticleFinder"
                };
            }

            string name = ResolveCameraName(target);
            Fail("VISION-CAMERA-CAL-VISION-NOT-CONNECTED", name, name + " Vision이 연결되지 않아 ReticleFinder를 실행할 수 없습니다.");
            return null;
        }

        private VisionTcpClient ResolveClient(VisionCameraCalibrationTarget target)
        {
            switch (target)
            {
                case VisionCameraCalibrationTarget.Bottom:
                    return VisionHub.Inspection;
                case VisionCameraCalibrationTarget.Input:
                    return VisionHub.Wafer;
                case VisionCameraCalibrationTarget.Output:
                    return VisionHub.Bin;
                default:
                    return null;
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
            return unit != null &&
                   ((unit.Setup != null && unit.Setup.IsSimulationMode) ||
                    (unit.Config != null && unit.Config.IsSimulationMode));
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
