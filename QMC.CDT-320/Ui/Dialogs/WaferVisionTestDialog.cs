using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Wafer Vision 전용 동작 테스트 팝업 — <b>입력 없이 버튼만</b>으로 단계별 테스트(좌측) +
    /// Wafer 뷰어 스트림 <b>이미지 표시</b>(우측).
    /// <para>
    /// 각 버튼은 시퀀서(InputStageAlignSequence)가 호출하는 것과 동일한
    /// <see cref="WaferVisionAdapter"/> 메소드를 그대로 호출한다(수동 == 시퀀서 경로).
    /// 결과는 <see cref="WaferVisionResultStore"/>에 기록되어 시퀀서/리포트가 공유한다.
    /// 동작 테스트는 Vision 명령(EXPOSE/MATCH)만 보내며 <b>모션을 구동하지 않는다</b>.
    /// 이미지는 명령 채널과 별개인 뷰어 포트(Wafer=5200)에서 받아 표시한다. 코드 전용 폼(Designer 없음).
    /// </summary>
    public sealed class WaferVisionTestDialog : Form
    {
        private readonly WaferVisionAdapter _adapter = new WaferVisionAdapter();

        private readonly Button _btnExpose   = new Button();
        private readonly Button _btnCenter   = new Button();
        private readonly Button _btnRef1     = new Button();
        private readonly Button _btnRef2     = new Button();
        private readonly Button _btnDieCheck = new Button();

        private readonly Label _lblExpose   = new Label();
        private readonly Label _lblCenter   = new Label();
        private readonly Label _lblRef1     = new Label();
        private readonly Label _lblRef2     = new Label();
        private readonly Label _lblDieCheck = new Label();
        private readonly Label _lblSummary  = new Label();

        private VisionViewerPanel _viewer;

        /// <summary>Wafer Vision 테스트 팝업을 연다.</summary>
        public static void Open(IWin32Window owner)
        {
            using (var dlg = new WaferVisionTestDialog())
                dlg.ShowDialog(owner);
        }

        public WaferVisionTestDialog()
        {
            int port = VisionHub.Wafer != null ? VisionHub.Wafer.Port : 0;
            Text = "VISION 동작 테스트 — Wafer Vision" +
                   (port > 0 ? $"  (명령 {port} / 이미지 {VisionViewerPorts.Wafer})" : "");
            Font = new Font("맑은 고딕", 9F);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            ClientSize = new Size(1080, 600);
            BackColor = Color.White;
            MinimumSize = new Size(900, 500);

            // SplitContainer 대신 2열 TableLayoutPanel (SplitterDistance 제약 회피).
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            root.Controls.Add(BuildControlGrid(), 0, 0);

            _viewer = new VisionViewerPanel(VisionHub.Host, VisionViewerPorts.Wafer, "Wafer 이미지", VisionHub.Wafer)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            root.Controls.Add(_viewer, 1, 0);

            Controls.Add(root);

            _btnExpose.Click   += async (s, e) => await RunExpose();
            _btnCenter.Click   += async (s, e) => await RunAlign("Center", _btnCenter, _lblCenter);
            _btnRef1.Click     += async (s, e) => await RunAlign("Ref1",   _btnRef1,   _lblRef1);
            _btnRef2.Click     += async (s, e) => await RunAlign("Ref2",   _btnRef2,   _lblRef2);
            _btnDieCheck.Click += async (s, e) => await RunDieCheck();

            RefreshSummary();
        }

        private Control BuildControlGrid()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 7
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 5; i++)
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F)); // summary
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // hint

            ConfigCmd(_btnExpose,   "① GRAB (EXPOSE)");
            ConfigCmd(_btnCenter,   "② ALIGN: Center");
            ConfigCmd(_btnRef1,     "③ ALIGN: Ref1");
            ConfigCmd(_btnRef2,     "④ ALIGN: Ref2");
            ConfigCmd(_btnDieCheck, "⑤ DIE CHECK");

            ConfigResult(_lblExpose,   "노출 트리거 → ACK 대기");
            ConfigResult(_lblCenter,   "센터 마크 정렬 → dx/dy/θ/pitch");
            ConfigResult(_lblRef1,     "기준1 마크 정렬 → dx/dy/θ/pitch");
            ConfigResult(_lblRef2,     "기준2 마크 정렬 → dx/dy/θ/pitch");
            ConfigResult(_lblDieCheck, "다이 검출 → OK/NG");

            grid.Controls.Add(_btnExpose,   0, 0); grid.Controls.Add(_lblExpose,   1, 0);
            grid.Controls.Add(_btnCenter,   0, 1); grid.Controls.Add(_lblCenter,   1, 1);
            grid.Controls.Add(_btnRef1,     0, 2); grid.Controls.Add(_lblRef1,     1, 2);
            grid.Controls.Add(_btnRef2,     0, 3); grid.Controls.Add(_lblRef2,     1, 3);
            grid.Controls.Add(_btnDieCheck, 0, 4); grid.Controls.Add(_lblDieCheck, 1, 4);

            _lblSummary.Dock = DockStyle.Fill;
            _lblSummary.Font = new Font("Consolas", 10F, FontStyle.Bold);
            _lblSummary.ForeColor = Color.FromArgb(40, 40, 40);
            _lblSummary.TextAlign = ContentAlignment.MiddleLeft;
            _lblSummary.BorderStyle = BorderStyle.FixedSingle;
            _lblSummary.Padding = new Padding(8, 0, 0, 0);
            grid.Controls.Add(_lblSummary, 0, 5); grid.SetColumnSpan(_lblSummary, 2);

            var hint = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                Font = new Font("맑은 고딕", 9F),
                Padding = new Padding(0, 8, 0, 0),
                Text = "버튼은 시퀀서와 동일한 Wafer 어댑터를 호출합니다(모션 구동 없음).\n" +
                       "Vision은 촬상·검출 결과만 반환. 결과는 자동으로 결과 저장소에 기록됩니다."
            };
            grid.Controls.Add(hint, 0, 6); grid.SetColumnSpan(hint, 2);
            return grid;
        }

        private static void ConfigCmd(Button b, string text)
        {
            b.Text = text;
            b.Dock = DockStyle.Fill;
            b.BackColor = Color.FromArgb(60, 60, 60);
            b.FlatStyle = FlatStyle.Flat;
            b.ForeColor = Color.White;
            b.Font = new Font("맑은 고딕", 10.5F, FontStyle.Bold);
            b.Margin = new Padding(3, 6, 12, 6);
            b.UseVisualStyleBackColor = false;
        }

        private static void ConfigResult(Label l, string placeholder)
        {
            l.Dock = DockStyle.Fill;
            l.Text = placeholder;
            l.Font = new Font("Consolas", 10F);
            l.ForeColor = Color.DimGray;
            l.TextAlign = ContentAlignment.MiddleLeft;
        }

        private bool Ready(Label target)
        {
            var c = VisionHub.Wafer;
            if (c == null) { target.ForeColor = Color.Firebrick; target.Text = "Wafer 모듈 없음"; return false; }
            if (!c.IsConnected) { target.ForeColor = Color.Firebrick; target.Text = $"미연결 (port {c.Port}) — 먼저 연결하세요"; return false; }
            return true;
        }

        private async Task RunExpose()
        {
            if (!Ready(_lblExpose)) return;
            _btnExpose.Enabled = false;
            _lblExpose.ForeColor = Color.DimGray; _lblExpose.Text = "GRAB 중...";
            try
            {
                bool ok = await _adapter.TriggerExposeAsync(0);
                _lblExpose.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                bool stillConn = VisionHub.Wafer != null && VisionHub.Wafer.IsConnected;
                _lblExpose.Text = ok
                    ? "EXPOSE ACK (노출 완료)"
                    : (stillConn ? "EXPOSE 거부 — Vision 측 READY 완료/RUN 아님(Vision에서 READY→RUN 후 가능)" : "EXPOSE 실패 — 미연결");
                // 촬상 성공 시, 그 프레임을 보려면 뷰어(5200)가 연결돼 있어야 한다 → Live 시작(클릭이 트리거).
                if (ok) { try { _viewer?.StartLive(); } catch { } }
            }
            catch (Exception ex)
            {
                _lblExpose.ForeColor = Color.Firebrick; _lblExpose.Text = "GRAB 실패: " + ex.Message;
            }
            finally { _btnExpose.Enabled = true; }
        }

        private async Task RunAlign(string targetId, Button btn, Label lbl)
        {
            if (!Ready(lbl)) return;
            btn.Enabled = false;
            lbl.ForeColor = Color.DimGray; lbl.Text = $"ALIGN({targetId}) 중...";
            try
            {
                VisionAlignResult r = await _adapter.TriggerAlignAsync(targetId);
                if (r != null)
                {
                    lbl.ForeColor = Color.SeaGreen;
                    lbl.Text = $"OK  dx={r.DeltaX:F4}  dy={r.DeltaY:F4}  θ={r.DeltaTheta:F4}  pitch={r.PitchX:F4}/{r.PitchY:F4}";
                }
                else
                {
                    lbl.ForeColor = Color.Firebrick;
                    lbl.Text = "ALIGN 실패 (매칭 없음 / 미연결)";
                }
            }
            catch (Exception ex)
            {
                lbl.ForeColor = Color.Firebrick; lbl.Text = "ALIGN 실패: " + ex.Message;
            }
            finally
            {
                btn.Enabled = true;
                RefreshSummary();
            }
        }

        private async Task RunDieCheck()
        {
            if (!Ready(_lblDieCheck)) return;
            _btnDieCheck.Enabled = false;
            _lblDieCheck.ForeColor = Color.DimGray; _lblDieCheck.Text = "DIE CHECK 중...";
            try
            {
                bool ok = await _adapter.GetResultAsync(0);
                _lblDieCheck.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                _lblDieCheck.Text = ok ? "OK (score ≥ threshold)" : "NG / 미연결";
            }
            catch (Exception ex)
            {
                _lblDieCheck.ForeColor = Color.Firebrick; _lblDieCheck.Text = "DIE CHECK 실패: " + ex.Message;
            }
            finally
            {
                _btnDieCheck.Enabled = true;
                RefreshSummary();
            }
        }

        private void RefreshSummary()
        {
            try
            {
                WaferVisionInspectionResult last = WaferVisionResultStore.LastInspection;
                string dieCheck = WaferVisionResultStore.LastDieCheckTime == DateTime.MinValue
                    ? "-"
                    : (WaferVisionResultStore.LastDieCheckOk ? "OK" : "NG");
                if (last == null)
                {
                    _lblSummary.Text = "저장된 결과 없음";
                    return;
                }
                _lblSummary.Text =
                    $"저장됨 ▶ DieOffset X={last.DieOffsetX:F4}  Y={last.DieOffsetY:F4}  R={last.DieRotation:F4}   |   DieCheck={dieCheck}";
            }
            catch (Exception ex)
            {
                _lblSummary.Text = "요약 표시 실패: " + ex.Message;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _viewer?.StopLive(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
