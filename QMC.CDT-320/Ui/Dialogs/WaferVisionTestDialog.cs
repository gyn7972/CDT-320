using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class WaferVisionTestDialog : Form
    {
        public static void Open(IWin32Window owner)
        {
            ModelessDialogHost.Show("WaferVisionTestDialog", owner, () => new WaferVisionTestDialog());
        }

        public WaferVisionTestDialog()
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
                return;

            waferVisionTestControl.Configure();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            waferVisionTestControl.StopLive();
            base.OnFormClosing(e);
        }
    }
}
