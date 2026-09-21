using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;
using QMC.Common.Data.Store;

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

        private void InitializeLanguageBindings()
        {
            Lang.BindKey(_lightGroup, "settingsUi.caption.lightController");
            Lang.BindKey(_actionGroup, "settingsUi.caption.action");
            Lang.BindKey(btnSave, "settingsUi.caption.save");
            Lang.BindKey(btnReload, "settingsUi.caption.reload");
            Lang.BindKey(btnAllOn, "settingsUi.caption.allOn");
            Lang.BindKey(btnAllOff, "settingsUi.caption.allOff");
            Lang.BindKey(_grid.Columns["CH"], "settingsUi.caption.ch");
            Lang.BindKey(_grid.Columns["NAME"], "settingsUi.caption.name");
            Lang.BindKey(_grid.Columns["COM"], "settingsUi.caption.comPort");
            Lang.BindKey(_grid.Columns["LEVEL"], "settingsUi.caption.level0255");
            Lang.BindKey(_grid.Columns["MODE"], "settingsUi.caption.mode");
            Lang.BindKey(_grid.Columns["COLOR"], "settingsUi.caption.color");
            Lang.BindKey(_grid.Columns["ACTIVE"], "settingsUi.caption.active");
            Load += (sender, args) => Lang.Apply(this);
        }

        public LightControllerPage()
        {
            InitializeComponent();
            ApplyRuntimeUi();
            SettingsPageLayoutStyler.Apply(this);
            ApplyCompactLayout();
            InitializeLanguageBindings();
            _items = LoadOrSeed();
            FillGrid();
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T("set.lightSetup");
            lblHeader.Tag = "i18n:set.lightSetup";
        }

        private void ApplyCompactLayout()
        {
            SettingsPageLayoutStyler.ApplyRoot(rootLayout);
            SettingsPageLayoutStyler.ApplyHeader(lblHeader);

            SettingsPageLayoutStyler.ApplyGroupBox(_lightGroup);
            _lightGroup.Dock = DockStyle.Top;                 // 스타일러가 Fill로 바꾸므로 Top 재지정
            _lightGroup.Padding = new Padding(1, 8, 1, 1);

            SettingsPageLayoutStyler.ApplyGroupBox(_actionGroup);
            SettingsPageLayoutStyler.ApplyActionRow(actionsLayout);
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
                new LightRow { Channel=6, Name="FRONT SIDE VISION", ComPort="COM3", Level=200, Mode="Strobe", Color="White", Active=true },
                new LightRow { Channel=7, Name="REAR SIDE VISION", ComPort="COM3", Level=200, Mode="Strobe", Color="White", Active=true },
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
                    JsonPrettySerializer.WriteObject(fs, typeof(LightStore), new LightStore { Items = _items });
                }
                int sent = SendToControllers();
                QMC.Common.MessageDialog.Show(Lang.Format("settingsUi.light.saved", SavePath, sent, _items.Count));
            }
            catch (Exception ex) { QMC.Common.MessageDialog.Show(Lang.Format("settingsUi.message.saveFailed", ex.Message)); }
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
            AdjustLightGroupHeight();
        }

        private void AdjustLightGroupHeight()
        {
            if (_lightGroup == null)
                return;

            int rowsHeight = 0;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (!row.IsNewRow)
                    rowsHeight += row.Height;
            }

            int headerHeight = _grid.ColumnHeadersVisible ? _grid.ColumnHeadersHeight : 0;
            _lightGroup.Height = Math.Max(150, headerHeight + rowsHeight + 46);
        }

        private void OnCellEdit(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var it = _items[e.RowIndex];
            string col = _grid.Columns[e.ColumnIndex].Name;
            string txt = (_grid.Rows[e.RowIndex].Cells[col].Value as string) ?? "";
            switch (col)
            {
                // 조명 컨트롤러 COM 포트 적용
                case "COM": it.ComPort = txt.Trim(); break;
                // 조명 밝기 레벨 적용
                case "LEVEL": if (int.TryParse(txt, out var v)) it.Level = Math.Max(0, Math.Min(255, v)); break;
                // 조명 모드 적용
                case "MODE": it.Mode = txt.Trim(); break;
                // 조명 색상 적용
                case "COLOR": it.Color = txt.Trim(); break;
                // 조명 활성 여부 적용
                case "ACTIVE": it.Active = txt.Trim().ToUpper().StartsWith("ON"); break;
            }
            FillGrid();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            DoSave();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            _items = LoadOrSeed();
            FillGrid();
        }

        private void btnAllOn_Click(object sender, EventArgs e)
        {
            foreach (var item in _items) item.Active = true;
            FillGrid();
        }

        private void btnAllOff_Click(object sender, EventArgs e)
        {
            foreach (var item in _items) item.Active = false;
            FillGrid();
        }
    }
}
