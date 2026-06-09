namespace QMC.Vision.Core.Parameters
{
    /// <summary>
    /// P2 — 전역 ParameterStore 접근점. 부팅 시 상위 레이어(Config\ParameterStoreBootstrap)가 Current 설정.
    /// finder/inspector 의 Save/Load 래퍼가 Current?.SaveTarget/LoadTarget 위임에 사용(null 안전).
    /// </summary>
    public static class ParameterStoreHost
    {
        public static ParameterStore Current { get; set; }
    }
}
