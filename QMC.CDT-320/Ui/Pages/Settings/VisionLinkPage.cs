using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - QMC.Vision TCP link. 6 채널(Wafer/BottomInspection/Bin/Main/TopSide/BottomSide) 연결·Ping·상태.</summary>
    public partial class VisionLinkPage : PageBase
    {
        public VisionLinkPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            LoadSettings();
            WireEvents();

            VisionHub.ConnectionChanged += OnConnChanged;
            Disposed += (s, e) => VisionHub.ConnectionChanged -= OnConnChanged;
            OnConnChanged();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.simulator");
            lblHeader.Tag = "i18n:set.simulator";
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;
        }

        private void LoadSettings()
        {
            var cfg = AppSettingsStore.Current;
            _tbHost.Text = cfg.VisionHost;
            _tbWafer.Text = cfg.VisionWaferPort.ToString();
            _tbInsp.Text  = cfg.VisionInspectionPort.ToString();
            _tbBin.Text   = cfg.VisionBinPort.ToString();
            _tbMain.Text  = cfg.VisionMainPort.ToString();
            _tbTop.Text   = cfg.VisionTopSidePort.ToString();
            _tbBot.Text   = cfg.VisionBottomSidePort.ToString();
            _cbAuto.Checked = cfg.VisionAutoConnect;
        }

        private void WireEvents()
        {
            _cbAuto.CheckedChanged += (s, e) =>
            {
                AppSettingsStore.Current.VisionAutoConnect = _cbAuto.Checked;
                AppSettingsStore.Save();
            };

            _btnConnect.Click += async (s, e) => await DoConnect();
            _btnDisconnect.Click += (s, e) =>
            {
                VisionHub.DisconnectAll();
                OnConnChanged();
            };
            _btnPing.Click += async (s, e) => await DoPing();
        }

        private async Task DoConnect()
        {
            var cfg = AppSettingsStore.Current;
            cfg.VisionHost = _tbHost.Text.Trim();
            cfg.VisionWaferPort      = ParsePort(_tbWafer, cfg.VisionWaferPort);
            cfg.VisionInspectionPort = ParsePort(_tbInsp,  cfg.VisionInspectionPort);
            cfg.VisionBinPort        = ParsePort(_tbBin,   cfg.VisionBinPort);
            cfg.VisionMainPort       = ParsePort(_tbMain,  cfg.VisionMainPort);
            cfg.VisionTopSidePort    = ParsePort(_tbTop,   cfg.VisionTopSidePort);
            cfg.VisionBottomSidePort = ParsePort(_tbBot,   cfg.VisionBottomSidePort);
            AppSettingsStore.Save();

            _btnConnect.Enabled = false;
            try
            {
                await VisionHub.ConnectAllAsync(cfg.VisionHost,
                    cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                    cfg.VisionMainPort, cfg.VisionTopSidePort, cfg.VisionBottomSidePort);
            }
            catch { }
            finally
            {
                _btnConnect.Enabled = true;
            }

            OnConnChanged();
        }

        private static int ParsePort(TextBox tb, int fallback)
            => int.TryParse(tb.Text, out var p) && p > 0 && p < 65536 ? p : fallback;

        private async Task DoPing()
        {
            await PingOne(VisionHub.Wafer);
            await PingOne(VisionHub.Inspection);
            await PingOne(VisionHub.Bin);
            await PingOne(VisionHub.Main);
            await PingOne(VisionHub.TopSide);
            await PingOne(VisionHub.BottomSide);
            OnConnChanged();
        }

        private static async Task PingOne(VisionTcpClient c)
        {
            if (c == null) return;
            try { await c.PingAsync(); } catch { }
        }

        private void OnConnChanged()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnConnChanged));
                return;
            }

            SetLamp(_lblWafer, VisionHub.Wafer);
            SetLamp(_lblInsp,  VisionHub.Inspection);
            SetLamp(_lblBin,   VisionHub.Bin);
            SetLamp(_lblMain,  VisionHub.Main);
            SetLamp(_lblTop,   VisionHub.TopSide);
            SetLamp(_lblBot,   VisionHub.BottomSide);
        }

        /// <summary>연결됨(초록) / 미연결(회색). 채널별 1:1 매핑.</summary>
        private static void SetLamp(Label lamp, VisionTcpClient c)
        {
            bool on = c != null && c.IsConnected;
            lamp.ForeColor = on ? Color.LimeGreen : Color.Gray;
            lamp.Text = on ? "● 연결" : "● 미연결";
        }
    }
}
