using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed class SideVisionFocusCalibrationDialog : VisionFocusCalibrationDialog
    {
        public new static SideVisionFocusCalibrationDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "SideVisionFocusCalibrationDialog",
                owner,
                () => new SideVisionFocusCalibrationDialog());
        }

        public SideVisionFocusCalibrationDialog()
            : base(VisionFocusCalibrationDialogProfile.SideOnly)
        {
        }
    }
}
