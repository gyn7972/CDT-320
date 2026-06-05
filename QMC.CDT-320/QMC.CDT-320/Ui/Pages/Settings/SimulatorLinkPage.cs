using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>시뮬레이터(WPF 3D 뷰어) TCP 연결 제어 페이지.</summary>
    public class SimulatorLinkPage : PageBase
    {
        private TextBox     _tbHost;
        private TextBox     _tbPort;
        private Button      _btnConnect;
        private Label       _lblStatus;
        private RichTextBox _txtLog;

        public SimulatorLinkPage()
        {
            Controls.Add(CreateSectionHeader("set.simulator"));
            BuildBody();
            Load += (s, e) => Hook();
            Disposed += (s, e) => Unhook();
        }

        private Form1 Host => FindForm() as Form1;

        private void BuildBody()
        {
            var grp = new GroupBox
            {
                Location = new Point(30, 50),
                Size     = new Size(700, 280),
                Text     = Lang.T("set.simulator"),
                Tag      = "i18n:set.simulator;level:Engineer",
                Font     = UiTheme.SectionFont,
                BackColor = UiTheme.MainBg
            };

            grp.Controls.Add(new Label { Location = new Point(16, 34), AutoSize = true, Text = Lang.T("set.host"), Tag = "i18n:set.host", Font = UiTheme.ButtonFont });
            _tbHost = new TextBox { Location = new Point(80, 30), Size = new Size(160, 26), Text = "127.0.0.1", Font = UiTheme.ButtonFont };
            grp.Controls.Add(_tbHost);

            grp.Controls.Add(new Label { Location = new Point(260, 34), AutoSize = true, Text = Lang.T("set.port"), Tag = "i18n:set.port", Font = UiTheme.ButtonFont });
            _tbPort = new TextBox { Location = new Point(310, 30), Size = new Size(80, 26), Text = "7001", Font = UiTheme.ButtonFont };
            grp.Controls.Add(_tbPort);

            _btnConnect = new Button
            {
                Location  = new Point(410, 28),
                Size      = new Size(150, 32),
                Text      = Lang.T("set.connect"),
                Tag       = "i18n:set.connect;level:Engineer",
                FlatStyle = FlatStyle.Flat,
                Font      = UiTheme.ButtonFont
            };
            _btnConnect.Click += BtnConnect_Click;
            grp.Controls.Add(_btnConnect);

            grp.Controls.Add(new Label { Location = new Point(16, 72), AutoSize = true, Text = Lang.T("set.connStatus"), Tag = "i18n:set.connStatus", Font = UiTheme.ButtonFont });
            _lblStatus = new Label
            {
                Location = new Point(80, 72), Size = new Size(400, 22),
                Text = Lang.T("set.disconnected"), Tag = "i18n:set.disconnected",
                ForeColor = Color.IndianRed, Font = UiTheme.ButtonFont
            };
            grp.Controls.Add(_lblStatus);

            _txtLog = new RichTextBox
            {
                Location = new Point(16, 104), Size = new Size(660, 160),
                ReadOnly = true, BackColor = Color.Black, ForeColor = Color.LightGray,
                Font = new Font("Consolas", 9F), WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            grp.Controls.Add(_txtLog);

            Controls.Add(grp);
            Controls.SetChildIndex(grp, 0);
        }

        private void Hook()
        {
            if (Host == null) return;
            Host.Bridge.Log               += OnBridgeLog;
            Host.Bridge.ConnectionChanged += OnConnChanged;
            OnConnChanged(Host.Bridge.IsConnected);
        }

        private void Unhook()
        {
            if (Host == null) return;
            Host.Bridge.Log               -= OnBridgeLog;
            Host.Bridge.ConnectionChanged -= OnConnChanged;
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            if (Host == null) return;
            if (Host.Bridge.IsConnected) { Host.Bridge.Disconnect(); return; }
            try
            {
                _btnConnect.Enabled = false;
                int port = int.TryParse(_tbPort.Text, out var p) ? p : 7001;
                await Host.Bridge.ConnectAsync(_tbHost.Text.Trim(), port);
            }
            catch (Exception ex) { AppendLog("[ERROR] " + ex.Message); }
            finally { _btnConnect.Enabled = true; }
        }

        private void OnConnChanged(bool on)
        {
            if (InvokeRequired) { BeginInvoke(new Action<bool>(OnConnChanged), on); return; }
            _lblStatus.Tag       = on ? "i18n:set.connected" : "i18n:set.disconnected";
            _lblStatus.Text      = Lang.T(on ? "set.connected" : "set.disconnected");
            _lblStatus.ForeColor = on ? Color.SeaGreen : Color.IndianRed;
            _btnConnect.Tag      = (on ? "i18n:set.disconnect" : "i18n:set.connect") + ";level:Engineer";
            _btnConnect.Text     = Lang.T(on ? "set.disconnect" : "set.connect");
        }

        private void OnBridgeLog(string msg) => AppendLog(msg);

        private void AppendLog(string msg)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(AppendLog), msg); return; }
            _txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + msg + Environment.NewLine);
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.ScrollToCaret();
        }
    }
}
