using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using QMC.Common.Recipes;
using QMC.Vision.Config;
using QMC.Vision.Core;
using QMC.Vision.Modules;
using QMC.Vision.Ui.Controls;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// Inspector 공용 페이지 — SurfaceInspector / PlacementInspector / FocusInspector 등.
    /// 좌측: CameraView + JogBox + Illuminator
    /// 우측: 결과 키-값 테이블 + Grab/Load/Save/Inspect 버튼 + ROI 편집 + PASS/FAIL 표시
    /// </summary>
    public class InspectorPage : UserControl
    {
        private readonly VisionModule _module;
        private readonly IInspector   _inspector;
        private CameraView _cam;
        private DataGridView _result;
        private Button _btnGrab, _btnLoad, _btnSave, _btnInspect, _btnEditRoi;
        private Label  _lblStatus, _lblVerdict;

        /// <summary>디자이너용 파라미터 없는 생성자.</summary>
        public InspectorPage() { BuildLayout(); }

        public InspectorPage(VisionModule module, IInspector inspector)
        {
            _module = module; _inspector = inspector;
            BuildLayout();
            Text = module.Name + " / " + inspector.Id;
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
            var illum = new InspectionLightPanel(_module?.AlgorithmKey ?? "", _inspector?.Id ?? "")
            { Location = new Point(6, 544), Size = new Size(440, 280) };
            var jog   = new JogBox { Location = new Point(456, 544), Size = new Size(260, 280) };
            Controls.Add(illum);
            Controls.Add(jog);

            // 결과 키-값 테이블
            _result = new DataGridView
            {
                Location = new Point(720, 34), Size = new Size(540, 270),
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
            foreach (var c in new[] { "Item", "Value", "Pass" })
                _result.Columns.Add(c, c);
            Controls.Add(_result);

            // PASS/FAIL 라벨
            _lblVerdict = new Label
            {
                Location = new Point(720, 312), Size = new Size(540, 50),
                Text = "—", Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.DimGray, BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(_lblVerdict);

            // 1행: GRAB / LOAD / SAVE
            _btnGrab = new Button { Location = new Point(720, 372), Size = new Size(170, 44), Text = "GRAB", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };
            _btnLoad = new Button { Location = new Point(900, 372), Size = new Size(170, 44), Text = "LOAD", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };
            _btnSave = new Button { Location = new Point(1080,372), Size = new Size(180, 44), Text = "SAVE", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };

            // 2행: INSPECT (큰 버튼)
            _btnInspect = new Button
            {
                Location = new Point(720, 422), Size = new Size(540, 56),
                Text = "INSPECT", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = UiTheme.Accent, ForeColor = Color.White
            };

            // 3행: ROI 편집
            _btnEditRoi = new Button
            {
                Location = new Point(720, 488), Size = new Size(540, 36),
                Text = "Edit INSPECTION ROI (drag)", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont,
                BackColor = Color.LightYellow
            };

            _lblStatus = new Label
            {
                Location = new Point(720, 532), Size = new Size(540, 30),
                Text = "Ready.", Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            };

            _btnGrab   .Click += (s,e) => DoGrab();
            _btnLoad   .Click += (s,e) => DoLoad();
            _btnSave   .Click += (s,e) => DoSave();
            _btnInspect.Click += (s,e) => DoInspect();
            _btnEditRoi.Click += (s,e) => BeginEditRoi();

            Controls.Add(_btnGrab); Controls.Add(_btnLoad); Controls.Add(_btnSave);
            Controls.Add(_btnInspect); Controls.Add(_btnEditRoi); Controls.Add(_lblStatus);

            // Stage 87 — 라이브 튜닝 패널 우측 빈 공간 (720, 566) 540×264. 카메라 라이브 + 조명 펄스 통합.
            var liveTuning = new LightLiveTuningPanel
            {
                Location = new Point(720, 566), Size = new Size(540, 264), Name = "_liveTuning"
            };
            liveTuning.Initialize(CollectRowsForLiveTuning);
            liveTuning.BindCameraLive(() => StartLive(0), () => StopLive());
            Controls.Add(liveTuning);

            _cam.RoiEdited += (which, roi) =>
            {
                if (_inspector == null) return;
                _inspector.InspectionRoi = roi;
                Status($"INSPECTION ROI updated: x={roi.CenterX:F0} y={roi.CenterY:F0} w={roi.Width:F0} h={roi.Height:F0}");
                _cam.SetOverlay(_inspector.InspectionRoi, null);
            };
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

        /// <summary>Stage 87 — 현재 검사(algorithm+id)의 저장된 조명 설정을 TuningRow 로 변환 (라이브 튜닝 송신 소스).</summary>
        private IEnumerable<LightLiveTuningPanel.TuningRow> CollectRowsForLiveTuning()
        {
            var ov = AlgorithmCameraMapStore.Current?.Get(_module?.AlgorithmKey)?.GetLightOverride(_inspector?.Id);
            if (ov?.Settings == null) yield break;
            foreach (var s in ov.Settings)
                if (!string.IsNullOrEmpty(s.ControllerPort) && s.Channel > 0)
                    yield return new LightLiveTuningPanel.TuningRow
                    { ControllerPort = s.ControllerPort, Channel = s.Channel, Level = s.Level };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { StopLive(); _liveTimer?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }

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
