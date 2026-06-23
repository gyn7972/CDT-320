using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    partial class CalibrationSetupDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private Label lblPurposeHeader;
        private Label lblPurpose;
        private Label lblStorageHeader;
        private Label lblStorageGuide;
        private Label lblStatusHeader;
        private Label lblStatus;
        private TableLayoutPanel buttonLayout;
        private Button btnCheck;
        private Button btnClose;

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
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblPurposeHeader = new System.Windows.Forms.Label();
            this.lblPurpose = new System.Windows.Forms.Label();
            this.lblStorageHeader = new System.Windows.Forms.Label();
            this.lblStorageGuide = new System.Windows.Forms.Label();
            this.lblStatusHeader = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnCheck = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.buttonLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblTitle, 0, 0);
            this.rootLayout.Controls.Add(this.lblPurposeHeader, 0, 1);
            this.rootLayout.Controls.Add(this.lblPurpose, 0, 2);
            this.rootLayout.Controls.Add(this.lblStorageHeader, 0, 3);
            this.rootLayout.Controls.Add(this.lblStorageGuide, 0, 4);
            this.rootLayout.Controls.Add(this.lblStatusHeader, 0, 5);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 6);
            this.rootLayout.Controls.Add(this.buttonLayout, 0, 7);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(12, 12);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 8;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.rootLayout.Size = new System.Drawing.Size(776, 456);
            this.rootLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(776, 46);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "CALIBRATION";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPurposeHeader
            // 
            this.lblPurposeHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPurposeHeader.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblPurposeHeader.Location = new System.Drawing.Point(3, 46);
            this.lblPurposeHeader.Name = "lblPurposeHeader";
            this.lblPurposeHeader.Size = new System.Drawing.Size(770, 28);
            this.lblPurposeHeader.TabIndex = 1;
            this.lblPurposeHeader.Text = "목적";
            this.lblPurposeHeader.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblPurpose
            // 
            this.lblPurpose.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblPurpose.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPurpose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPurpose.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblPurpose.Location = new System.Drawing.Point(3, 74);
            this.lblPurpose.Name = "lblPurpose";
            this.lblPurpose.Padding = new System.Windows.Forms.Padding(10);
            this.lblPurpose.Size = new System.Drawing.Size(770, 88);
            this.lblPurpose.TabIndex = 2;
            this.lblPurpose.Text = "-";
            // 
            // lblStorageHeader
            // 
            this.lblStorageHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStorageHeader.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblStorageHeader.Location = new System.Drawing.Point(3, 162);
            this.lblStorageHeader.Name = "lblStorageHeader";
            this.lblStorageHeader.Size = new System.Drawing.Size(770, 28);
            this.lblStorageHeader.TabIndex = 3;
            this.lblStorageHeader.Text = "데이터 저장 위치 제안";
            this.lblStorageHeader.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblStorageGuide
            // 
            this.lblStorageGuide.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblStorageGuide.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStorageGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStorageGuide.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblStorageGuide.Location = new System.Drawing.Point(3, 190);
            this.lblStorageGuide.Name = "lblStorageGuide";
            this.lblStorageGuide.Padding = new System.Windows.Forms.Padding(10);
            this.lblStorageGuide.Size = new System.Drawing.Size(770, 88);
            this.lblStorageGuide.TabIndex = 4;
            this.lblStorageGuide.Text = "-";
            // 
            // lblStatusHeader
            // 
            this.lblStatusHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatusHeader.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatusHeader.Location = new System.Drawing.Point(3, 278);
            this.lblStatusHeader.Name = "lblStatusHeader";
            this.lblStatusHeader.Size = new System.Drawing.Size(770, 28);
            this.lblStatusHeader.TabIndex = 5;
            this.lblStatusHeader.Text = "상태";
            this.lblStatusHeader.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblStatus
            // 
            this.lblStatus.BackColor = System.Drawing.Color.White;
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblStatus.Location = new System.Drawing.Point(3, 306);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10);
            this.lblStatus.Size = new System.Drawing.Size(770, 94);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "-";
            // 
            // buttonLayout
            // 
            this.buttonLayout.ColumnCount = 3;
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.buttonLayout.Controls.Add(this.btnCheck, 1, 0);
            this.buttonLayout.Controls.Add(this.btnClose, 2, 0);
            this.buttonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonLayout.Location = new System.Drawing.Point(0, 400);
            this.buttonLayout.Margin = new System.Windows.Forms.Padding(0);
            this.buttonLayout.Name = "buttonLayout";
            this.buttonLayout.RowCount = 1;
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.Size = new System.Drawing.Size(776, 56);
            this.buttonLayout.TabIndex = 7;
            // 
            // btnCheck
            // 
            this.btnCheck.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnCheck.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCheck.FlatAppearance.BorderSize = 0;
            this.btnCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCheck.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnCheck.ForeColor = System.Drawing.Color.White;
            this.btnCheck.Location = new System.Drawing.Point(504, 8);
            this.btnCheck.Margin = new System.Windows.Forms.Padding(8);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Size = new System.Drawing.Size(144, 40);
            this.btnCheck.TabIndex = 0;
            this.btnCheck.Text = "CHECK READY";
            this.btnCheck.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnClose.Location = new System.Drawing.Point(664, 8);
            this.btnClose.Margin = new System.Windows.Forms.Padding(8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(104, 40);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // CalibrationSetupDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 480);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimizeBox = false;
            this.Name = "CalibrationSetupDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.ShowIcon = false;
            this.Text = "Calibration";
            this.rootLayout.ResumeLayout(false);
            this.buttonLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
