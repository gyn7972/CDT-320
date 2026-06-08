using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class SettingsPage
    {
        private System.ComponentModel.IContainer components = null;

        private Label    _hdr;
        private Panel    _sidebar;
        private Label    _sideHdr;
        private TreeView _tree;
        private Panel    _detailHost;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // 정적 chrome 만(헤더/사이드바/트리/디테일 호스트). 서브패널 인스턴스화·노드 구성은 Code.cs.
        private void InitializeComponent()
        {
            this._hdr = new Label();
            this._sidebar = new Panel();
            this._sideHdr = new Label();
            this._tree = new TreeView();
            this._detailHost = new Panel();
            this._sidebar.SuspendLayout();
            this.SuspendLayout();

            this.BackColor = UiTheme.MainBg;

            // _hdr
            this._hdr.Dock = DockStyle.Top;
            this._hdr.Height = 30;
            this._hdr.Text = "설정 — 비전 알고리즘별 카메라 + 검사 파라미터";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _sidebar
            this._sidebar.Dock = DockStyle.Left;
            this._sidebar.Width = UiTheme.SidebarWidth + 20;
            this._sidebar.BackColor = UiTheme.SidebarBg;

            // _sideHdr
            this._sideHdr.Dock = DockStyle.Top;
            this._sideHdr.Height = 28;
            this._sideHdr.Text = "  알고리즘 선택";
            this._sideHdr.BackColor = UiTheme.SidebarHeaderBg;
            this._sideHdr.ForeColor = UiTheme.SidebarHeaderFg;
            this._sideHdr.Font = UiTheme.SectionFont;
            this._sideHdr.TextAlign = ContentAlignment.MiddleLeft;

            // _tree
            this._tree.Dock = DockStyle.Fill;
            this._tree.BorderStyle = BorderStyle.None;
            this._tree.BackColor = UiTheme.SidebarBtnBg;
            this._tree.ForeColor = UiTheme.SidebarBtnFg;
            this._tree.Font = UiTheme.ButtonFont;
            this._tree.ShowLines = false;
            this._tree.ShowPlusMinus = true;
            this._tree.ShowRootLines = true;
            this._tree.HideSelection = false;
            this._tree.FullRowSelect = true;
            this._tree.ItemHeight = 28;
            this._tree.AfterSelect += new TreeViewEventHandler(this.Tree_AfterSelect);

            this._sidebar.Controls.Add(this._sideHdr);
            this._sidebar.Controls.Add(this._tree);
            this._sidebar.Controls.SetChildIndex(this._tree, 0);
            this._sidebar.Controls.SetChildIndex(this._sideHdr, 1);

            // _detailHost
            this._detailHost.Dock = DockStyle.Fill;
            this._detailHost.BackColor = UiTheme.MainBg;

            // SettingsPage (원본 추가순서 + SetChildIndex)
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._sidebar);
            this.Controls.Add(this._detailHost);
            this.Controls.SetChildIndex(this._detailHost, 0);
            this.Controls.SetChildIndex(this._sidebar, 1);
            this.Controls.SetChildIndex(this._hdr, 2);
            this.Name = "SettingsPage";
            this._sidebar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
