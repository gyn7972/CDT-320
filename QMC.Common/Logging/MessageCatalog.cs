using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QMC.Common.Logging
{
    /// <summary>
    /// 코드/메시지 문구(한/영) 카탈로그.
    /// <list type="bullet">
    ///   <item><description>기본 문구는 코드(CreateDefaults)에 내장 — 파일이 없어도 동작.</description></item>
    ///   <item><description>화면에서 편집/저장하면 그때만 Config\Messages\message_catalog.csv 로 기록.</description></item>
    ///   <item><description>실시간으로 발생하는 메시지 종류는 EnsureRegistered 로 자동 등록(중복 종류는 1개만).</description></item>
    ///   <item><description>Resolve(): 카탈로그 우선, 알람이면 AlarmMaster 보조, 없으면 호출자 문구.</description></item>
    /// </list>
    /// 저장은 <b>목록(List)</b> 형태라 같은 코드의 행이 여러 개여도 모두 보존된다.
    /// (코드+디스크립션이 모두 같은 완전 중복만 제거)
    /// <para>
    /// 로그 기록 스레드(EnsureRegistered)와 UI 스레드(Items/Resolve/ReplaceAll)가 동시에 접근하므로
    /// 모든 목록 접근은 <see cref="_sync"/> 로 보호한다.
    /// </para>
    /// </summary>
    public static class MessageCatalog
    {
        // --- Const ---

        // 자동 등록 항목 수 상한(백스톱). 정상적으로 Code+Kind 단위면 수백 개에서 포화하므로
        // 도달할 일이 거의 없지만, Code 가 가변값을 포함하는 예외 상황의 무한 증식을 막는다.
        private const int MaxAutoEntries = 5000;
        private const long MaxSeedLogBytes = 50L * 1024L * 1024L;

        // --- Fields ---

        // 목록/색인 동시 접근 보호용 락(로그 기록 스레드 ↔ UI 스레드).
        private static readonly object _sync = new object();

        private static List<MessageDefinition> _items = new List<MessageDefinition>();

        // 이미 등록된 메시지 종류 빠른 확인용 색인. 키 = Code + Kind (디스크립션은 가변값이 섞여
        // 종류 판정 기준으로 못 쓰므로 제외한다 — 같은 코드는 desc 가 달라도 한 종류로 본다).
        private static readonly HashSet<string> _index = new HashSet<string>(StringComparer.Ordinal);

        // EnsureRegistered 로 목록이 바뀌었고 아직 파일에 반영되지 않았으면 true.
        private static bool _dirty;

        // 자동 등록 상한 도달 경고를 한 번만 남기기 위한 플래그(_sync 안에서만 접근).
        private static bool _capWarned;

        // --- Properties ---

        // 메시지 카탈로그는 로그 루트 아래 Messages 폴더에 저장한다 (예: D:\CDT-320\Log\Messages\message_catalog.csv).
        public static string Dir   => Path.Combine(EventLogger.LogRoot, "Messages");
        public static string Path_ => Path.Combine(Dir, "message_catalog.csv");

        /// <summary>전체 목록의 스냅샷(읽기 전용). 코드 중복 가능. 순회 중 변경에 안전하도록 복사본을 돌려준다.</summary>
        public static IReadOnlyList<MessageDefinition> Items
        {
            get { lock (_sync) { return new List<MessageDefinition>(_items); } }
        }

        public static int Count
        {
            get { lock (_sync) { return _items.Count; } }
        }

        // --- Constructor ---

        static MessageCatalog()
        {
            try { Directory.CreateDirectory(Dir); } catch { /* 폴더 생성 실패는 Load/Save 에서 다시 처리 */ }
            Load();
        }

        // --- Public Methods ---

        /// <summary>코드로 첫 번째 항목을 찾는다(중복 코드면 처음 것).</summary>
        public static MessageDefinition Get(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            lock (_sync)
            {
                foreach (var d in _items)
                    if (string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase))
                        return d;
            }
            return null;
        }

        /// <summary>
        /// 코드 문구를 언어에 맞게 해석한다.
        /// <para>
        /// 카탈로그는 디스크립션 단위(코드 중복 허용)이므로, <b>코드 + 원문 디스크립션(fallback)</b> 이 정확히
        /// 일치하는 항목만 그 항목의 번역으로 치환한다(같은 코드의 다른 문구에는 영향 없음).
        /// 일치 항목이 없으면 (알람은) AlarmMaster, 그래도 없으면 원문(fallback)을 그대로 쓴다.
        /// </para>
        /// </summary>
        public static string Resolve(EventKind kind, string code, string lang, string fallback)
        {
            try
            {
                string desc = (fallback ?? string.Empty).Trim();

                // 1) 코드 + 디스크립션(KO 또는 EN 원문)이 정확히 일치하는 항목의 번역을 사용.
                if (!string.IsNullOrEmpty(code) && desc.Length > 0)
                {
                    lock (_sync)
                    {
                        foreach (var m in _items)
                        {
                            if (m == null) continue;
                            if (!string.Equals(m.Code, code, StringComparison.OrdinalIgnoreCase)) continue;
                            if (string.Equals((m.Ko ?? string.Empty).Trim(), desc, StringComparison.Ordinal)
                             || string.Equals((m.En ?? string.Empty).Trim(), desc, StringComparison.Ordinal))
                            {
                                string t = m.Text(lang);
                                if (!string.IsNullOrEmpty(t)) return t;
                            }
                        }
                    }
                }

                // 2) 알람은 AlarmMaster 의 코드 기준 제목으로 보조.
                if (kind == EventKind.Alarm)
                {
                    var d = QMC.Common.Alarms.AlarmMaster.Get(code);
                    if (d != null)
                    {
                        string t = d.GetTitle(lang);
                        if (!string.IsNullOrEmpty(t)) return t;
                    }
                }
            }
            catch
            {
                // 번역 해석 실패는 원문(fallback)으로 폴백한다.
            }
            return fallback;
        }

        /// <summary>
        /// 실시간으로 발생한 메시지 종류를 카탈로그에 자동 등록한다(번역은 빈 칸으로 시작).
        /// 종류는 <b>Code+Kind</b> 단위로 판정하므로 같은 코드는 디스크립션(좌표·결과코드 등 가변값)이
        /// 달라도 한 번만 등록된다 — 이 덕에 로그가 수만 건 발생해도 항목 수가 폭증하지 않는다.
        /// 처음 본 종류의 디스크립션을 대표값으로 보관하고, 새로 추가되면 dirty 로 표시한다.
        /// 로그 기록 스레드에서 호출되므로 절대 예외를 밖으로 던지지 않는다.
        /// </summary>
        public static void EnsureRegistered(EventKind kind, string code, string description)
        {
            try
            {
                string desc = (description ?? string.Empty).Trim();
                if (desc.Length == 0) return;
                string c = code ?? string.Empty;

                lock (_sync)
                {
                    string key = MakeKey(c, kind);
                    if (!_index.Add(key)) return;   // 이미 등록된 종류(Code+Kind)

                    // 백스톱 — 상한을 넘으면 신규 종류를 더 받지 않는다(가변 Code 등 예외 상황 방어).
                    if (_items.Count >= MaxAutoEntries)
                    {
                        WarnCapOnce();
                        return;
                    }

                    var def = new MessageDefinition { Code = c, Kind = kind };
                    if (HasKorean(desc)) def.Ko = desc; else def.En = desc;
                    _items.Add(def);
                    _dirty = true;
                }
            }
            catch
            {
                // 자동 등록 실패가 로그 기록 자체를 막지 않도록 무시한다(다음 발생 시 재시도됨).
            }
        }

        /// <summary>화면 편집 결과로 전체 목록을 통째로 교체. 코드 중복은 허용하되 완전 동일 행은 1개만 남긴다.</summary>
        public static void ReplaceAll(IEnumerable<MessageDefinition> items)
        {
            var list = new List<MessageDefinition>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (items != null)
            {
                foreach (var d in items)
                {
                    if (d == null || string.IsNullOrWhiteSpace(d.Code)) continue;
                    // 코드+종류+한/영이 전부 같은 완전 중복만 스킵 (디스크립션이 다르면 코드 같아도 유지)
                    string key = (d.Code ?? "") + "|" + d.Kind + "|" + (d.Ko ?? "") + "|" + (d.En ?? "");
                    if (!seen.Add(key)) continue;
                    list.Add(d);
                }
            }

            lock (_sync)
            {
                _items = list;
                RebuildIndex();   // 편집 결과 기준으로 자동 등록 색인을 다시 맞춘다.
            }
        }

        public static void Load()
        {
            try
            {
                // 파일이 없으면 기본 샘플 없이 빈 목록으로 시작한다(파일은 저장 시 생성).
                if (!File.Exists(Path_))
                {
                    lock (_sync)
                    {
                        _items = new List<MessageDefinition>();
                        RebuildIndex();
                        _dirty = false;
                    }
                    return;
                }

                var list = new List<MessageDefinition>();
                foreach (string line in File.ReadAllLines(Path_, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("Code,", StringComparison.OrdinalIgnoreCase)) continue; // 헤더

                    var parts = ParseCsv(line);
                    if (parts.Count < 4) continue;

                    // 저장 순서: Code, Kind, Ko, En
                    EventKind kind;
                    if (!Enum.TryParse(parts[1], out kind)) kind = EventKind.Event;

                    var def = new MessageDefinition
                    {
                        Code = parts[0],
                        Kind = kind,
                        Ko   = parts[2],
                        En   = parts[3]
                    };
                    if (!string.IsNullOrWhiteSpace(def.Code)) list.Add(def); // 코드 중복 허용
                }

                lock (_sync)
                {
                    _items = list;
                    RebuildIndex();
                    _dirty = false;
                }
            }
            catch
            {
                lock (_sync)
                {
                    _items = new List<MessageDefinition>();
                    RebuildIndex();
                    _dirty = false;
                }
            }
        }

        public static void Save()
        {
            try
            {
                // 파일 쓰기는 락 밖에서 하기 위해 스냅샷을 먼저 뜬다.
                List<MessageDefinition> snapshot;
                lock (_sync)
                {
                    snapshot = new List<MessageDefinition>(_items);
                    _dirty = false;
                }

                Directory.CreateDirectory(Dir);
                var sb = new StringBuilder();
                sb.AppendLine("Code,Kind,Ko,En");
                foreach (var d in snapshot)
                {
                    sb.AppendLine(string.Join(",",
                        Csv(d.Code), d.Kind.ToString(), Csv(d.Ko), Csv(d.En)));
                }
                File.WriteAllText(Path_, sb.ToString(), new UTF8Encoding(true)); // UTF-8 BOM
            }
            catch
            {
                // 저장 실패 시 다음 자동 등록에서 다시 dirty 가 되어 재시도된다.
                lock (_sync) { _dirty = true; }
            }
        }

        /// <summary>자동 등록으로 목록이 바뀌었으면(=dirty) 파일에 저장한다. 로그 기록 배치 단위로 호출된다.</summary>
        public static void FlushIfDirty()
        {
            try
            {
                lock (_sync)
                {
                    if (!_dirty) return;
                }
                Save();
            }
            catch
            {
                // Save 내부에서 이미 dirty 복구를 처리하므로 여기서는 무시한다.
            }
        }

        /// <summary>
        /// 앱 시작 시 1회만, 기존 이벤트 로그(Log\Event\*.csv)를 훑어 등장한 메시지 종류를 카탈로그에 시드한다.
        /// 마커 파일(seed_done.flag)이 있으면 건너뛴다(다음 실행부터는 전체 로그를 다시 훑지 않음).
        /// 전체 로그를 읽는 무거운 작업이므로 반드시 백그라운드 스레드에서 호출한다(<see cref="SeedFromLogsInBackground"/>).
        /// 장비 PC 메모리 보호를 위해 대용량 로그는 시드 대상에서 제외한다.
        /// </summary>
        public static void SeedFromLogs()
        {
            try
            {
                string marker = Path.Combine(Dir, "seed_done.flag");
                if (File.Exists(marker)) return;   // 이미 1회 시드 완료

                string logDir = EventLogger.LogDir;
                if (Directory.Exists(logDir))
                {
                    // 파일명(날짜) 순으로 과거 → 최신 순으로 수집. 중복 종류는 EnsureRegistered 가 알아서 스킵.
                    var files = ResolveLatestSeedLogPaths(logDir);
                    Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                    foreach (var path in files)
                    {
                        if (ShouldSkipSeedLog(path))
                            continue;

                        foreach (var r in EventLogger.EnumerateFile(path))
                            EnsureRegistered(r.Kind, r.Code, r.Description);
                    }
                }

                FlushIfDirty();

                // 시드 완료 마커 기록. 이후 실행에서는 전체 로그를 다시 훑지 않는다.
                Directory.CreateDirectory(Dir);
                File.WriteAllText(marker, "seeded", Encoding.UTF8);
            }
            catch
            {
                // 시드 실패는 치명적이지 않다. 마커를 남기지 않으므로 다음 실행에서 재시도되고,
                // 그 사이 메시지가 발생하면 EnsureRegistered 로 점진적으로 채워진다.
            }
        }

        /// <summary><see cref="SeedFromLogs"/> 를 백그라운드 스레드에서 실행한다(UI 시작을 막지 않음).</summary>
        public static void SeedFromLogsInBackground()
        {
            try { System.Threading.Tasks.Task.Run((Action)SeedFromLogs); }
            catch { /* 백그라운드 시작 실패는 무시(자동 등록으로 점진 복구). */ }
        }

        // --- Private Methods ---

        private static string[] ResolveLatestSeedLogPaths(string logDir)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logDir) || !Directory.Exists(logDir))
                    return new string[0];

                FileInfo latest = null;
                foreach (string path in Directory.GetFiles(logDir, "*.csv"))
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    FileInfo info = new FileInfo(path);
                    if (!info.Exists)
                        continue;

                    if (latest == null ||
                        info.LastWriteTimeUtc > latest.LastWriteTimeUtc ||
                        (info.LastWriteTimeUtc == latest.LastWriteTimeUtc &&
                         string.Compare(info.Name, latest.Name, StringComparison.OrdinalIgnoreCase) > 0))
                    {
                        latest = info;
                    }
                }

                return latest != null ? new[] { latest.FullName } : new string[0];
            }
            catch
            {
                return new string[0];
            }
            finally
            {
            }
        }

        private static bool ShouldSkipSeedLog(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return true;

                var info = new FileInfo(path);
                return info.Length > MaxSeedLogBytes;
            }
            catch
            {
                return true;
            }
            finally
            {
            }
        }

        // 현재 _items 기준으로 색인을 다시 만든다. 반드시 _sync 락 안에서 호출한다.
        // 색인 키는 Code+Kind 단위이므로, 편집 화면에서 같은 코드의 여러 행이 있어도
        // 그 코드는 자동 등록에서 "이미 있음"으로 처리되어 중복 추가되지 않는다.
        private static void RebuildIndex()
        {
            _index.Clear();
            foreach (var d in _items)
            {
                if (d == null) continue;
                _index.Add(MakeKey(d.Code, d.Kind));
            }
        }

        private static string MakeKey(string code, EventKind kind)
        {
            // 탭은 코드/종류 문자열에 쓰이지 않으므로 필드 경계 구분자로 사용한다.
            return (code ?? string.Empty) + "\t" + kind;
        }

        // 자동 등록 상한 도달을 한 번만 로그로 남긴다. 반드시 _sync 락 안에서 호출한다.
        // EventLogger 를 거치지 않고 LogManager 로 직접 남겨 기록 경로 재진입을 피한다.
        private static void WarnCapOnce()
        {
            if (_capWarned) return;
            _capWarned = true;
            try
            {
                QMC.Common.LogManager.Instance.Write(
                    QMC.Common.LogLevel.AboveNormal,
                    "MessageCatalog", "EnsureRegistered",
                    "메시지 카탈로그 자동 등록이 상한(" + MaxAutoEntries + ")에 도달해 신규 종류 등록을 중단합니다. "
                    + "Code 가 가변값을 포함하는지 확인이 필요합니다.");
            }
            catch
            {
                // 경고 로깅 실패는 무시한다(자동 등록 동작 자체에는 영향 없음).
            }
        }

        // 문자열에 한글(완성형/자모)이 포함되어 있는지.
        private static bool HasKorean(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
            {
                if ((c >= '가' && c <= '힣') || (c >= 'ᄀ' && c <= 'ᇿ') || (c >= '㄰' && c <= '㆏'))
                    return true;
            }
            return false;
        }

        // --- CSV 헬퍼 (EventRow 와 동일한 따옴표 규칙) ---

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static List<string> ParseCsv(string line)
        {
            var result = new List<string>();
            var builder = new StringBuilder();
            bool quoted = false;

            line = line ?? string.Empty;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { builder.Append('"'); i++; }
                    else if (c == '"') { quoted = false; }
                    else { builder.Append(c); }
                }
                else
                {
                    if (c == ',') { result.Add(builder.ToString()); builder.Clear(); }
                    else if (c == '"' && builder.Length == 0) { quoted = true; }
                    else { builder.Append(c); }
                }
            }
            result.Add(builder.ToString());
            return result;
        }
    }
}
