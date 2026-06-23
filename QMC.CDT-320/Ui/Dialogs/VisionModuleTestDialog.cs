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

        private readonly Button  _btnGrab    = new Button { Text = "GRAB",    Dock = DockStyle.Fill };
        private readonly Label   _lblGrab    = new Label  { Text = "대기",    Dock = DockStyle.Fill };
        private readonly TextBox _txtFinder  = new TextBox { Dock = DockStyle.Fill };
        private readonly Button  _btnMatch   = new Button { Text = "MATCH",   Dock = DockStyle.Fill };
        private readonly Label   _lblMatch   = new Label  { Text = "finder 입력 후 MATCH → x/y/θ/score", Dock = DockStyle.Fill };
        private readonly TextBox _txtInsp    = new TextBox { Dock = DockStyle.Fill };
        private readonly Button  _btnInspect = new Button { Text = "INSPECT", Dock = DockStyle.Fill };
        private readonly Label   _lblInsp    = new Label  { Text = "inspector 입력 후 INSPECT → PASS/FAIL", Dock = DockStyle.Fill };

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

        public VisionModuleTestDialog(VisionTcpClient client, string displayName)
        {
            _c = client;

            Text = $"VISION 동작 테스트 — {displayName}" + (client != null ? $"  (port {client.Port})" : "");
            Font = new Font("맑은 고딕", 9F);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            ClientSize = new Size(1080, 420);
            BackColor = Color.White;
            MinimumSize = new Size(900, 360);

            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 3, RowCount = 4 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            StyleCmd(_btnGrab);
            StyleCmd(_btnMatch);
            StyleCmd(_btnInspect);
            StyleResult(_lblGrab);
            StyleResult(_lblMatch);
            StyleResult(_lblInsp);
            _txtFinder.Font = new Font("맑은 고딕", 10F); _txtFinder.Margin = new Padding(3, 8, 3, 8);
            _txtInsp.Font   = new Font("맑은 고딕", 10F); _txtInsp.Margin   = new Padding(3, 8, 3, 8);

            grid.Controls.Add(_btnGrab, 0, 0);
            grid.Controls.Add(_lblGrab, 1, 0); grid.SetColumnSpan(_lblGrab, 2);
            grid.Controls.Add(_txtFinder, 0, 1);
            grid.Controls.Add(_btnMatch, 1, 1);
            grid.Controls.Add(_lblMatch, 2, 1);
            grid.Controls.Add(_txtInsp, 0, 2);
            grid.Controls.Add(_btnInspect, 1, 2);
            grid.Controls.Add(_lblInsp, 2, 2);

            var hint = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Font = new Font("맑은 고딕", 9F),
                Padding = new Padding(0, 8, 0, 0),
                Text = "Vision RUN 상태에서만 명령 수락(PING 제외). finder/inspector 는 레시피 등록 도구명."
            };
            grid.Controls.Add(hint, 0, 3); grid.SetColumnSpan(hint, 3);

            // 좌측 = 명령/결과 그리드, 우측 = 모듈별 뷰어(이미지). 명령 채널과 별개 포트.
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 470F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(grid, 0, 0);

            int viewerPort = client != null ? VisionViewerPorts.ResolveByModule(client.ModuleName) : 0;
            _viewer = new VisionViewerPanel(client != null ? client.Host : null, viewerPort, displayName + " 이미지", client)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            root.Controls.Add(_viewer, 1, 0);

            Controls.Add(root);

            _btnGrab.Click    += async (s, e) => await RunGrab();
            _btnMatch.Click   += async (s, e) => await RunMatch();
            _btnInspect.Click += async (s, e) => await RunInspect();
        }

        private static void StyleCmd(Button b)
        {
            b.BackColor = Color.FromArgb(60, 60, 60);
            b.FlatStyle = FlatStyle.Flat;
            b.ForeColor = Color.White;
            b.Font = new Font("맑은 고딕", 10.5F, FontStyle.Bold);
            b.Margin = new Padding(3, 6, 12, 6);
            b.UseVisualStyleBackColor = false;
        }

        private static void StyleResult(Label l)
        {
            l.Font = new Font("Consolas", 10F);
            l.ForeColor = Color.DimGray;
            l.TextAlign = ContentAlignment.MiddleLeft;
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
                string resp = await _c.SendAsync($"{_c.ModuleName}|EXPOSE|0", 30000);   // 고해상도 그랩 대비 30s
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
                MatchResultDto r = await _c.MatchAsync(finder, 0, 30000);
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
                InspectionResultDto r = await _c.InspectAsync(inspector, 0, 30000);
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

        // ── 비동기 매칭(2단계) — 시작(그랩→1차 ACK) 후 완료까지 폴링하고 소요시간 표시 ──
        private async Task RunMatchAsync()
        {
            if (!Ready(_lblMatchA)) return;
            string finder = (_txtFinder.Text ?? "").Trim();
            if (finder.Length == 0) { _lblMatchA.ForeColor = Color.Firebrick; _lblMatchA.Text = "finder 이름을 입력하세요"; return; }
            _btnMatchA.Enabled = false;
            try
            {
                _lblMatchA.ForeColor = Color.DimGray; _lblMatchA.Text = $"ASYNC MATCH({finder}) 시작...";
                if (!await _c.MatchAsyncStartAsync(finder))
                { _lblMatchA.ForeColor = Color.Firebrick; _lblMatchA.Text = "시작 실패(그랩/게이트 확인)"; return; }
                _lblMatchA.Text = "STARTED — 폴링 중...";
                var sw = System.Diagnostics.Stopwatch.StartNew();
                AsyncMatchPoll poll;
                do
                {
                    await Task.Delay(20);
                    poll = await _c.PollMatchResultAsync(finder);
                    if (poll.Error) { _lblMatchA.ForeColor = Color.Firebrick; _lblMatchA.Text = "실패: " + poll.Raw; return; }
                    if (sw.ElapsedMilliseconds > 30000) { _lblMatchA.ForeColor = Color.Firebrick; _lblMatchA.Text = "폴링 타임아웃(30s)"; return; }
                } while (!poll.Done);
                var r = poll.Result;
                _lblMatchA.ForeColor = Color.SeaGreen;
                _lblMatchA.Text = $"OK ({sw.ElapsedMilliseconds}ms)  x={r.X:F2} y={r.Y:F2} θ={r.AngleDeg:F3} score={r.Score:F3}";
            }
            catch (Exception ex) { _lblMatchA.ForeColor = Color.Firebrick; _lblMatchA.Text = "실패: " + ex.Message; }
            finally { _btnMatchA.Enabled = true; }
        }

        // ── 비동기 검사(2단계) — 시작 후 완료까지 폴링하고 소요시간 표시 ──
        private async Task RunInspectAsync()
        {
            if (!Ready(_lblInspA)) return;
            string insp = (_txtInsp.Text ?? "").Trim();
            if (insp.Length == 0) { _lblInspA.ForeColor = Color.Firebrick; _lblInspA.Text = "inspector 이름을 입력하세요"; return; }
            _btnInspA.Enabled = false;
            try
            {
                _lblInspA.ForeColor = Color.DimGray; _lblInspA.Text = $"ASYNC INSPECT({insp}) 시작...";
                if (!await _c.InspectAsyncStartAsync(insp))
                { _lblInspA.ForeColor = Color.Firebrick; _lblInspA.Text = "시작 실패(그랩/게이트 확인)"; return; }
                _lblInspA.Text = "STARTED — 폴링 중...";
                var sw = System.Diagnostics.Stopwatch.StartNew();
                AsyncInspectPoll poll;
                do
                {
                    await Task.Delay(20);
                    poll = await _c.PollInspectResultAsync(insp);
                    if (poll.Error) { _lblInspA.ForeColor = Color.Firebrick; _lblInspA.Text = "실패: " + poll.Raw; return; }
                    if (sw.ElapsedMilliseconds > 30000) { _lblInspA.ForeColor = Color.Firebrick; _lblInspA.Text = "폴링 타임아웃(30s)"; return; }
                } while (!poll.Done);
                var r = poll.Result;
                _lblInspA.ForeColor = r.IsPass ? Color.SeaGreen : Color.Firebrick;
                _lblInspA.Text = $"{(r.IsPass ? "PASS" : "FAIL")} ({sw.ElapsedMilliseconds}ms)  ({r.Raw})";
            }
            catch (Exception ex) { _lblInspA.ForeColor = Color.Firebrick; _lblInspA.Text = "실패: " + ex.Message; }
            finally { _btnInspA.Enabled = true; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _viewer?.StopLive(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
