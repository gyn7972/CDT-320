namespace QMC.CDT320.Interlocks
{
    public static class OutputCassetteInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "OutputLifterZ", "ElevatorZ_Output", "OutputLifterZ"))
                return VerifyBinLifterZ(request, out reason);

            return true;
        }

        private static bool VerifyBinLifterZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            OutputFeederUnit feeder = request.Machine.OutputFeederUnit;
            if (feeder == null)
                return true;

            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    request.MoveKind == MotionGuardMoveKind.AxisHome
                        ? "OutputFeederY is moving. OutputLifterZ home is blocked."
                        : "OutputFeederY is moving. OutputLifterZ move is blocked.",
                    out reason);

            if (request.MoveKind == MotionGuardMoveKind.AxisHome)
            {
                if (feeder.IsFeederOccupied() && !IsOutputFeederYSafeForOutputLifterZ(feeder))
                    return MotionGuardRuleHelpers.Block(
                        "OutputLifterZ",
                        "OutputFeederY must be at Avoid or Exchange position before OutputLifterZ home when feeder has a wafer.",
                        out reason);

                return true;
            }

            if (!IsOutputFeederYSafeForOutputLifterZ(feeder))
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY must be at Avoid or Exchange position before OutputLifterZ move.",
                    out reason);

            return true;
        }

        private static bool IsOutputFeederYSafeForOutputLifterZ(OutputFeederUnit feeder)
        {
            if (feeder == null || feeder.FeederY == null)
                return true;

            return feeder.IsBinFeederYInAvoidPosition()
                || feeder.IsBinFeederYInExchangePosition(BinSide.Good)
                || feeder.IsBinFeederYInExchangePosition(BinSide.Ng);
        }
    }
}
