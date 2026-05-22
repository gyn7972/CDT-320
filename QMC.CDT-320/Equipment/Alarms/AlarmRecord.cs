using System;

namespace QMC.CDT320.Alarms
{
    /// <summary>1건의 알람 레코드 (불변).</summary>
    public class AlarmRecord
    {
        public int           Id          { get; }
        public DateTime      Raised      { get; }
        public DateTime?     Cleared     { get; set; }
        public AlarmSeverity Severity    { get; }
        public string        Code        { get; }
        public string        Source      { get; }
        public string        Message     { get; }

        public bool IsActive => !Cleared.HasValue;

        public AlarmRecord(int id, AlarmSeverity sev, string code, string source, string message)
        {
            Id       = id;
            Raised   = DateTime.Now;
            Severity = sev;
            Code     = code ?? "";
            Source   = source ?? "";
            Message  = message ?? "";
        }
    }
}
