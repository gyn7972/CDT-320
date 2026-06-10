namespace QMC.CDT320.Sequencing
{
    public sealed class OutputCassetteSequenceOptions
    {
        public bool FineMove { get; set; }
        public bool RequireActiveLot { get; set; }
        public int RequiredCassetteSize { get; set; }
        public int MoveTimeoutMs { get; set; }
        public int GoodLevelCount { get; set; }
        public int SlotIndex { get; set; }
        public TargetCassette TargetCassette { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static OutputCassetteSequenceOptions Default()
        {
            return new OutputCassetteSequenceOptions
            {
                FineMove = false,
                RequireActiveLot = false,
                RequiredCassetteSize = 0,
                MoveTimeoutMs = 0,
                GoodLevelCount = 2,
                SlotIndex = 0,
                TargetCassette = TargetCassette.Good1,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }
}
