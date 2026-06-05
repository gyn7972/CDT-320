using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>설정 — QMC.Vision 프로세스와의 TCP 연결 설정/상태.</summary>
    public class VisionLinkPage : PageBase
    {
        private TextBox _tbHost, _tbWafer, _tbInsp, _tbBin;
        private CheckBox _cbAuto;
        private Button _btnConnect, _btnDisconnect, _btnPing;
        private Label _lblWafer, _lblInsp, _lblBin;

        public VisionLinkPage()
        {
            Controls.Add(CreateSectionHeader("set.simulator")); // 섹션 재사용
            BuildBody();

            VisionHub.ConnectionChanged += OnConnChanged;
            Disposed += (s, e) => VisionHub.ConnectionChanged -= OnConnChanged;
            OnConnChanged();
        }

        private void BuildBody()
        {
            var cfg = AppSettingsStore.Current;

            var grp = new GroupBox
            {
                Location = new Point(30, 50), Size = new Size(780, 360),
                Text = "QMC.Vision TCP Link", Font = UiTheme.SectionFont
            };

            grp.Controls.Add(new Label { Location = new Point(16, 30), AutoSize = true, Text = "Host", Font = UiTheme.ButtonFont });
            _tbHost = new TextBox { Location = new Point(80, 26), Size = new Size(160, 26), Text = cfg.VisionHost, Font = UiTheme.ButtonFont };
            grp.Controls.Add(_tbHost);

            grp.Controls.Add(new Label { Location = new Point(16, 66), AutoSize = true, Text = "Wafer Port", Font = UiTheme.ButtonFont });
            _tbWafer = new TextBox { Location = new Point(120, 62), Size = new Size(80, 26), Text = cfg.VisionWaferPort.ToString(), Font = UiTheme.ValueFont };
            _lblWafer = new Label  { Location = new Point(210, 66), AutoSize = true, Text = "●", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.Gray };
            grp.Controls.Add(_tbWafer); grp.Controls.Add(_lblWafer);

            grp.Controls.Add(new Label { Location = new Point(16, 98), AutoSize = true, Text = "Inspection Port", Font = UiTheme.ButtonFont });
            _tbInsp = new TextBox { Location = new Point(120, 94), Size = new Size(80, 26), Text = cfg.VisionInspectionPort.ToString(), Font = UiTheme.ValueFont };
            _lblInsp = new Label  { Location = new Point(210, 98), AutoSize = true, Text = "●", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.Gray };
            grp.Controls.Add(_tbInsp); grp.Controls.Add(_lblInsp);

            grp.Controls.Add(new Label { Location = new Point(16, 130), AutoSize = true, Text = "Bin Port", Font = UiTheme.ButtonFont });
            _tbBin = new TextBox { Location = new Point(120, 126), Size = new Size(80, 26), Text = cfg.VisionBinPort.ToString(), Font = UiTheme.ValueFont };
            _lblBin = new Label  { Location = new Point(210, 130), AutoSize = true, Text = "●", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.Gray };
            grp.Controls.Add(_tbBin); grp.Controls.Add(_lblBin);

            _cbAuto = new CheckBox { Location = new Point(16, 164), AutoSize = true, Text = "앱 시작 시 자동 연결", Font = UiTheme.ButtonFont, Checked = cfg.VisionAutoConnect };
            _cbAuto.CheckedChanged += (s, e) =>
            {
                AppSettingsStore.Current.VisionAutoConnect = _cbAuto.Checked;
                AppSettingsStore.Save();
            };
            grp.Controls.Add(_cbAuto);

            _btnConnect    = new Button { Location = new Point(16, 200), Size = new Size(140, 36), Text = "CONNECT",    FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            _btnDisconnect = new Button { Location = new Point(164, 200), Size = new Size(140, 36), Text = "DISCONNECT", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            _btnPing       = new Button { Location = new Point(312, 200), Size = new Size(140, 36), Text = "PING ALL",  FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            _btnConnect   .Click += async (s, e) => await DoConnect();
            _btnDisconnect.Click += (s, e) => { VisionHub.DisconnectAll(); OnConnChanged(); };
            _btnPing      .Click += async (s, e) => await DoPing();
            grp.Controls.Add(_btnConnect); grp.Controls.Add(_btnDisconnect); grp.Controls.Add(_btnPing);

            var hint = new Label
            {
                Location = new Point(16, 250), Size = new Size(740, 80),
                Text = "QMC.Vision.exe 를 먼저 실행해야 연결됩니다.\n" +
                       "매뉴얼 기준 포트: Wafer=5100, Inspection=5101, Bin=5103.",
                ForeColor = Color.DimGray, Font = UiTheme.ValueFont
            };
            grp.Controls.Add(hint);

            Controls.Add(grp);
        }

        private async Task DoConnect()
        {
            // 입력값 저장
            AppSettingsStore.Current.VisionHost = _tbHost.Text.Trim();
            int.TryParse(_tbWafer.Text, out var pW); AppSettingsStore.Current.VisionWaferPort      = pW;
            int.TryParse(_tbInsp .Text, out var pI); AppSettingsStore.Current.VisionInspectionPort = pI;
            int.TryParse(_tbBin  .Text, out var pB); AppSettingsStore.Current.VisionBinPort        = pB;
            AppSettingsStore.Save();

            _btnConnect.Enabled = false;
            try
            {
                await VisionHub.ConnectAllAsync(
                    AppSettingsStore.Current.VisionHost,
                    pW, pI, pB);
            }
            catch { }
            _btnConnect.Enabled = true;
            OnConnChanged();
        }

        private async Task DoPing()
        {
            if (VisionHub.Wafer      != null) await VisionHub.Wafer.PingAsync();
            if (VisionHub.Inspection != null) await VisionHub.Inspection.PingAsync();
            if (VisionHub.Bin        != null) await VisionHub.Bin.PingAsync();
            OnConnChanged();
        }

        private void OnConnChanged()
        {
            if (InvokeRequired) { BeginInvoke(new Action(OnConnChanged)); return; }
            Color OnCol  = Color.LimeGreen;
            Color OffCol = Color.Gray;
            _lblWafer.ForeColor = VisionHub.Wafer?.IsConnected      == true ? OnCol : OffCol;
            _lblInsp .ForeColor = VisionHub.Inspection?.IsConnected == true ? OnCol : OffCol;
            _lblBin  .ForeColor = VisionHub.Bin?.IsConnected        == true ? OnCol : OffCol;
        }
    }
}
