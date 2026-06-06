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
    /// DIGITAL / DIGITAL LINK / CYLINDER / LAMP / SWITCH / LIGHT SOURCE 공용 리스트 페이지.
    /// Stage 59 — 편집 + JSON 영속화 추가 (i18nKey 별 별도 파일).
    /// </summary>
    public class IoListPage : PageBase
    {
        [DataContract]
        public class IoTable
        {
            [DataMember] public string[]   Columns { get; set; }
            [DataMember] public List<string[]> Rows { get; set; } = new List<string[]>();
        }

        private readonly string _i18nKey;
        private readonly string[] _columns;
        private DataGridView _grid;

        private string SavePath
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config",
                            "io_" + Sanitize(_i18nKey) + ".json");

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }

        public IoListPage(string i18nKey, string[] columns, string[][] seedRows)
        {
            _i18nKey = i18nKey;
            _columns = columns;

            Controls.Add(CreateSectionHeader(i18nKey));

            var hdr = new Label
            {
                Location = new Point(8, 36), Size = new Size(1400, 26),
                Text = Lang.T(i18nKey) + " LIST  —  편집 + SAVE 가능 (Config/io_*.json)",
                Tag = "i18n:" + i18nKey,
                BackColor = UiTheme.StatusBarBg, ForeColor = Color.White,
                Font = UiTheme.SectionFont, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            Controls.Add(hdr);

            _grid = new DataGridView
            {
                Location             = new Point(8, 66),
                Size                 = new Size(1400, 660),
                ReadOnly             = false,                  // ← Stage 59: 편집 가능
                AllowUserToAddRows   = true,
                AllowUserToDeleteRows= true,
                RowHeadersVisible    = false,
                MultiSelect          = false,
                SelectionMode        = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode  = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor      = Color.White,
                Font                 = new Font("맑은 고딕", 9F),
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
            foreach (var c in columns) _grid.Columns.Add(c, c);

            // 영속화된 데이터 로드 시도 — 없으면 seed
            var loaded = LoadFromDisk();
            if (loaded != null && loaded.Rows.Count > 0)
            {
                foreach (var row in loaded.Rows) _grid.Rows.Add(row);
            }
            else if (seedRows != null)
            {
                foreach (var row in seedRows) _grid.Rows.Add(row);
            }
            Controls.Add(_grid);

            BuildActions();
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
            reload.Click += (s, e) =>
            {
                var l = LoadFromDisk();
                if (l != null) { _grid.Rows.Clear(); foreach (var r in l.Rows) _grid.Rows.Add(r); }
            };
            var addRow = new Controls.ActionButton { Text = "ADD ROW", Size = new Size(120, 44), Margin = new Padding(4) };
            addRow.Click += (s, e) =>
            {
                var blank = new string[_columns.Length];
                for (int i = 0; i < blank.Length; i++) blank[i] = "";
                _grid.Rows.Add(blank);
            };
            actions.Controls.Add(save);
            actions.Controls.Add(reload);
            actions.Controls.Add(addRow);
            Controls.Add(actions);
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
            catch { return null; }
        }

        private void DoSave()
        {
            try
            {
                var rows = new List<string[]>();
                foreach (DataGridViewRow r in _grid.Rows)
                {
                    if (r.IsNewRow) continue;
                    var row = new string[_columns.Length];
                    for (int i = 0; i < _columns.Length; i++)
                        row[i] = r.Cells[i].Value?.ToString() ?? "";
                    rows.Add(row);
                }
                var t = new IoTable { Columns = _columns, Rows = rows };
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                using (var fs = File.Create(SavePath))
                {
                    var ser = new DataContractJsonSerializer(typeof(IoTable));
                    ser.WriteObject(fs, t);
                }
                MessageBox.Show("저장 완료.\n" + SavePath);
            }
            catch (Exception ex) { MessageBox.Show("실패: " + ex.Message); }
        }
    }
}
