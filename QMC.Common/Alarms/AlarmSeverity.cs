namespace QMC.Common.Alarms
{
    /// <summary>알람 심각도.</summary>
    public enum AlarmSeverity
    {
        /// <summary>경고 — 운전 계속 가능.</summary>
        Warning,

        /// <summary>오류 — 현재 동작 중단 필요.</summary>
        Error,

        /// <summary>치명적 — 전체 정지.</summary>
        Critical
    }
}
