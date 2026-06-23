using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace QMC.CDT320
{
    internal static class PickerMoveTimeFileLogger
    {
        private static readonly object SyncRoot = new object();
        private static readonly object QueueSyncRoot = new object();
        private static readonly Queue<string> PendingLines = new Queue<string>();
        private const string Header =
            "When,Unit,Kind,Axis,ElapsedMs,CommandMs,VerifyMs,SafeZMs,XyMs,ZMs,Result,Verify,Target,TargetName,Fine,Velocity,TargetCount,NonZCount,ZCount,Start,Distance,Acceleration,Deceleration";
        private static bool _writerRunning;

        public static void WriteAxis(
            string unitName,
            PickerAxis axis,
            double startPos,
            double targetPos,
            string targetName,
            bool bFine,
            double velocity,
            double acceleration,
            double deceleration,
            int result,
            long commandMs,
            long verifyMs,
            long totalMs,
            AxisMoveWaitResult waitResult)
        {
            try
            {
                string line = BuildCsvLine(
                    DateTime.Now,
                    unitName,
                    "Axis",
                    axis.ToString(),
                    totalMs,
                    commandMs,
                    verifyMs,
                    null,
                    null,
                    null,
                    result,
                    waitResult != null ? waitResult.Failure.ToString() : "-",
                    targetPos,
                    targetName,
                    bFine,
                    velocity,
                    null,
                    null,
                    null,
                    startPos,
                    Math.Abs(targetPos - startPos),
                    acceleration,
                    deceleration);
                AppendLine(line);
            }
            catch
            {
            }
        }

        public static void WriteGroup(
            string unitName,
            string targetName,
            bool bFine,
            int targetCount,
            int nonZCount,
            int zCount,
            int result,
            long safeZMs,
            long xyMs,
            long zMs,
            long totalMs)
        {
            try
            {
                string line = BuildCsvLine(
                    DateTime.Now,
                    unitName,
                    "Group",
                    string.Empty,
                    totalMs,
                    null,
                    null,
                    safeZMs,
                    xyMs,
                    zMs,
                    result,
                    string.Empty,
                    null,
                    targetName,
                    bFine,
                    null,
                    targetCount,
                    nonZCount,
                    zCount,
                    null,
                    null,
                    null,
                    null);
                AppendLine(line);
            }
            catch
            {
            }
        }

        private static void AppendLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            lock (QueueSyncRoot)
            {
                PendingLines.Enqueue(line);
                if (_writerRunning)
                    return;

                _writerRunning = true;
                Task.Run((Action)WriteQueuedLines);
            }
        }

        private static void WriteQueuedLines()
        {
            try
            {
                while (true)
                {
                    List<string> lines = new List<string>();
                    lock (QueueSyncRoot)
                    {
                        while (PendingLines.Count > 0)
                            lines.Add(PendingLines.Dequeue());

                        if (lines.Count == 0)
                        {
                            _writerRunning = false;
                            return;
                        }
                    }

                    WriteLines(lines);
                }
            }
            catch
            {
                lock (QueueSyncRoot)
                {
                    _writerRunning = false;
                }
            }
        }

        private static void WriteLines(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return;

            try
            {
                lock (SyncRoot)
                {
                    string dir = Path.Combine(EventLogger.LogRoot, "PickerMoveTime");
                    Directory.CreateDirectory(dir);

                    string path = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".csv");
                    if (!File.Exists(path))
                        File.WriteAllText(path, Header + Environment.NewLine, Encoding.UTF8);

                    StringBuilder buffer = new StringBuilder();
                    for (int i = 0; i < lines.Count; i++)
                        buffer.AppendLine(lines[i]);

                    File.AppendAllText(path, buffer.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        private static string BuildCsvLine(
            DateTime when,
            string unitName,
            string kind,
            string axis,
            long? elapsedMs,
            long? commandMs,
            long? verifyMs,
            long? safeZMs,
            long? xyMs,
            long? zMs,
            int result,
            string verify,
            double? target,
            string targetName,
            bool fine,
            double? velocity,
            int? targetCount,
            int? nonZCount,
            int? zCount,
            double? start,
            double? distance,
            double? acceleration,
            double? deceleration)
        {
            return string.Join(",",
                Csv(when.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)),
                Csv(unitName),
                Csv(kind),
                Csv(axis),
                Csv(elapsedMs),
                Csv(commandMs),
                Csv(verifyMs),
                Csv(safeZMs),
                Csv(xyMs),
                Csv(zMs),
                Csv(result),
                Csv(verify),
                Csv(target),
                Csv(targetName),
                Csv(fine),
                Csv(velocity),
                Csv(targetCount),
                Csv(nonZCount),
                Csv(zCount),
                Csv(start),
                Csv(distance),
                Csv(acceleration),
                Csv(deceleration));
        }

        private static string Csv(object value)
        {
            string text;
            if (value == null)
            {
                text = string.Empty;
            }
            else if (value is IFormattable)
            {
                text = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            }
            else
            {
                text = value.ToString();
            }

            if (text == null)
                text = string.Empty;

            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
                return "\"" + text.Replace("\"", "\"\"") + "\"";

            return text;
        }
    }
}
