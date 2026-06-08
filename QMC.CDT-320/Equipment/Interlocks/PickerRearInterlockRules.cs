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
            return VerifyRearPickerNotBusy(request.Machine.PickerRearUnit, "RearPickerX", out reason);
        }

        private static bool VerifyRearPickerY(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
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
            return VerifyRearPickerNotBusy(request.Machine.PickerRearUnit, request.MovingName, out reason);
        }

        private static bool VerifyRearPickerZ(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
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
    }
}
