namespace QMC.Vision.Ui.Pages
{
    partial class CommLinkPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label _hdr;

        // TCP 포트/상태 — DataGridView (채널 / 명령포트(편집) / 뷰어포트(편집) / 명령상태 / 뷰어상태 / RX)
        private System.Windows.Forms.DataGridView _gridPorts;
        private System.Windows.Forms.DataGridViewTextBoxColumn colChannel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCmdPort;
        private System.Windows.Forms.DataGridViewTextBoxColumn colViewPort;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCmdStat;
        private System.Windows.Forms.DataGridViewTextBoxColumn colViewStat;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRx;

        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.TextBox _txtLog;

        private System.Windows.Forms.TableLayoutPanel _toolbar;
        private System.Windows.Forms.Button _btnClearLog;
        private System.Windows.Forms.Button _btnLoad;
        private System.Windows.Forms.Button _btnSave;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this._hdr = new System.Windows.Forms.Label();
            this._gridPorts = new System.Windows.Forms.DataGridView();
            this.colChannel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCmdPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colViewPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCmdStat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colViewStat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRx = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this._txtLog = new System.Windows.Forms.TextBox();
            this._toolbar = new System.Windows.Forms.TableLayoutPanel();
            this._btnClearLog = new System.Windows.Forms.Button();
            this._btnLoad = new System.Windows.Forms.Button();
            this._btnSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this._gridPorts)).BeginInit();
            this.rootLayout.SuspendLayout();
            this.grpLog.SuspendLayout();
            this._toolbar.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this._hdr, 0, 0);
            this.rootLayout.Controls.Add(this._gridPorts, 0, 1);
            this.rootLayout.Controls.Add(this.grpLog, 0, 2);
            this.rootLayout.Controls.Add(this._toolbar, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 290F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.rootLayout.Size = new System.Drawing.Size(1100, 760);
            this.rootLayout.TabIndex = 0;
            //
            // _hdr
            //
            this._hdr.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this._hdr.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hdr.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._hdr.ForeColor = System.Drawing.Color.White;
            this._hdr.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this._hdr.Name = "_hdr";
            this._hdr.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._hdr.TabIndex = 0;
            this._hdr.Text = "통신 (핸들러 ↔ Vision TCP)";
            this._hdr.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _gridPorts
            //
            this._gridPorts.AllowUserToAddRows = false;
            this._gridPorts.AllowUserToDeleteRows = false;
            this._gridPorts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._gridPorts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colChannel,
            this.colCmdPort,
            this.colViewPort,
            this.colCmdStat,
            this.colViewStat,
            this.colRx});
            this._gridPorts.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridPorts.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this._gridPorts.Name = "_gridPorts";
            this._gridPorts.TabIndex = 1;
            //
            // colChannel
            //
            this.colChannel.FillWeight = 22F;
            this.colChannel.HeaderText = "채널";
            this.colChannel.Name = "colChannel";
            this.colChannel.ReadOnly = true;
            this.colChannel.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colCmdPort
            //
            this.colCmdPort.FillWeight = 14F;
            this.colCmdPort.HeaderText = "명령 포트";
            this.colCmdPort.Name = "colCmdPort";
            this.colCmdPort.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colViewPort
            //
            this.colViewPort.FillWeight = 16F;
            this.colViewPort.HeaderText = "뷰어 포트(5200)";
            this.colViewPort.Name = "colViewPort";
            this.colViewPort.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colCmdStat
            //
            this.colCmdStat.FillWeight = 18F;
            this.colCmdStat.HeaderText = "명령 상태";
            this.colCmdStat.Name = "colCmdStat";
            this.colCmdStat.ReadOnly = true;
            this.colCmdStat.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colViewStat
            //
            this.colViewStat.FillWeight = 18F;
            this.colViewStat.HeaderText = "뷰어 상태";
            this.colViewStat.Name = "colViewStat";
            this.colViewStat.ReadOnly = true;
            this.colViewStat.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colRx
            //
            this.colRx.FillWeight = 12F;
            this.colRx.HeaderText = "RX";
            this.colRx.Name = "colRx";
            this.colRx.ReadOnly = true;
            this.colRx.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // grpLog
            //
            this.grpLog.Controls.Add(this._txtLog);
            this.grpLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLog.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpLog.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.grpLog.Name = "grpLog";
            this.grpLog.Padding = new System.Windows.Forms.Padding(8);
            this.grpLog.TabIndex = 2;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "통신 로그 (TX / RX / EPD / ARM)";
            //
            // _txtLog
            //
            this._txtLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this._txtLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtLog.Font = new System.Drawing.Font("Consolas", 9.5F);
            this._txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            this._txtLog.Multiline = true;
            this._txtLog.Name = "_txtLog";
            this._txtLog.ReadOnly = true;
            this._txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._txtLog.WordWrap = false;
            this._txtLog.TabIndex = 0;
            //
            // _toolbar
            //
            this._toolbar.ColumnCount = 4;
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this._toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this._toolbar.Controls.Add(this._btnClearLog, 0, 0);
            this._toolbar.Controls.Add(this._btnLoad, 2, 0);
            this._toolbar.Controls.Add(this._btnSave, 3, 0);
            this._toolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            this._toolbar.Name = "_toolbar";
            this._toolbar.RowCount = 1;
            this._toolbar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._toolbar.TabIndex = 3;
            //
            // _btnClearLog
            //
            this._btnClearLog.BackColor = System.Drawing.Color.White;
            this._btnClearLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnClearLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClearLog.Font = new System.Drawing.Font("맑은 고딕", 10.5F);
            this._btnClearLog.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._btnClearLog.Name = "_btnClearLog";
            this._btnClearLog.TabIndex = 0;
            this._btnClearLog.Text = "로그 지움";
            this._btnClearLog.UseVisualStyleBackColor = false;
            //
            // _btnLoad
            //
            this._btnLoad.BackColor = System.Drawing.Color.White;
            this._btnLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnLoad.Font = new System.Drawing.Font("맑은 고딕", 10.5F);
            this._btnLoad.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._btnLoad.Name = "_btnLoad";
            this._btnLoad.TabIndex = 1;
            this._btnLoad.Text = "불러오기";
            this._btnLoad.UseVisualStyleBackColor = false;
            //
            // _btnSave
            //
            this._btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this._btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSave.Font = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this._btnSave.ForeColor = System.Drawing.Color.White;
            this._btnSave.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            this._btnSave.Name = "_btnSave";
            this._btnSave.TabIndex = 2;
            this._btnSave.Text = "저장";
            this._btnSave.UseVisualStyleBackColor = false;
            //
            // CommLinkPage
            //
            this.Controls.Add(this.rootLayout);
            this.Name = "CommLinkPage";
            this.Size = new System.Drawing.Size(1100, 760);
            ((System.ComponentModel.ISupportInitialize)(this._gridPorts)).EndInit();
            this.rootLayout.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this._toolbar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
