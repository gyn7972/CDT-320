using System;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    /// <summary>Die subset editor. Values are stored in RecipeProject.Die and mirrored to MaterialSpecs.</summary>
    public partial class DieSubsetPage : SubsetPageBase
    {
        public DieSubsetPage() : base("recipe.dieSubset")
        {
            InitializeComponent();
        }

        protected override void BuildEditor(Panel c)
        {
        }

        protected override void LoadFromRecipe()
        {
            var d = _project.Die ?? new DieSubset();
            RefreshSpecList(d.DieSpecName);
            _tbName.Text = d.DieSpecName ?? "";
            _nW.Value = ClampDecimal((decimal)d.WidthMm, _nW.Minimum, _nW.Maximum);
            _nH.Value = ClampDecimal((decimal)d.HeightMm, _nH.Minimum, _nH.Maximum);
            _nT.Value = ClampDecimal((decimal)d.ThicknessMm, _nT.Minimum, _nT.Maximum);
            _nWLow.Value = ClampDecimal((decimal)d.ChipLowerSpecLimitWidth, _nWLow.Minimum, _nWLow.Maximum);
            _nWUp.Value = ClampDecimal((decimal)d.ChipUpperSpecLimitWidth, _nWUp.Minimum, _nWUp.Maximum);
            _nHLow.Value = ClampDecimal((decimal)d.ChipLowerSpecLimitHeight, _nHLow.Minimum, _nHLow.Maximum);
            _nHUp.Value = ClampDecimal((decimal)d.ChipUpperSpecLimitHeight, _nHUp.Minimum, _nHUp.Maximum);
            _nChipDepth.Value = ClampDecimal((decimal)d.ChippingDepthMax, _nChipDepth.Minimum, _nChipDepth.Maximum);
            _nChipLen.Value = ClampDecimal((decimal)d.ChippingLengthMax, _nChipLen.Minimum, _nChipLen.Maximum);
            _nForeign.Value = ClampDecimal((decimal)d.ForeignSizeMax, _nForeign.Minimum, _nForeign.Maximum);
        }

        protected override void SaveToRecipe()
        {
            var d = _project.Die ?? (_project.Die = new DieSubset());
            ApplyControlsToDie(d);
            MaterialStateService.SyncRecipeDieSpec(_project);
            MaterialStateService.SyncRecipeTapeFrameSpec(_project);
            RefreshSpecList(d.DieSpecName);
        }

        private void btnLoadSpec_Click(object sender, EventArgs e)
        {
            try
            {
                string specName = _cbSpecLibrary.SelectedItem != null ? _cbSpecLibrary.SelectedItem.ToString() : "";
                var spec = MaterialSpecs.FindDie(specName);
                if (spec == null)
                {
                    MessageBox.Show("선택된 Die Spec을 찾을 수 없습니다.", "Die Spec", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ApplySpecToControls(spec);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Die Spec 불러오기 실패: " + ex.Message, "Die Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnSaveSpec_Click(object sender, EventArgs e)
        {
            try
            {
                var d = _project.Die ?? (_project.Die = new DieSubset());
                ApplyControlsToDie(d);
                MaterialStateService.SyncRecipeDieSpec(_project);
                MaterialStateService.SyncRecipeTapeFrameSpec(_project);
                RecipeStore.Save(_project);
                RecipeStore.SaveLastProjectName(_project.FileName);
                var host = FindForm() as Form1;
                if (host != null)
                    host.SaveMachineRecipe(_project.FileName);
                RefreshSpecList(d.DieSpecName);
                MessageBox.Show("Die Spec 저장 및 Recipe 연동 완료.", "Die Spec", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Die Spec 저장 실패: " + ex.Message, "Die Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void RefreshSpecList(string selectedName)
        {
            try
            {
                string selected = selectedName ?? "";
                _cbSpecLibrary.Items.Clear();
                if (MaterialSpecs.Data != null && MaterialSpecs.Data.Dies != null)
                {
                    foreach (var spec in MaterialSpecs.Data.Dies)
                    {
                        if (spec != null && !string.IsNullOrWhiteSpace(spec.Name))
                            _cbSpecLibrary.Items.Add(spec.Name);
                    }
                }

                if (!string.IsNullOrWhiteSpace(selected) && !_cbSpecLibrary.Items.Contains(selected))
                    _cbSpecLibrary.Items.Add(selected);

                if (!string.IsNullOrWhiteSpace(selected))
                    _cbSpecLibrary.SelectedItem = selected;
                if (_cbSpecLibrary.SelectedIndex < 0 && _cbSpecLibrary.Items.Count > 0)
                    _cbSpecLibrary.SelectedIndex = 0;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplySpecToControls(DieSpec spec)
        {
            if (spec == null)
                return;

            _tbName.Text = spec.Name ?? "";
            _nW.Value = ClampDecimal((decimal)spec.WidthMm, _nW.Minimum, _nW.Maximum);
            _nH.Value = ClampDecimal((decimal)spec.HeightMm, _nH.Minimum, _nH.Maximum);
            _nT.Value = ClampDecimal((decimal)spec.ThicknessMm, _nT.Minimum, _nT.Maximum);
            _nWLow.Value = ClampDecimal((decimal)spec.WidthLower, _nWLow.Minimum, _nWLow.Maximum);
            _nWUp.Value = ClampDecimal((decimal)spec.WidthUpper, _nWUp.Minimum, _nWUp.Maximum);
            _nHLow.Value = ClampDecimal((decimal)spec.HeightLower, _nHLow.Minimum, _nHLow.Maximum);
            _nHUp.Value = ClampDecimal((decimal)spec.HeightUpper, _nHUp.Minimum, _nHUp.Maximum);
            _nChipDepth.Value = ClampDecimal((decimal)spec.ChippingDepthMax, _nChipDepth.Minimum, _nChipDepth.Maximum);
            _nChipLen.Value = ClampDecimal((decimal)spec.ChippingLengthMax, _nChipLen.Minimum, _nChipLen.Maximum);
            _nForeign.Value = ClampDecimal((decimal)spec.ForeignSizeMax, _nForeign.Minimum, _nForeign.Maximum);
        }

        private void ApplyControlsToDie(DieSubset d)
        {
            if (d == null)
                return;

            d.DieSpecName = string.IsNullOrWhiteSpace(_tbName.Text) ? "Default" : _tbName.Text.Trim();
            d.WidthMm = (double)_nW.Value;
            d.HeightMm = (double)_nH.Value;
            d.ThicknessMm = (double)_nT.Value;
            d.ChipLowerSpecLimitWidth = (double)_nWLow.Value;
            d.ChipUpperSpecLimitWidth = (double)_nWUp.Value;
            d.ChipLowerSpecLimitHeight = (double)_nHLow.Value;
            d.ChipUpperSpecLimitHeight = (double)_nHUp.Value;
            d.ChippingDepthMax = (double)_nChipDepth.Value;
            d.ChippingLengthMax = (double)_nChipLen.Value;
            d.ForeignSizeMax = (double)_nForeign.Value;
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
