using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Remote;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Remote Viewer 제어 다이얼로그.
    /// - 포트 입력 + Start/Stop
    /// - 자체 미리보기 (Form1 화면을 그대로 캡처해서 PictureBox 에 표시)
    /// </summary>
    public class RemoteViewerDialog : Form
    {
        private readonly Form1 _host;
        private RemoteViewer _viewer;
        private NumericUpDown _nPort;
        private Button _btnStart, _btnStop, _btnClose;
        private Label _lblStatus, _lblClients;
        private PictureBox _preview;
        private System.Windows.Forms.Timer _previewTimer;

        public RemoteViewerDialog(Form1 host)
        {
            _host = host;
            Text = "Remote Viewer";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            ClientSize = new Size(680, 600);
            BackColor = UiTheme.MainBg;
            ShowIcon = false;

            BuildLayout();
        }

        private void BuildLayout()
        {
            var title = new Label
            {
                Dock = DockStyle.Top, Height = 36, Text = "REMOTE VIEWER",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = new Font("맑은 고딕", 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(title);

            var ctl = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UiTheme.OptionPanelBg };
            ctl.Controls.Add(new Label { Location = new Point(10, 18), AutoSize = true, Text = "Port:", Font = UiTheme.ButtonFont });
            _nPort = new NumericUpDown
            {
                Location = new Point(56, 14), Size = new Size(100, 28),
                Minimum = 1024, Maximum = 65535, Value = 5099, Font = UiTheme.ValueFont
            };
            ctl.Controls.Add(_nPort);
            _btnStart = new Button { Location = new Point(170, 12), Size = new Size(100, 32), Text = "Start", FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Accent, ForeColor = Color.White, Font = UiTheme.ButtonFont };
            _btnStop  = new Button { Location = new Point(280, 12), Size = new Size(100, 32), Text = "Stop",  FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = UiTheme.ButtonFont, Enabled = false };
            _btnStart.Click += (s, e) => OnStart();
            _btnStop .Click += (s, e) => OnStop();
            ctl.Controls.Add(_btnStart); ctl.Controls.Add(_btnStop);
            _lblStatus = new Label
            {
                Location = new Point(390, 18), Size = new Size(280, 24),
                Text = "stopped", Font = UiTheme.ValueFont, ForeColor = Color.DimGray
            };
            ctl.Controls.Add(_lblStatus);
            Controls.Add(ctl);

            _lblClients = new Label
            {
                Dock = DockStyle.Top, Height = 28, Text = "Connected viewers: 0",
                BackColor = Color.WhiteSmoke, Font = UiTheme.ValueFont,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(_lblClients);

            _preview = new PictureBox
            {
                Dock = DockStyle.Fill, BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            Controls.Add(_preview);

            var btnBar = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = UiTheme.MainBg };
            _btnClose = new Button { Location = new Point(560, 6), Size = new Size(100, 32), Text = "Close", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont, BackColor = Color.White, DialogResult = DialogResult.OK };
            btnBar.Controls.Add(_btnClose);
            Controls.Add(btnBar);

            _previewTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _previewTimer.Tick += (s, e) => UpdatePreview();
            _previewTimer.Start();
        }

        private void OnStart()
        {
            try
            {
                int port = (int)_nPort.Value;
                _viewer = new RemoteViewer(_host, port);
                _viewer.IntervalMs = 1000;
                _viewer.Start();
                _btnStart.Enabled = false; _btnStop.Enabled = true;
                _lblStatus.Text = $"listening on {port}";
                _lblStatus.ForeColor = Color.SeaGreen;
            }
            catch (Exception ex) { MessageBox.Show("Start fail: " + ex.Message); }
        }

        private void OnStop()
        {
            try { _viewer?.Stop(); _viewer?.Dispose(); } catch { }
            _viewer = null;
            _btnStart.Enabled = true; _btnStop.Enabled = false;
            _lblStatus.Text = "stopped";
            _lblStatus.ForeColor = Color.DimGray;
        }

        private void UpdatePreview()
        {
            try
            {
                if (_host == null || _host.IsDisposed) return;
                using (var bmp = new Bitmap(_host.Width, _host.Height))
                {
                    _host.DrawToBitmap(bmp, new Rectangle(0, 0, _host.Width, _host.Height));
                    var clone = new Bitmap(bmp);
                    _preview.Image?.Dispose();
                    _preview.Image = clone;
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _previewTimer?.Stop();
            try { _viewer?.Stop(); _viewer?.Dispose(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
