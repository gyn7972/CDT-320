using QMC.CDT_320.Ui.Localization;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Vision 모듈 1개 동작 테스트 팝업.
    /// UI 생성/배치는 Designer에 두고, 이 파일에는 명령 호출/이벤트/뷰어 연결만 둔다.
    /// </summary>
    public sealed partial class VisionModuleTestDialog : Form
    {
        private VisionTcpClient _client;

        public VisionModuleTestDialog()
        {
            InitializeComponent();
            InitializeLanguageBindings();
        }

        public VisionModuleTestDialog(VisionTcpClient client, string displayName)
            : this()
        {
            Init(client, displayName);
        }

        public void Init(VisionTcpClient client, string displayName)
        {
            try
            {
                _client = client;

                string title = string.IsNullOrWhiteSpace(displayName) ? "VISION" : displayName;
                Lang.BindFormat(this, "visionUi.visionModuleTestDialog.Text.text", (object)(title), (object)((client != null ? "  (port " + client.Port + ")" : string.Empty)));

                int viewerPort = 0;
                AutoVisionChannel channel;
                if (client != null && VisionModuleNames.TryResolveByModule(client.ModuleName, out channel))
                    viewerPort = VisionViewerPorts.ResolveByChannel(channel);
                _viewer.Configure(client != null ? client.Host : null, viewerPort, title + " 이미지", client);
            }
            catch (Exception ex)
            {
                _lblGrab.ForeColor = Color.Firebrick;
                Lang.BindFormat(_lblGrab, "visionUi.visionModuleTestDialog._lblGrab.text", (object)(ex.Message));
            }
            finally
            {
            }
        }

        public static void Open(IWin32Window owner, VisionTcpClient client, string displayName)
        {
            string key = "VisionModuleTestDialog:" +
                         (client != null ? client.ModuleName + ":" + client.Port.ToString() : (displayName ?? "Unknown"));

            ModelessDialogHost.Show(
                key,
                owner,
                () => new VisionModuleTestDialog(client, displayName),
                dialog => dialog.Init(client, displayName));
        }

        public static void AddLaunchers(
            Control.ControlCollection actions,
            IWin32Window owner,
            Control stopButton,
            params Tuple<string, Func<VisionTcpClient>, string>[] modules)
        {
            if (actions == null || modules == null)
                return;

            foreach (Tuple<string, Func<VisionTcpClient>, string> module in modules)
            {
                var button = new QMC.CDT_320.Ui.Controls.ActionButton
                {
                    Text = module.Item1,
                    Width = 180,
                    Height = 64,
                    Margin = new Padding(6),
                    Font = new Font("맑은 고딕", 11F)
                };

                Func<VisionTcpClient> getClient = module.Item2;
                string displayName = module.Item3;
                button.Click += (sender, args) => Open(owner, getClient(), displayName);
                actions.Add(button);
            }

            if (stopButton != null && actions.Contains(stopButton))
                actions.SetChildIndex(stopButton, actions.Count - 1);
        }

        private async void btnGrab_Click(object sender, EventArgs e)
        {
            await RunGrabAsync().ConfigureAwait(true);
        }

        private async void btnMatch_Click(object sender, EventArgs e)
        {
            await RunMatchAsync().ConfigureAwait(true);
        }

        private async void btnInspect_Click(object sender, EventArgs e)
        {
            await RunInspectAsync().ConfigureAwait(true);
        }

        private async Task RunGrabAsync()
        {
            if (!CheckReady(_lblGrab))
                return;

            _btnGrab.Enabled = false;
            _lblGrab.ForeColor = Color.DimGray;
            Lang.BindKey(_lblGrab, "visionUi.visionModuleTestDialog._lblGrab.state2");

            try
            {
                VisionProtocolResponse response = await _client.SendCommandAsync(VisionProtocolCommand.Grab, 30000, System.Threading.CancellationToken.None, 0).ConfigureAwait(true);
                bool ok = response != null && response.IsAck;
                _lblGrab.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                {
                    if (ok)
                        Lang.BindFormat(_lblGrab, "visionUi.literal", (object)(response.Payload));
                    else
                        Lang.BindFormat(_lblGrab, "visionUi.literal", (object)((response != null ? response.RawLine : "응답 없음")));
                }
            }
            catch (Exception ex)
            {
                _lblGrab.ForeColor = Color.Firebrick;
                Lang.BindFormat(_lblGrab, "visionUi.visionModuleTestDialog._lblGrab.state3", (object)(ex.Message));
            }
            finally
            {
                _btnGrab.Enabled = true;
            }
        }

        private async Task RunMatchAsync()
        {
            if (!CheckReady(_lblMatch))
                return;

            string finder = (_txtFinder.Text ?? string.Empty).Trim();
            if (finder.Length == 0)
            {
                _lblMatch.ForeColor = Color.Firebrick;
                Lang.BindKey(_lblMatch, "visionUi.visionModuleTestDialog._lblMatch.text");
                return;
            }

            _btnMatch.Enabled = false;
            _lblMatch.ForeColor = Color.DimGray;
            Lang.BindFormat(_lblMatch, "visionUi.visionModuleTestDialog._lblMatch.state2", (object)(finder));

            try
            {
                AutoVisionChannel channel;
                if (!TryResolveInspectionChannel(_lblMatch, out channel))
                    return;

                MatchResultDto result = await AutoVisionRequestService.MatchAsync(
                    channel,
                    finder,
                    0,
                    30000,
                    System.Threading.CancellationToken.None).ConfigureAwait(true);
                if (result != null && result.Success)
                {
                    _lblMatch.ForeColor = Color.SeaGreen;
                    Lang.BindFormat(_lblMatch, "visionUi.visionModuleTestDialog._lblMatch.state3", (object)(result.X), (object)(result.Y), (object)(result.AngleDeg), (object)(result.Score));
                }
                else
                {
                    _lblMatch.ForeColor = Color.Firebrick;
                    Lang.BindFormat(_lblMatch, "visionUi.visionModuleTestDialog._lblMatch.state4", (object)((result != null && !string.IsNullOrEmpty(result.RawError) ? result.RawError : "no match")));
                }
            }
            catch (Exception ex)
            {
                _lblMatch.ForeColor = Color.Firebrick;
                Lang.BindFormat(_lblMatch, "visionUi.visionModuleTestDialog._lblMatch.state4", (object)(ex.Message));
            }
            finally
            {
                _btnMatch.Enabled = true;
            }
        }

        private async Task RunInspectAsync()
        {
            if (!CheckReady(_lblInsp))
                return;

            string inspector = (_txtInsp.Text ?? string.Empty).Trim();
            if (inspector.Length == 0)
            {
                _lblInsp.ForeColor = Color.Firebrick;
                Lang.BindKey(_lblInsp, "visionUi.visionModuleTestDialog._lblInsp.text");
                return;
            }

            _btnInspect.Enabled = false;
            _lblInsp.ForeColor = Color.DimGray;
            Lang.BindFormat(_lblInsp, "visionUi.visionModuleTestDialog._lblInsp.state2", (object)(inspector));

            try
            {
                AutoVisionChannel channel;
                if (!TryResolveInspectionChannel(_lblInsp, out channel))
                    return;

                InspectionResultDto result = await AutoVisionRequestService.InspectAsync(
                    channel,
                    inspector,
                    0,
                    30000,
                    System.Threading.CancellationToken.None).ConfigureAwait(true);
                if (result != null)
                {
                    _lblInsp.ForeColor = result.IsPass ? Color.SeaGreen : Color.Firebrick;
                    Lang.BindFormat(_lblInsp, "visionUi.literal", (object)((result.IsPass ? "PASS" : "FAIL") + "   (" + result.Raw + ")"));
                }
                else
                {
                    _lblInsp.ForeColor = Color.Firebrick;
                    Lang.BindFormat(_lblInsp, "visionUi.visionModuleTestDialog._lblInsp.state3", (object)((result != null ? result.Raw : "응답 없음")));
                }
            }
            catch (Exception ex)
            {
                _lblInsp.ForeColor = Color.Firebrick;
                Lang.BindFormat(_lblInsp, "visionUi.visionModuleTestDialog._lblInsp.state3", (object)(ex.Message));
            }
            finally
            {
                _btnInspect.Enabled = true;
            }
        }

        private bool TryResolveInspectionChannel(Label target, out AutoVisionChannel channel)
        {
            channel = AutoVisionChannel.Wafer;
            if (_client != null &&
                VisionModuleNames.TryResolveByModule(_client.ModuleName, out channel) &&
                channel != AutoVisionChannel.Main)
                return true;

            target.ForeColor = Color.Firebrick;
            Lang.BindKey(target, "visionUi.visionModuleTestDialog.target.text");
            return false;
        }

        private bool CheckReady(Label target)
        {
            if (_client == null)
            {
                target.ForeColor = Color.Firebrick;
                Lang.BindKey(target, "visionUi.visionModuleTestDialog.target.state2");
                return false;
            }

            if (!_client.IsConnected)
            {
                target.ForeColor = Color.Firebrick;
                Lang.BindFormat(target, "visionUi.visionModuleTestDialog.target.state3", (object)(_client.Port));
                return false;
            }

            return true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_viewer != null)
                    _viewer.StopLive();
            }
            catch
            {
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this._btnGrab, "visionUi.waferVisionTestControl.btnExpose.text");
            Lang.BindKey(this._lblGrab, "visionUi.visionModuleTestDialog._lblGrab.state4");
            Lang.BindKey(this._btnMatch, "visionUi.visionModuleTestDialog._btnMatch.text");
            Lang.BindKey(this._lblMatch, "visionUi.visionModuleTestDialog._lblMatch.state5");
            Lang.BindKey(this._btnInspect, "visionUi.visionModuleTestDialog._btnInspect.text");
            Lang.BindKey(this._lblInsp, "visionUi.visionModuleTestDialog._lblInsp.state4");
            Lang.BindKey(this._hint, "visionUi.visionModuleTestDialog._hint.text");
            Lang.BindKey(this, "visionUi.visionModuleTestDialog.Text.state2");
        }
    }
}
