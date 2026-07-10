using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT_320.Ui.Localization;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// Settings - Motion.
    /// The grid is built from Ajin mapping registrations and Motion AxisData.
    /// </summary>
    public partial class MotionPage : PageBase
    {
        private sealed class MotionAxisRow
        {
            public BaseAxis Axis { get; set; }
        }

        private readonly List<MotionAxisRow> _rows = new List<MotionAxisRow>();
        private Timer _refresh;
        private Dialogs.MotionTestDialog _motionTestDialog;

        public MotionPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyMotionLayout();
            InitializeConfigPanels();
            ConfigureCompactConfigLayout();
            InitializeSpeedTab();
            InitializeStatusPanels();

            Disposed += (s, e) =>
            {
                _refresh?.Stop();
                DetachAxes();
                try { if (_motionTestDialog != null && !_motionTestDialog.IsDisposed) _motionTestDialog.Dispose(); } catch { }
            };
        }

        private void ApplyMotionLayout()
        {
            configTabs.ItemSize = new Size(92, 28);
            configTabs.BackColor = Color.White;
            foreach (TabPage page in new[] { tabConfig, tabStatus, tabSpeed })
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = Color.White;
            }
            tabConfig.Padding = new Padding(1);
            tabStatus.Padding = new Padding(1);
            tabSpeed.Padding = new Padding(1);

            ConfigureCompactConfigLayout();
            ConfigureActionPanel();
            ConfigureSpeedButtons();
        }

        private void ConfigureCompactConfigLayout()
        {
            configLayout.SuspendLayout();
            try
            {
                configLayout.Controls.Clear();
                configLayout.AutoScroll = false;
                configLayout.BackColor = Color.White;
                configLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
                configLayout.ColumnCount = 3;
                configLayout.Margin = Padding.Empty;
                configLayout.Padding = Padding.Empty;

                configLayout.ColumnStyles.Clear();
                for (int i = 0; i < 3; i++)
                    configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3F));

                configLayout.RowCount = 3;
                configLayout.RowStyles.Clear();
                configLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
                configLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
                configLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));

                AddConfigGroup(grpConfig, 0, 0);
                AddConfigGroup(grpInposition, 1, 0);
                AddConfigGroup(grpLimit, 2, 0);
                AddConfigGroup(grpEmergency, 0, 1);
                AddConfigGroup(grpHome, 1, 1);
                AddConfigGroup(grpAlarm, 2, 1);
                AddConfigGroup(grpPositionClear, 0, 2);
                AddConfigFiller(1, 2, 2);

                var grids = new[]
                {
                    pgConfig, pgInposition, pgLimit, pgEmergency, pgHome, pgAlarm, pgPositionClear
                };

                foreach (var gridControl in grids)
                {
                    gridControl.Margin = Padding.Empty;
                    gridControl.Padding = new Padding(2);
                    gridControl.PairsPerRow = 2;
                    gridControl.RowHeight = 20;
                    gridControl.NameWidth = 98;
                }
            }
            finally
            {
                configLayout.ResumeLayout(false);
            }
        }

        private static void StyleConfigGroup(GroupBox group)
        {
            group.BackColor = Color.White;
            group.ForeColor = Color.Black;
            group.Padding = new Padding(1, 9, 1, 1);
        }

        private Panel AddConfigGroup(GroupBox group, int column, int row)
        {
            if (group.Parent != null)
            {
                group.Parent.Controls.Remove(group);
            }

            var border = new Panel
            {
                BackColor = Color.FromArgb(170, 170, 170),
                Dock = DockStyle.Fill,
                Margin = new Padding(1),
                Padding = new Padding(1),
                Name = group.Name + "Border"
            };

            StyleConfigGroup(group);
            group.Dock = DockStyle.Fill;
            group.Margin = Padding.Empty;
            border.Controls.Add(group);
            configLayout.Controls.Add(border, column, row);
            return border;
        }

        private void AddConfigFiller(int column, int row, int columnSpan)
        {
            var filler = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            configLayout.Controls.Add(filler, column, row);
            configLayout.SetColumnSpan(filler, columnSpan);
        }

        private void ConfigureActionPanel()
        {
            actionsPanel.Controls.Clear();
            actionsPanel.Margin = Padding.Empty;
            actionsPanel.Padding = new Padding(1);
            actionsPanel.Dock = DockStyle.Fill;
            actionsPanel.BackColor = Color.White;
            // 액션 버튼 폭을 다른 설정 페이지와 동일하게(14열 균등 = 각 7.14%) 맞춘다. 버튼 10개는 앞 10칸에 배치, 오른쪽 4칸은 빈칸.
            actionsPanel.ColumnCount = 14;
            actionsPanel.ColumnStyles.Clear();
            for (int i = 0; i < 14; i++)
                actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 14F));

            actionsPanel.RowCount = 1;
            actionsPanel.RowStyles.Clear();
            actionsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var buttons = new[]
            {
                btnServoOn, btnServoOff, btnHome,
                btnAllStop, btnAlarmClear, btnAllServoOff, btnParaLoad, btnParaSave, btnBoardScan, btnMotionTest
            };

            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(2);
                button.Font = new Font("맑은 고딕", 8F, FontStyle.Bold);
                button.MinimumSize = new Size(72, 28);
                button.BackColor = Color.FromArgb(128, 128, 128);
                actionsPanel.Controls.Add(button, i, 0);
            }

            btnAllStop.BackColor = Color.FromArgb(156, 66, 66);
        }

        private void ConfigureSpeedButtons()
        {
            speedLayout.Margin = Padding.Empty;
            speedLayout.Padding = Padding.Empty;
            speedLayout.RowStyles[1].SizeType = SizeType.Absolute;
            speedLayout.RowStyles[1].Height = 42F;

            speedButtons.Margin = Padding.Empty;
            speedButtons.Padding = new Padding(2);
            speedButtons.Dock = DockStyle.Fill;

            var buttons = new[] { btnSpeedReload, btnSpeedSave, btnSpeedScale };
            foreach (var button in buttons)
            {
                button.Margin = new Padding(3);
                button.Size = new Size(112, 32);
                button.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                button.BackColor = Color.FromArgb(128, 128, 128);
            }

            lblSpeedScaleCaption.Margin = new Padding(3);
            lblSpeedScaleCaption.Size = new Size(166, 32);
        }

        private Form1 Host => FindForm() as Form1;

        private void ApplyRuntimeUi()
        {
            lblPageHeader.Text = Lang.T("set.motion");
            lblPageHeader.Tag = "i18n:set.motion";
            // MODULE LIST/CONFIGURATION 라벨은 그룹박스 제목으로 대체됨(죽은 라벨 제거)

            actionsPanel.BackColor = UiTheme.OptionPanelBg;
            configTabs.SelectedTab = tabConfig;

            configLayout.AutoScroll = false;

            grid.AllowUserToResizeColumns = true;
            grid.AllowUserToResizeRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                col.Resizable = DataGridViewTriState.True;
            }
        }

        private static void SuppressHorizontalScroll(object sender, EventArgs e)
        {
            var ctl = sender as ScrollableControl;
            if (ctl == null) return;
            // 가로 스크롤바를 강제로 숨김. 세로 스크롤은 AutoScroll이 자동으로 관리한다.
            ctl.HorizontalScroll.Maximum = 0;
            ctl.HorizontalScroll.Visible = false;
            ctl.HorizontalScroll.Enabled = false;
            ctl.AutoScroll = true;
        }

        private void MotionPage_Load(object sender, EventArgs e)
        {
            LoadAxisRows();
            StartRefresh();
            RefreshConfigForSelected();
            LoadSpeedRows();
        }

        private async void btnHome_Click(object sender, EventArgs e)
        {
            await InitializeSelectedAxisAsync();
        }

        private void btnAllStop_Click(object sender, EventArgs e)
        {
            RunAllAxes(ax => ax.Stop());
        }

        private void btnAlarmClear_Click(object sender, EventArgs e)
        {
            ClearAllAxisAlarms();
        }

        private void btnAllServoOff_Click(object sender, EventArgs e)
        {
            RunAllAxes(ax => ax.ServoOff());
        }

        private void btnServoOn_Click(object sender, EventArgs e)
        {
            RunSelectedAxis(ax => ax.ServoOn());
        }

        private void btnServoOff_Click(object sender, EventArgs e)
        {
            RunSelectedAxis(ax => ax.ServoOff());
        }

        private void btnParaLoad_Click(object sender, EventArgs e)
        {
            DoLoadPara();
        }

        private void btnParaSave_Click(object sender, EventArgs e)
        {
            DoSavePara();
        }

        private void btnBoardScan_Click(object sender, EventArgs e)
        {
            ShowBoardScan();
        }

        private void btnMotionTest_Click(object sender, EventArgs e)
        {
            ShowOrRestoreMotionTestDialog();
        }

        private void LoadAxisRows()
        {
            try
            {
                DetachAxes();
                _rows.Clear();
                grid.Rows.Clear();

                List<BaseAxis> axes = AjinAxisRegistry.GetOrderedAxes(Host?.Machine)
                    .Where(x => x != null)
                    .OrderBy(x => x.Setup != null ? x.Setup.AxisNo : int.MaxValue)
                    .ThenBy(x => x.Name)
                    .ToList();

                int index = 0;
                foreach (BaseAxis axis in axes)
                {
                    AxisSetup setup = axis.Setup;
                    AxisMap map = FindAxisMap(axis.Name);

                    grid.Rows.Add(
                        ++index,
                        setup != null ? setup.UnitName : string.Empty,
                        AjinAxisDefaults.ToDisplayName(setup != null && !string.IsNullOrWhiteSpace(setup.DisplayName) ? setup.DisplayName : axis.Name),
                        setup != null ? setup.AxisNo.ToString() : string.Empty,
                        setup != null ? setup.BoardNo.ToString() : string.Empty,
                        map != null ? map.ChannelNo.ToString("X") : string.Empty,
                        StateText(axis),
                        axis.IsServoOn ? "ON" : "OFF",
                        FormatAxisValue(axis.CommandPosition, axis, "0.###"),
                        FormatAxisValue(axis.ActualPosition, axis, "0.###"),
                        FormatAxisValue(axis.CurrentVelocity, axis, "0.###"),
                        axis.IsInPosition ? "ON" : "OFF",
                        axis.IsInPosition ? "ON" : "OFF",
                        axis.IsHomeDone ? "ON" : "OFF",
                        axis.IsAlarm ? "ON" : "OFF",
                        axis.Sensor_PEL ? "ON" : "OFF",
                        axis.Sensor_MEL ? "ON" : "OFF",
                        axis.Sensor_ORG ? "ON" : "OFF");

                    _rows.Add(new MotionAxisRow { Axis = axis });
                    axis.ActualPositionChanged += OnAxisPos;
                    axis.MoveCompleted += OnAxisDone;
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Error,
                    "UI-MOTION",
                    "MotionPage",
                    "LoadAxisRows failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private static AxisMap FindAxisMap(string axisName)
        {
            try
            {
                AjinConfig cfg = AjinConfigStore.Current ?? AjinConfigStore.Load();
                if (cfg?.Axes == null) return null;
                AxisMap map;
                return cfg.Axes.TryGetValue(AjinAxisDefaults.ResolveName(axisName), out map) ? map : null;
            }
            catch
            {
                return null;
            }
            finally
            {
            }
        }

        private void DetachAxes()
        {
            foreach (MotionAxisRow row in _rows)
            {
                if (row.Axis == null) continue;
                row.Axis.ActualPositionChanged -= OnAxisPos;
                row.Axis.MoveCompleted -= OnAxisDone;
            }
        }

        private void StartRefresh()
        {
            if (_refresh != null)
                return;

            _refresh = new Timer { Interval = 250 };
            _refresh.Tick += (s, e) =>
            {
                if (!ShouldRefreshVisible(this))
                {
                    _refresh.Stop();
                    return;
                }

                RefreshAllRows();
            };
            if (ShouldRefreshVisible(this))
                _refresh.Start();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            try
            {
                if (_refresh == null)
                    return;

                if (ShouldRefreshVisible(this))
                    _refresh.Start();
                else
                    _refresh.Stop();
            }
            catch { }
        }

        private void RefreshAllRows()
        {
            if (grid.IsDisposed) return;

            for (int i = 0; i < _rows.Count && i < grid.Rows.Count; i++)
            {
                MotionAxisRow item = _rows[i];
                DataGridViewRow row = grid.Rows[i];

                if (item.Axis == null)
                {
                    row.Cells["STATUS"].Value = "NO AXIS";
                    row.DefaultCellStyle.ForeColor = Color.Gray;
                    continue;
                }

                AxisStatusSnapshot snapshot = Host?.MotionMonitor?.GetLatest(item.Axis);
                if (snapshot == null)
                {
                    ApplyAxisToGrid(row, item.Axis);
                    continue;
                }

                ApplySnapshotToGrid(row, snapshot);
            }

            RefreshConfigDynamic();
            RefreshStatusDynamic();
        }

        private static void ApplyAxisToGrid(DataGridViewRow row, BaseAxis axis)
        {
            row.Cells["NO"].Value = axis.Setup.AxisNo.ToString();
            row.Cells["STATUS"].Value = StateText(axis);
            row.Cells["SERVO"].Value = axis.IsServoOn ? "ON" : "OFF";
            row.Cells["COMMAND_POSITION"].Value = FormatAxisValue(axis.CommandPosition, axis, "0.###");
            row.Cells["ACTUAL_POSITION"].Value = FormatAxisValue(axis.ActualPosition, axis, "0.###");
            row.Cells["VELOCITY"].Value = FormatAxisValue(axis.CurrentVelocity, axis, "0.###");
            row.Cells["DONE"].Value = axis.IsInPosition ? "ON" : "OFF";
            row.Cells["INP_DONE"].Value = axis.IsInPosition ? "ON" : "OFF";
            row.Cells["HOME_END"].Value = axis.IsHomeDone ? "ON" : "OFF";
            row.Cells["ALARM"].Value = axis.IsAlarm ? "ON" : "OFF";
            row.Cells["PEL"].Value = axis.Sensor_PEL ? "ON" : "OFF";
            row.Cells["MEL"].Value = axis.Sensor_MEL ? "ON" : "OFF";
            row.Cells["ORG"].Value = axis.Sensor_ORG ? "ON" : "OFF";
            row.DefaultCellStyle.ForeColor = axis.IsAlarm ? Color.IndianRed : axis.IsMoving ? Color.SteelBlue : Color.Black;
        }

        private static void ApplySnapshotToGrid(DataGridViewRow row, AxisStatusSnapshot snapshot)
        {
            row.Cells["NO"].Value = snapshot.AxisNo.ToString();
            row.Cells["STATUS"].Value = StateText(snapshot);
            row.Cells["SERVO"].Value = snapshot.IsServoOn ? "ON" : "OFF";
            row.Cells["COMMAND_POSITION"].Value = FormatAxisValue(snapshot.CommandPosition, snapshot.Unit, "0.###");
            row.Cells["ACTUAL_POSITION"].Value = FormatAxisValue(snapshot.ActualPosition, snapshot.Unit, "0.###");
            row.Cells["VELOCITY"].Value = FormatAxisValue(snapshot.CurrentVelocity, snapshot.Unit, "0.###");
            row.Cells["DONE"].Value = snapshot.IsInPosition ? "ON" : "OFF";
            row.Cells["INP_DONE"].Value = snapshot.IsInPosition ? "ON" : "OFF";
            row.Cells["HOME_END"].Value = snapshot.IsHomeDone ? "ON" : "OFF";
            row.Cells["ALARM"].Value = snapshot.IsAlarm ? "ON" : "OFF";
            row.Cells["PEL"].Value = snapshot.SensorPel ? "ON" : "OFF";
            row.Cells["MEL"].Value = snapshot.SensorMel ? "ON" : "OFF";
            row.Cells["ORG"].Value = snapshot.SensorOrg ? "ON" : "OFF";
            row.DefaultCellStyle.ForeColor = snapshot.IsAlarm ? Color.IndianRed : snapshot.IsMoving ? Color.SteelBlue : Color.Black;
        }

        private void RunSelectedAxis(Action<BaseAxis> operation)
        {
            BaseAxis axis = SelectedAxis();
            if (axis == null) return;
            operation(axis);
        }

        private static string FormatAxisValue(double nativeValue, BaseAxis axis, string format)
        {
            try
            {
                return FormatAxisValue(nativeValue, AxisUnitConverter.DisplayUnitFor(axis), format);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MotionPageFormatAxisValue",
                    "Axis display unit format failed: " + ex.Message + " - Failed");
                return nativeValue.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            }
            finally
            {
            }
        }

        private static string FormatAxisValue(double nativeValue, string displayUnit, string format)
        {
            try
            {
                double displayValue = AxisUnitConverter.ToDisplay(nativeValue, displayUnit);
                return displayValue.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MotionPageFormatAxisValue",
                    "Axis display value format failed: " + ex.Message + " - Failed");
                return nativeValue.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            }
            finally
            {
            }
        }

        private async System.Threading.Tasks.Task RunSelectedAxisAsync(Func<BaseAxis, System.Threading.Tasks.Task> operation)
        {
            BaseAxis axis = SelectedAxis();
            if (axis == null) return;
            await operation(axis);
        }

        private async System.Threading.Tasks.Task InitializeSelectedAxisAsync()
        {
            try
            {
                BaseAxis axis = SelectedAxis();
                if (axis == null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "MotionInitializeAxis",
                        "Axis initialize failed: selected axis is null. - Failed");
                    QMC.Common.MessageDialog.Show(this, "선택된 축이 없습니다.", "Motion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ConfirmMotionAction(AjinAxisDefaults.ToDisplayName(axis.Name) + " 축 초기화를 진행하시겠습니까?"))
                    return;

                if (Host == null || Host.Controller == null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "MotionInitializeAxis",
                        "Axis initialize failed: controller is null. - Failed");
                    QMC.Common.MessageDialog.Show(this, "Machine Controller를 찾을 수 없습니다.", "Motion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int result = await Host.Controller.InitializeAxisAsync(axis.Name);
                ShowMotionActionResult(result, "MotionInitializeAxis");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MotionInitializeAxis",
                    "Axis initialize failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "축 초기화 실패:\r\n" + ex.Message, "Motion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshAllRows();
            }
        }

        private async System.Threading.Tasks.Task InitializeSelectedAxisGroupAsync()
        {
            try
            {
                BaseAxis axis = SelectedAxis();
                if (axis == null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "MotionInitializeGroup",
                        "Axis group initialize failed: selected axis is null. - Failed");
                    QMC.Common.MessageDialog.Show(this, "선택된 축이 없습니다.", "Motion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string groupName = axis.Setup != null ? axis.Setup.UnitName : "";
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "MotionInitializeGroup",
                        "Axis group initialize failed: group name is empty. axis=" + axis.Name + " - Failed");
                    QMC.Common.MessageDialog.Show(this, "선택 축의 그룹 정보가 없습니다.", "Motion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ConfirmMotionAction(groupName + " 그룹 초기화를 진행하시겠습니까?"))
                    return;

                if (Host == null || Host.Controller == null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "MotionInitializeGroup",
                        "Axis group initialize failed: controller is null. group=" + groupName + " - Failed");
                    QMC.Common.MessageDialog.Show(this, "Machine Controller를 찾을 수 없습니다.", "Motion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int result = await Host.Controller.InitializeAxisGroupAsync(groupName);
                ShowMotionActionResult(result, "MotionInitializeGroup");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MotionInitializeGroup",
                    "Axis group initialize failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "축 그룹 초기화 실패:\r\n" + ex.Message, "Motion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshAllRows();
            }
        }

        private async System.Threading.Tasks.Task InitializeAllAxesAsync()
        {
            try
            {
                if (Host == null || Host.Controller == null)
                {
                    QMC.Common.Log.Write("Main", "SYSTEM", "MotionInitializeAll",
                        "All axes initialize failed: controller is null. - Failed");
                    QMC.Common.MessageDialog.Show(this, "Machine Controller를 찾을 수 없습니다.", "Motion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ConfirmMotionAction("전체 축 초기화를 진행하시겠습니까?"))
                    return;

                int result = await Host.Controller.InitializeAllAxesAsync(true);
                ShowMotionActionResult(result, "MotionInitializeAll");
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "MotionInitializeAll",
                    "All axes initialize failed: " + ex.Message + " - Failed");
                QMC.Common.MessageDialog.Show(this, "전체 축 초기화 실패:\r\n" + ex.Message, "Motion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                RefreshAllRows();
            }
        }

        private bool ConfirmMotionAction(string message)
        {
            try
            {
                return QMC.Common.MessageDialog.Show(this, message, "Motion", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "ConfirmMotionAction",
                    "Motion action confirm failed: " + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private void ShowMotionActionResult(int result, string source)
        {
            try
            {
                if (result == 0)
                    return;

                string message = Host != null && Host.Controller != null && !string.IsNullOrWhiteSpace(Host.Controller.LastActionFailureMessage)
                    ? Host.Controller.LastActionFailureMessage
                    : null;

                if (string.IsNullOrWhiteSpace(message))
                {
                    BaseAxis axis = SelectedAxis();
                    if (axis != null && !string.IsNullOrWhiteSpace(axis.LastMotionFailureMessage))
                        message = axis.LastMotionFailureMessage;
                }

                if (string.IsNullOrWhiteSpace(message))
                    message = "Motion 작업이 실패했습니다. result=" + result + ". Alarm/Event Log를 확인하세요.";

                QMC.Common.Log.Write("Main", "SYSTEM", source,
                    "Motion action failed: return=" + result + ", message=" + message + " - Failed");
                QMC.Common.MessageDialog.Show(this, message, "Motion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", source,
                    "Motion action result handling failed: " + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private void RunAllAxes(Action<BaseAxis> operation)
        {
            foreach (BaseAxis axis in _rows.Select(x => x.Axis).Where(x => x != null))
                operation(axis);
        }

        private void ClearAllAxisAlarms()
        {
            try
            {
                RunAllAxes(ax => ax.ResetAlarm());
                AlarmManager.ClearAll();
            }
            catch (Exception ex)
            {
                QMC.Common.Alarms.AlarmManager.Raise(
                    QMC.Common.Alarms.AlarmSeverity.Error,
                    "AX-ALARM-CLEAR",
                    "MotionPage",
                    ex.Message);
            }
            finally
            {
            }
        }

        private BaseAxis SelectedAxis()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Index < 0 || grid.CurrentRow.Index >= _rows.Count)
                return null;
            return _rows[grid.CurrentRow.Index].Axis;
        }

        private bool SaveMotionAxisSettings()
        {
            bool ok = true;

            try
            {
                QMC.CDT320.Ajin.AjinFactory.AxisManager.Save(MotionAxisStore.DefaultPath);
            }
            catch (Exception ex)
            {
                ok = false;
                QMC.Common.Log.Write("Main", "SYSTEM", "MotionPageSave",
                    "Motion axis store save failed: " + ex.Message + " - Failed");
            }

            try
            {
                if (Host != null && Host.Machine != null)
                    ok &= Host.Machine.SaveSettings();
            }
            catch (Exception ex)
            {
                ok = false;
                QMC.Common.Log.Write("Main", "SYSTEM", "MotionPageSave",
                    "Machine unit settings save failed: " + ex.Message + " - Failed");
            }

            return ok;
        }

        private void ShowBoardScan()
        {
            using (var dlg = new Dialogs.BoardScanDialog(_rows.Select(x => x.Axis).Where(x => x != null).ToList()))
                dlg.ShowDialog(FindForm());
        }

        private void ShowOrRestoreMotionTestDialog()
        {
            try
            {
                if (_motionTestDialog == null || _motionTestDialog.IsDisposed)
                {
                    _motionTestDialog = new Dialogs.MotionTestDialog(CurrentMotionTestAxes())
                    {
                        StartPosition = FormStartPosition.CenterParent,
                        ShowInTaskbar = false
                    };
                    _motionTestDialog.FormClosed += (s, e) => { _motionTestDialog = null; };
                    _motionTestDialog.FormClosing += (s, e) =>
                    {
                        if (e.CloseReason == CloseReason.UserClosing)
                        {
                            e.Cancel = true;
                            _motionTestDialog.Hide();
                        }
                    };
                }

                Form owner = FindForm();
                if (!_motionTestDialog.Visible)
                {
                    if (owner != null)
                        _motionTestDialog.Show(owner);
                    else
                        _motionTestDialog.Show();
                }

                if (_motionTestDialog.WindowState == FormWindowState.Minimized)
                    _motionTestDialog.WindowState = FormWindowState.Normal;

                _motionTestDialog.BringToFront();
                _motionTestDialog.Activate();
            }
            catch (Exception ex)
            {
                QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "UI", "MOTION-TEST", "Open motion test failed: " + ex.Message);
                QMC.Common.MessageDialog.Show(this, "Motion Test 창을 열 수 없습니다:\r\n" + ex.Message, "Motion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
            }
        }

        private IEnumerable<BaseAxis> CurrentMotionTestAxes()
        {
            List<BaseAxis> axes = _rows.Select(x => x.Axis).Where(x => x != null).ToList();
            if (axes.Count > 0)
                return axes;

            return AjinAxisRegistry.GetOrderedAxes(Host?.Machine).Where(x => x != null).ToList();
        }

        private void DoLoadPara()
        {
            if (!AjinSystem.IsOpen)
            {
                QMC.Common.MessageDialog.Show("AXL library is not open. Enable UseAjin in Settings > GENERAL and restart.");
                return;
            }

            using (var dlg = new OpenFileDialog { Filter = "Motion parameters (*.mot)|*.mot|All files (*.*)|*.*" })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                int r = QMC.Common.Motion.Ajin.AXM.LoadParameters(dlg.FileName);
                if (r == 0)
                {
                    ApplyParametersFromBoard();
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "QMC", "PARA-LOAD", dlg.FileName);
                    QMC.Common.MessageDialog.Show("Parameter load complete.");
                }
                else
                {
                    QMC.Common.MessageDialog.Show("Parameter load failed. 0x" + r.ToString("X4"));
                }
            }
        }

        private void DoSavePara()
        {
            if (!AjinSystem.IsOpen)
            {
                QMC.Common.MessageDialog.Show("AXL library is not open.");
                return;
            }

            if (IsMotFileSaveDisabledDuringSetup())
            {
                QMC.Common.MessageDialog.Show("현재 잔비 Setup중에는 mot 파일 저장 불가.");
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = "Motion parameters (*.mot)|*.mot", FileName = "axl_para.mot" })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                int r = QMC.Common.Motion.Ajin.AXM.SaveParameters(dlg.FileName);
                if (r == 0)
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "QMC", "PARA-SAVE", dlg.FileName);
                    QMC.Common.MessageDialog.Show("Parameter save complete.");
                }
                else
                {
                    QMC.Common.MessageDialog.Show("Parameter save failed. 0x" + r.ToString("X4"));
                }
            }
        }

        private static bool IsMotFileSaveDisabledDuringSetup()
        {
            return true;
        }

        private static string StateText(BaseAxis ax)
        {
            if (ax.IsAlarm) return "ALARM";
            if (ax.IsMoving) return "MOVING";
            if (!ax.IsServoOn) return "SV-OFF";
            if (ax.IsHomeDone) return "READY";
            return "NONE";
        }

        private static string StateText(AxisStatusSnapshot snapshot)
        {
            if (snapshot.IsAlarm) return "ALARM";
            if (snapshot.IsMoving) return "MOVING";
            if (!snapshot.IsServoOn) return "SV-OFF";
            if (snapshot.IsHomeDone) return "READY";
            return "NONE";
        }

        private void OnAxisPos(BaseAxis ax, double pos) { }
        private void OnAxisDone(BaseAxis ax) { }

        private static IEnumerable<BaseAxis> EnumerateAxes(CDT320_Machine m)
        {
            foreach (var u in m.Units)
                foreach (var a in Rec(u))
                    yield return a;
        }

        private static IEnumerable<BaseAxis> Rec(BaseEquipmentNode node)
        {
            if (node is BaseAxis ax)
            {
                yield return ax;
                yield break;
            }

            var prop = node.GetType().GetProperty("Components");
            if (prop != null && prop.GetValue(node) is System.Collections.IEnumerable comps)
                foreach (BaseEquipmentNode c in comps)
                    foreach (var a in Rec(c))
                        yield return a;
        }
    }
}


