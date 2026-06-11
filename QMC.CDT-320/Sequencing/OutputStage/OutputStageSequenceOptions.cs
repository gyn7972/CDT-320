namespace QMC.CDT320.Sequencing
{
    public sealed class OutputStageSequenceOptions
    {
        public BinSide Side { get; set; }
        public DieGrade Grade { get; set; }
        public double TpuOffsetX { get; set; }
        public double TpuOffsetY { get; set; }
        public double VisionOffsetX { get; set; }
        public double VisionOffsetY { get; set; }
        public bool FineMove { get; set; }
        public int MoveTimeoutMs { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static OutputStageSequenceOptions Default()
        {
            return new OutputStageSequenceOptions
            {
                Side = BinSide.Good,
                Grade = DieGrade.Good,
                TpuOffsetX = 0.0,
                TpuOffsetY = 0.0,
                VisionOffsetX = 0.0,
                VisionOffsetY = 0.0,
                FineMove = false,
                MoveTimeoutMs = 10000,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }
}
