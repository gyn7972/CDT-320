namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class FeederRecipePage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.TableLayoutPanel centerLayout;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.GroupBox grpActions;
        private System.Windows.Forms.GroupBox grpIo;
        private System.Windows.Forms.GroupBox grpOptions;
        private System.Windows.Forms.GroupBox grpWait;
        private System.Windows.Forms.GroupBox grpJog;
        private System.Windows.Forms.GroupBox grpSpeed;
        private System.Windows.Forms.TableLayoutPanel actionLayout;
        private QMC.CDT_320.Ui.Controls.ActionButton btnHomeMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnCassetteMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnStageMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnUp;
        private QMC.CDT_320.Ui.Controls.ActionButton btnDown;
        private QMC.CDT_320.Ui.Controls.ActionButton btnClamp;
        private QMC.CDT_320.Ui.Controls.ActionButton btnUnclamp;
        private System.Windows.Forms.TableLayoutPanel ioLayout;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotClampCheck;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotUpDownCheck;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotRingCheck;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotOverloadCheck;
        private System.Windows.Forms.Label lblClampCheck;
        private System.Windows.Forms.Label lblUpDownCheck;
        private System.Windows.Forms.Label lblRingCheck;
        private System.Windows.Forms.Label lblOverloadCheck;
        private System.Windows.Forms.TableLayoutPanel optionLayout;
        private System.Windows.Forms.Label lblHomePositionKey;
        private System.Windows.Forms.Label lblHomePositionValue;
        private System.Windows.Forms.Label lblCassettePositionKey;
        private System.Windows.Forms.Label lblCassettePositionValue;
        private System.Windows.Forms.Label lblStagePositionKey;
        private System.Windows.Forms.Label lblStagePositionValue;
        private System.Windows.Forms.Label lblUpSensorKey;
        private System.Windows.Forms.Label lblUpSensorValue;
        private System.Windows.Forms.Label lblDownSensorKey;
        private System.Windows.Forms.Label lblDownSensorValue;
        private System.Windows.Forms.Label lblClampTimeKey;
        private System.Windows.Forms.Label lblClampTimeValue;
        private System.Windows.Forms.Label lblFeederSpeedKey;
        private System.Windows.Forms.Label lblFeederSpeedValue;
        private System.Windows.Forms.TableLayoutPanel waitLayout;
        private System.Windows.Forms.Label lblMoveAfterWaitKey;
        private System.Windows.Forms.Label lblMoveAfterWaitValue;
        private System.Windows.Forms.Label lblUpDownWaitKey;
        private System.Windows.Forms.Label lblUpDownWaitValue;
        private System.Windows.Forms.TableLayoutPanel jogLayout;
        private System.Windows.Forms.Button btnJogYPlus;
        private System.Windows.Forms.Button btnJogXMinus;
        private System.Windows.Forms.Button btnJogStop;
        private System.Windows.Forms.Button btnJogXPlus;
        private System.Windows.Forms.Button btnJogYMinus;
        private System.Windows.Forms.TableLayoutPanel speedLayout;
        private System.Windows.Forms.TrackBar trkSpeed;
        private System.Windows.Forms.Label lblSpeedValue;

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
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpActions = new System.Windows.Forms.GroupBox();
            this.grpIo = new System.Windows.Forms.GroupBox();
            this.grpOptions = new System.Windows.Forms.GroupBox();
            this.grpWait = new System.Windows.Forms.GroupBox();
            this.grpJog = new System.Windows.Forms.GroupBox();
            this.grpSpeed = new System.Windows.Forms.GroupBox();
            this.actionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnHomeMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnCassetteMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnStageMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUp = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDown = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnClamp = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnUnclamp = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.ioLayout = new System.Windows.Forms.TableLayoutPanel();
            this.dotClampCheck = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.dotUpDownCheck = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.dotRingCheck = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.dotOverloadCheck = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblClampCheck = new System.Windows.Forms.Label();
            this.lblUpDownCheck = new System.Windows.Forms.Label();
            this.lblRingCheck = new System.Windows.Forms.Label();
            this.lblOverloadCheck = new System.Windows.Forms.Label();
            this.optionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHomePositionKey = new System.Windows.Forms.Label();
            this.lblHomePositionValue = new System.Windows.Forms.Label();
            this.lblCassettePositionKey = new System.Windows.Forms.Label();
            this.lblCassettePositionValue = new System.Windows.Forms.Label();
            this.lblStagePositionKey = new System.Windows.Forms.Label();
            this.lblStagePositionValue = new System.Windows.Forms.Label();
            this.lblUpSensorKey = new System.Windows.Forms.Label();
            this.lblUpSensorValue = new System.Windows.Forms.Label();
            this.lblDownSensorKey = new System.Windows.Forms.Label();
            this.lblDownSensorValue = new System.Windows.Forms.Label();
            this.lblClampTimeKey = new System.Windows.Forms.Label();
            this.lblClampTimeValue = new System.Windows.Forms.Label();
            this.lblFeederSpeedKey = new System.Windows.Forms.Label();
            this.lblFeederSpeedValue = new System.Windows.Forms.Label();
            this.waitLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblMoveAfterWaitKey = new System.Windows.Forms.Label();
            this.lblMoveAfterWaitValue = new System.Windows.Forms.Label();
            this.lblUpDownWaitKey = new System.Windows.Forms.Label();
            this.lblUpDownWaitValue = new System.Windows.Forms.Label();
            this.jogLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnJogYPlus = new System.Windows.Forms.Button();
            this.btnJogXMinus = new System.Windows.Forms.Button();
            this.btnJogStop = new System.Windows.Forms.Button();
            this.btnJogXPlus = new System.Windows.Forms.Button();
            this.btnJogYMinus = new System.Windows.Forms.Button();
            this.speedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.trkSpeed = new System.Windows.Forms.TrackBar();
            this.lblSpeedValue = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.centerLayout.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpActions.SuspendLayout();
            this.grpIo.SuspendLayout();
            this.grpOptions.SuspendLayout();
            this.grpWait.SuspendLayout();
            this.grpJog.SuspendLayout();
            this.grpSpeed.SuspendLayout();
            this.actionLayout.SuspendLayout();
            this.ioLayout.SuspendLayout();
            this.optionLayout.SuspendLayout();
            this.waitLayout.SuspendLayout();
            this.jogLayout.SuspendLayout();
            this.speedLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).BeginInit();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.contentLayout, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:recipe.inputFeeder";
            this.lblHeader.Text = "INPUT FEEDER";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 460F));
            this.contentLayout.Controls.Add(this.leftLayout, 0, 0);
            this.contentLayout.Controls.Add(this.centerLayout, 1, 0);
            this.contentLayout.Controls.Add(this.rightLayout, 2, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.Padding = new System.Windows.Forms.Padding(8);
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1400, 870);
            this.contentLayout.TabIndex = 1;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 1;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Controls.Add(this.grpActions, 0, 0);
            this.leftLayout.Controls.Add(this.grpIo, 0, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(8, 8);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 430F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.leftLayout.Size = new System.Drawing.Size(260, 854);
            this.leftLayout.TabIndex = 0;
            // 
            // centerLayout
            // 
            this.centerLayout.ColumnCount = 1;
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Controls.Add(this.grpOptions, 0, 0);
            this.centerLayout.Controls.Add(this.grpWait, 0, 1);
            this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerLayout.Location = new System.Drawing.Point(268, 8);
            this.centerLayout.Margin = new System.Windows.Forms.Padding(0);
            this.centerLayout.Name = "centerLayout";
            this.centerLayout.RowCount = 2;
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 320F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.centerLayout.Size = new System.Drawing.Size(664, 854);
            this.centerLayout.TabIndex = 1;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpJog, 0, 0);
            this.rightLayout.Controls.Add(this.grpSpeed, 0, 1);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(932, 8);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 2;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 430F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(460, 854);
            this.rightLayout.TabIndex = 2;
            // 
            // group boxes
            // 
            this.grpActions.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.grpActions.Controls.Add(this.actionLayout);
            this.grpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpActions.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpActions.Margin = new System.Windows.Forms.Padding(4);
            this.grpActions.Name = "grpActions";
            this.grpActions.TabIndex = 0;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "ACTION";
            this.grpIo.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.grpIo.Controls.Add(this.ioLayout);
            this.grpIo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpIo.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpIo.Margin = new System.Windows.Forms.Padding(4);
            this.grpIo.Name = "grpIo";
            this.grpIo.TabIndex = 1;
            this.grpIo.TabStop = false;
            this.grpIo.Text = "CYLINDER && I/O";
            this.grpOptions.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.grpOptions.Controls.Add(this.optionLayout);
            this.grpOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOptions.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpOptions.Margin = new System.Windows.Forms.Padding(4);
            this.grpOptions.Name = "grpOptions";
            this.grpOptions.TabIndex = 0;
            this.grpOptions.TabStop = false;
            this.grpOptions.Text = "OPTION";
            this.grpWait.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.grpWait.Controls.Add(this.waitLayout);
            this.grpWait.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWait.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpWait.Margin = new System.Windows.Forms.Padding(4);
            this.grpWait.Name = "grpWait";
            this.grpWait.TabIndex = 1;
            this.grpWait.TabStop = false;
            this.grpWait.Text = "WAIT TIME";
            this.grpJog.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.grpJog.Controls.Add(this.jogLayout);
            this.grpJog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJog.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpJog.Margin = new System.Windows.Forms.Padding(4);
            this.grpJog.Name = "grpJog";
            this.grpJog.TabIndex = 0;
            this.grpJog.TabStop = false;
            this.grpJog.Text = "JOG";
            this.grpSpeed.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.grpSpeed.Controls.Add(this.speedLayout);
            this.grpSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSpeed.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSpeed.Margin = new System.Windows.Forms.Padding(4);
            this.grpSpeed.Name = "grpSpeed";
            this.grpSpeed.TabIndex = 1;
            this.grpSpeed.TabStop = false;
            this.grpSpeed.Text = "SPEED";
            // 
            // actionLayout
            // 
            this.actionLayout.ColumnCount = 1;
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionLayout.Controls.Add(this.btnHomeMove, 0, 0);
            this.actionLayout.Controls.Add(this.btnCassetteMove, 0, 1);
            this.actionLayout.Controls.Add(this.btnStageMove, 0, 2);
            this.actionLayout.Controls.Add(this.btnUp, 0, 3);
            this.actionLayout.Controls.Add(this.btnDown, 0, 4);
            this.actionLayout.Controls.Add(this.btnClamp, 0, 5);
            this.actionLayout.Controls.Add(this.btnUnclamp, 0, 6);
            this.actionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionLayout.Location = new System.Drawing.Point(3, 26);
            this.actionLayout.Name = "actionLayout";
            this.actionLayout.Padding = new System.Windows.Forms.Padding(8);
            this.actionLayout.RowCount = 7;
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.29F));
            this.actionLayout.Size = new System.Drawing.Size(248, 397);
            this.actionLayout.TabIndex = 0;
            this.btnHomeMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHomeMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnHomeMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnHomeMove.Name = "btnHomeMove";
            this.btnHomeMove.Text = "HOME MOVE";
            this.btnCassetteMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCassetteMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnCassetteMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnCassetteMove.Name = "btnCassetteMove";
            this.btnCassetteMove.Text = "CASSETTE MOVE";
            this.btnStageMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStageMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnStageMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnStageMove.Name = "btnStageMove";
            this.btnStageMove.Text = "STAGE MOVE";
            this.btnUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUp.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnUp.Margin = new System.Windows.Forms.Padding(4);
            this.btnUp.Name = "btnUp";
            this.btnUp.Text = "UP";
            this.btnDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDown.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnDown.Margin = new System.Windows.Forms.Padding(4);
            this.btnDown.Name = "btnDown";
            this.btnDown.Text = "DOWN";
            this.btnClamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClamp.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnClamp.Margin = new System.Windows.Forms.Padding(4);
            this.btnClamp.Name = "btnClamp";
            this.btnClamp.Text = "CLAMP";
            this.btnUnclamp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnUnclamp.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnUnclamp.Margin = new System.Windows.Forms.Padding(4);
            this.btnUnclamp.Name = "btnUnclamp";
            this.btnUnclamp.Text = "UNCLAMP";
            // 
            // ioLayout
            // 
            this.ioLayout.ColumnCount = 2;
            this.ioLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ioLayout.Controls.Add(this.dotClampCheck, 0, 0);
            this.ioLayout.Controls.Add(this.lblClampCheck, 1, 0);
            this.ioLayout.Controls.Add(this.dotUpDownCheck, 0, 1);
            this.ioLayout.Controls.Add(this.lblUpDownCheck, 1, 1);
            this.ioLayout.Controls.Add(this.dotRingCheck, 0, 2);
            this.ioLayout.Controls.Add(this.lblRingCheck, 1, 2);
            this.ioLayout.Controls.Add(this.dotOverloadCheck, 0, 3);
            this.ioLayout.Controls.Add(this.lblOverloadCheck, 1, 3);
            this.ioLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioLayout.Location = new System.Drawing.Point(3, 26);
            this.ioLayout.Name = "ioLayout";
            this.ioLayout.Padding = new System.Windows.Forms.Padding(8, 18, 8, 8);
            this.ioLayout.RowCount = 4;
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.Size = new System.Drawing.Size(248, 157);
            this.ioLayout.TabIndex = 0;
            this.dotClampCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotClampCheck.Margin = new System.Windows.Forms.Padding(7);
            this.dotClampCheck.Name = "dotClampCheck";
            this.dotClampCheck.OnColor = System.Drawing.Color.LimeGreen;
            this.dotUpDownCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotUpDownCheck.Margin = new System.Windows.Forms.Padding(7);
            this.dotUpDownCheck.Name = "dotUpDownCheck";
            this.dotUpDownCheck.OnColor = System.Drawing.Color.LimeGreen;
            this.dotRingCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotRingCheck.Margin = new System.Windows.Forms.Padding(7);
            this.dotRingCheck.Name = "dotRingCheck";
            this.dotRingCheck.OnColor = System.Drawing.Color.LimeGreen;
            this.dotOverloadCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotOverloadCheck.Margin = new System.Windows.Forms.Padding(7);
            this.dotOverloadCheck.Name = "dotOverloadCheck";
            this.dotOverloadCheck.OnColor = System.Drawing.Color.LimeGreen;
            this.lblClampCheck.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblClampCheck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblClampCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblClampCheck.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblClampCheck.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblClampCheck.Text = "FEEDER CLAMP CHECK";
            this.lblClampCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblUpDownCheck.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblUpDownCheck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblUpDownCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUpDownCheck.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblUpDownCheck.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblUpDownCheck.Text = "FEEDER UP/DOWN CHECK";
            this.lblUpDownCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblRingCheck.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblRingCheck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRingCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRingCheck.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblRingCheck.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblRingCheck.Text = "FEEDER RING CHECK";
            this.lblRingCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblOverloadCheck.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblOverloadCheck.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOverloadCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOverloadCheck.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblOverloadCheck.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblOverloadCheck.Text = "FEEDER OVERLOAD CHECK";
            this.lblOverloadCheck.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optionLayout
            // 
            this.optionLayout.ColumnCount = 2;
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 62F));
            this.optionLayout.Controls.Add(this.lblHomePositionKey, 0, 0);
            this.optionLayout.Controls.Add(this.lblHomePositionValue, 1, 0);
            this.optionLayout.Controls.Add(this.lblCassettePositionKey, 0, 1);
            this.optionLayout.Controls.Add(this.lblCassettePositionValue, 1, 1);
            this.optionLayout.Controls.Add(this.lblStagePositionKey, 0, 2);
            this.optionLayout.Controls.Add(this.lblStagePositionValue, 1, 2);
            this.optionLayout.Controls.Add(this.lblUpSensorKey, 0, 3);
            this.optionLayout.Controls.Add(this.lblUpSensorValue, 1, 3);
            this.optionLayout.Controls.Add(this.lblDownSensorKey, 0, 4);
            this.optionLayout.Controls.Add(this.lblDownSensorValue, 1, 4);
            this.optionLayout.Controls.Add(this.lblClampTimeKey, 0, 5);
            this.optionLayout.Controls.Add(this.lblClampTimeValue, 1, 5);
            this.optionLayout.Controls.Add(this.lblFeederSpeedKey, 0, 6);
            this.optionLayout.Controls.Add(this.lblFeederSpeedValue, 1, 6);
            this.optionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.optionLayout.Location = new System.Drawing.Point(3, 26);
            this.optionLayout.Name = "optionLayout";
            this.optionLayout.Padding = new System.Windows.Forms.Padding(10, 18, 10, 10);
            this.optionLayout.RowCount = 7;
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.optionLayout.Size = new System.Drawing.Size(652, 287);
            this.optionLayout.TabIndex = 0;
            // 
            // option labels
            // 
            this.lblHomePositionKey.Text = "HOME POSITION";
            this.lblHomePositionValue.Text = "0 um";
            this.lblCassettePositionKey.Text = "CASSETTE POSITION";
            this.lblCassettePositionValue.Text = "50000 um";
            this.lblStagePositionKey.Text = "STAGE POSITION";
            this.lblStagePositionValue.Text = "200000 um";
            this.lblUpSensorKey.Text = "UP COMPLETE SENSOR";
            this.lblUpSensorValue.Text = "ENABLE";
            this.lblDownSensorKey.Text = "DOWN COMPLETE SENSOR";
            this.lblDownSensorValue.Text = "ENABLE";
            this.lblClampTimeKey.Text = "CLAMP TIME";
            this.lblClampTimeValue.Text = "300 ms";
            this.lblFeederSpeedKey.Text = "FEEDER SPEED";
            this.lblFeederSpeedValue.Text = "500 mm/s";
            // 
            // waitLayout
            // 
            this.waitLayout.ColumnCount = 2;
            this.waitLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this.waitLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 62F));
            this.waitLayout.Controls.Add(this.lblMoveAfterWaitKey, 0, 0);
            this.waitLayout.Controls.Add(this.lblMoveAfterWaitValue, 1, 0);
            this.waitLayout.Controls.Add(this.lblUpDownWaitKey, 0, 1);
            this.waitLayout.Controls.Add(this.lblUpDownWaitValue, 1, 1);
            this.waitLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitLayout.Location = new System.Drawing.Point(3, 26);
            this.waitLayout.Name = "waitLayout";
            this.waitLayout.Padding = new System.Windows.Forms.Padding(10, 18, 10, 10);
            this.waitLayout.RowCount = 2;
            this.waitLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.waitLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.waitLayout.Size = new System.Drawing.Size(652, 117);
            this.waitLayout.TabIndex = 0;
            this.lblMoveAfterWaitKey.Text = "MOVE AFTER WAIT";
            this.lblMoveAfterWaitValue.Text = "100 ms";
            this.lblUpDownWaitKey.Text = "UP/DOWN COMPLETE WAIT";
            this.lblUpDownWaitValue.Text = "50 ms";
            // 
            // pair label styling
            // 
            this.lblHomePositionKey.BackColor = this.lblCassettePositionKey.BackColor = this.lblStagePositionKey.BackColor = this.lblUpSensorKey.BackColor = this.lblDownSensorKey.BackColor = this.lblClampTimeKey.BackColor = this.lblFeederSpeedKey.BackColor = this.lblMoveAfterWaitKey.BackColor = this.lblUpDownWaitKey.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblHomePositionValue.BackColor = this.lblCassettePositionValue.BackColor = this.lblStagePositionValue.BackColor = this.lblUpSensorValue.BackColor = this.lblDownSensorValue.BackColor = this.lblClampTimeValue.BackColor = this.lblFeederSpeedValue.BackColor = this.lblMoveAfterWaitValue.BackColor = this.lblUpDownWaitValue.BackColor = System.Drawing.Color.White;
            this.lblHomePositionKey.BorderStyle = this.lblCassettePositionKey.BorderStyle = this.lblStagePositionKey.BorderStyle = this.lblUpSensorKey.BorderStyle = this.lblDownSensorKey.BorderStyle = this.lblClampTimeKey.BorderStyle = this.lblFeederSpeedKey.BorderStyle = this.lblMoveAfterWaitKey.BorderStyle = this.lblUpDownWaitKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHomePositionValue.BorderStyle = this.lblCassettePositionValue.BorderStyle = this.lblStagePositionValue.BorderStyle = this.lblUpSensorValue.BorderStyle = this.lblDownSensorValue.BorderStyle = this.lblClampTimeValue.BorderStyle = this.lblFeederSpeedValue.BorderStyle = this.lblMoveAfterWaitValue.BorderStyle = this.lblUpDownWaitValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHomePositionKey.Dock = this.lblCassettePositionKey.Dock = this.lblStagePositionKey.Dock = this.lblUpSensorKey.Dock = this.lblDownSensorKey.Dock = this.lblClampTimeKey.Dock = this.lblFeederSpeedKey.Dock = this.lblMoveAfterWaitKey.Dock = this.lblUpDownWaitKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHomePositionValue.Dock = this.lblCassettePositionValue.Dock = this.lblStagePositionValue.Dock = this.lblUpSensorValue.Dock = this.lblDownSensorValue.Dock = this.lblClampTimeValue.Dock = this.lblFeederSpeedValue.Dock = this.lblMoveAfterWaitValue.Dock = this.lblUpDownWaitValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHomePositionKey.Font = this.lblCassettePositionKey.Font = this.lblStagePositionKey.Font = this.lblUpSensorKey.Font = this.lblDownSensorKey.Font = this.lblClampTimeKey.Font = this.lblFeederSpeedKey.Font = this.lblMoveAfterWaitKey.Font = this.lblUpDownWaitKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblHomePositionValue.Font = this.lblCassettePositionValue.Font = this.lblStagePositionValue.Font = this.lblUpSensorValue.Font = this.lblDownSensorValue.Font = this.lblClampTimeValue.Font = this.lblFeederSpeedValue.Font = this.lblMoveAfterWaitValue.Font = this.lblUpDownWaitValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblHomePositionKey.Padding = this.lblCassettePositionKey.Padding = this.lblStagePositionKey.Padding = this.lblUpSensorKey.Padding = this.lblDownSensorKey.Padding = this.lblClampTimeKey.Padding = this.lblFeederSpeedKey.Padding = this.lblMoveAfterWaitKey.Padding = this.lblUpDownWaitKey.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblHomePositionValue.Padding = this.lblCassettePositionValue.Padding = this.lblStagePositionValue.Padding = this.lblUpSensorValue.Padding = this.lblDownSensorValue.Padding = this.lblClampTimeValue.Padding = this.lblFeederSpeedValue.Padding = this.lblMoveAfterWaitValue.Padding = this.lblUpDownWaitValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblHomePositionKey.TextAlign = this.lblCassettePositionKey.TextAlign = this.lblStagePositionKey.TextAlign = this.lblUpSensorKey.TextAlign = this.lblDownSensorKey.TextAlign = this.lblClampTimeKey.TextAlign = this.lblFeederSpeedKey.TextAlign = this.lblMoveAfterWaitKey.TextAlign = this.lblUpDownWaitKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblHomePositionValue.TextAlign = this.lblCassettePositionValue.TextAlign = this.lblStagePositionValue.TextAlign = this.lblUpSensorValue.TextAlign = this.lblDownSensorValue.TextAlign = this.lblClampTimeValue.TextAlign = this.lblFeederSpeedValue.TextAlign = this.lblMoveAfterWaitValue.TextAlign = this.lblUpDownWaitValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // jogLayout
            // 
            this.jogLayout.ColumnCount = 3;
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogLayout.Controls.Add(this.btnJogYPlus, 1, 0);
            this.jogLayout.Controls.Add(this.btnJogXMinus, 0, 1);
            this.jogLayout.Controls.Add(this.btnJogStop, 1, 1);
            this.jogLayout.Controls.Add(this.btnJogXPlus, 2, 1);
            this.jogLayout.Controls.Add(this.btnJogYMinus, 1, 2);
            this.jogLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogLayout.Location = new System.Drawing.Point(3, 26);
            this.jogLayout.Name = "jogLayout";
            this.jogLayout.Padding = new System.Windows.Forms.Padding(24);
            this.jogLayout.RowCount = 3;
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogLayout.Size = new System.Drawing.Size(448, 397);
            this.jogLayout.TabIndex = 0;
            this.btnJogYPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogYPlus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogYPlus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogYPlus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogYPlus.Name = "btnJogYPlus";
            this.btnJogYPlus.Text = "+Y";
            this.btnJogXMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogXMinus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogXMinus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogXMinus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogXMinus.Name = "btnJogXMinus";
            this.btnJogXMinus.Text = "-X";
            this.btnJogStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogStop.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogStop.Name = "btnJogStop";
            this.btnJogStop.Text = "STOP";
            this.btnJogXPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogXPlus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogXPlus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogXPlus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogXPlus.Name = "btnJogXPlus";
            this.btnJogXPlus.Text = "+X";
            this.btnJogYMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogYMinus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnJogYMinus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogYMinus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogYMinus.Name = "btnJogYMinus";
            this.btnJogYMinus.Text = "-Y";
            // 
            // speedLayout
            // 
            this.speedLayout.ColumnCount = 1;
            this.speedLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedLayout.Controls.Add(this.trkSpeed, 0, 0);
            this.speedLayout.Controls.Add(this.lblSpeedValue, 0, 1);
            this.speedLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedLayout.Location = new System.Drawing.Point(3, 26);
            this.speedLayout.Name = "speedLayout";
            this.speedLayout.Padding = new System.Windows.Forms.Padding(16, 22, 16, 16);
            this.speedLayout.RowCount = 2;
            this.speedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.speedLayout.Size = new System.Drawing.Size(448, 389);
            this.speedLayout.TabIndex = 0;
            this.trkSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trkSpeed.Location = new System.Drawing.Point(19, 25);
            this.trkSpeed.Maximum = 100;
            this.trkSpeed.Name = "trkSpeed";
            this.trkSpeed.Size = new System.Drawing.Size(410, 313);
            this.trkSpeed.TabIndex = 0;
            this.trkSpeed.TickFrequency = 10;
            this.trkSpeed.Value = 50;
            this.lblSpeedValue.BackColor = System.Drawing.Color.FromArgb(208, 208, 208);
            this.lblSpeedValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSpeedValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpeedValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSpeedValue.Location = new System.Drawing.Point(19, 341);
            this.lblSpeedValue.Name = "lblSpeedValue";
            this.lblSpeedValue.Size = new System.Drawing.Size(410, 32);
            this.lblSpeedValue.TabIndex = 1;
            this.lblSpeedValue.Text = "50%";
            this.lblSpeedValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FeederRecipePage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "FeederRecipePage";
            this.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.centerLayout.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpActions.ResumeLayout(false);
            this.grpIo.ResumeLayout(false);
            this.grpOptions.ResumeLayout(false);
            this.grpWait.ResumeLayout(false);
            this.grpJog.ResumeLayout(false);
            this.grpSpeed.ResumeLayout(false);
            this.actionLayout.ResumeLayout(false);
            this.ioLayout.ResumeLayout(false);
            this.optionLayout.ResumeLayout(false);
            this.waitLayout.ResumeLayout(false);
            this.jogLayout.ResumeLayout(false);
            this.speedLayout.ResumeLayout(false);
            this.speedLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
