using System;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 픽셀 → mm 좌표 변환용 스케일 (mm/pixel).
    /// 310 의 <c>SentiCore.Products.Devices.Visions.VisionScale</c> 와 동일 의미.
    /// </summary>
    [Serializable]
    public class VisionScale
    {
        public double X { get; set; } = 1.0;  // mm/pixel (가로)
        public double Y { get; set; } = 1.0;  // mm/pixel (세로)

        public VisionScale() { }
        public VisionScale(double x, double y) { X = x; Y = y; }

        public VisionScale Clone() => new VisionScale(X, Y);

        /// <summary>
        /// 픽셀 좌표 → 카메라 좌표(mm) 로 변환. 이미지 중앙을 원점으로 함.
        /// </summary>
        /// <param name="scale">스케일 (mm/pixel).</param>
        /// <param name="vec">카메라 부호/회전 벡터.</param>
        /// <param name="imageWidth">이미지 폭(픽셀).</param>
        /// <param name="imageHeight">이미지 높이(픽셀).</param>
        /// <param name="pixelX">픽셀 X.</param>
        /// <param name="pixelY">픽셀 Y.</param>
        public static void ConvertPosition(
            VisionScale scale, CameraVector vec,
            int imageWidth, int imageHeight,
            double pixelX, double pixelY,
            out double mmX, out double mmY)
        {
            if (scale == null) scale = new VisionScale();
            if (vec == null)   vec   = new CameraVector();

            // 이미지 중심 기준 오프셋
            double dx = pixelX - imageWidth  / 2.0;
            double dy = pixelY - imageHeight / 2.0;

            // 회전(가로/세로 스왑)
            if (vec.IsRotated)
            {
                double t = dx; dx = dy; dy = t;
            }

            // 부호 반전
            if (vec.InvertedX) dx = -dx;
            if (vec.InvertedY) dy = -dy;

            // 픽셀 → mm
            mmX = dx * scale.X;
            mmY = dy * scale.Y;
        }
    }
}
