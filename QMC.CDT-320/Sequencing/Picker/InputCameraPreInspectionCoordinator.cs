using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal static class InputCameraPreInspectionCoordinator
    {
        private sealed class RunningInspection
        {
            public Task<int> Task;
            public CancellationTokenSource Cancellation;
            public DateTime StartedAt;
            public string Reason;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<PickerSequenceSide, RunningInspection> Running =
            new Dictionary<PickerSequenceSide, RunningInspection>();

        public static bool EnsureStarted(
            MachineSequenceContext context,
            PickerSequenceSide side,
            PickerSequenceOptions options,
            CancellationToken ct,
            string reason)
        {
            if (context == null)
                return false;

            if (InputCameraPickUpPermissionStore.HasPermission(side))
                return false;

            lock (Sync)
            {
                RunningInspection current;
                if (Running.TryGetValue(side, out current) &&
                    current != null &&
                    current.Task != null &&
                    !current.Task.IsCompleted)
                {
                    return false;
                }

                ClearCompletedNoLock(side);

                PickerSequenceOptions runOptions = CloneForPreInspection(options);
                CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
                Task<int> task = Task.Run(
                    () => RunPreInspectionAsync(context, side, runOptions, linkedCancellation.Token, reason),
                    linkedCancellation.Token);

                Running[side] = new RunningInspection
                {
                    Task = task,
                    Cancellation = linkedCancellation,
                    StartedAt = DateTime.Now,
                    Reason = reason ?? string.Empty
                };
            }

            WriteLog("InputCameraPreInspectionCoordinator",
                side + " InputCamera 선행검사를 시작했습니다. reason=" + (reason ?? "-") + " - Start");
            return true;
        }

        public static async Task<InputCameraPreInspectionWaitResult> WaitForPermissionOrCompletionAsync(
            MachineSequenceContext context,
            PickerSequenceSide side,
            PickerSequenceOptions options,
            CancellationToken ct,
            string reason)
        {
            bool waitLogged = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (context != null)
                    context.StopIfCycleStopRequested("InputCameraPreInspectionCoordinator.Wait:" + side);

                if (InputCameraPickUpPermissionStore.HasPermission(side))
                {
                    if (waitLogged)
                    {
                        WriteLog("InputCameraPreInspectionCoordinator",
                            side + " InputCamera 선행검사 대기 완료. PickUp 허가가 준비되었습니다. - Ok");
                    }

                    return InputCameraPreInspectionWaitResult.PermissionReady();
                }

                EnsureStarted(context, side, options, ct, reason);

                Task<int> runningTask = GetRunningTask(side);
                if (runningTask == null)
                    return InputCameraPreInspectionWaitResult.NoTarget();

                if (runningTask.IsCompleted)
                {
                    int result;
                    try
                    {
                        result = await runningTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        RemoveIfSame(side, runningTask);
                        return InputCameraPreInspectionWaitResult.Failed(
                            -1,
                            "InputCamera 선행검사 task 예외. error=" + ex.Message);
                    }

                    RemoveIfSame(side, runningTask);

                    if (InputCameraPickUpPermissionStore.HasPermission(side))
                        return InputCameraPreInspectionWaitResult.PermissionReady();

                    if (result != 0)
                    {
                        return InputCameraPreInspectionWaitResult.Failed(
                            result,
                            "InputCamera 선행검사 실패. result=" + result);
                    }

                    return InputCameraPreInspectionWaitResult.NoTarget();
                }

                if (!waitLogged)
                {
                    WriteLog("InputCameraPreInspectionCoordinator",
                        side + " InputCamera 선행검사 완료 대기 중입니다. 조건이 맞을 때까지 대기합니다. " +
                        "reason=" + (reason ?? "-") + " - Wait");
                    waitLogged = true;
                }

                await Task.Delay(50, ct).ConfigureAwait(false);
            }
        }

        public static void Clear(PickerSequenceSide side)
        {
            RunningInspection current = null;
            lock (Sync)
            {
                if (Running.TryGetValue(side, out current))
                {
                    Running.Remove(side);
                }
            }

            if (current != null && current.Cancellation != null)
            {
                try
                {
                    current.Cancellation.Cancel();
                    current.Cancellation.Dispose();
                }
                catch
                {
                }
            }

            lock (Sync)
            {
                Running.Remove(side);
            }

            InputCameraPickUpPermissionStore.Clear(side);
        }

        private static async Task<int> RunPreInspectionAsync(
            MachineSequenceContext context,
            PickerSequenceSide side,
            PickerSequenceOptions options,
            CancellationToken ct,
            string reason)
        {
            try
            {
                var sequence = new InputCameraMarkInspectionSequence(context, side);
                int result = await sequence.RunAsync(ct, options).ConfigureAwait(false);
                WriteLog("InputCameraPreInspectionCoordinator",
                    side + " InputCamera 선행검사가 종료되었습니다. result=" + result +
                    ", inspectedCount=" + (sequence.InspectedItems != null ? sequence.InspectedItems.Count : 0) +
                    ", reason=" + (reason ?? "-") + " - Check");
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteLog("InputCameraPreInspectionCoordinator",
                    side + " InputCamera 선행검사 예외. error=" + ex.Message + " - Failed");
                return -1;
            }
        }

        private static Task<int> GetRunningTask(PickerSequenceSide side)
        {
            lock (Sync)
            {
                RunningInspection current;
                if (!Running.TryGetValue(side, out current) || current == null)
                    return null;

                return current.Task;
            }
        }

        private static void RemoveIfSame(PickerSequenceSide side, Task<int> task)
        {
            lock (Sync)
            {
                RunningInspection current;
                if (Running.TryGetValue(side, out current) &&
                    current != null &&
                    object.ReferenceEquals(current.Task, task))
                {
                    Running.Remove(side);
                    if (current.Cancellation != null)
                        current.Cancellation.Dispose();
                }
            }
        }

        private static void ClearCompletedNoLock(PickerSequenceSide side)
        {
            RunningInspection current;
            if (Running.TryGetValue(side, out current) &&
                current != null &&
                current.Task != null &&
                current.Task.IsCompleted)
            {
                Running.Remove(side);
                if (current.Cancellation != null)
                    current.Cancellation.Dispose();
            }
        }

        private static PickerSequenceOptions CloneForPreInspection(PickerSequenceOptions source)
        {
            PickerSequenceOptions options = source ?? PickerSequenceOptions.Default();
            return new PickerSequenceOptions
            {
                RunMode = options.RunMode,
                StartMode = options.StartMode,
                FineMove = options.FineMove,
                MoveTimeoutMs = options.MoveTimeoutMs,
                ResourceTimeoutMs = options.ResourceTimeoutMs,
                PickerNo = options.PickerNo,
                RestrictToPickerNo = options.RestrictToPickerNo,
                VisionRetryCount = options.VisionRetryCount,
                SimulateVisionResult = options.SimulateVisionResult,
                PickerMotionOnlyTestMode = options.PickerMotionOnlyTestMode,
                RequireInputCameraMarkInspectionPermission = false,
                InputCameraPreInspectionMode = true,
                KeepZAfterBottomInspection = options.KeepZAfterBottomInspection,
                EnterSideFromBottomInspection = options.EnterSideFromBottomInspection,
                KeepZUntilSideInspectionComplete = options.KeepZUntilSideInspectionComplete
            };
        }

        private static void WriteLog(string source, string message)
        {
            try
            {
                QMC.Common.Log.Write("Main", "SYSTEM", source, message);
            }
            catch
            {
            }
        }
    }
}
