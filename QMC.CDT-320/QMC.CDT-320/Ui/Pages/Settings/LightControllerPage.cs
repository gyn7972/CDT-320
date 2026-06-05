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
    /// Stage 59 — Light Controller Setup 페이지.
    /// 메뉴얼 기준 광원 채널 — Stage Ring / Bottom Vision / Side Vision / Bin Vision / Top Side / Bottom Side.
    /// 채널별 강도(0~255), Mode(Continuous/Strobe), Active 여부, COM 포트 설정.
    /// </summary>
    public class LightControllerPage : PageBase
    {
        public class LightRow
        {
            [DataMember] public int    Channel    { get; set; }   // 1~8
            [DataMember] public string Name       { get; set; }
            [DataMember] public string ComPort    { get; set; }   // COM1, COM2 ...
            [DataMember] public int    Level      { get; set; }   // 0~255
            [DataMember] public string Mode       { get; set; } = "Continuous"; // Strobe
            [DataMember] public bool   Active     { get; set; } = true;
            [DataMember] public string Color      { get; set; } = "White";
        }

        [DataContract]
        public class LightStore
        {
            [DataMember] public List<LightRow> Items { get; set; } = new List<LightRow>();
        }

        private DataGridView _grid;
        private List<LightRow> _items;
        private static readonly string SavePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "light_setup.json");

        public LightControllerPage()
        {
            Controls.Add(CreateSectionHeader("set.lightSetup"));
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
                Text = "LIGHT CONTROLLER — RS-232C 채널별 강도/모드 (Stage Ring / Bottom / Side / Bin)",
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
        }

        private void BuildGrid()
        {
            _grid = new DataGridView
            {
                Location = new Point(8, 66), Size = new Size(1400, 500),
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
            _grid.Columns.Add("CH",     "CH");
            _grid.Columns.Add("NAME",   "NAME");
            _grid.Columns.Add("COM",    "COM PORT");
            _grid.Columns.Add("LEVEL",  "LEVEL (0~255)");
            _grid.Columns.Add("MODE",   "MODE");
            _grid.Columns.Add("COLOR",  "COLOR");
            _grid.Columns.Add("ACTIVE", "ACTIVE");

            _grid.Columns["CH"]   .ReadOnly = true;
            _grid.Columns["NAME"] .ReadOnly = true;
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
            var allOn = new Controls.ActionButton { Text = "ALL ON", Size = new Size(120, 44), Margin = new Padding(4) };
            allOn.Click += (s, e) => { foreach (var x in _items) x.Active = true; FillGrid(); };
            var allOff = new Controls.ActionButton { Text = "ALL OFF", Size = new Size(120, 44), Margin = new Padding(4) };
            allOff.Click += (s, e) => { foreach (var x in _items) x.Active = false; FillGrid(); };
            actions.Controls.Add(save);
            actions.Controls.Add(reload);
            actions.Controls.Add(allOn);
            actions.Controls.Add(allOff);
            Controls.Add(actions);
        }

        public static List<LightRow> SeedDefault()
        {
            return new List<LightRow>
            {
                new LightRow { Channel=1, Name="STAGE RING (Wafer Align)", ComPort="COM1", Level=128, Mode="Continuous", Color="White",  Active=true },
                new LightRow { Channel=2, Name="BOTTOM VISION",            ComPort="COM2", Level=180, Mode="Continuous", Color="White",  Active=true },
                new LightRow { Channel=3, Name="SIDE VISION 1",            ComPort="COM2", Level=200, Mode="Strobe",     Color="White",  Active=true },
                new LightRow { Channel=4, Name="SIDE VISION 2",            ComPort="COM2", Level=200, Mode="Strobe",     Color="White",  Active=true },
                new LightRow { Channel=5, Name="BIN VISION",               ComPort="COM3", Level=140, Mode="Continuous", Color="White",  Active=true },
                new LightRow { Channel=6, Name="TOP SIDE VISION",          ComPort="COM3", Level=200, Mode="Strobe",     Color="White",  Active=true },
                new LightRow { Channel=7, Name="BOTTOM SIDE VISION",       ComPort="COM3", Level=200, Mode="Strobe",     Color="White",  Active=true },
                new LightRow { Channel=8, Name="ALIGN MARK ILLUM",         ComPort="COM1", Level=100, Mode="Continuous", Color="Red",    Active=false },
            };
        }

        private static List<LightRow> LoadOrSeed()
        {
            try
            {
                if (File.Exists(SavePath))
                    using (var fs = File.OpenRead(SavePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(LightStore));
                        var s = (LightStore)ser.ReadObject(fs);
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
                    var ser = new DataContractJsonSerializer(typeof(LightStore));
                    ser.WriteObject(fs, new LightStore { Items = _items });
                }
                // Round 14 — 저장과 동시에 실 RS-232C 컨트롤러로 명령 전송 시도 (best-effort)
                int sent = SendToControllers();
                MessageBox.Show($"저장 완료.\n{SavePath}\n\n조명 컨트롤러 명령 전송: {sent}/{_items.Count}");
            }
            catch (Exception ex) { MessageBox.Show("실패: " + ex.Message); }
        }

        /// <summary>
        /// Round 14 — RS-232C 컨트롤러로 채널별 강도 명령 전송.
        /// 실 시리얼 포트가 없으면 시뮬 모드로 콘솔에만 기록.
        /// 명령 형식 (메뉴얼 표준): "$LCH&lt;CH&gt;,&lt;LEVEL&gt;\r\n"
        /// </summary>
        private int SendToControllers()
        {
            int success = 0;
            // COM 포트 별로 그룹화
            var byPort = _items.GroupBy(x => x.ComPort);
            foreach (var grp in byPort)
            {
                System.IO.Ports.SerialPort port = null;
                try
                {
                    port = new System.IO.Ports.SerialPort(grp.Key, 9600,
                        System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    port.WriteTimeout = 500;
                    try { port.Open(); }
                    catch
                    {
                        // 포트 없음 (Sim 모드) — 콘솔에만 기록
                        foreach (var it in grp)
                            Console.WriteLine($"[LIGHT-SIM] {it.ComPort} CH{it.Channel} LEVEL={(it.Active ? it.Level : 0)} ({it.Mode})");
                        continue;
                    }
                    foreach (var it in grp)
                    {
                        int level = it.Active ? it.Level : 0;
                        string cmd = $"$LCH{it.Channel},{level}\r\n";
                        try { port.Write(cmd); success++; }
                        catch { /* timeout — skip */ }
                    }
                }
                catch { }
                finally
                {
                    try { port?.Close(); port?.Dispose(); } catch { }
                }
            }
            return success;
        }

        private void FillGrid()
        {
            _grid.Rows.Clear();
            foreach (var it in _items)
            {
                int idx = _grid.Rows.Add(
                    "CH" + it.Channel, it.Name, it.ComPort,
                    it.Level.ToString(), it.Mode, it.Color,
                    it.Active ? "ON" : "OFF");
                if (it.Active)
                {
                    _grid.Rows[idx].Cells["LEVEL"].Style.BackColor = Color.FromArgb(0xFF, 0xF6, 0xCC);
                }
                else
                {
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.Gray;
                }
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
                case "COM":    it.ComPort = txt.Trim(); break;
                case "LEVEL":  if (int.TryParse(txt, out var v)) it.Level = Math.Max(0, Math.Min(255, v)); break;
                case "MODE":   it.Mode = txt.Trim(); break;
                case "COLOR":  it.Color = txt.Trim(); break;
                case "ACTIVE": it.Active = txt.Trim().ToUpper().StartsWith("ON"); break;
            }
            FillGrid();
        }
    }
}
