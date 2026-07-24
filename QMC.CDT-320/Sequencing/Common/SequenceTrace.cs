using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Diagnostics.TactTime;
using QMC.Common.Logging;

namespace QMC.CDT320.Sequencing
{
    internal static class SequenceTrace
    {
        private static int _runSeed;
        private static long _traceSeed;

        public static string CreateRunId(EventKind kind, string sequenceName)
        {
            string safeName = SanitizeIdPart(sequenceName);
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = SanitizeIdPart(kind.ToString());

            int seq = Interlocked.Increment(ref _runSeed);
            return DateTime.Now.ToString("yyyyMMdd-HHmmss.fff") +
                   "-" + safeName.ToUpperInvariant() +
                   "-" + seq.ToString("000");
        }

        public static string NextTraceId()
        {
            long seq = Interlocked.Increment(ref _traceSeed);
            return seq.ToString("000000000");
        }

        public static void RunStart(string sequenceName, string mode, params string[] details)
        {
            Emit("RunStart", sequenceName, null, Merge(details, "mode=" + mode));
        }

        public static void RunEnd(string sequenceName, string status, int result, params string[] details)
        {
            // 실패/정지/취소 종료는 문자열이 아닌 명시 중요도(Failure)로 기록해 최소 로그 정책에서도 항상 영구 저장한다.
            LogSeverity severity = string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
                ? LogSeverity.Normal
                : LogSeverity.Failure;
            Emit("RunEnd", sequenceName, null, null, null, severity, Merge(details, "status=" + status, "result=" + result));
        }

        public static void StepStart(string sequenceName, string step, params string[] details)
        {
            Emit("StepStart", sequenceName, step, details);
        }

        public static void StepEnd(string sequenceName, string step, int result, params string[] details)
        {
            Emit("StepEnd", sequenceName, step, Merge(details, "result=" + result));
        }

        public static void StepFail(string sequenceName, string step, int result, params string[] details)
        {
            Emit("StepFail", sequenceName, step, null, null, LogSeverity.Failure, Merge(details, "result=" + result));
        }

        public static void ChildStart(string childSequenceName, string childStep, params string[] details)
        {
            SequenceLogScope scope = SequenceLog.Current;
            Emit("ChildStart", childSequenceName, childStep, CurrentSequenceName(scope), CurrentDepth(scope) + 1, details);
        }

        public static void ChildEnd(string childSequenceName, string childStep, int result, params string[] details)
        {
            SequenceLogScope scope = SequenceLog.Current;
            Emit("ChildEnd", childSequenceName, childStep, CurrentSequenceName(scope), CurrentDepth(scope) + 1, Merge(details, "result=" + result));
        }

        public static void ChildFail(string childSequenceName, string childStep, int result, params string[] details)
        {
            SequenceLogScope scope = SequenceLog.Current;
            Emit("ChildFail", childSequenceName, childStep, CurrentSequenceName(scope), CurrentDepth(scope) + 1, LogSeverity.Failure, Merge(details, "result=" + result));
        }

        public static async Task<int> ChildAsync(
            string childSequenceName,
            string childStep,
            Func<Task<int>> action,
            params string[] details)
        {
            ChildStart(childSequenceName, childStep, details);
            try
            {
                int result = action != null ? await action().ConfigureAwait(false) : -1;
                if (result == 0)
                    ChildEnd(childSequenceName, childStep, result, details);
                else
                    ChildFail(childSequenceName, childStep, result, details);
                return result;
            }
            catch (OperationCanceledException)
            {
                ChildFail(childSequenceName, childStep, -1, Merge(details, "status=Canceled"));
                throw;
            }
            catch (SequenceStopException)
            {
                ChildFail(childSequenceName, childStep, -1, Merge(details, "status=Stopped"));
                throw;
            }
            catch (Exception ex)
            {
                ChildFail(childSequenceName, childStep, -1, Merge(details, "error=" + ex.Message));
                throw;
            }
        }

        public static void WaitStart(string target, params string[] details)
        {
            Emit("WaitStart", target, null, details);
        }

        public static void WaitEnd(string target, int result, params string[] details)
        {
            Emit("WaitEnd", target, null, Merge(details, "result=" + result));
        }

        public static void MotionStart(string target, params string[] details)
        {
            Emit("MotionStart", target, null, details);
        }

        public static void MotionEnd(string target, int result, params string[] details)
        {
            Emit("MotionEnd", target, null, Merge(details, "result=" + result));
        }

        public static void ResourceAcquire(string resource, params string[] details)
        {
            Emit("ResourceAcquire", "SequenceResource", resource, details);
        }

        public static void ResourceRelease(string resource, params string[] details)
        {
            Emit("ResourceRelease", "SequenceResource", resource, details);
        }

        public static void SignalSet(string signalName, params string[] details)
        {
            Emit("SignalSet", "SequenceSignalBus", signalName, details);
        }

        public static void SignalReset(string signalName, params string[] details)
        {
            Emit("SignalReset", "SequenceSignalBus", signalName, details);
        }

        public static void SignalWaitStart(string signalName, params string[] details)
        {
            Emit("WaitStart", "SequenceSignalBus", signalName, Merge(details, "target=" + signalName));
        }

        public static void SignalWaitEnd(string signalName, int result, params string[] details)
        {
            Emit("WaitEnd", "SequenceSignalBus", signalName, Merge(details, "target=" + signalName, "result=" + result));
        }

        public static void MaterialChange(string action, params string[] details)
        {
            Emit("MaterialChange", "MaterialStateService", action, details);
        }

        public static void TactStart(TactTimeCategory category, string sequenceName, string stepOrProcess, params string[] details)
        {
            switch (category)
            {
                case TactTimeCategory.Motion:
                    MotionStart(sequenceName, Merge(details, "process=" + stepOrProcess));
                    break;
                case TactTimeCategory.Wait:
                    WaitStart(sequenceName, Merge(details, "process=" + stepOrProcess));
                    break;
                case TactTimeCategory.Step:
                case TactTimeCategory.Process:
                    StepStart(sequenceName, stepOrProcess, Merge(details, "category=" + category));
                    break;
            }
        }

        public static void TactEnd(TactTimeCategory category, string sequenceName, string stepOrProcess, int result, params string[] details)
        {
            switch (category)
            {
                case TactTimeCategory.Motion:
                    MotionEnd(sequenceName, result, Merge(details, "process=" + stepOrProcess));
                    break;
                case TactTimeCategory.Wait:
                    WaitEnd(sequenceName, result, Merge(details, "process=" + stepOrProcess));
                    break;
                case TactTimeCategory.Step:
                case TactTimeCategory.Process:
                    if (result == 0)
                        StepEnd(sequenceName, stepOrProcess, result, Merge(details, "category=" + category));
                    else
                        StepFail(sequenceName, stepOrProcess, result, Merge(details, "category=" + category));
                    break;
            }
        }

        private static void Emit(string phase, string source, string step, params string[] details)
        {
            Emit(phase, source, step, null, null, LogSeverity.Normal, details);
        }

        private static void Emit(string phase, string source, string step, string parentOverride, int? depthOverride, params string[] details)
        {
            Emit(phase, source, step, parentOverride, depthOverride, LogSeverity.Normal, details);
        }

        private static void Emit(string phase, string source, string step, string parentOverride, int? depthOverride, LogSeverity severity, params string[] details)
        {
            try
            {
                SequenceLogScope scope = SequenceLog.Current;
                EventKind kind = scope != null ? scope.Kind : EventKind.Event;
                string sequenceName = !string.IsNullOrWhiteSpace(source)
                    ? source
                    : scope != null ? scope.SequenceName : string.Empty;
                string stepName = !string.IsNullOrWhiteSpace(step)
                    ? step
                    : scope != null ? scope.Step : string.Empty;

                List<string> parts = new List<string>();
                Add(parts, "trace", NextTraceId());
                Add(parts, "run", scope != null ? scope.RunId : "");
                Add(parts, "parent", parentOverride ?? (scope != null ? scope.Parent : ""));
                Add(parts, "seq", sequenceName);
                if (scope != null &&
                    !string.IsNullOrWhiteSpace(scope.SequenceName) &&
                    !string.Equals(sequenceName, scope.SequenceName, StringComparison.OrdinalIgnoreCase))
                {
                    Add(parts, "owner", scope.SequenceName);
                }
                Add(parts, "step", stepName);
                Add(parts, "phase", phase);
                Add(parts, "mode", scope != null ? scope.Mode : "");
                Add(parts, "depth", depthOverride.HasValue ? depthOverride.Value.ToString() : scope != null ? scope.Depth.ToString() : "0");
                AddDetails(parts, details);

                string code = !string.IsNullOrWhiteSpace(stepName) ? stepName : phase;
                string eventSource = !string.IsNullOrWhiteSpace(sequenceName) ? sequenceName : "SequenceTrace";
                EventLogger.Write(kind, "SYSTEM", code, eventSource, string.Join(" ", parts.ToArray()), severity);
            }
            catch
            {
            }
        }

        private static string CurrentSequenceName(SequenceLogScope scope)
        {
            if (scope == null)
                return string.Empty;

            return !string.IsNullOrWhiteSpace(scope.SequenceName) ? scope.SequenceName : scope.Unit;
        }

        private static int CurrentDepth(SequenceLogScope scope)
        {
            return scope != null ? scope.Depth : 0;
        }

        private static void AddDetails(List<string> parts, string[] details)
        {
            if (details == null)
                return;

            for (int i = 0; i < details.Length; i++)
            {
                string item = details[i];
                if (string.IsNullOrWhiteSpace(item))
                    continue;

                int p = item.IndexOf('=');
                if (p <= 0)
                    Add(parts, "detail", item);
                else
                    Add(parts, item.Substring(0, p), item.Substring(p + 1));
            }
        }

        private static void Add(List<string> parts, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            parts.Add(key + "=" + Quote(value));
        }

        private static string Quote(string value)
        {
            if (value == null)
                return "";

            bool quote = value.Length == 0;
            for (int i = 0; i < value.Length && !quote; i++)
            {
                char c = value[i];
                quote = char.IsWhiteSpace(c) || c == ',' || c == '"' || c == '=';
            }

            if (!quote)
                return value;

            return "\"" + value.Replace("\"", "'") + "\"";
        }

        private static string[] Merge(string[] first, params string[] rest)
        {
            int firstCount = first != null ? first.Length : 0;
            int restCount = rest != null ? rest.Length : 0;
            string[] result = new string[firstCount + restCount];
            for (int i = 0; i < firstCount; i++)
                result[i] = first[i];
            for (int i = 0; i < restCount; i++)
                result[firstCount + i] = rest[i];
            return result;
        }

        private static string SanitizeIdPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
