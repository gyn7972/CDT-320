using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Logging;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    /// <summary>
    /// 이력 — 이벤트 로그. 오늘 날짜의 EventLogger 파일을 로드하여 표시.
    /// 상단 날짜 선택 + 새로고침.
    /// </summary>
    public class EventLogPage : PageBase
    {
        private DataGridView   _grid;
        private DateTimePicker _dp;

        public EventLogPage()
        {
            Controls.Add(CreateSectionHeader("hist.event"));
            BuildFilter();
            BuildGrid();
            EventLogger.EventLogged += OnLiveEvent;
            Disposed += (s, e) => EventLogger.EventLogged -= OnLiveEvent;
            Load += (s, e) => ReloadDay(DateTime.Today);
        }

        private void BuildFilter()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.OptionPanelBg };
            bar.Controls.Add(new Label { Location = new Point(10, 12), AutoSize = true, Text = "DATE", Font = UiTheme.ButtonFont });
            _dp = new DateTimePicker { Location = new Point(56, 8), Size = new Size(160, 24), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            _dp.ValueChanged += (s, e) => ReloadDay(_dp.Value.Date);
            bar.Controls.Add(_dp);

            var btnR = new Button { Location = new Point(226, 6), Size = new Size(100, 28), Text = "REFRESH", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            btnR.Click += (s, e) => ReloadDay(_dp.Value.Date);
            bar.Controls.Add(btnR);

            var btnOpen = new Button { Location = new Point(336, 6), Size = new Size(120, 28), Text = "OPEN FOLDER", FlatStyle = FlatStyle.Flat, Font = UiTheme.ButtonFont };
            btnOpen.Click += (s, e) => { try { System.Diagnostics.Process.Start(EventLogger.LogDir); } catch { } };
            bar.Controls.Add(btnOpen);

            Controls.Add(bar);
            Controls.SetChildIndex(bar, 0);
        }

        private void BuildGrid()
        {
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
                    BackColor = Color.FromArgb(0x50, 0x50, 0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                },
                RowTemplate = { Height = 26 }
            };
            _grid.Columns.Add("WHEN",  "DATE");
            _grid.Columns.Add("KIND",  "KIND");
            _grid.Columns.Add("USER",  "USER");
            _grid.Columns.Add("CODE",  "CODE");
            _grid.Columns.Add("DESC",  "DESCRIPTION");
            Controls.Add(_grid);
            Controls.SetChildIndex(_grid, 0);
        }

        private void ReloadDay(DateTime date)
        {
            _grid.Rows.Clear();
            foreach (var r in EventLogger.Read(date))
                AppendRow(r);
        }

        private void OnLiveEvent(EventRow r)
        {
            if (InvokeRequired) { BeginInvoke(new Action<EventRow>(OnLiveEvent), r); return; }
            if (_dp != null && _dp.Value.Date == DateTime.Today) AppendRow(r);
        }

        private void AppendRow(EventRow r)
        {
            int idx = _grid.Rows.Add(
                r.When.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                r.Kind.ToString(),
                r.User ?? "",
                r.Code ?? "",
                r.Description ?? "");
            // 색상
            var row = _grid.Rows[idx];
            switch (r.Kind)
            {
                case EventKind.Alarm:   row.DefaultCellStyle.ForeColor = Color.IndianRed;  row.DefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold); break;
                case EventKind.Warning: row.DefaultCellStyle.ForeColor = Color.DarkOrange; break;
                case EventKind.Data:    row.DefaultCellStyle.ForeColor = Color.SteelBlue;  break;
                case EventKind.Work:    row.DefaultCellStyle.ForeColor = Color.Teal;       break;
            }
        }
    }
}
