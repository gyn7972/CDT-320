using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Tabs
{
    partial class TabBase
    {
        private System.ComponentModel.IContainer components = null;

        /// <summary>우측 세로 사이드바 패널.</summary>
        protected System.Windows.Forms.Panel PnlSidebar;

        /// <summary>사이드바 상단 제목 라벨.</summary>
        protected System.Windows.Forms.Label LblSidebarHeader;

        /// <summary>사이드바 내부 상단 버튼 스택(주 메뉴).</summary>
        protected System.Windows.Forms.FlowLayoutPanel PnlSidebarButtons;

        /// <summary>사이드바 내부 하단 버튼 스택(보조 메뉴, LOGIC/바코드 등 분리 영역).</summary>
        protected System.Windows.Forms.FlowLayoutPanel PnlSidebarBottomButtons;

        /// <summary>메인 콘텐츠 영역 — 서브 페이지 UserControl이 Dock=Fill 로 호스트된다.</summary>
        protected System.Windows.Forms.Panel PnlContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.PnlSidebar              = new System.Windows.Forms.Panel();
            this.PnlSidebarBottomButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.PnlSidebarButtons       = new System.Windows.Forms.FlowLayoutPanel();
            this.LblSidebarHeader        = new System.Windows.Forms.Label();
            this.PnlContent              = new System.Windows.Forms.Panel();
            this.PnlSidebar.SuspendLayout();
            this.SuspendLayout();

            // PnlSidebar  (우측 docked, 세 구역: 헤더 / 버튼 / 하단 버튼)
            this.PnlSidebar.Controls.Add(this.PnlSidebarButtons);
            this.PnlSidebar.Controls.Add(this.PnlSidebarBottomButtons);
            this.PnlSidebar.Controls.Add(this.LblSidebarHeader);
            this.PnlSidebar.Dock      = DockStyle.Right;
            this.PnlSidebar.Name      = "PnlSidebar";
            this.PnlSidebar.Width     = UiTheme.SidebarWidth;
            this.PnlSidebar.BackColor = UiTheme.SidebarBg;

            // LblSidebarHeader (상단, 탭 제목)
            this.LblSidebarHeader.Dock      = DockStyle.Top;
            this.LblSidebarHeader.Height    = 50;
            this.LblSidebarHeader.Name      = "LblSidebarHeader";
            this.LblSidebarHeader.BackColor = UiTheme.SidebarHeaderBg;
            this.LblSidebarHeader.ForeColor = UiTheme.SidebarHeaderFg;
            this.LblSidebarHeader.Font      = UiTheme.SectionFont;
            this.LblSidebarHeader.TextAlign = ContentAlignment.MiddleRight;
            this.LblSidebarHeader.Padding   = new Padding(0, 0, 16, 0);
            this.LblSidebarHeader.Text      = "";

            // PnlSidebarBottomButtons — 과거 분리된 하단 영역 (deprecated).
            // TabBase.AddSidebarButton 가 이제 toBottomArea=true 도 PnlSidebarButtons 에 통합 배치하므로
            // 이 패널은 시각적으로 비활성화하지만 디자이너 호환을 위해 필드는 유지한다.
            this.PnlSidebarBottomButtons.Dock          = DockStyle.Bottom;
            this.PnlSidebarBottomButtons.FlowDirection = FlowDirection.TopDown;
            this.PnlSidebarBottomButtons.WrapContents  = false;
            this.PnlSidebarBottomButtons.AutoSize      = false;
            this.PnlSidebarBottomButtons.Height        = 0;
            this.PnlSidebarBottomButtons.Visible       = false;
            this.PnlSidebarBottomButtons.BackColor     = UiTheme.SidebarBg;
            this.PnlSidebarBottomButtons.Padding       = new Padding(0);
            this.PnlSidebarBottomButtons.Name          = "PnlSidebarBottomButtons";

            // PnlSidebarButtons (Fill — 주 버튼 스택)
            this.PnlSidebarButtons.Dock          = DockStyle.Fill;
            this.PnlSidebarButtons.FlowDirection = FlowDirection.TopDown;
            this.PnlSidebarButtons.WrapContents  = false;
            this.PnlSidebarButtons.AutoScroll    = true;
            this.PnlSidebarButtons.BackColor     = UiTheme.SidebarBg;
            this.PnlSidebarButtons.Padding       = new Padding(4, 6, 4, 6);
            this.PnlSidebarButtons.Name          = "PnlSidebarButtons";

            // PnlContent (Fill)
            this.PnlContent.Dock      = DockStyle.Fill;
            this.PnlContent.Name      = "PnlContent";
            this.PnlContent.BackColor = UiTheme.MainBg;

            // TabBase
            this.Controls.Add(this.PnlContent);
            this.Controls.Add(this.PnlSidebar);
            this.BackColor      = UiTheme.MainBg;
            this.DoubleBuffered = true;
            this.Name           = "TabBase";
            this.Size           = new Size(1694, 980);

            this.PnlSidebar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
