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
    /// <summary>Light controller setup page.</summary>
    public partial class LightControllerPage : PageBase
    {
        public class LightRow
        {
            [DataMember] public int Channel { get; set; }
            [DataMember] public string Name { get; set; }
            [DataMember] public string ComPort { get; set; }
            [DataMember] public int Level { get; set; }
            [DataMember] public string Mode { get; set; } = "Continuous";
            [DataMember] public bool Active { get; set; } = true;
            [DataMember] public string Color { get; set; } = "White";
        }

        [DataContract]
        public class LightStore
        {
            [DataMember] public List<LightRow> Items { get; set; } = new List<LightRow>();
        }

        private List<LightRow> _items;
        private static readonly string SavePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "light_setup.json");

        public LightControllerPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            WireEvents();
            _items = LoadOrSeed();
            FillGrid();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.lightSetup");
            lblHeader.Tag = "i18n:set.lightSetup";
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;

            lblSubHeader.BackColor = UiTheme.StatusBarBg;
            lblSubHeader.ForeColor = Color.White;
            lblSubHeader.Font = UiTheme.SectionFont;
        }

        private void WireEvents()
        {
            _grid.CellEndEdit += OnCellEdit;
            btnSave.Click += (s, e) => DoSave();
            btnReload.Click += (s, e) =>
            {
                _items = LoadOrSeed();
                FillGrid();
            };
            btnAllOn.Click += (s, e) =>
            {
                foreach (var item in _items) item.Active = true;
                FillGrid();
            };
            btnAllOff.Click += (s, e) =>
            {
                foreach (var item in _items) item.Active = false;
                FillGrid();
            };
        }

        public static List<LightRow> SeedDefault()
        {
            return new List<LightRow>
            {
                new LightRow { Channel=1, Name="STAGE RING (Wafer Align)", ComPort="COM1", Level=128, Mode="Continuous", Color="White", Active=true },
                new LightRow { Channel=2, Name="BOTTOM VISION", ComPort="COM2", Level=180, Mode="Continuous", Color="White", Active=true },
                new LightRow { Channel=3, Name="SIDE VISION 1", ComPort="COM2", Level=200, Mode="Strobe", Color="White", Active=true },
                new LightRow { Channel=4, Name="SIDE VISION 2", ComPort="COM2", Level=200, Mode="Strobe", Color="White", Active=true },
                new LightRow { Channel=5, Name="BIN VISION", ComPort="COM3", Level=140, Mode="Continuous", Color="White", Active=true },
                new LightRow { Channel=6, Name="TOP SIDE VISION", ComPort="COM3", Level=200, Mode="Strobe", Color="White", Active=true },
                new LightRow { Channel=7, Name="BOTTOM SIDE VISION", ComPort="COM3", Level=200, Mode="Strobe", Color="White", Active=true },
                new LightRow { Channel=8, Name="ALIGN MARK ILLUM", ComPort="COM1", Level=100, Mode="Continuous", Color="Red", Active=false },
            };
        }

        private static List<LightRow> LoadOrSeed()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    using (var fs = File.OpenRead(SavePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(LightStore));
                        var store = (LightStore)ser.ReadObject(fs);
                        if (store?.Items != null && store.Items.Count > 0) return store.Items;
                    }
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
                int sent = SendToControllers();
                MessageBox.Show($"Save complete.\n{SavePath}\n\nLight command sent: {sent}/{_items.Count}");
            }
            catch (Exception ex) { MessageBox.Show("Save failed: " + ex.Message); }
        }

        private int SendToControllers()
        {
            int success = 0;
            foreach (var group in _items.GroupBy(x => x.ComPort))
            {
                System.IO.Ports.SerialPort port = null;
                try
                {
                    port = new System.IO.Ports.SerialPort(group.Key, 9600,
                        System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                    port.WriteTimeout = 500;
                    try { port.Open(); }
                    catch
                    {
                        foreach (var item in group)
                            Console.WriteLine($"[LIGHT-SIM] {item.ComPort} CH{item.Channel} LEVEL={(item.Active ? item.Level : 0)} ({item.Mode})");
                        continue;
                    }

                    foreach (var item in group)
                    {
                        int level = item.Active ? item.Level : 0;
                        string command = $"$LCH{item.Channel},{level}\r\n";
                        try { port.Write(command); success++; }
                        catch { }
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
                int rowIndex = _grid.Rows.Add("CH" + it.Channel, it.Name, it.ComPort,
                    it.Level.ToString(), it.Mode, it.Color, it.Active ? "ON" : "OFF");
                if (it.Active)
                    _grid.Rows[rowIndex].Cells["LEVEL"].Style.BackColor = Color.FromArgb(255, 246, 204);
                else
                    _grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
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
                case "COM": it.ComPort = txt.Trim(); break;
                case "LEVEL": if (int.TryParse(txt, out var v)) it.Level = Math.Max(0, Math.Min(255, v)); break;
                case "MODE": it.Mode = txt.Trim(); break;
                case "COLOR": it.Color = txt.Trim(); break;
                case "ACTIVE": it.Active = txt.Trim().ToUpper().StartsWith("ON"); break;
            }
            FillGrid();
        }
    }
}
