using QMC.Common;

namespace QMC.Common.IO
{
    // ──────────────────────────────────────────────────────────────────────────
    //  IoSetup ? 기구적 설정값 (모듈/비트 번호, 접점 타입)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// I/O 포인트의 기구적 설정값.
    /// 하드웨어 배선이 바뀌지 않는 한 유지되는 값들을 담는다.
    /// </summary>
    public class IoSetup : ISetupData
    {
        /// <summary>
        /// I/O 보드의 모듈 번호 (슬롯 번호).
        /// </summary>
        public int ModuleNo { get; set; } = 0;

        /// <summary>
        /// 모듈 내 비트(채널) 번호.
        /// </summary>
        public int BitNo { get; set; } = 0;

        /// <summary>
        /// B접점(Normally Closed) 여부.<br/>
        /// <list type="bullet">
        ///   <item><description>false (기본값) : A접점 ? 하드웨어 신호 ON = 논리 ON</description></item>
        ///   <item><description>true          : B접점 ? 하드웨어 신호 ON = 논리 OFF (반전)</description></item>
        /// </list>
        /// </summary>
        public bool IsNormallyClosed { get; set; } = false;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  IoConfig ? 고정 사양 파라미터
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// I/O 포인트의 고정 사양 파라미터.
    /// 실제 하드웨어 없이 시뮬레이션으로 동작할지 여부를 포함한다.
    /// </summary>
    public class IoConfig : IConfigData
    {
        /// <summary>
        /// true이면 시뮬레이션 모드로 동작한다 (기본값: true).<br/>
        /// false이면 실제 I/O 보드 API 호출 경로를 사용한다.
        /// </summary>
        public bool IsSimulationMode { get; set; } = true;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  IoRecipe ? 공정별 파라미터
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// I/O 포인트의 공정별 파라미터.
    /// </summary>
    public class IoRecipe : IRecipeData
    {
        /// <summary>
        /// 상태 도달 후 안정화를 위한 추가 대기 시간 [ms].<br/>
        /// 센서 채터링 방지에 활용한다 (기본값: 0ms).
        /// </summary>
        public int SettleTimeMs { get; set; } = 0;
    }
}
