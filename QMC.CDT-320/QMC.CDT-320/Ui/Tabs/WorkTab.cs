using System;
using System.ComponentModel;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT_320.Ui.Dialogs;
using QMC.CDT_320.Ui.Pages;
using QMC.CDT_320.Ui.Pages.Material;
using QMC.CDT_320.Ui.Pages.Work;
using QMC.CDT_320.Ui.Security;

namespace QMC.CDT_320.Ui.Tabs
{
    /// <summary>작업 탭 — CDT-300 메인 화면 + INPUT/OUTPUT 맵 전환 + 모드 버튼들.</summary>
    public partial class WorkTab : TabBase
    {
        public WorkTab()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            SetSidebarHeader("tab.work");
            const UserLevel op = UserLevel.Operator;
            const UserLevel en = UserLevel.Engineer;
            const UserLevel mt = UserLevel.Maintenance;

            AddSidebarButton("work.page.main", op, () => new WorkMainPage());

            // ── 실제 장비 동작 버튼들 (Controller 호출) ──
            AddActionButton("work.init",       op, () => RunSafe(async c => await c.InitAsync()));
            AddActionButton("work.start",      op, () => RunSafe(async c => await c.StartAsync()));
            AddActionButton("work.stop",       op, () => RunSafe(async c => await c.StopAsync()));
            AddActionButton("work.cycleRun",   op, () => RunSafe(async c => await c.CycleRunAsync()));
            AddActionButton("work.cycleStop",  op, () => RunSafe(async c => await c.CycleStopAsync()));
            // R2 — 알람 해제 (Engineer 이상)
            AddActionButton("work.resetAlarm", en, () => RunSafe(async c => await c.ResetAlarmAsync()));
            // Stage 35 — 설비 라이프사이클 (Shutdown / E-Stop)
            AddActionButton("work.shutdown",   mt, () => RunSafe(async c => await c.ShutdownAsync()));
            AddActionButton("work.estop",      op, () => RunSafe(async c => await c.EmergencyStopAsync()));
            AddActionButton("work.inputCst",   op, OpenInputCstStatus);
            AddActionButton("work.outputCst",  op, OpenOutputCstStatus);

            // ── 모드 다이얼로그 버튼들 ──
            AddModeButton("work.colletMode",      en, () => new ColletChangeDialog());
            AddModeButton("work.needleMode",      mt, () => new NeedleChangeDialog());
            AddModeButton("work.selfCheckMode",   en, () => new SelfInspectionDialog());
            AddModeButton("work.autoPosMode",     mt, () => new AutoPositionDialog());
            AddModeButton("work.colletCleanMode", en, () => new ColletCleaningDialog());
            AddModeButton("work.colletCheckMode", en, () => new PositionCheckDialog());
            AddModeButton("work.posCheck",        en, () => new PositionCheckDialog());
            AddModeButton("work.needlePosMode",   en, () => new PositionCheckDialog());

            // ── 페이지 전환 버튼들 ──
            AddSidebarButton("work.inputMapTransfer",  op, () => new MapTransferPage("work.page.inputMap"));
            AddSidebarButton("work.outputMapTransfer", op, () => new MapTransferPage("work.page.outputMap"));
            AddSidebarButton("work.visionAlign",       en, () => new VisionAlignPage());
            AddSidebarButton("work.waferMapOpen",      en, () => new WaferMapOpenPage());
            AddSidebarButton("work.dieMap",            en, () => new DieMapPage());
        }

        // ──────────────────────────────────────────
        //  액션 버튼: 메인화면(WorkMainPage)을 보여준 채 Controller 메서드 호출.
        //  사용자 요구 — 초기화/시작/정지/CYCLE RUN/CYCLE STOP/설비종료/RESET ALARM/E-STOP 등
        //  액션 버튼 클릭 시 다른 페이지로 전환되지 않고 메인화면 유지.
        // ──────────────────────────────────────────

        private void AddActionButton(string i18nKey, UserLevel minLevel, Action onClick)
        {
            var btn = AddSidebarButton(i18nKey, minLevel, () => new WorkMainPage());
            btn.Click += (s, e) => onClick?.Invoke();
        }

        private void AddModeButton(string i18nKey, UserLevel minLevel, Func<Form> dlgFactory)
        {
            var btn = AddSidebarButton(i18nKey, minLevel, () => new PlaceholderPage(i18nKey));
            btn.Click += (s, e) =>
            {
                using (var dlg = dlgFactory()) dlg.ShowDialog(FindForm());
            };
        }

        private void RunSafe(Func<MachineController, System.Threading.Tasks.Task> action)
        {
            if (Host == null || Host.Controller == null) return;
            _ = action(Host.Controller);
        }

        private void OpenInputCstStatus()
        {
            using (var dlg = new CstStatusDialog(isInput: true)) dlg.ShowDialog(FindForm());
        }
        private void OpenOutputCstStatus()
        {
            using (var dlg = new CstStatusDialog(isInput: false)) dlg.ShowDialog(FindForm());
        }
    }
}
