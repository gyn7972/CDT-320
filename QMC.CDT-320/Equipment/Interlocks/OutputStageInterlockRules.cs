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

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyOutputGoodStageYHome(request.Machine, out reason);
                default:
                    return true;
            }

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

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return true;
                default:
                    return true;
            }

            if (!VerifyOutputTransportClear(request.Machine, "OutputGoodStageZ", out reason))
                return false;

            return VerifyOutputStageNotBusy(request.Machine.OutputStageUnit, "OutputGoodStageZ", out reason);
        }

        private static bool VerifyBinNgY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyOutputNgStageYHome(request.Machine, out reason);
                default:
                    return true;
            }

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

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyOutputVisionXHome(request.Machine, out reason);
                default:
                    return true;
            }

            if (!VerifyOutputTransportClear(request.Machine, "OutputVisionX", out reason))
                return false;

            return VerifyOutputStageNotBusy(request.Machine.OutputStageUnit, "OutputVisionX", out reason);
        }

        private static bool VerifyOutputVisionXHome(CDT320_Machine machine, out string reason)
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

        private static bool VerifyOutputGoodStageYHome(CDT320_Machine machine, out string reason)
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

        private static bool VerifyOutputNgStageYHome(CDT320_Machine machine, out string reason)
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

                if (outputStage.GoodBinGuideDownSensor != null && !outputStage.GoodBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY HOME blocked. Good Bin Guide cylinder must be down.",
                        out reason);

                if (outputStage.NgBinGuideDownSensor != null && !outputStage.NgBinGuideDownSensor.IsOn)
                    return MotionGuardRuleHelpers.Block(
                        "OutputNGStageY",
                        "OutputNGStageY HOME blocked. NG Bin Guide cylinder must be down.",
                        out reason);

                if (outputStage.NgBinClampUpSensor != null && !outputStage.NgBinClampUpSensor.IsOn)
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
