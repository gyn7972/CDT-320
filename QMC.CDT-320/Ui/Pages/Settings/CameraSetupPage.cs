using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;
using QMC.Common.Data.Store;

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
        private GroupBox _cameraGroup;
        private GroupBox _actionGroup;

        public CameraSetupPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
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

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);
            lblSubHeader.Visible = false;

            if (_cameraGroup == null)
                _cameraGroup = new GroupBox();
            _cameraGroup.Text = "CAMERA SETUP";
            SettingsPageLayoutStyler.ApplyGroupBox(_cameraGroup);
            _cameraGroup.Dock = DockStyle.Top;
            _cameraGroup.Height = 200;
            _cameraGroup.Padding = new Padding(1, 8, 1, 1);
            _grid.Dock = DockStyle.Fill;
            _grid.Margin = Padding.Empty;
            if (_grid.Parent != _cameraGroup)
            {
                if (_grid.Parent != null)
                    _grid.Parent.Controls.Remove(_grid);
                _cameraGroup.Controls.Add(_grid);
            }

            if (_actionGroup == null)
                _actionGroup = new GroupBox();
            _actionGroup.Text = "ACTION";
            SettingsPageLayoutStyler.ApplyGroupBox(_actionGroup);
            if (actionsLayout.Parent != _actionGroup)
            {
                if (actionsLayout.Parent != null)
                    actionsLayout.Parent.Controls.Remove(actionsLayout);
                _actionGroup.Controls.Add(actionsLayout);
            }

            actionsLayout.Margin = Padding.Empty;
            actionsLayout.Padding = Padding.Empty;
            actionsLayout.ColumnStyles.Clear();
            actionsLayout.ColumnCount = 14;
            for (int i = 0; i < 14; i++)
                actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 14F));
            SettingsPageLayoutStyler.ApplyActionRow(actionsLayout);

            rootLayout.Controls.Clear();
            rootLayout.ColumnStyles.Clear();
            rootLayout.RowStyles.Clear();
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 4;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            rootLayout.Controls.Add(lblHeader, 0, 0);
            rootLayout.Controls.Add(_cameraGroup, 0, 1);
            rootLayout.Controls.Add(_actionGroup, 0, 3);

            if (rootLayout.RowStyles.Count >= 4)
            {
                rootLayout.RowStyles[0].SizeType = SizeType.Absolute;
                rootLayout.RowStyles[0].Height = 30F;
                rootLayout.RowStyles[1].SizeType = SizeType.Percent;
                rootLayout.RowStyles[1].Height = 45F;
                rootLayout.RowStyles[2].SizeType = SizeType.Percent;
                rootLayout.RowStyles[2].Height = 45F;
                rootLayout.RowStyles[3].SizeType = SizeType.Percent;
                rootLayout.RowStyles[3].Height = 10F;
            }
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
                new CameraRow { Index=4, Channel="FrontSide", Role="FrontSide4Side", Host="127.0.0.1", Port=5105, ExposureMs=300, LightLevel=0.8, Trigger="Software" },
                new CameraRow { Index=5, Channel="RearSide", Role="RearSide4Side", Host="127.0.0.1", Port=5106, ExposureMs=300, LightLevel=0.8, Trigger="Software" },
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
                    JsonPrettySerializer.WriteObject(fs, typeof(CameraStore), new CameraStore { Items = _items });
                }
                QMC.Common.MessageDialog.Show("Save complete.\n" + SavePath);
            }
            catch (Exception ex) { QMC.Common.MessageDialog.Show("Save failed: " + ex.Message); }
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
            AdjustCameraGroupHeight();
        }

        private void AdjustCameraGroupHeight()
        {
            if (_cameraGroup == null)
                return;

            int rowsHeight = 0;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (!row.IsNewRow)
                    rowsHeight += row.Height;
            }

            int headerHeight = _grid.ColumnHeadersVisible ? _grid.ColumnHeadersHeight : 0;
            _cameraGroup.Height = Math.Max(150, headerHeight + rowsHeight + 46);
        }

        private void OnCellEdit(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var it = _items[e.RowIndex];
            string col = _grid.Columns[e.ColumnIndex].Name;
            string txt = (_grid.Rows[e.RowIndex].Cells[col].Value as string) ?? "";
            switch (col)
            {
                // 카메라 Host 설정 적용
                case "HOST": it.Host = txt.Trim(); break;
                // 카메라 Port 설정 적용
                case "PORT": if (int.TryParse(txt, out var p)) it.Port = p; break;
                // 카메라 Width 설정 적용
                case "W": if (int.TryParse(txt, out var w)) it.Width = w; break;
                // 카메라 Height 설정 적용
                case "H": if (int.TryParse(txt, out var h)) it.Height = h; break;
                // 카메라 Exposure 설정 적용
                case "EXP": if (int.TryParse(txt, out var x)) it.ExposureMs = x; break;
                // 카메라 Light Level 설정 적용
                case "LIGHT": if (double.TryParse(txt, out var l)) it.LightLevel = Math.Max(0, Math.Min(1, l)); break;
                // 카메라 Trigger 설정 적용
                case "TRG": it.Trigger = txt.Trim(); break;
                // 카메라 Auto Connect 설정 적용
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

            QMC.Common.MessageDialog.Show($"TCP connection test: {success}/{_items.Count}", "Test Connection",
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
                        // Wafer 카메라 AppSettings 적용
                        case "Wafer": cfg.VisionWaferPort = it.Port; cfg.VisionHost = it.Host; break;
                        // Bottom Inspection 카메라 AppSettings 적용
                        case "BottomInspection": cfg.VisionInspectionPort = it.Port; break;
                        // Bin 카메라 AppSettings 적용
                        case "Bin": cfg.VisionBinPort = it.Port; break;
                        // Main 카메라 AppSettings 적용
                        case "Main": cfg.VisionMainPort = it.Port; break;
                        // Front Side 카메라 AppSettings 적용
                        case "FrontSide": cfg.VisionFrontSidePort = it.Port; break;
                        // Rear Side 카메라 AppSettings 적용
                        case "RearSide": cfg.VisionRearSidePort = it.Port; break;
                    }
                }
                QMC.CDT320.AppSettingsStore.Save();
                QMC.Common.MessageDialog.Show("AppSettings apply complete. Restart may be required.");
            }
            catch (Exception ex) { QMC.Common.MessageDialog.Show("Apply failed: " + ex.Message); }
        }
    }
}

