namespace QMC.CDT_320.Ui.Dialogs
{
    partial class SharedRailXSetupDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.CheckBox chkSameVelocity;
        private System.Windows.Forms.Label lblHeadAxis;
        private System.Windows.Forms.ComboBox cboHeadAxis;
        private System.Windows.Forms.Label lblSubAxes;
        private System.Windows.Forms.CheckBox chkSubInputVision;
        private System.Windows.Forms.CheckBox chkSubOutputVision;
        private System.Windows.Forms.CheckBox chkSubFrontPicker;
        private System.Windows.Forms.CheckBox chkSubRearPicker;
        private System.Windows.Forms.TableLayoutPanel _gridGroups;
        private System.Windows.Forms.Label lblStatusParameter;
        private System.Windows.Forms.Label lblTestParameter;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.DataGridView _testGrid;
        private System.Windows.Forms.TableLayoutPanel pnlButtons;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnValidate;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.Button btnMoveSelected;
        private System.Windows.Forms.Button btnMoveHeadSub;
        private System.Windows.Forms.Button btnHomeSelected;
        private System.Windows.Forms.Button btnHomeAll;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAxis;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTarget;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVelocity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMessage;

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
            System.Windows.Forms.DataGridViewCellStyle rightStyle = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle centerStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.chkSameVelocity = new System.Windows.Forms.CheckBox();
            this.lblHeadAxis = new System.Windows.Forms.Label();
            this.cboHeadAxis = new System.Windows.Forms.ComboBox();
            this.lblSubAxes = new System.Windows.Forms.Label();
            this.chkSubInputVision = new System.Windows.Forms.CheckBox();
            this.chkSubOutputVision = new System.Windows.Forms.CheckBox();
            this.chkSubFrontPicker = new System.Windows.Forms.CheckBox();
            this.chkSubRearPicker = new System.Windows.Forms.CheckBox();
            this._gridGroups = new System.Windows.Forms.TableLayoutPanel();
            this.lblStatusParameter = new System.Windows.Forms.Label();
            this.lblTestParameter = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this._testGrid = new System.Windows.Forms.DataGridView();
            this.colAxis = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colVelocity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlButtons = new System.Windows.Forms.TableLayoutPanel();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnValidate = new System.Windows.Forms.Button();
            this.btnHelp = new System.Windows.Forms.Button();
            this.btnMoveSelected = new System.Windows.Forms.Button();
            this.btnMoveHeadSub = new System.Windows.Forms.Button();
            this.btnHomeSelected = new System.Windows.Forms.Button();
            this.btnHomeAll = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblPath = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.layoutRoot.SuspendLayout();
            this.pnlTop.SuspendLayout();
            this._gridGroups.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._testGrid)).BeginInit();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // layoutRoot
            // 
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Controls.Add(this.lblTitle, 0, 0);
            this.layoutRoot.Controls.Add(this.pnlTop, 0, 1);
            this.layoutRoot.Controls.Add(this._gridGroups, 0, 2);
            this.layoutRoot.Controls.Add(this.pnlButtons, 0, 3);
            this.layoutRoot.Controls.Add(this.lblPath, 0, 4);
            this.layoutRoot.Controls.Add(this.lblStatus, 0, 5);
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Location = new System.Drawing.Point(12, 12);
            this.layoutRoot.Name = "layoutRoot";
            this.layoutRoot.RowCount = 6;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.layoutRoot.Size = new System.Drawing.Size(1160, 617);
            this.layoutRoot.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(1154, 34);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "SharedRailX Setup / Move / X Home Test";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.chkSameVelocity);
            this.pnlTop.Controls.Add(this.lblHeadAxis);
            this.pnlTop.Controls.Add(this.cboHeadAxis);
            this.pnlTop.Controls.Add(this.lblSubAxes);
            this.pnlTop.Controls.Add(this.chkSubInputVision);
            this.pnlTop.Controls.Add(this.chkSubOutputVision);
            this.pnlTop.Controls.Add(this.chkSubFrontPicker);
            this.pnlTop.Controls.Add(this.chkSubRearPicker);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTop.Location = new System.Drawing.Point(3, 37);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1154, 70);
            this.pnlTop.TabIndex = 1;
            // chkSameVelocity
            // 
            this.chkSameVelocity.AutoSize = true;
            this.chkSameVelocity.Checked = true;
            this.chkSameVelocity.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSameVelocity.Location = new System.Drawing.Point(0, 9);
            this.chkSameVelocity.Name = "chkSameVelocity";
            this.chkSameVelocity.Size = new System.Drawing.Size(182, 19);
            this.chkSameVelocity.TabIndex = 3;
            this.chkSameVelocity.Text = "Require Same Group Velocity";
            this.chkSameVelocity.UseVisualStyleBackColor = true;
            // 
            // lblHeadAxis
            // 
            this.lblHeadAxis.Location = new System.Drawing.Point(0, 42);
            this.lblHeadAxis.Name = "lblHeadAxis";
            this.lblHeadAxis.Size = new System.Drawing.Size(42, 22);
            this.lblHeadAxis.TabIndex = 4;
            this.lblHeadAxis.Text = "Head";
            this.lblHeadAxis.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cboHeadAxis
            // 
            this.cboHeadAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboHeadAxis.FormattingEnabled = true;
            this.cboHeadAxis.Items.AddRange(new object[] {
            "InputVisionX",
            "OutputVisionX",
            "FrontPickerX",
            "RearPickerX"});
            this.cboHeadAxis.Location = new System.Drawing.Point(56, 41);
            this.cboHeadAxis.Name = "cboHeadAxis";
            this.cboHeadAxis.Size = new System.Drawing.Size(126, 23);
            this.cboHeadAxis.TabIndex = 5;
            this.cboHeadAxis.SelectedIndexChanged += new System.EventHandler(this.cboHeadAxis_SelectedIndexChanged);
            // 
            // lblSubAxes
            // 
            this.lblSubAxes.Location = new System.Drawing.Point(202, 42);
            this.lblSubAxes.Name = "lblSubAxes";
            this.lblSubAxes.Size = new System.Drawing.Size(32, 22);
            this.lblSubAxes.TabIndex = 6;
            this.lblSubAxes.Text = "Sub";
            this.lblSubAxes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkSubInputVision
            // 
            this.chkSubInputVision.AutoSize = true;
            this.chkSubInputVision.Location = new System.Drawing.Point(244, 43);
            this.chkSubInputVision.Name = "chkSubInputVision";
            this.chkSubInputVision.Size = new System.Drawing.Size(51, 19);
            this.chkSubInputVision.TabIndex = 7;
            this.chkSubInputVision.Text = "InputVisionX";
            this.chkSubInputVision.UseVisualStyleBackColor = true;
            // 
            // chkSubOutputVision
            // 
            this.chkSubOutputVision.AutoSize = true;
            this.chkSubOutputVision.Location = new System.Drawing.Point(352, 43);
            this.chkSubOutputVision.Name = "chkSubOutputVision";
            this.chkSubOutputVision.Size = new System.Drawing.Size(62, 19);
            this.chkSubOutputVision.TabIndex = 8;
            this.chkSubOutputVision.Text = "OutputVisionX";
            this.chkSubOutputVision.UseVisualStyleBackColor = true;
            // 
            // chkSubFrontPicker
            // 
            this.chkSubFrontPicker.AutoSize = true;
            this.chkSubFrontPicker.Location = new System.Drawing.Point(468, 43);
            this.chkSubFrontPicker.Name = "chkSubFrontPicker";
            this.chkSubFrontPicker.Size = new System.Drawing.Size(55, 19);
            this.chkSubFrontPicker.TabIndex = 9;
            this.chkSubFrontPicker.Text = "FrontPickerX";
            this.chkSubFrontPicker.UseVisualStyleBackColor = true;
            // 
            // chkSubRearPicker
            // 
            this.chkSubRearPicker.AutoSize = true;
            this.chkSubRearPicker.Location = new System.Drawing.Point(576, 43);
            this.chkSubRearPicker.Name = "chkSubRearPicker";
            this.chkSubRearPicker.Size = new System.Drawing.Size(50, 19);
            this.chkSubRearPicker.TabIndex = 10;
            this.chkSubRearPicker.Text = "RearPickerX";
            this.chkSubRearPicker.UseVisualStyleBackColor = true;
            //
            // _gridGroups
            //
            this._gridGroups.ColumnCount = 3;
            this._gridGroups.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this._gridGroups.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24F));
            this._gridGroups.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this._gridGroups.Controls.Add(this.lblStatusParameter, 0, 0);
            this._gridGroups.Controls.Add(this.lblTestParameter, 1, 0);
            this._gridGroups.Controls.Add(this.grid, 0, 1);
            this._gridGroups.Controls.Add(this._testGrid, 1, 1);
            this._gridGroups.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridGroups.Location = new System.Drawing.Point(3, 113);
            this._gridGroups.Name = "_gridGroups";
            this._gridGroups.RowCount = 2;
            this._gridGroups.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this._gridGroups.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._gridGroups.Size = new System.Drawing.Size(1154, 361);
            this._gridGroups.TabIndex = 2;
            //
            // lblStatusParameter
            //
            this.lblStatusParameter.BackColor = System.Drawing.Color.FromArgb(238, 242, 246);
            this.lblStatusParameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatusParameter.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblStatusParameter.Location = new System.Drawing.Point(3, 0);
            this.lblStatusParameter.Name = "lblStatusParameter";
            this.lblStatusParameter.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblStatusParameter.Size = new System.Drawing.Size(432, 24);
            this.lblStatusParameter.TabIndex = 0;
            this.lblStatusParameter.Text = "STATUS PARAMETER";
            this.lblStatusParameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblTestParameter
            //
            this.lblTestParameter.BackColor = System.Drawing.Color.FromArgb(238, 242, 246);
            this.lblTestParameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTestParameter.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblTestParameter.Location = new System.Drawing.Point(441, 0);
            this.lblTestParameter.Name = "lblTestParameter";
            this.lblTestParameter.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblTestParameter.Size = new System.Drawing.Size(270, 24);
            this.lblTestParameter.TabIndex = 1;
            this.lblTestParameter.Text = "TEST PARAMETER";
            this.lblTestParameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // grid
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToOrderColumns = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(238, 242, 246);
            headerStyle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            headerStyle.ForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
            headerStyle.SelectionBackColor = System.Drawing.Color.FromArgb(238, 242, 246);
            headerStyle.SelectionForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
            headerStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.grid.ColumnHeadersDefaultCellStyle = headerStyle;
            this.grid.ColumnHeadersHeight = 34;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colAxis,
            this.colCurrent,
            this.colTarget,
            this.colVelocity,
            this.colStatus,
            this.colMessage});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Location = new System.Drawing.Point(3, 27);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.RowHeadersVisible = false;
            this.grid.RowTemplate.Height = 30;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(432, 331);
            this.grid.TabIndex = 2;
            //
            // _testGrid
            //
            this._testGrid.AllowUserToAddRows = false;
            this._testGrid.AllowUserToDeleteRows = false;
            this._testGrid.AllowUserToOrderColumns = false;
            this._testGrid.AllowUserToResizeRows = false;
            this._testGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._testGrid.BackgroundColor = System.Drawing.Color.White;
            this._testGrid.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._testGrid.ColumnHeadersDefaultCellStyle = headerStyle;
            this._testGrid.ColumnHeadersHeight = 34;
            this._testGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._testGrid.EnableHeadersVisualStyles = false;
            this._testGrid.Location = new System.Drawing.Point(441, 27);
            this._testGrid.MultiSelect = false;
            this._testGrid.Name = "_testGrid";
            this._testGrid.RowHeadersVisible = false;
            this._testGrid.RowTemplate.Height = 30;
            this._testGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this._testGrid.Size = new System.Drawing.Size(270, 331);
            this._testGrid.TabIndex = 3;
            // colAxis
            // 
            this.colAxis.FillWeight = 105F;
            this.colAxis.HeaderText = "Axis";
            this.colAxis.Name = "colAxis";
            this.colAxis.ReadOnly = true;
            this.colAxis.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colCurrent
            // 
            this.colCurrent.DefaultCellStyle = rightStyle;
            this.colCurrent.FillWeight = 70F;
            this.colCurrent.HeaderText = "Current";
            this.colCurrent.Name = "colCurrent";
            this.colCurrent.ReadOnly = true;
            this.colCurrent.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colTarget
            // 
            this.colTarget.DefaultCellStyle = rightStyle;
            this.colTarget.FillWeight = 70F;
            this.colTarget.HeaderText = "Move Target";
            this.colTarget.Name = "colTarget";
            this.colTarget.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colVelocity
            // 
            this.colVelocity.DefaultCellStyle = rightStyle;
            this.colVelocity.FillWeight = 70F;
            this.colVelocity.HeaderText = "Velocity";
            this.colVelocity.Name = "colVelocity";
            this.colVelocity.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // colStatus
            // 
            this.colStatus.DefaultCellStyle = centerStyle;
            this.colStatus.FillWeight = 80F;
            this.colStatus.HeaderText = "Status";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colMessage
            // 
            this.colMessage.FillWeight = 180F;
            this.colMessage.HeaderText = "Message";
            this.colMessage.Name = "colMessage";
            this.colMessage.ReadOnly = true;
            this.colMessage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // pnlButtons
            // 
            this.pnlButtons.ColumnCount = 10;
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 102F));
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 102F));
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 118F));
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 102F));
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 102F));
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 122F));
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 132F));
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 102F));
            this.pnlButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.pnlButtons.Controls.Add(this.btnReload, 0, 0);
            this.pnlButtons.Controls.Add(this.btnSave, 1, 0);
            this.pnlButtons.Controls.Add(this.btnApply, 2, 0);
            this.pnlButtons.Controls.Add(this.btnValidate, 3, 0);
            this.pnlButtons.Controls.Add(this.btnHelp, 4, 0);
            this.pnlButtons.Controls.Add(this.btnMoveSelected, 5, 0);
            this.pnlButtons.Controls.Add(this.btnMoveHeadSub, 6, 0);
            this.pnlButtons.Controls.Add(this.btnStop, 8, 0);
            this.pnlButtons.Controls.Add(this.btnClose, 9, 0);
            this.pnlButtons.Controls.Add(this.btnHomeSelected, 0, 1);
            this.pnlButtons.SetColumnSpan(this.btnHomeSelected, 2);
            this.pnlButtons.Controls.Add(this.btnHomeAll, 2, 1);
            this.pnlButtons.Controls.Add(this.btnRefresh, 3, 1);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlButtons.Location = new System.Drawing.Point(3, 480);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.pnlButtons.RowCount = 2;
            this.pnlButtons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.pnlButtons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.pnlButtons.Size = new System.Drawing.Size(1154, 82);
            this.pnlButtons.TabIndex = 3;
            // 
            // buttons
            // 
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.Location = new System.Drawing.Point(3, 11);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(96, 32);
            this.btnReload.TabIndex = 0;
            this.btnReload.Text = "Reload";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Location = new System.Drawing.Point(105, 11);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(96, 32);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApply.Location = new System.Drawing.Point(207, 11);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(112, 32);
            this.btnApply.TabIndex = 2;
            this.btnApply.Text = "Save + Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            this.btnValidate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnValidate.Location = new System.Drawing.Point(325, 11);
            this.btnValidate.Name = "btnValidate";
            this.btnValidate.Size = new System.Drawing.Size(96, 32);
            this.btnValidate.TabIndex = 3;
            this.btnValidate.Text = "Validate";
            this.btnValidate.UseVisualStyleBackColor = true;
            this.btnValidate.Click += new System.EventHandler(this.btnValidate_Click);
            this.btnHelp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHelp.Location = new System.Drawing.Point(427, 11);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(96, 32);
            this.btnHelp.TabIndex = 4;
            this.btnHelp.Text = "설명";
            this.btnHelp.UseVisualStyleBackColor = true;
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            this.btnMoveSelected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveSelected.Location = new System.Drawing.Point(529, 11);
            this.btnMoveSelected.Name = "btnMoveSelected";
            this.btnMoveSelected.Size = new System.Drawing.Size(116, 32);
            this.btnMoveSelected.TabIndex = 5;
            this.btnMoveSelected.Text = "Move Selected";
            this.btnMoveSelected.UseVisualStyleBackColor = true;
            this.btnMoveSelected.Click += new System.EventHandler(this.btnMoveSelected_Click);
            this.btnMoveHeadSub.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveHeadSub.Location = new System.Drawing.Point(651, 11);
            this.btnMoveHeadSub.Name = "btnMoveHeadSub";
            this.btnMoveHeadSub.Size = new System.Drawing.Size(126, 32);
            this.btnMoveHeadSub.TabIndex = 7;
            this.btnMoveHeadSub.Text = "Move Head/Sub";
            this.btnMoveHeadSub.UseVisualStyleBackColor = true;
            this.btnMoveHeadSub.Click += new System.EventHandler(this.btnMoveHeadSub_Click);
            this.btnHomeSelected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHomeSelected.Location = new System.Drawing.Point(3, 49);
            this.btnHomeSelected.Name = "btnHomeSelected";
            this.btnHomeSelected.Size = new System.Drawing.Size(126, 32);
            this.btnHomeSelected.TabIndex = 9;
            this.btnHomeSelected.Text = "Home Selected X";
            this.btnHomeSelected.UseVisualStyleBackColor = true;
            this.btnHomeSelected.Click += new System.EventHandler(this.btnHomeSelected_Click);
            this.btnHomeAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHomeAll.Location = new System.Drawing.Point(207, 49);
            this.btnHomeAll.Name = "btnHomeAll";
            this.btnHomeAll.Size = new System.Drawing.Size(102, 32);
            this.btnHomeAll.TabIndex = 10;
            this.btnHomeAll.Text = "Home All X";
            this.btnHomeAll.UseVisualStyleBackColor = true;
            this.btnHomeAll.Click += new System.EventHandler(this.btnHomeAll_Click);
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefresh.Location = new System.Drawing.Point(325, 49);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(84, 32);
            this.btnRefresh.TabIndex = 11;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(176, 48, 48);
            this.btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(1067, 11);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(96, 32);
            this.btnStop.TabIndex = 12;
            this.btnStop.Text = "STOP";
            this.btnStop.UseVisualStyleBackColor = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.Location = new System.Drawing.Point(1169, 11);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(84, 32);
            this.btnClose.TabIndex = 13;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // lblPath
            // 
            this.lblPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPath.ForeColor = System.Drawing.Color.DimGray;
            this.lblPath.Location = new System.Drawing.Point(3, 565);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(1154, 24);
            this.lblPath.TabIndex = 4;
            this.lblPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblStatus.Location = new System.Drawing.Point(3, 589);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1154, 28);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.Text = "Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SharedRailXSetupDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 641);
            this.Controls.Add(this.layoutRoot);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimumSize = new System.Drawing.Size(1080, 520);
            this.Name = "SharedRailXSetupDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SharedRailX Setup";
            this.Load += new System.EventHandler(this.SharedRailXSetupDialog_Load);
            this.layoutRoot.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this._gridGroups.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._testGrid)).EndInit();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
