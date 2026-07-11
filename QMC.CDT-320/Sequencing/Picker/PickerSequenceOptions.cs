namespace QMC.CDT320.Sequencing
{
    public sealed class PickerSequenceOptions
    {
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }
        public bool FineMove { get; set; }
        public int MoveTimeoutMs { get; set; }
        public int ResourceTimeoutMs { get; set; }
        public int PickerNo { get; set; }
        public int RestrictToPickerNo { get; set; }
        public int VisionRetryCount { get; set; }
        public InputDieVisionFailureAction InputDieVisionFailureAction { get; set; }
        public bool SimulateVisionResult { get; set; }
        public bool PickerMotionOnlyTestMode { get; set; }
        public bool RequireInputCameraMarkInspectionPermission { get; set; }
        public bool InputCameraPreInspectionMode { get; set; }
        public bool KeepZAfterBottomInspection { get; set; }
        public bool EnterSideFromBottomInspection { get; set; }
        public bool KeepZUntilSideInspectionComplete { get; set; }

        public static PickerSequenceOptions Default()
        {
            return new PickerSequenceOptions
            {
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume,
                FineMove = false,
                MoveTimeoutMs = 30000,
                ResourceTimeoutMs = 30000,
                PickerNo = 0,
                RestrictToPickerNo = 0,
                VisionRetryCount = 3,
                InputDieVisionFailureAction = InputDieVisionFailureAction.SkipDie,
                SimulateVisionResult = false,
                PickerMotionOnlyTestMode = false,
                RequireInputCameraMarkInspectionPermission = false,
                InputCameraPreInspectionMode = false,
                KeepZAfterBottomInspection = false,
                EnterSideFromBottomInspection = false,
                KeepZUntilSideInspectionComplete = false
            };
        }

        public void ApplyInputStageVisionPolicy(CDT320_Machine machine)
        {
            try
            {
                InputStageUnit inputStage = machine != null ? machine.InputStageUnit : null;
                InputStageConfig config = inputStage != null ? inputStage.Config : null;
                if (config == null)
                    return;

                config.EnsurePickUpMotionDefaults();
                VisionRetryCount = config.InputDieVisionRetryCount > 0 ? config.InputDieVisionRetryCount : 3;
                InputDieVisionFailureAction = config.InputDieVisionFailureAction;
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
