using System;
using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Sequencing
{
    internal static class PickerCoordinateTransformHelper
    {
        public static bool TryResolveInputVisionToPickerOffsets(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            out double offsetX,
            out double offsetY,
            out string reason)
        {
            return TryResolveVisionToPickerOffsets(machine, side, pickerIndex, true, BinSide.Good, out offsetX, out offsetY, out reason);
        }

        public static bool TryResolveOutputVisionToPickerOffsets(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            out double offsetX,
            out double offsetY,
            out string reason)
        {
            return TryResolveVisionToPickerOffsets(machine, side, pickerIndex, false, BinSide.Good, out offsetX, out offsetY, out reason);
        }

        public static bool TryResolveOutputVisionToPickerOffsets(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            BinSide outputSide,
            out double offsetX,
            out double offsetY,
            out string reason)
        {
            return TryResolveVisionToPickerOffsets(machine, side, pickerIndex, false, outputSide, out offsetX, out offsetY, out reason);
        }

        private static bool TryResolveVisionToPickerOffsets(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            bool inputVision,
            BinSide outputSide,
            out double offsetX,
            out double offsetY,
            out string reason)
        {
            offsetX = 0.0;
            offsetY = 0.0;
            reason = string.Empty;

            try
            {
                if (machine == null)
                {
                    reason = "machine is null.";
                    return false;
                }

                if (pickerIndex < 0 || pickerIndex >= 4)
                {
                    reason = "picker index is out of range. pickerIndex=" + pickerIndex;
                    return false;
                }

                if (side == PickerSequenceSide.Front)
                    return TryResolveFrontOffsets(machine, pickerIndex, inputVision, outputSide, out offsetX, out offsetY, out reason);

                return TryResolveRearOffsets(machine, pickerIndex, inputVision, outputSide, out offsetX, out offsetY, out reason);
            }
            catch (Exception ex)
            {
                reason = "offset resolve exception. side=" + side +
                    ", pickerIndex=" + pickerIndex +
                    ", inputVision=" + inputVision +
                    ", error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        private static bool TryResolveFrontOffsets(
            CDT320_Machine machine,
            int pickerIndex,
            bool inputVision,
            BinSide outputSide,
            out double offsetX,
            out double offsetY,
            out string reason)
        {
            offsetX = 0.0;
            offsetY = 0.0;
            reason = string.Empty;

            PickerFrontUnit front = machine.PickerFrontUnit;
            if (front == null || front.Setup == null)
            {
                reason = "FrontPicker setup is null.";
                return false;
            }

            front.Setup.EnsureGeometryData();
            PickerVisionCoordinateOffsets offsets = inputVision
                ? front.Setup.InputVisionToPicker
                : front.Setup.OutputVisionToPicker;
            if (offsets == null)
            {
                reason = inputVision ? "Front InputVisionToPicker offset is null." : "Front OutputVisionToPicker offset is null.";
                return false;
            }

            offsetX = offsets.GetOffsetX(pickerIndex, front.Setup.PickerPitchX);
            offsetY = offsets.GetOffsetY(pickerIndex, front.Setup.PickerPitchY);
            if (!inputVision)
            {
                offsetX += ResolveOutputHomeGap(PickerSequenceSide.Front);
            }
            return true;
        }

        private static bool TryResolveRearOffsets(
            CDT320_Machine machine,
            int pickerIndex,
            bool inputVision,
            BinSide outputSide,
            out double offsetX,
            out double offsetY,
            out string reason)
        {
            offsetX = 0.0;
            offsetY = 0.0;
            reason = string.Empty;

            PickerRearUnit rear = machine.PickerRearUnit;
            if (rear == null || rear.Setup == null)
            {
                reason = "RearPicker setup is null.";
                return false;
            }

            rear.Setup.EnsureGeometryData();
            PickerVisionCoordinateOffsets offsets = inputVision
                ? rear.Setup.InputVisionToPicker
                : rear.Setup.OutputVisionToPicker;
            if (offsets == null)
            {
                reason = inputVision ? "Rear InputVisionToPicker offset is null." : "Rear OutputVisionToPicker offset is null.";
                return false;
            }

            offsetX = offsets.GetOffsetX(pickerIndex, rear.Setup.PickerPitchX);
            offsetY = offsets.GetOffsetY(pickerIndex, rear.Setup.PickerPitchY);
            if (!inputVision)
            {
                offsetX += ResolveOutputHomeGap(PickerSequenceSide.Rear);
            }
            return true;
        }

        private static double ResolveOutputHomeGap(PickerSequenceSide side)
        {
            try
            {
                SharedRailXConfig config = SharedRailXConfigStore.LoadOrCreateDefault();
                if (config == null)
                    return 0.0;

                SharedRailXAxis pickerAxis = side == PickerSequenceSide.Front
                    ? SharedRailXAxis.FrontPickerX
                    : SharedRailXAxis.RearPickerX;

                SharedRailXAxisPair pair;
                if (config.TryGetCollisionPair(SharedRailXAxis.OutputVisionX, pickerAxis, out pair))
                    return pair.HomeClearance;
            }
            catch
            {
            }
            finally
            {
            }

            return 0.0;
        }
    }
}
