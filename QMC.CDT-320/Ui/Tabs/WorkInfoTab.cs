using System.ComponentModel;
using QMC.CDT_320.Ui.Pages.WorkInfo;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>작업 정보 — 320: HEAD를 FRONT/REAR 2개로 분리.</summary>
    public partial class WorkInfoTab : TabBase
    {
        public WorkInfoTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("tab.workInfo");
            const UserLevel op = UserLevel.Operator;

            AddSidebarButton("wi.inputCassette",  op, () => new InputCassettePage());
            AddSidebarButton("wi.inputFeeder",    op, () => new InputFeederPage());
            AddSidebarButton("wi.inputStage",     op, () => new InputStagePage());
            AddSidebarButton("wi.frontHead",      op, () => new HeadPage("wi.frontHead"));
            AddSidebarButton("wi.rearHead",       op, () => new HeadPage("wi.rearHead"));
            AddSidebarButton("wi.outputStage",    op, () => new OutputStagePage());
            AddSidebarButton("wi.outputFeeder",   op, () => new OutputFeederPage());
            AddSidebarButton("wi.outputCassette", op, () => new OutputCassettePage());

            // Stage 4 — Active Lot 패널
            AddSidebarButton("wi.activeLot", op, () => new ActiveLotPage());

            // Stage 55 — Operation Panel Status (DI/DO/TowerLamp/Resource/Ionizer)
            AddSidebarButton("wi.opPanelStatus", op, () => new OperationPanelStatusPage());
            // Stage 56 — Plate Status (NG/Good 적재 현황)
            AddSidebarButton("wi.plateStatus",   op, () => new PlateStatusPage());

            AddSidebarButton("wi.logic", UserLevel.Engineer, () => new LogicDetailPage(), toBottomArea: true);
        }
    }
}
