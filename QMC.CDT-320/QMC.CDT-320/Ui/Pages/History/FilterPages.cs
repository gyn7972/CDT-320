using System;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    public partial class FilterGridPage : PageBase
    {
        private string _kind;

        public FilterGridPage()
            : this("History", "History")
        {
        }

        public FilterGridPage(string titleI18n, string kind, string[][] seed = null)
        {
            _kind = kind;
            InitializeComponent();
            lblHeader.Tag = "i18n:" + titleI18n;
            lblHeader.Text = Lang.T(titleI18n);
            Seed(seed);
        }

        private void Seed(string[][] rows)
        {
            if (rows != null)
            {
                foreach (var r in rows) _grid.Rows.Add(r);
                return;
            }

            var now = DateTime.Now;
            for (int i = 0; i < 6; i++)
            {
                _grid.Rows.Add(
                    (i + 1).ToString(),
                    now.AddMinutes(-i).ToString("yyyy-MM-dd HH:mm:ss"),
                    "QMC",
                    "8" + (1000 + i),
                    _kind + " sample message " + i);
            }
        }
    }
}
