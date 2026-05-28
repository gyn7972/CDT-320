using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>Common editable IO list page.</summary>
    public partial class IoListPage : PageBase
    {
        [DataContract]
        public class IoTable
        {
            [DataMember] public string[] Columns { get; set; }
            [DataMember] public List<string[]> Rows { get; set; } = new List<string[]>();
        }

        private readonly string _i18nKey;
        private readonly string[] _columns;

        private string SavePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "io_" + Sanitize(_i18nKey) + ".json");

        public IoListPage(string i18nKey, string[] columns, string[][] seedRows)
        {
            _i18nKey = i18nKey;
            _columns = columns;

            InitializeComponent();
            ApplyRuntimeUi();
            WireEvents();
            BuildColumns();
            LoadRows(seedRows);
        }

        private void ApplyRuntimeUi()
        {
            lblHeader.Text = Lang.T(_i18nKey);
            lblHeader.Tag = "i18n:" + _i18nKey;
            lblHeader.BackColor = UiTheme.StatusBarBg;
            lblHeader.ForeColor = UiTheme.StatusBarFg;
            lblHeader.Font = UiTheme.SectionFont;

            lblSubHeader.Text = Lang.T(_i18nKey) + " LIST - editable / save to Config";
            lblSubHeader.Tag = "i18n:" + _i18nKey;
            lblSubHeader.BackColor = UiTheme.StatusBarBg;
            lblSubHeader.ForeColor = System.Drawing.Color.White;
            lblSubHeader.Font = UiTheme.SectionFont;
        }

        private void WireEvents()
        {
            btnSave.Click += (s, e) => DoSave();
            btnReload.Click += (s, e) => ReloadFromDisk();
            btnAddRow.Click += (s, e) => AddBlankRow();
        }

        private void BuildColumns()
        {
            _grid.Columns.Clear();
            foreach (var column in _columns)
                _grid.Columns.Add(column, column);
        }

        private void LoadRows(string[][] seedRows)
        {
            _grid.Rows.Clear();
            var loaded = LoadFromDisk();
            if (loaded != null && loaded.Rows.Count > 0)
            {
                foreach (var row in loaded.Rows)
                    _grid.Rows.Add(row);
                return;
            }

            if (seedRows == null) return;
            foreach (var row in seedRows)
                _grid.Rows.Add(row);
        }

        private void ReloadFromDisk()
        {
            var loaded = LoadFromDisk();
            if (loaded == null) return;
            _grid.Rows.Clear();
            foreach (var row in loaded.Rows)
                _grid.Rows.Add(row);
        }

        private void AddBlankRow()
        {
            var blank = new string[_columns.Length];
            for (int i = 0; i < blank.Length; i++)
                blank[i] = string.Empty;
            _grid.Rows.Add(blank);
        }

        private static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        private IoTable LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                using (var fs = File.OpenRead(SavePath))
                {
                    var ser = new DataContractJsonSerializer(typeof(IoTable));
                    return (IoTable)ser.ReadObject(fs);
                }
            }
            catch
            {
                return null;
            }
        }

        private void DoSave()
        {
            try
            {
                var rows = new List<string[]>();
                foreach (DataGridViewRow gridRow in _grid.Rows)
                {
                    if (gridRow.IsNewRow) continue;
                    var row = new string[_columns.Length];
                    for (int i = 0; i < _columns.Length; i++)
                        row[i] = gridRow.Cells[i].Value?.ToString() ?? string.Empty;
                    rows.Add(row);
                }

                var table = new IoTable { Columns = _columns, Rows = rows };
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                using (var fs = File.Create(SavePath))
                {
                    var ser = new DataContractJsonSerializer(typeof(IoTable));
                    ser.WriteObject(fs, table);
                }

                MessageBox.Show("Save complete.\n" + SavePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }
    }
}
