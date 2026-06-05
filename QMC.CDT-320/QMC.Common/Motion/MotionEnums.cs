namespace QMC.Common.Motion
{
    /// <summary>
    /// 축의 현재 동작 모드를 나타내는 열거형.
    /// </summary>
    public enum MotionMode
    {
        /// <summary>대기 상태 (이동 없음)</summary>
        None,
        /// <summary>절대 좌표 이동</summary>
        Absolute,
        /// <summary>상대 거리 이동</summary>
        Relative,
        /// <summary>Jog 연속 이동</summary>
        Jog,
        /// <summary>원점 복귀 시퀀스</summary>
        Homing
    }

    /// <summary>
    /// Jog 이동 시 속도 단계를 나타내는 열거형.
    /// </summary>
    public enum JogSpeedType
    {
        /// <summary>Recipe.JogCoarseVelocity 사용 (빠른 조그)</summary>
        Coarse,
        /// <summary>Recipe.JogFineVelocity 사용 (미세 조그)</summary>
        Fine,
        /// <summary>호출자가 직접 지정한 속도 사용</summary>
        Custom
    }
}
