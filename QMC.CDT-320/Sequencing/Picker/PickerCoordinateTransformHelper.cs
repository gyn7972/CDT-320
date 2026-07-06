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
            // 현재 기준: HomeClearance는 홈 기준 카메라/픽커 하드웨어 거리이므로 sign 기준으로 X 좌표 변환에 반영한다.
            offsetX += ResolveVisionToPickerHomeBridge(inputVision, PickerSequenceSide.Front);
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
            // 현재 기준: HomeClearance는 홈 기준 카메라/픽커 하드웨어 거리이므로 sign 기준으로 X 좌표 변환에 반영한다.
            offsetX += ResolveVisionToPickerHomeBridge(inputVision, PickerSequenceSide.Rear);
            return true;
        }

        private static double ResolveVisionToPickerHomeBridge(bool inputVision, PickerSequenceSide side)
        {
            try
            {
                SharedRailXConfig config = SharedRailXConfigStore.LoadOrCreateDefault();
                if (config == null)
                    return 0.0;

                SharedRailXAxis visionAxis = inputVision
                    ? SharedRailXAxis.InputVisionX
                    : SharedRailXAxis.OutputVisionX;
                SharedRailXAxis pickerAxis = side == PickerSequenceSide.Front
                    ? SharedRailXAxis.FrontPickerX
                    : SharedRailXAxis.RearPickerX;

                SharedRailXAxisPair pair;
                if (!config.TryGetCollisionPair(visionAxis, pickerAxis, out pair))
                    return 0.0;

                int visionSign;
                int pickerSign;
                ResolvePairSigns(visionAxis, pickerAxis, pair, out visionSign, out pickerSign);
                if (pickerSign == 0)
                    return 0.0;
                if (-visionSign != pickerSign)
                    return 0.0;

                // 현재 기준: clearance=0일 때 pickerX=(homeClearance-visionSign*visionX)/pickerSign 이다.
                return pair.HomeClearance / pickerSign;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
            }
        }

        private static void ResolvePairSigns(
            SharedRailXAxis axisA,
            SharedRailXAxis axisB,
            SharedRailXAxisPair pair,
            out int signA,
            out int signB)
        {
            if (pair.AxisA == axisA && pair.AxisB == axisB)
            {
                signA = pair.AxisATowardSign;
                signB = pair.AxisBTowardSign;
                return;
            }

            signA = pair.AxisBTowardSign;
            signB = pair.AxisATowardSign;
        }

    }
}
