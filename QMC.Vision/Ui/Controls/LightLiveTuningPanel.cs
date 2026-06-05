using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Vision.Comm;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// Stage 86 — 임시 라이브 튜닝 패널.
    /// 카메라 라이브 영상 보면서 조명 세팅 맞추기 위한 사이드 패널.
    /// 펄스 스트로브 컨트롤러용 — 주기적 명령 송신으로 화면 동안 조명 유지.
    /// <para><b>제거 방법</b>: 본 파일 2개 삭제 + InspectionLightPanel 의 4 블록 삭제 + csproj 의 Compile 2줄 삭제.</para>
    /// </summary>
    public partial class LightLiveTuningPanel : UserControl
    {
        public class TuningRow
        {
            public string ControllerPort;
            public int    Channel;
            public int    Level;
        }

        // 상태 필드
        private readonly Timer _timer;
        private Func<IEnumerable<TuningRow>> _provider;
        private bool _liveOn;
        private bool _sending;          // 재진입 가드
        private int  _sendCount;

        public LightLiveTuningPanel()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            InitializeComponent();

            _timer = new Timer { Interval = 50, Enabled = false };
            _timer.Tick += OnTick;
        }

        public void Initialize(Func<IEnumerable<TuningRow>> currentValuesProvider)
        {
            _provider = currentValuesProvider;
        }

        // ─── 이벤트 핸들러 본문 ──────────────────────────────────
        private void OnPeriodChanged(object sender, EventArgs e)
        {
            if (_timer != null) _timer.Interval = (int)_numPeriod.Value;
        }

        private void OnToggleLiveClick(object sender, EventArgs e)
        {
            if (_liveOn) StopLive();
            else         StartLive();
        }

        private void OnAllOnClick(object sender, EventArgs e)
        {
            if (_liveOn) StopLive();
            _ = SendOnceAsync(allZero: false);
        }

        private async void OnAllOffClick(object sender, EventArgs e)
        {
            if (_liveOn) StopLive();
            await SendOnceAsync(allZero: true);
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_liveOn || _sending) return;
            _ = SendOnceAsync(allZero: false);
        }

        // ─── 비즈니스 로직 ──────────────────────────────────────
        private void StartLive()
        {
            _liveOn = true;
            _btnToggleLive.Text = "■ 라이브 중지";
            _btnToggleLive.BackColor = Color.FromArgb(0xB3, 0x1B, 0x1B);
            _timer.Interval = (int)_numPeriod.Value;
            _timer.Start();
            SetStatus("라이브 중 — 주기적 송신", false);
        }

        private void StopLive()
        {
            _liveOn = false;
            _timer.Stop();
            _btnToggleLive.Text = "▶ 라이브 시작";
            _btnToggleLive.BackColor = Color.FromArgb(0x2E, 0x7D, 0x32);
            SetStatus("정지", false);
        }

        private async System.Threading.Tasks.Task SendOnceAsync(bool allZero)
        {
            if (_sending) return;
            _sending = true;
            try
            {
                var rows = _provider?.Invoke()?.ToList();
                if (rows == null || rows.Count == 0)
                {
                    SetStatus("결선 없음 — Setup 에서 컨트롤러·채널 배정 필요", true);
                    return;
                }

                int okPorts = 0, failPorts = 0;
                foreach (var grp in rows.GroupBy(r => r.ControllerPort))
                {
                    if (string.IsNullOrEmpty(grp.Key)) continue;
                    var ctrl = LightHub.Get(grp.Key);
                    if (ctrl == null) { failPorts++; continue; }

                    int[] times = new int[ctrl.ChannelCount];
                    foreach (var r in grp)
                    {
                        if (r.Channel < 1 || r.Channel > ctrl.ChannelCount) continue;
                        times[r.Channel - 1] = allZero ? 0 : r.Level;
                    }
                    try
                    {
                        bool ok = await ctrl.SetChannelBatchAsync(0, times).ConfigureAwait(true);
                        if (ok) okPorts++; else failPorts++;
                    }
                    catch { failPorts++; }
                }

                _sendCount++;
                _lblCount.Text = "송신: " + _sendCount + "회";
                _lblLast.Text  = "마지막: " + DateTime.Now.ToString("HH:mm:ss.fff");
                SetStatus(allZero
                    ? $"전체 OFF 송신 — OK {okPorts} / 실패 {failPorts}"
                    : (_liveOn ? "라이브 중" : $"단발 송신 — OK {okPorts} / 실패 {failPorts}"),
                    failPorts > 0);
            }
            finally { _sending = false; }
        }

        private void SetStatus(string msg, bool err)
        {
            if (_lblStatus == null) return;
            _lblStatus.ForeColor = err ? Color.Firebrick : Color.DarkSlateGray;
            _lblStatus.Text = "상태: " + msg;
        }
    }
}
