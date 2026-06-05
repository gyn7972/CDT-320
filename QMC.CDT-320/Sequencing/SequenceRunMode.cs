namespace QMC.CDT320.Sequencing
{
    /// <summary>시퀀스 실행 모드를 나타냅니다.</summary>
    public enum SequenceRunMode
    {
        /// <summary>선택된 유닛을 자동으로 연속 실행합니다.</summary>
        Auto = 0,

        /// <summary>사용자 트리거마다 선택 유닛을 한 단계씩 실행합니다.</summary>
        Manual = 1,

        /// <summary>다이 또는 단계마다 게이트 확인 후 실행합니다.</summary>
        Step = 2
    }
}
