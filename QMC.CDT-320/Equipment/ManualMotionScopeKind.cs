namespace QMC.CDT320
{
    /// <summary>
    /// 작업자가 수동으로 실행하는 동작의 속도/인터락 적용 범위를 구분합니다.
    /// </summary>
    public enum ManualMotionScopeKind
    {
        /// <summary>
        /// 속도만 수동 안전 비율로 제한합니다.
        /// Lifter Prev/Next 같은 단순 수동 모션에 사용합니다.
        /// </summary>
        SpeedOnly,

        /// <summary>
        /// 작업 화면 READY 시퀀스 전용 속도 비율로 제한합니다.
        /// </summary>
        ReadySequence,

        /// <summary>
        /// 속도 제한과 공정 시컨스 인터락을 함께 적용합니다.
        /// Auto 공정 흐름을 수동으로 한 스텝 실행하는 Load/Unload/Pick/Place 등에 사용합니다.
        /// </summary>
        ProcessSequence
    }
}
