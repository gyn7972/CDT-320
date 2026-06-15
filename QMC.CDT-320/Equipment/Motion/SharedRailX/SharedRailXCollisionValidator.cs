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

                    if (!pair.HasClearanceRule)
                    {
                        return SharedRailXValidationResult.Block(
                            "SharedRailX pair clearance rule is not configured. pair=" +
                            a.Axis + "<->" + b.Axis);
                    }

                    SharedRailXValidationResult clearance = ValidatePairClearance(a, b, pair);
                    if (!clearance.Allowed)
                        return clearance;
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
                Moving = moving,
                SafetyDistance = setting.SafetyDistance
            };
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

        private sealed class SharedRailXAxisState
        {
            public SharedRailXAxis Axis;
            public string Name;
            public double CurrentAxis;
            public double TargetAxis;
            public bool Moving;
            public double SafetyDistance;
        }
    }
}
