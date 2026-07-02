using System;
using QMC.CDT320.Calibration;
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
                || moveKind == MotionGuardMoveKind.AxisContinuousJog
                || moveKind == MotionGuardMoveKind.AxisStepJog
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

        public static bool IsColletCalibrationFineAlignMove(MotionGuardRuleContext request, bool isFront, out string detail)
        {
            detail = string.Empty;

            try
            {
                if (request == null || request.Machine == null)
                {
                    detail = "request 또는 machine 정보가 없습니다.";
                    return false;
                }

                string targetName = request.TargetName ?? string.Empty;
                if (targetName.IndexOf("ColletCalibrationFineAlign", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    detail = "ColletCalibrationFineAlign targetName이 아닙니다.";
                    return false;
                }

                if (request.MoveKind != MotionGuardMoveKind.AxisTeachingMove)
                {
                    detail = "자동 티칭 이동이 아닙니다. moveKind=" + request.MoveKind;
                    WriteColletFineAlignDecision(request, isFront, false, detail);
                    return false;
                }

                if (targetName.IndexOf("PickerZone=Bottom", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    detail = "Bottom zone 미세 정렬 이동이 아닙니다. targetName=" + targetName;
                    WriteColletFineAlignDecision(request, isFront, false, detail);
                    return false;
                }

                if (!IsMoving(request,
                    isFront ? "FrontPickerX" : "RearPickerX",
                    isFront ? "FrontPickerY" : "RearPickerY"))
                {
                    detail = "Picker X/Y 이동이 아닙니다. moving=" + request.MovingName;
                    WriteColletFineAlignDecision(request, isFront, false, detail);
                    return false;
                }

                PickerWorkZone workZone;
                string owner;
                if (!PickerZoneInterlockRules.TryGetPickerWorkArea(isFront, out workZone, out owner) ||
                    workZone != PickerWorkZone.Bottom ||
                    owner.IndexOf("ColletCalibration", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    detail = "ColletCalibration Bottom 작업 점유 상태가 아닙니다. workZone=" + workZone +
                        ", owner=" + (string.IsNullOrWhiteSpace(owner) ? "-" : owner);
                    WriteColletFineAlignDecision(request, isFront, false, detail);
                    return false;
                }

                BaseAxis axis = request.GetAxis(request.MovingName);
                if (axis == null)
                {
                    detail = "이동 축 정보를 찾을 수 없습니다. moving=" + request.MovingName;
                    WriteColletFineAlignDecision(request, isFront, false, detail);
                    return false;
                }

                double maxMove = ResolveColletFineAlignMaxMoveMm(request.Machine);
                double distance = Math.Abs(request.TargetValue - axis.ActualPosition);
                if (distance > maxMove)
                {
                    detail = "ColletCalibrationFineAlign 이동량이 허용값을 초과했습니다. moving=" + request.MovingName +
                        ", distance=" + distance.ToString("F6") +
                        ", max=" + maxMove.ToString("F6") +
                        ", actual=" + axis.ActualPosition.ToString("F6") +
                        ", target=" + request.TargetValue.ToString("F6");
                    WriteColletFineAlignDecision(request, isFront, false, detail);
                    return false;
                }

                detail = "ColletCalibrationFineAlign 허용. moving=" + request.MovingName +
                    ", distance=" + distance.ToString("F6") +
                    ", max=" + maxMove.ToString("F6") +
                    ", owner=" + owner;
                WriteColletFineAlignDecision(request, isFront, true, detail);
                return true;
            }
            catch (Exception ex)
            {
                detail = "ColletCalibrationFineAlign 판정 중 예외가 발생했습니다. error=" + ex.Message;
                WriteColletFineAlignDecision(request, isFront, false, detail);
                return false;
            }
            finally
            {
            }
        }

        private static void WriteColletFineAlignDecision(MotionGuardRuleContext request, bool isFront, bool allowed, string detail)
        {
            try
            {
                if (request == null)
                    return;

                string targetName = request.TargetName ?? string.Empty;
                if (targetName.IndexOf("ColletCalibrationFineAlign", StringComparison.OrdinalIgnoreCase) < 0)
                    return;

                PickerWorkZone workZone;
                string owner;
                bool workArea = PickerZoneInterlockRules.TryGetPickerWorkArea(isFront, out workZone, out owner);
                BaseAxis axis = request.GetAxis(request.MovingName);
                double actual = axis != null ? axis.ActualPosition : 0.0;
                double maxMove = ResolveColletFineAlignMaxMoveMm(request.Machine);
                double distance = axis != null ? Math.Abs(request.TargetValue - axis.ActualPosition) : 0.0;

                QMC.Common.Log.Write("Calibration", "SYSTEM", "ColletCalFineAlignGuard",
                    "ColletCalibrationFineAlign 인터락 판정. side=" + (isFront ? "Front" : "Rear") +
                    ", allowed=" + allowed +
                    ", detail=" + (string.IsNullOrWhiteSpace(detail) ? "-" : detail) +
                    ", moving=" + request.MovingName +
                    ", moveKind=" + request.MoveKind +
                    ", originalMoveKind=" + request.OriginalMoveKind +
                    ", executionMode=" + request.ExecutionMode +
                    ", target=" + request.TargetValue.ToString("F6") +
                    ", targetName=" + targetName +
                    ", axisActual=" + (axis != null ? actual.ToString("F6") : "null") +
                    ", distance=" + (axis != null ? distance.ToString("F6") : "null") +
                    ", max=" + maxMove.ToString("F6") +
                    ", workArea=" + workArea +
                    ", workZone=" + workZone +
                    ", owner=" + (string.IsNullOrWhiteSpace(owner) ? "-" : owner));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static double ResolveColletFineAlignMaxMoveMm(CDT320_Machine machine)
        {
            try
            {
                ColletCalibrationSettings settings = machine != null &&
                    machine.VisionUnit != null &&
                    machine.VisionUnit.Config != null &&
                    machine.VisionUnit.Config.CalibrationData != null &&
                    machine.VisionUnit.Config.CalibrationData.Collet != null
                        ? machine.VisionUnit.Config.CalibrationData.Collet.Settings
                        : null;

                if (settings != null)
                {
                    settings.EnsureDefaults();
                    return settings.FineAlignMaxXyMoveMm;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return 0.2;
        }

        private static double ResolveTolerance(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return DefaultPositionTolerance;
        }
    }
}
