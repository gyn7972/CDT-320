using QMC.CDT_320.Ui.Localization;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class LoadTapeFrameSubsetPage : SubsetPageBase
    {
        public LoadTapeFrameSubsetPage() : base("recipe.loadFrame")
        {
            InitializeComponent();
            InitializeRecipeLanguageBindings();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        protected override void LoadFromRecipe()
        {
            var l = _project.LoadFrame ?? new LoadTapeFrameSubset();
            _cbRole.SelectedItem = l.Role ?? "Load";
            if (_cbRole.SelectedIndex < 0) _cbRole.SelectedIndex = 0;
            // 바코드 사용 여부는 레시피에서 관리하지 않는다(설정 → 바코드 화면 단일 소스).
            _cbAutoAlign.Checked = l.AutoAlignment;
            _nAlignPts.Value = l.AlignmentPoints;
        }

        protected override void SaveToRecipe()
        {
            var l = _project.LoadFrame ?? (_project.LoadFrame = new LoadTapeFrameSubset());
            l.Role = _cbRole.SelectedItem?.ToString() ?? "Load";
            l.AutoAlignment = _cbAutoAlign.Checked;
            l.AlignmentPoints = (int)_nAlignPts.Value;
        }
        // Keep Designer declarations intact; language bindings only affect displayed captions.
        private void InitializeRecipeLanguageBindings()
        {
            Lang.BindChoices(_cbRole, FormatRecipeChoice);
            Lang.BindKey(this.grpLoad, "recipeUi.loadTapeFrameSubsetPage.grpLoad.text");
            Lang.BindKey(this.lblRole, "recipeUi.loadTapeFrameSubsetPage.lblRole.text");
            Lang.BindKey(this.lblAlignPts, "recipeUi.loadTapeFrameSubsetPage.lblAlignPts.text");
            Lang.BindKey(this.lblAutoBarcode, "recipeUi.loadTapeFrameSubsetPage.lblAutoBarcode.text");
            Lang.BindKey(this._cbAutoBarcode, "recipeUi.loadTapeFrameSubsetPage._cbAutoBarcode.text");
            Lang.BindKey(this.lblAutoAlign, "recipeUi.loadTapeFrameSubsetPage.lblAutoAlign.text");
            Lang.BindKey(this._cbAutoAlign, "recipeUi.loadTapeFrameSubsetPage._cbAutoAlign.text");
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
