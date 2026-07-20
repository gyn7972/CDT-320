using System;
using System.Threading;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public static class MotionGuardRuntime
    {
        private static readonly object Sync = new object();
        private static readonly AsyncLocal<AxisMoveScope> CurrentAxisMoveScope = new AsyncLocal<AxisMoveScope>();
        private static readonly AsyncLocal<CylinderMoveScope> CurrentCylinderMoveScope = new AsyncLocal<CylinderMoveScope>();
        private static readonly AsyncLocal<ExecutionModeScope> CurrentExecutionModeScope = new AsyncLocal<ExecutionModeScope>();
        private static readonly AsyncLocal<PickerYPairHomeScope> CurrentPickerYPairHomeScope = new AsyncLocal<PickerYPairHomeScope>();
        private static PickerYPairLimitSearchScope _pickerYPairLimitSearchScope;
        private static MotionGuardService _service;

        public static Func<MotionGuardContext> ContextProvider { get; set; }
        public static bool Enabled { get; set; } = true;

        public static bool VerifyAxisMove(BaseAxis axis, double targetPosition, out string reason)
        {
            return VerifyAxisMove(axis, targetPosition, false, out reason);
        }

        public static bool VerifyAxisMoveWithoutSharedRailX(BaseAxis axis, double targetPosition, out string reason)
        {
            return VerifyAxisMove(axis, targetPosition, true, out reason);
        }

        public static bool VerifyAxisContinuousJog(BaseAxis axis, double probeTargetPosition, string targetName, out string reason)
        {
            return VerifyAxisJog(axis, probeTargetPosition, targetName, MotionGuardMoveKind.AxisContinuousJog, false, out reason);
        }

        public static bool VerifyAxisContinuousJogWithoutSharedRailX(BaseAxis axis, double probeTargetPosition, string targetName, out string reason)
        {
            return VerifyAxisJog(axis, probeTargetPosition, targetName, MotionGuardMoveKind.AxisContinuousJog, true, out reason);
        }

        public static bool VerifyAxisStepJog(BaseAxis axis, double targetPosition, string targetName, out string reason)
        {
            return VerifyAxisJog(axis, targetPosition, targetName, MotionGuardMoveKind.AxisStepJog, false, out reason);
        }

        public static bool VerifyAxisStepJogWithoutSharedRailX(BaseAxis axis, double targetPosition, string targetName, out string reason)
        {
            return VerifyAxisJog(axis, targetPosition, targetName, MotionGuardMoveKind.AxisStepJog, true, out reason);
        }

        private static bool VerifyAxisMove(
            BaseAxis axis,
            double targetPosition,
            bool skipSharedRailXRule,
            out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                if (IsAxisAlreadyAtTarget(axis, targetPosition))
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                AxisMoveScope scope = CurrentAxisMoveScope.Value;
                MotionGuardExecutionMode executionMode = ResolveExecutionMode();
                MotionGuardResult result = IsMatchingScope(scope, axis, targetPosition)
                    ? service.VerifyAxisTeachingMove(axis, targetPosition, scope.TargetName, context, executionMode)
                    : service.VerifyAxisMove(axis, targetPosition, context, skipSharedRailXRule, executionMode);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Check");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", axis.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Blocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. axis=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", axis != null ? axis.Name : "Axis", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Failed");
                return false;
            }
        }

        public static bool IsPickerYPairInitializeHomeActive(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            lock (Sync)
            {
                PickerYPairLimitSearchScope scope = _pickerYPairLimitSearchScope;
                return scope != null &&
                       scope.IsHomeActive &&
                       ReferenceEquals(scope.FrontPickerY, frontPickerY) &&
                       ReferenceEquals(scope.RearPickerY, rearPickerY);
            }
        }

        public static bool BeginPickerYPairInitializeHome(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            lock (Sync)
            {
                PickerYPairLimitSearchScope scope = _pickerYPairLimitSearchScope;
                if (scope == null ||
                    !ReferenceEquals(scope.FrontPickerY, frontPickerY) ||
                    !ReferenceEquals(scope.RearPickerY, rearPickerY))
                {
                    return false;
                }

                scope.IsHomeActive = true;
                return true;
            }
        }

        private static bool VerifyAxisJog(
            BaseAxis axis,
            double targetPosition,
            string targetName,
            MotionGuardMoveKind moveKind,
            bool skipSharedRailXRule,
            out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardExecutionMode executionMode = ResolveExecutionMode();
                MotionGuardResult result = moveKind == MotionGuardMoveKind.AxisStepJog
                    ? service.VerifyAxisStepJog(axis, targetPosition, targetName, context, skipSharedRailXRule, executionMode)
                    : service.VerifyAxisContinuousJog(axis, targetPosition, targetName, context, skipSharedRailXRule, executionMode);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - JogCheck");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", axis.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - JogBlocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. axis jog=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", axis != null ? axis.Name : "Axis", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - JogFailed");
                return false;
            }
        }

        public static IDisposable BeginAxisTeachingMove(BaseAxis axis, double targetPosition, string targetName)
        {
            AxisMoveScope previous = CurrentAxisMoveScope.Value;
            CurrentAxisMoveScope.Value = new AxisMoveScope(axis, targetPosition, targetName, previous);
            return new AxisMoveScopeToken(previous);
        }

        public static bool VerifyAxisTeachingMove(BaseAxis axis, double targetPosition, string targetName, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardResult result = service.VerifyAxisTeachingMove(
                    axis,
                    targetPosition,
                    targetName,
                    context,
                    ResolveExecutionMode());
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - TeachingCheck");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", axis.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - TeachingBlocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. axis teaching=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", axis != null ? axis.Name : "Axis", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - TeachingFailed");
                return false;
            }
        }

        /// <summary>
        /// 실제 이동을 발행하지 않고(부작용 없음) 지정한 Teaching 이동이 지금 MotionGuard 전체 판정을 통과하는지 확인한다.
        /// 대기 폴링과 실제 이동이 동일한 인터락 규칙(PickerZone·SharedRailX 포함)을 공유하도록 하기 위한 dry-run 판정이다.
        /// 실제 이동 경로와 달리 알람을 발생시키지 않고 Blocked 로그도 남기지 않는다.
        /// </summary>
        public static bool CanAxisTeachingMove(BaseAxis axis, double targetPosition, string targetName, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                if (IsAxisAlreadyAtTarget(axis, targetPosition))
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardResult result = service.VerifyAxisTeachingMove(
                    axis,
                    targetPosition,
                    targetName,
                    context,
                    ResolveExecutionMode());
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                return result.Allowed;
            }
            catch (Exception ex)
            {
                reason = "Motion guard dry-run 예외. axis=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                return false;
            }
            finally
            {
            }
        }

        public static bool VerifyAxisHome(BaseAxis axis, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                double target = axis.Setup != null ? axis.Setup.HomeOffset : 0.0;
                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardResult result = service.VerifyAxisHome(axis, target, context);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - HomeCheck");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", axis.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - HomeBlocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. axis home=" + (axis != null ? axis.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", axis != null ? axis.Name : "Axis", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - HomeFailed");
                return false;
            }
        }

        public static bool VerifyCylinderMove(QMC.Common.IO.BaseCylinder cylinder, bool moveFwd, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || cylinder == null)
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                CylinderMoveScope scope = CurrentCylinderMoveScope.Value;
                MotionGuardResult result = IsMatchingScope(scope, cylinder, moveFwd)
                    ? service.VerifyCylinderInitialize(cylinder, moveFwd, context)
                    : service.VerifyCylinderMove(cylinder, moveFwd, context);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Check");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK", cylinder.Name, reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Blocked");
                return false;
            }
            catch (Exception ex)
            {
                reason = "Motion guard exception. cylinder=" + (cylinder != null ? cylinder.Name : "") + ", error=" + ex.Message;
                AlarmManager.Raise(AlarmSeverity.Error, "INTERLOCK-GUARD", cylinder != null ? cylinder.Name : "Cylinder", reason);
                Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Failed");
                return false;
            }
        }

        public static IDisposable BeginCylinderInitializeMove(QMC.Common.IO.BaseCylinder cylinder, bool moveFwd, string targetName)
        {
            CylinderMoveScope previous = CurrentCylinderMoveScope.Value;
            CurrentCylinderMoveScope.Value = new CylinderMoveScope(cylinder, moveFwd, targetName, previous);
            return new CylinderMoveScopeToken(previous);
        }

        public static IDisposable BeginManualSequenceProcessMove(string reason)
        {
            ExecutionModeScope previous = CurrentExecutionModeScope.Value;
            CurrentExecutionModeScope.Value = new ExecutionModeScope(
                MotionGuardExecutionMode.ManualSequenceProcess,
                reason,
                previous);
            return new ExecutionModeScopeToken(previous);
        }

        public static IDisposable BeginAutoSequenceProcessMove(string reason)
        {
            ExecutionModeScope previous = CurrentExecutionModeScope.Value;
            CurrentExecutionModeScope.Value = new ExecutionModeScope(
                MotionGuardExecutionMode.AutoSequenceProcess,
                reason,
                previous);
            return new ExecutionModeScopeToken(previous);
        }

        public static IDisposable BeginSequenceProcessMove(bool autoMode, string reason)
        {
            return autoMode
                ? BeginAutoSequenceProcessMove(reason)
                : BeginManualSequenceProcessMove(reason);
        }

        public static IDisposable BeginPickerYPairHome(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            if (frontPickerY == null)
                throw new ArgumentNullException("frontPickerY");
            if (rearPickerY == null)
                throw new ArgumentNullException("rearPickerY");
            if (ReferenceEquals(frontPickerY, rearPickerY))
                throw new ArgumentException("PickerY PairHome requires two distinct axes.");

            PickerYPairHomeScope previous = CurrentPickerYPairHomeScope.Value;
            CurrentPickerYPairHomeScope.Value = new PickerYPairHomeScope(
                frontPickerY,
                rearPickerY,
                previous);
            return new PickerYPairHomeScopeToken(previous);
        }

        public static IDisposable BeginPickerYPairLimitSearch(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            if (frontPickerY == null)
                throw new ArgumentNullException("frontPickerY");
            if (rearPickerY == null)
                throw new ArgumentNullException("rearPickerY");
            if (ReferenceEquals(frontPickerY, rearPickerY))
                throw new ArgumentException("PickerY Pair limit search requires two distinct axes.");

            var scope = new PickerYPairLimitSearchScope(frontPickerY, rearPickerY);
            lock (Sync)
            {
                if (_pickerYPairLimitSearchScope != null)
                    throw new InvalidOperationException("PickerY Pair limit search scope is already active.");
                _pickerYPairLimitSearchScope = scope;
            }
            return new PickerYPairLimitSearchScopeToken(scope);
        }

        public static bool IsPickerYPairLimitSearchActive(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            lock (Sync)
            {
                PickerYPairLimitSearchScope scope = _pickerYPairLimitSearchScope;
                return scope != null &&
                       ReferenceEquals(scope.FrontPickerY, frontPickerY) &&
                       ReferenceEquals(scope.RearPickerY, rearPickerY);
            }
        }

        public static bool IsPickerYPairHomeActive(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            PickerYPairHomeScope scope = CurrentPickerYPairHomeScope.Value;
            return scope != null &&
                   ReferenceEquals(scope.FrontPickerY, frontPickerY) &&
                   ReferenceEquals(scope.RearPickerY, rearPickerY);
        }

        public static bool IsPickerYPairHomeAuthorized(BaseAxis frontPickerY, BaseAxis rearPickerY)
        {
            PickerYPairHomeScope scope = CurrentPickerYPairHomeScope.Value;
            return scope != null &&
                   scope.IsAuthorized &&
                   ReferenceEquals(scope.FrontPickerY, frontPickerY) &&
                   ReferenceEquals(scope.RearPickerY, rearPickerY);
        }

        public static bool AuthorizePickerYPairHome(
            BaseAxis frontPickerY,
            BaseAxis rearPickerY,
            out string reason)
        {
            reason = string.Empty;
            PickerYPairHomeScope scope = CurrentPickerYPairHomeScope.Value;
            if (scope == null ||
                !ReferenceEquals(scope.FrontPickerY, frontPickerY) ||
                !ReferenceEquals(scope.RearPickerY, rearPickerY))
            {
                reason = "PickerY PairHome 실행 범위와 대상 축 쌍이 일치하지 않습니다.";
                return false;
            }

            if (!VerifyAxisHome(frontPickerY, out reason))
                return false;
            if (!VerifyAxisHome(rearPickerY, out reason))
                return false;

            scope.IsAuthorized = true;
            return true;
        }

        public static void Reload()
        {
            lock (Sync)
                _service = new MotionGuardService(InterlockCheckMatrixStore.LoadOrDefault());
        }

        private static MotionGuardService GetService()
        {
            lock (Sync)
            {
                if (_service == null)
                    _service = new MotionGuardService(InterlockCheckMatrixStore.LoadOrDefault());
                return _service;
            }
        }

        private static bool IsAxisAlreadyAtTarget(BaseAxis axis, double targetPosition)
        {
            try
            {
                if (axis == null || axis.IsMoving || axis.IsAlarm)
                    return false;

                double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                    ? axis.Config.InPositionTolerance
                    : 0.01;

                return Math.Abs(axis.ActualPosition - targetPosition) <= tolerance;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsMatchingScope(AxisMoveScope scope, BaseAxis axis, double targetPosition)
        {
            if (scope == null || axis == null || !object.ReferenceEquals(scope.Axis, axis))
                return false;

            return Math.Abs(scope.TargetPosition - targetPosition) <= 0.0001;
        }

        private static bool IsMatchingScope(CylinderMoveScope scope, QMC.Common.IO.BaseCylinder cylinder, bool moveFwd)
        {
            if (scope == null || cylinder == null || !object.ReferenceEquals(scope.Cylinder, cylinder))
                return false;

            return scope.MoveFwd == moveFwd;
        }

        private static MotionGuardExecutionMode ResolveExecutionMode()
        {
            ExecutionModeScope scope = CurrentExecutionModeScope.Value;
            return scope != null ? scope.Mode : MotionGuardExecutionMode.Default;
        }

        private sealed class AxisMoveScope
        {
            public AxisMoveScope(BaseAxis axis, double targetPosition, string targetName, AxisMoveScope previous)
            {
                Axis = axis;
                TargetPosition = targetPosition;
                TargetName = targetName ?? string.Empty;
                Previous = previous;
            }

            public BaseAxis Axis { get; private set; }
            public double TargetPosition { get; private set; }
            public string TargetName { get; private set; }
            public AxisMoveScope Previous { get; private set; }
        }

        private sealed class AxisMoveScopeToken : IDisposable
        {
            private readonly AxisMoveScope _previous;
            private bool _disposed;

            public AxisMoveScopeToken(AxisMoveScope previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                CurrentAxisMoveScope.Value = _previous;
                _disposed = true;
            }
        }

        private sealed class PickerYPairLimitSearchScope
        {
            public PickerYPairLimitSearchScope(BaseAxis frontPickerY, BaseAxis rearPickerY)
            {
                FrontPickerY = frontPickerY;
                RearPickerY = rearPickerY;
            }

            public BaseAxis FrontPickerY { get; private set; }
            public BaseAxis RearPickerY { get; private set; }
            public bool IsHomeActive { get; set; }
        }

        private sealed class PickerYPairLimitSearchScopeToken : IDisposable
        {
            private readonly PickerYPairLimitSearchScope _scope;
            private bool _disposed;

            public PickerYPairLimitSearchScopeToken(PickerYPairLimitSearchScope scope)
            {
                _scope = scope;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                lock (Sync)
                {
                    if (ReferenceEquals(_pickerYPairLimitSearchScope, _scope))
                        _pickerYPairLimitSearchScope = null;
                }
                _disposed = true;
            }
        }

        private sealed class CylinderMoveScope
        {
            public CylinderMoveScope(QMC.Common.IO.BaseCylinder cylinder, bool moveFwd, string targetName, CylinderMoveScope previous)
            {
                Cylinder = cylinder;
                MoveFwd = moveFwd;
                TargetName = targetName ?? string.Empty;
                Previous = previous;
            }

            public QMC.Common.IO.BaseCylinder Cylinder { get; private set; }
            public bool MoveFwd { get; private set; }
            public string TargetName { get; private set; }
            public CylinderMoveScope Previous { get; private set; }
        }

        private sealed class CylinderMoveScopeToken : IDisposable
        {
            private readonly CylinderMoveScope _previous;
            private bool _disposed;

            public CylinderMoveScopeToken(CylinderMoveScope previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                CurrentCylinderMoveScope.Value = _previous;
                _disposed = true;
            }
        }

        private sealed class ExecutionModeScope
        {
            public ExecutionModeScope(
                MotionGuardExecutionMode mode,
                string reason,
                ExecutionModeScope previous)
            {
                Mode = mode;
                Reason = reason ?? string.Empty;
                Previous = previous;
            }

            public MotionGuardExecutionMode Mode { get; private set; }
            public string Reason { get; private set; }
            public ExecutionModeScope Previous { get; private set; }
        }

        private sealed class ExecutionModeScopeToken : IDisposable
        {
            private readonly ExecutionModeScope _previous;
            private bool _disposed;

            public ExecutionModeScopeToken(ExecutionModeScope previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                CurrentExecutionModeScope.Value = _previous;
                _disposed = true;
            }
        }

        private sealed class PickerYPairHomeScope
        {
            public PickerYPairHomeScope(
                BaseAxis frontPickerY,
                BaseAxis rearPickerY,
                PickerYPairHomeScope previous)
            {
                FrontPickerY = frontPickerY;
                RearPickerY = rearPickerY;
                Previous = previous;
            }

            public BaseAxis FrontPickerY { get; private set; }
            public BaseAxis RearPickerY { get; private set; }
            public PickerYPairHomeScope Previous { get; private set; }
            public bool IsAuthorized { get; set; }
        }

        private sealed class PickerYPairHomeScopeToken : IDisposable
        {
            private readonly PickerYPairHomeScope _previous;
            private bool _disposed;

            public PickerYPairHomeScopeToken(PickerYPairHomeScope previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                CurrentPickerYPairHomeScope.Value = _previous;
                _disposed = true;
            }
        }
    }
}
