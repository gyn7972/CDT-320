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
        private static string _currentDate;
        private static string _currentPath;
        private static string _logRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        private static bool _writerRunning;

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
                try
                {
                    EventLogged?.Invoke(row);
                }
                catch
                {
                }
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
            string path = Path.Combine(LogDir, date.ToString("yyyy-MM-dd") + ".csv");
            return ReadFile(path);
        }

        // 임의 경로의 이벤트 로그 CSV 파일을 읽어 행 목록으로 반환한다(헤더/빈 줄 건너뜀).
        public static List<EventRow> ReadFile(string path)
        {
            try
            {
                List<EventRow> list = new List<EventRow>();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return list;

                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    // 빈 줄과 CSV 헤더 줄("When,Kind,...")은 건너뛴다. 실제 데이터 행은 타임스탬프로 시작한다.
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    if (line.StartsWith("When,", StringComparison.OrdinalIgnoreCase))
                        continue;

                    EventRow row = EventRow.FromCsv(line);
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
                        while (PendingRows.Count > 0)
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

                lock (SyncRoot)
                {
                    WriteEventCsvRows(rows);
                }

                foreach (EventRow row in rows)
                    WriteLegacyLog(row);
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
                DateTime currentDate = DateTime.MinValue;
                StringBuilder buffer = new StringBuilder();

                foreach (EventRow row in rows)
                {
                    DateTime rowDate = row.When.Date;
                    if (currentDate != DateTime.MinValue && rowDate != currentDate)
                    {
                        FlushEventCsvBuffer(buffer);
                        buffer.Length = 0;
                    }

                    if (rowDate != currentDate)
                    {
                        currentDate = rowDate;
                        RotateIfNeeded(row.When);
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

                File.AppendAllText(_currentPath, buffer.ToString(), Encoding.UTF8);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void RotateIfNeeded(DateTime when)
        {
            try
            {
                string today = when.Date.ToString("yyyy-MM-dd");
                if (_currentDate == today && !string.IsNullOrWhiteSpace(_currentPath))
                    return;

                Directory.CreateDirectory(LogDir);
                _currentDate = today;
                _currentPath = Path.Combine(LogDir, today + ".csv");
                if (!File.Exists(_currentPath))
                    File.WriteAllText(_currentPath, "When,Kind,User,Code,Source,Description" + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                throw;
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
