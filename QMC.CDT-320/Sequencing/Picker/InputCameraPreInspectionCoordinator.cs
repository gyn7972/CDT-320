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

        /// <summary>
        /// 정상 Auto Cycle Stop에서 이미 시작된 선행검사가 자체 안전 경계까지 종료될 때까지 기다린다.
        /// 호출자는 prefetch 러너의 신규 시작을 먼저 차단해야 하며, 이 메서드는 진행 중 모션을 취소하지 않는다.
        /// </summary>
        public static async Task<int> WaitUntilIdleAsync(
            string reason,
            int timeoutMs,
            CancellationToken ct)
        {
            string safeReason = string.IsNullOrWhiteSpace(reason) ? "CycleStopDrain" : reason;
            int safeTimeoutMs = timeoutMs > 0 ? timeoutMs : 30000;
            DateTime startedAt = DateTime.UtcNow;
            bool waitLogged = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var snapshot = new List<KeyValuePair<PickerSequenceSide, Task<int>>>();
                lock (Sync)
                {
                    foreach (KeyValuePair<PickerSequenceSide, RunningInspection> pair in Running)
                    {
                        if (pair.Value != null && pair.Value.Task != null)
                        {
                            snapshot.Add(new KeyValuePair<PickerSequenceSide, Task<int>>(
                                pair.Key,
                                pair.Value.Task));
                        }
                    }
                }

                if (snapshot.Count == 0)
                {
                    if (waitLogged)
                    {
                        WriteLog("InputCameraPreInspectionCoordinator",
                            safeReason + " InputCamera 선행검사 drain 완료. - Ok");
                    }
                    return 0;
                }

                for (int i = 0; i < snapshot.Count; i++)
                {
                    PickerSequenceSide side = snapshot[i].Key;
                    Task<int> task = snapshot[i].Value;
                    if (task == null || !task.IsCompleted)
                        continue;

                    int result;
                    try
                    {
                        result = await task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        WriteLog("InputCameraPreInspectionCoordinator",
                            safeReason + " InputCamera 선행검사 drain 중 작업이 취소되었습니다. side=" + side +
                            ", error=" + ex.Message + " - Failed");
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        WriteLog("InputCameraPreInspectionCoordinator",
                            safeReason + " InputCamera 선행검사 drain 중 작업이 실패했습니다. side=" + side +
                            ", error=" + ex.Message + " - Failed");
                        return -1;
                    }

                    RemoveIfSame(side, task);
                    if (result != 0 && result != CycleStoppedResult)
                    {
                        WriteLog("InputCameraPreInspectionCoordinator",
                            safeReason + " InputCamera 선행검사 drain 결과 실패. side=" + side +
                            ", result=" + result + " - Failed");
                        return result;
                    }
                }

                if ((DateTime.UtcNow - startedAt).TotalMilliseconds >= safeTimeoutMs)
                {
                    WriteLog("InputCameraPreInspectionCoordinator",
                        safeReason + " InputCamera 선행검사 drain 시간 초과. running=" + snapshot.Count +
                        ", timeoutMs=" + safeTimeoutMs + " - Failed");
                    return -1;
                }

                if (!waitLogged)
                {
                    waitLogged = true;
                    WriteLog("InputCameraPreInspectionCoordinator",
                        safeReason + " 진행 중 InputCamera 선행검사의 Cycle Stop 안전 종료를 기다립니다. running=" +
                        snapshot.Count + " - Wait");
                }

                await Task.Delay(20, ct).ConfigureAwait(false);
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
            if (options != null &&
                options.RunMode == SequenceRunMode.Auto &&
                context.IsCycleStopRequested)
            {
                return false;
            }

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
                if (options != null &&
                    options.RunMode == SequenceRunMode.Auto &&
                    context.IsCycleStopRequested)
                {
                    return false;
                }

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
            // [가시성 2026-08-11] 허가 대기 로그는 4-인자 WriteLog 라 운영 최소 로그 정책에서 버려져,
            // 픽커 본 시퀀스가 선행검사 허가를 기다리는 정체 구간이 무로그였다(실측 2026-08-11 16:37 —
            // Place 완료 후 33초 무진행). 60초 초과 시 항상 남는 Warning 을 60초마다 남긴다.
            DateTime waitStartUtc = DateTime.UtcNow;
            DateTime lastLongNotifiedUtc = DateTime.MinValue;

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
                    // [진단 가시성 2026-08-25, 사용자 승인] 정지 감지 로그가 4-인자 WriteLog라 운영 최소
                    // 로그 정책에서 버려져, 정지 직후 "각 대기가 정지를 봤는지"를 로그로 확인할 수 없었다.
                    QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "InputCameraPreInspectionCoordinator",
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
                        // [가시성 2026-08-11] 최소 로그 정책에서도 남도록 레벨 지정 로그를 사용한다.
                        QMC.Common.Log.Write(QMC.Common.LogLevel.AboveNormal, "Main", "InputCameraPreInspectionCoordinator",
                            side + " InputCamera 선행검사 완료 대기 중입니다. 조건이 맞을 때까지 대기합니다. " +
                            "reason=" + (reason ?? "-") + " - Wait");
                        waitLogged = true;
                    }

                    double waitedSec = (DateTime.UtcNow - waitStartUtc).TotalSeconds;
                    if (waitedSec >= 60.0 &&
                        (lastLongNotifiedUtc == DateTime.MinValue ||
                         (DateTime.UtcNow - lastLongNotifiedUtc).TotalSeconds >= 60.0))
                    {
                        lastLongNotifiedUtc = DateTime.UtcNow;
                        QMC.Common.Logging.EventLogger.Write(
                            QMC.Common.Logging.EventKind.Warning,
                            "SYSTEM",
                            "INPUT-CAMERA-PERMISSION-WAIT-LONG",
                            side + " InputCamera 선행검사 허가 대기가 길어지고 있습니다. elapsedSec=" + (int)waitedSec +
                            ", reason=" + (reason ?? "-") +
                            ", queue=" + InputEntryQueue.Describe());
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
