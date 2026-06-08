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
        private ActionButton btnPrepareUnload;
        private ActionButton btnMoveAvoid;
        private bool _manualSequenceRunning;
        private string _lastMaterialDisplayKey = "";
        private const string LogSource = "INPUT-STAGE-PAGE";

        public InputStagePage()
        {
            InitializeComponent();
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
            if (btnStop != null && actionsLayout.Controls.Contains(btnStop))
                actionsLayout.Controls.Remove(btnStop);

            btnPrepareLoad = CreateActionButton("PREP LOAD");
            btnPrepareUnload = CreateActionButton("PREP UNLOAD");
            btnMoveAvoid = CreateActionButton("AVOID");
            actionsLayout.Controls.Add(btnPrepareLoad);
            actionsLayout.Controls.Add(btnPrepareUnload);
            actionsLayout.Controls.Add(btnMoveAvoid);
            if (btnStop != null)
                actionsLayout.Controls.Add(btnStop);
            AlignStopButton();
        }

        private static ActionButton CreateActionButton(string text)
        {
            return new ActionButton
            {
                Text = text,
                Width = 160,
                Height = 44,
                Margin = new Padding(6)
            };
        }

        private void WireEvents()
        {
            btnPrepareLoad.Click += async (s, e) => await RunSequenceAction("INPUT STAGE PREP LOAD", RunPrepareLoadAsync);
            btnWfAlign.Click += async (s, e) => await RunSequenceAction("INPUT STAGE ALIGN", RunAlignAsync);
            btnWfBarcode.Click += async (s, e) => await RunSequenceAction("INPUT STAGE MAP LOAD", RunPrepareLoadAsync);
            btnPrepareUnload.Click += async (s, e) => await RunSequenceAction("INPUT STAGE PREP UNLOAD", RunPrepareUnloadAsync);
            btnMoveAvoid.Click += async (s, e) => await RunSequenceAction("INPUT STAGE AVOID", RunMoveAvoidAsync);
            btnStop.Click += async (s, e) => await StopManualActionAsync();
            materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
            materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;
            actionsLayout.Resize += (s, e) => AlignStopButton();
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
                WriteEvent("INPUT-STAGE-ACTION", actionName + " start");
                bool ok = await action(host);
                WriteEvent("INPUT-STAGE-ACTION", actionName + " result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-STAGE-FAIL", actionName + " failed.");
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패\nAlarm/Event Log를 확인하세요.", "Input Stage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            }
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
                btnStop.BringToFront();
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

        private async Task<bool> RunPrepareLoadAsync(Form1 host)
        {
            return await CreateSequence(host).RunPrepareLoadAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private async Task<bool> RunAlignAsync(Form1 host)
        {
            return await CreateSequence(host).RunAlignAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
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
            return options;
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

                lblStageExistValue.Text = stage.CurrentWaferMap != null ? "WAFER" : "EMPTY";
                lblStageAlignValue.Text = stage.PitchX != 0.0 || stage.PitchY != 0.0 ? "COMPLETE" : "INCOMPLETE";
                lblStageBarcodeValue.Text = stage.CurrentWaferMap != null && !string.IsNullOrWhiteSpace(stage.CurrentWaferMap.WaferId) ? stage.CurrentWaferMap.WaferId : "INCOMPLETE";
                lblStageChipAlignValue.Text = stage.OriginX != 0.0 || stage.OriginY != 0.0 ? "COMPLETE" : "INCOMPLETE";

                lblStageAxisXValue.Text = AxisUnitConverter.FormatDisplay(stage.CameraX.ActualPosition, stage.CameraX, "0.###", true);
                lblStageAxisTValue.Text = AxisUnitConverter.FormatDisplay(stage.StageT.ActualPosition, stage.StageT, "0.###", true);
                lblStageAxisYValue.Text = AxisUnitConverter.FormatDisplay(stage.StageY.ActualPosition, stage.StageY, "0.###", true);
                lblNeedleAxisZValue.Text = AxisUnitConverter.FormatDisplay(stage.NeedleZ.ActualPosition, stage.NeedleZ, "0.###", true);
                lblExpendingValue.Text = AxisUnitConverter.FormatDisplay(stage.ExpanderZ.ActualPosition, stage.ExpanderZ, "0.###", true);
                lblNeedleUpDownValue.Text = stage.NeedleZ.IsMoving ? "MOVING" : "STOP";
                dotNeedleVacuum.IsOn = stage.NeedleVacuum.IsOn;
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
            if (stage != null && stage.CurrentWaferMaterial != null)
                return stage.CurrentWaferMaterial;

            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputStage);
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
