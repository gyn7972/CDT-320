using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class UnloadTapeFrameSubsetPage : SubsetPageBase
    {
        public UnloadTapeFrameSubsetPage() : base("recipe.unloadFrame")
        {
            InitializeComponent();
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
    }
}