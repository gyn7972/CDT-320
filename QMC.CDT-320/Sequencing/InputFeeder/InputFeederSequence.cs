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
