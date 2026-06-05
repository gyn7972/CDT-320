using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Motion.SharedRailX
{
    public static class SharedRailXMotionRuntime
    {
        private static readonly AsyncLocal<int> InternalDispatchDepth = new AsyncLocal<int>();

        public static Func<SharedRailXMotionService> ServiceProvider { get; set; }

        public static bool IsInternalDispatch
        {
            get { return InternalDispatchDepth.Value > 0; }
        }

        public static IDisposable EnterInternalDispatch()
        {
            InternalDispatchDepth.Value = InternalDispatchDepth.Value + 1;
            return new DispatchScope();
        }

        public static bool IsSharedRailAxis(BaseAxis axis)
        {
            SharedRailXMotionService service = ResolveService(null);
            return service != null && service.IsSharedRailAxis(axis);
        }

        public static Task<int> MoveAxisAsync(BaseAxis axis, double targetPosition, double velocity)
        {
            if (axis == null)
                return Task.FromResult(-1);

            SharedRailXMotionService service = ResolveService(null);
            SharedRailXAxis railAxis;
            if (service != null && service.TryResolve(axis, out railAxis))
                return service.MoveAsync(railAxis, targetPosition, velocity);

            return axis.MoveAbsoluteAsync(targetPosition, velocity);
        }

        public static void MoveJogContinuous(BaseAxis axis, int direction, double speed)
        {
            if (axis == null)
                return;

            SharedRailXMotionService service = ResolveService(null);
            if (service != null && service.IsSharedRailAxis(axis))
            {
                string reason;
                if (!service.VerifyJogMove(axis, direction, out reason))
                {
                    AlarmManager.Raise(AlarmSeverity.Warning, "SHARED-RAIL-X", "SharedRailX", reason);
                    return;
                }
            }

            axis.MoveJogContinuous(direction, JogSpeedType.Custom, speed);
        }

        public static SharedRailXMotionService ResolveService(CDT320_Machine machine)
        {
            SharedRailXMotionService service = ServiceProvider != null ? ServiceProvider() : null;
            if (service != null)
                return service;

            return machine != null ? new SharedRailXMotionService(machine) : null;
        }

        private sealed class DispatchScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                InternalDispatchDepth.Value = Math.Max(0, InternalDispatchDepth.Value - 1);
            }
        }
    }
}
