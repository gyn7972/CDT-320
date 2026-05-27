using System.Windows.Forms;
using QMC.Vision.Core.Inspectors;

namespace QMC.Vision.Ui.Editors
{
    public class DieGapInspectionParameterEditor : ParameterEditorBase
    {
        private TextBox _tbName;
        private NumericUpDown _nThr, _nUpper, _nLower;

        public DieGapInspectionParameterEditor() : base("DieGapInspection") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 8;
            c.Controls.Add(MakeLabel("Spec name",       10, yy));    _tbName = MakeText(240, yy, 280);          c.Controls.Add(_tbName);   yy += 30;
            c.Controls.Add(MakeLabel("Threshold",       10, yy));    _nThr   = MakeNum(240, yy, 0, 255);        c.Controls.Add(_nThr);     yy += 30;
            c.Controls.Add(MakeLabel("Gap upper limit (mm)",  10, yy)); _nUpper = MakeNum(240, yy, 0, 5, 4);    c.Controls.Add(_nUpper);   yy += 30;
            c.Controls.Add(MakeLabel("Gap lower limit (mm)",  10, yy)); _nLower = MakeNum(240, yy, 0, 5, 4);    c.Controls.Add(_nLower);
        }

        protected override void LoadFromParameters()
        {
            var p = DieGapInspectionParameters.LoadJson(_jsonPath);
            _tbName.Text = p.Name;
            _nThr.Value = p.Threshold;
            _nUpper.Value = (decimal)p.UpperLimit;
            _nLower.Value = (decimal)p.LowerLimit;
        }

        protected override void SaveToParameters()
        {
            var p = new DieGapInspectionParameters
            {
                Name = _tbName.Text,
                Threshold = (int)_nThr.Value,
                UpperLimit = (double)_nUpper.Value,
                LowerLimit = (double)_nLower.Value
            };
            p.SaveJson(_jsonPath);
        }
    }
}
