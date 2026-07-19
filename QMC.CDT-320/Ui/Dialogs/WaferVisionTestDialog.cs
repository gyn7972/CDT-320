using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class WaferVisionTestDialog : Form
    {
        private const string GeneralDialogHostKey = "WaferVisionTestDialog";
        private const string ReviewDialogHostKey = "WaferVisionTestDialog.Review";

        private readonly object _closeSync = new object();
        private readonly CancellationTokenSource _requestLifetimeCts;
        private Task _requestCloseTask;
        private bool _allowClose;
        private bool _lifetimeDisposed;

        public static WaferVisionTestDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(GeneralDialogHostKey, owner, () => new WaferVisionTestDialog());
        }

        public static WaferVisionTestDialog OpenReview(IWin32Window owner, CancellationToken cancellationToken)
        {
            return ModelessDialogHost.Show(
                ReviewDialogHostKey,
                owner,
                () => new WaferVisionTestDialog(cancellationToken));
        }

        public WaferVisionTestDialog()
            : this(CancellationToken.None)
        {
        }

        private WaferVisionTestDialog(CancellationToken cancellationToken)
        {
            _requestLifetimeCts = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : new CancellationTokenSource();

            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
                return;

            waferVisionTestControl.RequestBusyChanged += OnRequestBusyChanged;
            waferVisionTestControl.Configure(_requestLifetimeCts.Token);
        }

        public event Action<bool> RequestBusyChanged;

        public bool IsRequestBusy
        {
            get { return waferVisionTestControl != null && waferVisionTestControl.IsRequestBusy; }
        }

        public Task WaitForRequestCompletionAsync()
        {
            return waferVisionTestControl != null
                ? waferVisionTestControl.WaitForRequestCompletionAsync()
                : Task.FromResult(0);
        }

        public Task RequestClose()
        {
            if (IsDisposed || Disposing)
                return Task.FromResult(0);

            if (InvokeRequired)
                return BeginRequestCloseOnUiThread();

            lock (_closeSync)
            {
                if (_requestCloseTask == null)
                    _requestCloseTask = RequestCloseCoreAsync();
                return _requestCloseTask;
            }
        }

        private Task BeginRequestCloseOnUiThread()
        {
            var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                BeginInvoke(new Action(() =>
                {
                    Task closeTask = RequestClose();
                    closeTask.ContinueWith(
                        task =>
                        {
                            if (task.IsFaulted)
                                completion.TrySetException(task.Exception.InnerExceptions);
                            else if (task.IsCanceled)
                                completion.TrySetCanceled();
                            else
                                completion.TrySetResult(null);
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }));
            }
            catch (ObjectDisposedException)
            {
                completion.TrySetResult(null);
            }
            catch (InvalidOperationException)
            {
                completion.TrySetResult(null);
            }
            return completion.Task;
        }

        private async Task RequestCloseCoreAsync()
        {
            waferVisionTestControl.Enabled = false;
            CancelRequestLifetime();
            waferVisionTestControl.StopLive();

            Task activeRequest = waferVisionTestControl.WaitForRequestCompletionAsync();
            if (activeRequest != null)
                await activeRequest.ConfigureAwait(true);

            // OnFormClosing 호출 스택 안에서 Close를 재진입하지 않도록 한 번 UI 큐에 양보한다.
            await Task.Yield();
            if (IsDisposed || Disposing)
                return;

            _allowClose = true;
            Close();
        }

        private void CancelRequestLifetime()
        {
            lock (_closeSync)
            {
                if (_lifetimeDisposed || _requestLifetimeCts.IsCancellationRequested)
                    return;

                try { _requestLifetimeCts.Cancel(); }
                catch (ObjectDisposedException) { }
            }
        }

        private void OnRequestBusyChanged(bool busy)
        {
            Action<bool> handler = RequestBusyChanged;
            if (handler == null)
                return;

            try { handler(busy); }
            catch (Exception ex)
            {
                try
                {
                    QMC.Common.Log.Write(
                        "Main",
                        "SYSTEM",
                        "WaferVisionTestDialog",
                        "Vision 요청 busy 이벤트 처리 실패: " + ex.Message + " - Failed");
                }
                catch { }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose)
            {
                e.Cancel = true;
                RequestClose();
                base.OnFormClosing(e);
                return;
            }

            waferVisionTestControl.StopLive();
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            waferVisionTestControl.RequestBusyChanged -= OnRequestBusyChanged;
            waferVisionTestControl.StopLive();
            DisposeRequestLifetime();
            base.OnFormClosed(e);
        }

        private void DisposeRequestLifetime()
        {
            lock (_closeSync)
            {
                if (_lifetimeDisposed)
                    return;
                _lifetimeDisposed = true;
            }

            try { _requestLifetimeCts.Dispose(); }
            catch (ObjectDisposedException) { }
        }
    }
}
