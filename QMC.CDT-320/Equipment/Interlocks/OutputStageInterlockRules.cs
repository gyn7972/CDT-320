using QMC.Common.Motion;

using QMC.Common.IO;
using QMC.CDT320.Materials;

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

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveOutputGoodStageY(request.Machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputGoodStageY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveOutputGoodStageY(CDT320_Machine machine, out string reason)
        {
            if (!CanHomeOutputGoodStageY(machine, out reason))
                return false;

            OutputStageUnit stage = machine != null ? machine.OutputStageUnit : null;
            if (!VerifyOutputTransportClear(machine, "OutputGoodStageY", out reason))
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

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveOutputGoodStageZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputGoodStageZ(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeOutputGoodStageZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (!VerifyNgClampLiftUpForGoodStageMove(machine != null ? machine.OutputStageUnit : null, "OutputGoodStageZ", out reason))
                return false;

            return true;
        }

        private static bool CanMoveOutputGoodStageZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!CanHomeOutputGoodStageZ(machine, out reason))
                return false;

            if (!VerifyOutputTransportClear(machine, "OutputGoodStageZ", out reason))
                return false;

            OutputStageUnit stage = machine != null ? machine.OutputStageUnit : null;
            if (IsGoodStageZLoadOrUnloadTarget(stage, request != null ? request.TargetValue : 0.0) &&
                stage != null &&
                !stage.IsNgStageInAvoidPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputGoodStageZ",
                    "GoodStageZ load/unload move blocked. NgStage must be at Avoid position.",
                    out reason);
            }

            return VerifyOutputStageNotBusy(machine != null ? machine.OutputStageUnit : null, "OutputGoodStageZ", out reason);
        }

        private static bool VerifyBinNgY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveOutputNgStageY(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputNgStageY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveOutputNgStageY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            OutputStageUnit stage = machine != null ? machine.OutputStageUnit : null;
            if (!VerifyOutputNgStageYMechanicalClear(request, "OutputNGStageY", out reason))
                return false;

            if (!VerifyOutputTransportClear(machine, "OutputNGStageY", out reason))
                return false;

            if (stage != null &&
                stage.GoodStage != null &&
                !stage.GoodStage.IsAtAvoidPosition())
                return MotionGuardRuleHelpers.Block(
                    "OutputNGStageY",
                    "OutputNGStageY move blocked. GoodStageZ must be at Avoid position.",
                    out reason);

            return VerifyOutputStageNotBusy(stage, "OutputNGStageY", out reason);
        }

        private static bool VerifyBinVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveOutputVisionX(request.Machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeOutputVisionX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveOutputVisionX(CDT320_Machine machine, out string reason)
        {
            if (!CanHomeOutputVisionX(machine, out reason))
                return false;

            if (!VerifyOutputTransportClear(machine, "OutputVisionX", out reason))
                return false;

            return VerifyOutputStageNotBusy(machine != null ? machine.OutputStageUnit : null, "OutputVisionX", out reason);
        }

        private static bool CanHomeOutputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
                if (front != null && !IsPickerXHomeDoneOrInputAvoid(
                        front.PickerX,
                        delegate { return front.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "InputAvoidPosition"); },
                        "FrontPickerX",
                        out reason))
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX HOME blocked. FrontPickerX must be HomeDone or InputAvoid. " + reason,
                        out reason);
                }

                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
                if (rear != null && !IsPickerXHomeDoneOrInputAvoid(
                        rear.PickerX,
                        delegate { return rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerX, "InputAvoidPosition"); },
                        "RearPickerX",
                        out reason))
                {
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX HOME blocked. RearPickerX must be HomeDone or InputAvoid. " + reason,
                        out reason);
                }

                OutputFeederUnit outputFeeder = machine != null ? machine.OutputFeederUnit : null;
                if (outputFeeder != null && !outputFeeder.IsBinFeederYInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX HOME blocked. OutputFeederY must be at Avoid position.",
                        out reason);

                if (outputFeeder != null && !outputFeeder.IsFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "OutputVisionX",
                        "OutputVisionX HOME blocked. OutputFeeder lift cylinder must be down.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputVisionX",
                    "Exception occurred while verifying OutputVisionX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanHomeOutputGoodStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                if (outputStage != null && outputStage.GoodStage != null && !outputStage.GoodStage.IsAtAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "OutputGoodStageY",
                        "OutputGoodStageY HOME blocked. OutputGoodStageZ must be at Avoid position.",
                        out reason);

                if (!VerifyNgClampLiftUpForGoodStageMove(outputStage, "OutputGoodStageY", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputGoodStageY",
                    "Exception occurred while verifying OutputGoodStageY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanHomeOutputNgStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                if (outputStage == null)
                    return true;

                if (outputStage.GoodStage != null && !outputStage.GoodStage.IsAtAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY HOME blocked. GoodBinZ(GoodStageZ) must be at Avoid position.",
                        out reason);

                if (outputStage.GoodBinGuideDownSensor != null &&
                    !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                    !outputStage.GoodBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY HOME blocked. Good Bin Guide cylinder must be down.",
                        out reason);

                if (outputStage.NgBinGuideDownSensor != null &&
                    !IsDryRunInput(outputStage.NgBinGuideDownSensor) &&
                    !outputStage.NgBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY HOME blocked. NG Bin Guide cylinder must be down.",
                        out reason);

                if (outputStage.NgBinClampUpSensor != null &&
                    !IsDryRunInput(outputStage.NgBinClampUpSensor) &&
                    !outputStage.NgBinClampUpSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY HOME blocked. NG Bin Clamp cylinder must be up.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputNGStageY",
                    "Exception occurred while verifying OutputNGStageY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
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

        private static bool VerifyNgClampLiftUpForGoodStageMove(OutputStageUnit outputStage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (outputStage == null)
                return true;

            if (!outputStage.IsBinGuideClampLiftUp(BinSide.Ng))
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " move blocked. NG Bin Clamp Lift must be up before GoodStage movement.",
                    out reason);

            return true;
        }

        private static bool VerifyOutputNgStageYMechanicalClear(MotionGuardRuleContext request, string movingName, out string reason)
        {
            reason = string.Empty;
            OutputStageUnit outputStage = request != null && request.Machine != null ? request.Machine.OutputStageUnit : null;
            if (outputStage == null)
                return true;

            bool ngAvoidTarget = IsNgStageYAvoidTarget(outputStage, request != null ? request.TargetValue : 0.0);

            if (outputStage.GoodBinGuideDownSensor != null &&
                !IsDryRunInput(outputStage.GoodBinGuideDownSensor) &&
                !outputStage.GoodBinGuideDownSensor.IsOn)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " move blocked. Good Bin Guide cylinder must be down.",
                    out reason);

            if (outputStage.NgBinGuideDownSensor != null &&
                !ngAvoidTarget &&
                !IsDryRunInput(outputStage.NgBinGuideDownSensor) &&
                !outputStage.NgBinGuideDownSensor.IsOn)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " move blocked. NG Bin Guide cylinder must be down.",
                    out reason);

            if (outputStage.NgBinClampUpSensor != null &&
                !IsDryRunInput(outputStage.NgBinClampUpSensor) &&
                !outputStage.NgBinClampUpSensor.IsOn)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " move blocked. NG Bin Clamp cylinder must be up.",
                    out reason);

            return true;
        }

        private static bool IsNgStageYAvoidTarget(OutputStageUnit outputStage, double target)
        {
            if (outputStage == null || outputStage.Recipe == null || outputStage.Recipe.NGStageY == null)
                return false;

            return System.Math.Abs(target - outputStage.Recipe.NGStageY.AvoidPosition) <= 0.001;
        }

        private static bool IsGoodStageZLoadOrUnloadTarget(OutputStageUnit stage, double target)
        {
            if (stage == null || stage.Recipe == null || stage.GoodStage == null || stage.GoodStage.StageZ == null)
                return false;

            return IsTargetPosition(stage.GoodStage.StageZ, target, stage.Recipe.GoodStageZ.LoadPosition) ||
                   IsTargetPosition(stage.GoodStage.StageZ, target, stage.Recipe.GoodStageZ.UnloadPosition);
        }

        private static bool IsTargetPosition(BaseAxis axis, double target, double position)
        {
            double tolerance = axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;

            return System.Math.Abs(target - position) <= tolerance;
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

        private static bool IsDryRunInput(BaseDigitalInput input)
        {
            return input != null && input.Config != null && input.Config.IgnoreWaits;
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

        private static bool IsPickerXHomeDoneOrInputAvoid(
            BaseAxis axis,
            System.Func<bool> isInputAvoid,
            string axisName,
            out string reason)
        {
            reason = string.Empty;

            if (axis == null)
            {
                reason = axisName + " axis is null.";
                return false;
            }

            bool homeDone = axis.IsHomeDone;
            bool inputAvoid = false;
            string inputAvoidError = string.Empty;

            try
            {
                inputAvoid = isInputAvoid != null && isInputAvoid();
            }
            catch (System.Exception ex)
            {
                inputAvoidError = ex.Message;
            }
            finally
            {
            }

            if (homeDone || inputAvoid)
                return true;

            reason = "homeDone=" + homeDone +
                     ", inputAvoid=" + inputAvoid +
                     ", actual=" + axis.ActualPosition;

            if (!string.IsNullOrEmpty(inputAvoidError))
                reason += ", inputAvoidCheckError=" + inputAvoidError;

            return false;
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "OutputStageInterlock", reason + " - Blocked");
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
