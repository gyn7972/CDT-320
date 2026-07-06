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
            this.btnClearLog = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblExposureEnd = new System.Windows.Forms.Label();
            this.picView = new System.Windows.Forms.PictureBox();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.panelLight = new System.Windows.Forms.Panel();
            this.lblLightHdr = new System.Windows.Forms.Label();
            this.panelLightTop = new System.Windows.Forms.Panel();
            this.lblLightPort = new System.Windows.Forms.Label();
            this.txtLightPort = new System.Windows.Forms.TextBox();
            this.btnLightConnect = new System.Windows.Forms.Button();
            this.lblLightPage = new System.Windows.Forms.Label();
            this.cmbLightPage = new System.Windows.Forms.ComboBox();
            this.gridLight = new System.Windows.Forms.DataGridView();
            this.colLightCh = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLightName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLightOnTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelLightButtons = new System.Windows.Forms.Panel();
            this.btnLightApply = new System.Windows.Forms.Button();
            this.btnLightSave = new System.Windows.Forms.Button();
            this.lblLightNote = new System.Windows.Forms.Label();
            this.panelTop.SuspendLayout();
            this.panelLight.SuspendLayout();
            this.panelLightTop.SuspendLayout();
            this.panelLightButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLight)).BeginInit();
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
            this.panelTop.Controls.Add(this.btnClearLog);
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
            // btnClearLog
            //
            this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLog.Location = new System.Drawing.Point(874, 8);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(100, 28);
            this.btnClearLog.TabIndex = 8;
            this.btnClearLog.Text = "로그 지우기";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
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
            // panelLight — 엘파인 LCP24 조명 (채널별 밝기만 — 와이어링이 페이지 미사용)
            //
            this.panelLight.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelLight.Width = 300;
            this.panelLight.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelLight.Controls.Add(this.gridLight);
            this.panelLight.Controls.Add(this.panelLightButtons);
            this.panelLight.Controls.Add(this.panelLightTop);
            this.panelLight.Controls.Add(this.lblLightHdr);
            //
            // lblLightHdr
            //
            this.lblLightHdr.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLightHdr.Height = 28;
            this.lblLightHdr.Text = "  엘파인 조명 (LCP24 · 채널 밝기)";
            this.lblLightHdr.BackColor = System.Drawing.Color.FromArgb(60, 63, 70);
            this.lblLightHdr.ForeColor = System.Drawing.Color.White;
            this.lblLightHdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // panelLightTop — 포트 + 연결 + 사용 페이지(1~12)
            //
            this.panelLightTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelLightTop.Height = 74;
            this.panelLightTop.Controls.Add(this.lblLightPort);
            this.panelLightTop.Controls.Add(this.txtLightPort);
            this.panelLightTop.Controls.Add(this.btnLightConnect);
            this.panelLightTop.Controls.Add(this.lblLightPage);
            this.panelLightTop.Controls.Add(this.cmbLightPage);
            this.lblLightPort.AutoSize = true;
            this.lblLightPort.Location = new System.Drawing.Point(8, 12);
            this.lblLightPort.Text = "포트";
            this.txtLightPort.Location = new System.Drawing.Point(56, 8);
            this.txtLightPort.Size = new System.Drawing.Size(90, 23);
            this.txtLightPort.Text = "COM4";
            this.btnLightConnect.Location = new System.Drawing.Point(156, 7);
            this.btnLightConnect.Size = new System.Drawing.Size(72, 26);
            this.btnLightConnect.Text = "연결";
            this.btnLightConnect.UseVisualStyleBackColor = true;
            this.btnLightConnect.Click += new System.EventHandler(this.btnLightConnect_Click);
            this.lblLightPage.AutoSize = true;
            this.lblLightPage.Location = new System.Drawing.Point(8, 45);
            this.lblLightPage.Text = "페이지";
            this.cmbLightPage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLightPage.Location = new System.Drawing.Point(56, 41);
            this.cmbLightPage.Size = new System.Drawing.Size(90, 23);
            this.cmbLightPage.SelectedIndexChanged += new System.EventHandler(this.cmbLightPage_SelectedIndexChanged);
            //
            // gridLight — 채널 1~16 ON-TIME(µs, 0~1500 · 10µs 단위)
            //
            this.gridLight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridLight.AllowUserToAddRows = false;
            this.gridLight.AllowUserToDeleteRows = false;
            this.gridLight.RowHeadersVisible = false;
            this.gridLight.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridLight.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.colLightCh.HeaderText = "CH";
            this.colLightCh.Name = "colLightCh";
            this.colLightCh.ReadOnly = true;
            this.colLightCh.FillWeight = 25;
            this.colLightName.HeaderText = "용도";
            this.colLightName.Name = "colLightName";
            this.colLightName.ReadOnly = true;
            this.colLightName.FillWeight = 40;
            this.colLightOnTime.HeaderText = "ON-TIME (µs)";
            this.colLightOnTime.Name = "colLightOnTime";
            this.colLightOnTime.FillWeight = 35;
            this.gridLight.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colLightCh, this.colLightName, this.colLightOnTime });
            //
            // panelLightButtons — 적용/장비 저장 + 안내
            //
            this.panelLightButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelLightButtons.Height = 84;
            this.panelLightButtons.Controls.Add(this.btnLightApply);
            this.panelLightButtons.Controls.Add(this.btnLightSave);
            this.panelLightButtons.Controls.Add(this.lblLightNote);
            this.btnLightApply.Location = new System.Drawing.Point(8, 6);
            this.btnLightApply.Size = new System.Drawing.Size(138, 28);
            this.btnLightApply.Text = "조명 적용 (SP)";
            this.btnLightApply.UseVisualStyleBackColor = true;
            this.btnLightApply.Click += new System.EventHandler(this.btnLightApply_Click);
            this.btnLightSave.Location = new System.Drawing.Point(154, 6);
            this.btnLightSave.Size = new System.Drawing.Size(138, 28);
            this.btnLightSave.Text = "장비 저장 (WP)";
            this.btnLightSave.UseVisualStyleBackColor = true;
            this.btnLightSave.Click += new System.EventHandler(this.btnLightSave_Click);
            this.lblLightNote.Location = new System.Drawing.Point(8, 40);
            this.lblLightNote.Size = new System.Drawing.Size(284, 40);
            this.lblLightNote.ForeColor = System.Drawing.Color.DimGray;
            this.lblLightNote.Text = "스트로브 — 트리거(그랩) 순간에만 발광. 바텀 돔=CH6~8.\r\n0~1500µs(10µs 단위). 적용/저장은 선택 페이지 대상.";
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1300, 661);
            this.Controls.Add(this.picView);
            this.Controls.Add(this.panelLight);
            this.Controls.Add(this.lstLog);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "QMC MilCamera Test — Grab / Live / ExposureEnd";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelLightTop.ResumeLayout(false);
            this.panelLightTop.PerformLayout();
            this.panelLightButtons.ResumeLayout(false);
            this.panelLight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridLight)).EndInit();
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
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblExposureEnd;
        private System.Windows.Forms.PictureBox picView;
        private System.Windows.Forms.ListBox lstLog;
        private System.Windows.Forms.Panel panelLight;
        private System.Windows.Forms.Label lblLightHdr;
        private System.Windows.Forms.Panel panelLightTop;
        private System.Windows.Forms.Label lblLightPort;
        private System.Windows.Forms.TextBox txtLightPort;
        private System.Windows.Forms.Button btnLightConnect;
        private System.Windows.Forms.Label lblLightPage;
        private System.Windows.Forms.ComboBox cmbLightPage;
        private System.Windows.Forms.DataGridView gridLight;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLightCh;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLightName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLightOnTime;
        private System.Windows.Forms.Panel panelLightButtons;
        private System.Windows.Forms.Button btnLightApply;
        private System.Windows.Forms.Button btnLightSave;
        private System.Windows.Forms.Label lblLightNote;
    }
}
