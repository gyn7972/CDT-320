using QMC.CDT320.Calibration;

namespace QMC.CDT320.Sequencing
{
    internal static class InputPickerPickTargetResolver
    {
        public static bool TryResolveInputVisionToPickerOffsets(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            out double offsetX,
            out double offsetY,
            out string reason)
        {
            return PickerCoordinateTransformHelper.TryResolveInputVisionToPickerOffsets(
                machine,
                side,
                pickerIndex,
                out offsetX,
                out offsetY,
                out reason);
        }

        public static PickCoordinateResult CalculateManualInputMapTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            string dieId,
            double inputVisionX,
            double inputStageY,
            double inputVisionToPickerX,
            double inputVisionToPickerY,
            bool logFormula = false)
        {
            double cameraOffsetX;
            double cameraOffsetY;
            TryResolveInputCameraToBottomOffsets(machine, out cameraOffsetX, out cameraOffsetY);

            return DieCoordinateTransformService.CalculatePickTarget(
                "InputPickerPickTargetResolver.ManualInputMap",
                side,
                pickerIndex,
                string.IsNullOrWhiteSpace(dieId) ? "" : dieId,
                inputVisionX,
                inputStageY,
                inputVisionToPickerX,
                inputVisionToPickerY,
                ResolvePickerAlignOffsetX(machine, side, pickerIndex),
                ResolvePickerAlignOffsetY(machine, side, pickerIndex),
                ResolvePickerAlignOffsetT(machine, side, pickerIndex),
                cameraOffsetX,
                cameraOffsetY,
                0.0,
                0.0,
                0.0,
                ResolveNeedleCalibrationOffsetX(machine),
                ResolveNeedleCalibrationOffsetY(machine),
                ResolvePickerYPickTeaching(machine, side),
                ResolvePickerTeachingPosition(machine, side, CalibrationCoordinateService.ResolvePickerTAxis(pickerIndex), "PickPosition"),
                ResolvePickerTeachingPosition(machine, side, CalibrationCoordinateService.ResolvePickerZAxis(pickerIndex), "PickPosition"),
                ResolveNeedleZPickTarget(machine),
                ResolveEjectPinZPickTarget(machine),
                logFormula);
        }

        public static bool TryResolveInputCameraToBottomOffsets(
            CDT320_Machine machine,
            out double offsetX,
            out double offsetY)
        {
            offsetX = 0.0;
            offsetY = 0.0;

            try
            {
                VisionCameraCalibrationData camera = CalibrationCoordinateService.ResolveCamera(machine);
                if (camera == null)
                    return false;

                camera.EnsureObjects();
                if (!camera.Valid)
                    return false;

                offsetX = camera.InputToBottomOffsetX;
                offsetY = camera.InputToBottomOffsetY;
                return true;
            }
            catch
            {
                offsetX = 0.0;
                offsetY = 0.0;
                return false;
            }
            finally
            {
            }
        }

        public static double ResolvePickerAlignOffsetX(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            PickerAlignOffset runtime = ResolveRuntimePickerOffset(machine, side, pickerIndex);
            PickerCalibrationOffset calibration = ResolvePickerCalibrationOffset(machine, side, pickerIndex);
            return (runtime != null ? runtime.AlignOffsetX : 0.0) + (calibration != null ? calibration.X : 0.0);
        }

        public static double ResolvePickerAlignOffsetY(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            PickerAlignOffset runtime = ResolveRuntimePickerOffset(machine, side, pickerIndex);
            PickerCalibrationOffset calibration = ResolvePickerCalibrationOffset(machine, side, pickerIndex);
            return (runtime != null ? runtime.AlignOffsetY : 0.0) + (calibration != null ? calibration.Y : 0.0);
        }

        public static double ResolvePickerAlignOffsetT(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            PickerAlignOffset runtime = ResolveRuntimePickerOffset(machine, side, pickerIndex);
            PickerCalibrationOffset calibration = ResolvePickerCalibrationOffset(machine, side, pickerIndex);
            return (runtime != null ? runtime.AlignOffsetT : 0.0) + (calibration != null ? calibration.T : 0.0);
        }

        public static PickerAlignOffset ResolveRuntimePickerOffset(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            if (machine == null || pickerIndex < 0)
                return null;

            if (side == PickerSequenceSide.Front && machine.PickerFrontUnit != null)
                return machine.PickerFrontUnit.GetRuntimePickerOffset(pickerIndex);

            if (side == PickerSequenceSide.Rear && machine.PickerRearUnit != null)
                return machine.PickerRearUnit.GetRuntimePickerOffset(pickerIndex);

            return null;
        }

        public static double ResolvePickerYPickTeaching(CDT320_Machine machine, PickerSequenceSide side)
        {
            return ResolvePickerTeachingPosition(machine, side, PickerAxis.PickerY, "PickPosition");
        }

        public static double ResolvePickerTeachingPosition(
            CDT320_Machine machine,
            PickerSequenceSide side,
            PickerAxis axis,
            string positionName)
        {
            try
            {
                if (machine == null)
                    return 0.0;

                if (side == PickerSequenceSide.Front && machine.PickerFrontUnit != null)
                    return machine.PickerFrontUnit.GetPickerTeachingPosition(axis, positionName);

                if (side == PickerSequenceSide.Rear && machine.PickerRearUnit != null)
                    return machine.PickerRearUnit.GetPickerTeachingPosition(axis, positionName);
            }
            catch
            {
            }
            finally
            {
            }

            return 0.0;
        }

        public static double ResolveNeedleCalibrationOffsetX(CDT320_Machine machine)
        {
            NeedleCalibrationData needle = CalibrationCoordinateService.ResolveNeedle(machine);
            return needle != null ? needle.NeedleXToVisionXOffset : 0.0;
        }

        public static double ResolveNeedleCalibrationOffsetY(CDT320_Machine machine)
        {
            NeedleCalibrationData needle = CalibrationCoordinateService.ResolveNeedle(machine);
            return needle != null ? needle.NeedleYToVisionYOffset : 0.0;
        }

        public static double ResolveNeedleZPickTarget(CDT320_Machine machine)
        {
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            try
            {
                CalibrationData data = machine.VisionUnit != null &&
                                       machine.VisionUnit.Config != null
                    ? machine.VisionUnit.Config.CalibrationData
                    : null;
                if (data != null)
                {
                    data.EnsureObjects();
                    if (data.Needle != null && data.Needle.NeedleZCalibrationValid)
                        return data.Needle.NeedlePinReadyPosition;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return stage.Recipe.NeedleZ.ProcessPosition;
        }

        public static double ResolveEjectPinZPickTarget(CDT320_Machine machine)
        {
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            if (stage == null || stage.Recipe == null)
                return 0.0;

            stage.Recipe.EnsurePositionObjects();
            double offset = stage.Config != null ? stage.Config.PickUpEjectPinOffset : 0.0;
            return stage.Recipe.EjectPinZ.ProcessPosition + offset;
        }

        private static PickerCalibrationOffset ResolvePickerCalibrationOffset(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            return CalibrationCoordinateService.ResolvePickerCalibrationOffset(
                machine,
                ToVisionFocusPickerSide(side),
                pickerIndex);
        }

        private static VisionFocusPickerSide ToVisionFocusPickerSide(PickerSequenceSide side)
        {
            return side == PickerSequenceSide.Front
                ? VisionFocusPickerSide.Front
                : VisionFocusPickerSide.Rear;
        }
    }
}
