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

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                AxisMoveScope scope = CurrentAxisMoveScope.Value;
                MotionGuardResult result = IsMatchingScope(scope, axis, targetPosition)
                    ? service.VerifyAxisTeachingMove(axis, targetPosition, scope.TargetName, context)
                    : service.VerifyAxisMove(axis, targetPosition, context, skipSharedRailXRule);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - Check");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Warning, "INTERLOCK", axis.Name, reason);
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
                MotionGuardResult result = service.VerifyAxisTeachingMove(axis, targetPosition, targetName, context);
                if (result == null)
                    return true;

                reason = result.Message ?? "";
                if (result.RequiresDetailedCheck)
                    Log.Write("Main", "INTERLOCK", "MotionGuard", reason + " - TeachingCheck");

                if (result.Allowed)
                    return true;

                AlarmManager.Raise(AlarmSeverity.Warning, "INTERLOCK", axis.Name, reason);
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

                AlarmManager.Raise(AlarmSeverity.Warning, "INTERLOCK", axis.Name, reason);
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

                AlarmManager.Raise(AlarmSeverity.Warning, "INTERLOCK", cylinder.Name, reason);
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
    }
}
