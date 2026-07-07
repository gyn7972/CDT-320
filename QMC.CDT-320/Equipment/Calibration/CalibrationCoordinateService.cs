using System;

namespace QMC.CDT320.Calibration
{
    public sealed class PickerCalibrationOffset
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double T { get; set; }
        public bool Valid { get; set; }
    }

    public sealed class PickerCalibratedZoneTarget
    {
        public VisionFocusPickerSide Side { get; set; }
        public string PositionArrayName { get; set; }
        public string ZonePositionName { get; set; }
        public int PickerIndex { get; set; }
        public PickerAxis PickerZAxis { get; set; }
        public PickerAxis PickerTAxis { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double T { get; set; }
        public double Z { get; set; }
        public double TeachingX { get; set; }
        public double TeachingY { get; set; }
        public double TeachingT { get; set; }
        public double TeachingZ { get; set; }
        public double PitchOffsetX { get; set; }
        public double RuntimeOffsetX { get; set; }
        public double RuntimeOffsetY { get; set; }
        public double RuntimeOffsetT { get; set; }
        public double ColletOffsetX { get; set; }
        public double ColletOffsetY { get; set; }
        public double ColletOffsetT { get; set; }
        public string Formula { get; set; }
    }

    public sealed class PickerCalibratedManualInputTarget
    {
        public double PickerX { get; set; }
        public double StageY { get; set; }
        public double InputVisionToPickerX { get; set; }
        public double InputVisionToPickerY { get; set; }
        public double PickerYForward { get; set; }
        public double ColletOffsetX { get; set; }
        public double ColletOffsetY { get; set; }
        public string Formula { get; set; }
    }

    public sealed class PickerCalibratedManualOutputTarget
    {
        public double OutputStageY { get; set; }
        public double PickerX { get; set; }
        public double PickerY { get; set; }
        public double PickerT { get; set; }
        public double OutputVisionToPickerX { get; set; }
        public double OutputVisionToPickerY { get; set; }
        public double PickerYForward { get; set; }
        public double RuntimeOffsetX { get; set; }
        public double RuntimeOffsetY { get; set; }
        public double RuntimeOffsetT { get; set; }
        public double ColletOffsetX { get; set; }
        public double ColletOffsetY { get; set; }
        public double ColletOffsetT { get; set; }
        public string Formula { get; set; }
    }

    public static class CalibrationCoordinateService
    {
        public static System.Func<CDT320_Machine> MachineProvider { get; set; }

        public static CDT320_Machine ResolveMachine()
        {
            try
            {
                return MachineProvider != null ? MachineProvider() : null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        public static CalibrationData ResolveData(CDT320_Machine machine)
        {
            if (machine == null ||
                machine.VisionUnit == null ||
                machine.VisionUnit.Config == null)
                return null;

            machine.VisionUnit.Config.EnsureCalibrationObjects();
            return machine.VisionUnit.Config.CalibrationData;
        }

        public static VisionCameraCalibrationData ResolveCamera(CDT320_Machine machine)
        {
            CalibrationData data = ResolveData(machine);
            return data != null ? data.Camera : null;
        }

        public static ColletCalibrationRecord ResolveCollet(
            CDT320_Machine machine,
            VisionFocusPickerSide side,
            int pickerIndex)
        {
            CalibrationData data = ResolveData(machine);
            if (data == null || data.Collet == null)
                return null;

            data.Collet.EnsureObjects();
            ColletCalibrationRecord record = data.Collet.GetRecord(side, pickerIndex + 1);
            return record != null && record.Valid ? record : null;
        }

        public static ColletCalibrationRecord ResolveCollet(
            VisionFocusPickerSide side,
            int pickerIndex)
        {
            return ResolveCollet(ResolveMachine(), side, pickerIndex);
        }

        public static PickerCalibrationOffset ResolvePickerCalibrationOffset(
            CDT320_Machine machine,
            VisionFocusPickerSide side,
            int pickerIndex)
        {
            ColletCalibrationRecord record = ResolveCollet(machine, side, pickerIndex);
            if (record == null)
                return new PickerCalibrationOffset();

            return new PickerCalibrationOffset
            {
                X = record.OffsetX,
                Y = record.OffsetY,
                T = record.ThetaOffset,
                Valid = true
            };
        }

        public static PickerCalibrationOffset ResolvePickerCalibrationOffset(
            VisionFocusPickerSide side,
            int pickerIndex)
        {
            return ResolvePickerCalibrationOffset(ResolveMachine(), side, pickerIndex);
        }

        public static PickerCalibratedZoneTarget ResolvePickerZoneTarget(
            CDT320_Machine machine,
            VisionFocusPickerSide side,
            string positionArrayName,
            int pickerIndex,
            PickerAlignOffset runtimeOffset,
            bool includeRuntimeOffset,
            bool includeColletCalibrationOffset)
        {
            PickerFrontUnit front = side == VisionFocusPickerSide.Front && machine != null ? machine.PickerFrontUnit : null;
            PickerRearUnit rear = side == VisionFocusPickerSide.Rear && machine != null ? machine.PickerRearUnit : null;
            int index = NormalizePickerIndex(pickerIndex);
            string arrayName = string.IsNullOrWhiteSpace(positionArrayName) ? "DiePickPosition" : positionArrayName;
            string zonePositionName = ResolveZonePositionName(arrayName);
            PickerAxis tAxis = ResolvePickerTAxis(index);
            PickerAxis zAxis = ResolvePickerZAxis(index);

            double teachingX = 0.0;
            double teachingY = 0.0;
            double teachingT = 0.0;
            double teachingZ = 0.0;
            double pitchX = 0.0;

            if (front != null)
            {
                teachingX = front.GetPickerTeachingPosition(PickerAxis.PickerX, zonePositionName);
                teachingY = front.GetPickerTeachingPosition(PickerAxis.PickerY, zonePositionName);
                teachingT = front.GetPickerTeachingPosition(tAxis, zonePositionName);
                teachingZ = front.GetPickerTeachingPosition(zAxis, zonePositionName);
                pitchX = front.Setup != null ? front.Setup.PickerPitchX : 0.0;
            }
            else if (rear != null)
            {
                teachingX = rear.GetPickerTeachingPosition(PickerAxis.PickerX, zonePositionName);
                teachingY = rear.GetPickerTeachingPosition(PickerAxis.PickerY, zonePositionName);
                teachingT = rear.GetPickerTeachingPosition(tAxis, zonePositionName);
                teachingZ = rear.GetPickerTeachingPosition(zAxis, zonePositionName);
                pitchX = rear.Setup != null ? rear.Setup.PickerPitchX : 0.0;
            }

            double pitchOffsetX = ResolvePickerPitchXOffset(arrayName, index, pitchX);
            double runtimeX = includeRuntimeOffset && runtimeOffset != null ? runtimeOffset.AlignOffsetX : 0.0;
            double runtimeY = includeRuntimeOffset && runtimeOffset != null ? runtimeOffset.AlignOffsetY : 0.0;
            double runtimeT = includeRuntimeOffset && runtimeOffset != null ? runtimeOffset.AlignOffsetT : 0.0;
            PickerCalibrationOffset collet = includeColletCalibrationOffset
                ? ResolvePickerCalibrationOffset(machine, side, index)
                : new PickerCalibrationOffset();

            PickerCalibratedZoneTarget target = new PickerCalibratedZoneTarget
            {
                Side = side,
                PositionArrayName = arrayName,
                ZonePositionName = zonePositionName,
                PickerIndex = index,
                PickerZAxis = zAxis,
                PickerTAxis = tAxis,
                TeachingX = teachingX,
                TeachingY = teachingY,
                TeachingT = teachingT,
                TeachingZ = teachingZ,
                PitchOffsetX = pitchOffsetX,
                RuntimeOffsetX = runtimeX,
                RuntimeOffsetY = runtimeY,
                RuntimeOffsetT = runtimeT,
                ColletOffsetX = collet.X,
                ColletOffsetY = collet.Y,
                ColletOffsetT = collet.T
            };
            target.X = teachingX + pitchOffsetX + runtimeX + collet.X;
            target.Y = teachingY + runtimeY + collet.Y;
            target.T = teachingT + runtimeT + collet.T;
            target.Z = teachingZ;
            target.Formula =
                "X=teachingX(" + F(teachingX) + ")+pitchX(" + F(pitchOffsetX) + ")+runtimeX(" + F(runtimeX) + ")+colletX(" + F(collet.X) + ")=" + F(target.X) +
                " / Y=teachingY(" + F(teachingY) + ")+runtimeY(" + F(runtimeY) + ")+colletY(" + F(collet.Y) + ")=" + F(target.Y) +
                " / T=teachingT(" + F(teachingT) + ")+runtimeT(" + F(runtimeT) + ")+colletT(" + F(collet.T) + ")=" + F(target.T) +
                " / Z=teachingZ(" + F(teachingZ) + ")";
            return target;
        }

        public static PickerCalibratedManualInputTarget ResolveManualInputMapTarget(
            CDT320_Machine machine,
            VisionFocusPickerSide side,
            int pickerIndex,
            double dieX,
            double dieY,
            double inputVisionToPickerX,
            double inputVisionToPickerY,
            double pickerYTeaching)
        {
            PickerCalibrationOffset collet = ResolvePickerCalibrationOffset(machine, side, pickerIndex);
            double pickerYForward = Math.Abs(pickerYTeaching);
            PickerCalibratedManualInputTarget target = new PickerCalibratedManualInputTarget
            {
                InputVisionToPickerX = inputVisionToPickerX,
                InputVisionToPickerY = inputVisionToPickerY,
                PickerYForward = pickerYForward,
                ColletOffsetX = collet.X,
                ColletOffsetY = collet.Y
            };
            target.PickerX = dieX + inputVisionToPickerX + collet.X;
            // 현재 기준: PickerY 전진량은 StageY 보정에서 제외해 Y 방향 보상이 중복되지 않게 한다.
            target.StageY = dieY + inputVisionToPickerY - pickerYForward + collet.Y;
            target.Formula =
                "PickerX=dieX(" + F(dieX) + ")+inputVisionToPickerX(" + F(inputVisionToPickerX) + ")+colletX(" + F(collet.X) + ")=" + F(target.PickerX) +
                " / StageY=dieY(" + F(dieY) + ")+inputVisionToPickerY(" + F(inputVisionToPickerY) + ")-pickerYForward(" + F(pickerYForward) + ")+colletY(" + F(collet.Y) + ")=" + F(target.StageY);
            return target;
        }

        public static PickerCalibratedManualOutputTarget ResolveManualOutputMapTarget(
            CDT320_Machine machine,
            VisionFocusPickerSide side,
            int pickerIndex,
            double slotX,
            double slotY,
            double outputVisionToPickerX,
            double outputVisionToPickerY,
            PickerAlignOffset runtimeOffset,
            double pickerYTeaching,
            double pickerTTeaching)
        {
            PickerCalibrationOffset collet = ResolvePickerCalibrationOffset(machine, side, pickerIndex);
            double runtimeX = runtimeOffset != null ? runtimeOffset.AlignOffsetX : 0.0;
            double runtimeY = runtimeOffset != null ? runtimeOffset.AlignOffsetY : 0.0;
            double runtimeT = runtimeOffset != null ? runtimeOffset.AlignOffsetT : 0.0;
            double pickerYTarget = pickerYTeaching + runtimeY + collet.Y;
            double pickerYForward = Math.Abs(pickerYTarget);
            PickerCalibratedManualOutputTarget target = new PickerCalibratedManualOutputTarget
            {
                OutputVisionToPickerX = outputVisionToPickerX,
                OutputVisionToPickerY = outputVisionToPickerY,
                PickerYForward = pickerYForward,
                RuntimeOffsetX = runtimeX,
                RuntimeOffsetY = runtimeY,
                RuntimeOffsetT = runtimeT,
                ColletOffsetX = collet.X,
                ColletOffsetY = collet.Y,
                ColletOffsetT = collet.T
            };
            // 현재 기준: Picker별 Y 보정까지 포함한 최종 PickerY 전진량으로 OutputStageY를 보상한다.
            target.OutputStageY = slotY + outputVisionToPickerY - pickerYForward;
            target.PickerX = slotX + outputVisionToPickerX + runtimeX + collet.X;
            target.PickerY = pickerYTarget;
            target.PickerT = pickerTTeaching + runtimeT + collet.T;
            target.Formula =
                "OutputStageY=slotY(" + F(slotY) + ")+outputVisionToPickerY(" + F(outputVisionToPickerY) + ")-pickerYForward(abs(PickerY))(" + F(pickerYForward) + ")=" + F(target.OutputStageY) +
                " / PickerX=slotX(" + F(slotX) + ")+outputVisionToPickerX(" + F(outputVisionToPickerX) + ")+runtimeX(" + F(runtimeX) + ")+colletX(" + F(collet.X) + ")=" + F(target.PickerX) +
                " / PickerY=teachingY(" + F(pickerYTeaching) + ")+runtimeY(" + F(runtimeY) + ")+colletY(" + F(collet.Y) + ")=" + F(target.PickerY) +
                " / PickerT=teachingT(" + F(pickerTTeaching) + ")+runtimeT(" + F(runtimeT) + ")+colletT(" + F(collet.T) + ")=" + F(target.PickerT);
            return target;
        }

        public static NeedleCalibrationData ResolveNeedle(CDT320_Machine machine)
        {
            CalibrationData data = ResolveData(machine);
            if (data == null || data.Needle == null || !data.Needle.Valid)
                return null;

            return data.Needle;
        }

        public static bool IsCameraValid(CDT320_Machine machine)
        {
            VisionCameraCalibrationData camera = ResolveCamera(machine);
            return camera != null && camera.Valid;
        }

        public static bool IsNeedleValid(CDT320_Machine machine)
        {
            return ResolveNeedle(machine) != null;
        }

        public static bool AreAllColletsValid(CDT320_Machine machine)
        {
            CalibrationData data = ResolveData(machine);
            if (data == null || data.Collet == null)
                return false;

            data.Collet.EnsureObjects();
            for (int i = 1; i <= 4; i++)
            {
                ColletCalibrationRecord front = data.Collet.GetRecord(VisionFocusPickerSide.Front, i);
                ColletCalibrationRecord rear = data.Collet.GetRecord(VisionFocusPickerSide.Rear, i);
                if (front == null || !front.Valid || rear == null || !rear.Valid)
                    return false;
            }

            return true;
        }

        public static string ResolveZonePositionName(string positionArrayName)
        {
            if (string.Equals(positionArrayName, "DieBottomPosition", StringComparison.OrdinalIgnoreCase))
                return "BottomPosition";
            if (string.Equals(positionArrayName, "DieSidePosition", StringComparison.OrdinalIgnoreCase))
                return "SidePosition";
            if (string.Equals(positionArrayName, "DiePlacePosition", StringComparison.OrdinalIgnoreCase))
                return "PlacePosition";
            return "PickPosition";
        }

        public static PickerAxis ResolvePickerZAxis(int index)
        {
            if (index <= 0) return PickerAxis.PickerZ0;
            if (index == 1) return PickerAxis.PickerZ1;
            if (index == 2) return PickerAxis.PickerZ2;
            return PickerAxis.PickerZ3;
        }

        public static PickerAxis ResolvePickerTAxis(int index)
        {
            if (index <= 0) return PickerAxis.PickerT0;
            if (index == 1) return PickerAxis.PickerT1;
            if (index == 2) return PickerAxis.PickerT2;
            return PickerAxis.PickerT3;
        }

        private static int NormalizePickerIndex(int pickerIndex)
        {
            if (pickerIndex < 0)
                return 0;
            if (pickerIndex > 3)
                return 3;
            return pickerIndex;
        }

        private static double ResolvePickerPitchXOffset(string positionArrayName, int index, double pitchX)
        {
            int pitchIndex = IsReversePickerPitchZone(positionArrayName)
                ? Math.Max(0, 3 - index)
                : index;

            return Math.Abs(pitchX) * pitchIndex;
        }

        private static bool IsReversePickerPitchZone(string positionArrayName)
        {
            return string.Equals(positionArrayName, "DieBottomPosition", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(positionArrayName, "DieSidePosition", StringComparison.OrdinalIgnoreCase);
        }

        private static string F(double value)
        {
            return value.ToString("F6");
        }
    }
}
