using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    public sealed class OutputCassetteSequence
    {
        private readonly MachineSequenceContext _context;

        public OutputCassetteSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException("context");
        }

        public Task<int> RunLoadingAsync(CancellationToken ct)
        {
            return RunLoadingAsync(ct, OutputCassetteSequenceOptions.Default());
        }

        public Task<int> RunLoadingAsync(CancellationToken ct, OutputCassetteSequenceOptions options)
        {
            return new OutputCassetteLoadingSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunMappingAsync(CancellationToken ct)
        {
            return RunMappingAsync(ct, OutputCassetteSequenceOptions.Default());
        }

        public Task<int> RunMappingAsync(CancellationToken ct, OutputCassetteSequenceOptions options)
        {
            return new OutputCassetteMappingSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunUnloadingAsync(CancellationToken ct)
        {
            return RunUnloadingAsync(ct, OutputCassetteSequenceOptions.Default());
        }

        public Task<int> RunUnloadingAsync(CancellationToken ct, OutputCassetteSequenceOptions options)
        {
            return new OutputCassetteUnloadingSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunMoveSlotAsync(CancellationToken ct)
        {
            return RunMoveSlotAsync(ct, OutputCassetteSequenceOptions.Default());
        }

        public Task<int> RunMoveSlotAsync(CancellationToken ct, OutputCassetteSequenceOptions options)
        {
            return new OutputCassetteMoveSlotSequence(_context).RunAsync(ct, options);
        }
    }
}
