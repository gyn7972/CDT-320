using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    internal static class InputCameraPreInspectionCoordinator
    {
        private const int CycleStoppedResult = -320901;

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

        // [동적 선행 대기점 2026-07-27] 촬영(선행검사) 진행 중 여부 — 읽기 전용 조회(부수효과 없음).
        // 대기 픽커의 동적 선행 대기 게이트 1(자기 측 촬영 진행 중) 판정용. 허가 발행/소비 무변경.
        public static bool IsInspectionRunning(PickerSequenceSide side)
        {
            lock (Sync)
            {
                RunningInspection running;
                return Running.TryGetValue(side, out running) &&
                       running != null &&
                       running.Task != null &&
                       !running.Task.IsCompleted;
            }
        }

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

            string pendingPermissionDetail;
            if (InputCameraPickUpPermissionStore.HasAnyPermission(out pendingPermissionDetail))
            {
                WriteLog("InputCameraPreInspectionCoordinator",
                    side + " InputCamera 선행검사를 시작하지 않습니다. 이미 PickUp 허가가 발급되어 InputVisionX Avoid 상태를 유지해야 합니다. " +
                    "pendingPermission=" + pendingPermissionDetail +
                    ", reason=" + (reason ?? "-") + " - Wait");
                return false;
            }

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

                // 진짜 FIFO(Q2): 선행검사 Task를 시작하는 이 임계구역에서 진입 티켓을 발급한다.
                // 카메라존 양보 대기(선행검사 Task 내부)보다 반드시 먼저 발급돼, 대기 중 주체는 항상
                // 자기 티켓을 보유한다(mine=none 상호양보 소멸). 멱등이라 재기동 시 기존 순서 유지.
                InputEntryQueue.Enqueue(side, InputEntryKind.PreInspection);
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
                if (ct.IsCancellationRequested)
                {
                    string pendingPermissionDetail;
                    bool hasAnyPermission = InputCameraPickUpPermissionStore.HasAnyPermission(out pendingPermissionDetail);
                    WriteLog("InputCameraPreInspectionCoordinator",
                        side + " InputCamera 선행검사 대기 진입 전 취소 토큰 감지. " +
                        "cycleStopRequested=" + (context != null && context.IsCycleStopRequested) +
                        ", hasOwnPermission=" + InputCameraPickUpPermissionStore.HasPermission(side) +
                        ", hasAnyPermission=" + hasAnyPermission +
                        ", permissionDetail=" + (string.IsNullOrWhiteSpace(pendingPermissionDetail) ? "-" : pendingPermissionDetail) +
                        ", reason=" + (reason ?? "-") + " - Canceled");
                }

                ct.ThrowIfCancellationRequested();
                if (context != null && context.IsCycleStopRequested)
                {
                    WriteLog("InputCameraPreInspectionCoordinator",
                        side + " InputCamera 선행검사 대기 중 CYCLE STOP 요청 감지. " +
                        "reason=" + (reason ?? "-") + " - CycleStop");
                    context.StopIfCycleStopRequested("InputCameraPreInspectionCoordinator.Wait:" + side);
                }

                if (InputCameraPickUpPermissionStore.HasPermission(side))
                {
                    if (waitLogged)
                    {
                        WriteLog("InputCameraPreInspectionCoordinator",
                            side + " InputCamera 선행검사 대기 완료. PickUp 허가가 준비되었습니다. - Ok");
                    }

                    return InputCameraPreInspectionWaitResult.PermissionReady();
                }

                Task<int> runningTask = GetRunningTask(side);
                if (runningTask != null)
                {
                    if (runningTask.IsCompleted)
                    {
                        int result;
                        try
                        {
                            result = await runningTask.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            RemoveIfSame(side, runningTask);
                            if (ct.IsCancellationRequested ||
                                (context != null && context.IsCycleStopRequested))
                            {
                                throw;
                            }

                            WriteLog("InputCameraPreInspectionCoordinator",
                                side + " InputCamera 선행검사 취소 완료 Task가 남아 있어 제거 후 새 선행검사를 시작합니다. " +
                                "reason=" + (reason ?? "-") + " - Recover");
                            waitLogged = false;
                            continue;
                        }
                        catch (SequenceStopException ex)
                        {
                            RemoveIfSame(side, runningTask);
                            if (context != null && context.IsCycleStopRequested)
                            {
                                throw;
                            }

                            WriteLog("InputCameraPreInspectionCoordinator",
                                side + " InputCamera 선행검사 Cycle Stop 완료 Task가 남아 있어 제거 후 새 선행검사를 시작합니다. " +
                                "reason=" + (reason ?? "-") +
                                ", stopReason=" + ex.Message + " - Recover");
                            waitLogged = false;
                            continue;
                        }
                        catch (ObjectDisposedException ex)
                        {
                            RemoveIfSame(side, runningTask);
                            if (ct.IsCancellationRequested ||
                                (context != null && context.IsCycleStopRequested))
                            {
                                throw new OperationCanceledException(ex.Message, ex, ct);
                            }

                            WriteLog("InputCameraPreInspectionCoordinator",
                                side + " InputCamera 선행검사 Task 정리 중 dispose 상태를 감지해 제거 후 새 선행검사를 시작합니다. " +
                                "reason=" + (reason ?? "-") + ", error=" + ex.Message + " - Recover");
                            waitLogged = false;
                            continue;
                        }
                        catch (Exception ex)
                        {
                            RemoveIfSame(side, runningTask);
                            return InputCameraPreInspectionWaitResult.Failed(
                                -1,
                                "InputCamera 선행검사 task 예외. error=" + ex.Message);
                        }

                        RemoveIfSame(side, runningTask);

                        if (result == CycleStoppedResult)
                        {
                            if (context != null && context.IsCycleStopRequested)
                                context.StopIfCycleStopRequested("InputCameraPreInspectionCoordinator.CompletedStop:" + side);

                            WriteLog("InputCameraPreInspectionCoordinator",
                                side + " InputCamera 선행검사 Cycle Stop 결과를 실패로 사용하지 않고 제거 후 새 선행검사를 시작합니다. " +
                                "reason=" + (reason ?? "-") + " - Recover");
                            waitLogged = false;
                            continue;
                        }

                        if (InputCameraPickUpPermissionStore.HasPermission(side))
                            return InputCameraPreInspectionWaitResult.PermissionReady();

                        if (result != 0)
                        {
                            return InputCameraPreInspectionWaitResult.Failed(
                                result,
                                "InputCamera 선행검사 실패. result=" + result);
                        }

                        WriteLog("InputCameraPreInspectionCoordinator",
                            side + " InputCamera 선행검사 완료 후 PickUp 대상이 없습니다. " +
                            "reason=" + (reason ?? "-") + " - NoTarget");
                        return InputCameraPreInspectionWaitResult.NoTarget();
                    }

                    if (!waitLogged)
                    {
                        WriteLog("InputCameraPreInspectionCoordinator",
                            side + " InputCamera 선행검사 완료 대기 중입니다. 조건이 맞을 때까지 대기합니다. " +
                            "reason=" + (reason ?? "-") + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(1, ct).ConfigureAwait(false);
                    continue;
                }

                EnsureStarted(context, side, options, ct, reason);

                runningTask = GetRunningTask(side);
                if (runningTask == null)
                {
                    string pendingPermissionDetail;
                    if (InputCameraPickUpPermissionStore.HasAnyPermission(out pendingPermissionDetail))
                    {
                        if (!waitLogged)
                        {
                            WriteLog("InputCameraPreInspectionCoordinator",
                                side + " InputCamera 선행검사 시작을 보류합니다. 다른 Picker의 PickUp 허가가 살아 있어 InputVisionX를 Avoid로 유지합니다. " +
                                "pendingPermission=" + pendingPermissionDetail +
                                ", reason=" + (reason ?? "-") + " - Wait");
                            waitLogged = true;
                        }

                        await Task.Delay(1, ct).ConfigureAwait(false);
                        continue;
                    }

                    WriteLog("InputCameraPreInspectionCoordinator",
                        side + " InputCamera 선행검사 시작 대상이 없습니다. " +
                        "reason=" + (reason ?? "-") + " - NoTarget");
                    return InputCameraPreInspectionWaitResult.NoTarget();
                }

                if (!waitLogged)
                {
                    WriteLog("InputCameraPreInspectionCoordinator",
                        side + " InputCamera 선행검사 완료 대기 중입니다. 조건이 맞을 때까지 대기합니다. " +
                        "reason=" + (reason ?? "-") + " - Wait");
                    waitLogged = true;
                }

                await Task.Delay(1, ct).ConfigureAwait(false);
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
            catch (SequenceStopException ex)
            {
                WriteLog("InputCameraPreInspectionCoordinator",
                    side + " InputCamera 선행검사가 Cycle Stop 경계에서 정지되었습니다. reason=" +
                    ex.Message + ", requestReason=" + (reason ?? "-") + " - Stopped");
                return CycleStoppedResult;
            }
            catch (Exception ex)
            {
                WriteLog("InputCameraPreInspectionCoordinator",
                    side + " InputCamera 선행검사 예외. error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
                // 진짜 FIFO(Q3-c): 선행검사 Task가 종료됐는데 PickUp 허가가 발급되지 않았으면
                // (NoTarget/Fail/Cancel) 진입 티켓을 반납한다. await 경로든 fire-and-forget(prefetch/
                // StartSafe)이든 모든 완료가 자기 티켓을 스윕해 유령 head를 제거한다. 허가가 발급됐으면
                // 티켓은 그 허가가 대표하며 Store 소비(TryConsume)/Clear 시 반납된다.
                if (!InputCameraPickUpPermissionStore.HasPermission(side))
                    InputEntryQueue.Dequeue(side);
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
                InputDieVisionFailureAction = options.InputDieVisionFailureAction,
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
