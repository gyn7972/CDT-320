using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QMC.CDT320.Alarms;

namespace QMC.CDT_320.Ui.Pages.History
{
    public partial class AlarmHistoryPage : PageBase
    {
        private System.Windows.Forms.Timer _refresh;

        public AlarmHistoryPage()
        {
            InitializeComponent();
            WireEvents();

            if (!IsDesignerMode())
            {
                AlarmManager.AlarmRaised += OnRaise;
                _refresh = new System.Windows.Forms.Timer { Interval = 2000 };
                _refresh.Tick += (s, e) => LoadGrid();
                _refresh.Start();
                LoadGrid();
            }
        }

        private void WireEvents()
        {
            _cbSeverity.Items.Add("(All)");
            foreach (var s in Enum.GetNames(typeof(AlarmSeverity))) _cbSeverity.Items.Add(s);
            _cbSeverity.SelectedIndex = 0;
            _cbSeverity.SelectedIndexChanged += (s, e) => LoadGrid();
            _tbFilter.TextChanged += (s, e) => LoadGrid();
            btnClear.Click += (s, e) => { AlarmManager.ClearAll(); LoadGrid(); };
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
                    {
                        continue;
                    }

                    var def = AlarmMaster.Get(a.Code);
                    string lang = Localization.Lang.Current ?? "ko";
                    _grid.Rows.Add(
                        a.Raised.ToString("HH:mm:ss.fff"),
                        a.Severity,
                        a.Code,
                        a.Source ?? "",
                        a.Message ?? "",
                        def?.GetCause(lang) ?? "",
                        def?.GetAction(lang) ?? "");
                    n++;

                    var row = _grid.Rows[_grid.Rows.Count - 1];
                    Color bg;
                    switch (a.Severity)
                    {
                        case AlarmSeverity.Critical: bg = Color.FromArgb(0xFF, 0xC0, 0xC0); break;
                        case AlarmSeverity.Error: bg = Color.FromArgb(0xFF, 0xDD, 0xDD); break;
                        case AlarmSeverity.Warning: bg = Color.FromArgb(0xFF, 0xF7, 0xCC); break;
                        default: bg = Color.White; break;
                    }
                    row.DefaultCellStyle.BackColor = bg;
                }

                if (_lblCount != null) _lblCount.Text = "(" + n + ")";
            }
            catch
            {
            }
        }

        private void OnRaise(AlarmRecord r)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<AlarmRecord>(OnRaise), r); } catch { }
                return;
            }

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
