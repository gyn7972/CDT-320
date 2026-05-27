using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>언로드 웨이퍼 (310 의 UnloadDieTapeFrameSubsetRecipe).</summary>
    public class UnloadTapeFrameSubsetPage : SubsetPageBase
    {
        private ComboBox _cbRole;
        private CheckBox _cbGapInsp;
        private NumericUpDown _nUpper, _nLower;

        public UnloadTapeFrameSubsetPage() : base("recipe.unloadFrame") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 10, dy = 36;
            c.Controls.Add(MakeLabel("Role",            10, yy));     _cbRole = MakeCombo(220, yy, new[] { "Load", "GoodUnload", "NgUnload" }); c.Controls.Add(_cbRole);  yy += dy + 8;
            _cbGapInsp = MakeCheck(10, yy, "Gap inspection enabled (after place)", 450); c.Controls.Add(_cbGapInsp); yy += dy;
            c.Controls.Add(MakeLabel("Gap upper limit (mm)",10, yy));   _nUpper = MakeNum(220, yy, 0m, 5m, 4); c.Controls.Add(_nUpper); yy += dy;
            c.Controls.Add(MakeLabel("Gap lower limit (mm)",10, yy));   _nLower = MakeNum(220, yy, 0m, 5m, 4); c.Controls.Add(_nLower);
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
            u.Role          = _cbRole.SelectedItem?.ToString() ?? "GoodUnload";
            u.GapInspection = _cbGapInsp.Checked;
            u.GapUpperLimit = (double)_nUpper.Value;
            u.GapLowerLimit = (double)_nLower.Value;
        }
    }
}
