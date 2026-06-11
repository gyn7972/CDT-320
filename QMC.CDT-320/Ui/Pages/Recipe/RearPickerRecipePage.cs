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
    public sealed partial class RearPickerRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private sealed class PositionItem
        {
            public string DisplayName;
            public PickerAxis Axis;
            public string PositionName;
        }

        private readonly Timer refreshTimer = new Timer();
        private readonly Dictionary<string, PositionItem> positionItems = new Dictionary<string, PositionItem>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PositionItem>> groupMoves = new Dictionary<string, List<PositionItem>>(StringComparer.OrdinalIgnoreCase);
        private PickerRearUnit unit;

        public RearPickerRecipePage()
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

            lblHeader.Tag = "i18n:recipe.rearHead";
            lblHeader.Text = Lang.T("recipe.rearHead");
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
            unit = machine != null ? machine.PickerRearUnit : null;
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
            manualLayout.SuspendLayout();
            try
            {
                manualLayout.Controls.Clear();
                manualLayout.RowStyles.Clear();
                manualLayout.RowCount = 0;

                // 옵션 파라미터 그룹과 1:1 매칭 — 버튼 하나가 해당 그룹 항목 전체를 이동
                AddActionButton("AVOID POSITION", delegate { return MoveGroupAsync("K_AVOID"); });
                AddActionButton("PICK POSITION", delegate { return MoveGroupAsync("K_PICK"); });
                AddActionButton("BOTTOM POSITION", delegate { return MoveGroupAsync("K_BOTTOM"); });
                AddActionButton("SIDE POSITION", delegate { return MoveGroupAsync("K_SIDE"); });
                AddActionButton("PLACE POSITION", delegate { return MoveGroupAsync("K_PLACE"); });
                AddActionButton("DIE PICK POSITION", delegate { return MoveGroupAsync("K_DiePickPosition"); });
                AddActionButton("DIE BOTTOM POSITION", delegate { return MoveGroupAsync("K_DieBottomPosition"); });
                AddActionButton("DIE SIDE POSITION", delegate { return MoveGroupAsync("K_DieSidePosition"); });
                AddActionButton("DIE PLACE POSITION", delegate { return MoveGroupAsync("K_DiePlacePosition"); });
            }
            finally
            {
                manualLayout.ResumeLayout(true);
            }
        }

        private void AddActionButton(string text, Func<Task<int>> command)
        {
            ActionButton button = new ActionButton();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(2);
            button.BackColor = Color.FromArgb(128, 128, 128);
            button.ForeColor = Color.White;
            button.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.Click += async delegate { await ConfirmMoveAsync(text, command); };

            int index = manualLayout.Controls.Count;
            int column = index % manualLayout.ColumnCount;
            int targetRow = index / manualLayout.ColumnCount;
            while (manualLayout.RowStyles.Count <= targetRow)
                manualLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));

            manualLayout.Controls.Add(button, column, targetRow);
            manualLayout.RowCount = targetRow + 1;
        }

        private void BindParameterGrids()
        {
            if (unit == null)
                return;

            EnsureData();
            positionItems.Clear();
            groupMoves.Clear();

            List<ParameterGridItem> optionItems = new List<ParameterGridItem>();

            // ===== RECIPE (teaching positions) — 위치 종류별 접이식 그룹 =====
            PickerAxis[] tzKeys = { PickerAxis.PickerT0, PickerAxis.PickerZ0, PickerAxis.PickerT1, PickerAxis.PickerZ1, PickerAxis.PickerT2, PickerAxis.PickerZ2, PickerAxis.PickerT3, PickerAxis.PickerZ3 };
            string[] tzNames = { "PICKER T1", "PICKER Z1", "PICKER T2", "PICKER Z2", "PICKER T3", "PICKER Z3", "PICKER T4", "PICKER Z4" };
            string[] tzUnits = { "deg", "um", "deg", "um", "deg", "um", "deg", "um" };

            PickerAxis[] dieKeys = { PickerAxis.PickerX, PickerAxis.PickerY };
            string[] dieNames = { "PICKER X", "PICKER Y" };

            // AVOID — 전체 축의 avoid 위치
            optionItems.Add(ParameterGridItem.Header("AVOID POSITION", "K_AVOID"));
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "INPUT AVOID POSITION", "InputAvoidPosition", "um", "PICKER X INPUT", "K_AVOID");
            AddPositionItem(optionItems, PickerAxis.PickerX, "PICKER X", "OUTPUT AVOID POSITION", "OutputAvoidPosition", "um", "PICKER X OUTPUT", "K_AVOID");
            AddPositionItem(optionItems, PickerAxis.PickerY, "PICKER Y", "AVOID POSITION", "AvoidPosition", "um", "PICKER Y", "K_AVOID");
            for (int i = 0; i < tzKeys.Length; i++)
                AddPositionItem(optionItems, tzKeys[i], tzNames[i], "AVOID POSITION", "AvoidPosition", tzUnits[i], tzNames[i], "K_AVOID");

            // T/Z 모션 종류 — PICK / BOTTOM / SIDE / PLACE
            string[] tzKinds = { "PICK", "BOTTOM", "SIDE", "PLACE" };
            string[] tzKindPos = { "PickPosition", "BottomPosition", "SidePosition", "PlacePosition" };
            for (int k = 0; k < tzKinds.Length; k++)
            {
                string gk = "K_" + tzKinds[k];
                optionItems.Add(ParameterGridItem.Header(tzKinds[k] + " POSITION", gk));
                for (int i = 0; i < tzKeys.Length; i++)
                    AddPositionItem(optionItems, tzKeys[i], tzNames[i], tzKinds[k] + " POSITION", tzKindPos[k], tzUnits[i], tzNames[i], gk);
            }

            // X/Y 다이 어레이 종류 — DIE PICK / DIE BOTTOM / DIE SIDE / DIE PLACE (P1~4)
            string[] dieKinds = { "DIE PICK", "DIE BOTTOM", "DIE SIDE", "DIE PLACE" };
            string[] dieKindPos = { "DiePickPosition", "DieBottomPosition", "DieSidePosition", "DiePlacePosition" };
            for (int k = 0; k < dieKinds.Length; k++)
            {
                string gk = "K_" + dieKindPos[k];
                optionItems.Add(ParameterGridItem.Header(dieKinds[k] + " POSITION", gk));
                for (int a = 0; a < dieKeys.Length; a++)
                    for (int i = 0; i < 4; i++)
                        AddPositionItem(optionItems, dieKeys[a], dieNames[a], "P" + (i + 1) + " " + dieKinds[k] + " POSITION", dieKindPos[k] + "[" + i + "]", "um", dieNames[a] + " P" + (i + 1), gk);
            }

            // ===== CONFIG (그룹 없이 펼친 상태로 표시) =====
            optionItems.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));
            AddPickerConfigItems(optionItems);

            // ===== SETUP (그룹 없이 펼친 상태로 표시) =====
            optionItems.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
            optionItems.Add(ParameterGridItem.Double("INPUT SAFETY OFFSET", "um", ParameterGridScope.Setup, () => unit.Setup.InputSafetyOffset * 1000.0, v => unit.Setup.InputSafetyOffset = v / 1000.0));
            optionItems.Add(ParameterGridItem.Double("OUTPUT SAFETY OFFSET", "um", ParameterGridScope.Setup, () => unit.Setup.OutputSafetyOffset * 1000.0, v => unit.Setup.OutputSafetyOffset = v / 1000.0));

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

        private void AddPositionItem(List<ParameterGridItem> items, PickerAxis axis, string axisName, string displaySuffix, string positionName, string displayUnit, string memberDisplay, string groupKey)
        {
            string display = axisName + " " + displaySuffix;
            PositionItem posItem = new PositionItem { DisplayName = display, Axis = axis, PositionName = positionName };
            positionItems[display] = posItem;

            List<PositionItem> groupList;
            if (!groupMoves.TryGetValue(groupKey, out groupList))
            {
                groupList = new List<PositionItem>();
                groupMoves[groupKey] = groupList;
            }
            groupList.Add(posItem);

            ParameterGridItem item;
            if (displayUnit == "deg")
                item = ParameterGridItem.Double(memberDisplay, "deg", ParameterGridScope.Recipe, () => unit.GetPickerTeachingPosition(axis, positionName), v => SetPosition(axis, positionName, v));
            else
                item = ParameterGridItem.Double(memberDisplay, "um", ParameterGridScope.Recipe, () => unit.GetPickerTeachingPosition(axis, positionName) * 1000.0, v => SetPosition(axis, positionName, v / 1000.0));

            item.Key = display;                     // 이동/티칭 조회는 전체 이름(positionItems 키)으로 매칭
            item.GroupKey = groupKey;
            items.Add(item);
        }

        private void BindIoPanel()
        {
            if (unit == null)
                return;

            List<IoCylinderItem> items = new List<IoCylinderItem>();

            // 공용 압력 체크
            items.Add(IoCylinderItem.Input("REAR PICKER CDA TANK PRESSURE CHECK", () => unit.IsPickerCdaPressureOk()));
            items.Add(IoCylinderItem.Input("REAR PICKER VACUUM TANK PRESSURE CHECK", () => unit.IsPickerVacuumPressureOk()));

            // 피커 1~4: FLOW / VACUUM / BLOW 를 한 세트로 묶어 정렬 (5~8 미사용)
            for (int i = 1; i <= 4; i++)
            {
                int pickerNo = i;
                items.Add(IoCylinderItem.Input("REAR PICKER " + pickerNo + " FLOW CHECK", () => unit.IsPickerFlowDetected(pickerNo)));
                items.Add(IoCylinderItem.Output("REAR PICKER " + pickerNo + " VACUUM", () => OutputOn(unit.Vacuums, pickerNo), on => { unit.SetPickerVacuum(pickerNo, on); return Task.FromResult(0); }, "ON", "OFF"));
                items.Add(IoCylinderItem.Output("REAR PICKER " + pickerNo + " BLOW", () => OutputOn(unit.Blows, pickerNo), on => { unit.SetPickerBlow(pickerNo, on); return Task.FromResult(0); }, "ON", "OFF"));
            }

            ioCylinderPanel.SetItems(items);
        }

        private void BindJogPanel()
        {
            if (unit == null)
                return;

            List<JogAxisItem> items = new List<JogAxisItem>();
            AddJogItem(items, "PICKER X", PickerAxis.PickerX, "X+", "X-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Y", PickerAxis.PickerY, "Y+", "Y-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T0", PickerAxis.PickerT0, "T+", "T-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Z0", PickerAxis.PickerZ0, "Z+", "Z-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T1", PickerAxis.PickerT1, "T+", "T-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Z1", PickerAxis.PickerZ1, "Z+", "Z-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T2", PickerAxis.PickerT2, "T+", "T-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Z2", PickerAxis.PickerZ2, "Z+", "Z-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER T3", PickerAxis.PickerT3, "T+", "T-", JogAxisControlKind.Vertical);
            AddJogItem(items, "PICKER Z3", PickerAxis.PickerZ3, "Z+", "Z-", JogAxisControlKind.Vertical);
            jogAxisMoveControl.SpeedControl = jogSpeedControl;
            jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.PickerTabbed;
            jogAxisMoveControl.ShowCurrentSpeedMode = true;
            jogAxisMoveControl.ButtonAreaMinHeight = 360;
            jogAxisMoveControl.ButtonAreaMaxHeight = 700;
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

        private async Task<int> MoveGroupAsync(string groupKey)
        {
            if (unit == null || string.IsNullOrEmpty(groupKey) || !groupMoves.ContainsKey(groupKey))
                return -1;

            List<PositionItem> members = groupMoves[groupKey];
            if (members == null || members.Count == 0)
                return 0;

            bool fine = IsFineMove();

            // 같은 축에 위치가 여러 개면(예: PICKER X INPUT/OUTPUT AVOID) 순차 실행, 서로 다른 축은 병렬 실행
            Dictionary<PickerAxis, List<PositionItem>> byAxis = new Dictionary<PickerAxis, List<PositionItem>>();
            foreach (PositionItem m in members)
            {
                List<PositionItem> list;
                if (!byAxis.TryGetValue(m.Axis, out list))
                {
                    list = new List<PositionItem>();
                    byAxis[m.Axis] = list;
                }
                list.Add(m);
            }

            List<Task<int>> tasks = new List<Task<int>>();
            foreach (KeyValuePair<PickerAxis, List<PositionItem>> pair in byAxis)
                tasks.Add(MoveAxisSequentialAsync(pair.Value, fine));

            int[] results = await Task.WhenAll(tasks);
            int finalResult = 0;
            foreach (int r in results)
            {
                if (r != 0)
                    finalResult = r;
            }
            return finalResult;
        }

        private async Task<int> MoveAxisSequentialAsync(List<PositionItem> moves, bool fine)
        {
            int last = 0;
            foreach (PositionItem m in moves)
            {
                int r = await unit.MovePickerAxisToTeachingPosition(m.Axis, m.PositionName, fine);
                if (r != 0)
                    last = r;
            }
            return last;
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

            if (ManualMoveGuard.BlockIfNotReady(this, "Rear Picker"))
                return;

            DialogResult result = QMC.Common.MessageDialog.Show(this, actionName + " move?", "Rear Picker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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
                    QMC.Common.MessageDialog.Show(this, actionName + " failed. result=" + result, "Rear Picker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                EventLogger.Write(EventKind.Alarm, "UI", "REAR-PICKER", "Parameter save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Rear Picker Parameter Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string body =
                "REAR PICKER" + Environment.NewLine +
                "X : " + AxisText(GetAxis(PickerAxis.PickerX), false) + Environment.NewLine +
                "Y : " + AxisText(GetAxis(PickerAxis.PickerY), false) + Environment.NewLine +
                "T0: " + AxisText(GetAxis(PickerAxis.PickerT0), true) + Environment.NewLine +
                "Z0: " + AxisText(GetAxis(PickerAxis.PickerZ0), false) + Environment.NewLine +
                "CDA: " + OnOff(unit.IsPickerCdaPressureOk()) + Environment.NewLine +
                "VAC: " + OnOff(unit.IsPickerVacuumPressureOk());

            lblVisionInfo.Text = "BOTTOM VISION" + Environment.NewLine + body;
            lblVisionInfo2.Text = "SIDE VISION 1" + Environment.NewLine + body;
            lblVisionInfo3.Text = "SIDE VISION 2" + Environment.NewLine + body;
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
            if (axis == PickerAxis.PickerT0) return unit.Recipe.PickerT0;
            if (axis == PickerAxis.PickerT1) return unit.Recipe.PickerT1;
            if (axis == PickerAxis.PickerT2) return unit.Recipe.PickerT2;
            if (axis == PickerAxis.PickerT3) return unit.Recipe.PickerT3;
            if (axis == PickerAxis.PickerZ1) return unit.Recipe.PickerZ1;
            if (axis == PickerAxis.PickerZ2) return unit.Recipe.PickerZ2;
            if (axis == PickerAxis.PickerZ3) return unit.Recipe.PickerZ3;
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
