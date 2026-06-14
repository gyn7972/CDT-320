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
                return VerifyBinLifterZ(request, out reason);

            return true;
        }

        private static bool VerifyBinLifterZ(MotionGuardRuleContext request, out string reason)
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
                        return CanMoveBinLifterZ(Cassette, feeder, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
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

            if (!IsOutputFeederYSafeForOutputLifterZ(feeder))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY must be at a cassette-safe position before OutputLifterZ home.",
                    out reason);
            }

            return true;
        }

        private static bool CanMoveBinLifterZ(OutputCassetteUnit cassette, OutputFeederUnit feeder, out string reason)
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

            if (!IsOutputFeederYSafeForOutputLifterZ(feeder))
            {
                return MotionGuardRuleHelpers.Block(
                    "OutputLifterZ",
                    "OutputFeederY must be at a cassette-safe position before OutputLifterZ move.",
                    out reason);
            }

            return true;
        }

        private static bool IsOutputFeederYSafeForOutputLifterZ(OutputFeederUnit feeder)
        {
            if (feeder == null || feeder.FeederY == null)
                return true;

            if (feeder.IsBinFeederYInAvoidPosition())
                return true;

            return IsOutputFeederYSafeForOutputLifterZ(feeder, BinSide.Good) ||
                   IsOutputFeederYSafeForOutputLifterZ(feeder, BinSide.Ng);
        }

        private static bool IsOutputFeederYSafeForOutputLifterZ(OutputFeederUnit feeder, BinSide side)
        {
            return feeder.IsBinFeederYInCassetteLoadPosition(side) ||
                   feeder.IsBinFeederYInCassetteUnloadPosition(side) ||
                   feeder.IsBinFeederYInStageLoadPosition(side) ||
                   feeder.IsBinFeederYInStageLoadAvoidPosition(side) ||
                   feeder.IsBinFeederYInStageUnloadPosition(side) ||
                   feeder.IsBinFeederYInStageUnloadAvoidPosition(side);
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
