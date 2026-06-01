using System;
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
    /// Finder 공용 페이지 — 매뉴얼의 "EjectPin finder/Reticle finder/AlignDie finder/..." 에 대응.
    /// 좌측: CameraView + JogBox + Illuminator
    /// 우측: 결과 테이블 + Train/Match 버튼
    /// </summary>
    public class FinderPage : UserControl
    {
        private readonly VisionModule _module;
        private readonly IPatternFinder _finder;
        private CameraView _cam;
        private DataGridView _result;
        private Button _btnGrab, _btnLoad, _btnSave, _btnTrain, _btnMatch;
        private Button _btnEditSearch, _btnEditTrain;
        private Label  _lblStatus;

        /// <summary>디자이너용 파라미터 없는 생성자 — 실행 시 Initialize() 호출.</summary>
        public FinderPage() { BuildLayout(); }

        public FinderPage(VisionModule module, IPatternFinder finder)
        {
            _module = module; _finder = finder;

            BuildLayout();
            Text = module.Name + " / " + finder.Id;
        }

        private void BuildLayout()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            var title = new Label
            {
                Dock = DockStyle.Top, Height = 26,
                Text = Text, BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(title);

            _cam = new CameraView { Location = new Point(6, 34), Size = new Size(700, 500) };
            Controls.Add(_cam);

            // Stage 70 E — 더미 IlluminatorPanel → 검사별 InspectionLightPanel.
            // Stage 72 — 겹침/잘림 해소: 카메라 높이 500 으로 소폭 축소 → 밴드 y544 상향.
            //   illum x:6~446 / jog x:456~716 (10px 간격, 우측 컨트롤 x720~ 과 분리)
            //   illum,jog 하단 824 ≤ Recipe 콘텐츠 높이(≈832) — JogBox 280 컴팩트화로 내부 버튼도 안 잘림.
            var illum = new InspectionLightPanel(_module?.AlgorithmKey ?? "", _finder?.Id ?? "")
            { Location = new Point(6, 544), Size = new Size(440, 280) };
            var jog   = new JogBox { Location = new Point(456, 544), Size = new Size(260, 280) };
            Controls.Add(illum);
            Controls.Add(jog);

            // 결과 테이블
            _result = new DataGridView
            {
                Location = new Point(720, 34), Size = new Size(540, 320),
                ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, BackgroundColor = Color.White,
                Font = UiTheme.ValueFont,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50,0x50,0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                }
            };
            foreach (var c in new[] { "Index", "X", "Y", "R", "Score" })
                _result.Columns.Add(c, c);
            Controls.Add(_result);

            // 1행: GRAB / LOAD / SAVE
            _btnGrab  = new Button { Location = new Point(720, 364), Size = new Size(170, 44), Text = "GRAB",  FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };
            _btnLoad  = new Button { Location = new Point(900, 364), Size = new Size(170, 44), Text = "LOAD",  FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };
            _btnSave  = new Button { Location = new Point(1080,364), Size = new Size(180, 44), Text = "SAVE",  FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };

            // 2행: TRAIN / MATCH
            _btnTrain = new Button { Location = new Point(720, 416), Size = new Size(265, 50), Text = "TRAIN", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = UiTheme.Accent, ForeColor = Color.White };
            _btnMatch = new Button { Location = new Point(995, 416), Size = new Size(265, 50), Text = "MATCH", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = UiTheme.Accent, ForeColor = Color.White };

            // 3행: ROI 편집
            _btnEditSearch = new Button { Location = new Point(720, 478), Size = new Size(265, 36), Text = "Edit SEARCH ROI (drag)", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.LightYellow };
            _btnEditTrain  = new Button { Location = new Point(995, 478), Size = new Size(265, 36), Text = "Edit TRAIN ROI (drag)",  FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.LightYellow };

            // 상태 라벨
            _lblStatus = new Label
            {
                Location = new Point(720, 524), Size = new Size(540, 30),
                Text = "Ready.", Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };

            _btnGrab .Click += (s,e) => DoGrab();
            _btnLoad .Click += (s,e) => DoLoad();
            _btnSave .Click += (s,e) => DoSave();
            _btnTrain.Click += (s,e) => DoTrain();
            _btnMatch.Click += (s,e) => DoMatch();
            _btnEditSearch.Click += (s,e) => BeginEditRoi(true);
            _btnEditTrain .Click += (s,e) => BeginEditRoi(false);

            Controls.Add(_btnGrab); Controls.Add(_btnLoad); Controls.Add(_btnSave);
            Controls.Add(_btnTrain); Controls.Add(_btnMatch);
            Controls.Add(_btnEditSearch); Controls.Add(_btnEditTrain);
            Controls.Add(_lblStatus);

            // CameraView ROI 변경 콜백 — 편집 후 finder 에 반영
            _cam.RoiEdited += (which, roi) =>
            {
                if (_finder == null) return;
                if (which == "Search") _finder.SearchRoi = roi;
                else if (which == "Train") _finder.TrainRoi = roi;
                Status($"{which} ROI updated: x={roi.CenterX:F0} y={roi.CenterY:F0} w={roi.Width:F0} h={roi.Height:F0}");
                _cam.SetOverlay(_finder.SearchRoi, null);
            };
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
