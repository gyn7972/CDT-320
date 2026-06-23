namespace QMC.CDT320.Sequencing
{
    internal sealed class InputCameraPreInspectionWaitResult
    {
        public InputCameraPreInspectionWaitStatus Status { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; }

        public static InputCameraPreInspectionWaitResult PermissionReady()
        {
            return new InputCameraPreInspectionWaitResult
            {
                Status = InputCameraPreInspectionWaitStatus.PermissionReady,
                ResultCode = 0,
                Message = "permission ready"
            };
        }

        public static InputCameraPreInspectionWaitResult NoTarget()
        {
            return new InputCameraPreInspectionWaitResult
            {
                Status = InputCameraPreInspectionWaitStatus.NoTarget,
                ResultCode = 0,
                Message = "no inspected target"
            };
        }

        public static InputCameraPreInspectionWaitResult Failed(int resultCode, string message)
        {
            return new InputCameraPreInspectionWaitResult
            {
                Status = InputCameraPreInspectionWaitStatus.Failed,
                ResultCode = resultCode != 0 ? resultCode : -1,
                Message = message ?? string.Empty
            };
        }
    }
}
