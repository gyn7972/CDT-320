using System;
using System.Windows.Forms;
using QMC.CDT320.Sequencing;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class RearPickerPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private PickerWorkInfoPageRuntime _runtime;

        public RearPickerPage()
        {
            InitializeComponent();
            _runtime = new PickerWorkInfoPageRuntime(
                this,
                PickerSequenceSide.Rear,
                GetHost,
                lblHeader,
                new Label[] { lblHead1Value, lblHead2Value, lblHead3Value, lblHead4Value },
                lblColletChangeValue,
                lblAutoPosValue,
                lblColletCleaningValue,
                lblColletCheckValue,
                lblPickFailValue,
                lblPlaceFailValue,
                lblHeadZoneValue,
                lblProcessDetailValue,
                new Label[] { lblCollet1UseTitle, lblCollet2UseTitle, lblCollet3UseTitle, lblCollet4UseTitle },
                new Label[] { lblCollet1UseValue, lblCollet2UseValue, lblCollet3UseValue, lblCollet4UseValue },
                new IndicatorDot[] { dotHeadVacuum1, dotHeadVacuum2, dotHeadVacuum3, dotHeadVacuum4 },
                new IndicatorDot[] { dotHeadBlow1, dotHeadBlow2, dotHeadBlow3, dotHeadBlow4 },
                new Label[] { lblHeadVacuum1, lblHeadVacuum2, lblHeadVacuum3, lblHeadVacuum4 },
                new Label[] { lblHeadBlow1, lblHeadBlow2, lblHeadBlow3, lblHeadBlow4 },
                new Label[] { lblAxis1Value, lblAxis2Value, lblAxis3Value, lblAxis4Value, lblAxis5Value, lblAxis6Value, lblAxis7Value, lblAxis8Value, lblAxis9Value, lblAxis10Value },
                headDieDetailView,
                new RadioButton[] { btnHead1Select, btnHead2Select, btnHead3Select, btnHead4Select },
                btnCountClear,
                btnInput,
                btnInspect,
                btnBottom,
                btnSide,
                btnOutput,
                btnPickUpTest,
                null,
                null,
                btnStop,
                actionPanel.Controls);

            // 버튼 전용(입력 없음) Head 비전 테스트 — 시퀀서(PickerUnit)와 동일한 TpuVisionAdapter 호출(수동==실제 시퀀스).
            TpuVisionTestDialog.AddLaunchers(actionRightPanel.Controls, this, btnStop);

            // STOP/비전 런처 버튼을 메인 액션 버튼과 동일 사이즈로 통일하고 그리드 셀에 배치.
            // 배치: [빈칸][STOP] / [비전][비전] / [비전]
            int visionIndex = 0;
            foreach (Control control in actionRightPanel.Controls)
            {
                if (!(control is ActionButton button))
                    continue;

                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(3);
                button.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);

                if (ReferenceEquals(button, btnStop))
                {
                    actionRightPanel.SetCellPosition(button, new TableLayoutPanelCellPosition(1, 0));
                }
                else
                {
                    actionRightPanel.SetCellPosition(button, new TableLayoutPanelCellPosition(visionIndex % 2, 1 + visionIndex / 2));
                    visionIndex++;
                }
            }
        }

        private Form1 GetHost()
        {
            return FindForm() as Form1;
        }

        private void lblHead1Value_Click(object sender, System.EventArgs e)
        {
            _runtime.ShowHeadDieDialog(1);
        }

        private void lblHead2Value_Click(object sender, System.EventArgs e)
        {
            _runtime.ShowHeadDieDialog(2);
        }

        private void lblHead3Value_Click(object sender, System.EventArgs e)
        {
            _runtime.ShowHeadDieDialog(3);
        }

        private void lblHead4Value_Click(object sender, System.EventArgs e)
        {
            _runtime.ShowHeadDieDialog(4);
        }
    }
}
