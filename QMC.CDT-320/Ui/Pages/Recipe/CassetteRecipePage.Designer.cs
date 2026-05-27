namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class CassetteRecipePage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel contentLayout;

        private System.Windows.Forms.TableLayoutPanel panelLeft;
        private System.Windows.Forms.TableLayoutPanel actionSection;
        private System.Windows.Forms.Label lblActionTitle;
        private QMC.CDT_320.Ui.Controls.ActionButton btnLoadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnUnloadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnReadyMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSlotLoadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSlotUnloadingMove;
        private System.Windows.Forms.TableLayoutPanel ioSection;
        private System.Windows.Forms.Label lblIoTitle;
        private System.Windows.Forms.TableLayoutPanel ioSensor1Row;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotSensor1;
        private System.Windows.Forms.Label lblSensor1;
        private System.Windows.Forms.TableLayoutPanel ioSensor2Row;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotSensor2;
        private System.Windows.Forms.Label lblSensor2;
        private System.Windows.Forms.TableLayoutPanel ioProtrusionRow;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotProtrusion;
        private System.Windows.Forms.Label lblProtrusion;
        private System.Windows.Forms.TableLayoutPanel ioMappingRow;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotMapping;
        private System.Windows.Forms.Label lblMapping;

        private System.Windows.Forms.TableLayoutPanel panelCenter;
        private System.Windows.Forms.Label lblOptTitle;
        private System.Windows.Forms.TableLayoutPanel optionRows;
        private System.Windows.Forms.Label lblOptLoadingZKey;
        private System.Windows.Forms.Label lblOptLoadingZVal;
        private System.Windows.Forms.Label lblOptUnloadingZKey;
        private System.Windows.Forms.Label lblOptUnloadingZVal;
        private System.Windows.Forms.Label lblOptReadyPosKey;
        private System.Windows.Forms.Label lblOptReadyPosVal;
        private System.Windows.Forms.Label lblOptMappingZKey;
        private System.Windows.Forms.Label lblOptMappingZVal;
        private System.Windows.Forms.Label lblOptSlotPitchKey;
        private System.Windows.Forms.Label lblOptSlotPitchVal;
        private System.Windows.Forms.Label lblOptCassetteGapKey;
        private System.Windows.Forms.Label lblOptCassetteGapVal;
        private System.Windows.Forms.Label lblOptInchKey;
        private System.Windows.Forms.Label lblOptInchVal;
        private System.Windows.Forms.Label lblOptStageKey;
        private System.Windows.Forms.Label lblOptStageVal;
        private System.Windows.Forms.Label lblWaitTitle;
        private System.Windows.Forms.TableLayoutPanel waitRows;
        private System.Windows.Forms.Label lblWaitKey;
        private System.Windows.Forms.Label lblWaitVal;

        private System.Windows.Forms.TableLayoutPanel panelRight;
        private System.Windows.Forms.TableLayoutPanel jogSection;
        private System.Windows.Forms.Label lblJogTitle;
        private System.Windows.Forms.Label lblAxisName;
        private System.Windows.Forms.Button btnJogPlus;
        private System.Windows.Forms.Button btnJogStop;
        private System.Windows.Forms.Button btnJogMinus;
        private System.Windows.Forms.TableLayoutPanel speedSection;
        private System.Windows.Forms.Label lblSpeedTitle;
        private System.Windows.Forms.Label lblSpeedHigh;
        private System.Windows.Forms.TrackBar trkSpeed;
        private System.Windows.Forms.Label lblSpeedMid;
        private System.Windows.Forms.Label lblSpeedLow;
        private System.Windows.Forms.Label lblSpeedValue;

        private QMC.CDT_320.Ui.Controls.UnitConfigGrid unitConfigGrid;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.panelLeft = new System.Windows.Forms.TableLayoutPanel();
            this.actionSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblActionTitle = new System.Windows.Forms.Label();
            this.btnLoadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnloadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReadyMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSlotLoadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSlotUnloadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.ioSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblIoTitle = new System.Windows.Forms.Label();
            this.ioSensor1Row = new System.Windows.Forms.TableLayoutPanel();
            this.dotSensor1 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblSensor1 = new System.Windows.Forms.Label();
            this.ioSensor2Row = new System.Windows.Forms.TableLayoutPanel();
            this.dotSensor2 = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblSensor2 = new System.Windows.Forms.Label();
            this.ioProtrusionRow = new System.Windows.Forms.TableLayoutPanel();
            this.dotProtrusion = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblProtrusion = new System.Windows.Forms.Label();
            this.ioMappingRow = new System.Windows.Forms.TableLayoutPanel();
            this.dotMapping = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblMapping = new System.Windows.Forms.Label();
            this.panelCenter = new System.Windows.Forms.TableLayoutPanel();
            this.lblOptTitle = new System.Windows.Forms.Label();
            this.optionRows = new System.Windows.Forms.TableLayoutPanel();
            this.lblOptLoadingZKey = new System.Windows.Forms.Label();
            this.lblOptLoadingZVal = new System.Windows.Forms.Label();
            this.lblOptUnloadingZKey = new System.Windows.Forms.Label();
            this.lblOptUnloadingZVal = new System.Windows.Forms.Label();
            this.lblOptReadyPosKey = new System.Windows.Forms.Label();
            this.lblOptReadyPosVal = new System.Windows.Forms.Label();
            this.lblOptMappingZKey = new System.Windows.Forms.Label();
            this.lblOptMappingZVal = new System.Windows.Forms.Label();
            this.lblOptSlotPitchKey = new System.Windows.Forms.Label();
            this.lblOptSlotPitchVal = new System.Windows.Forms.Label();
            this.lblOptCassetteGapKey = new System.Windows.Forms.Label();
            this.lblOptCassetteGapVal = new System.Windows.Forms.Label();
            this.lblOptInchKey = new System.Windows.Forms.Label();
            this.lblOptInchVal = new System.Windows.Forms.Label();
            this.lblOptStageKey = new System.Windows.Forms.Label();
            this.lblOptStageVal = new System.Windows.Forms.Label();
            this.lblWaitTitle = new System.Windows.Forms.Label();
            this.waitRows = new System.Windows.Forms.TableLayoutPanel();
            this.lblWaitKey = new System.Windows.Forms.Label();
            this.lblWaitVal = new System.Windows.Forms.Label();
            this.panelRight = new System.Windows.Forms.TableLayoutPanel();
            this.jogSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblJogTitle = new System.Windows.Forms.Label();
            this.lblAxisName = new System.Windows.Forms.Label();
            this.btnJogPlus = new System.Windows.Forms.Button();
            this.btnJogStop = new System.Windows.Forms.Button();
            this.btnJogMinus = new System.Windows.Forms.Button();
            this.speedSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblSpeedTitle = new System.Windows.Forms.Label();
            this.trkSpeed = new System.Windows.Forms.TrackBar();
            this.lblSpeedHigh = new System.Windows.Forms.Label();
            this.lblSpeedMid = new System.Windows.Forms.Label();
            this.lblSpeedLow = new System.Windows.Forms.Label();
            this.lblSpeedValue = new System.Windows.Forms.Label();
            this.unitConfigGrid = new QMC.CDT_320.Ui.Controls.UnitConfigGrid();
            this.mainLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.panelLeft.SuspendLayout();
            this.actionSection.SuspendLayout();
            this.ioSection.SuspendLayout();
            this.ioSensor1Row.SuspendLayout();
            this.ioSensor2Row.SuspendLayout();
            this.ioProtrusionRow.SuspendLayout();
            this.ioMappingRow.SuspendLayout();
            this.panelCenter.SuspendLayout();
            this.optionRows.SuspendLayout();
            this.waitRows.SuspendLayout();
            this.panelRight.SuspendLayout();
            this.jogSection.SuspendLayout();
            this.speedSection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).BeginInit();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.lblHeader, 0, 0);
            this.mainLayout.Controls.Add(this.contentLayout, 0, 1);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.RowCount = 2;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Size = new System.Drawing.Size(1360, 920);
            this.mainLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1360, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "INPUT/OUTPUT CASSETTE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 4;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 240F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 490F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 266F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Controls.Add(this.panelLeft, 0, 0);
            this.contentLayout.Controls.Add(this.panelCenter, 1, 0);
            this.contentLayout.Controls.Add(this.panelRight, 2, 0);
            this.contentLayout.Controls.Add(this.unitConfigGrid, 3, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(8, 6, 8, 8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1360, 890);
            this.contentLayout.TabIndex = 1;
            // 
            // panelLeft
            // 
            this.panelLeft.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelLeft.ColumnCount = 1;
            this.panelLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelLeft.Controls.Add(this.actionSection, 0, 0);
            this.panelLeft.Controls.Add(this.ioSection, 0, 1);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLeft.Location = new System.Drawing.Point(8, 6);
            this.panelLeft.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.RowCount = 2;
            this.panelLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 330F));
            this.panelLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelLeft.Size = new System.Drawing.Size(232, 876);
            this.panelLeft.TabIndex = 0;
            // 
            // actionSection
            // 
            this.actionSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.actionSection.ColumnCount = 1;
            this.actionSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionSection.Controls.Add(this.lblActionTitle, 0, 0);
            this.actionSection.Controls.Add(this.btnLoadingMove, 0, 1);
            this.actionSection.Controls.Add(this.btnUnloadingMove, 0, 2);
            this.actionSection.Controls.Add(this.btnReadyMove, 0, 3);
            this.actionSection.Controls.Add(this.btnSlotLoadingMove, 0, 5);
            this.actionSection.Controls.Add(this.btnSlotUnloadingMove, 0, 6);
            this.actionSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionSection.Location = new System.Drawing.Point(0, 0);
            this.actionSection.Margin = new System.Windows.Forms.Padding(0);
            this.actionSection.Name = "actionSection";
            this.actionSection.RowCount = 7;
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 12F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.actionSection.Size = new System.Drawing.Size(232, 330);
            this.actionSection.TabIndex = 0;
            // 
            // lblActionTitle
            // 
            this.lblActionTitle.BackColor = System.Drawing.Color.Orange;
            this.lblActionTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActionTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblActionTitle.ForeColor = System.Drawing.Color.Black;
            this.lblActionTitle.Location = new System.Drawing.Point(0, 0);
            this.lblActionTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblActionTitle.Name = "lblActionTitle";
            this.lblActionTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblActionTitle.Size = new System.Drawing.Size(232, 26);
            this.lblActionTitle.TabIndex = 0;
            this.lblActionTitle.Text = "동작";
            this.lblActionTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnLoadingMove
            // 
            this.btnLoadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadingMove.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnLoadingMove.ForeColor = System.Drawing.Color.White;
            this.btnLoadingMove.Location = new System.Drawing.Point(8, 30);
            this.btnLoadingMove.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.btnLoadingMove.Name = "btnLoadingMove";
            this.btnLoadingMove.Size = new System.Drawing.Size(216, 44);
            this.btnLoadingMove.TabIndex = 1;
            this.btnLoadingMove.Text = "LOADING 위치 이동";
            this.btnLoadingMove.Click += new System.EventHandler(this.btnLoadingMove_Click);
            // 
            // btnUnloadingMove
            // 
            this.btnUnloadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnUnloadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUnloadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUnloadingMove.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnUnloadingMove.ForeColor = System.Drawing.Color.White;
            this.btnUnloadingMove.Location = new System.Drawing.Point(8, 82);
            this.btnUnloadingMove.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.btnUnloadingMove.Name = "btnUnloadingMove";
            this.btnUnloadingMove.Size = new System.Drawing.Size(216, 44);
            this.btnUnloadingMove.TabIndex = 2;
            this.btnUnloadingMove.Text = "UNLOADING 위치 이동";
            this.btnUnloadingMove.Click += new System.EventHandler(this.btnUnloadingMove_Click);
            // 
            // btnReadyMove
            // 
            this.btnReadyMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnReadyMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReadyMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReadyMove.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnReadyMove.ForeColor = System.Drawing.Color.White;
            this.btnReadyMove.Location = new System.Drawing.Point(8, 134);
            this.btnReadyMove.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.btnReadyMove.Name = "btnReadyMove";
            this.btnReadyMove.Size = new System.Drawing.Size(216, 44);
            this.btnReadyMove.TabIndex = 3;
            this.btnReadyMove.Text = "준비 위치 이동";
            this.btnReadyMove.Click += new System.EventHandler(this.btnReadyMove_Click);
            // 
            // btnSlotLoadingMove
            // 
            this.btnSlotLoadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSlotLoadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSlotLoadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSlotLoadingMove.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnSlotLoadingMove.ForeColor = System.Drawing.Color.White;
            this.btnSlotLoadingMove.Location = new System.Drawing.Point(8, 198);
            this.btnSlotLoadingMove.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.btnSlotLoadingMove.Name = "btnSlotLoadingMove";
            this.btnSlotLoadingMove.Size = new System.Drawing.Size(216, 44);
            this.btnSlotLoadingMove.TabIndex = 4;
            this.btnSlotLoadingMove.Text = "SLOT 위치 이동 (LOADING)";
            this.btnSlotLoadingMove.Click += new System.EventHandler(this.btnSlotLoadingMove_Click);
            // 
            // btnSlotUnloadingMove
            // 
            this.btnSlotUnloadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSlotUnloadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSlotUnloadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSlotUnloadingMove.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnSlotUnloadingMove.ForeColor = System.Drawing.Color.White;
            this.btnSlotUnloadingMove.Location = new System.Drawing.Point(8, 250);
            this.btnSlotUnloadingMove.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.btnSlotUnloadingMove.Name = "btnSlotUnloadingMove";
            this.btnSlotUnloadingMove.Size = new System.Drawing.Size(216, 76);
            this.btnSlotUnloadingMove.TabIndex = 5;
            this.btnSlotUnloadingMove.Text = "SLOT 위치 이동 (UNLOADING)";
            this.btnSlotUnloadingMove.Click += new System.EventHandler(this.btnSlotUnloadingMove_Click);
            // 
            // ioSection
            // 
            this.ioSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ioSection.ColumnCount = 1;
            this.ioSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioSection.Controls.Add(this.lblIoTitle, 0, 0);
            this.ioSection.Controls.Add(this.ioSensor1Row, 0, 1);
            this.ioSection.Controls.Add(this.ioSensor2Row, 0, 2);
            this.ioSection.Controls.Add(this.ioProtrusionRow, 0, 3);
            this.ioSection.Controls.Add(this.ioMappingRow, 0, 4);
            this.ioSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioSection.Location = new System.Drawing.Point(0, 330);
            this.ioSection.Margin = new System.Windows.Forms.Padding(0);
            this.ioSection.Name = "ioSection";
            this.ioSection.RowCount = 6;
            this.ioSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.ioSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioSection.Size = new System.Drawing.Size(232, 546);
            this.ioSection.TabIndex = 1;
            // 
            // lblIoTitle
            // 
            this.lblIoTitle.BackColor = System.Drawing.Color.Orange;
            this.lblIoTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIoTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblIoTitle.ForeColor = System.Drawing.Color.Black;
            this.lblIoTitle.Location = new System.Drawing.Point(0, 0);
            this.lblIoTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblIoTitle.Name = "lblIoTitle";
            this.lblIoTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblIoTitle.Size = new System.Drawing.Size(232, 26);
            this.lblIoTitle.TabIndex = 0;
            this.lblIoTitle.Text = "실린더 & I/O";
            this.lblIoTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ioSensor1Row
            // 
            this.ioSensor1Row.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ioSensor1Row.ColumnCount = 2;
            this.ioSensor1Row.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.ioSensor1Row.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioSensor1Row.Controls.Add(this.dotSensor1, 0, 0);
            this.ioSensor1Row.Controls.Add(this.lblSensor1, 1, 0);
            this.ioSensor1Row.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioSensor1Row.Location = new System.Drawing.Point(0, 26);
            this.ioSensor1Row.Margin = new System.Windows.Forms.Padding(0);
            this.ioSensor1Row.Name = "ioSensor1Row";
            this.ioSensor1Row.RowCount = 1;
            this.ioSensor1Row.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioSensor1Row.Size = new System.Drawing.Size(232, 28);
            this.ioSensor1Row.TabIndex = 1;
            // 
            // dotSensor1
            // 
            this.dotSensor1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotSensor1.BackColor = System.Drawing.Color.Transparent;
            this.dotSensor1.Location = new System.Drawing.Point(5, 7);
            this.dotSensor1.Margin = new System.Windows.Forms.Padding(0);
            this.dotSensor1.Name = "dotSensor1";
            this.dotSensor1.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotSensor1.OnColor = System.Drawing.Color.LimeGreen;
            this.dotSensor1.Size = new System.Drawing.Size(14, 14);
            this.dotSensor1.TabIndex = 0;
            // 
            // lblSensor1
            // 
            this.lblSensor1.BackColor = System.Drawing.Color.Gainsboro;
            this.lblSensor1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSensor1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSensor1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSensor1.Location = new System.Drawing.Point(26, 1);
            this.lblSensor1.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.lblSensor1.Name = "lblSensor1";
            this.lblSensor1.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblSensor1.Size = new System.Drawing.Size(204, 26);
            this.lblSensor1.TabIndex = 1;
            this.lblSensor1.Text = "CASSETTE 감지 SENSOR 1";
            this.lblSensor1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ioSensor2Row
            // 
            this.ioSensor2Row.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ioSensor2Row.ColumnCount = 2;
            this.ioSensor2Row.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.ioSensor2Row.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioSensor2Row.Controls.Add(this.dotSensor2, 0, 0);
            this.ioSensor2Row.Controls.Add(this.lblSensor2, 1, 0);
            this.ioSensor2Row.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioSensor2Row.Location = new System.Drawing.Point(0, 54);
            this.ioSensor2Row.Margin = new System.Windows.Forms.Padding(0);
            this.ioSensor2Row.Name = "ioSensor2Row";
            this.ioSensor2Row.RowCount = 1;
            this.ioSensor2Row.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioSensor2Row.Size = new System.Drawing.Size(232, 28);
            this.ioSensor2Row.TabIndex = 2;
            // 
            // dotSensor2
            // 
            this.dotSensor2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotSensor2.BackColor = System.Drawing.Color.Transparent;
            this.dotSensor2.Location = new System.Drawing.Point(5, 7);
            this.dotSensor2.Margin = new System.Windows.Forms.Padding(0);
            this.dotSensor2.Name = "dotSensor2";
            this.dotSensor2.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotSensor2.OnColor = System.Drawing.Color.LimeGreen;
            this.dotSensor2.Size = new System.Drawing.Size(14, 14);
            this.dotSensor2.TabIndex = 0;
            // 
            // lblSensor2
            // 
            this.lblSensor2.BackColor = System.Drawing.Color.Gainsboro;
            this.lblSensor2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSensor2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSensor2.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSensor2.Location = new System.Drawing.Point(26, 1);
            this.lblSensor2.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.lblSensor2.Name = "lblSensor2";
            this.lblSensor2.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblSensor2.Size = new System.Drawing.Size(204, 26);
            this.lblSensor2.TabIndex = 1;
            this.lblSensor2.Text = "CASSETTE 감지 SENSOR 2";
            this.lblSensor2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ioProtrusionRow
            // 
            this.ioProtrusionRow.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ioProtrusionRow.ColumnCount = 2;
            this.ioProtrusionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.ioProtrusionRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioProtrusionRow.Controls.Add(this.dotProtrusion, 0, 0);
            this.ioProtrusionRow.Controls.Add(this.lblProtrusion, 1, 0);
            this.ioProtrusionRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioProtrusionRow.Location = new System.Drawing.Point(0, 82);
            this.ioProtrusionRow.Margin = new System.Windows.Forms.Padding(0);
            this.ioProtrusionRow.Name = "ioProtrusionRow";
            this.ioProtrusionRow.RowCount = 1;
            this.ioProtrusionRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioProtrusionRow.Size = new System.Drawing.Size(232, 28);
            this.ioProtrusionRow.TabIndex = 3;
            // 
            // dotProtrusion
            // 
            this.dotProtrusion.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotProtrusion.BackColor = System.Drawing.Color.Transparent;
            this.dotProtrusion.Location = new System.Drawing.Point(5, 7);
            this.dotProtrusion.Margin = new System.Windows.Forms.Padding(0);
            this.dotProtrusion.Name = "dotProtrusion";
            this.dotProtrusion.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotProtrusion.OnColor = System.Drawing.Color.LimeGreen;
            this.dotProtrusion.Size = new System.Drawing.Size(14, 14);
            this.dotProtrusion.TabIndex = 0;
            // 
            // lblProtrusion
            // 
            this.lblProtrusion.BackColor = System.Drawing.Color.Gainsboro;
            this.lblProtrusion.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblProtrusion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProtrusion.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblProtrusion.Location = new System.Drawing.Point(26, 1);
            this.lblProtrusion.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.lblProtrusion.Name = "lblProtrusion";
            this.lblProtrusion.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblProtrusion.Size = new System.Drawing.Size(204, 26);
            this.lblProtrusion.TabIndex = 1;
            this.lblProtrusion.Text = "돌출 감지 SENSOR";
            this.lblProtrusion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ioMappingRow
            // 
            this.ioMappingRow.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ioMappingRow.ColumnCount = 2;
            this.ioMappingRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.ioMappingRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioMappingRow.Controls.Add(this.dotMapping, 0, 0);
            this.ioMappingRow.Controls.Add(this.lblMapping, 1, 0);
            this.ioMappingRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioMappingRow.Location = new System.Drawing.Point(0, 110);
            this.ioMappingRow.Margin = new System.Windows.Forms.Padding(0);
            this.ioMappingRow.Name = "ioMappingRow";
            this.ioMappingRow.RowCount = 1;
            this.ioMappingRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioMappingRow.Size = new System.Drawing.Size(232, 28);
            this.ioMappingRow.TabIndex = 4;
            // 
            // dotMapping
            // 
            this.dotMapping.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dotMapping.BackColor = System.Drawing.Color.Transparent;
            this.dotMapping.Location = new System.Drawing.Point(5, 7);
            this.dotMapping.Margin = new System.Windows.Forms.Padding(0);
            this.dotMapping.Name = "dotMapping";
            this.dotMapping.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotMapping.OnColor = System.Drawing.Color.LimeGreen;
            this.dotMapping.Size = new System.Drawing.Size(14, 14);
            this.dotMapping.TabIndex = 0;
            // 
            // lblMapping
            // 
            this.lblMapping.BackColor = System.Drawing.Color.Gainsboro;
            this.lblMapping.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMapping.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapping.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblMapping.Location = new System.Drawing.Point(26, 1);
            this.lblMapping.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.lblMapping.Name = "lblMapping";
            this.lblMapping.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblMapping.Size = new System.Drawing.Size(204, 26);
            this.lblMapping.TabIndex = 1;
            this.lblMapping.Text = "맵핑센서";
            this.lblMapping.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelCenter
            // 
            this.panelCenter.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelCenter.ColumnCount = 1;
            this.panelCenter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelCenter.Controls.Add(this.lblOptTitle, 0, 0);
            this.panelCenter.Controls.Add(this.optionRows, 0, 1);
            this.panelCenter.Controls.Add(this.lblWaitTitle, 0, 2);
            this.panelCenter.Controls.Add(this.waitRows, 0, 3);
            this.panelCenter.Location = new System.Drawing.Point(248, 6);
            this.panelCenter.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.panelCenter.Name = "panelCenter";
            this.panelCenter.RowCount = 5;
            this.panelCenter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.panelCenter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 256F));
            this.panelCenter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.panelCenter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.panelCenter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelCenter.Size = new System.Drawing.Size(482, 876);
            this.panelCenter.TabIndex = 1;
            // 
            // lblOptTitle
            // 
            this.lblOptTitle.BackColor = System.Drawing.Color.Orange;
            this.lblOptTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblOptTitle.ForeColor = System.Drawing.Color.Black;
            this.lblOptTitle.Location = new System.Drawing.Point(0, 0);
            this.lblOptTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblOptTitle.Name = "lblOptTitle";
            this.lblOptTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblOptTitle.Size = new System.Drawing.Size(482, 26);
            this.lblOptTitle.TabIndex = 0;
            this.lblOptTitle.Text = "옵션";
            this.lblOptTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optionRows
            // 
            this.optionRows.BackColor = System.Drawing.Color.WhiteSmoke;
            this.optionRows.ColumnCount = 2;
            this.optionRows.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.optionRows.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.optionRows.Controls.Add(this.lblOptLoadingZKey, 0, 0);
            this.optionRows.Controls.Add(this.lblOptLoadingZVal, 1, 0);
            this.optionRows.Controls.Add(this.lblOptUnloadingZKey, 0, 1);
            this.optionRows.Controls.Add(this.lblOptUnloadingZVal, 1, 1);
            this.optionRows.Controls.Add(this.lblOptReadyPosKey, 0, 2);
            this.optionRows.Controls.Add(this.lblOptReadyPosVal, 1, 2);
            this.optionRows.Controls.Add(this.lblOptMappingZKey, 0, 3);
            this.optionRows.Controls.Add(this.lblOptMappingZVal, 1, 3);
            this.optionRows.Controls.Add(this.lblOptSlotPitchKey, 0, 4);
            this.optionRows.Controls.Add(this.lblOptSlotPitchVal, 1, 4);
            this.optionRows.Controls.Add(this.lblOptCassetteGapKey, 0, 5);
            this.optionRows.Controls.Add(this.lblOptCassetteGapVal, 1, 5);
            this.optionRows.Controls.Add(this.lblOptInchKey, 0, 6);
            this.optionRows.Controls.Add(this.lblOptInchVal, 1, 6);
            this.optionRows.Controls.Add(this.lblOptStageKey, 0, 7);
            this.optionRows.Controls.Add(this.lblOptStageVal, 1, 7);
            this.optionRows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.optionRows.Location = new System.Drawing.Point(0, 26);
            this.optionRows.Margin = new System.Windows.Forms.Padding(0);
            this.optionRows.Name = "optionRows";
            this.optionRows.RowCount = 8;
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionRows.Size = new System.Drawing.Size(482, 256);
            this.optionRows.TabIndex = 1;
            // 
            // lblOptLoadingZKey
            // 
            this.lblOptLoadingZKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblOptLoadingZKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptLoadingZKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptLoadingZKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptLoadingZKey.Location = new System.Drawing.Point(2, 2);
            this.lblOptLoadingZKey.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptLoadingZKey.Name = "lblOptLoadingZKey";
            this.lblOptLoadingZKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptLoadingZKey.Size = new System.Drawing.Size(216, 28);
            this.lblOptLoadingZKey.TabIndex = 0;
            this.lblOptLoadingZKey.Text = "LOADING Z";
            this.lblOptLoadingZKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptLoadingZVal
            // 
            this.lblOptLoadingZVal.BackColor = System.Drawing.Color.White;
            this.lblOptLoadingZVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptLoadingZVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptLoadingZVal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptLoadingZVal.Location = new System.Drawing.Point(222, 2);
            this.lblOptLoadingZVal.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptLoadingZVal.Name = "lblOptLoadingZVal";
            this.lblOptLoadingZVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptLoadingZVal.Size = new System.Drawing.Size(258, 28);
            this.lblOptLoadingZVal.TabIndex = 1;
            this.lblOptLoadingZVal.Text = "121070 um";
            this.lblOptLoadingZVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptUnloadingZKey
            // 
            this.lblOptUnloadingZKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblOptUnloadingZKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptUnloadingZKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptUnloadingZKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptUnloadingZKey.Location = new System.Drawing.Point(2, 34);
            this.lblOptUnloadingZKey.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptUnloadingZKey.Name = "lblOptUnloadingZKey";
            this.lblOptUnloadingZKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptUnloadingZKey.Size = new System.Drawing.Size(216, 28);
            this.lblOptUnloadingZKey.TabIndex = 2;
            this.lblOptUnloadingZKey.Text = "UNLOADING Z";
            this.lblOptUnloadingZKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptUnloadingZVal
            // 
            this.lblOptUnloadingZVal.BackColor = System.Drawing.Color.White;
            this.lblOptUnloadingZVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptUnloadingZVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptUnloadingZVal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptUnloadingZVal.Location = new System.Drawing.Point(222, 34);
            this.lblOptUnloadingZVal.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptUnloadingZVal.Name = "lblOptUnloadingZVal";
            this.lblOptUnloadingZVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptUnloadingZVal.Size = new System.Drawing.Size(258, 28);
            this.lblOptUnloadingZVal.TabIndex = 3;
            this.lblOptUnloadingZVal.Text = "117000 um";
            this.lblOptUnloadingZVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptReadyPosKey
            // 
            this.lblOptReadyPosKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblOptReadyPosKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptReadyPosKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptReadyPosKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptReadyPosKey.Location = new System.Drawing.Point(2, 66);
            this.lblOptReadyPosKey.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptReadyPosKey.Name = "lblOptReadyPosKey";
            this.lblOptReadyPosKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptReadyPosKey.Size = new System.Drawing.Size(216, 28);
            this.lblOptReadyPosKey.TabIndex = 4;
            this.lblOptReadyPosKey.Text = "READY POSITION";
            this.lblOptReadyPosKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptReadyPosVal
            // 
            this.lblOptReadyPosVal.BackColor = System.Drawing.Color.White;
            this.lblOptReadyPosVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptReadyPosVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptReadyPosVal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptReadyPosVal.Location = new System.Drawing.Point(222, 66);
            this.lblOptReadyPosVal.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptReadyPosVal.Name = "lblOptReadyPosVal";
            this.lblOptReadyPosVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptReadyPosVal.Size = new System.Drawing.Size(258, 28);
            this.lblOptReadyPosVal.TabIndex = 5;
            this.lblOptReadyPosVal.Text = "129771 um";
            this.lblOptReadyPosVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptMappingZKey
            // 
            this.lblOptMappingZKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblOptMappingZKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptMappingZKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptMappingZKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptMappingZKey.Location = new System.Drawing.Point(2, 98);
            this.lblOptMappingZKey.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptMappingZKey.Name = "lblOptMappingZKey";
            this.lblOptMappingZKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptMappingZKey.Size = new System.Drawing.Size(216, 28);
            this.lblOptMappingZKey.TabIndex = 6;
            this.lblOptMappingZKey.Text = "MAPPING Z";
            this.lblOptMappingZKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptMappingZVal
            // 
            this.lblOptMappingZVal.BackColor = System.Drawing.Color.White;
            this.lblOptMappingZVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptMappingZVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptMappingZVal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptMappingZVal.Location = new System.Drawing.Point(222, 98);
            this.lblOptMappingZVal.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptMappingZVal.Name = "lblOptMappingZVal";
            this.lblOptMappingZVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptMappingZVal.Size = new System.Drawing.Size(258, 28);
            this.lblOptMappingZVal.TabIndex = 7;
            this.lblOptMappingZVal.Text = "104745 um";
            this.lblOptMappingZVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptSlotPitchKey
            // 
            this.lblOptSlotPitchKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblOptSlotPitchKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptSlotPitchKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptSlotPitchKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptSlotPitchKey.Location = new System.Drawing.Point(2, 130);
            this.lblOptSlotPitchKey.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptSlotPitchKey.Name = "lblOptSlotPitchKey";
            this.lblOptSlotPitchKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptSlotPitchKey.Size = new System.Drawing.Size(216, 28);
            this.lblOptSlotPitchKey.TabIndex = 8;
            this.lblOptSlotPitchKey.Text = "SLOT PITCH";
            this.lblOptSlotPitchKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptSlotPitchVal
            // 
            this.lblOptSlotPitchVal.BackColor = System.Drawing.Color.White;
            this.lblOptSlotPitchVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptSlotPitchVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptSlotPitchVal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptSlotPitchVal.Location = new System.Drawing.Point(222, 130);
            this.lblOptSlotPitchVal.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptSlotPitchVal.Name = "lblOptSlotPitchVal";
            this.lblOptSlotPitchVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptSlotPitchVal.Size = new System.Drawing.Size(258, 28);
            this.lblOptSlotPitchVal.TabIndex = 9;
            this.lblOptSlotPitchVal.Text = "19000 um";
            this.lblOptSlotPitchVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptCassetteGapKey
            // 
            this.lblOptCassetteGapKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblOptCassetteGapKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptCassetteGapKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptCassetteGapKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptCassetteGapKey.Location = new System.Drawing.Point(2, 162);
            this.lblOptCassetteGapKey.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptCassetteGapKey.Name = "lblOptCassetteGapKey";
            this.lblOptCassetteGapKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptCassetteGapKey.Size = new System.Drawing.Size(216, 28);
            this.lblOptCassetteGapKey.TabIndex = 10;
            this.lblOptCassetteGapKey.Text = "카세트 간격";
            this.lblOptCassetteGapKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptCassetteGapVal
            // 
            this.lblOptCassetteGapVal.BackColor = System.Drawing.Color.White;
            this.lblOptCassetteGapVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptCassetteGapVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptCassetteGapVal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptCassetteGapVal.Location = new System.Drawing.Point(222, 162);
            this.lblOptCassetteGapVal.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptCassetteGapVal.Name = "lblOptCassetteGapVal";
            this.lblOptCassetteGapVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptCassetteGapVal.Size = new System.Drawing.Size(258, 28);
            this.lblOptCassetteGapVal.TabIndex = 11;
            this.lblOptCassetteGapVal.Text = "59000 um";
            this.lblOptCassetteGapVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptInchKey
            // 
            this.lblOptInchKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblOptInchKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptInchKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptInchKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptInchKey.Location = new System.Drawing.Point(2, 194);
            this.lblOptInchKey.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptInchKey.Name = "lblOptInchKey";
            this.lblOptInchKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptInchKey.Size = new System.Drawing.Size(216, 28);
            this.lblOptInchKey.TabIndex = 12;
            this.lblOptInchKey.Text = "8인치 or 12인치";
            this.lblOptInchKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptInchVal
            // 
            this.lblOptInchVal.BackColor = System.Drawing.Color.White;
            this.lblOptInchVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptInchVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptInchVal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptInchVal.Location = new System.Drawing.Point(222, 194);
            this.lblOptInchVal.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptInchVal.Name = "lblOptInchVal";
            this.lblOptInchVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptInchVal.Size = new System.Drawing.Size(258, 28);
            this.lblOptInchVal.TabIndex = 13;
            this.lblOptInchVal.Text = "12인치";
            this.lblOptInchVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptStageKey
            // 
            this.lblOptStageKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblOptStageKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptStageKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptStageKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptStageKey.Location = new System.Drawing.Point(2, 226);
            this.lblOptStageKey.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptStageKey.Name = "lblOptStageKey";
            this.lblOptStageKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptStageKey.Size = new System.Drawing.Size(216, 28);
            this.lblOptStageKey.TabIndex = 14;
            this.lblOptStageKey.Text = "카세트 2단 활성화";
            this.lblOptStageKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblOptStageVal
            // 
            this.lblOptStageVal.BackColor = System.Drawing.Color.White;
            this.lblOptStageVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOptStageVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOptStageVal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOptStageVal.Location = new System.Drawing.Point(222, 226);
            this.lblOptStageVal.Margin = new System.Windows.Forms.Padding(2);
            this.lblOptStageVal.Name = "lblOptStageVal";
            this.lblOptStageVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblOptStageVal.Size = new System.Drawing.Size(258, 28);
            this.lblOptStageVal.TabIndex = 15;
            this.lblOptStageVal.Text = "1단";
            this.lblOptStageVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaitTitle
            // 
            this.lblWaitTitle.BackColor = System.Drawing.Color.Orange;
            this.lblWaitTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaitTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblWaitTitle.ForeColor = System.Drawing.Color.Black;
            this.lblWaitTitle.Location = new System.Drawing.Point(0, 282);
            this.lblWaitTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblWaitTitle.Name = "lblWaitTitle";
            this.lblWaitTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblWaitTitle.Size = new System.Drawing.Size(482, 26);
            this.lblWaitTitle.TabIndex = 2;
            this.lblWaitTitle.Text = "대기시간";
            this.lblWaitTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // waitRows
            // 
            this.waitRows.BackColor = System.Drawing.Color.WhiteSmoke;
            this.waitRows.ColumnCount = 2;
            this.waitRows.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.waitRows.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.waitRows.Controls.Add(this.lblWaitKey, 0, 0);
            this.waitRows.Controls.Add(this.lblWaitVal, 1, 0);
            this.waitRows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitRows.Location = new System.Drawing.Point(0, 308);
            this.waitRows.Margin = new System.Windows.Forms.Padding(0);
            this.waitRows.Name = "waitRows";
            this.waitRows.RowCount = 1;
            this.waitRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.waitRows.Size = new System.Drawing.Size(482, 32);
            this.waitRows.TabIndex = 3;
            // 
            // lblWaitKey
            // 
            this.lblWaitKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblWaitKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaitKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaitKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblWaitKey.Location = new System.Drawing.Point(2, 2);
            this.lblWaitKey.Margin = new System.Windows.Forms.Padding(2);
            this.lblWaitKey.Name = "lblWaitKey";
            this.lblWaitKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblWaitKey.Size = new System.Drawing.Size(216, 28);
            this.lblWaitKey.TabIndex = 0;
            this.lblWaitKey.Text = "이동 후 대기시간";
            this.lblWaitKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaitVal
            // 
            this.lblWaitVal.BackColor = System.Drawing.Color.White;
            this.lblWaitVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaitVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaitVal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblWaitVal.Location = new System.Drawing.Point(222, 2);
            this.lblWaitVal.Margin = new System.Windows.Forms.Padding(2);
            this.lblWaitVal.Name = "lblWaitVal";
            this.lblWaitVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblWaitVal.Size = new System.Drawing.Size(258, 28);
            this.lblWaitVal.TabIndex = 1;
            this.lblWaitVal.Text = "100 ms";
            this.lblWaitVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelRight
            // 
            this.panelRight.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelRight.ColumnCount = 2;
            this.panelRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.panelRight.Controls.Add(this.jogSection, 0, 0);
            this.panelRight.Controls.Add(this.speedSection, 1, 0);
            this.panelRight.Location = new System.Drawing.Point(738, 6);
            this.panelRight.Margin = new System.Windows.Forms.Padding(0);
            this.panelRight.Name = "panelRight";
            this.panelRight.RowCount = 1;
            this.panelRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelRight.Size = new System.Drawing.Size(266, 876);
            this.panelRight.TabIndex = 2;
            // 
            // jogSection
            // 
            this.jogSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.jogSection.ColumnCount = 1;
            this.jogSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogSection.Controls.Add(this.lblJogTitle, 0, 0);
            this.jogSection.Controls.Add(this.lblAxisName, 0, 1);
            this.jogSection.Controls.Add(this.btnJogPlus, 0, 2);
            this.jogSection.Controls.Add(this.btnJogStop, 0, 3);
            this.jogSection.Controls.Add(this.btnJogMinus, 0, 4);
            this.jogSection.Location = new System.Drawing.Point(0, 0);
            this.jogSection.Margin = new System.Windows.Forms.Padding(0);
            this.jogSection.Name = "jogSection";
            this.jogSection.RowCount = 6;
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogSection.Size = new System.Drawing.Size(174, 876);
            this.jogSection.TabIndex = 0;
            // 
            // lblJogTitle
            // 
            this.lblJogTitle.BackColor = System.Drawing.Color.Orange;
            this.lblJogTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJogTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblJogTitle.ForeColor = System.Drawing.Color.Black;
            this.lblJogTitle.Location = new System.Drawing.Point(0, 0);
            this.lblJogTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblJogTitle.Name = "lblJogTitle";
            this.lblJogTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblJogTitle.Size = new System.Drawing.Size(174, 26);
            this.lblJogTitle.TabIndex = 0;
            this.lblJogTitle.Text = "조그 운전";
            this.lblJogTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxisName
            // 
            this.lblAxisName.BackColor = System.Drawing.Color.Gainsboro;
            this.lblAxisName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisName.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblAxisName.Location = new System.Drawing.Point(8, 30);
            this.lblAxisName.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.lblAxisName.Name = "lblAxisName";
            this.lblAxisName.Size = new System.Drawing.Size(158, 24);
            this.lblAxisName.TabIndex = 1;
            this.lblAxisName.Text = "AXIS Z";
            this.lblAxisName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnJogPlus
            // 
            this.btnJogPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogPlus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogPlus.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnJogPlus.Location = new System.Drawing.Point(8, 62);
            this.btnJogPlus.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.btnJogPlus.Name = "btnJogPlus";
            this.btnJogPlus.Size = new System.Drawing.Size(158, 56);
            this.btnJogPlus.TabIndex = 2;
            this.btnJogPlus.Text = "JOG +";
            this.btnJogPlus.UseVisualStyleBackColor = true;
            this.btnJogPlus.Click += new System.EventHandler(this.btnJogPlus_Click);
            // 
            // btnJogStop
            // 
            this.btnJogStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogStop.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnJogStop.Location = new System.Drawing.Point(8, 126);
            this.btnJogStop.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.btnJogStop.Name = "btnJogStop";
            this.btnJogStop.Size = new System.Drawing.Size(158, 44);
            this.btnJogStop.TabIndex = 3;
            this.btnJogStop.Text = "STOP";
            this.btnJogStop.UseVisualStyleBackColor = true;
            this.btnJogStop.Click += new System.EventHandler(this.btnJogStop_Click);
            // 
            // btnJogMinus
            // 
            this.btnJogMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogMinus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogMinus.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.btnJogMinus.Location = new System.Drawing.Point(8, 178);
            this.btnJogMinus.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.btnJogMinus.Name = "btnJogMinus";
            this.btnJogMinus.Size = new System.Drawing.Size(158, 56);
            this.btnJogMinus.TabIndex = 4;
            this.btnJogMinus.Text = "JOG -";
            this.btnJogMinus.UseVisualStyleBackColor = true;
            this.btnJogMinus.Click += new System.EventHandler(this.btnJogMinus_Click);
            // 
            // speedSection
            // 
            this.speedSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.speedSection.ColumnCount = 2;
            this.speedSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.speedSection.Controls.Add(this.lblSpeedTitle, 0, 0);
            this.speedSection.Controls.Add(this.lblSpeedHigh, 1, 1);
            this.speedSection.Controls.Add(this.lblSpeedMid, 1, 2);
            this.speedSection.Controls.Add(this.lblSpeedLow, 1, 3);
            this.speedSection.Controls.Add(this.lblSpeedValue, 0, 4);
            this.speedSection.Controls.Add(this.trkSpeed, 0, 1);
            this.speedSection.Location = new System.Drawing.Point(174, 0);
            this.speedSection.Margin = new System.Windows.Forms.Padding(0);
            this.speedSection.Name = "speedSection";
            this.speedSection.RowCount = 5;
            this.speedSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.speedSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.speedSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.speedSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.speedSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.speedSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.speedSection.Size = new System.Drawing.Size(91, 876);
            this.speedSection.TabIndex = 1;
            // 
            // lblSpeedTitle
            // 
            this.lblSpeedTitle.BackColor = System.Drawing.Color.Orange;
            this.speedSection.SetColumnSpan(this.lblSpeedTitle, 2);
            this.lblSpeedTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpeedTitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpeedTitle.ForeColor = System.Drawing.Color.Black;
            this.lblSpeedTitle.Location = new System.Drawing.Point(0, 0);
            this.lblSpeedTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblSpeedTitle.Name = "lblSpeedTitle";
            this.lblSpeedTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblSpeedTitle.Size = new System.Drawing.Size(91, 26);
            this.lblSpeedTitle.TabIndex = 0;
            this.lblSpeedTitle.Text = "속도";
            this.lblSpeedTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // trkSpeed
            // 
            this.trkSpeed.Location = new System.Drawing.Point(8, 30);
            this.trkSpeed.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.trkSpeed.Maximum = 100;
            this.trkSpeed.Name = "trkSpeed";
            this.trkSpeed.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.speedSection.SetRowSpan(this.trkSpeed, 3);
            this.trkSpeed.Size = new System.Drawing.Size(35, 808);
            this.trkSpeed.TabIndex = 1;
            this.trkSpeed.TickFrequency = 10;
            this.trkSpeed.Value = 50;
            this.trkSpeed.ValueChanged += new System.EventHandler(this.trkSpeed_ValueChanged);
            // 
            // lblSpeedHigh
            // 
            this.lblSpeedHigh.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpeedHigh.Location = new System.Drawing.Point(51, 26);
            this.lblSpeedHigh.Margin = new System.Windows.Forms.Padding(0);
            this.lblSpeedHigh.Name = "lblSpeedHigh";
            this.lblSpeedHigh.Size = new System.Drawing.Size(40, 269);
            this.lblSpeedHigh.TabIndex = 2;
            this.lblSpeedHigh.Text = "H";
            this.lblSpeedHigh.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lblSpeedMid
            // 
            this.lblSpeedMid.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpeedMid.Location = new System.Drawing.Point(51, 295);
            this.lblSpeedMid.Margin = new System.Windows.Forms.Padding(0);
            this.lblSpeedMid.Name = "lblSpeedMid";
            this.lblSpeedMid.Size = new System.Drawing.Size(40, 278);
            this.lblSpeedMid.TabIndex = 3;
            this.lblSpeedMid.Text = "M";
            this.lblSpeedMid.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSpeedLow
            // 
            this.lblSpeedLow.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpeedLow.Location = new System.Drawing.Point(51, 573);
            this.lblSpeedLow.Margin = new System.Windows.Forms.Padding(0);
            this.lblSpeedLow.Name = "lblSpeedLow";
            this.lblSpeedLow.Size = new System.Drawing.Size(40, 269);
            this.lblSpeedLow.TabIndex = 4;
            this.lblSpeedLow.Text = "L";
            this.lblSpeedLow.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // lblSpeedValue
            // 
            this.lblSpeedValue.BackColor = System.Drawing.Color.White;
            this.lblSpeedValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.speedSection.SetColumnSpan(this.lblSpeedValue, 2);
            this.lblSpeedValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpeedValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblSpeedValue.Location = new System.Drawing.Point(2, 844);
            this.lblSpeedValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblSpeedValue.Name = "lblSpeedValue";
            this.lblSpeedValue.Size = new System.Drawing.Size(87, 30);
            this.lblSpeedValue.TabIndex = 5;
            this.lblSpeedValue.Text = "50 %";
            this.lblSpeedValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // unitConfigGrid
            // 
            this.unitConfigGrid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.unitConfigGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.unitConfigGrid.Location = new System.Drawing.Point(1012, 6);
            this.unitConfigGrid.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.unitConfigGrid.Name = "unitConfigGrid";
            this.unitConfigGrid.Size = new System.Drawing.Size(340, 876);
            this.unitConfigGrid.TabIndex = 3;
            this.unitConfigGrid.Title = "UNIT CONFIG";
            // 
            // CassetteRecipePage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.mainLayout);
            this.Name = "CassetteRecipePage";
            this.Size = new System.Drawing.Size(1360, 920);
            this.mainLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.panelLeft.ResumeLayout(false);
            this.actionSection.ResumeLayout(false);
            this.ioSection.ResumeLayout(false);
            this.ioSensor1Row.ResumeLayout(false);
            this.ioSensor2Row.ResumeLayout(false);
            this.ioProtrusionRow.ResumeLayout(false);
            this.ioMappingRow.ResumeLayout(false);
            this.panelCenter.ResumeLayout(false);
            this.optionRows.ResumeLayout(false);
            this.waitRows.ResumeLayout(false);
            this.panelRight.ResumeLayout(false);
            this.jogSection.ResumeLayout(false);
            this.speedSection.ResumeLayout(false);
            this.speedSection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).EndInit();
            this.ResumeLayout(false);

        }

        private void InitOptionLabel(System.Windows.Forms.Label label, string name, string text, bool isKey)
        {
            label.Name = name;
            label.Text = text;
            label.Dock = System.Windows.Forms.DockStyle.Fill;
            label.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            label.Font = new System.Drawing.Font("맑은 고딕", 9F);
            label.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            if (isKey)
            {
                label.BackColor = System.Drawing.Color.Gainsboro;
                label.ForeColor = System.Drawing.Color.Black;
                label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }
            else
            {
                label.BackColor = System.Drawing.Color.White;
                label.ForeColor = System.Drawing.Color.Black;
                label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }
        }
    }
}