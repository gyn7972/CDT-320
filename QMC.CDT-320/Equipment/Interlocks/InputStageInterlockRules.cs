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
            {
                if (!PickerZoneInterlockRules.VerifyPickerXStoppedForClearanceMechanismMove(
                    request.Machine, "InputExpandingZ", out reason))
                    return false;

                return VerifyWaferExpandingZ(request, out reason);
            }

            if (MotionGuardRuleHelpers.IsMoving(request, "InputVisionX", "InputCameraX", "CameraX"))
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

        // WaferStageY 이동 전제(Wafer Feeder): Ring Check==false, Unclamp==true, Overload==false.
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

                // 인터락 조건: StageY/T 이동 전 Wafer Feeder Ring Check가 감지되면 Stage 간섭 위험으로 차단한다.
                if (feeder.IsWaferFeederRingCheck())
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: Wafer Feeder Ring Check가 감지되었습니다.",
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

        // 인터락 항목: StageT 홈 전 NeedlePinZ(EjectPinZ)가 Home(0) 또는 Avoid 위치인지 확인한다.
        private static bool VerifyEjectPinZAtZeroOrAvoidForStageTHome(CDT320_Machine machine, string movingName, out string reason)
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
                        movingName + " HOME 이동 불가: EjectPinZ 레시피 위치가 없습니다.",
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
                    movingName + " HOME 이동 불가: StageT 홈 전 EjectPinZ(NeedlePinZ)가 0 이하 또는 Avoid 위치여야 합니다. actual=" +
                    actual.ToString("F3") + ", zero=0.000, avoid=" + pos.AvoidPosition.ToString("F3"),
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    "Exception occurred while verifying EjectPinZ zero/avoid for " + movingName + " home: " + ex.Message,
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
                    return CanManualInputVisionX(request, out reason);
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
            if (!CanManualInputVisionX(request, out reason))
                return false;

            // 인터락 조건: InputFeeder가 InputVisionX 이동과 간섭 없는 상태인지 확인한다.
            if (!VerifyInputFeederClear(machine, "InputVisionX", out reason))
                return false;

            // InputVisionX는 카메라/캘리브레이션/티칭 위치 때문에 wafer 작업 원 밖으로 이동할 수 있어야 한다.
            // 공유레일/피커 Input zone/피더 인터락은 위 조건에서 유지하고, 원형 작업영역 체크만 적용하지 않는다.

            return VerifyInputStageNotBusy(machine != null ? machine.InputStageUnit : null, "InputVisionX", out reason);
        }

        // InputVisionX는 InputFeederY가 정지된 Avoid/Down 상태일 때만 HOME·수동·자동·Jog 이동한다.
        private static bool VerifyInputFeederAvoidAndDownForInputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder == null || feeder.FeederY == null || feeder.Recipe == null)
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX 이동 불가: InputFeederY Avoid/Down 상태를 확인할 수 없습니다.",
                        out reason);

                if (feeder.FeederY.IsMoving)
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX 이동 불가: InputFeederY가 이동 중입니다.",
                        out reason);

                if (!feeder.IsWaferFeederAvoidPositionCheck())
                    return MotionGuardRuleHelpers.Block(
                        "InputVisionX",
                        "InputVisionX 이동 불가: InputFeeder Avoid Dog(X090)가 ON이 아닙니다. actual=" +
                        feeder.FeederY.ActualPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                        ", avoid=" + feeder.Recipe.AvoidPosition.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
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
                    "InputVisionX 이동 전 InputFeederY Avoid/Down 인터락 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // InputVisionX 이동 전제 ②: Front/Rear Picker가 실제 Input 영역을 점유하면 +방향 진입만 차단하고 Avoid/마이너스 방향 퇴피는 허용한다.
        // 인터락 항목: InputVisionX 이동 전 Front/Rear Picker가 Input 존을 점유할 때 이동 방향을 확인한다.
        private static bool VerifyFrontRearPickerInputZoneClearForInputVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;

            try
            {
                if (!VerifyPickerInputZoneClearForInputVisionX(request, machine, true, "Front", out reason))
                    return false;

                if (!VerifyPickerInputZoneClearForInputVisionX(request, machine, false, "Rear", out reason))
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
        private static bool VerifyPickerInputZoneClearForInputVisionX(MotionGuardRuleContext request, CDT320_Machine machine, bool isFront, string prefix, out string reason)
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
            bool targetAtAvoid = IsInputVisionXTargetAtAvoid(request);

            // 인터락 조건: InputVisionX가 Avoid 목표로 복귀하는 이동은 간섭이 없으므로(기구 확인 2026-07-12),
            // PickerY가 Avoid(후퇴)이고 Picker X/Y가 정지 상태이며 작업영역 점유/Unknown이 없으면
            // Picker가 Input 존 X 범위에 있어도 허용한다. Avoid 외 목표 이동은 기존대로 차단한다.
            if (targetAtAvoid &&
                state != null &&
                state.YAvoid &&
                !xMoving &&
                !yMoving &&
                !movingIntoOrInsideInput &&
                !state.WorkAreaBlocksTransport &&
                !state.UnknownUnsafe)
                return true;

            bool blocking = state != null && (state.BlocksTransport || movingIntoOrInsideInput);
            string inputVisionMoveDetail;
            bool inputVisionRetreat = IsInputVisionXAvoidOrNegativeDirectionMove(request, machine, out inputVisionMoveDetail);
            string detail =
                "movingX=" + xMoving +
                ", movingY=" + yMoving +
                ", movingInputRisk=" + movingIntoOrInsideInput +
                ", inputVisionRetreat=" + inputVisionRetreat +
                ", " + inputVisionMoveDetail +
                ", " + (state != null ? state.Describe() : "state=null");

            if (!blocking)
                return true;

            // 현재 기준: Picker가 정지 상태로 Input 존을 점유 중이어도 InputVisionX가 Avoid/마이너스 방향으로 빠지는 이동은 허용한다.
            if (!movingIntoOrInsideInput && inputVisionRetreat)
                return true;

            // 제3 분기(사용자 승인 2026-07-24): 피커가 존을 점유/이동(퇴장 포함) 중이어도, 비전 이동 목표와
            // 해당 피커 X의 Actual/Command 양쪽이 SharedRailX 페어 간격식으로 SafetyDistance를 만족하면
            // 진입을 허용한다 (팔로잉 진입의 유지 간격 50mm > 요구 10mm, RetreatExtra 미포함 — R5.
            // 피커가 비전 쪽으로 접근 중이면 Command 판정에서 차단된다 — fail-closed).
            BaseAxis clearanceVisionAxis = machine.InputStageUnit != null ? machine.InputStageUnit.CameraX : null;
            BaseAxis clearancePickerAxis = state != null ? state.PickerX : null;
            string clearanceDetail;
            if (request != null &&
                MotionGuardRuleHelpers.IsPairClearanceSatisfiedForEntry(
                    machine, clearanceVisionAxis, request.TargetValue, clearancePickerAxis, out clearanceDetail))
                return true;

            return MotionGuardRuleHelpers.Block(
                "InputVisionX",
                "InputVisionX 이동 불가: " + prefix + "Picker가 Input 영역을 점유하거나 간섭 중이고 페어 간격도 부족합니다. " + detail,
                out reason);
        }

        // 인터락 기준: InputVisionX는 Picker Input 점유 중에도 Avoid 위치 이하 또는 마이너스 방향 퇴피 이동이면 허용한다.
        private static bool IsInputVisionXAvoidOrNegativeDirectionMove(MotionGuardRuleContext request, CDT320_Machine machine, out string detail)
        {
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            BaseAxis axis = stage != null ? stage.CameraX : null;
            if (axis == null && request != null)
                axis = request.GetAxis("InputVisionX") ?? request.GetAxis("InputCameraX") ?? request.GetAxis("CameraX");

            double target = request != null ? request.TargetValue : 0.0;
            double actual = axis != null ? axis.ActualPosition : target;
            double avoid = stage != null && stage.Recipe != null && stage.Recipe.VisionX != null
                ? stage.Recipe.VisionX.AvoidPosition
                : 0.0;
            double tolerance = ResolveAxisPositionTolerance(axis);
            bool targetAtOrBehindAvoid = target <= avoid + tolerance;
            bool negativeDirection = axis != null && target < actual - tolerance;

            detail =
                "inputVisionActual=" + actual.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", inputVisionTarget=" + target.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", inputVisionAvoid=" + avoid.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", tolerance=" + tolerance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", targetAtOrBehindAvoid=" + targetAtOrBehindAvoid +
                ", negativeDirection=" + negativeDirection;

            return targetAtOrBehindAvoid || negativeDirection;
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
        private static bool CanManualInputVisionX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;

            try
            {
                if (!VerifyInputFeederAvoidAndDownForInputVisionX(machine, out reason))
                    return false;

                // 인터락 조건: ExpanderZ가 Load/Unload 높이에 있으면 InputVisionX 수동 이동을 차단한다.
                if (!VerifyExpanderZNotLoadOrUnloadForStagePlaneMove(machine, "InputVisionX", out reason))
                    return false;

                // Picker 전체 Avoid를 강제하지 않는다. 실제 Input 존 점유/간섭만 차단한다.
                if (!VerifyFrontRearPickerInputZoneClearForInputVisionX(request, out reason))
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

        // 인터락 항목: InputVisionX 홈은 InputFeederY가 정지된 Avoid/Down 상태인지 확인한다.
        private static bool CanHomeInputVisionX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                return VerifyInputFeederAvoidAndDownForInputVisionX(machine, out reason);
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

                // 인터락 조건: FrontPicker가 Input 영역 위험 상태일 때만 Z축 Avoid를 강제한다.
                if (!VerifyPickerZAxesAvoidWhenInputRisk(request, machine, true, "InputStageY", out reason))
                    return false;

                // 인터락 조건: RearPicker가 Input 영역 위험 상태일 때만 Z축 Avoid를 강제한다.
                if (!VerifyPickerZAxesAvoidWhenInputRisk(request, machine, false, "InputStageY", out reason))
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
                // 인터락 조건: InputFeederY가 Avoid 또는 실제 위치 0 이하가 아니면 StageT 수동 회전을 차단한다.
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

                // 인터락 조건: FrontPicker가 Input 영역 위험 상태일 때만 Z축 Avoid를 강제한다.
                if (!VerifyPickerZAxesAvoidWhenInputRisk(null, machine, true, "InputStageT", out reason))
                    return false;

                // 인터락 조건: RearPicker가 Input 영역 위험 상태일 때만 Z축 Avoid를 강제한다.
                if (!VerifyPickerZAxesAvoidWhenInputRisk(null, machine, false, "InputStageT", out reason))
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

        // 인터락 항목: WaferStageY 홈은 NeedleZ Home/Avoid와 PickerZ 안전 위치를 확인한다.
        private static bool CanHomeWaferStageY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage == null || stage.NeedleZ == null || stage.EjectPinZ == null)
                    return MotionGuardRuleHelpers.Block(
                        "InputStageY",
                        "InputStageY HOME 절대 인터락 확인 불가: NeedleZ/EjectPinZ 축 정보가 없습니다.",
                        out reason);

                // 절대 인터락: InputStageY HOME은 NeedleZ와 EjectPinZ가 모두 HOME 완료된 뒤에만 허용한다.
                if (!stage.NeedleZ.IsHomeDone || !stage.EjectPinZ.IsHomeDone)
                    return MotionGuardRuleHelpers.Block(
                        "InputStageY",
                        "InputStageY HOME 불가: NeedleZ와 EjectPinZ가 모두 HOME 완료되어야 합니다. " +
                        "needleHome=" + stage.NeedleZ.IsHomeDone +
                        ", ejectHome=" + stage.EjectPinZ.IsHomeDone +
                        ", needleActual=" + stage.NeedleZ.ActualPosition.ToString("0.###") +
                        ", ejectActual=" + stage.EjectPinZ.ActualPosition.ToString("0.###"),
                        out reason);

                // 인터락 조건: NeedleZ가 Home(0) 또는 Avoid 위치가 아니면 StageY 홈 이동을 차단한다.
                if (!stage.IsNeedleZInHomeOrSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "InputStageY",
                        "InputStageY HOME blocked. NeedleZ must be at Home(0) or Avoid position. " + BuildNeedleZState(stage),
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

                // 인터락 조건: FrontPicker Z축들이 Home(0) 또는 Avoid 위치가 아니면 StageY 홈 이동을 차단한다.
                if (!VerifyPickerZAxesHomeOrAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageY", "Front", out reason))
                    return false;

                // 인터락 조건: RearPicker Z축들이 Home(0) 또는 Avoid 위치가 아니면 StageY 홈 이동을 차단한다.
                if (!VerifyPickerZAxesHomeOrAvoid(machine != null ? machine.PickerRearUnit : null, "InputStageY", "Rear", out reason))
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
                // 인터락 조건: InputFeederY가 Home(0) 또는 Avoid 위치가 아니면 StageT 홈 이동을 차단한다.
                if (!VerifyInputFeederYHomeOrAvoid(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: StageT 홈 전 EjectPinZ가 0 이하 또는 Avoid 위치인지 확인한다.
                if (!VerifyEjectPinZAtZeroOrAvoidForStageTHome(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: ExpanderZ가 Load/Unload 높이에 있으면 StageT 홈 이동을 차단한다.
                if (!VerifyExpanderZNotLoadOrUnloadForStagePlaneMove(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: Wafer Feeder 자재/센서 상태가 StageT 홈 가능 상태인지 확인한다.
                if (!VerifyWaferFeederReadyForStageY(machine, "WaferStageT", out reason))
                    return false;

                // 인터락 조건: FrontPicker Z축들이 Home(0) 또는 Avoid 위치가 아니면 StageT 홈 이동을 차단한다.
                if (!VerifyPickerZAxesHomeOrAvoid(machine != null ? machine.PickerFrontUnit : null, "InputStageT", "Front", out reason))
                    return false;

                // 인터락 조건: RearPicker Z축들이 Home(0) 또는 Avoid 위치가 아니면 StageT 홈 이동을 차단한다.
                if (!VerifyPickerZAxesHomeOrAvoid(machine != null ? machine.PickerRearUnit : null, "InputStageT", "Rear", out reason))
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

                // 인터락 조건: NeedleZ가 Home(0) 또는 Avoid 위치가 아니면 NeedleX 홈 이동을 차단한다.
                if (stage != null && !stage.IsNeedleZInHomeOrSafePosition())
                    return MotionGuardRuleHelpers.Block(
                        "NeedleX",
                        "NeedleX HOME blocked. NeedleZ must be at Home(0) or Avoid position.",
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

            // 인터락 조건: 내부에서 AxisMove로 변환된 Step/Continuous Jog도 원래 Jog 요청이면 조그 룰로 처리한다.
            if (MotionGuardRuleHelpers.IsJogMove(request))
                return CanJogEjectPinZ(request, out reason);

            switch (request.MoveKind)
            {
                // 자동 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanAutoEjectPinZ(request, out reason);
                // 매뉴얼 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                    return CanManualEjectPinZ(request, out reason);
                // 조그 이동 인터락 확인
                case MotionGuardMoveKind.AxisContinuousJog:
                case MotionGuardMoveKind.AxisStepJog:
                    return CanJogEjectPinZ(request, out reason);
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

        // 인터락 항목: 조그 EjectPinZ 이동은 복구/위치 확인을 위해 Avoid 위치 제한을 적용하지 않는다.
        private static bool CanJogEjectPinZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            // 인터락 조건: EjectPinZ Step/Continuous Jog는 작업자가 직접 복구할 수 있도록 위치 제한 없이 허용한다.
            return true;
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

        // 인터락 기준: InputVisionX 이동 목표가 Avoid 티칭 위치인지 판단한다.
        private static bool IsInputVisionXTargetAtAvoid(MotionGuardRuleContext request)
        {
            try
            {
                InputStageUnit stage = request != null && request.Machine != null ? request.Machine.InputStageUnit : null;
                if (stage == null || stage.CameraX == null)
                    return false;

                var pos = stage.Recipe != null ? stage.Recipe.VisionX : null;
                if (pos == null)
                    return false;

                double tolerance = stage.CameraX.Config != null && stage.CameraX.Config.InPositionTolerance > 0.0
                    ? stage.CameraX.Config.InPositionTolerance
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

                if (stage.IsNeedleZInHomeOrSafePosition())
                    return true;

                if (!stage.IsNeedleZInSafePosition())
                {
                    double currentNeedleX = stage.NeedleBlockX != null
                        ? stage.NeedleBlockX.ActualPosition
                        : stage.ResolveNeedleWorkAreaCenterX();
                    double currentStageY = stage.StageY != null
                        ? stage.StageY.ActualPosition
                        : stage.ResolveNeedleWorkAreaCenterY();

                    if (!stage.IsNeedleWorkPointInArea(currentNeedleX, currentStageY, out areaReason))
                    {
                        if (!stage.IsNeedleZInHomeOrSafePosition())
                        {
                            return MotionGuardRuleHelpers.Block(
                                movingName,
                                movingName + " 이동 불가: 현재 NeedleX/StageY가 작업영역 밖일 때 NeedleZ는 반드시 Home(0) 또는 Avoid 위치여야 합니다. " +
                                areaReason +
                                ", currentNeedleX=" + currentNeedleX.ToString("F3") +
                                ", currentStageY=" + currentStageY.ToString("F3") +
                                ", targetStageY=" + targetY.ToString("F3") +
                                ", overrideWorkAreaNeedleX=" + overrideWorkAreaNeedleX.ToString("F3") +
                                ", " + BuildNeedleZState(stage),
                                out reason);
                        }
                    }
                }

                // 현재 기준: StageY 이동 작업 반경은 CameraX가 아니라 NeedleX/StageY 실축 좌표로 확인한다.
                if (stage.IsNeedleWorkPointInArea(overrideWorkAreaNeedleX, targetY, out areaReason))
                    return true;

                if (!stage.VerifyNeedleZSafeForWaferYNonProcessTravel(targetY, out areaReason))
                {
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: NeedleZ가 Home(0) 또는 Avoid 위치가 아닐 때는 목표 NeedleX/StageY가 작업영역 안이어야 합니다. " +
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

        // 이동 전제: InputFeederY가 Avoid 위치이거나 실제 위치가 0 이하여야 한다(아니면 차단/알람).
        // 인터락 항목: InputStageT 이동 전 InputFeederY의 Avoid 또는 실제 위치 0 이하 조건을 확인한다.
        private static bool VerifyInputFeederYAvoid(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
            if (feeder == null)
                return true;

            bool feederAtAvoid = feeder.IsWaferFeederYInAvoidPosition();
            bool feederAtOrBelowZero =
                feeder.FeederY != null && feeder.FeederY.ActualPosition <= 0.0;
            if (feederAtAvoid || feederAtOrBelowZero)
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " 이동 불가: InputFeederY가 Avoid 위치가 아니고 실제 위치가 0보다 큽니다. " +
                "feederActual=" +
                (feeder.FeederY != null ? feeder.FeederY.ActualPosition.ToString("0.###") : "missing"),
                out reason);
        }

        // 인터락 항목: 홈 이동 전 InputFeederY가 Home(0) 또는 Avoid 위치인지 확인한다.
        private static bool VerifyInputFeederYHomeOrAvoid(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
            if (feeder == null)
                return true;

            if (feeder.IsWaferFeederYInHomePosition() ||
                feeder.IsWaferFeederYInAvoidPosition())
                return true;

            //if(feeder.IsWaferFeeederD
            //    )

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " HOME 이동 불가: InputFeederY가 Home(0) 또는 Avoid 위치가 아닙니다.",
                out reason);
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
            // 기존 조건: InputVisionX가 이동 중이면 NeedleX를 제외한 모든 InputStage 축 이동을 차단했다.
            // 현재 기준(사용자 승인 2026-07-25): WaferStageY도 예외로 둔다.
            //   근거 1 — 물리 간섭 없음(사용자 확인 2026-07-25): InputVisionX(카메라 X)와
            //            InputStageY(웨이퍼 Y)는 기계적으로 간섭하지 않는 축이다.
            //   근거 2 — 선언 매트릭스 불일치: interlock-check-matrix.json의 MovingName="WaferY" 행이
            //            요구하는 검사는 InputFeederY / Feeder Up-Down / Feeder Clamp-UnClamp /
            //            WaferExpandingZ / NeedleX / NeedleZ / EjectPinZ 7건뿐이며 InputVisionX는 없다.
            //            이 차단은 선언된 인터락이 아니라 이 함수의 하드코딩 결합이었다.
            //   근거 3 — StageY의 실제 간섭 반경은 CameraX가 아니라 NeedleX/StageY 좌표로 판정한다
            //            (IsNeedleWorkPointInArea / TryResolveNeedleWorkPointMoveOrder,
            //             PickerInputStageMoveHelper.BuildWorkPointTargetName 주석). NeedleX는 이미
            //            이 규칙의 예외이므로 NeedleX∥StageY 동시 이동은 기존에도 허용됐다.
            //   목적 — 픽업의 InputVisionX 이연 최소 회피(약 330mm, 5%에서 ~6.6초)와 StageY 진입
            //          이동이 겹쳐 -11로 실패하던 문제 해소(실장비 2026-07-25 17:18:55).
            // 현재 기준(사용자 승인 2026-07-27): EjectPinZ도 예외로 둔다.
            //   근거 1 — 물리 간섭 없음(사용자 확인 2026-07-27): EjectPinZ(웨이퍼 하부 이젝트 핀)와
            //            InputVisionX(카메라 X)는 기계적으로 간섭하지 않는 축이다.
            //   근거 2 — 선언 매트릭스 불일치: interlock-check-matrix.json의 MovingName="EjectPinZ" 행이
            //            요구하는 검사는 WaferY(H18) / NeedleX(L18) 2건뿐이며 InputVisionX는 없다.
            //   목적 — 픽업 중 InputVisionX 비동기 전진과 SyncLift 동반 EjectPinZ 상승(3.1→3.7)이
            //          겹쳐 Critical INTERLOCK → 전축 비상정지 → 상승 중 PickerZ -5로 이어지던
            //          문제 해소(실장비 2026-07-27 05:09:16).
            // 추가(사용자 지시 2026-07-27): NeedleZ도 예외 — NeedleZ와 InputVisionX는 아무 인터락
            //   관계가 없다(사용자 확인). 선언 매트릭스의 MovingName="NeedleZ" 행 검사도
            //   WaferY(H17) / NeedleX(L17) 2건뿐이며 InputVisionX는 없다(EjectPinZ와 동일 유형).
            //   주의 — 완화 조합은 (WaferStageY / EjectPinZ / NeedleZ) × InputVisionX 3개다.
            //          WaferStageT / ExpanderZ는 그대로 차단된다.
            if ((IsEjectPinZMove(movingName) || IsNeedleZMove(movingName)) &&
                IsMovingExcept(stage.CameraX, movingName, "InputVisionX", "CameraX"))
            {
                QMC.Common.Log.Write("Main", "INTERLOCK", "MotionGuard",
                    movingName + " 이동 허용: InputVisionX 이동 중이지만 예외 적용(사용자 승인 2026-07-27). cameraActual=" +
                    (stage.CameraX != null ? stage.CameraX.ActualPosition.ToString("F3") : "-") + " - Check");
            }
            else if (!IsNeedleXMove(movingName) &&
                !IsWaferStageYMove(movingName) &&
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

        // 인터락 기준: 현재 이동 대상이 NeedleZ인지 판단한다.
        private static bool IsNeedleZMove(string movingName)
        {
            return string.Equals(movingName, "NeedleZ", System.StringComparison.OrdinalIgnoreCase);
        }

        // 인터락 기준: 현재 이동 대상이 EjectPinZ(NeedlePinZ)인지 판단한다.
        private static bool IsEjectPinZMove(string movingName)
        {
            return string.Equals(movingName, "EjectPinZ", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "NeedlePinZ", System.StringComparison.OrdinalIgnoreCase);
        }

        // 인터락 기준: 현재 이동 대상이 WaferStageY인지 판단한다.
        private static bool IsWaferStageYMove(string movingName)
        {
            return string.Equals(movingName, "WaferStageY", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(movingName, "InputStageY", System.StringComparison.OrdinalIgnoreCase) ||
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

        // 인터락 항목: Picker가 Input 영역 위험 상태일 때만 해당 PickerZ 전체 Avoid를 강제한다.
        private static bool VerifyPickerZAxesAvoidWhenInputRisk(MotionGuardRuleContext request, CDT320_Machine machine, bool isFront, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                string prefix = isFront ? "Front" : "Rear";
                PickerZoneTransportState state = PickerZoneInterlockRules.ResolvePickerZoneTransportState(
                    machine,
                    isFront,
                    PickerWorkZone.Input,
                    null,
                    ResolvePickerInputRiskTargetName(request, isFront, movingName));

                if (IsSameAutoPickUpInputStageMove(request, isFront, movingName, state))
                    return true;

                if (!IsPickerInputRiskForZAvoid(state))
                    return true;

                string detail = state != null ? state.Describe() : prefix + "Picker state=null";
                if (isFront)
                    return VerifyPickerZAxesAvoidForInputRisk(
                        machine != null ? machine.PickerFrontUnit : null,
                        movingName,
                        prefix,
                        detail,
                        out reason);

                return VerifyPickerZAxesAvoidForInputRisk(
                    machine != null ? machine.PickerRearUnit : null,
                    movingName,
                    prefix,
                    detail,
                    out reason);
            }
            catch (System.Exception ex)
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: Picker Input 영역 위험 상태 확인 중 예외가 발생했습니다. error=" + ex.Message,
                    out reason);
            }
            finally
            {
            }
        }

        // 인터락 기준: InputStageY 요청 targetName이 특정 Picker side의 행위라면
        // 반대 Picker의 target zone 판단에는 그 targetName을 사용하지 않는다.
        private static string ResolvePickerInputRiskTargetName(MotionGuardRuleContext request, bool isFront, string movingName)
        {
            string fallback = movingName + ";InputStagePickerZRiskCheck";
            if (request == null || string.IsNullOrWhiteSpace(request.TargetName))
                return fallback;

            string requestedSide;
            if (request.Intent != null &&
                request.Intent.TryGetValue("Side", out requestedSide) &&
                !IsMatchingPickerSide(isFront, requestedSide))
                return fallback;

            return request.TargetName;
        }

        // 인터락 기준: 자동 PickUp이 같은 Input work area를 점유하고 수행 중인 StageY 이동은
        // Encoder X 존 겹침보다 현재 행위(owner)를 우선해 PickerZ Avoid 강제 대상에서 제외한다.
        private static bool IsSameAutoPickUpInputStageMove(
            MotionGuardRuleContext request,
            bool isFront,
            string movingName,
            PickerZoneTransportState state)
        {
            if (state == null)
                return false;

            if (!IsWaferStageYMove(movingName))
                return false;

            if (!state.HasWorkArea ||
                !PickerZoneInterlockRules.IsSameInterlockZone(state.WorkAreaZone, PickerWorkZone.Input))
                return false;

            string expectedOwnerPrefix = isFront ? "FrontPickerPickUpSequence" : "RearPickerPickUpSequence";
            if (string.IsNullOrWhiteSpace(state.WorkAreaOwner) ||
                !state.WorkAreaOwner.StartsWith(expectedOwnerPrefix, System.StringComparison.OrdinalIgnoreCase))
                return false;

            bool activeAutoPickUpOwner =
                state.WorkAreaOwner.EndsWith(":PickUp", System.StringComparison.OrdinalIgnoreCase) ||
                state.WorkAreaOwner.EndsWith(":PickUp ContiNode", System.StringComparison.OrdinalIgnoreCase);
            if (!activeAutoPickUpOwner)
                return false;

            if (request == null)
                return true;

            if (request.IsManualSequenceProcess)
                return false;

            string requestedSide;
            if (request.Intent != null &&
                request.Intent.TryGetValue("Side", out requestedSide) &&
                !IsMatchingPickerSide(isFront, requestedSide))
                return false;

            return true;
        }

        // 인터락 기준: targetName의 Side 메타가 현재 검사 중인 Picker side와 같은지 판단한다.
        private static bool IsMatchingPickerSide(bool isFront, string side)
        {
            if (string.IsNullOrWhiteSpace(side))
                return false;

            return isFront
                ? side.Equals("Front", System.StringComparison.OrdinalIgnoreCase)
                : side.Equals("Rear", System.StringComparison.OrdinalIgnoreCase);
        }

        // 인터락 기준: Input 영역에서 PickerY가 실제 돌출/이동 중이거나 작업영역/Unknown 위험이면 PickerZ Avoid 강제 대상이다.
        private static bool IsPickerInputRiskForZAvoid(PickerZoneTransportState state)
        {
            if (state == null)
                return false;

            if (state.UnknownUnsafe || state.WorkAreaBlocksTransport)
                return true;

            bool inputZoneActive =
                PickerZoneInterlockRules.IsSameInterlockZone(state.CurrentZone, PickerWorkZone.Input) ||
                PickerZoneInterlockRules.IsSameInterlockZone(state.TargetZone, PickerWorkZone.Input);

            if (!inputZoneActive)
                return false;

            return !state.YAvoid || IsAxisMoving(state.PickerY);
        }

        // 인터락 기준: 축이 이동 중인지 판단한다.
        private static bool IsAxisMoving(BaseAxis axis)
        {
            return axis != null && axis.IsMoving;
        }

        // 인터락 항목: Input 위험 상태인 Front PickerZ 전체가 Avoid 위치인지 확인한다.
        private static bool VerifyPickerZAxesAvoidForInputRisk(PickerFrontUnit picker, string movingName, string prefix, string detail, out string reason)
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
                        movingName + " 이동 불가: " + prefix + zAxis +
                        "가 Avoid 위치가 아닙니다. " + prefix +
                        "Picker가 Input 영역 위험 상태입니다. " + detail,
                        out reason);
            }

            return true;
        }

        // 인터락 항목: Input 위험 상태인 Rear PickerZ 전체가 Avoid 위치인지 확인한다.
        private static bool VerifyPickerZAxesAvoidForInputRisk(PickerRearUnit picker, string movingName, string prefix, string detail, out string reason)
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
                        movingName + " 이동 불가: " + prefix + zAxis +
                        "가 Avoid 위치가 아닙니다. " + prefix +
                        "Picker가 Input 영역 위험 상태입니다. " + detail,
                        out reason);
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

        // 인터락 항목: InputStage 홈 전 Front PickerZ 전체가 Home(0) 또는 Avoid 위치인지 확인한다.
        private static bool VerifyPickerZAxesHomeOrAvoid(PickerFrontUnit picker, string movingName, string prefix, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = ResolvePickerZAxis(picker, zAxis);
                if (!IsAxisAtHomeOrTeachingAvoid(axis, () => picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. " + prefix + zAxis + " must be at Home(0) or Avoid position.",
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

        // 인터락 항목: InputStage 홈 전 Rear PickerZ 전체가 Home(0) 또는 Avoid 위치인지 확인한다.
        private static bool VerifyPickerZAxesHomeOrAvoid(PickerRearUnit picker, string movingName, string prefix, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = ResolvePickerZAxis(picker, zAxis);
                if (!IsAxisAtHomeOrTeachingAvoid(axis, () => picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. " + prefix + zAxis + " must be at Home(0) or Avoid position.",
                        out reason);
            }

            return true;
        }

        // 인터락 기준: Front PickerZ enum에 대응하는 실제 축을 가져온다.
        private static BaseAxis ResolvePickerZAxis(PickerFrontUnit picker, PickerAxis axis)
        {
            if (picker == null)
                return null;

            switch (axis)
            {
                case PickerAxis.PickerZ0: return picker.PickerZ0;
                case PickerAxis.PickerZ1: return picker.PickerZ1;
                case PickerAxis.PickerZ2: return picker.PickerZ2;
                case PickerAxis.PickerZ3: return picker.PickerZ3;
                default: return null;
            }
        }

        // 인터락 기준: Rear PickerZ enum에 대응하는 실제 축을 가져온다.
        private static BaseAxis ResolvePickerZAxis(PickerRearUnit picker, PickerAxis axis)
        {
            if (picker == null)
                return null;

            switch (axis)
            {
                case PickerAxis.PickerZ0: return picker.PickerZ0;
                case PickerAxis.PickerZ1: return picker.PickerZ1;
                case PickerAxis.PickerZ2: return picker.PickerZ2;
                case PickerAxis.PickerZ3: return picker.PickerZ3;
                default: return null;
            }
        }

        // 인터락 기준: 축이 Home(0) 위치이거나 티칭 Avoid 위치인지 판단한다.
        private static bool IsAxisAtHomeOrTeachingAvoid(BaseAxis axis, System.Func<bool> isTeachingAvoid)
        {
            if (axis == null)
                return true;

            if (MotionGuardRuleHelpers.IsAt(axis, 0.0))
                return true;

            return isTeachingAvoid != null && isTeachingAvoid();
        }

        private static string BuildNeedleZState(InputStageUnit stage)
        {
            try
            {
                if (stage == null || stage.NeedleZ == null)
                    return "NeedleZ=null";

                double avoid = stage.Recipe != null && stage.Recipe.NeedleZ != null
                    ? stage.Recipe.NeedleZ.AvoidPosition
                    : 0.0;
                double tolerance = stage.NeedleZ.Config != null && stage.NeedleZ.Config.InPositionTolerance > 0.0
                    ? stage.NeedleZ.Config.InPositionTolerance
                    : 0.01;

                return "NeedleZ[name=" + stage.NeedleZ.Name +
                    ", actual=" + stage.NeedleZ.ActualPosition.ToString("F3") +
                    ", avoid=" + avoid.ToString("F3") +
                    ", tolerance=" + tolerance.ToString("F3") +
                    ", servo=" + (stage.NeedleZ.IsServoOn ? "ON" : "OFF") +
                    ", alarm=" + (stage.NeedleZ.IsAlarm ? "ON" : "OFF") +
                    ", moving=" + (stage.NeedleZ.IsMoving ? "Y" : "N") + "]";
            }
            catch
            {
                return "NeedleZ state unavailable.";
            }
            finally
            {
            }
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
