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

        /// <summary>
        /// [NeedleZ 왕복 제거 2026-08-06] 캘리브레이션 배치에서 뒤에 실행할 대상이 더 있을 때 true.
        /// PickUpZ 캘이 대상 종료 후 NeedleZ 를 Avoid 로 올리는 동작을 생략해
        /// 픽커4 -> 3 -> 2 전환 시 NeedleZ 가 내려갔다 올라오는 왕복을 없앤다.
        /// ★마지막 대상에서는 반드시 false 로 두어 종료 상태를 안전하게 만든다.★
        /// 기존 KeepZAfterBottomInspection / KeepZUntilSideInspectionComplete 와 같은
        /// "연속 실행 중 Z 유지" 계열 플래그다.
        /// </summary>
        public bool KeepNeedleZAtWorkForNextTarget { get; set; }

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
                KeepZUntilSideInspectionComplete = false,
                KeepNeedleZAtWorkForNextTarget = false
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
