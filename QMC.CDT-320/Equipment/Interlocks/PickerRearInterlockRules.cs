using QMC.Common.Motion;

using QMC.Common.IO;

namespace QMC.CDT320.Interlocks
{
    public static class PickerRearInterlockRules
    {
        #region 규칙 진입

        // 인터락 항목: RearPicker X/Y/T/Z 이동 요청을 해당 축별 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: 요청 또는 장비 참조가 없으면 RearPicker 인터락을 적용하지 않는다.
            if (request == null || request.Machine == null)
                return true;

            // 현재 기준: 이동 축이 RearPickerX이면 X축 인터락 규칙으로 분기한다.
            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerX"))
                return VerifyRearPickerX(request, out reason);

            // 현재 기준: 이동 축이 RearPickerY이면 Y축 인터락 규칙으로 분기한다.
            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerY"))
                return VerifyRearPickerY(request, out reason);

            // 현재 기준: 이동 축이 RearPickerT0~T3이면 T축 인터락 규칙으로 분기한다.
            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerT0", "RearPickerT1", "RearPickerT2", "RearPickerT3"))
                return VerifyRearPickerT(request, out reason);

            // 현재 기준: 이동 축이 RearPickerZ0~Z3이면 Z축 인터락 규칙으로 분기한다.
            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3"))
                return VerifyRearPickerZ(request, out reason);

            return true;
        }

        #endregion

        #region Picker X 및 존 진입

        // 인터락 항목: RearPickerX 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyRearPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            // 기구 간섭 방지: Input/Output Feeder와 Cassette가 모두
            // 정지된 Avoid 위치일 때만 RearPickerX 이동을 허용한다.
            if (!VerifyAxisAvoidForRearPickerX(
                request != null ? request.Machine : null,
                out reason))
            {
                return false;
            }

            if (!PickerZoneInterlockRules.VerifyPickerXGlobalMachineClearance(
                request != null ? request.Machine : null,
                "RearPickerX",
                out reason))
                return false;

            // 인터락 항목: RearPickerX 조그는 Z 상승 조건을 확인한 뒤 목표 Zone 판정만 생략한다.
            if (MotionGuardRuleHelpers.IsJogMove(request))
                return CanJogRearPickerX(request, out reason);

            // 인터락 항목(사용자 승인 2026-07-25): 구동 중 위치 오버라이드(팔로잉 중간 세그먼트)는
            // 조그와 같은 방식으로 목표 Zone 판정만 생략하고 Z 상승/Reticle/Busy는 유지한다.
            // Y 대향 거리와 SharedRailX 페어 간격은 이 룰 밖(레지스트리 선행 룰)에서 계속 평가된다.
            if (MotionGuardRuleHelpers.IsPositionOverrideStep(request))
                return CanPositionOverrideRearPickerX(request, out reason);

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoRearPickerX(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualRearPickerX(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeRearPickerX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool VerifyAxisAvoidForRearPickerX(
                            CDT320_Machine machine,
                            out string reason)
        {
            reason = string.Empty;

            if (machine == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 불가: Machine 참조가 없습니다.",
                    out reason);
            }

            InputFeederUnit inputFeeder = machine.InputFeederUnit;
            if (inputFeeder == null || inputFeeder.FeederY == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 불가: InputFeederY 상태를 확인할 수 없습니다.",
                    out reason);
            }

            if (inputFeeder.FeederY.IsMoving ||
                !inputFeeder.IsWaferFeederAvoidPositionCheck())
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 불가: InputFeeder가 정지된 Avoid 위치가 아닙니다.",
                    out reason);
            }

            OutputFeederUnit outputFeeder = machine.OutputFeederUnit;
            if (outputFeeder == null || outputFeeder.FeederY == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 불가: OutputFeederY 상태를 확인할 수 없습니다.",
                    out reason);
            }

            if (outputFeeder.FeederY.IsMoving ||
                !outputFeeder.IsBinFeederAvoidPositionCheck())
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 불가: OutputFeeder가 정지된 Avoid 위치가 아닙니다.",
                    out reason);
            }

            InputCassetteUnit inputCassette = machine.InputCassetteUnit;
            if (inputCassette == null || inputCassette.InputLifterZ == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 불가: InputCassette 상태를 확인할 수 없습니다.",
                    out reason);
            }

            if (inputCassette.InputLifterZ.IsMoving ||
                !inputCassette.IsWaferLifterZInAvoidPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 불가: InputCassette가 정지된 Avoid 위치가 아닙니다.",
                    out reason);
            }

            OutputCassetteUnit outputCassette = machine.OutputCassetteUnit;
            if (outputCassette == null || outputCassette.OutputLifterZ == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 불가: OutputCassette 상태를 확인할 수 없습니다.",
                    out reason);
            }

            if (outputCassette.OutputLifterZ.IsMoving ||
                !outputCassette.IsBinLifterZInAvoidPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 불가: OutputCassette가 정지된 Avoid 위치가 아닙니다.",
                    out reason);
            }

            return true;
        }

        // 인터락 항목: 조그 RearPickerX는 Z Home/Avoid, Reticle, Busy 조건을 유지하고 Zone 판정만 생략한다.
        private static bool CanJogRearPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;

                // 현재 기준: RearPickerX 조그 전 Z0~Z3는 모두 상승(Home 또는 Avoid) 상태여야 한다.
                if (!VerifyRearPickerZAxesHomeOrAvoid(rear, "RearPickerX", out reason))
                    return false;

                // 현재 기준: RearPickerX 조그 전 Reticle 관련 실린더가 이동 중이면 차단한다.
                if (!VerifyReticleCylinderClear(machine, "RearPickerX", out reason))
                    return false;

                return VerifyRearPickerNotBusy(rear, "RearPickerX", out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX Jog 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: 구동 중 위치 오버라이드 RearPickerX는 Z 상승, Reticle, Busy 조건을 유지하고 Zone 판정만 생략한다.
        // 생략 근거: 팔로잉 중간 세그먼트 좌표는 어떤 티칭 존에도 속하지 않아 목표 존이 Unknown이 되고,
        //   최종 목표에 대한 존 진입 조건(Z Avoid/ExpandingZ/Feeder Dog/Feeder Down/VisionX Avoid/상대 Y)은
        //   팔로잉 최초 명령(AxisMove 경로)에서 이미 1회 검증된다. 이동 중 실제 안전은 Y 대향 거리
        //   (VerifyFacingYDistanceFirst), SharedRailX 페어 간격, 소프트리밋, 실시간 충돌 감시가 담당한다.
        private static bool CanPositionOverrideRearPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;

                // 현재 기준: 오버라이드 중에도 RearPickerZ0~Z3는 모두 상승(Home 또는 Avoid) 상태여야 한다.
                // PickUpZHold 면제(사용자 승인 2026-07-26): Conti 픽업 팔로잉 진입의 오버라이드는
                // 유지 픽커 Z만 위치 요구 면제(비이동 요구는 AvoidForMove/입구 룰이 유지).
                int overridePickUpZHoldExempt;
                bool overrideHasPickUpZHold =
                    MotionGuardRuleHelpers.TryGetPickUpZHoldExemptPickerIndex(request, out overridePickUpZHoldExempt);
                // Place ZHold 면제(사용자 지시 2026-07-26): Auto Conti Place의 팔로잉 오버라이드도
                // Output존 InspectionZHold 신뢰 범위로 Z 위치 요구를 면제한다((A)와 동일 범위).
                bool overridePlaceZHold = request != null && request.Intent != null &&
                    request.Intent.InspectionZHold &&
                    request.Intent.InspectionContinuous &&
                    request.Intent.PickerZone == PickerWorkZone.Output;
                if (!overridePlaceZHold && !overrideHasPickUpZHold &&
                    !VerifyRearPickerZAxesHomeOrAvoid(rear, "RearPickerX", out reason))
                    return false;
                if (!overridePlaceZHold && overrideHasPickUpZHold &&
                    !VerifyRearPickerZAxesHomeOrAvoidExcept(rear, "RearPickerX", overridePickUpZHoldExempt, out reason))
                    return false;

                // 현재 기준: Reticle 관련 실린더가 이동 중이면 차단한다.
                if (!VerifyReticleCylinderClear(machine, "RearPickerX", out reason))
                    return false;

                return VerifyRearPickerNotBusy(rear, "RearPickerX", out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 위치 오버라이드 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: 자동 RearPickerX 이동 전 수동 기본 조건, Z Avoid, VisionX Avoid, Busy 상태를 확인한다.
        private static bool CanAutoRearPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;

            try
            {
                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;

                // 현재 기준: Auto RearPickerX도 Manual RearPickerX 기본 인터락을 먼저 통과해야 한다.
                if (!CanManualRearPickerX(request, out reason))
                    return false;

                // 현재 기준: RearPickerX 자동 이동 전 Z0~Z3가 Avoid 위치여야 한다.
                if (!VerifyRearPickerZAxesAvoidForMove(rear, "RearPickerX", request, out reason))
                    return false;

                // 현재 기준: Collet Calibration Bottom 이동이면 Input/Output VisionX가 Avoid 위치여야 한다.
                if (!VerifyVisionXAvoidForColletCalibrationBottomMove(machine, "RearPickerX", request, out reason))
                    return false;

                // 기존 조건: Auto RearPickerX에서 PickerZone 룰을 직접 확인했다.
                // 현재 필요 여부: Manual RearPickerX 기본 인터락에서 먼저 확인하므로 중복 호출하지 않는다.
                //if (!PickerZoneInterlockRules.VerifyRearPickerXMove(request, out reason))
                //    return false;

                return VerifyRearPickerNotBusy(rear, "RearPickerX", out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "RearPickerX 이동 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: 수동 RearPickerX 이동 전 목표 존별 Input/Process/Output 진입 조건과 PickerZone 조건을 확인한다.
        private static bool CanManualRearPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                PickerWorkZone targetZone = PickerZoneInterlockRules.ResolveManualPickerXTargetZone(request, false);
                PickerWorkZone currentZone = PickerZoneInterlockRules.ResolveManualPickerXCurrentZone(machine, false);

                // 현재 기준: RearPickerX Manual 목표 존을 판단할 수 없으면 충돌 방지를 위해 차단한다.
                if (targetZone == PickerWorkZone.Unknown)
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX 이동 불가: Manual X 목표 존을 판단할 수 없습니다. target=" +
                        (request != null ? request.TargetValue.ToString("0.###") : "<null>") +
                        ", targetName=" + (request != null ? request.TargetName : "<null>"),
                        out reason);

                // 현재 기준: Input 진입은 Z Avoid/0 이상, ExpandingZ 0 이하, InputFeederY Avoid/0 이하, Feeder Down, InputVisionX Avoid/0 이하, X 안전거리 안에서 양쪽 PickerY 동시 전진을 금지한다.
                if (targetZone == PickerWorkZone.Input &&
                    !VerifyManualRearPickerXInputEntry(request, machine, out reason))
                    return false;

                // 현재 기준: Output 진입은 Z Avoid/0 이상, GoodStageZ Process 이하, OutputFeederY Avoid/0 이하, Feeder Down, OutputVisionX Avoid/0 이하, X 안전거리 안에서 양쪽 PickerY 동시 전진을 금지한다.
                if (targetZone == PickerWorkZone.Output &&
                    !VerifyManualRearPickerXOutputEntry(request, machine, out reason))
                    return false;

                // 현재 기준: Manual Process 존 진입은 RearPickerZ0~Z3가 Avoid 또는 0 이상 위치여야 한다.
                if (PickerZoneInterlockRules.IsManualPickerXProcessZone(targetZone) &&
                    !PickerZoneInterlockRules.IsManualPickerXProcessZone(currentZone) &&
                    !CanKeepRearPickerZDuringXMove(request) &&
                    !VerifyRearPickerZAxesAvoidOrNonNegative(machine != null ? machine.PickerRearUnit : null, "RearPickerX", out reason))
                    return false;

                // 현재 기준: RearPickerX 목표 존과 반대 Picker 점유 상태를 PickerZone 룰에서 최종 확인한다.
                if (!PickerZoneInterlockRules.VerifyRearPickerXMove(request, out reason))
                    return false;

                // 기존 조건: Manual X 이동 전체에 InputVisionX Home, FrontPickerY Avoid, RearPickerY Avoid, Z Avoid를 일괄 적용했다.
                // 현재 필요 여부: 사용 안 함. 현재는 Input/Process/Output 목표 존별 진입 조건으로 분리해서 적용한다.
                //InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                //string axisReason;
                //if (stage != null &&
                //    !MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(stage.CameraX, "InputVisionX", out axisReason))
                //    return MotionGuardRuleHelpers.Block("RearPickerX", "RearPickerX HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason, out reason);
                //if (front != null && !front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                //    return MotionGuardRuleHelpers.Block("RearPickerX", "RearPickerX HOME blocked. FrontPickerY must be at Avoid position.", out reason);
                //if (rear != null && !rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                //    return MotionGuardRuleHelpers.Block("RearPickerX", "RearPickerX HOME blocked. RearPickerY must be at Avoid position.", out reason);
                //if (!VerifyRearPickerZAxesAvoid(rear, "RearPickerX", out reason))
                //    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "Exception occurred while verifying RearPickerX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: RearPickerX Input 진입 전 Z, ExpanderZ, InputFeeder, InputVisionX, 상대 PickerY 거리 조건을 확인한다.
        private static bool VerifyManualRearPickerXInputEntry(MotionGuardRuleContext request, CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            // 현재 기준: Input 진입 전 RearPickerZ0~Z3는 Avoid 또는 0 이상 위치여야 한다.
            // PickUpZHold 면제(사용자 승인 2026-07-26): Auto Conti 픽업 die 간 이동은 지정 픽커만 면제.
            int inputEntryPickUpZHoldExempt;
            if (!MotionGuardRuleHelpers.TryGetPickUpZHoldExemptPickerIndex(request, out inputEntryPickUpZHoldExempt))
                inputEntryPickUpZHoldExempt = -1;
            if (!VerifyRearPickerZAxesAvoidOrNonNegative(machine != null ? machine.PickerRearUnit : null, "RearPickerX", inputEntryPickUpZHoldExempt, out reason))
                return false;

            // 현재 기준: Input 진입 전 InputExpandingZ는 0 이하 위치여야 한다.
            if (!VerifyInputExpanderZAtOrBelowZero(machine != null ? machine.InputStageUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputFeeder Avoid Dog(X090)가 ON이어야 한다.
            if (!VerifyInputFeederAvoidDog(machine != null ? machine.InputFeederUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputFeeder Lift는 Down 상태여야 한다.
            if (!VerifyInputFeederDown(machine != null ? machine.InputFeederUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputVisionX는 Avoid 또는 0 이하 위치여야 한다.
            if (!VerifyInputVisionXAtAvoidOrBelowZero(request, machine, machine != null ? machine.InputStageUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: RearPickerX Input 진입 전 X 안전거리 안에서 Front/Rear PickerY가 동시에 전진하면 차단한다.
            if (!PickerZoneInterlockRules.VerifyPickerXOppositeYClearance(request, false, "RearPickerX", out reason))
                return false;

            // 기존 조건: RearPickerX Input 진입 전 상대 FrontPickerY는 거리와 무관하게 무조건 Avoid 위치여야 했다.
            // 현재 필요 여부: 사용 안 함. X 안전거리 안에서도 한쪽 PickerY만 전진한 상태는 허용하고 양쪽 동시 전진만 차단한다.
            //return VerifyFrontPickerYAvoidForRearPickerX(machine, out reason);

            return true;
        }

        // 인터락 항목: RearPickerX Output 진입 전 Z, GoodStageZ, OutputFeeder, OutputVisionX, 상대 PickerY 거리 조건을 확인한다.
        private static bool VerifyManualRearPickerXOutputEntry(MotionGuardRuleContext request, CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            // 현재 기준: Output 진입 전 RearPickerZ0~Z3는 Avoid 또는 0 이상 위치여야 한다.
            // Place ZHold 면제(사용자 지시 2026-07-26, "Auto Place Conti 인터락 통과"): Auto Conti
            // Place의 InspectionZHold 이동(PrePlace 파킹/선행 하강 유지)은 Z 위치 요구만 면제한다 —
            // (A) AvoidForMove의 기존 Output존 ZHold 신뢰와 동일 범위. 그 외 검사는 전부 유지.
            if (!CanKeepRearPickerZDuringXMove(request) &&
                !VerifyRearPickerZAxesAvoidOrNonNegative(machine != null ? machine.PickerRearUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputGoodStageZ는 ProcessPos 이하 위치여야 한다.
            if (!VerifyGoodStageZAtOrBelowProcess(machine != null ? machine.OutputStageUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputFeeder Avoid Dog(X091)가 ON이어야 한다.
            if (!VerifyOutputFeederAvoidDog(machine != null ? machine.OutputFeederUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputFeeder Lift는 Down 상태여야 한다.
            if (!VerifyOutputFeederDown(machine != null ? machine.OutputFeederUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputVisionX는 Avoid 또는 0 이하 위치여야 한다.
            if (!VerifyOutputVisionXAtAvoidOrBelowZero(request, machine, machine != null ? machine.OutputStageUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: RearPickerX Output 진입 전 X 안전거리 안에서 Front/Rear PickerY가 동시에 전진하면 차단한다.
            if (!PickerZoneInterlockRules.VerifyPickerXOppositeYClearance(request, false, "RearPickerX", out reason))
                return false;

            // 기존 조건: RearPickerX Output 진입 전 상대 FrontPickerY는 거리와 무관하게 무조건 Avoid 위치여야 했다.
            // 현재 필요 여부: 사용 안 함. X 안전거리 안에서도 한쪽 PickerY만 전진한 상태는 허용하고 양쪽 동시 전진만 차단한다.
            //return VerifyFrontPickerYAvoidForRearPickerX(machine, out reason);

            return true;
        }

        // 인터락 항목: RearPickerZ 전체가 Avoid 위치 또는 0 이상 위치인지 확인한다.
        internal static bool VerifyRearPickerZAxesAvoidOrNonNegative(PickerRearUnit picker, string movingName, out string reason)
        {
            return VerifyRearPickerZAxesAvoidOrNonNegative(picker, movingName, -1, out reason);
        }

        // PickUpZHold 면제 오버로드(정정 2026-07-26): exemptIndex 픽커 Z는 검사에서 제외
        // (Avoid 상승 중 이동 허용). exemptIndex=-1이면 기존과 완전 동일.
        internal static bool VerifyRearPickerZAxesAvoidOrNonNegative(PickerRearUnit picker, string movingName, int exemptIndex, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: RearPicker 참조가 없으면 Z 위치 조건을 적용하지 않는다.
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = ResolveRearPickerAxis(picker, zAxis);
                // PickUpZHold 면제(정정 2026-07-26): 시퀀스가 Avoid로 상승 명령한 축은
                // 이동 중이어도 허용 — 검사에서 제외한다.
                if (i == exemptIndex)
                    continue;

                // 현재 기준: RearPickerZ축이 이동 중이면 Input/Output/Process 진입을 차단한다.
                if (axis != null && axis.IsMoving)
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: Rear" + zAxis + " 축이 이동 중입니다.",
                        out reason);

                // 현재 기준: RearPickerZ축은 Avoid 위치이거나 ActualPosition이 0 이상이어야 한다.
                if (picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition") ||
                    IsAxisAtOrAboveZero(axis))
                    continue;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: Rear" + zAxis + " 축이 Avoid 또는 0 이상 위치가 아닙니다. actual=" +
                    (axis != null ? axis.ActualPosition.ToString("0.###") : "<null>"),
                    out reason);
            }

            return true;
        }

        // 인터락 항목: Picker Input 진입 전 InputExpandingZ가 0 이하 또는 Avoid 위치인지 확인한다.
        private static bool VerifyInputExpanderZAtOrBelowZero(InputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: InputStage 또는 ExpanderZ 참조가 없으면 ExpanderZ 조건을 적용하지 않는다.
            if (stage == null || stage.ExpanderZ == null)
                return true;

            // 현재 기준: InputExpandingZ가 이동 중이면 Picker Input 진입을 차단한다.
            if (stage.ExpanderZ.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: InputExpandingZ가 이동 중입니다.", out reason);

            // 현재 기준: InputExpandingZ ActualPosition이 0 이하이거나 Avoid 위치이면 Input 진입/PickerZ 이동을 허용한다.
            if (stage.ExpanderZ.ActualPosition <= ResolveAxisTolerance(stage.ExpanderZ))
                return true;
            if (stage.Recipe != null &&
                stage.Recipe.WaferZ != null &&
                System.Math.Abs(stage.ExpanderZ.ActualPosition - stage.Recipe.WaferZ.AvoidPosition) <= ResolveAxisTolerance(stage.ExpanderZ))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " 이동 불가: InputExpandingZ가 0 이하 또는 Avoid 위치가 아닙니다. actual=" + stage.ExpanderZ.ActualPosition.ToString("0.###"),
                out reason);
        }

        // 인터락 항목: Picker Input 진입 전 InputFeeder Avoid Dog(X090)가 ON인지 확인한다.
        private static bool VerifyInputFeederAvoidDog(InputFeederUnit feeder, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: InputFeeder 또는 FeederY 참조가 없으면 InputFeederY 조건을 적용하지 않는다.
            if (feeder == null || feeder.FeederY == null)
                return true;

            // 현재 기준: InputFeederY가 이동 중이면 Picker Input 진입을 차단한다.
            if (feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: InputFeederY가 이동 중입니다.", out reason);

            if (feeder.IsWaferFeederAvoidPositionCheck())
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " 이동 불가: InputFeeder Avoid Dog(X090)가 ON이 아닙니다. actual=" + feeder.FeederY.ActualPosition.ToString("0.###"),
                out reason);
        }

        // 인터락 항목: Picker Input 진입 전 InputFeeder Lift가 Down 상태인지 확인한다.
        private static bool VerifyInputFeederDown(InputFeederUnit feeder, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: InputFeeder 참조가 없으면 Lift Down 조건을 적용하지 않는다.
            if (feeder == null)
                return true;

            // 현재 기준: InputFeeder Lift는 Down 상태여야 Picker Input 진입을 허용한다.
            if (feeder.IsWaferFeederDown())
                return true;

            return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: InputFeeder Lift가 Down 상태가 아닙니다.", out reason);
        }

        // 인터락 항목: Picker Input 진입 전 InputVisionX가 Avoid 또는 0 이하 위치인지 확인한다.
        // 제3 분기(사용자 승인 2026-07-24): 피커 이동 목표와 InputVisionX Actual/Command 양쪽이
        // 페어 간격식으로 SafetyDistance를 만족하면 진입 허용 (RetreatExtra 미포함 — R5).
        private static bool VerifyInputVisionXAtAvoidOrBelowZero(MotionGuardRuleContext request, CDT320_Machine machine, InputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: InputStage 또는 InputVisionX 참조가 없으면 InputVisionX 조건을 적용하지 않는다.
            if (stage == null || stage.CameraX == null)
                return true;

            string clearanceDetail;
            BaseAxis pickerX = machine != null && machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerX : null;
            if (request != null &&
                MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry(
                    machine, pickerX, request.TargetValue, stage.CameraX, out clearanceDetail))
                return true;

            // 현재 기준: InputVisionX가 이동 중이면 Picker Input 진입을 차단한다.
            if (stage.CameraX.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: InputVisionX가 이동 중입니다.", out reason);

            // 현재 기준: InputVisionX는 Avoid 위치이거나 ActualPosition이 0 이하이어야 한다.
            if (stage.IsVisionXInAvoidPosition() ||
                stage.CameraX.ActualPosition <= ResolveAxisTolerance(stage.CameraX))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " 이동 불가: InputVisionX가 Avoid/0 이하 위치가 아니고 페어 간격도 부족합니다. actual=" + stage.CameraX.ActualPosition.ToString("0.###"),
                out reason);
        }

        // 인터락 항목: Picker Output 진입 전 OutputGoodStageZ가 Process 이하 위치인지 확인한다.
        private static bool VerifyGoodStageZAtOrBelowProcess(OutputStageUnit outputStage, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: OutputStage 참조가 없으면 GoodStageZ 조건을 적용하지 않는다.
            if (outputStage == null)
                return true;

            BaseAxis goodZ = outputStage.GoodStage != null ? outputStage.GoodStage.StageZ : null;
            // 방어 조건: GoodStageZ 축 또는 레시피가 없으면 Process 이하 위치를 판단할 수 없어 차단한다.
            if (goodZ == null || outputStage.Recipe == null || outputStage.Recipe.GoodStageZ == null)
                return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: OutputGoodStageZ 정보를 확인할 수 없습니다.", out reason);

            // 현재 기준: OutputGoodStageZ가 이동 중이면 Picker Output 진입을 차단한다.
            if (goodZ.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: OutputGoodStageZ가 이동 중입니다.", out reason);

            double process = outputStage.Recipe.GoodStageZ.ProcessPosition;
            // 현재 기준: OutputGoodStageZ ActualPosition이 ProcessPos 이하이면 Output 진입을 허용한다.
            if (goodZ.ActualPosition <= process + ResolveAxisTolerance(goodZ))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " 이동 불가: OutputGoodStageZ가 ProcessPos 이하가 아닙니다. actual=" +
                goodZ.ActualPosition.ToString("0.###") + ", process=" + process.ToString("0.###"),
                out reason);
        }

        // 인터락 항목: Picker Output 진입 전 OutputFeeder Avoid Dog(X091)가 ON인지 확인한다.
        private static bool VerifyOutputFeederAvoidDog(OutputFeederUnit feeder, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: OutputFeeder 또는 FeederY 참조가 없으면 OutputFeederY 조건을 적용하지 않는다.
            if (feeder == null || feeder.FeederY == null)
                return true;

            // 현재 기준: OutputFeederY가 이동 중이면 Picker Output 진입을 차단한다.
            if (feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: OutputFeederY가 이동 중입니다.", out reason);

            if (feeder.IsBinFeederAvoidPositionCheck())
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " 이동 불가: OutputFeeder Avoid Dog(X091)가 ON이 아닙니다. actual=" + feeder.FeederY.ActualPosition.ToString("0.###"),
                out reason);
        }

        // 인터락 항목: Picker Output 진입 전 OutputFeeder Lift가 Down 상태인지 확인한다.
        private static bool VerifyOutputFeederDown(OutputFeederUnit feeder, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: OutputFeeder 참조가 없으면 Lift Down 조건을 적용하지 않는다.
            if (feeder == null)
                return true;

            // 현재 기준: OutputFeeder Lift는 Down 상태여야 Picker Output 진입을 허용한다.
            if (feeder.IsFeederDown())
                return true;

            return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: OutputFeeder Lift가 Down 상태가 아닙니다.", out reason);
        }

        // 인터락 항목: Picker Output 진입 전 OutputVisionX가 Avoid 또는 0 이하 위치인지 확인한다.
        // 제3 분기(사용자 승인 2026-07-24): 피커 이동 목표와 OutputVisionX Actual/Command 양쪽이
        // 페어 간격식으로 SafetyDistance를 만족하면 진입 허용 (RetreatExtra 미포함 — R5).
        private static bool VerifyOutputVisionXAtAvoidOrBelowZero(MotionGuardRuleContext request, CDT320_Machine machine, OutputStageUnit outputStage, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: OutputStage 또는 OutputVisionX 참조가 없으면 OutputVisionX 조건을 적용하지 않는다.
            if (outputStage == null || outputStage.OutputCameraX == null)
                return true;

            string clearanceDetail;
            BaseAxis pickerX = machine != null && machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerX : null;
            if (request != null &&
                MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry(
                    machine, pickerX, request.TargetValue, outputStage.OutputCameraX, out clearanceDetail))
                return true;

            // 현재 기준: OutputVisionX가 이동 중이면 Picker Output 진입을 차단한다.
            if (outputStage.OutputCameraX.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: OutputVisionX가 이동 중입니다.", out reason);

            // 현재 기준: OutputVisionX는 Avoid 위치이거나 ActualPosition이 0 이하이어야 한다.
            if (outputStage.IsVisionXInAvoidPosition() ||
                outputStage.OutputCameraX.ActualPosition <= ResolveAxisTolerance(outputStage.OutputCameraX))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " 이동 불가: OutputVisionX가 Avoid/0 이하 위치가 아니고 페어 간격도 부족합니다. actual=" + outputStage.OutputCameraX.ActualPosition.ToString("0.###"),
                out reason);
        }

        // 인터락 항목: RearPickerX 이동 전 상대 FrontPickerY가 Avoid 위치인지 확인한다.
        private static bool VerifyFrontPickerYAvoidForRearPickerX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
            // 방어 조건: FrontPicker 참조가 없으면 상대 PickerY 조건을 적용하지 않는다.
            if (front == null)
                return true;

            // 현재 기준: RearPickerX 진입 전 상대 FrontPickerY는 Avoid 위치여야 한다.
            if (front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                return true;

            return MotionGuardRuleHelpers.Block("RearPickerX", "RearPickerX 이동 불가: 상대 FrontPickerY가 Avoid 위치가 아닙니다.", out reason);
        }

        // 인터락 항목: PickerX 이동 전 InputVisionX가 정지 및 Avoid 위치인지 확인한다.
        private static bool VerifyInputVisionXAvoidForPickerX(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                // 방어 조건: InputStage 또는 InputVisionX 축이 없으면 VisionX Avoid 조건을 건너뛴다.
                if (stage == null || stage.CameraX == null)
                    return true;

                // 현재 기준: InputVisionX가 이동 중이면 PickerX 이동을 차단한다.
                if (MotionGuardRuleHelpers.IsAxisMoving(stage.CameraX))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: InputVisionX가 이동 중입니다. PickerX 이동 전 InputVisionX가 Avoid 위치에 있어야 합니다.",
                        out reason);

                // 현재 기준: InputVisionX가 Avoid 위치이면 PickerX 이동을 허용한다.
                if (stage.IsVisionXInAvoidPosition())
                    return true;

                double avoid = stage.Recipe != null && stage.Recipe.VisionX != null ? stage.Recipe.VisionX.AvoidPosition : 0.0;
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: PickerX 이동 전 InputVisionX가 Avoid 위치에 있어야 합니다. actual=" +
                    stage.CameraX.ActualPosition.ToString("F3") +
                    ", avoid=" + avoid.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 전 InputVisionX Avoid 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
        }

        // 인터락 항목: Collet Calibration Bottom 진입 전 Input/Output VisionX가 Avoid 위치인지 확인한다.
        private static bool VerifyVisionXAvoidForColletCalibrationBottomMove(CDT320_Machine machine, string movingName, MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            // 현재 기준: Collet Calibration Bottom 이동이 아니면 VisionX Avoid 추가 조건을 적용하지 않는다.
            if (!IsColletCalibrationBottomMove(request))
                return true;

            // 현재 기준: Collet Calibration Bottom 진입 전 InputVisionX가 Avoid 위치여야 한다.
            if (!VerifyInputVisionXAvoidForPickerX(machine, movingName, out reason))
                return false;

            try
            {
                OutputStageUnit stage = machine != null ? machine.OutputStageUnit : null;
                // 방어 조건: OutputStage 또는 OutputVisionX 축이 없으면 OutputVisionX 조건을 건너뛴다.
                if (stage == null || stage.OutputCameraX == null)
                    return true;

                // 현재 기준: OutputVisionX가 이동 중이면 Collet Calibration Bottom 진입을 차단한다.
                if (MotionGuardRuleHelpers.IsAxisMoving(stage.OutputCameraX))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: Collet Calibration Bottom 진입 전 OutputVisionX가 이동 중입니다.",
                        out reason);
                }

                // 현재 기준: OutputVisionX가 Avoid 위치이면 Collet Calibration Bottom 진입을 허용한다.
                if (stage.IsVisionXInAvoidPosition())
                    return true;

                double avoid = stage.Recipe != null && stage.Recipe.VisionX != null ? stage.Recipe.VisionX.AvoidPosition : 0.0;
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: Collet Calibration Bottom 진입 전 OutputVisionX가 Avoid 위치에 있어야 합니다. actual=" +
                    stage.OutputCameraX.ActualPosition.ToString("F3") +
                    ", avoid=" + avoid.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " Collet Calibration Bottom 진입 전 OutputVisionX Avoid 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
        }

        // 인터락 기준: 현재 이동이 Collet Calibration Bottom 존 이동인지 판단한다.
        private static bool IsColletCalibrationBottomMove(MotionGuardRuleContext request)
        {
            // 현재 기준: ColletCalibration 플래그와 Bottom 존이 모두 맞을 때 Bottom 보정 이동으로 본다.
            return request != null &&
                   request.Intent != null &&
                   request.Intent.ColletCalibration &&
                   request.Intent.PickerZone == PickerWorkZone.Bottom;
        }

        // 인터락 항목: RearPicker 평면 이동 전 Z축 전체 Avoid 여부를 확인하되 검사/보정 예외를 반영한다.
        private static bool VerifyRearPickerZAxesAvoidForMove(PickerRearUnit picker, string movingName, MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: RearPicker 참조가 없으면 Z Avoid 조건을 적용하지 않는다.
            if (picker == null)
                return true;

            // 현재 기준: Inspection Z Hold 이동은 Z축을 유지해야 하므로 Z Avoid 조건에서 제외한다.
            if (IsInspectionZHoldMove(request))
                return true;

            string fineAlignDetail;
            // 현재 기준: Collet Calibration Fine Align 이동은 Z축을 유지해야 하므로 Z Avoid 조건에서 제외한다.
            if (MotionGuardRuleHelpers.IsColletCalibrationFineAlignMove(request, false, out fineAlignDetail))
                return true;

            // PickUpZHold 면제(사용자 승인 2026-07-26, 정정: Avoid 상승 중 이동 허용):
            // Auto Conti 픽업 die 간 이동에서 지정 픽커 Z는 검사에서 제외한다 — 시퀀스가
            // Avoid로 상승 명령한 축이므로 이동 중이어도 허용(나머지 Z는 기존 그대로).
            int pickUpZHoldExemptIndex;
            bool hasPickUpZHoldExempt =
                MotionGuardRuleHelpers.TryGetPickUpZHoldExemptPickerIndex(request, out pickUpZHoldExemptIndex);

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                if (hasPickUpZHoldExempt && i == pickUpZHoldExemptIndex)
                    continue;

                // 현재 기준: RearPicker 평면 이동 전 Z0~Z3는 모두 Avoid 위치여야 한다.
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: Rear" + zAxis + " 축이 Avoid 위치가 아닙니다.",
                        out reason);
            }

            return true;
        }

        // 인터락 기준: 검사 중 PickerZ를 유지해도 되는 Z Hold 이동인지 판단한다.
        private static bool IsInspectionZHoldMove(MotionGuardRuleContext request)
        {
            // 현재 기준: InspectionZHold 의도가 없으면 Z Hold 이동으로 보지 않는다.
            if (request == null || request.Intent == null || !request.Intent.InspectionZHold)
                return false;

            // 현재 기준: Bottom/Side/Output 존 이동에서만 Inspection Z Hold 예외를 적용한다.
            return request.Intent.PickerZone == PickerWorkZone.Bottom ||
                   request.Intent.PickerZone == PickerWorkZone.Side ||
                   request.Intent.PickerZone == PickerWorkZone.Output;
        }

        // 인터락 기준: RearPickerY 이동 중 PickerZ 유지 예외를 허용할지 판단한다.
        private static bool CanKeepRearPickerZDuringYMove(MotionGuardRuleContext request)
        {
            // 현재 기준: 자동 티칭 이동에서만 Z Hold/FineAlign 예외를 적용한다.
            if (request == null || request.MoveKind != MotionGuardMoveKind.AxisTeachingMove)
                return false;

            if (IsInspectionZHoldMove(request))
                return true;

            string fineAlignDetail;
            return MotionGuardRuleHelpers.IsColletCalibrationFineAlignMove(request, false, out fineAlignDetail);
        }

        // 인터락 기준: RearPickerX 이동 중 PickerZ 유지 예외를 허용할지 판단한다.
        private static bool CanKeepRearPickerZDuringXMove(MotionGuardRuleContext request)
        {
            // 현재 기준: Auto Bottom/Side 검사 연속 X 이동은 PickerZ가 검사 높이를 유지할 수 있다.
            if (request == null || request.MoveKind != MotionGuardMoveKind.AxisTeachingMove)
                return false;

            if (request.Intent != null &&
                request.Intent.InspectionContinuous &&
                IsInspectionZHoldMove(request))
                return true;

            string fineAlignDetail;
            return MotionGuardRuleHelpers.IsColletCalibrationFineAlignMove(request, false, out fineAlignDetail);
        }

        #endregion

        #region Picker Y·T

        // 인터락 항목: RearPickerY 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyRearPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoRearPickerY(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualRearPickerY(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeRearPickerY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: 자동 RearPickerY 이동 전 수동 기본 조건과 Busy 상태를 확인한다.
        private static bool CanAutoRearPickerY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;

            // 현재 기준: Auto RearPickerY도 Manual RearPickerY 기본 인터락을 먼저 통과해야 한다.
            if (!CanManualRearPickerY(request, out reason))
                return false;

            // 기존 조건: Auto RearPickerY는 InspectionZHold/FineAlign이면 Z Home/Avoid 조건을 예외 처리했다.
            // 현재 필요 여부: 사용 안 함. Auto도 Manual 기본 인터락을 먼저 통과시키고, Auto 전용 예외는 별도 검토한다.
            //string fineAlignDetail;
            //if (!IsInspectionZHoldMove(request) &&
            //    !MotionGuardRuleHelpers.IsColletCalibrationFineAlignMove(request, false, out fineAlignDetail) &&
            //    !VerifyRearPickerZAxesHomeOrAvoid(machine != null ? machine.PickerRearUnit : null, "RearPickerY", out reason))
            //    return false;

            return VerifyRearPickerNotBusy(machine != null ? machine.PickerRearUnit : null, "RearPickerY", out reason);
        }

        // 인터락 항목: 수동 RearPickerY 이동 전 Z Home/Avoid, Reticle, OutputStageZ, PickerZone 조건을 확인한다.
        private static bool CanManualRearPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;

                // 현재 기준: 자동 검사 Z Hold/FineAlign 이동은 Z축을 유지해야 하므로 Home/Avoid 조건에서 제외한다.
                // PickUpZHold 면제(검증 FAIL E5 수정 2026-07-26): Conti 픽업 die 간 Y 이동(선보정/전진)도
                // 유지 픽커 Z만 위치 요구를 면제한다(비이동 요구는 Except 변형이 유지).
                // PlaceDoneSafeY 면제(사용자 승인 2026-07-27): Front 미러 — Z 위치 요구 면제,
                // Z Avoid 도착은 X 이동 전 join+복구가 보장.
                int yPickUpZHoldExempt;
                bool yHasPickUpZHold =
                    MotionGuardRuleHelpers.TryGetPickUpZHoldExemptPickerIndex(request, out yPickUpZHoldExempt);
                bool ySkipZRequirement =
                    CanKeepRearPickerZDuringYMove(request) ||
                    PickerFrontInterlockRules.IsPlaceDoneSafeYRetreatMove(request);
                if (!ySkipZRequirement &&
                    !yHasPickUpZHold &&
                    !VerifyRearPickerZAxesHomeOrAvoid(machine != null ? machine.PickerRearUnit : null, "RearPickerY", out reason))
                    return false;
                if (!ySkipZRequirement &&
                    yHasPickUpZHold &&
                    !VerifyRearPickerZAxesHomeOrAvoidExcept(machine != null ? machine.PickerRearUnit : null, "RearPickerY", yPickUpZHoldExempt, out reason))
                    return false;

                // 현재 기준: Reticle 실린더가 이동 중이면 RearPickerY 수동 이동을 차단한다.
                if (!VerifyReticleCylinderClear(machine, "RearPickerY", out reason))
                    return false;

                // 기존 조건: 조그 RearPickerY는 Z/Reticle 확인 후 목표 Zone 판정을 생략하고 바로 허용했다.
                // 현재 필요 여부: 목표 Zone 판정은 생략하지만 Front/Rear Y 돌출 + X 안전거리는 반드시 확인한다.
                //if (MotionGuardRuleHelpers.IsJogMove(request))
                //    return true;
                if (MotionGuardRuleHelpers.IsJogMove(request))
                    return PickerZoneInterlockRules.VerifyRearPickerYJogFacingMove(request, out reason);

                PickerWorkZone targetZone = ResolvePickerZTargetZone(request);
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                // 현재 기준: Output/Unknown 존으로 Y 이동 시 OutputStage GoodStageZ가 Avoid 또는 Process 위치여야 한다.
                if (RequiresOutputStageZSafeForPickerY(targetZone) &&
                    outputStage != null &&
                    !outputStage.IsGoodStageZInAvoidOrProcessPosition())
                {
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerY",
                        "RearPickerY 이동 불가: OutputStage GoodStageZ가 Avoid 또는 Process 위치가 아닙니다. pickerZone=" + targetZone + ".",
                        out reason);
                }

                // 현재 기준: AvoidPosition보다 작은 Y 목표는 Input 진입이며 InputExpandingZ가 0 이하 위치여야 한다.
                if (!PickerZoneInterlockRules.VerifyRearPickerYMove(request, out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerY",
                    "Exception occurred while verifying RearPickerY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: RearPickerT 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyRearPickerT(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoRearPickerT(request.Machine, request.MovingName, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualRearPickerT(request.Machine, request.MovingName, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeRearPickerT(request.Machine, request.MovingName, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: 자동 RearPickerT 이동 전 RearPicker Busy 상태를 확인한다.
        private static bool CanAutoRearPickerT(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                // 현재 기준: RearPickerT 자동 이동은 현재 RearPicker 다른 축 Busy 상태만 확인한다.
                return VerifyRearPickerNotBusy(machine != null ? machine.PickerRearUnit : null, movingName, out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " T축 이동 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: 수동 RearPickerT 이동 전 대응 Z축이 Avoid 위치인지 확인한다.
        private static bool CanManualRearPickerT(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                PickerAxis zAxis;
                // 방어 조건: T축 이름에서 대응 Z축을 찾지 못하면 T-Z 페어 조건을 적용하지 않는다.
                if (!TryResolvePairedZAxis(movingName, out zAxis))
                    return true;

                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
                // 현재 기준: RearPickerT 수동 이동 전 대응 RearPickerZ축은 Avoid 위치여야 한다.
                if (rear != null && !rear.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Rear" + zAxis + " must be at Avoid position.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying " + movingName + " home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        #endregion

        #region Picker X·Y·T Home

        // 인터락 항목: RearPickerX 홈 전 VisionX, ExpanderZ, 양쪽 PickerY, RearPickerZ 안전 위치를 확인한다.
        private static bool CanHomeRearPickerX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                string axisReason;
                // 현재 기준: RearPickerX Home 전 InputVisionX는 미홈 상태이거나 Home(0) 위치여야 한다.
                if (stage != null &&
                    !MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(stage.CameraX, "InputVisionX", out axisReason))
                {
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason,
                        out reason);
                }

                // 현재 기준: RearPickerX Home 전 InputExpandingZ는 Home(0), Avoid, Process, Ready 중 하나여야 한다.
                if (stage != null && !IsExpanderZHomeAvoidProcessOrReady(stage))
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. InputExpandingZ must be at Home(0), Avoid, Process or Ready position.",
                        out reason);

                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
                // 현재 기준: RearPickerX Home 전 FrontPickerY는 Home(0) 또는 Avoid 위치여야 한다.
                if (front != null &&
                    !IsAxisAtHomeOrTeachingAvoid(
                        ResolveFrontPickerAxis(front, PickerAxis.PickerY),
                        () => front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. FrontPickerY must be at Home(0) or Avoid position.",
                        out reason);

                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
                // 현재 기준: RearPickerX Home 전 RearPickerY는 Home(0) 또는 Avoid 위치여야 한다.
                if (rear != null &&
                    !IsAxisAtHomeOrTeachingAvoid(
                        ResolveRearPickerAxis(rear, PickerAxis.PickerY),
                        () => rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. RearPickerY must be at Home(0) or Avoid position.",
                        out reason);

                // 현재 기준: RearPickerX Home 전 RearPickerZ0~Z3는 모두 Home(0) 또는 Avoid 위치여야 한다.
                if (!VerifyRearPickerZAxesHomeOrAvoid(rear, "RearPickerX", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerX",
                    "Exception occurred while verifying RearPickerX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: RearPickerY 홈 전 RearPickerZ 전체가 Home 또는 Avoid 위치인지 확인한다.
        private static bool CanHomeRearPickerY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!PickerZoneInterlockRules.VerifyPickerYHomePairSafety(
                    machine,
                    false,
                    "RearPickerY",
                    out reason))
                    return false;

                // 현재 기준: RearPickerY Home 전 Z0~Z3는 Home(0) 또는 Avoid 위치여야 한다.
                if (!VerifyRearPickerZAxesHomeOrAvoid(machine != null ? machine.PickerRearUnit : null, "RearPickerY", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerY",
                    "Exception occurred while verifying RearPickerY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: RearPickerT 홈 전 대응 Z축이 Avoid 위치인지 확인한다.
        private static bool CanHomeRearPickerT(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                PickerAxis zAxis;
                // 방어 조건: T축 이름에서 대응 Z축을 찾지 못하면 Home T-Z 페어 조건을 적용하지 않는다.
                if (!TryResolvePairedZAxis(movingName, out zAxis))
                    return true;

                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
                // 현재 기준: RearPickerT Home 전 대응 RearPickerZ축은 Home(0) 또는 Avoid 위치여야 한다.
                if (rear != null &&
                    !IsAxisAtHomeOrTeachingAvoid(
                        ResolveRearPickerAxis(rear, zAxis),
                        () => rear.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Rear" + zAxis + " must be at Home(0) or Avoid position.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying " + movingName + " home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        #endregion

        #region Picker Z

        // 인터락 항목: RearPickerZ 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyRearPickerZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoRearPickerZ(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualRearPickerZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeRearPickerZ(request.Machine, request.MovingName, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: RearPickerZ 홈은 현재 별도 차단 조건 없이 허용한다.
        private static bool CanHomeRearPickerZ(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        // 인터락 항목: RearPickerZ 수동 이동 전 Reticle, InputExpanderZ, OutputGoodStageZ, Busy 조건을 확인한다.
        private static bool CanManualRearPickerZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            string movingName = request != null ? request.MovingName : "RearPickerZ";
            PickerWorkZone targetZone = ResolvePickerZTargetZone(request);

            // 현재 기준: RearPickerZ 수동 이동 전 Z Home 룰을 먼저 확인한다.
            if (!CanHomeRearPickerZ(machine, movingName, out reason))
                return false;

            // 현재 기준: PickerY가 Input/Output/공통 Avoid 위치이면 RearPickerZ 하강 이동을 차단한다.
            if (!VerifyRearPickerYAvoidBlocksZDown(request, out reason))
                return false;

            // 절대 인터락: HOME 외 모든 RearPickerZ 이동 전 Reticle은 Retract 상태여야 한다.
            if (!MotionGuardRuleHelpers.VerifyReticleRetractedBeforeAnyNonHomePickerZMove(request, out reason))
                return false;

            // 기존 작업 이동 Reticle 조건도 추가 조건으로 유지한다.
            if (!MotionGuardRuleHelpers.VerifyReticleRetractedBeforePickerZWorkMove(request, out reason))
                return false;

            // 기존 조건: PickerZ 이동 전 해당 존의 FeederY가 Avoid 위치여야 했다.
            // 현재 필요 여부: 사용 안 함. FeederY 이동 쪽에서 Picker가 Input/Output 존에 있으면 FeederY를 차단한다.
            //InputFeederUnit inputFeeder = machine != null ? machine.InputFeederUnit : null;
            //OutputFeederUnit outputFeeder = machine != null ? machine.OutputFeederUnit : null;
            //if (RequiresInputFeederAvoid(targetZone) && inputFeeder != null && !inputFeeder.IsWaferFeederYInAvoidPosition())
            //    return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: InputFeederY가 Avoid 위치가 아닙니다. pickerZone=" + targetZone + ".", out reason);
            //if (RequiresOutputFeederAvoid(targetZone) && outputFeeder != null && !outputFeeder.IsBinFeederYInAvoidPosition())
            //    return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: OutputFeederY가 Avoid 위치가 아닙니다. pickerZone=" + targetZone + ".", out reason);

            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            // 현재 기준: Input/Unknown 존에서 RearPickerZ 이동 전 InputExpandingZ는 0 이하 또는 Avoid 위치여야 한다.
            if (RequiresInputStageZSafe(targetZone) &&
                !VerifyInputExpanderZAtOrBelowZero(stage, movingName, out reason))
                return false;

            OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
            // 현재 기준: Output/Unknown 존에서 RearPickerZ 이동 전 OutputGoodStageZ는 ProcessPos 이하 위치여야 한다.
            if (RequiresOutputStageZSafeForPickerY(targetZone) &&
                !VerifyGoodStageZAtOrBelowProcess(outputStage, movingName, out reason))
                return false;

            return VerifyRearPickerNotBusy(machine != null ? machine.PickerRearUnit : null, movingName, out reason);
        }

        // 인터락 항목: 자동 RearPickerZ 이동은 수동 RearPickerZ 조건과 동일하게 확인한다.
        private static bool CanAutoRearPickerZ(MotionGuardRuleContext request, out string reason)
        {
            // 현재 기준: Auto RearPickerZ도 Manual RearPickerZ 기본 인터락과 동일하게 확인한다.
            return CanManualRearPickerZ(request, out reason);
        }

        // 인터락 항목: RearPickerY가 Avoid 계열 위치일 때 RearPickerZ 하강 명령을 차단한다.
        private static bool VerifyRearPickerYAvoidBlocksZDown(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            // 방어 조건: 요청 또는 RearPicker 참조가 없으면 Y-Z Avoid 연동 조건을 적용하지 않는다.
            PickerRearUnit picker = request != null && request.Machine != null ? request.Machine.PickerRearUnit : null;
            if (picker == null)
                return true;

            PickerAxis zAxis;
            // 방어 조건: 이동 Z축을 해석하지 못하면 Y-Z Avoid 연동 조건을 적용하지 않는다.
            if (!TryResolveMovingZAxis(request.MovingName, out zAxis))
                return true;

            BaseAxis zItem = ResolveRearPickerAxis(picker, zAxis);
            if (zItem == null)
                return true;

            // 현재 기준: Home/0 또는 Z Avoid 위치로 가는 복귀 목표는 하강 차단 대상에서 제외한다.
            if (IsRearPickerZSafeRetreatTarget(request, picker, zAxis, zItem))
                return true;

            // 현재 기준: 실제 Z 하강 목표가 아니면 차단하지 않는다.
            if (!IsZTargetDown(request.TargetValue, zItem))
                return true;

            string yAvoidName = ResolveRearPickerYAvoidPositionName(picker);
            if (string.IsNullOrWhiteSpace(yAvoidName))
                return true;

            double yActual = picker.PickerY != null ? picker.PickerY.ActualPosition : 0.0;
            return MotionGuardRuleHelpers.Block(
                request.MovingName,
                request.MovingName + " 이동 불가: RearPickerY가 " + yAvoidName +
                " 위치일 때 PickerZ 하강 이동은 금지됩니다. " +
                "pickerY=" + yActual.ToString("F3") +
                ", zActual=" + zItem.ActualPosition.ToString("F3") +
                ", zTarget=" + request.TargetValue.ToString("F3") +
                ", targetName=" + (string.IsNullOrWhiteSpace(request.TargetName) ? "-" : request.TargetName),
                out reason);
        }

        // 인터락 기준: RearPickerZ 목표가 Home/0 또는 Z Avoid 위치 복귀 목표인지 판단한다.
        private static bool IsRearPickerZSafeRetreatTarget(MotionGuardRuleContext request, PickerRearUnit picker, PickerAxis zAxis, BaseAxis zItem)
        {
            if (request == null)
                return true;

            double tolerance = ResolveAxisTolerance(zItem);
            double target = request.TargetValue;

            // 현재 기준: 목표가 0 근처이면 Home 복귀 목표로 본다.
            if (System.Math.Abs(target) <= tolerance)
                return true;

            // 현재 기준: 목표가 해당 Z축 Avoid 티칭 위치이면 복귀 목표로 본다.
            return System.Math.Abs(target - picker.GetPickerTeachingPosition(zAxis, "AvoidPosition")) <= tolerance;
        }

        // 인터락 기준: RearPickerY가 InputAvoid/OutputAvoid/Avoid 중 어느 위치에 있는지 반환한다.
        private static string ResolveRearPickerYAvoidPositionName(PickerRearUnit picker)
        {
            if (picker == null)
                return string.Empty;

            // 현재 기준: InputAvoidPosition도 Z 하강 금지 Y 위치로 본다.
            if (picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "InputAvoidPosition"))
                return "InputAvoidPosition";

            // 현재 기준: OutputAvoidPosition도 Z 하강 금지 Y 위치로 본다.
            if (picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "OutputAvoidPosition"))
                return "OutputAvoidPosition";

            // 현재 기준: 공통 AvoidPosition도 Z 하강 금지 Y 위치로 본다.
            if (picker.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                return "AvoidPosition";

            return string.Empty;
        }

        // 인터락 기준: Z축 목표가 현재 위치보다 아래 방향인지 판단한다.
        private static bool IsZTargetDown(double target, BaseAxis zItem)
        {
            if (zItem == null)
                return false;

            // 현재 기준: 1um 단위 하강도 차단해야 하므로 InPositionTolerance 대신 최소 오차만 사용한다.
            return target < zItem.ActualPosition - 0.000001;
        }

        // 인터락 기준: PickerZ 이동 요청의 목표 작업 존을 해석한다.
        private static PickerWorkZone ResolvePickerZTargetZone(MotionGuardRuleContext request)
        {
            // 현재 기준: 요청 Intent에 PickerZone이 있으면 Z축 목표 존으로 사용한다.
            if (request != null && request.Intent != null)
                return request.Intent.PickerZone;

            return PickerWorkZone.Unknown;
        }

        // 인터락 기준: 목표 존에 InputFeeder Avoid 조건이 필요한지 판단한다.
        private static bool RequiresInputFeederAvoid(PickerWorkZone targetZone)
        {
            // 현재 기준: Input 또는 Unknown 존이면 InputFeederY Avoid 조건을 적용한다.
            return targetZone == PickerWorkZone.Input ||
                   targetZone == PickerWorkZone.Unknown;
        }

        // 인터락 기준: 목표 존에 OutputFeeder Avoid 조건이 필요한지 판단한다.
        private static bool RequiresOutputFeederAvoid(PickerWorkZone targetZone)
        {
            // 현재 기준: Output 또는 Unknown 존이면 OutputFeederY Avoid 조건을 적용한다.
            return targetZone == PickerWorkZone.Output ||
                   targetZone == PickerWorkZone.Unknown;
        }

        // 인터락 기준: 목표 존에 InputStageZ 안전 조건이 필요한지 판단한다.
        private static bool RequiresInputStageZSafe(PickerWorkZone targetZone)
        {
            // 현재 기준: Input 또는 Unknown 존이면 InputExpandingZ 안전 위치 조건을 적용한다.
            return targetZone == PickerWorkZone.Input ||
                   targetZone == PickerWorkZone.Unknown;
        }

        // 인터락 기준: 목표 존에 OutputStageZ 안전 조건이 필요한지 판단한다.
        private static bool RequiresOutputStageZSafeForPickerY(PickerWorkZone targetZone)
        {
            // 현재 기준: Output 또는 Unknown 존이면 OutputStage GoodStageZ 안전 위치 조건을 적용한다.
            return targetZone == PickerWorkZone.Output ||
                   targetZone == PickerWorkZone.Unknown;
        }

        // 인터락 기준: 목표명 문자열에 특정 존/위치 키워드가 포함되어 있는지 판단한다.
        private static bool Contains(string value, string pattern)
        {
            return (value ?? string.Empty).IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region 공통 기구 및 축 상태

        // 인터락 항목: Picker 이동 전 Reticle 관련 실린더가 이동 중인지 확인한다.
        private static bool VerifyReticleCylinderClear(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: VisionUnit 참조가 없으면 Reticle 실린더 이동 조건을 적용하지 않는다.
            if (machine == null || machine.VisionUnit == null)
                return true;

            // 현재 기준: ReticleLift가 이동 중이면 Picker 이동을 차단한다.
            if (IsCylinderMoving(machine.VisionUnit.ReticleLift))
                return MotionGuardRuleHelpers.Block(movingName, "ReticleLift is moving.", out reason);
            // 현재 기준: Reticle Front Side Slide가 이동 중이면 Picker 이동을 차단한다.
            if (IsCylinderMoving(machine.VisionUnit.ReticleFrontSideSlide))
                return MotionGuardRuleHelpers.Block(movingName, "ReticleSideSlideFront is moving.", out reason);
            // 현재 기준: Reticle Rear Side Slide가 이동 중이면 Picker 이동을 차단한다.
            if (IsCylinderMoving(machine.VisionUnit.ReticleRearSideSlide))
                return MotionGuardRuleHelpers.Block(movingName, "ReticleSideSlideRear is moving.", out reason);

            return true;
        }

        // 인터락 기준: 실린더가 현재 이동 중인지 판단한다.
        private static bool IsCylinderMoving(BaseCylinder cylinder)
        {
            return MotionGuardRuleHelpers.IsCylinderMoving(cylinder);
        }

        // 인터락 항목: RearPicker 내부 다른 축 Busy 상태를 확인한다.
        private static bool VerifyRearPickerNotBusy(PickerRearUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: RearPicker 참조가 없으면 Busy 조건을 적용하지 않는다.
            if (picker == null)
                return true;

            //if (IsMovingExcept(picker.PickerX, movingName, "RearPickerX"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerX is moving.", out reason);
            //if (IsMovingExcept(picker.PickerY, movingName, "RearPickerY"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerY is moving.", out reason);
            //if (IsMovingExcept(picker.PickerT0, movingName, "RearPickerT0"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerT0 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerT1, movingName, "RearPickerT1"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerT1 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerT2, movingName, "RearPickerT2"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerT2 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerT3, movingName, "RearPickerT3"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerT3 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerZ0, movingName, "RearPickerZ0"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerZ0 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerZ1, movingName, "RearPickerZ1"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerZ1 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerZ2, movingName, "RearPickerZ2"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerZ2 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerZ3, movingName, "RearPickerZ3"))
            //    return MotionGuardRuleHelpers.Block(movingName, "RearPickerZ3 is moving.", out reason);

            return true;
        }

        // 인터락 기준: 현재 명령 축을 제외한 축이 이동 중인지 판단한다.
        private static bool IsMovingExcept(BaseAxis axis, string movingName, params string[] names)
        {
            // 현재 기준: 축이 이동 중이 아니면 Busy 차단 대상이 아니다.
            if (!MotionGuardRuleHelpers.IsAxisMoving(axis))
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                // 현재 기준: 현재 명령 축과 같은 축이면 자기 자신 이동으로 보고 Busy 차단하지 않는다.
                if (string.Equals(movingName, names[i], System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        // 인터락 항목: RearPickerZ 전체가 Avoid 위치인지 확인한다.
        private static bool VerifyRearPickerZAxesAvoid(PickerRearUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: RearPicker 참조가 없으면 Z Avoid 조건을 적용하지 않는다.
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                // 현재 기준: RearPickerZ0~Z3 중 하나라도 Avoid 위치가 아니면 이동을 차단한다.
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Rear" + zAxis + " must be at Avoid position.",
                        out reason);
            }

            return true;
        }

        // 인터락 항목: RearPickerZ 전체가 Home 또는 Avoid 위치인지 확인한다.
        // PickUpZHold 면제 변형(정정 2026-07-26): exemptIndex 픽커 Z는 검사에서 제외
        // (Avoid 상승 중 이동 허용). 나머지 픽커 Z는 기존 Home/Avoid 요구 그대로.
        private static bool VerifyRearPickerZAxesHomeOrAvoidExcept(PickerRearUnit picker, string movingName, int exemptIndex, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = ResolveRearPickerAxis(picker, zAxis);
                // PickUpZHold 면제(정정 2026-07-26): Avoid 상승 중 이동 허용 — 검사 제외.
                if (i == exemptIndex)
                    continue;

                if (!IsAxisAtHomeOrTeachingAvoid(axis, () => picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Rear" + zAxis + " must be at Home(0) or Avoid position.",
                        out reason);
            }

            return true;
        }

        private static bool VerifyRearPickerZAxesHomeOrAvoid(PickerRearUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: RearPicker 참조가 없으면 Z Home/Avoid 조건을 적용하지 않는다.
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = ResolveRearPickerAxis(picker, zAxis);
                // 현재 기준: RearPickerZ0~Z3는 Home(0) 또는 Avoid 위치여야 한다.
                if (!IsAxisAtHomeOrTeachingAvoid(axis, () => picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Rear" + zAxis + " must be at Home(0) or Avoid position.",
                        out reason);
            }

            return true;
        }

        // 인터락 기준: 축이 Home 위치이거나 티칭 Avoid 위치인지 판단한다.
        private static bool IsAxisAtHomeOrTeachingAvoid(BaseAxis axis, System.Func<bool> isTeachingAvoid)
        {
            // 방어 조건: 축 참조가 없으면 Home/Avoid 확인을 통과시킨다.
            if (axis == null)
                return true;

            double tolerance = ResolveAxisTolerance(axis);

            // 현재 기준: ActualPosition이 0 근처이면 Home 위치로 본다.
            if (System.Math.Abs(axis.ActualPosition) <= tolerance)
                return true;

            // 현재 기준: Home이 아니면 티칭 Avoid 위치 여부로 안전 위치를 판단한다.
            return isTeachingAvoid != null && isTeachingAvoid();
        }

        // 인터락 기준: 축이 0 이상 위치인지 tolerance를 포함해 판단한다.
        private static bool IsAxisAtOrAboveZero(BaseAxis axis)
        {
            // 방어 조건: 축 참조가 없으면 0 이상 조건을 만족하지 않은 것으로 본다.
            if (axis == null)
                return false;

            // 현재 기준: ActualPosition이 0 이상이거나 tolerance 안쪽이면 0 이상 위치로 본다.
            return axis.ActualPosition >= -ResolveAxisTolerance(axis);
        }

        // 인터락 기준: 위치 비교에 사용할 축별 tolerance 값을 결정한다.
        private static double ResolveAxisTolerance(BaseAxis axis)
        {
            // 현재 기준: 축 InPositionTolerance가 있으면 그 값을 위치 비교 tolerance로 사용한다.
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return 0.05;
        }

        // 인터락 기준: ExpanderZ가 Home/Avoid/Process/Ready 중 안전 위치인지 판단한다.
        private static bool IsExpanderZHomeAvoidProcessOrReady(InputStageUnit stage)
        {
            // 방어 조건: InputStage 또는 ExpanderZ 참조가 없으면 ExpanderZ 조건을 적용하지 않는다.
            if (stage == null || stage.ExpanderZ == null)
                return true;

            double tolerance = stage.ExpanderZ.Config != null && stage.ExpanderZ.Config.InPositionTolerance > 0.0
                ? stage.ExpanderZ.Config.InPositionTolerance
                : 0.05;

            double actual = stage.ExpanderZ.ActualPosition;
            // 현재 기준: ExpanderZ actual이 0 근처이면 Home 위치로 본다.
            if (System.Math.Abs(actual) <= tolerance)
                return true;

            StageAxisPositions waferZ = stage.Recipe != null ? stage.Recipe.WaferZ : null;
            // 방어 조건: WaferZ 레시피가 없으면 Avoid/Process/Ready 위치를 판단할 수 없어 차단한다.
            if (waferZ == null)
                return false;

            // 현재 기준: ExpanderZ는 Avoid, Process, Ready 중 하나에 있으면 안전 위치로 본다.
            return System.Math.Abs(actual - waferZ.AvoidPosition) <= tolerance ||
                   System.Math.Abs(actual - waferZ.ProcessPosition) <= tolerance ||
                   System.Math.Abs(actual - waferZ.ReadyPosition) <= tolerance;
        }

        // RearPickerX 이동 전제: ExpanderZ가 Avoid/Process/Ready 위치여야 한다. (Home(0)은 제외)
        // 인터락 기준: ExpanderZ가 RearPicker 평면 이동 가능한 Avoid/Process/Ready 위치인지 판단한다.
        private static bool IsExpanderZAvoidProcessOrReady(InputStageUnit stage)
        {
            // 방어 조건: InputStage 또는 ExpanderZ 참조가 없으면 ExpanderZ 조건을 적용하지 않는다.
            if (stage == null || stage.ExpanderZ == null)
                return true;

            double tolerance = stage.ExpanderZ.Config != null && stage.ExpanderZ.Config.InPositionTolerance > 0.0
                ? stage.ExpanderZ.Config.InPositionTolerance
                : 0.05;

            StageAxisPositions waferZ = stage.Recipe != null ? stage.Recipe.WaferZ : null;
            // 방어 조건: WaferZ 레시피가 없으면 Avoid/Process/Ready 위치를 판단할 수 없어 차단한다.
            if (waferZ == null)
                return false;

            double actual = stage.ExpanderZ.ActualPosition;
            // 현재 기준: ExpanderZ는 Avoid, Process, Ready 중 하나에 있으면 안전 위치로 본다.
            return System.Math.Abs(actual - waferZ.AvoidPosition) <= tolerance ||
                   System.Math.Abs(actual - waferZ.ProcessPosition) <= tolerance ||
                   System.Math.Abs(actual - waferZ.ReadyPosition) <= tolerance;
        }

        // 인터락 기준: FrontPicker 논리 축을 실제 Axis 객체로 변환한다.
        private static BaseAxis ResolveFrontPickerAxis(PickerFrontUnit picker, PickerAxis axis)
        {
            // 방어 조건: FrontPicker 참조가 없으면 축을 해석하지 않는다.
            if (picker == null)
                return null;

            switch (axis)
            {
                case PickerAxis.PickerZ0: return picker.PickerZ0;
                case PickerAxis.PickerZ1: return picker.PickerZ1;
                case PickerAxis.PickerZ2: return picker.PickerZ2;
                case PickerAxis.PickerZ3: return picker.PickerZ3;
                case PickerAxis.PickerX: return picker.PickerX;
                case PickerAxis.PickerY: return picker.PickerY;
                case PickerAxis.PickerT0: return picker.PickerT0;
                case PickerAxis.PickerT1: return picker.PickerT1;
                case PickerAxis.PickerT2: return picker.PickerT2;
                case PickerAxis.PickerT3: return picker.PickerT3;
                default: return null;
            }
        }

        // 인터락 기준: RearPicker 논리 축을 실제 Axis 객체로 변환한다.
        private static BaseAxis ResolveRearPickerAxis(PickerRearUnit picker, PickerAxis axis)
        {
            // 방어 조건: RearPicker 참조가 없으면 축을 해석하지 않는다.
            if (picker == null)
                return null;

            switch (axis)
            {
                case PickerAxis.PickerZ0: return picker.PickerZ0;
                case PickerAxis.PickerZ1: return picker.PickerZ1;
                case PickerAxis.PickerZ2: return picker.PickerZ2;
                case PickerAxis.PickerZ3: return picker.PickerZ3;
                case PickerAxis.PickerX: return picker.PickerX;
                case PickerAxis.PickerY: return picker.PickerY;
                case PickerAxis.PickerT0: return picker.PickerT0;
                case PickerAxis.PickerT1: return picker.PickerT1;
                case PickerAxis.PickerT2: return picker.PickerT2;
                case PickerAxis.PickerT3: return picker.PickerT3;
                default: return null;
            }
        }

        // 인터락 기준: RearPickerT 축명에 대응되는 Z축을 해석한다.
        private static bool TryResolvePairedZAxis(string movingName, out PickerAxis zAxis)
        {
            zAxis = PickerAxis.PickerZ0;

            switch (movingName)
            {
                // 리어 피커 T0축 처리
                case "RearPickerT0":
                    zAxis = PickerAxis.PickerZ0;
                    return true;
                // 리어 피커 T1축 처리
                case "RearPickerT1":
                    zAxis = PickerAxis.PickerZ1;
                    return true;
                // 리어 피커 T2축 처리
                case "RearPickerT2":
                    zAxis = PickerAxis.PickerZ2;
                    return true;
                // 리어 피커 T3축 처리
                case "RearPickerT3":
                    zAxis = PickerAxis.PickerZ3;
                    return true;
                default:
                    return false;
            }
        }

        // 인터락 기준: RearPickerZ 축명에서 이동 대상 Z축을 해석한다.
        private static bool TryResolveMovingZAxis(string movingName, out PickerAxis zAxis)
        {
            zAxis = PickerAxis.PickerZ0;

            switch (movingName)
            {
                // 리어 피커 Z0축 처리
                case "RearPickerZ0":
                    zAxis = PickerAxis.PickerZ0;
                    return true;
                // 리어 피커 Z1축 처리
                case "RearPickerZ1":
                    zAxis = PickerAxis.PickerZ1;
                    return true;
                // 리어 피커 Z2축 처리
                case "RearPickerZ2":
                    zAxis = PickerAxis.PickerZ2;
                    return true;
                // 리어 피커 Z3축 처리
                case "RearPickerZ3":
                    zAxis = PickerAxis.PickerZ3;
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region 차단 로그

        // 인터락 기준: RearPicker 인터락 차단 사유를 로그에 기록한다.
        private static void LogBlockedReason(string reason)
        {
            try
            {
                // 현재 기준: 차단 사유가 있으면 RearPicker 인터락 로그로 남긴다.
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "PickerRearInterlock", reason + " - Blocked");
            }
            catch
            {
            }
            finally
            {
            }
        }

        #endregion
    }
}
