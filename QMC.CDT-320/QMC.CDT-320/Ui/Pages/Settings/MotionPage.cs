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
    /// 설정 - MOTION.
    /// 상단: 축 전체 목록(DataGridView) — 호스트 Machine 트리에서 실제 축을 읽어 실시간 갱신.
    /// 하단: CONFIGURATION (STATUS/CONFIG/SPEED 탭).
    /// 최하단: ENABLE/DISABLE/HOME/ALL STOP/... 공통 액션.
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

        // ──────────────────────────────────────
        //  상단 모듈 리스트
        // ──────────────────────────────────────
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
                Font                    = new Font("맑은 고딕", 9F),
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50, 0x50, 0x50),
                    ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font      = new Font("맑은 고딕", 9F, FontStyle.Bold)
                },
                RowTemplate = { Height = 26 }
            };
            _grid.Columns.Add("INDEX",            "INDEX");
            _grid.Columns.Add("KEY",              "KEY");
            _grid.Columns.Add("NO",               "NO.");
            _grid.Columns.Add("STATUS",           "STATUS");
            _grid.Columns.Add("COMMAND_POSITION", "COMMAND POSITION");
            _grid.Columns.Add("ACTUAL_POSITION",  "ACTUAL POSITION");
            _grid.Columns.Add("DONE",             "DONE");
            _grid.Columns.Add("INP_DONE",         "INP DONE");
            _grid.Columns.Add("HOME_END",         "HOME END");
            _grid.Columns.Add("ALARM",            "ALARM");
            Controls.Add(_grid);
        }

        // ──────────────────────────────────────
        //  축 수집 + 실시간 갱신
        // ──────────────────────────────────────
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
                    ++idx, ax.Name, "-",
                    StateText(ax),
                    ax.CommandPosition.ToString("F1"),
                    ax.ActualPosition.ToString("F1"),
                    ax.IsInPosition ? "ON" : "OFF",
                    ax.IsInPosition ? "ON" : "OFF",
                    ax.IsHomeDone   ? "ON" : "OFF",
                    ax.IsAlarm      ? "ON" : "OFF");

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
                { "WAFER LIFTER_Z", "0" }, { "WAFER FEEDER_Y", "1" }, { "WAFER STAGE_Y", "2" },
                { "WAFER STAGE_T", "3" }, { "WAFER EXPANDING_Z", "4" }, { "ALIGN VISION_X", "5" },
                { "NEEDLE_X", "6" }, { "NEEDLE_Z", "7" }, { "FRONT PICKER_X", "9" }, { "REAR PICKER_X", "21" }
            };
            for (int i = 0; i < axes.GetLength(0); i++)
                _grid.Rows.Add(i + 1, axes[i, 0], axes[i, 1], "NONE", "0", "0", "OFF", "OFF", "OFF", "OFF");
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
                row.Cells[3].Value = StateText(ax);
                row.Cells[4].Value = ax.CommandPosition.ToString("F1");
                row.Cells[5].Value = ax.ActualPosition.ToString("F1");
                row.Cells[6].Value = ax.IsInPosition ? "ON" : "OFF";
                row.Cells[7].Value = ax.IsInPosition ? "ON" : "OFF";
                row.Cells[8].Value = ax.IsHomeDone   ? "ON" : "OFF";
                row.Cells[9].Value = ax.IsAlarm      ? "ON" : "OFF";
                row.DefaultCellStyle.ForeColor = ax.IsAlarm ? Color.IndianRed
                    : (ax.IsMoving ? Color.SteelBlue : Color.Black);
            }
        }

        private void OnAxisPos(BaseAxis ax, double pos) { /* Timer 가 처리 */ }
        private void OnAxisDone(BaseAxis ax)            { /* Timer 가 처리 */ }

        // ──────────────────────────────────────
        //  AXL 파라미터 파일 LOAD/SAVE
        // ──────────────────────────────────────

        private void DoLoadPara()
        {
            if (!QMC.CDT320.Ajin.AjinSystem.IsOpen)
            {
                MessageBox.Show("AXL library is not open. Enable UseAjin in Settings → GENERAL and restart.");
                return;
            }
            using (var dlg = new OpenFileDialog { Filter = "Motion parameters (*.mot)|*.mot|All files (*.*)|*.*" })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                uint r = QMC.CDT320.Ajin.Axl.AxmMotLoadParaAll(dlg.FileName);
                if (QMC.CDT320.Ajin.AxtReturn.IsSuccess(r))
                {
                    QMC.CDT320.Logging.EventLogger.Write(QMC.CDT320.Logging.EventKind.Event, "QMC", "PARA-LOAD", dlg.FileName);
                    MessageBox.Show("파라미터 로드 완료.");
                }
                else
                {
                    MessageBox.Show("파라미터 로드 실패. 0x" + r.ToString("X4"));
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
                uint r = QMC.CDT320.Ajin.Axl.AxmMotSaveParaAll(dlg.FileName);
                if (QMC.CDT320.Ajin.AxtReturn.IsSuccess(r))
                {
                    QMC.CDT320.Logging.EventLogger.Write(QMC.CDT320.Logging.EventKind.Event, "QMC", "PARA-SAVE", dlg.FileName);
                    MessageBox.Show("파라미터 저장 완료.");
                }
                else
                {
                    MessageBox.Show("파라미터 저장 실패. 0x" + r.ToString("X4"));
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

        // ──────────────────────────────────────
        //  CONFIGURATION 탭
        // ──────────────────────────────────────
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
                // Stage 60 — ItemSize 너무 작아 탭 이름 잘리는 이슈 수정 (30,80 → 100,32)
                SizeMode  = TabSizeMode.Fixed, ItemSize = new Size(100, 32),
                Font      = UiTheme.ButtonFont
            };
            tabs.TabPages.Add(new TabPage { Text = "STATUS", BackColor = UiTheme.OptionPanelBg });
            tabs.TabPages.Add(new TabPage { Text = "CONFIG", BackColor = UiTheme.OptionPanelBg });
            tabs.TabPages.Add(new TabPage { Text = "SPEED",  BackColor = UiTheme.OptionPanelBg });
            FillConfigTab(tabs.TabPages[1]);
            // Stage 60 — STATUS 탭은 비어있으므로 진입 시 CONFIG 탭이 보이도록 default 변경
            tabs.SelectedIndex = 1;
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
                    p.Controls.Add(new Label { Location = new Point(4, yy),  Size = new Size(140, 24), Text = pair.Item1, BackColor = Color.FromArgb(0xD0,0xD0,0xD0), Font = new Font("맑은 고딕", 9F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6,0,0,0), BorderStyle = BorderStyle.FixedSingle });
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

        // ──────────────────────────────────────
        //  최하단 공통 액션
        // ──────────────────────────────────────
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

            // ── AXL 파라미터 파일 로드/저장 (Ajin 사용 시에만 의미 있음) ──
            var btnLoad = new Controls.ActionButton { Text = "PARA LOAD", Size = new Size(120, 44), Margin = new Padding(4) };
            var btnSave = new Controls.ActionButton { Text = "PARA SAVE", Size = new Size(120, 44), Margin = new Padding(4) };
            btnLoad.Click += (s, e) => DoLoadPara();
            btnSave.Click += (s, e) => DoSavePara();
            actions.Controls.Add(btnLoad);
            actions.Controls.Add(btnSave);

            // ── 보드 스캔 테스트 다이얼로그 ──
            var btnScan = new Controls.ActionButton { Text = "BOARD SCAN", Size = new Size(140, 44), Margin = new Padding(4) };
            btnScan.Click += (s, e) =>
            {
                using (var dlg = new Dialogs.BoardScanDialog(_axes))
                    dlg.ShowDialog(FindForm());
            };
            actions.Controls.Add(btnScan);

            Controls.Add(actions);
        }

        // ──────────────────────────────────────
        //  트리 순회
        // ──────────────────────────────────────
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
