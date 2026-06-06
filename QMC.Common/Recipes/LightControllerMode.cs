namespace QMC.Common.Recipes
{
    /// <summary>
    /// Stage 79 — 조명 컨트롤러 동작 모드. 캐시 skip 정책을 결정한다.
    /// </summary>
    public enum LightControllerMode
    {
        /// <summary>PWM dimming — 명령 후 값 유지. 같은 값 재송신은 skip 안전.</summary>
        Continuous      = 0,
        /// <summary>시간/밝기 설정 + 외부 HW 트리거가 발사. 설정 재송신 skip 안전.</summary>
        StrobeExternal  = 1,
        /// <summary>명령 송신 자체가 발사 트리거 — 캐시 skip 절대 금지(매 호출 송신).</summary>
        StrobeOnCommand = 2,
    }
}
