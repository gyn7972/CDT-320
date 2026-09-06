using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;
using QMC.Common.Alarms;
using QMC.Common.Diagnostics.TactTime;

namespace QMC.CDT320.Sequencing
{
    /// <summary>등록된 유닛 시퀀스를 선택 옵션에 따라 병렬 실행하는 오케스트레이터입니다.</summary>
    public class AutoSequenceCoordinator
    {
        #region 등록 및 실행 구성

        private readonly MachineSequenceContext _ctx;
        private readonly Dictionary<SequenceUnitKind, Func<UnitSequenceBase>> _factories =
            new Dictionary<SequenceUnitKind, Func<UnitSequenceBase>>();
        private readonly Dictionary<SequenceUnitKind, UnitSequenceBase> _active =
            new Dictionary<SequenceUnitKind, UnitSequenceBase>();
        private const int AbortPendingWaitLogIntervalMs = 1000;
        private const int CycleStopPendingWaitTimeoutMs = 5000;
        private const int PendingAbortFinishTimeoutMs = 3000;
        private const int CycleStopPrefetchExitTimeoutMs = 5000;
        private const int CycleStopPreInspectionDrainTimeoutMs = 30000;
        private const int CycleStopOutputInspectionDrainTimeoutMs = 120000;
        private const int CycleStopOutputVisionAvoidTimeoutMs = 15000;
        private const int CycleStopResultFileFlushTimeoutMs = 30000;
        private const int CycleStopAllAxesStoppedTimeoutMs = 30000;
        private CancellationTokenSource _childrenCts;
        private SequenceRunOptions _options = SequenceRunOptions.FullAuto();

        /// <summary>지정한 시퀀스 컨텍스트로 Coordinator를 생성합니다.</summary>
        public AutoSequenceCoordinator(MachineSequenceContext ctx)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        /// <summary>유닛 종류와 유닛 시퀀스 팩토리를 등록합니다.</summary>
        public void Register(SequenceUnitKind kind, Func<UnitSequenceBase> factory)
        {
            if (kind == SequenceUnitKind.None)
                throw new ArgumentException("등록할 유닛 종류가 필요합니다.", nameof(kind));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factories[kind] = factory;
        }

        /// <summary>실행 옵션에 따라 활성 유닛 시퀀스를 구성합니다.</summary>
        public void Configure(SequenceRunOptions options)
        {
            _options = options ?? SequenceRunOptions.FullAuto();
            bool autoPickerRequested = _options.Mode == SequenceRunMode.Auto &&
                                       ((_options.Units & SequenceUnitKind.PickerFront) == SequenceUnitKind.PickerFront ||
                                        (_options.Units & SequenceUnitKind.PickerRear) == SequenceUnitKind.PickerRear);
            bool autoOutputRequested = (_options.Units & SequenceUnitKind.OutputUnloader) ==
                                       SequenceUnitKind.OutputUnloader;
            if (autoPickerRequested && !autoOutputRequested)
            {
                throw new InvalidOperationException(
                    "Picker Auto run에는 OutputStage Ready/Full 교체를 담당할 OutputUnloader가 필요합니다. " +
                    "units=" + _options.Units);
            }

            _ctx.ResetCycleStopRequest();
            _ctx.WaferCompletion.Configure(
                _options.Mode == SequenceRunMode.Auto &&
                AppSettingsStore.Current.WaferCompleteRunMode == WaferCompleteRunMode.StopAfterDrain);
            if (_ctx.AutoSequenceGate != null)
                _ctx.AutoSequenceGate.ConfigureRun(_options.Units, _options.Mode);
            _active.Clear();

            foreach (var item in _factories)
            {
                if ((_options.Units & item.Key) != item.Key)
                    continue;

                var sequence = item.Value();
                sequence.Configure(_options.Mode);
                _active[item.Key] = sequence;
            }

            _ctx.LogPublic("[SEQ] Configure units=" + _options.Units + ", mode=" + _options.Mode +
                           ", active=" + _active.Count);
        }

        #endregion

        #region Coordinator 실행 수명주기

        /// <summary>활성 유닛 시퀀스를 병렬로 실행하고 모든 유닛 종료를 대기합니다.</summary>
        public async Task RunAsync(CancellationToken ct)
        {
            using (TactTimeScope tactScope = _ctx.Tact.BeginScope(
                TactTimeCategory.Run,
                "Machine",
                "AutoSequenceCoordinator",
                "Run",
                _options != null ? _options.Mode.ToString() : ""))
            {
                if (_active.Count == 0)
                {
                    _ctx.LogPublic("[SEQ] 실행할 활성 유닛이 없습니다.");
                    tactScope.Skip("실행할 활성 유닛이 없습니다.");
                    return;
                }

                ResetCoordinatorRunState();

                // §4: 이번 run의 크로스-픽커 첫 전진 우선순위 상태를 초기화한다(신규 시작/재시작 순서 게이트).
                PickerFirstForwardSequencer.BeginRun();
                // Input die vision Wait 재시도 카운터 초기화(사용자 확정 2026-07-29) — 자동 운전 시작 시점.
                InputDieVisionWaitRetryStore.ClearAll("AutoStart");
                // [Cycle Stop 래치 2026-08-27, 팀장님 승인 A안] 이전 run(비상정지 등)의 독립 회피
                // 실패 래치 리셋 — 래치는 이번 run의 실패 은폐 방지용이며 run 경계를 넘겨 유지하지 않는다.
                VisionIndependentRetreatCoordinator.ResetInputRetreatFailure("AutoStart");
                PickerFirstForwardSequencer.ConfigureActiveSides(
                    IsPickerSideActive(PickerSequenceSide.Front),
                    IsPickerSideActive(PickerSequenceSide.Rear));
                ConfigureRestartPickerDrain();
                await RestorePendingOutputPostPlaceInspectionAsync(ct).ConfigureAwait(false);

                CancellationTokenSource childrenCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _childrenCts = childrenCts;
                CancellationToken childrenToken = childrenCts.Token;
                var unitTasks = new List<Task>();
                foreach (var sequence in _active.Values)
                {
                    Task unitTask = Task.Run(() => sequence.RunAsync(childrenToken), childrenToken);
                    PendingSequenceTaskRegistry.Track(unitTask, "Unit:" + sequence.Name);
                    unitTasks.Add(unitTask);
                }

                CancellationTokenSource waferMonitorCts = null;
                Task waferMonitorTask = null;
                if (_ctx.WaferCompletion.Enabled)
                {
                    waferMonitorCts = CancellationTokenSource.CreateLinkedTokenSource(childrenToken);
                    CancellationToken waferMonitorToken = waferMonitorCts.Token;
                    waferMonitorTask = Task.Run(
                        () => _ctx.WaferCompletion.RunMonitorAsync(waferMonitorToken),
                        waferMonitorToken);
                    PendingSequenceTaskRegistry.Track(waferMonitorTask, "WaferCompletionMonitor");
                }

                // Input Vision Prefetch 러너: Auto + VisionConfig 플래그 ON + 픽커 유닛 활성 시에만 기동.
                // 촬영 오버랩(픽커 Bottom/Place 중 선행검사) 기동 판단 전용 루프 — waferMonitor와 동일한 배선 패턴.
                CancellationTokenSource prefetchCts = null;
                Task prefetchTask = null;
                bool prefetchFrontActive = IsPickerSideActive(PickerSequenceSide.Front);
                bool prefetchRearActive = IsPickerSideActive(PickerSequenceSide.Rear);
                if (_options != null &&
                    _options.Mode == SequenceRunMode.Auto &&
                    (prefetchFrontActive || prefetchRearActive) &&
                    InputVisionPrefetchRunner.IsEnabled(_ctx))
                {
                    prefetchCts = CancellationTokenSource.CreateLinkedTokenSource(childrenToken);
                    CancellationToken prefetchToken = prefetchCts.Token;
                    prefetchTask = Task.Run(
                        () => InputVisionPrefetchRunner.RunAsync(_ctx, prefetchFrontActive, prefetchRearActive, prefetchToken),
                        prefetchToken);
                    PendingSequenceTaskRegistry.Track(prefetchTask, "InputVisionPrefetchRunner");
                    _ctx.LogPublic("[SEQ] Input Vision Prefetch 러너를 시작합니다. front=" + prefetchFrontActive +
                                   ", rear=" + prefetchRearActive);
                }

                _ctx.LogPublic("[SEQ] Run start (unitTasks=" + unitTasks.Count +
                               ", waferCompletionMonitor=" + (waferMonitorTask != null) + ")");
                QMC.Common.Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                    "Coordinator run start. unitTasks=" + unitTasks.Count +
                    ", waferCompletionMonitor=" + (waferMonitorTask != null) + " - Start");
                try
                {
                    await WaitUnitsWithWaferMonitorAsync(
                        unitTasks,
                        waferMonitorTask,
                        childrenToken).ConfigureAwait(false);
                    _ctx.LogPublic("[SEQ] Run complete");
                    tactScope.Complete();
                }
                catch (SequenceStopException)
                {
                    _ctx.LogPublic("[SEQ] Run stopped");
                    tactScope.Stop("", "시퀀스가 Cycle Stop 경계에서 정지되었습니다.");
                    await AwaitPendingAfterCycleStopAsync(unitTasks, false).ConfigureAwait(false);
                    await WaitInputVisionPrefetchRunnerAfterCycleStopAsync(
                        prefetchTask,
                        childrenToken).ConfigureAwait(false);
                    await CompleteNormalCycleStopDrainAsync(childrenToken).ConfigureAwait(false);
                    throw;
                }
                catch (OperationCanceledException) when (
                    _ctx != null &&
                    _ctx.IsCycleStopRequested &&
                    _options != null &&
                    _options.Mode == SequenceRunMode.Auto &&
                    !ct.IsCancellationRequested &&
                    _ctx.Controller != null &&
                    _ctx.Controller.Status != EquipmentStatus.Alarm)
                {
                    _ctx.LogPublic("[SEQ] Cycle Stop용 중단 가능 대기가 깨어나 최종 drain 경로로 전환됩니다.");
                    tactScope.Stop("", "Cycle Stop 대기 해제 후 최종 drain을 수행합니다.");
                    await AwaitPendingAfterCycleStopAsync(unitTasks, false).ConfigureAwait(false);
                    await WaitInputVisionPrefetchRunnerAfterCycleStopAsync(
                        prefetchTask,
                        childrenToken).ConfigureAwait(false);
                    await CompleteNormalCycleStopDrainAsync(childrenToken).ConfigureAwait(false);
                    throw new SequenceStopException(
                        "CYCLE STOP 요청으로 중단 가능 대기를 해제하고 검사·저장·전축 정지 배리어를 완료했습니다.");
                }
                catch (OperationCanceledException)
                {
                    _ctx.LogPublic("[SEQ] Run canceled");
                    tactScope.Cancel("시퀀스가 취소되었습니다.");
                    AbortChildren();
                    await AwaitPendingAfterAbortAsync(unitTasks).ConfigureAwait(false);
                    throw;
                }
                catch (Exception ex)
                {
                    tactScope.Fail("", ex.Message);
                    throw;
                }
                finally
                {
                    await StopInputVisionPrefetchRunnerAsync(
                        prefetchCts,
                        prefetchTask).ConfigureAwait(false);

                    await StopWaferCompletionMonitorAsync(
                        waferMonitorCts,
                        waferMonitorTask).ConfigureAwait(false);

                    if (_childrenCts == childrenCts)
                        _childrenCts = null;

                    childrenCts.Dispose();
                }
            }
        }

        private async Task WaitUnitsWithWaferMonitorAsync(
            List<Task> unitTasks,
            Task waferMonitorTask,
            CancellationToken ct)
        {
            Task unitCompletionTask = WaitAllOrCancelOnFirstFailureAsync(unitTasks, ct);
            if (waferMonitorTask == null)
            {
                await unitCompletionTask.ConfigureAwait(false);
                return;
            }

            Task first = await Task.WhenAny(unitCompletionTask, waferMonitorTask).ConfigureAwait(false);
            if (first == waferMonitorTask && waferMonitorTask.IsFaulted)
            {
                Exception monitorError = waferMonitorTask.Exception != null
                    ? waferMonitorTask.Exception.GetBaseException()
                    : null;
                _ctx.LogPublic("[SEQ] WaferCompletion 감시 작업 실패로 유닛 시퀀스를 취소합니다. error=" +
                               (monitorError != null ? monitorError.Message : "unknown"));
                QMC.Common.Log.Write("Main", "SYSTEM", "WaferCompletionRun",
                    "Wafer completion monitor failed. error=" +
                    (monitorError != null ? monitorError.Message : "unknown") + " - Failed");
                AbortChildren();
                try
                {
                    await unitCompletionTask.ConfigureAwait(false);
                }
                catch
                {
                }

                await waferMonitorTask.ConfigureAwait(false);
                return;
            }

            if (first == waferMonitorTask && waferMonitorTask.IsCanceled && !ct.IsCancellationRequested)
            {
                AbortChildren();
                try
                {
                    await unitCompletionTask.ConfigureAwait(false);
                }
                catch
                {
                }

                throw new InvalidOperationException("WaferCompletion 감시 작업이 예기치 않게 취소되었습니다.");
            }

            await unitCompletionTask.ConfigureAwait(false);
            if (waferMonitorTask.IsFaulted)
                await waferMonitorTask.ConfigureAwait(false);
        }

        private async Task StopInputVisionPrefetchRunnerAsync(
            CancellationTokenSource prefetchCts,
            Task prefetchTask)
        {
            if (prefetchTask == null)
            {
                if (prefetchCts != null)
                    prefetchCts.Dispose();
                return;
            }

            try
            {
                if (!prefetchTask.IsCompleted &&
                    prefetchCts != null &&
                    !prefetchCts.IsCancellationRequested)
                {
                    _ctx.LogPublic("[SEQ] 유닛 시퀀스 종료 후 Input Vision Prefetch 러너를 종료합니다.");
                    prefetchCts.Cancel();
                }

                await prefetchTask.ConfigureAwait(false);
                QMC.Common.Log.Write("Main", "SYSTEM", "InputVisionPrefetchRunner",
                    "Input Vision Prefetch 러너 종료. canceled=False - Ok");
            }
            catch (OperationCanceledException)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputVisionPrefetchRunner",
                    "Input Vision Prefetch 러너 종료. canceled=True - Stopped");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputVisionPrefetchRunner",
                    "Input Vision Prefetch 러너 종료 실패. error=" + ex.Message + " - Failed");
            }
            finally
            {
                if (prefetchCts != null)
                    prefetchCts.Dispose();
            }
        }

        private async Task WaitInputVisionPrefetchRunnerAfterCycleStopAsync(
            Task prefetchTask,
            CancellationToken ct)
        {
            if (prefetchTask == null)
                return;

            ct.ThrowIfCancellationRequested();
            if (!prefetchTask.IsCompleted)
            {
                _ctx.LogPublic("[SEQ] 정상 Cycle Stop 신규 Input Vision 선행검사 차단 완료를 기다립니다.");
                Task timeoutTask = Task.Delay(CycleStopPrefetchExitTimeoutMs, ct);
                Task completed = await Task.WhenAny(prefetchTask, timeoutTask).ConfigureAwait(false);
                if (completed != prefetchTask)
                {
                    ct.ThrowIfCancellationRequested();
                    throw new TimeoutException(
                        "정상 Cycle Stop Input Vision Prefetch 러너 종료 시간이 초과되었습니다. timeoutMs=" +
                        CycleStopPrefetchExitTimeoutMs);
                }
            }

            await prefetchTask.ConfigureAwait(false);
            QMC.Common.Log.Write("Main", "SYSTEM", "InputVisionPrefetchRunner",
                "정상 Cycle Stop 신규 선행검사 차단 및 Prefetch 러너 종료 확인 완료. - Ok");
        }

        /// <summary>
        /// 정상 Auto Cycle Stop을 CycleStopped로 전달하기 전의 최종 완료 배리어다.
        /// 검사/저장/모션 실패는 SequenceStopException으로 숨기지 않고 일반 실패로 상위에 전파한다.
        /// </summary>
        private async Task CompleteNormalCycleStopDrainAsync(CancellationToken ct)
        {
            if (_ctx == null || !_ctx.IsCycleStopRequested)
                return;
            if (_options == null || _options.Mode != SequenceRunMode.Auto)
                return;
            if (_ctx.Controller == null || _ctx.Controller.Status == EquipmentStatus.Alarm)
                throw new InvalidOperationException("정상 Cycle Stop 최종 drain 중 장비 Alarm 상태가 감지되었습니다.");

            _ctx.LogPublic("[SEQ] 정상 Cycle Stop 최종 drain을 시작합니다. " +
                           "Input 선행검사 → Output 후검사 → 데이터 저장 → 전축 정지 순서로 확인합니다.");
            QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                "Normal cycle stop final drain started. - Start");

            int inputInspectionResult = await InputCameraPreInspectionCoordinator.WaitUntilIdleAsync(
                "AutoSequenceCoordinator:CycleStopFinalDrain",
                CycleStopPreInspectionDrainTimeoutMs,
                ct).ConfigureAwait(false);
            if (inputInspectionResult != 0)
            {
                throw new InvalidOperationException(
                    "정상 Cycle Stop InputCamera 선행검사 안전 종료 확인 실패. result=" + inputInspectionResult);
            }

            int inputPrePositionResult = await InputVisionXPrePositionCoordinator.WaitUntilIdleAsync(
                "AutoSequenceCoordinator:CycleStopFinalDrain",
                CycleStopPreInspectionDrainTimeoutMs,
                ct).ConfigureAwait(false);
            if (inputPrePositionResult != 0)
            {
                throw new InvalidOperationException(
                    "정상 Cycle Stop InputVisionX 선행이동 안전 종료 확인 실패. result=" + inputPrePositionResult);
            }

            int inputRetreatResult = await VisionIndependentRetreatCoordinator
                .WaitInputRetreatsUntilIdleAsync(
                    "AutoSequenceCoordinator:CycleStopFinalDrain",
                    CycleStopPreInspectionDrainTimeoutMs,
                    ct).ConfigureAwait(false);
            if (inputRetreatResult != 0)
            {
                throw new InvalidOperationException(
                    "정상 Cycle Stop InputVisionX 독립 회피 완료 확인 실패. result=" + inputRetreatResult);
            }

            if (_ctx.OutputPostPlaceInspections == null)
                throw new InvalidOperationException("정상 Cycle Stop Output 후검사 큐를 확인할 수 없습니다.");

            int outputIdleResult = await _ctx.OutputPostPlaceInspections.WaitUntilIdleAsync(
                "AutoSequenceCoordinator:CycleStopFinalDrain",
                CycleStopOutputInspectionDrainTimeoutMs,
                ct).ConfigureAwait(false);
            if (outputIdleResult != 0)
            {
                throw new InvalidOperationException(
                    "정상 Cycle Stop OutputCameraX 후검사/RESULT/Material 반영 완료 대기 실패. result=" +
                    outputIdleResult);
            }

            string pickerSafeReason = _ctx.WaferCompletion == null
                ? "WaferCompletion=null"
                : string.Empty;
            if (_ctx.WaferCompletion == null ||
                !_ctx.WaferCompletion.AreAllEnabledPickersAvoidAndStopped(out pickerSafeReason))
            {
                throw new InvalidOperationException(
                    "정상 Cycle Stop Picker 전체 고정 Avoid/정지 최종 확인 실패. reason=" +
                    (pickerSafeReason ?? "unknown"));
            }

            int outputVisionAvoidResult = await _ctx.OutputPostPlaceInspections
                .EnsureVisionXFullAvoidForCycleStopAsync(
                    "AutoSequenceCoordinator:CycleStopFinalDrain",
                    CycleStopOutputVisionAvoidTimeoutMs,
                    ct).ConfigureAwait(false);
            if (outputVisionAvoidResult != 0)
            {
                throw new InvalidOperationException(
                    "정상 Cycle Stop OutputCameraX 전체 Recipe Avoid 완료 확인 실패. result=" +
                    outputVisionAvoidResult);
            }

            if (!MaterialStateService.TryFlushPendingSave("AutoCycleStopFinalDrain"))
                throw new InvalidOperationException("정상 Cycle Stop Material 상태 파일 저장 완료 확인 실패.");

            if (!PlaceRuntimeOffsetService.TryFlushPendingSave("AutoCycleStopFinalDrain"))
                throw new InvalidOperationException("정상 Cycle Stop Place 런타임 오프셋 저장 완료 확인 실패.");

            int resultFileFlush = await VisionInspectionResultFileWriter.WaitUntilFlushedAsync(
                "AutoCycleStopFinalDrain",
                CycleStopResultFileFlushTimeoutMs,
                ct).ConfigureAwait(false);
            if (resultFileFlush != 0)
                throw new InvalidOperationException("정상 Cycle Stop 검사 결과 CSV/Raw 저장 완료 확인 실패.");

            int allAxesStopped = await _ctx.Controller.WaitUntilAllAxesStoppedAsync(
                "AutoCycleStopFinalDrain",
                CycleStopAllAxesStoppedTimeoutMs,
                ct).ConfigureAwait(false);
            if (allAxesStopped != 0)
                throw new InvalidOperationException("정상 Cycle Stop 전체 축 완전 정지 확인 실패.");

            QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                "Normal cycle stop final drain completed. " +
                "inputInspection=Idle, outputInspection=Idle, picker=FullAvoid, outputVisionX=FullAvoid, " +
                "material=Durable, inspectionFiles=Durable, runtimeOffset=Durable, allAxes=Stopped - Ok");
            _ctx.LogPublic("[SEQ] 정상 Cycle Stop 최종 drain 완료. 검사·데이터 저장·모든 축 정지를 확인했습니다.");
        }

        private async Task StopWaferCompletionMonitorAsync(
            CancellationTokenSource waferMonitorCts,
            Task waferMonitorTask)
        {
            if (waferMonitorTask == null)
            {
                if (waferMonitorCts != null)
                    waferMonitorCts.Dispose();
                return;
            }

            try
            {
                if (!waferMonitorTask.IsCompleted &&
                    waferMonitorCts != null &&
                    !waferMonitorCts.IsCancellationRequested)
                {
                    _ctx.LogPublic("[SEQ] 유닛 시퀀스 종료 후 WaferCompletion 감시 작업을 종료합니다.");
                    waferMonitorCts.Cancel();
                }

                await waferMonitorTask.ConfigureAwait(false);
                QMC.Common.Log.Write("Main", "SYSTEM", "WaferCompletionRun",
                    "Wafer completion monitor terminated. canceled=False - Ok");
            }
            catch (OperationCanceledException)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "WaferCompletionRun",
                    "Wafer completion monitor terminated. canceled=True - Stopped");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "WaferCompletionRun",
                    "Wafer completion monitor termination failed. error=" + ex.Message + " - Failed");
            }
            finally
            {
                if (waferMonitorCts != null)
                    waferMonitorCts.Dispose();
            }
        }

        #endregion

        #region 실행 시작 상태 복원

        private void ResetCoordinatorRunState()
        {
            try
            {
                if (_ctx.PickerPhases != null)
                    _ctx.PickerPhases.ResetAll();

                if (_ctx.AutoSequenceGate != null)
                    _ctx.AutoSequenceGate.ResetPickerWorkZones("RunStart");

                InputCameraPreInspectionCoordinator.Clear(PickerSequenceSide.Front);
                InputCameraPreInspectionCoordinator.Clear(PickerSequenceSide.Rear);
                // C3(2026-07-26): 직전 런의 독립 회피 세션 잔여분 정리(RunStart 경계).
                VisionIndependentRetreatCoordinator.CancelInput(PickerSequenceSide.Front, "RunStart reset");
                VisionIndependentRetreatCoordinator.CancelInput(PickerSequenceSide.Rear, "RunStart reset");
                VisionIndependentRetreatCoordinator.ClearOutput("RunStart reset");
                _ctx.LogPublic("[SEQ] InputCamera pre-inspection state reset at run start.");

                string clearDetail;
                if (QMC.CDT320.Interlocks.PickerZoneInterlockRules.ClearPickerWorkAreasForReadyIfSafe(
                    _ctx.Machine,
                    out clearDetail))
                {
                    _ctx.LogPublic("[SEQ] Picker work area counters reset at run start. " + clearDetail);
                }
                else if (!string.IsNullOrWhiteSpace(clearDetail))
                {
                    _ctx.LogPublic("[SEQ] Picker work area counters kept at run start. " + clearDetail);
                }
            }
            catch (Exception ex)
            {
                _ctx.LogPublic("[SEQ] Coordinator run state reset failed. error=" + ex.Message);
                QMC.Common.Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                    "Coordinator run state reset failed. error=" + ex.Message + " - Check");
            }
        }

        private void ConfigureRestartPickerDrain()
        {
            try
            {
                bool frontRequired;
                int frontRank;
                string frontReason;
                PickerResumeDrainFifoKey frontFifoKey;
                ResolveRestartPickerDrain(PickerSequenceSide.Front, out frontRequired, out frontRank, out frontReason, out frontFifoKey);

                bool rearRequired;
                int rearRank;
                string rearReason;
                PickerResumeDrainFifoKey rearFifoKey;
                ResolveRestartPickerDrain(PickerSequenceSide.Rear, out rearRequired, out rearRank, out rearReason, out rearFifoKey);

                PickerFirstForwardSequencer.ConfigureResumeDrain(
                    frontRequired,
                    frontRank,
                    frontFifoKey,
                    rearRequired,
                    rearRank,
                    rearFifoKey);

                // [선입선출 2026-08-13] 확정 순서·판정 근거(tiebreak=SequenceNo|PickedAt|FrontFallback)를
                // configure 시점에 남긴다 — 실런 1회로 "왜 이 픽커가 먼저인가"를 확정하기 위한 계측.
                string drainOrderDetail = PickerFirstForwardSequencer.GetResumeDrainConfigureDetail();

                _ctx.LogPublic("[SEQ] Picker restart drain configured. front=" +
                    frontRequired + "(" + frontReason + "), rear=" +
                    rearRequired + "(" + rearReason + "), " + drainOrderDetail);
                QMC.Common.Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                    "Picker restart drain configured. frontRequired=" + frontRequired +
                    ", frontRank=" + frontRank +
                    ", frontReason=" + frontReason +
                    ", frontFifoKey=" + PickerFirstForwardSequencer.DescribeFifoKey(frontFifoKey) +
                    ", rearRequired=" + rearRequired +
                    ", rearRank=" + rearRank +
                    ", rearReason=" + rearReason +
                    ", rearFifoKey=" + PickerFirstForwardSequencer.DescribeFifoKey(rearFifoKey) +
                    ", " + drainOrderDetail + " - Check");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                    "Picker restart drain configure failed. error=" + ex.Message + " - Failed");
            }
        }

        private async Task RestorePendingOutputPostPlaceInspectionAsync(CancellationToken ct)
        {
            try
            {
                if (_ctx == null || _ctx.OutputPostPlaceInspections == null)
                    return;

                int restored = _ctx.OutputPostPlaceInspections.EnqueuePendingMaterialInspections(
                    "AutoSequenceCoordinator:RunStart",
                    false,
                    10000,
                    ct);
                if (restored < 0)
                    throw new InvalidOperationException("Run start pending Output camera post-place inspection restore failed. result=" + restored);
                if (restored == 0)
                    return;

                _ctx.LogPublic("[SEQ] Run start 이전 Output camera 미완료 후검사를 먼저 처리합니다. count=" +
                               restored);
                int idleResult = await _ctx.OutputPostPlaceInspections.WaitUntilIdleAsync(
                    "AutoSequenceCoordinator:RunStartPendingOutputInspection",
                    0,
                    ct).ConfigureAwait(false);
                if (idleResult != 0)
                    throw new InvalidOperationException("Run start pending Output camera post-place inspection failed. result=" + idleResult);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                    "Run start pending Output camera post-place inspection restore failed. error=" +
                    ex.Message + " - Failed");
                throw;
            }
        }

        private void ResolveRestartPickerDrain(
            PickerSequenceSide side,
            out bool required,
            out int rank,
            out string reason,
            out PickerResumeDrainFifoKey fifoKey)
        {
            required = false;
            rank = PickerFirstForwardSequencer.RankPickUp;
            reason = "no picker work";
            fifoKey = new PickerResumeDrainFifoKey();

            if (!IsPickerSideActive(side))
            {
                reason = "picker side inactive";
                return;
            }

            bool hasPickerDie = false;
            bool hasTargetPickerDie = false;
            int targetDieCount = 0;
            // [선입선출 2026-08-13] PickedAt 유효 기준은 MaterialStateCompactor와 동일(1900-01-01 23:59:59 초과).
            // GetPickerDieSortTime류는 UpdatedAt이 섞여 FIFO 비교가 오염되므로 die.PickedAt 원본만 사용한다.
            DateTime pickedAtValidThreshold = new DateTime(1900, 1, 1, 23, 59, 59);
            MaterialLocationKind location = side == PickerSequenceSide.Front
                ? MaterialLocationKind.PickerFront
                : MaterialLocationKind.PickerRear;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                if (die == null)
                    continue;

                hasPickerDie = true;
                if (!die.IsInputTarget)
                    continue;

                hasTargetPickerDie = true;
                targetDieCount++;

                if (die.InputSequenceNo > 0 &&
                    (!fifoKey.HasSequenceNo || die.InputSequenceNo < fifoKey.SequenceNo))
                {
                    fifoKey.HasSequenceNo = true;
                    fifoKey.SequenceNo = die.InputSequenceNo;
                    fifoKey.WaferKey = !string.IsNullOrWhiteSpace(die.InputWaferInstanceId)
                        ? die.InputWaferInstanceId
                        : die.WaferID_Input;
                }

                if (die.PickedAt > pickedAtValidThreshold &&
                    (!fifoKey.HasPickedAt || die.PickedAt < fifoKey.PickedAt))
                {
                    fifoKey.HasPickedAt = true;
                    fifoKey.PickedAt = die.PickedAt;
                }
            }

            if (hasTargetPickerDie || hasPickerDie)
            {
                required = true;
                rank = PickerFirstForwardSequencer.RankBottomSide;
                reason = hasTargetPickerDie
                    ? "picked die remains on picker; Bottom/Side reinspection required; targetDieCount=" + targetDieCount
                    : "non-target die remains on picker; operator/material recovery required";
                return;
            }

            if (MaterialStateService.HasReadyInputStagePickTarget())
            {
                required = true;
                rank = PickerFirstForwardSequencer.RankPickUp;
                reason = "ready input pick target exists at restart";
                return;
            }
        }

        private bool IsPickerSideActive(PickerSequenceSide side)
        {
            try
            {
                if (side == PickerSequenceSide.Front)
                {
                    return (_options.Units & SequenceUnitKind.PickerFront) == SequenceUnitKind.PickerFront &&
                           _ctx.Machine != null &&
                           _ctx.Machine.PickerFrontUnit != null &&
                           _ctx.Machine.PickerFrontUnit.Config != null &&
                           _ctx.Machine.PickerFrontUnit.Config.UseUnit;
                }

                return (_options.Units & SequenceUnitKind.PickerRear) == SequenceUnitKind.PickerRear &&
                       _ctx.Machine != null &&
                       _ctx.Machine.PickerRearUnit != null &&
                       _ctx.Machine.PickerRearUnit.Config != null &&
                       _ctx.Machine.PickerRearUnit.Config.UseUnit;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Manual·Step 및 Cycle Stop 제어

        /// <summary>Manual 또는 Step 모드에서 지정 유닛을 1단계 진행시킵니다.</summary>
        public void StepUnit(SequenceUnitKind unit)
        {
            UnitSequenceBase sequence;
            if (_active.TryGetValue(unit, out sequence))
            {
                _ctx.LogPublic("[SEQ] StepUnit " + unit);
                sequence.StepUnit();
                return;
            }

            _ctx.LogPublic("[SEQ] StepUnit ignored: inactive unit=" + unit);
        }

        /// <summary>Manual 또는 Step 모드에서 활성 유닛 전체를 1단계 진행시킵니다.</summary>
        public void StepAll()
        {
            if (_active.Count == 0)
            {
                _ctx.LogPublic("[SEQ] StepAll ignored: active unit 없음");
                return;
            }

            foreach (var item in _active)
            {
                _ctx.LogPublic("[SEQ] StepAll gate release " + item.Key);
                item.Value.StepUnit();
            }
        }

        /// <summary>현재 진행 중인 작업 단위가 끝나는 지점에서 자동 시퀀스를 정지하도록 요청합니다.</summary>
        public void RequestCycleStop()
        {
            _ctx.RequestCycleStop();
            _ctx.LogPublic("[SEQ] CYCLE STOP 요청 접수. 현재 작업 경계에서 정지합니다.");
            QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                "Sequence cycle stop requested. - Requested");
        }

        #endregion

        #region 하위 시컨스 종료 대기

        private async Task WaitAllOrCancelOnFirstFailureAsync(List<Task> tasks, CancellationToken ct)
        {
            var pending = new List<Task>(tasks);
            while (pending.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                Task completed = await Task.WhenAny(pending).ConfigureAwait(false);
                pending.Remove(completed);

                if (completed.IsCanceled)
                {
                    if (_ctx != null &&
                        _ctx.IsCycleStopRequested &&
                        _options != null &&
                        _options.Mode == SequenceRunMode.Auto &&
                        !ct.IsCancellationRequested &&
                        _ctx.Controller != null &&
                        _ctx.Controller.Status != EquipmentStatus.Alarm)
                    {
                        _ctx.LogPublic("[SEQ] Cycle Stop으로 중단 가능 대기가 종료되었습니다. 형제 유닛의 안전 경계를 기다립니다.");
                        await AwaitPendingAfterCycleStopAsync(pending, false).ConfigureAwait(false);
                        throw new SequenceStopException(
                            "CYCLE STOP 요청으로 중단 가능 대기가 해제되어 작업 경계 정지로 전환합니다.");
                    }

                    AbortChildren();
                    await AwaitPendingAfterAbortAsync(pending).ConfigureAwait(false);
                    throw new OperationCanceledException(ct);
                }

                if (completed.IsFaulted)
                {
                    Exception ex = completed.Exception != null ? completed.Exception.GetBaseException() : null;
                    if (SequenceStopException.IsSequenceStop(completed.Exception ?? ex))
                    {
                        string reason = SequenceStopException.ResolveReason(completed.Exception ?? ex);

                        // 한 유닛이 정상 정지(작업 완료/소진)를 선언해도 형제 유닛은 그 사실을 모른다.
                        // CycleStop 플래그를 여기서 켜야 형제 유닛이 각자 안전 경계에서 스스로 정지하고,
                        // abortOnTimeout=false 대기가 무한 대기로 남지 않는다.
                        _ctx.RequestCycleStop();

                        _ctx.LogPublic("[SEQ] Cycle Stop 경계에서 유닛 시퀀스가 정상 정지되었습니다. " + reason);
                        await AwaitPendingAfterCycleStopAsync(pending, false).ConfigureAwait(false);
                        throw new SequenceStopException(reason);
                    }

                    if (HasCriticalActiveAlarm())
                    {
                        AbortChildren();
                        await AwaitPendingAfterAbortAsync(pending).ConfigureAwait(false);
                    }
                    else
                    {
                        _ctx.RequestCycleStop();
                        _ctx.LogPublic("[SEQ] 유닛 알람 발생. 다른 유닛은 현재 작업 경계에서 정지합니다.");
                        await AwaitPendingAfterCycleStopAsync(pending, true).ConfigureAwait(false);
                    }

                    if (ex != null)
                        throw ex;
                    throw new InvalidOperationException("Sequence unit failed.");
                }

                await completed.ConfigureAwait(false);
            }
        }

        private async Task AwaitPendingAfterAbortAsync(List<Task> pending)
        {
            if (pending == null || pending.Count == 0)
                return;

            try
            {
                Task allPending = Task.WhenAll(pending);
                bool waitLogWritten = false;
                int waitStartTick = System.Environment.TickCount;
                while (!allPending.IsCompleted)
                {
                    Task logDelay = Task.Delay(AbortPendingWaitLogIntervalMs);
                    Task completed = await Task.WhenAny(allPending, logDelay).ConfigureAwait(false);
                    if (completed == allPending)
                        break;

                    if (!waitLogWritten)
                    {
                        waitLogWritten = true;
                        _ctx.LogPublic("[SEQ] Abort 이후 남은 시퀀스가 정리되는 중입니다. pending=" +
                                       pending.Count);
                        QMC.Common.Log.Write("Main", "SYSTEM", "SequenceAbort",
                            "Sequence abort pending wait still running. pending=" + pending.Count +
                            ", intervalMs=" + AbortPendingWaitLogIntervalMs + " - Wait");
                    }

                    if (ElapsedMilliseconds(waitStartTick) >= CycleStopPendingWaitTimeoutMs)
                    {
                        _ctx.LogPublic("[SEQ] Cycle Stop 경계 대기 시간이 초과되어 남은 시퀀스를 취소합니다. pending=" +
                                       pending.Count);
                        QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                            "Sequence cycle stop pending wait timeout. pending=" + pending.Count +
                            ", timeoutMs=" + CycleStopPendingWaitTimeoutMs + " - Abort");

                        AbortChildren();

                        Task abortWait = Task.Delay(PendingAbortFinishTimeoutMs);
                        Task abortCompleted = await Task.WhenAny(allPending, abortWait).ConfigureAwait(false);
                        if (abortCompleted != allPending)
                        {
                            _ctx.LogPublic("[SEQ] 취소 요청 후에도 남은 시퀀스가 완료되지 않았습니다. READY 차단을 피하기 위해 Coordinator를 종료합니다. pending=" +
                                           pending.Count);
                            QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                                "Sequence pending tasks did not complete after abort request. pending=" + pending.Count +
                                ", timeoutMs=" + PendingAbortFinishTimeoutMs + " - Timeout");
                            return;
                        }

                        break;
                    }
                }

                await allPending.ConfigureAwait(false);
                _ctx.LogPublic("[SEQ] Abort 이후 남은 시퀀스가 모두 정리되었습니다.");
            }
            catch (OperationCanceledException)
            {
                _ctx.LogPublic("[SEQ] Abort 이후 남은 시퀀스가 취소 상태로 정리되었습니다.");
            }
            catch (Exception ex)
            {
                if (SequenceStopException.IsSequenceStop(ex))
                {
                    _ctx.LogPublic("[SEQ] Abort 이후 남은 시퀀스가 Cycle Stop 경계에서 정리되었습니다. " +
                                   SequenceStopException.ResolveReason(ex));
                    QMC.Common.Log.Write("Main", "SYSTEM", "SequenceAbort",
                        "Sequence abort pending wait ended by cycle stop. reason=" +
                        SequenceStopException.ResolveReason(ex) + " - Stopped");
                    return;
                }

                _ctx.LogPublic("[SEQ] Abort 이후 남은 시퀀스 정리 중 예외가 발생했습니다. error=" + ex.Message);
                QMC.Common.Log.Write("Main", "SYSTEM", "SequenceAbort",
                    "Sequence abort pending wait failed. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static int ElapsedMilliseconds(int startTick)
        {
            return unchecked(System.Environment.TickCount - startTick);
        }

        private async Task AwaitPendingAfterCycleStopAsync(List<Task> pending, bool abortOnTimeout)
        {
            if (pending == null || pending.Count == 0)
                return;

            try
            {
                Task allPending = Task.WhenAll(pending);
                bool waitLogWritten = false;
                int waitStartTick = System.Environment.TickCount;
                while (!allPending.IsCompleted)
                {
                    Task logDelay = Task.Delay(AbortPendingWaitLogIntervalMs);
                    Task completed = await Task.WhenAny(allPending, logDelay).ConfigureAwait(false);
                    if (completed == allPending)
                        break;

                    if (!waitLogWritten)
                    {
                        waitLogWritten = true;
                        _ctx.LogPublic("[SEQ] Cycle Stop 경계까지 남은 시퀀스가 정리되는 중입니다. pending=" +
                                       pending.Count);
                        QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                            "Sequence pending wait still running after unit alarm. pending=" + pending.Count +
                            ", intervalMs=" + AbortPendingWaitLogIntervalMs + " - Wait");
                    }

                    if (abortOnTimeout && ElapsedMilliseconds(waitStartTick) >= CycleStopPendingWaitTimeoutMs)
                    {
                        _ctx.LogPublic("[SEQ] Cycle Stop 경계 대기 시간이 초과되어 남은 시퀀스를 취소합니다. pending=" +
                                       pending.Count);
                        QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                            "Sequence cycle stop pending wait timeout. pending=" + pending.Count +
                            ", timeoutMs=" + CycleStopPendingWaitTimeoutMs + " - Abort");

                        AbortChildren();

                        Task abortWait = Task.Delay(PendingAbortFinishTimeoutMs);
                        Task abortCompleted = await Task.WhenAny(allPending, abortWait).ConfigureAwait(false);
                        if (abortCompleted != allPending)
                        {
                            _ctx.LogPublic("[SEQ] 취소 요청 후에도 남은 시퀀스가 완료되지 않았습니다. READY 차단을 피하기 위해 Coordinator를 종료합니다. pending=" +
                                           pending.Count);
                            QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                                "Sequence pending tasks did not complete after cycle stop abort request. pending=" + pending.Count +
                                ", timeoutMs=" + PendingAbortFinishTimeoutMs + " - Timeout");
                            return;
                        }

                        break;
                    }
                }

                await allPending.ConfigureAwait(false);
                _ctx.LogPublic("[SEQ] 유닛 알람 이후 남은 시퀀스가 작업 경계에서 정리되었습니다.");
            }
            catch (OperationCanceledException)
            {
                _ctx.LogPublic("[SEQ] Cycle Stop 대기 중 남은 시퀀스가 취소되었습니다.");
            }
            catch (Exception ex)
            {
                if (SequenceStopException.IsSequenceStop(ex))
                {
                    _ctx.LogPublic("[SEQ] Cycle Stop 대기 중 남은 시퀀스가 작업 경계에서 정리되었습니다. " +
                                   SequenceStopException.ResolveReason(ex));
                    QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                        "Sequence pending wait ended by cycle stop. reason=" +
                        SequenceStopException.ResolveReason(ex) + " - Stopped");
                    return;
                }

                _ctx.LogPublic("[SEQ] Cycle Stop 대기 중 추가 유닛 알람이 발생했습니다. error=" + ex.Message);
                QMC.Common.Log.Write("Main", "SYSTEM", "SequenceCycleStop",
                    "Sequence pending wait after unit alarm failed. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        #endregion

        #region Critical 알람 판정

        private static bool HasCriticalActiveAlarm()
        {
            try
            {
                if (AlarmManager.HighestActiveSeverity == AlarmSeverity.Critical)
                    return true;

                foreach (AlarmRecord alarm in AlarmManager.Active)
                {
                    if (IsCriticalMotionOrInterlockAlarm(alarm))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool IsCriticalMotionOrInterlockAlarm(AlarmRecord alarm)
        {
            try
            {
                if (alarm == null)
                    return false;

                string code = alarm.Code ?? "";
                string message = alarm.Message ?? "";

                if (IsExactOrPrefix(code, "E-STOP") ||
                    Contains(code, "INTERLOCK") ||
                    Contains(code, "LIMIT"))
                    return true;

                if (code.StartsWith("AX-MOVE", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("AX-HOME", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("AX-JOG", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("AX-SOFT-LIMIT", StringComparison.OrdinalIgnoreCase) ||
                    code.StartsWith("LIMIT-", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (Contains(code, "MOVE") &&
                    (Contains(message, "alarm=True") ||
                     Contains(message, "alarm=ON") ||
                     Contains(message, "알람=ON") ||
                     Contains(message, "Axis alarm is ON") ||
                     Contains(message, "축 알람이 ON")))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static bool Contains(string value, string text)
        {
            return (value ?? "").IndexOf(text ?? "", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsExactOrPrefix(string value, string token)
        {
            value = value ?? "";
            token = token ?? "";
            return value.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith(token + "-", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 하위 시컨스 중단

        /// <summary>실행 중인 모든 하위 유닛 시퀀스를 중단합니다.</summary>
        public void AbortChildren()
        {
            var cts = _childrenCts;
            if (cts == null)
                return;

            try
            {
                if (!cts.IsCancellationRequested)
                {
                    _ctx.LogPublic("[SEQ] AbortChildren");
                    cts.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                _ctx.LogPublic("[SEQ] AbortChildren ignored: child cancellation source already disposed.");
            }
        }

        #endregion
    }
}

