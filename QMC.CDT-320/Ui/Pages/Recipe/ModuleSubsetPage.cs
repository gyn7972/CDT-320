using QMC.CDT_320.Ui.Localization;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class ModuleSubsetPage : SubsetPageBase
    {
        public ModuleSubsetPage() : base("recipe.moduleSubset")
        {
            InitializeComponent();
            InitializeRecipeLanguageBindings();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        protected override void LoadFromRecipe()
        {
            var m = _project.Module ?? new ModuleSubset();
            _nPickRetry.Value = m.PickRetryCount;
            _nPickDelay.Value = m.PickDelayMs;
            _nPlaceDelay.Value = m.PlaceDelayMs;
            _cbColletEnable.Checked = m.ColletCleanEnable;
            _nColletInterval.Value = m.ColletCleanInterval;
            _cbBottomInspect.Checked = m.BottomInspectionEnable;
            _cbPlacementInspect.Checked = m.PlacementInspectionEnable;
        }

        protected override void SaveToRecipe()
        {
            var m = _project.Module ?? (_project.Module = new ModuleSubset());
            m.PickRetryCount = (int)_nPickRetry.Value;
            m.PickDelayMs = (int)_nPickDelay.Value;
            m.PlaceDelayMs = (int)_nPlaceDelay.Value;
            m.ColletCleanEnable = _cbColletEnable.Checked;
            m.ColletCleanInterval = (int)_nColletInterval.Value;
            m.BottomInspectionEnable = _cbBottomInspect.Checked;
            m.PlacementInspectionEnable = _cbPlacementInspect.Checked;
        }
        // Keep Designer declarations intact; language bindings only affect displayed captions.
        private void InitializeRecipeLanguageBindings()
        {
            Lang.BindKey(this.grpPickPlace, "recipeUi.moduleSubsetPage.grpPickPlace.text");
            Lang.BindKey(this.lblPickRetry, "recipeUi.moduleSubsetPage.lblPickRetry.text");
            Lang.BindKey(this.lblPickDelay, "recipeUi.moduleSubsetPage.lblPickDelay.text");
            Lang.BindKey(this.lblPlaceDelay, "recipeUi.moduleSubsetPage.lblPlaceDelay.text");
            Lang.BindKey(this.grpCollet, "recipeUi.moduleSubsetPage.grpCollet.text");
            Lang.BindKey(this.lblColletEnable, "recipeUi.moduleSubsetPage.grpCollet.text");
            Lang.BindKey(this._cbColletEnable, "recipeUi.moduleSubsetPage._cbColletEnable.text");
            Lang.BindKey(this.lblColletInterval, "recipeUi.moduleSubsetPage.lblColletInterval.text");
            Lang.BindKey(this.grpInspection, "recipeUi.moduleSubsetPage.grpInspection.text");
            Lang.BindKey(this.lblBottomInspection, "recipeUi.moduleSubsetPage.lblBottomInspection.text");
            Lang.BindKey(this._cbBottomInspect, "recipeUi.moduleSubsetPage._cbBottomInspect.text");
            Lang.BindKey(this.lblPlacementInspection, "recipeUi.moduleSubsetPage.lblPlacementInspection.text");
            Lang.BindKey(this._cbPlacementInspect, "recipeUi.moduleSubsetPage._cbPlacementInspect.text");
        }
    }
}
