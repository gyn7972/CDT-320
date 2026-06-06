using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Jobs
{
    /// <summary>
    /// Job 큐 + 이력 — 310 의 Job 시스템 단순화 버전 (글로벌 정적).
    /// </summary>
    public static class JobQueue
    {
        private static readonly ConcurrentQueue<JobOrder> _pending = new ConcurrentQueue<JobOrder>();
        private static readonly List<JobOrder>            _history = new List<JobOrder>();
        private static readonly object _historyLock = new object();
        private const int MaxHistory = 5000;

        public static event Action<JobOrder> JobStateChanged;

        public static int PendingCount => _pending.Count;
        public static int HistoryCount { get { lock (_historyLock) return _history.Count; } }

        // ── Pending ──
        public static void Enqueue(JobOrder job)
        {
            if (job == null) return;
            _pending.Enqueue(job);
            Raise(job);
        }

        public static bool TryDequeue(out JobOrder job) => _pending.TryDequeue(out job);

        // ── Lifecycle ──
        public static void MarkRunning(JobOrder job)
        {
            if (job == null) return;
            job.State = JobState.Running;
            job.StartedAt = DateTime.Now;
            Raise(job);
        }

        public static void MarkDone(JobOrder job, string note = "")
        {
            if (job == null) return;
            job.State = JobState.Done;
            job.FinishedAt = DateTime.Now;
            if (!string.IsNullOrEmpty(note)) job.Note = note;
            AddHistory(job);
            Raise(job);
        }

        public static void MarkFailed(JobOrder job, string reason)
        {
            if (job == null) return;
            job.State = JobState.Failed;
            job.FinishedAt = DateTime.Now;
            job.ErrorReason = reason ?? "";
            AddHistory(job);
            Raise(job);
        }

        public static void MarkCancelled(JobOrder job)
        {
            if (job == null) return;
            job.State = JobState.Cancelled;
            job.FinishedAt = DateTime.Now;
            AddHistory(job);
            Raise(job);
        }

        // ── History ──
        public static IReadOnlyList<JobOrder> Snapshot()
        {
            lock (_historyLock) return _history.ToList();
        }

        public static int CountByType(JobType type)
        {
            lock (_historyLock)
                return _history.Count(j => j.Type == type);
        }

        public static int CountByState(JobState state)
        {
            lock (_historyLock)
                return _history.Count(j => j.State == state);
        }

        public static void Clear()
        {
            while (_pending.TryDequeue(out _)) { }
            lock (_historyLock) _history.Clear();
        }

        private static void AddHistory(JobOrder j)
        {
            lock (_historyLock)
            {
                _history.Add(j);
                if (_history.Count > MaxHistory)
                    _history.RemoveRange(0, _history.Count - MaxHistory);
            }
        }

        private static void Raise(JobOrder j)
        {
            try { JobStateChanged?.Invoke(j); } catch { }
        }
    }
}
