using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Alarms;

namespace QMC.CDT320.Sequencing
{
    /// <summary>InputLoader의 카세트 스캔, 슬롯 이동, 교환 위치, InputStage 핸드오프를 수행하는 시퀀스입니다.</summary>
    public class InputLoaderSequence : UnitSequenceBase
    {
        /// <summary>지정한 시퀀스 컨텍스트로 InputLoader 시퀀스를 생성합니다.</summary>
        public InputLoaderSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.InputLoader, "InputLoader")
        {
        }

        /// <summary>Auto 모드에서 다음 웨이퍼 1매 로딩 흐름을 실행합니다.</summary>
        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            await ExecuteLoadOnceAsync(ct).ConfigureAwait(false);
        }

        /// <summary>Manual 또는 Step 모드에서 다음 웨이퍼 1매 로딩 흐름을 실행합니다.</summary>
        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            await ExecuteLoadOnceAsync(ct).ConfigureAwait(false);
        }

        private async Task ExecuteLoadOnceAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Context.LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer 시작");

            // Stage 01 — 기존 MachineController 로트포트 로딩 흐름을 안전하게 재사용한다.
            bool ok = await Context.Controller.LoadNextWaferAsync().ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            if (ok)
            {
                Context.Bus.Set("InputStageReady");
                Context.LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer 완료");
                return;
            }

            AlarmManager.Raise(
                AlarmSeverity.Warning,
                "SEQ-INLOAD",
                "InputLoaderSequence",
                "InputLoader 시퀀스에서 다음 웨이퍼 로딩을 완료하지 못했습니다.");
            Context.LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer 실패");
        }
    }
}
