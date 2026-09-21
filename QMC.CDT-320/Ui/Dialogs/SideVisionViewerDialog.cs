using QMC.CDT_320.Ui.Localization;
using System.Windows.Forms;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class SideVisionViewerDialog : Form
    {
        public SideVisionViewerDialog()
        {
            InitializeComponent();
            InitializeLanguageBindings();
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
                return;

            sideViewer.Configure(VisionHub.Host);
        }

        public static void Open(IWin32Window owner)
        {
            ModelessDialogHost.Show("SideVisionViewerDialog", owner, () => new SideVisionViewerDialog());
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this, "visionUi.sideVisionViewerDialog.Text.text");
        }
    }
}
