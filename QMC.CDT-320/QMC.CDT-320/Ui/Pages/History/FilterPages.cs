using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    /// <summary>이력 - 알람/경고/데이터/작업 공용 필터 테이블 페이지.</summary>
    public class FilterGridPage : PageBase
    {
        private readonly string _kind;
        private readonly DataGridView _grid;

        public FilterGridPage(string titleI18n, string kind, string[][] seed = null)
        {
            _kind = kind;
            Controls.Add(CreateSectionHeader(titleI18n));

            // 상단 필터 바
            var filter = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UiTheme.OptionPanelBg };
            filter.Controls.Add(new Label { Location = new Point(12, 16),  AutoSize = true, Text = "기간",    Font = UiTheme.ButtonFont });
            filter.Controls.Add(new DateTimePicker { Location = new Point(56, 12), Size = new Size(160, 24), Format = DateTimePickerFormat.Short });
            filter.Controls.Add(new Label { Location = new Point(220, 16), AutoSize = true, Text = " ~ ",     Font = UiTheme.ButtonFont });
            filter.Controls.Add(new DateTimePicker { Location = new Point(244, 12),Size = new Size(160, 24), Format = DateTimePickerFormat.Short });
            filter.Controls.Add(new Label { Location = new Point(420, 16), AutoSize = true, Text = "코드",    Font = UiTheme.ButtonFont });
            filter.Controls.Add(new TextBox { Location = new Point(456, 12), Size = new Size(100, 24), Font = UiTheme.ValueFont });
            filter.Controls.Add(new Label { Location = new Point(568, 16), AutoSize = true, Text = "사용자",  Font = UiTheme.ButtonFont });
            filter.Controls.Add(new TextBox { Location = new Point(620, 12), Size = new Size(140, 24), Font = UiTheme.ValueFont });
            filter.Controls.Add(new Button  { Location = new Point(776, 10), Size = new Size(100, 28), Text = "검색",   FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont });
            filter.Controls.Add(new Button  { Location = new Point(884, 10), Size = new Size(100, 28), Text = "CSV",    FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont });
            filter.Controls.Add(new Button  { Location = new Point(992, 10), Size = new Size(100, 28), Text = "전체",    FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont });
            Controls.Add(filter);
            Controls.SetChildIndex(filter, 0);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, Font = new Font("맑은 고딕", 9F),
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50,0x50,0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                },
                RowTemplate = { Height = 26 }
            };
            foreach (var c in new[] { "INDEX", "DATE", "USER", "CODE", "DESCRIPTION" })
                _grid.Columns.Add(c, c);

            Seed(seed);
            Controls.Add(_grid);
            Controls.SetChildIndex(_grid, 0);
        }

        private void Seed(string[][] rows)
        {
            if (rows != null) { foreach (var r in rows) _grid.Rows.Add(r); return; }
            var now = DateTime.Now;
            for (int i = 0; i < 6; i++)
                _grid.Rows.Add((i + 1).ToString(), now.AddMinutes(-i).ToString("yyyy-MM-dd HH:mm:ss"),
                               "QMC", "8" + (1000 + i), _kind + " sample message " + i);
        }
    }
}
