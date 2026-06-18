using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;
using QMC.Vision.Ui.Localization; // Lang

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// R2b — Handler VisionRecipePage 미러(3열 TLP). 좌 카메라+매치 / 중 ACTION 3×3 / 우 ParameterGridControl+JOG+SPEED.
    /// ROI 라디오 제거(세팅선택기는 RecipePage 영속 바). 액션 SAVE=이미지저장(상단바 SAVE=타깃 레시피저장).
    /// dirty 추적(세팅 단위) + SaveTarget/LoadTarget(BaseUnit 노드 SaveRecipe/LoadRecipe). JOG/SPEED inert.
    /// 기능(Grab/Match/Train/Load/EditROI)은 FinderPage 동일.
    /// </summary>
    public partial class VisionTargetPage : PageBase, ITargetPage
    {
        private readonly IVisionModule _module;
        private readonly IPatternFinder _finder;
        private QMC.Vision.Modules.IAlgorithmNode _node;   // B — BaseUnit 알고리즘 노드(저장/로드 위임)
        private string RecipeName = "default";       // 핸들러 수신 레시피명(주입). 미주입 시 default
        private bool _dirty;
        private bool _langHooked;                   // LanguageChanged 중복 구독 방지
        private InspectionLightPanel _lightPanel;   // R2e — 편입 조명패널(통합 저장 대상)

        /// <summary>세팅(finder) 변경 미저장 여부.</summary>
        public bool IsDirty => _dirty;
        /// <summary>저장된 레시피 데이터(파라미터 파일) 존재 여부.</summary>
        public bool HasSavedData => _finder != null && File.Exists(TargetPath());
        /// <summary>dirty 상태 변경 알림(RecipePage 상태점 갱신용).</summary>
        public event EventHandler DirtyChanged;

        public VisionTargetPage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            WireCamera();
        }

        public VisionTargetPage(IVisionModule module, IPatternFinder finder, string recipeName = "default")
        {
            _module = module; _finder = finder;
            RecipeName = string.IsNullOrWhiteSpace(recipeName) ? "default" : recipeName;
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            _node = _module?.Algorithms.FirstOrDefault(a => a.Finder == _finder);
            WireCamera();
            BuildParams();
            LoadTarget();
            BuildChildPanels();
            if (_finder != null) _cam.SetOverlay(_finder.SearchRoi, null);
            ShowTrainImage();
            BuildRoiControls();
            BuildCamContextMenu();
            if (!_langHooked) { Lang.LanguageChanged += OnLanguageChanged; _langHooked = true; }
            ApplyLanguage();
            Status((module?.Name ?? "?") + " / " + (finder?.Id ?? "?"));
        }

        private void WireCamera()
        {
            _cam.RoiEdited += OnCamRoiEdited;
            // 공용 CameraView 내장 툴바(Grab/Live/Stop/Save/Load/측정/맞춤) 사용 — 모듈 지정 한 줄.
            _cam.AttachModule(_module);
            _cam.ShowToolbar = true;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_langHooked) { Lang.LanguageChanged -= OnLanguageChanged; _langHooked = false; }
            base.OnHandleDestroyed(e);
        }

        /// <summary>언어 변경 — UI 스레드로 마샬링 후 표시 문구 재적용.</summary>
        private void OnLanguageChanged()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke((Action)ApplyLanguage); } catch { } return; }
            ApplyLanguage();
        }

        /// <summary>현재 언어로 섹션 헤더/버튼(한글 항목)을 적용. (영문 식별 라벨 GRAB/MATCH 등은 유지)</summary>
        private void ApplyLanguage()
        {
            if (_secRoi         != null) _secRoi.Text         = Lang.T("rec.roiCtrl");
            if (_secLight       != null) _secLight.Text       = Lang.T("rec.inspLight");
            if (_btnClearResult != null) _btnClearResult.Text = Lang.T("rec.clearResult");
        }

        /// <summary>C3b-3 — 조명 지정(SettingsPage) 변경을 레벨 그리드에 반영. RecipePage 가 타깃 표시 시 호출(캐시 재바인딩).</summary>
        public void RefreshLightAssignment()
            => _lightPanel?.SelectInspection(_node, _module?.AlgorithmKey ?? "", _finder?.Id ?? "");

        // ── 우측: 검사 조명(InspectionLightPanel) 주입(런타임). 라이브튜닝 패널은 제거(중복) —
        //    레벨+점등 = InspectionLightPanel(Apply), 실물 확인 = Settings 라이브/그랩. ──
        private void BuildChildPanels()
        {
            _lightPanel = new InspectionLightPanel { Dock = DockStyle.Fill, EmbeddedMode = true };
            _lightPanel.SelectInspection(_node, _module?.AlgorithmKey ?? "", _finder?.Id ?? "");   // C2 — 조명 SSOT=노드
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
                if (r != null && r.IsSuccess) _cam.SetFrame(r);
            }
            catch (Exception ex) { StopLive(); Status(Lang.T("rec.liveStop") + ex.Message); }
        }

        private int ResolveDefaultLiveIntervalMs()
        {
            string key = (_module?.Name ?? string.Empty).ToLowerInvariant();
            if (key.Contains("bottom") || key.Contains("btm")) return 667;
            return 333;
        }

        // ── 파라미터(우측 ParameterGridControl) = B 실 finder 직접 바인딩 ──
        // 편집은 런타임 finder 속성에 직접 쓰고, 저장 시 노드(CollectFromRuntime)가 수집. SCOPE=계층.
        private void BuildParams()
        {
            if (_finder == null) return;
            var items = new System.Collections.Generic.List<ParameterGridItem>
            {
                ParameterGridItem.Double("Search X", "px", ParameterGridScope.Recipe, () => _finder.SearchRoi.CenterX, v => { _finder.SearchRoi.CenterX = v; RefreshOverlay(); }),
                ParameterGridItem.Double("Search Y", "px", ParameterGridScope.Recipe, () => _finder.SearchRoi.CenterY, v => { _finder.SearchRoi.CenterY = v; RefreshOverlay(); }),
                ParameterGridItem.Double("Search W", "px", ParameterGridScope.Recipe, () => _finder.SearchRoi.Width,   v => { _finder.SearchRoi.Width = v;   RefreshOverlay(); }),
                ParameterGridItem.Double("Search H", "px", ParameterGridScope.Recipe, () => _finder.SearchRoi.Height,  v => { _finder.SearchRoi.Height = v;  RefreshOverlay(); }),
                ParameterGridItem.Double("Train X", "px", ParameterGridScope.Recipe, () => _finder.TrainRoi.CenterX, v => { _finder.TrainRoi.CenterX = v; }),
                ParameterGridItem.Double("Train Y", "px", ParameterGridScope.Recipe, () => _finder.TrainRoi.CenterY, v => { _finder.TrainRoi.CenterY = v; }),
                ParameterGridItem.Double("Train W", "px", ParameterGridScope.Recipe, () => _finder.TrainRoi.Width,   v => { _finder.TrainRoi.Width = v;   }),
                ParameterGridItem.Double("Train H", "px", ParameterGridScope.Recipe, () => _finder.TrainRoi.Height,  v => { _finder.TrainRoi.Height = v;  }),
                ParameterGridItem.Double("Accept Threshold", "", ParameterGridScope.Recipe, () => _finder.AcceptThreshold, v => { _finder.AcceptThreshold = v; }),
                ParameterGridItem.Int("Max Instances", "", ParameterGridScope.Config, () => _finder.MaxInstances, v => { _finder.MaxInstances = v; }),
            };
            AppendNodeParams(items);   // ② 검사 전용 POCO 필드 칸(인프라 — 현재 케이스 0)
            _params.SetItems(items);
            _params.ParameterValueChanged += (s, e) => { RefreshOverlay(); UpdateRoiInfo(); MarkDirty(); };
        }

        /// <summary>② per-algorithm 전용필드 칸 확장점 — 노드 구체 Recipe/Config 캐스트해 POCO 바인딩(저장=POCO).
        /// 전용필드 추가 시 아래 패턴 1줄. (인프라: 현재 케이스 0 — 현 동작 불변.)</summary>
        private void AppendNodeParams(System.Collections.Generic.List<ParameterGridItem> items)
        {
            // 예) if (_node?.Recipe is EjectPinFinderRecipe r)
            //         items.Add(ParameterGridItem.Double("Pin Gap", "px", ParameterGridScope.Recipe,
            //                   () => r.PinGap, v => { r.PinGap = v; MarkDirty(); }));
        }

        private void RefreshOverlay()
        {
            if (_finder != null) _cam.SetOverlay(_finder.SearchRoi, null);
        }

        // ── dirty / 타깃 저장(상단바 SAVE 가 호출) — BaseUnit 노드 위임 ──
        // 레시피 파일: Recipes/default/<모듈.알고>.recipe.json (HasSavedData/상태점 일치용).
        private string TargetPath()
        {
            string key = _node?.StorageKey ?? ((_module?.StorageKey ?? "Unknown") + "." + (_finder?.Id ?? "x"));
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recipes", RecipeName, key + ".recipe.json");
        }

        private void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>타깃 저장 — 노드 SaveSettings(Config) + SaveRecipe(Recipe). Collect 가 런타임→POCO 수집.</summary>
        public void SaveTarget()
        {
            if (_node == null) { Status(Lang.T("rec.noSaveNode")); return; }
            try
            {
                _node.SaveSettings();
                _node.SaveRecipe(RecipeName);
                SaveTrainImageFile();          // 학습 패턴 PNG 영속화(레시피와 co-locate)
                _lightPanel?.PersistLight();   // R2e — 조명(별도 저장소) 유지
                _dirty = false;
                DirtyChanged?.Invoke(this, EventArgs.Empty);
                Status(Lang.T("rec.targetSaved") + TargetPath());
            }
            catch (Exception ex) { Status(Lang.T("rec.targetSaveFail") + ex.Message); }
        }

        private void LoadTarget()
        {
            if (_node == null) return;
            try
            {
                _node.LoadSettings();
                _node.LoadRecipe(RecipeName);   // Apply 가 POCO→런타임 finder 주입
                LoadTrainImageFile();           // 저장된 학습 패턴 PNG 복원
                _params.RefreshValues();
                RefreshOverlay();
            }
            catch { }
        }

        // ── 학습 패턴 이미지 영속화 (Recipes\<레시피명>\<StorageKey>.train.png) ──
        private string TrainImagePath()
        {
            string key = _node?.StorageKey ?? ((_module?.StorageKey ?? "Unknown") + "." + (_finder?.Id ?? "x"));
            return Path.Combine(
                QMC.Common.Data.Store.RecipeDataStore.DirOf(RecipeName),
                QMC.Common.Data.Store.StorageName.Safe(key) + ".train.png");
        }

        private void SaveTrainImageFile()
        {
            try
            {
                string path = TrainImagePath();
                var ti = _finder?.TrainImage;
                if (ti == null) { if (File.Exists(path)) File.Delete(path); return; }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var bmp = new Bitmap(ti))
                    bmp.Save(path, ImageFormat.Png);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[VisionTargetPage] train img save: " + ex.Message); }
        }

        private void LoadTrainImageFile()
        {
            try
            {
                if (_finder == null) return;
                string path = TrainImagePath();
                if (!File.Exists(path)) return;
                using (var src = (Bitmap)Image.FromFile(path))
                    _finder.LoadTrainImage(src);   // 내부에서 clone
                ShowTrainImage();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[VisionTargetPage] train img load: " + ex.Message); }
        }

        // ── 액션(중앙 3×3) — FinderPage 동일 로직 ──
        private GrabResult _lastGrab;
        private Bitmap _loadedImage;
        private Bitmap CurrentImage => _lastGrab?.Image ?? _loadedImage;

        private void OnGrabClick(object sender, EventArgs e) => DoGrab();
        private void OnMatchClick(object sender, EventArgs e) => DoMatch();
        private void OnTrainClick(object sender, EventArgs e) => DoTrain();
        private void OnLoadClick(object sender, EventArgs e) => DoLoad();
        private void OnSaveImageClick(object sender, EventArgs e) => DoSaveImage();
        private void OnEditSearchClick(object sender, EventArgs e) => BeginEditRoi(true);
        private void OnEditTrainClick(object sender, EventArgs e) => BeginEditRoi(false);

        private void OnCamRoiEdited(string which, Roi roi)
        {
            if (_finder == null) return;
            if (which == "Search") _finder.SearchRoi = roi;
            else if (which == "Train") _finder.TrainRoi = roi;
            Status($"{which} ROI updated: x={roi.CenterX:F0} y={roi.CenterY:F0} w={roi.Width:F0} h={roi.Height:F0}");
            _params.RefreshValues();
            SetTarget(which == "Train");   // ROI 패드 대상/표시/오버레이 동기화
            MarkDirty();
        }

        private void DoGrab()
        {
            if (_module == null) { Status("ERR: module not bound"); return; }
            _lastGrab?.Dispose(); _lastGrab = null;
            _loadedImage?.Dispose(); _loadedImage = null;
            _lastGrab = _module.Grab();
            if (_lastGrab.IsSuccess)
            {
                _cam.SetFrame(_lastGrab);
                _cam.SetOverlay(_finder?.SearchRoi, null);
                Status($"GRAB OK — {_lastGrab.Width}x{_lastGrab.Height} frame={_lastGrab.FrameNumber}");
            }
            else Status("GRAB FAIL: " + _lastGrab.ErrorMessage);
        }

        private void DoLoad()
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "Load image for pattern training/matching",
                Filter = "Image files|*.png;*.bmp;*.jpg;*.jpeg;*.tif;*.tiff|All files|*.*"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    _lastGrab?.Dispose(); _lastGrab = null;
                    _loadedImage?.Dispose();
                    using (var src = (Bitmap)Image.FromFile(dlg.FileName))
                        _loadedImage = new Bitmap(src);

                    var fake = GrabResult.Success(new Bitmap(_loadedImage), 0);
                    _cam.SetFrame(fake);
                    _cam.SetOverlay(_finder?.SearchRoi, null);
                    fake.Dispose();
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
                FileName = $"{(_module?.Name ?? "img")}_{(_finder?.Id ?? "x").Replace('/', '_')}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
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

        private void DoTrain()
        {
            if (_finder == null) { Status("ERR: finder not bound"); return; }
            var img = CurrentImage;
            if (img == null) { DoGrab(); img = CurrentImage; }
            if (img == null) { Status("TRAIN: no image"); return; }
            try
            {
                _finder.Train(img);
                MarkDirty();
                ShowTrainImage();
                Status($"TRAIN OK — pattern from rect[{_finder.TrainRoi.CenterX:F0},{_finder.TrainRoi.CenterY:F0} {_finder.TrainRoi.Width:F0}x{_finder.TrainRoi.Height:F0}]");
            }
            catch (Exception ex) { Status("TRAIN FAIL: " + ex.Message); }
        }

        /// <summary>학습된 패턴 이미지를 미리보기 PictureBox 에 표시(복제본).</summary>
        private void ShowTrainImage()
        {
            try
            {
                var ti = _finder?.TrainImage;
                var old = _trainPic.Image;
                _trainPic.Image = (ti != null) ? new System.Drawing.Bitmap(ti) : null;
                old?.Dispose();
            }
            catch { }
        }

        // ── ROI 미세조정 (기존 ROI/Finder 그대로 사용, UI 편의 추가) ──
        private bool _roiTrainTarget = true;
        private int  _moveStepPx = 10;
        private int  _sizeStepPx = 10;
        private Button _btnTgtTrain, _btnTgtSearch;
        private TextBox _txtMoveStep, _txtSizeStep;
        private Label  _roiInfo;

        private Roi ActiveRoi()
            => _finder == null ? null : (_roiTrainTarget ? _finder.TrainRoi : _finder.SearchRoi);

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

            // 우측: 대상 토글 / 스텝 / 표시
            var col = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, Width = 210, Height = 120, Margin = new Padding(8, 0, 0, 0) };
            _btnTgtTrain  = RoiFlow("Train ROI",  (s, e) => SetTarget(true),  96);
            _btnTgtSearch = RoiFlow("Search ROI", (s, e) => SetTarget(false), 96);
            var tgt = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(0) };
            tgt.Controls.Add(_btnTgtTrain); tgt.Controls.Add(_btnTgtSearch);

            _txtMoveStep = new TextBox { Width = 44, Text = _moveStepPx.ToString() };
            _txtSizeStep = new TextBox { Width = 44, Text = _sizeStepPx.ToString() };
            _txtMoveStep.TextChanged += (s, e) => { int v; if (int.TryParse(_txtMoveStep.Text, out v) && v > 0) _moveStepPx = v; };
            _txtSizeStep.TextChanged += (s, e) => { int v; if (int.TryParse(_txtSizeStep.Text, out v) && v > 0) _sizeStepPx = v; };
            var steps = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            steps.Controls.Add(new Label { Text = "Move", AutoSize = true, Margin = new Padding(0, 7, 2, 0) });
            steps.Controls.Add(_txtMoveStep);
            steps.Controls.Add(new Label { Text = "Size", AutoSize = true, Margin = new Padding(10, 7, 2, 0) });
            steps.Controls.Add(_txtSizeStep);

            _roiInfo = new Label { AutoSize = true, Margin = new Padding(0, 6, 0, 0), Font = UiTheme.ValueFont, ForeColor = System.Drawing.Color.DarkSlateGray };

            col.Controls.Add(tgt); col.Controls.Add(steps); col.Controls.Add(_roiInfo);

            flow.Controls.Add(pad); flow.Controls.Add(rz); flow.Controls.Add(col);
            _roiHost.Controls.Add(flow);

            SetTarget(_roiTrainTarget);
        }

        private Button RoiCell(string text, EventHandler on)
        {
            var b = new Button { Text = text, Dock = DockStyle.Fill, Margin = new Padding(2), FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            b.Click += on; return b;
        }
        private Button RoiFlow(string text, EventHandler on, int w)
        {
            var b = new Button { Text = text, Width = w, Height = 28, Margin = new Padding(0, 0, 4, 0), FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            b.Click += on; return b;
        }

        private void SetTarget(bool train)
        {
            _roiTrainTarget = train;
            if (_btnTgtTrain != null && _btnTgtSearch != null)
            {
                _btnTgtTrain.BackColor  = train  ? UiTheme.Accent : System.Drawing.Color.White;
                _btnTgtTrain.ForeColor  = train  ? System.Drawing.Color.White : System.Drawing.Color.Black;
                _btnTgtSearch.BackColor = !train ? UiTheme.Accent : System.Drawing.Color.White;
                _btnTgtSearch.ForeColor = !train ? System.Drawing.Color.White : System.Drawing.Color.Black;
            }
            if (_cam != null) _cam.SetOverlay(ActiveRoi(), null);
            UpdateRoiInfo();
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
            _params?.RefreshValues();   // 기존 파라미터 그리드와 동기화
            UpdateRoiInfo();
            MarkDirty();
        }
        private void UpdateRoiInfo()
        {
            if (_roiInfo == null) return;
            var r = ActiveRoi();
            _roiInfo.Text = (r == null) ? "" :
                $"[{(_roiTrainTarget ? "Train" : "Search")}]  Center ({r.CenterX:F0}, {r.CenterY:F0})\r\nSize ({r.Width:F0} x {r.Height:F0})";
        }

        private void DoMatch()
        {
            if (_finder == null) { Status("ERR: finder not bound"); return; }
            var img = CurrentImage;
            if (img == null) { DoGrab(); img = CurrentImage; }
            if (img == null) { Status("MATCH: no image"); return; }
            try
            {
                var r = _finder.Match(img);
                if (r.Success)
                {
                    // 누적: 최신 결과를 맨 위(0번)에 삽입 → 위에서 아래로 최신→과거.
                    int at = 0;
                    foreach (var m in r.Instances)
                        _result.Rows.Insert(at++, 0, m.CenterX.ToString("F3"), m.CenterY.ToString("F3"),
                                            m.AngleDeg.ToString("F3"), m.Score.ToString("F3"));
                    RenumberResults();
                    Status($"MATCH OK — {r.Instances.Count} instance(s), best score={r.Best?.Score:F3}");
                }
                else Status("MATCH FAIL: " + r.ErrorMessage);
                _cam.SetOverlay(_finder.SearchRoi, r);
            }
            catch (Exception ex) { Status("MATCH FAIL: " + ex.Message); }
        }

        /// <summary>결과 그리드 Idx 재번호 — 맨 위(최신)=0, 아래로 증가.</summary>
        private void RenumberResults()
        {
            for (int i = 0; i < _result.Rows.Count; i++)
                _result.Rows[i].Cells[0].Value = i;
        }

        private void OnClearResultClick(object sender, EventArgs e) => _result.Rows.Clear();

        // ── 측정(Measure) — 토글 ──
        private bool _measureOn;
        private System.Windows.Forms.ToolStripMenuItem _measureMenuItem;

        private void OnMeasureClick(object sender, EventArgs e)
        {
            _measureOn = !_measureOn;
            if (_measureOn) { ApplyScaleToCam(); _cam?.BeginMeasure(); }
            else            { _cam?.EndMeasure(); }
            UpdateMeasureButton();
            Status(_measureOn
                ? Lang.T("rec.measureOnHint")
                : Lang.T("rec.measureOff"));
        }

        /// <summary>측정 활성 상태를 메뉴에 시각적으로 구분 표시.(측정 버튼은 CameraView 툴바로 통일)</summary>
        private void UpdateMeasureButton()
        {
            if (_measureMenuItem != null) _measureMenuItem.Checked = _measureOn;
        }

        /// <summary>카메라 스케일(mm/px)을 CameraView 에 주입 — 측정 mm 환산용. (모듈별 CameraConfig SSOT)</summary>
        private void ApplyScaleToCam()
        {
            try
            {
                if (_cam == null || _module == null) return;
                var map = _module.ExportCameraMapping();
                _cam.MmPerPixelX = map.ScaleX;
                _cam.MmPerPixelY = map.ScaleY;
            }
            catch { }
        }

        // ── 카메라 우클릭 컨텍스트 메뉴 ──
        private void BuildCamContextMenu()
        {
            if (_cam == null) return;
            var cms = new ContextMenuStrip();
            cms.Items.Add("Live",  null, (s, e) => StartLive());
            cms.Items.Add("Stop",  null, (s, e) => StopLive());
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("Image load",  null, (s, e) => DoLoad());
            cms.Items.Add("Image save",  null, (s, e) => DoSaveImage());
            cms.Items.Add(new ToolStripSeparator());
            _measureMenuItem = new ToolStripMenuItem(Lang.T("rec.measureMenu"));
            _measureMenuItem.Click += (s, e) => OnMeasureClick(s, e);
            cms.Items.Add(_measureMenuItem);
            cms.Items.Add("Result overlay clear", null, (s, e) => { if (_cam != null) _cam.SetOverlay(_finder?.SearchRoi, null); });
            cms.Items.Add(new ToolStripSeparator());
            // 줌은 CameraView 제자리(휠=확대/축소, 가운데드래그=이동, 더블클릭=맞춤). 메뉴는 배율 프리셋만 제공.
            cms.Items.Add("Image auto fit", null, (s, e) => _cam?.ZoomFit());
            cms.Items.Add("x2", null, (s, e) => _cam?.SetZoom(2));
            cms.Items.Add("x4", null, (s, e) => _cam?.SetZoom(4));
            cms.Items.Add("x8", null, (s, e) => _cam?.SetZoom(8));
            _cam.ContextMenuStrip = cms;
        }

        private void BeginEditRoi(bool isSearch)
        {
            if (_finder == null) { Status("ERR: finder not bound"); return; }
            _cam.BeginRoiDrag(isSearch ? "Search" : "Train", isSearch ? _finder.SearchRoi : _finder.TrainRoi);
            Status($"Drag a rectangle on the image to set {(isSearch ? "SEARCH" : "TRAIN")} ROI…");
        }

        private void Status(string s) { if (_lblStatus != null) _lblStatus.Text = s; }
    }
}
