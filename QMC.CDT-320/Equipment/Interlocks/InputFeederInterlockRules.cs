using System;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;

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

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanMoveInputFeederY(machine, out reason);
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveInputFeederY(machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeInputFeederY(machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveInputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            InputCassetteUnit cassette = machine.InputCassetteUnit;
            InputStageUnit stage = machine.InputStageUnit;
            if (cassette != null && cassette.InputLifterZ != null && cassette.InputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputLifterZ is moving. InputFeederY move is blocked.",
                    out reason);

            if (stage != null)
            {
                if (!IsInputStageYAtLoadOrUnload(stage))
                    return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage StageY must be at Loading or Unloading position.", out reason);

                if (!IsExpanderZAtLoadOrUnload(stage))
                    return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage ExpanderZ must be at Loading or Unloading position.", out reason);

                if (!IsInputVisionXInAvoidPosition(stage))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY move blocked. InputVisionX must be at Avoid position.",
                        out reason);
            }

            if (!IsFrontPickerInAvoidPosition(machine.PickerFrontUnit))
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY move blocked. FrontPicker must be at Avoid position.",
                    out reason);

            if (!IsRearPickerInAvoidPosition(machine.PickerRearUnit))
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY move blocked. RearPicker must be at Avoid position.",
                    out reason);

            InputFeederUnit feeder = machine.InputFeederUnit;
            if (feeder == null)
                return true;

            if (feeder.IsWaferFeederOverload())
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY move blocked. InputFeeder overload sensor is detected.",
                    out reason);

            return true;
        }

        private static bool CanHomeInputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (machine == null)
                    return true;

                InputCassetteUnit cassette = machine.InputCassetteUnit;
                if (cassette != null && cassette.InputLifterZ != null && cassette.InputLifterZ.IsMoving)
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputLifterZ is moving. InputFeederY home is blocked.",
                        out reason);
                }

                string axisReason;
                if (!IsInputVisionXHomeReadyForInputFeederHome(machine.InputStageUnit, out axisReason))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason,
                        out reason);

                if (!IsFrontPickerXHomeReadyForInputFeederHome(machine.PickerFrontUnit, out axisReason))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. FrontPickerX must be not homed yet or at Home position. " + axisReason,
                        out reason);

                if (!IsRearPickerXHomeReadyForInputFeederHome(machine.PickerRearUnit, out axisReason))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. RearPickerX must be not homed yet or at Home position. " + axisReason,
                        out reason);

                InputFeederUnit feeder = machine.InputFeederUnit;
                if (feeder == null)
                    return true;

                if (!VerifyInputFeederEmptyForHome(feeder, out reason))
                    return false;

                if (feeder.IsWaferFeederOverload())
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder overload sensor is detected.",
                        out reason);

                if (!ShouldBypassHardwareMechanismChecks())
                {
                    if (!IsFeederUnclamp(feeder))
                        return MotionGuardRuleHelpers.Block(
                            "InputFeederY",
                            "InputFeederY HOME blocked. InputFeeder must be unclamped.",
                            out reason);
                }

                if (!feeder.IsWaferFeederSimulationOrDryRun() && feeder.IsWaferFeederRingCheck())
                {
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
            try
            {
                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeInputFeederLift(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveInputFeederLift(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "Exception occurred while verifying InputFeederLift cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanInitializeInputFeederLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            string direction = targetValue >= 0.5 ? "Fwd/Up" : "Bwd/Down";

            if (machine.InputCassetteUnit != null &&
                machine.InputCassetteUnit.InputLifterZ != null &&
                machine.InputCassetteUnit.InputLifterZ.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputLifterZ is moving. InputFeederLift initialize move is blocked. direction=" + direction,
                    out reason);
            }

            if (machine.InputFeederUnit != null &&
                machine.InputFeederUnit.FeederY != null &&
                machine.InputFeederUnit.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederY is moving. InputFeederLift initialize move is blocked. direction=" + direction,
                    out reason);
            }

            if (machine.InputFeederUnit != null && IsFeederUnclamp(machine.InputFeederUnit) == false)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederLift initialize move blocked. InputFeeder must be unclamped before lift initialize. direction=" + direction,
                    out reason);
            }

            if (targetValue >= 0.5 &&
                machine.InputFeederUnit != null &&
                machine.InputFeederUnit.IsWaferFeederTransferDataOccupied())
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederLift initialize Fwd/Up blocked. InputFeeder material data exists before lift up.",
                    out reason);
            }

            if (targetValue >= 0.5 &&
                machine.InputFeederUnit != null &&
                !machine.InputFeederUnit.IsWaferFeederSimulationOrDryRun() &&
                machine.InputFeederUnit.IsWaferFeederRingCheck())
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederLift initialize Fwd/Up blocked. InputFeeder wafer detect sensor is ON before lift up.",
                    out reason);
            }

            return true;
        }

        private static bool CanMoveInputFeederLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            string direction = targetValue >= 0.5 ? "Fwd/Up" : "Bwd/Down";

            if (machine.InputCassetteUnit != null &&
                machine.InputCassetteUnit.InputLifterZ != null &&
                machine.InputCassetteUnit.InputLifterZ.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputLifterZ is moving. InputFeederLift move is blocked. direction=" + direction,
                    out reason);
            }

            if (machine.InputFeederUnit != null &&
                machine.InputFeederUnit.FeederY != null &&
                machine.InputFeederUnit.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederY is moving. InputFeederLift move is blocked. direction=" + direction,
                    out reason);
            }

            if (machine.InputFeederUnit != null &&
                IsFeederUnclamp(machine.InputFeederUnit) &&
                IsFeederHoldingMaterial(machine.InputFeederUnit))
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederLift move blocked. InputFeeder is unclamped and material is still detected. direction=" + direction,
                    out reason);
            }

            return true;
        }

        private static bool VerifyInputFeederClamp(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            try
            {
                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeInputFeederClamp(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveInputFeederClamp(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederClamp",
                    "Exception occurred while verifying InputFeederClamp cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanInitializeInputFeederClamp(CDT320_Machine machine, double targetValue, out string reason)
        {
            return CanMoveInputFeederClamp(machine, targetValue, out reason);
        }

        private static bool CanMoveInputFeederClamp(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            string direction = targetValue >= 0.5 ? "Fwd/Clamp" : "Bwd/Unclamp";

            if (machine.InputCassetteUnit != null &&
                machine.InputCassetteUnit.InputLifterZ != null &&
                machine.InputCassetteUnit.InputLifterZ.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederClamp",
                    "InputLifterZ is moving. InputFeederClamp move is blocked. direction=" + direction,
                    out reason);
            }

            if (machine.InputFeederUnit != null &&
                machine.InputFeederUnit.FeederY != null &&
                machine.InputFeederUnit.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederClamp",
                    "InputFeederY is moving. InputFeederClamp move is blocked. direction=" + direction,
                    out reason);
            }

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
            return waferZ != null && (IsAt(stage.ExpanderZ, waferZ.LoadPosition) || IsAt(stage.ExpanderZ, waferZ.UnloadPosition));
        }

        private static bool IsInputVisionXInAvoidPosition(InputStageUnit stage)
        {
            if (stage == null)
                return true;

            StageAxisPositions visionX = stage.Recipe != null ? stage.Recipe.VisionX : null;
            return visionX != null && IsAt(stage.CameraX, visionX.AvoidPosition);
        }

        private static bool IsFeederHoldingMaterial(InputFeederUnit feeder)
        {
            if (feeder == null)
                return false;

            if (feeder.IsWaferFeederTransferDataOccupied())
                return true;

            if (!feeder.IsWaferFeederSimulationOrDryRun() && feeder.IsWaferFeederRingCheck())
                return true;

            return false;
        }

        private static bool IsInputVisionXHomeReadyForInputFeederHome(InputStageUnit stage, out string reason)
        {
            reason = string.Empty;
            if (stage == null)
                return true;

            return IsAxisNotHomedOrAtHomePosition(stage.CameraX, "InputVisionX", out reason);
        }

        private static bool IsFrontPickerXHomeReadyForInputFeederHome(PickerFrontUnit picker, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            return IsAxisNotHomedOrAtHomePosition(picker.PickerX, "FrontPickerX", out reason);
        }

        private static bool IsRearPickerXHomeReadyForInputFeederHome(PickerRearUnit picker, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            return IsAxisNotHomedOrAtHomePosition(picker.PickerX, "RearPickerX", out reason);
        }

        private static bool IsAxisNotHomedOrAtHomePosition(BaseAxis axis, string axisName, out string reason)
        {
            return MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(axis, axisName, out reason);
        }

        private static bool VerifyInputFeederEmptyForHome(InputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            try
            {
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
                if (wafer != null)
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder material data exists. waferId=" +
                        wafer.WaferId + ", state=" + wafer.State,
                        out reason);
                }

                if (feeder != null &&
                    !feeder.IsWaferFeederSimulationOrDryRun() &&
                    feeder.IsWaferFeederRingCheck())
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder wafer detect sensor is ON while material data is empty.",
                        out reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "Exception occurred while checking InputFeeder material before home: " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        private static bool IsFrontPickerInAvoidPosition(PickerFrontUnit picker)
        {
            if (picker == null)
                return true;

            return picker.IsFrontPickerInAvoidPosition();
        }

        private static bool IsRearPickerInAvoidPosition(PickerRearUnit picker)
        {
            if (picker == null)
                return true;

            return picker.IsRearPickerInAvoidPosition();
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

        private static bool ShouldBypassHardwareMechanismChecks()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                return settings == null ||
                       settings.BypassHardware ||
                       settings.SimulationMode ||
                       !settings.UseAjin ||
                       !AjinFactory.IsRealBoardReady;
            }
            catch
            {
                return true;
            }
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
