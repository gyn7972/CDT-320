using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    public sealed class InputCassetteSequence
    {
        private readonly MachineSequenceContext _context;

        public InputCassetteSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<int> RunLoadingAsync(CancellationToken ct)
        {
            return RunLoadingAsync(ct, InputCassetteSequenceOptions.Default());
        }

        public Task<int> RunLoadingAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            return new InputCassetteLoadingSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunMappingAsync(CancellationToken ct)
        {
            return RunMappingAsync(ct, InputCassetteSequenceOptions.Default());
        }

        public Task<int> RunMappingAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            return new InputCassetteMappingSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunUnloadingAsync(CancellationToken ct)
        {
            return RunUnloadingAsync(ct, InputCassetteSequenceOptions.Default());
        }

        public Task<int> RunUnloadingAsync(CancellationToken ct, InputCassetteSequenceOptions options)
        {
            return new InputCassetteUnloadingSequence(_context).RunAsync(ct, options);
        }
    }
}
