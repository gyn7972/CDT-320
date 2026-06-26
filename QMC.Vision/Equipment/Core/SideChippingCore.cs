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
        /// <summary>칩핑 검사 파라미터 — 310 SideInspectionParameter/FindLine 조정값(레시피 노출).</summary>
        public sealed class Params
        {
            public int    Threshold       = 60;        // 칩/배경 분리(310 ChippingThreshold)
            public double ChipThicknessMm = 0.25;      // 하단 오프셋 폴백
            public double PxW = 0.003125, PxH = 0.003125;
            public double ScanRate        = 0.9;       // 310 FindTopLineOfChip dMagin
            public int    EnvelopeBinSize = 6;         // 310 FindLine.EnvelopeBinSize
            public double KeepQuantile    = 0.35;      // 310 FindLine.TopKeepQuantile
            public int    EdgeGap         = 6;         // 칩핑 에지결합 허용[px]
        }

        public static Result Inspect(byte[] gray, int w, int h, Params p)
        {
            var res = new Result();
            if (gray == null || w < 8 || h < 8 || p == null) return res;
            int threshold = p.Threshold; double pxW = p.PxW, pxH = p.PxH;

            // 1) 칩 상단 라인 검출(강건 피팅)
            Line topLine = FindTopLineOfChip(w, h, gray, threshold, p.ScanRate, p.EnvelopeBinSize, p.KeepQuantile);
            if (topLine == null) return res;

            // 2) 라인 각도로 이미지 회전(칩을 수평으로) — 310 RotateImage(bilinear)
            double angle = topLine.GetAngle();
            byte[] rot = RotateImage(gray, w, h, angle);

            // 3) 회전 이미지에서 다시 상단 라인
            Line rTop = FindTopLineOfChip(w, h, rot, threshold, p.ScanRate, p.EnvelopeBinSize, p.KeepQuantile);
            if (rTop == null) return res;

            // 4) 하단 라인 — 310 FindLine.FindBottomLineOfChip 로 독립 검출(310 코드). 실패 시 상단+칩두께 오프셋(310 SideChippingInspector) 폴백.
            double dOffset = pxW > 0 ? (p.ChipThicknessMm / pxW) : (h * 0.5);
            Line botLine = FindBottomLineOfChip(w, h, rot, threshold, p.EnvelopeBinSize, p.KeepQuantile);
            if (botLine == null || botLine.GetY(w / 2.0) <= rTop.GetY(w / 2.0) + 4)
                botLine = new Line(rTop.mA, rTop.mB + dOffset);   // 폴백

            // 5) 칩핑 스캔 깊이(마진) — 상단~하단 라인 간격(칩 두께)
            int margin = (int)Math.Max(8, botLine.GetY(w / 2.0) - rTop.GetY(w / 2.0));

            // 6) 상/하 칩핑 — CDT-310 SideChippingInspector dark-scan (라인 기준 어두운 결손, 양끝 15개 제외 max)
            int topMaxX, topMaxY;
            double topMm = InspectTopChipping(rot, w, h, rTop, botLine, threshold, margin, pxH, p.EdgeGap, out topMaxX, out topMaxY);
            var info = InspectBottomChipping(rot, w, h, rTop, botLine, threshold, margin, pxH, p.EdgeGap);
            double botMm = info.Depth;

            // 칩 가로 유효 범위(밝은 띠가 존재하는 x)
            int xs = -1, xe = -1;
            for (int x = 0; x < w; x++)
            {
                int ty = (int)rTop.GetY(x);
                if (ty >= 0 && ty < h && rot[ty * w + x] >= threshold) { if (xs < 0) xs = x; xe = x; }
            }

            res.Valid     = true;
            res.TopMm     = topMm;
            res.BottomMm  = botMm;
            res.MaxMm     = Math.Max(topMm, botMm);
            res.TopBaseY  = (int)rTop.GetY(w / 2.0);
            res.BottomBaseY = (int)botLine.GetY(w / 2.0);
            res.Contour   = info.Contour;
            // NG 마커 위치 — 상/하 중 더 깊은 쪽 결손 지점
            if (botMm >= topMm && info.Contour != null && info.Contour.Count > 0)
            { res.MaxX = (int)info.Contour.Average(pt => pt.X); res.MaxY = (int)info.Contour.Average(pt => pt.Y); }
            else if (topMm > 0 && topMaxX >= 0) { res.MaxX = topMaxX; res.MaxY = topMaxY; }
            res.XStart = xs < 0 ? 0 : xs;
            res.XEnd   = xe < 0 ? w - 1 : xe;
            return res;
        }

        // ── CDT-310 SideChippingInspector.InspectTopChipping (dark-scan, 양끝 15 제외 max, 최대위치 회수) ──
        private static double InspectTopChipping(byte[] image, int w, int h, Line topLine, Line bottomLine,
                                                 int threshold, int chippingMargin, double pxH, int edgeGap, out int maxX, out int maxY)
        {
            maxX = -1; maxY = -1;
            var vals = new List<double>(); var pxs = new List<int>(); var pys = new List<int>();
            for (int x = 0; x < w; x++)
            {
                double startY = topLine.GetY(x), endY = bottomLine.GetY(x);
                if (startY < 0 || endY >= h || startY >= endY) continue;
                int topY = Math.Max(0, (int)startY);
                bool dark = false; int darkStart = -1;
                int yEnd = Math.Min(topY + chippingMargin, (int)endY);
                for (int y = topY; y < yEnd; y++)
                {
                    byte v = image[y * w + x];
                    if (v < threshold && !dark) { if (y - topY > edgeGap) break; dark = true; darkStart = y; }
                    else if (v >= threshold && dark)
                    {
                        int depth = y - darkStart;
                        if (depth > 0) { vals.Add(depth * pxH); pxs.Add(x); pys.Add((darkStart + y) / 2); }
                        dark = false; break;
                    }
                }
            }
            if (vals.Count < 15) return 0;
            double max = 0;
            for (int i = 15; i < vals.Count - 15; i++) if (vals[i] > max) { max = vals[i]; maxX = pxs[i]; maxY = pys[i]; }
            return max;
        }

        private sealed class ChipInfo { public double Depth; public List<PointF> Contour = new List<PointF>(); }

        // ── CDT-310 SideChippingInspector.InspectBottomChipping (dark-scan + 최대영역 Contour) ──
        private static ChipInfo InspectBottomChipping(byte[] image, int w, int h, Line topLine, Line bottomLine,
                                                      int threshold, int chippingMargin, double pxH, int edgeGap)
        {
            var list = new List<ChipInfo>();
            for (int x = 0; x < w; x++)
            {
                double startY = topLine.GetY(x), endY = bottomLine.GetY(x);
                if (startY < 0 || endY >= h || startY >= endY) continue;
                int bottomY = Math.Min(h - 1, (int)endY);
                bool dark = false; int darkStart = -1; int scan = 0;
                int yEnd = Math.Max(bottomY - chippingMargin, (int)startY);
                for (int y = bottomY; y > yEnd; y--)
                {
                    byte v = image[y * w + x];
                    if (v < threshold && !dark) { if (bottomY - y > edgeGap) break; dark = true; darkStart = y; }
                    else if (v >= threshold && dark)
                    {
                        int depth = darkStart - y;
                        if (depth > 0) { var ci = new ChipInfo { Depth = depth * pxH }; ci.Contour.Add(new PointF(x, y)); ci.Contour.Add(new PointF(x, bottomY)); list.Add(ci); }
                        dark = false; break;
                    }
                    else { scan++; if (scan > 10) break; }
                }
            }
            var maxInfo = new ChipInfo();
            if (list.Count < 15) return maxInfo;
            double max = 0; int maxIdx = 0;
            for (int i = 15; i < list.Count - 15; i++) if (list[i].Depth > max) { max = list[i].Depth; maxInfo = list[i]; maxIdx = i; }
            int wR = 0; for (int i = 0; i < 100; i++) { int idx = maxIdx - i; if (idx < 0) break; if (list[idx].Depth > 0.02) wR = i; else break; }
            int wL = 0; for (int i = 0; i < 100; i++) { int idx = maxIdx + i; if (idx >= list.Count) break; if (list[idx].Depth > 0.02) wL = i; else break; }
            if (maxInfo.Contour.Count > 0)
            {
                int xRef = (int)maxInfo.Contour[0].X; var v = maxInfo.Contour.ToList(); maxInfo.Contour.Clear();
                for (int i = 0; i < 2; i++) { maxInfo.Contour.Add(new PointF(xRef - wR, v[i].Y)); maxInfo.Contour.Add(new PointF(xRef + wL, v[i].Y)); }
            }
            return maxInfo;
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

        // ── 라인 후보 검출: CUDA(가용 시) 우선, 미가용 시 CPU 스캔(310 FindLine 동일 로직) ──
        // 반환: 각 열의 상/하 에지 후보 y(-1=없음). 310 CudaWrapper.GetTopBottomLineCandidates 와 동등.
        private static void GetLineCandidates(byte[] image, int w, int h, int threshold, double scanRate,
                                              out int[] topC, out int[] botC)
        {
            if (scanRate < 0.3) scanRate = 0.3;
            if (scanRate > 1) scanRate = 0.99;

            if (CudaInterop.TryGetTopBottomLineCandidates(image, w, h, (byte)threshold, scanRate, out topC, out botC)
                && topC != null && botC != null)
            { GpuBackend.Note("SideLine", ComputeBackend.Cuda); return; }   // GPU 경로

            GpuBackend.Note("SideLine", ComputeBackend.Cpu);
            // CPU 폴백(310 FindLine.cs 동일 교차 검출)
            topC = new int[w]; botC = new int[w];
            int tStart = Math.Max(2, 10), tEnd = Math.Min(h - 3, (int)(h * scanRate) - 10);
            int bStart = Math.Min(h - 3, h - 10), bEnd = Math.Max(2, (int)(h * 0.5) + 10);
            for (int x = 0; x < w; x++)
            {
                topC[x] = -1; botC[x] = -1;
                for (int y = tStart; y <= tEnd; y++)
                    if (image[y * w + x] > threshold && image[(y - 1) * w + x] <= threshold && image[(y - 2) * w + x] <= threshold)
                    { topC[x] = y; break; }
                for (int y = bStart; y >= bEnd; y--)
                    if (image[y * w + x] > threshold && image[(y + 1) * w + x] <= threshold && image[(y + 2) * w + x] <= threshold)
                    { botC[x] = y; break; }
            }
        }

        // ── 310 FindLine.FindTopLineOfChip (envelope + quantile trim + trend line) ──
        private static Line FindTopLineOfChip(int w, int h, byte[] image, int threshold, double scanRate,
                                              int binSize, double keepQuantile)
        {
            int[] topC, botC;
            GetLineCandidates(image, w, h, threshold, scanRate, out topC, out botC);
            var pts = new List<PointF>();
            for (int x = 0; x < w; x++) if (topC[x] >= 0) pts.Add(new PointF(x, topC[x]));
            if (pts.Count < 2) return null;

            var env = KeepOuterPerBin(pts, binSize, takeMinY: true);
            var listPoint = (env != null && env.Count >= 2) ? env : pts;
            var trimmed = ReMoveTop(listPoint, keepQuantile);
            if (trimmed == null || trimmed.Count < 2) trimmed = pts;

            double dA, dB;
            GetTrendLine(trimmed, out dA, out dB);
            return new Line(dA, dB);
        }

        // ── 310 FindLine.FindBottomLineOfChip (하단 바깥 에지 강건 피팅) ──
        private static Line FindBottomLineOfChip(int w, int h, byte[] image, int threshold,
                                                 int binSize, double keepQuantile)
        {
            int[] topC, botC;
            GetLineCandidates(image, w, h, threshold, 0.9, out topC, out botC);
            var pts = new List<PointF>();
            for (int x = 0; x < w; x++) if (botC[x] >= 0) pts.Add(new PointF(x, botC[x]));
            if (pts.Count < 2) return null;

            var env = KeepOuterPerBin(pts, binSize, takeMinY: false);   // 하단 = 가장 아래(maxY)
            var listPoint = (env != null && env.Count >= 2) ? env : pts;
            var trimmed = ReMoveBottom(listPoint, keepQuantile);
            if (trimmed == null || trimmed.Count < 2) trimmed = pts;

            double dA, dB;
            GetTrendLine(trimmed, out dA, out dB);
            return new Line(dA, dB);
        }

        // ReMoveTop 의 하단 버전(잔차 상위 분위 유지 = 바깥쪽 아래 에지로 수렴)
        private static List<PointF> ReMoveBottom(List<PointF> listPoint, double keepQuantile)
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
                cut = Percentile(residuals, 1.0 - keepQuantile);
                var next = temp.Where(p => (p.Y - (dA * p.X + dB)) >= cut).ToList();
                if (next.Count < 2) break;
                temp = next;
            }
            return temp;
        }

        private static List<PointF> ReMoveTop(List<PointF> listPoint, double keepQuantile)
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
                cut = Percentile(residuals, keepQuantile);
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
