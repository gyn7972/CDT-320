using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class InputStageRecipePage : PageBase
    {
        private enum StagePositionKind
        {
            Avoid,
            Load,
            Process,
            Unload,
            Ready,
            Reticle
        }

        private sealed class StageTeachingPosition
        {
            public StageTeachingPosition(
                string axisLabel,
                StagePositionKind kind,
                Func<InputStageUnit, BaseAxis> axisGetter,
                Func<InputStageUnit, StageAxisPositions> positionSetGetter,
                Func<StageAxisPositions, double> getter,
                Action<StageAxisPositions, double> setter)
            {
                AxisLabel = axisLabel;
                Kind = kind;
                DisplayName = GetPositionLabel(kind) + " - " + axisLabel;
                AxisGetter = axisGetter;
                PositionSetGetter = positionSetGetter;
                Getter = getter;
                Setter = setter;
            }

            public string AxisLabel { get; private set; }
            public StagePositionKind Kind { get; private set; }
            public string DisplayName { get; private set; }
            public Func<InputStageUnit, BaseAxis> AxisGetter { get; private set; }
            public Func<InputStageUnit, StageAxisPositions> PositionSetGetter { get; private set; }
            public Func<StageAxisPositions, double> Getter { get; private set; }
            public Action<StageAxisPositions, double> Setter { get; private set; }
        }

        private static readonly StageTeachingPosition[] TeachingPositions = CreateTeachingPositions();

        private readonly string _titleI18n;
        private readonly Timer _refreshTimer = new Timer();
        private readonly ToolTip _toolTip = new ToolTip();
        private InputStageUnit _InputStageUnit;

        private static StageTeachingPosition[] CreateTeachingPositions()
        {
            var axes = new List<StageTeachingAxis>();
            AddTeachingAxis(axes, "WAFER Y", unit => unit.StageY, unit => unit.Recipe.WaferY);
            AddTeachingAxis(axes, "WAFER T", unit => unit.StageT, unit => unit.Recipe.WaferT);
            AddTeachingAxis(axes, "EXPANDER Z", unit => unit.ExpanderZ, unit => unit.Recipe.WaferZ);
            AddTeachingAxis(axes, "VISION X", unit => unit.CameraX, unit => unit.Recipe.VisionX, false, false);
            AddTeachingAxis(axes, "NEEDLE X", unit => unit.NeedleBlockX, unit => unit.Recipe.NeedleX, false, false);
            AddTeachingAxis(axes, "NEEDLE Z", unit => unit.NeedleZ, unit => unit.Recipe.NeedleZ);
            AddTeachingAxis(axes, "EJECT PIN Z", unit => unit.EjectPinZ, unit => unit.Recipe.EjectPinZ, false, false);

            var positions = new List<StageTeachingPosition>();
            foreach (StagePositionKind kind in new[] { StagePositionKind.Avoid, StagePositionKind.Load, StagePositionKind.Process, StagePositionKind.Unload, StagePositionKind.Ready, StagePositionKind.Reticle })
            {
                foreach (StageTeachingAxis axis in axes)
                {
                    if (!axis.Supports(kind))
                        continue;

                    AddTeachingPosition(positions, axis, kind);
                }
            }

            return positions.ToArray();
        }

        private sealed class StageTeachingAxis
        {
            public StageTeachingAxis(
                string axisLabel,
                Func<InputStageUnit, BaseAxis> axisGetter,
                Func<InputStageUnit, StageAxisPositions> positionSetGetter,
                bool includeLoad,
                bool includeUnload)
            {
                AxisLabel = axisLabel;
                AxisGetter = axisGetter;
                PositionSetGetter = positionSetGetter;
                IncludeLoad = includeLoad;
                IncludeUnload = includeUnload;
            }

            public string AxisLabel { get; private set; }
            public Func<InputStageUnit, BaseAxis> AxisGetter { get; private set; }
            public Func<InputStageUnit, StageAxisPositions> PositionSetGetter { get; private set; }
            public bool IncludeLoad { get; private set; }
            public bool IncludeUnload { get; private set; }

            public bool Supports(StagePositionKind kind)
            {
                if (kind == StagePositionKind.Load)
                    return IncludeLoad;
                if (kind == StagePositionKind.Unload)
                    return IncludeUnload;
                return true;
            }
        }

        private static void AddTeachingAxis(
            List<StageTeachingAxis> axes,
            string axisLabel,
            Func<InputStageUnit, BaseAxis> axisGetter,
            Func<InputStageUnit, StageAxisPositions> positionSetGetter,
            bool includeLoad = true,
            bool includeUnload = true)
        {
            axes.Add(new StageTeachingAxis(axisLabel, axisGetter, positionSetGetter, includeLoad, includeUnload));
        }

        private static void AddTeachingPosition(
            List<StageTeachingPosition> positions,
            StageTeachingAxis axis,
            StagePositionKind kind,
            Func<StageAxisPositions, double> getter,
            Action<StageAxisPositions, double> setter)
        {
            positions.Add(new StageTeachingPosition(axis.AxisLabel, kind, axis.AxisGetter, axis.PositionSetGetter, getter, setter));
        }

        private static void AddTeachingPosition(
            List<StageTeachingPosition> positions,
            StageTeachingAxis axis,
            StagePositionKind kind)
        {
            switch (kind)
            {
                case StagePositionKind.Avoid:
                    AddTeachingPosition(positions, axis, kind, set => set.AvoidPosition, (set, value) => set.AvoidPosition = value);
                    break;
                case StagePositionKind.Load:
                    AddTeachingPosition(positions, axis, kind, set => set.LoadPosition, (set, value) => set.LoadPosition = value);
                    break;
                case StagePositionKind.Process:
                    AddTeachingPosition(positions, axis, kind, set => set.ProcessPosition, (set, value) => set.ProcessPosition = value);
                    break;
                case StagePositionKind.Unload:
                    AddTeachingPosition(positions, axis, kind, set => set.UnloadPosition, (set, value) => set.UnloadPosition = value);
                    break;
                case StagePositionKind.Ready:
                    AddTeachingPosition(positions, axis, kind, set => set.ReadyPosition, (set, value) => set.ReadyPosition = value);
                    break;
                case StagePositionKind.Reticle:
                    AddTeachingPosition(positions, axis, kind, set => set.ReticlePosition, (set, value) => set.ReticlePosition = value);
                    break;
            }
        }

        private static string GetPositionLabel(StagePositionKind kind)
        {
            switch (kind)
            {
                case StagePositionKind.Avoid:
                    return "AVOID POSITION";
                case StagePositionKind.Load:
                    return "LOAD POSITION";
                case StagePositionKind.Process:
                    return "PROCESS POSITION";
                case StagePositionKind.Unload:
                    return "UNLOAD POSITION";
                case StagePositionKind.Ready:
                    return "READY POSITION";
                case StagePositionKind.Reticle:
                    return "RETICLE POSITION";
                default:
                    return kind.ToString().ToUpperInvariant() + " POSITION";
            }
        }

        public InputStageRecipePage() : this("recipe.inputStage")
        {
        }

        public InputStageRecipePage(string titleI18n)
        {
            try
            {
                _titleI18n = titleI18n;
                InitializeComponent();
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                    return;

                ApplyTitle();
                ApplyRuntimeLayout();
                ConfigureRuntimeBehavior();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(ex.Message, "Input Stage", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                base.OnLoad(e);
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

                ResolveUnit();
                BindParameterGrids();
                BindIoPanel();
                BindJogPanel();
                RefreshView();
                _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _refreshTimer.Stop();
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

        private void ApplyTitle()
        {
            try
            {
                lblHeader.Tag = "i18n:" + _titleI18n;
                lblHeader.Text = Lang.T(_titleI18n);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void ApplyRuntimeLayout()
        {
            try
            {
                optionLayout.Visible = false;
                waitLayout.Visible = false;
                ioLayout.Visible = false;
                jogLayout.Visible = false;
                jogCommonLayout.Visible = true;
                speedLayout.Visible = false;

                optionParameterGrid.BringToFront();
                waitParameterGrid.BringToFront();
                ioCylinderPanel.BringToFront();
                jogCommonLayout.BringToFront();
                jogSpeedControl.BringToFront();

                BackColor = Color.FromArgb(207, 210, 214);
                rootLayout.BackColor = BackColor;
                contentLayout.BackColor = BackColor;
                lblHeader.BackColor = Color.FromArgb(64, 64, 64);
                lblHeader.ForeColor = Color.White;
                lblHeader.Font = new Font("Malgun Gothic", 11F, FontStyle.Bold);
                foreach (var group in new[] { grpOptions, grpWait, grpManual, grpIo, grpVision, grpJog, grpSpeed })
                    group.Font = new Font("Malgun Gothic", 10F, FontStyle.Bold);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ConfigureRuntimeBehavior()
        {
            try
            {
                _refreshTimer.Interval = 250;
                _refreshTimer.Tick += RefreshTimer_Tick;
                optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                optionParameterGrid.ParameterRowDoubleClicked += OptionParameterGrid_RowDoubleClicked;
                waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;

                jogAxisMoveControl.SpeedControl = jogSpeedControl;
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.Stage;
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 520;
                jogAxisMoveControl.ButtonAreaMaxHeight = 620;
                jogAxisMoveControl.ButtonAreaMinWidth = 300;
                jogAxisMoveControl.ButtonAreaMaxWidth = 460;

                ConfigureActionButtons();
                BindParameterGridMenus();

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                grpIo.ContextMenuStrip.Items.Add("Input stage I/O 상태를 다시 읽습니다.", null, IoRefresh_Click);

                _toolTip.SetToolTip(optionParameterGrid, "이름 셀 더블클릭: 현재 위치 티칭, 값 셀 더블클릭: 직접 편집");
                _toolTip.SetToolTip(waitParameterGrid, "Input Stage 대기 관련 파라미터를 표시합니다.");
                _toolTip.SetToolTip(ioCylinderPanel, "Input Stage 출력 상태를 확인하고 제어합니다.");
                _toolTip.SetToolTip(jogAxisMoveControl, "Input Stage 축을 Unit jog 경로로 이동합니다.");
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ConfigureActionButtons()
        {
            manualScrollPanel.AutoScroll = true;
            manualScrollPanel.HorizontalScroll.Enabled = false;
            manualScrollPanel.HorizontalScroll.Visible = false;

            manualLayout.Dock = DockStyle.Top;
            BuildManualActionButtons();
        }

        private void BuildManualActionButtons()
        {
            manualScrollPanel.SuspendLayout();
            try
            {
                manualScrollPanel.Controls.Clear();
                manualLayout.Controls.Clear();
                manualLayout.RowStyles.Clear();
                manualLayout.ColumnStyles.Clear();
                manualLayout.ColumnCount = 2;
                manualLayout.RowCount = 0;
                manualLayout.AutoSize = true;
                manualLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                manualLayout.Dock = DockStyle.Top;
                manualLayout.Padding = new Padding(8, 18, 8, 8);
                manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                manualLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

                foreach (StageTeachingPosition position in TeachingPositions)
                    AddManualButton(CreateManualActionButton(position.DisplayName, () => MoveByTeachingPositionAsync(position)));

                AddManualButton(CreateManualActionButton("PICK TEST", PickTestAsync));
                manualScrollPanel.Controls.Add(manualLayout);
            }
            finally
            {
                manualScrollPanel.ResumeLayout(true);
            }
        }

        private ActionButton CreateManualActionButton(string text, Func<Task<int>> action)
        {
            var button = new ActionButton
            {
                BackColor = Color.FromArgb(0x80, 0x80, 0x80),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                Font = new Font("Malgun Gothic", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Margin = new Padding(4),
                Size = new Size(150, 39),
                Text = text
            };

            button.Click += async (s, e) => await ConfirmAndRunAsync(text, action);
            return button;
        }

        private void AddManualButton(ActionButton button)
        {
            if (button == null)
                return;

            int index = manualLayout.Controls.Count;
            int column = index % manualLayout.ColumnCount;
            int targetRow = index / manualLayout.ColumnCount;

            while (manualLayout.RowStyles.Count <= targetRow)
                manualLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));

            button.Visible = true;
            button.Dock = DockStyle.Fill;
            manualLayout.Controls.Add(button, column, targetRow);
            manualLayout.RowCount = targetRow + 1;
        }

        private void BindTeachingActionButton(ActionButton button, string displayName)
        {
            StageTeachingPosition position = FindTeachingPosition(displayName);
            if (position == null)
                return;

            BindActionButton(button, position.DisplayName, () => MoveByTeachingPositionAsync(position));
        }

        private void BindActionButton(ActionButton button, string text, Func<Task<int>> action)
        {
            if (button == null)
                return;

            button.Text = text;
            button.Click += async (s, e) => await ConfirmAndRunAsync(text, action);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                RefreshView();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ResolveUnit()
        {
            try
            {
                var machine = FindMachine();
                _InputStageUnit = machine != null ? machine.InputStageUnit : null;
                SetEnabledState(_InputStageUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private CDT320_Machine FindMachine()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    var host = form as Form1;
                    if (host != null)
                        return host.Machine;
                }

                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private Form1 FindHostForm()
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    var host = form as Form1;
                    if (host != null)
                        return host;
                }

                return FindForm() as Form1;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private void SetEnabledState(bool enabled)
        {
            try
            {
                foreach (Control control in new Control[] { grpOptions, grpWait, grpManual, grpIo, grpVision, grpJog, grpSpeed })
                    control.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void IoRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                BindIoPanel();
                RefreshView();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindParameterGridMenus()
        {
            try
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add("Move To Position", null, async (s, e) =>
                {
                    StageTeachingPosition position = GetSelectedTeachingPosition();
                    if (position != null)
                        await ConfirmAndRunAsync(position.DisplayName, () => MoveByTeachingPositionAsync(position));
                });
                menu.Items.Add("Teach Current Position", null, (s, e) =>
                {
                    StageTeachingPosition position = GetSelectedTeachingPosition();
                    if (position == null)
                        return;

                    TeachPosition(position);
                    SaveCurrentRecipeData();
                    RefreshView();
                });

                optionParameterGrid.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void OptionParameterGrid_RowDoubleClicked(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                StageTeachingPosition position = FindTeachingPosition(e.Item.Key);
                if (position == null)
                    return;

                TeachPosition(position);
                SaveCurrentRecipeData();
                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "Option double click teach failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private StageTeachingPosition GetSelectedTeachingPosition()
        {
            try
            {
                var item = optionParameterGrid.SelectedItem;
                return item != null ? FindTeachingPosition(item.Key) : null;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "GetSelectedTeachingPosition failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            finally
            {
            }
        }

        private StageTeachingPosition FindTeachingPosition(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            foreach (StageTeachingPosition position in TeachingPositions)
            {
                if (string.Equals(displayName, position.DisplayName, StringComparison.OrdinalIgnoreCase))
                    return position;
            }

            return null;
        }

        private async Task<int> MoveByTeachingPositionAsync(StageTeachingPosition position)
        {
            try
            {
                if (_InputStageUnit == null || position == null)
                    return -1;

                StageAxisPositions positionSet = position.PositionSetGetter(_InputStageUnit);
                BaseAxis axis = position.AxisGetter(_InputStageUnit);
                if (positionSet == null || axis == null)
                    return -1;

                return await MoveAxisAsync(axis, position.Getter(positionSet), false);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void TeachPosition(StageTeachingPosition position)
        {
            try
            {
                if (_InputStageUnit == null || position == null)
                    return;

                StageAxisPositions positionSet = position.PositionSetGetter(_InputStageUnit);
                BaseAxis axis = position.AxisGetter(_InputStageUnit);
                if (positionSet == null || axis == null)
                    return;

                position.Setter(positionSet, axis.ActualPosition);
                EventLogger.Write(EventKind.Event, "UI", "INPUT-STAGE", position.DisplayName + " taught=" + axis.ActualPosition.ToString("F3", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Teach", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindParameterGrids()
        {
            try
            {
                if (_InputStageUnit == null)
                    return;

                optionParameterGrid.SetItems(BuildOptionItems());
                waitParameterGrid.SetItems(BuildWaitItems());
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private IEnumerable<ParameterGridItem> BuildOptionItems()
        {
            try
            {
                var unit = _InputStageUnit;
                unit.Recipe.EnsurePositionObjects();

                var items = new List<ParameterGridItem>();
                AddStagePositions(items, unit);
                
                items.Add(ParameterGridItem.Micron("SAFETY RADIUS", ParameterGridScope.Setup, () => unit.Setup.SafetyRadius, v => unit.Setup.SafetyRadius = Math.Max(0.0, v)));
                items.Add(ParameterGridItem.Int("BARCODE READ TIMEOUT", "ms", ParameterGridScope.Setup, () => unit.Setup.BarcodeReadTimeoutMs, v => unit.Setup.BarcodeReadTimeoutMs = Math.Max(0, v)));
                items.Add(ParameterGridItem.Int("ALIGN ITERATIONS", "count", ParameterGridScope.Config, () => unit.Config.MaxAlignIterations, v => unit.Config.MaxAlignIterations = Math.Max(1, v)));
                items.Add(ParameterGridItem.Double("ALIGN THRESHOLD", "deg", ParameterGridScope.Config, () => unit.Config.AlignConvergenceThresholdDeg, v => unit.Config.AlignConvergenceThresholdDeg = Math.Max(0.0, v)));
                items.Add(ParameterGridItem.Micron("PICK UP EJECT PIN OFFSET", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinOffset, v => unit.Config.PickUpEjectPinOffset = v));
                items.Add(ParameterGridItem.Double("PICK UP EJECT PIN SPEED", "mm/s", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinSpeed, v => unit.Config.PickUpEjectPinSpeed = Math.Max(0.0, v)));
                items.Add(ParameterGridItem.Double("PICK UP EJECT PIN ACC", "mm/s2", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinAcc, v => unit.Config.PickUpEjectPinAcc = Math.Max(0.0, v)));
                items.Add(ParameterGridItem.Double("PICK UP EJECT PIN DEC", "mm/s2", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinDec, v => unit.Config.PickUpEjectPinDec = Math.Max(0.0, v)));
                items.Add(ParameterGridItem.Bool("CONFIG DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));
                items.Add(ParameterGridItem.Bool("SETUP SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
                return items;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void AddStagePositions(List<ParameterGridItem> items, InputStageUnit unit)
        {
            foreach (StageTeachingPosition position in TeachingPositions)
            {
                StageTeachingPosition captured = position;
                items.Add(ParameterGridItem.Micron(captured.DisplayName, ParameterGridScope.Recipe,
                    () => captured.Getter(captured.PositionSetGetter(unit)),
                    v => captured.Setter(captured.PositionSetGetter(unit), v)));
            }
        }

        private IEnumerable<ParameterGridItem> BuildWaitItems()
        {
            try
            {
                return new ParameterGridItem[0];
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void BindIoPanel()
        {
            try
            {
                if (_InputStageUnit == null)
                    return;

                ioCylinderPanel.SetItems(new[]
                {
                    IoCylinderItem.Output("NEEDLE VACUUM", () => _InputStageUnit.NeedleVacuum != null && _InputStageUnit.NeedleVacuum.IsOn, WriteNeedleVacuumAsync)
                });
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindJogPanel()
        {
            try
            {
                if (_InputStageUnit == null)
                    return;

                var items = new List<JogAxisItem>
                {
                    BuildJogAxis("StageY", _InputStageUnit.StageY, "Y+", "Y-", JogAxisControlKind.CrossWithT),
                    BuildJogAxis("StageT", _InputStageUnit.StageT, "T+", "T-", JogAxisControlKind.CrossWithT),
                    BuildJogAxis("ExpanderZ", _InputStageUnit.ExpanderZ, "Z+", "Z-", JogAxisControlKind.Vertical),
                    BuildJogAxis("CameraX", _InputStageUnit.CameraX, "X+", "X-", JogAxisControlKind.CrossWithT),
                    BuildJogAxis("NeedleX", _InputStageUnit.NeedleBlockX, "X+", "X-", JogAxisControlKind.Horizontal),
                    BuildJogAxis("NeedleZ", _InputStageUnit.NeedleZ, "Z+", "Z-", JogAxisControlKind.Vertical),
                    BuildJogAxis("EjectPinZ", _InputStageUnit.EjectPinZ, "Z+", "Z-", JogAxisControlKind.Vertical)
                };

                jogPositionListControl.SetItems(items);
                jogAxisMoveControl.SetItems(items);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private JogAxisItem BuildJogAxis(string name, BaseAxis axis, string plus, string minus, JogAxisControlKind kind)
        {
            JogAxisItem item = JogAxisItem.Single(name, axis, string.Empty, 1.0, plus, minus).WithControlKind(kind);
            item.StepMoveAsync = (it, direction, speedType, customSpeed, axisStepDistance) =>
                _InputStageUnit.JogStepAsync(axis, direction, speedType, customSpeed, axisStepDistance);
            item.ContinuousMoveAsync = (it, direction, speedType, customSpeed) =>
                _InputStageUnit.JogContinuousAsync(axis, direction, speedType, customSpeed);
            item.StopAsync = it => _InputStageUnit.StopJogAsync(axis);
            return item;
        }

        private async Task<int> WriteNeedleVacuumAsync(bool value)
        {
            try
            {
                if (_InputStageUnit == null || _InputStageUnit.NeedleVacuum == null)
                    return -1;

                _InputStageUnit.NeedleVacuum.Write(value);
                await Task.CompletedTask;
                return 0;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "INPUT-STAGE", "Needle vacuum write failed: " + ex.Message);
                return -1;
            }
            finally
            {
            }
        }

        private void ParameterGrid_ParameterValueChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e == null || e.Item == null)
                    return;

                if (e.Item.Scope == ParameterGridScope.Recipe)
                    SaveCurrentRecipeData();
                else
                    SaveCurrentSettingsData();

                RefreshView();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task ConfirmAndRunAsync(string actionName, Func<Task<int>> action)
        {
            try
            {
                if (_InputStageUnit == null || action == null)
                    return;

                DialogResult confirm = QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Input Stage", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                Cursor = Cursors.WaitCursor;
                int result = await action();
                EventLogger.Write(EventKind.Event, "UI", "INPUT-STAGE", actionName + " result=" + result);
                if (result != 0)
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패", "Input Stage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private async Task<int> MoveAxisAsync(BaseAxis axis, double target, bool bFine)
        {
            try
            {
                if (_InputStageUnit == null || axis == null)
                    return -1;

                WaferStageAxis stageAxis;
                if (TryResolveStageAxis(axis, out stageAxis))
                    return await _InputStageUnit.MoveInputStageAxis(stageAxis, target, bFine);

                return await axis.MoveAbsoluteAsync(target, bFine ? ResolveAxisFineVelocity(axis) : ResolveAxisVelocity(axis));
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private bool TryResolveStageAxis(BaseAxis axis, out WaferStageAxis stageAxis)
        {
            stageAxis = WaferStageAxis.WaferY;
            if (_InputStageUnit == null || axis == null)
                return false;

            if (ReferenceEquals(axis, _InputStageUnit.StageY))
            {
                stageAxis = WaferStageAxis.WaferY;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.StageT))
            {
                stageAxis = WaferStageAxis.WaferT;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.ExpanderZ))
            {
                stageAxis = WaferStageAxis.WaferExpandingZ;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.CameraX))
            {
                stageAxis = WaferStageAxis.VisionX;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.NeedleBlockX))
            {
                stageAxis = WaferStageAxis.NeedleX;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.NeedleZ))
            {
                stageAxis = WaferStageAxis.NeedleZ;
                return true;
            }

            if (ReferenceEquals(axis, _InputStageUnit.EjectPinZ))
            {
                stageAxis = WaferStageAxis.EjectPinZ;
                return true;
            }

            return false;
        }

        private async Task<int> MoveCenterAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                Task<int> moveY = MoveAxisAsync(_InputStageUnit.StageY, _InputStageUnit.Recipe.WaferY.ReadyPosition, false);
                Task<int> moveX = MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.ReadyPosition, false);
                int[] results = await Task.WhenAll(moveY, moveX);
                return results[0] != 0 ? results[0] : results[1];
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> MoveCameraOriginAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                return await MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.ReadyPosition, false);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> MoveFirstDieAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                double targetX = Math.Abs(_InputStageUnit.OriginX) > 0.000001 ? _InputStageUnit.OriginX : _InputStageUnit.Recipe.VisionX.ReadyPosition;
                double targetY = Math.Abs(_InputStageUnit.OriginY) > 0.000001 ? _InputStageUnit.OriginY : _InputStageUnit.Recipe.WaferY.ReadyPosition;

                Task<int> moveY = MoveAxisAsync(_InputStageUnit.StageY, targetY, false);
                Task<int> moveX = MoveAxisAsync(_InputStageUnit.CameraX, targetX, false);
                int[] results = await Task.WhenAll(moveY, moveX);
                return results[0] != 0 ? results[0] : results[1];
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> PickTestAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                int result = await MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.ProcessPosition, false);
                if (result != 0)
                    return result;

                await Task.Delay(50).ContinueWith(_ => { });
                return await MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.LoadPosition, false);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void SaveCurrentRecipeData()
        {
            try
            {
                var host = FindHostForm();
                if (host == null || string.IsNullOrWhiteSpace(host.CurrentRecipeName))
                    return;

                host.SaveMachineRecipe(host.CurrentRecipeName);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void SaveCurrentSettingsData()
        {
            try
            {
                var host = FindHostForm();
                host?.SaveMachineSettings();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void RefreshView()
        {
            try
            {
                if (_InputStageUnit == null)
                    return;

                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                lblVisionInfo.Text =
                    "VISION VIEW" + Environment.NewLine +
                    "Origin X : " + FormatUm(_InputStageUnit.OriginX) + Environment.NewLine +
                    "Origin Y : " + FormatUm(_InputStageUnit.OriginY) + Environment.NewLine +
                    "Pitch X  : " + FormatUm(_InputStageUnit.PitchX) + Environment.NewLine +
                    "Pitch Y  : " + FormatUm(_InputStageUnit.PitchY) + Environment.NewLine +
                    "Align X  : " + FormatUm(_InputStageUnit.WaferAlignOffsetX) + Environment.NewLine +
                    "Align Y  : " + FormatUm(_InputStageUnit.WaferAlignOffsetY) + Environment.NewLine +
                    "Needle Vacuum : " + OnOff(_InputStageUnit.NeedleVacuum != null && _InputStageUnit.NeedleVacuum.IsOn);
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string FormatUm(double value)
        {
            try
            {
                return (value * 1000.0).ToString("0.###", CultureInfo.InvariantCulture) + " um";
            }
            catch
            {
                return "0 um";
            }
            finally
            {
            }
        }

        private static double ResolveAxisVelocity(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.DefaultVelocity > 0.0
                ? axis.Config.DefaultVelocity
                : 100.0;
        }

        private static double ResolveAxisFineVelocity(BaseAxis axis)
        {
            return axis != null && axis.Config != null && axis.Config.JogFineVelocity > 0.0
                ? axis.Config.JogFineVelocity
                : ResolveAxisVelocity(axis);
        }

        private static string OnOff(bool value)
        {
            try
            {
                return value ? "ON" : "OFF";
            }
            catch
            {
                return "OFF";
            }
            finally
            {
            }
        }
    }
}
