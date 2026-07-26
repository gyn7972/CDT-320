using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;
using QMC.Common.Motion;
using QMC.CDT320.Interlocks;

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
            return MoveAxisAsync(axis, targetPosition, velocity, false);
        }

        public static Task<int> MoveAxisAsync(BaseAxis axis, double targetPosition, double velocity, bool forceMove)
        {
            if (axis == null)
                return Task.FromResult(-1);

            SharedRailXMotionService service = ResolveService(null);
            SharedRailXAxis railAxis;
            if (service != null && service.TryResolve(axis, out railAxis))
            {
                SharedRailXMovePlan plan = SharedRailXMovePlan.Create(railAxis.ToString(), velocity)
                    .Add(railAxis, targetPosition);
                plan.ForceMove = forceMove;
                return service.MoveAsync(plan);
            }

            if (forceMove)
                return MoveAxisAbsoluteForceAsync(axis, targetPosition, velocity);
            return axis.MoveAbsoluteAsync(targetPosition, velocity);
        }

        public static Task<int> MoveAxisAsync(
            BaseAxis axis,
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration)
        {
            return MoveAxisAsync(axis, targetPosition, velocity, acceleration, deceleration, false);
        }

        public static Task<int> MoveAxisAsync(
            BaseAxis axis,
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration,
            bool forceMove)
        {
            if (axis == null)
                return Task.FromResult(-1);

            SharedRailXMotionService service = ResolveService(null);
            SharedRailXAxis railAxis;
            if (service != null && service.TryResolve(axis, out railAxis))
            {
                SharedRailXMovePlan plan = SharedRailXMovePlan.Create("SingleAxisGuard", velocity)
                    .Add(railAxis, targetPosition, velocity, acceleration, deceleration);
                plan.ForceMove = forceMove;
                return service.MoveAsync(plan);
            }

            return MoveAxisWithTemporaryMotionAsync(axis, targetPosition, velocity, acceleration, deceleration, forceMove);
        }

        public static void MoveJogContinuous(BaseAxis axis, int direction, double speed)
        {
            if (axis == null)
                return;

            SharedRailXMotionService service = ResolveService(null);
            // 이동 중 반복 조그 입력은 새 명령은 막고, 현재 방향 인터락만 재확인한다.
            if (IsAxisMovingForJog(axis))
            {
                VerifyJogSafetyWhileMoving(axis, direction, service);
                return;
            }

            if (service != null && service.IsSharedRailAxis(axis))
            {
                string reason;
                double guardTarget = ResolveJogGuardTarget(axis, direction);
                if (!MotionGuardRuntime.VerifyAxisContinuousJogWithoutSharedRailX(
                    axis,
                    guardTarget,
                    BuildContinuousJogTargetName(direction),
                    out reason))
                    return;

                if (!service.VerifyJogMove(axis, direction, out reason))
                {
                    AlarmManager.Raise(AlarmSeverity.Error, "SHARED-RAIL-X", "SharedRailX", reason);
                    return;
                }
            }

            using (EnterInternalDispatch())
                axis.MoveJogContinuous(direction, JogSpeedType.Custom, speed);

            if (service != null && service.IsSharedRailAxis(axis))
                StartJogGuard(axis, direction, service);
        }

        public static Task<int> MoveJogStepAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double stepDistance,
            double customSpeed)
        {
            if (axis == null)
                return Task.FromResult(-1);

            SharedRailXMotionService service = ResolveService(null);
            // 이동 중 반복 Step Jog 입력은 새 명령은 막고, 현재 방향 인터락만 재확인한다.
            if (IsAxisMovingForJog(axis))
            {
                VerifyJogSafetyWhileMoving(axis, direction, service);
                return Task.FromResult(0);
            }

            double velocity = ResolveJogVelocity(axis, speedType, customSpeed);
            double target = axis.ActualPosition + ((direction < 0 ? -1.0 : 1.0) * Math.Abs(stepDistance));
            string reason;
            if (!MotionGuardRuntime.VerifyAxisStepJogWithoutSharedRailX(axis, target, "StepJog", out reason))
                return Task.FromResult(-1);

            return MoveJogStepWithVerifyAsync(axis, target, velocity);
        }

        public static void VerifyJogSafetyWhileMoving(BaseAxis axis, int direction)
        {
            VerifyJogSafetyWhileMoving(axis, direction, ResolveService(null));
        }

        private static void VerifyJogSafetyWhileMoving(BaseAxis axis, int direction, SharedRailXMotionService service)
        {
            try
            {
                if (axis == null)
                    return;

                double guardTarget = ResolveJogGuardTarget(axis, direction);
                string reason;
                if (service != null && service.IsSharedRailAxis(axis))
                {
                    if (!MotionGuardRuntime.VerifyAxisContinuousJogWithoutSharedRailX(
                        axis,
                        guardTarget,
                        BuildContinuousJogTargetName(direction),
                        out reason))
                        return;

                    if (!service.VerifyJogMove(axis, direction, out reason))
                    {
                        AlarmManager.Raise(AlarmSeverity.Error, "SHARED-RAIL-X", "SharedRailX", reason);
                    }

                    return;
                }

                MotionGuardRuntime.VerifyAxisContinuousJog(axis, guardTarget, "ContinuousJog", out reason);
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "SHARED-RAIL-X-JOG-GUARD", "SharedRailX", ex.Message);
            }
            finally
            {
            }
        }

        private static bool IsAxisMovingForJog(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return false;

                axis.UpdateStatus();
                return axis.IsMoving;
            }
            catch
            {
                return axis != null && axis.IsMoving;
            }
            finally
            {
            }
        }

        private static async Task<int> MoveJogStepWithVerifyAsync(BaseAxis axis, double target, double velocity)
        {
            // 기존 조건: 이동 후 AxisMoveWaiter 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 제거(R3).
            return await MoveAxisAsync(axis, target, velocity, true).ConfigureAwait(false);
        }

        private static double ResolveAxisInPositionTolerance(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.05;
        }

        private static string BuildContinuousJogTargetName(int direction)
        {
            return direction >= 0 ? "ContinuousJogPlus" : "ContinuousJogMinus";
        }

        private static double ResolveJogVelocity(BaseAxis axis, JogSpeedType speedType, double customSpeed)
        {
            if (axis == null || axis.Config == null)
                return customSpeed > 0 ? customSpeed : 1.0;

            switch (speedType)
            {
                case JogSpeedType.Coarse:
                    return axis.Config.JogCoarseVelocity;
                case JogSpeedType.Custom:
                    return customSpeed > 0 ? customSpeed : axis.Config.JogFineVelocity;
                case JogSpeedType.Fine:
                default:
                    return axis.Config.JogFineVelocity;
            }
        }

        private static double ResolveJogGuardTarget(BaseAxis axis, int direction)
        {
            if (axis == null || axis.Setup == null)
                return axis != null ? axis.ActualPosition : 0.0;

            double sign = direction > 0 ? 1.0 : -1.0;
            double probeDistance = ResolveJogGuardProbeDistance(axis);
            double target = axis.ActualPosition + (sign * probeDistance);

            if (axis.Setup.SoftLimitEnabled)
            {
                if (target > axis.Setup.SoftLimitPlus)
                    target = axis.Setup.SoftLimitPlus;
                if (target < axis.Setup.SoftLimitMinus)
                    target = axis.Setup.SoftLimitMinus;
            }

            return target;
        }

        private static double ResolveJogGuardProbeDistance(BaseAxis axis)
        {
            double tolerance = axis != null && axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return Math.Max(1.0, tolerance * 10.0);
        }

        public static SharedRailXMotionService ResolveService(CDT320_Machine machine)
        {
            SharedRailXMotionService service = ServiceProvider != null ? ServiceProvider() : null;
            if (service != null)
                return service;

            return machine != null ? new SharedRailXMotionService(machine) : null;
        }

        internal static async Task<int> MoveAxisWithTemporaryMotionAsync(
            BaseAxis axis,
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration)
        {
            return await MoveAxisWithTemporaryMotionAsync(
                axis,
                targetPosition,
                velocity,
                acceleration,
                deceleration,
                false).ConfigureAwait(false);
        }

        internal static async Task<int> MoveAxisWithTemporaryMotionAsync(
            BaseAxis axis,
            double targetPosition,
            double velocity,
            double acceleration,
            double deceleration,
            bool forceMove)
        {
            if (axis == null)
                return -1;

            bool useCustomAcceleration = axis.Config != null && acceleration > 0.0 && deceleration > 0.0;
            double oldAcceleration = useCustomAcceleration ? axis.Config.Acceleration : 0.0;
            double oldDeceleration = useCustomAcceleration ? axis.Config.Deceleration : 0.0;
            double oldDefaultVelocity = useCustomAcceleration ? axis.Config.DefaultVelocity : 0.0;
            try
            {
                if (useCustomAcceleration)
                {
                    // 가감속 이중 스케일(S²) 차단(2026-07-26): 이미 스케일된 가감속을 임시 치환한
                    // 상태에서 전달 velocity가 "DefaultVelocity×스케일"과 일치하면 축 레이어의
                    // 기본속도 추론이 가감속에 스케일을 재적용했다. FollowMove와 동일하게
                    // DefaultVelocity=0 치환으로 추론을 결정적으로 차단한다(finally 원복).
                    axis.Config.DefaultVelocity = 0.0;
                    axis.Config.Acceleration = acceleration;
                    axis.Config.Deceleration = deceleration;
                }

                if (forceMove)
                    return await MoveAxisAbsoluteForceAsync(axis, targetPosition, velocity).ConfigureAwait(false);

                return await axis.MoveAbsoluteAsync(targetPosition, velocity).ConfigureAwait(false);
            }
            finally
            {
                if (useCustomAcceleration)
                {
                    axis.Config.DefaultVelocity = oldDefaultVelocity;
                    axis.Config.Acceleration = oldAcceleration;
                    axis.Config.Deceleration = oldDeceleration;
                }
            }
        }

        private static async Task<int> MoveAxisAbsoluteForceAsync(BaseAxis axis, double targetPosition, double velocity)
        {
            using (BaseAxis.BeginForceMoveScope())
                return await axis.MoveAbsoluteAsync(targetPosition, velocity).ConfigureAwait(false);
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

                        AlarmManager.Raise(AlarmSeverity.Error, "SHARED-RAIL-X-JOG-STOP", "SharedRailX", reason);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                AlarmManager.Raise(AlarmSeverity.Error, "SHARED-RAIL-X-JOG-GUARD", "SharedRailX", ex.Message);
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
