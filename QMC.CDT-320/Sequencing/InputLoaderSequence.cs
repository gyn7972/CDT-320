using System.Threading;
using System.Threading.Tasks;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    /// <summary>InputLoader??移댁꽭???ㅼ틪, ?щ’ ?대룞, 援먰솚 ?꾩튂, InputStage ?몃뱶?ㅽ봽瑜??섑뻾?섎뒗 ?쒗?ㅼ엯?덈떎.</summary>
    public class InputLoaderSequence : UnitSequenceBase
    {
        /// <summary>吏?뺥븳 ?쒗??而⑦뀓?ㅽ듃濡?InputLoader ?쒗?ㅻ? ?앹꽦?⑸땲??</summary>
        public InputLoaderSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.InputLoader, "InputLoader")
        {
        }

        /// <summary>Auto 紐⑤뱶?먯꽌 ?ㅼ쓬 ?⑥씠??1留?濡쒕뵫 ?먮쫫???ㅽ뻾?⑸땲??</summary>
        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            await ExecuteLoadOnceAsync(ct).ConfigureAwait(false);
        }

        /// <summary>Manual ?먮뒗 Step 紐⑤뱶?먯꽌 ?ㅼ쓬 ?⑥씠??1留?濡쒕뵫 ?먮쫫???ㅽ뻾?⑸땲??</summary>
        protected override async Task ExecuteStepAsync(CancellationToken ct)
        {
            await ExecuteLoadOnceAsync(ct).ConfigureAwait(false);
        }

        private async Task ExecuteLoadOnceAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Context.LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer ?쒖옉");

            // Stage 01 ??湲곗〈 MachineController 濡쒗듃?ы듃 濡쒕뵫 ?먮쫫???덉쟾?섍쾶 ?ъ궗?⑺븳??
            bool ok = await Context.Controller.LoadNextWaferAsync().ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            if (ok)
            {
                Context.Bus.Set("InputStageReady");
                Context.LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer ?꾨즺");
                return;
            }

            AlarmManager.Raise(
                AlarmSeverity.Warning,
                "SEQ-INLOAD",
                "InputLoaderSequence",
                "InputLoader ?쒗?ㅼ뿉???ㅼ쓬 ?⑥씠??濡쒕뵫???꾨즺?섏? 紐삵뻽?듬땲??");
            Context.LogPublic("[UNIT-INPUT-LOADER] LoadNextWafer ?ㅽ뙣");
        }
    }
}

