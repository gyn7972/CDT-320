using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Pages.Settings
{
    /// <summary>
    /// 알람 마스터 — 코드 → 정의 (Title/Cause/Action) 조회 + 편집.
    /// Stage 19.
    /// </summary>
    public class AlarmMasterPage : PageBase
    {
        private DataGridView _grid;
        private TextBox _tbFilter;
        private ComboBox _cbCategory;
        private Label _lblCount;

        public AlarmMasterPage()
        {
            Controls.Add(CreateSectionHeader("settings.alarmMaster"));
            BuildBody();
            if (!IsDesignerMode()) LoadGrid();
        }

        private void BuildBody()
        {
            // 필터바
            var bar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.OptionPanelBg };
            bar.Controls.Add(new Label { Location = new Point(10, 12), AutoSize = true, Text = "Search:", Font = UiTheme.ButtonFont });
            _tbFilter = new TextBox
            {
                Location = new Point(70, 8), Size = new Size(280, 26),
                Font = UiTheme.ValueFont
            };
            _tbFilter.TextChanged += (s, e) => LoadGrid();
            bar.Controls.Add(_tbFilter);

            bar.Controls.Add(new Label { Location = new Point(360, 12), AutoSize = true, Text = "Category:", Font = UiTheme.ButtonFont });
            _cbCategory = new ComboBox
            {
                Location = new Point(430, 8), Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont
            };
            _cbCategory.Items.Add("(All)");
            foreach (var c in Enum.GetNames(typeof(AlarmCategory))) _cbCategory.Items.Add(c);
            _cbCategory.SelectedIndex = 0;
            _cbCategory.SelectedIndexChanged += (s, e) => LoadGrid();
            bar.Controls.Add(_cbCategory);

            _lblCount = new Label
            {
                Location = new Point(600, 12), AutoSize = true,
                Font = UiTheme.ValueFont, Text = "(0)"
            };
            bar.Controls.Add(_lblCount);

            var btnReload = new Button
            {
                Location = new Point(720, 6), Size = new Size(100, 32),
                Text = "Reload JSON", FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = UiTheme.ButtonFont
            };
            btnReload.Click += (s, e) => { AlarmMaster.Load(); LoadGrid(); };
            bar.Controls.Add(btnReload);

            var btnSave = new Button
            {
                Location = new Point(826, 6), Size = new Size(120, 32),
                Text = "SAVE", FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Accent,
                ForeColor = Color.White, Font = UiTheme.ButtonFont
            };
            btnSave.Click += (s, e) => { AlarmMaster.Save();
                MessageBox.Show("Saved: " + AlarmMaster.Path_, "AlarmMaster",
                                MessageBoxButtons.OK, MessageBoxIcon.Information); };
            bar.Controls.Add(btnSave);

            Controls.Add(bar);

            // Grid
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = Color.White,
                AllowUserToAddRows = false, RowHeadersVisible = false,
                Font = UiTheme.ValueFont, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50,0x50,0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                }
            };
            _grid.Columns.Add("Code", "Code");
            _grid.Columns.Add("Category", "Category");
            _grid.Columns.Add("Severity", "Severity");
            _grid.Columns.Add("Title", "Title");
            _grid.Columns.Add("Cause", "Cause");
            _grid.Columns.Add("Action", "Action");
            _grid.Columns["Code"].FillWeight = 12;
            _grid.Columns["Category"].FillWeight = 12;
            _grid.Columns["Severity"].FillWeight = 10;
            _grid.Columns["Title"].FillWeight = 22;
            _grid.Columns["Cause"].FillWeight = 22;
            _grid.Columns["Action"].FillWeight = 22;
            _grid.CellEndEdit += (s, e) => CommitRow(e.RowIndex);
            Controls.Add(_grid);
            Controls.SetChildIndex(_grid, 0);
        }

        private void LoadGrid()
        {
            _grid.Rows.Clear();
            string filter = (_tbFilter?.Text ?? "").Trim().ToLowerInvariant();
            string cat = _cbCategory?.SelectedItem?.ToString() ?? "(All)";
            int n = 0;
            foreach (var d in AlarmMaster.ByCode.Values.OrderBy(x => x.Code))
            {
                if (cat != "(All)" && d.Category.ToString() != cat) continue;
                if (!string.IsNullOrEmpty(filter)
                    && (d.Code ?? "").ToLowerInvariant().IndexOf(filter) < 0
                    && (d.Title ?? "").ToLowerInvariant().IndexOf(filter) < 0)
                    continue;
                _grid.Rows.Add(d.Code, d.Category, d.DefaultSeverity, d.Title, d.Cause, d.Action);
                n++;
            }
            if (_lblCount != null) _lblCount.Text = "(" + n + ")";
        }

        private void CommitRow(int rowIdx)
        {
            if (rowIdx < 0 || rowIdx >= _grid.Rows.Count) return;
            var row = _grid.Rows[rowIdx];
            string code = row.Cells[0].Value as string;
            if (string.IsNullOrEmpty(code)) return;
            var def = AlarmMaster.Get(code);
            if (def == null) return;
            try
            {
                if (Enum.TryParse<AlarmCategory>(row.Cells[1].Value?.ToString(), out var cat)) def.Category = cat;
                if (Enum.TryParse<AlarmSeverity>(row.Cells[2].Value?.ToString(), out var sev)) def.DefaultSeverity = sev;
                def.Title  = row.Cells[3].Value as string ?? def.Title;
                def.Cause  = row.Cells[4].Value as string ?? def.Cause;
                def.Action = row.Cells[5].Value as string ?? def.Action;
            }
            catch { }
        }
    }
}
