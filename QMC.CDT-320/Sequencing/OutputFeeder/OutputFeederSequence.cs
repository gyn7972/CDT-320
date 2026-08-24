using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    public sealed class OutputFeederSequence
    {
        private readonly MachineSequenceContext _context;

        public OutputFeederSequence(MachineSequenceContext context)
        {
            _context = context;
        }

        public Task<int> RunLoadFromCassetteAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            return new OutputFeederLoadFromCassetteSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunLoadToStageAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            return new OutputFeederLoadToStageSequence(_context).RunAsync(ct, options);
        }

        // [P2 2026-08-22 재시작 바코드 게이트] Bin이 이미 OutputStage에 있는 재시작 상황에서
        // Ring 전달 재확인 -> 바코드 판독 -> 공정 위치 복귀까지 정상 이적재 경로의 후반부
        // 스텝을 그대로 수행한다. options.StartMode=Restart 권장.
        public Task<int> RunStageBarcodeRecoveryAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            return new OutputFeederLoadToStageSequence(_context, true).RunAsync(ct, options);
        }

        public Task<int> RunUnloadFromStageAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            return new OutputFeederUnloadFromStageSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunUnloadToCassetteAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            return new OutputFeederUnloadToCassetteSequence(_context).RunAsync(ct, options);
        }

        internal Task<int> RunUnloadToCassetteWithHeldResourcesAsync(
            CancellationToken ct,
            OutputFeederSequenceOptions options,
            SequenceResourceLease outputPlaceAreaLease,
            SequenceResourceLease outputStageAreaLease)
        {
            return new OutputFeederUnloadToCassetteSequence(
                _context,
                outputPlaceAreaLease,
                outputStageAreaLease).RunAsync(ct, options);
        }

        public Task<int> RunExchangeAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            return new OutputFeederExchangeSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunRecoverAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            return new OutputFeederRecoverSequence(_context).RunAsync(ct, options);
        }
    }
}
