namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class SimulatorLinkPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.GroupBox grpLink;
        private System.Windows.Forms.TableLayoutPanel linkLayout;
        private System.Windows.Forms.Label lblHost;
        private System.Windows.Forms.TextBox _tbHost;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox _tbPort;
        private System.Windows.Forms.Button _btnConnect;
        private System.Windows.Forms.Label lblConnStatus;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.RichTextBox _txtLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.grpLink = new System.Windows.Forms.GroupBox();
            this.linkLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHost = new System.Windows.Forms.Label();
            this._tbHost = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this._tbPort = new System.Windows.Forms.TextBox();
            this._btnConnect = new System.Windows.Forms.Button();
            this.lblConnStatus = new System.Windows.Forms.Label();
            this._lblStatus = new System.Windows.Forms.Label();
            this._txtLog = new System.Windows.Forms.RichTextBox();
            this.rootLayout.SuspendLayout();
            this.grpLink.SuspendLayout();
            this.linkLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.grpLink, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1416, 980);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Location = new System.Drawing.Point(8, 8);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 28);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "SIMULATOR";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpLink
            // 
            this.grpLink.Controls.Add(this.linkLayout);
            this.grpLink.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLink.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.grpLink.Location = new System.Drawing.Point(8, 40);
            this.grpLink.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpLink.Name = "grpLink";
            this.grpLink.Padding = new System.Windows.Forms.Padding(10);
            this.grpLink.Size = new System.Drawing.Size(1400, 292);
            this.grpLink.TabIndex = 1;
            this.grpLink.TabStop = false;
            this.grpLink.Text = "Simulator";
            // 
            // linkLayout
            // 
            this.linkLayout.ColumnCount = 5;
            this.linkLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.linkLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.linkLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.linkLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.linkLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.linkLayout.Controls.Add(this.lblHost, 0, 0);
            this.linkLayout.Controls.Add(this._tbHost, 1, 0);
            this.linkLayout.Controls.Add(this.lblPort, 2, 0);
            this.linkLayout.Controls.Add(this._tbPort, 3, 0);
            this.linkLayout.Controls.Add(this._btnConnect, 4, 0);
            this.linkLayout.Controls.Add(this.lblConnStatus, 0, 1);
            this.linkLayout.Controls.Add(this._lblStatus, 1, 1);
            this.linkLayout.Controls.Add(this._txtLog, 0, 2);
            this.linkLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.linkLayout.Location = new System.Drawing.Point(10, 33);
            this.linkLayout.Name = "linkLayout";
            this.linkLayout.RowCount = 3;
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.linkLayout.Size = new System.Drawing.Size(1380, 249);
            this.linkLayout.TabIndex = 0;
            // 
            // lblHost
            // 
            this.lblHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHost.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblHost.Location = new System.Drawing.Point(3, 0);
            this.lblHost.Name = "lblHost";
            this.lblHost.Size = new System.Drawing.Size(74, 40);
            this.lblHost.TabIndex = 0;
            this.lblHost.Text = "Host";
            this.lblHost.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbHost
            // 
            this._tbHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbHost.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._tbHost.Location = new System.Drawing.Point(83, 3);
            this._tbHost.Name = "_tbHost";
            this._tbHost.Size = new System.Drawing.Size(174, 27);
            this._tbHost.TabIndex = 1;
            this._tbHost.Text = "127.0.0.1";
            // 
            // lblPort
            // 
            this.lblPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPort.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPort.Location = new System.Drawing.Point(263, 0);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(64, 40);
            this.lblPort.TabIndex = 2;
            this.lblPort.Text = "Port";
            this.lblPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbPort
            // 
            this._tbPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbPort.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._tbPort.Location = new System.Drawing.Point(333, 3);
            this._tbPort.Name = "_tbPort";
            this._tbPort.Size = new System.Drawing.Size(94, 27);
            this._tbPort.TabIndex = 3;
            this._tbPort.Text = "7001";
            // 
            // _btnConnect
            // 
            this._btnConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnConnect.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._btnConnect.Location = new System.Drawing.Point(433, 3);
            this._btnConnect.Name = "_btnConnect";
            this._btnConnect.Size = new System.Drawing.Size(944, 34);
            this._btnConnect.TabIndex = 4;
            this._btnConnect.Text = "CONNECT";
            // 
            // lblConnStatus
            // 
            this.lblConnStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblConnStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblConnStatus.Location = new System.Drawing.Point(3, 40);
            this.lblConnStatus.Name = "lblConnStatus";
            this.lblConnStatus.Size = new System.Drawing.Size(74, 36);
            this.lblConnStatus.TabIndex = 5;
            this.lblConnStatus.Text = "Status";
            this.lblConnStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblStatus
            // 
            this.linkLayout.SetColumnSpan(this._lblStatus, 4);
            this._lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblStatus.ForeColor = System.Drawing.Color.IndianRed;
            this._lblStatus.Location = new System.Drawing.Point(83, 40);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(1294, 36);
            this._lblStatus.TabIndex = 6;
            this._lblStatus.Text = "Disconnected";
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtLog
            // 
            this._txtLog.BackColor = System.Drawing.Color.Black;
            this.linkLayout.SetColumnSpan(this._txtLog, 5);
            this._txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this._txtLog.ForeColor = System.Drawing.Color.LightGray;
            this._txtLog.Location = new System.Drawing.Point(3, 79);
            this._txtLog.Name = "_txtLog";
            this._txtLog.ReadOnly = true;
            this._txtLog.Size = new System.Drawing.Size(1374, 167);
            this._txtLog.TabIndex = 7;
            this._txtLog.Text = "";
            this._txtLog.WordWrap = false;
            // 
            // SimulatorLinkPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "SimulatorLinkPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.grpLink.ResumeLayout(false);
            this.linkLayout.ResumeLayout(false);
            this.linkLayout.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
