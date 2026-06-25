using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace QMC.Vision.Core
{
    /// <summary>
    /// CDT-310 측면 칩핑 알고리즘(CUDA 비의존 경로) 포팅.
    /// 원본: <c>FindLine.FindTopLineOfChip</c>(에지 라인 강건 피팅) + <c>SideChippingInspector</c>(라인 기준 칩핑 깊이 측정).
    /// CUDA 커널(GetTopBottomLineCandidates)은 동일 로직의 CPU 스캔으로 대체(FindLine.cs와 동일).
    /// 입력은 ROI 그레이 버퍼(byte[w*h]). 좌표는 ROI 로컬.
    /// </summary>
    internal static class SideChippingCore
    {
        /// <summary>2D 직선 y = mA*x + mB (수직이면 mA=+Inf, mB=x절편). 310 Line 호환 최소 구현.</summary>
        internal sealed class Line
        {
            public double mA, mB;
            public Line(double a, double b) { mA = a; mB = b; }
            public double GetY(double x) => mA * x + mB;
            /// <summary>라인 기울기 각도[deg](수평=0).</summary>
            public double GetAngle() => Math.Atan(mA) * 180.0 / Math.PI;
        }

        public sealed class Result
        {
            public bool   Valid;
            public double TopMm, BottomMm, MaxMm;     // 상/하/최대 칩핑[mm]
            public int    TopBaseY, BottomBaseY;       // (회전 후 좌표계의) 상/하 기준 라인 y
            public int    XStart, XEnd;                // 칩 가로 유효 범위
            public int    MaxX, MaxY;                  // 최대 칩핑 위치(오버레이용, ROI 로컬)
            public List<PointF> Contour = new List<PointF>();  // 최대 칩핑 영역 외곽(310 ChippingInfo.Contour)
        }

        /// <summary>
        /// 칩핑 검사. threshold=칩(밝은 띠)/배경 분리, chipThicknessMm=칩 두께, pxW/pxH=픽셀크기[mm].
        /// 반환값의 TopMm/BottomMm/MaxMm 이 핵심 결과. (NG 판정은 호출측 스펙으로)
        /// </summary>
        public static Result Inspect(byte[] gray, int w, int h, int threshold,
                                     double chipThicknessMm, double pxW, double pxH)
        {
            var res = new Result();
            if (gray == null || w < 8 || h < 8) return res;

            // 1) 칩 상단 라인 검출(강건 피팅)
            Line topLine = FindTopLineOfChip(w, h, gray, threshold, 0.9);
            if (topLine == null) return res;

            // 2) 라인 각도로 이미지 회전(칩을 수평으로) — 310 RotateImage(bilinear)
            double angle = topLine.GetAngle();
            byte[] rot = RotateImage(gray, w, h, angle);

            // 3) 회전 이미지에서 다시 상단 라인
            Line rTop = FindTopLineOfChip(w, h, rot, threshold, 0.9);
            if (rTop == null) return res;

            // 4) 하단 라인 — 실제 하단 에지를 독립 검출(상단과 동일 강건 피팅). 실패 시에만 상단+칩두께 오프셋 폴백.
            //    (310은 오프셋만 썼으나, 실제 칩 두께/배율이 ChipThickness와 어긋나면 바닥을 못 잡아 독립검출로 개선)
            double dOffset = pxW > 0 ? (chipThicknessMm / pxW) : (h * 0.5);
            Line botLine = FindBottomLineOfChip(w, h, rot, threshold);
            bool botDetected = botLine != null && botLine.GetY(w / 2.0) > rTop.GetY(w / 2.0) + 4;
            if (!botDetected) botLine = new Line(rTop.mA, rTop.mB + dOffset);   // 폴백

            // 5) 컬럼별 실제 에지 프로파일(첫 밝은 픽셀) + 칩 가로 유효 범위
            var topProf = new int[w]; var botProf = new int[w];
            int xs = -1, xe = -1;
            for (int x = 0; x < w; x++)
            {
                int t = -1, b = -1;
                for (int y = 0; y < h; y++)       if (rot[y * w + x] >= threshold) { t = y; break; }
                for (int y = h - 1; y >= 0; y--)  if (rot[y * w + x] >= threshold) { b = y; break; }
                topProf[x] = t; botProf[x] = b;
                if (t >= 0) { if (xs < 0) xs = x; xe = x; }
            }
            if (xs < 0) return res;

            // 6) 칩핑 깊이 = 기준선 대비 실제 에지 프로파일의 편차(상=라인보다 아래로 파임, 하=위로 파임).
            //    깊은 노치/관통 칩아웃도 그대로 측정되고, 표면 이물은 에지에 영향 없어 자동 제외. (양끝 15개 제외=310)
            const int trim = 15;
            int x0 = Math.Min(xs + trim, xe), x1 = Math.Max(xe - trim, xs);
            double topMaxPx = 0, botMaxPx = 0;
            int topMaxX = -1, topMaxY = -1, botMaxX = -1, botMaxY = -1;
            for (int x = x0; x <= x1; x++)
            {
                if (topProf[x] >= 0)
                {
                    double dev = topProf[x] - rTop.GetY(x);              // +면 상단 칩핑
                    if (dev > topMaxPx) { topMaxPx = dev; topMaxX = x; topMaxY = topProf[x]; }
                }
                if (botProf[x] >= 0)
                {
                    double dev = botLine.GetY(x) - botProf[x];          // +면 하단 칩핑
                    if (dev > botMaxPx) { botMaxPx = dev; botMaxX = x; botMaxY = botProf[x]; }
                }
            }
            double topMm = topMaxPx * pxH, botMm = botMaxPx * pxH;

            res.Valid     = true;
            res.TopMm     = topMm;
            res.BottomMm  = botMm;
            res.MaxMm     = Math.Max(topMm, botMm);
            res.TopBaseY  = (int)rTop.GetY(w / 2.0);
            res.BottomBaseY = (int)botLine.GetY(w / 2.0);
            // NG 마커 위치 — 상/하 중 더 깊은 쪽의 실제 결손 지점
            if (botMm >= topMm && botMaxX >= 0) { res.MaxX = botMaxX; res.MaxY = botMaxY; }
            else if (topMaxX >= 0)              { res.MaxX = topMaxX; res.MaxY = topMaxY; }
            res.XStart = xs;
            res.XEnd   = xe;
            return res;
        }

        // ── 310 RotateImage (bilinear, 크기 유지) ──
        private static byte[] RotateImage(byte[] src, int w, int h, double angleDeg)
        {
            if (Math.Abs(angleDeg) < 1e-4) return src;
            var dst = new byte[w * h];
            double rad = angleDeg * Math.PI / 180.0;
            double cx = w / 2.0, cy = h / 2.0;
            double cos = Math.Cos(rad), sin = Math.Sin(rad);
            for (int ny = 0; ny < h; ny++)
            {
                for (int nx = 0; nx < w; nx++)
                {
                    double rx = nx - cx, ry = ny - cy;
                    double ox = rx * cos - ry * sin + cx;
                    double oy = rx * sin + ry * cos + cy;
                    if (ox >= 0 && ox < w - 1 && oy >= 0 && oy < h - 1)
                    {
                        int x1 = (int)ox, y1 = (int)oy, x2 = x1 + 1, y2 = y1 + 1;
                        double fx = ox - x1, fy = oy - y1;
                        double val = (1 - fx) * (1 - fy) * src[y1 * w + x1]
                                   + fx * (1 - fy) * src[y1 * w + x2]
                                   + (1 - fx) * fy * src[y2 * w + x1]
                                   + fx * fy * src[y2 * w + x2];
                        dst[ny * w + nx] = (byte)Math.Round(val);
                    }
                }
            }
            return dst;
        }

        // ── 310 FindLine.FindTopLineOfChip (CPU, envelope + quantile trim + trend line) ──
        private const int EnvelopeBinSize = 6;
        private const double TopKeepQuantile = 0.35;

        private static Line FindTopLineOfChip(int w, int h, byte[] image, int threshold, double scanRate)
        {
            if (scanRate < 0.3) scanRate = 0.3;
            if (scanRate > 1) scanRate = 0.99;

            var pts = new List<PointF>();
            int start = Math.Max(2, 10);
            int end = Math.Min(h - 3, (int)(h * scanRate) - 10);
            for (int x = 0; x < w; x++)
            {
                for (int y = start; y <= end; y++)
                {
                    if (image[y * w + x] > threshold &&
                        image[(y - 1) * w + x] <= threshold &&
                        image[(y - 2) * w + x] <= threshold)
                    {
                        pts.Add(new PointF(x, y));
                        break;  // 첫 교차 = 바깥 에지
                    }
                }
            }
            if (pts.Count < 2) return null;
            pts = pts.OrderBy(t => t.X).ToList();

            var env = KeepOuterPerBin(pts, EnvelopeBinSize, takeMinY: true);
            var listPoint = (env != null && env.Count >= 2) ? env : pts;

            var trimmed = ReMoveTop(listPoint);
            if (trimmed == null || trimmed.Count < 2) trimmed = pts;

            double dA, dB;
            GetTrendLine(trimmed, out dA, out dB);
            return new Line(dA, dB);
        }

        // ── 310 FindLine.FindBottomLineOfChip (CPU, 하단 바깥 에지 강건 피팅) ──
        private static Line FindBottomLineOfChip(int w, int h, byte[] image, int threshold)
        {
            var pts = new List<PointF>();
            int start = Math.Min(h - 3, h - 10);
            int end = Math.Max(2, (int)(h * 0.5) + 10);
            for (int x = 0; x < w; x++)
            {
                for (int y = start; y >= end; y--)
                {
                    if (image[y * w + x] > threshold &&
                        image[(y + 1) * w + x] <= threshold &&
                        image[(y + 2) * w + x] <= threshold)
                    {
                        pts.Add(new PointF(x, y));
                        break;  // 아래에서 첫 교차 = 바깥 하단 에지
                    }
                }
            }
            if (pts.Count < 2) return null;
            pts = pts.OrderBy(t => t.X).ToList();

            var env = KeepOuterPerBin(pts, EnvelopeBinSize, takeMinY: false);   // 하단 = 가장 아래(maxY)
            var listPoint = (env != null && env.Count >= 2) ? env : pts;

            var trimmed = ReMoveBottom(listPoint);
            if (trimmed == null || trimmed.Count < 2) trimmed = pts;

            double dA, dB;
            GetTrendLine(trimmed, out dA, out dB);
            return new Line(dA, dB);
        }

        // ReMoveTop 의 하단 버전(잔차 상위 분위 유지 = 바깥쪽 아래 에지로 수렴)
        private static List<PointF> ReMoveBottom(List<PointF> listPoint)
        {
            var temp = listPoint?.ToList() ?? new List<PointF>();
            if (temp.Count < 2) return temp;

            double dA, dB;
            GetTrendLine(temp, out dA, out dB);
            var residuals = temp.Select(p => p.Y - (dA * p.X + dB)).ToList();
            double cut = Percentile(residuals, 0.98);
            temp = temp.Where(p => (p.Y - (dA * p.X + dB)) <= cut).ToList();

            for (int iter = 0; iter < 3; iter++)
            {
                if (temp.Count < 2) break;
                GetTrendLine(temp, out dA, out dB);
                residuals = temp.Select(p => p.Y - (dA * p.X + dB)).ToList();
                cut = Percentile(residuals, 1.0 - TopKeepQuantile);
                var next = temp.Where(p => (p.Y - (dA * p.X + dB)) >= cut).ToList();
                if (next.Count < 2) break;
                temp = next;
            }
            return temp;
        }

        private static List<PointF> ReMoveTop(List<PointF> listPoint)
        {
            var temp = listPoint?.ToList() ?? new List<PointF>();
            if (temp.Count < 2) return temp;

            double dA, dB;
            GetTrendLine(temp, out dA, out dB);
            var residuals = temp.Select(p => p.Y - (dA * p.X + dB)).ToList();
            double cut = Percentile(residuals, 0.02);
            temp = temp.Where(p => (p.Y - (dA * p.X + dB)) >= cut).ToList();

            for (int iter = 0; iter < 3; iter++)
            {
                if (temp.Count < 2) break;
                GetTrendLine(temp, out dA, out dB);
                residuals = temp.Select(p => p.Y - (dA * p.X + dB)).ToList();
                cut = Percentile(residuals, TopKeepQuantile);
                var next = temp.Where(p => (p.Y - (dA * p.X + dB)) <= cut).ToList();
                if (next.Count < 2) break;
                temp = next;
            }
            return temp;
        }

        private static List<PointF> KeepOuterPerBin(List<PointF> points, int binSize, bool takeMinY)
        {
            if (points == null || points.Count == 0) return new List<PointF>();
            if (binSize < 1) binSize = 1;
            var result = new List<PointF>();
            foreach (var g in points.GroupBy(p => (int)(p.X / binSize)))
                result.Add(takeMinY ? g.OrderBy(v => v.Y).First() : g.OrderByDescending(v => v.Y).First());
            result.Sort((a, b) => a.X.CompareTo(b.X));
            return result;
        }

        private static double Percentile(List<double> values, double p)
        {
            if (values == null || values.Count == 0) return 0;
            if (p <= 0) return values.Min();
            if (p >= 1) return values.Max();
            var sorted = values.OrderBy(v => v).ToList();
            double pos = p * (sorted.Count - 1);
            int idx = (int)Math.Floor(pos);
            double frac = pos - idx;
            return idx + 1 < sorted.Count ? sorted[idx] * (1 - frac) + sorted[idx + 1] * frac : sorted[idx];
        }

        // 310 GetTrendLine (수직 보정 포함, RemovePoint 미적용 = bRemvePoint:false 경로)
        private static void GetTrendLine(List<PointF> listPoint, out double dA, out double dB)
        {
            dA = 0; dB = 0;
            var pts = new List<PointF>(listPoint);
            if (pts.Count < 2) return;

            double xrange = pts.Max(t => t.X) - pts.Min(t => t.X);
            double yrange = pts.Max(t => t.Y) - pts.Min(t => t.Y);
            if (xrange == 0 && yrange == 0) return;

            bool vertical = false;
            if (xrange < yrange)
            {
                for (int i = 0; i < pts.Count; i++) pts[i] = new PointF(pts[i].Y, pts[i].X);
                vertical = true;
            }

            int n = pts.Count;
            double sx = 0, sy = 0, sxx = 0, sxy = 0;
            foreach (var v in pts) { sx += v.X; sy += v.Y; sxx += v.X * v.X; sxy += v.X * v.Y; }
            double den = n * sxx - sx * sx;
            if (Math.Abs(den) < 1e-9)
            {
                if (vertical) { dA = double.PositiveInfinity; dB = pts.Average(p => p.Y); }
                else { dA = 0; dB = pts.Average(p => p.Y); }
                return;
            }
            dA = (n * sxy - sx * sy) / den;
            dB = (sxx * sy - sx * sxy) / den;
            if (vertical)
            {
                double a = dA, b = dB;
                if (Math.Abs(a) < 1e-9) { dA = double.PositiveInfinity; dB = pts.Average(p => p.Y); }
                else { dA = 1 / a; dB = -b / a; }
            }
        }
    }
}
