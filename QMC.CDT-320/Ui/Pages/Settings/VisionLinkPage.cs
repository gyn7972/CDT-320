using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - QMC.Vision TCP link.</summary>
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
            _tbInsp.Text = cfg.VisionInspectionPort.ToString();
            _tbBin.Text = cfg.VisionBinPort.ToString();
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
            AppSettingsStore.Current.VisionHost = _tbHost.Text.Trim();
            int.TryParse(_tbWafer.Text, out var pW);
            int.TryParse(_tbInsp.Text, out var pI);
            int.TryParse(_tbBin.Text, out var pB);
            AppSettingsStore.Current.VisionWaferPort = pW;
            AppSettingsStore.Current.VisionInspectionPort = pI;
            AppSettingsStore.Current.VisionBinPort = pB;
            AppSettingsStore.Save();

            _btnConnect.Enabled = false;
            try
            {
                await VisionHub.ConnectAllAsync(AppSettingsStore.Current.VisionHost, pW, pI, pB);
            }
            catch { }
            finally
            {
                _btnConnect.Enabled = true;
            }

            OnConnChanged();
        }

        private async Task DoPing()
        {
            if (VisionHub.Wafer != null) await VisionHub.Wafer.PingAsync();
            if (VisionHub.Inspection != null) await VisionHub.Inspection.PingAsync();
            if (VisionHub.Bin != null) await VisionHub.Bin.PingAsync();
            OnConnChanged();
        }

        private void OnConnChanged()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnConnChanged));
                return;
            }

            Color onColor = Color.LimeGreen;
            Color offColor = Color.Gray;
            _lblWafer.ForeColor = VisionHub.Wafer?.IsConnected == true ? onColor : offColor;
            _lblInsp.ForeColor = VisionHub.Inspection?.IsConnected == true ? onColor : offColor;
            _lblBin.ForeColor = VisionHub.Bin?.IsConnected == true ? onColor : offColor;
        }
    }
}
