using System.Windows.Forms;
using QMC.Vision.Core.Inspectors;

namespace QMC.Vision.Ui.Editors
{
    public class SideInspectionParameterEditor : ParameterEditorBase
    {
        private TextBox _tbName;
        private ComboBox _cbChip, _cbSurface;
        private NumericUpDown _nThr, _nDepth, _nLen, _nForeign, _nBlade, _nThick;
        private NumericUpDown _nLW, _nUW, _nLH, _nUH;

        public SideInspectionParameterEditor() : base("SideInspection") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 8;
            c.Controls.Add(MakeLabel("Spec name",   10, yy));   _tbName = MakeText(240, yy, 280);  c.Controls.Add(_tbName);  yy += 30;
            c.Controls.Add(MakeLabel("Chip type",   10, yy));   _cbChip = MakeCombo(240, yy, new[] { "White","Black" }); c.Controls.Add(_cbChip); yy += 30;
            c.Controls.Add(MakeLabel("Surface",     10, yy));   _cbSurface = MakeCombo(240, yy, new[] { "FrontWidth","BackWidth","FrontHeight","BackHeight" }); c.Controls.Add(_cbSurface); yy += 30;
            c.Controls.Add(MakeLabel("Threshold",   10, yy));   _nThr = MakeNum(240, yy, 0, 255); c.Controls.Add(_nThr); yy += 30;
            c.Controls.Add(MakeLabel("Chipping depth (mm)",  10, yy)); _nDepth = MakeNum(240, yy, 0, 5, 4); c.Controls.Add(_nDepth);  yy += 30;
            c.Controls.Add(MakeLabel("Chipping length (mm)", 10, yy)); _nLen = MakeNum(240, yy, 0, 5, 4);  c.Controls.Add(_nLen);   yy += 30;
            c.Controls.Add(MakeLabel("Foreign size (mm)",10, yy));   _nForeign = MakeNum(240, yy, 0, 1, 5); c.Controls.Add(_nForeign); yy += 30;
            c.Controls.Add(MakeLabel("Blade width (mm)", 10, yy));   _nBlade = MakeNum(240, yy, 0, 5, 4);  c.Controls.Add(_nBlade);   yy += 30;
            c.Controls.Add(MakeLabel("Chip thickness (mm)", 10, yy)); _nThick = MakeNum(240, yy, 0, 5, 4); c.Controls.Add(_nThick);  yy += 30;
            c.Controls.Add(MakeLabel("Chip W lower",  10, yy)); _nLW = MakeNum(240, yy, -10m, 10m, 4); c.Controls.Add(_nLW); yy += 30;
            c.Controls.Add(MakeLabel("Chip W upper",  10, yy)); _nUW = MakeNum(240, yy, -10m, 10m, 4); c.Controls.Add(_nUW); yy += 30;
            c.Controls.Add(MakeLabel("Chip H lower",  10, yy)); _nLH = MakeNum(240, yy, -10m, 10m, 4); c.Controls.Add(_nLH); yy += 30;
            c.Controls.Add(MakeLabel("Chip H upper",  10, yy)); _nUH = MakeNum(240, yy, -10m, 10m, 4); c.Controls.Add(_nUH);
        }

        protected override void LoadFromParameters()
        {
            var p = SideInspectionParameters.LoadJson(_jsonPath);
            _tbName.Text = p.Name; _cbChip.SelectedIndex = (int)p.ChipType;
            _cbSurface.SelectedIndex = (int)p.Surface;
            _nThr.Value = p.Threshold;
            _nDepth.Value = (decimal)p.ChippingDepth; _nLen.Value = (decimal)p.ChippingLength;
            _nForeign.Value = (decimal)p.ForeignObjectSize; _nBlade.Value = (decimal)p.BladeWidth; _nThick.Value = (decimal)p.ChipThickness;
            _nLW.Value = (decimal)p.ChipLowerSpecLimitWidth; _nUW.Value = (decimal)p.ChipUpperSpecLimitWidth;
            _nLH.Value = (decimal)p.ChipLowerSpecLimitHeight; _nUH.Value = (decimal)p.ChipUpperSpecLimitHeight;
        }

        protected override void SaveToParameters()
        {
            var p = new SideInspectionParameters
            {
                Name = _tbName.Text, ChipType = (ChipType)_cbChip.SelectedIndex,
                Surface = (SideSurface)_cbSurface.SelectedIndex,
                Threshold = (int)_nThr.Value,
                ChippingDepth = (double)_nDepth.Value, ChippingLength = (double)_nLen.Value,
                ForeignObjectSize = (double)_nForeign.Value, BladeWidth = (double)_nBlade.Value, ChipThickness = (double)_nThick.Value,
                ChipLowerSpecLimitWidth = (float)_nLW.Value, ChipUpperSpecLimitWidth = (float)_nUW.Value,
                ChipLowerSpecLimitHeight = (float)_nLH.Value, ChipUpperSpecLimitHeight = (float)_nUH.Value,
            };
            p.SaveJson(_jsonPath);
        }
    }
}
