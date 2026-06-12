using System.ComponentModel;
using QMC.CDT_320.Ui.Pages.WorkInfo;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>?? ?? ? - INPUT/OUTPUT ?? ?? ??.</summary>
    public partial class WorkInfoTab : TabBase
    {
        public WorkInfoTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("tab.workInfo");
            const UserLevel op = UserLevel.Operator;

            RegisterSidebarButton(BtnInputCassette,        "wi.inputCassette",     op, () => new InputCassettePage());
            RegisterSidebarButton(BtnInputFeeder,          "wi.inputFeeder",       op, () => new InputFeederPage());
            RegisterSidebarButton(BtnInputStage,           "wi.inputStage",        op, () => new InputStagePage());
            RegisterSidebarButton(BtnFrontHead,            "wi.frontHead",         op, () => new FrontPickerPage());
            RegisterSidebarButton(BtnRearHead,             "wi.rearHead",          op, () => new RearPickerPage());
            RegisterSidebarButton(BtnOutputStage,          "wi.outputStage",       op, () => new OutputStagePage());
            RegisterSidebarButton(BtnOutputFeeder,         "wi.outputFeeder",      op, () => new OutputFeederPage());
            RegisterSidebarButton(BtnOutputCassette,       "wi.outputCassette",    op, () => new OutputCassettePage());
            RegisterSidebarButton(BtnActiveLot,            "wi.activeLot",         op, () => new ActiveLotPage());
            RegisterSidebarButton(BtnOperationPanelStatus, "wi.opPanelStatus",     op, () => new OperationPanelStatusPage());
            RegisterSidebarButton(BtnPlateStatus,          "wi.plateStatus",       op, () => new PlateStatusPage());
            RegisterSidebarButton(BtnLogic,                "wi.logic", UserLevel.Engineer, () => new LogicDetailPage());
        }
    }
}
