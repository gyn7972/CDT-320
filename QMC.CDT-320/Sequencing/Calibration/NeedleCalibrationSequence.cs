using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    public enum NeedleCalibrationStep
    {
        None,
        CheckUnit,
        MoveSafeStartPosition,
        MoveNeedleXToTouchTeachingPosition,
        SearchNeedleCapBy100um,
        BackOffNeedleCap100um,
        SearchNeedleCapBy10um,
        BackOffNeedleCap10um,
        SearchNeedleCapBy1um,
        BackOffNeedleCap1um,
        SaveNeedleCapTouchPosition,
        MoveNeedlePinTeachingPosition,
        MoveNeedleCapNearTouchPosition,
        SearchNeedlePinBy10um,
        BackOffNeedlePin10um,
        SearchNeedlePinBy1um,
        BackOffNeedlePin1um,
        SaveNeedlePinFlushPosition,
        CalculateNeedlePinReadyPosition,
        Complete
    }

    public sealed class NeedleCalibrationResult
    {
        public bool Success { get; set; }
        public double TouchStageYPosition { get; set; }
        public double TouchNeedleXPosition { get; set; }
        public double NeedleCapTouchPosition { get; set; }
        public double NeedlePinFlushPosition { get; set; }
        public double NeedlePinReadyPosition { get; set; }
        public string Message { get; set; }
    }

    public sealed class NeedleCalibrationSequence
    {
        private const double Step100umMm = 0.1;
        private const double Step10umMm = 0.01;
        private const double Step1umMm = 0.001;
        private const int MaxSearchIterations = 20000;

        private readonly MachineSequenceContext _context;
        private SequenceRunMode _runMode;
        private SequenceResourceLease _inputStageLease;
        private InputStageUnit _stage;
        private NeedleZCalibrationSettings _settings;
        private double _capApproachSign;
        private double _pinApproachSign;
        private double _needleCapTouchPosition;
        private double _needlePinFlushPosition;
        private double _needlePinReadyPosition;

        private sealed class NeedleSearchResult
        {
            public int Code { get; set; }
            public double DetectedPosition { get; set; }
        }

        public NeedleCalibrationSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            CurrentStep = NeedleCalibrationStep.None;
            Result = new NeedleCalibrationResult
            {
                Message = string.Empty
            };
        }

        public NeedleCalibrationStep CurrentStep { get; private set; }
        public NeedleCalibrationResult Result { get; private set; }

        public async Task<int> MoveTouchTeachingPositionOnlyAsync(CancellationToken ct)
        {
            return await MoveTouchTeachingPositionOnlyAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> MoveTouchTeachingPositionOnlyAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(
                runMode == SequenceRunMode.Auto,
                "NeedleCalibrationSequence.MoveTouchTeachingPositionOnlyAsync:" + runMode))
            {
                _runMode = runMode;
                try
                {
                    int result = CheckUnit();
                    if (result != 0) return result;

                    result = await AcquireInputStageAreaAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.MoveSafeStartPosition;
                    result = await PrepareSafeStartPositionAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.MoveNeedleXToTouchTeachingPosition;
                    result = await MoveNeedleXToTouchTeachingPositionAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    Result.Success = true;
                    Result.Message = "Needle Calibration touch teaching position move complete.";
                    return 0;
                }
                catch (OperationCanceledException)
                {
                    StopNeedleAxes();
                    throw;
                }
                catch (Exception ex)
                {
                    StopNeedleAxes();
                    return Fail("NEEDLE-CAL-MOVE-TOUCH-EX", "NeedleCalibrationSequence", "Needle Calibration touch teaching 이동 예외 발생. error=" + ex.Message);
                }
                finally
                {
                    ReleaseInputStageArea();
                }
            }
        }

        public async Task<int> MoveAvoidOnlyAsync(CancellationToken ct)
        {
            return await MoveAvoidOnlyAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> MoveAvoidOnlyAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(
                runMode == SequenceRunMode.Auto,
                "NeedleCalibrationSequence.MoveAvoidOnlyAsync:" + runMode))
            {
                _runMode = runMode;
                try
                {
                    int result = CheckUnit();
                    if (result != 0) return result;

                    result = await MoveNeedleAxesToAvoidAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    Result.Success = true;
                    Result.Message = "Needle Calibration Z avoid move complete.";
                    return 0;
                }
                catch (OperationCanceledException)
                {
                    StopNeedleAxes();
                    throw;
                }
                catch (Exception ex)
                {
                    StopNeedleAxes();
                    return Fail("NEEDLE-CAL-AVOID-EX", "NeedleCalibrationSequence", "Needle Calibration Z Avoid 이동 예외 발생. error=" + ex.Message);
                }
                finally
                {
                }
            }
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            return await RunAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> RunAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(
                runMode == SequenceRunMode.Auto,
                "NeedleCalibrationSequence.RunAsync:" + runMode))
            {
                _runMode = runMode;
                try
                {
                    CurrentStep = NeedleCalibrationStep.CheckUnit;
                    int result = CheckUnit();
                    if (result != 0) return result;

                    result = await AcquireInputStageAreaAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.MoveSafeStartPosition;
                    result = await PrepareSafeStartPositionAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.MoveNeedleXToTouchTeachingPosition;
                    result = await MoveNeedleXToTouchTeachingPositionAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.SearchNeedleCapBy100um;
                    result = await MoveNeedleCapTeachingAndSearchAsync(Step100umMm, _settings.CapSearch100umMaxDistanceMm, "NeedleCap 100um search", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.BackOffNeedleCap100um;
                    result = await BackOffAxisAsync(WaferStageAxis.EjectPinZ, _stage.EjectPinZ, _capApproachSign, Step100umMm, "NeedleCap 100um backoff", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.SearchNeedleCapBy10um;
                    NeedleSearchResult search = await SearchAxisByStepAsync(WaferStageAxis.EjectPinZ, _stage.EjectPinZ, _capApproachSign, Step10umMm, _settings.CapSearch10umMaxDistanceMm, "NeedleCap 10um search", ct).ConfigureAwait(false);
                    if (search.Code != 0) return search.Code;
                    _needleCapTouchPosition = search.DetectedPosition;

                    CurrentStep = NeedleCalibrationStep.BackOffNeedleCap10um;
                    result = await BackOffAxisAsync(WaferStageAxis.EjectPinZ, _stage.EjectPinZ, _capApproachSign, Step10umMm, "NeedleCap 10um backoff", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.SearchNeedleCapBy1um;
                    search = await SearchAxisByStepAsync(WaferStageAxis.EjectPinZ, _stage.EjectPinZ, _capApproachSign, Step1umMm, _settings.CapSearch1umMaxDistanceMm, "NeedleCap 1um search", ct).ConfigureAwait(false);
                    if (search.Code != 0) return search.Code;
                    _needleCapTouchPosition = search.DetectedPosition;

                    CurrentStep = NeedleCalibrationStep.BackOffNeedleCap1um;
                    result = await BackOffAxisAsync(WaferStageAxis.EjectPinZ, _stage.EjectPinZ, _capApproachSign, Step1umMm, "NeedleCap 1um backoff", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.SaveNeedleCapTouchPosition;
                    result = SaveNeedleCapTouchPosition();
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.MoveNeedlePinTeachingPosition;
                    result = await MoveNeedlePinTeachingPositionAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.MoveNeedleCapNearTouchPosition;
                    result = await MoveNeedleCapNearTouchPositionAsync(ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.SearchNeedlePinBy10um;
                    search = await SearchAxisByStepAsync(WaferStageAxis.NeedleZ, _stage.NeedleZ, _pinApproachSign, Step10umMm, _settings.PinSearch10umMaxDistanceMm, "NeedlePin 10um search", ct).ConfigureAwait(false);
                    if (search.Code != 0) return search.Code;
                    _needlePinFlushPosition = search.DetectedPosition;

                    CurrentStep = NeedleCalibrationStep.BackOffNeedlePin10um;
                    result = await BackOffAxisAsync(WaferStageAxis.NeedleZ, _stage.NeedleZ, _pinApproachSign, Step10umMm, "NeedlePin 10um backoff", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.SearchNeedlePinBy1um;
                    search = await SearchAxisByStepAsync(WaferStageAxis.NeedleZ, _stage.NeedleZ, _pinApproachSign, Step1umMm, _settings.PinSearch1umMaxDistanceMm, "NeedlePin 1um search", ct).ConfigureAwait(false);
                    if (search.Code != 0) return search.Code;
                    _needlePinFlushPosition = search.DetectedPosition;

                    CurrentStep = NeedleCalibrationStep.BackOffNeedlePin1um;
                    result = await BackOffAxisAsync(WaferStageAxis.NeedleZ, _stage.NeedleZ, _pinApproachSign, Step1umMm, "NeedlePin 1um backoff", ct).ConfigureAwait(false);
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.SaveNeedlePinFlushPosition;
                    result = SaveNeedlePinFlushPosition();
                    if (result != 0) return result;

                    CurrentStep = NeedleCalibrationStep.CalculateNeedlePinReadyPosition;
                    result = CalculateAndSaveNeedlePinReadyPosition();
                    if (result != 0) return result;

                    if (_settings.MoveAvoidAfterCalibration)
                    {
                        result = await MoveNeedleAxesToAvoidAsync(ct).ConfigureAwait(false);
                        if (result != 0) return result;
                    }

                    CurrentStep = NeedleCalibrationStep.Complete;
                    Result.Success = true;
                    Result.Message = "Needle Calibration complete.";
                    EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-DONE",
                        "Needle Calibration 완료. capTouch=" + _needleCapTouchPosition.ToString("F6") +
                        ", pinFlush=" + _needlePinFlushPosition.ToString("F6") +
                        ", pinReady=" + _needlePinReadyPosition.ToString("F6"));
                    return 0;
                }
                catch (OperationCanceledException)
                {
                    StopNeedleAxes();
                    throw;
                }
                catch (SequenceStopException)
                {
                    StopNeedleAxes();
                    throw;
                }
                catch (Exception ex)
                {
                    StopNeedleAxes();
                    return Fail("NEEDLE-CAL-EX", "NeedleCalibrationSequence", "Needle Calibration 예외 발생. error=" + ex.Message);
                }
                finally
                {
                    ReleaseInputStageArea();
                }
            }
        }

        private int CheckUnit()
        {
            try
            {
                if (_context == null || _context.Machine == null)
                    return Fail("NEEDLE-CAL-MACHINE", "NeedleCalibrationSequence", "Machine is null.");
                if (_context.Machine.InputStageUnit == null)
                    return Fail("NEEDLE-CAL-STAGE", "NeedleCalibrationSequence", "InputStageUnit is null.");
                if (_context.Machine.VisionUnit == null || _context.Machine.VisionUnit.Config == null)
                    return Fail("NEEDLE-CAL-VISION", "NeedleCalibrationSequence", "VisionUnit calibration config is null.");

                _stage = _context.Machine.InputStageUnit;
                if (_stage.Recipe == null)
                    return Fail("NEEDLE-CAL-RECIPE", "InputStageUnit", "InputStage recipe is null.");
                if (_stage.NeedleBlockX == null || _stage.StageY == null || _stage.NeedleZ == null || _stage.EjectPinZ == null)
                    return Fail("NEEDLE-CAL-AXIS", "InputStageUnit", "Needle Calibration axis is null.");
                if (_stage.WaferStageTouchSensor == null)
                    return Fail("NEEDLE-CAL-TOUCH-SENSOR", "InputStageUnit", "WaferStageTouchSensor is null.");

                _stage.Recipe.EnsurePositionObjects();
                _context.Machine.VisionUnit.Config.EnsureCalibrationObjects();
                _context.Machine.VisionUnit.Config.CalibrationData.EnsureObjects();
                _context.Machine.VisionUnit.Config.CalibrationData.Needle.EnsureObjects();
                _settings = _context.Machine.VisionUnit.Config.CalibrationData.Needle.ZCalibration;
                _settings.EnsureDefaults();
                ApplyRecipeFallbacks(_settings, _stage);

                int axisResult = CheckAxisReady(_stage.StageY, "StageY");
                if (axisResult != 0) return axisResult;
                axisResult = CheckAxisReady(_stage.NeedleBlockX, "NeedleX");
                if (axisResult != 0) return axisResult;
                axisResult = CheckAxisReady(_stage.NeedleZ, "NeedleZ");
                if (axisResult != 0) return axisResult;
                axisResult = CheckAxisReady(_stage.EjectPinZ, "EjectPinZ");
                if (axisResult != 0) return axisResult;

                _capApproachSign = ResolveApproachSign(
                    _settings.NeedleCapTeachingPosition,
                    _stage.Recipe.EjectPinZ.AvoidPosition,
                    "NeedleCap/EjectPinZ");
                if (_capApproachSign == 0.0)
                    return Fail("NEEDLE-CAL-CAP-DIRECTION", "InputStageUnit",
                        "NeedleCapTeachingPosition과 EjectPinZ AvoidPosition이 같아 탐색 방향을 계산할 수 없습니다.");

                _pinApproachSign = ResolveApproachSign(
                    _settings.NeedlePinTeachingPosition,
                    _stage.Recipe.NeedleZ.AvoidPosition,
                    "NeedlePin/NeedleZ");
                if (_pinApproachSign == 0.0)
                    return Fail("NEEDLE-CAL-PIN-DIRECTION", "InputStageUnit",
                        "NeedlePinTeachingPosition과 NeedleZ AvoidPosition이 같아 탐색 방향을 계산할 수 없습니다.");

                Result.TouchStageYPosition = _settings.TouchStageYPosition;
                Result.TouchNeedleXPosition = _settings.TouchNeedleXPosition;
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("NEEDLE-CAL-CHECK-EX", "NeedleCalibrationSequence", "Needle Calibration 준비 확인 예외 발생. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private static void ApplyRecipeFallbacks(NeedleZCalibrationSettings settings, InputStageUnit stage)
        {
            if (settings == null || stage == null || stage.Recipe == null)
                return;

            stage.Recipe.EnsurePositionObjects();
            if (Math.Abs(settings.TouchStageYPosition) <= double.Epsilon)
                settings.TouchStageYPosition = stage.Recipe.WaferY.ProcessPosition;
            if (Math.Abs(settings.TouchNeedleXPosition) <= double.Epsilon)
                settings.TouchNeedleXPosition = stage.Recipe.NeedleX.NeedlePinCalPosition;
            if (Math.Abs(settings.NeedleCapTeachingPosition) <= double.Epsilon)
                settings.NeedleCapTeachingPosition = stage.Recipe.EjectPinZ.NeedlePinCalPosition;
            if (Math.Abs(settings.NeedlePinTeachingPosition) <= double.Epsilon)
                settings.NeedlePinTeachingPosition = stage.Recipe.NeedleZ.NeedlePinCalPosition;
        }

        private int CheckAxisReady(BaseAxis axis, string axisName)
        {
            if (axis == null)
                return Fail("NEEDLE-CAL-AXIS", "InputStageUnit", axisName + " axis is null.");

            axis.UpdateStatus();
            if (!axis.IsServoOn || axis.IsAlarm)
            {
                return Fail("NEEDLE-CAL-AXIS-NOT-READY", axisName,
                    axisName + " axis is not ready. servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                    ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                    ", actual=" + axis.ActualPosition.ToString("F6"));
            }

            return 0;
        }

        private async Task<int> AcquireInputStageAreaAsync(CancellationToken ct)
        {
            try
            {
                string holder = "NeedleCalibration:" + _runMode;
                if (_runMode != SequenceRunMode.Auto)
                {
                    _inputStageLease = await _context.Resources
                        .AcquireAsync(SequenceResourceKind.InputStageArea, holder, ResolveResourceTimeout(), ct)
                        .ConfigureAwait(false);
                    if (_inputStageLease == null)
                        return Fail("NEEDLE-CAL-RESOURCE", "NeedleCalibrationSequence", "InputStageArea 리소스 점유 실패.");
                    return 0;
                }

                bool waitLogged = false;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    _context.StopIfCycleStopRequested("NeedleCalibration.AcquireInputStageArea");
                    _inputStageLease = await _context.Resources
                        .AcquireAsync(SequenceResourceKind.InputStageArea, holder, 200, ct, false)
                        .ConfigureAwait(false);
                    if (_inputStageLease != null)
                        return 0;

                    if (!waitLogged)
                    {
                        waitLogged = true;
                        _context.LogPublic("[SEQ] NeedleCalibration InputStageArea 리소스 대기");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("NEEDLE-CAL-RESOURCE-EX", "NeedleCalibrationSequence", "InputStageArea 리소스 점유 예외 발생. error=" + ex.Message);
            }
        }

        private int ResolveResourceTimeout()
        {
            return _settings != null && _settings.Motion != null && _settings.Motion.MoveTimeoutMs > 0
                ? _settings.Motion.MoveTimeoutMs
                : CalibrationMotionSettings.DefaultMoveTimeoutMs;
        }

        private void ReleaseInputStageArea()
        {
            try
            {
                if (_inputStageLease != null)
                {
                    _inputStageLease.Dispose();
                    _inputStageLease = null;
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private async Task<int> MoveNeedleXToTouchTeachingPositionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _context.StopIfCycleStopRequested("NeedleCalibration.MoveTouchTeaching");
            // [안전이동 적용 2026-08-06] NeedleX/StageY 는 X/Y 이동이므로 전부 안전이동이다.
            // 기존 조건: _settings.Motion(측정 모션)을 그대로 넘겼다.
            // NeedleX 축 Config 를 기준으로 % 를 산출한다(두 축을 함께 움직이는 복합 이동이라
            // 대표 축 하나로 감속 배율을 정한다 — 더 느린 쪽으로 맞추는 것이 안전 방향).
            double touchVelocity = _settings.Motion.MoveVelocity;
            double touchAcceleration = _settings.Motion.MoveAcceleration;
            double touchDeceleration = _settings.Motion.MoveDeceleration;
            double touchSafePercent = CalibrationSafeMoveMotion.ResolvePercent(
                _context != null ? _context.Machine : null);
            if (CalibrationSafeMoveMotion.TryResolveAxisMotion(
                    _stage.NeedleBlockX, touchSafePercent,
                    ref touchVelocity, ref touchAcceleration, ref touchDeceleration))
            {
                CalibrationSafeMoveMotion.LogAxisSafeMove(
                    "NeedleCalibrationSequence", "NeedleX/StageY touch teaching 이동",
                    touchSafePercent, true, touchVelocity, touchAcceleration, touchDeceleration);
            }
            else
            {
                CalibrationSafeMoveMotion.LogSafeMoveMiss(
                    "NeedleCalibrationSequence", "NeedleX/StageY",
                    "touch teaching 이동", touchSafePercent, touchVelocity);
            }

            int result = await _stage.MoveNeedleWorkPointSafelyAsync(
                _settings.TouchNeedleXPosition,
                _settings.TouchStageYPosition,
                touchVelocity,
                touchAcceleration,
                touchDeceleration,
                _settings.Motion.MoveTimeoutMs,
                "NeedleCalibrationSequence.MoveNeedleXToTouchTeachingPosition").ConfigureAwait(false);
            if (result != 0)
                return Fail("NEEDLE-CAL-XY-MOVE", "InputStageUnit",
                    "NeedleX/StageY touch teaching 위치 이동 실패. result=" + result +
                    ", needleX=" + _settings.TouchNeedleXPosition.ToString("F6") +
                    ", stageY=" + _settings.TouchStageYPosition.ToString("F6"));

            result = CheckAxisInPosition(WaferStageAxis.NeedleX, _stage.NeedleBlockX, _settings.TouchNeedleXPosition, "NeedleX touch teaching");
            if (result != 0) return result;
            return CheckAxisInPosition(WaferStageAxis.WaferY, _stage.StageY, _settings.TouchStageYPosition, "StageY touch teaching");
        }

        private async Task<int> PrepareSafeStartPositionAsync(CancellationToken ct)
        {
            // 캘리브레이션 시작 전 NeedleZ/EjectPinZ를 먼저 Avoid로 복귀시켜 StageY/NeedleX 이동 인터락 조건을 만족시킨다.
            int result = await MoveNeedleAxesToAvoidAsync(ct).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!IsSimulationOrDryRun() && IsTouchSensorOn())
            {
                return Fail("NEEDLE-CAL-SAFE-TOUCH-ON", "WaferStageTouchSensor",
                    "Needle Calibration 안전 시작 위치 정렬 후에도 Touch Sensor가 ON입니다. 센서/축 접촉 상태를 확인하세요.");
            }

            EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-SAFE-START",
                "Needle Calibration 안전 시작 위치 정렬 완료. needleZ=" + _stage.NeedleZ.ActualPosition.ToString("F6") +
                ", ejectPinZ=" + _stage.EjectPinZ.ActualPosition.ToString("F6"));
            return 0;
        }

        private async Task<int> MoveNeedleAxesToAvoidAsync(CancellationToken ct)
        {
            int result = await MoveAxisForceAndVerifyAsync(
                WaferStageAxis.NeedleZ,
                _stage.NeedleZ,
                _stage.Recipe.NeedleZ.AvoidPosition,
                "NeedleZ avoid",
                false,
                ct,
                false).ConfigureAwait(false);
            if (result != 0) return result;

            return await MoveAxisForceAndVerifyAsync(
                WaferStageAxis.EjectPinZ,
                _stage.EjectPinZ,
                _stage.Recipe.EjectPinZ.AvoidPosition,
                "EjectPinZ avoid",
                false,
                ct,
                false).ConfigureAwait(false);
        }

        private async Task<int> MoveNeedleCapTeachingAndSearchAsync(
            double stepMm,
            double maxDistanceMm,
            string label,
            CancellationToken ct)
        {
            int result = await MoveAxisForceAndVerifyAsync(
                WaferStageAxis.EjectPinZ,
                _stage.EjectPinZ,
                _settings.NeedleCapTeachingPosition,
                "NeedleCap teaching position",
                false,
                ct,
                false).ConfigureAwait(false);
            if (result != 0) return result;

            NeedleSearchResult search = await SearchAxisByStepAsync(
                WaferStageAxis.EjectPinZ,
                _stage.EjectPinZ,
                _capApproachSign,
                stepMm,
                maxDistanceMm,
                label,
                ct).ConfigureAwait(false);
            if (search.Code == 0)
                _needleCapTouchPosition = search.DetectedPosition;
            return search.Code;
        }

        private async Task<int> MoveNeedlePinTeachingPositionAsync(CancellationToken ct)
        {
            return await MoveAxisForceAndVerifyAsync(
                WaferStageAxis.NeedleZ,
                _stage.NeedleZ,
                _settings.NeedlePinTeachingPosition,
                "NeedlePin teaching position",
                false,
                ct,
                false).ConfigureAwait(false);
        }

        private async Task<int> MoveNeedleCapNearTouchPositionAsync(CancellationToken ct)
        {
            double target = _needleCapTouchPosition - (_capApproachSign * Math.Abs(_settings.NeedleCapNearTouchOffsetMm));
            return await MoveAxisForceAndVerifyAsync(
                WaferStageAxis.EjectPinZ,
                _stage.EjectPinZ,
                target,
                "NeedleCap near touch position",
                true,
                ct,
                false).ConfigureAwait(false);
        }

        private async Task<NeedleSearchResult> SearchAxisByStepAsync(
            WaferStageAxis stageAxis,
            BaseAxis axis,
            double approachSign,
            double stepMm,
            double maxDistanceMm,
            string label,
            CancellationToken ct)
        {
            if (axis == null)
                return BuildSearchFail(Fail("NEEDLE-CAL-SEARCH-AXIS", "NeedleCalibrationSequence", label + " axis is null."));

            if (IsSimulationOrDryRun())
            {
                axis.UpdateStatus();
                double detectedPosition = axis.ActualPosition;
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-SIM-SEARCH",
                    label + " simulation detected position=" + detectedPosition.ToString("F6"));
                return new NeedleSearchResult { Code = 0, DetectedPosition = detectedPosition };
            }

            if (IsTouchSensorOn())
            {
                return BuildSearchFail(Fail("NEEDLE-CAL-TOUCH-ALREADY-ON", "WaferStageTouchSensor",
                    label + " 시작 전 Touch Sensor가 이미 ON입니다. backoff/센서 상태를 확인하세요."));
            }

            double safeStep = Math.Abs(stepMm);
            double safeMax = Math.Abs(maxDistanceMm);
            if (safeStep <= 0.0 || safeMax <= 0.0)
                return BuildSearchFail(Fail("NEEDLE-CAL-SEARCH-PARAM", "NeedleCalibrationSequence",
                    label + " search parameter invalid. step=" + stepMm.ToString("F6") +
                    ", max=" + maxDistanceMm.ToString("F6")));

            int iterationLimit = (int)Math.Ceiling(safeMax / safeStep) + 2;
            if (iterationLimit > MaxSearchIterations)
                return BuildSearchFail(Fail("NEEDLE-CAL-SEARCH-ITERATION", "NeedleCalibrationSequence",
                    label + " 반복 횟수가 너무 큽니다. step=" + safeStep.ToString("F6") +
                    ", max=" + safeMax.ToString("F6") +
                    ", iteration=" + iterationLimit));

            axis.UpdateStatus();
            double start = axis.ActualPosition;
            double moved = 0.0;
            for (int i = 0; i < iterationLimit && moved < safeMax - 0.0000001; i++)
            {
                ct.ThrowIfCancellationRequested();
                _context.StopIfCycleStopRequested("NeedleCalibration." + label);
                if (AlarmManager.HasActive)
                    return BuildSearchFail(Fail("NEEDLE-CAL-ACTIVE-ALARM", "NeedleCalibrationSequence", label + " 중 알람이 발생했습니다."));

                moved = Math.Min(safeMax, moved + safeStep);
                double target = start + (approachSign * moved);
                // [측정 스트로크] Touch Sensor 접촉을 찾는 스텝 접근 — 여기만 측정 모션(_settings.Motion)을 쓴다.
                int result = await MoveAxisForceAndVerifyAsync(stageAxis, axis, target, label, true, ct, true).ConfigureAwait(false);
                if (result != 0)
                    return BuildSearchFail(result);

                if (await IsTouchSensorOnStableAsync(ct).ConfigureAwait(false))
                {
                    axis.UpdateStatus();
                    double detectedPosition = axis.ActualPosition;
                    EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-TOUCH-DETECTED",
                        label + " touch detected. axis=" + axis.Name +
                        ", detected=" + detectedPosition.ToString("F6") +
                        ", moved=" + moved.ToString("F6"));
                    return new NeedleSearchResult { Code = 0, DetectedPosition = detectedPosition };
                }
            }

            return BuildSearchFail(Fail("NEEDLE-CAL-TOUCH-NOT-DETECTED", "WaferStageTouchSensor",
                label + " Touch Sensor 미감지. maxDistance=" + safeMax.ToString("F6") +
                ", step=" + safeStep.ToString("F6") +
                ", axis=" + axis.Name +
                ", start=" + start.ToString("F6") +
                ", actual=" + axis.ActualPosition.ToString("F6")));
        }

        private static NeedleSearchResult BuildSearchFail(int code)
        {
            return new NeedleSearchResult
            {
                Code = code,
                DetectedPosition = 0.0
            };
        }

        private async Task<int> BackOffAxisAsync(
            WaferStageAxis stageAxis,
            BaseAxis axis,
            double approachSign,
            double distanceMm,
            string label,
            CancellationToken ct)
        {
            if (axis == null)
                return Fail("NEEDLE-CAL-BACKOFF-AXIS", "NeedleCalibrationSequence", label + " axis is null.");

            axis.UpdateStatus();
            double target = axis.ActualPosition - (approachSign * Math.Abs(distanceMm));
            // 탐색 후 후퇴(BackOff) — 측정이 아니므로 안전이동.
            int result = await MoveAxisForceAndVerifyAsync(stageAxis, axis, target, label, true, ct, false).ConfigureAwait(false);
            if (result != 0)
                return result;

            if (!IsSimulationOrDryRun() && IsTouchSensorOn())
            {
                return Fail("NEEDLE-CAL-BACKOFF-TOUCH-ON", "WaferStageTouchSensor",
                    label + " 후 Touch Sensor가 OFF되지 않았습니다. actual=" + axis.ActualPosition.ToString("F6") +
                    ", target=" + target.ToString("F6"));
            }

            return 0;
        }

        // ============================================================================
        // [안전이동 적용 2026-08-06]  ★실장비 미검증 — 실장비에서 테스트 필요★
        //
        // 사용자 확정 규칙(2026-08-06):
        //   캘 이동은 전부 안전이동(축 Default × SafeMovePercent)이고,
        //   예외는 "실제 측정 스트로크" 하나뿐이다.
        //
        // NEEDLE Z CAL 의 모든 축 이동이 이 메서드 하나를 통과하는데,
        // 기존에는 전부 _settings.Motion.MoveVelocity(측정 모션)로 나갔다.
        // SafeMovePercent 참조가 이 파일에 0건이었다 — 화면의 % 가 전혀 적용되지 않았다.
        //
        // 측정 스트로크는 SearchAxisByStepAsync 의 스텝 접근(1um/10um)뿐이므로
        // 그 호출부만 measurementMotion=true 이고 나머지(Avoid/티칭위치/근접/백오프)는 안전이동이다.
        //
        // measurementMotion 에 기본값을 주지 않아, 새 호출부가 생기면 역할을 명시하도록 강제한다.
        // ============================================================================
        private async Task<int> MoveAxisForceAndVerifyAsync(
            WaferStageAxis stageAxis,
            BaseAxis axis,
            double target,
            string label,
            bool forceMove,
            CancellationToken ct,
            bool measurementMotion)
        {
            if (axis == null)
                return Fail("NEEDLE-CAL-MOVE-AXIS", "NeedleCalibrationSequence", label + " axis is null.");

            ct.ThrowIfCancellationRequested();
            _context.StopIfCycleStopRequested("NeedleCalibration.MoveAxis:" + label);
            string reason;
            if (!MotionGuardRuntime.VerifyAxisMove(axis, target, out reason))
            {
                return Fail("NEEDLE-CAL-MOVE-INTERLOCK", axis.Name,
                    label + " 이동 인터락 차단. target=" + target.ToString("F6") + ". " + reason);
            }

            double moveVelocity = _settings.Motion.MoveVelocity;
            double moveAcceleration = _settings.Motion.MoveAcceleration;
            double moveDeceleration = _settings.Motion.MoveDeceleration;
            if (!measurementMotion)
            {
                double safePercent = CalibrationSafeMoveMotion.ResolvePercent(
                    _context != null ? _context.Machine : null);
                bool safeApplied = CalibrationSafeMoveMotion.TryResolveAxisMotion(
                    axis, safePercent, ref moveVelocity, ref moveAcceleration, ref moveDeceleration);
                if (safeApplied)
                {
                    CalibrationSafeMoveMotion.LogAxisSafeMove(
                        "NeedleCalibrationSequence", label, safePercent, true,
                        moveVelocity, moveAcceleration, moveDeceleration);
                }
                else
                {
                    CalibrationSafeMoveMotion.LogSafeMoveMiss(
                        "NeedleCalibrationSequence", axis.Name, label, safePercent, moveVelocity);
                }
            }

            int result = await SharedRailXMotionRuntime.MoveAxisAsync(
                axis,
                target,
                moveVelocity,
                moveAcceleration,
                moveDeceleration,
                forceMove).ConfigureAwait(false);
            if (result != 0 || axis.IsAlarm)
            {
                return Fail("NEEDLE-CAL-MOVE", axis.Name,
                    label + " 이동 실패. axis=" + axis.Name +
                    ", result=" + result +
                    ", alarm=" + axis.IsAlarm +
                    ", alarmCode=" + axis.AlarmCode +
                    ", target=" + target.ToString("F6") +
                    ", actual=" + axis.ActualPosition.ToString("F6"));
            }

            // 기존 조건: 이동 후 재대기(+실측 수용 분기) — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
            // 캘리브레이션 최종 위치 확인은 실측 정확도 게이트로 유지(R4 애매 지점).
            return CheckAxisInPosition(stageAxis, axis, target, label);
        }

        private int CheckAxisInPosition(WaferStageAxis stageAxis, BaseAxis axis, double target, string label)
        {
            if (IsAxisAtTarget(axis, target))
                return 0;

            return Fail("NEEDLE-CAL-FINAL-CHECK", axis != null ? axis.Name : stageAxis.ToString(),
                label + " 최종 위치 확인 실패. target=" + target.ToString("F6") +
                ", actual=" + (axis != null ? axis.ActualPosition.ToString("F6") : "null"));
        }

        private static bool IsAxisAtTarget(BaseAxis axis, double target)
        {
            if (axis == null)
                return false;

            axis.UpdateStatus();
            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
            return axis.IsServoOn &&
                   !axis.IsAlarm &&
                   !axis.IsMoving &&
                   axis.IsInPosition &&
                   Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private int SaveNeedleCapTouchPosition()
        {
            Result.NeedleCapTouchPosition = _needleCapTouchPosition;
            EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-CAP-TOUCH",
                "NeedleCapTouchPosition 저장 준비. position=" + _needleCapTouchPosition.ToString("F6"));
            return 0;
        }

        private int SaveNeedlePinFlushPosition()
        {
            Result.NeedlePinFlushPosition = _needlePinFlushPosition;
            EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-PIN-FLUSH",
                "NeedlePinFlushPosition 저장 준비. position=" + _needlePinFlushPosition.ToString("F6"));
            return 0;
        }

        private int CalculateAndSaveNeedlePinReadyPosition()
        {
            try
            {
                // Ready 위치는 Flush 위치에서 NeedlePin 접촉 탐색 방향으로 더 내려간 위치를 사용한다.
                _needlePinReadyPosition = _needlePinFlushPosition + (_pinApproachSign * Math.Abs(_settings.NeedlePinReadyBelowFlushMm));
                Result.NeedlePinReadyPosition = _needlePinReadyPosition;

                CalibrationData data = _context.Machine.VisionUnit.Config.CalibrationData;
                data.EnsureObjects();
                data.Needle.NeedleCapTouchPosition = _needleCapTouchPosition;
                data.Needle.NeedlePinFlushPosition = _needlePinFlushPosition;
                data.Needle.NeedlePinReadyPosition = _needlePinReadyPosition;
                data.Needle.NeedleZCalibrationValid = true;
                data.Needle.NeedleZCalibrationUpdatedAt = DateTime.Now;
                data.Needle.NeedleZCalibrationUpdatedBy = ResolveUpdatedBy();
                data.Needle.NeedleZCalibrationMessage = "OK";
                data.Touch("NeedleCalibration");

                string saveReason;
                if (!CalibrationDataStore.Save(data, out saveReason))
                    return Fail("NEEDLE-CAL-DATA-SAVE", "NeedleCalibrationSequence", "Needle CalibrationData 저장 실패. reason=" + saveReason);

                _context.Machine.SaveSettings();
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-CAL-SAVE",
                    "Needle Calibration 저장. capTouch=" + _needleCapTouchPosition.ToString("F6") +
                    ", pinFlush=" + _needlePinFlushPosition.ToString("F6") +
                    ", pinReady=" + _needlePinReadyPosition.ToString("F6") +
                    ", readyBelowFlush=" + _settings.NeedlePinReadyBelowFlushMm.ToString("F6"));
                return 0;
            }
            catch (Exception ex)
            {
                return Fail("NEEDLE-CAL-SAVE-EX", "NeedleCalibrationSequence", "Needle Calibration 결과 저장 예외 발생. error=" + ex.Message);
            }
        }

        private bool IsTouchSensorOn()
        {
            BaseDigitalInput sensor = _stage != null ? _stage.WaferStageTouchSensor : null;
            if (sensor == null)
                return false;

            sensor.UpdateStatus();
            return sensor.IsOn;
        }

        private async Task<bool> IsTouchSensorOnStableAsync(CancellationToken ct)
        {
            if (!IsTouchSensorOn())
                return false;

            int stableMs = _settings.TouchStableMs;
            if (stableMs <= 0)
                return true;

            DateTime start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalMilliseconds < stableMs)
            {
                ct.ThrowIfCancellationRequested();
                if (!IsTouchSensorOn())
                    return false;

                await Task.Delay(Math.Max(1, _settings.TouchPollIntervalMs), ct).ConfigureAwait(false);
            }

            return IsTouchSensorOn();
        }

        private bool IsSimulationOrDryRun()
        {
            try
            {
                return _stage == null || _stage.IsInputStageSimulationOrDryRun();
            }
            catch
            {
                return false;
            }
        }

        private static double ResolveApproachSign(double teachingPosition, double avoidPosition, string label)
        {
            double delta = teachingPosition - avoidPosition;
            if (Math.Abs(delta) <= 0.0000001)
                return 0.0;

            return Math.Sign(delta);
        }

        private void StopNeedleAxes()
        {
            try
            {
                if (_stage != null && _stage.NeedleZ != null)
                    _stage.NeedleZ.StopJog();
            }
            catch
            {
            }

            try
            {
                if (_stage != null && _stage.EjectPinZ != null)
                    _stage.EjectPinZ.StopJog();
            }
            catch
            {
            }
        }

        private string ResolveUpdatedBy()
        {
            try
            {
                return Environment.UserName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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
