using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Interlocks
{
    /// <summary>
    /// 모션 인터록 베이스 — 310 의 MotionInterlock 단순화.
    /// 서브클래스가 <see cref="VerifyMove"/> 를 override 하여 충돌 위험 시 false + reason 반환.
    /// </summary>
    public abstract class MotionInterlock
    {
        public string Name { get; }

        protected MotionInterlock(string name) { Name = name; }

        /// <summary>
        /// 이 인터록이 axisName/targetPos 의 이동을 허용하는지 검증.
        /// true = 허용, false = 차단 (reason 에 사유).
        /// </summary>
        public abstract bool VerifyMove(string axisName, double targetPos, out string reason);
    }

    /// <summary>
    /// 인터록 레지스트리 — MachineController 가 MoveAxis 전 호출.
    /// </summary>
    public static class InterlockRegistry
    {
        private static readonly List<MotionInterlock> _interlocks = new List<MotionInterlock>();
        private static readonly object _lock = new object();

        public static IReadOnlyList<MotionInterlock> All { get { lock (_lock) return _interlocks.ToList(); } }

        public static void Register(MotionInterlock il)
        {
            if (il == null) return;
            lock (_lock) _interlocks.Add(il);
        }

        public static void Clear() { lock (_lock) _interlocks.Clear(); }

        /// <summary>등록된 모든 인터록을 검사. 하나라도 false → 즉시 false + 사유.</summary>
        public static bool VerifyMove(string axisName, double targetPos, out string blockingReason)
        {
            blockingReason = null;
            List<MotionInterlock> snapshot;
            lock (_lock) snapshot = _interlocks.ToList();
            foreach (var il in snapshot)
            {
                if (!il.VerifyMove(axisName, targetPos, out var reason))
                {
                    blockingReason = $"[{il.Name}] {reason}";
                    return false;
                }
            }
            return true;
        }
    }
}
