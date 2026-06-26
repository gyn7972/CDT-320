using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 측면(Side) 칩핑 검사 — CDT-310 <c>SideChippingInspector</c> 기능 포팅.
    /// 칩 상/하 에지 라인을 찾아 라인 대비 결손(칩핑) 최대 깊이를 측정한다. 순수 C#, 백엔드 무관.
    /// 파라미터는 310 <c>SideInspectionParameter</c> 값 그대로(아래 프로퍼티 기본값).
    /// </summary>
    public class SideAppearanceInspector : IInspector, IStepImageProvider
    {
        public string Id { get; }
        public Roi InspectionRoi { get; set; }

        // 단계별 디버그 이미지(원본/그레이/임계/블롭) — CaptureDebug=true 일 때만 채움.
        public bool CaptureDebug { get; set; }
        private readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Bitmap>> _steps
            = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Bitmap>>();
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Bitmap>> DebugSteps => _steps;

        // ── CDT-310 SideInspectionParameter 파라미터 (기본값 동일) ──
        public double PixelSizeWidthMm  { get; set; } = 0.003125;  // VisionConfig.SideVisionFront/Back
        public double PixelSizeHeightMm { get; set; } = 0.003125;
        public int    ChipThreshold     { get; set; } = 60;        // 칩(밝은 띠)/배경 분리
        /// <summary>칩핑 깊이 스펙[mm](=UpperLimit 미설정 시 폴백). 초과 시 NG.</summary>
        public double ChippingDepth     { get; set; } = 0.020;
        /// <summary>칩핑 상한[mm] — Max Chipping Depth 가 이를 초과하면 NG(화면 UpperLimit).</summary>
        public double ChippingUpperLimit { get; set; } = 0.050;
        /// <summary>칩핑 하한[mm] — 차트 표시/판정 하한(화면 LowerLimit, 보통 음수=측정 노이즈 허용).</summary>
        public double ChippingLowerLimit { get; set; } = -0.025;
        public double ChippingLength    { get; set; } = 0.0;
        public double ForeignObjectSize { get; set; } = 0.5;
        public SizeF  ChipLowerSpecLimit{ get; set; } = new SizeF(0, 0);
        public SizeF  ChipUpperSpecLimit{ get; set; } = new SizeF(0, 0);
        /// <summary>칩 두께[mm] — 하단 라인 오프셋 산출.</summary>
        public double ChipThickness     { get; set; } = 0.25;

        // ── 표면 이물(오염) — 310 ContaminationInspector(Black-Hat 블롭) 파라미터 ──
        public int    TopHatRadius             { get; set; } = 21;
        public int    TopHatThreshold          { get; set; } = 30;
        public int    MinForeignAreaFilterSize { get; set; } = 36;
        public int    MaxForeignAreaFilterSize { get; set; } = 100000;
        public int    LinkDistance             { get; set; } = 25;
        public int    LastForeignCount         { get; private set; }

        // 직전 검출(오버레이용)
        public bool     LastValid   { get; private set; }
        public bool     LastPass    { get; private set; }
        public string   LastText    { get; private set; }
        public PointF[] LastCorners { get; private set; }   // 검출 에지 기준선 박스(상/하 baseline, 이미지 px)
        public int      LastTopBaseY { get; private set; }
        public int      LastBotBaseY { get; private set; }
        // 컬럼별 실제 에지 프로파일(울퉁불퉁) — 오버레이용(절대 이미지 좌표, 없으면 null). 평균 직선이 아닌 실제 면.
        public PointF[] LastTopProfile { get; private set; }
        public PointF[] LastBotProfile { get; private set; }

        // 역할 — id 에 "Chipping" 이면 엣지 칩핑, "Surface" 면 표면 이물/오염. 둘 다 아니면(수동테스트 등) 둘 다.
        private readonly bool _doChipping;
        private readonly bool _doSurface;
        /// <summary>엣지 칩핑 검사 역할(앞쪽 칩핑).</summary>
        public bool IsChippingRole => _doChipping;
        /// <summary>표면 이물/오염 검사 역할(앞쪽 면).</summary>
        public bool IsSurfaceRole => _doSurface;

        public SideAppearanceInspector(string id)
        {
            Id = id;
            bool isChip = id != null && id.IndexOf("Chipping", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isSurf = id != null && id.IndexOf("Surface", StringComparison.OrdinalIgnoreCase) >= 0;
            _doChipping = isChip || (!isChip && !isSurf);
            _doSurface  = isSurf || (!isChip && !isSurf);
            InspectionRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 600, Height = 200 };
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

            // 각 열의 밝은 띠 상/하 경계 + 칩 실제 가로 범위(xStart~xEnd)
            var topY = new int[w]; var botY = new int[w];
            int xStart = -1, xEnd = -1, valid = 0;
            for (int x = 0; x < w; x++)
            {
                int t = -1, b = -1;
                for (int y = 0; y < h; y++) if (g[y * w + x] >= ChipThreshold) { t = y; break; }
                for (int y = h - 1; y >= 0; y--) if (g[y * w + x] >= ChipThreshold) { b = y; break; }
                topY[x] = t; botY[x] = b;
                if (t >= 0) { valid++; if (xStart < 0) xStart = x; xEnd = x; }
            }
            if (valid < w / 8 || xStart < 0) { r.ErrorMessage = "chip edge not found"; r.IsPass = false; return r; }

            int topBase = MedianValid(topY);
            int botBase = MedianValid(botY);

            // 컬럼별 실제 에지 점(절대 이미지 좌표) — 오버레이에 실제 굴곡 면을 그림(평균 직선 아님)
            var topProf = new List<PointF>(); var botProf = new List<PointF>();
            for (int x = 0; x < w; x++)
            {
                if (topY[x] >= 0) topProf.Add(new PointF(roi.X + x, roi.Y + topY[x]));
                if (botY[x] >= 0) botProf.Add(new PointF(roi.X + x, roi.Y + botY[x]));
            }
            LastTopProfile = topProf.Count >= 2 ? topProf.ToArray() : null;
            LastBotProfile = botProf.Count >= 2 ? botProf.ToArray() : null;

            // 칩핑 = 라인 대비 안쪽 결손 최대(상=아래로 파임, 하=위로 파임). 칩 실제 범위 [xStart,xEnd] 안에서만.
            double topChipPx = 0, botChipPx = 0; int topAt = xStart, botAt = xStart;
            for (int x = xStart; x <= xEnd; x++)
            {
                if (topY[x] >= 0) { double d = topY[x] - topBase; if (d > topChipPx) { topChipPx = d; topAt = x; } }
                if (botY[x] >= 0) { double d = botBase - botY[x]; if (d > botChipPx) { botChipPx = d; botAt = x; } }
            }
            double topMm = topChipPx * PixelSizeHeightMm;
            double botMm = botChipPx * PixelSizeHeightMm;
            double maxMm = Math.Max(topMm, botMm);

            double foreignMm = 0;
            bool chipPass = true, foreignPass = true;

            // ── 칩핑 역할(앞쪽 칩핑) — CDT-310 SideChippingInspector 알고리즘(라인검출+회전+상/하 스캔) ──
            if (_doChipping)
            {
                var cc = SideChippingCore.Inspect(g, w, h, ChipThreshold, ChipThickness, PixelSizeWidthMm, PixelSizeHeightMm);
                if (cc.Valid)
                {
                    topMm = cc.TopMm; botMm = cc.BottomMm; maxMm = cc.MaxMm;   // 310 결과로 대체
                    topBase = cc.TopBaseY; botBase = cc.BottomBaseY;
                    xStart = cc.XStart; xEnd = cc.XEnd;
                }
                double upper = ChippingUpperLimit > 0 ? ChippingUpperLimit : ChippingDepth;   // NG 스펙(폴백)
                chipPass = (maxMm <= upper) && (maxMm >= ChippingLowerLimit);
                r.Items.Add(new InspectionItem { Name = "Max Chipping Depth", Value = maxMm.ToString("F4"), IsPass = chipPass });
                r.Items.Add(new InspectionItem { Name = "Chipping Top",       Value = topMm.ToString("F4"), IsPass = topMm <= upper });
                r.Items.Add(new InspectionItem { Name = "Chipping Bottom",    Value = botMm.ToString("F4"), IsPass = botMm <= upper });
                // NG(스펙 초과)일 때만 최대 칩핑 위치에 마커 — PASS면 박스 안 그림(저배율에서도 보이게 크게)
                if (!chipPass && cc.Valid && cc.MaxMm > 0 && cc.MaxX >= 0)
                    r.Defects.Add(new DefectMark { X = roi.X + cc.MaxX, Y = roi.Y + cc.MaxY, Width = 48, Height = 48, Area = cc.MaxMm });
            }

            // ── 표면 역할(앞쪽 면) — 이물/오염: 310 ContaminationInspector(Black-Hat 블롭) ──
            if (_doSurface)
            {
                // 얇은 띠에서 커널 반경이 띠 두께보다 크면 가장자리를 오검 → 띠 두께의 1/3 로 자동 제한.
                int bandH = Math.Max(2, botBase - topBase);
                int effRadius = Math.Max(2, Math.Min(TopHatRadius, bandH / 3));
                var blobs = ContaminationDetector.Detect(g, w, h, effRadius, TopHatThreshold,
                                                         MinForeignAreaFilterSize, MaxForeignAreaFilterSize, LinkDistance);
                LastForeignCount = blobs.Count;
                double maxPx = 0;
                foreach (var b in blobs)
                {
                    double sz = Math.Max(b.Width, b.Height);
                    if (sz > maxPx) maxPx = sz;
                    // 결함 = 빨간 박스(310 RenderResult 동일 색)
                    r.Defects.Add(new DefectMark { X = roi.X + b.CenterX, Y = roi.Y + b.CenterY, Width = b.Width, Height = b.Height, Area = b.Area });
                }
                foreignMm = maxPx * PixelSizeWidthMm;
                foreignPass = LastForeignCount == 0;      // 이물 블롭이 하나라도 있으면 NG(310 defectCount)
                r.Items.Add(new InspectionItem { Name = "Foreign Count", Value = LastForeignCount.ToString(), IsPass = foreignPass });
                r.Items.Add(new InspectionItem { Name = "Foreign Max",   Value = foreignMm.ToString("F4"),    IsPass = foreignPass });
            }

            bool pass = chipPass && foreignPass;
            r.IsPass = pass;

            // 검출 에지 기준선(상/하 baseline) — 칩 실제 범위만. 오버레이/디버그 이미지에 "어떻게 찾았는지" 표시.
            int tY = roi.Y + topBase, bY = roi.Y + botBase;
            int sX = roi.X + xStart, eX = roi.X + xEnd;
            LastTopBaseY = tY; LastBotBaseY = bY;
            LastCorners = new[]
            {
                new PointF(sX, tY), new PointF(eX, tY),
                new PointF(eX, bY), new PointF(sX, bY)
            };
            if (CaptureDebug)
            {
                foreach (var kv in _steps) { try { kv.Value?.Dispose(); } catch { } }
                _steps.Clear();
                _steps.Add(new System.Collections.Generic.KeyValuePair<string, Bitmap>("1_gray", ToBmp(g, w, h, false, 0)));
                _steps.Add(new System.Collections.Generic.KeyValuePair<string, Bitmap>("2_threshold", ToBmp(g, w, h, true, ChipThreshold)));
            }

            LastPass  = pass;
            LastValid = true;
            LastText = (_doChipping && _doSurface) ? $"Chip {maxMm:F4}  Foreign x{LastForeignCount}"
                     : _doChipping ? $"Chipping Max {maxMm:F4} (upper {ChippingUpperLimit:F4})"
                     : $"Foreign Count {LastForeignCount}  Max {foreignMm:F4}";
            return r;
        }

        /// <summary>그레이 배열 → 24bpp 비트맵(threshold=true 면 thr 기준 이진화). 단계 디버그용.</summary>
        private static Bitmap ToBmp(byte[] gray, int w, int h, bool threshold, int thr)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = data.Stride;
                byte[] row = new byte[stride];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        byte v = gray[y * w + x];
                        if (threshold) v = (byte)(v >= thr ? 255 : 0);
                        int o = x * 3; row[o] = row[o + 1] = row[o + 2] = v;
                    }
                    Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * stride), stride);
                }
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }

        /// <summary>띠 내부 박스[x0..x1, y0..y1]에서 가장 큰 어두운 블롭(칩 위 검은 물체)의 크기[px]와 bbox.</summary>
        private static double LargestDarkBlob(byte[] g, int w, int h, int x0, int x1, int y0, int y1, int thr,
            out int bx, out int by, out int bw, out int bh)
        {
            bx = by = bw = bh = 0;
            if (x1 <= x0 || y1 <= y0) return 0;
            x0 = Math.Max(0, x0); y0 = Math.Max(0, y0);
            x1 = Math.Min(w - 1, x1); y1 = Math.Min(h - 1, y1);
            var visited = new bool[w * h];
            var stack = new Stack<int>();
            double best = 0; const int minArea = 24;
            for (int yy = y0; yy <= y1; yy++)
                for (int xx = x0; xx <= x1; xx++)
                {
                    int idx = yy * w + xx;
                    if (visited[idx] || g[idx] >= thr) continue;
                    stack.Push(idx);
                    int minx = xx, maxx = xx, miny = yy, maxy = yy, area = 0;
                    while (stack.Count > 0)
                    {
                        int p = stack.Pop();
                        if (p < 0 || p >= g.Length || visited[p]) continue;
                        visited[p] = true;
                        int px = p % w, py = p / w;
                        if (px < x0 || px > x1 || py < y0 || py > y1 || g[p] >= thr) continue;
                        area++;
                        if (px < minx) minx = px; if (px > maxx) maxx = px;
                        if (py < miny) miny = py; if (py > maxy) maxy = py;
                        stack.Push(p - 1); stack.Push(p + 1); stack.Push(p - w); stack.Push(p + w);
                    }
                    if (area < minArea) continue;
                    double sz = Math.Max(maxx - minx + 1, maxy - miny + 1);
                    if (sz > best) { best = sz; bx = minx; by = miny; bw = maxx - minx + 1; bh = maxy - miny + 1; }
                }
            return best;
        }

        private static int MedianValid(int[] a)
        {
            var list = new List<int>();
            foreach (int v in a) if (v >= 0) list.Add(v);
            if (list.Count == 0) return 0;
            list.Sort();
            return list[list.Count / 2];
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
