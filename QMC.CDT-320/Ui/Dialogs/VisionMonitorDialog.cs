using QMC.CDT_320.Ui.Localization;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    public sealed partial class VisionMonitorDialog : Form
    {
        public static void Open(IWin32Window owner)
        {
            ModelessDialogHost.Show("VisionMonitorDialog", owner, () => new VisionMonitorDialog());
        }

        public VisionMonitorDialog()
        {
            InitializeComponent();
            InitializeLanguageBindings();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            visionMonitorControl.Disconnect();
            base.OnFormClosing(e);
        }
        // Keep Designer serialization declarative; register display resources after controls exist.
        private void InitializeLanguageBindings()
        {
            Lang.BindKey(this, "visionUi.visionMonitorDialog.Text.text");
        }
    }
}
