using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace QMC.Common.Logging
{
    /// <summary>로그 운영 모드. ProductionMinimal이 기본이며, DiagnosticVerbose는 제한 시간 후 자동 복귀한다.</summary>
    public enum LogMode
    {
        ProductionMinimal = 0,
        DiagnosticVerbose = 1
    }

    /// <summary>문자열 접미사(- Ok / - Failed)가 아닌 명시적 중요도. 신규 핵심 경로는 이 타입을 사용한다.</summary>
    public enum LogSeverity
    {
        /// <summary>정상 상세 로그. ProductionMinimal에서는 디스크에 저장하지 않고 블랙박스에만 보관한다.</summary>
        Normal = 0,
        /// <summary>운영 감사(모드 변경, Auto Start/Stop 등). 항상 저장한다.</summary>
        Audit = 1,
        /// <summary>실패/정지/취소/인터락 차단. 항상 저장하며 드롭이 금지된다.</summary>
        Failure = 2
    }

    /// <summary>
    /// 최소 로그 정책과 알람 블랙박스(메모리 순환 버퍼)의 중앙 관리자.<br/>
    /// - 정상 반복 로그는 디스크에 쓰지 않고 최근 이벤트만 메모리에 보관한다.<br/>
    /// - Alarm/Warning/Failure/Audit은 항상 영구 저장 대상으로 판정한다.<br/>
    /// - 알람 발생 시 블랙박스 내용을 AlarmContext 파일로 1회 덤프한다.
    /// </summary>
    public static class LogPolicy
    {
        // 코드 접두사 allowlist: 운영 감사성 이벤트는 ProductionMinimal에서도 저장한다.
        private static readonly string[] DefaultPersistCodePrefixes =
        {
            "AUTO", "START", "STOP", "CYCLE", "EMERGENCY", "EMO",
            "LOGIN", "LOGOUT", "USER", "RECIPE", "SETTING", "LOT", "MODE", "INIT"
        };

        private static readonly object SyncRoot = new object();
        private static readonly Queue<EventRow> Blackbox = new Queue<EventRow>();
#if !DEBUG
        // RELEASE 전용 모드 상태 머신. DEBUG는 강제 활성이라 이 상태를 읽지 않으므로
        // 미사용 필드 경고(CS0414)를 만들지 않도록 구성별로 분리한다. RELEASE 코드는 무변경.
        private static LogMode _mode = LogMode.ProductionMinimal;
        private static DateTime _diagnosticExpireAt = DateTime.MinValue;
        private static bool _modeAuditInProgress;
#endif
        private static int _blackboxCapacity = 20000;
        private static int _blackboxWindowSeconds = 30;
        private static string[] _persistCodePrefixes = DefaultPersistCodePrefixes;
        private static long _droppedDiagnosticRows;
        private static long _suppressedAlarmContextDumps;
        private static DateTime _lastAlarmContextDumpAt = DateTime.MinValue;

#if DEBUG
        /// <summary>
        /// DEBUG 빌드 강제 활성화 플래그.
        /// 기존 조건: 빌드 구성과 무관하게 ProductionMinimal로 시작하고, 설정 또는 UI로만
        ///           DiagnosticVerbose를 켰으며 제한 시간 후 자동 복귀했다.
        /// 현재 기준(사용자 지시 2026-07-25): DEBUG 빌드는 프로그램 시작과 동시에 진단 상세 로그를
        ///           활성화하고 만료·해제되지 않는다.
        /// </summary>
        private const bool ForcedDiagnosticVerbose = true;
#else
        // [팀장님 지시 2026-08-28] 프로그램 안정화 전까지 RELEASE 빌드도 시작과 동시에 진단 상세
        // 로그를 강제 활성화한다("명시적 명령이 있을 때까지는 무조건 시작과 동시에 ENABLE" —
        // 로그 없이는 디버깅 불가). 만료·해제 없음(UI ENABLE/DISABLE·제한 시간보다 우선).
        // 되돌릴 때는 팀장님 명시 지시 후 이 값만 false로 원복하면 기존 동작(ProductionMinimal
        // 시작 + 사용자 조작/제한 시간 만료)으로 복귀한다.
        private const bool ForcedDiagnosticVerbose = true;
#endif

        /// <summary>DEBUG 빌드 강제 활성화 여부. UI가 조작 가능 여부를 판단하는 데 사용한다.</summary>
        public static bool IsDiagnosticVerboseForcedByBuild
        {
            get { return ForcedDiagnosticVerbose; }
        }

        /// <summary>AlarmContext 덤프 최소 간격. 동일 시점 다발 알람으로 파일이 폭증하지 않게 한다.</summary>
        private static readonly TimeSpan AlarmContextMinInterval = TimeSpan.FromSeconds(1);

        /// <summary>알람 시점 장비 스냅샷(축/리소스/상태) 제공자. MachineController가 시작 시 등록한다.</summary>
        public static Func<string> EquipmentSnapshotProvider { get; set; }

        /// <summary>AlarmContext 저장 실패 상태 노출용. true면 마지막 덤프가 실패했다.</summary>
        public static bool LastAlarmContextDumpFailed { get; private set; }

        public static long DroppedDiagnosticRows { get { return Interlocked.Read(ref _droppedDiagnosticRows); } }

        public static LogMode Mode
        {
            get
            {
#if DEBUG
                // DEBUG 빌드는 항상 상세 모드다. 만료 판정/자동 복귀를 수행하지 않는다.
                return LogMode.DiagnosticVerbose;
#else
                lock (SyncRoot)
                {
                    if (_mode == LogMode.DiagnosticVerbose && DateTime.Now >= _diagnosticExpireAt)
                        RevertToProductionMinimalNoLock("DiagnosticVerbose 제한 시간 만료");
                    return _mode;
                }
#endif
            }
        }

        public static bool IsDiagnosticVerbose
        {
            get { return ForcedDiagnosticVerbose || Mode == LogMode.DiagnosticVerbose || _verboseScopeCount > 0; }
        }

        /// <summary>진단 상세 모드의 남은 시간. 비활성(최소 모드)이면 TimeSpan.Zero. UI 표시용.</summary>
        public static TimeSpan DiagnosticVerboseRemaining
        {
            get
            {
#if DEBUG
                // DEBUG 빌드는 만료가 없다. UI는 IsDiagnosticVerboseForcedByBuild로 분기한다.
                return TimeSpan.MaxValue;
#else
                lock (SyncRoot)
                {
                    if (_mode != LogMode.DiagnosticVerbose)
                        return TimeSpan.Zero;
                    TimeSpan remain = _diagnosticExpireAt - DateTime.Now;
                    return remain > TimeSpan.Zero ? remain : TimeSpan.Zero;
                }
#endif
            }
        }

        private static int _verboseScopeCount;

        /// <summary>
        /// 스코프가 살아있는 동안 모든 로그를 영속한다(캘리브레이션 등 검증이 필요한 저빈도 작업용).
        /// ref-count 방식이라 중첩 안전. Dispose 시 원복.
        /// </summary>
        public static IDisposable BeginVerboseScope(string reason)
        {
            System.Threading.Interlocked.Increment(ref _verboseScopeCount);
            return new VerboseScopeToken();
        }

        private sealed class VerboseScopeToken : IDisposable
        {
            private int _disposed;
            public void Dispose()
            {
                if (System.Threading.Interlocked.Exchange(ref _disposed, 1) == 0)
                    System.Threading.Interlocked.Decrement(ref _verboseScopeCount);
            }
        }

        private static int _forcedVerboseAuditWritten;

        /// <summary>시작 시(설정 로드 후) 정책 파라미터를 적용한다.</summary>
        public static void Configure(int blackboxCapacity, int blackboxWindowSeconds, string persistCodePrefixesCsv)
        {
            lock (SyncRoot)
            {
                if (blackboxCapacity >= 1000 && blackboxCapacity <= 200000)
                    _blackboxCapacity = blackboxCapacity;
                if (blackboxWindowSeconds >= 5 && blackboxWindowSeconds <= 600)
                    _blackboxWindowSeconds = blackboxWindowSeconds;

                if (!string.IsNullOrWhiteSpace(persistCodePrefixesCsv))
                {
                    List<string> prefixes = new List<string>();
                    foreach (string token in persistCodePrefixesCsv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string trimmed = token.Trim();
                        if (trimmed.Length > 0)
                            prefixes.Add(trimmed);
                    }
                    if (prefixes.Count > 0)
                        _persistCodePrefixes = prefixes.ToArray();
                }
            }

            // 강제 활성화 사실을 시작 시 1회만 남긴다. 로그 실패가 시작 경로를 막지 않도록 방어한다.
            try
            {
                if (IsDiagnosticVerboseForcedByBuild &&
                    System.Threading.Interlocked.Exchange(ref _forcedVerboseAuditWritten, 1) == 0)
                {
                    WriteModeAudit("SYSTEM",
                        "진단 상세 로그(DiagnosticVerbose)를 프로그램 시작과 동시에 강제 활성화했습니다(빌드 강제, 해제 불가 — 팀장님 지시 2026-08-28 안정화 전 상시 활성).");
                }
            }
            catch
            {
            }
        }

        /// <summary>진단 상세 모드를 제한 시간으로 활성화한다. 모드 변경 자체를 Audit으로 남긴다.
        /// DEBUG 빌드에서는 시작 시부터 강제 활성 상태이므로 요청을 무시하고 감사 기록만 남긴다
        /// (_mode를 건드리지 않아 상태 불일치를 만들지 않는다).</summary>
        public static void EnableDiagnosticVerbose(int minutes, string user)
        {
#if DEBUG
            WriteModeAudit(user, "DEBUG 빌드는 진단 상세 로그가 시작 시부터 강제 활성화되어 있습니다. 활성화 요청을 무시합니다.");
#else
            if (minutes < 1) minutes = 1;
            if (minutes > 24 * 60) minutes = 24 * 60;
            lock (SyncRoot)
            {
                _mode = LogMode.DiagnosticVerbose;
                _diagnosticExpireAt = DateTime.Now.AddMinutes(minutes);
            }
            WriteModeAudit(user, "DiagnosticVerbose 활성화. 만료=" + minutes + "분 후");
#endif
        }

        public static void DisableDiagnosticVerbose(string user)
        {
#if DEBUG
            WriteModeAudit(user, "DEBUG 빌드는 진단 상세 로그를 해제할 수 없습니다. 해제 요청을 무시합니다.");
#else
            lock (SyncRoot)
            {
                if (_mode == LogMode.ProductionMinimal)
                    return;
                _mode = LogMode.ProductionMinimal;
                _diagnosticExpireAt = DateTime.MinValue;
            }
            WriteModeAudit(user, "DiagnosticVerbose 해제. ProductionMinimal 복귀");
#endif
        }

#if !DEBUG
        private static void RevertToProductionMinimalNoLock(string reason)
        {
            _mode = LogMode.ProductionMinimal;
            _diagnosticExpireAt = DateTime.MinValue;
            // 만료 자동 복귀도 감사 기록을 남긴다(재진입 방지 플래그로 순환 호출 차단).
            if (!_modeAuditInProgress)
            {
                _modeAuditInProgress = true;
                try { WriteModeAudit("SYSTEM", reason + ". ProductionMinimal 자동 복귀"); }
                finally { _modeAuditInProgress = false; }
            }
        }
#endif

        private static void WriteModeAudit(string user, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Event, user ?? "SYSTEM", "LOG-MODE", "LogPolicy", message, LogSeverity.Audit);
            }
            catch
            {
            }
        }

        /// <summary>영구 저장 여부 판정. Alarm/Warning/Data/Work 종류와 Failure/Audit 중요도는 항상 저장한다.</summary>
        public static bool ShouldPersist(EventRow row, LogSeverity severity)
        {
            try
            {
                if (row == null)
                    return false;
                if (severity >= LogSeverity.Audit)
                    return true;
                if (row.Kind == EventKind.Alarm || row.Kind == EventKind.Warning ||
                    row.Kind == EventKind.Data || row.Kind == EventKind.Work)
                    return true;
                if (IsDiagnosticVerbose)
                    return true;

                string code = row.Code;
                if (!string.IsNullOrEmpty(code))
                {
                    string[] prefixes = _persistCodePrefixes;
                    for (int i = 0; i < prefixes.Length; i++)
                    {
                        if (code.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }

                return false;
            }
            catch
            {
                // 판정 실패 시 안전측(저장)으로 처리해 중요 로그 유실을 막는다.
                return true;
            }
        }

        /// <summary>모든 로그 행을 알람 분석용 블랙박스에 보관한다(디스크 미기록 행 포함).</summary>
        public static void CaptureBlackbox(EventRow row)
        {
            if (row == null)
                return;
            try
            {
                lock (SyncRoot)
                {
                    Blackbox.Enqueue(row);
                    while (Blackbox.Count > _blackboxCapacity)
                        Blackbox.Dequeue();
                }
            }
            catch
            {
            }
        }

        /// <summary>Log.Write(레거시 Main 등) 경로의 정상 로그를 블랙박스에 캡처한다.</summary>
        public static void CaptureLegacy(string classification, string source, string message)
        {
            try
            {
                CaptureBlackbox(new EventRow
                {
                    When = DateTime.Now,
                    Kind = EventKind.Event,
                    User = "SYSTEM",
                    Code = classification ?? string.Empty,
                    Source = source ?? string.Empty,
                    Description = message ?? string.Empty
                });
            }
            catch
            {
            }
        }

        public static void IncrementDroppedDiagnostic()
        {
            Interlocked.Increment(ref _droppedDiagnosticRows);
        }

        /// <summary>
        /// 알람 발생 시 블랙박스 최근 이벤트와 장비 스냅샷을 AlarmContext 파일로 1회 저장한다.<br/>
        /// 최소 간격(1초) 안의 연속 알람은 파일 폭증 방지를 위해 억제하고 억제 건수를 기록한다.
        /// </summary>
        public static void DumpAlarmContext(string alarmCode, string severity, string source, string message)
        {
            try
            {
                DateTime now = DateTime.Now;
                EventRow[] snapshot;
                long dropped;
                long suppressed;
                lock (SyncRoot)
                {
                    if (now - _lastAlarmContextDumpAt < AlarmContextMinInterval)
                    {
                        _suppressedAlarmContextDumps++;
                        return;
                    }
                    _lastAlarmContextDumpAt = now;
                    suppressed = _suppressedAlarmContextDumps;
                    _suppressedAlarmContextDumps = 0;

                    // 시간창(기본 30초) 밖의 오래된 행은 덤프에서 제외한다(보관 자체는 건수 상한으로 관리).
                    DateTime cutoff = now.AddSeconds(-_blackboxWindowSeconds);
                    List<EventRow> rows = new List<EventRow>(Blackbox.Count);
                    foreach (EventRow row in Blackbox)
                    {
                        if (row != null && row.When >= cutoff)
                            rows.Add(row);
                    }
                    snapshot = rows.ToArray();
                }
                dropped = DroppedDiagnosticRows;

                string dir = Path.Combine(EventLogger.LogRoot, "AlarmContext");
                Directory.CreateDirectory(dir);
                string safeCode = SanitizeFileToken(alarmCode);
                string path = Path.Combine(dir, now.ToString("yyyyMMdd_HHmmss_fff") + "_" + safeCode + ".log");

                StringBuilder sb = new StringBuilder(64 * 1024);
                sb.AppendLine("=== ALARM CONTEXT ===");
                sb.AppendLine("RaisedAt   : " + now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.AppendLine("AlarmCode  : " + (alarmCode ?? string.Empty));
                sb.AppendLine("Severity   : " + (severity ?? string.Empty));
                sb.AppendLine("Source     : " + (source ?? string.Empty));
                sb.AppendLine("Message    : " + (message ?? string.Empty));
                sb.AppendLine("LogMode    : " + Mode);
                sb.AppendLine("DroppedDiagnosticRows(total) : " + dropped);
                sb.AppendLine("SuppressedContextDumps       : " + suppressed);
                sb.AppendLine();

                sb.AppendLine("=== EQUIPMENT SNAPSHOT ===");
                Func<string> provider = EquipmentSnapshotProvider;
                if (provider != null)
                {
                    try { sb.AppendLine(provider() ?? "(empty)"); }
                    catch (Exception ex) { sb.AppendLine("snapshot provider failed: " + ex.Message); }
                }
                else
                {
                    sb.AppendLine("(no provider registered)");
                }
                sb.AppendLine();

                sb.AppendLine("=== RECENT EVENTS (last " + _blackboxWindowSeconds + "s, " + snapshot.Length + " rows) ===");
                foreach (EventRow row in snapshot)
                {
                    sb.Append(row.When.ToString("HH:mm:ss.fff")).Append(' ')
                      .Append(row.Kind).Append(' ')
                      .Append(row.Code ?? string.Empty).Append(' ')
                      .Append(row.Source ?? string.Empty).Append(" | ")
                      .AppendLine(row.Description ?? string.Empty);
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                LastAlarmContextDumpFailed = false;
            }
            catch
            {
                // 덤프 실패는 상태로 노출하고(비상 경로), 알람 처리 자체를 막지 않는다.
                LastAlarmContextDumpFailed = true;
            }
        }

        private static string SanitizeFileToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "ALARM";
            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char ch in value)
                sb.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_');
            return sb.Length > 60 ? sb.ToString(0, 60) : sb.ToString();
        }
    }
}
