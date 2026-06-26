using System;
using System.Windows.Forms;
using QMC.Vision.DieMaps;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 레시피 — 웨이퍼 사양 페이지(핸들러 TapeFrameSubsetPage 기준).
    /// Grid X/Y 개수 + Pitch + Outer diameter + Rotate 를 입력하고, 저장 스펙 라이브러리(LOAD/SAVE SPEC)로
    /// 명명된 사양을 관리한다. 핸들러와 동일한 격자 내접 원으로 미리보기를 그리고, 적용 시 활성 레시피에 저장한다.
    /// </summary>
    public partial class WaferSpecPage : PageBase
    {
        private DieMap _preview;

        public WaferSpecPage()
        {
            InitializeComponent();
        }

        // ── Event Methods ──
        private void OnPageLoad(object sender, EventArgs e)
        {
            LoadFromRecipe();
        }

        private void btnLoadSpec_Click(object sender, EventArgs e)
        {
            LoadSpecFromLibrary();
        }

        private void btnSaveSpec_Click(object sender, EventArgs e)
        {
            SaveSpecToLibrary();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SaveToRecipe();
        }

        // ── Public Methods ──
        public void LoadFromRecipe()
        {
            try
            {
                var r = GetRecipe();
                if (r == null) { _lblInfo.Text = "활성 레시피 없음"; return; }

                _tbName.Text = r.FrameSpecName ?? "";
                _nGridX.Value = ClampDec(r.WaferGridX, _nGridX);
                _nGridY.Value = ClampDec(r.WaferGridY, _nGridY);
                _nPitchX.Value = ClampDec((decimal)r.WaferPitchX, _nPitchX);
                _nPitchY.Value = ClampDec((decimal)r.WaferPitchY, _nPitchY);
                _nDiameter.Value = ClampDec((decimal)r.WaferOuterDiameterMm, _nDiameter);
                SelectRotate(r.WaferRotate);
                RefreshSpecList(r.FrameSpecName);
                UpdatePreview();
            }
            catch (Exception ex)
            {
                _lblInfo.Text = "로드 실패: " + ex.Message;
            }
        }

        public void SaveToRecipe()
        {
            try
            {
                if (IsRecipeLockedByReady()) return;
                var host = FindForm() as Form1;
                var m = host?.Machine;
                var r = m?.Recipe;
                if (m == null || r == null)
                {
                    MessageBox.Show("활성 레시피가 없어 저장할 수 없습니다.", "웨이퍼 사양",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                r.FrameSpecName = _tbName.Text != null ? _tbName.Text.Trim() : "";
                r.WaferGridX = (int)_nGridX.Value;
                r.WaferGridY = (int)_nGridY.Value;
                r.WaferPitchX = (double)_nPitchX.Value;
                r.WaferPitchY = (double)_nPitchY.Value;
                r.WaferOuterDiameterMm = (double)_nDiameter.Value;
                r.WaferRotate = _cbRotate.SelectedItem != null ? _cbRotate.SelectedItem.ToString() : "None";

                string name = m.CurrentRecipeName;
                m.SaveRecipe(name);
                host.SetRecipeStatus(name);
                UpdatePreview();
                _lblInfo.Text = "저장됨 [" + name + "]  " + DescribePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show("웨이퍼 사양 저장에 실패했습니다.\n" + ex.Message, "웨이퍼 사양",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Private Methods ──
        private void LoadSpecFromLibrary()
        {
            try
            {
                string name = _cbSpecLibrary.SelectedItem != null ? _cbSpecLibrary.SelectedItem.ToString() : "";
                var spec = TapeFrameSpecStore.Find(name);
                if (spec == null)
                {
                    MessageBox.Show("선택한 웨이퍼 사양을 찾을 수 없습니다.", "웨이퍼 사양",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _tbName.Text = spec.Name ?? "";
                _nGridX.Value = ClampDec(spec.DieMapX, _nGridX);
                _nGridY.Value = ClampDec(spec.DieMapY, _nGridY);
                _nPitchX.Value = ClampDec((decimal)spec.PitchX, _nPitchX);
                _nPitchY.Value = ClampDec((decimal)spec.PitchY, _nPitchY);
                _nDiameter.Value = ClampDec((decimal)spec.OuterDiameterMm, _nDiameter);
                SelectRotate(spec.Rotate);
                UpdatePreview();
                _lblInfo.Text = "사양 불러옴: " + spec.Name;
            }
            catch (Exception ex)
            {
                MessageBox.Show("사양 불러오기 실패: " + ex.Message, "웨이퍼 사양",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveSpecToLibrary()
        {
            try
            {
                string name = _tbName.Text != null ? _tbName.Text.Trim() : "";
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("저장할 사양 이름(Spec name)을 입력하세요.", "웨이퍼 사양",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var spec = new TapeFrameSpec
                {
                    Name = name,
                    DieMapX = (int)_nGridX.Value,
                    DieMapY = (int)_nGridY.Value,
                    PitchX = (double)_nPitchX.Value,
                    PitchY = (double)_nPitchY.Value,
                    OuterDiameterMm = (double)_nDiameter.Value,
                    Rotate = _cbRotate.SelectedItem != null ? _cbRotate.SelectedItem.ToString() : "None"
                };
                if (TapeFrameSpecStore.AddOrUpdate(spec))
                {
                    RefreshSpecList(name);
                    _lblInfo.Text = "사양 저장됨: " + name;
                }
                else
                {
                    MessageBox.Show("사양 저장에 실패했습니다.", "웨이퍼 사양",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("사양 저장 실패: " + ex.Message, "웨이퍼 사양",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshSpecList(string selected)
        {
            try
            {
                _cbSpecLibrary.Items.Clear();
                foreach (var n in TapeFrameSpecStore.Names())
                    _cbSpecLibrary.Items.Add(n);
                if (!string.IsNullOrWhiteSpace(selected) && !_cbSpecLibrary.Items.Contains(selected))
                    _cbSpecLibrary.Items.Add(selected);
                if (!string.IsNullOrWhiteSpace(selected))
                    _cbSpecLibrary.SelectedItem = selected;
                if (_cbSpecLibrary.SelectedIndex < 0 && _cbSpecLibrary.Items.Count > 0)
                    _cbSpecLibrary.SelectedIndex = 0;
            }
            catch { /* 목록 갱신 실패 무시 */ }
        }

        private void SelectRotate(string rotate)
        {
            string r = string.IsNullOrWhiteSpace(rotate) ? "None" : rotate;
            int idx = _cbRotate.Items.IndexOf(r);
            _cbRotate.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private VisionMachineRecipe GetRecipe()
            => (FindForm() as Form1)?.Machine?.Recipe;

        private bool IsRecipeLockedByReady()
        {
            var host = FindForm() as Form1;
            if (host != null && host.IsReady)
            {
                MessageBox.Show("READY(핸들러 사용 중) 상태에서는 레시피를 변경할 수 없습니다.\r\nREADY 해제 후 진행하세요.",
                    "레시피 잠금", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }
            return false;
        }

        private static decimal ClampDec(decimal v, NumericUpDown n)
        {
            if (v < n.Minimum) return n.Minimum;
            if (v > n.Maximum) return n.Maximum;
            return v;
        }

        // ── UI Update Methods ──
        private void UpdatePreview()
        {
            try
            {
                int gridX = (int)_nGridX.Value;
                int gridY = (int)_nGridY.Value;
                double pitchX = (double)_nPitchX.Value;
                double pitchY = (double)_nPitchY.Value;
                double diameter = (double)_nDiameter.Value;

                _preview = DieMapBuilder.GenerateCircleDieMap(gridX, gridY, pitchX, pitchY, diameter, 0, 0, "WAFER");
                _mapView.ShowWaferOutline = true;
                _mapView.Caption = "Wafer Spec Preview";
                _mapView.Map = _preview;
                _lblInfo.Text = DescribePreview();
            }
            catch (Exception ex)
            {
                _lblInfo.Text = "미리보기 실패: " + ex.Message;
            }
        }

        private string DescribePreview()
        {
            if (_preview == null) return "";
            int targets = DieMapBuilder.CountTargets(_preview);
            return string.Format("격자 {0}×{1}  pitch=({2:F3},{3:F3})mm  활성 다이={4}",
                _preview.DieMapX, _preview.DieMapY, _preview.PitchX, _preview.PitchY, targets);
        }
    }
}
