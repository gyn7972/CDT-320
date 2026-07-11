using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT320.VisionComm;
using QMC.CDT_320.Ui.Dialogs;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OutputStagePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _timer;
        private bool _manualSequenceRunning;
        private SequenceStartMode _manualSequenceStartMode = SequenceStartMode.Resume;
        private BinSide _selectedMaterialSide = BinSide.Good;

        public OutputStagePage()
        {
            InitializeComponent();
            WireEvents();

            materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
            materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;

            VisionModuleTestDialog.AddLaunchers(
                actionRightPanel.Controls, this, btnStop,
                Tuple.Create<string, Func<VisionTcpClient>, string>("VISION: BIN", () => VisionHub.Bin, "Bin Vision"));

            // STOP/비전 런처를 그리드 셀에 통일 배치: [빈칸][STOP] / [VISION][빈칸]
            int visionIndex = 0;
            foreach (Control control in actionRightPanel.Controls)
            {
                if (!(control is ActionButton button))
                    continue;

                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(3);

                if (ReferenceEquals(button, btnStop))
                {
                    actionRightPanel.SetCellPosition(button, new TableLayoutPanelCellPosition(1, 0));
                }
                else
                {
                    button.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
                    actionRightPanel.SetCellPosition(button, new TableLayoutPanelCellPosition(0, 1 + visionIndex));
                    visionIndex++;
                }
            }

            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                    return;

                RefreshData();
            };
            VisibleChanged += (s, e) => { if (Visible) _timer.Start(); else _timer.Stop(); };
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost()
        {
            return FindForm() as Form1;
        }

        private void WireEvents()
        {
            btnStageReady.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE GOOD LOAD",
                host => RunPrepareLoadAsync(host, BinSide.Good));
            btnNgStageReady.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE NG LOAD",
                host => RunPrepareLoadAsync(host, BinSide.Ng));
            btnGoodProcess.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE GOOD PROCESS",
                host => RunMoveProcessAsync(host, BinSide.Good));
            btnNgProcess.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE NG PROCESS",
                host => RunMoveProcessAsync(host, BinSide.Ng));
            btnGoodReceive.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE RECEIVE GOOD",
                host => RunReceiveDieAsync(host, DieGrade.Good));
            btnNgReceive.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE RECEIVE NG",
                host => RunReceiveDieAsync(host, DieGrade.Ng));
            btnGoodUnload.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE GOOD UNLOAD",
                host => RunPrepareUnloadAsync(host, BinSide.Good));
            btnNgUnload.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE NG UNLOAD",
                host => RunPrepareUnloadAsync(host, BinSide.Ng));
            btnInspect.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE INSPECT",
                RunInspectBinAsync);
            btnStageInit.Click += async (s, e) => await RunSequenceAction(
                "OUTPUT STAGE AVOID",
                RunMoveAvoidAsync);
            btnStop.Click += async (s, e) => await StopManualActionAsync();
        }

        private void rdoGoodMaterial_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdoGoodMaterial.Checked)
                return;

            _selectedMaterialSide = BinSide.Good;
            RefreshData();
        }

        private void rdoNgMaterial_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdoNgMaterial.Checked)
                return;

            _selectedMaterialSide = BinSide.Ng;
            RefreshData();
        }

        private async Task RunSequenceAction(string actionName, Func<Form1, Task<bool>> action)
        {
            IDisposable actionScope = null;
            bool showFailure = false;
            string exceptionMessage = null;
            try
            {
                Form1 host = GetHost();
                if (host == null || host.Controller == null || host.Machine == null || action == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (!ConfirmAction(actionName))
                    return;
                if (!TryAskManualSequenceStartMode(actionName, out _manualSequenceStartMode))
                    return;

                _manualSequenceRunning = true;
                SetSequenceButtonsEnabled(false);
                actionScope = host.Controller.BeginManualActionScope(ManualMotionScopeKind.ProcessSequence, "OutputStagePage:" + actionName);
                CancellationToken manualToken = host.Controller.ManualOperationToken;
                SequenceFailureStore.Clear();
                WriteEvent("OUTPUT-STAGE-ACTION", actionName + " start");

                Task<bool> actionTask = action(host);
                Task cancelTask = WaitForCancellationAsync(manualToken);
                Task completed = await Task.WhenAny(actionTask, cancelTask).ConfigureAwait(true);
                if (completed == cancelTask)
                {
                    ObserveManualActionTask(actionTask, actionName);
                    WriteEvent("OUTPUT-STAGE-CANCEL", actionName + " canceled by stop.");
                    return;
                }

                bool ok = await actionTask.ConfigureAwait(true);
                WriteEvent("OUTPUT-STAGE-ACTION", actionName + " result=" + ok);
                if (!ok)
                {
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
                WriteEvent("OUTPUT-STAGE-CANCEL", actionName + " canceled.");
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-STAGE-ACTION-EX", actionName + " failed: " + ex.Message);
                exceptionMessage = ex.Message;
            }
            finally
            {
                try
                {
                    if (actionScope != null)
                        actionScope.Dispose();
                }
                catch (Exception ex)
                {
                    WriteAlarm("OUTPUT-STAGE-MANUAL-CLEANUP", "Output Stage 수동 시컨스 정리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    try { SetSequenceButtonsEnabled(true); } catch (Exception ex) { WriteAlarm("OUTPUT-STAGE-BUTTON-RESTORE", "Output Stage 버튼 복구 실패: " + ex.Message); }
                    try { RefreshData(); } catch (Exception ex) { WriteAlarm("OUTPUT-STAGE-REFRESH", "Output Stage 화면 갱신 실패: " + ex.Message); }
                    try { BeginRestoreSequenceButtons(); } catch (Exception ex) { WriteAlarm("OUTPUT-STAGE-BUTTON-RESTORE-DELAY", "Output Stage 버튼 지연 복구 실패: " + ex.Message); }
                }
            }

            if (showFailure)
            {
                string message = SequenceFailureStore.BuildManualFailureMessage(
                    actionName,
                    actionName + " 실패\r\nAlarm/Event Log를 확인하세요.");
                QMC.Common.MessageDialog.Show(this, message, "Output Stage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, exceptionMessage, "Output Stage", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static async Task WaitForCancellationAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
            {
                await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
                return;
            }

            if (ct.IsCancellationRequested)
                return;

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(() => tcs.TrySetResult(0)))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }

        private void ObserveManualActionTask(Task<bool> task, string actionName)
        {
            if (task == null)
                return;

            _ = ObserveManualActionTaskAsync(task, actionName);
        }

        private async Task ObserveManualActionTaskAsync(Task<bool> task, string actionName)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                WriteEvent("OUTPUT-STAGE-ACTION-LATE-CANCEL", actionName + " 정지 후 취소 완료.");
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-STAGE-ACTION-LATE-EX", actionName + " 정지 후 종료 처리 중 오류: " + ex.Message);
            }
            finally
            {
            }
        }

        private bool ConfirmAction(string actionName)
        {
            return QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Output Stage", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void SetSequenceButtonsEnabled(bool enabled)
        {
            if (actionPanel != null)
                actionPanel.Enabled = true;

            foreach (Control control in actionPanel.Controls)
            {
                if (!ReferenceEquals(control, btnStop))
                    control.Enabled = enabled;
            }

            if (btnStop != null)
                btnStop.Enabled = true;
        }

        private void BeginRestoreSequenceButtons()
        {
            try
            {
                if (!IsHandleCreated)
                    return;

                BeginInvoke((Action)(() =>
                {
                    _manualSequenceRunning = false;
                    SetSequenceButtonsEnabled(true);
                    RefreshData();
                }));
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-STAGE-BUTTON-RESTORE-EX", "Manual action button restore failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task StopManualActionAsync()
        {
            try
            {
                Form1 host = GetHost();
                if (host == null || host.Controller == null)
                    return;

                WriteEvent("OUTPUT-STAGE-STOP", "Manual action stop requested.");
                await host.Controller.StopAsync();
                _manualSequenceRunning = false;
                SetSequenceButtonsEnabled(true);
                RefreshData();
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-STAGE-STOP-EX", "Manual action stop failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<bool> RunPrepareLoadAsync(Form1 host, BinSide side)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int readyResult = await RunReadyBeforeOutputStageActionAsync(host, ct, "LOAD " + side).ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            int stageResult = await CreateSequence(host).RunPrepareLoadAsync(ct, BuildOptions(side, ResolveGrade(side))).ConfigureAwait(true);
            if (stageResult != 0)
                return false;

            int feederResult = await MoveOutputFeederToStageAndLiftUpAsync(host, ct, side, true, "LOAD " + side).ConfigureAwait(true);
            return feederResult == 0;
        }

        private async Task<bool> RunMoveProcessAsync(Form1 host, BinSide side)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int readyResult = await RunReadyBeforeOutputStageActionAsync(host, ct, "PROCESS " + side).ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            return await CreateSequence(host).RunMoveProcessAsync(ct, BuildOptions(side, ResolveGrade(side))).ConfigureAwait(true) == 0;
        }

        private async Task<bool> RunReceiveDieAsync(Form1 host, DieGrade grade)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int readyResult = await RunReadyBeforeOutputStageActionAsync(host, ct, "RECEIVE " + grade).ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            return await CreateSequence(host).RunReceiveDieAsync(ct, BuildOptions(ResolveSide(grade), grade)).ConfigureAwait(true) == 0;
        }

        private async Task<bool> RunPrepareUnloadAsync(Form1 host, BinSide side)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int readyResult = await RunReadyBeforeOutputStageActionAsync(host, ct, "UNLOAD " + side).ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            int stageResult = await CreateSequence(host).RunPrepareUnloadAsync(ct, BuildOptions(side, ResolveGrade(side))).ConfigureAwait(true);
            if (stageResult != 0)
                return false;

            int feederResult = await MoveOutputFeederToStageAndLiftUpAsync(host, ct, side, false, "UNLOAD " + side).ConfigureAwait(true);
            return feederResult == 0;
        }

        private async Task<bool> RunInspectBinAsync(Form1 host)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int readyResult = await RunReadyBeforeOutputStageActionAsync(host, ct, "INSPECT").ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            return await CreateSequence(host).RunInspectBinAsync(ct, BuildOptions(BinSide.Good, DieGrade.Good)).ConfigureAwait(true) == 0;
        }

        private async Task<bool> RunMoveAvoidAsync(Form1 host)
        {
            CancellationToken ct = host.Controller.ManualOperationToken;

            int readyResult = await RunReadyBeforeOutputStageActionAsync(host, ct, "AVOID").ConfigureAwait(true);
            if (readyResult != 0)
                return false;

            return await CreateSequence(host).RunMoveAvoidAsync(ct, BuildOptions(BinSide.Good, DieGrade.Good)).ConfigureAwait(true) == 0;
        }

        private OutputStageSequence CreateSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new OutputStageSequence(ctx);
        }

        private async Task<int> RunReadyBeforeOutputStageActionAsync(Form1 host, CancellationToken ct, string actionName)
        {
            try
            {
                if (host == null || host.Machine == null)
                {
                    WriteAlarm("OUTPUT-STAGE-READY-NO-MACHINE", actionName + " 전 Ready 시컨스를 실행할 장비 객체가 없습니다.");
                    return -1;
                }

                int feederResult = await NormalizeOutputFeederBeforeReadyAsync(host, ct, actionName).ConfigureAwait(true);
                if (feederResult != 0)
                    return feederResult;

                WriteEvent("OUTPUT-STAGE-READY-START", actionName + " 전 Ready 시컨스 시작.");
                var readySequence = new MachineReadySequence(host.Machine);
                int result = await readySequence.RunAsync(ct).ConfigureAwait(true);
                if (result != 0)
                {
                    string reason = string.IsNullOrWhiteSpace(readySequence.LastErrorMessage)
                        ? "result=" + result
                        : readySequence.LastErrorMessage;
                    WriteAlarm("OUTPUT-STAGE-READY-FAIL", actionName + " 전 Ready 시컨스 실패: " + reason);
                    return result;
                }

                WriteEvent("OUTPUT-STAGE-READY-OK", actionName + " 전 Ready 시컨스 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteEvent("OUTPUT-STAGE-READY-CANCEL", actionName + " 전 Ready 시컨스가 정지 요청으로 중단되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-STAGE-READY-EX", actionName + " 전 Ready 시컨스 예외: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> NormalizeOutputFeederBeforeReadyAsync(Form1 host, CancellationToken ct, string actionName)
        {
            try
            {
                OutputFeederUnit feeder = host != null && host.Machine != null ? host.Machine.OutputFeederUnit : null;
                if (feeder == null)
                    return 0;

                if (feeder.IsBinFeederInAvoidPosition() && feeder.IsFeederDown())
                    return 0;

                if (feeder.IsFeederUp())
                {
                    if (!IsOutputFeederAtStageTransferPosition(feeder))
                    {
                        WriteAlarm("OUTPUT-STAGE-READY-FEEDER-UP-POS", actionName + " 전 Ready 준비 차단: OutputFeeder가 Up 상태인데 Stage Load/Unload 계열 위치가 아닙니다. " + feeder.DescribeFeederCylinderState() + ", " + feeder.DescribeBinFeederYMoveDoneState());
                        return -1;
                    }

                    int downTimeoutMs = ResolveOutputFeederLiftTimeoutMs(feeder, false);
                    WriteEvent("OUTPUT-STAGE-READY-FEEDER-DOWN", actionName + " 전 OutputFeeder Lift Down 시작. timeoutMs=" + downTimeoutMs);
                    int downResult = await feeder.SetFeederUpDownAsync(false, downTimeoutMs, ct).ConfigureAwait(true);
                    if (downResult != 0 || !feeder.IsFeederDown())
                    {
                        WriteAlarm("OUTPUT-STAGE-READY-FEEDER-DOWN-FAIL", actionName + " 전 OutputFeeder Lift Down 실패. result=" + downResult + ", " + feeder.DescribeFeederCylinderState());
                        return downResult != 0 ? downResult : -1;
                    }
                }

                if (!feeder.IsBinFeederInAvoidPosition())
                {
                    string readyReason;
                    if (!feeder.CheckBinFeederYMoveReady(out readyReason))
                    {
                        WriteAlarm("OUTPUT-STAGE-READY-FEEDER-Y-READY", actionName + " 전 OutputFeederY Avoid 이동 차단: " + readyReason);
                        return -1;
                    }

                    WriteEvent("OUTPUT-STAGE-READY-FEEDER-Y-AVOID", actionName + " 전 OutputFeederY Avoid 이동 시작.");
                    int moveResult = await feeder.MoveToFeederAvoidPosition(false).ConfigureAwait(true);
                    if (moveResult != 0)
                    {
                        WriteAlarm("OUTPUT-STAGE-READY-FEEDER-Y-AVOID-FAIL", actionName + " 전 OutputFeederY Avoid 이동 실패. result=" + moveResult + ", " + feeder.DescribeBinFeederYMoveDoneState() + feeder.DescribeBinFeederYLastMotionFailure());
                        return moveResult;
                    }

                    AxisMoveWaitResult waitResult = await feeder.WaitBinFeederYMoveDoneInPosition(
                        feeder.Recipe.AvoidPosition,
                        ResolveOutputFeederMoveTimeoutMs(feeder),
                        ct).ConfigureAwait(true);
                    if (waitResult == null || !waitResult.Success || !feeder.IsBinFeederInAvoidPosition())
                    {
                        WriteAlarm("OUTPUT-STAGE-READY-FEEDER-Y-AVOID-WAIT", actionName + " 전 OutputFeederY Avoid 위치 확인 실패. " + AxisMoveWaiter.FormatResult(waitResult, feeder.DescribeBinFeederYMoveDoneState()));
                        return -1;
                    }
                }

                if (!feeder.IsFeederDown())
                {
                    WriteAlarm("OUTPUT-STAGE-READY-FEEDER-DOWN-CHECK", actionName + " 전 Ready 준비 실패: OutputFeeder가 Down 상태가 아닙니다. " + feeder.DescribeFeederCylinderState());
                    return -1;
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteEvent("OUTPUT-STAGE-READY-FEEDER-CANCEL", actionName + " 전 OutputFeeder Ready 준비가 정지 요청으로 중단되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-STAGE-READY-FEEDER-EX", actionName + " 전 OutputFeeder Ready 준비 예외: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private static bool IsOutputFeederAtStageTransferPosition(OutputFeederUnit feeder)
        {
            if (feeder == null)
                return false;

            return feeder.IsBinFeederInAvoidPosition() ||
                   feeder.IsBinFeederYInStageLoadPosition(BinSide.Good) ||
                   feeder.IsBinFeederYInStageLoadPosition(BinSide.Ng) ||
                   feeder.IsBinFeederYInStageLoadAvoidPosition(BinSide.Good) ||
                   feeder.IsBinFeederYInStageLoadAvoidPosition(BinSide.Ng) ||
                   feeder.IsBinFeederYInStageUnloadPosition(BinSide.Good) ||
                   feeder.IsBinFeederYInStageUnloadPosition(BinSide.Ng) ||
                   feeder.IsBinFeederYInStageUnloadAvoidPosition(BinSide.Good) ||
                   feeder.IsBinFeederYInStageUnloadAvoidPosition(BinSide.Ng);
        }

        private async Task<int> MoveOutputFeederToStageAndLiftUpAsync(Form1 host, CancellationToken ct, BinSide side, bool load, string actionName)
        {
            try
            {
                OutputStageUnit stage = host != null && host.Machine != null ? host.Machine.OutputStageUnit : null;
                OutputFeederUnit feeder = host != null && host.Machine != null ? host.Machine.OutputFeederUnit : null;
                if (stage == null)
                {
                    WriteAlarm("OUTPUT-STAGE-FEEDER-UP-NO-STAGE", actionName + " 마지막 Feeder Up 실패: OutputStage 유닛을 찾을 수 없습니다.");
                    return -1;
                }

                if (feeder == null)
                {
                    WriteAlarm("OUTPUT-STAGE-FEEDER-UP-NO-FEEDER", actionName + " 마지막 Feeder Up 실패: OutputFeeder 유닛을 찾을 수 없습니다.");
                    return -1;
                }

                bool stageReady = load ? stage.IsStageInLoadPosition(side) : stage.IsStageInUnloadPosition(side);
                if (!stageReady)
                {
                    WriteAlarm("OUTPUT-STAGE-FEEDER-UP-STAGE-POS", actionName + " 마지막 Feeder Up 차단: OutputStage가 " + (load ? "Load" : "Unload") + " 위치가 아닙니다. side=" + side + ", " + (load ? stage.DescribeStageLoadMoveState(side) : stage.DescribeOutputStageInterlockState(side)));
                    return -1;
                }

                Func<OutputFeederUnit, bool> isTargetPosition = load
                    ? new Func<OutputFeederUnit, bool>(f => f.IsBinFeederYInStageLoadPosition(side))
                    : new Func<OutputFeederUnit, bool>(f => f.IsBinFeederYInStageUnloadPosition(side));

                if (feeder.IsFeederUp())
                {
                    if (!isTargetPosition(feeder))
                    {
                        WriteAlarm("OUTPUT-STAGE-FEEDER-UP-Y", actionName + " 마지막 Feeder Up 차단: 이미 Up 상태이지만 OutputFeederY가 Stage " + (load ? "Load" : "Unload") + " 위치가 아닙니다. side=" + side + ", " + feeder.DescribeBinFeederYMoveDoneState());
                        return -1;
                    }

                    WriteEvent("OUTPUT-STAGE-FEEDER-UP-SKIP", actionName + " 마지막 Feeder Up 생략: 이미 Up 상태입니다.");
                    return 0;
                }

                if (!feeder.IsFeederDown())
                {
                    int downTimeoutMs = ResolveOutputFeederLiftTimeoutMs(feeder, false);
                    WriteEvent("OUTPUT-STAGE-FEEDER-DOWN", actionName + " 마지막 FeederY 이동 전 Lift Down 시작. timeoutMs=" + downTimeoutMs);
                    int downResult = await feeder.SetFeederUpDownAsync(false, downTimeoutMs, ct).ConfigureAwait(true);
                    if (downResult != 0 || !feeder.IsFeederDown())
                    {
                        WriteAlarm("OUTPUT-STAGE-FEEDER-DOWN-FAIL", actionName + " 마지막 FeederY 이동 전 Lift Down 실패. result=" + downResult + ", " + feeder.DescribeFeederCylinderState());
                        return downResult != 0 ? downResult : -1;
                    }
                }

                string readyReason;
                if (!feeder.CheckBinFeederYMoveReady(out readyReason))
                {
                    WriteAlarm("OUTPUT-STAGE-FEEDER-Y-READY", actionName + " 마지막 FeederY 이동 차단: " + readyReason);
                    return -1;
                }

                WriteEvent("OUTPUT-STAGE-FEEDER-Y-MOVE", actionName + " 마지막 OutputFeederY Stage " + (load ? "Load" : "Unload") + " 위치 이동 시작. side=" + side);
                int moveResult = load
                    ? await feeder.MoveToFeederStageLoadPosition(side, false).ConfigureAwait(true)
                    : await feeder.MoveToFeederStageUnloadPosition(side, false).ConfigureAwait(true);
                if (moveResult != 0)
                {
                    WriteAlarm("OUTPUT-STAGE-FEEDER-Y-MOVE-FAIL", actionName + " 마지막 OutputFeederY Stage " + (load ? "Load" : "Unload") + " 위치 이동 실패. result=" + moveResult + ", side=" + side + ", " + feeder.DescribeBinFeederYMoveDoneState() + feeder.DescribeBinFeederYLastMotionFailure());
                    return moveResult;
                }

                if (!isTargetPosition(feeder))
                {
                    WriteAlarm("OUTPUT-STAGE-FEEDER-Y-CHECK", actionName + " 마지막 OutputFeederY Stage " + (load ? "Load" : "Unload") + " 위치 확인 실패. side=" + side + ", " + feeder.DescribeBinFeederYMoveDoneState());
                    return -1;
                }

                int upTimeoutMs = ResolveOutputFeederLiftTimeoutMs(feeder, true);
                WriteEvent("OUTPUT-STAGE-FEEDER-UP-START", actionName + " 마지막 Feeder Up 시작. timeoutMs=" + upTimeoutMs);
                int upResult = await feeder.SetFeederUpDownAsync(true, upTimeoutMs, ct).ConfigureAwait(true);
                if (upResult != 0 || !feeder.IsFeederUp())
                {
                    WriteAlarm("OUTPUT-STAGE-FEEDER-UP-FAIL", actionName + " 마지막 Feeder Up 실패. result=" + upResult + ", " + feeder.DescribeFeederCylinderState());
                    return upResult != 0 ? upResult : -1;
                }

                WriteEvent("OUTPUT-STAGE-FEEDER-UP-OK", actionName + " 마지막 Feeder Up 완료.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                WriteEvent("OUTPUT-STAGE-FEEDER-UP-CANCEL", actionName + " 마지막 Feeder Up이 정지 요청으로 중단되었습니다.");
                throw;
            }
            catch (Exception ex)
            {
                WriteAlarm("OUTPUT-STAGE-FEEDER-UP-EX", actionName + " 마지막 Feeder Up 예외: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private static int ResolveOutputFeederLiftTimeoutMs(OutputFeederUnit feeder, bool up)
        {
            try
            {
                if (feeder != null && feeder.FeederUpDownCyl != null && feeder.FeederUpDownCyl.Recipe != null)
                {
                    int timeoutMs = up ? feeder.FeederUpDownCyl.Recipe.FwdTimeoutMs : feeder.FeederUpDownCyl.Recipe.BwdTimeoutMs;
                    if (timeoutMs > 0)
                        return timeoutMs;
                }
            }
            catch
            {
            }
            finally
            {
            }

            return 3000;
        }

        private static int ResolveOutputFeederMoveTimeoutMs(OutputFeederUnit feeder)
        {
            try
            {
                if (feeder != null && feeder.FeederY != null && feeder.FeederY.Setup != null && feeder.FeederY.Setup.MoveTimeoutMs > 0)
                    return feeder.FeederY.Setup.MoveTimeoutMs;

                return 60000;
            }
            catch
            {
                return 60000;
            }
            finally
            {
            }
        }

        private OutputStageSequenceOptions BuildOptions(BinSide side, DieGrade grade)
        {
            OutputStageSequenceOptions options = OutputStageSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = _manualSequenceStartMode;
            options.Side = side;
            options.Grade = grade;
            options.FineMove = false;
            return options;
        }

        private static DieGrade ResolveGrade(BinSide side)
        {
            return side == BinSide.Ng ? DieGrade.Ng : DieGrade.Good;
        }

        private static BinSide ResolveSide(DieGrade grade)
        {
            return grade == DieGrade.Ng ? BinSide.Ng : BinSide.Good;
        }

        private static void WriteEvent(string code, string message)
        {
            EventLogger.Write(EventKind.Event, "QMC", code, message);
        }

        private static void WriteAlarm(string code, string message)
        {
            EventLogger.Write(EventKind.Alarm, "QMC", code, message);
        }

        private void RefreshData()
        {
            try
            {
                var host = GetHost();
                var stage = host != null && host.Machine != null ? host.Machine.OutputStageUnit : null;

                if (stage != null && stage.GoodStage != null && stage.GoodStage.StageZ != null)
                    lblGoodZValue.Text = AxisUnitConverter.FormatDisplay(stage.GoodStage.StageZ.ActualPosition, stage.GoodStage.StageZ, "0.###", true);
                if (stage != null && stage.GoodStage != null && stage.GoodStage.StageY != null)
                    lblGoodYValue.Text = AxisUnitConverter.FormatDisplay(stage.GoodStage.StageY.ActualPosition, stage.GoodStage.StageY, "0.###", true);
                if (stage != null && stage.NgStage != null && stage.NgStage.StageY != null)
                    lblNgYValue.Text = AxisUnitConverter.FormatDisplay(stage.NgStage.StageY.ActualPosition, stage.NgStage.StageY, "0.###", true);
                if (stage != null && stage.OutputCameraX != null)
                    lblVisionXValue.Text = AxisUnitConverter.FormatDisplay(stage.OutputCameraX.ActualPosition, stage.OutputCameraX, "0.###", true);

                lblGoodGuideValue.Text = ResolveUpDownState(
                    stage != null && stage.IsBinGuideUp(BinSide.Good),
                    stage != null && stage.IsBinGuideDown(BinSide.Good));
                lblGoodClampValue.Text = ResolveUpDownState(
                    stage != null && stage.IsBinGuideClampLiftUp(BinSide.Good),
                    stage != null && stage.IsBinGuideClampLiftDown(BinSide.Good));
                lblGoodClampStateValue.Text = ResolveClampState(
                    stage != null && stage.IsBinGuideClamped(BinSide.Good),
                    stage != null && stage.IsBinGuideUnclamped(BinSide.Good));
                lblNgGuideValue.Text = ResolveUpDownState(
                    stage != null && stage.IsBinGuideUp(BinSide.Ng),
                    stage != null && stage.IsBinGuideDown(BinSide.Ng));
                lblNgClampValue.Text = ResolveUpDownState(
                    stage != null && stage.IsBinGuideClampLiftUp(BinSide.Ng),
                    stage != null && stage.IsBinGuideClampLiftDown(BinSide.Ng));
                lblNgClampStateValue.Text = ResolveClampState(
                    stage != null && stage.IsBinGuideClamped(BinSide.Ng),
                    stage != null && stage.IsBinGuideUnclamped(BinSide.Ng));

                WaferMaterial good = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageGood);
                WaferMaterial ng = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg);

                lblGoodExistValue.Text = good != null ? "BIN" : "EMPTY";
                lblGoodStateValue.Text = good != null ? WaferMaterialStateText.ToDisplayName(good.State) : "INCOMPLETE";
                lblNgExistValue.Text = ng != null ? "BIN" : "EMPTY";
                lblNgStateValue.Text = ng != null ? WaferMaterialStateText.ToDisplayName(ng.State) : "INCOMPLETE";
                lblGoodCountValue.Text = good != null ? "1 ea" : "0 ea";
                lblNgCountValue.Text = ng != null ? "1 ea" : "0 ea";
                lblTotalCountValue.Text = ((good != null ? 1 : 0) + (ng != null ? 1 : 0)) + " ea";

                WaferMaterial wafer = _selectedMaterialSide == BinSide.Ng ? ng : good;
                string side = _selectedMaterialSide == BinSide.Ng ? "NG" : "Good";
                string title = _selectedMaterialSide == BinSide.Ng ? "OUTPUT STAGE NG MATERIAL" : "OUTPUT STAGE GOOD MATERIAL";
                materialDetailView.SetRows(title, BuildStageMaterialRows(wafer, side));
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void MaterialDetailView_CreateDataRequested_Legacy(object sender, EventArgs e)
        {
            try
            {
                if (QMC.Common.MessageDialog.Show(this, "Output Stage Good 위치에 Material Data를 생성하시겠습니까?", "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                MaterialStateService.CreateWaferAtLocation(
                    MaterialLocationKind.OutputStageGood,
                    "OUTPUT-STAGE-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    WaferMaterialState.WorkReady);
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Output Stage Material Data 생성 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_ClearDataRequested_Legacy(object sender, EventArgs e)
        {
            try
            {
                if (QMC.Common.MessageDialog.Show(this, "Output Stage의 Material Data를 초기화하시겠습니까?", "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                MaterialStateService.ClearWaferAtLocation(MaterialLocationKind.OutputStageGood);
                MaterialStateService.ClearWaferAtLocation(MaterialLocationKind.OutputStageNg);
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Output Stage Material Data 초기화 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                string sideName = _selectedMaterialSide == BinSide.Ng ? "NG" : "Good";
                if (QMC.Common.MessageDialog.Show(this, "Output Stage " + sideName + " 위치에 Wafer Data를 새로 생성하시겠습니까?", "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                string message;
                bool created = MaterialStateService.CreateProcessTestOutputStageWafer(_selectedMaterialSide, out message);
                if (!created)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "OutputStagePage",
                        "Output Stage 공정 테스트 Wafer Data 생성 실패. side=" + sideName + ", message=" + message + " - Failed");
                    QMC.Common.MessageDialog.Show(this,
                        "Output Stage Wafer Data 생성에 실패했습니다.\r\n" + message,
                        "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    RefreshData();
                    return;
                }

                QMC.Common.Log.Write("Main", "SYSTEM", "OutputStagePage",
                    "Output Stage 공정 테스트 Wafer Data를 새로 생성했습니다. side=" + sideName + ", message=" + message + " - Ok");

                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Output Stage Material Data 생성 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_ClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                string sideName = _selectedMaterialSide == BinSide.Ng ? "NG" : "Good";
                if (QMC.Common.MessageDialog.Show(this, "Output Stage " + sideName + " Material Data를 초기화하시겠습니까?", "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                MaterialStateService.ClearWaferAtLocation(ResolveMaterialLocation(_selectedMaterialSide));
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Output Stage Material Data 초기화 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static IEnumerable<MaterialDetailRow> BuildStageMaterialRows(WaferMaterial wafer, string stageSide)
        {
            int receivedCount = wafer != null && wafer.DieIds != null ? wafer.DieIds.Count : 0;
            int totalCount = wafer != null ? wafer.OutputReceiveTotalCount : 0;
            int nextIndex = wafer != null ? wafer.OutputReceiveNextIndex : 0;
            int slotCount = wafer != null && wafer.OutputReceiveSlots != null ? wafer.OutputReceiveSlots.Count : 0;

            return new[]
            {
                Row("Unit", "Output Stage"),
                Row("Side", stageSide),
                Row("Wafer ID", wafer != null ? wafer.WaferId : ""),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : "EMPTY"),
                Row("Grade", wafer != null ? wafer.OutputGrade.ToString() : ""),
                Row("DieMap ObjId", wafer != null ? wafer.DieMapFrameObjId : ""),
                Row("DieMap Grid", wafer != null ? wafer.OutputReceiveDieMapX + " x " + wafer.OutputReceiveDieMapY : ""),
                Row("DieMap Slots", wafer != null ? slotCount.ToString() : ""),
                Row("Receive Progress", wafer != null ? receivedCount + " / " + totalCount : ""),
                Row("Next Receive Index", wafer != null ? nextIndex.ToString() : ""),
                Row("Source Wafer", wafer != null ? wafer.OutputReceiveSourceWaferId : ""),
                Row("Source Cassette", wafer != null ? wafer.SourceCassetteRole.ToString() : ""),
                Row("Source Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : ""),
                Row("Current Loc", wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : ""),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : ""),
                Row("TapeFrame Spec", wafer != null ? wafer.TapeFrameSpecName : ""),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "")
            };
        }

        private static MaterialDetailRow Row(string name, string value)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = string.IsNullOrWhiteSpace(value) ? "-" : value,
                Editable = false
            };
        }

        private static MaterialLocationKind ResolveMaterialLocation(BinSide side)
        {
            return side == BinSide.Ng
                ? MaterialLocationKind.OutputStageNg
                : MaterialLocationKind.OutputStageGood;
        }

        private static string ResolveUpDownState(bool isUp, bool isDown)
        {
            if (isUp && !isDown)
                return "UP";
            if (!isUp && isDown)
                return "DOWN";
            if (isUp && isDown)
                return "BOTH";
            return "--";
        }

        private static string ResolveClampState(bool isClamp, bool isUnclamp)
        {
            if (isClamp && !isUnclamp)
                return "CLAMP";
            if (!isClamp && isUnclamp)
                return "UNCLAMP";
            if (isClamp && isUnclamp)
                return "BOTH";
            return "--";
        }
    }
}
