using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    internal sealed class RecipePickerMoveTarget
    {
        public int PickerIndex { get; set; }
        public int PickerNo { get; set; }
        public PickerAxis PickerTAxis { get; set; }
        public PickerAxis PickerZAxis { get; set; }
        public string PositionArrayName { get; set; }
        public string ZonePositionName { get; set; }
        public string SourceMode { get; set; }
        public string LoadedDieId { get; set; }
        public DieResult LoadedDieResult { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double T { get; set; }
        public double Z { get; set; }
        public string AxisFormula { get; set; }
        public string Formula { get; set; }

        public bool HasLoadedDie
        {
            get { return !string.IsNullOrWhiteSpace(LoadedDieId); }
        }

        public string LoadedDieText
        {
            get
            {
                if (!HasLoadedDie)
                    return "None";

                return LoadedDieId + " / Result=" + LoadedDieResult;
            }
        }
    }

    internal static class RecipePickerMoveTargetResolver
    {
        public static bool TryResolve(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerNo,
            string positionArrayName,
            string zoneText,
            out RecipePickerMoveTarget target,
            out string reason)
        {
            target = null;
            reason = string.Empty;

            int pickerIndex = pickerNo - 1;
            if (pickerIndex < 0 || pickerIndex >= 4)
            {
                reason = "Picker number is out of range. pickerNo=" + pickerNo;
                return false;
            }

            MaterialLocationKind pickerLocation = side == PickerSequenceSide.Front
                ? MaterialLocationKind.PickerFront
                : MaterialLocationKind.PickerRear;
            DieMaterial loadedDie = MaterialStateService.GetDieAtPicker(pickerLocation, pickerNo);

            if (loadedDie != null && IsPickPosition(positionArrayName))
                return TryResolveLoadedDiePickTarget(
                    machine,
                    side,
                    pickerIndex,
                    pickerNo,
                    positionArrayName,
                    loadedDie,
                    out target,
                    out reason);

            PickerCalibratedZoneTarget zoneTarget = PickerMotionTargetResolver.ResolveCarryZoneTarget(
                machine,
                side,
                positionArrayName,
                pickerIndex);

            target = FromZoneTarget(
                zoneTarget,
                loadedDie,
                loadedDie != null ? "LoadedDieCarryZone" : "TeachingCarryZone");
            return true;
        }

        private static bool TryResolveLoadedDiePickTarget(
            CDT320_Machine machine,
            PickerSequenceSide side,
            int pickerIndex,
            int pickerNo,
            string positionArrayName,
            DieMaterial loadedDie,
            out RecipePickerMoveTarget target,
            out string reason)
        {
            target = null;
            reason = string.Empty;

            if (loadedDie.WaferOffset == null || !loadedDie.WaferOffset.IsValid)
            {
                reason = "Loaded die has no valid WaferOffset. die=" + loadedDie.DieId;
                return false;
            }

            VisionOffset inputVisionOffset;
            if (!MaterialStateService.TryGetLatestInputPickVisionOffset(loadedDie.DieId, out inputVisionOffset) ||
                inputVisionOffset == null ||
                !inputVisionOffset.IsValid)
            {
                reason = "Loaded die has no valid InputPickVision offset. die=" + loadedDie.DieId;
                return false;
            }

            PickCoordinateResult pickTarget;
            string coordinateReason;
            if (!PickerMotionTargetResolver.TryCalculateInputPickTarget(
                machine,
                side,
                pickerIndex,
                "RecipePickerMoveTarget.LoadedDiePick",
                loadedDie.DieId,
                loadedDie.WaferOffset.X,
                loadedDie.WaferOffset.Y,
                0.0,
                0.0,
                inputVisionOffset.R,
                true,
                out pickTarget,
                out coordinateReason))
            {
                reason = coordinateReason;
                return false;
            }

            target = new RecipePickerMoveTarget
            {
                PickerIndex = pickerIndex,
                PickerNo = pickerNo,
                PickerTAxis = CalibrationCoordinateService.ResolvePickerTAxis(pickerIndex),
                PickerZAxis = CalibrationCoordinateService.ResolvePickerZAxis(pickerIndex),
                PositionArrayName = string.IsNullOrWhiteSpace(positionArrayName) ? "DiePickPosition" : positionArrayName,
                ZonePositionName = "PickPosition",
                SourceMode = "LoadedDieInputPick",
                LoadedDieId = loadedDie.DieId,
                LoadedDieResult = loadedDie.Result,
                X = pickTarget.PickerX,
                Y = pickTarget.PickerY,
                T = pickTarget.PickerT,
                Z = pickTarget.PickerZ,
                Formula = pickTarget.Formula,
                AxisFormula =
                    "X axis: PickerX=" + F(pickTarget.PickerX) + " mm" + System.Environment.NewLine +
                    "Y axis: PickerY=" + F(pickTarget.PickerY) + " mm" + System.Environment.NewLine +
                    "T axis: PickerT=" + F(pickTarget.PickerT) + " deg" + System.Environment.NewLine +
                    "Z axis: PickerZ=" + F(pickTarget.PickerZ) + " mm" + System.Environment.NewLine +
                    "Input die: " + loadedDie.DieId +
                    ", waferOffset=(" + F(loadedDie.WaferOffset.X) + "," + F(loadedDie.WaferOffset.Y) + ")" +
                    ", inputVisionT=" + F(inputVisionOffset.R) + System.Environment.NewLine +
                    "Auto formula: " + pickTarget.Formula
            };
            return true;
        }

        private static RecipePickerMoveTarget FromZoneTarget(
            PickerCalibratedZoneTarget zoneTarget,
            DieMaterial loadedDie,
            string sourceMode)
        {
            return new RecipePickerMoveTarget
            {
                PickerIndex = zoneTarget.PickerIndex,
                PickerNo = zoneTarget.PickerIndex + 1,
                PickerTAxis = zoneTarget.PickerTAxis,
                PickerZAxis = zoneTarget.PickerZAxis,
                PositionArrayName = zoneTarget.PositionArrayName,
                ZonePositionName = zoneTarget.ZonePositionName,
                SourceMode = sourceMode,
                LoadedDieId = loadedDie != null ? loadedDie.DieId : string.Empty,
                LoadedDieResult = loadedDie != null ? loadedDie.Result : DieResult.Unknown,
                X = zoneTarget.X,
                Y = zoneTarget.Y,
                T = zoneTarget.T,
                Z = zoneTarget.Z,
                Formula = zoneTarget.Formula,
                AxisFormula = PickerMotionTargetResolver.FormatZoneTargetByAxis(zoneTarget, System.Environment.NewLine)
            };
        }

        private static bool IsPickPosition(string positionArrayName)
        {
            return string.Equals(positionArrayName, "DiePickPosition", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(positionArrayName, "PickPosition", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string F(double value)
        {
            return value.ToString("F6");
        }
    }
}
