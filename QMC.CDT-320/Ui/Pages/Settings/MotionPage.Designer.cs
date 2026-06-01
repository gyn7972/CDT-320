namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class MotionPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblPageHeader;
        private System.Windows.Forms.Label lblModuleHeader;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.Label lblConfigHeader;
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
        private System.Windows.Forms.TableLayoutPanel actionsPanel;
        private QMC.CDT_320.Ui.Controls.ActionButton btnEnable;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDisable;
        private QMC.CDT_320.Ui.Controls.ActionButton btnHome;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAllStop;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAlarmClear;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAllServoOff;
        private QMC.CDT_320.Ui.Controls.ActionButton btnServoOn;
        private QMC.CDT_320.Ui.Controls.ActionButton btnServoOff;
        private QMC.CDT_320.Ui.Controls.ActionButton btnParaLoad;
        private QMC.CDT_320.Ui.Controls.ActionButton btnParaSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnBoardScan;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle gridHeaderStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPageHeader = new System.Windows.Forms.Label();
            this.lblModuleHeader = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.lblConfigHeader = new System.Windows.Forms.Label();
            this.configTabs = new System.Windows.Forms.TabControl();
            this.tabStatus = new System.Windows.Forms.TabPage();
            this.tabConfig = new System.Windows.Forms.TabPage();
            this.tabSpeed = new System.Windows.Forms.TabPage();
            this.configLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpConfig = new System.Windows.Forms.GroupBox();
            this.grpInposition = new System.Windows.Forms.GroupBox();
            this.grpHome = new System.Windows.Forms.GroupBox();
            this.grpLimit = new System.Windows.Forms.GroupBox();
            this.grpEmergency = new System.Windows.Forms.GroupBox();
            this.grpAlarm = new System.Windows.Forms.GroupBox();
            this.grpPositionClear = new System.Windows.Forms.GroupBox();
            this.pgConfig = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.pgInposition = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.pgHome = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.pgLimit = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.pgEmergency = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.pgAlarm = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.pgPositionClear = new QMC.CDT_320.Ui.Controls.ParamGrid();
            this.speedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.speedGrid = new System.Windows.Forms.DataGridView();
            this.speedButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSpeedReload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSpeedSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.actionsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnEnable = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDisable = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnHome = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAllStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAlarmClear = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAllServoOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnServoOn = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnServoOff = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnParaLoad = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnParaSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnBoardScan = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.configTabs.SuspendLayout();
            this.tabConfig.SuspendLayout();
            this.configLayout.SuspendLayout();
            this.tabSpeed.SuspendLayout();
            this.speedLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.speedGrid)).BeginInit();
            this.speedButtons.SuspendLayout();
            this.actionsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblPageHeader, 0, 0);
            this.rootLayout.Controls.Add(this.lblModuleHeader, 0, 1);
            this.rootLayout.Controls.Add(this.grid, 0, 2);
            this.rootLayout.Controls.Add(this.lblConfigHeader, 0, 3);
            this.rootLayout.Controls.Add(this.configTabs, 0, 4);
            this.rootLayout.Controls.Add(this.actionsPanel, 0, 5);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 6;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblPageHeader
            // 
            this.lblPageHeader.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.lblPageHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblPageHeader.ForeColor = System.Drawing.Color.White;
            this.lblPageHeader.Location = new System.Drawing.Point(8, 8);
            this.lblPageHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblPageHeader.Name = "lblPageHeader";
            this.lblPageHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblPageHeader.Size = new System.Drawing.Size(1400, 30);
            this.lblPageHeader.TabIndex = 0;
            this.lblPageHeader.Text = "MOTION";
            this.lblPageHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblModuleHeader
            // 
            this.lblModuleHeader.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.lblModuleHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblModuleHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblModuleHeader.ForeColor = System.Drawing.Color.White;
            this.lblModuleHeader.Location = new System.Drawing.Point(8, 38);
            this.lblModuleHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblModuleHeader.Name = "lblModuleHeader";
            this.lblModuleHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblModuleHeader.Size = new System.Drawing.Size(1400, 26);
            this.lblModuleHeader.TabIndex = 1;
            this.lblModuleHeader.Text = "MODULE LIST";
            this.lblModuleHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            gridHeaderStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            gridHeaderStyle.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
            gridHeaderStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            gridHeaderStyle.ForeColor = System.Drawing.Color.White;
            gridHeaderStyle.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            gridHeaderStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            gridHeaderStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.grid.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            this.grid.ColumnHeadersHeight = 29;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.grid.Location = new System.Drawing.Point(8, 68);
            this.grid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.RowHeadersWidth = 51;
            this.grid.RowTemplate.Height = 26;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1400, 292);
            this.grid.TabIndex = 2;
            this.grid.Columns.Add("INDEX", "INDEX");
            this.grid.Columns.Add("MODULE", "MODULE");
            this.grid.Columns.Add("KEY", "KEY");
            this.grid.Columns.Add("NO", "NO.");
            this.grid.Columns.Add("BOARD", "BOARD");
            this.grid.Columns.Add("CH", "CH");
            this.grid.Columns.Add("STATUS", "STATUS");
            this.grid.Columns.Add("SERVO", "SERVO");
            this.grid.Columns.Add("COMMAND_POSITION", "COMMAND POSITION");
            this.grid.Columns.Add("ACTUAL_POSITION", "ACTUAL POSITION");
            this.grid.Columns.Add("VELOCITY", "VELOCITY");
            this.grid.Columns.Add("DONE", "DONE");
            this.grid.Columns.Add("INP_DONE", "INP DONE");
            this.grid.Columns.Add("HOME_END", "HOME END");
            this.grid.Columns.Add("ALARM", "ALARM");
            this.grid.Columns.Add("PEL", "PEL");
            this.grid.Columns.Add("MEL", "MEL");
            this.grid.Columns.Add("ORG", "ORG");
            // 
            // lblConfigHeader
            // 
            this.lblConfigHeader.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.lblConfigHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblConfigHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblConfigHeader.ForeColor = System.Drawing.Color.White;
            this.lblConfigHeader.Location = new System.Drawing.Point(8, 368);
            this.lblConfigHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblConfigHeader.Name = "lblConfigHeader";
            this.lblConfigHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblConfigHeader.Size = new System.Drawing.Size(1400, 26);
            this.lblConfigHeader.TabIndex = 3;
            this.lblConfigHeader.Text = "CONFIGURATION";
            this.lblConfigHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // configTabs
            // 
            this.configTabs.Alignment = System.Windows.Forms.TabAlignment.Left;
            this.configTabs.Controls.Add(this.tabStatus);
            this.configTabs.Controls.Add(this.tabConfig);
            this.configTabs.Controls.Add(this.tabSpeed);
            this.configTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configTabs.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.configTabs.ItemSize = new System.Drawing.Size(100, 32);
            this.configTabs.Location = new System.Drawing.Point(8, 398);
            this.configTabs.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.configTabs.Multiline = true;
            this.configTabs.Name = "configTabs";
            this.configTabs.SelectedIndex = 0;
            this.configTabs.Size = new System.Drawing.Size(1400, 506);
            this.configTabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.configTabs.TabIndex = 4;
            // 
            // tabStatus
            // 
            this.tabStatus.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabStatus.Location = new System.Drawing.Point(36, 4);
            this.tabStatus.Name = "tabStatus";
            this.tabStatus.Padding = new System.Windows.Forms.Padding(3);
            this.tabStatus.Size = new System.Drawing.Size(1360, 498);
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
            this.tabConfig.Size = new System.Drawing.Size(1360, 498);
            this.tabConfig.TabIndex = 1;
            this.tabConfig.Text = "CONFIG";
            // 
            // tabSpeed
            // 
            this.tabSpeed.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabSpeed.Controls.Add(this.speedLayout);
            this.tabSpeed.Location = new System.Drawing.Point(36, 4);
            this.tabSpeed.Name = "tabSpeed";
            this.tabSpeed.Padding = new System.Windows.Forms.Padding(8);
            this.tabSpeed.Size = new System.Drawing.Size(1360, 498);
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
            this.speedLayout.Size = new System.Drawing.Size(1344, 482);
            this.speedLayout.TabIndex = 0;
            // 
            // speedGrid
            // 
            this.speedGrid.AllowUserToAddRows = false;
            this.speedGrid.AllowUserToDeleteRows = false;
            this.speedGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.speedGrid.BackgroundColor = System.Drawing.Color.White;
            this.speedGrid.ColumnHeadersDefaultCellStyle = gridHeaderStyle;
            this.speedGrid.ColumnHeadersHeight = 29;
            this.speedGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedGrid.EnableHeadersVisualStyles = false;
            this.speedGrid.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.speedGrid.Location = new System.Drawing.Point(0, 0);
            this.speedGrid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.speedGrid.MultiSelect = false;
            this.speedGrid.Name = "speedGrid";
            this.speedGrid.RowHeadersVisible = false;
            this.speedGrid.RowHeadersWidth = 51;
            this.speedGrid.RowTemplate.Height = 26;
            this.speedGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.speedGrid.Size = new System.Drawing.Size(1344, 426);
            this.speedGrid.TabIndex = 0;
            this.speedGrid.Columns.Add("AXIS", "AXIS");
            this.speedGrid.Columns.Add("DEFAULT_VEL", "DEFAULT VEL");
            this.speedGrid.Columns.Add("ACCEL", "ACCEL");
            this.speedGrid.Columns.Add("DECEL", "DECEL");
            this.speedGrid.Columns.Add("HOME_VEL_1", "HOME VEL 1");
            this.speedGrid.Columns.Add("HOME_VEL_2", "HOME VEL 2");
            this.speedGrid.Columns.Add("HOME_VEL_3", "HOME VEL 3");
            this.speedGrid.Columns.Add("HOME_VEL_4", "HOME VEL 4");
            this.speedGrid.Columns.Add("HOME_ACC_1", "HOME ACC 1");
            this.speedGrid.Columns.Add("HOME_DEC_1", "HOME DEC 1");
            this.speedGrid.Columns.Add("HOME_ACC_2", "HOME ACC 2");
            this.speedGrid.Columns.Add("HOME_DEC_2", "HOME DEC 2");
            this.speedGrid.Columns.Add("JOG_COARSE", "JOG COARSE");
            this.speedGrid.Columns.Add("JOG_FINE", "JOG FINE");
            this.speedGrid.Columns.Add("JOG_ACC", "JOG ACC");
            this.speedGrid.Columns.Add("JOG_DEC", "JOG DEC");
            this.speedGrid.Columns.Add("INPOS_TOL", "IN-POS TOL");
            this.speedGrid.Columns[0].ReadOnly = true;
            // 
            // speedButtons
            // 
            this.speedButtons.Controls.Add(this.btnSpeedReload);
            this.speedButtons.Controls.Add(this.btnSpeedSave);
            this.speedButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.speedButtons.Location = new System.Drawing.Point(0, 432);
            this.speedButtons.Margin = new System.Windows.Forms.Padding(0);
            this.speedButtons.Name = "speedButtons";
            this.speedButtons.Size = new System.Drawing.Size(1344, 50);
            this.speedButtons.TabIndex = 1;
            // 
            // btnSpeedSave
            // 
            this.btnSpeedSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSpeedSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSpeedSave.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnSpeedSave.Name = "btnSpeedSave";
            this.btnSpeedSave.Size = new System.Drawing.Size(120, 38);
            this.btnSpeedSave.TabIndex = 0;
            this.btnSpeedSave.Text = "SAVE";
            // 
            // btnSpeedReload
            // 
            this.btnSpeedReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSpeedReload.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSpeedReload.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnSpeedReload.Name = "btnSpeedReload";
            this.btnSpeedReload.Size = new System.Drawing.Size(120, 38);
            this.btnSpeedReload.TabIndex = 1;
            this.btnSpeedReload.Text = "RELOAD";
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
            this.configLayout.Size = new System.Drawing.Size(1344, 482);
            this.configLayout.TabIndex = 0;
            // 
            // grpConfig
            // 
            this.grpConfig.Controls.Add(this.pgConfig);
            this.grpConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpConfig.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpConfig.Location = new System.Drawing.Point(3, 3);
            this.grpConfig.Name = "grpConfig";
            this.grpConfig.Size = new System.Drawing.Size(442, 210);
            this.grpConfig.TabIndex = 0;
            this.grpConfig.TabStop = false;
            this.grpConfig.Text = "CONFIG";
            // 
            // pgConfig
            // 
            this.pgConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgConfig.Location = new System.Drawing.Point(3, 19);
            this.pgConfig.Name = "pgConfig";
            this.pgConfig.NameWidth = 140;
            this.pgConfig.Padding = new System.Windows.Forms.Padding(6);
            this.pgConfig.Size = new System.Drawing.Size(436, 188);
            this.pgConfig.TabIndex = 0;
            // 
            // grpInposition
            // 
            this.grpInposition.Controls.Add(this.pgInposition);
            this.grpInposition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInposition.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpInposition.Location = new System.Drawing.Point(451, 3);
            this.grpInposition.Name = "grpInposition";
            this.grpInposition.Size = new System.Drawing.Size(442, 210);
            this.grpInposition.TabIndex = 1;
            this.grpInposition.TabStop = false;
            this.grpInposition.Text = "INPOSITION";
            // 
            // pgInposition
            // 
            this.pgInposition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgInposition.Location = new System.Drawing.Point(3, 19);
            this.pgInposition.Name = "pgInposition";
            this.pgInposition.NameWidth = 140;
            this.pgInposition.Padding = new System.Windows.Forms.Padding(6);
            this.pgInposition.Size = new System.Drawing.Size(436, 188);
            this.pgInposition.TabIndex = 0;
            // 
            // grpLimit
            // 
            this.grpLimit.Controls.Add(this.pgLimit);
            this.grpLimit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLimit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpLimit.Location = new System.Drawing.Point(899, 3);
            this.grpLimit.Name = "grpLimit";
            this.grpLimit.Size = new System.Drawing.Size(442, 210);
            this.grpLimit.TabIndex = 2;
            this.grpLimit.TabStop = false;
            this.grpLimit.Text = "LIMIT";
            // 
            // pgLimit
            // 
            this.pgLimit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgLimit.Location = new System.Drawing.Point(3, 19);
            this.pgLimit.Name = "pgLimit";
            this.pgLimit.NameWidth = 140;
            this.pgLimit.Padding = new System.Windows.Forms.Padding(6);
            this.pgLimit.Size = new System.Drawing.Size(436, 188);
            this.pgLimit.TabIndex = 0;
            // 
            // grpEmergency
            // 
            this.grpEmergency.Controls.Add(this.pgEmergency);
            this.grpEmergency.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpEmergency.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpEmergency.Location = new System.Drawing.Point(3, 219);
            this.grpEmergency.Name = "grpEmergency";
            this.grpEmergency.Size = new System.Drawing.Size(442, 126);
            this.grpEmergency.TabIndex = 3;
            this.grpEmergency.TabStop = false;
            this.grpEmergency.Text = "EMERGENCY SIGNAL";
            // 
            // pgEmergency
            // 
            this.pgEmergency.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgEmergency.Location = new System.Drawing.Point(3, 19);
            this.pgEmergency.Name = "pgEmergency";
            this.pgEmergency.NameWidth = 140;
            this.pgEmergency.Padding = new System.Windows.Forms.Padding(6);
            this.pgEmergency.Size = new System.Drawing.Size(436, 104);
            this.pgEmergency.TabIndex = 0;
            // 
            // grpHome
            // 
            this.grpHome.Controls.Add(this.pgHome);
            this.grpHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpHome.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpHome.Location = new System.Drawing.Point(451, 219);
            this.grpHome.Name = "grpHome";
            this.grpHome.Size = new System.Drawing.Size(442, 126);
            this.grpHome.TabIndex = 4;
            this.grpHome.TabStop = false;
            this.grpHome.Text = "HOME";
            // 
            // pgHome
            // 
            this.pgHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgHome.Location = new System.Drawing.Point(3, 19);
            this.pgHome.Name = "pgHome";
            this.pgHome.NameWidth = 140;
            this.pgHome.Padding = new System.Windows.Forms.Padding(6);
            this.pgHome.Size = new System.Drawing.Size(436, 104);
            this.pgHome.TabIndex = 0;
            // 
            // grpAlarm
            // 
            this.grpAlarm.Controls.Add(this.pgAlarm);
            this.grpAlarm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAlarm.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpAlarm.Location = new System.Drawing.Point(899, 219);
            this.grpAlarm.Name = "grpAlarm";
            this.grpAlarm.Size = new System.Drawing.Size(442, 126);
            this.grpAlarm.TabIndex = 5;
            this.grpAlarm.TabStop = false;
            this.grpAlarm.Text = "ALARM";
            // 
            // pgAlarm
            // 
            this.pgAlarm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgAlarm.Location = new System.Drawing.Point(3, 19);
            this.pgAlarm.Name = "pgAlarm";
            this.pgAlarm.NameWidth = 140;
            this.pgAlarm.Padding = new System.Windows.Forms.Padding(6);
            this.pgAlarm.Size = new System.Drawing.Size(436, 104);
            this.pgAlarm.TabIndex = 0;
            // 
            // grpPositionClear
            // 
            this.grpPositionClear.Controls.Add(this.pgPositionClear);
            this.grpPositionClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPositionClear.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpPositionClear.Location = new System.Drawing.Point(3, 351);
            this.grpPositionClear.Name = "grpPositionClear";
            this.grpPositionClear.Size = new System.Drawing.Size(442, 128);
            this.grpPositionClear.TabIndex = 6;
            this.grpPositionClear.TabStop = false;
            this.grpPositionClear.Text = "POSITION CLEAR";
            // 
            // pgPositionClear
            // 
            this.pgPositionClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgPositionClear.Location = new System.Drawing.Point(3, 19);
            this.pgPositionClear.Name = "pgPositionClear";
            this.pgPositionClear.NameWidth = 140;
            this.pgPositionClear.Padding = new System.Windows.Forms.Padding(6);
            this.pgPositionClear.Size = new System.Drawing.Size(436, 106);
            this.pgPositionClear.TabIndex = 0;
            // 
            // actionsPanel
            // 
            this.actionsPanel.ColumnCount = 11;
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.actionsPanel.Controls.Add(this.btnEnable, 0, 0);
            this.actionsPanel.Controls.Add(this.btnDisable, 1, 0);
            this.actionsPanel.Controls.Add(this.btnHome, 2, 0);
            this.actionsPanel.Controls.Add(this.btnAllStop, 3, 0);
            this.actionsPanel.Controls.Add(this.btnAlarmClear, 4, 0);
            this.actionsPanel.Controls.Add(this.btnAllServoOff, 5, 0);
            this.actionsPanel.Controls.Add(this.btnServoOn, 6, 0);
            this.actionsPanel.Controls.Add(this.btnServoOff, 7, 0);
            this.actionsPanel.Controls.Add(this.btnParaLoad, 8, 0);
            this.actionsPanel.Controls.Add(this.btnParaSave, 9, 0);
            this.actionsPanel.Controls.Add(this.btnBoardScan, 10, 0);
            this.actionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionsPanel.Location = new System.Drawing.Point(8, 912);
            this.actionsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionsPanel.Name = "actionsPanel";
            this.actionsPanel.RowCount = 1;
            this.actionsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionsPanel.Size = new System.Drawing.Size(1400, 60);
            this.actionsPanel.TabIndex = 5;
            // 
            // btnEnable
            // 
            this.btnEnable.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEnable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnEnable.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnEnable.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.Size = new System.Drawing.Size(104, 44);
            this.btnEnable.TabIndex = 0;
            this.btnEnable.Text = "ENABLE";
            // 
            // btnDisable
            // 
            this.btnDisable.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDisable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDisable.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnDisable.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnDisable.Name = "btnDisable";
            this.btnDisable.Size = new System.Drawing.Size(104, 44);
            this.btnDisable.TabIndex = 1;
            this.btnDisable.Text = "DISABLE";
            // 
            // btnHome
            // 
            this.btnHome.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHome.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnHome.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnHome.Name = "btnHome";
            this.btnHome.Size = new System.Drawing.Size(104, 44);
            this.btnHome.TabIndex = 2;
            this.btnHome.Text = "HOME";
            // 
            // btnAllStop
            // 
            this.btnAllStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAllStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllStop.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllStop.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAllStop.Name = "btnAllStop";
            this.btnAllStop.Size = new System.Drawing.Size(104, 44);
            this.btnAllStop.TabIndex = 3;
            this.btnAllStop.Text = "ALL STOP";
            // 
            // btnAlarmClear
            // 
            this.btnAlarmClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAlarmClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAlarmClear.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAlarmClear.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAlarmClear.Name = "btnAlarmClear";
            this.btnAlarmClear.Size = new System.Drawing.Size(132, 44);
            this.btnAlarmClear.TabIndex = 4;
            this.btnAlarmClear.Text = "ALARM CLEAR";
            // 
            // btnAllServoOff
            // 
            this.btnAllServoOff.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAllServoOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllServoOff.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllServoOff.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAllServoOff.Name = "btnAllServoOff";
            this.btnAllServoOff.Size = new System.Drawing.Size(160, 44);
            this.btnAllServoOff.TabIndex = 5;
            this.btnAllServoOff.Text = "ALL SERVO OFF";
            // 
            // btnServoOn
            // 
            this.btnServoOn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnServoOn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnServoOn.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnServoOn.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnServoOn.Name = "btnServoOn";
            this.btnServoOn.Size = new System.Drawing.Size(118, 44);
            this.btnServoOn.TabIndex = 6;
            this.btnServoOn.Text = "SERVO ON";
            // 
            // btnServoOff
            // 
            this.btnServoOff.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnServoOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnServoOff.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnServoOff.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnServoOff.Name = "btnServoOff";
            this.btnServoOff.Size = new System.Drawing.Size(118, 44);
            this.btnServoOff.TabIndex = 7;
            this.btnServoOff.Text = "SERVO OFF";
            // 
            // btnParaLoad
            // 
            this.btnParaLoad.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnParaLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnParaLoad.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnParaLoad.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnParaLoad.Name = "btnParaLoad";
            this.btnParaLoad.Size = new System.Drawing.Size(118, 44);
            this.btnParaLoad.TabIndex = 8;
            this.btnParaLoad.Text = "PARA LOAD";
            // 
            // btnParaSave
            // 
            this.btnParaSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnParaSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnParaSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnParaSave.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnParaSave.Name = "btnParaSave";
            this.btnParaSave.Size = new System.Drawing.Size(118, 44);
            this.btnParaSave.TabIndex = 9;
            this.btnParaSave.Text = "PARA SAVE";
            // 
            // btnBoardScan
            // 
            this.btnBoardScan.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBoardScan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBoardScan.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnBoardScan.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnBoardScan.Name = "btnBoardScan";
            this.btnBoardScan.Size = new System.Drawing.Size(132, 44);
            this.btnBoardScan.TabIndex = 10;
            this.btnBoardScan.Text = "BOARD SCAN";
            // 
            // MotionPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "MotionPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.configTabs.ResumeLayout(false);
            this.tabConfig.ResumeLayout(false);
            this.configLayout.ResumeLayout(false);
            this.tabSpeed.ResumeLayout(false);
            this.speedLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.speedGrid)).EndInit();
            this.speedButtons.ResumeLayout(false);
            this.actionsPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
