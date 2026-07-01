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
                QMC.Common.Alarms.AlarmManager.Raise(QMC.Common.Alarms.AlarmSeverity.Error, "MOTION-GUARD", "INTERLOCK", reason);
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

        public static bool IsAxisNotHomedOrAtHomePosition(BaseAxis axis, string axisName, out string reason)
        {
            reason = string.Empty;
            if (axis == null)
                return true;

            if (axis.IsMoving)
            {
                reason = axisName + " is moving. actual=" + axis.ActualPosition.ToString("0.###");
                return false;
            }

            if (!axis.IsHomeDone)
                return true;

            const double homePosition = 0.0;
            double tolerance = ResolveTolerance(axis);
            if (IsAt(axis, homePosition, tolerance))
                return true;

            reason = axisName +
                     " homeDone=ON but not at Home position. target=0, actual=" +
                     axis.ActualPosition.ToString("0.###") +
                     ", tolerance=" + tolerance.ToString("0.###");
            return false;
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

        public static bool IsReticleRetracted(CDT320_Machine machine)
        {
            VisionUnit vision = machine != null ? machine.VisionUnit : null;
            if (vision == null)
                return true;

            return vision.IsVisionReticleDown() &&
                   vision.IsVisionReticleFrontSideBackward() &&
                   vision.IsVisionReticleRearSideBackward();
        }

        public static bool VerifyReticleRetractedBeforePickerZWorkMove(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (request.MoveKind == MotionGuardMoveKind.AxisHome)
                return true;

            string targetName = request.TargetName ?? string.Empty;
            if (IsSafeTeachingTarget(targetName) || IsPickerZSafeRetreatTarget(request))
                return true;

            VisionUnit vision = request.Machine.VisionUnit;
            if (vision == null || IsReticleRetracted(request.Machine))
                return true;

            string movingName = string.IsNullOrWhiteSpace(request.MovingName) ? "PickerZ" : request.MovingName;
            return Block(
                movingName,
                movingName + " 이동 불가: Reticle이 Bottom 카메라 위치에 있거나 안전 복귀 상태가 아닙니다. " +
                "PickerZ 공정/하강 이동 전 Reticle은 Down + Front Back + Rear Back 상태여야 합니다. " +
                BuildReticleStateDetail(vision) +
                ", target=" + request.TargetValue.ToString("0.###") +
                ", targetName=" + (string.IsNullOrWhiteSpace(targetName) ? "-" : targetName),
                out reason);
        }

        public static string BuildReticleStateDetail(VisionUnit vision)
        {
            if (vision == null)
                return "reticle=none";

            return "reticleDown=" + vision.IsVisionReticleDown() +
                   ", reticleUp=" + vision.IsVisionReticleUp() +
                   ", frontBack=" + vision.IsVisionReticleFrontSideBackward() +
                   ", frontForward=" + vision.IsVisionReticleFrontSideForward() +
                   ", rearBack=" + vision.IsVisionReticleRearSideBackward() +
                   ", rearForward=" + vision.IsVisionReticleRearSideForward();
        }

        private static bool IsPickerZSafeRetreatTarget(MotionGuardRuleContext request)
        {
            if (request == null)
                return false;

            string targetName = request.TargetName ?? string.Empty;
            if (targetName.IndexOf("WaitPosition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                targetName.IndexOf("StandbyPosition", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            BaseAxis axis = request.GetAxis(request.MovingName);
            double tolerance = axis != null ? ResolveTolerance(axis) : DefaultPositionTolerance;
            return Math.Abs(request.TargetValue) <= tolerance;
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
