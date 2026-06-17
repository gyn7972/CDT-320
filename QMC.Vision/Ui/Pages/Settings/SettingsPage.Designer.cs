using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class SettingsPage
    {
        private System.ComponentModel.IContainer components = null;

        private Label           _hdr;
        private Panel           _sidebar;     // 핸들러 정렬: 우측 사이드바
        private Label           _sideHdr;
        private FlowLayoutPanel _btnHost;     // SidebarButton 세로 리스트
        private Panel           _detailHost;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // 정적 chrome 만(헤더/우측 사이드바/버튼 호스트/디테일 호스트). 버튼 구성·전환은 Code.cs.
        private void InitializeComponent()
        {
            this._hdr = new System.Windows.Forms.Label();
            this._sidebar = new System.Windows.Forms.Panel();
            this._sideHdr = new System.Windows.Forms.Label();
            this._btnHost = new System.Windows.Forms.FlowLayoutPanel();
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
            this._hdr.Text = "설정";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _sidebar (우측)
            //
            this._sidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._sidebar.Controls.Add(this._btnHost);
            this._sidebar.Controls.Add(this._sideHdr);
            this._sidebar.Dock = System.Windows.Forms.DockStyle.Right;
            this._sidebar.Location = new System.Drawing.Point(869, 30);
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
            this._sideHdr.Text = "  설정";
            this._sideHdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _btnHost (SidebarButton 세로 리스트)
            //
            this._btnHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(89)))), ((int)(((byte)(89)))), ((int)(((byte)(89)))));
            this._btnHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnHost.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this._btnHost.WrapContents = false;
            this._btnHost.AutoScroll = true;
            this._btnHost.Location = new System.Drawing.Point(0, 28);
            this._btnHost.Name = "_btnHost";
            this._btnHost.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this._btnHost.Size = new System.Drawing.Size(210, 564);
            this._btnHost.TabIndex = 1;
            //
            // _detailHost
            //
            this._detailHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this._detailHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._detailHost.Location = new System.Drawing.Point(0, 30);
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
