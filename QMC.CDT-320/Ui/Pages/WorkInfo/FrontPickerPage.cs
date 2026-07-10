using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.Sequencing;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;
using QMC.Common.Motion;
using QMC.Common.Motion.Ajin;

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

        private void lblHead1Value_Click(object sender, EventArgs e)
        {
            _runtime.ShowHeadDieDialog(1);
        }

        private void lblHead2Value_Click(object sender, EventArgs e)
        {
            _runtime.ShowHeadDieDialog(2);
        }

        private void lblHead3Value_Click(object sender, EventArgs e)
        {
            _runtime.ShowHeadDieDialog(3);
        }

        private void lblHead4Value_Click(object sender, EventArgs e)
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
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MAP-TEST", "FrontPickerPage", readyReason);
                    QMC.Common.MessageDialog.Show(this, readyReason, "LINE MAP TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                List<PickerContiLineMapTestResult> results =
                    PickerContiLineTestRunner.RunGoodStageYLineMapTests(host.Machine, PickerSequenceSide.Front);
                int failCount = results.FindAll(x => x.Result == null || !x.Result.Success).Count;
                QMC.Common.Logging.EventKind kind = failCount == 0 ? QMC.Common.Logging.EventKind.Event : QMC.Common.Logging.EventKind.Warning;

                foreach (PickerContiLineMapTestResult item in results)
                {
                    string detail = item.Name + ": " + (item.Result != null ? item.Result.ToString() : "결과 없음");
                    QMC.Common.Logging.EventLogger.Write(kind, "UI", "AJIN-LINE-MAP-TEST", "FrontPickerPage", detail);
                }

                string message = failCount == 0
                    ? "GOOD StageY 기준 ContiNode LineMap 검증이 완료되었습니다. 전체 성공=" + results.Count + "건"
                    : "GOOD StageY 기준 ContiNode LineMap 검증 중 실패가 있습니다. 실패=" + failCount + "건 / 전체=" + results.Count + "건";

                QMC.Common.MessageDialog.Show(this, message + "\r\n상세 내용은 Alarm/Event Log를 확인하세요.", "LINE MAP TEST",
                    MessageBoxButtons.OK, failCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                string message = "GOOD StageY 기준 ContiNode LineMap 검증 중 예외가 발생했습니다. error=" + ex.Message;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MAP-TEST", "FrontPickerPage", message);
                QMC.Common.MessageDialog.Show(this, message, "LINE MAP TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnAjinLineMapTest.Enabled = true;
            }
        }

        private static List<LineMapTestResult> RunGoodStageYLineMapTests(CDT320_Machine machine)
        {
            var results = new List<LineMapTestResult>();
            BaseAxis goodStageY = machine != null &&
                                  machine.OutputStageUnit != null &&
                                  machine.OutputStageUnit.GoodStage != null
                ? machine.OutputStageUnit.GoodStage.StageY
                : null;

            int goodStageYAxisNo = ResolveAxisNo(goodStageY, "OutputGoodStageY");
            PickerPlaceMotionConfig frontPlace = ResolveFrontPlaceConfig(machine);
            PickerPlaceMotionConfig rearPlace = ResolveRearPlaceConfig(machine);

            AddPickerLineMapTests(
                results,
                "Front",
                frontPlace,
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
                rearPlace,
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
            List<LineMapTestResult> results,
            string pickerName,
            PickerPlaceMotionConfig placeConfig,
            int goodStageYAxisNo,
            int pickerXAxisNo,
            int[] pickerZAxisNos)
        {
            if (placeConfig == null)
                placeConfig = new PickerPlaceMotionConfig();
            placeConfig.Ensure();

            int[,] pairs =
            {
                { 3, 2 },
                { 2, 1 },
                { 1, 0 }
            };

            for (int i = 0; i < pairs.GetLength(0); i++)
            {
                int previousIndex = pairs[i, 0];
                int currentIndex = pairs[i, 1];
                string name = pickerName + " GOOD-Y/X/P" + (previousIndex + 1) + "-P" + (currentIndex + 1) +
                    " Z" + previousIndex + "-Z" + currentIndex +
                    " coord=" + placeConfig.ContiCoordinate;
                int[] axes = { goodStageYAxisNo, pickerXAxisNo, pickerZAxisNos[previousIndex], pickerZAxisNos[currentIndex] };
                InterpolatedMotionMapResult result = ValidateContiLineMap(placeConfig.ContiCoordinate, axes);

                results.Add(new LineMapTestResult(name, result));
            }
        }

        private static InterpolatedMotionMapResult ValidateContiLineMap(int coordinate, int[] requestedAxes)
        {
            var result = new InterpolatedMotionMapResult
            {
                Coordinate = coordinate,
                RequestedAxes = requestedAxes != null ? requestedAxes.ToArray() : new int[0]
            };

            try
            {
                if (coordinate <= 0)
                    return MapFail(result, -1, "ContiNode coordinate는 1 이상이어야 합니다. coordinate=" + coordinate);

                if (requestedAxes == null || requestedAxes.Length < 2 || requestedAxes.Length > 4)
                    return MapFail(result, -1, "ContiNode LineMap 축 개수가 맞지 않습니다. axes=" + result.RequestedAxesText);

                if (requestedAxes.Any(x => x < 0))
                    return MapFail(result, -1, "ContiNode LineMap 축 번호에 음수가 포함되어 있습니다. axes=" + result.RequestedAxesText);

                if (requestedAxes.Distinct().Count() != requestedAxes.Length)
                    return MapFail(result, -1, "ContiNode LineMap 축 번호가 중복되었습니다. axes=" + result.RequestedAxesText);

                int[] mappedAxes = requestedAxes.OrderBy(x => x).ToArray();

                int ret = AXM.SetPathAxisMap(coordinate, mappedAxes);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 축 맵 설정 실패. coordinate=" + coordinate + ", axes=" + string.Join(",", mappedAxes));

                ret = AXM.ClearPath(coordinate);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 버퍼 초기화 실패. coordinate=" + coordinate);

                ret = AXM.SetPathAbsRelMode(coordinate, AXT_MOTION_ABSREL.POS_ABS_MODE);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 절대좌표 모드 설정 실패. coordinate=" + coordinate);

                uint mappedSize = (uint)mappedAxes.Length;
                int[] readAxes = new int[mappedAxes.Length];
                ret = AXM.GetPathAxisMap(coordinate, ref mappedSize, readAxes);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 축 맵 조회 실패. coordinate=" + coordinate);

                AXT_MOTION_ABSREL readMode = AXT_MOTION_ABSREL.POS_ABS_MODE;
                ret = AXM.GetPathAbsRelMode(coordinate, ref readMode);
                if (ret != 0)
                    return MapFail(result, ret, "ContiNode LineMap 좌표 모드 조회 실패. coordinate=" + coordinate);

                result.MappedSize = mappedSize;
                result.MappedAxes = readAxes.Take((int)mappedSize).ToArray();
                result.AbsRelMode = readMode;

                if (mappedSize != mappedAxes.Length || !result.MappedAxes.SequenceEqual(mappedAxes))
                    return MapFail(result, -1, "ContiNode LineMap 확인값이 요청값과 다릅니다. request=" + string.Join(",", mappedAxes) + ", actual=" + result.MappedAxesText);

                if (readMode != AXT_MOTION_ABSREL.POS_ABS_MODE)
                    return MapFail(result, -1, "ContiNode LineMap 좌표 모드 확인값이 ABS가 아닙니다. actual=" + readMode);

                result.ResultCode = 0;
                result.Message = "ContiNode LineMap 검증 성공.";
                return result;
            }
            catch (Exception ex)
            {
                return MapFail(result, -1, "ContiNode LineMap 검증 중 예외가 발생했습니다. error=" + ex.Message);
            }
        }

        private static InterpolatedMotionMapResult MapFail(InterpolatedMotionMapResult result, int code, string message)
        {
            result.ResultCode = code == 0 ? -1 : code;
            result.Message = message;
            return result;
        }

        private static int ResolveAxisNo(BaseAxis axis, string axisName)
        {
            if (axis == null)
                throw new InvalidOperationException(axisName + " 축 객체를 찾을 수 없습니다.");

            if (axis.Setup == null)
                throw new InvalidOperationException(axisName + " 축 설정을 찾을 수 없습니다.");

            if (axis.Setup.AxisNo < 0)
                throw new InvalidOperationException(axisName + " 축 번호가 설정되지 않았습니다. axisNo=" + axis.Setup.AxisNo);

            return axis.Setup.AxisNo;
        }

        private sealed class LineMapTestResult
        {
            public LineMapTestResult(string name, InterpolatedMotionMapResult result)
            {
                Name = name;
                Result = result;
            }

            public string Name { get; private set; }
            public InterpolatedMotionMapResult Result { get; private set; }
        }

        private async void btnAjinLineMoveTest_Click(object sender, EventArgs e)
        {
            Form1 confirmHost = GetHost();
            PickerPlaceMotionConfig placeConfig = PickerContiLineTestRunner.ResolvePlaceConfig(
                confirmHost != null ? confirmHost.Machine : null,
                PickerSequenceSide.Front);

            DialogResult confirm = QMC.Common.MessageDialog.Show(
                this,
                "FrontPicker Place teaching center ContiNode 이동 테스트를 실행할까요?\r\n" +
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
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MOVE-TEST", "FrontPickerPage", readyReason);
                    QMC.Common.MessageDialog.Show(this, readyReason, "LINE MOVE TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                PickerContiLineMoveRunResult runResult =
                    await PickerContiLineTestRunner.RunGoodStagePlaceLineMoveTestAsync(
                        host.Machine,
                        PickerSequenceSide.Front,
                        CancellationToken.None).ConfigureAwait(true);

                if (!runResult.Success)
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MOVE-TEST", "FrontPickerPage", runResult.Message);
                    QMC.Common.MessageDialog.Show(this, runResult.Message, "LINE MOVE TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                QMC.Common.MessageDialog.Show(this, runResult.Message, "LINE MOVE TEST",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                string message = "GOOD StageY 기준 ContiNode 이동 테스트 중 예외가 발생했습니다. error=" + ex.Message;
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Warning, "UI", "AJIN-LINE-MOVE-TEST", "FrontPickerPage", message);
                QMC.Common.MessageDialog.Show(this, message, "LINE MOVE TEST", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnAjinLineMapTest.Enabled = true;
                btnAjinLineMoveTest.Enabled = true;
            }
        }

        private static async Task<InterpolatedMotionMoveResult> RunContiLineMoveAsync(
            LineMoveAxisSet axes,
            PickerPlaceMotionConfig placeConfig,
            PlaceLineMoveTarget previous,
            PlaceLineMoveTarget current,
            CancellationToken ct)
        {
            if (axes == null)
                throw new InvalidOperationException("Conti LineMove 축 정보를 찾을 수 없습니다.");

            if (placeConfig == null)
                placeConfig = new PickerPlaceMotionConfig();
            placeConfig.Ensure();

            List<PickerPlaceContiNode> nodes = BuildLineMoveNodes(
                axes,
                placeConfig,
                previous,
                current);

            return await PickerPlaceContiSegmentedMotion.MoveStageYPickerXAndPickerZByNodesAsync(
                axes.StageY,
                axes.PickerX,
                axes.PreviousPickerZ,
                axes.PickerZ,
                nodes,
                placeConfig,
                ct).ConfigureAwait(true);
        }

        private static List<PickerPlaceContiNode> BuildLineMoveNodes(
            LineMoveAxisSet axes,
            PickerPlaceMotionConfig placeConfig,
            PlaceLineMoveTarget previous,
            PlaceLineMoveTarget current)
        {
            double startStageY = axes.StageY.ActualPosition;
            double startPickerX = axes.PickerX.ActualPosition;
            double startPickerZ = axes.PickerZ.ActualPosition;
            double targetStageY = current.StageY;
            double targetPickerX = current.PickerX;
            double previousZStep1 = previous.PickerZ + placeConfig.ContiZ1Step1Clearance;
            double previousZStep2 = previous.PickerZ + placeConfig.ContiZ1Step1Clearance + placeConfig.ContiZ1Step2Clearance;
            double previousZNearAvoid = ResolveNearAvoidPosition(previous.PickerZAvoid, previous.PickerZ, placeConfig.ContiNearAvoidDistance);
            double currentZNearAvoid = ResolveNearAvoidPosition(current.PickerZAvoid, current.PickerZ, placeConfig.ContiNearAvoidDistance);
            double currentZStep = current.PickerZ + placeConfig.ContiZ1Step1Clearance + placeConfig.ContiZ1Step2Clearance;
            double ratio = placeConfig.ContiXYMidRatio;

            return new List<PickerPlaceContiNode>
            {
                new PickerPlaceContiNode(0, startStageY, startPickerX, previousZStep1, startPickerZ),
                new PickerPlaceContiNode(1, startStageY, startPickerX, previousZStep2, startPickerZ),
                new PickerPlaceContiNode(2, Lerp(startStageY, targetStageY, ratio), Lerp(startPickerX, targetPickerX, ratio), previousZNearAvoid, currentZNearAvoid),
                new PickerPlaceContiNode(3, targetStageY, targetPickerX, previousZNearAvoid, currentZStep),
                new PickerPlaceContiNode(4, targetStageY, targetPickerX, previous.PickerZAvoid, current.PickerZ)
            };
        }

        private static double ResolveNearAvoidPosition(double avoidPosition, double placePosition, double distanceFromAvoid)
        {
            if (distanceFromAvoid <= 0.0)
                return avoidPosition;

            double directionToPlace = placePosition >= avoidPosition ? 1.0 : -1.0;
            return avoidPosition + (directionToPlace * distanceFromAvoid);
        }

        private static double Lerp(double start, double target, double ratio)
        {
            return start + ((target - start) * ratio);
        }

        private static PickerPlaceMotionConfig ResolveFrontPlaceConfigFromHostOrDefault(Form1 host)
        {
            return host != null && host.Machine != null
                ? ResolveFrontPlaceConfig(host.Machine)
                : new PickerPlaceMotionConfig();
        }

        private static PickerPlaceMotionConfig ResolveFrontPlaceConfig(CDT320_Machine machine)
        {
            PickerPlaceMotionConfig config = machine != null &&
                                             machine.PickerFrontUnit != null &&
                                             machine.PickerFrontUnit.Config != null
                ? machine.PickerFrontUnit.Config.Place
                : null;
            if (config == null)
                config = new PickerPlaceMotionConfig();
            config.Ensure();
            return config;
        }

        private static PickerPlaceMotionConfig ResolveRearPlaceConfig(CDT320_Machine machine)
        {
            PickerPlaceMotionConfig config = machine != null &&
                                             machine.PickerRearUnit != null &&
                                             machine.PickerRearUnit.Config != null
                ? machine.PickerRearUnit.Config.Place
                : null;
            if (config == null)
                config = new PickerPlaceMotionConfig();
            config.Ensure();
            return config;
        }

        private static PlaceLineMoveTarget[] ResolveFrontPlaceLineMoveTargets(CDT320_Machine machine)
        {
            if (machine == null || machine.PickerFrontUnit == null)
                throw new InvalidOperationException("FrontPicker unit is missing.");
            if (machine.OutputStageUnit == null || machine.OutputStageUnit.Recipe == null)
                throw new InvalidOperationException("OutputStage recipe is missing.");

            machine.OutputStageUnit.Recipe.EnsurePositionObjects();

            int[] order = { 3, 2, 1, 0 };
            var targets = new List<PlaceLineMoveTarget>();
            for (int i = 0; i < order.Length; i++)
            {
                int pickerIndex = order[i];
                PickerCalibratedZoneTarget pickerTarget =
                    CalibrationCoordinateService.ResolvePickerZoneTarget(
                        machine,
                        VisionFocusPickerSide.Front,
                        "DiePlacePosition",
                        pickerIndex,
                        null,
                        false,
                        false);

                PickerAxis zAxis = CalibrationCoordinateService.ResolvePickerZAxis(pickerIndex);
                targets.Add(new PlaceLineMoveTarget
                {
                    PickerIndex = pickerIndex,
                    PickerNo = pickerIndex + 1,
                    PickerTAxis = pickerTarget.PickerTAxis,
                    PickerZAxis = pickerTarget.PickerZAxis,
                    StageY = machine.OutputStageUnit.Recipe.GoodStageY.ProcessPosition - Math.Abs(pickerTarget.Y),
                    PickerX = pickerTarget.X,
                    PickerY = pickerTarget.Y,
                    PickerT = pickerTarget.T,
                    PickerZ = pickerTarget.Z,
                    PickerZAvoid = machine.PickerFrontUnit.GetPickerTeachingPosition(zAxis, "AvoidPosition")
                });
            }

            return targets.ToArray();
        }

        private static void ValidateFrontPlaceLineMoveTargets(PlaceLineMoveTarget[] targets)
        {
            if (targets == null || targets.Length < 2)
                throw new InvalidOperationException("Place teaching target count is invalid.");

            double pickerY = targets[0].PickerY;
            for (int i = 1; i < targets.Length; i++)
            {
                if (Math.Abs(targets[i].PickerY - pickerY) > 0.001)
                {
                    throw new InvalidOperationException(
                        "Place teaching PickerY differs by picker. Conti line move test requires same PickerY. " +
                        "picker#" + targets[0].PickerNo + "=" + pickerY.ToString("F3") +
                        ", picker#" + targets[i].PickerNo + "=" + targets[i].PickerY.ToString("F3"));
                }
            }
        }

        private static async Task<int> PrepareFrontPlaceLineMoveStartAsync(
            CDT320_Machine machine,
            PlaceLineMoveTarget[] targets,
            CancellationToken ct)
        {
            if (machine == null || machine.PickerFrontUnit == null || machine.OutputStageUnit == null)
                return -1;

            PlaceLineMoveTarget first = targets[0];

            var zAvoidTargets = new Dictionary<PickerAxis, double>();
            for (int i = 0; i < targets.Length; i++)
                zAvoidTargets[targets[i].PickerZAxis] = targets[i].PickerZAvoid;

            ct.ThrowIfCancellationRequested();
            int result = await machine.PickerFrontUnit.MovePickerAxes(
                zAvoidTargets,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;PlaceStart;PickerZone=Output;Step=SafeZ").ConfigureAwait(true);
            if (result != 0)
                return result;

            var yAvoidTarget = new Dictionary<PickerAxis, double>();
            yAvoidTarget[PickerAxis.PickerY] = machine.PickerFrontUnit.GetPickerTeachingPosition(PickerAxis.PickerY, "AvoidPosition");

            ct.ThrowIfCancellationRequested();
            result = await machine.PickerFrontUnit.MovePickerAxes(
                yAvoidTarget,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;PlaceStart;PickerZone=Output;Step=SafeY").ConfigureAwait(true);
            if (result != 0)
                return result;

            machine.OutputStageUnit.Recipe.EnsurePositionObjects();

            ct.ThrowIfCancellationRequested();
            result = await machine.OutputStageUnit.MoveStageAxis(
                BinStageAxis.GoodBinZ,
                machine.OutputStageUnit.Recipe.GoodStageZ.AvoidPosition,
                true,
                "LineMoveTest;GoodZAvoid").ConfigureAwait(true);
            if (result != 0)
                return result;

            ct.ThrowIfCancellationRequested();
            result = await machine.OutputStageUnit.MoveStageAxis(
                BinStageAxis.GoodBinY,
                first.StageY,
                true,
                "LineMoveTest;GoodYPlaceCenter").ConfigureAwait(true);
            if (result != 0)
                return result;

            var xAndTTargets = new Dictionary<PickerAxis, double>();
            xAndTTargets[PickerAxis.PickerX] = first.PickerX;
            for (int i = 0; i < targets.Length; i++)
                xAndTTargets[targets[i].PickerTAxis] = targets[i].PickerT;

            ct.ThrowIfCancellationRequested();
            result = await machine.PickerFrontUnit.MovePickerAxes(
                xAndTTargets,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;PlaceStart;PickerZone=Output;Step=XT").ConfigureAwait(true);
            if (result != 0)
                return result;

            var yPlaceTarget = new Dictionary<PickerAxis, double>();
            yPlaceTarget[PickerAxis.PickerY] = first.PickerY;

            ct.ThrowIfCancellationRequested();
            result = await machine.PickerFrontUnit.MovePickerAxes(
                yPlaceTarget,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;PlaceStart;PickerZone=Output;Step=PlaceY").ConfigureAwait(true);
            if (result != 0)
                return result;

            ct.ThrowIfCancellationRequested();
            result = await machine.OutputStageUnit.MoveNgStageToAvoidAndVerifyAsync(
                10000,
                true,
                ct).ConfigureAwait(true);
            if (result != 0)
                return result;

            if (!machine.OutputStageUnit.IsNgStageInAvoidPosition())
                return -11;

            ct.ThrowIfCancellationRequested();
            result = await machine.OutputStageUnit.MoveStageAxis(
                BinStageAxis.GoodBinZ,
                machine.OutputStageUnit.Recipe.GoodStageZ.ProcessPosition,
                true,
                "LineMoveTest;GoodZProcess").ConfigureAwait(true);
            if (result != 0)
                return result;

            var firstZTarget = new Dictionary<PickerAxis, double>();
            firstZTarget[first.PickerZAxis] = first.PickerZ;

            ct.ThrowIfCancellationRequested();
            return await machine.PickerFrontUnit.MovePickerAxes(
                firstZTarget,
                JogSpeedType.Fine,
                0.0,
                "LineMoveTest;PlaceStart;PickerZone=Output;Step=PlaceZ;Picker" + first.PickerNo).ConfigureAwait(true);
        }

        private static LineMoveAxisSet ResolveGoodStageFrontPickerLineMoveAxes(CDT320_Machine machine, int previousPickerIndex, int currentPickerIndex)
        {
            var axes = new LineMoveAxisSet
            {
                StageY = machine.OutputStageUnit != null && machine.OutputStageUnit.GoodStage != null ? machine.OutputStageUnit.GoodStage.StageY : null,
                PickerX = machine.PickerFrontUnit != null ? machine.PickerFrontUnit.PickerX : null,
                PreviousPickerZ = machine.PickerFrontUnit != null ? ResolveFrontPickerZAxis(machine, previousPickerIndex) : null,
                PickerZ = machine.PickerFrontUnit != null ? ResolveFrontPickerZAxis(machine, currentPickerIndex) : null,
                PreviousPickerIndex = previousPickerIndex,
                PickerIndex = currentPickerIndex
            };

            ResolveAxisNo(axes.StageY, "OutputGoodStageY");
            ResolveAxisNo(axes.PickerX, "FrontPickerX");
            ResolveAxisNo(axes.PreviousPickerZ, "FrontPickerZ" + previousPickerIndex);
            ResolveAxisNo(axes.PickerZ, "FrontPickerZ" + currentPickerIndex);
            return axes;
        }

        private static BaseAxis ResolveFrontPickerZAxis(CDT320_Machine machine, int pickerIndex)
        {
            if (machine == null || machine.PickerFrontUnit == null)
                return null;

            if (pickerIndex <= 0)
                return machine.PickerFrontUnit.PickerZ0;
            if (pickerIndex == 1)
                return machine.PickerFrontUnit.PickerZ1;
            if (pickerIndex == 2)
                return machine.PickerFrontUnit.PickerZ2;
            return machine.PickerFrontUnit.PickerZ3;
        }

        private sealed class PlaceLineMoveTarget
        {
            public int PickerIndex { get; set; }
            public int PickerNo { get; set; }
            public PickerAxis PickerTAxis { get; set; }
            public PickerAxis PickerZAxis { get; set; }
            public double StageY { get; set; }
            public double PickerX { get; set; }
            public double PickerY { get; set; }
            public double PickerT { get; set; }
            public double PickerZ { get; set; }
            public double PickerZAvoid { get; set; }
        }

        private sealed class LineMoveAxisSet
        {
            public BaseAxis StageY { get; set; }
            public BaseAxis PickerX { get; set; }
            public BaseAxis PreviousPickerZ { get; set; }
            public BaseAxis PickerZ { get; set; }
            public int PreviousPickerIndex { get; set; }
            public int PickerIndex { get; set; }
        }
    }
}
