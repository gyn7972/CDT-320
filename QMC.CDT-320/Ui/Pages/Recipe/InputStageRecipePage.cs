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
        private sealed class StageTeachingPosition
        {
            public StageTeachingPosition(
                string displayName,
                Func<InputStageUnit, BaseAxis> axisGetter,
                Func<InputStageUnit, StageAxisPositions> positionSetGetter,
                Func<StageAxisPositions, double> getter,
                Action<StageAxisPositions, double> setter)
            {
                DisplayName = displayName;
                AxisGetter = axisGetter;
                PositionSetGetter = positionSetGetter;
                Getter = getter;
                Setter = setter;
            }

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
            var positions = new List<StageTeachingPosition>();
            AddAxisTeachingPositions(positions, "WAFER Y", unit => unit.StageY, unit => unit.Recipe.WaferY);
            AddAxisTeachingPositions(positions, "WAFER T", unit => unit.StageT, unit => unit.Recipe.WaferT);
            AddAxisTeachingPositions(positions, "EXPANDER Z", unit => unit.ExpanderZ, unit => unit.Recipe.WaferZ);
            AddAxisTeachingPositions(positions, "VISION X", unit => unit.CameraX, unit => unit.Recipe.VisionX);
            AddAxisTeachingPositions(positions, "NEEDLE X", unit => unit.NeedleBlockX, unit => unit.Recipe.NeedleX);
            AddAxisTeachingPositions(positions, "NEEDLE Z", unit => unit.NeedleZ, unit => unit.Recipe.NeedleZ);
            AddAxisTeachingPositions(positions, "EJECT PIN Z", unit => unit.EjectPinZ, unit => unit.Recipe.EjectPinZ);
            return positions.ToArray();
        }

        private static void AddAxisTeachingPositions(
            List<StageTeachingPosition> positions,
            string axisLabel,
            Func<InputStageUnit, BaseAxis> axisGetter,
            Func<InputStageUnit, StageAxisPositions> positionSetGetter)
        {
            positions.Add(new StageTeachingPosition(axisLabel + " AVOID POSITION", axisGetter, positionSetGetter, set => set.AvoidPosition, (set, value) => set.AvoidPosition = value));
            positions.Add(new StageTeachingPosition(axisLabel + " LOAD POSITION", axisGetter, positionSetGetter, set => set.LoadPosition, (set, value) => set.LoadPosition = value));
            positions.Add(new StageTeachingPosition(axisLabel + " PROCESS POSITION", axisGetter, positionSetGetter, set => set.ProcessPosition, (set, value) => set.ProcessPosition = value));
            positions.Add(new StageTeachingPosition(axisLabel + " UNLOAD POSITION", axisGetter, positionSetGetter, set => set.UnloadPosition, (set, value) => set.UnloadPosition = value));
            positions.Add(new StageTeachingPosition(axisLabel + " READY POSITION", axisGetter, positionSetGetter, set => set.ReadyPosition, (set, value) => set.ReadyPosition = value));
            positions.Add(new StageTeachingPosition(axisLabel + " RETICLE POSITION", axisGetter, positionSetGetter, set => set.ReticlePosition, (set, value) => set.ReticlePosition = value));
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

                _toolTip.SetToolTip(optionParameterGrid, "Input Stage 티칭 위치와 설정값을 편집합니다.");
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

            BindTeachingActionButton(btnLoadingMove, "WAFER Y AVOID POSITION");
            BindTeachingActionButton(btnCenterMove, "WAFER Y LOAD POSITION");
            BindTeachingActionButton(btnBarcodeMove, "WAFER Y PROCESS POSITION");
            BindTeachingActionButton(btnFirstDieMove, "WAFER Y UNLOAD POSITION");
            BindTeachingActionButton(btnPickUpTest, "WAFER Y READY POSITION");
            BindTeachingActionButton(btnNeedleUpMove, "WAFER Y RETICLE POSITION");
            BindTeachingActionButton(btnNeedleDownMove, "WAFER T AVOID POSITION");
            BindTeachingActionButton(btnNeedleReadyMove, "WAFER T LOAD POSITION");
            BindTeachingActionButton(btnNeedleBlockReady, "WAFER T PROCESS POSITION");
            BindTeachingActionButton(btnNeedleBlockWork, "WAFER T UNLOAD POSITION");
            BindTeachingActionButton(btnAutoSettingMove, "WAFER T READY POSITION");
            BindTeachingActionButton(btnInputConversion, "WAFER T RETICLE POSITION");
            BindTeachingActionButton(btnExpandWorkMove, "EXPANDER Z AVOID POSITION");
            BindTeachingActionButton(btnExpanderZLoad, "EXPANDER Z LOAD POSITION");
            BindTeachingActionButton(btnExpanderZProcess, "EXPANDER Z PROCESS POSITION");
            BindTeachingActionButton(btnExpanderZUnload, "EXPANDER Z UNLOAD POSITION");
            BindTeachingActionButton(btnExpanderZReady, "EXPANDER Z READY POSITION");
            BindTeachingActionButton(btnExpanderZReticle, "EXPANDER Z RETICLE POSITION");
            BindTeachingActionButton(btnVisionXAvoid, "VISION X AVOID POSITION");
            BindTeachingActionButton(btnVisionXLoad, "VISION X LOAD POSITION");
            BindTeachingActionButton(btnVisionXProcess, "VISION X PROCESS POSITION");
            BindTeachingActionButton(btnVisionXUnload, "VISION X UNLOAD POSITION");
            BindTeachingActionButton(btnVisionXReady, "VISION X READY POSITION");
            BindTeachingActionButton(btnVisionXReticle, "VISION X RETICLE POSITION");
            BindTeachingActionButton(btnNeedleXAvoid, "NEEDLE X AVOID POSITION");
            BindTeachingActionButton(btnNeedleXLoad, "NEEDLE X LOAD POSITION");
            BindTeachingActionButton(btnNeedleXProcess, "NEEDLE X PROCESS POSITION");
            BindTeachingActionButton(btnNeedleXUnload, "NEEDLE X UNLOAD POSITION");
            BindTeachingActionButton(btnNeedleXReady, "NEEDLE X READY POSITION");
            BindTeachingActionButton(btnNeedleXReticle, "NEEDLE X RETICLE POSITION");
            BindTeachingActionButton(btnNeedleZAvoid, "NEEDLE Z AVOID POSITION");
            BindTeachingActionButton(btnNeedleZLoad, "NEEDLE Z LOAD POSITION");
            BindTeachingActionButton(btnNeedleZProcess, "NEEDLE Z PROCESS POSITION");
            BindTeachingActionButton(btnNeedleZUnload, "NEEDLE Z UNLOAD POSITION");
            BindTeachingActionButton(btnNeedleZReady, "NEEDLE Z READY POSITION");
            BindTeachingActionButton(btnNeedleZReticle, "NEEDLE Z RETICLE POSITION");
            BindTeachingActionButton(btnEjectPinZAvoid, "EJECT PIN Z AVOID POSITION");
            BindTeachingActionButton(btnEjectPinZLoad, "EJECT PIN Z LOAD POSITION");
            BindTeachingActionButton(btnEjectPinZProcess, "EJECT PIN Z PROCESS POSITION");
            BindTeachingActionButton(btnEjectPinZUnload, "EJECT PIN Z UNLOAD POSITION");
            BindTeachingActionButton(btnEjectPinZReady, "EJECT PIN Z READY POSITION");
            BindTeachingActionButton(btnEjectPinZReticle, "EJECT PIN Z RETICLE POSITION");
            BindActionButton(btnInputStagePickTest, "PICK TEST", PickTestAsync);
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

                var items = new List<ParameterGridItem>
                {
                    ParameterGridItem.Bool("SETUP SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v),
                    ParameterGridItem.Micron("SAFETY RADIUS", ParameterGridScope.Setup, () => unit.Setup.SafetyRadius, v => unit.Setup.SafetyRadius = Math.Max(0.0, v)),
                    ParameterGridItem.Int("BARCODE READ TIMEOUT", "ms", ParameterGridScope.Setup, () => unit.Setup.BarcodeReadTimeoutMs, v => unit.Setup.BarcodeReadTimeoutMs = Math.Max(0, v)),
                    ParameterGridItem.Bool("CONFIG DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v),
                    ParameterGridItem.Micron("PICK UP EJECT PIN OFFSET", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinOffset, v => unit.Config.PickUpEjectPinOffset = v),
                    ParameterGridItem.Double("PICK UP EJECT PIN SPEED", "mm/s", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinSpeed, v => unit.Config.PickUpEjectPinSpeed = Math.Max(0.0, v)),
                    ParameterGridItem.Double("PICK UP EJECT PIN ACC", "mm/s2", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinAcc, v => unit.Config.PickUpEjectPinAcc = Math.Max(0.0, v)),
                    ParameterGridItem.Double("PICK UP EJECT PIN DEC", "mm/s2", ParameterGridScope.Config, () => unit.Config.PickUpEjectPinDec, v => unit.Config.PickUpEjectPinDec = Math.Max(0.0, v)),
                    ParameterGridItem.Int("ALIGN ITERATIONS", "count", ParameterGridScope.Config, () => unit.Config.MaxAlignIterations, v => unit.Config.MaxAlignIterations = Math.Max(1, v)),
                    ParameterGridItem.Double("ALIGN THRESHOLD", "deg", ParameterGridScope.Config, () => unit.Config.AlignConvergenceThresholdDeg, v => unit.Config.AlignConvergenceThresholdDeg = Math.Max(0.0, v))
                };

                AddStagePositions(items, "WAFER Y", () => unit.Recipe.WaferY);
                AddStagePositions(items, "WAFER T", () => unit.Recipe.WaferT);
                AddStagePositions(items, "EXPANDER Z", () => unit.Recipe.WaferZ);
                AddStagePositions(items, "VISION X", () => unit.Recipe.VisionX);
                AddStagePositions(items, "NEEDLE X", () => unit.Recipe.NeedleX);
                AddStagePositions(items, "NEEDLE Z", () => unit.Recipe.NeedleZ);
                AddStagePositions(items, "EJECT PIN Z", () => unit.Recipe.EjectPinZ);

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

        private static void AddStagePositions(List<ParameterGridItem> items, string axisLabel, Func<StageAxisPositions> set)
        {
            items.Add(ParameterGridItem.Micron(axisLabel + " AVOID POSITION", ParameterGridScope.Recipe, () => set().AvoidPosition, v => set().AvoidPosition = v));
            items.Add(ParameterGridItem.Micron(axisLabel + " LOAD POSITION", ParameterGridScope.Recipe, () => set().LoadPosition, v => set().LoadPosition = v));
            items.Add(ParameterGridItem.Micron(axisLabel + " PROCESS POSITION", ParameterGridScope.Recipe, () => set().ProcessPosition, v => set().ProcessPosition = v));
            items.Add(ParameterGridItem.Micron(axisLabel + " UNLOAD POSITION", ParameterGridScope.Recipe, () => set().UnloadPosition, v => set().UnloadPosition = v));
            items.Add(ParameterGridItem.Micron(axisLabel + " READY POSITION", ParameterGridScope.Recipe, () => set().ReadyPosition, v => set().ReadyPosition = v));
            items.Add(ParameterGridItem.Micron(axisLabel + " RETICLE POSITION", ParameterGridScope.Recipe, () => set().ReticlePosition, v => set().ReticlePosition = v));
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
