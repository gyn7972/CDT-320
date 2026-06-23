using QMC.Common.Alarms;
using QMC.Common.Recipes;
using QMC.Vision.Optics.LFine;
using QMC.Vision.Optics.Leesos;
using QMC.Vision.Optics.Sim;

namespace QMC.Vision.Optics
{
    /// <summary>
    /// 조명 컨트롤러 생성 (Stage 67, BaseAxis 패턴 / Stage 77 벤더 분기).
    /// useSim=true → SimLightController. 실장비는 entry.Vendor 로 분기 (LFine / Leesos).
    /// ConnectAsync 실패 시 호출자에 false 반환 — Sim 자동 fallback 은 하지 않음
    /// (가짜 통과 방지, Stage 66 §8). 호출자가 명시적으로 Sim 을 선택해야 함.
    /// </summary>
    public static class LightControllerFactory
    {
        /// <summary>Stage 77 — Setup 의 LightControllerEntry 로 벤더별 컨트롤러 생성.</summary>
        public static ILightController Create(LightControllerEntry entry, bool useSim)
        {
            if (useSim || entry == null)
                return new SimLightController(entry?.ChannelCount ?? 8);

            string vendor = string.IsNullOrEmpty(entry.Vendor) ? "LFine" : entry.Vendor;
            switch (vendor)
            {
                case "LFine":
                    return new LFineLightController(ToLFineConfig(entry));
                case "Leesos":
                    return new LeesosLightController(ToLeesosConfig(entry));
                default:
                    AlarmManager.Raise(AlarmSeverity.Error, "LIGHT-MAP-INVALID", "LightControllerFactory",
                        $"알 수 없는 Vendor '{entry.Vendor}' — Sim 으로 대체");
                    return new SimLightController(entry.ChannelCount);
            }
        }

        /// <summary>레거시 호환 — LFineLightConfig 직접 (Stage 67 시그니처 유지).</summary>
        public static ILightController Create(LFineLightConfig cfg, bool useSim)
        {
            if (useSim || cfg == null)
                return new SimLightController(cfg?.ChannelCount ?? 8);
            return new LFineLightController(cfg);
        }

        // ── 어댑터: LightControllerEntry → 벤더 Config ──
        public static LFineLightConfig ToLFineConfig(LightControllerEntry e)
            => new LFineLightConfig
            {
                PortName     = e.PortName,
                BaudRate     = e.BaudRate,
                MaxPower     = e.MaxPower,
                MaxOnTimeUs  = e.MaxOnTimeUs,
                ChannelCount = e.ChannelCount
                // DataBits/StopBits/Parity/Handshake/TimeoutMs 는 LFineLightConfig 기본값 사용
            };

        public static LeesosLightConfig ToLeesosConfig(LightControllerEntry e)
            => new LeesosLightConfig
            {
                PortName     = e.PortName,
                BaudRate     = e.BaudRate,
                MaxPower     = e.MaxPower,   // Leesos 는 0~255 — UI 가 Vendor=Leesos 시 255 제안
                ChannelCount = e.ChannelCount
                // DataBits/StopBits/Parity/Handshake/TimeoutMs 는 LeesosLightConfig 기본값(8N1/None/1000ms)
            };
    }
}
