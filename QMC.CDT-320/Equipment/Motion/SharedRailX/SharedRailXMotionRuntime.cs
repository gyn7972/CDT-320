using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Motion.SharedRailX
{
    public static class SharedRailXMotionRuntime
    {
        private static readonly AsyncLocal<int> InternalDispatchDepth = new AsyncLocal<int>();
        private static readonly ConcurrentDictionary<BaseAxis, CancellationTokenSource> JogGuardTokens =
            new ConcurrentDictionary<BaseAxis, CancellationTokenSource>();

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

            using (EnterInternalDispatch())
                axis.MoveJogContinuous(direction, JogSpeedType.Custom, speed);

            if (service != null && service.IsSharedRailAxis(axis))
                StartJogGuard(axis, direction, service);
        }

        public static SharedRailXMotionService ResolveService(CDT320_Machine machine)
        {
            SharedRailXMotionService service = ServiceProvider != null ? ServiceProvider() : null;
            if (service != null)
                return service;

            return machine != null ? new SharedRailXMotionService(machine) : null;
        }

        private static void StartJogGuard(BaseAxis axis, int direction, SharedRailXMotionService service)
        {
            if (axis == null || service == null)
                return;

            StopJogGuard(axis);
            var cts = new CancellationTokenSource();
            JogGuardTokens[axis] = cts;
            Task.Run(() => MonitorJogDistanceAsync(axis, direction, cts.Token), cts.Token);
        }

        private static void StopJogGuard(BaseAxis axis)
        {
            if (axis == null)
                return;

            CancellationTokenSource old;
            if (JogGuardTokens.TryRemove(axis, out old) && old != null)
            {
                try { old.Cancel(); } catch { }
                old.Dispose();
            }
        }

        private static async Task MonitorJogDistanceAsync(BaseAxis axis, int direction, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(20, ct).ConfigureAwait(false);
                    if (axis == null || !axis.IsMoving)
                        break;

                    SharedRailXMotionService service = ResolveService(null);
                    if (service == null || !service.IsSharedRailAxis(axis))
                        break;

                    string reason;
                    if (!service.VerifyJogCurrentDistance(axis, direction, out reason))
                    {
                        using (EnterInternalDispatch())
                            axis.StopJog();

                        AlarmManager.Raise(AlarmSeverity.Warning, "SHARED-RAIL-X-JOG-STOP", "SharedRailX", reason);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Warning, "SHARED-RAIL-X-JOG-GUARD", "SharedRailX", ex.Message);
            }
            finally
            {
                StopJogGuard(axis);
            }
        }

        private sealed class DispatchScope : IDisposable
        {
            private readonly IDisposable _motionGuardBypass;
            private bool _disposed;

            public DispatchScope()
            {
                _motionGuardBypass = BaseAxis.BeginMotionGuardBypass();
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                if (_motionGuardBypass != null)
                    _motionGuardBypass.Dispose();
                InternalDispatchDepth.Value = Math.Max(0, InternalDispatchDepth.Value - 1);
            }
        }
    }
}
