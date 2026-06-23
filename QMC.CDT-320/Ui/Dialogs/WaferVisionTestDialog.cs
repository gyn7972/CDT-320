using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// Wafer Vision 전용 동작 테스트 팝업 — 버튼 전용 단계별 테스트(좌) + Wafer 뷰어 이미지(우).
    /// UI 배치는 Designer(.Designer.cs)에 있고, 여기에는 어댑터 호출/이벤트/뷰어 Configure 등 로직만 둔다.
    /// 각 버튼은 시퀀서(InputStageAlignSequence)와 동일한 <see cref="WaferVisionAdapter"/>를 호출(모션 구동 없음).
    /// </summary>
    public sealed partial class WaferVisionTestDialog : Form
    {
        private readonly WaferVisionAdapter _adapter = new WaferVisionAdapter();

        /// <summary>Wafer Vision 테스트 팝업을 연다.</summary>
        public static void Open(IWin32Window owner)
        {
            using (var dlg = new WaferVisionTestDialog())
                dlg.ShowDialog(owner);
        }

        public WaferVisionTestDialog()
        {
            InitializeComponent();

            int port = VisionHub.Wafer != null ? VisionHub.Wafer.Port : 0;
            if (port > 0)
                Text = "VISION 동작 테스트 — Wafer Vision  (명령 " + port + " / 이미지 " + VisionViewerPorts.Wafer + ")";

            _viewer.Configure(VisionHub.Host, VisionViewerPorts.Wafer, "Wafer 이미지", VisionHub.Wafer);

            _btnExpose.Click   += async (s, e) => await RunExpose();
            _btnCenter.Click   += async (s, e) => await RunAlign("Center", _btnCenter, _lblCenter);
            _btnRef1.Click     += async (s, e) => await RunAlign("Ref1",   _btnRef1,   _lblRef1);
            _btnRef2.Click     += async (s, e) => await RunAlign("Ref2",   _btnRef2,   _lblRef2);
            _btnDieCheck.Click += async (s, e) => await RunDieCheck();

            RefreshSummary();
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
                    try { _viewer?.StartLive(); } catch { }
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
                if (last == null) { _lblSummary.Text = "저장된 결과 없음"; return; }
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
