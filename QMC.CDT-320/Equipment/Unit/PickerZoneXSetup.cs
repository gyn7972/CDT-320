using System;
using System.Runtime.Serialization;

namespace QMC.CDT320
{
    [DataContract]
    public sealed class PickerZoneXRange
    {
        [DataMember] public bool Enabled { get; set; }
        [DataMember] public double MinX { get; set; }
        [DataMember] public double MaxX { get; set; }

        public bool Contains(double position, double tolerance)
        {
            double safeTolerance = Math.Max(0.0, tolerance);
            double min = Math.Min(MinX, MaxX) - safeTolerance;
            double max = Math.Max(MinX, MaxX) + safeTolerance;
            return position >= min && position <= max;
        }

        public void SetAround(double center, double halfWidth)
        {
            double safeHalfWidth = Math.Max(0.0, halfWidth);
            MinX = center - safeHalfWidth;
            MaxX = center + safeHalfWidth;
            Enabled = true;
        }
    }

    [DataContract]
    public sealed class PickerZoneXSetup
    {
        [DataMember] public bool UseEncoderZone { get; set; } = true;
        [DataMember] public double ZoneTolerance { get; set; } = 1.0;
        [DataMember] public PickerZoneXRange Avoid { get; set; } = new PickerZoneXRange();
        [DataMember] public PickerZoneXRange Input { get; set; } = new PickerZoneXRange();
        [DataMember] public PickerZoneXRange Bottom { get; set; } = new PickerZoneXRange();
        [DataMember] public PickerZoneXRange Side { get; set; } = new PickerZoneXRange();
        [DataMember] public PickerZoneXRange Output { get; set; } = new PickerZoneXRange();

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Ensure();
        }

        public void Ensure()
        {
            if (ZoneTolerance <= 0.0)
                ZoneTolerance = 1.0;
            if (Avoid == null)
                Avoid = new PickerZoneXRange();
            if (Input == null)
                Input = new PickerZoneXRange();
            if (Bottom == null)
                Bottom = new PickerZoneXRange();
            if (Side == null)
                Side = new PickerZoneXRange();
            if (Output == null)
                Output = new PickerZoneXRange();
        }
    }
}
