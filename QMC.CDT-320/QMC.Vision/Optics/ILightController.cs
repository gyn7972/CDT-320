using System;
using System.Threading.Tasks;

namespace QMC.Vision.Optics
{
    /// <summary>
    /// 조명 컨트롤러 추상화 (Stage 67).
    /// 실장비(<see cref="LFine.LFineLightController"/>) / Sim(<see cref="Sim.SimLightController"/>) 공통 인터페이스.
    /// 모든 명령은 비동기 + fire-and-forget (LFine 은 무응답 — Stage 66 #4 확정).
    /// 채널 번호는 1-기반 (장비 프로토콜과 동일).
    /// </summary>
    public interface ILightController : IDisposable
    {
        /// <summary>시리얼 Open 성공 여부 (Sim 은 항상 true).</summary>
        bool   IsConnected  { get; }
        /// <summary>연결된 시리얼 포트명 (Sim 은 "Sim").</summary>
        string PortName     { get; }
        /// <summary>이 컨트롤러가 관리하는 채널 수.</summary>
        int    ChannelCount { get; }

        /// <summary>시리얼 Open. 실패 시 false + LIGHT-OPEN-FAIL 알람.</summary>
        Task<bool> ConnectAsync();
        /// <summary>시리얼 Close.</summary>
        Task       DisconnectAsync();

        /// <summary>채널 밝기 설정 (0 ~ MaxPower). 범위 밖이면 false + LIGHT-PWR-RANGE.</summary>
        Task<bool> SetPowerAsync(int channel, int power);
        /// <summary>채널 Strobe On-Time 설정 (0 ~ MaxOnTimeUs).</summary>
        Task<bool> SetStrobeTimeAsync(int channel, int onTimeUs);
        /// <summary>채널 On/Off. OFF = Power 0 전송, ON = 직전 저장 Power 복원 (Stage 66 #3 확정).</summary>
        Task<bool> SetOnOffAsync(int channel, bool on);
        /// <summary>채널의 마지막 설정 Power (캐시). 무응답 프로토콜이라 device read 없음.</summary>
        Task<int>  GetPowerAsync(int channel);
        /// <summary>컨트롤러 헬스 — 연결 + 채널 유효성. 무응답이라 IsConnected 기반 판정.</summary>
        Task<bool> CheckPowerOnAsync(int channel);
    }
}
