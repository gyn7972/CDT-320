namespace QMC.CDT_320.Ui.Controls
{
    partial class VisionMonitorControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Label lblModule;
        private System.Windows.Forms.ComboBox cbModule;
        private System.Windows.Forms.Label lblHost;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblStatus;
        private QMC.Common.Ui.Controls.CameraViewBase cameraView;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.topPanel = new System.Windows.Forms.Panel();
            this.lblModule = new System.Windows.Forms.Label();
            this.cbModule = new System.Windows.Forms.ComboBox();
            this.lblHost = new System.Windows.Forms.Label();
            this.txtHost = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.cameraView = new QMC.Common.Ui.Controls.CameraViewBase();
            this.topPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // topPanel
            // 
            this.topPanel.Controls.Add(this.lblModule);
            this.topPanel.Controls.Add(this.cbModule);
            this.topPanel.Controls.Add(this.lblHost);
            this.topPanel.Controls.Add(this.txtHost);
            this.topPanel.Controls.Add(this.btnConnect);
            this.topPanel.Controls.Add(this.btnDisconnect);
            this.topPanel.Controls.Add(this.lblStatus);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.topPanel.Location = new System.Drawing.Point(0, 0);
            this.topPanel.Name = "topPanel";
            this.topPanel.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.topPanel.Size = new System.Drawing.Size(1000, 42);
            this.topPanel.TabIndex = 0;
            // 
            // lblModule
            // 
            this.lblModule.AutoSize = true;
            this.lblModule.Location = new System.Drawing.Point(8, 13);
            this.lblModule.Name = "lblModule";
            this.lblModule.Size = new System.Drawing.Size(53, 15);
            this.lblModule.TabIndex = 0;
            this.lblModule.Text = "Module";
            // 
            // cbModule
            // 
            this.cbModule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbModule.FormattingEnabled = true;
            this.cbModule.Location = new System.Drawing.Point(67, 9);
            this.cbModule.Name = "cbModule";
            this.cbModule.Size = new System.Drawing.Size(230, 23);
            this.cbModule.TabIndex = 1;
            // 
            // lblHost
            // 
            this.lblHost.AutoSize = true;
            this.lblHost.Location = new System.Drawing.Point(312, 13);
            this.lblHost.Name = "lblHost";
            this.lblHost.Size = new System.Drawing.Size(54, 15);
            this.lblHost.TabIndex = 2;
            this.lblHost.Text = "Vision IP";
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(372, 9);
            this.txtHost.Name = "txtHost";
            this.txtHost.Size = new System.Drawing.Size(150, 23);
            this.txtHost.TabIndex = 3;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(534, 8);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(94, 25);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(634, 8);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(94, 25);
            this.btnDisconnect.TabIndex = 5;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblStatus.Location = new System.Drawing.Point(744, 13);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(24, 15);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "idle";
            // 
            // cameraView
            // 
            this.cameraView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cameraView.Location = new System.Drawing.Point(0, 42);
            this.cameraView.Name = "cameraView";
            this.cameraView.ShowToolbar = true;
            this.cameraView.Size = new System.Drawing.Size(1000, 718);
            this.cameraView.TabIndex = 1;
            // 
            // VisionMonitorControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.cameraView);
            this.Controls.Add(this.topPanel);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Name = "VisionMonitorControl";
            this.Size = new System.Drawing.Size(1000, 760);
            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
