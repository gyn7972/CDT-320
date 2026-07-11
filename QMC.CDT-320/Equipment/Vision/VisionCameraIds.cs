namespace QMC.CDT320.VisionComm
{
    /// <summary>
    /// Handler-side SSOT for simulator camera ids.
    /// These ids are sent to CDT320Simulator through SimulatorBridge CAMERA_FLASH.
    /// They are not Vision PC module names. Use VisionModuleNames for TCP module names.
    /// </summary>
    public static class VisionCameraIds
    {
        public const string Wafer = "WAFER";
        public const string Bin = "BIN";
        public const string Bottom = "BOTTOM";
        public const string Side1 = "SIDE1";
        public const string Side2 = "SIDE2";
    }
}
