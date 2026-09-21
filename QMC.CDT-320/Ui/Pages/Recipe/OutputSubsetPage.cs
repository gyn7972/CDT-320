using QMC.CDT_320.Ui.Localization;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class OutputSubsetPage : SubsetPageBase
    {
        public OutputSubsetPage() : base("recipe.outputSubset")
        {
            InitializeComponent();
            InitializeRecipeLanguageBindings();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        protected override void LoadFromRecipe()
        {
            var o = _project.Output ?? new OutputSubset();
            _nGoodMax.Value = o.GoodPlateMaxSlots;
            _nNgMax.Value = o.NgPlateMaxSlots;
            _nDiesPerWafer.Value = o.DiesPerWafer;
            _nWafersPerBatch.Value = o.WafersPerOutputBatch;
            _cbAutoBin.Checked = o.AutoBinTransition;
            _cbAlarmFull.Checked = o.AlarmOnFull;
            _tbDefaultGood.Text = o.DefaultGoodCassette ?? "";
        }

        protected override void SaveToRecipe()
        {
            var o = _project.Output ?? (_project.Output = new OutputSubset());
            o.GoodPlateMaxSlots = (int)_nGoodMax.Value;
            o.NgPlateMaxSlots = (int)_nNgMax.Value;
            o.DiesPerWafer = (int)_nDiesPerWafer.Value;
            o.WafersPerOutputBatch = (int)_nWafersPerBatch.Value;
            o.AutoBinTransition = _cbAutoBin.Checked;
            o.AlarmOnFull = _cbAlarmFull.Checked;
            o.DefaultGoodCassette = _tbDefaultGood.Text;
        }
        // Keep Designer declarations intact; language bindings only affect displayed captions.
        private void InitializeRecipeLanguageBindings()
        {
            Lang.BindKey(this.grpOutput, "recipeUi.outputSubsetPage.grpOutput.text");
            Lang.BindKey(this.lblGoodMax, "recipeUi.outputSubsetPage.lblGoodMax.text");
            Lang.BindKey(this.lblNgMax, "recipeUi.outputSubsetPage.lblNgMax.text");
            Lang.BindKey(this.lblDiesPerWafer, "recipeUi.outputSubsetPage.lblDiesPerWafer.text");
            Lang.BindKey(this.lblWafersPerBatch, "recipeUi.outputSubsetPage.lblWafersPerBatch.text");
            Lang.BindKey(this.lblAutoBin, "recipeUi.outputSubsetPage.lblAutoBin.text");
            Lang.BindKey(this._cbAutoBin, "recipeUi.outputSubsetPage._cbAutoBin.text");
            Lang.BindKey(this.lblAlarmFull, "recipeUi.outputSubsetPage.lblAlarmFull.text");
            Lang.BindKey(this._cbAlarmFull, "recipeUi.outputSubsetPage._cbAlarmFull.text");
            Lang.BindKey(this.lblDefaultGood, "recipeUi.outputSubsetPage.lblDefaultGood.text");
        }
    }
}
