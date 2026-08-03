using QMC.CDT320.Motion.SharedRailX;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public static class CollisionDistanceCalculator
    {
        #region 축 Pair Snapshot 생성

        public static AxisPairSafetySnapshot BuildSnapshot(
            SharedRailXAxis axisA,
            BaseAxis baseAxisA,
            SharedRailXAxis axisB,
            BaseAxis baseAxisB,
            SharedRailXConfig config,
            double fallbackRequiredClearance,
            string ruleKind)
        {
            string pairName = axisA + "<->" + axisB;
            var snapshot = new AxisPairSafetySnapshot
            {
                PairName = pairName,
                RuleKind = ruleKind ?? string.Empty,
                RequiredClearance = fallbackRequiredClearance
            };

            if (baseAxisA == null || baseAxisB == null)
            {
                snapshot.Configured = false;
                snapshot.Reason = "축 객체가 없어 거리 계산을 수행할 수 없습니다.";
                return snapshot;
            }

            snapshot.AxisAActual = baseAxisA.ActualPosition;
            snapshot.AxisBActual = baseAxisB.ActualPosition;
            snapshot.AxisACommand = baseAxisA.CommandPosition;
            snapshot.AxisBCommand = baseAxisB.CommandPosition;
            snapshot.AxisAMoving = baseAxisA.IsMoving;
            snapshot.AxisBMoving = baseAxisB.IsMoving;

            SharedRailXAxisPair pair;
            if (config == null || !config.TryGetCollisionPair(axisA, axisB, out pair) || !pair.HasClearanceRule)
            {
                snapshot.Configured = false;
                snapshot.Reason = "SharedRailX 거리 pair 설정이 없습니다. HomeClearance/sign 등록이 필요합니다.";
                return snapshot;
            }

            int signA;
            int signB;
            ResolvePairSigns(axisA, axisB, pair, out signA, out signB);
            double required = pair.SafetyDistance.HasValue ? pair.SafetyDistance.Value : fallbackRequiredClearance;

            snapshot.Configured = true;
            snapshot.HomeClearance = pair.HomeClearance;
            snapshot.AxisATowardSign = signA;
            snapshot.AxisBTowardSign = signB;
            snapshot.RequiredClearance = required;
            snapshot.ActualClearance = CalculateClearance(
                pair.HomeClearance,
                signA,
                baseAxisA.ActualPosition,
                signB,
                baseAxisB.ActualPosition);
            snapshot.TargetClearance = snapshot.ActualClearance;
            return snapshot;
        }

        #endregion

        #region 간격 계산 및 방향 부호 해석

        public static double CalculateClearance(
            double homeClearance,
            int axisATowardSign,
            double axisAPosition,
            int axisBTowardSign,
            double axisBPosition)
        {
            return homeClearance - (axisATowardSign * axisAPosition) - (axisBTowardSign * axisBPosition);
        }

        private static void ResolvePairSigns(
            SharedRailXAxis axisA,
            SharedRailXAxis axisB,
            SharedRailXAxisPair pair,
            out int signA,
            out int signB)
        {
            if (pair.AxisA == axisA && pair.AxisB == axisB)
            {
                signA = pair.AxisATowardSign;
                signB = pair.AxisBTowardSign;
                return;
            }

            signA = pair.AxisBTowardSign;
            signB = pair.AxisATowardSign;
        }

        #endregion
    }
}
