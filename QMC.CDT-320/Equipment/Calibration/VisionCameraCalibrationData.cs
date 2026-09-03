using System;
using System.Runtime.Serialization;

namespace QMC.CDT320.Calibration
{
    [DataContract]
    public sealed class VisionReticleMeasurement
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

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
            MeasuredAt = SafeUnsetDateTime;
            Raw = string.Empty;
        }

        public void EnsureSerializableDateTimes()
        {
            MeasuredAt = EnsureSerializableDateTime(MeasuredAt);
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }
    }

    [DataContract]
    public sealed class VisionCameraPixelCalibration
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public double ImageWidthPixel { get; set; } = 640.0;
        [DataMember] public double ImageHeightPixel { get; set; } = 480.0;
        [DataMember] public double ImageCenterPixelX { get; set; } = 320.0;
        [DataMember] public double ImageCenterPixelY { get; set; } = 240.0;
        [DataMember] public double PixelToMmX { get; set; } = 0.001;
        [DataMember] public double PixelToMmY { get; set; } = 0.001;
        [DataMember] public bool ResolutionFromVision { get; set; }
        [DataMember] public DateTime ResolutionUpdatedAt { get; set; }

        public void EnsureDefaults(double fallbackCenterX, double fallbackCenterY, double fallbackPixelToMmX, double fallbackPixelToMmY)
        {
            if (ImageWidthPixel <= 0) ImageWidthPixel = 640.0;
            if (ImageHeightPixel <= 0) ImageHeightPixel = 480.0;
            if (ImageCenterPixelX == 0) ImageCenterPixelX = fallbackCenterX != 0 ? fallbackCenterX : ImageWidthPixel / 2.0;
            if (ImageCenterPixelY == 0) ImageCenterPixelY = fallbackCenterY != 0 ? fallbackCenterY : ImageHeightPixel / 2.0;
            if (PixelToMmX == 0) PixelToMmX = fallbackPixelToMmX != 0 ? fallbackPixelToMmX : 0.001;
            if (PixelToMmY == 0) PixelToMmY = fallbackPixelToMmY != 0 ? fallbackPixelToMmY : 0.001;
            ResolutionUpdatedAt = EnsureSerializableDateTime(ResolutionUpdatedAt);
        }

        public void ApplyImageSize(double widthPixel, double heightPixel)
        {
            if (widthPixel <= 0 || heightPixel <= 0)
                return;

            ImageWidthPixel = widthPixel;
            ImageHeightPixel = heightPixel;
            ImageCenterPixelX = widthPixel / 2.0;
            ImageCenterPixelY = heightPixel / 2.0;
            ResolutionFromVision = true;
            ResolutionUpdatedAt = DateTime.Now;
        }

        public void EnsureSerializableDateTimes()
        {
            ResolutionUpdatedAt = EnsureSerializableDateTime(ResolutionUpdatedAt);
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }

        public double PixelToMmOffsetX(double pixelX)
        {
            return (pixelX - ImageCenterPixelX) * PixelToMmX;
        }

        public double PixelToMmOffsetY(double pixelY)
        {
            return (ImageCenterPixelY - pixelY) * PixelToMmY;
        }
    }

    [DataContract]
    public sealed class VisionCameraCalibrationData
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

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
        [DataMember] public VisionCameraPixelCalibration BottomCamera { get; set; } = new VisionCameraPixelCalibration();
        [DataMember] public VisionCameraPixelCalibration InputCamera { get; set; } = new VisionCameraPixelCalibration();
        [DataMember] public VisionCameraPixelCalibration OutputCamera { get; set; } = new VisionCameraPixelCalibration();
        [DataMember] public VisionCameraPixelCalibration FrontSideCamera { get; set; } = new VisionCameraPixelCalibration();
        [DataMember] public VisionCameraPixelCalibration RearSideCamera { get; set; } = new VisionCameraPixelCalibration();
        [DataMember] public CalibrationMotionSettings Motion { get; set; } = new CalibrationMotionSettings();
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
            if (BottomCamera == null) BottomCamera = new VisionCameraPixelCalibration();
            if (InputCamera == null) InputCamera = new VisionCameraPixelCalibration();
            if (OutputCamera == null) OutputCamera = new VisionCameraPixelCalibration();
            if (FrontSideCamera == null) FrontSideCamera = new VisionCameraPixelCalibration();
            if (RearSideCamera == null) RearSideCamera = new VisionCameraPixelCalibration();
            if (Motion == null) Motion = new CalibrationMotionSettings();
            Motion.EnsureDefaults();
            BottomCamera.EnsureDefaults(ImageCenterPixelX, ImageCenterPixelY, PixelToMmX, PixelToMmY);
            InputCamera.EnsureDefaults(ImageCenterPixelX, ImageCenterPixelY, PixelToMmX, PixelToMmY);
            OutputCamera.EnsureDefaults(ImageCenterPixelX, ImageCenterPixelY, PixelToMmX, PixelToMmY);
            FrontSideCamera.EnsureDefaults(ImageCenterPixelX, ImageCenterPixelY, PixelToMmX, PixelToMmY);
            RearSideCamera.EnsureDefaults(ImageCenterPixelX, ImageCenterPixelY, PixelToMmX, PixelToMmY);
            EnsureSerializableDateTimes();
            if (UpdatedBy == null) UpdatedBy = string.Empty;
        }

        public void EnsureSerializableDateTimes()
        {
            BottomReticle.EnsureSerializableDateTimes();
            InputReticle.EnsureSerializableDateTimes();
            OutputReticle.EnsureSerializableDateTimes();
            BottomCamera.EnsureSerializableDateTimes();
            InputCamera.EnsureSerializableDateTimes();
            OutputCamera.EnsureSerializableDateTimes();
            FrontSideCamera.EnsureSerializableDateTimes();
            RearSideCamera.EnsureSerializableDateTimes();
            UpdatedAt = EnsureSerializableDateTime(UpdatedAt);
        }

        private static DateTime EnsureSerializableDateTime(DateTime value)
        {
            if (value <= DateTime.MinValue.AddDays(1) || value >= DateTime.MaxValue.AddDays(-1))
                return SafeUnsetDateTime;

            return value;
        }

        public bool CanCalculate
        {
            get
            {
                EnsureObjects();
                return BottomReticle.Valid && InputReticle.Valid && OutputReticle.Valid;
            }
        }

        // 카메라 브리지 산식은 여기서만 정의한다. Calculate와 소비처 정합 검사가 같은 식을 써야
        // 한쪽만 바뀌어 "구버전 산식"으로 차단되는 상태가 생기지 않는다.
        // 레티클을 두 카메라의 공통 기준점으로 놓으면 브리지는 두 측정값의 차다.
        // Mm은 PixelToMmOffsetX/Y가 이미 기계 프레임으로 환산한 값이라 여기서 부호를 다시 뒤집지 않는다.
        public double ResolveInputBridgeX()
        {
            return BottomReticle.MmX - InputReticle.MmX;
        }

        public double ResolveInputBridgeY()
        {
            return BottomReticle.MmY - InputReticle.MmY;
        }

        // Output은 실측 검증 전이라 기존 식을 유지한다.
        public double ResolveOutputBridgeX()
        {
            return -(BottomReticle.MmX + OutputReticle.MmX);
        }

        public double ResolveOutputBridgeY()
        {
            return -(BottomReticle.MmY + OutputReticle.MmY);
        }

        public bool Calculate(string updatedBy)
        {
            EnsureObjects();
            if (!CanCalculate)
            {
                Valid = false;
                return false;
            }

            InputToBottomOffsetX = ResolveInputBridgeX();
            InputToBottomOffsetY = ResolveInputBridgeY();
            OutputToBottomOffsetX = ResolveOutputBridgeX();
            OutputToBottomOffsetY = ResolveOutputBridgeY();
            UpdatedAt = DateTime.Now;
            UpdatedBy = updatedBy ?? string.Empty;
            Valid = true;
            return true;
        }
    }
}
