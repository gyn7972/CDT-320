using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class RecipePage
    {
        private System.ComponentModel.IContainer components = null;

        private TableLayoutPanel _root;
        private TableLayoutPanel _body;          // 3열: 콘텐츠(에디터) / Finder 레일 / 모듈 레일(우측)
        private TableLayoutPanel _contentHost;   // 콘텐츠 col: 브레드크럼 + 에디터 스왑
        private Panel            _hdrPanel;       // 브레드크럼(상단 SAVE 제거)
        private Label            _hdr;
        private Panel            _content;        // 에디터 스왑 영역

        private Panel            _finderRail;     // 우측 1: 현 모듈의 finder/inspector 세로 목록 + 저장
        private Label            _setHdr;
        private FlowLayoutPanel  _setFlow;
        private Panel            _recipeBar;
        private Button           _btnSaveRecipe;  // 레시피 저장(품목별) — Finder 레일 하단

        private Panel            _sidebar;        // 우측 2(고정): 모듈(검사 알고리즘) 평면 선택
        private Label            _sideHdr;
        private FlowLayoutPanel  _sideFlow;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._root        = new TableLayoutPanel();
            this._body        = new TableLayoutPanel();
            this._contentHost = new TableLayoutPanel();
            this._hdrPanel    = new Panel();
            this._hdr         = new Label();
            this._content     = new Panel();
            this._finderRail  = new Panel();
            this._setHdr      = new Label();
            this._setFlow     = new FlowLayoutPanel();
            this._recipeBar   = new Panel();
            this._btnSaveRecipe = new Button();
            this._sidebar     = new Panel();
            this._sideHdr     = new Label();
            this._sideFlow    = new FlowLayoutPanel();

            this._root.SuspendLayout();
            this._body.SuspendLayout();
            this._contentHost.SuspendLayout();
            this._hdrPanel.SuspendLayout();
            this._finderRail.SuspendLayout();
            this._recipeBar.SuspendLayout();
            this._sidebar.SuspendLayout();
            this.SuspendLayout();

            // _root
            this._root.BackColor = UiTheme.MainBg;
            this._root.ColumnCount = 1;
            this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._root.RowCount = 1;
            this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._root.Controls.Add(this._body, 0, 0);
            this._root.Dock = DockStyle.Fill;
            this._root.Margin = new Padding(0);
            this._root.Name = "_root";

            // _body — 3열(콘텐츠 100% / Finder 레일 200 / 모듈 레일 210)
            this._body.BackColor = UiTheme.MainBg;
            this._body.ColumnCount = 3;
            this._body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            this._body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
            this._body.RowCount = 1;
            this._body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._body.Dock = DockStyle.Fill;
            this._body.Margin = new Padding(0);
            this._body.Name = "_body";
            this._body.Controls.Add(this._contentHost, 0, 0);
            this._body.Controls.Add(this._finderRail,  1, 0);
            this._body.Controls.Add(this._sidebar,     2, 0);

            // _contentHost — 브레드크럼(40) + 에디터(Fill)
            this._contentHost.ColumnCount = 1;
            this._contentHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._contentHost.RowCount = 2;
            this._contentHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            this._contentHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this._contentHost.Dock = DockStyle.Fill;
            this._contentHost.Margin = new Padding(0);
            this._contentHost.Name = "_contentHost";
            this._contentHost.Controls.Add(this._hdrPanel, 0, 0);
            this._contentHost.Controls.Add(this._content,  0, 1);

            // _hdrPanel — 브레드크럼만(SAVE 제거)
            this._hdrPanel.BackColor = UiTheme.StatusBarBg;
            this._hdrPanel.Dock = DockStyle.Fill;
            this._hdrPanel.Margin = new Padding(0);
            this._hdrPanel.Name = "_hdrPanel";
            this._hdrPanel.Controls.Add(this._hdr);

            // _hdr
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.Dock = DockStyle.Fill;
            this._hdr.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            this._hdr.ForeColor = Color.White;
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new Padding(10, 0, 0, 0);
            this._hdr.Text = "Recipe";
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;

            // _content — 에디터 스왑
            this._content.BackColor = UiTheme.MainBg;
            this._content.Dock = DockStyle.Fill;
            this._content.Margin = new Padding(0);
            this._content.Name = "_content";

            // _finderRail (우측 1)
            this._finderRail.BackColor = UiTheme.SidebarBg;
            this._finderRail.Dock = DockStyle.Fill;
            this._finderRail.Margin = new Padding(0);
            this._finderRail.Name = "_finderRail";
            this._finderRail.Controls.Add(this._setFlow);
            this._finderRail.Controls.Add(this._recipeBar);
            this._finderRail.Controls.Add(this._setHdr);

            // _setHdr
            this._setHdr.BackColor = UiTheme.SidebarHeaderBg;
            this._setHdr.Dock = DockStyle.Top;
            this._setHdr.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._setHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this._setHdr.Height = 44;
            this._setHdr.Name = "_setHdr";
            this._setHdr.Padding = new Padding(10, 0, 0, 0);
            this._setHdr.Text = "Finder / Inspector";
            this._setHdr.TextAlign = ContentAlignment.MiddleLeft;

            // _setFlow — 세로 목록
            this._setFlow.AutoScroll = true;
            this._setFlow.BackColor = UiTheme.SidebarBg;
            this._setFlow.Dock = DockStyle.Fill;
            this._setFlow.FlowDirection = FlowDirection.TopDown;
            this._setFlow.Name = "_setFlow";
            this._setFlow.Padding = new Padding(5, 6, 5, 6);
            this._setFlow.WrapContents = false;

            // _recipeBar — 하단 저장
            this._recipeBar.BackColor = UiTheme.SidebarBg;
            this._recipeBar.Dock = DockStyle.Bottom;
            this._recipeBar.Height = 50;
            this._recipeBar.Name = "_recipeBar";
            this._recipeBar.Padding = new Padding(6, 6, 6, 8);
            this._recipeBar.Controls.Add(this._btnSaveRecipe);

            // _btnSaveRecipe (품목별 저장)
            this._btnSaveRecipe.BackColor = UiTheme.Accent;
            this._btnSaveRecipe.Dock = DockStyle.Fill;
            this._btnSaveRecipe.FlatStyle = FlatStyle.Flat;
            this._btnSaveRecipe.FlatAppearance.BorderSize = 0;
            this._btnSaveRecipe.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._btnSaveRecipe.ForeColor = Color.White;
            this._btnSaveRecipe.Name = "_btnSaveRecipe";
            this._btnSaveRecipe.Text = "레시피 저장 (품목별)";
            this._btnSaveRecipe.UseVisualStyleBackColor = false;
            this._btnSaveRecipe.Click += new System.EventHandler(this.OnSaveRecipeClick);

            // _sidebar (우측 2, 고정) — 모듈
            this._sidebar.BackColor = UiTheme.SidebarBg;
            this._sidebar.Dock = DockStyle.Fill;
            this._sidebar.Margin = new Padding(0);
            this._sidebar.Name = "_sidebar";
            this._sidebar.Controls.Add(this._sideFlow);
            this._sidebar.Controls.Add(this._sideHdr);

            // _sideHdr
            this._sideHdr.BackColor = UiTheme.SidebarHeaderBg;
            this._sideHdr.Dock = DockStyle.Top;
            this._sideHdr.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._sideHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this._sideHdr.Height = 44;
            this._sideHdr.Name = "_sideHdr";
            this._sideHdr.Padding = new Padding(10, 0, 0, 0);
            this._sideHdr.Text = "모듈 (Module)";
            this._sideHdr.TextAlign = ContentAlignment.MiddleLeft;

            // _sideFlow
            this._sideFlow.AutoScroll = true;
            this._sideFlow.BackColor = UiTheme.SidebarBg;
            this._sideFlow.Dock = DockStyle.Fill;
            this._sideFlow.FlowDirection = FlowDirection.TopDown;
            this._sideFlow.Name = "_sideFlow";
            this._sideFlow.Padding = new Padding(5, 6, 5, 6);
            this._sideFlow.WrapContents = false;

            // RecipePage
            this.AutoScaleMode = AutoScaleMode.None;
            this.Controls.Add(this._root);
            this.Name = "RecipePage";
            this.Load += new System.EventHandler(this.OnPageLoad);

            this._root.ResumeLayout(false);
            this._body.ResumeLayout(false);
            this._contentHost.ResumeLayout(false);
            this._hdrPanel.ResumeLayout(false);
            this._finderRail.ResumeLayout(false);
            this._recipeBar.ResumeLayout(false);
            this._sidebar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
