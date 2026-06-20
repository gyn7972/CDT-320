namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// 자동/수동 시퀀스 유닛의 현재 동작 상태(공식 상태 객체).<br/>
    /// 로그 문자열이 아니라 이 enum 으로 UI 표시 상태를 관리한다.
    /// </summary>
    public enum SequenceActivityState
    {
        /// <summary>대기(시작 전/유휴).</summary>
        Idle,

        /// <summary>동작 진행 중.</summary>
        Running,

        /// <summary>다른 유닛/리소스/신호 대기 중.</summary>
        Waiting,

        /// <summary>정상 완료.</summary>
        Completed,

        /// <summary>정지(Stop / Cycle Stop).</summary>
        Stopped,

        /// <summary>취소(Cancel).</summary>
        Canceled,

        /// <summary>알람/예외로 중단.</summary>
        Alarm
    }
}
