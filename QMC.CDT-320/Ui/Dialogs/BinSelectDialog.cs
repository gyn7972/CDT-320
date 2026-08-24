using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Bin;
using QMC.CDT320.Lots;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// [P4 2026-08-22] 픽업 BIN 선택 다이얼로그 (작업/LOT 단위).
    /// - 기본 ALL(맵의 다이 전부). BIN 번호를 지정하면 그 BIN만 픽업.
    /// - bin 목록/다이 수/이름은 수신된 LOT 웨이퍼맵(기준 슬롯 1개)에서 채운다 — 같은 LOT은
    ///   레이아웃이 동일하므로(실측 확정) 목록 기준으로 충분하고, 실제 필터는 웨이퍼별 자기 bin 값에 적용된다.
    /// - 변경 적용 경계는 다음 웨이퍼(맵 적용 시점)부터 — 화면에 명시한다.
    /// - bin 색은 기존 BinCodeMap(구간 사전)으로 표시만 한다(편집은 범위 밖).
    /// [검토수정 2026-08-22] AGENTS.md §8에 맞춰 Designer 구조(partial + InitializeComponent)로 재작성.
    /// </summary>
    internal sealed partial class BinSelectDialog : Form
    {
        public string SelectedMode { get; private set; } = "All";
        public List<int> SelectedBins { get; private set; } = new List<int>();

        public BinSelectDialog(string lotId, LotWaferMapSlotInfo reference, string currentMode, List<int> currentBins)
        {
            InitializeComponent();

            // [P5 2026-08-24] 기준 맵 표기: 파일명=바코드(1:1)라 슬롯 번호 대신 바코드를 표시한다.
            lblHeader.Text = "LOT: " + (string.IsNullOrWhiteSpace(lotId) ? "(진행 중인 LOT 없음)" : lotId) +
                             (reference != null
                                 ? "   /   기준 맵: " +
                                   (string.IsNullOrWhiteSpace(reference.Barcode) ? "-" : reference.Barcode) +
                                   " (" + reference.DieCount + "다이)"
                                 : "   /   기준 맵: 수신된 웨이퍼맵 없음(첫 웨이퍼 바코드 판독 후 표시)");

            PopulateBins(reference, currentBins);

            bool selectedMode = string.Equals(currentMode, "Selected", StringComparison.OrdinalIgnoreCase) &&
                                currentBins != null && currentBins.Count > 0;
            rdoAll.Checked = !selectedMode;
            rdoSelected.Checked = selectedMode;
            lsvBins.Enabled = selectedMode;
        }

        private void rdoMode_CheckedChanged(object sender, EventArgs e)
        {
            lsvBins.Enabled = rdoSelected.Checked;
        }

        private void PopulateBins(LotWaferMapSlotInfo reference, List<int> currentBins)
        {
            lsvBins.Items.Clear();

            if (reference != null && reference.BinDieCounts != null)
            {
                var bins = new List<int>(reference.BinDieCounts.Keys);
                bins.Sort();
                foreach (int bin in bins)
                {
                    string name;
                    if (reference.BinNames == null || !reference.BinNames.TryGetValue(bin, out name))
                        name = "";

                    var item = new ListViewItem(bin.ToString("000"));
                    item.SubItems.Add(reference.BinDieCounts[bin].ToString("N0"));
                    item.SubItems.Add(name);
                    item.Tag = bin;
                    item.ForeColor = ResolveBinDisplayColor(bin);
                    item.Checked = currentBins != null && currentBins.Contains(bin);
                    lsvBins.Items.Add(item);
                }
            }

            // 현재 선택에 있으나 기준 맵에 없는 bin도 잃지 않게 표시한다(다른 로트에서 저장된 값 방어).
            if (currentBins != null)
            {
                foreach (int bin in currentBins)
                {
                    if (reference != null && reference.BinDieCounts != null &&
                        reference.BinDieCounts.ContainsKey(bin))
                        continue;

                    var item = new ListViewItem(bin.ToString("000"));
                    item.SubItems.Add("-");
                    item.SubItems.Add("(기준 맵에 없음)");
                    item.Tag = bin;
                    item.ForeColor = ResolveBinDisplayColor(bin);
                    item.Checked = true;
                    lsvBins.Items.Add(item);
                }
            }

            if (lsvBins.Items.Count == 0)
            {
                var empty = new ListViewItem("-");
                empty.SubItems.Add("-");
                empty.SubItems.Add("수신된 웨이퍼맵이 없습니다. LOT 시작(프리페치) 후 다시 여세요.");
                empty.Tag = null;
                lsvBins.Items.Add(empty);
            }
        }

        private static Color ResolveBinDisplayColor(int bin)
        {
            try
            {
                Color color = BinCodeMap.ConvertToBinCodeColor(bin);
                // 흰 배경 리스트라 검정(미지정)과 지나치게 밝은 색만 보정한다.
                if (color.ToArgb() == Color.Black.ToArgb())
                    return Color.FromArgb(60, 60, 60);
                if (color.GetBrightness() > 0.82f)
                    return Color.FromArgb(150, 120, 0);
                return color;
            }
            catch
            {
                return Color.FromArgb(60, 60, 60);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (rdoAll.Checked)
            {
                SelectedMode = "All";
                SelectedBins = new List<int>();
                DialogResult = DialogResult.OK;
                return;
            }

            var bins = new List<int>();
            foreach (ListViewItem item in lsvBins.Items)
            {
                if (item.Checked && item.Tag is int)
                    bins.Add((int)item.Tag);
            }

            if (bins.Count == 0)
            {
                QMC.Common.MessageDialog.Show(this,
                    "지정 BIN 모드에는 최소 1개의 BIN을 선택해야 합니다.\r\n" +
                    "(전부 픽업하려면 ALL을 선택하세요)",
                    "픽업 BIN 선택", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedMode = "Selected";
            SelectedBins = bins;
            DialogResult = DialogResult.OK;
        }
    }
}
