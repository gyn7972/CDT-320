namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class MotionPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblPageHeader;
        private System.Windows.Forms.GroupBox grpModule;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.GroupBox grpConfiguration;
        private System.Windows.Forms.GroupBox grpAction;
        private System.Windows.Forms.TabControl configTabs;
        private System.Windows.Forms.TabPage tabStatus;
        private System.Windows.Forms.TabPage tabConfig;
        private System.Windows.Forms.TabPage tabSpeed;
        private System.Windows.Forms.TableLayoutPanel configLayout;
        private System.Windows.Forms.GroupBox grpConfig;
        private System.Windows.Forms.GroupBox grpInposition;
        private System.Windows.Forms.GroupBox grpHome;
        private System.Windows.Forms.GroupBox grpLimit;
        private System.Windows.Forms.GroupBox grpEmergency;
        private System.Windows.Forms.GroupBox grpAlarm;
        private System.Windows.Forms.GroupBox grpPositionClear;
        private QMC.CDT_320.Ui.Controls.ParamGrid pgConfig;
        private QMC.CDT_320.Ui.Controls.ParamGrid pgInposition;
        private QMC.CDT_320.Ui.Controls.ParamGrid pgHome;
        private QMC.CDT_320.Ui.Controls.ParamGrid pgLimit;
        private QMC.CDT_320.Ui.Controls.ParamGrid pgEmergency;
        private QMC.CDT_320.Ui.Controls.ParamGrid pgAlarm;
        private QMC.CDT_320.Ui.Controls.ParamGrid pgPositionClear;
        private System.Windows.Forms.TableLayoutPanel speedLayout;
        private System.Windows.Forms.DataGridView speedGrid;
        private System.Windows.Forms.FlowLayoutPanel speedButtons;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSpeedReload;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSpeedSave;
        private System.Windows.Forms.Label lblSpeedScaleCaption;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSpeedScale;
        private System.Windows.Forms.TableLayoutPanel actionsPanel;
        private QMC.CDT_320.Ui.Controls.ActionButton btnHome;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAllStop;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAlarmClear;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAllServoOff;
        private QMC.CDT_320.Ui.Controls.ActionButton btnServoOn;
        private QMC.CDT_320.Ui.Controls.ActionButton btnServoOff;
        private QMC.CDT_320.Ui.Controls.ActionButton btnParaLoad;
        private QMC.CDT_320.Ui.Controls.ActionButton btnParaSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnBoardScan;
        private QMC.CDT_320.Ui.Controls.ActionButton btnMotionTest;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPageHeader = new System.Windows.Forms.Label();
            this.grpModule = new System.Windows.Forms.GroupBox();
            this.grid = new System.Windows.Forms.DataGridView();
            this.INDEX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MODULE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.KEY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NO = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BOARD = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CH = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.STATUS = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SERVO = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.COMMAND_POSITION = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ACTUAL_POSITION = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.VELOCITY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DONE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.INP_DONE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOME_END = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ALARM = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PEL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MEL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ORG = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpConfiguration = new System.Windows.Forms.GroupBox();
            this.configTabs = new System.Windows.Forms.TabControl();
            this.tabStatus = new System.Windows.Forms.TabPage();
            this.tabConfig = new System.Windows.Forms.TabPage();
            this.configLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpConfig = new System.Windows.Forms.GroupBox();
            this.pgConfig = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.grpInposition = new System.Windows.Forms.GroupBox();
            this.pgInposition = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.grpLimit = new System.Windows.Forms.GroupBox();
            this.pgLimit = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.grpEmergency = new System.Windows.Forms.GroupBox();
            this.pgEmergency = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.grpHome = new System.Windows.Forms.GroupBox();
            this.pgHome = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.grpAlarm = new System.Windows.Forms.GroupBox();
            this.pgAlarm = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.grpPositionClear = new System.Windows.Forms.GroupBox();
            this.pgPositionClear = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.tabSpeed = new System.Windows.Forms.TabPage();
            this.speedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.speedGrid = new System.Windows.Forms.DataGridView();
            this.AXIS = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DEFAULT_VEL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ACCEL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DECEL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.STOP_DEC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOME_VEL_1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOME_VEL_2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOME_VEL_3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOME_VEL_4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOME_ACC_1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOME_DEC_1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOME_ACC_2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.HOME_DEC_2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.JOG_COARSE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.JOG_FINE = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.JOG_ACC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.JOG_DEC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.JOG_STOP_DEC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.INPOS_TOL = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.speedButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSpeedReload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSpeedSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSpeedScale = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.lblSpeedScaleCaption = new System.Windows.Forms.Label();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnHome = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAllStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAlarmClear = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAllServoOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnServoOn = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnServoOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnParaLoad = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnParaSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnBoardScan = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnMotionTest = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            this.grpModule.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.grpConfiguration.SuspendLayout();
            this.configTabs.SuspendLayout();
            this.tabConfig.SuspendLayout();
            this.configLayout.SuspendLayout();
            this.grpConfig.SuspendLayout();
            this.grpInposition.SuspendLayout();
            this.grpLimit.SuspendLayout();
            this.grpEmergency.SuspendLayout();
            this.grpHome.SuspendLayout();
            this.grpAlarm.SuspendLayout();
            this.grpPositionClear.SuspendLayout();
            this.tabSpeed.SuspendLayout();
            this.speedLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.speedGrid)).BeginInit();
            this.speedButtons.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblPageHeader, 0, 0);
            this.rootLayout.Controls.Add(this.grpModule, 0, 1);
            this.rootLayout.Controls.Add(this.grpConfiguration, 0, 2);
            this.rootLayout.Controls.Add(this.grpAction, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblPageHeader
            // 
            this.lblPageHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblPageHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblPageHeader.ForeColor = System.Drawing.Color.White;
            this.lblPageHeader.Location = new System.Drawing.Point(8, 8);
            this.lblPageHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblPageHeader.Name = "lblPageHeader";
            this.lblPageHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblPageHeader.Size = new System.Drawing.Size(1662, 30);
            this.lblPageHeader.TabIndex = 0;
            this.lblPageHeader.Text = "MOTION";
            this.lblPageHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpModule
            // 
            this.grpModule.Controls.Add(this.grid);
            this.grpModule.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpModule.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpModule.ForeColor = System.Drawing.Color.Black;
            this.grpModule.Location = new System.Drawing.Point(8, 38);
            this.grpModule.Margin = new System.Windows.Forms.Padding(0);
            this.grpModule.Name = "grpModule";
            this.grpModule.Padding = new System.Windows.Forms.Padding(1, 9, 1, 1);
            this.grpModule.Size = new System.Drawing.Size(1662, 384);
            this.grpModule.TabIndex = 1;
            this.grpModule.TabStop = false;
            this.grpModule.Text = "MODULE LIST";
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.grid.ColumnHeadersHeight = 29;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.INDEX,
            this.MODULE,
            this.KEY,
            this.NO,
            this.BOARD,
            this.CH,
            this.STATUS,
            this.SERVO,
            this.COMMAND_POSITION,
            this.ACTUAL_POSITION,
            this.VELOCITY,
            this.DONE,
            this.INP_DONE,
            this.HOME_END,
            this.ALARM,
            this.PEL,
            this.MEL,
            this.ORG});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.grid.Location = new System.Drawing.Point(1, 29);
            this.grid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.RowHeadersWidth = 51;
            this.grid.RowTemplate.Height = 26;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1660, 354);
            this.grid.TabIndex = 2;
            this.grid.SelectionChanged += new System.EventHandler(this.grid_SelectionChanged);
            // 
            // INDEX
            // 
            this.INDEX.HeaderText = "INDEX";
            this.INDEX.Name = "INDEX";
            this.INDEX.ReadOnly = true;
            // 
            // MODULE
            // 
            this.MODULE.HeaderText = "MODULE";
            this.MODULE.Name = "MODULE";
            this.MODULE.ReadOnly = true;
            // 
            // KEY
            // 
            this.KEY.HeaderText = "KEY";
            this.KEY.Name = "KEY";
            this.KEY.ReadOnly = true;
            // 
            // NO
            // 
            this.NO.HeaderText = "NO.";
            this.NO.Name = "NO";
            this.NO.ReadOnly = true;
            // 
            // BOARD
            // 
            this.BOARD.HeaderText = "BOARD";
            this.BOARD.Name = "BOARD";
            this.BOARD.ReadOnly = true;
            // 
            // CH
            // 
            this.CH.HeaderText = "CH";
            this.CH.Name = "CH";
            this.CH.ReadOnly = true;
            // 
            // STATUS
            // 
            this.STATUS.HeaderText = "STATUS";
            this.STATUS.Name = "STATUS";
            this.STATUS.ReadOnly = true;
            // 
            // SERVO
            // 
            this.SERVO.HeaderText = "SERVO";
            this.SERVO.Name = "SERVO";
            this.SERVO.ReadOnly = true;
            // 
            // COMMAND_POSITION
            // 
            this.COMMAND_POSITION.HeaderText = "COMMAND POSITION";
            this.COMMAND_POSITION.Name = "COMMAND_POSITION";
            this.COMMAND_POSITION.ReadOnly = true;
            // 
            // ACTUAL_POSITION
            // 
            this.ACTUAL_POSITION.HeaderText = "ACTUAL POSITION";
            this.ACTUAL_POSITION.Name = "ACTUAL_POSITION";
            this.ACTUAL_POSITION.ReadOnly = true;
            // 
            // VELOCITY
            // 
            this.VELOCITY.HeaderText = "VELOCITY";
            this.VELOCITY.Name = "VELOCITY";
            this.VELOCITY.ReadOnly = true;
            // 
            // DONE
            // 
            this.DONE.HeaderText = "DONE";
            this.DONE.Name = "DONE";
            this.DONE.ReadOnly = true;
            // 
            // INP_DONE
            // 
            this.INP_DONE.HeaderText = "INP DONE";
            this.INP_DONE.Name = "INP_DONE";
            this.INP_DONE.ReadOnly = true;
            // 
            // HOME_END
            // 
            this.HOME_END.HeaderText = "HOME END";
            this.HOME_END.Name = "HOME_END";
            this.HOME_END.ReadOnly = true;
            // 
            // ALARM
            // 
            this.ALARM.HeaderText = "ALARM";
            this.ALARM.Name = "ALARM";
            this.ALARM.ReadOnly = true;
            // 
            // PEL
            // 
            this.PEL.HeaderText = "PEL";
            this.PEL.Name = "PEL";
            this.PEL.ReadOnly = true;
            // 
            // MEL
            // 
            this.MEL.HeaderText = "MEL";
            this.MEL.Name = "MEL";
            this.MEL.ReadOnly = true;
            // 
            // ORG
            // 
            this.ORG.HeaderText = "ORG";
            this.ORG.Name = "ORG";
            this.ORG.ReadOnly = true;
            // 
            // grpConfiguration
            // 
            this.grpConfiguration.Controls.Add(this.configTabs);
            this.grpConfiguration.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpConfiguration.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpConfiguration.ForeColor = System.Drawing.Color.Black;
            this.grpConfiguration.Location = new System.Drawing.Point(8, 422);
            this.grpConfiguration.Margin = new System.Windows.Forms.Padding(0);
            this.grpConfiguration.Name = "grpConfiguration";
            this.grpConfiguration.Padding = new System.Windows.Forms.Padding(1, 9, 1, 1);
            this.grpConfiguration.Size = new System.Drawing.Size(1662, 384);
            this.grpConfiguration.TabIndex = 2;
            this.grpConfiguration.TabStop = false;
            this.grpConfiguration.Text = "CONFIGURATION";
            // 
            // configTabs
            // 
            this.configTabs.Alignment = System.Windows.Forms.TabAlignment.Left;
            this.configTabs.Controls.Add(this.tabStatus);
            this.configTabs.Controls.Add(this.tabConfig);
            this.configTabs.Controls.Add(this.tabSpeed);
            this.configTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configTabs.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.configTabs.ItemSize = new System.Drawing.Size(100, 32);
            this.configTabs.Location = new System.Drawing.Point(1, 29);
            this.configTabs.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.configTabs.Multiline = true;
            this.configTabs.Name = "configTabs";
            this.configTabs.SelectedIndex = 0;
            this.configTabs.Size = new System.Drawing.Size(1660, 354);
            this.configTabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.configTabs.TabIndex = 4;
            // 
            // tabStatus
            // 
            this.tabStatus.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabStatus.Location = new System.Drawing.Point(36, 4);
            this.tabStatus.Name = "tabStatus";
            this.tabStatus.Padding = new System.Windows.Forms.Padding(3);
            this.tabStatus.Size = new System.Drawing.Size(1620, 346);
            this.tabStatus.TabIndex = 0;
            this.tabStatus.Text = "STATUS";
            // 
            // tabConfig
            // 
            this.tabConfig.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabConfig.Controls.Add(this.configLayout);
            this.tabConfig.Location = new System.Drawing.Point(36, 4);
            this.tabConfig.Name = "tabConfig";
            this.tabConfig.Padding = new System.Windows.Forms.Padding(8);
            this.tabConfig.Size = new System.Drawing.Size(1620, 346);
            this.tabConfig.TabIndex = 1;
            this.tabConfig.Text = "CONFIG";
            // 
            // configLayout
            // 
            this.configLayout.AutoScroll = true;
            this.configLayout.ColumnCount = 3;
            this.configLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.configLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.configLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.configLayout.Controls.Add(this.grpConfig, 0, 0);
            this.configLayout.Controls.Add(this.grpInposition, 1, 0);
            this.configLayout.Controls.Add(this.grpLimit, 2, 0);
            this.configLayout.Controls.Add(this.grpEmergency, 0, 1);
            this.configLayout.Controls.Add(this.grpHome, 1, 1);
            this.configLayout.Controls.Add(this.grpAlarm, 2, 1);
            this.configLayout.Controls.Add(this.grpPositionClear, 0, 2);
            this.configLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configLayout.Location = new System.Drawing.Point(8, 8);
            this.configLayout.Name = "configLayout";
            this.configLayout.RowCount = 3;
            this.configLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.configLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 140F));
            this.configLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.configLayout.Size = new System.Drawing.Size(1604, 330);
            this.configLayout.TabIndex = 0;
            // 
            // grpConfig
            // 
            this.grpConfig.Controls.Add(this.pgConfig);
            this.grpConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpConfig.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpConfig.Location = new System.Drawing.Point(3, 3);
            this.grpConfig.Name = "grpConfig";
            this.grpConfig.Size = new System.Drawing.Size(528, 184);
            this.grpConfig.TabIndex = 0;
            this.grpConfig.TabStop = false;
            this.grpConfig.Text = "CONFIG";
            // 
            // pgConfig
            // 
            this.pgConfig.AlertColor = System.Drawing.Color.IndianRed;
            this.pgConfig.BackColor = System.Drawing.Color.White;
            this.pgConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgConfig.EditableColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(73)))), ((int)(((byte)(125)))));
            this.pgConfig.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.pgConfig.Location = new System.Drawing.Point(3, 19);
            this.pgConfig.Name = "pgConfig";
            this.pgConfig.NameColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.pgConfig.NameWidth = 140;
            this.pgConfig.Padding = new System.Windows.Forms.Padding(6);
            this.pgConfig.PairsPerRow = 1;
            this.pgConfig.Placeholder = "?";
            this.pgConfig.RowHeight = 22;
            this.pgConfig.Size = new System.Drawing.Size(522, 162);
            this.pgConfig.TabIndex = 0;
            this.pgConfig.ValueColor = System.Drawing.Color.Black;
            // 
            // grpInposition
            // 
            this.grpInposition.Controls.Add(this.pgInposition);
            this.grpInposition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInposition.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpInposition.Location = new System.Drawing.Point(537, 3);
            this.grpInposition.Name = "grpInposition";
            this.grpInposition.Size = new System.Drawing.Size(528, 184);
            this.grpInposition.TabIndex = 1;
            this.grpInposition.TabStop = false;
            this.grpInposition.Text = "INPOSITION";
            // 
            // pgInposition
            // 
            this.pgInposition.AlertColor = System.Drawing.Color.IndianRed;
            this.pgInposition.BackColor = System.Drawing.Color.White;
            this.pgInposition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgInposition.EditableColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(73)))), ((int)(((byte)(125)))));
            this.pgInposition.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.pgInposition.Location = new System.Drawing.Point(3, 19);
            this.pgInposition.Name = "pgInposition";
            this.pgInposition.NameColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.pgInposition.NameWidth = 140;
            this.pgInposition.Padding = new System.Windows.Forms.Padding(6);
            this.pgInposition.PairsPerRow = 1;
            this.pgInposition.Placeholder = "?";
            this.pgInposition.RowHeight = 22;
            this.pgInposition.Size = new System.Drawing.Size(522, 162);
            this.pgInposition.TabIndex = 0;
            this.pgInposition.ValueColor = System.Drawing.Color.Black;
            // 
            // grpLimit
            // 
            this.grpLimit.Controls.Add(this.pgLimit);
            this.grpLimit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLimit.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpLimit.Location = new System.Drawing.Point(1071, 3);
            this.grpLimit.Name = "grpLimit";
            this.grpLimit.Size = new System.Drawing.Size(530, 184);
            this.grpLimit.TabIndex = 2;
            this.grpLimit.TabStop = false;
            this.grpLimit.Text = "LIMIT";
            // 
            // pgLimit
            // 
            this.pgLimit.AlertColor = System.Drawing.Color.IndianRed;
            this.pgLimit.BackColor = System.Drawing.Color.White;
            this.pgLimit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgLimit.EditableColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(73)))), ((int)(((byte)(125)))));
            this.pgLimit.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.pgLimit.Location = new System.Drawing.Point(3, 19);
            this.pgLimit.Name = "pgLimit";
            this.pgLimit.NameColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.pgLimit.NameWidth = 140;
            this.pgLimit.Padding = new System.Windows.Forms.Padding(6);
            this.pgLimit.PairsPerRow = 1;
            this.pgLimit.Placeholder = "?";
            this.pgLimit.RowHeight = 22;
            this.pgLimit.Size = new System.Drawing.Size(524, 162);
            this.pgLimit.TabIndex = 0;
            this.pgLimit.ValueColor = System.Drawing.Color.Black;
            // 
            // grpEmergency
            // 
            this.grpEmergency.Controls.Add(this.pgEmergency);
            this.grpEmergency.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpEmergency.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpEmergency.Location = new System.Drawing.Point(3, 193);
            this.grpEmergency.Name = "grpEmergency";
            this.grpEmergency.Size = new System.Drawing.Size(528, 134);
            this.grpEmergency.TabIndex = 3;
            this.grpEmergency.TabStop = false;
            this.grpEmergency.Text = "EMERGENCY SIGNAL";
            // 
            // pgEmergency
            // 
            this.pgEmergency.AlertColor = System.Drawing.Color.IndianRed;
            this.pgEmergency.BackColor = System.Drawing.Color.White;
            this.pgEmergency.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgEmergency.EditableColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(73)))), ((int)(((byte)(125)))));
            this.pgEmergency.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.pgEmergency.Location = new System.Drawing.Point(3, 19);
            this.pgEmergency.Name = "pgEmergency";
            this.pgEmergency.NameColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.pgEmergency.NameWidth = 140;
            this.pgEmergency.Padding = new System.Windows.Forms.Padding(6);
            this.pgEmergency.PairsPerRow = 1;
            this.pgEmergency.Placeholder = "?";
            this.pgEmergency.RowHeight = 22;
            this.pgEmergency.Size = new System.Drawing.Size(522, 112);
            this.pgEmergency.TabIndex = 0;
            this.pgEmergency.ValueColor = System.Drawing.Color.Black;
            // 
            // grpHome
            // 
            this.grpHome.Controls.Add(this.pgHome);
            this.grpHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpHome.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpHome.Location = new System.Drawing.Point(537, 193);
            this.grpHome.Name = "grpHome";
            this.grpHome.Size = new System.Drawing.Size(528, 134);
            this.grpHome.TabIndex = 4;
            this.grpHome.TabStop = false;
            this.grpHome.Text = "HOME";
            // 
            // pgHome
            // 
            this.pgHome.AlertColor = System.Drawing.Color.IndianRed;
            this.pgHome.BackColor = System.Drawing.Color.White;
            this.pgHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgHome.EditableColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(73)))), ((int)(((byte)(125)))));
            this.pgHome.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.pgHome.Location = new System.Drawing.Point(3, 19);
            this.pgHome.Name = "pgHome";
            this.pgHome.NameColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.pgHome.NameWidth = 140;
            this.pgHome.Padding = new System.Windows.Forms.Padding(6);
            this.pgHome.PairsPerRow = 1;
            this.pgHome.Placeholder = "?";
            this.pgHome.RowHeight = 22;
            this.pgHome.Size = new System.Drawing.Size(522, 112);
            this.pgHome.TabIndex = 0;
            this.pgHome.ValueColor = System.Drawing.Color.Black;
            // 
            // grpAlarm
            // 
            this.grpAlarm.Controls.Add(this.pgAlarm);
            this.grpAlarm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAlarm.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpAlarm.Location = new System.Drawing.Point(1071, 193);
            this.grpAlarm.Name = "grpAlarm";
            this.grpAlarm.Size = new System.Drawing.Size(530, 134);
            this.grpAlarm.TabIndex = 5;
            this.grpAlarm.TabStop = false;
            this.grpAlarm.Text = "ALARM";
            // 
            // pgAlarm
            // 
            this.pgAlarm.AlertColor = System.Drawing.Color.IndianRed;
            this.pgAlarm.BackColor = System.Drawing.Color.White;
            this.pgAlarm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgAlarm.EditableColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(73)))), ((int)(((byte)(125)))));
            this.pgAlarm.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.pgAlarm.Location = new System.Drawing.Point(3, 19);
            this.pgAlarm.Name = "pgAlarm";
            this.pgAlarm.NameColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.pgAlarm.NameWidth = 140;
            this.pgAlarm.Padding = new System.Windows.Forms.Padding(6);
            this.pgAlarm.PairsPerRow = 1;
            this.pgAlarm.Placeholder = "?";
            this.pgAlarm.RowHeight = 22;
            this.pgAlarm.Size = new System.Drawing.Size(524, 112);
            this.pgAlarm.TabIndex = 0;
            this.pgAlarm.ValueColor = System.Drawing.Color.Black;
            // 
            // grpPositionClear
            // 
            this.grpPositionClear.Controls.Add(this.pgPositionClear);
            this.grpPositionClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPositionClear.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpPositionClear.Location = new System.Drawing.Point(3, 333);
            this.grpPositionClear.Name = "grpPositionClear";
            this.grpPositionClear.Size = new System.Drawing.Size(528, 124);
            this.grpPositionClear.TabIndex = 6;
            this.grpPositionClear.TabStop = false;
            this.grpPositionClear.Text = "POSITION CLEAR";
            // 
            // pgPositionClear
            // 
            this.pgPositionClear.AlertColor = System.Drawing.Color.IndianRed;
            this.pgPositionClear.BackColor = System.Drawing.Color.White;
            this.pgPositionClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgPositionClear.EditableColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(73)))), ((int)(((byte)(125)))));
            this.pgPositionClear.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.pgPositionClear.Location = new System.Drawing.Point(3, 19);
            this.pgPositionClear.Name = "pgPositionClear";
            this.pgPositionClear.NameColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.pgPositionClear.NameWidth = 140;
            this.pgPositionClear.Padding = new System.Windows.Forms.Padding(6);
            this.pgPositionClear.PairsPerRow = 1;
            this.pgPositionClear.Placeholder = "?";
            this.pgPositionClear.RowHeight = 22;
            this.pgPositionClear.Size = new System.Drawing.Size(522, 102);
            this.pgPositionClear.TabIndex = 0;
            this.pgPositionClear.ValueColor = System.Drawing.Color.Black;
            // 
            // tabSpeed
            // 
            this.tabSpeed.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabSpeed.Controls.Add(this.speedLayout);
            this.tabSpeed.Location = new System.Drawing.Point(36, 4);
            this.tabSpeed.Name = "tabSpeed";
            this.tabSpeed.Padding = new System.Windows.Forms.Padding(8);
            this.tabSpeed.Size = new System.Drawing.Size(1620, 346);
            this.tabSpeed.TabIndex = 2;
            this.tabSpeed.Text = "SPEED";
            // 
            // speedLayout
            // 
            this.speedLayout.ColumnCount = 1;
            this.speedLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedLayout.Controls.Add(this.speedGrid, 0, 0);
            this.speedLayout.Controls.Add(this.speedButtons, 0, 1);
            this.speedLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedLayout.Location = new System.Drawing.Point(8, 8);
            this.speedLayout.Name = "speedLayout";
            this.speedLayout.RowCount = 2;
            this.speedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.speedLayout.Size = new System.Drawing.Size(1604, 330);
            this.speedLayout.TabIndex = 0;
            // 
            // speedGrid
            // 
            this.speedGrid.AllowUserToAddRows = false;
            this.speedGrid.AllowUserToDeleteRows = false;
            this.speedGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.speedGrid.BackgroundColor = System.Drawing.Color.White;
            this.speedGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.speedGrid.ColumnHeadersHeight = 29;
            this.speedGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.AXIS,
            this.DEFAULT_VEL,
            this.ACCEL,
            this.DECEL,
            this.STOP_DEC,
            this.HOME_VEL_1,
            this.HOME_VEL_2,
            this.HOME_VEL_3,
            this.HOME_VEL_4,
            this.HOME_ACC_1,
            this.HOME_DEC_1,
            this.HOME_ACC_2,
            this.HOME_DEC_2,
            this.JOG_COARSE,
            this.JOG_FINE,
            this.JOG_ACC,
            this.JOG_DEC,
            this.JOG_STOP_DEC,
            this.INPOS_TOL});
            this.speedGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedGrid.EnableHeadersVisualStyles = false;
            this.speedGrid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.speedGrid.Location = new System.Drawing.Point(0, 0);
            this.speedGrid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.speedGrid.MultiSelect = false;
            this.speedGrid.Name = "speedGrid";
            this.speedGrid.RowHeadersVisible = false;
            this.speedGrid.RowHeadersWidth = 51;
            this.speedGrid.RowTemplate.Height = 26;
            this.speedGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.speedGrid.Size = new System.Drawing.Size(1604, 274);
            this.speedGrid.TabIndex = 0;
            this.speedGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnSpeedCellDoubleClick);
            this.speedGrid.ColumnHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.OnSpeedHeaderDoubleClick);
            this.speedGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.speedGrid_DataError);
            // 
            // AXIS
            // 
            this.AXIS.HeaderText = "AXIS";
            this.AXIS.Name = "AXIS";
            // 
            // DEFAULT_VEL
            // 
            this.DEFAULT_VEL.HeaderText = "DEFAULT VEL";
            this.DEFAULT_VEL.Name = "DEFAULT_VEL";
            // 
            // ACCEL
            // 
            this.ACCEL.HeaderText = "ACCEL";
            this.ACCEL.Name = "ACCEL";
            // 
            // DECEL
            // 
            this.DECEL.HeaderText = "DECEL";
            this.DECEL.Name = "DECEL";
            // 
            // STOP_DEC
            // 
            this.STOP_DEC.HeaderText = "STOP DEC";
            this.STOP_DEC.Name = "STOP_DEC";
            // 
            // HOME_VEL_1
            // 
            this.HOME_VEL_1.HeaderText = "HOME VEL 1";
            this.HOME_VEL_1.Name = "HOME_VEL_1";
            // 
            // HOME_VEL_2
            // 
            this.HOME_VEL_2.HeaderText = "HOME VEL 2";
            this.HOME_VEL_2.Name = "HOME_VEL_2";
            // 
            // HOME_VEL_3
            // 
            this.HOME_VEL_3.HeaderText = "HOME VEL 3";
            this.HOME_VEL_3.Name = "HOME_VEL_3";
            // 
            // HOME_VEL_4
            // 
            this.HOME_VEL_4.HeaderText = "HOME VEL 4";
            this.HOME_VEL_4.Name = "HOME_VEL_4";
            // 
            // HOME_ACC_1
            // 
            this.HOME_ACC_1.HeaderText = "HOME ACC 1";
            this.HOME_ACC_1.Name = "HOME_ACC_1";
            // 
            // HOME_DEC_1
            // 
            this.HOME_DEC_1.HeaderText = "HOME DEC 1";
            this.HOME_DEC_1.Name = "HOME_DEC_1";
            // 
            // HOME_ACC_2
            // 
            this.HOME_ACC_2.HeaderText = "HOME ACC 2";
            this.HOME_ACC_2.Name = "HOME_ACC_2";
            // 
            // HOME_DEC_2
            // 
            this.HOME_DEC_2.HeaderText = "HOME DEC 2";
            this.HOME_DEC_2.Name = "HOME_DEC_2";
            // 
            // JOG_COARSE
            // 
            this.JOG_COARSE.HeaderText = "JOG COARSE";
            this.JOG_COARSE.Name = "JOG_COARSE";
            // 
            // JOG_FINE
            // 
            this.JOG_FINE.HeaderText = "JOG FINE";
            this.JOG_FINE.Name = "JOG_FINE";
            // 
            // JOG_ACC
            // 
            this.JOG_ACC.HeaderText = "JOG ACC";
            this.JOG_ACC.Name = "JOG_ACC";
            // 
            // JOG_DEC
            // 
            this.JOG_DEC.HeaderText = "JOG DEC";
            this.JOG_DEC.Name = "JOG_DEC";
            // 
            // JOG_STOP_DEC
            // 
            this.JOG_STOP_DEC.HeaderText = "JOG STOP DEC";
            this.JOG_STOP_DEC.Name = "JOG_STOP_DEC";
            // 
            // INPOS_TOL
            // 
            this.INPOS_TOL.HeaderText = "IN-POS TOL";
            this.INPOS_TOL.Name = "INPOS_TOL";
            // 
            // speedButtons
            // 
            this.speedButtons.Controls.Add(this.btnSpeedReload);
            this.speedButtons.Controls.Add(this.btnSpeedSave);
            this.speedButtons.Controls.Add(this.btnSpeedScale);
            this.speedButtons.Controls.Add(this.lblSpeedScaleCaption);
            this.speedButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.speedButtons.Location = new System.Drawing.Point(0, 280);
            this.speedButtons.Margin = new System.Windows.Forms.Padding(0);
            this.speedButtons.Name = "speedButtons";
            this.speedButtons.Size = new System.Drawing.Size(1604, 50);
            this.speedButtons.TabIndex = 1;
            // 
            // btnSpeedReload
            // 
            this.btnSpeedReload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSpeedReload.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnSpeedReload.BadgeText = "ACTION";
            this.btnSpeedReload.BorderColor = System.Drawing.Color.Empty;
            this.btnSpeedReload.BorderWidth = 0;
            this.btnSpeedReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSpeedReload.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSpeedReload.ForeColor = System.Drawing.Color.White;
            this.btnSpeedReload.Location = new System.Drawing.Point(1484, 6);
            this.btnSpeedReload.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnSpeedReload.Name = "btnSpeedReload";
            this.btnSpeedReload.Size = new System.Drawing.Size(120, 38);
            this.btnSpeedReload.TabIndex = 1;
            this.btnSpeedReload.Text = "RELOAD";
            this.btnSpeedReload.Click += new System.EventHandler(this.btnSpeedReload_Click);
            // 
            // btnSpeedSave
            // 
            this.btnSpeedSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSpeedSave.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnSpeedSave.BadgeText = "ACTION";
            this.btnSpeedSave.BorderColor = System.Drawing.Color.Empty;
            this.btnSpeedSave.BorderWidth = 0;
            this.btnSpeedSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSpeedSave.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSpeedSave.ForeColor = System.Drawing.Color.White;
            this.btnSpeedSave.Location = new System.Drawing.Point(1358, 6);
            this.btnSpeedSave.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnSpeedSave.Name = "btnSpeedSave";
            this.btnSpeedSave.Size = new System.Drawing.Size(120, 38);
            this.btnSpeedSave.TabIndex = 0;
            this.btnSpeedSave.Text = "SAVE";
            this.btnSpeedSave.Click += new System.EventHandler(this.btnSpeedSave_Click);
            // 
            // btnSpeedScale
            // 
            this.btnSpeedScale.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSpeedScale.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnSpeedScale.BadgeText = "ACTION";
            this.btnSpeedScale.BorderColor = System.Drawing.Color.Empty;
            this.btnSpeedScale.BorderWidth = 0;
            this.btnSpeedScale.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSpeedScale.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSpeedScale.ForeColor = System.Drawing.Color.White;
            this.btnSpeedScale.Location = new System.Drawing.Point(1242, 6);
            this.btnSpeedScale.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnSpeedScale.Name = "btnSpeedScale";
            this.btnSpeedScale.Size = new System.Drawing.Size(110, 38);
            this.btnSpeedScale.TabIndex = 2;
            this.btnSpeedScale.Text = "100 %";
            this.btnSpeedScale.Click += new System.EventHandler(this.OnSpeedScaleClick);
            // 
            // lblSpeedScaleCaption
            // 
            this.lblSpeedScaleCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpeedScaleCaption.Location = new System.Drawing.Point(1066, 6);
            this.lblSpeedScaleCaption.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.lblSpeedScaleCaption.Name = "lblSpeedScaleCaption";
            this.lblSpeedScaleCaption.Size = new System.Drawing.Size(170, 38);
            this.lblSpeedScaleCaption.TabIndex = 3;
            this.lblSpeedScaleCaption.Text = "DEFAULT SPEED SCALE %";
            this.lblSpeedScaleCaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpAction
            // 
            this.grpAction.Controls.Add(this.actionsPanel);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.ForeColor = System.Drawing.Color.Black;
            this.grpAction.Location = new System.Drawing.Point(8, 806);
            this.grpAction.Margin = new System.Windows.Forms.Padding(0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Padding = new System.Windows.Forms.Padding(1, 9, 1, 1);
            this.grpAction.Size = new System.Drawing.Size(1662, 86);
            this.grpAction.TabIndex = 3;
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
            // 
            // actionsPanel
            // 
            this.actionsPanel.ColumnCount = 14;
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7.142857F));
            this.actionsPanel.Controls.Add(this.btnHome, 0, 0);
            this.actionsPanel.Controls.Add(this.btnAllStop, 1, 0);
            this.actionsPanel.Controls.Add(this.btnAlarmClear, 2, 0);
            this.actionsPanel.Controls.Add(this.btnAllServoOff, 3, 0);
            this.actionsPanel.Controls.Add(this.btnServoOn, 4, 0);
            this.actionsPanel.Controls.Add(this.btnServoOff, 5, 0);
            this.actionsPanel.Controls.Add(this.btnParaLoad, 6, 0);
            this.actionsPanel.Controls.Add(this.btnParaSave, 7, 0);
            this.actionsPanel.Controls.Add(this.btnBoardScan, 8, 0);
            this.actionsPanel.Controls.Add(this.btnMotionTest, 9, 0);
            this.actionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionsPanel.Location = new System.Drawing.Point(1, 29);
            this.actionsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionsPanel.Name = "actionsPanel";
            this.actionsPanel.RowCount = 1;
            this.actionsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionsPanel.Size = new System.Drawing.Size(1660, 56);
            this.actionsPanel.TabIndex = 5;
            // 
            // btnHome
            // 
            this.btnHome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnHome.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnHome.BadgeText = "ACTION";
            this.btnHome.BorderColor = System.Drawing.Color.Empty;
            this.btnHome.BorderWidth = 0;
            this.btnHome.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHome.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnHome.ForeColor = System.Drawing.Color.White;
            this.btnHome.Location = new System.Drawing.Point(4, 8);
            this.btnHome.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnHome.Name = "btnHome";
            this.btnHome.Size = new System.Drawing.Size(110, 40);
            this.btnHome.TabIndex = 2;
            this.btnHome.Text = "INIT AXIS";
            this.btnHome.Click += new System.EventHandler(this.btnHome_Click);
            // 
            // btnAllStop
            // 
            this.btnAllStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAllStop.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnAllStop.BadgeText = "ACTION";
            this.btnAllStop.BorderColor = System.Drawing.Color.Empty;
            this.btnAllStop.BorderWidth = 0;
            this.btnAllStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAllStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllStop.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllStop.ForeColor = System.Drawing.Color.White;
            this.btnAllStop.Location = new System.Drawing.Point(122, 8);
            this.btnAllStop.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAllStop.Name = "btnAllStop";
            this.btnAllStop.Size = new System.Drawing.Size(110, 40);
            this.btnAllStop.TabIndex = 5;
            this.btnAllStop.Text = "ALL STOP";
            this.btnAllStop.Click += new System.EventHandler(this.btnAllStop_Click);
            // 
            // btnAlarmClear
            // 
            this.btnAlarmClear.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAlarmClear.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnAlarmClear.BadgeText = "ACTION";
            this.btnAlarmClear.BorderColor = System.Drawing.Color.Empty;
            this.btnAlarmClear.BorderWidth = 0;
            this.btnAlarmClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAlarmClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAlarmClear.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnAlarmClear.ForeColor = System.Drawing.Color.White;
            this.btnAlarmClear.Location = new System.Drawing.Point(240, 8);
            this.btnAlarmClear.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAlarmClear.Name = "btnAlarmClear";
            this.btnAlarmClear.Size = new System.Drawing.Size(110, 40);
            this.btnAlarmClear.TabIndex = 4;
            this.btnAlarmClear.Text = "ALARM CLEAR";
            this.btnAlarmClear.Click += new System.EventHandler(this.btnAlarmClear_Click);
            // 
            // btnAllServoOff
            // 
            this.btnAllServoOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAllServoOff.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnAllServoOff.BadgeText = "ACTION";
            this.btnAllServoOff.BorderColor = System.Drawing.Color.Empty;
            this.btnAllServoOff.BorderWidth = 0;
            this.btnAllServoOff.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAllServoOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllServoOff.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllServoOff.ForeColor = System.Drawing.Color.White;
            this.btnAllServoOff.Location = new System.Drawing.Point(358, 8);
            this.btnAllServoOff.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAllServoOff.Name = "btnAllServoOff";
            this.btnAllServoOff.Size = new System.Drawing.Size(110, 40);
            this.btnAllServoOff.TabIndex = 5;
            this.btnAllServoOff.Text = "ALL SERVO OFF";
            this.btnAllServoOff.Click += new System.EventHandler(this.btnAllServoOff_Click);
            // 
            // btnServoOn
            // 
            this.btnServoOn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnServoOn.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnServoOn.BadgeText = "ACTION";
            this.btnServoOn.BorderColor = System.Drawing.Color.Empty;
            this.btnServoOn.BorderWidth = 0;
            this.btnServoOn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnServoOn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnServoOn.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnServoOn.ForeColor = System.Drawing.Color.White;
            this.btnServoOn.Location = new System.Drawing.Point(476, 8);
            this.btnServoOn.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnServoOn.Name = "btnServoOn";
            this.btnServoOn.Size = new System.Drawing.Size(110, 40);
            this.btnServoOn.TabIndex = 6;
            this.btnServoOn.Text = "SERVO ON";
            this.btnServoOn.Click += new System.EventHandler(this.btnServoOn_Click);
            // 
            // btnServoOff
            // 
            this.btnServoOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnServoOff.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnServoOff.BadgeText = "ACTION";
            this.btnServoOff.BorderColor = System.Drawing.Color.Empty;
            this.btnServoOff.BorderWidth = 0;
            this.btnServoOff.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnServoOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnServoOff.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnServoOff.ForeColor = System.Drawing.Color.White;
            this.btnServoOff.Location = new System.Drawing.Point(594, 8);
            this.btnServoOff.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnServoOff.Name = "btnServoOff";
            this.btnServoOff.Size = new System.Drawing.Size(110, 40);
            this.btnServoOff.TabIndex = 7;
            this.btnServoOff.Text = "SERVO OFF";
            this.btnServoOff.Click += new System.EventHandler(this.btnServoOff_Click);
            // 
            // btnParaLoad
            // 
            this.btnParaLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnParaLoad.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnParaLoad.BadgeText = "ACTION";
            this.btnParaLoad.BorderColor = System.Drawing.Color.Empty;
            this.btnParaLoad.BorderWidth = 0;
            this.btnParaLoad.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnParaLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnParaLoad.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnParaLoad.ForeColor = System.Drawing.Color.White;
            this.btnParaLoad.Location = new System.Drawing.Point(712, 8);
            this.btnParaLoad.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnParaLoad.Name = "btnParaLoad";
            this.btnParaLoad.Size = new System.Drawing.Size(110, 40);
            this.btnParaLoad.TabIndex = 8;
            this.btnParaLoad.Text = "PARA LOAD";
            this.btnParaLoad.Click += new System.EventHandler(this.btnParaLoad_Click);
            // 
            // btnParaSave
            // 
            this.btnParaSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnParaSave.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnParaSave.BadgeText = "ACTION";
            this.btnParaSave.BorderColor = System.Drawing.Color.Empty;
            this.btnParaSave.BorderWidth = 0;
            this.btnParaSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnParaSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnParaSave.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnParaSave.ForeColor = System.Drawing.Color.White;
            this.btnParaSave.Location = new System.Drawing.Point(830, 8);
            this.btnParaSave.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnParaSave.Name = "btnParaSave";
            this.btnParaSave.Size = new System.Drawing.Size(110, 40);
            this.btnParaSave.TabIndex = 9;
            this.btnParaSave.Text = "PARA SAVE";
            this.btnParaSave.Click += new System.EventHandler(this.btnParaSave_Click);
            // 
            // btnBoardScan
            // 
            this.btnBoardScan.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBoardScan.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnBoardScan.BadgeText = "ACTION";
            this.btnBoardScan.BorderColor = System.Drawing.Color.Empty;
            this.btnBoardScan.BorderWidth = 0;
            this.btnBoardScan.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBoardScan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBoardScan.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnBoardScan.ForeColor = System.Drawing.Color.White;
            this.btnBoardScan.Location = new System.Drawing.Point(948, 8);
            this.btnBoardScan.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnBoardScan.Name = "btnBoardScan";
            this.btnBoardScan.Size = new System.Drawing.Size(110, 40);
            this.btnBoardScan.TabIndex = 10;
            this.btnBoardScan.Text = "BOARD SCAN";
            this.btnBoardScan.Click += new System.EventHandler(this.btnBoardScan_Click);
            // 
            // btnMotionTest
            // 
            this.btnMotionTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnMotionTest.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnMotionTest.BadgeText = "ACTION";
            this.btnMotionTest.BorderColor = System.Drawing.Color.Empty;
            this.btnMotionTest.BorderWidth = 0;
            this.btnMotionTest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMotionTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMotionTest.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnMotionTest.ForeColor = System.Drawing.Color.White;
            this.btnMotionTest.Location = new System.Drawing.Point(1066, 8);
            this.btnMotionTest.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnMotionTest.Name = "btnMotionTest";
            this.btnMotionTest.Size = new System.Drawing.Size(110, 40);
            this.btnMotionTest.TabIndex = 11;
            this.btnMotionTest.Text = "MOTION TEST";
            this.btnMotionTest.Click += new System.EventHandler(this.btnMotionTest_Click);
            // 
            // MotionPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "MotionPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.Load += new System.EventHandler(this.MotionPage_Load);
            this.rootLayout.ResumeLayout(false);
            this.grpModule.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.grpConfiguration.ResumeLayout(false);
            this.configTabs.ResumeLayout(false);
            this.tabConfig.ResumeLayout(false);
            this.configLayout.ResumeLayout(false);
            this.grpConfig.ResumeLayout(false);
            this.grpInposition.ResumeLayout(false);
            this.grpLimit.ResumeLayout(false);
            this.grpEmergency.ResumeLayout(false);
            this.grpHome.ResumeLayout(false);
            this.grpAlarm.ResumeLayout(false);
            this.grpPositionClear.ResumeLayout(false);
            this.tabSpeed.ResumeLayout(false);
            this.speedLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.speedGrid)).EndInit();
            this.speedButtons.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn11;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn12;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn13;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn14;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn15;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn16;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn17;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn18;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn19;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn20;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn21;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn22;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn23;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn24;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn25;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn26;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn27;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn28;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn29;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn30;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn31;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn32;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn33;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn34;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn35;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn36;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn37;
        private System.Windows.Forms.DataGridViewTextBoxColumn INDEX;
        private System.Windows.Forms.DataGridViewTextBoxColumn MODULE;
        private System.Windows.Forms.DataGridViewTextBoxColumn KEY;
        private System.Windows.Forms.DataGridViewTextBoxColumn NO;
        private System.Windows.Forms.DataGridViewTextBoxColumn BOARD;
        private System.Windows.Forms.DataGridViewTextBoxColumn CH;
        private System.Windows.Forms.DataGridViewTextBoxColumn STATUS;
        private System.Windows.Forms.DataGridViewTextBoxColumn SERVO;
        private System.Windows.Forms.DataGridViewTextBoxColumn COMMAND_POSITION;
        private System.Windows.Forms.DataGridViewTextBoxColumn ACTUAL_POSITION;
        private System.Windows.Forms.DataGridViewTextBoxColumn VELOCITY;
        private System.Windows.Forms.DataGridViewTextBoxColumn DONE;
        private System.Windows.Forms.DataGridViewTextBoxColumn INP_DONE;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOME_END;
        private System.Windows.Forms.DataGridViewTextBoxColumn ALARM;
        private System.Windows.Forms.DataGridViewTextBoxColumn PEL;
        private System.Windows.Forms.DataGridViewTextBoxColumn MEL;
        private System.Windows.Forms.DataGridViewTextBoxColumn ORG;
        private System.Windows.Forms.DataGridViewTextBoxColumn AXIS;
        private System.Windows.Forms.DataGridViewTextBoxColumn DEFAULT_VEL;
        private System.Windows.Forms.DataGridViewTextBoxColumn ACCEL;
        private System.Windows.Forms.DataGridViewTextBoxColumn DECEL;
        private System.Windows.Forms.DataGridViewTextBoxColumn STOP_DEC;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOME_VEL_1;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOME_VEL_2;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOME_VEL_3;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOME_VEL_4;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOME_ACC_1;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOME_DEC_1;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOME_ACC_2;
        private System.Windows.Forms.DataGridViewTextBoxColumn HOME_DEC_2;
        private System.Windows.Forms.DataGridViewTextBoxColumn JOG_COARSE;
        private System.Windows.Forms.DataGridViewTextBoxColumn JOG_FINE;
        private System.Windows.Forms.DataGridViewTextBoxColumn JOG_ACC;
        private System.Windows.Forms.DataGridViewTextBoxColumn JOG_DEC;
        private System.Windows.Forms.DataGridViewTextBoxColumn JOG_STOP_DEC;
        private System.Windows.Forms.DataGridViewTextBoxColumn INPOS_TOL;
    }
}
