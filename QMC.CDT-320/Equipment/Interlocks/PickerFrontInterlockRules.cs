using QMC.Common.IO;
using QMC.Common.Motion;
using System;

namespace QMC.CDT320.Interlocks
{
    public static class PickerFrontInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null)
                return true;

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerX"))
                return VerifyFrontPickerX(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerY"))
                return VerifyFrontPickerY(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerT0", "FrontPickerT1", "FrontPickerT2", "FrontPickerT3"))
                return VerifyFrontPickerT(request, out reason);

            if (MotionGuardRuleHelpers.IsMoving(request, "FrontPickerZ0", "FrontPickerZ1", "FrontPickerZ2", "FrontPickerZ3"))
                return VerifyFrontPickerZ(request, out reason);

            return true;
        }

        private static bool VerifyFrontPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveFrontPickerX(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontPickerX(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveFrontPickerX(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            CDT320_Machine machine = request != null ? request.Machine : null;

            try
            {
                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
                if (!VerifyFrontPickerZAxesAvoidForMove(front, "FrontPickerX", request, out reason))
                    return false;
                
                if (!PickerZoneInterlockRules.VerifyFrontPickerXMove(request, out reason))
                    return false;

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

        private static bool VerifyFrontPickerZAxesAvoidForMove(PickerFrontUnit picker, string movingName, MotionGuardRuleContext request, out string reason)
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
                        movingName + " 이동 불가: Front" + zAxis + " 축이 Avoid 위치가 아닙니다.",
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

        private static bool VerifyFrontPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveFrontPickerY(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontPickerY(request.Machine, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveFrontPickerY(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            if (!IsInspectionZHoldMove(request) &&
                !VerifyFrontPickerZAxesHomeOrAvoid(machine != null ? machine.PickerFrontUnit : null, "FrontPickerY", out reason))
                return false;

            if (!VerifyReticleCylinderClear(machine, "FrontPickerY", out reason))
                return false;

            PickerWorkZone targetZone = ResolvePickerZTargetZone(request);
            OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
            if (RequiresOutputStageZSafeForPickerY(targetZone) &&
                outputStage != null &&
                !outputStage.IsGoodStageZInAvoidOrProcessPosition())
            {
                return MotionGuardRuleHelpers.Block(
                    "FrontPickerY",
                    "FrontPickerY 이동 불가: OutputStage GoodStageZ가 Avoid 또는 Process 위치가 아닙니다. pickerZone=" + targetZone + ".",
                    out reason);
            }

            if (!PickerZoneInterlockRules.VerifyFrontPickerYMove(request, out reason))
                return false;

            return VerifyFrontPickerNotBusy(machine != null ? machine.PickerFrontUnit : null, "FrontPickerY", out reason);
        }

        private static bool VerifyFrontPickerT(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveFrontPickerT(request.Machine, request.MovingName, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontPickerT(request.Machine, request.MovingName, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanMoveFrontPickerT(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
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

        private static bool CanHomeFrontPickerX(CDT320_Machine machine, out string reason)
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
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputVisionX must be not homed yet or at Home position. " + axisReason,
                        out reason);
                }

                if (stage != null && !IsExpanderZHomeAvoidProcessOrReady(stage))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputExpandingZ must be at Home(0), Avoid, Process or Ready position.",
                        out reason);

                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
                if (front != null && !front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. FrontPickerY must be at Avoid position.",
                        out reason);

                if (!VerifyFrontPickerZAxesAvoid(front, "FrontPickerX", out reason))
                    return false;

                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder != null && !feeder.IsWaferFeederYInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputFeederY must be at Avoid position.",
                        out reason);

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

        private static bool CanHomeFrontPickerY(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                if (!VerifyFrontPickerZAxesHomeOrAvoid(machine != null ? machine.PickerFrontUnit : null, "FrontPickerY", out reason))
                    return false;

                // InputExpandingZ가 Avoid/Process/Ready 위치여야 FrontPickerY 이동 가능.
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !IsExpanderZAvoidProcessOrReady(stage))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerY",
                        "FrontPickerY 이동 불가: InputExpandingZ가 Avoid/Process/Ready 위치가 아닙니다.",
                        out reason);

                // OutputStage GoodStageZ가 안전 위치(Avoid 또는 Process)여야 FrontPickerY 이동 가능.
                OutputStageUnit outputStage = machine != null ? machine.OutputStageUnit : null;
                if (outputStage != null && !outputStage.IsGoodStageZInAvoidOrProcessPosition())
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerY",
                        "FrontPickerY 이동 불가: OutputStage GoodStageZ가 Avoid 또는 Process 위치가 아닙니다.",
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

        private static bool CanHomeFrontPickerT(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;

            try
            {
                PickerAxis zAxis;
                if (!TryResolvePairedZAxis(movingName, out zAxis))
                    return true;

                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
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

        private static bool VerifyFrontPickerZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                // 일반 이동 인터락 확인
                case MotionGuardMoveKind.AxisMove:
                // 티칭 이동 인터락 확인
                case MotionGuardMoveKind.AxisTeachingMove:
                    return CanMoveFrontPickerZ(request, out reason);
                // 홈 이동 인터락 확인
                case MotionGuardMoveKind.AxisHome:
                    return CanHomeFrontPickerZ(request.Machine, request.MovingName, out reason);
                default:
                    return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);
            }
        }

        private static bool CanHomeFrontPickerZ(CDT320_Machine machine, string movingName, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        private static bool CanMoveFrontPickerZ(MotionGuardRuleContext request, out string reason)
        {
            CDT320_Machine machine = request != null ? request.Machine : null;
            string movingName = request != null ? request.MovingName : "FrontPickerZ";
            PickerWorkZone targetZone = ResolvePickerZTargetZone(request);

            if (!CanHomeFrontPickerZ(machine, movingName, out reason))
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

            return VerifyFrontPickerNotBusy(machine != null ? machine.PickerFrontUnit : null, movingName, out reason);
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
            return (value ?? string.Empty).IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
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

        private static bool VerifyFrontPickerNotBusy(PickerFrontUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
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

        private static bool VerifyFrontPickerZAxesAvoid(PickerFrontUnit picker, string movingName, out string reason)
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
                        movingName + " HOME blocked. Front" + zAxis + " must be at Avoid position.",
                        out reason);
            }

            return true;
        }

        private static bool VerifyFrontPickerZAxesHomeOrAvoid(PickerFrontUnit picker, string movingName, out string reason)
        {
            reason = string.Empty;
            if (picker == null)
                return true;

            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            for (int i = 0; i < zAxes.Length; i++)
            {
                PickerAxis zAxis = zAxes[i];
                BaseAxis axis = ResolveFrontPickerAxis(picker, zAxis);
                if (!IsAxisAtHomeOrTeachingAvoid(axis, () => picker.IsPickerAxisInTeachingPosition(zAxis, "AvoidPosition")))
                    return MotionGuardRuleHelpers.Block(
                        movingName,
                        movingName + " HOME blocked. Front" + zAxis + " must be at Home(0) or Avoid position.",
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

            //이렇게 하면 홈 위치가 0이구나..
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
                   System.Math.Abs(actual - waferZ.ProcessPosition) <= tolerance||
                   System.Math.Abs(actual - waferZ.ReadyPosition) <= tolerance;
        }

        // FrontPickerX,Y 평면 이동 전제: ExpanderZ가 Avoid/Process/Ready 위치여야 한다. (Home(0)은 제외)
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

        private static BaseAxis ResolveFrontPickerAxis(PickerFrontUnit picker, PickerAxis axis)
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

        private static void LogBlockedReason(string reason)
        {
            try
            {
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
