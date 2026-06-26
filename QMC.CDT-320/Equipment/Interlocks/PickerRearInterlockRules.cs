using QMC.Common.Motion;

using QMC.Common.IO;

namespace QMC.CDT320.Interlocks
{
    public static class PickerRearInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerX"))
                return VerifyRearPickerX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerY"))
                return VerifyRearPickerY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerT0", "RearPickerT1", "RearPickerT2", "RearPickerT3"))
                return VerifyRearPickerT(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "RearPickerZ0", "RearPickerZ1", "RearPickerZ2", "RearPickerZ3"))
                return VerifyRearPickerZ(request, out reason);

            return true;
        }

        private static bool VerifyRearPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveRearPickerX(request, out reason);
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeRearPickerX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveRearPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;

            try
            {
                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
                if (!VerifyRearPickerZAxesAvoidForMove(rear, "RearPickerX", request, out reason))
                    return false;

                if (!PickerZoneInterlockRules.VerifyRearPickerXMove(request, out reason))
                    return false;

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

        private static bool VerifyInputVisionXAvoidForPickerX(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage == null || stage.CameraX == null)
                    return true;

                if (MotionGuardRuleHelpers.IsAxisMoving(stage.CameraX))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " 이동 불가: InputVisionX가 이동 중입니다. PickerX 이동 전 InputVisionX가 Avoid 위치에 있어야 합니다.",
                        out reason);

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

        private static bool VerifyRearPickerZAxesAvoidForMove(PickerRearUnit picker, string movingName, MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            if (IsInspectionZHoldMove(request))
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
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
            if (request == null || string.IsNullOrWhiteSpace(request.TargetName))
                return false;

            if (request.TargetName.IndexOf("PickerPhase=InspectionZHold", System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return request.TargetName.IndexOf("DieBottomPosition", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   request.TargetName.IndexOf("DieSidePosition", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   request.TargetName.IndexOf("DiePlacePosition", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool VerifyRearPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveRearPickerY(request, out reason);
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeRearPickerY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveRearPickerY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!IsInspectionZHoldMove(request) &&
                !VerifyRearPickerZAxesHomeOrAvoid(machine != null ? machine.PickerRearUnit : null, "RearPickerY", out reason))
                return false;

            if (!VerifyReticleCylinderClear(machine, "RearPickerY", out reason))
                return false;

            PickerWorkZone targetZone = ResolvePickerZTargetZone(request);
            OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
            if (RequiresOutputStageZSafeForPickerY(targetZone) &&
                outputStage != null &&
                !outputStage.IsGoodStageZInAvoidOrProcessPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    "RearPickerY",
                    "RearPickerY 이동 불가: OutputStage GoodStageZ가 Avoid 또는 Process 위치가 아닙니다. pickerZone=" + targetZone + ".",
                    out reason);
            }

            if (!PickerZoneInterlockRules.VerifyRearPickerYMove(request, out reason))
                return false;

            return VerifyRearPickerNotBusy(machine != null ? machine.PickerRearUnit : null, "RearPickerY", out reason);
        }

        private static bool VerifyRearPickerT(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveRearPickerT(request.Machine, request.MovingName, out reason);
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeRearPickerT(request.Machine, request.MovingName, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveRearPickerT(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
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

        private static bool CanHomeRearPickerX(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                string axisReason;
                if (stage != null &&
                    !MotionGuardRuleHelpers.IsAxisNotHomedOrAtHomePosition(stage.CameraX, "InputVisionX", out axisReason))
                {
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason,
                        out reason);
                }

                if (stage != null && !IsExpanderZHomeAvoidProcessOrReady(stage))
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. InputExpandingZ must be at Home(0), Avoid, Process or Ready position.",
                        out reason);

                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
                if (front != null && !front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. FrontPickerY must be at Avoid position.",
                        out reason);

                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
                if (rear != null && !rear.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. RearPickerY must be at Avoid position.",
                        out reason);

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
                if (!TryResolvePairedZAxis(movingName, out zAxis))
                    return true;

                PickerRearUnit rear = machine != null ? machine.PickerRearUnit : null;
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
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveRearPickerZ(request, out reason);
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
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

        private static bool CanMoveRearPickerZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            string movingName = request != null ? request.MovingName : "RearPickerZ";
            PickerWorkZone targetZone = ResolvePickerZTargetZone(request);

            if (!CanHomeRearPickerZ(machine, movingName, out reason))
                return false;

            // PickerZ는 현재 작업 존 기준으로 필요한 feeder만 확인한다.
            // Avoid 복귀는 Z가 안전 위치로 올라가는 동작이므로 feeder 위치로 차단하지 않는다.
            // 존을 알 수 없으면 기존처럼 양쪽 feeder를 모두 확인한다.
            InputFeederUnit inputFeeder = machine != null ? machine.InputFeederUnit : null;
            OutputFeederUnit outputFeeder = machine != null ? machine.OutputFeederUnit : null;

            if (RequiresInputFeederAvoid(targetZone) &&
                inputFeeder != null &&
                !inputFeeder.IsWaferFeederYInAvoidPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: InputFeederY가 Avoid 위치가 아닙니다. pickerZone=" + targetZone + ".",
                    out reason);
            }

            if (RequiresOutputFeederAvoid(targetZone) &&
                outputFeeder != null &&
                !outputFeeder.IsBinFeederYInAvoidPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: OutputFeederY가 Avoid 위치가 아닙니다. pickerZone=" + targetZone + ".",
                    out reason);
            }

            // Input PickUp 존에서만 ExpanderZ와 직접 간섭을 확인한다.
            InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
            if (RequiresInputStageZSafe(targetZone) &&
                stage != null &&
                !IsExpanderZAvoidProcessOrReady(stage))
            {
                return MotionGuardRuleHelpers.Block(
                    movingName,
                    movingName + " 이동 불가: InputExpandingZ가 Avoid/Process/Ready 위치가 아닙니다. pickerZone=" + targetZone + ".",
                    out reason);
            }

            return VerifyRearPickerNotBusy(machine != null ? machine.PickerRearUnit : null, movingName, out reason);
        }

        private static PickerWorkZone ResolvePickerZTargetZone(MotionGuardRuleContext request)
        {
            string name = request != null ? request.TargetName ?? string.Empty : string.Empty;
            if (Contains(name, "PickerZone=Input") || Contains(name, "DiePick") || Contains(name, "PickPosition"))
                return PickerWorkZone.Input;
            if (Contains(name, "PickerZone=Output") || Contains(name, "DiePlace") || Contains(name, "PlacePosition"))
                return PickerWorkZone.Output;
            if (Contains(name, "PickerZone=Bottom") || Contains(name, "DieBottom") || Contains(name, "BottomPosition"))
                return PickerWorkZone.Bottom;
            if (Contains(name, "PickerZone=Side") || Contains(name, "DieSide") || Contains(name, "SidePosition"))
                return PickerWorkZone.Side;
            if (Contains(name, "PickerZone=Avoid") || Contains(name, "AvoidPosition") || Contains(name, "SafeRetreat"))
                return PickerWorkZone.Avoid;

            return PickerWorkZone.Unknown;
        }

        private static bool RequiresInputFeederAvoid(PickerWorkZone targetZone)
        {
            return targetZone == PickerWorkZone.Input ||
                   targetZone == PickerWorkZone.Unknown;
        }

        private static bool RequiresOutputFeederAvoid(PickerWorkZone targetZone)
        {
            return targetZone == PickerWorkZone.Output ||
                   targetZone == PickerWorkZone.Unknown;
        }

        private static bool RequiresInputStageZSafe(PickerWorkZone targetZone)
        {
            return targetZone == PickerWorkZone.Input ||
                   targetZone == PickerWorkZone.Unknown;
        }

        private static bool RequiresOutputStageZSafeForPickerY(PickerWorkZone targetZone)
        {
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
            if (machine == null || machine.VisionUnit == null)
                return true;

            if (IsCylinderMoving(machine.VisionUnit.ReticleLift))
                return MotionGuardRuleHelpers.Block(movingName, "ReticleLift is moving.", out reason);
            if (IsCylinderMoving(machine.VisionUnit.ReticleFrontSideSlide))
                return MotionGuardRuleHelpers.Block(movingName, "ReticleSideSlideFront is moving.", out reason);
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
            if (!MotionGuardRuleHelpers.IsAxisMoving(axis))
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(movingName, names[i], System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static bool VerifyRearPickerZAxesAvoid(PickerRearUnit picker, string movingName, out string reason)
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
                        movingName + " HOME blocked. Rear" + zAxis + " must be at Avoid position.",
                        out reason);
            }

            return true;
        }

        private static bool VerifyRearPickerZAxesHomeOrAvoid(PickerRearUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = ResolveRearPickerAxis(picker, zAxis);
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
            if (axis == null)
                return true;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;

            if (System.Math.Abs(axis.ActualPosition) <= tolerance)
                return true;

            return isTeachingAvoid != null && isTeachingAvoid();
        }

        private static bool IsExpanderZHomeAvoidProcessOrReady(InputStageUnit stage)
        {
            if (stage == null || stage.ExpanderZ == null)
                return true;

            double tolerance = stage.ExpanderZ.Config != null && stage.ExpanderZ.Config.InPositionTolerance > 0.0
                ? stage.ExpanderZ.Config.InPositionTolerance
                : 0.05;

            double actual = stage.ExpanderZ.ActualPosition;
            if (System.Math.Abs(actual) <= tolerance)
                return true;

            StageAxisPositions waferZ = stage.Recipe != null ? stage.Recipe.WaferZ : null;
            if (waferZ == null)
                return false;

            return System.Math.Abs(actual - waferZ.AvoidPosition) <= tolerance ||
                   System.Math.Abs(actual - waferZ.ProcessPosition) <= tolerance ||
                   System.Math.Abs(actual - waferZ.ReadyPosition) <= tolerance;
        }

        // RearPickerX 이동 전제: ExpanderZ가 Avoid/Process/Ready 위치여야 한다. (Home(0)은 제외)
        private static bool IsExpanderZAvoidProcessOrReady(InputStageUnit stage)
        {
            if (stage == null || stage.ExpanderZ == null)
                return true;

            double tolerance = stage.ExpanderZ.Config != null && stage.ExpanderZ.Config.InPositionTolerance > 0.0
                ? stage.ExpanderZ.Config.InPositionTolerance
                : 0.05;

            StageAxisPositions waferZ = stage.Recipe != null ? stage.Recipe.WaferZ : null;
            if (waferZ == null)
                return false;

            double actual = stage.ExpanderZ.ActualPosition;
            return System.Math.Abs(actual - waferZ.AvoidPosition) <= tolerance ||
                   System.Math.Abs(actual - waferZ.ProcessPosition) <= tolerance ||
                   System.Math.Abs(actual - waferZ.ReadyPosition) <= tolerance;
        }

        private static BaseAxis ResolveRearPickerAxis(PickerRearUnit picker, PickerAxis axis)
        {
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
