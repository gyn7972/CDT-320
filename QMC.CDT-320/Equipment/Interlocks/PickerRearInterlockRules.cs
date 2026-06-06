namespace QMC.CDT320.Interlocks
{
    public static class PickerRearInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerX"))
                return VerifyRearPickerX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerY"))
                return VerifyRearPickerY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerT0", "RearPickerT1", "RearPickerT2", "RearPickerT3"))
                return VerifyRearPickerT(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3"))
                return VerifyRearPickerZ(request, out reason);

            return true;
        }

        private static bool VerifyRearPickerX(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyRearPickerY(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyRearPickerT(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyRearPickerZ(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
    }
}
