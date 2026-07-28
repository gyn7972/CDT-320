namespace QMC.CDT_320.Ui.Dialogs
{
    partial class RuntimeOffsetMonitorDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.GroupBox grpPlace;
        private System.Windows.Forms.DataGridView gridPlace;
        private System.Windows.Forms.GroupBox grpPick;
        private System.Windows.Forms.DataGridView gridPick;
        private System.Windows.Forms.FlowLayoutPanel buttonBar;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnApplyPick;
        private System.Windows.Forms.Button btnApplyPlace;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Timer timerRefresh;

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
            this.components = new System.ComponentModel.Container();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpPlace = new System.Windows.Forms.GroupBox();
            this.gridPlace = new System.Windows.Forms.DataGridView();
            this.grpPick = new System.Windows.Forms.GroupBox();
            this.gridPick = new System.Windows.Forms.DataGridView();
            this.buttonBar = new System.Windows.Forms.FlowLayoutPanel();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnApplyPick = new System.Windows.Forms.Button();
            this.btnApplyPlace = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.rootLayout.SuspendLayout();
            this.grpPlace.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPlace)).BeginInit();
            this.grpPick.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPick)).BeginInit();
            this.buttonBar.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.grpPlace, 0, 0);
            this.rootLayout.Controls.Add(this.grpPick, 0, 1);
            this.rootLayout.Controls.Add(this.buttonBar, 0, 2);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.Size = new System.Drawing.Size(900, 720);
            this.rootLayout.TabIndex = 0;
            //
            // grpPlace
            //
            this.grpPlace.Controls.Add(this.gridPlace);
            this.grpPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPlace.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpPlace.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.grpPlace.Location = new System.Drawing.Point(11, 11);
            this.grpPlace.Name = "grpPlace";
            this.grpPlace.Padding = new System.Windows.Forms.Padding(6);
            this.grpPlace.Size = new System.Drawing.Size(878, 310);
            this.grpPlace.TabIndex = 0;
            this.grpPlace.TabStop = false;
            this.grpPlace.Text = "PLACE RUNTIME OFFSET (Bin 후검사 폐루프)";
            //
            // gridPlace
            //
            this.gridPlace.AllowUserToAddRows = false;
            this.gridPlace.AllowUserToDeleteRows = false;
            this.gridPlace.AllowUserToResizeRows = false;
            this.gridPlace.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridPlace.BackgroundColor = System.Drawing.Color.White;
            this.gridPlace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.gridPlace.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPlace.EnableHeadersVisualStyles = false;
            this.gridPlace.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.gridPlace.Location = new System.Drawing.Point(6, 25);
            this.gridPlace.MultiSelect = false;
            this.gridPlace.Name = "gridPlace";
            this.gridPlace.ReadOnly = true;
            this.gridPlace.RowHeadersVisible = false;
            this.gridPlace.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPlace.Size = new System.Drawing.Size(866, 279);
            this.gridPlace.TabIndex = 0;
            //
            // grpPick
            //
            this.grpPick.Controls.Add(this.gridPick);
            this.grpPick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPick.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpPick.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.grpPick.Location = new System.Drawing.Point(11, 327);
            this.grpPick.Name = "grpPick";
            this.grpPick.Padding = new System.Windows.Forms.Padding(6);
            this.grpPick.Size = new System.Drawing.Size(878, 310);
            this.grpPick.TabIndex = 1;
            this.grpPick.TabStop = false;
            this.grpPick.Text = "PICK RUNTIME OFFSET (Bottom 검사 폐루프) — 표시 전용";
            //
            // gridPick
            //
            this.gridPick.AllowUserToAddRows = false;
            this.gridPick.AllowUserToDeleteRows = false;
            this.gridPick.AllowUserToResizeRows = false;
            this.gridPick.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridPick.BackgroundColor = System.Drawing.Color.White;
            this.gridPick.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.gridPick.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPick.EnableHeadersVisualStyles = false;
            this.gridPick.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.gridPick.Location = new System.Drawing.Point(6, 25);
            this.gridPick.MultiSelect = false;
            this.gridPick.Name = "gridPick";
            this.gridPick.ReadOnly = true;
            this.gridPick.RowHeadersVisible = false;
            this.gridPick.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPick.Size = new System.Drawing.Size(866, 279);
            this.gridPick.TabIndex = 0;
            //
            // buttonBar
            //
            this.buttonBar.Controls.Add(this.btnClose);
            this.buttonBar.Controls.Add(this.btnRefresh);
            this.buttonBar.Controls.Add(this.btnApplyPick);
            this.buttonBar.Controls.Add(this.btnApplyPlace);
            this.buttonBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonBar.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonBar.Location = new System.Drawing.Point(11, 643);
            this.buttonBar.Name = "buttonBar";
            this.buttonBar.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.buttonBar.Size = new System.Drawing.Size(878, 52);
            this.buttonBar.TabIndex = 2;
            //
            // btnClose
            //
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.Location = new System.Drawing.Point(725, 11);
            this.btnClose.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(150, 36);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // btnRefresh
            //
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.Location = new System.Drawing.Point(569, 11);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(150, 36);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "REFRESH";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            //
            // btnApplyPick
            //
            this.btnApplyPick.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(190)))), ((int)(((byte)(190)))), ((int)(((byte)(190)))));
            this.btnApplyPick.Enabled = false;
            this.btnApplyPick.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyPick.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnApplyPick.ForeColor = System.Drawing.Color.White;
            this.btnApplyPick.Location = new System.Drawing.Point(413, 11);
            this.btnApplyPick.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnApplyPick.Name = "btnApplyPick";
            this.btnApplyPick.Size = new System.Drawing.Size(150, 36);
            this.btnApplyPick.TabIndex = 1;
            this.btnApplyPick.Text = "PICK → 메카 적용";
            this.btnApplyPick.UseVisualStyleBackColor = false;
            this.btnApplyPick.Click += new System.EventHandler(this.btnApplyPick_Click);
            //
            // btnApplyPlace
            //
            this.btnApplyPlace.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(88)))), ((int)(((byte)(31)))));
            this.btnApplyPlace.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyPlace.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnApplyPlace.ForeColor = System.Drawing.Color.White;
            this.btnApplyPlace.Location = new System.Drawing.Point(257, 11);
            this.btnApplyPlace.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnApplyPlace.Name = "btnApplyPlace";
            this.btnApplyPlace.Size = new System.Drawing.Size(150, 36);
            this.btnApplyPlace.TabIndex = 0;
            this.btnApplyPlace.Text = "PLACE → 메카 적용";
            this.btnApplyPlace.UseVisualStyleBackColor = false;
            this.btnApplyPlace.Click += new System.EventHandler(this.btnApplyPlace_Click);
            //
            // lblStatus
            //
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.lblStatus.Location = new System.Drawing.Point(11, 695);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(878, 30);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "PLACE 적용은 기구 오프셋 = 기구 오프셋 − 필터값(X/Y)으로 이관하고, 이관한 채널의 필터를 0으로 초기화합니다.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // timerRefresh
            //
            this.timerRefresh.Interval = 1000;
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            //
            // RuntimeOffsetMonitorDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 720);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(776, 600);
            this.Name = "RuntimeOffsetMonitorDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "RUNTIME OFFSET MONITOR";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RuntimeOffsetMonitorDialog_FormClosing);
            this.rootLayout.ResumeLayout(false);
            this.grpPlace.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPlace)).EndInit();
            this.grpPick.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPick)).EndInit();
            this.buttonBar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
