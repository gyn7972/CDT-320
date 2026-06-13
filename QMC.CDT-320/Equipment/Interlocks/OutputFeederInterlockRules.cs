using QMC.Common.IO;

namespace QMC.CDT320.Interlocks
{
    public static class OutputFeederInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputFeederY", "FeederY_Output", "OutputFeederY"))
                return VerifyBinFeederY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputFeederLift", "OutputFeeder Up/Down"))
                return VerifyOutputFeederLift(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputFeederClamp", "OutputFeeder Clamp/UnClamp"))
                return VerifyOutputFeederClamp(request, out reason);

            return true;
        }

        private static bool VerifyBinFeederY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                    break;
                case MotionGuardMoveKind.AxisTeachingMove:
                    if (MotionGuardRuleHelpers.IsSafeTeachingTarget(request.TargetName))
                        return true;
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyOutputFeederYHome(machine, out reason);
                default:
                    return true;
            }

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputLifterZ is moving. OutputFeederY move is blocked.",
                    out reason);

            if (!IsOutputStageSafeForFeeder(machine.OutputStageUnit))
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "Output stages and OutputVisionX must be at Avoid position before OutputFeederY move.",
                    out reason);

            return true;
        }

        private static bool VerifyOutputFeederYHome(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (machine == null)
                    return true;

                OutputCassetteUnit cassette = machine.OutputCassetteUnit;
                if (cassette != null && cassette.OutputLifterZ != null && cassette.OutputLifterZ.IsMoving)
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputLifterZ is moving. OutputFeederY home is blocked.",
                        out reason);

                if (cassette != null && !cassette.IsBinLifterZInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputLifterZ must be at Avoid position.",
                        out reason);

                OutputStageUnit outputStage = machine.OutputStageUnit;
                if (outputStage != null && !outputStage.IsVisionXInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputVisionX must be at Avoid position.",
                        out reason);

                if (outputStage != null && outputStage.GoodStage != null && !outputStage.GoodStage.IsAtAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. GoodBinZ(GoodStageZ) must be at Avoid position.",
                        out reason);

                if (outputStage != null &&
                    outputStage.GoodBinGuideDownSensor != null &&
                    !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                    !outputStage.GoodBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. Good Bin Guide must be down.",
                        out reason);

                OutputFeederUnit feeder = machine.OutputFeederUnit;
                if (feeder == null)
                    return true;

                if (feeder.IsFeederOverload())
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputFeeder overload sensor is detected.",
                        out reason);

                if (!feeder.IsOutputFeederSimulationOrDryRun() && !IsFeederUnclamp(feeder))
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputFeeder must be unclamped.",
                        out reason);

                if (!feeder.IsOutputFeederSimulationOrDryRun() && !IsFeederUp(feeder))
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputFeeder must be up.",
                        out reason);

                if (!feeder.IsOutputFeederSimulationOrDryRun() && feeder.IsBinFeederRingCheck())
                    return MotionGuardRuleHelpers.Block(
                        "OutputFeederY",
                        "OutputFeederY HOME blocked. OutputFeeder ring check is detected.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "Exception occurred while verifying OutputFeederY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool VerifyOutputFeederLift(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block("OutputFeederLift", "OutputLifterZ is moving.", out reason);

            if (machine.OutputFeederUnit != null &&
                machine.OutputFeederUnit.FeederY != null &&
                machine.OutputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block("OutputFeederLift", "OutputFeederY is moving.", out reason);

            return true;
        }

        private static bool VerifyOutputFeederClamp(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block("OutputFeederClamp", "OutputLifterZ is moving.", out reason);

            if (machine.OutputFeederUnit != null &&
                machine.OutputFeederUnit.FeederY != null &&
                machine.OutputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block("OutputFeederClamp", "OutputFeederY is moving.", out reason);

            return true;
        }

        private static bool IsOutputStageSafeForFeeder(OutputStageUnit stage)
        {
            if (stage == null)
                return true;

            return MotionGuardRuleHelpers.IsAt(stage.OutputCameraX, stage.Recipe.VisionX.AvoidPosition);
        }

        private static bool IsStageModuleAtAvoid(StageModule stage)
        {
            return stage == null || stage.IsAtAvoidPosition();
        }

        private static bool IsFeederUp(OutputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsFeederUp())
                return true;

            BaseCylinder cylinder = feeder.FeederUpDownCyl;
            return cylinder != null && cylinder.IsFwd;
        }

        private static bool IsFeederUnclamp(OutputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsFeederUnclamped())
                return true;

            BaseCylinder cylinder = feeder.FeederClampCyl;
            return cylinder != null && cylinder.IsBwd;
        }

        private static bool IsDryRunInput(BaseDigitalInput input)
        {
            return input != null && input.Config != null && input.Config.IgnoreWaits;
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "OutputFeederInterlock", reason + " - Blocked");
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
