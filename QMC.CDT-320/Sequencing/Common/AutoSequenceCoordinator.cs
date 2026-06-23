using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;

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
        private const int AbortPendingWaitLogIntervalMs = 3000;
        private const int CycleStopPendingWaitTimeoutMs = 15000;
        private const int PendingAbortFinishTimeoutMs = 5000;
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
            if (_active.Count == 0)
            {
                _ctx.LogPublic("[SEQ] 실행할 활성 유닛이 없습니다.");
                return;
            }

            CancellationTokenSource childrenCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _childrenCts = childrenCts;
            CancellationToken childrenToken = childrenCts.Token;
            var tasks = new List<Task>();
            foreach (var sequence in _active.Values)
                tasks.Add(Task.Run(() => sequence.RunAsync(childrenToken), childrenToken));

            _ctx.LogPublic("[SEQ] Run start (" + tasks.Count + " units)");
            try
            {
                await WaitAllOrCancelOnFirstFailureAsync(tasks, childrenToken).ConfigureAwait(false);
                _ctx.LogPublic("[SEQ] Run complete");
            }
            catch (SequenceStopException)
            {
                _ctx.LogPublic("[SEQ] Run stopped");
                AbortChildren();
                await AwaitPendingAfterAbortAsync(tasks).ConfigureAwait(false);
                throw;
            }
            catch (OperationCanceledException)
            {
                _ctx.LogPublic("[SEQ] Run canceled");
                AbortChildren();
                await AwaitPendingAfterAbortAsync(tasks).ConfigureAwait(false);
                throw;
            }
            finally
            {
                if (_childrenCts == childrenCts)
                    _childrenCts = null;

                childrenCts.Dispose();
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
                    if (HasCriticalActiveAlarm())
                    {
                        AbortChildren();
                        await AwaitPendingAfterAbortAsync(pending).ConfigureAwait(false);
                    }
                    else
                    {
                        _ctx.RequestCycleStop();
                        _ctx.LogPublic("[SEQ] 유닛 알람 발생. 다른 유닛은 현재 작업 경계에서 정지합니다.");
                        await AwaitPendingAfterCycleStopAsync(pending).ConfigureAwait(false);
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

        private async Task AwaitPendingAfterCycleStopAsync(List<Task> pending)
        {
            if (pending == null || pending.Count == 0)
                return;

            try
            {
                Task allPending = Task.WhenAll(pending);
                bool waitLogWritten = false;
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

