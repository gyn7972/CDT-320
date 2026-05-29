using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Input Loader 시퀀스 ? 카세트의 웨이퍼를 차례로 InputStage 교환 위치까지 공급.
    /// <para>
    /// 흐름: LoadNext → (성공 시 WaferTicket 게시) → WaitConsumed → Retract → 다음 슬롯.
    /// 더 이상 웨이퍼가 없으면 Done.
    /// </para>
    /// </summary>
    public sealed class InputLoaderSequence : SequenceBase<InputLoaderSequence.Step>
    {
        /// <summary>Input Loader FSM step.</summary>
        public enum Step
        {
            /// <summary>다음 웨이퍼 로드 (교환 위치까지).</summary>
            LoadNext,

            /// <summary>InputStage 가 웨이퍼를 소비할 때까지 대기.</summary>
            WaitConsumed,

            /// <summary>피더 후퇴.</summary>
            Retract,

            /// <summary>완료.</summary>
            Done
        }

        public InputLoaderSequence(ISequenceContext ctx)
            : base(ctx, SequenceUnitKind.InputLoader, "InputLoader") { }

        protected override Step InitialStep => Step.LoadNext;

        protected override async Task<Step?> StepAsync(Step step, CancellationToken ct)
        {
            var mc = Ctx.Controller;
            switch (step)
            {
                case Step.LoadNext:
                    bool loaded = await mc.LoadNextWaferAsync().ConfigureAwait(false);
                    if (!loaded)
                    {
                        Ctx.Log("[SEQ:InputLoader] 더 이상 웨이퍼 없음 → Done");
                        return Step.Done;
                    }
                    // 교환 위치 도달 → InputStage 에 티켓 게시
                    Ctx.Signals.LoaderToStage.Post(new WaferTicket
                    {
                        SlotIndex = mc.CurrentInputSlot,
                        WaferId   = "WAFER-" + mc.CurrentInputSlot
                    }, ct);
                    return Step.WaitConsumed;

                case Step.WaitConsumed:
                    // InputStage 가 웨이퍼를 받으면 MachineController 가 InputWaferAtExchange=false 로 변경.
                    // (LoadNextWaferAsync 내부 handoff 가 이미 처리하는 경우 즉시 통과.)
                    while (mc.InputWaferAtExchange && !ct.IsCancellationRequested)
                        await Task.Delay(100, ct).ConfigureAwait(false);
                    return Step.Retract;

                case Step.Retract:
                    await mc.RetractCurrentWaferAsync().ConfigureAwait(false);
                    return Step.LoadNext;

                case Step.Done:
                default:
                    Ctx.Signals.LoaderToStage.Complete();
                    return null;
            }
        }
    }
}
