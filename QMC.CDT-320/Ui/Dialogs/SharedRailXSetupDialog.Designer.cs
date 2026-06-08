namespace QMC.CDT_320.Ui.Dialogs
{
    partial class SharedRailXSetupDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblDefaultSafety;
        private System.Windows.Forms.TextBox txtDefaultSafety;
        private System.Windows.Forms.CheckBox chkPathCheck;
        private System.Windows.Forms.CheckBox chkSameVelocity;
        private System.Windows.Forms.Label lblHeadAxis;
        private System.Windows.Forms.ComboBox cboHeadAxis;
        private System.Windows.Forms.Label lblSubAxes;
        private System.Windows.Forms.CheckBox chkSubInputVision;
        private System.Windows.Forms.CheckBox chkSubOutputVision;
        private System.Windows.Forms.CheckBox chkSubFrontPicker;
        private System.Windows.Forms.CheckBox chkSubRearPicker;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.FlowLayoutPanel pnlButtons;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnValidate;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.Button btnMoveSelected;
        private System.Windows.Forms.Button btnMoveHeadSub;
        private System.Windows.Forms.Button btnMoveAll;
        private System.Windows.Forms.Button btnHomeSelected;
        private System.Windows.Forms.Button btnHomeAll;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAxis;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCurrent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTarget;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVelocity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBodyMin;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBodyMax;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOrigin;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScale;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSafety;
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
            this.lblDefaultSafety = new System.Windows.Forms.Label();
            this.txtDefaultSafety = new System.Windows.Forms.TextBox();
            this.chkPathCheck = new System.Windows.Forms.CheckBox();
            this.chkSameVelocity = new System.Windows.Forms.CheckBox();
            this.lblHeadAxis = new System.Windows.Forms.Label();
            this.cboHeadAxis = new System.Windows.Forms.ComboBox();
            this.lblSubAxes = new System.Windows.Forms.Label();
            this.chkSubInputVision = new System.Windows.Forms.CheckBox();
            this.chkSubOutputVision = new System.Windows.Forms.CheckBox();
            this.chkSubFrontPicker = new System.Windows.Forms.CheckBox();
            this.chkSubRearPicker = new System.Windows.Forms.CheckBox();
            this.grid = new System.Windows.Forms.DataGridView();
            this.colAxis = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCurrent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colVelocity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBodyMin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBodyMax = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOrigin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScale = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSafety = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnValidate = new System.Windows.Forms.Button();
            this.btnHelp = new System.Windows.Forms.Button();
            this.btnMoveSelected = new System.Windows.Forms.Button();
            this.btnMoveHeadSub = new System.Windows.Forms.Button();
            this.btnMoveAll = new System.Windows.Forms.Button();
            this.btnHomeSelected = new System.Windows.Forms.Button();
            this.btnHomeAll = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblPath = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.layoutRoot.SuspendLayout();
            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // layoutRoot
            // 
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Controls.Add(this.lblTitle, 0, 0);
            this.layoutRoot.Controls.Add(this.pnlTop, 0, 1);
            this.layoutRoot.Controls.Add(this.grid, 0, 2);
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
            this.lblTitle.Font = new System.Drawing.Font("Malgun Gothic", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(3, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(1154, 34);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "SharedRailX Setup / Move / X Home Test";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.lblDefaultSafety);
            this.pnlTop.Controls.Add(this.txtDefaultSafety);
            this.pnlTop.Controls.Add(this.chkPathCheck);
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
            // 
            // lblDefaultSafety
            // 
            this.lblDefaultSafety.Location = new System.Drawing.Point(0, 8);
            this.lblDefaultSafety.Name = "lblDefaultSafety";
            this.lblDefaultSafety.Size = new System.Drawing.Size(164, 22);
            this.lblDefaultSafety.TabIndex = 0;
            this.lblDefaultSafety.Text = "Default Safety (mm)";
            this.lblDefaultSafety.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtDefaultSafety
            // 
            this.txtDefaultSafety.Location = new System.Drawing.Point(170, 7);
            this.txtDefaultSafety.Name = "txtDefaultSafety";
            this.txtDefaultSafety.Size = new System.Drawing.Size(90, 23);
            this.txtDefaultSafety.TabIndex = 1;
            this.txtDefaultSafety.Text = "10";
            this.txtDefaultSafety.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // chkPathCheck
            // 
            this.chkPathCheck.AutoSize = true;
            this.chkPathCheck.Checked = true;
            this.chkPathCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPathCheck.Location = new System.Drawing.Point(286, 9);
            this.chkPathCheck.Name = "chkPathCheck";
            this.chkPathCheck.Size = new System.Drawing.Size(125, 19);
            this.chkPathCheck.TabIndex = 2;
            this.chkPathCheck.Text = "Enable Path Check";
            this.chkPathCheck.UseVisualStyleBackColor = true;
            // 
            // chkSameVelocity
            // 
            this.chkSameVelocity.AutoSize = true;
            this.chkSameVelocity.Checked = true;
            this.chkSameVelocity.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSameVelocity.Location = new System.Drawing.Point(433, 9);
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
            headerStyle.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
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
            this.colBodyMin,
            this.colBodyMax,
            this.colOrigin,
            this.colScale,
            this.colSafety,
            this.colStatus,
            this.colMessage});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Location = new System.Drawing.Point(3, 113);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.RowHeadersVisible = false;
            this.grid.RowTemplate.Height = 30;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1154, 361);
            this.grid.TabIndex = 2;
            // 
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
            // 
            // colBodyMin
            // 
            this.colBodyMin.DefaultCellStyle = rightStyle;
            this.colBodyMin.FillWeight = 75F;
            this.colBodyMin.HeaderText = "Body Min";
            this.colBodyMin.Name = "colBodyMin";
            this.colBodyMin.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colBodyMax
            // 
            this.colBodyMax.DefaultCellStyle = rightStyle;
            this.colBodyMax.FillWeight = 75F;
            this.colBodyMax.HeaderText = "Body Max";
            this.colBodyMax.Name = "colBodyMax";
            this.colBodyMax.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colOrigin
            // 
            this.colOrigin.DefaultCellStyle = rightStyle;
            this.colOrigin.FillWeight = 80F;
            this.colOrigin.HeaderText = "Rail Origin";
            this.colOrigin.Name = "colOrigin";
            this.colOrigin.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colScale
            // 
            this.colScale.DefaultCellStyle = rightStyle;
            this.colScale.FillWeight = 65F;
            this.colScale.HeaderText = "Scale";
            this.colScale.Name = "colScale";
            this.colScale.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colSafety
            // 
            this.colSafety.DefaultCellStyle = rightStyle;
            this.colSafety.FillWeight = 70F;
            this.colSafety.HeaderText = "Safety";
            this.colSafety.Name = "colSafety";
            this.colSafety.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
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
            this.pnlButtons.Controls.Add(this.btnReload);
            this.pnlButtons.Controls.Add(this.btnSave);
            this.pnlButtons.Controls.Add(this.btnApply);
            this.pnlButtons.Controls.Add(this.btnValidate);
            this.pnlButtons.Controls.Add(this.btnHelp);
            this.pnlButtons.Controls.Add(this.btnMoveSelected);
            this.pnlButtons.Controls.Add(this.btnMoveHeadSub);
            this.pnlButtons.Controls.Add(this.btnMoveAll);
            this.pnlButtons.Controls.Add(this.btnHomeSelected);
            this.pnlButtons.Controls.Add(this.btnHomeAll);
            this.pnlButtons.Controls.Add(this.btnRefresh);
            this.pnlButtons.Controls.Add(this.btnClose);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlButtons.Location = new System.Drawing.Point(3, 480);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.pnlButtons.Size = new System.Drawing.Size(1154, 82);
            this.pnlButtons.TabIndex = 3;
            // 
            // buttons
            // 
            this.btnReload.Location = new System.Drawing.Point(3, 11);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(96, 32);
            this.btnReload.TabIndex = 0;
            this.btnReload.Text = "Reload";
            this.btnReload.UseVisualStyleBackColor = true;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            this.btnSave.Location = new System.Drawing.Point(105, 11);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(96, 32);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnApply.Location = new System.Drawing.Point(207, 11);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(112, 32);
            this.btnApply.TabIndex = 2;
            this.btnApply.Text = "Save + Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            this.btnValidate.Location = new System.Drawing.Point(325, 11);
            this.btnValidate.Name = "btnValidate";
            this.btnValidate.Size = new System.Drawing.Size(96, 32);
            this.btnValidate.TabIndex = 3;
            this.btnValidate.Text = "Validate";
            this.btnValidate.UseVisualStyleBackColor = true;
            this.btnValidate.Click += new System.EventHandler(this.btnValidate_Click);
            this.btnHelp.Location = new System.Drawing.Point(427, 11);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(96, 32);
            this.btnHelp.TabIndex = 4;
            this.btnHelp.Text = "설명";
            this.btnHelp.UseVisualStyleBackColor = true;
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            this.btnMoveSelected.Location = new System.Drawing.Point(529, 11);
            this.btnMoveSelected.Name = "btnMoveSelected";
            this.btnMoveSelected.Size = new System.Drawing.Size(116, 32);
            this.btnMoveSelected.TabIndex = 5;
            this.btnMoveSelected.Text = "Move Selected";
            this.btnMoveSelected.UseVisualStyleBackColor = true;
            this.btnMoveSelected.Click += new System.EventHandler(this.btnMoveSelected_Click);
            this.btnMoveHeadSub.Location = new System.Drawing.Point(651, 11);
            this.btnMoveHeadSub.Name = "btnMoveHeadSub";
            this.btnMoveHeadSub.Size = new System.Drawing.Size(126, 32);
            this.btnMoveHeadSub.TabIndex = 7;
            this.btnMoveHeadSub.Text = "Move Head/Sub";
            this.btnMoveHeadSub.UseVisualStyleBackColor = true;
            this.btnMoveHeadSub.Click += new System.EventHandler(this.btnMoveHeadSub_Click);
            this.btnMoveAll.Location = new System.Drawing.Point(783, 11);
            this.btnMoveAll.Name = "btnMoveAll";
            this.btnMoveAll.Size = new System.Drawing.Size(94, 32);
            this.btnMoveAll.TabIndex = 8;
            this.btnMoveAll.Text = "Move All";
            this.btnMoveAll.UseVisualStyleBackColor = true;
            this.btnMoveAll.Click += new System.EventHandler(this.btnMoveAll_Click);
            this.btnHomeSelected.Location = new System.Drawing.Point(3, 49);
            this.btnHomeSelected.Name = "btnHomeSelected";
            this.btnHomeSelected.Size = new System.Drawing.Size(126, 32);
            this.btnHomeSelected.TabIndex = 9;
            this.btnHomeSelected.Text = "Home Selected X";
            this.btnHomeSelected.UseVisualStyleBackColor = true;
            this.btnHomeSelected.Click += new System.EventHandler(this.btnHomeSelected_Click);
            this.btnHomeAll.Location = new System.Drawing.Point(135, 49);
            this.btnHomeAll.Name = "btnHomeAll";
            this.btnHomeAll.Size = new System.Drawing.Size(102, 32);
            this.btnHomeAll.TabIndex = 10;
            this.btnHomeAll.Text = "Home All X";
            this.btnHomeAll.UseVisualStyleBackColor = true;
            this.btnHomeAll.Click += new System.EventHandler(this.btnHomeAll_Click);
            this.btnRefresh.Location = new System.Drawing.Point(243, 49);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(84, 32);
            this.btnRefresh.TabIndex = 11;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            this.btnClose.Location = new System.Drawing.Point(333, 49);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(84, 32);
            this.btnClose.TabIndex = 12;
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
            this.lblStatus.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
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
            this.Font = new System.Drawing.Font("Malgun Gothic", 9F);
            this.MinimumSize = new System.Drawing.Size(1080, 520);
            this.Name = "SharedRailXSetupDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SharedRailX Setup";
            this.Load += new System.EventHandler(this.SharedRailXSetupDialog_Load);
            this.layoutRoot.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
