namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class PickerHeadDieDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel infoLayout;
        private System.Windows.Forms.Label lblDieIdTitle;
        private System.Windows.Forms.Label lblDieIdValue;
        private System.Windows.Forms.Label lblWaferTitle;
        private System.Windows.Forms.Label lblWaferValue;
        private System.Windows.Forms.Label lblSequenceTitle;
        private System.Windows.Forms.Label lblSequenceValue;
        private System.Windows.Forms.Label lblMapTitle;
        private System.Windows.Forms.Label lblMapValue;
        private System.Windows.Forms.Label lblLocationTitle;
        private System.Windows.Forms.Label lblLocationValue;
        private System.Windows.Forms.Label lblResultTitle;
        private System.Windows.Forms.ComboBox cmbResult;
        private System.Windows.Forms.CheckBox chkInputTarget;
        private System.Windows.Forms.Label lblNgCodeTitle;
        private System.Windows.Forms.TextBox txtNgCode;
        private System.Windows.Forms.Label lblReasonTitle;
        private System.Windows.Forms.TextBox txtReason;
        private System.Windows.Forms.FlowLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.infoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblDieIdTitle = new System.Windows.Forms.Label();
            this.lblDieIdValue = new System.Windows.Forms.Label();
            this.lblWaferTitle = new System.Windows.Forms.Label();
            this.lblWaferValue = new System.Windows.Forms.Label();
            this.lblSequenceTitle = new System.Windows.Forms.Label();
            this.lblSequenceValue = new System.Windows.Forms.Label();
            this.lblMapTitle = new System.Windows.Forms.Label();
            this.lblMapValue = new System.Windows.Forms.Label();
            this.lblLocationTitle = new System.Windows.Forms.Label();
            this.lblLocationValue = new System.Windows.Forms.Label();
            this.lblResultTitle = new System.Windows.Forms.Label();
            this.cmbResult = new System.Windows.Forms.ComboBox();
            this.chkInputTarget = new System.Windows.Forms.CheckBox();
            this.lblNgCodeTitle = new System.Windows.Forms.Label();
            this.txtNgCode = new System.Windows.Forms.TextBox();
            this.lblReasonTitle = new System.Windows.Forms.Label();
            this.txtReason = new System.Windows.Forms.TextBox();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblTitle, 0, 0);
            this.rootLayout.Controls.Add(this.infoLayout, 0, 1);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(12);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.rootLayout.Size = new System.Drawing.Size(520, 430);
            this.rootLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(230, 126, 0);
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(12, 12);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(496, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Picker Head Die";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // infoLayout
            // 
            this.infoLayout.ColumnCount = 2;
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoLayout.Controls.Add(this.lblDieIdTitle, 0, 0);
            this.infoLayout.Controls.Add(this.lblDieIdValue, 1, 0);
            this.infoLayout.Controls.Add(this.lblWaferTitle, 0, 1);
            this.infoLayout.Controls.Add(this.lblWaferValue, 1, 1);
            this.infoLayout.Controls.Add(this.lblSequenceTitle, 0, 2);
            this.infoLayout.Controls.Add(this.lblSequenceValue, 1, 2);
            this.infoLayout.Controls.Add(this.lblMapTitle, 0, 3);
            this.infoLayout.Controls.Add(this.lblMapValue, 1, 3);
            this.infoLayout.Controls.Add(this.lblLocationTitle, 0, 4);
            this.infoLayout.Controls.Add(this.lblLocationValue, 1, 4);
            this.infoLayout.Controls.Add(this.lblResultTitle, 0, 5);
            this.infoLayout.Controls.Add(this.cmbResult, 1, 5);
            this.infoLayout.Controls.Add(this.chkInputTarget, 1, 6);
            this.infoLayout.Controls.Add(this.lblNgCodeTitle, 0, 7);
            this.infoLayout.Controls.Add(this.txtNgCode, 1, 7);
            this.infoLayout.Controls.Add(this.lblReasonTitle, 0, 8);
            this.infoLayout.Controls.Add(this.txtReason, 1, 8);
            this.infoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLayout.Location = new System.Drawing.Point(12, 52);
            this.infoLayout.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.infoLayout.Name = "infoLayout";
            this.infoLayout.RowCount = 10;
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoLayout.Size = new System.Drawing.Size(496, 318);
            this.infoLayout.TabIndex = 1;
            // 
            // lblDieIdTitle
            // 
            this.lblDieIdTitle.BackColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblDieIdTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieIdTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieIdTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblDieIdTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblDieIdTitle.Name = "lblDieIdTitle";
            this.lblDieIdTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDieIdTitle.TabIndex = 0;
            this.lblDieIdTitle.Text = "Die ID";
            this.lblDieIdTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDieIdValue
            // 
            this.lblDieIdValue.BackColor = System.Drawing.Color.White;
            this.lblDieIdValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieIdValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieIdValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblDieIdValue.Name = "lblDieIdValue";
            this.lblDieIdValue.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDieIdValue.TabIndex = 1;
            this.lblDieIdValue.Text = "-";
            this.lblDieIdValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaferTitle
            // 
            this.lblWaferTitle.BackColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblWaferTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblWaferTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblWaferTitle.Name = "lblWaferTitle";
            this.lblWaferTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblWaferTitle.TabIndex = 2;
            this.lblWaferTitle.Text = "Wafer";
            this.lblWaferTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaferValue
            // 
            this.lblWaferValue.BackColor = System.Drawing.Color.White;
            this.lblWaferValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblWaferValue.Name = "lblWaferValue";
            this.lblWaferValue.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblWaferValue.TabIndex = 3;
            this.lblWaferValue.Text = "-";
            this.lblWaferValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSequenceTitle
            // 
            this.lblSequenceTitle.BackColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblSequenceTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSequenceTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSequenceTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblSequenceTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblSequenceTitle.Name = "lblSequenceTitle";
            this.lblSequenceTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblSequenceTitle.TabIndex = 4;
            this.lblSequenceTitle.Text = "Sequence";
            this.lblSequenceTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSequenceValue
            // 
            this.lblSequenceValue.BackColor = System.Drawing.Color.White;
            this.lblSequenceValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSequenceValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSequenceValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblSequenceValue.Name = "lblSequenceValue";
            this.lblSequenceValue.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblSequenceValue.TabIndex = 5;
            this.lblSequenceValue.Text = "-";
            this.lblSequenceValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMapTitle
            // 
            this.lblMapTitle.BackColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblMapTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMapTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblMapTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblMapTitle.Name = "lblMapTitle";
            this.lblMapTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblMapTitle.TabIndex = 6;
            this.lblMapTitle.Text = "Map X/Y";
            this.lblMapTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMapValue
            // 
            this.lblMapValue.BackColor = System.Drawing.Color.White;
            this.lblMapValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMapValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblMapValue.Name = "lblMapValue";
            this.lblMapValue.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblMapValue.TabIndex = 7;
            this.lblMapValue.Text = "-";
            this.lblMapValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLocationTitle
            // 
            this.lblLocationTitle.BackColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblLocationTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLocationTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLocationTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblLocationTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblLocationTitle.Name = "lblLocationTitle";
            this.lblLocationTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLocationTitle.TabIndex = 8;
            this.lblLocationTitle.Text = "Location";
            this.lblLocationTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLocationValue
            // 
            this.lblLocationValue.BackColor = System.Drawing.Color.White;
            this.lblLocationValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLocationValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLocationValue.Margin = new System.Windows.Forms.Padding(0);
            this.lblLocationValue.Name = "lblLocationValue";
            this.lblLocationValue.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLocationValue.TabIndex = 9;
            this.lblLocationValue.Text = "-";
            this.lblLocationValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblResultTitle
            // 
            this.lblResultTitle.BackColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblResultTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblResultTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblResultTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblResultTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblResultTitle.Name = "lblResultTitle";
            this.lblResultTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblResultTitle.TabIndex = 10;
            this.lblResultTitle.Text = "Result";
            this.lblResultTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbResult.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResult.Items.AddRange(new object[] { "Unknown", "Good", "NG" });
            this.cmbResult.Location = new System.Drawing.Point(153, 163);
            this.cmbResult.Name = "cmbResult";
            this.cmbResult.Size = new System.Drawing.Size(340, 23);
            this.cmbResult.TabIndex = 11;
            this.chkInputTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkInputTarget.Location = new System.Drawing.Point(153, 195);
            this.chkInputTarget.Name = "chkInputTarget";
            this.chkInputTarget.Size = new System.Drawing.Size(340, 26);
            this.chkInputTarget.TabIndex = 12;
            this.chkInputTarget.Text = "Input Target";
            this.chkInputTarget.UseVisualStyleBackColor = true;
            // 
            // lblNgCodeTitle
            // 
            this.lblNgCodeTitle.BackColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblNgCodeTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNgCodeTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNgCodeTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNgCodeTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblNgCodeTitle.Name = "lblNgCodeTitle";
            this.lblNgCodeTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblNgCodeTitle.TabIndex = 13;
            this.lblNgCodeTitle.Text = "NG Code";
            this.lblNgCodeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtNgCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNgCode.Location = new System.Drawing.Point(153, 227);
            this.txtNgCode.Name = "txtNgCode";
            this.txtNgCode.Size = new System.Drawing.Size(340, 23);
            this.txtNgCode.TabIndex = 14;
            // 
            // lblReasonTitle
            // 
            this.lblReasonTitle.BackColor = System.Drawing.Color.FromArgb(200, 200, 200);
            this.lblReasonTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblReasonTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblReasonTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblReasonTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblReasonTitle.Name = "lblReasonTitle";
            this.lblReasonTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblReasonTitle.TabIndex = 15;
            this.lblReasonTitle.Text = "Reason";
            this.lblReasonTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtReason.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtReason.Location = new System.Drawing.Point(153, 259);
            this.txtReason.Name = "txtReason";
            this.txtReason.Size = new System.Drawing.Size(340, 23);
            this.txtReason.TabIndex = 16;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnClose);
            this.buttonPanel.Controls.Add(this.btnClear);
            this.buttonPanel.Controls.Add(this.btnApply);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Location = new System.Drawing.Point(12, 370);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(0);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.buttonPanel.Size = new System.Drawing.Size(496, 48);
            this.buttonPanel.TabIndex = 2;
            // 
            // buttons
            // 
            this.btnApply.Location = new System.Drawing.Point(167, 11);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(100, 30);
            this.btnApply.TabIndex = 4;
            this.btnApply.Text = "APPLY";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            this.btnClear.Location = new System.Drawing.Point(273, 11);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(100, 30);
            this.btnClear.TabIndex = 5;
            this.btnClear.Text = "CLEAR";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            this.btnClose.Location = new System.Drawing.Point(379, 11);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 30);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // PickerHeadDieDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 430);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PickerHeadDieDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.rootLayout.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.infoLayout.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

    }
}

