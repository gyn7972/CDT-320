namespace QMC.CDT_320.Ui.Controls
{
    partial class SessionControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel pnlCard;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblId;
        private System.Windows.Forms.TextBox txtId;
        private System.Windows.Forms.Label lblPw;
        private System.Windows.Forms.TextBox txtPw;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label lblMsg;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlCard = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblId = new System.Windows.Forms.Label();
            this.txtId = new System.Windows.Forms.TextBox();
            this.lblPw = new System.Windows.Forms.Label();
            this.txtPw = new System.Windows.Forms.TextBox();
            this.btnLogin = new System.Windows.Forms.Button();
            this.lblMsg = new System.Windows.Forms.Label();
            this.pnlCard.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlCard
            //
            this.pnlCard.BackColor = System.Drawing.Color.White;
            this.pnlCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlCard.Controls.Add(this.lblTitle);
            this.pnlCard.Controls.Add(this.lblId);
            this.pnlCard.Controls.Add(this.txtId);
            this.pnlCard.Controls.Add(this.lblPw);
            this.pnlCard.Controls.Add(this.txtPw);
            this.pnlCard.Controls.Add(this.btnLogin);
            this.pnlCard.Controls.Add(this.lblMsg);
            this.pnlCard.Location = new System.Drawing.Point(40, 40);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Size = new System.Drawing.Size(400, 320);
            //
            // lblTitle
            //
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(398, 52);
            this.lblTitle.Text = "로그인";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblId
            //
            this.lblId.AutoSize = true;
            this.lblId.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblId.ForeColor = System.Drawing.Color.Gray;
            this.lblId.Location = new System.Drawing.Point(40, 90);
            this.lblId.Text = "ID";
            //
            // txtId
            //
            this.txtId.Font = new System.Drawing.Font("Consolas", 12F);
            this.txtId.Location = new System.Drawing.Point(40, 112);
            this.txtId.Name = "txtId";
            this.txtId.Size = new System.Drawing.Size(320, 32);
            //
            // lblPw
            //
            this.lblPw.AutoSize = true;
            this.lblPw.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblPw.ForeColor = System.Drawing.Color.Gray;
            this.lblPw.Location = new System.Drawing.Point(40, 156);
            this.lblPw.Text = "PASSWORD";
            //
            // txtPw
            //
            this.txtPw.Font = new System.Drawing.Font("Consolas", 12F);
            this.txtPw.Location = new System.Drawing.Point(40, 178);
            this.txtPw.Name = "txtPw";
            this.txtPw.Size = new System.Drawing.Size(320, 32);
            this.txtPw.UseSystemPasswordChar = true;
            //
            // btnLogin
            //
            this.btnLogin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.btnLogin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogin.FlatAppearance.BorderSize = 0;
            this.btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogin.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.btnLogin.ForeColor = System.Drawing.Color.White;
            this.btnLogin.Location = new System.Drawing.Point(40, 232);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(320, 44);
            this.btnLogin.Text = "로그인";
            this.btnLogin.UseVisualStyleBackColor = false;
            //
            // lblMsg
            //
            this.lblMsg.AutoSize = true;
            this.lblMsg.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblMsg.ForeColor = System.Drawing.Color.IndianRed;
            this.lblMsg.Location = new System.Drawing.Point(40, 285);
            this.lblMsg.Name = "lblMsg";
            this.lblMsg.Text = "";
            //
            // SessionControl
            //
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.Controls.Add(this.pnlCard);
            this.Name = "SessionControl";
            this.Size = new System.Drawing.Size(1678, 900);
            this.pnlCard.ResumeLayout(false);
            this.pnlCard.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
