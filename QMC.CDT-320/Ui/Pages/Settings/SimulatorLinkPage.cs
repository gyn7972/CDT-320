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
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();

            Disposed += (s, e) => Unhook();
        }

        private Form1 Host => FindForm() as Form1;

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.simulator");
            lblHeader.Tag = "i18n:set.simulator";

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

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);

            grpLink.Margin = Padding.Empty;
            grpLink.Padding = new Padding(4, 14, 4, 4);
            grpLink.ForeColor = Color.Black;

            _tbHost.Margin = new Padding(2, 7, 2, 2);
            _tbPort.Margin = new Padding(2, 7, 2, 2);

            // 모던 플랫 버튼 — 공용 스타일러(ApplyActionControl)가 강제하던 회색 대신 적용. 스타일러 이후라 런타임에 확실히 반영되고, Designer에도 같은 색을 넣어 미리보기를 맞춘다.
            StyleModernButton(_btnConnect, Color.FromArgb(34, 139, 84), Color.FromArgb(46, 160, 98), Color.FromArgb(27, 115, 68));
        }

        // 모던 플랫 버튼: 테두리 없음 + hover/press 색. (Designer 프리뷰용으로 .Designer.cs에도 동일 색을 박아둠)
        private static void StyleModernButton(Button b, Color back, Color hover, Color down)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = down;
            b.BackColor = back;
            b.ForeColor = Color.White;
            b.UseVisualStyleBackColor = false;
            b.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(2);
            b.Cursor = Cursors.Hand;
        }

        private void SimulatorLinkPage_Load(object sender, EventArgs e)
        {
            Hook();
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
