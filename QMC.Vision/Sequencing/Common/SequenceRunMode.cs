namespace QMC.Vision.Sequencing
{
    /// <summary>시퀀스 실행 모드. 핸들러 QMC.CDT320.Sequencing.SequenceRunMode 미러.</summary>
    public enum SequenceRunMode
    {
        /// <summary>선택 모듈을 자동으로 연속 실행한다.</summary>
        Auto = 0,

        /// <summary>사용자 트리거마다 한 단계씩 실행한다.</summary>
        Manual = 1,

        /// <summary>사이클마다 게이트 확인 후 실행한다.</summary>
        Step = 2
    }

    /// <summary>시퀀스 시작 지점 모드.</summary>
    public enum SequenceStartMode
    {
        /// <summary>저장된 재개 지점을 무시하고 처음부터 시작한다.</summary>
        Restart = 0,

        /// <summary>보존된 현재 스텝부터 이어서 시작한다.</summary>
        Resume = 1
    }
}
