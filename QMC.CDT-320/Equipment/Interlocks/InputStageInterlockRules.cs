using QMC.Common;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public static class InputStageInterlockRules
    {
        // 현재 기준: InputStage 축별 홈/수동/자동 인터락을 이 파일에서 분기한다.
        // 인터락 항목: InputStage의 Y/T/Z/VisionX/Needle/EjectPinZ 이동 요청을 해당 인터락으로 라우팅한다.
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

        // 인터락 항목: WaferStageY 이동 종류별로 자동/수동/홈 조건을 선택한다.
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

        // 인터락 항목: 자동 WaferStageY 이동은 수동 이동 룰을 먼저 확인한 뒤 자동 전용 조건을 추가 확인한다.
        private static bool CanAutoWaferStageY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            // 인터락 조건: 자동 StageY 이동도 수동 StageY 기본 안전 조건을 먼저 통과해야 한다.
            if (!CanManualWaferStageY(request, out reason))
                return false;

            // 인터락 조건: InputFeeder가 StageY 이동과 간섭 없는 상태인지 확인한다.
            if (!VerifyInputFeederClear(machine, "WaferStageY", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "WaferStageY", out reason);
        }

        // WaferStageY 이동 전제(Wafer Feeder): Ring Check==true, Unclamp==true, Overload==false.
        // 세 조건 중 하나라도 아니면 차단/알람.
        // 인터락 항목: StageY 이동 전 InputFeederY가 Stage 간섭 없는 준비 위치인지 확인한다.
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
        // 인터락 항목: StageY/NeedleX 이동 전 EjectPinZ가 0 이하 또는 Avoid 위치인지 확인한다.
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

        // 인터락 항목: InputStage 평면축(X/Y/T)은 ExpanderZ가 Load/Unload 높이에 있을 때 이동할 수 없다.
        private static bool VerifyExpanderZNotLoadOrUnloadForStagePlaneMove(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage == null || stage.ExpanderZ == null)
                    return true;

                var pos = stage.Recipe != null ? stage.Recipe.WaferZ : null;
                if (pos == null)
                    return true;

                double tolerance = ResolveAxisPositionTolerance(stage.ExpanderZ);
                double actual = stage.ExpanderZ.ActualPosition;
                bool atLoad = System.Math.Abs(actual - pos.LoadPosition) <= tolerance;
                bool atUnload = System.Math.Abs(actual - pos.UnloadPosition) <= tolerance;
                if (!atLoad && !atUnload)
                    return true;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: ExpanderZ가 Load/Unload 높이에 있어 X/Y/T 이동할 수 없습니다. " +
                    "먼저 ExpanderZ를 Avoid 위치로 이동하세요. actual=" + actual.ToString("F3") +
                    ", load=" + pos.LoadPosition.ToString("F3") +
                    ", unload=" + pos.UnloadPosition.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying ExpanderZ Load/Unload state for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 항목: WaferStageT 이동 종류별로 자동/수동/홈 조건을 선택한다.
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

        // 인터락 항목: 자동 WaferStageT 이동은 수동 이동 룰을 먼저 확인한 뒤 자동 전용 조건을 추가 확인한다.
        private static bool CanAutoWaferStageT(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            // 인터락 조건: 자동 StageT 이동도 수동 StageT 기본 안전 조건을 먼저 통과해야 한다.
            if (!CanManualWaferStageT(machine, out reason))
                return false;

            // 인터락 조건: InputFeeder가 StageT 회전과 간섭 없는 상태인지 확인한다.
            if (!VerifyInputFeederClear(machine, "WaferStageT", out reason))
                return false;

            // 인터락 조건: StageT 목표가 InputStage 작업영역 안에서 허용되는 위치인지 확인한다.
            if (!VerifyInputStageWorkArea(request, WaferStageAxis.WaferT, "WaferStageT", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "WaferStageT", out reason);
        }

        // 인터락 항목: StageT 회전 전 NeedlePinZ(EjectPinZ)가 Avoid 위치인지 확인한다.
        private static bool VerifyEjectPinZAtAvoidForStageT(CDT320_Machine machine, string movingName, out string reason)
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
                if (System.Math.Abs(actual - pos.AvoidPosition) <= tolerance)
                    return true;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: StageT 회전 전 EjectPinZ(NeedlePinZ)가 Avoid 위치여야 합니다. actual=" +
                    actual.ToString("F3") + ", avoid=" + pos.AvoidPosition.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying EjectPinZ Avoid for " + movingName + ": " + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 항목: ExpanderZ 이동 종류별로 홈/수동/자동 조건을 선택한다.
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

        // 인터락 항목: ExpanderZ 홈은 현재 별도 차단 조건 없이 허용한다.
        private static bool CanHomeWaferExpandingZ(CDT320_Machine machine, out string reason)
        {
            // 현재 기준: ExpanderZ Home은 어느 위치에서도 허용하며 실제 홈 방향은 축 설정의 NEG를 따른다.
            reason = string.Empty;
            return true;
        }

        // 인터락 항목: 수동 ExpanderZ 이동은 StageT/Feeder/PickerZ/InputVisionX 안전 위치를 확인한다.
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

            // 현재 기준: StageT가 Home(0) 또는 티칭된 고정 위치일 때 ExpanderZ 이동을 허용한다.
            if (!VerifyStageTFixedPositionForExpanderZ(machine, out reason))
                return false;

            // 현재 기준: InputFeederY는 Home(0) 또는 안전 위치(Avoid/Unload)에서만 ExpanderZ 이동을 허용한다.
            if (!VerifyFeederYHomeOrSafeForExpanderZ(machine, out reason))
                return false;

            // 기존 조건: InputStage 다른 축이 동작 중이면 ExpanderZ 이동을 차단한다.
            return VerifyInputStageNotBusy(stage, "ExpanderZ", out reason);
        }

        // 인터락 항목: 자동 ExpanderZ 이동은 StageT/Feeder/PickerZ/InputVisionX 안전 위치를 확인한다.
        private static bool CanAutoWaferExpandingZ(MotionGuardRuleContext request, out string reason)
        {
            // 현재 기준: Auto ExpanderZ는 우선 Manual ExpanderZ와 동일 조건으로 검사한다.
            // 기존 조건: Auto에서 InputVisionX Avoid 확인(VerifyInputVisionXClearForExpanderZ)을 추가로 수행했다.
            // 현재 필요 여부: 사용 안 함. Manual과 동일하게 맞추기 위해 호출하지 않는다.
            return CanManualWaferExpandingZ(request, out reason);
        }

        // ExpanderZ 이동 전제 ①: StageT가 Home(0) 또는 티칭된 고정 위치 중 하나여야 한다.
        // 인터락 항목: ExpanderZ 이동 전 StageT가 Home/Avoid/Load/Unload/Ready/Process 중 하나인지 확인한다.
        private static bool VerifyStageTFixedPositionForExpanderZ(CDT320_Machine machine, out string reason)
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
                    System.Math.Abs(actual - waferT.LoadPosition) <= tolerance ||
                    System.Math.Abs(actual - waferT.UnloadPosition) <= tolerance ||
                    System.Math.Abs(actual - waferT.ReadyPosition) <= tolerance ||
                    System.Math.Abs(actual - waferT.ProcessPosition) <= tolerance)
                    return true;

                return MotionGuardRuleHelpers.Block(
                    "ExpanderZ",
                    "ExpanderZ 이동 불가: WaferStageT가 Home(0) 또는 티칭된 고정 위치가 아닙니다. actual=" +
                    actual.ToString("F3") + ", home=0.000, avoid=" + waferT.AvoidPosition.ToString("F3") +
                    ", load=" + waferT.LoadPosition.ToString("F3") +
                    ", unload=" + waferT.UnloadPosition.ToString("F3") +
                    ", ready=" + waferT.ReadyPosition.ToString("F3") +
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
        // 인터락 항목: ExpanderZ 이동 전 InputFeederY가 Home 또는 안전 위치인지 확인한다.
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
        // 인터락 항목: ExpanderZ 이동 전 Front/Rear PickerZ가 모두 Avoid 위치인지 확인한다.
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

        // 인터락 항목: ExpanderZ 이동 전 Front PickerZ 개별 축이 Avoid 위치인지 확인한다.
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

        // 인터락 항목: ExpanderZ 이동 전 Rear PickerZ 개별 축이 Avoid 위치인지 확인한다.
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

        // 인터락 항목: InputVisionX 이동 종류별로 자동/수동/홈 조건을 선택한다.
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

        // 인터락 항목: 자동 InputVisionX 이동은 수동 이동 룰을 먼저 확인한 뒤 자동 전용 조건을 추가 확인한다.
        private static bool CanAutoInputVisionX(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            // 인터락 조건: 자동 InputVisionX 이동도 수동 InputVisionX 기본 안전 조건을 먼저 통과해야 한다.
            if (!CanManualInputVisionX(machine, out reason))
                return false;

            // 인터락 조건: InputFeeder가 InputVisionX 이동과 간섭 없는 상태인지 확인한다.
            if (!VerifyInputFeederClear(machine, "InputVisionX", out reason))
                return false;

            // InputVisionX는 카메라/캘리브레이션/티칭 위치 때문에 wafer 작업 원 밖으로 이동할 수 있어야 한다.
            // 공유레일/피커 Input zone/피더 인터락은 위 조건에서 유지하고, 원형 작업영역 체크만 적용하지 않는다.

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "InputVisionX", out reason);
        }

        // InputVisionX 이동 전제 ①: InputFeederY가 Avoid 위치 + Wafer Feeder Down 센서 감지. (둘 다 만족해야 함)
        // 인터락 항목: InputVisionX 이동 전 InputFeederY Avoid와 Feeder Down 센서를 확인한다.
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
        // 인터락 항목: InputVisionX 이동 전 Front/Rear Picker가 Input 존을 점유하지 않는지 확인한다.
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

        // 인터락 항목: InputVisionX 이동 전 지정 Picker의 Input 존 X/Y 이동 위험을 확인한다.
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

        // 인터락 기준: Picker가 Input 존에 머물거나 진입/이탈 중인지 판단한다.
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

        // 인터락 항목: NeedleX 이동 종류별로 자동/수동/홈 조건을 선택한다.
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

        // 인터락 항목: 자동 NeedleX 이동은 수동 이동 룰을 먼저 확인한 뒤 자동 전용 조건을 추가 확인한다.
        private static bool CanAutoNeedleX(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            // 인터락 조건: 자동 NeedleX 이동도 수동 NeedleX 기본 안전 조건을 먼저 통과해야 한다.
            if (!CanManualNeedleX(request, out reason))
                return false;

            // 인터락 조건: InputFeeder가 NeedleX 이동과 간섭 없는 상태인지 확인한다.
            if (!VerifyInputFeederClear(machine, "NeedleX", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "NeedleX", out reason);
        }

        // 인터락 항목: 수동 InputVisionX 이동은 FeederY Avoid, Picker Input 존 간섭, Feeder Down 상태를 확인한다.
        private static bool CanManualInputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                // 인터락 조건: InputFeederY가 Avoid 위치가 아니면 InputVisionX 수동 이동을 차단한다.
                if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX HOME blocked. InputFeederY must be at Avoid position.",
                        out reason);

                // 인터락 조건: Feeder Lift가 Down 상태가 아니면 InputVisionX 수동 이동을 차단한다.
                if (feeder != null && !feeder.IsWaferFeederDown())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX HOME blocked. InputFeeder lift cylinder must be down.",
                        out reason);

                // 인터락 조건: ExpanderZ가 Load/Unload 높이에 있으면 InputVisionX 수동 이동을 차단한다.
                if (!VerifyExpanderZNotLoadOrUnloadForStagePlaneMove(machine, "InputVisionX", out reason))
                    return false;

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

        // 인터락 항목: InputVisionX 홈은 FeederY Avoid와 Picker Input 존 간섭을 확인한다.
        private static bool CanHomeInputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                // 인터락 조건: InputFeederY가 Avoid 위치가 아니면 InputVisionX 홈 이동을 차단한다.
                if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX HOME blocked. InputFeederY must be at Avoid position.",
                        out reason);

                // 인터락 조건: Feeder Lift가 Down 상태가 아니면 InputVisionX 홈 이동을 차단한다.
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

        // 인터락 항목: 수동 WaferStageY 이동은 EjectPinZ 0/Avoid, Feeder/Pickers 안전 위치, 작업영역 조건을 확인한다.
        private static bool CanManualWaferStageY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;

                // 인터락 조건: StageY 평면 이동 전 EjectPinZ가 0 이하 또는 Avoid 위치인지 확인한다.
                if (!VerifyEjectPinZAtZeroOrAvoid(machine, "WaferStageY", out reason))
                    return false;

                // 인터락 조건: ExpanderZ가 Load/Unload 높이에 있으면 StageY 이동을 차단한다.
                if (!VerifyExpanderZNotLoadOrUnloadForStagePlaneMove(machine, "WaferStageY", out reason))
                    return false;

                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                // 인터락 조건: InputFeederY가 Avoid 위치가 아니면 StageY 수동 이동을 차단한다.
                if (feeder != null && !feeder.IsWaferFeederInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputStageY",
                        "InputStageY HOME blocked. InputFeederY must be at Avoid position.",
                        out reason);

                // 인터락 조건: Wafer Feeder 자재/센서 상태가 StageY 이동 가능 상태인지 확인한다.
                if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageY", out reason))
                    return false;

                // 인터락 조건: StageY 목표가 InputStage 작업영역 안에서 허용되는 위치인지 확인한다.
                if (!VerifyInputStageWorkArea(request, WaferStageAxis.WaferY, "WaferStageY", out reason))
                    return false;

                // 인터락 조건: FrontPicker Z축들이 Avoid 위치가 아니면 StageY 이동을 차단한다.
                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageY", "Front", out reason))
                    return false;

                // 인터락 조건: RearPicker Z축들이 Avoid 위치가 아니면 StageY 이동을 차단한다.
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

        // 인터락 항목: 수동 WaferStageT 이동은 Feeder, NeedlePinZ(EjectPinZ), PickerZ 안전 위치를 확인한다.
        private static bool CanManualWaferStageT(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                // 인터락 조건: InputFeederY가 Avoid 위치가 아니면 StageT 수동 회전을 차단한다.
                if (!VerifyInputFeederYAvoid(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: StageT 회전 전 EjectPinZ가 Avoid 위치인지 확인한다.
                if (!VerifyEjectPinZAtAvoidForStageT(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: ExpanderZ가 Load/Unload 높이에 있으면 StageT 회전을 차단한다.
                if (!VerifyExpanderZNotLoadOrUnloadForStagePlaneMove(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: Wafer Feeder 자재/센서 상태가 StageT 이동 가능 상태인지 확인한다.
                if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: FrontPicker Z축들이 Avoid 위치가 아니면 StageT 회전을 차단한다.
                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageT", "Front", out reason))
                    return false;

                // 인터락 조건: RearPicker Z축들이 Avoid 위치가 아니면 StageT 회전을 차단한다.
                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerRearUnit : null, "InputStageT", "Rear", out reason))
                    return false;

                return true;
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    "InputStageT",
                    "Exception occurred while verifying InputStageT manual move rules: " + ex.Message,
                    out reason);
            }
            finally
            {
                LogBlockedReason(reason);
            }
        }

        // 인터락 항목: WaferStageY 홈은 NeedleZ Avoid와 PickerZ 안전 위치를 확인한다.
        private static bool CanHomeWaferStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                // 인터락 조건: NeedleZ가 안전 위치가 아니면 StageY 홈 이동을 차단한다.
                if (stage != null && !stage.IsNeedleZInSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputStageY",
                        "InputStageY HOME blocked. NeedleZ must be at Avoid position.",
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

                // 인터락 조건: FrontPicker Z축들이 Avoid 위치가 아니면 StageY 홈 이동을 차단한다.
                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageY", "Front", out reason))
                    return false;

                // 인터락 조건: RearPicker Z축들이 Avoid 위치가 아니면 StageY 홈 이동을 차단한다.
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

        // 인터락 항목: WaferStageT 홈은 Feeder, NeedlePinZ(EjectPinZ), PickerZ 안전 위치를 확인한다.
        private static bool CanHomeWaferStageT(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                // 인터락 조건: InputFeederY가 Avoid 위치가 아니면 StageT 홈 이동을 차단한다.
                if (!VerifyInputFeederYAvoid(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: StageT 홈 전 EjectPinZ가 Avoid 위치인지 확인한다.
                if (!VerifyEjectPinZAtAvoidForStageT(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: ExpanderZ가 Load/Unload 높이에 있으면 StageT 홈 이동을 차단한다.
                if (!VerifyExpanderZNotLoadOrUnloadForStagePlaneMove(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: Wafer Feeder 자재/센서 상태가 StageT 홈 가능 상태인지 확인한다.
                if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: FrontPicker Z축들이 Avoid 위치가 아니면 StageT 홈 이동을 차단한다.
                if (!VerifyPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageT", "Front", out reason))
                    return false;

                // 인터락 조건: RearPicker Z축들이 Avoid 위치가 아니면 StageT 홈 이동을 차단한다.
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

        // 인터락 항목: 수동 NeedleX 이동은 EjectPinZ 0/Avoid와 Needle 작업영역 조건을 확인한다.
        private static bool CanManualNeedleX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;

                // 인터락 조건: NeedleX 이동 전 EjectPinZ가 0 이하 또는 Avoid 위치인지 확인한다.
                if (!VerifyEjectPinZAtZeroOrAvoid(machine, "NeedleX", out reason))
                    return false;

                // 인터락 조건: ExpanderZ가 Load/Unload 높이에 있으면 NeedleX 이동을 차단한다.
                if (!VerifyExpanderZNotLoadOrUnloadForStagePlaneMove(machine, "NeedleX", out reason))
                    return false;

                // 인터락 조건: NeedleX 목표가 InputStage 작업영역 안에서 허용되는 위치인지 확인한다.
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

        // 인터락 항목: NeedleX 홈은 EjectPinZ 0/Avoid와 NeedleZ Avoid 조건을 확인한다.
        private static bool CanHomeNeedleX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                // 인터락 조건: NeedleX 홈 전 EjectPinZ가 0 이하 또는 Avoid 위치인지 확인한다.
                if (!VerifyEjectPinZAtZeroOrAvoid(machine, "NeedleX", out reason))
                    return false;

                // 인터락 조건: NeedleZ가 안전 위치가 아니면 NeedleX 홈 이동을 차단한다.
                if (stage != null && !stage.IsNeedleZInSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "NeedleX",
                        "NeedleX HOME blocked. NeedleZ must be at Avoid position.",
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

        // 인터락 항목: NeedleZ 이동 종류별로 자동/수동/홈 조건을 선택한다.
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

        // 인터락 항목: NeedleZ 홈은 현재 별도 차단 조건 없이 허용한다.
        private static bool CanHomeNeedleZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            // 인터락 조건: NeedleZ 홈은 현재 별도 차단 조건 없이 허용한다.
            return true;
        }

        // 인터락 항목: 수동 NeedleZ 이동은 Needle 작업영역 조건과 InputStage Busy 여부를 확인한다.
        private static bool CanManualNeedleZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;

            // 인터락 조건: NeedleZ 목표가 InputStage 작업영역 안에서 허용되는 위치인지 확인한다.
            if (!VerifyInputStageWorkArea(request, WaferStageAxis.NeedleZ, "NeedleZ", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "NeedleZ", out reason);
        }

        // 인터락 항목: 자동 NeedleZ 이동은 수동 이동 룰을 먼저 확인한 뒤 자동 전용 조건을 추가 확인한다.
        private static bool CanAutoNeedleZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            // 인터락 조건: 자동 NeedleZ 이동도 수동 NeedleZ 기본 안전 조건을 먼저 통과해야 한다.
            if (!CanManualNeedleZ(request, out reason))
                return false;

            // 인터락 조건: InputFeeder가 NeedleZ 이동과 간섭 없는 상태인지 확인한다.
            if (!VerifyInputFeederClear(machine, "NeedleZ", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "NeedleZ", out reason);
        }

        // 인터락 항목: EjectPinZ 이동 종류별로 자동/수동/홈 조건을 선택한다.
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

        // 인터락 항목: EjectPinZ 홈은 현재 별도 차단 조건 없이 허용한다.
        private static bool CanHomeEjectPinZ(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;
            // 인터락 조건: EjectPinZ 홈은 현재 별도 차단 조건 없이 허용한다.
            return true;
        }

        // 인터락 항목: 수동 EjectPinZ 이동은 Avoid 위치에서만 이동 가능한 조건을 확인한다.
        private static bool CanManualEjectPinZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            // 인터락 조건: EjectPinZ 수동 이동은 Avoid 복귀 또는 Avoid 위치에서만 허용한다.
            return VerifyEjectPinZManualMoveSafe(request, "EjectPinZ", out reason);
        }

        // 인터락 항목: 자동 EjectPinZ 이동은 수동 이동 룰을 먼저 확인한 뒤 자동 전용 조건을 추가 확인한다.
        private static bool CanAutoEjectPinZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            // 인터락 조건: 자동 EjectPinZ 이동도 수동 EjectPinZ 기본 안전 조건을 먼저 통과해야 한다.
            if (!CanManualEjectPinZ(request, out reason))
                return false;

            // 인터락 조건: InputFeeder가 EjectPinZ 이동과 간섭 없는 상태인지 확인한다.
            if (!VerifyInputFeederClear(machine, "EjectPinZ", out reason))
                return false;

            bool targetAtAvoid = IsEjectPinZTargetAtAvoid(request);
            // 인터락 조건: Jog/Avoid 복귀가 아닌 EjectPinZ 이동은 InputStage 작업영역 안에서만 허용한다.
            if (!IsContinuousJogMove(request) &&
                !targetAtAvoid &&
                !VerifyInputStageWorkArea(request, WaferStageAxis.EjectPinZ, "EjectPinZ", out reason))
                return false;

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "EjectPinZ", out reason);
        }

        // 인터락 항목: EjectPinZ 수동/조그 이동은 Avoid 위치 복귀 또는 Avoid 위치에서의 이동만 허용한다.
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
                bool actualAtAvoid = System.Math.Abs(actual - pos.AvoidPosition) <= tolerance;
                bool targetAtAvoid = request != null && System.Math.Abs(target - pos.AvoidPosition) <= tolerance;
                if (actualAtAvoid || targetAtAvoid)
                    return true;

                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 조그/수동 이동 불가: EjectPinZ(NeedlePinZ)는 Avoid 위치 복귀 또는 Avoid 위치에서의 이동만 허용합니다. actual=" +
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

        private static bool IsEjectPinZTargetAtAvoid(MotionGuardRuleContext request)
        {
            try
            {
                InputStageUnit stage = request != null && request.Machine != null ? request.Machine.InputStageUnit : null;
                if (stage == null || stage.EjectPinZ == null)
                    return false;

                var pos = stage.Recipe != null ? stage.Recipe.EjectPinZ : null;
                if (pos == null)
                    return false;

                double tolerance = stage.EjectPinZ.Config != null && stage.EjectPinZ.Config.InPositionTolerance > 0.0
                    ? stage.EjectPinZ.Config.InPositionTolerance
                    : 0.05;

                return System.Math.Abs(request.TargetValue - pos.AvoidPosition) <= tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        // 인터락 기준: 현재 요청이 연속 조그 이동인지 판단한다.
        private static bool IsContinuousJogMove(MotionGuardRuleContext request)
        {
            if (request == null)
                return false;

            return request.Intent != null && request.Intent.ContinuousJog;
        }

        // 인터락 항목: InputStage 축 목표가 Needle 작업영역/원형 작업영역/비공정 안전 조건을 만족하는지 확인한다.
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

                if (!stage.IsNeedleZInHomeOrSafePosition())
                {
                    double currentNeedleX = stage.NeedleBlockX != null
                        ? stage.NeedleBlockX.ActualPosition
                        : stage.ResolveNeedleWorkAreaCenterX();
                    double currentStageY = stage.StageY != null
                        ? stage.StageY.ActualPosition
                        : stage.ResolveNeedleWorkAreaCenterY();

                    if (!stage.IsNeedleWorkPointInArea(currentNeedleX, currentStageY, out areaReason))
                    {
                        return MotionGuardRuleHelpers.Block(
                            movingName,
                            movingName + " 이동 불가: NeedleZ 상승 상태에서는 현재 NeedleX/StageY가 작업영역 안이어야 합니다. " +
                            areaReason +
                            ", currentNeedleX=" + currentNeedleX.ToString("F3") +
                            ", currentStageY=" + currentStageY.ToString("F3") +
                            ", targetStageY=" + targetY.ToString("F3") +
                            ", overrideWorkAreaNeedleX=" + overrideWorkAreaNeedleX.ToString("F3"),
                            out reason);
                    }
                }

                // 현재 기준: StageY 이동 작업 반경은 CameraX가 아니라 NeedleX/StageY 실축 좌표로 확인한다.
                if (stage.IsNeedleWorkPointInArea(overrideWorkAreaNeedleX, targetY, out areaReason))
                    return true;

                if (!stage.VerifyNeedleZSafeForWaferYNonProcessTravel(targetY, out areaReason))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: NeedleZ 상승 상태에서는 목표 NeedleX/StageY가 작업영역 안이어야 합니다. " +
                        areaReason +
                        ", overrideWorkAreaNeedleX=" + overrideWorkAreaNeedleX.ToString("F3"),
                        out reason);
                }

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
                // 기존 조건: InputStageWorkAreaX를 Camera/VisionX 원형 작업영역에 직접 대입하는 방식은 사용하지 않는다.
                // if (stage.IsInputStageWorkPointInArea(overrideWorkAreaX, targetY, out areaReason)) ...
                // 현재 기준: InputStageWorkAreaX는 위 TryResolveInputStageWorkAreaNeedleX에서 NeedleX 좌표로 변환해 먼저 판단한다.
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
                if (request == null || request.Intent == null)
                    return false;

                if (request.Intent.InputStageWorkAreaNeedleX.HasValue)
                {
                    workAreaNeedleX = request.Intent.InputStageWorkAreaNeedleX.Value;
                    return true;
                }

                double workAreaVisionX;
                if (!TryResolveInputStageWorkAreaX(request, out workAreaVisionX))
                    return false;

                // 현재 기준: VisionX 기준 Die 목표는 NeedleXToVisionX 캘리브레이션 offset을 빼서 NeedleX 작업 좌표로 변환한다.
                workAreaNeedleX = workAreaVisionX - ResolveNeedleXToVisionXOffset(request.Machine);
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

        private static double ResolveNeedleXToVisionXOffset(CDT320_Machine machine)
        {
            try
            {
                if (machine == null ||
                    machine.VisionUnit == null ||
                    machine.VisionUnit.Config == null)
                    return 0.0;

                machine.VisionUnit.Config.EnsureCalibrationObjects();
                if (machine.VisionUnit.Config.CalibrationData == null ||
                    machine.VisionUnit.Config.CalibrationData.Needle == null ||
                    !machine.VisionUnit.Config.CalibrationData.Needle.Valid)
                    return 0.0;

                return machine.VisionUnit.Config.CalibrationData.Needle.NeedleXToVisionXOffset;
            }
            catch
            {
                return 0.0;
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
        // 인터락 항목: InputStage 이동 전 InputFeederY가 Avoid 위치인지 확인한다.
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

        // 인터락 항목: InputStage 내부 다른 축이 이동 중인지 확인한다.
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

        // 인터락 기준: 현재 이동 대상이 InputVisionX인지 판단한다.
        private static bool IsInputVisionXMove(string movingName)
        {
            return string.Equals(movingName, "InputVisionX", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "CameraX", System.StringComparison.OrdinalIgnoreCase);
        }

        // 인터락 기준: 현재 이동 대상이 NeedleX인지 판단한다.
        private static bool IsNeedleXMove(string movingName)
        {
            return string.Equals(movingName, "NeedleX", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "NeedleBlockX", System.StringComparison.OrdinalIgnoreCase);
        }

        // 인터락 기준: 현재 이동 대상이 WaferStageY인지 판단한다.
        private static bool IsWaferStageYMove(string movingName)
        {
            return string.Equals(movingName, "WaferStageY", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "StageY", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "WaferY", System.StringComparison.OrdinalIgnoreCase);
        }

        // 인터락 기준: ExpanderZ가 플러스 방향으로 상승 이동하는지 판단한다.
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

        // 인터락 항목: ExpanderZ 상승 전 InputVisionX가 Avoid 위치인지 확인한다.
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

        // 인터락 항목: ExpanderZ 상승 전 FrontPicker가 Input 존 간섭 없는 상태인지 확인한다.
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

        // 인터락 항목: ExpanderZ 상승 전 RearPicker가 Input 존 간섭 없는 상태인지 확인한다.
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

        // 인터락 항목: ExpanderZ 상승 전 지정 Picker의 X/Y/Z 위치와 Input 존 간섭을 확인한다.
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

        // 인터락 기준: 지정 축이 현재 이동 대상이 아닌데 이동 중인지 판단한다.
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

        // 인터락 항목: InputStage 홈/이동 전 Front PickerZ 전체가 Avoid 위치인지 확인한다.
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

        // 인터락 항목: InputStage 홈/이동 전 Rear PickerZ 전체가 Avoid 위치인지 확인한다.
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
