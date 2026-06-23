using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Head(측면/바텀) 비전 동작 테스트 — <b>입력 없이 버튼만</b>.
    /// <para>Bottom Inspection: 시퀀서(PickerUnit)와 동일한 <see cref="TpuVisionAdapter"/>(Inspection 5101/5201, 4픽커 DieFinder) 호출.</para>
    /// <para>Top/Bottom Side: 각 측면 모듈 자체 포트(TopSide 5105/뷰어 5205, BottomSide 5106/5206)로
    /// EXPOSE + INSPECT(해당 모듈 Surface 인스펙터). 검출된 결함은 영상에 박스로 표시.</para>
    /// 모션 없음. 코드 전용 폼.
    /// </summary>
    public sealed class TpuVisionTestDialog : Form
    {
        public enum Mode { BottomInspection, Side }

        private readonly TpuVisionAdapter _adapter = new TpuVisionAdapter();
        private readonly Mode _mode;
        private readonly int  _pickerNo;
        private readonly Func<VisionTcpClient> _sideClientGetter;
        private readonly int    _sideViewerPort;
        private readonly string _sideInspectorId;

        private readonly Button _btnExpose = new Button();
        private readonly Button _btnResult = new Button();
        private readonly Label  _lblExpose = new Label();
        private readonly Label  _lblResult = new Label();
        private VisionViewerPanel _viewer;

        public static void Open(IWin32Window owner, string title, Mode mode,
            int pickerNo = 1, Func<VisionTcpClient> sideClient = null, int sideViewerPort = 0, string sideInspectorId = null)
        {
            using (var dlg = new TpuVisionTestDialog(title, mode, pickerNo, sideClient, sideViewerPort, sideInspectorId))
                dlg.ShowDialog(owner);
        }

        /// <summary>Front/Rear Head 페이지 하단 Action 컨테이너에 버튼 전용 Head 비전 런처 3개 추가.</summary>
        public static void AddLaunchers(Control.ControlCollection actions, IWin32Window owner, Control stopButton)
        {
            if (actions == null) return;
            void Add(string label, Action open)
            {
                var b = new ActionButton { Text = label, Width = 132, Height = 60, Margin = new Padding(6), Font = new Font("맑은 고딕", 11F) };
                b.Click += (s, e) => open();
                actions.Add(b);
            }
            Add("VISION: BOTTOM INSP", () => Open(owner, "Bottom Inspection", Mode.BottomInspection));
            Add("VISION: TOP SIDE",    () => Open(owner, "Top Side",    Mode.Side, 1, () => VisionHub.TopSide,    VisionViewerPorts.TopSide,    "TopSurfaceInspector"));
            Add("VISION: BOTTOM SIDE", () => Open(owner, "Bottom Side", Mode.Side, 1, () => VisionHub.BottomSide, VisionViewerPorts.BottomSide, "BottomSurfaceInspector"));
            if (stopButton != null && actions.Contains(stopButton))
                actions.SetChildIndex(stopButton, actions.Count - 1);
        }

        public TpuVisionTestDialog(string title, Mode mode, int pickerNo, Func<VisionTcpClient> sideClient, int sideViewerPort, string sideInspectorId)
        {
            _mode = mode; _pickerNo = pickerNo;
            _sideClientGetter = sideClient; _sideViewerPort = sideViewerPort; _sideInspectorId = sideInspectorId;

            var cmdClient = ActiveClient();
            int cmdPort   = cmdClient != null ? cmdClient.Port : 0;
            int viewerPort = _mode == Mode.BottomInspection ? VisionViewerPorts.BottomInspection : _sideViewerPort;
            Text = $"VISION 동작 테스트 — {title}" + (cmdPort > 0 ? $"  (명령 {cmdPort} / 이미지 {viewerPort})" : "");
            Font = new Font("맑은 고딕", 9F);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            ClientSize = new Size(1080, 560);
            BackColor = Color.White;
            MinimumSize = new Size(900, 460);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(BuildControlGrid(), 0, 0);

            _viewer = new VisionViewerPanel(VisionHub.Host, viewerPort, title + " 이미지", cmdClient)
            { Dock = DockStyle.Fill, Margin = new Padding(0) };
            root.Controls.Add(_viewer, 1, 0);
            Controls.Add(root);

            _btnExpose.Click += async (s, e) => await RunExpose();
            _btnResult.Click += async (s, e) => await RunResult();
            this.Shown += (s, e) => { try { _viewer?.StartLive(); } catch { } };
        }

        /// <summary>현재 모드의 명령 클라이언트 — Bottom=Inspection, Side=해당 측면 모듈.</summary>
        private VisionTcpClient ActiveClient()
            => _mode == Mode.BottomInspection ? VisionHub.Inspection : (_sideClientGetter != null ? _sideClientGetter() : null);

        private Control BuildControlGrid()
        {
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 4 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            bool bottom = _mode == Mode.BottomInspection;
            ConfigCmd(_btnExpose, bottom ? "① BOTTOM EXPOSE" : "① SIDE EXPOSE");
            ConfigCmd(_btnResult, bottom ? "② BOTTOM RESULT" : "② SIDE INSPECT");
            ConfigResult(_lblExpose, bottom ? $"픽커{_pickerNo} 노출 트리거 → ACK" : "노출 트리거 → ACK");
            ConfigResult(_lblResult, bottom ? "4 픽커 DieFinder → offset/OK" : (_sideInspectorId + " → 결함 박스(영상)/판정"));
            _lblResult.AutoSize = false; _lblResult.TextAlign = ContentAlignment.TopLeft;

            grid.Controls.Add(_btnExpose, 0, 0); grid.Controls.Add(_lblExpose, 1, 0);
            grid.Controls.Add(_btnResult, 0, 1); grid.Controls.Add(_lblResult, 1, 1);
            grid.SetRowSpan(_lblResult, 2);

            var hint = new Label
            {
                Dock = DockStyle.Fill, ForeColor = Color.DimGray, Font = new Font("맑은 고딕", 9F), Padding = new Padding(0, 8, 0, 0),
                Text = bottom
                    ? "Bottom 은 시퀀서(PickerUnit)와 동일한 TpuVisionAdapter 호출(Inspection 채널, 모션 없음)."
                    : "Side 는 해당 측면 모듈 자체 포트로 EXPOSE+INSPECT. 검출 결함은 영상에 박스로 표시. Vision READY 상태에서만 수락."
            };
            grid.Controls.Add(hint, 0, 3); grid.SetColumnSpan(hint, 2);
            return grid;
        }

        private static void ConfigCmd(Button b, string text)
        {
            b.Text = text; b.Dock = DockStyle.Fill;
            b.BackColor = Color.FromArgb(60, 60, 60); b.FlatStyle = FlatStyle.Flat; b.ForeColor = Color.White;
            b.Font = new Font("맑은 고딕", 10.5F, FontStyle.Bold); b.Margin = new Padding(3, 6, 12, 6);
            b.UseVisualStyleBackColor = false;
        }

        private static void ConfigResult(Label l, string placeholder)
        {
            l.Dock = DockStyle.Fill; l.Text = placeholder; l.Font = new Font("Consolas", 10F);
            l.ForeColor = Color.DimGray; l.TextAlign = ContentAlignment.MiddleLeft;
        }

        private bool Ready(Label target)
        {
            var c = ActiveClient();
            if (c == null) { target.ForeColor = Color.Firebrick; target.Text = "모듈 없음"; return false; }
            if (!c.IsConnected) { target.ForeColor = Color.Firebrick; target.Text = $"미연결 (port {c.Port}) — 먼저 연결하세요"; return false; }
            return true;
        }

        private async Task RunExpose()
        {
            if (!Ready(_lblExpose)) return;
            _btnExpose.Enabled = false;
            _lblExpose.ForeColor = Color.DimGray; _lblExpose.Text = "EXPOSE 중...";
            try
            {
                bool ok;
                if (_mode == Mode.BottomInspection)
                    ok = await _adapter.TriggerBottomExposeAsync(_pickerNo, 30000);
                else
                {
                    var c = ActiveClient();
                    ok = c != null && await c.ExposeAsync(0, 30000, CancellationToken.None);
                }
                _lblExpose.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                _lblExpose.Text = ok ? "EXPOSE ACK (노출 완료)" : "EXPOSE 거부/실패 — Vision READY/연결 확인";
                if (ok) { try { _viewer?.StartLive(); } catch { } }
            }
            catch (Exception ex) { _lblExpose.ForeColor = Color.Firebrick; _lblExpose.Text = "EXPOSE 실패: " + ex.Message; }
            finally { _btnExpose.Enabled = true; }
        }

        private async Task RunResult()
        {
            if (!Ready(_lblResult)) return;
            _btnResult.Enabled = false;
            _lblResult.ForeColor = Color.DimGray;
            _lblResult.Text = (_mode == Mode.BottomInspection ? "RESULT" : "INSPECT") + " 중...";
            try
            {
                if (_mode == Mode.BottomInspection)
                {
                    var rs = await _adapter.GetBottomResultsAsync(30000);
                    if (rs == null) { _lblResult.ForeColor = Color.Firebrick; _lblResult.Text = "결과 없음 / 미연결"; return; }
                    var lines = new System.Collections.Generic.List<string>();
                    bool allOk = true;
                    foreach (var o in rs)
                    {
                        if (o == null) continue;
                        if (!o.IsOk) allOk = false;
                        lines.Add($"P{o.PickerNo}: {(o.IsOk ? "OK" : "NG")}  x={o.OffsetX:F2} y={o.OffsetY:F2}");
                    }
                    _lblResult.ForeColor = allOk ? Color.SeaGreen : Color.Firebrick;
                    _lblResult.Text = string.Join("\r\n", lines);
                    try { _viewer?.SetVerdictText(allOk ? "OK" : "NG", allOk); _viewer?.SetResultLines(lines.ToArray()); } catch { }
                }
                else
                {
                    var c = ActiveClient();
                    var r = await c.InspectAsync(_sideInspectorId, 0, 30000);
                    bool acked = r != null && r.Raw != null && r.Raw.StartsWith("ACK|");
                    if (!acked) { _lblResult.ForeColor = Color.Firebrick; _lblResult.Text = "INSPECT 실패: " + (r?.Raw ?? "no response"); return; }
                    _lblResult.ForeColor = r.IsPass ? Color.SeaGreen : Color.Firebrick;
                    _lblResult.Text = $"{(r.IsPass ? "PASS" : "FAIL")}\r\n({r.Raw})";
                    // 검출된 결함 박스는 영상(뷰어)에 메타로 자동 표시. 판정만 영상 우측상단에.
                    try { _viewer?.SetVerdictText(r.IsPass ? "OK" : "NG", r.IsPass); } catch { }
                }
            }
            catch (Exception ex)
            {
                _lblResult.ForeColor = Color.Firebrick;
                _lblResult.Text = (_mode == Mode.BottomInspection ? "RESULT" : "INSPECT") + " 실패: " + ex.Message;
            }
            finally { _btnResult.Enabled = true; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _viewer?.StopLive(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
