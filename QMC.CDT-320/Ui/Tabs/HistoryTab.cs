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

            HideFileLogHistoryButton(BtnEvent);
            HideFileLogHistoryButton(BtnWarning);
            RegisterSidebarButton(BtnAlarm,        "hist.alarm",        op, () => new AlarmHistoryPage());
            HideFileLogHistoryButton(BtnData);
            HideFileLogHistoryButton(BtnWork);
            HideFileLogHistoryButton(BtnInputSeq);
            HideFileLogHistoryButton(BtnFrontHeadSeq);
            HideFileLogHistoryButton(BtnRearHeadSeq);
            HideFileLogHistoryButton(BtnOutputSeq);
            HideFileLogHistoryButton(BtnMessageEdit);
        }

        private static void HideFileLogHistoryButton(System.Windows.Forms.Control button)
        {
            if (button == null)
                return;

            // 로그 파일 기반 Event/Sequence 이력은 메모리 사용량이 커서 우선 차단한다.
            // 이력 탭은 AlarmHistoryPage만 표시한다.
            button.Enabled = false;
            button.Visible = false;
        }
    }
}
