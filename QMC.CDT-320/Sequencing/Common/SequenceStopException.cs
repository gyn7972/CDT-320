using System;

namespace QMC.CDT320.Sequencing
{
    public sealed class SequenceStopException : Exception
    {
        public SequenceStopException(string reason)
            : base(reason ?? "Sequence stopped.")
        {
        }

        public static bool IsCycleStopMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            return message.IndexOf("CYCLE STOP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("Cycle Stop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("현재 작업 경계에서 정지", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsSequenceStop(Exception ex)
        {
            if (ex == null)
                return false;

            if (ex is SequenceStopException)
                return true;

            AggregateException aggregate = ex as AggregateException;
            if (aggregate != null)
            {
                foreach (Exception inner in aggregate.Flatten().InnerExceptions)
                {
                    if (IsSequenceStop(inner))
                        return true;
                }
            }

            if (IsCycleStopMessage(ex.Message))
                return true;

            return ex.InnerException != null && IsSequenceStop(ex.InnerException);
        }

        public static string ResolveReason(Exception ex)
        {
            if (ex == null)
                return "CYCLE STOP 요청으로 현재 작업 경계에서 정지합니다.";

            SequenceStopException stop = ex as SequenceStopException;
            if (stop != null && !string.IsNullOrWhiteSpace(stop.Message))
                return stop.Message;

            AggregateException aggregate = ex as AggregateException;
            if (aggregate != null)
            {
                foreach (Exception inner in aggregate.Flatten().InnerExceptions)
                {
                    if (IsSequenceStop(inner))
                        return ResolveReason(inner);
                }
            }

            if (IsCycleStopMessage(ex.Message))
                return ex.Message;

            return ex.InnerException != null ? ResolveReason(ex.InnerException) : "CYCLE STOP 요청으로 현재 작업 경계에서 정지합니다.";
        }
    }
}
