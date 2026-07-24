using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Data.Store;

namespace QMC.Common.Alarms
{
    /// <summary>
    /// 프로세스 전역 알람 관리자.
    /// <list type="bullet">
    ///   <item><description>Raise(...) 로 알람 발생, Clear(id) / ClearAll() 로 해제</description></item>
    ///   <item><description>Active: 해제되지 않은 알람 리스트(복원 이력 제외 — 설비 제어가 참조)</description></item>
    ///   <item><description>History: 전체 레코드 (해제/복원 포함) — 이력 화면이 참조</description></item>
    ///   <item><description>AlarmRaised / AlarmCleared 이벤트 발행 — UI 배너가 구독</description></item>
    /// </list>
    /// <para>
    /// 발생/해제 시 오늘자 파일(Log\Alarms\alarm_yyyy-MM-dd.json)에 상태 전체를 저장하고,
    /// 시작 시 오늘자 파일을 읽어 이력을 복원한다(복원분은 Restored=true → Active 에서 제외).
    /// 저장은 백그라운드 디바운스로 묶어 알람이 몰려도 파일 쓰기 부하를 제한한다.
    /// </para>
    /// </summary>
    public static class AlarmManager
    {
        // 저장 디바운스(ms) — 알람이 연속 발생해도 이 간격으로 묶어 1회만 저장한다.
        private const int SaveDebounceMs = 500;

        private static readonly object _lock = new object();
        private static readonly List<AlarmRecord> _all = new List<AlarmRecord>();
        private static int _seq;

        // 저장 대기(변경됨) / 저장 예약 상태 플래그.
        private static volatile bool _dirty;
        private static volatile bool _saveScheduled;

        public static event Action<AlarmRecord> AlarmRaised;
        public static event Action<AlarmRecord> AlarmCleared;
        public static Func<string> LanguageProvider { get; set; }

        static AlarmManager()
        {
            LoadToday();
        }

        // 알람 저장 폴더 (예: D:\CDT-320\Log\Alarms).
        public static string Dir
        {
            get { return Path.Combine(QMC.Common.Logging.EventLogger.LogRoot, "Alarms"); }
        }

        // 설비 제어가 참조하는 활성 알람 — 복원된 이전 세션 이력은 제외한다.
        public static IReadOnlyList<AlarmRecord> Active
        {
            get { lock (_lock) return _all.Where(a => a.IsActive && !a.Restored).ToList(); }
        }

        // 이력 화면용 — 복원/해제 포함 전체.
        public static IReadOnlyList<AlarmRecord> History
        {
            get { lock (_lock) return _all.ToList(); }
        }

        public static bool HasActive
        {
            get { lock (_lock) return _all.Any(a => a.IsActive && !a.Restored); }
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
                        if (!a.IsActive || a.Restored) continue;
                        if (max == null || a.Severity > max) max = a.Severity;
                    }
                    return max;
                }
            }
        }

        public static AlarmRecord Raise(AlarmSeverity sev, string code, string source, string message)
        {
            sev = NormalizeSeverity(sev, code, message);

            // Stage 19 — AlarmMaster lookup: message 가 비어있으면 정의된 Title 사용
            // Stage 23 — Lang.Current (ko/en) 적용
            if (string.IsNullOrEmpty(message))
            {
                var def = AlarmMaster.Get(code);
                if (def != null)
                {
                    string lang = GetLanguage();
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
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "QMC", rec.Code, rec.Source, "[" + rec.Severity + "] " + rec.Message); } catch { }
            // 알람 블랙박스 덤프: 직전 20~30초 이벤트 + 장비 스냅샷을 AlarmContext 파일로 1회 저장한다.
            try { QMC.Common.Logging.LogPolicy.DumpAlarmContext(rec.Code, rec.Severity.ToString(), rec.Source, rec.Message); } catch { }
            RequestSave();
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
            // 알람 해제 상관 기록은 최소 로그 정책에서도 항상 영구 저장한다(Audit).
            try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "QMC", rec.Code, rec.Source, "[CLEARED] " + rec.Message, QMC.Common.Logging.LogSeverity.Audit); } catch { }
            RequestSave();
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
            {
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "QMC", a.Code, a.Source, "[CLEARED] " + a.Message, QMC.Common.Logging.LogSeverity.Audit); } catch { }
                try { AlarmCleared?.Invoke(a); } catch { }
            }
            if (cleared.Count > 0) RequestSave();
        }

        /// <summary>테스트/개발 — 모든 기록 삭제.</summary>
        public static void ResetAll()
        {
            lock (_lock)
            {
                _all.Clear();
                _seq = 0;
            }
            RequestSave();
        }

        // --- 저장 / 복원 ---

        // 변경 시 저장을 예약한다. 연속 호출은 디바운스로 묶어 1회만 파일에 쓴다.
        private static void RequestSave()
        {
            try
            {
                _dirty = true;
                if (_saveScheduled) return;
                _saveScheduled = true;

                Task.Run(() =>
                {
                    try { Thread.Sleep(SaveDebounceMs); }
                    catch { }
                    _saveScheduled = false;
                    if (_dirty) SaveSnapshot();
                });
            }
            catch
            {
                // 저장 예약 실패는 다음 변경에서 다시 시도된다(_dirty 유지).
            }
        }

        // 오늘 발생한 알람 전체를 오늘자 파일에 저장한다(백그라운드 스레드에서만 호출).
        private static void SaveSnapshot()
        {
            try
            {
                _dirty = false;

                DateTime today = DateTime.Today;
                List<AlarmSaveDto> dtos;
                lock (_lock)
                {
                    dtos = _all.Where(a => a.Raised.Date == today)
                               .Select(AlarmSaveDto.From)
                               .ToList();
                }

                Directory.CreateDirectory(Dir);
                string path = FilePath(today);
                using (var fs = File.Create(path))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(List<AlarmSaveDto>), dtos);
                }
            }
            catch
            {
                // 저장 실패 시 다음 변경에서 재시도되도록 dirty 를 되돌린다.
                _dirty = true;
            }
        }

        // 시작 시 오늘자 파일을 읽어 이력을 복원한다(복원분은 Restored=true → Active 제외).
        private static void LoadToday()
        {
            try
            {
                string path = FilePath(DateTime.Today);
                if (!File.Exists(path)) return;

                List<AlarmSaveDto> dtos;
                using (var fs = File.OpenRead(path))
                {
                    var ser = new DataContractJsonSerializer(typeof(List<AlarmSaveDto>));
                    dtos = ser.ReadObject(fs) as List<AlarmSaveDto>;
                }
                if (dtos == null) return;

                lock (_lock)
                {
                    foreach (var d in dtos)
                    {
                        if (d == null) continue;
                        var rec = d.ToRecord(restored: true);
                        _all.Add(rec);
                        if (rec.Id > _seq) _seq = rec.Id;   // 다음 발생 Id 가 겹치지 않도록
                    }
                }
            }
            catch
            {
                // 복원 실패는 치명적이지 않다(이력만 비어 있고, 신규 알람은 정상 동작).
            }
        }

        private static string FilePath(DateTime date)
        {
            return Path.Combine(Dir, "alarm_" + date.ToString("yyyy-MM-dd") + ".json");
        }

        private static string GetLanguage()
        {
            try
            {
                if (LanguageProvider != null)
                    return LanguageProvider() ?? "ko";

                return "ko";
            }
            catch
            {
                return "ko";
            }
            finally
            {
            }
        }

        private static AlarmSeverity NormalizeSeverity(AlarmSeverity severity, string code, string message)
        {
            try
            {
                if (severity == AlarmSeverity.Critical)
                    return severity;

                if (IsCriticalMotionOrInterlockAlarm(code, message))
                    return AlarmSeverity.Critical;

                return severity;
            }
            catch
            {
                return severity;
            }
            finally
            {
            }
        }

        private static bool IsCriticalMotionOrInterlockAlarm(string code, string message)
        {
            try
            {
                code = code ?? string.Empty;
                message = message ?? string.Empty;

                if (IsExactOrPrefix(code, "E-STOP") ||
                    Contains(code, "INTERLOCK") ||
                    Contains(code, "LIMIT"))
                    return true;

                if (code.StartsWith("AX-MOVE", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("AX-HOME", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("AX-JOG", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("AX-SOFT-LIMIT", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("LIMIT-", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (Contains(code, "MOVE") &&
                    (Contains(message, "alarm=True") ||
                     Contains(message, "alarm=ON") ||
                     Contains(message, "알람=ON") ||
                     Contains(message, "Axis alarm is ON") ||
                     Contains(message, "축 알람이 ON")))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsExactOrPrefix(string value, string token)
        {
            value = value ?? string.Empty;
            token = token ?? string.Empty;
            return value.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith(token + "-", StringComparison.OrdinalIgnoreCase);
        }

        private static bool Contains(string value, string text)
        {
            return (value ?? string.Empty).IndexOf(text ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // 알람 레코드 저장/복원용 DTO(직렬화 위해 get/set 프로퍼티로 구성).
        [DataContract]
        internal class AlarmSaveDto
        {
            [DataMember] public int           Id       { get; set; }
            [DataMember] public DateTime      Raised   { get; set; }
            [DataMember] public DateTime?     Cleared  { get; set; }
            [DataMember] public AlarmSeverity Severity { get; set; }
            [DataMember] public string        Code     { get; set; }
            [DataMember] public string        Source   { get; set; }
            [DataMember] public string        Message  { get; set; }

            public static AlarmSaveDto From(AlarmRecord a)
            {
                return new AlarmSaveDto
                {
                    Id = a.Id,
                    Raised = a.Raised,
                    Cleared = a.Cleared,
                    Severity = a.Severity,
                    Code = a.Code,
                    Source = a.Source,
                    Message = a.Message
                };
            }

            public AlarmRecord ToRecord(bool restored)
            {
                return new AlarmRecord(Id, Raised, Cleared, Severity, Code ?? "", Source ?? "", Message ?? "", restored);
            }
        }
    }
}
