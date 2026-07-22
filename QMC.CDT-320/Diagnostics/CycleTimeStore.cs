using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace QMC.CDT320.Diagnostics
{
    /// <summary>사이클 내 모터(축) 동작 1구간 — 시작/종료 상대 오프셋(ms). EndMs=-1이면 진행 중.</summary>
    public sealed class CycleMotionSegment
    {
        public string Axis;
        public double StartMs;
        public double EndMs = -1;

        public CycleMotionSegment Clone()
        {
            return (CycleMotionSegment)MemberwiseClone();
        }
    }

    /// <summary>
    /// 운전 사이클 1건의 Cycle Time 계측 항목.
    /// 시각은 Stopwatch tick 기준(StartTick) + 상대 오프셋(ms). 미기록 = -1.
    /// 비전 원본과 달리 고정 단계 슬롯 대신 모터 동작 세그먼트 리스트를 보관한다(사용자 지시).
    /// </summary>
    public sealed class CycleTimeEntry
    {
        public long Seq;
        public string Unit;        // 행 분류 키: INPUTVISION/PICKUP/BOTTOM/SIDE/PLACE
        public string Head;        // "FRONT"/"REAR"
        public int HeadIndex;      // 피커 번호 1~4
        public int DieIndex;       // 다이 번호 (없으면 -1)
        public string Motion;      // 부가 정보(다이 ID 등)
        public string RequestId;   // 사이클 고유키
        public string GroupId;
        public DateTime StartUtc;
        public long StartTick;
        public List<CycleMotionSegment> Motions = new List<CycleMotionSegment>();
        public double ResultMs = -1;   // 총 소요(완료 시각). 미완료 -1.
        public bool Failed;

        public double TotalMs
        {
            get { return ResultMs; }
        }

        public bool IsCompleted
        {
            get { return ResultMs >= 0; }
        }

        /// <summary>픽커 번호 — FRONT n→n(1~4), REAR n→n+4(5~8). HEAD 정보 없으면 0.</summary>
        public int Picker
        {
            get
            {
                if (HeadIndex < 1 || HeadIndex > 4) return 0;
                return string.Equals(Head, "REAR", StringComparison.OrdinalIgnoreCase)
                    ? HeadIndex + 4 : HeadIndex;
            }
        }

        public CycleTimeEntry Clone()
        {
            CycleTimeEntry clone = (CycleTimeEntry)MemberwiseClone();
            clone.Motions = new List<CycleMotionSegment>(Motions.Count);
            for (int i = 0; i < Motions.Count; i++)
            {
                CycleMotionSegment seg = Motions[i];
                if (seg != null)
                    clone.Motions.Add(seg.Clone());
            }
            return clone;
        }
    }

    /// <summary>
    /// 실시간 Cycle Time 링버퍼 — 시퀀스 훅이 기록하고 간트 UI가 Snapshot으로 조회한다.
    /// 운전 경로 영향 최소화: 모든 공개 API는 내부 try/catch로 무해, lock은 짧게.
    /// </summary>
    public static class CycleTimeStore
    {
        /// <summary>최대 보존 건수(초과 시 오래된 항목부터 제거).</summary>
        public static int MaxEntries { get; set; }

        /// <summary>보존 시간 — 지난 완료 항목은 정리에서 제거.</summary>
        public static TimeSpan Retention { get; set; }

        private const int MaxMotionsPerEntry = 64;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, CycleTimeEntry> Active =
            new Dictionary<string, CycleTimeEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<CycleTimeEntry> Ring = new List<CycleTimeEntry>();
        private static long _seq;

        static CycleTimeStore()
        {
            MaxEntries = 2000;
            Retention = TimeSpan.FromMinutes(10);
        }

        /// <summary>사이클 시작(항목 생성). 시작 없이 온 다른 마크는 전부 버려진다(중간 합류 방지).</summary>
        public static void MarkStart(string unit, string requestId, string head, int headIndex, int dieIndex, string motionInfo)
        {
            if (string.IsNullOrEmpty(unit) || string.IsNullOrEmpty(requestId)) return;
            try
            {
                long nowTick = Stopwatch.GetTimestamp();
                lock (Sync)
                {
                    string key = BuildKey(unit, requestId);
                    if (Active.ContainsKey(key))
                        return;   // 중복 시작은 최초 항목 유지

                    var entry = new CycleTimeEntry
                    {
                        Seq = ++_seq,
                        Unit = unit,
                        Head = head ?? "",
                        HeadIndex = headIndex,
                        DieIndex = dieIndex,
                        Motion = motionInfo ?? "",
                        RequestId = requestId,
                        GroupId = "",
                        StartUtc = DateTime.UtcNow,
                        StartTick = nowTick
                    };
                    Active[key] = entry;
                    Ring.Add(entry);
                    TrimLocked();
                }
            }
            catch { }
        }

        /// <summary>모터 동작 시작 — 같은 축의 미종료 세그먼트가 있으면 무시(중복 발화 방어).</summary>
        public static void MarkMotionStart(string unit, string requestId, string axis)
        {
            if (string.IsNullOrEmpty(axis)) return;
            try
            {
                long nowTick = Stopwatch.GetTimestamp();
                lock (Sync)
                {
                    CycleTimeEntry entry = FindActiveLocked(unit, requestId);
                    if (entry == null || entry.Motions.Count >= MaxMotionsPerEntry) return;

                    for (int i = entry.Motions.Count - 1; i >= 0; i--)
                    {
                        CycleMotionSegment open = entry.Motions[i];
                        if (open.EndMs < 0 && string.Equals(open.Axis, axis, StringComparison.OrdinalIgnoreCase))
                            return;   // 이미 진행 중인 동일 축 세그먼트 → 최초값 유지
                    }

                    entry.Motions.Add(new CycleMotionSegment
                    {
                        Axis = axis,
                        StartMs = TickToMs(nowTick - entry.StartTick)
                    });
                }
            }
            catch { }
        }

        /// <summary>모터 동작 종료 — 같은 축의 마지막 미종료 세그먼트를 닫는다. 없으면 무시.</summary>
        public static void MarkMotionEnd(string unit, string requestId, string axis)
        {
            if (string.IsNullOrEmpty(axis)) return;
            try
            {
                long nowTick = Stopwatch.GetTimestamp();
                lock (Sync)
                {
                    CycleTimeEntry entry = FindActiveLocked(unit, requestId);
                    if (entry == null) return;

                    for (int i = entry.Motions.Count - 1; i >= 0; i--)
                    {
                        CycleMotionSegment open = entry.Motions[i];
                        if (open.EndMs < 0 && string.Equals(open.Axis, axis, StringComparison.OrdinalIgnoreCase))
                        {
                            open.EndMs = TickToMs(nowTick - entry.StartTick);
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>정상 종결 — 총시간 기록 + Active 제거. 미종료 세그먼트는 종결 시각으로 닫는다.</summary>
        public static void MarkResult(string unit, string requestId)
        {
            Complete(unit, requestId, false);
        }

        /// <summary>실패 종결(ERR) — Failed=true + 종료 시각 기록 + Active 제거.</summary>
        public static void MarkError(string unit, string requestId)
        {
            Complete(unit, requestId, true);
        }

        /// <summary>
        /// 초크 포인트용: 키가 prefix로 시작하는 활성 항목을 전부 ERR 종결한다.
        /// (시퀀스 실패 반환/catch 한 곳에서 호출 — 분기 전수 삽입 대신, 사용자 승인 방식.)
        /// </summary>
        public static void MarkErrorAllActive(string keyPrefix)
        {
            if (string.IsNullOrEmpty(keyPrefix)) return;
            try
            {
                long nowTick = Stopwatch.GetTimestamp();
                lock (Sync)
                {
                    List<string> keys = null;
                    foreach (KeyValuePair<string, CycleTimeEntry> pair in Active)
                    {
                        if (!pair.Key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (keys == null)
                            keys = new List<string>();
                        keys.Add(pair.Key);
                    }

                    if (keys == null) return;
                    for (int i = 0; i < keys.Count; i++)
                        CompleteLocked(keys[i], nowTick, true);
                }
            }
            catch { }
        }

        /// <summary>현재 보존 항목 스냅샷(복사본) — UI 페인트용. Seq 오름차순.</summary>
        public static List<CycleTimeEntry> Snapshot()
        {
            try
            {
                lock (Sync)
                {
                    CleanupLocked();
                    var list = new List<CycleTimeEntry>(Ring.Count);
                    foreach (CycleTimeEntry entry in Ring)
                        list.Add(entry.Clone());
                    return list;
                }
            }
            catch
            {
                return new List<CycleTimeEntry>();
            }
        }

        public static void Clear()
        {
            try
            {
                lock (Sync) { Active.Clear(); Ring.Clear(); }
            }
            catch { }
        }

        private static void Complete(string unit, string requestId, bool failed)
        {
            if (string.IsNullOrEmpty(unit) || string.IsNullOrEmpty(requestId)) return;
            try
            {
                long nowTick = Stopwatch.GetTimestamp();
                lock (Sync)
                {
                    CompleteLocked(BuildKey(unit, requestId), nowTick, failed);
                }
            }
            catch { }
        }

        private static void CompleteLocked(string key, long nowTick, bool failed)
        {
            CycleTimeEntry entry;
            if (!Active.TryGetValue(key, out entry))
                return;

            double ms = TickToMs(nowTick - entry.StartTick);
            if (entry.ResultMs < 0)
                entry.ResultMs = ms;   // 중복 종결은 최초값 유지
            if (failed)
                entry.Failed = true;

            for (int i = 0; i < entry.Motions.Count; i++)
            {
                if (entry.Motions[i].EndMs < 0)
                    entry.Motions[i].EndMs = entry.ResultMs;
            }

            Active.Remove(key);
        }

        private static CycleTimeEntry FindActiveLocked(string unit, string requestId)
        {
            if (string.IsNullOrEmpty(unit) || string.IsNullOrEmpty(requestId)) return null;
            CycleTimeEntry entry;
            return Active.TryGetValue(BuildKey(unit, requestId), out entry) ? entry : null;
        }

        private static string BuildKey(string unit, string requestId)
        {
            return unit + "|" + requestId;
        }

        private static double TickToMs(long ticks)
        {
            return ticks * 1000.0 / Stopwatch.Frequency;
        }

        private static void TrimLocked()
        {
            int excess = Ring.Count - MaxEntries;
            if (excess <= 0) return;
            for (int i = 0; i < excess; i++)
            {
                CycleTimeEntry old = Ring[i];
                Active.Remove(BuildKey(old.Unit, old.RequestId));
            }
            Ring.RemoveRange(0, excess);
        }

        private static void CleanupLocked()
        {
            DateTime cut = DateTime.UtcNow - Retention;
            int remove = 0;
            while (remove < Ring.Count && Ring[remove].StartUtc < cut && Ring[remove].IsCompleted)
                remove++;
            if (remove > 0)
            {
                for (int i = 0; i < remove; i++)
                    Active.Remove(BuildKey(Ring[i].Unit, Ring[i].RequestId));
                Ring.RemoveRange(0, remove);
            }
        }
    }
}
