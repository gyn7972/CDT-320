using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>웨이퍼(테이프 프레임) 사양 Subset (310 의 DieTapeFrameSubsetRecipe).</summary>
    public class TapeFrameSubsetPage : SubsetPageBase
    {
        private TextBox       _tbName;
        private NumericUpDown _nGridX, _nGridY, _nPitchX, _nPitchY, _nDiameter;
        private ComboBox      _cbRotate;

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // _lblProject
            // 
            this._lblProject.Size = new System.Drawing.Size(0, 36);
            // 
            // TapeFrameSubsetPage
            // 
            this.Name = "TapeFrameSubsetPage";
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this.ResumeLayout(false);

        }

        public TapeFrameSubsetPage() : base("recipe.tapeFrameSubset") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 10, dy = 36;
            c.Controls.Add(MakeLabel("Spec name",        10, yy));  _tbName     = MakeText(220, yy, 280);                              c.Controls.Add(_tbName);     yy += dy;
            c.Controls.Add(MakeLabel("Grid X (count)",   10, yy));  _nGridX     = MakeNum (220, yy, 1m, 200m);                          c.Controls.Add(_nGridX);     yy += dy;
            c.Controls.Add(MakeLabel("Grid Y (count)",   10, yy));  _nGridY     = MakeNum (220, yy, 1m, 200m);                          c.Controls.Add(_nGridY);     yy += dy;
            c.Controls.Add(MakeLabel("Pitch X (mm)",     10, yy));  _nPitchX    = MakeNum (220, yy, 0.001m, 100m, 3);                   c.Controls.Add(_nPitchX);    yy += dy;
            c.Controls.Add(MakeLabel("Pitch Y (mm)",     10, yy));  _nPitchY    = MakeNum (220, yy, 0.001m, 100m, 3);                   c.Controls.Add(_nPitchY);    yy += dy;
            c.Controls.Add(MakeLabel("Outer diameter (mm)", 10, yy));_nDiameter = MakeNum (220, yy, 50m, 500m, 1);                       c.Controls.Add(_nDiameter);  yy += dy;
            c.Controls.Add(MakeLabel("Rotate",           10, yy));  _cbRotate   = MakeCombo(220, yy, new[] { "None","R90","R180","R270" }, 160); c.Controls.Add(_cbRotate);
        }

        protected override void LoadFromRecipe()
        {
            var f = _project.Frame ?? new TapeFrameSubset();
            _tbName.Text         = f.FrameSpecName ?? "";
            _nGridX.Value        = f.GridX;
            _nGridY.Value        = f.GridY;
            _nPitchX.Value       = (decimal)f.PitchX;
            _nPitchY.Value       = (decimal)f.PitchY;
            _nDiameter.Value     = (decimal)f.OuterDiameterMm;
            _cbRotate.SelectedItem = f.Rotate ?? "None";
            if (_cbRotate.SelectedIndex < 0) _cbRotate.SelectedIndex = 0;
        }

        protected override void SaveToRecipe()
        {
            var f = _project.Frame ?? (_project.Frame = new TapeFrameSubset());
            f.FrameSpecName    = _tbName.Text;
            f.GridX            = (int)_nGridX.Value;
            f.GridY            = (int)_nGridY.Value;
            f.PitchX           = (double)_nPitchX.Value;
            f.PitchY           = (double)_nPitchY.Value;
            f.OuterDiameterMm  = (double)_nDiameter.Value;
            f.Rotate           = _cbRotate.SelectedItem?.ToString() ?? "None";
        }
    }
}
