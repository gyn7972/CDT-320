using System;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT320.Recipes;
using QMC.CDT_320.Ui;

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
            RefreshSpecList(f.FrameSpecName);
            _tbName.Text = f.FrameSpecName ?? "";
            _nGridX.Value = ClampDecimal(f.GridX, _nGridX.Minimum, _nGridX.Maximum);
            _nGridY.Value = ClampDecimal(f.GridY, _nGridY.Minimum, _nGridY.Maximum);
            _nPitchX.Value = ClampDecimal((decimal)f.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
            _nPitchY.Value = ClampDecimal((decimal)f.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
            _nDiameter.Value = ClampDecimal((decimal)f.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
            _cbRotate.SelectedItem = f.Rotate ?? "None";
            if (_cbRotate.SelectedIndex < 0) _cbRotate.SelectedIndex = 0;
        }

        protected override void SaveToRecipe()
        {
            var f = _project.Frame ?? (_project.Frame = new TapeFrameSubset());
            ApplyControlsToFrame(f);
            MaterialStateService.SyncRecipeTapeFrameSpec(_project);
            RefreshSpecList(f.FrameSpecName);
        }

        private void btnLoadSpec_Click(object sender, EventArgs e)
        {
            try
            {
                string specName = _cbSpecLibrary.SelectedItem != null ? _cbSpecLibrary.SelectedItem.ToString() : "";
                var spec = MaterialSpecs.FindFrame(specName);
                if (spec == null)
                {
                    MessageBox.Show("선택된 TapeFrame Spec을 찾을 수 없습니다.", "TapeFrame Spec", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ApplySpecToControls(spec);
            }
            catch (Exception ex)
            {
                MessageBox.Show("TapeFrame Spec 불러오기 실패: " + ex.Message, "TapeFrame Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void btnSaveSpec_Click(object sender, EventArgs e)
        {
            try
            {
                var f = _project.Frame ?? (_project.Frame = new TapeFrameSubset());
                ApplyControlsToFrame(f);
                MaterialStateService.SyncRecipeTapeFrameSpec(_project);
                RecipeStore.Save(_project);
                RecipeStore.SaveLastProjectName(_project.FileName);
                var host = FindForm() as Form1;
                if (host != null)
                    host.SaveMachineRecipe(_project.FileName);
                RefreshSpecList(f.FrameSpecName);
                MessageBox.Show("TapeFrame Spec 저장 및 Recipe 연동 완료.", "TapeFrame Spec", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("TapeFrame Spec 저장 실패: " + ex.Message, "TapeFrame Spec", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (MaterialSpecs.Data != null && MaterialSpecs.Data.Frames != null)
                {
                    foreach (var spec in MaterialSpecs.Data.Frames)
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

        private void ApplySpecToControls(TapeFrameSpec spec)
        {
            if (spec == null)
                return;

            _tbName.Text = spec.Name ?? "";
            _nGridX.Value = ClampDecimal(spec.GridX, _nGridX.Minimum, _nGridX.Maximum);
            _nGridY.Value = ClampDecimal(spec.GridY, _nGridY.Minimum, _nGridY.Maximum);
            _nPitchX.Value = ClampDecimal((decimal)spec.PitchX, _nPitchX.Minimum, _nPitchX.Maximum);
            _nPitchY.Value = ClampDecimal((decimal)spec.PitchY, _nPitchY.Minimum, _nPitchY.Maximum);
            _nDiameter.Value = ClampDecimal((decimal)spec.OuterDiameterMm, _nDiameter.Minimum, _nDiameter.Maximum);
        }

        private void ApplyControlsToFrame(TapeFrameSubset f)
        {
            if (f == null)
                return;

            f.FrameSpecName = string.IsNullOrWhiteSpace(_tbName.Text) ? "8inch_5x5" : _tbName.Text.Trim();
            f.GridX = (int)_nGridX.Value;
            f.GridY = (int)_nGridY.Value;
            f.PitchX = (double)_nPitchX.Value;
            f.PitchY = (double)_nPitchY.Value;
            f.OuterDiameterMm = (double)_nDiameter.Value;
            f.Rotate = _cbRotate.SelectedItem != null ? _cbRotate.SelectedItem.ToString() : "None";
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
