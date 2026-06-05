using QMC.Common;

namespace QMC.Common.Motion
{
    // ──────────────────────────────────────────────────────────────────────────
    //  AxisSetup ? 기구적 설정값 (전원 OFF 후에도 보존)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 축의 기구적 설정값.
    /// 소프트 리밋 및 원점 오프셋은 하드웨어 교체 후 재설정 전까지 유지된다.
    /// </summary>
    public class AxisSetup : ISetupData
    {
        /// <summary>플러스 방향 소프트웨어 리밋 위치 [mm 또는 pulse]</summary>
        public double SoftLimitPlus  { get; set; } = 200.0;

        /// <summary>마이너스 방향 소프트웨어 리밋 위치 [mm 또는 pulse]</summary>
        public double SoftLimitMinus { get; set; } = -5.0;

        /// <summary>원점 센서 감지 후 적용할 오프셋 값 (원점 좌표 보정)</summary>
        public double HomeOffset     { get; set; } = 0.0;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  AxisConfig ? 고정 사양 파라미터 (설비 모델별 불변값)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 축의 고정 사양 파라미터.
    /// 실제 하드웨어 없이 시뮬레이션으로 동작할지 여부를 포함한다.
    /// </summary>
    public class AxisConfig : IConfigData
    {
        /// <summary>
        /// true이면 시뮬레이션 엔진으로 동작한다 (기본값: true).
        /// false이면 실제 모션 보드 API 호출 경로를 사용한다.
        /// </summary>
        public bool IsSimulationMode { get; set; } = true;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  AxisRecipe ? 공정별 속도/가감속 파라미터
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 축의 공정별 모션 파라미터.
    /// 이 값들은 제품/공정이 바뀔 때마다 로드·변경된다.
    /// </summary>
    public class AxisRecipe : IRecipeData
    {
        /// <summary>일반 이동 기본 속도 [단위/s]. 사용자 xlsx 기준 평균 상향 (100→1000).</summary>
        public double DefaultVelocity    { get; set; } = 1000.0;

        /// <summary>가속도 [단위/s²]. 사용자 xlsx 기준 상향 (500→10000).</summary>
        public double Acceleration       { get; set; } = 10000.0;

        /// <summary>감속도 [단위/s²]. 사용자 xlsx 기준 상향 (500→10000).</summary>
        public double Deceleration       { get; set; } = 10000.0;

        /// <summary>원점 복귀 시 사용할 속도 [단위/s]. SIM 환경 빠른 모션 위해 200 상향.</summary>
        public double HomeVelocity       { get; set; } = 200.0;

        /// <summary>Jog Coarse(빠른) 속도 [단위/s]</summary>
        public double JogCoarseVelocity  { get; set; } = 50.0;

        /// <summary>Jog Fine(미세) 속도 [단위/s]</summary>
        public double JogFineVelocity    { get; set; } = 5.0;
    }
}
