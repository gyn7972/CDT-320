namespace QMC.CDT320.Sequencing
{
    public sealed class InputCassetteSequenceOptions
    {
        public bool FineMove { get; set; }
        public bool RequireActiveLot { get; set; }
        public int RequiredCassetteSize { get; set; }
        public int MoveTimeoutMs { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static InputCassetteSequenceOptions Default()
        {
            return new InputCassetteSequenceOptions
            {
                FineMove = false,
                RequireActiveLot = false,
                RequiredCassetteSize = 0,
                MoveTimeoutMs = 0,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }
}
