using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class TapeFrameSubsetPage : SubsetPageBase
    {
        public TapeFrameSubsetPage() : base("recipe.tapeFrameSubset")
        {
            InitializeComponent();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        protected override void LoadFromRecipe()
        {
            var f = _project.Frame ?? new TapeFrameSubset();
            _tbName.Text = f.FrameSpecName ?? "";
            _nGridX.Value = f.GridX;
            _nGridY.Value = f.GridY;
            _nPitchX.Value = (decimal)f.PitchX;
            _nPitchY.Value = (decimal)f.PitchY;
            _nDiameter.Value = (decimal)f.OuterDiameterMm;
            _cbRotate.SelectedItem = f.Rotate ?? "None";
            if (_cbRotate.SelectedIndex < 0) _cbRotate.SelectedIndex = 0;
        }

        protected override void SaveToRecipe()
        {
            var f = _project.Frame ?? (_project.Frame = new TapeFrameSubset());
            f.FrameSpecName = _tbName.Text;
            f.GridX = (int)_nGridX.Value;
            f.GridY = (int)_nGridY.Value;
            f.PitchX = (double)_nPitchX.Value;
            f.PitchY = (double)_nPitchY.Value;
            f.OuterDiameterMm = (double)_nDiameter.Value;
            f.Rotate = _cbRotate.SelectedItem?.ToString() ?? "None";
        }
    }
}