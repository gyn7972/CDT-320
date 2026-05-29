using QMC.Vision.Optics.LFine;
using QMC.Vision.Optics.Sim;

namespace QMC.Vision.Optics
{
    /// <summary>
    /// 조명 컨트롤러 생성 (Stage 67, BaseAxis 패턴).
    /// useSim=true → SimLightController, false → LFineLightController.
    /// LFine 의 ConnectAsync 실패 시 호출자에 false 반환 — Sim 자동 fallback 은 하지 않음
    /// (가짜 통과 방지, Stage 66 §8). 호출자가 명시적으로 Sim 을 선택해야 함.
    /// </summary>
    public static class LightControllerFactory
    {
        public static ILightController Create(LFineLightConfig cfg, bool useSim)
        {
            if (useSim || cfg == null)
                return new SimLightController(cfg?.ChannelCount ?? 8);
            return new LFineLightController(cfg);
        }
    }
}
