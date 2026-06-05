using QMC.Common.Motion;

namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXAxisSetting
    {
        public SharedRailXAxisSetting(SharedRailXAxis railAxis, BaseAxis axis)
        {
            RailAxis = railAxis;
            Axis = axis;
        }

        public SharedRailXAxis RailAxis { get; private set; }
        public BaseAxis Axis { get; private set; }
        public double BodyOffsetMin { get; set; }
        public double BodyOffsetMax { get; set; }
        public double SafetyDistance { get; set; }

        public double GetMinAt(double centerPosition)
        {
            return centerPosition + BodyOffsetMin;
        }

        public double GetMaxAt(double centerPosition)
        {
            return centerPosition + BodyOffsetMax;
        }
    }
}
