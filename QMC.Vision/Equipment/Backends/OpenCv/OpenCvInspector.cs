using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.OpenCv
{
    /// <summary>OpenCV 기반 외관 검사 — EmguCV 로드 시 Canny 에지 + 평균 밝기, 미로드 시 평균 밝기 폴백.</summary>
    public class OpenCvInspector : IInspector
    {
        public string Id { get; }
        public Roi InspectionRoi { get; set; }

        /// <summary>Canny 임계값(하/상).</summary>
        public double CannyThreshold1 { get; set; } = 50;
        public double CannyThreshold2 { get; set; } = 150;

        private readonly OpenCvBackend _be;

        public OpenCvInspector(string id, OpenCvBackend be)
        {
            Id = id; _be = be;
            InspectionRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 200, Height = 200 };
        }

        public InspectionResult Inspect(Bitmap image)
        {
            var r = new InspectionResult { RoiName = Id, IsPass = true };
            if (image == null) return r;

            Rectangle roi = InspectionRoi.BoundingBox;
            roi.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            if (roi.Width <= 0 || roi.Height <= 0) return r;

            int w, h;
            byte[] gray = ToGray(image, roi, out w, out h);
            r.Items.Add(new InspectionItem { Name = "Area", Value = (w * h).ToString(), IsPass = true });

            // EmguCV 로드 시 실제 Canny 에지/평균. 실패 시 C# 평균으로 폴백.
            if (_be != null && _be.EmguLoaded)
            {
                double mean; long edges;
                if (OpenCvInterop.Instance.TryEdgeStats(gray, w, h, CannyThreshold1, CannyThreshold2, out mean, out edges))
                {
                    double edgeRatio = (w * h > 0) ? (double)edges / (w * h) : 0.0;
                    r.Items.Add(new InspectionItem { Name = "Mean Gray",  Value = mean.ToString("F1"),       IsPass = true });
                    r.Items.Add(new InspectionItem { Name = "EdgePixels", Value = edges.ToString(),          IsPass = true });
                    r.Items.Add(new InspectionItem { Name = "EdgeRatio",  Value = edgeRatio.ToString("F4"),  IsPass = true });
                    return r;
                }
            }

            // Fallback — 순수 C# 평균 밝기
            long sum = 0;
            for (int i = 0; i < gray.Length; i++) sum += gray[i];
            double meanFb = gray.Length > 0 ? (double)sum / gray.Length : 0.0;
            r.Items.Add(new InspectionItem { Name = "Mean Gray", Value = meanFb.ToString("F1"), IsPass = true });
            return r;
        }

        /// <summary>비트맵 rect 영역을 8bit 그레이 배열로 변환(24bpp 락 후 평균).</summary>
        private static byte[] ToGray(Bitmap bmp, Rectangle rect, out int w, out int h)
        {
            rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
            w = rect.Width; h = rect.Height;
            var gray = new byte[w * h];
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = data.Stride;
                byte[] row = new byte[stride];
                for (int y = 0; y < h; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * stride), row, 0, stride);
                    int gi = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int o = x * 3;
                        gray[gi + x] = (byte)((row[o] + row[o + 1] + row[o + 2]) / 3);
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
            return gray;
        }
    }
}
