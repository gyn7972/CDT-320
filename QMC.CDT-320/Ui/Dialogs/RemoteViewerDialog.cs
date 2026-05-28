using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Remote;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class RemoteViewerDialog : Form
    {
        private readonly Form1 _host;
        private RemoteViewer _viewer;

        public RemoteViewerDialog(Form1 host)
        {
            _host = host;
            InitializeComponent();
            WireEvents();
            StartPreviewTimer();
        }

        private void WireEvents()
        {
            _btnStart.Click += (s, e) => OnStart();
            _btnStop.Click += (s, e) => OnStop();
        }

        private void StartPreviewTimer()
        {
            _previewTimer.Interval = 1000;
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
                _btnStart.Enabled = false;
                _btnStop.Enabled = true;
                _lblStatus.Text = $"listening on {port}";
                _lblStatus.ForeColor = Color.SeaGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Start fail: " + ex.Message, "Remote Viewer");
            }
        }

        private void OnStop()
        {
            try { _viewer?.Stop(); _viewer?.Dispose(); } catch { }
            _viewer = null;
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
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
