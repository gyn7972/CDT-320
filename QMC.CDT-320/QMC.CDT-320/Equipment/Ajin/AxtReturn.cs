namespace QMC.CDT320.Ajin
{
    /// <summary>AXL.dll 반환 코드 (AXT_FUNC_RESULT). 성공=0.</summary>
    public static class AxtReturn
    {
        public const uint SUCCESS              = 0x0000;
        public const uint OPEN_ERROR           = 0x1001;
        public const uint OPEN_ALREADY         = 0x1002;
        public const uint NOT_OPEN             = 0x1053;
        public const uint NOT_SUPPORT_VERSION  = 0x1054;
        public const uint LOCK_FILE_MISMATCH   = 0x1055;
        public const uint INVALID_AXIS_NO      = 0x4001;
        public const uint INVALID_TYPE         = 0x4054;
        public const uint INVALID_BIT_NO       = 0x7000;
        public const uint INVALID_MODULE_NO    = 0x7001;

        public static bool IsSuccess(uint r) => r == SUCCESS;
    }

    /// <summary>AxmSignalServoOn 의 USE 플래그.</summary>
    public static class AxtServo
    {
        public const int OFF = 0;
        public const int ON  = 1;
    }
}
