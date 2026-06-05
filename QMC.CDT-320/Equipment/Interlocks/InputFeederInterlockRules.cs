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

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferFeederY", "FeederY"))
                return VerifyWaferFeederY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederLift"))
                return VerifyInputFeederLift(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederClamp"))
                return VerifyInputFeederClamp(request, out reason);

            return true;
        }

        private static bool VerifyWaferFeederY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;
            InputCassetteUnit cassette = machine.InputCassette;
            InputStageUnit stage = machine.InputStage;

            if (cassette != null && cassette.WaferLifterZ != null && cassette.WaferLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block("WaferFeederY", "WaferLifterZ is moving. WaferFeederY move is blocked.", out reason);

            if (stage == null)
                return true;

            if (!IsInputStageYAtLoadOrUnload(stage))
                return MotionGuardRuleHelpers.Block("WaferFeederY", "InputStage StageY must be at Loading or Unloading position.", out reason);

            if (!IsExpanderZAtLoadOrUnload(stage))
                return MotionGuardRuleHelpers.Block("WaferFeederY", "InputStage ExpanderZ must be at Loading or Unloading position.", out reason);

            return true;
        }

        private static bool VerifyInputFeederLift(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

            if (machine.InputCassette != null &&
                machine.InputCassette.WaferLifterZ != null &&
                machine.InputCassette.WaferLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block("InputFeederLift", "WaferLifterZ is moving. InputFeederLift move is blocked.", out reason);

            if (machine.InputFeeder != null &&
                machine.InputFeeder.FeederY != null &&
                machine.InputFeeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block("InputFeederLift", "WaferFeederY is moving. InputFeederLift move is blocked.", out reason);

            return true;
        }

        private static bool VerifyInputFeederClamp(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

            if (machine.InputCassette != null &&
                machine.InputCassette.WaferLifterZ != null &&
                machine.InputCassette.WaferLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block("InputFeederClamp", "WaferLifterZ is moving. InputFeederClamp move is blocked.", out reason);

            if (machine.InputFeeder != null &&
                machine.InputFeeder.FeederY != null &&
                machine.InputFeeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block("InputFeederClamp", "WaferFeederY is moving. InputFeederClamp move is blocked.", out reason);

            return true;
        }

        private static bool IsInputStageYAtLoadOrUnload(InputStageUnit stage)
        {
            if (stage == null || stage.StageY == null)
                return true;

            StageAxisPositions waferY = stage.Recipe != null ? stage.Recipe.WaferY : null;
            return IsAt(stage.StageY, stage.Setup.StageYTeachPosition)
                   || IsAt(stage.StageY, stage.Setup.UnloadPositionY)
                   || (waferY != null && IsAt(stage.StageY, waferY.LoadPosition))
                   || (waferY != null && IsAt(stage.StageY, waferY.UnloadPosition));
        }

        private static bool IsExpanderZAtLoadOrUnload(InputStageUnit stage)
        {
            if (stage == null || stage.ExpanderZ == null)
                return true;

            return IsAt(stage.ExpanderZ, stage.Setup.ExpanderDownPosition)
                   || IsAt(stage.ExpanderZ, stage.Setup.ExpanderUpPosition);
        }

        private static bool IsAt(BaseAxis axis, double target)
        {
            if (axis == null || double.IsNaN(target) || double.IsInfinity(target))
                return false;

            return Math.Abs(axis.ActualPosition - target) <= PositionTolerance;
        }

    }
}
