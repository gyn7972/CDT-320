using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public static class InputStageInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferStageY", "StageY", "WaferY"))
                return VerifyWaferStageY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferStageT", "StageT", "WaferT"))
                return VerifyWaferStageT(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferExpandingZ", "ExpanderZ"))
                return VerifyWaferExpandingZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "InputVisionX", "CameraX"))
                return VerifyWaferVisionX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NeedleX", "NeedleBlockX"))
                return VerifyNeedleX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NeedleZ"))
                return VerifyNeedleZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "EjectPinZ"))
                return VerifyEjectPinZ(request, out reason);

            return true;
        }

        private static bool VerifyWaferStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (!VerifyInputFeederClear(request.Machine, "WaferStageY", out reason))
                return false;

            return VerifyInputStageNotBusy(request.Machine.InputStageUnit, "WaferStageY", out reason);
        }

        private static bool VerifyWaferStageT(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (!VerifyInputFeederClear(request.Machine, "WaferStageT", out reason))
                return false;

            return VerifyInputStageNotBusy(request.Machine.InputStageUnit, "WaferStageT", out reason);
        }

        private static bool VerifyWaferExpandingZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (!VerifyInputFeederClear(request.Machine, "ExpanderZ", out reason))
                return false;

            return VerifyInputStageNotBusy(request.Machine.InputStageUnit, "ExpanderZ", out reason);
        }

        private static bool VerifyWaferVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (!VerifyInputFeederClear(request.Machine, "InputVisionX", out reason))
                return false;

            return VerifyInputStageNotBusy(request.Machine.InputStageUnit, "InputVisionX", out reason);
        }

        private static bool VerifyNeedleX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (!VerifyInputFeederClear(request.Machine, "NeedleX", out reason))
                return false;

            return VerifyInputStageNotBusy(request.Machine.InputStageUnit, "NeedleX", out reason);
        }

        private static bool VerifyNeedleZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (!VerifyInputFeederClear(request.Machine, "NeedleZ", out reason))
                return false;

            return VerifyInputStageNotBusy(request.Machine.InputStageUnit, "NeedleZ", out reason);
        }

        private static bool VerifyEjectPinZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (!VerifyInputFeederClear(request.Machine, "EjectPinZ", out reason))
                return false;

            return VerifyInputStageNotBusy(request.Machine.InputStageUnit, "EjectPinZ", out reason);
        }

        private static bool VerifyInputFeederClear(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
            if (feeder == null)
                return true;

            if (MotionGuardRuleHelpers.IsAxisMoving(feeder.FeederY))
                return MotionGuardRuleHelpers.Block(movingName, "InputFeederY is moving.", out reason);

            if (!feeder.IsWaferFeederYInAvoidPosition() &&
                !feeder.IsWaferFeederYInExchangePosition() &&
                !feeder.IsWaferFeederYInHomePosition())
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "InputFeederY must be at Avoid, Exchange, or Home position.",
                    out reason);

            return true;
        }

        private static bool VerifyInputStageNotBusy(InputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (stage == null)
                return true;

            if (IsMovingExcept(stage.StageY, movingName, "WaferStageY", "StageY", "WaferY"))
                return MotionGuardRuleHelpers.Block(movingName, "WaferStageY is moving.", out reason);
            if (IsMovingExcept(stage.StageT, movingName, "WaferStageT", "StageT", "WaferT"))
                return MotionGuardRuleHelpers.Block(movingName, "WaferStageT is moving.", out reason);
            if (IsMovingExcept(stage.ExpanderZ, movingName, "WaferExpandingZ", "ExpanderZ"))
                return MotionGuardRuleHelpers.Block(movingName, "ExpanderZ is moving.", out reason);
            if (IsMovingExcept(stage.CameraX, movingName, "InputVisionX", "CameraX"))
                return MotionGuardRuleHelpers.Block(movingName, "InputVisionX is moving.", out reason);
            if (IsMovingExcept(stage.NeedleBlockX, movingName, "NeedleX", "NeedleBlockX"))
                return MotionGuardRuleHelpers.Block(movingName, "NeedleX is moving.", out reason);
            if (IsMovingExcept(stage.NeedleZ, movingName, "NeedleZ", "EjectPinZ"))
                return MotionGuardRuleHelpers.Block(movingName, "NeedleZ is moving.", out reason);
            if (IsMovingExcept(stage.EjectPinZ, movingName, "EjectPinZ", "NeedleZ"))
                return MotionGuardRuleHelpers.Block(movingName, "EjectPinZ is moving.", out reason);

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
