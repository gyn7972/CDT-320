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
        private System.Windows.Forms.Label lblConfigBody;
        private System.Windows.Forms.Label lblInpositionBody;
        private System.Windows.Forms.Label lblHomeBody;
        private System.Windows.Forms.Label lblLimitBody;
        private System.Windows.Forms.Label lblEmergencyBody;
        private System.Windows.Forms.Label lblAlarmBody;
        private System.Windows.Forms.Label lblPositionClearBody;
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
            this.lblConfigBody = new System.Windows.Forms.Label();
            this.lblInpositionBody = new System.Windows.Forms.Label();
            this.lblHomeBody = new System.Windows.Forms.Label();
            this.lblLimitBody = new System.Windows.Forms.Label();
            this.lblEmergencyBody = new System.Windows.Forms.Label();
            this.lblAlarmBody = new System.Windows.Forms.Label();
            this.lblPositionClearBody = new System.Windows.Forms.Label();
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
            this.rootLayout.Size = new System.Drawing.Size(1416, 980);
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
            this.tabSpeed.Location = new System.Drawing.Point(36, 4);
            this.tabSpeed.Name = "tabSpeed";
            this.tabSpeed.Padding = new System.Windows.Forms.Padding(3);
            this.tabSpeed.Size = new System.Drawing.Size(1360, 498);
            this.tabSpeed.TabIndex = 2;
            this.tabSpeed.Text = "SPEED";
            // 
            // configLayout
            // 
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
            this.configLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.configLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 27.5F));
            this.configLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 27.5F));
            this.configLayout.Size = new System.Drawing.Size(1344, 482);
            this.configLayout.TabIndex = 0;
            // 
            // grpConfig
            // 
            this.grpConfig.Controls.Add(this.lblConfigBody);
            this.grpConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpConfig.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpConfig.Location = new System.Drawing.Point(3, 3);
            this.grpConfig.Name = "grpConfig";
            this.grpConfig.Size = new System.Drawing.Size(442, 210);
            this.grpConfig.TabIndex = 0;
            this.grpConfig.TabStop = false;
            this.grpConfig.Text = "CONFIG";
            // 
            // lblConfigBody
            // 
            this.lblConfigBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblConfigBody.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblConfigBody.Location = new System.Drawing.Point(3, 19);
            this.lblConfigBody.Name = "lblConfigBody";
            this.lblConfigBody.Padding = new System.Windows.Forms.Padding(8);
            this.lblConfigBody.Size = new System.Drawing.Size(436, 188);
            this.lblConfigBody.TabIndex = 0;
            this.lblConfigBody.Text = "OUTPUT MODE    PULSE-HIGH/CW/CCW\r\nINPUT MODE     OBVERSE SQR4\r\nINPUT SOURCE   EN" +
    "CODER\r\nZ PHASE LEVEL   HIGH\r\nSERVO LEVEL     HIGH\r\nMAX VELOCITY    3,000,000";
            // 
            // grpInposition
            // 
            this.grpInposition.Controls.Add(this.lblInpositionBody);
            this.grpInposition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInposition.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpInposition.Location = new System.Drawing.Point(451, 3);
            this.grpInposition.Name = "grpInposition";
            this.grpInposition.Size = new System.Drawing.Size(442, 210);
            this.grpInposition.TabIndex = 1;
            this.grpInposition.TabStop = false;
            this.grpInposition.Text = "INPOSITION";
            // 
            // lblInpositionBody
            // 
            this.lblInpositionBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInpositionBody.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblInpositionBody.Location = new System.Drawing.Point(3, 19);
            this.lblInpositionBody.Name = "lblInpositionBody";
            this.lblInpositionBody.Padding = new System.Windows.Forms.Padding(8);
            this.lblInpositionBody.Size = new System.Drawing.Size(436, 188);
            this.lblInpositionBody.TabIndex = 0;
            this.lblInpositionBody.Text = "LEVEL           ACTIVE HIGH\r\nSOFTWARE        DISABLE\r\nSOFTWARE LENGTH 10 pulse";
            // 
            // grpLimit
            // 
            this.grpLimit.Controls.Add(this.lblLimitBody);
            this.grpLimit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLimit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpLimit.Location = new System.Drawing.Point(899, 3);
            this.grpLimit.Name = "grpLimit";
            this.grpLimit.Size = new System.Drawing.Size(442, 210);
            this.grpLimit.TabIndex = 2;
            this.grpLimit.TabStop = false;
            this.grpLimit.Text = "LIMIT";
            // 
            // lblLimitBody
            // 
            this.lblLimitBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLimitBody.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblLimitBody.Location = new System.Drawing.Point(3, 19);
            this.lblLimitBody.Name = "lblLimitBody";
            this.lblLimitBody.Padding = new System.Windows.Forms.Padding(8);
            this.lblLimitBody.Size = new System.Drawing.Size(436, 188);
            this.lblLimitBody.TabIndex = 0;
            this.lblLimitBody.Text = "STOP MODE       EMERGENCY\r\nNEG LEVEL       ACTIVE LOW\r\nPOS LEVEL       ACTIVE LOW" +
    "\r\nSOFTWARE        DISABLE\r\nSW NEGATIVE     0 pulse\r\nSW POSITIVE     1,000,000 pulse";
            // 
            // grpEmergency
            // 
            this.grpEmergency.Controls.Add(this.lblEmergencyBody);
            this.grpEmergency.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpEmergency.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpEmergency.Location = new System.Drawing.Point(3, 219);
            this.grpEmergency.Name = "grpEmergency";
            this.grpEmergency.Size = new System.Drawing.Size(442, 126);
            this.grpEmergency.TabIndex = 3;
            this.grpEmergency.TabStop = false;
            this.grpEmergency.Text = "EMERGENCY SIGNAL";
            // 
            // lblEmergencyBody
            // 
            this.lblEmergencyBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmergencyBody.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblEmergencyBody.Location = new System.Drawing.Point(3, 19);
            this.lblEmergencyBody.Name = "lblEmergencyBody";
            this.lblEmergencyBody.Padding = new System.Windows.Forms.Padding(8);
            this.lblEmergencyBody.Size = new System.Drawing.Size(436, 104);
            this.lblEmergencyBody.TabIndex = 0;
            this.lblEmergencyBody.Text = "LEVEL           DISABLE\r\nSTOP MODE       EMERGENCY";
            // 
            // grpHome
            // 
            this.grpHome.Controls.Add(this.lblHomeBody);
            this.grpHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpHome.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpHome.Location = new System.Drawing.Point(451, 219);
            this.grpHome.Name = "grpHome";
            this.grpHome.Size = new System.Drawing.Size(442, 126);
            this.grpHome.TabIndex = 4;
            this.grpHome.TabStop = false;
            this.grpHome.Text = "HOME";
            // 
            // lblHomeBody
            // 
            this.lblHomeBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHomeBody.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblHomeBody.Location = new System.Drawing.Point(3, 19);
            this.lblHomeBody.Name = "lblHomeBody";
            this.lblHomeBody.Padding = new System.Windows.Forms.Padding(8);
            this.lblHomeBody.Size = new System.Drawing.Size(436, 104);
            this.lblHomeBody.TabIndex = 0;
            this.lblHomeBody.Text = "SIGNAL          LOW\r\nMODE            NEGATIVE LIMIT";
            // 
            // grpAlarm
            // 
            this.grpAlarm.Controls.Add(this.lblAlarmBody);
            this.grpAlarm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAlarm.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpAlarm.Location = new System.Drawing.Point(899, 219);
            this.grpAlarm.Name = "grpAlarm";
            this.grpAlarm.Size = new System.Drawing.Size(442, 126);
            this.grpAlarm.TabIndex = 5;
            this.grpAlarm.TabStop = false;
            this.grpAlarm.Text = "ALARM";
            // 
            // lblAlarmBody
            // 
            this.lblAlarmBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAlarmBody.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblAlarmBody.Location = new System.Drawing.Point(3, 19);
            this.lblAlarmBody.Name = "lblAlarmBody";
            this.lblAlarmBody.Padding = new System.Windows.Forms.Padding(8);
            this.lblAlarmBody.Size = new System.Drawing.Size(436, 104);
            this.lblAlarmBody.TabIndex = 0;
            this.lblAlarmBody.Text = "RESET SIGNAL    HIGH\r\nLEVEL           ACTIVE LOW";
            // 
            // grpPositionClear
            // 
            this.grpPositionClear.Controls.Add(this.lblPositionClearBody);
            this.grpPositionClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPositionClear.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpPositionClear.Location = new System.Drawing.Point(3, 351);
            this.grpPositionClear.Name = "grpPositionClear";
            this.grpPositionClear.Size = new System.Drawing.Size(442, 128);
            this.grpPositionClear.TabIndex = 6;
            this.grpPositionClear.TabStop = false;
            this.grpPositionClear.Text = "POSITION CLEAR";
            // 
            // lblPositionClearBody
            // 
            this.lblPositionClearBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPositionClearBody.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblPositionClearBody.Location = new System.Drawing.Point(3, 19);
            this.lblPositionClearBody.Name = "lblPositionClearBody";
            this.lblPositionClearBody.Padding = new System.Windows.Forms.Padding(8);
            this.lblPositionClearBody.Size = new System.Drawing.Size(436, 106);
            this.lblPositionClearBody.TabIndex = 0;
            this.lblPositionClearBody.Text = "ENABLED         FALSE\r\nPULSE           10,000 pulse";
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
            this.Size = new System.Drawing.Size(1416, 980);
            this.rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.configTabs.ResumeLayout(false);
            this.tabConfig.ResumeLayout(false);
            this.configLayout.ResumeLayout(false);
            this.actionsPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
