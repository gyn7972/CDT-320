using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;

namespace QMC.CDT_320.Ui.Controls
{
    public sealed partial class WaferVisionTestControl : UserControl
    {
        private readonly WaferVisionAdapter _adapter = new WaferVisionAdapter();

        public WaferVisionTestControl()
        {
            InitializeComponent();

            btnExpose.Click += async (s, e) => await RunExposeAsync().ConfigureAwait(true);
            btnCenter.Click += async (s, e) => await RunAlignAsync("Center", btnCenter, lblCenter).ConfigureAwait(true);
            btnRef1.Click += async (s, e) => await RunAlignAsync("Ref1", btnRef1, lblRef1).ConfigureAwait(true);
            btnRef2.Click += async (s, e) => await RunAlignAsync("Ref2", btnRef2, lblRef2).ConfigureAwait(true);
            btnDieCheck.Click += async (s, e) => await RunDieCheckAsync().ConfigureAwait(true);
        }

        public void Configure()
        {
            viewer.Configure(VisionHub.Host, VisionViewerPorts.Wafer, "Wafer Image", VisionHub.Wafer);
            RefreshSummary();
        }

        public void StopLive()
        {
            try { viewer.StopLive(); } catch { }
        }

        private bool Ready(Label target)
        {
            VisionTcpClient client = VisionHub.Wafer;
            if (client == null)
            {
                target.ForeColor = Color.Firebrick;
                target.Text = "Wafer Vision 모듈이 없습니다.";
                return false;
            }

            if (!client.IsConnected)
            {
                target.ForeColor = Color.Firebrick;
                target.Text = "Wafer Vision이 연결되지 않았습니다. port=" + client.Port;
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
            lblExpose.Text = "GRAB 실행 중...";
            try
            {
                bool ok = await _adapter.TriggerExposeAsync(0).ConfigureAwait(true);
                lblExpose.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                lblExpose.Text = ok ? "EXPOSE ACK 완료" : "EXPOSE 실패. Vision READY/연결 상태를 확인하세요.";
                if (ok)
                    TryStartLive();
            }
            catch (Exception ex)
            {
                lblExpose.ForeColor = Color.Firebrick;
                lblExpose.Text = "GRAB 실패: " + ex.Message;
            }
            finally
            {
                btnExpose.Enabled = true;
            }
        }

        private async Task RunAlignAsync(string targetId, Button button, Label label)
        {
            if (!Ready(label))
                return;

            button.Enabled = false;
            label.ForeColor = Color.DimGray;
            label.Text = "ALIGN(" + targetId + ") 실행 중...";
            try
            {
                VisionAlignResult result = await _adapter.TriggerAlignAsync(targetId).ConfigureAwait(true);
                if (result != null)
                {
                    label.ForeColor = Color.SeaGreen;
                    label.Text = "OK  dx=" + result.DeltaX.ToString("F4") +
                                 "  dy=" + result.DeltaY.ToString("F4") +
                                 "  t=" + result.DeltaTheta.ToString("F4") +
                                 "  pitch=" + result.PitchX.ToString("F4") + "/" + result.PitchY.ToString("F4");
                    TryStartLive();
                }
                else
                {
                    label.ForeColor = Color.Firebrick;
                    label.Text = "ALIGN 실패. 결과가 없습니다.";
                }
            }
            catch (Exception ex)
            {
                label.ForeColor = Color.Firebrick;
                label.Text = "ALIGN 실패: " + ex.Message;
            }
            finally
            {
                button.Enabled = true;
                RefreshSummary();
            }
        }

        private async Task RunDieCheckAsync()
        {
            if (!Ready(lblDieCheck))
                return;

            btnDieCheck.Enabled = false;
            lblDieCheck.ForeColor = Color.DimGray;
            lblDieCheck.Text = "DIE CHECK 실행 중...";
            try
            {
                bool ok = await _adapter.GetResultAsync(0).ConfigureAwait(true);
                lblDieCheck.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                lblDieCheck.Text = ok ? "OK" : "NG";
            }
            catch (Exception ex)
            {
                lblDieCheck.ForeColor = Color.Firebrick;
                lblDieCheck.Text = "DIE CHECK 실패: " + ex.Message;
            }
            finally
            {
                btnDieCheck.Enabled = true;
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
                    lblSummary.Text = "저장된 결과 없음";
                    return;
                }

                lblSummary.Text =
                    "저장됨: DieOffset X=" + last.DieOffsetX.ToString("F4") +
                    "  Y=" + last.DieOffsetY.ToString("F4") +
                    "  R=" + last.DieRotation.ToString("F4") +
                    "   |   DieCheck=" + dieCheck;
            }
            catch (Exception ex)
            {
                lblSummary.Text = "요약 표시 실패: " + ex.Message;
            }
        }

        private void TryStartLive()
        {
            try { viewer.StartLive(); } catch { }
        }
    }
}
