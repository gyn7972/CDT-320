using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    // §4 크로스-픽커 시작/재시작 우선순위 게이트.
    //
    // 목적: 신규 시작·정지 후 재시작 시 두 픽커가 "동시에" 첫 전진(Y forward)에 돌입하지 않고
    //       완료에 가까운 쪽(Place > Bottom/Side > PickUp)부터 "한 번에 한 픽커씩" 첫 전진하도록 순서를 준다.
    //
    // 범위: run(1회 StartSequence) 단위로 side별 "첫 전진"만 게이트한다. 각 die 사이클은 새 PickerProcessSequence
    //       인스턴스이지만 done 집합이 run 동안 유지되므로, 두 번째 die부터는 즉시 통과한다(정상 생산 UPH 영향 없음).
    //
    // 안전: 이 게이트는 순서 최적화이며 최종 충돌 방지는 기존 상대 PickerY Avoid 대기 게이트와
    //       RealtimeCollisionSupervisor(전축 하드정지)가 담당한다. 데드락 방지를 위해 우선순위 승자는 항상 결정되고,
    //       상대가 참여하지 않으면 settle 시간 후 단독 진행한다.
    internal static class PickerFirstForwardSequencer
    {
        // Place=3 > Bottom/Side=2 > PickUp/Mark=1. (사용자 정책: Bottom/Side는 하나로 취급)
        public const int RankPlace = 3;
        public const int RankBottomSide = 2;
        public const int RankPickUp = 1;

        private const int PollMs = 20;
        private const int WaitLogThrottleMs = 1000;

        private static readonly object Sync = new object();
        private static readonly HashSet<PickerSequenceSide> Expected =
            new HashSet<PickerSequenceSide>();
        private static readonly Dictionary<PickerSequenceSide, int> Registered =
            new Dictionary<PickerSequenceSide, int>();
        private static readonly HashSet<PickerSequenceSide> Done =
            new HashSet<PickerSequenceSide>();
        private static readonly Dictionary<PickerSequenceSide, int> ResumeDrainRanks =
            new Dictionary<PickerSequenceSide, int>();
        private static readonly HashSet<PickerSequenceSide> ResumeDrainDone =
            new HashSet<PickerSequenceSide>();
        private static PickerSequenceSide? _holder;
        private static PickerSequenceSide? _resumeDrainHolder;

        // run 시작(StartSequence) 시 1회 호출. 이전 run의 순서 상태를 초기화한다.
        public static void BeginRun()
        {
            lock (Sync)
            {
                Registered.Clear();
                Done.Clear();
                Expected.Clear();
                ResumeDrainRanks.Clear();
                ResumeDrainDone.Clear();
                _holder = null;
                _resumeDrainHolder = null;
            }
        }

        public static void ConfigureActiveSides(bool frontActive, bool rearActive)
        {
            lock (Sync)
            {
                Expected.Clear();
                if (frontActive)
                    Expected.Add(PickerSequenceSide.Front);
                if (rearActive)
                    Expected.Add(PickerSequenceSide.Rear);
            }
        }

        public static void ConfigureResumeDrain(
            bool frontRequired,
            int frontRank,
            bool rearRequired,
            int rearRank)
        {
            lock (Sync)
            {
                ResumeDrainRanks.Clear();
                ResumeDrainDone.Clear();
                _resumeDrainHolder = null;

                if (frontRequired)
                    ResumeDrainRanks[PickerSequenceSide.Front] = frontRank;
                if (rearRequired)
                    ResumeDrainRanks[PickerSequenceSide.Rear] = rearRank;

                if (ResumeDrainRanks.Count > 0)
                    ConfigureExpectedForNextResumeDrainNoLock(false);
            }
        }

        public static bool IsResumeDrainRequired(PickerSequenceSide side)
        {
            lock (Sync)
            {
                return ResumeDrainRanks.ContainsKey(side) && !ResumeDrainDone.Contains(side);
            }
        }

        public static async Task<bool> WaitResumeDrainTurnAsync(
            PickerSequenceSide side,
            MachineSequenceContext ctx,
            Action<string> log,
            CancellationToken ct)
        {
            DateTime lastWaitLog = DateTime.MinValue;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (ctx != null)
                    ctx.StopIfCycleStopRequested(
                        "PickerResumeDrainSequencer:" + side,
                        ShouldDeferCycleStopForPickerDrain(side, ctx),
                        "Picker target die resume drain");

                bool ownDrain;
                int ownRank;
                string waitReason;
                lock (Sync)
                {
                    ownDrain = ResumeDrainRanks.TryGetValue(side, out ownRank) &&
                               !ResumeDrainDone.Contains(side);

                    if (ResumeDrainRanks.Count == 0 || AllResumeDrainDoneNoLock())
                        return false;

                    if (ownDrain)
                    {
                        if (_resumeDrainHolder == side)
                            return true;

                        if (_resumeDrainHolder == null && IsHighestResumeDrainPriorityNoLock(side))
                        {
                            _resumeDrainHolder = side;
                            ConfigureFirstForwardExpectedNoLock(side);
                            return true;
                        }
                    }

                    waitReason = BuildResumeDrainWaitReasonNoLock(side, ownDrain, ownRank);
                }

                if (log != null && (DateTime.UtcNow - lastWaitLog).TotalMilliseconds >= WaitLogThrottleMs)
                {
                    lastWaitLog = DateTime.UtcNow;
                    log(side + " resume drain 대기. " + waitReason);
                }

                await Task.Delay(PollMs, ct).ConfigureAwait(false);
            }
        }

        public static void CompleteResumeDrain(PickerSequenceSide side)
        {
            lock (Sync)
            {
                if (ResumeDrainRanks.ContainsKey(side))
                    ResumeDrainDone.Add(side);

                if (_resumeDrainHolder == side)
                    _resumeDrainHolder = null;

                if (ResumeDrainRanks.Count > 0)
                    ConfigureExpectedForNextResumeDrainNoLock(true);
            }
        }

        // 해당 side가 이번 run에서 첫 전진 순서를 획득할 때까지 대기한다.
        // 이미 이번 run에서 첫 전진을 마친 side는 즉시 통과한다.
        public static async Task AcquireAsync(
            PickerSequenceSide side,
            int rank,
            MachineSequenceContext ctx,
            Action<string> log,
            CancellationToken ct)
        {
            lock (Sync)
            {
                if (Done.Contains(side))
                    return;
                if (Expected.Count == 0)
                    Expected.Add(side);
                Registered[side] = rank;
            }

            DateTime lastWaitLog = DateTime.MinValue;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (ctx != null)
                    ctx.StopIfCycleStopRequested(
                        "PickerFirstForwardSequencer:" + side,
                        ShouldDeferCycleStopForPickerDrain(side, ctx),
                        "Picker target die first-forward drain");

                lock (Sync)
                {
                    if (Done.Contains(side))
                        return;

                    if (_holder == side)
                        return;

                    if (_holder == null && CanGrantNoLock(side))
                    {
                        _holder = side;
                        return;
                    }
                }

                if (log != null && (DateTime.UtcNow - lastWaitLog).TotalMilliseconds >= WaitLogThrottleMs)
                {
                    lastWaitLog = DateTime.UtcNow;
                    log(side + " 첫 전진 순서 대기. 완료에 가까운 상대 픽커가 먼저 전진하도록 양보합니다. rank=" + rank +
                        ", expected=" + DescribeExpectedNoLock() +
                        ", registered=" + DescribeRegisteredNoLock() +
                        ", done=" + DescribeDoneNoLock());
                }

                await Task.Delay(PollMs, ct).ConfigureAwait(false);
            }
        }

        // 해당 side가 첫 전진 스텝을 마쳤음을 알린다(순서 토큰 반납 + 이후 재게이트 방지).
        public static void Complete(PickerSequenceSide side)
        {
            lock (Sync)
            {
                Done.Add(side);
                if (_holder == side)
                    _holder = null;
            }
        }

        private static bool CanGrantNoLock(PickerSequenceSide side)
        {
            // 상대가 아직 등록/완료하지 않았다면 settle 시간 동안 기다려(양쪽 rank 비교 확보) 데드락 없이 결정한다.
            if (!AreExpectedSidesAccountedForNoLock())
                return false;

            return IsHighestPriorityNoLock(side);
        }

        private static bool ShouldDeferCycleStopForPickerDrain(PickerSequenceSide side, MachineSequenceContext ctx)
        {
            if (ctx == null || !ctx.IsCycleStopRequested)
                return false;
            try
            {
                if (ctx.Controller != null && ctx.Controller.Status == EquipmentStatus.Alarm)
                    return false;

                MaterialLocationKind location = side == PickerSequenceSide.Front
                    ? MaterialLocationKind.PickerFront
                    : MaterialLocationKind.PickerRear;
                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                    if (die != null && die.IsInputTarget)
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool AreExpectedSidesAccountedForNoLock()
        {
            if (Expected.Count == 0)
                return Registered.Count > 0;

            foreach (PickerSequenceSide side in Expected)
            {
                if (!Registered.ContainsKey(side) && !Done.Contains(side))
                    return false;
            }

            return true;
        }

        // 등록된(아직 done 아닌) side 중 rank 최고, 동률이면 Front 우선.
        private static bool IsHighestPriorityNoLock(PickerSequenceSide side)
        {
            int selfRank;
            if (!Registered.TryGetValue(side, out selfRank))
                return false;

            foreach (KeyValuePair<PickerSequenceSide, int> other in Registered)
            {
                if (other.Key == side || Done.Contains(other.Key))
                    continue;

                if (other.Value > selfRank)
                    return false;

                // 동률이면 Front가 우선한다.
                if (other.Value == selfRank && other.Key == PickerSequenceSide.Front && side != PickerSequenceSide.Front)
                    return false;
            }

            return true;
        }

        private static bool AllResumeDrainDoneNoLock()
        {
            foreach (PickerSequenceSide side in ResumeDrainRanks.Keys)
            {
                if (!ResumeDrainDone.Contains(side))
                    return false;
            }

            return true;
        }

        private static bool IsHighestResumeDrainPriorityNoLock(PickerSequenceSide side)
        {
            int selfRank;
            if (!ResumeDrainRanks.TryGetValue(side, out selfRank) ||
                ResumeDrainDone.Contains(side))
                return false;

            foreach (KeyValuePair<PickerSequenceSide, int> other in ResumeDrainRanks)
            {
                if (other.Key == side || ResumeDrainDone.Contains(other.Key))
                    continue;

                if (other.Value > selfRank)
                    return false;

                if (other.Value == selfRank &&
                    other.Key == PickerSequenceSide.Front &&
                    side != PickerSequenceSide.Front)
                    return false;
            }

            return true;
        }

        private static void ConfigureExpectedForNextResumeDrainNoLock(bool clearWhenDone)
        {
            PickerSequenceSide? nextSide = null;

            foreach (KeyValuePair<PickerSequenceSide, int> candidate in ResumeDrainRanks)
            {
                if (ResumeDrainDone.Contains(candidate.Key))
                    continue;

                if (!nextSide.HasValue || IsHigherResumeDrainPriorityNoLock(candidate.Key, nextSide.Value))
                    nextSide = candidate.Key;
            }

            if (nextSide.HasValue)
            {
                ConfigureFirstForwardExpectedNoLock(nextSide.Value);
                return;
            }

            if (clearWhenDone)
                Expected.Clear();
        }

        private static bool IsHigherResumeDrainPriorityNoLock(
            PickerSequenceSide candidate,
            PickerSequenceSide current)
        {
            int candidateRank;
            int currentRank;
            if (!ResumeDrainRanks.TryGetValue(candidate, out candidateRank))
                return false;
            if (!ResumeDrainRanks.TryGetValue(current, out currentRank))
                return true;

            if (candidateRank != currentRank)
                return candidateRank > currentRank;

            return candidate == PickerSequenceSide.Front &&
                   current != PickerSequenceSide.Front;
        }

        private static void ConfigureFirstForwardExpectedNoLock(PickerSequenceSide side)
        {
            Expected.Clear();
            Expected.Add(side);
        }

        private static string BuildResumeDrainWaitReasonNoLock(
            PickerSequenceSide side,
            bool ownDrain,
            int ownRank)
        {
            return "ownDrain=" + ownDrain +
                   ", ownRank=" + (ownDrain ? ownRank.ToString() : "-") +
                   ", holder=" + (_resumeDrainHolder.HasValue ? _resumeDrainHolder.Value.ToString() : "-") +
                   ", front=" + DescribeResumeDrainSideNoLock(PickerSequenceSide.Front) +
                   ", rear=" + DescribeResumeDrainSideNoLock(PickerSequenceSide.Rear);
        }

        private static string DescribeResumeDrainSideNoLock(PickerSequenceSide side)
        {
            int rank;
            if (!ResumeDrainRanks.TryGetValue(side, out rank))
                return "none";

            return "rank=" + rank + ",done=" + ResumeDrainDone.Contains(side);
        }

        private static string DescribeExpectedNoLock()
        {
            return "front=" + Expected.Contains(PickerSequenceSide.Front) +
                   ",rear=" + Expected.Contains(PickerSequenceSide.Rear);
        }

        private static string DescribeRegisteredNoLock()
        {
            return "front=" + DescribeRegisteredSideNoLock(PickerSequenceSide.Front) +
                   ",rear=" + DescribeRegisteredSideNoLock(PickerSequenceSide.Rear);
        }

        private static string DescribeRegisteredSideNoLock(PickerSequenceSide side)
        {
            int rank;
            if (!Registered.TryGetValue(side, out rank))
                return "none";

            return "rank=" + rank;
        }

        private static string DescribeDoneNoLock()
        {
            return "front=" + Done.Contains(PickerSequenceSide.Front) +
                   ",rear=" + Done.Contains(PickerSequenceSide.Rear);
        }
    }
}
