using QMC.Common.Motion;

using QMC.Common.IO;

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
                if (!VerifyFrontPickerZAxesAvoidForMove(front, "FrontPickerX", out reason))
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

        private static bool VerifyFrontPickerZAxesAvoidForMove(PickerFrontUnit picker, string movingName, out string reason)
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
                        movingName + " 이동 불가: Front" + zAxis + " 축이 Avoid 위치가 아닙니다.",
                        out reason);
            }

            return true;
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
            if (!CanHomeFrontPickerY(machine, out reason))
                return false;

            if (!VerifyReticleCylinderClear(machine, "FrontPickerY", out reason))
                return false;

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

                if (stage != null && !IsExpanderZHomeAvoidOrProcess(stage))
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputExpandingZ must be at Home(0), Avoid, or Process position.",
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
                    return CanMoveFrontPickerZ(request.Machine, request.MovingName, out reason);
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

        private static bool CanMoveFrontPickerZ(CDT320_Machine machine, string movingName, out string reason)
        {
            if (!CanHomeFrontPickerZ(machine, movingName, out reason))
                return false;

            return VerifyFrontPickerNotBusy(machine != null ? machine.PickerFrontUnit : null, movingName, out reason);
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

        private static bool IsExpanderZHomeAvoidOrProcess(InputStageUnit stage)
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
                   System.Math.Abs(actual - waferZ.ProcessPosition) <= tolerance;
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
