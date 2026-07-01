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
                null,
                null,
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
                    actionRightPanel.SetCellPosition(button, new TableLayoutPanelCellPosition(visionIndex % 2, 2 + visionIndex / 2));
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

        private void btnAjinLineMapTest_Click(object sender, System.EventArgs e)
        {
            btnAjinLineMapTest.Enabled = false;
            try
            {
                Form1 host = GetHost();
                if (host == null || host.Machine == null)
                {
                    QMC.Common.MessageDialog.Show(this, "장비 객체를 찾을 수 없어 Ajin 보간 맵핑 검증을 실행할 수 없습니다.", "LINE MAP TEST",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                System.Collections.Generic.List<LineMapTestResult> results = RunGoodStageYLineMapTests(host.Machine);
                int failCount = results.FindAll(x => x.Result == null || !x.Result.Success).Count;
                QMC.Common.Logging.EventKind kind = failCount == 0 ? QMC.Common.Logging.EventKind.Event : QMC.Common.Logging.EventKind.Warning;

                foreach (LineMapTestResult item in results)
                {
                    string detail = item.Name + ": " + (item.Result != null ? item.Result.ToString() : "결과 없음");
                    QMC.Common.Logging.EventLogger.Write(kind, "UI", "AJIN-LINE-MAP-TEST", "FrontPickerPage", detail);
                }

                string message = failCount == 0
                    ? "GOOD StageY 기준 Ajin 보간 맵핑 검증이 완료되었습니다. 전체 성공=" + results.Count + "건"
                    : "GOOD StageY 기준 Ajin 보간 맵핑 검증 중 실패가 있습니다. 실패=" + failCount + "건 / 전체=" + results.Count + "건";

                QMC.Common.MessageDialog.Show(this, message + "\r\n상세 내용은 Alarm/Event Log를 확인하세요.", "LINE MAP TEST",
                    MessageBoxButtons.OK, failCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (System.Exception ex)
            {
                string message = "GOOD StageY 기준 Ajin 보간 맵핑 검증 중 예외가 발생했습니다. error=" + ex.Message;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MAP-TEST", "FrontPickerPage", message);
                QMC.Common.MessageDialog.Show(this, message, "LINE MAP TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnAjinLineMapTest.Enabled = true;
            }
        }

        private static System.Collections.Generic.List<LineMapTestResult> RunGoodStageYLineMapTests(QMC.CDT320.CDT320_Machine machine)
        {
            var results = new System.Collections.Generic.List<LineMapTestResult>();
            QMC.Common.Motion.BaseAxis goodStageY = machine != null &&
                                                    machine.OutputStageUnit != null &&
                                                    machine.OutputStageUnit.GoodStage != null
                ? machine.OutputStageUnit.GoodStage.StageY
                : null;

            int goodStageYAxisNo = ResolveAxisNo(goodStageY, "OutputGoodStageY");

            AddPickerLineMapTests(
                results,
                "Front",
                goodStageYAxisNo,
                ResolveAxisNo(machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null, "FrontPickerX"),
                new[]
                {
                    ResolveAxisNo(machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerZ0 : null, "FrontPickerZ0"),
                    ResolveAxisNo(machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerZ1 : null, "FrontPickerZ1"),
                    ResolveAxisNo(machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerZ2 : null, "FrontPickerZ2"),
                    ResolveAxisNo(machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerZ3 : null, "FrontPickerZ3")
                });

            AddPickerLineMapTests(
                results,
                "Rear",
                goodStageYAxisNo,
                ResolveAxisNo(machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerX : null, "RearPickerX"),
                new[]
                {
                    ResolveAxisNo(machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerZ0 : null, "RearPickerZ0"),
                    ResolveAxisNo(machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerZ1 : null, "RearPickerZ1"),
                    ResolveAxisNo(machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerZ2 : null, "RearPickerZ2"),
                    ResolveAxisNo(machine.PickerRearUnit != null ? machine.PickerRearUnit.PickerZ3 : null, "RearPickerZ3")
                });

            return results;
        }

        private static void AddPickerLineMapTests(
            System.Collections.Generic.List<LineMapTestResult> results,
            string pickerName,
            int goodStageYAxisNo,
            int pickerXAxisNo,
            int[] pickerZAxisNos)
        {
            for (int i = 0; i < pickerZAxisNos.Length - 1; i++)
            {
                string name = pickerName + " GOOD-Y/X/Z" + i + "-Z" + (i + 1);
                int[] axes = { goodStageYAxisNo, pickerXAxisNo, pickerZAxisNos[i], pickerZAxisNos[i + 1] };
                QMC.Common.Motion.InterpolatedMotionMapResult result = QMC.Common.Motion.AjinInterpolatedMotionService.ValidateSynchronizedArrivalMap(
                    0,
                    axes,
                    false);

                results.Add(new LineMapTestResult(name, result));
            }
        }

        private static int ResolveAxisNo(QMC.Common.Motion.BaseAxis axis, string axisName)
        {
            if (axis == null)
                throw new System.InvalidOperationException(axisName + " 축 객체를 찾을 수 없습니다.");

            if (axis.Setup == null)
                throw new System.InvalidOperationException(axisName + " 축 설정을 찾을 수 없습니다.");

            if (axis.Setup.AxisNo < 0)
                throw new System.InvalidOperationException(axisName + " 축 번호가 설정되지 않았습니다. axisNo=" + axis.Setup.AxisNo);

            return axis.Setup.AxisNo;
        }

        private sealed class LineMapTestResult
        {
            public LineMapTestResult(string name, QMC.Common.Motion.InterpolatedMotionMapResult result)
            {
                Name = name;
                Result = result;
            }

            public string Name { get; private set; }
            public QMC.Common.Motion.InterpolatedMotionMapResult Result { get; private set; }
        }

        private async void btnAjinLineMoveTest_Click(object sender, System.EventArgs e)
        {
            const double TestDistance = 0.02;
            const double TestVelocity = 1.0;
            const double TestAcceleration = 10.0;
            const double TestDeceleration = 10.0;
            const int TestTimeoutMs = 5000;

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                this,
                "GOOD StageY + FrontPickerX + FrontPickerZ0 + FrontPickerZ1 보간 이동 테스트를 실행할까요?\r\n" +
                "현재 위치 기준 GoodY/PickerX/Z0는 +" + TestDistance.ToString("F3") + ", Z1은 -" + TestDistance.ToString("F3") + " 이동합니다.\r\n" +
                "성공하면 반대 방향으로 복귀합니다.\r\n" +
                "축 주변 안전 상태를 확인한 뒤 실행하세요.",
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
                    QMC.Common.MessageDialog.Show(this, "장비 객체를 찾을 수 없어 Ajin 보간 이동 테스트를 실행할 수 없습니다.", "LINE MOVE TEST",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int[] axes = ResolveGoodStageFrontPickerLineMoveAxes(host.Machine);
                double[] forward = { TestDistance, TestDistance, TestDistance, -TestDistance };
                QMC.Common.Motion.InterpolatedMotionMoveResult forwardResult =
                    await QMC.Common.Motion.AjinInterpolatedMotionService.RunSynchronizedArrivalRelativeMoveAsync(
                        0,
                        axes,
                        forward,
                        TestVelocity,
                        TestAcceleration,
                        TestDeceleration,
                        TestTimeoutMs,
                        System.Threading.CancellationToken.None).ConfigureAwait(true);

                QMC.Common.Logging.EventLogger.Write(
                    forwardResult.Success ? QMC.Common.Logging.EventKind.Event : QMC.Common.Logging.EventKind.Warning,
                    "UI",
                    "AJIN-LINE-MOVE-TEST",
                    "FrontPickerPage",
                    "정방향 보간 이동 테스트: " + forwardResult);

                if (!forwardResult.Success)
                {
                    QMC.Common.MessageDialog.Show(this, "정방향 보간 이동 테스트 실패.\r\n" + forwardResult.Message, "LINE MOVE TEST",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double[] reverse = { -TestDistance, -TestDistance, -TestDistance, TestDistance };
                QMC.Common.Motion.InterpolatedMotionMoveResult reverseResult =
                    await QMC.Common.Motion.AjinInterpolatedMotionService.RunSynchronizedArrivalRelativeMoveAsync(
                        0,
                        axes,
                        reverse,
                        TestVelocity,
                        TestAcceleration,
                        TestDeceleration,
                        TestTimeoutMs,
                        System.Threading.CancellationToken.None).ConfigureAwait(true);

                QMC.Common.Logging.EventLogger.Write(
                    reverseResult.Success ? QMC.Common.Logging.EventKind.Event : QMC.Common.Logging.EventKind.Warning,
                    "UI",
                    "AJIN-LINE-MOVE-TEST",
                    "FrontPickerPage",
                    "복귀 보간 이동 테스트: " + reverseResult);

                if (!reverseResult.Success)
                {
                    QMC.Common.MessageDialog.Show(this, "복귀 보간 이동 테스트 실패.\r\n" + reverseResult.Message, "LINE MOVE TEST",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                QMC.Common.MessageDialog.Show(this, "GOOD StageY 기준 Ajin 보간 이동 테스트가 완료되었습니다.\r\n상세 내용은 Alarm/Event Log를 확인하세요.", "LINE MOVE TEST",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (System.Exception ex)
            {
                string message = "GOOD StageY 기준 Ajin 보간 이동 테스트 중 예외가 발생했습니다. error=" + ex.Message;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MOVE-TEST", "FrontPickerPage", message);
                QMC.Common.MessageDialog.Show(this, message, "LINE MOVE TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnAjinLineMapTest.Enabled = true;
                btnAjinLineMoveTest.Enabled = true;
            }
        }

        private static int[] ResolveGoodStageFrontPickerLineMoveAxes(QMC.CDT320.CDT320_Machine machine)
        {
            return new[]
            {
                ResolveAxisNo(machine.OutputStageUnit != null && machine.OutputStageUnit.GoodStage != null ? machine.OutputStageUnit.GoodStage.StageY : null, "OutputGoodStageY"),
                ResolveAxisNo(machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null, "FrontPickerX"),
                ResolveAxisNo(machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerZ0 : null, "FrontPickerZ0"),
                ResolveAxisNo(machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerZ1 : null, "FrontPickerZ1")
            };
        }
    }
}
