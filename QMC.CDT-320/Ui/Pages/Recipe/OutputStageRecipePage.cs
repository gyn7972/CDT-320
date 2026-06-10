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

                // 매뉴얼 액션 버튼 — 옵션 그룹과 매칭, GOOD/NG 분리
                BuildManualButtons();
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

                // Recipe 위치 — 위치 종류별 접이식 그룹 (멤버 = GOOD Y/Z, NG Y, VISION X)
                AddKindGroup(items, "AVOID POSITION", "Avoid", true, true, true, true);
                AddKindGroup(items, "LOAD POSITION", "Load", true, true, true, false);
                AddKindGroup(items, "PROCESS POSITION", "Process", true, true, true, true);
                AddKindGroup(items, "UNLOAD POSITION", "Unload", true, true, true, false);
                AddKindGroup(items, "RETICLE POSITION", "Reticle", false, false, false, true);

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

        // 한 위치 종류(kind)를 헤더로 묶고, 그 종류를 가진 축들을 멤버로 추가
        private void AddKindGroup(List<ParameterGridItem> items, string kindLabel, string kind, bool goodY, bool goodZ, bool ng, bool vision)
        {
            string groupKey = "K_" + kind.ToUpperInvariant();
            items.Add(ParameterGridItem.Header(kindLabel, groupKey));

            var recipe = _outputStageUnit.Recipe;
            if (goodY) items.Add(StageMember("GOOD Y", groupKey, kindLabel, kind, () => recipe.GoodStageY));
            if (goodZ) items.Add(StageMember("GOOD Z", groupKey, kindLabel, kind, () => recipe.GoodStageZ));
            if (ng) items.Add(StageMember("NG Y", groupKey, kindLabel, kind, () => recipe.NGStageY));
            if (vision) items.Add(StageMember("VISION X", groupKey, kindLabel, kind, () => recipe.VisionX));
        }

        private static ParameterGridItem StageMember(string axisLabel, string groupKey, string kindLabel, string kind, Func<StageAxisPositions> set)
        {
            Func<double> getter;
            Action<double> setter;
            switch (kind)
            {
                case "Avoid": getter = () => set().AvoidPosition; setter = v => set().AvoidPosition = v; break;
                case "Load": getter = () => set().LoadPosition; setter = v => set().LoadPosition = v; break;
                case "Process": getter = () => set().ProcessPosition; setter = v => set().ProcessPosition = v; break;
                case "Unload": getter = () => set().UnloadPosition; setter = v => set().UnloadPosition = v; break;
                case "Reticle": getter = () => set().ReticlePosition; setter = v => set().ReticlePosition = v; break;
                default: getter = () => 0.0; setter = v => { }; break;
            }

            var item = ParameterGridItem.Micron(axisLabel, ParameterGridScope.Recipe, getter, setter);
            item.Key = axisLabel + " " + kindLabel;   // 이동/티칭 조회는 전체 이름(Key)으로 파싱
            item.GroupKey = groupKey;
            return item;
        }

        // 매뉴얼 액션 버튼을 옵션 그룹과 매칭해 구성 (GOOD/NG는 무조건 분리)
        private void BuildManualButtons()
        {
            manualLayout.SuspendLayout();
            try
            {
                manualLayout.Controls.Clear();
                manualLayout.RowStyles.Clear();
                manualLayout.RowCount = 0;
                manualLayout.ColumnCount = 2;

                // 좌측 = GOOD, 우측 = NG
                AddManualButton("GOOD AVOID POSITION", 0, 0, () => MoveStageAxesAsync("Avoid", BinStageAxis.GoodBinY, BinStageAxis.GoodBinZ));
                AddManualButton("NG AVOID POSITION", 1, 0, () => MoveStageAxesAsync("Avoid", BinStageAxis.NgBinY));
                AddManualButton("GOOD LOAD POSITION", 0, 1, () => _outputStageUnit.MoveToStageLoadPosition(BinSide.Good));
                AddManualButton("NG LOAD POSITION", 1, 1, () => _outputStageUnit.MoveToStageLoadPosition(BinSide.Ng));
                AddManualButton("GOOD PROCESS POSITION", 0, 2, () => _outputStageUnit.MoveToStageProcessPosition(BinSide.Good));
                AddManualButton("NG PROCESS POSITION", 1, 2, () => _outputStageUnit.MoveToStageProcessPosition(BinSide.Ng));
                AddManualButton("GOOD UNLOAD POSITION", 0, 3, () => _outputStageUnit.MoveToStageUnloadPosition(BinSide.Good));
                AddManualButton("NG UNLOAD POSITION", 1, 3, () => _outputStageUnit.MoveToStageUnloadPosition(BinSide.Ng));

                // VISION 축 (별도)
                AddManualButton("VISION AVOID POSITION", 0, 4, () => MoveStageAxesAsync("Avoid", BinStageAxis.VisionX));
                AddManualButton("VISION PROCESS POSITION", 1, 4, () => MoveStageAxesAsync("Process", BinStageAxis.VisionX));
                AddManualButton("VISION RETICLE POSITION", 0, 5, () => MoveStageAxesAsync("Reticle", BinStageAxis.VisionX));
            }
            finally
            {
                manualLayout.ResumeLayout(true);
            }
        }

        private void AddManualButton(string text, int column, int row, Func<Task<int>> command)
        {
            QMC.CDT_320.Ui.Controls.ActionButton button = new QMC.CDT_320.Ui.Controls.ActionButton();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(2);
            button.BackColor = Color.FromArgb(128, 128, 128);
            button.ForeColor = Color.White;
            button.Font = new Font("Malgun Gothic", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.Click += async delegate { await ConfirmAndRunAsync(text, command); };

            while (manualLayout.RowStyles.Count <= row)
                manualLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            if (manualLayout.RowCount <= row)
                manualLayout.RowCount = row + 1;

            manualLayout.Controls.Add(button, column, row);
        }

        // 지정한 축들을 같은 위치명으로 동시 이동
        private async Task<int> MoveStageAxesAsync(string positionName, params BinStageAxis[] axes)
        {
            if (_outputStageUnit == null || axes == null || axes.Length == 0)
                return -1;

            List<Task<int>> tasks = new List<Task<int>>();
            foreach (BinStageAxis ax in axes)
                tasks.Add(_outputStageUnit.MoveStageAxisToTeachingPosition(ax, positionName));

            int[] results = await Task.WhenAll(tasks);
            int finalResult = 0;
            foreach (int r in results)
            {
                if (r != 0)
                    finalResult = r;
            }
            return finalResult;
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
                    // ===== NG BIN : 같은 항목의 입력(DI)/출력(DO)을 묶어서 =====
                    IoCylinderItem.Input("NG BIN GUIDE UP", () => unit.NgBinGuideUpSensor.IsOn),
                    IoCylinderItem.Output("NG BIN GUIDE UP", () => unit.NgBinGuideUpOut.IsOn, on => WriteOutAsync(unit.NgBinGuideUpOut, on), "ON", "OFF"),
                    IoCylinderItem.Input("NG BIN GUIDE DOWN", () => unit.NgBinGuideDownSensor.IsOn),
                    IoCylinderItem.Output("NG BIN GUIDE DOWN", () => unit.NgBinGuideDownOut.IsOn, on => WriteOutAsync(unit.NgBinGuideDownOut, on), "ON", "OFF"),
                    IoCylinderItem.Input("NG BIN CLAMP UP", () => unit.NgBinClampUpSensor.IsOn),
                    IoCylinderItem.Output("NG BIN CLAMP UP", () => unit.NgBinClampUpOut.IsOn, on => WriteOutAsync(unit.NgBinClampUpOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("NG BIN CLAMP DOWN", () => unit.NgBinClampDownOut.IsOn, on => WriteOutAsync(unit.NgBinClampDownOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("NG BIN CLAMP", () => unit.NgBinClampOut.IsOn, on => WriteOutAsync(unit.NgBinClampOut, on), "ON", "OFF"),
                    IoCylinderItem.Input("NG BIN UNCLAMP", () => unit.NgBinUnclampSensor.IsOn),
                    IoCylinderItem.Output("NG BIN UNCLAMP", () => unit.NgBinUnclampOut.IsOn, on => WriteOutAsync(unit.NgBinUnclampOut, on), "ON", "OFF"),
                    IoCylinderItem.Input("NG BIN RING CHECK", () => unit.NgBinRingSensor.IsOn),

                    // ===== GOOD BIN : 같은 항목의 입력(DI)/출력(DO)을 묶어서 =====
                    IoCylinderItem.Input("GOOD BIN GUIDE UP", () => unit.GoodBinGuideUpSensor.IsOn),
                    IoCylinderItem.Output("GOOD BIN GUIDE UP", () => unit.GoodBinGuideUpOut.IsOn, on => WriteOutAsync(unit.GoodBinGuideUpOut, on), "ON", "OFF"),
                    IoCylinderItem.Input("GOOD BIN GUIDE DOWN", () => unit.GoodBinGuideDownSensor.IsOn),
                    IoCylinderItem.Output("GOOD BIN GUIDE DOWN", () => unit.GoodBinGuideDownOut.IsOn, on => WriteOutAsync(unit.GoodBinGuideDownOut, on), "ON", "OFF"),
                    IoCylinderItem.Input("GOOD BIN CLAMP UP", () => unit.GoodBinClampUpSensor.IsOn),
                    IoCylinderItem.Output("GOOD BIN CLAMP UP", () => unit.GoodBinClampUpOut.IsOn, on => WriteOutAsync(unit.GoodBinClampUpOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("GOOD BIN CLAMP DOWN", () => unit.GoodBinClampDownOut.IsOn, on => WriteOutAsync(unit.GoodBinClampDownOut, on), "ON", "OFF"),
                    IoCylinderItem.Output("GOOD BIN CLAMP", () => unit.GoodBinClampOut.IsOn, on => WriteOutAsync(unit.GoodBinClampOut, on), "ON", "OFF"),
                    IoCylinderItem.Input("GOOD BIN UNCLAMP", () => unit.GoodBinUnclampSensor.IsOn),
                    IoCylinderItem.Output("GOOD BIN UNCLAMP", () => unit.GoodBinUnclampOut.IsOn, on => WriteOutAsync(unit.GoodBinUnclampOut, on), "ON", "OFF"),
                    IoCylinderItem.Input("GOOD BIN RING CHECK", () => unit.GoodBinRingSensor.IsOn),

                    // ===== BOTTOM VISION (출력) =====
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

                if (ManualMoveGuard.BlockIfNotReady(this, "Output Stage"))
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
