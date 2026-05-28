using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class HeadPage : PageBase
    {
        public HeadPage()
            : this("wi.frontHead")
        {
        }

        public HeadPage(string headTitleI18n)
        {
            InitializeComponent();
            lblHeader.Tag = "i18n:" + headTitleI18n;
            lblHeader.Text = Lang.T(headTitleI18n);
        }
    }
}
