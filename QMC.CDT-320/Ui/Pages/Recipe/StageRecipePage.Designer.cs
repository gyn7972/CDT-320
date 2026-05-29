namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class StageRecipePage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.TableLayoutPanel leftLayout;
        private System.Windows.Forms.TableLayoutPanel centerLayout;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.GroupBox grpOptions;
        private System.Windows.Forms.GroupBox grpWait;
        private System.Windows.Forms.GroupBox grpManual;
        private System.Windows.Forms.GroupBox grpIo;
        private System.Windows.Forms.GroupBox grpVision;
        private System.Windows.Forms.GroupBox grpJog;
        private System.Windows.Forms.GroupBox grpSpeed;
        private System.Windows.Forms.TableLayoutPanel optionLayout;
        private System.Windows.Forms.TableLayoutPanel waitLayout;
        private System.Windows.Forms.TableLayoutPanel manualLayout;
        private System.Windows.Forms.TableLayoutPanel ioLayout;
        private System.Windows.Forms.Panel visionPanel;
        private System.Windows.Forms.Label lblVisionInfo;
        private System.Windows.Forms.TableLayoutPanel jogLayout;
        private System.Windows.Forms.TableLayoutPanel jogAxisLayout;
        private System.Windows.Forms.TableLayoutPanel jogXyLayout;
        private System.Windows.Forms.TableLayoutPanel jogZLayout;
        private System.Windows.Forms.TableLayoutPanel speedLayout;
        private System.Windows.Forms.TrackBar trkSpeed;
        private System.Windows.Forms.Label lblSpeedValue;

        private System.Windows.Forms.Label lblLoadingPositionKey;
        private System.Windows.Forms.Label lblLoadingPositionValue;
        private System.Windows.Forms.Label lblCenterPositionKey;
        private System.Windows.Forms.Label lblCenterPositionValue;
        private System.Windows.Forms.Label lblNeedlePositionKey;
        private System.Windows.Forms.Label lblNeedlePositionValue;
        private System.Windows.Forms.Label lblTestBedPositionKey;
        private System.Windows.Forms.Label lblTestBedPositionValue;
        private System.Windows.Forms.Label lblBarcodePositionKey;
        private System.Windows.Forms.Label lblBarcodePositionValue;
        private System.Windows.Forms.Label lblVisionAlignKey;
        private System.Windows.Forms.Label lblVisionAlignValue;
        private System.Windows.Forms.Label lblWorkRadiusKey;
        private System.Windows.Forms.Label lblWorkRadiusValue;
        private System.Windows.Forms.Label lblFirstDiePositionKey;
        private System.Windows.Forms.Label lblFirstDiePositionValue;
        private System.Windows.Forms.Label lblNeedleMeasurePositionKey;
        private System.Windows.Forms.Label lblNeedleMeasurePositionValue;

        private System.Windows.Forms.Label lblNeedleUpWaitKey;
        private System.Windows.Forms.Label lblNeedleUpWaitValue;
        private System.Windows.Forms.Label lblVacuumOnWaitKey;
        private System.Windows.Forms.Label lblVacuumOnWaitValue;
        private System.Windows.Forms.Label lblNeedleDownWaitKey;
        private System.Windows.Forms.Label lblNeedleDownWaitValue;
        private System.Windows.Forms.Label lblVacuumOffWaitKey;
        private System.Windows.Forms.Label lblVacuumOffWaitValue;
        private System.Windows.Forms.Label lblMovingWaitKey;
        private System.Windows.Forms.Label lblMovingWaitValue;

        private QMC.CDT_320.Ui.Controls.ActionButton btnLoadingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnCenterMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnBarcodeMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnFirstDieMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnPickUpTest;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNeedleUpMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNeedleDownMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNeedleReadyMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNeedleBlockReady;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNeedleBlockWork;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAutoSettingMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnInputConversion;
        private QMC.CDT_320.Ui.Controls.ActionButton btnExpandWorkMove;

        private QMC.CDT_320.Ui.Controls.IndicatorDot dotNeedleVacuum;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotRingSensor;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotExpandCylinder;
        private QMC.CDT_320.Ui.Controls.IndicatorDot dotNeedleBlock;
        private System.Windows.Forms.Label lblNeedleVacuumKey;
        private System.Windows.Forms.Label lblNeedleVacuumValue;
        private System.Windows.Forms.Label lblRingSensorKey;
        private System.Windows.Forms.Label lblRingSensorValue;
        private System.Windows.Forms.Label lblExpandCylinderKey;
        private System.Windows.Forms.Label lblExpandCylinderValue;
        private System.Windows.Forms.Label lblNeedleBlockKey;
        private System.Windows.Forms.Label lblNeedleBlockValue;

        private System.Windows.Forms.Label lblAxisXKey;
        private System.Windows.Forms.Label lblAxisXValue;
        private System.Windows.Forms.Label lblAxisYKey;
        private System.Windows.Forms.Label lblAxisYValue;
        private System.Windows.Forms.Label lblAxisTKey;
        private System.Windows.Forms.Label lblAxisTValue;
        private System.Windows.Forms.Label lblExpandZKey;
        private System.Windows.Forms.Label lblExpandZValue;
        private System.Windows.Forms.Label lblNeedleZKey;
        private System.Windows.Forms.Label lblNeedleZValue;
        private System.Windows.Forms.Label lblNeedleBlockZKey;
        private System.Windows.Forms.Label lblNeedleBlockZValue;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogYPlus;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogXMinus;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogStop;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogXPlus;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogYMinus;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogTMinus;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogTPlus;
        private QMC.CDT_320.Ui.Controls.ActionButton btnExpandUp;
        private QMC.CDT_320.Ui.Controls.ActionButton btnExpandDown;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNeedleUp;
        private QMC.CDT_320.Ui.Controls.ActionButton btnNeedleDown;
        private QMC.CDT_320.Ui.Controls.ActionButton btnBlockUp;
        private QMC.CDT_320.Ui.Controls.ActionButton btnBlockDown;
        private System.Windows.Forms.Label lblExpandAxis;
        private System.Windows.Forms.Label lblNeedleAxis;
        private System.Windows.Forms.Label lblBlockAxis;

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
            this.grpOptions = new System.Windows.Forms.GroupBox();
            this.optionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblLoadingPositionKey = new System.Windows.Forms.Label();
            this.lblLoadingPositionValue = new System.Windows.Forms.Label();
            this.lblCenterPositionKey = new System.Windows.Forms.Label();
            this.lblCenterPositionValue = new System.Windows.Forms.Label();
            this.lblNeedlePositionKey = new System.Windows.Forms.Label();
            this.lblNeedlePositionValue = new System.Windows.Forms.Label();
            this.lblTestBedPositionKey = new System.Windows.Forms.Label();
            this.lblTestBedPositionValue = new System.Windows.Forms.Label();
            this.lblBarcodePositionKey = new System.Windows.Forms.Label();
            this.lblBarcodePositionValue = new System.Windows.Forms.Label();
            this.lblVisionAlignKey = new System.Windows.Forms.Label();
            this.lblVisionAlignValue = new System.Windows.Forms.Label();
            this.lblWorkRadiusKey = new System.Windows.Forms.Label();
            this.lblWorkRadiusValue = new System.Windows.Forms.Label();
            this.lblFirstDiePositionKey = new System.Windows.Forms.Label();
            this.lblFirstDiePositionValue = new System.Windows.Forms.Label();
            this.lblNeedleMeasurePositionKey = new System.Windows.Forms.Label();
            this.lblNeedleMeasurePositionValue = new System.Windows.Forms.Label();
            this.grpWait = new System.Windows.Forms.GroupBox();
            this.waitLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblNeedleUpWaitKey = new System.Windows.Forms.Label();
            this.lblNeedleUpWaitValue = new System.Windows.Forms.Label();
            this.lblVacuumOnWaitKey = new System.Windows.Forms.Label();
            this.lblVacuumOnWaitValue = new System.Windows.Forms.Label();
            this.lblNeedleDownWaitKey = new System.Windows.Forms.Label();
            this.lblNeedleDownWaitValue = new System.Windows.Forms.Label();
            this.lblVacuumOffWaitKey = new System.Windows.Forms.Label();
            this.lblVacuumOffWaitValue = new System.Windows.Forms.Label();
            this.lblMovingWaitKey = new System.Windows.Forms.Label();
            this.lblMovingWaitValue = new System.Windows.Forms.Label();
            this.grpManual = new System.Windows.Forms.GroupBox();
            this.manualLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnLoadingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnCenterMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnBarcodeMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnFirstDieMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPickUpTest = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleUpMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleDownMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleReadyMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleBlockReady = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleBlockWork = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAutoSettingMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnInputConversion = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnExpandWorkMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.grpIo = new System.Windows.Forms.GroupBox();
            this.ioLayout = new System.Windows.Forms.TableLayoutPanel();
            this.dotNeedleVacuum = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblNeedleVacuumKey = new System.Windows.Forms.Label();
            this.lblNeedleVacuumValue = new System.Windows.Forms.Label();
            this.dotRingSensor = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblRingSensorKey = new System.Windows.Forms.Label();
            this.lblRingSensorValue = new System.Windows.Forms.Label();
            this.dotExpandCylinder = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblExpandCylinderKey = new System.Windows.Forms.Label();
            this.lblExpandCylinderValue = new System.Windows.Forms.Label();
            this.dotNeedleBlock = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblNeedleBlockKey = new System.Windows.Forms.Label();
            this.lblNeedleBlockValue = new System.Windows.Forms.Label();
            this.centerLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpVision = new System.Windows.Forms.GroupBox();
            this.visionPanel = new System.Windows.Forms.Panel();
            this.lblVisionInfo = new System.Windows.Forms.Label();
            this.grpJog = new System.Windows.Forms.GroupBox();
            this.jogLayout = new System.Windows.Forms.TableLayoutPanel();
            this.jogAxisLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblAxisXKey = new System.Windows.Forms.Label();
            this.lblAxisXValue = new System.Windows.Forms.Label();
            this.lblAxisYKey = new System.Windows.Forms.Label();
            this.lblAxisYValue = new System.Windows.Forms.Label();
            this.lblAxisTKey = new System.Windows.Forms.Label();
            this.lblAxisTValue = new System.Windows.Forms.Label();
            this.lblExpandZKey = new System.Windows.Forms.Label();
            this.lblExpandZValue = new System.Windows.Forms.Label();
            this.lblNeedleZKey = new System.Windows.Forms.Label();
            this.lblNeedleZValue = new System.Windows.Forms.Label();
            this.lblNeedleBlockZKey = new System.Windows.Forms.Label();
            this.lblNeedleBlockZValue = new System.Windows.Forms.Label();
            this.jogXyLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnJogYPlus = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogXMinus = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogXPlus = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogTMinus = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogYMinus = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogTPlus = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.jogZLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnExpandUp = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleUp = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnBlockUp = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.lblExpandAxis = new System.Windows.Forms.Label();
            this.lblNeedleAxis = new System.Windows.Forms.Label();
            this.lblBlockAxis = new System.Windows.Forms.Label();
            this.btnExpandDown = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleDown = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnBlockDown = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpSpeed = new System.Windows.Forms.GroupBox();
            this.speedLayout = new System.Windows.Forms.TableLayoutPanel();
            this.trkSpeed = new System.Windows.Forms.TrackBar();
            this.lblSpeedValue = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.grpOptions.SuspendLayout();
            this.optionLayout.SuspendLayout();
            this.grpWait.SuspendLayout();
            this.waitLayout.SuspendLayout();
            this.grpManual.SuspendLayout();
            this.manualLayout.SuspendLayout();
            this.grpIo.SuspendLayout();
            this.ioLayout.SuspendLayout();
            this.centerLayout.SuspendLayout();
            this.grpVision.SuspendLayout();
            this.visionPanel.SuspendLayout();
            this.grpJog.SuspendLayout();
            this.jogLayout.SuspendLayout();
            this.jogAxisLayout.SuspendLayout();
            this.jogXyLayout.SuspendLayout();
            this.jogZLayout.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.grpSpeed.SuspendLayout();
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
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:recipe.inputStage";
            this.lblHeader.Text = "INPUT STAGE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 3;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 420F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
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
            this.leftLayout.Controls.Add(this.grpOptions, 0, 0);
            this.leftLayout.Controls.Add(this.grpWait, 0, 1);
            this.leftLayout.Controls.Add(this.grpManual, 0, 2);
            this.leftLayout.Controls.Add(this.grpIo, 0, 3);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(8, 8);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 4;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 340F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftLayout.Size = new System.Drawing.Size(420, 854);
            this.leftLayout.TabIndex = 0;
            // 
            // grpOptions
            // 
            this.grpOptions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpOptions.Controls.Add(this.optionLayout);
            this.grpOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOptions.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpOptions.Location = new System.Drawing.Point(4, 4);
            this.grpOptions.Margin = new System.Windows.Forms.Padding(4);
            this.grpOptions.Name = "grpOptions";
            this.grpOptions.Size = new System.Drawing.Size(412, 332);
            this.grpOptions.TabIndex = 0;
            this.grpOptions.TabStop = false;
            this.grpOptions.Text = "OPTION";
            // 
            // optionLayout
            // 
            this.optionLayout.ColumnCount = 2;
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.optionLayout.Controls.Add(this.lblLoadingPositionKey, 0, 0);
            this.optionLayout.Controls.Add(this.lblLoadingPositionValue, 1, 0);
            this.optionLayout.Controls.Add(this.lblCenterPositionKey, 0, 1);
            this.optionLayout.Controls.Add(this.lblCenterPositionValue, 1, 1);
            this.optionLayout.Controls.Add(this.lblNeedlePositionKey, 0, 2);
            this.optionLayout.Controls.Add(this.lblNeedlePositionValue, 1, 2);
            this.optionLayout.Controls.Add(this.lblTestBedPositionKey, 0, 3);
            this.optionLayout.Controls.Add(this.lblTestBedPositionValue, 1, 3);
            this.optionLayout.Controls.Add(this.lblBarcodePositionKey, 0, 4);
            this.optionLayout.Controls.Add(this.lblBarcodePositionValue, 1, 4);
            this.optionLayout.Controls.Add(this.lblVisionAlignKey, 0, 5);
            this.optionLayout.Controls.Add(this.lblVisionAlignValue, 1, 5);
            this.optionLayout.Controls.Add(this.lblWorkRadiusKey, 0, 6);
            this.optionLayout.Controls.Add(this.lblWorkRadiusValue, 1, 6);
            this.optionLayout.Controls.Add(this.lblFirstDiePositionKey, 0, 7);
            this.optionLayout.Controls.Add(this.lblFirstDiePositionValue, 1, 7);
            this.optionLayout.Controls.Add(this.lblNeedleMeasurePositionKey, 0, 8);
            this.optionLayout.Controls.Add(this.lblNeedleMeasurePositionValue, 1, 8);
            this.optionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.optionLayout.Location = new System.Drawing.Point(3, 26);
            this.optionLayout.Name = "optionLayout";
            this.optionLayout.Padding = new System.Windows.Forms.Padding(10, 18, 10, 8);
            this.optionLayout.RowCount = 9;
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.Size = new System.Drawing.Size(406, 303);
            this.optionLayout.TabIndex = 0;
            // 
            // lblLoadingPositionKey
            // 
            this.lblLoadingPositionKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblLoadingPositionKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLoadingPositionKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLoadingPositionKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblLoadingPositionKey.Location = new System.Drawing.Point(13, 18);
            this.lblLoadingPositionKey.Name = "lblLoadingPositionKey";
            this.lblLoadingPositionKey.Size = new System.Drawing.Size(206, 30);
            this.lblLoadingPositionKey.TabIndex = 0;
            this.lblLoadingPositionKey.Text = "LOADING POSITION";
            this.lblLoadingPositionKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLoadingPositionValue
            // 
            this.lblLoadingPositionValue.BackColor = System.Drawing.Color.White;
            this.lblLoadingPositionValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLoadingPositionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLoadingPositionValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblLoadingPositionValue.Location = new System.Drawing.Point(225, 18);
            this.lblLoadingPositionValue.Name = "lblLoadingPositionValue";
            this.lblLoadingPositionValue.Size = new System.Drawing.Size(168, 30);
            this.lblLoadingPositionValue.TabIndex = 1;
            this.lblLoadingPositionValue.Text = "SET";
            this.lblLoadingPositionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCenterPositionKey
            // 
            this.lblCenterPositionKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblCenterPositionKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCenterPositionKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCenterPositionKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblCenterPositionKey.Location = new System.Drawing.Point(13, 48);
            this.lblCenterPositionKey.Name = "lblCenterPositionKey";
            this.lblCenterPositionKey.Size = new System.Drawing.Size(206, 30);
            this.lblCenterPositionKey.TabIndex = 2;
            this.lblCenterPositionKey.Text = "CENTER POSITION";
            this.lblCenterPositionKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCenterPositionValue
            // 
            this.lblCenterPositionValue.BackColor = System.Drawing.Color.White;
            this.lblCenterPositionValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblCenterPositionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCenterPositionValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblCenterPositionValue.Location = new System.Drawing.Point(225, 48);
            this.lblCenterPositionValue.Name = "lblCenterPositionValue";
            this.lblCenterPositionValue.Size = new System.Drawing.Size(168, 30);
            this.lblCenterPositionValue.TabIndex = 3;
            this.lblCenterPositionValue.Text = "SET";
            this.lblCenterPositionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNeedlePositionKey
            // 
            this.lblNeedlePositionKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedlePositionKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedlePositionKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedlePositionKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNeedlePositionKey.Location = new System.Drawing.Point(13, 78);
            this.lblNeedlePositionKey.Name = "lblNeedlePositionKey";
            this.lblNeedlePositionKey.Size = new System.Drawing.Size(206, 30);
            this.lblNeedlePositionKey.TabIndex = 4;
            this.lblNeedlePositionKey.Text = "NEEDLE POSITION";
            this.lblNeedlePositionKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedlePositionValue
            // 
            this.lblNeedlePositionValue.BackColor = System.Drawing.Color.White;
            this.lblNeedlePositionValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedlePositionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedlePositionValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedlePositionValue.Location = new System.Drawing.Point(225, 78);
            this.lblNeedlePositionValue.Name = "lblNeedlePositionValue";
            this.lblNeedlePositionValue.Size = new System.Drawing.Size(168, 30);
            this.lblNeedlePositionValue.TabIndex = 5;
            this.lblNeedlePositionValue.Text = "SET";
            this.lblNeedlePositionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTestBedPositionKey
            // 
            this.lblTestBedPositionKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblTestBedPositionKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTestBedPositionKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTestBedPositionKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblTestBedPositionKey.Location = new System.Drawing.Point(13, 108);
            this.lblTestBedPositionKey.Name = "lblTestBedPositionKey";
            this.lblTestBedPositionKey.Size = new System.Drawing.Size(206, 30);
            this.lblTestBedPositionKey.TabIndex = 6;
            this.lblTestBedPositionKey.Text = "TEST BED POSITION";
            this.lblTestBedPositionKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTestBedPositionValue
            // 
            this.lblTestBedPositionValue.BackColor = System.Drawing.Color.White;
            this.lblTestBedPositionValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTestBedPositionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTestBedPositionValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblTestBedPositionValue.Location = new System.Drawing.Point(225, 108);
            this.lblTestBedPositionValue.Name = "lblTestBedPositionValue";
            this.lblTestBedPositionValue.Size = new System.Drawing.Size(168, 30);
            this.lblTestBedPositionValue.TabIndex = 7;
            this.lblTestBedPositionValue.Text = "SET";
            this.lblTestBedPositionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblBarcodePositionKey
            // 
            this.lblBarcodePositionKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblBarcodePositionKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBarcodePositionKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBarcodePositionKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblBarcodePositionKey.Location = new System.Drawing.Point(13, 138);
            this.lblBarcodePositionKey.Name = "lblBarcodePositionKey";
            this.lblBarcodePositionKey.Size = new System.Drawing.Size(206, 30);
            this.lblBarcodePositionKey.TabIndex = 8;
            this.lblBarcodePositionKey.Text = "BARCODE POSITION";
            this.lblBarcodePositionKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBarcodePositionValue
            // 
            this.lblBarcodePositionValue.BackColor = System.Drawing.Color.White;
            this.lblBarcodePositionValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBarcodePositionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBarcodePositionValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblBarcodePositionValue.Location = new System.Drawing.Point(225, 138);
            this.lblBarcodePositionValue.Name = "lblBarcodePositionValue";
            this.lblBarcodePositionValue.Size = new System.Drawing.Size(168, 30);
            this.lblBarcodePositionValue.TabIndex = 9;
            this.lblBarcodePositionValue.Text = "SET";
            this.lblBarcodePositionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblVisionAlignKey
            // 
            this.lblVisionAlignKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblVisionAlignKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVisionAlignKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionAlignKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblVisionAlignKey.Location = new System.Drawing.Point(13, 168);
            this.lblVisionAlignKey.Name = "lblVisionAlignKey";
            this.lblVisionAlignKey.Size = new System.Drawing.Size(206, 30);
            this.lblVisionAlignKey.TabIndex = 10;
            this.lblVisionAlignKey.Text = "VISION ALIGN";
            this.lblVisionAlignKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVisionAlignValue
            // 
            this.lblVisionAlignValue.BackColor = System.Drawing.Color.White;
            this.lblVisionAlignValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVisionAlignValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionAlignValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblVisionAlignValue.Location = new System.Drawing.Point(225, 168);
            this.lblVisionAlignValue.Name = "lblVisionAlignValue";
            this.lblVisionAlignValue.Size = new System.Drawing.Size(168, 30);
            this.lblVisionAlignValue.TabIndex = 11;
            this.lblVisionAlignValue.Text = "SET";
            this.lblVisionAlignValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblWorkRadiusKey
            // 
            this.lblWorkRadiusKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblWorkRadiusKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWorkRadiusKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWorkRadiusKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblWorkRadiusKey.Location = new System.Drawing.Point(13, 198);
            this.lblWorkRadiusKey.Name = "lblWorkRadiusKey";
            this.lblWorkRadiusKey.Size = new System.Drawing.Size(206, 30);
            this.lblWorkRadiusKey.TabIndex = 12;
            this.lblWorkRadiusKey.Text = "WORK RADIUS";
            this.lblWorkRadiusKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWorkRadiusValue
            // 
            this.lblWorkRadiusValue.BackColor = System.Drawing.Color.White;
            this.lblWorkRadiusValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWorkRadiusValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWorkRadiusValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblWorkRadiusValue.Location = new System.Drawing.Point(225, 198);
            this.lblWorkRadiusValue.Name = "lblWorkRadiusValue";
            this.lblWorkRadiusValue.Size = new System.Drawing.Size(168, 30);
            this.lblWorkRadiusValue.TabIndex = 13;
            this.lblWorkRadiusValue.Text = "SET";
            this.lblWorkRadiusValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblFirstDiePositionKey
            // 
            this.lblFirstDiePositionKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblFirstDiePositionKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFirstDiePositionKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFirstDiePositionKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblFirstDiePositionKey.Location = new System.Drawing.Point(13, 228);
            this.lblFirstDiePositionKey.Name = "lblFirstDiePositionKey";
            this.lblFirstDiePositionKey.Size = new System.Drawing.Size(206, 30);
            this.lblFirstDiePositionKey.TabIndex = 14;
            this.lblFirstDiePositionKey.Text = "FIRST DIE POSITION";
            this.lblFirstDiePositionKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFirstDiePositionValue
            // 
            this.lblFirstDiePositionValue.BackColor = System.Drawing.Color.White;
            this.lblFirstDiePositionValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblFirstDiePositionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFirstDiePositionValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblFirstDiePositionValue.Location = new System.Drawing.Point(225, 228);
            this.lblFirstDiePositionValue.Name = "lblFirstDiePositionValue";
            this.lblFirstDiePositionValue.Size = new System.Drawing.Size(168, 30);
            this.lblFirstDiePositionValue.TabIndex = 15;
            this.lblFirstDiePositionValue.Text = "SET";
            this.lblFirstDiePositionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNeedleMeasurePositionKey
            // 
            this.lblNeedleMeasurePositionKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedleMeasurePositionKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleMeasurePositionKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleMeasurePositionKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNeedleMeasurePositionKey.Location = new System.Drawing.Point(13, 258);
            this.lblNeedleMeasurePositionKey.Name = "lblNeedleMeasurePositionKey";
            this.lblNeedleMeasurePositionKey.Size = new System.Drawing.Size(206, 37);
            this.lblNeedleMeasurePositionKey.TabIndex = 16;
            this.lblNeedleMeasurePositionKey.Text = "NEEDLE MEASURE POSITION";
            this.lblNeedleMeasurePositionKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleMeasurePositionValue
            // 
            this.lblNeedleMeasurePositionValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleMeasurePositionValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleMeasurePositionValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleMeasurePositionValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedleMeasurePositionValue.Location = new System.Drawing.Point(225, 258);
            this.lblNeedleMeasurePositionValue.Name = "lblNeedleMeasurePositionValue";
            this.lblNeedleMeasurePositionValue.Size = new System.Drawing.Size(168, 37);
            this.lblNeedleMeasurePositionValue.TabIndex = 17;
            this.lblNeedleMeasurePositionValue.Text = "SET";
            this.lblNeedleMeasurePositionValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpWait
            // 
            this.grpWait.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpWait.Controls.Add(this.waitLayout);
            this.grpWait.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWait.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpWait.Location = new System.Drawing.Point(4, 344);
            this.grpWait.Margin = new System.Windows.Forms.Padding(4);
            this.grpWait.Name = "grpWait";
            this.grpWait.Size = new System.Drawing.Size(412, 142);
            this.grpWait.TabIndex = 1;
            this.grpWait.TabStop = false;
            this.grpWait.Text = "WAIT TIME";
            // 
            // waitLayout
            // 
            this.waitLayout.ColumnCount = 2;
            this.waitLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.waitLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.waitLayout.Controls.Add(this.lblNeedleUpWaitKey, 0, 0);
            this.waitLayout.Controls.Add(this.lblNeedleUpWaitValue, 1, 0);
            this.waitLayout.Controls.Add(this.lblVacuumOnWaitKey, 0, 1);
            this.waitLayout.Controls.Add(this.lblVacuumOnWaitValue, 1, 1);
            this.waitLayout.Controls.Add(this.lblNeedleDownWaitKey, 0, 2);
            this.waitLayout.Controls.Add(this.lblNeedleDownWaitValue, 1, 2);
            this.waitLayout.Controls.Add(this.lblVacuumOffWaitKey, 0, 3);
            this.waitLayout.Controls.Add(this.lblVacuumOffWaitValue, 1, 3);
            this.waitLayout.Controls.Add(this.lblMovingWaitKey, 0, 4);
            this.waitLayout.Controls.Add(this.lblMovingWaitValue, 1, 4);
            this.waitLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waitLayout.Location = new System.Drawing.Point(3, 26);
            this.waitLayout.Name = "waitLayout";
            this.waitLayout.Padding = new System.Windows.Forms.Padding(10, 18, 10, 8);
            this.waitLayout.RowCount = 5;
            this.waitLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.waitLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.waitLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.waitLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.waitLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.waitLayout.Size = new System.Drawing.Size(406, 113);
            this.waitLayout.TabIndex = 0;
            // 
            // lblNeedleUpWaitKey
            // 
            this.lblNeedleUpWaitKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedleUpWaitKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleUpWaitKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleUpWaitKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNeedleUpWaitKey.Location = new System.Drawing.Point(13, 18);
            this.lblNeedleUpWaitKey.Name = "lblNeedleUpWaitKey";
            this.lblNeedleUpWaitKey.Size = new System.Drawing.Size(206, 30);
            this.lblNeedleUpWaitKey.TabIndex = 0;
            this.lblNeedleUpWaitKey.Text = "NEEDLE UP";
            this.lblNeedleUpWaitKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleUpWaitValue
            // 
            this.lblNeedleUpWaitValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleUpWaitValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleUpWaitValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleUpWaitValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedleUpWaitValue.Location = new System.Drawing.Point(225, 18);
            this.lblNeedleUpWaitValue.Name = "lblNeedleUpWaitValue";
            this.lblNeedleUpWaitValue.Size = new System.Drawing.Size(168, 30);
            this.lblNeedleUpWaitValue.TabIndex = 1;
            this.lblNeedleUpWaitValue.Text = "0 ms";
            this.lblNeedleUpWaitValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblVacuumOnWaitKey
            // 
            this.lblVacuumOnWaitKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblVacuumOnWaitKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVacuumOnWaitKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVacuumOnWaitKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblVacuumOnWaitKey.Location = new System.Drawing.Point(13, 48);
            this.lblVacuumOnWaitKey.Name = "lblVacuumOnWaitKey";
            this.lblVacuumOnWaitKey.Size = new System.Drawing.Size(206, 30);
            this.lblVacuumOnWaitKey.TabIndex = 2;
            this.lblVacuumOnWaitKey.Text = "VACUUM ON";
            this.lblVacuumOnWaitKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVacuumOnWaitValue
            // 
            this.lblVacuumOnWaitValue.BackColor = System.Drawing.Color.White;
            this.lblVacuumOnWaitValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVacuumOnWaitValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVacuumOnWaitValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblVacuumOnWaitValue.Location = new System.Drawing.Point(225, 48);
            this.lblVacuumOnWaitValue.Name = "lblVacuumOnWaitValue";
            this.lblVacuumOnWaitValue.Size = new System.Drawing.Size(168, 30);
            this.lblVacuumOnWaitValue.TabIndex = 3;
            this.lblVacuumOnWaitValue.Text = "0 ms";
            this.lblVacuumOnWaitValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNeedleDownWaitKey
            // 
            this.lblNeedleDownWaitKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedleDownWaitKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleDownWaitKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleDownWaitKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNeedleDownWaitKey.Location = new System.Drawing.Point(13, 78);
            this.lblNeedleDownWaitKey.Name = "lblNeedleDownWaitKey";
            this.lblNeedleDownWaitKey.Size = new System.Drawing.Size(206, 30);
            this.lblNeedleDownWaitKey.TabIndex = 4;
            this.lblNeedleDownWaitKey.Text = "NEEDLE DOWN";
            this.lblNeedleDownWaitKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleDownWaitValue
            // 
            this.lblNeedleDownWaitValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleDownWaitValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleDownWaitValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleDownWaitValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedleDownWaitValue.Location = new System.Drawing.Point(225, 78);
            this.lblNeedleDownWaitValue.Name = "lblNeedleDownWaitValue";
            this.lblNeedleDownWaitValue.Size = new System.Drawing.Size(168, 30);
            this.lblNeedleDownWaitValue.TabIndex = 5;
            this.lblNeedleDownWaitValue.Text = "0 ms";
            this.lblNeedleDownWaitValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblVacuumOffWaitKey
            // 
            this.lblVacuumOffWaitKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblVacuumOffWaitKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVacuumOffWaitKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVacuumOffWaitKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblVacuumOffWaitKey.Location = new System.Drawing.Point(13, 108);
            this.lblVacuumOffWaitKey.Name = "lblVacuumOffWaitKey";
            this.lblVacuumOffWaitKey.Size = new System.Drawing.Size(206, 30);
            this.lblVacuumOffWaitKey.TabIndex = 6;
            this.lblVacuumOffWaitKey.Text = "VACUUM OFF";
            this.lblVacuumOffWaitKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVacuumOffWaitValue
            // 
            this.lblVacuumOffWaitValue.BackColor = System.Drawing.Color.White;
            this.lblVacuumOffWaitValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVacuumOffWaitValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVacuumOffWaitValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblVacuumOffWaitValue.Location = new System.Drawing.Point(225, 108);
            this.lblVacuumOffWaitValue.Name = "lblVacuumOffWaitValue";
            this.lblVacuumOffWaitValue.Size = new System.Drawing.Size(168, 30);
            this.lblVacuumOffWaitValue.TabIndex = 7;
            this.lblVacuumOffWaitValue.Text = "0 ms";
            this.lblVacuumOffWaitValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMovingWaitKey
            // 
            this.lblMovingWaitKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblMovingWaitKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMovingWaitKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMovingWaitKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblMovingWaitKey.Location = new System.Drawing.Point(13, 138);
            this.lblMovingWaitKey.Name = "lblMovingWaitKey";
            this.lblMovingWaitKey.Size = new System.Drawing.Size(206, 30);
            this.lblMovingWaitKey.TabIndex = 8;
            this.lblMovingWaitKey.Text = "MOVING";
            this.lblMovingWaitKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMovingWaitValue
            // 
            this.lblMovingWaitValue.BackColor = System.Drawing.Color.White;
            this.lblMovingWaitValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblMovingWaitValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMovingWaitValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblMovingWaitValue.Location = new System.Drawing.Point(225, 138);
            this.lblMovingWaitValue.Name = "lblMovingWaitValue";
            this.lblMovingWaitValue.Size = new System.Drawing.Size(168, 30);
            this.lblMovingWaitValue.TabIndex = 9;
            this.lblMovingWaitValue.Text = "0 ms";
            this.lblMovingWaitValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpManual
            // 
            this.grpManual.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpManual.Controls.Add(this.manualLayout);
            this.grpManual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpManual.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpManual.Location = new System.Drawing.Point(4, 494);
            this.grpManual.Margin = new System.Windows.Forms.Padding(4);
            this.grpManual.Name = "grpManual";
            this.grpManual.Size = new System.Drawing.Size(412, 292);
            this.grpManual.TabIndex = 2;
            this.grpManual.TabStop = false;
            this.grpManual.Text = "MANUAL ACTION";
            // 
            // manualLayout
            // 
            this.manualLayout.ColumnCount = 2;
            this.manualLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.manualLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.manualLayout.Controls.Add(this.btnLoadingMove, 0, 0);
            this.manualLayout.Controls.Add(this.btnCenterMove, 1, 0);
            this.manualLayout.Controls.Add(this.btnBarcodeMove, 0, 1);
            this.manualLayout.Controls.Add(this.btnFirstDieMove, 1, 1);
            this.manualLayout.Controls.Add(this.btnPickUpTest, 0, 2);
            this.manualLayout.Controls.Add(this.btnNeedleUpMove, 1, 2);
            this.manualLayout.Controls.Add(this.btnNeedleDownMove, 0, 3);
            this.manualLayout.Controls.Add(this.btnNeedleReadyMove, 1, 3);
            this.manualLayout.Controls.Add(this.btnNeedleBlockReady, 0, 4);
            this.manualLayout.Controls.Add(this.btnNeedleBlockWork, 1, 4);
            this.manualLayout.Controls.Add(this.btnAutoSettingMove, 0, 5);
            this.manualLayout.Controls.Add(this.btnInputConversion, 1, 5);
            this.manualLayout.Controls.Add(this.btnExpandWorkMove, 0, 6);
            this.manualLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.manualLayout.Location = new System.Drawing.Point(3, 26);
            this.manualLayout.Name = "manualLayout";
            this.manualLayout.Padding = new System.Windows.Forms.Padding(8, 18, 8, 8);
            this.manualLayout.RowCount = 7;
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.manualLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.29F));
            this.manualLayout.Size = new System.Drawing.Size(406, 263);
            this.manualLayout.TabIndex = 0;
            // 
            // btnLoadingMove
            // 
            this.btnLoadingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnLoadingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadingMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnLoadingMove.ForeColor = System.Drawing.Color.White;
            this.btnLoadingMove.Location = new System.Drawing.Point(12, 22);
            this.btnLoadingMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnLoadingMove.Name = "btnLoadingMove";
            this.btnLoadingMove.Size = new System.Drawing.Size(187, 25);
            this.btnLoadingMove.TabIndex = 0;
            this.btnLoadingMove.Text = "LOADING MOVE";
            // 
            // btnCenterMove
            // 
            this.btnCenterMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnCenterMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCenterMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCenterMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnCenterMove.ForeColor = System.Drawing.Color.White;
            this.btnCenterMove.Location = new System.Drawing.Point(207, 22);
            this.btnCenterMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnCenterMove.Name = "btnCenterMove";
            this.btnCenterMove.Size = new System.Drawing.Size(187, 25);
            this.btnCenterMove.TabIndex = 1;
            this.btnCenterMove.Text = "CENTER MOVE";
            // 
            // btnBarcodeMove
            // 
            this.btnBarcodeMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBarcodeMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBarcodeMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBarcodeMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnBarcodeMove.ForeColor = System.Drawing.Color.White;
            this.btnBarcodeMove.Location = new System.Drawing.Point(12, 55);
            this.btnBarcodeMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnBarcodeMove.Name = "btnBarcodeMove";
            this.btnBarcodeMove.Size = new System.Drawing.Size(187, 25);
            this.btnBarcodeMove.TabIndex = 2;
            this.btnBarcodeMove.Text = "BARCODE MOVE";
            // 
            // btnFirstDieMove
            // 
            this.btnFirstDieMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnFirstDieMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFirstDieMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFirstDieMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnFirstDieMove.ForeColor = System.Drawing.Color.White;
            this.btnFirstDieMove.Location = new System.Drawing.Point(207, 55);
            this.btnFirstDieMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnFirstDieMove.Name = "btnFirstDieMove";
            this.btnFirstDieMove.Size = new System.Drawing.Size(187, 25);
            this.btnFirstDieMove.TabIndex = 3;
            this.btnFirstDieMove.Text = "FIRST DIE MOVE";
            // 
            // btnPickUpTest
            // 
            this.btnPickUpTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPickUpTest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickUpTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickUpTest.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnPickUpTest.ForeColor = System.Drawing.Color.White;
            this.btnPickUpTest.Location = new System.Drawing.Point(12, 88);
            this.btnPickUpTest.Margin = new System.Windows.Forms.Padding(4);
            this.btnPickUpTest.Name = "btnPickUpTest";
            this.btnPickUpTest.Size = new System.Drawing.Size(187, 25);
            this.btnPickUpTest.TabIndex = 4;
            this.btnPickUpTest.Text = "PICK UP TEST";
            // 
            // btnNeedleUpMove
            // 
            this.btnNeedleUpMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleUpMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleUpMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleUpMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNeedleUpMove.ForeColor = System.Drawing.Color.White;
            this.btnNeedleUpMove.Location = new System.Drawing.Point(207, 88);
            this.btnNeedleUpMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnNeedleUpMove.Name = "btnNeedleUpMove";
            this.btnNeedleUpMove.Size = new System.Drawing.Size(187, 25);
            this.btnNeedleUpMove.TabIndex = 5;
            this.btnNeedleUpMove.Text = "NEEDLE UP MOVE";
            // 
            // btnNeedleDownMove
            // 
            this.btnNeedleDownMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleDownMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleDownMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleDownMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNeedleDownMove.ForeColor = System.Drawing.Color.White;
            this.btnNeedleDownMove.Location = new System.Drawing.Point(12, 121);
            this.btnNeedleDownMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnNeedleDownMove.Name = "btnNeedleDownMove";
            this.btnNeedleDownMove.Size = new System.Drawing.Size(187, 25);
            this.btnNeedleDownMove.TabIndex = 6;
            this.btnNeedleDownMove.Text = "NEEDLE DOWN MOVE";
            // 
            // btnNeedleReadyMove
            // 
            this.btnNeedleReadyMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleReadyMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleReadyMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleReadyMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNeedleReadyMove.ForeColor = System.Drawing.Color.White;
            this.btnNeedleReadyMove.Location = new System.Drawing.Point(207, 121);
            this.btnNeedleReadyMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnNeedleReadyMove.Name = "btnNeedleReadyMove";
            this.btnNeedleReadyMove.Size = new System.Drawing.Size(187, 25);
            this.btnNeedleReadyMove.TabIndex = 7;
            this.btnNeedleReadyMove.Text = "NEEDLE READY MOVE";
            // 
            // btnNeedleBlockReady
            // 
            this.btnNeedleBlockReady.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleBlockReady.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleBlockReady.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleBlockReady.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNeedleBlockReady.ForeColor = System.Drawing.Color.White;
            this.btnNeedleBlockReady.Location = new System.Drawing.Point(12, 154);
            this.btnNeedleBlockReady.Margin = new System.Windows.Forms.Padding(4);
            this.btnNeedleBlockReady.Name = "btnNeedleBlockReady";
            this.btnNeedleBlockReady.Size = new System.Drawing.Size(187, 25);
            this.btnNeedleBlockReady.TabIndex = 8;
            this.btnNeedleBlockReady.Text = "NEEDLE BLOCK READY";
            // 
            // btnNeedleBlockWork
            // 
            this.btnNeedleBlockWork.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleBlockWork.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleBlockWork.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleBlockWork.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNeedleBlockWork.ForeColor = System.Drawing.Color.White;
            this.btnNeedleBlockWork.Location = new System.Drawing.Point(207, 154);
            this.btnNeedleBlockWork.Margin = new System.Windows.Forms.Padding(4);
            this.btnNeedleBlockWork.Name = "btnNeedleBlockWork";
            this.btnNeedleBlockWork.Size = new System.Drawing.Size(187, 25);
            this.btnNeedleBlockWork.TabIndex = 9;
            this.btnNeedleBlockWork.Text = "NEEDLE BLOCK WORK";
            // 
            // btnAutoSettingMove
            // 
            this.btnAutoSettingMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAutoSettingMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAutoSettingMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAutoSettingMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnAutoSettingMove.ForeColor = System.Drawing.Color.White;
            this.btnAutoSettingMove.Location = new System.Drawing.Point(12, 187);
            this.btnAutoSettingMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnAutoSettingMove.Name = "btnAutoSettingMove";
            this.btnAutoSettingMove.Size = new System.Drawing.Size(187, 25);
            this.btnAutoSettingMove.TabIndex = 10;
            this.btnAutoSettingMove.Text = "AUTO SETTING MOVE";
            // 
            // btnInputConversion
            // 
            this.btnInputConversion.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInputConversion.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInputConversion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInputConversion.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnInputConversion.ForeColor = System.Drawing.Color.White;
            this.btnInputConversion.Location = new System.Drawing.Point(207, 187);
            this.btnInputConversion.Margin = new System.Windows.Forms.Padding(4);
            this.btnInputConversion.Name = "btnInputConversion";
            this.btnInputConversion.Size = new System.Drawing.Size(187, 25);
            this.btnInputConversion.TabIndex = 11;
            this.btnInputConversion.Text = "INPUT CONVERSION";
            // 
            // btnExpandWorkMove
            // 
            this.btnExpandWorkMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnExpandWorkMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExpandWorkMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExpandWorkMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnExpandWorkMove.ForeColor = System.Drawing.Color.White;
            this.btnExpandWorkMove.Location = new System.Drawing.Point(12, 220);
            this.btnExpandWorkMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnExpandWorkMove.Name = "btnExpandWorkMove";
            this.btnExpandWorkMove.Size = new System.Drawing.Size(187, 31);
            this.btnExpandWorkMove.TabIndex = 12;
            this.btnExpandWorkMove.Text = "EXPAND WORK MOVE";
            // 
            // grpIo
            // 
            this.grpIo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpIo.Controls.Add(this.ioLayout);
            this.grpIo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpIo.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpIo.Location = new System.Drawing.Point(4, 794);
            this.grpIo.Margin = new System.Windows.Forms.Padding(4);
            this.grpIo.Name = "grpIo";
            this.grpIo.Size = new System.Drawing.Size(412, 56);
            this.grpIo.TabIndex = 3;
            this.grpIo.TabStop = false;
            this.grpIo.Text = "CYLINDER && I/O";
            // 
            // ioLayout
            // 
            this.ioLayout.ColumnCount = 3;
            this.ioLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.ioLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.ioLayout.Controls.Add(this.dotNeedleVacuum, 0, 0);
            this.ioLayout.Controls.Add(this.lblNeedleVacuumKey, 1, 0);
            this.ioLayout.Controls.Add(this.lblNeedleVacuumValue, 2, 0);
            this.ioLayout.Controls.Add(this.dotRingSensor, 0, 1);
            this.ioLayout.Controls.Add(this.lblRingSensorKey, 1, 1);
            this.ioLayout.Controls.Add(this.lblRingSensorValue, 2, 1);
            this.ioLayout.Controls.Add(this.dotExpandCylinder, 0, 2);
            this.ioLayout.Controls.Add(this.lblExpandCylinderKey, 1, 2);
            this.ioLayout.Controls.Add(this.lblExpandCylinderValue, 2, 2);
            this.ioLayout.Controls.Add(this.dotNeedleBlock, 0, 3);
            this.ioLayout.Controls.Add(this.lblNeedleBlockKey, 1, 3);
            this.ioLayout.Controls.Add(this.lblNeedleBlockValue, 2, 3);
            this.ioLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ioLayout.Location = new System.Drawing.Point(3, 26);
            this.ioLayout.Name = "ioLayout";
            this.ioLayout.Padding = new System.Windows.Forms.Padding(8, 18, 8, 8);
            this.ioLayout.RowCount = 4;
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.ioLayout.Size = new System.Drawing.Size(406, 27);
            this.ioLayout.TabIndex = 0;
            // 
            // dotNeedleVacuum
            // 
            this.dotNeedleVacuum.BackColor = System.Drawing.Color.Transparent;
            this.dotNeedleVacuum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotNeedleVacuum.Location = new System.Drawing.Point(15, 25);
            this.dotNeedleVacuum.Margin = new System.Windows.Forms.Padding(7);
            this.dotNeedleVacuum.Name = "dotNeedleVacuum";
            this.dotNeedleVacuum.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotNeedleVacuum.OnColor = System.Drawing.Color.LimeGreen;
            this.dotNeedleVacuum.Size = new System.Drawing.Size(14, 14);
            this.dotNeedleVacuum.TabIndex = 0;
            // 
            // lblNeedleVacuumKey
            // 
            this.lblNeedleVacuumKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedleVacuumKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleVacuumKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleVacuumKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNeedleVacuumKey.Location = new System.Drawing.Point(39, 18);
            this.lblNeedleVacuumKey.Name = "lblNeedleVacuumKey";
            this.lblNeedleVacuumKey.Size = new System.Drawing.Size(203, 28);
            this.lblNeedleVacuumKey.TabIndex = 1;
            this.lblNeedleVacuumKey.Text = "NEEDLE VACUUM";
            this.lblNeedleVacuumKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleVacuumValue
            // 
            this.lblNeedleVacuumValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleVacuumValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleVacuumValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleVacuumValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedleVacuumValue.Location = new System.Drawing.Point(248, 18);
            this.lblNeedleVacuumValue.Name = "lblNeedleVacuumValue";
            this.lblNeedleVacuumValue.Size = new System.Drawing.Size(147, 28);
            this.lblNeedleVacuumValue.TabIndex = 2;
            this.lblNeedleVacuumValue.Text = "VACUUM";
            this.lblNeedleVacuumValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dotRingSensor
            // 
            this.dotRingSensor.BackColor = System.Drawing.Color.Transparent;
            this.dotRingSensor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotRingSensor.Location = new System.Drawing.Point(15, 53);
            this.dotRingSensor.Margin = new System.Windows.Forms.Padding(7);
            this.dotRingSensor.Name = "dotRingSensor";
            this.dotRingSensor.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotRingSensor.OnColor = System.Drawing.Color.LimeGreen;
            this.dotRingSensor.Size = new System.Drawing.Size(14, 14);
            this.dotRingSensor.TabIndex = 3;
            // 
            // lblRingSensorKey
            // 
            this.lblRingSensorKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblRingSensorKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRingSensorKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRingSensorKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblRingSensorKey.Location = new System.Drawing.Point(39, 46);
            this.lblRingSensorKey.Name = "lblRingSensorKey";
            this.lblRingSensorKey.Size = new System.Drawing.Size(203, 28);
            this.lblRingSensorKey.TabIndex = 4;
            this.lblRingSensorKey.Text = "RING SENSOR";
            this.lblRingSensorKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRingSensorValue
            // 
            this.lblRingSensorValue.BackColor = System.Drawing.Color.White;
            this.lblRingSensorValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRingSensorValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRingSensorValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblRingSensorValue.Location = new System.Drawing.Point(248, 46);
            this.lblRingSensorValue.Name = "lblRingSensorValue";
            this.lblRingSensorValue.Size = new System.Drawing.Size(147, 28);
            this.lblRingSensorValue.TabIndex = 5;
            this.lblRingSensorValue.Text = "OFF";
            this.lblRingSensorValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dotExpandCylinder
            // 
            this.dotExpandCylinder.BackColor = System.Drawing.Color.Transparent;
            this.dotExpandCylinder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotExpandCylinder.Location = new System.Drawing.Point(15, 81);
            this.dotExpandCylinder.Margin = new System.Windows.Forms.Padding(7);
            this.dotExpandCylinder.Name = "dotExpandCylinder";
            this.dotExpandCylinder.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotExpandCylinder.OnColor = System.Drawing.Color.LimeGreen;
            this.dotExpandCylinder.Size = new System.Drawing.Size(14, 14);
            this.dotExpandCylinder.TabIndex = 6;
            // 
            // lblExpandCylinderKey
            // 
            this.lblExpandCylinderKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblExpandCylinderKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblExpandCylinderKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExpandCylinderKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblExpandCylinderKey.Location = new System.Drawing.Point(39, 74);
            this.lblExpandCylinderKey.Name = "lblExpandCylinderKey";
            this.lblExpandCylinderKey.Size = new System.Drawing.Size(203, 28);
            this.lblExpandCylinderKey.TabIndex = 7;
            this.lblExpandCylinderKey.Text = "EXPAND CYLINDER";
            this.lblExpandCylinderKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblExpandCylinderValue
            // 
            this.lblExpandCylinderValue.BackColor = System.Drawing.Color.White;
            this.lblExpandCylinderValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblExpandCylinderValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExpandCylinderValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblExpandCylinderValue.Location = new System.Drawing.Point(248, 74);
            this.lblExpandCylinderValue.Name = "lblExpandCylinderValue";
            this.lblExpandCylinderValue.Size = new System.Drawing.Size(147, 28);
            this.lblExpandCylinderValue.TabIndex = 8;
            this.lblExpandCylinderValue.Text = "READY";
            this.lblExpandCylinderValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dotNeedleBlock
            // 
            this.dotNeedleBlock.BackColor = System.Drawing.Color.Transparent;
            this.dotNeedleBlock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotNeedleBlock.Location = new System.Drawing.Point(15, 109);
            this.dotNeedleBlock.Margin = new System.Windows.Forms.Padding(7);
            this.dotNeedleBlock.Name = "dotNeedleBlock";
            this.dotNeedleBlock.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotNeedleBlock.OnColor = System.Drawing.Color.LimeGreen;
            this.dotNeedleBlock.Size = new System.Drawing.Size(14, 14);
            this.dotNeedleBlock.TabIndex = 9;
            // 
            // lblNeedleBlockKey
            // 
            this.lblNeedleBlockKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedleBlockKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleBlockKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleBlockKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNeedleBlockKey.Location = new System.Drawing.Point(39, 102);
            this.lblNeedleBlockKey.Name = "lblNeedleBlockKey";
            this.lblNeedleBlockKey.Size = new System.Drawing.Size(203, 28);
            this.lblNeedleBlockKey.TabIndex = 10;
            this.lblNeedleBlockKey.Text = "NEEDLE BLOCK";
            this.lblNeedleBlockKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleBlockValue
            // 
            this.lblNeedleBlockValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleBlockValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleBlockValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleBlockValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedleBlockValue.Location = new System.Drawing.Point(248, 102);
            this.lblNeedleBlockValue.Name = "lblNeedleBlockValue";
            this.lblNeedleBlockValue.Size = new System.Drawing.Size(147, 28);
            this.lblNeedleBlockValue.TabIndex = 11;
            this.lblNeedleBlockValue.Text = "READY";
            this.lblNeedleBlockValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // centerLayout
            // 
            this.centerLayout.ColumnCount = 1;
            this.centerLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.centerLayout.Controls.Add(this.grpVision, 0, 0);
            this.centerLayout.Controls.Add(this.grpJog, 0, 1);
            this.centerLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.centerLayout.Location = new System.Drawing.Point(428, 8);
            this.centerLayout.Margin = new System.Windows.Forms.Padding(0);
            this.centerLayout.Name = "centerLayout";
            this.centerLayout.RowCount = 2;
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 62F));
            this.centerLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 38F));
            this.centerLayout.Size = new System.Drawing.Size(794, 854);
            this.centerLayout.TabIndex = 1;
            // 
            // grpVision
            // 
            this.grpVision.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpVision.Controls.Add(this.visionPanel);
            this.grpVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpVision.Location = new System.Drawing.Point(4, 4);
            this.grpVision.Margin = new System.Windows.Forms.Padding(4);
            this.grpVision.Name = "grpVision";
            this.grpVision.Size = new System.Drawing.Size(786, 521);
            this.grpVision.TabIndex = 0;
            this.grpVision.TabStop = false;
            this.grpVision.Text = "STAGE VISION";
            // 
            // visionPanel
            // 
            this.visionPanel.BackColor = System.Drawing.Color.Black;
            this.visionPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.visionPanel.Controls.Add(this.lblVisionInfo);
            this.visionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionPanel.Location = new System.Drawing.Point(3, 26);
            this.visionPanel.Margin = new System.Windows.Forms.Padding(10);
            this.visionPanel.Name = "visionPanel";
            this.visionPanel.Size = new System.Drawing.Size(780, 492);
            this.visionPanel.TabIndex = 0;
            // 
            // lblVisionInfo
            // 
            this.lblVisionInfo.AutoSize = true;
            this.lblVisionInfo.BackColor = System.Drawing.Color.Black;
            this.lblVisionInfo.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblVisionInfo.ForeColor = System.Drawing.Color.Lime;
            this.lblVisionInfo.Location = new System.Drawing.Point(10, 10);
            this.lblVisionInfo.Name = "lblVisionInfo";
            this.lblVisionInfo.Size = new System.Drawing.Size(72, 114);
            this.lblVisionInfo.TabIndex = 0;
            this.lblVisionInfo.Text = "STAGE\r\nW : 640\r\nH : 480\r\nX : 0\r\nY : 0\r\nT : 0";
            // 
            // grpJog
            // 
            this.grpJog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpJog.Controls.Add(this.jogLayout);
            this.grpJog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJog.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpJog.Location = new System.Drawing.Point(4, 533);
            this.grpJog.Margin = new System.Windows.Forms.Padding(4);
            this.grpJog.Name = "grpJog";
            this.grpJog.Size = new System.Drawing.Size(786, 317);
            this.grpJog.TabIndex = 1;
            this.grpJog.TabStop = false;
            this.grpJog.Text = "JOG OPERATION";
            // 
            // jogLayout
            // 
            this.jogLayout.ColumnCount = 3;
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.jogLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.jogLayout.Controls.Add(this.jogAxisLayout, 0, 0);
            this.jogLayout.Controls.Add(this.jogXyLayout, 1, 0);
            this.jogLayout.Controls.Add(this.jogZLayout, 2, 0);
            this.jogLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogLayout.Location = new System.Drawing.Point(3, 26);
            this.jogLayout.Name = "jogLayout";
            this.jogLayout.Padding = new System.Windows.Forms.Padding(10, 18, 10, 10);
            this.jogLayout.RowCount = 1;
            this.jogLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.jogLayout.Size = new System.Drawing.Size(780, 288);
            this.jogLayout.TabIndex = 0;
            // 
            // jogAxisLayout
            // 
            this.jogAxisLayout.ColumnCount = 2;
            this.jogAxisLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.jogAxisLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.jogAxisLayout.Controls.Add(this.lblAxisXKey, 0, 0);
            this.jogAxisLayout.Controls.Add(this.lblAxisXValue, 1, 0);
            this.jogAxisLayout.Controls.Add(this.lblAxisYKey, 0, 1);
            this.jogAxisLayout.Controls.Add(this.lblAxisYValue, 1, 1);
            this.jogAxisLayout.Controls.Add(this.lblAxisTKey, 0, 2);
            this.jogAxisLayout.Controls.Add(this.lblAxisTValue, 1, 2);
            this.jogAxisLayout.Controls.Add(this.lblExpandZKey, 0, 3);
            this.jogAxisLayout.Controls.Add(this.lblExpandZValue, 1, 3);
            this.jogAxisLayout.Controls.Add(this.lblNeedleZKey, 0, 4);
            this.jogAxisLayout.Controls.Add(this.lblNeedleZValue, 1, 4);
            this.jogAxisLayout.Controls.Add(this.lblNeedleBlockZKey, 0, 5);
            this.jogAxisLayout.Controls.Add(this.lblNeedleBlockZValue, 1, 5);
            this.jogAxisLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogAxisLayout.Location = new System.Drawing.Point(13, 21);
            this.jogAxisLayout.Name = "jogAxisLayout";
            this.jogAxisLayout.RowCount = 6;
            this.jogAxisLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.jogAxisLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.jogAxisLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.jogAxisLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.jogAxisLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.jogAxisLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.7F));
            this.jogAxisLayout.Size = new System.Drawing.Size(267, 254);
            this.jogAxisLayout.TabIndex = 0;
            // 
            // lblAxisXKey
            // 
            this.lblAxisXKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblAxisXKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisXKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisXKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblAxisXKey.Location = new System.Drawing.Point(3, 0);
            this.lblAxisXKey.Name = "lblAxisXKey";
            this.lblAxisXKey.Size = new System.Drawing.Size(127, 42);
            this.lblAxisXKey.TabIndex = 0;
            this.lblAxisXKey.Text = "AXIS X";
            this.lblAxisXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxisXValue
            // 
            this.lblAxisXValue.BackColor = System.Drawing.Color.White;
            this.lblAxisXValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisXValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisXValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblAxisXValue.Location = new System.Drawing.Point(136, 0);
            this.lblAxisXValue.Name = "lblAxisXValue";
            this.lblAxisXValue.Size = new System.Drawing.Size(128, 42);
            this.lblAxisXValue.TabIndex = 1;
            this.lblAxisXValue.Text = "0 um";
            this.lblAxisXValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblAxisYKey
            // 
            this.lblAxisYKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblAxisYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisYKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblAxisYKey.Location = new System.Drawing.Point(3, 42);
            this.lblAxisYKey.Name = "lblAxisYKey";
            this.lblAxisYKey.Size = new System.Drawing.Size(127, 42);
            this.lblAxisYKey.TabIndex = 2;
            this.lblAxisYKey.Text = "AXIS Y";
            this.lblAxisYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxisYValue
            // 
            this.lblAxisYValue.BackColor = System.Drawing.Color.White;
            this.lblAxisYValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisYValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisYValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblAxisYValue.Location = new System.Drawing.Point(136, 42);
            this.lblAxisYValue.Name = "lblAxisYValue";
            this.lblAxisYValue.Size = new System.Drawing.Size(128, 42);
            this.lblAxisYValue.TabIndex = 3;
            this.lblAxisYValue.Text = "0 um";
            this.lblAxisYValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblAxisTKey
            // 
            this.lblAxisTKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblAxisTKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisTKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisTKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblAxisTKey.Location = new System.Drawing.Point(3, 84);
            this.lblAxisTKey.Name = "lblAxisTKey";
            this.lblAxisTKey.Size = new System.Drawing.Size(127, 42);
            this.lblAxisTKey.TabIndex = 4;
            this.lblAxisTKey.Text = "AXIS T";
            this.lblAxisTKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxisTValue
            // 
            this.lblAxisTValue.BackColor = System.Drawing.Color.White;
            this.lblAxisTValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisTValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisTValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblAxisTValue.Location = new System.Drawing.Point(136, 84);
            this.lblAxisTValue.Name = "lblAxisTValue";
            this.lblAxisTValue.Size = new System.Drawing.Size(128, 42);
            this.lblAxisTValue.TabIndex = 5;
            this.lblAxisTValue.Text = "0 deg";
            this.lblAxisTValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblExpandZKey
            // 
            this.lblExpandZKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblExpandZKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblExpandZKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExpandZKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblExpandZKey.Location = new System.Drawing.Point(3, 126);
            this.lblExpandZKey.Name = "lblExpandZKey";
            this.lblExpandZKey.Size = new System.Drawing.Size(127, 42);
            this.lblExpandZKey.TabIndex = 6;
            this.lblExpandZKey.Text = "EXPAND Z";
            this.lblExpandZKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblExpandZValue
            // 
            this.lblExpandZValue.BackColor = System.Drawing.Color.White;
            this.lblExpandZValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblExpandZValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExpandZValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblExpandZValue.Location = new System.Drawing.Point(136, 126);
            this.lblExpandZValue.Name = "lblExpandZValue";
            this.lblExpandZValue.Size = new System.Drawing.Size(128, 42);
            this.lblExpandZValue.TabIndex = 7;
            this.lblExpandZValue.Text = "0 um";
            this.lblExpandZValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNeedleZKey
            // 
            this.lblNeedleZKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedleZKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleZKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleZKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNeedleZKey.Location = new System.Drawing.Point(3, 168);
            this.lblNeedleZKey.Name = "lblNeedleZKey";
            this.lblNeedleZKey.Size = new System.Drawing.Size(127, 42);
            this.lblNeedleZKey.TabIndex = 8;
            this.lblNeedleZKey.Text = "NEEDLE Z";
            this.lblNeedleZKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleZValue
            // 
            this.lblNeedleZValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleZValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleZValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleZValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedleZValue.Location = new System.Drawing.Point(136, 168);
            this.lblNeedleZValue.Name = "lblNeedleZValue";
            this.lblNeedleZValue.Size = new System.Drawing.Size(128, 42);
            this.lblNeedleZValue.TabIndex = 9;
            this.lblNeedleZValue.Text = "0 um";
            this.lblNeedleZValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblNeedleBlockZKey
            // 
            this.lblNeedleBlockZKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedleBlockZKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleBlockZKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleBlockZKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNeedleBlockZKey.Location = new System.Drawing.Point(3, 210);
            this.lblNeedleBlockZKey.Name = "lblNeedleBlockZKey";
            this.lblNeedleBlockZKey.Size = new System.Drawing.Size(127, 44);
            this.lblNeedleBlockZKey.TabIndex = 10;
            this.lblNeedleBlockZKey.Text = "NEEDLE BLOCK Z";
            this.lblNeedleBlockZKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleBlockZValue
            // 
            this.lblNeedleBlockZValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleBlockZValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleBlockZValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleBlockZValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblNeedleBlockZValue.Location = new System.Drawing.Point(136, 210);
            this.lblNeedleBlockZValue.Name = "lblNeedleBlockZValue";
            this.lblNeedleBlockZValue.Size = new System.Drawing.Size(128, 44);
            this.lblNeedleBlockZValue.TabIndex = 11;
            this.lblNeedleBlockZValue.Text = "0 um";
            this.lblNeedleBlockZValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // jogXyLayout
            // 
            this.jogXyLayout.ColumnCount = 3;
            this.jogXyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogXyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.jogXyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogXyLayout.Controls.Add(this.btnJogYPlus, 1, 0);
            this.jogXyLayout.Controls.Add(this.btnJogXMinus, 0, 1);
            this.jogXyLayout.Controls.Add(this.btnJogStop, 1, 1);
            this.jogXyLayout.Controls.Add(this.btnJogXPlus, 2, 1);
            this.jogXyLayout.Controls.Add(this.btnJogTMinus, 0, 2);
            this.jogXyLayout.Controls.Add(this.btnJogYMinus, 1, 2);
            this.jogXyLayout.Controls.Add(this.btnJogTPlus, 2, 2);
            this.jogXyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogXyLayout.Location = new System.Drawing.Point(286, 21);
            this.jogXyLayout.Name = "jogXyLayout";
            this.jogXyLayout.RowCount = 3;
            this.jogXyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogXyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.jogXyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogXyLayout.Size = new System.Drawing.Size(252, 254);
            this.jogXyLayout.TabIndex = 1;
            // 
            // btnJogYPlus
            // 
            this.btnJogYPlus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnJogYPlus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnJogYPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogYPlus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogYPlus.ForeColor = System.Drawing.Color.White;
            this.btnJogYPlus.Location = new System.Drawing.Point(87, 4);
            this.btnJogYPlus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogYPlus.Name = "btnJogYPlus";
            this.btnJogYPlus.Size = new System.Drawing.Size(76, 76);
            this.btnJogYPlus.TabIndex = 0;
            this.btnJogYPlus.Text = "+Y";
            // 
            // btnJogXMinus
            // 
            this.btnJogXMinus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnJogXMinus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnJogXMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogXMinus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogXMinus.ForeColor = System.Drawing.Color.White;
            this.btnJogXMinus.Location = new System.Drawing.Point(4, 88);
            this.btnJogXMinus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogXMinus.Name = "btnJogXMinus";
            this.btnJogXMinus.Size = new System.Drawing.Size(75, 76);
            this.btnJogXMinus.TabIndex = 1;
            this.btnJogXMinus.Text = "-X";
            // 
            // btnJogStop
            // 
            this.btnJogStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnJogStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnJogStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogStop.ForeColor = System.Drawing.Color.White;
            this.btnJogStop.Location = new System.Drawing.Point(87, 88);
            this.btnJogStop.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogStop.Name = "btnJogStop";
            this.btnJogStop.Size = new System.Drawing.Size(76, 76);
            this.btnJogStop.TabIndex = 2;
            this.btnJogStop.Text = "STOP";
            // 
            // btnJogXPlus
            // 
            this.btnJogXPlus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnJogXPlus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnJogXPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogXPlus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogXPlus.ForeColor = System.Drawing.Color.White;
            this.btnJogXPlus.Location = new System.Drawing.Point(171, 88);
            this.btnJogXPlus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogXPlus.Name = "btnJogXPlus";
            this.btnJogXPlus.Size = new System.Drawing.Size(77, 76);
            this.btnJogXPlus.TabIndex = 3;
            this.btnJogXPlus.Text = "+X";
            // 
            // btnJogTMinus
            // 
            this.btnJogTMinus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnJogTMinus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnJogTMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogTMinus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogTMinus.ForeColor = System.Drawing.Color.White;
            this.btnJogTMinus.Location = new System.Drawing.Point(4, 172);
            this.btnJogTMinus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogTMinus.Name = "btnJogTMinus";
            this.btnJogTMinus.Size = new System.Drawing.Size(75, 78);
            this.btnJogTMinus.TabIndex = 4;
            this.btnJogTMinus.Text = "-T";
            // 
            // btnJogYMinus
            // 
            this.btnJogYMinus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnJogYMinus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnJogYMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogYMinus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogYMinus.ForeColor = System.Drawing.Color.White;
            this.btnJogYMinus.Location = new System.Drawing.Point(87, 172);
            this.btnJogYMinus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogYMinus.Name = "btnJogYMinus";
            this.btnJogYMinus.Size = new System.Drawing.Size(76, 78);
            this.btnJogYMinus.TabIndex = 5;
            this.btnJogYMinus.Text = "-Y";
            // 
            // btnJogTPlus
            // 
            this.btnJogTPlus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnJogTPlus.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnJogTPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogTPlus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnJogTPlus.ForeColor = System.Drawing.Color.White;
            this.btnJogTPlus.Location = new System.Drawing.Point(171, 172);
            this.btnJogTPlus.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogTPlus.Name = "btnJogTPlus";
            this.btnJogTPlus.Size = new System.Drawing.Size(77, 78);
            this.btnJogTPlus.TabIndex = 6;
            this.btnJogTPlus.Text = "+T";
            // 
            // jogZLayout
            // 
            this.jogZLayout.ColumnCount = 3;
            this.jogZLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogZLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.jogZLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogZLayout.Controls.Add(this.btnExpandUp, 0, 0);
            this.jogZLayout.Controls.Add(this.btnNeedleUp, 1, 0);
            this.jogZLayout.Controls.Add(this.btnBlockUp, 2, 0);
            this.jogZLayout.Controls.Add(this.lblExpandAxis, 0, 1);
            this.jogZLayout.Controls.Add(this.lblNeedleAxis, 1, 1);
            this.jogZLayout.Controls.Add(this.lblBlockAxis, 2, 1);
            this.jogZLayout.Controls.Add(this.btnExpandDown, 0, 2);
            this.jogZLayout.Controls.Add(this.btnNeedleDown, 1, 2);
            this.jogZLayout.Controls.Add(this.btnBlockDown, 2, 2);
            this.jogZLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogZLayout.Location = new System.Drawing.Point(544, 21);
            this.jogZLayout.Name = "jogZLayout";
            this.jogZLayout.RowCount = 3;
            this.jogZLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.jogZLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.jogZLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.jogZLayout.Size = new System.Drawing.Size(223, 254);
            this.jogZLayout.TabIndex = 2;
            // 
            // btnExpandUp
            // 
            this.btnExpandUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnExpandUp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExpandUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExpandUp.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnExpandUp.ForeColor = System.Drawing.Color.White;
            this.btnExpandUp.Location = new System.Drawing.Point(4, 4);
            this.btnExpandUp.Margin = new System.Windows.Forms.Padding(4);
            this.btnExpandUp.Name = "btnExpandUp";
            this.btnExpandUp.Size = new System.Drawing.Size(66, 93);
            this.btnExpandUp.TabIndex = 0;
            this.btnExpandUp.Text = "UP";
            // 
            // btnNeedleUp
            // 
            this.btnNeedleUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleUp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleUp.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNeedleUp.ForeColor = System.Drawing.Color.White;
            this.btnNeedleUp.Location = new System.Drawing.Point(78, 4);
            this.btnNeedleUp.Margin = new System.Windows.Forms.Padding(4);
            this.btnNeedleUp.Name = "btnNeedleUp";
            this.btnNeedleUp.Size = new System.Drawing.Size(66, 93);
            this.btnNeedleUp.TabIndex = 1;
            this.btnNeedleUp.Text = "UP";
            // 
            // btnBlockUp
            // 
            this.btnBlockUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBlockUp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBlockUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBlockUp.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnBlockUp.ForeColor = System.Drawing.Color.White;
            this.btnBlockUp.Location = new System.Drawing.Point(152, 4);
            this.btnBlockUp.Margin = new System.Windows.Forms.Padding(4);
            this.btnBlockUp.Name = "btnBlockUp";
            this.btnBlockUp.Size = new System.Drawing.Size(67, 93);
            this.btnBlockUp.TabIndex = 2;
            this.btnBlockUp.Text = "UP";
            // 
            // lblExpandAxis
            // 
            this.lblExpandAxis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblExpandAxis.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblExpandAxis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExpandAxis.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblExpandAxis.Location = new System.Drawing.Point(3, 101);
            this.lblExpandAxis.Name = "lblExpandAxis";
            this.lblExpandAxis.Size = new System.Drawing.Size(68, 50);
            this.lblExpandAxis.TabIndex = 3;
            this.lblExpandAxis.Text = "EXPAND";
            this.lblExpandAxis.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNeedleAxis
            // 
            this.lblNeedleAxis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblNeedleAxis.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleAxis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleAxis.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblNeedleAxis.Location = new System.Drawing.Point(77, 101);
            this.lblNeedleAxis.Name = "lblNeedleAxis";
            this.lblNeedleAxis.Size = new System.Drawing.Size(68, 50);
            this.lblNeedleAxis.TabIndex = 4;
            this.lblNeedleAxis.Text = "NEEDLE";
            this.lblNeedleAxis.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblBlockAxis
            // 
            this.lblBlockAxis.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblBlockAxis.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBlockAxis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBlockAxis.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblBlockAxis.Location = new System.Drawing.Point(151, 101);
            this.lblBlockAxis.Name = "lblBlockAxis";
            this.lblBlockAxis.Size = new System.Drawing.Size(69, 50);
            this.lblBlockAxis.TabIndex = 5;
            this.lblBlockAxis.Text = "BLOCK";
            this.lblBlockAxis.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnExpandDown
            // 
            this.btnExpandDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnExpandDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExpandDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExpandDown.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnExpandDown.ForeColor = System.Drawing.Color.White;
            this.btnExpandDown.Location = new System.Drawing.Point(4, 155);
            this.btnExpandDown.Margin = new System.Windows.Forms.Padding(4);
            this.btnExpandDown.Name = "btnExpandDown";
            this.btnExpandDown.Size = new System.Drawing.Size(66, 95);
            this.btnExpandDown.TabIndex = 6;
            this.btnExpandDown.Text = "DOWN";
            // 
            // btnNeedleDown
            // 
            this.btnNeedleDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleDown.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNeedleDown.ForeColor = System.Drawing.Color.White;
            this.btnNeedleDown.Location = new System.Drawing.Point(78, 155);
            this.btnNeedleDown.Margin = new System.Windows.Forms.Padding(4);
            this.btnNeedleDown.Name = "btnNeedleDown";
            this.btnNeedleDown.Size = new System.Drawing.Size(66, 95);
            this.btnNeedleDown.TabIndex = 7;
            this.btnNeedleDown.Text = "DOWN";
            // 
            // btnBlockDown
            // 
            this.btnBlockDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBlockDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBlockDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBlockDown.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnBlockDown.ForeColor = System.Drawing.Color.White;
            this.btnBlockDown.Location = new System.Drawing.Point(152, 155);
            this.btnBlockDown.Margin = new System.Windows.Forms.Padding(4);
            this.btnBlockDown.Name = "btnBlockDown";
            this.btnBlockDown.Size = new System.Drawing.Size(67, 95);
            this.btnBlockDown.TabIndex = 8;
            this.btnBlockDown.Text = "DOWN";
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.grpSpeed, 0, 0);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(1222, 8);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 1;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(170, 854);
            this.rightLayout.TabIndex = 2;
            // 
            // grpSpeed
            // 
            this.grpSpeed.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.grpSpeed.Controls.Add(this.speedLayout);
            this.grpSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSpeed.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSpeed.Location = new System.Drawing.Point(4, 4);
            this.grpSpeed.Margin = new System.Windows.Forms.Padding(4);
            this.grpSpeed.Name = "grpSpeed";
            this.grpSpeed.Size = new System.Drawing.Size(162, 846);
            this.grpSpeed.TabIndex = 0;
            this.grpSpeed.TabStop = false;
            this.grpSpeed.Text = "SPEED";
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
            this.speedLayout.Padding = new System.Windows.Forms.Padding(12, 18, 12, 12);
            this.speedLayout.RowCount = 2;
            this.speedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.speedLayout.Size = new System.Drawing.Size(156, 817);
            this.speedLayout.TabIndex = 0;
            // 
            // trkSpeed
            // 
            this.trkSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trkSpeed.Location = new System.Drawing.Point(15, 21);
            this.trkSpeed.Maximum = 100;
            this.trkSpeed.Name = "trkSpeed";
            this.trkSpeed.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trkSpeed.Size = new System.Drawing.Size(126, 749);
            this.trkSpeed.TabIndex = 0;
            this.trkSpeed.TickFrequency = 10;
            this.trkSpeed.Value = 50;
            // 
            // lblSpeedValue
            // 
            this.lblSpeedValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblSpeedValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSpeedValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpeedValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSpeedValue.Location = new System.Drawing.Point(15, 773);
            this.lblSpeedValue.Name = "lblSpeedValue";
            this.lblSpeedValue.Size = new System.Drawing.Size(126, 32);
            this.lblSpeedValue.TabIndex = 1;
            this.lblSpeedValue.Text = "50%";
            this.lblSpeedValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // StageRecipePage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "StageRecipePage";
            this.Size = new System.Drawing.Size(1400, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.grpOptions.ResumeLayout(false);
            this.optionLayout.ResumeLayout(false);
            this.grpWait.ResumeLayout(false);
            this.waitLayout.ResumeLayout(false);
            this.grpManual.ResumeLayout(false);
            this.manualLayout.ResumeLayout(false);
            this.grpIo.ResumeLayout(false);
            this.ioLayout.ResumeLayout(false);
            this.centerLayout.ResumeLayout(false);
            this.grpVision.ResumeLayout(false);
            this.visionPanel.ResumeLayout(false);
            this.visionPanel.PerformLayout();
            this.grpJog.ResumeLayout(false);
            this.jogLayout.ResumeLayout(false);
            this.jogAxisLayout.ResumeLayout(false);
            this.jogXyLayout.ResumeLayout(false);
            this.jogZLayout.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.grpSpeed.ResumeLayout(false);
            this.speedLayout.ResumeLayout(false);
            this.speedLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).EndInit();
            this.ResumeLayout(false);

        }
    }
}

