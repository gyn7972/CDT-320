using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class ModuleSubsetPage : SubsetPageBase
    {
        public ModuleSubsetPage() : base("recipe.moduleSubset")
        {
            InitializeComponent();
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
    }
}