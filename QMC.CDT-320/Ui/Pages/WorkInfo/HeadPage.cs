using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class HeadPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        public HeadPage()
        {
            InitializeComponent();
            ApplyHeader("wi.frontHead");
        }

        public HeadPage(string headTitleI18n)
        {
            InitializeComponent();
            ApplyHeader(headTitleI18n);
        }

        private void ApplyHeader(string headTitleI18n)
        {
            lblHeader.Tag = "i18n:" + headTitleI18n;
            lblHeader.Text = Lang.T(headTitleI18n);
        }
    }
}
