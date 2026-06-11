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
            this._hdr = new System.Windows.Forms.Label();
            this._sidebar = new System.Windows.Forms.Panel();
            this._sideHdr = new System.Windows.Forms.Label();
            this._tree = new System.Windows.Forms.TreeView();
            this._detailHost = new System.Windows.Forms.Panel();
            this._sidebar.SuspendLayout();
            this.SuspendLayout();
            // 
            // _hdr
            // 
            this._hdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdr.Dock = System.Windows.Forms.DockStyle.Top;
            this._hdr.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._hdr.ForeColor = System.Drawing.Color.White;
            this._hdr.Location = new System.Drawing.Point(0, 0);
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._hdr.Size = new System.Drawing.Size(1079, 30);
            this._hdr.TabIndex = 0;
            this._hdr.Text = "설정 — 비전 알고리즘별 카메라 + 검사 파라미터";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _sidebar
            // 
            this._sidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._sidebar.Controls.Add(this._tree);
            this._sidebar.Controls.Add(this._sideHdr);
            this._sidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this._sidebar.Location = new System.Drawing.Point(0, 30);
            this._sidebar.Name = "_sidebar";
            this._sidebar.Size = new System.Drawing.Size(210, 592);
            this._sidebar.TabIndex = 1;
            // 
            // _sideHdr
            // 
            this._sideHdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._sideHdr.Dock = System.Windows.Forms.DockStyle.Top;
            this._sideHdr.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this._sideHdr.ForeColor = System.Drawing.Color.Black;
            this._sideHdr.Location = new System.Drawing.Point(0, 0);
            this._sideHdr.Name = "_sideHdr";
            this._sideHdr.Size = new System.Drawing.Size(210, 28);
            this._sideHdr.TabIndex = 0;
            this._sideHdr.Text = "  알고리즘 선택";
            this._sideHdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tree
            // 
            this._tree.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._tree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._tree.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tree.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this._tree.ForeColor = System.Drawing.Color.White;
            this._tree.FullRowSelect = true;
            this._tree.HideSelection = false;
            this._tree.ItemHeight = 28;
            this._tree.Location = new System.Drawing.Point(0, 28);
            this._tree.Name = "_tree";
            this._tree.ShowLines = false;
            this._tree.Size = new System.Drawing.Size(210, 564);
            this._tree.TabIndex = 1;
            this._tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.Tree_AfterSelect);
            // 
            // _detailHost
            // 
            this._detailHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._detailHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._detailHost.Location = new System.Drawing.Point(210, 30);
            this._detailHost.Name = "_detailHost";
            this._detailHost.Size = new System.Drawing.Size(869, 592);
            this._detailHost.TabIndex = 2;
            // 
            // SettingsPage
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.Controls.Add(this._detailHost);
            this.Controls.Add(this._sidebar);
            this.Controls.Add(this._hdr);
            this.Name = "SettingsPage";
            this.Size = new System.Drawing.Size(1079, 622);
            this._sidebar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
