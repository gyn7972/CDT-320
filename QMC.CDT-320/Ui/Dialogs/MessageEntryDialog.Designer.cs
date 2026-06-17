namespace QMC.CDT_320.Ui.Dialogs
{
    partial class MessageEntryDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblCode;
        private System.Windows.Forms.TextBox txtCode;
        private System.Windows.Forms.Label lblKo;
        private System.Windows.Forms.TextBox txtKo;
        private System.Windows.Forms.Label lblEn;
        private System.Windows.Forms.TextBox txtEn;
        private System.Windows.Forms.Label lblKind;
        private System.Windows.Forms.ComboBox cmbKind;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblCode = new System.Windows.Forms.Label();
            this.txtCode = new System.Windows.Forms.TextBox();
            this.lblKo = new System.Windows.Forms.Label();
            this.txtKo = new System.Windows.Forms.TextBox();
            this.lblEn = new System.Windows.Forms.Label();
            this.txtEn = new System.Windows.Forms.TextBox();
            this.lblKind = new System.Windows.Forms.Label();
            this.cmbKind = new System.Windows.Forms.ComboBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblCode
            //
            this.lblCode.AutoSize = true;
            this.lblCode.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblCode.Location = new System.Drawing.Point(20, 25);
            this.lblCode.Name = "lblCode";
            this.lblCode.Size = new System.Drawing.Size(40, 19);
            this.lblCode.Text = "CODE";
            //
            // txtCode
            //
            this.txtCode.Font = new System.Drawing.Font("Consolas", 11F);
            this.txtCode.Location = new System.Drawing.Point(180, 22);
            this.txtCode.Name = "txtCode";
            this.txtCode.Size = new System.Drawing.Size(300, 30);
            //
            // lblKo
            //
            this.lblKo.AutoSize = true;
            this.lblKo.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblKo.Location = new System.Drawing.Point(20, 109);
            this.lblKo.Name = "lblKo";
            this.lblKo.Size = new System.Drawing.Size(70, 19);
            this.lblKo.Text = "DESCRIPTION (KO)";
            //
            // txtKo
            //
            this.txtKo.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.txtKo.Location = new System.Drawing.Point(180, 106);
            this.txtKo.Name = "txtKo";
            this.txtKo.Size = new System.Drawing.Size(300, 30);
            //
            // lblEn
            //
            this.lblEn.AutoSize = true;
            this.lblEn.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblEn.Location = new System.Drawing.Point(20, 151);
            this.lblEn.Name = "lblEn";
            this.lblEn.Size = new System.Drawing.Size(55, 19);
            this.lblEn.Text = "DESCRIPTION (EN)";
            //
            // txtEn
            //
            this.txtEn.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.txtEn.Font = new System.Drawing.Font("Consolas", 11F);
            this.txtEn.Location = new System.Drawing.Point(180, 148);
            this.txtEn.Name = "txtEn";
            this.txtEn.Size = new System.Drawing.Size(300, 30);
            //
            // lblKind
            //
            this.lblKind.AutoSize = true;
            this.lblKind.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblKind.Location = new System.Drawing.Point(20, 67);
            this.lblKind.Name = "lblKind";
            this.lblKind.Size = new System.Drawing.Size(40, 19);
            this.lblKind.Text = "KIND";
            //
            // cmbKind
            //
            this.cmbKind.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbKind.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.cmbKind.Location = new System.Drawing.Point(180, 64);
            this.cmbKind.Name = "cmbKind";
            this.cmbKind.Size = new System.Drawing.Size(160, 31);
            //
            // btnOk
            //
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.Location = new System.Drawing.Point(180, 200);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(135, 36);
            this.btnOk.Text = "확인";
            this.btnOk.UseVisualStyleBackColor = false;
            //
            // btnCancel
            //
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnCancel.Location = new System.Drawing.Point(345, 200);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(135, 36);
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // MessageEntryDialog
            //
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(500, 260);
            this.Controls.Add(this.lblCode);
            this.Controls.Add(this.txtCode);
            this.Controls.Add(this.lblKo);
            this.Controls.Add(this.txtKo);
            this.Controls.Add(this.lblEn);
            this.Controls.Add(this.txtEn);
            this.Controls.Add(this.lblKind);
            this.Controls.Add(this.cmbKind);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MessageEntryDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MESSAGE";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
