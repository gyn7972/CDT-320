using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Main 시퀀스 ? 등록된 유닛 시퀀스들을 병렬 Task 로 띄우고 전체 라이프사이클을 관리.
    /// <para>
    /// FSM 단계:
    /// <list type="bullet">
    ///   <item><description><see cref="CoordinatorStep.Init"/> ? 옵션 검증 + 시그널버스 초기화.</description></item>
    ///   <item><description><see cref="CoordinatorStep.Spawning"/> ? Units 플래그에 해당하는 시퀀스 Task 시작.</description></item>
    ///   <item><description><see cref="CoordinatorStep.Running"/> ? 모든 child 완료/실패까지 감시.</description></item>
    ///   <item><description><see cref="CoordinatorStep.Stopping"/> ? child cancel + 시그널버스 종료.</description></item>
    ///   <item><description><see cref="CoordinatorStep.Done"/> ? 완료.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class AutoSequenceCoordinator : SequenceBase<AutoSequenceCoordinator.CoordinatorStep>
    {
        /// <summary>Coordinator FSM step.</summary>
        public enum CoordinatorStep
        {
            /// <summary>초기화.</summary>
            Init,

            /// <summary>child 시퀀스 생성.</summary>
            Spawning,

            /// <summary>병렬 감시.</summary>
            Running,

            /// <summary>정지/정리.</summary>
            Stopping,

            /// <summary>완료.</summary>
            Done
        }

        // 유닛별 시퀀스 팩토리 ? 등록된 것만 옵션 플래그에 따라 spawn.
        private readonly Dictionary<SequenceUnitKind, Func<ISequence>> _factories
            = new Dictionary<SequenceUnitKind, Func<ISequence>>();

        // 실제로 spawn 된 시퀀스 인스턴스.
        private readonly Dictionary<SequenceUnitKind, ISequence> _active
            = new Dictionary<SequenceUnitKind, ISequence>();

        private readonly List<Task> _childTasks = new List<Task>();
        private CancellationTokenSource _childCts;
        private SequenceRunOptions _options = SequenceRunOptions.FullAuto();

        public AutoSequenceCoordinator(ISequenceContext ctx)
            : base(ctx, SequenceUnitKind.None, "Main") { }

        /// <summary>현재 활성 child 시퀀스 (읽기 전용).</summary>
        public IReadOnlyDictionary<SequenceUnitKind, ISequence> ActiveSequences => _active;

        /// <summary>유닛 시퀀스 팩토리 등록. Configure 의 Units 플래그에 포함될 때만 spawn.</summary>
        public void Register(SequenceUnitKind unit, Func<ISequence> factory)
        {
            if (factory == null) return;
            _factories[unit] = factory;
        }

        /// <summary>실행 옵션 설정 (RunAsync 전에 호출).</summary>
        public void Configure(SequenceRunOptions options)
        {
            _options = options ?? SequenceRunOptions.FullAuto();
        }

        protected override CoordinatorStep InitialStep => CoordinatorStep.Init;

        protected override async Task<CoordinatorStep?> StepAsync(CoordinatorStep step, CancellationToken ct)
        {
            switch (step)
            {
                case CoordinatorStep.Init:
                    Ctx.Log($"[MAIN] Init ? units={_options.Units}, mode={_options.Mode}");
                    _active.Clear();
                    _childTasks.Clear();
                    _childCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    return CoordinatorStep.Spawning;

                case CoordinatorStep.Spawning:
                    foreach (var kv in _factories)
                    {
                        if ((_options.Units & kv.Key) == 0) continue;
                        var seq = kv.Value();
                        if (seq == null) continue;
                        seq.Mode = _options.Mode;
                        _active[kv.Key] = seq;
                        var childCt = _childCts.Token;
                        _childTasks.Add(Task.Run(() => seq.RunAsync(childCt), childCt));
                        Ctx.Log($"[MAIN] spawned {seq.Name} ({kv.Key})");
                    }
                    if (_childTasks.Count == 0)
                    {
                        Ctx.Log("[MAIN] 구동할 유닛 없음 ? 종료");
                        return CoordinatorStep.Done;
                    }
                    return CoordinatorStep.Running;

                case CoordinatorStep.Running:
                    await Task.WhenAll(_childTasks).ConfigureAwait(false);
                    Ctx.Log("[MAIN] 모든 child 시퀀스 종료");
                    return CoordinatorStep.Stopping;

                case CoordinatorStep.Stopping:
                    Ctx.Signals?.CompleteAll();
                    int faulted = _active.Values.Count(s => s.State == SequenceState.Faulted);
                    Ctx.Log($"[MAIN] Stopping ? faulted={faulted}");
                    return CoordinatorStep.Done;

                case CoordinatorStep.Done:
                default:
                    return null;
            }
        }

        // ──────────────────────────────────────────
        //  외부 제어 (MachineController 가 위임)
        // ──────────────────────────────────────────

        /// <summary>전체 child Pause.</summary>
        public void PauseAll() { foreach (var s in _active.Values) s.Pause(); }

        /// <summary>전체 child Resume.</summary>
        public void ResumeAll() { foreach (var s in _active.Values) s.Resume(); }

        /// <summary>특정 유닛 Pause.</summary>
        public void PauseUnit(SequenceUnitKind unit)
        {
            if (_active.TryGetValue(unit, out var s)) s.Pause();
        }

        /// <summary>특정 유닛 Resume.</summary>
        public void ResumeUnit(SequenceUnitKind unit)
        {
            if (_active.TryGetValue(unit, out var s)) s.Resume();
        }

        /// <summary>Manual 모드 유닛에 한 step 진행 허용.</summary>
        public void StepUnit(SequenceUnitKind unit)
        {
            if (_active.TryGetValue(unit, out var s)) s.StepOnce();
        }

        /// <summary>모든 Manual 모드 유닛에 한 step 진행 허용.</summary>
        public void StepAll() { foreach (var s in _active.Values) s.StepOnce(); }

        /// <summary>child 시퀀스 강제 취소 (Coordinator 자체 RunAsync 의 ct 와 별개).</summary>
        public void AbortChildren()
        {
            try { _childCts?.Cancel(); } catch { }
        }
    }
}
