namespace QMC.CDT320.Sequencing
{
    public sealed class OutputStageSequenceOptions
    {
        public BinSide Side { get; set; }
        public bool FineMove { get; set; }
        public int MoveTimeoutMs { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static OutputStageSequenceOptions Default()
        {
            return new OutputStageSequenceOptions
            {
                Side = BinSide.Good,
                FineMove = false,
                MoveTimeoutMs = 10000,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }
}
