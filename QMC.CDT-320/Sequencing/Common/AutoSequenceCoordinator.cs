using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

            _childrenCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var tasks = new List<Task>();
            foreach (var sequence in _active.Values)
                tasks.Add(Task.Run(() => sequence.RunAsync(_childrenCts.Token), _childrenCts.Token));

            _ctx.LogPublic("[SEQ] Run start (" + tasks.Count + " units)");
            try
            {
                await WaitAllOrCancelOnFirstFailureAsync(tasks, _childrenCts.Token).ConfigureAwait(false);
                _ctx.LogPublic("[SEQ] Run complete");
            }
            catch (SequenceStopException)
            {
                _ctx.LogPublic("[SEQ] Run stopped");
                AbortChildren();
                throw;
            }
            catch (OperationCanceledException)
            {
                _ctx.LogPublic("[SEQ] Run canceled");
                throw;
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
                    AbortChildren();
                    await AwaitPendingAfterAbortAsync(pending).ConfigureAwait(false);
                    Exception ex = completed.Exception != null ? completed.Exception.GetBaseException() : null;
                    if (ex != null)
                        throw ex;
                    throw new InvalidOperationException("Sequence unit failed.");
                }

                await completed.ConfigureAwait(false);
            }
        }

        private static async Task AwaitPendingAfterAbortAsync(List<Task> pending)
        {
            if (pending == null || pending.Count == 0)
                return;

            try
            {
                await Task.WhenAll(pending).ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {
            }
        }

        /// <summary>실행 중인 모든 하위 유닛 시퀀스를 중단합니다.</summary>
        public void AbortChildren()
        {
            var cts = _childrenCts;
            if (cts != null && !cts.IsCancellationRequested)
            {
                _ctx.LogPublic("[SEQ] AbortChildren");
                cts.Cancel();
            }
        }
    }
}
