using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Vision 모듈 1개 동작 테스트 팝업 — GRAB/MATCH/INSPECT(좌) + 모듈 뷰어 이미지(우).
    /// UI 배치는 Designer(.Designer.cs)에 있고, 여기에는 명령 호출/이벤트/뷰어 Configure 로직만 둔다.
    /// 생성자는 매개변수 없음(VS 디자이너 호환). 사용 시 <see cref="Init"/>로 명령 클라이언트/표시명을 주입한다.
    /// </summary>
    public sealed partial class VisionModuleTestDialog : Form
    {
        private VisionTcpClient _c;

        public VisionModuleTestDialog()
        {
            InitializeComponent();
            _btnGrab.Click    += async (s, e) => await RunGrab();
            _btnMatch.Click   += async (s, e) => await RunMatch();
            _btnInspect.Click += async (s, e) => await RunInspect();
        }

        /// <summary>런타임 주입 — 명령 채널/표시명 설정 후 뷰어 Configure.</summary>
        public void Init(VisionTcpClient client, string displayName)
        {
            _c = client;
            Text = $"VISION 동작 테스트 — {displayName}" + (client != null ? $"  (port {client.Port})" : "");
            int viewerPort = client != null ? VisionViewerPorts.ResolveByModule(client.ModuleName) : 0;
            _viewer.Configure(client != null ? client.Host : null, viewerPort, displayName + " 이미지", client);
        }

        /// <summary>모듈 1개에 대한 테스트 팝업을 연다.</summary>
        public static void Open(IWin32Window owner, VisionTcpClient client, string displayName)
        {
            using (var dlg = new VisionModuleTestDialog())
            {
                dlg.Init(client, displayName);
                dlg.ShowDialog(owner);
            }
        }

        /// <summary>
        /// 작업정보 페이지 하단 Action 컨테이너에 모듈별 'VISION: ...' 버튼을 추가한다(기존 ActionButton 스타일).
        /// 클릭 시 해당 모듈로 <see cref="Open"/>. STOP 버튼은 맨 끝 유지.
        /// </summary>
        public static void AddLaunchers(
            Control.ControlCollection actions, IWin32Window owner, Control stopButton,
            params Tuple<string, Func<VisionTcpClient>, string>[] mods)
        {
            if (actions == null || mods == null) return;
            foreach (var m in mods)
            {
                var b = new QMC.CDT_320.Ui.Controls.ActionButton
                {
                    Text   = m.Item1,
                    Width  = 180,
                    Height = 64,
                    Margin = new Padding(6),
                    Font   = new Font("맑은 고딕", 11F)
                };
                var getter = m.Item2;
                var disp   = m.Item3;
                b.Click += (s, e) => Open(owner, getter(), disp);
                actions.Add(b);
            }
            if (stopButton != null && actions.Contains(stopButton))
                actions.SetChildIndex(stopButton, actions.Count - 1);
        }

        private bool Ready(Label target)
        {
            if (_c == null) { target.ForeColor = Color.Firebrick; target.Text = "모듈 없음"; return false; }
            if (!_c.IsConnected) { target.ForeColor = Color.Firebrick; target.Text = $"미연결 (port {_c.Port}) — 먼저 연결하세요"; return false; }
            return true;
        }

        private async Task RunGrab()
        {
            if (!Ready(_lblGrab)) return;
            _btnGrab.Enabled = false;
            _lblGrab.ForeColor = Color.DimGray; _lblGrab.Text = "GRAB 중...";
            try
            {
                string resp = await _c.SendAsync($"{_c.ModuleName}|EXPOSE|0");
                bool ok = resp != null && resp.StartsWith("ACK|");
                string body = ""; var p = (resp ?? "").Split('|'); if (p.Length > 3) body = p[3];
                _lblGrab.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                _lblGrab.Text = ok ? body : (resp ?? "no response");
            }
            catch (Exception ex)
            {
                _lblGrab.ForeColor = Color.Firebrick; _lblGrab.Text = "GRAB 실패: " + ex.Message;
            }
            finally { _btnGrab.Enabled = true; }
        }

        private async Task RunMatch()
        {
            if (!Ready(_lblMatch)) return;
            string finder = (_txtFinder.Text ?? "").Trim();
            if (finder.Length == 0) { _lblMatch.ForeColor = Color.Firebrick; _lblMatch.Text = "finder 이름을 입력하세요"; return; }
            _btnMatch.Enabled = false;
            _lblMatch.ForeColor = Color.DimGray; _lblMatch.Text = $"MATCH({finder}) 중...";
            try
            {
                MatchResultDto r = await _c.MatchAsync(finder);
                if (r.Success)
                {
                    _lblMatch.ForeColor = Color.SeaGreen;
                    _lblMatch.Text = $"OK  x={r.X:F2}  y={r.Y:F2}  θ={r.AngleDeg:F3}  score={r.Score:F3}";
                }
                else
                {
                    _lblMatch.ForeColor = Color.Firebrick;
                    _lblMatch.Text = "MATCH 실패: " + (string.IsNullOrEmpty(r.RawError) ? "no match" : r.RawError);
                }
            }
            catch (Exception ex)
            {
                _lblMatch.ForeColor = Color.Firebrick; _lblMatch.Text = "MATCH 실패: " + ex.Message;
            }
            finally { _btnMatch.Enabled = true; }
        }

        private async Task RunInspect()
        {
            if (!Ready(_lblInsp)) return;
            string inspector = (_txtInsp.Text ?? "").Trim();
            if (inspector.Length == 0) { _lblInsp.ForeColor = Color.Firebrick; _lblInsp.Text = "inspector 이름을 입력하세요"; return; }
            _btnInspect.Enabled = false;
            _lblInsp.ForeColor = Color.DimGray; _lblInsp.Text = $"INSPECT({inspector}) 중...";
            try
            {
                InspectionResultDto r = await _c.InspectAsync(inspector);
                bool acked = r.Raw != null && r.Raw.StartsWith("ACK|");
                if (acked)
                {
                    _lblInsp.ForeColor = r.IsPass ? Color.SeaGreen : Color.Firebrick;
                    _lblInsp.Text = $"{(r.IsPass ? "PASS" : "FAIL")}   ({r.Raw})";
                }
                else
                {
                    _lblInsp.ForeColor = Color.Firebrick;
                    _lblInsp.Text = "INSPECT 실패: " + r.Raw;
                }
            }
            catch (Exception ex)
            {
                _lblInsp.ForeColor = Color.Firebrick; _lblInsp.Text = "INSPECT 실패: " + ex.Message;
            }
            finally { _btnInspect.Enabled = true; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _viewer?.StopLive(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
