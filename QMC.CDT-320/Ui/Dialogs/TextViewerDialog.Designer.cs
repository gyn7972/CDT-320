namespace QMC.CDT_320.Ui.Dialogs
{
    partial class TextViewerDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelBody = new System.Windows.Forms.Panel();
            this.txtContent = new System.Windows.Forms.TextBox();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.lblInfo = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panelHeader.SuspendLayout();
            this.panelBody.SuspendLayout();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            //
            // panelHeader
            //
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(724, 56);
            this.panelHeader.TabIndex = 0;
            //
            // lblTitle
            //
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(724, 56);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "DESCRIPTION";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // panelBody
            //
            this.panelBody.BackColor = System.Drawing.Color.White;
            this.panelBody.Controls.Add(this.txtContent);
            this.panelBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBody.Location = new System.Drawing.Point(0, 56);
            this.panelBody.Name = "panelBody";
            this.panelBody.Padding = new System.Windows.Forms.Padding(16, 14, 16, 14);
            this.panelBody.Size = new System.Drawing.Size(724, 364);
            this.panelBody.TabIndex = 1;
            //
            // txtContent
            //
            this.txtContent.BackColor = System.Drawing.Color.White;
            this.txtContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtContent.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.txtContent.HideSelection = false;
            this.txtContent.Location = new System.Drawing.Point(16, 14);
            this.txtContent.Multiline = true;
            this.txtContent.Name = "txtContent";
            this.txtContent.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtContent.Size = new System.Drawing.Size(692, 336);
            this.txtContent.TabIndex = 0;
            //
            // panelButtons
            //
            this.panelButtons.BackColor = System.Drawing.Color.FromArgb(236, 240, 241);
            this.panelButtons.Controls.Add(this.lblInfo);
            this.panelButtons.Controls.Add(this.btnOk);
            this.panelButtons.Controls.Add(this.btnCancel);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(0, 420);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(724, 60);
            this.panelButtons.TabIndex = 2;
            //
            // lblInfo
            //
            this.lblInfo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInfo.AutoSize = true;
            this.lblInfo.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
            this.lblInfo.Location = new System.Drawing.Point(18, 22);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(0, 15);
            this.lblInfo.TabIndex = 2;
            //
            // btnOk
            //
            this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.Location = new System.Drawing.Point(608, 12);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 36);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "확인";
            this.btnOk.UseVisualStyleBackColor = false;
            //
            // btnCancel
            //
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(44, 62, 80);
            this.btnCancel.Location = new System.Drawing.Point(500, 12);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 36);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = false;
            //
            // TextViewerDialog
            //
            this.AcceptButton = this.btnOk;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(724, 480);
            this.Controls.Add(this.panelBody);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelButtons);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(420, 300);
            this.Name = "TextViewerDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "DESCRIPTION";
            this.panelHeader.ResumeLayout(false);
            this.panelBody.ResumeLayout(false);
            this.panelBody.PerformLayout();
            this.panelButtons.ResumeLayout(false);
            this.panelButtons.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panelBody;
        private System.Windows.Forms.TextBox txtContent;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
