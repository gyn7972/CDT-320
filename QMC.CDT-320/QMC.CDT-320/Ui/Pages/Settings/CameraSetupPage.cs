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
    /// Stage 59 — Camera Setup 페이지.
    /// CDT-310/300 비전 사양: Wafer / Bottom Inspection / Bin / Main / FrontSide / RearSide 6채널.
    /// 각 채널의 IP/Port/Role/Exposure/ROI 설정 + JSON 저장.
    /// </summary>
    public class CameraSetupPage : PageBase
    {
        public class CameraRow
        {
            [DataMember] public int    Index      { get; set; }
            [DataMember] public string Channel    { get; set; }
            [DataMember] public string Role       { get; set; }
            [DataMember] public string Host       { get; set; }
            [DataMember] public int    Port       { get; set; }
            [DataMember] public int    Width      { get; set; } = 640;
            [DataMember] public int    Height     { get; set; } = 480;
            [DataMember] public int    ExposureMs { get; set; } = 500;
            [DataMember] public double LightLevel { get; set; } = 0.5;
            [DataMember] public string Trigger    { get; set; } = "Software";
            [DataMember] public bool   AutoConnect{ get; set; } = true;
        }

        [DataContract]
        public class CameraStore
        {
            [DataMember] public List<CameraRow> Items { get; set; } = new List<CameraRow>();
        }

        private DataGridView _grid;
        private List<CameraRow> _items;
        private static readonly string SavePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "camera_setup.json");

        public CameraSetupPage()
        {
            Controls.Add(CreateSectionHeader("set.cameraSetup"));
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
                Text = "CAMERA SETUP — Vision PC 6채널 (Wafer / Bottom Inspection / Bin / Main / Top-Side / Bottom-Side)",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
        }

        private void BuildGrid()
        {
            _grid = new DataGridView
            {
                Location = new Point(8, 66), Size = new Size(1400, 400),
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
                RowTemplate = { Height = 26 }
            };
            _grid.Columns.Add("IDX",   "INDEX");
            _grid.Columns.Add("CH",    "CHANNEL");
            _grid.Columns.Add("ROLE",  "ROLE");
            _grid.Columns.Add("HOST",  "HOST");
            _grid.Columns.Add("PORT",  "PORT");
            _grid.Columns.Add("W",     "WIDTH");
            _grid.Columns.Add("H",     "HEIGHT");
            _grid.Columns.Add("EXP",   "EXPOSURE(ms)");
            _grid.Columns.Add("LIGHT", "LIGHT");
            _grid.Columns.Add("TRG",   "TRIGGER");
            _grid.Columns.Add("AUTO",  "AUTO CONN");

            _grid.Columns["IDX"]  .ReadOnly = true;
            _grid.Columns["CH"]   .ReadOnly = true;
            _grid.Columns["ROLE"] .ReadOnly = true;
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
            var test = new Controls.ActionButton { Text = "TEST CONN", Size = new Size(140, 44), Margin = new Padding(4) };
            test.Click += (s, e) => DoTestConnection();
            var apply = new Controls.ActionButton { Text = "APPLY (AppSettings)", Size = new Size(180, 44), Margin = new Padding(4) };
            apply.Click += (s, e) => ApplyToAppSettings();
            actions.Controls.Add(save);
            actions.Controls.Add(reload);
            actions.Controls.Add(test);
            actions.Controls.Add(apply);
            Controls.Add(actions);
        }

        public static List<CameraRow> SeedDefault()
        {
            return new List<CameraRow>
            {
                new CameraRow { Index=0, Channel="Wafer",          Role="WaferAlign",       Host="127.0.0.1", Port=5100, ExposureMs=400, LightLevel=0.6, Trigger="Software" },
                new CameraRow { Index=1, Channel="BottomInspection",Role="DiePresence",     Host="127.0.0.1", Port=5101, ExposureMs=300, LightLevel=0.7, Trigger="Software" },
                new CameraRow { Index=2, Channel="Bin",             Role="PlacementInspector",Host="127.0.0.1", Port=5103, ExposureMs=300, LightLevel=0.5, Trigger="Software" },
                new CameraRow { Index=3, Channel="Main",            Role="MainComm",        Host="127.0.0.1", Port=5104, ExposureMs=0,   LightLevel=0.0, Trigger="None" },
                new CameraRow { Index=4, Channel="FrontSide",         Role="FrontSide4Side",    Host="127.0.0.1", Port=5105, ExposureMs=300, LightLevel=0.8, Trigger="Software" },
                new CameraRow { Index=5, Channel="RearSide",      Role="RearSide4Side", Host="127.0.0.1", Port=5106, ExposureMs=300, LightLevel=0.8, Trigger="Software" },
            };
        }

        private static List<CameraRow> LoadOrSeed()
        {
            try
            {
                if (File.Exists(SavePath))
                    using (var fs = File.OpenRead(SavePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(CameraStore));
                        var s = (CameraStore)ser.ReadObject(fs);
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
                    var ser = new DataContractJsonSerializer(typeof(CameraStore));
                    ser.WriteObject(fs, new CameraStore { Items = _items });
                }
                MessageBox.Show("저장 완료.\n" + SavePath);
            }
            catch (Exception ex) { MessageBox.Show("실패: " + ex.Message); }
        }

        private void FillGrid()
        {
            _grid.Rows.Clear();
            foreach (var it in _items)
            {
                _grid.Rows.Add(
                    it.Index.ToString(), it.Channel, it.Role, it.Host, it.Port.ToString(),
                    it.Width.ToString(), it.Height.ToString(),
                    it.ExposureMs.ToString(), it.LightLevel.ToString("F2"),
                    it.Trigger, it.AutoConnect ? "ON" : "OFF");
            }
        }

        private void OnCellEdit(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var it = _items[e.RowIndex];
            string col = _grid.Columns[e.ColumnIndex].Name;
            string txt = (_grid.Rows[e.RowIndex].Cells[col].Value as string) ?? "";
            switch (col)
            {
                case "HOST":  it.Host = txt.Trim(); break;
                case "PORT":  if (int.TryParse(txt, out var p)) it.Port = p; break;
                case "W":     if (int.TryParse(txt, out var w)) it.Width = w; break;
                case "H":     if (int.TryParse(txt, out var h)) it.Height = h; break;
                case "EXP":   if (int.TryParse(txt, out var x)) it.ExposureMs = x; break;
                case "LIGHT": if (double.TryParse(txt, out var l)) it.LightLevel = Math.Max(0, Math.Min(1, l)); break;
                case "TRG":   it.Trigger = txt.Trim(); break;
                case "AUTO":  it.AutoConnect = txt.Trim().ToUpper().StartsWith("ON"); break;
            }
            FillGrid();
        }

        private void DoTestConnection()
        {
            // Round 13 — 모든 채널 일괄 테스트 + 그리드 색상 표시
            int success = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                bool ok = false;
                try
                {
                    using (var c = new System.Net.Sockets.TcpClient())
                    {
                        var ar = c.BeginConnect(it.Host, it.Port, null, null);
                        if (ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1)) && c.Connected)
                        {
                            c.EndConnect(ar);
                            ok = true;
                        }
                    }
                }
                catch { ok = false; }

                // 그리드 색상 갱신
                if (i < _grid.Rows.Count)
                {
                    var row = _grid.Rows[i];
                    row.Cells["PORT"].Style.BackColor = ok ? Color.FromArgb(0xCC, 0xF2, 0xDD)
                                                          : Color.FromArgb(0xFF, 0xCC, 0xCC);
                    row.Cells["PORT"].Style.ForeColor = ok ? Color.DarkGreen : Color.DarkRed;
                }
                if (ok) success++;
            }
            MessageBox.Show($"TCP 연결 테스트: {success}/{_items.Count} 성공", "Test Connection",
                             MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ApplyToAppSettings()
        {
            try
            {
                var cfg = QMC.CDT320.AppSettingsStore.Current;
                foreach (var it in _items)
                {
                    switch (it.Channel)
                    {
                        case "Wafer":           cfg.VisionWaferPort      = it.Port; cfg.VisionHost = it.Host; break;
                        case "BottomInspection":cfg.VisionInspectionPort = it.Port; break;
                        case "Bin":             cfg.VisionBinPort        = it.Port; break;
                        case "Main":            cfg.VisionMainPort       = it.Port; break;
                        case "FrontSide":         cfg.VisionFrontSidePort    = it.Port; break;
                        case "RearSide":      cfg.VisionRearSidePort = it.Port; break;
                    }
                }
                QMC.CDT320.AppSettingsStore.Save();
                MessageBox.Show("AppSettings 반영 완료. (재시작 후 적용)");
            }
            catch (Exception ex) { MessageBox.Show("실패: " + ex.Message); }
        }
    }
}
