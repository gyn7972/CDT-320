using QMC.Common;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public static class InputStageInterlockRules
    {
        // 현재 기준: InputStage 축별 홈/수동/자동 인터락을 이 파일에서 분기한다.
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferStageY", "StageY", "WaferY"))
                return VerifyWaferStageY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferStageT", "StageT", "WaferT"))
                return VerifyWaferStageT(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "WaferExpandingZ", "InputExpandingZ", "ExpanderZ"))
                return VerifyWaferExpandingZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "InputVisionX", "CameraX"))
                return VerifyWaferVisionX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NeedleX", "NeedleBlockX"))
                return VerifyNeedleX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "NeedleZ"))
                return VerifyNeedleZ(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "EjectPinZ"))
                return VerifyEjectPinZ(request, out reason);

            return true;
        }

        private static bool VerifyWaferStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoWaferStageY(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualWaferStageY(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeWaferStageY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanAutoWaferStageY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "WaferStageY", out reason))
                return false;

            if (!VerifyEjectPinZAtZeroOrAvoid(machine, "WaferStageY", out reason))
                return false;

            if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageY", out reason))
                return false;

            if (!VerifyInputStageWorkArea(request, WaferStageAxis.WaferY, "WaferStageY", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "WaferStageY", out reason);
        }

        // WaferStageY 이동 전제(Wafer Feeder): Ring Check==true, Unclamp==true, Overload==false.
        // 세 조건 중 하나라도 아니면 차단/알람.
        private static bool VerifyWaferFeederReadyForStageY(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder == null)
                    return true;

                // 1. Wafer Feeder Ring Check == true
                if (!feeder.IsWaferFeederRingCheck())
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: Wafer Feeder Ring Check가 감지되지 않았습니다.",
                        out reason);

                // 2. Wafer Feeder Unclamp == true
                //if (!feeder.IsWaferFeederUnclamp())
                //    return MotionGuardRuleHelpers.Block(
                //        movingName,
                //        movingName + " 이동 불가: Wafer Feeder가 Unclamp 상태가 아닙니다.",
                //        out reason);

                // 3. Wafer Feeder Overload == false
                if (feeder.IsWaferFeederOverload())
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: Wafer Feeder Overload가 감지되었습니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying Wafer Feeder state for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // WaferStageY/NeedleX 이동 전제: NeedlePinZ(EjectPinZ)는 0 이하 또는 Avoid 위치여야 한다.
        private static bool VerifyEjectPinZAtZeroOrAvoid(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage == null || stage.EjectPinZ == null)
                    return true;

                var pos = stage.Recipe != null ? stage.Recipe.EjectPinZ : null;
                if (pos == null)
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: EjectPinZ 레시피 위치가 없습니다.",
                        out reason);

                double tolerance = stage.EjectPinZ.Config != null && stage.EjectPinZ.Config.InPositionTolerance > 0.0
                    ? stage.EjectPinZ.Config.InPositionTolerance
                    : 0.05;

                double actual = stage.EjectPinZ.ActualPosition;
                if (actual <= 0.0 + tolerance ||
                    System.Math.Abs(actual - pos.AvoidPosition) <= tolerance)
                    return true;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: EjectPinZ(NeedlePinZ)는 0 이하 또는 Avoid 위치여야 합니다. actual=" + actual.ToString("F3") +
                    ", zero=0.000, avoid=" + pos.AvoidPosition.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying EjectPinZ zero/avoid for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        private static bool VerifyWaferStageT(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoWaferStageT(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualWaferStageT(request.Machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeWaferStageT(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanAutoWaferStageT(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "WaferStageT", out reason))
                return false;

            if (!VerifyInputFeederYAvoid(machine, "WaferStageT", out reason))
                return false;

            if (!VerifyEjectPinZNotAboveProcess(machine, "WaferStageT", out reason))
                return false;

            if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageT", out reason))
                return false;

            if (!VerifyInputStageWorkArea(request, WaferStageAxis.WaferT, "WaferStageT", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "WaferStageT", out reason);
        }

        // WaferStageT 이동 전제: EjectPinZ가 Process 위치이면 차단/알람.
        // 이동 전제: EjectPinZ Actual이 Process 위치보다 (허용오차 초과) 크면 차단/알람.
        // Process 오차 이내(|actual-process| <= tol) 또는 그 이하면 통과.
        private static bool VerifyEjectPinZNotAboveProcess(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage == null || stage.EjectPinZ == null)
                    return true;

                var pos = stage.Recipe != null ? stage.Recipe.EjectPinZ : null;
                if (pos == null)
                    return true;

                double tolerance = stage.EjectPinZ.Config != null && stage.EjectPinZ.Config.InPositionTolerance > 0.0
                    ? stage.EjectPinZ.Config.InPositionTolerance
                    : 0.05;

                double actual = stage.EjectPinZ.ActualPosition;

                // Process 오차 이내면 통과(Process 위치로 간주).
                if (System.Math.Abs(actual - pos.ProcessPosition) <= tolerance)
                    return true;

                // Process보다 (오차 초과) 크면 차단.
                if (actual > pos.ProcessPosition + tolerance)
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: EjectPinZ가 Process 위치보다 큽니다. actual=" + actual.ToString("F3") +
                        ", process=" + pos.ProcessPosition.ToString("F3"),
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying EjectPinZ Process for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        private static bool VerifyWaferExpandingZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 현재 기준: ExpanderZ Home은 하강(-) 홈이며 위치 조건 없이 최우선 허용한다.
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeWaferExpandingZ(request.Machine, out reason);

                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualWaferExpandingZ(request, out reason);

                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoWaferExpandingZ(request, out reason);
                
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeWaferExpandingZ(CDT320_Machine machine, out string reason)
        {
            // 현재 기준: ExpanderZ Home은 어느 위치에서도 허용하며 실제 홈 방향은 축 설정의 NEG를 따른다.
            reason = string.Empty;
            return true;
        }

        private static bool CanManualWaferExpandingZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;

            // 현재 기준: ExpanderZ가 플러스 방향으로 올라갈 때 Input 존 PickerZ가 0 이상 또는 Avoid여야 한다.
            bool movingPositive = IsExpanderZMovingPositive(request);

            // 현재 기준: Picker Input 영역 조건은 Front/Rear 모두 동일하게 적용한다.
            // 현재 기준: FrontPicker가 Input 영역이고 ExpanderZ가 플러스 방향이면 FrontPickerZ 0 이상 또는 Avoid 조건을 확인한다.
            if (!VerifyFrontPickerClearForExpanderZ(machine, machine != null ? machine.PickerFrontUnit : null, movingPositive, out reason))
                return false;
            // 현재 기준: RearPicker가 Input 영역이고 ExpanderZ가 플러스 방향이면 RearPickerZ 0 이상 또는 Avoid 조건을 확인한다.
            if (!VerifyRearPickerClearForExpanderZ(machine, machine != null ? machine.PickerRearUnit : null, movingPositive, out reason))
                return false;

            // 현재 기준: ExpanderZ가 플러스 방향으로 올라갈 때 Input 존 PickerZ0~Z3는 0 이상 또는 Avoid여야 한다.
            if (!PickerZoneInterlockRules.VerifyPickerZAtOrAboveZeroForZoneStageZMove(
                machine,
                PickerWorkZone.Input,
                "ExpanderZ",
                movingPositive,
                out reason))
                return false;

            // 현재 기준: InputFeederY가 이동 중이면 ExpanderZ 이동을 차단한다.
            if (!VerifyInputFeederClear(machine, "ExpanderZ", out reason))
                return false;

            // 현재 기준: StageT는 Home(0), Avoid, Process 위치에서만 ExpanderZ 이동을 허용한다.
            if (!VerifyStageTZeroAvoidOrProcessForExpanderZ(machine, out reason))
                return false;

            // 현재 기준: InputFeederY는 Home(0) 또는 안전 위치(Avoid/Unload)에서만 ExpanderZ 이동을 허용한다.
            if (!VerifyFeederYHomeOrSafeForExpanderZ(machine, out reason))
                return false;

            // 기존 조건: InputStage 다른 축이 동작 중이면 ExpanderZ 이동을 차단한다.
            return VerifyInputStageNotBusy(stage, "ExpanderZ", out reason);
        }

        private static bool CanAutoWaferExpandingZ(MotionGuardRuleContext request, out string reason)
        {
            // 현재 기준: Auto ExpanderZ는 우선 Manual ExpanderZ와 동일 조건으로 검사한다.
            // 기존 조건: Auto에서 InputVisionX Avoid 확인(VerifyInputVisionXClearForExpanderZ)을 추가로 수행했다.
            // 현재 필요 여부: 사용 안 함. Manual과 동일하게 맞추기 위해 호출하지 않는다.
            return CanManualWaferExpandingZ(request, out reason);
        }

        // ExpanderZ 이동 전제 ①: StageT가 Home(0), Avoid, Process 위치 중 하나여야 한다.
        private static bool VerifyStageTZeroAvoidOrProcessForExpanderZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage == null || stage.StageT == null)
                    return true;

                StageAxisPositions waferT = stage.Recipe != null ? stage.Recipe.WaferT : null;
                if (waferT == null)
                    return MotionGuardRuleHelpers.Block(
                        "ExpanderZ",
                        "ExpanderZ 이동 불가: WaferStageT 레시피 위치가 없습니다.",
                        out reason);

                double tolerance = ResolveAxisPositionTolerance(stage.StageT);
                double actual = stage.StageT.ActualPosition;
                if (System.Math.Abs(actual - 0.0) <= tolerance ||
                    System.Math.Abs(actual - waferT.AvoidPosition) <= tolerance ||
                    System.Math.Abs(actual - waferT.ProcessPosition) <= tolerance)
                    return true;

                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ 이동 불가: WaferStageT가 Home(0)/Avoid/Process 위치가 아닙니다. actual=" +
                    actual.ToString("F3") + ", home=0.000, avoid=" + waferT.AvoidPosition.ToString("F3") +
                    ", process=" + waferT.ProcessPosition.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "Exception occurred while verifying WaferStageT position for ExpanderZ: " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // ExpanderZ 이동 전제 ②: InputFeederY가 Home(0) 또는 안전 위치(Avoid/Unload)여야 한다.
        // 기존 조건: InputFeederY는 Avoid 또는 StageUnload만 허용했다.
        // 현재 필요 여부: Home(0)도 안전 위치로 포함해야 하므로 현재 함수로 대체한다.
        private static bool VerifyFeederYHomeOrSafeForExpanderZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder != null &&
                    !feeder.IsWaferFeederYInHomePosition() &&
                    !feeder.IsWaferFeederYInAvoidPosition() &&
                    !feeder.IsWaferFeederYInStageUnloadPosition())
                    return MotionGuardRuleHelpers.Block(
                        "ExpanderZ",
                        "ExpanderZ 이동 불가: InputFeederY가 Home(0) 또는 안전 위치(Avoid/Unload)가 아닙니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "Exception occurred while verifying InputFeederY safe position for ExpanderZ: " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // ExpanderZ 이동 전제 ③: Front/Rear Picker Z0~Z3가 모두 Avoid 위치여야 한다(아니면 차단/알람).
        private static bool VerifyFrontRearPickerZAvoidForExpanderZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!VerifyPickerZAvoidForExpanderZ(machine != null ? machine.PickerFrontUnit : null, "Front", out reason))
                    return false;

                if (!VerifyPickerZAvoidForExpanderZ(machine != null ? machine.PickerRearUnit : null, "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "Exception occurred while verifying Front/Rear PickerZ avoid for ExpanderZ: " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        private static bool VerifyPickerZAvoidForExpanderZ(PickerFrontUnit picker, string prefix, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        "ExpanderZ",
                        "ExpanderZ 이동 불가: " + prefix + zAxis + "가 Avoid 위치가 아닙니다.",
                        out reason);
            }

            return true;
        }

        private static bool VerifyPickerZAvoidForExpanderZ(PickerRearUnit picker, string prefix, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        "ExpanderZ",
                        "ExpanderZ 이동 불가: " + prefix + zAxis + "가 Avoid 위치가 아닙니다.",
                        out reason);
            }

            return true;
        }

        private static bool VerifyWaferVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoInputVisionX(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualInputVisionX(request.Machine, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeInputVisionX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanAutoInputVisionX(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "InputVisionX", out reason))
                return false;

            // InputFeederY가 Avoid이고 Wafer Feeder Down 센서가 감지되어야만 InputVisionX 이동 가능.
            if (!VerifyFeederYAvoidAndDownForInputVisionX(machine, out reason))
                return false;

            // Picker 전체 Avoid를 강제하지 않는다. 실제 Input 존 점유/간섭만 차단한다.
            if (!VerifyFrontRearPickerInputZoneClearForInputVisionX(machine, out reason))
                return false;

            // InputVisionX는 카메라/캘리브레이션/티칭 위치 때문에 wafer 작업 원 밖으로 이동할 수 있어야 한다.
            // 공유레일/피커 Input zone/피더 인터락은 위 조건에서 유지하고, 원형 작업영역 체크만 적용하지 않는다.

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "InputVisionX", out reason);
        }

        // InputVisionX 이동 전제 ①: InputFeederY가 Avoid 위치 + Wafer Feeder Down 센서 감지. (둘 다 만족해야 함)
        private static bool VerifyFeederYAvoidAndDownForInputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder == null)
                    return true;

                if (!feeder.IsWaferFeederYInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX 이동 불가: InputFeederY가 Avoid 위치가 아닙니다.",
                        out reason);

                if (!feeder.IsWaferFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX 이동 불가: Wafer Feeder Down 센서가 감지되지 않았습니다.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputVisionX",
                    "Exception occurred while verifying InputFeederY avoid/down for InputVisionX: " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // InputVisionX 이동 전제 ②: Front/Rear Picker가 실제 Input 영역을 점유하거나 간섭하면 안 된다.
        private static bool VerifyFrontRearPickerInputZoneClearForInputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!VerifyPickerInputZoneClearForInputVisionX(machine, true, "Front", out reason))
                    return false;

                if (!VerifyPickerInputZoneClearForInputVisionX(machine, false, "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputVisionX",
                    "InputVisionX 이동 전 Picker Input 영역 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        private static bool VerifyPickerInputZoneClearForInputVisionX(CDT320_Machine machine, bool isFront, string prefix, out string reason)
        {
            reason = string.Empty;
            if (machine == null)
                return true;

            PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                machine,
                isFront,
                PickerWorkZone.Input,
                null,
                "InputVisionX 이동 전 Picker Input 영역 확인");

            bool xMoving = state != null && state.PickerX != null && state.PickerX.IsMoving;
            bool yMoving = state != null && state.PickerY != null && state.PickerY.IsMoving;
            bool movingIntoOrInsideInput = IsPickerInputZoneMotionRisk(state, xMoving, yMoving);
            bool blocking = state != null && (state.BlocksTransport || movingIntoOrInsideInput);
            string detail =
                "movingX=" + xMoving +
                ", movingY=" + yMoving +
                ", movingInputRisk=" + movingIntoOrInsideInput +
                ", " + (state != null ? state.Describe() : "state=null");

            if (!blocking)
                return true;

            return MotionGuardRuleHelpers.Block(
                "InputVisionX",
                "InputVisionX 이동 불가: " + prefix + "Picker가 Input 영역을 점유하거나 간섭 중입니다. " + detail,
                out reason);
        }

        private static bool IsPickerInputZoneMotionRisk(PickerZoneTransportState state, bool xMoving, bool yMoving)
        {
            if (!xMoving && !yMoving)
                return false;

            if (state == null)
                return true;

            return state.CurrentZone == PickerWorkZone.Input ||
                   state.TargetZone == PickerWorkZone.Input ||
                   state.CurrentZone == PickerWorkZone.Unknown ||
                   state.TargetZone == PickerWorkZone.Unknown ||
                   state.UnknownUnsafe;
        }

        private static bool VerifyNeedleX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoNeedleX(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualNeedleX(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeNeedleX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanAutoNeedleX(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "NeedleX", out reason))
                return false;

            if (!VerifyEjectPinZAtZeroOrAvoid(machine, "NeedleX", out reason))
                return false;

            if (!VerifyInputStageWorkArea(request, WaferStageAxis.NeedleX, "NeedleX", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "NeedleX", out reason);
        }

        private static bool CanManualInputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX HOME blocked. InputFeederY must be at Avoid position.",
                        out reason);

                if (feeder != null && !feeder.IsWaferFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX HOME blocked. InputFeeder lift cylinder must be down.",
                        out reason);

                // Picker 전체 Avoid를 강제하지 않는다. 실제 Input 존 점유/간섭만 차단한다.
                if (!VerifyFrontRearPickerInputZoneClearForInputVisionX(machine, out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputVisionX",
                    "Exception occurred while verifying InputVisionX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanHomeInputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX HOME blocked. InputFeederY must be at Avoid position.",
                        out reason);

                if (feeder != null && !feeder.IsWaferFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX HOME blocked. InputFeeder lift cylinder must be down.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputVisionX",
                    "Exception occurred while verifying InputVisionX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanManualWaferStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;

                if (!VerifyEjectPinZAtZeroOrAvoid(machine, "WaferStageY", out reason))
                    return false;

                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputStageY",
                        "InputStageY HOME blocked. InputFeederY must be at Avoid position.",
                        out reason);

                if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageY", out reason))
                    return false;

                if (!VerifyInputStageWorkArea(request, WaferStageAxis.WaferY, "WaferStageY", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageY", "Front", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerRearUnit : null, "InputStageY", "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputStageY",
                    "Exception occurred while verifying InputStageY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanManualWaferStageT(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsNeedleZInSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputStageT",
                        "InputStageT HOME blocked. NeedleZ must be at Avoid position.",
                        out reason);

                if (!VerifyInputFeederYAvoid(machine, "WaferStageT", out reason))
                    return false;

                if (!VerifyEjectPinZNotAboveProcess(machine, "WaferStageT", out reason))
                    return false;

                if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageT", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageT", "Front", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerRearUnit : null, "InputStageT", "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputStageT",
                    "Exception occurred while verifying InputStageT home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanHomeWaferStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsNeedleZInHomeOrSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputStageY",
                        "InputStageY HOME blocked. NeedleZ must be at 0 or below, or Avoid position.",
                        out reason);

                //여기 조건에 따라 다르다.
                //InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                //if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
                //    return MotionGuardRuleHelpers.Block(
                //        "InputStageY",
                //        "InputStageY HOME blocked. InputFeederY must be at Avoid position.",
                //        out reason);

                if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageY", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageY", "Front", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerRearUnit : null, "InputStageY", "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputStageY",
                    "Exception occurred while verifying InputStageY home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanHomeWaferStageT(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsNeedleZInSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputStageT",
                        "InputStageT HOME blocked. NeedleZ must be at Avoid position.",
                        out reason);

                if (!VerifyInputFeederYAvoid(machine, "WaferStageT", out reason))
                    return false;

                if (!VerifyEjectPinZNotAboveProcess(machine, "WaferStageT", out reason))
                    return false;

                if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageT", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageT", "Front", out reason))
                    return false;

                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerRearUnit : null, "InputStageT", "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputStageT",
                    "Exception occurred while verifying InputStageT home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanManualNeedleX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;

                if (!VerifyEjectPinZAtZeroOrAvoid(machine, "NeedleX", out reason))
                    return false;

                if (!VerifyInputStageWorkArea(request, WaferStageAxis.NeedleX, "NeedleX", out reason))
                    return false;

                return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "NeedleX", out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "NeedleX",
                    "Exception occurred while verifying NeedleX manual move rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool CanHomeNeedleX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (!VerifyEjectPinZAtZeroOrAvoid(machine, "NeedleX", out reason))
                    return false;

                if (stage != null && !stage.IsNeedleZInHomeOrSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "NeedleX",
                        "NeedleX HOME blocked. NeedleZ must be at 0 or below, or Avoid position.",
                        out reason);

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "NeedleX",
                    "Exception occurred while verifying NeedleX home rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool VerifyNeedleZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoNeedleZ(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualNeedleZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeNeedleZ(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeNeedleZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanManualNeedleZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;

            if (!VerifyInputStageWorkArea(request, WaferStageAxis.NeedleZ, "NeedleZ", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "NeedleZ", out reason);
        }

        private static bool CanAutoNeedleZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "NeedleZ", out reason))
                return false;

            if (!VerifyInputStageWorkArea(request, WaferStageAxis.NeedleZ, "NeedleZ", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "NeedleZ", out reason);
        }

        private static bool VerifyEjectPinZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoEjectPinZ(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualEjectPinZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeEjectPinZ(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeEjectPinZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanManualEjectPinZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            return VerifyEjectPinZManualMoveSafe(request, "EjectPinZ", out reason);
        }

        private static bool CanAutoEjectPinZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!VerifyInputFeederClear(machine, "EjectPinZ", out reason))
                return false;

            if (IsContinuousJogMove(request) &&
                !VerifyEjectPinZManualMoveSafe(request, "EjectPinZ", out reason))
                return false;

            if (!IsContinuousJogMove(request) &&
                !VerifyInputStageWorkArea(request, WaferStageAxis.EjectPinZ, "EjectPinZ", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "EjectPinZ", out reason);
        }

        private static bool VerifyEjectPinZManualMoveSafe(MotionGuardRuleContext request, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = request != null && request.Machine != null ? request.Machine.InputStageUnit : null;
                if (stage == null || stage.EjectPinZ == null)
                    return true;

                var pos = stage.Recipe != null ? stage.Recipe.EjectPinZ : null;
                if (pos == null)
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: EjectPinZ 레시피 위치가 없습니다.",
                        out reason);

                double tolerance = stage.EjectPinZ.Config != null && stage.EjectPinZ.Config.InPositionTolerance > 0.0
                    ? stage.EjectPinZ.Config.InPositionTolerance
                    : 0.05;

                double actual = stage.EjectPinZ.ActualPosition;
                double target = request != null ? request.TargetValue : actual;
                if (System.Math.Abs(actual - pos.AvoidPosition) <= tolerance)
                    return true;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 조그/수동 이동 불가: EjectPinZ(NeedlePinZ)는 현재 Avoid 위치일 때만 움직일 수 있습니다. actual=" +
                    actual.ToString("F3") + ", target=" + target.ToString("F3") +
                    ", avoid=" + pos.AvoidPosition.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying EjectPinZ manual move for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        private static bool IsContinuousJogMove(MotionGuardRuleContext request)
        {
            if (request == null)
                return false;

            return request.Intent != null && request.Intent.ContinuousJog;
        }

        private static bool VerifyInputStageWorkArea(MotionGuardRuleContext request, WaferStageAxis axis, string movingName, out string reason)
        {
            reason = string.Empty;
            InputStageUnit stage = request != null && request.Machine != null ? request.Machine.InputStageUnit : null;
            if (stage == null)
                return true;

            string areaReason;
            double overrideWorkAreaNeedleX;
            if (axis == WaferStageAxis.WaferY &&
                TryResolveInputStageWorkAreaNeedleX(request, out overrideWorkAreaNeedleX))
            {
                double targetY = request != null ? request.TargetValue : 0.0;
                if (!stage.VerifyNeedleZSafeForWaferYNonProcessTravel(targetY, out areaReason))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: InputStageY 비공정 위치 이동 전 NeedleZ가 반드시 Avoid 위치에 있어야 합니다. " +
                        areaReason +
                        ", overrideWorkAreaNeedleX=" + overrideWorkAreaNeedleX.ToString("F3"),
                        out reason);
                }

                // 현재 기준: StageY 이동 작업 반경은 CameraX가 아니라 NeedleX/StageY 실축 좌표로 확인한다.
                if (stage.IsNeedleWorkPointInArea(overrideWorkAreaNeedleX, targetY, out areaReason))
                    return true;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " blocked by InputStage needle work area. " + areaReason +
                    ", overrideWorkAreaNeedleX=" + overrideWorkAreaNeedleX.ToString("F3"),
                    out reason);
            }

            double overrideWorkAreaX;
            if (axis == WaferStageAxis.WaferY &&
                TryResolveInputStageWorkAreaX(request, out overrideWorkAreaX))
            {
                // 기존 조건: InputStageWorkAreaX는 Camera/VisionX 기준 힌트라 StageY 작업반경 차단에 사용하지 않는다.
                // if (stage.IsInputStageWorkPointInArea(overrideWorkAreaX, targetY, out areaReason)) ...
                // 현재 기준: WaferStageY 작업반경은 아래 공통 경로에서 NeedleX/StageY 실축 좌표로만 판단한다.
            }

            if (stage.IsInputStageAxisTargetAllowedInWorkArea(axis, request != null ? request.TargetValue : 0.0, out areaReason))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " blocked by InputStage work area. " + areaReason,
                out reason);
        }

        private static bool TryResolveInputStageWorkAreaX(MotionGuardRuleContext request, out double workAreaX)
        {
            workAreaX = 0.0;
            try
            {
                if (request == null || request.Intent == null || !request.Intent.InputStageWorkAreaX.HasValue)
                    return false;

                workAreaX = request.Intent.InputStageWorkAreaX.Value;
                return true;
            }
            catch
            {
                workAreaX = 0.0;
                return false;
            }
            finally
            {
            }
        }

        private static bool TryResolveInputStageWorkAreaNeedleX(MotionGuardRuleContext request, out double workAreaNeedleX)
        {
            workAreaNeedleX = 0.0;
            try
            {
                if (request == null || request.Intent == null || !request.Intent.InputStageWorkAreaNeedleX.HasValue)
                    return false;

                workAreaNeedleX = request.Intent.InputStageWorkAreaNeedleX.Value;
                return true;
            }
            catch
            {
                workAreaNeedleX = 0.0;
                return false;
            }
            finally
            {
            }
        }

        private static bool VerifyInputFeederClear(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
            if (feeder == null)
                return true;

            if (MotionGuardRuleHelpers.IsAxisMoving(feeder.FeederY))
                return MotionGuardRuleHelpers.Block(movingName, "InputFeederY is moving.", out reason);

            return true;
        }

        // 이동 전제: InputFeederY가 Avoid 위치여야 한다(아니면 차단/알람).
        private static bool VerifyInputFeederYAvoid(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
            if (feeder == null)
                return true;

            if (!feeder.IsWaferFeederYInAvoidPosition())
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: InputFeederY가 Avoid 위치가 아닙니다.",
                    out reason);

            return true;
        }

        private static bool VerifyInputStageNotBusy(InputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            if (stage == null)
                return true;

            // PickUp 보정 이동에서는 WaferY와 NeedleX가 같은 목표 다이에 대해 동시에 이동한다.
            if (!IsNeedleXMove(movingName) &&
                IsMovingExcept(stage.StageY, movingName, "WaferStageY", "StageY", "WaferY"))
                return MotionGuardRuleHelpers.Block(movingName, "WaferStageY is moving.", out reason);
            if (IsMovingExcept(stage.StageT, movingName, "WaferStageT", "StageT", "WaferT"))
                return MotionGuardRuleHelpers.Block(movingName, "WaferStageT is moving.", out reason);
            if (IsMovingExcept(stage.ExpanderZ, movingName, "WaferExpandingZ", "ExpanderZ"))
                return MotionGuardRuleHelpers.Block(movingName, "ExpanderZ is moving.", out reason);
            if (!IsNeedleXMove(movingName) &&
                IsMovingExcept(stage.CameraX, movingName, "InputVisionX", "CameraX"))
                return MotionGuardRuleHelpers.Block(movingName, "InputVisionX is moving.", out reason);
            if (!IsInputVisionXMove(movingName) &&
                !IsWaferStageYMove(movingName) &&
                IsMovingExcept(stage.NeedleBlockX, movingName, "NeedleX", "NeedleBlockX"))
                return MotionGuardRuleHelpers.Block(movingName, "NeedleX is moving.", out reason);

            //서로 같이 움직여도 상관없음.
            //if (IsMovingExcept(stage.NeedleZ, movingName, "NeedleZ"))
            //    return MotionGuardRuleHelpers.Block(movingName, "NeedleZ is moving.", out reason);
            //if (IsMovingExcept(stage.EjectPinZ, movingName, "EjectPinZ"))
            //    return MotionGuardRuleHelpers.Block(movingName, "EjectPinZ is moving.", out reason);

            return true;
        }

        private static bool IsInputVisionXMove(string movingName)
        {
            return string.Equals(movingName, "InputVisionX", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "CameraX", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNeedleXMove(string movingName)
        {
            return string.Equals(movingName, "NeedleX", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "NeedleBlockX", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWaferStageYMove(string movingName)
        {
            return string.Equals(movingName, "WaferStageY", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "StageY", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "WaferY", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExpanderZMovingPositive(MotionGuardRuleContext request)
        {
            try
            {
                if (request == null)
                    return false;

                InputStageUnit stage = request.Machine != null ? request.Machine.InputStageUnit : null;
                BaseAxis axis = stage != null ? stage.ExpanderZ : null;
                double tolerance = ResolveAxisPositionTolerance(axis);
                if (stage != null &&
                    stage.Recipe != null &&
                    stage.Recipe.WaferZ != null &&
                    System.Math.Abs(request.TargetValue - stage.Recipe.WaferZ.AvoidPosition) <= tolerance)
                {
                    return false;
                }

                return axis != null && request.TargetValue > axis.ActualPosition + tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static double ResolveAxisPositionTolerance(BaseAxis axis)
        {
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return 0.05;
        }

        private static bool VerifyInputVisionXClearForExpanderZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage == null)
                    return true;

                if (MotionGuardRuleHelpers.IsAxisMoving(stage.CameraX))
                    return MotionGuardRuleHelpers.Block(
                        "ExpanderZ",
                        "ExpanderZ move blocked. InputVisionX is moving.",
                        out reason);

                if (stage.IsVisionXInAvoidPosition())
                    return true;

                double actual = stage.CameraX != null ? stage.CameraX.ActualPosition : 0.0;
                double avoid = stage.Recipe != null && stage.Recipe.VisionX != null ? stage.Recipe.VisionX.AvoidPosition : 0.0;
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ move blocked. InputVisionX must be at Avoid position before StageZ moves. actual=" +
                    actual.ToString("F3") + ", avoid=" + avoid.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "Exception occurred while verifying InputVisionX clear condition for ExpanderZ: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool VerifyFrontPickerClearForExpanderZ(CDT320_Machine machine, PickerFrontUnit picker, bool targetAtOrAboveZero, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (picker == null)
                    return true;

                PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                    machine,
                    true,
                    PickerWorkZone.Input,
                    null,
                    string.Empty);
                return VerifyPickerClearForExpanderZ(
                    "FrontPicker",
                    state,
                    targetAtOrAboveZero,
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "Exception occurred while verifying FrontPicker clear condition for ExpanderZ: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool VerifyRearPickerClearForExpanderZ(CDT320_Machine machine, PickerRearUnit picker, bool targetAtOrAboveZero, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (picker == null)
                    return true;

                PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                    machine,
                    false,
                    PickerWorkZone.Input,
                    null,
                    string.Empty);
                return VerifyPickerClearForExpanderZ(
                    "RearPicker",
                    state,
                    targetAtOrAboveZero,
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "Exception occurred while verifying RearPicker clear condition for ExpanderZ: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        private static bool VerifyPickerClearForExpanderZ(
            string pickerName,
            PickerZoneTransportState state,
            bool movingPositive,
            out string reason)
        {
            reason = string.Empty;
            BaseAxis pickerX = state != null ? state.PickerX : null;
            BaseAxis pickerY = state != null ? state.PickerY : null;
            string stateText = state != null ? state.Describe() : pickerName + " state unavailable.";

            // 현재 기준: ExpanderZ가 마이너스 방향으로 내려가는 이동은 PickerX/Y 이동 상태로 차단하지 않는다.
            if (!movingPositive)
                return true;

            if (MotionGuardRuleHelpers.IsAxisMoving(pickerX))
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ move blocked. " + pickerName + "X is moving.",
                    out reason);

            if (MotionGuardRuleHelpers.IsAxisMoving(pickerY))
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ move blocked. " + pickerName + "Y is moving.",
                    out reason);

            if (state == null)
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ move blocked. " + pickerName + " zone state is unavailable.",
                    out reason);

            if (!state.IsRequestedZoneActive && !state.UnknownUnsafe)
                return true;

            if (state.UnknownUnsafe && !state.IsRequestedZoneActive)
                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ move blocked. " + pickerName + " zone is unknown while PickerY is not safe. " +
                    stateText,
                    out reason);

            // 기존 조건: Picker가 Input 영역이면 ExpanderZ 목표 0 이상 이동을 무조건 차단했다.
            // 현재 필요 여부: 사용 안 함. 현재는 ExpanderZ 목표가 0/Avoid보다 클 때 PickerZ 0 이상 또는 Avoid 조건으로 별도 확인한다.
            //if (targetAtOrAboveZero)
            //    return MotionGuardRuleHelpers.Block(
            //        "ExpanderZ",
            //        "ExpanderZ 이동 불가: " + pickerName + "가 Input 영역에 있을 때 목표 위치를 0 이상으로 이동할 수 없습니다. " +
            //        stateText +
            //        ", targetAtOrAboveZero=" + targetAtOrAboveZero,
            //        out reason);

            return true;
        }

        private static string BuildPickerZoneState(
            string pickerName,
            string encoderZone,
            BaseAxis pickerX,
            BaseAxis pickerY,
            bool pickerXAtInputZone,
            bool pickerXAtBottomZone,
            bool pickerXAtSideZone,
            bool pickerXAtOutputZone,
            bool pickerXAtInputAvoid,
            bool pickerXAtMainAvoid,
            bool pickerXAtOutputAvoid)
        {
            try
            {
                return pickerName +
                       "X=" + FormatAxisPosition(pickerX) +
                       ", " + pickerName + "Y=" + FormatAxisPosition(pickerY) +
                       ", encoderZone=" + FormatEncoderZone(encoderZone) +
                       ", inputZone=" + pickerXAtInputZone +
                       ", bottomZone=" + pickerXAtBottomZone +
                       ", sideZone=" + pickerXAtSideZone +
                       ", outputZone=" + pickerXAtOutputZone +
                       ", inputAvoid=" + pickerXAtInputAvoid +
                       ", avoid=" + pickerXAtMainAvoid +
                       ", outputAvoid=" + pickerXAtOutputAvoid;
            }
            catch
            {
                return pickerName + " state unavailable.";
            }
            finally
            {
            }
        }

        private static string ResolvePickerEncoderZone(CDT320_Machine machine, bool isFront)
        {
            try
            {
                return PickerZoneInterlockRules.ResolvePickerPhysicalZoneName(machine, isFront);
            }
            catch
            {
                return "UNKNOWN";
            }
            finally
            {
            }
        }

        private static string FormatEncoderZone(string encoderZone)
        {
            return string.IsNullOrWhiteSpace(encoderZone) ? "UNKNOWN" : encoderZone;
        }

        private static string FormatAxisPosition(BaseAxis axis)
        {
            if (axis == null)
                return "null";

            return axis.ActualPosition.ToString("F3") +
                   ", moving=" + axis.IsMoving +
                   ", servo=" + axis.IsServoOn +
                   ", alarm=" + axis.IsAlarm;
        }

        private static bool IsMovingExcept(BaseAxis axis, string movingName, params string[] names)
        {
            if (!MotionGuardRuleHelpers.IsAxisMoving(axis))
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(movingName, names[i], System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static bool VerifyPickerZAxesAvoid(PickerFrontUnit picker, string movingName, string prefix, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. " + prefix + zAxis + " must be at Avoid position.",
                        out reason);
            }

            return true;
        }

        private static bool VerifyPickerZAxesAvoid(PickerRearUnit picker, string movingName, string prefix, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. " + prefix + zAxis + " must be at Avoid position.",
                        out reason);
            }

            return true;
        }

        private static void LogBlockedReason(string reason)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    QMC.Common.Log.Write("Main", "INTERLOCK", "InputStageInterlock", reason + " - Blocked");
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
