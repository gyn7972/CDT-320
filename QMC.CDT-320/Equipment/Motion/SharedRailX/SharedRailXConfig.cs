using System.Collections.Generic;

namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXConfig
    {
        public double DefaultSafetyDistance { get; set; } = 10.0;
        public bool RequireSameVelocityForGroupMove { get; set; } = true;

        /// <summary>
        /// 웨이퍼(Input) 비전 최소 회피 목표 계산에만 쓰는 추가 여유[mm].
        /// 회피 목표 = 피커 최대 진입 위치에서 (페어 SafetyDistance + 이 값)만큼 떨어진 위치.
        /// R5: 이 값은 피커/비전 진입 허용 인터락(SafetyDistance 판정)에는 절대 더하지 않는다 —
        /// 오직 TryResolveMinimalVisionRetreatTarget의 회피 목표 계산에만 사용한다.
        /// </summary>
        public double InputVisionRetreatExtraClearance { get; set; } = 40.0;

        /// <summary>
        /// 빈(Output) 비전 최소 회피 목표 계산에만 쓰는 추가 여유[mm].
        /// R5: 인터락(SafetyDistance 판정)에는 절대 더하지 않는다 — 회피 목표 계산 전용.
        /// </summary>
        public double OutputVisionRetreatExtraClearance { get; set; } = 40.0;

        /// <summary>
        /// 비전∥피커 팔로잉 진입(FollowMoveAsync)의 타임아웃[ms]. 기본 15000, 최소 1000.
        /// </summary>
        public int VisionFollowEntryTimeoutMs { get; set; } = 15000;

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
            if (IsFrontRearPickerPair(axisA, axisB))
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
            if (IsFrontRearPickerPair(axisA, axisB))
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
                IsFrontRearPickerPair(axisA, axisB) ||
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

        private static bool IsFrontRearPickerPair(SharedRailXAxis axisA, SharedRailXAxis axisB)
        {
            return (axisA == SharedRailXAxis.FrontPickerX && axisB == SharedRailXAxis.RearPickerX) ||
                   (axisA == SharedRailXAxis.RearPickerX && axisB == SharedRailXAxis.FrontPickerX);
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
