using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Ajin;
using QMC.CDT_320.Ui.Localization;
using QMC.Common;
using QMC.Common.Motion;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// Settings - Motion.
    /// The grid is built from AxisSetupPage data first, then live machine axes are matched by axis name.
    /// </summary>
    public partial class MotionPage : PageBase
    {
        private sealed class MotionAxisRow
        {
            public AxisSetupPage.AxisRow Setup { get; set; }
            public BaseAxis Axis { get; set; }
        }

        private readonly List<MotionAxisRow> _rows = new List<MotionAxisRow>();
        private Timer _refresh;

        public MotionPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            WireActions();
            InitializeConfigPanels();
            InitializeSpeedTab();
            InitializeStatusPanels();

            Load += (s, e) =>
            {
                LoadAxisRows();
                StartRefresh();
                RefreshConfigForSelected();
                LoadSpeedRows();
            };

            Disposed += (s, e) =>
            {
                _refresh?.Stop();
                DetachAxes();
            };
        }

        private Form1 Host => FindForm() as Form1;

        private void ApplyRuntimeUi()
        {
            lblPageHeader.Text = Lang.T("set.motion");
            lblPageHeader.Tag = "i18n:set.motion";
            lblPageHeader.BackColor = UiTheme.StatusBarBg;
            lblPageHeader.ForeColor = UiTheme.StatusBarFg;
            lblPageHeader.Font = UiTheme.SectionFont;

            lblModuleHeader.BackColor = UiTheme.StatusBarBg;
            lblModuleHeader.ForeColor = Color.White;
            lblModuleHeader.Font = UiTheme.SectionFont;

            lblConfigHeader.BackColor = UiTheme.StatusBarBg;
            lblConfigHeader.ForeColor = Color.White;
            lblConfigHeader.Font = UiTheme.SectionFont;

            actionsPanel.BackColor = UiTheme.OptionPanelBg;
            configTabs.SelectedTab = tabConfig;

            grid.AllowUserToResizeColumns = true;
            grid.AllowUserToResizeRows = false;
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                col.Resizable = DataGridViewTriState.True;
            }
        }

        private void WireActions()
        {
            btnEnable.Click += (s, e) => RunSelectedAxis(ax => ax.ServoOn());
            btnDisable.Click += (s, e) => RunSelectedAxis(ax => ax.ServoOff());
            btnHome.Click += async (s, e) => await RunSelectedAxisAsync(ax => ax.HomeSearchAsync());
            btnAllStop.Click += (s, e) => RunAllAxes(ax => ax.Stop());
            btnAlarmClear.Click += (s, e) => RunAllAxes(ax => ax.ResetAlarm());
            btnAllServoOff.Click += (s, e) => RunAllAxes(ax => ax.ServoOff());
            btnServoOn.Click += (s, e) => RunSelectedAxis(ax => ax.ServoOn());
            btnServoOff.Click += (s, e) => RunSelectedAxis(ax => ax.ServoOff());
            btnParaLoad.Click += (s, e) => DoLoadPara();
            btnParaSave.Click += (s, e) => DoSavePara();
            btnBoardScan.Click += (s, e) => ShowBoardScan();
        }

        private void LoadAxisRows()
        {
            DetachAxes();
            _rows.Clear();
            grid.Rows.Clear();

            Dictionary<string, BaseAxis> actualAxes = BuildActualAxisMap();
            List<AxisSetupPage.AxisRow> setupRows = AxisSetupPage.LoadConfiguredRows()
                .OrderBy(x => x.No)
                .ThenBy(x => x.Name)
                .ToList();

            int index = 0;
            foreach (AxisSetupPage.AxisRow setup in setupRows)
            {
                string axisName = AjinAxisDefaults.ResolveName(setup.ConfigKey);
                if (string.IsNullOrEmpty(axisName))
                    axisName = AjinAxisDefaults.ResolveName(setup.Name);

                BaseAxis axis = null;
                if (!string.IsNullOrEmpty(axisName))
                    actualAxes.TryGetValue(axisName, out axis);

                _rows.Add(new MotionAxisRow { Setup = setup, Axis = axis });

                grid.Rows.Add(
                    ++index,
                    setup.Module,
                    setup.Name,
                    setup.No.ToString(),
                    setup.BoardNo.ToString(),
                    setup.ChannelNo.ToString("X"),
                    axis != null ? StateText(axis) : "NO AXIS",
                    axis != null && axis.IsServoOn ? "ON" : "OFF",
                    axis != null ? axis.CommandPosition.ToString("F1") : "0.0",
                    axis != null ? axis.ActualPosition.ToString("F1") : "0.0",
                    axis != null ? axis.CurrentVelocity.ToString("F1") : "0.0",
                    axis != null && axis.IsInPosition ? "ON" : "OFF",
                    axis != null && axis.IsInPosition ? "ON" : "OFF",
                    axis != null && axis.IsHomeDone ? "ON" : "OFF",
                    axis != null && axis.IsAlarm ? "ON" : "OFF",
                    axis != null && axis.Sensor_PEL ? "ON" : "OFF",
                    axis != null && axis.Sensor_MEL ? "ON" : "OFF",
                    axis != null && axis.Sensor_ORG ? "ON" : "OFF");

                if (axis != null)
                {
                    axis.ActualPositionChanged += OnAxisPos;
                    axis.MoveCompleted += OnAxisDone;
                }
            }
        }

        private Dictionary<string, BaseAxis> BuildActualAxisMap()
        {
            return AjinAxisRegistry.GetAxisMapByName();
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
            _refresh.Tick += (s, e) => RefreshAllRows();
            _refresh.Start();
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
        }

        private static void ApplyAxisToGrid(DataGridViewRow row, BaseAxis axis)
        {
            row.Cells["NO"].Value = axis.Setup.AxisNo.ToString();
            row.Cells["STATUS"].Value = StateText(axis);
            row.Cells["SERVO"].Value = axis.IsServoOn ? "ON" : "OFF";
            row.Cells["COMMAND_POSITION"].Value = axis.CommandPosition.ToString("F1");
            row.Cells["ACTUAL_POSITION"].Value = axis.ActualPosition.ToString("F1");
            row.Cells["VELOCITY"].Value = axis.CurrentVelocity.ToString("F1");
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
            row.Cells["COMMAND_POSITION"].Value = snapshot.CommandPosition.ToString("F1");
            row.Cells["ACTUAL_POSITION"].Value = snapshot.ActualPosition.ToString("F1");
            row.Cells["VELOCITY"].Value = snapshot.CurrentVelocity.ToString("F1");
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

        private async System.Threading.Tasks.Task RunSelectedAxisAsync(Func<BaseAxis, System.Threading.Tasks.Task> operation)
        {
            BaseAxis axis = SelectedAxis();
            if (axis == null) return;
            await operation(axis);
        }

        private void RunAllAxes(Action<BaseAxis> operation)
        {
            foreach (BaseAxis axis in _rows.Select(x => x.Axis).Where(x => x != null))
                operation(axis);
        }

        private BaseAxis SelectedAxis()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Index < 0 || grid.CurrentRow.Index >= _rows.Count)
                return null;
            return _rows[grid.CurrentRow.Index].Axis;
        }

        private void ShowBoardScan()
        {
            using (var dlg = new Dialogs.BoardScanDialog(_rows.Select(x => x.Axis).Where(x => x != null).ToList()))
                dlg.ShowDialog(FindForm());
        }

        private void DoLoadPara()
        {
            if (!AjinSystem.IsOpen)
            {
                MessageBox.Show("AXL library is not open. Enable UseAjin in Settings > GENERAL and restart.");
                return;
            }

            using (var dlg = new OpenFileDialog { Filter = "Motion parameters (*.mot)|*.mot|All files (*.*)|*.*" })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                int r = QMC.Common.Motion.Ajin.AXM.LoadParameters(dlg.FileName);
                if (r == 0)
                {
                    ApplyParametersFromBoard();
                    QMC.CDT320.Logging.EventLogger.Write(QMC.CDT320.Logging.EventKind.Event, "QMC", "PARA-LOAD", dlg.FileName);
                    MessageBox.Show("Parameter load complete.");
                }
                else
                {
                    MessageBox.Show("Parameter load failed. 0x" + r.ToString("X4"));
                }
            }
        }

        private void DoSavePara()
        {
            if (!AjinSystem.IsOpen)
            {
                MessageBox.Show("AXL library is not open.");
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = "Motion parameters (*.mot)|*.mot", FileName = "axl_para.mot" })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                int r = QMC.Common.Motion.Ajin.AXM.SaveParameters(dlg.FileName);
                if (r == 0)
                {
                    QMC.CDT320.Logging.EventLogger.Write(QMC.CDT320.Logging.EventKind.Event, "QMC", "PARA-SAVE", dlg.FileName);
                    MessageBox.Show("Parameter save complete.");
                }
                else
                {
                    MessageBox.Show("Parameter save failed. 0x" + r.ToString("X4"));
                }
            }
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
