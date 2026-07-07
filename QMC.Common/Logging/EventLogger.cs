using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Logging
{
    public static class EventLogger
    {
        private static readonly object SyncRoot = new object();
        private static readonly object QueueSyncRoot = new object();
        private static readonly Queue<EventRow> PendingRows = new Queue<EventRow>();
        private const int FlushSleepMs = 20;
        private const int MaxWriterBatchRows = 2000;
        private const int DefaultSafeReadLimit = 10000;
        private const long MaxEventCsvBytes = 100L * 1024L * 1024L;
        private const string CsvHeader = "When,Kind,User,Code,Source,Description";

        // 이력 화면 '오늘' 뷰용 메모리 최근 버퍼. 종류별로 최근 N개를 유지해 파일 IO 없이 즉시 조회한다.
        // (한 종류가 폭주해도 다른 종류의 최근 이력이 밀려나지 않도록 종류별로 분리)
        private const int RecentRowsPerKind = 10000;
        private static readonly object RecentSyncRoot = new object();
        private static readonly Dictionary<EventKind, Queue<EventRow>> RecentByKind
            = new Dictionary<EventKind, Queue<EventRow>>();
        private static string _currentDate;
        private static string _currentPath;
        private static string _logRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        private static bool _writerRunning;

        // 로그 저장 방식 오버라이드(로그 설정창). false=전체 한 폴더, true=종류별 폴더.
        private static readonly object _kindDirLock = new object();
        private static readonly Dictionary<EventKind, string> _kindDirs = new Dictionary<EventKind, string>();
        private static bool _splitByKind;    // 기본 false = 전체 한 폴더(기존 동작)
        private static string _allDir;       // 전체 모드 저장 폴더(null → <LogRoot>\Event)

        public static event Action<EventRow> EventLogged;

        public static string LogRoot
        {
            get
            {
                try
                {
                    return _logRoot;
                }
                catch
                {
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                }
                finally
                {
                }
            }
        }

        public static string LogDir
        {
            get
            {
                try
                {
                    return Path.Combine(LogRoot, "Event");
                }
                catch
                {
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Event");
                }
                finally
                {
                }
            }
        }

        /// <summary>종류별 기본 폴더명. 기본 경로는 &lt;LogRoot&gt;\&lt;이 이름&gt; 이다.
        /// (Event 는 기존과 동일하게 LogDir 과 일치 → 기존 로그 위치 유지)</summary>
        public static string KindFolderName(EventKind kind)
        {
            switch (kind)
            {
                case EventKind.Event:        return "Event";
                case EventKind.Warning:      return "Warning";
                case EventKind.Alarm:        return "Alarm";
                case EventKind.Data:         return "Data";
                case EventKind.Work:         return "Work";
                case EventKind.InputSeq:     return "InputSeq";
                case EventKind.OutputSeq:    return "OutputSeq";
                case EventKind.FrontHeadSeq: return "FrontHeadSeq";
                case EventKind.RearHeadSeq:  return "RearHeadSeq";
                default:                     return "Event";
            }
        }

        /// <summary>해당 종류의 로그 폴더. 설정 오버라이드가 있으면 그것을, 없으면 &lt;LogRoot&gt;\&lt;종류&gt; 를 반환한다.</summary>
        public static string ResolveKindDir(EventKind kind)
        {
            try
            {
                lock (_kindDirLock)
                {
                    // 전체 모드: 모든 종류가 한 폴더(_allDir, 기본 <LogRoot>\Event)로 간다.
                    if (!_splitByKind)
                        return !string.IsNullOrWhiteSpace(_allDir) ? _allDir : Path.Combine(LogRoot, "Event");

                    // 종류별 모드: 오버라이드가 있으면 그것, 없으면 <LogRoot>\<종류>.
                    string ov;
                    if (_kindDirs.TryGetValue(kind, out ov) && !string.IsNullOrWhiteSpace(ov))
                        return ov;
                }
                return Path.Combine(LogRoot, KindFolderName(kind));
            }
            catch
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", KindFolderName(kind));
            }
        }

        /// <summary>로그 저장 경로 설정(설정창 SAVE / 앱 시작 주입).
        /// splitByKind=false 면 전체(allDir 한 폴더), true 면 종류별(kindDirs 오버라이드+기본 &lt;LogRoot&gt;\&lt;종류&gt;).
        /// 이후 flush 부터 새 경로에 기록된다.</summary>
        public static void ConfigureLogPaths(bool splitByKind, string allDir, IDictionary<EventKind, string> kindDirs)
        {
            try
            {
                lock (_kindDirLock)
                {
                    _splitByKind = splitByKind;
                    _allDir = string.IsNullOrWhiteSpace(allDir) ? null : allDir;
                    _kindDirs.Clear();
                    if (kindDirs != null)
                    {
                        foreach (var kv in kindDirs)
                        {
                            if (!string.IsNullOrWhiteSpace(kv.Value))
                                _kindDirs[kv.Key] = kv.Value;
                        }
                    }
                }

                // 다음 flush 에서 새 경로로 파일을 다시 잡도록 현재 캐시를 초기화한다.
                lock (SyncRoot)
                {
                    _currentDate = null;
                    _currentPath = null;
                }
            }
            catch
            {
            }
        }

        /// <summary>종류 이름(EventKind.ToString) 키의 맵으로 경로 설정(설정 저장값·앱 시작 주입용).</summary>
        public static void ConfigureLogPathsByName(bool splitByKind, string allDir, IDictionary<string, string> kindDirsByName)
        {
            var map = new Dictionary<EventKind, string>();
            if (kindDirsByName != null)
            {
                foreach (var kv in kindDirsByName)
                {
                    EventKind kind;
                    if (!string.IsNullOrWhiteSpace(kv.Value) &&
                        Enum.TryParse(kv.Key, out kind) && Enum.IsDefined(typeof(EventKind), kind))
                        map[kind] = kv.Value;
                }
            }
            ConfigureLogPaths(splitByKind, allDir, map);
        }

        public static void Configure(string logRoot)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(logRoot))
                    _logRoot = logRoot;

                lock (SyncRoot)
                {
                    _currentDate = null;
                    _currentPath = null;
                    Directory.CreateDirectory(LogDir);
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        // 기존 호출부 호환용 (source 없음). 내부적으로 source="" 로 위임한다.
        public static void Write(EventKind kind, string user, string code, string description)
        {
            Write(kind, user, code, string.Empty, description);
        }

        public static void Write(EventKind kind, string user, string code, string source, string description)
        {
            EventRow row = new EventRow
            {
                When = DateTime.Now,
                Kind = kind,
                User = user ?? string.Empty,
                Code = code ?? string.Empty,
                Source = source ?? string.Empty,
                Description = description ?? string.Empty
            };

            try
            {
                EnqueueWrite(row);
            }
            catch
            {
            }
            finally
            {
            }
        }

        // 메모리 최근 버퍼에 1건 추가(종류별 상한 유지). 기록 경로에서 호출되므로 예외를 밖으로 내지 않는다.
        private static void AddRecentRow(EventRow row)
        {
            try
            {
                if (row == null)
                    return;

                lock (RecentSyncRoot)
                {
                    Queue<EventRow> queue;
                    if (!RecentByKind.TryGetValue(row.Kind, out queue))
                    {
                        queue = new Queue<EventRow>();
                        RecentByKind[row.Kind] = queue;
                    }

                    queue.Enqueue(row);
                    while (queue.Count > RecentRowsPerKind)
                        queue.Dequeue();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// 메모리에 유지 중인 최근 로그를 조회한다(파일 IO 없음). 이력 화면의 '오늘' 뷰가 사용한다.
        /// 앱 시작 이후 기록된 로그만 담고 있으며, 과거분은 ReadRecent(파일 기반)로 조회한다.
        /// </summary>
        public static List<EventRow> ReadRecentMemory(EventKind? kind, int maxRows, Predicate<EventRow> filter = null)
        {
            try
            {
                int limit = maxRows > 0 ? maxRows : DefaultSafeReadLimit;

                // 락 유지 시간을 줄이기 위해 스냅샷만 뜨고, 필터링은 락 밖에서 수행한다.
                List<EventRow> snapshot = new List<EventRow>();
                lock (RecentSyncRoot)
                {
                    if (kind != null)
                    {
                        Queue<EventRow> queue;
                        if (RecentByKind.TryGetValue(kind.Value, out queue))
                            snapshot.AddRange(queue);
                    }
                    else
                    {
                        foreach (Queue<EventRow> queue in RecentByKind.Values)
                            snapshot.AddRange(queue);
                    }
                }

                // 여러 종류를 합친 경우에만 시간순 정렬이 필요하다(단일 종류는 이미 기록 순).
                if (kind == null)
                    snapshot.Sort((a, b) => a.When.CompareTo(b.When));

                Queue<EventRow> rows = new Queue<EventRow>(Math.Min(limit, 1024));
                foreach (EventRow row in snapshot)
                {
                    if (row == null)
                        continue;
                    if (filter != null && !filter(row))
                        continue;

                    rows.Enqueue(row);
                    while (rows.Count > limit)
                        rows.Dequeue();
                }

                return new List<EventRow>(rows);
            }
            catch
            {
                return new List<EventRow>();
            }
            finally
            {
            }
        }
         
        public static bool FlushPending(int timeoutMs)
        {
            try
            {
                DateTime deadline = DateTime.Now.AddMilliseconds(Math.Max(0, timeoutMs));
                while (DateTime.Now <= deadline)
                {
                    lock (QueueSyncRoot)
                    {
                        if (PendingRows.Count == 0 && !_writerRunning)
                            return true;
                    }

                    Thread.Sleep(FlushSleepMs);
                }

                lock (QueueSyncRoot)
                {
                    return PendingRows.Count == 0 && !_writerRunning;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        public static List<EventRow> Read(DateTime date)
        {
            try
            {
                List<EventRow> list = new List<EventRow>();
                foreach (string path in GetLogFilesForDate(date))
                {
                    foreach (EventRow row in EnumerateFile(path))
                    {
                        if (row != null)
                            list.Add(row);
                    }
                }

                return list;
            }
            catch
            {
                return new List<EventRow>();
            }
            finally
            {
            }
        }

        // 임의 경로의 이벤트 로그 CSV 파일을 읽어 행 목록으로 반환한다(헤더/빈 줄 건너뜀).
        public static List<EventRow> ReadFile(string path)
        {
            try
            {
                List<EventRow> list = new List<EventRow>();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return list;

                foreach (EventRow row in EnumerateFile(path))
                {
                    if (row != null)
                        list.Add(row);
                }

                return list;
            }
            catch
            {
                return new List<EventRow>();
            }
            finally
            {
            }
        }

        public static IEnumerable<EventRow> EnumerateFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                yield break;

            // FileShare.ReadWrite 로 열어 읽는 동안에도 로그 기록(Append)이 막히지 않게 한다.
            // (File.ReadLines 는 FileShare.Read 라 대용량 파일을 읽는 동안 기록이 조용히 유실된다)
            StreamReader reader = TryOpenSharedReader(path);
            if (reader == null)
                yield break;

            try
            {
                while (true)
                {
                    string line;
                    try
                    {
                        line = reader.ReadLine();
                    }
                    catch
                    {
                        break;
                    }

                    if (line == null)
                        break;

                    EventRow row = TryParseLogLine(line);
                    if (row != null)
                        yield return row;
                }
            }
            finally
            {
                try { reader.Dispose(); } catch { }
            }
        }

        private static StreamReader TryOpenSharedReader(string path)
        {
            try
            {
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return new StreamReader(stream, Encoding.UTF8);
            }
            catch
            {
                return null;
            }
        }

        public static List<EventRow> ReadRecentFile(string path, int maxRows, Predicate<EventRow> filter = null, Func<bool> cancelRequested = null)
        {
            try
            {
                int limit = maxRows > 0 ? maxRows : DefaultSafeReadLimit;
                Queue<EventRow> rows = new Queue<EventRow>(Math.Min(limit, 1024));

                int scanned = 0;
                foreach (EventRow row in EnumerateFile(path))
                {
                    // 조건이 바뀌어 더 읽을 필요가 없어지면 즉시 중단한다(대용량 파일 낭비 읽기 방지).
                    // 콜백 비용을 줄이기 위해 2048행마다 한 번만 확인한다.
                    if (cancelRequested != null && (++scanned & 0x7FF) == 0 && cancelRequested())
                        break;

                    if (row == null)
                        continue;
                    if (filter != null && !filter(row))
                        continue;

                    rows.Enqueue(row);
                    while (rows.Count > limit)
                        rows.Dequeue();
                }

                return new List<EventRow>(rows);
            }
            catch
            {
                return new List<EventRow>();
            }
            finally
            {
            }
        }

        public static List<EventRow> ReadRecent(DateTime date, int maxRows, Predicate<EventRow> filter = null, Func<bool> cancelRequested = null)
        {
            try
            {
                string path = GetActiveLogFileForDate(date, null);
                if (string.IsNullOrWhiteSpace(path))
                    return new List<EventRow>();

                return ReadRecentFile(path, maxRows, filter, cancelRequested);
            }
            catch
            {
                return new List<EventRow>();
            }
            finally
            {
            }
        }

        /// <summary><see cref="ReadRecentFileTail"/> 의 날짜 버전 — 해당 날짜 활성 파일을 끝에서 거꾸로 읽는다.</summary>
        public static List<EventRow> ReadRecentTail(DateTime date, int maxRows, Predicate<EventRow> filter = null, Func<bool> cancelRequested = null, EventKind? kindHint = null)
        {
            try
            {
                string path = GetActiveLogFileForDate(date, kindHint);
                if (string.IsNullOrWhiteSpace(path))
                    return new List<EventRow>();

                return ReadRecentFileTail(path, maxRows, filter, cancelRequested, kindHint);
            }
            catch
            {
                return new List<EventRow>();
            }
            finally
            {
            }
        }

        /// <summary>
        /// 파일을 <b>끝에서 거꾸로</b> 읽어 필터를 통과하는 최신 maxRows 개를 시간순(과거→최신)으로 돌려준다.
        /// 최신 로그는 파일 끝에 있으므로, 수 GB 파일이라도 필요한 만큼(보통 끝의 수 MB)만 읽고 끝난다.
        /// 줄 경계는 0x0A(LF) 바이트로 찾는다 — UTF-8 멀티바이트 문자에는 0x0A 가 나올 수 없어 안전하다.
        /// </summary>
        public static List<EventRow> ReadRecentFileTail(string path, int maxRows, Predicate<EventRow> filter = null, Func<bool> cancelRequested = null, EventKind? kindHint = null)
        {
            try
            {
                var newestFirst = new List<EventRow>();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return newestFirst;

                int limit = maxRows > 0 ? maxRows : DefaultSafeReadLimit;

                // 종류 힌트가 있으면 ",Kind," 토큰을 바이트로 미리 대조해, 종류가 다른 줄은
                // 문자열 변환/CSV 파싱 없이 건너뛴다 — 원하는 종류가 드문 파일에서 스캔이 몇 배 빨라진다.
                byte[] kindToken = kindHint != null
                    ? Encoding.ASCII.GetBytes("," + kindHint.Value + ",")
                    : null;

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long pos = stream.Length;
                    // 역방향 읽기는 OS 미리읽기(순방향 최적화)를 못 받으므로 청크를 크게 잡아
                    // 탐색(seek) 횟수를 줄인다 — 대용량 파일을 거슬러 훑을 때 디스크 순차 속도에 근접한다.
                    byte[] chunk = new byte[4 * 1024 * 1024];
                    byte[] carry = new byte[0];   // 줄 시작을 아직 못 읽은 잘린 앞부분(하위 청크와 이어 붙임)

                    while (pos > 0 && newestFirst.Count < limit)
                    {
                        if (cancelRequested != null && cancelRequested())
                            break;

                        int readSize = (int)Math.Min(chunk.Length, pos);
                        pos -= readSize;
                        stream.Seek(pos, SeekOrigin.Begin);

                        int read = 0;
                        while (read < readSize)
                        {
                            int n = stream.Read(chunk, read, readSize - read);
                            if (n <= 0) break;
                            read += n;
                        }
                        if (read <= 0)
                            break;

                        // data = 이번 청크 + 직전 반복에서 남긴 carry (carry 는 이번 청크 뒤에 이어지는 내용)
                        byte[] data = new byte[read + carry.Length];
                        Buffer.BlockCopy(chunk, 0, data, 0, read);
                        Buffer.BlockCopy(carry, 0, data, read, carry.Length);

                        // 뒤에서부터 LF 를 찾아 완성된 줄만 처리한다(최신 줄 먼저).
                        int lineEnd = data.Length;
                        for (int i = data.Length - 1; i >= 0 && newestFirst.Count < limit; i--)
                        {
                            if (data[i] != (byte)'\n')
                                continue;

                            AppendTailLine(data, i + 1, lineEnd - (i + 1), filter, newestFirst, limit, kindToken);
                            lineEnd = i;
                        }

                        if (newestFirst.Count >= limit)
                            break;

                        // 앞부분(줄 시작 미확정)은 다음 하위 청크와 이어 붙이기 위해 보관한다.
                        carry = new byte[lineEnd];
                        Buffer.BlockCopy(data, 0, carry, 0, lineEnd);
                    }

                    // 파일 맨 앞 줄(남은 carry) 처리.
                    if (newestFirst.Count < limit && carry.Length > 0 &&
                        (cancelRequested == null || !cancelRequested()))
                    {
                        AppendTailLine(carry, 0, carry.Length, filter, newestFirst, limit, kindToken);
                    }
                }

                newestFirst.Reverse();   // 호출부(이력 화면)는 과거→최신 순 목록을 기대한다.
                return newestFirst;
            }
            catch
            {
                return new List<EventRow>();
            }
            finally
            {
            }
        }

        // 역방향 읽기에서 찾은 줄 하나를 파싱/필터링해 결과에 추가한다(최신 줄 먼저 쌓임).
        private static void AppendTailLine(byte[] data, int offset, int count, Predicate<EventRow> filter, List<EventRow> newestFirst, int limit, byte[] kindToken)
        {
            try
            {
                if (count <= 0 || newestFirst.Count >= limit)
                    return;

                // 줄 끝의 CR(0x0D) 제거 (CRLF 파일 대응).
                while (count > 0 && data[offset + count - 1] == (byte)'\r')
                    count--;
                if (count <= 0)
                    return;

                // 종류(Kind) 바이트 사전 대조 — 안 맞으면 비싼 문자열 변환/파싱을 건너뛴다.
                if (kindToken != null && !MatchesKindToken(data, offset, count, kindToken))
                    return;

                string line = Encoding.UTF8.GetString(data, offset, count);
                EventRow row = TryParseLogLine(line);
                if (row == null)
                    return;
                if (filter != null && !filter(row))
                    return;

                newestFirst.Add(row);
            }
            catch
            {
            }
            finally
            {
            }
        }

        // 줄의 첫 번째 콤마 위치부터 ",Kind," 토큰(바이트)이 정확히 이어지는지 확인한다.
        // CSV 두 번째 필드(Kind)만 보는 값싼 검사로, 전체 파싱보다 훨씬 빠르다.
        private static bool MatchesKindToken(byte[] data, int offset, int count, byte[] token)
        {
            int end = offset + count;
            for (int i = offset; i < end; i++)
            {
                if (data[i] != (byte)',')
                    continue;

                // token 은 ','로 시작하므로 i 위치부터 token 전체가 일치해야 한다.
                if (i + token.Length > end)
                    return false;
                for (int j = 1; j < token.Length; j++)
                {
                    if (data[i + j] != token[j])
                        return false;
                }
                return true;
            }
            return false;
        }

        private static EventRow TryParseLogLine(string line)
        {
            try
            {
                // 빈 줄과 CSV 헤더 줄("When,Kind,...")은 건너뛴다. 실제 데이터 행은 타임스탬프로 시작한다.
                if (string.IsNullOrWhiteSpace(line))
                    return null;
                if (line.StartsWith("When,", StringComparison.OrdinalIgnoreCase))
                    return null;

                return EventRow.FromCsv(line);
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static void EnqueueWrite(EventRow row)
        {
            try
            {
                lock (QueueSyncRoot)
                {
                    PendingRows.Enqueue(row);
                    if (_writerRunning)
                        return;

                    _writerRunning = true;
                    Task.Run((Action)WriteQueuedRows);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void WriteQueuedRows()
        {
            try
            {
                while (true)
                {
                    List<EventRow> rows = new List<EventRow>();
                    lock (QueueSyncRoot)
                    {
                        while (PendingRows.Count > 0 && rows.Count < MaxWriterBatchRows)
                            rows.Add(PendingRows.Dequeue());

                        if (rows.Count == 0)
                        {
                            _writerRunning = false;
                            return;
                        }
                    }

                    WriteRows(rows);
                }
            }
            catch
            {
                lock (QueueSyncRoot)
                {
                    _writerRunning = false;
                }
            }
            finally
            {
            }
        }

        private static void WriteRows(List<EventRow> rows)
        {
            try
            {
                if (rows == null || rows.Count == 0)
                    return;

                PublishRows(rows);

                lock (SyncRoot)
                {
                    WriteEventCsvRows(rows);
                }

                foreach (EventRow row in rows)
                    WriteLegacyLog(row);

                // 발생한 메시지 종류를 번역 카탈로그에 자동 등록한다(편집 페이지가 로그 전체를 다시 훑지 않도록).
                // 이 메서드는 백그라운드 writer 스레드에서만 실행되므로 UI 부하가 없고, 새 종류가 생긴
                // 배치에 한해 1회만 파일로 flush 한다(EnsureRegistered/FlushIfDirty 는 예외를 던지지 않음).
                foreach (EventRow row in rows)
                    MessageCatalog.EnsureRegistered(row.Kind, row.Code, row.Description);
                MessageCatalog.FlushIfDirty();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void PublishRows(List<EventRow> rows)
        {
            try
            {
                foreach (EventRow row in rows)
                    AddRecentRow(row);

                Action<EventRow> handler = EventLogged;
                if (handler == null)
                    return;

                foreach (EventRow row in rows)
                {
                    try { handler(row); } catch { }
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void WriteEventCsvRows(List<EventRow> rows)
        {
            try
            {
                // 종류별 폴더의 그날 CSV 로 기록한다. 종류 또는 날짜가 바뀌면 버퍼를 비우고 대상 파일을 전환한다.
                EventKind currentKind = EventKind.Event;
                bool hasCurrent = false;
                DateTime currentDate = DateTime.MinValue;
                StringBuilder buffer = new StringBuilder();

                foreach (EventRow row in rows)
                {
                    DateTime rowDate = row.When.Date;
                    bool switchTarget = !hasCurrent || row.Kind != currentKind || rowDate != currentDate;

                    if (switchTarget && buffer.Length > 0)
                    {
                        FlushEventCsvBuffer(buffer);
                        buffer.Length = 0;
                    }

                    if (switchTarget)
                    {
                        currentKind = row.Kind;
                        currentDate = rowDate;
                        hasCurrent = true;
                        RotateIfNeeded(row.When, ResolveKindDir(row.Kind));
                    }

                    buffer.Append(row.ToCsv());
                    buffer.Append(Environment.NewLine);
                }

                FlushEventCsvBuffer(buffer);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void FlushEventCsvBuffer(StringBuilder buffer)
        {
            try
            {
                if (buffer == null || buffer.Length == 0 || string.IsNullOrWhiteSpace(_currentPath))
                    return;

                string text = buffer.ToString();
                RotateBySizeIfNeeded(Encoding.UTF8.GetByteCount(text));
                File.AppendAllText(_currentPath, text, Encoding.UTF8);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void RotateIfNeeded(DateTime when, string dir)
        {
            try
            {
                string today = when.Date.ToString("yyyy-MM-dd");
                string desired = Path.Combine(dir, today + ".csv");
                if (_currentDate == today &&
                    !string.IsNullOrWhiteSpace(_currentPath) &&
                    string.Equals(_currentPath, desired, StringComparison.OrdinalIgnoreCase))
                    return;

                Directory.CreateDirectory(dir);
                _currentDate = today;
                _currentPath = desired;
                if (!File.Exists(_currentPath))
                    WriteEventCsvHeader(_currentPath);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void RotateBySizeIfNeeded(long pendingBytes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentPath))
                    return;

                long currentBytes = 0;
                if (File.Exists(_currentPath))
                    currentBytes = new FileInfo(_currentPath).Length;

                if (currentBytes <= 0 || currentBytes + Math.Max(0, pendingBytes) <= MaxEventCsvBytes)
                    return;

                string archivePath = ResolveNextArchivePath(_currentPath);
                try
                {
                    File.Move(_currentPath, archivePath);
                }
                catch
                {
                    archivePath = ResolveFallbackArchivePath(_currentPath);
                    File.Move(_currentPath, archivePath);
                }

                WriteEventCsvHeader(_currentPath);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string ResolveNextArchivePath(string activePath)
        {
            string directory = Path.GetDirectoryName(activePath);
            string name = Path.GetFileNameWithoutExtension(activePath);
            string extension = Path.GetExtension(activePath);
            if (string.IsNullOrEmpty(extension))
                extension = ".csv";

            for (int i = 1; i < 10000; i++)
            {
                string candidate = Path.Combine(directory, name + "_" + i.ToString("D3") + extension);
                if (!File.Exists(candidate))
                    return candidate;
            }

            return ResolveFallbackArchivePath(activePath);
        }

        private static string ResolveFallbackArchivePath(string activePath)
        {
            string directory = Path.GetDirectoryName(activePath);
            string name = Path.GetFileNameWithoutExtension(activePath);
            string extension = Path.GetExtension(activePath);
            if (string.IsNullOrEmpty(extension))
                extension = ".csv";

            return Path.Combine(directory, name + "_" + DateTime.Now.ToString("HHmmss_fff") + extension);
        }

        private static void WriteEventCsvHeader(string path)
        {
            File.WriteAllText(path, CsvHeader + Environment.NewLine, Encoding.UTF8);
        }

        private static IEnumerable<string> GetLogFilesForDate(DateTime date)
        {
            var result = new List<string>();
            try
            {
                string dateName = date.ToString("yyyy-MM-dd");

                // 종류별 분리 후에도 '그날 전체' 읽기를 유지하기 위해 모든 종류 폴더(+기본 LogDir)를 훑는다.
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var dirs = new List<string>();
                if (seen.Add(LogDir))
                    dirs.Add(LogDir);
                foreach (EventKind kind in (EventKind[])Enum.GetValues(typeof(EventKind)))
                {
                    string kd = ResolveKindDir(kind);
                    if (seen.Add(kd))
                        dirs.Add(kd);
                }

                foreach (string dir in dirs)
                {
                    if (!Directory.Exists(dir))
                        continue;

                    string[] archived = Directory.GetFiles(dir, dateName + "_*.csv");
                    Array.Sort(archived, StringComparer.OrdinalIgnoreCase);
                    foreach (string path in archived)
                    {
                        if (IsNumberedArchivePath(path, dateName))
                            result.Add(path);
                    }

                    string activePath = Path.Combine(dir, dateName + ".csv");
                    if (File.Exists(activePath))
                        result.Add(activePath);
                }
            }
            catch
            {
            }
            finally
            {
            }

            return result;
        }

        private static string GetActiveLogFileForDate(DateTime date, EventKind? kindHint)
        {
            try
            {
                string dir = kindHint.HasValue ? ResolveKindDir(kindHint.Value) : LogDir;
                string path = Path.Combine(dir, date.ToString("yyyy-MM-dd") + ".csv");
                return File.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private static bool IsNumberedArchivePath(string path, string dateName)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(path);
                string prefix = dateName + "_";
                if (string.IsNullOrWhiteSpace(name) ||
                    !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                string suffix = name.Substring(prefix.Length);
                if (suffix.Length == 0)
                    return false;

                for (int i = 0; i < suffix.Length; i++)
                {
                    if (!char.IsDigit(suffix[i]))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static void WriteLegacyLog(EventRow row)
        {
            try
            {
                QMC.Common.LogLevel level = QMC.Common.LogLevel.Normal;
                if (row.Kind == EventKind.Warning)
                    level = QMC.Common.LogLevel.AboveNormal;
                else if (row.Kind == EventKind.Alarm)
                    level = QMC.Common.LogLevel.Highest;

                QMC.Common.LogManager.Instance.Write(level, row.Kind.ToString(), row.Code, row.Description);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
