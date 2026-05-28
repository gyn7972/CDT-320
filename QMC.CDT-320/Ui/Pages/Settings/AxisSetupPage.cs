using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using QMC.CDT320.Ajin;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// Stage 59 — Axis Setup 페이지.
    /// 메뉴얼(CDT-310/CDT-300)의 37개 축 정보를 그리드로 표시 + 편집 + 저장.
    /// MotionMap.cs(Sim) / SimulatorBridge(실축 매핑) 와 동기화.
    /// </summary>
    public partial class AxisSetupPage : PageBase
    {
        public class AxisRow
        {
            [DataMember] public int No { get; set; }
            [DataMember] public string Name { get; set; }
            [DataMember] public string Module { get; set; }
            [DataMember] public double Stroke { get; set; }
            [DataMember] public bool Brake { get; set; }
            [DataMember] public double SoftLimitNeg { get; set; }
            [DataMember] public double SoftLimitPos { get; set; }
            [DataMember] public double DefaultVel { get; set; }
            [DataMember] public string HomeDir { get; set; }   // POS/NEG
            [DataMember] public string Unit { get; set; }   // mm/deg
            // Stage 61 — AJINEXTEK 보드/채널 매핑 (IO LIST_R0 의 Master.Slot 기준)
            [DataMember] public int BoardNo { get; set; }
            [DataMember] public int ChannelNo { get; set; }
            /// <summary>AjinConfig 매핑에 사용할 키 (PickerComponent 의 BaseAxis.Name 과 일치).</summary>
            [DataMember] public string ConfigKey { get; set; }
        }

        [DataContract]
        public class AxisStore
        {
            [DataMember] public List<AxisRow> Items { get; set; } = new List<AxisRow>();
        }

        private List<AxisRow> _items;
        private static readonly string SavePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "axis_setup.json");

        public AxisSetupPage()
        {
            InitializeComponent();
            if (!DesignMode)
            {
                ApplyRuntimeUi();
                _items = LoadOrSeed();
                FillGrid();
            }
            //ApplyRuntimeUi();
            //_items = LoadOrSeed();
            //FillGrid();
        }

        /// <summary>
        /// Designer 에서 표현하기 어려운 UI 요소(PageBase.CreateSectionHeader, UiTheme 정적 색상 등)를 런타임에 적용한다.
        /// </summary>
        private void ApplyRuntimeUi()
        {
            // i18n 섹션 헤더 (PageBase 헬퍼 사용)
            Controls.Add(CreateSectionHeader("set.axisSetup"));

            // UiTheme 정적 색상은 디자이너에서 직접 표현되지 않으므로 런타임 적용
            lblSubHeader.BackColor = UiTheme.StatusBarBg;
            lblSubHeader.ForeColor = Color.White;
            lblSubHeader.Font = UiTheme.SectionFont;

            actionsPanel.BackColor = UiTheme.OptionPanelBg;
        }

        // ── Button click handlers (Designer 에서 연결) ────────────────
        private void OnSaveClick(object sender, EventArgs e) => DoSave();

        private void OnReloadClick(object sender, EventArgs e)
        {
            _items = LoadOrSeed();
            FillGrid();
        }

        private void OnResetClick(object sender, EventArgs e)
        {
            if (MessageBox.Show("기본값으로 초기화?", "Reset", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            _items = SeedDefault();
            FillGrid();
        }

        private void OnApplyClick(object sender, EventArgs e) => ApplyToAxes();

        // ── 메뉴얼 기준 37 axes seed ─────────────────────────────────
        public static List<AxisRow> SeedDefault()
        {
            var L = new List<AxisRow>();
            foreach (var axis in AjinAxisDefaults.All)
            {
                L.Add(new AxisRow
                {
                    No = axis.Axis,
                    Module = axis.Module,
                    Name = axis.AxisName,
                    ConfigKey = axis.AxisName,
                    BoardNo = axis.BoardNo,
                    ChannelNo = axis.ChannelNo,
                    Stroke = axis.Stroke,
                    Brake = axis.Brake,
                    Unit = axis.Unit,
                    DefaultVel = axis.DefaultVel,
                    HomeDir = axis.HomeDir,
                    SoftLimitNeg = 0,
                    SoftLimitPos = axis.Stroke
                });
            }
            return L;
        }
        // Persistence ──────────────────────────────────────────────
        private static List<AxisRow> LoadOrSeed()
        {
            try
            {
                if (File.Exists(SavePath))
                    using (var fs = File.OpenRead(SavePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(AxisStore));
                        var s = (AxisStore)ser.ReadObject(fs);
                        if (s?.Items != null && s.Items.Count > 0) return MergeWithDefaults(s.Items);
                    }
            }
            catch { }
            return SeedDefault();
        }

        private static List<AxisRow> MergeWithDefaults(List<AxisRow> saved)
        {
            var result = SeedDefault();
            foreach (var row in result)
            {
                var savedRow = saved.FirstOrDefault(x =>
                    string.Equals(AjinAxisDefaults.ResolveName(x.ConfigKey), row.ConfigKey, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(AjinAxisDefaults.ResolveName(x.Name), row.ConfigKey, StringComparison.OrdinalIgnoreCase));
                if (savedRow == null) continue;

                row.BoardNo = savedRow.BoardNo;
                row.ChannelNo = savedRow.ChannelNo;
                row.Stroke = savedRow.Stroke;
                row.Brake = savedRow.Brake;
                row.SoftLimitNeg = savedRow.SoftLimitNeg;
                row.SoftLimitPos = savedRow.SoftLimitPos;
                row.DefaultVel = savedRow.DefaultVel;
                row.HomeDir = savedRow.HomeDir;
                row.Unit = savedRow.Unit;
            }
            return result;
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
            grid.Rows.Clear();
            string lastMod = null;
            foreach (var it in _items)
            {
                int idx = grid.Rows.Add(
                    "#" + it.No.ToString("00"), it.Module, it.Name,
                    it.BoardNo.ToString(),
                    it.ChannelNo.ToString("X"),     // hex (slot 0~F)
                    it.Unit,
                    it.Stroke.ToString("F1"), it.Brake ? "ON" : "OFF",
                    it.SoftLimitNeg.ToString("F1"), it.SoftLimitPos.ToString("F1"),
                    it.DefaultVel.ToString("F1"), it.HomeDir);
                if (it.Module != lastMod)
                {
                    grid.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(0xEC, 0xF0, 0xF6);
                    grid.Rows[idx].DefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                    lastMod = it.Module;
                }
            }
        }

        private void OnCellEdit(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var it = _items[e.RowIndex];
            string col = grid.Columns[e.ColumnIndex].Name;
            string txt = (grid.Rows[e.RowIndex].Cells[col].Value as string) ?? "";
            try
            {
                switch (col)
                {
                    case "STROKE": if (double.TryParse(txt, out var v1)) it.Stroke = v1; break;
                    case "BRAKE": it.Brake = txt.Trim().ToUpper().StartsWith("ON"); break;
                    case "SLN": if (double.TryParse(txt, out var v2)) it.SoftLimitNeg = v2; break;
                    case "SLP": if (double.TryParse(txt, out var v3)) it.SoftLimitPos = v3; break;
                    case "VEL": if (double.TryParse(txt, out var v4)) it.DefaultVel = v4; break;
                    case "HOMEDIR": it.HomeDir = txt.Trim().ToUpper(); break;
                    case "BOARD": if (int.TryParse(txt.Trim(), out var b1)) it.BoardNo = b1; break;
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
            int cfgApplied = 0;

            foreach (var ax in EnumerateAxes(host.Machine))
            {
                string axisName = AjinAxisDefaults.ResolveName(ax.Name);
                var match = _items.FirstOrDefault(x =>
                    string.Equals(x.ConfigKey, axisName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Name, axisName, StringComparison.OrdinalIgnoreCase));
                if (match == null) continue;
                try
                {
                    ax.Setup.SoftLimitMinus = match.SoftLimitNeg;
                    ax.Setup.SoftLimitPlus = match.SoftLimitPos;
                    ax.Recipe.DefaultVelocity = match.DefaultVel;
                    var setupType = ax.Setup.GetType();
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
                    string configKey = AjinAxisDefaults.ResolveName(it.ConfigKey);
                    if (string.IsNullOrEmpty(configKey)) continue;
                    if (!cfg.Axes.TryGetValue(configKey, out var am))
                    {
                        am = new QMC.CDT320.Ajin.AxisMap();
                        cfg.Axes[configKey] = am;
                    }
                    am.Axis = it.No;
                    am.BoardNo = it.BoardNo;
                    am.ChannelNo = it.ChannelNo;
                    cfgApplied++;
                }
                QMC.CDT320.Ajin.AjinConfigStore.Save();
                QMC.CDT320.Ajin.AjinFactory.ReloadConfiguredAxes();
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
