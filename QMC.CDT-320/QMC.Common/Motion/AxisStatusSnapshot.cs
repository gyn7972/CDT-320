using System;

namespace QMC.Common.Motion
{
    public sealed class AxisStatusSnapshot
    {
        public DateTime Timestamp { get; set; }
        public string UnitName { get; set; }
        public string AxisName { get; set; }
        public string DisplayName { get; set; }
        public int BoardNo { get; set; }
        public int AxisNo { get; set; }
        public string Unit { get; set; }
        public double CommandPosition { get; set; }
        public double ActualPosition { get; set; }
        public double CurrentVelocity { get; set; }
        public bool IsServoOn { get; set; }
        public bool IsMoving { get; set; }
        public bool IsInPosition { get; set; }
        public bool IsAlarm { get; set; }
        public uint AlarmCode { get; set; }
        public bool IsHomeDone { get; set; }
        public bool SensorPel { get; set; }
        public bool SensorMel { get; set; }
        public bool SensorOrg { get; set; }
        public bool IsSimulationMode { get; set; }

        public static AxisStatusSnapshot FromAxis(BaseAxis axis)
        {
            if (axis == null) throw new ArgumentNullException(nameof(axis));

            return new AxisStatusSnapshot
            {
                Timestamp = DateTime.Now,
                UnitName = axis.Setup != null ? axis.Setup.UnitName : string.Empty,
                AxisName = axis.Name,
                DisplayName = axis.Setup != null ? axis.Setup.DisplayName : axis.Name,
                BoardNo = axis.Setup != null ? axis.Setup.BoardNo : 0,
                AxisNo = axis.Setup != null ? axis.Setup.AxisNo : -1,
                Unit = axis.Setup != null ? axis.Setup.Unit : string.Empty,
                CommandPosition = axis.CommandPosition,
                ActualPosition = axis.ActualPosition,
                CurrentVelocity = axis.CurrentVelocity,
                IsServoOn = axis.IsServoOn,
                IsMoving = axis.IsMoving,
                IsInPosition = axis.IsInPosition,
                IsAlarm = axis.IsAlarm,
                AlarmCode = axis.AlarmCode,
                IsHomeDone = axis.IsHomeDone,
                SensorPel = axis.Sensor_PEL,
                SensorMel = axis.Sensor_MEL,
                SensorOrg = axis.Sensor_ORG,
                IsSimulationMode = axis.Config == null || axis.Config.IsSimulationMode
            };
        }
    }
}
