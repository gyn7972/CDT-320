using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Motion
{
    public enum AxisMoveWaitFailure
    {
        None = 0,
        Timeout = -1,
        AxisMissing = -2,
        ServoOff = -3,
        Alarm = -4,
        Moving = -5,
        InPositionSignalOff = -6,
        TargetToleranceOut = -7
    }

    public sealed class AxisMoveWaitResult
    {
        public AxisMoveWaitResult(
            AxisMoveWaitFailure failure,
            string reason,
            string axisState)
        {
            Failure = failure;
            Reason = reason ?? string.Empty;
            AxisState = axisState ?? string.Empty;
        }

        public AxisMoveWaitFailure Failure { get; private set; }
        public string Reason { get; private set; }
        public string AxisState { get; private set; }
        public bool Success { get { return Failure == AxisMoveWaitFailure.None; } }
        public int Code { get { return (int)Failure; } }

        public static AxisMoveWaitResult Ok(BaseAxis axis, double target, double tolerance)
        {
            return new AxisMoveWaitResult(
                AxisMoveWaitFailure.None,
                "Axis reached target position.",
                AxisMoveWaiter.BuildAxisState(axis, target, tolerance));
        }
    }

    public static class AxisMoveWaiter
    {
        public static async Task<AxisMoveWaitResult> WaitMoveDoneInPositionAsync(
            BaseAxis axis,
            double target,
            double tolerance,
            int timeoutMs,
            int settleMs)
        {
            return await WaitMoveDoneInPositionAsync(axis, target, tolerance, timeoutMs, settleMs, CancellationToken.None);
        }

        public static async Task<AxisMoveWaitResult> WaitMoveDoneInPositionAsync(
            BaseAxis axis,
            double target,
            double tolerance,
            int timeoutMs,
            int settleMs,
            CancellationToken ct)
        {
            try
            {
                if (axis == null)
                    return Fail(axis, target, tolerance, AxisMoveWaitFailure.AxisMissing, "Axis is null.");

                int timeout = timeoutMs > 0 ? timeoutMs : 60000;
                int settle = settleMs > 0 ? settleMs : 0;
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeout);

                while (DateTime.UtcNow <= deadline)
                {
                    ct.ThrowIfCancellationRequested();

                    if (axis.IsAlarm)
                        return Fail(axis, target, tolerance, AxisMoveWaitFailure.Alarm, "Axis alarm is ON.");

                    if (IsMoveDoneInPosition(axis, target, tolerance))
                    {
                        if (settle > 0)
                        {
                            await Task.Delay(settle, ct).ConfigureAwait(false);
                            if (!IsMoveDoneInPosition(axis, target, tolerance))
                                continue;
                        }

                        return AxisMoveWaitResult.Ok(axis, target, tolerance);
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }

                return ResolveFailure(axis, target, tolerance);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Fail(axis, target, tolerance, AxisMoveWaitFailure.Timeout, "Axis wait exception: " + ex.Message);
            }
            finally
            {
            }
        }

        public static string BuildAxisState(BaseAxis axis, double target, double tolerance)
        {
            if (axis == null)
                return "axis=null, target=" + target + ", tolerance=" + tolerance;

            return "axis=" + axis.Name +
                   ", servo=" + axis.IsServoOn +
                   ", alarm=" + axis.IsAlarm +
                   ", alarmCode=" + axis.AlarmCode +
                   ", moving=" + axis.IsMoving +
                   ", inPositionSignal=" + axis.IsInPosition +
                   ", actual=" + axis.ActualPosition +
                   ", command=" + axis.CommandPosition +
                   ", target=" + target +
                   ", tolerance=" + tolerance +
                   ", targetInTolerance=" + IsTargetInTolerance(axis, target, tolerance);
        }

        public static string ResolveAlarmCode(string prefix, AxisMoveWaitResult waitResult)
        {
            AxisMoveWaitFailure failure = waitResult != null ? waitResult.Failure : AxisMoveWaitFailure.Timeout;
            return ResolveAlarmCode(prefix, failure);
        }

        public static string ResolveAlarmCode(string prefix, AxisMoveWaitFailure failure)
        {
            switch (failure)
            {
                case AxisMoveWaitFailure.Alarm: return prefix + "-ALARM";
                case AxisMoveWaitFailure.AxisMissing: return prefix + "-AXIS-MISSING";
                case AxisMoveWaitFailure.ServoOff: return prefix + "-SERVO-OFF";
                case AxisMoveWaitFailure.Moving: return prefix + "-MOVING";
                case AxisMoveWaitFailure.InPositionSignalOff: return prefix + "-INPOS-SIGNAL";
                case AxisMoveWaitFailure.TargetToleranceOut: return prefix + "-POSITION";
                default: return prefix + "-WAIT";
            }
        }

        public static string FormatResult(AxisMoveWaitResult waitResult, string fallbackState)
        {
            if (waitResult == null)
                return fallbackState ?? string.Empty;

            string state = string.IsNullOrEmpty(waitResult.AxisState) ? (fallbackState ?? string.Empty) : waitResult.AxisState;
            return "reason=" + waitResult.Reason + ", failure=" + waitResult.Failure + ", " + state;
        }

        private static AxisMoveWaitResult ResolveFailure(BaseAxis axis, double target, double tolerance)
        {
            if (axis == null)
                return Fail(axis, target, tolerance, AxisMoveWaitFailure.AxisMissing, "Axis is null.");
            if (!axis.IsServoOn)
                return Fail(axis, target, tolerance, AxisMoveWaitFailure.ServoOff, "Axis servo is OFF.");
            if (axis.IsAlarm)
                return Fail(axis, target, tolerance, AxisMoveWaitFailure.Alarm, "Axis alarm is ON.");
            if (axis.IsMoving)
                return Fail(axis, target, tolerance, AxisMoveWaitFailure.Moving, "Axis is still moving.");
            if (!axis.IsInPosition)
                return Fail(axis, target, tolerance, AxisMoveWaitFailure.InPositionSignalOff, "Axis in-position signal is OFF.");
            if (!IsTargetInTolerance(axis, target, tolerance))
                return Fail(axis, target, tolerance, AxisMoveWaitFailure.TargetToleranceOut, "Axis actual position is out of target tolerance.");

            return Fail(axis, target, tolerance, AxisMoveWaitFailure.Timeout, "Axis move wait timeout.");
        }

        private static AxisMoveWaitResult Fail(
            BaseAxis axis,
            double target,
            double tolerance,
            AxisMoveWaitFailure failure,
            string reason)
        {
            return new AxisMoveWaitResult(failure, reason, BuildAxisState(axis, target, tolerance));
        }

        private static bool IsMoveDoneInPosition(BaseAxis axis, double target, double tolerance)
        {
            return axis != null &&
                   axis.IsServoOn &&
                   !axis.IsAlarm &&
                   !axis.IsMoving &&
                   axis.IsInPosition &&
                   IsTargetInTolerance(axis, target, tolerance);
        }

        private static bool IsTargetInTolerance(BaseAxis axis, double target, double tolerance)
        {
            return axis != null && Math.Abs(axis.ActualPosition - target) <= tolerance;
        }
    }
}
