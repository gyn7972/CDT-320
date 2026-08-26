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

        // Manual right-click input map moves use the same pick target formula as the automatic pickup sequence.
        public static PickCoordinateResult CalculateManualInputMapTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            string dieId,
            double inputVisionX,
            double inputStageY,
            double inputVisionToPickerX,
            double inputVisionToPickerY,
            bool includePickerRuntimeAlignOffset = true,
            bool logFormula = false)
        {
            return PickerMotionTargetResolver.CalculateInputPickTarget(
                machine,
                side,
                pickerIndex,
                "InputPickerPickTargetResolver.ManualInputMap",
                string.IsNullOrWhiteSpace(dieId) ? "" : dieId,
                inputVisionX,
                inputStageY,
                inputVisionToPickerX,
                inputVisionToPickerY,
                0.0,
                0.0,
                0.0,
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
            // InputVisionToPicker X/Y는 카메라/콜렛 캘을 포함한 최종 변환값이므로 Collet X는 여기서 다시 더하지 않는다.
            return runtime != null ? runtime.AlignOffsetX : 0.0;
        }

        public static double ResolvePickerAlignOffsetY(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            PickerAlignOffset runtime = ResolveRuntimePickerOffset(machine, side, pickerIndex);
            // InputVisionToPicker X/Y는 카메라/콜렛 캘을 포함한 최종 변환값이므로 Collet Y는 여기서 다시 더하지 않는다.
            return runtime != null ? runtime.AlignOffsetY : 0.0;
        }

        public static double ResolvePickerAlignOffsetT(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            PickerAlignOffset runtime = ResolveRuntimePickerOffset(machine, side, pickerIndex);
            // runtimeT는 PickerAlignOffset.AlignOffsetT이고, ColletT는 홈 기준 보정이라 이동식에는 넣지 않는다.
            return runtime != null ? runtime.AlignOffsetT : 0.0;
        }

        public static double ResolveColletTOffset(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
        {
            PickerCalibrationOffset collet = ResolvePickerCalibrationOffset(machine, side, pickerIndex);
            return collet != null ? collet.T : 0.0;
        }

        // Runtime offset is the temporary alignment value currently held by the selected picker unit.
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
            return stage.Recipe.EjectPinZ.ProcessPosition;
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
