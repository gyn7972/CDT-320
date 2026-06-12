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
        public int VisionRetryCount { get; set; }
        public bool SimulateVisionResult { get; set; }

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
                VisionRetryCount = 3,
                SimulateVisionResult = false
            };
        }
    }
}
