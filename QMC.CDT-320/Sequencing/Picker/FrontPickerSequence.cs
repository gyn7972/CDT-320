using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.Sequencing
{
    public sealed class FrontPickerSequence : UnitSequenceBase
    {
        private PickerProcessSequence _stepSequence;

        public FrontPickerSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.TpuLeft, "FrontPicker")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var options = PickerSequenceOptions.Default();
                options.RunMode = Mode;
                int result = await new PickerProcessSequence(Context, PickerSequenceSide.Front)
                    .RunAsync(ct, options)
                    .ConfigureAwait(false);
                if (result != 0)
                    throw new System.InvalidOperationException("FrontPicker sequence failed. result=" + result);
            }
        }

        protected override Task ExecuteStepAsync(CancellationToken ct)
        {
            if (_stepSequence == null || _stepSequence.IsComplete)
                _stepSequence = new PickerProcessSequence(Context, PickerSequenceSide.Front);

            var options = PickerSequenceOptions.Default();
            options.RunMode = Mode;
            return RunStepSequenceAsync(ct, options);
        }

        private async Task RunStepSequenceAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            int result = await _stepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (result != 0)
                throw new System.InvalidOperationException("FrontPicker step sequence failed. result=" + result);

            if (_stepSequence.IsComplete)
                _stepSequence = null;
        }
    }
}
