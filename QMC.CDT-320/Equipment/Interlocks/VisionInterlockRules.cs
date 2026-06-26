using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;

namespace QMC.CDT320.Interlocks
{
    public static class VisionInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontSideVisionY", "FrontSideVisionY0"))
                return VerifyFrontSideVisionY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "RearSideVisionY", "RearSideVisionY0"))
                return VerifyRearSideVisionY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "ReticleLift", "Reticle Up/Down"))
                return VerifyReticleLift(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "ReticleSideSlideFront", "Reticle Front FW/BW"))
                return VerifyReticleFrontSlide(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "ReticleSideSlideRear", "Reticle Back FW/BW"))
                return VerifyReticleRearSlide(request, out reason);

            return true;
        }

        private static bool VerifyFrontSideVisionY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveFrontSideVisionY(request.Machine, out reason);
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontSideVisionY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeFrontSideVisionY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanMoveFrontSideVisionY(CDT320_Machine machine, out string reason)
        {
            if (!CanHomeFrontSideVisionY(machine, out reason))
                return false;

            // SideVisionY는 InputStage/InputVisionX와 기구 간섭이 없는 독립 검사축이다.
            // Auto 병렬 운전에서는 Input die vision 준비와 Side 검사 카메라 위치 이동이 겹칠 수 있으므로
            // InputStage/InputVisionX 이동 상태로 SideVisionY를 차단하지 않는다.
            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "FrontSideVisionY", out reason);
        }

        private static bool VerifyRearSideVisionY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveRearSideVisionY(request.Machine, out reason);
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeRearSideVisionY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeRearSideVisionY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanMoveRearSideVisionY(CDT320_Machine machine, out string reason)
        {
            if (!CanHomeRearSideVisionY(machine, out reason))
                return false;

            // SideVisionY는 InputStage/InputVisionX와 기구 간섭이 없는 독립 검사축이다.
            // Auto 병렬 운전에서는 Input die vision 준비와 Side 검사 카메라 위치 이동이 겹칠 수 있으므로
            // InputStage/InputVisionX 이동 상태로 SideVisionY를 차단하지 않는다.
            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "RearSideVisionY", out reason);
        }
        
        private static bool VerifyReticleLift(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeReticleLift(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveReticleLift(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ReticleLift",
                    "Exception occurred while verifying ReticleLift cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanInitializeReticleLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Up", "Bwd/Down");

            //if (!VerifyInputStageClear(machine, "ReticleLift", out reason))
            //{
            //    reason = "ReticleLift initialize " + direction + " blocked. " + reason;
            //    return false;
            //}

            // ReticleSideSlide Front/Rear가 모두 Backward 상태여야 ReticleLift 이동 가능.
            // (둘 중 하나라도 Backward가 아니면 차단/알람)
            VisionUnit vision = machine != null ? machine.VisionUnit : null;
            if (!vision.IsVisionReticleFrontSideBackward() || !vision.IsVisionReticleRearSideBackward())
                return MotionGuardRuleHelpers.Block(
                    "ReticleLift",
                    "ReticleLift move " + direction + " blocked. ReticleSideSlide Front/Rear가 모두 Backward 상태가 아닙니다.",
                    out reason);

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleLift", out reason);
        }

        private static bool CanMoveReticleLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Up", "Bwd/Down");

            //if (!VerifyInputStageClear(machine, "ReticleLift", out reason))
            //{
            //    reason = "ReticleLift move " + direction + " blocked. " + reason;
            //    return false;
            //}

            // ReticleSideSlide Front/Rear가 모두 Backward 상태여야 ReticleLift 이동 가능.
            // (둘 중 하나라도 Backward가 아니면 차단/알람)
            VisionUnit vision = machine != null ? machine.VisionUnit : null;
            if (!vision.IsVisionReticleFrontSideBackward() || !vision.IsVisionReticleRearSideBackward())
                return MotionGuardRuleHelpers.Block(
                    "ReticleLift",
                    "ReticleLift move " + direction + " blocked. ReticleSideSlide Front/Rear가 모두 Backward 상태가 아닙니다.",
                    out reason);

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleLift", out reason);
        }

        private static bool VerifyReticleFrontSlide(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeReticleFrontSlide(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveReticleFrontSlide(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ReticleSideSlideFront",
                    "Exception occurred while verifying ReticleSideSlideFront cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanInitializeReticleFrontSlide(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleSideSlideFront", out reason);
        }

        private static bool CanMoveReticleFrontSlide(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleSideSlideFront", out reason);
        }

        private static bool VerifyReticleRearSlide(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeReticleRearSlide(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveReticleRearSlide(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ReticleSideSlideRear",
                    "Exception occurred while verifying ReticleSideSlideRear cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanInitializeReticleRearSlide(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleSideSlideRear", out reason);
        }

        private static bool CanMoveReticleRearSlide(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleSideSlideRear", out reason);
        }

        private static bool VerifyInputStageClear(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            if (stage == null)
                return true;

            if (MotionGuardRuleHelpers.IsAxisMoving(stage.StageY))
                return MotionGuardRuleHelpers.Block(movingName, "InputStage StageY is moving.", out reason);
            if (MotionGuardRuleHelpers.IsAxisMoving(stage.CameraX))
                return MotionGuardRuleHelpers.Block(movingName, "InputVisionX is moving.", out reason);
            //if (MotionGuardRuleHelpers.IsAxisMoving(stage.ExpanderZ))
            //    return MotionGuardRuleHelpers.Block(movingName, "InputStage ExpanderZ is moving.", out reason);

            return true;
        }

        private static bool VerifyVisionNotBusy(VisionUnit vision, string movingName, out string reason)
        {
            reason = string.Empty;
            if (vision == null)
                return true;

            //서로 움직여도 상관없음.
            //if (IsMovingExcept(vision.FrontSideVisionY, movingName, "FrontSideVisionY", "FrontSideVisionY0"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontSideVisionY is moving.", out reason);
            //if (IsMovingExcept(vision.RearSideVisionY, movingName, "RearSideVisionY", "RearSideVisionY0"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearSideVisionY is moving.", out reason);


            // 간섭 무
            //if (IsCylinderMovingExcept(vision.ReticleLift, movingName, "ReticleLift", "Reticle Up/Down"))
            //    return MotionGuardRuleHelpers.Block(movingName, "ReticleLift is moving.", out reason);
            //if (IsCylinderMovingExcept(vision.ReticleFrontSideSlide, movingName, "ReticleSideSlideFront", "Reticle Front FW/BW"))
            //    return MotionGuardRuleHelpers.Block(movingName, "Reticle front slide is moving.", out reason);
            //if (IsCylinderMovingExcept(vision.ReticleRearSideSlide, movingName, "ReticleSideSlideRear", "Reticle Back FW/BW"))
            //    return MotionGuardRuleHelpers.Block(movingName, "Reticle rear slide is moving.", out reason);

            return true;
        }

        private static bool IsMovingExcept(BaseAxis axis, string movingName, params string[] names)
        {
            if (!MotionGuardRuleHelpers.IsAxisMoving(axis))
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(movingName, names[i], System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static bool IsCylinderMovingExcept(BaseCylinder cylinder, string movingName, params string[] names)
        {
            if (!MotionGuardRuleHelpers.IsCylinderMoving(cylinder))
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(movingName, names[i], System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static string ResolveCylinderDirection(double targetValue, string fwdText, string bwdText)
        {
            return targetValue >= 0.5 ? fwdText : bwdText;
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "VisionInterlock", reason + " - Blocked");
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
