using QMC.CDT320.Motion.SharedRailX;

namespace QMC.CDT320.Interlocks
{
    public static class SharedRailXInterlockRules
    {
        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;
            if (request == null || request.Machine == null ||
                (request.MoveKind != MotionGuardMoveKind.AxisMove &&
                 request.MoveKind != MotionGuardMoveKind.AxisTeachingMove))
                return true;
            if (SharedRailXMotionRuntime.IsInternalDispatch)
                return true;

            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(request.Machine);
            if (service == null)
                return true;

            var axis = request.GetAxis(request.MovingName);
            if (axis == null)
                axis = request.GetAxis(request.MovingKey);

            if (axis == null)
                return true;

            return service.VerifySingleAxisMove(axis, request.TargetValue, out reason);
        }
    }
}
