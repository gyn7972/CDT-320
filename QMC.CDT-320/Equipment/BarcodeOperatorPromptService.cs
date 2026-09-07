using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Dialogs;

namespace QMC.CDT320.Barcode
{
    public enum BarcodeReaderChannel
    {
        InputWafer,
        OutputBin
    }

    public enum BarcodeRecoveryDecision
    {
        Retry,
        ManualApply,
        Cancelled
    }

    public sealed class BarcodeRecoveryRequest
    {
        public BarcodeReaderChannel Channel { get; set; }
        public string MaterialId { get; set; } = "";
        public string MaterialInstanceId { get; set; } = "";
        public string FailureMessage { get; set; } = "";
        // 얼라인 전 값/맵 검증 복구는 동일 후보 재확인만 수행하고 판독 모션을 재발행하지 않는다.
        public bool ValidationRecovery { get; set; }
        public string CurrentBarcode { get; set; } = "";
        public string LotId { get; set; } = "";
        public int PrefixLength { get; set; }
        public int RetryCount { get; set; } = 3;
        public double RetryStepMm { get; set; } = 1.000;

        // 호출부에서 현재값이라는 이름을 선호해도 같은 설정값을 사용합니다.
        public int CurrentRetryCount
        {
            get { return RetryCount; }
            set { RetryCount = value; }
        }

        public double CurrentRetryStepMm
        {
            get { return RetryStepMm; }
            set { RetryStepMm = value; }
        }
    }

    public sealed class BarcodeRecoveryResponse
    {
        public BarcodeRecoveryDecision Decision { get; set; }
        public string ManualBarcode { get; set; } = "";
        public int RetryCount { get; set; }
        public double RetryStepMm { get; set; }

        public static BarcodeRecoveryResponse Cancelled(int retryCount, double retryStepMm)
        {
            return new BarcodeRecoveryResponse
            {
                Decision = BarcodeRecoveryDecision.Cancelled,
                RetryCount = retryCount,
                RetryStepMm = retryStepMm
            };
        }
    }

    /// <summary>
    /// 자동 판독 실패 시 UI thread에 운영자 복구창을 열고 시퀀스가 결정을 await할 수 있게 합니다.
    /// 동시에 둘 이상의 복구창을 열지 않으며 Cycle Stop token은 Cancelled로 종료합니다.
    /// </summary>
    public static class BarcodeOperatorPromptService
    {
        private sealed class PendingPrompt
        {
            public BarcodeRecoveryRequest Request;
            public TaskCompletionSource<BarcodeRecoveryResponse> Completion;
            public BarcodeRecoveryDialog Dialog;
            public QMC.CDT_320.Form1 Host;
            public CancellationTokenRegistration CancellationRegistration;
        }

        private static readonly object Sync = new object();
        private static readonly SemaphoreSlim PromptGate = new SemaphoreSlim(1, 1);
        private static PendingPrompt _pending;

        public static async Task<BarcodeRecoveryResponse> RequestAsync(
            BarcodeRecoveryRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            request.RetryCount = ClampRetryCount(request.RetryCount);
            request.RetryStepMm = ClampRetryStep(request.RetryStepMm);
            if (cancellationToken.IsCancellationRequested)
                return BarcodeRecoveryResponse.Cancelled(request.RetryCount, request.RetryStepMm);

            bool gateEntered = false;
            try
            {
                await PromptGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                gateEntered = true;
                return await RequestOwnedPromptAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return BarcodeRecoveryResponse.Cancelled(request.RetryCount, request.RetryStepMm);
            }
            finally
            {
                if (gateEntered)
                    PromptGate.Release();
            }
        }

        private static async Task<BarcodeRecoveryResponse> RequestOwnedPromptAsync(
            BarcodeRecoveryRequest request,
            CancellationToken cancellationToken)
        {

            PendingPrompt pending;
            lock (Sync)
            {
                if (_pending != null)
                {
                    return BarcodeRecoveryResponse.Cancelled(request.RetryCount, request.RetryStepMm);
                }

                pending = new PendingPrompt
                {
                    Request = request,
                    Completion = new TaskCompletionSource<BarcodeRecoveryResponse>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _pending = pending;
            }

            pending.CancellationRegistration = cancellationToken.Register(
                () => CompletePrompt(
                    pending,
                    BarcodeRecoveryResponse.Cancelled(request.RetryCount, request.RetryStepMm),
                    true));

            try
            {
                bool shown = false;
                for (int attempt = 0; attempt < 10 && !shown && !pending.Completion.Task.IsCompleted; attempt++)
                {
                    shown = TryShowPrompt(pending);
                    if (!shown)
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                }
                if (!shown && !pending.Completion.Task.IsCompleted)
                    CompletePrompt(
                        pending,
                        BarcodeRecoveryResponse.Cancelled(request.RetryCount, request.RetryStepMm),
                        true);

                return await pending.Completion.Task.ConfigureAwait(false);
            }
            finally
            {
                pending.CancellationRegistration.Dispose();
            }
        }

        private static bool TryShowPrompt(PendingPrompt pending)
        {
            try
            {
                QMC.CDT_320.Form1 host = Application.OpenForms
                    .Cast<Form>()
                    .OfType<QMC.CDT_320.Form1>()
                    .FirstOrDefault(form => !form.IsDisposed);
                if (host == null || host.IsDisposed || !host.IsHandleCreated)
                    return false;

                pending.Host = host;
                Action showAction = () => ShowPromptOnUiThread(pending);
                if (host.InvokeRequired)
                    host.BeginInvoke(showAction);
                else
                    showAction();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ShowPromptOnUiThread(PendingPrompt pending)
        {
            if (!IsCurrentPending(pending) || pending.Completion.Task.IsCompleted)
                return;

            try
            {
                var dialog = new BarcodeRecoveryDialog(pending.Request);
                pending.Dialog = dialog;
                dialog.RetryRequested += (retryCount, retryStepMm) =>
                    CompletePrompt(
                        pending,
                        new BarcodeRecoveryResponse
                        {
                            Decision = BarcodeRecoveryDecision.Retry,
                            RetryCount = retryCount,
                            RetryStepMm = retryStepMm
                        },
                        false);
                dialog.ManualApplyRequested += (barcode, retryCount, retryStepMm) =>
                    CompletePrompt(
                        pending,
                        new BarcodeRecoveryResponse
                        {
                            Decision = BarcodeRecoveryDecision.ManualApply,
                            ManualBarcode = barcode,
                            RetryCount = retryCount,
                            RetryStepMm = retryStepMm
                        },
                        false);
                dialog.CancelRequested += () =>
                    CompletePrompt(
                        pending,
                        BarcodeRecoveryResponse.Cancelled(
                            pending.Request.RetryCount,
                            pending.Request.RetryStepMm),
                        false);
                dialog.BuzzerStopRequested += () => StopBuzzer(pending.Host);
                StartBuzzer(pending.Host);
                dialog.Show(pending.Host);
                dialog.Activate();
            }
            catch
            {
                CompletePrompt(
                    pending,
                    BarcodeRecoveryResponse.Cancelled(
                        pending.Request.RetryCount,
                        pending.Request.RetryStepMm),
                    true);
            }
        }

        private static bool IsCurrentPending(PendingPrompt pending)
        {
            lock (Sync)
                return ReferenceEquals(_pending, pending);
        }

        private static void CompletePrompt(
            PendingPrompt pending,
            BarcodeRecoveryResponse response,
            bool closeForCancellation)
        {
            if (pending == null || response == null)
                return;

            bool accepted;
            lock (Sync)
            {
                accepted = ReferenceEquals(_pending, pending);
                if (accepted)
                    _pending = null;
            }
            if (!accepted)
                return;

            response.RetryCount = ClampRetryCount(response.RetryCount);
            response.RetryStepMm = ClampRetryStep(response.RetryStepMm);
            if (response.Decision != BarcodeRecoveryDecision.Cancelled && !pending.Request.ValidationRecovery)
                SaveRetrySettings(pending.Request.Channel, response.RetryCount, response.RetryStepMm);

            pending.Completion.TrySetResult(response);
            EndBuzzer(pending.Host);

            if (closeForCancellation && pending.Dialog != null && !pending.Dialog.IsDisposed)
            {
                Action closeAction = pending.Dialog.CloseForCancellation;
                try
                {
                    if (pending.Dialog.InvokeRequired)
                        pending.Dialog.BeginInvoke(closeAction);
                    else
                        closeAction();
                }
                catch
                {
                }
            }
        }

        private static void SaveRetrySettings(
            BarcodeReaderChannel channel,
            int retryCount,
            double retryStepMm)
        {
            AppSettings settings = AppSettingsStore.Current;
            if (settings == null)
                return;

            if (channel == BarcodeReaderChannel.InputWafer)
            {
                settings.InputBarcodeRetryCount = retryCount;
                settings.InputBarcodeRetryStepMm = retryStepMm;
            }
            else
            {
                settings.OutputBarcodeRetryCount = retryCount;
                settings.OutputBarcodeRetryStepMm = retryStepMm;
            }
            AppSettingsStore.Save();
        }

        private static int ClampRetryCount(int value)
        {
            return Math.Max(0, Math.Min(20, value));
        }

        private static double ClampRetryStep(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1.000;
            return Math.Max(0.001, Math.Min(100.000, value));
        }

        // 작업자 호출 알림(사용자 확정 2026-08-05):
        //  - 부저는 계속 울리지 않고 짧게 2회만 울린다(Auto가 응답을 기다리는 동안 소음 지속 방지).
        //  - 경광등은 녹색+노란색을 켜 "라인 대기 / 작업자 확인 필요"를 표시한다(빨강=알람과 구분).
        private const int AttentionBeepCount = 2;
        private const int AttentionBeepOnMs = 250;
        private const int AttentionBeepOffMs = 200;

        private static void StartBuzzer(QMC.CDT_320.Form1 host)
        {
            try
            {
                var panel = host?.Machine?.OpPanelUnit;
                if (panel == null)
                    return;

                try { panel.TowerLampOperatorAttention(); }
                catch { }

                // 부저 2회는 시퀀스 스레드를 막지 않도록 백그라운드에서 처리한다.
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        for (int i = 0; i < AttentionBeepCount; i++)
                        {
                            panel.Buzzer?.On();
                            await System.Threading.Tasks.Task.Delay(AttentionBeepOnMs).ConfigureAwait(false);
                            panel.Buzzer?.Off();
                            if (i < AttentionBeepCount - 1)
                                await System.Threading.Tasks.Task.Delay(AttentionBeepOffMs).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        try { panel.Buzzer?.Off(); }
                        catch { }
                    }
                });
            }
            catch
            {
            }
        }

        private static void StopBuzzer(QMC.CDT_320.Form1 host)
        {
            try
            {
                if (host != null && host.OpPanelMonitor != null)
                    host.OpPanelMonitor.StopBuzzer();
                else
                    host?.Machine?.OpPanelUnit?.Buzzer?.Off();
            }
            catch
            {
            }
        }

        // 작업자 응답이 끝나면 경광등 표시를 되돌린다. 현재 장비 상태에 맞는 색으로 복원하고,
        // 상태 확인이 어려우면 운전 중(녹색)으로 되돌린다(이 대화는 Auto 진행 중에만 뜬다).
        private static void RestoreTowerLampAfterAttention(QMC.CDT_320.Form1 host)
        {
            try
            {
                var panel = host?.Machine?.OpPanelUnit;
                if (panel == null)
                    return;

                try { panel.Buzzer?.Off(); }
                catch { }

                if (host.Controller != null && host.Controller.Status == EquipmentStatus.AutoRunning)
                    panel.TowerLampRunning();
                else
                    panel.TowerLampWarning();
            }
            catch
            {
            }
        }

        private static void EndBuzzer(QMC.CDT_320.Form1 host)
        {
            if (host == null || host.IsDisposed)
                return;

            Action endAction = () =>
            {
                try
                {
                    // 작업자 확인 표시(녹색+노란색)를 현재 상태 색으로 되돌린다.
                    RestoreTowerLampAfterAttention(host);

                    if (host.OpPanelMonitor != null)
                        host.OpPanelMonitor.EndRunReviewBuzzer();
                    else
                        host.Machine?.OpPanelUnit?.Buzzer?.Off();
                }
                catch
                {
                }
            };

            try
            {
                if (host.InvokeRequired)
                    host.BeginInvoke(endAction);
                else
                    endAction();
            }
            catch
            {
            }
        }
    }
}
