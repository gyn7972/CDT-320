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

        public OverlayMark(double centerX, double centerY, double score)
        {
            CenterX = centerX;
            CenterY = centerY;
            Score = score;
        }
    }
}
