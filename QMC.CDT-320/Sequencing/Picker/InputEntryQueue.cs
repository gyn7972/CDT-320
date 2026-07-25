using System;
using System.Collections.Generic;
using System.Threading;

namespace QMC.CDT320.Sequencing
{
    internal enum InputEntryKind
    {
        PreInspection,
        PickUp
    }

    /// <summary>
    /// Input 진입(선행검사 카메라존 admission)을 하나의 전역 순서로 직렬화하는 진짜 FIFO 큐.
    /// 단일 Interlocked 단조 티켓으로 '하나의 전역 순서'만 만든다 — 살아있는 티켓 집합의 최소값은
    /// 항상 유일하므로 head(IsHead==true)는 정확히 1개다. 따라서 두 대기자가 동시에 '상대가 앞섰다'로
    /// 판정하는 상호 양보(라이브락)가 구조적으로 불가능하다.
    /// 이전 실패(side별 두 seq 카운터 pairwise 비교 → mine=none이면 양쪽 다 상대가 foreign)를 대체한다.
    /// 자체 lock을 최내측으로만 사용하고, InputCameraPickUpPermissionStore.Sync나
    /// AutoSequenceCoordinatorGate._pickerWorkZoneGate 안에서 호출돼도 이 큐는 다른 lock을 잡지 않아
    /// 중첩 lock 데드락이 발생하지 않는다.
    /// </summary>
    internal static class InputEntryQueue
    {
        private sealed class Entry
        {
            public long Ticket;
            public PickerSequenceSide Side;
            public InputEntryKind Kind;
            public DateTime EnqueuedAt;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<PickerSequenceSide, Entry> Entries =
            new Dictionary<PickerSequenceSide, Entry>();
        private static long _ticketCounter;

        /// <summary>
        /// 이 side의 진입 티켓을 발급한다. 멱등 — 이미 티켓이 있으면 기존 티켓(순서)을 그대로 유지한다.
        /// 카메라존 양보 대기(선행검사 Task 내부)보다 반드시 먼저 호출돼, 대기 중인 주체는 항상 자기
        /// 티켓을 보유한다(mine=none 병리 제거).
        /// </summary>
        public static long Enqueue(PickerSequenceSide side, InputEntryKind kind)
        {
            lock (Sync)
            {
                Entry existing;
                if (Entries.TryGetValue(side, out existing) && existing != null)
                    return existing.Ticket;

                long ticket = Interlocked.Increment(ref _ticketCounter);
                Entries[side] = new Entry
                {
                    Ticket = ticket,
                    Side = side,
                    Kind = kind,
                    EnqueuedAt = DateTime.Now
                };
                return ticket;
            }
        }

        /// <summary>
        /// 이 side의 티켓이 살아있는 티켓 중 최소(=큐 맨 앞)인가. 순수 읽기 — 외부 lock/콜백을 잡지
        /// 않는다. 살아있는 티켓 최소값이 유일하므로 IsHead==true인 side는 임의 시점에 최대 1개다.
        /// </summary>
        public static bool IsHead(PickerSequenceSide side, out string detail)
        {
            lock (Sync)
            {
                detail = string.Empty;
                Entry mine;
                if (!Entries.TryGetValue(side, out mine) || mine == null)
                {
                    detail = "no ticket. side=" + side + ", queue=" + DescribeNoLock();
                    return false;
                }

                foreach (KeyValuePair<PickerSequenceSide, Entry> pair in Entries)
                {
                    if (pair.Key == side)
                        continue;

                    Entry other = pair.Value;
                    if (other != null && other.Ticket < mine.Ticket)
                    {
                        detail = "head=" + pair.Key + ":ticket=" + other.Ticket +
                                 ", mine=" + side + ":ticket=" + mine.Ticket;
                        return false;
                    }
                }

                detail = "head=" + side + ":ticket=" + mine.Ticket;
                return true;
            }
        }

        /// <summary>이 side의 진입 티켓을 반납한다(큐에서 제거). 없으면 무해.</summary>
        public static void Dequeue(PickerSequenceSide side)
        {
            lock (Sync)
            {
                Entries.Remove(side);
            }
        }

        public static bool HasTicket(PickerSequenceSide side)
        {
            lock (Sync)
            {
                return Entries.ContainsKey(side);
            }
        }

        public static string Describe()
        {
            lock (Sync)
            {
                return DescribeNoLock();
            }
        }

        private static string DescribeNoLock()
        {
            if (Entries.Count == 0)
                return "empty";

            var parts = new List<string>();
            foreach (KeyValuePair<PickerSequenceSide, Entry> pair in Entries)
            {
                Entry e = pair.Value;
                if (e != null)
                    parts.Add(pair.Key + ":ticket=" + e.Ticket + ",kind=" + e.Kind);
            }

            return string.Join(";", parts.ToArray());
        }
    }
}
