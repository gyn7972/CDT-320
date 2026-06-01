namespace SP_RemoteViewer
{
    partial class Form1
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabServer = new System.Windows.Forms.TabPage();
            this.lblServerStatus = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.numServerFPS = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.numServerQuality = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.txtServerLog = new System.Windows.Forms.TextBox();
            this.btnServerStop = new System.Windows.Forms.Button();
            this.btnServerStart = new System.Windows.Forms.Button();
            this.numServerPort = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbScreenSelect = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tabClient = new System.Windows.Forms.TabPage();
            this.remoteViewer = new QMC.RemoteViewer.RemoteViewerControl();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblClientStatus = new System.Windows.Forms.Label();
            this.chkStretchImage = new System.Windows.Forms.CheckBox();
            this.btnClientDisconnect = new System.Windows.Forms.Button();
            this.btnClientConnect = new System.Windows.Forms.Button();
            this.numClientPort = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.txtServerAddress = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabServer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numServerFPS)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numServerQuality)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numServerPort)).BeginInit();
            this.tabClient.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numClientPort)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabServer);
            this.tabControl1.Controls.Add(this.tabClient);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1445, 521);
            this.tabControl1.TabIndex = 0;
            // 
            // tabServer
            // 
            this.tabServer.Controls.Add(this.lblServerStatus);
            this.tabServer.Controls.Add(this.label6);
            this.tabServer.Controls.Add(this.numServerFPS);
            this.tabServer.Controls.Add(this.label5);
            this.tabServer.Controls.Add(this.numServerQuality);
            this.tabServer.Controls.Add(this.label4);
            this.tabServer.Controls.Add(this.txtServerLog);
            this.tabServer.Controls.Add(this.btnServerStop);
            this.tabServer.Controls.Add(this.btnServerStart);
            this.tabServer.Controls.Add(this.numServerPort);
            this.tabServer.Controls.Add(this.label1);
            this.tabServer.Controls.Add(this.cmbScreenSelect);
            this.tabServer.Controls.Add(this.label7);
            this.tabServer.Location = new System.Drawing.Point(4, 22);
            this.tabServer.Name = "tabServer";
            this.tabServer.Padding = new System.Windows.Forms.Padding(3);
            this.tabServer.Size = new System.Drawing.Size(1437, 495);
            this.tabServer.TabIndex = 0;
            this.tabServer.Text = "서버 (화면 전송)";
            this.tabServer.UseVisualStyleBackColor = true;
            // 
            // lblServerStatus
            // 
            this.lblServerStatus.AutoSize = true;
            this.lblServerStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblServerStatus.Location = new System.Drawing.Point(18, 105);
            this.lblServerStatus.Name = "lblServerStatus";
            this.lblServerStatus.Size = new System.Drawing.Size(74, 15);
            this.lblServerStatus.TabIndex = 10;
            this.lblServerStatus.Text = "상태: 중지됨";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(277, 58);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 12);
            this.label6.TabIndex = 9;
            this.label6.Text = "프레임";
            // 
            // numServerFPS
            // 
            this.numServerFPS.Location = new System.Drawing.Point(231, 54);
            this.numServerFPS.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numServerFPS.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numServerFPS.Name = "numServerFPS";
            this.numServerFPS.Size = new System.Drawing.Size(40, 21);
            this.numServerFPS.TabIndex = 8;
            this.numServerFPS.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(186, 58);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(40, 12);
            this.label5.TabIndex = 7;
            this.label5.Text = "FPS : ";
            // 
            // numServerQuality
            // 
            this.numServerQuality.Location = new System.Drawing.Point(94, 54);
            this.numServerQuality.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numServerQuality.Name = "numServerQuality";
            this.numServerQuality.Size = new System.Drawing.Size(60, 21);
            this.numServerQuality.TabIndex = 6;
            this.numServerQuality.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 58);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(69, 12);
            this.label4.TabIndex = 5;
            this.label4.Text = "압축 품질 : ";
            // 
            // txtServerLog
            // 
            this.txtServerLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtServerLog.BackColor = System.Drawing.Color.Black;
            this.txtServerLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtServerLog.ForeColor = System.Drawing.Color.Lime;
            this.txtServerLog.Location = new System.Drawing.Point(18, 134);
            this.txtServerLog.Multiline = true;
            this.txtServerLog.Name = "txtServerLog";
            this.txtServerLog.ReadOnly = true;
            this.txtServerLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtServerLog.Size = new System.Drawing.Size(1401, 344);
            this.txtServerLog.TabIndex = 4;
            // 
            // btnServerStop
            // 
            this.btnServerStop.Enabled = false;
            this.btnServerStop.Location = new System.Drawing.Point(611, 54);
            this.btnServerStop.Name = "btnServerStop";
            this.btnServerStop.Size = new System.Drawing.Size(100, 30);
            this.btnServerStop.TabIndex = 3;
            this.btnServerStop.Text = "중지";
            this.btnServerStop.UseVisualStyleBackColor = true;
            this.btnServerStop.Click += new System.EventHandler(this.btnServerStop_Click);
            // 
            // btnServerStart
            // 
            this.btnServerStart.Location = new System.Drawing.Point(488, 52);
            this.btnServerStart.Name = "btnServerStart";
            this.btnServerStart.Size = new System.Drawing.Size(100, 30);
            this.btnServerStart.TabIndex = 2;
            this.btnServerStart.Text = "시작";
            this.btnServerStart.UseVisualStyleBackColor = true;
            this.btnServerStart.Click += new System.EventHandler(this.btnServerStart_Click);
            // 
            // numServerPort
            // 
            this.numServerPort.Location = new System.Drawing.Point(94, 20);
            this.numServerPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numServerPort.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.numServerPort.Name = "numServerPort";
            this.numServerPort.Size = new System.Drawing.Size(80, 21);
            this.numServerPort.TabIndex = 1;
            this.numServerPort.Value = new decimal(new int[] {
            8888,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "포트 번호 : ";
            // 
            // cmbScreenSelect
            // 
            this.cmbScreenSelect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbScreenSelect.FormattingEnabled = true;
            this.cmbScreenSelect.Location = new System.Drawing.Point(251, 20);
            this.cmbScreenSelect.Name = "cmbScreenSelect";
            this.cmbScreenSelect.Size = new System.Drawing.Size(250, 20);
            this.cmbScreenSelect.TabIndex = 12;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(186, 24);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(53, 12);
            this.label7.TabIndex = 11;
            this.label7.Text = "모니터 : ";
            // 
            // tabClient
            // 
            this.tabClient.Controls.Add(this.remoteViewer);
            this.tabClient.Controls.Add(this.panel1);
            this.tabClient.Location = new System.Drawing.Point(4, 22);
            this.tabClient.Name = "tabClient";
            this.tabClient.Padding = new System.Windows.Forms.Padding(3);
            this.tabClient.Size = new System.Drawing.Size(1437, 495);
            this.tabClient.TabIndex = 1;
            this.tabClient.Text = "클라이언트 (화면 보기)";
            this.tabClient.UseVisualStyleBackColor = true;
            // 
            // remoteViewer
            // 
            this.remoteViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.remoteViewer.AutoSizeToImage = false;
            this.remoteViewer.BackColor = System.Drawing.Color.Black;
            this.remoteViewer.Location = new System.Drawing.Point(20, 89);
            this.remoteViewer.Name = "remoteViewer";
            this.remoteViewer.Size = new System.Drawing.Size(1414, 406);
            this.remoteViewer.StretchImage = true;
            this.remoteViewer.TabIndex = 1;
            this.remoteViewer.Text = "remoteViewer";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblClientStatus);
            this.panel1.Controls.Add(this.chkStretchImage);
            this.panel1.Controls.Add(this.btnClientDisconnect);
            this.panel1.Controls.Add(this.btnClientConnect);
            this.panel1.Controls.Add(this.numClientPort);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.txtServerAddress);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1431, 80);
            this.panel1.TabIndex = 0;
            // 
            // lblClientStatus
            // 
            this.lblClientStatus.AutoSize = true;
            this.lblClientStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblClientStatus.Location = new System.Drawing.Point(15, 53);
            this.lblClientStatus.Name = "lblClientStatus";
            this.lblClientStatus.Size = new System.Drawing.Size(90, 15);
            this.lblClientStatus.TabIndex = 7;
            this.lblClientStatus.Text = "상태: 연결 안됨";
            // 
            // chkStretchImage
            // 
            this.chkStretchImage.AutoSize = true;
            this.chkStretchImage.Checked = true;
            this.chkStretchImage.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkStretchImage.Location = new System.Drawing.Point(663, 26);
            this.chkStretchImage.Name = "chkStretchImage";
            this.chkStretchImage.Size = new System.Drawing.Size(88, 16);
            this.chkStretchImage.TabIndex = 6;
            this.chkStretchImage.Text = "화면에 맞춤";
            this.chkStretchImage.UseVisualStyleBackColor = true;
            this.chkStretchImage.CheckedChanged += new System.EventHandler(this.chkStretchImage_CheckedChanged);
            // 
            // btnClientDisconnect
            // 
            this.btnClientDisconnect.Enabled = false;
            this.btnClientDisconnect.Location = new System.Drawing.Point(497, 18);
            this.btnClientDisconnect.Name = "btnClientDisconnect";
            this.btnClientDisconnect.Size = new System.Drawing.Size(100, 30);
            this.btnClientDisconnect.TabIndex = 5;
            this.btnClientDisconnect.Text = "연결 해제";
            this.btnClientDisconnect.UseVisualStyleBackColor = true;
            this.btnClientDisconnect.Click += new System.EventHandler(this.btnClientDisconnect_Click);
            // 
            // btnClientConnect
            // 
            this.btnClientConnect.Location = new System.Drawing.Point(230, 12);
            this.btnClientConnect.Name = "btnClientConnect";
            this.btnClientConnect.Size = new System.Drawing.Size(100, 30);
            this.btnClientConnect.TabIndex = 4;
            this.btnClientConnect.Text = "연결";
            this.btnClientConnect.UseVisualStyleBackColor = true;
            this.btnClientConnect.Click += new System.EventHandler(this.btnClientConnect_Click);
            // 
            // numClientPort
            // 
            this.numClientPort.Location = new System.Drawing.Point(321, 15);
            this.numClientPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numClientPort.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.numClientPort.Name = "numClientPort";
            this.numClientPort.Size = new System.Drawing.Size(80, 21);
            this.numClientPort.TabIndex = 3;
            this.numClientPort.Value = new decimal(new int[] {
            8888,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(276, 19);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "포트 : ";
            // 
            // txtServerAddress
            // 
            this.txtServerAddress.Location = new System.Drawing.Point(99, 15);
            this.txtServerAddress.Name = "txtServerAddress";
            this.txtServerAddress.Size = new System.Drawing.Size(150, 21);
            this.txtServerAddress.TabIndex = 1;
            this.txtServerAddress.Text = "127.0.0.1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 19);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 12);
            this.label2.TabIndex = 0;
            this.label2.Text = "서버 주소 :  ";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1445, 521);
            this.Controls.Add(this.tabControl1);
            this.Name = "Form1";
            this.Text = "SP Remote Viewer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.tabControl1.ResumeLayout(false);
            this.tabServer.ResumeLayout(false);
            this.tabServer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numServerFPS)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numServerQuality)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numServerPort)).EndInit();
            this.tabClient.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numClientPort)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabServer;
        private System.Windows.Forms.TabPage tabClient;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numServerPort;
        private System.Windows.Forms.Button btnServerStart;
        private System.Windows.Forms.Button btnServerStop;
        private System.Windows.Forms.TextBox txtServerLog;
        private System.Windows.Forms.NumericUpDown numServerQuality;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numServerFPS;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblServerStatus;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbScreenSelect;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtServerAddress;
        private System.Windows.Forms.NumericUpDown numClientPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnClientConnect;
        private System.Windows.Forms.Button btnClientDisconnect;
        private System.Windows.Forms.CheckBox chkStretchImage;
        private System.Windows.Forms.Label lblClientStatus;
        private QMC.RemoteViewer.RemoteViewerControl remoteViewer;
    }
}

