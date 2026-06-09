namespace QMC.CDT320.Sequencing
{
    public sealed class InputStageSequenceOptions
    {
        public bool FineMove { get; set; }
        public bool EnableMotion { get; set; }
        public bool RequireMapData { get; set; }
        public bool RequireVisionAlign { get; set; }
        public int MoveTimeoutMs { get; set; }
        public string WaferId { get; set; }
        public bool AllowFallbackMap { get; set; }
        public double AlignThetaToleranceDeg { get; set; }
        public int AlignRetryCount { get; set; }
        public string CenterAlignTargetId { get; set; }
        public string Ref1AlignTargetId { get; set; }
        public string Ref2AlignTargetId { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static InputStageSequenceOptions Default()
        {
            return new InputStageSequenceOptions
            {
                FineMove = false,
                EnableMotion = true,
                RequireMapData = false,
                RequireVisionAlign = false,
                MoveTimeoutMs = 10000,
                WaferId = "",
                AllowFallbackMap = true,
                AlignThetaToleranceDeg = 0.005,
                AlignRetryCount = 3,
                CenterAlignTargetId = "Center",
                Ref1AlignTargetId = "Ref1",
                Ref2AlignTargetId = "Ref2",
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }
}
