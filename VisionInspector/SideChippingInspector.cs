using QMC.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// 사이드 치핑 검사를 전담하는 클래스
    /// </summary>
    public class SideChippingInspector
    {
        private VisionConfig _visionConfig;
        private QMC_FindChippingNForeign _findChippingTool;

        public SideChippingInspector(VisionConfig config)
        {
            _visionConfig = config ?? throw new ArgumentNullException(nameof(config));
            _findChippingTool = new QMC_FindChippingNForeign();
        }

        /// <summary>
        /// 사이드 이미지에서 치핑 검사 수행
        /// </summary>
        public SideChippingResult InspectChipping(ref byte[,]  image, int imageWidth, int imageHeight, 
            SideInspectionParameter parameter)
        {
            var result = new SideChippingResult();

            try
            {
                // ── 재설계(2026-07-12, 사용자 지시) ──
                // 측면 영상에는 밝은 띠가 2개(Z1/Z2 블레이드) 있고, 그 사이 어두운 틈은 치핑이 아니다.
                // 탑 치핑은 검사하지 않는다 — Bottom(최하단 에지) 치핑만 검사한다.
                // 방식: 컬럼별로 '아래에서 처음 만나는 밝은 띠'의 하단 에지를 서브픽셀(임계 교차 선형 보간)로
                // 찾고, 그 에지들을 최소제곱+이상치 제거로 피팅한 것이 기준 라인. 치핑 깊이 = 기준 라인에서
                // 컬럼별 실제 에지(서브픽셀)까지의 거리(위로 파인 것만). 에지 위쪽(띠 내부/블레이드 틈)은
                // 아예 보지 않으므로 틈 오검출이 원천 차단되고, 라인 피팅이 기울기를 흡수해 회전 보정도 불필요.
                int thr = parameter.Threshold;

                // 1. 컬럼별 최하단 에지(서브픽셀) 수집
                var colXs = new List<int>(imageWidth);
                var colEdges = new List<double>(imageWidth);
                for (int x = 0; x < imageWidth; x++)
                {
                    double e = FindBottomEdgeSubPixel(image, imageWidth, imageHeight, x, thr);
                    if (!double.IsNaN(e)) { colXs.Add(x); colEdges.Add(e); }
                }
                if (colXs.Count < 64)
                {
                    Log.Write("SideChippingInspector", "하단 에지 미검출(유효 컬럼 " + colXs.Count + "개) — 검사 불가");
                    result.IsSuccess = false;
                    return result;
                }

                // 2. 기준 라인 피팅 — 치핑 컬럼(에지가 위로 밀림)·버(아래로 튐)를 이상치로 걸러 기준면 유지.
                double la = 0, lb = 0;
                {
                    var fx = new List<double>(colXs.Count);
                    var fy = new List<double>(colEdges.Count);
                    for (int i = 0; i < colXs.Count; i++) { fx.Add(colXs[i]); fy.Add(colEdges[i]); }
                    FitLeastSquares(fx, fy, out la, out lb);
                    for (int pass = 0; pass < 2; pass++)   // 깊은 치핑이 1차 피팅을 끌고 갔을 때를 위한 2회 트림
                    {
                        var tx = new List<double>(fx.Count);
                        var ty = new List<double>(fy.Count);
                        for (int i = 0; i < fx.Count; i++)
                            if (Math.Abs(fy[i] - (la * fx[i] + lb)) <= 1.0) { tx.Add(fx[i]); ty.Add(fy[i]); }
                        if (tx.Count < Math.Max(32, colXs.Count / 3)) break;   // 과도 트림 방지
                        FitLeastSquares(tx, ty, out la, out lb);
                        fx = tx; fy = ty;
                    }
                }
                Line bottomRefLine = new Line(la, lb);

                // 다이 기울기 감안(2026-07-12): 기준 라인 피팅이 기울기 자체는 흡수하지만, 세로 측정
                // 거리는 실제 법선(에지 면 수직) 깊이보다 1/cos(θ) 커진다 — cos(atan(기울기))로 환산.
                double tiltCos = Math.Cos(Math.Atan(la));

                // 3. 치핑 깊이 = 기준 라인 − 컬럼 에지(서브픽셀)의 법선 환산 거리. 위로 파인(+) 것만 치핑.
                //    기존 정책 유지: 유효 컬럼 양끝 15개 제외.
                var chipRegions = new List<ChippingRegion>();
                var vals = new List<double>(colXs.Count);
                var xs = new List<int>(colXs.Count);
                var y0s = new List<int>(colXs.Count);
                var y1s = new List<int>(colXs.Count);
                double maxChip = 0;
                int maxIdx = -1;
                for (int i = 0; i < colXs.Count; i++)
                {
                    double lineY = bottomRefLine.GetY(colXs[i]);
                    double depthPx = (lineY - colEdges[i]) * tiltCos;   // 잡은 에지 픽셀 ~ 기준 라인 법선 거리
                    double mm = depthPx > 0 ? ConvertPixelToMM(depthPx) : 0;
                    vals.Add(mm);
                    xs.Add(colXs[i]);
                    y0s.Add((int)Math.Round(colEdges[i]));
                    y1s.Add((int)Math.Round(lineY));
                }
                if (vals.Count >= 31)
                {
                    for (int i = 15; i < vals.Count - 15; i++)
                    {
                        if (vals[i] > maxChip) { maxChip = vals[i]; maxIdx = i; }
                    }
                }
                CollectChippingRegions(vals, xs, y0s, y1s, parameter.ChippingDepth, false, chipRegions);

                // 4. 결과 설정 — 탑 치핑은 미검사(0), Bottom 만 사용(2026-07-12 지시).
                result.TopChippingSize = 0;
                result.BottomChippingSize = maxChip;
                result.MaxChippingSize = maxChip;
                result.ChippingRegions = chipRegions;
                result.IsSuccess = true;

                // 6. 스펙 판정
                double chippingSpec = parameter.ChippingDepth;

                result.IsDefect = result.MaxChippingSize > chippingSpec;

                if (result.IsDefect)
                {
                    string strFileName = "d:\\Log\\Image\\Side";

                    CDTInspector.IfNotExistMakeFolder(strFileName);

                    strFileName += "\\" + parameter.WaferID;
                    CDTInspector.IfNotExistMakeFolder(strFileName);

                    strFileName += "\\NG\\";
                    CDTInspector.IfNotExistMakeFolder(strFileName);

                    strFileName += "X_" + parameter.IndexX.ToString();
                    strFileName += "_Y_" + parameter.IndexY.ToString();
                    strFileName += DateTime.Now.Ticks.ToString();

                    // NG 저장(2026-07-12 지시): 측면 영상은 작으므로 크랍하지 않고 '전체 이미지'에
                    // 스펙 초과 치핑 영역마다 박스 + 치핑 크기 텍스트를 그려 저장(바텀 불량 저장과 동일 취지).
                    var saveRegions = chipRegions.Count > 0
                        ? chipRegions
                        : (maxIdx >= 0   // 영역 집계가 비어도 최대 치핑 위치 1개는 표시(방어)
                            ? new List<ChippingRegion> { new ChippingRegion {
                                  XStart = xs[maxIdx], XEnd = xs[maxIdx], X = xs[maxIdx],
                                  StartY = Math.Min(y0s[maxIdx], y1s[maxIdx]), EndY = Math.Max(y0s[maxIdx], y1s[maxIdx]),
                                  SizeMM = maxChip } }
                            : new List<ChippingRegion>());
                    SaveChippingNgImage(image, imageWidth, imageHeight, saveRegions, strFileName);
                }

                    Log.Write("SideChippingInspector",
                        $"검사 완료 - Bottom(최하단 에지): {result.BottomChippingSize:F4}mm (Top 미검사), " +
                        $"기준라인 y={bottomRefLine.GetY(imageWidth / 2.0):F2}px(중앙), 기울기 {Math.Atan(la) * 180.0 / Math.PI:F4}°, " +
                        $"유효컬럼 {colXs.Count}, Spec: {chippingSpec:F4}mm, Defect: {result.IsDefect}");
            }
            catch (Exception ex)
            {
                Log.Write("SideChippingInspector", $"검사 중 오류: {ex.Message}");
                Log.Write(ex);
                result.IsSuccess = false;
            }

            return result;
        }

        /// <summary>치핑 NG 이미지 저장(2026-07-12 지시) — 측면 영상은 작으므로 크랍 없이 '전체 이미지'에
        /// 스펙 초과 치핑 영역마다 빨간 박스 + 치핑 크기(µm) 텍스트를 그려 저장한다(바텀 불량 저장과 동일 취지).
        /// 이미지/영역은 사본으로 캡처하고 PNG 인코드·쓰기는 ImageSaveQueue(비동기) — 검사 흐름 비차단.</summary>
        private void SaveChippingNgImage(byte[,] image, int width, int height, List<ChippingRegion> regions, string strFileName)
        {
            if (image == null || width <= 0 || height <= 0 || string.IsNullOrWhiteSpace(strFileName))
                return;

            // 비동기 저장 전 사본 고정 — 원본 배열/목록이 이후 재사용·변경되어도 저장 내용 불변.
            byte[,] copy = new byte[height, width];
            Buffer.BlockCopy(image, 0, copy, 0, width * height);
            var regionsCopy = regions != null ? regions.ToList() : new List<ChippingRegion>();

            ImageSaveQueue.Enqueue(() =>
            {
                try
                {
                    using (var bmp24 = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    {
                        // 그레이 → 24bpp 직접 기록
                        var rect = new Rectangle(0, 0, width, height);
                        var data = bmp24.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp24.PixelFormat);
                        try
                        {
                            unsafe
                            {
                                fixed (byte* pSrc = &copy[0, 0])
                                {
                                    byte* basePtr = (byte*)data.Scan0;
                                    int stride = data.Stride;
                                    for (int y = 0; y < height; y++)
                                    {
                                        byte* src = pSrc + (long)y * width;
                                        byte* dst = basePtr + (long)y * stride;
                                        for (int x = 0; x < width; x++)
                                        {
                                            byte v = src[x];
                                            int o = x * 3;
                                            dst[o] = v; dst[o + 1] = v; dst[o + 2] = v;
                                        }
                                    }
                                }
                            }
                        }
                        finally { bmp24.UnlockBits(data); }

                        using (Graphics g = Graphics.FromImage(bmp24))
                        using (var pen = new Pen(Color.Red, 2f))
                        using (var font = new Font("Arial", 12, FontStyle.Bold))
                        using (var textBrush = new SolidBrush(Color.Red))
                        using (var textBg = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                        {
                            foreach (var r in regionsCopy)
                            {
                                if (r == null) continue;
                                const int pad = 8;
                                int x0 = Math.Max(0, r.XStart - pad);
                                int y0 = Math.Max(0, r.StartY - pad);
                                int x1 = Math.Min(width - 1, r.XEnd + pad);
                                int y1 = Math.Min(height - 1, r.EndY + pad);
                                g.DrawRectangle(pen, x0, y0, Math.Max(1, x1 - x0), Math.Max(1, y1 - y0));

                                string txt = (r.SizeMM * 1000.0).ToString("F1") + " um";
                                SizeF sz = g.MeasureString(txt, font);
                                float tx = Math.Max(0, Math.Min(x0, width - sz.Width));
                                float ty = y0 - sz.Height - 2;                       // 기본: 박스 위
                                if (ty < 0) ty = Math.Min(height - sz.Height, y1 + 2);   // 위 공간 없으면 박스 아래
                                g.FillRectangle(textBg, tx, ty, sz.Width, sz.Height);
                                g.DrawString(txt, font, textBrush, tx, ty);
                            }
                        }

                        bmp24.Save(strFileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            });
        }

        /// <summary>컬럼 x 의 최하단 에지(서브픽셀) — '아래에서 처음 만나는 연속 2px 밝음(띠)'의 하단 경계를
        /// 임계 교차 선형 보간으로 반환(2026-07-12). 띠가 없으면 NaN. 에지 위쪽(띠 내부/블레이드 틈)은 보지 않는다.</summary>
        private static double FindBottomEdgeSubPixel(byte[,] image, int width, int height, int x, int threshold)
        {
            int yb = -1;
            for (int y = height - 1; y >= 1; y--)
            {
                if (image[y, x] >= threshold)
                {
                    if (image[y - 1, x] >= threshold) { yb = y; break; }   // 연속 2px 밝음 = 띠(고립 노이즈 배제)
                }
            }
            if (yb < 0) return double.NaN;
            if (yb >= height - 1) return yb;              // 이미지 최하단까지 밝음 — 경계 그대로
            int a = image[yb, x];                          // ≥ threshold (띠 하단 픽셀)
            int b = image[yb + 1, x];                      // < threshold (아래는 전부 어두움 — 스캔 순서상 보장)
            if (b >= threshold) return yb;                 // 방어(이론상 도달 불가)
            return yb + (double)(a - threshold) / (a - b); // 임계 교차 위치(yb ~ yb+1 사이)
        }

        /// <summary>스펙(specMM) 초과 컬럼을 x-연속(간격≤5px) 그룹으로 묶어 칩핑 영역 목록에 추가 — 다중 칩핑 검출/실측 마커용.</summary>
        private void CollectChippingRegions(List<double> vals, List<int> xs, List<int> y0s, List<int> y1s,
            double specMM, bool isTop, List<ChippingRegion> regions)
        {
            if (regions == null || specMM <= 0 || vals.Count < 15) return;
            ChippingRegion cur = null; int lastX = int.MinValue; double curMax = 0;
            for (int i = 15; i < vals.Count - 15; i++)
            {
                if (vals[i] <= specMM) continue;
                int x = xs[i];
                int ya = Math.Min(y0s[i], y1s[i]), yb = Math.Max(y0s[i], y1s[i]);
                if (cur == null || x - lastX > 5)
                {
                    curMax = vals[i];
                    cur = new ChippingRegion { XStart = x, XEnd = x, X = x, StartY = ya, EndY = yb,
                                               SizeMM = curMax, IsFrontSide = isTop };
                    regions.Add(cur);
                }
                else
                {
                    cur.XEnd = x;
                    if (ya < cur.StartY) cur.StartY = ya;
                    if (yb > cur.EndY)   cur.EndY   = yb;
                    if (vals[i] > curMax) { curMax = vals[i]; cur.SizeMM = curMax; }
                    cur.X = (cur.XStart + cur.XEnd) / 2;
                }
                cur.DepthPixels = cur.EndY - cur.StartY;
                lastX = x;
            }
        }

        // (2026-07-12) 구 상/하단 치핑 스캔(InspectTopChipping/InspectBottomChipping)·회전·마진 계산 제거 —
        // 최하단 에지 서브픽셀 + 기준 라인 거리 방식(InspectChipping 본문)으로 대체.
        // Z1/Z2 블레이드 사이 어두운 틈을 치핑으로 오검출하던 문제의 원천 차단.

        /// <summary>
        /// 픽셀을 mm로 변환
        /// </summary>
        private double ConvertPixelToMM(int pixels)
        {
            return ConvertPixelToMM((double)pixels);
        }

        /// <summary>픽셀(서브픽셀 소수 포함)을 mm 로 변환(2026-07-12) — 기준 라인 거리 기반 깊이용.</summary>
        private double ConvertPixelToMM(double pixels)
        {
            if (_visionConfig?.SideVisionFront == null || pixels <= 0)
                return 0;
            _visionConfig.SideVisionFront.PixelSizeHeightMm = 0.003125;
            _visionConfig.SideVisionFront.PixelSizeWidthMm = 0.003125;
            return pixels * _visionConfig.SideVisionFront.PixelSizeHeightMm;
        }

        private static void FitLeastSquares(List<double> xs, List<double> ys, out double a, out double b)
        {
            double sx = 0, sy = 0, sxx = 0, sxy = 0;
            int n = xs.Count;
            for (int i = 0; i < n; i++)
            {
                sx += xs[i]; sy += ys[i]; sxx += xs[i] * xs[i]; sxy += xs[i] * ys[i];
            }
            double den = n * sxx - sx * sx;
            if (Math.Abs(den) < 1e-9) { a = 0; b = n > 0 ? sy / n : 0; }
            else { a = (n * sxy - sx * sy) / den; b = (sy - a * sx) / n; }
        }

    }

    /// <summary>
    /// 사이드 치핑 검사 결과
    /// </summary>
    public class SideChippingResult
    {
        /// <summary>
        /// 검사 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 불량 여부
        /// </summary>
        public bool IsDefect { get; set; }

        /// <summary>
        /// 상단 최대 치핑 크기 (mm)
        /// </summary>
        public double TopChippingSize { get; set; }

        /// <summary>
        /// 하단 최대 치핑 크기 (mm)
        /// </summary>
        public double BottomChippingSize { get; set; }

        /// <summary>
        /// 전체 최대 치핑 크기 (mm)
        /// </summary>
        public double MaxChippingSize { get; set; }

        public int XIndex = 0, YIndex = 0;
        /// <summary>
        /// 검출된 치핑 영역 목록
        /// </summary>
        public List<ChippingRegion> ChippingRegions { get; set; } = new List<ChippingRegion>();
    }

    /// <summary>
    /// 치핑 영역 정보
    /// </summary>
    public class ChippingRegion
    {
        /// <summary>
        /// X 좌표(영역 중심)
        /// </summary>
        public int X { get; set; }

        /// <summary>영역 가로 시작/끝(px) — 실측 폭 마커용</summary>
        public int XStart { get; set; }
        public int XEnd { get; set; }

        /// <summary>
        /// 시작 Y 좌표
        /// </summary>
        public int StartY { get; set; }

        /// <summary>
        /// 종료 Y 좌표
        /// </summary>
        public int EndY { get; set; }

        /// <summary>
        /// 치핑 깊이 (픽셀)
        /// </summary>
        public int DepthPixels { get; set; }

        /// <summary>
        /// 치핑 크기 (mm)
        /// </summary>
        public double SizeMM { get; set; }

        /// <summary>
        /// 상단 치핑 여부 (false면 하단)
        /// </summary>
        public bool IsFrontSide { get; set; }
    }
}