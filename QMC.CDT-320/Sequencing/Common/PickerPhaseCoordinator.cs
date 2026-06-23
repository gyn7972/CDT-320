using System;

namespace QMC.CDT320.Sequencing
{
    internal sealed class PickerPhaseCoordinator
    {
        private readonly object _gate = new object();
        private PickerPhaseState _front = PickerPhaseState.Idle(PickerSequenceSide.Front);
        private PickerPhaseState _rear = PickerPhaseState.Idle(PickerSequenceSide.Rear);

        public bool TryEnter(
            PickerSequenceSide side,
            PickerProcessPhase phase,
            string owner,
            out PickerPhaseLease lease,
            out string reason)
        {
            lease = null;
            reason = string.Empty;

            lock (_gate)
            {
                PickerPhaseState own = GetStateNoLock(side);
                if (own.Phase != PickerProcessPhase.Idle)
                {
                    reason = BuildBlockedReason(side, phase, "요청 Picker가 이미 phase를 점유 중입니다.", own, GetOppositeStateNoLock(side));
                    return false;
                }

                PickerPhaseState opposite = GetOppositeStateNoLock(side);
                if (!IsAllowedNoLock(phase, opposite.Phase, out reason))
                {
                    reason = BuildBlockedReason(side, phase, reason, own, opposite);
                    return false;
                }

                SetStateNoLock(side, new PickerPhaseState(side, phase, SafeOwner(owner), DateTime.Now));
                lease = new PickerPhaseLease(this, side, SafeOwner(owner));
                return true;
            }
        }

        public bool TryTransition(PickerPhaseLease lease, PickerProcessPhase nextPhase, out string reason)
        {
            reason = string.Empty;
            if (lease == null || lease.IsDisposed)
            {
                reason = "Picker phase 전환 실패: 유효한 phase lease가 없습니다.";
                return false;
            }

            lock (_gate)
            {
                PickerPhaseState own = GetStateNoLock(lease.Side);
                if (!string.Equals(own.Owner, lease.Owner, StringComparison.Ordinal))
                {
                    reason = BuildBlockedReason(
                        lease.Side,
                        nextPhase,
                        "Picker phase owner가 일치하지 않습니다.",
                        own,
                        GetOppositeStateNoLock(lease.Side));
                    return false;
                }

                PickerPhaseState opposite = GetOppositeStateNoLock(lease.Side);
                if (!IsAllowedNoLock(nextPhase, opposite.Phase, out reason))
                {
                    reason = BuildBlockedReason(lease.Side, nextPhase, reason, own, opposite);
                    return false;
                }

                SetStateNoLock(lease.Side, new PickerPhaseState(lease.Side, nextPhase, lease.Owner, DateTime.Now));
                return true;
            }
        }

        public PickerPhaseSnapshot GetSnapshot()
        {
            lock (_gate)
            {
                return new PickerPhaseSnapshot(_front, _rear);
            }
        }

        internal void Exit(PickerPhaseLease lease)
        {
            if (lease == null)
                return;

            lock (_gate)
            {
                PickerPhaseState own = GetStateNoLock(lease.Side);
                if (!string.Equals(own.Owner, lease.Owner, StringComparison.Ordinal))
                    return;

                SetStateNoLock(lease.Side, PickerPhaseState.Idle(lease.Side));
            }
        }

        private PickerPhaseState GetStateNoLock(PickerSequenceSide side)
        {
            return side == PickerSequenceSide.Front ? _front : _rear;
        }

        private PickerPhaseState GetOppositeStateNoLock(PickerSequenceSide side)
        {
            return side == PickerSequenceSide.Front ? _rear : _front;
        }

        private void SetStateNoLock(PickerSequenceSide side, PickerPhaseState state)
        {
            if (side == PickerSequenceSide.Front)
                _front = state;
            else
                _rear = state;
        }

        private static string SafeOwner(string owner)
        {
            return string.IsNullOrWhiteSpace(owner) ? "Unknown" : owner;
        }

        private static bool IsAllowedNoLock(
            PickerProcessPhase requested,
            PickerProcessPhase opposite,
            out string reason)
        {
            reason = string.Empty;

            if (requested == PickerProcessPhase.Idle)
                return true;

            if (opposite == PickerProcessPhase.Idle)
                return true;

            if (opposite == PickerProcessPhase.Unknown)
            {
                reason = "상대 Picker phase가 Unknown이라 안전하게 대기합니다.";
                return false;
            }

            // GYN - 여기서 시컨스 관련 정책을 정한다.
            // CDT-320 현재 기구 기준 phase matrix.
            // FrontPicker와 RearPicker에 동일하게 적용한다.
            // 초기 PickUp 선점 정책은 RearPickerSequence에서만 별도로 처리하고,
            // 여기서는 공정 간섭 방지만 대칭으로 판단한다.
            switch (requested)
            {
                case PickerProcessPhase.PickUp:
                    if (opposite == PickerProcessPhase.BottomInspection ||
                        opposite == PickerProcessPhase.SideInspection ||
                        opposite == PickerProcessPhase.Place)
                        return true;
                    reason = "PickUp은 상대 Picker가 BottomInspection, SideInspection 또는 Place 중일 때 진입할 수 있습니다.";
                    return false;

                case PickerProcessPhase.BottomInspection:
                    if (opposite == PickerProcessPhase.Place)
                        return true;
                    reason = "BottomInspection은 상대 Picker가 PickUp/BottomInspection/SideInspection 중이면 진입할 수 없습니다.";
                    return false;

                case PickerProcessPhase.SideInspection:
                    if (opposite == PickerProcessPhase.PickUp ||
                        opposite == PickerProcessPhase.Place)
                        return true;
                    reason = "SideInspection은 상대 Picker가 BottomInspection/SideInspection 중이면 진입할 수 없습니다.";
                    return false;

                case PickerProcessPhase.Place:
                    if (opposite != PickerProcessPhase.Place)
                        return true;
                    reason = "Place는 상대 Picker Place와 동시에 진입할 수 없습니다.";
                    return false;

                default:
                    reason = "지원하지 않는 Picker phase 요청입니다. requested=" + requested;
                    return false;
            }
        }

        private static string BuildBlockedReason(
            PickerSequenceSide requestedSide,
            PickerProcessPhase requestedPhase,
            string policyReason,
            PickerPhaseState own,
            PickerPhaseState opposite)
        {
            return "Picker phase 진입 대기/차단. " +
                   "request=" + requestedSide + "/" + requestedPhase +
                   ", own=" + own +
                   ", opposite=" + opposite +
                   ", reason=" + policyReason;
        }
    }

    internal sealed class PickerPhaseLease : IDisposable
    {
        private readonly PickerPhaseCoordinator _coordinator;
        private bool _disposed;

        internal PickerPhaseLease(PickerPhaseCoordinator coordinator, PickerSequenceSide side, string owner)
        {
            _coordinator = coordinator;
            Side = side;
            Owner = owner;
        }

        public PickerSequenceSide Side { get; private set; }
        public string Owner { get; private set; }
        public bool IsDisposed { get { return _disposed; } }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_coordinator != null)
                _coordinator.Exit(this);
        }
    }

    internal struct PickerPhaseState
    {
        public PickerPhaseState(
            PickerSequenceSide side,
            PickerProcessPhase phase,
            string owner,
            DateTime enteredAt)
        {
            Side = side;
            Phase = phase;
            Owner = owner ?? string.Empty;
            EnteredAt = enteredAt;
        }

        public PickerSequenceSide Side { get; private set; }
        public PickerProcessPhase Phase { get; private set; }
        public string Owner { get; private set; }
        public DateTime EnteredAt { get; private set; }

        public static PickerPhaseState Idle(PickerSequenceSide side)
        {
            return new PickerPhaseState(side, PickerProcessPhase.Idle, string.Empty, DateTime.MinValue);
        }

        public override string ToString()
        {
            return Side + "/" + Phase + "/" + (string.IsNullOrWhiteSpace(Owner) ? "-" : Owner);
        }
    }

    internal sealed class PickerPhaseSnapshot
    {
        internal PickerPhaseSnapshot(PickerPhaseState front, PickerPhaseState rear)
        {
            Front = front;
            Rear = rear;
        }

        public PickerPhaseState Front { get; private set; }
        public PickerPhaseState Rear { get; private set; }

        public override string ToString()
        {
            return "Front=" + Front + ", Rear=" + Rear;
        }
    }
}
