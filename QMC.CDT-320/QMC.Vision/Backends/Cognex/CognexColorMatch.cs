using System;
using System.Drawing;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.Cognex
{
    /// <summary>
    /// 컬러 매칭 — Cognex CogColorMatchTool 또는 단순 RGB 거리.
    /// </summary>
    public class CognexColorMatch : IColorMatcher
    {
        public string Id { get; }
        public Color  TargetColor { get; set; } = Color.Red;
        public int    Tolerance   { get; set; } = 30;   // 0~255 RGB 거리

        private readonly CognexBackend _be;

        public CognexColorMatch(string id, CognexBackend be) { Id = id; _be = be; }

        public ColorMatchResult Match(Bitmap image)
        {
            if (image == null) return new ColorMatchResult { Success = false, ErrorMessage = "no image" };

            // Cognex 시도 (있을 경우)
            if (_be != null && _be.CognexLoaded)
            {
                try
                {
                    var asms = _be.LoadedAssemblies;
                    var cmType = CognexInterop.FindAny(asms,
                        "Cognex.VisionPro.ColorMatch.CogColorMatchTool",
                        "Cognex.VisionPro.CogColorMatchTool");
                    if (cmType != null)
                    {
                        // Cognex ColorMatch 는 학습된 ColorPalette 가 필요 — 미지원 fallback
                    }
                }
                catch { }
            }

            return MatchFallback(image);
        }

        private ColorMatchResult MatchFallback(Bitmap image)
        {
            try
            {
                int total = 0, matched = 0;
                int step = Math.Max(1, Math.Min(image.Width, image.Height) / 200);
                int tol2 = Tolerance * Tolerance;

                for (int y = 0; y < image.Height; y += step)
                {
                    for (int x = 0; x < image.Width; x += step)
                    {
                        var c = image.GetPixel(x, y);
                        int dr = c.R - TargetColor.R;
                        int dg = c.G - TargetColor.G;
                        int db = c.B - TargetColor.B;
                        int dist2 = dr * dr + dg * dg + db * db;
                        if (dist2 <= tol2 * 3) matched++;   // RGB 합거리^2 비교
                        total++;
                    }
                }
                double pct = total > 0 ? (matched * 100.0 / total) : 0;
                return new ColorMatchResult
                {
                    Success = true, MatchPercent = pct,
                    MatchedPixels = matched, TotalPixels = total
                };
            }
            catch (Exception ex)
            {
                return new ColorMatchResult { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
