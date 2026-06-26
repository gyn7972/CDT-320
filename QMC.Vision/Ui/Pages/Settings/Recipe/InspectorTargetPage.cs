using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.Vision.Backends.Cognex;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;
using QMC.Vision.Ui.Localization; // Lang

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// R2d Step 2 — Inspector 타깃 3열 페이지(VisionTargetPage 병렬). 좌 CAMERA+검사결과+verdict /
    /// 중 ACTION(INSPECT 강조 + GRAB/LOAD/이미지저장/EDIT ROI) / 우 PARAMETERS(InspectionRoi)+검사조명+라이브튜닝.
    /// 세팅선택기에서 inspector 선택 시 본 페이지로 스왑(옛 InspectorPage 대체). InspectorPage public·주입·동작 보존.
    /// dirty 추적(세팅 단위) + SaveTarget/LoadTarget(BaseUnit 노드 SaveRecipe/LoadRecipe). 기능=InspectorPage 동일.
    /// </summary>
    public partial class InspectorTargetPage : PageBase, ITargetPage
    {
        private readonly IVisionModule _module;
        private readonly IInspector _inspector;
        private IAlgorithmNode _node;                // B — BaseUnit 알고리즘 노드
        private string RecipeName = "default";   // 핸들러 수신 레시피명(주입). 미주입 시 default
        private bool _dirty;
        private bool _langHooked;                   // LanguageChanged 중복 구독 방지
        private InspectionLightPanel _lightPanel;   // R2e — 편입 조명패널(통합 저장 대상)

        /// <summary>세팅(inspector) 변경 미저장 여부.</summary>
        public bool IsDirty => _dirty;
        /// <summary>저장된 레시피 데이터(파라미터 파일) 존재 여부.</summary>
        public bool HasSavedData => _inspector != null && File.Exists(TargetPath());
        /// <summary>dirty 상태 변경 알림(RecipePage 상태점 갱신용).</summary>
        public event EventHandler DirtyChanged;

        public InspectorTargetPage()
        {
            InitializeComponent();
            // __COLLAPSIBLE_WRAP__
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                QMC.Vision.Ui.Controls.CollapsibleGrids.Wrap(this._result, "검사 결과");
                this._params.Title = "PARAMETERS";
                this._secResult.Visible = false; this._center.RowStyles[4].Height = 0;
                this._secParam.Visible = false; this._right.RowStyles[0].Height = 0;
                QMC.Vision.Ui.Controls.SectionHeaderStyle.Apply(_secCam, _secAction, _secRoi, _secLight);
            }
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            WireCamera();
        }

        public InspectorTargetPage(IVisionModule module, IInspector inspector, string recipeName = "default")
        {
            _module = module; _inspector = inspector;
            RecipeName = string.IsNullOrWhiteSpace(recipeName) ? "default" : recipeName;
            InitializeComponent();
            // __COLLAPSIBLE_WRAP__
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                QMC.Vision.Ui.Controls.CollapsibleGrids.Wrap(this._result, "검사 결과");
                this._params.Title = "PARAMETERS";
                this._secResult.Visible = false; this._center.RowStyles[4].Height = 0;
                this._secParam.Visible = false; this._right.RowStyles[0].Height = 0;
                QMC.Vision.Ui.Controls.SectionHeaderStyle.Apply(_secCam, _secAction, _secRoi, _secLight);
            }
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _node = _module?.Algorithms.FirstOrDefault(a => a.Inspector == _inspector);
            WireCamera();
            BuildParams();
            LoadTarget();
            BuildChildPanels();
            WireRoiPad();
            BuildCamContextMenu();
            if (_inspector != null) _cam.SetOverlay(_inspector.InspectionRoi, null);
            UpdateRoiInfo();
            if (!_langHooked) { Lang.LanguageChanged += OnLanguageChanged; _langHooked = true; }
            ApplyLanguage();
            Status((module?.Name ?? "?") + " / " + (inspector?.Id ?? "?"));
        }

        private void WireCamera()
        {
            _cam.RoiEdited += OnCamRoiEdited;
            // 툴바 Grab/Live(=CameraView 자체 핸들러)로 프레임이 바뀌어도 STAGE/ROI 자동맞춤이 되도록 알림 수신.
            _cam.FrameChanged += OnCamFrameChanged;
            // 공용 CameraView 내장 툴바 — 모듈 지정 한 줄.
            _cam.AttachModule(_module);
            // 툴바 Grab 이 이 Inspector 전용 시뮬 저장이미지(GrabForTool)를 쓰도록 활성 도구 id 지정.
            _cam.SetActiveTool(ResolveToolId());
            _cam.ShowToolbar = true;
        }

        /// <summary>CameraView 프레임 크기 변경(툴바 Grab/Live 포함) → STAGE 갱신 + 미구성 InspectionRoi 자동맞춤.</summary>
        private void OnCamFrameChanged()
        {
            try { OnImageReady(_cam?.CurrentFrame); } catch { }
        }

        /// <summary>그랩/로드된 이미지에 맞춰 STAGE 표시 갱신 + 미구성(640×480 가정) InspectionRoi 1회 자동 배치 + 갱신.</summary>
        private void OnImageReady(Bitmap img)
        {
            if (img == null) return;
            if (_cam != null) _cam.InfoText = "STAGE\r\nW:" + img.Width + " H:" + img.Height;
            bool changed = FitDefaultRoiToImage(img);
            if (_cam != null) { _cam.CustomOverlayPaint = null; _cam.SetOverlay(_inspector?.InspectionRoi, null); }   // 새 이미지 → 이전 검출 오버레이 제거
            _params?.RefreshValues();
            if (changed) MarkDirty();
        }

        /// <summary>미구성(생성자 기본 좌표: 중심 320,240 또는 0,0) InspectionRoi 를 실제 이미지 중앙 절반 영역으로 1회 배치.
        /// 중심이 이미지 밖이면 중앙 재배치. 사용자가 이미 조정한 ROI 는 손대지 않는다.</summary>
        private bool FitDefaultRoiToImage(Bitmap img)
        {
            var r = _inspector?.InspectionRoi;
            if (r == null) return false;
            int iw = img.Width, ih = img.Height;
            bool defaultCenter = (System.Math.Abs(r.CenterX - 320) < 0.5 && System.Math.Abs(r.CenterY - 240) < 0.5)
                              || (r.CenterX < 0.5 && r.CenterY < 0.5);
            bool centerOutside = r.CenterX < 0 || r.CenterY < 0 || r.CenterX > iw || r.CenterY > ih;
            if ((defaultCenter && (iw > 800 || ih > 600)) || centerOutside)
            {
                r.CenterX = iw / 2.0; r.CenterY = ih / 2.0;
                r.Width   = iw / 2.0; r.Height  = ih / 2.0;
                return true;
            }
            return false;
        }

        // ── ROI 제어(단일 Inspection ROI — Finder 와 동일 UX, Train 토글 없음) ───────────
        private int  _moveStepPx = 10;
        private int  _sizeStepPx = 10;

        private Roi ActiveRoi() => _inspector?.InspectionRoi;

        // 공용 RoiNudgePad(이동/크기 버튼) 이벤트를 ROI 로직에 연결. 컨트롤 배치는 Designer.
        private void WireRoiPad()
        {
            _roiPad.MoveRequested     += Nudge;
            _roiPad.ResizeRequested   += Resize;
            _roiPad.RecenterRequested += Recenter;
            _roiPad.FullSizeRequested += FullSizeRoi;
            UpdateRoiInfo();
        }

        // 스텝 입력 — Designer 에서 TextChanged 배선.
        private void _txtMoveStep_TextChanged(object sender, EventArgs e)
        {
            int v; if (int.TryParse(_txtMoveStep.Text, out v) && v > 0) _moveStepPx = v;
        }
        private void _txtSizeStep_TextChanged(object sender, EventArgs e)
        {
            int v; if (int.TryParse(_txtSizeStep.Text, out v) && v > 0) _sizeStepPx = v;
        }

        private void Nudge(int dx, int dy)
        {
            var r = ActiveRoi(); if (r == null) return;
            r.CenterX += dx * _moveStepPx; r.CenterY += dy * _moveStepPx;
            AfterRoiChange();
        }
        private void Resize(int dw, int dh)
        {
            var r = ActiveRoi(); if (r == null) return;
            r.Width  = System.Math.Max(4.0, r.Width  + dw * _sizeStepPx);
            r.Height = System.Math.Max(4.0, r.Height + dh * _sizeStepPx);
            AfterRoiChange();
        }
        private void Recenter()
        {
            var r = ActiveRoi(); if (r == null) return;
            var img = CurrentImage;
            r.CenterX = (img?.Width  ?? 0) / 2.0;
            r.CenterY = (img?.Height ?? 0) / 2.0;
            AfterRoiChange();
        }
        private void FullSizeRoi()
        {
            var r = ActiveRoi(); if (r == null) return;
            var img = CurrentImage;
            double w = img?.Width  ?? r.Width;
            double h = img?.Height ?? r.Height;
            r.CenterX = w / 2.0; r.CenterY = h / 2.0; r.Width = w; r.Height = h;
            AfterRoiChange();
        }
        private void AfterRoiChange()
        {
            if (_cam != null) _cam.SetOverlay(ActiveRoi(), null);
            _params?.RefreshValues();
            UpdateRoiInfo();
            MarkDirty();
        }
        private void UpdateRoiInfo()
        {
            if (_roiInfo == null) return;
            var r = ActiveRoi();
            _roiInfo.Text = (r == null) ? "" :
                $"[Inspection]  Center ({r.CenterX:F0}, {r.CenterY:F0})\r\nSize ({r.Width:F0} x {r.Height:F0})";
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_langHooked) { Lang.LanguageChanged -= OnLanguageChanged; _langHooked = false; }
            try { if (_cam != null) _cam.FrameChanged -= OnCamFrameChanged; } catch { }
            base.OnHandleDestroyed(e);
        }

        /// <summary>언어 변경 — UI 스레드로 마샬링 후 표시 문구 재적용.</summary>
        private void OnLanguageChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((Action)ApplyLanguage); } catch { } return; }
            ApplyLanguage();
        }

        /// <summary>현재 언어로 섹션 헤더/버튼/결과 컬럼(한글 항목)을 적용. (INSPECT/GRAB 등 영문 식별 라벨은 유지)</summary>
        private void ApplyLanguage()
        {
            if (_secResult  != null) _secResult.Text  = Lang.T("rec.inspResult");
            if (_secLight   != null) _secLight.Text   = Lang.T("rec.inspLight");
            if (_result != null && _result.Columns.Count >= 3)
            {
                if (_result.Columns["Item"]  != null) _result.Columns["Item"].HeaderText  = Lang.T("col.item");
                if (_result.Columns["Value"] != null) _result.Columns["Value"].HeaderText = Lang.T("col.value");
                if (_result.Columns["Pass"]  != null) _result.Columns["Pass"].HeaderText  = Lang.T("col.result");
            }
        }

        /// <summary>C3b-3 — 조명 지정(SettingsPage) 변경을 레벨 그리드에 반영. RecipePage 가 타깃 표시 시 호출(캐시 재바인딩).</summary>
        public void RefreshLightAssignment()
            => _lightPanel?.SelectInspection(_node, _module?.AlgorithmKey ?? "", _inspector?.Id ?? "");

        // ── 우측: 검사 조명(InspectionLightPanel) 주입(런타임). 라이브튜닝 패널은 제거(중복) —
        //    레벨+점등 = InspectionLightPanel(Apply), 실물 확인 = Settings 라이브/그랩. ──
        private void BuildChildPanels()
        {
            _lightPanel = new InspectionLightPanel { Dock = DockStyle.Fill, EmbeddedMode = true, RecipeName = RecipeName };
            _lightPanel.SelectInspection(_node, _module?.AlgorithmKey ?? "", _inspector?.Id ?? "");   // C2 — 조명 SSOT=노드
            _lightPanel.LightChanged += (s, e) => MarkDirty();   // R2e — 조명 변경 → 상태점 점등
            _lightHost.Controls.Add(_lightPanel);
        }

        // ── 카메라 라이브 grab loop ──
        private System.Windows.Forms.Timer _liveTimer;
        private bool _liveOn;

        public void StartLive(int intervalMs = 0)
        {
            if (_liveOn) return;
            if (_liveTimer == null) { _liveTimer = new System.Windows.Forms.Timer(); _liveTimer.Tick += OnLiveTick; }
            _liveTimer.Interval = intervalMs > 0 ? intervalMs : ResolveDefaultLiveIntervalMs();
            _liveTimer.Start();
            _liveOn = true;
        }

        public void StopLive()
        {
            if (_liveTimer != null) _liveTimer.Stop();
            _liveOn = false;
        }

        private void OnLiveTick(object sender, EventArgs e)
        {
            if (_module == null) return;
            try
            {
                var r = _module.Grab();
                if (r != null && r.IsSuccess)
                {
                    // 라이브 프레임을 현재 이미지로 유지 — Inspect/Full Size 가 라이브 중에도 동작.
                    _lastGrab?.Dispose();
                    _loadedImage?.Dispose(); _loadedImage = null;
                    _lastGrab = r;
                    _cam.SetFrame(r);
                    OnImageReady(r.Image);
                }
                else r?.Dispose();
            }
            catch (Exception ex) { StopLive(); Status(Lang.T("rec.liveStop") + ex.Message); }
        }

        private int ResolveDefaultLiveIntervalMs()
        {
            string key = (_module?.Name ?? string.Empty).ToLowerInvariant();
            if (key.Contains("bottom") || key.Contains("btm")) return 667;
            return 333;
        }

        // ── 파라미터(우측 ParameterGridControl) = B 실 inspector 직접 바인딩 ──
        // InspectionRoi(Recipe) + Threshold(Cognex 한정, Recipe). 저장 시 노드 Collect.
        private void BuildParams()
        {
            if (_inspector == null) return;
            var items = new System.Collections.Generic.List<ParameterGridItem>
            {
                ParameterGridItem.Double("Inspect X", "px", ParameterGridScope.Recipe, () => _inspector.InspectionRoi.CenterX, v => { _inspector.InspectionRoi.CenterX = v; RefreshOverlay(); }),
                ParameterGridItem.Double("Inspect Y", "px", ParameterGridScope.Recipe, () => _inspector.InspectionRoi.CenterY, v => { _inspector.InspectionRoi.CenterY = v; RefreshOverlay(); }),
                ParameterGridItem.Double("Inspect W", "px", ParameterGridScope.Recipe, () => _inspector.InspectionRoi.Width,   v => { _inspector.InspectionRoi.Width = v;   RefreshOverlay(); }),
                ParameterGridItem.Double("Inspect H", "px", ParameterGridScope.Recipe, () => _inspector.InspectionRoi.Height,  v => { _inspector.InspectionRoi.Height = v;  RefreshOverlay(); }),
            };
            if (_inspector is CognexInspector cog)
                items.Add(ParameterGridItem.Double("Threshold", "", ParameterGridScope.Recipe, () => cog.Threshold, v => { cog.Threshold = v; }));
            else if (_inspector is QMC.Vision.Core.PlacementGapInspector pg)
            {
                items.Add(ParameterGridItem.Double("Threshold", "", ParameterGridScope.Recipe, () => pg.Threshold,     v => { pg.Threshold = v; }));
                items.Add(ParameterGridItem.Double("Gap Lower", "px", ParameterGridScope.Recipe, () => pg.GapLowerLimit, v => { pg.GapLowerLimit = v; PushChartLimits(); }));
                items.Add(ParameterGridItem.Double("Gap Upper", "px", ParameterGridScope.Recipe, () => pg.GapUpperLimit, v => { pg.GapUpperLimit = v; PushChartLimits(); }));
                items.Add(ParameterGridItem.Double("Gap Offset","px", ParameterGridScope.Recipe, () => pg.GapOffset,     v => { pg.GapOffset = v; }));
                items.Add(ParameterGridItem.Bool("Dark Die", ParameterGridScope.Recipe, () => pg.DarkDie, v => { pg.DarkDie = v; }));
                items.Add(ParameterGridItem.Int("Edge Step", "px", ParameterGridScope.Recipe, () => pg.EdgeStep, v => { pg.EdgeStep = v; }));
                items.Add(ParameterGridItem.Double("Band Trim", "", ParameterGridScope.Recipe, () => pg.BandTrim, v => { pg.BandTrim = v; }));
                items.Add(ParameterGridItem.Double("Outlier Sigma", "", ParameterGridScope.Recipe, () => pg.OutlierSigma, v => { pg.OutlierSigma = v; }));
            }
            else if (_inspector is QMC.Vision.Core.BottomInspector bi)
            {
                // Bottom 사이즈·칩핑·이물 (CDT-310 BottomInspectionParameter)
                items.Add(ParameterGridItem.Int   ("Chip Threshold", "", ParameterGridScope.Recipe, () => bi.ChipThreshold, v => { bi.ChipThreshold = v; }));
                items.Add(ParameterGridItem.Bool  ("Dark Chip", ParameterGridScope.Recipe, () => bi.DarkChip, v => { bi.DarkChip = v; }));
                items.Add(ParameterGridItem.Double("Chipping Depth", "mm", ParameterGridScope.Recipe, () => bi.ChippingDepth, v => { bi.ChippingDepth = v; }));
                items.Add(ParameterGridItem.Int   ("Chip Edge Margin", "px", ParameterGridScope.Recipe, () => bi.ChipEdgeMargin, v => { bi.ChipEdgeMargin = v; }));
                // 너비/높이 상·하한[mm] (0=미설정) — 차트 Limit 점선 + 사이즈 NG 기준.
                items.Add(ParameterGridItem.Double("Width Lower",  "mm", ParameterGridScope.Recipe, () => bi.ChipLowerSpecLimit.Width,  v => { bi.ChipLowerSpecLimit = new System.Drawing.SizeF((float)v, bi.ChipLowerSpecLimit.Height); PushChartLimits(); }));
                items.Add(ParameterGridItem.Double("Width Upper",  "mm", ParameterGridScope.Recipe, () => bi.ChipUpperSpecLimit.Width,  v => { bi.ChipUpperSpecLimit = new System.Drawing.SizeF((float)v, bi.ChipUpperSpecLimit.Height); PushChartLimits(); }));
                items.Add(ParameterGridItem.Double("Height Lower", "mm", ParameterGridScope.Recipe, () => bi.ChipLowerSpecLimit.Height, v => { bi.ChipLowerSpecLimit = new System.Drawing.SizeF(bi.ChipLowerSpecLimit.Width, (float)v); PushChartLimits(); }));
                items.Add(ParameterGridItem.Double("Height Upper", "mm", ParameterGridScope.Recipe, () => bi.ChipUpperSpecLimit.Height, v => { bi.ChipUpperSpecLimit = new System.Drawing.SizeF(bi.ChipUpperSpecLimit.Width, (float)v); PushChartLimits(); }));
                items.Add(ParameterGridItem.Double("Foreign Size", "mm", ParameterGridScope.Recipe, () => bi.ForeignObjectSize, v => { bi.ForeignObjectSize = v; }));
                items.Add(ParameterGridItem.Int   ("Foreign Edge Margin", "px", ParameterGridScope.Recipe, () => bi.ForeignEdgeMargin, v => { bi.ForeignEdgeMargin = v; }));
                items.Add(ParameterGridItem.Int   ("TopHat Radius", "px", ParameterGridScope.Recipe, () => bi.TopHatRadius, v => { bi.TopHatRadius = v; }));
                items.Add(ParameterGridItem.Int   ("TopHat Threshold", "", ParameterGridScope.Recipe, () => bi.TopHatThreshold, v => { bi.TopHatThreshold = v; }));
                items.Add(ParameterGridItem.Int   ("Min Foreign Area", "px", ParameterGridScope.Recipe, () => bi.MinForeignAreaFilterSize, v => { bi.MinForeignAreaFilterSize = v; }));
                items.Add(ParameterGridItem.Int   ("Max Foreign Area", "px", ParameterGridScope.Recipe, () => bi.MaxForeignAreaFilterSize, v => { bi.MaxForeignAreaFilterSize = v; }));
                items.Add(ParameterGridItem.Int   ("Link Distance", "px", ParameterGridScope.Recipe, () => bi.LinkDistance, v => { bi.LinkDistance = v; }));
                items.Add(ParameterGridItem.Bool  ("Use Contamination", ParameterGridScope.Recipe, () => bi.UseContaminationInspection, v => { bi.UseContaminationInspection = v; }));
                items.Add(ParameterGridItem.Double("Pixel Size X", "mm/px", ParameterGridScope.Recipe, () => bi.PixelSizeWidthMm, v => { bi.PixelSizeWidthMm = v; }));
                items.Add(ParameterGridItem.Double("Pixel Size Y", "mm/px", ParameterGridScope.Recipe, () => bi.PixelSizeHeightMm, v => { bi.PixelSizeHeightMm = v; }));
            }
            else if (_inspector is QMC.Vision.Core.SideAppearanceInspector si)
            {
                // Side 측면 칩핑 (CDT-310 SideInspectionParameter)
                items.Add(ParameterGridItem.Int   ("Chip Threshold", "", ParameterGridScope.Recipe, () => si.ChipThreshold, v => { si.ChipThreshold = v; }));
                if (si.IsChippingRole)
                {
                    items.Add(ParameterGridItem.Double("Upper Limit", "mm", ParameterGridScope.Recipe, () => si.ChippingUpperLimit, v => { si.ChippingUpperLimit = v; PushChartLimits(); }));
                    items.Add(ParameterGridItem.Double("Lower Limit", "mm", ParameterGridScope.Recipe, () => si.ChippingLowerLimit, v => { si.ChippingLowerLimit = v; PushChartLimits(); }));
                    items.Add(ParameterGridItem.Double("Chip Thickness", "mm", ParameterGridScope.Recipe, () => si.ChipThickness, v => { si.ChipThickness = v; }));
                    // CDT-310 FindLine 라인검출 조정값
                    items.Add(ParameterGridItem.Double("Scan Rate", "", ParameterGridScope.Recipe, () => si.ScanRate, v => { si.ScanRate = v; }));
                    items.Add(ParameterGridItem.Int   ("Envelope Bin", "px", ParameterGridScope.Recipe, () => si.EnvelopeBinSize, v => { si.EnvelopeBinSize = v; }));
                    items.Add(ParameterGridItem.Double("Keep Quantile", "", ParameterGridScope.Recipe, () => si.KeepQuantile, v => { si.KeepQuantile = v; }));
                    items.Add(ParameterGridItem.Int   ("Edge Gap", "px", ParameterGridScope.Recipe, () => si.EdgeGap, v => { si.EdgeGap = v; }));
                    items.Add(ParameterGridItem.Double("Pixel Size Y", "mm/px", ParameterGridScope.Recipe, () => si.PixelSizeHeightMm, v => { si.PixelSizeHeightMm = v; }));
                }
                if (si.IsSurfaceRole)
                {
                    // 이물/오염(Black-Hat 블롭) — 310 ContaminationInspector 파라미터
                    items.Add(ParameterGridItem.Int("TopHat Radius", "px", ParameterGridScope.Recipe, () => si.TopHatRadius, v => { si.TopHatRadius = v; }));
                    items.Add(ParameterGridItem.Int("TopHat Threshold", "", ParameterGridScope.Recipe, () => si.TopHatThreshold, v => { si.TopHatThreshold = v; }));
                    items.Add(ParameterGridItem.Int("Min Foreign Area", "px", ParameterGridScope.Recipe, () => si.MinForeignAreaFilterSize, v => { si.MinForeignAreaFilterSize = v; }));
                    items.Add(ParameterGridItem.Int("Max Foreign Area", "px", ParameterGridScope.Recipe, () => si.MaxForeignAreaFilterSize, v => { si.MaxForeignAreaFilterSize = v; }));
                    items.Add(ParameterGridItem.Int("Link Distance", "px", ParameterGridScope.Recipe, () => si.LinkDistance, v => { si.LinkDistance = v; }));
                }
                items.Add(ParameterGridItem.Double("Pixel Size X", "mm/px", ParameterGridScope.Recipe, () => si.PixelSizeWidthMm, v => { si.PixelSizeWidthMm = v; }));
            }
            AppendNodeParams(items);   // ② 검사 전용 POCO 필드 칸(인프라 — 현재 케이스 0)
            _params.SetItems(items);
            _params.ParameterValueChanged += (s, e) => { RefreshOverlay(); MarkDirty(); };
        }

        /// <summary>② per-algorithm 전용필드 칸 확장점 — 노드 구체 Recipe/Config 캐스트해 POCO 바인딩(저장=POCO).
        /// 전용필드 추가 시 아래 패턴 1줄. (인프라: 현재 케이스 0 — 현 동작 불변.)</summary>
        private void AppendNodeParams(System.Collections.Generic.List<ParameterGridItem> items)
        {
            if (_node == null) return;

            // 검사기별 '검사 사용'(품목별) — false 면 시퀀스/핸들러에서 이 검사를 건너뛴다(PASS 처리).
            // 측면 Surface 검사기에서는 '오염검사 사용' 역할. 로드 시 Recipe POCO 가 교체될 수 있어 람다에서 매번 _node 로 읽는다.
            if (_node.Recipe is QMC.Vision.Modules.InspectorAlgoRecipe)
            {
                items.Add(ParameterGridItem.Bool("검사 사용", ParameterGridScope.Recipe,
                    () => (_node.Recipe as QMC.Vision.Modules.InspectorAlgoRecipe)?.UseInspection ?? true,
                    v => { if (_node.Recipe is QMC.Vision.Modules.InspectorAlgoRecipe r) { r.UseInspection = v; MarkDirty(); } }));
            }

            // 도구별 시뮬 저장이미지 — Inspector 마다 다른 시뮬 이미지를 사용/경로 지정(Finder 와 동일).
            // (지정 없으면 모듈 저장이미지/실제 카메라로 폴백. 클릭 시 파일 찾아보기로 경로 설정.)
            // 로드 시 Setup POCO 인스턴스가 교체될 수 있어 람다에서 매번 _node 로 최신 POCO 를 읽는다.
            if (_node.Setup is QMC.Vision.Modules.AlgoSetupBase)
            {
                items.Add(ParameterGridItem.Bool("시뮬 저장이미지 사용", ParameterGridScope.Setup,
                    () => (_node.Setup as QMC.Vision.Modules.AlgoSetupBase)?.SimUseSavedImage ?? false,
                    v => { if (_node.Setup is QMC.Vision.Modules.AlgoSetupBase s) { s.SimUseSavedImage = v; MarkDirty(); } }));
                string imgFilter = "이미지 파일 (*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff)|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|모든 파일 (*.*)|*.*";
                items.Add(ParameterGridItem.FilePath("시뮬 이미지 경로 Ch1(0°)", ParameterGridScope.Setup,
                    () => (_node.Setup as QMC.Vision.Modules.AlgoSetupBase)?.SimSavedImagePath ?? "",
                    v => { if (_node.Setup is QMC.Vision.Modules.AlgoSetupBase s) { s.SimSavedImagePath = v?.Trim() ?? ""; MarkDirty(); } },
                    imgFilter));
                items.Add(ParameterGridItem.FilePath("시뮬 이미지 경로 Ch2(90°)", ParameterGridScope.Setup,
                    () => (_node.Setup as QMC.Vision.Modules.AlgoSetupBase)?.SimSavedImagePathCh2 ?? "",
                    v => { if (_node.Setup is QMC.Vision.Modules.AlgoSetupBase s) { s.SimSavedImagePathCh2 = v?.Trim() ?? ""; MarkDirty(); } },
                    imgFilter));

                // INSPECT 결과(검출 오버레이) 이미지 저장 — 사용 여부 + 폴더 경로.
                items.Add(ParameterGridItem.Bool("결과 저장 사용", ParameterGridScope.Setup,
                    () => (_node.Setup as QMC.Vision.Modules.AlgoSetupBase)?.DebugSaveEnabled ?? false,
                    v => { if (_node.Setup is QMC.Vision.Modules.AlgoSetupBase s) { s.DebugSaveEnabled = v; MarkDirty(); } }));
                items.Add(ParameterGridItem.FolderPath("결과 저장 경로", ParameterGridScope.Setup,
                    () => (_node.Setup as QMC.Vision.Modules.AlgoSetupBase)?.DebugSavePath ?? "",
                    v => { if (_node.Setup is QMC.Vision.Modules.AlgoSetupBase s) { s.DebugSavePath = v?.Trim() ?? ""; MarkDirty(); } }));
            }

            // 예) if (_node?.Recipe is SurfaceInspectorRecipe r)
            //         items.Add(ParameterGridItem.Double("Min Blob Area", "px²", ParameterGridScope.Recipe,
            //                   () => r.MinBlobArea, v => { r.MinBlobArea = v; MarkDirty(); }));
        }

        private void RefreshOverlay()
        {
            if (_inspector != null) _cam.SetOverlay(_inspector.InspectionRoi, null);
        }

        // ── dirty / 타깃 저장(상단바 SAVE 가 호출) — BaseUnit 노드 위임 ──
        private string TargetPath()
        {
            string key = _node?.StorageKey ?? ((_module?.StorageKey ?? "Unknown") + "." + (_inspector?.Id ?? "x"));
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", RecipeName, key + ".recipe.json");
        }

        private void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>타깃 저장 — 노드 SaveSettings + SaveRecipe. Collect 가 런타임→POCO 수집.</summary>
        public void SaveTarget()
        {
            if (_node == null) { Status(Lang.T("rec.noSaveNode")); return; }
            try
            {
                _node.SaveSettings();
                _lightPanel?.PersistLight();   // 조명 레벨을 recipe POCO 에 반영(저장은 아래 SaveRecipe 가 활성 레시피로 일괄)
                _node.SaveRecipe(RecipeName);
                _dirty = false;
                DirtyChanged?.Invoke(this, EventArgs.Empty);
                Status(Lang.T("rec.targetSaved") + TargetPath());
            }
            catch (Exception ex) { Status(Lang.T("rec.targetSaveFail") + ex.Message); }
        }

        public void LoadTarget()
        {
            if (_node == null) return;
            try
            {
                _node.LoadSettings();
                _node.LoadRecipe(RecipeName);   // Apply 가 POCO→런타임 inspector 주입
                _params.RefreshValues();
                RefreshOverlay();
                _dirty = false;                 // 저장본으로 되돌렸으므로 변경상태 해제
                DirtyChanged?.Invoke(this, EventArgs.Empty);
            }
            catch { }
        }

        // ── 액션(중앙) — InspectorPage 동일 로직 ──
        private GrabResult _lastGrab;
        private Bitmap _loadedImage;
        // 페이지 자체 Grab/Load 외에, 툴바 Grab/Live 로 CameraView 가 표시 중인 실제 프레임도 사용.
        private Bitmap CurrentImage => _lastGrab?.Image ?? _loadedImage ?? _cam?.CurrentFrame;

        private void OnGrabClick(object sender, EventArgs e) => DoGrab();
        private void OnInspectClick(object sender, EventArgs e) => DoInspect();
        private void OnLoadClick(object sender, EventArgs e) => DoLoad();
        private void OnSaveImageClick(object sender, EventArgs e) => DoSaveImage();

        // ── 카메라 우클릭 컨텍스트 메뉴(좌표·밝기 토글 포함) ──
        private void BuildCamContextMenu()
        {
            if (_cam == null) return;
            var cms = new ContextMenuStrip();
            cms.Items.Add("Live", null, (s, e) => StartLive());
            cms.Items.Add("Stop", null, (s, e) => StopLive());
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("Image load", null, (s, e) => DoLoad());
            cms.Items.Add("Image save", null, (s, e) => DoSaveImage());
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("Result overlay clear", null, (s, e) => { _cam.ClearDetectOverlay(); _cam.SetOverlay(_inspector?.InspectionRoi, null); });
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("Image auto fit", null, (s, e) => _cam?.ZoomFit());
            cms.Items.Add("x2", null, (s, e) => _cam?.SetZoom(2));
            cms.Items.Add("x4", null, (s, e) => _cam?.SetZoom(4));
            cms.Items.Add("x8", null, (s, e) => _cam?.SetZoom(8));
            cms.Items.Add(new ToolStripSeparator());
            var coordItem = new ToolStripMenuItem("좌표·밝기 표시 (PX / V)") { CheckOnClick = true, Checked = false };
            coordItem.Click += (s, e) => { if (_cam != null) _cam.ShowCursorReadout = coordItem.Checked; };
            cms.Items.Add(coordItem);
            _cam.ContextMenuStrip = cms;
        }
        private void OnEditRoiClick(object sender, EventArgs e) => BeginEditRoi();

        private void OnCamRoiEdited(string which, Roi roi)
        {
            if (_inspector == null) return;
            _inspector.InspectionRoi = roi;
            Status($"INSPECTION ROI updated: x={roi.CenterX:F0} y={roi.CenterY:F0} w={roi.Width:F0} h={roi.Height:F0}");
            _cam.SetOverlay(_inspector.InspectionRoi, null);
            _params.RefreshValues();
            MarkDirty();
        }

        private void DoGrab()
        {
            if (_module == null) { Status("ERR: module not bound"); return; }
            _lastGrab?.Dispose(); _lastGrab = null;
            _loadedImage?.Dispose(); _loadedImage = null;
            // 도구(Inspector) 전용 시뮬 저장이미지 우선 — SimUseSavedImage=true 면 SimSavedImagePath 로드,
            // 아니면(또는 경로 미지정) GrabForTool 내부에서 카메라 Grab 으로 폴백.
            // 주의: GrabForTool/GetAlgorithm 은 등록 id(Inspectors 딕셔너리 키)를 받는다.
            //       _inspector.Id 는 "모듈명/등록id" 전체이름이라 키와 다르므로 ResolveToolId 로 등록 키를 구한다.
            string toolId = ResolveToolId();
            try
            {
                var sb = _node?.Setup as QMC.Vision.Modules.AlgoSetupBase;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "RecipeGrab",
                    "Inspector DoGrab — resolvedKey='" + (toolId ?? "(null)") + "', inspector.Id='" + (_inspector?.Id ?? "") +
                    "', page.SimUse=" + (sb?.SimUseSavedImage.ToString() ?? "null") + ", page.Path='" + (sb?.SimSavedImagePath ?? "") + "'");
            }
            catch { }
            _lastGrab = _module.GrabForTool(toolId);
            if (_lastGrab.IsSuccess)
            {
                _cam.SetFrame(_lastGrab);
                OnImageReady(_lastGrab.Image);
                Status($"GRAB OK — {_lastGrab.Width}x{_lastGrab.Height} frame={_lastGrab.FrameNumber}");
            }
            else Status("GRAB FAIL: " + _lastGrab.ErrorMessage);
        }

        /// <summary>현재 inspector 의 등록 id(Inspectors 딕셔너리 키 = _algoById 키) 반환. GrabForTool/GetAlgorithm 용.
        /// _inspector.Id("모듈명/등록id" 전체이름)와 다르므로 dict 역참조로 키를 찾는다. 못 찾으면 null(카메라 폴백).</summary>
        private string ResolveToolId()
        {
            if (_module == null || _inspector == null) return null;
            foreach (var kv in _module.Inspectors)
                if (ReferenceEquals(kv.Value, _inspector)) return kv.Key;
            return null;
        }

        private void DoLoad()
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "Load image for inspection",
                Filter = "Image files|*.png;*.bmp;*.jpg;*.jpeg;*.tif;*.tiff|All files|*.*"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    _lastGrab?.Dispose(); _lastGrab = null;
                    _loadedImage?.Dispose();
                    // Image.FromFile 은 대용량 최초 디코드가 매우 느리다 → 스트림 고속 로드(원본 픽셀 보존).
                    using (var fs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
                    using (var src = System.Drawing.Image.FromStream(fs, false, false))
                        _loadedImage = new Bitmap(src);

                    var fake = GrabResult.Success(new Bitmap(_loadedImage), 0);
                    _cam.SetFrame(fake);
                    fake.Dispose();
                    OnImageReady(_loadedImage);
                    Status($"LOAD OK — {_loadedImage.Width}x{_loadedImage.Height}  ({Path.GetFileName(dlg.FileName)})");
                }
                catch (Exception ex) { Status("LOAD FAIL: " + ex.Message); }
            }
        }

        /// <summary>이미지 저장(현재 프레임 → 파일). 타깃 레시피 저장과 별개.</summary>
        private void DoSaveImage()
        {
            var img = CurrentImage;
            if (img == null) { Status(Lang.T("rec.imgNone")); return; }
            using (var dlg = new SaveFileDialog
            {
                Title = "Save current image",
                Filter = "PNG|*.png|BMP|*.bmp|JPEG|*.jpg",
                FileName = $"{(_module?.Name ?? "img")}_{(_inspector?.Id ?? "x").Replace('/', '_')}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    ImageFormat fmt = ImageFormat.Png;
                    var ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                    if (ext == ".bmp") fmt = ImageFormat.Bmp;
                    else if (ext == ".jpg" || ext == ".jpeg") fmt = ImageFormat.Jpeg;
                    img.Save(dlg.FileName, fmt);
                    Status(Lang.T("rec.imgSaveOk") + dlg.FileName);
                }
                catch (Exception ex) { Status(Lang.T("rec.imgSaveFail") + ex.Message); }
            }
        }

        private void DoInspect()
        {
            if (_inspector == null) { Status("ERR: inspector not bound"); return; }
            var img = CurrentImage;
            if (img == null) { DoGrab(); img = CurrentImage; }
            if (img == null) { Status("INSPECT: no image"); return; }
            try
            {
                // 결과 저장 ON 이면 단계 이미지 캡처 활성화.
                var dbg = _node?.Setup as QMC.Vision.Modules.AlgoSetupBase;
                if (_inspector is QMC.Vision.Core.IStepImageProvider sp)
                    sp.CaptureDebug = dbg != null && dbg.DebugSaveEnabled;

                var r = _inspector.Inspect(img);
                _result.Rows.Clear();
                if (r.Items != null)
                    foreach (var it in r.Items)
                        _result.Rows.Add(it.Name, it.Value, it.IsPass ? "✓" : "✗");

                _lblVerdict.Text = r.IsPass ? "PASS" : "FAIL";
                _lblVerdict.BackColor = r.IsPass ? Color.FromArgb(40, 180, 90) : Color.FromArgb(220, 60, 60);
                _lblVerdict.ForeColor = Color.White;

                Status($"INSPECT {(r.IsPass ? "OK" : "FAIL")} — {r.Items?.Count ?? 0} item(s)" +
                       (string.IsNullOrEmpty(r.ErrorMessage) ? "" : " | " + r.ErrorMessage));
                // 공용은 ROI(노랑)만, 검출 기하·결함·상세값은 검사기별 전용 오버레이(CustomOverlayPaint)로 그린다.
                {
                    var roi = _inspector.InspectionRoi;
                    var rrect = (roi != null && roi.Width > 0 && roi.Height > 0)
                        ? new System.Drawing.RectangleF((float)(roi.CenterX - roi.Width / 2.0), (float)(roi.CenterY - roi.Height / 2.0), (float)roi.Width, (float)roi.Height)
                        : System.Drawing.RectangleF.Empty;
                    _cam.SetOverlay(rrect, null);
                    var insp = _inspector; var res = r;
                    _cam.CustomOverlayPaint = (gr, toScreen) => DrawInspectorOverlay(gr, toScreen, insp, res);
                    _cam.Invalidate();
                }

                // 상세값 = 기존 결과라인 스타일(우측 하단 정렬·동일 색·폰트) 그대로. 항목 + 결함별 NG 라인.
                try
                {
                    _cam.SetVerdict(r.IsPass ? "OK" : "NG", r.IsPass);
                    var lines = new System.Collections.Generic.List<string>();
                    var cols  = new System.Collections.Generic.List<Color>();
                    Color green = Color.FromArgb(120, 230, 120);   // 일반
                    Color red   = Color.FromArgb(255, 90, 90);     // 에러(NG)
                    if (r.Items != null)
                        foreach (var it in r.Items) { lines.Add(it.Name + ": " + it.Value); cols.Add(green); }
                    if (r.Defects != null)
                    {
                        int i = 1;
                        foreach (var d in r.Defects)
                        {
                            string ng = string.IsNullOrEmpty(d.Type) ? "NG" : ("NG(" + d.Type + ")");
                            lines.Add(string.Format("{0}: #{1} ({2:F0},{3:F0}) {4:F0}x{5:F0}px", ng, i, d.X, d.Y, d.Width, d.Height));
                            cols.Add(red);
                            i++;
                        }
                    }
                    _cam.SetResultLines(lines.ToArray(), cols.ToArray());
                }
                catch { }

                // 결과(검출 오버레이) 이미지 저장 — 레시피 'Setup' 의 결과 저장 사용/경로.
                try
                {
                    var setup = _node?.Setup as QMC.Vision.Modules.AlgoSetupBase;
                    if (setup != null && setup.DebugSaveEnabled && !string.IsNullOrWhiteSpace(setup.DebugSavePath))
                        SaveDebugImage(img, r, setup.DebugSavePath);
                }
                catch (Exception ex) { Status("결과 저장 실패: " + ex.Message); }
            }
            catch (Exception ex) { Status("INSPECT FAIL: " + ex.Message); }
        }

        private static System.Drawing.PointF AvgPoint(System.Drawing.PointF[] pts)
        {
            if (pts == null || pts.Length == 0) return System.Drawing.PointF.Empty;
            float x = 0, y = 0;
            foreach (var p in pts) { x += p.X; y += p.Y; }
            return new System.Drawing.PointF(x / pts.Length, y / pts.Length);
        }

        // ── 검사기별 전용 오버레이(이미지 위) + 우측 상세값 패널 ──
        private static System.Drawing.PointF[] ToScreenArr(System.Drawing.PointF[] pts, System.Func<System.Drawing.PointF, System.Drawing.PointF> toS)
        {
            var a = new System.Drawing.PointF[pts.Length];
            for (int i = 0; i < pts.Length; i++) a[i] = toS(pts[i]);
            return a;
        }

        /// <summary>현재 검사기의 상/하한(Limit)을 운영뷰 차트 스토어로 즉시 송출 — 레시피에서 값 바꾸면 바로 빨간 점선 반영.</summary>
        private void PushChartLimits()
        {
            try
            {
                if (_inspector is QMC.Vision.Core.BottomInspector b)
                {
                    QMC.Vision.Core.ChartLimitStore.Set("Bottom", 0, b.ChipUpperSpecLimit.Width,  b.ChipLowerSpecLimit.Width);
                    QMC.Vision.Core.ChartLimitStore.Set("Bottom", 1, b.ChipUpperSpecLimit.Height, b.ChipLowerSpecLimit.Height);
                }
                else if (_inspector is QMC.Vision.Core.SideAppearanceInspector s)
                {
                    QMC.Vision.Core.ChartLimitStore.Set("Side", 0, s.ChippingUpperLimit, s.ChippingLowerLimit);
                    QMC.Vision.Core.ChartLimitStore.Set("Side", 1, s.ChippingUpperLimit, s.ChippingLowerLimit);
                }
                else if (_inspector is QMC.Vision.Core.PlacementGapInspector p)
                {
                    QMC.Vision.Core.ChartLimitStore.Set("Bin", 0, p.GapUpperLimit, p.GapLowerLimit);
                    QMC.Vision.Core.ChartLimitStore.Set("Bin", 1, p.GapUpperLimit, p.GapLowerLimit);
                }
            }
            catch { }
        }

        private static void DrawInspectorOverlay(System.Drawing.Graphics g,
            System.Func<System.Drawing.PointF, System.Drawing.PointF> toS, IInspector insp, InspectionResult r)
        {
            if (g == null || toS == null || r == null) return;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 바텀(SurfaceInspector) 전용 오버레이 — 작업 모니터와 동일한 공용 렌더러로 그린다(구조 일원화).
            if (insp is QMC.Vision.Core.BottomInspector biTop && biTop.LastValid)
            {
                string cap = null;
                { string wv = null, hv = null, av = null;
                  if (r.Items != null) foreach (var it in r.Items)
                  { if (it.Name == "Width") wv = it.Value; else if (it.Name == "Height") hv = it.Value; else if (it.Name == "Angle") av = it.Value; }
                  if (wv != null || hv != null) cap = "W " + (wv ?? "?") + " H " + (hv ?? "?") + (av != null ? (" θ" + av) : ""); }
                QMC.Vision.Core.InspectionOverlayRenderer.Draw(g, toS, new QMC.Vision.Core.InspectionOverlayStore.Geom
                {
                    Kind = QMC.Vision.Core.InspectionOverlayStore.OverlayKind.Bottom,
                    Corners = biTop.LastCorners, Defects = r.Defects, Caption = cap, Pass = r.IsPass
                });
                return;
            }

            using (var green = new System.Drawing.Pen(System.Drawing.Color.LimeGreen, 2f))
            using (var red   = new System.Drawing.Pen(System.Drawing.Color.Red, 2f))
            {
                // 검출 기하(초록) — 검사기 타입별로 다르게
                if (insp is QMC.Vision.Core.SideAppearanceInspector si && si.LastValid && si.IsChippingRole)
                {
                    // 검출 기준선(강건 피팅 직선) — 얇은 노랑. 칩핑 = 이 선 대비 프로파일의 벗어남.
                    if (si.LastCorners != null && si.LastCorners.Length >= 4)
                        using (var refp = new System.Drawing.Pen(System.Drawing.Color.Gold, 1f))
                        {
                            var c = ToScreenArr(si.LastCorners, toS);
                            g.DrawLine(refp, c[0], c[1]); g.DrawLine(refp, c[3], c[2]);
                        }
                    // 컬럼별 실제 굴곡 에지 프로파일(초록) — 노치/톱니를 그대로 따라감
                    if (si.LastTopProfile != null && si.LastTopProfile.Length >= 2) g.DrawLines(green, ToScreenArr(si.LastTopProfile, toS));
                    if (si.LastBotProfile != null && si.LastBotProfile.Length >= 2) g.DrawLines(green, ToScreenArr(si.LastBotProfile, toS));
                }
                else if (insp is QMC.Vision.Core.BottomInspector bi && bi.LastValid && bi.LastCorners != null && bi.LastCorners.Length >= 3)
                    g.DrawPolygon(green, ToScreenArr(bi.LastCorners, toS));
                else if (insp is QMC.Vision.Core.PlacementGapInspector pg && pg.LastValid && pg.LastCorners != null && pg.LastCorners.Length >= 3)
                    g.DrawPolygon(green, ToScreenArr(pg.LastCorners, toS));

                // 결함(빨강 박스 + 번호)
                if (r.Defects != null)
                {
                    int i = 1;
                    using (var f  = new System.Drawing.Font("Consolas", 8f, System.Drawing.FontStyle.Bold))
                    using (var br = new System.Drawing.SolidBrush(System.Drawing.Color.Red))
                        foreach (var d in r.Defects)
                        {
                            var p1 = toS(new System.Drawing.PointF((float)(d.X - d.Width / 2), (float)(d.Y - d.Height / 2)));
                            var p2 = toS(new System.Drawing.PointF((float)(d.X + d.Width / 2), (float)(d.Y + d.Height / 2)));
                            float w = System.Math.Max(6, p2.X - p1.X), h = System.Math.Max(6, p2.Y - p1.Y);
                            g.DrawRectangle(red, p1.X, p1.Y, w, h);
                            g.DrawString(i.ToString(), f, br, p1.X, p1.Y - 12);
                            i++;
                        }
                }
            }
            // 상세값 텍스트는 영상에 그리지 않음 — 기존 결과라인(SetResultLines, 우측하단 핑크) 사용.
        }

        /// <summary>바텀(SurfaceInspector) 전용 오버레이 — 공용 박스/결함 대신 다이 박스 + 사이즈 라벨 +
        /// 칩핑(주황)·이물(자홍) 구분 마커. 검출종류(DefectMark.Type)에 따라 색·모양·접두(C/F)를 다르게 그린다.</summary>
        private static void DrawBottomOverlay(System.Drawing.Graphics g,
            System.Func<System.Drawing.PointF, System.Drawing.PointF> toS, QMC.Vision.Core.BottomInspector bi, InspectionResult r)
        {
            string ItemVal(string name)
            {
                if (r.Items != null) foreach (var it in r.Items) if (it.Name == name) return it.Value;
                return null;
            }
            bool ok = r.IsPass;

            using (var die    = new System.Drawing.Pen(ok ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Orange, 2.5f))
            using (var chipPen= new System.Drawing.Pen(System.Drawing.Color.DarkOrange, 2f))
            using (var forPen = new System.Drawing.Pen(System.Drawing.Color.Magenta, 2f))
            using (var corner = new System.Drawing.SolidBrush(System.Drawing.Color.LimeGreen))
            using (var fChip  = new System.Drawing.SolidBrush(System.Drawing.Color.DarkOrange))
            using (var fFor   = new System.Drawing.SolidBrush(System.Drawing.Color.Magenta))
            using (var fLbl   = new System.Drawing.Font("Consolas", 9f, System.Drawing.FontStyle.Bold))
            {
                // 다이 박스(검출 사각형) + 코너 점
                if (bi.LastCorners != null && bi.LastCorners.Length >= 4)
                {
                    var c = ToScreenArr(bi.LastCorners, toS);
                    g.DrawPolygon(die, c);
                    foreach (var p in c) g.FillEllipse(corner, p.X - 3, p.Y - 3, 6, 6);

                    // 사이즈 라벨(좌상단 코너 근처) — Width × Height mm + 각도
                    string wv = ItemVal("Width"), hv = ItemVal("Height"), av = ItemVal("Angle");
                    if (wv != null && hv != null)
                    {
                        string label = "DIE " + wv + " x " + hv + " mm" + (av != null ? ("  θ" + av + "°") : "");
                        var anchor = c[0];
                        using (var bg = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(150, 0, 0, 0)))
                        {
                            var sz = g.MeasureString(label, fLbl);
                            g.FillRectangle(bg, anchor.X, anchor.Y - sz.Height - 4, sz.Width + 6, sz.Height + 2);
                        }
                        g.DrawString(label, fLbl, ok ? corner : fChip, anchor.X + 3, anchor.Y - g.MeasureString(label, fLbl).Height - 3);
                    }
                }

                // 결함 — 종류별로 모양/색 구분(칩핑=주황 사각, 이물=자홍 원)
                if (r.Defects != null)
                {
                    int ic = 1, ifn = 1;
                    foreach (var d in r.Defects)
                    {
                        var p1 = toS(new System.Drawing.PointF((float)(d.X - d.Width / 2), (float)(d.Y - d.Height / 2)));
                        var p2 = toS(new System.Drawing.PointF((float)(d.X + d.Width / 2), (float)(d.Y + d.Height / 2)));
                        float ww = System.Math.Max(8, p2.X - p1.X), hh = System.Math.Max(8, p2.Y - p1.Y);
                        bool isForeign = string.Equals(d.Type, "Foreign", System.StringComparison.OrdinalIgnoreCase);
                        if (isForeign)
                        {
                            g.DrawEllipse(forPen, p1.X, p1.Y, ww, hh);
                            g.DrawString("F" + (ifn++), fLbl, fFor, p1.X, p1.Y - 14);
                        }
                        else   // 칩핑(또는 미지정)
                        {
                            g.DrawRectangle(chipPen, p1.X, p1.Y, ww, hh);
                            g.DrawString("C" + (ic++), fLbl, fChip, p1.X, p1.Y - 14);
                        }
                    }
                }
            }
        }

        /// <summary>우측 하단 상세값 패널 — 기본 초록, 에러(스펙 초과 항목·검출 결함)만 빨강. 항목 + 결함별 좌표/크기.</summary>
        private static void DrawDetailPanel(System.Drawing.Graphics g, IInspector insp, InspectionResult r)
        {
            var green = System.Drawing.Color.LimeGreen;
            var red   = System.Drawing.Color.Red;

            // (텍스트, 색) 줄 누적 — 통과=초록, 에러=빨강.
            var lines = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, System.Drawing.Color>>();
            void Add(string t, System.Drawing.Color c) => lines.Add(new System.Collections.Generic.KeyValuePair<string, System.Drawing.Color>(t, c));

            Add("[ " + (insp != null ? insp.Id : "") + " ]   " + (r.IsPass ? "OK" : "NG"), r.IsPass ? green : red);
            if (r.Items != null)
                foreach (var it in r.Items)
                    Add("  " + it.Name + " : " + it.Value, it.IsPass ? green : red);   // 항목별 합/불 색
            if (r.Defects != null && r.Defects.Count > 0)
            {
                Add("- Defects (" + r.Defects.Count + ") -", red);
                int i = 1;
                foreach (var d in r.Defects)
                {
                    Add(string.Format("#{0} {1} ({2:F0},{3:F0})  {4:F0}x{5:F0}px", i, string.IsNullOrEmpty(d.Type) ? "" : ("[" + d.Type + "]"), d.X, d.Y, d.Width, d.Height), red);
                    if (++i > 20) { Add("  ...", red); break; }
                }
            }

            // 박스/테두리 없이 우측 하단에 텍스트만 — 줄마다 색(기본 초록 / 에러 빨강), 아래로 누적(하단 정렬).
            var cb = g.ClipBounds;
            float lh = 15f, pw = 300f;
            float px = cb.Right - pw - 8f;
            if (px < cb.Left + 4) px = cb.Left + 4;
            float startY = cb.Bottom - 8f - lines.Count * lh;
            if (startY < cb.Top + 4) startY = cb.Top + 4;
            using (var f = new System.Drawing.Font("Consolas", 8.5f, System.Drawing.FontStyle.Bold))
            {
                float yy = startY;
                foreach (var ln in lines)
                    using (var br = new System.Drawing.SolidBrush(ln.Value))
                    { g.DrawString(ln.Key, f, br, px, yy); yy += lh; }
            }
        }

        /// <summary>검출 오버레이(ROI·에지/박스·결함·측정값·판정)를 그린 결과 이미지를 폴더에 저장.</summary>
        private void SaveDebugImage(System.Drawing.Bitmap src, InspectionResult r, string pathOrDir)
        {
            string dir = pathOrDir;
            try { if (System.IO.Path.HasExtension(dir)) dir = System.IO.Path.GetDirectoryName(dir); } catch { }
            if (string.IsNullOrWhiteSpace(dir)) return;
            System.IO.Directory.CreateDirectory(dir);

            string baseName = (_inspector?.Id ?? "inspect").Replace('/', '_').Replace('\\', '_')
                              + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

            // 0. 원본
            try { src.Save(System.IO.Path.Combine(dir, baseName + "_0_original.png"), System.Drawing.Imaging.ImageFormat.Png); } catch { }
            // 1..n 처리 단계(그레이/임계/블롭 등) — 검사기가 제공
            if (_inspector is QMC.Vision.Core.IStepImageProvider sp && sp.DebugSteps != null)
                foreach (var st in sp.DebugSteps)
                    try { st.Value?.Save(System.IO.Path.Combine(dir, baseName + "_" + st.Key + ".png"), System.Drawing.Imaging.ImageFormat.Png); } catch { }

            using (var bmp = new System.Drawing.Bitmap(src))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var roi = _inspector?.InspectionRoi;
                // ROI(노랑)
                if (roi != null && roi.Width > 0 && roi.Height > 0)
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.Yellow, 1.5f))
                        g.DrawRectangle(pen, (float)(roi.CenterX - roi.Width / 2.0), (float)(roi.CenterY - roi.Height / 2.0), (float)roi.Width, (float)roi.Height);

                // 검출 기하 — Side: 에지 상/하 기준선, Bottom/Gap: 검출 박스
                using (var gp = new System.Drawing.Pen(r.IsPass ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Orange, 2f))
                {
                    if (_inspector is QMC.Vision.Core.SideAppearanceInspector si && si.LastValid)
                    {
                        // 검출 기준선(직선) — 얇은 노랑. 칩핑 = 이 선 대비 프로파일 벗어남.
                        if (si.LastCorners != null && si.LastCorners.Length >= 4)
                            using (var refp = new System.Drawing.Pen(System.Drawing.Color.Gold, 1f))
                            { g.DrawLine(refp, si.LastCorners[0], si.LastCorners[1]); g.DrawLine(refp, si.LastCorners[3], si.LastCorners[2]); }
                        // 컬럼별 실제 굴곡 에지 프로파일(평균 직선 아님)
                        if (si.LastTopProfile != null && si.LastTopProfile.Length >= 2) g.DrawLines(gp, si.LastTopProfile);
                        if (si.LastBotProfile != null && si.LastBotProfile.Length >= 2) g.DrawLines(gp, si.LastBotProfile);
                        if (si.LastCorners != null) g.DrawString("edge profile", Font, System.Drawing.Brushes.LimeGreen, si.LastCorners[0]);
                    }
                    else if (_inspector is QMC.Vision.Core.BottomInspector bi && bi.LastValid && bi.LastCorners != null)
                        g.DrawPolygon(gp, bi.LastCorners);
                    else if (_inspector is QMC.Vision.Core.PlacementGapInspector pg && pg.LastValid && pg.LastCorners != null)
                        g.DrawPolygon(gp, pg.LastCorners);
                }

                // 결함 마크 — 종류별 구분(이물=자홍 원, 칩핑/기타=주황 사각). 저장 이미지도 라이브 오버레이와 동일 표기.
                if (r.Defects != null)
                    using (var chipPen = new System.Drawing.Pen(System.Drawing.Color.DarkOrange, 2f))
                    using (var forPen  = new System.Drawing.Pen(System.Drawing.Color.Magenta, 2f))
                        foreach (var d in r.Defects)
                        {
                            float dx = (float)(d.X - d.Width / 2), dy = (float)(d.Y - d.Height / 2);
                            float dw = (float)Math.Max(8, d.Width), dh = (float)Math.Max(8, d.Height);
                            if (string.Equals(d.Type, "Foreign", System.StringComparison.OrdinalIgnoreCase))
                                g.DrawEllipse(forPen, dx, dy, dw, dh);
                            else
                                g.DrawRectangle(chipPen, dx, dy, dw, dh);
                        }

                // 측정값 + 판정 텍스트
                int ty = 6;
                using (var bg = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(160, 0, 0, 0)))
                    g.FillRectangle(bg, 4, 4, 260, 20 + (r.Items?.Count ?? 0) * 16);
                using (var f = new System.Drawing.Font("Consolas", 9f))
                {
                    g.DrawString((r.IsPass ? "OK" : "NG"), new System.Drawing.Font("Consolas", 11f, System.Drawing.FontStyle.Bold),
                        r.IsPass ? System.Drawing.Brushes.LimeGreen : System.Drawing.Brushes.OrangeRed, 8, ty); ty += 18;
                    if (r.Items != null) foreach (var it in r.Items) { g.DrawString(it.Name + " : " + it.Value, f, System.Drawing.Brushes.White, 8, ty); ty += 16; }
                }

                string full = System.IO.Path.Combine(dir, baseName + "_9_result.png");
                bmp.Save(full, System.Drawing.Imaging.ImageFormat.Png);
                Status("결과 저장됨(원본+단계+결과): " + dir);
            }
        }

        private void BeginEditRoi()
        {
            if (_inspector == null) { Status("ERR: inspector not bound"); return; }
            _cam.BeginRoiDrag("Search", _inspector.InspectionRoi);  // Search 색(주황)으로 표시
            Status("Drag a rectangle on the image to set INSPECTION ROI…");
        }

        private void Status(string s) { if (_lblStatus != null) _lblStatus.Text = s; }
    }
}
