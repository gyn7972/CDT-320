using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXMovePlan
    {
        private readonly List<SharedRailXTarget> _targets = new List<SharedRailXTarget>();

        public string Name { get; set; }
        public double Velocity { get; set; }
        public SharedRailXMoveMode Mode { get; set; } = SharedRailXMoveMode.SoftwareParallel;
        public IReadOnlyList<SharedRailXTarget> Targets { get { return _targets; } }

        public SharedRailXMovePlan Add(SharedRailXAxis axis, double targetPosition)
        {
            _targets.Add(new SharedRailXTarget(axis, targetPosition));
            return this;
        }

        public SharedRailXMovePlan Add(SharedRailXAxis axis, double targetPosition, double velocity)
        {
            _targets.Add(new SharedRailXTarget(axis, targetPosition) { Velocity = velocity });
            return this;
        }

        public bool TryGetTarget(SharedRailXAxis axis, out double targetPosition)
        {
            SharedRailXTarget target = _targets.LastOrDefault(x => x.Axis == axis);
            if (target == null)
            {
                targetPosition = 0.0;
                return false;
            }

            targetPosition = target.TargetPosition;
            return true;
        }
    }
}
