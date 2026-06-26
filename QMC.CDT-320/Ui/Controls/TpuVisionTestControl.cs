using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;

namespace QMC.CDT_320.Ui.Controls
{
    public sealed partial class TpuVisionTestControl : UserControl
    {
        public enum Mode
        {
            BottomInspection,
            Side
        }

        private readonly TpuVisionAdapter _adapter = new TpuVisionAdapter();
        private Mode _mode = Mode.BottomInspection;
        private int _pickerNo = 1;
        private Func<VisionTcpClient> _sideClientGetter;
        private int _sideViewerPort;
        private string _sideInspectorId;

        public TpuVisionTestControl()
        {
            InitializeComponent();

            btnExpose.Click += async (s, e) => await RunExposeAsync().ConfigureAwait(true);
            btnResult.Click += async (s, e) => await RunResultAsync().ConfigureAwait(true);
        }

        public string DialogTitle { get; private set; } = "Vision Test";

        public void Configure(
            string title,
            Mode mode,
            int pickerNo = 1,
            Func<VisionTcpClient> sideClient = null,
            int sideViewerPort = 0,
            string sideInspectorId = null)
        {
            _mode = mode;
            _pickerNo = pickerNo;
            _sideClientGetter = sideClient;
            _sideViewerPort = sideViewerPort;
            _sideInspectorId = sideInspectorId;

            VisionTcpClient commandClient = ActiveClient();
            int commandPort = commandClient != null ? commandClient.Port : 0;
            int viewerPort = ResolveViewerPort();
            bool bottom = _mode == Mode.BottomInspection;

            DialogTitle = "Vision Test - " + title +
                          (commandPort > 0 ? "  (Command " + commandPort + " / Image " + viewerPort + ")" : string.Empty);

            btnExpose.Text = bottom ? "BOTTOM EXPOSE" : "SIDE EXPOSE";
            btnResult.Text = bottom ? "BOTTOM RESULT" : "SIDE INSPECT";
            lblExpose.Text = bottom ? "Picker " + _pickerNo + " 노출 ACK 대기" : "Side 노출 ACK 대기";
            lblResult.Text = bottom ? "4 Picker DieFinder offset/OK 대기" : _sideInspectorId + " 검사 결과 대기";
            lblHint.Text = bottom
                ? "Bottom 검사는 TpuVisionAdapter의 Inspection 채널에 노출을 요청합니다. 모션 구동은 하지 않습니다."
                : "Side 검사는 해당 Side Vision 모듈에 EXPOSE/INSPECT를 요청합니다. Vision READY 상태에서만 사용하세요.";

            viewer.Configure(VisionHub.Host, viewerPort, title + " Image", commandClient);
        }

        public void StopLive()
        {
            try { viewer.StopLive(); } catch { }
        }

        private VisionTcpClient ActiveClient()
        {
            return _mode == Mode.BottomInspection
                ? VisionHub.Inspection
                : (_sideClientGetter != null ? _sideClientGetter() : null);
        }

        private int ResolveViewerPort()
        {
            return _mode == Mode.BottomInspection ? VisionViewerPorts.BottomInspection : _sideViewerPort;
        }

        private bool Ready(Label target)
        {
            VisionTcpClient client = ActiveClient();
            if (client == null)
            {
                target.ForeColor = Color.Firebrick;
                target.Text = "Vision 모듈이 없습니다.";
                return false;
            }

            if (!client.IsConnected)
            {
                target.ForeColor = Color.Firebrick;
                target.Text = "Vision이 연결되지 않았습니다. port=" + client.Port;
                return false;
            }

            return true;
        }

        private async Task RunExposeAsync()
        {
            if (!Ready(lblExpose))
                return;

            btnExpose.Enabled = false;
            lblExpose.ForeColor = Color.DimGray;
            lblExpose.Text = "EXPOSE 실행 중...";
            try
            {
                bool ok;
                if (_mode == Mode.BottomInspection)
                {
                    ok = await _adapter.TriggerBottomExposeAsync(_pickerNo, 30000).ConfigureAwait(true);
                }
                else
                {
                    VisionTcpClient client = ActiveClient();
                    ok = client != null && await client.ExposeAsync(0, 30000, CancellationToken.None).ConfigureAwait(true);
                }

                lblExpose.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                lblExpose.Text = ok ? "EXPOSE ACK 완료" : "EXPOSE 실패. Vision READY/연결 상태를 확인하세요.";
                if (ok)
                    TryStartLive();
            }
            catch (Exception ex)
            {
                lblExpose.ForeColor = Color.Firebrick;
                lblExpose.Text = "EXPOSE 실패: " + ex.Message;
            }
            finally
            {
                btnExpose.Enabled = true;
            }
        }

        private async Task RunResultAsync()
        {
            if (!Ready(lblResult))
                return;

            btnResult.Enabled = false;
            lblResult.ForeColor = Color.DimGray;
            lblResult.Text = (_mode == Mode.BottomInspection ? "RESULT" : "INSPECT") + " 실행 중...";
            try
            {
                if (_mode == Mode.BottomInspection)
                    await RunBottomResultAsync().ConfigureAwait(true);
                else
                    await RunSideResultAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                lblResult.ForeColor = Color.Firebrick;
                lblResult.Text = (_mode == Mode.BottomInspection ? "RESULT" : "INSPECT") + " 실패: " + ex.Message;
            }
            finally
            {
                btnResult.Enabled = true;
            }
        }

        private async Task RunBottomResultAsync()
        {
            BottomVisionOffset[] results = await _adapter.GetBottomResultsAsync(30000).ConfigureAwait(true);
            if (results == null)
            {
                lblResult.ForeColor = Color.Firebrick;
                lblResult.Text = "결과가 없습니다.";
                return;
            }

            var lines = new List<string>();
            bool allOk = true;
            foreach (BottomVisionOffset offset in results)
            {
                if (offset == null)
                    continue;

                if (!offset.IsOk)
                    allOk = false;

                lines.Add("P" + offset.PickerNo + ": " + (offset.IsOk ? "OK" : "NG") +
                          "  x=" + offset.OffsetX.ToString("F2") +
                          " y=" + offset.OffsetY.ToString("F2") +
                          " t=" + offset.OffsetT.ToString("F2"));
            }

            lblResult.ForeColor = allOk ? Color.SeaGreen : Color.Firebrick;
            lblResult.Text = string.Join("\r\n", lines.ToArray());
            try
            {
                viewer.SetVerdictText(allOk ? "OK" : "NG", allOk);
                viewer.SetResultLines(lines.ToArray());
            }
            catch
            {
            }
        }

        private async Task RunSideResultAsync()
        {
            VisionTcpClient client = ActiveClient();
            if (client == null)
            {
                lblResult.ForeColor = Color.Firebrick;
                lblResult.Text = "Vision 모듈이 없습니다.";
                return;
            }

            InspectionResultDto result = await client.InspectAsync(_sideInspectorId, 0, 30000).ConfigureAwait(true);
            bool acked = result != null && !string.IsNullOrEmpty(result.Raw) &&
                         result.Raw.StartsWith("ACK|", StringComparison.OrdinalIgnoreCase);
            if (!acked)
            {
                lblResult.ForeColor = Color.Firebrick;
                lblResult.Text = "INSPECT 실패: " + (result != null ? result.Raw : "no response");
                return;
            }

            lblResult.ForeColor = result.IsPass ? Color.SeaGreen : Color.Firebrick;
            lblResult.Text = (result.IsPass ? "PASS" : "FAIL") + "\r\n" + result.Raw;
            try { viewer.SetVerdictText(result.IsPass ? "OK" : "NG", result.IsPass); } catch { }
        }

        private void TryStartLive()
        {
            try { viewer.StartLive(); } catch { }
        }
    }
}
