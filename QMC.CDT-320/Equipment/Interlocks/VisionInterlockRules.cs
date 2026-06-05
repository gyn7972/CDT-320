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

        private static bool VerifyFrontSideVisionY(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyRearSideVisionY(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyReticleLift(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyReticleFrontSlide(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyReticleRearSlide(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
    }
}
