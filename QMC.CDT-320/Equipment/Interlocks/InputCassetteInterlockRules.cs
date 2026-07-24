using QMC.Common;
using QMC.Common.IO;
using System;

namespace QMC.CDT320.Interlocks
{
    public static class InputCassetteInterlockRules
    {
        // 인터락 항목: InputLifterZ 이동 요청을 Input Cassette 리프터 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "InputLifterZ"))
                return VerifyWaferLifterZ(request.Machine, request.TargetValue, request.MoveKind, out reason);

            return true;
        }

        // 인터락 항목: InputLifterZ 이동 종류별로 수동/홈/자동 리프터 조건을 선택한다.
        public static bool VerifyWaferLifterZ(
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
                PickerFrontUnit frontPicker = machine.PickerFrontUnit;

                switch (moveKind)
                {
                    // 매뉴얼 이동 인터락 확인
                    case MotionGuardMoveKind.AxisMove:
                        if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                            return false;

                        return CanManualWaferLifterZ(Cassette, feeder, out reason);

                    // 홈 이동 인터락 확인
                    case MotionGuardMoveKind.AxisHome:
                        if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                            return false;

                        return CanHomeWaferLifterZ(Cassette, feeder, out reason);
                    
                    // 자동 이동 인터락 확인
                    case MotionGuardMoveKind.AxisTeachingMove:
                        if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                            return false;

                        if (feeder == null)
                            return true;

                        return CanAutoWaferLifterZ(Cassette, feeder, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(
                            new MotionGuardRuleContext("InputLifterZ", "InputLifterZ", targetPosition, moveKind, string.Empty, null, null),
                            out reason);
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

        // 인터락 조건: InputLifterZ 이동 전 FrontPickerX가 정확한 AvoidPosition인지 확인한다.
        private static bool VerifyFrontPickerXAvoidPosition(
            PickerFrontUnit frontPicker,
            out string reason)
        {
            reason = string.Empty;

            if (frontPicker == null ||
                frontPicker.PickerX == null ||
                frontPicker.Recipe == null ||
                frontPicker.Recipe.PickerX == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "FrontPickerX AvoidPosition을 확인할 수 없습니다. InputLifterZ 이동이 차단되었습니다.",
                    out reason);
            }

            double target = frontPicker.Recipe.PickerX.AvoidPosition;

            if (!frontPicker.IsFrontPickerAxisInTeachingPosition(
                PickerAxis.PickerX,
                "AvoidPosition"))
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "FrontPickerX가 AvoidPosition에 있어야 합니다. " +
                    "target=" + target.ToString("0.###") +
                    ", actual=" + frontPicker.PickerX.ActualPosition.ToString("0.###"),
                    out reason);
            }

            return true;
        }

        // 인터락 항목: 수동 InputLifterZ 이동은 카세트 돌출 감지와 InputFeederY 이동 중 여부를 확인한다.
        private static bool CanManualWaferLifterZ(InputCassetteUnit Cassette, InputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            // 인터락 조건: 카세트 돌출이 감지되면 리프터 수동 이동을 차단한다.
            if (Cassette != null && Cassette.IsWaferProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputCassette Jut detected. InputLifterZ home is blocked.",
                    out reason);
            }

            // 방어 조건: Feeder 참조가 없으면 Feeder 연동 조건은 적용하지 않는다.
            if (feeder == null)
                return true;

            // 인터락 조건: InputFeederY가 이동 중이면 리프터 수동 이동을 차단한다.
            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputFeederY is moving. InputLifterZ home is blocked.",
                    out reason);
            }

            //PickerFrontUnit pickerfront = machine.PickerFrontUnit;

            return true;
        }

        // 인터락 항목: InputLifterZ 홈은 카세트 돌출, FeederY 이동, 카세트 측 안전 위치를 확인한다.
        private static bool CanHomeWaferLifterZ(InputCassetteUnit Cassette, InputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            // 인터락 조건: 카세트 돌출이 감지되면 리프터 홈 이동을 차단한다.
            if (Cassette != null && Cassette.IsWaferProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputCassette Jut detected. InputLifterZ home is blocked.",
                    out reason);
            }

            // 방어 조건: Feeder 참조가 없으면 Feeder 연동 조건은 적용하지 않는다.
            if (feeder == null)
                return true;

            // 인터락 조건: InputFeederY가 이동 중이면 리프터 홈 이동을 차단한다.
            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputFeederY is moving. InputLifterZ home is blocked.",
                    out reason);
            }

            // 인터락 조건: 카세트가 장착되어 있으면 FeederY가 리프터 간섭 없는 안전 위치여야 한다.
            if (Cassette != null &&
                (Cassette.IsWaferCassetteExist(8) || Cassette.IsWaferCassetteExist(12)))
            {
                // 인터락 조건: FeederY가 카세트 측 안전 위치가 아니면 리프터 홈 이동을 차단한다.
                if (!IsWaferFeederYSafeForWaferLifterZ(feeder))
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "InputFeederY must be at a cassette-side safe teaching position before InputLifterZ move.",
                        out reason);
            }

            return true;
        }

        // 인터락 항목: 자동 InputLifterZ 이동은 카세트 돌출, FeederY 정지, FeederY 안전 위치를 확인한다.
        private static bool CanAutoWaferLifterZ(InputCassetteUnit Cassette, InputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;
            // 인터락 조건: 카세트 돌출이 감지되면 리프터 자동 이동을 차단한다.
            if (Cassette != null && Cassette.IsWaferProtrusionDetected())
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputCassette Jut detected. InputLifterZ move is blocked.",
                    out reason);
            }

            // 방어 조건: Feeder 참조가 없으면 Feeder 연동 조건은 적용하지 않는다.
            if (feeder == null)
                return true;

            // 인터락 조건: InputFeederY가 이동 중이면 리프터 자동 이동을 차단한다.
            if (feeder.FeederY != null && feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputFeederY is moving. InputLifterZ move is blocked.",
                    out reason);

            // 인터락 조건: FeederY가 카세트 측 안전 위치가 아니면 리프터 자동 이동을 차단한다.
            if (!IsWaferFeederYSafeForWaferLifterZ(feeder))
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputFeederY must be at a cassette-side safe teaching position before InputLifterZ move.",
                    out reason);

            return true;
        }

        // 인터락 기준: InputLifterZ 이동 전 InputFeederY가 카세트 측 안전 위치인지 판단한다.
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

        // 인터락 기준: InputFeeder Lift가 Down 상태인지 센서/실린더 상태로 판단한다.
        private static bool IsFeederDown(InputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsWaferFeederDown())
                return true;

            BaseCylinder cylinder = feeder.InputFeederLift;
            return cylinder != null && cylinder.IsBwd;
        }

        // 인터락 기준: InputFeeder Clamp가 Clamp 상태인지 센서/실린더 상태로 판단한다.
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
