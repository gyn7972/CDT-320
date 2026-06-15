using QMC.Common.IO;

using System;

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
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanMoveOutputFeederY(request, out reason);
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveOutputFeederY(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputFeederY(machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveOutputFeederY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (machine == null)
                return true;

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputLifterZ is moving. OutputFeederY move is blocked.",
                    out reason);

            OutputStageUnit outputStage = machine.OutputStageUnit;
            if (!IsOutputVisionXInAvoidPosition(outputStage))
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeederY move blocked. OutputVisionX must be at Avoid position.",
                    out reason);

            if (!IsFrontPickerInAvoidPosition(machine.PickerFrontUnit))
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeederY move blocked. FrontPicker must be at Avoid position.",
                    out reason);

            if (!IsRearPickerInAvoidPosition(machine.PickerRearUnit))
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeederY move blocked. RearPicker must be at Avoid position.",
                    out reason);

            OutputFeederUnit feeder = machine.OutputFeederUnit;
            if (feeder == null)
                return true;

            if (feeder.IsFeederOverload())
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederY",
                    "OutputFeederY move blocked. OutputFeeder overload sensor is detected.",
                    out reason);

            return true;
        }

        private static bool CanHomeOutputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (machine == null)
                    return true;

                // 홈 잡을때 picker 위치 봐야 하는데 이거 잡기전에 이미 이 둘은 잡혀 있어야함.
                // 근데.. 홈이 잡혔는지를 확인하고.. output쪽에 위치 하는지를 봐야함.
                //if (!IsFrontPickerInAvoidPosition(machine.PickerFrontUnit))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. FrontPicker must be at Avoid position.",
                //        out reason);

                //if (!IsRearPickerInAvoidPosition(machine.PickerRearUnit))
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. RearPicker must be at Avoid position.",
                //        out reason);

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

                // 근데.. 홈이 잡혔는지를 확인하고.. output쪽에 위치 하는지를 봐야함.
                //if (outputStage != null && !outputStage.IsVisionXInAvoidPosition())
                //    return MotionGuardRuleHelpers.Block(
                //        "OutputFeederY",
                //        "OutputFeederY HOME blocked. OutputVisionX must be at Avoid position.",
                //        out reason);

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

            try
            {
                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeOutputFeederLift(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveOutputFeederLift(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "Exception occurred while verifying OutputFeederLift cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanInitializeOutputFeederLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Up", "Bwd/Down");

            if (machine == null)
                return true;

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift initialize " + direction + " blocked. OutputLifterZ is moving.",
                    out reason);

            if (machine.OutputFeederUnit != null &&
                machine.OutputFeederUnit.FeederY != null &&
                machine.OutputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift initialize " + direction + " blocked. OutputFeederY is moving.",
                    out reason);

            return true;
        }

        private static bool CanMoveOutputFeederLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Up", "Bwd/Down");

            if (machine == null)
                return true;

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift move " + direction + " blocked. OutputLifterZ is moving.",
                    out reason);

            if (machine.OutputFeederUnit != null &&
                machine.OutputFeederUnit.FeederY != null &&
                machine.OutputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift move " + direction + " blocked. OutputFeederY is moving.",
                    out reason);

            if (machine.OutputFeederUnit != null &&
                IsFeederUnclamp(machine.OutputFeederUnit) &&
                !machine.OutputFeederUnit.IsFeederTransferDataEmpty())
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederLift",
                    "OutputFeederLift move blocked. OutputFeeder is unclamped, so feeder is assumed to be holding material.",
                    out reason);

            return true;
        }

        private static bool VerifyOutputFeederClamp(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeOutputFeederClamp(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveOutputFeederClamp(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederClamp",
                    "Exception occurred while verifying OutputFeederClamp cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanInitializeOutputFeederClamp(CDT320_Machine machine, double targetValue, out string reason)
        {
            return CanMoveOutputFeederClamp(machine, targetValue, out reason);
        }

        private static bool CanMoveOutputFeederClamp(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            string direction = ResolveCylinderDirection(targetValue, "Fwd/Clamp", "Bwd/Unclamp");

            if (machine == null)
                return true;

            if (machine.OutputCassetteUnit != null &&
                machine.OutputCassetteUnit.OutputLifterZ != null &&
                machine.OutputCassetteUnit.OutputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederClamp",
                    "OutputFeederClamp move " + direction + " blocked. OutputLifterZ is moving.",
                    out reason);

            if (machine.OutputFeederUnit != null &&
                machine.OutputFeederUnit.FeederY != null &&
                machine.OutputFeederUnit.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputFeederClamp",
                    "OutputFeederClamp move " + direction + " blocked. OutputFeederY is moving.",
                    out reason);

            return true;
        }

        private static string ResolveCylinderDirection(double targetValue, string fwdText, string bwdText)
        {
            return targetValue >= 0.5 ? fwdText : bwdText;
        }

        private static bool IsOutputVisionXInAvoidPosition(OutputStageUnit stage)
        {
            if (stage == null)
                return true;

            return MotionGuardRuleHelpers.IsAt(stage.OutputCameraX, stage.Recipe.VisionX.AvoidPosition);
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
