namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXTarget
    {
        public SharedRailXTarget(SharedRailXAxis axis, double targetPosition)
        {
            Axis = axis;
            TargetPosition = targetPosition;
        }

        public SharedRailXAxis Axis { get; private set; }
        public double TargetPosition { get; private set; }
        public double? Velocity { get; set; }
        public double? Acceleration { get; set; }
        public double? Deceleration { get; set; }
    }
}
