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
using QMC.Vision.Core.Parameters;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// R2b — Handler VisionRecipePage 미러(3열 TLP). 좌 카메라+매치 / 중 ACTION 3×3 / 우 ParameterGridControl+JOG+SPEED.
    /// ROI 라디오 제거(세팅선택기는 RecipePage 영속 바). 액션 SAVE=이미지저장(상단바 SAVE=타깃 레시피저장).
    /// dirty 추적(세팅 단위) + SaveTarget/LoadTarget(finder.SaveParameters/LoadParameters). JOG/SPEED inert.
    /// 기능(Grab/Match/Train/Load/EditROI)은 FinderPage 동일.
    /// </summary>
    public partial class VisionTargetPage : UserControl, ITargetPage
    {
        private readonly IVisionModule _module;
        private readonly IPatternFinder _finder;
        private bool _dirty;
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

        public VisionTargetPage(IVisionModule module, IPatternFinder finder)
        {
            _module = module; _finder = finder;
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            WireCamera();
            BuildParams();
            LoadTarget();
            BuildChildPanels();
            if (_finder != null) _cam.SetOverlay(_finder.SearchRoi, null);
            Status((module?.Name ?? "?") + " / " + (finder?.Id ?? "?"));
        }

        private void WireCamera()
        {
            _cam.RoiEdited += OnCamRoiEdited;
        }

        // ── 우측: 검사 조명(InspectionLightPanel) + 라이브튜닝(LightLiveTuningPanel) 주입(런타임) ──
        private void BuildChildPanels()
        {
            _lightPanel = new InspectionLightPanel(_module?.AlgorithmKey ?? "", _finder?.Id ?? "") { Dock = DockStyle.Fill, EmbeddedMode = true };
            _lightPanel.LightChanged += (s, e) => MarkDirty();   // R2e — 조명 변경 → 상태점 점등
            _lightHost.Controls.Add(_lightPanel);

            var live = new LightLiveTuningPanel { Dock = DockStyle.Fill };
            live.Initialize(CollectRowsForLiveTuning);
            live.BindCameraLive(() => StartLive(0), () => StopLive());
            _liveHost.Controls.Add(live);
        }

        // ── 카메라 라이브 grab loop (라이브튜닝 패널 start/stop 트리거) — FinderPage 동일 ──
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
            catch (Exception ex) { StopLive(); Status("LIVE 정지: " + ex.Message); }
        }

        private int ResolveDefaultLiveIntervalMs()
        {
            string key = (_module?.Name ?? string.Empty).ToLowerInvariant();
            if (key.Contains("bottom") || key.Contains("btm")) return 667;
            return 333;
        }

        private IEnumerable<LightLiveTuningPanel.TuningRow> CollectRowsForLiveTuning()
        {
            var ov = AlgorithmCameraMapStore.Current?.Get(_module?.AlgorithmKey)?.GetLightOverride(_finder?.Id);
            if (ov?.Settings == null) yield break;
            foreach (var s in ov.Settings)
                if (!string.IsNullOrEmpty(s.ControllerPort) && s.Channel > 0)
                    yield return new LightLiveTuningPanel.TuningRow
                    { ControllerPort = s.ControllerPort, Channel = s.Channel, Level = s.Level };
        }

        // ── 파라미터(우측 ParameterGridControl) = P4 스토어 질의(GetByTarget) → FromDescriptor ──
        // 타깃 전 디스크립터(Setup ROI + Recipe 임계/Train ROI + ② 등) 노출, SCOPE=계층. scope 하드코딩 제거.
        private void BuildParams()
        {
            if (_finder == null) return;
            var store = ParameterStoreHost.Current;
            if (store != null)
                _params.SetItems(store.GetByTarget(_finder.Id)
                    .Where(d => d.Domain != ParameterDomain.Lighting)   // 조명=전용 패널 담당, 그리드 제외
                    .Select(d => ParameterGridItem.FromDescriptor(d, store))
                    .Where(x => x != null));
            _params.ParameterValueChanged += (s, e) => { RefreshOverlay(); MarkDirty(); };
        }

        private void RefreshOverlay()
        {
            if (_finder != null) _cam.SetOverlay(_finder.SearchRoi, null);
        }

        // ── dirty / 타깃 저장(상단바 SAVE 가 호출) ──
        private string TargetPath()
        {
            string alg = _module?.AlgorithmKey ?? "Unknown";
            string id = (_finder?.Id ?? "x").Replace('/', '_');
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "VisionRecipe", alg, id + ".json");
        }

        private void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>타깃 레시피 저장 — finder 파라미터(ROI 등)를 Config/VisionRecipe/&lt;alg&gt;/&lt;id&gt;.json 으로.</summary>
        public void SaveTarget()
        {
            if (_finder == null) { Status("저장 대상 없음"); return; }
            try
            {
                string path = TargetPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                _finder.SaveParameters(path);
                _lightPanel?.PersistLight();   // R2e — 통합 저장(ROI + 조명)
                _dirty = false;
                DirtyChanged?.Invoke(this, EventArgs.Empty);
                Status("타깃 저장됨 — " + path);
            }
            catch (Exception ex) { Status("타깃 저장 실패: " + ex.Message); }
        }

        private void LoadTarget()
        {
            if (_finder == null) return;
            try
            {
                string path = TargetPath();
                if (File.Exists(path))
                {
                    _finder.LoadParameters(path);
                    _params.RefreshValues();
                    RefreshOverlay();
                }
            }
            catch { }
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
            _cam.SetOverlay(_finder.SearchRoi, null);
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
            if (img == null) { Status("이미지저장: 이미지 없음 (GRAB/LOAD 먼저)"); return; }
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
                    Status("이미지 저장 OK: " + dlg.FileName);
                }
                catch (Exception ex) { Status("이미지 저장 실패: " + ex.Message); }
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
                Status($"TRAIN OK — pattern from rect[{_finder.TrainRoi.CenterX:F0},{_finder.TrainRoi.CenterY:F0} {_finder.TrainRoi.Width:F0}x{_finder.TrainRoi.Height:F0}]");
            }
            catch (Exception ex) { Status("TRAIN FAIL: " + ex.Message); }
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
                _result.Rows.Clear();
                if (r.Success)
                {
                    foreach (var m in r.Instances)
                        _result.Rows.Add(m.Index, m.CenterX.ToString("F3"), m.CenterY.ToString("F3"),
                                         m.AngleDeg.ToString("F3"), m.Score.ToString("F3"));
                    Status($"MATCH OK — {r.Instances.Count} instance(s), best score={r.Best?.Score:F3}");
                }
                else Status("MATCH FAIL: " + r.ErrorMessage);
                _cam.SetOverlay(_finder.SearchRoi, r);
            }
            catch (Exception ex) { Status("MATCH FAIL: " + ex.Message); }
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
