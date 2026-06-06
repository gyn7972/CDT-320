namespace QMC.CDT320
{
    public enum EquipmentStatus
    {
        Idle = 0,
        Initializing = 1,
        Ready = 2,
        ManualRunning = 3,
        AutoRunning = 4,
        Stopped = 5,
        CycleStopped = 6,
        Completed = 7,
        Alarm = 8,

        Running = ManualRunning,
        Cycling = AutoRunning
    }
}
