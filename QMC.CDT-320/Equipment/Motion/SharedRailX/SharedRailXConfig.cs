using System.Collections.Generic;

namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXConfig
    {
        public double DefaultSafetyDistance { get; set; } = 10.0;
        public bool RequireSameVelocityForGroupMove { get; set; } = true;

        public List<SharedRailXAxisPair> CollisionPairs { get; private set; }

        public SharedRailXConfig()
        {
            CollisionPairs = new List<SharedRailXAxisPair>();
            AddCollisionPair(SharedRailXAxis.InputVisionX, SharedRailXAxis.FrontPickerX);
            AddCollisionPair(SharedRailXAxis.InputVisionX, SharedRailXAxis.RearPickerX);
            AddCollisionPair(SharedRailXAxis.OutputVisionX, SharedRailXAxis.FrontPickerX);
            AddCollisionPair(SharedRailXAxis.OutputVisionX, SharedRailXAxis.RearPickerX);
        }

        public SharedRailXConfig SetCollisionPairs(IEnumerable<SharedRailXAxisPair> pairs)
        {
            CollisionPairs.Clear();
            if (pairs == null)
                return this;

            foreach (SharedRailXAxisPair pair in pairs)
                AddCollisionPair(pair);

            return this;
        }

        public SharedRailXConfig AddCollisionPair(SharedRailXAxis axisA, SharedRailXAxis axisB)
        {
            return AddCollisionPair(new SharedRailXAxisPair(axisA, axisB));
        }

        public SharedRailXConfig AddCollisionPair(SharedRailXAxisPair pair)
        {
            SharedRailXAxis axisA = pair.AxisA;
            SharedRailXAxis axisB = pair.AxisB;
            if (axisA == axisB)
                return this;
            if (IsInputOutputVisionPair(axisA, axisB))
                return this;
            if (IsCollisionPairEnabled(axisA, axisB))
                return this;

            CollisionPairs.Add(pair);
            return this;
        }

        public bool IsCollisionPairEnabled(SharedRailXAxis axisA, SharedRailXAxis axisB)
        {
            if (axisA == axisB)
                return false;
            if (IsInputOutputVisionPair(axisA, axisB))
                return false;
            if (CollisionPairs == null || CollisionPairs.Count == 0)
                return false;

            foreach (SharedRailXAxisPair pair in CollisionPairs)
            {
                if (pair.Matches(axisA, axisB))
                    return true;
            }

            return false;
        }

        public bool TryGetCollisionPair(SharedRailXAxis axisA, SharedRailXAxis axisB, out SharedRailXAxisPair matchedPair)
        {
            matchedPair = default(SharedRailXAxisPair);
            if (axisA == axisB || IsInputOutputVisionPair(axisA, axisB) ||
                CollisionPairs == null || CollisionPairs.Count == 0)
            {
                return false;
            }

            foreach (SharedRailXAxisPair pair in CollisionPairs)
            {
                if (pair.Matches(axisA, axisB))
                {
                    matchedPair = pair;
                    return true;
                }
            }

            return false;
        }

        private static bool IsInputOutputVisionPair(SharedRailXAxis axisA, SharedRailXAxis axisB)
        {
            return (axisA == SharedRailXAxis.InputVisionX && axisB == SharedRailXAxis.OutputVisionX) ||
                   (axisA == SharedRailXAxis.OutputVisionX && axisB == SharedRailXAxis.InputVisionX);
        }

        public static SharedRailXConfig CreateDefault()
        {
            return new SharedRailXConfig();
        }
    }

    public struct SharedRailXAxisPair
    {
        public SharedRailXAxis AxisA { get; private set; }
        public SharedRailXAxis AxisB { get; private set; }
        public double HomeClearance { get; private set; }
        public int AxisATowardSign { get; private set; }
        public int AxisBTowardSign { get; private set; }
        public double? SafetyDistance { get; private set; }

        public SharedRailXAxisPair(SharedRailXAxis axisA, SharedRailXAxis axisB)
            : this(axisA, axisB, 0.0, 0, 0, null)
        {
        }

        public SharedRailXAxisPair(
            SharedRailXAxis axisA,
            SharedRailXAxis axisB,
            double homeClearance,
            int axisATowardSign,
            int axisBTowardSign,
            double? safetyDistance)
        {
            AxisA = axisA;
            AxisB = axisB;
            HomeClearance = homeClearance;
            AxisATowardSign = axisATowardSign;
            AxisBTowardSign = axisBTowardSign;
            SafetyDistance = safetyDistance;
        }

        public bool HasClearanceRule
        {
            get { return HomeClearance > 0.0 && AxisATowardSign != 0 && AxisBTowardSign != 0; }
        }

        public bool Matches(SharedRailXAxis axisA, SharedRailXAxis axisB)
        {
            return (AxisA == axisA && AxisB == axisB) ||
                   (AxisA == axisB && AxisB == axisA);
        }
    }
}
