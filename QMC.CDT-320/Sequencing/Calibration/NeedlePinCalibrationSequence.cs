using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
using QMC.CDT320.VisionComm;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing.Calibration
{
    public sealed class NeedlePinCalibrationResult
    {
        public bool Success { get; set; }
        public double VisionXPosition { get; set; }
        public double StageYPosition { get; set; }
        public double NeedleXPosition { get; set; }
        public double NeedleZPosition { get; set; }
        public double EjectPinZPosition { get; set; }
        public double VisionOffsetX { get; set; }
        public double VisionOffsetY { get; set; }
        public double NeedleXToVisionXOffset { get; set; }
        public double NeedleYToVisionYOffset { get; set; }
        public string Message { get; set; }
    }

    public sealed class NeedlePinCalibrationSequence
    {
        private const double SimVisionOffsetRangeMm = 0.03;
        private const double VisionAlignPitchMm = 0.15;
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();

        private readonly MachineSequenceContext _context;
        public NeedlePinCalibrationSequence(MachineSequenceContext context)
            : this(context, false)
        {
        }

        public NeedlePinCalibrationSequence(MachineSequenceContext context, bool fineMove)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Result = new NeedlePinCalibrationResult();
        }

        public NeedlePinCalibrationResult Result { get; private set; }

        public async Task<int> MoveReadyPositionOnlyAsync(CancellationToken ct)
        {
            return await MoveReadyPositionOnlyAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> MoveReadyPositionOnlyAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(runMode == SequenceRunMode.Auto, "NeedlePinCalibrationSequence.MoveReadyPositionOnlyAsync:" + runMode))
            {
            try
            {
                int result = CheckUnit();
                if (result != 0) return result;

                result = await MoveSharedRailAxesToAvoidAsync(ct).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveInputStageToProcessAsync(ct).ConfigureAwait(false);
                if (result != 0) return result;

                Result.Success = true;
                Result.Message = "Needle Pin Cal ready position move complete.";
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("Needle Pin Cal position move exception: " + ex.Message);
            }
            finally
            {
            }
            }
        }

        public async Task<int> MoveTeachingPositionOnlyAsync(CancellationToken ct)
        {
            return await MoveTeachingPositionOnlyAsync(ct, SequenceRunMode.Manual).ConfigureAwait(false);
        }

        public async Task<int> MoveTeachingPositionOnlyAsync(CancellationToken ct, SequenceRunMode runMode)
        {
            using (MotionGuardRuntime.BeginSequenceProcessMove(runMode == SequenceRunMode.Auto, "NeedlePinCalibrationSequence.MoveTeachingPositionOnlyAsync:" + runMode))
            {
            try
            {
                int result = await MoveReadyPositionOnlyAsync(ct, runMode).ConfigureAwait(false);
                if (result != 0) return result;

                result = await MoveNeedlePinCalPositionAsync(ct).ConfigureAwait(false);
                if (result != 0) return result;

                Result.Success = true;
                Result.Message = "Needle Pin Cal teaching position move complete.";
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("Needle Pin Cal teaching position move exception: " + ex.Message);
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
            using (MotionGuardRuntime.BeginSequenceProcessMove(runMode == SequenceRunMode.Auto, "NeedlePinCalibrationSequence.RunAsync:" + runMode))
            {
            try
            {
                int result = await MoveTeachingPositionOnlyAsync(ct, runMode).ConfigureAwait(false);
                if (result != 0) return result;

                VisionAlignResult vision = await RequestVisionAsync(ct).ConfigureAwait(false);
                if (vision == null)
                    return Fail("Needle Pin Cal vision result is null.");

                SaveCalibration(vision);
                Result.Success = true;
                Result.Message = "Needle Pin Calibration complete.";
                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-PIN-CAL",
                    "Needle Pin Calibration complete. needleXToVisionX=" +
                    Result.NeedleXToVisionXOffset.ToString("F6") +
                    ", needleYToVisionY=" + Result.NeedleYToVisionYOffset.ToString("F6") +
                    ", visionOffsetX=" + Result.VisionOffsetX.ToString("F6") +
                    ", visionOffsetY=" + Result.VisionOffsetY.ToString("F6"));
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail("Needle Pin Calibration exception: " + ex.Message);
            }
            finally
            {
            }
            }
        }

        private int CheckUnit()
        {
            CDT320_Machine machine = _context.Machine;
            if (machine == null)
                return Fail("Machine is null.");
            if (machine.InputStageUnit == null)
                return Fail("InputStageUnit is null.");
            if (machine.InputStageUnit.Recipe == null)
                return Fail("InputStage recipe is null.");
            if (machine.InputStageUnit.Setup == null)
                return Fail("InputStage setup is null.");
            if (machine.VisionUnit == null || machine.VisionUnit.Config == null)
                return Fail("VisionUnit calibration config is null.");
            if (machine.PickerFrontUnit == null)
                return Fail("FrontPickerUnit is null.");
            if (machine.PickerRearUnit == null)
                return Fail("RearPickerUnit is null.");
            if (machine.OutputStageUnit == null)
                return Fail("OutputStageUnit is null.");

            machine.InputStageUnit.Recipe.EnsurePositionObjects();
            ResolveMotionSettings();
            return 0;
        }

        private CalibrationMotionSettings ResolveMotionSettings()
        {
            CDT320_Machine machine = _context != null ? _context.Machine : null;
            if (machine != null && machine.VisionUnit != null && machine.VisionUnit.Config != null)
            {
                machine.VisionUnit.Config.EnsureCalibrationObjects();
                machine.VisionUnit.Config.CalibrationData.EnsureObjects();
                machine.VisionUnit.Config.CalibrationData.Needle.EnsureObjects();
                return machine.VisionUnit.Config.CalibrationData.Needle.Motion.Clone();
            }

            var motion = new CalibrationMotionSettings();
            motion.EnsureDefaults();
            return motion;
        }

        private async Task<int> MoveSharedRailAxesToAvoidAsync(CancellationToken ct)
        {
            CDT320_Machine machine = _context.Machine;
            CalibrationMotionSettings motion = ResolveMotionSettings();
            int timeout = motion.MoveTimeoutMs;

            ct.ThrowIfCancellationRequested();
            int result = await machine.OutputStageUnit.MoveVisionXToAvoidAndVerifyAsync(
                timeout,
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration,
                ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OutputCameraX avoid move failed. result=" + result);

            ct.ThrowIfCancellationRequested();
            result = await machine.PickerFrontUnit.MoveToFrontPickerAvoidPosition(JogSpeedType.Custom, motion.MoveVelocity).ConfigureAwait(false);
            if (result != 0)
                return Fail("FrontPickerX avoid move failed. result=" + result);

            ct.ThrowIfCancellationRequested();
            result = await machine.PickerRearUnit.MoveToRearPickerAvoidPosition(JogSpeedType.Custom, motion.MoveVelocity).ConfigureAwait(false);
            if (result != 0)
                return Fail("RearPickerX avoid move failed. result=" + result);

            return 0;
        }

        private async Task<int> MoveInputStageToProcessAsync(CancellationToken ct)
        {
            InputStageUnit stage = _context.Machine.InputStageUnit;

            int result = await MoveStageAxisAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.AvoidPosition, ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.AvoidPosition, ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAsync(stage, WaferStageAxis.WaferY, stage.Recipe.WaferY.ProcessPosition, ct).ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAsync(stage, WaferStageAxis.WaferT, stage.Recipe.WaferT.ProcessPosition, ct).ConfigureAwait(false);
            if (result != 0) return result;

            return await MoveStageAxisAsync(stage, WaferStageAxis.WaferExpandingZ, stage.Recipe.WaferZ.ProcessPosition, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveNeedlePinCalPositionAsync(CancellationToken ct)
        {
            InputStageUnit stage = _context.Machine.InputStageUnit;

            int result = await MoveStageAxisAsync(stage, WaferStageAxis.VisionX, stage.Recipe.VisionX.NeedlePinCalPosition, ct).ConfigureAwait(false);
            if (result != 0) return result;

            ct.ThrowIfCancellationRequested();
            CalibrationMotionSettings motion = ResolveMotionSettings();
            result = await stage.MoveNeedleWorkPointSafelyAsync(
                stage.Recipe.NeedleX.NeedlePinCalPosition,
                stage.Recipe.WaferY.ProcessPosition,
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration,
                motion.MoveTimeoutMs,
                "NeedlePinCalibrationSequence.MoveNeedlePinCalPositionAsync").ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.NeedlePinCalPosition, ct).ConfigureAwait(false);
            if (result != 0) return result;

            return await MoveStageAxisAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.NeedlePinCalPosition, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveStageAxisAsync(InputStageUnit stage, WaferStageAxis axis, double target, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            CalibrationMotionSettings motion = ResolveMotionSettings();
            int result = await stage.MoveInputStageAxisCommandWithMotion(
                axis,
                target,
                motion.MoveVelocity,
                motion.MoveAcceleration,
                motion.MoveDeceleration).ConfigureAwait(false);
            if (result != 0)
                return Fail(axis + " move failed. target=" + target.ToString("F6") + ", result=" + result);

            // 기존 조건: 이동 후 재대기(+실측 수용 분기) — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
            return 0;
        }

        private static bool IsStageAxisAtTarget(InputStageUnit stage, WaferStageAxis axis, double target)
        {
            BaseAxis item = ResolveStageAxis(stage, axis);
            if (item == null)
                return false;

            double tolerance = item.Config != null && item.Config.InPositionTolerance > 0.0
                ? item.Config.InPositionTolerance
                : 0.05;

            return item.IsServoOn &&
                   !item.IsAlarm &&
                   !item.IsMoving &&
                   item.IsInPosition &&
                   Math.Abs(item.ActualPosition - target) <= tolerance;
        }

        private static BaseAxis ResolveStageAxis(InputStageUnit stage, WaferStageAxis axis)
        {
            if (stage == null)
                return null;

            switch (axis)
            {
                case WaferStageAxis.WaferY:
                    return stage.StageY;
                case WaferStageAxis.WaferT:
                    return stage.StageT;
                case WaferStageAxis.WaferExpandingZ:
                    return stage.ExpanderZ;
                case WaferStageAxis.VisionX:
                    return stage.CameraX;
                case WaferStageAxis.NeedleX:
                    return stage.NeedleBlockX;
                case WaferStageAxis.NeedleZ:
                    return stage.NeedleZ;
                case WaferStageAxis.EjectPinZ:
                    return stage.EjectPinZ;
                default:
                    return null;
            }
        }

        private async Task<VisionAlignResult> RequestVisionAsync(CancellationToken ct)
        {
            InputStageUnit stage = _context.Machine.InputStageUnit;
            string targetId = stage != null && stage.Setup != null ? stage.Setup.NeedlePinCalVisionTargetId : null;
            if (string.IsNullOrWhiteSpace(targetId))
                targetId = VisionToolIds.Wafer.EjectPinFinder;

            int timeoutMs = stage != null && stage.Setup != null && stage.Setup.NeedlePinCalVisionTimeoutMs > 0
                ? stage.Setup.NeedlePinCalVisionTimeoutMs
                : 5000;
            string finder = ResolveAlignFinder(targetId);

            if (IsDryRunWithWaferVisionConnected(stage))
            {
                await AutoVisionRequestService.GrabAsync(
                    AutoVisionChannel.Wafer,
                    0,
                    timeoutMs,
                    ct).ConfigureAwait(false);

                return CreateSimulatedVisionResult(stage);
            }

            if (IsSimulationOrVisionBypass(stage))
                return CreateSimulatedVisionResult(stage);

            ct.ThrowIfCancellationRequested();
            MatchResultDto match = await AutoVisionRequestService.MatchAsync(
                AutoVisionChannel.Wafer,
                finder,
                0,
                timeoutMs,
                ct).ConfigureAwait(false);

            VisionAlignResult align = BuildNeedlePinVisionAlignResult(match);
            LogVisionMatch(targetId, finder, timeoutMs, match, align);

            if (align != null)
                QMC.CDT_320.Equipment.Vision.WaferVisionResultStore.RecordAlign(targetId, align);

            return align;
        }

        private VisionAlignResult BuildNeedlePinVisionAlignResult(MatchResultDto match)
        {
            if (match == null || !match.Success)
                return null;

            VisionCameraPixelCalibration camera = ResolveNeedlePinCameraCalibration();
            if (match.HasImageSize)
                camera.ApplyImageSize(match.ImageWidthPixel, match.ImageHeightPixel);

            return new VisionAlignResult
            {
                DeltaX = camera.PixelToMmOffsetX(match.X),
                DeltaY = camera.PixelToMmOffsetY(match.Y),
                DeltaTheta = match.AngleDeg,
                PitchX = VisionAlignPitchMm,
                PitchY = VisionAlignPitchMm
            };
        }

        private VisionCameraPixelCalibration ResolveNeedlePinCameraCalibration()
        {
            CalibrationData calibration = _context.Machine.VisionUnit.Config.CalibrationData;
            if (calibration == null)
                calibration = new CalibrationData();
            calibration.EnsureObjects();

            VisionCameraCalibrationData data = calibration.Camera;
            if (data == null)
                data = new VisionCameraCalibrationData();
            data.EnsureObjects();

            return VisionCameraCalibrationTransform.ResolveCamera(data, AutoVisionChannel.Wafer);
        }

        private static string ResolveAlignFinder(string alignTargetId)
        {
            return VisionAlignTargetIds.ResolveWaferFinder(alignTargetId);
        }

        private void LogVisionMatch(
            string targetId,
            string finder,
            int timeoutMs,
            MatchResultDto match,
            VisionAlignResult align)
        {
            try
            {
                VisionCameraPixelCalibration camera = ResolveNeedlePinCameraCalibration();
                double pixelDeltaX = match != null && match.Success ? match.X - camera.ImageCenterPixelX : 0.0;
                double pixelDeltaY = match != null && match.Success ? camera.ImageCenterPixelY - match.Y : 0.0;
                double pixelMmX = match != null && match.Success ? camera.PixelToMmOffsetX(match.X) : 0.0;
                double pixelMmY = match != null && match.Success ? camera.PixelToMmOffsetY(match.Y) : 0.0;

                EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-PIN-CAL-VISION-RAW",
                    "Needle Pin Cal vision raw. target=" + targetId +
                    ", finder=" + finder +
                    ", timeoutMs=" + timeoutMs +
                    ", success=" + (match != null && match.Success) +
                    ", pixelX=" + (match != null ? match.X.ToString("F6") : "null") +
                    ", pixelY=" + (match != null ? match.Y.ToString("F6") : "null") +
                    ", angleDeg=" + (match != null ? match.AngleDeg.ToString("F6") : "null") +
                    ", score=" + (match != null ? match.Score.ToString("F6") : "null") +
                    ", hasImageSize=" + (match != null && match.HasImageSize) +
                    ", imageW=" + (match != null ? match.ImageWidthPixel.ToString("F3") : "null") +
                    ", imageH=" + (match != null ? match.ImageHeightPixel.ToString("F3") : "null") +
                    ", centerX=" + camera.ImageCenterPixelX.ToString("F3") +
                    ", centerY=" + camera.ImageCenterPixelY.ToString("F3") +
                    ", pixelDeltaX=pixelX-centerX=" + pixelDeltaX.ToString("F6") +
                    ", pixelDeltaY=centerY-pixelY=" + pixelDeltaY.ToString("F6") +
                    ", pixelToMmX=" + camera.PixelToMmX.ToString("F9") +
                    ", pixelToMmY=" + camera.PixelToMmY.ToString("F9") +
                    ", pixelMmX=(pixelX-centerX)*pixelToMmX=" + pixelMmX.ToString("F6") +
                    ", pixelMmY=(centerY-pixelY)*pixelToMmY=" + pixelMmY.ToString("F6") +
                    ", inputBottomRef=not_applied" +
                    ", finalDeltaX=" + (align != null ? align.DeltaX.ToString("F6") : "null") +
                    ", finalDeltaY=" + (align != null ? align.DeltaY.ToString("F6") : "null") +
                    ", raw=" + (match != null ? (match.RawError ?? string.Empty) : "null"));
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-PIN-CAL-VISION-LOG",
                    "Needle Pin Cal vision raw log failed. error=" + ex.Message);
            }
        }

        private bool IsSimulationOrVisionBypass(InputStageUnit stage)
        {
            AppSettings settings = AppSettingsStore.Current;
            if (settings != null &&
                (settings.SimulationMode ||
                 settings.DryRunMode ||
                 settings.BypassHardware ||
                 !settings.UseVision))
                return true;

            if (stage == null)
                return true;

            return stage.IsInputStageSimulationOrDryRun() || stage.Vision == null;
        }

        private VisionAlignResult CreateSimulatedVisionResult(InputStageUnit stage)
        {
            if (IsDryRunWithVisionDisabled())
                return CreateZeroVisionResult();

            double offsetX;
            double offsetY;
            lock (SimVisionRandomLock)
            {
                offsetX = NextSignedOffset(SimVisionRandom, SimVisionOffsetRangeMm);
                offsetY = NextSignedOffset(SimVisionRandom, SimVisionOffsetRangeMm);
            }

            string targetId = stage != null && stage.Setup != null ? stage.Setup.NeedlePinCalVisionTargetId : null;
            if (string.IsNullOrWhiteSpace(targetId))
                targetId = VisionToolIds.Wafer.EjectPinFinder;

            EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-PIN-CAL-SIM-VISION",
                "Needle Pin Cal simulated vision offset generated. target=" + targetId +
                ", offsetX=" + offsetX.ToString("F6") +
                ", offsetY=" + offsetY.ToString("F6") +
                ", range=+/-" + SimVisionOffsetRangeMm.ToString("F6") + "mm");

            return new VisionAlignResult
            {
                DeltaX = offsetX,
                DeltaY = offsetY,
                DeltaTheta = 0.0,
                PitchX = 0.0,
                PitchY = 0.0
            };
        }

        private static VisionAlignResult CreateZeroVisionResult()
        {
            return new VisionAlignResult
            {
                DeltaX = 0.0,
                DeltaY = 0.0,
                DeltaTheta = 0.0,
                PitchX = 0.0,
                PitchY = 0.0
            };
        }

        private static bool IsDryRunWithVisionDisabled()
        {
            AppSettings settings = AppSettingsStore.Current;
            return settings != null && settings.DryRunMode && !settings.UseVision;
        }

        private static bool IsDryRunWithWaferVisionConnected(InputStageUnit stage)
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current;
                if (settings == null || !settings.DryRunMode || !settings.UseVision)
                    return false;

                if (stage == null || stage.Vision == null)
                    return false;

                return VisionCommandService.IsConnected(AutoVisionChannel.Wafer);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static double NextSignedOffset(Random random, double range)
        {
            if (random == null || range <= 0.0)
                return 0.0;

            return (random.NextDouble() * 2.0 - 1.0) * range;
        }

        private void SaveCalibration(VisionAlignResult vision)
        {
            InputStageUnit stage = _context.Machine.InputStageUnit;
            double visionX = stage.CameraX != null ? stage.CameraX.ActualPosition : stage.Recipe.VisionX.NeedlePinCalPosition;
            double stageY = stage.StageY != null ? stage.StageY.ActualPosition : stage.Recipe.WaferY.ProcessPosition;
            double needleX = stage.NeedleBlockX != null ? stage.NeedleBlockX.ActualPosition : stage.Recipe.NeedleX.NeedlePinCalPosition;
            double needleZ = stage.NeedleZ != null ? stage.NeedleZ.ActualPosition : stage.Recipe.NeedleZ.NeedlePinCalPosition;
            double ejectPinZ = stage.EjectPinZ != null ? stage.EjectPinZ.ActualPosition : stage.Recipe.EjectPinZ.NeedlePinCalPosition;

            double offsetX = vision != null ? vision.DeltaX : 0.0;
            double offsetY = vision != null ? vision.DeltaY : 0.0;
            double needleXToVisionX = visionX + offsetX - needleX;
            double needleYToVisionY = offsetY;

            LogCalibrationCalculation(
                visionX,
                stageY,
                needleX,
                needleZ,
                ejectPinZ,
                offsetX,
                offsetY,
                needleXToVisionX,
                needleYToVisionY);

            CalibrationData calibration = _context.Machine.VisionUnit.Config.CalibrationData;
            calibration.EnsureObjects();
            calibration.Needle.VisionXPosition = visionX;
            calibration.Needle.StageYPosition = stageY;
            calibration.Needle.NeedleXPosition = needleX;
            calibration.Needle.NeedleZPosition = needleZ;
            calibration.Needle.EjectPinZPosition = ejectPinZ;
            calibration.Needle.VisionOffsetX = offsetX;
            calibration.Needle.VisionOffsetY = offsetY;
            calibration.Needle.NeedleXToVisionXOffset = needleXToVisionX;
            calibration.Needle.NeedleYToVisionYOffset = needleYToVisionY;
            calibration.Needle.Valid = true;
            calibration.Needle.UpdatedAt = DateTime.Now;
            calibration.Needle.UpdatedBy = "NeedlePinCalibration";
            calibration.Touch("NeedlePinCalibration");

            Result.VisionXPosition = visionX;
            Result.StageYPosition = stageY;
            Result.NeedleXPosition = needleX;
            Result.NeedleZPosition = needleZ;
            Result.EjectPinZPosition = ejectPinZ;
            Result.VisionOffsetX = offsetX;
            Result.VisionOffsetY = offsetY;
            Result.NeedleXToVisionXOffset = needleXToVisionX;
            Result.NeedleYToVisionYOffset = needleYToVisionY;
            _context.Machine.SaveSettings();
        }

        private static void LogCalibrationCalculation(
            double visionX,
            double stageY,
            double needleX,
            double needleZ,
            double ejectPinZ,
            double visionOffsetX,
            double visionOffsetY,
            double needleXToVisionX,
            double needleYToVisionY)
        {
            EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-PIN-CAL-CALC",
                "Needle Pin Cal calculation. " +
                "visionX=" + visionX.ToString("F6") +
                ", stageY=" + stageY.ToString("F6") +
                ", needleX=" + needleX.ToString("F6") +
                ", needleZ=" + needleZ.ToString("F6") +
                ", ejectPinZ=" + ejectPinZ.ToString("F6") +
                ", visionOffsetX=" + visionOffsetX.ToString("F6") +
                ", visionOffsetY=" + visionOffsetY.ToString("F6") +
                ", needleXToVisionX=visionX+visionOffsetX-needleX=" + needleXToVisionX.ToString("F6") +
                ", needleYToVisionY=visionOffsetY=" + needleYToVisionY.ToString("F6"));
        }

        private int Fail(string message)
        {
            Result.Success = false;
            Result.Message = message;
            EventLogger.Write(EventKind.Alarm, "CAL", "NEEDLE-PIN-CAL", message);
            return -1;
        }
    }
}
