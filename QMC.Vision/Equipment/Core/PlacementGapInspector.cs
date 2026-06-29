using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using VI = global::QMC.Vision.Inspector;   // 원본 검사 라이브러리(QMc.Vision.Inspector) 별칭

namespace QMC.Vision.Core
{
    /// <summary>
    /// 안착(Placement) 갭 검사 — CDT-310 <c>CDTInspector.DieGapInspect</c> 방식 충실 포팅(기하).
    /// 검사 ROI 안에서 임계값으로 중앙 다이를 잡고, 4변 각각 <b>인접 다이와의 틈(gap)</b>을 에지 스캔으로 측정한다.
    /// 4변 에지점으로 추세선(최소자승)을 적합 → 교점으로 4코너·중심·각도 산출.
    /// 각 변 gap.Avg 가 [Lower, Upper] 안이면 OK, 4변 모두 OK → PASS (310 JudgmentDieGapOK 동일).
    /// 패턴매칭(원점=ROI중심) 한계와 무관하게 다이의 '진짜 중심'(코너 평균)을 구한다.
    /// 백엔드 무관 — Cognex 카메라 그랩 이미지(Bitmap)에 직접 동작.
    /// </summary>
    public class PlacementGapInspector : IInspector
    {
        public string Id { get; }
        public Roi InspectionRoi { get; set; }

        /// <summary>다이/배경 분리 임계값(0~255).</summary>
        public double Threshold     { get; set; } = 128;
        /// <summary>변별 gap 하한. PixelSize 적용 시 mm, 아니면 px.</summary>
        public double GapLowerLimit { get; set; } = 0.0;
        /// <summary>변별 gap 상한(기본 px). PixelSize 설정 시 mm.</summary>
        public double GapUpperLimit { get; set; } = 50.0;
        /// <summary>가로 mm/pixel (0이면 px 단위로 측정·판정).</summary>
        public double PixelSizeXmm  { get; set; } = 0.0;
        /// <summary>세로 mm/pixel (0이면 px 단위로 측정·판정).</summary>
        public double PixelSizeYmm  { get; set; } = 0.0;
        /// <summary>gap 측정값 보정 오프셋(기본 0, px). 310은 mm기준 -0.025 사용.</summary>
        public double GapOffset     { get; set; } = 0.0;
        /// <summary>다이가 배경(틈)보다 어두우면 true. 기본 false(다이가 밝음) → 내부에서 반전해 310(다이=어두움) 기준으로 처리.</summary>
        public bool DarkDie { get; set; } = false;
        /// <summary>에지 검출 스텝(px). 310 기본 3. 큰 이미지/완만한 에지는 키운다.</summary>
        public int EdgeStep { get; set; } = 3;
        /// <summary>각 변 양끝 무시 비율(0~0.5). 310 기본 0.05.</summary>
        public double BandTrim { get; set; } = 0.05;
        /// <summary>gap 이상치 제거 표준편차 배수. 310 기본 2.0.</summary>
        public double OutlierSigma { get; set; } = 2.0;

        // ── 직전 검출 기하(화면 오버레이용, 이미지 좌표) ──
        public bool     LastValid   { get; private set; }
        public PointF[] LastCorners { get; private set; }
        public PointF   LastCenter  { get; private set; }
        public bool     LastPass    { get; private set; }
        public string   LastGapText { get; private set; }

        /// <summary>true(기본)=원본 QMc.Vision.Inspector.CDTInspector.DieGapInspect 사용, false=기존 C# 구현(InspectLegacy).</summary>
        public bool UseInspectorLib { get; set; } = true;
        private VI.CDTInspector _libInspector;

        public PlacementGapInspector(string id)
        {
            Id = id;
            InspectionRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
        }

        /// <summary>
        /// 안착/갭 검사 — 원본 <c>QMc.Vision.Inspector.CDTInspector.DieGapInspect</c> 사용(4변 갭/오프셋/각도/코너).
        /// 단일 Bitmap → ROI 그레이 → (다이=밝으면 반전) → DieGapInspectionParameter → InspectionResult 매핑.
        /// <c>UseInspectorLib=false</c> 거나 lib 예외 시 기존 C#(<see cref="InspectLegacy"/>)으로 폴백.
        /// </summary>
        public InspectionResult Inspect(Bitmap image)
        {
            if (!UseInspectorLib) return InspectLegacy(image);

            var res = new InspectionResult { RoiName = Id };
            LastValid = false;
            if (image == null) { res.IsPass = false; res.ErrorMessage = "no image"; return res; }
            try
            {
                Rectangle roi = ClampRoi(InspectionRoi, image.Width, image.Height);
                if (roi.Width < 16 || roi.Height < 16) { res.IsPass = false; res.ErrorMessage = "ROI too small"; return res; }

                byte[,] img = ToGray(image, roi);   // [y, x]
                int th = (int)Math.Round(Math.Max(0, Math.Min(255, Threshold)));
                // 다이가 배경보다 밝으면 반전 — lib FindDieGap 은 '다이=어두움(threshold 미만)' 기준.
                if (!DarkDie)
                {
                    int hh = img.GetLength(0), ww = img.GetLength(1);
                    for (int yy = 0; yy < hh; yy++)
                        for (int xx = 0; xx < ww; xx++)
                            img[yy, xx] = (byte)(255 - img[yy, xx]);
                    th = 255 - th;
                }
                int gw2 = img.GetLength(1), gh2 = img.GetLength(0);
                var flat = new byte[gw2 * gh2];
                System.Buffer.BlockCopy(img, 0, flat, 0, flat.Length);

                var param = new VI.DieGapInspectionParameter
                {
                    Image = flat,
                    ImageWidth = gw2,
                    ImageHeight = gh2,
                    Roi = new Rectangle(0, 0, gw2, gh2),
                    Threshold = th,
                    UpperLimit = GapUpperLimit,
                    LowerLimit = GapLowerLimit,
                    WaferID = "Empty",
                    IndexX = 1,
                    IndexY = 1,
                };
                VI.DieGapResult dg = GetLibInspector().DieGapInspect(param);
                if (dg != null && dg.Gaps != null)
                {
                    MapDieGapResult(dg, res, roi);
                    return res;
                }

                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "PlacementGapInspector", Id + " DieGapInspect 결과 없음 → 레거시 폴백");
                return InspectLegacy(image);
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "PlacementGapInspector", Id + " DieGapInspect 실패 → 레거시 폴백: " + ex.Message);
                try { return InspectLegacy(image); }
                catch (Exception ex2) { return new InspectionResult { RoiName = Id, IsPass = false, ErrorMessage = "placement gap inspect: " + ex2.Message }; }
            }
        }

        /// <summary>원본 검사기 1회 생성 + VisionConfig(TargetVision 픽셀사이즈) 주입. px 모드(픽셀사이즈 0)면 1.0 → 갭이 px 로 유지.</summary>
        private VI.CDTInspector GetLibInspector()
        {
            if (_libInspector == null)
            {
                _libInspector = new VI.CDTInspector();
                var cfg = new VI.VisionConfig();
                cfg.TargetVision.PixelSizeWidthMm  = PixelSizeXmm > 0 ? PixelSizeXmm : 1.0;
                cfg.TargetVision.PixelSizeHeightMm = PixelSizeYmm > 0 ? PixelSizeYmm : 1.0;
                _libInspector.SetVisionConfig(cfg);
            }
            return _libInspector;
        }

        /// <summary>DieGapResult → InspectionResult(Items/오버레이 기하). 항목명은 기존(InspectLegacy)과 동일 — 그리드/자재추적 호환.</summary>
        private void MapDieGapResult(VI.DieGapResult dg, InspectionResult res, Rectangle roi)
        {
            var gaps = dg.Gaps;
            double sx = PixelSizeXmm > 0 ? PixelSizeXmm : 1.0;
            double sy = PixelSizeYmm > 0 ? PixelSizeYmm : 1.0;

            bool okL = JudgeGap(gaps.Left), okR = JudgeGap(gaps.Right), okT = JudgeGap(gaps.Top), okB = JudgeGap(gaps.Bottom);
            bool pass = dg.DefectCode == 0;
            res.IsPass = pass;

            // 코너: lib 는 Roi=(0,0,..) 로 호출했으므로 ROI-로컬 좌표 → 이미지 좌표로 평행이동.
            PointF[] ic;
            if (dg.Corners != null && dg.Corners.Length == 4)
            {
                ic = new PointF[4];
                for (int i = 0; i < 4; i++) ic[i] = new PointF(dg.Corners[i].X + roi.X, dg.Corners[i].Y + roi.Y);
            }
            else
            {
                ic = new[] { new PointF(roi.X, roi.Y), new PointF(roi.Right, roi.Y), new PointF(roi.Right, roi.Bottom), new PointF(roi.X, roi.Bottom) };
            }
            PointF center = new PointF((ic[0].X + ic[1].X + ic[2].X + ic[3].X) / 4f, (ic[0].Y + ic[1].Y + ic[2].Y + ic[3].Y) / 4f);

            double nomX = roi.X + roi.Width / 2.0, nomY = roi.Y + roi.Height / 2.0;
            double offX = (center.X - nomX) * sx, offY = (center.Y - nomY) * sy;

            void AddG(string n, double v, bool ok) => res.Items.Add(new InspectionItem { Name = n, Value = v.ToString("F3"), IsPass = ok });
            AddG("Top gap min", gaps.Top.Min, okT);     AddG("Top gap max", gaps.Top.Max, okT);
            AddG("Right max",   gaps.Right.Max, okR);   AddG("Right min",  gaps.Right.Min, okR);
            AddG("Bottom gap",  gaps.Bottom.Max, okB);  AddG("Bottom min", gaps.Bottom.Min, okB);
            AddG("Left max",    gaps.Left.Max, okL);     AddG("Left min",  gaps.Left.Min, okL);
            res.Items.Add(new InspectionItem { Name = "Offset X", Value = offX.ToString("F4"), IsPass = true });
            res.Items.Add(new InspectionItem { Name = "Offset Y", Value = offY.ToString("F4"), IsPass = true });
            res.Items.Add(new InspectionItem { Name = "Angle",    Value = dg.Angle.ToString("F4"), IsPass = true });
            AddG("Top Gap Avg", gaps.Top.Avg, okT);     AddG("Bottom Gap Avg", gaps.Bottom.Avg, okB);
            AddG("Left Gap Avg", gaps.Left.Avg, okL);   AddG("Right Gap Avg",  gaps.Right.Avg, okR);

            LastValid = true;
            LastCorners = ic;
            LastCenter = center;
            LastPass = pass;
            LastGapText = "L" + gaps.Left.Avg.ToString("F0") + " T" + gaps.Top.Avg.ToString("F0") +
                          " R" + gaps.Right.Avg.ToString("F0") + " B" + gaps.Bottom.Avg.ToString("F0");
        }

        private bool JudgeGap(VI.Gap g)
        {
            if (g == null) return false;
            if (g.Avg == 0) return true;
            if (g.Avg < GapLowerLimit || GapUpperLimit < g.Avg) return false;
            return true;
        }

        // ===== [LEGACY] 기존 순수 C# 구현 — UseInspectorLib=false 또는 lib 예외 시 폴백 =====
        private InspectionResult InspectLegacy(Bitmap image)
        {
            var res = new InspectionResult { RoiName = Id };
            LastValid = false;
            if (image == null) { res.IsPass = false; res.ErrorMessage = "no image"; return res; }
            try
            {
                Rectangle roi = ClampRoi(InspectionRoi, image.Width, image.Height);
                if (roi.Width < 16 || roi.Height < 16)
                { res.IsPass = false; res.ErrorMessage = "ROI too small"; return res; }

                byte[,] img = ToGray(image, roi);   // [y, x]
                int th = (int)Math.Round(Math.Max(0, Math.Min(255, Threshold)));

                // 다이가 배경보다 밝으면 반전 — 310 알고리즘은 '다이=어두움(threshold 미만)' 기준이므로.
                if (!DarkDie)
                {
                    for (int yy = 0; yy < img.GetLength(0); yy++)
                        for (int xx = 0; xx < img.GetLength(1); xx++)
                            img[yy, xx] = (byte)(255 - img[yy, xx]);
                    th = 255 - th;
                }

                // 분리 검증 — Threshold 가 다이/틈을 실제로 가르는지 확인.
                // 한쪽으로 쏠리면(예 Threshold=0 → 전부 밝음) 에지/틈을 못 찾아 gap=0 → 거짓 PASS 가 되므로 NG 로 명시.
                int gh = img.GetLength(0), gw = img.GetLength(1);
                long bright = 0;
                for (int y = 0; y < gh; y++)
                    for (int x = 0; x < gw; x++)
                        if (img[y, x] >= th) bright++;
                double frac = (double)bright / ((long)gh * gw);
                if (frac < 0.02 || frac > 0.98)
                {
                    res.IsPass = false;
                    res.ErrorMessage = "다이/틈 분리 안됨 — Threshold/ROI 확인 (bright=" + (frac * 100).ToString("F0") + "%)";
                    res.Items.Add(new InspectionItem { Name = "Detect", Value = "FAIL bright " + (frac * 100).ToString("F0") + "%", IsPass = false });
                    return res;
                }

                // EdgeStep 은 1 이상이어야 함 — 0이면 검출식(image[x]<thr && image[x-0]>=thr)이 항상 거짓이 되어
                // 에지를 못 찾고 코너가 ROI 중심으로 폴백(다이 안쪽처럼 보임) + 갭 0. 320 방어: 최소 1로 클램프.
                if (EdgeStep < 1) EdgeStep = 1;

                Gap left   = FindDieGapLeft  (img, th, out double aL, out Line lineLeft);
                Gap right  = FindDieGapRight (img, th, out double aR, out Line lineRight);
                Gap top    = FindDieGapTop   (img, th, out double aT, out Line lineTop);
                Gap bottom = FindDieGapBottom(img, th, out double aB, out Line lineBottom);

                // px → mm (PixelSize 0 이면 px 유지: 스케일 1)
                double sx = PixelSizeXmm > 0 ? PixelSizeXmm : 1.0;
                double sy = PixelSizeYmm > 0 ? PixelSizeYmm : 1.0;
                left.Mul(sx); right.Mul(sx); top.Mul(sy); bottom.Mul(sy);

                // 310 SetOffset(-0.025) 동일
                var gaps = new GapSet { Left = left, Right = right, Top = top, Bottom = bottom };
                gaps.SetOffset(GapOffset);

                // 각도 = top/bottom 편차 평균(310)
                double angle = AverageAngle(NormalizeAngle(aT), NormalizeAngle(aB));

                // 4코너(추세선 교점) — ROI 좌표 → 이미지 좌표
                PointF lt = Cross(lineLeft, lineTop, roi);
                PointF rt = Cross(lineRight, lineTop, roi);
                PointF rb = Cross(lineRight, lineBottom, roi);
                PointF lb = Cross(lineLeft, lineBottom, roi);
                PointF center = new PointF((lt.X + rt.X + rb.X + lb.X) / 4f, (lt.Y + rt.Y + rb.Y + lb.Y) / 4f);

                // 판정 (310 JudgmentDieGapOK)
                bool okL = Judge(left), okR = Judge(right), okT = Judge(top), okB = Judge(bottom);
                bool pass = okL & okR & okT & okB;

                res.IsPass = pass;
                // 안착 오프셋(다이중심 − ROI공칭중심) [mm 또는 px]
                double nomX = roi.X + roi.Width / 2.0, nomY = roi.Y + roi.Height / 2.0;
                double offX = (center.X - nomX) * sx, offY = (center.Y - nomY) * sy;
                // 운영뷰 그리드/차트는 Values(숫자 파싱)로 읽으므로 단위 접미사 없는 '순수 숫자'로 출력하고,
                // 이름도 뷰어 컬럼(Right max/Right min/Bottom gap/Offset X·Y/Angle 등)과 일치시킨다.
                void AddG(string n, double v, bool ok) => res.Items.Add(new InspectionItem { Name = n, Value = v.ToString("F3"), IsPass = ok });
                AddG("Top gap min", top.Min, okT);     AddG("Top gap max", top.Max, okT);
                AddG("Right max",   right.Max, okR);   AddG("Right min",  right.Min, okR);
                AddG("Bottom gap",  bottom.Max, okB);  AddG("Bottom min", bottom.Min, okB);
                AddG("Left max",    left.Max, okL);     AddG("Left min",  left.Min, okL);
                res.Items.Add(new InspectionItem { Name = "Offset X", Value = offX.ToString("F4"), IsPass = true });
                res.Items.Add(new InspectionItem { Name = "Offset Y", Value = offY.ToString("F4"), IsPass = true });
                res.Items.Add(new InspectionItem { Name = "Angle",    Value = angle.ToString("F4"), IsPass = true });
                // 자재추적(MaterialTracker.ApplyDieGap)용 평균 항목
                AddG("Top Gap Avg", top.Avg, okT);     AddG("Bottom Gap Avg", bottom.Avg, okB);
                AddG("Left Gap Avg", left.Avg, okL);   AddG("Right Gap Avg",  right.Avg, okR);

                // 화면 오버레이용 검출 기하 보존
                LastValid = true;
                LastCorners = new[] { lt, rt, rb, lb };
                LastCenter = center;
                LastPass = pass;
                LastGapText = "L" + left.Avg.ToString("F0") + " T" + top.Avg.ToString("F0") +
                              " R" + right.Avg.ToString("F0") + " B" + bottom.Avg.ToString("F0");

                // 검출 결과를 이미지로 저장(육안 확인) — 설정 '이미지 로그' 사용 + 경로 지정 시.
                TrySaveDetectImage(image, roi, lt, rt, rb, lb, center, left, top, right, bottom, pass);
                return res;
            }
            catch (Exception ex)
            {
                res.IsPass = false; res.ErrorMessage = "placement gap inspect: " + ex.Message; return res;
            }
        }

        // 310 JudgmentDieGapOK: avg==0 → 그 변 skip(OK), 아니면 Lower~Upper.
        // CDT-310 JudgmentDieGapOK 그대로: avg==0 → skip(OK), 그 외 [Lower, Upper] 벗어나면 NG. (0=무제한 처리 안 함)
        private bool Judge(Gap g)
        {
            if (g == null) return false;
            if (g.Avg == 0) return true;
            if (g.Avg < GapLowerLimit || GapUpperLimit < g.Avg) return false;
            return true;
        }

        // ── 4변 갭 검출 (310 FindDieGapLeft/Right/Top/Bottom 동일) ──

        private Gap FindDieGapLeft(byte[,] image, int threshold, out double angle, out Line line)
        {
            int width = image.GetLength(1), height = image.GetLength(0);
            int centerX = width / 2;
            line = new Line();
            int chipTop = -1, chipBottom = -1;
            for (int y = height / 2; y > 0; y--)
                if (image[y, centerX] < threshold && image[y - 1, centerX] >= threshold) { chipTop = y; break; }
            for (int y = height / 2; y < height - 1; y++)
                if (image[y, centerX] < threshold && image[y + 1, centerX] >= threshold) { chipBottom = y; break; }
            if (chipTop < 0 || chipBottom < 0 || chipBottom <= chipTop) { angle = 0; return new Gap(); }

            int yStart = chipTop + (int)((chipBottom - chipTop) * BandTrim);
            int yEnd   = chipBottom - (int)((chipBottom - chipTop) * BandTrim);
            var gaps = new List<int>(); var pts = new List<PointF>();
            for (int y = yStart; y < yEnd; y++)
            {
                int chipEdge = -1;
                for (int x = centerX; x > EdgeStep; x--)
                    if (image[y, x] < threshold && image[y, x - EdgeStep] >= threshold) { chipEdge = x; pts.Add(new PointF(x, y)); break; }
                if (chipEdge > 0 && chipEdge < width - 1)
                    for (int x = chipEdge - EdgeStep; x > EdgeStep; x--)
                        if (image[y, x] >= threshold && image[y, x - EdgeStep] < threshold) { gaps.Add(chipEdge - x); break; }
            }
            angle = pts.Count > 2 ? (line = new Line(pts)).GetAngle() : 0;
            gaps = RemoveGap(gaps);
            return new Gap { Min = gaps.Count > 0 ? gaps.Min() : 0, Max = gaps.Count > 0 ? gaps.Max() : 0 };
        }

        private Gap FindDieGapRight(byte[,] image, int threshold, out double angle, out Line line)
        {
            int width = image.GetLength(1), height = image.GetLength(0);
            int centerX = width / 2;
            line = new Line();
            int chipTop = -1, chipBottom = -1;
            for (int y = height / 2; y > 0; y--)
                if (image[y, centerX] < threshold && image[y - 1, centerX] >= threshold) { chipTop = y; break; }
            for (int y = height / 2; y < height - 1; y++)
                if (image[y, centerX] < threshold && image[y + 1, centerX] >= threshold) { chipBottom = y; break; }
            if (chipTop < 0 || chipBottom < 0 || chipBottom <= chipTop) { angle = 0; return new Gap(); }

            int yStart = chipTop + (int)((chipBottom - chipTop) * BandTrim);
            int yEnd   = chipBottom - (int)((chipBottom - chipTop) * BandTrim);
            var gaps = new List<int>(); var pts = new List<PointF>();
            for (int y = yStart; y < yEnd; y++)
            {
                int chipEdge = -1;
                for (int x = centerX; x < width - EdgeStep; x++)
                    if (image[y, x] < threshold && image[y, x + EdgeStep] >= threshold) { chipEdge = x; pts.Add(new PointF(x, y)); break; }
                if (chipEdge > 0 && chipEdge < width - EdgeStep)
                    for (int x = chipEdge + EdgeStep; x < width - EdgeStep; x++)
                        if (image[y, x] >= threshold && image[y, x + EdgeStep] < threshold) { gaps.Add(x - chipEdge); break; }
            }
            angle = pts.Count > 2 ? (line = new Line(pts)).GetAngle() : 0;
            gaps = RemoveGap(gaps);
            return new Gap { Min = gaps.Count > 0 ? gaps.Min() : 0, Max = gaps.Count > 0 ? gaps.Max() : 0 };
        }

        private Gap FindDieGapTop(byte[,] image, int threshold, out double angle, out Line line)
        {
            int width = image.GetLength(1), height = image.GetLength(0);
            int centerX = width / 2, centerY = height / 2;
            line = new Line();
            int chipLeft = -1, chipRight = -1;
            for (int x = width / 2; x > 0; x--)
                if (image[centerY, x] < threshold && image[centerY, x - 1] >= threshold) { chipLeft = x; break; }
            for (int x = width / 2; x < width - 1; x++)
                if (image[centerY, x] < threshold && image[centerY, x + 1] >= threshold) { chipRight = x; break; }
            if (chipLeft < 0 || chipRight < 0 || chipRight <= chipLeft) { angle = 0; return new Gap(); }

            int xStart = chipLeft + (int)((chipRight - chipLeft) * BandTrim);
            int xEnd   = chipRight - (int)((chipRight - chipLeft) * BandTrim);
            var gaps = new List<int>(); var pts = new List<PointF>();
            for (int x = xStart; x < xEnd; x++)
            {
                int chipEdge = -1;
                for (int y = centerY; y > EdgeStep; y--)
                    if (image[y, x] < threshold && image[y - EdgeStep, x] >= threshold) { chipEdge = y; pts.Add(new PointF(x, y)); break; }
                if (chipEdge > 0 && chipEdge < height - 1)
                    for (int y = chipEdge - EdgeStep; y > EdgeStep; y--)
                        if (image[y, x] >= threshold && image[y - EdgeStep, x] < threshold) { gaps.Add(chipEdge - y); break; }
            }
            angle = pts.Count > 2 ? (line = new Line(pts)).GetAngle() : 0;
            gaps = RemoveGap(gaps);
            return new Gap { Min = gaps.Count > 0 ? gaps.Min() : 0, Max = gaps.Count > 0 ? gaps.Max() : 0 };
        }

        private Gap FindDieGapBottom(byte[,] image, int threshold, out double angle, out Line line)
        {
            int width = image.GetLength(1), height = image.GetLength(0);
            int centerX = width / 2, centerY = height / 2;
            line = new Line();
            int chipLeft = -1, chipRight = -1;
            for (int x = width / 2; x > 0; x--)
                if (image[centerY, x] < threshold && image[centerY, x - 1] >= threshold) { chipLeft = x; break; }
            for (int x = width / 2; x < width - 1; x++)
                if (image[centerY, x] < threshold && image[centerY, x + 1] >= threshold) { chipRight = x; break; }
            if (chipLeft < 0 || chipRight < 0 || chipRight <= chipLeft) { angle = 0; return new Gap(); }

            int xStart = chipLeft + (int)((chipRight - chipLeft) * BandTrim);
            int xEnd   = chipRight - (int)((chipRight - chipLeft) * BandTrim);
            var gaps = new List<int>(); var pts = new List<PointF>();
            for (int x = xStart; x < xEnd; x++)
            {
                int chipEdge = -1;
                for (int y = centerY; y < height - EdgeStep; y++)
                    if (image[y, x] < threshold && image[y + EdgeStep, x] >= threshold) { chipEdge = y; pts.Add(new PointF(x, y)); break; }
                if (chipEdge > 0 && chipEdge < height - EdgeStep)
                    for (int y = chipEdge + EdgeStep; y < height - EdgeStep; y++)
                        if (image[y, x] >= threshold && image[y + EdgeStep, x] < threshold) { gaps.Add(y - chipEdge); break; }
            }
            angle = pts.Count > 2 ? (line = new Line(pts)).GetAngle() : 0;
            gaps = RemoveGap(gaps);
            return new Gap { Min = gaps.Count > 0 ? gaps.Min() : 0, Max = gaps.Count > 0 ? gaps.Max() : 0 };
        }

        // 310 RemoveGap: 0 제거 + 평균±2σ 이내만.
        private List<int> RemoveGap(List<int> gaps)
        {
            if (gaps == null || gaps.Count == 0) return new List<int>();
            var f = gaps.Where(g => g > 0).ToList();
            if (f.Count < 3) return f;
            double avg = f.Average();
            double std = Math.Sqrt(f.Average(v => Math.Pow(v - avg, 2)));
            return f.Where(g => Math.Abs(g - avg) <= OutlierSigma * std).ToList();
        }

        private static double NormalizeAngle(double a)
        {
            while (a > 90) a -= 180;
            while (a < -90) a += 180;
            return a;
        }

        private static double AverageAngle(double t, double b)
        {
            int c = 0; double s = 0;
            if (t != 0) { c++; s += t; }
            if (b != 0) { c++; s += b; }
            return c > 0 ? s / c : 0;
        }

        private static PointF Cross(Line a, Line b, Rectangle roi)
        {
            if (a != null && b != null && a.GetCrossPoint(b, out double x, out double y))
                return new PointF((float)(x + roi.X), (float)(y + roi.Y));
            return new PointF(roi.X + roi.Width / 2f, roi.Y + roi.Height / 2f);
        }

        private static Rectangle ClampRoi(Roi roi, int iw, int ih)
        {
            if (roi == null) return new Rectangle(0, 0, iw, ih);
            var b = roi.BoundingBox; b.Intersect(new Rectangle(0, 0, iw, ih)); return b;
        }

        // Bitmap ROI → 8bit 그레이 [y, x] (LockBits, 고속).
        private static byte[,] ToGray(Bitmap bmp, Rectangle roi)
        {
            var gray = new byte[roi.Height, roi.Width];
            BitmapData data = bmp.LockBits(roi, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = data.Stride;
                byte[] buf = new byte[stride * roi.Height];
                Marshal.Copy(data.Scan0, buf, 0, buf.Length);
                for (int y = 0; y < roi.Height; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < roi.Width; x++)
                    {
                        int p = row + x * 4;   // BGRA
                        gray[y, x] = (byte)((buf[p] + buf[p + 1] + buf[p + 2]) / 3);
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
            return gray;
        }

        // 검출 오버레이 이미지 저장 — 설정 ImageLogEnable + ImageLogPath 일 때만. {경로}/Detect/{시각}_{PASS|NG}.png
        private void TrySaveDetectImage(Bitmap src, Rectangle roi,
                                        PointF lt, PointF rt, PointF rb, PointF lb, PointF center,
                                        Gap l, Gap t, Gap r, Gap b, bool pass)
        {
            try
            {
                var cfg = QMC.Vision.Config.VisionConfigStore.Current;
                if (cfg == null || !cfg.ImageLogEnable || string.IsNullOrWhiteSpace(cfg.ImageLogPath)) return;
                // 레시피별 로그 토글 + OK/NG/ALL 필터 (활성 레시피 미등록 시 안전 기본값으로 통과).
                var recipe = QMC.Vision.Core.ActiveRecipeContext.Current;
                if (recipe != null && !recipe.LogEnable) return;
                if (!QMC.Vision.Core.ImageLogSaver.ShouldSaveForMode(
                        recipe?.ImageSaveMode ?? QMC.Vision.Modules.ImageSaveMode.ALL, pass)) return;
                string dir = System.IO.Path.Combine(cfg.ImageLogPath, "Detect");
                System.IO.Directory.CreateDirectory(dir);

                using (var crop = new Bitmap(roi.Width, roi.Height))
                {
                    using (var g = Graphics.FromImage(crop))
                    {
                        g.DrawImage(src, new Rectangle(0, 0, roi.Width, roi.Height), roi, GraphicsUnit.Pixel);
                        PointF[] poly = { Local(lt, roi), Local(rt, roi), Local(rb, roi), Local(lb, roi) };
                        using (var pen = new Pen(pass ? Color.Lime : Color.OrangeRed, 3f))
                            g.DrawPolygon(pen, poly);
                        using (var cb = new SolidBrush(Color.Lime))
                            g.FillEllipse(cb, center.X - roi.X - 5, center.Y - roi.Y - 5, 10, 10);
                        using (var f = new Font("Consolas", 14f, FontStyle.Bold))
                        using (var tb = new SolidBrush(pass ? Color.Lime : Color.OrangeRed))
                        using (var bgb = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                        {
                            string txt = (pass ? "PASS" : "NG") +
                                "  L" + l.Avg.ToString("F0") + " T" + t.Avg.ToString("F0") +
                                " R" + r.Avg.ToString("F0") + " B" + b.Avg.ToString("F0");
                            var sz = g.MeasureString(txt, f);
                            g.FillRectangle(bgb, 6, 6, sz.Width + 6, sz.Height + 4);
                            g.DrawString(txt, f, tb, 9, 8);
                        }
                    }
                    string name = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + (pass ? "PASS" : "NG") + ".png";
                    crop.Save(System.IO.Path.Combine(dir, name), ImageFormat.Png);
                }
            }
            catch { }
        }

        private static PointF Local(PointF p, Rectangle roi) => new PointF(p.X - roi.X, p.Y - roi.Y);

        // ── 310 Gap / GapSet / Line 충실 포팅(내부) ──

        private sealed class Gap
        {
            public double Min { get; set; }
            public double Max { get; set; }
            public double Avg => (Min + Max) / 2.0;
            public void Mul(double s) { Min *= s; Max *= s; }
        }

        private sealed class GapSet
        {
            public Gap Left = new Gap(), Top = new Gap(), Right = new Gap(), Bottom = new Gap();
            public void SetOffset(double d)
            {
                Off(Left, d); Off(Right, d); Off(Top, d); Off(Bottom, d);
            }
            private static void Off(Gap g, double d)
            {
                if (g.Max != 0) { g.Max += d; g.Min += d; }
                if (g.Max < 0 || g.Min <= 0) { g.Max = 0; g.Min = 0; }
            }
        }

        // 310 Line: y = mA*x + mB (mA=무한대면 수직선 x=mB). 점목록 최소자승 적합.
        private sealed class Line
        {
            public double mA, mB;
            public Line() { }
            public Line(double a, double b) { mA = a; mB = b; }
            public Line(List<PointF> pts) { GetTrendLine(pts, out mA, out mB); }

            public double GetAngle() => Math.Atan2(mA, 1) * (180.0 / Math.PI);

            public bool GetCrossPoint(Line line, out double dX, out double dY)
            {
                dX = 0; dY = 0;
                if (line == null) return false;
                if (mA == line.mA) return false;
                if (double.IsPositiveInfinity(mA)) { dX = mB; dY = line.mA == 0 ? line.mB : line.mA * dX + line.mB; return true; }
                if (double.IsPositiveInfinity(line.mA)) { dX = line.mB; dY = mA == 0 ? mB : mA * dX + mB; return true; }
                if (mA == 0) { dY = mB; dX = (dY - line.mB) / line.mA; return true; }
                if (line.mA == 0) { dY = line.mB; dX = (dY - mB) / mA; return true; }
                dX = (line.mB - mB) / (mA - line.mA); dY = mA * dX + mB; return true;
            }

            // 310 FindLine.GetTrendLine — 최소자승(수직 근접 시 X/Y 스왑).
            private static void GetTrendLine(List<PointF> listPoint, out double dA, out double dB)
            {
                dA = 0; dB = 0;
                if (listPoint == null || listPoint.Count < 2) return;
                var pts = new List<PointF>(listPoint);
                double xr = pts.Max(t => t.X) - pts.Min(t => t.X);
                double yr = pts.Max(t => t.Y) - pts.Min(t => t.Y);
                if (xr == 0 && yr == 0) return;
                bool vert = false;
                if (xr < yr) { for (int i = 0; i < pts.Count; i++) pts[i] = new PointF(pts[i].Y, pts[i].X); vert = true; }
                int n = pts.Count;
                double sx = 0, sy = 0, sxx = 0, sxy = 0;
                foreach (var v in pts) { sx += v.X; sy += v.Y; sxx += v.X * v.X; sxy += v.X * v.Y; }
                double den = n * sxx - sx * sx;
                if (Math.Abs(den) < 1e-9)
                {
                    if (vert) { dA = double.PositiveInfinity; dB = pts.Average(p => p.Y); }
                    else { dA = 0; dB = pts.Average(p => p.Y); }
                    return;
                }
                dA = (n * sxy - sx * sy) / den;
                dB = (sxx * sy - sx * sxy) / den;
                if (vert)
                {
                    double a = dA, b = dB;
                    if (Math.Abs(a) < 1e-9) { dA = double.PositiveInfinity; dB = pts.Average(p => p.Y); }
                    else { dA = 1 / a; dB = -b / a; }
                }
            }
        }
    }
}
