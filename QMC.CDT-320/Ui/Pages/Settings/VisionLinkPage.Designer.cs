namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class VisionLinkPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.GroupBox grpLink;
        private System.Windows.Forms.TableLayoutPanel linkLayout;
        private System.Windows.Forms.Label lblHost;
        private System.Windows.Forms.TextBox _tbHost;

        private System.Windows.Forms.Label lblWaferPort;
        private System.Windows.Forms.TextBox _tbWafer;
        private System.Windows.Forms.Label _lblWafer;

        private System.Windows.Forms.Label lblInspectionPort;
        private System.Windows.Forms.TextBox _tbInsp;
        private System.Windows.Forms.Label _lblInsp;

        private System.Windows.Forms.Label lblBinPort;
        private System.Windows.Forms.TextBox _tbBin;
        private System.Windows.Forms.Label _lblBin;

        private System.Windows.Forms.Label lblMainPort;
        private System.Windows.Forms.TextBox _tbMain;
        private System.Windows.Forms.Label _lblMain;

        private System.Windows.Forms.Label lblTopPort;
        private System.Windows.Forms.TextBox _tbTop;
        private System.Windows.Forms.Label _lblTop;

        private System.Windows.Forms.Label lblBotPort;
        private System.Windows.Forms.TextBox _tbBot;
        private System.Windows.Forms.Label _lblBot;

        private System.Windows.Forms.CheckBox _cbAuto;
        private System.Windows.Forms.TableLayoutPanel buttonLayout;
        private System.Windows.Forms.Button _btnConnect;
        private System.Windows.Forms.Button _btnDisconnect;
        private System.Windows.Forms.Button _btnPing;
        private System.Windows.Forms.Label lblHint;

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
            this.lblWaferPort = new System.Windows.Forms.Label();
            this._tbWafer = new System.Windows.Forms.TextBox();
            this._lblWafer = new System.Windows.Forms.Label();
            this.lblInspectionPort = new System.Windows.Forms.Label();
            this._tbInsp = new System.Windows.Forms.TextBox();
            this._lblInsp = new System.Windows.Forms.Label();
            this.lblBinPort = new System.Windows.Forms.Label();
            this._tbBin = new System.Windows.Forms.TextBox();
            this._lblBin = new System.Windows.Forms.Label();
            this.lblMainPort = new System.Windows.Forms.Label();
            this._tbMain = new System.Windows.Forms.TextBox();
            this._lblMain = new System.Windows.Forms.Label();
            this.lblTopPort = new System.Windows.Forms.Label();
            this._tbTop = new System.Windows.Forms.TextBox();
            this._lblTop = new System.Windows.Forms.Label();
            this.lblBotPort = new System.Windows.Forms.Label();
            this._tbBot = new System.Windows.Forms.TextBox();
            this._lblBot = new System.Windows.Forms.Label();
            this._cbAuto = new System.Windows.Forms.CheckBox();
            this.buttonLayout = new System.Windows.Forms.TableLayoutPanel();
            this._btnConnect = new System.Windows.Forms.Button();
            this._btnDisconnect = new System.Windows.Forms.Button();
            this._btnPing = new System.Windows.Forms.Button();
            this.lblHint = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.grpLink.SuspendLayout();
            this.linkLayout.SuspendLayout();
            this.buttonLayout.SuspendLayout();
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
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 470F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1416, 980);
            this.rootLayout.TabIndex = 0;
            //
            // lblHeader
            //
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "VISION LINK";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grpLink
            //
            this.grpLink.Controls.Add(this.linkLayout);
            this.grpLink.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLink.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpLink.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpLink.Name = "grpLink";
            this.grpLink.Padding = new System.Windows.Forms.Padding(10);
            this.grpLink.Size = new System.Drawing.Size(1400, 462);
            this.grpLink.TabIndex = 1;
            this.grpLink.TabStop = false;
            this.grpLink.Text = "QMC.Vision TCP Link (6 channels)";
            //
            // linkLayout
            //
            this.linkLayout.ColumnCount = 3;
            this.linkLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.linkLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.linkLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.linkLayout.Controls.Add(this.lblHost, 0, 0);
            this.linkLayout.Controls.Add(this._tbHost, 1, 0);
            this.linkLayout.Controls.Add(this.lblWaferPort, 0, 1);
            this.linkLayout.Controls.Add(this._tbWafer, 1, 1);
            this.linkLayout.Controls.Add(this._lblWafer, 2, 1);
            this.linkLayout.Controls.Add(this.lblInspectionPort, 0, 2);
            this.linkLayout.Controls.Add(this._tbInsp, 1, 2);
            this.linkLayout.Controls.Add(this._lblInsp, 2, 2);
            this.linkLayout.Controls.Add(this.lblBinPort, 0, 3);
            this.linkLayout.Controls.Add(this._tbBin, 1, 3);
            this.linkLayout.Controls.Add(this._lblBin, 2, 3);
            this.linkLayout.Controls.Add(this.lblMainPort, 0, 4);
            this.linkLayout.Controls.Add(this._tbMain, 1, 4);
            this.linkLayout.Controls.Add(this._lblMain, 2, 4);
            this.linkLayout.Controls.Add(this.lblTopPort, 0, 5);
            this.linkLayout.Controls.Add(this._tbTop, 1, 5);
            this.linkLayout.Controls.Add(this._lblTop, 2, 5);
            this.linkLayout.Controls.Add(this.lblBotPort, 0, 6);
            this.linkLayout.Controls.Add(this._tbBot, 1, 6);
            this.linkLayout.Controls.Add(this._lblBot, 2, 6);
            this.linkLayout.Controls.Add(this._cbAuto, 0, 7);
            this.linkLayout.Controls.Add(this.buttonLayout, 0, 8);
            this.linkLayout.Controls.Add(this.lblHint, 0, 9);
            this.linkLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.linkLayout.Name = "linkLayout";
            this.linkLayout.RowCount = 10;
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.linkLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.linkLayout.Size = new System.Drawing.Size(1380, 419);
            this.linkLayout.TabIndex = 0;
            //
            // lblHost
            //
            this.lblHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHost.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblHost.Name = "lblHost";
            this.lblHost.TabIndex = 0;
            this.lblHost.Text = "Vision PC IP";
            this.lblHost.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbHost
            //
            this._tbHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbHost.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._tbHost.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._tbHost.Name = "_tbHost";
            this._tbHost.TabIndex = 1;
            //
            // lblWaferPort
            //
            this.lblWaferPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferPort.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblWaferPort.Name = "lblWaferPort";
            this.lblWaferPort.TabIndex = 2;
            this.lblWaferPort.Text = "Wafer";
            this.lblWaferPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbWafer
            //
            this._tbWafer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbWafer.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._tbWafer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._tbWafer.Name = "_tbWafer";
            this._tbWafer.TabIndex = 3;
            //
            // _lblWafer
            //
            this._lblWafer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblWafer.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this._lblWafer.ForeColor = System.Drawing.Color.Gray;
            this._lblWafer.Name = "_lblWafer";
            this._lblWafer.TabIndex = 4;
            this._lblWafer.Text = "● 미연결";
            this._lblWafer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblInspectionPort
            //
            this.lblInspectionPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInspectionPort.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblInspectionPort.Name = "lblInspectionPort";
            this.lblInspectionPort.TabIndex = 5;
            this.lblInspectionPort.Text = "Bottom Inspection";
            this.lblInspectionPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbInsp
            //
            this._tbInsp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbInsp.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._tbInsp.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._tbInsp.Name = "_tbInsp";
            this._tbInsp.TabIndex = 6;
            //
            // _lblInsp
            //
            this._lblInsp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblInsp.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this._lblInsp.ForeColor = System.Drawing.Color.Gray;
            this._lblInsp.Name = "_lblInsp";
            this._lblInsp.TabIndex = 7;
            this._lblInsp.Text = "● 미연결";
            this._lblInsp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblBinPort
            //
            this.lblBinPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinPort.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblBinPort.Name = "lblBinPort";
            this.lblBinPort.TabIndex = 8;
            this.lblBinPort.Text = "Bin";
            this.lblBinPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbBin
            //
            this._tbBin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbBin.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._tbBin.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._tbBin.Name = "_tbBin";
            this._tbBin.TabIndex = 9;
            //
            // _lblBin
            //
            this._lblBin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblBin.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this._lblBin.ForeColor = System.Drawing.Color.Gray;
            this._lblBin.Name = "_lblBin";
            this._lblBin.TabIndex = 10;
            this._lblBin.Text = "● 미연결";
            this._lblBin.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblMainPort
            //
            this.lblMainPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMainPort.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblMainPort.Name = "lblMainPort";
            this.lblMainPort.TabIndex = 11;
            this.lblMainPort.Text = "Main (Recipe)";
            this.lblMainPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbMain
            //
            this._tbMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbMain.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._tbMain.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._tbMain.Name = "_tbMain";
            this._tbMain.TabIndex = 12;
            //
            // _lblMain
            //
            this._lblMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblMain.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this._lblMain.ForeColor = System.Drawing.Color.Gray;
            this._lblMain.Name = "_lblMain";
            this._lblMain.TabIndex = 13;
            this._lblMain.Text = "● 미연결";
            this._lblMain.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblTopPort
            //
            this.lblTopPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTopPort.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblTopPort.Name = "lblTopPort";
            this.lblTopPort.TabIndex = 14;
            this.lblTopPort.Text = "Top Side";
            this.lblTopPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbTop
            //
            this._tbTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbTop.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._tbTop.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._tbTop.Name = "_tbTop";
            this._tbTop.TabIndex = 15;
            //
            // _lblTop
            //
            this._lblTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblTop.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this._lblTop.ForeColor = System.Drawing.Color.Gray;
            this._lblTop.Name = "_lblTop";
            this._lblTop.TabIndex = 16;
            this._lblTop.Text = "● 미연결";
            this._lblTop.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblBotPort
            //
            this.lblBotPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBotPort.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblBotPort.Name = "lblBotPort";
            this.lblBotPort.TabIndex = 17;
            this.lblBotPort.Text = "Bottom Side";
            this.lblBotPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbBot
            //
            this._tbBot.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbBot.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._tbBot.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._tbBot.Name = "_tbBot";
            this._tbBot.TabIndex = 18;
            //
            // _lblBot
            //
            this._lblBot.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblBot.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this._lblBot.ForeColor = System.Drawing.Color.Gray;
            this._lblBot.Name = "_lblBot";
            this._lblBot.TabIndex = 19;
            this._lblBot.Text = "● 미연결";
            this._lblBot.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbAuto
            //
            this._cbAuto.AutoSize = true;
            this.linkLayout.SetColumnSpan(this._cbAuto, 3);
            this._cbAuto.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbAuto.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbAuto.Name = "_cbAuto";
            this._cbAuto.TabIndex = 20;
            this._cbAuto.Text = "Auto connect on app start";
            this._cbAuto.UseVisualStyleBackColor = true;
            //
            // buttonLayout
            //
            this.buttonLayout.ColumnCount = 4;
            this.linkLayout.SetColumnSpan(this.buttonLayout, 3);
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.buttonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.Controls.Add(this._btnConnect, 0, 0);
            this.buttonLayout.Controls.Add(this._btnDisconnect, 1, 0);
            this.buttonLayout.Controls.Add(this._btnPing, 2, 0);
            this.buttonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonLayout.Name = "buttonLayout";
            this.buttonLayout.RowCount = 1;
            this.buttonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonLayout.TabIndex = 21;
            //
            // _btnConnect
            //
            this._btnConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnConnect.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._btnConnect.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this._btnConnect.Name = "_btnConnect";
            this._btnConnect.TabIndex = 0;
            this._btnConnect.Text = "CONNECT";
            //
            // _btnDisconnect
            //
            this._btnDisconnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnDisconnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnDisconnect.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._btnDisconnect.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this._btnDisconnect.Name = "_btnDisconnect";
            this._btnDisconnect.TabIndex = 1;
            this._btnDisconnect.Text = "DISCONNECT";
            //
            // _btnPing
            //
            this._btnPing.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnPing.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnPing.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._btnPing.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this._btnPing.Name = "_btnPing";
            this._btnPing.TabIndex = 2;
            this._btnPing.Text = "PING ALL";
            //
            // lblHint
            //
            this.linkLayout.SetColumnSpan(this.lblHint, 3);
            this.lblHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblHint.Name = "lblHint";
            this.lblHint.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.lblHint.TabIndex = 22;
            this.lblHint.Text = "Run QMC.Vision.exe before connecting.  기본 포트: Wafer 5100 / Bottom Insp 5101 / Bin 5103 / Main 5104 / Top 5105 / Bottom 5106.";
            //
            // VisionLinkPage
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "VisionLinkPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.grpLink.ResumeLayout(false);
            this.linkLayout.ResumeLayout(false);
            this.linkLayout.PerformLayout();
            this.buttonLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
