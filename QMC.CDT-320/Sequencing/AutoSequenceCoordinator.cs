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
        private readonly MachineSequenceContext _ctx;
        private readonly Dictionary<SequenceUnitKind, Func<UnitSequenceBase>> _factories =
            new Dictionary<SequenceUnitKind, Func<UnitSequenceBase>>();
        private readonly Dictionary<SequenceUnitKind, UnitSequenceBase> _active =
            new Dictionary<SequenceUnitKind, UnitSequenceBase>();
        private const int AbortPendingWaitLogIntervalMs = 1000;
        private const int CycleStopPendingWaitTimeoutMs = 5000;
        private const int PendingAbortFinishTimeoutMs = 3000;
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
            _ctx.ResetCycleStopRequest();
            _ctx.WaferCompletion.Configure(
                _options.Mode == SequenceRunMode.Auto &&
                AppSettingsStore.Current.WaferCompleteRunMode == WaferCompleteRunMode.StopAfterDrain);
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
            PickerFirstForwardSequencer.ConfigureActiveSides(
                IsPickerSideActive(PickerSequenceSide.Front),
                IsPickerSideActive(PickerSequenceSide.Rear));
            ConfigureRestartPickerDrain();
            await RestorePendingOutputPostPlaceInspectionAsync(ct).ConfigureAwait(false);

            CancellationTokenSource childrenCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _childrenCts = childrenCts;
            CancellationToken childrenToken = childrenCts.Token;
            var tasks = new List<Task>();
            foreach (var sequence in _active.Values)
                tasks.Add(Task.Run(() => sequence.RunAsync(childrenToken), childrenToken));
            if (_ctx.WaferCompletion.Enabled)
                tasks.Add(Task.Run(() => _ctx.WaferCompletion.RunMonitorAsync(childrenToken), childrenToken));

            _ctx.LogPublic("[SEQ] Run start (" + tasks.Count + " units)");
            try
            {
                await WaitAllOrCancelOnFirstFailureAsync(tasks, childrenToken).ConfigureAwait(false);
                _ctx.LogPublic("[SEQ] Run complete");
                tactScope.Complete();
            }
            catch (SequenceStopException)
            {
                _ctx.LogPublic("[SEQ] Run stopped");
                tactScope.Stop("", "시퀀스가 Cycle Stop 경계에서 정지되었습니다.");
                await AwaitPendingAfterCycleStopAsync(tasks, false).ConfigureAwait(false);
                throw;
            }
            catch (OperationCanceledException)
            {
                _ctx.LogPublic("[SEQ] Run canceled");
                tactScope.Cancel("시퀀스가 취소되었습니다.");
                AbortChildren();
                await AwaitPendingAfterAbortAsync(tasks).ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                tactScope.Fail("", ex.Message);
                throw;
            }
            finally
            {
                if (_childrenCts == childrenCts)
                    _childrenCts = null;

                childrenCts.Dispose();
            }
            }
        }

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
                ResolveRestartPickerDrain(PickerSequenceSide.Front, out frontRequired, out frontRank, out frontReason);

                bool rearRequired;
                int rearRank;
                string rearReason;
                ResolveRestartPickerDrain(PickerSequenceSide.Rear, out rearRequired, out rearRank, out rearReason);

                PickerFirstForwardSequencer.ConfigureResumeDrain(
                    frontRequired,
                    frontRank,
                    rearRequired,
                    rearRank);

                _ctx.LogPublic("[SEQ] Picker restart drain configured. front=" +
                    frontRequired + "(" + frontReason + "), rear=" +
                    rearRequired + "(" + rearReason + ")");
                QMC.Common.Log.Write("Main", "SYSTEM", "AutoSequenceCoordinator",
                    "Picker restart drain configured. frontRequired=" + frontRequired +
                    ", frontRank=" + frontRank +
                    ", frontReason=" + frontReason +
                    ", rearRequired=" + rearRequired +
                    ", rearRank=" + rearRank +
                    ", rearReason=" + rearReason + " - Check");
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
            out string reason)
        {
            required = false;
            rank = PickerFirstForwardSequencer.RankPickUp;
            reason = "no picker work";

            if (!IsPickerSideActive(side))
            {
                reason = "picker side inactive";
                return;
            }

            bool hasPickerDie = false;
            bool hasTargetPickerDie = false;
            MaterialLocationKind location = side == PickerSequenceSide.Front
                ? MaterialLocationKind.PickerFront
                : MaterialLocationKind.PickerRear;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                DieMaterial die = MaterialStateService.GetDieAtPicker(location, pickerNo);
                if (die == null)
                    continue;

                hasPickerDie = true;
                if (die.IsInputTarget)
                    hasTargetPickerDie = true;
            }

            if (hasTargetPickerDie || hasPickerDie)
            {
                required = true;
                rank = PickerFirstForwardSequencer.RankBottomSide;
                reason = hasTargetPickerDie
                    ? "picked die remains on picker; Bottom/Side reinspection required"
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
    }
}

