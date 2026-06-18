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
            // 이 유닛(및 하위 시퀀스)의 모든 공개 로그를 유닛 종류에 맞는 EventKind 로 분류한다.
            using (SequenceLog.Push(SequenceLog.FromUnitKind(Kind), Name, null))
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

        protected async Task<SequenceResourceLease> AcquireResourceForRunAsync(
            SequenceResourceKind resource,
            string holder,
            int manualTimeoutMs,
            CancellationToken ct)
        {
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? Name : holder;
            try
            {
                if (Mode != SequenceRunMode.Auto)
                {
                    return await Context.Resources
                        .AcquireAsync(resource, safeHolder, manualTimeoutMs, ct)
                        .ConfigureAwait(false);
                }

                bool waitLogged = false;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    Context.StopIfCycleStopRequested(Name + ".AcquireResource:" + resource);

                    SequenceResourceLease lease = await Context.Resources
                        .AcquireAsync(resource, safeHolder, 200, ct, false)
                        .ConfigureAwait(false);
                    if (lease != null)
                    {
                        if (waitLogged)
                        {
                            Context.LogPublic("[SEQ] " + Name + " 리소스 대기 완료. resource=" +
                                resource + ", holder=" + safeHolder);
                        }

                        return lease;
                    }

                    if (!waitLogged)
                    {
                        string currentHolder = Context.Resources.GetHolder(resource);
                        Context.LogPublic("[SEQ] " + Name + " 리소스 사용 대기 중입니다. resource=" +
                            resource + ", holder=" + safeHolder + ", current=" +
                            (string.IsNullOrWhiteSpace(currentHolder) ? "-" : currentHolder));
                        waitLogged = true;
                    }

                    await Task.Delay(100, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SequenceStopException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Context.LogPublic("[SEQ] " + Name + " 리소스 점유 중 예외가 발생했습니다. resource=" +
                    resource + ", holder=" + safeHolder + ", error=" + ex.Message);
                throw;
            }
            finally
            {
            }
        }
    }
}

