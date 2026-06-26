using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.Vision.DieMaps;
using QMC.Vision.Modules;

namespace QMC.Vision.Ui.Pages
{
    /// <summary>
    /// 레시피 — 픽업 순서 페이지(핸들러 PickupSubsetPage 기준).
    /// Pickup target(WAFER INPUT / BIN OUTPUT) + 시작 코너/방향/지그재그를 고르면, 웨이퍼 사양으로 만든
    /// 격자 내접 원 다이맵에 픽업 순번을 부여해 순서대로(번호 + 색상 그라데이션) 그린다.
    /// 적용 시 대상에 맞는 레시피 픽업 옵션(Pickup/OutputPickup)을 저장한다.
    /// </summary>
    public partial class PickupSequencePage : PageBase
    {
        private DieMap _map;
        private int _maxSeq;
        private bool _isOutput;   // true = BIN OUTPUT, false = WAFER INPUT

        public PickupSequencePage()
        {
            InitializeComponent();
        }

        // ── Event Methods ──
        private void OnPageLoad(object sender, EventArgs e)
        {
            LoadFromRecipe();
        }

        private void OnTargetChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton rb && !rb.Checked) return;
            _isOutput = _rbOutput.Checked;
            LoadOptionsForTarget();
            UpdatePreview();
        }

        private void OnOptionChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton rb && !rb.Checked) return;
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
                _isOutput = false;
                _rbInput.Checked = true;
                _rbOutput.Checked = false;
                LoadOptionsForTarget();
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
                    MessageBox.Show("활성 레시피가 없어 저장할 수 없습니다.", "픽업 순서",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var opt = ReadOptions();
                if (_isOutput) r.OutputPickup = opt;
                else r.Pickup = opt;

                string name = m.CurrentRecipeName;
                m.SaveRecipe(name);
                host.SetRecipeStatus(name);
                _lblInfo.Text = "저장됨 [" + name + "]  " + (_isOutput ? "BIN OUTPUT" : "WAFER INPUT") + "  순번 다이=" + _maxSeq;
            }
            catch (Exception ex)
            {
                MessageBox.Show("픽업 순서 저장에 실패했습니다.\n" + ex.Message, "픽업 순서",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Private Methods ──
        private void LoadOptionsForTarget()
        {
            var r = GetRecipe();
            PickupSubset p = (_isOutput ? r?.OutputPickup : r?.Pickup) ?? new PickupSubset();

            _rbTL.Checked = p.StartCorner == PickupStartCorner.TopLeft;
            _rbTR.Checked = p.StartCorner == PickupStartCorner.TopRight;
            _rbBL.Checked = p.StartCorner == PickupStartCorner.BottomLeft;
            _rbBR.Checked = p.StartCorner == PickupStartCorner.BottomRight;
            if (!_rbTL.Checked && !_rbTR.Checked && !_rbBL.Checked && !_rbBR.Checked)
                _rbTR.Checked = true;

            _rbVert.Checked = p.Direction == PickupDirection.Vertical;
            _rbHoriz.Checked = p.Direction == PickupDirection.Horizontal;
            if (!_rbVert.Checked && !_rbHoriz.Checked) _rbVert.Checked = true;

            _rbZigZag.Checked = p.Pattern == PickupPattern.ZigZag;
            _rbStraight.Checked = p.Pattern == PickupPattern.Straight;
            if (!_rbZigZag.Checked && !_rbStraight.Checked) _rbZigZag.Checked = true;
        }

        private PickupSubset ReadOptions()
        {
            var p = new PickupSubset();
            if (_rbTL.Checked) p.StartCorner = PickupStartCorner.TopLeft;
            else if (_rbBL.Checked) p.StartCorner = PickupStartCorner.BottomLeft;
            else if (_rbBR.Checked) p.StartCorner = PickupStartCorner.BottomRight;
            else p.StartCorner = PickupStartCorner.TopRight;

            p.Direction = _rbHoriz.Checked ? PickupDirection.Horizontal : PickupDirection.Vertical;
            p.Pattern = _rbStraight.Checked ? PickupPattern.Straight : PickupPattern.ZigZag;
            return p;
        }

        private DieMap BuildBaseMap(VisionMachineRecipe r)
        {
            // 입력 대상이고 저장된 INPUT DIE 맵이 있으면 그것을, 아니면 웨이퍼 사양으로 격자 내접 원 생성.
            if (!_isOutput && r.InputDieMap != null && r.InputDieMap.Entries != null && r.InputDieMap.Entries.Count > 0)
                return r.InputDieMap;
            return DieMapBuilder.GenerateCircleDieMap(r.WaferGridX, r.WaferGridY, r.WaferPitchX, r.WaferPitchY,
                r.WaferOuterDiameterMm, r.WaferSideEdgeSkip, r.WaferTopBottomEdgeSkip,
                _isOutput ? "BIN" : "WAFER");
        }

        private static string DescribeOptions(PickupSubset p)
            => p.StartCorner + " / " + p.Direction + " / " + p.Pattern;

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

        private static int Clamp(int v) => v < 0 ? 0 : (v > 255 ? 255 : v);

        // ── UI Update Methods ──
        private void UpdatePreview()
        {
            try
            {
                var r = GetRecipe();
                if (r == null) { _lblInfo.Text = "활성 레시피 없음"; return; }

                _map = BuildBaseMap(r);
                PickupSubset opt = ReadOptions();
                PickupSequenceGenerator.ApplySequenceNumbers(_map, opt);
                _maxSeq = DieMapBuilder.CountTargets(_map);

                _mapView.ShowWaferOutline = true;
                _mapView.Caption = (_isOutput ? "Bin Output" : "Wafer Input") + " Pickup Order";
                _mapView.CellColorResolver = ResolveSequenceColor;
                _mapView.Map = _map;

                _lblInfo.Text = string.Format("격자 {0}×{1}  순번 다이={2}  ({3})",
                    _map.DieMapX, _map.DieMapY, _maxSeq, DescribeOptions(opt));
            }
            catch (Exception ex)
            {
                _lblInfo.Text = "미리보기 실패: " + ex.Message;
            }
        }

        /// <summary>픽업 순번을 파랑(처음)→빨강(마지막) 그라데이션으로 표시.</summary>
        private Color ResolveSequenceColor(DieMapEntry entry)
        {
            if (entry == null || !entry.IsTarget)
                return Color.FromArgb(60, 60, 60);
            if (entry.SequenceNo <= 0 || _maxSeq <= 1)
                return Color.FromArgb(80, 80, 100);

            double t = (entry.SequenceNo - 1) / (double)(_maxSeq - 1);
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            int rr = (int)(60 + t * (210 - 60));
            int gg = (int)(120 + (1 - Math.Abs(0.5 - t) * 2) * 80);
            int bb = (int)(210 - t * (210 - 60));
            return Color.FromArgb(Clamp(rr), Clamp(gg), Clamp(bb));
        }
    }
}
