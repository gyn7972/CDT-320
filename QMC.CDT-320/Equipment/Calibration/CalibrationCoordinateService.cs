namespace QMC.CDT320.Calibration
{
    public static class CalibrationCoordinateService
    {
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
    }
}
