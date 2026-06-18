using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
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

            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshData();
            HandleCreated += (s, e) => _timer.Start();
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
            rdoGoodMaterial.CheckedChanged += (s, e) =>
            {
                if (!rdoGoodMaterial.Checked)
                    return;

                _selectedMaterialSide = BinSide.Good;
                RefreshData();
            };
            rdoNgMaterial.CheckedChanged += (s, e) =>
            {
                if (!rdoNgMaterial.Checked)
                    return;

                _selectedMaterialSide = BinSide.Ng;
                RefreshData();
            };
            actionPanel.Resize += (s, e) => AlignStopButton();
            EnsureStopButtonLast();
            AlignStopButton();
        }

        private async Task RunSequenceAction(string actionName, Func<Form1, Task<bool>> action)
        {
            IDisposable manualScope = null;
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
                manualScope = host.Controller.EnterManualOperation();
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
                if (manualScope != null)
                    manualScope.Dispose();

                _manualSequenceRunning = false;
                SetSequenceButtonsEnabled(true);
                RefreshData();
                BeginRestoreSequenceButtons();
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

        private static Task WaitForCancellationAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
                return Task.Delay(Timeout.Infinite);
            if (ct.IsCancellationRequested)
                return Task.FromResult(0);

            var tcs = new TaskCompletionSource<int>();
            ct.Register(() => tcs.TrySetResult(0));
            return tcs.Task;
        }

        private void ObserveManualActionTask(Task<bool> task, string actionName)
        {
            if (task == null)
                return;

            task.ContinueWith(t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        Exception ex = t.Exception != null ? t.Exception.GetBaseException() : null;
                        WriteAlarm("OUTPUT-STAGE-ACTION-LATE-EX", actionName + " finished after stop: " + (ex != null ? ex.Message : "unknown"));
                    }
                    else if (t.IsCanceled)
                    {
                        WriteEvent("OUTPUT-STAGE-ACTION-LATE-CANCEL", actionName + " canceled after stop.");
                    }
                }
                catch
                {
                }
                finally
                {
                }
            });
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
            {
                btnStop.Enabled = true;
                EnsureStopButtonLast();
                AlignStopButton();
            }
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

        private void AlignStopButton()
        {
            if (actionPanel == null || btnStop == null)
                return;

            EnsureStopButtonLast();

            int usedWidth = actionPanel.Padding.Left + actionPanel.Padding.Right;
            foreach (Control control in actionPanel.Controls)
            {
                if (ReferenceEquals(control, btnStop))
                    continue;
                usedWidth += control.Width + control.Margin.Left + control.Margin.Right;
            }

            int stopWidth = btnStop.Width + 6;
            int leftMargin = Math.Max(6, actionPanel.ClientSize.Width - usedWidth - stopWidth - btnStop.Margin.Right);
            btnStop.Margin = new Padding(leftMargin, 6, 6, 6);
        }

        private void EnsureStopButtonLast()
        {
            if (actionPanel == null || btnStop == null || !actionPanel.Controls.Contains(btnStop))
                return;

            int lastIndex = actionPanel.Controls.Count - 1;
            if (actionPanel.Controls.GetChildIndex(btnStop) != lastIndex)
                actionPanel.Controls.SetChildIndex(btnStop, lastIndex);
        }

        private async Task<bool> RunPrepareLoadAsync(Form1 host, BinSide side)
        {
            return await CreateSequence(host).RunPrepareLoadAsync(host.Controller.ManualOperationToken, BuildOptions(side, ResolveGrade(side))) == 0;
        }

        private async Task<bool> RunMoveProcessAsync(Form1 host, BinSide side)
        {
            return await CreateSequence(host).RunMoveProcessAsync(host.Controller.ManualOperationToken, BuildOptions(side, ResolveGrade(side))) == 0;
        }

        private async Task<bool> RunReceiveDieAsync(Form1 host, DieGrade grade)
        {
            return await CreateSequence(host).RunReceiveDieAsync(host.Controller.ManualOperationToken, BuildOptions(ResolveSide(grade), grade)) == 0;
        }

        private async Task<bool> RunPrepareUnloadAsync(Form1 host, BinSide side)
        {
            return await CreateSequence(host).RunPrepareUnloadAsync(host.Controller.ManualOperationToken, BuildOptions(side, ResolveGrade(side))) == 0;
        }

        private async Task<bool> RunInspectBinAsync(Form1 host)
        {
            return await CreateSequence(host).RunInspectBinAsync(host.Controller.ManualOperationToken, BuildOptions(BinSide.Good, DieGrade.Good)) == 0;
        }

        private async Task<bool> RunMoveAvoidAsync(Form1 host)
        {
            return await CreateSequence(host).RunMoveAvoidAsync(host.Controller.ManualOperationToken, BuildOptions(BinSide.Good, DieGrade.Good)) == 0;
        }

        private OutputStageSequence CreateSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new OutputStageSequence(ctx);
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
                if (QMC.Common.MessageDialog.Show(this, "Output Stage " + sideName + " 위치에 Material Data를 생성하시겠습니까?", "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                MaterialStateService.CreateWaferAtLocation(
                    ResolveMaterialLocation(_selectedMaterialSide),
                    "OUTPUT-STAGE-" + sideName.ToUpperInvariant() + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
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
