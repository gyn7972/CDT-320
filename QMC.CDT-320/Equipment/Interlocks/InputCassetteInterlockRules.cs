using System;
using QMC.Common.IO;

namespace QMC.CDT320.Interlocks
{
    public static class InputCassetteInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferLifterZ"))
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
            if (machine == null)
                return true;

            InputFeederUnit feeder = machine.InputFeeder;

            if (feeder == null)
                return true;

            if (!IsWaferFeederYSafeForWaferLifterZ(feeder))
                return MotionGuardRuleHelpers.Block(
                    "WaferLifterZ",
                    "WaferFeederY must be at a cassette-side safe teaching position before WaferLifterZ move/home.",
                    out reason);

            // 아래 실린더 조건 필요없음.
            //if (!IsFeederDown(feeder))
            //    return Block("WaferLifterZ", "InputFeederLift must be Down before WaferLifterZ move/home.", out reason);
            //if (!IsFeederClamp(feeder))
            //    return Block("WaferLifterZ", "InputFeederClamp must be Clamp before WaferLifterZ move/home.", out reason);

            return true;
        }

        private static bool IsWaferFeederYSafeForWaferLifterZ(InputFeederUnit feeder)
        {
            if (feeder == null || feeder.FeederY == null)
                return true;

            return feeder.IsWaferFeederYInAvoidPosition()
                || feeder.IsWaferFeederYInExchangePosition();
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

    }
}
