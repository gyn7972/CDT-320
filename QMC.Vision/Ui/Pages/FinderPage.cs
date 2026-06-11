using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// Finder 공용 페이지 — 좌측 CameraView + JogBox + 검사별 조명, 우측 결과 테이블 + Train/Match.
    /// Stage 94 — Designer/Code 분리. 정적 shell 은 .Designer.cs, 그랩/오버레이/검색 로직·런타임 자식패널은 Code.
    /// </summary>
    public partial class FinderPage : UserControl
    {
        private readonly IVisionModule _module;
        private readonly IPatternFinder _finder;

        /// <summary>디자이너용 파라미터 없는 생성자.</summary>
        public FinderPage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildChildPanels();
        }

        public FinderPage(IVisionModule module, IPatternFinder finder)
        {
            _module = module; _finder = finder;
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildChildPanels();
            Text = module.Name + " / " + finder.Id;
        }

        /// <summary>런타임 의존 자식 패널(주입 _module/_finder 기반) — Designer 직렬화 불가라 Code 유지.</summary>
        private void BuildChildPanels()
        {
            // Stage 70 E — 검사별 InspectionLightPanel (좌하단). C2: 조명 SSOT=노드, 참조로 해석해 주입.
            var illum = new InspectionLightPanel { Location = new Point(6, 544), Size = new Size(440, 280) };
            illum.SelectInspection(LightNode(), _module?.AlgorithmKey ?? "", _finder?.Id ?? "");
            Controls.Add(illum);

            // Stage 87 — 라이브 튜닝 패널 우측 빈 공간 (720, 560). 카메라 라이브 + 조명 펄스 통합.
            var liveTuning = new LightLiveTuningPanel
            {
                Location = new Point(720, 560), Size = new Size(540, 264), Name = "_liveTuning"
            };
            liveTuning.Initialize(CollectRowsForLiveTuning);
            liveTuning.BindCameraLive(() => StartLive(0), () => StopLive());
            Controls.Add(liveTuning);
        }

        // ── 이벤트 핸들러 (Designer 에서 named 연결) ──
        private void OnGrabClick(object sender, EventArgs e) => DoGrab();
        private void OnLoadClick(object sender, EventArgs e) => DoLoad();
        private void OnSaveClick(object sender, EventArgs e) => DoSave();
        private void OnTrainClick(object sender, EventArgs e) => DoTrain();
        private void OnMatchClick(object sender, EventArgs e) => DoMatch();
        private void OnEditSearchClick(object sender, EventArgs e) => BeginEditRoi(true);
        private void OnEditTrainClick(object sender, EventArgs e) => BeginEditRoi(false);

        private void OnCamRoiEdited(string which, Roi roi)
        {
            if (_finder == null) return;
            if (which == "Search") _finder.SearchRoi = roi;
            else if (which == "Train") _finder.TrainRoi = roi;
            Status($"{which} ROI updated: x={roi.CenterX:F0} y={roi.CenterY:F0} w={roi.Width:F0} h={roi.Height:F0}");
            _cam.SetOverlay(_finder.SearchRoi, null);
        }

        // ── Stage 87 — 카메라 라이브 grab loop (라이브 튜닝 패널이 start/stop 트리거) ──
        private System.Windows.Forms.Timer _liveTimer;
        private bool _liveOn;

        /// <summary>카메라 라이브 grab 시작. intervalMs=0 이면 검사별 기본 주기(Bottom 667ms / 그 외 333ms).</summary>
        public void StartLive(int intervalMs = 0)
        {
            if (_liveOn) return;
            if (_liveTimer == null)
            {
                _liveTimer = new System.Windows.Forms.Timer();
                _liveTimer.Tick += OnLiveTick;
            }
            _liveTimer.Interval = intervalMs > 0 ? intervalMs : ResolveDefaultLiveIntervalMs();
            _liveTimer.Start();
            _liveOn = true;
        }

        public void StopLive()
        {
            if (_liveTimer != null) _liveTimer.Stop();
            _liveOn = false;
        }

        public bool IsLiveOn => _liveOn;

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

        /// <summary>검사/카메라명 기반 권장 grab 주기 — Bottom 계열 667ms(≈1.5fps), 그 외 333ms(≈3fps).</summary>
        private int ResolveDefaultLiveIntervalMs()
        {
            string key = (_module?.Name ?? string.Empty).ToLowerInvariant();
            if (key.Contains("bottom") || key.Contains("btm")) return 667;
            return 333;
        }

        /// <summary>C2 — 현재 검사 노드의 조명 레벨(Recipe.LightSettings)을 TuningRow 로 변환 (라이브 튜닝 송신 소스).</summary>
        private IAlgorithmNode LightNode() => _module?.Algorithms.FirstOrDefault(a => a.Finder == _finder);

        private IEnumerable<LightLiveTuningPanel.TuningRow> CollectRowsForLiveTuning()
        {
            var settings = (LightNode()?.Recipe as AlgoRecipeBase)?.LightSettings;
            if (settings == null) yield break;
            foreach (var s in settings)
                if (!string.IsNullOrEmpty(s.ControllerPort) && s.Channel > 0)
                    yield return new LightLiveTuningPanel.TuningRow
                    { ControllerPort = s.ControllerPort, Channel = s.Channel, Level = s.Level };
        }

        private GrabResult _lastGrab;
        private Bitmap    _loadedImage;     // LOAD 로 가져온 이미지 (GrabResult 가 아님)

        /// <summary>현재 활성 이미지 — Grab 또는 Load 둘 중 마지막.</summary>
        private Bitmap CurrentImage => _lastGrab?.Image ?? _loadedImage;

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
            else
            {
                Status("GRAB FAIL: " + _lastGrab.ErrorMessage);
            }
        }

        private void DoLoad()
        {
            using (var dlg = new OpenFileDialog
            {
                Title  = "Load image for pattern training/matching",
                Filter = "Image files|*.png;*.bmp;*.jpg;*.jpeg;*.tif;*.tiff|All files|*.*"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    _lastGrab?.Dispose(); _lastGrab = null;
                    _loadedImage?.Dispose();
                    using (var src = (Bitmap)Image.FromFile(dlg.FileName))
                        _loadedImage = new Bitmap(src);   // 파일 핸들 즉시 해제

                    // GrabResult 호환을 위해 임시 wrap
                    var fake = GrabResult.Success(new Bitmap(_loadedImage), 0);
                    _cam.SetFrame(fake);
                    _cam.SetOverlay(_finder?.SearchRoi, null);
                    fake.Dispose();
                    Status($"LOAD OK — {_loadedImage.Width}x{_loadedImage.Height}  ({Path.GetFileName(dlg.FileName)})");
                }
                catch (Exception ex)
                {
                    Status("LOAD FAIL: " + ex.Message);
                }
            }
        }

        private void DoSave()
        {
            var img = CurrentImage;
            if (img == null) { Status("SAVE: no image (do GRAB or LOAD first)"); return; }
            using (var dlg = new SaveFileDialog
            {
                Title    = "Save current image",
                Filter   = "PNG|*.png|BMP|*.bmp|JPEG|*.jpg",
                FileName = $"{(_module?.Name ?? "img")}_{(_finder?.Id ?? "x").Replace('/','_')}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
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
                    Status("SAVE OK: " + dlg.FileName);
                }
                catch (Exception ex) { Status("SAVE FAIL: " + ex.Message); }
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
            _cam.BeginRoiDrag(isSearch ? "Search" : "Train",
                              isSearch ? _finder.SearchRoi : _finder.TrainRoi);
            Status($"Drag a rectangle on the image to set {(isSearch ? "SEARCH" : "TRAIN")} ROI…");
        }

        private void Status(string s)
        {
            if (_lblStatus != null) _lblStatus.Text = s;
        }
    }
}
