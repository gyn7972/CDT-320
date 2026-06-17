using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class RecipePage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private Panel            _hdrPanel;     // 헤더(브레드크럼 + 상단바 SAVE)
        private Label            _hdr;
        private Button           _btnSaveRecipe;
        private FlowLayoutPanel  _setFlow;      // 영속 세팅선택기 바(현 알고리즘의 finder/inspector)
        private TableLayoutPanel _body;
        private Panel            _content;      // 타깃 페이지 호스트(계약 보존)
        private Panel            _sidebar;      // 우: 검사 알고리즘 평면 선택
        private Label            _sideHdr;
        private FlowLayoutPanel  _sideFlow;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root = new TableLayoutPanel();
            this._hdrPanel = new Panel();
            this._hdr = new Label();
            this._btnSaveRecipe = new Button();
            this._setFlow = new FlowLayoutPanel();
            this._body = new TableLayoutPanel();
            this._content = new Panel();
            this._sidebar = new Panel();
            this._sideHdr = new Label();
            this._sideFlow = new FlowLayoutPanel();
            this._root.SuspendLayout();
            this._hdrPanel.SuspendLayout();
            this._body.SuspendLayout();
            this._sidebar.SuspendLayout();
            this.SuspendLayout();

            // _hdr (브레드크럼)
            this._hdr.Dock = DockStyle.Fill;
            this._hdr.Text = "Recipe";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _btnSaveRecipe (상단바 = 타깃 레시피 저장)
            this._btnSaveRecipe.Dock = DockStyle.Right;
            this._btnSaveRecipe.Width = 160;
            this._btnSaveRecipe.Text = "저장 (레시피)";
            this._btnSaveRecipe.FlatStyle = FlatStyle.Flat;
            this._btnSaveRecipe.Font = UiTheme.ButtonFont;
            this._btnSaveRecipe.BackColor = UiTheme.Accent;
            this._btnSaveRecipe.ForeColor = Color.White;
            this._btnSaveRecipe.Click += new System.EventHandler(this.OnSaveRecipeClick);

            // _hdrPanel (브레드크럼 + SAVE)
            this._hdrPanel.Dock = DockStyle.Fill;
            this._hdrPanel.BackColor = UiTheme.StatusBarBg;
            this._hdrPanel.Controls.Add(this._hdr);
            this._hdrPanel.Controls.Add(this._btnSaveRecipe);

            // _setFlow (세팅선택기 바 — 런타임 BuildSettingSelector)
            this._setFlow.Dock = DockStyle.Fill;
            this._setFlow.FlowDirection = FlowDirection.LeftToRight;
            this._setFlow.WrapContents = false;
            this._setFlow.AutoScroll = true;
            this._setFlow.BackColor = Color.WhiteSmoke;
            this._setFlow.Padding = new Padding(4, 4, 4, 4);

            // _content
            this._content.Dock = DockStyle.Fill;
            this._content.BackColor = UiTheme.MainBg;

            // _sideHdr (Handler LblSidebarHeader 미러: H50, MiddleRight, SectionFont, Pad 우16)
            this._sideHdr.Dock = DockStyle.Top;
            this._sideHdr.Height = 50;
            this._sideHdr.Text = "검사 알고리즘";
            this._sideHdr.BackColor = UiTheme.SidebarHeaderBg;
            this._sideHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this._sideHdr.Font = UiTheme.SectionFont;
            this._sideHdr.TextAlign = ContentAlignment.MiddleRight;
            this._sideHdr.Padding = new Padding(0, 0, 16, 0);

            // _sideFlow (알고리즘 버튼 — 런타임 BuildSidebar)
            this._sideFlow.Dock = DockStyle.Fill;
            this._sideFlow.FlowDirection = FlowDirection.TopDown;
            this._sideFlow.WrapContents = false;
            this._sideFlow.AutoScroll = true;
            this._sideFlow.BackColor = UiTheme.SidebarBg;
            this._sideFlow.Padding = new Padding(4, 4, 4, 4);

            // _sidebar
            this._sidebar.Dock = DockStyle.Fill;
            this._sidebar.BackColor = UiTheme.SidebarBg;
            this._sidebar.Controls.Add(this._sideFlow);
            this._sidebar.Controls.Add(this._sideHdr);

            // _body (content % | sidebar 240)
            this._body.Dock = DockStyle.Fill;
            this._body.ColumnCount = 2;
            this._body.RowCount = 1;
            this._body.BackColor = UiTheme.MainBg;
            this._body.Margin = Padding.Empty;
            this._body.Padding = Padding.Empty;
            this._body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210f));   // Handler SidebarWidth
            this._body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._body.Controls.Add(this._content, 0, 0);
            this._body.Controls.Add(this._sidebar, 1, 0);

            // _root (Row0 헤더 30, Row1 세팅바 40, Row2 본문 %)
            this._root.Dock = DockStyle.Fill;
            this._root.ColumnCount = 1;
            this._root.RowCount = 3;
            this._root.BackColor = UiTheme.MainBg;
            this._root.Margin = Padding.Empty;
            this._root.Padding = Padding.Empty;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this._root.Controls.Add(this._hdrPanel, 0, 0);
            this._root.Controls.Add(this._setFlow, 0, 1);
            this._root.Controls.Add(this._body, 0, 2);

            // RecipePage
            this.Controls.Add(this._root);
            this.Load += new System.EventHandler(this.OnPageLoad);
            this.AutoScaleMode = AutoScaleMode.None;
            this.Name = "RecipePage";
            this.Size = new Size(1920, 902);
            this._sidebar.ResumeLayout(false);
            this._body.ResumeLayout(false);
            this._hdrPanel.ResumeLayout(false);
            this._root.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
