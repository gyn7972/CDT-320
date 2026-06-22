using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.Comm;
using QMC.Vision.Config;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 설정 - 통신(TCP) 페이지. 핸들러 SettingsTab 의 VisionLinkPage 와 대칭.
    /// <para>토폴로지: Vision = TCP 서버(listen), 핸들러 = TCP 클라이언트(접속).</para>
    /// 6 채널(Wafer/Inspection/Bin/Main/TopSide/BottomSide) 서버 포트 편집·저장 +
    /// 1초 주기 상태 램프(접속) + 최근 수신 경과(워치독) + 통신 로그(TX/RX/EPD/ARM) 표시.
    /// </summary>
    public partial class CommLinkPage : PageBase
    {
        /// <summary>접속돼 있는데 이 시간(초) 이상 무통신이면 RX 경과를 경고색으로 표시.</summary>
        private const double StaleSeconds = 30.0;

        private Label[]   _lamps;
        private Label[]   _rx;
        private TextBox[] _ports;
        private System.Windows.Forms.Timer _timer;
        private long _lastLogRev = -1;

        public CommLinkPage()
        {
            InitializeComponent();
            if (IsDesignerMode()) return;

            _lamps = new[] { _lampWafer, _lampInsp, _lampBin, _lampMain, _lampTop, _lampBot };
            _rx    = new[] { _rxWafer,   _rxInsp,   _rxBin,   _rxMain,   _rxTop,   _rxBot   };
            _ports = new[] { _tbWafer,   _tbInsp,   _tbBin,   _tbMain,   _tbTop,   _tbBot   };

            LoadPorts();
            WireEvents();

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => { RefreshStatus(); RefreshLog(); };
            _timer.Start();
            RefreshStatus();
            RefreshLog();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }

        private void WireEvents()
        {
            _btnSave.Click += OnSaveClick;
            _btnLoad.Click += OnLoadClick;
            _btnClearLog.Click += (s, e) => { VisionCommLog.Clear(); _lastLogRev = -1; RefreshLog(); };
        }

        private void LoadPorts()
        {
            var cfg = VisionConfigStore.Current;
            _tbWafer.Text = cfg.WaferVisionPort.ToString();
            _tbInsp.Text  = cfg.InspectionVisionPort.ToString();
            _tbBin.Text   = cfg.BinVisionPort.ToString();
            _tbMain.Text  = cfg.MainCommPort.ToString();
            _tbTop.Text   = cfg.TopSideVisionPort.ToString();
            _tbBot.Text   = cfg.BottomSideVisionPort.ToString();
        }

        // ── 저장 — 6 포트를 vision.json 에 기록(재시작 후 반영) ──
        private void OnSaveClick(object sender, EventArgs e)
        {
            try
            {
                var cfg = VisionConfigStore.Current;
                cfg.WaferVisionPort      = ParsePort(_tbWafer, cfg.WaferVisionPort);
                cfg.InspectionVisionPort = ParsePort(_tbInsp,  cfg.InspectionVisionPort);
                cfg.BinVisionPort        = ParsePort(_tbBin,   cfg.BinVisionPort);
                cfg.MainCommPort         = ParsePort(_tbMain,  cfg.MainCommPort);
                cfg.TopSideVisionPort    = ParsePort(_tbTop,   cfg.TopSideVisionPort);
                cfg.BottomSideVisionPort = ParsePort(_tbBot,   cfg.BottomSideVisionPort);
                VisionConfigStore.Save();
                LoadPorts();
                MessageBox.Show("저장되었습니다.\n(포트 변경은 재시작 후 반영)",
                    "통신 설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("저장 실패: " + ex.Message, "통신 설정",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnLoadClick(object sender, EventArgs e)
        {
            try
            {
                VisionConfigStore.Load();
                LoadPorts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("불러오기 실패: " + ex.Message, "통신 설정",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static int ParsePort(TextBox tb, int fallback)
            => int.TryParse(tb.Text, out var p) && p > 0 && p < 65536 ? p : fallback;

        // ── 상태 램프 + 최근 수신(워치독) 갱신(1초 주기) ──
        private void RefreshStatus()
        {
            if (IsDisposed || _lamps == null) return;

            List<Form1.CommChannelStatus> st = null;
            try { st = (FindForm() as Form1)?.GetVisionCommStatus(); } catch { }

            if (st == null)
            {
                for (int i = 0; i < _lamps.Length; i++) { SetLamp(_lamps[i], false, false, 0); SetRx(_rx[i], default(DateTime), false); }
                return;
            }

            int n = Math.Min(_lamps.Length, st.Count);
            for (int i = 0; i < n; i++)
            {
                SetLamp(_lamps[i], st[i].Listening, st[i].Connected, st[i].Port);
                SetRx(_rx[i], st[i].LastRxUtc, st[i].Connected);
            }
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

        /// <summary>접속됨(초록) / 대기·listen(주황) / 중지(회색).</summary>
        private static void SetLamp(Label lamp, bool listening, bool connected, int port)
        {
            string suffix = port > 0 ? $" :{port}" : "";
            if (connected)      { lamp.ForeColor = Color.LimeGreen; lamp.Text = "● 접속됨" + suffix; }
            else if (listening) { lamp.ForeColor = Color.Goldenrod; lamp.Text = "● 대기"   + suffix; }
            else                { lamp.ForeColor = Color.Gray;      lamp.Text = "● 중지"   + suffix; }
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
