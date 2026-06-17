using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class OutputCassettePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private Timer _timer;
        private bool _manualSequenceRunning;
        private CassetteMaterialRole _selectedCassetteRole = CassetteMaterialRole.Good1;
        private int _selectedMaterialSlot = -1;

        public OutputCassettePage()
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
            btnPrev.Click += async (s, e) => await RunMotionAction("LIFTER PREV", host => MoveSlotAsync(host, -1));
            btnNext.Click += async (s, e) => await RunMotionAction("LIFTER NEXT", host => MoveSlotAsync(host, 1));
            btnInit.Click += async (s, e) => await RunMotionAction("LIFTER INIT", LifterInitAsync);
            btnReady.Click += async (s, e) => await RunMotionAction("LIFTER READY", LifterReadyAsync);
            btnMap.Click += async (s, e) => await RunSequenceAction("LIFT BIN MAPPING", MapAsync);
            btnLoad.Click += async (s, e) => await RunSequenceAction("LIFT BIN LOADING", LoadAsync);
            btnUnload.Click += async (s, e) => await RunSequenceAction("LIFT BIN UNLOADING", UnloadAsync);
            btnStop.Click += async (s, e) => await StopManualActionAsync();
            actionPanel.Resize += (s, e) => AlignStopButton();

            _good1CassetteView.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Good1, e.SlotIndex);
            _good2CassetteView.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Good2, e.SlotIndex);
            _ngCassetteView.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Ng1, e.SlotIndex);
            _good1CassetteView.SlotMoveRequested += async (s, e) => await MoveSlotFromContextMenuAsync(CassetteMaterialRole.Good1, e.SlotIndex);
            _good2CassetteView.SlotMoveRequested += async (s, e) => await MoveSlotFromContextMenuAsync(CassetteMaterialRole.Good2, e.SlotIndex);
            _ngCassetteView.SlotMoveRequested += async (s, e) => await MoveSlotFromContextMenuAsync(CassetteMaterialRole.Ng1, e.SlotIndex);
            materialDetailView.CreateDataRequested += MaterialDetailView_CreateDataRequested;
            materialDetailView.ClearDataRequested += MaterialDetailView_ClearDataRequested;
            materialDetailView.ClearAllDataRequested += MaterialDetailView_ClearAllDataRequested;

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
                var host = GetHost();
                if (host == null || host.Controller == null || action == null)
                    return;
                if (_manualSequenceRunning)
                    return;
                if (!ConfirmAction(actionName))
                    return;

                _manualSequenceRunning = true;
                SetActionButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                SequenceFailureStore.Clear();
                bool ok = await action(host);
                if (!ok)
                {
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }
            finally
            {
                if (manualScope != null)
                    manualScope.Dispose();
                _manualSequenceRunning = false;
                SetActionButtonsEnabled(true);
                RefreshData();
            }

            if (showFailure)
            {
                QMC.Common.MessageDialog.Show(
                    this,
                    SequenceFailureStore.BuildManualFailureMessage(actionName, actionName + " 실패\r\nAlarm/Event Log를 확인하세요."),
                    "Output Cassette",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, "Output Cassette error:\r\n" + exceptionMessage, "Output Cassette", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async Task RunMotionAction(string actionName, Func<Form1, Task<int>> action)
        {
            IDisposable manualScope = null;
            bool showFailure = false;
            string failureMessage = null;
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

                _manualSequenceRunning = true;
                SetActionButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                SequenceFailureStore.Clear();
                int result = await action(host);
                if (result != 0)
                {
                    failureMessage = SequenceFailureStore.BuildManualFailureMessage(
                        actionName,
                        actionName + " failed. result=" + result + "\r\nAlarm/Event Log를 확인하세요.");
                    showFailure = true;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }
            finally
            {
                if (manualScope != null)
                    manualScope.Dispose();
                _manualSequenceRunning = false;
                SetActionButtonsEnabled(true);
                RefreshData();
            }

            if (showFailure)
                QMC.Common.MessageDialog.Show(this, failureMessage, "Output Cassette", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
                QMC.Common.MessageDialog.Show(this, exceptionMessage, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool ConfirmAction(string actionName)
        {
            return QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Output Cassette", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            btnPrev.Enabled = enabled;
            btnNext.Enabled = enabled;
            btnInit.Enabled = enabled;
            btnReady.Enabled = enabled;
            btnMap.Enabled = enabled;
            btnLoad.Enabled = enabled;
            btnUnload.Enabled = enabled;
            actionPanel.Enabled = true;
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

                await host.Controller.StopAsync();
            }
            catch
            {
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

        private async Task<int> LifterInitAsync(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            if (cassette == null || cassette.OutputLifterZ == null)
                return -1;

            cassette.OutputLifterZ.ResetAlarm();
            cassette.OutputLifterZ.ServoOn();
            return await cassette.OutputLifterZ.HomeSearchAsync();
        }

        private async Task<int> LifterReadyAsync(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            if (cassette == null || cassette.OutputLifterZ == null)
                return -1;

            cassette.OutputLifterZ.ServoOn();
            await Task.CompletedTask;
            return 0;
        }

        private async Task<bool> MapAsync(Form1 host)
        {
            var sequence = CreateOutputCassetteSequence(host);
            return await sequence.RunMappingAsync(host.Controller.ManualOperationToken, BuildCassetteOptions(host, SequenceStartMode.Resume)) == 0;
        }

        private async Task<bool> LoadAsync(Form1 host)
        {
            var sequence = CreateOutputCassetteSequence(host);
            return await sequence.RunLoadingAsync(host.Controller.ManualOperationToken, BuildCassetteOptions(host, SequenceStartMode.Resume)) == 0;
        }

        private async Task<bool> UnloadAsync(Form1 host)
        {
            var sequence = CreateOutputCassetteSequence(host);
            return await sequence.RunUnloadingAsync(host.Controller.ManualOperationToken, BuildCassetteOptions(host, SequenceStartMode.Resume)) == 0;
        }

        private OutputCassetteSequence CreateOutputCassetteSequence(Form1 host)
        {
            var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
            return new OutputCassetteSequence(ctx);
        }

        private OutputCassetteSequenceOptions BuildCassetteOptions(Form1 host, SequenceStartMode startMode)
        {
            var options = OutputCassetteSequenceOptions.Default();
            options.RunMode = SequenceRunMode.Manual;
            options.StartMode = startMode;
            options.MoveTimeoutMs = ResolveManualMoveTimeoutMs(host);
            options.FineMove = false;
            options.TargetCassette = ResolveTargetCassette(_selectedCassetteRole);
            options.SlotIndex = _selectedMaterialSlot >= 0 ? _selectedMaterialSlot : 0;
            options.GoodLevelCount = host != null && host.Machine != null && host.Machine.OutputCassetteUnit != null && host.Machine.OutputCassetteUnit.Config != null
                ? Math.Max(1, Math.Min(2, host.Machine.OutputCassetteUnit.Config.SelectedCassetteLevel))
                : 2;
            return options;
        }

        private async Task<int> MoveSlotAsync(Form1 host, int delta)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            if (cassette == null || cassette.Config == null)
                return -1;

            int slotCount = cassette.Config.SlotCount;
            if (slotCount <= 0)
                return -1;

            int currentSlot = _selectedMaterialSlot >= 0 ? _selectedMaterialSlot : 0;
            int targetSlot = Math.Max(0, Math.Min(slotCount - 1, currentSlot + delta));
            if (targetSlot == currentSlot)
                return 0;

            SelectMaterialSlot(_selectedCassetteRole, targetSlot);
            var sequence = CreateOutputCassetteSequence(host);
            var options = BuildCassetteOptions(host, SequenceStartMode.Resume);
            options.SlotIndex = targetSlot;
            return await sequence.RunMoveSlotAsync(host.Controller.ManualOperationToken, options);
        }

        private void SelectMaterialSlot(CassetteMaterialRole role, int slotIndex)
        {
            _selectedCassetteRole = role;
            _selectedMaterialSlot = slotIndex;
            RefreshSelectedMaterialDetail();
            RefreshSelectedSlotState();
        }

        private async Task MoveSlotFromContextMenuAsync(CassetteMaterialRole role, int slotIndex)
        {
            SelectMaterialSlot(role, slotIndex);
            string actionName = "LIFT BIN MOVE " + GetCassetteRoleDisplay(role) + " / " + (slotIndex + 1).ToString("00");
            await RunMotionAction(actionName, host => MoveSpecificSlotAsync(host, role, slotIndex));
        }

        private async Task<int> MoveSpecificSlotAsync(Form1 host, CassetteMaterialRole role, int slotIndex)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            if (cassette == null || cassette.Config == null)
                return -1;

            if (slotIndex < 0 || slotIndex >= cassette.Config.SlotCount)
                return -1;

            SelectMaterialSlot(role, slotIndex);
            var sequence = CreateOutputCassetteSequence(host);
            var options = BuildCassetteOptions(host, SequenceStartMode.Resume);
            options.TargetCassette = ResolveTargetCassette(role);
            options.SlotIndex = slotIndex;
            return await sequence.RunMoveSlotAsync(host.Controller.ManualOperationToken, options);
        }

        private void RefreshData()
        {
            var host = GetHost();
            if (host?.Machine == null) return;

            var outputCassette = host.Machine.OutputCassetteUnit;
            lblElevatorPos.Text = AxisUnitConverter.FormatDisplay(outputCassette.OutputLifterZ.ActualPosition, outputCassette.OutputLifterZ, "0.###", true);
            dotGood1Check.IsOn = outputCassette.GoodBin8CassetteCheck0.IsOn || outputCassette.GoodBin12CassetteCheck0.IsOn;
            dotGood2Check.IsOn = outputCassette.GoodBin8CassetteCheck1.IsOn || outputCassette.GoodBin12CassetteCheck1.IsOn;
            dotNgCheck.IsOn = outputCassette.NgBin8CassetteCheck0.IsOn || outputCassette.NgBin8CassetteCheck1.IsOn ||
                              outputCassette.NgBin12CassetteCheck0.IsOn || outputCassette.NgBin12CassetteCheck1.IsOn;

            var driver = host.CassetteDriver;
            int slotCount = outputCassette != null && outputCassette.Config != null && outputCassette.Config.SlotCount > 0
                ? outputCassette.Config.SlotCount
                : 0;

            UpdateMaterialView(_good1CassetteView, slotCount, CassetteMaterialRole.Good1, ResolveSlots(outputCassette, TargetCassette.Good1, driver != null ? driver.OutputGood1Slots : null));
            UpdateMaterialView(_good2CassetteView, slotCount, CassetteMaterialRole.Good2, ResolveSlots(outputCassette, TargetCassette.Good2, driver != null ? driver.OutputGood2Slots : null));
            UpdateMaterialView(_ngCassetteView, slotCount, CassetteMaterialRole.Ng1, ResolveSlots(outputCassette, TargetCassette.Ng, driver != null ? driver.OutputNgSlots : null));

            RefreshSelectedSlotState();
            RefreshSelectedMaterialDetail();
        }

        private void RefreshSelectedSlotState()
        {
            lblSlotNoValue.Text = _selectedMaterialSlot >= 0
                ? GetCassetteRoleDisplay(_selectedCassetteRole) + " / " + (_selectedMaterialSlot + 1).ToString("00")
                : GetCassetteRoleDisplay(_selectedCassetteRole) + " / -";

            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c.Role == _selectedCassetteRole)
                : null;
            var slot = cassette != null && cassette.Slots != null && _selectedMaterialSlot >= 0 && _selectedMaterialSlot < cassette.Slots.Count
                ? cassette.Slots[_selectedMaterialSlot]
                : null;
            var wafer = ResolveCassetteSlotWafer(snapshot, _selectedCassetteRole, _selectedMaterialSlot, slot);
            WaferMaterialState state = wafer != null ? WaferMaterialStateText.Normalize(wafer.State) : WaferMaterialState.Empty;
            lblSlotStateValue.Text = wafer != null ? WaferMaterialStateText.ToDisplayName(state) : "-";
            lblSlotStateValue.BackColor = ResolveStateColor(state);
        }

        private void RefreshSelectedMaterialDetail()
        {
            if (materialDetailView == null)
                return;

            if (_selectedMaterialSlot < 0)
            {
                materialDetailView.Clear();
                return;
            }

            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c.Role == _selectedCassetteRole)
                : null;
            var slot = cassette != null && cassette.Slots != null && _selectedMaterialSlot >= 0 && _selectedMaterialSlot < cassette.Slots.Count
                ? cassette.Slots[_selectedMaterialSlot]
                : null;
            var wafer = ResolveCassetteSlotWafer(snapshot, _selectedCassetteRole, _selectedMaterialSlot, slot);

            materialDetailView.SetRows("WAFER MATERIAL", BuildWaferMaterialRows(cassette, slot, wafer));
        }

        private IEnumerable<MaterialDetailRow> BuildWaferMaterialRows(CassetteMaterial cassette, CassetteSlotMaterial slot, WaferMaterial wafer)
        {
            return new[]
            {
                Row("Selected", GetCassetteRoleDisplay(_selectedCassetteRole) + " / SLOT " + (_selectedMaterialSlot + 1).ToString("00")),
                Row("Cassette ID", cassette != null ? cassette.CassetteId : ""),
                Row("Cassette Mapped", cassette != null && cassette.IsMapped ? "Y" : "N"),
                Row("Slot", BuildSlotOccupancyText(slot, wafer)),
                Row("Wafer ID", wafer != null ? wafer.WaferId : ""),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : (cassette != null ? cassette.CassetteLotId : "")),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : ""),
                Row("Location", wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : ""),
                Row("Cassette Role", wafer != null ? wafer.SourceCassetteRole.ToString() : ""),
                Row("Cassette Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : ""),
                Row("Cassette Position", wafer != null ? FormatCassettePosition(wafer.CurrentCassetteSlotPosition) : ""),
                Row("TapeFrame Spec", wafer != null ? wafer.TapeFrameSpecName : ""),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "")
            };
        }

        private void MaterialDetailView_CreateDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (_selectedMaterialSlot < 0)
                    return;
                if (!ConfirmMaterialDataAction("선택한 Output Cassette Slot에 Material Data를 생성하시겠습니까?"))
                    return;

                string waferId = BuildGeneratedOutputWaferId(_selectedCassetteRole, _selectedMaterialSlot);
                MaterialStateService.PutWaferInCassette(waferId, _selectedCassetteRole, _selectedMaterialSlot, ResolveCassetteLotId(_selectedCassetteRole), double.NaN, WaferMaterialState.Ready);
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Material Data 생성 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_ClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (_selectedMaterialSlot < 0)
                    return;
                if (!ConfirmMaterialDataAction("선택한 Output Cassette Slot의 Material Data를 초기화하시겠습니까?"))
                    return;

                MaterialStateService.ClearOutputCassetteSlotData(_selectedCassetteRole, _selectedMaterialSlot);
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Material Data 초기화 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void MaterialDetailView_ClearAllDataRequested(object sender, EventArgs e)
        {
            try
            {
                if (!ConfirmMaterialDataAction("Output Cassette의 모든 Material Data를 초기화하시겠습니까?"))
                    return;

                MaterialStateService.ClearOutputCassetteAllSlotData();
                RefreshData();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, "Material Data 전체 초기화 실패:\r\n" + ex.Message, "Material Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool ConfirmMaterialDataAction(string message)
        {
            return QMC.Common.MessageDialog.Show(this, message, "Material Data", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private static IReadOnlyList<bool> ResolveSlots(OutputCassetteUnit unit, TargetCassette cassette, bool[] fallback)
        {
            if (unit != null && unit.SlotMap != null)
            {
                bool[] map;
                if (unit.SlotMap.TryGetValue(cassette, out map) && map != null && map.Length > 0)
                    return map;
            }

            return fallback;
        }

        private static void UpdateMaterialView(CassetteSlotView view, int slotCount, CassetteMaterialRole role, IReadOnlyList<bool> fallbackMap)
        {
            if (view == null)
                return;

            if (slotCount <= 0 && fallbackMap != null)
                slotCount = fallbackMap.Count;

            view.SetSlotCount(slotCount);
            view.UpdateMaterialSlots(BuildMaterialSlotItems(role, slotCount, fallbackMap));
        }

        private static IReadOnlyList<CassetteSlotDisplayItem> BuildMaterialSlotItems(CassetteMaterialRole role, int slotCount, IReadOnlyList<bool> fallbackMap)
        {
            var items = new List<CassetteSlotDisplayItem>();
            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c.Role == role)
                : null;

            if (cassette != null)
            {
                cassette.EnsureSlots();
                if (slotCount <= 0)
                    slotCount = cassette.Slots.Count;
            }

            for (int i = 0; i < slotCount; i++)
            {
                bool fallbackHasWafer = fallbackMap != null && i < fallbackMap.Count && fallbackMap[i];
                CassetteSlotMaterial slot = cassette != null && cassette.Slots != null && i < cassette.Slots.Count
                    ? cassette.Slots[i]
                    : null;
                WaferMaterial wafer = ResolveCassetteSlotWafer(snapshot, role, i, slot);
                WaferMaterialState state = wafer != null ? WaferMaterialStateText.Normalize(wafer.State) : WaferMaterialState.Empty;
                bool hasWafer = ((slot != null && slot.HasWafer) || IsWaferInOutputTransferLocation(wafer) || fallbackHasWafer) &&
                                state != WaferMaterialState.Empty;

                items.Add(new CassetteSlotDisplayItem
                {
                    IsKnown = cassette != null || fallbackMap != null,
                    HasWafer = hasWafer,
                    WaferId = hasWafer && wafer != null ? wafer.WaferId : "",
                    State = hasWafer ? (wafer != null ? state : WaferMaterialState.Ready) : WaferMaterialState.Empty
                });
            }

            return items;
        }

        private static WaferMaterial ResolveCassetteSlotWafer(MaterialSnapshot snapshot, CassetteMaterialRole role, int slotIndex, CassetteSlotMaterial slot)
        {
            if (snapshot == null || snapshot.Wafers == null || slotIndex < 0)
                return null;

            if (slot != null && !string.IsNullOrWhiteSpace(slot.WaferId))
            {
                WaferMaterial slotWafer = snapshot.Wafers.FirstOrDefault(w => string.Equals(w.WaferId, slot.WaferId, StringComparison.OrdinalIgnoreCase));
                if (slotWafer != null)
                    return slotWafer;
            }

            return snapshot.Wafers.FirstOrDefault(w =>
                w != null &&
                w.SourceCassetteRole == role &&
                w.SourceSlotNumber == slotIndex &&
                WaferMaterialStateText.Normalize(w.State) != WaferMaterialState.Empty &&
                IsWaferInOutputTransferLocation(w));
        }

        private static bool IsWaferInOutputTransferLocation(WaferMaterial wafer)
        {
            if (wafer == null || wafer.CurrentLocation == null)
                return false;

            return wafer.CurrentLocation.Kind == MaterialLocationKind.OutputFeeder ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageGood ||
                   wafer.CurrentLocation.Kind == MaterialLocationKind.OutputStageNg;
        }

        private static TargetCassette ResolveTargetCassette(CassetteMaterialRole role)
        {
            if (role == CassetteMaterialRole.Good2)
                return TargetCassette.Good2;
            if (role == CassetteMaterialRole.Ng1)
                return TargetCassette.Ng;
            return TargetCassette.Good1;
        }

        private static string GetCassetteRoleDisplay(CassetteMaterialRole role)
        {
            if (role == CassetteMaterialRole.Good2)
                return "GOOD2";
            if (role == CassetteMaterialRole.Ng1)
                return "NG";
            return "GOOD1";
        }

        private static int ResolveManualMoveTimeoutMs(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.OutputCassetteUnit : null;
            int configured = cassette != null && cassette.OutputLifterZ != null && cassette.OutputLifterZ.Setup != null ? cassette.OutputLifterZ.Setup.MoveTimeoutMs : 0;
            return configured > 0 ? configured : 3000;
        }

        private static string BuildSlotOccupancyText(CassetteSlotMaterial slot, WaferMaterial wafer)
        {
            if (wafer != null)
                return WaferMaterialStateText.ToDisplayName(wafer.State);
            if (slot != null && slot.HasWafer)
                return "HAS WAFER";
            return "EMPTY";
        }

        private static string FormatCassettePosition(double value)
        {
            return double.IsNaN(value) ? "" : value.ToString("0.###") + " mm";
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

        private static Color ResolveStateColor(WaferMaterialState state)
        {
            switch (WaferMaterialStateText.Normalize(state))
            {
                // READY 슬롯 색상
                case WaferMaterialState.Ready:
                    return Color.Cyan;
                // WORKING 슬롯 색상
                case WaferMaterialState.Working:
                    return Color.Orange;
                // FINISH 슬롯 색상
                case WaferMaterialState.Finish:
                    return Color.Red;
                // WORK READY 슬롯 색상
                case WaferMaterialState.WorkReady:
                    return Color.Navy;
                default:
                    return Color.Lime;
            }
        }

        private static string BuildGeneratedOutputWaferId(CassetteMaterialRole role, int slot)
        {
            return role.ToString().ToUpperInvariant() + "-S" + (slot + 1).ToString("00");
        }

        private static string ResolveCassetteLotId(CassetteMaterialRole role)
        {
            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null ? snapshot.Cassettes.FirstOrDefault(c => c.Role == role) : null;
            return cassette != null ? cassette.CassetteLotId : "";
        }
    }
}
