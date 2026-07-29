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
            // Pick 런타임 보정(pickRuntimeOffset*)은 Bottom 검사 LowPassFilter 출력(raw)이다.
            // 기존 조건(~2026-07-29): 전 채널 감산으로 상쇄.
            // 현재 기준(사용자 실장비 확인 2026-07-30 최종): X/Y 가산, T 감산.
            //   (Y는 감산 복귀 시험까지 거쳐 가산으로 확정 — P4 기준 델타는 전처리 쪽에서 감산으로 정정)
            // X는 피커·니들 정렬 유지를 위해 PickerX와 NeedleX 양쪽에 동일하게 적용한다.
            result.StageY = inputStageY + needleYToVisionYOffset - alignOffsetY + pickRuntimeOffsetY;
            result.PickerX = inputVisionX + inputVisionToPickerX + pickerAlignOffsetX + alignOffsetX + pickRuntimeOffsetX;
            // Collet T offset은 Picker T 홈 기준 보정에 이미 반영되므로 Pick 이동식에는 다시 더하지 않는다.
            // result.PickerT = pickerTTeaching + pickerAlignOffsetT + colletTOffset + alignOffsetT;
            result.PickerT = pickerTTeaching + pickerAlignOffsetT + alignOffsetT - pickRuntimeOffsetT;
            result.PickerZ = pickerZTeaching;
            result.NeedleX = inputVisionX + alignOffsetX - needleXToVisionXOffset + pickRuntimeOffsetX;
            // PickerY는 Die별 Vision Y와 무관하게 Picker별 고정 Pick 위치를 유지한다.
            double pickerYOffset = pickerAlignOffsetY;
            result.PickerY = inputVisionToPickerY + needleYToVisionYOffset + pickerYOffset;
            result.NeedleZ = needleZTeaching;
            result.EjectPinZ = ejectPinZTeaching;
            result.Formula =
                "stageY = inputStageY(" + F(inputStageY) + ") + needleYToVisionYOffset(" + F(needleYToVisionYOffset) + ") - alignOffsetY(" + F(alignOffsetY) + ") + pickRuntimeOffsetY(" + F(pickRuntimeOffsetY) + ") = " + F(result.StageY) +
                " [cameraOffset=(" + F(cameraOffsetX) + "," + F(cameraOffsetY) + ") applied once inside InputVisionToPicker (PickerX/PickerY only); alignOffset is raw camera delta]" +
                " / pickerX = inputVisionX(" + F(inputVisionX) + ") + inputVisionToPickerX(" + F(inputVisionToPickerX) + ") + pickerAlignOffsetX(" + F(pickerAlignOffsetX) + ") + alignOffsetX(" + F(alignOffsetX) + ") + pickRuntimeOffsetX(" + F(pickRuntimeOffsetX) + ") = " + F(result.PickerX) +
                " / pickerT = pickerTTeaching(" + F(pickerTTeaching) + ") + pickerAlignOffsetT(" + F(pickerAlignOffsetT) + ") + alignOffsetT(" + F(alignOffsetT) + ") - pickRuntimeOffsetT(" + F(pickRuntimeOffsetT) + ") = " + F(result.PickerT) +
                " / needleX = inputVisionX(" + F(inputVisionX) + ") + alignOffsetX(" + F(alignOffsetX) + ") - needleXToVisionXOffset(" + F(needleXToVisionXOffset) + ") + pickRuntimeOffsetX(" + F(pickRuntimeOffsetX) + ") = " + F(result.NeedleX) +
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
            double placeMechanicalOffsetY = 0.0,
            bool bottomFinalItemOffsetYIsSoleColletYCorrection = false)
        {
            PlaceCoordinateResult result = new PlaceCoordinateResult();
            result.TargetSide = targetSide;
            result.PickerY = pickerYTeaching;
            double pickerYRuntimeOffset = pickerAlignOffsetY;
            // 자동 Place는 모든 Collet을 P4 기준 PickerY에서 촬영한 Bottom FINAL을 사용한다.
            // 이 모드에서는 BottomItemOffsetY가 Collet별 실제 Y 오차를 포함한 단일 Place 보정값이므로
            // OutputVisionToPickerY(Collet Calibration 포함)를 OutputStageY에 다시 더하지 않는다.
            // Calibration/Preview 등 Bottom FINAL이 없는 호출은 기존 Camera-to-Picker Y 보정을 유지한다.
            double outputCameraToPickerY = bottomFinalItemOffsetYIsSoleColletYCorrection
                ? 0.0
                : outputVisionToPickerY - pickerYTeaching;

            // 구조: 축별 목표 = 다이맵 명목 목표(map*) + Place 보정(placeCorrection*)
            // 다이맵 명목 목표 — 보정이 전부 0일 때 빈 맵 슬롯 중심에 안착하는 좌표.
            double mapStageY = outputStageBaseY + receiveTargetY + outputCameraToPickerY;
            double mapPickerX = outputVisionProcessX + receiveTargetX + outputVisionToPickerX + pickerAlignOffsetX;

            // Place 보정 — Bottom 검사 보정은 이동축 기준으로 X/T/StageY 모두 감산 방향,
            // 런타임 보정(placeRuntimeOffset*)은 Bin 후검사 LowPassFilter 출력(raw)이다.
            // 기존 조건(~2026-07-29): Y만 "스테이지 이동 방향 정의상 가산"으로 두었다.
            // 현재 기준(사용자 실장비 확인 2026-07-29): X/Y/T 전 채널 감산 — Y 가산이 실측과
            //   반대 방향으로 확인되어 감산으로 정정한다.
            // (UsePlaceRuntimeOffset=false면 0이 전달되지만 항은 수식에 항상 유지한다)
            // Place Y 기구 보정은 PickerY 티칭을 바꾸지 않고 선택된 GOOD/NG OutputStageY에만 더한다.
            double placeCorrectionY = -bottomOffsetY - placeRuntimeOffsetY + placeMechanicalOffsetY;
            double placeCorrectionX = -bottomOffsetX - placeRuntimeOffsetX + placeMechanicalOffsetX;
            double placeCorrectionT = -bottomOffsetT - placeRuntimeOffsetT;

            result.OutputStageY = mapStageY + placeCorrectionY;
            result.PickerX = mapPickerX + placeCorrectionX;
            result.PickerT = pickerTTeaching + placeCorrectionT;
            result.PickerZ = pickerZTeaching;
            result.Formula =
                "targetSide = " + targetSide +
                " / outputCameraToPickerY = outputVisionToPickerY(" + F(outputVisionToPickerY) + ") - pickerYTeaching(" + F(pickerYTeaching) + ") = " + F(outputVisionToPickerY - pickerYTeaching) +
                ", appliedToOutputStageY=" + (!bottomFinalItemOffsetYIsSoleColletYCorrection) +
                ", usedValue=" + F(outputCameraToPickerY) +
                " / mapStageY = outputStageBaseY(" + F(outputStageBaseY) + ") + receiveTargetY(" + F(receiveTargetY) + ") + outputCameraToPickerY(" + F(outputCameraToPickerY) + ") = " + F(mapStageY) +
                " / placeCorrectionY = -bottomOffsetY(" + F(bottomOffsetY) + ") - placeRuntimeOffsetY(" + F(placeRuntimeOffsetY) + ") + placeMechanicalOffsetY(" + F(placeMechanicalOffsetY) + ") = " + F(placeCorrectionY) +
                " / outputStageY = mapStageY + placeCorrectionY = " + F(result.OutputStageY) +
                " / pickerColletOffsetY(" + F(pickerColletOffsetY) + ") " +
                (bottomFinalItemOffsetYIsSoleColletYCorrection
                    ? "is represented by Bottom FINAL ItemOffsetY and is not reapplied to OutputStageY"
                    : "is already included in outputVisionToPickerY") +
                " / pickerYRuntimeOffset=" + F(pickerYRuntimeOffset) +
                " / mapPickerX = outputVisionProcessX(" + F(outputVisionProcessX) + ") + receiveTargetX(" + F(receiveTargetX) + ") + outputVisionToPickerX(" + F(outputVisionToPickerX) + ") + runtimeOffsetX(" + F(pickerAlignOffsetX) + ") = " + F(mapPickerX) +
                " / placeCorrectionX = -bottomOffsetX(" + F(bottomOffsetX) + ") - placeRuntimeOffsetX(" + F(placeRuntimeOffsetX) + ") + placeMechanicalOffsetX(" + F(placeMechanicalOffsetX) + ") = " + F(placeCorrectionX) +
                " / pickerX = mapPickerX + placeCorrectionX = " + F(result.PickerX) +
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
