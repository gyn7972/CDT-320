using QMC.Common.IO;
using System;

namespace QMC.CDT320.Interlocks
{
    public static class InputCassetteInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "InputLifterZ"))
                return CanMoveWaferLifterZ(request.Machine, request.TargetValue, request.MoveKind, out reason);

            return true;
        }

        public static bool CanMoveWaferLifterZ(
            CDT320_Machine machine,
            double targetPosition,
            MotionGuardMoveKind moveKind,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                if (machine == null)
                    return true;

                InputCassetteUnit Cassette = machine.InputCassetteUnit;
                InputFeederUnit feeder = machine.InputFeederUnit;

                switch (moveKind)
                {
                    case MotionGuardMoveKind.AxisHome:
                        return CanHomeWaferLifterZ(Cassette, feeder, out reason);
                    case MotionGuardMoveKind.AxisMove:
                    case MotionGuardMoveKind.AxisTeachingMove:
                        if (feeder == null)
                            return true;

                        return CanMoveWaferLifterZToPosition(feeder, out reason);
                    default:
                        return true;
                }
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    $"Exception occurred while evaluating InputLifterZ motion guard rules: {ex.Message}",
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }


        }

        private static bool CanHomeWaferLifterZ(InputCassetteUnit Cassette, InputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            if (Cassette != null && Cassette.IsWaferProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputCassette Jut detected. InputLifterZ home is blocked.",
                    out reason);
            }

            if (feeder == null)
                return true;

            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputFeederY is moving. InputLifterZ home is blocked.",
                    out reason);
            }

            if (Cassette != null &&
                (Cassette.IsWaferCassetteExist(8) || Cassette.IsWaferCassetteExist(12)))
            {
                if (!IsWaferFeederYSafeForWaferLifterZ(feeder))
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "InputFeederY must be at a cassette-side safe teaching position before InputLifterZ move.",
                        out reason);
            }

            return true;
        }

        private static bool CanMoveWaferLifterZToPosition(InputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;
            if (feeder == null)
                return true;

            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputFeederY is moving. InputLifterZ move is blocked.",
                    out reason);

            if (!IsWaferFeederYSafeForWaferLifterZ(feeder))
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputFeederY must be at a cassette-side safe teaching position before InputLifterZ move.",
                    out reason);

            return true;
        }

        private static bool IsWaferFeederYSafeForWaferLifterZ(InputFeederUnit feeder)
        {
            if (feeder == null || feeder.FeederY == null)
            {
                return true;
            }

            // Todo : 추후 FeederY 상태를 재확인 후 조건 수정할 것. 강제 true 리턴처리함.
            //return feeder.IsWaferFeederYInAvoidPosition()
            //    || feeder.IsWaferFeederYInExchangePosition()
            //    || feeder.IsWaferFeederYInHomePosition(); // 실제로 초기화 위치가 안전한지 실장비에서 확인 필요.
            return true;
        }

        private static bool IsFeederDown(InputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsWaferFeederDown())
                return true;

            BaseCylinder cylinder = feeder.InputFeederLift;
            return cylinder != null && cylinder.IsBwd;
        }

        private static bool IsFeederClamp(InputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsWaferFeederClamp())
                return true;

            BaseCylinder cylinder = feeder.InputFeederClamp;
            return cylinder != null && cylinder.IsFwd;
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "InputCassetteInterlock", reason + " - Blocked");
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
