namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ColletCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.GroupBox groupSettings;
        private System.Windows.Forms.DataGridView gridSettings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSettingUnit;
        private System.Windows.Forms.GroupBox groupResults;
        private System.Windows.Forms.DataGridView gridResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn colItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSide;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCollet;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOffsetX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOffsetY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTheta;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTZero;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFinalX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFinalY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValid;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private CalibrationDialogButton btnCheck;
        private CalibrationDialogButton btnStart;
        private CalibrationDialogButton btnSaveBottomTeaching;
        private CalibrationDialogButton btnApplyHomeOffset;
        private CalibrationDialogButton btnMoveZForward;
        private CalibrationDialogButton btnMoveYAvoid;
        private CalibrationDialogButton btnReload;
        private CalibrationDialogButton btnSave;
        private CalibrationDialogButton btnClose;

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
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.groupSettings = new System.Windows.Forms.GroupBox();
            this.gridSettings = new System.Windows.Forms.DataGridView();
            this.colSettingName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSettingUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupResults = new System.Windows.Forms.GroupBox();
            this.gridResults = new System.Windows.Forms.DataGridView();
            this.colItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSide = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCollet = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOffsetX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOffsetY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTheta = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTZero = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFinalX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFinalY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnCheck = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnStart = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnSaveBottomTeaching = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnApplyHomeOffset = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnMoveZForward = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnMoveYAvoid = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnReload = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnSave = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.btnClose = new QMC.CDT_320.Ui.Dialogs.CalibrationDialogButton();
            this.rootLayout.SuspendLayout();
            this.mainLayout.SuspendLayout();
            this.groupSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).BeginInit();
            this.groupResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridResults)).BeginInit();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.mainLayout, 0, 1);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 2);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.rootLayout.Size = new System.Drawing.Size(1134, 737);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 15F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1134, 56);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "COLLET CALIBRATION";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 338F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.groupSettings, 0, 0);
            this.mainLayout.Controls.Add(this.groupResults, 1, 0);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(10, 69);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(10, 13, 10, 13);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 1;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Size = new System.Drawing.Size(1114, 538);
            this.mainLayout.TabIndex = 1;
            // 
            // groupSettings
            // 
            this.groupSettings.Controls.Add(this.gridSettings);
            this.groupSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupSettings.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupSettings.Location = new System.Drawing.Point(0, 0);
            this.groupSettings.Margin = new System.Windows.Forms.Padding(0, 0, 9, 0);
            this.groupSettings.Name = "groupSettings";
            this.groupSettings.Size = new System.Drawing.Size(329, 538);
            this.groupSettings.TabIndex = 0;
            this.groupSettings.TabStop = false;
            this.groupSettings.Text = "SETTING";
            // 
            // gridSettings
            // 
            this.gridSettings.AllowUserToAddRows = false;
            this.gridSettings.AllowUserToDeleteRows = false;
            this.gridSettings.AllowUserToResizeRows = false;
            this.gridSettings.BackgroundColor = System.Drawing.Color.White;
            this.gridSettings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSettings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSettingName,
            this.colSettingValue,
            this.colSettingUnit});
            this.gridSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSettings.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.gridSettings.Location = new System.Drawing.Point(3, 21);
            this.gridSettings.MultiSelect = false;
            this.gridSettings.Name = "gridSettings";
            this.gridSettings.RowHeadersVisible = false;
            this.gridSettings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSettings.Size = new System.Drawing.Size(323, 514);
            this.gridSettings.TabIndex = 0;
            this.gridSettings.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.gridSettings_CellBeginEdit);
            this.gridSettings.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSettings_CellDoubleClick);
            this.gridSettings.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this.gridSettings_CellToolTipTextNeeded);
            this.gridSettings.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridSettings_CellValueChanged);
            this.gridSettings.CurrentCellDirtyStateChanged += new System.EventHandler(this.gridSettings_CurrentCellDirtyStateChanged);
            // 
            // colSettingName
            // 
            this.colSettingName.HeaderText = "PARAMETER";
            this.colSettingName.Name = "colSettingName";
            this.colSettingName.ReadOnly = true;
            this.colSettingName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colSettingName.Width = 155;
            // 
            // colSettingValue
            // 
            this.colSettingValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colSettingValue.HeaderText = "VALUE";
            this.colSettingValue.Name = "colSettingValue";
            this.colSettingValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colSettingUnit
            // 
            this.colSettingUnit.HeaderText = "UNIT";
            this.colSettingUnit.Name = "colSettingUnit";
            this.colSettingUnit.ReadOnly = true;
            this.colSettingUnit.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colSettingUnit.Width = 50;
            // 
            // groupResults
            // 
            this.groupResults.Controls.Add(this.gridResults);
            this.groupResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupResults.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.groupResults.Location = new System.Drawing.Point(338, 0);
            this.groupResults.Margin = new System.Windows.Forms.Padding(0);
            this.groupResults.Name = "groupResults";
            this.groupResults.Size = new System.Drawing.Size(776, 538);
            this.groupResults.TabIndex = 1;
            this.groupResults.TabStop = false;
            this.groupResults.Text = "SAVED COLLET OFFSET";
            // 
            // gridResults
            // 
            this.gridResults.AllowUserToAddRows = false;
            this.gridResults.AllowUserToDeleteRows = false;
            this.gridResults.BackgroundColor = System.Drawing.Color.White;
            this.gridResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colItem,
            this.colSide,
            this.colCollet,
            this.colOffsetX,
            this.colOffsetY,
            this.colTheta,
            this.colTZero,
            this.colFinalX,
            this.colFinalY,
            this.colValid});
            this.gridResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridResults.Location = new System.Drawing.Point(3, 21);
            this.gridResults.MultiSelect = false;
            this.gridResults.Name = "gridResults";
            this.gridResults.ReadOnly = true;
            this.gridResults.RowHeadersVisible = false;
            this.gridResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridResults.Size = new System.Drawing.Size(770, 514);
            this.gridResults.TabIndex = 0;
            // 
            // colItem
            // 
            this.colItem.HeaderText = "ITEM";
            this.colItem.Name = "colItem";
            this.colItem.ReadOnly = true;
            this.colItem.ToolTipText = "저장된 Collet Calibration 항목입니다.";
            this.colItem.Width = 80;
            // 
            // colSide
            // 
            this.colSide.HeaderText = "SIDE";
            this.colSide.Name = "colSide";
            this.colSide.ReadOnly = true;
            this.colSide.ToolTipText = "Front 또는 Rear Picker Side입니다.";
            this.colSide.Width = 60;
            // 
            // colCollet
            // 
            this.colCollet.HeaderText = "COLLET";
            this.colCollet.Name = "colCollet";
            this.colCollet.ReadOnly = true;
            this.colCollet.ToolTipText = "Collet 번호입니다. #4는 Bottom 기준 Collet로 사용합니다.";
            this.colCollet.Width = 60;
            // 
            // colOffsetX
            // 
            this.colOffsetX.HeaderText = "OFFSET X";
            this.colOffsetX.Name = "colOffsetX";
            this.colOffsetX.ReadOnly = true;
            this.colOffsetX.ToolTipText = "#3/#2/#1은 #4 Bottom 기준 피치 위치에서 최종 OK 위치까지의 X 보정량입니다. #4는 기준이므로 0으로 저장합니다.";
            this.colOffsetX.Width = 85;
            // 
            // colOffsetY
            // 
            this.colOffsetY.HeaderText = "OFFSET Y";
            this.colOffsetY.Name = "colOffsetY";
            this.colOffsetY.ReadOnly = true;
            this.colOffsetY.ToolTipText = "#3/#2/#1은 #4 Bottom 기준 위치에서 최종 OK 위치까지의 Y 보정량입니다. #4는 기준이므로 0으로 저장합니다.";
            this.colOffsetY.Width = 85;
            // 
            // colTheta
            // 
            this.colTheta.HeaderText = "THETA";
            this.colTheta.Name = "colTheta";
            this.colTheta.ReadOnly = true;
            this.colTheta.ToolTipText = "최종 Vision 결과의 T/Theta 보정값입니다.";
            this.colTheta.Width = 80;
            // 
            // colTZero
            // 
            this.colTZero.HeaderText = "T ZERO";
            this.colTZero.Name = "colTZero";
            this.colTZero.ReadOnly = true;
            this.colTZero.ToolTipText = "현재 적용 중인 PC Offset에 캘리브레이션 잔여 T 오차를 더한 절대 보정값입니다. APPLY T HOME 시 보드에 쓰지 않고 Picker T 홈 완료 후 이동/0점 설정값으로 사용합니다.";
            this.colTZero.Width = 80;
            // 
            // colFinalX
            // 
            this.colFinalX.HeaderText = "FINAL X";
            this.colFinalX.Name = "colFinalX";
            this.colFinalX.ReadOnly = true;
            this.colFinalX.ToolTipText = "캘리브레이션이 OK로 끝난 최종 Picker X 실제 위치입니다.";
            this.colFinalX.Width = 85;
            // 
            // colFinalY
            // 
            this.colFinalY.HeaderText = "FINAL Y";
            this.colFinalY.Name = "colFinalY";
            this.colFinalY.ReadOnly = true;
            this.colFinalY.ToolTipText = "캘리브레이션이 OK로 끝난 최종 Picker Y 실제 위치입니다.";
            this.colFinalY.Width = 85;
            // 
            // colValid
            // 
            this.colValid.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colValid.HeaderText = "VALID";
            this.colValid.Name = "colValid";
            this.colValid.ReadOnly = true;
            this.colValid.ToolTipText = "이 Collet Calibration 결과가 검사 이동 보정에 사용 가능한 상태인지 표시합니다.";
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblStatus.Location = new System.Drawing.Point(10, 620);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1114, 50);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "대기 중입니다.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonPanel
            // 
            this.buttonPanel.ColumnCount = 9;
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.buttonPanel.Controls.Add(this.btnCheck, 0, 0);
            this.buttonPanel.Controls.Add(this.btnStart, 1, 0);
            this.buttonPanel.Controls.Add(this.btnSaveBottomTeaching, 2, 0);
            this.buttonPanel.Controls.Add(this.btnApplyHomeOffset, 3, 0);
            this.buttonPanel.Controls.Add(this.btnMoveZForward, 4, 0);
            this.buttonPanel.Controls.Add(this.btnMoveYAvoid, 5, 0);
            this.buttonPanel.Controls.Add(this.btnReload, 6, 0);
            this.buttonPanel.Controls.Add(this.btnSave, 7, 0);
            this.buttonPanel.Controls.Add(this.btnClose, 8, 0);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Location = new System.Drawing.Point(10, 670);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(10, 0, 10, 13);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.Size = new System.Drawing.Size(1114, 54);
            this.buttonPanel.TabIndex = 3;
            // 
            // btnCheck
            // 
            this.btnCheck.BackColor = System.Drawing.Color.White;
            this.btnCheck.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCheck.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCheck.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCheck.ForeColor = System.Drawing.Color.Black;
            this.btnCheck.Location = new System.Drawing.Point(6, 4);
            this.btnCheck.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnCheck.Size = new System.Drawing.Size(111, 46);
            this.btnCheck.TabIndex = 0;
            this.btnCheck.Text = "CHECK";
            this.btnCheck.UseVisualStyleBackColor = false;
            this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(0)))));
            this.btnStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStart.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(129, 4);
            this.btnStart.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.btnStart.Name = "btnStart";
            this.btnStart.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Primary;
            this.btnStart.Size = new System.Drawing.Size(111, 46);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "START";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnSaveBottomTeaching
            // 
            this.btnSaveBottomTeaching.BackColor = System.Drawing.Color.White;
            this.btnSaveBottomTeaching.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSaveBottomTeaching.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveBottomTeaching.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnSaveBottomTeaching.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveBottomTeaching.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSaveBottomTeaching.ForeColor = System.Drawing.Color.Black;
            this.btnSaveBottomTeaching.Location = new System.Drawing.Point(252, 4);
            this.btnSaveBottomTeaching.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.btnSaveBottomTeaching.Name = "btnSaveBottomTeaching";
            this.btnSaveBottomTeaching.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnSaveBottomTeaching.Size = new System.Drawing.Size(111, 46);
            this.btnSaveBottomTeaching.TabIndex = 2;
            this.btnSaveBottomTeaching.Text = "SAVE BOTTOM POS";
            this.btnSaveBottomTeaching.UseVisualStyleBackColor = false;
            this.btnSaveBottomTeaching.Click += new System.EventHandler(this.btnSaveBottomTeaching_Click);
            // 
            // btnApplyHomeOffset
            // 
            this.btnApplyHomeOffset.BackColor = System.Drawing.Color.White;
            this.btnApplyHomeOffset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApplyHomeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyHomeOffset.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnApplyHomeOffset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyHomeOffset.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnApplyHomeOffset.ForeColor = System.Drawing.Color.Black;
            this.btnApplyHomeOffset.Location = new System.Drawing.Point(375, 4);
            this.btnApplyHomeOffset.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.btnApplyHomeOffset.Name = "btnApplyHomeOffset";
            this.btnApplyHomeOffset.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnApplyHomeOffset.Size = new System.Drawing.Size(111, 46);
            this.btnApplyHomeOffset.TabIndex = 3;
            this.btnApplyHomeOffset.Text = "APPLY T HOME";
            this.btnApplyHomeOffset.UseVisualStyleBackColor = false;
            this.btnApplyHomeOffset.Click += new System.EventHandler(this.btnApplyHomeOffset_Click);
            // 
            // btnMoveZForward
            // 
            this.btnMoveZForward.BackColor = System.Drawing.Color.White;
            this.btnMoveZForward.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMoveZForward.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveZForward.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnMoveZForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMoveZForward.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnMoveZForward.ForeColor = System.Drawing.Color.Black;
            this.btnMoveZForward.Location = new System.Drawing.Point(498, 4);
            this.btnMoveZForward.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.btnMoveZForward.Name = "btnMoveZForward";
            this.btnMoveZForward.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnMoveZForward.Size = new System.Drawing.Size(111, 46);
            this.btnMoveZForward.TabIndex = 4;
            this.btnMoveZForward.Text = "Z MOVE";
            this.btnMoveZForward.UseVisualStyleBackColor = false;
            this.btnMoveZForward.Click += new System.EventHandler(this.btnMoveZForward_Click);
            // 
            // btnMoveYAvoid
            // 
            this.btnMoveYAvoid.BackColor = System.Drawing.Color.White;
            this.btnMoveYAvoid.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMoveYAvoid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveYAvoid.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnMoveYAvoid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMoveYAvoid.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnMoveYAvoid.ForeColor = System.Drawing.Color.Black;
            this.btnMoveYAvoid.Location = new System.Drawing.Point(621, 4);
            this.btnMoveYAvoid.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.btnMoveYAvoid.Name = "btnMoveYAvoid";
            this.btnMoveYAvoid.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnMoveYAvoid.Size = new System.Drawing.Size(111, 46);
            this.btnMoveYAvoid.TabIndex = 5;
            this.btnMoveYAvoid.Text = "P-Y AVOID";
            this.btnMoveYAvoid.UseVisualStyleBackColor = false;
            this.btnMoveYAvoid.Click += new System.EventHandler(this.btnMoveYAvoid_Click);
            // 
            // btnReload
            // 
            this.btnReload.BackColor = System.Drawing.Color.White;
            this.btnReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnReload.ForeColor = System.Drawing.Color.Black;
            this.btnReload.Location = new System.Drawing.Point(744, 4);
            this.btnReload.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.btnReload.Name = "btnReload";
            this.btnReload.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnReload.Size = new System.Drawing.Size(111, 46);
            this.btnReload.TabIndex = 6;
            this.btnReload.Text = "RELOAD";
            this.btnReload.UseVisualStyleBackColor = false;
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(867, 4);
            this.btnSave.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Dark;
            this.btnSave.Size = new System.Drawing.Size(111, 46);
            this.btnSave.TabIndex = 7;
            this.btnSave.Text = "SAVE";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.White;
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(176)))), ((int)(((byte)(176)))));
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.Black;
            this.btnClose.Location = new System.Drawing.Point(990, 4);
            this.btnClose.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.btnClose.Name = "btnClose";
            this.btnClose.Role = QMC.CDT_320.Ui.Dialogs.CalibrationDialogButtonRole.Normal;
            this.btnClose.Size = new System.Drawing.Size(118, 46);
            this.btnClose.TabIndex = 8;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ColletCalibrationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1134, 737);
            this.Controls.Add(this.rootLayout);
            this.MinimumSize = new System.Drawing.Size(842, 647);
            this.Name = "ColletCalibrationDialog";
            this.Text = "COLLET CALIBRATION";
            this.rootLayout.ResumeLayout(false);
            this.mainLayout.ResumeLayout(false);
            this.groupSettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSettings)).EndInit();
            this.groupResults.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridResults)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}


