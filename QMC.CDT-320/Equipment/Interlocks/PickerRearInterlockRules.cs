using QMC.Common.Motion;

using QMC.Common.IO;

namespace QMC.CDT320.Interlocks
{
    public static class PickerRearInterlockRules
    {
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

        private static bool VerifyRearPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

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

                // 현재 기준: Input 진입은 Z Avoid/0 이상, ExpandingZ 0 이하, InputFeederY Avoid/0 이하, Feeder Down, InputVisionX Avoid/0 이하, 상대 FrontPickerY Avoid일 때만 허용한다.
                if (targetZone == PickerWorkZone.Input &&
                    !VerifyManualRearPickerXInputEntry(machine, out reason))
                    return false;

                // 현재 기준: Output 진입은 Z Avoid/0 이상, GoodStageZ Process 이하, OutputFeederY Avoid/0 이하, Feeder Down, OutputVisionX Avoid/0 이하, 상대 FrontPickerY Avoid일 때만 허용한다.
                if (targetZone == PickerWorkZone.Output &&
                    !VerifyManualRearPickerXOutputEntry(machine, out reason))
                    return false;

                // 현재 기준: Process 존을 다른 존에서 진입할 때는 RearPickerZ0~Z3가 Avoid 또는 0 이상 위치여야 한다.
                if (PickerZoneInterlockRules.IsManualPickerXProcessZone(targetZone) &&
                    !PickerZoneInterlockRules.IsManualPickerXProcessZone(currentZone) &&
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

        private static bool VerifyManualRearPickerXInputEntry(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            // 현재 기준: Input 진입 전 RearPickerZ0~Z3는 Avoid 또는 0 이상 위치여야 한다.
            if (!VerifyRearPickerZAxesAvoidOrNonNegative(machine != null ? machine.PickerRearUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputExpandingZ는 0 이하 위치여야 한다.
            if (!VerifyInputExpanderZAtOrBelowZero(machine != null ? machine.InputStageUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputFeederY는 Avoid 또는 0 이하 위치여야 한다.
            if (!VerifyInputFeederYAtAvoidOrBelowZero(machine != null ? machine.InputFeederUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputFeeder Lift는 Down 상태여야 한다.
            if (!VerifyInputFeederDown(machine != null ? machine.InputFeederUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Input 진입 전 InputVisionX는 Avoid 또는 0 이하 위치여야 한다.
            if (!VerifyInputVisionXAtAvoidOrBelowZero(machine != null ? machine.InputStageUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: RearPickerX Input 진입 전 상대 FrontPickerY는 Avoid 위치여야 한다.
            return VerifyFrontPickerYAvoidForRearPickerX(machine, out reason);
        }

        private static bool VerifyManualRearPickerXOutputEntry(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            // 현재 기준: Output 진입 전 RearPickerZ0~Z3는 Avoid 또는 0 이상 위치여야 한다.
            if (!VerifyRearPickerZAxesAvoidOrNonNegative(machine != null ? machine.PickerRearUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputGoodStageZ는 ProcessPos 이하 위치여야 한다.
            if (!VerifyGoodStageZAtOrBelowProcess(machine != null ? machine.OutputStageUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputFeederY는 Avoid 또는 0 이하 위치여야 한다.
            if (!VerifyOutputFeederYAtAvoidOrBelowZero(machine != null ? machine.OutputFeederUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputFeeder Lift는 Down 상태여야 한다.
            if (!VerifyOutputFeederDown(machine != null ? machine.OutputFeederUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: Output 진입 전 OutputVisionX는 Avoid 또는 0 이하 위치여야 한다.
            if (!VerifyOutputVisionXAtAvoidOrBelowZero(machine != null ? machine.OutputStageUnit : null, "RearPickerX", out reason))
                return false;

            // 현재 기준: RearPickerX Output 진입 전 상대 FrontPickerY는 Avoid 위치여야 한다.
            return VerifyFrontPickerYAvoidForRearPickerX(machine, out reason);
        }

        private static bool VerifyRearPickerZAxesAvoidOrNonNegative(PickerRearUnit picker, string movingName, out string reason)
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

        private static bool VerifyInputFeederYAtAvoidOrBelowZero(InputFeederUnit feeder, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: InputFeeder 또는 FeederY 참조가 없으면 InputFeederY 조건을 적용하지 않는다.
            if (feeder == null || feeder.FeederY == null)
                return true;

            // 현재 기준: InputFeederY가 이동 중이면 Picker Input 진입을 차단한다.
            if (feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: InputFeederY가 이동 중입니다.", out reason);

            // 현재 기준: InputFeederY는 Avoid 위치이거나 ActualPosition이 0 이하이어야 한다.
            if (feeder.IsWaferFeederYInAvoidPosition() ||
                feeder.FeederY.ActualPosition <= ResolveAxisTolerance(feeder.FeederY))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " 이동 불가: InputFeederY가 Avoid 또는 0 이하 위치가 아닙니다. actual=" + feeder.FeederY.ActualPosition.ToString("0.###"),
                out reason);
        }

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

        private static bool VerifyInputVisionXAtAvoidOrBelowZero(InputStageUnit stage, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: InputStage 또는 InputVisionX 참조가 없으면 InputVisionX 조건을 적용하지 않는다.
            if (stage == null || stage.CameraX == null)
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
                movingName + " 이동 불가: InputVisionX가 Avoid 또는 0 이하 위치가 아닙니다. actual=" + stage.CameraX.ActualPosition.ToString("0.###"),
                out reason);
        }

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

        private static bool VerifyOutputFeederYAtAvoidOrBelowZero(OutputFeederUnit feeder, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: OutputFeeder 또는 FeederY 참조가 없으면 OutputFeederY 조건을 적용하지 않는다.
            if (feeder == null || feeder.FeederY == null)
                return true;

            // 현재 기준: OutputFeederY가 이동 중이면 Picker Output 진입을 차단한다.
            if (feeder.FeederY.IsMoving)
                return MotionGuardRuleHelpers.Block(movingName, movingName + " 이동 불가: OutputFeederY가 이동 중입니다.", out reason);

            // 현재 기준: OutputFeederY는 Avoid 위치이거나 ActualPosition이 0 이하이어야 한다.
            if (feeder.IsBinFeederYInAvoidPosition() ||
                feeder.FeederY.ActualPosition <= ResolveAxisTolerance(feeder.FeederY))
                return true;

            return MotionGuardRuleHelpers.Block(
                movingName,
                movingName + " 이동 불가: OutputFeederY가 Avoid 또는 0 이하 위치가 아닙니다. actual=" + feeder.FeederY.ActualPosition.ToString("0.###"),
                out reason);
        }

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

        private static bool VerifyOutputVisionXAtAvoidOrBelowZero(OutputStageUnit outputStage, string movingName, out string reason)
        {
            reason = string.Empty;
            // 방어 조건: OutputStage 또는 OutputVisionX 참조가 없으면 OutputVisionX 조건을 적용하지 않는다.
            if (outputStage == null || outputStage.OutputCameraX == null)
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
                movingName + " 이동 불가: OutputVisionX가 Avoid 또는 0 이하 위치가 아닙니다. actual=" + outputStage.OutputCameraX.ActualPosition.ToString("0.###"),
                out reason);
        }

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

        private static bool IsColletCalibrationBottomMove(MotionGuardRuleContext request)
        {
            // 현재 기준: ColletCalibration 플래그와 Bottom 존이 모두 맞을 때 Bottom 보정 이동으로 본다.
            return request != null &&
                   request.Intent != null &&
                   request.Intent.ColletCalibration &&
                   request.Intent.PickerZone == PickerWorkZone.Bottom;
        }

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

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                // 현재 기준: RearPicker 평면 이동 전 Z0~Z3는 모두 Avoid 위치여야 한다.
                if (!picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: Rear" + zAxis + " 축이 Avoid 위치가 아닙니다.",
                        out reason);
            }

            return true;
        }

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

        private static bool CanManualRearPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            try
            {
                CDT320_Machine machine = request != null ? request.Machine : null;

                // 현재 기준: 자동 검사 Z Hold/FineAlign 이동은 Z축을 유지해야 하므로 Home/Avoid 조건에서 제외한다.
                if (!CanKeepRearPickerZDuringYMove(request) &&
                    !VerifyRearPickerZAxesHomeOrAvoid(machine != null ? machine.PickerRearUnit : null, "RearPickerY", out reason))
                    return false;

                // 현재 기준: Reticle 실린더가 이동 중이면 RearPickerY 수동 이동을 차단한다.
                if (!VerifyReticleCylinderClear(machine, "RearPickerY", out reason))
                    return false;

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
                // 현재 기준: RearPickerX Home 전 FrontPickerY는 Avoid 위치여야 한다.
                if (front != null && !front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. FrontPickerY must be at Avoid position.",
                        out reason);

                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
                // 현재 기준: RearPickerX Home 전 RearPickerY는 Avoid 위치여야 한다.
                if (rear != null && !rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. RearPickerY must be at Avoid position.",
                        out reason);

                // 현재 기준: RearPickerX Home 전 RearPickerZ0~Z3는 모두 Avoid 위치여야 한다.
                if (!VerifyRearPickerZAxesAvoid(rear, "RearPickerX", out reason))
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

        private static bool CanHomeRearPickerY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
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
                // 현재 기준: RearPickerT Home 전 대응 RearPickerZ축은 Avoid 위치여야 한다.
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

        private static bool CanHomeRearPickerZ(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanManualRearPickerZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            string movingName = request != null ? request.MovingName : "RearPickerZ";
            PickerWorkZone targetZone = ResolvePickerZTargetZone(request);

            // 현재 기준: RearPickerZ 수동 이동 전 Z Home 룰을 먼저 확인한다.
            if (!CanHomeRearPickerZ(machine, movingName, out reason))
                return false;

            // 현재 기준: RearPickerZ 작업 이동 전 Reticle은 Retract 상태여야 한다.
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

        private static bool CanAutoRearPickerZ(MotionGuardRuleContext request, out string reason)
        {
            // 현재 기준: Auto RearPickerZ도 Manual RearPickerZ 기본 인터락과 동일하게 확인한다.
            return CanManualRearPickerZ(request, out reason);
        }

        private static PickerWorkZone ResolvePickerZTargetZone(MotionGuardRuleContext request)
        {
            // 현재 기준: 요청 Intent에 PickerZone이 있으면 Z축 목표 존으로 사용한다.
            if (request != null && request.Intent != null)
                return request.Intent.PickerZone;

            return PickerWorkZone.Unknown;
        }

        private static bool RequiresInputFeederAvoid(PickerWorkZone targetZone)
        {
            // 현재 기준: Input 또는 Unknown 존이면 InputFeederY Avoid 조건을 적용한다.
            return targetZone == PickerWorkZone.Input ||
                   targetZone == PickerWorkZone.Unknown;
        }

        private static bool RequiresOutputFeederAvoid(PickerWorkZone targetZone)
        {
            // 현재 기준: Output 또는 Unknown 존이면 OutputFeederY Avoid 조건을 적용한다.
            return targetZone == PickerWorkZone.Output ||
                   targetZone == PickerWorkZone.Unknown;
        }

        private static bool RequiresInputStageZSafe(PickerWorkZone targetZone)
        {
            // 현재 기준: Input 또는 Unknown 존이면 InputExpandingZ 안전 위치 조건을 적용한다.
            return targetZone == PickerWorkZone.Input ||
                   targetZone == PickerWorkZone.Unknown;
        }

        private static bool RequiresOutputStageZSafeForPickerY(PickerWorkZone targetZone)
        {
            // 현재 기준: Output 또는 Unknown 존이면 OutputStage GoodStageZ 안전 위치 조건을 적용한다.
            return targetZone == PickerWorkZone.Output ||
                   targetZone == PickerWorkZone.Unknown;
        }

        private static bool Contains(string value, string pattern)
        {
            return (value ?? string.Empty).IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

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

        private static bool IsCylinderMoving(BaseCylinder cylinder)
        {
            return MotionGuardRuleHelpers.IsCylinderMoving(cylinder);
        }

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

        private static bool IsAxisAtOrAboveZero(BaseAxis axis)
        {
            // 방어 조건: 축 참조가 없으면 0 이상 조건을 만족하지 않은 것으로 본다.
            if (axis == null)
                return false;

            // 현재 기준: ActualPosition이 0 이상이거나 tolerance 안쪽이면 0 이상 위치로 본다.
            return axis.ActualPosition >= -ResolveAxisTolerance(axis);
        }

        private static double ResolveAxisTolerance(BaseAxis axis)
        {
            // 현재 기준: 축 InPositionTolerance가 있으면 그 값을 위치 비교 tolerance로 사용한다.
            if (axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0)
                return axis.Config.InPositionTolerance;

            return 0.05;
        }

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
    }
}
