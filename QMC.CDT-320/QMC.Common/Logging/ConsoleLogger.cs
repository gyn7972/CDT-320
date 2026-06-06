using System;

namespace QMC.Common.Logging
{
    public class ConsoleLogger : ILogger
    {
        private readonly string _name;
        public ConsoleLogger(string name) { _name = name; }
        public void Info(string message) => Write("INFO", message);
        public void Warn(string message) => Write("WARN", message);
        public void Error(string message, Exception ex = null) => Write("ERROR", ex == null ? message : message + " | " + ex.Message);
        public void Debug(string message) => Write("DEBUG", message);

        private void Write(string level, string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}][{level}][{_name}] {message}");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][{level}][{_name}] {message}");
        }
    }
}
