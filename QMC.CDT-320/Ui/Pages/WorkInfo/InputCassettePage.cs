using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.Common.Logging;
using QMC.CDT320.Materials;
using QMC.CDT320.Sequencing;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Dialogs;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    public partial class InputCassettePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private System.Windows.Forms.Timer _refreshTimer;
        private bool _manualSequenceRunning;
        private CassetteSlotView _cassetteSlotView;
        private CassetteSlotView _cassetteSlotViewLevel2;
        private CassetteMaterialRole _selectedCassetteRole = CassetteMaterialRole.Input1;
        private int _selectedMaterialSlot = -1;
        private const string LogSource = "INPUT-CASSETTE-PAGE";

        public InputCassettePage()
        {
            try
            {
                InitializeComponent();
                BindDesignerControls();
                WireEvents();

                _refreshTimer = new System.Windows.Forms.Timer { Interval = 200 };
                _refreshTimer.Tick += (s, e) => RefreshFromMachine();
                HandleCreated += (s, e) => _refreshTimer.Start();
                HandleDestroyed += (s, e) => _refreshTimer.Stop();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-INIT", "InputCassettePage initialize failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindDesignerControls()
        {
            try
            {
                _cassetteSlotView = cassetteSlotView;
                _cassetteSlotViewLevel2 = cassetteSlotViewLevel2;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-BIND", "Designer control bind failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void WireEvents()
        {
            try
            {
                btnPrev.Click += async (s, e) => await RunMotionAction("LIFTER PREV", host => MoveSlotAsync(host, -1));
                btnNext.Click += async (s, e) => await RunMotionAction("LIFTER NEXT", host => MoveSlotAsync(host, +1));
                btnInit.Click += async (s, e) => await RunMotionAction("LIFTER INIT", LifterInitAsync);
                btnReady.Click += async (s, e) => await RunMotionAction("LIFTER READY", LifterReadyAsync);
                btnMap.Click += async (s, e) => await RunSequenceAction("LIFT WAFER MAPPING", MapAsync);
                btnLoad.Click += async (s, e) => await RunSequenceAction("LIFT WAFER LOADING", LoadAsync);
                btnUnload.Click += async (s, e) => await RunSequenceAction("LIFT WAFER UNLOADING", UnloadAsync);

                if (cassetteSlotView != null)
                    cassetteSlotView.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Input1, e.SlotIndex);
                if (cassetteSlotViewLevel2 != null)
                    cassetteSlotViewLevel2.SlotSelected += (s, e) => SelectMaterialSlot(CassetteMaterialRole.Input2, e.SlotIndex);
                if (materialDetailView != null)
                    materialDetailView.EditRequested += MaterialDetailView_EditRequested;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-EVENT", "Event bind failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Cassette", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private Form1 GetHost() => FindForm() as Form1;

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
                SetActionButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                WriteEvent("INPUT-CST-ACTION", actionName + " start");
                bool ok = await action(host);
                WriteEvent("INPUT-CST-ACTION", actionName + " result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-CST-CONDITION", actionName + " condition failed.");
                    QMC.Common.MessageDialog.Show(
                        this,
                        "Input Cassette 조건이 맞지 않아 동작을 중단했습니다.\nAlarm/Event Log를 확인하세요.",
                        "Input Cassette",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-ACTION-EX", actionName + " failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "LotPort error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (manualScope != null)
                    manualScope.Dispose();
                _manualSequenceRunning = false;
                SetActionButtonsEnabled(true);
                RefreshFromMachine();
            }
        }

        private async Task RunMotionAction(string actionName, Func<Form1, Task<int>> action)
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
                SetActionButtonsEnabled(false);
                manualScope = host.Controller.EnterManualOperation();
                WriteEvent("INPUT-CST-MOTION", actionName + " start");
                int result = await action(host);
                WriteEvent("INPUT-CST-MOTION", actionName + " result=" + result);
                if (result != 0)
                {
                    RaiseWarning("INPUT-CST-MOTION-FAIL", actionName + " result=" + result);
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패\nAlarm/Event Log를 확인하세요.", "Input Cassette", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-MOTION-EX", actionName + " failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (manualScope != null)
                    manualScope.Dispose();
                _manualSequenceRunning = false;
                SetActionButtonsEnabled(true);
                RefreshFromMachine();
            }
        }

        private bool ConfirmAction(string actionName)
        {
            try
            {
                DialogResult result = QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Input Cassette", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result == DialogResult.Yes;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-CONFIRM", "Confirm failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            try
            {
                btnPrev.Enabled = enabled;
                btnNext.Enabled = enabled;
                btnInit.Enabled = enabled;
                btnReady.Enabled = enabled;
                btnMap.Enabled = enabled;
                btnLoad.Enabled = enabled;
                btnUnload.Enabled = enabled;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-BUTTON", "Button enable failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private async Task<int> LifterInitAsync(Form1 host)
        {
            try
            {
                var cassette = host != null && host.Machine != null ? host.Machine.InputCassette : null;
                var feeder = host != null && host.Machine != null ? host.Machine.InputFeeder : null;
                if (cassette == null || feeder == null)
                    return -1;

                cassette.WaferLifterZ.ResetAlarm();
                cassette.WaferLifterZ.ServoOn();
                feeder.FeederY.ResetAlarm();
                feeder.FeederY.ServoOn();
                int lifterResult = await cassette.WaferLifterZ.HomeSearchAsync();
                if (lifterResult != 0)
                    return lifterResult;
                return await feeder.FeederY.HomeSearchAsync();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-INIT-MOVE", "Lifter init failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<int> LifterReadyAsync(Form1 host)
        {
            try
            {
                var cassette = host != null && host.Machine != null ? host.Machine.InputCassette : null;
                var feeder = host != null && host.Machine != null ? host.Machine.InputFeeder : null;
                if (cassette == null || feeder == null)
                    return -1;

                cassette.WaferLifterZ.ServoOn();
                feeder.FeederY.ServoOn();
                await Task.CompletedTask;
                return 0;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-READY-MOVE", "Lifter ready failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private async Task<bool> MapAsync(Form1 host)
        {
            try
            {
                if (!ValidateInputCassetteManualCondition(host, true))
                    return false;

                var sequence = CreateManualInputSequence(host);
                return await sequence.ExecuteMappingAsync(CancellationToken.None, false, ResolveManualMoveTimeoutMs(host)) == 0;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-MAP", "Mapping failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private async Task<bool> LoadAsync(Form1 host)
        {
            try
            {
                if (!ValidateInputCassetteManualCondition(host, false))
                    return false;

                var sequence = CreateManualInputSequence(host);
                return await sequence.ExecuteCassetteLoadingAsync(CancellationToken.None, false, ResolveManualMoveTimeoutMs(host)) == 0;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-LOAD", "Loading failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private async Task<bool> UnloadAsync(Form1 host)
        {
            try
            {
                if (!ValidateInputCassetteManualCondition(host, false))
                    return false;

                var sequence = CreateManualInputSequence(host);
                return await sequence.ExecuteCassetteUnloadingAsync(CancellationToken.None, false, ResolveManualMoveTimeoutMs(host)) == 0;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-UNLOAD", "Unloading failed: " + ex.Message);
                return false;
            }
            finally
            {
            }
        }

        private InputSequence CreateManualInputSequence(Form1 host)
        {
            try
            {
                var ctx = new MachineSequenceContext(host.Controller, new SequenceSignalBus());
                var sequence = new InputSequence(ctx);
                sequence.Configure(SequenceRunMode.Manual);
                return sequence;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void SelectMaterialSlot(CassetteMaterialRole role, int slotIndex)
        {
            try
            {
                _selectedCassetteRole = role;
                _selectedMaterialSlot = slotIndex;
                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-SLOT", "Slot select failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static bool ValidateInputCassetteManualCondition(Form1 host, bool mapping)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.InputCassette : null;
            if (cassette == null || cassette.Config == null || cassette.Recipe == null)
                return false;

            if (cassette.Config.SlotCount <= 0)
                return false;

            if (!cassette.CheckWaferCassetteMoveReady())
                return false;

            if (IsHardwareBypassed(host))
                return true;

            int cassetteSize = cassette.Config.InchSelect == 0 ? 8 : 12;
            if (!cassette.IsWaferCassetteExist(cassetteSize))
                return false;

            return !mapping || cassette.CheckWaferCassetteMappingReady();
        }

        private static int ResolveManualMoveTimeoutMs(Form1 host)
        {
            var cassette = host != null && host.Machine != null ? host.Machine.InputCassette : null;
            int configured = cassette != null ? cassette.ResolveWaferLifterZMoveTimeoutMs() : 0;
            return configured > 0 ? configured : 3000;
        }

        private static bool IsHardwareBypassed(Form1 host)
        {
            var settings = QMC.CDT320.AppSettingsStore.Current;
            return (settings != null && settings.BypassHardware) ||
                   (host != null && host.Controller != null && host.Controller.GlobalDryRun);
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
            var slot = cassette != null && cassette.Slots != null &&
                       _selectedMaterialSlot >= 0 && _selectedMaterialSlot < cassette.Slots.Count
                ? cassette.Slots[_selectedMaterialSlot]
                : null;
            var wafer = snapshot != null && snapshot.Wafers != null && slot != null && !string.IsNullOrEmpty(slot.WaferId)
                ? snapshot.Wafers.FirstOrDefault(w => w.WaferId == slot.WaferId)
                : null;

            materialDetailView.SetRows("WAFER MATERIAL", BuildWaferMaterialRows(cassette, slot, wafer));
        }

        private System.Collections.Generic.IEnumerable<MaterialDetailRow> BuildWaferMaterialRows(CassetteMaterial cassette, CassetteSlotMaterial slot, WaferMaterial wafer)
        {
            bool mapped = cassette != null && cassette.IsMapped;
            string specName = wafer != null ? wafer.TapeFrameSpecName : "";
            var spec = !string.IsNullOrEmpty(specName) ? MaterialSpecs.FindFrame(specName) : null;

            return new[]
            {
                Row("Selected", _selectedCassetteRole + " / SLOT " + (_selectedMaterialSlot + 1).ToString("00"), "", false),
                Row("Cassette ID", cassette != null ? cassette.CassetteId : "", "", false),
                Row("Cassette Mapped", cassette != null && cassette.IsMapped ? "Y" : "N", "", false),
                Row("Slot", slot != null ? (slot.HasWafer ? "HAS WAFER" : "EMPTY") : "", "", false),
                Row("Wafer ID", wafer != null ? wafer.WaferId : "", "WaferId", mapped),
                Row("Lot ID", wafer != null ? wafer.CassetteLotId : (cassette != null ? cassette.CassetteLotId : ""), "CassetteLotId", mapped),
                Row("State", wafer != null ? WaferMaterialStateText.ToDisplayName(wafer.State) : "", "State", mapped),
                Row("Location", wafer != null && wafer.CurrentLocation != null ? wafer.CurrentLocation.ToString() : "", "", false),
                Row("Cassette Role", wafer != null ? wafer.SourceCassetteRole.ToString() : "", "", false),
                Row("Cassette Slot", wafer != null && wafer.SourceSlotNumber >= 0 ? (wafer.SourceSlotNumber + 1).ToString("00") : "", "", false),
                Row("Cassette Position", wafer != null ? FormatCassettePosition(wafer.CurrentCassetteSlotPosition) : "", "", false),
                Row("TapeFrame Spec", specName, "TapeFrameSpecName", mapped),
                Row("Spec Grid X", spec != null ? spec.GridX.ToString() : "", "", false),
                Row("Spec Grid Y", spec != null ? spec.GridY.ToString() : "", "", false),
                Row("Spec Pitch X", spec != null ? spec.PitchX.ToString("0.###") : "", "", false),
                Row("Spec Pitch Y", spec != null ? spec.PitchY.ToString("0.###") : "", "", false),
                Row("Spec Diameter", spec != null ? spec.OuterDiameterMm.ToString("0.###") : "", "", false),
                Row("Die Spec", spec != null ? spec.DieSpecName : "", "", false),
                Row("Updated", wafer != null ? wafer.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss") : "", "", false)
            };
        }

        private static MaterialDetailRow Row(string name, string value, string key, bool editable)
        {
            return new MaterialDetailRow
            {
                Name = name,
                Value = value,
                Key = key,
                Editable = editable
            };
        }

        private void MaterialDetailView_EditRequested(object sender, MaterialDetailEditEventArgs e)
        {
            try
            {
                if (e == null || e.Row == null)
                    return;

                var snapshot = MaterialStorage.State;
                var cassette = snapshot != null && snapshot.Cassettes != null
                    ? snapshot.Cassettes.FirstOrDefault(c => c.Role == _selectedCassetteRole)
                    : null;

                if (cassette == null || !cassette.IsMapped)
                {
                    RaiseWarning("INPUT-CST-MATERIAL-EDIT", "Material edit requested before mapping.");
                    QMC.Common.MessageDialog.Show(this, "Mapping 완료된 Cassette Slot에서만 Material을 수정할 수 있습니다.", "Material", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string newValue;
                if (!TryEditMaterialValue(e.Row, out newValue))
                    return;

                bool ok = MaterialStateService.UpdateWaferFieldInMappedCassette(
                    _selectedCassetteRole,
                    _selectedMaterialSlot,
                    e.Row.Key,
                    newValue);

                WriteEvent("INPUT-CST-MATERIAL", e.Row.Key + " update result=" + ok);
                if (!ok)
                {
                    RaiseWarning("INPUT-CST-MATERIAL-FAIL", e.Row.Key + " update failed.");
                    QMC.Common.MessageDialog.Show(this, "Material 값을 변경하지 못했습니다.", "Material", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                RefreshSelectedMaterialDetail();
                RefreshFromMachine();
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-MATERIAL-EX", "Material edit failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Material", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool TryEditMaterialValue(MaterialDetailRow row, out string value)
        {
            value = row != null ? row.Value : "";
            if (row == null)
                return false;

            if (row.Key == "State")
            {
                var states = WaferMaterialStateText.DisplayNames;
                using (var dialog = new EnumPickerDialog("Wafer State", states, row.Value))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return false;
                    value = dialog.SelectedValue;
                    return true;
                }
            }

            if (row.Key == "TapeFrameSpecName")
            {
                var specs = MaterialSpecs.Data != null && MaterialSpecs.Data.Frames != null
                    ? MaterialSpecs.Data.Frames.Select(f => f.Name).Where(n => !string.IsNullOrEmpty(n)).ToArray()
                    : new string[0];

                using (var dialog = new EnumPickerDialog("TapeFrame Spec", specs, row.Value))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return false;
                    value = dialog.SelectedValue;
                    return true;
                }
            }

            using (var dialog = new MaterialValueEditDialog(row.Name, row.Value))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return false;
                value = dialog.ValueText;
                return true;
            }
        }

        private async Task<int> MoveSlotAsync(Form1 host, int delta)
        {
            try
            {
                var loader = host != null && host.Machine != null ? host.Machine.InputCassette : null;
                if (loader == null || loader.Config == null)
                    return -1;

                int slotCount = loader.Config.SlotCount;
                if (slotCount <= 0)
                    return -1;

                int currentSlot = _selectedMaterialSlot >= 0 ? _selectedMaterialSlot : (host.Controller != null ? host.Controller.CurrentInputSlot : -1);
                if (currentSlot < 0)
                    currentSlot = delta >= 0 ? 0 : slotCount - 1;

                int targetSlot = Math.Max(0, Math.Min(slotCount - 1, currentSlot + delta));
                if (targetSlot == currentSlot)
                    return 0;

                double targetPosition;
                if (!MaterialStateService.TryGetCassetteSlotPosition(_selectedCassetteRole, targetSlot, out targetPosition))
                {
                    RaiseWarning("INPUT-CST-SLOT-POS", "Cassette slot position data is missing. role=" + _selectedCassetteRole + ", slot=" + targetSlot);
                    return -1;
                }

                _selectedMaterialSlot = targetSlot;
                int moveResult = await loader.MoveWaferLifterZ(targetPosition, false);
                if (moveResult != 0)
                    return moveResult;

                int waitResult = await loader.WaitWaferLifterZMoveDone(ResolveManualMoveTimeoutMs(host));
                RefreshSelectedMaterialDetail();
                return waitResult;
            }
            catch (Exception ex)
            {
                WriteAlarm("INPUT-CST-SLOT-MOVE", "Slot move failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private void RefreshFromMachine()
        {
            try
            {
                var host = GetHost();
                if (host == null || host.Machine == null)
                    return;

                var loader = host.Machine.InputCassette;
                var ctrl = host.Controller;
                int slotCount = loader.Config != null && loader.Config.SlotCount > 0 ? loader.Config.SlotCount : 0;

                if (_lifterPosLabel != null)
                {
                    _lifterPosLabel.Text = AxisUnitConverter.FormatDisplay(loader.WaferLifterZ.ActualPosition, loader.WaferLifterZ, "0.###", true);
                }

                if (dotCassetteCheck1 != null)
                    dotCassetteCheck1.IsOn = IsSelectedCassetteLevel(loader, 0);
                if (dotCassetteCheck2 != null)
                    dotCassetteCheck2.IsOn = IsSelectedCassetteLevel(loader, 1);

                UpdateCassetteLevelLabels(loader);

                var map = loader.WaferMap;
                int curSlot = ResolveDisplayedSlot(ctrl);
                if (lblSlotNoValue != null)
                    lblSlotNoValue.Text = curSlot >= 0 ? GetCassetteRoleDisplay(_selectedCassetteRole) + " / " + (curSlot + 1).ToString("00") : GetCassetteRoleDisplay(_selectedCassetteRole) + " / -";
                if (lblSlotStateValue != null)
                {
                    string waferId;
                    WaferMaterialState state;
                    bool hasWafer;
                    ResolveDisplayedSlotState(_selectedCassetteRole, curSlot, map, out waferId, out state, out hasWafer);
                    Color stateColor = GetStateColor(state);
                    lblSlotStateValue.Text = BuildStateText(state, waferId, false);
                    lblSlotStateValue.BackColor = stateColor;
                    lblSlotStateValue.ForeColor = stateColor == Color.Navy ? Color.White : Color.Black;
                }

                RefreshCassetteLevelViews(loader, map, curSlot, slotCount);
                RefreshSelectedMaterialDetail();
            }
            catch (Exception ex)
            {
                WriteWarning("INPUT-CST-REFRESH", "Refresh failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void UpdateCassetteLevelLabels(QMC.CDT320.InputCassetteUnit loader)
        {
            int level = GetSelectedCassetteLevel(loader);
            if (lblCassetteCheck1 != null)
            {
                lblCassetteCheck1.Text = "1단 사용";
                lblCassetteCheck1.BackColor = Color.White;
            }
            if (lblCassetteCheck2 != null)
            {
                bool useLevel2 = level >= 2;
                lblCassetteCheck2.Text = useLevel2 ? "2단 사용" : "2단 미사용";
                lblCassetteCheck2.BackColor = useLevel2 ? Color.White : Color.FromArgb(0xD0, 0xD0, 0xD0);
            }
        }

        private int ResolveDisplayedSlot(QMC.CDT320.MachineController ctrl)
        {
            try
            {
                if (_selectedMaterialSlot >= 0)
                    return _selectedMaterialSlot;
                return ctrl != null ? ctrl.CurrentInputSlot : -1;
            }
            catch
            {
                return -1;
            }
            finally
            {
            }
        }

        private void ResolveDisplayedSlotState(
            CassetteMaterialRole role,
            int curSlot,
            System.Collections.Generic.IReadOnlyList<bool> fallbackMap,
            out string waferId,
            out WaferMaterialState state,
            out bool hasWafer)
        {
            waferId = "";
            state = WaferMaterialState.Empty;
            hasWafer = false;
            try
            {
                if (curSlot < 0)
                    return;

                if (TryGetSlotMaterial(role, curSlot, out waferId, out state, out hasWafer))
                    return;

                hasWafer = fallbackMap != null && curSlot >= 0 && curSlot < fallbackMap.Count && fallbackMap[curSlot];
                state = hasWafer ? WaferMaterialState.Ready : WaferMaterialState.Empty;
                waferId = "";
            }
            catch (Exception ex)
            {
                WriteWarning("INPUT-CST-SLOT-STATE", "Slot state resolve failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void RefreshCassetteLevelViews(QMC.CDT320.InputCassetteUnit loader, System.Collections.Generic.IReadOnlyList<bool> map, int curSlot, int slotCount)
        {
            int levelCount = GetConfiguredCassetteLevelCount(loader);
            var level1Items = BuildMaterialSlotItems(CassetteMaterialRole.Input1, slotCount, map);
            var level2Items = BuildMaterialSlotItems(CassetteMaterialRole.Input2, slotCount, null);

            ApplyCassetteLevelLayout(levelCount);
            UpdateCassetteLevelView(_cassetteSlotView, "INPUT CASSETTE 1단", true, slotCount, level1Items);
            UpdateCassetteLevelView(_cassetteSlotViewLevel2, "INPUT CASSETTE 2단", levelCount >= 2, slotCount, level2Items);
        }

        private static bool IsSelectedCassetteLevel(QMC.CDT320.InputCassetteUnit loader, int level)
        {
            if (level == 0)
                return true;
            return GetConfiguredCassetteLevelCount(loader) >= 2;
        }

        private static int GetSelectedCassetteLevel(QMC.CDT320.InputCassetteUnit loader)
        {
            return GetConfiguredCassetteLevelCount(loader);
        }

        private static int GetConfiguredCassetteLevelCount(QMC.CDT320.InputCassetteUnit loader)
        {
            int configured = loader != null && loader.Config != null ? loader.Config.SelectedCassetteLevel : 0;
            return configured >= 2 ? 2 : 1;
        }

        private void ApplyCassetteLevelLayout(int levelCount)
        {
            if (cassetteLevelLayout == null || cassetteLevelLayout.ColumnStyles.Count < 2)
                return;

            bool useLevel2 = levelCount >= 2;
            cassetteLevelLayout.ColumnStyles[0].SizeType = SizeType.Percent;
            cassetteLevelLayout.ColumnStyles[0].Width = useLevel2 ? 50F : 100F;
            cassetteLevelLayout.ColumnStyles[1].SizeType = useLevel2 ? SizeType.Percent : SizeType.Absolute;
            cassetteLevelLayout.ColumnStyles[1].Width = useLevel2 ? 50F : 0F;

            if (_cassetteSlotView != null)
                _cassetteSlotView.Visible = true;
            if (_cassetteSlotViewLevel2 != null)
                _cassetteSlotViewLevel2.Visible = useLevel2;
        }

        private static void UpdateCassetteLevelView(CassetteSlotView view, string title, bool active, int slotCount, System.Collections.Generic.IReadOnlyList<CassetteSlotDisplayItem> items)
        {
            if (view == null)
                return;

            view.Title = title;
            view.EmptyColor = Color.Lime;
            view.Enabled = active;
            view.SetSlotCount(slotCount);
            view.UpdateMaterialSlots(active ? items : null);
        }

        private static System.Collections.Generic.IReadOnlyList<CassetteSlotDisplayItem> BuildMaterialSlotItems(
            CassetteMaterialRole role,
            int slotCount,
            System.Collections.Generic.IReadOnlyList<bool> fallbackMap)
        {
            var items = new CassetteSlotDisplayItem[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                bool fallbackHasWafer = fallbackMap != null && i < fallbackMap.Count && fallbackMap[i];
                items[i] = new CassetteSlotDisplayItem
                {
                    HasWafer = fallbackHasWafer,
                    State = fallbackHasWafer ? WaferMaterialState.Ready : WaferMaterialState.Empty
                };
            }

            var snapshot = MaterialStorage.State;
            var cassette = snapshot != null && snapshot.Cassettes != null
                ? snapshot.Cassettes.FirstOrDefault(c => c.Role == role)
                : null;

            if (cassette == null || !cassette.IsMapped)
                return items;

            int count = Math.Min(slotCount, cassette.Slots != null ? cassette.Slots.Count : 0);
            for (int i = 0; i < count; i++)
            {
                var slot = cassette.Slots[i];
                var wafer = snapshot != null && snapshot.Wafers != null && slot != null && !string.IsNullOrEmpty(slot.WaferId)
                    ? snapshot.Wafers.FirstOrDefault(w => w.WaferId == slot.WaferId)
                    : null;

                WaferMaterialState state = wafer != null ? WaferMaterialStateText.Normalize(wafer.State) : WaferMaterialState.Empty;
                bool hasWafer = slot != null && slot.HasWafer && state != WaferMaterialState.Empty;
                items[i].HasWafer = hasWafer;
                items[i].WaferId = hasWafer && slot != null ? slot.WaferId : "";
                items[i].State = hasWafer ? state : WaferMaterialState.Empty;
            }

            return items;
        }

        private static bool TryGetSlotMaterial(CassetteMaterialRole role, int slotIndex, out string waferId, out WaferMaterialState state, out bool hasWafer)
        {
            waferId = "";
            state = WaferMaterialState.Empty;
            hasWafer = false;
            try
            {
                var snapshot = MaterialStorage.State;
                var cassette = snapshot != null && snapshot.Cassettes != null
                    ? snapshot.Cassettes.FirstOrDefault(c => c.Role == role)
                    : null;
                var slot = cassette != null && cassette.Slots != null && slotIndex >= 0 && slotIndex < cassette.Slots.Count
                    ? cassette.Slots[slotIndex]
                    : null;
                if (slot == null)
                    return false;

                var wafer = snapshot != null && snapshot.Wafers != null && !string.IsNullOrEmpty(slot.WaferId)
                    ? snapshot.Wafers.FirstOrDefault(w => w.WaferId == slot.WaferId)
                    : null;
                waferId = slot.WaferId ?? "";
                state = wafer != null ? WaferMaterialStateText.Normalize(wafer.State) : (slot.HasWafer ? WaferMaterialState.Ready : WaferMaterialState.Empty);
                hasWafer = slot.HasWafer && state != WaferMaterialState.Empty;
                return cassette != null && cassette.IsMapped;
            }
            catch
            {
                return false;
            }
            finally
            {
            }
        }

        private static string GetCassetteRoleDisplay(CassetteMaterialRole role)
        {
            try
            {
                return role == CassetteMaterialRole.Input2 ? "2단" : "1단";
            }
            catch
            {
                return "1단";
            }
            finally
            {
            }
        }

        private static string BuildStateText(WaferMaterialState state, string waferId, bool includeWaferId)
        {
            try
            {
                WaferMaterialState normalized = WaferMaterialStateText.Normalize(state);
                string stateText = WaferMaterialStateText.ToDisplayName(normalized);
                if (!includeWaferId || normalized == WaferMaterialState.Empty || string.IsNullOrWhiteSpace(waferId))
                    return stateText;
                return stateText + " / " + waferId;
            }
            catch
            {
                return "EMPTY";
            }
            finally
            {
            }
        }

        private static string FormatCassettePosition(double position)
        {
            try
            {
                if (double.IsNaN(position))
                    return "";
                return position.ToString("0.###") + " mm";
            }
            catch
            {
                return "";
            }
            finally
            {
            }
        }

        private static Color GetStateColor(WaferMaterialState state)
        {
            try
            {
                switch (WaferMaterialStateText.Normalize(state))
                {
                    case WaferMaterialState.Ready:
                        return Color.Cyan;
                    case WaferMaterialState.WorkReady:
                        return Color.Navy;
                    case WaferMaterialState.Working:
                        return Color.Orange;
                    case WaferMaterialState.Finish:
                        return Color.Red;
                    default:
                        return Color.Lime;
                }
            }
            catch
            {
                return Color.Lime;
            }
            finally
            {
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
            }
            catch (Exception ex)
            {
                WriteWarning("INPUT-CST-DISPOSE", "Dispose failed: " + ex.Message);
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }

        private static void WriteEvent(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Event, "UI", code, message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void WriteWarning(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Warning, "UI", code, message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void WriteAlarm(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Alarm, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Warning, code, LogSource, message);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static void RaiseWarning(string code, string message)
        {
            try
            {
                EventLogger.Write(EventKind.Warning, "UI", code, message);
                AlarmManager.Raise(AlarmSeverity.Warning, code, LogSource, message);
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}


