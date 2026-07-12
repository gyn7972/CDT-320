using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using QMC.Vision.Inspector;

namespace QMC.BottomInspectTest
{
    /// <summary>
    /// 검사 실행 공용 헬퍼 — GUI(MainForm)와 헤드리스(--auto) 모드가 공유한다.
    /// 이미지 로드(그레이 변환) / 2배 확장 / BottomInspectionParameter 구성.
    /// </summary>
    public static class InspectionRunner
    {
        /// <summary>이미지 파일 → 8bit 그레이 배열(24bpp 경유 — 인덱스/컬러 포맷 모두 처리).</summary>
        public static byte[] LoadGray(string path, out int w, out int h)
        {
            using (var src = new Bitmap(path))
            {
                w = src.Width; h = src.Height;
                using (var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb))
                {
                    using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, new Rectangle(0, 0, w, h));
                    var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        var gray = new byte[w * h];
                        var row = new byte[Math.Abs(data.Stride)];
                        for (int y = 0; y < h; y++)
                        {
                            Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                            int o = y * w;
                            for (int x = 0; x < w; x++)
                            {
                                int i = x * 3;
                                gray[o + x] = (byte)((row[i] + row[i + 1] + row[i + 2]) / 3);
                            }
                        }
                        return gray;
                    }
                    finally { bmp.UnlockBits(data); }
                }
            }
        }

        /// <summary>1배 그레이를 2배 bilinear 확장 — 검사부(bSimulate)의 '2배 확장 이미지' 입력 규약 충족용.</summary>
        public static byte[] Upscale2xBilinear(byte[] src, int w, int h, out int w2, out int h2)
        {
            w2 = w * 2; h2 = h * 2;
            var dst = new byte[w2 * h2];
            int dw = w2;
            Parallel.For(0, h2, y =>
            {
                double sy = (y + 0.5) * 0.5 - 0.5;
                int y0 = (int)Math.Floor(sy);
                double fy = sy - y0;
                int y1 = y0 + 1;
                if (y0 < 0) { y0 = 0; y1 = 0; fy = 0; }
                else if (y1 >= h) { y1 = h - 1; y0 = Math.Min(y0, h - 1); }
                int row = y * dw;
                for (int x = 0; x < dw; x++)
                {
                    double sx = (x + 0.5) * 0.5 - 0.5;
                    int x0 = (int)Math.Floor(sx);
                    double fx = sx - x0;
                    int x1 = x0 + 1;
                    if (x0 < 0) { x0 = 0; x1 = 0; fx = 0; }
                    else if (x1 >= w) { x1 = w - 1; x0 = Math.Min(x0, w - 1); }
                    double v = src[y0 * w + x0] * (1 - fx) * (1 - fy)
                             + src[y0 * w + x1] * fx * (1 - fy)
                             + src[y1 * w + x0] * (1 - fx) * fy
                             + src[y1 * w + x1] * fx * fy;
                    dst[row + x] = (byte)(v + 0.5);
                }
            });
            return dst;
        }

        /// <summary>테스트 파라미터 → BottomInspectionParameter 구성.</summary>
        public static BottomInspectionParameter BuildParameter(TestParameters p, byte[] gray, int w, int h, string savePath)
        {
            return new BottomInspectionParameter
            {
                Images = new List<byte[]> { gray },
                ImageWidth = w,
                ImageHeight = h,
                ChipRoi = new Rectangle(0, 0, w, h),
                Threshold = p.Threshold,
                SelectedChipType = p.DarkChip ? InspectionParameterBase.ChipType.Black : InspectionParameterBase.ChipType.White,
                ChippingDepth = p.ChippingDepth,
                ChippingLength = p.ChippingLength,
                ChipLowerSpecLimit = new SizeF((float)p.ChipWidthLower, (float)p.ChipHeightLower),
                ChipUpperSpecLimit = new SizeF((float)p.ChipWidthUpper, (float)p.ChipHeightUpper),
                ForeignObjectSize = p.ForeignObjectSize,
                FirstPeekValueThreshold = p.FirstPeekValueThreshold,
                PeekValueThreshold = p.PeekValueThreshold,
                Stdev = p.Stdev,
                TopHatRadius = p.TopHatRadius,
                TopHatThreshold = p.TopHatThreshold,
                MinForeignAreaFilterSize = p.MinForeignAreaFilterSize,
                LinkDistance = p.LinkDistance,
                PortentiolDefactMinSize = p.PortentiolDefactMinSize,
                UseContaminationInspection = p.UseContaminationInspection,
                IsSaveGoodImage = p.SaveInspectImages,
                FileSavePath = string.IsNullOrEmpty(savePath) ? Path.GetTempPath() : savePath,
                WaferID = "TEST",
                IndexX = 0,
                IndexY = 0
            };
        }

        /// <summary>검사기 생성 + 픽셀사이즈 주입.</summary>
        public static CDTInspector CreateInspector(TestParameters p)
        {
            var inspector = new CDTInspector { bSimulate = true };
            var cfg = new VisionConfig();
            cfg.BottomVision.PixelSizeWidthMm = p.PixelSizeWidthMm;
            cfg.BottomVision.PixelSizeHeightMm = p.PixelSizeHeightMm;
            inspector.SetVisionConfig(cfg);
            return inspector;
        }
    }
}
