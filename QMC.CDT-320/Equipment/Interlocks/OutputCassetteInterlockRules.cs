namespace QMC.CDT320.Interlocks
{
    public static class OutputCassetteInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputLifterZ", "ElevatorZ_Output", "OutputLifterZ"))
                return VerifyBinLifterZ(request, out reason);

            return true;
        }

        private static bool VerifyBinLifterZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            return true;
        }
    }
}
