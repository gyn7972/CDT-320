using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>시퀀스 유닛 간 핸드오프 신호를 교환하는 동기화 버스입니다.</summary>
    public class SequenceSignalBus
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _signals =
            new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        /// <summary>지정한 신호를 대기 중인 유닛에 통지합니다.</summary>
        public void Set(string signalName)
        {
            if (string.IsNullOrWhiteSpace(signalName))
                throw new ArgumentException("신호 이름이 필요합니다.", nameof(signalName));

            var signal = GetSignal(signalName);
            bool alreadySet = signal.Task.IsCompleted;
            signal.TrySetResult(true);
            if (!alreadySet)
                SequenceTrace.SignalSet(signalName, "state=Set");
        }

        /// <summary>지정한 신호가 들어올 때까지 비동기로 대기합니다.</summary>
        public async Task WaitAsync(string signalName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(signalName))
                throw new ArgumentException("신호 이름이 필요합니다.", nameof(signalName));

            SequenceTrace.SignalWaitStart(signalName);
            var task = GetSignal(signalName).Task;
            var cancelTask = Task.Delay(Timeout.Infinite, ct);
            var completed = await Task.WhenAny(task, cancelTask).ConfigureAwait(false);
            if (completed == cancelTask)
            {
                SequenceTrace.SignalWaitEnd(signalName, -1, "status=Canceled");
                ct.ThrowIfCancellationRequested();
            }

            await task.ConfigureAwait(false);
            SequenceTrace.SignalWaitEnd(signalName, 0, "state=Set");
        }

        /// <summary>지정한 신호를 초기화하여 다음 핸드오프를 다시 기다릴 수 있게 합니다.</summary>
        public void Reset(string signalName)
        {
            if (string.IsNullOrWhiteSpace(signalName))
                throw new ArgumentException("신호 이름이 필요합니다.", nameof(signalName));

            TaskCompletionSource<bool> ignored;
            if (_signals.TryRemove(signalName, out ignored))
                SequenceTrace.SignalReset(signalName, "state=Reset");
        }

        /// <summary>지정한 신호가 현재 Set 상태인지 확인합니다.</summary>
        public bool IsSet(string signalName)
        {
            if (string.IsNullOrWhiteSpace(signalName))
                throw new ArgumentException("신호 이름이 필요합니다.", nameof(signalName));

            TaskCompletionSource<bool> signal;
            if (!_signals.TryGetValue(signalName, out signal) || signal == null)
                return false;

            return signal.Task.IsCompleted;
        }

        private TaskCompletionSource<bool> GetSignal(string signalName)
        {
            return _signals.GetOrAdd(signalName, _ =>
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        }
    }
}

