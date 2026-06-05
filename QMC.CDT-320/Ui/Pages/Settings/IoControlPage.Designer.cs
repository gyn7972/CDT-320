namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class IoControlPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblDiTitle;
        private System.Windows.Forms.Label lblDoTitle;
        private System.Windows.Forms.DataGridView diGrid;
        private System.Windows.Forms.DataGridView doGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn diNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn diAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn diName;
        private System.Windows.Forms.DataGridViewTextBoxColumn diModule;
        private System.Windows.Forms.DataGridViewTextBoxColumn diBit;
        private System.Windows.Forms.DataGridViewTextBoxColumn diState;
        private System.Windows.Forms.DataGridViewTextBoxColumn doNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn doAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn doName;
        private System.Windows.Forms.DataGridViewTextBoxColumn doModule;
        private System.Windows.Forms.DataGridViewTextBoxColumn doBit;
        private System.Windows.Forms.DataGridViewTextBoxColumn doState;
        private System.Windows.Forms.FlowLayoutPanel actionsPanel;
        private QMC.CDT_320.Ui.Controls.ActionButton btnRefresh;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDoOn;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDoOff;
        private QMC.CDT_320.Ui.Controls.ActionButton btnPulse;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblDiTitle = new System.Windows.Forms.Label();
            this.lblDoTitle = new System.Windows.Forms.Label();
            this.diGrid = new System.Windows.Forms.DataGridView();
            this.diNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diModule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diBit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.diState = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doGrid = new System.Windows.Forms.DataGridView();
            this.doNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doModule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doBit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.doState = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.actionsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefresh = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDoOn = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDoOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPulse = new QMC.CDT_320.Ui.Controls.ActionButton();
            ((System.ComponentModel.ISupportInitialize)(this.diGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.doGrid)).BeginInit();
            this.actionsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(8, 8);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 26);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "I/O CONTROL";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStatus
            // 
            this.lblStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.White;
            this.lblStatus.Location = new System.Drawing.Point(8, 36);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(1400, 26);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Live hardware I/O. DO commands are written directly to the mapped Ajin output.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDiTitle
            // 
            this.lblDiTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.lblDiTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblDiTitle.ForeColor = System.Drawing.Color.White;
            this.lblDiTitle.Location = new System.Drawing.Point(8, 68);
            this.lblDiTitle.Name = "lblDiTitle";
            this.lblDiTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblDiTitle.Size = new System.Drawing.Size(690, 24);
            this.lblDiTitle.TabIndex = 2;
            this.lblDiTitle.Text = "Digital Input";
            this.lblDiTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDoTitle
            // 
            this.lblDoTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.lblDoTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblDoTitle.ForeColor = System.Drawing.Color.White;
            this.lblDoTitle.Location = new System.Drawing.Point(718, 68);
            this.lblDoTitle.Name = "lblDoTitle";
            this.lblDoTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblDoTitle.Size = new System.Drawing.Size(690, 24);
            this.lblDoTitle.TabIndex = 3;
            this.lblDoTitle.Text = "Digital Output";
            this.lblDoTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // diGrid
            // 
            this.diGrid.AllowUserToAddRows = false;
            this.diGrid.AllowUserToDeleteRows = false;
            this.diGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.diGrid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.diGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.diGrid.ColumnHeadersHeight = 29;
            this.diGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.diNo,
            this.diAddress,
            this.diName,
            this.diModule,
            this.diBit,
            this.diState});
            this.diGrid.EnableHeadersVisualStyles = false;
            this.diGrid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.diGrid.Location = new System.Drawing.Point(8, 96);
            this.diGrid.MultiSelect = false;
            this.diGrid.Name = "diGrid";
            this.diGrid.ReadOnly = true;
            this.diGrid.RowHeadersVisible = false;
            this.diGrid.RowHeadersWidth = 51;
            this.diGrid.RowTemplate.Height = 26;
            this.diGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.diGrid.Size = new System.Drawing.Size(690, 685);
            this.diGrid.TabIndex = 4;
            // 
            // diNo
            // 
            this.diNo.FillWeight = 45F;
            this.diNo.HeaderText = "NO";
            this.diNo.MinimumWidth = 6;
            this.diNo.Name = "diNo";
            this.diNo.ReadOnly = true;
            // 
            // diAddress
            // 
            this.diAddress.FillWeight = 70F;
            this.diAddress.HeaderText = "ADDR";
            this.diAddress.MinimumWidth = 6;
            this.diAddress.Name = "diAddress";
            this.diAddress.ReadOnly = true;
            // 
            // diName
            // 
            this.diName.HeaderText = "NAME";
            this.diName.MinimumWidth = 6;
            this.diName.Name = "diName";
            this.diName.ReadOnly = true;
            // 
            // diModule
            // 
            this.diModule.HeaderText = "MODULE";
            this.diModule.MinimumWidth = 6;
            this.diModule.Name = "diModule";
            this.diModule.ReadOnly = true;
            // 
            // diBit
            // 
            this.diBit.HeaderText = "BIT";
            this.diBit.MinimumWidth = 6;
            this.diBit.Name = "diBit";
            this.diBit.ReadOnly = true;
            // 
            // diState
            // 
            this.diState.HeaderText = "STATE";
            this.diState.MinimumWidth = 6;
            this.diState.Name = "diState";
            this.diState.ReadOnly = true;
            // 
            // doGrid
            // 
            this.doGrid.AllowUserToAddRows = false;
            this.doGrid.AllowUserToDeleteRows = false;
            this.doGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.doGrid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.doGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.doGrid.ColumnHeadersHeight = 29;
            this.doGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.doNo,
            this.doAddress,
            this.doName,
            this.doModule,
            this.doBit,
            this.doState});
            this.doGrid.EnableHeadersVisualStyles = false;
            this.doGrid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.doGrid.Location = new System.Drawing.Point(718, 96);
            this.doGrid.MultiSelect = false;
            this.doGrid.Name = "doGrid";
            this.doGrid.ReadOnly = true;
            this.doGrid.RowHeadersVisible = false;
            this.doGrid.RowHeadersWidth = 51;
            this.doGrid.RowTemplate.Height = 26;
            this.doGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.doGrid.Size = new System.Drawing.Size(690, 685);
            this.doGrid.TabIndex = 5;
            // 
            // doNo
            // 
            this.doNo.FillWeight = 45F;
            this.doNo.HeaderText = "NO";
            this.doNo.MinimumWidth = 6;
            this.doNo.Name = "doNo";
            this.doNo.ReadOnly = true;
            // 
            // doAddress
            // 
            this.doAddress.FillWeight = 70F;
            this.doAddress.HeaderText = "ADDR";
            this.doAddress.MinimumWidth = 6;
            this.doAddress.Name = "doAddress";
            this.doAddress.ReadOnly = true;
            // 
            // doName
            // 
            this.doName.HeaderText = "NAME";
            this.doName.MinimumWidth = 6;
            this.doName.Name = "doName";
            this.doName.ReadOnly = true;
            // 
            // doModule
            // 
            this.doModule.HeaderText = "MODULE";
            this.doModule.MinimumWidth = 6;
            this.doModule.Name = "doModule";
            this.doModule.ReadOnly = true;
            // 
            // doBit
            // 
            this.doBit.HeaderText = "BIT";
            this.doBit.MinimumWidth = 6;
            this.doBit.Name = "doBit";
            this.doBit.ReadOnly = true;
            // 
            // doState
            // 
            this.doState.HeaderText = "STATE";
            this.doState.MinimumWidth = 6;
            this.doState.Name = "doState";
            this.doState.ReadOnly = true;
            // 
            // actionsPanel
            // 
            this.actionsPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.actionsPanel.Controls.Add(this.btnRefresh);
            this.actionsPanel.Controls.Add(this.btnDoOn);
            this.actionsPanel.Controls.Add(this.btnDoOff);
            this.actionsPanel.Controls.Add(this.btnPulse);
            this.actionsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.actionsPanel.Location = new System.Drawing.Point(0, 790);
            this.actionsPanel.Name = "actionsPanel";
            this.actionsPanel.Padding = new System.Windows.Forms.Padding(8);
            this.actionsPanel.Size = new System.Drawing.Size(1416, 60);
            this.actionsPanel.TabIndex = 6;
            // 
            // btnRefresh
            // 
            this.btnRefresh.BackColor = System.Drawing.Color.Gray;
            this.btnRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefresh.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.Location = new System.Drawing.Point(12, 12);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(4);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(130, 44);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "REFRESH";
            // 
            // btnDoOn
            // 
            this.btnDoOn.BackColor = System.Drawing.Color.Gray;
            this.btnDoOn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDoOn.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnDoOn.ForeColor = System.Drawing.Color.White;
            this.btnDoOn.Location = new System.Drawing.Point(150, 12);
            this.btnDoOn.Margin = new System.Windows.Forms.Padding(4);
            this.btnDoOn.Name = "btnDoOn";
            this.btnDoOn.Size = new System.Drawing.Size(130, 44);
            this.btnDoOn.TabIndex = 1;
            this.btnDoOn.Text = "DO ON";
            // 
            // btnDoOff
            // 
            this.btnDoOff.BackColor = System.Drawing.Color.Gray;
            this.btnDoOff.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDoOff.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnDoOff.ForeColor = System.Drawing.Color.White;
            this.btnDoOff.Location = new System.Drawing.Point(288, 12);
            this.btnDoOff.Margin = new System.Windows.Forms.Padding(4);
            this.btnDoOff.Name = "btnDoOff";
            this.btnDoOff.Size = new System.Drawing.Size(130, 44);
            this.btnDoOff.TabIndex = 2;
            this.btnDoOff.Text = "DO OFF";
            // 
            // btnPulse
            // 
            this.btnPulse.BackColor = System.Drawing.Color.Gray;
            this.btnPulse.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPulse.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnPulse.ForeColor = System.Drawing.Color.White;
            this.btnPulse.Location = new System.Drawing.Point(426, 12);
            this.btnPulse.Margin = new System.Windows.Forms.Padding(4);
            this.btnPulse.Name = "btnPulse";
            this.btnPulse.Size = new System.Drawing.Size(150, 44);
            this.btnPulse.TabIndex = 3;
            this.btnPulse.Text = "PULSE 200ms";
            // 
            // IoControlPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.actionsPanel);
            this.Controls.Add(this.doGrid);
            this.Controls.Add(this.diGrid);
            this.Controls.Add(this.lblDoTitle);
            this.Controls.Add(this.lblDiTitle);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblHeader);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.Name = "IoControlPage";
            this.Size = new System.Drawing.Size(1678, 900);
            ((System.ComponentModel.ISupportInitialize)(this.diGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.doGrid)).EndInit();
            this.actionsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
