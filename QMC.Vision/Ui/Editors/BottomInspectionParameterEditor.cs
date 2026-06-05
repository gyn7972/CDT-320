using System.Windows.Forms;
using QMC.Vision.Core.Inspectors;

namespace QMC.Vision.Ui.Editors
{
    public class BottomInspectionParameterEditor : ParameterEditorBase
    {
        // 22 필드
        private TextBox _tbName;
        private ComboBox _cbChip;
        private NumericUpDown _nThr, _nWMin, _nWMax, _nHMin, _nHMax;
        private NumericUpDown _nC1, _nC2, _nTopHat, _nMinFor, _nLink;
        private NumericUpDown _nChipDepth, _nChipLen, _nForeign;
        private NumericUpDown _nFirstPeek, _nPeek, _nStdev, _nDefMin;
        private NumericUpDown _nChipLW, _nChipUW, _nChipLH, _nChipUH;
        private CheckBox _cbContam;
        private TextBox _tbSavePath;

        public BottomInspectionParameterEditor() : base("BottomInspection") { }

        protected override void BuildEditor(Panel c)
        {
            int yy = 8;
            c.Controls.Add(MakeLabel("Spec name",        10, yy));   _tbName = MakeText(240, yy, 280);  c.Controls.Add(_tbName);  yy += 30;
            c.Controls.Add(MakeLabel("Chip type",        10, yy));   _cbChip = MakeCombo(240, yy, new[] {"White","Black"}); c.Controls.Add(_cbChip); yy += 30;
            c.Controls.Add(MakeLabel("Threshold",        10, yy));   _nThr   = MakeNum(240, yy, 0, 255);                c.Controls.Add(_nThr);    yy += 30;
            c.Controls.Add(MakeLabel("Width min color gap",  10, yy)); _nWMin = MakeNum(240, yy, 0, 1000);           c.Controls.Add(_nWMin); yy += 30;
            c.Controls.Add(MakeLabel("Width max color gap",  10, yy)); _nWMax = MakeNum(240, yy, 0, 1000);           c.Controls.Add(_nWMax); yy += 30;
            c.Controls.Add(MakeLabel("Height min color gap", 10, yy)); _nHMin = MakeNum(240, yy, 0, 1000);           c.Controls.Add(_nHMin); yy += 30;
            c.Controls.Add(MakeLabel("Height max color gap", 10, yy)); _nHMax = MakeNum(240, yy, 0, 1000);           c.Controls.Add(_nHMax); yy += 30;
            c.Controls.Add(MakeLabel("Chipping1 max color gap", 10, yy)); _nC1 = MakeNum(240, yy, 0, 1000);          c.Controls.Add(_nC1); yy += 30;
            c.Controls.Add(MakeLabel("Chipping2 max color gap", 10, yy)); _nC2 = MakeNum(240, yy, 0, 1000);          c.Controls.Add(_nC2); yy += 30;
            c.Controls.Add(MakeLabel("Top hat radius",   10, yy));   _nTopHat = MakeNum(240, yy, 0, 100);            c.Controls.Add(_nTopHat); yy += 30;
            c.Controls.Add(MakeLabel("Min foreign filter", 10, yy)); _nMinFor = MakeNum(240, yy, 0, 1000);           c.Controls.Add(_nMinFor); yy += 30;
            c.Controls.Add(MakeLabel("Link distance",    10, yy));   _nLink = MakeNum(240, yy, 0, 100);              c.Controls.Add(_nLink); yy += 30;
            c.Controls.Add(MakeLabel("Chipping depth (mm)",  10, yy)); _nChipDepth = MakeNum(240, yy, 0, 5, 4);      c.Controls.Add(_nChipDepth); yy += 30;
            c.Controls.Add(MakeLabel("Chipping length (mm)", 10, yy)); _nChipLen   = MakeNum(240, yy, 0, 5, 4);      c.Controls.Add(_nChipLen);   yy += 30;
            c.Controls.Add(MakeLabel("Foreign size (mm)",10, yy));   _nForeign = MakeNum(240, yy, 0, 1, 5);          c.Controls.Add(_nForeign); yy += 30;
            c.Controls.Add(MakeLabel("First peek thr",   10, yy));   _nFirstPeek = MakeNum(240, yy, 0, 10, 4);       c.Controls.Add(_nFirstPeek); yy += 30;
            c.Controls.Add(MakeLabel("Peek thr",         10, yy));   _nPeek = MakeNum(240, yy, 0, 10, 4);            c.Controls.Add(_nPeek); yy += 30;
            c.Controls.Add(MakeLabel("Stdev",            10, yy));   _nStdev = MakeNum(240, yy, 0, 100, 4);          c.Controls.Add(_nStdev); yy += 30;
            c.Controls.Add(MakeLabel("Defect min size",  10, yy));   _nDefMin = MakeNum(240, yy, 0, 10, 5);          c.Controls.Add(_nDefMin); yy += 30;
            c.Controls.Add(MakeLabel("Chip W lower spec",10, yy));   _nChipLW = MakeNum(240, yy, -10m, 10m, 4);      c.Controls.Add(_nChipLW); yy += 30;
            c.Controls.Add(MakeLabel("Chip W upper spec",10, yy));   _nChipUW = MakeNum(240, yy, -10m, 10m, 4);      c.Controls.Add(_nChipUW); yy += 30;
            c.Controls.Add(MakeLabel("Chip H lower spec",10, yy));   _nChipLH = MakeNum(240, yy, -10m, 10m, 4);      c.Controls.Add(_nChipLH); yy += 30;
            c.Controls.Add(MakeLabel("Chip H upper spec",10, yy));   _nChipUH = MakeNum(240, yy, -10m, 10m, 4);      c.Controls.Add(_nChipUH); yy += 30;
            _cbContam = MakeCheck(10, yy, "Use contamination inspection", 400); c.Controls.Add(_cbContam); yy += 30;
            c.Controls.Add(MakeLabel("File save path",   10, yy));   _tbSavePath = MakeText(240, yy, 380); c.Controls.Add(_tbSavePath);
        }

        protected override void LoadFromParameters()
        {
            var p = BottomInspectionParameters.LoadJson(_jsonPath);
            _tbName.Text = p.Name; _cbChip.SelectedIndex = (int)p.ChipType;
            _nThr.Value = p.Threshold;
            _nWMin.Value = p.WidthMinimumColorGap; _nWMax.Value = p.WidthMaximumColorGap;
            _nHMin.Value = p.HeightMinimumColorGap; _nHMax.Value = p.HeightMaximumColorGap;
            _nC1.Value = p.ChippingSize1MaximumColorGap; _nC2.Value = p.ChippingSize2MaximumColorGap;
            _nTopHat.Value = p.TopHatRadius; _nMinFor.Value = p.MinForeignAreaFilterSize; _nLink.Value = p.LinkDistance;
            _nChipDepth.Value = (decimal)p.ChippingDepth; _nChipLen.Value = (decimal)p.ChippingLength; _nForeign.Value = (decimal)p.ForeignObjectSize;
            _nFirstPeek.Value = (decimal)p.FirstPeekValueThreshold; _nPeek.Value = (decimal)p.PeekValueThreshold;
            _nStdev.Value = (decimal)p.Stdev; _nDefMin.Value = (decimal)p.PortentialDefactMinSize;
            _nChipLW.Value = (decimal)p.ChipLowerSpecLimitWidth; _nChipUW.Value = (decimal)p.ChipUpperSpecLimitWidth;
            _nChipLH.Value = (decimal)p.ChipLowerSpecLimitHeight; _nChipUH.Value = (decimal)p.ChipUpperSpecLimitHeight;
            _cbContam.Checked = p.UseContaminationInspection; _tbSavePath.Text = p.FileSavePath ?? "";
        }

        protected override void SaveToParameters()
        {
            var p = new BottomInspectionParameters
            {
                Name = _tbName.Text, ChipType = (ChipType)_cbChip.SelectedIndex,
                Threshold = (int)_nThr.Value,
                WidthMinimumColorGap  = (int)_nWMin.Value, WidthMaximumColorGap  = (int)_nWMax.Value,
                HeightMinimumColorGap = (int)_nHMin.Value, HeightMaximumColorGap = (int)_nHMax.Value,
                ChippingSize1MaximumColorGap = (int)_nC1.Value, ChippingSize2MaximumColorGap = (int)_nC2.Value,
                TopHatRadius = (int)_nTopHat.Value, MinForeignAreaFilterSize = (int)_nMinFor.Value, LinkDistance = (int)_nLink.Value,
                ChippingDepth = (double)_nChipDepth.Value, ChippingLength = (double)_nChipLen.Value, ForeignObjectSize = (double)_nForeign.Value,
                FirstPeekValueThreshold = (double)_nFirstPeek.Value, PeekValueThreshold = (double)_nPeek.Value,
                Stdev = (double)_nStdev.Value, PortentialDefactMinSize = (double)_nDefMin.Value,
                ChipLowerSpecLimitWidth = (float)_nChipLW.Value, ChipUpperSpecLimitWidth = (float)_nChipUW.Value,
                ChipLowerSpecLimitHeight = (float)_nChipLH.Value, ChipUpperSpecLimitHeight = (float)_nChipUH.Value,
                UseContaminationInspection = _cbContam.Checked, FileSavePath = _tbSavePath.Text
            };
            p.SaveJson(_jsonPath);
        }
    }
}
