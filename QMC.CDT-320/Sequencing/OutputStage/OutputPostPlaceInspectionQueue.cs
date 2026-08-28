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

        // 다이 자신의 소스 웨이퍼 ID — Place 런타임 옵셋의 웨이퍼 단위 루프 전환 키.
        public string SourceWaferInstanceId { get; set; } = "";

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
        // F8(2026-07-26): 현재 처리 중 배치가 "EPD 완료 + 회피 시작 + lease 조기 반환" 상태에
        // 도달했는지. 1이면 다음 Place가 RESULT 수집 완료를 기다리지 않고 진입할 수 있다.
        // 다음 배치 처리 시작 시 0으로 리셋된다.
        private int _placeEntryClear;
        // C1-(b)(2026-07-26): Place 진입 대기자 수 — 배치 EPD 완료 시점에 대기자가 있으면
        // 최소 회피(잔류), 없으면 전체 Avoid 완주(FeederY 자동 이동 인터락 보존).
        private int _placeEntryWaiters;
        // [공유레일 근접 사고 후속 2026-08-18] 현재 촬영 중(in-flight) 요청의 카메라 목표 X.
        // 대기 큐(_queue)에서 이미 빠져나와 카메라가 접근 중/촬영 중인 요청은 큐 스냅샷에
        // 안 잡히므로 별도 게시한다. 배치 종료(finally)에서 NaN으로 정리. 픽커 Bottom/Side
        // 진입 게이트가 "예약 검사 목표 X 최솟값" 계산에 함께 반영한다.
        private double _activeInspectionTargetVisionX = double.NaN;

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

        // [공유레일 근접 사고 후속 2026-08-18] 읽기 전용 조회: 예약(대기 큐)+촬영 중(in-flight)
        // 검사 요청들의 카메라 목표 X 최솟값. OutputVisionX↔PickerX 페어는 카메라 X가 작을수록
        // 간격이 줄므로(간격 = HomeClearance + visionX − pickerX) 최솟값이 최악 지점이다.
        // 픽커 Bottom/Side 진입 게이트가 "카메라 현재/지령/예약 최악 X"를 합성할 때 쓴다.
        // 큐 상태는 건드리지 않는다. ConcurrentQueue 열거는 스냅샷이라 스레드 세이프.
        public bool TryGetReservedInspectionMinVisionTargetX(out double minTargetX)
        {
            minTargetX = double.NaN;
            OutputStageUnit stage = _context != null && _context.Machine != null
                ? _context.Machine.OutputStageUnit
                : null;
            if (stage == null || stage.Recipe == null || stage.Recipe.VisionX == null)
                return false;

            double processPosition = stage.Recipe.VisionX.ProcessPosition;
            double min = double.PositiveInfinity;
            foreach (OutputPostPlaceInspectionRequest pending in _queue)
            {
                if (pending == null || pending.ReceiveTarget == null)
                    continue;
                double candidate = processPosition + pending.ReceiveTarget.TargetX;
                if (candidate < min)
                    min = candidate;
            }

            double active = Volatile.Read(ref _activeInspectionTargetVisionX);
            if (!double.IsNaN(active) && active < min)
                min = active;

            if (double.IsPositiveInfinity(min))
                return false;

            minTargetX = min;
            return true;
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
            // [사용자 지시 2026-08-25] Output camera 후검사는 Good Stage만 수행한다 — NG Stage
            // 배치분은 등록하지 않는다. NG Y가 후검사 위치로 이동하지 않으므로 Place와의 스테이지
            // 경합이 줄고 택트도 개선된다. (모든 등록 경로가 이 Enqueue를 지나므로 여기 한 곳에서
            // 거른다. NG 슬롯은 IsOutputInspectionDone=false로 남지만 이 플래그는 복원 재등록과
            // UI 표시에만 쓰이고, 복원 경로도 NG 사이드를 스캔하지 않도록 함께 수정했다.)
            if (request.OutputSide == BinSide.Ng)
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "NG Stage 배치는 Output camera 후검사를 생략합니다(사용자 지시 2026-08-25 — Good만 검사). die=" +
                    request.DieId +
                    ", owner=" + request.Owner + " - Skip");
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

        /// <summary>
        /// 정상 Auto Cycle Stop 최종 배리어에서 OutputVisionX를 최소 회피가 아닌 전체 Recipe Avoid로 정리한다.
        /// 기존 camera work zone, OutputPlaceArea, SharedRailX 및 MotionGuard 경로를 그대로 사용한다.
        /// </summary>
        public async Task<int> EnsureVisionXFullAvoidForCycleStopAsync(
            string waiter,
            int timeoutMs,
            CancellationToken ct)
        {
            AutoSequenceCameraWorkZoneLease cameraWorkLease = null;
            SequenceResourceLease placeLease = null;
            string safeWaiter = string.IsNullOrWhiteSpace(waiter) ? "CycleStopFinalDrain" : waiter;
            int safeTimeoutMs = timeoutMs > 0 ? timeoutMs : 10000;
            try
            {
                ct.ThrowIfCancellationRequested();
                if (_context == null || !_context.IsCycleStopRequested)
                    return 0;
                if (IsAlarmStopActive())
                    return -1;
                if (Volatile.Read(ref _pendingOrRunning) > 0)
                {
                    return RaiseFailure(
                        "OUT-POST-INSPECT-CYCLE-STOP-NOT-IDLE",
                        "OutputPostPlaceInspection",
                        safeWaiter + " OutputVisionX 최종 Avoid 전 후검사 큐가 Idle이 아닙니다. " +
                        BuildWaitStateMessage());
                }

                OutputStageUnit stage = _context.Machine != null
                    ? _context.Machine.OutputStageUnit
                    : null;
                if (stage == null || stage.OutputCameraX == null || stage.Recipe == null)
                {
                    return RaiseFailure(
                        "OUT-POST-INSPECT-CYCLE-STOP-STAGE-MISSING",
                        "OutputStage",
                        safeWaiter + " 정상 Cycle Stop OutputVisionX 전체 Avoid 확인에 필요한 축/Recipe가 없습니다.");
                }

                stage.Recipe.EnsurePositionObjects();
                stage.OutputCameraX.UpdateStatus();
                double fullAvoid = stage.Recipe.VisionX.AvoidPosition;
                if (IsAxisAlreadyInPosition(stage.OutputCameraX, fullAvoid) &&
                    stage.OutputCameraX.IsInPosition)
                {
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        safeWaiter + " 정상 Cycle Stop OutputVisionX 전체 Avoid가 이미 확인되었습니다. " +
                        "target=" + fullAvoid.ToString("F6") + " - Ok");
                    return 0;
                }

                // 존/리소스 획득에만 제한 시간을 적용한다. 모션 발행 후에는 기존 축 완료 timeout으로 끝까지 확인한다.
                using (CancellationTokenSource acquireCts =
                    CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    acquireCts.CancelAfter(safeTimeoutMs);
                    if (_context.AutoSequenceGate != null)
                    {
                        cameraWorkLease = await _context.AutoSequenceGate
                            .BeginOutputCameraWorkAsync(
                                "OutputPostPlaceInspection:CycleStopFullAvoid",
                                acquireCts.Token)
                            .ConfigureAwait(false);
                        if (cameraWorkLease == null)
                        {
                            return RaiseFailure(
                                "OUT-POST-INSPECT-CYCLE-STOP-CAMERA-ZONE",
                                "OutputStage",
                                safeWaiter + " 정상 Cycle Stop Output camera 작업 존을 획득하지 못했습니다.");
                        }
                    }

                    placeLease = await _context.Resources.AcquireAsync(
                        SequenceResourceKind.OutputPlaceArea,
                        "OutputPostPlaceInspection:CycleStopFullAvoid",
                        safeTimeoutMs,
                        acquireCts.Token).ConfigureAwait(false);
                    if (placeLease == null)
                    {
                        return RaiseFailure(
                            "OUT-POST-INSPECT-CYCLE-STOP-PLACE-AREA",
                            "OutputStage",
                            safeWaiter + " 정상 Cycle Stop OutputPlaceArea를 획득하지 못했습니다.");
                    }
                }

                int clearResult = await WaitOutputVisionXSharedRailClearAsync(
                    stage,
                    stage.OutputCameraX,
                    fullAvoid,
                    "AvoidPosition;OutputStageStep=CycleStopFullAvoid",
                    "정상 Cycle Stop OutputVisionX 전체 Avoid",
                    null,
                    false, // 회피(탈출) 이동 — 픽커 Extra 판정을 켜면 카메라가 갇힐 수 있어 제외.
                    safeTimeoutMs,
                    ct).ConfigureAwait(false);
                if (clearResult != 0)
                    return clearResult;

                int moveResult = await SequenceAwaiter.AwaitAsync(
                    stage.MoveVisionXToAvoidAndVerifyAsync(safeTimeoutMs, false, ct),
                    -1,
                    ct).ConfigureAwait(false);
                if (moveResult != 0)
                {
                    return RaiseFailure(
                        "OUT-POST-INSPECT-CYCLE-STOP-VISION-AVOID",
                        "OutputStage",
                        safeWaiter + " 정상 Cycle Stop OutputVisionX 전체 Avoid 이동 실패. result=" + moveResult);
                }

                stage.OutputCameraX.UpdateStatus();
                if (stage.OutputCameraX.IsMoving ||
                    stage.OutputCameraX.IsAlarm ||
                    !stage.OutputCameraX.IsInPosition ||
                    !stage.IsVisionXInAvoidPosition())
                {
                    return RaiseFailure(
                        "OUT-POST-INSPECT-CYCLE-STOP-VISION-CHECK",
                        "OutputStage",
                        safeWaiter + " 정상 Cycle Stop OutputVisionX 전체 Avoid 최종 확인 실패. " +
                        "actual=" + stage.OutputCameraX.ActualPosition.ToString("F6") +
                        ", command=" + stage.OutputCameraX.CommandPosition.ToString("F6") +
                        ", target=" + fullAvoid.ToString("F6") +
                        ", moving=" + stage.OutputCameraX.IsMoving +
                        ", inPosition=" + stage.OutputCameraX.IsInPosition +
                        ", alarm=" + stage.OutputCameraX.IsAlarm);
                }

                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    safeWaiter + " 정상 Cycle Stop OutputVisionX 전체 Avoid 이동/정지 확인 완료. " +
                    "target=" + fullAvoid.ToString("F6") + " - Ok");
                return 0;
            }
            catch (OperationCanceledException)
            {
                if (ct.IsCancellationRequested)
                    throw;

                return RaiseFailure(
                    "OUT-POST-INSPECT-CYCLE-STOP-ACQUIRE-TIMEOUT",
                    "OutputStage",
                    safeWaiter + " 정상 Cycle Stop OutputVisionX 최종 Avoid 리소스 획득 시간이 초과되었습니다. " +
                    "timeoutMs=" + safeTimeoutMs);
            }
            catch (Exception ex)
            {
                return RaiseFailure(
                    "OUT-POST-INSPECT-CYCLE-STOP-EX",
                    "OutputStage",
                    safeWaiter + " 정상 Cycle Stop OutputVisionX 전체 Avoid 처리 중 예외. error=" + ex.Message);
            }
            finally
            {
                if (placeLease != null)
                    placeLease.Dispose();
                if (cameraWorkLease != null)
                    cameraWorkLease.Dispose();
            }
        }

        /// <summary>C1-(b): 현재 Place 진입 대기자가 있는지 — 배치 EPD 완료 시점 회피 목표 결정에 사용.</summary>
        public bool HasPlaceEntryWaiter
        {
            get { return Volatile.Read(ref _placeEntryWaiters) > 0; }
        }

        // 현재 기준(사용자 지시 2026-07-26): Place 임박 신호 — 다이를 보유한 픽커가 있으면 다음
        // Place가 오는 중이므로 최소 회피를 유지한다(과회피로 인한 다음 BIN 진입 지연 제거).
        // 단, 출력 스테이지 교체(receive complete) 국면이면 FeederY 자동 이동 인터락(정확 Avoid
        // 요구) 보존을 위해 전체 Avoid를 완주한다 — C1 해소책 유지. 로트말(다이 없음)도 전체 Avoid.
        private static bool HasUpcomingPlaceIntent()
        {
            try
            {
                if (MaterialStateService.IsOutputStageReceiveComplete(BinSide.Good) ||
                    MaterialStateService.IsOutputStageReceiveComplete(BinSide.Ng))
                    return false;

                for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
                {
                    if (MaterialStateService.GetDieAtPicker(MaterialLocationKind.PickerFront, pickerNo) != null)
                        return true;
                    if (MaterialStateService.GetDieAtPicker(MaterialLocationKind.PickerRear, pickerNo) != null)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // F8(2026-07-26): Place 진입 전용 대기 — 기존 WaitUntilIdleAsync(완전 유휴)와 달리
        // "현재 배치가 EPD 완료 + 회피 시작 + lease 조기 반환(PlaceEntryClear)"에 도달했고
        // 뒤에 등록된 다음 배치가 없으면(큐 비어있음 + batchDepth 0) RESULT 수집 완료를 기다리지
        // 않고 통과시킨다. 다음 배치가 이미 등록돼 있으면 그 배치의 EPD 완료까지 대기한다
        // (1-D 3단계-③ "아직 촬영 중 → 배치 마지막 EPD 완료까지만 대기"의 큐 측 구현).
        // 다른 사용처(교체/드레인 등)는 계속 WaitUntilIdleAsync를 사용한다 — 동작 무변경.
        public async Task<int> WaitUntilPlaceEntryClearAsync(string waiter, int timeoutMs, CancellationToken ct)
        {
            string safeWaiter = string.IsNullOrWhiteSpace(waiter) ? "Unknown" : waiter;
            int safeTimeoutMs = timeoutMs > 0 ? timeoutMs : 0;
            DateTime start = DateTime.UtcNow;
            bool waitLogged = false;
            Interlocked.Increment(ref _placeEntryWaiters);
            SequenceTrace.WaitStart("OutputPostPlaceInspectionPlaceEntry",
                "waiter=" + safeWaiter,
                "timeoutMs=" + safeTimeoutMs,
                BuildWaitStateDetail());
            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (Volatile.Read(ref _failed) != 0)
                    {
                        SequenceTrace.WaitEnd("OutputPostPlaceInspectionPlaceEntry",
                            -1,
                            "waiter=" + safeWaiter,
                            "status=Failed",
                            "elapsedMs=" + ElapsedMs(start),
                            BuildWaitStateDetail());
                        return ReportStoredFailure(safeWaiter);
                    }

                    if (Volatile.Read(ref _pendingOrRunning) <= 0)
                    {
                        SequenceTrace.WaitEnd("OutputPostPlaceInspectionPlaceEntry",
                            0,
                            "waiter=" + safeWaiter,
                            "status=Idle",
                            "elapsedMs=" + ElapsedMs(start),
                            BuildWaitStateDetail());
                        if (waitLogged)
                        {
                            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                                safeWaiter + " Output camera 후검사 Place 진입 대기 종료(완전 유휴). - Ok");
                        }
                        return 0;
                    }

                    // 자기 배치 예외(실장비 2026-07-26 03:52 분석 반영): 진입하려는 Place 자신이
                    // 방금 연 후검사 묶음 등록(BeginBatch, depth=1)이 이 조건에 걸려 entry-clear
                    // 경로가 무력화되고 완전 유휴(RESULT 수집 완료)까지 9.7초를 헛대기했다.
                    // 기존 WaitUntilIdleAsync의 BatchOpenDeferred와 동일하게 waiter 자신의 배치는
                    // 대기 사유에서 제외한다(다른 주체의 배치 등록은 계속 대기 — 3단계-③ 유지).
                    if (Volatile.Read(ref _placeEntryClear) != 0 &&
                        _queue.IsEmpty &&
                        (Volatile.Read(ref _batchDepth) <= 0 || IsSameBatchOwner(safeWaiter)))
                    {
                        SequenceTrace.WaitEnd("OutputPostPlaceInspectionPlaceEntry",
                            0,
                            "waiter=" + safeWaiter,
                            "status=EntryClear",
                            "elapsedMs=" + ElapsedMs(start),
                            BuildWaitStateDetail());
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            safeWaiter + " Output camera 후검사 배치 EPD 완료+회피 시작 상태로 Place 진입을 허용합니다" +
                            "(RESULT 수집은 병렬 계속). elapsedMs=" + ElapsedMs(start) + " - Ok");
                        return 0;
                    }

                    if (safeTimeoutMs > 0 && (DateTime.UtcNow - start).TotalMilliseconds >= safeTimeoutMs)
                    {
                        string message = safeWaiter +
                            " Output camera post-place inspection place-entry wait timeout. timeoutMs=" + safeTimeoutMs +
                            ", elapsedMs=" + ElapsedMs(start) +
                            ", " + BuildWaitStateMessage();
                        SequenceTrace.WaitEnd("OutputPostPlaceInspectionPlaceEntry",
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
                            safeWaiter + " Output camera 후검사 Place 진입 대기 시작(EPD 완료+회피 시작 또는 유휴까지). " +
                            BuildWaitStateMessage() + " - Wait");
                        waitLogged = true;
                    }

                    await Task.Delay(10, ct).ConfigureAwait(false);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _placeEntryWaiters);
            }
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
                    // [사용자 지시 2026-08-25] 후검사는 Good Stage만 — NG 사이드 미검사 슬롯은
                    // 복원 대상에서 제외한다(Enqueue의 NG 필터와 한 쌍).
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
                        SourceWaferInstanceId = die != null ? die.InputWaferInstanceId : "",
                        OutputSide = side,
                        PickerNo = die != null ? die.PickedPickerNo : -1,
                        PickerSide = pickerSide,
                        HasPickerContext = hasPickerContext,
                        ReceiveTarget = new OutputStageReceiveTarget
                        {
                            StageLocation = stageLocation,
                            OutputWaferId = wafer.WaferId,
                            OutputWaferInstanceId = MaterialStateService.EnsureWaferInstanceId(wafer),
                            SourceWaferId = die != null ? die.WaferID_Input : "",
                            SourceWaferInstanceId = die != null ? die.InputWaferInstanceId : "",
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
            // F8(2026-07-26): 새 배치 처리 시작 — 이전 배치의 진입 허용 상태를 닫는다.
            Interlocked.Exchange(ref _placeEntryClear, 0);
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
                // [공유레일 근접 사고 후속 2026-08-18] OutputVisionX 명령권 토큰 획득 — 픽커
                // Bottom/Side 게이트의 유휴 회피 명령과 카메라 접근/배치말 회피 명령이 교차하지
                // 않도록 배치 동안 보유한다. 평상시 무경합(유휴 회피는 예약 0건일 때만 발동,
                // 보유 시간은 회피 이동 수 초)이라 즉시 획득된다. 해제는 배치 finally.
                int commandTokenResult = await AcquireOutputVisionCommandTokenAsync(firstRequest, ct)
                    .ConfigureAwait(false);
                if (commandTokenResult != 0)
                    return commandTokenResult;

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

                    // O3(2026-07-26): 회피 목표를 배리어 밖에서 먼저 확정한다 — Place 시퀀스가
                    // 인수할 수 있도록 목표/Task를 VisionIndependentRetreatCoordinator에 발행한다.
                    double visionRetreatTarget;
                    bool visionRetreatMinimal;
                    string visionRetreatDetail;
                    ResolvePostBatchVisionRetreatTarget(
                        stage,
                        lastRequest,
                        out visionRetreatTarget,
                        out visionRetreatMinimal,
                        out visionRetreatDetail);
                    Task<int> visionAvoidTask = MoveVisionXToAvoidAsync(
                        stage,
                        lastRequest,
                        timeout,
                        visionRetreatTarget,
                        visionRetreatMinimal,
                        visionRetreatDetail,
                        ct);
                    VisionIndependentRetreatCoordinator.RegisterOutput(
                        visionAvoidTask,
                        visionRetreatTarget,
                        "OutputPostPlaceInspection:Batch");

                    // F8(2026-07-26): 배치 EPD 완료 + 회피 시작 시점에 OutputPlaceArea/카메라 존을
                    // 조기 반환한다 — 다음 Place는 RESULT 수집 완료를 기다리지 않고 3단계 진입한다.
                    // 물리 안전은 회피 방향(+Avoid) 이동 허용 규칙 + 픽커 진입 측 페어 간격
                    // 인터락 + 팔로잉 safetyGap이 담당한다. RESULT 수집은 비전 IPC뿐이라 lease가
                    // 필요 없다. finally의 이중 해제는 null 대입으로 방지.
                    if (placeLease != null)
                    {
                        placeLease.Dispose();
                        placeLease = null;
                    }
                    if (cameraWorkLease != null)
                    {
                        cameraWorkLease.Dispose();
                        cameraWorkLease = null;
                    }
                    Interlocked.Exchange(ref _placeEntryClear, 1);
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "배치 EPD 완료+회피 시작 — OutputPlaceArea/카메라 존을 조기 반환하고 Place 진입을 허용합니다. " +
                        "retreatTarget=" + visionRetreatTarget.ToString("F6") +
                        ", retreatMode=" + (visionRetreatMinimal ? "minimal" : "fullAvoid") +
                        ", placeEntryWaiters=" + Volatile.Read(ref _placeEntryWaiters) +
                        ", count=" + inspectedCount + " - Ok");

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
                // [공유레일 근접 사고 후속 2026-08-18] in-flight 목표 정리 + 명령권 토큰 반환.
                // 배치말 회피 Task는 위 병렬 배리어(Task.WhenAll)에서 완료를 기다린 뒤라
                // 이 시점에 큐 발행 카메라 모션은 남아 있지 않다. Release는 소유자 일치 시에만
                // 해제되므로 토큰 미획득 조기 반환 경로에서도 무해하다.
                Volatile.Write(ref _activeInspectionTargetVisionX, double.NaN);
                VisionIndependentRetreatCoordinator.ReleaseOutputVisionCommand(OutputVisionCommandTokenOwner);
            }
        }

        // [공유레일 근접 사고 후속 2026-08-18] OutputVisionX 명령권 토큰 소유자 이름(고정).
        private const string OutputVisionCommandTokenOwner = "OutputPostPlaceInspection:Batch";

        // [공유레일 근접 사고 후속 2026-08-18] 명령권 토큰 획득 대기 — 평상시 첫 시도에 성공해
        // 무로그 통과. 픽커 유휴 회피가 쥐고 있으면(수 초) 폴링 대기하고, 한도 초과 시 알람
        // (무언정지 금지). 대기 사유에 현재 소유자를 남긴다.
        private async Task<int> AcquireOutputVisionCommandTokenAsync(
            OutputPostPlaceInspectionRequest request,
            CancellationToken ct)
        {
            if (VisionIndependentRetreatCoordinator.TryAcquireOutputVisionCommand(OutputVisionCommandTokenOwner))
                return 0;

            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                _context != null ? _context.Machine : null);
            int timeoutMs = service != null && service.Config != null
                ? service.Config.VisionFollowEntryTimeoutMs
                : 15000;
            string holder;
            VisionIndependentRetreatCoordinator.TryGetOutputVisionCommandOwner(out holder);
            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "Output camera 후검사 배치 시작 전 OutputVisionX 명령권 토큰 대기. holder=" + (holder ?? "-") +
                ", die=" + (request != null ? request.DieId : "-") +
                ", timeoutMs=" + timeoutMs + " - Wait");

            DateTime start = DateTime.UtcNow;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (IsStopOrAlarmActive())
                    return StopRequestedResult;

                if (VisionIndependentRetreatCoordinator.TryAcquireOutputVisionCommand(OutputVisionCommandTokenOwner))
                {
                    Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                        "Output camera 후검사 배치 OutputVisionX 명령권 토큰 획득 완료. elapsedMs=" +
                        (DateTime.UtcNow - start).TotalMilliseconds.ToString("0") + " - Ok");
                    return 0;
                }

                if ((DateTime.UtcNow - start).TotalMilliseconds >= timeoutMs)
                {
                    VisionIndependentRetreatCoordinator.TryGetOutputVisionCommandOwner(out holder);
                    return RaiseFailure("OUT-POST-INSPECT-VISION-CMD-TOKEN-TIMEOUT", "OutputStage",
                        "Output camera 후검사 배치 시작 전 OutputVisionX 명령권 토큰 대기가 시간 초과되었습니다. " +
                        "holder=" + (holder ?? "-") +
                        ", die=" + (request != null ? request.DieId : "-") +
                        ", timeoutMs=" + timeoutMs);
                }

                await Task.Delay(20, ct).ConfigureAwait(false);
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
                string targetReason;
                if (!MaterialStateService.IsOutputStageReceiveTargetCurrent(
                    request.OutputSide,
                    request.ReceiveTarget,
                    out targetReason))
                {
                    return RaiseFailure("OUT-POST-INSPECT-TARGET-STALE", "Material",
                        "Output camera 후검사 시작 전에 Output Bin이 교체되었거나 대상 세대가 일치하지 않습니다. die=" +
                        request.DieId + ", side=" + request.OutputSide +
                        ", reason=" + targetReason);
                }
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
                // [공유레일 근접 사고 후속 2026-08-18] in-flight 목표 게시 — 대기 큐에서 빠진 뒤에도
                // 픽커 게이트의 "예약 최솟값" 계산에 잡히도록 한다. 배치 finally에서 NaN 정리.
                Volatile.Write(ref _activeInspectionTargetVisionX, targetVisionX);
                double cameraToPickerY = 0.0;
                // Place 후 촬영 X/Y는 항상 다이맵 기준으로 계산한다.
                // Bottom/기구/런타임 보정은 Picker Place 이동에만 쓰고, 촬영 좌표에 다시 실으면
                // 카메라가 맵 중심이 아니라 보정된 Place 궤적을 따라가므로 사용하지 않는다.
                double targetStageY = baseY + request.ReceiveTarget.TargetY;
                string targetStageYFormula =
                    "baseY(" + baseY.ToString("F6") +
                    ") + receiveTargetY(" + request.ReceiveTarget.TargetY.ToString("F6") + ")";
                if (request.HasPlacedDieCameraTarget)
                {
                    cameraToPickerY = request.OutputVisionToPickerY - request.PlacedPickerY;
                    targetStageYFormula +=
                        " [placed trajectory reference(not used): placedStageY(" + request.PlacedStageY.ToString("F6") +
                        ") - cameraToPickerY(" + cameraToPickerY.ToString("F6") +
                        ") = " + (request.PlacedStageY - cameraToPickerY).ToString("F6") + "]";
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
                // 현재 기준(사용자 지시 2026-07-25, O-3): 즉시 판정이 아니라 "검사 위치까지 한 번에
                // 도달 가능해질 때"를 기다린 뒤 follow 여부를 정한다(#18 R2 대체).
                bool followEntryUsed = false;
                if (IsMinimalRetreatGateSatisfied(request) &&
                    await WaitOutputVisionReturnFollowOpportunityAsync(
                        stage, targetVisionX, request, timeout, ct).ConfigureAwait(false))
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
                        true, // 검사 접근 이동 — 최근접 픽커 Extra 포함 간격(31mm)까지 요구.
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
            {
                string targetReason;
                OutputPostPlaceInspectionRequest request = capturedRequests[i];
                if (!MaterialStateService.IsOutputStageReceiveTargetCurrent(
                    request.OutputSide,
                    request.ReceiveTarget,
                    out targetReason))
                {
                    return new BinResultCollectionOutcome(
                        -1,
                        "BIN RESULT 반영 전에 Output Bin이 교체되었거나 대상 세대가 일치하지 않습니다. die=" +
                        request.DieId + ", side=" + request.OutputSide +
                        ", reason=" + targetReason);
                }
            }

            for (int i = 0; i < capturedRequests.Count; i++)
            {
                if (!ApplyPlacedDieResult(capturedRequests[i]))
                {
                    return new BinResultCollectionOutcome(
                        -1,
                        "BIN RESULT Material 반영이 세대/slot 일치 검증에서 차단되었습니다. die=" +
                        capturedRequests[i].DieId +
                        ", side=" + capturedRequests[i].OutputSide);
                }
            }

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

        private static bool ApplyPlacedDieResult(OutputPostPlaceInspectionRequest request)
        {
            InspectionResultDto inspection = request.InspectionResult;
            bool inspectionOk = inspection != null && inspection.IsPass;
            // inspection.OffsetX/Y는 Vision 원문 x/y(비-mm) 값이므로 Material/필터에 쓰지 않는다.
            // Material BinOffset과 Place 런타임 필터에는 mm 단위 placement offset만 저장한다.
            double placementX;
            double placementY;
            double placementT;
            bool hasPlacementX = TryReadPlacementMm(inspection, out placementX, "placement_offset_x_mm");
            bool hasPlacementY = TryReadPlacementMm(inspection, out placementY, "placement_offset_y_mm");
            bool hasPlacementT = TryReadPlacementAngle(inspection, out placementT);
            VisionOffset offset = new VisionOffset
            {
                X = hasPlacementX ? placementX : 0.0,
                Y = hasPlacementY ? placementY : 0.0,
                R = hasPlacementT ? placementT : 0.0,
                IsValid = hasPlacementX && hasPlacementY
            };
            bool materialUpdated = MaterialStateService.UpdateOutputStageDieInspection(
                request.DieId,
                request.OutputSide,
                request.ReceiveTarget,
                inspectionOk,
                offset,
                inspection != null ? inspection.Raw : string.Empty,
                inspection != null ? inspection.Values : null);
            if (!materialUpdated)
                return false;

            // Place 런타임 보정 필터는 Material/Output slot 세대 일치가 최종 확인된 뒤에만 갱신한다.
            // IsPass == true 인 경우에만 갱신한다 — NG 판정 Die의 위치 측정은 신뢰할 수 없으므로 제외.
            if (inspectionOk && offset.IsValid &&
                request.HasPickerContext && !request.SkipInspection)
            {
                PlaceRuntimeOffsetService.OnInspectionOffset(
                    request.PickerSide,
                    request.PickerNo,
                    offset.X,
                    offset.Y,
                    offset.R,
                    request.DieId,
                    request.SourceWaferInstanceId);
            }
            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "Output camera BIN RESULT 반영 완료. die=" + request.DieId +
                ", side=" + request.OutputSide +
                ", slotIndex=" + (request.ReceiveTarget != null ? request.ReceiveTarget.OrderIndex : -1) +
                ", ok=" + inspectionOk +
                ", offsetSource=placement_offset_mm" +
                ", offsetValid=" + offset.IsValid +
                ", offsetX=" + offset.X.ToString("F6") +
                ", offsetY=" + offset.Y.ToString("F6") +
                ", offsetT=" + offset.R.ToString("F6") + " - Ok");
            return true;
        }

        private static bool TryReadPlacementMm(InspectionResultDto inspection, out double value, string key)
        {
            value = 0.0;
            if (inspection == null)
                return false;

            double parsed;
            if (!inspection.TryGetDoubleValue(out parsed, key))
                return false;

            // mm 소량 보정값만 허용하고 픽셀/원문 좌표 유입은 차단한다.
            if (double.IsNaN(parsed) || double.IsInfinity(parsed) || Math.Abs(parsed) > 50.0)
                return false;

            value = parsed;
            return true;
        }

        private static bool TryReadPlacementAngle(InspectionResultDto inspection, out double value)
        {
            value = 0.0;
            if (inspection == null)
                return false;

            double parsed;
            if (!inspection.TryGetDoubleValue(out parsed, "placement_angle_deg", "placement_item_angle"))
                return false;

            if (double.IsNaN(parsed) || double.IsInfinity(parsed) || Math.Abs(parsed) > 360.0)
                return false;

            value = parsed;
            return true;
        }

        private async Task<int> WaitOutputVisionXSharedRailClearAsync(
            OutputStageUnit stage,
            BaseAxis visionAxis,
            double target,
            string guardTargetName,
            string description,
            OutputPostPlaceInspectionRequest request,
            bool enforcePickerExtraClearance,
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

                    // [공유레일 근접 사고 후속 2026-08-18] 검사 접근 전용(opt-in) 추가 판정 —
                    // 검증기 SafetyDistance(10mm)만으로는 하드웨어 리미트 도그 밴드(~17mm)에
                    // 들어갈 수 있어, 픽커 게이트와 동일한 Extra 포함 기준(10+20+1=31mm)으로
                    // 최근접 픽커와의 페어 간격을 함께 요구한다. 픽커 위치는 actual/command 중
                    // 최악값 — 픽커가 진입 "이동 중"인 경우도 지령 기준으로 잡는다.
                    // 회피/탈출 이동은 판정을 켜지 않으므로(호출부 opt-in) 카메라 갇힘이 없다.
                    string pickerExtraReason = string.Empty;
                    bool pickerExtraClear = !enforcePickerExtraClearance || !sharedRailClear || !guardClear ||
                        IsVisionTargetClearOfPickerExtraClearance(stage, service, target, out pickerExtraReason);

                    if (sharedRailClear && guardClear && pickerExtraClear)
                        break;

                    reason = !sharedRailClear
                        ? "SharedRailX: " + sharedRailReason
                        : !guardClear
                            ? "MotionGuard: " + guardReason
                            : "PickerExtra: " + pickerExtraReason;

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
                            : reason.StartsWith("PickerExtra:", StringComparison.Ordinal)
                                ? "OUT-POST-INSPECT-PICKER-EXTRA-CLEARANCE-TIMEOUT"
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

        // [공유레일 근접 사고 후속 2026-08-18] 카메라 접근 목표가 Front/Rear 픽커 각각과
        // Extra 포함 기준(페어 SafetyDistance + OutputVisionRetreatExtraClearance +
        // RetreatTargetExtraMarginMm = 31mm)을 만족하는지 판정한다.
        // - 픽커 위치는 actual/command 중 간격이 작아지는 쪽(최악값) — 진입 이동 중인 픽커도
        //   지령 기준으로 잡아 이동 중 충돌 창을 막는다.
        // - 탈출 의미론(검증기 :92~99 미러): 목표 간격이 현재 간격보다 좋아지는 이동은 통과 —
        //   판정 자체는 접근 호출부에만 켜지지만 이중 안전으로 유지한다.
        // - 파라미터 조회 실패 시 true(기존 검증기·MotionGuard 판정만으로 진행 — 동작 무변경 폴백).
        private bool IsVisionTargetClearOfPickerExtraClearance(
            OutputStageUnit stage,
            SharedRailXMotionService service,
            double target,
            out string reason)
        {
            reason = string.Empty;
            try
            {
                if (service == null || stage == null || stage.OutputCameraX == null ||
                    _context == null || _context.Machine == null)
                    return true;

                BaseAxis visionX = stage.OutputCameraX;
                for (int side = 0; side < 2; side++)
                {
                    string pickerName = side == 0 ? "Front" : "Rear";
                    BaseAxis pickerX = null;
                    if (side == 0)
                    {
                        PickerFrontUnit front = _context.Machine.PickerFrontUnit;
                        if (front != null && front.Axes.ContainsKey(PickerAxis.PickerX))
                            pickerX = front.Axes[PickerAxis.PickerX];
                    }
                    else
                    {
                        PickerRearUnit rear = _context.Machine.PickerRearUnit;
                        if (rear != null && rear.Axes.ContainsKey(PickerAxis.PickerX))
                            pickerX = rear.Axes[PickerAxis.PickerX];
                    }
                    if (pickerX == null)
                        continue;

                    int direction;
                    double homeGap;
                    double safetyGap;
                    string gapDetail;
                    if (!service.TryGetFollowGapParameters(
                        visionX,
                        pickerX,
                        service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0,
                        out direction,
                        out homeGap,
                        out safetyGap,
                        out gapDetail))
                    {
                        continue;
                    }

                    double required = safetyGap + VisionIndependentRetreatCoordinator.RetreatTargetExtraMarginMm;
                    // 최악 픽커 X: 간격식(direction<0: gap=vision+homeGap−picker)이 작아지는 쪽.
                    double pickerActual = pickerX.ActualPosition;
                    double pickerCommand = pickerX.CommandPosition;
                    double worstPicker = direction > 0
                        ? Math.Min(pickerActual, pickerCommand)
                        : Math.Max(pickerActual, pickerCommand);
                    double gapAtTarget = direction > 0
                        ? (worstPicker + homeGap) - target
                        : (target + homeGap) - worstPicker;
                    if (gapAtTarget >= required)
                        continue;

                    // 탈출 면제: 현재 간격(간격이 커 보이는 쪽 비전값 기준 = 보수적)보다
                    // 좋아지는 목표면 통과.
                    double visionActual = visionX.ActualPosition;
                    double visionCommand = visionX.CommandPosition;
                    double currentVision = direction > 0
                        ? Math.Min(visionActual, visionCommand)
                        : Math.Max(visionActual, visionCommand);
                    double gapAtCurrent = direction > 0
                        ? (worstPicker + homeGap) - currentVision
                        : (currentVision + homeGap) - worstPicker;
                    if (gapAtTarget > gapAtCurrent + 0.000001)
                        continue;

                    reason = "nearestPicker=" + pickerName +
                        ", clearanceAtTarget=" + gapAtTarget.ToString("F3") +
                        ", required=" + required.ToString("F3") +
                        ", pickerX=" + pickerActual.ToString("F3") +
                        "(cmd " + pickerCommand.ToString("F3") + ")" +
                        ", visionTarget=" + target.ToString("F3") +
                        ", clearanceAtCurrent=" + gapAtCurrent.ToString("F3");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                // 판정 실패는 기존 판정(검증기+MotionGuard)만으로 진행 — 사유만 남긴다.
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Output camera 접근 픽커 Extra 간격 판정 중 예외 — 기존 판정만으로 진행합니다. error=" +
                    ex.Message + " - Check");
                return true;
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

        // O3/C1-(b)(2026-07-26): 배치 EPD 완료 직후 회피 목표 확정 —
        // Place 진입 대기자가 있으면 최소 회피(planned 없이 피커 현재 Actual/Command만 —
        // 서비스가 자동 포함; 다음 Place가 정확 좌표로 부족분을 연장), 대기자가 없으면
        // 전체 Avoid 완주(OutputFeederY 자동 이동 인터락 "정확 Avoid 요구" 보존 — 로트말/
        // 트레이 교체/RunStart 복구 경로). 게이트 미충족/계산 실패 시도 전체 Avoid.
        private void ResolvePostBatchVisionRetreatTarget(
            OutputStageUnit stage,
            OutputPostPlaceInspectionRequest request,
            out double visionTarget,
            out bool useMinimalRetreat,
            out string retreatDetail)
        {
            visionTarget = 0.0;
            useMinimalRetreat = false;
            retreatDetail = string.Empty;

            double recipeFullAvoid = 0.0;
            if (stage != null && stage.Recipe != null)
            {
                stage.Recipe.EnsurePositionObjects();
                recipeFullAvoid = stage.Recipe.VisionX.AvoidPosition;
            }
            visionTarget = recipeFullAvoid;

            if (_context != null && _context.IsCycleStopRequested)
            {
                retreatDetail = "정상 Cycle Stop 요청으로 최소 회피를 사용하지 않고 전체 Avoid를 완주합니다.";
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "BIN 촬영 종료 후 OutputVisionX 회피 목표 확정. mode=cycleStopFullAvoid" +
                    ", target=" + recipeFullAvoid.ToString("F6") +
                    ", die=" + (request != null ? request.DieId : "-") + " - Check");
                return;
            }

            // 보강(사용자 지시 2026-07-26, "아웃풋 비전이 너무 멀리 빠짐" 해소): 대기자 카운터는
            // 존 승인 이후에야 올라 EPD 시점엔 항상 0이었다(매 배치 fullAvoid 완주 실측 — 다음
            // BIN 진입이 11.5초까지 늘어난 직접 원인). 다이 보유 픽커 기반 임박 신호를 병행한다.
            if (!HasPlaceEntryWaiter && !HasUpcomingPlaceIntent())
            {
                retreatDetail = "Place 진입 대기자/임박 신호가 없어 전체 Avoid를 완주합니다(C1: FeederY 자동 이동 인터락 보존).";
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "BIN 촬영 종료 후 OutputVisionX 회피 목표 확정. mode=fullAvoid, " +
                    "detail=" + retreatDetail +
                    ", die=" + (request != null ? request.DieId : "-") + " - Check");
                return;
            }

            if (IsMinimalRetreatGateSatisfied(request) &&
                stage != null && stage.Recipe != null && stage.OutputCameraX != null)
            {
                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    _context != null ? _context.Machine : null);
                if (service != null)
                {
                    double fullAvoid = recipeFullAvoid;
                    double dynamicTarget;
                    string dynamicDetail;
                    // 회피 목표 마진(사용자 승인 2026-07-26, 4번): Extra에 +1mm — 다음 Place의
                    // 팔로잉 최종 간격 경계치 해소(팔로잉 gap은 무변경).
                    if (service.TryResolveMinimalVisionRetreatTarget(
                        stage.OutputCameraX,
                        fullAvoid,
                        null,
                        (service.Config != null ? service.Config.OutputVisionRetreatExtraClearance : 40.0) +
                        VisionIndependentRetreatCoordinator.RetreatTargetExtraMarginMm,
                        out dynamicTarget,
                        out dynamicDetail))
                    {
                        visionTarget = dynamicTarget;
                        useMinimalRetreat = true;
                        retreatDetail = dynamicDetail;

                        // 회피 no-op 방지(사용자 승인 2026-07-26): 이 시점 계산은 피커의 "현재"
                        // 위치만 장애물로 보므로, 다음 피커가 아직 사이드 촬영 존에 있으면 요구
                        // 좌표가 이미 충족돼 "현재 위치 유지(0mm 이동)"로 해소된다. 그러면 실제
                        // 회피가 다음 Place의 연장 회피까지 밀려 비전이 스테이지 위에 5~7초
                        // 잔류했다(실장비 2026-07-26 4개 배치 전부). Place가 게시한 "진입 요구
                        // 좌표"까지 미리 물러나 사이드 촬영과 병렬로 회피를 끝낸다 — 총 이동량은
                        // 같고 시점만 앞당겨지며, 정확 좌표 차이는 기존 연장 회피가 흡수한다.
                        double placeEntryTarget;
                        string placeEntryOwner;
                        if (VisionIndependentRetreatCoordinator.TryGetOutputPlaceEntryTarget(
                                out placeEntryTarget, out placeEntryOwner))
                        {
                            // 회피 방향으로만 확장하고 전체 Avoid를 넘지 않는다.
                            double extended = fullAvoid >= visionTarget
                                ? Math.Min(fullAvoid, Math.Max(visionTarget, placeEntryTarget))
                                : Math.Max(fullAvoid, Math.Min(visionTarget, placeEntryTarget));
                            if (Math.Abs(extended - visionTarget) > 0.000001)
                            {
                                retreatDetail = retreatDetail +
                                    " Place 진입 요구 좌표까지 선회피로 확장(" +
                                    visionTarget.ToString("F6") + "→" + extended.ToString("F6") +
                                    ", owner=" + (placeEntryOwner ?? "-") + ").";
                                visionTarget = extended;
                            }
                        }

                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            "BIN 촬영 종료 후 OutputVisionX 최소 회피 좌표를 확정했습니다. mode=minimal" +
                            ", target=" + visionTarget.ToString("F6") +
                            ", fullAvoid=" + fullAvoid.ToString("F6") +
                            ", die=" + (request != null ? request.DieId : "-") +
                            ", side=" + (request != null ? request.OutputSide.ToString() : "-") +
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
        }

        private async Task<int> MoveVisionXToAvoidAsync(
            OutputStageUnit stage,
            OutputPostPlaceInspectionRequest request,
            int timeout,
            double visionTarget,
            bool useMinimalRetreat,
            string retreatDetail,
            CancellationToken ct)
        {
            if (IsStopOrAlarmActive())
                return StopRequestedResult;

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

        #region OutputVision 검사 복귀 — PickerX 선행 Follow

        // SAFETY CONTRACT:
        // - Conti Place 대상에서 PickerX가 선행하고 OutputVisionX가 후행한다.
        // - 제약 Picker와 반대편 Picker를 함께 검사하며, 복귀 유지간격을 Output Retreat Extra와 혼동하지 않는다.
        // - Vision 후행축의 targetName 미지정과 Follow 실패 후 일반 이동 폴백은 현재 의도된 계약이다.

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

        // 기존 조건(2026-07-25 Command 게이트): 제약 피커의 CommandPosition이 검사 목표를
        //   safetyGap까지 "한 번에" 열어줄 때까지 출발하지 않았다. 그러나 CommandPosition은 이동
        //   명령의 최종 목표가 아니라 보드 순시 프로파일 값이라(B1 알람 실측 증명) 이 게이트는
        //   "피커가 물리적으로 길을 다 열 때까지 출발 금지"로 동작했고, 비전이 추종 없이 뒤늦게
        //   일반 이동하는 모습이 됐다(실장비 2026-07-26 사용자 관측).
        // 현재 기준(사용자 승인 2026-07-26): (Input 측 WaitInputVisionReturnFollowOpportunityAsync 미러)
        //   (a) 목표가 양 피커 Actual/Command 페어 간격을 이미 만족 → false(기존 대기+일반 이동)
        //   (b) 제약 피커 실측 위치 기준 전진 여유(slack)가 있고 간격이 실제로 벌어지는 중
        //       (20ms 샘플 간 간격 확대 + 선행축 IsMoving = 퇴장 이동 감지) → true(즉시 추종 진입).
        //       중간 creep·간격 유지는 FollowMoveAsync(safetyGap=페어 SafetyDistance — Extra 제외, 2026-07-30)가 담당한다.
        //   (c) 피커 정지/작업 중(간격 불변·축소)이면 출발하지 않는다 — "정지 피커 앞 선진입 금지"
        //       (사용자 지시 2026-07-25)는 이 조건이 보존한다. 타임아웃 시 false —
        //       기존 WaitOutputVisionXSharedRailClearAsync + 일반 이동 경로에 위임한다
        //       (신규 알람/Fail 코드를 만들지 않는다).
        //   반대(비제약) 피커 간섭은 follow 내부 MotionGuard(SharedRailX 페어 간격 포함)가 -11로
        //   거부해 기존 폴백(대기+일반 이동)이 받는다 — 기존과 동일.

        // 팔로잉 출발 판정 임계: 최소 전진 여유 / 20ms 샘플 간 간격 확대 감지(엔코더 노이즈 여유).
        private const double FollowStartMinSlackMm = 0.5;
        private const double FollowStartGapOpeningEpsilonMm = 0.02;
        private async Task<bool> WaitOutputVisionReturnFollowOpportunityAsync(
            OutputStageUnit stage,
            double targetVisionX,
            OutputPostPlaceInspectionRequest request,
            int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                if (stage == null || !(stage.OutputCameraX is AjinAxis))
                    return false;

                SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                    _context != null ? _context.Machine : null);
                if (service == null || _context == null || _context.Machine == null)
                    return false;

                string dieId = request != null ? request.DieId : "-";
                string sideText = request != null ? request.OutputSide.ToString() : "-";
                DateTime start = DateTime.UtcNow;
                bool waitLogged = false;
                // 직전 폴링의 페어 간격 — 간격 확대(퇴장 이동) 감지용.
                // 제약 피커가 바뀌면(페어 HomeClearance가 다를 수 있음) 기준이 점프하므로 리셋한다.
                double lastGap = double.NaN;
                BaseAxis lastConstrainingPickerX = null;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    if (IsStopOrAlarmActive())
                        return false;

                    // (a) 존이 이미 비었으면 follow 없이 기존 대기+일반 이동으로 진행한다.
                    if (!ShouldFollowPickerForOutputVisionEntry(stage, targetVisionX))
                    {
                        if (waitLogged)
                        {
                            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                                "Output camera 후검사 피커 존이 비어 팔로잉 없이 기존 대기+일반 이동으로 진행합니다. " +
                                "die=" + dieId +
                                ", side=" + sideText +
                                ", target=" + targetVisionX.ToString("F6") + " - Ok");
                        }

                        return false;
                    }

                    BaseAxis constrainingPickerX = ResolveConstrainingPickerXForOutputVisionEntry(
                        stage.OutputCameraX);
                    if (constrainingPickerX == null)
                        return false;

                    // 제약 피커가 바뀌면 페어가 달라져 gapNow 기준이 불연속으로 점프한다 —
                    // 그 점프를 "간격 확대"로 오탐하지 않도록 직전 샘플을 버린다.
                    if (!ReferenceEquals(constrainingPickerX, lastConstrainingPickerX))
                    {
                        lastGap = double.NaN;
                        lastConstrainingPickerX = constrainingPickerX;
                    }

                    int direction;
                    double homeGap;
                    double safetyGap;
                    string gapDetail;
                    if (!service.TryGetFollowGapParameters(
                        stage.OutputCameraX,
                        constrainingPickerX,
                        // 기존 조건: 회피 Extra(40)를 진입 유지갭에도 더해 safetyGap=50 — 피커가 검사/플레이스
                        //           대역에 있는 동안 비전 접근 한계가 피커±20mm뿐이라 스톨/왕복을 만들었다.
                        // 현재 기준(사용자 승인 2026-07-30): 진입 유지갭 = 페어 SafetyDistance + 경계여유 2mm.
                        //           Extra(40)는 제외하되, 실시간 간격 가드가 clearance<=required(등호 포함)에서
                        //           정지하므로 목표가 정지선 위에 정확히 얹히지 않게 2mm를 띄운다
                        //           (실장비 2026-07-30 01:xx, 유지갭=10 진입이 등호 정지 알람 유발 — 재발 방지).
                        //           회피 깊이 계산의 Extra는 기존 유지 — 진입 게이트/한계/제약/팔로잉 4곳 동일 적용.
                        2.0,
                        out direction,
                        out homeGap,
                        out safetyGap,
                        out gapDetail))
                    {
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            "Output camera 후검사 VisionX 팔로잉 파라미터 조회에 실패해 기존 경로로 진행합니다. " +
                            "die=" + dieId +
                            ", side=" + sideText +
                            ", detail=" + gapDetail + " - Check");
                        return false;
                    }

                    // (b) 출발 판정: 제약 피커 "실측 위치" 대비 전진 여유(slack)가 있고,
                    //     간격이 실제로 벌어지는 중(퇴장 이동 감지)이면 즉시 추종 진입한다.
                    //     FollowMoveAsync와 동일한 페어식 — direction<0: (후행+homeGap)−선행.
                    double leadingActual = constrainingPickerX.ActualPosition;
                    double visionActual = stage.OutputCameraX.ActualPosition;
                    double gapNow = direction > 0
                        ? (leadingActual + homeGap) - visionActual
                        : (visionActual + homeGap) - leadingActual;
                    double startSlack = gapNow - safetyGap;
                    bool gapOpening = !double.IsNaN(lastGap) &&
                        gapNow > lastGap + FollowStartGapOpeningEpsilonMm;
                    lastGap = gapNow;

                    if (startSlack >= FollowStartMinSlackMm && gapOpening && constrainingPickerX.IsMoving)
                    {
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            "Output camera 후검사 제약 피커 퇴장(간격 확대)을 감지해 팔로잉 진입을 진행합니다. " +
                            "die=" + dieId +
                            ", side=" + sideText +
                            ", leading=" + constrainingPickerX.Name +
                            ", leadingActual=" + leadingActual.ToString("F6") +
                            ", visionActual=" + visionActual.ToString("F6") +
                            ", gapNow=" + gapNow.ToString("F6") +
                            ", slack=" + startSlack.ToString("F6") +
                            ", safetyGap=" + safetyGap.ToString("F6") + " - Ok");

                        return true;
                    }

                    // (c) 전진 여유가 없거나 피커 정지/진입 중 — 출발하지 않고 대기한다
                    //     (정지 피커 앞 선진입 방지, 사용자 지시 2026-07-25 보존).
                    if (!waitLogged)
                    {
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            "Output camera 후검사 제약 피커 퇴장(간격 확대) 감지를 대기합니다. " +
                            "die=" + dieId +
                            ", side=" + sideText +
                            ", target=" + targetVisionX.ToString("F6") +
                            ", leading=" + constrainingPickerX.Name +
                            ", leadingActual=" + leadingActual.ToString("F6") +
                            ", gapNow=" + gapNow.ToString("F6") +
                            ", slack=" + startSlack.ToString("F6") +
                            ", safetyGap=" + safetyGap.ToString("F6") + " - Wait");
                        waitLogged = true;
                    }

                    double elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    if (elapsedMs >= timeoutMs)
                    {
                        Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                            "Output camera 후검사 팔로잉 기회 대기가 타임아웃되어 기존 대기+일반 이동으로 위임합니다. " +
                            "die=" + dieId +
                            ", side=" + sideText +
                            ", elapsedMs=" + elapsedMs.ToString("0") +
                            ", timeoutMs=" + timeoutMs + " - Check");
                        return false;
                    }

                    await Task.Delay(20, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Output camera 후검사 팔로잉 기회 판정 중 예외가 발생해 기존 경로로 진행합니다. " +
                    "die=" + (request != null ? request.DieId : "-") +
                    ", error=" + ex.Message + " - Check");
                return false;
            }
        }

        // 기존 조건(C2/R2): 원시 X 최대값으로 제약 피커를 골랐다 — 두 페어의 HomeClearance가
        //   같을 때만 최소 간격과 동치다(현 실장비 설정은 525/525 대칭이라 결과가 같다).
        // 현재 기준(사용자 지시 2026-07-26): Input 미러 — 페어식 한계가 더 불리한 쪽을 고른다.
        //   조회 실패 시 기존 원시 X 기준으로 폴백한다(동작 무변경).
        private BaseAxis ResolveConstrainingPickerXForOutputVisionEntry(BaseAxis visionAxis)
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

            SharedRailXMotionService service = SharedRailXMotionRuntime.ResolveService(
                _context != null ? _context.Machine : null);
            double frontBound;
            double rearBound;
            int frontDirection;
            int rearDirection;
            if (visionAxis != null && service != null &&
                TryResolveOutputVisionEntryBound(service, visionAxis, frontX, out frontBound, out frontDirection) &&
                TryResolveOutputVisionEntryBound(service, visionAxis, rearX, out rearBound, out rearDirection) &&
                frontDirection == rearDirection)
            {
                // direction>0: 상한이 작은 쪽 / direction<0: 하한이 큰 쪽이 더 불리하다.
                if (frontDirection > 0)
                    return frontBound <= rearBound ? frontX : rearX;
                return frontBound >= rearBound ? frontX : rearX;
            }

            return frontX.ActualPosition >= rearX.ActualPosition ? frontX : rearX;
        }

        // 페어식으로 "비전이 갈 수 있는 한계 좌표"를 구한다(팔로잉 유지 간격 safetyGap 기준).
        private bool TryResolveOutputVisionEntryBound(
            SharedRailXMotionService service,
            BaseAxis visionAxis,
            BaseAxis pickerAxis,
            out double bound,
            out int direction)
        {
            bound = 0.0;
            direction = 0;
            if (service == null || visionAxis == null || pickerAxis == null)
                return false;

            double homeGap;
            double safetyGap;
            string detail;
            if (!service.TryGetFollowGapParameters(
                visionAxis,
                pickerAxis,
                2.0, // 진입 유지갭 = SafetyDistance + 경계여유 2mm(2026-07-30) — 게이트/팔로잉과 동일 기준.
                out direction,
                out homeGap,
                out safetyGap,
                out detail))
            {
                return false;
            }

            double pickerActual = pickerAxis.ActualPosition;
            bound = direction > 0
                ? pickerActual + homeGap - safetyGap
                : pickerActual - homeGap + safetyGap;
            return true;
        }

        // 2안(사용자 승인 2026-07-26): 선행축이 아닌 반대편 피커를 FollowMoveAsync의 추가 제약으로
        // 넘긴다 — 팔로잉이 축 하나만 보는 구조라, 이 목록이 없으면 반대편 피커에 대해서는
        // 명령 인터락의 SafetyDistance(Extra 미포함)만 남아 그 앞까지 파고들어 주차된다.
        private IList<AjinAxis.FollowConstraint> BuildOppositePickerFollowConstraints(
            SharedRailXMotionService service,
            BaseAxis visionAxis,
            BaseAxis leadingPickerX)
        {
            if (service == null || visionAxis == null || leadingPickerX == null ||
                _context == null || _context.Machine == null)
            {
                return null;
            }

            BaseAxis frontX = _context.Machine.PickerFrontUnit != null
                ? _context.Machine.PickerFrontUnit.PickerX
                : null;
            BaseAxis rearX = _context.Machine.PickerRearUnit != null
                ? _context.Machine.PickerRearUnit.PickerX
                : null;
            BaseAxis oppositePickerX = ReferenceEquals(leadingPickerX, frontX) ? rearX : frontX;
            if (oppositePickerX == null || ReferenceEquals(oppositePickerX, leadingPickerX))
                return null;

            int direction;
            double homeGap;
            double safetyGap;
            string detail;
            if (!service.TryGetFollowGapParameters(
                visionAxis,
                oppositePickerX,
                2.0, // 진입 유지갭 = SafetyDistance + 경계여유 2mm(2026-07-30) — 게이트/한계/팔로잉과 동일 기준.
                out direction,
                out homeGap,
                out safetyGap,
                out detail))
            {
                Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                    "Output camera 후검사 반대편 피커 팔로잉 제약 조회에 실패해 선행축 제약만 사용합니다. " +
                    "opposite=" + oppositePickerX.Name +
                    ", detail=" + detail + " - Check");
                return null;
            }

            return new List<AjinAxis.FollowConstraint>
            {
                new AjinAxis.FollowConstraint(oppositePickerX, homeGap, safetyGap, direction)
            };
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
            BaseAxis leadingPickerX = ResolveConstrainingPickerXForOutputVisionEntry(
                stage != null ? stage.OutputCameraX : null);
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
                2.0, // 진입 유지갭 = SafetyDistance + 경계여유 2mm(2026-07-30) — 게이트/한계/제약과 동일 기준.
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

            // [팔로잉 시작 게이트 2026-08-27, 팀장님 승인] 거리 = 선행 픽커 유닛의 OUTPUT SAFETY OFFSET
            // (0 이하 = 비활성). 지연 전용 — 통과/해제/타임아웃 모두 아래 팔로잉으로 그대로 진행한다.
            double gateDistanceMm = 0.0;
            var gateMachine = _context != null ? _context.Machine : null;
            if (gateMachine != null && gateMachine.PickerFrontUnit != null &&
                ReferenceEquals(leadingPickerX, gateMachine.PickerFrontUnit.PickerX))
                gateDistanceMm = gateMachine.PickerFrontUnit.Setup != null
                    ? gateMachine.PickerFrontUnit.Setup.OutputSafetyOffset : 0.0;
            else if (gateMachine != null && gateMachine.PickerRearUnit != null &&
                ReferenceEquals(leadingPickerX, gateMachine.PickerRearUnit.PickerX))
                gateDistanceMm = gateMachine.PickerRearUnit.Setup != null
                    ? gateMachine.PickerRearUnit.Setup.OutputSafetyOffset : 0.0;
            await SharedRailXMotionService.WaitFollowStartGateAsync(
                stage.OutputCameraX,
                leadingPickerX,
                targetVisionX,
                direction,
                homeGap,
                safetyGap,
                gateDistanceMm,
                "OutputPostPlaceInspection",
                "Output camera 후검사 VisionX",
                ct).ConfigureAwait(false);

            // C2(2026-07-26): 타임아웃은 100% 기준 설정값이므로 속도 스케일 역수로 확장한다(저속 오탐 -21 방지).
            int timeoutMs = MotionSpeedScale.ScaleDefaultTimeoutMs(
                service.Config != null ? service.Config.VisionFollowEntryTimeoutMs : 15000);
            // 현재 기준: follow의 명령/오버라이드 경로는 축 레이어 자동 스케일이 없으므로 여기서 1회 스케일.
            double trailingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                stage.OutputCameraX.Config != null ? stage.OutputCameraX.Config.GetRawDefaultVelocity() : 0.0);
            double trailingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                stage.OutputCameraX.Config != null ? stage.OutputCameraX.Config.GetRawAcceleration() : 0.0);
            double trailingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                stage.OutputCameraX.Config != null ? stage.OutputCameraX.Config.GetRawDeceleration() : 0.0);
            double leadingVelocity = MotionSpeedScale.ApplyDefaultVelocityScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.GetRawDefaultVelocity() : 0.0);
            double leadingAcceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.GetRawAcceleration() : 0.0);
            double leadingDeceleration = MotionSpeedScale.ApplyDefaultAccelerationScale(
                leadingPickerX.Config != null ? leadingPickerX.Config.GetRawDeceleration() : 0.0);

            IList<AjinAxis.FollowConstraint> additionalConstraints =
                BuildOppositePickerFollowConstraints(service, stage.OutputCameraX, leadingPickerX);

            Log.Write("Main", "SYSTEM", "OutputPostPlaceInspection",
                "Output camera 후검사 VisionX 팔로잉 진입을 시작합니다. die=" + (request != null ? request.DieId : "-") +
                ", side=" + (request != null ? request.OutputSide.ToString() : "-") +
                ", leading=" + leadingPickerX.Name +
                ", leadingCommand=" + leadingPickerX.CommandPosition.ToString("F6") +
                ", visionTarget=" + targetVisionX.ToString("F6") +
                ", " + gapDetail +
                ", constraints=" + (additionalConstraints != null ? additionalConstraints.Count : 0) +
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
                // trailingTargetName 미지정(후행축이 OutputVisionX라 Picker 존 규칙과 무관) —
                // 오버라이드는 기존 "PositionOverride" 폴백을 그대로 쓴다. 동작 무변경.
                ct: ct,
                additionalConstraints: additionalConstraints).ConfigureAwait(false);
        }

        #endregion

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
                OutputWaferInstanceId = source.OutputWaferInstanceId,
                SourceWaferId = source.SourceWaferId,
                SourceWaferInstanceId = source.SourceWaferInstanceId,
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
