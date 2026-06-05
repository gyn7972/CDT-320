namespace QMC.CDT320.Interlocks
{
    public static class OutputStageInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputGoodStageY", "GoodBinY", "OutputGoodStageY"))
                return VerifyBinGoodY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputGoodStageZ", "GoodBinZ", "OutputGoodStageZ"))
                return VerifyBinGoodZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputNGStageY", "NgBinY", "OutputNGStageY"))
                return VerifyBinNgY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "BinNgZ", "NgBinZ", "NgStage_StageZ"))
                return VerifyBinNgZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputVisionX", "OutputVisionX"))
                return VerifyBinVisionX(request, out reason);

            return true;
        }

        private static bool VerifyBinGoodY(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyBinGoodZ(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyBinNgY(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyBinNgZ(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
        private static bool VerifyBinVisionX(MotionGuardRuleContext request, out string reason) { reason = string.Empty; return true; }
    }
}
