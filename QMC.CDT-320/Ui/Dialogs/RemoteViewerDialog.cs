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
            ApplyDialogStyle();
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
                SetStatus($"listening on {port}", true);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Start fail: " + ex.Message, "Remote Viewer");
            }
        }

        private void OnStop()
        {
            try { _viewer?.Stop(); _viewer?.Dispose(); } catch { }
            _viewer = null;
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
            SetStatus("stopped", false);
        }

        private void ApplyDialogStyle()
        {
            Text = "원격 뷰어";
            ClientSize = new Size(760, 620);
            BackColor = Color.White;

            rootLayout.Margin = Padding.Empty;
            rootLayout.Padding = Padding.Empty;
            rootLayout.BackColor = Color.White;
            if (rootLayout.RowStyles.Count >= 5)
            {
                rootLayout.RowStyles[0].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[0].Height = 68F;
                rootLayout.RowStyles[1].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[1].Height = 62F;
                rootLayout.RowStyles[2].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[2].Height = 30F;
                rootLayout.RowStyles[4].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[4].Height = 56F;
            }

            lblTitle.Text = "원격 뷰어";
            lblTitle.Margin = Padding.Empty;
            lblTitle.Padding = new Padding(20, 0, 20, 0);
            lblTitle.BackColor = Color.FromArgb(38, 50, 66);
            lblTitle.ForeColor = Color.White;
            lblTitle.Font = new Font("Malgun Gothic", 20F, FontStyle.Bold);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;

            controlLayout.Margin = new Padding(12, 10, 12, 6);
            controlLayout.Padding = new Padding(12, 10, 12, 10);
            controlLayout.BackColor = Color.White;
            controlLayout.ColumnStyles.Clear();
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));

            lblPort.Text = "PORT";
            lblPort.Margin = Padding.Empty;
            lblPort.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            lblPort.ForeColor = Color.FromArgb(57, 65, 75);
            _nPort.Margin = new Padding(0, 2, 8, 2);
            _nPort.Font = new Font("Consolas", 10F, FontStyle.Regular);

            StyleActionButton(_btnStart, true);
            StyleActionButton(_btnStop, false);

            _lblStatus.Margin = Padding.Empty;
            _lblStatus.Padding = new Padding(12, 0, 12, 0);
            _lblStatus.Font = new Font("Consolas", 10F, FontStyle.Bold);
            SetStatus("stopped", false);

            _lblClients.Margin = new Padding(12, 0, 12, 6);
            _lblClients.Padding = new Padding(12, 0, 12, 0);
            _lblClients.BackColor = Color.FromArgb(248, 250, 252);
            _lblClients.ForeColor = Color.FromArgb(57, 65, 75);
            _lblClients.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            _lblClients.Text = "CONNECTED VIEWERS : 0";

            _preview.Margin = new Padding(12, 0, 12, 8);
            _preview.BackColor = Color.FromArgb(18, 22, 28);
            _preview.BorderStyle = BorderStyle.FixedSingle;

            bottomLayout.Margin = Padding.Empty;
            bottomLayout.Padding = new Padding(12, 6, 12, 12);
            bottomLayout.BackColor = Color.White;
            bottomLayout.ColumnStyles.Clear();
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            StyleActionButton(_btnClose, false);
        }

        private void SetStatus(string text, bool active)
        {
            _lblStatus.Text = text;
            _lblStatus.ForeColor = active ? Color.FromArgb(30, 113, 75) : Color.FromArgb(92, 101, 114);
            _lblStatus.BackColor = active ? Color.FromArgb(226, 244, 233) : Color.FromArgb(242, 244, 247);
        }

        private static void StyleActionButton(Button button, bool primary)
        {
            if (button == null)
                return;

            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(4, 0, 4, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.UseVisualStyleBackColor = false;
            button.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            button.ForeColor = Color.White;
            button.BackColor = primary ? Color.FromArgb(72, 94, 130) : Color.FromArgb(128, 128, 128);
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

