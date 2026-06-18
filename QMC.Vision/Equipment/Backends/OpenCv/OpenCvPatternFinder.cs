using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.OpenCv
{
    /// <summary>
    /// OpenCV Pattern Finder — 그레이스케일 정규화상관(NCC, CCoeff-normed 계열) 템플릿 매칭.
    /// CDT-310 OpenCVTemplateMatchingVisionTool(Cv2.MatchTemplate + MinMaxLoc + 중복제거)와 동일 계열을
    /// EmguCV 없이 순수 C#(System.Drawing)으로 구현 → LOAD 이미지로 오프라인 테스트 가능.
    /// 점수는 0~1 정규화라 AcceptThreshold 와 직접 비교. 회전은 미지원(translation) — 회전은 추후 EmguCV/PatMax 단계.
    /// </summary>
    public class OpenCvPatternFinder : IPatternFinder
    {
        public string Id { get; }
        public Roi SearchRoi { get; set; }
        public Roi TrainRoi  { get; set; }
        public Bitmap TrainImage { get; private set; }
        public double AcceptThreshold { get; set; } = 0.7;
        public int    MaxInstances    { get; set; } = 1;

        private readonly OpenCvBackend _be;

        public OpenCvPatternFinder(string id, OpenCvBackend be)
        {
            Id = id; _be = be;
            SearchRoi = new Roi { Name = id + ".Search", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
            TrainRoi  = new Roi { Name = id + ".Train",  CenterX = 320, CenterY = 240, Width = 100, Height = 100 };
        }

        public void Train(Bitmap image)
        {
            if (image == null) return;
            var rect = TrainRoi.BoundingBox;
            rect.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            TrainImage?.Dispose();
            TrainImage = image.Clone(rect, image.PixelFormat);
        }

        public void LoadTrainImage(Bitmap pattern)
        {
            TrainImage?.Dispose();
            TrainImage = pattern != null ? (Bitmap)pattern.Clone() : null;   // null = 학습 패턴 제거
        }

        public MatchResult Match(Bitmap image)
        {
            if (image == null || TrainImage == null)
                return MatchResult.Fail(Id, "no image or train");
            try
            {
                // EmguCV 로드 시 실제 CvInvoke.MatchTemplate 사용. 실패하면 순수 C# NCC 로 폴백.
                if (_be != null && _be.EmguLoaded)
                {
                    var em = EmguMatch(image);
                    if (em != null) return em;
                }
                return NccMatch(image);
            }
            catch (Exception ex)
            {
                return MatchResult.Fail(Id, "match error: " + ex.Message);
            }
        }

        // ── EmguCV(CvInvoke.MatchTemplate) 매칭 — 전역 최고 1개. 실패 시 null 반환(폴백 유도). ──
        private MatchResult EmguMatch(Bitmap src)
        {
            var io = OpenCvInterop.Instance;
            if (!io.Ready) return null;

            Rectangle sr = (SearchRoi != null) ? SearchRoi.BoundingBox : new Rectangle(0, 0, src.Width, src.Height);
            sr.Intersect(new Rectangle(0, 0, src.Width, src.Height));
            if (sr.Width <= 0 || sr.Height <= 0) return null;

            int sw, sh, tw, th;
            byte[] S = ToGray(src, sr, out sw, out sh);
            byte[] T = ToGray(TrainImage, new Rectangle(0, 0, TrainImage.Width, TrainImage.Height), out tw, out th);
            if (tw < 2 || th < 2 || sw < tw || sh < th) return null;

            double score; int locX, locY;
            if (!io.TryMatch(S, sw, sh, T, tw, th, out score, out locX, out locY)) return null;

            var res = new MatchResult { RoiName = Id, Success = true };
            res.Instances.Add(new MatchInstance
            {
                Index    = 0,
                CenterX  = sr.X + locX + tw / 2.0,   // CDT-310: loc + tpl/2
                CenterY  = sr.Y + locY + th / 2.0,
                AngleDeg = 0,
                Score    = Math.Max(0.0, Math.Min(1.0, score))
            });
            return res;
        }

        // ── 그레이스케일 NCC 매칭 ──
        private MatchResult NccMatch(Bitmap src)
        {
            Rectangle sr = (SearchRoi != null) ? SearchRoi.BoundingBox : new Rectangle(0, 0, src.Width, src.Height);
            sr.Intersect(new Rectangle(0, 0, src.Width, src.Height));
            if (sr.Width <= 0 || sr.Height <= 0) return MatchResult.Fail(Id, "invalid search ROI");

            int sw, sh, tw, th;
            byte[] S = ToGray(src, sr, out sw, out sh);
            byte[] T = ToGray(TrainImage, new Rectangle(0, 0, TrainImage.Width, TrainImage.Height), out tw, out th);
            if (tw < 2 || th < 2) return MatchResult.Fail(Id, "template too small");
            if (sw < tw || sh < th) return MatchResult.Fail(Id, "search ROI smaller than template");

            // 템플릿 서브샘플(속도) — NCC 는 정규화라 부분표본으로도 상대 점수 유효.
            int tstep = Math.Max(1, Math.Min(tw, th) / 40);
            var tx = new List<int>(); var ty = new List<int>(); var tv = new List<double>();
            double tSum = 0, tSS = 0;
            for (int yy = 0; yy < th; yy += tstep)
                for (int xx = 0; xx < tw; xx += tstep)
                {
                    double v = T[yy * tw + xx];
                    tx.Add(xx); ty.Add(yy); tv.Add(v);
                    tSum += v; tSS += v * v;
                }
            int n = tv.Count;
            double tMean = tSum / n;
            double tVar = tSS - tSum * tSum / n;     // Σ(t-t̄)²
            if (tVar < 1e-6) return MatchResult.Fail(Id, "template has no contrast");

            int maxX = sw - tw, maxY = sh - th;
            int posStep = Math.Max(1, Math.Min(tw, th) / 20);

            // 위치별 NCC (지역 함수)
            double Ncc(int x, int y)
            {
                double s = 0, ss = 0, st = 0;
                for (int k = 0; k < n; k++)
                {
                    double sv = S[(y + ty[k]) * sw + (x + tx[k])];
                    s += sv; ss += sv * sv; st += sv * tv[k];
                }
                double num = st - s * tMean;
                double den = Math.Sqrt(Math.Max(1e-9, (ss - s * s / n)) * tVar);
                return num / den;          // -1 ~ 1
            }

            // 코어스 스캔 — 후보 수집 + 전역 최고 추적
            var cand = new List<MatchInstance>();
            double bestScore = -2; int bestX = 0, bestY = 0;
            for (int y = 0; y <= maxY; y += posStep)
                for (int x = 0; x <= maxX; x += posStep)
                {
                    double ncc = Ncc(x, y);
                    if (ncc > bestScore) { bestScore = ncc; bestX = x; bestY = y; }
                    if (ncc >= AcceptThreshold)
                        cand.Add(new MatchInstance { CenterX = x, CenterY = y, Score = ncc });
                }

            // 임계 통과 후보가 없으면 전역 최고 1개라도 반환(점수로 합부 판단).
            if (cand.Count == 0)
                cand.Add(new MatchInstance { CenterX = bestX, CenterY = bestY, Score = bestScore });

            // 점수 내림차순 + NMS(중복제거) — CDT-310 ConfirmDuplication 대응
            cand.Sort((a, b) => b.Score.CompareTo(a.Score));
            double minDist = Math.Max(tw, th) * 0.5;
            var picked = new List<MatchInstance>();
            foreach (var c in cand)
            {
                bool far = true;
                foreach (var p in picked)
                    if (Math.Abs(p.CenterX - c.CenterX) < minDist && Math.Abs(p.CenterY - c.CenterY) < minDist) { far = false; break; }
                if (far) picked.Add(c);
                if (picked.Count >= Math.Max(1, MaxInstances)) break;
            }

            // 각 채택 위치 정밀화(±posStep, step 1) + 중심/점수 확정
            var res = new MatchResult { RoiName = Id, Success = true };
            int idx = 0;
            foreach (var p in picked)
            {
                int cx = (int)p.CenterX, cy = (int)p.CenterY;
                double bScore = p.Score; int bx = cx, by = cy;
                int x0 = Math.Max(0, cx - posStep), x1 = Math.Min(maxX, cx + posStep);
                int y0 = Math.Max(0, cy - posStep), y1 = Math.Min(maxY, cy + posStep);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        double v = Ncc(x, y);
                        if (v > bScore) { bScore = v; bx = x; by = y; }
                    }
                res.Instances.Add(new MatchInstance
                {
                    Index    = idx++,
                    CenterX  = sr.X + bx + tw / 2.0,   // CDT-310: loc + tpl/2
                    CenterY  = sr.Y + by + th / 2.0,
                    AngleDeg = 0,
                    Score    = Math.Max(0.0, Math.Min(1.0, bScore))
                });
            }
            return res;
        }

        /// <summary>비트맵의 rect 영역을 8bit 그레이 배열로 변환(24bpp 로 락 후 평균).</summary>
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
