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
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyRearPickerXHome(request.Machine, out reason);
                default:
                    return true;
            }

            return VerifyRearPickerNotBusy(request.Machine.PickerRearUnit, "RearPickerX", out reason);
        }

        private static bool VerifyRearPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyRearPickerYHome(request.Machine, out reason);
                default:
                    return true;
            }

            if (!VerifyReticleCylinderClear(request.Machine, "RearPickerY", out reason))
                return false;
            if (request.Machine.PickerFrontUnit != null &&
                MotionGuardRuleHelpers.IsAxisMoving(request.Machine.PickerFrontUnit.PickerY))
                return MotionGuardRuleHelpers.Block("RearPickerY", "FrontPickerY is moving.", out reason);

            return VerifyRearPickerNotBusy(request.Machine.PickerRearUnit, "RearPickerY", out reason);
        }

        private static bool VerifyRearPickerT(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            switch (request.MoveKind)
            {
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return VerifyRearPickerTHome(request.Machine, request.MovingName, out reason);
                default:
                    return true;
            }

            return VerifyRearPickerNotBusy(request.Machine.PickerRearUnit, request.MovingName, out reason);
        }

        private static bool VerifyRearPickerXHome(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsVisionXInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. InputVisionX must be at Avoid position.",
                        out reason);

                if (stage != null && !stage.IsExpanderZInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerX",
                        "RearPickerX HOME blocked. InputExpandingZ must be at Avoid position.",
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

        private static bool VerifyRearPickerYHome(CDT320_Machine machine, out string reason)
        {
            reason = string.Empty;

            try
            {
                InputStageUnit stage = machine != null ? machine.InputStageUnit : null;
                if (stage != null && !stage.IsExpanderZInAvoidPosition())
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerY",
                        "RearPickerY HOME blocked. InputExpandingZ must be at Avoid position.",
                        out reason);

                PickerFrontUnit front = machine != null ? machine.PickerFrontUnit : null;
                if (front != null && !front.IsPickerAxisInTeachingPosition(PickerAxis.PickerY, "AvoidPosition"))
                    return MotionGuardRuleHelpers.Block(
                        "RearPickerY",
                        "RearPickerY HOME blocked. FrontPickerY must be at Avoid position.",
                        out reason);

                if (!VerifyRearPickerZAxesAvoid(machine != null ? machine.PickerRearUnit : null, "RearPickerY", out reason))
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

        private static bool VerifyRearPickerTHome(CDT320_Machine machine, string movingName, out string reason)
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
                case MotionGuardMoveKind.AxisMove:
                case MotionGuardMoveKind.AxisTeachingMove:
                    break;
                case MotionGuardMoveKind.AxisHome:
                    return true;
                default:
                    return true;
            }

            return VerifyRearPickerNotBusy(request.Machine.PickerRearUnit, request.MovingName, out reason);
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

        private static bool TryResolvePairedZAxis(string movingName, out PickerAxis zAxis)
        {
            zAxis = PickerAxis.PickerZ0;

            switch (movingName)
            {
                case "RearPickerT0":
                    zAxis = PickerAxis.PickerZ0;
                    return true;
                case "RearPickerT1":
                    zAxis = PickerAxis.PickerZ1;
                    return true;
                case "RearPickerT2":
                    zAxis = PickerAxis.PickerZ2;
                    return true;
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
