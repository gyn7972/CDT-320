using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common;
using QMC.Common.Motion;
using QMC.CDT320;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// ?�정 - MOTION.
    /// ?�단: �??�체 목록(DataGridView) ???�스??Machine ?�리?�서 ?�제 축을 ?�어 ?�시�?갱신.
    /// ?�단: CONFIGURATION (STATUS/CONFIG/SPEED ??.
    /// 최하?? ENABLE/DISABLE/HOME/ALL STOP/... 공통 ?�션.
    /// </summary>
    public class MotionPage : PageBase
    {
        private DataGridView _grid;
        private readonly List<BaseAxis> _axes = new List<BaseAxis>();
        private Timer _refresh;

        public MotionPage()
        {
            Controls.Add(CreateSectionHeader("set.motion"));
            BuildTop();
            BuildConfig();
            BuildActions();

            Load += (s, e) =>
            {
                AttachAxes();
                StartRefresh();
            };
            Disposed += (s, e) =>
            {
                _refresh?.Stop();
                DetachAxes();
            };
        }

        private Form1 Host => FindForm() as Form1;

        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        //  ?�단 모듈 리스??        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        private void BuildTop()
        {
            Controls.Add(new Label
            {
                Location = new Point(8, 36), Size = new Size(1400, 26),
                Text = "MODUEL LIST",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });

            _grid = new DataGridView
            {
                Location                = new Point(8, 66),
                Size                    = new Size(1400, 300),
                ReadOnly                = true,
                AllowUserToAddRows      = false,
                AllowUserToDeleteRows   = false,
                RowHeadersVisible       = false,
                MultiSelect             = false,
                SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor         = Color.White,
                Font                    = new Font("맑�? 고딕", 9F),
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50, 0x50, 0x50),
                    ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font      = new Font("맑�? 고딕", 9F, FontStyle.Bold)
                },
                RowTemplate = { Height = 26 }
            };
            _grid.Columns.Add("INDEX",            "INDEX");
            _grid.Columns.Add("KEY",              "KEY");
            _grid.Columns.Add("NO",               "NO.");
            _grid.Columns.Add("STATUS",           "STATUS");
            _grid.Columns.Add("SERVO",            "SERVO");
            _grid.Columns.Add("COMMAND_POSITION", "COMMAND POSITION");
            _grid.Columns.Add("ACTUAL_POSITION",  "ACTUAL POSITION");
            _grid.Columns.Add("VELOCITY",         "VELOCITY");
            _grid.Columns.Add("DONE",             "DONE");
            _grid.Columns.Add("INP_DONE",         "INP DONE");
            _grid.Columns.Add("HOME_END",         "HOME END");
            _grid.Columns.Add("ALARM",            "ALARM");
            _grid.Columns.Add("PEL",              "PEL");
            _grid.Columns.Add("MEL",              "MEL");
            _grid.Columns.Add("ORG",              "ORG");
            Controls.Add(_grid);
        }

        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        //  �??�집 + ?�시�?갱신
        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        private void AttachAxes()
        {
            _axes.Clear();
            _grid.Rows.Clear();
            if (Host?.Machine == null) { SeedFallback(); return; }

            int idx = 0;
            foreach (var ax in EnumerateAxes(Host.Machine))
            {
                _axes.Add(ax);
                _grid.Rows.Add(
                    ++idx, ax.Name, ax.Setup.AxisNo.ToString(),
                    StateText(ax),
                    ax.IsServoOn ? "ON" : "OFF",
                    ax.CommandPosition.ToString("F1"),
                    ax.ActualPosition.ToString("F1"),
                    ax.CurrentVelocity.ToString("F1"),
                    ax.IsInPosition ? "ON" : "OFF",
                    ax.IsInPosition ? "ON" : "OFF",
                    ax.IsHomeDone   ? "ON" : "OFF",
                    ax.IsAlarm      ? "ON" : "OFF",
                    ax.Sensor_PEL   ? "ON" : "OFF",
                    ax.Sensor_MEL   ? "ON" : "OFF",
                    ax.Sensor_ORG   ? "ON" : "OFF");

                ax.ActualPositionChanged += OnAxisPos;
                ax.MoveCompleted         += OnAxisDone;
            }
        }

        private void DetachAxes()
        {
            foreach (var ax in _axes)
            {
                ax.ActualPositionChanged -= OnAxisPos;
                ax.MoveCompleted         -= OnAxisDone;
            }
            _axes.Clear();
        }

        private void SeedFallback()
        {
            string[,] axes =
            {
                { "WaferLifterZ", "0" }, { "WaferFeederY", "1" }, { "WaferStageY", "2" },
                { "WaferStageT", "3" }, { "WaferExpandingZ", "4" }, { "WaferVisionX", "5" },
                { "NeedleX", "6" }, { "NeedleZ", "7" }, { "FrontPickerX", "9" }, { "RearPickerX", "21" }
            };
            for (int i = 0; i < axes.GetLength(0); i++)
                _grid.Rows.Add(i + 1, axes[i, 0], axes[i, 1], "NONE", "OFF", "0", "0", "0", "OFF", "OFF", "OFF", "OFF", "OFF", "OFF", "OFF");
        }

        private void StartRefresh()
        {
            _refresh = new Timer { Interval = 250 };
            _refresh.Tick += (s, e) => RefreshAllRows();
            _refresh.Start();
        }

        private void RefreshAllRows()
        {
            if (_grid.IsDisposed) return;
            for (int i = 0; i < _axes.Count && i < _grid.Rows.Count; i++)
            {
                var ax = _axes[i];
                var row = _grid.Rows[i];
                var snapshot = Host?.MotionMonitor?.GetLatest(ax);
                if (snapshot == null)
                {
                    row.Cells[3].Value = StateText(ax);
                    row.Cells[4].Value = ax.IsServoOn ? "ON" : "OFF";
                    row.Cells[5].Value = ax.CommandPosition.ToString("F1");
                    row.Cells[6].Value = ax.ActualPosition.ToString("F1");
                    row.Cells[7].Value = ax.CurrentVelocity.ToString("F1");
                    row.Cells[8].Value = ax.IsInPosition ? "ON" : "OFF";
                    row.Cells[9].Value = ax.IsInPosition ? "ON" : "OFF";
                    row.Cells[10].Value = ax.IsHomeDone ? "ON" : "OFF";
                    row.Cells[11].Value = ax.IsAlarm ? "ON" : "OFF";
                    row.Cells[12].Value = ax.Sensor_PEL ? "ON" : "OFF";
                    row.Cells[13].Value = ax.Sensor_MEL ? "ON" : "OFF";
                    row.Cells[14].Value = ax.Sensor_ORG ? "ON" : "OFF";
                    row.DefaultCellStyle.ForeColor = ax.IsAlarm ? Color.IndianRed
                        : (ax.IsMoving ? Color.SteelBlue : Color.Black);
                    continue;
                }

                row.Cells[2].Value = snapshot.AxisNo.ToString();
                row.Cells[3].Value = StateText(snapshot);
                row.Cells[4].Value = snapshot.IsServoOn ? "ON" : "OFF";
                row.Cells[5].Value = snapshot.CommandPosition.ToString("F1");
                row.Cells[6].Value = snapshot.ActualPosition.ToString("F1");
                row.Cells[7].Value = snapshot.CurrentVelocity.ToString("F1");
                row.Cells[8].Value = snapshot.IsInPosition ? "ON" : "OFF";
                row.Cells[9].Value = snapshot.IsInPosition ? "ON" : "OFF";
                row.Cells[10].Value = snapshot.IsHomeDone ? "ON" : "OFF";
                row.Cells[11].Value = snapshot.IsAlarm ? "ON" : "OFF";
                row.Cells[12].Value = snapshot.SensorPel ? "ON" : "OFF";
                row.Cells[13].Value = snapshot.SensorMel ? "ON" : "OFF";
                row.Cells[14].Value = snapshot.SensorOrg ? "ON" : "OFF";
                row.DefaultCellStyle.ForeColor = snapshot.IsAlarm ? Color.IndianRed
                    : (snapshot.IsMoving ? Color.SteelBlue : Color.Black);
            }
        }

        private void OnAxisPos(BaseAxis ax, double pos) { /* Timer 가 처리 */ }
        private void OnAxisDone(BaseAxis ax)            { /* Timer 가 처리 */ }

        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        //  AXL ?�라미터 ?�일 LOAD/SAVE
        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�

        private void DoLoadPara()
        {
            if (!QMC.CDT320.Ajin.AjinSystem.IsOpen)
            {
                MessageBox.Show("AXL library is not open. Enable UseAjin in Settings ??GENERAL and restart.");
                return;
            }
            using (var dlg = new OpenFileDialog { Filter = "Motion parameters (*.mot)|*.mot|All files (*.*)|*.*" })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                int r = QMC.Common.Motion.Ajin.AXM.LoadParameters(dlg.FileName);
                if (r == 0)
                {
                    QMC.CDT320.Logging.EventLogger.Write(QMC.CDT320.Logging.EventKind.Event, "QMC", "PARA-LOAD", dlg.FileName);
                    MessageBox.Show("?�라미터 로드 ?�료.");
                }
                else
                {
                    MessageBox.Show("?�라미터 로드 ?�패. 0x" + r.ToString("X4"));
                }
            }
        }

        private void DoSavePara()
        {
            if (!QMC.CDT320.Ajin.AjinSystem.IsOpen)
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
                    MessageBox.Show("?�라미터 ?�???�료.");
                }
                else
                {
                    MessageBox.Show("?�라미터 ?�???�패. 0x" + r.ToString("X4"));
                }
            }
        }

        private static string StateText(BaseAxis ax)
        {
            if (ax.IsAlarm)    return "ALARM";
            if (ax.IsMoving)   return "MOVING";
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

        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        //  CONFIGURATION ??        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        private void BuildConfig()
        {
            Controls.Add(new Label
            {
                Location = new Point(8, 380), Size = new Size(1400, 26),
                Text = "CONFIGURATION",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });

            var tabs = new TabControl
            {
                Location  = new Point(8, 410), Size = new Size(1400, 500),
                Alignment = TabAlignment.Left, Multiline = true,
                // Stage 60 ??ItemSize ?�무 ?�아 ???�름 ?�리???�슈 ?�정 (30,80 ??100,32)
                SizeMode  = TabSizeMode.Fixed, ItemSize = new Size(100, 32),
                Font      = UiTheme.ButtonFont
            };
            tabs.TabPages.Add(new TabPage { Text = "STATUS", BackColor = UiTheme.OptionPanelBg });
            tabs.TabPages.Add(new TabPage { Text = "CONFIG", BackColor = UiTheme.OptionPanelBg });
            tabs.TabPages.Add(new TabPage { Text = "SPEED",  BackColor = UiTheme.OptionPanelBg });
            FillConfigTab(tabs.TabPages[1]);
            // Stage 60 ??STATUS ??? 비어?�으므�?진입 ??CONFIG ??�� 보이?�록 default 변�?            tabs.SelectedIndex = 1;
            Controls.Add(tabs);
        }

        private static void FillConfigTab(TabPage tab)
        {
            void Block(int x, int y, int w, int h, string title, (string, string)[] pairs)
            {
                var p = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = UiTheme.OptionPanelBg };
                p.Controls.Add(new Label
                {
                    Dock = DockStyle.Top, Height = 22, Text = title,
                    BackColor = Color.FromArgb(0x80,0x80,0x80), ForeColor = Color.White,
                    Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(6,0,0,0)
                });
                int yy = 28;
                foreach (var pair in pairs)
                {
                    p.Controls.Add(new Label { Location = new Point(4, yy),  Size = new Size(140, 24), Text = pair.Item1, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑�? 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
                    p.Controls.Add(new Label { Location = new Point(148, yy),Size = new Size(w - 152, 24), Text = pair.Item2, BackColor = Color.White, Font = new Font("Consolas", 9F), TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0,0,6,0), BorderStyle = BorderStyle.FixedSingle });
                    yy += 26;
                }
                tab.Controls.Add(p);
            }

            Block(0,   0, 430, 260, "CONFIG", new[] { ("OUTPUT MODE","PULSE-HIGH/CW/CCW"), ("INPUT MODE","OBVERSE SQR4"), ("INPUT SOURCE","ENCODER"), ("Z PHASE LEVEL","HIGH"), ("SERVO LEVEL","HIGH"), ("MAXIMUM VELOCITY","3,000,000") });
            Block(440, 0, 430, 170, "INPOSITION", new[] { ("LEVEL","ACTIVE HIGH"), ("SOFTWARE","DISABLE"), ("SOFTWARE LENGTH","10 pulse") });
            Block(440, 174, 430, 86, "HOME", new[] { ("SIGNAL","LOW"), ("MODE","NEGATIVE LIMIT") });
            Block(880, 0, 430, 260, "LIMIT", new[] { ("STOP MODE","EMERGENCY"), ("NEGATIVE LEVEL","ACTIVE LOW"), ("POSITIVE LEVEL","ACTIVE LOW"), ("SOFTWARE","DISABLE"), ("SOFTWARE NEGATIVE","0 pulse"), ("SOFTWARE POSITIVE","1,000,000 pulse") });
            Block(0,   266, 430, 120, "EMERGENCY SIGNAL", new[] { ("LEVEL","DISABLE"), ("STOP MODE","EMERGENCY") });
            Block(440, 266, 430, 120, "ALARM", new[] { ("RESET SIGNAL","HIGH"), ("LEVEL","ACTIVE LOW") });
            Block(880, 266, 430, 120, "POSITION CLEAR", new[] { ("ENABLED","FALSE"), ("PULSE","10,000 pulse") });
        }

        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        //  최하??공통 ?�션
        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        private void BuildActions()
        {
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(8),
                BackColor = UiTheme.OptionPanelBg, FlowDirection = FlowDirection.LeftToRight
            };
            Action<string, Action<BaseAxis>> add = (label, op) =>
            {
                var b = new Controls.ActionButton { Text = label, Size = new Size(120, 44), Margin = new Padding(4) };
                b.Click += (s, e) => { if (_grid.CurrentRow is DataGridViewRow row && row.Index < _axes.Count) op(_axes[row.Index]); };
                actions.Controls.Add(b);
            };
            add("ENABLE",    ax => ax.ServoOn());
            add("DISABLE",   ax => ax.ServoOff());
            add("HOME",      async ax => await ax.HomeSearchAsync());
            add("ALL STOP",  ax => { foreach (var a in _axes) a.Stop(); });
            add("ALRAM CLEAR", ax => { foreach (var a in _axes) a.ResetAlarm(); });
            add("ALL SERVO OFF",ax => { foreach (var a in _axes) a.ServoOff(); });
            add("SERVO ON",  ax => ax.ServoOn());
            add("SERVO OFF", ax => ax.ServoOff());

            // ?�?� AXL ?�라미터 ?�일 로드/?�??(Ajin ?�용 ?�에�??��? ?�음) ?�?�
            var btnLoad = new Controls.ActionButton { Text = "PARA LOAD", Size = new Size(120, 44), Margin = new Padding(4) };
            var btnSave = new Controls.ActionButton { Text = "PARA SAVE", Size = new Size(120, 44), Margin = new Padding(4) };
            btnLoad.Click += (s, e) => DoLoadPara();
            btnSave.Click += (s, e) => DoSavePara();
            actions.Controls.Add(btnLoad);
            actions.Controls.Add(btnSave);

            // ?�?� 보드 ?�캔 ?�스???�이?�로�??�?�
            var btnScan = new Controls.ActionButton { Text = "BOARD SCAN", Size = new Size(140, 44), Margin = new Padding(4) };
            btnScan.Click += (s, e) =>
            {
                using (var dlg = new Dialogs.BoardScanDialog(_axes))
                    dlg.ShowDialog(FindForm());
            };
            actions.Controls.Add(btnScan);

            Controls.Add(actions);
        }

        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        //  ?�리 ?�회
        // ?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�?�
        private static IEnumerable<BaseAxis> EnumerateAxes(CDT320_Machine m)
        {
            foreach (var u in m.Units)
                foreach (var a in Rec(u)) yield return a;
        }
        private static IEnumerable<BaseAxis> Rec(BaseEquipmentNode node)
        {
            if (node is BaseAxis ax) { yield return ax; yield break; }
            var prop = node.GetType().GetProperty("Components");
            if (prop != null && prop.GetValue(node) is System.Collections.IEnumerable comps)
                foreach (BaseEquipmentNode c in comps)
                    foreach (var a in Rec(c))
                        yield return a;
        }
    }
}
