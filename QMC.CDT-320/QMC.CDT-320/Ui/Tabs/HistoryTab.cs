using System.ComponentModel;
using QMC.CDT_320.Ui.Pages.History;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>이력 — 알람/경고/이벤트/데이터/작업 + 하단 MESSAGE 편집.</summary>
    public partial class HistoryTab : TabBase
    {
        public HistoryTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("tab.history");

            const UserLevel op = UserLevel.Operator;
            AddSidebarButton("hist.alarm",   op, () => new AlarmHistoryPage());  // Stage 20
            AddSidebarButton("hist.warning", op, () => new FilterGridPage("hist.warning","WARN"));
            AddSidebarButton("hist.event",   op, () => new EventLogPage());
            AddSidebarButton("hist.data",    op, () => new FilterGridPage("hist.data",   "DATA"));
            AddSidebarButton("hist.work",    op, () => new FilterGridPage("hist.work",   "WORK"));

            AddSidebarButton("hist.msgEdit", UserLevel.Maintenance, () => new MessageEditPage(), toBottomArea: true);
        }
    }
}
