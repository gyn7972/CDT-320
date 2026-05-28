using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Camera setup page.</summary>
    public partial class CameraSetupPage : PageBase
    {
        public class CameraRow
        {
            [DataMember] public int Index { get; set; }
            [DataMember] public string Channel { get; set; }
            [DataMember] public string Role { get; set; }
            [DataMember] public string Host { get; set; }
            [DataMember] public int Port { get; set; }
            [DataMember] public int Width { get; set; } = 640;
            [DataMember] public int Height { get; set; } = 480;
            [DataMember] public int ExposureMs { get; set; } = 500;
            [DataMember] public double LightLevel { get; set; } = 0.5;
            [DataMember] public string Trigger { get; set; } = "Software";
            [DataMember] public bool AutoConnect { get; set; } = true;
        }

        [DataContract]
        public class CameraStore
        {
            [DataMember] public List<CameraRow> Items { get; set; } = new List<CameraRow>();
        }

        private List<CameraRow> _items;
        private static readonly string SavePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "camera_setup.json");

        public CameraSetupPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            WireEvents();
            _items = LoadOrSeed();
            FillGrid();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.cameraSetup");
            lblHeader.Tag = "i18n:set.cameraSetup";
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
            btnTest.Click += (s, e) => DoTestConnection();
            btnApply.Click += (s, e) => ApplyToAppSettings();
        }

        public static List<CameraRow> SeedDefault()
        {
            return new List<CameraRow>
            {
                new CameraRow { Index=0, Channel="Wafer", Role="WaferAlign", Host="127.0.0.1", Port=5100, ExposureMs=400, LightLevel=0.6, Trigger="Software" },
                new CameraRow { Index=1, Channel="BottomInspection", Role="DiePresence", Host="127.0.0.1", Port=5101, ExposureMs=300, LightLevel=0.7, Trigger="Software" },
                new CameraRow { Index=2, Channel="Bin", Role="PlacementInspector", Host="127.0.0.1", Port=5103, ExposureMs=300, LightLevel=0.5, Trigger="Software" },
                new CameraRow { Index=3, Channel="Main", Role="MainComm", Host="127.0.0.1", Port=5104, ExposureMs=0, LightLevel=0.0, Trigger="None" },
                new CameraRow { Index=4, Channel="TopSide", Role="TopSide4Side", Host="127.0.0.1", Port=5105, ExposureMs=300, LightLevel=0.8, Trigger="Software" },
                new CameraRow { Index=5, Channel="BottomSide", Role="BottomSide4Side", Host="127.0.0.1", Port=5106, ExposureMs=300, LightLevel=0.8, Trigger="Software" },
            };
        }

        private static List<CameraRow> LoadOrSeed()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    using (var fs = File.OpenRead(SavePath))
                    {
                        var ser = new DataContractJsonSerializer(typeof(CameraStore));
                        var store = (CameraStore)ser.ReadObject(fs);
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
                    var ser = new DataContractJsonSerializer(typeof(CameraStore));
                    ser.WriteObject(fs, new CameraStore { Items = _items });
                }
                MessageBox.Show("Save complete.\n" + SavePath);
            }
            catch (Exception ex) { MessageBox.Show("Save failed: " + ex.Message); }
        }

        private void FillGrid()
        {
            _grid.Rows.Clear();
            foreach (var it in _items)
            {
                _grid.Rows.Add(it.Index.ToString(), it.Channel, it.Role, it.Host, it.Port.ToString(),
                    it.Width.ToString(), it.Height.ToString(), it.ExposureMs.ToString(),
                    it.LightLevel.ToString("F2"), it.Trigger, it.AutoConnect ? "ON" : "OFF");
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
                case "HOST": it.Host = txt.Trim(); break;
                case "PORT": if (int.TryParse(txt, out var p)) it.Port = p; break;
                case "W": if (int.TryParse(txt, out var w)) it.Width = w; break;
                case "H": if (int.TryParse(txt, out var h)) it.Height = h; break;
                case "EXP": if (int.TryParse(txt, out var x)) it.ExposureMs = x; break;
                case "LIGHT": if (double.TryParse(txt, out var l)) it.LightLevel = Math.Max(0, Math.Min(1, l)); break;
                case "TRG": it.Trigger = txt.Trim(); break;
                case "AUTO": it.AutoConnect = txt.Trim().ToUpper().StartsWith("ON"); break;
            }
            FillGrid();
        }

        private void DoTestConnection()
        {
            int success = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                bool ok = false;
                try
                {
                    using (var client = new System.Net.Sockets.TcpClient())
                    {
                        var async = client.BeginConnect(it.Host, it.Port, null, null);
                        if (async.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1)) && client.Connected)
                        {
                            client.EndConnect(async);
                            ok = true;
                        }
                    }
                }
                catch { ok = false; }

                if (i < _grid.Rows.Count)
                {
                    var row = _grid.Rows[i];
                    row.Cells["PORT"].Style.BackColor = ok ? Color.FromArgb(204, 242, 221) : Color.FromArgb(255, 204, 204);
                    row.Cells["PORT"].Style.ForeColor = ok ? Color.DarkGreen : Color.DarkRed;
                }
                if (ok) success++;
            }

            MessageBox.Show($"TCP connection test: {success}/{_items.Count}", "Test Connection",
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
                        case "Wafer": cfg.VisionWaferPort = it.Port; cfg.VisionHost = it.Host; break;
                        case "BottomInspection": cfg.VisionInspectionPort = it.Port; break;
                        case "Bin": cfg.VisionBinPort = it.Port; break;
                        case "Main": cfg.VisionMainPort = it.Port; break;
                        case "TopSide": cfg.VisionTopSidePort = it.Port; break;
                        case "BottomSide": cfg.VisionBottomSidePort = it.Port; break;
                    }
                }
                QMC.CDT320.AppSettingsStore.Save();
                MessageBox.Show("AppSettings apply complete. Restart may be required.");
            }
            catch (Exception ex) { MessageBox.Show("Apply failed: " + ex.Message); }
        }
    }
}
