using QMC.Common;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 비전 시스템별 픽셀 사이즈 및 카메라 해상도 정보 클래스
    /// </summary>
    public class VisionConfig
    {
        public VisionConfig()
        {
                       // 기본값 설정
            WaferVision = new VisionSystemConfig
            {
                PixelSizeWidthMm = 0.005, // 예시 값
                PixelSizeHeightMm = 0.005, // 예시 값
                CameraResolutionWidth = 5120, // 예시 값
                CameraResolutionHeight = 5120// 예시 값
            };
            BottomVision = new VisionSystemConfig
            {
                PixelSizeWidthMm = 0.001399356618, // 예시 값
                PixelSizeHeightMm = 0.001399356618, // 예시 값
                CameraResolutionWidth = 12000, // 예시 값
                CameraResolutionHeight = 12000// 예시 값
            };
            TargetVision = new VisionSystemConfig
            {
                PixelSizeWidthMm = 0.003125, // 예시 값
                PixelSizeHeightMm = 0.003125, // 예시 값
                CameraResolutionWidth = 5120, // 예시 값
                CameraResolutionHeight = 5120// 예시 값
            };

            SideVisionBack = new VisionSystemConfig
            {
                PixelSizeWidthMm = 0.003125, // 예시 값
                PixelSizeHeightMm = 0.003125, // 예시 값
                CameraResolutionWidth = 5120, // 예시 값
                CameraResolutionHeight = 5120// 예시 값
            };
            _sideVisionFront = new VisionSystemConfig
            {
                PixelSizeWidthMm = 0.003125, // 예시 값
                PixelSizeHeightMm = 0.003125, // 예시 값
                CameraResolutionWidth = 5120, // 예시 값
                CameraResolutionHeight = 5120// 예시 값
            };

        }
        public VisionSystemConfig WaferVision { get; set; } = new VisionSystemConfig();
        private VisionSystemConfig _sideVisionFront;
        public VisionSystemConfig SideVisionFront 
        { 
            get
            {
                return _sideVisionFront;
            }
            set
            {
                if (value != null)
                {
                    Log.Write("SideVisionFront", "Size", "X:" + value.PixelSizeWidthMm.ToString() + "  Y:" + value.PixelSizeWidthMm.ToString());
                }
                _sideVisionFront = value;
            }
        } 

        public VisionSystemConfig SideVisionBack { get; set; } = new VisionSystemConfig();
        public VisionSystemConfig BottomVision { get; set; } = new VisionSystemConfig();
        public VisionSystemConfig TargetVision { get; set; } = new VisionSystemConfig();
    }

    /// <summary>
    /// 개별 비전 시스템의 픽셀 사이즈 및 해상도 정보
    /// </summary>
    public class VisionSystemConfig
    {
        /// <summary>
        /// 픽셀당 mm (Width)
        /// </summary>
        public double PixelSizeWidthMm { get; set; }

        /// <summary>
        /// 픽셀당 mm (Height)
        /// </summary>
        public double PixelSizeHeightMm { get; set; }

        /// <summary>
        /// 카메라 해상도 (Width, Height)
        /// </summary>
        public int CameraResolutionWidth { get; set; }
        public int CameraResolutionHeight { get; set; }
        public override string ToString()
        {
            return "W:" + PixelSizeWidthMm.ToString() + " H:" + PixelSizeHeightMm.ToString();
        }
    }
}