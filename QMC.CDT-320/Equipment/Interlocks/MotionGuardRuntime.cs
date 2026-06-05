using System;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public static class MotionGuardRuntime
    {
        private static readonly object Sync = new object();
        private static MotionGuardService _service;

        public static Func<MotionGuardContext> ContextProvider { get; set; }
        public static bool Enabled { get; set; } = true;

        public static bool VerifyAxisMove(BaseAxis axis, double targetPosition, out string reason)
        {
            reason = "";
            try
            {
                if (!Enabled || axis == null)
                    return true;

                MotionGuardService service = GetService();
                MotionGuardContext context = ContextProvider != null ? ContextProvider() : null;
                MotionGuardResult result = service.VerifyAxisMove(axis, targetPosition, context);
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
                MotionGuardResult result = service.VerifyCylinderMove(cylinder, moveFwd, context);
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
    }
}
