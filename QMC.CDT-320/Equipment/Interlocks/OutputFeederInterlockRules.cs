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

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    request.MoveKind == MotionGuardMoveKind.AxisHome
                        ? "OutputLifterZ is moving. OutputFeederY home is blocked."
                        : "OutputLifterZ is moving. OutputFeederY move is blocked.",
                    out reason);

            if (request.MoveKind == MotionGuardMoveKind.AxisHome)
                return VerifyOutputFeederYHome(machine, out reason);

            if (request.MoveKind == MotionGuardMoveKind.AxisTeachingMove &&
                MotionGuardRuleHelpers.IsSafeTeachingTarget(request.TargetName))
                return true;

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
            OutputFeederUnit feeder = machine.OutputFeederUnit;

            //if (!IsOutputStageSafeForFeeder(machine.OutputStageUnit))
            //    return MotionGuardRuleHelpers.Block(
            //        "OutputFeederY",
            //        "Output stages and OutputVisionX must be at Avoid position before OutputFeederY home.",
            //        out reason);

            if (!IsFeederUnclamp(feeder))
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeeder must be unclamped before OutputFeederY home.",
                    out reason);

            if (!IsFeederUp(feeder))
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeeder must be up before OutputFeederY home.",
                    out reason);

            return true;
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

            return IsStageModuleAtAvoid(stage.GoodStage)
                && IsStageModuleAtAvoid(stage.NgStage)
                && MotionGuardRuleHelpers.IsAt(stage.OutputCameraX, stage.Recipe.VisionX.AvoidPosition);
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
    }
}
