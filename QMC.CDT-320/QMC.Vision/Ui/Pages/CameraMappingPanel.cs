using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Config;
using QMC.Vision.Core;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 알고리즘 1개의 카메라 매핑 + 카메라 파라미터 편집 패널.
    /// SettingsPage 의 우측 디테일로 호스팅됨. <see cref="SelectAlgorithm"/> 으로 표시 알고리즘 변경.
    /// </summary>
    public class CameraMappingPanel : UserControl
    {
        private Label    _lblAlgorithm;
        private ComboBox _cbCameraId;
        private Button   _btnDiscover;
        private NumericUpDown _numExposure, _numGain, _numFps, _numDelay;
        private ComboBox _cbTrigger, _cbPixel;
        private Button   _btnSave, _btnApply, _btnTestGrab;
        private Label    _lblStatus;
        private PictureBox _picPreview;

        private string _algorithm;
        private bool   _suspendBinding;

        public CameraMappingPanel()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            BuildLayout();
        }

        public void SelectAlgorithm(string algorithm)
        {
            _algorithm = algorithm;
            _lblAlgorithm.Text = "카메라 매핑 — " + VisionAlgorithm.Label(algorithm) + "  (" + algorithm + ")";
            BindFields();
        }

        private void BuildLayout()
        {
            BackColor = UiTheme.MainBg;

            _lblAlgorithm = new Label
            {
                Dock = DockStyle.Top, Height = 30, Text = "카메라 매핑",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(_lblAlgorithm);

            var body = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.MainBg, AutoScroll = true, Padding = new Padding(10) };
            Controls.Add(body);

            int x1 = 20, x2 = 180, y = 10, dy = 36;

            body.Controls.Add(L("카메라 ID", x1, y));
            _cbCameraId = new ComboBox { Location = new Point(x2, y - 2), Size = new Size(360, 26), Font = UiTheme.ValueFont };
            _cbCameraId.SelectedIndexChanged += (s, e) => OnFieldChanged();
            _cbCameraId.TextChanged          += (s, e) => OnFieldChanged();
            body.Controls.Add(_cbCameraId);
            _btnDiscover = new Button
            {
                Location = new Point(x2 + 370, y - 2), Size = new Size(120, 28),
                Text = "카메라 검색", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White
            };
            _btnDiscover.Click += (s, e) => DiscoverCameras();
            body.Controls.Add(_btnDiscover);

            y += dy;
            body.Controls.Add(L("Exposure (μs)", x1, y));
            _numExposure = MakeNum(x2, y, 1, 1_000_000, 0, 100); body.Controls.Add(_numExposure);
            _numExposure.ValueChanged += (s, e) => OnFieldChanged();

            y += dy;
            body.Controls.Add(L("Gain (dB)", x1, y));
            _numGain = MakeNum(x2, y, 0, 48, 1, 0.5M); body.Controls.Add(_numGain);
            _numGain.ValueChanged += (s, e) => OnFieldChanged();

            y += dy;
            body.Controls.Add(L("Frame rate (fps)", x1, y));
            _numFps = MakeNum(x2, y, 1, 1000, 0, 1); body.Controls.Add(_numFps);
            _numFps.ValueChanged += (s, e) => OnFieldChanged();

            y += dy;
            body.Controls.Add(L("Trigger mode", x1, y));
            _cbTrigger = new ComboBox { Location = new Point(x2, y - 2), Size = new Size(150, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont };
            foreach (var v in Enum.GetNames(typeof(CameraTriggerMode))) _cbTrigger.Items.Add(v);
            _cbTrigger.SelectedIndexChanged += (s, e) => OnFieldChanged();
            body.Controls.Add(_cbTrigger);

            y += dy;
            body.Controls.Add(L("Pixel format", x1, y));
            _cbPixel = new ComboBox { Location = new Point(x2, y - 2), Size = new Size(150, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont };
            foreach (var v in Enum.GetNames(typeof(CameraPixelFormat))) _cbPixel.Items.Add(v);
            _cbPixel.SelectedIndexChanged += (s, e) => OnFieldChanged();
            body.Controls.Add(_cbPixel);

            y += dy;
            body.Controls.Add(L("Delay before grab (ms)", x1, y));
            _numDelay = MakeNum(x2, y, 0, 60_000, 0, 10); body.Controls.Add(_numDelay);
            _numDelay.ValueChanged += (s, e) => OnFieldChanged();

            // 액션
            y += dy + 12;
            _btnSave = new Button { Location = new Point(x1, y), Size = new Size(140, 32), Text = "저장", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = UiTheme.Accent, ForeColor = Color.White };
            _btnSave.Click += (s, e) => SaveAll();
            body.Controls.Add(_btnSave);

            _btnApply = new Button { Location = new Point(x1 + 150, y), Size = new Size(180, 32), Text = "실행 모듈에 적용", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };
            _btnApply.Click += (s, e) => ApplyToRunningModule();
            body.Controls.Add(_btnApply);

            _btnTestGrab = new Button { Location = new Point(x1 + 340, y), Size = new Size(140, 32), Text = "테스트 그랩", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White };
            _btnTestGrab.Click += (s, e) => TestGrab();
            body.Controls.Add(_btnTestGrab);

            _lblStatus = new Label { Location = new Point(x1, y + 40), Size = new Size(900, 24), Text = "", Font = UiTheme.ValueFont, ForeColor = Color.DarkSlateGray };
            body.Controls.Add(_lblStatus);

            _picPreview = new PictureBox { Location = new Point(x1, y + 70), Size = new Size(640, 480), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.Black, SizeMode = PictureBoxSizeMode.Zoom };
            body.Controls.Add(_picPreview);
        }

        private static Label L(string text, int x, int y)
            => new Label { Location = new Point(x, y), Size = new Size(160, 22), Text = text, Font = UiTheme.ButtonFont };

        private static NumericUpDown MakeNum(int x, int y, decimal min, decimal max, int dp, decimal inc)
            => new NumericUpDown
            {
                Location = new Point(x, y - 2), Size = new Size(150, 26),
                Minimum = min, Maximum = max, DecimalPlaces = dp, Increment = inc,
                Font = UiTheme.ValueFont
            };

        private AlgorithmCameraMapping CurrentMapping()
        {
            if (string.IsNullOrEmpty(_algorithm)) return null;
            var m = AlgorithmCameraMapStore.Current.Get(_algorithm);
            if (m == null)
            {
                m = new AlgorithmCameraMapping { Algorithm = _algorithm };
                AlgorithmCameraMapStore.Current.Items.Add(m);
            }
            return m;
        }

        private void BindFields()
        {
            var m = CurrentMapping();
            if (m == null) return;
            _suspendBinding = true;
            try
            {
                if (!_cbCameraId.Items.Contains(m.CameraId)) _cbCameraId.Items.Add(m.CameraId);
                _cbCameraId.SelectedItem = m.CameraId;
                _cbCameraId.Text         = m.CameraId;
                _numExposure.Value = Clamp((decimal)m.ExposureUs, _numExposure.Minimum, _numExposure.Maximum);
                _numGain    .Value = Clamp((decimal)m.Gain,       _numGain.Minimum,     _numGain.Maximum);
                _numFps     .Value = Clamp((decimal)m.FrameRate,  _numFps.Minimum,      _numFps.Maximum);
                _cbTrigger  .SelectedItem = m.TriggerMode ?? "Software"; if (_cbTrigger.SelectedIndex < 0) _cbTrigger.SelectedIndex = (int)CameraTriggerMode.Software;
                _cbPixel    .SelectedItem = m.PixelFormat ?? "Mono8";    if (_cbPixel.SelectedIndex   < 0) _cbPixel.SelectedIndex   = (int)CameraPixelFormat.Mono8;
                _numDelay   .Value = Clamp((decimal)m.DelayBeforeGrabMs, _numDelay.Minimum, _numDelay.Maximum);
            }
            finally { _suspendBinding = false; }
        }

        private static decimal Clamp(decimal v, decimal min, decimal max)
            => v < min ? min : (v > max ? max : v);

        private void OnFieldChanged()
        {
            if (_suspendBinding) return;
            var m = CurrentMapping();
            if (m == null) return;
            m.CameraId          = (_cbCameraId.SelectedItem as string) ?? _cbCameraId.Text;
            m.ExposureUs        = (double)_numExposure.Value;
            m.Gain              = (double)_numGain.Value;
            m.FrameRate         = (double)_numFps.Value;
            m.TriggerMode       = (string)_cbTrigger.SelectedItem ?? "Software";
            m.PixelFormat       = (string)_cbPixel.SelectedItem   ?? "Mono8";
            m.DelayBeforeGrabMs = (int)_numDelay.Value;
        }

        private void DiscoverCameras()
        {
            try
            {
                _lblStatus.Text = "카메라 검색 중..."; _lblStatus.Refresh();
                var list = CameraFactory.EnumerateAll();
                _cbCameraId.Items.Clear();
                foreach (var info in list)
                    if (!_cbCameraId.Items.Contains(info.Id)) _cbCameraId.Items.Add(info.Id);
                foreach (var fallback in new[] { "Sim/Wafer", "Sim/Bin", "Sim/BottomInsp", "Sim/TopSide", "Sim/BottomSide", "Sim/0" })
                    if (!_cbCameraId.Items.Contains(fallback)) _cbCameraId.Items.Add(fallback);

                var m = CurrentMapping();
                if (m != null && !string.IsNullOrEmpty(m.CameraId))
                {
                    if (!_cbCameraId.Items.Contains(m.CameraId)) _cbCameraId.Items.Add(m.CameraId);
                    _cbCameraId.SelectedItem = m.CameraId;
                }
                _lblStatus.Text = $"검색 완료 — {list.Count} 대 발견";
            }
            catch (Exception ex) { _lblStatus.Text = "검색 실패: " + ex.Message; }
        }

        private void SaveAll()
        {
            OnFieldChanged();
            AlgorithmCameraMapStore.Save();
            _lblStatus.Text = "저장 완료 — " + AlgorithmCameraMapStore.Path_;
        }

        private void ApplyToRunningModule()
        {
            OnFieldChanged();
            var m = CurrentMapping();
            var form = this.FindForm() as Form1;
            if (form == null || m == null) { _lblStatus.Text = "메인 폼을 찾을 수 없음"; return; }
            try
            {
                form.RebindAlgorithmCamera(_algorithm, m);
                _lblStatus.Text = $"[{VisionAlgorithm.Label(_algorithm)}] 실행 모듈에 적용됨";
            }
            catch (Exception ex) { _lblStatus.Text = "적용 실패: " + ex.Message; }
        }

        private void TestGrab()
        {
            OnFieldChanged();
            var m = CurrentMapping();
            if (m == null) return;
            _lblStatus.Text = "테스트 그랩 중..."; _lblStatus.Refresh();
            ICamera cam = null;
            try
            {
                cam = AlgorithmCameraBinder.CreateAndApply(m);
                using (var g = cam.Grab(3000))
                {
                    if (g.IsSuccess && g.Image != null)
                    {
                        _picPreview.Image?.Dispose();
                        _picPreview.Image = new Bitmap(g.Image);
                        _lblStatus.Text = $"그랩 OK — {g.Width}x{g.Height}  Exposure={m.ExposureUs}μs  Gain={m.Gain}dB";
                    }
                    else _lblStatus.Text = "그랩 실패: " + (g.ErrorMessage ?? "-");
                }
            }
            catch (Exception ex) { _lblStatus.Text = "예외: " + ex.Message; }
            finally { try { cam?.Dispose(); } catch { } }
        }
    }
}
