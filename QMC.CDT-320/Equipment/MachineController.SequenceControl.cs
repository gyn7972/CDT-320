using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.IO;
using QMC.Common.Motion;
using QMC.Common.Alarms;
using QMC.Common.Diagnostics.TactTime;
using QMC.CDT320.Bin;
using QMC.CDT320.Interlocks;
using QMC.CDT320.Jobs;
using QMC.CDT320.Lots;
using QMC.CDT320.Materials;
using QMC.CDT320.Alarms;
using QMC.CDT320.Ajin;
using QMC.CDT320.Calibration;
using QMC.CDT320.Initialization;
using QMC.CDT320.Motion.SharedRailX;
using QMC.CDT320.Recipes;

namespace QMC.CDT320
{
    public partial class MachineController
    {
        /// <summary>
        /// 지정한 옵션으로 병렬 시퀀스 Coordinator를 시작합니다.
        /// 자동 운전의 기준 진입점이며, Unit/Mode/StartMode는 SequenceRunOptions로 결정합니다.
        /// </summary>
        public async Task<int> StartSequenceAsync(QMC.CDT320.Sequencing.SequenceRunOptions options)
        {
            CancellationTokenSource startAttemptCts = null;
            long recipeGeneration = 0;
            try
            {
                string reason;
                if (!TryBeginRecipeStartAttempt(
                        "StartSequenceAsync",
                        out startAttemptCts,
                        out recipeGeneration,
                        out reason))
                {
                    LastActionFailureMessage = reason;
                    QMC.Common.Log.Write(
                        "Main",
                        "SYSTEM",
                        "StartSequenceAsync",
                        "Sequence START 진입 차단. " + reason + " - Blocked");
                    return -1;
                }

                return await StartSequenceCoreAsync(
                    options,
                    startAttemptCts,
                    recipeGeneration).ConfigureAwait(false);
            }
            finally
            {
                EndRecipeStartAttempt(startAttemptCts, "StartSequenceAsync");
            }
        }

        private async Task<int> StartSequenceCoreAsync(
            QMC.CDT320.Sequencing.SequenceRunOptions options,
            CancellationTokenSource startAttemptCts,
            long recipeGeneration)
        {
            try
            {
                string startAttemptReason;
                if (!IsRecipeStartAttemptValid(
                        startAttemptCts,
                        recipeGeneration,
                        "StartSequenceCoreAsync",
                        out startAttemptReason))
                {
                    LastActionFailureMessage = startAttemptReason;
                    return -1;
                }

                if (!EnsureMachineInitializedForRun("StartSequenceAsync"))
                    return -1;

                if (_coordinatorTask != null && !_coordinatorTask.IsCompleted)
                {
                    LastActionFailureMessage = "다른 Sequence가 실행 중이므로 새 Sequence START를 차단했습니다.";
                    return -1;
                }

                if (options == null)
                    options = QMC.CDT320.Sequencing.SequenceRunOptions.FullAuto();

                // 모든 자동 운전 진입점이 지나는 최종 관문(운전 패널·UI·내부 호출 공통).
                if (options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto &&
                    !EnsureActiveLotForAutoStart("StartSequenceAsync"))
                    return -1;

                if (options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto &&
                    !await EnsureRecipeReadyForAutoStartAsync(
                        "StartSequenceAsync",
                        false,
                        startAttemptCts.Token).ConfigureAwait(false))
                {
                    return -1;
                }

                if (options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto &&
                    !EnsureReticleAvoidForAutoStart("StartSequenceAsync"))
                    return -1;

                if (!IsRecipeStartAttemptValid(
                        startAttemptCts,
                        recipeGeneration,
                        "StartSequenceAsync.BeforeCoordinator",
                        out startAttemptReason))
                {
                    LastActionFailureMessage = startAttemptReason;
                    return -1;
                }

                _autoCts = CancellationTokenSource.CreateLinkedTokenSource(
                    startAttemptCts.Token);
                var bus = new QMC.CDT320.Sequencing.SequenceSignalBus();
                TactTimeRecorder tact = CreateTactTimeRecorder(options);
                _activeTactTimeRecorder = tact;
                ResetInspectionTactTimeState();
                ResetOutputReceiveTactTimeState();
                // 새 시퀀스 시작: 4개 유닛 상태를 Idle 로 초기화하고 동일 ActivityMonitor 를 컨텍스트에 주입한다.
                _sequenceActivity.Reset();

                _seqContext = new QMC.CDT320.Sequencing.MachineSequenceContext(
                    this, bus, new QMC.CDT320.Sequencing.SequenceResourceManager(), _sequenceActivity, tact);

                _coordinator = new QMC.CDT320.Sequencing.AutoSequenceCoordinator(_seqContext);

                // Coordinator에 각 UnitSequence를 등록한다.
                _coordinator.Register(
                    QMC.CDT320.Sequencing.SequenceUnitKind.InputLoader,
                    () => new QMC.CDT320.Sequencing.InputSequence(_seqContext));

                _coordinator.Register(
                    QMC.CDT320.Sequencing.SequenceUnitKind.PickerFront,
                    () => new QMC.CDT320.Sequencing.FrontPickerSequence(_seqContext));

                _coordinator.Register(
                    QMC.CDT320.Sequencing.SequenceUnitKind.PickerRear,
                    () => new QMC.CDT320.Sequencing.RearPickerSequence(_seqContext));

                _coordinator.Register(
                    QMC.CDT320.Sequencing.SequenceUnitKind.OutputUnloader,
                    () => new QMC.CDT320.Sequencing.OutputSequence(_seqContext));

                _coordinator.Configure(options);
                ActiveSequenceRunMode = options.Mode;
                if (options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    BeginAutoProductionStats();

                SetStatus(options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto
                    ? EquipmentStatus.AutoRunning
                    : EquipmentStatus.ManualRunning);

                Log("[SEQ] StartSequenceAsync units=" + options.Units + ", mode=" + options.Mode);

                // 오토시컨스 속도 % 확인을 위한 로그 수정
                //QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                //    "Sequence start. units=" + options.Units + ", mode=" + options.Mode + " - Ok");
                QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                    "Sequence start. units=" + options.Units + ", mode=" + options.Mode +
                    ", scalePercent=" + QMC.Common.Motion.MotionSpeedScale.ScalePercent.ToString("0.###") + " - Ok");

                if (options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    LogMachineAxisSnapshot("AutoStartBeforeCoordinatorRun");

                // 콜렛 클리닝 "Auto 시작" 트리거를 이번 런에서 1회 발동 가능하게 리셋한다.
                // 코디네이터를 띄우기 전에 수행해야 InputSequence가 먼저 트리거를 소비하고 뒤늦게 리셋되어
                // 한 런에서 두 번 발동하는 경합이 생기지 않는다. Auto 진입은 모두 이 지점을 지난다.
                if (options.Mode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                    QMC.CDT320.Sequencing.Calibration.ColletCleaningTriggerService.NotifyAutoRunStarted();

                var coordinator = _coordinator;
                var cts = _autoCts;
                var runMode = options.Mode;
                var waferCompletion = _seqContext.WaferCompletion;
                _coordinatorTask = Task.Run(async () =>
                {
                    IDisposable sequenceScope = null;
                    try
                    {
                        sequenceScope = BeginManualProcessSequenceScopeIfNeeded(runMode);
                        await coordinator.RunAsync(cts.Token).ConfigureAwait(false);
                        if (!cts.IsCancellationRequested && _status != EquipmentStatus.Alarm)
                        {
                            Log("[SEQ] Complete");
                            if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                                EndAutoProductionStats();

                            SetStatus(EquipmentStatus.Ready);
                            if (runMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto &&
                                waferCompletion != null &&
                                waferCompletion.IsRunComplete)
                            {
                                RequestOperatorMessage(
                                    "Wafer 작업 완료",
                                    waferCompletion.BuildCompletionMessage());
                            }
                        }
                    }
                    catch (QMC.CDT320.Sequencing.SequenceStopException ex)
                    {
                        // 남아 있는 진행/대기 유닛을 정지 상태로 정리한다.
                        _sequenceActivity.SweepActiveTo(QMC.CDT320.Sequencing.SequenceActivityState.Stopped,
                            "시퀀스가 정지되었습니다.");

                        QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                            "Sequence stopped: " + ex.Message + " - Stopped");
                        Log("[SEQ] stopped: " + ex.Message);

                        if (_status != EquipmentStatus.Alarm)
                        {
                            if (_seqContext != null && _seqContext.IsCycleStopRequested)
                            {
                                QMC.CDT320.Sequencing.SequenceResumeStore.MarkCycleStopped(
                                    "AutoSequence",
                                    "",
                                    ex.Message);
                                SetStatus(EquipmentStatus.CycleStopped);
                            }
                            else
                            {
                                if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                                    EndAutoProductionStats();
                                SetStatus(EquipmentStatus.Stopped);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 남아 있는 진행/대기 유닛을 취소 상태로 정리한다.
                        _sequenceActivity.SweepActiveTo(QMC.CDT320.Sequencing.SequenceActivityState.Canceled,
                            "시퀀스가 취소되었습니다.");
                        bool cycleStopRequested = _seqContext != null && _seqContext.IsCycleStopRequested;
                        Log("[SEQ] Canceled");
                        QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                            "Sequence canceled. runMode=" + runMode +
                            ", tokenCanceled=" + (cts != null && cts.IsCancellationRequested) +
                            ", cycleStopRequested=" + cycleStopRequested +
                            ", status=" + _status + " - Canceled");
                        if (runMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                            EndAutoProductionStats();

                        if (_status != EquipmentStatus.Alarm)
                        {
                            if (cycleStopRequested &&
                                runMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                            {
                                const string drainMissingMessage =
                                    "정상 CYCLE STOP 요청이 최종 검사·저장·전축 정지 배리어를 통과하지 못하고 취소 경로로 종료되었습니다.";
                                QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                                    drainMissingMessage + " - Failed");
                                AlarmManager.Raise(
                                    AlarmSeverity.Error,
                                    "SEQ-CYCLE-STOP-DRAIN-MISSING",
                                    "MachineController",
                                    drainMissingMessage);
                                SetStatus(EquipmentStatus.Alarm);
                            }
                            else
                            {
                                SetStatus(EquipmentStatus.Stopped);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 실패 유닛은 UnitSequenceBase 에서 이미 Alarm. 남은 진행/대기 유닛은 정지로 정리한다.
                        _sequenceActivity.SweepActiveTo(QMC.CDT320.Sequencing.SequenceActivityState.Stopped,
                            "다른 유닛 알람으로 정지되었습니다.");
                        QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                            "자동 시퀀스 실패: " + ex.Message + " - Failed");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-EX", "MachineController", "자동 시퀀스 실패: " + ex.Message);
                        Log("[SEQ] failed: " + ex.Message);
                        if (ActiveSequenceRunMode == QMC.CDT320.Sequencing.SequenceRunMode.Auto)
                            EndAutoProductionStats();
                        SetStatus(EquipmentStatus.Alarm);
                    }
                    finally
                    {
                        ClearInputStageRunReviewManualState("CoordinatorFinally");

                        if (sequenceScope != null)
                            sequenceScope.Dispose();

                        if (_coordinator == coordinator)
                        {
                            if (_activeTactTimeRecorder != null &&
                                !object.ReferenceEquals(_activeTactTimeRecorder, NullTactTimeRecorder.Instance))
                            {
                                _activeTactTimeRecorder.Dispose();
                            }

                            _activeTactTimeRecorder = null;
                            _coordinator = null;
                            _seqContext = null;
                            _coordinatorTask = null;
                            ActiveSequenceRunMode = null;
                        }
                    }
                });
                return 0;
            }
            catch (OperationCanceledException)
            {
                LastActionFailureMessage = "Sequence START 준비가 STOP/Alarm 요청으로 취소되었습니다.";
                QMC.Common.Log.Write(
                    "Main",
                    "SYSTEM",
                    "StartSequenceAsync",
                    LastActionFailureMessage + " - Canceled");
                return -1;
            }
            catch (Exception ex)
            {
                if (_activeTactTimeRecorder != null &&
                    !object.ReferenceEquals(_activeTactTimeRecorder, NullTactTimeRecorder.Instance))
                {
                    _activeTactTimeRecorder.Dispose();
                    _activeTactTimeRecorder = null;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "StartSequenceAsync",
                    "자동 시퀀스 시작 실패: " + ex.Message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-START-EX", "MachineController", "자동 시퀀스 시작 실패: " + ex.Message);
                Log("[SEQ] start failed: " + ex.Message);
                SetStatus(EquipmentStatus.Alarm);
                throw;
            }
            finally
            {
            }
        }

        private void LogMachineAxisSnapshot(string phase)
        {
            try
            {
                string snapshotPhase = string.IsNullOrWhiteSpace(phase) ? "Unknown" : phase;
                List<BaseAxis> axes = AjinAxisRegistry.GetOrderedAxes(_machine);
                QMC.Common.Log.Write("Main", "SYSTEM", "AxisSnapshot",
                    "phase=" + snapshotPhase + ", axisCount=" + (axes != null ? axes.Count : 0) + " - Start");

                if (axes == null)
                    return;

                for (int i = 0; i < axes.Count; i++)
                {
                    BaseAxis axis = axes[i];
                    if (axis == null)
                        continue;

                    QMC.Common.Log.Write("Main", "SYSTEM", "AxisSnapshot",
                        "phase=" + snapshotPhase +
                        ", no=" + i +
                        ", axis=" + SafeAxisName(axis) +
                        ", display=" + SafeAxisDisplayName(axis) +
                        ", unit=" + SafeAxisUnitName(axis) +
                        ", axisNo=" + SafeAxisNo(axis) +
                        ", servo=" + (axis.IsServoOn ? "ON" : "OFF") +
                        ", alarm=" + (axis.IsAlarm ? "ON" : "OFF") +
                        ", moving=" + (axis.IsMoving ? "Y" : "N") +
                        ", homeDone=" + (axis.IsHomeDone ? "Y" : "N") +
                        ", inPosition=" + (axis.IsInPosition ? "Y" : "N") +
                        ", actual=" + axis.ActualPosition.ToString("F6") +
                        ", command=" + axis.CommandPosition.ToString("F6") +
                        ", tolerance=" + ResolveAxisInPositionTolerance(axis).ToString("F6") +
                        " - State");
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "AxisSnapshot",
                    "phase=" + snapshotPhase + " - End");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "AxisSnapshot",
                    "phase=" + phase + ", snapshot failed. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static string SafeAxisName(BaseAxis axis)
        {
            return axis != null && !string.IsNullOrWhiteSpace(axis.Name) ? axis.Name : "-";
        }

        private static string SafeAxisDisplayName(BaseAxis axis)
        {
            return axis != null && axis.Setup != null && !string.IsNullOrWhiteSpace(axis.Setup.DisplayName)
                ? axis.Setup.DisplayName
                : SafeAxisName(axis);
        }

        private static string SafeAxisUnitName(BaseAxis axis)
        {
            return axis != null && axis.Setup != null && !string.IsNullOrWhiteSpace(axis.Setup.UnitName)
                ? axis.Setup.UnitName
                : "-";
        }

        private static int SafeAxisNo(BaseAxis axis)
        {
            return axis != null && axis.Setup != null ? axis.Setup.AxisNo : -1;
        }

        /// <summary>실행 중인 병렬 시퀀스를 중단하고 Coordinator 종료를 대기합니다.</summary>
        public async Task StopSequenceAsync()
        {
            CancelRecipeStartAttempt("StopSequence");

            var coordinator = _coordinator;
            var cts = _autoCts;
            var task = _coordinatorTask;

            if (coordinator != null)
                coordinator.AbortChildren();
            if (cts != null && !cts.IsCancellationRequested)
                cts.Cancel();

            if (task != null)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Log("[SEQ] StopSequenceAsync canceled");
                }
            }

            _coordinatorTask = null;
            _coordinator = null;
            _seqContext = null;
            ActiveSequenceRunMode = null;
            if (_autoCts != null)
            {
                _autoCts.Dispose();
                _autoCts = null;
            }

            if (_status == EquipmentStatus.ManualRunning || _status == EquipmentStatus.AutoRunning)
                SetStatus(EquipmentStatus.Stopped);
        }

        /// <summary>현재 자동 시퀀스를 즉시 취소하지 않고 작업 경계에서 CYCLE STOP 되도록 요청합니다.</summary>
        public Task RequestCycleStopSequenceAsync()
        {
            try
            {
                LastActionFailureMessage = "";

                var coordinator = _coordinator;
                if (coordinator == null || _coordinatorTask == null || _coordinatorTask.IsCompleted)
                {
                    LastActionFailureMessage = "진행 중인 자동 시퀀스가 없어 CYCLE STOP 요청을 무시합니다.";
                    QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopSequence",
                        LastActionFailureMessage + " - Ignored");
                    Log("[SEQ] Cycle stop ignored: no active auto sequence");
                    return Task.CompletedTask;
                }

                coordinator.RequestCycleStop();
                // [무언정지 방지 2026-08-17, 팀장님 확인] 기존 4-인자 Log.Write는 최소 로그 정책에서
                // 디스크에 남지 않아, STOP으로 세운 정지가 로그상 "무언정지"로 보였다(15:33 실사례).
                // 정지 발원은 반드시 남도록 Warning으로 승격한다.
                QMC.Common.Logging.EventLogger.Write(
                    QMC.Common.Logging.EventKind.Warning,
                    "SYSTEM",
                    "SEQ-CYCLE-STOP-REQUEST",
                    "MachineController",
                    "CYCLE STOP 요청 — 자동 시퀀스를 다음 안전 경계에서 정지합니다. status=" + _status);
                Log("[SEQ] Cycle stop requested");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LastActionFailureMessage = "CYCLE STOP 요청 실패: " + ex.Message;
                QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopSequence",
                    LastActionFailureMessage + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-CYCLE-STOP-EX", "MachineController", LastActionFailureMessage);
                Log("[SEQ] Cycle stop request failed: " + ex.Message);
                return Task.CompletedTask;
            }
            finally
            {
            }
        }

        public async Task<int> StopSequenceForAlarmAsync(string alarmCode)
        {
            bool alarmStopGateEntered = false;
            try
            {
                await _alarmSequenceStopGate.WaitAsync().ConfigureAwait(false);
                alarmStopGateEntered = true;
                return await StopSequenceForAlarmCoreAsync(alarmCode).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopSequenceForAlarm",
                    "Sequence stop by alarm failed. code=" + alarmCode +
                    ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
                if (alarmStopGateEntered)
                    _alarmSequenceStopGate.Release();
            }
        }

        private async Task<int> StopSequenceForAlarmCoreAsync(string alarmCode)
        {
            var coordinator = _coordinator;
            var autoCts = _autoCts;
            var coordinatorTask = _coordinatorTask;
            var initializeCts = GetAxisInitializeOperationCancellationSource();

            QMC.Common.Log.Write("Main", "SYSTEM", "StopSequenceForAlarm",
                "Immediate operation cancellation start. code=" + alarmCode +
                ", taskStatus=" +
                (coordinatorTask != null ? coordinatorTask.Status.ToString() : "null") +
                ", coordinator=" + (coordinator != null) +
                ", " + BuildAlarmControlledOperationState() + " - Start");

            // 알람 E-Stop 직후 HOME 준비 대기에서 ServoOn/HomeSearch가 새로 시작되지 않도록
            // 다른 운전 취소보다 먼저 초기화 CTS를 끊습니다.
            TryCancelAlarmOperationToken(initializeCts, "AxisInitialize", alarmCode);
            OnStopRequested();
            CancelInputStageRunReviewAction();
            CancelManualOperation();
            if (coordinator != null)
                coordinator.AbortChildren();
            TryCancelAlarmOperationToken(autoCts, "AutoSequence", alarmCode);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool allStopped = await WaitForAlarmControlledOperationsToStopAsync(
                coordinatorTask,
                stopwatch).ConfigureAwait(false);

            if (!allStopped)
            {
                string timeoutState = BuildAlarmControlledOperationState();
                LastActionFailureMessage =
                    "알람 발생 후 실행 중인 동작이 제한시간 내 종료되지 않았습니다.";
                QMC.Common.Log.Write("Main", "SYSTEM", "StopSequenceForAlarm",
                    LastActionFailureMessage + " code=" + alarmCode +
                    ", elapsedMs=" + stopwatch.ElapsedMilliseconds +
                    ", " + timeoutState + " - Failed");
                return -1;
            }

            if (coordinatorTask == null || coordinatorTask.IsCompleted)
            {
                if (object.ReferenceEquals(_coordinatorTask, coordinatorTask))
                    _coordinatorTask = null;
                if (_coordinator == null || object.ReferenceEquals(_coordinator, coordinator))
                {
                    _coordinator = null;
                    _seqContext = null;
                    ActiveSequenceRunMode = null;
                }

                if (object.ReferenceEquals(_autoCts, autoCts))
                {
                    _autoCts = null;
                    if (autoCts != null)
                    {
                        try { autoCts.Dispose(); }
                        catch (ObjectDisposedException) { }
                    }
                }
            }

            if ((_status == EquipmentStatus.ManualRunning ||
                 _status == EquipmentStatus.AutoRunning) &&
                !AlarmManager.HasActive)
            {
                SetStatus(EquipmentStatus.Stopped);
            }

            QMC.Common.Log.Write("Main", "SYSTEM", "StopSequenceForAlarm",
                "Alarm response stopped active operations. code=" + alarmCode +
                ", elapsedMs=" + stopwatch.ElapsedMilliseconds +
                ", status=" + _status +
                ", " + BuildAlarmControlledOperationState() + " - Ok");
            return 0;
        }

        private async Task<bool> WaitForAlarmControlledOperationsToStopAsync(
            Task coordinatorTask,
            System.Diagnostics.Stopwatch stopwatch)
        {
            while (coordinatorTask != null &&
                   !coordinatorTask.IsCompleted &&
                   stopwatch.ElapsedMilliseconds < AlarmSequenceStopTimeoutMs)
            {
                await Task.Delay(AlarmSequenceStopPollIntervalMs).ConfigureAwait(false);
            }

            if (coordinatorTask != null && coordinatorTask.IsCompleted)
            {
                try
                {
                    await coordinatorTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "StopSequenceForAlarm",
                        "Coordinator completed with exception during alarm stop. error=" +
                        ex.Message + " - Check");
                }
            }

            while (stopwatch.ElapsedMilliseconds < AlarmSequenceStopTimeoutMs)
            {
                if ((coordinatorTask == null || coordinatorTask.IsCompleted) &&
                    IsInputStageRunReviewManualActive &&
                    !IsInputStageRunReviewActionBusy)
                {
                    ClearInputStageRunReviewManualState("AlarmStop");
                }

                if (!HasActiveAlarmControlledOperation)
                    return true;

                await Task.Delay(AlarmSequenceStopPollIntervalMs).ConfigureAwait(false);
            }

            return !HasActiveAlarmControlledOperation;
        }

        private static void TryCancelAlarmOperationToken(
            CancellationTokenSource cts,
            string operationName,
            string alarmCode)
        {
            if (cts == null)
                return;

            try
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopSequenceForAlarm",
                    "Operation cancellation failed. code=" + alarmCode +
                    ", operation=" + operationName +
                    ", error=" + ex.Message + " - Failed");
            }
        }

        private string BuildAlarmControlledOperationState()
        {
            return "sequenceRunning=" + IsSequenceRunning +
                   ", manualBusy=" + IsManualBusy +
                   ", reviewManualActive=" + IsInputStageRunReviewManualActive +
                   ", reviewActionBusy=" + IsInputStageRunReviewActionBusy +
                   ", readyRunning=" + IsReadySequenceRunning +
                   ", initializeBusy=" + IsAxisInitializeOperationRunning;
        }

        private static int GetLatestAlarmRecordId()
        {
            int latestId = 0;
            IReadOnlyList<AlarmRecord> history = AlarmManager.History;
            if (history == null)
                return latestId;

            foreach (AlarmRecord alarm in history)
            {
                if (alarm != null && alarm.Id > latestId)
                    latestId = alarm.Id;
            }

            return latestId;
        }

        public async Task<int> StopAxesAsync(IEnumerable<string> axisNames, bool emergencyStop = false)
        {
            try
            {
                if (axisNames == null)
                    return 0;

                int total = 0;
                int failed = 0;
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var axisName in axisNames)
                {
                    if (string.IsNullOrWhiteSpace(axisName))
                        continue;
                    if (!visited.Add(axisName.Trim()))
                        continue;

                    var axis = FindAxisByName(axisName);
                    if (axis == null)
                    {
                        failed++;
                        QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                            "Axis stop failed: axis not found. axis=" + axisName + " - Failed");
                        continue;
                    }

                    total++;
                    try
                    {
                        if (emergencyStop)
                            axis.EStop();
                        else
                            axis.Stop();

                        QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                            "Axis stopped. axis=" + axis.Name + ", emergency=" + emergencyStop + " - Ok");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                            "Axis stop failed. axis=" + axis.Name + ", error=" + ex.Message + " - Failed");
                    }
                }

                await Task.Yield();
                return failed == 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopAxes",
                    "Axis stop failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> StopInterferenceGroupAsync(string sourceAxisName, bool emergencyStop = false)
        {
            try
            {
                var axes = _axisInterferenceMap.ResolveInterferenceAxes(sourceAxisName);
                QMC.Common.Log.Write("Main", "SYSTEM", "StopInterferenceGroup",
                    "Interference group stop requested. sourceAxis=" + sourceAxisName +
                    ", count=" + (axes != null ? axes.Count : 0) + ", emergency=" + emergencyStop + " - Start");
                return await StopAxesAsync(axes, emergencyStop).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopInterferenceGroup",
                    "Interference group stop failed. sourceAxis=" + sourceAxisName + ", error=" + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        public async Task<int> StopAllAxesAsync(bool emergencyStop = false)
        {
            try
            {
                MotionGuardRuntime.CancelPickerYCollisionRecoveryJog(null);
                var axes = new List<string>();
                foreach (var axis in EnumerateAxes())
                    axes.Add(axis.Name);

                QMC.Common.Log.Write("Main", "SYSTEM", "StopAllAxes",
                    "All axis stop requested. count=" + axes.Count + ", emergency=" + emergencyStop + " - Start");
                return await StopAxesAsync(axes, emergencyStop).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "StopAllAxes",
                    "All axis stop failed: " + ex.Message + " - Failed");
                return -1;
            }
            finally
            {
            }
        }

        /// <summary>
        /// 정상 Auto Cycle Stop 최종 배리어용 읽기 전용 확인이다.
        /// 축 정지 명령을 새로 발행하지 않고 모든 등록 축의 IsMoving=false가 연속 샘플에서 유지되는지 확인한다.
        /// </summary>
        internal async Task<int> WaitUntilAllAxesStoppedAsync(
            string reason,
            int timeoutMs,
            CancellationToken ct)
        {
            string safeReason = string.IsNullOrWhiteSpace(reason) ? "CycleStopFinalDrain" : reason;
            int safeTimeoutMs = timeoutMs > 0 ? timeoutMs : 30000;
            const int requiredStableSamples = 3;
            const int sampleIntervalMs = 50;
            DateTime startedAt = DateTime.UtcNow;
            int stableSamples = 0;
            bool waitLogged = false;

            List<BaseAxis> axes = EnumerateAxes()
                .Where(axis => axis != null)
                .Distinct()
                .ToList();
            if (axes.Count == 0)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopAxisBarrier",
                    safeReason + " 전체 축 정지 확인 대상이 없습니다. - Failed");
                return -1;
            }

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var movingAxes = new List<string>();
                for (int i = 0; i < axes.Count; i++)
                {
                    BaseAxis axis = axes[i];
                    try
                    {
                        axis.UpdateStatus();
                    }
                    catch (Exception ex)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopAxisBarrier",
                            safeReason + " 축 상태 갱신 실패. axis=" + axis.Name +
                            ", error=" + ex.Message + " - Failed");
                        return -1;
                    }

                    if (axis.IsAlarm)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopAxisBarrier",
                            safeReason + " 전체 축 정지 확인 중 축 Alarm이 감지되었습니다. axis=" + axis.Name +
                            ", actual=" + axis.ActualPosition.ToString("F6") +
                            ", command=" + axis.CommandPosition.ToString("F6") + " - Failed");
                        return -1;
                    }

                    if (axis.IsMoving)
                    {
                        movingAxes.Add(
                            axis.Name + "(actual=" + axis.ActualPosition.ToString("F6") +
                            ",command=" + axis.CommandPosition.ToString("F6") + ")");
                    }
                }

                if (movingAxes.Count == 0)
                {
                    stableSamples++;
                    if (stableSamples >= requiredStableSamples)
                    {
                        QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopAxisBarrier",
                            safeReason + " 전체 축 완전 정지 확인 완료. axisCount=" + axes.Count +
                            ", stableSamples=" + stableSamples + " - Ok");
                        LogMachineAxisSnapshot("CycleStopFinalDrainStopped");
                        return 0;
                    }
                }
                else
                {
                    stableSamples = 0;
                    if (!waitLogged)
                    {
                        waitLogged = true;
                        QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopAxisBarrier",
                            safeReason + " 전체 축 완전 정지를 기다립니다. moving=" +
                            string.Join(", ", movingAxes) + " - Wait");
                    }
                }

                if ((DateTime.UtcNow - startedAt).TotalMilliseconds >= safeTimeoutMs)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "CycleStopAxisBarrier",
                        safeReason + " 전체 축 완전 정지 확인 시간 초과. moving=" +
                        (movingAxes.Count > 0 ? string.Join(", ", movingAxes) : "stableSamplePending") +
                        ", timeoutMs=" + safeTimeoutMs + " - Failed");
                    LogMachineAxisSnapshot("CycleStopFinalDrainTimeout");
                    return -1;
                }

                await Task.Delay(sampleIntervalMs, ct).ConfigureAwait(false);
            }
        }

        public string ResolveAxisNameFromAlarm(string alarmSource, string alarmCode)
        {
            try
            {
                string source = alarmSource ?? "";
                string code = alarmCode ?? "";

                foreach (var axis in EnumerateAxes())
                {
                    if (string.Equals(source, axis.Name, StringComparison.OrdinalIgnoreCase))
                        return axis.Name;

                    if (source.IndexOf(axis.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                        return axis.Name;

                    if (code.IndexOf(axis.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                        return axis.Name;
                }

                return "";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        public void SetAlarmStateFromAlarmResponse(string alarmCode)
        {
            try
            {
                if (_status != EquipmentStatus.Alarm)
                    SetStatus(EquipmentStatus.Alarm);

                QMC.Common.Log.Write("Main", "SYSTEM", "SetAlarmStateFromAlarmResponse",
                    "Machine status set to Alarm by alarm response. code=" + alarmCode + " - Ok");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "SetAlarmStateFromAlarmResponse",
                    "Machine status alarm update failed. code=" + alarmCode + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

    }
}
