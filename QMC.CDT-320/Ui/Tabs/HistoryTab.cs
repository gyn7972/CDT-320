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

            RegisterSidebarButton(BtnEvent,        "hist.event",        op, () => new EventLogPage(EventKind.Event));
            RegisterSidebarButton(BtnWarning,      "hist.warning",      op, () => new EventLogPage(EventKind.Warning));
            RegisterSidebarButton(BtnAlarm,        "hist.alarm",        op, () => new AlarmHistoryPage());
            RegisterSidebarButton(BtnData,         "hist.data",         op, () => new EventLogPage(EventKind.Data));
            RegisterSidebarButton(BtnWork,         "hist.work",         op, () => new EventLogPage(EventKind.Work));
            RegisterSidebarButton(BtnInputSeq,     "hist.inputSeq",     op, () => new EventLogPage(EventKind.InputSeq));
            RegisterSidebarButton(BtnFrontHeadSeq, "hist.frontHeadSeq", op, () => new EventLogPage(EventKind.FrontHeadSeq));
            RegisterSidebarButton(BtnRearHeadSeq,  "hist.rearHeadSeq",  op, () => new EventLogPage(EventKind.RearHeadSeq));
            RegisterSidebarButton(BtnOutputSeq,    "hist.outputSeq",    op, () => new EventLogPage(EventKind.OutputSeq));
            RegisterSidebarButton(BtnMessageEdit,  "hist.msgEdit",      UserLevel.Maintenance, () => new MessageEditPage());
        }
    }
}
