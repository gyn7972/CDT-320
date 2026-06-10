using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.Logging;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OutputFeederPage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private Timer _timer;
        private bool _manualSequenceRunning;
        private string _lastMaterialDisplayKey = "";

        public OutputFeederPage()
        {
            InitializeComponent();
            WireEvents();

            _timer = new Timer { Interval = 200 };
            _timer.Tick += (s, e) => RefreshData();
            HandleCreated += (s, e) => _timer.Start();
            HandleDestroyed += (s, e) => _timer.Stop();
        }

        private Form1 GetHost() => FindForm() as Form1;

        private void WireEvents()
        {
            btnLoadFromCassette.Click += async (s, e) => await RunSequenceAction(btnLoadFromCassette.Text, RunLoadFromCassetteAsync);
            btnLoadToStage.Click += async (s, e) => await RunSequenceAction(btnLoadToStage.Text, RunLoadToStageAsync);
            btnUnloadFromStage.Click += async (s, e) => await RunSequenceAction(btnUnloadFromStage.Text, RunUnloadFromStageAsync);
            btnUnloadToCassette.Click += async (s, e) => await RunSequenceAction(btnUnloadToCassette.Text, RunUnloadToCassetteAsync);
            btnRecover.Click += async (s, e) => await RunSequenceAction(btnRecover.Text, RunRecoverAsync);
            btnStop.Click += async (s, e) => await StopManualActionAsync();
            materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
            materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;
            actionPanel.Resize += (s, e) => AlignStopButton();
            EnsureStopButtonLast();
            AlignStopButton();
        }

        private async Task RunSequenceAction(string actionName, Func<Form1, Task<bool>> action)
        {
            IDisposable manualScope = null;
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null || host.Machine == null || action == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (!ConfirmAction(actionName))
                    return;

                _manualSequenceRunning = true;
                SetSequenceButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                SequenceFailureStore.Clear();
                EventLogger.Write(EventKind.Event, "QMC", "OUTPUT-FEEDER-ACTION", actionName + " start");
                bool ok = await action(host);
                EventLogger.Write(EventKind.Event, "QMC", "OUTPUT-FEEDER-ACTION", actionName + " result=" + ok);
                if (!ok)
                {
                    string message = SequenceFailureStore.BuildManualFailureMessage(actionName, actionName + " 실패\r\nAlarm/Event Log를 확인하세요.");
                    QMC.Common.MessageDialog.Show(this, message, "Output Feeder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                EventLogger.Write(EventKind.Event, "QMC", "OUTPUT-FEEDER-CANCEL", actionName + " canceled.");
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "OUTPUT-FEEDER-ACTION-EX", actionName + " failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (manualScope != null)
                    manualScope.Dispose();
                _manualSequenceRunning = false;
                SetSequenceButtonsEnabled(true);
                RefreshData();
            }
        }

        private bool ConfirmAction(string actionName)
        {
            return QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Output Feeder", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void SetSequenceButtonsEnabled(bool enabled)
        {
            actionPanel.Enabled = true;
            foreach (Control control in actionPanel.Controls)
            {
                if (!ReferenceEquals(control, btnStop))
                    control.Enabled = enabled;
            }

            btnStop.Enabled = true;
            EnsureStopButtonLast();
            AlignStopButton();
        }

        private async Task StopManualActionAsync()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Controller == null)
                    return;

                EventLogger.Write(EventKind.Event, "QMC", "OUTPUT-FEEDER-STOP", "Manual action stop requested.");
                await host.Controller.StopAsync();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "QMC", "OUTPUT-FEEDER-STOP-EX", "Manual action stop failed: " + ex.Message);
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

        private async Task<bool> RunLoadFromCassetteAsync(Form1 host)
        {
            OutputSlotPlan plan;
            if (!OutputSlotPlanner.TryResolveNextSupplySlot(BinSide.Good, out plan))
                return false;

            var options = BuildOptions(host, plan);
            return await CreateSequence(host).RunLoadFromCassetteAsync(host.Controller.ManualOperationToken, options) == 0;
        }

        private async Task<bool> RunLoadToStageAsync(Form1 host)
        {
            var options = BuildOptions(host, ResolveFeederSide(), ResolveFeederRole(), ResolveFeederSlot());
            return await CreateSequence(host).RunLoadToStageAsync(host.Controller.ManualOperationToken, options) == 0;
        }

        private async Task<bool> RunUnloadFromStageAsync(Form1 host)
        {
            BinSide side = ResolveStageUnloadSide();
            var options = BuildOptions(host, side, side == BinSide.Ng ? CassetteMaterialRole.Ng1 : CassetteMaterialRole.Good1, 0);
            return await CreateSequence(host).RunUnloadFromStageAsync(host.Controller.ManualOperationToken, options) == 0;
        }

        private async Task<bool> RunUnloadToCassetteAsync(Form1 host)
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            CassetteMaterialRole role = wafer != null ? wafer.SourceCassetteRole : ResolveFeederRole();
            int slot = wafer != null && wafer.SourceSlotNumber >= 0 ? wafer.SourceSlotNumber : ResolveFeederSlot();
            BinSide side = role == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
            var options = BuildOptions(host, side, role, slot);
            return await CreateSequence(host).RunUnloadToCassetteAsync(host.Controller.ManualOperationToken, options) == 0;
        }

        private async Task<bool> RunRecoverAsync(Form1 host)
        {
            var options = BuildOptions(host, ResolveFeederSide(), ResolveFeederRole(), ResolveFeederSlot());
            return await CreateSequence(host).RunRecoverAsync(host.Controller.ManualOperationToken, options) == 0;
        }

        private OutputFeederSequence CreateSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new OutputFeederSequence(ctx);
        }

        private OutputFeederSequenceOptions BuildOptions(Form1 host, OutputSlotPlan plan)
        {
            return BuildOptions(host, plan != null ? plan.Side : BinSide.Good, plan != null ? plan.CassetteRole : CassetteMaterialRole.Good1, plan != null ? plan.SlotIndex : 0);
        }

        private OutputFeederSequenceOptions BuildOptions(Form1 host, BinSide side, CassetteMaterialRole role, int slotIndex)
        {
            var options = OutputFeederSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = SequenceStartMode.Restart;
            options.Side = side;
            options.CassetteRole = role;
            options.SlotIndex = Math.Max(0, slotIndex);
            options.NextSlotIndex = options.SlotIndex;
            options.MoveTimeoutMs = ResolveMoveTimeoutMs(host);
            options.FineMove = false;
            return options;
        }

        private static int ResolveMoveTimeoutMs(Form1 host)
        {
            return OutputFeederSequenceOptions.Default().MoveTimeoutMs;
        }

        private static BinSide ResolveStageUnloadSide()
        {
            if (MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputStageNg) != null)
                return BinSide.Ng;
            return BinSide.Good;
        }

        private static BinSide ResolveFeederSide()
        {
            CassetteMaterialRole role = ResolveFeederRole();
            return role == CassetteMaterialRole.Ng1 ? BinSide.Ng : BinSide.Good;
        }

        private static CassetteMaterialRole ResolveFeederRole()
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (wafer != null &&
                (wafer.SourceCassetteRole == CassetteMaterialRole.Good1 ||
                 wafer.SourceCassetteRole == CassetteMaterialRole.Good2 ||
                 wafer.SourceCassetteRole == CassetteMaterialRole.Ng1))
                return wafer.SourceCassetteRole;

            return CassetteMaterialRole.Good1;
        }

        private static int ResolveFeederSlot()
        {
            WaferMaterial wafer = MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
            if (wafer != null && wafer.SourceSlotNumber >= 0)
                return wafer.SourceSlotNumber;

            return 0;
        }

        private void RefreshData()
        {
            var host = GetHost();
            if (host?.Machine == null)
                return;

            var feeder = host.Machine.OutputFeederUnit;
            var cassette = host.Machine.OutputCassetteUnit;
            if (feeder == null || cassette == null)
                return;

            bool hasFeederWaferData = HasDisplayFeederWafer();
            bool hasFeederWafer = IsOutputFeederSimulationOrDryRun(feeder)
                ? hasFeederWaferData
                : feeder.IsFeederOccupied();
            CassetteMaterialRole role = ResolveFeederRole();
            int slot = ResolveFeederSlot();

            _lblFeederPos.Text = AxisUnitConverter.FormatDisplay(feeder.FeederY.ActualPosition, feeder.FeederY, "0.###", true);
            _lblExist.Text = hasFeederWafer ? "WAFER" : "--";
            _lblSide.Text = role == CassetteMaterialRole.Ng1 ? "NG" : "GOOD";
            _lblSlot.Text = slot >= 0 ? (slot + 1).ToString("00") : "--";
            _lblClampState.Text = feeder.FeederClampCyl.IsFwd ? "CLAMP" : (feeder.FeederClampCyl.IsBwd ? "UNCLAMP" : "ERROR");
            _lblUpDownState.Text = feeder.FeederUpDownCyl.IsFwd ? "UP" : (feeder.FeederUpDownCyl.IsBwd ? "DOWN" : "--");

            _markRing.BackColor = IsOutputFeederSimulationOrDryRun(feeder)
                ? (hasFeederWaferData ? Color.LimeGreen : Color.Black)
                : (feeder.BinFeederRingCheckSensor.IsOn ? Color.LimeGreen : Color.Black);
            _markOverload.BackColor = feeder.IsFeederOverload() ? Color.Red : Color.Black;
            _markUp.BackColor = feeder.BinFeederUpSensor.IsOn ? Color.LimeGreen : Color.Black;
            _markDown.BackColor = feeder.BinFeederDownSensor.IsOn ? Color.LimeGreen : Color.Black;
            _markUnclamp.BackColor = feeder.BinFeederUnclampSensor.IsOn ? Color.LimeGreen : Color.Black;

            _markGood1.BackColor = IsGoodCassette1Detected(cassette) ? Color.LimeGreen : Color.Black;
            _markGood2.BackColor = IsGoodCassette2Detected(cassette) ? Color.LimeGreen : Color.Black;
            _markNg.BackColor = IsNgCassetteDetected(cassette) ? Color.LimeGreen : Color.Black;
            _markProtrusion.BackColor = cassette.IsBinProtrusionDetected() ? Color.Red : Color.Black;
            _markMapping.BackColor = cassette.IsBinMapping() ? Color.LimeGreen : Color.Black;
            _markNgBw.BackColor = cassette.IsNgBinBW() ? Color.LimeGreen : Color.Black;
            _markNgLock.BackColor = cassette.IsNgBinLock() ? Color.LimeGreen : Color.Black;

            RefreshMaterialDetail(false);
        }

        private static bool IsOutputFeederSimulationOrDryRun(OutputFeederUnit feeder)
        {
            return feeder != null &&
                   ((feeder.Setup != null && feeder.Setup.IsSimulationMode) ||
                    (feeder.Config != null && feeder.Config.bDryRun));
        }

        private static bool IsGoodCassette1Detected(OutputCassetteUnit cassette)
        {
            return cassette != null &&
                   (cassette.GoodBin8CassetteCheck0.IsOn ||
                    cassette.GoodBin12CassetteCheck0.IsOn);
        }

        private static bool IsGoodCassette2Detected(OutputCassetteUnit cassette)
        {
            return cassette != null &&
                   (cassette.GoodBin8CassetteCheck1.IsOn ||
                    cassette.GoodBin12CassetteCheck1.IsOn);
        }

        private static bool IsNgCassetteDetected(OutputCassetteUnit cassette)
        {
            return cassette != null &&
                   (cassette.NgBin8CassetteCheck0.IsOn ||
                    cassette.NgBin8CassetteCheck1.IsOn ||
                    cassette.NgBin12CassetteCheck0.IsOn ||
                    cassette.NgBin12CassetteCheck1.IsOn);
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
            materialDetailView.SetRows("OUTPUT FEEDER MATERIAL", BuildFeederMaterialRows(wafer));
        }

        private WaferMaterial ResolveFeederWaferMaterial()
        {
            return MaterialStateService.GetWaferAtLocation(MaterialLocationKind.OutputFeeder);
        }

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction("Output Feeder에 Material Data를 생성하시겠습니까?"))
                    return;

                MaterialStateService.CreateWaferAtLocation(
                    MaterialLocationKind.OutputFeeder,
                    "OUTPUT-FEEDER-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    WaferMaterialState.WorkReady);
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Output Feeder Material Data 생성 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_ClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction("Output Feeder의 Material Data를 초기화하시겠습니까?"))
                    return;

                MaterialStateService.ClearWaferAtLocation(MaterialLocationKind.OutputFeeder);
                _lastMaterialDisplayKey = "";
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Output Feeder Material Data 초기화 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
            string location = wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "";

            return new[]
            {
                Row("Unit", "Output Feeder"),
                Row("Wafer ID", wafer != null ? wafer.WaferId : ""),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : ""),
                Row("TapeFrame Spec", specName),
                Row("Frame Size", spec != null ? spec.OuterDiameterMm.ToString("0.###") + " mm" : ""),
                Row("Grid", spec != null ? spec.GridX + " x " + spec.GridY : ""),
                Row("Die Spec", spec != null ? spec.DieSpecName : ""),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : ""),
                Row("Source Cassette", wafer != null ? wafer.SourceCassetteRole.ToString() : ""),
                Row("Source Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : ""),
                Row("Source Pos", wafer != null && !double.IsNaN(wafer.SourceCassetteSlotPosition) ? wafer.SourceCassetteSlotPosition.ToString("0.###") : ""),
                Row("Current Loc", location),
                Row("DieMap ObjId", wafer != null ? wafer.DieMapFrameObjId : ""),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "")
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

        private static MaterialDetailRow Row(string name, string value)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = string.IsNullOrWhiteSpace(value) ? "-" : value,
                Editable = false
            };
        }
    }
}
