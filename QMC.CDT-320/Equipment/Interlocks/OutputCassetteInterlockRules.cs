using QMC.Common;
using System;
using System.Threading.Tasks;

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
                return CanMoveBinLifterZ(request, out reason);

            return true;
        }

        private static bool CanMoveBinLifterZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                OutputCassetteUnit Cassette = request.Machine.OutputCassetteUnit;
                OutputFeederUnit feeder = request.Machine.OutputFeederUnit;

                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.AxisHome:
                        return CanHomeBinLifterZ(Cassette, feeder, out reason);
                    case MotionGuardMoveKind.AxisMove:
                    case MotionGuardMoveKind.AxisTeachingMove:
                        return CanMoveBinLifterZToPosition(Cassette, feeder, out reason);
                    default:
                        return true;
                }
            }
            catch(Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    $"Exception occurred while verifying OutputLifterZ motion guard rules: {ex.Message}",
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
            
        }

        private static bool CanHomeBinLifterZ(OutputCassetteUnit cassette, OutputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            if (cassette != null && cassette.IsBinProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputCassette bin protrusion detected. OutputLifterZ home is blocked.",
                    out reason);
            }

            if (feeder == null)
                return true;

            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY is moving. OutputLifterZ home is blocked.",
                    out reason);
            }

            if (feeder.IsFeederOccupied() && !IsOutputFeederYSafeForOutputLifterZ(feeder))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY must be at Avoid or Exchange position before OutputLifterZ home when feeder has a wafer.",
                    out reason);
            }

            return true;
        }

        private static bool CanMoveBinLifterZToPosition(OutputCassetteUnit cassette, OutputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            if (cassette != null && cassette.IsBinProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputCassette bin protrusion detected. OutputLifterZ move is blocked.",
                    out reason);
            }

            if (feeder == null)
                return true;

            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY is moving. OutputLifterZ move is blocked.",
                    out reason);
            }

            if (feeder.IsFeederOccupied() && !IsOutputFeederYSafeForOutputLifterZ(feeder))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY must be at Avoid or Exchange position before OutputLifterZ move when feeder has a wafer.",
                    out reason);
            }

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

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "OutputCassetteInterlock", reason + " - Blocked");
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
