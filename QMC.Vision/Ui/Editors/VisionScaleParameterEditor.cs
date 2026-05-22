using System.Windows.Forms;
using QMC.Vision.Core.Inspectors;

namespace QMC.Vision.Ui.Editors
{
    public class VisionScaleParameterEditor : ParameterEditorBase
    {
        private TextBox _tbName;
        private ComboBox _cbChip;
        private NumericUpDown _nThr, _nW, _nH;

        public VisionScaleParameterEditor() : base("VisionScale") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 8;
            c.Controls.Add(MakeLabel("Spec name",         10, yy));   _tbName = MakeText(240, yy, 280);                          c.Controls.Add(_tbName); yy += 30;
            c.Controls.Add(MakeLabel("Chip type",         10, yy));   _cbChip = MakeCombo(240, yy, new[] { "White","Black" });   c.Controls.Add(_cbChip); yy += 30;
            c.Controls.Add(MakeLabel("Threshold",         10, yy));   _nThr = MakeNum(240, yy, 0, 255);                          c.Controls.Add(_nThr); yy += 30;
            c.Controls.Add(MakeLabel("Chip width (mm)",   10, yy));   _nW = MakeNum(240, yy, 0.001m, 100m, 4);                   c.Controls.Add(_nW); yy += 30;
            c.Controls.Add(MakeLabel("Chip height (mm)",  10, yy));   _nH = MakeNum(240, yy, 0.001m, 100m, 4);                   c.Controls.Add(_nH);
        }

        protected override void LoadFromParameters()
        {
            var p = VisionScaleParameters.LoadJson(_jsonPath);
            _tbName.Text = p.Name;
            _cbChip.SelectedIndex = (int)p.ChipType;
            _nThr.Value = p.Threshold;
            _nW.Value = (decimal)p.ChipWidth;
            _nH.Value = (decimal)p.ChipHeight;
        }

        protected override void SaveToParameters()
        {
            var p = new VisionScaleParameters
            {
                Name = _tbName.Text,
                ChipType = (ChipType)_cbChip.SelectedIndex,
                Threshold = (int)_nThr.Value,
                ChipWidth = (double)_nW.Value,
                ChipHeight = (double)_nH.Value
            };
            p.SaveJson(_jsonPath);
        }
    }
}
