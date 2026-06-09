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

        public Task<int> RunUnloadFromStageAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            return new OutputFeederUnloadFromStageSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunUnloadToCassetteAsync(CancellationToken ct, OutputFeederSequenceOptions options)
        {
            return new OutputFeederUnloadToCassetteSequence(_context).RunAsync(ct, options);
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
