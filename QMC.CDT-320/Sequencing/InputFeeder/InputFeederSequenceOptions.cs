using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public enum InputFeederPostUnloadMove
    {
        Avoid,
        Exchange
    }

    public sealed class InputFeederSequenceOptions
    {
        public int SlotIndex { get; set; }
        public int NextSlotIndex { get; set; }
        public CassetteMaterialRole CassetteRole { get; set; }
        public int WaferSize { get; set; }
        public int MoveTimeoutMs { get; set; }
        public bool FineMove { get; set; }
        public bool UseBarcode { get; set; }
        public bool UseVacuum { get; set; }
        public double StageLoadOffset { get; set; }
        public double StageUnloadOffset { get; set; }
        public InputFeederPostUnloadMove PostUnloadMove { get; set; }
        public bool ReturnCassetteToUnloadSlotAfterUnload { get; set; }
        public SequenceRunMode RunMode { get; set; }
        public SequenceStartMode StartMode { get; set; }

        public static InputFeederSequenceOptions Default()
        {
            return new InputFeederSequenceOptions
            {
                SlotIndex = 0,
                NextSlotIndex = 0,
                CassetteRole = CassetteMaterialRole.Input1,
                WaferSize = 12,
                MoveTimeoutMs = 10000,
                FineMove = false,
                UseBarcode = false,
                UseVacuum = true,
                StageLoadOffset = 0.0,
                StageUnloadOffset = 0.0,
                PostUnloadMove = InputFeederPostUnloadMove.Avoid,
                ReturnCassetteToUnloadSlotAfterUnload = true,
                RunMode = SequenceRunMode.Auto,
                StartMode = SequenceStartMode.Resume
            };
        }
    }
}
