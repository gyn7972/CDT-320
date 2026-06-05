using System;
using System.Threading.Tasks;
using QMC.Common.Recipes;

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

        /// <summary>Stage 79 — 동작 모드. 캐시 skip 정책 결정 (StrobeOnCommand 면 무조건 송신).</summary>
        LightControllerMode Mode { get; }

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

        /// <summary>Stage 69 — 페이지 전환 (LFine 페이지 모델). PageCount==1 이면 no-op(true).</summary>
        Task<bool> SwitchPageAsync(int page);

        /// <summary>Stage 79 — 한 페이지/배치의 채널 값 일괄 적용 (valuesPerChannel.Length == ChannelCount, 인덱스 0 = 채널 1).
        /// <para>Continuous/StrobeExternal: 직전과 동일하면 skip(no-op+true). StrobeOnCommand: 무조건 송신.</para>
        /// LFine = SP 1프레임. Leesos = 전체동일값 LCT 1프레임 / 그 외 LC loop(변경분만). Sim = 캐시 갱신.</summary>
        Task<bool> SetChannelBatchAsync(int page, int[] valuesPerChannel);

        /// <summary>Stage 75 — 응답 1프레임 수신 (적용 후 검증/디버그용).
        /// LFine 은 시리얼에서 Stx..Etx 프레임을 읽어 payload 문자열 반환(무응답/타임아웃이면 null).
        /// Sim 은 합성 ACK 반환. <paramref name="timeoutMs"/>=0 이면 컨트롤러 기본 timeout 사용.</summary>
        Task<string> ReceiveResponseAsync(int timeoutMs = 0);
    }
}
