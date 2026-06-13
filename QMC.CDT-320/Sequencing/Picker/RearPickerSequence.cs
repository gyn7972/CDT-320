using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public sealed class RearPickerSequence : UnitSequenceBase
    {
        private PickerProcessSequence _stepSequence;

        public RearPickerSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.TpuRight, "RearPicker")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await WaitForPickerWorkAsync(ct).ConfigureAwait(false);

                var options = PickerSequenceOptions.Default();
                options.RunMode = Mode;
                int result = await new PickerProcessSequence(Context, PickerSequenceSide.Rear)
                    .RunAsync(ct, options)
                    .ConfigureAwait(false);
                if (result != 0)
                    throw new System.InvalidOperationException(
                        SequenceFailureStore.AppendRecentDetail(
                            "RearPicker sequence failed. result=" + result,
                            "RearPicker",
                            "REAR-PICKER-SEQUENCE"));
            }
        }

        protected override Task ExecuteStepAsync(CancellationToken ct)
        {
            if (_stepSequence == null || _stepSequence.IsComplete)
                _stepSequence = new PickerProcessSequence(Context, PickerSequenceSide.Rear);

            var options = PickerSequenceOptions.Default();
            options.RunMode = Mode;
            return RunStepSequenceAsync(ct, options);
        }

        private async Task RunStepSequenceAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            int result = await _stepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (result != 0)
                throw new System.InvalidOperationException(
                    SequenceFailureStore.AppendRecentDetail(
                        "RearPicker step sequence failed. result=" + result,
                        "RearPicker",
                        "REAR-PICKER-STEP"));

            if (_stepSequence.IsComplete)
                _stepSequence = null;
        }

        private async Task WaitForPickerWorkAsync(CancellationToken ct)
        {
            while (!HasPickerWork())
            {
                ct.ThrowIfCancellationRequested();

                await Task.Delay(200, ct).ConfigureAwait(false);
            }
        }

        private static bool HasPickerWork()
        {
            if (MaterialStateService.HasReadyInputStagePickTarget())
                return true;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (MaterialStateService.GetDieAtPicker(MaterialLocationKind.PickerRear, pickerNo) != null)
                    return true;
            }

            return false;
        }
    }
}
