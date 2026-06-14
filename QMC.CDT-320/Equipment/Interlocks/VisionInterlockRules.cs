using QMC.Common.IO;
using QMC.Common.Motion;

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
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveFrontSideVisionY(request.Machine, out reason);
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

            if (!VerifyInputStageClear(machine, "FrontSideVisionY", out reason))
                return false;

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "FrontSideVisionY", out reason);
        }

        private static bool VerifyRearSideVisionY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveRearSideVisionY(request.Machine, out reason);
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

            if (!VerifyInputStageClear(machine, "RearSideVisionY", out reason))
                return false;

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "RearSideVisionY", out reason);
        }

        private static bool VerifyReticleLift(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            return CanMoveReticleLift(request.Machine, out reason);
        }

        private static bool CanHomeReticleLift(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanMoveReticleLift(CDT320_Machine machine, out string reason)
        {
            if (!CanHomeReticleLift(machine, out reason))
                return false;

            if (!VerifyInputStageClear(machine, "ReticleLift", out reason))
                return false;

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleLift", out reason);
        }

        private static bool VerifyReticleFrontSlide(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            return CanMoveReticleFrontSlide(request.Machine, out reason);
        }

        private static bool CanHomeReticleFrontSlide(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanMoveReticleFrontSlide(CDT320_Machine machine, out string reason)
        {
            if (!CanHomeReticleFrontSlide(machine, out reason))
                return false;

            return VerifyVisionNotBusy(machine != null ? machine.VisionUnit : null, "ReticleSideSlideFront", out reason);
        }

        private static bool VerifyReticleRearSlide(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            return CanMoveReticleRearSlide(request.Machine, out reason);
        }

        private static bool CanHomeReticleRearSlide(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanMoveReticleRearSlide(CDT320_Machine machine, out string reason)
        {
            if (!CanHomeReticleRearSlide(machine, out reason))
                return false;

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
    }
}
