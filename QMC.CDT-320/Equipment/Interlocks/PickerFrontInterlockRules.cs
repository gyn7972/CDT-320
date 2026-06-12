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
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyFrontPickerXHome(request.Machine, out reason);
                default:
                    return true;
            }

            return VerifyFrontPickerNotBusy(request.Machine.PickerFrontUnit, "FrontPickerX", out reason);
        }

        private static bool VerifyFrontPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyFrontPickerYHome(request.Machine, out reason);
                default:
                    return true;
            }

            if (!VerifyReticleCylinderClear(request.Machine, "FrontPickerY", out reason))
                return false;
            if (request.Machine.PickerRearUnit != null &&
                MotionGuardRuleHelpers.IsAxisMoving(request.Machine.PickerRearUnit.PickerY))
                return MotionGuardRuleHelpers.Block("FrontPickerY", "RearPickerY is moving.", out reason);

            return VerifyFrontPickerNotBusy(request.Machine.PickerFrontUnit, "FrontPickerY", out reason);
        }

        private static bool VerifyFrontPickerT(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyFrontPickerTHome(request.Machine, request.MovingName, out reason);
                default:
                    return true;
            }

            return VerifyFrontPickerNotBusy(request.Machine.PickerFrontUnit, request.MovingName, out reason);
        }

        private static bool VerifyFrontPickerXHome(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsVisionXInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputVisionX must be at Avoid position.",
                        out reason);

                if (stage != null && !stage.IsExpanderZInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerX",
                        "FrontPickerX HOME blocked. InputExpandingZ must be at Avoid position.",
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

        private static bool VerifyFrontPickerYHome(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsExpanderZInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerY",
                        "FrontPickerY HOME blocked. InputExpandingZ must be at Avoid position.",
                        out reason);

                if (!VerifyFrontPickerZAxesAvoid(machine != null ? machine.PickerFrontUnit : null, "FrontPickerY", out reason))
                    return false;

                InputFeederUnit feeder = machine != null ? machine.InputFeederUnit : null;
                if (feeder != null && !feeder.IsWaferFeederYInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "FrontPickerY",
                        "FrontPickerY HOME blocked. InputFeederY must be at Avoid position.",
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

        private static bool VerifyFrontPickerTHome(CDT320_Machine machine, string movingName, out string reason)
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
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return true;
                default:
                    return true;
            }

            return VerifyFrontPickerNotBusy(request.Machine.PickerFrontUnit, request.MovingName, out reason);
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

        private static bool TryResolvePairedZAxis(string movingName, out PickerAxis zAxis)
        {
            zAxis = PickerAxis.PickerZ0;

            switch (movingName)
            {
                case "FrontPickerT0":
                    zAxis = PickerAxis.PickerZ0;
                    return true;
                case "FrontPickerT1":
                    zAxis = PickerAxis.PickerZ1;
                    return true;
                case "FrontPickerT2":
                    zAxis = PickerAxis.PickerZ2;
                    return true;
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
