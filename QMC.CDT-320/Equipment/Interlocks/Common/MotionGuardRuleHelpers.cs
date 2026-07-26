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

        /// <summary>
        /// 인터락 제3 분기(사용자 승인 2026-07-24): 이동 축의 목표 위치와 상대 공유 레일 축의
        /// Actual/Command 양쪽 모두가 SharedRailX 페어 간격식으로 SafetyDistance를 만족하면 true.
        /// 상대 축이 회피(멀어지는) 이동 중이면 Command가 더 멀어 통과하고, 접근 이동 중이면
        /// Command 판정에서 차단된다(fail-closed). RetreatExtra는 절대 포함하지 않는다 — R5.
        /// </summary>
        public static bool IsPairClearanceSatisfiedForEntry(
            CDT320_Machine machine,
            BaseAxis movingAxis,
            double movingTargetPosition,
            BaseAxis otherAxis,
            out string detail)
        {
            detail = string.Empty;
            if (machine == null || movingAxis == null || otherAxis == null)
                return false;

            QMC.CDT320.Motion.SharedRailX.SharedRailXMotionService service =
                QMC.CDT320.Motion.SharedRailX.SharedRailXMotionRuntime.ResolveService(machine);
            if (service == null)
                return false;

            string actualDetail;
            string commandDetail;
            bool actualOk = service.IsPairClearanceSatisfied(
                movingAxis, movingTargetPosition, otherAxis, otherAxis.ActualPosition, out actualDetail);
            bool commandOk = service.IsPairClearanceSatisfied(
                movingAxis, movingTargetPosition, otherAxis, otherAxis.CommandPosition, out commandDetail);
            detail = "actual[" + actualDetail + "], command[" + commandDetail + "]";
            return actualOk && commandOk;
        }

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

        // 인터락 기준: Step/Continuous Jog 요청은 MoveKind와 TargetName 힌트를 함께 보고 판정한다.
        /// <summary>
        /// 구동 중 위치 오버라이드(팔로잉 중간 세그먼트) 요청인지 판정한다.
        /// 인터락 기준(사용자 승인 2026-07-25): 중간 좌표는 어떤 티칭 존에도 속하지 않아 목표 존이
        /// Unknown이 되고, 최종 목표의 존 진입 조건은 팔로잉 최초 명령(AxisMove)에서 이미 검증된다.
        /// 이 요청은 존 판정을 생략하고 위치 기반 안전만 확인한다(기존 Jog 경로와 동일한 방식).
        /// </summary>
        public static bool IsPositionOverrideStep(MotionGuardRuleContext request)
        {
            return request != null && request.IsPositionOverrideStep;
        }

        public static bool IsJogMove(MotionGuardRuleContext request)
        {
            if (request == null)
                return false;

            if (request.MoveKind == MotionGuardMoveKind.AxisContinuousJog ||
                request.MoveKind == MotionGuardMoveKind.AxisStepJog ||
                request.OriginalMoveKind == MotionGuardMoveKind.AxisContinuousJog ||
                request.OriginalMoveKind == MotionGuardMoveKind.AxisStepJog)
                return true;

            string targetName = request.TargetName ?? string.Empty;
            return targetName.IndexOf("JogStep", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   targetName.IndexOf("StepJog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   targetName.IndexOf("ContinuousJog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   targetName.IndexOf("JogContinuous", StringComparison.OrdinalIgnoreCase) >= 0;
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

        // 절대 인터락: HOME을 제외한 모든 PickerZ 이동은 Reticle Down + 양 Slide Back을 요구한다.
        public static bool VerifyReticleRetractedBeforeAnyNonHomePickerZMove(
            MotionGuardRuleContext request,
            out string reason)
        {
            reason = string.Empty;
            string movingName = request != null && !string.IsNullOrWhiteSpace(request.MovingName)
                ? request.MovingName
                : "PickerZ";

            if (request == null || request.Machine == null)
                return Block(
                    movingName,
                    movingName + " 절대 인터락 확인 불가: Motion request/Machine 정보가 없습니다.",
                    out reason);

            if (request.MoveKind == MotionGuardMoveKind.AxisHome)
                return true;

            VisionUnit vision = request.Machine.VisionUnit;
            if (vision == null)
                return Block(
                    movingName,
                    movingName + " 절대 인터락 확인 불가: VisionUnit 정보가 없습니다.",
                    out reason);

            if (vision.IsVisionReticleDown() &&
                vision.IsVisionReticleFrontSideBackward() &&
                vision.IsVisionReticleRearSideBackward())
                return true;

            return Block(
                movingName,
                movingName + " 이동 불가: HOME 외 PickerZ 이동 전 Reticle Down + Front Back + Rear Back이 필요합니다. " +
                BuildReticleStateDetail(vision),
                out reason);
        }

        // 절대 인터락 공통 판정: PickerY 실제 위치가 축 tolerance 안의 정확한 teaching Avoid인지 확인한다.
        public static bool IsPickerYAtExactTeachingAvoid(CDT320_Machine machine, bool isFront)
        {
            BaseAxis axis = ResolvePickerYAxis(machine, isFront);
            return axis != null && IsPickerYAtExactTeachingAvoid(machine, isFront, axis.ActualPosition);
        }

        // 절대 인터락 공통 판정: 지정 PickerY 위치가 축 tolerance 안의 정확한 teaching Avoid인지 확인한다.
        public static bool IsPickerYAtExactTeachingAvoid(
            CDT320_Machine machine,
            bool isFront,
            double position)
        {
            try
            {
                BaseAxis axis = ResolvePickerYAxis(machine, isFront);
                if (axis == null)
                    return false;

                if (isFront)
                {
                    if (machine.PickerFrontUnit == null ||
                        machine.PickerFrontUnit.Recipe == null ||
                        machine.PickerFrontUnit.Recipe.PickerY == null)
                        return false;
                }
                else if (machine.PickerRearUnit == null ||
                         machine.PickerRearUnit.Recipe == null ||
                         machine.PickerRearUnit.Recipe.PickerY == null)
                {
                    return false;
                }

                double avoid = isFront
                    ? machine.PickerFrontUnit.GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition")
                    : machine.PickerRearUnit.GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");
                return Math.Abs(position - avoid) <= ResolveTolerance(axis);
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static BaseAxis ResolvePickerYAxis(CDT320_Machine machine, bool isFront)
        {
            if (machine == null)
                return null;

            return isFront
                ? (machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerY : null)
                : (machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerY : null);
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

        // PickUpZHold 면제(사용자 승인 2026-07-26): Auto Conti 픽업의 die 간 이동에서 지정 픽커 Z가
        // PrePick 높이를 유지한 채 X/Y 이동을 허용하는 Input존 한정 면제 판정.
        // 시퀀스가 "Auto + ContiSegmentedPickUp + Needle 작업영역 반경 게이트 충족"일 때만
        // targetName에 "PickUpZHold={pickerNo}" 토큰을 부착하며, 이 판정은 그 토큰과 함께
        // InspectionZHold/InspectionContinuous/From=Input;To=Input/PickerZone=Input 태그 전부와
        // Auto 계열 이동 종류(AxisTeachingMove 또는 위치 오버라이드 스텝)를 요구한다.
        // 면제 범위는 "해당 픽커 Z의 위치 요구"뿐이다 — 비이동 요구와 다른 픽커 Z 요구는
        // 호출부에서 기존 그대로 유지해야 한다(인터락 완화 최소화).
        public static bool TryGetPickUpZHoldExemptPickerIndex(MotionGuardRuleContext request, out int exemptPickerIndex)
        {
            exemptPickerIndex = -1;
            if (request == null || request.Intent == null)
                return false;

            MotionGuardMoveIntent intent = request.Intent;
            if (!intent.PickUpZHoldPickerNo.HasValue)
                return false;
            if (!intent.InspectionZHold || !intent.InspectionContinuous)
                return false;
            if (intent.PickerZone != PickerWorkZone.Input ||
                intent.InspectionFromZone != PickerWorkZone.Input ||
                intent.InspectionToZone != PickerWorkZone.Input)
                return false;
            if (request.MoveKind != MotionGuardMoveKind.AxisTeachingMove &&
                !request.IsPositionOverrideStep)
                return false;

            int pickerNo = (int)System.Math.Round(intent.PickUpZHoldPickerNo.Value);
            if (pickerNo < 1 || pickerNo > 4)
                return false;

            exemptPickerIndex = pickerNo - 1;
            return true;
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
                if (request.Intent == null || !request.Intent.Contains("ColletCalibrationFineAlign"))
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

                if (request.Intent.PickerZone != PickerWorkZone.Bottom)
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
