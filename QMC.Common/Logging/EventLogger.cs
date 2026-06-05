using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QMC.Common.Logging
{
    public static class EventLogger
    {
        private static readonly object SyncRoot = new object();
        private static string _currentDate;
        private static string _currentPath;
        private static string _logRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");

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

        public static void Write(EventKind kind, string user, string code, string description)
        {
            EventRow row = new EventRow
            {
                When = DateTime.Now,
                Kind = kind,
                User = user ?? string.Empty,
                Code = code ?? string.Empty,
                Description = description ?? string.Empty
            };

            try
            {
                lock (SyncRoot)
                {
                    RotateIfNeeded();
                    File.AppendAllText(_currentPath, row.ToCsv() + Environment.NewLine, Encoding.UTF8);
                }

                WriteLegacyLog(row);
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

        public static List<EventRow> Read(DateTime date)
        {
            try
            {
                string path = Path.Combine(LogDir, date.ToString("yyyy-MM-dd") + ".csv");
                List<EventRow> list = new List<EventRow>();
                if (!File.Exists(path))
                    return list;

                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
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

        private static void RotateIfNeeded()
        {
            try
            {
                string today = DateTime.Today.ToString("yyyy-MM-dd");
                if (_currentDate == today && !string.IsNullOrWhiteSpace(_currentPath))
                    return;

                Directory.CreateDirectory(LogDir);
                _currentDate = today;
                _currentPath = Path.Combine(LogDir, today + ".csv");
                if (!File.Exists(_currentPath))
                    File.WriteAllText(_currentPath, "When,Kind,User,Code,Description" + Environment.NewLine, Encoding.UTF8);
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
