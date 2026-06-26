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
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            visionMonitorControl.Disconnect();
            base.OnFormClosing(e);
        }
    }
}
