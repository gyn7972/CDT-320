using System;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public static class InputFeederInterlockRules
    {
        private const double PositionTolerance = 0.05;

        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederY", "FeederY"))
                return VerifyInputFeederY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederLift"))
                return VerifyInputFeederLift(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederClamp"))
                return VerifyInputFeederClamp(request, out reason);

            return true;
        }

        private static bool VerifyInputFeederY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;
            InputCassetteUnit cassette = machine.InputCassetteUnit;
            InputStageUnit stage = machine.InputStageUnit;

            // 움직이지 않아야함.
            if (cassette != null && cassette.InputLifterZ != null && cassette.InputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block("InputFeederY", "InputLifterZ is moving. InputFeederY move is blocked.", out reason);

            if (stage == null)
                return true;

            if (!IsInputStageYAtLoadOrUnload(stage))
                return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage StageY must be at Loading or Unloading position.", out reason);

            if (!IsExpanderZAtLoadOrUnload(stage))
                return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage ExpanderZ must be at Loading or Unloading position.", out reason);



            return true;
        }

        private static bool VerifyInputFeederLift(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

            if (machine.InputCassetteUnit != null &&
                machine.InputCassetteUnit.InputLifterZ != null &&
                machine.InputCassetteUnit.InputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block("InputFeederLift", "InputLifterZ is moving. InputFeederLift move is blocked.", out reason);

            if (machine.InputFeederUnit != null &&
                machine.InputFeederUnit.FeederY != null &&
                machine.InputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block("InputFeederLift", "InputFeederY is moving. InputFeederLift move is blocked.", out reason);

            return true;
        }

        private static bool VerifyInputFeederClamp(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

            if (machine.InputCassetteUnit != null &&
                machine.InputCassetteUnit.InputLifterZ != null &&
                machine.InputCassetteUnit.InputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block("InputFeederClamp", "InputLifterZ is moving. InputFeederClamp move is blocked.", out reason);

            if (machine.InputFeederUnit != null &&
                machine.InputFeederUnit.FeederY != null &&
                machine.InputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block("InputFeederClamp", "InputFeederY is moving. InputFeederClamp move is blocked.", out reason);

            return true;
        }

        private static bool IsInputStageYAtLoadOrUnload(InputStageUnit stage)
        {
            if (stage == null || stage.StageY == null)
                return true;

            StageAxisPositions waferY = stage.Recipe != null ? stage.Recipe.WaferY : null;
            return (waferY != null && IsAt(stage.StageY, waferY.ReadyPosition))
                   || (waferY != null && IsAt(stage.StageY, waferY.LoadPosition))
                   || (waferY != null && IsAt(stage.StageY, waferY.UnloadPosition));
        }

        private static bool IsExpanderZAtLoadOrUnload(InputStageUnit stage)
        {
            if (stage == null || stage.ExpanderZ == null)
                return true;

            StageAxisPositions waferZ = stage.Recipe != null ? stage.Recipe.WaferZ : null;
            return waferZ != null && IsAt(stage.ExpanderZ, waferZ.ReadyPosition);
        }

        private static bool IsAt(BaseAxis axis, double target)
        {
            if (axis == null || double.IsNaN(target) || double.IsInfinity(target))
                return false;

            return Math.Abs(axis.ActualPosition - target) <= PositionTolerance;
        }

    }
}
