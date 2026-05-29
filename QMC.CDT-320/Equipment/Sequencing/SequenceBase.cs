using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// switch-case FSM 기반 시퀀스 베이스.
    /// <para>
    /// 파생 클래스는 <typeparamref name="TStep"/> enum 으로 단계를 정의하고
    /// <see cref="StepAsync"/> 에서 현재 step 을 처리한 뒤 다음 step 을 반환한다.
    /// null 을 반환하면 시퀀스 완료(Completed).
    /// </para>
    /// <para>
    /// RunAsync 루프는 매 step 전에 다음 게이트를 검사한다:
    /// <list type="number">
    ///   <item><description>Pause 게이트 ? Pause() 호출 시 Resume() 까지 블록.</description></item>
    ///   <item><description>Manual 게이트 ? Mode==Manual 이면 StepOnce() 신호까지 블록.</description></item>
    ///   <item><description>Alarm Hold ? Ctx.HasBlockingAlarm 동안 100ms polling 으로 대기.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public abstract class SequenceBase<TStep> : ISequence
        where TStep : struct, Enum
    {
        private readonly ManualResetEventSlim _pauseGate = new ManualResetEventSlim(true);
        private readonly SemaphoreSlim _manualTick = new SemaphoreSlim(0, 1);
        private SequenceState _state = SequenceState.Idle;
        private TStep _step;

        /// <summary>시퀀스 컨텍스트 (하드웨어/인터록/시그널 접근).</summary>
        protected ISequenceContext Ctx { get; }

        protected SequenceBase(ISequenceContext ctx, SequenceUnitKind unit, string name)
        {
            Ctx  = ctx ?? throw new ArgumentNullException(nameof(ctx));
            Unit = unit;
            Name = name;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public SequenceUnitKind Unit { get; }

        /// <inheritdoc/>
        public SequenceRunMode Mode { get; set; } = SequenceRunMode.Auto;

        /// <inheritdoc/>
        public SequenceState State => _state;

        /// <inheritdoc/>
        public string CurrentStep => _step.ToString();

        /// <inheritdoc/>
        public event Action<ISequence, SequenceState> StateChanged;

        /// <summary>FSM 의 시작 step.</summary>
        protected abstract TStep InitialStep { get; }

        /// <summary>
        /// 현재 step 을 처리하고 다음 step 을 반환. null 반환 = 시퀀스 완료.
        /// </summary>
        protected abstract Task<TStep?> StepAsync(TStep step, CancellationToken ct);

        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken ct)
        {
            _step = InitialStep;
            SetState(SequenceState.Running);
            Ctx.Log($"[SEQ:{Name}] start (mode={Mode}, step={_step})");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // 1) Pause 게이트
                    if (!_pauseGate.IsSet)
                    {
                        SetState(SequenceState.Paused);
                        _pauseGate.Wait(ct);
                        SetState(SequenceState.Running);
                    }

                    // 2) Manual 게이트
                    if (Mode == SequenceRunMode.Manual)
                    {
                        SetState(SequenceState.Paused);
                        await _manualTick.WaitAsync(ct).ConfigureAwait(false);
                        SetState(SequenceState.Running);
                    }

                    // 3) Alarm Hold
                    while (Ctx.HasBlockingAlarm && !ct.IsCancellationRequested)
                    {
                        if (_state != SequenceState.Paused) SetState(SequenceState.Paused);
                        await Task.Delay(100, ct).ConfigureAwait(false);
                    }
                    if (ct.IsCancellationRequested) break;
                    if (_state == SequenceState.Paused) SetState(SequenceState.Running);

                    // 4) step 실행
                    TStep? next = await StepAsync(_step, ct).ConfigureAwait(false);
                    if (next == null)
                    {
                        SetState(SequenceState.Completed);
                        Ctx.Log($"[SEQ:{Name}] completed");
                        return;
                    }
                    _step = next.Value;
                }

                SetState(SequenceState.Aborted);
                Ctx.Log($"[SEQ:{Name}] aborted (cancel)");
            }
            catch (OperationCanceledException)
            {
                SetState(SequenceState.Aborted);
                Ctx.Log($"[SEQ:{Name}] aborted (cancel)");
            }
            catch (Exception ex)
            {
                SetState(SequenceState.Faulted);
                Ctx.Alarm("SEQ-" + Name, Name, "시퀀스 예외: " + ex.Message);
                Ctx.Log($"[SEQ:{Name}] FAULTED: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void Pause() => _pauseGate.Reset();

        /// <inheritdoc/>
        public void Resume() => _pauseGate.Set();

        /// <inheritdoc/>
        public void StepOnce()
        {
            if (_manualTick.CurrentCount == 0)
            {
                try { _manualTick.Release(); } catch { }
            }
        }

        private void SetState(SequenceState s)
        {
            if (_state == s) return;
            _state = s;
            var h = StateChanged;
            if (h != null) try { h(this, s); } catch { }
        }
    }
}
