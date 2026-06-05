using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Controls;
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
    /// <summary>Output Feeder 레시피에서 OutputFeederUnit을 조작하는 화면입니다.</summary>
    public partial class OutputFeederRecipePage : QMC.CDT_320.Ui.Pages.PageBase
    {
        private OutputFeederUnit _outputFeederUnit;
        private readonly Timer _refreshTimer = new Timer();
        private string _titleI18n = "recipe.outputFeeder";

        public OutputFeederRecipePage() : this("recipe.outputFeeder")
        {
        }

        public OutputFeederRecipePage(string titleI18n)
        {
            try
            {
                _titleI18n = titleI18n;
                InitializeComponent();
                if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                    return;

                ApplyRecipeTheme();
                ConfigureRuntimeBehavior();
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(ex.Message, "Output Feeder", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void ConfigureRuntimeBehavior()
        {
            try
            {
                lblHeader.Tag = "i18n:" + _titleI18n;
                lblHeader.Text = Lang.T(_titleI18n);

                _refreshTimer.Interval = 250;
                _refreshTimer.Tick += (s, e) => RefreshView();

                optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;

                grpIo.ContextMenuStrip = new ContextMenuStrip();
                grpIo.ContextMenuStrip.Items.Add("Output feeder DI 상태를 다시 읽습니다.", null, (s, e) => RefreshView());
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _outputFeederUnit = machine != null ? machine.OutputFeeder : null;
                SetEnabledState(_outputFeederUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                btnGoodLoadingMove.Enabled = enabled;
                btnGoodUnloadingMove.Enabled = enabled;
                btnGoodSlotStartMove.Enabled = enabled;
                btnGoodSlotEndMove.Enabled = enabled;
                btnNgLoadingMove.Enabled = enabled;
                btnNgUnloadingMove.Enabled = enabled;
                btnNgSlotStartMove.Enabled = enabled;
                btnNgSlotEndMove.Enabled = enabled;
                btnReadyMove.Enabled = enabled;

                jogPositionListControl.Enabled = enabled;
                jogAxisMoveControl.Enabled = enabled;
                jogSpeedControl.Enabled = enabled;
            }
            catch
            {
            }
            finally
            {
            }
        }

        // ===== ACTION 버튼 (OutputCassette와 동일 구성, Designer에서 Click 배선) =====
        private async void btnReadyMove_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("AVOID MOVE", () => _outputFeederUnit.MoveToFeederAvoidPosition());
        }

        private async void btnGoodLoadingMove_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("GOOD FEEDER LOAD", () => _outputFeederUnit.MoveToFeederCassetteLoadPosition(BinSide.Good, 0));
        }

        private async void btnGoodUnloadingMove_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("GOOD FEEDER UNLOAD", () => _outputFeederUnit.MoveToFeederCassetteUnloadPosition(BinSide.Good, 0));
        }

        private async void btnGoodSlotStartMove_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("GOOD STAGE LOAD", () => _outputFeederUnit.MoveToFeederStageLoadPosition(BinSide.Good));
        }

        private async void btnGoodSlotEndMove_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("GOOD STAGE UNLOAD", () => _outputFeederUnit.MoveToFeederStageUnloadPosition(BinSide.Good));
        }

        private async void btnNgLoadingMove_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("NG FEEDER LOAD", () => _outputFeederUnit.MoveToFeederCassetteLoadPosition(BinSide.Ng, 0));
        }

        private async void btnNgUnloadingMove_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("NG FEEDER UNLOAD", () => _outputFeederUnit.MoveToFeederCassetteUnloadPosition(BinSide.Ng, 0));
        }

        private async void btnNgSlotStartMove_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("NG STAGE LOAD", () => _outputFeederUnit.MoveToFeederStageLoadPosition(BinSide.Ng));
        }

        private async void btnNgSlotEndMove_Click(object sender, EventArgs e)
        {
            await ConfirmMoveAsync("NG STAGE UNLOAD", () => _outputFeederUnit.MoveToFeederStageUnloadPosition(BinSide.Ng));
        }

        private async Task ConfirmMoveAsync(string actionName, Func<Task<int>> move)
        {
            try
            {
                if (_outputFeederUnit == null) return;

                DialogResult result = QMC.Common.MessageDialog.Show(
                    this, actionName + " 진행하시겠습니까?", "Output Feeder Move", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    EventLogger.Write(EventKind.Event, "UI", "OUTPUT-FEEDER", actionName + " canceled.");
                    return;
                }

                await RunSafeAsync(async () =>
                {
                    int r = await move();
                    if (r != 0)
                        return r;

                    return await _outputFeederUnit.WaitBinFeederYMoveDone(_outputFeederUnit.FeederY.Setup.MoveTimeoutMs) ? 0 : -1;
                }, actionName);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, actionName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private async Task RunSafeAsync(Func<Task<int>> action, string actionName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                int result = await action();
                if (result != 0)
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패", "Output Feeder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void BindParameterGrids()
        {
            try
            {
                if (_outputFeederUnit == null)
                    return;

                var unit = _outputFeederUnit;

                optionParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Micron("AVOID POSITION", ParameterGridScope.Recipe, () => unit.Recipe.AvoidPosition, v => unit.Recipe.AvoidPosition = v),

                    ParameterGridItem.Micron("GOOD CASSETTE LOAD", ParameterGridScope.Recipe, () => unit.Recipe.GoodCassetteLoadPosition, v => unit.Recipe.GoodCassetteLoadPosition = v),
                    ParameterGridItem.Micron("GOOD CASSETTE UNLOAD", ParameterGridScope.Recipe, () => unit.Recipe.GoodCassetteUnloadPosition, v => unit.Recipe.GoodCassetteUnloadPosition = v),
                    ParameterGridItem.Micron("GOOD CASSETTE EXCHANGE", ParameterGridScope.Recipe, () => unit.Recipe.GoodCassetteExchangePosition, v => unit.Recipe.GoodCassetteExchangePosition = v),
                    ParameterGridItem.Micron("GOOD WAFER LOAD AVOID", ParameterGridScope.Recipe, () => unit.Recipe.GoodWaferLoadAvoidPosition, v => unit.Recipe.GoodWaferLoadAvoidPosition = v),
                    ParameterGridItem.Micron("GOOD WAFER LOAD", ParameterGridScope.Recipe, () => unit.Recipe.GoodWaferLoadPosition, v => unit.Recipe.GoodWaferLoadPosition = v),
                    ParameterGridItem.Micron("GOOD WAFER UNLOAD AVOID", ParameterGridScope.Recipe, () => unit.Recipe.GoodWaferUnloadAvoidPosition, v => unit.Recipe.GoodWaferUnloadAvoidPosition = v),
                    ParameterGridItem.Micron("GOOD WAFER UNLOAD", ParameterGridScope.Recipe, () => unit.Recipe.GoodWaferUnloadPosition, v => unit.Recipe.GoodWaferUnloadPosition = v),
                    ParameterGridItem.Micron("GOOD WAFER BARCODE", ParameterGridScope.Recipe, () => unit.Recipe.GoodWaferBarcodePosition, v => unit.Recipe.GoodWaferBarcodePosition = v),

                    ParameterGridItem.Micron("NG CASSETTE LOAD", ParameterGridScope.Recipe, () => unit.Recipe.NGCassetteLoadPosition, v => unit.Recipe.NGCassetteLoadPosition = v),
                    ParameterGridItem.Micron("NG CASSETTE UNLOAD", ParameterGridScope.Recipe, () => unit.Recipe.NGCassetteUnloadPosition, v => unit.Recipe.NGCassetteUnloadPosition = v),
                    ParameterGridItem.Micron("NG CASSETTE EXCHANGE", ParameterGridScope.Recipe, () => unit.Recipe.NGCassetteExchangePosition, v => unit.Recipe.NGCassetteExchangePosition = v),
                    ParameterGridItem.Micron("NG WAFER LOAD AVOID", ParameterGridScope.Recipe, () => unit.Recipe.NGWaferLoadAvoidPosition, v => unit.Recipe.NGWaferLoadAvoidPosition = v),
                    ParameterGridItem.Micron("NG WAFER LOAD", ParameterGridScope.Recipe, () => unit.Recipe.NGWaferLoadPosition, v => unit.Recipe.NGWaferLoadPosition = v),
                    ParameterGridItem.Micron("NG WAFER UNLOAD AVOID", ParameterGridScope.Recipe, () => unit.Recipe.NGWaferUnloadAvoidPosition, v => unit.Recipe.NGWaferUnloadAvoidPosition = v),
                    ParameterGridItem.Micron("NG WAFER UNLOAD", ParameterGridScope.Recipe, () => unit.Recipe.NGWaferUnloadPosition, v => unit.Recipe.NGWaferUnloadPosition = v),
                    ParameterGridItem.Micron("NG WAFER BARCODE", ParameterGridScope.Recipe, () => unit.Recipe.NGWaferBarcodePosition, v => unit.Recipe.NGWaferBarcodePosition = v),

                    ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v),
                    ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v)
                });

                waitParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Setup, () => unit.FeederY.Setup.MoveTimeoutMs, v => unit.FeederY.Setup.MoveTimeoutMs = Math.Max(0, v))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindIoPanel()
        {
            try
            {
                if (_outputFeederUnit == null)
                    return;

                var unit = _outputFeederUnit;

                ioCylinderPanel.SetItems(new[]
                {
                    IoCylinderItem.Input("FEEDER UP CHECK", () => unit.IsFeederUp()),
                    IoCylinderItem.Input("FEEDER DOWN CHECK", () => unit.IsFeederDown()),
                    IoCylinderItem.Input("FEEDER UNCLAMP CHECK", () => unit.IsFeederUnclamped()),
                    IoCylinderItem.Input("FEEDER RING CHECK", () => unit.IsBinFeederRingCheck()),
                    IoCylinderItem.Input("FEEDER OVERLOAD CHECK", () => unit.IsFeederOverload()),
                    IoCylinderItem.Output("FEEDER UP", () => unit.BinFeederUpOut.IsOn, on => unit.SetFeederLiftUpOutput(on)),
                    IoCylinderItem.Output("FEEDER DOWN", () => unit.BinFeederDownOut.IsOn, on => unit.SetFeederLiftDownOutput(on)),
                    IoCylinderItem.Output("FEEDER CLAMP", () => unit.BinFeederClampOut.IsOn, on => unit.SetFeederClampOutput(on)),
                    IoCylinderItem.Output("FEEDER UNCLAMP", () => unit.BinFeederUnclampOut.IsOn, on => unit.SetFeederUnclampOutput(on))
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void BindJogPanel()
        {
            try
            {
                if (_outputFeederUnit == null)
                    return;

                var unit = _outputFeederUnit;

                JogAxisItem axisItem = JogAxisItem.Single("BinFeederY", unit.FeederY, "um", 1000.0, "Y+", "Y-").WithControlKind(JogAxisControlKind.Vertical);
                axisItem.StepMoveAsync = async (item, direction, speedType, customSpeed, axisStepDistance) =>
                {
                    double target = unit.FeederY.ActualPosition + (direction * axisStepDistance);
                    int r = await unit.MoveBinFeederY(target, false);
                    if (r != 0)
                        return r;

                    return await unit.WaitBinFeederYMoveDone(unit.FeederY.Setup.MoveTimeoutMs) ? 0 : -1;
                };
                axisItem.ContinuousMoveAsync = (item, direction, speedType, customSpeed) =>
                {
                    unit.ManualMoveBinFeederYJog(direction, customSpeed);
                    return Task.FromResult(0);
                };
                axisItem.StopAsync = item =>
                {
                    unit.ManualStopBinFeederY();
                    return Task.FromResult(0);
                };

                jogAxisMoveControl.SpeedControl = jogSpeedControl;
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.AxisColumns;
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 164;
                jogAxisMoveControl.ButtonAreaMaxHeight = 164;
                jogAxisMoveControl.ButtonAreaMinWidth = 222;
                jogAxisMoveControl.ButtonAreaMaxWidth = 222;
                jogAxisMoveControl.SetItems(new[] { axisItem });
                jogPositionListControl.SetItems(new[] { axisItem });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void ParameterGrid_ParameterValueChanged(object sender, ParameterGridChangedEventArgs e)
        {
            try
            {
                if (e != null && e.Scope == ParameterGridScope.Recipe)
                    SaveCurrentRecipeData();
                else
                    SaveCurrentSettingsData();

                RefreshView();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-FEEDER", "Parameter save failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Parameter Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (_outputFeederUnit == null)
                    return;

                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void ApplyRecipeTheme()
        {
            try
            {
                Color bg = Color.FromArgb(207, 210, 214);
                BackColor = bg;
                rootLayout.BackColor = bg;
                contentLayout.BackColor = bg;
                leftLayout.BackColor = bg;
                centerLayout.BackColor = bg;
                rightLayout.BackColor = bg;

                lblHeader.BackColor = Color.FromArgb(64, 64, 64);
                lblHeader.ForeColor = Color.White;
                lblHeader.Font = new Font("Malgun Gothic", 11F, FontStyle.Bold);

                foreach (var g in new[] { grpActions, grpIo, grpOptions, grpWait, grpJog, grpSpeed })
                {
                    g.BackColor = Color.FromArgb(245, 245, 245);
                    g.Font = new Font("Malgun Gothic", 10F, FontStyle.Bold);
                }

                Color actionColor = Color.FromArgb(88, 94, 103);
                foreach (var b in new[] { btnGoodLoadingMove, btnGoodUnloadingMove, btnGoodSlotStartMove, btnGoodSlotEndMove, btnNgLoadingMove, btnNgUnloadingMove, btnNgSlotStartMove, btnNgSlotEndMove, btnReadyMove })
                {
                    b.BackColor = actionColor;
                    b.ForeColor = Color.White;
                    b.Font = new Font("Malgun Gothic", 8F, FontStyle.Bold);
                }

                Color groupHeaderBg = Color.FromArgb(245, 245, 245);
                Color groupHeaderFg = Color.FromArgb(64, 64, 64);
                Font groupFont = new Font("Malgun Gothic", 8.5F, FontStyle.Bold);
                foreach (var lbl in new[] { lblGoodGroup, lblNgGroup, lblCommonGroup })
                {
                    lbl.BackColor = groupHeaderBg;
                    lbl.ForeColor = groupHeaderFg;
                    lbl.Font = groupFont;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Feeder Theme", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }
    }
}
