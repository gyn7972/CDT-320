using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.Common.Motion;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class InputStagePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _timer;
        private ActionButton btnPrepareLoad;
        private ActionButton btnDieMapping;
        private ActionButton btnPrepareUnload;
        private ActionButton btnMoveAvoid;
        private bool _manualSequenceRunning;
        private string _lastMaterialDisplayKey = "";
        private const string LogSource = "INPUT-STAGE-PAGE";

        public InputStagePage()
        {
            InitializeComponent();
            ConfigureInfoLayoutForReadableText();
            CreateSequenceButtons();
            WireEvents();

            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshFromMachine();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost() => FindForm() as Form1;

        private void CreateSequenceButtons()
        {
            if (actionsLayout != null)
                actionsLayout.WrapContents = false;

            ConfigureActionButtonSize(btnWfAlign, 180, 64);
            ConfigureActionButtonSize(btnWfBarcode, 180, 64);
            ConfigureActionButtonSize(btnStop, 140, 64);

            if (btnStop != null && actionsLayout.Controls.Contains(btnStop))
                actionsLayout.Controls.Remove(btnStop);

            btnPrepareLoad = CreateActionButton("PREP LOAD");
            btnDieMapping = CreateActionButton("DIE MAPPING");
            btnPrepareUnload = CreateActionButton("PREP UNLOAD");
            btnMoveAvoid = CreateActionButton("AVOID");
            actionsLayout.Controls.Add(btnDieMapping);
            actionsLayout.Controls.Add(btnPrepareLoad);
            actionsLayout.Controls.Add(btnPrepareUnload);
            actionsLayout.Controls.Add(btnMoveAvoid);
            PlaceDieMappingAfterAlign();
            if (btnStop != null)
                actionsLayout.Controls.Add(btnStop);
            EnsureStopButtonLast();
            AlignStopButton();
        }

        private static void ConfigureActionButtonSize(ActionButton button, int width, int height)
        {
            if (button == null)
                return;

            button.Width = width;
            button.Height = height;
            button.Margin = new Padding(6);
        }

        private static ActionButton CreateActionButton(string text)
        {
            var button = new ActionButton
            {
                Text = text,
                Margin = new Padding(6)
            };
            ConfigureActionButtonSize(button, 180, 64);
            return button;
        }

        private void WireEvents()
        {
            btnPrepareLoad.Click += async (s, e) => await RunSequenceAction("INPUT STAGE PREP LOAD", RunPrepareLoadAsync);
            btnWfAlign.Click += async (s, e) => await RunSequenceAction("INPUT STAGE ALIGN", RunAlignAsync);
            btnDieMapping.Click += async (s, e) => await RunSequenceAction("INPUT STAGE DIE MAPPING", RunDieMappingAsync);
            btnWfBarcode.Click += async (s, e) => await RunSequenceAction("INPUT STAGE MAP LOAD", RunPrepareLoadAsync);
            btnPrepareUnload.Click += async (s, e) => await RunSequenceAction("INPUT STAGE PREP UNLOAD", RunPrepareUnloadAsync);
            btnMoveAvoid.Click += async (s, e) => await RunSequenceAction("INPUT STAGE AVOID", RunMoveAvoidAsync);
            btnStop.Click += async (s, e) => await StopManualActionAsync();
            materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
            materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;
            actionsLayout.Resize += (s, e) => AlignStopButton();
            actionsLayout.WrapContents = false;
            EnsureStopButtonLast();
            AlignStopButton();
        }

        private async Task RunSequenceAction(string actionName, Func<Form1, Task<bool>> action)
        {
            IDisposable manualScope = null;
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null || action == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (!ConfirmAction(actionName))
                    return;

                _manualSequenceRunning = true;
                SetSequenceButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                CancellationToken manualToken = host.Controller.ManualOperationToken;
                SequenceFailureStore.Clear();
                WriteEvent("INPUT-STAGE-ACTION", actionName + " start");
                Task<bool> actionTask = action(host);
                Task cancelTask = WaitForCancellationAsync(manualToken);
                Task completed = await Task.WhenAny(actionTask, cancelTask).ConfigureAwait(true);
                if (completed == cancelTask)
                {
                    ObserveManualActionTask(actionTask, actionName);
                    WriteEvent("INPUT-STAGE-CANCEL", actionName + " canceled by stop.");
                    return;
                }

                bool ok = await actionTask.ConfigureAwait(true);
                WriteEvent("INPUT-STAGE-ACTION", actionName + " result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-STAGE-FAIL", actionName + " failed.");
                    string message = SequenceFailureStore.BuildManualFailureMessage(actionName, actionName + " failed. Alarm/Event Log를 확인하세요.");
                    QMC.Common.MessageDialog.Show(this, message, "Input Stage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-STAGE-CANCEL", actionName + " canceled.");
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-ACTION-EX", actionName + " failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (manualScope != null)
                    manualScope.Dispose();
                _manualSequenceRunning = false;
                SetSequenceButtonsEnabled(true);
                RefreshFromMachine();
                BeginRestoreSequenceButtons();
            }
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
                        WriteAlarm("INPUT-STAGE-ACTION-LATE-EX", actionName + " finished after stop: " + (ex != null ? ex.Message : "unknown"));
                    }
                    else if (t.IsCanceled)
                    {
                        WriteEvent("INPUT-STAGE-ACTION-LATE-CANCEL", actionName + " canceled after stop.");
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
            return QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Input Stage", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void SetSequenceButtonsEnabled(bool enabled)
        {
            if (actionsLayout != null)
                actionsLayout.Enabled = true;

            foreach (Control control in actionsLayout.Controls)
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
                    RefreshFromMachine();
                }));
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-BUTTON-RESTORE-EX", "Manual action button restore failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task StopManualActionAsync()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null)
                    return;

                WriteEvent("INPUT-STAGE-STOP", "Manual action stop requested.");
                await host.Controller.StopAsync();
                _manualSequenceRunning = false;
                SetSequenceButtonsEnabled(true);
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-STOP-EX", "Manual action stop failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AlignStopButton()
        {
            if (actionsLayout == null || btnStop == null)
                return;

            EnsureStopButtonLast();

            int usedWidth = actionsLayout.Padding.Left + actionsLayout.Padding.Right;
            foreach (Control control in actionsLayout.Controls)
            {
                if (ReferenceEquals(control, btnStop))
                    continue;
                usedWidth += control.Width + control.Margin.Left + control.Margin.Right;
            }

            int stopWidth = btnStop.Width + 6;
            int leftMargin = Math.Max(6, actionsLayout.ClientSize.Width - usedWidth - stopWidth - btnStop.Margin.Right);
            btnStop.Margin = new Padding(leftMargin, 6, 6, 6);
        }

        private void EnsureStopButtonLast()
        {
            if (actionsLayout == null || btnStop == null || !actionsLayout.Controls.Contains(btnStop))
                return;

            int lastIndex = actionsLayout.Controls.Count - 1;
            if (actionsLayout.Controls.GetChildIndex(btnStop) != lastIndex)
                actionsLayout.Controls.SetChildIndex(btnStop, lastIndex);
        }

        private void PlaceDieMappingAfterAlign()
        {
            try
            {
                if (actionsLayout == null || btnWfAlign == null || btnDieMapping == null)
                    return;
                if (!actionsLayout.Controls.Contains(btnWfAlign) || !actionsLayout.Controls.Contains(btnDieMapping))
                    return;

                int alignIndex = actionsLayout.Controls.GetChildIndex(btnWfAlign);
                actionsLayout.Controls.SetChildIndex(btnDieMapping, alignIndex + 1);
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-ACTION-ORDER-EX", "Input stage action button order failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<bool> RunPrepareLoadAsync(Form1 host)
        {
            return await CreateSequence(host).RunPrepareLoadAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private async Task<bool> RunAlignAsync(Form1 host)
        {
            return await CreateSequence(host).RunAlignAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private async Task<bool> RunDieMappingAsync(Form1 host)
        {
            return await CreateSequence(host).RunDieMappingAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private async Task<bool> RunPrepareUnloadAsync(Form1 host)
        {
            return await CreateSequence(host).RunPrepareUnloadAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private async Task<bool> RunMoveAvoidAsync(Form1 host)
        {
            return await CreateSequence(host).RunMoveAvoidAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private InputStageSequence CreateSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new InputStageSequence(ctx);
        }

        private InputStageSequenceOptions BuildOptions(Form1 host)
        {
            var options = InputStageSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = SequenceStartMode.Restart;
            options.FineMove = false;
            options.RequireVisionAlign = false;
            options.RequireMapData = false;
            options.WaferId = ResolveWaferId(host);
            ApplyInputStageUnitParameters(host, options);
            return options;
        }

        private static void ApplyInputStageUnitParameters(Form1 host, InputStageSequenceOptions options)
        {
            try
            {
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage == null || stage.Config == null || options == null)
                    return;

                if (stage.Config.SequenceMoveTimeoutMs > 0)
                    options.MoveTimeoutMs = stage.Config.SequenceMoveTimeoutMs;
                if (stage.Config.AlignConvergenceThresholdDeg > 0.0)
                    options.AlignThetaToleranceDeg = stage.Config.AlignConvergenceThresholdDeg;
                if (stage.Config.MaxAlignIterations > 0)
                    options.AlignRetryCount = stage.Config.MaxAlignIterations;
                if (stage.Recipe != null && stage.Recipe.DieMap != null)
                {
                    if (!string.IsNullOrWhiteSpace(stage.Recipe.DieMap.VisionTargetId))
                        options.DieMapVisionTargetId = stage.Recipe.DieMap.VisionTargetId;
                    if (stage.Recipe.DieMap.VisionRetryCount > 0)
                        options.DieMapVisionRetryCount = stage.Recipe.DieMap.VisionRetryCount;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "InputStagePage",
                    "InputStage sequence option parameter apply failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static string ResolveWaferId(Form1 host)
        {
            int slot = host != null && host.Controller != null ? host.Controller.CurrentInputSlot : -1;
            return slot >= 0 ? "INPUT-SLOT-" + (slot + 1).ToString("00") : "";
        }

        private void RefreshFromMachine()
        {
            try
            {
                var host = GetHost();
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage == null)
                    return;

                WaferMaterial currentWafer = stage.GetCurrentStageWaferMaterial();
                bool hasWafer = stage.HasWaferOnStage();

                lblStageExistValue.Text = hasWafer ? "WAFER" : "EMPTY";
                lblStageAlignValue.Text = stage.PitchX != 0.0 || stage.PitchY != 0.0 ? "COMPLETE" : "INCOMPLETE";
                lblStageBarcodeValue.Text = ResolveStageWaferId(stage, currentWafer);
                lblStageChipAlignValue.Text = stage.OriginX != 0.0 || stage.OriginY != 0.0 ? "COMPLETE" : "INCOMPLETE";

                lblVisionAxisXValue.Text = AxisUnitConverter.FormatDisplay(stage.CameraX.ActualPosition, stage.CameraX, "0.###", true);
                lblStageAxisTValue.Text = AxisUnitConverter.FormatDisplay(stage.StageT.ActualPosition, stage.StageT, "0.###", true);
                lblStageAxisYValue.Text = AxisUnitConverter.FormatDisplay(stage.StageY.ActualPosition, stage.StageY, "0.###", true);
                lblStageAxisZValue.Text = AxisUnitConverter.FormatDisplay(stage.ExpanderZ.ActualPosition, stage.ExpanderZ, "0.###", true);
                label2.Text = AxisUnitConverter.FormatDisplay(stage.NeedleBlockX.ActualPosition, stage.NeedleBlockX, "0.###", true);
                lblNeedleAxisZValue.Text = AxisUnitConverter.FormatDisplay(stage.NeedleZ.ActualPosition, stage.NeedleZ, "0.###", true);
                label4.Text = AxisUnitConverter.FormatDisplay(stage.EjectPinZ.ActualPosition, stage.EjectPinZ, "0.###", true);
                lblExpendingValue.Text = AxisUnitConverter.FormatDisplay(stage.ExpanderZ.ActualPosition, stage.ExpanderZ, "0.###", true);
                lblNeedleUpDownValue.Text = stage.NeedleZ.IsMoving ? "MOVING" : "STOP";
                dotNeedleVacuum.IsOn = stage.IsInputStageSimulationOrDryRun() ? hasWafer : stage.NeedleVacuum.IsOn;
                RefreshMaterialDetail(false);
            }
            catch (Exception ex)
            {
                WriteWarning("INPUT-STAGE-REFRESH", "Refresh failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RefreshMaterialDetail(bool force)
        {
            if (materialDetailView == null)
                return;

            WaferMaterial wafer = ResolveStageWaferMaterial();
            if (wafer == null || string.IsNullOrWhiteSpace(wafer.WaferId) ||
                WaferMaterialStateText.Normalize(wafer.State) == WaferMaterialState.Empty)
            {
                _lastMaterialDisplayKey = "";
                materialDetailView.Clear();
                materialDetailView.Visible = true;
                return;
            }

            string displayKey = BuildMaterialDisplayKey(wafer);
            if (!force && materialDetailView.Visible && string.Equals(displayKey, _lastMaterialDisplayKey, StringComparison.Ordinal))
                return;

            _lastMaterialDisplayKey = displayKey;
            materialDetailView.Visible = true;
            materialDetailView.SetRows("WAFER MATERIAL", BuildStageMaterialRows(wafer));
        }

        private WaferMaterial ResolveStageWaferMaterial()
        {
            var host = GetHost();
            var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
            if (stage != null)
                return stage.GetCurrentStageWaferMaterial();

            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
        }

        private void ConfigureInfoLayoutForReadableText()
        {
            try
            {
                ConfigureAxisPanel(stageAxisYPanel, lblStageAxisYTitle, lblStageAxisYValue);
                ConfigureAxisPanel(stageAxisTPanel, lblStageAxisTTitle, lblStageAxisTValue);
                ConfigureAxisPanel(tableLayoutPanel1, lblStageAxisZTitle, lblStageAxisZValue);
                ConfigureAxisPanel(stageAxisXPanel, lblStageAxisXTitle, lblVisionAxisXValue);
                ConfigureAxisPanel(tableLayoutPanel2, label1, label2);
                ConfigureAxisPanel(needleAxisZPanel, lblNeedleAxisZTitle, lblNeedleAxisZValue);
                ConfigureAxisPanel(tableLayoutPanel3, label3, label4);
                ConfigureStatusValueLabels();

                if (infoLayout != null)
                {
                    infoLayout.SuspendLayout();
                    infoLayout.RowStyles.Clear();
                    infoLayout.RowCount = 8;
                    for (int i = 0; i < 4; i++)
                        infoLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
                    for (int i = 4; i < 8; i++)
                        infoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                    infoLayout.ResumeLayout();
                }
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void ConfigureAxisPanel(TableLayoutPanel panel, Label title, Label value)
        {
            if (panel != null)
            {
                panel.RowStyles.Clear();
                panel.RowCount = 2;
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
                panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                panel.MinimumSize = new System.Drawing.Size(0, 56);
                panel.Margin = new Padding(4, 4, 4, 4);
            }

            if (title != null)
            {
                title.AutoSize = false;
                title.AutoEllipsis = true;
                title.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
                title.Padding = new Padding(6, 0, 0, 0);
                title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }

            if (value != null)
            {
                value.AutoSize = false;
                value.AutoEllipsis = true;
                value.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
                value.Padding = new Padding(0, 0, 6, 0);
                value.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            }
        }

        private void ConfigureStatusValueLabels()
        {
            ConfigureCompactValueLabel(lblStageExistValue);
            ConfigureCompactValueLabel(lblStageAlignValue);
            ConfigureCompactValueLabel(lblStageBarcodeValue);
            ConfigureCompactValueLabel(lblStageChipAlignValue);
            ConfigureCompactValueLabel(lblStageFinishValue);
            ConfigureCompactValueLabel(lblExpendingValue);
            ConfigureCompactValueLabel(lblNeedleUpDownValue);
        }

        private static void ConfigureCompactValueLabel(Label label)
        {
            if (label == null)
                return;

            label.AutoSize = false;
            label.AutoEllipsis = true;
            label.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        }

        private static string ResolveStageWaferId(QMC.CDT320.InputStageUnit stage, WaferMaterial currentWafer)
        {
            if (stage != null && stage.CurrentWaferMap != null && !string.IsNullOrWhiteSpace(stage.CurrentWaferMap.WaferId))
                return stage.CurrentWaferMap.WaferId;
            if (currentWafer != null && !string.IsNullOrWhiteSpace(currentWafer.WaferId))
                return currentWafer.WaferId;
            return "INCOMPLETE";
        }

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction("Input Stage에 Material Data를 생성하시겠습니까?"))
                    return;

                var wafer = MaterialStateService.CreateWaferAtLocation(
                    MaterialLocationKind.InputStage,
                    "INPUT-STAGE-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    WaferMaterialState.Working);

                var host = GetHost();
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage != null)
                    stage.SetCurrentWaferMaterial(wafer);

                WriteEvent("INPUT-STAGE-DATA-CREATE", "wafer=" + (wafer != null ? wafer.WaferId : ""));
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-DATA-CREATE-EX", "Material data create failed: " + ex.Message);
            }
        }

        private void MaterialDetailView_ClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction("Input Stage의 Material Data를 초기화하시겠습니까?"))
                    return;

                MaterialStateService.ClearWaferAtLocation(MaterialLocationKind.InputStage);
                var host = GetHost();
                var stage = host != null && host.Machine != null ? host.Machine.InputStageUnit : null;
                if (stage != null)
                    stage.ClearCurrentWaferMaterial();

                WriteEvent("INPUT-STAGE-DATA-CLEAR", "Input stage material data cleared.");
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-STAGE-DATA-CLEAR-EX", "Material data clear failed: " + ex.Message);
            }
        }

        private bool ConfirmMaterialDataAction(string message)
        {
            return QMC.Common.MessageDialog.Show(this, message, "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private static IEnumerable<MaterialDetailRow> BuildStageMaterialRows(WaferMaterial wafer)
        {
            string specName = wafer != null ? wafer.TapeFrameSpecName : "";
            var spec = !string.IsNullOrEmpty(specName) ? MaterialSpecs.FindFrame(specName) : null;
            var location = wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";

            return new[]
            {
                Row("Unit", "Input Stage", "", false),
                Row("Wafer ID", wafer != null ? wafer.WaferId : "", "", false),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : "", "", false),
                Row("TapeFrame Spec", specName, "", false),
                Row("Frame Size", spec != null ? spec.OuterDiameterMm.ToString("0.###") + " mm" : "", "", false),
                Row("Grid", spec != null ? spec.GridX + " x " + spec.GridY : "", "", false),
                Row("Die Spec", spec != null ? spec.DieSpecName : "", "", false),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : "", "", false),
                Row("Source Cassette", wafer != null ? wafer.SourceCassetteRole.ToString() : "", "", false),
                Row("Source Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : "", "", false),
                Row("Source Pos", wafer != null && !double.IsNaN(wafer.SourceCassetteSlotPosition) ? wafer.SourceCassetteSlotPosition.ToString("0.###") : "", "", false),
                Row("Current Loc", location, "", false),
                Row("DieMap ObjId", wafer != null ? wafer.DieMapFrameObjId : "", "", false),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "", "", false)
            };
        }

        private static string BuildMaterialDisplayKey(WaferMaterial wafer)
        {
            if (wafer == null)
                return "";

            string location = wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";
            return string.Join("|",
                wafer.WaferId ?? "",
                WaferMaterialStateText.ToDisplayName(wafer.State),
                wafer.TapeFrameSpecName ?? "",
                wafer.CassetteLotId ?? "",
                wafer.SourceCassetteRole.ToString(),
                wafer.SourceSlotNumber.ToString(),
                location,
                wafer.DieMapFrameObjId ?? "",
                wafer.UpdatedAt.Ticks.ToString());
        }

        private static MaterialDetailRow Row(string name, string value, string key, bool editable)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = value,
                Key = key ?? "",
                Editable = editable
            };
        }

        private static void WriteEvent(string code, string message)
        {
            try { EventLogger.Write(EventKind.Event, "UI", code, message); } catch { }
        }

        private static void WriteWarning(string code, string message)
        {
            try { EventLogger.Write(EventKind.Warning, "UI", code, message); } catch { }
        }

        private static void WriteAlarm(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Alarm, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Warning, code, LogSource, message);
            }
            catch { }
        }

        private static void RaiseWarning(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Warning, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Warning, code, LogSource, message);
            }
            catch { }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { _timer?.Stop(); _timer?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
