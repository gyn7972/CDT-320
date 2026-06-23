using System;
using System.Runtime.Serialization;

namespace QMC.CDT320.Calibration
{
    [DataContract]
    public sealed class VisionReticleMeasurement
    {
        [DataMember] public bool Valid { get; set; }
        [DataMember] public string CameraName { get; set; }
        [DataMember] public double PixelX { get; set; }
        [DataMember] public double PixelY { get; set; }
        [DataMember] public double MmX { get; set; }
        [DataMember] public double MmY { get; set; }
        [DataMember] public double Score { get; set; }
        [DataMember] public double AngleDeg { get; set; }
        [DataMember] public double VisionXPosition { get; set; }
        [DataMember] public double StageYPosition { get; set; }
        [DataMember] public bool HasVisionXPosition { get; set; }
        [DataMember] public bool HasStageYPosition { get; set; }
        [DataMember] public DateTime MeasuredAt { get; set; }
        [DataMember] public string Raw { get; set; }

        public void Clear()
        {
            Valid = false;
            CameraName = string.Empty;
            PixelX = 0;
            PixelY = 0;
            MmX = 0;
            MmY = 0;
            Score = 0;
            AngleDeg = 0;
            VisionXPosition = 0;
            StageYPosition = 0;
            HasVisionXPosition = false;
            HasStageYPosition = false;
            MeasuredAt = DateTime.MinValue;
            Raw = string.Empty;
        }
    }

    [DataContract]
    public sealed class VisionCameraCalibrationData
    {
        [DataMember] public VisionReticleMeasurement BottomReticle { get; set; } = new VisionReticleMeasurement();
        [DataMember] public VisionReticleMeasurement InputReticle { get; set; } = new VisionReticleMeasurement();
        [DataMember] public VisionReticleMeasurement OutputReticle { get; set; } = new VisionReticleMeasurement();
        [DataMember] public double InputToBottomOffsetX { get; set; }
        [DataMember] public double InputToBottomOffsetY { get; set; }
        [DataMember] public double OutputToBottomOffsetX { get; set; }
        [DataMember] public double OutputToBottomOffsetY { get; set; }
        [DataMember] public double ImageCenterPixelX { get; set; } = 320.0;
        [DataMember] public double ImageCenterPixelY { get; set; } = 240.0;
        [DataMember] public double PixelToMmX { get; set; } = 0.001;
        [DataMember] public double PixelToMmY { get; set; } = 0.001;
        [DataMember] public bool Valid { get; set; }
        [DataMember] public DateTime UpdatedAt { get; set; }
        [DataMember] public string UpdatedBy { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureObjects();
        }

        public void EnsureObjects()
        {
            if (BottomReticle == null) BottomReticle = new VisionReticleMeasurement();
            if (InputReticle == null) InputReticle = new VisionReticleMeasurement();
            if (OutputReticle == null) OutputReticle = new VisionReticleMeasurement();
            if (ImageCenterPixelX == 0) ImageCenterPixelX = 320.0;
            if (ImageCenterPixelY == 0) ImageCenterPixelY = 240.0;
            if (PixelToMmX == 0) PixelToMmX = 0.001;
            if (PixelToMmY == 0) PixelToMmY = 0.001;
            if (UpdatedBy == null) UpdatedBy = string.Empty;
        }

        public bool CanCalculate
        {
            get
            {
                EnsureObjects();
                return BottomReticle.Valid && InputReticle.Valid && OutputReticle.Valid;
            }
        }

        public bool Calculate(string updatedBy)
        {
            EnsureObjects();
            if (!CanCalculate)
            {
                Valid = false;
                return false;
            }

            InputToBottomOffsetX = InputReticle.MmX - BottomReticle.MmX;
            InputToBottomOffsetY = InputReticle.MmY - BottomReticle.MmY;
            OutputToBottomOffsetX = OutputReticle.MmX - BottomReticle.MmX;
            OutputToBottomOffsetY = OutputReticle.MmY - BottomReticle.MmY;
            UpdatedAt = DateTime.Now;
            UpdatedBy = updatedBy ?? string.Empty;
            Valid = true;
            return true;
        }
    }
}
