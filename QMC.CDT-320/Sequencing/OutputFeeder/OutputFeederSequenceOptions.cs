using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public sealed class OutputFeederSequenceOptions
    {
        public int SlotIndex { get; set; }
        public int NextSlotIndex { get; set; }
        public BinSide Side { get; set; }
        public CassetteMaterialRole CassetteRole { get; set; }
        public int MoveTimeoutMs { get; set; }
        public bool FineMove { get; set; }
        public bool UseBarcode { get; set; }
        public bool UseVacuum { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static OutputFeederSequenceOptions Default()
        {
            return new OutputFeederSequenceOptions
            {
                SlotIndex = 0,
                NextSlotIndex = 0,
                Side = BinSide.Good,
                CassetteRole = CassetteMaterialRole.Good1,
                MoveTimeoutMs = 10000,
                FineMove = false,
                UseBarcode = false,
                UseVacuum = true,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }
}
