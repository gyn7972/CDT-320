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
                _refreshTimer.Tick += (s, e) => RefreshView();
                optionParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;
                waitParameterGrid.ParameterValueChanged += ParameterGrid_ParameterValueChanged;

                jogAxisMoveControl.SpeedControl = jogSpeedControl;
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.AxisColumns;
                jogAxisMoveControl.AxisColumnsPerRow = 2; // 4축을 2x2 그리드로 배치
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 340;
                jogAxisMoveControl.ButtonAreaMaxHeight = 360;
                jogAxisMoveControl.ButtonAreaMinWidth = 170;
                jogAxisMoveControl.ButtonAreaMaxWidth = 320;

                // GOOD 스테이지
                btnLoadingMove.Click += async (s, e) => await ConfirmAndRunAsync("GOOD LOAD", () => _outputStageUnit.MoveToStageLoadPosition(BinSide.Good));
                btnCenterMove.Click += async (s, e) => await ConfirmAndRunAsync("GOOD PROCESS", () => _outputStageUnit.MoveToStageProcessPosition(BinSide.Good));
                btnBarcodeMove.Click += async (s, e) => await ConfirmAndRunAsync("GOOD UNLOAD", () => _outputStageUnit.MoveToStageUnloadPosition(BinSide.Good));
                btnFirstDieMove.Click += async (s, e) => await ConfirmAndRunAsync("GOOD RETICLE", () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.GoodBinY, "Reticle"));
                // NG 스테이지
                btnNeedleUpMove.Click += async (s, e) => await ConfirmAndRunAsync("NG LOAD", () => _outputStageUnit.MoveToStageLoadPosition(BinSide.Ng));
                btnNeedleDownMove.Click += async (s, e) => await ConfirmAndRunAsync("NG PROCESS", () => _outputStageUnit.MoveToStageProcessPosition(BinSide.Ng));
                btnNeedleReadyMove.Click += async (s, e) => await ConfirmAndRunAsync("NG UNLOAD", () => _outputStageUnit.MoveToStageUnloadPosition(BinSide.Ng));
                btnNeedleBlockReady.Click += async (s, e) => await ConfirmAndRunAsync("NG RETICLE", () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.NgBinY, "Reticle"));
                // 공통 / 비전 / Bin 카메라
                btnNeedleBlockWork.Click += async (s, e) => await ConfirmAndRunAsync("AVOID ALL", () => _outputStageUnit.MoveToStageAvoidPosition());
                btnExpandWorkMove.Click += async (s, e) => await ConfirmAndRunAsync("VISION LOAD", () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, "Load"));
                btnPickUpTest.Click += async (s, e) => await ConfirmAndRunAsync("VISION PROCESS", () => _outputStageUnit.MoveStageAxisToTeachingPosition(BinStageAxis.VisionX, "Process"));
                btnAutoSettingMove.Click += async (s, e) => await ConfirmAndRunAsync("BIN CAM WORK", () => _outputStageUnit.BinCameraX.MoveAbsoluteAsync(_outputStageUnit.Setup.BinCameraWorkPositionX, _outputStageUnit.Recipe.BinCameraVelocity));
                btnInputConversion.Click += async (s, e) => await ConfirmAndRunAsync("BIN CAM RETRACT", () => _outputStageUnit.BinCameraX.MoveAbsoluteAsync(_outputStageUnit.Setup.BinCameraRetractPositionX, _outputStageUnit.Recipe.BinCameraVelocity));
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Output Stage Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                AddStagePositions(items, "GOOD Y", () => unit.Recipe.GoodBinY, true, true, true, true, false);
                AddStagePositions(items, "GOOD Z", () => unit.Recipe.GoodBinZ, true, true, true, true, false);
                AddStagePositions(items, "NG Y", () => unit.Recipe.NGBinY, true, true, true, true, false);
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
            if (avoid) items.Add(ParameterGridItem.Micron(axisLabel + " AVOID", ParameterGridScope.Recipe, () => set().AvoidPosition, v => set().AvoidPosition = v));
            if (load) items.Add(ParameterGridItem.Micron(axisLabel + " LOAD", ParameterGridScope.Recipe, () => set().LoadPosition, v => set().LoadPosition = v));
            if (process) items.Add(ParameterGridItem.Micron(axisLabel + " PROCESS", ParameterGridScope.Recipe, () => set().ProcessPosition, v => set().ProcessPosition = v));
            if (unload) items.Add(ParameterGridItem.Micron(axisLabel + " UNLOAD", ParameterGridScope.Recipe, () => set().UnloadPosition, v => set().UnloadPosition = v));
            if (reticle) items.Add(ParameterGridItem.Micron(axisLabel + " RETICLE", ParameterGridScope.Recipe, () => set().ReticlePosition, v => set().ReticlePosition = v));
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

                    // ===== OUTPUT (DO) =====
                    IoCylinderItem.Output("NG BIN GUIDE UP", () => unit.NgBinGuideUpOut.IsOn, on => WriteOut(unit.NgBinGuideUpOut, on)),
                    IoCylinderItem.Output("NG BIN GUIDE DOWN", () => unit.NgBinGuideDownOut.IsOn, on => WriteOut(unit.NgBinGuideDownOut, on)),
                    IoCylinderItem.Output("NG BIN CLAMP UP", () => unit.NgBinClampUpOut.IsOn, on => WriteOut(unit.NgBinClampUpOut, on)),
                    IoCylinderItem.Output("NG BIN CLAMP DOWN", () => unit.NgBinClampDownOut.IsOn, on => WriteOut(unit.NgBinClampDownOut, on)),
                    IoCylinderItem.Output("NG BIN CLAMP", () => unit.NgBinClampOut.IsOn, on => WriteOut(unit.NgBinClampOut, on)),
                    IoCylinderItem.Output("NG BIN UNCLAMP", () => unit.NgBinUnclampOut.IsOn, on => WriteOut(unit.NgBinUnclampOut, on)),
                    IoCylinderItem.Output("GOOD BIN GUIDE UP", () => unit.GoodBinGuideUpOut.IsOn, on => WriteOut(unit.GoodBinGuideUpOut, on)),
                    IoCylinderItem.Output("GOOD BIN GUIDE DOWN", () => unit.GoodBinGuideDownOut.IsOn, on => WriteOut(unit.GoodBinGuideDownOut, on)),
                    IoCylinderItem.Output("GOOD BIN CLAMP UP", () => unit.GoodBinClampUpOut.IsOn, on => WriteOut(unit.GoodBinClampUpOut, on)),
                    IoCylinderItem.Output("GOOD BIN CLAMP DOWN", () => unit.GoodBinClampDownOut.IsOn, on => WriteOut(unit.GoodBinClampDownOut, on)),
                    IoCylinderItem.Output("GOOD BIN CLAMP", () => unit.GoodBinClampOut.IsOn, on => WriteOut(unit.GoodBinClampOut, on)),
                    IoCylinderItem.Output("GOOD BIN UNCLAMP", () => unit.GoodBinUnclampOut.IsOn, on => WriteOut(unit.GoodBinUnclampOut, on)),
                    IoCylinderItem.Output("BOTTOM VISION BLOW ON", () => unit.BottomVisionBlowOnOut.IsOn, on => WriteOut(unit.BottomVisionBlowOnOut, on)),
                    IoCylinderItem.Output("BOTTOM VISION BLOW OFF", () => unit.BottomVisionBlowOffOut.IsOn, on => WriteOut(unit.BottomVisionBlowOffOut, on))
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

        private void BindJogPanel()
        {
            try
            {
                if (_outputStageUnit == null)
                    return;

                var unit = _outputStageUnit;
                // 축 순서: NgBinY → NgBinZ → GoodBinY → VisionX (세로 컬럼 4개 나란히)
                var items = new List<JogAxisItem>
                {
                    BuildJogAxis("NG Y", unit.NgStage.StageY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("NG Z", unit.NgStage.StageZ, "Z+", "Z-", JogAxisControlKind.Vertical),
                    BuildJogAxis("GOOD Y", unit.GoodStage.StageY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("VISION X", unit.BinCameraX, "X+", "X-", JogAxisControlKind.Vertical)
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
                if (e != null && e.Scope == ParameterGridScope.Recipe)
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
                    "BIN X  : " + FormatMm(unit.BinCameraX.ActualPosition) + Environment.NewLine +
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
