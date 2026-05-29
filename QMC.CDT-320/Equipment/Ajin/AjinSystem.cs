using QMC.CDT320.Alarms;
using QMC.CDT320.Logging;
using QMC.Common.Motion.Ajin;
using System;
using System.IO;

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
            if (IsOpen) 
                return true;

            try
            {


                int r = AXL.Open(irqNo);
                if (r != 0)
                {
                    LastErrorCode = r;
                    LastError     = "AxlOpen failed 0x" + r.ToString("X4");
                    EventLogger.Write(EventKind.Alarm, "SYS", "AXL-OPEN", LastError);
                    AlarmManager.Raise(AlarmSeverity.Critical, "AXL-OPEN", "AjinSystem", LastError);
                    return false;
                }

                string motPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Motor","CDT-320.mot");
                if (!string.IsNullOrEmpty(motPath) && File.Exists(motPath))
                {
                    r = (int)AXM.AxmMotLoadParaAll(motPath);
                    if (r != 0)
                    {
                        LastErrorCode = r;
                        LastError = "AxmMotLoadParaAll failed. Code=" + r + " (" + DescribeAxlError(r) + "), Path=" + motPath;
                        EventLogger.Write(EventKind.Alarm, "SYS", "AXM-MOT-LOAD", LastError);
                        AlarmManager.Raise(AlarmSeverity.Critical, "AXM-MOT-LOAD", "AjinSystem", LastError);
                        IsOpen = false;
                        // 파라미터 로드 실패해도 AXL은 열려 있으므로, 필요 시 Close
                        AXL.Close();
                        return false;
                    }
                    else
                    {
                        IsOpen = true;
                    }
                }

                int n = 0;
                if (AXM.GetAxisCount(out n) == 0) 
                    AxisCount = n;

                n = 0;
                if (AXD.GetModuleCount(out n) == 0) 
                    DioModuleCount = n;

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
            try { AXL.Close(); } catch { }
            IsOpen = false;
            EventLogger.Write(EventKind.Event, "SYS", "AXL-CLOSE", "AXL closed");
        }

        private static string DescribeAxlError(int code)
        {
            switch (code)
            {
                case 1152: return "AXT_RT_NETWORK_ERROR";
                case 1153: return "AXT_RT_NETWORK_LOCK_MISMATCH";
                case 4051: return "AXT_RT_MOTION_NOT_MODULE";
                case 4053: return "AXT_RT_MOTION_NOT_INITIAL_AXIS_NO";
                case 4055: return "AXT_RT_MOTION_NOT_PARA_READ";
                case 4111: return "AXT_RT_MOTION_INVALID_FILE_LOAD";
                default: return "AXL/AXM error";
            }
        }
    }
}
