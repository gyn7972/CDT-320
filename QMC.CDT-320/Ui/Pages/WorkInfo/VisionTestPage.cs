using QMC.CDT_320.Ui.Localization;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class VisionTestPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        public VisionTestPage()
        {
            InitializeComponent();
            InitializeLanguageBindings();
            Lang.BindChoices(_cbVisionModule, FormatModuleChoice);

            _cbVisionModule.Items.AddRange(new object[] { "Wafer", "Inspection", "Bin", "FrontSideVision", "RearSideVision" });
            _cbVisionModule.SelectedIndex = 0;
        }

        private async void _btnCommTest_Click(object sender, EventArgs e)
        {
            await RunCommTest();
        }

        private async void _btnGrab_Click(object sender, EventArgs e)
        {
            await RunGrab();
        }

        private async void _btnMatch_Click(object sender, EventArgs e)
        {
            await RunMatch();
        }

        private async void _btnInspect_Click(object sender, EventArgs e)
        {
            await RunInspect();
        }

        /// <summary>선택한 Vision 모듈 채널.</summary>
        private VisionTcpClient SelectedVisionModule()
        {
            switch (_cbVisionModule.SelectedIndex)
            {
                case 0:  return VisionHub.Wafer;
                case 1:  return VisionHub.Inspection;
                case 2:  return VisionHub.Bin;
                case 3:  return VisionHub.FrontSideVision;
                case 4:  return VisionHub.RearSideVision;
                default: return VisionHub.Wafer;
            }
        }

        /// <summary>선택 모듈에 EXPOSE(1장 그랩) 요청 후 결과(w/h/frame)를 표시.</summary>
        private async Task RunGrab()
        {
            var c = SelectedVisionModule();
            string name = _cbVisionModule.SelectedItem?.ToString() ?? "Vision";
            if (c == null)
            {
                _lblGrabResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindFormat(_lblGrabResult, "visionUi.visionTestPage._lblGrabResult.text", (object)(name));
                return;
            }
            if (!c.IsConnected)
            {
                _lblGrabResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindFormat(_lblGrabResult, "visionUi.test.notConnectedPort", (object)(name), (object)(c.Port));
                return;
            }

            _btnGrab.Enabled = false;
            _lblGrabResult.ForeColor = System.Drawing.Color.DimGray;
            Lang.BindFormat(_lblGrabResult, "visionUi.visionTestPage._lblGrabResult.state2", (object)(name));
            try
            {
                VisionProtocolResponse resp = await c.SendCommandAsync(VisionProtocolCommand.Grab, 5000, System.Threading.CancellationToken.None, 0);
                bool ok = resp != null && resp.IsAck;
                string body = resp != null ? resp.Payload : "응답 없음";
                _lblGrabResult.ForeColor = ok ? System.Drawing.Color.SeaGreen : System.Drawing.Color.Firebrick;
                if (resp == null)
                    Lang.BindFormat(_lblGrabResult, "visionUi.test.grabNoResponse", c.ModuleName);
                else
                    Lang.BindFormat(_lblGrabResult, "visionUi.test.grabResponse", c.ModuleName, ok ? body : resp.RawLine);
            }
            catch (Exception ex)
            {
                _lblGrabResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindFormat(_lblGrabResult, "visionUi.visionTestPage._lblGrabResult.state3", (object)(name), (object)(ex.Message));
                QMC.Common.MessageDialog.Show(Lang.Format("visionUi.visionTestPage._lblGrabResult.state3", (object)(name), (object)(ex.Message)), Lang.T("visionUi.visionModuleTestDialog.Text.state2"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnGrab.Enabled = true;
            }
        }

        /// <summary>선택 모듈에 MATCH(finder) 요청 → 좌표/각도/score 데이터 파싱·표시.</summary>
        private async Task RunMatch()
        {
            var c = SelectedVisionModule();
            string name = _cbVisionModule.SelectedItem?.ToString() ?? "Vision";
            string finder = (_txtFinder.Text ?? "").Trim();
            if (c == null || !c.IsConnected)
            {
                _lblMatchResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindFormat(_lblMatchResult, "visionUi.test.notConnected", (object)(name));
                return;
            }
            if (finder.Length == 0)
            {
                _lblMatchResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindKey(_lblMatchResult, "visionUi.visionTestPage._lblMatchResult.text");
                return;
            }

            _btnMatch.Enabled = false;
            _lblMatchResult.ForeColor = System.Drawing.Color.DimGray;
            Lang.BindFormat(_lblMatchResult, "visionUi.test.matching", (object)(name), (object)(finder));
            try
            {
                AutoVisionChannel channel;
                if (!VisionModuleNames.TryResolveByModule(c.ModuleName, out channel) || channel == AutoVisionChannel.Main)
                    throw new InvalidOperationException("신규 검사 규약을 지원하는 Vision 카메라 채널이 아닙니다.");

                MatchResultDto r = await AutoVisionRequestService.MatchAsync(
                    channel,
                    finder,
                    0,
                    30000,
                    System.Threading.CancellationToken.None);
                if (r != null && r.Success)
                {
                    _lblMatchResult.ForeColor = System.Drawing.Color.SeaGreen;
                    Lang.BindFormat(_lblMatchResult, "visionUi.test.matchOk", (object)(c.ModuleName), (object)(r.X), (object)(r.Y), (object)(r.AngleDeg), (object)(r.Score));
                }
                else
                {
                    _lblMatchResult.ForeColor = System.Drawing.Color.Firebrick;
                    if (r == null || string.IsNullOrEmpty(r.RawError))
                        Lang.BindFormat(_lblMatchResult, "visionUi.test.matchNoResponse", c.ModuleName);
                    else
                        Lang.BindFormat(_lblMatchResult, "visionUi.test.matchFailed", c.ModuleName, r.RawError);
                }
            }
            catch (Exception ex)
            {
                _lblMatchResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindFormat(_lblMatchResult, "visionUi.visionTestPage._lblMatchResult.state2", (object)(name), (object)(ex.Message));
                QMC.Common.MessageDialog.Show(Lang.Format("visionUi.visionTestPage._lblMatchResult.state2", (object)(name), (object)(ex.Message)), Lang.T("visionUi.visionModuleTestDialog.Text.state2"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnMatch.Enabled = true;
            }
        }

        /// <summary>선택 모듈에 INSPECT(inspector) 요청 → PASS/FAIL 판정 데이터 파싱·표시.</summary>
        private async Task RunInspect()
        {
            var c = SelectedVisionModule();
            string name = _cbVisionModule.SelectedItem?.ToString() ?? "Vision";
            string inspector = (_txtInspector.Text ?? "").Trim();
            if (c == null || !c.IsConnected)
            {
                _lblInspectResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindFormat(_lblInspectResult, "visionUi.test.notConnected", (object)(name));
                return;
            }
            if (inspector.Length == 0)
            {
                _lblInspectResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindKey(_lblInspectResult, "visionUi.visionTestPage._lblInspectResult.text");
                return;
            }

            _btnInspect.Enabled = false;
            _lblInspectResult.ForeColor = System.Drawing.Color.DimGray;
            Lang.BindFormat(_lblInspectResult, "visionUi.test.inspecting", (object)(name), (object)(inspector));
            try
            {
                AutoVisionChannel channel;
                if (!VisionModuleNames.TryResolveByModule(c.ModuleName, out channel) || channel == AutoVisionChannel.Main)
                    throw new InvalidOperationException("신규 검사 규약을 지원하는 Vision 카메라 채널이 아닙니다.");

                InspectionResultDto r = await AutoVisionRequestService.InspectAsync(
                    channel,
                    inspector,
                    0,
                    30000,
                    System.Threading.CancellationToken.None);
                if (r != null)
                {
                    _lblInspectResult.ForeColor = r.IsPass ? System.Drawing.Color.SeaGreen : System.Drawing.Color.Firebrick;
                    Lang.BindFormat(_lblInspectResult, r.IsPass ? "visionUi.test.inspectPass" : "visionUi.test.inspectFail", c.ModuleName, r.Raw);
                }
                else
                {
                    _lblInspectResult.ForeColor = System.Drawing.Color.Firebrick;
                    Lang.BindFormat(_lblInspectResult, "visionUi.test.inspectNoResponse", (object)(c.ModuleName));
                }
            }
            catch (Exception ex)
            {
                _lblInspectResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindFormat(_lblInspectResult, "visionUi.visionTestPage._lblInspectResult.state2", (object)(name), (object)(ex.Message));
                QMC.Common.MessageDialog.Show(Lang.Format("visionUi.visionTestPage._lblInspectResult.state2", (object)(name), (object)(ex.Message)), Lang.T("visionUi.visionModuleTestDialog.Text.state2"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnInspect.Enabled = true;
            }
        }

        /// <summary>미접속이면 ConnectAll 후 6채널(Wafer/Inspection/Bin/Main/FrontSideVision/RearSideVision) PING → 채널별 OK/FAIL 표시.</summary>
        private async Task RunCommTest()
        {
            _btnCommTest.Enabled = false;
            _lblCommResult.ForeColor = System.Drawing.Color.DimGray;
            Lang.BindKey(_lblCommResult, "visionUi.visionTestPage._lblCommResult.text");
            try
            {
                var cfg = AppSettingsStore.Current;
                if (!VisionHub.AnyConnected)
                {
                    await VisionHub.ConnectAllAsync(
                        cfg.VisionHost,
                        cfg.VisionWaferPort, cfg.VisionInspectionPort, cfg.VisionBinPort,
                        cfg.VisionMainPort,  cfg.VisionFrontSidePort,    cfg.VisionRearSidePort);
                }

                int ok = 0, total = 0;
                var sb = new StringBuilder();
                foreach (var ch in new[]
                {
                    ("Wafer",      VisionHub.Wafer),
                    ("Inspection", VisionHub.Inspection),
                    ("Bin",        VisionHub.Bin),
                    ("Main",       VisionHub.Main),
                    ("FrontSideVision", VisionHub.FrontSideVision),
                    ("RearSideVision",  VisionHub.RearSideVision),
                })
                {
                    total++;
                    string line = await PingLine(ch.Item1, ch.Item2);
                    if (line.Contains(": OK")) ok++;
                    sb.AppendLine(line);
                }

                string summary = Lang.Format("visionUi.test.commSummary", ok, total);
                _lblCommResult.ForeColor = (ok == total) ? System.Drawing.Color.SeaGreen : System.Drawing.Color.Firebrick;
                Lang.BindFormat(_lblCommResult, "visionUi.test.commSummary", (object)(ok), (object)(total));
                QMC.Common.MessageDialog.Show(sb.ToString().TrimEnd(), summary,
                    MessageBoxButtons.OK, (ok == total) ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                _lblCommResult.ForeColor = System.Drawing.Color.Firebrick;
                Lang.BindFormat(_lblCommResult, "visionUi.visionTestPage._lblCommResult.state2", (object)(ex.Message));
                QMC.Common.MessageDialog.Show(Lang.Format("visionUi.visionTestPage._lblCommResult.state2", (object)(ex.Message)), Lang.T("visionUi.visionTestPage.message.text"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnCommTest.Enabled = true;
            }
        }

        private static async Task<string> PingLine(string name, VisionTcpClient c)
        {
            if (c == null) return $"{name}: - (없음)";
            if (!c.IsConnected) return $"{name} ({c.Port}): 미연결";
            bool ok = false;
            try { ok = await c.PingAsync(); } catch { ok = false; }
            return $"{name} ({c.Port}): {(ok ? "OK" : "FAIL")}";
        }
        private static string FormatModuleChoice(string value)
        {
            switch (value)
            {
                case "Wafer": return Lang.T("visionUi.moduleChoice.wafer");
                case "Inspection": return Lang.T("visionUi.moduleChoice.inspection");
                case "Bin": return Lang.T("visionUi.moduleChoice.bin");
                case "FrontSideVision": return Lang.T("visionUi.moduleChoice.front");
                case "RearSideVision": return Lang.T("visionUi.moduleChoice.rear");
                default: return value;
            }
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this.lblHeader, "visionUi.visionTestPage.lblHeader.text");
            Lang.BindKey(this.grpComm, "visionUi.visionTestPage.grpComm.text");
            Lang.BindKey(this._btnCommTest, "visionUi.visionTestPage._btnCommTest.text");
            Lang.BindKey(this._lblCommResult, "visionUi.visionTestPage._lblCommResult.state3");
            Lang.BindKey(this._btnGrab, "visionUi.waferVisionTestControl.btnExpose.text");
            Lang.BindKey(this._lblGrabResult, "visionUi.visionTestPage._lblGrabResult.state4");
            Lang.BindKey(this._btnMatch, "visionUi.visionModuleTestDialog._btnMatch.text");
            Lang.BindKey(this._lblMatchResult, "visionUi.visionTestPage._lblMatchResult.state3");
            Lang.BindKey(this._btnInspect, "visionUi.visionModuleTestDialog._btnInspect.text");
            Lang.BindKey(this._lblInspectResult, "visionUi.visionTestPage._lblInspectResult.state3");
        }
    }
}
