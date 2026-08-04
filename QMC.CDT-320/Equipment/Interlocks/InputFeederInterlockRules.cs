using System;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Interlocks
{
    public static class InputFeederInterlockRules
    {
        #region Stage T 복구 및 규칙 진입

        private const double PositionTolerance = 0.05;
        private const double StageTZeroTolerance = 0.1;

        // 수동 Jog 화면의 HOME END 선행 게이트에서 사용할 읽기 전용 판정이다.
        // 실제 이동 허용 여부는 이후 MotionGuard 전체 조건에서 다시 확인한다.
        internal static bool CanBeginStageTCollisionRecoveryJog(BaseAxis axis, int direction)
        {
            if (axis == null || direction == 0)
                return false;

            MotionGuardContext context = MotionGuardRuntime.ContextProvider != null
                ? MotionGuardRuntime.ContextProvider()
                : null;
            CDT320_Machine machine = context != null ? context.Machine : null;
            InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            if (feeder == null || stage == null || stage.StageT == null ||
                feeder.FeederY == null ||
                !ReferenceEquals(axis, feeder.FeederY))
            {
                return false;
            }

            if (Math.Abs(stage.StageT.ActualPosition) <= StageTZeroTolerance)
                return false;

            return direction < 0;
        }

        // 인터락 항목: InputFeederY/Lift/Clamp 이동 요청을 각 Feeder 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederY", "FeederY"))
            {
                if (!PickerZoneInterlockRules.VerifyPickerXStoppedForClearanceMechanismMove(
                    request.Machine, "InputFeederY", out reason))
                    return false;

                return VerifyInputFeederY(request, out reason);
            }

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederLift"))
            {
                if (!PickerZoneInterlockRules.VerifyPickerXStoppedForClearanceMechanismMove(
                    request.Machine, "InputFeederLift", out reason))
                    return false;

                return VerifyInputFeederLift(request, out reason);
            }

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederClamp"))
                return VerifyInputFeederClamp(request, out reason);

            return true;
        }

        #endregion

        #region Input Feeder Y

        // 인터락 항목: InputFeederY 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyInputFeederY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

            if (!VerifyInputFeederYAbsoluteGuard(request, out reason))
                return false;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoInputFeederY(machine, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualInputFeederY(machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeInputFeederY(machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        // 절대 인터락: 기존 FeederY 분기 전에 Camera/StageT/Lift/Overload 최소 안전조건을 AND로 적용한다.
        private static bool VerifyInputFeederYAbsoluteGuard(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
            BaseAxis cameraX = stage != null ? stage.CameraX : null;

            if (stage == null || cameraX == null || feeder == null || feeder.FeederY == null ||
                stage.Recipe == null || stage.Recipe.VisionX == null)
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY 절대 인터락 확인 불가: InputStage/InputCameraX/InputFeederY teaching 정보가 없습니다.",
                    out reason);

            //Todo: Feeder 가 안전 위치고 클램프가 업상태이면 PASS

            bool feederAvoidDogOn = feeder.IsWaferFeederAvoidPositionCheck();
            bool initializeVisionRetreatVerified =
                request.MoveKind == MotionGuardMoveKind.AxisHome &&
                MotionGuardRuntime.IsFeederHomeVisionRetreatActive(
                    feeder.FeederY,
                    cameraX,
                    true);

            if (!feederAvoidDogOn)
            {
                // 전체 초기화 Step 180이 MEL(-)을 확인한 정확한 축 쌍만 좌표 유실 상태의
                // Feeder HOME 1회에 허용합니다. 5mm 이탈은 Dog 복구 후 수행하며 Auto/Manual은 그대로입니다.
                if (!initializeVisionRetreatVerified &&
                    !IsInputVisionXInAvoidPosition(stage) &&
                    cameraX.ActualPosition > 0.0)
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY 이동 불가: InputCameraX가 정확한 Avoid 또는 0 이하 위치여야 합니다. " +
                        "cameraActual=" + cameraX.ActualPosition.ToString("0.###"),
                        out reason);

            }

            if (request.MoveKind == MotionGuardMoveKind.AxisMove ||
                (request.MoveKind == MotionGuardMoveKind.AxisHome && !feederAvoidDogOn))
            {
                BaseAxis stageT = stage.StageT;
                if (stageT == null || Math.Abs(stageT.ActualPosition) > StageTZeroTolerance)
                {
                    bool recoveryJog = stageT != null &&
                        MotionGuardRuleHelpers.IsJogMove(request) &&
                        request.TargetValue < feeder.FeederY.ActualPosition;
                    if (!recoveryJog)
                    {
                        return MotionGuardRuleHelpers.Block(
                            "InputFeederY",
                            "InputFeederY Manual/HOME 이동 불가: InputStageT 실제 위치가 -0.1~+0.1 범위여야 합니다. " +
                            "수동 복구 Jog는 InputFeederY -방향만 허용됩니다. " +
                            "stageT=" + (stageT != null ? stageT.ActualPosition.ToString("0.###") : "missing") +
                            ", feederActual=" + feeder.FeederY.ActualPosition.ToString("0.###") +
                            ", target=" + request.TargetValue.ToString("0.###"),
                            out reason);
                    }
                }
            }

            // 기존 Auto Load-to-Stage는 제품 전달 후 Lift Up 상태로 FeederY를 Avoid 복귀시킨다.
            // 이 Auto 경로의 Lift Down 강제는 시퀀스 변경 승인이 필요하므로 Manual/HOME에만 신규 적용한다.
            if (request.MoveKind == MotionGuardMoveKind.AxisMove ||
                request.MoveKind == MotionGuardMoveKind.AxisHome)
            {
                if (!feeder.IsWaferFeederSimulationOrDryRun())
                {
                    int downReadError = -1;
                    if (feeder.WaferFeederDownSensor == null ||
                        !AjinIoScanService.TryReadHardwareInput(feeder.WaferFeederDownSensor, out downReadError))
                    {
                        return MotionGuardRuleHelpers.Block(
                            "InputFeederY",
                            "InputFeederY Lift Down 센서 갱신 실패. error=" + downReadError,
                            out reason);
                    }
                }

                if (!feeder.IsWaferFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY Manual/HOME 이동 불가: InputFeeder Lift가 Down 상태여야 합니다.",
                        out reason);
            }

            

            if (!feeder.IsWaferFeederSimulationOrDryRun())
            {
                int overloadReadError = -1;
                if (feeder.WaferFeederOverloadSensor == null ||
                    !AjinIoScanService.TryReadHardwareInput(feeder.WaferFeederOverloadSensor, out overloadReadError))
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeeder Overload 센서 갱신 실패. error=" + overloadReadError,
                        out reason);
                }
            }

            if (feeder.IsWaferFeederOverload())
            {
                double tolerance = feeder.FeederY.Config != null && feeder.FeederY.Config.InPositionTolerance > 0.0
                    ? feeder.FeederY.Config.InPositionTolerance
                    : PositionTolerance;
                bool positiveJog = MotionGuardRuleHelpers.IsJogMove(request) &&
                    request.TargetValue > feeder.FeederY.ActualPosition + tolerance;
                if (!positiveJog)
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeeder Overload 감지 중에는 +방향 Jog만 허용됩니다. " +
                        "current=" + feeder.FeederY.ActualPosition.ToString("0.###") +
                        ", target=" + request.TargetValue.ToString("0.###"),
                        out reason);
            }

            return true;
        }

        // 인터락 항목: 자동 InputFeederY 이동은 Stage 로드/언로드 위치, 피커 Input 존 점유, LifterZ, Lift/Clamp 상태를 확인한다.
        private static bool CanAutoInputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: 장비 참조가 없으면 InputFeederY 자동 인터락을 적용하지 않는다.
            if (machine == null)
                return true;

            InputCassetteUnit cassette = machine.InputCassetteUnit;
            InputStageUnit stage = machine.InputStageUnit;
            // 인터락 조건: InputLifterZ가 이동 중이면 InputFeederY 자동 이동을 차단한다.
            if (cassette != null && cassette.InputLifterZ != null && cassette.InputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputLifterZ is moving. InputFeederY move is blocked.",
                    out reason);

            // 인터락 조건: InputStage가 있으면 T/Z/VisionX 안전 위치를 확인한다.
            if (stage != null)
            {
                //if (!IsInputStageYAtLoadOrUnload(stage))
                //    return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage StageY must be at Loading or Unloading position.", out reason);

                // 인터락 조건: StageT가 Load/Unload 계열 위치가 아니면 FeederY 이송을 차단한다.
                if (!IsInputStageTAtLoadOrUnload(stage))
                    return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage StageT must be at Loading or Unloading position.", out reason);

                // 인터락 조건: ExpanderZ가 Load/Unload 높이가 아니면 FeederY 이송을 차단한다.
                if (!IsExpanderZAtLoadOrUnload(stage))
                    return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage ExpanderZ must be at Loading or Unloading position.", out reason);

                // 인터락 조건: InputVisionX가 Avoid 위치가 아니면 FeederY 이송을 차단한다.
                if (!IsInputVisionXInAvoidPosition(stage))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY move blocked. InputVisionX must be at Avoid position.",
                        out reason);
            }

            string pickerDetail;
            // 인터락 조건: FrontPicker가 Input 존을 점유하거나 위치를 확정할 수 없으면 FeederY 이동을 차단한다.
            if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, true, PickerWorkZone.Input, out pickerDetail))
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY 이동 차단. FrontPicker가 Input zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                    out reason);

            // 인터락 조건: RearPicker가 Input 존을 점유하거나 위치를 확정할 수 없으면 FeederY 이동을 차단한다.
            if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, false, PickerWorkZone.Input, out pickerDetail))
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY 이동 차단. RearPicker가 Input zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                    out reason);

            InputFeederUnit feeder = machine.InputFeederUnit;
            // 방어 조건: Feeder 참조가 없으면 Feeder 센서 조건은 적용하지 않는다.
            if (feeder == null)
                return true;

            // 인터락 조건: Feeder 과부하 센서가 감지되면 FeederY 자동 이동을 차단한다.
            if (feeder.IsWaferFeederOverload())
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY move blocked. InputFeeder overload sensor is detected.",
                    out reason);

            return true;
        }

        // 인터락 항목: 수동 InputFeederY 이동은 카세트 돌출, 피커 Input 존 점유, LifterZ, Stage/Feeder 상태를 확인한다.
        private static bool CanManualInputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                // 방어 조건: 장비 참조가 없으면 InputFeederY 수동 인터락을 적용하지 않는다.
                if (machine == null)
                    return true;

                InputCassetteUnit cassette = machine.InputCassetteUnit;
                // 인터락 조건: InputLifterZ가 이동 중이면 InputFeederY 수동 이동을 차단한다.
                if (cassette != null && cassette.InputLifterZ != null && cassette.InputLifterZ.IsMoving)
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputLifterZ is moving. InputFeederY home is blocked.",
                        out reason);
                }

                string pickerDetail;
                // 현재 기준: Manual InputFeederY 이동 전 FrontPicker가 Input 존에 있으면 차단한다.
                if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, true, PickerWorkZone.Input, out pickerDetail))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY 이동 차단. FrontPicker가 Input zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                        out reason);

                // 현재 기준: Manual InputFeederY 이동 전 RearPicker가 Input 존에 있으면 차단한다.
                if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, false, PickerWorkZone.Input, out pickerDetail))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY 이동 차단. RearPicker가 Input zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                        out reason);

                string axisReason;
                // 인터락 조건: InputVisionX가 홈 준비 상태가 아니면 FeederY 수동 이동을 차단한다.
                if (!IsInputVisionXHomeReadyForInputFeederHome(machine.InputStageUnit, out axisReason))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason,
                        out reason);

                // 인터락 조건: FrontPickerX가 홈 준비 상태가 아니면 FeederY 수동 이동을 차단한다.
                // To do: 이건 AVOID로 변경해야한다
                //if (!IsFrontPickerXHomeReadyForInputFeederHome(machine.PickerFrontUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "InputFeederY",
                //        "InputFeederY HOME blocked. FrontPickerX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                //// 인터락 조건: RearPickerX가 홈 준비 상태가 아니면 FeederY 수동 이동을 차단한다.
                //if (!IsRearPickerXHomeReadyForInputFeederHome(machine.PickerRearUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "InputFeederY",
                //        "InputFeederY HOME blocked. RearPickerX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                InputFeederUnit feeder = machine.InputFeederUnit;
                // 방어 조건: Feeder 참조가 없으면 Feeder 센서/자재 조건은 적용하지 않는다.
                if (feeder == null)
                    return true;

                // 인터락 조건: Feeder 위에 자재 데이터나 검출 센서가 남아 있으면 홈 계열 이동을 차단한다.

                // To do: 이건 메뉴얼로 움직일떄는 걸리면 안된다.
                //if (!VerifyInputFeederEmptyForHome(feeder, out reason))
                //    return false;

                // 인터락 조건: Feeder 과부하 센서가 감지되면 FeederY 수동 이동을 차단한다.
                // To do: IsWaferFeederOverload 걸리면 + 방향으로 움직일때는 움직여야 한다.
                //if (feeder.IsWaferFeederOverload())
                //    return MotionGuardRuleHelpers.Block(
                //        "InputFeederY",
                //        "InputFeederY HOME blocked. InputFeeder overload sensor is detected.",
                //        out reason);

                // 인터락 조건: 실장비 모드에서는 Feeder Unclamp 상태를 확인한다.
                // To do: 실제로 메뉴얼로 움직일때는 상황 보고 해야함
                //if (!ShouldBypassHardwareMechanismChecks())
                //{
                //    // 인터락 조건: Feeder가 Unclamp 상태가 아니면 FeederY 수동 이동을 차단한다.
                //    if (!IsFeederUnclamp(feeder))
                //        return MotionGuardRuleHelpers.Block(
                //            "InputFeederY",
                //            "InputFeederY HOME blocked. InputFeeder must be unclamped.",
                //            out reason);
                //}

                // To do: 홈잡을떄만 이다. 메뉴얼일때는 움직여도 된다
                // 인터락 조건: 실장비에서 Ring Check가 감지되면 FeederY 수동 이동을 차단한다.
                //if (!feeder.IsWaferFeederSimulationOrDryRun() && feeder.IsWaferFeederRingCheck())
                //{
                //    return MotionGuardRuleHelpers.Block(
                //        "InputFeederY",
                //        "InputFeederY HOME blocked. InputFeeder ring check is detected.",
                //        out reason);
                //}

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "Exception occurred while verifying InputFeederY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: InputFeederY 홈은 카세트 돌출, 주변 X축 홈 준비, 피커 Input 존 점유, 빈 자재 상태를 확인한다.
        private static bool CanHomeInputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                // 방어 조건: 장비 참조가 없으면 InputFeederY 홈 인터락을 적용하지 않는다.
                if (machine == null)
                    return true;

                InputCassetteUnit cassette = machine.InputCassetteUnit;
                // 인터락 조건: InputLifterZ가 이동 중이면 InputFeederY 홈 이동을 차단한다.
                if (cassette != null && cassette.InputLifterZ != null && cassette.InputLifterZ.IsMoving)
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputLifterZ is moving. InputFeederY home is blocked.",
                        out reason);
                }

                string pickerDetail;
                // 현재 기준: Home InputFeederY는 PickerY가 Home/Avoid이고 Picker X/Y/Z가 정지 상태면 초기 홈 이동을 허용한다.
                if (PickerZoneInterlockRules.IsPickerBlockingZoneTransportForFeederHome(machine, true, PickerWorkZone.Input, out pickerDetail))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME 이동 차단. FrontPicker가 Input zone을 사용 중이거나 Home 안전 상태가 아닙니다. " + pickerDetail,
                        out reason);

                // 현재 기준: Home InputFeederY는 RearPicker도 동일한 Home 안전 기준을 통과해야 한다.
                if (PickerZoneInterlockRules.IsPickerBlockingZoneTransportForFeederHome(machine, false, PickerWorkZone.Input, out pickerDetail))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME 이동 차단. RearPicker가 Input zone을 사용 중이거나 Home 안전 상태가 아닙니다. " + pickerDetail,
                        out reason);

                // 기존 조건: Home InputFeederY도 일반 이송처럼 Picker가 Input zone이면 무조건 차단했다.
                // 현재 필요 여부: 사용 안 함. 초기화 순서상 Picker 홈 전 X=0/Y=0 상태가 Input으로 잡힐 수 있어 Home 전용 안전 기준을 사용한다.
                //if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, true, PickerWorkZone.Input, out pickerDetail))
                //    return MotionGuardRuleHelpers.Block(
                //        "InputFeederY",
                //        "InputFeederY 이동 차단. FrontPicker가 Input zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                //        out reason);
                //if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, false, PickerWorkZone.Input, out pickerDetail))
                //    return MotionGuardRuleHelpers.Block(
                //        "InputFeederY",
                //        "InputFeederY 이동 차단. RearPicker가 Input zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                //        out reason);

                //string axisReason;

                //if (!IsInputVisionXHomeReadyForInputFeederHome(machine.InputStageUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "InputFeederY",
                //        "InputFeederY HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                //if (!IsFrontPickerXHomeReadyForInputFeederHome(machine.PickerFrontUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "InputFeederY",
                //        "InputFeederY HOME blocked. FrontPickerX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                //if (!IsRearPickerXHomeReadyForInputFeederHome(machine.PickerRearUnit, out axisReason))
                //    return MotionGuardRuleHelpers.Block(
                //        "InputFeederY",
                //        "InputFeederY HOME blocked. RearPickerX must be not homed yet or at Home position. " + axisReason,
                //        out reason);

                InputFeederUnit feeder = machine.InputFeederUnit;
                // 방어 조건: Feeder 참조가 없으면 Feeder 센서/자재 조건은 적용하지 않는다.
                if (feeder == null)
                    return true;

                // 인터락 조건: Feeder 위에 자재 데이터나 검출 센서가 남아 있으면 홈 이동을 차단한다.
                if (!VerifyInputFeederEmptyForHome(feeder, out reason))
                    return false;

                // 인터락 조건: Feeder 과부하 센서가 감지되면 FeederY 홈 이동을 차단한다.
                if (feeder.IsWaferFeederOverload())
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder overload sensor is detected.",
                        out reason);

                // 인터락 조건: 실장비 모드에서는 Feeder Unclamp 상태를 확인한다.
                if (!ShouldBypassHardwareMechanismChecks())
                {
                    // 인터락 조건: Feeder가 Unclamp 상태가 아니면 FeederY 홈 이동을 차단한다.
                    if (!IsFeederUnclamp(feeder))
                        return MotionGuardRuleHelpers.Block(
                            "InputFeederY",
                            "InputFeederY HOME blocked. InputFeeder must be unclamped.",
                            out reason);
                }

                // 인터락 조건: 실장비에서 Ring Check가 감지되면 FeederY 홈 이동을 차단한다.
                if (!feeder.IsWaferFeederSimulationOrDryRun() && feeder.IsWaferFeederRingCheck())
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder ring check is detected.",
                        out reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "Exception occurred while verifying InputFeederY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        #endregion

        #region Lift 실린더

        // 인터락 항목: InputFeederLift 실린더 이동 종류별로 초기화/일반 이동 조건을 선택한다.
        private static bool VerifyInputFeederLift(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (!VerifyInputFeederLiftMaterialClear(request, out reason))
                    return false;

                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeInputFeederLift(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveInputFeederLift(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "Exception occurred while verifying InputFeederLift cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 절대 인터락: 기존 Ring/Override 센서 또는 Material 데이터가 있으면 Lift 양방향을 모두 차단한다.
        private static bool VerifyInputFeederLiftMaterialClear(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            InputFeederUnit feeder = request != null && request.Machine != null
                ? request.Machine.InputFeederUnit
                : null;
            return VerifyInputFeederMaterialClear(feeder, "InputFeederLift", out reason);
        }

        // 전체 초기화 Preflight와 Lift Guard가 동일한 자재/센서 판정을 사용한다.
        internal static bool VerifyInputFeederMaterialClear(
            InputFeederUnit feeder,
            string movingName,
            out string reason)
        {
            reason = string.Empty;
            if (feeder == null)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 절대 인터락 확인 불가: InputFeederUnit 정보가 없습니다.",
                    out reason);

            if (!feeder.IsWaferFeederSimulationOrDryRun())
            {
                if (feeder.WaferFeederRingCheckSensor == null)
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 절대 인터락 확인 불가: InputFeeder Ring/Override 센서가 없습니다.",
                        out reason);

                int errorCode;
                if (!AjinIoScanService.TryReadHardwareInput(feeder.WaferFeederRingCheckSensor, out errorCode))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 절대 인터락 확인 불가: InputFeeder Ring/Override 센서 갱신 실패. error=" + errorCode,
                        out reason);
            }

            bool dataEmpty = feeder.IsWaferFeederTransferDataEmpty();
            bool ringDetected = feeder.IsWaferFeederRingCheck();
            if (!dataEmpty || ringDetected)
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: Feeder 자재 데이터 또는 기존 Ring/Override 센서가 감지되었습니다. " +
                    "dataEmpty=" + dataEmpty + ", ring=" + ringDetected,
                    out reason);

            return true;
        }

        // 인터락 항목: InputFeederLift 초기화는 FeederY 안전 위치, LifterZ, Clamp/자재 상태를 확인한다.
        private static bool CanInitializeInputFeederLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            string direction = targetValue >= 0.5 ? "Fwd/Up" : "Bwd/Down";

            if (machine.InputCassetteUnit != null &&
                machine.InputCassetteUnit.InputLifterZ != null &&
                machine.InputCassetteUnit.InputLifterZ.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputLifterZ is moving. InputFeederLift initialize move is blocked. direction=" + direction,
                    out reason);
            }

            if (machine.InputFeederUnit != null &&
                machine.InputFeederUnit.FeederY != null &&
                machine.InputFeederUnit.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederY is moving. InputFeederLift initialize move is blocked. direction=" + direction,
                    out reason);
            }

            if (machine.InputFeederUnit != null && IsFeederUnclamp(machine.InputFeederUnit) == false)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederLift initialize move blocked. InputFeeder must be unclamped before lift initialize. direction=" + direction,
                    out reason);
            }

            if (targetValue >= 0.5 &&
                machine.InputFeederUnit != null &&
                machine.InputFeederUnit.IsWaferFeederTransferDataOccupied())
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederLift initialize Fwd/Up blocked. InputFeeder material data exists before lift up.",
                    out reason);
            }

            if (targetValue >= 0.5 &&
                machine.InputFeederUnit != null &&
                !machine.InputFeederUnit.IsWaferFeederSimulationOrDryRun() &&
                machine.InputFeederUnit.IsWaferFeederRingCheck())
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederLift initialize Fwd/Up blocked. InputFeeder wafer detect sensor is ON before lift up.",
                    out reason);
            }

            return true;
        }

        // 인터락 항목: InputFeederLift 이동은 FeederY 안전 위치, LifterZ, Clamp 상태를 확인한다.
        private static bool CanMoveInputFeederLift(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            string direction = targetValue >= 0.5 ? "Fwd/Up" : "Bwd/Down";

            if (machine.InputCassetteUnit != null &&
                machine.InputCassetteUnit.InputLifterZ != null &&
                machine.InputCassetteUnit.InputLifterZ.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputLifterZ is moving. InputFeederLift move is blocked. direction=" + direction,
                    out reason);
            }

            if (machine.InputFeederUnit != null &&
                machine.InputFeederUnit.FeederY != null &&
                machine.InputFeederUnit.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederY is moving. InputFeederLift move is blocked. direction=" + direction,
                    out reason);
            }

            // Todo : Data 이송 처리 기능 구현하고 
            //if (machine.InputFeederUnit != null &&
            //    IsFeederUnclamp(machine.InputFeederUnit) &&
            //    IsFeederHoldingMaterial(machine.InputFeederUnit))
            //{
            //    return MotionGuardRuleHelpers.Block(
            //        "InputFeederLift",
            //        "InputFeederLift move blocked. InputFeeder is unclamped and material is still detected. direction=" + direction,
            //        out reason);
            //}

            return true;
        }

        #endregion

        #region Clamp 실린더

        // 인터락 항목: InputFeederClamp 실린더 이동 종류별로 초기화/일반 이동 조건을 선택한다.
        private static bool VerifyInputFeederClamp(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            try
            {
                switch (request.MoveKind)
                {
                    case MotionGuardMoveKind.CylinderInitialize:
                        return CanInitializeInputFeederClamp(request.Machine, request.TargetValue, out reason);
                    case MotionGuardMoveKind.CylinderMove:
                        return CanMoveInputFeederClamp(request.Machine, request.TargetValue, out reason);
                    default:
                        return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
                }
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederClamp",
                    "Exception occurred while verifying InputFeederClamp cylinder rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: InputFeederClamp 초기화는 일반 Clamp 이동 조건과 동일하게 확인한다.
        private static bool CanInitializeInputFeederClamp(CDT320_Machine machine, double targetValue, out string reason)
        {
            return CanMoveInputFeederClamp(machine, targetValue, out reason);
        }

        // 인터락 항목: InputFeederClamp 이동은 FeederY 안전 위치, LifterZ, Lift Down 상태를 확인한다.
        private static bool CanMoveInputFeederClamp(CDT320_Machine machine, double targetValue, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            string direction = targetValue >= 0.5 ? "Fwd/Clamp" : "Bwd/Unclamp";

            if (machine.InputCassetteUnit != null &&
                machine.InputCassetteUnit.InputLifterZ != null &&
                machine.InputCassetteUnit.InputLifterZ.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederClamp",
                    "InputLifterZ is moving. InputFeederClamp move is blocked. direction=" + direction,
                    out reason);
            }

            if (machine.InputFeederUnit != null &&
                machine.InputFeederUnit.FeederY != null &&
                machine.InputFeederUnit.FeederY.IsMoving)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederClamp",
                    "InputFeederY is moving. InputFeederClamp move is blocked. direction=" + direction,
                    out reason);
            }

            return true;
        }

        #endregion

        #region 연관 장비 및 센서 상태

        // 인터락 기준: InputFeederY 이동 전 InputStageY가 로드 또는 언로드 위치인지 판단한다.
        private static bool IsInputStageYAtLoadOrUnload(InputStageUnit stage)
        {
            if (stage == null || stage.StageY == null)
                return true;

            StageAxisPositions waferY = stage.Recipe != null ? stage.Recipe.WaferY : null;
            return (waferY != null && IsAt(stage.StageY, waferY.ReadyPosition))
                   || (waferY != null && IsAt(stage.StageY, waferY.LoadPosition))
                   || (waferY != null && IsAt(stage.StageY, waferY.UnloadPosition));
        }

        // 인터락 기준: InputFeederY 이동 전 InputStageT가 로드 또는 언로드 위치인지 판단한다.
        private static bool IsInputStageTAtLoadOrUnload(InputStageUnit stage)
        {
            if (stage == null || stage.StageT == null)
                return true;

            StageAxisPositions waferT = stage.Recipe != null ? stage.Recipe.WaferT : null;
            return (waferT != null && IsAt(stage.StageT, waferT.ReadyPosition))
                   || (waferT != null && IsAt(stage.StageT, waferT.LoadPosition))
                   || (waferT != null && IsAt(stage.StageT, waferT.UnloadPosition));
        }

        // 인터락 기준: InputFeederY 이동 전 ExpanderZ가 로드 또는 언로드 위치인지 판단한다.
        private static bool IsExpanderZAtLoadOrUnload(InputStageUnit stage)
        {
            if (stage == null || stage.ExpanderZ == null)
                return true;

            StageAxisPositions waferZ = stage.Recipe != null ? stage.Recipe.WaferZ : null;
            return waferZ != null && (IsAt(stage.ExpanderZ, waferZ.LoadPosition) || IsAt(stage.ExpanderZ, waferZ.UnloadPosition));
        }

        // 인터락 기준: InputFeederY 이동 전 InputVisionX가 Avoid 위치인지 판단한다.
        private static bool IsInputVisionXInAvoidPosition(InputStageUnit stage)
        {
            if (stage == null)
                return true;

            StageAxisPositions visionX = stage.Recipe != null ? stage.Recipe.VisionX : null;
            return visionX != null && IsAt(stage.CameraX, visionX.AvoidPosition);
        }

        // 인터락 기준: InputFeeder가 자재를 들고 있는지 Vacuum/센서 상태로 판단한다.
        private static bool IsFeederHoldingMaterial(InputFeederUnit feeder)
        {
            if (feeder == null)
                return false;

            if (feeder.IsWaferFeederTransferDataOccupied())
                return true;

            if (!feeder.IsWaferFeederSimulationOrDryRun() && feeder.IsWaferFeederRingCheck())
                return true;

            return false;
        }

        // 인터락 기준: InputFeederY 홈 전 InputVisionX가 홈 완료 또는 홈 위치인지 판단한다.
        private static bool IsInputVisionXHomeReadyForInputFeederHome(InputStageUnit stage, out string reason)
        {
            reason = string.Empty;
            if (stage == null)
                return true;

            return IsAxisNotHomedOrAtHomePosition(stage.CameraX, "InputVisionX", out reason);
        }

        // 인터락 기준: InputFeederY 홈 전 FrontPickerX가 홈 완료 또는 홈 위치인지 판단한다.
        private static bool IsFrontPickerXHomeReadyForInputFeederHome(PickerFrontUnit picker, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            return IsAxisNotHomedOrAtHomePosition(picker.PickerX, "FrontPickerX", out reason);
        }

        // 인터락 기준: InputFeederY 홈 전 RearPickerX가 홈 완료 또는 홈 위치인지 판단한다.
        private static bool IsRearPickerXHomeReadyForInputFeederHome(PickerRearUnit picker, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            return IsAxisNotHomedOrAtHomePosition(picker.PickerX, "RearPickerX", out reason);
        }

        // 인터락 기준: 축이 홈 미완료이거나 홈 위치에 있으면 Feeder 홈 준비로 인정한다.
        private static bool IsAxisNotHomedOrAtHomePosition(BaseAxis axis, string axisName, out string reason)
        {
            return MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(axis, axisName, out reason);
        }

        // 인터락 항목: InputFeederY 홈 전 Feeder 위 자재 존재 여부와 Vacuum 상태를 확인한다.
        private static bool VerifyInputFeederEmptyForHome(InputFeederUnit feeder, out string reason)
        {
            reason = string.Empty;

            try
            {
                WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
                if (wafer != null)
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder material data exists. waferId=" +
                        wafer.WaferId + ", state=" + wafer.State,
                        out reason);
                }

                if (feeder != null &&
                    !feeder.IsWaferFeederSimulationOrDryRun() &&
                    feeder.IsWaferFeederRingCheck())
                {
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder wafer detect sensor is ON while material data is empty.",
                        out reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "Exception occurred while checking InputFeeder material before home: " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 기준: FrontPicker가 Avoid 위치인지 판단한다.
        private static bool IsFrontPickerInAvoidPosition(PickerFrontUnit picker)
        {
            if (picker == null)
                return true;

            return picker.IsFrontPickerInAvoidPosition();
        }

        // 인터락 기준: RearPicker가 Avoid 위치인지 판단한다.
        private static bool IsRearPickerInAvoidPosition(PickerRearUnit picker)
        {
            if (picker == null)
                return true;

            return picker.IsRearPickerInAvoidPosition();
        }

        // 인터락 기준: InputFeeder Lift가 Up 상태인지 센서/실린더 상태로 판단한다.
        private static bool IsFeederUp(InputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsWaferFeederUp())
                return true;

            BaseCylinder cylinder = feeder.InputFeederLift;
            return cylinder != null && cylinder.IsFwd;
        }

        // 인터락 기준: InputFeeder Clamp가 Unclamp 상태인지 센서/실린더 상태로 판단한다.
        private static bool IsFeederUnclamp(InputFeederUnit feeder)
        {
            if (feeder == null)
                return true;

            if (feeder.IsWaferFeederUnclamp())
                return true;

            BaseCylinder cylinder = feeder.InputFeederClamp;
            return cylinder != null && cylinder.IsBwd;
        }

        // 인터락 기준: 축 Actual 위치가 지정 위치 허용오차 안인지 판단한다.
        private static bool IsAt(BaseAxis axis, double target)
        {
            if (axis == null || double.IsNaN(target) || double.IsInfinity(target))
                return false;

            return Math.Abs(axis.ActualPosition - target) <= PositionTolerance;
        }

        private static bool ShouldBypassHardwareMechanismChecks()
        {
            try
            {
                AppSettings settings = AppSettingsStore.Current ?? AppSettingsStore.Load();
                return settings == null ||
                       settings.BypassHardware ||
                       settings.SimulationMode ||
                       !settings.UseAjin ||
                       !AjinFactory.IsRealBoardReady;
            }
            catch
            {
                return true;
            }
        }

        #endregion

        #region 차단 로그

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "InputFeederInterlock", reason + " - Blocked");
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
