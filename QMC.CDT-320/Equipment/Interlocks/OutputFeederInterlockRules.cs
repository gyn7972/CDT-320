namespace QMC.CDT320.Interlocks
{
    public static class OutputFeederInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "BinFeederY", "FeederY_Output", "OutputUnloader_FeederY"))
                return VerifyBinFeederY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputFeederLift", "BinFeeder Up/Down"))
                return VerifyOutputFeederLift(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputFeederClamp", "BinFeeder Clamp/UnClamp"))
                return VerifyOutputFeederClamp(request, out reason);

            return true;
        }

        private static bool VerifyBinFeederY(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyOutputFeederLift(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyOutputFeederClamp(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
    }
}
