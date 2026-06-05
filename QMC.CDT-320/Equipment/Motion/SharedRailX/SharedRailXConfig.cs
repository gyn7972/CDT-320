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
            Geometry[SharedRailXAxis.WaferVisionX] = new SharedRailXAxisGeometry();
            Geometry[SharedRailXAxis.FrontPickerX] = new SharedRailXAxisGeometry();
            Geometry[SharedRailXAxis.RearPickerX] = new SharedRailXAxisGeometry();
            Geometry[SharedRailXAxis.BinVisionX] = new SharedRailXAxisGeometry();
        }

        public SharedRailXConfig SetGeometry(
            SharedRailXAxis axis,
            double bodyOffsetMin,
            double bodyOffsetMax,
            double? safetyDistance = null)
        {
            Geometry[axis] = new SharedRailXAxisGeometry
            {
                BodyOffsetMin = bodyOffsetMin,
                BodyOffsetMax = bodyOffsetMax,
                SafetyDistance = safetyDistance
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
        public double? SafetyDistance { get; set; }
    }
}
