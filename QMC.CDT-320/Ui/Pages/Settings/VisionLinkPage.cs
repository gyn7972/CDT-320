using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Localization;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - QMC.Vision TCP link. 6 채널(Wafer/BottomInspection/Bin/Main/FrontSideVision/RearSideVision) 연결·Ping·상태.</summary>
    public partial class VisionLinkPage : PageBase
    {
        /// <summary>접속돼 있는데 이 시간(초) 이상 무통신이면 RX 경과를 경고색으로 표시.</summary>
        private const double StaleSeconds = 30.0;

        private Label[] _lamps;
        private Label[] _rx;
        private Label[] _vs;
        private System.Windows.Forms.Timer _timer;
        private long _lastLogRev = -1;
        private bool _loadingSettings;

        public VisionLinkPage()
        {
            InitializeComponent();
            InitializeLanguageBindings();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
            LoadSettings();

            _lamps = new[] { _lblWafer, _lblInsp, _lblBin, _lblMain, _lblTop, _lblBot };
            _rx    = new[] { _rxWafer,  _rxInsp,  _rxBin,  _rxMain,  _rxTop,  _rxBot  };
            _vs    = new Label[] { _vsWafer, _vsInsp, _vsBin, null, _vsTop, _vsBot };

            VisionHub.ConnectionChanged += OnConnChanged;
            Disposed += (s, e) => VisionHub.ConnectionChanged -= OnConnChanged;

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                {
                    _timer.Stop();
                    return;
                }

                RefreshStatus();
                RefreshLog();
            };
            if (ShouldRefreshVisible(this))
                _timer.Start();

            RefreshStatus();
            RefreshLog();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try
            {
                if (_timer == null)
                    return;

                if (ShouldRefreshVisible(this))
                    _timer.Start();
                else
                    _timer.Stop();
            }
            catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.visionLink");
            lblHeader.Tag = "i18n:set.visionLink";

            Lang.BindKey(grpLink, "visionUi.visionLinkPage.grpLink.text");
            Lang.BindKey(grpLog, "visionUi.visionLinkPage.grpLog.text");
            Lang.BindKey(lblColModule, "visionUi.visionMonitorControl.lblModule.text");
            Lang.BindKey(lblColCmd, "visionUi.visionLinkPage.lblColCmd.text");
            Lang.BindKey(lblColViewer, "visionUi.visionLinkPage.lblColViewer.text");
            Lang.BindKey(lblColStatus, "visionUi.visionLinkPage.lblColStatus.text");
            Lang.BindKey(lblColRx, "visionUi.visionLinkPage.lblColRx.text");
            Lang.BindKey(lblColViewerStatus, "visionUi.visionLinkPage.lblColViewerStatus.text");
            Lang.BindKey(_btnClearLog, "visionUi.visionLinkPage._btnClearLog.text");
            Lang.BindKey(_btnCameraScale, "visionUi.visionLinkPage._btnCameraScale.text");
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);
            SettingsPageLayoutStyler.ApplyGroupBox(grpLink);
            SettingsPageLayoutStyler.ApplyGroupBox(_actionGroup);
            SettingsPageLayoutStyler.ApplyGroupBox(grpLog);
            SettingsPageLayoutStyler.ApplyActionRow(buttonLayout);
        }

        private void LoadSettings()
        {
            _loadingSettings = true;
            var cfg = AppSettingsStore.Current;
            try
            {
                _tbHost.Text = cfg.VisionHost;
                _tbWafer.Text = cfg.VisionWaferPort.ToString();
                _tbInsp.Text  = cfg.VisionInspectionPort.ToString();
                _tbBin.Text   = cfg.VisionBinPort.ToString();
                _tbMain.Text  = cfg.VisionMainPort.ToString();
                _tbTop.Text   = cfg.VisionFrontSidePort.ToString();
                _tbBot.Text   = cfg.VisionRearSidePort.ToString();

                _tbWaferV.Text = cfg.VisionWaferViewerPort.ToString();
                _tbInspV.Text  = cfg.VisionInspectionViewerPort.ToString();
                _tbBinV.Text   = cfg.VisionBinViewerPort.ToString();
                _tbTopV.Text   = cfg.VisionFrontSideViewerPort.ToString();
                _tbBotV.Text   = cfg.VisionRearSideViewerPort.ToString();

                _cbAuto.Checked = cfg.VisionAutoConnect;
            }
            finally
            {
                _loadingSettings = false;
            }
        }

        private void _btnCameraScale_Click(object sender, EventArgs e)
        {
            VisionCameraScaleDialog.Open(this);
        }

        private void _cbAuto_CheckedChanged(object sender, EventArgs e)
        {
            if (_loadingSettings) return;
            AppSettingsStore.Current.VisionAutoConnect = _cbAuto.Checked;
            AppSettingsStore.Save();
        }

        private async void _btnConnect_Click(object sender, EventArgs e)
        {
            await DoConnect();
        }

        private void _btnDisconnect_Click(object sender, EventArgs e)
        {
            VisionHub.DisconnectAll();
            OnConnChanged();
        }

        private async void _btnPing_Click(object sender, EventArgs e)
        {
            await DoPing();
        }

        private void _btnClearLog_Click(object sender, EventArgs e)
        {
            VisionCommLog.Clear();
            _lastLogRev = -1;
            RefreshLog();
        }

        private async Task DoConnect()
        {
            var cfg = AppSettingsStore.Current;
            cfg.VisionHost = _tbHost.Text.Trim();
            cfg.VisionWaferPort      = ParsePort(_tbWafer, cfg.VisionWaferPort);
            cfg.VisionInspectionPort = ParsePort(_tbInsp,  cfg.VisionInspectionPort);
            cfg.VisionBinPort        = ParsePort(_tbBin,   cfg.VisionBinPort);
            cfg.VisionMainPort       = ParsePort(_tbMain,  cfg.VisionMainPort);
            cfg.VisionFrontSidePort    = ParsePort(_tbTop,   cfg.VisionFrontSidePort);
            cfg.VisionRearSidePort = ParsePort(_tbBot,   cfg.VisionRearSidePort);

            // 뷰어(이미지) 포트 — 연결과 무관하지만 같은 페이지에서 함께 저장한다.
            cfg.VisionWaferViewerPort      = ParsePort(_tbWaferV, cfg.VisionWaferViewerPort);
            cfg.VisionInspectionViewerPort = ParsePort(_tbInspV,  cfg.VisionInspectionViewerPort);
            cfg.VisionBinViewerPort        = ParsePort(_tbBinV,   cfg.VisionBinViewerPort);
            cfg.VisionFrontSideViewerPort    = ParsePort(_tbTopV,   cfg.VisionFrontSideViewerPort);
            cfg.VisionRearSideViewerPort = ParsePort(_tbBotV,   cfg.VisionRearSideViewerPort);
            AppSettingsStore.Save();

            _btnConnect.Enabled = false;
            try
            {
                await VisionHub.ConnectAllAsync(cfg.VisionHost,
                    cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                    cfg.VisionMainPort, cfg.VisionFrontSidePort, cfg.VisionRearSidePort);
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

        private Form1 FindHostForm()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    Form1 host = form as Form1;
                    if (host != null)
                        return host;
                }

                return FindForm() as Form1;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private async Task DoPing()
        {
            await PingOne(VisionHub.Wafer);
            await PingOne(VisionHub.Inspection);
            await PingOne(VisionHub.Bin);
            await PingOne(VisionHub.Main);
            await PingOne(VisionHub.FrontSideVision);
            await PingOne(VisionHub.RearSideVision);
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
            RefreshStatus();
        }

        // ── 상태 램프 + 최근 수신(워치독) 갱신(1초 주기) ──
        private void RefreshStatus()
        {
            if (IsDisposed || _lamps == null) return;

            var cfg = AppSettingsStore.Current;
            SetCh(0, VisionHub.Wafer,      cfg.VisionWaferPort,      VisionViewerPorts.Wafer);
            SetCh(1, VisionHub.Inspection, cfg.VisionInspectionPort, VisionViewerPorts.BottomInspection);
            SetCh(2, VisionHub.Bin,        cfg.VisionBinPort,        VisionViewerPorts.Bin);
            SetCh(3, VisionHub.Main,       cfg.VisionMainPort,       0);
            SetCh(4, VisionHub.FrontSideVision, cfg.VisionFrontSidePort,    VisionViewerPorts.FrontSideVision);
            SetCh(5, VisionHub.RearSideVision,  cfg.VisionRearSidePort, VisionViewerPorts.RearSideVision);
        }

        private void SetCh(int i, VisionTcpClient c, int cfgPort, int viewerPort)
        {
            bool connected = c != null && c.IsConnected;
            int port = c != null ? c.Port : cfgPort;
            SetLamp(_lamps[i], connected, port);
            SetRx(_rx[i], c != null ? c.LastRxUtc : default(DateTime), connected);
            SetViewerStatus(_vs[i], viewerPort);
        }

        /// <summary>뷰어 스트림 상태. on-demand 라 평소 회색 '대기', Grab/Live 로 스트림 중이면 초록 '스트리밍'.</summary>
        private static void SetViewerStatus(Label vs, int viewerPort)
        {
            if (vs == null) return;
            if (viewerPort <= 0) { vs.ForeColor = Color.DimGray; Lang.BindFormat(vs, "visionUi.link.noViewer"); return; }
            if (VisionViewerRegistry.IsStreaming(viewerPort))
            { vs.ForeColor = Color.LimeGreen; Lang.BindFormat(vs, "visionUi.link.streaming", (object)(viewerPort)); }
            else
            { vs.ForeColor = Color.Gray; Lang.BindFormat(vs, "visionUi.link.waitingPort", (object)(viewerPort)); }
        }

        // ── 통신 로그 갱신(변경 시에만) ──
        private void RefreshLog()
        {
            if (IsDisposed || _txtLog == null) return;
            long rev = VisionCommLog.Revision;
            if (rev == _lastLogRev) return;
            _lastLogRev = rev;
            _txtLog.Lines = VisionCommLog.Snapshot();
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.ScrollToCaret();
        }

        /// <summary>접속됨(초록 :port) / 대기(회색 :port). 핸들러=클라이언트 기준.</summary>
        private static void SetLamp(Label lamp, bool connected, int port)
        {
            string suffix = port > 0 ? $" :{port}" : "";
            if (connected) { lamp.ForeColor = Color.LimeGreen; Lang.BindFormat(lamp, "visionUi.link.connected", (object)(suffix)); }
            else           { lamp.ForeColor = Color.Gray;      Lang.BindFormat(lamp, "visionUi.link.waiting", (object)(suffix)); }
        }

        /// <summary>마지막 수신 경과. 접속 중 무통신이 길면(StaleSeconds↑) 경고색.</summary>
        private static void SetRx(Label rx, DateTime lastUtc, bool connected)
        {
            if (lastUtc == default(DateTime))
            {
                rx.ForeColor = Color.DimGray;
                Lang.BindKey(rx, "visionUi.visionLinkPage.rx.text");
                return;
            }
            var d = DateTime.UtcNow - lastUtc;
            if (d.Ticks < 0) d = TimeSpan.Zero;

            string ageKey;
            int age;
            if (d.TotalSeconds < 60) { ageKey = "visionUi.link.secondsAgo"; age = (int)d.TotalSeconds; }
            else if (d.TotalMinutes < 60) { ageKey = "visionUi.link.minutesAgo"; age = (int)d.TotalMinutes; }
            else { ageKey = "visionUi.link.hoursAgo"; age = (int)d.TotalHours; }

            bool stale = connected && d.TotalSeconds > StaleSeconds;
            rx.ForeColor = stale ? Color.Goldenrod : Color.DimGray;
            Lang.BindFormat(rx, ageKey, age);
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this.lblHeader, "visionUi.visionLinkPage.lblHeader.text");
            Lang.BindKey(this.grpLink, "visionUi.visionLinkPage.grpLink.state2");
            Lang.BindKey(this.lblHost, "visionUi.visionLinkPage.lblHost.text");
            Lang.BindKey(this.lblColModule, "visionUi.visionLinkPage.lblColModule.text");
            Lang.BindKey(this.lblColCmd, "visionUi.visionLinkPage.lblColCmd.state2");
            Lang.BindKey(this.lblColViewer, "visionUi.visionLinkPage.lblColViewer.state2");
            Lang.BindKey(this.lblColStatus, "visionUi.visionLinkPage.lblColStatus.state2");
            Lang.BindKey(this.lblColRx, "visionUi.visionLinkPage.lblColRx.state2");
            Lang.BindKey(this.lblColViewerStatus, "visionUi.visionLinkPage.lblColViewerStatus.state2");
            Lang.BindKey(this.lblWaferPort, "visionUi.visionLinkPage.lblWaferPort.text");
            Lang.BindKey(this._lblWafer, "visionUi.visionLinkPage._lblWafer.text");
            Lang.BindKey(this._rxWafer, "visionUi.visionLinkPage.rx.text");
            Lang.BindFormat(this._vsWafer, "visionUi.literal", (object)("—"));
            Lang.BindKey(this.lblInspectionPort, "visionUi.visionLinkPage.lblInspectionPort.text");
            Lang.BindKey(this._lblInsp, "visionUi.visionLinkPage._lblWafer.text");
            Lang.BindKey(this._rxInsp, "visionUi.visionLinkPage.rx.text");
            Lang.BindFormat(this._vsInsp, "visionUi.literal", (object)("—"));
            Lang.BindKey(this.lblBinPort, "visionUi.visionLinkPage.lblBinPort.text");
            Lang.BindKey(this._lblBin, "visionUi.visionLinkPage._lblWafer.text");
            Lang.BindKey(this._rxBin, "visionUi.visionLinkPage.rx.text");
            Lang.BindFormat(this._vsBin, "visionUi.literal", (object)("—"));
            Lang.BindKey(this.lblMainPort, "visionUi.visionLinkPage.lblMainPort.text");
            Lang.BindKey(this._lblMain, "visionUi.visionLinkPage._lblWafer.text");
            Lang.BindKey(this._rxMain, "visionUi.visionLinkPage.rx.text");
            Lang.BindKey(this.lblTopPort, "visionUi.visionLinkPage.lblTopPort.text");
            Lang.BindKey(this._lblTop, "visionUi.visionLinkPage._lblWafer.text");
            Lang.BindKey(this._rxTop, "visionUi.visionLinkPage.rx.text");
            Lang.BindFormat(this._vsTop, "visionUi.literal", (object)("—"));
            Lang.BindKey(this.lblBotPort, "visionUi.visionLinkPage.lblBotPort.text");
            Lang.BindKey(this._lblBot, "visionUi.visionLinkPage._lblWafer.text");
            Lang.BindKey(this._rxBot, "visionUi.visionLinkPage.rx.text");
            Lang.BindFormat(this._vsBot, "visionUi.literal", (object)("—"));
            Lang.BindKey(this._cbAuto, "visionUi.visionLinkPage._cbAuto.text");
            Lang.BindKey(this._actionGroup, "visionUi.visionLinkPage._actionGroup.text");
            Lang.BindKey(this._btnConnect, "visionUi.visionLinkPage._btnConnect.text");
            Lang.BindKey(this._btnDisconnect, "visionUi.visionLinkPage._btnDisconnect.text");
            Lang.BindKey(this._btnPing, "visionUi.visionLinkPage._btnPing.text");
            Lang.BindKey(this._btnClearLog, "visionUi.visionLinkPage._btnClearLog.state2");
            Lang.BindKey(this._btnCameraScale, "visionUi.visionLinkPage._btnCameraScale.text");
            Lang.BindKey(this.grpLog, "visionUi.visionLinkPage.grpLog.text");
        }
    }
}
