using System;
using System.Collections.Generic;
using System.Drawing;
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
    public partial class InputFeederPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _timer;
        private FlowLayoutPanel _sequenceActions;
        private bool _manualSequenceRunning;
        private SequenceStartMode _manualSequenceStartMode = SequenceStartMode.Resume;
        private string _lastMaterialDisplayKey = "";
        private const string LogSource = "INPUT-FEEDER-PAGE";

        public InputFeederPage()
        {
            InitializeComponent();
            WireSequenceActionButtons();

            _timer = new System.Windows.Forms.Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshFromMachine();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost() => FindForm() as Form1;

        private void WireSequenceActionButtons()
        {
            try
            {
                _sequenceActions = actionsLayout;
                btnLoadFromCassette.Click += async (s, e) => await RunSequenceAction(btnLoadFromCassette.Text, RunLoadFromCassetteAsync);
                btnLoadToStage.Click += async (s, e) => await RunSequenceAction(btnLoadToStage.Text, RunLoadToStageAsync);
                btnUnloadFromStage.Click += async (s, e) => await RunSequenceAction(btnUnloadFromStage.Text, RunUnloadFromStageAsync);
                btnUnloadToCassette.Click += async (s, e) => await RunSequenceAction(btnUnloadToCassette.Text, RunUnloadToCassetteAsync);
                btnRecover.Click += async (s, e) => await RunSequenceAction(btnRecover.Text, RunRecoverAsync);
                btnStop.Click += async (s, e) => await StopManualActionAsync();
                materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
                materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;
                actionsLayout.Resize += (s, e) => AlignStopButton();
                EnsureStopButtonLast();
                AlignStopButton();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-BUTTON", "Wire sequence buttons failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void AddSequenceButton(string text, Func<Form1, Task<bool>> action)
        {
            var button = new ActionButton
            {
                Text = text,
                BackColor = Color.FromArgb(128, 128, 128),
                Cursor = Cursors.Hand,
                Font = new Font("맑은 고딕", 11F),
                ForeColor = Color.White,
                Width = 180,
                Height = 64,
                Margin = new Padding(6)
            };
            button.Click += async (s, e) => await RunSequenceAction(text, action);
            _sequenceActions.Controls.Add(button);
        }

        private async Task RunSequenceAction(string actionName, Func<Form1, Task<bool>> action)
        {
            IDisposable manualScope = null;
            bool showFailure = false;
            string exceptionMessage = null;
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null || action == null)
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
                SequenceFailureStore.Clear();
                WriteEvent("INPUT-FEEDER-ACTION", actionName + " start");
                bool ok = await action(host);
                WriteEvent("INPUT-FEEDER-ACTION", actionName + " result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-FEEDER-FAIL", actionName + " failed.");
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
                WriteEvent("INPUT-FEEDER-CANCEL", actionName + " canceled.");
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-ACTION-EX", actionName + " failed: " + ex.Message);
                exceptionMessage = ex.Message;
            }
            finally
            {
                try
                {
                    if (manualScope != null)
                        manualScope.Dispose();
                }
                catch (Exception ex)
                {
                    WriteAlarm("INPUT-FEEDER-MANUAL-CLEANUP", "Input Feeder 수동 시컨스 정리 중 오류: " + ex.Message);
                }
                finally
                {
                    _manualSequenceRunning = false;
                    try { SetSequenceButtonsEnabled(true); } catch (Exception ex) { WriteAlarm("INPUT-FEEDER-BUTTON-RESTORE", "Input Feeder 버튼 복구 실패: " + ex.Message); }
                    try { RefreshFromMachine(); } catch (Exception ex) { WriteAlarm("INPUT-FEEDER-REFRESH", "Input Feeder 화면 갱신 실패: " + ex.Message); }
                }
            }

            if (showFailure)
            {
                string message = SequenceFailureStore.BuildManualFailureMessage(
                    actionName,
                    actionName + " 실패\r\nAlarm/Event Log를 확인하세요.");
                QMC.Common.MessageDialog.Show(this, message, "Input Feeder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, exceptionMessage, "Input Feeder", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool ConfirmAction(string actionName)
        {
            return QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Input Feeder", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void SetSequenceButtonsEnabled(bool enabled)
        {
            if (_sequenceActions == null)
                return;

            _sequenceActions.Enabled = true;
            foreach (Control control in _sequenceActions.Controls)
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

        private async Task StopManualActionAsync()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null)
                    return;

                WriteEvent("INPUT-FEEDER-STOP", "Manual action stop requested.");
                await host.Controller.StopAsync();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-STOP-EX", "Manual action stop failed: " + ex.Message);
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

        private async Task<bool> RunLoadFromCassetteAsync(Form1 host)
        {
            return await CreateSequence(host).RunLoadFromCassetteAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private async Task<bool> RunLoadToStageAsync(Form1 host)
        {
            return await CreateSequence(host).RunLoadToStageAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private async Task<bool> RunUnloadFromStageAsync(Form1 host)
        {
            return await CreateSequence(host).RunUnloadFromStageAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private async Task<bool> RunUnloadToCassetteAsync(Form1 host)
        {
            return await CreateSequence(host).RunUnloadToCassetteAsync(host.Controller.ManualOperationToken, BuildUnloadToCassetteOptions(host)) == 0;
        }

        private async Task<bool> RunRecoverAsync(Form1 host)
        {
            return await CreateSequence(host).RunRecoverAsync(host.Controller.ManualOperationToken, BuildOptions(host)) == 0;
        }

        private InputFeederSequence CreateSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new InputFeederSequence(ctx);
        }

        private InputFeederSequenceOptions BuildOptions(Form1 host)
        {
            var options = InputFeederSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = _manualSequenceStartMode;
            options.SlotIndex = ResolveInputSlot(host);
            options.NextSlotIndex = options.SlotIndex;
            options.WaferSize = ResolveInputWaferSize(host);
            options.MoveTimeoutMs = ResolveMoveTimeoutMs(host);
            options.FineMove = false;
            return options;
        }

        private InputFeederSequenceOptions BuildUnloadToCassetteOptions(Form1 host)
        {
            var options = BuildOptions(host);
            WaferMaterial wafer = ResolveFeederWaferMaterial();
            if (wafer != null &&
                wafer.SourceCassetteRole == options.CassetteRole &&
                wafer.SourceSlotNumber >= 0)
            {
                options.SlotIndex = wafer.SourceSlotNumber;
                options.NextSlotIndex = options.SlotIndex;
            }

            return options;
        }

        private static int ResolveInputSlot(Form1 host)
        {
            var controller = host != null ? host.Controller : null;
            if (controller != null && controller.CurrentInputSlot >= 0)
                return controller.CurrentInputSlot;

            var cassette = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
            if (cassette == null)
                return 0;

            int slot = cassette.FindNextProcessWaferSlot();
            return slot >= 0 ? slot : 0;
        }

        private static int ResolveInputWaferSize(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.InputCassetteUnit : null;
            return MaterialStateService.ResolveWaferSizeInch(cassette != null && cassette.Config != null ? cassette.Config.InchSelect : 8);
        }

        private static int ResolveMoveTimeoutMs(Form1 host)
        {
            var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
            if (feeder != null && feeder.FeederY != null && feeder.FeederY.Setup != null && feeder.FeederY.Setup.MoveTimeoutMs > 0)
                return feeder.FeederY.Setup.MoveTimeoutMs;
            return 10000;
        }

        private void RefreshFromMachine()
        {
            var host = GetHost();
            if (host?.Machine == null) return;
            var loader = host.Machine.InputFeederUnit;

            _lblFeederPos.Text = AxisUnitConverter.FormatDisplay(loader.FeederY.ActualPosition, loader.FeederY, "0.###", true);
            bool clamp = loader.IsWaferFeederClamp();
            bool unclamp = loader.IsWaferFeederUnclamp();
            _lblClampState.Text = clamp ? "CLAMP" : (unclamp ? "UNCLAMP" : "ERROR");
            _lblClampState.ForeColor = clamp || unclamp ? Color.Black : Color.Red;

            bool up = loader.IsWaferFeederUp();
            bool down = loader.IsWaferFeederDown();
            _lblUpDownState.Text = down ? "DOWN" : (up ? "UP" : "--");
            _lblUpDownState.ForeColor = up || down ? Color.Black : Color.Red;
            bool hasFeederWaferData = HasDisplayFeederWafer();
            bool hasFeederWafer = loader.IsWaferFeederSimulationOrDryRun()
                ? hasFeederWaferData
                : loader.HasWaferOnFeeder();
            _lblExist.Text = hasFeederWafer ? "WAFER" : "--";

            _markRing.BackColor = loader.IsWaferFeederSimulationOrDryRun()
                ? (hasFeederWaferData ? Color.LimeGreen : Color.Black)
                : (loader.WaferFeederRingCheckSensor.IsOn ? Color.LimeGreen : Color.Black);
            _markOverload.BackColor = loader.IsWaferFeederOverload() ? Color.Red : Color.Black;

            RefreshMaterialDetail(false);
        }

        private bool HasDisplayFeederWafer()
        {
            try
            {
                WaferMaterial wafer = ResolveFeederWaferMaterial();
                return wafer != null &&
                       !string.IsNullOrWhiteSpace(wafer.WaferId) &&
                       WaferMaterialStateText.Normalize(wafer.State) != WaferMaterialState.Empty;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private void RefreshMaterialDetail(bool force)
        {
            if (materialDetailView == null)
                return;

            WaferMaterial wafer = ResolveFeederWaferMaterial();
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
            materialDetailView.SetRows("FEEDER MATERIAL", BuildFeederMaterialRows(wafer));
        }

        private WaferMaterial ResolveFeederWaferMaterial()
        {
            var host = GetHost();
            var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
            if (feeder != null && feeder.CurrentWaferMaterial != null)
                return feeder.CurrentWaferMaterial;

            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.InputFeeder);
        }

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction("Input Feeder에 Material Data를 생성하시겠습니까?"))
                    return;

                var wafer = MaterialStateService.CreateWaferAtLocation(
                    MaterialLocationKind.InputFeeder,
                    "INPUT-FEEDER-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    WaferMaterialState.WorkReady);

                var host = GetHost();
                var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
                if (feeder != null)
                    feeder.SetCurrentWaferMaterial(wafer);

                WriteEvent("INPUT-FEEDER-DATA-CREATE", "wafer=" + (wafer != null ? wafer.WaferId : ""));
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-DATA-CREATE-EX", "Material data create failed: " + ex.Message);
            }
        }

        private void MaterialDetailView_ClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction("Input Feeder의 Material Data를 초기화하시겠습니까?"))
                    return;

                MaterialStateService.ClearWaferAtLocation(MaterialLocationKind.InputFeeder);
                var host = GetHost();
                var feeder = host != null && host.Machine != null ? host.Machine.InputFeederUnit : null;
                if (feeder != null)
                    feeder.ClearCurrentWaferMaterial();

                WriteEvent("INPUT-FEEDER-DATA-CLEAR", "Input feeder material data cleared.");
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-FEEDER-DATA-CLEAR-EX", "Material data clear failed: " + ex.Message);
            }
        }

        private bool ConfirmMaterialDataAction(string message)
        {
            return QMC.Common.MessageDialog.Show(this, message, "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private static IEnumerable<MaterialDetailRow> BuildFeederMaterialRows(WaferMaterial wafer)
        {
            string specName = wafer != null ? wafer.TapeFrameSpecName : "";
            var spec = !string.IsNullOrEmpty(specName) ? MaterialSpecs.FindFrame(specName) : null;
            var location = wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";

            return new[]
            {
                Row("Unit", "Input Feeder", "", false),
                Row("Wafer ID", wafer != null ? wafer.WaferId : "", "", false),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : "", "", false),
                Row("TapeFrame Spec", specName, "", false),
                Row("Frame Size", spec != null ? spec.OuterDiameterMm.ToString("0.###") + " mm" : "", "", false),
                Row("Grid", spec != null ? spec.DieMapX + " x " + spec.DieMapY : "", "", false),
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
