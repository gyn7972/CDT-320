using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Ajin;
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

        public bool HasPlacedDieCameraTarget { get; set; }

        public int PickerNo { get; set; }

        public PickerSequenceSide PickerSide { get; set; }

        public bool HasPickerContext { get; set; }

        /// <summary>
        /// Auto 플레이스(Conti) 등록 경로에서만 true — 촬영 후 OutputVisionX 최소 회피 허용 플래그.
        /// 복원 경로(MaterialPendingRestore)/기타 등록은 false로 기존 전체 Avoid 경로를 탄다.
        /// </summary>
        public bool MinimalRetreatEligible { get; set; }

        public double PlacedStageY { get; set; }

        public double PlacedPickerY { get; set; }

        public double OutputVisionToPickerY { get; set; }

        public bool FineMove { get; set; }

        public int MoveTimeoutMs { get; set; }

        public string Owner { get; set; } = "";

        public bool SkipInspection { get; set; }

        public VisionRequestHandle VisionRequest { get; set; }

        public InspectionResultDto InspectionResult { get; set; }
    }

    internal sealed class OutputPostPlaceInspectionQueue
    {
        // Place 시퀀스는 4-head를 모두 내려놓는 동안 OutputPlaceArea를 보유한다.
        // 후검사는 요청만 큐에 등록하고, Place가 끝나 Picker가 Output zone에서 빠진 뒤
        // 이 큐가 OutputPlaceArea를 넘겨받아 Output camera 검사와 Material 업데이트를 수행한다.
        private const int StopRequestedResult = -9001;

        private sealed class BinResultCollectionOutcome
        {
            public BinResultCollectionOutcome(int resultCode, string failureMessage)
            {
                ResultCode = resultCode;
                FailureMessage = failureMessage ?? string.Empty;
            }

            public int ResultCode { get; private set; }

            public string FailureMessage { get; private set; }
        }

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

        public bool IsIdle
        {
            get
            {
                return Volatile.Read(ref _pendingOrRunning) <= 0 &&
                       Volatile.Read(ref _batchDepth) <= 0;
            }
        }

        public bool IsSafelyIdleForDrain(out string reason)
        {
            bool safelyIdle = Volatile.Read(ref _pendingOrRunning) <= 0 &&
                              Volatile.Read(ref _batchDepth) <= 0 &&
                              Volatile.Read(ref _workerRunning) <= 0 &&
                              _queue.IsEmpty;
            reason = safelyIdle ? string.Empty : BuildWaitStateMessage();
            return safelyIdle;
        }

        public bool HasFailure
        {
            get { return Volatile.Read(ref _failed) != 0; }
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
                ", hasPlacedDieCameraTarget=" + request.HasPlacedDieCameraTarget +
                ", pickerNo=" + request.PickerNo +
                ", placedStageY=" + request.PlacedStageY.ToString("F6") +
                ", placedPickerY=" + request.PlacedPickerY.ToString("F6") +
                ", outputVisionToPickerY=" + request.OutputVisionToPickerY.ToString("F6") +
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

                string batchOwner = string.IsNullOrWhiteSpace(owner) ? "MaterialPendingRestore" : owner;
                int restored = 0;
                BeginBatch(batchOwner);
                try
                {
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
                }
                finally
                {
                    EndBatch(batchOwner);
                }

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

                DieMaterial die = MaterialStateService.GetDieMaterial(slot.DieUid);
                bool hasPickerContext = die != null &&
                    die.PickedPickerNo >= 1 && die.PickedPickerNo <= 4 &&
                    (die.PickedPickerLocation == MaterialLocationKind.PickerFront ||
                     die.PickedPickerLocation == MaterialLocationKind.PickerRear);
                PickerSequenceSide pickerSide = die != null &&
                    die.PickedPickerLocation == MaterialLocationKind.PickerRear
                    ? PickerSequenceSide.Rear
                    : PickerSequenceSide.Front;

                int result = Enqueue(
                    new OutputPostPlaceInspectionRequest
                    {
                        DieId = slot.DieUid,
                        OutputSide = side,
                        PickerNo = die != null ? die.PickedPickerNo : -1,
                        PickerSide = pickerSide,
                        HasPickerContext = hasPickerContext,
                        ReceiveTarget = new OutputStageReceiveTarget
                        {
                            StageLocation = stageLocation,
                            OutputWaferId = wafer.WaferId,
                            SourceWaferId = wafer.OutputReceiveSourceWaferId,
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
            var capturedRequests = new List<OutputPostPlaceInspectionRequest>();
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
                    capturedRequests.Add(request);
                    if (IsStopOrAlarmActive())
                    {
                        DrainQueuedRequests("활성 알람 상태라 Output camera 후검사 묶음을 정리합니다.");
                        return StopRequestedResult;
                    }
                    lastRequest = request;
                    int result = await CapturePlacedDieAsync(stage, request, ct).ConfigureAwait(false);
                    if (result != 0)
                    {
                        await DrainCapturedBinResultsAfterFailureAsync(
                            capturedRequests,
                            ct,
                            "BIN capture failure. result=" + result).ConfigureAwait(false);
                        return result;
                    }
                    shouldMoveVisionAvoid = true;
                    inspectedCount++;
                    OutputPostPlaceInspectionRequest next;
                    request = _queue.TryDequeue(out next) ? next : null;
                }

                if (shouldMoveVisionAvoid && lastRequest != null)
                {
                    if (IsStopOrAlarmActive())
                    {
                        await DrainCapturedBinResultsAfterFailureAsync(
                            capturedRequests,
                            ct,
                            "Stop/Alarm became active after all BIN EPD responses.").ConfigureAwait(false);
                        return StopRequestedResult;
                    }

                    DateTime pipelineStart = DateTime.UtcNow;
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "BIN 전체 REQ/EPD 완료. RESULT 일괄 수집/판정과 OutputVisionX Avoid 복귀를 병렬 시작합니다. count=" +
                        inspectedCount +
                        ", lastDie=" + lastRequest.DieId +
                        ", lastSide=" + lastRequest.OutputSide +
                        " - Start");
                    timeout = lastRequest.MoveTimeoutMs > 0 ? lastRequest.MoveTimeoutMs : 10000;
                    Task<int> visionAvoidTask = MoveVisionXToAvoidAsync(
                        stage,
                        lastRequest,
                        timeout,
                        ct);
                    Task<BinResultCollectionOutcome> resultCollectionTask =
                        CollectPlacedDieResultsAsync(capturedRequests, ct);

                    try
                    {
                        await Task.WhenAll(new Task[] { visionAvoidTask, resultCollectionTask })
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        await DrainCapturedBinResultsAfterFailureAsync(
                            capturedRequests,
                            ct,
                            "BIN RESULT/Avoid parallel pipeline exception.").ConfigureAwait(false);
                        throw;
                    }

                    int visionAvoidResult = await visionAvoidTask.ConfigureAwait(false);
                    BinResultCollectionOutcome collectionOutcome =
                        await resultCollectionTask.ConfigureAwait(false);
                    int collectResult = collectionOutcome != null
                        ? collectionOutcome.ResultCode
                        : -1;

                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "BIN RESULT/Avoid 병렬 배리어 완료. count=" + inspectedCount +
                        ", resultCode=" + collectResult +
                        ", avoidResult=" + visionAvoidResult +
                        ", elapsedMs=" + ElapsedMs(pipelineStart) +
                        " - " + (collectResult == 0 && visionAvoidResult == 0 ? "Ok" : "Check"));

                    if (visionAvoidResult != 0)
                    {
                        if (collectResult != 0)
                        {
                            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                                "OutputVisionX Avoid와 BIN RESULT 수집이 함께 실패했습니다. " +
                                "avoidResult=" + visionAvoidResult +
                                ", resultCode=" + collectResult +
                                ", resultFailure=" +
                                (collectionOutcome != null ? collectionOutcome.FailureMessage : "outcome missing") +
                                " - Failed");
                            await DrainCapturedBinResultsAfterFailureAsync(
                                capturedRequests,
                                ct,
                                "BIN RESULT/Avoid parallel pipeline failed.").ConfigureAwait(false);
                        }
                        return visionAvoidResult;
                    }

                    if (collectResult != 0)
                    {
                        await DrainCapturedBinResultsAfterFailureAsync(
                            capturedRequests,
                            ct,
                            "BIN RESULT collection failed after VisionX Avoid.").ConfigureAwait(false);
                        if (collectResult == StopRequestedResult)
                            return StopRequestedResult;

                        return RaiseFailure(
                            "OUT-POST-INSPECT-VISION-RESULT",
                            "Vision",
                            collectionOutcome != null && !string.IsNullOrWhiteSpace(collectionOutcome.FailureMessage)
                                ? collectionOutcome.FailureMessage
                                : "BIN 전체 측정 후 RESULT 수집에 실패했습니다. Material 결과는 갱신하지 않습니다.");
                    }
                }
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                return StopRequestedResult;
            }
            catch (Exception ex)
            {
                return RaiseFailure("OUT-POST-INSPECT-BATCH-EX", "OutputPostPlaceInspection",
                    "Output camera 후검사 묶음 처리 중 예외가 발생했습니다. error=" + ex.Message);
            }
            finally
            {
                for (int i = 0; i < capturedRequests.Count; i++)
                {
                    OutputPostPlaceInspectionRequest captured = capturedRequests[i];
                    CompleteRequest(captured);
                    if (object.ReferenceEquals(captured, firstRequest))
                        firstRequestCompleted = true;
                }
                if (!firstRequestCompleted && firstRequest != null)
                    CompleteRequest(firstRequest);
                if (placeLease != null)
                    placeLease.Dispose();
                if (cameraWorkLease != null)
                    cameraWorkLease.Dispose();
            }
        }

        private async Task<int> CapturePlacedDieAsync(
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
                double cameraToPickerY = 0.0;
                double targetStageY;
                string targetStageYFormula;
                if (request.HasPlacedDieCameraTarget)
                {
                    cameraToPickerY = request.OutputVisionToPickerY - request.PlacedPickerY;
                    targetStageY = request.PlacedStageY - cameraToPickerY;
                    targetStageYFormula =
                        "placedStageY(" + request.PlacedStageY.ToString("F6") +
                        ") - cameraToPickerY(outputVisionToPickerY(" + request.OutputVisionToPickerY.ToString("F6") +
                        ") - placedPickerY(" + request.PlacedPickerY.ToString("F6") +
                        ") = " + cameraToPickerY.ToString("F6") + ")";
                }
                else
                {
                    targetStageY = baseY + request.ReceiveTarget.TargetY;
                    targetStageYFormula =
                        "fallback baseY(" + baseY.ToString("F6") +
                        ") + receiveTargetY(" + request.ReceiveTarget.TargetY.ToString("F6") + ")";
                }
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Output camera 후검사 StageY 계산. die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", pickerNo=" + request.PickerNo +
                    ", hasPlacedDieCameraTarget=" + request.HasPlacedDieCameraTarget +
                    ", formula=" + targetStageYFormula +
                    ", targetStageY=" + targetStageY.ToString("F6") + " - Calc");
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
                // C2(return-follow): 게이트+제약(목표가 피커 페어 간격 미충족)이면 공유레일 클리어
                // 대기를 생략하고 VisionX를 퇴장하는 제약 피커 추종(follow)으로 진입한다.
                // 존이 이미 비면(2번째 이후 검사 포함) 기존 대기(인포지션 단락 포함)+일반 이동 그대로.
                // follow 실패 시 R5 폴백: 아래 기존 경로(대기+일반 이동)로 1회 재시도.
                bool followEntryUsed = false;
                if (IsMinimalRetreatGateSatisfied(request) &&
                    ShouldFollowPickerForOutputVisionEntry(stage, targetVisionX))
                {
                    int followResult = await TryFollowOutputVisionXBehindPickerAsync(
                        stage,
                        targetVisionX,
                        request,
                        ct).ConfigureAwait(false);
                    if (followResult == 0)
                    {
                        followEntryUsed = true;
                    }
                    else
                    {
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            "Output camera 후검사 VisionX 팔로잉 진입이 실패해 기존 대기+일반 이동으로 재시도합니다. " +
                            "die=" + request.DieId +
                            ", side=" + request.OutputSide +
                            ", followResult=" + followResult + " - Check");
                    }
                }

                if (!followEntryUsed)
                {
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
                }
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                if (!request.HasPickerContext || request.PickerNo < 1 || request.PickerNo > 4)
                {
                    return RaiseFailure("OUT-POST-INSPECT-PICKER-CONTEXT", "Material",
                        "BIN 신규 규약 요청에 필요한 Picker 문맥이 없습니다. die=" + request.DieId +
                        ", pickerSide=" + request.PickerSide +
                        ", pickerNo=" + request.PickerNo);
                }

                int slotIndex = request.ReceiveTarget.OrderIndex;
                int fb = request.PickerSide == PickerSequenceSide.Front ? 0 : 1;
                VisionInspectionRequestContext visionContext = VisionInspectionContextFactory.CreateAuto(
                    AutoVisionChannel.Bin,
                    VisionToolIds.Bin.PlacementInspector,
                    fb,
                    request.PickerNo,
                    slotIndex,
                    request.ReceiveTarget.DieMapX,
                    request.ReceiveTarget.DieMapY,
                    0,
                    request.DieId,
                    request.ReceiveTarget.OutputWaferId,
                    VisionInspectionOperations.Inspect,
                    VisionResultTimings.Deferred,
                    string.Empty);
                request.VisionRequest = await AutoVisionRequestService.StartInspectionRequestAsync(
                    visionContext,
                    timeout,
                    ct).ConfigureAwait(false);
                if (request.VisionRequest == null)
                {
                    return RaiseFailure("OUT-POST-INSPECT-VISION-EPD", "Vision",
                        "BIN 검사 REQ/EPD 단계가 실패했습니다. die=" + request.DieId +
                        ", side=" + request.OutputSide +
                        ", slotIndex=" + slotIndex);
                }

                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Output camera BIN REQ/EPD 완료. RESULT는 전체 측정 후 수집합니다. die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", slotIndex=" + slotIndex +
                    ", requestId=" + request.VisionRequest.Request.RequestId +
                    ", groupId=" + request.VisionRequest.Request.GroupId +
                    ", visionX=" + targetVisionX.ToString("F6") +
                    ", stageY=" + targetStageY.ToString("F6") +
                    ", cameraToPickerY=" + cameraToPickerY.ToString("F6") +
                    ", stageYFormula=" + targetStageYFormula + " - Ok");
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

        private async Task<BinResultCollectionOutcome> CollectPlacedDieResultsAsync(
            IList<OutputPostPlaceInspectionRequest> capturedRequests,
            CancellationToken ct)
        {
            var failed = new List<string>();
            for (int i = 0; i < capturedRequests.Count; i++)
            {
                // 이미 EPD가 끝난 요청은 AVOID 모션의 성공/실패와 관계없이 RESULT를 회수해
                // correlation handle이 남지 않게 한다. 실제 취소는 전달된 token으로 처리한다.
                ct.ThrowIfCancellationRequested();
                OutputPostPlaceInspectionRequest request = capturedRequests[i];
                int timeout = request.MoveTimeoutMs > 0 ? request.MoveTimeoutMs : 10000;
                VisionInspectionResult result = await AutoVisionRequestService.WaitInspectionStageAsync(
                    request.VisionRequest,
                    VisionInspectionCommands.Result,
                    timeout,
                    ct).ConfigureAwait(false);
                if (result == null || result.InspectionResult == null)
                {
                    failed.Add("die=" + request.DieId +
                               ", groupId=" + (request.VisionRequest != null && request.VisionRequest.Request != null
                                   ? request.VisionRequest.Request.GroupId
                                   : "-"));
                    continue;
                }

                request.InspectionResult = result.InspectionResult;
            }

            if (failed.Count > 0)
            {
                string failureMessage =
                    "BIN 전체 측정 후 RESULT 수집에 실패했습니다. Material 결과는 갱신하지 않습니다. failed=" +
                    string.Join("; ", failed.ToArray());
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    failureMessage + " AVOID 병렬 복귀가 끝난 뒤 실패를 확정합니다. - Check");
                return new BinResultCollectionOutcome(-1, failureMessage);
            }

            for (int i = 0; i < capturedRequests.Count; i++)
                ApplyPlacedDieResult(capturedRequests[i]);

            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "BIN 전체 REQ/EPD 완료 후 RESULT 일괄 수집 및 Material 반영 완료. count=" +
                capturedRequests.Count + " - Ok");
            return new BinResultCollectionOutcome(0, string.Empty);
        }

        private async Task DrainCapturedBinResultsAfterFailureAsync(
            IList<OutputPostPlaceInspectionRequest> capturedRequests,
            CancellationToken ct,
            string reason)
        {
            if (capturedRequests == null)
                return;

            for (int i = 0; i < capturedRequests.Count; i++)
            {
                OutputPostPlaceInspectionRequest request = capturedRequests[i];
                VisionRequestHandle handle = request != null ? request.VisionRequest : null;
                if (handle == null || handle.IsResultDone || !string.IsNullOrWhiteSpace(handle.Error))
                    continue;

                if (ct.IsCancellationRequested)
                {
                    handle.MarkError("BIN RESULT cleanup canceled. " + (reason ?? string.Empty));
                    continue;
                }

                try
                {
                    int timeout = request.MoveTimeoutMs > 0 ? request.MoveTimeoutMs : 10000;
                    VisionInspectionResult result = await AutoVisionRequestService.WaitInspectionStageAsync(
                        handle,
                        VisionInspectionCommands.Result,
                        timeout,
                        ct).ConfigureAwait(false);
                    if (result == null && string.IsNullOrWhiteSpace(handle.Error))
                        handle.MarkError("BIN RESULT cleanup failed. " + (reason ?? string.Empty));
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "BIN 실패 전 EPD 완료 RESULT 정리. die=" + (request.DieId ?? string.Empty) +
                        ", groupId=" + handle.Request.GroupId +
                        ", received=" + (result != null) +
                        ", reason=" + (reason ?? string.Empty) + " - Check");
                }
                catch (OperationCanceledException)
                {
                    handle.MarkError("BIN RESULT cleanup canceled. " + (reason ?? string.Empty));
                    break;
                }
                catch (Exception ex)
                {
                    handle.MarkError("BIN RESULT cleanup exception. " + ex.Message);
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "BIN 실패 전 RESULT 정리 예외. groupId=" + handle.Request.GroupId +
                        ", reason=" + (reason ?? string.Empty) +
                        ", error=" + ex.Message + " - Check");
                }
            }
        }

        private static void ApplyPlacedDieResult(OutputPostPlaceInspectionRequest request)
        {
            InspectionResultDto inspection = request.InspectionResult;
            bool inspectionOk = inspection != null && inspection.IsPass;
            VisionOffset offset = new VisionOffset
            {
                X = inspection != null ? inspection.OffsetX : 0.0,
                Y = inspection != null ? inspection.OffsetY : 0.0,
                R = inspection != null ? inspection.OffsetT : 0.0,
                IsValid = inspection != null && inspection.HasOffset
            };
            // Place 런타임 보정 필터 갱신.
            // IsPass == true 인 경우에만 갱신한다 — NG 판정 Die의 위치 측정은 신뢰할 수 없으므로 제외.
            if (inspection != null && inspection.HasOffset && inspectionOk &&
                request.HasPickerContext && !request.SkipInspection)
            {
                PlaceRuntimeOffsetService.OnInspectionOffset(
                    request.PickerSide,
                    request.PickerNo,
                    offset.X,
                    offset.Y,
                    offset.R,
                    request.DieId);
            }

            MaterialStateService.UpdateOutputStageDieInspection(
                request.DieId,
                request.OutputSide,
                request.ReceiveTarget,
                inspectionOk,
                offset,
                inspection != null ? inspection.Raw : string.Empty,
                inspection != null ? inspection.Values : null);
            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "Output camera BIN RESULT 반영 완료. die=" + request.DieId +
                ", side=" + request.OutputSide +
                ", slotIndex=" + (request.ReceiveTarget != null ? request.ReceiveTarget.OrderIndex : -1) +
                ", ok=" + inspectionOk +
                ", offsetX=" + offset.X.ToString("F6") +
                ", offsetY=" + offset.Y.ToString("F6") +
                ", offsetT=" + offset.R.ToString("F6") + " - Ok");
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

                double feederTolerance = feeder.FeederY != null && feeder.FeederY.Config != null && feeder.FeederY.Config.InPositionTolerance > 0.0
                    ? feeder.FeederY.Config.InPositionTolerance
                    : 0.01;
                if (feeder.FeederY.IsAtTargetPosition(feeder.Recipe.AvoidPosition, feederTolerance))
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

                // 기존 조건: 이동 후 재대기 — 현재 기준: 이동 함수가 완료를 보장하므로 재대기는 제거하고
                // Avoid 도착 안전 게이트만 유지(R4).
                if (!feeder.IsBinFeederInAvoidPosition())
                {
                    if (IsStopOrAlarmActive())
                        return StopRequestedResult;
                    return RaiseFailure("OUT-POST-INSPECT-FEEDER-AVOID-MOVE", "OutputFeeder",
                        "Output camera 후검사 전 OutputFeederY Avoid 이동 완료/위치 확인 실패. die=" + request.DieId +
                        ", side=" + request.OutputSide +
                        ". " + feeder.DescribeBinFeederYMoveDoneState() +
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

            // 촬영 종료 후 회피: 플레이스 Conti 게이트 충족 시 부호 인지 최소 회피(Extra 포함).
            // 이 시점에는 다음 플레이스 대상 좌표를 알 수 없으므로 planned 없이 피커 현재
            // Actual/Command만으로 계산한다(서비스가 자동 포함). 다음 플레이스가 시작되면
            // PickerPlaceSequence의 회피 결정부가 정확한 배치 좌표로 재계산한다.
            // 게이트 미충족/계산 실패 시 기존 전체 Avoid 경로 그대로 (동작 무변경).
            double visionTarget = 0.0;
            bool useMinimalRetreat = false;
            string retreatDetail = string.Empty;
            if (IsMinimalRetreatGateSatisfied(request) &&
                stage.Recipe != null && stage.OutputCameraX != null)
            {
                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    _context != null ? _context.Machine : null);
                if (service != null)
                {
                    stage.Recipe.EnsurePositionObjects();
                    double fullAvoid = stage.Recipe.VisionX.AvoidPosition;
                    double dynamicTarget;
                    string dynamicDetail;
                    if (service.TryResolveMinimalVisionRetreatTarget(
                        stage.OutputCameraX,
                        fullAvoid,
                        null,
                        service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0,
                        out dynamicTarget,
                        out dynamicDetail))
                    {
                        visionTarget = dynamicTarget;
                        useMinimalRetreat = true;
                        retreatDetail = dynamicDetail;
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            "BIN 촬영 종료 후 OutputVisionX 최소 회피 좌표를 확정했습니다. mode=minimal" +
                            ", target=" + visionTarget.ToString("F6") +
                            ", fullAvoid=" + fullAvoid.ToString("F6") +
                            ", die=" + request.DieId +
                            ", side=" + request.OutputSide +
                            ", detail=" + retreatDetail + " - Check");
                    }
                    else
                    {
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            "BIN 촬영 종료 후 OutputVisionX 최소 회피 계산 실패 — 전체 Avoid를 사용합니다. " +
                            "detail=" + dynamicDetail + " - Check");
                    }
                }
            }

            int result = useMinimalRetreat
                ? await SequenceAwaiter.AwaitAsync(
                    stage.MoveVisionXToTargetAndVerifyAsync(visionTarget, timeout, request.FineMove, ct),
                    -1,
                    ct).ConfigureAwait(false)
                : await SequenceAwaiter.AwaitAsync(
                    stage.MoveVisionXToAvoidAndVerifyAsync(timeout, request.FineMove, ct),
                    -1,
                    ct).ConfigureAwait(false);
            if (result != 0)
            {
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;
                return RaiseFailure("OUT-POST-INSPECT-VISION-AVOID", "OutputStage",
                    "Output camera 후검사 후 OutputVisionX Avoid 이동 실패. mode=" + (useMinimalRetreat ? "minimal" : "legacy") +
                    ", die=" + request.DieId +
                    ", side=" + request.OutputSide +
                    ", result=" + result + ", " + stage.DescribeStageLoadMoveState(request.OutputSide));
            }
            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "BIN RESULT 수집과 병렬로 OutputVisionX Avoid 이동 및 위치 확인 완료. mode=" + (useMinimalRetreat ? "minimal" : "legacy") +
                ", die=" + request.DieId +
                ", side=" + request.OutputSide + " - Ok");
            return 0;
        }

        // 플레이스 Conti 게이트(수정 2026-07-24): 요청에 명시 플래그(MinimalRetreatEligible —
        // Auto 플레이스 등록 경로에서만 설정)가 있고 픽커 컨텍스트를 가지며 해당 픽커의
        // Place.MotionMode가 ContiSegmentedPlace일 때만 최소 회피를 적용한다.
        // 복원 경로(MaterialPendingRestore) 등은 플래그가 없어 기존 전체 Avoid 경로 그대로.
        private bool IsMinimalRetreatGateSatisfied(OutputPostPlaceInspectionRequest request)
        {
            if (request == null || !request.MinimalRetreatEligible || !request.HasPickerContext)
                return false;
            if (_context == null || _context.Machine == null)
                return false;

            PickerPlaceMotionConfig config = null;
            if (request.PickerSide == PickerSequenceSide.Front &&
                _context.Machine.PickerFrontUnit != null && _context.Machine.PickerFrontUnit.Config != null)
                config = _context.Machine.PickerFrontUnit.Config.Place;
            else if (request.PickerSide == PickerSequenceSide.Rear &&
                _context.Machine.PickerRearUnit != null && _context.Machine.PickerRearUnit.Config != null)
                config = _context.Machine.PickerRearUnit.Config.Place;

            if (config == null)
                return false;

            return config.MotionMode == PickerPlaceMotionMode.ContiSegmentedPlace;
        }

        // C2(return-follow): 검사 목표가 양 피커의 Actual/Command 페어 간격을 이미 만족하면
        // 존이 빈 상태 — follow 없이 기존 대기(인포지션 단락 포함)+일반 이동을 쓴다.
        private bool ShouldFollowPickerForOutputVisionEntry(OutputStageUnit stage, double targetVisionX)
        {
            try
            {
                if (stage == null || !(stage.OutputCameraX is AjinAxis))
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    _context != null ? _context.Machine : null);
                if (service == null || _context == null || _context.Machine == null)
                    return false;

                BaseAxis frontX = _context.Machine.PickerFrontUnit != null ? _context.Machine.PickerFrontUnit.PickerX : null;
                BaseAxis rearX = _context.Machine.PickerRearUnit != null ? _context.Machine.PickerRearUnit.PickerX : null;
                string detail;
                bool frontClear = frontX == null ||
                    (service.IsPairClearanceSatisfied(frontX, frontX.ActualPosition, stage.OutputCameraX, targetVisionX, out detail) &&
                     service.IsPairClearanceSatisfied(frontX, frontX.CommandPosition, stage.OutputCameraX, targetVisionX, out detail));
                bool rearClear = rearX == null ||
                    (service.IsPairClearanceSatisfied(rearX, rearX.ActualPosition, stage.OutputCameraX, targetVisionX, out detail) &&
                     service.IsPairClearanceSatisfied(rearX, rearX.CommandPosition, stage.OutputCameraX, targetVisionX, out detail));

                return !(frontClear && rearClear);
            }
            catch
            {
                return false;
            }
        }

        // C2/R2(return-follow): 제약이 되는 선행 피커 선택 — Output(−방향 진입)은 페어 간격식
        // (비전 + HomeClearance − 피커)에서 X가 큰 피커가 여유가 작아 제약이 된다.
        private BaseAxis ResolveConstrainingPickerXForOutputVisionEntry()
        {
            BaseAxis frontX = _context != null && _context.Machine != null && _context.Machine.PickerFrontUnit != null
                ? _context.Machine.PickerFrontUnit.PickerX
                : null;
            BaseAxis rearX = _context != null && _context.Machine != null && _context.Machine.PickerRearUnit != null
                ? _context.Machine.PickerRearUnit.PickerX
                : null;
            if (frontX == null)
                return rearX;
            if (rearX == null)
                return frontX;

            return frontX.ActualPosition >= rearX.ActualPosition ? frontX : rearX;
        }

        // C2/R2/R4(return-follow): OutputVisionX(후행)가 퇴장하는 제약 피커X(선행)를 추종해 검사
        // 위치로 진입한다. 판단 로직 없이 항상 follow 시도(정지 선행축 포함) — 여유 ≤ 0이면 명령
        // 없이 대기, 피커가 끝내 안 움직이면 타임아웃(-21) → R5 폴백. 역방향 판단 없음: 피커의
        // 비전 방향 접근 이동은 피커 자신의 인터락(SafetyDistance)이 차단하고, follow는 후행축을
        // 전진만 시킨다. 미선택 피커와의 충돌은 follow 내부 이동/오버라이드가 MotionGuard
        // (SharedRailX 페어 간격 포함)를 통과하며 검증된다 — 위반 시도는 -11 → R5 폴백.
        // 간격 공식 정합: direction=−1 → 거리 = (후행 + homeGap) − 선행 = 비전 + HomeClearance − 피커
        // (기존 페어 간격식과 동일). homeGap/safetyGap/direction/timeout은 설정 런타임 조회.
        private async Task<int> TryFollowOutputVisionXBehindPickerAsync(
            OutputStageUnit stage,
            double targetVisionX,
            OutputPostPlaceInspectionRequest request,
            CancellationToken ct)
        {
            AjinAxis followVisionX = stage != null ? stage.OutputCameraX as AjinAxis : null;
            BaseAxis leadingPickerX = ResolveConstrainingPickerXForOutputVisionEntry();
            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                _context != null ? _context.Machine : null);
            if (followVisionX == null || leadingPickerX == null || service == null)
                return -1;

            int direction;
            double homeGap;
            double safetyGap;
            string gapDetail;
            if (!service.TryGetFollowGapParameters(
                stage.OutputCameraX,
                leadingPickerX,
                service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0,
                out direction,
                out homeGap,
                out safetyGap,
                out gapDetail))
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Output camera 후검사 VisionX 팔로잉 파라미터 조회에 실패해 기존 경로로 진행합니다. " +
                    "die=" + (request != null ? request.DieId : "-") +
                    ", detail=" + gapDetail + " - Check");
                return -1;
            }

            int timeoutMs = service.Config != null ? service.Config.VisionFollowEntryTimeoutMs : 15000;
            // 현재 기준: follow의 명령/오버라이드 경로는 축 레이어 자동 스케일이 없으므로 여기서 1회 스케일.
            double trailingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                stage.OutputCameraX.Config != null ? stage.OutputCameraX.Config.DefaultVelocity : 0.0);
            double trailingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                stage.OutputCameraX.Config != null ? stage.OutputCameraX.Config.Acceleration : 0.0);
            double trailingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                stage.OutputCameraX.Config != null ? stage.OutputCameraX.Config.Deceleration : 0.0);
            double leadingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.DefaultVelocity : 0.0);
            double leadingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.Acceleration : 0.0);
            double leadingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.Deceleration : 0.0);

            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "Output camera 후검사 VisionX 팔로잉 진입을 시작합니다. die=" + (request != null ? request.DieId : "-") +
                ", side=" + (request != null ? request.OutputSide.ToString() : "-") +
                ", leading=" + leadingPickerX.Name +
                ", leadingCommand=" + leadingPickerX.CommandPosition.ToString("F6") +
                ", visionTarget=" + targetVisionX.ToString("F6") +
                ", " + gapDetail +
                ", timeoutMs=" + timeoutMs + " - Start");

            // 선행 목표는 퇴장 목표를 모르므로 현재 Command(정보용)를 사용한다(스펙 확정).
            return await followVisionX.FollowMoveAsync(
                leadingPickerX,
                leadingPickerX.CommandPosition,
                leadingVelocity,
                leadingAcceleration,
                leadingDeceleration,
                targetVisionX,
                trailingVelocity,
                trailingAcceleration,
                trailingDeceleration,
                direction,
                safetyGap,
                homeGap,
                timeoutMs,
                ct).ConfigureAwait(false);
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

            ct.ThrowIfCancellationRequested();
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
