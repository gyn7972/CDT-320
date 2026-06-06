/*
 * Purpose
 *     Ajin common library
 * 
 * Revision
 *     1. Created: 2009/04/30 
 * 
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;


namespace QMC.Common.Motion.Ajin
{
    internal static class AXL
    {
        #region Define
        public const string LibraryFileName = "AXL.dll";
        #endregion

        #region Dll Imports
        //========== 입력 인자 사용시 주의 사항. =======================================================================
        // lNodeNum   : CPU 모듈의 ID 설정 로터리 스위치의 값을 의미 합니다.(0x00 이상,  0xF9 이하)
        //==============================================================================================================

        // 라이브러리 초기화
        [DllImport(LibraryFileName)]
        private static extern uint AxlOpen(int lIrqNo);
        // 라이브러리 초기화시 하드웨어 칩에 리셋을 하지 않음.
        [DllImport(LibraryFileName)]
        private static extern uint AxlOpenNoReset(uint lIrqNo);
        // 라이브러리 사용을 종료
        [DllImport(LibraryFileName)]
        private static extern int AxlClose();
        // 라이브러리가 초기화 되어 있는 지 확인
        [DllImport(LibraryFileName)]
        private static extern int AxlIsOpened();

        // 인터럽트를 사용한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxlInterruptEnable();
        // 인터럽트를 사용안한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxlInterruptDisable();

        //========== 라이브러리 및 베이스 보드 정보 =================================================================================

        // 등록된 베이스 보드의 개수 확인
        [DllImport(LibraryFileName)]
        private static extern uint AxlGetBoardCount(ref int lpBoardCount);
        // 라이브러리 버전 확인
        [DllImport(LibraryFileName)]
        private static extern uint AxlGetLibVersion(ref char szVersion);

        //========= 로그 레벨 =================================================================================

        // EzSpy에 출력할 메시지 레벨 설정
        // uLevel : 0 - 3 설정
        // LEVEL_NONE(0)    : 모든 메시지를 출력하지 않는다.
        // LEVEL_ERROR(1)   : 에러가 발생한 메시지만 출력한다.
        // LEVEL_RUNSTOP(2) : 모션에서 Run / Stop 관련 메시지를 출력한다.
        // LEVEL_FUNCTION(3): 모든 메시지를 출력한다.
        [DllImport(LibraryFileName)]
        private static extern uint AxlSetLogLevel(uint uLevel);
        // EzSpy에 출력할 메시지 레벨 확인
        [DllImport(LibraryFileName)]
        private static extern uint AxlGetLogLevel(ref uint upLevel);
        #endregion

        #region Field
        private const string LogServiceName = "Ajin.AXL";

        private static object m_SyncRoot;
        private static bool m_InterruptEnabled;
        #endregion

        #region Constructor
        static AXL()
        {
            AXL.m_SyncRoot = new object();
        }
        #endregion

        #region Property
        public static bool InterruptEnabled
        {
            get { return AXL.m_InterruptEnabled; }
            private set { AXL.m_InterruptEnabled = value; }
        }
        #endregion

        #region Method
        #region 라이브러리 초기화
        public static int Open(int irqNo = 7)
        {
            int ret = 0;

            lock (AXL.m_SyncRoot) 
            {
                if (AXL.IsOpened() == false)
                {
                    //if ((ret = AXL.CheckErrorCode("AXL.AxlOpen", AXL.AxlOpen(irqNo))) != 0) return ret;
                    if ((ret = AXL.CheckErrorCode("AXL.AxlOpen", AXL.AxlOpenNoReset((uint)irqNo))) != 0) return ret;
                }
                // 전체 라이브러리에서 interrupt 사용을 설정한다.
                if ((ret = AXL.InterruptEnable()) != 0) return ret;
            }

            return ret;
        }

        public static bool Close()
        {
            lock (AXL.m_SyncRoot)
            {
                if (AXL.IsOpened() == true)
                    return AXL.AxlClose() != 0;
            }

            return true;
        }

        public static bool IsOpened()
        {
            bool opened = true;

            lock (AXL.m_SyncRoot)
            {
                opened = AXL.AxlIsOpened() != 0;
            }

            return opened;
        }
        #endregion

        #region 인터럽트 설정
        public static int InterruptEnable()
        {
            int ret = 0;

            lock (AXL.m_SyncRoot)
            {
                if (AXL.IsOpened() == true && AXL.InterruptEnabled == false)
                {
                    if ((ret = AXL.CheckErrorCode("AXL.AxlInterruptEnable", AXL.AxlInterruptEnable())) != 0) return ret;
                    AXL.InterruptEnabled = true;
                }
            }

            return ret;
        }

        public static int InterruptDisable()
        {
            int ret = 0;

            lock (AXL.m_SyncRoot)
            {
                if (AXL.IsOpened() == true && AXL.InterruptEnabled == true)
                {
                    if ((ret = AXL.CheckErrorCode("AXL.AxlInterruptDisable", AXL.AxlInterruptDisable())) != 0) return ret;
                    AXL.InterruptEnabled = false;
                }
            }

            return ret;
        }
        #endregion

        #region misc functions
        //public static void WriteLog(LogEntry entry)
        //{
        //    Log.Write(AXL.LogServiceName, entry);
        //}
        //public static void WriteLog(LogLevel level, string message)
        //{
        //    AXL.WriteLog(new LogEntry(level, message));
        //}

        /// <summary>
        /// dll 함수에서  return된 값을 확인하여 실패여부를 확인한다
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static int CheckErrorCode(string function, uint code)
        {
            int ret = 0;
            //Error error = null;

            if (0 == code) return ret;
            
            if (Enum.IsDefined(typeof(AXT_FUNC_RESULT), code) == true)
            {
                ret = (int)code;
            }
            

            return ret;
        }
        #endregion
        #endregion
    }
}
