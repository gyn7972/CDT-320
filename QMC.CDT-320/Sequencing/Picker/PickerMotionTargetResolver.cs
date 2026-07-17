using QMC.CDT320.Calibration;

namespace QMC.CDT320.Sequencing
{
    internal enum PickerCoordinateCorrectionPolicy
    {
        // InputVisionToPicker X/Y already uses the collet final position, so Pick X/Y adds runtime only.
        InputPick,
        // Carry moves use runtime + collet for X/Y. T uses runtime only because collet T is handled by home zero.
        CarryRuntimeAndCollet,
        // Permanent calibration uses teaching coordinates without runtime/collet corrections.
        CalibrationNominal,
        // Re-measurement uses saved collet X/Y correction only. T is not added to motion targets.
        CalibrationSavedCollet
    }

    // Centralizes picker X/Y/T target math so auto sequences, manual map moves, and calibration moves use one formula path.
    internal static class PickerMotionTargetResolver
    {
        // Resolves InputVision -> Picker setup offsets, then calculates the input die pick target.
        public static bool TryCalculateInputPickTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            string sequenceName,
            string dieId,
            double inputVisionX,
            double inputStageY,
            double visionAlignOffsetX,
            double visionAlignOffsetY,
            double visionAlignOffsetT,
            bool logFormula,
            out PickCoordinateResult target,
            out string reason)
        {
            target = null;
            reason = string.Empty;

            double inputVisionToPickerX;
            double inputVisionToPickerY;
            string offsetReason;
            if (!PickerCoordinateTransformHelper.TryResolveInputVisionToPickerOffsets(
                machine,
                side,
                pickerIndex,
                out inputVisionToPickerX,
                out inputVisionToPickerY,
                out offsetReason))
            {
                reason = offsetReason;
                return false;
            }

            target = CalculateInputPickTarget(
                machine,
                side,
                pickerIndex,
                sequenceName,
                dieId,
                inputVisionX,
                inputStageY,
                inputVisionToPickerX,
                inputVisionToPickerY,
                visionAlignOffsetX,
                visionAlignOffsetY,
                visionAlignOffsetT,
                logFormula);
            return true;
        }

        // Calculates the picker/input-stage target for picking a die already centered by InputVision.
        // runtimeT는 적용하고 ColletT는 홈 기준 보정이라 로그만 남기고 이동식에는 적용하지 않는다.
        public static PickCoordinateResult CalculateInputPickTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            string sequenceName,
            string dieId,
            double inputVisionX,
            double inputStageY,
            double inputVisionToPickerX,
            double inputVisionToPickerY,
            double visionAlignOffsetX,
            double visionAlignOffsetY,
            double visionAlignOffsetT,
            bool logFormula)
        {
            double cameraOffsetX;
            double cameraOffsetY;
            InputPickerPickTargetResolver.TryResolveInputCameraToBottomOffsets(
                machine,
                out cameraOffsetX,
                out cameraOffsetY);

            PickerAlignOffset runtime = InputPickerPickTargetResolver.ResolveRuntimePickerOffset(machine, side, pickerIndex);
            double runtimeX = runtime != null ? runtime.AlignOffsetX : 0.0;
            double runtimeY = runtime != null ? runtime.AlignOffsetY : 0.0;
            double runtimeT = runtime != null ? runtime.AlignOffsetT : 0.0;
            double colletT = InputPickerPickTargetResolver.ResolveColletTOffset(machine, side, pickerIndex);
            // Collet T offset은 Picker T 홈 기준 보정에 이미 반영되므로 Pick 이동식에는 다시 더하지 않는다.
            // double appliedColletT = colletT;
            double appliedColletT = 0.0;

            PickCoordinateResult result = DieCoordinateTransformService.CalculatePickTarget(
                sequenceName,
                side,
                pickerIndex,
                string.IsNullOrWhiteSpace(dieId) ? string.Empty : dieId,
                inputVisionX,
                inputStageY,
                inputVisionToPickerX,
                inputVisionToPickerY,
                runtimeX,
                runtimeY,
                runtimeT,
                cameraOffsetX,
                cameraOffsetY,
                visionAlignOffsetX,
                visionAlignOffsetY,
                visionAlignOffsetT,
                InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetX(machine),
                InputPickerPickTargetResolver.ResolveNeedleCalibrationOffsetY(machine),
                InputPickerPickTargetResolver.ResolvePickerYPickTeaching(machine, side),
                InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                    machine,
                    side,
                    CalibrationCoordinateService.ResolvePickerTAxis(pickerIndex),
                    "PickPosition"),
                InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                    machine,
                    side,
                    CalibrationCoordinateService.ResolvePickerZAxis(pickerIndex),
                    "PickPosition"),
                InputPickerPickTargetResolver.ResolveNeedleZPickTarget(machine),
                InputPickerPickTargetResolver.ResolveEjectPinZPickTarget(machine),
                logFormula);

            WriteCoordinateLog(
                "InputPickTarget",
                "sequence=" + (sequenceName ?? string.Empty) +
                ", side=" + side +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", die=" + (dieId ?? string.Empty) +
                ", runtimeSource=PickerAlignOffset" +
                ", runtimeX=" + F(runtimeX) +
                ", runtimeY=" + F(runtimeY) +
                ", runtimeT=" + F(runtimeT) +
                ", colletTOffset=" + F(colletT) +
                ", colletTAppliedToMove=" + F(appliedColletT) +
                ", inputVisionToPickerX=" + F(inputVisionToPickerX) +
                ", inputVisionToPickerY=" + F(inputVisionToPickerY) +
                ", cameraOffsetX=" + F(cameraOffsetX) +
                ", cameraOffsetY=" + F(cameraOffsetY) +
                ", visionAlignOffsetX=" + F(visionAlignOffsetX) +
                ", visionAlignOffsetY=" + F(visionAlignOffsetY) +
                ", visionAlignOffsetT=" + F(visionAlignOffsetT) +
                ", finalStageY=" + F(result.StageY) +
                ", finalPickerX=" + F(result.PickerX) +
                ", finalPickerY=" + F(result.PickerY) +
                ", finalPickerT=" + F(result.PickerT) +
                ", finalNeedleX=" + F(result.NeedleX) +
                ", formula=" + result.Formula);
            return result;
        }

        // Calculates bottom/side/place carry-position targets with runtime and saved collet X/Y corrections applied.
        public static PickerCalibratedZoneTarget ResolveCarryZoneTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            string positionArrayName,
            int pickerIndex)
        {
            return ResolveZoneTarget(
                machine,
                side,
                positionArrayName,
                pickerIndex,
                PickerCoordinateCorrectionPolicy.CarryRuntimeAndCollet);
        }

        // Applies the selected correction policy to a taught picker zone position.
        // T always excludes collet theta from move targets because theta is handled by homing zero.
        public static PickerCalibratedZoneTarget ResolveZoneTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            string positionArrayName,
            int pickerIndex,
            PickerCoordinateCorrectionPolicy policy)
        {
            bool includeRuntime = policy == PickerCoordinateCorrectionPolicy.InputPick ||
                                  policy == PickerCoordinateCorrectionPolicy.CarryRuntimeAndCollet;
            bool includeCollet = policy == PickerCoordinateCorrectionPolicy.CarryRuntimeAndCollet ||
                                 policy == PickerCoordinateCorrectionPolicy.CalibrationSavedCollet;

            PickerCalibratedZoneTarget target = CalibrationCoordinateService.ResolvePickerZoneTarget(
                machine,
                ToVisionFocusPickerSide(side),
                positionArrayName,
                pickerIndex,
                InputPickerPickTargetResolver.ResolveRuntimePickerOffset(machine, side, pickerIndex),
                includeRuntime,
                includeCollet);

            WriteCoordinateLog(
                "PickerZoneTarget",
                "side=" + side +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", positionArrayName=" + (positionArrayName ?? string.Empty) +
                ", policy=" + policy +
                ", runtimeSource=PickerAlignOffset" +
                ", runtimeX=" + F(target.RuntimeOffsetX) +
                ", runtimeY=" + F(target.RuntimeOffsetY) +
                ", runtimeT=" + F(target.RuntimeOffsetT) +
                ", colletX=" + F(target.ColletOffsetX) +
                ", colletY=" + F(target.ColletOffsetY) +
                ", colletTAppliedToMove=" + F(target.ColletOffsetT) +
                ", finalX=" + F(target.X) +
                ", finalY=" + F(target.Y) +
                ", finalT=" + F(target.T) +
                ", finalZ=" + F(target.Z) +
                ", formula=" + target.Formula);
            return target;
        }

        // Formats a corrected zone target by axis so operators can compare each correction source.
        public static string FormatZoneTargetByAxis(PickerCalibratedZoneTarget target, string lineBreak)
        {
            if (target == null)
                return string.Empty;

            string br = string.IsNullOrEmpty(lineBreak) ? System.Environment.NewLine : lineBreak;
            return
                "X축: teachingX(" + F(target.TeachingX) + ") + pitchX(" + F(target.PitchOffsetX) +
                ") + runtimeX(" + F(target.RuntimeOffsetX) + ") + colletX(" + F(target.ColletOffsetX) +
                ") = " + F(target.X) + " mm" + br +
                "Y축: teachingY(" + F(target.TeachingY) + ") + runtimeY(" + F(target.RuntimeOffsetY) +
                ") + colletY(" + F(target.ColletOffsetY) + ") = " + F(target.Y) + " mm" + br +
                "T축: teachingT(" + F(target.TeachingT) + ") + runtimeT(" + F(target.RuntimeOffsetT) +
                ") + colletT(homeZeroApplied)(" + F(target.ColletOffsetT) + ") = " + F(target.T) + " deg" + br +
                "Z축: teachingZ(" + F(target.TeachingZ) + ") = " + F(target.Z) + " mm";
        }

        // Calculates the output-stage place target using the same carried picker correction values as pickup.
        public static PlaceCoordinateResult CalculateOutputPlaceTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            string sequenceName,
            string dieId,
            BinSide targetSide,
            double outputStageBaseY,
            double receiveTargetX,
            double receiveTargetY,
            double outputVisionProcessX,
            double outputVisionToPickerX,
            double outputVisionToPickerY,
            double bottomOffsetX = 0.0,
            double bottomOffsetY = 0.0,
            double bottomOffsetT = 0.0)
        {
            PickerAlignOffset runtime = InputPickerPickTargetResolver.ResolveRuntimePickerOffset(machine, side, pickerIndex);
            PickerCalibrationOffset collet = ResolveColletOffset(machine, side, pickerIndex);
            double runtimeOffsetX = runtime != null ? runtime.AlignOffsetX : 0.0;
            double runtimeOffsetY = runtime != null ? runtime.AlignOffsetY : 0.0;
            double runtimeOffsetT = runtime != null ? runtime.AlignOffsetT : 0.0;
            double colletOffsetX = collet != null ? collet.X : 0.0;
            double colletOffsetY = collet != null ? collet.Y : 0.0;
            double pickerYTeaching = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                machine,
                side,
                PickerAxis.PickerY,
                "PlacePosition");
            double pickerTTeaching = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                machine,
                side,
                CalibrationCoordinateService.ResolvePickerTAxis(pickerIndex),
                "PlacePosition");
            double pickerZTeaching = InputPickerPickTargetResolver.ResolvePickerTeachingPosition(
                machine,
                side,
                CalibrationCoordinateService.ResolvePickerZAxis(pickerIndex),
                "PlacePosition");
            PlaceCoordinateResult result = DieCoordinateTransformService.CalculatePlaceTarget(
                sequenceName,
                side,
                pickerIndex,
                string.IsNullOrWhiteSpace(dieId) ? string.Empty : dieId,
                targetSide,
                outputStageBaseY,
                receiveTargetY,
                outputVisionProcessX,
                receiveTargetX,
                outputVisionToPickerX,
                outputVisionToPickerY,
                colletOffsetY,
                runtimeOffsetX,
                runtimeOffsetY,
                pickerYTeaching,
                pickerTTeaching,
                runtimeOffsetT,
                pickerZTeaching,
                bottomOffsetX,
                bottomOffsetY,
                bottomOffsetT);

            WriteCoordinateLog(
                "OutputPlaceTarget",
                "sequence=" + (sequenceName ?? string.Empty) +
                ", side=" + side +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", die=" + (dieId ?? string.Empty) +
                ", targetSide=" + targetSide +
                ", runtimeSource=PickerAlignOffset" +
                ", runtimeOffsetX=" + F(runtimeOffsetX) +
                ", runtimeOffsetY=" + F(runtimeOffsetY) +
                ", runtimeT=" + F(runtimeOffsetT) +
                ", colletXAlreadyInOutputVisionToPicker=" + F(colletOffsetX) +
                ", colletYAppliedOnceToOutputStageY=" + F(colletOffsetY) +
                ", colletTAppliedToMove=0.000000" +
                ", outputStageBaseY=" + F(outputStageBaseY) +
                ", receiveTargetX=" + F(receiveTargetX) +
                ", receiveTargetY=" + F(receiveTargetY) +
                ", outputVisionProcessX=" + F(outputVisionProcessX) +
                ", outputVisionToPickerX=" + F(outputVisionToPickerX) +
                ", outputVisionToPickerY=" + F(outputVisionToPickerY) +
                ", bottomOffsetX=" + F(bottomOffsetX) +
                ", bottomOffsetY=" + F(bottomOffsetY) +
                ", bottomOffsetT=" + F(bottomOffsetT) +
                ", pickerYTeaching=" + F(pickerYTeaching) +
                ", pickerTTeaching=" + F(pickerTTeaching) +
                ", pickerZTeaching=" + F(pickerZTeaching) +
                ", finalOutputStageY=" + F(result.OutputStageY) +
                ", finalPickerX=" + F(result.PickerX) +
                ", finalPickerY=" + F(result.PickerY) +
                ", finalPickerT=" + F(result.PickerT) +
                ", formula=" + result.Formula);

            WriteCoordinateLog(
                "OutputPlaceFormula",
                "sequence=" + (sequenceName ?? string.Empty) +
                ", side=" + side +
                ", pickerNo=" + ToPickerNo(pickerIndex) +
                ", die=" + (dieId ?? string.Empty) +
                ", targetSide=" + targetSide +
                ", formulaPickerX=outputVisionProcessX(" + F(outputVisionProcessX) +
                ")+receiveTargetX(" + F(receiveTargetX) +
                ")+outputVisionToPickerX(" + F(outputVisionToPickerX) +
                ")+runtimeOffsetX(" + F(runtimeOffsetX) +
                ")-bottomOffsetX(" + F(bottomOffsetX) +
                ")=" + F(result.PickerX) +
                ", colletXAlreadyInOutputVisionToPicker=" + F(colletOffsetX) +
                ", colletXNotAddedAgain=True" +
                ", pickerXIfColletDoubleAdded=" + F(result.PickerX + colletOffsetX) +
                ", formulaOutputStageY=outputStageBaseY(" + F(outputStageBaseY) +
                ")+receiveTargetY(" + F(receiveTargetY) +
                ")-bottomOffsetY(" + F(bottomOffsetY) +
                ")-pickerColletOffsetY(" + F(colletOffsetY) +
                ")=" + F(result.OutputStageY) +
                ", outputVisionToPickerYNotUsedForPlaceStageY=" + F(outputVisionToPickerY) +
                ", runtimeOffsetYLoggedOnly=" + F(runtimeOffsetY) +
                ", colletYAppliedOnceToOutputStageY=" + F(colletOffsetY) +
                ", pickerYFixed=" + F(result.PickerY) +
                ", pickerT=placeTeachingT(" + F(pickerTTeaching) +
                ")-bottomOffsetT(" + F(bottomOffsetT) +
                ")=" + F(result.PickerT) +
                ", pickerZ=placeTeachingZ(" + F(pickerZTeaching) +
                ")=" + F(result.PickerZ) +
                " - Calc");
            return result;
        }

        private static PickerCalibrationOffset ResolveColletOffset(CDT320_Machine machine, PickerSequenceSide side, int pickerIndex)
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

        // Writes coordinate calculation values before motion so field logs can compare formula target and final axis position.
        private static void WriteCoordinateLog(string source, string message)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", source, message + " - Calc");
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string F(double value)
        {
            return value.ToString("F6");
        }

        private static int ToPickerNo(int pickerIndex)
        {
            return pickerIndex + 1;
        }
    }
}
