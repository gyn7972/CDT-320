using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public static class OutputStageInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputGoodStageY", "GoodBinY", "GoodStage_StageY"))
                return VerifyBinGoodY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputGoodStageZ", "GoodBinZ", "GoodStage_StageZ"))
                return VerifyBinGoodZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputNGStageY", "NgBinY", "NgStage_StageY"))
                return VerifyBinNgY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputVisionX", "OutputVisionX"))
                return VerifyBinVisionX(request, out reason);

            return true;
        }

        private static bool VerifyBinGoodY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            OutputStageUnit stage = request.Machine.OutputStageUnit;
            if (!VerifyOutputTransportClear(request.Machine, "OutputGoodStageY", out reason))
                return false;

            if (stage != null && stage.NgStage != null && !stage.NgStage.IsAtAvoidPosition())
                return MotionGuardRuleHelpers.Block(
                    "OutputGoodStageY",
                    "NgStage must be at Avoid position before GoodStage Y move.",
                    out reason);

            return VerifyOutputStageNotBusy(stage, "OutputGoodStageY", out reason);
        }

        private static bool VerifyBinGoodZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (!VerifyOutputTransportClear(request.Machine, "OutputGoodStageZ", out reason))
                return false;

            return VerifyOutputStageNotBusy(request.Machine.OutputStageUnit, "OutputGoodStageZ", out reason);
        }

        private static bool VerifyBinNgY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            OutputStageUnit stage = request.Machine.OutputStageUnit;
            if (!VerifyOutputTransportClear(request.Machine, "OutputNGStageY", out reason))
                return false;

            if (stage != null && stage.GoodStage != null && !stage.GoodStage.IsAtAvoidPosition())
                return MotionGuardRuleHelpers.Block(
                    "OutputNGStageY",
                    "GoodStage Z must be at Avoid position before NgStage Y move.",
                    out reason);

            return VerifyOutputStageNotBusy(stage, "OutputNGStageY", out reason);
        }

        private static bool VerifyBinVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (!VerifyOutputTransportClear(request.Machine, "OutputVisionX", out reason))
                return false;

            return VerifyOutputStageNotBusy(request.Machine.OutputStageUnit, "OutputVisionX", out reason);
        }

        private static bool VerifyOutputTransportClear(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            OutputFeederUnit feeder = machine.OutputFeederUnit;
            if (feeder != null && feeder.FeederY != null && feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, "OutputFeederY is moving.", out reason);

            OutputCassetteUnit cassette = machine.OutputCassetteUnit;
            if (cassette != null && cassette.OutputLifterZ != null && cassette.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, "OutputLifterZ is moving.", out reason);

            return true;
        }

        private static bool VerifyOutputStageNotBusy(OutputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (stage == null)
                return true;

            if (IsMovingExcept(stage.GoodStage != null ? stage.GoodStage.StageY : null, movingName, "OutputGoodStageY", "GoodBinY", "GoodStage_StageY"))
                return MotionGuardRuleHelpers.Block(movingName, "GoodStage Y is moving.", out reason);
            if (IsMovingExcept(stage.GoodStage != null ? stage.GoodStage.StageZ : null, movingName, "OutputGoodStageZ", "GoodBinZ", "GoodStage_StageZ"))
                return MotionGuardRuleHelpers.Block(movingName, "GoodStage Z is moving.", out reason);
            if (IsMovingExcept(stage.NgStage != null ? stage.NgStage.StageY : null, movingName, "OutputNGStageY", "NgBinY", "NgStage_StageY"))
                return MotionGuardRuleHelpers.Block(movingName, "NgStage Y is moving.", out reason);
            if (IsMovingExcept(stage.OutputCameraX, movingName, "OutputVisionX"))
                return MotionGuardRuleHelpers.Block(movingName, "OutputVisionX is moving.", out reason);

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
    }
}
