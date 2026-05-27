using System.Windows.Forms;
using QMC.Vision.Core.Inspectors;

namespace QMC.Vision.Ui.Editors
{
    public class DistortionParameterEditor : ParameterEditorBase
    {
        private TextBox _tbName;
        private ComboBox _cbChip, _cbTarget;
        private NumericUpDown _nThr, _nPx, _nPy;

        public DistortionParameterEditor() : base("Distortion") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 8;
            c.Controls.Add(MakeLabel("Spec name",      10, yy));    _tbName = MakeText(240, yy, 280);                            c.Controls.Add(_tbName);  yy += 30;
            c.Controls.Add(MakeLabel("Chip type",      10, yy));    _cbChip = MakeCombo(240, yy, new[] { "White","Black" });     c.Controls.Add(_cbChip); yy += 30;
            c.Controls.Add(MakeLabel("Target search",  10, yy));    _cbTarget = MakeCombo(240, yy, new[] { "CrossLine","Circle" }); c.Controls.Add(_cbTarget); yy += 30;
            c.Controls.Add(MakeLabel("Threshold",      10, yy));    _nThr = MakeNum(240, yy, 0, 255);                            c.Controls.Add(_nThr); yy += 30;
            c.Controls.Add(MakeLabel("Pitch X (mm)",   10, yy));    _nPx = MakeNum(240, yy, 0.001m, 100m, 4);                    c.Controls.Add(_nPx); yy += 30;
            c.Controls.Add(MakeLabel("Pitch Y (mm)",   10, yy));    _nPy = MakeNum(240, yy, 0.001m, 100m, 4);                    c.Controls.Add(_nPy);
        }

        protected override void LoadFromParameters()
        {
            var p = DistortionParameters.LoadJson(_jsonPath);
            _tbName.Text = p.Name;
            _cbChip.SelectedIndex = (int)p.ChipType;
            _cbTarget.SelectedIndex = (int)p.TargetSearch;
            _nThr.Value = p.Threshold;
            _nPx.Value  = (decimal)p.PitchX;
            _nPy.Value  = (decimal)p.PitchY;
        }

        protected override void SaveToParameters()
        {
            var p = new DistortionParameters
            {
                Name = _tbName.Text,
                ChipType = (ChipType)_cbChip.SelectedIndex,
                TargetSearch = (DistortionTargetSearch)_cbTarget.SelectedIndex,
                Threshold = (int)_nThr.Value,
                PitchX = (double)_nPx.Value,
                PitchY = (double)_nPy.Value
            };
            p.SaveJson(_jsonPath);
        }
    }
}
