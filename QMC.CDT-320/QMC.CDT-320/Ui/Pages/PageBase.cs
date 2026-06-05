using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;
using QMC.CDT_320.Ui.Util;

namespace QMC.CDT_320.Ui.Pages
{
    /// <summary>
    /// 서브 페이지 UserControl 공통 베이스.
    /// Host(Form1)는 생성자에서 주입받지 않고, 부모 TabBase에서 정적 참조로 접근 가능.
    /// 단, 디자이너 지원을 위해 파라미터 없는 기본 생성자를 유지한다.
    /// </summary>
    public class PageBase : UserControl
    {
        /// <summary>
        /// Stage 60 — 페이지가 처음 표시될 때 1회 호출. Click 핸들러가 부착되지 않은
        /// Button/ActionButton/SidebarButton 에 placeholder 피드백 핸들러를 자동 부착.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            try { UiClickAuditor.EnsureFeedback(this); } catch { }
        }

        /// <summary>페이지 상단 주황색 섹션 헤더 (CDT-300 스타일).</summary>
        protected Label CreateSectionHeader(string i18nKey, int width = 0)
        {
            var h = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 30,
                Text      = Lang.T(i18nKey),
                Tag       = "i18n:" + i18nKey,
                BackColor = UiTheme.StatusBarBg,
                ForeColor = UiTheme.StatusBarFg,
                Font      = UiTheme.SectionFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };
            if (width > 0) { h.Dock = DockStyle.None; h.Width = width; }
            return h;
        }

        public PageBase()
        {
            BackColor      = UiTheme.MainBg;
            DoubleBuffered = true;
            // Stage 60 — 일부 페이지(StageRecipePage/VisionRecipePage 등) 콘텐츠가 표시 영역을 넘쳐
            // 좌측 옵션 패널 / 트리·리스트 / 하단 액션 영역이 잘려 보이는 이슈가 있다.
            // 사용자 보고: "왼쪽에 트리 컨트롤 있는 페이지들 이게 가려서 안보이는것들 있어".
            // AutoScroll = true 로 콘텐츠 초과 시 스크롤바를 자동 표시한다.
            AutoScroll     = true;
        }

        /// <summary>VS 디자이너 모드 체크.</summary>
        protected bool IsDesignerMode()
            => LicenseManager.UsageMode == LicenseUsageMode.Designtime;
    }
}
