using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Collet = QMC.Vision.Core.Collet;   // 원본 알고리즘(StdDevFilter/BlobRectFinder/DetectedRect)

namespace QMC.Vision.Core
{
    /// <summary>
    /// 플랫 콜렛 파인더 — docs\Collet Finder 원본 알고리즘(StdDevFilter + BlobRectFinder)을
    /// 그대로 사용하는 얇은 어댑터. 풀해상도로 std-dev 텍스처 필터 → (정밀)최대 블랍 / (고속)모멘트
    /// → 최소면적 회전사각형. 결과를 <see cref="MatchResult"/> 로 변환해 MATCH 처리부에 연결한다.
    /// CUDA(ColletFinderCuda.dll) 있으면 GPU, 없으면 CPU 멀티스레드 폴백(원본 동작 동일).
    ///
    /// ※ 이전의 재구성(다운샘플+밀도채움) 제거 — 원본은 풀해상도로 처리해야 텍스처 std-dev 가
    ///   동일하게 솔리드 영역이 되어 최대 블랍이 콜렛 전체로 잡힌다.
    /// </summary>
    public static class FlatColletFinder
    {
        private const string RoiName = "ColletFinder.Flat";

        public static MatchResult Find(Bitmap image, Roi searchRoi, int blockSize,
                                       double stdThreshold, double minAreaPx, bool useCuda, bool fast)
            => Find(image, searchRoi, blockSize, stdThreshold, minAreaPx, useCuda, fast, false, out _);

        /// <summary>검출 + (wantResultImage=true 면) std-dev 결과 이미지(검출 사각형·중심·라벨 포함) 반환.
        /// resultImage 는 테스트 프로그램 우측 결과 패널과 동일. 호출측이 Dispose 책임.</summary>
        public static MatchResult Find(Bitmap image, Roi searchRoi, int blockSize,
                                       double stdThreshold, double minAreaPx, bool useCuda, bool fast,
                                       bool wantResultImage, out Bitmap resultImage)
        {
            resultImage = null;
            if (image == null) return MatchResult.Fail(RoiName, "no image");

            Rectangle roi = RoiToRect(searchRoi, image.Width, image.Height);
            if (roi.Width < 4 || roi.Height < 4) return MatchResult.Fail(RoiName, "roi too small");

            int blk = blockSize < 1 ? 1 : blockSize;

            // 원본 그대로: 전체 이미지 기준 std-dev(ROI 안만 흰색), 결과 비트맵은 전체 크기.
            Collet.ComputeBackend backend;
            byte[] mask;
            Bitmap res = Collet.StdDevFilter.Apply(image, roi, blk, stdThreshold, useCuda, out backend, out mask);

            // 정밀(블랍): 연결요소 라벨링 최대 블랍 / 고속(모멘트): 전경 2차모멘트. (원본 chkFast 와 동일)
            Collet.DetectedRect d = fast
                ? Collet.BlobRectFinder.FindByMoments(mask, image.Width, image.Height)
                : Collet.BlobRectFinder.FindLargestRect(mask, image.Width, image.Height);

            bool ok = d.Found && d.Area >= minAreaPx;

            if (wantResultImage)
            {
                if (d.Found) DrawDetectedRect(res, d);   // 결과 이미지에 검출 사각형/중심/라벨
                resultImage = res;
            }
            else res.Dispose();

            if (!ok)
                return MatchResult.Fail(RoiName, d.Found ? "area < minArea" : "no blob");

            double longLen  = Math.Max(d.Width, d.Height);   // 긴 변(=AngleDeg 방향) 길이
            double shortLen = Math.Min(d.Width, d.Height);
            double fill = (d.Width > 0 && d.Height > 0) ? Clamp(d.Area / (d.Width * d.Height), 0.0, 1.0) : 1.0;

            var result = new MatchResult { RoiName = RoiName, Success = true };
            result.Instances.Add(new MatchInstance
            {
                Index    = 0,
                CenterX  = d.Center.X,
                CenterY  = d.Center.Y,
                AngleDeg = d.AngleDeg,    // 긴 변 기준 (-90,90] — 테스트 프로그램과 동일 표기
                Score    = fill,
                BoxW     = longLen,       // 오버레이 박스 재구성(RectCorners)이 AngleDeg 방향과 일치하도록
                BoxH     = shortLen
            });
            return result;
        }

        // ── ROI(중심/크기) → 정수 사각형(이미지 경계 클램프) ─────────────────
        private static Rectangle RoiToRect(Roi roi, int iw, int ih)
        {
            if (roi == null || roi.Width <= 0 || roi.Height <= 0)
                return new Rectangle(0, 0, iw, ih);
            int rw = (int)Math.Round(roi.Width);
            int rh = (int)Math.Round(roi.Height);
            int x  = (int)Math.Round(roi.CenterX - rw / 2.0);
            int y  = (int)Math.Round(roi.CenterY - rh / 2.0);
            var r = Rectangle.Intersect(new Rectangle(x, y, rw, rh), new Rectangle(0, 0, iw, ih));
            return (r.Width <= 0 || r.Height <= 0) ? new Rectangle(0, 0, iw, ih) : r;
        }

        // ── 결과 비트맵에 검출 사각형(초록)+코너 점+중심 십자+노란 라벨(원본 DrawDetection 동일) ──
        private static void DrawDetectedRect(Bitmap bmp, Collet.DetectedRect d)
        {
            if (!d.Found || d.Corners == null) return;
            float lw = Math.Max(1.5f, bmp.Width / 500f);
            float cs = Math.Max(6f, bmp.Width / 100f);
            float fontPx = Math.Max(13f, bmp.Width / 45f);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.Lime, lw)) g.DrawPolygon(pen, d.Corners);
                using (var brush = new SolidBrush(Color.Yellow))
                    foreach (var c in d.Corners) g.FillEllipse(brush, c.X - lw * 1.5f, c.Y - lw * 1.5f, lw * 3f, lw * 3f);
                using (var bc = new SolidBrush(Color.Red))
                    g.FillEllipse(bc, d.Center.X - lw * 1.5f, d.Center.Y - lw * 1.5f, lw * 3f, lw * 3f);
                using (var pc = new Pen(Color.Red, lw))
                {
                    g.DrawLine(pc, d.Center.X - cs, d.Center.Y, d.Center.X + cs, d.Center.Y);
                    g.DrawLine(pc, d.Center.X, d.Center.Y - cs, d.Center.X, d.Center.Y + cs);
                }
                string label = "중심(" + d.Center.X.ToString("0") + ", " + d.Center.Y.ToString("0") +
                               ")  각도 " + d.AngleDeg.ToString("0.0") + "°";
                using (var font = new Font("맑은 고딕", fontPx, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    var sz = g.MeasureString(label, font);
                    float tx = d.Center.X - sz.Width / 2f, ty = d.Center.Y + cs + 4;
                    if (tx < 2) tx = 2;
                    if (tx + sz.Width > bmp.Width - 2) tx = bmp.Width - 2 - sz.Width;
                    if (ty + sz.Height > bmp.Height - 2) ty = d.Center.Y - sz.Height - 8;
                    if (ty < 2) ty = 2;
                    using (var bg = new SolidBrush(Color.FromArgb(170, 0, 0, 0))) g.FillRectangle(bg, tx - 3, ty - 2, sz.Width + 6, sz.Height + 4);
                    using (var fg = new SolidBrush(Color.Yellow)) g.DrawString(label, font, fg, tx, ty);
                }
            }
        }

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
