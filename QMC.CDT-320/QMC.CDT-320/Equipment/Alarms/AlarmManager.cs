using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Alarms
{
    /// <summary>
    /// 프로세스 전역 알람 관리자.
    /// <list type="bullet">
    ///   <item><description>Raise(...) 로 알람 발생, Clear(id) / ClearAll() 로 해제</description></item>
    ///   <item><description>Active: 해제되지 않은 알람 리스트</description></item>
    ///   <item><description>History: 전체 레코드 (해제된 것 포함)</description></item>
    ///   <item><description>AlarmRaised / AlarmCleared 이벤트 발행 — UI 배너가 구독</description></item>
    /// </list>
    /// </summary>
    public static class AlarmManager
    {
        private static readonly object _lock = new object();
        private static readonly List<AlarmRecord> _all = new List<AlarmRecord>();
        private static int _seq;

        public static event Action<AlarmRecord> AlarmRaised;
        public static event Action<AlarmRecord> AlarmCleared;

        public static IReadOnlyList<AlarmRecord> Active
        {
            get { lock (_lock) return _all.Where(a => a.IsActive).ToList(); }
        }

        public static IReadOnlyList<AlarmRecord> History
        {
            get { lock (_lock) return _all.ToList(); }
        }

        public static bool HasActive
        {
            get { lock (_lock) return _all.Any(a => a.IsActive); }
        }

        public static AlarmSeverity? HighestActiveSeverity
        {
            get
            {
                lock (_lock)
                {
                    AlarmSeverity? max = null;
                    foreach (var a in _all)
                    {
                        if (!a.IsActive) continue;
                        if (max == null || a.Severity > max) max = a.Severity;
                    }
                    return max;
                }
            }
        }

        public static AlarmRecord Raise(AlarmSeverity sev, string code, string source, string message)
        {
            // Stage 19 — AlarmMaster lookup: message 가 비어있으면 정의된 Title 사용
            // Stage 23 — Lang.Current (ko/en) 적용
            if (string.IsNullOrEmpty(message))
            {
                var def = AlarmMaster.Get(code);
                if (def != null)
                {
                    string lang = "ko";
                    try { lang = QMC.CDT_320.Ui.Localization.Lang.Current ?? "ko"; } catch { }
                    message = def.GetTitle(lang);
                }
            }

            AlarmRecord rec;
            lock (_lock)
            {
                _seq++;
                rec = new AlarmRecord(_seq, sev, code, source, message);
                _all.Add(rec);
            }
            try { AlarmRaised?.Invoke(rec); } catch { }
            return rec;
        }

        public static void Clear(int id)
        {
            AlarmRecord rec;
            lock (_lock)
            {
                rec = _all.FirstOrDefault(a => a.Id == id);
                if (rec == null || !rec.IsActive) return;
                rec.Cleared = DateTime.Now;
            }
            try { AlarmCleared?.Invoke(rec); } catch { }
        }

        /// <summary>전체 활성 알람 해제.</summary>
        public static void ClearAll()
        {
            List<AlarmRecord> cleared;
            lock (_lock)
            {
                cleared = _all.Where(a => a.IsActive).ToList();
                foreach (var a in cleared) a.Cleared = DateTime.Now;
            }
            foreach (var a in cleared)
                try { AlarmCleared?.Invoke(a); } catch { }
        }

        /// <summary>테스트/개발 — 모든 기록 삭제.</summary>
        public static void ResetAll()
        {
            lock (_lock)
            {
                _all.Clear();
                _seq = 0;
            }
        }
    }
}
