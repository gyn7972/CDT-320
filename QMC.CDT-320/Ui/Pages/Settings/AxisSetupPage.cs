using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// Stage 59 — Axis Setup 페이지.
    /// 메뉴얼(CDT-310/CDT-300)의 37개 축 정보를 그리드로 표시 + 편집 + 저장.
    /// MotionMap.cs(Sim) / SimulatorBridge(실축 매핑) 와 동기화.
    /// </summary>
    public class AxisSetupPage : PageBase
    {
        public class AxisRow
        {
            [DataMember] public int    No          { get; set; }
            [DataMember] public string Name        { get; set; }
            [DataMember] public string Module      { get; set; }
            [DataMember] public double Stroke      { get; set; }
            [DataMember] public bool   Brake       { get; set; }
            [DataMember] public double SoftLimitNeg{ get; set; }
            [DataMember] public double SoftLimitPos{ get; set; }
            [DataMember] public double DefaultVel  { get; set; }
            [DataMember] public string HomeDir     { get; set; }   // POS/NEG
            [DataMember] public string Unit        { get; set; }   // mm/deg
            // Stage 61 — AJINEXTEK 보드/채널 매핑 (IO LIST_R0 의 Master.Slot 기준)
            [DataMember] public int    BoardNo     { get; set; }
            [DataMember] public int    ChannelNo   { get; set; }
            /// <summary>AjinConfig 매핑에 사용할 키 (PickerComponent 의 BaseAxis.Name 과 일치).</summary>
            [DataMember] public string ConfigKey   { get; set; }
        }

        [DataContract]
        public class AxisStore
        {
            [DataMember] public List<AxisRow> Items { get; set; } = new List<AxisRow>();
        }

        private DataGridView _grid;
        private List<AxisRow> _items;
        private static readonly string SavePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "axis_setup.json");

        public AxisSetupPage()
        {
            Controls.Add(CreateSectionHeader("set.axisSetup"));
            BuildHeader();
            BuildGrid();
            BuildActions();
            _items = LoadOrSeed();
            FillGrid();
        }

        private void BuildHeader()
        {
            Controls.Add(new Label
            {
                Location = new Point(8, 36), Size = new Size(1400, 26),
                Text = "AXIS SETUP — Stroke / Soft Limit / Velocity / Brake / Home Direction (37 axes)",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
        }

        private void BuildGrid()
        {
            _grid = new DataGridView
            {
                Location = new Point(8, 66), Size = new Size(1400, 800),
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                Font = new Font("맑은 고딕", 9F),
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50, 0x50, 0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                },
                RowTemplate = { Height = 22 }
            };
            _grid.Columns.Add("NO",      "NO");
            _grid.Columns.Add("MODULE",  "MODULE");
            _grid.Columns.Add("NAME",    "AXIS NAME");
            _grid.Columns.Add("BOARD",   "BOARD#");          // Stage 61
            _grid.Columns.Add("CH",      "CH (slot)");        // Stage 61
            _grid.Columns.Add("UNIT",    "UNIT");
            _grid.Columns.Add("STROKE",  "STROKE");
            _grid.Columns.Add("BRAKE",   "BRAKE");
            _grid.Columns.Add("SLN",     "SOFT LIMIT(-)");
            _grid.Columns.Add("SLP",     "SOFT LIMIT(+)");
            _grid.Columns.Add("VEL",     "DEFAULT VEL");
            _grid.Columns.Add("HOMEDIR", "HOME DIR");

            _grid.Columns["NO"]    .ReadOnly = true;
            _grid.Columns["MODULE"].ReadOnly = true;
            _grid.Columns["NAME"]  .ReadOnly = true;
            _grid.Columns["UNIT"]  .ReadOnly = true;

            _grid.CellEndEdit += OnCellEdit;
            Controls.Add(_grid);
        }

        private void BuildActions()
        {
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(8),
                BackColor = UiTheme.OptionPanelBg, FlowDirection = FlowDirection.LeftToRight
            };
            var save = new Controls.ActionButton { Text = "SAVE", Size = new Size(120, 44), Margin = new Padding(4) };
            save.Click += (s, e) => DoSave();
            var reload = new Controls.ActionButton { Text = "RELOAD", Size = new Size(120, 44), Margin = new Padding(4) };
            reload.Click += (s, e) => { _items = LoadOrSeed(); FillGrid(); };
            var reset = new Controls.ActionButton { Text = "RESET DEFAULT", Size = new Size(160, 44), Margin = new Padding(4) };
            reset.Click += (s, e) =>
            {
                if (MessageBox.Show("기본값으로 초기화?", "Reset", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
                _items = SeedDefault();
                FillGrid();
            };
            var apply = new Controls.ActionButton { Text = "APPLY (Soft Limit 반영)", Size = new Size(220, 44), Margin = new Padding(4) };
            apply.Click += (s, e) => ApplyToAxes();
            actions.Controls.Add(save);
            actions.Controls.Add(reload);
            actions.Controls.Add(reset);
            actions.Controls.Add(apply);
            Controls.Add(actions);
        }

        // ── 메뉴얼 기준 37 axes seed ─────────────────────────────────
        public static List<AxisRow> SeedDefault()
        {
            var L = new List<AxisRow>();
            void Add(int no, string mod, string nm, string cfgKey, int board, int ch,
                     double stroke, bool brk, string unit = "mm",
                     double vel = 100.0, string homedir = "NEG")
                => L.Add(new AxisRow {
                    No = no, Module = mod, Name = nm, ConfigKey = cfgKey,
                    BoardNo = board, ChannelNo = ch,
                    Stroke = stroke, Brake = brk, Unit = unit,
                    DefaultVel = vel, HomeDir = homedir,
                    SoftLimitNeg = 0, SoftLimitPos = stroke });

            // ── Board 0 (slot 9~F) ───────────────────────────────────────
            Add(0,  "InputLoader",     "WAFER LIFTER_Z",        "ElevatorZ_Input",      0,  9, 200, true);
            Add(1,  "InputLoader",     "WAFER FEEDER_Y",        "FeederY_Input",        0, 10, 300, false);
            Add(2,  "InputStage",      "WAFER STAGE_Y",         "StageY",               0, 11, 400, false);
            Add(3,  "InputStage",      "WAFER STAGE_T",         "StageT",               0, 12, 360, false, "deg", vel: 30, homedir: "POS");
            Add(4,  "InputStage",      "WAFER EXPANDING_Z",     "ExpanderZ",            0, 13, 100, false);
            Add(5,  "InputStage",      "ALIGN VISION_X",        "CameraX",              0, 14, 300, false);
            Add(6,  "InputStage",      "NEEDLE_X",              "NeedleBlockX",         0, 15, 200, false);
            // ── Board 1 (slot 0~F) ───────────────────────────────────────
            Add(7,  "InputStage",      "NEEDLE_Z",              "NeedleZ",              1,  0, 100, true);
            Add(8,  "InputStage",      "EJECT PIN_Z",           "EjectPinZ",            1,  1,  50, false, vel: 50);
            Add(9,  "FrontPicker",     "FRONT PICKER_X",        "LeftArm_ArmX",         1,  2,1500, false, vel: 800);
            Add(10, "FrontPicker",     "FRONT PICKER_Y",        "LeftArm_ArmY",         1,  3, 750, false);
            Add(11, "FrontPicker",     "FRONT PICKER_T0",       "LeftArm_Picker1_T",    1,  4, 360, false, "deg");
            Add(12, "FrontPicker",     "FRONT PICKER_Z0",       "LeftArm_Picker1_Z",    1,  5,  50, false, vel: 200);
            Add(13, "FrontPicker",     "FRONT PICKER_T1",       "LeftArm_Picker2_T",    1,  6, 360, false, "deg");
            Add(14, "FrontPicker",     "FRONT PICKER_Z1",       "LeftArm_Picker2_Z",    1,  7,  50, false, vel: 200);
            Add(15, "FrontPicker",     "FRONT PICKER_T2",       "LeftArm_Picker3_T",    1,  8, 360, false, "deg");
            Add(16, "FrontPicker",     "FRONT PICKER_Z2",       "LeftArm_Picker3_Z",    1,  9,  50, false, vel: 200);
            Add(17, "FrontPicker",     "FRONT PICKER_T3",       "LeftArm_Picker4_T",    1, 10, 360, false, "deg");
            Add(18, "FrontPicker",     "FRONT PICKER_Z3",       "LeftArm_Picker4_Z",    1, 11,  50, false, vel: 200);
            Add(19, "FrontPicker",     "FRONT SIDE VISION_Y0",  "LeftArm_SideVisionY",  1, 12, 200, false);
            Add(20, "RearPicker",      "REAR SIDE VISION_Y0",   "RightArm_SideVisionY", 1, 13, 200, false);
            Add(21, "RearPicker",      "REAR PICKER_X",         "RightArm_ArmX",        1, 14,1500, false, vel: 800);
            Add(22, "RearPicker",      "REAR PICKER_Y",         "RightArm_ArmY",        1, 15, 750, false);
            // ── Board 2 (slot 0~E) ───────────────────────────────────────
            Add(23, "RearPicker",      "REAR PICKER_T0",        "RightArm_Picker1_T",   2,  0, 360, false, "deg");
            Add(24, "RearPicker",      "REAR PICKER_Z0",        "RightArm_Picker1_Z",   2,  1,  50, false, vel: 200);
            Add(25, "RearPicker",      "REAR PICKER_T1",        "RightArm_Picker2_T",   2,  2, 360, false, "deg");
            Add(26, "RearPicker",      "REAR PICKER_Z1",        "RightArm_Picker2_Z",   2,  3,  50, false, vel: 200);
            Add(27, "RearPicker",      "REAR PICKER_T2",        "RightArm_Picker3_T",   2,  4, 360, false, "deg");
            Add(28, "RearPicker",      "REAR PICKER_Z2",        "RightArm_Picker3_Z",   2,  5,  50, false, vel: 200);
            Add(29, "RearPicker",      "REAR PICKER_T3",        "RightArm_Picker4_T",   2,  6, 360, false, "deg");
            Add(30, "RearPicker",      "REAR PICKER_Z3",        "RightArm_Picker4_Z",   2,  7,  50, false, vel: 200);
            Add(31, "OutputStage",     "NG BIN_Y",              "NgStage_StageY",       2,  8, 500, false);
            Add(32, "OutputStage",     "NG BIN_Z",              "NgStage_StageZ",       2,  9, 100, false);
            Add(33, "OutputStage",     "GOOD BIN_Y",            "GoodStage_StageY",     2, 10, 500, false);
            Add(34, "OutputStage",     "INSPECTION VISION_X",   "BinCameraX",           2, 11, 300, false);
            Add(35, "OutputUnloader",  "BIN FEEDER_Y",          "FeederY_Output",       2, 12, 300, false);
            Add(36, "OutputUnloader",  "BIN LIFTER_Z",          "ElevatorZ_Output",     2, 13, 200, true);
            Add(37, "InputStage",      "ALIGN VISION_Z",        "CameraZ",              2, 14, 100, true, vel: 50);
            return L;
        }

        // ── Persistence ──────────────────────────────────────────────
        private static List<AxisRow> LoadOrSeed()
        {
            try
            {
                if (File.Exists(SavePath))
                    using (var fs = File.OpenRead(SavePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(AxisStore));
                        var s = (AxisStore)ser.ReadObject(fs);
                        if (s?.Items != null && s.Items.Count > 0) return s.Items;
                    }
            }
            catch { }
            return SeedDefault();
        }

        private void DoSave()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                using (var fs = File.Create(SavePath))
                {
                    var ser = new DataContractJsonSerializer(typeof(AxisStore));
                    ser.WriteObject(fs, new AxisStore { Items = _items });
                }
                MessageBox.Show("저장 완료.\n" + SavePath);
            }
            catch (Exception ex) { MessageBox.Show("실패: " + ex.Message); }
        }

        // ── Grid ─────────────────────────────────────────────────────
        private void FillGrid()
        {
            _grid.Rows.Clear();
            string lastMod = null;
            foreach (var it in _items)
            {
                int idx = _grid.Rows.Add(
                    "#" + it.No.ToString("00"), it.Module, it.Name,
                    it.BoardNo.ToString(),
                    it.ChannelNo.ToString("X"),     // hex (slot 0~F)
                    it.Unit,
                    it.Stroke.ToString("F1"), it.Brake ? "ON" : "OFF",
                    it.SoftLimitNeg.ToString("F1"), it.SoftLimitPos.ToString("F1"),
                    it.DefaultVel.ToString("F1"), it.HomeDir);
                if (it.Module != lastMod)
                {
                    _grid.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(0xEC, 0xF0, 0xF6);
                    _grid.Rows[idx].DefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                    lastMod = it.Module;
                }
            }
        }

        private void OnCellEdit(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var it = _items[e.RowIndex];
            string col = _grid.Columns[e.ColumnIndex].Name;
            string txt = (_grid.Rows[e.RowIndex].Cells[col].Value as string) ?? "";
            try
            {
                switch (col)
                {
                    case "STROKE": if (double.TryParse(txt, out var v1)) it.Stroke = v1; break;
                    case "BRAKE":  it.Brake = txt.Trim().ToUpper().StartsWith("ON"); break;
                    case "SLN":    if (double.TryParse(txt, out var v2)) it.SoftLimitNeg = v2; break;
                    case "SLP":    if (double.TryParse(txt, out var v3)) it.SoftLimitPos = v3; break;
                    case "VEL":    if (double.TryParse(txt, out var v4)) it.DefaultVel = v4; break;
                    case "HOMEDIR": it.HomeDir = txt.Trim().ToUpper(); break;
                    case "BOARD":  if (int.TryParse(txt.Trim(), out var b1)) it.BoardNo = b1; break;
                    case "CH":     // hex 또는 decimal 모두 허용
                        if (int.TryParse(txt.Trim(),
                                System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture, out var c1))
                            it.ChannelNo = c1;
                        else if (int.TryParse(txt.Trim(), out var c2))
                            it.ChannelNo = c2;
                        break;
                }
                FillGrid();
            }
            catch { }
        }

        /// <summary>SoftLimit + Default Velocity + Board/Channel 값을 실 축 및 AjinConfig 에 반영 (Apply).</summary>
        private void ApplyToAxes()
        {
            var host = FindForm() as Form1;
            if (host?.Machine == null) { MessageBox.Show("Machine 미초기화"); return; }

            int axisApplied = 0;
            int cfgApplied  = 0;

            foreach (var ax in EnumerateAxes(host.Machine))
            {
                var match = _items.FirstOrDefault(x => string.Equals(x.Name, ax.Name, StringComparison.OrdinalIgnoreCase));
                if (match == null) continue;
                try
                {
                    ax.Setup.SoftLimitMinus = match.SoftLimitNeg;
                    ax.Setup.SoftLimitPlus  = match.SoftLimitPos;
                    var setupType = ax.Setup.GetType();
                    var velProp = setupType.GetProperty("DefaultVelocity") ?? setupType.GetProperty("Velocity");
                    if (velProp != null && velProp.CanWrite) velProp.SetValue(ax.Setup, match.DefaultVel);
                    var strokeProp = setupType.GetProperty("Stroke");
                    if (strokeProp != null && strokeProp.CanWrite) strokeProp.SetValue(ax.Setup, match.Stroke);
                    axisApplied++;
                }
                catch { }
            }

            // Stage 61 — Board/Channel 값을 AjinConfig 에도 반영 (ConfigKey 기준)
            try
            {
                var cfg = QMC.CDT320.Ajin.AjinConfigStore.Current;
                foreach (var it in _items)
                {
                    if (string.IsNullOrEmpty(it.ConfigKey)) continue;
                    if (!cfg.Axes.TryGetValue(it.ConfigKey, out var am))
                    {
                        am = new QMC.CDT320.Ajin.AxisMap();
                        cfg.Axes[it.ConfigKey] = am;
                    }
                    am.Axis      = it.No;
                    am.BoardNo   = it.BoardNo;
                    am.ChannelNo = it.ChannelNo;
                    cfgApplied++;
                }
                QMC.CDT320.Ajin.AjinConfigStore.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("AjinConfig 반영 실패: " + ex.Message);
            }

            MessageBox.Show($"Soft Limit/Velocity 적용 축: {axisApplied}\nAjinConfig (Board/Ch) 반영: {cfgApplied}");
        }

        private static IEnumerable<QMC.Common.Motion.BaseAxis> EnumerateAxes(QMC.CDT320.CDT320_Machine m)
        {
            foreach (var u in m.Units) foreach (var a in Rec(u)) yield return a;
        }
        private static IEnumerable<QMC.Common.Motion.BaseAxis> Rec(QMC.Common.BaseEquipmentNode node)
        {
            if (node is QMC.Common.Motion.BaseAxis ax) { yield return ax; yield break; }
            var prop = node.GetType().GetProperty("Components");
            if (prop != null && prop.GetValue(node) is System.Collections.IEnumerable comps)
                foreach (QMC.Common.BaseEquipmentNode c in comps) foreach (var a in Rec(c)) yield return a;
        }
    }
}
