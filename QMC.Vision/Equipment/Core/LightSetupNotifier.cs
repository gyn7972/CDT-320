using System;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 조명 설정 변경 통지 — [설정&gt;조명](컨트롤러 인벤토리 저장)과 [설정&gt;카메라](모듈 조명 지정 저장)가 발화하고,
    /// 레시피 검사 조명 패널(InspectionLightPanel)이 구독해 프로그램 재시작 없이 즉시 재바인딩한다(2026-07-11).
    /// </summary>
    public static class LightSetupNotifier
    {
        public static event Action Changed;

        /// <summary>설정 저장 직후 호출. 구독자 예외가 저장 흐름을 깨지 않도록 흡수.</summary>
        public static void Notify()
        {
            try { Changed?.Invoke(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[LightSetupNotifier] 구독자 예외: " + ex.Message); }
        }
    }
}
