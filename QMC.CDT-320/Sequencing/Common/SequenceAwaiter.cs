using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{
    internal static class SequenceAwaiter
    {
        public static Task<int> AwaitIntAsync(Task<int> stepTask, CancellationToken ct)
        {
            return AwaitAsync(stepTask, -1, ct);
        }

        public static Task<bool> AwaitBoolAsync(Task<bool> stepTask, CancellationToken ct)
        {
            return AwaitAsync(stepTask, false, ct);
        }

        public static Task<AxisMoveWaitResult> AwaitAxisWaitAsync(Task<AxisMoveWaitResult> stepTask, CancellationToken ct)
        {
            return AwaitAsync(stepTask, null, ct);
        }

        public static async Task<T> AwaitAsync<T>(Task<T> stepTask, T defaultValue, CancellationToken ct)
        {
            try
            {
                if (stepTask == null)
                    return defaultValue;

                if (stepTask.IsCompleted)
                    return await stepTask.ConfigureAwait(false);

                Task cancelTask = Task.Delay(Timeout.Infinite, ct);
                Task completed = await Task.WhenAny(stepTask, cancelTask).ConfigureAwait(false);
                if (!ReferenceEquals(completed, stepTask))
                    ct.ThrowIfCancellationRequested();

                return await stepTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
