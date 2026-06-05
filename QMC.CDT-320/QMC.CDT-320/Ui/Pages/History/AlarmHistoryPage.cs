using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.Common.Alarms;
using QMC.CDT_320.Ui;

namespace QMC.CDT_320.Ui.Pages.History
{
    /// <summary>
    /// 알람 이력 페이지 — AlarmManager.History + AlarmMaster.Get 으로 정의 매핑.
    /// Stage 20.
    /// </summary>
    public class AlarmHistoryPage : PageBase
    {
        private DataGridView _grid;
        private ComboBox _cbSeverity;
        private TextBox _tbFilter;
        private Label _lblCount;
        private System.Windows.Forms.Timer _refresh;

        public AlarmHistoryPage()
        {
            Controls.Add(CreateSectionHeader("hist.alarm"));
            BuildBody();
            if (!IsDesignerMode())
            {
                AlarmManager.AlarmRaised += OnRaise;
                _refresh = new System.Windows.Forms.Timer { Interval = 2000 };
                _refresh.Tick += (s, e) => LoadGrid();
                _refresh.Start();
                LoadGrid();
            }
        }

        private void BuildBody()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.OptionPanelBg };
            bar.Controls.Add(new Label { Location = new Point(10, 12), AutoSize = true, Text = "Severity:", Font = UiTheme.ButtonFont });
            _cbSeverity = new ComboBox
            {
                Location = new Point(80, 8), Size = new Size(140, 26),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.ValueFont
            };
            _cbSeverity.Items.Add("(All)");
            foreach (var s in Enum.GetNames(typeof(AlarmSeverity))) _cbSeverity.Items.Add(s);
            _cbSeverity.SelectedIndex = 0;
            _cbSeverity.SelectedIndexChanged += (s, e) => LoadGrid();
            bar.Controls.Add(_cbSeverity);

            bar.Controls.Add(new Label { Location = new Point(230, 12), AutoSize = true, Text = "Search:", Font = UiTheme.ButtonFont });
            _tbFilter = new TextBox
            {
                Location = new Point(290, 8), Size = new Size(280, 26),
                Font = UiTheme.ValueFont
            };
            _tbFilter.TextChanged += (s, e) => LoadGrid();
            bar.Controls.Add(_tbFilter);

            _lblCount = new Label
            {
                Location = new Point(580, 12), AutoSize = true,
                Font = UiTheme.ValueFont, Text = "(0)"
            };
            bar.Controls.Add(_lblCount);

            var btnClear = new Button
            {
                Location = new Point(700, 6), Size = new Size(140, 32),
                Text = "Clear active alarms", FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.Accent, ForeColor = Color.White, Font = UiTheme.ButtonFont
            };
            btnClear.Click += (s, e) => { AlarmManager.ClearAll(); LoadGrid(); };
            bar.Controls.Add(btnClear);

            Controls.Add(bar);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = Color.White,
                AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false,
                Font = UiTheme.ValueFont, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(0x50,0x50,0x50), ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
                }
            };
            _grid.Columns.Add("Time",     "Time");
            _grid.Columns.Add("Severity", "Severity");
            _grid.Columns.Add("Code",     "Code");
            _grid.Columns.Add("Source",   "Source");
            _grid.Columns.Add("Message",  "Message");
            _grid.Columns.Add("Cause",    "Cause");
            _grid.Columns.Add("Action",   "Action");
            _grid.Columns["Time"].FillWeight = 14;
            _grid.Columns["Severity"].FillWeight = 8;
            _grid.Columns["Code"].FillWeight = 12;
            _grid.Columns["Source"].FillWeight = 12;
            _grid.Columns["Message"].FillWeight = 18;
            _grid.Columns["Cause"].FillWeight = 18;
            _grid.Columns["Action"].FillWeight = 18;
            Controls.Add(_grid);
            Controls.SetChildIndex(_grid, 0);
        }

        private void LoadGrid()
        {
            try
            {
                _grid.Rows.Clear();
                string filter = (_tbFilter?.Text ?? "").Trim().ToLowerInvariant();
                string sev = _cbSeverity?.SelectedItem?.ToString() ?? "(All)";
                int n = 0;
                foreach (var a in AlarmManager.History.Reverse().Take(500))
                {
                    if (sev != "(All)" && a.Severity.ToString() != sev) continue;
                    if (!string.IsNullOrEmpty(filter)
                        && (a.Code ?? "").ToLowerInvariant().IndexOf(filter) < 0
                        && (a.Source ?? "").ToLowerInvariant().IndexOf(filter) < 0
                        && (a.Message ?? "").ToLowerInvariant().IndexOf(filter) < 0)
                        continue;

                    var def = AlarmMaster.Get(a.Code);
                    string lang = QMC.CDT_320.Ui.Localization.Lang.Current ?? "ko";
                    _grid.Rows.Add(
                        a.Raised.ToString("HH:mm:ss.fff"),
                        a.Severity, a.Code, a.Source ?? "", a.Message ?? "",
                        def?.GetCause(lang) ?? "", def?.GetAction(lang) ?? "");
                    n++;
                    var row = _grid.Rows[_grid.Rows.Count - 1];
                    Color bg;
                    switch (a.Severity)
                    {
                        case AlarmSeverity.Critical: bg = Color.FromArgb(0xFF, 0xC0, 0xC0); break;
                        case AlarmSeverity.Error:    bg = Color.FromArgb(0xFF, 0xDD, 0xDD); break;
                        case AlarmSeverity.Warning:  bg = Color.FromArgb(0xFF, 0xF7, 0xCC); break;
                        default:                     bg = Color.White; break;
                    }
                    row.DefaultCellStyle.BackColor = bg;
                }
                if (_lblCount != null) _lblCount.Text = "(" + n + ")";
            }
            catch { }
        }

        private void OnRaise(AlarmRecord r)
        {
            if (InvokeRequired) { try { BeginInvoke(new Action<AlarmRecord>(OnRaise), r); } catch { } return; }
            LoadGrid();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { AlarmManager.AlarmRaised -= OnRaise; } catch { }
            try { _refresh?.Stop(); _refresh?.Dispose(); } catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
