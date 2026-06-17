using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Common.Motion.Ajin;
using System;
using System.IO;

namespace QMC.CDT320.Ajin
{
    /// <summary>
    /// AXL ?쇱씠釉뚮윭由??꾩뿭 ?섎챸二쇨린 愿由? ???쒖옉 ??Open, 醫낅즺 ??Close.
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
                    EventLogger.Write(EventKind.Alarm, "SYS", "AXL-OPEN", "AjinSystem", LastError);
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
                        EventLogger.Write(EventKind.Alarm, "SYS", "AXM-MOT-LOAD", "AjinSystem", LastError);
                        AlarmManager.Raise(AlarmSeverity.Critical, "AXM-MOT-LOAD", "AjinSystem", LastError);
                        
                        //Test?좊븣???곗꽑 ?섏뼱媛?? I/O留??뺤씤?섎뒗嫄몃줈.
                        //IsOpen = false;
                        // ?뚮씪誘명꽣 濡쒕뱶 ?ㅽ뙣?대룄 AXL? ?대젮 ?덉쑝誘濡? ?꾩슂 ??Close
                        //AXL.Close();
                        //return false;
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
                EventLogger.Write(EventKind.Alarm, "SYS", "AXL-DLL", "AjinSystem", LastError);
                AlarmManager.Raise(AlarmSeverity.Critical, "AXL-DLL", "AjinSystem", LastError);
                return false;
            }
            catch (Exception ex)
            {
                LastError = "AXL open exception: " + ex.Message;
                EventLogger.Write(EventKind.Alarm, "SYS", "AXL-OPEN", "AjinSystem", LastError);
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
                // AXL 네트워크 에러
                case 1152: return "AXT_RT_NETWORK_ERROR";
                // AXL 네트워크 Lock 불일치
                case 1153: return "AXT_RT_NETWORK_LOCK_MISMATCH";
                // 모션 모듈 미장착
                case 4051: return "AXT_RT_MOTION_NOT_MODULE";
                // 초기 축 번호 오류
                case 4053: return "AXT_RT_MOTION_NOT_INITIAL_AXIS_NO";
                // 모션 파라미터 읽기 실패
                case 4055: return "AXT_RT_MOTION_NOT_PARA_READ";
                // 모션 파라미터 파일 로드 실패
                case 4111: return "AXT_RT_MOTION_INVALID_FILE_LOAD";
                default: return "AXL/AXM error";
            }
        }
    }
}

