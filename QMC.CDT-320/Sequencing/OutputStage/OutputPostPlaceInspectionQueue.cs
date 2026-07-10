using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Materials;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.VisionComm;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Sequencing
{

    internal sealed class OutputPostPlaceInspectionRequest
    {

        public string DieId { get; set; } = "";

        public BinSide OutputSide { get; set; }

        public OutputStageReceiveTarget ReceiveTarget { get; set; }

        public bool FineMove { get; set; }

        public int MoveTimeoutMs { get; set; }

        public string Owner { get; set; } = "";

        public bool SkipInspection { get; set; }
    }

    internal sealed class OutputPostPlaceInspectionQueue
    {
        // Place 시퀀스는 4-head를 모두 내려놓는 동안 OutputPlaceArea를 보유한다.
        // 후검사는 요청만 큐에 등록하고, Place가 끝나 Picker가 Output zone에서 빠진 뒤
        // 이 큐가 OutputPlaceArea를 넘겨받아 Output camera 검사와 Material 업데이트를 수행한다.
        private const int StopRequestedResult = -9001;

        private readonly MachineSequenceContext _context;
        private readonly ConcurrentQueue<OutputPostPlaceInspectionRequest> _queue =
            new ConcurrentQueue<OutputPostPlaceInspectionRequest>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private int _workerRunning;
        private int _pendingOrRunning;
        private int _failed;
        private int _batchDepth;
        private string _batchOwner = "";
        private string _failureCode = "";
        private string _failureMessage = "";

        public OutputPostPlaceInspectionQueue(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException("context");
        }

        public void BeginBatch(string owner)
        {
            int depth = Interlocked.Increment(ref _batchDepth);
            if (depth == 1)
                _batchOwner = string.IsNullOrWhiteSpace(owner) ? "" : owner;
            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "Output camera 후검사 묶음 등록 시작. owner=" +
                (string.IsNullOrWhiteSpace(owner) ? "-" : owner) +
                ", depth=" + depth + " - Ok");
        }

        public void EndBatch(string owner)
        {
            int depth = Interlocked.Decrement(ref _batchDepth);
            if (depth < 0)
            {
                Interlocked.Exchange(ref _batchDepth, 0);
                depth = 0;
            }
            if (depth == 0)
                _batchOwner = "";
            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "Output camera 후검사 묶음 등록 종료. owner=" +
                (string.IsNullOrWhiteSpace(owner) ? "-" : owner) +
                ", depth=" + depth +
                ", pendingOrRunning=" + Volatile.Read(ref _pendingOrRunning) + " - Ok");
            if (depth == 0 && Volatile.Read(ref _pendingOrRunning) > 0)
            {
                _signal.Release();
                if (Interlocked.CompareExchange(ref _workerRunning, 1, 0) == 0)
                    Task.Run(() => ProcessQueueAsync(CancellationToken.None));
            }
        }

        public void CancelBatch(string owner, string reason)
        {
            int depth = Interlocked.Decrement(ref _batchDepth);
            if (depth < 0)
            {
                Interlocked.Exchange(ref _batchDepth, 0);
                depth = 0;
            }

            if (depth == 0)
                _batchOwner = "";

            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "Output camera 후검사 묶음을 취소합니다. owner=" +
                (string.IsNullOrWhiteSpace(owner) ? "-" : owner) +
                ", reason=" + (string.IsNullOrWhiteSpace(reason) ? "-" : reason) +
                ", depth=" + depth +
                ", pendingOrRunning=" + Volatile.Read(ref _pendingOrRunning) + " - Cancel");

            if (depth == 0)
                DrainQueuedRequests("Output camera 후검사 묶음 취소로 대기 요청을 정리합니다. owner=" +
                    (string.IsNullOrWhiteSpace(owner) ? "-" : owner));
        }

        public int Enqueue(OutputPostPlaceInspectionRequest request, CancellationToken ct)
        {
            if (IsStopOrAlarmActive())
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "활성 알람 상태라 Output camera 후검사 요청 등록을 중단합니다. - Stopped");
                return 0;
            }
            if (request == null)
                return RaiseFailure("OUT-POST-INSPECT-REQUEST", "OutputPostPlaceInspection",
                    "Output camera 후검사 요청 정보가 없습니다.");
            if (Volatile.Read(ref _failed) != 0)
            {
                return RaiseFailure(
                    string.IsNullOrWhiteSpace(_failureCode) ? "OUT-POST-INSPECT-FAILED" : _failureCode,
                    "OutputPostPlaceInspection",
                    "이전 Output camera 후검사 실패가 정리되지 않아 새 요청을 등록할 수 없습니다. " +
                    "die=" + request.DieId + ", side=" + request.OutputSide +
                    ", lastFailure=" + _failureMessage);
            }
            if (request.SkipInspection)
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Picker Motion Only Test 모드: Output camera 후검사 요청을 등록하지 않습니다. die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", owner=" + request.Owner + " - Check");
                return 0;
            }
            request.ReceiveTarget = CloneReceiveTarget(request.ReceiveTarget);
            Interlocked.Increment(ref _pendingOrRunning);
            _queue.Enqueue(request);
            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "Output camera 후검사 요청 등록. die=" + request.DieId +
                ", side=" + request.OutputSide +
                ", orderIndex=" + (request.ReceiveTarget != null ? request.ReceiveTarget.OrderIndex.ToString() : "-") +
                ", targetX=" + (request.ReceiveTarget != null ? request.ReceiveTarget.TargetX.ToString("F6") : "-") +
                ", targetY=" + (request.ReceiveTarget != null ? request.ReceiveTarget.TargetY.ToString("F6") : "-") +
                ", owner=" + request.Owner + " - Ok");
            if (Volatile.Read(ref _batchDepth) <= 0)
            {
                _signal.Release();
                if (Interlocked.CompareExchange(ref _workerRunning, 1, 0) == 0)
                    Task.Run(() => ProcessQueueAsync(ct));
            }
            return 0;
        }

        public async Task<int> WaitUntilIdleAsync(string waiter, int timeoutMs, CancellationToken ct)
        {
            string safeWaiter = string.IsNullOrWhiteSpace(waiter) ? "Unknown" : waiter;
            int safeTimeoutMs = timeoutMs > 0 ? timeoutMs : 0;
            DateTime start = DateTime.UtcNow;
            bool waitLogged = false;
            SequenceTrace.WaitStart("OutputPostPlaceInspectionIdle",
                "waiter=" + safeWaiter,
                "timeoutMs=" + safeTimeoutMs,
                BuildWaitStateDetail());
            if (Volatile.Read(ref _failed) != 0)
            {
                SequenceTrace.WaitEnd("OutputPostPlaceInspectionIdle",
                    -1,
                    "waiter=" + safeWaiter,
                    "status=FailedBeforeWait",
                    BuildWaitStateDetail());
                return ReportStoredFailure(safeWaiter);
            }
            while (Volatile.Read(ref _pendingOrRunning) > 0)
            {
                ct.ThrowIfCancellationRequested();
                if (Volatile.Read(ref _failed) != 0)
                {
                    SequenceTrace.WaitEnd("OutputPostPlaceInspectionIdle",
                        -1,
                        "waiter=" + safeWaiter,
                        "status=FailedDuringWait",
                        "elapsedMs=" + ElapsedMs(start),
                        BuildWaitStateDetail());
                    return ReportStoredFailure(safeWaiter);
                }
                if (Volatile.Read(ref _batchDepth) > 0 && IsSameBatchOwner(safeWaiter))
                {
                    SequenceTrace.WaitEnd("OutputPostPlaceInspectionIdle",
                        0,
                        "waiter=" + safeWaiter,
                        "status=BatchOpenDeferred",
                        "elapsedMs=" + ElapsedMs(start),
                        BuildWaitStateDetail());
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        safeWaiter + " Output camera post-place inspection is deferred until current place batch ends. " +
                        BuildWaitStateMessage() + " - Check");
                    return 0;
                }
                if (safeTimeoutMs > 0 && (DateTime.UtcNow - start).TotalMilliseconds >= safeTimeoutMs)
                {
                    string message = safeWaiter +
                        " Output camera post-place inspection idle wait timeout. timeoutMs=" + safeTimeoutMs +
                        ", elapsedMs=" + ElapsedMs(start) +
                        ", " + BuildWaitStateMessage();
                    SequenceTrace.WaitEnd("OutputPostPlaceInspectionIdle",
                        -1,
                        "waiter=" + safeWaiter,
                        "status=Timeout",
                        "timeoutMs=" + safeTimeoutMs,
                        "elapsedMs=" + ElapsedMs(start),
                        BuildWaitStateDetail());
                    return RaiseFailure("OUT-POST-INSPECT-IDLE-TIMEOUT", "OutputPostPlaceInspection", message);
                }
                if (!waitLogged)
                {
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        safeWaiter + " Output camera 후검사 완료 대기 시작. pendingOrRunning=" +
                        Volatile.Read(ref _pendingOrRunning) + " - Wait");
                    waitLogged = true;
                }
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
            if (waitLogged)
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    safeWaiter + " Output camera 후검사 완료 대기 종료. - Ok");
            }
            if (Volatile.Read(ref _failed) != 0)
            {
                SequenceTrace.WaitEnd("OutputPostPlaceInspectionIdle",
                    -1,
                    "waiter=" + safeWaiter,
                    "status=FailedAfterWait",
                    "elapsedMs=" + ElapsedMs(start),
                    BuildWaitStateDetail());
                return ReportStoredFailure(safeWaiter);
            }
            SequenceTrace.WaitEnd("OutputPostPlaceInspectionIdle",
                0,
                "waiter=" + safeWaiter,
                "status=Idle",
                "elapsedMs=" + ElapsedMs(start),
                BuildWaitStateDetail());
            return 0;
        }

        public int EnqueuePendingMaterialInspections(
            string owner,
            bool fineMove,
            int moveTimeoutMs,
            CancellationToken ct)
        {
            try
            {
                int existing = Volatile.Read(ref _pendingOrRunning);
                if (existing > 0)
                {
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "Output camera 후검사 대기/진행 요청이 이미 있어 material pending 복구 등록을 생략하고 기존 요청 완료를 기다립니다. " +
                        "owner=" + (string.IsNullOrWhiteSpace(owner) ? "-" : owner) +
                        ", pendingOrRunning=" + existing + " - Wait");
                    return existing;
                }

                int restored = 0;
                restored += EnqueuePendingMaterialInspectionsForSide(
                    BinSide.Good,
                    MaterialLocationKind.OutputStageGood,
                    owner,
                    fineMove,
                    moveTimeoutMs,
                    ct);
                restored += EnqueuePendingMaterialInspectionsForSide(
                    BinSide.Ng,
                    MaterialLocationKind.OutputStageNg,
                    owner,
                    fineMove,
                    moveTimeoutMs,
                    ct);

                if (restored > 0)
                {
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "Output camera 미완료 후검사 material pending 복구 등록 완료. owner=" +
                        (string.IsNullOrWhiteSpace(owner) ? "-" : owner) +
                        ", count=" + restored + " - Ok");
                }

                return restored;
            }
            catch (Exception ex)
            {
                RaiseFailure("OUT-POST-INSPECT-RESTORE-EX", "OutputPostPlaceInspection",
                    "Output camera 미완료 후검사 material pending 복구 중 예외가 발생했습니다. error=" +
                    ex.Message);
                return -1;
            }
        }

        private int EnqueuePendingMaterialInspectionsForSide(
            BinSide side,
            MaterialLocationKind stageLocation,
            string owner,
            bool fineMove,
            int moveTimeoutMs,
            CancellationToken ct)
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(stageLocation);
            if (wafer == null || wafer.OutputReceiveSlots == null || wafer.OutputReceiveSlots.Count == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < wafer.OutputReceiveSlots.Count; i++)
            {
                OutputReceiveSlotMaterial slot = wafer.OutputReceiveSlots[i];
                if (slot == null)
                    continue;
                if (!slot.IsTarget)
                    continue;
                if (slot.IsOutputInspectionDone)
                    continue;
                if (string.IsNullOrWhiteSpace(slot.DieUid))
                    continue;

                int result = Enqueue(
                    new OutputPostPlaceInspectionRequest
                    {
                        DieId = slot.DieUid,
                        OutputSide = side,
                        ReceiveTarget = new OutputStageReceiveTarget
                        {
                            StageLocation = stageLocation,
                            OutputWaferId = wafer.WaferId,
                            OrderIndex = slot.OrderIndex,
                            DieMapX = slot.DieMapX,
                            DieMapY = slot.DieMapY,
                            TargetX = slot.PosX,
                            TargetY = slot.PosY
                        },
                        FineMove = fineMove,
                        MoveTimeoutMs = moveTimeoutMs,
                        Owner = string.IsNullOrWhiteSpace(owner) ? "MaterialPendingRestore" : owner
                    },
                    ct);
                if (result != 0)
                    return count > 0 ? count : result;

                count++;
            }

            return count;
        }

        private string BuildWaitStateDetail()
        {
            return "pendingOrRunning=" + Volatile.Read(ref _pendingOrRunning) +
                   ",batchDepth=" + Volatile.Read(ref _batchDepth) +
                   ",workerRunning=" + Volatile.Read(ref _workerRunning) +
                   ",failed=" + Volatile.Read(ref _failed) +
                   ",queueEmpty=" + _queue.IsEmpty +
                   ",batchOwner=" + (string.IsNullOrWhiteSpace(_batchOwner) ? "-" : _batchOwner);
        }

        private string BuildWaitStateMessage()
        {
            return "pendingOrRunning=" + Volatile.Read(ref _pendingOrRunning) +
                   ", batchDepth=" + Volatile.Read(ref _batchDepth) +
                   ", workerRunning=" + Volatile.Read(ref _workerRunning) +
                   ", failed=" + Volatile.Read(ref _failed) +
                   ", queueEmpty=" + _queue.IsEmpty +
                   ", batchOwner=" + (string.IsNullOrWhiteSpace(_batchOwner) ? "-" : _batchOwner);
        }

        private static string ElapsedMs(DateTime start)
        {
            return ((int)Math.Max(0.0, (DateTime.UtcNow - start).TotalMilliseconds)).ToString();
        }

        private bool IsSameBatchOwner(string waiter)
        {
            string owner = _batchOwner;
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(waiter))
                return false;

            return waiter.IndexOf(owner, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task ProcessQueueAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    await _signal.WaitAsync(ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                    if (IsStopOrAlarmActive())
                    {
                        DrainQueuedRequests("활성 알람 상태라 Output camera 후검사 큐를 정리합니다.");
                        return;
                    }
                    if (Volatile.Read(ref _batchDepth) > 0)
                    {
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            "Output camera 후검사 묶음 등록 중이라 검사 시작을 대기합니다. depth=" +
                            Volatile.Read(ref _batchDepth) + " - Wait");
                        continue;
                    }
                    OutputPostPlaceInspectionRequest request;
                    if (_queue.TryDequeue(out request))
                    {
                        int result;
                        using (MotionGuardRuntime.BeginAutoSequenceProcessMove("OutputPostPlaceInspectionQueue"))
                        {
                            result = await InspectPlacedDieBatchAsync(request, ct).ConfigureAwait(false);
                        }
                        if (result == StopRequestedResult)
                        {
                            DrainQueuedRequests("Cycle Stop/Alarm state. Output camera post-place inspection queue is drained.");
                            return;
                        }
                        if (result != 0)
                        {
                            if (Volatile.Read(ref _failed) == 0)
                            {
                                MarkFailed("OUT-POST-INSPECT-FAILED",
                                    "Output camera 후검사 작업자가 실패 결과를 반환했습니다. result=" + result);
                            }
                            DrainQueuedRequests("Output camera 후검사 실패로 남은 요청을 정리합니다.");
                            return;
                        }
                    }
                    if (_queue.IsEmpty)
                    {
                        Interlocked.Exchange(ref _workerRunning, 0);
                        if (_queue.IsEmpty || Interlocked.CompareExchange(ref _workerRunning, 1, 0) != 0)
                            return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                DrainQueuedRequests("Output camera 후검사 큐가 취소되어 남은 요청을 정리합니다.");
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Output camera 후검사 큐가 취소되었습니다. - Failed");
            }
            catch (Exception ex)
            {
                DrainQueuedRequests("Output camera 후검사 큐 예외로 남은 요청을 정리합니다.");
                RaiseFailure("OUT-POST-INSPECT-EX", "OutputPostPlaceInspection",
                    "Output camera 후검사 큐 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _workerRunning, 0);
            }
        }

        private async Task<int> InspectPlacedDieBatchAsync(OutputPostPlaceInspectionRequest firstRequest, CancellationToken ct)
        {
            SequenceResourceLease placeLease = null;
            AutoSequenceCameraWorkZoneLease cameraWorkLease = null;
            OutputPostPlaceInspectionRequest lastRequest = null;
            bool shouldMoveVisionAvoid = false;
            int inspectedCount = 0;
            bool firstRequestCompleted = false;
            try
            {
                if (IsStopOrAlarmActive())
                {
                    CompleteRequest(firstRequest);
                    firstRequestCompleted = true;
                    return StopRequestedResult;
                }

                OutputStageUnit stage = _context.Machine != null ? _context.Machine.OutputStageUnit : null;
                if (stage == null)
                {
                    CompleteRequest(firstRequest);
                    firstRequestCompleted = true;
                    return RaiseFailure("OUT-POST-INSPECT-STAGE-MISSING", "OutputStage",
                        "Output camera 후검사를 위한 OutputStageUnit이 없습니다.");
                }
                int timeout = firstRequest != null && firstRequest.MoveTimeoutMs > 0
                    ? firstRequest.MoveTimeoutMs
                    : 10000;
                if (_context.AutoSequenceGate != null)
                {
                    cameraWorkLease = await _context.AutoSequenceGate
                        .BeginOutputCameraWorkAsync("OutputPostPlaceInspection:Batch", ct)
                        .ConfigureAwait(false);
                    if (cameraWorkLease == null)
                    {
                        CompleteRequest(firstRequest);
                        firstRequestCompleted = true;
                        return -1;
                    }

                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "Output camera work zone approved for post-place inspection batch. die=" +
                        (firstRequest != null ? firstRequest.DieId : "-") + " - Ok");
                }
                // Place 시퀀스가 모든 다이를 내려놓을 때까지 OutputPlaceArea를 정상 보유한다.
                // 후검사는 같은 영역을 이어받아야 하므로 여기서는 모션 timeout으로 실패시키지 않고
                // Stop/Alarm 취소 토큰이 들어올 때까지 기다린다.
                placeLease = await _context.Resources.AcquireAsync(
                    SequenceResourceKind.OutputPlaceArea,
                    "OutputPostPlaceInspection:Batch",
                    0,
                    ct).ConfigureAwait(false);
                if (placeLease == null)
                {
                    CompleteRequest(firstRequest);
                    firstRequestCompleted = true;
                    return -1;
                }
                OutputPostPlaceInspectionRequest request = firstRequest;
                while (request != null)
                {
                    if (IsStopOrAlarmActive())
                    {
                        CompleteRequest(request);
                        if (object.ReferenceEquals(request, firstRequest))
                            firstRequestCompleted = true;
                        DrainQueuedRequests("활성 알람 상태라 Output camera 후검사 묶음을 정리합니다.");
                        return StopRequestedResult;
                    }
                    lastRequest = request;
                    int result = -1;
                    try
                    {
                        result = await InspectPlacedDieAsync(stage, request, ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        CompleteRequest(request);
                        if (object.ReferenceEquals(request, firstRequest))
                            firstRequestCompleted = true;
                    }
                    if (result != 0)
                        return result;
                    shouldMoveVisionAvoid = true;
                    inspectedCount++;
                    OutputPostPlaceInspectionRequest next;
                    request = _queue.TryDequeue(out next) ? next : null;
                }
                if (shouldMoveVisionAvoid && lastRequest != null)
                {
                    if (IsStopOrAlarmActive())
                        return StopRequestedResult;

                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "Output camera 후검사 묶음 완료. count=" + inspectedCount +
                        ", lastDie=" + lastRequest.DieId +
                        ", lastSide=" + lastRequest.OutputSide +
                        ". 이제 OutputVisionX를 Avoid로 이동합니다. - Ok");
                    timeout = lastRequest.MoveTimeoutMs > 0 ? lastRequest.MoveTimeoutMs : 10000;
                    int result = await MoveVisionXToAvoidAsync(stage, lastRequest, timeout, ct).ConfigureAwait(false);
                    if (result != 0)
                        return result;
                }
                return 0;
            }
            catch (OperationCanceledException)
            {
                if (!firstRequestCompleted && firstRequest != null)
                    CompleteRequest(firstRequest);
                throw;
            }
            catch (SequenceStopException)
            {
                if (!firstRequestCompleted && firstRequest != null)
                    CompleteRequest(firstRequest);
                return StopRequestedResult;
            }
            catch (Exception ex)
            {
                if (!firstRequestCompleted && firstRequest != null)
                    CompleteRequest(firstRequest);
                return RaiseFailure("OUT-POST-INSPECT-BATCH-EX", "OutputPostPlaceInspection",
                    "Output camera 후검사 묶음 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                if (placeLease != null)
                    placeLease.Dispose();
                if (cameraWorkLease != null)
                    cameraWorkLease.Dispose();
            }
        }

        private async Task<int> InspectPlacedDieAsync(
            OutputStageUnit stage,
            OutputPostPlaceInspectionRequest request,
            CancellationToken ct)
        {
            SequenceResourceLease feederLease = null;
            SequenceResourceLease stageLease = null;
            try
            {
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;

                if (stage.Recipe == null)
                    return RaiseFailure("OUT-POST-INSPECT-RECIPE", "OutputStage",
                        "Output camera 후검사를 위한 OutputStage recipe가 없습니다.");
                if (request.ReceiveTarget == null)
                    return RaiseFailure("OUT-POST-INSPECT-TARGET", "Material",
                        "Output camera 후검사 대상 좌표가 없습니다. die=" + request.DieId +
                        ", side=" + request.OutputSide);
                int timeout = request.MoveTimeoutMs > 0 ? request.MoveTimeoutMs : 10000;
                SequenceResourceKind stageResource = request.OutputSide == BinSide.Ng
                    ? SequenceResourceKind.OutputNgStageArea
                    : SequenceResourceKind.OutputGoodStageArea;
                feederLease = await _context.Resources.AcquireAsync(
                    SequenceResourceKind.OutputFeederArea,
                    "OutputPostPlaceInspection:OutputFeederAvoid:" + request.OutputSide + ":" + request.DieId,
                    timeout,
                    ct).ConfigureAwait(false);
                if (feederLease == null)
                    return -1;
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                int feederReadyResult = await EnsureOutputFeederAvoidForInspectionAsync(
                    stage,
                    request,
                    timeout,
                    ct).ConfigureAwait(false);
                if (feederReadyResult != 0)
                    return feederReadyResult;
                stageLease = await _context.Resources.AcquireAsync(
                    stageResource,
                    "OutputPostPlaceInspection:" + request.OutputSide + ":" + request.DieId,
                    timeout,
                    ct).ConfigureAwait(false);
                if (stageLease == null)
                    return -1;
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                stage.Recipe.EnsurePositionObjects();
                BinStageAxis yAxis = request.OutputSide == BinSide.Ng ? BinStageAxis.NgBinY : BinStageAxis.GoodBinY;
                double baseY = request.OutputSide == BinSide.Ng
                    ? stage.Recipe.NGStageY.ProcessPosition
                    : stage.Recipe.GoodStageY.ProcessPosition;
                double targetVisionX = stage.Recipe.VisionX.ProcessPosition + request.ReceiveTarget.TargetX;
                double targetStageY = baseY + request.ReceiveTarget.TargetY;
                int readyResult = await EnsureStageReadyForInspectionAsync(
                    stage,
                    request,
                    timeout,
                    ct).ConfigureAwait(false);
                if (readyResult != 0)
                    return readyResult;
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                int result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    yAxis,
                    targetStageY,
                    request.FineMove,
                    timeout,
                    "Output camera 후검사 StageY",
                    request,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                int visionXClearResult = await WaitOutputVisionXSharedRailClearAsync(
                    stage,
                    stage.OutputCameraX,
                    targetVisionX,
                    BuildOutputPostPlaceTargetName(BinStageAxis.VisionX, "Output camera inspection VisionX", request),
                    "Output camera inspection VisionX",
                    request,
                    timeout,
                    ct).ConfigureAwait(false);
                if (visionXClearResult != 0)
                    return visionXClearResult;

                result = await MoveStageAxisAndVerifyAsync(
                    stage,
                    BinStageAxis.VisionX,
                    targetVisionX,
                    request.FineMove,
                    timeout,
                    "Output camera 후검사 VisionX",
                    request,
                    ct).ConfigureAwait(false);
                if (result != 0)
                    return result;
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                int slotIndex = request.ReceiveTarget.OrderIndex;
                InspectionResultDto inspection = await SequenceAwaiter.AwaitAsync(
                    BinVisionHelper.CheckPlacementAsync(slotIndex, timeout, ct),
                    null,
                    ct).ConfigureAwait(false);
                bool inspectionOk = inspection != null && inspection.IsPass;
                VisionOffset offset = new VisionOffset
                {
                    X = inspection != null ? inspection.OffsetX : 0.0,
                    Y = inspection != null ? inspection.OffsetY : 0.0,
                    R = inspection != null ? inspection.OffsetT : 0.0,
                    IsValid = inspection != null && inspection.HasOffset
                };
                MaterialStateService.UpdateOutputStageDieInspection(
                    request.DieId,
                    request.OutputSide,
                    request.ReceiveTarget,
                    inspectionOk,
                    offset,
                    inspection != null ? inspection.Raw : "",
                    inspection != null ? inspection.Values : null);
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Output camera 후검사 완료. die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", slotIndex=" + slotIndex +
                    ", ok=" + inspectionOk +
                    ", offsetX=" + offset.X.ToString("F6") +
                    ", offsetY=" + offset.Y.ToString("F6") +
                    ", offsetT=" + offset.R.ToString("F6") +
                    ", visionX=" + targetVisionX.ToString("F6") +
                    ", stageY=" + targetStageY.ToString("F6") + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return RaiseFailure("OUT-POST-INSPECT-EX", "OutputPostPlaceInspection",
                    "Output camera 후검사 중 예외가 발생했습니다. die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", error=" + ex.Message);
            }
            finally
            {
                if (stageLease != null)
                    stageLease.Dispose();
                if (feederLease != null)
                    feederLease.Dispose();
            }
        }

        private async Task<int> WaitOutputVisionXSharedRailClearAsync(
            OutputStageUnit stage,
            BaseAxis visionAxis,
            double target,
            string guardTargetName,
            string description,
            OutputPostPlaceInspectionRequest request,
            int timeout,
            CancellationToken ct)
        {
            try
            {
                if (stage == null || stage.OutputCameraX == null)
                    return 0;

                BaseAxis guardAxis = visionAxis ?? stage.OutputCameraX;
                if (IsAxisAlreadyInPosition(guardAxis, target))
                    return 0;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    _context != null ? _context.Machine : null);
                bool sharedRailApplicable = service != null && service.IsSharedRailAxis(stage.OutputCameraX);

                int timeoutMs = timeout > 0 ? timeout : 10000;
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;
                string reason = string.Empty;
                SequenceTrace.WaitStart("OutputVisionXSharedRailClear",
                    "target=" + target.ToString("F3"),
                    "description=" + description,
                    "die=" + (request != null ? request.DieId : "-"),
                    "side=" + (request != null ? request.OutputSide.ToString() : "-"));

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (IsStopOrAlarmActive())
                        return StopRequestedResult;

                    // Current rule: Output post-inspection VisionX waits until SharedRailX and MotionGuard are clear.
                    string sharedRailReason = string.Empty;
                    bool sharedRailClear = !sharedRailApplicable ||
                        service.VerifySingleAxisMove(stage.OutputCameraX, target, out sharedRailReason);

                    string guardReason = string.Empty;
                    bool guardClear = sharedRailClear &&
                        MotionGuardRuntime.CanAxisTeachingMove(guardAxis, target, guardTargetName, out guardReason);

                    if (sharedRailClear && guardClear)
                        break;

                    reason = !sharedRailClear
                        ? "SharedRailX: " + sharedRailReason
                        : "MotionGuard: " + guardReason;

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        SequenceTrace.WaitEnd("OutputVisionXSharedRailClear",
                            -1,
                            "status=Timeout",
                            "elapsedMs=" + elapsedMs.ToString("0"),
                            "timeoutMs=" + timeoutMs,
                            "reason=" + reason);
                        string timeoutAlarmCode = !sharedRailClear
                            ? "OUT-POST-INSPECT-SHARED-RAIL-X-TIMEOUT"
                            : "OUT-POST-INSPECT-MOTION-GUARD-TIMEOUT";
                        return RaiseFailure(timeoutAlarmCode, "OutputStage",
                            description + " wait before move timed out. " +
                            "target=" + target.ToString("F6") +
                            ", die=" + (request != null ? request.DieId : "-") +
                            ", side=" + (request != null ? request.OutputSide.ToString() : "-") +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs +
                            ", reason=" + reason);
                    }

                    if (!waitLogged)
                    {
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            description + " wait before move. " +
                            "target=" + target.ToString("F6") +
                            ", die=" + (request != null ? request.DieId : "-") +
                            ", side=" + (request != null ? request.OutputSide.ToString() : "-") +
                            ", reason=" + reason + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(20, ct).ConfigureAwait(false);
                }

                if (waitLogged)
                {
                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        description + " wait before move complete. " +
                        "target=" + target.ToString("F6") +
                        ", elapsedMs=" + elapsedMs.ToString("0") + " - Ok");
                }

                SequenceTrace.WaitEnd("OutputVisionXSharedRailClear",
                    0,
                    "status=Clear",
                    "elapsedMs=" + ((DateTime.UtcNow - start).TotalMilliseconds).ToString("0"),
                    "target=" + target.ToString("F3"));
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return RaiseFailure("OUT-POST-INSPECT-SHARED-RAIL-X-WAIT-EX", "OutputStage",
                    description + " wait before move exception. error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> EnsureStageReadyForInspectionAsync(
            OutputStageUnit stage,
            OutputPostPlaceInspectionRequest request,
            int timeout,
            CancellationToken ct)
        {
            if (IsStopOrAlarmActive())
                return StopRequestedResult;

            if (request.OutputSide == BinSide.Ng)
            {
                int goodZResult = await SequenceAwaiter.AwaitAsync(
                    stage.MoveGoodStageZToAvoidAndVerifyAsync(timeout, request.FineMove, ct),
                    -1,
                    ct).ConfigureAwait(false);
                if (goodZResult != 0)
                {
                    if (IsStopOrAlarmActive())
                        return StopRequestedResult;
                    return RaiseFailure("OUT-POST-INSPECT-GOOD-Z-AVOID", "OutputStage",
                        "NG 후검사 전 GoodStageZ Avoid 이동 실패. die=" + request.DieId +
                        ", side=" + request.OutputSide +
                        ", result=" + goodZResult + ", " +
                        stage.DescribeOutputStageInterlockState(request.OutputSide));
                }
                return 0;
            }
            if (!stage.IsNgStageInAvoidPosition())
            {
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;

                int goodZToAvoidResult = await SequenceAwaiter.AwaitAsync(
                    stage.MoveGoodStageZToAvoidAndVerifyAsync(timeout, request.FineMove, ct),
                    -1,
                    ct).ConfigureAwait(false);
                if (goodZToAvoidResult != 0)
                {
                    if (IsStopOrAlarmActive())
                        return StopRequestedResult;
                    return RaiseFailure("OUT-POST-INSPECT-GOOD-Z-AVOID", "OutputStage",
                        "Good 후검사 전 NG Stage Avoid 확보를 위한 GoodStageZ Avoid 이동 실패. die=" +
                        request.DieId + ", result=" + goodZToAvoidResult + ", " +
                        stage.DescribeOutputStageInterlockState(request.OutputSide));
                }
            }
            if (IsStopOrAlarmActive())
                return StopRequestedResult;

            int ngAvoidResult = await SequenceAwaiter.AwaitAsync(
                stage.MoveNgStageToAvoidAndVerifyAsync(timeout, request.FineMove, ct),
                -1,
                ct).ConfigureAwait(false);
            if (ngAvoidResult != 0)
            {
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                return RaiseFailure("OUT-POST-INSPECT-NG-STAGE-AVOID", "OutputStage",
                    "Good 후검사 전 NG Stage Avoid 이동 실패. die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", result=" + ngAvoidResult + ", " +
                    stage.DescribeOutputStageInterlockState(request.OutputSide));
            }
            if (IsStopOrAlarmActive())
                return StopRequestedResult;

            int goodZProcessResult = await MoveStageAxisAndVerifyAsync(
                stage,
                BinStageAxis.GoodBinZ,
                stage.Recipe.GoodStageZ.ProcessPosition,
                request.FineMove,
                timeout,
                "Good 후검사 GoodStageZ Process",
                request,
                ct).ConfigureAwait(false);
            if (goodZProcessResult != 0)
                return goodZProcessResult;
            return 0;
        }

        private async Task<int> EnsureOutputFeederAvoidForInspectionAsync(
            OutputStageUnit stage,
            OutputPostPlaceInspectionRequest request,
            int timeout,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;

                OutputFeederUnit feeder = _context.Machine != null ? _context.Machine.OutputFeederUnit : null;
                if (feeder == null)
                    return RaiseFailure("OUT-POST-INSPECT-FEEDER-MISSING", "OutputFeeder",
                        "Output camera 후검사 전 OutputFeederUnit을 확인할 수 없습니다. die=" + request.DieId +
                        ", side=" + request.OutputSide);

                if (feeder.IsBinFeederInAvoidPosition())
                    return 0;

                if (stage != null && !stage.IsVisionXInAvoidPosition())
                {
                    if (IsStopOrAlarmActive())
                        return StopRequestedResult;

                    int visionAvoid = await SequenceAwaiter.AwaitAsync(
                        stage.MoveVisionXToAvoidAndVerifyAsync(timeout, request.FineMove, ct),
                        -1,
                        ct).ConfigureAwait(false);
                    if (visionAvoid != 0)
                    {
                        if (IsStopOrAlarmActive())
                            return StopRequestedResult;
                        return RaiseFailure("OUT-POST-INSPECT-VISION-AVOID-BEFORE-FEEDER", "OutputStage",
                            "Output camera 후검사 전 OutputFeederY Avoid 이동을 위해 OutputVisionX Avoid 이동 실패. die=" +
                            request.DieId + ", side=" + request.OutputSide +
                            ", result=" + visionAvoid + ", " + stage.DescribeStageLoadMoveState(request.OutputSide));
                    }
                }

                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                int move = await SequenceAwaiter.AwaitAsync(
                    feeder.MoveToFeederAvoidPosition(request.FineMove),
                    -1,
                    ct).ConfigureAwait(false);
                if (move != 0)
                {
                    if (IsStopOrAlarmActive())
                        return StopRequestedResult;
                    return RaiseFailure("OUT-POST-INSPECT-FEEDER-AVOID", "OutputFeeder",
                        "Output camera 후검사 전 OutputFeederY Avoid 이동 명령 실패. die=" + request.DieId +
                        ", side=" + request.OutputSide +
                        ", result=" + move + ", " +
                        feeder.DescribeBinFeederYMoveDoneState() +
                        feeder.DescribeBinFeederYLastMotionFailure());
                }

                AxisMoveWaitResult waitResult = await feeder.WaitBinFeederYMoveDoneInPosition(
                    feeder.FeederY.CommandPosition,
                    timeout,
                    ct).ConfigureAwait(false);
                if (waitResult == null || !waitResult.Success || !feeder.IsBinFeederInAvoidPosition())
                {
                    if (IsStopOrAlarmActive())
                        return StopRequestedResult;
                    return RaiseFailure(AxisMoveWaiter.ResolveAlarmCode("OUT-POST-INSPECT-FEEDER-AVOID", waitResult), "OutputFeeder",
                        "Output camera 후검사 전 OutputFeederY Avoid 이동 완료/위치 확인 실패. die=" + request.DieId +
                        ", side=" + request.OutputSide +
                        ". " + AxisMoveWaiter.FormatResult(waitResult, feeder.DescribeBinFeederYMoveDoneState()) +
                        ", finalAvoid=" + feeder.IsBinFeederInAvoidPosition());
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return RaiseFailure("OUT-POST-INSPECT-FEEDER-AVOID-EX", "OutputFeeder",
                    "Output camera 후검사 전 OutputFeederY Avoid 확인 중 예외 발생. die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", error=" + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> MoveVisionXToAvoidAsync(
            OutputStageUnit stage,
            OutputPostPlaceInspectionRequest request,
            int timeout,
            CancellationToken ct)
        {
            if (IsStopOrAlarmActive())
                return StopRequestedResult;

            int result = await SequenceAwaiter.AwaitAsync(
                stage.MoveVisionXToAvoidAndVerifyAsync(timeout, request.FineMove, ct),
                -1,
                ct).ConfigureAwait(false);
            if (result != 0)
            {
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                return RaiseFailure("OUT-POST-INSPECT-VISION-AVOID", "OutputStage",
                    "Output camera 후검사 후 OutputVisionX Avoid 이동 실패. die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", result=" + result + ", " + stage.DescribeStageLoadMoveState(request.OutputSide));
            }
            return 0;
        }

        private async Task<int> MoveStageAxisAndVerifyAsync(
            OutputStageUnit stage,
            BinStageAxis axis,
            double target,
            bool fineMove,
            int timeout,
            string description,
            OutputPostPlaceInspectionRequest request,
            CancellationToken ct)
        {
            if (IsStopOrAlarmActive())
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    description + " 이동을 중단합니다. 이미 활성 알람 상태입니다. die=" +
                    request.DieId + ", side=" + request.OutputSide + " - Stopped");
                return StopRequestedResult;
            }

            string targetName = BuildOutputPostPlaceTargetName(axis, description, request);
            int result = await SequenceAwaiter.AwaitAsync(
                stage.MoveStageAxis(axis, target, fineMove, targetName),
                -1,
                ct).ConfigureAwait(false);
            if (result != 0)
            {
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                return RaiseFailure("OUT-POST-INSPECT-MOVE", "OutputStage",
                    description + " 이동 명령 실패. axis=" + axis +
                    ", target=" + target +
                    ", result=" + result +
                    ", die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ". " + stage.BuildStageAxisState(axis, target));
            }
            AxisMoveWaitResult waitResult = await stage.WaitStageAxisMoveDoneInPosition(
                axis,
                target,
                timeout,
                ct).ConfigureAwait(false);
            if (waitResult == null || !waitResult.Success)
            {
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                return RaiseFailure(AxisMoveWaiter.ResolveAlarmCode("OUT-POST-INSPECT-MOVE", waitResult), "OutputStage",
                    description + " 이동 완료/위치 확인 실패. axis=" + axis +
                    ", target=" + target +
                    ", die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ". " + AxisMoveWaiter.FormatResult(waitResult, stage.BuildStageAxisState(axis, target)));
            }
            return 0;
        }

        private static string BuildOutputPostPlaceTargetName(
            BinStageAxis axis,
            string description,
            OutputPostPlaceInspectionRequest request)
        {
            return "OutputPostPlaceInspection;" +
                   "axis=" + axis +
                   ";side=" + (request != null ? request.OutputSide.ToString() : "-") +
                   ";die=" + (request != null ? request.DieId : "-") +
                   ";desc=" + (description ?? "");
        }

        private static bool IsAxisAlreadyInPosition(BaseAxis axis, double target)
        {
            if (axis == null || axis.IsMoving || axis.IsAlarm)
                return false;

            double tolerance = axis.Config != null && axis.Config.InPositionTolerance > 0.0
                ? axis.Config.InPositionTolerance
                : 0.01;
            return Math.Abs(axis.ActualPosition - target) <= tolerance;
        }

        private int RaiseFailure(string alarmCode, string source, string message)
        {
            if (IsStopOrAlarmActive() && !IsAlarmStopActive())
            {
                Log.Write("Main", "SYSTEM", source,
                    "Cycle Stop/Stopped state. Output camera post-place inspection failure is suppressed. code=" +
                    alarmCode + ", message=" + message + " - Stopped");
                return StopRequestedResult;
            }

            MarkFailed(alarmCode, message);
            SequenceFailureStore.Record(
                "OutputPostPlaceInspection",
                "OutputInspection",
                "Run",
                alarmCode,
                source,
                message);
            Log.Write("Main", "SYSTEM", source, message + " - Failed");
            if (IsAlarmStopActive())
            {
                Log.Write("Main", "SYSTEM", source,
                    "이미 활성 알람이 있어 Output camera 후검사 후속 알람 발생을 생략합니다. code=" +
                    alarmCode + ", message=" + message + " - Suppressed");
            }
            else
            {
                AlarmManager.Raise(AlarmSeverity.Error, alarmCode, source, message);
            }
            _context.LogPublic("[OUTPUT-INSPECT] FAIL " + alarmCode + " - " + message);
            return -1;
        }

        private bool IsAlarmStopActive()
        {
            try
            {
                if (AlarmManager.HasActive)
                    return true;

                return _context != null &&
                       _context.Controller != null &&
                       _context.Controller.Status == EquipmentStatus.Alarm;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private bool IsStopOrAlarmActive()
        {
            try
            {
                if (IsAlarmStopActive())
                    return true;
                if (_context != null && _context.Controller != null)
                {
                    EquipmentStatus status = _context.Controller.Status;
                    return status == EquipmentStatus.Alarm;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private void MarkFailed(string alarmCode, string message)
        {
            Interlocked.Exchange(ref _failed, 1);
            _failureCode = alarmCode ?? "";
            _failureMessage = message ?? "";
        }
        private int ReportStoredFailure(string waiter)
        {
            string safeWaiter = string.IsNullOrWhiteSpace(waiter) ? "Unknown" : waiter;
            string alarmCode = string.IsNullOrWhiteSpace(_failureCode)
                ? "OUT-POST-INSPECT-FAILED"
                : _failureCode;
            string message = safeWaiter +
                " Output camera 후검사 실패 상태입니다. code=" + alarmCode +
                ", reason=" + _failureMessage;
            SequenceFailureStore.Record(
                "OutputPostPlaceInspection",
                "OutputInspection",
                "WaitUntilIdle",
                alarmCode,
                "OutputPostPlaceInspection",
                message);
            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection", message + " - Failed");
            return -1;
        }

        private void CompleteRequest(OutputPostPlaceInspectionRequest request)
        {
            int remaining = Interlocked.Decrement(ref _pendingOrRunning);
            if (remaining < 0)
                Interlocked.Exchange(ref _pendingOrRunning, 0);
            if (request != null)
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Output camera 후검사 요청 정리. die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", pendingOrRunning=" + Volatile.Read(ref _pendingOrRunning) + " - Ok");
            }
        }

        private void DrainQueuedRequests(string reason)
        {
            OutputPostPlaceInspectionRequest request;
            int drained = 0;
            while (_queue.TryDequeue(out request))
            {
                CompleteRequest(request);
                drained++;
            }
            if (drained > 0)
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    reason + " drained=" + drained +
                    ", pendingOrRunning=" + Volatile.Read(ref _pendingOrRunning) + " - Check");
            }
        }

        private static OutputStageReceiveTarget CloneReceiveTarget(OutputStageReceiveTarget source)
        {
            if (source == null)
                return null;
            return new OutputStageReceiveTarget
            {
                StageLocation = source.StageLocation,
                OutputWaferId = source.OutputWaferId,
                SourceWaferId = source.SourceWaferId,
                OrderIndex = source.OrderIndex,
                DieMapX = source.DieMapX,
                DieMapY = source.DieMapY,
                OffsetX = source.OffsetX,
                OffsetY = source.OffsetY,
                TargetX = source.TargetX,
                TargetY = source.TargetY
            };
        }
    }
}
