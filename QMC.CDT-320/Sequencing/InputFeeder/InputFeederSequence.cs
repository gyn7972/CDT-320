using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    public sealed class InputFeederSequence
    {
        private readonly MachineSequenceContext _context;

        public InputFeederSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException("context");
        }

        public Task<int> RunLoadFromCassetteAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return new InputFeederLoadFromCassetteSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunLoadToStageAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return new InputFeederLoadToStageSequence(_context).RunAsync(ct, options);
        }

        // [P2 2026-08-22 재시작 바코드 게이트] 자재가 이미 InputStage에 있는 재시작 상황에서
        // 스테이지 데이터 검증 -> 바코드 판독 -> 공정 위치 복귀 -> 카세트 Avoid까지
        // 정상 이적재 경로의 후반부 스텝을 그대로 수행한다. options.StartMode=Restart 권장.
        public Task<int> RunStageBarcodeRecoveryAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return new InputFeederLoadToStageSequence(_context, true).RunAsync(ct, options);
        }

        public Task<int> RunUnloadFromStageAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return new InputFeederUnloadFromStageSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunUnloadToCassetteAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return new InputFeederUnloadToCassetteSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunExchangeAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return new InputFeederExchangeSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunRecoverAsync(CancellationToken ct, InputFeederSequenceOptions options)
        {
            return new InputFeederRecoverSequence(_context).RunAsync(ct, options);
        }
    }
}
