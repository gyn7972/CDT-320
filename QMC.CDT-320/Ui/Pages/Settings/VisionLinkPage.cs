using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Settings - QMC.Vision TCP link. 6 채널(Wafer/BottomInspection/Bin/Main/TopSide/BottomSide) 연결·Ping·상태.</summary>
    public partial class VisionLinkPage : PageBase
    {
        /// <summary>접속돼 있는데 이 시간(초) 이상 무통신이면 RX 경과를 경고색으로 표시.</summary>
        private const double StaleSeconds = 30.0;

        private Label[] _lamps;
        private Label[] _rx;
        private Label[] _vs;
        private System.Windows.Forms.Timer _timer;
        private long _lastLogRev = -1;

        public VisionLinkPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            LoadSettings();
            WireEvents();

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
            try { if (ShouldRefreshVisible(this)) _timer.Start(); else _timer.Stop(); } catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
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

            _tbWaferV.Text = cfg.VisionWaferViewerPort.ToString();
            _tbInspV.Text  = cfg.VisionInspectionViewerPort.ToString();
            _tbBinV.Text   = cfg.VisionBinViewerPort.ToString();
            _tbTopV.Text   = cfg.VisionTopSideViewerPort.ToString();
            _tbBotV.Text   = cfg.VisionBottomSideViewerPort.ToString();

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
            _btnClearLog.Click += (s, e) => { VisionCommLog.Clear(); _lastLogRev = -1; RefreshLog(); };
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

            // 뷰어(이미지) 포트 — 연결과 무관하지만 같은 페이지에서 함께 저장한다.
            cfg.VisionWaferViewerPort      = ParsePort(_tbWaferV, cfg.VisionWaferViewerPort);
            cfg.VisionInspectionViewerPort = ParsePort(_tbInspV,  cfg.VisionInspectionViewerPort);
            cfg.VisionBinViewerPort        = ParsePort(_tbBinV,   cfg.VisionBinViewerPort);
            cfg.VisionTopSideViewerPort    = ParsePort(_tbTopV,   cfg.VisionTopSideViewerPort);
            cfg.VisionBottomSideViewerPort = ParsePort(_tbBotV,   cfg.VisionBottomSideViewerPort);
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
            SetCh(4, VisionHub.TopSide,    cfg.VisionTopSidePort,    VisionViewerPorts.TopSide);
            SetCh(5, VisionHub.BottomSide, cfg.VisionBottomSidePort, VisionViewerPorts.BottomSide);
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
            if (viewerPort <= 0) { vs.ForeColor = Color.DimGray; vs.Text = "—"; return; }
            if (VisionViewerRegistry.IsStreaming(viewerPort))
            { vs.ForeColor = Color.LimeGreen; vs.Text = "● 스트리밍 :" + viewerPort; }
            else
            { vs.ForeColor = Color.Gray; vs.Text = "● 대기 :" + viewerPort; }
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
            if (connected) { lamp.ForeColor = Color.LimeGreen; lamp.Text = "● 접속됨" + suffix; }
            else           { lamp.ForeColor = Color.Gray;      lamp.Text = "● 대기"   + suffix; }
        }

        /// <summary>마지막 수신 경과. 접속 중 무통신이 길면(StaleSeconds↑) 경고색.</summary>
        private static void SetRx(Label rx, DateTime lastUtc, bool connected)
        {
            if (lastUtc == default(DateTime))
            {
                rx.ForeColor = Color.DimGray;
                rx.Text = "RX -";
                return;
            }
            var d = DateTime.UtcNow - lastUtc;
            if (d.Ticks < 0) d = TimeSpan.Zero;

            string ago;
            if (d.TotalSeconds < 60)      ago = $"{(int)d.TotalSeconds}s 전";
            else if (d.TotalMinutes < 60) ago = $"{(int)d.TotalMinutes}m 전";
            else                          ago = $"{(int)d.TotalHours}h 전";

            bool stale = connected && d.TotalSeconds > StaleSeconds;
            rx.ForeColor = stale ? Color.Goldenrod : Color.DimGray;
            rx.Text = "RX " + ago;
        }
    }
}
