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
    /// <summary>Output Stage 레시피에서 OutputStageUnit(Good/Ng 스테이지)을 조작하는 화면입니다.</summary>
    public partial class OutputStageRecipePage : PageBase
    {
        private readonly string _titleI18n;
        private readonly Timer _refreshTimer = new Timer();
        private OutputStageUnit _outputStageUnit;

        public OutputStageRecipePage() : this("recipe.outputStage")
        {
        }

        public OutputStageRecipePage(string titleI18n)
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
                QMC.Common.MessageDialog.Show(ex.Message, "Output Stage", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                BindParameterGridMenus();

                jogAxisMoveControl.SpeedControl = jogSpeedControl;
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.OutputStagePad;
                jogAxisMoveControl.AxisColumnsPerRow = 2;
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 420;
                jogAxisMoveControl.ButtonAreaMaxHeight = 460;
                jogAxisMoveControl.ButtonAreaMinWidth = 170;
                jogAxisMoveControl.ButtonAreaMaxWidth = 320;

                // GOOD 스테이지
                btnLoadingMove.Text = "GOOD LOAD POSITION";
                btnCenterMove.Text = "GOOD PROCESS POSITION";
                btnBarcodeMove.Text = "GOOD UNLOAD POSITION";
                btnFirstDieMove.Text = "GOOD RETICLE POSITION";
                btnLoadingMove.Click += async (s, e) => await ConfirmAndRunAsync(btnLoadingMove.Text, () => _outputStageUnit.MoveToStageLoadPosition(BinSide.Good));
                btnCenterMove.Click += async (s, e) => await ConfirmAndRunAsync(btnCenterMove.Text, () => _outputStageUnit.MoveToStageProcessPosition(BinSide.Good));
                btnBarcodeMove.Click += async (s, e) => await ConfirmAndRunAsync(btnBarcodeMove.Text, () => _outputStageUnit.MoveToStageUnloadPosition(BinSide.Good));
                btnFirstDieMove.Click += async (s, e) => await ConfirmAndRunAsync(btnFirstDieMove.Text, () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.GoodBinY, "Reticle"));
                // NG 스테이지
                btnNeedleUpMove.Text = "NG LOAD POSITION";
                btnNeedleDownMove.Text = "NG PROCESS POSITION";
                btnNeedleReadyMove.Text = "NG UNLOAD POSITION";
                btnNeedleBlockReady.Text = "NG RETICLE POSITION";
                btnNeedleUpMove.Click += async (s, e) => await ConfirmAndRunAsync(btnNeedleUpMove.Text, () => _outputStageUnit.MoveToStageLoadPosition(BinSide.Ng));
                btnNeedleDownMove.Click += async (s, e) => await ConfirmAndRunAsync(btnNeedleDownMove.Text, () => _outputStageUnit.MoveToStageProcessPosition(BinSide.Ng));
                btnNeedleReadyMove.Click += async (s, e) => await ConfirmAndRunAsync(btnNeedleReadyMove.Text, () => _outputStageUnit.MoveToStageUnloadPosition(BinSide.Ng));
                btnNeedleBlockReady.Click += async (s, e) => await ConfirmAndRunAsync(btnNeedleBlockReady.Text, () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.NgBinY, "Reticle"));
                // 공통 / 비전 / Bin 카메라
                btnNeedleBlockWork.Text = "AVOID ALL POSITION";
                btnExpandWorkMove.Text = "VISION AVOID POSITION";
                btnPickUpTest.Text = "VISION PROCESS POSITION";
                btnAutoSettingMove.Text = "BIN CAM WORK POSITION";
                btnInputConversion.Text = "BIN CAM RETRACT POSITION";
                btnNeedleBlockWork.Click += async (s, e) => await ConfirmAndRunAsync(btnNeedleBlockWork.Text, () => _outputStageUnit.MoveToStageAvoidPosition());
                btnExpandWorkMove.Click += async (s, e) => await ConfirmAndRunAsync(btnExpandWorkMove.Text, () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, "Avoid"));
                btnPickUpTest.Click += async (s, e) => await ConfirmAndRunAsync(btnPickUpTest.Text, () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, "Process"));
                btnAutoSettingMove.Click += async (s, e) => await ConfirmAndRunAsync(btnAutoSettingMove.Text, () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, "Process"));
                btnInputConversion.Click += async (s, e) => await ConfirmAndRunAsync(btnInputConversion.Text, () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, "Avoid"));
                // VISION X 티칭 위치 (Process / Reticle)
                btnVisionProcessMove.Text = "VISION X PROCESS POSITION";
                btnVisionReticleMove.Text = "VISION X RETICLE POSITION";
                btnVisionProcessMove.Click += async (s, e) => await ConfirmAndRunAsync(btnVisionProcessMove.Text, () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, "Process"));
                btnVisionReticleMove.Click += async (s, e) => await ConfirmAndRunAsync(btnVisionReticleMove.Text, () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, "Reticle"));
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _outputStageUnit = machine != null ? machine.OutputStageUnit : null;
                if (_outputStageUnit != null)
                    _outputStageUnit.Recipe.EnsurePositionObjects();
                SetEnabledState(_outputStageUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (_outputStageUnit == null)
                    return;

                _outputStageUnit.Recipe.EnsurePositionObjects();
                var unit = _outputStageUnit;
                var items = new List<ParameterGridItem>();

                // Recipe 위치 (Good/Ng/Vision) — avoid/load/process/unload/reticle 순
                AddStagePositions(items, "GOOD Y", () => unit.Recipe.GoodStageY, true, true, true, true, false);
                AddStagePositions(items, "GOOD Z", () => unit.Recipe.GoodStageZ, true, true, true, true, false);
                AddStagePositions(items, "NG Y", () => unit.Recipe.NGStageY, true, true, true, true, false);
                AddStagePositions(items, "VISION X", () => unit.Recipe.VisionX, true, false, true, false, true);

                items.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
                items.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));

                optionParameterGrid.SetItems(items);

                waitParameterGrid.SetItems(new ParameterGridItem[0]);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static void AddStagePositions(List<ParameterGridItem> items, string axisLabel, Func<StageAxisPositions> set,
            bool avoid, bool load, bool process, bool unload, bool reticle)
        {
            if (avoid) items.Add(ParameterGridItem.Micron(axisLabel + " AVOID POSITION", ParameterGridScope.Recipe, () => set().AvoidPosition, v => set().AvoidPosition = v));
            if (load) items.Add(ParameterGridItem.Micron(axisLabel + " LOAD POSITION", ParameterGridScope.Recipe, () => set().LoadPosition, v => set().LoadPosition = v));
            if (process) items.Add(ParameterGridItem.Micron(axisLabel + " PROCESS POSITION", ParameterGridScope.Recipe, () => set().ProcessPosition, v => set().ProcessPosition = v));
            if (unload) items.Add(ParameterGridItem.Micron(axisLabel + " UNLOAD POSITION", ParameterGridScope.Recipe, () => set().UnloadPosition, v => set().UnloadPosition = v));
            if (reticle) items.Add(ParameterGridItem.Micron(axisLabel + " RETICLE POSITION", ParameterGridScope.Recipe, () => set().ReticlePosition, v => set().ReticlePosition = v));
        }

        private void BindParameterGridMenus()
        {
            try
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add("Move To Position", null, async (s, e) =>
                {
                    BinStageAxis axis;
                    string positionName;
                    if (TryGetSelectedTeachingPosition(out axis, out positionName))
                        await ConfirmAndRunAsync(optionParameterGrid.SelectedItem.Key, () => _outputStageUnit.MoveStageAxisToTeachingPosition(axis, positionName, true));
                });
                menu.Items.Add("Teach Current Position", null, (s, e) =>
                {
                    BinStageAxis axis;
                    string positionName;
                    if (!TryGetSelectedTeachingPosition(out axis, out positionName))
                        return;

                    _outputStageUnit.TeachStageAxisPosition(axis, positionName);
                    SaveCurrentRecipeData();
                    RefreshView();
                });

                optionParameterGrid.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool TryGetSelectedTeachingPosition(out BinStageAxis axis, out string positionName)
        {
            axis = BinStageAxis.GoodBinY;
            positionName = string.Empty;

            var item = optionParameterGrid.SelectedItem;
            string key = item != null ? item.Key : string.Empty;
            if (string.IsNullOrWhiteSpace(key) || !key.EndsWith(" POSITION", StringComparison.OrdinalIgnoreCase))
                return false;

            string name = key.Substring(0, key.Length - " POSITION".Length);
            if (name.StartsWith("GOOD Y ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.GoodBinY;
                positionName = name.Substring("GOOD Y ".Length);
                return true;
            }
            if (name.StartsWith("GOOD Z ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.GoodBinZ;
                positionName = name.Substring("GOOD Z ".Length);
                return true;
            }
            if (name.StartsWith("NG Y ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.NgBinY;
                positionName = name.Substring("NG Y ".Length);
                return true;
            }
            if (name.StartsWith("VISION X ", StringComparison.OrdinalIgnoreCase))
            {
                axis = BinStageAxis.VisionX;
                positionName = name.Substring("VISION X ".Length);
                return true;
            }

            return false;
        }

        private void BindIoPanel()
        {
            try
            {
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                ioCylinderPanel.SetItems(new[]
                {
                    // ===== INPUT (DI) =====
                    IoCylinderItem.Input("NG BIN GUIDE UP", () => unit.NgBinGuideUpSensor.IsOn),
                    IoCylinderItem.Input("NG BIN GUIDE DOWN", () => unit.NgBinGuideDownSensor.IsOn),
                    IoCylinderItem.Input("NG BIN CLAMP UP", () => unit.NgBinClampUpSensor.IsOn),
                    IoCylinderItem.Input("NG BIN UNCLAMP", () => unit.NgBinUnclampSensor.IsOn),
                    IoCylinderItem.Input("NG BIN RING CHECK", () => unit.NgBinRingSensor.IsOn),
                    IoCylinderItem.Input("GOOD BIN GUIDE UP", () => unit.GoodBinGuideUpSensor.IsOn),
                    IoCylinderItem.Input("GOOD BIN GUIDE DOWN", () => unit.GoodBinGuideDownSensor.IsOn),
                    IoCylinderItem.Input("GOOD BIN CLAMP UP", () => unit.GoodBinClampUpSensor.IsOn),
                    IoCylinderItem.Input("GOOD BIN UNCLAMP", () => unit.GoodBinUnclampSensor.IsOn),
                    IoCylinderItem.Input("GOOD BIN RING CHECK", () => unit.GoodBinRingSensor.IsOn),

                    // ===== OUTPUT (DO) — 개별 14개 =====
                    IoCylinderItem.Output("NG BIN GUIDE UP", () => unit.NgBinGuideUpOut.IsOn, on => WriteOutAsync(unit.NgBinGuideUpOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("NG BIN GUIDE DOWN", () => unit.NgBinGuideDownOut.IsOn, on => WriteOutAsync(unit.NgBinGuideDownOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("NG BIN CLAMP UP", () => unit.NgBinClampUpOut.IsOn, on => WriteOutAsync(unit.NgBinClampUpOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("NG BIN CLAMP DOWN", () => unit.NgBinClampDownOut.IsOn, on => WriteOutAsync(unit.NgBinClampDownOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("NG BIN CLAMP", () => unit.NgBinClampOut.IsOn, on => WriteOutAsync(unit.NgBinClampOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("NG BIN UNCLAMP", () => unit.NgBinUnclampOut.IsOn, on => WriteOutAsync(unit.NgBinUnclampOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("GOOD BIN GUIDE UP", () => unit.GoodBinGuideUpOut.IsOn, on => WriteOutAsync(unit.GoodBinGuideUpOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("GOOD BIN GUIDE DOWN", () => unit.GoodBinGuideDownOut.IsOn, on => WriteOutAsync(unit.GoodBinGuideDownOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("GOOD BIN CLAMP UP", () => unit.GoodBinClampUpOut.IsOn, on => WriteOutAsync(unit.GoodBinClampUpOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("GOOD BIN CLAMP DOWN", () => unit.GoodBinClampDownOut.IsOn, on => WriteOutAsync(unit.GoodBinClampDownOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("GOOD BIN CLAMP", () => unit.GoodBinClampOut.IsOn, on => WriteOutAsync(unit.GoodBinClampOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("GOOD BIN UNCLAMP", () => unit.GoodBinUnclampOut.IsOn, on => WriteOutAsync(unit.GoodBinUnclampOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("BOTTOM VISION BLOW ON", () => unit.BottomVisionBlowOnOut.IsOn, on => WriteOutAsync(unit.BottomVisionBlowOnOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("BOTTOM VISION BLOW OFF", () => unit.BottomVisionBlowOffOut.IsOn, on => WriteOutAsync(unit.BottomVisionBlowOffOut, on), "ON", "OFF")
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static void WriteOut(QMC.Common.IO.BaseDigitalOutput output, bool on)
        {
            if (output == null) return;
            if (on) output.On(); else output.Off();
        }

        private static Task<int> WritePairOut(QMC.Common.IO.BaseDigitalOutput forward, QMC.Common.IO.BaseDigitalOutput backward, bool forwardOn)
        {
            try
            {
                WriteOut(forward, forwardOn);
                WriteOut(backward, !forwardOn);
                return Task.FromResult(0);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static Task<int> WriteOutAsync(QMC.Common.IO.BaseDigitalOutput output, bool on)
        {
            try
            {
                WriteOut(output, on);
                return Task.FromResult(0);
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private void BindJogPanel()
        {
            try
            {
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                // 축 순서: GoodY/Z, NgY/Z, VisionX
                var items = new List<JogAxisItem>
                {
                    BuildJogAxis("GOOD Y", unit.GoodStage.StageY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("GOOD Z", unit.GoodStage.StageZ, "Z+", "Z-", JogAxisControlKind.Vertical),
                    BuildJogAxis("NG Y", unit.NgStage.StageY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("NG Z", unit.NgStage.StageZ, "Z+", "Z-", JogAxisControlKind.Vertical),
                    BuildJogAxis("VISION X", unit.OutputCameraX, "X+", "X-", JogAxisControlKind.Vertical)
                };

                jogPositionListControl.SetItems(items);
                jogAxisMoveControl.SetItems(items);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "OUTPUT-STAGE", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private JogAxisItem BuildJogAxis(string name, BaseAxis axis, string plus, string minus, JogAxisControlKind kind)
        {
            JogAxisItem item = JogAxisItem.Single(name, axis, "um", 1000.0, plus, minus).WithControlKind(kind);
            item.StepMoveAsync = (it, direction, speedType, customSpeed, axisStepDistance) =>
                _outputStageUnit.JogStepAsync(axis, direction, speedType, customSpeed, axisStepDistance);
            item.ContinuousMoveAsync = (it, direction, speedType, customSpeed) =>
                _outputStageUnit.JogContinuousAsync(axis, direction, speedType, customSpeed);
            item.StopAsync = it => _outputStageUnit.StopJogAsync(axis);
            return item;
        }

        private async Task ConfirmAndRunAsync(string actionName, Func<Task<int>> action)
        {
            try
            {
                if (_outputStageUnit == null || action == null)
                    return;

                DialogResult confirm = QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Output Stage", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                Cursor = Cursors.WaitCursor;
                int result = await action();
                EventLogger.Write(EventKind.Event, "UI", "OUTPUT-STAGE", actionName + " result=" + result);
                if (result != 0)
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패", "Output Stage", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                lblVisionInfo.Text =
                    "OUTPUT STAGE" + Environment.NewLine +
                    "GOOD Y : " + FormatMm(unit.GoodStage.StageY.ActualPosition) + Environment.NewLine +
                    "GOOD Z : " + FormatMm(unit.GoodStage.StageZ.ActualPosition) + Environment.NewLine +
                    "NG Y   : " + FormatMm(unit.NgStage.StageY.ActualPosition) + Environment.NewLine +
                    "NG Z   : " + FormatMm(unit.NgStage.StageZ.ActualPosition) + Environment.NewLine +
                    "BIN X  : " + FormatMm(unit.OutputCameraX.ActualPosition) + Environment.NewLine +
                    "G-AVOID: " + OnOff(unit.GoodStage.IsAtAvoidPosition()) + Environment.NewLine +
                    "N-AVOID: " + OnOff(unit.NgStage.IsAtAvoidPosition());
            }
            catch
            {
            }
            finally
            {
            }
        }

        private static string FormatMm(double value)
        {
            try
            {
                return value.ToString("0.###", CultureInfo.InvariantCulture) + " mm";
            }
            catch
            {
                return "0 mm";
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
