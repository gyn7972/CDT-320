using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
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
        }

        private async void btnExpose_Click(object sender, EventArgs e)
        {
            await RunExposeAsync().ConfigureAwait(true);
        }

        private async void btnCenterMatchAsync_Click(object sender, EventArgs e)
        {
            await RunMatchAsyncRequestAsync(
                VisionAlignTargetIds.Center,
                lblCenterMatchAsync).ConfigureAwait(true);
        }

        private async void btnCenterMatchResult_Click(object sender, EventArgs e)
        {
            await RunAlignMatchAsyncAndResultAsync(
                VisionAlignTargetIds.Center,
                lblCenterMatchResult).ConfigureAwait(true);
        }

        private async void btnRef1MatchAsync_Click(object sender, EventArgs e)
        {
            await RunMatchAsyncRequestAsync(
                VisionAlignTargetIds.Ref1,
                lblRef1MatchAsync).ConfigureAwait(true);
        }

        private async void btnRef1MatchResult_Click(object sender, EventArgs e)
        {
            await RunAlignMatchAsyncAndResultAsync(
                VisionAlignTargetIds.Ref1,
                lblRef1MatchResult).ConfigureAwait(true);
        }

        private async void btnRef2MatchAsync_Click(object sender, EventArgs e)
        {
            await RunMatchAsyncRequestAsync(
                VisionAlignTargetIds.Ref2,
                lblRef2MatchAsync).ConfigureAwait(true);
        }

        private async void btnRef2MatchResult_Click(object sender, EventArgs e)
        {
            await RunAlignMatchAsyncAndResultAsync(
                VisionAlignTargetIds.Ref2,
                lblRef2MatchResult).ConfigureAwait(true);
        }

        private async void btnDieCheckMatchAsync_Click(object sender, EventArgs e)
        {
            await RunMatchAsyncRequestAsync(
                VisionAlignTargetIds.InputPickDie,
                lblDieCheckMatchAsync).ConfigureAwait(true);
        }

        private async void btnDieCheckMatchResult_Click(object sender, EventArgs e)
        {
            await RunDieCheckMatchAsyncAndResultAsync(
                VisionAlignTargetIds.InputPickDie,
                lblDieCheckMatchResult).ConfigureAwait(true);
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

            SetVisionCommandButtonsEnabled(false);
            lblExpose.ForeColor = Color.DimGray;
            lblExpose.Text = "GRAB 실행 중...";
            LogLiveAutoStartBlocked("EXPOSE 전 자동 Live 시작 차단");
            Stopwatch requestTact = Stopwatch.StartNew();
            try
            {
                bool ok = await _adapter.TriggerExposeAsync(0).ConfigureAwait(true);
                requestTact.Stop();
                lblExpose.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                lblExpose.Text = "REQ→ACK " + requestTact.ElapsedMilliseconds + " ms | " +
                                 (ok ? "EXPOSE ACK 완료" : "EXPOSE 실패. Vision READY/연결 상태를 확인하세요.");
                if (ok)
                    LogLiveAutoStartBlocked("EXPOSE 완료 후 자동 Live 시작 차단");
            }
            catch (Exception ex)
            {
                requestTact.Stop();
                lblExpose.ForeColor = Color.Firebrick;
                lblExpose.Text = "REQ→ACK " + requestTact.ElapsedMilliseconds + " ms | GRAB 실패: " + ex.Message;
            }
            finally
            {
                SetVisionCommandButtonsEnabled(true);
            }
        }

        private async Task RunMatchAsyncRequestAsync(string targetId, Label label)
        {
            if (!Ready(label))
                return;

            SetVisionCommandButtonsEnabled(false);
            label.ForeColor = Color.DimGray;
            string finder = VisionAlignTargetIds.ResolveWaferFinder(targetId);
            label.Text = "MATCHASYNC 요청/EPD 대기 중...";
            Stopwatch matchTact = Stopwatch.StartNew();
            try
            {
                bool epdReceived = await AutoVisionRequestService.StartMatchAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    CancellationToken.None).ConfigureAwait(true);

                matchTact.Stop();
                long epdElapsedMilliseconds = matchTact.ElapsedMilliseconds;
                label.ForeColor = epdReceived ? Color.SeaGreen : Color.Firebrick;
                label.Text = epdReceived
                    ? "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine + "EPD 수신 완료 (단독 테스트)"
                    : "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine + "실패 또는 EPD 타임아웃";
            }
            catch (Exception ex)
            {
                matchTact.Stop();
                label.ForeColor = Color.Firebrick;
                label.Text = "REQ→EPD " + matchTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                             "MATCHASYNC 실패: " + ex.Message;
            }
            finally
            {
                SetVisionCommandButtonsEnabled(true);
            }
        }

        private async Task RunAlignMatchAsyncAndResultAsync(string targetId, Label label)
        {
            if (!Ready(label))
                return;

            SetVisionCommandButtonsEnabled(false);
            label.ForeColor = Color.DimGray;
            label.Text = "MATCHASYNC 요청/EPD 대기 중...";
            string finder = VisionAlignTargetIds.ResolveWaferFinder(targetId);
            Stopwatch totalTact = Stopwatch.StartNew();
            try
            {
                bool epdReceived = await AutoVisionRequestService.StartMatchAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    CancellationToken.None).ConfigureAwait(true);

                long epdElapsedMilliseconds = totalTact.ElapsedMilliseconds;
                if (!epdReceived)
                {
                    totalTact.Stop();
                    label.ForeColor = Color.Firebrick;
                    label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "전체 " + totalTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "MATCHASYNC 실패 또는 EPD 타임아웃";
                    return;
                }

                label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                             "MATCHRESULT 최종 결과 대기 중...";
                Stopwatch resultTact = Stopwatch.StartNew();
                MatchResultDto result = await AutoVisionRequestService.WaitMatchResultAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    CancellationToken.None).ConfigureAwait(true);
                resultTact.Stop();
                totalTact.Stop();

                if (result == null || !result.Success)
                {
                    label.ForeColor = Color.Firebrick;
                    label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "EPD→RESULT " + resultTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "전체 " + totalTact.ElapsedMilliseconds + " ms | 결과 실패: " +
                                 (result != null ? result.RawError : "응답 없음");
                    return;
                }

                VisionAlignResult align = VisionCameraCalibrationTransform.ToAlignResult(
                    AutoVisionChannel.Wafer,
                    result,
                    0.0);
                if (align == null)
                {
                    label.ForeColor = Color.Firebrick;
                    label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "EPD→RESULT " + resultTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "전체 " + totalTact.ElapsedMilliseconds + " ms | 완료 데이터 변환 실패";
                    return;
                }

                WaferVisionResultStore.RecordAlign(targetId, align);
                label.ForeColor = Color.SeaGreen;
                label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                             "EPD→RESULT " + resultTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                             "전체 " + totalTact.ElapsedMilliseconds + " ms | DONE score=" + result.Score.ToString("F3");
                LogLiveAutoStartBlocked("ALIGN MATCHASYNC + RESULT 완료 후 자동 Live 시작 차단");
            }
            catch (Exception ex)
            {
                totalTact.Stop();
                label.ForeColor = Color.Firebrick;
                label.Text = "전체 " + totalTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                             "MATCHASYNC + RESULT 실패: " + ex.Message;
            }
            finally
            {
                SetVisionCommandButtonsEnabled(true);
                RefreshSummary();
            }
        }

        private async Task RunDieCheckMatchAsyncAndResultAsync(string targetId, Label label)
        {
            if (!Ready(label))
                return;

            SetVisionCommandButtonsEnabled(false);
            label.ForeColor = Color.DimGray;
            label.Text = "MATCHASYNC 요청/EPD 대기 중...";
            string finder = VisionAlignTargetIds.ResolveWaferFinder(targetId);
            Stopwatch totalTact = Stopwatch.StartNew();
            try
            {
                bool epdReceived = await AutoVisionRequestService.StartMatchAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    CancellationToken.None).ConfigureAwait(true);

                long epdElapsedMilliseconds = totalTact.ElapsedMilliseconds;
                if (!epdReceived)
                {
                    totalTact.Stop();
                    label.ForeColor = Color.Firebrick;
                    label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "전체 " + totalTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "MATCHASYNC 실패 또는 EPD 타임아웃";
                    return;
                }

                label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                             "MATCHRESULT 최종 결과 대기 중...";
                Stopwatch resultTact = Stopwatch.StartNew();
                MatchResultDto result = await AutoVisionRequestService.WaitMatchResultAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    CancellationToken.None).ConfigureAwait(true);
                resultTact.Stop();
                totalTact.Stop();

                if (result == null || !result.Success)
                {
                    label.ForeColor = Color.Firebrick;
                    label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "EPD→RESULT " + resultTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "전체 " + totalTact.ElapsedMilliseconds + " ms | 결과 실패: " +
                                 (result != null ? result.RawError : "응답 없음");
                    return;
                }

                bool ok = result.Success && result.Score >= 0.7;
                WaferVisionResultStore.RecordDieCheck(ok);
                label.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                             "EPD→RESULT " + resultTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                             "전체 " + totalTact.ElapsedMilliseconds + " ms | " +
                             (ok ? "DONE OK" : "DONE NG") + " score=" + result.Score.ToString("F3");
            }
            catch (Exception ex)
            {
                totalTact.Stop();
                label.ForeColor = Color.Firebrick;
                label.Text = "전체 " + totalTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                             "MATCHASYNC + RESULT 실패: " + ex.Message;
            }
            finally
            {
                SetVisionCommandButtonsEnabled(true);
                RefreshSummary();
            }
        }

        private void SetVisionCommandButtonsEnabled(bool enabled)
        {
            btnExpose.Enabled = enabled;
            btnCenterMatchAsync.Enabled = enabled;
            btnCenterMatchResult.Enabled = enabled;
            btnRef1MatchAsync.Enabled = enabled;
            btnRef1MatchResult.Enabled = enabled;
            btnRef2MatchAsync.Enabled = enabled;
            btnRef2MatchResult.Enabled = enabled;
            btnDieCheckMatchAsync.Enabled = enabled;
            btnDieCheckMatchResult.Enabled = enabled;
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
            LogLiveAutoStartBlocked("TryStartLive 호출 차단");
        }

        private void LogLiveAutoStartBlocked(string reason)
        {
            try
            {
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "VISION",
                    "VISION-LIVE-BLOCK",
                    "Wafer Vision 테스트 화면에서 Live 자동 시작을 차단했습니다. viewerPort=" +
                    VisionViewerPorts.Wafer + ", reason=" + reason);
            }
            catch { }
        }
    }
}
