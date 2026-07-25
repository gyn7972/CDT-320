namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ColletCleaningControlDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.GroupBox grpTarget;
        private System.Windows.Forms.CheckBox chkFront4;
        private System.Windows.Forms.CheckBox chkFront3;
        private System.Windows.Forms.CheckBox chkFront2;
        private System.Windows.Forms.CheckBox chkFront1;
        private System.Windows.Forms.CheckBox chkRear4;
        private System.Windows.Forms.CheckBox chkRear3;
        private System.Windows.Forms.CheckBox chkRear2;
        private System.Windows.Forms.CheckBox chkRear1;
        private System.Windows.Forms.Label lblFront;
        private System.Windows.Forms.Label lblRear;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnSelectNone;

        private System.Windows.Forms.GroupBox grpMotion;
        private System.Windows.Forms.Label lblCleanVelocity;
        private System.Windows.Forms.NumericUpDown numCleanVelocity;
        private System.Windows.Forms.Label lblCleanAcceleration;
        private System.Windows.Forms.NumericUpDown numCleanAcceleration;
        private System.Windows.Forms.Label lblCleanDeceleration;
        private System.Windows.Forms.NumericUpDown numCleanDeceleration;
        private System.Windows.Forms.Label lblContactZUserOffset;
        private System.Windows.Forms.NumericUpDown numContactZUserOffset;
        private System.Windows.Forms.Label lblMaxExtraPressDepth;
        private System.Windows.Forms.NumericUpDown numMaxExtraPressDepth;
        private System.Windows.Forms.Label lblArriveDwellMs;
        private System.Windows.Forms.NumericUpDown numArriveDwellMs;
        private System.Windows.Forms.Label lblCleanPressCount;
        private System.Windows.Forms.NumericUpDown numCleanPressCount;
        private System.Windows.Forms.Label lblRepeatLiftHeight;
        private System.Windows.Forms.NumericUpDown numRepeatLiftHeight;

        private System.Windows.Forms.GroupBox grpHeight;
        private System.Windows.Forms.Label lblDieHeight;
        private System.Windows.Forms.NumericUpDown numDieHeight;
        private System.Windows.Forms.Label lblRimHeight;
        private System.Windows.Forms.NumericUpDown numRimHeight;
        private System.Windows.Forms.Label lblFilmHeight;
        private System.Windows.Forms.NumericUpDown numFilmHeight;
        private System.Windows.Forms.Label lblMaxRetryCount;
        private System.Windows.Forms.NumericUpDown numMaxRetryCount;
        private System.Windows.Forms.CheckBox chkAllowPlaceOnCleanedCell;
        private System.Windows.Forms.CheckBox chkDisablePickerOnReplaceAlarm;

        private System.Windows.Forms.GroupBox grpTrigger;
        private System.Windows.Forms.CheckBox chkUseTriggerOnWaferExchange;
        private System.Windows.Forms.Label lblWaferExchangeInterval;
        private System.Windows.Forms.NumericUpDown numWaferExchangeInterval;
        private System.Windows.Forms.CheckBox chkUseTriggerOnProcessCount;
        private System.Windows.Forms.Label lblProcessCountInterval;
        private System.Windows.Forms.NumericUpDown numProcessCountInterval;
        private System.Windows.Forms.ComboBox cmbProcessCountUnit;
        private System.Windows.Forms.CheckBox chkUseTriggerOnAutoStart;

        private System.Windows.Forms.GroupBox grpHistory;
        private System.Windows.Forms.ListView lstHistory;
        private System.Windows.Forms.ColumnHeader colHistorySide;
        private System.Windows.Forms.ColumnHeader colHistoryCollet;
        private System.Windows.Forms.ColumnHeader colHistoryLastAt;
        private System.Windows.Forms.ColumnHeader colHistoryCount;
        private System.Windows.Forms.ColumnHeader colHistoryResult;

        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.ListBox lstLog;

        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.grpTarget = new System.Windows.Forms.GroupBox();
            this.lblFront = new System.Windows.Forms.Label();
            this.chkFront4 = new System.Windows.Forms.CheckBox();
            this.chkFront3 = new System.Windows.Forms.CheckBox();
            this.chkFront2 = new System.Windows.Forms.CheckBox();
            this.chkFront1 = new System.Windows.Forms.CheckBox();
            this.lblRear = new System.Windows.Forms.Label();
            this.chkRear4 = new System.Windows.Forms.CheckBox();
            this.chkRear3 = new System.Windows.Forms.CheckBox();
            this.chkRear2 = new System.Windows.Forms.CheckBox();
            this.chkRear1 = new System.Windows.Forms.CheckBox();
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.btnSelectNone = new System.Windows.Forms.Button();
            this.grpMotion = new System.Windows.Forms.GroupBox();
            this.lblCleanVelocity = new System.Windows.Forms.Label();
            this.numCleanVelocity = new System.Windows.Forms.NumericUpDown();
            this.lblCleanAcceleration = new System.Windows.Forms.Label();
            this.numCleanAcceleration = new System.Windows.Forms.NumericUpDown();
            this.lblCleanDeceleration = new System.Windows.Forms.Label();
            this.numCleanDeceleration = new System.Windows.Forms.NumericUpDown();
            this.lblContactZUserOffset = new System.Windows.Forms.Label();
            this.numContactZUserOffset = new System.Windows.Forms.NumericUpDown();
            this.lblMaxExtraPressDepth = new System.Windows.Forms.Label();
            this.numMaxExtraPressDepth = new System.Windows.Forms.NumericUpDown();
            this.lblArriveDwellMs = new System.Windows.Forms.Label();
            this.numArriveDwellMs = new System.Windows.Forms.NumericUpDown();
            this.lblCleanPressCount = new System.Windows.Forms.Label();
            this.numCleanPressCount = new System.Windows.Forms.NumericUpDown();
            this.lblRepeatLiftHeight = new System.Windows.Forms.Label();
            this.numRepeatLiftHeight = new System.Windows.Forms.NumericUpDown();
            this.grpHeight = new System.Windows.Forms.GroupBox();
            this.lblDieHeight = new System.Windows.Forms.Label();
            this.numDieHeight = new System.Windows.Forms.NumericUpDown();
            this.lblRimHeight = new System.Windows.Forms.Label();
            this.numRimHeight = new System.Windows.Forms.NumericUpDown();
            this.lblFilmHeight = new System.Windows.Forms.Label();
            this.numFilmHeight = new System.Windows.Forms.NumericUpDown();
            this.lblMaxRetryCount = new System.Windows.Forms.Label();
            this.numMaxRetryCount = new System.Windows.Forms.NumericUpDown();
            this.chkAllowPlaceOnCleanedCell = new System.Windows.Forms.CheckBox();
            this.chkDisablePickerOnReplaceAlarm = new System.Windows.Forms.CheckBox();
            this.grpTrigger = new System.Windows.Forms.GroupBox();
            this.chkUseTriggerOnWaferExchange = new System.Windows.Forms.CheckBox();
            this.lblWaferExchangeInterval = new System.Windows.Forms.Label();
            this.numWaferExchangeInterval = new System.Windows.Forms.NumericUpDown();
            this.chkUseTriggerOnProcessCount = new System.Windows.Forms.CheckBox();
            this.lblProcessCountInterval = new System.Windows.Forms.Label();
            this.numProcessCountInterval = new System.Windows.Forms.NumericUpDown();
            this.cmbProcessCountUnit = new System.Windows.Forms.ComboBox();
            this.chkUseTriggerOnAutoStart = new System.Windows.Forms.CheckBox();
            this.grpHistory = new System.Windows.Forms.GroupBox();
            this.lstHistory = new System.Windows.Forms.ListView();
            this.colHistorySide = new System.Windows.Forms.ColumnHeader();
            this.colHistoryCollet = new System.Windows.Forms.ColumnHeader();
            this.colHistoryLastAt = new System.Windows.Forms.ColumnHeader();
            this.colHistoryCount = new System.Windows.Forms.ColumnHeader();
            this.colHistoryResult = new System.Windows.Forms.ColumnHeader();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.grpTarget.SuspendLayout();
            this.grpMotion.SuspendLayout();
            this.grpHeight.SuspendLayout();
            this.grpTrigger.SuspendLayout();
            this.grpHistory.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCleanVelocity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCleanAcceleration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCleanDeceleration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numContactZUserOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxExtraPressDepth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numArriveDwellMs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCleanPressCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRepeatLiftHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDieHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRimHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFilmHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxRetryCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWaferExchangeInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProcessCountInterval)).BeginInit();
            this.SuspendLayout();
            //
            // titleLabel
            //
            this.titleLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.titleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleLabel.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(0, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.titleLabel.Size = new System.Drawing.Size(1004, 40);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "COLLET CLEANING";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grpTarget
            //
            this.grpTarget.Controls.Add(this.btnSelectNone);
            this.grpTarget.Controls.Add(this.btnSelectAll);
            this.grpTarget.Controls.Add(this.chkRear1);
            this.grpTarget.Controls.Add(this.chkRear2);
            this.grpTarget.Controls.Add(this.chkRear3);
            this.grpTarget.Controls.Add(this.chkRear4);
            this.grpTarget.Controls.Add(this.lblRear);
            this.grpTarget.Controls.Add(this.chkFront1);
            this.grpTarget.Controls.Add(this.chkFront2);
            this.grpTarget.Controls.Add(this.chkFront3);
            this.grpTarget.Controls.Add(this.chkFront4);
            this.grpTarget.Controls.Add(this.lblFront);
            this.grpTarget.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpTarget.Location = new System.Drawing.Point(14, 50);
            this.grpTarget.Name = "grpTarget";
            this.grpTarget.Size = new System.Drawing.Size(470, 96);
            this.grpTarget.TabIndex = 1;
            this.grpTarget.TabStop = false;
            this.grpTarget.Text = "대상 콜렛 (4 → 3 → 2 → 1 순서로 실행)";
            //
            // lblFront
            //
            this.lblFront.Location = new System.Drawing.Point(14, 26);
            this.lblFront.Name = "lblFront";
            this.lblFront.Size = new System.Drawing.Size(70, 24);
            this.lblFront.TabIndex = 0;
            this.lblFront.Text = "FRONT";
            this.lblFront.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // chkFront4
            //
            this.chkFront4.Location = new System.Drawing.Point(88, 26);
            this.chkFront4.Name = "chkFront4";
            this.chkFront4.Size = new System.Drawing.Size(56, 24);
            this.chkFront4.TabIndex = 1;
            this.chkFront4.Text = "4";
            this.chkFront4.UseVisualStyleBackColor = true;
            //
            // chkFront3
            //
            this.chkFront3.Location = new System.Drawing.Point(150, 26);
            this.chkFront3.Name = "chkFront3";
            this.chkFront3.Size = new System.Drawing.Size(56, 24);
            this.chkFront3.TabIndex = 2;
            this.chkFront3.Text = "3";
            this.chkFront3.UseVisualStyleBackColor = true;
            //
            // chkFront2
            //
            this.chkFront2.Location = new System.Drawing.Point(212, 26);
            this.chkFront2.Name = "chkFront2";
            this.chkFront2.Size = new System.Drawing.Size(56, 24);
            this.chkFront2.TabIndex = 3;
            this.chkFront2.Text = "2";
            this.chkFront2.UseVisualStyleBackColor = true;
            //
            // chkFront1
            //
            this.chkFront1.Location = new System.Drawing.Point(274, 26);
            this.chkFront1.Name = "chkFront1";
            this.chkFront1.Size = new System.Drawing.Size(56, 24);
            this.chkFront1.TabIndex = 4;
            this.chkFront1.Text = "1";
            this.chkFront1.UseVisualStyleBackColor = true;
            //
            // lblRear
            //
            this.lblRear.Location = new System.Drawing.Point(14, 58);
            this.lblRear.Name = "lblRear";
            this.lblRear.Size = new System.Drawing.Size(70, 24);
            this.lblRear.TabIndex = 5;
            this.lblRear.Text = "REAR";
            this.lblRear.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // chkRear4
            //
            this.chkRear4.Location = new System.Drawing.Point(88, 58);
            this.chkRear4.Name = "chkRear4";
            this.chkRear4.Size = new System.Drawing.Size(56, 24);
            this.chkRear4.TabIndex = 6;
            this.chkRear4.Text = "4";
            this.chkRear4.UseVisualStyleBackColor = true;
            //
            // chkRear3
            //
            this.chkRear3.Location = new System.Drawing.Point(150, 58);
            this.chkRear3.Name = "chkRear3";
            this.chkRear3.Size = new System.Drawing.Size(56, 24);
            this.chkRear3.TabIndex = 7;
            this.chkRear3.Text = "3";
            this.chkRear3.UseVisualStyleBackColor = true;
            //
            // chkRear2
            //
            this.chkRear2.Location = new System.Drawing.Point(212, 58);
            this.chkRear2.Name = "chkRear2";
            this.chkRear2.Size = new System.Drawing.Size(56, 24);
            this.chkRear2.TabIndex = 8;
            this.chkRear2.Text = "2";
            this.chkRear2.UseVisualStyleBackColor = true;
            //
            // chkRear1
            //
            this.chkRear1.Location = new System.Drawing.Point(274, 58);
            this.chkRear1.Name = "chkRear1";
            this.chkRear1.Size = new System.Drawing.Size(56, 24);
            this.chkRear1.TabIndex = 9;
            this.chkRear1.Text = "1";
            this.chkRear1.UseVisualStyleBackColor = true;
            //
            // btnSelectAll
            //
            this.btnSelectAll.Location = new System.Drawing.Point(346, 24);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(108, 28);
            this.btnSelectAll.TabIndex = 10;
            this.btnSelectAll.Text = "전체 선택";
            this.btnSelectAll.UseVisualStyleBackColor = true;
            //
            // btnSelectNone
            //
            this.btnSelectNone.Location = new System.Drawing.Point(346, 56);
            this.btnSelectNone.Name = "btnSelectNone";
            this.btnSelectNone.Size = new System.Drawing.Size(108, 28);
            this.btnSelectNone.TabIndex = 11;
            this.btnSelectNone.Text = "전체 해제";
            this.btnSelectNone.UseVisualStyleBackColor = true;
            //
            // grpMotion
            //
            this.grpMotion.Controls.Add(this.numRepeatLiftHeight);
            this.grpMotion.Controls.Add(this.lblRepeatLiftHeight);
            this.grpMotion.Controls.Add(this.numCleanPressCount);
            this.grpMotion.Controls.Add(this.lblCleanPressCount);
            this.grpMotion.Controls.Add(this.numArriveDwellMs);
            this.grpMotion.Controls.Add(this.lblArriveDwellMs);
            this.grpMotion.Controls.Add(this.numMaxExtraPressDepth);
            this.grpMotion.Controls.Add(this.lblMaxExtraPressDepth);
            this.grpMotion.Controls.Add(this.numContactZUserOffset);
            this.grpMotion.Controls.Add(this.lblContactZUserOffset);
            this.grpMotion.Controls.Add(this.numCleanDeceleration);
            this.grpMotion.Controls.Add(this.lblCleanDeceleration);
            this.grpMotion.Controls.Add(this.numCleanAcceleration);
            this.grpMotion.Controls.Add(this.lblCleanAcceleration);
            this.grpMotion.Controls.Add(this.numCleanVelocity);
            this.grpMotion.Controls.Add(this.lblCleanVelocity);
            this.grpMotion.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpMotion.Location = new System.Drawing.Point(14, 152);
            this.grpMotion.Name = "grpMotion";
            this.grpMotion.Size = new System.Drawing.Size(470, 180);
            this.grpMotion.TabIndex = 2;
            this.grpMotion.TabStop = false;
            this.grpMotion.Text = "클리닝 동작 조건";
            //
            // lblCleanVelocity
            //
            this.lblCleanVelocity.Location = new System.Drawing.Point(14, 26);
            this.lblCleanVelocity.Name = "lblCleanVelocity";
            this.lblCleanVelocity.Size = new System.Drawing.Size(150, 22);
            this.lblCleanVelocity.TabIndex = 0;
            this.lblCleanVelocity.Text = "Z 속도";
            this.lblCleanVelocity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numCleanVelocity
            //
            this.numCleanVelocity.DecimalPlaces = 3;
            this.numCleanVelocity.Location = new System.Drawing.Point(168, 24);
            this.numCleanVelocity.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numCleanVelocity.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numCleanVelocity.Name = "numCleanVelocity";
            this.numCleanVelocity.Size = new System.Drawing.Size(100, 25);
            this.numCleanVelocity.TabIndex = 1;
            this.numCleanVelocity.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numCleanVelocity.Value = new decimal(new int[] { 5, 0, 0, 0 });
            //
            // lblCleanAcceleration
            //
            this.lblCleanAcceleration.Location = new System.Drawing.Point(14, 56);
            this.lblCleanAcceleration.Name = "lblCleanAcceleration";
            this.lblCleanAcceleration.Size = new System.Drawing.Size(150, 22);
            this.lblCleanAcceleration.TabIndex = 2;
            this.lblCleanAcceleration.Text = "Z 가속";
            this.lblCleanAcceleration.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numCleanAcceleration
            //
            this.numCleanAcceleration.DecimalPlaces = 3;
            this.numCleanAcceleration.Location = new System.Drawing.Point(168, 54);
            this.numCleanAcceleration.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numCleanAcceleration.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numCleanAcceleration.Name = "numCleanAcceleration";
            this.numCleanAcceleration.Size = new System.Drawing.Size(100, 25);
            this.numCleanAcceleration.TabIndex = 3;
            this.numCleanAcceleration.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numCleanAcceleration.Value = new decimal(new int[] { 50, 0, 0, 0 });
            //
            // lblCleanDeceleration
            //
            this.lblCleanDeceleration.Location = new System.Drawing.Point(14, 86);
            this.lblCleanDeceleration.Name = "lblCleanDeceleration";
            this.lblCleanDeceleration.Size = new System.Drawing.Size(150, 22);
            this.lblCleanDeceleration.TabIndex = 4;
            this.lblCleanDeceleration.Text = "Z 감속";
            this.lblCleanDeceleration.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numCleanDeceleration
            //
            this.numCleanDeceleration.DecimalPlaces = 3;
            this.numCleanDeceleration.Location = new System.Drawing.Point(168, 84);
            this.numCleanDeceleration.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numCleanDeceleration.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numCleanDeceleration.Name = "numCleanDeceleration";
            this.numCleanDeceleration.Size = new System.Drawing.Size(100, 25);
            this.numCleanDeceleration.TabIndex = 5;
            this.numCleanDeceleration.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numCleanDeceleration.Value = new decimal(new int[] { 50, 0, 0, 0 });
            //
            // lblContactZUserOffset
            //
            this.lblContactZUserOffset.Location = new System.Drawing.Point(14, 116);
            this.lblContactZUserOffset.Name = "lblContactZUserOffset";
            this.lblContactZUserOffset.Size = new System.Drawing.Size(150, 22);
            this.lblContactZUserOffset.TabIndex = 6;
            this.lblContactZUserOffset.Text = "접촉 Z 보정 (+상승/-하강)";
            this.lblContactZUserOffset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numContactZUserOffset
            //
            this.numContactZUserOffset.DecimalPlaces = 4;
            this.numContactZUserOffset.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numContactZUserOffset.Location = new System.Drawing.Point(168, 114);
            this.numContactZUserOffset.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numContactZUserOffset.Minimum = new decimal(new int[] { 100, 0, 0, -2147483648 });
            this.numContactZUserOffset.Name = "numContactZUserOffset";
            this.numContactZUserOffset.Size = new System.Drawing.Size(100, 25);
            this.numContactZUserOffset.TabIndex = 7;
            this.numContactZUserOffset.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblMaxExtraPressDepth
            //
            this.lblMaxExtraPressDepth.Location = new System.Drawing.Point(280, 116);
            this.lblMaxExtraPressDepth.Name = "lblMaxExtraPressDepth";
            this.lblMaxExtraPressDepth.Size = new System.Drawing.Size(108, 22);
            this.lblMaxExtraPressDepth.TabIndex = 8;
            this.lblMaxExtraPressDepth.Text = "과압 한계";
            this.lblMaxExtraPressDepth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numMaxExtraPressDepth
            //
            this.numMaxExtraPressDepth.DecimalPlaces = 4;
            this.numMaxExtraPressDepth.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numMaxExtraPressDepth.Location = new System.Drawing.Point(390, 114);
            this.numMaxExtraPressDepth.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numMaxExtraPressDepth.Name = "numMaxExtraPressDepth";
            this.numMaxExtraPressDepth.Size = new System.Drawing.Size(64, 25);
            this.numMaxExtraPressDepth.TabIndex = 9;
            this.numMaxExtraPressDepth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblArriveDwellMs
            //
            this.lblArriveDwellMs.Location = new System.Drawing.Point(280, 26);
            this.lblArriveDwellMs.Name = "lblArriveDwellMs";
            this.lblArriveDwellMs.Size = new System.Drawing.Size(108, 22);
            this.lblArriveDwellMs.TabIndex = 10;
            this.lblArriveDwellMs.Text = "도착 대기 (ms)";
            this.lblArriveDwellMs.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numArriveDwellMs
            //
            this.numArriveDwellMs.Location = new System.Drawing.Point(390, 24);
            this.numArriveDwellMs.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numArriveDwellMs.Name = "numArriveDwellMs";
            this.numArriveDwellMs.Size = new System.Drawing.Size(64, 25);
            this.numArriveDwellMs.TabIndex = 11;
            this.numArriveDwellMs.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblCleanPressCount
            //
            this.lblCleanPressCount.Location = new System.Drawing.Point(280, 56);
            this.lblCleanPressCount.Name = "lblCleanPressCount";
            this.lblCleanPressCount.Size = new System.Drawing.Size(108, 22);
            this.lblCleanPressCount.TabIndex = 12;
            this.lblCleanPressCount.Text = "클린 횟수";
            this.lblCleanPressCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numCleanPressCount
            //
            this.numCleanPressCount.Location = new System.Drawing.Point(390, 54);
            this.numCleanPressCount.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            this.numCleanPressCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numCleanPressCount.Name = "numCleanPressCount";
            this.numCleanPressCount.Size = new System.Drawing.Size(64, 25);
            this.numCleanPressCount.TabIndex = 13;
            this.numCleanPressCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numCleanPressCount.Value = new decimal(new int[] { 3, 0, 0, 0 });
            //
            // lblRepeatLiftHeight
            //
            this.lblRepeatLiftHeight.Location = new System.Drawing.Point(280, 86);
            this.lblRepeatLiftHeight.Name = "lblRepeatLiftHeight";
            this.lblRepeatLiftHeight.Size = new System.Drawing.Size(108, 22);
            this.lblRepeatLiftHeight.TabIndex = 14;
            this.lblRepeatLiftHeight.Text = "반복 상승 높이";
            this.lblRepeatLiftHeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numRepeatLiftHeight
            //
            this.numRepeatLiftHeight.DecimalPlaces = 4;
            this.numRepeatLiftHeight.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numRepeatLiftHeight.Location = new System.Drawing.Point(390, 84);
            this.numRepeatLiftHeight.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numRepeatLiftHeight.Minimum = new decimal(new int[] { 1, 0, 0, 262144 });
            this.numRepeatLiftHeight.Name = "numRepeatLiftHeight";
            this.numRepeatLiftHeight.Size = new System.Drawing.Size(64, 25);
            this.numRepeatLiftHeight.TabIndex = 15;
            this.numRepeatLiftHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numRepeatLiftHeight.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // grpHeight
            //
            this.grpHeight.Controls.Add(this.chkDisablePickerOnReplaceAlarm);
            this.grpHeight.Controls.Add(this.chkAllowPlaceOnCleanedCell);
            this.grpHeight.Controls.Add(this.numMaxRetryCount);
            this.grpHeight.Controls.Add(this.lblMaxRetryCount);
            this.grpHeight.Controls.Add(this.numFilmHeight);
            this.grpHeight.Controls.Add(this.lblFilmHeight);
            this.grpHeight.Controls.Add(this.numRimHeight);
            this.grpHeight.Controls.Add(this.lblRimHeight);
            this.grpHeight.Controls.Add(this.numDieHeight);
            this.grpHeight.Controls.Add(this.lblDieHeight);
            this.grpHeight.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpHeight.Location = new System.Drawing.Point(14, 338);
            this.grpHeight.Name = "grpHeight";
            this.grpHeight.Size = new System.Drawing.Size(470, 180);
            this.grpHeight.TabIndex = 3;
            this.grpHeight.TabStop = false;
            this.grpHeight.Text = "티칭 제외 높이 / 판정 (접촉Z = Place티칭Z - 다이 - 림 - 필름 + 보정)";
            //
            // lblDieHeight
            //
            this.lblDieHeight.Location = new System.Drawing.Point(14, 26);
            this.lblDieHeight.Name = "lblDieHeight";
            this.lblDieHeight.Size = new System.Drawing.Size(150, 22);
            this.lblDieHeight.TabIndex = 0;
            this.lblDieHeight.Text = "다이 높이";
            this.lblDieHeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numDieHeight
            //
            this.numDieHeight.DecimalPlaces = 4;
            this.numDieHeight.Increment = new decimal(new int[] { 1, 0, 0, 262144 });
            this.numDieHeight.Location = new System.Drawing.Point(168, 24);
            this.numDieHeight.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numDieHeight.Name = "numDieHeight";
            this.numDieHeight.Size = new System.Drawing.Size(100, 25);
            this.numDieHeight.TabIndex = 1;
            this.numDieHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblRimHeight
            //
            this.lblRimHeight.Location = new System.Drawing.Point(14, 56);
            this.lblRimHeight.Name = "lblRimHeight";
            this.lblRimHeight.Size = new System.Drawing.Size(150, 22);
            this.lblRimHeight.TabIndex = 2;
            this.lblRimHeight.Text = "림 높이";
            this.lblRimHeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numRimHeight
            //
            this.numRimHeight.DecimalPlaces = 4;
            this.numRimHeight.Increment = new decimal(new int[] { 1, 0, 0, 262144 });
            this.numRimHeight.Location = new System.Drawing.Point(168, 54);
            this.numRimHeight.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numRimHeight.Name = "numRimHeight";
            this.numRimHeight.Size = new System.Drawing.Size(100, 25);
            this.numRimHeight.TabIndex = 3;
            this.numRimHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblFilmHeight
            //
            this.lblFilmHeight.Location = new System.Drawing.Point(14, 86);
            this.lblFilmHeight.Name = "lblFilmHeight";
            this.lblFilmHeight.Size = new System.Drawing.Size(150, 22);
            this.lblFilmHeight.TabIndex = 4;
            this.lblFilmHeight.Text = "필름 높이";
            this.lblFilmHeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numFilmHeight
            //
            this.numFilmHeight.DecimalPlaces = 4;
            this.numFilmHeight.Increment = new decimal(new int[] { 1, 0, 0, 262144 });
            this.numFilmHeight.Location = new System.Drawing.Point(168, 84);
            this.numFilmHeight.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numFilmHeight.Name = "numFilmHeight";
            this.numFilmHeight.Size = new System.Drawing.Size(100, 25);
            this.numFilmHeight.TabIndex = 5;
            this.numFilmHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            //
            // lblMaxRetryCount
            //
            this.lblMaxRetryCount.Location = new System.Drawing.Point(14, 116);
            this.lblMaxRetryCount.Name = "lblMaxRetryCount";
            this.lblMaxRetryCount.Size = new System.Drawing.Size(150, 22);
            this.lblMaxRetryCount.TabIndex = 6;
            this.lblMaxRetryCount.Text = "검사 NG 재시도 횟수";
            this.lblMaxRetryCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numMaxRetryCount
            //
            this.numMaxRetryCount.Location = new System.Drawing.Point(168, 114);
            this.numMaxRetryCount.Maximum = new decimal(new int[] { 99, 0, 0, 0 });
            this.numMaxRetryCount.Name = "numMaxRetryCount";
            this.numMaxRetryCount.Size = new System.Drawing.Size(100, 25);
            this.numMaxRetryCount.TabIndex = 7;
            this.numMaxRetryCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numMaxRetryCount.Value = new decimal(new int[] { 2, 0, 0, 0 });
            //
            // chkAllowPlaceOnCleanedCell
            //
            this.chkAllowPlaceOnCleanedCell.Location = new System.Drawing.Point(280, 26);
            this.chkAllowPlaceOnCleanedCell.Name = "chkAllowPlaceOnCleanedCell";
            this.chkAllowPlaceOnCleanedCell.Size = new System.Drawing.Size(180, 44);
            this.chkAllowPlaceOnCleanedCell.TabIndex = 8;
            this.chkAllowPlaceOnCleanedCell.Text = "클린한 셀에 NG die 배치 허용";
            this.chkAllowPlaceOnCleanedCell.UseVisualStyleBackColor = true;
            //
            // chkDisablePickerOnReplaceAlarm
            //
            this.chkDisablePickerOnReplaceAlarm.Location = new System.Drawing.Point(280, 74);
            this.chkDisablePickerOnReplaceAlarm.Name = "chkDisablePickerOnReplaceAlarm";
            this.chkDisablePickerOnReplaceAlarm.Size = new System.Drawing.Size(180, 44);
            this.chkDisablePickerOnReplaceAlarm.TabIndex = 9;
            this.chkDisablePickerOnReplaceAlarm.Text = "교체 알람 시 해당 Picker만 제외";
            this.chkDisablePickerOnReplaceAlarm.UseVisualStyleBackColor = true;
            //
            // grpTrigger
            //
            this.grpTrigger.Controls.Add(this.chkUseTriggerOnAutoStart);
            this.grpTrigger.Controls.Add(this.cmbProcessCountUnit);
            this.grpTrigger.Controls.Add(this.numProcessCountInterval);
            this.grpTrigger.Controls.Add(this.lblProcessCountInterval);
            this.grpTrigger.Controls.Add(this.chkUseTriggerOnProcessCount);
            this.grpTrigger.Controls.Add(this.numWaferExchangeInterval);
            this.grpTrigger.Controls.Add(this.lblWaferExchangeInterval);
            this.grpTrigger.Controls.Add(this.chkUseTriggerOnWaferExchange);
            this.grpTrigger.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpTrigger.Location = new System.Drawing.Point(500, 50);
            this.grpTrigger.Name = "grpTrigger";
            this.grpTrigger.Size = new System.Drawing.Size(490, 140);
            this.grpTrigger.TabIndex = 4;
            this.grpTrigger.TabStop = false;
            this.grpTrigger.Text = "자동 실행 조건 (각 항목 독립 사용)";
            //
            // chkUseTriggerOnWaferExchange
            //
            this.chkUseTriggerOnWaferExchange.Location = new System.Drawing.Point(14, 26);
            this.chkUseTriggerOnWaferExchange.Name = "chkUseTriggerOnWaferExchange";
            this.chkUseTriggerOnWaferExchange.Size = new System.Drawing.Size(190, 24);
            this.chkUseTriggerOnWaferExchange.TabIndex = 0;
            this.chkUseTriggerOnWaferExchange.Text = "웨이퍼 교체 시";
            this.chkUseTriggerOnWaferExchange.UseVisualStyleBackColor = true;
            //
            // lblWaferExchangeInterval
            //
            this.lblWaferExchangeInterval.Location = new System.Drawing.Point(214, 26);
            this.lblWaferExchangeInterval.Name = "lblWaferExchangeInterval";
            this.lblWaferExchangeInterval.Size = new System.Drawing.Size(120, 24);
            this.lblWaferExchangeInterval.TabIndex = 1;
            this.lblWaferExchangeInterval.Text = "교체 n회마다";
            this.lblWaferExchangeInterval.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numWaferExchangeInterval
            //
            this.numWaferExchangeInterval.Location = new System.Drawing.Point(340, 24);
            this.numWaferExchangeInterval.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            this.numWaferExchangeInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numWaferExchangeInterval.Name = "numWaferExchangeInterval";
            this.numWaferExchangeInterval.Size = new System.Drawing.Size(90, 25);
            this.numWaferExchangeInterval.TabIndex = 2;
            this.numWaferExchangeInterval.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numWaferExchangeInterval.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // chkUseTriggerOnProcessCount
            //
            this.chkUseTriggerOnProcessCount.Location = new System.Drawing.Point(14, 60);
            this.chkUseTriggerOnProcessCount.Name = "chkUseTriggerOnProcessCount";
            this.chkUseTriggerOnProcessCount.Size = new System.Drawing.Size(190, 24);
            this.chkUseTriggerOnProcessCount.TabIndex = 3;
            this.chkUseTriggerOnProcessCount.Text = "공정 횟수";
            this.chkUseTriggerOnProcessCount.UseVisualStyleBackColor = true;
            //
            // lblProcessCountInterval
            //
            this.lblProcessCountInterval.Location = new System.Drawing.Point(214, 60);
            this.lblProcessCountInterval.Name = "lblProcessCountInterval";
            this.lblProcessCountInterval.Size = new System.Drawing.Size(120, 24);
            this.lblProcessCountInterval.TabIndex = 4;
            this.lblProcessCountInterval.Text = "n개마다";
            this.lblProcessCountInterval.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numProcessCountInterval
            //
            this.numProcessCountInterval.Location = new System.Drawing.Point(340, 58);
            this.numProcessCountInterval.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numProcessCountInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numProcessCountInterval.Name = "numProcessCountInterval";
            this.numProcessCountInterval.Size = new System.Drawing.Size(90, 25);
            this.numProcessCountInterval.TabIndex = 5;
            this.numProcessCountInterval.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numProcessCountInterval.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            //
            // cmbProcessCountUnit
            //
            this.cmbProcessCountUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProcessCountUnit.FormattingEnabled = true;
            this.cmbProcessCountUnit.Items.AddRange(new object[] { "Die", "Wafer" });
            this.cmbProcessCountUnit.Location = new System.Drawing.Point(436, 58);
            this.cmbProcessCountUnit.Name = "cmbProcessCountUnit";
            this.cmbProcessCountUnit.Size = new System.Drawing.Size(44, 25);
            this.cmbProcessCountUnit.TabIndex = 6;
            //
            // chkUseTriggerOnAutoStart
            //
            this.chkUseTriggerOnAutoStart.Location = new System.Drawing.Point(14, 94);
            this.chkUseTriggerOnAutoStart.Name = "chkUseTriggerOnAutoStart";
            this.chkUseTriggerOnAutoStart.Size = new System.Drawing.Size(300, 24);
            this.chkUseTriggerOnAutoStart.TabIndex = 7;
            this.chkUseTriggerOnAutoStart.Text = "Auto 시작 시 (Ready 후 첫 Pick 전)";
            this.chkUseTriggerOnAutoStart.UseVisualStyleBackColor = true;
            //
            // grpHistory
            //
            this.grpHistory.Controls.Add(this.lstHistory);
            this.grpHistory.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpHistory.Location = new System.Drawing.Point(500, 196);
            this.grpHistory.Name = "grpHistory";
            this.grpHistory.Size = new System.Drawing.Size(490, 152);
            this.grpHistory.TabIndex = 5;
            this.grpHistory.TabStop = false;
            this.grpHistory.Text = "콜렛 클리닝 이력";
            //
            // lstHistory
            //
            this.lstHistory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colHistorySide,
            this.colHistoryCollet,
            this.colHistoryLastAt,
            this.colHistoryCount,
            this.colHistoryResult});
            this.lstHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstHistory.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lstHistory.FullRowSelect = true;
            this.lstHistory.GridLines = true;
            this.lstHistory.HideSelection = false;
            this.lstHistory.Location = new System.Drawing.Point(3, 21);
            this.lstHistory.Name = "lstHistory";
            this.lstHistory.Size = new System.Drawing.Size(484, 128);
            this.lstHistory.TabIndex = 0;
            this.lstHistory.UseCompatibleStateImageBehavior = false;
            this.lstHistory.View = System.Windows.Forms.View.Details;
            //
            // colHistorySide
            //
            this.colHistorySide.Text = "Side";
            this.colHistorySide.Width = 70;
            //
            // colHistoryCollet
            //
            this.colHistoryCollet.Text = "Collet";
            this.colHistoryCollet.Width = 60;
            //
            // colHistoryLastAt
            //
            this.colHistoryLastAt.Text = "마지막 클리닝";
            this.colHistoryLastAt.Width = 160;
            //
            // colHistoryCount
            //
            this.colHistoryCount.Text = "누적";
            this.colHistoryCount.Width = 60;
            //
            // colHistoryResult
            //
            this.colHistoryResult.Text = "결과";
            this.colHistoryResult.Width = 110;
            //
            // grpLog
            //
            this.grpLog.Controls.Add(this.lstLog);
            this.grpLog.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpLog.Location = new System.Drawing.Point(500, 354);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(490, 164);
            this.grpLog.TabIndex = 6;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "진행 로그";
            //
            // lstLog
            //
            this.lstLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstLog.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lstLog.FormattingEnabled = true;
            this.lstLog.HorizontalScrollbar = true;
            this.lstLog.ItemHeight = 19;
            this.lstLog.Location = new System.Drawing.Point(3, 21);
            this.lstLog.Name = "lstLog";
            this.lstLog.Size = new System.Drawing.Size(484, 140);
            this.lstLog.TabIndex = 0;
            //
            // bottomPanel
            //
            this.bottomPanel.Controls.Add(this.lblStatus);
            this.bottomPanel.Controls.Add(this.btnClose);
            this.bottomPanel.Controls.Add(this.btnReload);
            this.bottomPanel.Controls.Add(this.btnSave);
            this.bottomPanel.Controls.Add(this.btnStop);
            this.bottomPanel.Controls.Add(this.btnStart);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 528);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(1004, 56);
            this.bottomPanel.TabIndex = 7;
            //
            // lblStatus
            //
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblStatus.Location = new System.Drawing.Point(14, 16);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(470, 26);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "대기 중";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // btnStart
            //
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(140)))), ((int)(((byte)(70)))));
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.Location = new System.Drawing.Point(500, 12);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(110, 34);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "START";
            this.btnStart.UseVisualStyleBackColor = false;
            //
            // btnStop
            //
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.btnStop.Enabled = false;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(616, 12);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(110, 34);
            this.btnStop.TabIndex = 2;
            this.btnStop.Text = "STOP";
            this.btnStop.UseVisualStyleBackColor = false;
            //
            // btnSave
            //
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnSave.Location = new System.Drawing.Point(732, 12);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(84, 34);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "저장";
            this.btnSave.UseVisualStyleBackColor = true;
            //
            // btnReload
            //
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnReload.Location = new System.Drawing.Point(822, 12);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(84, 34);
            this.btnReload.TabIndex = 4;
            this.btnReload.Text = "불러오기";
            this.btnReload.UseVisualStyleBackColor = true;
            //
            // btnClose
            //
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.Location = new System.Drawing.Point(912, 12);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(78, 34);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "닫기";
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // ColletCleaningControlDialog
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1004, 584);
            this.Controls.Add(this.grpLog);
            this.Controls.Add(this.grpHistory);
            this.Controls.Add(this.grpTrigger);
            this.Controls.Add(this.grpHeight);
            this.Controls.Add(this.grpMotion);
            this.Controls.Add(this.grpTarget);
            this.Controls.Add(this.bottomPanel);
            this.Controls.Add(this.titleLabel);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ColletCleaningControlDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Collet Cleaning";
            this.grpTarget.ResumeLayout(false);
            this.grpMotion.ResumeLayout(false);
            this.grpHeight.ResumeLayout(false);
            this.grpTrigger.ResumeLayout(false);
            this.grpHistory.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.bottomPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numCleanVelocity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCleanAcceleration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCleanDeceleration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numContactZUserOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxExtraPressDepth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numArriveDwellMs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCleanPressCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRepeatLiftHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDieHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRimHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFilmHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxRetryCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWaferExchangeInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numProcessCountInterval)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
