using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Sequencing;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Equipment.Vision;
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

            WireVisionButtons();
        }

        private void WireVisionButtons()
        {
            btnVisionBottomInspect.Click += (s, e) =>
                TpuVisionTestDialog.Open(this, "Bottom Inspection", TpuVisionTestDialog.Mode.BottomInspection);
            btnVisionFrontSide.Click += (s, e) =>
                TpuVisionTestDialog.Open(this, "FrontSideVision", TpuVisionTestDialog.Mode.Side, 1, () => VisionHub.FrontSideVision, VisionViewerPorts.FrontSideVision, VisionToolIds.FrontSide.SurfaceInspector);
            btnVisionRearSide.Click += (s, e) =>
                TpuVisionTestDialog.Open(this, "RearSideVision", TpuVisionTestDialog.Mode.Side, 1, () => VisionHub.RearSideVision, VisionViewerPorts.RearSideVision, VisionToolIds.RearSide.SurfaceInspector);
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

        private void btnAjinLineMapTest_Click(object sender, EventArgs e)
        {
            btnAjinLineMapTest.Enabled = false;
            try
            {
                Form1 host = GetHost();
                if (host == null || host.Machine == null)
                {
                    QMC.Common.MessageDialog.Show(this, "장비 객체를 찾을 수 없어 ContiNode LineMap 검증을 실행할 수 없습니다.", "LINE MAP TEST",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string readyReason;
                if (!PickerContiLineTestRunner.EnsureAjinReady(out readyReason))
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MAP-TEST", "RearPickerPage", readyReason);
                    QMC.Common.MessageDialog.Show(this, readyReason, "LINE MAP TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                List<PickerContiLineMapTestResult> results =
                    PickerContiLineTestRunner.RunGoodStageYLineMapTests(host.Machine, PickerSequenceSide.Rear);
                int failCount = results.FindAll(x => x.Result == null || !x.Result.Success).Count;
                QMC.Common.Logging.EventKind kind = failCount == 0 ? QMC.Common.Logging.EventKind.Event : QMC.Common.Logging.EventKind.Warning;

                foreach (PickerContiLineMapTestResult item in results)
                {
                    string detail = item.Name + ": " + (item.Result != null ? item.Result.ToString() : "결과 없음");
                    QMC.Common.Logging.EventLogger.Write(kind, "UI", "AJIN-LINE-MAP-TEST", "RearPickerPage", detail);
                }

                string message = failCount == 0
                    ? "RearPicker GOOD StageY 기준 ContiNode LineMap 검증이 완료되었습니다. 전체 성공=" + results.Count + "건"
                    : "RearPicker GOOD StageY 기준 ContiNode LineMap 검증 중 실패가 있습니다. 실패=" + failCount + "건 / 전체=" + results.Count + "건";

                QMC.Common.MessageDialog.Show(this, message + "\r\n상세 내용은 Alarm/Event Log를 확인하세요.", "LINE MAP TEST",
                    MessageBoxButtons.OK, failCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                string message = "RearPicker GOOD StageY 기준 ContiNode LineMap 검증 중 예외가 발생했습니다. error=" + ex.Message;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MAP-TEST", "RearPickerPage", message);
                QMC.Common.MessageDialog.Show(this, message, "LINE MAP TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnAjinLineMapTest.Enabled = true;
            }
        }

        private async void btnAjinLineMoveTest_Click(object sender, EventArgs e)
        {
            Form1 confirmHost = GetHost();
            PickerPlaceMotionConfig placeConfig = PickerContiLineTestRunner.ResolvePlaceConfig(
                confirmHost != null ? confirmHost.Machine : null,
                PickerSequenceSide.Rear);

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                this,
                "RearPicker Place teaching center ContiNode 이동 테스트를 실행할까요?\r\n" +
                "순서: Picker #4 -> #3 -> #2 -> #1\r\n" +
                "시작 전 #4 Place teaching 위치로 이동한 뒤, 각 세그먼트는 GOOD StageY + PickerX + 이전 PickerZ + 현재 PickerZ를 ContiNode로 구동합니다.\r\n" +
                "Conti 파라미터: coord=" + placeConfig.ContiCoordinate +
                ", maxVel=" + placeConfig.ContiMaxVelocity.ToString("F3") +
                ", maxAcc=" + placeConfig.ContiMaxAcceleration.ToString("F3") +
                ", maxDec=" + placeConfig.ContiMaxDeceleration.ToString("F3") + "\r\n" +
                "축 주변 안전 상태와 제품 유무를 확인한 뒤 실행하세요.",
                "LINE MOVE TEST",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            btnAjinLineMoveTest.Enabled = false;
            btnAjinLineMapTest.Enabled = false;
            try
            {
                Form1 host = GetHost();
                if (host == null || host.Machine == null)
                {
                    QMC.Common.MessageDialog.Show(this, "장비 객체를 찾을 수 없어 ContiNode 이동 테스트를 실행할 수 없습니다.", "LINE MOVE TEST",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string readyReason;
                if (!PickerContiLineTestRunner.EnsureAjinReady(out readyReason))
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MOVE-TEST", "RearPickerPage", readyReason);
                    QMC.Common.MessageDialog.Show(this, readyReason, "LINE MOVE TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                PickerContiLineMoveRunResult runResult =
                    await PickerContiLineTestRunner.RunGoodStagePlaceLineMoveTestAsync(
                        host.Machine,
                        PickerSequenceSide.Rear,
                        CancellationToken.None).ConfigureAwait(true);

                if (!runResult.Success)
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MOVE-TEST", "RearPickerPage", runResult.Message);
                    QMC.Common.MessageDialog.Show(this, runResult.Message, "LINE MOVE TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                QMC.Common.MessageDialog.Show(this, runResult.Message, "LINE MOVE TEST",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                string message = "RearPicker GOOD StageY 기준 ContiNode 이동 테스트 중 예외가 발생했습니다. error=" + ex.Message;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MOVE-TEST", "RearPickerPage", message);
                QMC.Common.MessageDialog.Show(this, message, "LINE MOVE TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnAjinLineMapTest.Enabled = true;
                btnAjinLineMoveTest.Enabled = true;
            }
        }
    }
}
