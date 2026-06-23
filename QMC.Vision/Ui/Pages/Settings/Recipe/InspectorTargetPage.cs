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
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            WireCamera();
        }

        public InspectorTargetPage(IVisionModule module, IInspector inspector, string recipeName = "default")
        {
            _module = module; _inspector = inspector;
            RecipeName = string.IsNullOrWhiteSpace(recipeName) ? "default" : recipeName;
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _node = _module?.Algorithms.FirstOrDefault(a => a.Inspector == _inspector);
            WireCamera();
            BuildParams();
            LoadTarget();
            BuildChildPanels();
            BuildRoiControls();
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
            if (_cam != null) _cam.SetOverlay(_inspector?.InspectionRoi, null);
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
        private TextBox _txtMoveStep, _txtSizeStep;
        private Label  _roiInfo;

        private Roi ActiveRoi() => _inspector?.InspectionRoi;

        private void BuildRoiControls()
        {
            if (_roiHost == null) return;
            _roiHost.Controls.Clear();

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(4) };

            // 이동 패드(3×3)
            var pad = new TableLayoutPanel { Width = 120, Height = 96, ColumnCount = 3, RowCount = 3, Margin = new Padding(2) };
            for (int i = 0; i < 3; i++) { pad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f)); pad.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f)); }
            pad.Controls.Add(RoiCell("▲", (s, e) => Nudge(0, -1)), 1, 0);
            pad.Controls.Add(RoiCell("◀", (s, e) => Nudge(-1, 0)), 0, 1);
            pad.Controls.Add(RoiCell("●", (s, e) => Recenter()),   1, 1);
            pad.Controls.Add(RoiCell("▶", (s, e) => Nudge(1, 0)),  2, 1);
            pad.Controls.Add(RoiCell("▼", (s, e) => Nudge(0, 1)),  1, 2);

            // 크기 조정 + Full Size
            var rz = new TableLayoutPanel { Width = 140, Height = 96, ColumnCount = 2, RowCount = 3, Margin = new Padding(2) };
            rz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f)); rz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            rz.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f)); rz.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f)); rz.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34f));
            rz.Controls.Add(RoiCell("W +", (s, e) => Resize(1, 0)),  0, 0);
            rz.Controls.Add(RoiCell("W -", (s, e) => Resize(-1, 0)), 1, 0);
            rz.Controls.Add(RoiCell("H +", (s, e) => Resize(0, 1)),  0, 1);
            rz.Controls.Add(RoiCell("H -", (s, e) => Resize(0, -1)), 1, 1);
            var full = RoiCell("Full Size", (s, e) => FullSizeRoi()); rz.Controls.Add(full, 0, 2); rz.SetColumnSpan(full, 2);

            // 우측: 라벨(단일 ROI) / 스텝 / 표시
            var col = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, Width = 210, Height = 120, Margin = new Padding(8, 0, 0, 0) };
            col.Controls.Add(new Label { Text = "Inspection ROI", AutoSize = true, Font = UiTheme.ButtonFont, ForeColor = UiTheme.Accent, Margin = new Padding(0, 2, 0, 2) });

            _txtMoveStep = new TextBox { Width = 44, Text = _moveStepPx.ToString() };
            _txtSizeStep = new TextBox { Width = 44, Text = _sizeStepPx.ToString() };
            _txtMoveStep.TextChanged += (s, e) => { int v; if (int.TryParse(_txtMoveStep.Text, out v) && v > 0) _moveStepPx = v; };
            _txtSizeStep.TextChanged += (s, e) => { int v; if (int.TryParse(_txtSizeStep.Text, out v) && v > 0) _sizeStepPx = v; };
            var steps = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            steps.Controls.Add(new Label { Text = "Move", AutoSize = true, Margin = new Padding(0, 7, 2, 0) });
            steps.Controls.Add(_txtMoveStep);
            steps.Controls.Add(new Label { Text = "Size", AutoSize = true, Margin = new Padding(10, 7, 2, 0) });
            steps.Controls.Add(_txtSizeStep);

            _roiInfo = new Label { AutoSize = true, Margin = new Padding(0, 6, 0, 0), Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray };
            col.Controls.Add(steps); col.Controls.Add(_roiInfo);

            flow.Controls.Add(pad); flow.Controls.Add(rz); flow.Controls.Add(col);
            _roiHost.Controls.Add(flow);
            UpdateRoiInfo();
        }

        private Button RoiCell(string text, EventHandler on)
        {
            var b = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(2), FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            b.Click += on; return b;
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
                items.Add(ParameterGridItem.Double("Gap Lower", "px", ParameterGridScope.Recipe, () => pg.GapLowerLimit, v => { pg.GapLowerLimit = v; }));
                items.Add(ParameterGridItem.Double("Gap Upper", "px", ParameterGridScope.Recipe, () => pg.GapUpperLimit, v => { pg.GapUpperLimit = v; }));
                items.Add(ParameterGridItem.Double("Gap Offset","px", ParameterGridScope.Recipe, () => pg.GapOffset,     v => { pg.GapOffset = v; }));
                items.Add(ParameterGridItem.Bool("Dark Die", ParameterGridScope.Recipe, () => pg.DarkDie, v => { pg.DarkDie = v; }));
                items.Add(ParameterGridItem.Int("Edge Step", "px", ParameterGridScope.Recipe, () => pg.EdgeStep, v => { pg.EdgeStep = v; }));
                items.Add(ParameterGridItem.Double("Band Trim", "", ParameterGridScope.Recipe, () => pg.BandTrim, v => { pg.BandTrim = v; }));
                items.Add(ParameterGridItem.Double("Outlier Sigma", "", ParameterGridScope.Recipe, () => pg.OutlierSigma, v => { pg.OutlierSigma = v; }));
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
                items.Add(ParameterGridItem.FilePath("시뮬 이미지 경로", ParameterGridScope.Setup,
                    () => (_node.Setup as QMC.Vision.Modules.AlgoSetupBase)?.SimSavedImagePath ?? "",
                    v => { if (_node.Setup is QMC.Vision.Modules.AlgoSetupBase s) { s.SimSavedImagePath = v?.Trim() ?? ""; MarkDirty(); } },
                    "이미지 파일 (*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff)|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff|모든 파일 (*.*)|*.*"));
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
                if (_inspector is QMC.Vision.Core.PlacementGapInspector pgv && pgv.LastValid)
                    _cam.SetDetectOverlay(_inspector.InspectionRoi, pgv.LastCorners, pgv.LastCenter, pgv.LastPass, pgv.LastGapText);
                else
                {
                    // 검출된 모든 결함을 박스로 오버레이(인덱스 + 박스). ROI 는 노란 박스.
                    var roi = _inspector.InspectionRoi;
                    var rrect = (roi != null && roi.Width > 0 && roi.Height > 0)
                        ? new System.Drawing.RectangleF((float)(roi.CenterX - roi.Width / 2.0), (float)(roi.CenterY - roi.Height / 2.0), (float)roi.Width, (float)roi.Height)
                        : System.Drawing.RectangleF.Empty;
                    System.Collections.Generic.List<QMC.Common.Ui.Controls.OverlayMark> marks = null;
                    if (r.Defects != null && r.Defects.Count > 0)
                    {
                        marks = new System.Collections.Generic.List<QMC.Common.Ui.Controls.OverlayMark>(r.Defects.Count);
                        foreach (var d in r.Defects)
                            marks.Add(new QMC.Common.Ui.Controls.OverlayMark(d.X, d.Y, d.Area, 0, d.Width, d.Height));
                    }
                    _cam.SetOverlay(rrect, marks);
                }
            }
            catch (Exception ex) { Status("INSPECT FAIL: " + ex.Message); }
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
