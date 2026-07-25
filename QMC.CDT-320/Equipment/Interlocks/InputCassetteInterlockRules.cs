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
                PickerRearUnit rearPicker = machine.PickerRearUnit;

                switch (moveKind)
                {
                    // 매뉴얼 이동 인터락 확인
                    case MotionGuardMoveKind.AxisMove:
                        if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                            return false;

                        if (!VerifyRearPickerXAvoidPosition(rearPicker, out reason))
                            return false;

                        return CanManualWaferLifterZ(Cassette, feeder, out reason);

                    // 홈 이동 인터락 확인
                    case MotionGuardMoveKind.AxisHome:
                        //if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                        //    return false;

                        //if (!VerifyRearPickerXAvoidPosition(rearPicker, out reason))
                        //    return false;

                        return CanHomeWaferLifterZ(Cassette, feeder, out reason);

                    // 자동 이동 인터락 확인
                    case MotionGuardMoveKind.AxisTeachingMove:
                        if (!VerifyFrontPickerXAvoidPosition(frontPicker, out reason))
                            return false;

                        if (!VerifyRearPickerXAvoidPosition(rearPicker, out reason))
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

        // 인터락 조건: InputLifterZ 이동 전 RearPickerX가 정확한 AvoidPosition인지 확인한다.
        private static bool VerifyRearPickerXAvoidPosition(
            PickerRearUnit rearPicker,
            out string reason)
        {
            reason = string.Empty;

            if (rearPicker == null ||
                rearPicker.PickerX == null ||
                rearPicker.Recipe == null ||
                rearPicker.Recipe.PickerX == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "RearPickerX AvoidPosition을 확인할 수 없습니다. InputLifterZ 이동이 차단되었습니다.",
                    out reason);
            }

            double target = rearPicker.Recipe.PickerX.AvoidPosition;

            if (!rearPicker.IsRearPickerAxisInTeachingPosition(
                PickerAxis.PickerX,
                "AvoidPosition"))
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "RearPickerX가 AvoidPosition에 있어야 합니다. " +
                    "target=" + target.ToString("0.###") +
                    ", actual=" + rearPicker.PickerX.ActualPosition.ToString("0.###"),
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

            // 인터락 조건: 카세트가 장착되어 있으면 홈/자동 이동과 동일하게
            //             FeederY가 카세트 측 안전 위치가 아니면 수동 이동도 차단한다.
            //             (기존에는 수동 이동에 이 검사가 없어 FeederY 간섭 위치에서도 이동이 허용되었다.)
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
        // 기존 조건: 강제 true(검사 무력화, Todo 잔존) — FeederY가 카세트 간섭 위치에 있어도 리프터 이동이 허용되었다.
        // 현재 기준: 원래 의도된 티칭 위치 predicate(Avoid/Exchange/Home)를 복원한다.
        //           기계 전제: FeederY가 Avoid/Exchange/Home 티칭 위치에 있으면 카세트 슬롯 진입 경로와 간섭하지 않는다.
        //           Home 위치의 실기 안전성은 실장비 저속 검증 항목으로 유지한다.
        private static bool IsWaferFeederYSafeForWaferLifterZ(InputFeederUnit feeder)
        {
            if (feeder == null || feeder.FeederY == null)
            {
                return true;
            }

            return feeder.IsWaferFeederYInAvoidPosition()
                || feeder.IsWaferFeederYInExchangePosition()
                || feeder.IsWaferFeederYInHomePosition();
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
