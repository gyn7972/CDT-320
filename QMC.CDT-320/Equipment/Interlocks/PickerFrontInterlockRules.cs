using QMC.Common.IO;
using QMC.Common.Motion;
using System;

namespace QMC.CDT320.Interlocks
{
    public static class PickerFrontInterlockRules
    {
        // 인터락 항목: FrontPicker X/Y/T/Z 이동 요청을 해당 축별 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: 요청 또는 장비 참조가 없으면 FrontPicker 인터락을 적용하지 않는다.
            if (request == null || request.Machine == null)
                return true;

            // 현재 기준: 이동 축이 FrontPickerX이면 X축 인터락 규칙으로 분기한다.
            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerX"))
                return VerifyFrontPickerX(request, out reason);

            // 현재 기준: 이동 축이 FrontPickerY이면 Y축 인터락 규칙으로 분기한다.
            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerY"))
                return VerifyFrontPickerY(request, out reason);

            // 현재 기준: 이동 축이 FrontPickerT0~T3이면 T축 인터락 규칙으로 분기한다.
            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerT0", "FrontPickerT1", "FrontPickerT2", "FrontPickerT3"))
                return VerifyFrontPickerT(request, out reason);

            // 현재 기준: 이동 축이 FrontPickerZ0~Z3이면 Z축 인터락 규칙으로 분기한다.
            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3"))
                return VerifyFrontPickerZ(request, out reason);

            return true;
        }

        // 인터락 항목: FrontPickerX 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyFrontPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            if (!VerifyFeederAndCassetteAvoidForFrontPickerX(
                request != null ? request.Machine : null,
                out reason))
            {
                return false;
            }

            if (!PickerZoneInterlockRules.VerifyPickerXGlobalMachineClearance(
                request != null ? request.Machine : null,
                "FrontPickerX",
                out reason))
                return false;

            // 인터락 항목: FrontPickerX 조그는 Z 상승 조건을 확인한 뒤 목표 Zone 판정만 생략한다.
            if (MotionGuardRuleHelpers.IsJogMove(request))
                return CanJogFrontPickerX(request, out reason);

            // 인터락 항목(사용자 승인 2026-07-25): 구동 중 위치 오버라이드(팔로잉 중간 세그먼트)는
            // 조그와 같은 방식으로 목표 Zone 판정만 생략하고 Z 상승/Reticle/Busy는 유지한다.
            // Y 대향 거리와 SharedRailX 페어 간격은 이 룰 밖(레지스트리 선행 룰)에서 계속 평가된다.
            if (MotionGuardRuleHelpers.IsPositionOverrideStep(request))
                return CanPositionOverrideFrontPickerX(request, out reason);

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoFrontPickerX(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualFrontPickerX(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontPickerX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 카세트 피더 관련 인터락. 이거 무조건이다. 
        private static bool VerifyFeederAndCassetteAvoidForFrontPickerX(
        CDT320_Machine machine,
        out string reason)
        {
            reason = string.Empty;

            if (machine == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 불가: Machine 참조가 없습니다.",
                    out reason);
            }

            InputFeederUnit inputFeeder = machine.InputFeederUnit;
            if (inputFeeder == null || inputFeeder.FeederY == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 불가: InputFeederY 상태를 확인할 수 없습니다.",
                    out reason);
            }

            if (inputFeeder.FeederY.IsMoving ||
                !inputFeeder.IsWaferFeederAvoidPositionCheck())
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 불가: InputFeeder가 정지된 Avoid 위치가 아닙니다.",
                    out reason);
            }

            OutputFeederUnit outputFeeder = machine.OutputFeederUnit;
            if (outputFeeder == null || outputFeeder.FeederY == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 불가: OutputFeederY 상태를 확인할 수 없습니다.",
                    out reason);
            }

            if (outputFeeder.FeederY.IsMoving ||
                !outputFeeder.IsBinFeederAvoidPositionCheck())
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 불가: OutputFeeder가 정지된 Avoid 위치가 아닙니다.",
                    out reason);
            }

            InputCassetteUnit inputCassette = machine.InputCassetteUnit;
            if (inputCassette == null || inputCassette.InputLifterZ == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 불가: InputCassette 상태를 확인할 수 없습니다.",
                    out reason);
            }

            if (inputCassette.InputLifterZ.IsMoving ||
                !inputCassette.IsWaferLifterZInAvoidPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 불가: InputCassette가 정지된 Avoid 위치가 아닙니다.",
                    out reason);
            }

            OutputCassetteUnit outputCassette = machine.OutputCassetteUnit;
            if (outputCassette == null || outputCassette.OutputLifterZ == null)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 불가: OutputCassette 상태를 확인할 수 없습니다.",
                    out reason);
            }

            if (outputCassette.OutputLifterZ.IsMoving ||
                !outputCassette.IsBinLifterZInAvoidPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 불가: OutputCassette가 정지된 Avoid 위치가 아닙니다.",
                    out reason);
            }

            return true;
        }

        // 인터락 항목: 조그 FrontPickerX는 Z Home/Avoid, Reticle, Busy 조건을 유지하고 Zone 판정만 생략한다.
        private static bool CanJogFrontPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;

                // 현재 기준: FrontPickerX 조그 전 Z0~Z3는 모두 상승(Home 또는 Avoid) 상태여야 한다.
                if (!VerifyFrontPickerZAxesHomeOrAvoid(front, "FrontPickerX", out reason))
                    return false;

                // 현재 기준: FrontPickerX 조그 전 Reticle 관련 실린더가 이동 중이면 차단한다.
                if (!VerifyReticleCylinderClear(machine, "FrontPickerX", out reason))
                    return false;

                return VerifyFrontPickerNotBusy(front, "FrontPickerX", out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX Jog 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: 자동 FrontPickerX 이동 전 수동 기본 조건, Z Avoid, VisionX Avoid, Busy 상태를 확인한다.
        // 인터락 항목: 구동 중 위치 오버라이드 FrontPickerX는 Z 상승, Reticle, Busy 조건을 유지하고 Zone 판정만 생략한다.
        // 생략 근거: 팔로잉 중간 세그먼트 좌표는 어떤 티칭 존에도 속하지 않아 목표 존이 Unknown이 되고,
        //   최종 목표에 대한 존 진입 조건(Z Avoid/ExpandingZ/Feeder Dog/Feeder Down/VisionX Avoid/상대 Y)은
        //   팔로잉 최초 명령(AxisMove 경로)에서 이미 1회 검증된다. 이동 중 실제 안전은 Y 대향 거리
        //   (VerifyFacingYDistanceFirst), SharedRailX 페어 간격, 소프트리밋, 실시간 충돌 감시가 담당한다.
        private static bool CanPositionOverrideFrontPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;

                // 현재 기준: 오버라이드 중에도 FrontPickerZ0~Z3는 모두 상승(Home 또는 Avoid) 상태여야 한다.
                if (!VerifyFrontPickerZAxesHomeOrAvoid(front, "FrontPickerX", out reason))
                    return false;

                // 현재 기준: Reticle 관련 실린더가 이동 중이면 차단한다.
                if (!VerifyReticleCylinderClear(machine, "FrontPickerX", out reason))
                    return false;

                return VerifyFrontPickerNotBusy(front, "FrontPickerX", out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 위치 오버라이드 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanAutoFrontPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;

            try
            {
                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;

                // 현재 기준: Auto FrontPickerX도 Manual FrontPickerX 기본 인터락을 먼저 통과해야 한다.
                if (!CanManualFrontPickerX(request, out reason))
                    return false;

                // 현재 기준: FrontPickerX 자동 이동 전 Z0~Z3가 Avoid 위치여야 한다.
                if (!VerifyFrontPickerZAxesAvoidForMove(front, "FrontPickerX", request, out reason))
                    return false;

                // 현재 기준: Collet Calibration Bottom 이동이면 Input/Output VisionX가 Avoid 위치여야 한다.
                if (!VerifyVisionXAvoidForColletCalibrationBottomMove(machine, "FrontPickerX", request, out reason))
                    return false;
                
                // 기존 조건: Auto FrontPickerX에서 PickerZone 룰을 직접 확인했다.
                // 현재 필요 여부: Manual FrontPickerX 기본 인터락에서 먼저 확인하므로 중복 호출하지 않는다.
                //if (!PickerZoneInterlockRules.VerifyFrontPickerXMove(request, out reason))
                //    return false;

                return VerifyFrontPickerNotBusy(front, "FrontPickerX", out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "FrontPickerX 이동 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
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

        // 인터락 항목: FrontPicker 평면 이동 전 Z축 전체 Avoid 여부를 확인하되 검사/보정 예외를 반영한다.
        private static bool VerifyFrontPickerZAxesAvoidForMove(PickerFrontUnit picker, string movingName, MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: FrontPicker 참조가 없으면 Z Avoid 조건을 적용하지 않는다.
            if (picker == null)
                return true;

            // 현재 기준: Inspection Z Hold 이동은 Z축을 유지해야 하므로 Z Avoid 조건에서 제외한다.
            if (IsInspectionZHoldMove(request))
                return true;

            string fineAlignDetail;
            // 현재 기준: Collet Calibration Fine Align 이동은 Z축을 유지해야 하므로 Z Avoid 조건에서 제외한다.
            if (MotionGuardRuleHelpers.IsColletCalibrationFineAlignMove(request, true, out fineAlignDetail))
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                // 현재 기준: FrontPicker 평면 이동 전 Z0~Z3는 모두 Avoid 위치여야 한다.
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: Front" + zAxis + " 축이 Avoid 위치가 아닙니다.",
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

        // 인터락 기준: FrontPickerY 이동 중 PickerZ 유지 예외를 허용할지 판단한다.
        private static bool CanKeepFrontPickerZDuringYMove(MotionGuardRuleContext request)
        {
            // 현재 기준: 자동 티칭 이동에서만 Z Hold/FineAlign 예외를 적용한다.
            if (request == null || request.MoveKind != MotionGuardMoveKind.AxisTeachingMove)
                return false;

            if (IsInspectionZHoldMove(request))
                return true;

            string fineAlignDetail;
            return MotionGuardRuleHelpers.IsColletCalibrationFineAlignMove(request, true, out fineAlignDetail);
        }

        // 인터락 기준: FrontPickerX 이동 중 PickerZ 유지 예외를 허용할지 판단한다.
        private static bool CanKeepFrontPickerZDuringXMove(MotionGuardRuleContext request)
        {
            // 현재 기준: Auto Bottom/Side 검사 연속 X 이동은 PickerZ가 검사 높이를 유지할 수 있다.
            if (request == null || request.MoveKind != MotionGuardMoveKind.AxisTeachingMove)
                return false;

            if (request.Intent != null &&
                request.Intent.InspectionContinuous &&
                IsInspectionZHoldMove(request))
                return true;

            string fineAlignDetail;
            return MotionGuardRuleHelpers.IsColletCalibrationFineAlignMove(request, true, out fineAlignDetail);
        }

        // 인터락 항목: FrontPickerY 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyFrontPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoFrontPickerY(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualFrontPickerY(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontPickerY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: 자동 FrontPickerY 이동 전 수동 기본 조건과 Busy 상태를 확인한다.
        private static bool CanAutoFrontPickerY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;

            // 현재 기준: Auto FrontPickerY도 Manual FrontPickerY 기본 인터락을 먼저 통과해야 한다.
            if (!CanManualFrontPickerY(request, out reason))
                return false;

            // 기존 조건: Auto FrontPickerY는 InspectionZHold/FineAlign이면 Z Home/Avoid 조건을 예외 처리했다.
            // 현재 필요 여부: 사용 안 함. Auto도 Manual 기본 인터락을 먼저 통과시키고, Auto 전용 예외는 별도 검토한다.
            //string fineAlignDetail;
            //if (!IsInspectionZHoldMove(request) &&
            //    !MotionGuardRuleHelpers.IsColletCalibrationFineAlignMove(request, true, out fineAlignDetail) &&
            //    !VerifyFrontPickerZAxesHomeOrAvoid(machine != null ? machine.PickerFrontUnit : null, "FrontPickerY", out reason))
            //    return false;

            return VerifyFrontPickerNotBusy(machine != null ? machine.PickerFrontUnit : null, "FrontPickerY", out reason);
        }

        // 인터락 항목: 수동 FrontPickerY 이동 전 Z Home/Avoid, Reticle, PickerZone, OutputStageZ 조건을 확인한다.
        private static bool CanManualFrontPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;

                // 현재 기준: 자동 검사 Z Hold/FineAlign 이동은 Z축을 유지해야 하므로 Home/Avoid 조건에서 제외한다.
                if (!CanKeepFrontPickerZDuringYMove(request) &&
                    !VerifyFrontPickerZAxesHomeOrAvoid(machine != null ? machine.PickerFrontUnit : null, "FrontPickerY", out reason))
                    return false;

                // 현재 기준: Reticle 실린더가 이동 중이면 FrontPickerY 수동 이동을 차단한다.
                if (!VerifyReticleCylinderClear(machine, "FrontPickerY", out reason))
                    return false;

                // 기존 조건: 조그 FrontPickerY는 Z/Reticle 확인 후 목표 Zone 판정을 생략하고 바로 허용했다.
                // 현재 필요 여부: 목표 Zone 판정은 생략하지만 Front/Rear Y 돌출 + X 안전거리는 반드시 확인한다.
                //if (MotionGuardRuleHelpers.IsJogMove(request))
                //    return true;
                if (MotionGuardRuleHelpers.IsJogMove(request))
                    return PickerZoneInterlockRules.VerifyFrontPickerYJogFacingMove(request, out reason);

                // 현재 기준: AvoidPosition보다 작은 Y 목표는 Input 진입이며 InputExpandingZ가 0 이하 위치여야 한다.
                if (!PickerZoneInterlockRules.VerifyFrontPickerYMove(request, out reason))
                    return false;

                // 기존 조건: InputExpandingZ가 Avoid/Process/Ready 위치여야 FrontPickerY 이동 가능했다.
                // 현재 필요 여부: 사용 안 함. Input 진입 여부와 InputExpandingZ <= 0 기준은 PickerZoneInterlockRules에서 확인한다.
                //InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                //if (stage != null && !IsExpanderZAvoidProcessOrReady(stage))
                //    return MotionGuardRuleHelpers.Block(
                //        "FrontPickerY",
                //        "FrontPickerY 이동 불가: InputExpandingZ가 Avoid/Process/Ready 위치가 아닙니다.",
                //        out reason);

                PickerWorkZone targetZone = ResolvePickerZTargetZone(request);
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                // 현재 기준: FrontPickerY 목표가 Output/Unknown 존일 때만 OutputStage GoodStageZ는 Avoid 또는 Process 위치여야 한다.
                if (RequiresOutputStageZSafeForPickerY(targetZone) &&
                    outputStage != null &&
                    !outputStage.IsGoodStageZInAvoidOrProcessPosition())
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerY",
                        "FrontPickerY 이동 불가: OutputStage GoodStageZ가 Avoid 또는 Process 위치가 아닙니다. pickerZone=" + targetZone + ".",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerY",
                    "Exception occurred while verifying FrontPickerY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: FrontPickerT 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyFrontPickerT(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoFrontPickerT(request.Machine, request.MovingName, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualFrontPickerT(request.Machine, request.MovingName, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontPickerT(request.Machine, request.MovingName, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: 자동 FrontPickerT 이동 전 FrontPicker Busy 상태를 확인한다.
        private static bool CanAutoFrontPickerT(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                // 현재 기준: FrontPickerT 자동 이동은 현재 FrontPicker 다른 축 Busy 상태만 확인한다.
                return VerifyFrontPickerNotBusy(machine != null ? machine.PickerFrontUnit : null, movingName, out reason);
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

        // 인터락 항목: 수동 FrontPickerT 이동 전 대응 Z축이 Avoid 위치인지 확인한다.
        private static bool CanManualFrontPickerT(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                PickerAxis zAxis;
                // 방어 조건: T축 이름에서 대응 Z축을 찾지 못하면 T-Z 페어 조건을 적용하지 않는다.
                if (!TryResolvePairedZAxis(movingName, out zAxis))
                    return true;

                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
                // 현재 기준: FrontPickerT 수동 이동 전 대응 FrontPickerZ축은 Avoid 위치여야 한다.
                if (front != null && !front.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Front" + zAxis + " must be at Avoid position.",
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

        // 인터락 항목: 수동 FrontPickerX 이동 전 목표 존별 Input/Process/Output 진입 조건과 PickerZone 조건을 확인한다.
        private static bool CanManualFrontPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                PickerWorkZone targetZone = PickerZoneInterlockRules.ResolveManualPickerXTargetZone(request, true);
                PickerWorkZone currentZone = PickerZoneInterlockRules.ResolveManualPickerXCurrentZone(machine, true);

                // 현재 기준: FrontPickerX Manual 목표 존을 판단할 수 없으면 충돌 방지를 위해 차단한다.
                if (targetZone == PickerWorkZone.Unknown)
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX 이동 불가: Manual X 목표 존을 판단할 수 없습니다. target=" +
                        (request != null ? request.TargetValue.ToString("0.###") : "<null>") +
                        ", targetName=" + (request != null ? request.TargetName : "<null>"),
                        out reason);

                // 현재 기준: Input 진입은 Z Avoid/0 이상, ExpandingZ 0 이하, InputFeederY Avoid/0 이하, Feeder Down, InputVisionX Avoid/0 이하, X 안전거리 안에서 양쪽 PickerY 동시 전진을 금지한다.
                if (targetZone == PickerWorkZone.Input &&
                    !VerifyManualFrontPickerXInputEntry(request, machine, out reason))
                    return false;

                // 현재 기준: Output 진입은 Z Avoid/0 이상, GoodStageZ Process 이하, OutputFeederY Avoid/0 이하, Feeder Down, OutputVisionX Avoid/0 이하, X 안전거리 안에서 양쪽 PickerY 동시 전진을 금지한다.
                if (targetZone == PickerWorkZone.Output &&
                    !VerifyManualFrontPickerXOutputEntry(request, machine, out reason))
                    return false;

                // 현재 기준: Manual Process 존 진입은 FrontPickerZ0~Z3가 Avoid 또는 0 이상 위치여야 한다.
                if (PickerZoneInterlockRules.IsManualPickerXProcessZone(targetZone) &&
                    !PickerZoneInterlockRules.IsManualPickerXProcessZone(currentZone) &&
                    !CanKeepFrontPickerZDuringXMove(request) &&
                    !VerifyFrontPickerZAxesAvoidOrNonNegative(machine != null ? machine.PickerFrontUnit : null, "FrontPickerX", out reason))
                    return false;

                // 현재 기준: FrontPickerX 목표 존과 반대 Picker 점유 상태를 PickerZone 룰에서 최종 확인한다.
                if (!PickerZoneInterlockRules.VerifyFrontPickerXMove(request, out reason))
                    return false;

                // 기존 조건: Manual X 이동 전체에 InputVisionX Home, FrontPickerY Avoid, Z Avoid, InputFeederY Avoid, InputFeeder Down을 일괄 적용했다.
                // 현재 필요 여부: 사용 안 함. 현재는 Input/Process/Output 목표 존별 진입 조건으로 분리해서 적용한다.
                //InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                //string axisReason;
                //if (stage != null &&
                //    !MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(stage.CameraX, "InputVisionX", out axisReason))
                //    return MotionGuardRuleHelpers.Block("FrontPickerX", "FrontPickerX HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason, out reason);
                //if (front != null && !front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                //    return MotionGuardRuleHelpers.Block("FrontPickerX", "FrontPickerX HOME blocked. FrontPickerY must be at Avoid position.", out reason);
                //if (!VerifyFrontPickerZAxesAvoid(front, "FrontPickerX", out reason))
                //    return false;
                //if (feeder != null && !feeder.IsWaferFeederYInAvoidPosition())
                //    return MotionGuardRuleHelpers.Block("FrontPickerX", "FrontPickerX HOME blocked. InputFeederY must be at Avoid position.", out reason);
                //if (feeder != null && !feeder.IsWaferFeederDown())
                //    return MotionGuardRuleHelpers.Block("FrontPickerX", "FrontPickerX HOME blocked. InputFeeder lift cylinder must be down.", out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "Exception occurred while verifying FrontPickerX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: FrontPickerX Input 진입 전 Z, ExpanderZ, InputFeeder, InputVisionX, 상대 PickerY 거리 조건을 확인한다.
        private static bool VerifyManualFrontPickerXInputEntry(MotionGuardRuleContext request, CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            // 현재 기준: Input 진입 전 FrontPickerZ0~Z3는 Avoid 또는 0 이상 위치여야 한다.
            if (!VerifyFrontPickerZAxesAvoidOrNonNegative(machine != null ? machine.PickerFrontUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputExpandingZ는 0 이하 위치여야 한다.
            if (!VerifyInputExpanderZAtOrBelowZero(machine != null ? machine.InputStageUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputFeeder Avoid Dog(X090)가 ON이어야 한다.
            if (!VerifyInputFeederAvoidDog(machine != null ? machine.InputFeederUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputFeeder Lift는 Down 상태여야 한다.
            if (!VerifyInputFeederDown(machine != null ? machine.InputFeederUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputVisionX는 Avoid 또는 0 이하 위치여야 한다.
            if (!VerifyInputVisionXAtAvoidOrBelowZero(request, machine, machine != null ? machine.InputStageUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: FrontPickerX Input 진입 전 X 안전거리 안에서 Front/Rear PickerY가 동시에 전진하면 차단한다.
            if (!PickerZoneInterlockRules.VerifyPickerXOppositeYClearance(request, true, "FrontPickerX", out reason))
                return false;

            // 기존 조건: FrontPickerX Input 진입 전 상대 RearPickerY는 거리와 무관하게 무조건 Avoid 위치여야 했다.
            // 현재 필요 여부: 사용 안 함. X 안전거리 안에서도 한쪽 PickerY만 전진한 상태는 허용하고 양쪽 동시 전진만 차단한다.
            //return VerifyRearPickerYAvoidForFrontPickerX(machine, out reason);

            return true;
        }

        // 인터락 항목: FrontPickerX Output 진입 전 Z, GoodStageZ, OutputFeeder, OutputVisionX, 상대 PickerY 거리 조건을 확인한다.
        private static bool VerifyManualFrontPickerXOutputEntry(MotionGuardRuleContext request, CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            // 현재 기준: Output 진입 전 FrontPickerZ0~Z3는 Avoid 또는 0 이상 위치여야 한다.
            if (!VerifyFrontPickerZAxesAvoidOrNonNegative(machine != null ? machine.PickerFrontUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputGoodStageZ는 ProcessPos 이하 위치여야 한다.
            if (!VerifyGoodStageZAtOrBelowProcess(machine != null ? machine.OutputStageUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputFeeder Avoid Dog(X091)가 ON이어야 한다.
            if (!VerifyOutputFeederAvoidDog(machine != null ? machine.OutputFeederUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputFeeder Lift는 Down 상태여야 한다.
            if (!VerifyOutputFeederDown(machine != null ? machine.OutputFeederUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputVisionX는 Avoid 또는 0 이하 위치여야 한다.
            if (!VerifyOutputVisionXAtAvoidOrBelowZero(request, machine, machine != null ? machine.OutputStageUnit : null, "FrontPickerX", out reason))
                return false;

            // 현재 기준: FrontPickerX Output 진입 전 X 안전거리 안에서 Front/Rear PickerY가 동시에 전진하면 차단한다.
            if (!PickerZoneInterlockRules.VerifyPickerXOppositeYClearance(request, true, "FrontPickerX", out reason))
                return false;

            // 기존 조건: FrontPickerX Output 진입 전 상대 RearPickerY는 거리와 무관하게 무조건 Avoid 위치여야 했다.
            // 현재 필요 여부: 사용 안 함. X 안전거리 안에서도 한쪽 PickerY만 전진한 상태는 허용하고 양쪽 동시 전진만 차단한다.
            //return VerifyRearPickerYAvoidForFrontPickerX(machine, out reason);

            return true;
        }

        // 인터락 항목: FrontPickerZ 전체가 Avoid 위치 또는 0 이상 위치인지 확인한다.
        internal static bool VerifyFrontPickerZAxesAvoidOrNonNegative(PickerFrontUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: FrontPicker 참조가 없으면 Z 위치 조건을 적용하지 않는다.
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = ResolveFrontPickerAxis(picker, zAxis);
                // 현재 기준: FrontPickerZ축이 이동 중이면 Input/Output/Process 진입을 차단한다.
                if (axis != null && axis.IsMoving)
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: Front" + zAxis + " 축이 이동 중입니다.",
                        out reason);

                // 현재 기준: FrontPickerZ축은 Avoid 위치이거나 ActualPosition이 0 이상이어야 한다.
                if (picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition") ||
                    IsAxisAtOrAboveZero(axis))
                    continue;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: Front" + zAxis + " 축이 Avoid 또는 0 이상 위치가 아닙니다. actual=" +
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
        // 제3 분기(사용자 승인 2026-07-24): 위 두 조건이 아니거나 비전이 이동 중이어도, 피커 이동 목표와
        // InputVisionX의 Actual/Command 양쪽이 SharedRailX 페어 간격식으로 SafetyDistance를 만족하면
        // 진입을 허용한다 (최소 회피/팔로잉 진입의 유지 간격 50mm > 요구 10mm, RetreatExtra 미포함 — R5).
        private static bool VerifyInputVisionXAtAvoidOrBelowZero(MotionGuardRuleContext request, CDT320_Machine machine, InputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: InputStage 또는 InputVisionX 참조가 없으면 InputVisionX 조건을 적용하지 않는다.
            if (stage == null || stage.CameraX == null)
                return true;

            string clearanceDetail;
            BaseAxis pickerX = machine != null && machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null;
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
            BaseAxis pickerX = machine != null && machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null;
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

        // 인터락 항목: FrontPickerX 이동 전 상대 RearPickerY가 Avoid 위치인지 확인한다.
        private static bool VerifyRearPickerYAvoidForFrontPickerX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
            // 방어 조건: RearPicker 참조가 없으면 상대 PickerY 조건을 적용하지 않는다.
            if (rear == null)
                return true;

            // 현재 기준: FrontPickerX 진입 전 상대 RearPickerY는 Avoid 위치여야 한다.
            if (rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                return true;

            return MotionGuardRuleHelpers.Block("FrontPickerX", "FrontPickerX 이동 불가: 상대 RearPickerY가 Avoid 위치가 아닙니다.", out reason);
        }

        // 인터락 항목: FrontPickerX 홈 전 VisionX, ExpanderZ, PickerY/Z, InputFeeder 안전 위치를 확인한다.
        private static bool CanHomeFrontPickerX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                string axisReason;
                // 현재 기준: FrontPickerX Home 전 InputVisionX는 미홈 상태이거나 Home(0) 위치여야 한다.
                if (stage != null &&
                    !MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(stage.CameraX, "InputVisionX", out axisReason))
                {
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason,
                        out reason);
                }

                // 현재 기준: FrontPickerX Home 전 InputExpandingZ는 Home(0), Avoid, Process, Ready 중 하나여야 한다.
                if (stage != null && !IsExpanderZHomeAvoidProcessOrReady(stage))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputExpandingZ must be at Home(0), Avoid, Process or Ready position.",
                        out reason);

                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
                // 현재 기준: FrontPickerX Home 전 FrontPickerY는 Home(0) 또는 Avoid 위치여야 한다.
                if (front != null &&
                    !IsAxisAtHomeOrTeachingAvoid(
                        ResolveFrontPickerAxis(front, PickerAxis.PickerY),
                        () => front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. FrontPickerY must be at Home(0) or Avoid position.",
                        out reason);

                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
                // 현재 기준: FrontPickerX Home 전 RearPickerY도 Home(0) 또는 Avoid 위치여야 한다.
                if (rear != null &&
                    !IsAxisAtHomeOrTeachingAvoid(
                        ResolveRearPickerAxis(rear, PickerAxis.PickerY),
                        () => rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. RearPickerY must be at Home(0) or Avoid position.",
                        out reason);

                // 현재 기준: FrontPickerX Home 전 FrontPickerZ0~Z3는 모두 Home(0) 또는 Avoid 위치여야 한다.
                if (!VerifyFrontPickerZAxesHomeOrAvoid(front, "FrontPickerX", out reason))
                    return false;

                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                // 현재 기준: FrontPickerX Home 전 InputFeederY는 Home(0) 또는 Avoid 위치여야 한다.
                if (feeder != null && !IsInputFeederYHomeOrAvoid(feeder))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputFeederY must be at Home(0) or Avoid position.",
                        out reason);

                // 현재 기준: FrontPickerX Home 전 InputFeeder Lift 실린더는 Down 상태여야 한다.
                if (feeder != null && !feeder.IsWaferFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputFeeder lift cylinder must be down.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerX",
                    "Exception occurred while verifying FrontPickerX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: FrontPickerY 홈 전 PickerZ, ExpanderZ, OutputStageZ 안전 위치를 확인한다.
        private static bool CanHomeFrontPickerY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!PickerZoneInterlockRules.VerifyPickerYHomePairSafety(
                    machine,
                    true,
                    "FrontPickerY",
                    out reason))
                    return false;

                // 현재 기준: FrontPickerY Home 전 Z0~Z3는 Home(0) 또는 Avoid 위치여야 한다.
                if (!VerifyFrontPickerZAxesHomeOrAvoid(machine != null ? machine.PickerFrontUnit : null, "FrontPickerY", out reason))
                    return false;

                // InputExpandingZ가 Avoid/Process/Ready 위치여야 FrontPickerY 이동 가능.
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                // 현재 기준: FrontPickerY Home 전 InputExpandingZ는 Home(0), Avoid, Process, Ready 중 하나여야 한다.
                if (stage != null && !IsExpanderZHomeAvoidProcessOrReady(stage))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerY",
                        "FrontPickerY 이동 불가: InputExpandingZ가 Home(0)/Avoid/Process/Ready 위치가 아닙니다.",
                        out reason);

                // OutputStage GoodStageZ가 안전 위치(Avoid 또는 Process)여야 FrontPickerY 이동 가능.
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                // 현재 기준: FrontPickerY Home 전 OutputStage GoodStageZ는 Home(0), Avoid 또는 Process 위치여야 한다.
                if (outputStage != null && !IsGoodStageZHomeAvoidOrProcess(outputStage))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerY",
                        "FrontPickerY 이동 불가: OutputStage GoodStageZ가 Home(0), Avoid 또는 Process 위치가 아닙니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerY",
                    "Exception occurred while verifying FrontPickerY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: FrontPickerT 홈 전 대응 Z축이 Avoid 위치인지 확인한다.
        private static bool CanHomeFrontPickerT(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                PickerAxis zAxis;
                // 방어 조건: T축 이름에서 대응 Z축을 찾지 못하면 Home T-Z 페어 조건을 적용하지 않는다.
                if (!TryResolvePairedZAxis(movingName, out zAxis))
                    return true;

                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
                // 현재 기준: FrontPickerT Home 전 대응 FrontPickerZ축은 Home(0) 또는 Avoid 위치여야 한다.
                if (front != null &&
                    !IsAxisAtHomeOrTeachingAvoid(
                        ResolveFrontPickerAxis(front, zAxis),
                        () => front.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Front" + zAxis + " must be at Home(0) or Avoid position.",
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

        // 인터락 항목: FrontPickerZ 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyFrontPickerZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoFrontPickerZ(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualFrontPickerZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontPickerZ(request.Machine, request.MovingName, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 인터락 항목: FrontPickerZ 홈은 현재 별도 차단 조건 없이 허용한다.
        private static bool CanHomeFrontPickerZ(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        // 인터락 항목: FrontPickerZ 수동 이동 전 Reticle, InputExpanderZ, OutputGoodStageZ, Busy 조건을 확인한다.
        private static bool CanManualFrontPickerZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            string movingName = request != null ? request.MovingName : "FrontPickerZ";
            PickerWorkZone targetZone = ResolvePickerZTargetZone(request);

            // 현재 기준: FrontPickerZ 수동 이동 전 Z Home 룰을 먼저 확인한다.
            if (!CanHomeFrontPickerZ(machine, movingName, out reason))
                return false;

            // 현재 기준: PickerY가 Input/Output/공통 Avoid 위치이면 FrontPickerZ 하강 이동을 차단한다.
            if (!VerifyFrontPickerYAvoidBlocksZDown(request, out reason))
                return false;

            // 절대 인터락: HOME 외 모든 FrontPickerZ 이동 전 Reticle은 Retract 상태여야 한다.
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
            // 현재 기준: Input/Unknown 존에서 FrontPickerZ 이동 전 InputExpandingZ는 0 이하 또는 Avoid 위치여야 한다.
            if (RequiresInputStageZSafe(targetZone) &&
                !VerifyInputExpanderZAtOrBelowZero(stage, movingName, out reason))
                return false;

            OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
            // 현재 기준: Output/Unknown 존에서 FrontPickerZ 이동 전 OutputGoodStageZ는 ProcessPos 이하 위치여야 한다.
            if (RequiresOutputStageZSafeForPickerY(targetZone) &&
                !VerifyGoodStageZAtOrBelowProcess(outputStage, movingName, out reason))
                return false;

            return VerifyFrontPickerNotBusy(machine != null ? machine.PickerFrontUnit : null, movingName, out reason);
        }

        // 인터락 항목: 자동 FrontPickerZ 이동은 수동 FrontPickerZ 조건과 동일하게 확인한다.
        private static bool CanAutoFrontPickerZ(MotionGuardRuleContext request, out string reason)
        {
            // 현재 기준: Auto FrontPickerZ도 Manual FrontPickerZ 기본 인터락과 동일하게 확인한다.
            return CanManualFrontPickerZ(request, out reason);
        }

        // 인터락 항목: FrontPickerY가 Avoid 계열 위치일 때 FrontPickerZ 하강 명령을 차단한다.
        private static bool VerifyFrontPickerYAvoidBlocksZDown(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            // 방어 조건: 요청 또는 FrontPicker 참조가 없으면 Y-Z Avoid 연동 조건을 적용하지 않는다.
            PickerFrontUnit picker = request != null && request.Machine != null ? request.Machine.PickerFrontUnit : null;
            if (picker == null)
                return true;

            PickerAxis zAxis;
            // 방어 조건: 이동 Z축을 해석하지 못하면 Y-Z Avoid 연동 조건을 적용하지 않는다.
            if (!TryResolveMovingZAxis(request.MovingName, out zAxis))
                return true;

            BaseAxis zItem = ResolveFrontPickerAxis(picker, zAxis);
            if (zItem == null)
                return true;

            // 현재 기준: Home/0 또는 Z Avoid 위치로 가는 복귀 목표는 하강 차단 대상에서 제외한다.
            if (IsFrontPickerZSafeRetreatTarget(request, picker, zAxis, zItem))
                return true;

            // 현재 기준: 실제 Z 하강 목표가 아니면 차단하지 않는다.
            if (!IsZTargetDown(request.TargetValue, zItem))
                return true;

            string yAvoidName = ResolveFrontPickerYAvoidPositionName(picker);
            if (string.IsNullOrWhiteSpace(yAvoidName))
                return true;

            double yActual = picker.PickerY != null ? picker.PickerY.ActualPosition : 0.0;
            return MotionGuardRuleHelpers.Block(
                request.MovingName,
                request.MovingName + " 이동 불가: FrontPickerY가 " + yAvoidName +
                " 위치일 때 PickerZ 하강 이동은 금지됩니다. " +
                "pickerY=" + yActual.ToString("F3") +
                ", zActual=" + zItem.ActualPosition.ToString("F3") +
                ", zTarget=" + request.TargetValue.ToString("F3") +
                ", targetName=" + (string.IsNullOrWhiteSpace(request.TargetName) ? "-" : request.TargetName),
                out reason);
        }

        // 인터락 기준: FrontPickerZ 목표가 Home/0 또는 Z Avoid 위치 복귀 목표인지 판단한다.
        private static bool IsFrontPickerZSafeRetreatTarget(MotionGuardRuleContext request, PickerFrontUnit picker, PickerAxis zAxis, BaseAxis zItem)
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

        // 인터락 기준: FrontPickerY가 InputAvoid/OutputAvoid/Avoid 중 어느 위치에 있는지 반환한다.
        private static string ResolveFrontPickerYAvoidPositionName(PickerFrontUnit picker)
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
            return (value ?? string.Empty).IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

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

        // 인터락 항목: FrontPicker 내부 다른 축 Busy 상태를 확인한다.
        private static bool VerifyFrontPickerNotBusy(PickerFrontUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: FrontPicker 참조가 없으면 Busy 조건을 적용하지 않는다.
            if (picker == null)
                return true;


            // 같은 축에 있어서 동시에 구동되도 간섭은 되지 않는다.
            // 다른 유닛과의 상관관계를 봐야한다. Z축 내려왔을때.
            // 
            //if (IsMovingExcept(picker.PickerX, movingName, "FrontPickerX"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerX is moving.", out reason);
            //if (IsMovingExcept(picker.PickerY, movingName, "FrontPickerY"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerY is moving.", out reason);
            //if (IsMovingExcept(picker.PickerT0, movingName, "FrontPickerT0"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerT0 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerT1, movingName, "FrontPickerT1"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerT1 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerT2, movingName, "FrontPickerT2"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerT2 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerT3, movingName, "FrontPickerT3"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerT3 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerZ0, movingName, "FrontPickerZ0"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerZ0 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerZ1, movingName, "FrontPickerZ1"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerZ1 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerZ2, movingName, "FrontPickerZ2"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerZ2 is moving.", out reason);
            //if (IsMovingExcept(picker.PickerZ3, movingName, "FrontPickerZ3"))
            //    return MotionGuardRuleHelpers.Block(movingName, "FrontPickerZ3 is moving.", out reason);

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

        // 인터락 항목: FrontPickerZ 전체가 Avoid 위치인지 확인한다.
        private static bool VerifyFrontPickerZAxesAvoid(PickerFrontUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: FrontPicker 참조가 없으면 Z Avoid 조건을 적용하지 않는다.
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                // 현재 기준: FrontPickerZ0~Z3 중 하나라도 Avoid 위치가 아니면 이동을 차단한다.
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Front" + zAxis + " must be at Avoid position.",
                        out reason);
            }

            return true;
        }

        // 인터락 항목: FrontPickerZ 전체가 Home 또는 Avoid 위치인지 확인한다.
        private static bool VerifyFrontPickerZAxesHomeOrAvoid(PickerFrontUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: FrontPicker 참조가 없으면 Z Home/Avoid 조건을 적용하지 않는다.
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = ResolveFrontPickerAxis(picker, zAxis);
                // 현재 기준: FrontPickerZ0~Z3는 Home(0) 또는 Avoid 위치여야 한다.
                if (!IsAxisAtHomeOrTeachingAvoid(axis, () => picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Front" + zAxis + " must be at Home(0) or Avoid position.",
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

            //이렇게 하면 홈 위치가 0이구나..
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

        // 인터락 기준: 홈 이동 전 InputFeederY가 Home(0)이거나 Avoid Dog(X090)가 ON인지 판단한다.
        private static bool IsInputFeederYHomeOrAvoid(InputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            return feeder.IsWaferFeederYInHomePosition() ||
                   feeder.IsWaferFeederAvoidPositionCheck();
        }

        // 인터락 기준: 홈 이동 전 Output GoodStageZ가 Home(0), Avoid 또는 Process 위치인지 판단한다.
        private static bool IsGoodStageZHomeAvoidOrProcess(OutputStageUnit outputStage)
        {
            if (outputStage == null)
                return true;

            BaseAxis goodStageZ = outputStage.GoodStage != null ? outputStage.GoodStage.StageZ : null;
            if (MotionGuardRuleHelpers.IsAt(goodStageZ, 0.0))
                return true;

            return outputStage.IsGoodStageZInAvoidOrProcessPosition();
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
                   System.Math.Abs(actual - waferZ.ProcessPosition) <= tolerance||
                   System.Math.Abs(actual - waferZ.ReadyPosition) <= tolerance;
        }

        // FrontPickerX,Y 평면 이동 전제: ExpanderZ가 Avoid/Process/Ready 위치여야 한다. (Home(0)은 제외)
        // 인터락 기준: ExpanderZ가 FrontPicker 평면 이동 가능한 Avoid/Process/Ready 위치인지 판단한다.
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

        // 인터락 기준: FrontPickerT 축명에 대응되는 Z축을 해석한다.
        private static bool TryResolvePairedZAxis(string movingName, out PickerAxis zAxis)
        {
            zAxis = PickerAxis.PickerZ0;

            switch (movingName)
            {
                // 프론트 피커 T0축 처리
                case "FrontPickerT0":
                    zAxis = PickerAxis.PickerZ0;
                    return true;
                // 프론트 피커 T1축 처리
                case "FrontPickerT1":
                    zAxis = PickerAxis.PickerZ1;
                    return true;
                // 프론트 피커 T2축 처리
                case "FrontPickerT2":
                    zAxis = PickerAxis.PickerZ2;
                    return true;
                // 프론트 피커 T3축 처리
                case "FrontPickerT3":
                    zAxis = PickerAxis.PickerZ3;
                    return true;
                default:
                    return false;
            }
        }

        // 인터락 기준: FrontPickerZ 축명에서 이동 대상 Z축을 해석한다.
        private static bool TryResolveMovingZAxis(string movingName, out PickerAxis zAxis)
        {
            zAxis = PickerAxis.PickerZ0;

            switch (movingName)
            {
                // 프론트 피커 Z0축 처리
                case "FrontPickerZ0":
                    zAxis = PickerAxis.PickerZ0;
                    return true;
                // 프론트 피커 Z1축 처리
                case "FrontPickerZ1":
                    zAxis = PickerAxis.PickerZ1;
                    return true;
                // 프론트 피커 Z2축 처리
                case "FrontPickerZ2":
                    zAxis = PickerAxis.PickerZ2;
                    return true;
                // 프론트 피커 Z3축 처리
                case "FrontPickerZ3":
                    zAxis = PickerAxis.PickerZ3;
                    return true;
                default:
                    return false;
            }
        }

        // 인터락 기준: FrontPicker 인터락 차단 사유를 로그에 기록한다.
        private static void LogBlockedReason(string reason)
        {
            try
            {
                // 현재 기준: 차단 사유가 있으면 FrontPicker 인터락 로그로 남긴다.
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "PickerFrontInterlock", reason + " - Blocked");
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
