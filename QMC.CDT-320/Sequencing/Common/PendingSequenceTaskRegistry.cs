using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>취소/타임아웃으로 호출자가 먼저 끝나도 실제 작업이 반환할 때까지 추적합니다.</summary>
    internal static class PendingSequenceTaskRegistry
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<Task, string> Pending = new Dictionary<Task, string>();

        internal static void Track(Task task, string source)
        {
            if (task == null || task.IsCompleted)
                return;
            lock (Sync)
            {
                RemoveCompletedNoLock();
                Pending[task] = string.IsNullOrWhiteSpace(source) ? "Sequence" : source;
            }
        }

        /// <summary>잠금 안에서 Task 대기, 작업 취소, Controller/Material 접근은 하지 않습니다.</summary>
        internal static bool TryGetPending(out string reason)
        {
            lock (Sync)
            {
                RemoveCompletedNoLock();
                if (Pending.Count == 0)
                {
                    reason = string.Empty;
                    return false;
                }
                reason = "이전 시퀀스 작업의 실제 종료가 확인되지 않았습니다. pending=" + Pending.Count +
                    ", source=" + string.Join(", ", Pending.Values.Distinct().Take(4));
                return true;
            }
        }

        private static void RemoveCompletedNoLock()
        {
            foreach (Task completed in Pending.Keys.Where(task => task.IsCompleted).ToArray())
                Pending.Remove(completed);
        }
    }
}
