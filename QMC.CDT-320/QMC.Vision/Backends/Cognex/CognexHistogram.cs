using System;
using System.Drawing;
using System.Drawing.Imaging;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.Cognex
{
    /// <summary>
    /// 밝기 히스토그램 분석.
    /// Cognex CogHistogramTool (있으면) 또는 직접 픽셀 통계.
    /// </summary>
    public class CognexHistogram : IHistogramAnalyzer
    {
        public string Id { get; }
        public Roi AnalysisRoi { get; set; }

        private readonly CognexBackend _be;

        public CognexHistogram(string id, CognexBackend be)
        {
            Id = id; _be = be;
            AnalysisRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 200, Height = 200 };
        }

        public HistogramResult Analyze(Bitmap image)
        {
            if (image == null) return new HistogramResult { Success = false, ErrorMessage = "no image" };

            // Cognex Histogram tool 시도 (있을 경우)
            if (_be != null && _be.CognexLoaded)
            {
                try
                {
                    var asms = _be.LoadedAssemblies;
                    var hType = CognexInterop.FindAny(asms,
                        "Cognex.VisionPro.ImageProcessing.CogHistogramTool",
                        "Cognex.VisionPro.CogHistogramTool");
                    if (hType != null)
                    {
                        dynamic h = Activator.CreateInstance(hType);
                        dynamic cogImg = CognexInterop.BitmapToICogImage(image, asms);
                        h.InputImage = cogImg;
                        var r = AnalysisRoi.BoundingBox;
                        r.Intersect(new Rectangle(0, 0, image.Width, image.Height));
                        try
                        {
                            dynamic region = CognexInterop.NewRectangle(r.X, r.Y, r.Width, r.Height, asms);
                            h.Region = region;
                        }
                        catch { }
                        h.Run();
                        try
                        {
                            double mean   = (double)CognexInterop.TryGet(h.Result, "Mean",   0.0);
                            double stdev  = (double)CognexInterop.TryGet(h.Result, "Stdev",  0.0);
                            double minVal = (double)CognexInterop.TryGet(h.Result, "Min",    0.0);
                            double maxVal = (double)CognexInterop.TryGet(h.Result, "Max",    0.0);
                            return new HistogramResult
                            {
                                Success = true,
                                Mean = mean, Stdev = stdev,
                                Min = (int)minVal, Max = (int)maxVal
                            };
                        }
                        catch { /* fall through */ }
                    }
                }
                catch { /* fall through */ }
            }

            // Fallback — 직접 픽셀 분석
            return AnalyzeFallback(image);
        }

        private HistogramResult AnalyzeFallback(Bitmap image)
        {
            try
            {
                var r = AnalysisRoi.BoundingBox;
                r.Intersect(new Rectangle(0, 0, image.Width, image.Height));
                if (r.Width <= 0 || r.Height <= 0)
                    return new HistogramResult { Success = false, ErrorMessage = "ROI empty" };

                int[] dist = new int[256];
                long sum = 0;
                long sumSq = 0;
                int n = 0;
                int min = 255, max = 0;

                int step = Math.Max(1, Math.Min(r.Width, r.Height) / 256);
                for (int y = r.Top; y < r.Bottom; y += step)
                {
                    for (int x = r.Left; x < r.Right; x += step)
                    {
                        var c = image.GetPixel(x, y);
                        int gray = (c.R + c.G + c.B) / 3;
                        dist[gray]++;
                        sum   += gray;
                        sumSq += gray * gray;
                        if (gray < min) min = gray;
                        if (gray > max) max = gray;
                        n++;
                    }
                }
                if (n == 0) return new HistogramResult { Success = false, ErrorMessage = "no samples" };
                double mean  = sum * 1.0 / n;
                double var   = sumSq * 1.0 / n - mean * mean;
                double stdev = var > 0 ? Math.Sqrt(var) : 0;
                return new HistogramResult
                {
                    Success = true, Mean = mean, Stdev = stdev,
                    Min = min, Max = max, Distribution = dist
                };
            }
            catch (Exception ex)
            {
                return new HistogramResult { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
