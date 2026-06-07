namespace QMC.CDT_320.Ui.Dialogs
{
    partial class InitializationMonitorDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.FlowLayoutPanel pnlButtons;
        private System.Windows.Forms.Button btnRunSelected;
        private System.Windows.Forms.Button btnRunAll;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStepNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colGroupName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPhase;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTargetType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTarget;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCommand;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDescription;

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
            System.Windows.Forms.DataGridViewCellStyle headerStyle = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle centerStyle = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle statusStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.colStepNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGroupName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPhase = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTargetType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCommand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRunSelected = new System.Windows.Forms.Button();
            this.btnRunAll = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.layoutRoot.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // layoutRoot
            // 
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Controls.Add(this.lblTitle, 0, 0);
            this.layoutRoot.Controls.Add(this.grid, 0, 1);
            this.layoutRoot.Controls.Add(this.pnlButtons, 0, 2);
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Location = new System.Drawing.Point(12, 12);
            this.layoutRoot.Name = "layoutRoot";
            this.layoutRoot.RowCount = 3;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.layoutRoot.Size = new System.Drawing.Size(1160, 637);
            this.layoutRoot.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Malgun Gothic", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(1154, 34);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Initialization Sequence Monitor";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToOrderColumns = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(238, 242, 246);
            headerStyle.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            headerStyle.ForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
            headerStyle.SelectionBackColor = System.Drawing.Color.FromArgb(238, 242, 246);
            headerStyle.SelectionForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
            headerStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.grid.ColumnHeadersDefaultCellStyle = headerStyle;
            this.grid.ColumnHeadersHeight = 34;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colStepNo,
            this.colGroupName,
            this.colPhase,
            this.colTargetType,
            this.colTarget,
            this.colCommand,
            this.colStatus,
            this.colDescription});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Location = new System.Drawing.Point(3, 37);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.RowTemplate.Height = 30;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1154, 545);
            this.grid.TabIndex = 1;
            // 
            // colStepNo
            // 
            centerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.colStepNo.DefaultCellStyle = centerStyle;
            this.colStepNo.FillWeight = 45F;
            this.colStepNo.HeaderText = "Step";
            this.colStepNo.Name = "colStepNo";
            this.colStepNo.ReadOnly = true;
            this.colStepNo.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colGroupName
            // 
            this.colGroupName.FillWeight = 90F;
            this.colGroupName.HeaderText = "Group";
            this.colGroupName.Name = "colGroupName";
            this.colGroupName.ReadOnly = true;
            this.colGroupName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colPhase
            // 
            this.colPhase.FillWeight = 70F;
            this.colPhase.HeaderText = "Phase";
            this.colPhase.Name = "colPhase";
            this.colPhase.ReadOnly = true;
            this.colPhase.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colTargetType
            // 
            this.colTargetType.FillWeight = 70F;
            this.colTargetType.HeaderText = "Type";
            this.colTargetType.Name = "colTargetType";
            this.colTargetType.ReadOnly = true;
            this.colTargetType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colTarget
            // 
            this.colTarget.FillWeight = 170F;
            this.colTarget.HeaderText = "Target";
            this.colTarget.Name = "colTarget";
            this.colTarget.ReadOnly = true;
            this.colTarget.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colCommand
            // 
            this.colCommand.FillWeight = 80F;
            this.colCommand.HeaderText = "Command";
            this.colCommand.Name = "colCommand";
            this.colCommand.ReadOnly = true;
            this.colCommand.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colStatus
            // 
            statusStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            statusStyle.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.colStatus.DefaultCellStyle = statusStyle;
            this.colStatus.FillWeight = 70F;
            this.colStatus.HeaderText = "Status";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colDescription
            // 
            this.colDescription.FillWeight = 240F;
            this.colDescription.HeaderText = "Description";
            this.colDescription.Name = "colDescription";
            this.colDescription.ReadOnly = true;
            this.colDescription.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // pnlButtons
            // 
            this.pnlButtons.Controls.Add(this.btnRunSelected);
            this.pnlButtons.Controls.Add(this.btnRunAll);
            this.pnlButtons.Controls.Add(this.btnRefresh);
            this.pnlButtons.Controls.Add(this.btnClose);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlButtons.Location = new System.Drawing.Point(3, 588);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.pnlButtons.Size = new System.Drawing.Size(1154, 46);
            this.pnlButtons.TabIndex = 2;
            // 
            // btnRunSelected
            // 
            this.btnRunSelected.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.btnRunSelected.Location = new System.Drawing.Point(1021, 11);
            this.btnRunSelected.Name = "btnRunSelected";
            this.btnRunSelected.Size = new System.Drawing.Size(130, 32);
            this.btnRunSelected.TabIndex = 0;
            this.btnRunSelected.Text = "Run Step";
            this.btnRunSelected.UseVisualStyleBackColor = true;
            this.btnRunSelected.Click += new System.EventHandler(this.btnRunSelected_Click);
            // 
            // btnRunAll
            // 
            this.btnRunAll.Font = new System.Drawing.Font("Malgun Gothic", 10F, System.Drawing.FontStyle.Bold);
            this.btnRunAll.Location = new System.Drawing.Point(895, 11);
            this.btnRunAll.Name = "btnRunAll";
            this.btnRunAll.Size = new System.Drawing.Size(120, 32);
            this.btnRunAll.TabIndex = 1;
            this.btnRunAll.Text = "Run All";
            this.btnRunAll.UseVisualStyleBackColor = true;
            this.btnRunAll.Click += new System.EventHandler(this.btnRunAll_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Font = new System.Drawing.Font("Malgun Gothic", 10F);
            this.btnRefresh.Location = new System.Drawing.Point(789, 11);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, 32);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("Malgun Gothic", 10F);
            this.btnClose.Location = new System.Drawing.Point(683, 11);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 32);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // InitializationMonitorDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 661);
            this.Controls.Add(this.layoutRoot);
            this.Font = new System.Drawing.Font("Malgun Gothic", 9F);
            this.MinimumSize = new System.Drawing.Size(980, 560);
            this.Name = "InitializationMonitorDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Initialization Sequence Monitor";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.InitializationMonitorDialog_FormClosed);
            this.Load += new System.EventHandler(this.InitializationMonitorDialog_Load);
            this.layoutRoot.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
