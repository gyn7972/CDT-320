using System;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    internal static class MotionGuardRuleHelpers
    {
        private const double DefaultPositionTolerance = 0.05;

        public static bool IsMoving(MotionGuardRuleContext request, params string[] names)
        {
            if (request == null || names == null)
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                string key = InterlockCheckMatrix.NormalizeName(name);
                if (string.Equals(request.MovingKey, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(request.MovingName, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool Block(string movingName, string message, out string reason)
        {
            reason = "Interlock blocked. moving=" + movingName + ". " + message;
            return false;
        }

        public static bool IsKnownMoveKind(MotionGuardMoveKind moveKind)
        {
            return moveKind == MotionGuardMoveKind.AxisMove
                || moveKind == MotionGuardMoveKind.AxisHome
                || moveKind == MotionGuardMoveKind.AxisTeachingMove
                || moveKind == MotionGuardMoveKind.CylinderMove
                || moveKind == MotionGuardMoveKind.CylinderInitialize;
        }

        public static bool BlockUnsupportedMoveKind(MotionGuardRuleContext request, out string reason)
        {
            string movingName = request != null ? request.MovingName : string.Empty;
            string moveKind = request != null ? request.MoveKind.ToString() : "<null>";
            bool result = Block(
                movingName,
                "Unsupported motion guard move kind. moveKind=" + moveKind + ". Motion is blocked for safety.",
                out reason);

            WriteUnsupportedMoveKindAlarm(reason);
            return result;
        }

        private static void WriteUnsupportedMoveKindAlarm(string reason)
        {
            try
            {
                EventLogger.Write(EventKind.Alarm, "INTERLOCK", "MOTION-GUARD", reason);
            }
            catch
            {
            }
        }

        public static bool IsAt(BaseAxis axis, double target)
        {
            return IsAt(axis, target, ResolveTolerance(axis));
        }

        public static bool IsAt(BaseAxis axis, double target, double tolerance)
        {
            if (axis == null || double.IsNaN(target) || double.IsInfinity(target))
                return false;

            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        public static bool IsAxisMoving(BaseAxis axis)
        {
            return axis != null && axis.IsMoving;
        }

        public static bool IsCylinderMoving(BaseCylinder cylinder)
        {
            return cylinder != null && !cylinder.IsFwd && !cylinder.IsBwd;
        }

        public static bool IsSafeTeachingTarget(string targetName)
        {
            string name = NormalizeTargetName(targetName);
            return string.Equals(name, "Avoid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Ready", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Safe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Home", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Exchange", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("Avoid", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("Exchange", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("Ready", StringComparison.OrdinalIgnoreCase)
                || name.IndexOf("Safe", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string NormalizeTargetName(string targetName)
        {
            string name = targetName ?? string.Empty;
            name = name.Replace("1_WaferFeederY.", string.Empty);
            name = name.Replace("InputFeederY.", string.Empty);
            name = name.Replace("OutputFeederY.", string.Empty);
            name = name.Replace("Position", string.Empty);
            name = name.Replace("Pos", string.Empty);
            name = name.Replace("_", string.Empty);
            name = name.Replace(" ", string.Empty);
            return name.Trim();
        }

        private static double ResolveTolerance(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return DefaultPositionTolerance;
        }
    }
}
