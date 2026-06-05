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
        public double RailOriginOffset { get; set; }
        public double PositionScale { get; set; } = 1.0;
        public double SafetyDistance { get; set; }

        public double ToRailPosition(double axisPosition)
        {
            return RailOriginOffset + axisPosition * PositionScale;
        }

        public double GetMinAt(double axisPosition)
        {
            return ToRailPosition(axisPosition) + BodyOffsetMin;
        }

        public double GetMaxAt(double axisPosition)
        {
            return ToRailPosition(axisPosition) + BodyOffsetMax;
        }
    }
}
