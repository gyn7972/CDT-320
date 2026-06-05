using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class LoadTapeFrameSubsetPage : SubsetPageBase
    {
        public LoadTapeFrameSubsetPage() : base("recipe.loadFrame")
        {
            InitializeComponent();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        protected override void LoadFromRecipe()
        {
            var l = _project.LoadFrame ?? new LoadTapeFrameSubset();
            _cbRole.SelectedItem = l.Role ?? "Load";
            if (_cbRole.SelectedIndex < 0) _cbRole.SelectedIndex = 0;
            _cbAutoBarcode.Checked = l.AutoBarcodeRead;
            _cbAutoAlign.Checked = l.AutoAlignment;
            _nAlignPts.Value = l.AlignmentPoints;
        }

        protected override void SaveToRecipe()
        {
            var l = _project.LoadFrame ?? (_project.LoadFrame = new LoadTapeFrameSubset());
            l.Role = _cbRole.SelectedItem?.ToString() ?? "Load";
            l.AutoBarcodeRead = _cbAutoBarcode.Checked;
            l.AutoAlignment = _cbAutoAlign.Checked;
            l.AlignmentPoints = (int)_nAlignPts.Value;
        }
    }
}