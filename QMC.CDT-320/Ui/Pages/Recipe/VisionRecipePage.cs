using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT320;
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
    /// <summary>Vision 레시피에서 VisionUnit(Front/Rear Side Vision Y축 · 레티클/니들 I/O)을 조작하는 화면입니다.</summary>
    public partial class VisionRecipePage : PageBase
    {
        private readonly string _titleI18n;
        private readonly Timer _refreshTimer = new Timer();
        private VisionUnit _visionUnit;

        public VisionRecipePage() : this("recipe.inputVision")
        {
        }

        public VisionRecipePage(string titleI18n)
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
                QMC.Common.MessageDialog.Show(ex.Message, "Vision", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Vision Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try { if (Visible) _refreshTimer.Start(); else _refreshTimer.Stop(); } catch { }
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Vision Layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                jogAxisMoveControl.LayoutMode = JogAxisMoveLayoutMode.AxisColumns;
                // Front Y / Rear Y 두 축을 한 행에 좌우로 나란히(2열) 배치하고, 두 열 사이 간격을 넓게 둔다.
                jogAxisMoveControl.AxisColumnsPerRow = 2;
                jogAxisMoveControl.AxisColumnGap = 60;
                jogAxisMoveControl.ShowCurrentSpeedMode = true;
                jogAxisMoveControl.ButtonAreaMinHeight = 420;
                jogAxisMoveControl.ButtonAreaMaxHeight = 460;
                jogAxisMoveControl.ButtonAreaMinWidth = 170;
                jogAxisMoveControl.ButtonAreaMaxWidth = 320;
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Vision Configure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!ShouldRefreshVisible(this))
                    return;

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
                _visionUnit = machine != null ? machine.VisionUnit : null;
                if (_visionUnit != null && _visionUnit.Recipe != null)
                    _visionUnit.Recipe.EnsurePositionObjects();
                SetEnabledState(_visionUnit != null);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show(this, ex.Message, "Vision Resolve", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (_visionUnit == null)
                    return;

                _visionUnit.Recipe.EnsurePositionObjects();
                var unit = _visionUnit;
                var items = new List<ParameterGridItem>();

                // Recipe 위치 — 위치 종류별 접이식 그룹 (멤버 = FRONT Y / REAR Y)
                AddKindGroup(items, "AVOID POSITION", "Avoid");
                AddKindGroup(items, "PROCESS POSITION (0°)", "Process0");
                AddKindGroup(items, "PROCESS POSITION (90°)", "Process90");

                items.Add(ParameterGridItem.Bool("SIMULATION MODE", ParameterGridScope.Setup, () => unit.Setup.IsSimulationMode, v => unit.Setup.IsSimulationMode = v));
                items.Add(ParameterGridItem.Bool("DRY RUN", ParameterGridScope.Config, () => unit.Config.bDryRun, v => unit.Config.bDryRun = v));

                optionParameterGrid.SetItems(items);

                // WAIT TIME — VisionRecipe 타임아웃 값
                waitParameterGrid.SetItems(new[]
                {
                    ParameterGridItem.Int("MOVE TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.MoveTimeoutMs, v => unit.Recipe.MoveTimeoutMs = v),
                    ParameterGridItem.Int("I/O TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.IoTimeoutMs, v => unit.Recipe.IoTimeoutMs = v),
                    ParameterGridItem.Int("CAPTURE TIMEOUT", "ms", ParameterGridScope.Recipe, () => unit.Recipe.CaptureTimeoutMs, v => unit.Recipe.CaptureTimeoutMs = v)
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "BindParameterGrids failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Vision Parameters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        // 한 위치 종류(kind)를 헤더로 묶고, Front/Rear Side Vision Y축을 멤버로 추가
        private void AddKindGroup(List<ParameterGridItem> items, string kindLabel, string kind)
        {
            string groupKey = "K_" + kind.ToUpperInvariant();
            items.Add(ParameterGridItem.Header(kindLabel, groupKey));

            items.Add(VisionMember("FRONT Y", groupKey, kindLabel, kind, VisionSide.Front));
            items.Add(VisionMember("REAR Y", groupKey, kindLabel, kind, VisionSide.Rear));
        }

        private ParameterGridItem VisionMember(string axisLabel, string groupKey, string kindLabel, string kind, VisionSide side)
        {
            VisionAxisPositions positions = side == VisionSide.Front ? _visionUnit.Recipe.FrontSideVision : _visionUnit.Recipe.RearSideVision;
            BaseAxis axis = side == VisionSide.Front ? _visionUnit.FrontSideVisionY : _visionUnit.RearSideVisionY;

            Func<double> getter;
            Action<double> setter;
            switch (kind)
            {
                // Avoid 위치 레시피 연결
                case "Avoid": getter = () => positions.AvoidPosition; setter = v => positions.AvoidPosition = v; break;
                // Process 위치(0도) 레시피 연결
                case "Process0": getter = () => positions.Process0Position; setter = v => positions.Process0Position = v; break;
                // Process 위치(90도) 레시피 연결
                case "Process90": getter = () => positions.Process90Position; setter = v => positions.Process90Position = v; break;
                default: getter = () => 0.0; setter = v => { }; break;
            }

            var item = AxisDouble(axisLabel, ParameterGridScope.Recipe, axis, getter, setter);
            item.Key = axisLabel + " " + kindLabel;   // 이동/티칭 조회는 전체 이름(Key)으로 파싱
            item.GroupKey = groupKey;
            return item;
        }

        // ===================== 매뉴얼 액션 (Front/Rear 통합 단일 버튼) =====================
        private async void btnAvoidPosition_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("AVOID POSITION", () => _visionUnit.MoveToVisionAvoidPosition());
        }

        private async void btnProcessPosition0_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("PROCESS POSITION (0°)", MoveBothProcess0Async);
        }

        private async void btnProcessPosition90_Click(object sender, EventArgs e)
        {
            await ConfirmAndRunAsync("PROCESS POSITION (90°)", MoveBothProcess90Async);
        }

        // Front/Rear 양측을 각자 Process 위치(0도)로 이동
        private async Task<int> MoveBothProcess0Async()
        {
            if (_visionUnit == null)
                return -1;

            return await _visionUnit.ManualMoveBothSideVisionProcess0Position();
        }

        // Front/Rear 양측을 각자 Process 위치(90도)로 이동
        private async Task<int> MoveBothProcess90Async()
        {
            if (_visionUnit == null)
                return -1;

            return await _visionUnit.ManualMoveBothSideVisionProcess90Position();
        }

        private void BindParameterGridMenus()
        {
            try
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add("Move To Position", null, async (s, e) =>
                {
                    VisionAxis axis;
                    string positionName;
                    if (TryGetSelectedTeachingPosition(out axis, out positionName))
                        await ConfirmAndRunAsync(optionParameterGrid.SelectedItem.Key, () => _visionUnit.MoveVisionAxisToTeachingPosition(axis, positionName));
                });
                menu.Items.Add("Teach Current Position", null, (s, e) =>
                {
                    VisionAxis axis;
                    string positionName;
                    if (!TryGetSelectedTeachingPosition(out axis, out positionName))
                        return;

                    _visionUnit.TeachVisionAxisPosition(axis, positionName);
                    SaveCurrentRecipeData();
                    RefreshView();
                });

                optionParameterGrid.ContextMenuStrip = menu;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "BindParameterGridMenus failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Vision Grid Menu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private bool TryGetSelectedTeachingPosition(out VisionAxis axis, out string positionName)
        {
            axis = VisionAxis.FrontSideVisionY;
            positionName = string.Empty;

            var item = optionParameterGrid.SelectedItem;
            if (item == null || item.IsGroupHeader)
                return false;

            string key = item.Key ?? string.Empty;
            bool isFront = key.StartsWith("FRONT Y ", StringComparison.OrdinalIgnoreCase);
            bool isRear = key.StartsWith("REAR Y ", StringComparison.OrdinalIgnoreCase);
            if (!isFront && !isRear)
                return false;

            axis = isFront ? VisionAxis.FrontSideVisionY : VisionAxis.RearSideVisionY;
            // 위치 종류는 그룹 키(K_AVOID / K_PROCESS0 / K_PROCESS90)로 식별한다.
            positionName = ResolvePositionName(item.GroupKey);
            return !string.IsNullOrEmpty(positionName);
        }

        private static string ResolvePositionName(string groupKey)
        {
            if (string.Equals(groupKey, "K_AVOID", StringComparison.OrdinalIgnoreCase)) return "AvoidPosition";
            if (string.Equals(groupKey, "K_PROCESS0", StringComparison.OrdinalIgnoreCase)) return "Process0Position";
            if (string.Equals(groupKey, "K_PROCESS90", StringComparison.OrdinalIgnoreCase)) return "Process90Position";
            return string.Empty;
        }

        private void BindIoPanel()
        {
            try
            {
                if (_visionUnit == null)
                    return;

                var unit = _visionUnit;
                ioCylinderPanel.SetItems(new[]
                {
                    // ===== 단독(묶이지 않은) 체크 센서 — 최상단 =====
                    IoCylinderItem.Input("WAFER STAGE TOUCH", () => IsOn(unit.WaferStageTouchSensor)),

                    // ===== SET: RETICLE LIFT (Up/Down 체크 센서 + Up/Down 출력 통합 실린더) =====
                    IoCylinderItem.Input("RETICLE UP", () => IsOn(unit.ReticleUpSensor)),
                    IoCylinderItem.Input("RETICLE DOWN", () => IsOn(unit.ReticleDownSensor)),
                    IoCylinderItem.Cylinder("RETICLE LIFT", unit.ReticleLift, "UP", "DOWN"),

                    // ===== SET: RETICLE FRONT SLIDE (Fw/Bw 체크 센서 + Fw/Bw 출력 통합 실린더) =====
                    IoCylinderItem.Input("RETICLE FRONT FW", () => IsOn(unit.ReticleFrontSideFwSensor)),
                    IoCylinderItem.Input("RETICLE FRONT BW", () => IsOn(unit.ReticleFrontSideBwSensor)),
                    IoCylinderItem.Cylinder("RETICLE FRONT SLIDE", unit.ReticleFrontSideSlide, "FW", "BW"),

                    // ===== SET: RETICLE REAR SLIDE (Fw/Bw 체크 센서 + Fw/Bw 출력 통합 실린더) =====
                    IoCylinderItem.Input("RETICLE REAR FW", () => IsOn(unit.ReticleRearSideFwSensor)),
                    IoCylinderItem.Input("RETICLE REAR BW", () => IsOn(unit.ReticleRearSideBwSensor)),
                    IoCylinderItem.Cylinder("RETICLE REAR SLIDE", unit.ReticleRearSideSlide, "FW", "BW"),

                    // ===== SET: NEEDLE VACUUM (진공 확인 센서 + 진공 출력) =====
                    IoCylinderItem.Input("NEEDLE VACUUM CHECK", () => IsOn(unit.NeedleVacuumSensor)),
                    IoCylinderItem.Output("NEEDLE VACUUM", () => IsOn(unit.NeedleVacuumOutput),
                        on => WriteOutAsync(unit.NeedleVacuumOutput, on), "ON", "OFF")
                });
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "BindIoPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Vision I/O", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private static bool IsOn(QMC.Common.IO.BaseDigitalInput input)
        {
            try { input?.UpdateStatus(); } catch { }
            return input != null && input.IsOn;
        }

        private static bool IsOn(QMC.Common.IO.BaseDigitalOutput output)
        {
            try { output?.UpdateStatus(); } catch { }
            return output != null && output.IsOn;
        }

        private static void WriteOut(QMC.Common.IO.BaseDigitalOutput output, bool on)
        {
            if (output == null) return;
            if (on) output.On(); else output.Off();
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
                if (_visionUnit == null)
                    return;

                var unit = _visionUnit;
                // 축 순서: Front Y, Rear Y
                var items = new List<JogAxisItem>
                {
                    BuildJogAxis("FRONT Y", unit.FrontSideVisionY, "Y+", "Y-", JogAxisControlKind.Vertical),
                    BuildJogAxis("REAR Y", unit.RearSideVisionY, "Y+", "Y-", JogAxisControlKind.Vertical)
                };

                jogPositionListControl.SetItems(items);
                jogAxisMoveControl.SetItems(items);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "UI", "VISION", "BindJogPanel failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, ex.Message, "Vision Jog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private JogAxisItem BuildJogAxis(string name, BaseAxis axis, string plus, string minus, JogAxisControlKind kind)
        {
            JogAxisItem item = JogAxisItem.Single(name, axis, AxisUnitConverter.DisplayUnitFor(axis), 1.0, plus, minus).WithControlKind(kind);
            item.StepMoveAsync = (it, direction, speedType, customSpeed, axisStepDistance) =>
                _visionUnit.JogStepAsync(axis, direction, speedType, customSpeed, axisStepDistance);
            item.ContinuousMoveAsync = (it, direction, speedType, customSpeed) =>
                _visionUnit.JogContinuousAsync(axis, direction, speedType, customSpeed);
            item.StopAsync = it => _visionUnit.StopJogAsync(axis);
            return item;
        }

        private async Task ConfirmAndRunAsync(string actionName, Func<Task<int>> action)
        {
            try
            {
                if (_visionUnit == null || action == null)
                    return;

                if (ManualMoveGuard.BlockIfNotReady(this, "Vision"))
                    return;

                DialogResult confirm = QMC.Common.MessageDialog.Show(this, actionName + " 진행하시겠습니까?", "Vision", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                Cursor = Cursors.WaitCursor;
                int result = await action();
                EventLogger.Write(EventKind.Event, "UI", "VISION", actionName + " result=" + result);
                if (result != 0)
                    QMC.Common.MessageDialog.Show(this, actionName + " 실패", "Vision", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                QMC.Common.MessageDialog.Show(this, ex.Message, "Vision Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                if (_visionUnit == null)
                    return;

                var unit = _visionUnit;
                optionParameterGrid.RefreshValues();
                waitParameterGrid.RefreshValues();
                ioCylinderPanel.RefreshStates();
                jogPositionListControl.RefreshState();

                lblVisionInfo.Text =
                    "VISION" + Environment.NewLine +
                    "FRONT Y : " + FormatAxis(unit.FrontSideVisionY.ActualPosition, unit.FrontSideVisionY) + Environment.NewLine +
                    "REAR Y  : " + FormatAxis(unit.RearSideVisionY.ActualPosition, unit.RearSideVisionY) + Environment.NewLine +
                    "F-AVOID : " + OnOff(unit.IsFrontSideVisionYInAvoidPosition()) + Environment.NewLine +
                    "R-AVOID : " + OnOff(unit.IsRearSideVisionYInAvoidPosition()) + Environment.NewLine +
                    "F-PROC  : " + OnOff(unit.IsFrontSideVisionYInProcessPosition()) + Environment.NewLine +
                    "R-PROC  : " + OnOff(unit.IsRearSideVisionYInProcessPosition());
            }
            catch
            {
            }
            finally
            {
            }
        }

        private ParameterGridItem AxisDouble(string displayName, ParameterGridScope scope, BaseAxis axis, Func<double> getter, Action<double> setter)
        {
            ParameterGridItem item = ParameterGridItem.Double(
                displayName,
                AxisUnitConverter.DisplayUnitFor(axis),
                scope,
                () => AxisUnitConverter.ToDisplay(getter(), axis),
                v => setter(AxisUnitConverter.FromDisplay(v, axis)));
            item.UnitGetter = () => AxisUnitConverter.DisplayUnitFor(axis);
            return item;
        }

        private static string FormatAxis(double value, BaseAxis axis)
        {
            try
            {
                return AxisUnitConverter.FormatDisplay(value, axis, "0.###", true);
            }
            catch
            {
                return "0 " + AxisUnitConverter.DisplayUnitFor(axis);
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
