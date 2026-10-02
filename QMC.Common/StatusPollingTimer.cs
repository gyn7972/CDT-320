using System;
using System.Threading;

namespace QMC.Common
{
    // Perform one status update, then wait 10ms before scheduling the next one.
    // Reuse the timer so each tick does not allocate another async delay.
    internal sealed class StatusPollingTimer : IDisposable
    {
        private const int IntervalMilliseconds = 5; //10
        private readonly object _sync = new object();
        private readonly Action _updateStatus;
        private readonly CancellationToken _token;
        private readonly Timer _timer;
        private bool _disposed;
        private bool _stopped;
        private bool _callbackRunning;

        public StatusPollingTimer(Action updateStatus, CancellationToken token)
        {
            if (updateStatus == null)
                throw new ArgumentNullException("updateStatus");

            _updateStatus = updateStatus;
            _token = token;

            // Preserve the normal ExecutionContext capture. Publish the timer
            // before its first asynchronous callback can be queued.
            _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
            lock (_sync)
            {
                if (!_token.IsCancellationRequested)
                    _timer.Change(0, Timeout.Infinite);
                else
                    _stopped = true;
            }
        }

        private void OnTick(object state)
        {
            lock (_sync)
            {
                if (_disposed || _stopped || _token.IsCancellationRequested || _callbackRunning)
                    return;
                _callbackRunning = true;
            }

            bool stop = false;
            try
            {
                _updateStatus();
            }
            catch (OperationCanceledException)
            {
                // The previous polling loops also ended for an OCE belonging
                // to another token. Do not cancel the owner's motion token.
                stop = true;
            }
            catch (Exception)
            {
                // Status read failures retry after the same 10ms delay.
            }
            finally
            {
                lock (_sync)
                {
                    _callbackRunning = false;
                    if (stop)
                        _stopped = true;
                    if (!_disposed && !_stopped && !_token.IsCancellationRequested)
                        _timer.Change(IntervalMilliseconds, Timeout.Infinite);
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;
                _disposed = true;
                _timer.Dispose(); // An admitted callback may finish; do not wait.
            }
        }
    }
}
