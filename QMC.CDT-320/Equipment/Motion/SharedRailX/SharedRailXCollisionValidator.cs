using System;
using System.Collections.Generic;
using System.Linq;

namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXCollisionValidator
    {
        private readonly SharedRailXConfig _config;

        public SharedRailXCollisionValidator(SharedRailXConfig config)
        {
            _config = config ?? SharedRailXConfig.CreateDefault();
        }

        public SharedRailXValidationResult Validate(
            IEnumerable<SharedRailXAxisSetting> settings,
            SharedRailXMovePlan plan)
        {
            if (settings == null)
                return SharedRailXValidationResult.Block("SharedRailX settings are empty.");
            if (plan == null)
                return SharedRailXValidationResult.Block("SharedRailX move plan is null.");
            if (plan.Targets == null || plan.Targets.Count == 0)
                return SharedRailXValidationResult.Block("SharedRailX move target is empty.");

            List<SharedRailXAxisSetting> axisSettings = settings.Where(x => x != null && x.Axis != null).ToList();
            if (axisSettings.Count == 0)
                return SharedRailXValidationResult.Block("SharedRailX mapped axes are empty.");

            foreach (SharedRailXTarget target in plan.Targets)
            {
                if (!axisSettings.Any(x => x.RailAxis == target.Axis))
                    return SharedRailXValidationResult.Block("SharedRailX target axis is not mapped. axis=" + target.Axis);
            }

            var states = axisSettings.Select(x => BuildState(x, plan)).ToList();

            for (int i = 0; i < states.Count; i++)
            {
                for (int j = i + 1; j < states.Count; j++)
                {
                    SharedRailXAxisState a = states[i];
                    SharedRailXAxisState b = states[j];
                    if (!a.Moving && !b.Moving)
                        continue;

                    SharedRailXAxisPair pair;
                    if (!_config.TryGetCollisionPair(a.Axis, b.Axis, out pair))
                        continue;

                    if (IsInputVisionMovingNegative(a) || IsInputVisionMovingNegative(b))
                        continue;

                    if (pair.HasClearanceRule)
                    {
                        SharedRailXValidationResult clearance = ValidatePairClearance(a, b, pair);
                        if (!clearance.Allowed)
                            return clearance;
                        continue;
                    }

                    SharedRailXValidationResult current = ValidateCurrentDistance(a, b);
                    if (!current.Allowed)
                        return current;

                    SharedRailXValidationResult target = ValidateDistance(a, b, true);
                    if (!target.Allowed)
                        return target;

                    if (_config.EnablePathCheck)
                    {
                        SharedRailXValidationResult path = ValidatePath(a, b);
                        if (!path.Allowed)
                            return path;
                    }
                }
            }

            if (_config.RequireSameVelocityForGroupMove)
            {
                SharedRailXValidationResult velocity = ValidateVelocity(plan);
                if (!velocity.Allowed)
                    return velocity;
            }

            return SharedRailXValidationResult.Allow();
        }

        private static bool IsInputVisionMovingNegative(SharedRailXAxisState state)
        {
            return state != null &&
                   state.Axis == SharedRailXAxis.InputVisionX &&
                   state.Moving &&
                   state.TargetAxis < state.CurrentAxis;
        }

        private static SharedRailXValidationResult ValidatePairClearance(
            SharedRailXAxisState a,
            SharedRailXAxisState b,
            SharedRailXAxisPair pair)
        {
            int aSign;
            int bSign;
            ResolvePairSigns(a.Axis, b.Axis, pair, out aSign, out bSign);

            double required = pair.SafetyDistance.HasValue
                ? pair.SafetyDistance.Value
                : Math.Max(a.SafetyDistance, b.SafetyDistance);

            double currentClearance = CalculatePairClearance(pair.HomeClearance, aSign, a.CurrentAxis, bSign, b.CurrentAxis);
            double targetClearance = CalculatePairClearance(pair.HomeClearance, aSign, a.TargetAxis, bSign, b.TargetAxis);
            double minimumClearance = Math.Min(currentClearance, targetClearance);

            if (currentClearance < required && targetClearance <= currentClearance)
            {
                return BlockPairClearance("current", a, b, currentClearance, targetClearance, minimumClearance, required, pair);
            }

            if (targetClearance < required)
            {
                return BlockPairClearance("target", a, b, currentClearance, targetClearance, minimumClearance, required, pair);
            }

            return SharedRailXValidationResult.Allow();
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

        private static double CalculatePairClearance(
            double homeClearance,
            int aTowardSign,
            double aPosition,
            int bTowardSign,
            double bPosition)
        {
            return homeClearance - (aTowardSign * aPosition) - (bTowardSign * bPosition);
        }

        private static SharedRailXValidationResult BlockPairClearance(
            string state,
            SharedRailXAxisState a,
            SharedRailXAxisState b,
            double currentClearance,
            double targetClearance,
            double minimumClearance,
            double required,
            SharedRailXAxisPair pair)
        {
            return SharedRailXValidationResult.Block(
                "SharedRailX pair clearance is too close. state=" + state +
                ", " + a.Name + " axis " + a.CurrentAxis.ToString("F3") + "->" + a.TargetAxis.ToString("F3") +
                ", " + b.Name + " axis " + b.CurrentAxis.ToString("F3") + "->" + b.TargetAxis.ToString("F3") +
                ", currentClearance=" + currentClearance.ToString("F3") +
                ", targetClearance=" + targetClearance.ToString("F3") +
                ", minClearance=" + minimumClearance.ToString("F3") +
                ", required=" + required.ToString("F3") +
                ", homeClearance=" + pair.HomeClearance.ToString("F3") +
                ", signs=" + pair.AxisA + ":" + pair.AxisATowardSign + "," +
                pair.AxisB + ":" + pair.AxisBTowardSign);
        }

        private SharedRailXAxisState BuildState(SharedRailXAxisSetting setting, SharedRailXMovePlan plan)
        {
            double target;
            bool moving = plan.TryGetTarget(setting.RailAxis, out target);
            double current = setting.Axis.ActualPosition;
            double targetAxisPosition = moving ? target : current;
            return new SharedRailXAxisState
            {
                Axis = setting.RailAxis,
                Name = setting.Axis.Name,
                CurrentAxis = current,
                TargetAxis = targetAxisPosition,
                Current = setting.ToRailPosition(current),
                Target = setting.ToRailPosition(targetAxisPosition),
                Moving = moving,
                BodyOffsetMin = setting.BodyOffsetMin,
                BodyOffsetMax = setting.BodyOffsetMax,
                SafetyDistance = setting.SafetyDistance
            };
        }

        private static SharedRailXValidationResult ValidateCurrentDistance(
            SharedRailXAxisState a,
            SharedRailXAxisState b)
        {
            double currentGap = CalculateGap(a, a.Current, b, b.Current);
            double requiredGap = Math.Max(a.SafetyDistance, b.SafetyDistance);
            bool currentDistanceIsSafe = currentGap >= requiredGap;
            if (currentDistanceIsSafe)
                return SharedRailXValidationResult.Allow();

            bool aIsMoving = a.Moving;
            bool bIsMoving = b.Moving;
            bool anyAxisMoving = aIsMoving || bIsMoving;

            double targetGap = CalculateGap(a, a.Target, b, b.Target);
            bool targetGapIsIncreasing = targetGap > currentGap;
            bool movingAwayFromCollision = anyAxisMoving && targetGapIsIncreasing;
            if (movingAwayFromCollision)
                return SharedRailXValidationResult.Allow();

            string movingState =
                "moving=" +
                a.Name + ":" + (aIsMoving ? "Y" : "N") + "," +
                b.Name + ":" + (bIsMoving ? "Y" : "N");

            return SharedRailXValidationResult.Block(
                "SharedRailX current distance is too close. " +
                a.Name + " axis=" + a.CurrentAxis.ToString("F3") +
                " rail=" + a.Current.ToString("F3") + ", " +
                b.Name + " axis=" + b.CurrentAxis.ToString("F3") +
                " rail=" + b.Current.ToString("F3") +
                ", currentGap=" + currentGap.ToString("F3") +
                ", targetGap=" + targetGap.ToString("F3") +
                ", required=" + requiredGap.ToString("F3") +
                ", " + movingState);
        }

        private static SharedRailXValidationResult ValidateDistance(
            SharedRailXAxisState a,
            SharedRailXAxisState b,
            bool useTarget)
        {
            double aPos = useTarget ? a.Target : a.Current;
            double bPos = useTarget ? b.Target : b.Current;
            double gap = CalculateGap(a, aPos, b, bPos);
            double required = Math.Max(a.SafetyDistance, b.SafetyDistance);
            if (gap < required)
            {
                string state = useTarget ? "target" : "current";
                return SharedRailXValidationResult.Block(
                    "SharedRailX " + state + " distance is too close. " +
                    a.Name + " axis=" + (useTarget ? a.TargetAxis : a.CurrentAxis).ToString("F3") +
                    " rail=" + aPos.ToString("F3") + ", " +
                    b.Name + " axis=" + (useTarget ? b.TargetAxis : b.CurrentAxis).ToString("F3") +
                    " rail=" + bPos.ToString("F3") + ", gap=" +
                    gap.ToString("F3") + ", required=" + required.ToString("F3"));
            }

            return SharedRailXValidationResult.Allow();
        }

        private static SharedRailXValidationResult ValidatePath(
            SharedRailXAxisState a,
            SharedRailXAxisState b)
        {
            if (!a.Moving && !b.Moving)
                return SharedRailXValidationResult.Allow();

            double required = Math.Max(a.SafetyDistance, b.SafetyDistance);
            double aMin = Math.Min(a.Current + a.BodyOffsetMin, a.Target + a.BodyOffsetMin);
            double aMax = Math.Max(a.Current + a.BodyOffsetMax, a.Target + a.BodyOffsetMax);
            double bMin = Math.Min(b.Current + b.BodyOffsetMin, b.Target + b.BodyOffsetMin);
            double bMax = Math.Max(b.Current + b.BodyOffsetMax, b.Target + b.BodyOffsetMax);

            bool overlap = aMin <= bMax + required && bMin <= aMax + required;
            if (!overlap)
                return SharedRailXValidationResult.Allow();

            bool sameDirection = Math.Sign(a.Target - a.Current) == Math.Sign(b.Target - b.Current);
            double currentGap = CalculateGap(a, a.Current, b, b.Current);
            double targetGap = CalculateGap(a, a.Target, b, b.Target);
            if ((a.Moving || b.Moving) && targetGap > currentGap)
                return SharedRailXValidationResult.Allow();

            if (a.Moving && b.Moving && sameDirection && currentGap >= required && targetGap >= required)
                return SharedRailXValidationResult.Allow();

            return SharedRailXValidationResult.Block(
                "SharedRailX move path can be too close. " +
                a.Name + " axis " + a.CurrentAxis.ToString("F3") + "->" + a.TargetAxis.ToString("F3") +
                " rail " + a.Current.ToString("F3") + "->" + a.Target.ToString("F3") + ", " +
                b.Name + " axis " + b.CurrentAxis.ToString("F3") + "->" + b.TargetAxis.ToString("F3") +
                " rail " + b.Current.ToString("F3") + "->" + b.Target.ToString("F3") +
                ", required=" + required.ToString("F3"));
        }

        private static SharedRailXValidationResult ValidateVelocity(SharedRailXMovePlan plan)
        {
            List<double> velocities = plan.Targets
                .Select(x => x.Velocity.HasValue ? x.Velocity.Value : plan.Velocity)
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (velocities.Count > 1)
                return SharedRailXValidationResult.Block("SharedRailX group move requires same velocity.");

            return SharedRailXValidationResult.Allow();
        }

        private static double CalculateGap(
            SharedRailXAxisState a,
            double aCenter,
            SharedRailXAxisState b,
            double bCenter)
        {
            double aMin = aCenter + a.BodyOffsetMin;
            double aMax = aCenter + a.BodyOffsetMax;
            double bMin = bCenter + b.BodyOffsetMin;
            double bMax = bCenter + b.BodyOffsetMax;

            if (aMax <= bMin)
                return bMin - aMax;
            if (bMax <= aMin)
                return aMin - bMax;
            return 0.0;
        }

        private sealed class SharedRailXAxisState
        {
            public SharedRailXAxis Axis;
            public string Name;
            public double CurrentAxis;
            public double TargetAxis;
            public double Current;
            public double Target;
            public bool Moving;
            public double BodyOffsetMin;
            public double BodyOffsetMax;
            public double SafetyDistance;
        }
    }
}
