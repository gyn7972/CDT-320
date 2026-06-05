using System;
using Matrox.MatroxImagingLibrary;

namespace QMC.Vision.Cameras.Mil
{
    /// <summary>
    /// MIL Application + System(=Matrox 보드) 공유 할당 + 가용성 가드.
    /// <para>HikMvsDll 대응 — MIL 미설치 / 보드 없음 / 라이선스 없음이면 <see cref="IsAvailable"/>=false →
    /// MilCamera.Open 이 throw → CameraFactory 가 Sim 으로 fallback.</para>
    /// App/System 은 프로세스당 1회만 할당(여러 MilCamera 가 공유, 각자 digitizer 만 별도 alloc).
    /// </summary>
    internal static class MilSystem
    {
        private static readonly object _lock = new object();
        private static bool   _tried;
        private static MIL_ID _app = MIL.M_NULL;
        private static MIL_ID _sys = MIL.M_NULL;

        public static bool   IsAvailable    { get; private set; }
        public static string LastError      { get; private set; }
        public static long   SystemType     { get; private set; }
        public static int    DigitizerCount { get; private set; }
        public static MIL_ID SysId => _sys;

        private static bool IsNull(MIL_ID id) { return ((long)id) == 0; }

        /// <summary>최초 1회 MIL App/System 할당 시도. 실패해도 throw 하지 않고 IsAvailable=false 로만 표시.</summary>
        public static void EnsureInit()
        {
            if (_tried) return;
            lock (_lock)
            {
                if (_tried) return;
                _tried = true;
                try
                {
                    MIL.MappAlloc(MIL.M_NULL, MIL.M_DEFAULT, ref _app);
                    if (IsNull(_app)) { LastError = "MappAlloc returned NULL (MIL/license)"; return; }

                    // 보드/라이선스 문제 시 모달 에러 대화상자가 뜨지 않도록 억제
                    try { MIL.MappControl(_app, MIL.M_ERROR, MIL.M_PRINT_DISABLE); } catch { }

                    // 시스템(보드)은 항상 MILConfig 기본 시스템 사용 (별도 설정 불필요).
                    MIL.MsysAlloc(_app, MIL.M_SYSTEM_DEFAULT, MIL.M_DEFAULT, MIL.M_DEFAULT, ref _sys);
                    if (IsNull(_sys))
                    {
                        LastError = "MsysAlloc(M_SYSTEM_DEFAULT) returned NULL — 보드/기본시스템 없음";
                        return;
                    }

                    MIL_INT digs = 0, stype = 0;
                    try { MIL.MsysInquire(_sys, MIL.M_DIGITIZER_NUM, ref digs);  } catch { }
                    try { MIL.MsysInquire(_sys, MIL.M_SYSTEM_TYPE,   ref stype); } catch { }
                    DigitizerCount = (int)(long)digs;
                    SystemType     = (long)stype;
                    IsAvailable    = true;
                }
                catch (Exception ex)
                {
                    LastError   = ex.GetType().Name + ": " + ex.Message;
                    IsAvailable = false;
                }
            }
        }

        public static string GetInstallHint()
            => "MIL/Matrox 보드 사용 불가 (" + (LastError ?? "MIL 미설치/보드 없음/라이선스 없음") + "). Sim 으로 대체됨.";
    }
}
