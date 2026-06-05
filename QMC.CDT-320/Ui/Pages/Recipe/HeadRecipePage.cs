using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class HeadRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private string _titleI18n;

        public HeadRecipePage()
        {
            _titleI18n = "recipe.frontHead";
            InitializeComponent();
            ApplyTitle();
        }

        public HeadRecipePage(string titleI18n)
        {
            _titleI18n = titleI18n;
            InitializeComponent();
            ApplyTitle();
        }

        private void ApplyTitle()
        {
            lblHeader.Tag = "i18n:" + _titleI18n;
            lblHeader.Text = Lang.T(_titleI18n);
        }
    }
}
