using QMC.Common;
using QMC.Common.IO;
using QMC.CDT320.Materials;
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
            {
                if (request.IsSequenceProcess &&
                    request.MoveKind == MotionGuardMoveKind.AxisTeachingMove &&
                    string.Equals(
                        request.TargetName,
                        InputCassetteUnit.UnloadReleaseLiftTargetName,
                        StringComparison.Ordinal))
                {
                    return VerifyUnloadReleaseLift(
                        request.Machine,
                        request.TargetValue,
                        out reason);
                }

                return VerifyWaferLifterZ(request.Machine, request.TargetValue, request.MoveKind, out reason);
            }

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

        internal static bool VerifyUnloadReleaseLift(
            CDT320_Machine machine,
            double targetPosition,
            out string reason)
        {
            reason = string.Empty;

            try
            {
                if (machine == null ||
                    machine.InputCassetteUnit == null ||
                    machine.InputCassetteUnit.InputLifterZ == null ||
                    machine.InputCassetteUnit.Config == null ||
                    machine.InputFeederUnit == null ||
                    machine.InputFeederUnit.FeederY == null)
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "Unload release lift 상태를 확인할 수 없습니다.",
                        out reason);
                }

                InputCassetteUnit cassette = machine.InputCassetteUnit;
                InputFeederUnit feeder = machine.InputFeederUnit;

                if (!VerifyFrontPickerXAvoidPosition(machine.PickerFrontUnit, out reason))
                    return false;

                if (!VerifyRearPickerXAvoidPosition(machine.PickerRearUnit, out reason))
                    return false;

                // 언클램프 직후 Jut ON은 제품이 카세트에 걸쳐 있는 이 전용 release lift에서만 정상이다.
                // 일반 Manual/Home/Teaching 이동은 아래 기존 Jut 인터락을 그대로 사용한다.
                if (!cassette.InputLifterZ.IsServoOn ||
                    cassette.InputLifterZ.IsAlarm ||
                    cassette.InputLifterZ.IsMoving)
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "InputLifterZ가 unload release lift 준비 상태가 아닙니다. " +
                        "servo=" + cassette.InputLifterZ.IsServoOn +
                        ", alarm=" + cassette.InputLifterZ.IsAlarm +
                        ", moving=" + cassette.InputLifterZ.IsMoving,
                        out reason);
                }

                if (!feeder.FeederY.IsServoOn ||
                    feeder.FeederY.IsAlarm ||
                    feeder.FeederY.IsMoving ||
                    !feeder.IsWaferFeederYInCassetteUnloadPosition())
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "InputFeederY가 정지된 CassetteUnloadPosition이어야 합니다. " +
                        "servo=" + feeder.FeederY.IsServoOn +
                        ", alarm=" + feeder.FeederY.IsAlarm +
                        ", moving=" + feeder.FeederY.IsMoving +
                        ", actual=" + feeder.FeederY.ActualPosition.ToString("0.###"),
                        out reason);
                }

                if (!feeder.IsWaferFeederDown() ||
                    feeder.IsWaferFeederUp() ||
                    !feeder.IsWaferFeederUnclamp() ||
                    feeder.IsWaferFeederClamp())
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "Unload release lift 전 피더는 Down/Unclamp 상태여야 합니다. " +
                        feeder.GetWaferFeederTransferState(),
                        out reason);
                }

                if (!feeder.HasWaferOnFeeder())
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "Unload release lift 전 InputFeeder wafer 데이터 또는 Ring 감지가 없습니다. " +
                        feeder.GetWaferFeederTransferState(),
                        out reason);
                }

                double unloadOffset = cassette.Config.UnloadingPositionOffset;
                double releaseDistance = cassette.Config.UnloadReleaseLiftDistance;
                double tolerance = cassette.ResolveWaferLifterZInPositionTolerance();
                WaferMaterial wafer =
                    feeder.CurrentWaferMaterial ??
                    MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
                double moveDistance = targetPosition - cassette.InputLifterZ.ActualPosition;
                bool invalidConfig =
                    double.IsNaN(unloadOffset) ||
                    double.IsInfinity(unloadOffset) ||
                    double.IsNaN(releaseDistance) ||
                    double.IsInfinity(releaseDistance) ||
                    unloadOffset >= 0.0 ||
                    releaseDistance < InputCassetteUnit.MinUnloadReleaseLiftDistanceMm ||
                    releaseDistance > InputCassetteUnit.MaxUnloadReleaseLiftDistanceMm ||
                    releaseDistance > Math.Abs(unloadOffset);
                if (invalidConfig)
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "Unload release lift 설정이 안전 범위를 벗어났습니다. " +
                        "unloadOffset=" + unloadOffset.ToString("0.###") +
                        ", releaseDistance=" + releaseDistance.ToString("0.###") +
                        ", minReleaseDistance=" +
                        InputCassetteUnit.MinUnloadReleaseLiftDistanceMm.ToString("0.###") +
                        ", maxReleaseDistance=" +
                        InputCassetteUnit.MaxUnloadReleaseLiftDistanceMm.ToString("0.###"),
                        out reason);
                }

                if (wafer == null ||
                    wafer.SourceSlotNumber < 0 ||
                    (wafer.SourceCassetteRole != CassetteMaterialRole.Input1 &&
                     wafer.SourceCassetteRole != CassetteMaterialRole.Input2))
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "Unload release lift 대상 wafer의 원본 cassette/slot을 확인할 수 없습니다.",
                        out reason);
                }

                int level = InputCassetteUnit.ResolveCassetteLevel(wafer.SourceCassetteRole);
                double unloadTarget =
                    cassette.CalculateWaferCassetteSlotTargetPosition(
                        wafer.SourceSlotNumber,
                        level) +
                    unloadOffset;
                double releaseTarget = unloadTarget + releaseDistance;
                double actual = cassette.InputLifterZ.ActualPosition;
                bool actualAtUnload =
                    InputCassetteUnit.IsUnloadReleasePositionMatch(
                        actual,
                        unloadTarget,
                        releaseTarget,
                        tolerance);
                bool commandAtUnload =
                    InputCassetteUnit.IsUnloadReleasePositionMatch(
                        cassette.InputLifterZ.CommandPosition,
                        unloadTarget,
                        releaseTarget,
                        tolerance);
                bool targetMatches =
                    InputCassetteUnit.IsUnloadReleasePositionMatch(
                        targetPosition,
                        releaseTarget,
                        unloadTarget,
                        tolerance);
                bool moveDistanceMatches =
                    InputCassetteUnit.IsUnloadReleasePositionMatch(
                        moveDistance,
                        releaseDistance,
                        0.0,
                        tolerance);
                if (!cassette.InputLifterZ.IsInPosition ||
                    !actualAtUnload ||
                    !commandAtUnload ||
                    !targetMatches ||
                    !moveDistanceMatches)
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "Unload release lift 거리 조건이 맞지 않습니다. " +
                        "unloadOffset=" + unloadOffset.ToString("0.###") +
                        ", releaseDistance=" + releaseDistance.ToString("0.###") +
                        ", unloadTarget=" + unloadTarget.ToString("0.###") +
                        ", releaseTarget=" + releaseTarget.ToString("0.###") +
                        ", actual=" + actual.ToString("0.###") +
                        ", command=" + cassette.InputLifterZ.CommandPosition.ToString("0.###") +
                        ", target=" + targetPosition.ToString("0.###") +
                        ", moveDistance=" + moveDistance.ToString("0.###") +
                        ", inPosition=" + cassette.InputLifterZ.IsInPosition +
                        ", source=" + wafer.SourceCassetteRole +
                        "/" + (wafer.SourceSlotNumber + 1),
                        out reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "Unload release lift 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
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
                string feederDetail;
                if (!IsWaferFeederYSafeForWaferLifterZ(feeder, out feederDetail))
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "InputFeederY must be at a cassette-side safe teaching position before InputLifterZ move. " +
                        feederDetail,
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
                string feederDetail;
                if (!IsWaferFeederYSafeForWaferLifterZ(feeder, out feederDetail))
                    return MotionGuardRuleHelpers.Block(
                        "InputLifterZ",
                        "InputFeederY must be at a cassette-side safe teaching position before InputLifterZ move. " +
                        feederDetail,
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
            string autoFeederDetail;
            if (!IsWaferFeederYSafeForWaferLifterZ(feeder, out autoFeederDetail))
                return MotionGuardRuleHelpers.Block(
                    "InputLifterZ",
                    "InputFeederY must be at a cassette-side safe teaching position before InputLifterZ move. " +
                    autoFeederDetail,
                    out reason);

            return true;
        }

        // 인터락 기준: InputLifterZ 이동 전 InputFeederY가 카세트 측 안전 위치인지 판단한다.
        // 기존 조건: 티칭 위치 predicate(Avoid/Exchange/Home)만 안전으로 인정했다.
        //           세 위치 모두 0 부근이라, 피더가 wafer를 들고 스테이지 쪽(WaferUnloadPosition 607 등)에 있는
        //           동안에는 리프터 이동이 무조건 차단되었다. Stage -> Feeder -> Cassette 언로드는 피더가
        //           제품을 잡은 뒤 카세트 슬롯 위치를 잡아야 하므로, 리프터가 해당 슬롯에 미리 가 있지 않은
        //           상태(재시작 직후 등)에서는 구조적으로 항상 IN-CST-LIFTER-INTERLOCK이 발생했다.
        //           (실장비 2026-07-25 21:26:53, CYCLE RUN INPUT UNLOAD)
        // 현재 기준(사용자 승인 2026-07-25): "− 방향(카세트 진입)"에 있지 않으면 안전으로 인정한다.
        //           기계 전제(사용자 확인 2026-07-25): InputFeeder는 − 방향으로 이동할 때만 카세트 안으로
        //           들어간다. 티칭값도 이를 뒷받침한다 — CassetteLoad/UnloadPosition = -54.529(카세트 진입),
        //           Avoid/CassetteExchange = 0, 스테이지 측 WaferLoad/UnloadPosition = +607.269.
        //           즉 0 이상(+ 방향)에서는 피더가 카세트 진입 경로 밖에 있어 리프터 승강과 간섭하지 않는다.
        // 완화 범위: 기존 허용(Avoid/Exchange/Home)은 그대로 두고 "0 이상" 조건만 추가한다(순수 확대).
        //           − 방향 위치(카세트 진입/슬롯 접근)는 변경 없이 계속 차단된다.
        //           이동 중 판정은 호출부(Manual/Home/Auto)에서 이미 차단하며, 여기서도 방어적으로 재확인한다.
        private static bool IsWaferFeederYSafeForWaferLifterZ(InputFeederUnit feeder, out string detail)
        {
            detail = string.Empty;

            if (feeder == null || feeder.FeederY == null)
            {
                return true;
            }

            QMC.Common.Motion.BaseAxis feederY = feeder.FeederY;
            double tolerance = feederY.Config != null && feederY.Config.InPositionTolerance > 0.0
                ? feederY.Config.InPositionTolerance
                : 0.05;
            double actual = feederY.ActualPosition;
            detail = "InputFeederY actual=" + actual.ToString("0.###") +
                     ", cassetteEntryLimit=" + (-tolerance).ToString("0.###");

            // 기존 허용: 티칭된 Avoid / CassetteExchange / Home 위치.
            if (feeder.IsWaferFeederYInAvoidPosition()
                || feeder.IsWaferFeederYInExchangePosition()
                || feeder.IsWaferFeederYInHomePosition())
            {
                return true;
            }

            // 방어 조건: 이동 중이면 현재 좌표로 진입 여부를 확정할 수 없으므로 안전측으로 차단한다.
            if (feederY.IsMoving)
            {
                detail += ", moving=Y";
                return false;
            }

            // 완화 조건: − 방향(카세트 진입)이 아니면 리프터 승강과 물리 간섭이 없다.
            return actual >= -tolerance;
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
