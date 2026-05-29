using System.Collections.Generic;

namespace QMC.CDT320
{
    public enum TransferMode
    {
        Load,
        Unload,
        Mapping,
        Manual
    }

    public enum TransferPointType
    {
        Cassette,
        Stage,
        Picker
    }

    public enum SlotPresence
    {
        Unknown,
        Empty,
        Exist
    }

    public enum ProcessState
    {
        Unknown,
        Ready,
        Processing,
        Done,
        Ng
    }

    public enum WaferCassetteAlarmCode
    {
        None,
        CassetteMissing,
        SizeMismatch,
        ProtrusionDetected,
        MappingTimeout,
        MoveTimeout,
        TeachingMissing
    }

    public enum WaferFeederProcessState
    {
        Empty,
        HasWafer,
        Moving,
        Alarm
    }

    public sealed class WaferSlotState
    {
        public SlotPresence Presence { get; set; }
        public ProcessState Process { get; set; }
    }

    public sealed class WaferCassetteSensorState
    {
        public bool Wafer8CassetteCheck0 { get; set; }
        public bool Wafer8CassetteCheck1 { get; set; }
        public bool Wafer12CassetteCheck0 { get; set; }
        public bool Wafer12CassetteCheck1 { get; set; }
        public bool WaferRingJutCheck { get; set; }
        public bool WaferMapping { get; set; }
        public bool IsCassetteExist { get; set; }
        public bool IsSizeMatched { get; set; }
    }

    public sealed class WaferCassetteMaterial
    {
        public WaferCassetteMaterial(int maxSlots)
        {
            MaxSlots = maxSlots;
            Slots = new List<WaferSlotState>();
        }

        public int MaxSlots { get; private set; }
        public List<WaferSlotState> Slots { get; private set; }
    }
}
