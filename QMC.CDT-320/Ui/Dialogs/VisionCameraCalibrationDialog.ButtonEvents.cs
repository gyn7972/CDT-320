using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class VisionCameraCalibrationDialog
    {
        private void btnRunAll_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("PREPARE && FIND BOTTOM", ct => Sequence.RunAsync(ct));
        }

        private void btnFindBottom_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("FIND BOTTOM", ct => Sequence.FindBottomReticleAsync(ct), ManualCalibrationReadinessTarget.Bottom);
        }

        private void btnFindInput_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("FIND INPUT", ct => Sequence.FindInputReticleAsync(ct), ManualCalibrationReadinessTarget.Input);
        }

        private void btnFindOutput_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("FIND OUTPUT", ct => Sequence.FindOutputReticleAsync(ct), ManualCalibrationReadinessTarget.Output);
        }

        private void btnRetractReticle_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("RETICLE BACK", ct => Sequence.RetractReticleFromBottomCameraAsync(ct));
        }

        private void btnCalculateSave_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("CALC / SAVE", delegate(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                int result = Sequence.CalculateCalibration();
                if (result != 0)
                    return Task.FromResult(result);
                return Task.FromResult(Sequence.SaveCalibration());
            });
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
