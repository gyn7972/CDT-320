using System;
using QMC.Common.IO;
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

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                    break;
                case MotionGuardMoveKind.AxisTeachingMove:
                    if (IsSafeTeachingTarget(request.TargetName))
                        return VerifyInputFeederYSafeTeachingMove(machine, request.TargetName, out reason);
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyInputFeederYHome(machine, out reason);
                default:
                    return true;
            }

            if (cassette != null && cassette.InputLifterZ != null && cassette.InputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputLifterZ is moving. InputFeederY move is blocked.",
                    out reason);

            if (stage == null)
                return true;

            if (!IsInputStageYAtLoadOrUnload(stage))
                return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage StageY must be at Loading or Unloading position.", out reason);

            if (!IsExpanderZAtLoadOrUnload(stage))
                return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage ExpanderZ must be at Loading or Unloading position.", out reason);



            return true;
        }

        private static bool VerifyInputFeederYSafeTeachingMove(
            CDT320_Machine machine,
            string targetName,
            out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            InputFeederUnit feeder = machine.InputFeederUnit;
            if (feeder != null && feeder.FeederY != null && feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY is already moving. InputFeederY teaching move is blocked.",
                    out reason);

            return true;
        }

        private static bool VerifyInputFeederYHome(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (machine == null)
                    return true;

                InputCassetteUnit cassette = machine.InputCassetteUnit;
                if (cassette != null && cassette.InputLifterZ != null && cassette.InputLifterZ.IsMoving)
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputLifterZ is moving. InputFeederY home is blocked.",
                        out reason);

                if (!IsFrontPickerXInAvoidPosition(machine.PickerFrontUnit))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. FrontPickerX must be at Avoid position.",
                        out reason);

                if (!IsRearPickerXInAvoidPosition(machine.PickerRearUnit))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. RearPickerX must be at Avoid position.",
                        out reason);

                InputFeederUnit feeder = machine.InputFeederUnit;
                if (feeder == null)
                    return true;

                if (feeder.IsWaferFeederOverload())
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder overload sensor is detected.",
                        out reason);

                if (!feeder.IsWaferFeederSimulationOrDryRun())
                {
                    if (!IsFeederUnclamp(feeder))
                        return MotionGuardRuleHelpers.Block(
                            "InputFeederY",
                            "InputFeederY HOME blocked. InputFeeder must be unclamped.",
                            out reason);

                    if (feeder.IsWaferFeederRingCheck())
                        return MotionGuardRuleHelpers.Block(
                            "InputFeederY",
                            "InputFeederY HOME blocked. InputFeeder ring check is detected.",
                            out reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "Exception occurred while verifying InputFeederY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
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

        private static bool IsSafeTeachingTarget(string targetName)
        {
            string name = NormalizeTargetName(targetName);
            return string.Equals(name, "Avoid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Ready", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Safe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "CassetteExchange", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "CassetteLoad", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "CassetteUnload", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "WaferLoad", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "WaferUnload", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "WaferBarcode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "WaferLoadAvoid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "WaferUnloadAvoid", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeTargetName(string targetName)
        {
            string name = targetName ?? string.Empty;
            name = name.Replace("InputFeederY.", string.Empty);
            name = name.Replace("1_WaferFeederY.", string.Empty);
            name = name.Replace("Position", string.Empty);
            name = name.Replace("Pos", string.Empty);
            return name.Trim();
        }

        private static bool IsExpanderZAtLoadOrUnload(InputStageUnit stage)
        {
            if (stage == null || stage.ExpanderZ == null)
                return true;

            StageAxisPositions waferZ = stage.Recipe != null ? stage.Recipe.WaferZ : null;
            return waferZ != null && (IsAt(stage.ExpanderZ, waferZ.LoadPosition) || IsAt(stage.ExpanderZ, waferZ.UnloadPosition));
        }

        private static bool IsInputVisionXInAvoidPosition(InputStageUnit stage)
        {
            if (stage == null)
                return true;

            StageAxisPositions visionX = stage.Recipe != null ? stage.Recipe.VisionX : null;
            return visionX != null && IsAt(stage.CameraX, visionX.AvoidPosition);
        }

        private static bool IsFrontPickerXInAvoidPosition(PickerFrontUnit picker)
        {
            if (picker == null)
                return true;

            return IsAt(picker.PickerX, picker.GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition"));
        }

        private static bool IsRearPickerXInAvoidPosition(PickerRearUnit picker)
        {
            if (picker == null)
                return true;

            return IsAt(picker.PickerX, picker.GetPickerTeachingPosition(PickerAxis.PickerX, "AvoidPosition"));
        }

        private static bool IsFeederUp(InputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsWaferFeederUp())
                return true;

            BaseCylinder cylinder = feeder.InputFeederLift;
            return cylinder != null && cylinder.IsFwd;
        }

        private static bool IsFeederUnclamp(InputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsWaferFeederUnclamp())
                return true;

            BaseCylinder cylinder = feeder.InputFeederClamp;
            return cylinder != null && cylinder.IsBwd;
        }

        private static bool IsAt(BaseAxis axis, double target)
        {
            if (axis == null || double.IsNaN(target) || double.IsInfinity(target))
                return false;

            return Math.Abs(axis.ActualPosition - target) <= PositionTolerance;
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "InputFeederInterlock", reason + " - Blocked");
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
