using System.ComponentModel;
using QMC.Common.Logging;
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

            RegisterSidebarButton(BtnAlarm,        "hist.alarm",        op, () => new AlarmHistoryPage());
            RegisterSidebarButton(BtnEvent,        "hist.event",        op, () => new EventLogPage());
            RegisterSidebarButton(BtnInputSeq,     "hist.inputSeq",     op, () => new EventLogPage(EventKind.InputSeq));
            RegisterSidebarButton(BtnOutputSeq,    "hist.outputSeq",    op, () => new EventLogPage(EventKind.OutputSeq));
            RegisterSidebarButton(BtnFrontHeadSeq, "hist.frontHeadSeq", op, () => new EventLogPage(EventKind.FrontHeadSeq));
            RegisterSidebarButton(BtnRearHeadSeq,  "hist.rearHeadSeq",  op, () => new EventLogPage(EventKind.RearHeadSeq));
            RegisterSidebarButton(BtnMessageEdit,  "hist.msgEdit",      UserLevel.Maintenance, () => new MessageEditPage());
        }
    }
}
