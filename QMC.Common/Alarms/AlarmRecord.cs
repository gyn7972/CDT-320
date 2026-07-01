using System;

namespace QMC.Common.Alarms
{
    /// <summary>1건의 알람 레코드.</summary>
    public class AlarmRecord
    {
        public int           Id          { get; }
        public DateTime      Raised      { get; }
        public DateTime?     Cleared     { get; set; }
        public AlarmSeverity Severity    { get; }
        public string        Code        { get; }
        public string        Source      { get; }
        public string        Message     { get; }

        /// <summary>
        /// 이전 세션(프로그램 재시작 전)에서 저장돼 복원된 이력인지 여부.
        /// 복원분은 이력 표시에는 포함되지만, 설비 제어가 참조하는 Active 목록에서는 제외한다
        /// (재시작 후 옛 알람이 시퀀스/자동운전을 붙잡지 않도록).
        /// </summary>
        public bool Restored { get; }

        /// <summary>해제되지 않은(현재 걸려 있는) 알람인지. 표시/상태 구분용.</summary>
        public bool IsActive => !Cleared.HasValue;

        /// <summary>신규 발생용 — 발생 시각은 현재 시각으로 기록된다.</summary>
        public AlarmRecord(int id, AlarmSeverity sev, string code, string source, string message)
        {
            Id       = id;
            Raised   = DateTime.Now;
            Severity = sev;
            Code     = code ?? "";
            Source   = source ?? "";
            Message  = message ?? "";
            Restored = false;
        }

        /// <summary>저장본 복원용 — 발생/해제 시각을 그대로 보존한다. restored=true 면 이전 세션 이력.</summary>
        public AlarmRecord(int id, DateTime raised, DateTime? cleared, AlarmSeverity sev,
                           string code, string source, string message, bool restored)
        {
            Id       = id;
            Raised   = raised;
            Cleared  = cleared;
            Severity = sev;
            Code     = code ?? "";
            Source   = source ?? "";
            Message  = message ?? "";
            Restored = restored;
        }
    }
}
