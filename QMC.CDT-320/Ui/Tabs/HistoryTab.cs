using System.ComponentModel;
using QMC.CDT_320.Ui.Pages.History;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    public partial class HistoryTab : TabBase
    {
        public HistoryTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("tab.history");
            const UserLevel op = UserLevel.Operator;

            RegisterSidebarButton(BtnAlarm,       "hist.alarm",   op, () => new AlarmHistoryPage());
            RegisterSidebarButton(BtnEvent,       "hist.event",   op, () => new EventLogPage());
            RegisterSidebarButton(BtnMessageEdit, "hist.msgEdit", UserLevel.Maintenance, () => new MessageEditPage());
        }
    }
}
