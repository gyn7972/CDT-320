using System;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class VisionCameraCalibrationDialog
    {
        private void btnRunAll_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("PREPARE && FIND BOTTOM", ct => Sequence.RunAsync(ct, QMC.CDT320.Sequencing.SequenceRunMode.Manual));
        }

        private void btnFindBottom_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("FIND BOTTOM", ct => Sequence.FindBottomReticleAsync(ct), ManualCalibrationReadinessTarget.Bottom);
        }

        private void btnFindInput_Click(object sender, EventArgs e)
        {
            // 성공 시에만 세션 측정 기록을 남긴다(SAVE / APPLY 가드용).
            _ = RunOperationAsync("FIND INPUT",
                ct => Sequence.PrepareAndFindInputReticleAsync(ct, QMC.CDT320.Sequencing.SequenceRunMode.Manual),
                ManualCalibrationReadinessTarget.None,
                () => MarkReticleMeasuredInSession(true, false));
        }

        private void btnFindOutput_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("FIND OUTPUT",
                ct => Sequence.PrepareAndFindOutputReticleAsync(ct, QMC.CDT320.Sequencing.SequenceRunMode.Manual),
                ManualCalibrationReadinessTarget.None,
                () => MarkReticleMeasuredInSession(false, true));
        }

        private void btnRetractReticle_Click(object sender, EventArgs e)
        {
            _ = RunOperationAsync("RETICLE BACK", ct => Sequence.RetractReticleFromBottomCameraAsync(ct, QMC.CDT320.Sequencing.SequenceRunMode.Manual));
        }

        private void btnCalculateSave_Click(object sender, EventArgs e)
        {
            SaveAndApplyCameraValues(true);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
