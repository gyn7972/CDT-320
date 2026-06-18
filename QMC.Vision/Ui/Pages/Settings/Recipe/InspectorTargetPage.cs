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
            if (_inspector != null) _cam.SetOverlay(_inspector.InspectionRoi, null);
            if (!_langHooked) { Lang.LanguageChanged += OnLanguageChanged; _langHooked = true; }
            ApplyLanguage();
            Status((module?.Name ?? "?") + " / " + (inspector?.Id ?? "?"));
        }

        private void WireCamera()
        {
            _cam.RoiEdited += OnCamRoiEdited;
            // 공용 CameraView 내장 툴바 — 모듈 지정 한 줄.
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
            _lightPanel = new InspectionLightPanel { Dock = DockStyle.Fill, EmbeddedMode = true };
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
                items.Add(ParameterGridItem.Int("Threshold", "", ParameterGridScope.Recipe, () => cog.Threshold, v => { cog.Threshold = v; }));
            AppendNodeParams(items);   // ② 검사 전용 POCO 필드 칸(인프라 — 현재 케이스 0)
            _params.SetItems(items);
            _params.ParameterValueChanged += (s, e) => { RefreshOverlay(); MarkDirty(); };
        }

        /// <summary>② per-algorithm 전용필드 칸 확장점 — 노드 구체 Recipe/Config 캐스트해 POCO 바인딩(저장=POCO).
        /// 전용필드 추가 시 아래 패턴 1줄. (인프라: 현재 케이스 0 — 현 동작 불변.)</summary>
        private void AppendNodeParams(System.Collections.Generic.List<ParameterGridItem> items)
        {
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
                _node.SaveRecipe(RecipeName);
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
                _node.LoadRecipe(RecipeName);   // Apply 가 POCO→런타임 inspector 주입
                _params.RefreshValues();
                RefreshOverlay();
            }
            catch { }
        }

        // ── 액션(중앙) — InspectorPage 동일 로직 ──
        private GrabResult _lastGrab;
        private Bitmap _loadedImage;
        private Bitmap CurrentImage => _lastGrab?.Image ?? _loadedImage;

        private void OnGrabClick(object sender, EventArgs e) => DoGrab();
        private void OnInspectClick(object sender, EventArgs e) => DoInspect();
        private void OnLoadClick(object sender, EventArgs e) => DoLoad();
        private void OnSaveImageClick(object sender, EventArgs e) => DoSaveImage();
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
            _lastGrab = _module.Grab();
            if (_lastGrab.IsSuccess)
            {
                _cam.SetFrame(_lastGrab);
                _cam.SetOverlay(_inspector?.InspectionRoi, null);
                Status($"GRAB OK — {_lastGrab.Width}x{_lastGrab.Height} frame={_lastGrab.FrameNumber}");
            }
            else Status("GRAB FAIL: " + _lastGrab.ErrorMessage);
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
                    using (var src = (Bitmap)Image.FromFile(dlg.FileName))
                        _loadedImage = new Bitmap(src);

                    var fake = GrabResult.Success(new Bitmap(_loadedImage), 0);
                    _cam.SetFrame(fake);
                    _cam.SetOverlay(_inspector?.InspectionRoi, null);
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
                _cam.SetOverlay(_inspector.InspectionRoi, null);
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
