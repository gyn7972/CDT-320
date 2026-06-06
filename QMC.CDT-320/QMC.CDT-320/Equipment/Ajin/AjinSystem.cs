using System;
using QMC.Common.Alarms;
using QMC.CDT320.Logging;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// AXL 라이브러리 전역 수명주기 관리. 앱 시작 시 Open, 종료 시 Close.
    /// </summary>
    public static class AjinSystem
    {
        public static bool IsOpen         { get; private set; }
        public static int  AxisCount      { get; private set; }
        public static int  DioModuleCount { get; private set; }
        public static int  LastErrorCode  { get; private set; }
        public static string LastError    { get; private set; }

        public static bool Open(int irqNo = 7)
        {
            if (IsOpen) return true;
            try
            {
                uint r = Axl.AxlOpen(irqNo);
                if (!AxtReturn.IsSuccess(r))
                {
                    LastErrorCode = (int)r;
                    LastError     = "AxlOpen failed 0x" + r.ToString("X4");
                    EventLogger.Write(EventKind.Alarm, "SYS", "AXL-OPEN", LastError);
                    AlarmManager.Raise(AlarmSeverity.Critical, "AXL-OPEN", "AjinSystem", LastError);
                    return false;
                }

                int n = 0;
                if (AxtReturn.IsSuccess(Axl.AxmInfoGetAxisCount(ref n))) AxisCount = n;
                n = 0;
                if (AxtReturn.IsSuccess(Axl.AxdInfoGetModuleCount(ref n))) DioModuleCount = n;

                IsOpen = true;
                EventLogger.Write(EventKind.Event, "SYS", "AXL-OPEN",
                    $"AXL opened (irq={irqNo}, axes={AxisCount}, dio={DioModuleCount})");
                return true;
            }
            catch (DllNotFoundException ex)
            {
                LastError = "AXL.dll not found: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "SYS", "AXL-DLL", LastError);
                AlarmManager.Raise(AlarmSeverity.Critical, "AXL-DLL", "AjinSystem", LastError);
                return false;
            }
            catch (Exception ex)
            {
                LastError = "AXL open exception: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "SYS", "AXL-OPEN", LastError);
                AlarmManager.Raise(AlarmSeverity.Critical, "AXL-OPEN", "AjinSystem", LastError);
                return false;
            }
        }

        public static void Close()
        {
            if (!IsOpen) return;
            try { Axl.AxlClose(); } catch { }
            IsOpen = false;
            EventLogger.Write(EventKind.Event, "SYS", "AXL-CLOSE", "AXL closed");
        }
    }
}
