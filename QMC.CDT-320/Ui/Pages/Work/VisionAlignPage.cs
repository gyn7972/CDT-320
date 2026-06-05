using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    public partial class VisionAlignPage : PageBase
    {
        public VisionAlignPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("work.visionAlign");
            lblHeader.Tag = "i18n:work.visionAlign";
        }
    }

    public partial class WaferMapOpenPage : PageBase
    {
        public WaferMapOpenPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            LoadSampleMapList();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("work.waferMapOpen");
            lblHeader.Tag = "i18n:work.waferMapOpen";
        }

        private void LoadSampleMapList()
        {
            lbMapFiles.Items.Clear();
            lbMapFiles.Items.Add("Y482CB1_2026-04-24.map");
            lbMapFiles.Items.Add("Y482CB0_2026-04-23.map");
            lbMapFiles.Items.Add("SAMPLE.map");
        }
    }
}
