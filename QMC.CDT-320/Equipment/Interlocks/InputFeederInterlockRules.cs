using System;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.CDT320.Ajin;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Interlocks
{
    public static class InputFeederInterlockRules
    {
        private const double PositionTolerance = 0.05;

        // 인터락 항목: InputFeederY/Lift/Clamp 이동 요청을 각 Feeder 인터락으로 라우팅한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederY", "FeederY"))
                return VerifyInputFeederY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederLift"))
                return VerifyInputFeederLift(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "InputFeederClamp"))
                return VerifyInputFeederClamp(request, out reason);

            return true;
        }

        // 인터락 항목: InputFeederY 이동 종류별로 자동/수동/홈 조건을 선택한다.
        private static bool VerifyInputFeederY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request.Machine;

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

        // 인터락 항목: 자동 InputFeederY 이동은 Stage 로드/언로드 위치, 피커 Input 존 점유, LifterZ, Lift/Clamp 상태를 확인한다.
        private static bool CanAutoInputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            InputCassetteUnit cassette = machine.InputCassetteUnit;
            InputStageUnit stage = machine.InputStageUnit;
            if (cassette != null && cassette.InputLifterZ != null && cassette.InputLifterZ.IsMoving)
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputLifterZ is moving. InputFeederY move is blocked.",
                    out reason);

            if (stage != null)
            {
                //if (!IsInputStageYAtLoadOrUnload(stage))
                //    return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage StageY must be at Loading or Unloading position.", out reason);

                if (!IsInputStageTAtLoadOrUnload(stage))
                    return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage StageT must be at Loading or Unloading position.", out reason);

                if (!IsExpanderZAtLoadOrUnload(stage))
                    return MotionGuardRuleHelpers.Block("InputFeederY", "InputStage ExpanderZ must be at Loading or Unloading position.", out reason);

                if (!IsInputVisionXInAvoidPosition(stage))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY move blocked. InputVisionX must be at Avoid position.",
                        out reason);
            }

            string pickerDetail;
            if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, true, PickerWorkZone.Input, out pickerDetail))
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY 이동 차단. FrontPicker가 Input zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                    out reason);

            if (PickerZoneInterlockRules.IsPickerBlockingZoneTransport(machine, false, PickerWorkZone.Input, out pickerDetail))
                return MotionGuardRuleHelpers.Block(
                    "InputFeederY",
                    "InputFeederY 이동 차단. RearPicker가 Input zone을 사용 중이거나 위치를 확정할 수 없습니다. " + pickerDetail,
                    out reason);

            InputFeederUnit feeder = machine.InputFeederUnit;
            if (feeder == null)
                return true;

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
                if (machine == null)
                    return true;

                InputCassetteUnit cassette = machine.InputCassetteUnit;
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

                if (!IsInputVisionXHomeReadyForInputFeederHome(machine.InputStageUnit, out axisReason))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason,
                        out reason);

                if (!IsFrontPickerXHomeReadyForInputFeederHome(machine.PickerFrontUnit, out axisReason))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. FrontPickerX must be not homed yet or at Home position. " + axisReason,
                        out reason);

                if (!IsRearPickerXHomeReadyForInputFeederHome(machine.PickerRearUnit, out axisReason))
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. RearPickerX must be not homed yet or at Home position. " + axisReason,
                        out reason);

                InputFeederUnit feeder = machine.InputFeederUnit;
                if (feeder == null)
                    return true;

                if (!VerifyInputFeederEmptyForHome(feeder, out reason))
                    return false;

                if (feeder.IsWaferFeederOverload())
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder overload sensor is detected.",
                        out reason);

                if (!ShouldBypassHardwareMechanismChecks())
                {
                    if (!IsFeederUnclamp(feeder))
                        return MotionGuardRuleHelpers.Block(
                            "InputFeederY",
                            "InputFeederY HOME blocked. InputFeeder must be unclamped.",
                            out reason);
                }

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

        // 인터락 항목: InputFeederY 홈은 카세트 돌출, 주변 X축 홈 준비, 피커 Input 존 점유, 빈 자재 상태를 확인한다.
        private static bool CanHomeInputFeederY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (machine == null)
                    return true;

                InputCassetteUnit cassette = machine.InputCassetteUnit;
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
                if (feeder == null)
                    return true;

                if (!VerifyInputFeederEmptyForHome(feeder, out reason))
                    return false;

                if (feeder.IsWaferFeederOverload())
                    return MotionGuardRuleHelpers.Block(
                        "InputFeederY",
                        "InputFeederY HOME blocked. InputFeeder overload sensor is detected.",
                        out reason);

                if (!ShouldBypassHardwareMechanismChecks())
                {
                    if (!IsFeederUnclamp(feeder))
                        return MotionGuardRuleHelpers.Block(
                            "InputFeederY",
                            "InputFeederY HOME blocked. InputFeeder must be unclamped.",
                            out reason);
                }

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

        // 인터락 항목: InputFeederLift 실린더 이동 종류별로 초기화/일반 이동 조건을 선택한다.
        private static bool VerifyInputFeederLift(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            try
            {
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

            if (machine.InputFeederUnit != null &&
                IsFeederUnclamp(machine.InputFeederUnit) &&
                IsFeederHoldingMaterial(machine.InputFeederUnit))
            {
                return MotionGuardRuleHelpers.Block(
                    "InputFeederLift",
                    "InputFeederLift move blocked. InputFeeder is unclamped and material is still detected. direction=" + direction,
                    out reason);
            }

            return true;
        }

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

    }
}
