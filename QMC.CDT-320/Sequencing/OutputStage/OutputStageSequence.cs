using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    public sealed class OutputStageSequence
    {
        private readonly MachineSequenceContext _context;

        public OutputStageSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException("context");
        }

        public Task<int> RunPrepareLoadAsync(CancellationToken ct, OutputStageSequenceOptions options)
        {
            return new OutputStagePrepareLoadSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunPrepareUnloadAsync(CancellationToken ct, OutputStageSequenceOptions options)
        {
            return new OutputStagePrepareUnloadSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunReceiveDieAsync(CancellationToken ct, OutputStageSequenceOptions options)
        {
            return new OutputStageReceiveDieSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunInspectBinAsync(CancellationToken ct, OutputStageSequenceOptions options)
        {
            return new OutputStageInspectBinSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunMoveAvoidAsync(CancellationToken ct, OutputStageSequenceOptions options)
        {
            return new OutputStageMoveAvoidSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunMoveProcessAsync(CancellationToken ct, OutputStageSequenceOptions options)
        {
            return new OutputStageMoveProcessSequence(_context).RunAsync(ct, options);
        }
    }
}
