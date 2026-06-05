using System.Collections.Generic;

namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXConfig
    {
        public double DefaultSafetyDistance { get; set; } = 10.0;
        public bool EnablePathCheck { get; set; } = true;
        public bool RequireSameVelocityForGroupMove { get; set; } = true;

        public Dictionary<SharedRailXAxis, SharedRailXAxisGeometry> Geometry { get; private set; }

        public SharedRailXConfig()
        {
            Geometry = new Dictionary<SharedRailXAxis, SharedRailXAxisGeometry>();
            Geometry[SharedRailXAxis.InputVisionX] = new SharedRailXAxisGeometry();
            Geometry[SharedRailXAxis.FrontPickerX] = new SharedRailXAxisGeometry();
            Geometry[SharedRailXAxis.RearPickerX] = new SharedRailXAxisGeometry();
            Geometry[SharedRailXAxis.OutputVisionX] = new SharedRailXAxisGeometry();
        }

        public SharedRailXConfig SetGeometry(
            SharedRailXAxis axis,
            double bodyOffsetMin,
            double bodyOffsetMax,
            double? safetyDistance = null,
            double railOriginOffset = 0.0,
            double positionScale = 1.0)
        {
            Geometry[axis] = new SharedRailXAxisGeometry
            {
                BodyOffsetMin = bodyOffsetMin,
                BodyOffsetMax = bodyOffsetMax,
                SafetyDistance = safetyDistance,
                RailOriginOffset = railOriginOffset,
                PositionScale = positionScale
            };
            return this;
        }

        public static SharedRailXConfig CreateDefault()
        {
            return new SharedRailXConfig();
        }
    }

    public sealed class SharedRailXAxisGeometry
    {
        public double BodyOffsetMin { get; set; } = 0.0;
        public double BodyOffsetMax { get; set; } = 0.0;
        public double RailOriginOffset { get; set; } = 0.0;
        public double PositionScale { get; set; } = 1.0;
        public double? SafetyDistance { get; set; }
    }
}
