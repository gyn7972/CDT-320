using System;
using System.Collections;
using System.Drawing;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.Cognex
{
    /// <summary>
    /// Cognex CogCaliperTool 기반 에지 검출. 동적 reflection.
    /// 미로드/실패 시 픽셀 미분 fallback (간이).
    /// </summary>
    public class CognexCaliper : IEdgeFinder
    {
        public string Id { get; }
        public Roi MeasureRoi { get; set; }
        public double EdgeThreshold { get; set; } = 30;

        private readonly CognexBackend _be;

        public CognexCaliper(string id, CognexBackend be)
        {
            Id = id; _be = be;
            MeasureRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 200, Height = 100 };
        }

        public EdgeMeasurement Measure(Bitmap image)
        {
            if (image == null) return new EdgeMeasurement { Success = false, ErrorMessage = "no image" };

            if (_be != null && _be.CognexLoaded)
            {
                try
                {
                    var asms = _be.LoadedAssemblies;
                    var calType = CognexInterop.GetType("Cognex.VisionPro.Caliper.CogCaliperTool", asms);
                    if (calType == null) return MeasureFallback(image);

                    dynamic cal = Activator.CreateInstance(calType);
                    dynamic cogImg = CognexInterop.BitmapToICogImage(image, asms);
                    cal.InputImage = cogImg;

                    var r = MeasureRoi.BoundingBox;
                    r.Intersect(new Rectangle(0, 0, image.Width, image.Height));
                    if (r.Width > 0 && r.Height > 0)
                    {
                        try
                        {
                            dynamic region = CognexInterop.NewRectangle(r.X, r.Y, r.Width, r.Height, asms);
                            cal.Region = region;
                        }
                        catch { }
                    }

                    try { cal.RunParams.EdgeMode = "Edge"; } catch { }
                    try { cal.RunParams.ContrastThreshold = EdgeThreshold; } catch { }
                    cal.Run();

                    var result = new EdgeMeasurement { Success = true };
                    try
                    {
                        dynamic results = cal.Results;
                        if (results is IEnumerable e)
                        {
                            int n = 0;
                            foreach (dynamic res in e)
                            {
                                double x = (double)CognexInterop.TryGet(res, "PositionX", 0.0);
                                double y = (double)CognexInterop.TryGet(res, "PositionY", 0.0);
                                double s = (double)CognexInterop.TryGet(res, "ContrastScore", 0.0);
                                result.EdgePoints.Add(new Point((int)x, (int)y));
                                if (s > result.EdgeStrength) result.EdgeStrength = s;
                                n++;
                            }
                            result.WidthPixels  = r.Width;
                            result.HeightPixels = r.Height;
                        }
                    }
                    catch { }
                    if (result.EdgePoints.Count == 0) return MeasureFallback(image);
                    return result;
                }
                catch (Exception ex) { return MeasureFallback(image, "Cognex run: " + ex.Message); }
            }
            return MeasureFallback(image);
        }

        /// <summary>간이 fallback — ROI 안에서 수평 그라디언트 합 평균.</summary>
        private EdgeMeasurement MeasureFallback(Bitmap image, string note = null)
        {
            try
            {
                var r = MeasureRoi.BoundingBox;
                r.Intersect(new Rectangle(0, 0, image.Width, image.Height));
                if (r.Width <= 0 || r.Height <= 0)
                    return new EdgeMeasurement { Success = false, ErrorMessage = "ROI out of image" };

                double sum = 0;
                int n = 0;
                int step = Math.Max(1, Math.Min(r.Width, r.Height) / 64);
                for (int y = r.Top + step; y < r.Bottom - step; y += step)
                {
                    for (int x = r.Left + step; x < r.Right - step; x += step)
                    {
                        var c1 = image.GetPixel(x - step, y);
                        var c2 = image.GetPixel(x + step, y);
                        int gx = Math.Abs(c2.R - c1.R) + Math.Abs(c2.G - c1.G) + Math.Abs(c2.B - c1.B);
                        sum += gx;
                        n++;
                    }
                }
                double avg = n > 0 ? sum / n : 0;
                return new EdgeMeasurement
                {
                    Success = true,
                    WidthPixels = r.Width, HeightPixels = r.Height,
                    EdgeStrength = avg,
                    ErrorMessage = note
                };
            }
            catch (Exception ex)
            {
                return new EdgeMeasurement { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}
