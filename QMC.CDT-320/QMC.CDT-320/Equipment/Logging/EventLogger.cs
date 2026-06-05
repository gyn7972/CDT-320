using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QMC.Common.Alarms;

namespace QMC.CDT320.Logging
{
    /// <summary>이벤트 종류.</summary>
    public enum EventKind { Event, Warning, Alarm, Data, Work }

    /// <summary>이벤트 1건.</summary>
    public class EventRow
    {
        public DateTime  When { get; set; }
        public EventKind Kind { get; set; }
        public string    User { get; set; }
        public string    Code { get; set; }
        public string    Description { get; set; }

        public string ToCsv()
        {
            return string.Join(",",
                When.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Kind.ToString(),
                Csv(User),
                Csv(Code),
                Csv(Description));
        }

        public static EventRow FromCsv(string line)
        {
            var parts = ParseCsv(line);
            if (parts.Count < 5) return null;
            var r = new EventRow
            {
                Kind        = Enum.TryParse<EventKind>(parts[1], out var k) ? k : EventKind.Event,
                User        = parts[2],
                Code        = parts[3],
                Description = parts[4]
            };
            DateTime.TryParse(parts[0], out var dt);
            r.When = dt;
            return r;
        }

        private static string Csv(string s)
        {
            s = s ?? "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static List<string> ParseCsv(string line)
        {
            var r = new List<string>();
            var sb = new StringBuilder();
            bool q = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (q)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (c == '"') q = false;
                    else sb.Append(c);
                }
                else
                {
                    if (c == ',') { r.Add(sb.ToString()); sb.Clear(); }
                    else if (c == '"' && sb.Length == 0) q = true;
                    else sb.Append(c);
                }
            }
            r.Add(sb.ToString());
            return r;
        }
    }

    /// <summary>
    /// 프로세스 전역 이벤트 로거 — 일자별 CSV 파일 자동 회전.
    /// 파일: <c>./Log/Event/YYYY-MM-DD.csv</c>
    /// </summary>
    public static class EventLogger
    {
        private static readonly object _lock = new object();
        private static string _currentDate;
        private static string _currentPath;

        public static event Action<EventRow> EventLogged;

        public static string LogDir { get; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "Event");

        static EventLogger()
        {
            Directory.CreateDirectory(LogDir);
            AlarmManager.AlarmRaised  += a => Write(EventKind.Alarm,   "QMC", a.Code, $"[{a.Severity}] {a.Source} · {a.Message}");
            AlarmManager.AlarmCleared += a => Write(EventKind.Event,   "QMC", a.Code, $"[CLEARED] {a.Source} · {a.Message}");
        }

        public static void Write(EventKind kind, string user, string code, string description)
        {
            var row = new EventRow { When = DateTime.Now, Kind = kind, User = user, Code = code, Description = description };
            lock (_lock)
            {
                try
                {
                    RotateIfNeeded();
                    File.AppendAllText(_currentPath, row.ToCsv() + Environment.NewLine, Encoding.UTF8);
                }
                catch { /* 로그 실패는 무시 */ }
            }
            try { EventLogged?.Invoke(row); } catch { }
        }

        /// <summary>지정 일자의 로그 파일을 읽어 EventRow 목록을 반환.</summary>
        public static List<EventRow> Read(DateTime date)
        {
            var path = Path.Combine(LogDir, date.ToString("yyyy-MM-dd") + ".csv");
            var list = new List<EventRow>();
            if (!File.Exists(path)) return list;
            try
            {
                foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    var r = EventRow.FromCsv(line);
                    if (r != null) list.Add(r);
                }
            }
            catch { }
            return list;
        }

        private static void RotateIfNeeded()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            if (_currentDate != today)
            {
                _currentDate = today;
                _currentPath = Path.Combine(LogDir, today + ".csv");
                if (!File.Exists(_currentPath))
                    File.WriteAllText(_currentPath, "When,Kind,User,Code,Description" + Environment.NewLine, Encoding.UTF8);
            }
        }
    }
}
