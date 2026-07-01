using System.Drawing;

namespace QMC.Common.Ui.Controls
{
    /// <summary>검사 결과 오버레이(판정=우상단, 결과라인=우측하단 열) 공용 렌더러.
    /// CameraViewBase(레시피/작업 모니터링)와 VisionImageView(팝업 픽커 패널)가 동일하게 호출 → 표시 통일.</summary>
    public static class ResultOverlayRenderer
    {
        /// <param name="topInset">상단 툴바 등으로 밀린 오프셋(px). 없으면 0.</param>
        /// <param name="lineColors">줄별 색(null 또는 A==0 이면 판정색 사용).</param>
        public static void Draw(Graphics g, int width, int height, int topInset,
                                string verdict, bool pass, string[] lines, Color[] lineColors)
        {
            if (!string.IsNullOrEmpty(verdict))
                using (var f  = new Font("Segoe UI", 26F, FontStyle.Bold))
                using (var br = new SolidBrush(pass ? Color.LimeGreen : Color.Red))
                {
                    var sz = g.MeasureString(verdict, f);
                    g.DrawString(verdict, f, br, width - sz.Width - 14, 6 + topInset);
                }

            if (lines != null && lines.Length > 0)
                using (var f = new Font("맑은 고딕", 9F))
                {
                    Color def = pass ? Color.FromArgb(120, 230, 120) : Color.FromArgb(255, 120, 120);
                    float y = height - 6 - lines.Length * 17;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (string.IsNullOrEmpty(line)) { y += 17; continue; }
                        Color c = (lineColors != null && i < lineColors.Length && lineColors[i].A != 0)
                                  ? lineColors[i] : def;
                        var sz = g.MeasureString(line, f);
                        using (var br = new SolidBrush(c))
                            g.DrawString(line, f, br, width - sz.Width - 10, y);
                        y += 17;
                    }
                }
        }
    }
}
