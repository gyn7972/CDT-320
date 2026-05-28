using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages
{
    public partial class PlaceholderPage : PageBase
    {
        private string _i18nKey;

        public PlaceholderPage() : this("common.caption")
        {
        }

        public PlaceholderPage(string i18nKey)
        {
            _i18nKey = i18nKey;
            InitializeComponent();
            ApplyCaption();
        }

        private void ApplyCaption()
        {
            lblHeader.Tag = "i18n:" + _i18nKey;
            lblHeader.Text = Lang.T(_i18nKey);
            lblPlaceholder.Tag = "i18n:" + _i18nKey;
            lblPlaceholder.Text = Lang.T(_i18nKey) + "   (placeholder)";
        }
    }
}
