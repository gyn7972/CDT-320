using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.CDT320.Interlocks;
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
        private static readonly object SimVisionRandomLock = new object();
        private static readonly Random SimVisionRandom = new Random();

        private readonly MachineSequenceContext _context;
        private readonly bool _fineMove;

        public NeedlePinCalibrationSequence(MachineSequenceContext context, bool fineMove)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fineMove = fineMove;
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
            return 0;
        }

        private async Task<int> MoveSharedRailAxesToAvoidAsync(CancellationToken ct)
        {
            CDT320_Machine machine = _context.Machine;
            int timeout = ResolveTimeout();

            ct.ThrowIfCancellationRequested();
            int result = await machine.OutputStageUnit.MoveVisionXToAvoidAndVerifyAsync(timeout, _fineMove, ct).ConfigureAwait(false);
            if (result != 0)
                return Fail("OutputCameraX avoid move failed. result=" + result);

            ct.ThrowIfCancellationRequested();
            result = await machine.PickerFrontUnit.MoveToFrontPickerAvoidPosition(_fineMove).ConfigureAwait(false);
            if (result != 0)
                return Fail("FrontPickerX avoid move failed. result=" + result);

            ct.ThrowIfCancellationRequested();
            result = await machine.PickerRearUnit.MoveToRearPickerAvoidPosition(_fineMove).ConfigureAwait(false);
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
            result = await stage.MoveNeedleWorkPointSafelyAsync(
                stage.Recipe.NeedleX.NeedlePinCalPosition,
                stage.Recipe.WaferY.ProcessPosition,
                _fineMove,
                "NeedlePinCalibrationSequence.MoveNeedlePinCalPositionAsync").ConfigureAwait(false);
            if (result != 0) return result;

            result = await MoveStageAxisAsync(stage, WaferStageAxis.NeedleZ, stage.Recipe.NeedleZ.NeedlePinCalPosition, ct).ConfigureAwait(false);
            if (result != 0) return result;

            return await MoveStageAxisAsync(stage, WaferStageAxis.EjectPinZ, stage.Recipe.EjectPinZ.NeedlePinCalPosition, ct).ConfigureAwait(false);
        }

        private async Task<int> MoveStageAxisAsync(InputStageUnit stage, WaferStageAxis axis, double target, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            int result = await stage.MoveInputStageAxis(axis, target, _fineMove).ConfigureAwait(false);
            if (result != 0)
                return Fail(axis + " move failed. target=" + target.ToString("F6") + ", result=" + result);

            AxisMoveWaitResult waitResult = await stage.WaitInputStageAxisInPositionResult(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
            if (waitResult == null || !waitResult.Success)
            {
                if (IsStageAxisAtTarget(stage, axis, target))
                {
                    EventLogger.Write(EventKind.Event, "CAL", "NEEDLE-PIN-CAL-WAIT-ACCEPT",
                        axis + " wait returned non-success but actual position is already acceptable. target=" +
                        target.ToString("F6") + ", " +
                        AxisMoveWaiter.FormatResult(waitResult, axis.ToString()));
                    return 0;
                }

                return Fail(axis + " in-position wait failed. target=" + target.ToString("F6") +
                    ", " + AxisMoveWaiter.FormatResult(waitResult, axis.ToString()));
            }

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
            if (IsSimulationOrVisionBypass(stage))
                return CreateSimulatedVisionResult(stage);

            string targetId = stage.Setup.NeedlePinCalVisionTargetId;
            if (string.IsNullOrWhiteSpace(targetId))
                targetId = "NeedlePinCal";

            ct.ThrowIfCancellationRequested();
            return await stage.Vision.TriggerAlignAsync(targetId).ConfigureAwait(false);
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
                targetId = "NeedlePinCal";

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

        private int ResolveTimeout()
        {
            InputStageUnit stage = _context.Machine != null ? _context.Machine.InputStageUnit : null;
            if (stage != null && stage.Setup != null && stage.Setup.NeedlePinCalVisionTimeoutMs > 0)
                return stage.Setup.NeedlePinCalVisionTimeoutMs;
            return 5000;
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
