using System.Windows.Forms;
using QMC.CDT320.VisionComm;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>측면 Front+Rear 동시 뷰어 팝업. UI 는 Designer, 런타임 Configure 만 여기서.</summary>
    public sealed partial class SideVisionViewerDialog : Form
    {
        public SideVisionViewerDialog()
        {
            InitializeComponent();
            _side.Configure(VisionHub.Host);
        }

        public static void Open(IWin32Window owner)
        {
            using (var dlg = new SideVisionViewerDialog())
                dlg.ShowDialog(owner);
        }
    }
}
