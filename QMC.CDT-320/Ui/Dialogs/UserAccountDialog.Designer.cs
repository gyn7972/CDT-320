namespace QMC.CDT_320.Ui.Dialogs
{
    partial class UserAccountDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblId;
        private System.Windows.Forms.TextBox txtId;
        private System.Windows.Forms.Label lblPw;
        private System.Windows.Forms.TextBox txtPw;
        private System.Windows.Forms.Label lblLevel;
        private System.Windows.Forms.ComboBox cmbLevel;
        private System.Windows.Forms.CheckBox chkEnabled;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblId = new System.Windows.Forms.Label();
            this.txtId = new System.Windows.Forms.TextBox();
            this.lblPw = new System.Windows.Forms.Label();
            this.txtPw = new System.Windows.Forms.TextBox();
            this.lblLevel = new System.Windows.Forms.Label();
            this.cmbLevel = new System.Windows.Forms.ComboBox();
            this.chkEnabled = new System.Windows.Forms.CheckBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblId
            //
            this.lblId.AutoSize = true;
            this.lblId.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblId.Location = new System.Drawing.Point(20, 26);
            this.lblId.Text = "ID";
            //
            // txtId
            //
            this.txtId.Font = new System.Drawing.Font("Consolas", 11F);
            this.txtId.Location = new System.Drawing.Point(175, 22);
            this.txtId.Name = "txtId";
            this.txtId.Size = new System.Drawing.Size(250, 30);
            //
            // lblPw
            //
            this.lblPw.AutoSize = true;
            this.lblPw.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblPw.Location = new System.Drawing.Point(20, 68);
            this.lblPw.Text = "PASSWORD";
            //
            // txtPw
            //
            this.txtPw.Font = new System.Drawing.Font("Consolas", 11F);
            this.txtPw.Location = new System.Drawing.Point(175, 64);
            this.txtPw.Name = "txtPw";
            this.txtPw.Size = new System.Drawing.Size(250, 30);
            this.txtPw.UseSystemPasswordChar = true;
            //
            // lblLevel
            //
            this.lblLevel.AutoSize = true;
            this.lblLevel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblLevel.Location = new System.Drawing.Point(20, 110);
            this.lblLevel.Text = "LEVEL";
            //
            // cmbLevel
            //
            this.cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLevel.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.cmbLevel.Location = new System.Drawing.Point(175, 106);
            this.cmbLevel.Name = "cmbLevel";
            this.cmbLevel.Size = new System.Drawing.Size(200, 31);
            //
            // chkEnabled
            //
            this.chkEnabled.AutoSize = true;
            this.chkEnabled.Checked = true;
            this.chkEnabled.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.chkEnabled.Location = new System.Drawing.Point(175, 150);
            this.chkEnabled.Name = "chkEnabled";
            this.chkEnabled.Text = "사용(활성)";
            //
            // btnOk
            //
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.Location = new System.Drawing.Point(175, 195);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(120, 36);
            this.btnOk.Text = "확인";
            this.btnOk.UseVisualStyleBackColor = false;
            //
            // btnCancel
            //
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnCancel.Location = new System.Drawing.Point(305, 195);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(120, 36);
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // UserAccountDialog
            //
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(460, 255);
            this.Controls.Add(this.lblId);
            this.Controls.Add(this.txtId);
            this.Controls.Add(this.lblPw);
            this.Controls.Add(this.txtPw);
            this.Controls.Add(this.lblLevel);
            this.Controls.Add(this.cmbLevel);
            this.Controls.Add(this.chkEnabled);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserAccountDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "USER";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
