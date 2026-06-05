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
                    SharedRailXValidationResult current = ValidateDistance(a, b, false);
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
