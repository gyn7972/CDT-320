namespace QMC.MilCameraTest
{
    partial class MainForm
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblCameraId = new System.Windows.Forms.Label();
            this.txtCameraId = new System.Windows.Forms.TextBox();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnGrab = new System.Windows.Forms.Button();
            this.btnLive = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblExposureEnd = new System.Windows.Forms.Label();
            this.picView = new System.Windows.Forms.PictureBox();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picView)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.lblCameraId);
            this.panelTop.Controls.Add(this.txtCameraId);
            this.panelTop.Controls.Add(this.btnOpen);
            this.panelTop.Controls.Add(this.btnClose);
            this.panelTop.Controls.Add(this.btnGrab);
            this.panelTop.Controls.Add(this.btnLive);
            this.panelTop.Controls.Add(this.lblStatus);
            this.panelTop.Controls.Add(this.lblExposureEnd);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(984, 44);
            this.panelTop.TabIndex = 0;
            // 
            // lblCameraId
            // 
            this.lblCameraId.AutoSize = true;
            this.lblCameraId.Location = new System.Drawing.Point(10, 14);
            this.lblCameraId.Name = "lblCameraId";
            this.lblCameraId.Size = new System.Drawing.Size(62, 15);
            this.lblCameraId.TabIndex = 0;
            this.lblCameraId.Text = "Camera Id";
            // 
            // txtCameraId
            // 
            this.txtCameraId.Location = new System.Drawing.Point(78, 10);
            this.txtCameraId.Name = "txtCameraId";
            this.txtCameraId.Size = new System.Drawing.Size(90, 23);
            this.txtCameraId.TabIndex = 1;
            this.txtCameraId.Text = "Mil/0";
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(180, 8);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(80, 28);
            this.btnOpen.TabIndex = 2;
            this.btnOpen.Text = "Open";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // btnClose
            // 
            this.btnClose.Enabled = false;
            this.btnClose.Location = new System.Drawing.Point(266, 8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 28);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnGrab
            // 
            this.btnGrab.Enabled = false;
            this.btnGrab.Location = new System.Drawing.Point(362, 8);
            this.btnGrab.Name = "btnGrab";
            this.btnGrab.Size = new System.Drawing.Size(80, 28);
            this.btnGrab.TabIndex = 4;
            this.btnGrab.Text = "Grab";
            this.btnGrab.UseVisualStyleBackColor = true;
            this.btnGrab.Click += new System.EventHandler(this.btnGrab_Click);
            // 
            // btnLive
            // 
            this.btnLive.Enabled = false;
            this.btnLive.Location = new System.Drawing.Point(448, 8);
            this.btnLive.Name = "btnLive";
            this.btnLive.Size = new System.Drawing.Size(80, 28);
            this.btnLive.TabIndex = 5;
            this.btnLive.Text = "Live";
            this.btnLive.UseVisualStyleBackColor = true;
            this.btnLive.Click += new System.EventHandler(this.btnLive_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblStatus.Location = new System.Drawing.Point(544, 14);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(51, 15);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "Closed";
            // 
            // lblExposureEnd
            // 
            this.lblExposureEnd.AutoSize = true;
            this.lblExposureEnd.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblExposureEnd.Location = new System.Drawing.Point(680, 14);
            this.lblExposureEnd.Name = "lblExposureEnd";
            this.lblExposureEnd.Size = new System.Drawing.Size(96, 15);
            this.lblExposureEnd.TabIndex = 7;
            this.lblExposureEnd.Text = "ExposureEnd: 0";
            // 
            // picView
            // 
            this.picView.BackColor = System.Drawing.Color.Black;
            this.picView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picView.Location = new System.Drawing.Point(0, 44);
            this.picView.Name = "picView";
            this.picView.Size = new System.Drawing.Size(984, 507);
            this.picView.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picView.TabIndex = 1;
            this.picView.TabStop = false;
            // 
            // lstLog
            // 
            this.lstLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lstLog.FormattingEnabled = true;
            this.lstLog.HorizontalScrollbar = true;
            this.lstLog.IntegralHeight = false;
            this.lstLog.ItemHeight = 15;
            this.lstLog.Location = new System.Drawing.Point(0, 551);
            this.lstLog.Name = "lstLog";
            this.lstLog.Size = new System.Drawing.Size(984, 110);
            this.lstLog.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 661);
            this.Controls.Add(this.picView);
            this.Controls.Add(this.lstLog);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "QMC MilCamera Test — Grab / Live / ExposureEnd";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picView)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblCameraId;
        private System.Windows.Forms.TextBox txtCameraId;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnGrab;
        private System.Windows.Forms.Button btnLive;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblExposureEnd;
        private System.Windows.Forms.PictureBox picView;
        private System.Windows.Forms.ListBox lstLog;
    }
}
