using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - simulator TCP link.</summary>
    public partial class SimulatorLinkPage : PageBase
    {
        public SimulatorLinkPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            WireEvents();

            Load += (s, e) => Hook();
            Disposed += (s, e) => Unhook();
        }

        private Form1 Host => FindForm() as Form1;

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.simulator");
            lblHeader.Tag = "i18n:set.simulator";
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;

            grpLink.Text = Lang.T("set.simulator");
            grpLink.Tag = "i18n:set.simulator;level:Engineer";
            lblHost.Text = Lang.T("set.host");
            lblHost.Tag = "i18n:set.host";
            lblPort.Text = Lang.T("set.port");
            lblPort.Tag = "i18n:set.port";
            lblConnStatus.Text = Lang.T("set.connStatus");
            lblConnStatus.Tag = "i18n:set.connStatus";
            _btnConnect.Text = Lang.T("set.connect");
            _btnConnect.Tag = "i18n:set.connect;level:Engineer";
            _lblStatus.Text = Lang.T("set.disconnected");
            _lblStatus.Tag = "i18n:set.disconnected";
        }

        private void WireEvents()
        {
            _btnConnect.Click += BtnConnect_Click;
        }

        private void Hook()
        {
            if (Host == null) return;
            Host.Bridge.Log += OnBridgeLog;
            Host.Bridge.ConnectionChanged += OnConnChanged;
            OnConnChanged(Host.Bridge.IsConnected);
        }

        private void Unhook()
        {
            if (Host == null) return;
            Host.Bridge.Log -= OnBridgeLog;
            Host.Bridge.ConnectionChanged -= OnConnChanged;
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            if (Host == null) return;
            if (Host.Bridge.IsConnected)
            {
                Host.Bridge.Disconnect();
                return;
            }

            try
            {
                _btnConnect.Enabled = false;
                int port = int.TryParse(_tbPort.Text, out var parsed) ? parsed : 7001;
                await Host.Bridge.ConnectAsync(_tbHost.Text.Trim(), port);
            }
            catch (Exception ex)
            {
                AppendLog("[ERROR] " + ex.Message);
            }
            finally
            {
                _btnConnect.Enabled = true;
            }
        }

        private void OnConnChanged(bool on)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(OnConnChanged), on);
                return;
            }

            _lblStatus.Tag = on ? "i18n:set.connected" : "i18n:set.disconnected";
            _lblStatus.Text = Lang.T(on ? "set.connected" : "set.disconnected");
            _lblStatus.ForeColor = on ? Color.SeaGreen : Color.IndianRed;
            _btnConnect.Tag = (on ? "i18n:set.disconnect" : "i18n:set.connect") + ";level:Engineer";
            _btnConnect.Text = Lang.T(on ? "set.disconnect" : "set.connect");
        }

        private void OnBridgeLog(string msg) => AppendLog(msg);

        private void AppendLog(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), msg);
                return;
            }

            _txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + msg + Environment.NewLine);
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.ScrollToCaret();
        }
    }
}
