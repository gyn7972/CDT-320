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
            _btnGrab.Click    += async (s, e) => await RunGrab();
            _btnMatch.Click   += async (s, e) => await RunMatch();
            _btnInspect.Click += async (s, e) => await RunInspect();
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
                Text = "VISION 동작 테스트 - " + title + (client != null ? "  (port " + client.Port + ")" : string.Empty);

                int viewerPort = client != null ? VisionViewerPorts.ResolveByModule(client.ModuleName) : 0;
                _viewer.Configure(client != null ? client.Host : null, viewerPort, title + " 이미지", client);
            }
            catch (Exception ex)
            {
                _lblGrab.ForeColor = Color.Firebrick;
                _lblGrab.Text = "초기화 실패: " + ex.Message;
            }
            finally
            {
            }
        }

        public static void Open(IWin32Window owner, VisionTcpClient client, string displayName)
        {
            using (var dialog = new VisionModuleTestDialog())
            {
                dialog.Init(client, displayName);
                dialog.ShowDialog(owner);
            }
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

        private bool Ready(Label target)
        {
            if (!CheckReady(_lblGrab))
                return;

            _btnGrab.Enabled = false;
            _lblGrab.ForeColor = Color.DimGray;
            _lblGrab.Text = "GRAB 중...";

            try
            {
                string resp = await _c.SendAsync($"{_c.ModuleName}|EXPOSE|0");
                bool ok = resp != null && resp.StartsWith("ACK|");
                string body = ""; var p = (resp ?? "").Split('|'); if (p.Length > 3) body = p[3];
                _lblGrab.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                _lblGrab.Text = ok ? body : (response ?? "응답 없음");
            }
            catch (Exception ex)
            {
                _lblGrab.ForeColor = Color.Firebrick;
                _lblGrab.Text = "GRAB 실패: " + ex.Message;
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
                _lblMatch.Text = "finder 이름을 입력하세요.";
                return;
            }

            _btnMatch.Enabled = false;
            _lblMatch.ForeColor = Color.DimGray;
            _lblMatch.Text = "MATCH(" + finder + ") 중...";

            try
            {
                MatchResultDto r = await _c.MatchAsync(finder);
                if (r.Success)
                {
                    _lblMatch.ForeColor = Color.SeaGreen;
                    _lblMatch.Text = string.Format(
                        "OK  x={0:F2}  y={1:F2}  t={2:F3}  score={3:F3}",
                        result.X,
                        result.Y,
                        result.AngleDeg,
                        result.Score);
                }
                else
                {
                    _lblMatch.ForeColor = Color.Firebrick;
                    _lblMatch.Text = "MATCH 실패: " + (result != null && !string.IsNullOrEmpty(result.RawError) ? result.RawError : "no match");
                }
            }
            catch (Exception ex)
            {
                _lblMatch.ForeColor = Color.Firebrick;
                _lblMatch.Text = "MATCH 실패: " + ex.Message;
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
                _lblInsp.Text = "inspector 이름을 입력하세요.";
                return;
            }

            _btnInspect.Enabled = false;
            _lblInsp.ForeColor = Color.DimGray;
            _lblInsp.Text = "INSPECT(" + inspector + ") 중...";

            try
            {
                InspectionResultDto r = await _c.InspectAsync(inspector);
                bool acked = r.Raw != null && r.Raw.StartsWith("ACK|");
                if (acked)
                {
                    _lblInsp.ForeColor = result.IsPass ? Color.SeaGreen : Color.Firebrick;
                    _lblInsp.Text = (result.IsPass ? "PASS" : "FAIL") + "   (" + result.Raw + ")";
                }
                else
                {
                    _lblInsp.ForeColor = Color.Firebrick;
                    _lblInsp.Text = "INSPECT 실패: " + (result != null ? result.Raw : "응답 없음");
                }
            }
            catch (Exception ex)
            {
                _lblInsp.ForeColor = Color.Firebrick;
                _lblInsp.Text = "INSPECT 실패: " + ex.Message;
            }
            finally
            {
                _btnInspect.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _viewer?.StopLive(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
