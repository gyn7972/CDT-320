namespace QMC.CDT320.Interlocks
{
    public static class InputStageInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferStageY", "StageY", "WaferY"))
                return VerifyWaferStageY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferStageT", "StageT", "WaferT"))
                return VerifyWaferStageT(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferExpandingZ", "ExpanderZ"))
                return VerifyWaferExpandingZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferVisionX", "CameraX"))
                return VerifyWaferVisionX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NeedleX", "NeedleBlockX"))
                return VerifyNeedleX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NeedleZ"))
                return VerifyNeedleZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "EjectPinZ"))
                return VerifyEjectPinZ(request, out reason);

            return true;
        }

        private static bool VerifyWaferStageY(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyWaferStageT(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyWaferExpandingZ(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyWaferVisionX(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyNeedleX(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyNeedleZ(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyEjectPinZ(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
    }
}
