using System;
using System.Collections.Generic;
using System.Globalization;
using QMC.Common.Motion;

namespace QMC.CDT320.Initialization
{
    /// <summary>
    /// 초기화 경로를 판단한 순간의 축 상태입니다.
    /// 이 클래스는 상태를 읽어서 보관할 뿐 축을 움직이거나 상태를 변경하지 않습니다.
    /// </summary>
    internal sealed class AxisInitializeSafetySnapshot
    {
        public AxisInitializeSafetySnapshot(
            DateTime capturedAt,
            IList<AxisInitializeAxisState> axisStates)
        {
            CapturedAt = capturedAt;
            AxisStates = axisStates ?? new List<AxisInitializeAxisState>();
        }

        public DateTime CapturedAt { get; private set; }

        public IList<AxisInitializeAxisState> AxisStates { get; private set; }

        public string BuildSummary()
        {
            return "capturedAt=" + CapturedAt.ToString(
                       "yyyy-MM-dd HH:mm:ss.fff",
                       CultureInfo.InvariantCulture) +
                   ", axisCount=" + AxisStates.Count;
        }
    }

    /// <summary>
    /// 한 축의 현재 위치와 기본 모션 상태입니다.
    /// </summary>
    internal sealed class AxisInitializeAxisState
    {
        public string Name { get; private set; }
        public double ActualPosition { get; private set; }
        public double CommandPosition { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsInPosition { get; private set; }
        public bool IsServoOn { get; private set; }
        public bool IsAlarm { get; private set; }
        public uint AlarmCode { get; private set; }
        public bool IsHomeDone { get; private set; }

        public static AxisInitializeAxisState Capture(BaseAxis axis)
        {
            if (axis == null)
                return null;

            return new AxisInitializeAxisState
            {
                Name = axis.Name ?? string.Empty,
                ActualPosition = axis.ActualPosition,
                CommandPosition = axis.CommandPosition,
                IsMoving = axis.IsMoving,
                IsInPosition = axis.IsInPosition,
                IsServoOn = axis.IsServoOn,
                IsAlarm = axis.IsAlarm,
                AlarmCode = axis.AlarmCode,
                IsHomeDone = axis.IsHomeDone
            };
        }

        public string BuildLogText()
        {
            return "axis=" + Name +
                   ", actual=" + ActualPosition.ToString("0.###", CultureInfo.InvariantCulture) +
                   ", command=" + CommandPosition.ToString("0.###", CultureInfo.InvariantCulture) +
                   ", moving=" + IsMoving +
                   ", inPosition=" + IsInPosition +
                   ", servo=" + (IsServoOn ? "ON" : "OFF") +
                   ", alarm=" + (IsAlarm ? "ON" : "OFF") +
                   ", alarmCode=" + AlarmCode +
                   ", homeDone=" + IsHomeDone;
        }
    }
}
