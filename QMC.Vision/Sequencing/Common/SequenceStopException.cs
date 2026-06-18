using System;

namespace QMC.Vision.Sequencing
{
    /// <summary>시퀀스를 정상적으로 중단시키는 신호 예외(알람과 구분). 핸들러 SequenceStopException 미러.</summary>
    public class SequenceStopException : Exception
    {
        public SequenceStopException(string reason) : base(reason) { }
    }
}
