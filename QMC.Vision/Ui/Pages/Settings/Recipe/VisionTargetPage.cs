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
            // 툴바 Grab/Live(=CameraView 자체 핸들러)로 프레임이 바뀌어도 STAGE/ROI 자동맞춤이 되도록 알림 수신.
            _cam.FrameChanged += OnCamFrameChanged;
            // 공용 CameraView 내장 툴바(Grab/Live/Stop/Save/Load/측정/맞춤) 사용 — 모듈 지정 한 줄.
            _cam.AttachModule(_module);
            // 툴바 Grab 이 이 Finder 전용 시뮬 저장이미지(GrabForTool)를 쓰도록 활성 도구 id 지정.
            _cam.SetActiveTool(ResolveToolId());
            _cam.ShowToolbar = true;
        }

        /// <summary>CameraView 프레임 크기 변경(툴바 Grab/Live 포함) → STAGE 갱신 + 미구성 ROI 자동맞춤.</summary>
        private void OnCamFrameChanged()
        {
            try { OnImageReady(_cam?.CurrentFrame); } catch { }
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
            _lightPanel = new InspectionLightPanel { Dock = DockStyle.Fill, EmbeddedMode = true, RecipeName = RecipeName };
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
                if (r != null && r.IsSuccess)
                {
                    // 라이브 프레임을 현재 이미지로 유지 — Train/Match/Full Size 가 라이브 중에도 동작.
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
                ParameterGridItem.Bool("Angle Search", ParameterGridScope.Config, () => _finder.AngleEnabled, v => { _finder.AngleEnabled = v; }),
                ParameterGridItem.Double("Angle Tol (±deg)", "deg", ParameterGridScope.Config, () => _finder.AngleToleranceDeg, v => { _finder.AngleToleranceDeg = v; }),
                ParameterGridItem.Double("Angle Step", "deg", ParameterGridScope.Config, () => _finder.AngleStepDeg, v => { _finder.AngleStepDeg = v; }),
            };
            AppendNodeParams(items);   // ② 검사 전용 POCO 필드 칸(인프라 — 현재 케이스 0)
            _params.SetItems(items);
            _params.ParameterValueChanged += (s, e) => { RefreshOverlay(); UpdateRoiInfo(); MarkDirty(); };
        }

        /// <summary>② per-algorithm 전용필드 칸 확장점 — 노드 구체 Recipe/Config 캐스트해 POCO 바인딩(저장=POCO).
        /// 전용필드 추가 시 아래 패턴 1줄. (인프라: 현재 케이스 0 — 현 동작 불변.)</summary>
        private void AppendNodeParams(System.Collections.Generic.List<ParameterGridItem> items)
        {
            if (_node == null) return;

            // 도구별 시뮬 저장이미지 — 웨이퍼 2점 정렬의 이미지1/이미지2처럼 Finder 마다 다른 이미지 지정.
            // (지정 없으면 모듈 저장이미지/실제 카메라로 폴백. 클릭 시 파일 찾아보기로 경로 설정.)
            // 로드 시 Setup/Recipe POCO 인스턴스가 교체될 수 있어 람다에서 매번 _node 로 최신 POCO 를 읽는다.
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

            // 각도(θ) 산출 모드 — 웨이퍼 비전의 'AlignDie'(얼라인 다이) 도구에서만 노출.
            //   Single     = 다이 하나만 얼라인(최근접 매칭 각도)
            //   AverageAll = 화면 내 다이 격자 전체를 얼라인해 평균각 사용
            // 다른 모듈/도구(Reticle·Die·DieEdge 등)는 각도 평균 개념이 없으므로 표시하지 않는다.
            if (_node.Recipe is QMC.Vision.Modules.FinderAlgoRecipe
                && _module is QMC.Vision.Modules.WaferVisionModule wm
                && ReferenceEquals(_finder, wm.AlignDie))
            {
                items.Add(ParameterGridItem.Selection<QMC.Vision.Modules.DieAngleMode>(
                    "각도(θ) 모드", "", ParameterGridScope.Recipe,
                    () => (_node.Recipe as QMC.Vision.Modules.FinderAlgoRecipe)?.AngleMode ?? QMC.Vision.Modules.DieAngleMode.Single,
                    v => { if (_node.Recipe is QMC.Vision.Modules.FinderAlgoRecipe fr) { fr.AngleMode = v; MarkDirty(); } }));
            }
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
                _lightPanel?.PersistLight();   // 조명 레벨을 recipe POCO 에 반영(저장은 아래 SaveRecipe 가 활성 레시피로 일괄)
                _node.SaveRecipe(RecipeName);
                SaveTrainImageFile();          // 학습 패턴 PNG 영속화(레시피와 co-locate)
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
                _node.LoadRecipe(RecipeName);   // Apply 가 POCO→런타임 finder 주입
                LoadTrainImageFile();           // 저장된 학습 패턴 PNG 복원
                _params.RefreshValues();
                RefreshOverlay();
                ShowTrainImage();               // 패턴 미리보기도 복원본으로 갱신
                _dirty = false;                 // 저장본으로 되돌렸으므로 변경상태 해제
                DirtyChanged?.Invoke(this, EventArgs.Empty);
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
                // 고속 로드(컬러매니지먼트/검증 생략) — 학습 패턴 원본 픽셀 보존. Image.FromFile 최초 디코드 지연 회피.
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var src = (Bitmap)System.Drawing.Image.FromStream(fs, false, false))
                    _finder.LoadTrainImage(src);   // 내부에서 clone
                ShowTrainImage();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[VisionTargetPage] train img load: " + ex.Message); }
        }

        // ── 액션(중앙 3×3) — FinderPage 동일 로직 ──
        private GrabResult _lastGrab;
        private Bitmap _loadedImage;
        // 페이지 자체 Grab/Load 외에, 툴바 Grab/Live 로 CameraView 가 표시 중인 실제 프레임도 사용.
        private Bitmap CurrentImage => _lastGrab?.Image ?? _loadedImage ?? _cam?.CurrentFrame;

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
            // 도구(Finder) 전용 시뮬 저장이미지 우선 — SimUseSavedImage=true 면 SimSavedImagePath 로드,
            // 아니면(또는 경로 미지정) GrabForTool 내부에서 카메라 Grab 으로 폴백.
            // 주의: GrabForTool/GetAlgorithm 은 등록 id(Finders 딕셔너리 키)를 받는다.
            //       _finder.Id 는 "모듈명/등록id" 전체이름이라 키와 다르므로 ResolveToolId 로 등록 키를 구한다.
            _lastGrab = _module.GrabForTool(ResolveToolId());
            if (_lastGrab.IsSuccess)
            {
                _cam.SetFrame(_lastGrab);
                OnImageReady(_lastGrab.Image);
                Status($"GRAB OK — {_lastGrab.Width}x{_lastGrab.Height} frame={_lastGrab.FrameNumber}");
            }
            else Status("GRAB FAIL: " + _lastGrab.ErrorMessage);
        }

        /// <summary>현재 finder 의 등록 id(Finders 딕셔너리 키 = _algoById 키) 반환. GrabForTool/GetAlgorithm 용.
        /// _finder.Id("모듈명/등록id" 전체이름)와 다르므로 dict 역참조로 키를 찾는다. 못 찾으면 null(카메라 폴백).</summary>
        private string ResolveToolId()
        {
            if (_module == null || _finder == null) return null;
            foreach (var kv in _module.Finders)
                if (ReferenceEquals(kv.Value, _finder)) return kv.Key;
            return null;
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
                    // Image.FromFile 은 GDI+ 컬러매니지먼트/검증으로 대용량 최초 디코드가 매우 느리다.
                    // 스트림 + (useEmbeddedColorManagement:false, validateImageData:false) 로 고속 로드(원본 픽셀 보존).
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
            catch (Exception ex) { Status("TRAIN FAIL: " + ex.Message); return; }

            // 학습 직후 실제 이미지에서 패턴 검출을 한번 실행해 OK/NG 오버레이로 확인.
            TryShowDetection(img, "TRAIN");
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
        /// <summary>그랩/로드된 이미지에 맞춰 STAGE 표시 갱신 + 미구성(640×480 가정) ROI 1회 자동 배치 + 오버레이/정보 갱신.</summary>
        private void OnImageReady(Bitmap img)
        {
            if (img == null) return;
            if (_cam != null) _cam.InfoText = "STAGE\r\nW:" + img.Width + " H:" + img.Height;
            bool changed = FitDefaultRoisToImage(img);
            if (_cam != null) _cam.SetOverlay(ActiveRoi(), null);
            _params?.RefreshValues();   // 자동맞춤된 ROI 를 PARAM 그리드에 즉시 반영(표시 동기화)
            UpdateRoiInfo();
            if (changed) MarkDirty();
        }

        /// <summary>구성 전 기본 ROI(생성자값: 중심 320,240 또는 0,0)는 실제 이미지에 맞춘다 —
        /// Search=전체, Train=중앙 1/4. 사용자가 이미 조정한 ROI 는 손대지 않는다(좌표가 기본값일 때만).
        /// 중심이 이미지 밖이면 안전하게 중앙으로 재배치. 변경되면 true.</summary>
        private bool FitDefaultRoisToImage(Bitmap img)
        {
            if (_finder == null) return false;
            int iw = img.Width, ih = img.Height;
            bool changed = false;
            if (IsUnconfiguredRoi(_finder.SearchRoi, iw, ih))
            {
                _finder.SearchRoi.CenterX = iw / 2.0; _finder.SearchRoi.CenterY = ih / 2.0;
                _finder.SearchRoi.Width   = iw;       _finder.SearchRoi.Height  = ih;
                changed = true;
            }
            if (IsUnconfiguredRoi(_finder.TrainRoi, iw, ih))
            {
                _finder.TrainRoi.CenterX = iw / 2.0;  _finder.TrainRoi.CenterY = ih / 2.0;
                _finder.TrainRoi.Width   = iw / 4.0;  _finder.TrainRoi.Height  = ih / 4.0;
                changed = true;
            }
            return changed;
        }

        /// <summary>ROI 가 미구성(=생성자 기본 좌표)이거나 중심이 이미지 밖이면 true.</summary>
        private static bool IsUnconfiguredRoi(Roi r, int iw, int ih)
        {
            if (r == null) return false;
            bool defaultCenter = (System.Math.Abs(r.CenterX - 320) < 0.5 && System.Math.Abs(r.CenterY - 240) < 0.5)
                              || (r.CenterX < 0.5 && r.CenterY < 0.5);
            bool centerOutside = r.CenterX < 0 || r.CenterY < 0 || r.CenterX > iw || r.CenterY > ih;
            // 실제 이미지가 기본 가정(640×480)보다 큰데 ROI 가 기본 좌표면 미구성으로 본다.
            return (defaultCenter && (iw > 800 || ih > 600)) || centerOutside;
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

        private bool _matchBusy;
        private void DoMatch()
        {
            if (_matchBusy) { Status("MATCH 진행 중…"); return; }   // 연타 재진입 무시(프리즈/중복 방지)
            if (_finder == null) { Status("ERR: finder not bound"); return; }
            var img = CurrentImage;
            if (img == null) { DoGrab(); img = CurrentImage; }
            if (img == null) { Status("MATCH: no image"); return; }
            _matchBusy = true;
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
                }
                ShowDetectionResult(r, "MATCH");
            }
            catch (Exception ex) { Status("MATCH FAIL: " + ex.Message); }
            finally { _matchBusy = false; }
        }

        /// <summary>현재 이미지에서 패턴 검출을 실행해 OK/NG 오버레이를 표시한다(학습 직후 검증용).</summary>
        private void TryShowDetection(Bitmap img, string tag)
        {
            if (_finder == null || img == null) return;
            try { ShowDetectionResult(_finder.Match(img), tag); }
            catch (Exception ex) { Status(tag + " 검출 표시 실패: " + ex.Message); }
        }

        /// <summary>매칭 결과를 실제 이미지에 오버레이하고 AcceptThreshold 기준 OK/NG 를 표시한다.
        /// AcceptThreshold 가 0 이하이면 게이트 없이 '검출되면 OK'로 본다.</summary>
        private void ShowDetectionResult(MatchResult r, string tag)
        {
            // 매칭 박스(검출 각도로 회전) 크기 = Train ROI(학습 패턴 크기). 없으면 점+점수만.
            double boxW = _finder?.TrainRoi?.Width  ?? 0.0;
            double boxH = _finder?.TrainRoi?.Height ?? 0.0;
            if (_cam != null) _cam.SetOverlay(_finder?.SearchRoi, r, boxW, boxH);

            bool   found = r != null && r.Success && r.Best != null;
            double thr   = _finder?.AcceptThreshold ?? 0.0;
            double score = found ? r.Best.Score : 0.0;
            bool   ok    = found && (thr <= 0.0 || score >= thr);

            string label = !found
                ? "검출 NG — " + (string.IsNullOrEmpty(r?.ErrorMessage) ? "no match" : r.ErrorMessage)
                : ok
                    ? "검출 OK  score=" + score.ToString("F3") + (thr > 0 ? " (>= " + thr.ToString("F2") + ")" : " (threshold 0)")
                    : "검출 NG  score=" + score.ToString("F3") + " (< " + thr.ToString("F2") + ")";

            if (_cam != null) _cam.InfoText = (_finder?.Id ?? "") + "\r\n" + label;
            Status("[" + tag + "] " + label);
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
                if (_cam == null) return;
                _cam.MmPerPixelX = 0; _cam.MmPerPixelY = 0;   // 측정 px 단위(스케일 무관). mm 필요시 ExportCameraMapping().ScaleX/Y 주입.
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
            cms.Items.Add(new ToolStripSeparator());
            var coordItem = new ToolStripMenuItem("좌표·밝기 표시 (PX / V)") { CheckOnClick = true, Checked = false };
            coordItem.Click += (s, e) => { if (_cam != null) _cam.ShowCursorReadout = coordItem.Checked; };
            cms.Items.Add(coordItem);
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
