using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Logging;

namespace QMC.CDT320.VisionComm
{
    /// <summary>비전이 자발 푸시한 MRESULT/RESULT 1건 — 수신 원문과 파싱 응답, 수신 시각을 보관한다.</summary>
    public sealed class VisionInspectionResultEntry
    {
        public VisionInspectionResultEntry(
            string camera,
            string command,
            string groupId,
            VisionProtocolResponse response)
        {
            Camera = (camera ?? string.Empty).Trim();
            Command = (command ?? string.Empty).Trim();
            GroupId = (groupId ?? string.Empty).Trim();
            Response = response;
            RawLine = response != null ? response.RawLine : string.Empty;
            ReceivedAtUtc = DateTime.UtcNow;
            ReceivedTimestamp = Stopwatch.GetTimestamp();
        }

        public string Camera { get; private set; }
        public string Command { get; private set; }
        public string GroupId { get; private set; }
        public VisionProtocolResponse Response { get; private set; }
        public string RawLine { get; private set; }
        public DateTime ReceivedAtUtc { get; private set; }
        public long ReceivedTimestamp { get; private set; }
    }

    /// <summary>
    /// Vision → 핸들러 검사 결과(MRESULT/RESULT) 푸시 수신 스토어.
    /// <para>핸들러는 결과를 더 이상 요청(Pull)하지 않는다. 비전이 각 단계 완료 즉시 자발 푸시한
    /// 결과 라인을 수신 루프(<see cref="VisionTcpClient"/>)가 이 스토어에 보관하고,
    /// 소비 시퀀스는 필요 시점에 조회(있으면 즉시 소비) 또는 도착 대기(타임아웃)한다 —
    /// Bottom XYT 푸시(<see cref="BottomXytStore"/>) 패턴을 MRESULT/RESULT로 일반화한 것.</para>
    /// <para>스토어는 전역 static 1개이며 (camera, command, group_id) 복합 키를 쓴다 —
    /// 결과가 BOTTOM/SIDE/BIN/WAFER 5개 카메라 채널에 동일 규약으로 도착하므로
    /// 채널별 인스턴스보다 전역+채널 키가 단순하다 (BottomXytStore가 Bottom 단일 채널 전역
    /// static인 것과 일관). 스레드 안전: 수신 루프(Add)와 시퀀스(Consume/Wait)가 동시 접근.</para>
    /// </summary>
    public static class VisionInspectionResultStore
    {
        // 미소비 항목 보관 한도. 초과 시 가장 오래된 항목(FIFO)부터 제거한다.
        private const int MaxUnconsumedEntries = 500;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, VisionInspectionResultEntry> Entries =
            new Dictionary<string, VisionInspectionResultEntry>(StringComparer.OrdinalIgnoreCase);
        // Add 순서 보존용 키 큐 (소비/교체된 키는 잔류할 수 있어 제거 시 사전 존재 여부로 걸러낸다).
        private static readonly Queue<string> InsertionOrder = new Queue<string>();
        private static readonly Dictionary<string, List<TaskCompletionSource<VisionInspectionResultEntry>>> Waiters =
            new Dictionary<string, List<TaskCompletionSource<VisionInspectionResultEntry>>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>미소비 항목 수 (진단/로그용).</summary>
        public static int Count
        {
            get { lock (Sync) return Entries.Count; }
        }

        /// <summary>
        /// 수신 루프 전용: 푸시 결과 1건을 보관한다.
        /// 같은 키로 대기 중인 소비자가 있으면 스토어에 넣지 않고 대기자에게 즉시 전달(소비)한다.
        /// 동일 키 중복 수신 시 최신 값으로 교체하고 로그를 남긴다.
        /// </summary>
        public static void Add(VisionInspectionResultEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.GroupId))
                return;

            string key = BuildKey(entry.Camera, entry.Command, entry.GroupId);
            TaskCompletionSource<VisionInspectionResultEntry> waiter = null;
            bool replaced = false;
            int remainCount;

            lock (Sync)
            {
                List<TaskCompletionSource<VisionInspectionResultEntry>> waiterList;
                if (Waiters.TryGetValue(key, out waiterList) && waiterList.Count > 0)
                {
                    waiter = waiterList[0];
                    waiterList.RemoveAt(0);
                    if (waiterList.Count == 0)
                        Waiters.Remove(key);
                }
                else
                {
                    replaced = Entries.ContainsKey(key);
                    Entries[key] = entry;
                    if (!replaced)
                        InsertionOrder.Enqueue(key);
                    EvictOverLimitLocked();
                }

                remainCount = Entries.Count;
            }

            if (waiter != null)
            {
                waiter.TrySetResult(entry);
                EventLogger.Write(EventKind.Event, "VISION", "VISION-PUSH-RESULT-RX",
                    "Vision 결과 푸시를 대기 중 소비자에게 즉시 전달했습니다. camera=" + entry.Camera +
                    ", command=" + entry.Command +
                    ", groupId=" + entry.GroupId +
                    ", storeCount=" + remainCount);
                return;
            }

            if (replaced)
            {
                EventLogger.Write(EventKind.Warning, "VISION", "VISION-PUSH-RESULT-RX",
                    "동일 키의 Vision 결과 푸시가 중복 수신되어 최신 값으로 교체했습니다. camera=" + entry.Camera +
                    ", command=" + entry.Command +
                    ", groupId=" + entry.GroupId +
                    ", storeCount=" + remainCount);
                return;
            }

            EventLogger.Write(EventKind.Event, "VISION", "VISION-PUSH-RESULT-RX",
                "Vision 결과 푸시를 수신 스토어에 보관했습니다. camera=" + entry.Camera +
                ", command=" + entry.Command +
                ", groupId=" + entry.GroupId +
                ", storeCount=" + remainCount);
        }

        /// <summary>조회 성공 시 스토어에서 제거(소비 후 제거)한다.</summary>
        public static bool TryConsume(string camera, string command, string groupId, out VisionInspectionResultEntry entry)
        {
            string key = BuildKey(camera, command, groupId);
            lock (Sync)
            {
                if (!Entries.TryGetValue(key, out entry) || entry == null)
                {
                    entry = null;
                    return false;
                }

                Entries.Remove(key);
                return true;
            }
        }

        /// <summary>
        /// 있으면 즉시 소비, 없으면 도착을 타임아웃까지 대기한다 (재요청 없음).
        /// 등록과 재조회를 같은 lock 안에서 수행해 "등록 직전 도착" 경합이 없다.
        /// 타임아웃/취소 시 null을 반환한다 (취소는 OperationCanceledException 전파).
        /// </summary>
        public static async Task<VisionInspectionResultEntry> WaitAndConsumeAsync(
            string camera,
            string command,
            string groupId,
            int timeoutMs,
            CancellationToken ct)
        {
            string key = BuildKey(camera, command, groupId);
            TaskCompletionSource<VisionInspectionResultEntry> waiter;

            lock (Sync)
            {
                VisionInspectionResultEntry existing;
                if (Entries.TryGetValue(key, out existing) && existing != null)
                {
                    Entries.Remove(key);
                    return existing;
                }

                waiter = new TaskCompletionSource<VisionInspectionResultEntry>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                List<TaskCompletionSource<VisionInspectionResultEntry>> waiterList;
                if (!Waiters.TryGetValue(key, out waiterList))
                {
                    waiterList = new List<TaskCompletionSource<VisionInspectionResultEntry>>();
                    Waiters.Add(key, waiterList);
                }
                waiterList.Add(waiter);
            }

            try
            {
                int safeTimeoutMs = Math.Max(1, timeoutMs);
                Task delay = Task.Delay(safeTimeoutMs, ct);
                Task completed = await Task.WhenAny(waiter.Task, delay).ConfigureAwait(false);
                if (completed == waiter.Task)
                    return await waiter.Task.ConfigureAwait(false);

                // 타임아웃 경계에서 Add가 동시에 완료시킬 수 있으므로 제거 후 한 번 더 확인한다.
                RemoveWaiter(key, waiter);
                if (waiter.Task.IsCompleted && !waiter.Task.IsFaulted && !waiter.Task.IsCanceled)
                    return await waiter.Task.ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();
                return null;
            }
            catch (OperationCanceledException)
            {
                RemoveWaiter(key, waiter);
                throw;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 연결 단절 시 해당 카메라 채널의 미소비 항목을 전부 제거하고 대기자에게 오류를 통지한다.
        /// 재연결 후 이전 검사 결과는 무효라는 정책.
        /// </summary>
        public static void ClearChannel(string camera, string reason)
        {
            string prefix = (camera ?? string.Empty).Trim() + "\u001f";
            var failedWaiters = new List<TaskCompletionSource<VisionInspectionResultEntry>>();
            int removedCount = 0;

            lock (Sync)
            {
                var removeKeys = new List<string>();
                foreach (KeyValuePair<string, VisionInspectionResultEntry> item in Entries)
                {
                    if (item.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        removeKeys.Add(item.Key);
                }
                for (int i = 0; i < removeKeys.Count; i++)
                    Entries.Remove(removeKeys[i]);
                removedCount = removeKeys.Count;

                var waiterKeys = new List<string>();
                foreach (KeyValuePair<string, List<TaskCompletionSource<VisionInspectionResultEntry>>> item in Waiters)
                {
                    if (!item.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        continue;
                    failedWaiters.AddRange(item.Value);
                    waiterKeys.Add(item.Key);
                }
                for (int i = 0; i < waiterKeys.Count; i++)
                    Waiters.Remove(waiterKeys[i]);
            }

            for (int i = 0; i < failedWaiters.Count; i++)
                failedWaiters[i].TrySetException(new InvalidOperationException(
                    reason ?? ("Vision 연결이 종료되어 결과 대기를 중단합니다. camera=" + camera)));

            if (removedCount > 0 || failedWaiters.Count > 0)
            {
                EventLogger.Write(EventKind.Warning, "VISION", "VISION-PUSH-RESULT-CLEAR",
                    "Vision 연결 단절로 채널의 미소비 결과를 정리했습니다. camera=" + (camera ?? string.Empty) +
                    ", removedEntries=" + removedCount +
                    ", failedWaiters=" + failedWaiters.Count +
                    ", reason=" + (reason ?? string.Empty));
            }
        }

        /// <summary>전체 초기화 (랏 경계/테스트용). 대기자에게 오류를 통지한다.</summary>
        public static void Clear(string reason)
        {
            var failedWaiters = new List<TaskCompletionSource<VisionInspectionResultEntry>>();
            int removedCount;

            lock (Sync)
            {
                removedCount = Entries.Count;
                Entries.Clear();
                InsertionOrder.Clear();
                foreach (List<TaskCompletionSource<VisionInspectionResultEntry>> list in Waiters.Values)
                    failedWaiters.AddRange(list);
                Waiters.Clear();
            }

            for (int i = 0; i < failedWaiters.Count; i++)
                failedWaiters[i].TrySetException(new InvalidOperationException(
                    reason ?? "Vision 결과 스토어가 초기화되었습니다."));

            if (removedCount > 0 || failedWaiters.Count > 0)
            {
                EventLogger.Write(EventKind.Warning, "VISION", "VISION-PUSH-RESULT-CLEAR",
                    "Vision 결과 스토어 전체를 초기화했습니다. removedEntries=" + removedCount +
                    ", failedWaiters=" + failedWaiters.Count +
                    ", reason=" + (reason ?? string.Empty));
            }
        }

        // ── 내부 구현 (Sync lock 안에서만 호출) ─────────────────────

        private static void EvictOverLimitLocked()
        {
            while (Entries.Count > MaxUnconsumedEntries && InsertionOrder.Count > 0)
            {
                string oldestKey = InsertionOrder.Dequeue();
                VisionInspectionResultEntry oldest;
                if (!Entries.TryGetValue(oldestKey, out oldest))
                    continue;   // 이미 소비/교체된 키 — 스킵.

                Entries.Remove(oldestKey);
                EventLogger.Write(EventKind.Warning, "VISION", "VISION-PUSH-RESULT-EVICT",
                    "Vision 결과 스토어 보관 한도(" + MaxUnconsumedEntries + ")를 초과해 가장 오래된 미소비 항목을 제거했습니다. " +
                    "camera=" + oldest.Camera +
                    ", command=" + oldest.Command +
                    ", groupId=" + oldest.GroupId +
                    ", receivedAtUtc=" + oldest.ReceivedAtUtc.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
        }

        private static void RemoveWaiter(string key, TaskCompletionSource<VisionInspectionResultEntry> waiter)
        {
            lock (Sync)
            {
                List<TaskCompletionSource<VisionInspectionResultEntry>> waiterList;
                if (!Waiters.TryGetValue(key, out waiterList))
                    return;
                waiterList.Remove(waiter);
                if (waiterList.Count == 0)
                    Waiters.Remove(key);
            }
        }

        private static string BuildKey(string camera, string command, string groupId)
        {
            return (camera ?? string.Empty).Trim() + "\u001f" +
                   (command ?? string.Empty).Trim() + "\u001f" +
                   (groupId ?? string.Empty).Trim();
        }
    }
}
