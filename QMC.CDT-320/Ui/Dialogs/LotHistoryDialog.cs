using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Lots;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>
    /// LOT 진행 이력 창.
    ///
    /// [LOT 관리 2026-07-27] 작업 정보 화면 안에 리스트를 상주시키면 기존 정보 타일의 높이가
    /// 부족해져 값이 잘린다. 그래서 LOT ID 입력줄 옆의 [이력] 버튼으로만 열도록 분리했다.
    /// 이력 자체는 Log\Lots\*.json 으로 저장되고 LotStorage 가 기동 시 다시 읽으므로,
    /// 프로그램을 재시작해도 그대로 보인다.
    /// </summary>
    public partial class LotHistoryDialog : Form
    {
        public LotHistoryDialog()
        {
            InitializeComponent();
            RefreshHistory();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                // 다른 세션/파일 복사로 이력 파일이 늘어났을 수 있으므로 디스크를 다시 읽는다.
                LotStorage.LoadHistoryFromDisk();
                RefreshHistory();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "LOT 이력을 다시 읽지 못했습니다: " + ex.Message,
                    "LOT 진행 이력", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private void RefreshHistory()
        {
            try
            {
                string activeLotId = LotSessionService.ActiveLotId;
                lblActiveLot.Text = string.IsNullOrEmpty(activeLotId)
                    ? "진행 중인 LOT: 없음"
                    : "진행 중인 LOT: " + activeLotId;
                lblActiveLot.ForeColor = string.IsNullOrEmpty(activeLotId)
                    ? Color.FromArgb(110, 110, 110)
                    : Color.FromArgb(21, 128, 61);

                gridLots.Rows.Clear();

                var lots = new List<Lot>();
                foreach (Lot lot in LotStorage.Lots.Values)
                {
                    if (lot != null)
                        lots.Add(lot);
                }

                lots.Sort((a, b) => b.StartedAt.CompareTo(a.StartedAt));

                foreach (Lot lot in lots)
                {
                    int index = gridLots.Rows.Add(
                        lot.LotID ?? "",
                        DescribeLotState(lot.State),
                        lot.RecipeName ?? "",
                        lot.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        lot.FinishedAt.HasValue ? lot.FinishedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-",
                        lot.ProcessedDies.ToString(),
                        lot.GoodCount.ToString(),
                        lot.NgCount.ToString(),
                        lot.YieldPercent.ToString("F1"));

                    if (lot.State == LotState.Running &&
                        string.Equals(lot.LotID ?? "", activeLotId, StringComparison.Ordinal))
                    {
                        gridLots.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(226, 245, 232);
                        gridLots.Rows[index].DefaultCellStyle.ForeColor = Color.FromArgb(21, 92, 48);
                    }
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "LotHistory",
                    "LOT 이력 표시에 실패했습니다: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static string DescribeLotState(LotState state)
        {
            switch (state)
            {
                case LotState.Running: return "진행중";
                case LotState.Completed: return "완료";
                case LotState.Aborted: return "중단";
                default: return "대기";
            }
        }
    }
}
