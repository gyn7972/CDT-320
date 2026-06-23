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

            VisionModuleTestDialog.AddLaunchers(
                actionPanel.Controls, this, btnStop,
                Tuple.Create<string, Func<VisionTcpClient>, string>("VISION: BOTTOM INSP", () => VisionHub.Inspection, "Bottom Inspection"),
                Tuple.Create<string, Func<VisionTcpClient>, string>("VISION: TOP SIDE",    () => VisionHub.TopSide,    "Top Side Vision"),
                Tuple.Create<string, Func<VisionTcpClient>, string>("VISION: BOTTOM SIDE", () => VisionHub.BottomSide, "Bottom Side Vision"));
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
