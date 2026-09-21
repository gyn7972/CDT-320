using QMC.CDT_320.Ui.Localization;
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

        private TpuVisionAdapter _adapter = new TpuVisionAdapter(0);
        private Mode _mode = Mode.BottomInspection;
        private int _pickerNo = 1;
        private int _pickerFb;
        private Func<VisionTcpClient> _sideClientGetter;
        private int _sideViewerPort;
        private string _sideInspectorId;

        public TpuVisionTestControl()
        {
            InitializeComponent();
            InitializeLanguageBindings();

            btnExpose.Click += async (s, e) => await RunExposeAsync().ConfigureAwait(true);
            btnResult.Click += async (s, e) => await RunResultAsync().ConfigureAwait(true);
        }

        public string DialogTitle { get; private set; } = "Vision Test";
        private string _displayTitle;
        private int _displayCommandPort;
        private int _displayViewerPort;

        internal void BindDialogTitle(Control dialog)
        {
            Lang.BindFormat(dialog, _displayCommandPort > 0 ? "visionUi.tpuTitle.ports" : "visionUi.tpuTitle.name",
                _displayTitle, _displayCommandPort, _displayViewerPort);
        }

        public void Configure(
            string title,
            Mode mode,
            int pickerNo = 1,
            Func<VisionTcpClient> sideClient = null,
            int sideViewerPort = 0,
            string sideInspectorId = null,
            int pickerFb = 0)
        {
            _mode = mode;
            _pickerNo = pickerNo;
            _pickerFb = pickerFb == 1 ? 1 : 0;
            _adapter = new TpuVisionAdapter(_pickerFb);
            _sideClientGetter = sideClient;
            _sideViewerPort = sideViewerPort;
            _sideInspectorId = sideInspectorId;

            VisionTcpClient commandClient = ActiveClient();
            int commandPort = commandClient != null ? commandClient.Port : 0;
            int viewerPort = ResolveViewerPort();
            bool bottom = _mode == Mode.BottomInspection;

            _displayTitle = title;
            _displayCommandPort = commandPort;
            _displayViewerPort = viewerPort;
            DialogTitle = "Vision Test - " + title +
                          (commandPort > 0 ? "  (Command " + commandPort + " / Image " + viewerPort + ")" : string.Empty);

            {
                if (bottom)
                    Lang.BindKey(btnExpose, "visionUi.tpuVisionTestControl.btnExpose.text");
                else
                    Lang.BindKey(btnExpose, "visionUi.tpuVisionTestControl.btnExpose.state2");
            }
            {
                if (bottom)
                    Lang.BindKey(btnResult, "visionUi.tpuVisionTestControl.btnResult.text");
                else
                    Lang.BindKey(btnResult, "visionUi.tpuVisionTestControl.btnResult.state2");
            }
            {
                if (bottom)
                    Lang.BindFormat(lblExpose, "visionUi.tpuVisionTestControl.lblExpose.text", (object)(_pickerNo));
                else
                    Lang.BindKey(lblExpose, "visionUi.tpuVisionTestControl.lblExpose.state2");
            }
            {
                if (bottom)
                    Lang.BindKey(lblResult, "visionUi.tpuVisionTestControl.lblResult.text");
                else
                    Lang.BindFormat(lblResult, "visionUi.tpuVisionTestControl.lblResult.state2", (object)(_sideInspectorId));
            }
            {
                if (bottom)
                    Lang.BindKey(lblHint, "visionUi.tpuVisionTestControl.lblHint.text");
                else
                    Lang.BindKey(lblHint, "visionUi.tpuVisionTestControl.lblHint.state2");
            }

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
                Lang.BindKey(target, "visionUi.tpuVisionTestControl.target.text");
                return false;
            }

            if (!client.IsConnected)
            {
                target.ForeColor = Color.Firebrick;
                Lang.BindFormat(target, "visionUi.tpuVisionTestControl.target.state2", (object)(client.Port));
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
            Lang.BindKey(lblExpose, "visionUi.tpuVisionTestControl.lblExpose.state3");
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
                {
                    if (ok)
                        Lang.BindKey(lblExpose, "visionUi.tpuVisionTestControl.lblExpose.state4");
                    else
                        Lang.BindKey(lblExpose, "visionUi.tpuVisionTestControl.lblExpose.state5");
                }
                if (ok)
                    LogLiveAutoStartBlocked("EXPOSE 완료 후 자동 Live 시작 차단");
            }
            catch (Exception ex)
            {
                lblExpose.ForeColor = Color.Firebrick;
                Lang.BindFormat(lblExpose, "visionUi.tpuVisionTestControl.lblExpose.state6", (object)(ex.Message));
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
            Lang.BindFormat(lblResult, "visionUi.tpuVisionTestControl.lblResult.state3", (object)((_mode == Mode.BottomInspection ? "RESULT" : "INSPECT")));
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
                Lang.BindFormat(lblResult, "visionUi.tpuVisionTestControl.lblResult.state4", (object)((_mode == Mode.BottomInspection ? "RESULT" : "INSPECT")), (object)(ex.Message));
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
                Lang.BindKey(lblResult, "visionUi.tpuVisionTestControl.lblResult.state5");
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
            Lang.BindFormat(lblResult, "visionUi.literal", (object)(string.Join("\r\n", lines.ToArray())));
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
                Lang.BindKey(lblResult, "visionUi.tpuVisionTestControl.target.text");
                return;
            }

            AutoVisionChannel channel;
            if (!VisionModuleNames.TryResolveByModule(client.ModuleName, out channel) ||
                (channel != AutoVisionChannel.FrontSide && channel != AutoVisionChannel.RearSide))
            {
                lblResult.ForeColor = Color.Firebrick;
                Lang.BindKey(lblResult, "visionUi.tpuVisionTestControl.lblResult.state6");
                return;
            }

            InspectionResultDto result = await AutoVisionRequestService.InspectColletAsync(
                channel,
                _sideInspectorId,
                _pickerFb,
                _pickerNo,
                0,
                0,
                0,
                0,
                30000,
                CancellationToken.None).ConfigureAwait(true);
            if (result == null)
            {
                lblResult.ForeColor = Color.Firebrick;
                Lang.BindKey(lblResult, "visionUi.tpuVisionTestControl.lblResult.state7");
                return;
            }

            lblResult.ForeColor = result.IsPass ? Color.SeaGreen : Color.Firebrick;
            Lang.BindFormat(lblResult, "visionUi.literal", (object)((result.IsPass ? "PASS" : "FAIL") + "\r\n" + result.Raw));
            try { viewer.SetVerdictText(result.IsPass ? "OK" : "NG", result.IsPass); } catch { }
        }

        private void TryStartLive()
        {
            LogLiveAutoStartBlocked("TryStartLive 호출 차단");
        }

        private void LogLiveAutoStartBlocked(string reason)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "VISION",
                    "VISION-LIVE-BLOCK",
                    "Vision 테스트 화면에서 Live 자동 시작을 차단했습니다. mode=" + _mode +
                    ", viewerPort=" + ResolveViewerPort() +
                    ", reason=" + reason);
            }
            catch { }
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this.btnExpose, "visionUi.tpuVisionTestControl.btnExpose.state3");
            Lang.BindKey(this.btnResult, "visionUi.tpuVisionTestControl.btnResult.state3");
            Lang.BindKey(this.lblExpose, "visionUi.tpuVisionTestControl.lblExpose.state7");
            Lang.BindKey(this.lblResult, "visionUi.tpuVisionTestControl.lblResult.state8");
            Lang.BindKey(this.lblHint, "visionUi.tpuVisionTestControl.lblHint.state3");
        }
    }
}
