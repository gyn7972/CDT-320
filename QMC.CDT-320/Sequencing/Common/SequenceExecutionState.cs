using System;

namespace QMC.CDT320.Sequencing
{
    public enum SequenceStopKind
    {
        None = 0,
        Stop = 1,
        CycleStop = 2,
        Alarm = 3
    }

    public sealed class SequenceExecutionState
    {
        public SequenceExecutionState(string sequenceName)
        {
            SequenceName = sequenceName ?? "";
            CurrentStep = "";
            ResumeStep = "";
            LastCompletedStep = "";
            StopReason = "";
            Status = EquipmentStatus.Idle;
            Updated = DateTime.Now;
        }

        public string SequenceName { get; private set; }
        public string CurrentStep { get; set; }
        public string ResumeStep { get; set; }
        public string LastCompletedStep { get; set; }
        public EquipmentStatus Status { get; set; }
        public SequenceStopKind StopKind { get; set; }
        public string StopReason { get; set; }
        public DateTime Updated { get; set; }

        public bool CanResume
        {
            get
            {
                return (Status == EquipmentStatus.Alarm ||
                        Status == EquipmentStatus.CycleStopped) &&
                       !string.IsNullOrWhiteSpace(ResumeStep);
            }
        }

        public void Touch()
        {
            Updated = DateTime.Now;
        }
    }
}
