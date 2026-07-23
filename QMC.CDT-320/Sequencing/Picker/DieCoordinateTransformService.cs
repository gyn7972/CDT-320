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
            bool logFormula = true,
            double pickRuntimeOffsetX = 0.0,
            double pickRuntimeOffsetY = 0.0,
            double pickRuntimeOffsetT = 0.0)
        {
            PickCoordinateResult result = new PickCoordinateResult();
            // Input Vision Y 보정은 PickerY가 아니라 StageY를 반대 방향으로 이동해 Die를 고정 Pick Y에 맞춘다.
            // Pick 런타임 보정(pickRuntimeOffset*)은 Bottom 검사 LowPassFilter 출력(raw)이며 전 채널 감산으로 상쇄한다.
            // X는 피커·니들 정렬 유지를 위해 PickerX와 NeedleX 양쪽에 동일하게 적용한다.
            result.StageY = inputStageY + needleYToVisionYOffset - alignOffsetY - pickRuntimeOffsetY;
            result.PickerX = inputVisionX + inputVisionToPickerX + pickerAlignOffsetX + alignOffsetX - pickRuntimeOffsetX;
            // Collet T offset은 Picker T 홈 기준 보정에 이미 반영되므로 Pick 이동식에는 다시 더하지 않는다.
            // result.PickerT = pickerTTeaching + pickerAlignOffsetT + colletTOffset + alignOffsetT;
            result.PickerT = pickerTTeaching + pickerAlignOffsetT + alignOffsetT - pickRuntimeOffsetT;
            result.PickerZ = pickerZTeaching;
            result.NeedleX = inputVisionX + alignOffsetX - needleXToVisionXOffset - pickRuntimeOffsetX;
            // PickerY는 Die별 Vision Y와 무관하게 Picker별 고정 Pick 위치를 유지한다.
            double pickerYOffset = pickerAlignOffsetY;
            result.PickerY = inputVisionToPickerY + needleYToVisionYOffset + pickerYOffset;
            result.NeedleZ = needleZTeaching;
            result.EjectPinZ = ejectPinZTeaching;
            result.Formula =
                "stageY = inputStageY(" + F(inputStageY) + ") + needleYToVisionYOffset(" + F(needleYToVisionYOffset) + ") - alignOffsetY(" + F(alignOffsetY) + ") - pickRuntimeOffsetY(" + F(pickRuntimeOffsetY) + ") = " + F(result.StageY) +
                " [cameraOffset=(" + F(cameraOffsetX) + "," + F(cameraOffsetY) + ") already included in InputVisionToPicker offset]" +
                " / pickerX = inputVisionX(" + F(inputVisionX) + ") + inputVisionToPickerX(" + F(inputVisionToPickerX) + ") + pickerAlignOffsetX(" + F(pickerAlignOffsetX) + ") + alignOffsetX(" + F(alignOffsetX) + ") - pickRuntimeOffsetX(" + F(pickRuntimeOffsetX) + ") = " + F(result.PickerX) +
                " / pickerT = pickerTTeaching(" + F(pickerTTeaching) + ") + pickerAlignOffsetT(" + F(pickerAlignOffsetT) + ") + alignOffsetT(" + F(alignOffsetT) + ") - pickRuntimeOffsetT(" + F(pickRuntimeOffsetT) + ") = " + F(result.PickerT) +
                " / needleX = inputVisionX(" + F(inputVisionX) + ") + alignOffsetX(" + F(alignOffsetX) + ") - needleXToVisionXOffset(" + F(needleXToVisionXOffset) + ") - pickRuntimeOffsetX(" + F(pickRuntimeOffsetX) + ") = " + F(result.NeedleX) +
                " / pickerY = inputVisionToPickerY(" + F(inputVisionToPickerY) + ") + needleYToVisionYOffset(" + F(needleYToVisionYOffset) + ") + pickerAlignOffsetY(" + F(pickerAlignOffsetY) + ") = " + F(result.PickerY) +
                " / pickerZ = " + F(result.PickerZ) +
                " / needleZ = " + F(result.NeedleZ) +
                " / ejectPinZ = " + F(result.EjectPinZ);
            if (logFormula)
                LogFormula(sequenceName, "PICK", side, pickerIndex, dieId, result.Formula);
            return result;
        }

        // PickUp 기구 보정은 Needle/Stage의 1:1 좌표 관계를 유지하기 위해
        // X는 PickerX와 NeedleX에 동일 적용하고 Y는 PickerY에만 적용한다.
        public static PickCoordinateResult ApplyPickMechanicalOffsets(
            PickCoordinateResult result,
            double pickMechanicalOffsetX,
            double pickMechanicalOffsetY)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            result.PickerX += pickMechanicalOffsetX;
            result.NeedleX += pickMechanicalOffsetX;
            result.PickerY += pickMechanicalOffsetY;
            result.Formula =
                (result.Formula ?? string.Empty) +
                " / pickMechanicalOffsetX(" + F(pickMechanicalOffsetX) + ") applied equally to PickerX/NeedleX" +
                " / pickMechanicalOffsetY(" + F(pickMechanicalOffsetY) + ") applied only to PickerY" +
                " / pickup mechanical result=(pickerX=" + F(result.PickerX) +
                ", needleX=" + F(result.NeedleX) +
                ", pickerY=" + F(result.PickerY) +
                ", stageY unchanged=" + F(result.StageY) + ")";
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
            double pickerColletOffsetY,
            double pickerAlignOffsetX,
            double pickerAlignOffsetY,
            double pickerYTeaching,
            double pickerTTeaching,
            double pickerAlignOffsetT,
            double pickerZTeaching,
            double bottomOffsetX = 0.0,
            double bottomOffsetY = 0.0,
            double bottomOffsetT = 0.0,
            double placeRuntimeOffsetX = 0.0,
            double placeRuntimeOffsetY = 0.0,
            double placeRuntimeOffsetT = 0.0,
            double placeMechanicalOffsetX = 0.0,
            double placeMechanicalOffsetY = 0.0)
        {
            PlaceCoordinateResult result = new PlaceCoordinateResult();
            result.PickerY = pickerYTeaching;
            double pickerYRuntimeOffset = pickerAlignOffsetY;
            result.TargetSide = targetSide;
            // Preserve the recipe Y direction and apply Bottom/collet corrections once on OutputStageY.
            // Place 런타임 보정(placeRuntimeOffset*)은 Bin 후검사 LowPassFilter 출력(raw)이며
            // 비전 + 방향(과이동)을 상쇄하도록 X/T는 감산, Y는 스테이지 이동 방향 정의상 가산한다.
            // Place Y 기구 보정은 PickerY 티칭을 바꾸지 않고 선택된 GOOD/NG OutputStageY에만 더한다.
            result.OutputStageY =
                outputStageBaseY + receiveTargetY - bottomOffsetY - pickerColletOffsetY +
                placeRuntimeOffsetY + placeMechanicalOffsetY;

            // OutputCameraX와 PickerX는 Place 수령 방향이 같으므로 Output map X 오프셋은 PickerX에 더한다.
            // Bottom 검사 보정은 이동축 기준으로 PickerX/T와 OutputStageY에서 감산한다.
            result.PickerX =
                outputVisionProcessX + receiveTargetX + outputVisionToPickerX + pickerAlignOffsetX -
                bottomOffsetX - placeRuntimeOffsetX + placeMechanicalOffsetX;

            result.PickerT = pickerTTeaching - bottomOffsetT - placeRuntimeOffsetT;
            result.PickerZ = pickerZTeaching;
            result.Formula =
                "targetSide = " + targetSide +
                " / outputStageY = outputStageBaseY(" + F(outputStageBaseY) + ") + receiveTargetY(" + F(receiveTargetY) + ") - bottomOffsetY(" + F(bottomOffsetY) + ") - pickerColletOffsetY(" + F(pickerColletOffsetY) + ") + placeRuntimeOffsetY(" + F(placeRuntimeOffsetY) + ") + placeMechanicalOffsetY(" + F(placeMechanicalOffsetY) + ") = " + F(result.OutputStageY) +
                " / outputVisionToPickerY(" + F(outputVisionToPickerY) + ") is not used for PlaceStageY" +
                " / pickerYRuntimeOffset=" + F(pickerYRuntimeOffset) +
                " / pickerX = outputVisionProcessX(" + F(outputVisionProcessX) + ") + receiveTargetX(" + F(receiveTargetX) + ") + outputVisionToPickerX(" + F(outputVisionToPickerX) + ") + runtimeOffsetX(" + F(pickerAlignOffsetX) + ") - bottomOffsetX(" + F(bottomOffsetX) + ") - placeRuntimeOffsetX(" + F(placeRuntimeOffsetX) + ") + placeMechanicalOffsetX(" + F(placeMechanicalOffsetX) + ") = " + F(result.PickerX) +
                " / pickerT = placeTeachingT(" + F(pickerTTeaching) + ") - bottomOffsetT(" + F(bottomOffsetT) + ") - placeRuntimeOffsetT(" + F(placeRuntimeOffsetT) + ") [pickerAlignOffsetT ignored for place=" + F(pickerAlignOffsetT) + "] = " + F(result.PickerT) +
                " / pickerY = fixed pickerYTeaching(" + F(pickerYTeaching) + ") [runtimeOffsetY logged separately=" + F(pickerAlignOffsetY) + "] = " + F(result.PickerY) +
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
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", target=" + (targetId ?? string.Empty) +
                ", formula: " + formula);
        }

        private static int ToPickerNo(int pickerIndex)
        {
            return pickerIndex + 1;
        }

        private static string F(double value)
        {
            return value.ToString("F6");
        }
    }
}
