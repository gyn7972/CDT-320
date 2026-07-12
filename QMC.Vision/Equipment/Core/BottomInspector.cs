using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VI = global::QMC.Vision.Inspector;   // 원본 검사 라이브러리(QMc.Vision.Inspector) 별칭. global:: 로 QMC.Vision.Core 안에서의 네임스페이스 중첩 해석 방지

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

        /// <summary>true(기본)=원본 QMc.Vision.Inspector(CDTInspector.BottomInspect) 사용, false=기존 C# 구현(InspectLegacy).</summary>
        public bool UseInspectorLib { get; set; } = true;

        // 원본 라이브러리 검사기(생성자에서 12000² 버퍼 초기화 → 1회만 생성/재사용).
        private VI.CDTInspector _libInspector;

        // lib(BottomInspect)가 연속 null 이면 이후 lib 시도(그레이 변환+파라미터 구성 ~1초/회)를 생략하고 바로 레거시로.
        // 성공이 한 번이라도 나오면 리셋. 결과/판정에는 영향 없음(어차피 레거시 폴백이던 경로의 낭비 제거).
        private int _libNullStreak;
        private const int LibNullSkipThreshold = 2;

        public BottomInspector(string id)
        {
            Id = id;
            InspectionRoi = new Roi { Name = id + ".Roi", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
        }

        /// <summary>
        /// Bottom 검사 — 원본 <c>QMc.Vision.Inspector.CDTInspector.BottomInspect</c> 알고리즘 사용.
        /// 단일 Bitmap → ROI 그레이(byte[]) 1장으로 <c>BottomInspectionParameter</c> 구성 → 호출 → <c>InspectionResult</c> 매핑.
        /// <c>UseInspectorLib=false</c> 거나 라이브러리에서 예외 발생 시 기존 C# 구현(<see cref="InspectLegacy"/>)으로 폴백.
        /// </summary>
        public InspectionResult Inspect(Bitmap image)
        {
            //if (!UseInspectorLib)
            //    return InspectLegacy(image);
            //if (_libNullStreak >= LibNullSkipThreshold)
            //    return InspectLegacy(image);   // lib 연속 실패 래치 — 불필요한 이중 그레이변환/lib 호출 생략

            var r = new InspectionResult { RoiName = Id, IsPass = true };
            LastValid = false;
            byte[] rented1x = null;   // 풀 대여 버퍼 추적 — finally 에서 반납(2026-07-12)
            try
            {
                if (image == null) { r.ErrorMessage = "no image"; r.IsPass = false; return r; }

                Rectangle libRoi = InspectionRoi != null ? InspectionRoi.BoundingBox : new Rectangle(0, 0, image.Width, image.Height);
                libRoi.Intersect(new Rectangle(0, 0, image.Width, image.Height));
                if (libRoi.Width <= 4 || libRoi.Height <= 4) { r.ErrorMessage = "roi empty"; r.IsPass = false; return r; }

                // 실제 검사 모드 계약(2026-07-12, 사용자 확정 — lib 의 bSimulate=false 고정):
                // '1배 원본 전체' 그레이를 넣고, lib 가 FindChipCenter 로 ChipRoi 를 다이 중심에 재배치해
                // 크롭 → GPU 2배 확장(ctx) → 검사한다. 여기서 미리 2배 확장하면 이중 확장(527MP)으로
                // 검사가 수십 초로 느려지고 W/H·좌표가 2배 왜곡된다(2026-07-12 실장비 확인) — 사전 확장 금지.
                int gw, gh;
                byte[] gray = ToGray(image, new Rectangle(0, 0, image.Width, image.Height), out gw, out gh);
                rented1x = gray;

                // 이미지 저장 경로: 설정→일반(VisionSettings.ImageLogPath) 우선, 비어있으면 FileSavePath 폴백.
                string saveRoot = QMC.Vision.Config.VisionConfigStore.Current?.ImageLogPath;
                if (string.IsNullOrWhiteSpace(saveRoot)) saveRoot = FileSavePath;

                var bip = new VI.BottomInspectionParameter
                {
                    Images = new List<byte[]> { gray },
                    ImageWidth = gw,
                    ImageHeight = gh,
                    ChipRoi = libRoi,   // 위치는 lib 가 다이 중심으로 재배치 — 검사 창 크기(W/H)만 유효
                    Threshold = ChipThreshold,
                    SelectedChipType = DarkChip ? VI.InspectionParameterBase.ChipType.Black : VI.InspectionParameterBase.ChipType.White,
                    ChippingDepth = ChippingDepth,
                    ChippingLength = ChippingLength,
                    ChipLowerSpecLimit = ChipLowerSpecLimit,
                    ChipUpperSpecLimit = ChipUpperSpecLimit,
                    ForeignObjectSize = ForeignObjectSize,
                    FirstPeekValueThreshold = FirstPeekValueThreshold,
                    PeekValueThreshold = PeekValueThreshold,
                    Stdev = Stdev,
                    TopHatRadius = TopHatRadius,
                    TopHatThreshold = TopHatThreshold,
                    MinForeignAreaFilterSize = MinForeignAreaFilterSize,
                    LinkDistance = LinkDistance,
                    PortentiolDefactMinSize = PortentiolDefactMinSize,
                    UseContaminationInspection = UseContaminationInspection,
                    IsSaveGoodImage = false,
                    FileSavePath = saveRoot,
                };

                // 저장 경로 폴더 보장(없으면 생성) — BottomInspect 내부 finally 의 이미지 저장이 경로 없음으로 실패하지 않도록.
                try
                {
                    if (!string.IsNullOrEmpty(saveRoot))
                        System.IO.Directory.CreateDirectory(saveRoot);
                }
                catch (Exception exDir)
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "BottomInspector", Id + " 이미지 저장 경로 생성 실패(" + saveRoot + "): " + exDir.Message);
                }

                VI.CDTInspector inspector = GetLibInspector();
                VI.BottomResult br = inspector.BottomInspect(bip);

                if (br == null)
                {
                    // BottomInspect 가 null(칩 미검출/스펙 미설정 등) → 레거시로 폴백해 결과+오버레이(다이박스/이물·칩핑 마커/라벨)를 그대로 표시(원래 동작 유지).
                    _libNullStreak++;
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "BottomInspector", Id + " BottomInspect null → 레거시 폴백(오버레이/결과 유지)"
                        + (_libNullStreak == LibNullSkipThreshold ? " — 연속 " + LibNullSkipThreshold + "회, 이후 lib 시도 생략(성공 시 자동 복귀)" : ""));
                    return InspectLegacy(image);
                }

                _libNullStreak = 0;   // lib 성공 → 래치 해제
                // 실제모드에서 lib 결과 좌표는 (재배치된 ChipRoi 좌상단이 가산된) '1배 원본 전체' 좌표 —
                // 추가 ROI 오프셋 불필요 → roi=(0,0) 전달.
                MapLibResult(br, r, new Rectangle(0, 0, image.Width, image.Height), image.Width, image.Height);
                LastValid = true;
                return r;
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "BottomInspector", Id + " BottomInspect 실패 → 레거시 폴백: " + ex.Message);
                try
                {
                    return InspectLegacy(image);
                }
                catch (Exception ex2)
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "BottomInspector", Id + " 레거시 폴백도 실패: " + ex2.Message);
                    return new InspectionResult { RoiName = Id, IsPass = false, ErrorMessage = "Bottom 검사 실패: " + ex2.Message };
                }
            }
            finally
            {
                // 1배 원본 그레이는 BottomInspect 가 동기 완료(내부 크롭/확장 후 사용) 후라 반환 시점에 참조 없음.
                ReturnBuf(rented1x);
            }
        }

        /// <summary>원본 라이브러리 검사기 1회 생성 + VisionConfig(픽셀 사이즈) 주입.
        /// 설비 계약(2026-07-12): 실제 검사 모드(bSimulate=false) — 1배 원본 전체 입력, lib 가 크롭+2배 확장 수행.</summary>
        private VI.CDTInspector GetLibInspector()
        {
            if (_libInspector == null)
            {
                _libInspector = new VI.CDTInspector { bSimulate = false };
                var cfg = new VI.VisionConfig();
                cfg.BottomVision.PixelSizeWidthMm = PixelSizeWidthMm;
                cfg.BottomVision.PixelSizeHeightMm = PixelSizeHeightMm;
                _libInspector.SetVisionConfig(cfg);
            }
            return _libInspector;
        }

        /// <summary>BottomResult → InspectionResult(Items/Defects/IsPass) 매핑. 합/불은 원본 DefectCode(0=양품) 기준.</summary>
        private void MapLibResult(VI.BottomResult br, InspectionResult r, Rectangle roi, int imageW, int imageH)
        {
            bool sizePass = SpecOk(br.Width, ChipLowerSpecLimit.Width, ChipUpperSpecLimit.Width)
                         && SpecOk(br.Height, ChipLowerSpecLimit.Height, ChipUpperSpecLimit.Height);
            AddItem(r, "Width", br.Width.ToString("F4"), sizePass);
            AddItem(r, "Height", br.Height.ToString("F4"), sizePass);
            AddItem(r, "Angle", br.Angle.ToString("F3"), true);

            // BottomOffset = '화면(그랩 이미지) 센터' 기준 mm(2026-07-12 확정).
            // lib br.Offset = 다이 중심(px, ROI 크롭 좌표계) — deece5d5('W/H 바꾸는 코드 삭제') 이후
            // X/Y 스왑 없음. ROI 좌상단 가산(전체 이미지 좌표) → 이미지 센터 차감 → mm 변환.
            double dieCxPx = roi.X + br.Offset.X;
            double dieCyPx = roi.Y + br.Offset.Y;
            double offsetXmm = (dieCxPx - imageW / 2.0) * PixelSizeWidthMm;
            double offsetYmm = (dieCyPx - imageH / 2.0) * PixelSizeHeightMm;
            AddItem(r, "Offset X", offsetXmm.ToString("F4"), true);
            AddItem(r, "Offset Y", offsetYmm.ToString("F4"), true);

            AddItem(r, "Chipping Top", br.ChppingTopSize.ToString("F4"), br.ChppingTopSize <= ChippingDepth);
            AddItem(r, "Chipping Right", br.ChppingRightSize.ToString("F4"), br.ChppingRightSize <= ChippingDepth);
            AddItem(r, "Chipping Bottom", br.ChppingBottomSize.ToString("F4"), br.ChppingBottomSize <= ChippingDepth);
            AddItem(r, "Chipping Left", br.ChppingLeftSize.ToString("F4"), br.ChppingLeftSize <= ChippingDepth);
            if (br.Channel1ChippingSize > 0 || br.Channel2ChippingSize > 0)
            {
                AddItem(r, "Chipping ch1", br.Channel1ChippingSize.ToString("F4"), br.Channel1ChippingSize <= ChippingDepth);
                AddItem(r, "Chipping ch2", br.Channel2ChippingSize.ToString("F4"), br.Channel2ChippingSize <= ChippingDepth);
            }

            double foreignMm = Math.Max(br.ForeingSize, br.MaxDefactSize);
            AddItem(r, "Foreign Max", foreignMm.ToString("F4"), foreignMm <= ForeignObjectSize);

            // 이물 위치 마크 — lib 가 원본 입력 좌표(코너 규약)로 환산해 반환(2026-07-11). ROI 좌상단만 더해 전체 이미지 좌표로.
            if (br.ForeignInfos != null)
            {
                foreach (var fi in br.ForeignInfos)
                {
                    if (fi == null) continue;
                    r.Defects.Add(new DefectMark
                    {
                        X = roi.X + fi.Rect.X + fi.Rect.Width / 2.0,
                        Y = roi.Y + fi.Rect.Y + fi.Rect.Height / 2.0,
                        Width = fi.Rect.Width,
                        Height = fi.Rect.Height,
                        Area = fi.Area,
                        Type = "Foreign"
                    });
                }
                AddItem(r, "Foreign Count", br.ForeignInfos.Count.ToString(), true);
            }

            if (br.ChippingInfos != null)
            {
                foreach (var ci in br.ChippingInfos)
                {
                    if (ci.Contour == null || ci.Contour.Count == 0) continue;
                    float minx = float.MaxValue, miny = float.MaxValue, maxx = float.MinValue, maxy = float.MinValue;
                    foreach (var p in ci.Contour)
                    {
                        if (p.X < minx) minx = p.X;
                        if (p.Y < miny) miny = p.Y;
                        if (p.X > maxx) maxx = p.X;
                        if (p.Y > maxy) maxy = p.Y;
                    }
                    r.Defects.Add(new DefectMark
                    {
                        X = roi.X + (minx + maxx) / 2.0,
                        Y = roi.Y + (miny + maxy) / 2.0,
                        Width = maxx - minx,
                        Height = maxy - miny,
                        Area = ci.Length,
                        Type = "Chipping"
                    });
                }
            }

            if (br.Corners != null && br.Corners.Length == 4
                && !(br.Corners[0].IsEmpty && br.Corners[1].IsEmpty && br.Corners[2].IsEmpty && br.Corners[3].IsEmpty))
            {
                LastCorners = new[]
                {
                    new PointF(roi.X + br.Corners[0].X, roi.Y + br.Corners[0].Y),
                    new PointF(roi.X + br.Corners[1].X, roi.Y + br.Corners[1].Y),
                    new PointF(roi.X + br.Corners[2].X, roi.Y + br.Corners[2].Y),
                    new PointF(roi.X + br.Corners[3].X, roi.Y + br.Corners[3].Y)
                };
            }
            else
            {
                // lib 가 코너를 못 주면 최소한 ROI 박스라도 그려 오버레이가 비지 않도록.
                LastCorners = new[]
                {
                    new PointF(roi.X, roi.Y), new PointF(roi.Right, roi.Y),
                    new PointF(roi.Right, roi.Bottom), new PointF(roi.X, roi.Bottom)
                };
            }

            double chipMax = Math.Max(Math.Max(br.ChppingTopSize, br.ChppingBottomSize), Math.Max(br.ChppingLeftSize, br.ChppingRightSize));
            LastText = $"W {br.Width:F4} H {br.Height:F4} Chip {chipMax:F4} Foreign {foreignMm:F4}";

            r.IsPass = (br.DefectCode == 0);
        }

        // ===== [LEGACY] 기존 순수 C# 구현 — UseInspectorLib=false 또는 라이브러리 예외 시 폴백 =====
        private InspectionResult InspectLegacy(Bitmap image)
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

            // 각도(상단 에지 기울기) + 오프셋(다이 중심 − '화면(그랩 이미지) 센터', 2026-07-12 확정 —
            // 종전 ROI 중심 기준에서 변경: BottomOffset 은 화면 센터 기준 mm 로 나가야 한다. lib 경로(MapLibResult)와 동일 규약.
            double angleDeg = EdgeAngleDeg(topE, lx, rx);
            double cxImg = roi.X + (lx + rx) / 2.0, cyImg = roi.Y + (ty + by) / 2.0;
            double nomX = image.Width / 2.0;
            double nomY = image.Height / 2.0;
            double offXmm = (cxImg - nomX) * PixelSizeWidthMm;
            double offYmm = (cyImg - nomY) * PixelSizeHeightMm;
            AddItem(r, "Angle",    angleDeg.ToString("F3"), true);   // [°] 정보(스펙 별도)
            AddItem(r, "Offset X", offXmm.ToString("F4"),   true);   // [mm]
            AddItem(r, "Offset Y", offYmm.ToString("F4"),   true);

            // ── 2) 칩핑 — 4변 에지 라인(중앙값) 대비 안쪽 결손 최대. 검사영역은 ChipEdgeMargin 만큼 코너에서 줄임. ──
            int cm = Math.Max(0, ChipEdgeMargin);
            int specHpx = (int)Math.Ceiling(ChippingDepth / Math.Max(1e-9, PixelSizeHeightMm));   // 스펙[mm]→[px]
            int specWpx = (int)Math.Ceiling(ChippingDepth / Math.Max(1e-9, PixelSizeWidthMm));
            double cT = MaxInwardDev(topE,   lx + cm, rx - cm, ty, +1, false, r.Defects, specHpx) * PixelSizeHeightMm;
            double cB = MaxInwardDev(botE,   lx + cm, rx - cm, by, -1, false, r.Defects, specHpx) * PixelSizeHeightMm;
            double cL = MaxInwardDev(leftE,  ty + cm, by - cm, lx, +1, true,  r.Defects, specWpx) * PixelSizeWidthMm;
            double cR = MaxInwardDev(rightE, ty + cm, by - cm, rx, -1, true,  r.Defects, specWpx) * PixelSizeWidthMm;
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
                // 1) 다이 내부(4변 에지 안쪽) 원시 마스크 — 여기서는 em 미적용(노치/콜렛 윤곽을 그대로 반영).
                Parallel.For(lx, rxc + 1, x =>
                {
                    if (topE[x] < 0 || botE[x] < 0) return;
                    for (int y = topE[x]; y <= botE[x] && y < h; y++)
                        if (leftE[y] >= 0 && rightE[y] >= 0 && x >= leftE[y] && x <= rightE[y])
                            dieMask[y * w + x] = 1;
                });
                // 2) em(ForeignEdgeMargin) 여백은 Foreign() 내부에서 '다운스케일 후 저해상도 마스크'에
                //    등방(정사각) 침식으로 적용한다. 풀해상도 침식 대비 ~f² 배 저렴(속도 유지).
                //    이렇게 하면 외곽선뿐 아니라 상/하 변 가운데 노치의 '세로 측벽'까지 균일하게 em 여백이 생겨,
                //    측벽에 붙은 표면 1열이 Black-Hat 에서 가는 세로 슬리버로 이물 오검되던 문제가 해소된다.
                //    (노치 = 칩핑 알고리즘이 별도 판정하므로 이물에서 중복 검출할 필요가 없다.)
                if (CaptureDebug)
                {
                    _steps.Add(Step("1_gray", ToBmpDs(g, w, h, false, 0, null)));
                    _steps.Add(Step("2_die_mask", ToBmpDs(g, w, h, false, 0, dieMask)));
                }
                int count;
                foreignMm = Foreign(g, w, h, dieMask, em, lx, ty, rx, by, r.Defects, out count);
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
            // CUDA 가용 시 GPU(4변 한 번에), 아니면 CPU 폴백. 원칙: "CUDA 가능하면 CUDA, 아니면 CPU 폴백".
            // 결과는 두 경로 동일(GPU 커널이 아래 CPU 교차정의를 그대로 구현).
            if (CudaInterop.TryFindDieEdges(g, w, h, thr, out var gTop, out var gBot, out var gLeft, out var gRight))
            {
                GpuBackend.Note("BottomDie", ComputeBackend.Cuda);
                topE = gTop; botE = gBot; leftE = gLeft; rightE = gRight;
                ty = MedianValid(topE); by = MedianValid(botE); lx = MedianValid(leftE); rx = MedianValid(rightE);
                return ty >= 0 && by > ty && lx >= 0 && rx > lx;
            }
            GpuBackend.Note("BottomDie", ComputeBackend.Cpu);

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

        /// <summary>에지 라인(중앙값) 대비 안쪽 결손 최대[px]. a~b 범위, sign+1=상/좌, -1=하/우. vertical=true 면 인덱스=y.
        /// 스펙(specPx) 초과 '연속 구간'(간격≤5px)을 실측 bbox 마커로 수집 — 다중 칩핑, 실제 결손 폭/깊이 그대로 표시.</summary>
        private static double MaxInwardDev(int[] edge, int a, int b, int baseline, int sign, bool vertical, List<DefectMark> defects, int specPx = 2)
        {
            int maxDev = 0;
            int thr = Math.Max(2, specPx);
            int rs = -1, re = -1, rMax = 0;   // 현재 구간 시작/끝/최대 깊이
            Action flush = () =>
            {
                if (rs < 0 || defects == null) return;
                double p1 = baseline, p2 = baseline + sign * rMax;
                double lo = Math.Min(p1, p2), hi = Math.Max(p1, p2);
                defects.Add(vertical
                    ? new DefectMark { X = (lo + hi) / 2.0, Y = (rs + re) / 2.0, Width = Math.Max(4, hi - lo), Height = Math.Max(4, re - rs + 1), Area = rMax, Type = "Chipping" }
                    : new DefectMark { X = (rs + re) / 2.0, Y = (lo + hi) / 2.0, Width = Math.Max(4, re - rs + 1), Height = Math.Max(4, hi - lo), Area = rMax, Type = "Chipping" });
                rs = -1; re = -1; rMax = 0;
            };
            for (int i = a; i <= b; i++)
            {
                if (i < 0 || i >= edge.Length || edge[i] < 0) continue;
                int dev = sign * (edge[i] - baseline);   // 안쪽으로 들어오면 양수
                if (dev > maxDev) maxDev = dev;
                if (dev > thr)
                {
                    if (rs >= 0 && i - re > 5) flush();               // 간격>5px → 구간 분리
                    if (rs < 0) { rs = i; rMax = dev; }
                    else if (dev > rMax) rMax = dev;
                    re = i;
                }
            }
            flush();
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
        private double Foreign(byte[] g, int w, int h, byte[] mask, int em, int x0, int y0, int x1, int y1, List<DefectMark> defects, out int count)
        {
            count = 0;
            // 작업 픽셀이 ~9M(=3000²) 이하가 되도록 정수배 축소율 f 결정.
            const long MaxWork = 9_000_000;
            int f = 1; while ((long)(w / f) * (h / f) > MaxWork) f++;

            byte[] sg, sm; int sw, sh;
            if (f > 1) Downscale(g, mask, w, h, f, out sg, out sm, out sw, out sh);
            else { sg = g; sm = mask; sw = w; sh = h; }

            // 저해상도 마스크에 em 여백을 등방 침식으로 부여(반경 = em/f, 올림). 노치/콜렛 측벽 슬리버 오검 방지.
            // 풀해상도(w·h) 대신 저해상도(sw·sh)에서 수행 → 비용 ~f² 배 절감, black-hat 모폴로지 1패스 수준.
            if (em > 0) sm = ContaminationDetector.ErodeSquare(sm, sw, sh, Math.Max(1, (em + f - 1) / f));

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

        /// <summary>1배 그레이(ROI 크롭)를 2배 bilinear 확장 — BottomInspect 시뮬(단일 이미지) 경로의
        /// '2배 확장 이미지' 입력 규약 충족용(2026-07-11). 반환 좌표는 lib 가 ×0.5 로 1배로 환원한다.
        /// <para>정수 산술화(2026-07-12): 2배 업스케일의 보간 가중은 (0, 1/4, 3/4)로 고정되어 모든 항이
        /// 1/16 단위의 정확한 이진 분수다. 종전 double 식 v=Σ p·(a/4)(b/4), out=(byte)(v+0.5) 는
        /// 정수식 (Σ p·a·b + 8) >> 4 와 비트 동일하다(BottomInspectTest --upcheck 301케이스 전량 일치 확인).</para></summary>
        private static byte[] Upscale2xBilinear(byte[] src, int w, int h, out int w2, out int h2)
        {
            w2 = w * 2; h2 = h * 2;
            var dst = RentBuf(w2 * h2);   // 전 픽셀을 아래에서 덮어쓰므로 재사용 버퍼여도 결과 동일
            int dw = w2;
            var xs0 = new int[dw]; var xs1 = new int[dw]; var gxs = new int[dw];
            for (int x = 0; x < dw; x++)
            {
                int x0, gx;
                if ((x & 1) == 0) { x0 = x / 2 - 1; gx = 3; }
                else { x0 = x / 2; gx = 1; }
                int x1 = x0 + 1;
                if (x0 < 0) { x0 = 0; x1 = 0; gx = 0; }
                else if (x1 >= w) { x1 = w - 1; x0 = Math.Min(x0, w - 1); }
                xs0[x] = x0; xs1[x] = x1; gxs[x] = gx;
            }
            Parallel.For(0, h2, y =>
            {
                int y0, gy;
                if ((y & 1) == 0) { y0 = y / 2 - 1; gy = 3; }
                else { y0 = y / 2; gy = 1; }
                int y1 = y0 + 1;
                if (y0 < 0) { y0 = 0; y1 = 0; gy = 0; }
                else if (y1 >= h) { y1 = h - 1; y0 = Math.Min(y0, h - 1); }
                int rowA = y0 * w, rowB = y1 * w, row = y * dw;
                int wy1 = gy, wy0 = 4 - gy;
                for (int x = 0; x < dw; x++)
                {
                    int x0 = xs0[x], x1p = xs1[x], gx = gxs[x];
                    int sum = (4 - gx) * wy0 * src[rowA + x0] + gx * wy0 * src[rowA + x1p]
                            + (4 - gx) * wy1 * src[rowB + x0] + gx * wy1 * src[rowB + x1p];
                    dst[row + x] = (byte)((sum + 8) >> 4);
                }
            });
            return dst;
        }

        private static byte[] ToGray(Bitmap bmp, Rectangle rect, out int w, out int h)
        {
            rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
            w = rect.Width; h = rect.Height;
            var gray = RentBuf(w * h);
            int lw = w, lh = h;

            // 8bpp(그레이 카메라) 고속 경로(2026-07-12): 팔레트가 항등 그레이(entry[i]=(i,i,i))면
            // 24bpp 변환 경로는 픽셀마다 (v+v+v)/3 = v 를 계산하는 것과 정확히 같다 → 원바이트 복사로 대체.
            // 24bpp LockBits 의 GDI 팔레트 변환(131MP × 3B 생성)을 통째로 생략 — 값은 비트 동일.
            if (bmp.PixelFormat == PixelFormat.Format8bppIndexed && IsIdentityGrayPalette(bmp))
            {
                BitmapData d8 = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                try
                {
                    int stride8 = d8.Stride;
                    IntPtr scan0 = d8.Scan0;
                    Parallel.For(0, lh, y =>
                    {
                        Marshal.Copy(scan0 + y * stride8, gray, y * lw, lw);
                    });
                }
                finally { bmp.UnlockBits(d8); }
                return gray;
            }

            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int stride = data.Stride;
            var buf = RentBuf(stride * lh);
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
            ReturnBuf(buf);   // 스트라이드 중간 버퍼는 이 함수 안에서만 사용 → 즉시 풀 반납
            return gray;
        }

        /// <summary>팔레트가 항등 그레이(entry[i] == (i,i,i), 256개)인지 확인 — 고속 경로 적용 조건.</summary>
        private static bool IsIdentityGrayPalette(Bitmap bmp)
        {
            try
            {
                var entries = bmp.Palette.Entries;
                if (entries == null || entries.Length < 256) return false;
                for (int i = 0; i < 256; i++)
                {
                    var c = entries[i];
                    if (c.R != i || c.G != i || c.B != i) return false;
                }
                return true;
            }
            catch { return false; }
        }

        // ── 대형 버퍼 풀(2026-07-12) ──
        // 검사 1회마다 131MP급 byte[](그레이 1배 ≈131MB, 스트라이드 버퍼 ≈393MB, 2배 확장 ≈524MB)를
        // 새로 할당하면 LOH 할당·GC 압박으로 병렬 검사 tact 가 불안정해진다. '정확히 같은 길이'의 버퍼만
        // 재사용하고 소비자(ToGray/Upscale)가 전 픽셀을 덮어쓰므로 계산 결과에는 영향이 없다.
        // 반납 누락은 누수가 아니라 단순 미재사용(GC 회수)이라 실패 모드도 안전하다.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentBag<byte[]>> _bufPool
            = new System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentBag<byte[]>>();
        private const int MaxPooledPerSize = 6;

        private static byte[] RentBuf(int length)
        {
            System.Collections.Concurrent.ConcurrentBag<byte[]> bag;
            byte[] buf;
            if (_bufPool.TryGetValue(length, out bag) && bag.TryTake(out buf)) return buf;
            return new byte[length];
        }

        private static void ReturnBuf(byte[] buf)
        {
            if (buf == null) return;
            var bag = _bufPool.GetOrAdd(buf.Length, _ => new System.Collections.Concurrent.ConcurrentBag<byte[]>());
            if (bag.Count < MaxPooledPerSize) bag.Add(buf);   // 초과분은 GC에 맡김
        }
    }
}
