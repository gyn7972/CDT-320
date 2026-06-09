using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    public sealed class InputStageSequence
    {
        private readonly MachineSequenceContext _context;

        public InputStageSequence(MachineSequenceContext context)
        {
            _context = context ?? throw new ArgumentNullException("context");
        }

        public Task<int> RunPrepareLoadAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            return new InputStagePrepareLoadSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunAlignAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            return new InputStageAlignSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunPrepareUnloadAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            return new InputStagePrepareUnloadSequence(_context).RunAsync(ct, options);
        }

        public Task<int> RunMoveAvoidAsync(CancellationToken ct, InputStageSequenceOptions options)
        {
            return new InputStageMoveAvoidSequence(_context).RunAsync(ct, options);
        }
    }
}
