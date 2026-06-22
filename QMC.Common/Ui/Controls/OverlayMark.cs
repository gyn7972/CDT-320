namespace QMC.Common.Ui.Controls
{
    /// <summary>
    /// <see cref="CameraViewBase"/> 결과 오버레이의 마크 1개(이미지 좌표 기준 중심 + 점수).
    /// 비전 결과(MatchResult 등) 프로젝트 타입에 의존하지 않도록 한 일반 구조체.
    /// </summary>
    public struct OverlayMark
    {
        public double CenterX;
        public double CenterY;
        public double Score;
        /// <summary>매칭 회전각(deg). 박스 표시에 사용(0=무회전).</summary>
        public double AngleDeg;
        /// <summary>매칭 박스 가로(이미지 px). 0 이면 박스 미표시(점+점수만).</summary>
        public double BoxW;
        /// <summary>매칭 박스 세로(이미지 px).</summary>
        public double BoxH;

        public OverlayMark(double centerX, double centerY, double score)
        {
            CenterX = centerX;
            CenterY = centerY;
            Score = score;
            AngleDeg = 0;
            BoxW = 0;
            BoxH = 0;
        }

        public OverlayMark(double centerX, double centerY, double score, double angleDeg, double boxW, double boxH)
        {
            CenterX = centerX;
            CenterY = centerY;
            Score = score;
            AngleDeg = angleDeg;
            BoxW = boxW;
            BoxH = boxH;
        }
    }
}
