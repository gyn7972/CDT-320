namespace QMC.CDT320
{
    /// <summary>사이클 운영 모드 — Auto/Manual/Step.</summary>
    public enum CycleMode
    {
        /// <summary>자동 — 전체 다이를 끝까지 자동 진행 (기본).</summary>
        Auto = 0,
        /// <summary>수동 — 다이 1개 처리 후 자동 정지 (다음 다이는 다시 CYCLE RUN 필요).</summary>
        Manual = 1,
        /// <summary>스텝 — 다이 1개마다 사용자 확인 (StepRunGate 콜백).</summary>
        Step = 2,
    }
}
