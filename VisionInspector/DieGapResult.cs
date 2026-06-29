using System.Drawing;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// Die Gap 검사 결과 클래스
    /// </summary>
    public class DieGapResult
    {
        // 불량 코드
        public int DefectCode { get; set; }

        // Gap 정보 (Left, Top, Right, Bottom)
        public GapSet Gaps { get; set; } = new GapSet();

        // 각도
        public double Angle { get; set; }

        // 오프셋
        public PointF Offset { get; set; }

        // 코너 4포인트
        public PointF[] Corners { get; set; } = new PointF[4];

    }
}