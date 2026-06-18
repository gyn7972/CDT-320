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
    /// Inspector 공용 페이지 — 좌측 CameraView + JogBox + 검사별 조명, 우측 결과 테이블 + Inspect + PASS/FAIL.
    /// Stage 94 — Designer/Code 분리. 정적 shell 은 .Designer.cs, 그랩/검사/결과 로직·런타임 자식패널은 Code.
    /// </summary>
    public partial class InspectorPage : PageBase
    {
        private readonly IVisionModule _module;
        private readonly IInspector   _inspector;

        /// <summary>디자이너용 파라미터 없는 생성자.</summary>
        public InspectorPage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildChildPanels();
        }

        public InspectorPage(IVisionModule module, IInspector inspector)
        {
            _module = module; _inspector = inspector;
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildChildPanels();
            Text = module.Name + " / " + inspector.Id;
        }

        /// <summary>런타임 의존 자식 패널(주입 _module/_inspector 기반) — Designer 직렬화 불가라 Code 유지.</summary>
        private void BuildChildPanels()
        {
            var illum = new InspectionLightPanel { Location = new Point(6, 544), Size = new Size(440, 280) };
            illum.SelectInspection(LightNode(), _module?.AlgorithmKey ?? "", _inspector?.Id ?? "");   // C2 — 조명 SSOT=노드
            Controls.Add(illum);
            // (LightLiveTuningPanel 은 제거 — 레벨+점등은 InspectionLightPanel, 실물 확인은 Settings 라이브.)
        }

        // ── 이벤트 핸들러 (Designer 에서 named 연결) ──
        private void OnGrabClick(object sender, EventArgs e) => DoGrab();
        private void OnLoadClick(object sender, EventArgs e) => DoLoad();
        private void OnSaveClick(object sender, EventArgs e) => DoSave();
        private void OnInspectClick(object sender, EventArgs e) => DoInspect();
        private void OnEditRoiClick(object sender, EventArgs e) => BeginEditRoi();

        private void OnCamRoiEdited(string which, Roi roi)
        {
            if (_inspector == null) return;
            _inspector.InspectionRoi = roi;
            Status($"INSPECTION ROI updated: x={roi.CenterX:F0} y={roi.CenterY:F0} w={roi.Width:F0} h={roi.Height:F0}");
            _cam.SetOverlay(_inspector.InspectionRoi, null);
        }

        // ── Stage 87 — 카메라 라이브 grab loop ──
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

        /// <summary>C2 — 현재 inspector 의 검사 노드 해석(조명 패널 주입용).</summary>
        private IAlgorithmNode LightNode() => _module?.Algorithms.FirstOrDefault(a => a.Inspector == _inspector);

        private GrabResult _lastGrab;
        private Bitmap    _loadedImage;
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
                _cam.SetOverlay(_inspector?.InspectionRoi, null);
                Status($"GRAB OK — {_lastGrab.Width}x{_lastGrab.Height} frame={_lastGrab.FrameNumber}");
            }
            else Status("GRAB FAIL: " + _lastGrab.ErrorMessage);
        }

        private void DoLoad()
        {
            using (var dlg = new OpenFileDialog
            {
                Title  = "Load image for inspection",
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

        private void DoSave()
        {
            var img = CurrentImage;
            if (img == null) { Status("SAVE: no image (do GRAB or LOAD first)"); return; }
            using (var dlg = new SaveFileDialog
            {
                Title    = "Save current image",
                Filter   = "PNG|*.png|BMP|*.bmp|JPEG|*.jpg",
                FileName = $"{(_module?.Name ?? "img")}_{(_inspector?.Id ?? "x").Replace('/','_')}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
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
