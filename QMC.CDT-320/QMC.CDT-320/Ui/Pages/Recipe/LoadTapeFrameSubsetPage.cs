using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>로드 웨이퍼 (310 의 LoadDieTapeFrameSubsetRecipe).</summary>
    public class LoadTapeFrameSubsetPage : SubsetPageBase
    {
        private ComboBox _cbRole;
        private CheckBox _cbAutoBarcode, _cbAutoAlign;
        private NumericUpDown _nAlignPts;

        public LoadTapeFrameSubsetPage() : base("recipe.loadFrame") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 10, dy = 36;
            c.Controls.Add(MakeLabel("Role",            10, yy));     _cbRole = MakeCombo(220, yy, new[] { "Load", "GoodUnload", "NgUnload" }); c.Controls.Add(_cbRole); yy += dy;
            c.Controls.Add(MakeLabel("Alignment points",10, yy));     _nAlignPts = MakeNum(220, yy, 0m, 10m); c.Controls.Add(_nAlignPts);   yy += dy + 8;

            _cbAutoBarcode = MakeCheck(10, yy, "Auto barcode read on load",  450); c.Controls.Add(_cbAutoBarcode); yy += dy;
            _cbAutoAlign   = MakeCheck(10, yy, "Auto alignment on load",     450); c.Controls.Add(_cbAutoAlign);
        }

        protected override void LoadFromRecipe()
        {
            var l = _project.LoadFrame ?? new LoadTapeFrameSubset();
            _cbRole.SelectedItem = l.Role ?? "Load";
            if (_cbRole.SelectedIndex < 0) _cbRole.SelectedIndex = 0;
            _cbAutoBarcode.Checked = l.AutoBarcodeRead;
            _cbAutoAlign  .Checked = l.AutoAlignment;
            _nAlignPts.Value = l.AlignmentPoints;
        }

        protected override void SaveToRecipe()
        {
            var l = _project.LoadFrame ?? (_project.LoadFrame = new LoadTapeFrameSubset());
            l.Role             = _cbRole.SelectedItem?.ToString() ?? "Load";
            l.AutoBarcodeRead  = _cbAutoBarcode.Checked;
            l.AutoAlignment    = _cbAutoAlign  .Checked;
            l.AlignmentPoints  = (int)_nAlignPts.Value;
        }
    }
}
