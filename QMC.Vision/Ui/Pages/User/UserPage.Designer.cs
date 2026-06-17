using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Pages
{
    partial class UserPage
    {
        private System.ComponentModel.IContainer components = null;

        private Label _hdr;
        private Label _body;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._hdr = new Label();
            this._body = new Label();
            this.SuspendLayout();

            // _hdr
            this._hdr.Dock = DockStyle.Top;
            this._hdr.Height = 30;
            this._hdr.Text = "사용자 — 사용자/권한 관리";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _body
            this._body.Dock = DockStyle.Fill;
            this._body.Text = "사용자 관리 (placeholder)";
            this._body.Font = new Font("Segoe UI", 18F);
            this._body.ForeColor = Color.DimGray;
            this._body.TextAlign = ContentAlignment.MiddleCenter;

            // UserPage (추가순서: 헤더→본문)
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._body);
            this.Name = "UserPage";
            this.ResumeLayout(false);
        }
    }
}
