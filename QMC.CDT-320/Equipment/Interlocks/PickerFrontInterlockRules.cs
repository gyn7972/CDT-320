namespace QMC.CDT320.Interlocks
{
    public static class PickerFrontInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerX"))
                return VerifyFrontPickerX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerY"))
                return VerifyFrontPickerY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerT0", "FrontPickerT1", "FrontPickerT2", "FrontPickerT3"))
                return VerifyFrontPickerT(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3"))
                return VerifyFrontPickerZ(request, out reason);

            return true;
        }

        private static bool VerifyFrontPickerX(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyFrontPickerY(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyFrontPickerT(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyFrontPickerZ(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
    }
}
