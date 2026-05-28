using System;
using System.Drawing;
using QMC.CDT320.Logging;

namespace QMC.CDT_320.Ui.Pages.History
{
    public partial class EventLogPage : PageBase
    {
        public EventLogPage()
        {
            InitializeComponent();
            WireEvents();
        }

        private void WireEvents()
        {
            _dp.ValueChanged += (s, e) => ReloadDay(_dp.Value.Date);
            btnRefresh.Click += (s, e) => ReloadDay(_dp.Value.Date);
            btnOpenFolder.Click += (s, e) => { try { System.Diagnostics.Process.Start(EventLogger.LogDir); } catch { } };
            EventLogger.EventLogged += OnLiveEvent;
            Disposed += (s, e) => EventLogger.EventLogged -= OnLiveEvent;
            Load += (s, e) => ReloadDay(DateTime.Today);
        }

        private void ReloadDay(DateTime date)
        {
            _grid.Rows.Clear();
            foreach (var r in EventLogger.Read(date))
            {
                AppendRow(r);
            }
        }

        private void OnLiveEvent(EventRow r)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<EventRow>(OnLiveEvent), r);
                return;
            }

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

            var row = _grid.Rows[idx];
            switch (r.Kind)
            {
                case EventKind.Alarm:
                    row.DefaultCellStyle.ForeColor = Color.IndianRed;
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    break;
                case EventKind.Warning:
                    row.DefaultCellStyle.ForeColor = Color.DarkOrange;
                    break;
                case EventKind.Data:
                    row.DefaultCellStyle.ForeColor = Color.SteelBlue;
                    break;
                case EventKind.Work:
                    row.DefaultCellStyle.ForeColor = Color.Teal;
                    break;
            }
        }
    }
}
