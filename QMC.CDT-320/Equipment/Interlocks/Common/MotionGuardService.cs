using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public enum MotionGuardMoveKind
    {
        AxisMove,
        AxisHome,
        AxisTeachingMove,
        CylinderMove,
        CylinderInitialize
    }

    public sealed class MotionGuardResult
    {
        public bool Allowed { get; set; }
        public string MovingKey { get; set; }
        public string Message { get; set; }
        public IReadOnlyList<InterlockCheckPair> RequiredChecks { get; set; }

        public bool RequiresDetailedCheck
        {
            get { return RequiredChecks != null && RequiredChecks.Count > 0; }
        }
    }

    public sealed class MotionGuardContext
    {
        public MotionGuardContext(IEnumerable<BaseAxis> axes, IEnumerable<BaseCylinder> cylinders)
            : this(null, axes, cylinders)
        {
        }

        public MotionGuardContext(CDT320_Machine machine, IEnumerable<BaseAxis> axes, IEnumerable<BaseCylinder> cylinders)
        {
            Machine = machine;
            Axes = BuildAxisMap(axes);
            Cylinders = BuildCylinderMap(cylinders);
        }

        public CDT320_Machine Machine { get; private set; }
        public IReadOnlyDictionary<string, BaseAxis> Axes { get; private set; }
        public IReadOnlyDictionary<string, BaseCylinder> Cylinders { get; private set; }

        private static IReadOnlyDictionary<string, BaseAxis> BuildAxisMap(IEnumerable<BaseAxis> axes)
        {
            var map = new Dictionary<string, BaseAxis>(StringComparer.OrdinalIgnoreCase);
            foreach (BaseAxis axis in axes ?? Enumerable.Empty<BaseAxis>())
            {
                if (axis == null || string.IsNullOrWhiteSpace(axis.Name))
                    continue;

                string key = InterlockCheckMatrix.NormalizeName(axis.Name);
                if (!map.ContainsKey(key))
                    map.Add(key, axis);
            }
            return map;
        }

        private static IReadOnlyDictionary<string, BaseCylinder> BuildCylinderMap(IEnumerable<BaseCylinder> cylinders)
        {
            var map = new Dictionary<string, BaseCylinder>(StringComparer.OrdinalIgnoreCase);
            foreach (BaseCylinder cylinder in cylinders ?? Enumerable.Empty<BaseCylinder>())
            {
                if (cylinder == null || string.IsNullOrWhiteSpace(cylinder.Name))
                    continue;

                string key = InterlockCheckMatrix.NormalizeName(cylinder.Name);
                if (!map.ContainsKey(key))
                    map.Add(key, cylinder);
            }
            return map;
        }
    }

    public sealed class MotionGuardService
    {
        private readonly InterlockCheckMatrix _matrix;

        public MotionGuardService()
            : this(InterlockCheckMatrixStore.LoadOrDefault())
        {
        }

        public MotionGuardService(InterlockCheckMatrix matrix)
        {
            _matrix = matrix ?? InterlockCheckMatrix.Default;
        }

        public MotionGuardResult VerifyAxisMove(BaseAxis axis, double targetPosition, MotionGuardContext context)
        {
            return VerifyAxisMove(axis, targetPosition, context, false);
        }

        public MotionGuardResult VerifyAxisMove(
            BaseAxis axis,
            double targetPosition,
            MotionGuardContext context,
            bool skipSharedRailXRule)
        {
            string movingName = axis != null ? axis.Name : string.Empty;
            return VerifyMove(movingName, targetPosition, MotionGuardMoveKind.AxisMove, string.Empty, context, skipSharedRailXRule);
        }

        public MotionGuardResult VerifyAxisHome(BaseAxis axis, double homeTargetPosition, MotionGuardContext context)
        {
            string movingName = axis != null ? axis.Name : string.Empty;
            return VerifyMove(movingName, homeTargetPosition, MotionGuardMoveKind.AxisHome, context);
        }

        public MotionGuardResult VerifyAxisTeachingMove(
            BaseAxis axis,
            double targetPosition,
            string targetName,
            MotionGuardContext context)
        {
            string movingName = axis != null ? axis.Name : string.Empty;
            return VerifyMove(movingName, targetPosition, MotionGuardMoveKind.AxisTeachingMove, targetName, context);
        }

        public MotionGuardResult VerifyCylinderMove(BaseCylinder cylinder, bool moveFwd, MotionGuardContext context)
        {
            string movingName = cylinder != null ? cylinder.Name : string.Empty;
            double targetValue = moveFwd ? 1.0 : 0.0;
            return VerifyMove(movingName, targetValue, MotionGuardMoveKind.CylinderMove, context);
        }

        public MotionGuardResult VerifyCylinderInitialize(BaseCylinder cylinder, bool moveFwd, MotionGuardContext context)
        {
            string movingName = cylinder != null ? cylinder.Name : string.Empty;
            double targetValue = moveFwd ? 1.0 : 0.0;
            return VerifyMove(movingName, targetValue, MotionGuardMoveKind.CylinderInitialize, context);
        }

        public MotionGuardResult VerifyMove(string movingName, double targetValue, MotionGuardContext context)
        {
            return VerifyMove(movingName, targetValue, MotionGuardMoveKind.AxisMove, context);
        }

        public MotionGuardResult VerifyMove(
            string movingName,
            double targetValue,
            MotionGuardMoveKind moveKind,
            MotionGuardContext context)
        {
            return VerifyMove(movingName, targetValue, moveKind, string.Empty, context);
        }

        public MotionGuardResult VerifyMove(
            string movingName,
            double targetValue,
            MotionGuardMoveKind moveKind,
            string targetName,
            MotionGuardContext context)
        {
            return VerifyMove(movingName, targetValue, moveKind, targetName, context, false);
        }

        public MotionGuardResult VerifyMove(
            string movingName,
            double targetValue,
            MotionGuardMoveKind moveKind,
            string targetName,
            MotionGuardContext context,
            bool skipSharedRailXRule)
        {
            string movingKey = InterlockCheckMatrix.NormalizeName(movingName);
            IReadOnlyList<InterlockCheckPair> checks = _matrix.GetChecksFor(movingKey);

            var result = new MotionGuardResult
            {
                Allowed = true,
                MovingKey = movingKey,
                RequiredChecks = checks
            };

            var request = new MotionGuardRuleContext(
                movingName,
                movingKey,
                targetValue,
                moveKind,
                targetName,
                checks,
                context,
                skipSharedRailXRule);
            string ruleReason;
            if (!MotionGuardRuleRegistry.Verify(request, out ruleReason))
            {
                result.Allowed = false;
                result.Message = ruleReason;
                return result;
            }

            if (checks.Count == 0)
            {
                result.Message = "No matrix check is required.";
                return result;
            }

            result.Message = BuildMessage(movingKey, targetValue, targetName, checks, context);
            return result;
        }

        private static string BuildMessage(
            string movingKey,
            double targetValue,
            string targetName,
            IReadOnlyList<InterlockCheckPair> checks,
            MotionGuardContext context)
        {
            string targets = string.Join(", ", checks.Select(x =>
                x.CheckKey + "@" + x.SourceCell).ToArray());

            int missingCount = CountMissingTargets(checks, context);
            string suffix = missingCount > 0
                ? " Missing mapped targets=" + missingCount + "."
                : string.Empty;

            return "Matrix check required. moving=" + movingKey +
                   ", target=" + targetValue.ToString("F3") +
                   (string.IsNullOrWhiteSpace(targetName) ? "" : ", targetName=" + targetName) +
                   ", checks=[" + targets + "]." + suffix;
        }

        private static int CountMissingTargets(IReadOnlyList<InterlockCheckPair> checks, MotionGuardContext context)
        {
            if (context == null)
                return checks.Count;

            int missing = 0;
            foreach (InterlockCheckPair check in checks)
            {
                if (check.CheckKind == InterlockTargetKind.Axis)
                {
                    if (!context.Axes.ContainsKey(check.CheckKey))
                        missing++;
                }
                else
                {
                    if (!context.Cylinders.ContainsKey(check.CheckKey))
                        missing++;
                }
            }
            return missing;
        }
    }
}
