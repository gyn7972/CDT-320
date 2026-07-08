using System;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerZoneCoordinateResult
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double T { get; set; }
        public string Formula { get; set; }
    }

    internal sealed class PickCoordinateResult
    {
        public double StageY { get; set; }
        public double PickerX { get; set; }
        public double PickerY { get; set; }
        public double PickerT { get; set; }
        public double PickerZ { get; set; }
        public double NeedleX { get; set; }
        public double NeedleZ { get; set; }
        public double EjectPinZ { get; set; }
        public string Formula { get; set; }
    }

    internal sealed class PlaceCoordinateResult
    {
        public BinSide TargetSide { get; set; }
        public double OutputStageY { get; set; }
        public double PickerX { get; set; }
        public double PickerY { get; set; }
        public double PickerT { get; set; }
        public double PickerZ { get; set; }
        public string Formula { get; set; }
    }

    internal static class DieCoordinateTransformService
    {
        // Calculates taught picker zone coordinates with the selected X/Y/T correction values.
        public static PickerZoneCoordinateResult CalculatePickerZoneTarget(
            string sequenceName,
            PickerSequenceSide side,
            string zoneName,
            int pickerIndex,
            double teachingX,
            double teachingY,
            double teachingT,
            double pitchOffsetX,
            double alignOffsetX,
            double alignOffsetY,
            double alignOffsetT)
        {
            PickerZoneCoordinateResult result = new PickerZoneCoordinateResult();
            result.X = teachingX + pitchOffsetX + alignOffsetX;
            result.Y = teachingY + alignOffsetY;
            result.T = teachingT + alignOffsetT;
            result.Formula =
                "zoneX = teachingX(" + F(teachingX) + ") + pitchOffsetX(" + F(pitchOffsetX) + ") + alignOffsetX(" + F(alignOffsetX) + ") = " + F(result.X) +
                " / zoneY = teachingY(" + F(teachingY) + ") + alignOffsetY(" + F(alignOffsetY) + ") = " + F(result.Y) +
                " / zoneT = teachingT(" + F(teachingT) + ") + alignOffsetT(" + F(alignOffsetT) + ") = " + F(result.T);
            LogFormula(sequenceName, "PICKER-ZONE", side, pickerIndex, zoneName, result.Formula);
            return result;
        }

        // Converts an InputVision-centered die position into InputStage, Needle, and Picker pickup targets.
        // pickerAlignOffsetT is runtimeT; saved collet theta is not added because it is handled by picker T home zero.
        public static PickCoordinateResult CalculatePickTarget(
            string sequenceName,
            PickerSequenceSide side,
            int pickerIndex,
            string dieId,
            double inputVisionX,
            double inputStageY,
            double inputVisionToPickerX,
            double inputVisionToPickerY,
            double pickerAlignOffsetX,
            double pickerAlignOffsetY,
            double pickerAlignOffsetT,
            double cameraOffsetX,
            double cameraOffsetY,
            double alignOffsetX,
            double alignOffsetY,
            double alignOffsetT,
            double needleXToVisionXOffset,
            double needleYToVisionYOffset,
            double pickerYTeaching,
            double pickerTTeaching,
            double pickerZTeaching,
            double needleZTeaching,
            double ejectPinZTeaching,
            bool logFormula = true)
        {
            PickCoordinateResult result = new PickCoordinateResult();
            // StageY는 선택 Die Y와 Needle Y 캘리브레이션만 적용해 Needle 중심 기준을 유지한다.
            result.StageY = inputStageY + needleYToVisionYOffset;
            result.PickerX = inputVisionX + inputVisionToPickerX + pickerAlignOffsetX + alignOffsetX;
            result.PickerT = pickerTTeaching + pickerAlignOffsetT + alignOffsetT;
            result.PickerZ = pickerZTeaching;
            result.NeedleX = inputVisionX + alignOffsetX - needleXToVisionXOffset;
            double pickerYOffset = alignOffsetY + pickerAlignOffsetY;
            result.PickerY = inputVisionToPickerY + needleYToVisionYOffset + pickerYOffset;
            result.NeedleZ = needleZTeaching;
            result.EjectPinZ = ejectPinZTeaching;
            result.Formula =
                "stageY = inputStageY(" + F(inputStageY) + ") + needleYToVisionYOffset(" + F(needleYToVisionYOffset) + ") = " + F(result.StageY) +
                " [cameraOffset=(" + F(cameraOffsetX) + "," + F(cameraOffsetY) + ") already included in InputVisionToPicker offset]" +
                " / pickerX = inputVisionX(" + F(inputVisionX) + ") + inputVisionToPickerX(" + F(inputVisionToPickerX) + ") + pickerAlignOffsetX(" + F(pickerAlignOffsetX) + ") + alignOffsetX(" + F(alignOffsetX) + ") = " + F(result.PickerX) +
                " / pickerT = pickerTTeaching(" + F(pickerTTeaching) + ") + pickerAlignOffsetT(" + F(pickerAlignOffsetT) + ") + alignOffsetT(" + F(alignOffsetT) + ") = " + F(result.PickerT) +
                " / needleX = inputVisionX(" + F(inputVisionX) + ") + alignOffsetX(" + F(alignOffsetX) + ") - needleXToVisionXOffset(" + F(needleXToVisionXOffset) + ") = " + F(result.NeedleX) +
                " / pickerY = inputVisionToPickerY(" + F(inputVisionToPickerY) + ") + needleYToVisionYOffset(" + F(needleYToVisionYOffset) + ") + pickerYOffset(alignOffsetY(" + F(alignOffsetY) + ") + pickerAlignOffsetY(" + F(pickerAlignOffsetY) + "))(" + F(pickerYOffset) + ") = " + F(result.PickerY) +
                " / pickerZ = " + F(result.PickerZ) +
                " / needleZ = " + F(result.NeedleZ) +
                " / ejectPinZ = " + F(result.EjectPinZ);
            if (logFormula)
                LogFormula(sequenceName, "PICK", side, pickerIndex, dieId, result.Formula);
            return result;
        }

        private static double ResolveInputPickerYTarget(PickerSequenceSide side, double inputVisionToPickerY)
        {
            double magnitude = System.Math.Abs(inputVisionToPickerY);
            return side == PickerSequenceSide.Rear ? -magnitude : magnitude;
        }

        private static double ResolveSignedPickerYOffset(PickerSequenceSide side, double offsetY)
        {
            return side == PickerSequenceSide.Rear ? -offsetY : offsetY;
        }

        // Converts an output slot position into OutputStage and Picker place targets using the carried picker correction.
        public static PlaceCoordinateResult CalculatePlaceTarget(
            string sequenceName,
            PickerSequenceSide side,
            int pickerIndex,
            string dieId,
            BinSide targetSide,
            double outputStageBaseY,
            double receiveTargetY,
            double outputVisionProcessX,
            double receiveTargetX,
            double outputVisionToPickerX,
            double outputVisionToPickerY,
            double pickerAlignOffsetX,
            double pickerAlignOffsetY,
            double pickerYTeaching,
            double pickerTTeaching,
            double pickerAlignOffsetT,
            double pickerZTeaching)
        {
            PlaceCoordinateResult result = new PlaceCoordinateResult();
            result.PickerY = pickerYTeaching + pickerAlignOffsetY;
            double pickerYForward = Math.Abs(result.PickerY);
            result.TargetSide = targetSide;
            // 현재 기준: Picker별 Y 보정은 최종 PickerY 전진량으로 OutputStageY 보상에 반영한다.
            result.OutputStageY = outputStageBaseY + receiveTargetY + outputVisionToPickerY - pickerYForward;
            result.PickerX = outputVisionProcessX + receiveTargetX + outputVisionToPickerX + pickerAlignOffsetX;
            result.PickerT = pickerTTeaching;
            result.PickerZ = pickerZTeaching;
            result.Formula =
                "targetSide = " + targetSide +
                " / outputStageY = outputStageBaseY(" + F(outputStageBaseY) + ") + receiveTargetY(" + F(receiveTargetY) + ") + outputVisionToPickerY(" + F(outputVisionToPickerY) + ") - pickerYForward(abs(pickerY))(" + F(pickerYForward) + ") = " + F(result.OutputStageY) +
                " / pickerX = outputVisionProcessX(" + F(outputVisionProcessX) + ") + receiveTargetX(" + F(receiveTargetX) + ") + outputVisionToPickerX(" + F(outputVisionToPickerX) + ") + pickerAlignOffsetX(" + F(pickerAlignOffsetX) + ") = " + F(result.PickerX) +
                " / pickerT = placeTeachingT(" + F(pickerTTeaching) + ") [pickerAlignOffsetT ignored for place=" + F(pickerAlignOffsetT) + "] = " + F(result.PickerT) +
                " / pickerY = pickerYTeaching(" + F(pickerYTeaching) + ") + pickerAlignOffsetY(" + F(pickerAlignOffsetY) + ") = " + F(result.PickerY) +
                " / pickerZ = " + F(result.PickerZ);
            LogFormula(sequenceName, "PLACE", side, pickerIndex, dieId, result.Formula);
            return result;
        }

        private static void LogFormula(
            string sequenceName,
            string phase,
            PickerSequenceSide side,
            int pickerIndex,
            string targetId,
            string formula)
        {
            AppSettings settings = AppSettingsStore.Current;
            if (settings != null && (settings.SimulationMode || settings.DryRunMode))
                return;

            EventLogger.Write(
                EventKind.Event,
                "COORD",
                "DIE-COORD-CALC",
                (sequenceName ?? "UnknownSequence") +
                " coordinate calculation. phase=" + phase +
                ", side=" + side +
                ", pickerIndex=" + pickerIndex +
                ", target=" + (targetId ?? string.Empty) +
                ", formula: " + formula);
        }

        private static string F(double value)
        {
            return value.ToString("F6");
        }
    }
}
