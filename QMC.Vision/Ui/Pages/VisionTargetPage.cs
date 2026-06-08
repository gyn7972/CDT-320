using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// R2 프로토타입 — Handler VisionRecipePage 미러(3열 TLP): 좌 카메라+매치결과 / 중 ROI 라디오+액션3×3 /
    /// 우 ParameterGridControl(R1)+JOG+SPEED. 값 편집은 R1 NumericKeypadDialog(ParameterGridControl 내장).
    /// 기능(Grab/Match/Train/Load/Save/EditROI)은 FinderPage 와 동일 로직 — 동작 보존.
    /// </summary>
    public partial class VisionTargetPage : UserControl
    {
        private readonly VisionModule _module;
        private readonly IPatternFinder _finder;

        public VisionTargetPage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            WireCamera();
        }

        public VisionTargetPage(VisionModule module, IPatternFinder finder)
        {
            _module = module; _finder = finder;
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            WireCamera();
            BuildParams();
            if (_finder != null) _cam.SetOverlay(_finder.SearchRoi, null);
            Status((module?.Name ?? "?") + " / " + (finder?.Id ?? "?"));
        }

        private void WireCamera()
        {
            _cam.RoiEdited += OnCamRoiEdited;
        }

        // ── 파라미터(우측 ParameterGridControl) = finder ROI 바인딩 ──
        private void BuildParams()
        {
            if (_finder == null) return;
            var items = new List<ParameterGridItem>
            {
                ParameterGridItem.Double("Search X", "px", ParameterGridScope.Setup, () => _finder.SearchRoi.CenterX, v => { _finder.SearchRoi.CenterX = v; RefreshOverlay(); }),
                ParameterGridItem.Double("Search Y", "px", ParameterGridScope.Setup, () => _finder.SearchRoi.CenterY, v => { _finder.SearchRoi.CenterY = v; RefreshOverlay(); }),
                ParameterGridItem.Double("Search W", "px", ParameterGridScope.Setup, () => _finder.SearchRoi.Width,   v => { _finder.SearchRoi.Width = v;   RefreshOverlay(); }),
                ParameterGridItem.Double("Search H", "px", ParameterGridScope.Setup, () => _finder.SearchRoi.Height,  v => { _finder.SearchRoi.Height = v;  RefreshOverlay(); }),
                ParameterGridItem.Double("Train X", "px", ParameterGridScope.Recipe, () => _finder.TrainRoi.CenterX, v => { _finder.TrainRoi.CenterX = v; }),
                ParameterGridItem.Double("Train Y", "px", ParameterGridScope.Recipe, () => _finder.TrainRoi.CenterY, v => { _finder.TrainRoi.CenterY = v; }),
                ParameterGridItem.Double("Train W", "px", ParameterGridScope.Recipe, () => _finder.TrainRoi.Width,   v => { _finder.TrainRoi.Width = v;   }),
                ParameterGridItem.Double("Train H", "px", ParameterGridScope.Recipe, () => _finder.TrainRoi.Height,  v => { _finder.TrainRoi.Height = v;  }),
            };
            _params.SetItems(items);
        }

        private void RefreshOverlay()
        {
            if (_finder != null) _cam.SetOverlay(_finder.SearchRoi, null);
        }

        // ── 액션(중앙 3×3) — FinderPage 동일 로직 ──
        private GrabResult _lastGrab;
        private Bitmap _loadedImage;
        private Bitmap CurrentImage => _lastGrab?.Image ?? _loadedImage;

        private void OnGrabClick(object sender, EventArgs e) => DoGrab();
        private void OnMatchClick(object sender, EventArgs e) => DoMatch();
        private void OnTrainClick(object sender, EventArgs e) => DoTrain();
        private void OnLoadClick(object sender, EventArgs e) => DoLoad();
        private void OnSaveClick(object sender, EventArgs e) => DoSave();
        private void OnEditSearchClick(object sender, EventArgs e) => BeginEditRoi(true);
        private void OnEditTrainClick(object sender, EventArgs e) => BeginEditRoi(false);

        private void OnSpeedScroll(object sender, EventArgs e) => _lblSpeed.Text = _trkSpeed.Value + "%";

        private void OnCamRoiEdited(string which, Roi roi)
        {
            if (_finder == null) return;
            if (which == "Search") _finder.SearchRoi = roi;
            else if (which == "Train") _finder.TrainRoi = roi;
            Status($"{which} ROI updated: x={roi.CenterX:F0} y={roi.CenterY:F0} w={roi.Width:F0} h={roi.Height:F0}");
            _cam.SetOverlay(_finder.SearchRoi, null);
            _params.RefreshValues();
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

        private void DoSave()
        {
            var img = CurrentImage;
            if (img == null) { Status("SAVE: no image (do GRAB or LOAD first)"); return; }
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
            _cam.BeginRoiDrag(isSearch ? "Search" : "Train", isSearch ? _finder.SearchRoi : _finder.TrainRoi);
            Status($"Drag a rectangle on the image to set {(isSearch ? "SEARCH" : "TRAIN")} ROI…");
        }

        private void Status(string s) { if (_lblStatus != null) _lblStatus.Text = s; }
    }
}
