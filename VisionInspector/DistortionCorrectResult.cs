namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 왜곡 보정 결과 클래스
    /// </summary>
    public class DistortionCorrectResult
    {
        /// <summary>
        /// 보정 전 위치 배열 (X, Y)
        /// </summary>
        public PointD[] BeforeCorrection { get; set; }

        /// <summary>
        /// 보정 후 위치 배열 (X, Y)
        /// </summary>
        public PointD[] AfterCorrection { get; set; }
    }

    /// <summary>
    /// double 타입의 X, Y 좌표 구조체
    /// </summary>
    public struct PointD
    {
        public double X { get; set; }
        public double Y { get; set; }

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}