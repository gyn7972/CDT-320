using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    public sealed class FrontPickerSequence : UnitSequenceBase
    {
        private PickerProcessSequence _stepSequence;

        public FrontPickerSequence(MachineSequenceContext ctx)
            : base(ctx, SequenceUnitKind.PickerFront, "FrontPicker")
        {
        }

        protected override async Task ExecuteAutoAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await WaitForPickerWorkAsync(ct).ConfigureAwait(false);

                PickerSequenceOptions options = BuildSequenceOptions();
                int result = await new PickerProcessSequence(Context, PickerSequenceSide.Front)
                    .RunAsync(ct, options)
                    .ConfigureAwait(false);
                if (result != 0)
                    throw new System.InvalidOperationException(
                        SequenceFailureStore.AppendRecentDetail(
                            "FrontPicker sequence failed. result=" + result,
                            "FrontPicker",
                            "FRONT-PICKER-SEQUENCE"));

                Context.StopIfCycleStopRequested("FrontPickerSequence.ProcessComplete");
            }
        }

        protected override Task ExecuteStepAsync(CancellationToken ct)
        {
            if (_stepSequence == null || _stepSequence.IsComplete)
                _stepSequence = new PickerProcessSequence(Context, PickerSequenceSide.Front);

            PickerSequenceOptions options = BuildSequenceOptions();
            return RunStepSequenceAsync(ct, options);
        }

        private PickerSequenceOptions BuildSequenceOptions()
        {
            PickerSequenceOptions options = PickerSequenceOptions.Default();
            options.RunMode = Mode;
            options.SimulateVisionResult = IsSimulationOrDryRun();
            return options;
        }

        private bool IsSimulationOrDryRun()
        {
            try
            {
                if (QMC.CDT320.AppSettingsStore.Current != null &&
                    (QMC.CDT320.AppSettingsStore.Current.SimulationMode ||
                     QMC.CDT320.AppSettingsStore.Current.DryRunMode))
                    return true;

                if (Context != null && Context.Controller != null && Context.Controller.GlobalDryRun)
                    return true;

                if (Context != null &&
                    Context.Machine != null &&
                    Context.Machine.InputStageUnit != null &&
                    Context.Machine.InputStageUnit.IsInputStageSimulationOrDryRun())
                    return true;

                if (Context != null &&
                    Context.Machine != null &&
                    Context.Machine.PickerFrontUnit != null &&
                    Context.Machine.PickerFrontUnit.Config != null &&
                    Context.Machine.PickerFrontUnit.Config.bDryRun)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private async Task RunStepSequenceAsync(CancellationToken ct, PickerSequenceOptions options)
        {
            int result = await _stepSequence.RunAsync(ct, options).ConfigureAwait(false);
            if (result != 0)
                throw new System.InvalidOperationException(
                    SequenceFailureStore.AppendRecentDetail(
                        "FrontPicker step sequence failed. result=" + result,
                        "FrontPicker",
                        "FRONT-PICKER-STEP"));

            if (_stepSequence.IsComplete)
                _stepSequence = null;
        }

        private async Task WaitForPickerWorkAsync(CancellationToken ct)
        {
            while (!HasPickerWork())
            {
                ct.ThrowIfCancellationRequested();
                Context.StopIfCycleStopRequested("FrontPickerSequence.WaitForWork");

                await Task.Delay(200, ct).ConfigureAwait(false);
            }
        }

        private static bool HasPickerWork()
        {
            if (MaterialStateService.HasReadyInputStagePickTarget())
                return true;

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                if (MaterialStateService.GetDieAtPicker(MaterialLocationKind.PickerFront, pickerNo) != null)
                    return true;
            }

            return false;
        }
    }
}
