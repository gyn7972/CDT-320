using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>개별 장비 유닛 시퀀스의 공통 실행 기반 클래스입니다.</summary>
    public abstract class UnitSequenceBase
    {
        private readonly SemaphoreSlim _stepGate = new SemaphoreSlim(0, int.MaxValue);
        private int _stepBusyOrQueued;

        /// <summary>지정한 컨텍스트, 유닛 종류, 이름으로 유닛 시퀀스를 생성합니다.</summary>
        protected UnitSequenceBase(MachineSequenceContext ctx, SequenceUnitKind kind, string name)
        {
            Context = ctx ?? throw new ArgumentNullException(nameof(ctx));
            Kind = kind;
            Name = name ?? kind.ToString();
            Mode = SequenceRunMode.Auto;
        }

        /// <summary>시퀀스 실행 컨텍스트입니다.</summary>
        public MachineSequenceContext Context { get; private set; }

        /// <summary>이 시퀀스가 담당하는 유닛 종류입니다.</summary>
        public SequenceUnitKind Kind { get; private set; }

        /// <summary>로그와 진단에 사용하는 유닛 이름입니다.</summary>
        public string Name { get; private set; }

        /// <summary>현재 유닛 실행 모드입니다.</summary>
        public SequenceRunMode Mode { get; private set; }

        /// <summary>유닛 실행 모드를 설정합니다.</summary>
        public void Configure(SequenceRunMode mode)
        {
            Mode = mode;
        }

        /// <summary>유닛 시퀀스를 현재 실행 모드에 따라 실행합니다.</summary>
        public async Task RunAsync(CancellationToken ct)
        {
            if (Mode == SequenceRunMode.Auto)
            {
                await ExecuteAutoAsync(ct).ConfigureAwait(false);
                return;
            }

            Context.LogPublic("[SEQ] " + Name + " manual/step gate 대기");
            while (!ct.IsCancellationRequested)
            {
                await _stepGate.WaitAsync(ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                try
                {
                    await ExecuteStepAsync(ct).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Exchange(ref _stepBusyOrQueued, 0);
                }
            }
        }

        /// <summary>Manual 또는 Step 모드에서 유닛 1단계 실행 신호를 입력합니다.</summary>
        public void StepUnit()
        {
            if (Interlocked.CompareExchange(ref _stepBusyOrQueued, 1, 0) == 0)
            {
                _stepGate.Release();
                return;
            }

            Context.LogPublic("[SEQ] " + Name + " step ignored: previous step is still running or queued");
        }

        /// <summary>Auto 모드에서 유닛이 수행할 비동기 작업입니다.</summary>
        protected abstract Task ExecuteAutoAsync(CancellationToken ct);

        /// <summary>Manual 또는 Step 모드에서 유닛이 수행할 1단계 비동기 작업입니다.</summary>
        protected virtual Task ExecuteStepAsync(CancellationToken ct)
        {
            return ExecuteAutoAsync(ct);
        }
    }
}
