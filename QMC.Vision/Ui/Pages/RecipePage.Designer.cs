using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class RecipePage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private Label            _hdr;
        private TableLayoutPanel _body;
        private Panel            _content;     // 좌: 타깃 페이지 호스트 (계약 보존)
        private Panel            _sidebar;     // 우: RecipeTab 미러 사이드바
        private Label            _sideHdr;
        private FlowLayoutPanel  _sideFlow;    // 런타임 BuildSidebar 가 버튼 채움

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root = new TableLayoutPanel();
            this._hdr = new Label();
            this._body = new TableLayoutPanel();
            this._content = new Panel();
            this._sidebar = new Panel();
            this._sideHdr = new Label();
            this._sideFlow = new FlowLayoutPanel();
            this._root.SuspendLayout();
            this._body.SuspendLayout();
            this._sidebar.SuspendLayout();
            this.SuspendLayout();

            // _hdr (root 0,0 — Absolute 30) : 브레드크럼(현재 타깃 표시)
            this._hdr.Dock = DockStyle.Fill;
            this._hdr.Text = "Recipe";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _content (body 0,0 — Percent) : 타깃 페이지 호스트
            this._content.Dock = DockStyle.Fill;
            this._content.BackColor = UiTheme.MainBg;

            // _sideHdr (sidebar Top) : 사이드바 헤더
            this._sideHdr.Dock = DockStyle.Top;
            this._sideHdr.Height = 28;
            this._sideHdr.Text = "  비전 타깃";
            this._sideHdr.BackColor = UiTheme.SidebarHeaderBg;
            this._sideHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this._sideHdr.Font = UiTheme.ButtonFont;
            this._sideHdr.TextAlign = ContentAlignment.MiddleLeft;

            // _sideFlow (sidebar Fill, 위→아래 단일열, 런타임 채움)
            this._sideFlow.Dock = DockStyle.Fill;
            this._sideFlow.FlowDirection = FlowDirection.TopDown;
            this._sideFlow.WrapContents = false;
            this._sideFlow.AutoScroll = true;
            this._sideFlow.BackColor = UiTheme.SidebarBg;
            this._sideFlow.Padding = new Padding(4, 4, 4, 4);

            // _sidebar (body 0,1 — Absolute 240)
            this._sidebar.Dock = DockStyle.Fill;
            this._sidebar.BackColor = UiTheme.SidebarBg;
            this._sidebar.Controls.Add(this._sideFlow);
            this._sidebar.Controls.Add(this._sideHdr);

            // _body (2열: content % | sidebar 240)
            this._body.Dock = DockStyle.Fill;
            this._body.ColumnCount = 2;
            this._body.RowCount = 1;
            this._body.BackColor = UiTheme.MainBg;
            this._body.Margin = Padding.Empty;
            this._body.Padding = Padding.Empty;
            this._body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240f));
            this._body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._body.Controls.Add(this._content, 0, 0);
            this._body.Controls.Add(this._sidebar, 1, 0);

            // _root (Row0 30=hdr, Row1 100%=body)
            this._root.Dock = DockStyle.Fill;
            this._root.ColumnCount = 1;
            this._root.RowCount = 2;
            this._root.BackColor = UiTheme.MainBg;
            this._root.Margin = Padding.Empty;
            this._root.Padding = Padding.Empty;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._root.Controls.Add(this._hdr, 0, 0);
            this._root.Controls.Add(this._body, 0, 1);

            // RecipePage
            this.Controls.Add(this._root);
            this.Load += new System.EventHandler(this.OnPageLoad);
            this.Name = "RecipePage";
            this._sidebar.ResumeLayout(false);
            this._body.ResumeLayout(false);
            this._root.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
