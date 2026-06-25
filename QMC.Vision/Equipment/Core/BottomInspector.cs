using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QMC.Vision.Core
{
    /// <summary>
    /// Bottom 외관 검사 — CDT-310 <c>CDTInspector.BottomInspect</c> / <c>QMC_FindChippingNForeign</c> 기능 포팅.
    /// 사이즈(너비/높이), 칩핑(상/우/하/좌), 이물(TopHat)을 산출한다. 네이티브 CUDA 경로 대신 순수 C#.
    /// 파라미터는 310 <c>BottomInspectionParameter</c> 값 그대로(아래 프로퍼티 기본값).
    /// 백엔드 무관 — 카메라 그랩 Bitmap 에 직접 동작(PlacementGapInspector 와 동일 패턴).
    /// </summary>
    public class BottomInspector : IInspector
    {
        public string Id { get; }
        public Roi InspectionRoi { get; set; }

        // ── CDT-310 BottomInspectionParameter 파라미터 (기본값 동일) ──
        public double PixelSizeWidthMm  { get; set; } = 0.001399356618; // VisionConfig.BottomVision
        public double PixelSizeHeightMm { get; set; } = 0.001399356618;
        /// <summary>칩/배경 분리 임계(0~255). 다이=밝음 기준.</summary>
        public int    ChipThreshold     { get; set; } = 128;
        public double FirstPeekValueThreshold { get; set; } = 230;
        public double PeekValueThreshold       { get; set; } = 40;
        public double Stdev               { get; set; } = 0.01;
        /// <summary>칩핑 깊이 스펙[mm]. 초과 시 NG.</summary>
        public double ChippingDepth       { get; set; } = 0.020;
        public double ChippingLength      { get; set; } = 0.0;
        public SizeF  ChipLowerSpecLimit  { get; set; } = new SizeF(0, 0); // [mm] 0=미사용
        public SizeF  ChipUpperSpecLimit  { get; set; } = new SizeF(0, 0); // [mm] 0=미사용
        /// <summary>이물 크기 스펙[mm]. 초과 시 NG.</summary>
        public double ForeignObjectSize   { get; set; } = 0.5;
        public int    TopHatRadius        { get; set; } = 21;
        public int    TopHatThreshold     { get; set; } = 30;
        public int    MinForeignAreaFilterSize { get; set; } = 36;
        public int    LinkDistance        { get; set; } = 25;
        public double PortentiolDefactMinSize { get; set; } = 20;
        public bool   UseContaminationInspection { get; set; } = true;
        public bool   DarkChip            { get; set; } = false; // 다이가 어두우면 true
        public string FileSavePath        { get; set; } = "Z:\\Log\\Image";

        // ── 직전 검출 기하(오버레이용, 이미지 px) ──
        public bool     LastValid   { get; private set; }
        public PointF[] LastCorners { get; private set; }
        public string   LastText    { get; private set; }

        public BottomInspector(string id)
        {
            Id = id;
            InspectionRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
        }

        public InspectionResult Inspect(Bitmap image)
        {
            var r = new InspectionResult { RoiName = Id, IsPass = true };
            LastValid = false;
            if (image == null) { r.ErrorMessage = "no image"; r.IsPass = false; return r; }

            Rectangle roi = InspectionRoi != null ? InspectionRoi.BoundingBox : new Rectangle(0, 0, image.Width, image.Height);
            roi.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            if (roi.Width <= 4 || roi.Height <= 4) { r.ErrorMessage = "roi empty"; r.IsPass = false; return r; }

            int w, h;
            byte[] g = ToGray(image, roi, out w, out h);

            // ── 1) 칩 영역(밝은 다이) → 사이즈 ──
            byte[] mask = Threshold(g, w, h, ChipThreshold, DarkChip);
            int x0, y0, x1, y1;
            if (!BoundingBox(mask, w, h, out x0, out y0, out x1, out y1))
            { r.ErrorMessage = "chip not found"; r.IsPass = false; return r; }

            int pw = x1 - x0 + 1, ph = y1 - y0 + 1;
            double widthMm  = pw * PixelSizeWidthMm;
            double heightMm = ph * PixelSizeHeightMm;
            bool sizePass = SpecOk(widthMm, ChipLowerSpecLimit.Width, ChipUpperSpecLimit.Width)
                         && SpecOk(heightMm, ChipLowerSpecLimit.Height, ChipUpperSpecLimit.Height);
            AddItem(r, "Width",  widthMm.ToString("F4"),  sizePass);
            AddItem(r, "Height", heightMm.ToString("F4"), sizePass);

            // ── 2) 칩핑 (4변 에지 편차) ──
            double chipTop, chipBottom, chipLeft, chipRight;
            EdgeChipping(mask, w, h, x0, y0, x1, y1, out chipTop, out chipBottom, out chipLeft, out chipRight, r.Defects);
            double cT = chipTop * PixelSizeHeightMm, cB = chipBottom * PixelSizeHeightMm;
            double cL = chipLeft * PixelSizeWidthMm, cR = chipRight * PixelSizeWidthMm;
            double maxChip = Math.Max(Math.Max(cT, cB), Math.Max(cL, cR));
            bool chipPass = maxChip <= ChippingDepth;
            AddItem(r, "Chipping Top",    cT.ToString("F4"), cT <= ChippingDepth);
            AddItem(r, "Chipping Right",  cR.ToString("F4"), cR <= ChippingDepth);
            AddItem(r, "Chipping Bottom", cB.ToString("F4"), cB <= ChippingDepth);
            AddItem(r, "Chipping Left",   cL.ToString("F4"), cL <= ChippingDepth);

            // ── 3) 이물 (TopHat: gray - 박스배경, 칩 내부) ──
            double foreignMm = 0; bool foreignPass = true;
            if (UseContaminationInspection)
            {
                foreignMm = Foreign(g, w, h, mask, x0, y0, x1, y1, r.Defects);
                foreignPass = foreignMm <= ForeignObjectSize;
                AddItem(r, "Foreign", foreignMm.ToString("F4"), foreignPass);
            }

            r.IsPass = sizePass && chipPass && foreignPass;

            // 오버레이 기하(칩 바운딩 박스)
            LastCorners = new[]
            {
                new PointF(roi.X + x0, roi.Y + y0), new PointF(roi.X + x1, roi.Y + y0),
                new PointF(roi.X + x1, roi.Y + y1), new PointF(roi.X + x0, roi.Y + y1)
            };
            // Defect 좌표를 ROI 기준→이미지 기준으로 평행이동
            foreach (var d in r.Defects) { d.X += roi.X; d.Y += roi.Y; }
            LastValid = true;
            LastText = $"W {widthMm:F4} H {heightMm:F4} Chip {maxChip:F4} Foreign {foreignMm:F4}";
            return r;
        }

        // ── 알고리즘 헬퍼 ──
        private static void AddItem(InspectionResult r, string name, string val, bool pass)
            => r.Items.Add(new InspectionItem { Name = name, Value = val, IsPass = pass });

        private static bool SpecOk(double v, double lo, double hi)
        {
            if (lo <= 0 && hi <= 0) return true;     // 스펙 미설정 → 통과
            if (lo > 0 && v < lo) return false;
            if (hi > 0 && v > hi) return false;
            return true;
        }

        private static byte[] Threshold(byte[] g, int w, int h, int t, bool dark)
        {
            var m = new byte[w * h];
            for (int i = 0; i < g.Length; i++)
                m[i] = (byte)((dark ? g[i] < t : g[i] >= t) ? 1 : 0);
            return m;
        }

        private static bool BoundingBox(byte[] m, int w, int h, out int x0, out int y0, out int x1, out int y1)
        {
            x0 = w; y0 = h; x1 = -1; y1 = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (m[y * w + x] != 0)
                    {
                        if (x < x0) x0 = x; if (x > x1) x1 = x;
                        if (y < y0) y0 = y; if (y > y1) y1 = y;
                    }
            return x1 >= x0 && y1 >= y0;
        }

        /// <summary>4변 에지 편차 최대 결손(px). 각 변 boundary 의 중앙값 대비 안쪽으로 들어온 최대치.</summary>
        private static void EdgeChipping(byte[] m, int w, int h, int x0, int y0, int x1, int y1,
            out double top, out double bottom, out double left, out double right, List<DefectMark> defects)
        {
            int pw = x1 - x0 + 1, ph = y1 - y0 + 1;
            // 좌/우: 각 행의 첫/마지막 마스크 x
            var leftEdge = new int[ph]; var rightEdge = new int[ph];
            for (int yy = 0; yy < ph; yy++)
            {
                int y = y0 + yy; int lx = -1, rx = -1;
                for (int x = x0; x <= x1; x++) if (m[y * w + x] != 0) { lx = x; break; }
                for (int x = x1; x >= x0; x--) if (m[y * w + x] != 0) { rx = x; break; }
                leftEdge[yy] = lx < 0 ? x0 : lx; rightEdge[yy] = rx < 0 ? x1 : rx;
            }
            // 상/하: 각 열의 첫/마지막 마스크 y
            var topEdge = new int[pw]; var botEdge = new int[pw];
            for (int xx = 0; xx < pw; xx++)
            {
                int x = x0 + xx; int ty = -1, by = -1;
                for (int y = y0; y <= y1; y++) if (m[y * w + x] != 0) { ty = y; break; }
                for (int y = y1; y >= y0; y--) if (m[y * w + x] != 0) { by = y; break; }
                topEdge[xx] = ty < 0 ? y0 : ty; botEdge[xx] = by < 0 ? y1 : by;
            }
            left   = MaxInward(leftEdge, +1, Median(leftEdge), x0, y0, true, false, defects);
            right  = MaxInward(rightEdge, -1, Median(rightEdge), x0, y0, true, true, defects);
            top    = MaxInward(topEdge, +1, Median(topEdge), x0, y0, false, false, defects);
            bottom = MaxInward(botEdge, -1, Median(botEdge), x0, y0, false, true, defects);
        }

        private static double MaxInward(int[] edge, int sign, int baseline, int x0, int y0,
            bool vertical, bool farSide, List<DefectMark> defects)
        {
            int maxDev = 0, maxIdx = 0;
            for (int i = 0; i < edge.Length; i++)
            {
                int dev = sign * (edge[i] - baseline);   // 안쪽으로 들어오면 양수
                if (dev > maxDev) { maxDev = dev; maxIdx = i; }
            }
            if (maxDev > 2)
            {
                double dx = vertical ? edge[maxIdx] : (x0 + maxIdx);
                double dy = vertical ? (y0 + maxIdx) : edge[maxIdx];
                defects.Add(new DefectMark { X = dx, Y = dy, Width = 8, Height = 8, Area = maxDev });
            }
            return maxDev;
        }

        private static int Median(int[] a)
        {
            var b = (int[])a.Clone(); Array.Sort(b);
            return b.Length == 0 ? 0 : b[b.Length / 2];
        }

        /// <summary>이물: 칩 내부에서 TopHat(밝은 점) → 최대 결함 크기[mm].</summary>
        private double Foreign(byte[] g, int w, int h, byte[] mask, int x0, int y0, int x1, int y1, List<DefectMark> defects)
        {
            // 박스 평균(배경) — 적분영상
            long[] integ = new long[(w + 1) * (h + 1)];
            for (int y = 0; y < h; y++)
            {
                long rowSum = 0;
                for (int x = 0; x < w; x++)
                {
                    rowSum += g[y * w + x];
                    integ[(y + 1) * (w + 1) + (x + 1)] = integ[y * (w + 1) + (x + 1)] + rowSum;
                }
            }
            int rad = Math.Max(2, TopHatRadius);
            var spot = new byte[w * h];
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    if (mask[y * w + x] == 0) continue;     // 칩 내부만
                    int ax = Math.Max(0, x - rad), ay = Math.Max(0, y - rad);
                    int bx = Math.Min(w - 1, x + rad), by = Math.Min(h - 1, y + rad);
                    long sum = integ[(by + 1) * (w + 1) + (bx + 1)] - integ[ay * (w + 1) + (bx + 1)]
                             - integ[(by + 1) * (w + 1) + ax] + integ[ay * (w + 1) + ax];
                    int cnt = (bx - ax + 1) * (by - ay + 1);
                    double bg = cnt > 0 ? (double)sum / cnt : 0;
                    double th = DarkChip ? (bg - g[y * w + x]) : (g[y * w + x] - bg);
                    // 이물은 보통 칩 표면 대비 어두움 — 양방향 모두 검출
                    if (Math.Abs(g[y * w + x] - bg) > TopHatThreshold) spot[y * w + x] = 1;
                }
            // 연결성분 라벨링 → 면적 필터 → 최대 크기
            double maxMm = 0;
            var labels = new int[w * h];
            var stack = new Stack<int>();
            int lab = 0;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int idx = y * w + x;
                    if (spot[idx] == 0 || labels[idx] != 0) continue;
                    lab++; stack.Push(idx);
                    int minx = x, maxx = x, miny = y, maxy = y, area = 0;
                    while (stack.Count > 0)
                    {
                        int p = stack.Pop();
                        if (labels[p] != 0) continue;
                        labels[p] = lab; area++;
                        int px = p % w, py = p / w;
                        if (px < minx) minx = px; if (px > maxx) maxx = px;
                        if (py < miny) miny = py; if (py > maxy) maxy = py;
                        if (px > x0 && spot[p - 1] != 0 && labels[p - 1] == 0) stack.Push(p - 1);
                        if (px < x1 && spot[p + 1] != 0 && labels[p + 1] == 0) stack.Push(p + 1);
                        if (py > y0 && spot[p - w] != 0 && labels[p - w] == 0) stack.Push(p - w);
                        if (py < y1 && spot[p + w] != 0 && labels[p + w] == 0) stack.Push(p + w);
                    }
                    if (area < MinForeignAreaFilterSize) continue;
                    double szMm = Math.Max((maxx - minx + 1) * PixelSizeWidthMm, (maxy - miny + 1) * PixelSizeHeightMm);
                    if (szMm > maxMm) maxMm = szMm;
                    defects.Add(new DefectMark { X = (minx + maxx) / 2.0, Y = (miny + maxy) / 2.0,
                        Width = maxx - minx + 1, Height = maxy - miny + 1, Area = area });
                }
            return maxMm;
        }

        private static byte[] ToGray(Bitmap bmp, Rectangle rect, out int w, out int h)
        {
            rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
            w = rect.Width; h = rect.Height;
            var gray = new byte[w * h];
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = data.Stride; byte[] row = new byte[stride];
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
