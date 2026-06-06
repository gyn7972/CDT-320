using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public sealed partial class FrontPickerRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private sealed class PositionItem
        {
            public string DisplayName;
            public PickerAxis Axis;
            public string PositionName;
        }

        private readonly Timer refreshTimer = new Timer();
        private readonly Dictionary<string, PositionItem> positionItems = new Dictionary<string, PositionItem>(StringComparer.OrdinalIgnoreCase);
        private PickerFrontUnit unit;

        public FrontPickerRecipePage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            BackColor = Color.FromArgb(207, 210, 214);
            ForeColor = Color.Black;
            refreshTimer.Interval = 250;
            refreshTimer.Tick += delegate { RefreshView(); };
            optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
            waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
            optionParameterGrid.ParameterRowDoubleClicked += OptionParameterGrid_RowDoubleClicked;
            BindParameterGridMenus();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            lblHeader.Tag = "i18n:recipe.frontHead";
            lblHeader.Text = Lang.T("recipe.frontHead");
            ResolveUnit();
            BindParameterGrids();
            BindIoPanel();
            BindJogPanel();
            BindActions();
            RefreshView();
            refreshTimer.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                refreshTimer.Stop();
                if (jogAxisMoveControl != null)
                    jogAxisMoveControl.StopAllAsync(true).GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                base.OnHandleDestroyed(e);
            }
        }

        private void ResolveUnit()
        {
            CDT320_Machine machine = FindMachine();
            unit = machine != null ? machine.PickerFrontUnit : null;
            EnsureData();

            bool enabled = unit != null;
            optionParameterGrid.Enabled = enabled;
            waitParameterGrid.Enabled = enabled;
            ioCylinderPanel.Enabled = enabled;
            jogPositionListControl.Enabled = enabled;
            jogAxisMoveControl.Enabled = enabled;
            jogSpeedControl.Enabled = enabled;
            manualPanel.Enabled = enabled;
        }

        private void EnsureData()
        {
            if (unit == null)
                return;

            unit.Config.EnsureArrays();
            unit.Recipe.EnsurePositionObjects();
        }

        private void BindActions()
        {
            manualPanel.Controls.Clear();
            AddActionButton("AVOID MOVE", delegate { return unit.MoveToPickerAvoidPosition(IsFineMove()); });
            AddActionButton("LOAD MOVE", delegate { return unit.MoveToPickerLoadPosition(IsFineMove()); });
            AddActionButton("UNLOAD MOVE", delegate { return unit.MoveToPickerUnloadPosition(IsFineMove()); });
            AddActionButton("SAFE RETREAT", delegate { return unit.MoveToPickerSafeRetreatPosition(IsFineMove()); });

            for (int pickerNo = 1; pickerNo <= 4; pickerNo++)
            {
                int captured = pickerNo;
                AddActionButton("P" + captured + " DIE PICK", delegate { return unit.MoveToPickerDiePickPosition(captured, IsFineMove()); });
                AddActionButton("P" + captured + " DIE PROCESS", delegate { return unit.MoveToPickerDieProcessPosition(captured, IsFineMove()); });
                AddActionButton("P" + captured + " DIE PLACE", delegate { return unit.MoveToPickerDiePlacePosition(captured, IsFineMove()); });
            }
        }

        private void AddActionButton(string text, Func<Task<int>> command)
        {
            ActionButton button = new ActionButton();
            button.Text = text;
            button.Width = 260;
            button.Height = 34;
            button.Margin = new Padding(4);
            button.BackColor = Color.FromArgb(88, 94, 103);
            button.ForeColor = Color.White;
            button.Font = new Font("Malgun Gothic", 8F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.Click += async delegate { await ConfirmMoveAsync(text, command); };
            manualPanel.Controls.Add(button);
        }

        private void BindParameterGrids()
        {
            if (unit == null)
                return;

            EnsureData();
            positionItems.Clear();

            List<ParameterGridItem> optionItems = new List<ParameterGridItem>();
            AddAxisPositionItems(optionItems, PickerAxis.PickerX, "PICKER X", "um");
            AddAxisPositionItems(optionItems, PickerAxis.PickerY, "PICKER Y", "um");
            AddAxisPositionItems(optionItems, PickerAxis.PickerT0, "PICKER T", "deg");
            AddAxisPositionItems(optionItems, PickerAxis.PickerZ0, "PICKER Z", "um");
            optionItems.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
            optionItems.Add(ParameterGridItem.Double("INPUT SAFETY OFFSET", "um", ParameterGridScope.Setup, () => unit.Setup.InputSafetyOffset * 1000.0, v => unit.Setup.InputSafetyOffset = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("OUTPUT SAFETY OFFSET", "um", ParameterGridScope.Setup, () => unit.Setup.OutputSafetyOffset * 1000.0, v => unit.Setup.OutputSafetyOffset = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("ARM INPUT X", "um", ParameterGridScope.Setup, () => unit.Setup.ArmInputPositionX * 1000.0, v => unit.Setup.ArmInputPositionX = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("ARM INSPECTION X", "um", ParameterGridScope.Setup, () => unit.Setup.ArmInspectionPositionX * 1000.0, v => unit.Setup.ArmInspectionPositionX = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("ARM OUTPUT X", "um", ParameterGridScope.Setup, () => unit.Setup.ArmOutputPositionX * 1000.0, v => unit.Setup.ArmOutputPositionX = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("ARM Y PICKUP", "um", ParameterGridScope.Setup, () => unit.Setup.ArmYPickupPosition * 1000.0, v => unit.Setup.ArmYPickupPosition = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("ARM Y AVOID", "um", ParameterGridScope.Setup, () => unit.Setup.ArmYAvoidPosition * 1000.0, v => unit.Setup.ArmYAvoidPosition = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("PICKER PITCH X", "um", ParameterGridScope.Setup, () => unit.Setup.PickerPitchX * 1000.0, v => unit.Setup.PickerPitchX = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("SIDE VISION 1 X", "um", ParameterGridScope.Setup, () => unit.Setup.SideVision1X * 1000.0, v => unit.Setup.SideVision1X = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("SIDE VISION 1 Y", "um", ParameterGridScope.Setup, () => unit.Setup.SideVision1Y * 1000.0, v => unit.Setup.SideVision1Y = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("SIDE VISION Y0", "um", ParameterGridScope.Setup, () => unit.Setup.SideVisionY0 * 1000.0, v => unit.Setup.SideVisionY0 = v / 1000.0));
            optionItems.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));
            AddPickerConfigItems(optionItems);
            optionParameterGrid.SetItems(optionItems);

            waitParameterGrid.SetItems(new[]
            {
                ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.MoveTimeoutMs, v => unit.Recipe.MoveTimeoutMs = Math.Max(0, v)),
                ParameterGridItem.Int("I/O TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.IoTimeoutMs, v => unit.Recipe.IoTimeoutMs = Math.Max(0, v)),
                ParameterGridItem.Int("BLOW TIME", "ms", ParameterGridScope.Recipe, () => unit.Recipe.BlowTimeMs, v => unit.Recipe.BlowTimeMs = Math.Max(0, v)),
                ParameterGridItem.Int("VACUUM SETTLE", "ms", ParameterGridScope.Recipe, () => unit.Recipe.VacuumSettleMs, v => unit.Recipe.VacuumSettleMs = Math.Max(0, v)),
                ParameterGridItem.Int("PICK LIFT WAIT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.PickLiftWaitMs, v => unit.Recipe.PickLiftWaitMs = Math.Max(0, v)),
                ParameterGridItem.Int("PLACE DELAY", "ms", ParameterGridScope.Recipe, () => unit.Recipe.PlaceDelayMs, v => unit.Recipe.PlaceDelayMs = Math.Max(0, v)),
                ParameterGridItem.Double("ARM X VELOCITY", "um/s", ParameterGridScope.Recipe, () => unit.Recipe.ArmXVelocity * 1000.0, v => unit.Recipe.ArmXVelocity = v / 1000.0),
                ParameterGridItem.Double("ARM Y VELOCITY", "um/s", ParameterGridScope.Recipe, () => unit.Recipe.ArmYVelocity * 1000.0, v => unit.Recipe.ArmYVelocity = v / 1000.0),
                ParameterGridItem.Double("PICKER Z VELOCITY", "um/s", ParameterGridScope.Recipe, () => unit.Recipe.PickerZVelocity * 1000.0, v => unit.Recipe.PickerZVelocity = v / 1000.0),
                ParameterGridItem.Double("PICKER T VELOCITY", "deg/s", ParameterGridScope.Recipe, () => unit.Recipe.PickerTVelocity, v => unit.Recipe.PickerTVelocity = v),
                ParameterGridItem.Double("PICK LIFT POSITION", "um", ParameterGridScope.Recipe, () => unit.Recipe.PickLiftPosition * 1000.0, v => unit.Recipe.PickLiftPosition = v / 1000.0)
            });
        }

        private void AddPickerConfigItems(List<ParameterGridItem> items)
        {
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                string name = "PICKER " + (index + 1);
                items.Add(ParameterGridItem.Bool(name + " USE", ParameterGridScope.Config, () => unit.Config.UsePicker[index], v => unit.Config.UsePicker[index] = v));
                items.Add(ParameterGridItem.Double(name + " OFFSET X", "um", ParameterGridScope.Config, () => unit.Config.Picker[index].AlignOffsetX * 1000.0, v => unit.Config.Picker[index].AlignOffsetX = v / 1000.0));
                items.Add(ParameterGridItem.Double(name + " OFFSET Y", "um", ParameterGridScope.Config, () => unit.Config.Picker[index].AlignOffsetY * 1000.0, v => unit.Config.Picker[index].AlignOffsetY = v / 1000.0));
                items.Add(ParameterGridItem.Double(name + " OFFSET T", "deg", ParameterGridScope.Config, () => unit.Config.Picker[index].AlignOffsetT, v => unit.Config.Picker[index].AlignOffsetT = v));
            }
        }

        private void AddAxisPositionItems(List<ParameterGridItem> items, PickerAxis axis, string axisName, string displayUnit)
        {
            AddPositionItem(items, axis, axisName, "INPUT AVOID POSITION", "InputAvoidPosition", displayUnit);
            AddPositionItem(items, axis, axisName, "OUTPUT AVOID POSITION", "OutputAvoidPosition", displayUnit);
            AddPositionItem(items, axis, axisName, "AVOID POSITION", "AvoidPosition", displayUnit);
            AddPositionItem(items, axis, axisName, "PICK POSITION", "PickPosition", displayUnit);
            AddPositionItem(items, axis, axisName, "BOTTOM POSITION", "BottomPosition", displayUnit);
            AddPositionItem(items, axis, axisName, "SIDE POSITION", "SidePosition", displayUnit);
            AddPositionItem(items, axis, axisName, "PLACE POSITION", "PlacePosition", displayUnit);
            for (int i = 0; i < 4; i++)
            {
                string pickerLabel = "P" + (i + 1) + " ";
                AddPositionItem(items, axis, axisName, pickerLabel + "DIE PICK POSITION", "DiePickPosition[" + i + "]", displayUnit);
                AddPositionItem(items, axis, axisName, pickerLabel + "DIE BOTTOM POSITION", "DieBottomPosition[" + i + "]", displayUnit);
                AddPositionItem(items, axis, axisName, pickerLabel + "DIE SIDE POSITION", "DieSidePosition[" + i + "]", displayUnit);
                AddPositionItem(items, axis, axisName, pickerLabel + "DIE PLACE POSITION", "DiePlacePosition[" + i + "]", displayUnit);
            }
        }

        private void AddPositionItem(List<ParameterGridItem> items, PickerAxis axis, string axisName, string displaySuffix, string positionName, string displayUnit)
        {
            string display = axisName + " " + displaySuffix;
            positionItems[display] = new PositionItem { DisplayName = display, Axis = axis, PositionName = positionName };
            if (displayUnit == "deg")
                items.Add(ParameterGridItem.Double(display, "deg", ParameterGridScope.Recipe, () => unit.GetPickerTeachingPosition(axis, positionName), v => SetPosition(axis, positionName, v)));
            else
                items.Add(ParameterGridItem.Double(display, "um", ParameterGridScope.Recipe, () => unit.GetPickerTeachingPosition(axis, positionName) * 1000.0, v => SetPosition(axis, positionName, v / 1000.0)));
        }

        private void BindIoPanel()
        {
            if (unit == null)
                return;

            List<IoCylinderItem> items = new List<IoCylinderItem>();
            items.Add(IoCylinderItem.Input("CDA TANK PRESSURE", () => unit.IsPickerCdaPressureOk()));
            items.Add(IoCylinderItem.Input("VACUUM TANK PRESSURE", () => unit.IsPickerVacuumPressureOk()));
            for (int i = 1; i <= 8; i++)
            {
                int pickerNo = i;
                items.Add(IoCylinderItem.Input("P" + pickerNo + " FLOW", () => unit.IsPickerFlowDetected(pickerNo)));
                items.Add(IoCylinderItem.Output("P" + pickerNo + " VACUUM", () => OutputOn(unit.Vacuums, pickerNo), on => { unit.SetPickerVacuum(pickerNo, on); return Task.FromResult(0); }, "ON", "OFF"));
                items.Add(IoCylinderItem.Output("P" + pickerNo + " BLOW", () => OutputOn(unit.Blows, pickerNo), on => { unit.SetPickerBlow(pickerNo, on); return Task.FromResult(0); }, "ON", "OFF"));
            }
            ioCylinderPanel.SetItems(items);
        }

        private void BindJogPanel()
        {
            if (unit == null)
                return;

            List<JogAxisItem> items = new List<JogAxisItem>();
            AddJogItem(items, "PICKER X", PickerAxis.PickerX, "X+", "X-", JogAxisControlKind.Horizontal);
            AddJogItem(items, "PICKER Y", PickerAxis.PickerY, "Y+", "Y-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T0", PickerAxis.PickerT0, "T+", "T-", JogAxisControlKind.Horizontal);
            AddJogItem(items, "PICKER Z0", PickerAxis.PickerZ0, "Z+", "Z-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T1", PickerAxis.PickerT1, "T+", "T-", JogAxisControlKind.Horizontal);
            AddJogItem(items, "PICKER Z1", PickerAxis.PickerZ1, "Z+", "Z-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T2", PickerAxis.PickerT2, "T+", "T-", JogAxisControlKind.Horizontal);
            AddJogItem(items, "PICKER Z2", PickerAxis.PickerZ2, "Z+", "Z-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T3", PickerAxis.PickerT3, "T+", "T-", JogAxisControlKind.Horizontal);
            AddJogItem(items, "PICKER Z3", PickerAxis.PickerZ3, "Z+", "Z-", JogAxisControlKind.Vertical);
            jogAxisMoveControl.SpeedControl = jogSpeedControl;
            jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.AxisColumns;
            jogAxisMoveControl.AxisColumnsPerRow = 2;
            jogAxisMoveControl.ShowCurrentSpeedMode = true;
            jogAxisMoveControl.ButtonAreaMinHeight = 180;
            jogAxisMoveControl.ButtonAreaMaxHeight = 260;
            jogAxisMoveControl.SetItems(items);
            jogPositionListControl.SetItems(items);
        }

        private void AddJogItem(List<JogAxisItem> items, string name, PickerAxis axisKey, string plus, string minus, JogAxisControlKind kind)
        {
            BaseAxis axis = GetAxis(axisKey);
            if (axis == null)
                return;

            bool theta = IsTheta(axisKey);
            JogAxisItem item = JogAxisItem.Single(name, axis, theta ? "deg" : "um", theta ? 1.0 : 1000.0, plus, minus).WithControlKind(kind);
            item.StepMoveAsync = (jogItem, direction, speedType, customSpeed, axisStepDistance) => unit.JogStepAsync(axis, direction, speedType, customSpeed, axisStepDistance);
            item.ContinuousMoveAsync = (jogItem, direction, speedType, customSpeed) => unit.JogContinuousAsync(axis, direction, speedType, customSpeed);
            item.StopAsync = jogItem => unit.StopJogAsync(axis);
            items.Add(item);
        }

        private async void OptionParameterGrid_RowDoubleClicked(object sender, ParameterGridChangedEventArgs e)
        {
            string key = e != null && e.Item != null ? e.Item.Key : string.Empty;
            await MoveSelectedPositionAsync(key);
        }

        private void BindParameterGridMenus()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Move To Position", null, async delegate
            {
                ParameterGridItem selected = optionParameterGrid.SelectedItem;
                await MoveSelectedPositionAsync(selected != null ? selected.Key : string.Empty);
            });
            menu.Items.Add("Teach Current Position", null, delegate
            {
                ParameterGridItem selected = optionParameterGrid.SelectedItem;
                TeachSelectedPosition(selected != null ? selected.Key : string.Empty);
            });
            optionParameterGrid.ContextMenuStrip = menu;
        }

        private async Task MoveSelectedPositionAsync(string key)
        {
            if (unit == null || string.IsNullOrWhiteSpace(key) || !positionItems.ContainsKey(key))
                return;

            PositionItem item = positionItems[key];
            await ConfirmMoveAsync(item.DisplayName, delegate { return unit.MovePickerAxisToTeachingPosition(item.Axis, item.PositionName, IsFineMove()); });
        }

        private void TeachSelectedPosition(string key)
        {
            if (unit == null || string.IsNullOrWhiteSpace(key) || !positionItems.ContainsKey(key))
                return;

            PositionItem item = positionItems[key];
            unit.TeachPickerAxisPosition(item.Axis, item.PositionName);
            SaveCurrentRecipeData();
            RefreshView();
        }

        private async Task ConfirmMoveAsync(string actionName, Func<Task<int>> move)
        {
            if (unit == null || move == null)
                return;

            DialogResult result = QMC.Common.MessageDialog.Show(this, actionName + " move?", "Front Picker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            await RunSafeAsync(move, actionName);
        }

        private async Task RunSafeAsync(Func<Task<int>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                int result = await action();
                if (result != 0)
                    QMC.Common.MessageDialog.Show(this, actionName + " failed. result=" + result, "Front Picker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                RefreshView();
            }
        }

        private void ParameterGrid_ParameterValueChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e != null && e.Item != null && e.Item.Scope == ParameterGridScope.Recipe)
                    SaveCurrentRecipeData();
                else
                    SaveCurrentSettingsData();
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "FRONT-PICKER", "Parameter save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Front Picker Parameter Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveCurrentRecipeData()
        {
            Form1 host = FindHostForm();
            if (host == null || string.IsNullOrWhiteSpace(host.CurrentRecipeName))
                return;

            host.SaveMachineRecipe(host.CurrentRecipeName);
        }

        private void SaveCurrentSettingsData()
        {
            Form1 host = FindHostForm();
            if (host != null)
                host.SaveMachineSettings();
        }

        private void RefreshView()
        {
            try
            {
                if (unit == null)
                    return;

                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();
                RefreshVisionView();
            }
            catch
            {
            }
        }

        private void RefreshVisionView()
        {
            lblVisionInfo.Text =
                "VISION VIEW" + Environment.NewLine +
                "FRONT PICKER" + Environment.NewLine +
                "X : " + AxisText(GetAxis(PickerAxis.PickerX), false) + Environment.NewLine +
                "Y : " + AxisText(GetAxis(PickerAxis.PickerY), false) + Environment.NewLine +
                "T0: " + AxisText(GetAxis(PickerAxis.PickerT0), true) + Environment.NewLine +
                "Z0: " + AxisText(GetAxis(PickerAxis.PickerZ0), false) + Environment.NewLine +
                "CDA: " + OnOff(unit.IsPickerCdaPressureOk()) + Environment.NewLine +
                "VAC: " + OnOff(unit.IsPickerVacuumPressureOk());
        }

        private void SetPosition(PickerAxis axis, string positionName, double value)
        {
            PickerAxisPositionSet set = GetPositionSet(axis);
            if (set == null || string.IsNullOrWhiteSpace(positionName))
                return;

            if (positionName == "InputAvoidPosition") set.InputAvoidPosition = value;
            else if (positionName == "OutputAvoidPosition") set.OutputAvoidPosition = value;
            else if (positionName == "AvoidPosition") set.AvoidPosition = value;
            else if (positionName == "PickPosition") set.PickPosition = value;
            else if (positionName == "BottomPosition") set.BottomPosition = value;
            else if (positionName == "SidePosition") set.SidePosition = value;
            else if (positionName == "PlacePosition") set.PlacePosition = value;
            else if (positionName.StartsWith("DiePickPosition", StringComparison.OrdinalIgnoreCase)) set.DiePickPosition = SetIndexed(set.DiePickPosition, ExtractIndex(positionName), value);
            else if (positionName.StartsWith("DieBottomPosition", StringComparison.OrdinalIgnoreCase)) set.DieBottomPosition = SetIndexed(set.DieBottomPosition, ExtractIndex(positionName), value);
            else if (positionName.StartsWith("DieSidePosition", StringComparison.OrdinalIgnoreCase)) set.DieSidePosition = SetIndexed(set.DieSidePosition, ExtractIndex(positionName), value);
            else if (positionName.StartsWith("DiePlacePosition", StringComparison.OrdinalIgnoreCase)) set.DiePlacePosition = SetIndexed(set.DiePlacePosition, ExtractIndex(positionName), value);
        }

        private PickerAxisPositionSet GetPositionSet(PickerAxis axis)
        {
            if (axis == PickerAxis.PickerX) return unit.Recipe.PickerX;
            if (axis == PickerAxis.PickerY) return unit.Recipe.PickerY;
            if (IsTheta(axis)) return unit.Recipe.PickerT0;
            return unit.Recipe.PickerZ0;
        }

        private BaseAxis GetAxis(PickerAxis axis)
        {
            BaseAxis item;
            return unit != null && unit.Axes.TryGetValue(axis, out item) ? item : null;
        }

        private static double[] SetIndexed(double[] values, int index, double value)
        {
            if (index < 0)
                return values;
            if (values == null)
                values = new double[index + 1];
            if (values.Length <= index)
            {
                double[] next = new double[index + 1];
                Array.Copy(values, next, values.Length);
                values = next;
            }
            values[index] = value;
            return values;
        }

        private static int ExtractIndex(string name)
        {
            int start = name.IndexOf('[');
            int end = name.IndexOf(']');
            if (start < 0 || end <= start)
                return -1;
            int index;
            return int.TryParse(name.Substring(start + 1, end - start - 1), out index) ? index : -1;
        }

        private static bool IsTheta(PickerAxis axis)
        {
            return axis == PickerAxis.PickerT0 || axis == PickerAxis.PickerT1 || axis == PickerAxis.PickerT2 || axis == PickerAxis.PickerT3;
        }

        private static string AxisText(BaseAxis axis, bool theta)
        {
            if (axis == null)
                return "-";
            return theta ? axis.ActualPosition.ToString("0.###") + " deg" : (axis.ActualPosition * 1000.0).ToString("0.###") + " um";
        }

        private static string OnOff(bool value)
        {
            return value ? "ON" : "OFF";
        }

        private static bool OutputOn(IList<BaseDigitalOutput> outputs, int pickerNo)
        {
            int index = pickerNo - 1;
            return outputs != null && index >= 0 && index < outputs.Count && outputs[index] != null && outputs[index].IsOn;
        }

        private bool IsFineMove()
        {
            return true;
        }

        private CDT320_Machine FindMachine()
        {
            Form1 host = FindHostForm();
            return host != null ? host.Machine : null;
        }

        private Form1 FindHostForm()
        {
            foreach (Form form in Application.OpenForms)
            {
                Form1 host = form as Form1;
                if (host != null)
                    return host;
            }
            return FindForm() as Form1;
        }
    }
}
