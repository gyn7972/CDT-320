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

                btnLoadingMove.Click += async (s, e) => await ConfirmAndRunAsync("EXPANDER DOWN", () => MoveAxisAsync(_InputStageUnit.ExpanderZ, _InputStageUnit.Setup.ExpanderDownPosition, _InputStageUnit.Recipe.MoveVelocity));
                btnCenterMove.Click += async (s, e) => await ConfirmAndRunAsync("CENTER MOVE", MoveCenterAsync);
                btnBarcodeMove.Click += async (s, e) => await ConfirmAndRunAsync("BARCODE MOVE", MoveCameraOriginAsync);
                btnFirstDieMove.Click += async (s, e) => await ConfirmAndRunAsync("FIRST DIE MOVE", MoveFirstDieAsync);
                btnNeedleUpMove.Click += async (s, e) => await ConfirmAndRunAsync("NEEDLE UP", () => MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Setup.NeedleEjectPosition, _InputStageUnit.Recipe.NeedleVelocity));
                btnNeedleDownMove.Click += async (s, e) => await ConfirmAndRunAsync("NEEDLE DOWN", () => MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Setup.NeedleDownPosition, _InputStageUnit.Recipe.NeedleVelocity));
                btnNeedleReadyMove.Click += async (s, e) => await ConfirmAndRunAsync("NEEDLE READY", () => MoveAxisAsync(_InputStageUnit.NeedleZ, _InputStageUnit.Setup.NeedleDownPosition, _InputStageUnit.Recipe.NeedleVelocity));
                btnNeedleBlockReady.Click += async (s, e) => await ConfirmAndRunAsync("NEEDLE BLOCK READY", () => MoveAxisAsync(_InputStageUnit.NeedleBlockX, _InputStageUnit.Setup.NeedleTeachX, _InputStageUnit.Recipe.MoveVelocity));
                btnNeedleBlockWork.Click += async (s, e) => await ConfirmAndRunAsync("NEEDLE BLOCK WORK", () => MoveAxisAsync(_InputStageUnit.NeedleBlockX, _InputStageUnit.Setup.NeedleTeachX + _InputStageUnit.Setup.PickerOffsetX, _InputStageUnit.Recipe.MoveVelocity));
                btnExpandWorkMove.Click += async (s, e) => await ConfirmAndRunAsync("EXPAND WORK", () => MoveAxisAsync(_InputStageUnit.ExpanderZ, _InputStageUnit.Setup.ExpanderDownPosition, _InputStageUnit.Recipe.MoveVelocity));
                btnPickUpTest.Click += async (s, e) => await ConfirmAndRunAsync("PICK TEST", PickTestAsync);
                btnAutoSettingMove.Click += async (s, e) => await ConfirmAndRunAsync("VISION ALIGN", () => _InputStageUnit.VisionAlignAndSetupOriginAsync());
                btnInputConversion.Click += async (s, e) => await ConfirmAndRunAsync("LOAD/PREPARE", () => _InputStageUnit.LoadAndPrepareWaferAsync());
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Input Stage Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
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
                _InputStageUnit = machine != null ? machine.InputStage : null;
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
                return new[]
                {
                    ParameterGridItem.Micron("EXPANDER DOWN Z", ParameterGridScope.Setup, () => unit.Setup.ExpanderDownPosition, v => unit.Setup.ExpanderDownPosition = Math.Max(-1000.0, v)),
                    ParameterGridItem.Micron("EXPANDER UP Z", ParameterGridScope.Setup, () => unit.Setup.ExpanderUpPosition, v => unit.Setup.ExpanderUpPosition = v),
                    ParameterGridItem.Micron("UNLOAD STAGE Y", ParameterGridScope.Setup, () => unit.Setup.UnloadPositionY, v => unit.Setup.UnloadPositionY = v),
                    ParameterGridItem.Micron("NEEDLE EJECT Z", ParameterGridScope.Setup, () => unit.Setup.NeedleEjectPosition, v => unit.Setup.NeedleEjectPosition = v),
                    ParameterGridItem.Micron("NEEDLE DOWN Z", ParameterGridScope.Setup, () => unit.Setup.NeedleDownPosition, v => unit.Setup.NeedleDownPosition = v),
                    ParameterGridItem.Micron("PICKER OFFSET Y", ParameterGridScope.Setup, () => unit.Setup.PickerOffsetY, v => unit.Setup.PickerOffsetY = v),
                    ParameterGridItem.Micron("PICKER OFFSET X", ParameterGridScope.Setup, () => unit.Setup.PickerOffsetX, v => unit.Setup.PickerOffsetX = v),
                    ParameterGridItem.Micron("CAMERA ORIGIN X", ParameterGridScope.Setup, () => unit.Setup.CameraOriginX, v => unit.Setup.CameraOriginX = v),
                    ParameterGridItem.Micron("CAMERA FOCUS Z", ParameterGridScope.Setup, () => unit.Setup.CameraFocusZ, v => unit.Setup.CameraFocusZ = v),
                    ParameterGridItem.Micron("CAMERA SAFE Z", ParameterGridScope.Setup, () => unit.Setup.CameraSafeZ, v => unit.Setup.CameraSafeZ = v),
                    ParameterGridItem.Micron("STAGE Y TEACH", ParameterGridScope.Setup, () => unit.Setup.StageYTeachPosition, v => unit.Setup.StageYTeachPosition = v),
                    ParameterGridItem.Micron("NEEDLE TEACH X", ParameterGridScope.Setup, () => unit.Setup.NeedleTeachX, v => unit.Setup.NeedleTeachX = v),
                    ParameterGridItem.Double("VISION PIXEL", "mm/px", ParameterGridScope.Setup, () => unit.Setup.VisionPixelToMm, v => unit.Setup.VisionPixelToMm = Math.Max(0.0, v)),
                    ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Config, () => unit.Config.IsSimulationMode, v => unit.Config.IsSimulationMode = v),
                    ParameterGridItem.Int("ALIGN ITERATIONS", "count", ParameterGridScope.Config, () => unit.Config.MaxAlignIterations, v => unit.Config.MaxAlignIterations = Math.Max(1, v)),
                    ParameterGridItem.Double("ALIGN THRESHOLD", "deg", ParameterGridScope.Config, () => unit.Config.AlignConvergenceThresholdDeg, v => unit.Config.AlignConvergenceThresholdDeg = Math.Max(0.0, v)),
                    ParameterGridItem.Double("MOVE VELOCITY", "mm/s", ParameterGridScope.Recipe, () => unit.Recipe.MoveVelocity, v => unit.Recipe.MoveVelocity = Math.Max(0.1, v)),
                    ParameterGridItem.Double("ALIGN VELOCITY", "mm/s", ParameterGridScope.Recipe, () => unit.Recipe.AlignVelocity, v => unit.Recipe.AlignVelocity = Math.Max(0.1, v)),
                    ParameterGridItem.Double("NEEDLE VELOCITY", "mm/s", ParameterGridScope.Recipe, () => unit.Recipe.NeedleVelocity, v => unit.Recipe.NeedleVelocity = Math.Max(0.1, v)),
                    ParameterGridItem.Micron("DEFAULT PITCH X", ParameterGridScope.Recipe, () => unit.Recipe.DefaultPitchX, v => unit.Recipe.DefaultPitchX = Math.Max(0.0, v)),
                    ParameterGridItem.Micron("DEFAULT PITCH Y", ParameterGridScope.Recipe, () => unit.Recipe.DefaultPitchY, v => unit.Recipe.DefaultPitchY = Math.Max(0.0, v))
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private IEnumerable<ParameterGridItem> BuildWaitItems()
        {
            try
            {
                var unit = _InputStageUnit;
                return new[]
                {
                    ParameterGridItem.Int("NEEDLE VACUUM SETTLE", "ms", ParameterGridScope.Recipe, () => unit.Recipe.NeedleVacuumSettleMs, v => unit.Recipe.NeedleVacuumSettleMs = Math.Max(0, v)),
                    ParameterGridItem.Int("VISION EXPOSE TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.VisionExposeTimeoutMs, v => unit.Recipe.VisionExposeTimeoutMs = Math.Max(0, v)),
                    ParameterGridItem.Int("VISION RESULT TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.VisionResultTimeoutMs, v => unit.Recipe.VisionResultTimeoutMs = Math.Max(0, v)),
                    ParameterGridItem.Int("PICKER UP TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.PickerUpTimeoutMs, v => unit.Recipe.PickerUpTimeoutMs = Math.Max(0, v)),
                    ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.MoveTimeoutMs, v => unit.Recipe.MoveTimeoutMs = Math.Max(0, v))
                };
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
                    IoCylinderItem.Output("NEEDLE VACUUM", () => _InputStageUnit.NeedleVacuum != null && _InputStageUnit.NeedleVacuum.IsOn, WriteNeedleVacuumAsync),
                    IoCylinderItem.Input("8 INCH RING CHECK", () => _InputStageUnit.IsWaferStage8RingChecked()),
                    IoCylinderItem.Input("12 INCH RING CHECK", () => _InputStageUnit.IsWaferStage12RingChecked())
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
                    JogAxisItem.Single("StageY", _InputStageUnit.StageY, string.Empty, 1.0, "Y+", "Y-").WithControlKind(JogAxisControlKind.CrossWithT),
                    JogAxisItem.Single("StageT", _InputStageUnit.StageT, string.Empty, 1.0, "T+", "T-").WithControlKind(JogAxisControlKind.CrossWithT),
                    JogAxisItem.Single("ExpanderZ", _InputStageUnit.ExpanderZ, string.Empty, 1.0, "Z+", "Z-").WithControlKind(JogAxisControlKind.Vertical),
                    JogAxisItem.Single("CameraX", _InputStageUnit.CameraX, string.Empty, 1.0, "X+", "X-").WithControlKind(JogAxisControlKind.CrossWithT),
                    JogAxisItem.Single("NeedleX", _InputStageUnit.NeedleBlockX, string.Empty, 1.0, "X+", "X-").WithControlKind(JogAxisControlKind.Horizontal),
                    JogAxisItem.Single("NeedleZ", _InputStageUnit.NeedleZ, string.Empty, 1.0, "Z+", "Z-").WithControlKind(JogAxisControlKind.Vertical),
                    JogAxisItem.Single("EjectPinZ", _InputStageUnit.EjectPinZ, string.Empty, 1.0, "Z+", "Z-").WithControlKind(JogAxisControlKind.Vertical),
                    JogAxisItem.Single("CameraZ", _InputStageUnit.CameraZ, string.Empty, 1.0, "Z+", "Z-").WithControlKind(JogAxisControlKind.Vertical)
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

        private async Task<int> MoveAxisAsync(BaseAxis axis, double target, double velocity)
        {
            try
            {
                if (axis == null)
                    return -1;

                return await axis.MoveAbsoluteAsync(target, velocity);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private async Task<int> MoveCenterAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                Task<int> moveY = _InputStageUnit.StageY.MoveAbsoluteAsync(_InputStageUnit.Setup.StageYTeachPosition, _InputStageUnit.Recipe.MoveVelocity);
                Task<int> moveX = _InputStageUnit.CameraX.MoveAbsoluteAsync(_InputStageUnit.Setup.CameraOriginX, _InputStageUnit.Recipe.MoveVelocity);
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

                Task<int> moveX = _InputStageUnit.CameraX.MoveAbsoluteAsync(_InputStageUnit.Setup.CameraOriginX, _InputStageUnit.Recipe.MoveVelocity);
                Task<int> moveZ = _InputStageUnit.CameraZ.MoveAbsoluteAsync(_InputStageUnit.Setup.CameraFocusZ, _InputStageUnit.Recipe.MoveVelocity);
                int[] results = await Task.WhenAll(moveX, moveZ);
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

        private async Task<int> MoveFirstDieAsync()
        {
            try
            {
                if (_InputStageUnit == null)
                    return -1;

                double targetX = Math.Abs(_InputStageUnit.OriginX) > 0.000001 ? _InputStageUnit.OriginX : _InputStageUnit.Setup.CameraOriginX;
                double targetY = Math.Abs(_InputStageUnit.OriginY) > 0.000001 ? _InputStageUnit.OriginY : _InputStageUnit.Setup.StageYTeachPosition;

                Task<int> moveY = _InputStageUnit.StageY.MoveAbsoluteAsync(targetY, _InputStageUnit.Recipe.MoveVelocity);
                Task<int> moveX = _InputStageUnit.CameraX.MoveAbsoluteAsync(targetX, _InputStageUnit.Recipe.MoveVelocity);
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

                int result = await _InputStageUnit.NeedleZ.MoveAbsoluteAsync(_InputStageUnit.Setup.NeedleEjectPosition, _InputStageUnit.Recipe.NeedleVelocity);
                if (result != 0)
                    return result;

                await Task.Delay(Math.Max(0, _InputStageUnit.Recipe.NeedleVacuumSettleMs)).ContinueWith(_ => { });
                return await _InputStageUnit.NeedleZ.MoveAbsoluteAsync(_InputStageUnit.Setup.NeedleDownPosition, _InputStageUnit.Recipe.NeedleVelocity);
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
                    "Ring 8   : " + OnOff(_InputStageUnit.IsWaferStage8RingChecked()) + Environment.NewLine +
                    "Ring 12  : " + OnOff(_InputStageUnit.IsWaferStage12RingChecked());
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
