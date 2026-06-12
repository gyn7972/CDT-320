namespace QMC.Vision.Optics.LFine
{
    /// <summary>
    /// LFine 컨트롤러 실 하드웨어 모드 (SM 명령 0~3) — 전면 패널은 표시만, 설정은 @SM0000;{0~3} 시리얼 명령.
    /// 런타임 송신 전용(영속 config 아님). 현재모드 read 명령 없음(매뉴얼) — 마지막 송신값만 추적 가능.
    /// </summary>
    public enum LFineHardwareMode
    {
        /// <summary>모드 0 — 페이지 순차: 트리거마다 페이지 데이터로 발사(순차 진행).</summary>
        PageTrigger = 0,
        /// <summary>모드 1 — 임의(유저 시퀀스) 순서로 페이지 발사.</summary>
        UserSequence = 1,
        /// <summary>모드 2 — 채널 개별 트리거(page 0 만 사용).</summary>
        ChannelTrigger = 2,
        /// <summary>모드 3 — 소프트웨어 트리거(ST 명령으로 발사).</summary>
        SoftwareTrigger = 3,
    }
}
