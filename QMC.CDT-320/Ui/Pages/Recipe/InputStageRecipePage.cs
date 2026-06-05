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
        private readonly string _titleI18n;
        private readonly Timer _refreshTimer = new Timer();
        private InputStageUnit _InputStageUnit;

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

            BindActionButton(btnLoadingMove, "WAFER Y AVOID", () => MoveAxisAsync(_InputStageUnit.StageY, _InputStageUnit.Recipe.WaferY.AvoidPosition, false));
            BindActionButton(btnCenterMove, "WAFER Y LOAD", () => MoveAxisAsync(_InputStageUnit.StageY, _InputStageUnit.Recipe.WaferY.LoadPosition, false));
            BindActionButton(btnBarcodeMove, "WAFER Y PROCESS", () => MoveAxisAsync(_InputStageUnit.StageY, _InputStageUnit.Recipe.WaferY.ProcessPosition, false));
            BindActionButton(btnFirstDieMove, "WAFER Y UNLOAD", () => MoveAxisAsync(_InputStageUnit.StageY, _InputStageUnit.Recipe.WaferY.UnloadPosition, false));
            BindActionButton(btnPickUpTest, "WAFER Y READY", () => MoveAxisAsync(_InputStageUnit.StageY, _InputStageUnit.Recipe.WaferY.ReadyPosition, false));
            BindActionButton(btnNeedleUpMove, "WAFER Y RETICLE", () => MoveAxisAsync(_InputStageUnit.StageY, _InputStageUnit.Recipe.WaferY.ReticlePosition, false));
            BindActionButton(btnNeedleDownMove, "WAFER T AVOID", () => MoveAxisAsync(_InputStageUnit.StageT, _InputStageUnit.Recipe.WaferT.AvoidPosition, false));
            BindActionButton(btnNeedleReadyMove, "WAFER T LOAD", () => MoveAxisAsync(_InputStageUnit.StageT, _InputStageUnit.Recipe.WaferT.LoadPosition, false));
            BindActionButton(btnNeedleBlockReady, "WAFER T PROCESS", () => MoveAxisAsync(_InputStageUnit.StageT, _InputStageUnit.Recipe.WaferT.ProcessPosition, false));
            BindActionButton(btnNeedleBlockWork, "WAFER T UNLOAD", () => MoveAxisAsync(_InputStageUnit.StageT, _InputStageUnit.Recipe.WaferT.UnloadPosition, false));
            BindActionButton(btnAutoSettingMove, "WAFER T READY", () => MoveAxisAsync(_InputStageUnit.StageT, _InputStageUnit.Recipe.WaferT.ReadyPosition, false));
            BindActionButton(btnInputConversion, "WAFER T RETICLE", () => MoveAxisAsync(_InputStageUnit.StageT, _InputStageUnit.Recipe.WaferT.ReticlePosition, false));
            BindActionButton(btnExpandWorkMove, "EXPANDER Z AVOID", () => MoveAxisAsync(_InputStageUnit.ExpanderZ, _InputStageUnit.Recipe.WaferZ.AvoidPosition, false));
            BindActionButton(btnExpanderZLoad, "EXPANDER Z LOAD", () => MoveAxisAsync(_InputStageUnit.ExpanderZ, _InputStageUnit.Recipe.WaferZ.LoadPosition, false));
            BindActionButton(btnExpanderZProcess, "EXPANDER Z PROCESS", () => MoveAxisAsync(_InputStageUnit.ExpanderZ, _InputStageUnit.Recipe.WaferZ.ProcessPosition, false));
            BindActionButton(btnExpanderZUnload, "EXPANDER Z UNLOAD", () => MoveAxisAsync(_InputStageUnit.ExpanderZ, _InputStageUnit.Recipe.WaferZ.UnloadPosition, false));
            BindActionButton(btnExpanderZReady, "EXPANDER Z READY", () => MoveAxisAsync(_InputStageUnit.ExpanderZ, _InputStageUnit.Recipe.WaferZ.ReadyPosition, false));
            BindActionButton(btnExpanderZReticle, "EXPANDER Z RETICLE", () => MoveAxisAsync(_InputStageUnit.ExpanderZ, _InputStageUnit.Recipe.WaferZ.ReticlePosition, false));
            BindActionButton(btnVisionXAvoid, "VISION X AVOID", () => MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.AvoidPosition, false));
            BindActionButton(btnVisionXLoad, "VISION X LOAD", () => MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.LoadPosition, false));
            BindActionButton(btnVisionXProcess, "VISION X PROCESS", () => MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.ProcessPosition, false));
            BindActionButton(btnVisionXUnload, "VISION X UNLOAD", () => MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.UnloadPosition, false));
            BindActionButton(btnVisionXReady, "VISION X READY", () => MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.ReadyPosition, false));
            BindActionButton(btnVisionXReticle, "VISION X RETICLE", () => MoveAxisAsync(_InputStageUnit.CameraX, _InputStageUnit.Recipe.VisionX.ReticlePosition, false));
            BindActionButton(btnNeedleXAvoid, "NEEDLE X AVOID", () => MoveAxisAsync(_InputStageUnit.NeedleBlockX, _InputStageUnit.Recipe.NeedleX.AvoidPosition, false));
            BindActionButton(btnNeedleXLoad, "NEEDLE X LOAD", () => MoveAxisAsync(_InputStageUnit.NeedleBlockX, _InputStageUnit.Recipe.NeedleX.LoadPosition, false));
            BindActionButton(btnNeedleXProcess, "NEEDLE X PROCESS", () => MoveAxisAsync(_InputStageUnit.NeedleBlockX, _InputStageUnit.Recipe.NeedleX.ProcessPosition, false));
            BindActionButton(btnNeedleXUnload, "NEEDLE X UNLOAD", () => MoveAxisAsync(_InputStageUnit.NeedleBlockX, _InputStageUnit.Recipe.NeedleX.UnloadPosition, false));
            BindActionButton(btnNeedleXReady, "NEEDLE X READY", () => MoveAxisAsync(_InputStageUnit.NeedleBlockX, _InputStageUnit.Recipe.NeedleX.ReadyPosition, false));
            BindActionButton(btnNeedleXReticle, "NEEDLE X RETICLE", () => MoveAxisAsync(_InputStageUnit.NeedleBlockX, _InputStageUnit.Recipe.NeedleX.ReticlePosition, false));
            BindActionButton(btnNeedleZAvoid, "NEEDLE Z AVOID", () => MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.AvoidPosition, false));
            BindActionButton(btnNeedleZLoad, "NEEDLE Z LOAD", () => MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.LoadPosition, false));
            BindActionButton(btnNeedleZProcess, "NEEDLE Z PROCESS", () => MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.ProcessPosition, false));
            BindActionButton(btnNeedleZUnload, "NEEDLE Z UNLOAD", () => MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.UnloadPosition, false));
            BindActionButton(btnNeedleZReady, "NEEDLE Z READY", () => MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.ReadyPosition, false));
            BindActionButton(btnNeedleZReticle, "NEEDLE Z RETICLE", () => MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Recipe.NeedleZ.ReticlePosition, false));
            BindActionButton(btnEjectPinZAvoid, "EJECT PIN Z AVOID", () => MoveAxisAsync(_InputStageUnit.EjectPinZ, _InputStageUnit.Recipe.EjectPinZ.AvoidPosition, false));
            BindActionButton(btnEjectPinZLoad, "EJECT PIN Z LOAD", () => MoveAxisAsync(_InputStageUnit.EjectPinZ, _InputStageUnit.Recipe.EjectPinZ.LoadPosition, false));
            BindActionButton(btnEjectPinZProcess, "EJECT PIN Z PROCESS", () => MoveAxisAsync(_InputStageUnit.EjectPinZ, _InputStageUnit.Recipe.EjectPinZ.ProcessPosition, false));
            BindActionButton(btnEjectPinZUnload, "EJECT PIN Z UNLOAD", () => MoveAxisAsync(_InputStageUnit.EjectPinZ, _InputStageUnit.Recipe.EjectPinZ.UnloadPosition, false));
            BindActionButton(btnEjectPinZReady, "EJECT PIN Z READY", () => MoveAxisAsync(_InputStageUnit.EjectPinZ, _InputStageUnit.Recipe.EjectPinZ.ReadyPosition, false));
            BindActionButton(btnEjectPinZReticle, "EJECT PIN Z RETICLE", () => MoveAxisAsync(_InputStageUnit.EjectPinZ, _InputStageUnit.Recipe.EjectPinZ.ReticlePosition, false));
            BindActionButton(btnInputStagePickTest, "PICK TEST", PickTestAsync);
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
