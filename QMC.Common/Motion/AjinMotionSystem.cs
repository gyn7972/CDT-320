using System;
using QMC.Common.Motion.Ajin;

namespace QMC.Common.Motion
{
    /// <summary>
    /// AJINEXTEK AXL 라이브러리의 전역 수명주기 관리자.<br/>
    /// <see cref="Ajin.AXL"/> 가 internal 이므로 동일 어셈블리(QMC.Common)에 위치시켜
    /// 외부(QMC.CDT-320 등)에서 안전하게 Open/Close 할 수 있는 진입점을 제공한다.<br/>
    /// <list type="bullet">
    ///   <item><description>앱 시작 시 <see cref="Open"/> 1회 호출.</description></item>
    ///   <item><description>실 보드/드라이버 미설치 환경에서는 <see cref="IsOpen"/> = false 로 떨어져
    ///     <see cref="AjinAxis"/> 가 자동으로 시뮬레이션 모드로 동작한다.</description></item>
    /// </list>
    /// </summary>
    public static class AjinMotionSystem
    {
        // ────────────────────────────────────────────
        //  공개 상태
        // ────────────────────────────────────────────

        /// <summary>라이브러리가 정상적으로 열렸는지 여부.</summary>
        public static bool IsOpen        { get; private set; }

        /// <summary>현재 시스템에서 인식된 모션 축 개수.</summary>
        public static int  AxisCount     { get; private set; }

        /// <summary>현재 시스템에서 인식된 DIO 모듈 개수.</summary>
        public static int  DioModuleCount { get; private set; }

        /// <summary>마지막 에러 코드 (성공 시 0).</summary>
        public static int  LastErrorCode { get; private set; }

        /// <summary>마지막 에러 메시지 (성공 시 null).</summary>
        public static string LastError   { get; private set; }

        // ────────────────────────────────────────────
        //  Open / Close
        // ────────────────────────────────────────────

        /// <summary>
        /// AXL 라이브러리를 연다.<br/>
        /// DLL 미존재/하드웨어 미연결 시 false 를 반환하고 <see cref="LastError"/> 에 사유를 남긴다.
        /// </summary>
        /// <returns>open 성공 여부</returns>
        public static bool Open(int irqNo = 7)
        {
            if (IsOpen) return true;

            try
            {
                int ret = AXL.Open(irqNo);
                if (ret != 0)
                {
                    LastErrorCode = ret;
                    LastError     = "AXL.Open failed (0x" + ret.ToString("X4") + ")";
                    return false;
                }

                int count;
                if (AXM.GetAxisCount(out count) == 0)
                    AxisCount = count;

                int dioCount;
                if (AXD.GetModuleCount(out dioCount) == 0)
                    DioModuleCount = dioCount;

                IsOpen = true;
                return true;
            }
            catch (DllNotFoundException ex)
            {
                LastError = "AXL.dll not found: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                LastError = "AXL open exception: " + ex.Message;
                return false;
            }
        }

        /// <summary>AXL 라이브러리를 닫는다.</summary>
        public static void Close()
        {
            if (!IsOpen) return;
            try { AXL.Close(); } catch { /* ignore */ }
            IsOpen = false;
            AxisCount = 0;
            DioModuleCount = 0;
        }
    }
}
