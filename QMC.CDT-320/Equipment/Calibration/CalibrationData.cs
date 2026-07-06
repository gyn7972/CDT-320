using System;
using System.Runtime.Serialization;

namespace QMC.CDT320.Calibration
{
    [DataContract]
    public sealed class CalibrationMotionSettings
    {
        public const double DefaultMoveVelocity = 10.0;
        public const double DefaultMoveAcceleration = 200.0;
        public const double DefaultMoveDeceleration = 200.0;
        public const int DefaultMoveTimeoutMs = 10000;

        [DataMember] public double MoveVelocity { get; set; } = DefaultMoveVelocity;
        [DataMember] public double MoveAcceleration { get; set; } = DefaultMoveAcceleration;
        [DataMember] public double MoveDeceleration { get; set; } = DefaultMoveDeceleration;
        [DataMember] public int MoveTimeoutMs { get; set; } = DefaultMoveTimeoutMs;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            if (MoveVelocity <= 0.0)
                MoveVelocity = DefaultMoveVelocity;
            if (MoveAcceleration <= 0.0)
                MoveAcceleration = DefaultMoveAcceleration;
            if (MoveDeceleration <= 0.0)
                MoveDeceleration = DefaultMoveDeceleration;
            if (MoveTimeoutMs <= 0)
                MoveTimeoutMs = DefaultMoveTimeoutMs;
        }

        public CalibrationMotionSettings Clone()
        {
            EnsureDefaults();
            return new CalibrationMotionSettings
            {
                MoveVelocity = MoveVelocity,
                MoveAcceleration = MoveAcceleration,
                MoveDeceleration = MoveDeceleration,
                MoveTimeoutMs = MoveTimeoutMs
            };
        }
    }

    [DataContract]
    public sealed class NeedleCalibrationData
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public CalibrationMotionSettings Motion { get; set; } = new CalibrationMotionSettings();
        [DataMember] public double VisionXPosition { get; set; }
        [DataMember] public double StageYPosition { get; set; }
        [DataMember] public double NeedleXPosition { get; set; }
        [DataMember] public double NeedleZPosition { get; set; }
        [DataMember] public double EjectPinZPosition { get; set; }
        [DataMember] public double VisionOffsetX { get; set; }
        [DataMember] public double VisionOffsetY { get; set; }
        [DataMember] public double NeedleXToVisionXOffset { get; set; }
        [DataMember] public double NeedleYToVisionYOffset { get; set; }
        [DataMember] public bool Valid { get; set; }
        [DataMember] public DateTime UpdatedAt { get; set; }
        [DataMember] public string UpdatedBy { get; set; }

        public void EnsureObjects()
        {
            if (Motion == null)
                Motion = new CalibrationMotionSettings();
            Motion.EnsureDefaults();
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
            if (UpdatedBy == null)
                UpdatedBy = string.Empty;
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }
    }

    [DataContract]
    public sealed class CalibrationData
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public string Version { get; set; } = "1.0";
        [DataMember] public VisionCameraCalibrationData Camera { get; set; } = new VisionCameraCalibrationData();
        [DataMember] public ColletCalibrationData Collet { get; set; } = new ColletCalibrationData();
        [DataMember] public NeedleCalibrationData Needle { get; set; } = new NeedleCalibrationData();
        [DataMember] public DateTime UpdatedAt { get; set; }
        [DataMember] public string UpdatedBy { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureObjects();
        }

        public void EnsureObjects()
        {
            if (string.IsNullOrWhiteSpace(Version))
                Version = "1.0";
            if (Camera == null)
                Camera = new VisionCameraCalibrationData();
            if (Collet == null)
                Collet = new ColletCalibrationData();
            if (Needle == null)
                Needle = new NeedleCalibrationData();

            Camera.EnsureObjects();
            Collet.EnsureObjects();
            Needle.EnsureObjects();
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
            if (UpdatedBy == null)
                UpdatedBy = string.Empty;
        }

        public void Touch(string updatedBy)
        {
            UpdatedAt = DateTime.Now;
            UpdatedBy = updatedBy ?? string.Empty;
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }
    }
}
