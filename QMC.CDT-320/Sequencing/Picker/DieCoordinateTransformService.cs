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
            double pickerAlignOffsetT,
            double visionOffsetX,
            double visionOffsetY,
            double visionOffsetT,
            double needleXToVisionXOffset,
            double needleYToVisionYOffset,
            double pickerYTeaching,
            double pickerTTeaching,
            double pickerZTeaching,
            double needleZTeaching,
            double ejectPinZTeaching)
        {
            PickCoordinateResult result = new PickCoordinateResult();
            result.StageY = inputStageY + inputVisionToPickerY + visionOffsetY + needleYToVisionYOffset;
            result.PickerX = inputVisionX + inputVisionToPickerX + pickerAlignOffsetX + visionOffsetX;
            result.PickerY = pickerYTeaching;
            result.PickerT = pickerTTeaching + pickerAlignOffsetT + visionOffsetT;
            result.PickerZ = pickerZTeaching;
            result.NeedleX = inputVisionX + visionOffsetX - needleXToVisionXOffset;
            result.NeedleZ = needleZTeaching;
            result.EjectPinZ = ejectPinZTeaching;
            result.Formula =
                "stageY = inputStageY(" + F(inputStageY) + ") + inputVisionToPickerY(" + F(inputVisionToPickerY) + ") + visionOffsetY(" + F(visionOffsetY) + ") + needleYToVisionYOffset(" + F(needleYToVisionYOffset) + ") = " + F(result.StageY) +
                " / pickerX = inputVisionX(" + F(inputVisionX) + ") + inputVisionToPickerX(" + F(inputVisionToPickerX) + ") + pickerAlignOffsetX(" + F(pickerAlignOffsetX) + ") + visionOffsetX(" + F(visionOffsetX) + ") = " + F(result.PickerX) +
                " / pickerT = pickerTTeaching(" + F(pickerTTeaching) + ") + pickerAlignOffsetT(" + F(pickerAlignOffsetT) + ") + visionOffsetT(" + F(visionOffsetT) + ") = " + F(result.PickerT) +
                " / needleX = inputVisionX(" + F(inputVisionX) + ") + visionOffsetX(" + F(visionOffsetX) + ") - needleXToVisionXOffset(" + F(needleXToVisionXOffset) + ") = " + F(result.NeedleX) +
                " / pickerY = " + F(result.PickerY) +
                " / pickerZ = " + F(result.PickerZ) +
                " / needleZ = " + F(result.NeedleZ) +
                " / ejectPinZ = " + F(result.EjectPinZ);
            LogFormula(sequenceName, "PICK", side, pickerIndex, dieId, result.Formula);
            return result;
        }

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
            double pickerYTeaching,
            double pickerTTeaching,
            double pickerAlignOffsetT,
            double pickerZTeaching)
        {
            PlaceCoordinateResult result = new PlaceCoordinateResult();
            result.TargetSide = targetSide;
            result.OutputStageY = outputStageBaseY + receiveTargetY + outputVisionToPickerY;
            result.PickerX = outputVisionProcessX + receiveTargetX + outputVisionToPickerX + pickerAlignOffsetX;
            result.PickerY = pickerYTeaching;
            result.PickerT = pickerTTeaching + pickerAlignOffsetT;
            result.PickerZ = pickerZTeaching;
            result.Formula =
                "targetSide = " + targetSide +
                " / outputStageY = outputStageBaseY(" + F(outputStageBaseY) + ") + receiveTargetY(" + F(receiveTargetY) + ") + outputVisionToPickerY(" + F(outputVisionToPickerY) + ") = " + F(result.OutputStageY) +
                " / pickerX = outputVisionProcessX(" + F(outputVisionProcessX) + ") + receiveTargetX(" + F(receiveTargetX) + ") + outputVisionToPickerX(" + F(outputVisionToPickerX) + ") + pickerAlignOffsetX(" + F(pickerAlignOffsetX) + ") = " + F(result.PickerX) +
                " / pickerT = pickerTTeaching(" + F(pickerTTeaching) + ") + pickerAlignOffsetT(" + F(pickerAlignOffsetT) + ") = " + F(result.PickerT) +
                " / pickerY = " + F(result.PickerY) +
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
