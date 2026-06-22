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
    /// 점수는 0~1 정규화라 AcceptThreshold 와 직접 비교. 위치는 평행이동 NCC.
    /// 회전각(AngleDeg)은 매칭 위치 패치의 그라디언트(AlignAngleEstimator, Sobel+4θ)로 추정한다
    /// — 회전 템플릿 탐색 없이 격자/다이 틸트각을 산출(정밀 회전매칭은 추후 EmguCV/PatMax 단계).
    /// </summary>
    public class OpenCvPatternFinder : IPatternFinder
    {
        public string Id { get; }
        public Roi SearchRoi { get; set; }
        public Roi TrainRoi  { get; set; }
        public Bitmap TrainImage { get; private set; }
        public double AcceptThreshold { get; set; } = 0.7;
        public int    MaxInstances    { get; set; } = 1;
        public bool   AngleEnabled      { get; set; } = false;
        public double AngleToleranceDeg { get; set; } = 10.0;
        public double AngleStepDeg      { get; set; } = 1.0;
        public bool   PreferNearestCenter { get; set; } = false;

        /// <summary>각도 동점 판정 epsilon — 두 각의 NCC 차이가 이 값 이내면 0°에 가까운 각을 선호(무회전 지터 억제).</summary>
        private const double AngleTieEps = 2e-3;

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
            // ICloneable.Clone() 은 소스 픽셀을 지연 공유 → 로드 스트림 해제 후 "GDI+ 일반 오류".
            // new Bitmap(src) 로 즉시 깊은 복사하여 소스와 완전히 독립시킨다(재시작 복원 패턴 안정).
            TrainImage = pattern != null ? new Bitmap(pattern) : null;   // null = 학습 패턴 제거
        }

        public MatchResult Match(Bitmap image)
        {
            if (image == null || TrainImage == null)
                return MatchResult.Fail(Id, "no image or train");
            try
            {
                // 회전 탐색이 필요하면 EmguCV(평행이동 전용) 경로를 건너뛰고 회전 지원 NCC 를 사용.
                bool needAngle = AngleEnabled && AngleToleranceDeg > 0.0;
                if (!needAngle && _be != null && _be.EmguLoaded)
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
                AngleDeg = 0,                        // EmguCV 경로는 평행이동 전용(회전은 NccMatch)
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
            if (tw < 2 || th < 2) return MatchResult.Fail(Id, "template too small (" + tw + "x" + th + ")");
            if (sw < tw || sh < th) return MatchResult.Fail(Id,
                "search " + sw + "x" + sh + " < template " + tw + "x" + th +
                " (img " + src.Width + "x" + src.Height + ", roi @" + sr.X + "," + sr.Y + " " + sr.Width + "x" + sr.Height + ")");

            // 템플릿 서브샘플(속도) — 중심 기준 오프셋(ox,oy)으로 보관해 회전 샘플링에 사용.
            int tstep = Math.Max(1, Math.Min(tw, th) / 40);
            double tcx = tw / 2.0, tcy = th / 2.0;
            var ox = new List<double>(); var oy = new List<double>(); var tv = new List<double>();
            double tSum = 0, tSS = 0;
            for (int yy = 0; yy < th; yy += tstep)
                for (int xx = 0; xx < tw; xx += tstep)
                {
                    double v = T[yy * tw + xx];
                    ox.Add(xx - tcx); oy.Add(yy - tcy); tv.Add(v);
                    tSum += v; tSS += v * v;
                }
            int n = tv.Count;
            double tMean = tSum / n;
            double tVar = tSS - tSum * tSum / n;     // Σ(t-t̄)²
            if (tVar < 1e-6) return MatchResult.Fail(Id, "template has no contrast");

            // 회전 샘플 좌표 버퍼(각도마다 갱신).
            var rx = new double[n]; var ry = new double[n];

            // 스캔 중심 범위 = 템플릿(무회전)이 탐색영역에 들어가는 전 영역.
            // 회전 샘플이 경계를 벗어나면 Ncc 에서 클램프 처리 → 큰 회전 마진을 두지 않아
            // 탐색영역 가장자리에 위치한 타깃도 검출된다(마진 과다 제외 버그 수정).
            bool rotate = AngleEnabled && AngleToleranceDeg > 0.0;
            int loX = (int)Math.Ceiling(tcx), hiX = sw - 1 - (int)Math.Ceiling(tw - tcx);
            int loY = (int)Math.Ceiling(tcy), hiY = sh - 1 - (int)Math.Ceiling(th - tcy);
            if (hiX < loX) loX = hiX = sw / 2;   // Search≈Train 이면 중심 1점(평행이동 단일 위치)
            if (hiY < loY) loY = hiY = sh / 2;
            int posStep = Math.Max(1, Math.Min(tw, th) / 20);
            // 탐색 영역이 커도 코어스 스캔 위치 수를 제한(UI 프리즈 방지) — 위치축당 최대 ~180.
            const int posCap = 180;
            posStep = Math.Max(posStep, Math.Max(1, Math.Max(hiX - loX, hiY - loY) / posCap));

            // 각도 측정 범위 — "검색(스캔)은 평행이동, 찾은 위치에서 각도를 측정"한다.
            // AngleEnabled(검색의 회전 허용)와 무관하게 각도를 측정·보고한다. 범위 = AngleToleranceDeg(미지정 시 ±15°).
            double measRange = AngleToleranceDeg > 0.0 ? AngleToleranceDeg : 15.0;
            double aStep = Math.Max(0.2, AngleStepDeg);
            var angles = new List<double>();
            for (double a = -measRange; a <= measRange + 1e-9; a += aStep) angles.Add(a);

            // 현재 각도의 회전 오프셋 갱신(중심 기준).
            void SetRot(double deg)
            {
                double rad = deg * Math.PI / 180.0, c = Math.Cos(rad), s = Math.Sin(rad);
                for (int k = 0; k < n; k++) { rx[k] = ox[k] * c - oy[k] * s; ry[k] = ox[k] * s + oy[k] * c; }
            }

            // 중심(px,py) 위치의 NCC — 현재 회전(rx,ry) 사용.
            double Ncc(int px, int py)
            {
                double sX = 0, ss = 0, st = 0;
                for (int k = 0; k < n; k++)
                {
                    int ix = (int)(px + rx[k] + 0.5), iy = (int)(py + ry[k] + 0.5);
                    if (ix < 0) ix = 0; else if (ix >= sw) ix = sw - 1;
                    if (iy < 0) iy = 0; else if (iy >= sh) iy = sh - 1;
                    double sv = S[iy * sw + ix];
                    sX += sv; ss += sv * sv; st += sv * tv[k];
                }
                double num = st - sX * tMean;
                double den = Math.Sqrt(Math.Max(1e-9, (ss - sX * sX / n)) * tVar);
                return num / den;          // -1 ~ 1
            }

            // ── Phase 1: 각도 0 으로 전 위치 코어스 스캔(회전 시 완화 게이트) → 후보 위치 수집.
            //    회전은 후보 위치에서만 전범위 정밀화(Phase 2)하여, 위치 스캔 비용에서 각도 배수를 제거(프리즈 방지).
            SetRot(0.0);
            double gate = rotate ? Math.Min(AcceptThreshold, 0.45) : AcceptThreshold;
            var cand = new List<MatchInstance>();
            double bestScore = -2; int bestX = loX, bestY = loY;
            for (int y = loY; y <= hiY; y += posStep)
                for (int x = loX; x <= hiX; x += posStep)
                {
                    double ncc = Ncc(x, y);
                    if (ncc > bestScore) { bestScore = ncc; bestX = x; bestY = y; }
                    if (ncc >= gate)
                        cand.Add(new MatchInstance { CenterX = x, CenterY = y, Score = ncc, AngleDeg = 0 });
                }
            if (cand.Count == 0)
                cand.Add(new MatchInstance { CenterX = bestX, CenterY = bestY, Score = bestScore, AngleDeg = 0 });

            // 점수 내림차순 + 위치 NMS → 정밀화할 상위 후보(refineK) 확보.
            cand.Sort((a, b) => b.Score.CompareTo(a.Score));
            double minDist = Math.Max(tw, th) * 0.5;
            int want = Math.Max(1, MaxInstances);
            // 센터 최근접 모드는 더 많은 후보(센터 근처 다이가 최고점이 아닐 수 있음)를 정밀화 대상으로 확보.
            int refineK = PreferNearestCenter ? Math.Max(want, 32) : Math.Max(want, 8);
            var picked = new List<MatchInstance>();
            foreach (var c in cand)
            {
                bool far = true;
                foreach (var pp in picked)
                    if (Math.Abs(pp.CenterX - c.CenterX) < minDist && Math.Abs(pp.CenterY - c.CenterY) < minDist) { far = false; break; }
                if (far) picked.Add(c);
                if (picked.Count >= refineK) break;
            }

            // ── Phase 2: 후보별 각도 전범위(위치 고정) → 위치 ±posStep 정밀화 → 각도 미세 정밀화.
            double fineStep = Math.Max(0.1, AngleStepDeg * 0.5);
            var refined = new List<MatchInstance>();
            foreach (var p in picked)
            {
                int cx = (int)p.CenterX, cy = (int)p.CenterY;
                double bS = -2; int bx = cx, by = cy; double bA = 0;
                foreach (double a in angles)
                {
                    SetRot(a);
                    double v = Ncc(cx, cy);
                    // 동점이면 0°에 가까운 각 선호 — 무회전 패턴이 0°로 보고되도록.
                    if (v > bS + AngleTieEps || (v > bS - AngleTieEps && Math.Abs(a) < Math.Abs(bA)))
                    { bS = v; bA = a; }
                }
                SetRot(bA);
                int x0 = Math.Max(loX, cx - posStep), x1 = Math.Min(hiX, cx + posStep);
                int y0 = Math.Max(loY, cy - posStep), y1 = Math.Min(hiY, cy + posStep);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        double v = Ncc(x, y);
                        if (v > bS) { bS = v; bx = x; by = y; }
                    }
                if (angles.Count > 1)
                {
                    for (double a = bA - AngleStepDeg; a <= bA + AngleStepDeg + 1e-9; a += fineStep)
                    {
                        SetRot(a);
                        double v = Ncc(bx, by);
                        if (v > bS + AngleTieEps || (v > bS - AngleTieEps && Math.Abs(a) < Math.Abs(bA)))
                        { bS = v; bA = a; }
                    }
                }
                refined.Add(new MatchInstance
                {
                    CenterX  = sr.X + bx,            // px 는 이미 패턴 중심
                    CenterY  = sr.Y + by,
                    AngleDeg = bA,                   // Train 대비 회전각(deg)
                    Score    = Math.Max(0.0, Math.Min(1.0, bS))
                });
            }

            var res = new MatchResult { RoiName = Id, Success = true };

            // 센터 최근접: 임계 통과 후보 중 이미지 센터에 가장 가까운 1개 선택(웨이퍼 정렬).
            if (PreferNearestCenter && refined.Count > 0)
            {
                double ccx = src.Width / 2.0, ccy = src.Height / 2.0;
                MatchInstance pick = null; double bestD = double.MaxValue;
                foreach (var m in refined)
                {
                    if (AcceptThreshold > 0.0 && m.Score < AcceptThreshold) continue;
                    double dx = m.CenterX - ccx, dy = m.CenterY - ccy, d = dx * dx + dy * dy;
                    if (d < bestD) { bestD = d; pick = m; }
                }
                if (pick == null) { refined.Sort((a, b) => b.Score.CompareTo(a.Score)); pick = refined[0]; }
                pick.Index = 0;
                res.Instances.Add(pick);
                return res;
            }

            // 기본: 정밀 점수 내림차순 → 상위 want 개 채택(정밀화로 정확 일치 패턴이 평민 후보를 역전).
            refined.Sort((a, b) => b.Score.CompareTo(a.Score));
            for (int i = 0; i < refined.Count && i < want; i++)
            {
                var m = refined[i];
                m.Index = i;
                res.Instances.Add(m);
            }
            return res;
        }

        /// <summary>비트맵의 rect 영역을 8bit 그레이 배열로 변환.
        /// 8bpp 인덱스(그레이 PNG)·CMYK 등 비표준 포맷에서 LockBits(24bpp) 가 "GDI+ 일반 오류"를 내므로,
        /// 32bppArgb 버퍼에 DrawImage 로 정규화(모든 소스 포맷 허용)한 뒤 락한다.</summary>
        private static byte[] ToGray(Bitmap bmp, Rectangle rect, out int w, out int h)
        {
            rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
            w = rect.Width; h = rect.Height;
            var gray = new byte[w * h];
            if (w <= 0 || h <= 0) return gray;

            using (var norm = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(norm))
                {
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.DrawImage(bmp, new Rectangle(0, 0, w, h), rect, GraphicsUnit.Pixel);
                }
                BitmapData data = norm.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
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
                            int o = x * 4;   // BGRA — A(=row[o+3]) 제외하고 BGR 평균
                            gray[gi + x] = (byte)((row[o] + row[o + 1] + row[o + 2]) / 3);
                        }
                    }
                }
                finally { norm.UnlockBits(data); }
            }
            return gray;
        }
    }
}
