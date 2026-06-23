using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Vision 모듈 1개에 대한 동작 테스트 팝업 — 운전 패널 상태의 GRAB/MATCH/INSPECT 패널과 동일 구성.
    /// 작업정보 각 페이지(INPUT STAGE/OUTPUT STAGE/FRONT·REAR HEAD) 하단 Action 버튼에서 모듈별로 띄운다.
    /// 핸들러=명령 마스터: 선택 모듈에 EXPOSE/MATCH/INSPECT 전송 후 Vision 응답 데이터를 파싱·표시한다.
    /// 코드 전용 폼(Designer 없음).
    /// </summary>
    public sealed class VisionModuleTestDialog : Form
    {
        private readonly VisionTcpClient _c;
        private VisionViewerPanel _viewer;

        private readonly Button  _btnGrab    = new Button { Text = "GRAB",    Dock = DockStyle.Fill };
        private readonly Label   _lblGrab    = new Label  { Text = "대기",    Dock = DockStyle.Fill };
        private readonly TextBox _txtFinder  = new TextBox { Dock = DockStyle.Fill };
        private readonly Button  _btnMatch   = new Button { Text = "MATCH",   Dock = DockStyle.Fill };
        private readonly Label   _lblMatch   = new Label  { Text = "finder 입력 후 MATCH → x/y/θ/score", Dock = DockStyle.Fill };
        private readonly TextBox _txtInsp    = new TextBox { Dock = DockStyle.Fill };
        private readonly Button  _btnInspect = new Button { Text = "INSPECT", Dock = DockStyle.Fill };
        private readonly Label   _lblInsp    = new Label  { Text = "inspector 입력 후 INSPECT → PASS/FAIL", Dock = DockStyle.Fill };

        /// <summary>모듈 1개에 대한 테스트 팝업을 연다.</summary>
        public static void Open(IWin32Window owner, VisionTcpClient client, string displayName)
        {
            using (var dlg = new VisionModuleTestDialog(client, displayName))
                dlg.ShowDialog(owner);
        }

        /// <summary>
        /// 작업정보 페이지 하단 Action 컨테이너에 모듈별 'VISION: ...' 버튼을 추가한다.
        /// 기존 ActionButton 과 동일 스타일. 클릭 시 해당 모듈로 <see cref="Open"/>. STOP 버튼은 맨 끝 유지.
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
