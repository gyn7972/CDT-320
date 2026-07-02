using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Calibration;
using QMC.Common.Logging;

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

        public async Task<int> MoveTeachingPositionOnlyAsync(CancellationToken ct)
        {
            try
            {
                int result = await MoveReadyPositionOnlyAsync(ct).ConfigureAwait(false);
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

        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                int result = await MoveTeachingPositionOnlyAsync(ct).ConfigureAwait(false);
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

            result = await MoveStageAxisAsync(stage, WaferStageAxis.NeedleX, stage.Recipe.NeedleX.NeedlePinCalPosition, ct).ConfigureAwait(false);
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

            result = await stage.WaitInputStageAxisInPosition(axis, target, ResolveTimeout(), ct).ConfigureAwait(false);
            if (result != 0)
                return Fail(axis + " in-position wait failed. target=" + target.ToString("F6") + ", result=" + result);

            return 0;
        }

        private async Task<VisionAlignResult> RequestVisionAsync(CancellationToken ct)
        {
            InputStageUnit stage = _context.Machine.InputStageUnit;
            if (stage.IsInputStageSimulationOrDryRun() || stage.Vision == null)
                return new VisionAlignResult();

            string targetId = stage.Setup.NeedlePinCalVisionTargetId;
            if (string.IsNullOrWhiteSpace(targetId))
                targetId = "NeedlePinCal";

            ct.ThrowIfCancellationRequested();
            return await stage.Vision.TriggerAlignAsync(targetId).ConfigureAwait(false);
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
