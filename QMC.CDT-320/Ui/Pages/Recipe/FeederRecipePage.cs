using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class FeederRecipePage : PageBase
    {
        private readonly string _titleI18n;

        public FeederRecipePage() : this("recipe.inputFeeder")
        {
        }

        public FeederRecipePage(string titleI18n)
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
