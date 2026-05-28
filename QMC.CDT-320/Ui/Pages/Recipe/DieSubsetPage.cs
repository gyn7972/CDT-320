using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Die 사양 Subset 편집 (310 의 DieSubsetRecipe 동등 — 우리 RecipeProject.Die).</summary>
    public class DieSubsetPage : SubsetPageBase
    {
        private TextBox        _tbName;
        private NumericUpDown  _nW, _nH, _nT;
        private NumericUpDown  _nWLow, _nWUp, _nHLow, _nHUp;

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // _lblProject
            // 
            this._lblProject.Size = new System.Drawing.Size(0, 36);
            // 
            // DieSubsetPage
            // 
            this.Name = "DieSubsetPage";
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this.ResumeLayout(false);

        }

        private NumericUpDown  _nChipDepth, _nChipLen, _nForeign;

        public DieSubsetPage() : base("recipe.dieSubset") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 10, dy = 36;
            c.Controls.Add(MakeLabel("Spec name",   10, yy));         _tbName     = MakeText(220, yy, 280);                            c.Controls.Add(_tbName);     yy += dy;
            c.Controls.Add(MakeLabel("Width (mm)",  10, yy));         _nW         = MakeNum(220, yy, 0.001m, 50m, 4);                  c.Controls.Add(_nW);         yy += dy;
            c.Controls.Add(MakeLabel("Height (mm)", 10, yy));         _nH         = MakeNum(220, yy, 0.001m, 50m, 4);                  c.Controls.Add(_nH);         yy += dy;
            c.Controls.Add(MakeLabel("Thickness (mm)", 10, yy));      _nT         = MakeNum(220, yy, 0.001m, 5m, 4);                   c.Controls.Add(_nT);         yy += dy + 8;

            c.Controls.Add(new Label
            {
                Location = new Point(10, yy), Size = new Size(490, 24),
                Text = "Tolerances (mm)",
                Font = UiTheme.SectionFont, ForeColor = Color.DarkSlateGray,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightYellow,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            });
            yy += 30;
            c.Controls.Add(MakeLabel("Width — lower",  10, yy));   _nWLow = MakeNum(220, yy, -10m, 0m, 4);   c.Controls.Add(_nWLow);  yy += dy;
            c.Controls.Add(MakeLabel("Width — upper",  10, yy));   _nWUp  = MakeNum(220, yy, 0m, 10m, 4);    c.Controls.Add(_nWUp);   yy += dy;
            c.Controls.Add(MakeLabel("Height — lower", 10, yy));   _nHLow = MakeNum(220, yy, -10m, 0m, 4);   c.Controls.Add(_nHLow);  yy += dy;
            c.Controls.Add(MakeLabel("Height — upper", 10, yy));   _nHUp  = MakeNum(220, yy, 0m, 10m, 4);    c.Controls.Add(_nHUp);   yy += dy + 8;

            c.Controls.Add(new Label
            {
                Location = new Point(10, yy), Size = new Size(490, 24),
                Text = "Vision inspection thresholds (mm)",
                Font = UiTheme.SectionFont, ForeColor = Color.DarkSlateGray,
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.LightYellow,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0)
            });
            yy += 30;
            c.Controls.Add(MakeLabel("Chipping depth max", 10, yy));    _nChipDepth = MakeNum(220, yy, 0m, 5m, 4);   c.Controls.Add(_nChipDepth); yy += dy;
            c.Controls.Add(MakeLabel("Chipping length max",10, yy));    _nChipLen   = MakeNum(220, yy, 0m, 5m, 4);   c.Controls.Add(_nChipLen);   yy += dy;
            c.Controls.Add(MakeLabel("Foreign size max",   10, yy));    _nForeign   = MakeNum(220, yy, 0m, 1m, 5);   c.Controls.Add(_nForeign);
        }

        protected override void LoadFromRecipe()
        {
            var d = _project.Die ?? new DieSubset();
            _tbName.Text = d.DieSpecName ?? "";
            _nW.Value = (decimal)d.WidthMm;
            _nH.Value = (decimal)d.HeightMm;
            _nT.Value = (decimal)d.ThicknessMm;
            _nWLow.Value = (decimal)d.ChipLowerSpecLimitWidth;
            _nWUp .Value = (decimal)d.ChipUpperSpecLimitWidth;
            _nHLow.Value = (decimal)d.ChipLowerSpecLimitHeight;
            _nHUp .Value = (decimal)d.ChipUpperSpecLimitHeight;
            _nChipDepth.Value = (decimal)d.ChippingDepthMax;
            _nChipLen  .Value = (decimal)d.ChippingLengthMax;
            _nForeign  .Value = (decimal)d.ForeignSizeMax;
        }

        protected override void SaveToRecipe()
        {
            var d = _project.Die ?? (_project.Die = new DieSubset());
            d.DieSpecName              = _tbName.Text;
            d.WidthMm                  = (double)_nW.Value;
            d.HeightMm                 = (double)_nH.Value;
            d.ThicknessMm              = (double)_nT.Value;
            d.ChipLowerSpecLimitWidth  = (double)_nWLow.Value;
            d.ChipUpperSpecLimitWidth  = (double)_nWUp.Value;
            d.ChipLowerSpecLimitHeight = (double)_nHLow.Value;
            d.ChipUpperSpecLimitHeight = (double)_nHUp.Value;
            d.ChippingDepthMax         = (double)_nChipDepth.Value;
            d.ChippingLengthMax        = (double)_nChipLen.Value;
            d.ForeignSizeMax           = (double)_nForeign.Value;
        }
    }
}
