using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Bin;
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

        public async Task<bool> ExecuteMappingAsync(CancellationToken ct, bool bFine = false)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var loader = Context.Machine.InputLoader;
                if (loader == null || loader.InputCassette == null)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "SEQ-INMAP-UNIT",
                        "InputLoaderSequence",
                        "InputLoader 또는 InputCassette Unit을 찾을 수 없습니다.");
                    Context.LogPublic("[UNIT-INPUT-LOADER] InputCassette mapping failed: unit missing");
                    return false;
                }

                if (!loader.CassetteExistSensor.IsOn)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Warning,
                        "LOT-NOCASS",
                        loader.Name,
                        "Input 카세트가 감지되지 않았습니다.");
                    Context.LogPublic("[UNIT-INPUT-LOADER] InputCassette mapping skipped: cassette missing");
                    return false;
                }

                if (!loader.InputCassette.CheckWaferCassetteMappingReady())
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Warning,
                        "SEQ-INMAP-READY",
                        loader.Name,
                        "Input 카세트 매핑 준비 상태가 아닙니다.");
                    Context.LogPublic("[UNIT-INPUT-LOADER] InputCassette mapping failed: not ready");
                    return false;
                }

                Context.LogPublic("[UNIT-INPUT-LOADER] InputCassette mapping start");
                int result = await loader.InputCassette.WaferScan(
                    loader.InputCassette.Config.ElevatorMoveTimeoutMs,
                    bFine).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (result != 0)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Warning,
                        "SEQ-INMAP",
                        loader.Name,
                        "Input 카세트 매핑에 실패했습니다. result=" + result);
                    Context.LogPublic("[UNIT-INPUT-LOADER] InputCassette mapping failed. result=" + result);
                    return false;
                }

                try
                {
                    var arr = new bool[loader.WaferMap.Count];
                    for (int i = 0; i < arr.Length; i++)
                        arr[i] = loader.WaferMap[i];

                    SlotMapperRegistry.Update("InputCassette", arr);
                }
                catch (System.Exception ex)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Warning,
                        "SEQ-INMAP-REG",
                        loader.Name,
                        "Input 카세트 매핑 결과 등록 실패: " + ex.Message);
                    Context.LogPublic("[UNIT-INPUT-LOADER] InputCassette mapping registry update failed: " + ex.Message);
                    return false;
                }

                Context.Controller.ApplyInputCassetteMappingCompleted();
                Context.LogPublic("[UNIT-INPUT-LOADER] InputCassette mapping complete");
                return true;
            }
            catch (System.OperationCanceledException)
            {
                Context.LogPublic("[UNIT-INPUT-LOADER] InputCassette mapping canceled");
                throw;
            }
            catch (System.Exception ex)
            {
                AlarmManager.Raise(
                    AlarmSeverity.Error,
                    "SEQ-INMAP-EX",
                    "InputLoaderSequence",
                    ex.Message);
                Context.LogPublic("[UNIT-INPUT-LOADER] InputCassette mapping exception: " + ex.Message);
                return false;
            }
            finally
            {
            }
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

