using System;
using System.Text;

namespace QMC.CDT320.Sequencing
{
    public sealed class SequenceFailureInfo
    {
        public DateTime Timestamp { get; set; }
        public string SequenceName { get; set; }
        public string SequenceKind { get; set; }
        public string StepName { get; set; }
        public string AlarmCode { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
    }

    public static class SequenceFailureStore
    {
        private static readonly object Sync = new object();
        private static SequenceFailureInfo _lastFailure;

        public static void Clear()
        {
            lock (Sync)
            {
                _lastFailure = null;
            }
        }

        public static void Record(
            string sequenceName,
            string sequenceKind,
            string stepName,
            string alarmCode,
            string source,
            string message)
        {
            lock (Sync)
            {
                _lastFailure = new SequenceFailureInfo
                {
                    Timestamp = DateTime.Now,
                    SequenceName = sequenceName ?? "",
                    SequenceKind = sequenceKind ?? "",
                    StepName = stepName ?? "",
                    AlarmCode = alarmCode ?? "",
                    Source = source ?? "",
                    Message = message ?? ""
                };
            }
        }

        public static SequenceFailureInfo GetLast()
        {
            lock (Sync)
            {
                if (_lastFailure == null)
                    return null;

                return new SequenceFailureInfo
                {
                    Timestamp = _lastFailure.Timestamp,
                    SequenceName = _lastFailure.SequenceName,
                    SequenceKind = _lastFailure.SequenceKind,
                    StepName = _lastFailure.StepName,
                    AlarmCode = _lastFailure.AlarmCode,
                    Source = _lastFailure.Source,
                    Message = _lastFailure.Message
                };
            }
        }

        public static string BuildManualFailureMessage(string actionName, string fallbackMessage)
        {
            SequenceFailureInfo failure = GetLast();
            if (failure == null)
                return string.IsNullOrWhiteSpace(fallbackMessage) ? actionName + " failed." : fallbackMessage;

            var builder = new StringBuilder();
            builder.AppendLine(actionName + " 실패");
            builder.AppendLine();
            AppendLine(builder, "Sequence", failure.SequenceName);
            AppendLine(builder, "Kind", failure.SequenceKind);
            AppendLine(builder, "Step", failure.StepName);
            AppendLine(builder, "Alarm Code", failure.AlarmCode);
            AppendLine(builder, "Source", failure.Source);
            AppendLine(builder, "원인", failure.Message);
            builder.AppendLine();
            builder.Append("Alarm/Event Log에도 동일한 내용이 기록되어 있습니다.");
            return builder.ToString();
        }

        public static string AppendRecentDetail(
            string message,
            string currentSequenceName,
            string currentAlarmCode)
        {
            SequenceFailureInfo failure = GetLast();
            if (failure == null)
                return message ?? "";

            if ((DateTime.Now - failure.Timestamp).TotalSeconds > 30.0)
                return message ?? "";

            if (string.Equals(failure.SequenceName, currentSequenceName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(failure.AlarmCode, currentAlarmCode, StringComparison.OrdinalIgnoreCase))
            {
                return message ?? "";
            }

            var builder = new StringBuilder(message ?? "");
            builder.Append(" Detail: ");
            AppendInline(builder, "Sequence", failure.SequenceName);
            AppendInline(builder, "Kind", failure.SequenceKind);
            AppendInline(builder, "Step", failure.StepName);
            AppendInline(builder, "Alarm Code", failure.AlarmCode);
            AppendInline(builder, "Source", failure.Source);
            AppendInline(builder, "Cause", failure.Message);
            return builder.ToString().TrimEnd(' ', '|');
        }

        private static void AppendLine(StringBuilder builder, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                builder.AppendLine(name + ": " + value);
        }

        private static void AppendInline(StringBuilder builder, string name, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                builder.Append(name + "=" + value + " | ");
        }
    }
}
