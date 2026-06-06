using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QMC.Common
{
    [Serializable]
    public enum MotionBoardType
    {
        Unknown,
        Ajin,
        ACS
    }

    [Serializable]
    public enum MotionDirection
    {
        Forward,
        Backward,
    }

    [Serializable]
    public enum HomingState
    {
        /// <summary>
        /// Homing 동작을 수행하지 않는 상태이거나 Homing이 수행되지 않은 상태이다.
        /// </summary>
        None,
        /// <summary>
        /// Homing Procedure를 실행중인 상태이다.
        /// </summary>
        Homing,
        /// <summary>
        /// 모든 동작을 정상적으로 완료한 상태이다.
        /// </summary>
        Completed
    }
    [Serializable]
    public enum ActiveLevel
    {
        //
        // 요약:
        //     꺼진 상태를 액티브로 인식함을 나타냅니다.
        Low = 0,
        //
        // 요약:
        //     켜진 상태를 액티브로 인식함을 나타냅니다.
        High = 1
    }
    [Serializable]
    public enum MotorEventAction
    {
        //
        // 요약:
        //     액션을 하지 않습니다.
        None = 0,
        //
        // 요약:
        //     정지합니다.
        Stop = 1,
        //
        // 요약:
        //     비상정지합니다.
        EmergencyStop = 2,
        //
        // 요약:
        //     Abort합니다.
        Abort = 3
    }
    [Serializable]
    public enum AxisState
    {
        //
        // 요약:
        //     정상 상태임을 나타냅니다.
        Idle = 0,
        //
        // 요약:
        //     구동중인 상태임을 나타냅니다
        Moving = 1,
        //
        // 요약:
        //     멈추고 있는 상태임을 나타냅니다
        Stopping = 2,
        //
        // 요약:
        //     에러가 발생한 상태임을 나타냅니다
        Error = 3
    }
    [Serializable]
    public enum HomeDirection : int
    {
        Ccw = 0,
        Cw = 1,
    }

    [Serializable]
    public enum HomeSignal : uint
    {
        PositiveLimit = 0,
        NegativeLimit = 1,
        HomeSensor = 4,
        ZPhase = 5,
    }
    [Serializable]
    public enum HomeZPhase : uint
    {
        None,
        Cw = 1,
        Ccw = 2,
    }

    [Serializable]
    public enum DisplayAxisType
    {
        Horizontal,
        Horizontal2,

        Vertical,
        Vertical2,

        Theta,

        CombinationHorizontal,
        CombinationVertical,

        CombinationStacker0,
        CombinationStacker1,

        CombinationPicker,

        UVW_Horizontal,
        UVW_Vertical, 
    }

    [Serializable]
    public enum PulseOutput { TwoPulse_High_CCW_CW, TwoPulse_Low_CCW_CW, AB_Phase }

    [Serializable]
    public enum EncoderInput { Normal, Reverse, Reverse_SQR4 }

    [Serializable]
    public enum InputSource { Encoder, ServoDriver, External }

    [Serializable]
    public enum StopMode { Emergency, DecelStop }

    [Serializable]
    public enum HomeMode { NegativeLimit, PositiveLimit, HomeSensor }

    [Serializable]
    public enum InPosition { Low, High, Unused, Used }
}
