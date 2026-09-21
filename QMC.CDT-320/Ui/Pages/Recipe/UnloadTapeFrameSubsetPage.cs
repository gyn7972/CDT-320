using QMC.CDT_320.Ui.Localization;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class UnloadTapeFrameSubsetPage : SubsetPageBase
    {
        public UnloadTapeFrameSubsetPage() : base("recipe.unloadFrame")
        {
            InitializeComponent();
            InitializeRecipeLanguageBindings();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        protected override void LoadFromRecipe()
        {
            var u = _project.UnloadFrame ?? new UnloadTapeFrameSubset();
            _cbRole.SelectedItem = u.Role ?? "GoodUnload";
            if (_cbRole.SelectedIndex < 0) _cbRole.SelectedIndex = 1;
            _cbGapInsp.Checked = u.GapInspection;
            _nUpper.Value = (decimal)u.GapUpperLimit;
            _nLower.Value = (decimal)u.GapLowerLimit;
        }

        protected override void SaveToRecipe()
        {
            var u = _project.UnloadFrame ?? (_project.UnloadFrame = new UnloadTapeFrameSubset());
            u.Role = _cbRole.SelectedItem?.ToString() ?? "GoodUnload";
            u.GapInspection = _cbGapInsp.Checked;
            u.GapUpperLimit = (double)_nUpper.Value;
            u.GapLowerLimit = (double)_nLower.Value;
        }
        // Keep Designer declarations intact; language bindings only affect displayed captions.
        private void InitializeRecipeLanguageBindings()
        {
            Lang.BindChoices(_cbRole, FormatRecipeChoice);
            Lang.BindKey(this.grpUnload, "recipeUi.unloadTapeFrameSubsetPage.grpUnload.text");
            Lang.BindKey(this.lblRole, "recipeUi.loadTapeFrameSubsetPage.lblRole.text");
            Lang.BindKey(this.lblGapInspection, "recipeUi.unloadTapeFrameSubsetPage.lblGapInspection.text");
            Lang.BindKey(this._cbGapInsp, "recipeUi.unloadTapeFrameSubsetPage._cbGapInsp.text");
            Lang.BindKey(this.lblGapUpper, "recipeUi.unloadTapeFrameSubsetPage.lblGapUpper.text");
            Lang.BindKey(this.lblGapLower, "recipeUi.unloadTapeFrameSubsetPage.lblGapLower.text");
        }
        private static string FormatRecipeChoice(string value)
        {
            switch (value)
            {
                case "Load": return Lang.T("recipeUi.choice.load");
                case "GoodUnload": return Lang.T("recipeUi.choice.goodUnload");
                case "NgUnload": return Lang.T("recipeUi.choice.ngUnload");
                default: return value;
            }
        }

    }
}
