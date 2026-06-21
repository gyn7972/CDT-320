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
        private System.Windows.Forms.Label lblSpeedScaleCaption;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSpeedScale;
        private System.Windows.Forms.TableLayoutPanel actionsPanel;
        private QMC.CDT_320.Ui.Controls.ActionButton btnEnable;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDisable;
        private QMC.CDT_320.Ui.Controls.ActionButton btnHome;
        private QMC.CDT_320.Ui.Controls.ActionButton btnGroupHome;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAllHome;
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPageHeader = new System.Windows.Forms.Label();
            this.lblModuleHeader = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn11 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn12 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn13 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn14 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn15 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn16 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn17 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn18 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblConfigHeader = new System.Windows.Forms.Label();
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
            this.dataGridViewTextBoxColumn19 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn20 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn21 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn22 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn23 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn24 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn25 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn26 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn27 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn28 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn29 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn30 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn31 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn32 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn33 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn34 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn35 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn36 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn37 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.speedButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSpeedReload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSpeedSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSpeedScale = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.lblSpeedScaleCaption = new System.Windows.Forms.Label();
            this.actionsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnEnable = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDisable = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnHome = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnGroupHome = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAllHome = new QMC.CDT_320.Ui.Controls.ActionButton();
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
            this.lblPageHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblPageHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
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
            // lblModuleHeader
            // 
            this.lblModuleHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblModuleHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblModuleHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblModuleHeader.ForeColor = System.Drawing.Color.White;
            this.lblModuleHeader.Location = new System.Drawing.Point(8, 38);
            this.lblModuleHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblModuleHeader.Name = "lblModuleHeader";
            this.lblModuleHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblModuleHeader.Size = new System.Drawing.Size(1662, 26);
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
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewTextBoxColumn5,
            this.dataGridViewTextBoxColumn6,
            this.dataGridViewTextBoxColumn7,
            this.dataGridViewTextBoxColumn8,
            this.dataGridViewTextBoxColumn9,
            this.dataGridViewTextBoxColumn10,
            this.dataGridViewTextBoxColumn11,
            this.dataGridViewTextBoxColumn12,
            this.dataGridViewTextBoxColumn13,
            this.dataGridViewTextBoxColumn14,
            this.dataGridViewTextBoxColumn15,
            this.dataGridViewTextBoxColumn16,
            this.dataGridViewTextBoxColumn17,
            this.dataGridViewTextBoxColumn18});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.grid.Location = new System.Drawing.Point(8, 68);
            this.grid.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.RowHeadersWidth = 51;
            this.grid.RowTemplate.Height = 26;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(1662, 292);
            this.grid.TabIndex = 2;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "INDEX";
            this.dataGridViewTextBoxColumn1.Name = "INDEX";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "MODULE";
            this.dataGridViewTextBoxColumn2.Name = "MODULE";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "KEY";
            this.dataGridViewTextBoxColumn3.Name = "KEY";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.HeaderText = "NO.";
            this.dataGridViewTextBoxColumn4.Name = "NO";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.HeaderText = "BOARD";
            this.dataGridViewTextBoxColumn5.Name = "BOARD";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.HeaderText = "CH";
            this.dataGridViewTextBoxColumn6.Name = "CH";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.HeaderText = "STATUS";
            this.dataGridViewTextBoxColumn7.Name = "STATUS";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.HeaderText = "SERVO";
            this.dataGridViewTextBoxColumn8.Name = "SERVO";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.HeaderText = "COMMAND POSITION";
            this.dataGridViewTextBoxColumn9.Name = "COMMAND_POSITION";
            this.dataGridViewTextBoxColumn9.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.HeaderText = "ACTUAL POSITION";
            this.dataGridViewTextBoxColumn10.Name = "ACTUAL_POSITION";
            this.dataGridViewTextBoxColumn10.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn11
            // 
            this.dataGridViewTextBoxColumn11.HeaderText = "VELOCITY";
            this.dataGridViewTextBoxColumn11.Name = "VELOCITY";
            this.dataGridViewTextBoxColumn11.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn12
            // 
            this.dataGridViewTextBoxColumn12.HeaderText = "DONE";
            this.dataGridViewTextBoxColumn12.Name = "DONE";
            this.dataGridViewTextBoxColumn12.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn13
            // 
            this.dataGridViewTextBoxColumn13.HeaderText = "INP DONE";
            this.dataGridViewTextBoxColumn13.Name = "INP_DONE";
            this.dataGridViewTextBoxColumn13.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn14
            // 
            this.dataGridViewTextBoxColumn14.HeaderText = "HOME END";
            this.dataGridViewTextBoxColumn14.Name = "HOME_END";
            this.dataGridViewTextBoxColumn14.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn15
            // 
            this.dataGridViewTextBoxColumn15.HeaderText = "ALARM";
            this.dataGridViewTextBoxColumn15.Name = "ALARM";
            this.dataGridViewTextBoxColumn15.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn16
            // 
            this.dataGridViewTextBoxColumn16.HeaderText = "PEL";
            this.dataGridViewTextBoxColumn16.Name = "PEL";
            this.dataGridViewTextBoxColumn16.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn17
            // 
            this.dataGridViewTextBoxColumn17.HeaderText = "MEL";
            this.dataGridViewTextBoxColumn17.Name = "MEL";
            this.dataGridViewTextBoxColumn17.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn18
            // 
            this.dataGridViewTextBoxColumn18.HeaderText = "ORG";
            this.dataGridViewTextBoxColumn18.Name = "ORG";
            this.dataGridViewTextBoxColumn18.ReadOnly = true;
            // 
            // lblConfigHeader
            // 
            this.lblConfigHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblConfigHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblConfigHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblConfigHeader.ForeColor = System.Drawing.Color.White;
            this.lblConfigHeader.Location = new System.Drawing.Point(8, 368);
            this.lblConfigHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblConfigHeader.Name = "lblConfigHeader";
            this.lblConfigHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblConfigHeader.Size = new System.Drawing.Size(1662, 26);
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
            this.configTabs.Size = new System.Drawing.Size(1662, 426);
            this.configTabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.configTabs.TabIndex = 4;
            // 
            // tabStatus
            // 
            this.tabStatus.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tabStatus.Location = new System.Drawing.Point(36, 4);
            this.tabStatus.Name = "tabStatus";
            this.tabStatus.Padding = new System.Windows.Forms.Padding(3);
            this.tabStatus.Size = new System.Drawing.Size(1622, 418);
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
            this.grpConfig.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpConfig.Location = new System.Drawing.Point(3, 3);
            this.grpConfig.Name = "grpConfig";
            this.grpConfig.Size = new System.Drawing.Size(442, 184);
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
            this.pgConfig.Size = new System.Drawing.Size(436, 162);
            this.pgConfig.TabIndex = 0;
            this.pgConfig.ValueColor = System.Drawing.Color.Black;
            // 
            // grpInposition
            // 
            this.grpInposition.Controls.Add(this.pgInposition);
            this.grpInposition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInposition.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpInposition.Location = new System.Drawing.Point(451, 3);
            this.grpInposition.Name = "grpInposition";
            this.grpInposition.Size = new System.Drawing.Size(442, 184);
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
            this.pgInposition.Size = new System.Drawing.Size(436, 162);
            this.pgInposition.TabIndex = 0;
            this.pgInposition.ValueColor = System.Drawing.Color.Black;
            // 
            // grpLimit
            // 
            this.grpLimit.Controls.Add(this.pgLimit);
            this.grpLimit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLimit.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpLimit.Location = new System.Drawing.Point(899, 3);
            this.grpLimit.Name = "grpLimit";
            this.grpLimit.Size = new System.Drawing.Size(442, 184);
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
            this.pgLimit.Size = new System.Drawing.Size(436, 162);
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
            this.grpEmergency.Size = new System.Drawing.Size(442, 134);
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
            this.pgEmergency.Size = new System.Drawing.Size(436, 112);
            this.pgEmergency.TabIndex = 0;
            this.pgEmergency.ValueColor = System.Drawing.Color.Black;
            // 
            // grpHome
            // 
            this.grpHome.Controls.Add(this.pgHome);
            this.grpHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpHome.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpHome.Location = new System.Drawing.Point(451, 193);
            this.grpHome.Name = "grpHome";
            this.grpHome.Size = new System.Drawing.Size(442, 134);
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
            this.pgHome.Size = new System.Drawing.Size(436, 112);
            this.pgHome.TabIndex = 0;
            this.pgHome.ValueColor = System.Drawing.Color.Black;
            // 
            // grpAlarm
            // 
            this.grpAlarm.Controls.Add(this.pgAlarm);
            this.grpAlarm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAlarm.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpAlarm.Location = new System.Drawing.Point(899, 193);
            this.grpAlarm.Name = "grpAlarm";
            this.grpAlarm.Size = new System.Drawing.Size(442, 134);
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
            this.pgAlarm.Size = new System.Drawing.Size(436, 112);
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
            this.grpPositionClear.Size = new System.Drawing.Size(442, 146);
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
            this.pgPositionClear.Size = new System.Drawing.Size(436, 124);
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
            this.tabSpeed.Size = new System.Drawing.Size(1622, 418);
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
            this.speedLayout.Size = new System.Drawing.Size(1606, 402);
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
            this.dataGridViewTextBoxColumn19,
            this.dataGridViewTextBoxColumn20,
            this.dataGridViewTextBoxColumn21,
            this.dataGridViewTextBoxColumn22,
            this.dataGridViewTextBoxColumn23,
            this.dataGridViewTextBoxColumn24,
            this.dataGridViewTextBoxColumn25,
            this.dataGridViewTextBoxColumn26,
            this.dataGridViewTextBoxColumn27,
            this.dataGridViewTextBoxColumn28,
            this.dataGridViewTextBoxColumn29,
            this.dataGridViewTextBoxColumn30,
            this.dataGridViewTextBoxColumn31,
            this.dataGridViewTextBoxColumn32,
            this.dataGridViewTextBoxColumn33,
            this.dataGridViewTextBoxColumn34,
            this.dataGridViewTextBoxColumn35,
            this.dataGridViewTextBoxColumn36,
            this.dataGridViewTextBoxColumn37});
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
            this.speedGrid.Size = new System.Drawing.Size(1606, 346);
            this.speedGrid.TabIndex = 0;
            // 
            // dataGridViewTextBoxColumn19
            // 
            this.dataGridViewTextBoxColumn19.HeaderText = "AXIS";
            this.dataGridViewTextBoxColumn19.Name = "AXIS";
            // 
            // dataGridViewTextBoxColumn20
            // 
            this.dataGridViewTextBoxColumn20.HeaderText = "DEFAULT VEL";
            this.dataGridViewTextBoxColumn20.Name = "DEFAULT_VEL";
            // 
            // dataGridViewTextBoxColumn21
            // 
            this.dataGridViewTextBoxColumn21.HeaderText = "ACCEL";
            this.dataGridViewTextBoxColumn21.Name = "ACCEL";
            // 
            // dataGridViewTextBoxColumn22
            // 
            this.dataGridViewTextBoxColumn22.HeaderText = "DECEL";
            this.dataGridViewTextBoxColumn22.Name = "DECEL";
            // 
            // dataGridViewTextBoxColumn23
            // 
            this.dataGridViewTextBoxColumn23.HeaderText = "STOP DEC";
            this.dataGridViewTextBoxColumn23.Name = "STOP_DEC";
            // 
            // dataGridViewTextBoxColumn24
            // 
            this.dataGridViewTextBoxColumn24.HeaderText = "HOME VEL 1";
            this.dataGridViewTextBoxColumn24.Name = "HOME_VEL_1";
            // 
            // dataGridViewTextBoxColumn25
            // 
            this.dataGridViewTextBoxColumn25.HeaderText = "HOME VEL 2";
            this.dataGridViewTextBoxColumn25.Name = "HOME_VEL_2";
            // 
            // dataGridViewTextBoxColumn26
            // 
            this.dataGridViewTextBoxColumn26.HeaderText = "HOME VEL 3";
            this.dataGridViewTextBoxColumn26.Name = "HOME_VEL_3";
            // 
            // dataGridViewTextBoxColumn27
            // 
            this.dataGridViewTextBoxColumn27.HeaderText = "HOME VEL 4";
            this.dataGridViewTextBoxColumn27.Name = "HOME_VEL_4";
            // 
            // dataGridViewTextBoxColumn28
            // 
            this.dataGridViewTextBoxColumn28.HeaderText = "HOME ACC 1";
            this.dataGridViewTextBoxColumn28.Name = "HOME_ACC_1";
            // 
            // dataGridViewTextBoxColumn29
            // 
            this.dataGridViewTextBoxColumn29.HeaderText = "HOME DEC 1";
            this.dataGridViewTextBoxColumn29.Name = "HOME_DEC_1";
            // 
            // dataGridViewTextBoxColumn30
            // 
            this.dataGridViewTextBoxColumn30.HeaderText = "HOME ACC 2";
            this.dataGridViewTextBoxColumn30.Name = "HOME_ACC_2";
            // 
            // dataGridViewTextBoxColumn31
            // 
            this.dataGridViewTextBoxColumn31.HeaderText = "HOME DEC 2";
            this.dataGridViewTextBoxColumn31.Name = "HOME_DEC_2";
            // 
            // dataGridViewTextBoxColumn32
            // 
            this.dataGridViewTextBoxColumn32.HeaderText = "JOG COARSE";
            this.dataGridViewTextBoxColumn32.Name = "JOG_COARSE";
            // 
            // dataGridViewTextBoxColumn33
            // 
            this.dataGridViewTextBoxColumn33.HeaderText = "JOG FINE";
            this.dataGridViewTextBoxColumn33.Name = "JOG_FINE";
            // 
            // dataGridViewTextBoxColumn34
            // 
            this.dataGridViewTextBoxColumn34.HeaderText = "JOG ACC";
            this.dataGridViewTextBoxColumn34.Name = "JOG_ACC";
            // 
            // dataGridViewTextBoxColumn35
            // 
            this.dataGridViewTextBoxColumn35.HeaderText = "JOG DEC";
            this.dataGridViewTextBoxColumn35.Name = "JOG_DEC";
            // 
            // dataGridViewTextBoxColumn36
            // 
            this.dataGridViewTextBoxColumn36.HeaderText = "JOG STOP DEC";
            this.dataGridViewTextBoxColumn36.Name = "JOG_STOP_DEC";
            // 
            // dataGridViewTextBoxColumn37
            // 
            this.dataGridViewTextBoxColumn37.HeaderText = "IN-POS TOL";
            this.dataGridViewTextBoxColumn37.Name = "INPOS_TOL";
            // 
            // speedButtons
            // 
            this.speedButtons.Controls.Add(this.btnSpeedReload);
            this.speedButtons.Controls.Add(this.btnSpeedSave);
            this.speedButtons.Controls.Add(this.btnSpeedScale);
            this.speedButtons.Controls.Add(this.lblSpeedScaleCaption);
            this.speedButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.speedButtons.Location = new System.Drawing.Point(0, 352);
            this.speedButtons.Margin = new System.Windows.Forms.Padding(0);
            this.speedButtons.Name = "speedButtons";
            this.speedButtons.Size = new System.Drawing.Size(1606, 50);
            this.speedButtons.TabIndex = 1;
            // 
            // btnSpeedReload
            // 
            this.btnSpeedReload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSpeedReload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSpeedReload.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSpeedReload.ForeColor = System.Drawing.Color.White;
            this.btnSpeedReload.Location = new System.Drawing.Point(1486, 6);
            this.btnSpeedReload.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnSpeedReload.Name = "btnSpeedReload";
            this.btnSpeedReload.Size = new System.Drawing.Size(120, 38);
            this.btnSpeedReload.TabIndex = 1;
            this.btnSpeedReload.Text = "RELOAD";
            // 
            // btnSpeedSave
            // 
            this.btnSpeedSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSpeedSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSpeedSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSpeedSave.ForeColor = System.Drawing.Color.White;
            this.btnSpeedSave.Location = new System.Drawing.Point(1360, 6);
            this.btnSpeedSave.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnSpeedSave.Name = "btnSpeedSave";
            this.btnSpeedSave.Size = new System.Drawing.Size(120, 38);
            this.btnSpeedSave.TabIndex = 0;
            this.btnSpeedSave.Text = "SAVE";
            // 
            // btnSpeedScale
            // 
            this.btnSpeedScale.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSpeedScale.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSpeedScale.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSpeedScale.ForeColor = System.Drawing.Color.White;
            this.btnSpeedScale.Location = new System.Drawing.Point(1244, 6);
            this.btnSpeedScale.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.btnSpeedScale.Name = "btnSpeedScale";
            this.btnSpeedScale.Size = new System.Drawing.Size(110, 38);
            this.btnSpeedScale.TabIndex = 2;
            this.btnSpeedScale.Text = "100 %";
            // 
            // lblSpeedScaleCaption
            // 
            this.lblSpeedScaleCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpeedScaleCaption.Location = new System.Drawing.Point(1068, 6);
            this.lblSpeedScaleCaption.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.lblSpeedScaleCaption.Name = "lblSpeedScaleCaption";
            this.lblSpeedScaleCaption.Size = new System.Drawing.Size(170, 38);
            this.lblSpeedScaleCaption.TabIndex = 3;
            this.lblSpeedScaleCaption.Text = "DEFAULT SPEED SCALE %";
            this.lblSpeedScaleCaption.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // actionsPanel
            // 
            this.actionsPanel.ColumnCount = 13;
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.actionsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.actionsPanel.Controls.Add(this.btnEnable, 0, 0);
            this.actionsPanel.Controls.Add(this.btnDisable, 1, 0);
            this.actionsPanel.Controls.Add(this.btnHome, 2, 0);
            this.actionsPanel.Controls.Add(this.btnGroupHome, 3, 0);
            this.actionsPanel.Controls.Add(this.btnAllHome, 4, 0);
            this.actionsPanel.Controls.Add(this.btnAllStop, 5, 0);
            this.actionsPanel.Controls.Add(this.btnAlarmClear, 6, 0);
            this.actionsPanel.Controls.Add(this.btnAllServoOff, 7, 0);
            this.actionsPanel.Controls.Add(this.btnServoOn, 8, 0);
            this.actionsPanel.Controls.Add(this.btnServoOff, 9, 0);
            this.actionsPanel.Controls.Add(this.btnParaLoad, 10, 0);
            this.actionsPanel.Controls.Add(this.btnParaSave, 11, 0);
            this.actionsPanel.Controls.Add(this.btnBoardScan, 12, 0);
            this.actionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionsPanel.Location = new System.Drawing.Point(8, 832);
            this.actionsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.actionsPanel.Name = "actionsPanel";
            this.actionsPanel.RowCount = 1;
            this.actionsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionsPanel.Size = new System.Drawing.Size(1662, 60);
            this.actionsPanel.TabIndex = 5;
            // 
            // btnEnable
            // 
            this.btnEnable.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnEnable.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEnable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnEnable.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnEnable.ForeColor = System.Drawing.Color.White;
            this.btnEnable.Location = new System.Drawing.Point(4, 8);
            this.btnEnable.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.Size = new System.Drawing.Size(106, 44);
            this.btnEnable.TabIndex = 0;
            this.btnEnable.Text = "ENABLE";
            // 
            // btnDisable
            // 
            this.btnDisable.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnDisable.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDisable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDisable.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnDisable.ForeColor = System.Drawing.Color.White;
            this.btnDisable.Location = new System.Drawing.Point(118, 8);
            this.btnDisable.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnDisable.Name = "btnDisable";
            this.btnDisable.Size = new System.Drawing.Size(106, 44);
            this.btnDisable.TabIndex = 1;
            this.btnDisable.Text = "DISABLE";
            // 
            // btnHome
            // 
            this.btnHome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnHome.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHome.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnHome.ForeColor = System.Drawing.Color.White;
            this.btnHome.Location = new System.Drawing.Point(232, 8);
            this.btnHome.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnHome.Name = "btnHome";
            this.btnHome.Size = new System.Drawing.Size(122, 44);
            this.btnHome.TabIndex = 2;
            this.btnHome.Text = "INIT AXIS";
            // 
            // btnGroupHome
            // 
            this.btnGroupHome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnGroupHome.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGroupHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGroupHome.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnGroupHome.ForeColor = System.Drawing.Color.White;
            this.btnGroupHome.Location = new System.Drawing.Point(362, 8);
            this.btnGroupHome.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnGroupHome.Name = "btnGroupHome";
            this.btnGroupHome.Size = new System.Drawing.Size(122, 44);
            this.btnGroupHome.TabIndex = 3;
            this.btnGroupHome.Text = "INIT GROUP";
            // 
            // btnAllHome
            // 
            this.btnAllHome.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAllHome.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAllHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllHome.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllHome.ForeColor = System.Drawing.Color.White;
            this.btnAllHome.Location = new System.Drawing.Point(492, 8);
            this.btnAllHome.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAllHome.Name = "btnAllHome";
            this.btnAllHome.Size = new System.Drawing.Size(122, 44);
            this.btnAllHome.TabIndex = 4;
            this.btnAllHome.Text = "INIT ALL";
            // 
            // btnAllStop
            // 
            this.btnAllStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAllStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAllStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllStop.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllStop.ForeColor = System.Drawing.Color.White;
            this.btnAllStop.Location = new System.Drawing.Point(622, 8);
            this.btnAllStop.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAllStop.Name = "btnAllStop";
            this.btnAllStop.Size = new System.Drawing.Size(122, 44);
            this.btnAllStop.TabIndex = 5;
            this.btnAllStop.Text = "ALL STOP";
            // 
            // btnAlarmClear
            // 
            this.btnAlarmClear.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAlarmClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAlarmClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAlarmClear.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAlarmClear.ForeColor = System.Drawing.Color.White;
            this.btnAlarmClear.Location = new System.Drawing.Point(752, 8);
            this.btnAlarmClear.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAlarmClear.Name = "btnAlarmClear";
            this.btnAlarmClear.Size = new System.Drawing.Size(138, 44);
            this.btnAlarmClear.TabIndex = 4;
            this.btnAlarmClear.Text = "ALARM CLEAR";
            // 
            // btnAllServoOff
            // 
            this.btnAllServoOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAllServoOff.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAllServoOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllServoOff.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllServoOff.ForeColor = System.Drawing.Color.White;
            this.btnAllServoOff.Location = new System.Drawing.Point(898, 8);
            this.btnAllServoOff.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnAllServoOff.Name = "btnAllServoOff";
            this.btnAllServoOff.Size = new System.Drawing.Size(154, 44);
            this.btnAllServoOff.TabIndex = 5;
            this.btnAllServoOff.Text = "ALL SERVO OFF";
            // 
            // btnServoOn
            // 
            this.btnServoOn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnServoOn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnServoOn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnServoOn.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnServoOn.ForeColor = System.Drawing.Color.White;
            this.btnServoOn.Location = new System.Drawing.Point(1060, 8);
            this.btnServoOn.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnServoOn.Name = "btnServoOn";
            this.btnServoOn.Size = new System.Drawing.Size(122, 44);
            this.btnServoOn.TabIndex = 6;
            this.btnServoOn.Text = "SERVO ON";
            // 
            // btnServoOff
            // 
            this.btnServoOff.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnServoOff.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnServoOff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnServoOff.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnServoOff.ForeColor = System.Drawing.Color.White;
            this.btnServoOff.Location = new System.Drawing.Point(1190, 8);
            this.btnServoOff.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnServoOff.Name = "btnServoOff";
            this.btnServoOff.Size = new System.Drawing.Size(122, 44);
            this.btnServoOff.TabIndex = 7;
            this.btnServoOff.Text = "SERVO OFF";
            // 
            // btnParaLoad
            // 
            this.btnParaLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnParaLoad.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnParaLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnParaLoad.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnParaLoad.ForeColor = System.Drawing.Color.White;
            this.btnParaLoad.Location = new System.Drawing.Point(1320, 8);
            this.btnParaLoad.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnParaLoad.Name = "btnParaLoad";
            this.btnParaLoad.Size = new System.Drawing.Size(106, 44);
            this.btnParaLoad.TabIndex = 8;
            this.btnParaLoad.Text = "PARA LOAD";
            // 
            // btnParaSave
            // 
            this.btnParaSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnParaSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnParaSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnParaSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnParaSave.ForeColor = System.Drawing.Color.White;
            this.btnParaSave.Location = new System.Drawing.Point(1434, 8);
            this.btnParaSave.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnParaSave.Name = "btnParaSave";
            this.btnParaSave.Size = new System.Drawing.Size(106, 44);
            this.btnParaSave.TabIndex = 9;
            this.btnParaSave.Text = "PARA SAVE";
            // 
            // btnBoardScan
            // 
            this.btnBoardScan.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBoardScan.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBoardScan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBoardScan.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnBoardScan.ForeColor = System.Drawing.Color.White;
            this.btnBoardScan.Location = new System.Drawing.Point(1548, 8);
            this.btnBoardScan.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnBoardScan.Name = "btnBoardScan";
            this.btnBoardScan.Size = new System.Drawing.Size(110, 44);
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
    }
}
