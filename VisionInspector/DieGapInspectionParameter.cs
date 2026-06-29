using System.Drawing;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// Die Gap 검사 파라미터 클래스
    /// </summary>
    public class DieGapInspectionParameter
    {
        // ROI 영역
        public Rectangle Roi { get; set; }

        // 스레숄드
        public int Threshold { get; set; }

        // 이미지의 너비
        public int ImageWidth { get; set; }

        // 이미지의 높이
        public int ImageHeight { get; set; }
        public double UpperLimit { get; set; } = 0.1; // 상한값 (기본값: 0.0)
        public double LowerLimit { get; set; } = 0.0; // 하한값 (기본값: 0.0)

        public string WaferID { get; set; } = "Empty";
        public int IndexX { get; set; } = 1;
        public int IndexY { get; set; } = 1;

        // 검사 이미지 (byte 배열)
        public byte[] Image { get; set; }
    }
}