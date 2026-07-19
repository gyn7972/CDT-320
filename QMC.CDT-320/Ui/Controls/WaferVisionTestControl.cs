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
        private readonly object _requestSync = new object();
        private CancellationToken _requestCancellationToken = CancellationToken.None;
        private Task _activeRequest = Task.FromResult(0);
        private Task _requestExecution = Task.FromResult(0);
        private bool _requestBusy;
        private long _requestGeneration;

        public WaferVisionTestControl()
        {
            InitializeComponent();
        }

        public event Action<bool> RequestBusyChanged;

        public bool IsRequestBusy
        {
            get
            {
                lock (_requestSync)
                    return _requestBusy;
            }
        }

        private async void btnExpose_Click(object sender, EventArgs e)
        {
            await RunTrackedRequestAsync(RunExposeAsync).ConfigureAwait(true);
        }

        private async void btnCenterMatchAsync_Click(object sender, EventArgs e)
        {
            await RunTrackedRequestAsync(token => RunMatchAsyncRequestAsync(
                VisionAlignTargetIds.Center,
                lblCenterMatchAsync,
                token)).ConfigureAwait(true);
        }

        private async void btnCenterMatchResult_Click(object sender, EventArgs e)
        {
            await RunTrackedRequestAsync(token => RunAlignMatchAsyncAndResultAsync(
                VisionAlignTargetIds.Center,
                lblCenterMatchResult,
                token)).ConfigureAwait(true);
        }

        private async void btnRef1MatchAsync_Click(object sender, EventArgs e)
        {
            await RunTrackedRequestAsync(token => RunMatchAsyncRequestAsync(
                VisionAlignTargetIds.Ref1,
                lblRef1MatchAsync,
                token)).ConfigureAwait(true);
        }

        private async void btnRef1MatchResult_Click(object sender, EventArgs e)
        {
            await RunTrackedRequestAsync(token => RunAlignMatchAsyncAndResultAsync(
                VisionAlignTargetIds.Ref1,
                lblRef1MatchResult,
                token)).ConfigureAwait(true);
        }

        private async void btnRef2MatchAsync_Click(object sender, EventArgs e)
        {
            await RunTrackedRequestAsync(token => RunMatchAsyncRequestAsync(
                VisionAlignTargetIds.Ref2,
                lblRef2MatchAsync,
                token)).ConfigureAwait(true);
        }

        private async void btnRef2MatchResult_Click(object sender, EventArgs e)
        {
            await RunTrackedRequestAsync(token => RunAlignMatchAsyncAndResultAsync(
                VisionAlignTargetIds.Ref2,
                lblRef2MatchResult,
                token)).ConfigureAwait(true);
        }

        private async void btnDieCheckMatchAsync_Click(object sender, EventArgs e)
        {
            await RunTrackedRequestAsync(token => RunMatchAsyncRequestAsync(
                VisionAlignTargetIds.InputPickDie,
                lblDieCheckMatchAsync,
                token)).ConfigureAwait(true);
        }

        private async void btnDieCheckMatchResult_Click(object sender, EventArgs e)
        {
            await RunTrackedRequestAsync(token => RunDieCheckMatchAsyncAndResultAsync(
                VisionAlignTargetIds.InputPickDie,
                lblDieCheckMatchResult,
                token)).ConfigureAwait(true);
        }

        public void Configure()
        {
            Configure(CancellationToken.None);
        }

        public void Configure(CancellationToken cancellationToken)
        {
            lock (_requestSync)
            {
                if (_requestBusy)
                    throw new InvalidOperationException("Vision 요청 실행 중에는 취소 토큰을 변경할 수 없습니다.");
                _requestCancellationToken = cancellationToken;
            }

            viewer.Configure(VisionHub.Host, VisionViewerPorts.Wafer, "Wafer Image", VisionHub.Wafer);
            RefreshSummary();
        }

        public Task WaitForRequestCompletionAsync()
        {
            lock (_requestSync)
            {
                Task completion = _activeRequest;
                Task execution = _requestExecution;
                return completion ?? execution ?? Task.FromResult(0);
            }
        }

        public void StopLive()
        {
            try { viewer.StopLive(); } catch { }
        }

        private Task RunTrackedRequestAsync(Func<CancellationToken, Task> request)
        {
            if (request == null)
                return Task.FromResult(0);

            TaskCompletionSource<object> completion;
            CancellationToken cancellationToken;
            long generation;
            lock (_requestSync)
            {
                if (_requestBusy)
                    return _activeRequest ?? Task.FromResult(0);
                if (_requestCancellationToken.IsCancellationRequested)
                    return Task.FromResult(0);

                _requestBusy = true;
                generation = ++_requestGeneration;
                cancellationToken = _requestCancellationToken;
                completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                _activeRequest = completion.Task;
            }

            SetVisionCommandButtonsEnabled(false);
            RaiseRequestBusyChanged(true);
            _requestExecution = ExecuteTrackedRequestAsync(
                request,
                cancellationToken,
                generation,
                completion);
            return completion.Task;
        }

        private async Task ExecuteTrackedRequestAsync(
            Func<CancellationToken, Task> request,
            CancellationToken cancellationToken,
            long generation,
            TaskCompletionSource<object> completion)
        {
            try
            {
                // _activeRequest가 먼저 게시된 뒤 실제 요청을 시작하여 Close 대기가 요청을 놓치지 않게 한다.
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                await request(cancellationToken).ConfigureAwait(true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Review Close/Auto 전환에서 요청을 취소한 정상 제어 흐름이다.
            }
            catch (ObjectDisposedException)
            {
                // Application 종료 등으로 UI가 강제 폐기된 경우 후속 UI 갱신을 하지 않는다.
            }
            catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
            {
                // Dispose와 UI continuation이 경합한 경우만 종료 흐름으로 처리한다.
            }
            catch (Exception ex)
            {
                try
                {
                    QMC.Common.Log.Write(
                        "Main",
                        "SYSTEM",
                        "WaferVisionTestControl",
                        "Vision 테스트 요청 처리 실패: " + ex.Message + " - Failed");
                }
                catch { }
            }
            finally
            {
                bool changedToIdle = false;
                lock (_requestSync)
                {
                    if (_requestGeneration == generation)
                    {
                        _requestBusy = false;
                        changedToIdle = true;
                    }
                }

                if (changedToIdle)
                {
                    SetVisionCommandButtonsEnabled(true);
                    RaiseRequestBusyChanged(false);
                }

                // UI 정리와 busy=false 통지가 끝난 뒤 Close 대기자를 해제한다.
                completion.TrySetResult(null);
            }
        }

        private void RaiseRequestBusyChanged(bool busy)
        {
            Action<bool> handler = RequestBusyChanged;
            if (handler == null)
                return;

            try
            {
                handler(busy);
            }
            catch (Exception ex)
            {
                try
                {
                    QMC.Common.Log.Write(
                        "Main",
                        "SYSTEM",
                        "WaferVisionTestControl",
                        "Vision 요청 busy 이벤트 처리 실패: " + ex.Message + " - Failed");
                }
                catch { }
            }
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

        private async Task RunExposeAsync(CancellationToken cancellationToken)
        {
            if (!Ready(lblExpose))
                return;

            lblExpose.ForeColor = Color.DimGray;
            lblExpose.Text = "GRAB 실행 중...";
            LogLiveAutoStartBlocked("EXPOSE 전 자동 Live 시작 차단");
            Stopwatch requestTact = Stopwatch.StartNew();
            try
            {
                bool ok = await AutoVisionRequestService.GrabAsync(
                    AutoVisionChannel.Wafer,
                    0,
                    5000,
                    cancellationToken).ConfigureAwait(true);
                requestTact.Stop();
                lblExpose.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
                lblExpose.Text = "REQ→ACK " + requestTact.ElapsedMilliseconds + " ms | " +
                                 (ok ? "EXPOSE ACK 완료" : "EXPOSE 실패. Vision READY/연결 상태를 확인하세요.");
                if (ok)
                    LogLiveAutoStartBlocked("EXPOSE 완료 후 자동 Live 시작 차단");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                requestTact.Stop();
                SetRequestCancelledLabel(lblExpose, "GRAB 요청이 취소되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                requestTact.Stop();
                lblExpose.ForeColor = Color.Firebrick;
                lblExpose.Text = "REQ→ACK " + requestTact.ElapsedMilliseconds + " ms | GRAB 실패: " + ex.Message;
            }
        }

        private async Task RunMatchAsyncRequestAsync(
            string targetId,
            Label label,
            CancellationToken cancellationToken)
        {
            if (!Ready(label))
                return;

            label.ForeColor = Color.DimGray;
            string finder = VisionAlignTargetIds.ResolveWaferFinder(targetId);
            label.Text = "INSPECT_SYNC 요청/EPD 대기 중...";
            Stopwatch matchTact = Stopwatch.StartNew();
            try
            {
                bool epdReceived = await AutoVisionRequestService.StartMatchAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    cancellationToken).ConfigureAwait(true);

                long epdElapsedMilliseconds = matchTact.ElapsedMilliseconds;
                if (!epdReceived)
                {
                    matchTact.Stop();
                    label.ForeColor = Color.Firebrick;
                    label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "실패 또는 EPD 타임아웃";
                    return;
                }

                Stopwatch resultTact = Stopwatch.StartNew();
                MatchResultDto result = await AutoVisionRequestService.WaitMatchResultAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    cancellationToken).ConfigureAwait(true);
                resultTact.Stop();
                matchTact.Stop();
                bool completed = result != null && result.Success;
                label.ForeColor = completed ? Color.SeaGreen : Color.Firebrick;
                label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                             "EPD→RESULT " + resultTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                             "전체 " + matchTact.ElapsedMilliseconds + " ms | " +
                             (completed ? "DONE" : "RESULT 실패");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                matchTact.Stop();
                SetRequestCancelledLabel(label, "INSPECT_SYNC 요청이 취소되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                matchTact.Stop();
                label.ForeColor = Color.Firebrick;
                label.Text = "REQ→EPD " + matchTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                             "INSPECT_SYNC 실패: " + ex.Message;
            }
        }

        private async Task RunAlignMatchAsyncAndResultAsync(
            string targetId,
            Label label,
            CancellationToken cancellationToken)
        {
            if (!Ready(label))
                return;

            label.ForeColor = Color.DimGray;
            label.Text = "INSPECT_SYNC 요청/EPD 대기 중...";
            string finder = VisionAlignTargetIds.ResolveWaferFinder(targetId);
            Stopwatch totalTact = Stopwatch.StartNew();
            try
            {
                bool epdReceived = await AutoVisionRequestService.StartMatchAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    cancellationToken).ConfigureAwait(true);

                long epdElapsedMilliseconds = totalTact.ElapsedMilliseconds;
                if (!epdReceived)
                {
                    totalTact.Stop();
                    label.ForeColor = Color.Firebrick;
                    label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "전체 " + totalTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "INSPECT_SYNC 실패 또는 EPD 타임아웃";
                    return;
                }

                label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                             "RESULT 최종 결과 대기 중...";
                Stopwatch resultTact = Stopwatch.StartNew();
                MatchResultDto result = await AutoVisionRequestService.WaitMatchResultAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    cancellationToken).ConfigureAwait(true);
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
                LogLiveAutoStartBlocked("ALIGN INSPECT_SYNC + RESULT 완료 후 자동 Live 시작 차단");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                totalTact.Stop();
                SetRequestCancelledLabel(label, "INSPECT_SYNC + RESULT 요청이 취소되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                totalTact.Stop();
                label.ForeColor = Color.Firebrick;
                label.Text = "전체 " + totalTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                             "INSPECT_SYNC + RESULT 실패: " + ex.Message;
            }
            finally
            {
                RefreshSummary();
            }
        }

        private async Task RunDieCheckMatchAsyncAndResultAsync(
            string targetId,
            Label label,
            CancellationToken cancellationToken)
        {
            if (!Ready(label))
                return;

            label.ForeColor = Color.DimGray;
            label.Text = "INSPECT_SYNC 요청/EPD 대기 중...";
            string finder = VisionAlignTargetIds.ResolveWaferFinder(targetId);
            Stopwatch totalTact = Stopwatch.StartNew();
            try
            {
                bool epdReceived = await AutoVisionRequestService.StartMatchAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    cancellationToken).ConfigureAwait(true);

                long epdElapsedMilliseconds = totalTact.ElapsedMilliseconds;
                if (!epdReceived)
                {
                    totalTact.Stop();
                    label.ForeColor = Color.Firebrick;
                    label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "전체 " + totalTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                                 "INSPECT_SYNC 실패 또는 EPD 타임아웃";
                    return;
                }

                label.Text = "REQ→EPD " + epdElapsedMilliseconds + " ms" + Environment.NewLine +
                             "RESULT 최종 결과 대기 중...";
                Stopwatch resultTact = Stopwatch.StartNew();
                MatchResultDto result = await AutoVisionRequestService.WaitMatchResultAsync(
                    AutoVisionChannel.Wafer,
                    finder,
                    0,
                    5000,
                    cancellationToken).ConfigureAwait(true);
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                totalTact.Stop();
                SetRequestCancelledLabel(label, "INSPECT_SYNC + RESULT 요청이 취소되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                totalTact.Stop();
                label.ForeColor = Color.Firebrick;
                label.Text = "전체 " + totalTact.ElapsedMilliseconds + " ms" + Environment.NewLine +
                             "INSPECT_SYNC + RESULT 실패: " + ex.Message;
            }
            finally
            {
                RefreshSummary();
            }
        }

        private void SetVisionCommandButtonsEnabled(bool enabled)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<bool>(SetVisionCommandButtonsEnabled), enabled);
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

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

        private void SetRequestCancelledLabel(Label label, string message)
        {
            if (label == null || label.IsDisposed || IsDisposed || Disposing || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<Label, string>(SetRequestCancelledLabel), label, message);
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

            label.ForeColor = Color.DimGray;
            label.Text = string.IsNullOrWhiteSpace(message) ? "Vision 요청이 취소되었습니다." : message;
        }

        private void RefreshSummary()
        {
            if (IsDisposed || Disposing || lblSummary == null || lblSummary.IsDisposed)
                return;

            if (InvokeRequired)
            {
                if (!IsHandleCreated)
                    return;
                try { BeginInvoke(new Action(RefreshSummary)); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

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
                if (!lblSummary.IsDisposed)
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
