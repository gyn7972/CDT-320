using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace QMC.Vision.Core
{
    /// <summary>
    /// Bottom 외관 검사 — CDT-310 <c>CDTInspector.BottomInspect</c> / <c>QMC_FindChippingNForeign</c> 기능 포팅.
    /// 사이즈(너비/높이), 칩핑(상/우/하/좌), 이물(TopHat)을 산출한다. 네이티브 CUDA 경로 대신 순수 C#.
    /// 파라미터는 310 <c>BottomInspectionParameter</c> 값 그대로(아래 프로퍼티 기본값).
    /// 백엔드 무관 — 카메라 그랩 Bitmap 에 직접 동작(PlacementGapInspector 와 동일 패턴).
    /// </summary>
    public class BottomInspector : IInspector, IStepImageProvider
    {
        public string Id { get; }
        public Roi InspectionRoi { get; set; }

        // 단계별 디버그 이미지(그레이/다이마스크/마스킹/블랙햇/임계) — CaptureDebug=true 일 때만 채움.
        public bool CaptureDebug { get; set; }
        private readonly System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Bitmap>> _steps
            = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Bitmap>>();
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Bitmap>> DebugSteps => _steps;

        // ── CDT-310 BottomInspectionParameter 파라미터 (기본값 동일) ──
        public double PixelSizeWidthMm  { get; set; } = 0.0007; // VisionConfig.BottomVision (12000px 카메라, 1px=0.0007mm)
        public double PixelSizeHeightMm { get; set; } = 0.0007;
        /// <summary>칩/배경 분리 임계(0~255). 다이=밝음 기준.</summary>
        public int    ChipThreshold     { get; set; } = 128;
        public double FirstPeekValueThreshold { get; set; } = 230;
        public double PeekValueThreshold       { get; set; } = 40;
        public double Stdev               { get; set; } = 0.01;
        /// <summary>칩핑 깊이 스펙[mm]. 초과 시 NG.</summary>
        public double ChippingDepth       { get; set; } = 0.020;
        public double ChippingLength      { get; set; } = 0.0;
        /// <summary>칩핑 검사영역 오프셋[px] — 검출 에지에서 각 변을 안쪽으로 이만큼 줄여 검사(코너 영향 배제).
        /// CDT-310 m_nLeft/Right/Top/BottomMargin 대응(기본 0 = 에지까지 검사).</summary>
        public int    ChipEdgeMargin      { get; set; } = 0;
        /// <summary>이물 검사영역 오프셋[px] — 다이 내부에서 외곽선(점진적 전이)을 배제하기 위한 안쪽 여백.
        /// CDT-310 m_n*MarginForeign 대응. 너무 크면 가장자리 이물을 놓침(작게=에지 근접 검사).</summary>
        public int    ForeignEdgeMargin   { get; set; } = 12;
        public SizeF  ChipLowerSpecLimit  { get; set; } = new SizeF(0, 0); // [mm] 0=미사용
        public SizeF  ChipUpperSpecLimit  { get; set; } = new SizeF(0, 0); // [mm] 0=미사용
        /// <summary>이물 크기 스펙[mm]. 초과 시 NG.</summary>
        public double ForeignObjectSize   { get; set; } = 0.5;
        public int    TopHatRadius        { get; set; } = 21;
        public int    TopHatThreshold     { get; set; } = 30;
        public int    MinForeignAreaFilterSize { get; set; } = 36;
        public int    MaxForeignAreaFilterSize { get; set; } = 100000;
        public int    LinkDistance        { get; set; } = 25;
        /// <summary>이물 검출 직전 결함 개수(오버레이/판정).</summary>
        public int    LastForeignCount    { get; private set; }
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
            var _sw = Stopwatch.StartNew();
            byte[] g = ToGray(image, roi, out w, out h);
            long tGray = _sw.ElapsedMilliseconds;

            // ── 1) 제품(다이) 검출 — CDT-310 4변 강건 에지(콜렛→다이 전이). 흰배경/회색콜렛/흰다이 모두 처리. ──
            int[] topE, botE, leftE, rightE; int ty, by, lx, rx;
            if (!FindDie(g, w, h, ChipThreshold, out topE, out botE, out leftE, out rightE, out ty, out by, out lx, out rx))
            { r.ErrorMessage = "die not found"; r.IsPass = false; return r; }
            long tDie = _sw.ElapsedMilliseconds;

            int pw = rx - lx + 1, ph = by - ty + 1;
            double widthMm  = pw * PixelSizeWidthMm;
            double heightMm = ph * PixelSizeHeightMm;
            bool sizePass = SpecOk(widthMm, ChipLowerSpecLimit.Width, ChipUpperSpecLimit.Width)
                         && SpecOk(heightMm, ChipLowerSpecLimit.Height, ChipUpperSpecLimit.Height);
            AddItem(r, "Width",  widthMm.ToString("F4"),  sizePass);
            AddItem(r, "Height", heightMm.ToString("F4"), sizePass);

            // 각도(상단 에지 기울기) + 오프셋(다이 중심 − 공칭/ROI 중심) — CDT-310 BottomResult.Angle/Offset 이식.
            double angleDeg = EdgeAngleDeg(topE, lx, rx);
            double cxImg = roi.X + (lx + rx) / 2.0, cyImg = roi.Y + (ty + by) / 2.0;
            double nomX = (InspectionRoi != null) ? InspectionRoi.CenterX : (roi.X + w / 2.0);
            double nomY = (InspectionRoi != null) ? InspectionRoi.CenterY : (roi.Y + h / 2.0);
            double offXmm = (cxImg - nomX) * PixelSizeWidthMm;
            double offYmm = (cyImg - nomY) * PixelSizeHeightMm;
            AddItem(r, "Angle",    angleDeg.ToString("F3"), true);   // [°] 정보(스펙 별도)
            AddItem(r, "Offset X", offXmm.ToString("F4"),   true);   // [mm]
            AddItem(r, "Offset Y", offYmm.ToString("F4"),   true);

            // ── 2) 칩핑 — 4변 에지 라인(중앙값) 대비 안쪽 결손 최대. 검사영역은 ChipEdgeMargin 만큼 코너에서 줄임. ──
            int cm = Math.Max(0, ChipEdgeMargin);
            double cT = MaxInwardDev(topE,   lx + cm, rx - cm, ty, +1, false, r.Defects) * PixelSizeHeightMm;
            double cB = MaxInwardDev(botE,   lx + cm, rx - cm, by, -1, false, r.Defects) * PixelSizeHeightMm;
            double cL = MaxInwardDev(leftE,  ty + cm, by - cm, lx, +1, true,  r.Defects) * PixelSizeWidthMm;
            double cR = MaxInwardDev(rightE, ty + cm, by - cm, rx, -1, true,  r.Defects) * PixelSizeWidthMm;
            double maxChip = Math.Max(Math.Max(cT, cB), Math.Max(cL, cR));
            bool chipPass = maxChip <= ChippingDepth;
            AddItem(r, "Chipping Top",    cT.ToString("F4"), cT <= ChippingDepth);
            AddItem(r, "Chipping Right",  cR.ToString("F4"), cR <= ChippingDepth);
            AddItem(r, "Chipping Bottom", cB.ToString("F4"), cB <= ChippingDepth);
            AddItem(r, "Chipping Left",   cL.ToString("F4"), cL <= ChippingDepth);
            long tChip = _sw.ElapsedMilliseconds;

            // 단계 이미지: 1_gray(다운스케일), 2_die_mask
            if (CaptureDebug) { foreach (var kv in _steps) { try { kv.Value?.Dispose(); } catch { } } _steps.Clear(); }

            // ── 3) 이물 — 다이 내부(4변 에지 안쪽, 노치/콜렛 제외)만 ContaminationDetector(Black-Hat). ──
            double foreignMm = 0; bool foreignPass = true; LastForeignCount = 0;
            if (UseContaminationInspection)
            {
                // 4변 모두 안쪽인 픽셀만 다이 + 에지에서 em px 안쪽으로 축소 = 외곽 경계선/노치/콜렛 배제
                // (외곽 점진적 경계를 포함하면 Black-Hat 이 그 선을 이물로 오검 → 표면 내부만 검사).
                // em = ForeignEdgeMargin(레시피 노출) — 사용자가 가장자리 검사 깊이를 직접 조정.
                int em = Math.Max(0, ForeignEdgeMargin);
                var dieMask = new byte[w * h];
                int rxc = Math.Min(rx, w - 1);
                Parallel.For(lx, rxc + 1, x =>
                {
                    if (topE[x] < 0 || botE[x] < 0) return;
                    for (int y = topE[x] + em; y <= botE[x] - em && y < h; y++)
                        if (leftE[y] >= 0 && rightE[y] >= 0 && x >= leftE[y] + em && x <= rightE[y] - em)
                            dieMask[y * w + x] = 1;
                });
                if (CaptureDebug)
                {
                    _steps.Add(Step("1_gray", ToBmpDs(g, w, h, false, 0, null)));
                    _steps.Add(Step("2_die_mask", ToBmpDs(g, w, h, false, 0, dieMask)));
                }
                int count;
                foreignMm = Foreign(g, w, h, dieMask, lx, ty, rx, by, r.Defects, out count);
                LastForeignCount = count;
                foreignPass = (count == 0) && (foreignMm <= ForeignObjectSize);
                AddItem(r, "Foreign Count", count.ToString(),         foreignPass);
                AddItem(r, "Foreign Max",   foreignMm.ToString("F4"), foreignPass);
                AddItem(r, "Foreign Backend", GpuBackend.Last("Contamination").ToString(), true);  // CPU/CUDA 표시
            }
            long tFor = _sw.ElapsedMilliseconds;

            // 단계별 소요(ms) — 속도 분석용. gray=그레이변환, die=4변검출, chip=칩핑, foreign=이물.
            AddItem(r, "t_total",   tFor.ToString(), true);
            AddItem(r, "t_gray",    tGray.ToString(), true);
            AddItem(r, "t_die",     (tDie  - tGray).ToString(), true);
            AddItem(r, "t_chip",    (tChip - tDie ).ToString(), true);
            AddItem(r, "t_foreign", (tFor  - tChip).ToString(), true);

            r.IsPass = sizePass && chipPass && foreignPass;

            // 오버레이 기하(다이 박스)
            LastCorners = new[]
            {
                new PointF(roi.X + lx, roi.Y + ty), new PointF(roi.X + rx, roi.Y + ty),
                new PointF(roi.X + rx, roi.Y + by), new PointF(roi.X + lx, roi.Y + by)
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

        /// <summary>제품(다이) 4변 에지 검출 — CDT-310 FindLine 동일(콜렛/배경→다이 전이 = 첫 밝은 교차).
        /// 흰배경(앞에 dark 없어 스킵)/회색콜렛/흰다이 모두 처리. 각 열·행의 에지 + 4변 중앙값 반환.</summary>
        private static bool FindDie(byte[] g, int w, int h, int thr,
            out int[] topE, out int[] botE, out int[] leftE, out int[] rightE,
            out int ty, out int by, out int lx, out int rx)
        {
            // 각 열/행 독립 → Parallel.For 로 병렬화(대형 12000² 이미지에서 단일스레드 대비 코어수만큼 단축).
            var tE = new int[w]; var bE = new int[w];
            Parallel.For(0, w, x =>
            {
                tE[x] = -1; bE[x] = -1;
                for (int y = 2; y < h; y++)
                    if (g[y * w + x] > thr && g[(y - 1) * w + x] <= thr && g[(y - 2) * w + x] <= thr) { tE[x] = y; break; }
                for (int y = h - 3; y >= 0; y--)
                    if (g[y * w + x] > thr && g[(y + 1) * w + x] <= thr && g[(y + 2) * w + x] <= thr) { bE[x] = y; break; }
            });
            topE = tE; botE = bE;
            var lE = new int[h]; var rE = new int[h];
            Parallel.For(0, h, y =>
            {
                lE[y] = -1; rE[y] = -1;
                int row = y * w;
                for (int x = 2; x < w; x++)
                    if (g[row + x] > thr && g[row + x - 1] <= thr && g[row + x - 2] <= thr) { lE[y] = x; break; }
                for (int x = w - 3; x >= 0; x--)
                    if (g[row + x] > thr && g[row + x + 1] <= thr && g[row + x + 2] <= thr) { rE[y] = x; break; }
            });
            leftE = lE; rightE = rE;
            ty = MedianValid(topE); by = MedianValid(botE); lx = MedianValid(leftE); rx = MedianValid(rightE);
            return ty >= 0 && by > ty && lx >= 0 && rx > lx;
        }

        /// <summary>에지 라인(중앙값) 대비 안쪽 결손 최대[px]. a~b 범위, sign+1=상/좌, -1=하/우. vertical=true 면 인덱스=y.</summary>
        private static double MaxInwardDev(int[] edge, int a, int b, int baseline, int sign, bool vertical, List<DefectMark> defects)
        {
            int maxDev = 0, maxIdx = -1;
            for (int i = a; i <= b; i++)
            {
                if (i < 0 || i >= edge.Length || edge[i] < 0) continue;
                int dev = sign * (edge[i] - baseline);   // 안쪽으로 들어오면 양수
                if (dev > maxDev) { maxDev = dev; maxIdx = i; }
            }
            if (maxDev > 2 && maxIdx >= 0)
            {
                double dx = vertical ? edge[maxIdx] : maxIdx;   // vertical(좌/우): 인덱스=y, edge=x
                double dy = vertical ? maxIdx : edge[maxIdx];
                defects.Add(new DefectMark { X = dx, Y = dy, Width = 10, Height = 10, Area = maxDev, Type = "Chipping" });
            }
            return maxDev;
        }

        /// <summary>유효(>=0) 값들의 중앙값. 없으면 -1.</summary>
        private static int MedianValid(int[] a)
        {
            var list = new List<int>();
            foreach (int v in a) if (v >= 0) list.Add(v);
            if (list.Count == 0) return -1;
            list.Sort();
            return list[list.Count / 2];
        }

        /// <summary>상단 에지 점 (x, edge[x]) 최소제곱 기울기 → 다이 회전각[°]. CDT-310 BottomResult.Angle.</summary>
        private static double EdgeAngleDeg(int[] edge, int a, int b)
        {
            double n = 0, sx = 0, sy = 0, sxx = 0, sxy = 0;
            for (int x = a; x <= b; x++)
            {
                if (x < 0 || x >= edge.Length || edge[x] < 0) continue;
                n++; sx += x; sy += edge[x]; sxx += (double)x * x; sxy += (double)x * edge[x];
            }
            if (n < 2) return 0;
            double den = n * sxx - sx * sx;
            if (Math.Abs(den) < 1e-9) return 0;
            double slope = (n * sxy - sx * sy) / den;
            return Math.Atan(slope) * 180.0 / Math.PI;
        }

        /// <summary>이물: CDT-310 ContaminationDetector(Black-Hat) — 칩 내부만. 대형 이미지(12000² 등)는
        /// 정수배 다운스케일 후 검출(메모리/시간 안전), 결과는 원본 스케일로 환산. 반환=최대 결함 크기[mm], out count=개수.</summary>
        private double Foreign(byte[] g, int w, int h, byte[] mask, int x0, int y0, int x1, int y1, List<DefectMark> defects, out int count)
        {
            count = 0;
            // 작업 픽셀이 ~9M(=3000²) 이하가 되도록 정수배 축소율 f 결정.
            const long MaxWork = 9_000_000;
            int f = 1; while ((long)(w / f) * (h / f) > MaxWork) f++;

            byte[] sg, sm; int sw, sh;
            if (f > 1) Downscale(g, mask, w, h, f, out sg, out sm, out sw, out sh);
            else { sg = g; sm = mask; sw = w; sh = h; }

            // 칩 배경(마스크 밖)을 칩 평균으로 채워 경계/배경 오검 방지.
            long sum = 0; int n = 0;
            for (int i = 0; i < sg.Length; i++) if (sm[i] != 0) { sum += sg[i]; n++; }
            byte mean = n > 0 ? (byte)(sum / n) : (byte)128;
            var masked = (byte[])sg.Clone();
            for (int i = 0; i < masked.Length; i++) if (sm[i] == 0) masked[i] = mean;

            int rad  = Math.Max(1, TopHatRadius / f);
            int minA = Math.Max(1, MinForeignAreaFilterSize / (f * f));
            int maxA = Math.Max(minA, MaxForeignAreaFilterSize / (f * f));
            // 병합거리: LinkDistance(310) 와 커널지름(2*rad+2) 중 큰 값.
            // 커널보다 큰 이물은 Black-Hat 이 사각커널 때문에 상/하/좌/우 4개 아크로 쪼개지므로,
            // 최소 커널지름만큼은 병합해야 한 덩어리로 합쳐진다(Python 검증: link=2*rad+2 → 1개).
            int link = Math.Max(Math.Max(1, LinkDistance / f), 2 * rad + 2);
            byte[] binary;
            var blobs = ContaminationDetector.Detect(masked, sw, sh, rad, TopHatThreshold, minA, maxA, link, out binary);
            if (CaptureDebug)
            {
                _steps.Add(Step("3_masked",  ToBmpDs(masked, sw, sh, false, 0, null)));   // 블랙햇 입력(다이만 흰색, 외부=평균)
                _steps.Add(Step("4_binary",  ToBmpDs(binary, sw, sh, true,  0, null)));   // 검출된 이물 후보(이진)
            }

            double maxMm = 0;
            foreach (var b in blobs)
            {
                int bw = b.Width * f, bh = b.Height * f;
                double szMm = Math.Max(bw * PixelSizeWidthMm, bh * PixelSizeHeightMm);
                if (szMm > maxMm) maxMm = szMm;
                defects.Add(new DefectMark { X = b.CenterX * f, Y = b.CenterY * f, Width = bw, Height = bh, Area = b.Area * f * f, Type = "Foreign" });
            }
            count = blobs.Count;
            return maxMm;
        }

        /// <summary>정수배 축소 — 그레이=블록평균, 마스크=다수결(절반 이상이 칩이면 칩).</summary>
        private static void Downscale(byte[] g, byte[] mask, int w, int h, int f,
            out byte[] sg, out byte[] sm, out int sw, out int sh)
        {
            int lsw = w / f, lsh = h / f;
            sw = lsw; sh = lsh;
            var lsg = new byte[lsw * lsh]; var lsm = new byte[lsw * lsh];
            Parallel.For(0, lsh, y =>
            {
                for (int x = 0; x < lsw; x++)
                {
                    int sx = x * f, sy = y * f, sumv = 0, cnt = 0, mcnt = 0;
                    for (int dy = 0; dy < f; dy++)
                        for (int dx = 0; dx < f; dx++)
                        {
                            int idx = (sy + dy) * w + (sx + dx);
                            sumv += g[idx]; cnt++;
                            if (mask[idx] != 0) mcnt++;
                        }
                    lsg[y * lsw + x] = (byte)(sumv / cnt);
                    lsm[y * lsw + x] = (byte)(mcnt * 2 >= cnt ? 1 : 0);
                }
            });
            sg = lsg; sm = lsm;
        }

        private static System.Collections.Generic.KeyValuePair<string, Bitmap> Step(string name, Bitmap bmp)
            => new System.Collections.Generic.KeyValuePair<string, Bitmap>(name, bmp);

        /// <summary>그레이 배열 → 다운스케일 24bpp 비트맵(최대 1500px). binarize=이진(≠0→흰), mask≠null 이면 마스크=초록 틴트.</summary>
        private static Bitmap ToBmpDs(byte[] gray, int w, int h, bool binarize, int thr, byte[] mask)
        {
            const int MaxDim = 1500;
            int f = 1; while (w / f > MaxDim || h / f > MaxDim) f++;
            int sw = Math.Max(1, w / f), sh = Math.Max(1, h / f);
            var bmp = new Bitmap(sw, sh, PixelFormat.Format24bppRgb);
            var data = bmp.LockBits(new Rectangle(0, 0, sw, sh), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = data.Stride; byte[] row = new byte[stride];
                for (int y = 0; y < sh; y++)
                {
                    for (int x = 0; x < sw; x++)
                    {
                        int sx = Math.Min(w - 1, x * f), sy = Math.Min(h - 1, y * f);
                        int idx = sy * w + sx;
                        byte v = gray[idx];
                        if (binarize) v = (byte)(v != 0 ? 255 : 0);
                        int o = x * 3;
                        if (mask != null && mask[idx] != 0) { row[o] = 0; row[o + 1] = v; row[o + 2] = 0; }  // 다이=초록
                        else { row[o] = row[o + 1] = row[o + 2] = v; }
                    }
                    System.Runtime.InteropServices.Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * stride), stride);
                }
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }

        private static byte[] ToGray(Bitmap bmp, Rectangle rect, out int w, out int h)
        {
            rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
            w = rect.Width; h = rect.Height;
            var gray = new byte[w * h];
            int lw = w, lh = h;
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int stride = data.Stride;
            var buf = new byte[stride * lh];
            try { Marshal.Copy(data.Scan0, buf, 0, buf.Length); }   // 전체 1회 복사 후 unlock → 그레이 변환은 병렬
            finally { bmp.UnlockBits(data); }
            // 144M 픽셀(12000²) 그레이 변환을 행 단위 병렬화.
            Parallel.For(0, lh, y =>
            {
                int rb = y * stride, gi = y * lw;
                for (int x = 0; x < lw; x++)
                {
                    int o = rb + x * 3;
                    gray[gi + x] = (byte)((buf[o] + buf[o + 1] + buf[o + 2]) / 3);
                }
            });
            return gray;
        }
    }
}
