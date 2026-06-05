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

    /// <summary>시퀀스 시작 지점을 결정하는 모드를 나타냅니다.</summary>
    public enum SequenceStartMode
    {
        /// <summary>저장된 재개 지점을 무시하고 항상 처음(InitialStep)부터 시작합니다.</summary>
        Restart = 0,

        /// <summary>알람/사이클 정지로 보존된 현재 스텝부터 이어서 시작합니다.</summary>
        Resume = 1
    }
}
