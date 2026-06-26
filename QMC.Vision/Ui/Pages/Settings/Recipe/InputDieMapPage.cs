using System;
using System.IO;
using System.Windows.Forms;
using QMC.Vision.DieMaps;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 레시피 — INPUT DIE 페이지(핸들러 MapCreatePage 기준).
    /// 웨이퍼 사양(Grid/Pitch/직경) + Edge skip 으로 격자 내접 원 다이맵을 생성하고 셀 클릭으로 처리 대상(IsTarget)을
    /// 토글한다. INVERT TARGET, JSON 내보내기/불러오기, 레시피 저장을 지원한다.
    /// </summary>
    public partial class InputDieMapPage : PageBase
    {
        private DieMap _map;

        public InputDieMapPage()
        {
            InitializeComponent();
            _mapView.CellClicked += OnCellClicked;
        }

        // ── Event Methods ──
        private void OnPageLoad(object sender, EventArgs e)
        {
            LoadFromRecipe();
        }

        private void OnCellClicked(DieMapEntry entry)
        {
            try
            {
                if (entry == null) return;
                entry.IsTarget = !entry.IsTarget;
                if (!entry.IsTarget) entry.SequenceNo = 0;
                _mapView.Invalidate();
                UpdateInfo();
            }
            catch (Exception ex)
            {
                _lblInfo.Text = "토글 실패: " + ex.Message;
            }
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            GenerateFromSpec();
        }

        private void btnInvert_Click(object sender, EventArgs e)
        {
            try
            {
                if (_map == null) return;
                DieMapBuilder.InvertTarget(_map);
                _mapView.Invalidate();
                UpdateInfo();
            }
            catch (Exception ex)
            {
                _lblInfo.Text = "반전 실패: " + ex.Message;
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SaveToRecipe();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            ExportJson();
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            ImportJson();
        }

        // ── Public Methods ──
        public void LoadFromRecipe()
        {
            try
            {
                var r = GetRecipe();
                if (r == null) { _lblInfo.Text = "활성 레시피 없음"; return; }

                _nSideSkip.Value = ClampDec(r.WaferSideEdgeSkip, _nSideSkip);
                _nTbSkip.Value = ClampDec(r.WaferTopBottomEdgeSkip, _nTbSkip);
                UpdateSpecLabel(r);

                if (r.InputDieMap != null && r.InputDieMap.Entries != null && r.InputDieMap.Entries.Count > 0)
                    _map = r.InputDieMap;
                else
                    _map = BuildFromSpec(r);

                ShowMap();
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
                if (m == null || r == null || _map == null)
                {
                    MessageBox.Show("활성 레시피/다이맵이 없어 저장할 수 없습니다.", "INPUT DIE",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                r.WaferSideEdgeSkip = (int)_nSideSkip.Value;
                r.WaferTopBottomEdgeSkip = (int)_nTbSkip.Value;
                DieMapBuilder.Normalize(_map);
                r.InputDieMap = _map;
                string name = m.CurrentRecipeName;
                m.SaveRecipe(name);
                host.SetRecipeStatus(name);
                _lblInfo.Text = "저장됨 [" + name + "]  활성 다이=" + DieMapBuilder.CountTargets(_map);
            }
            catch (Exception ex)
            {
                MessageBox.Show("INPUT DIE 저장에 실패했습니다.\n" + ex.Message, "INPUT DIE",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Private Methods ──
        private DieMap BuildFromSpec(VisionMachineRecipe r)
        {
            int sideSkip = (int)_nSideSkip.Value;
            int tbSkip = (int)_nTbSkip.Value;
            return DieMapBuilder.GenerateCircleDieMap(r.WaferGridX, r.WaferGridY, r.WaferPitchX, r.WaferPitchY,
                r.WaferOuterDiameterMm, sideSkip, tbSkip, "INPUT");
        }

        private void GenerateFromSpec()
        {
            try
            {
                var r = GetRecipe();
                if (r == null) { _lblInfo.Text = "활성 레시피 없음"; return; }
                _map = BuildFromSpec(r);
                ShowMap();
            }
            catch (Exception ex)
            {
                _lblInfo.Text = "생성 실패: " + ex.Message;
            }
        }

        private void ExportJson()
        {
            try
            {
                if (_map == null) { _lblInfo.Text = "내보낼 다이맵 없음"; return; }
                using (var dlg = new SaveFileDialog
                {
                    Filter = "DieMap JSON (*.json)|*.json",
                    FileName = "input_die_map.json"
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    bool ok = DieMapBuilder.SaveJson(_map, dlg.FileName);
                    _lblInfo.Text = ok ? "내보냄: " + Path.GetFileName(dlg.FileName) : "내보내기 실패";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("내보내기에 실패했습니다.\n" + ex.Message, "INPUT DIE",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportJson()
        {
            try
            {
                using (var dlg = new OpenFileDialog
                {
                    Filter = "DieMap JSON (*.json)|*.json",
                    CheckFileExists = true
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    var loaded = DieMapBuilder.LoadJson(dlg.FileName);
                    if (loaded == null)
                    {
                        MessageBox.Show("다이맵을 불러오지 못했습니다(형식 확인).", "INPUT DIE",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    _map = loaded;
                    ShowMap();
                    _lblInfo.Text = "불러옴: " + Path.GetFileName(dlg.FileName) + "  활성 다이=" + DieMapBuilder.CountTargets(_map);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("불러오기에 실패했습니다.\n" + ex.Message, "INPUT DIE",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        private void ShowMap()
        {
            _mapView.ShowWaferOutline = true;
            _mapView.Caption = "Input Die Map (클릭=대상 토글)";
            _mapView.CellColorResolver = null;   // 기본(대상=회청/비대상=회색)
            _mapView.Map = _map;
            UpdateInfo();
        }

        private void UpdateSpecLabel(VisionMachineRecipe r)
        {
            if (r == null) { _lblSpec.Text = ""; return; }
            _lblSpec.Text = string.Format("Frame: {0}\r\nGrid {1}×{2}  pitch=({3:F3},{4:F3})mm\r\nØ {5:F1}mm",
                string.IsNullOrWhiteSpace(r.FrameSpecName) ? "(none)" : r.FrameSpecName,
                r.WaferGridX, r.WaferGridY, r.WaferPitchX, r.WaferPitchY, r.WaferOuterDiameterMm);
        }

        private void UpdateInfo()
        {
            if (_map == null) { _lblInfo.Text = ""; return; }
            _lblInfo.Text = string.Format("격자 {0}×{1}  전체 {2}  활성 다이 {3}",
                _map.DieMapX, _map.DieMapY, _map.TotalCells, DieMapBuilder.CountTargets(_map));
        }
    }
}
