using System;
using System.Windows.Forms;
using QMC.CDT320.Sequencing;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class FrontPickerPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private PickerWorkInfoPageRuntime _runtime;

        public FrontPickerPage()
        {
            InitializeComponent();
            _runtime = new PickerWorkInfoPageRuntime(
                this,
                PickerSequenceSide.Front,
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
                lblHeadProcessValue,
                new Label[] { lblFlowAvoid, lblFlowPickup, lblFlowBottom, lblFlowSide, lblFlowPlace },
                lblProcessDetailValue,
                new Label[] { lblCollet1UseTitle, lblCollet2UseTitle, lblCollet3UseTitle, lblCollet4UseTitle },
                new Label[] { lblCollet1UseValue, lblCollet2UseValue, lblCollet3UseValue, lblCollet4UseValue },
                new IndicatorDot[] { dotHeadVacuum1, dotHeadVacuum2, dotHeadVacuum3, dotHeadVacuum4 },
                new IndicatorDot[] { dotHeadBlow1, dotHeadBlow2, dotHeadBlow3, dotHeadBlow4 },
                new IndicatorDot[] { dotHeadFlow1, dotHeadFlow2, dotHeadFlow3, dotHeadFlow4 },
                new Label[] { lblHeadVacuum1, lblHeadVacuum2, lblHeadVacuum3, lblHeadVacuum4 },
                new Label[] { lblHeadBlow1, lblHeadBlow2, lblHeadBlow3, lblHeadBlow4 },
                new Label[] { lblHeadFlow1, lblHeadFlow2, lblHeadFlow3, lblHeadFlow4 },
                axisGrid,
                btnCountClear,
                btnInput,
                btnInspect,
                btnBottom,
                btnSide,
                btnOutput,
                btnPickUpTest,
                cmbPickZTestPickerNo,
                btnPickZTest,
                btnStop,
                actionPanel.Controls);

            // 버튼 전용(입력 없음) Head 비전 테스트 — 시퀀서(PickerUnit)와 동일한 TpuVisionAdapter 호출(수동==실제 시퀀스).
            TpuVisionTestDialog.AddLaunchers(actionRightPanel.Controls, this, btnStop);
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
