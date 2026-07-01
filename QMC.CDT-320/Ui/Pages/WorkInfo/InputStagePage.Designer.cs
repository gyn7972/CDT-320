using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Controls;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class InputStagePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel contentLayout;
        private TableLayoutPanel leftLayout;
        private GroupBox grpState;
        private GroupBox grpCounters;
        private GroupBox grpCylinder;
        private GroupBox grpInfo;
        private GroupBox grpAction;
        private MaterialDetailView materialDetailView;
        private TableLayoutPanel stateLayout;
        private TableLayoutPanel counterLayout;
        private TableLayoutPanel cylinderLayout;
        private TableLayoutPanel infoLayout;
        private TableLayoutPanel actionBar;
        private ActionButton btnWfAlign;
        private ActionButton btnWfBarcode;
        private ActionButton btnPrepareLoad;
        private ActionButton btnDieMapping;
        private ActionButton btnPrepareUnload;
        private ActionButton btnMoveAvoid;
        private ActionButton btnVisionWafer;
        private ActionButton btnStop;
        private Label lblStageExistTitle;
        private Label lblStageExistValue;
        private Label lblStageAlignTitle;
        private Label lblStageAlignValue;
        private Label lblStageAlignOffsetTitle;
        private Label lblStageAlignOffsetValue;
        private Label lblStageBarcodeTitle;
        private Label lblStageBarcodeValue;
        private Label lblStageChipAlignTitle;
        private Label lblStageChipAlignValue;
        private Label lblStageChipAlignOffsetTitle;
        private Label lblStageChipAlignOffsetValue;
        private Label lblStageFinishTitle;
        private Label lblStageFinishValue;
        private Label lblNeedleUsingTitle;
        private Label lblNeedleUsingValue;
        private Label lblJellPadUsingValue;
        private Label lblExpendingTitle;
        private Label lblExpendingValue;
        private Label lblNeedleUpDownTitle;
        private Label lblNeedleUpDownValue;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.leftLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpState = new System.Windows.Forms.GroupBox();
            this.stateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblStageExistTitle = new System.Windows.Forms.Label();
            this.lblStageExistValue = new System.Windows.Forms.Label();
            this.lblStageAlignTitle = new System.Windows.Forms.Label();
            this.lblStageAlignValue = new System.Windows.Forms.Label();
            this.lblStageAlignOffsetTitle = new System.Windows.Forms.Label();
            this.lblStageAlignOffsetValue = new System.Windows.Forms.Label();
            this.lblStageBarcodeTitle = new System.Windows.Forms.Label();
            this.lblStageBarcodeValue = new System.Windows.Forms.Label();
            this.lblStageChipAlignTitle = new System.Windows.Forms.Label();
            this.lblStageChipAlignValue = new System.Windows.Forms.Label();
            this.lblStageChipAlignOffsetTitle = new System.Windows.Forms.Label();
            this.lblStageChipAlignOffsetValue = new System.Windows.Forms.Label();
            this.lblStageFinishTitle = new System.Windows.Forms.Label();
            this.lblStageFinishValue = new System.Windows.Forms.Label();
            this.grpCounters = new System.Windows.Forms.GroupBox();
            this.counterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblNeedleUsingTitle = new System.Windows.Forms.Label();
            this.lblNeedleUsingValue = new System.Windows.Forms.Label();
            this.lblJellPadUsingTitle = new System.Windows.Forms.Label();
            this.lblJellPadUsingValue = new System.Windows.Forms.Label();
            this.grpInfo = new System.Windows.Forms.GroupBox();
            this.infoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.stageAxisYPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblStageAxisYTitle = new System.Windows.Forms.Label();
            this.lblStageAxisYValue = new System.Windows.Forms.Label();
            this.stageAxisTPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblStageAxisTTitle = new System.Windows.Forms.Label();
            this.lblStageAxisTValue = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblStageAxisZTitle = new System.Windows.Forms.Label();
            this.lblStageAxisZValue = new System.Windows.Forms.Label();
            this.stageAxisXPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblStageAxisXTitle = new System.Windows.Forms.Label();
            this.lblVisionAxisXValue = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.needleAxisZPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblNeedleAxisZTitle = new System.Windows.Forms.Label();
            this.lblNeedleAxisZValue = new System.Windows.Forms.Label();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.needleVacuumPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dotNeedleVacuum = new QMC.CDT_320.Ui.Controls.IndicatorDot();
            this.lblNeedleVacuum = new System.Windows.Forms.Label();
            this.grpCylinder = new System.Windows.Forms.GroupBox();
            this.cylinderLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblExpendingTitle = new System.Windows.Forms.Label();
            this.lblExpendingValue = new System.Windows.Forms.Label();
            this.lblNeedleUpDownTitle = new System.Windows.Forms.Label();
            this.lblNeedleUpDownValue = new System.Windows.Forms.Label();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionBar = new System.Windows.Forms.TableLayoutPanel();
            this.btnWfAlign = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnWfBarcode = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPrepareLoad = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnDieMapping = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPrepareUnload = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnMoveAvoid = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnVisionWafer = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.materialDetailView = new QMC.CDT_320.Ui.Controls.MaterialDetailView();
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.leftLayout.SuspendLayout();
            this.grpState.SuspendLayout();
            this.stateLayout.SuspendLayout();
            this.grpCounters.SuspendLayout();
            this.counterLayout.SuspendLayout();
            this.grpInfo.SuspendLayout();
            this.infoLayout.SuspendLayout();
            this.stageAxisYPanel.SuspendLayout();
            this.stageAxisTPanel.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.stageAxisXPanel.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.needleAxisZPanel.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.needleVacuumPanel.SuspendLayout();
            this.grpCylinder.SuspendLayout();
            this.cylinderLayout.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionBar.SuspendLayout();
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
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1678, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:wi.inputStage";
            this.lblHeader.Text = "INPUT STAGE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.contentLayout.Controls.Add(this.leftLayout, 0, 0);
            this.contentLayout.Controls.Add(this.grpAction, 0, 1);
            this.contentLayout.Controls.Add(this.materialDetailView, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 2;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 870);
            this.contentLayout.TabIndex = 1;
            // 
            // leftLayout
            // 
            this.leftLayout.ColumnCount = 3;
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 19F));
            this.leftLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 47F));
            this.leftLayout.Controls.Add(this.grpState, 0, 0);
            this.leftLayout.Controls.Add(this.grpCounters, 1, 0);
            this.leftLayout.Controls.Add(this.grpInfo, 2, 0);
            this.leftLayout.Controls.Add(this.grpCylinder, 1, 1);
            this.leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftLayout.Location = new System.Drawing.Point(0, 0);
            this.leftLayout.Margin = new System.Windows.Forms.Padding(0);
            this.leftLayout.Name = "leftLayout";
            this.leftLayout.RowCount = 2;
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.leftLayout.Size = new System.Drawing.Size(839, 300);
            this.leftLayout.TabIndex = 0;
            // 
            // grpState
            // 
            this.grpState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpState.Controls.Add(this.stateLayout);
            this.grpState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpState.Location = new System.Drawing.Point(0, 0);
            this.grpState.Margin = new System.Windows.Forms.Padding(0, 0, 3, 3);
            this.grpState.Name = "grpState";
            this.leftLayout.SetRowSpan(this.grpState, 2);
            this.grpState.Size = new System.Drawing.Size(282, 297);
            this.grpState.TabIndex = 0;
            this.grpState.TabStop = false;
            this.grpState.Text = "WORK INFO";
            // 
            // stateLayout
            // 
            this.stateLayout.ColumnCount = 2;
            this.stateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48.46154F));
            this.stateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 51.53846F));
            this.stateLayout.Controls.Add(this.lblStageExistTitle, 0, 0);
            this.stateLayout.Controls.Add(this.lblStageExistValue, 1, 0);
            this.stateLayout.Controls.Add(this.lblStageAlignTitle, 0, 1);
            this.stateLayout.Controls.Add(this.lblStageAlignValue, 1, 1);
            this.stateLayout.Controls.Add(this.lblStageAlignOffsetTitle, 0, 2);
            this.stateLayout.Controls.Add(this.lblStageAlignOffsetValue, 1, 2);
            this.stateLayout.Controls.Add(this.lblStageBarcodeTitle, 0, 3);
            this.stateLayout.Controls.Add(this.lblStageBarcodeValue, 1, 3);
            this.stateLayout.Controls.Add(this.lblStageChipAlignTitle, 0, 4);
            this.stateLayout.Controls.Add(this.lblStageChipAlignValue, 1, 4);
            this.stateLayout.Controls.Add(this.lblStageChipAlignOffsetTitle, 0, 5);
            this.stateLayout.Controls.Add(this.lblStageChipAlignOffsetValue, 1, 5);
            this.stateLayout.Controls.Add(this.lblStageFinishTitle, 0, 6);
            this.stateLayout.Controls.Add(this.lblStageFinishValue, 1, 6);
            this.stateLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stateLayout.Location = new System.Drawing.Point(3, 23);
            this.stateLayout.Name = "stateLayout";
            this.stateLayout.Padding = new System.Windows.Forms.Padding(8, 12, 8, 10);
            this.stateLayout.RowCount = 7;
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.stateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.stateLayout.Size = new System.Drawing.Size(276, 271);
            this.stateLayout.TabIndex = 0;
            // 
            // lblStageExistTitle
            // 
            this.lblStageExistTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblStageExistTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageExistTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageExistTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblStageExistTitle.Location = new System.Drawing.Point(11, 12);
            this.lblStageExistTitle.Name = "lblStageExistTitle";
            this.lblStageExistTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblStageExistTitle.Size = new System.Drawing.Size(120, 35);
            this.lblStageExistTitle.TabIndex = 0;
            this.lblStageExistTitle.Text = "STAGE EXIST";
            this.lblStageExistTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageExistValue
            // 
            this.lblStageExistValue.BackColor = System.Drawing.Color.White;
            this.lblStageExistValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageExistValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageExistValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStageExistValue.Location = new System.Drawing.Point(137, 12);
            this.lblStageExistValue.Name = "lblStageExistValue";
            this.lblStageExistValue.Size = new System.Drawing.Size(128, 35);
            this.lblStageExistValue.TabIndex = 1;
            this.lblStageExistValue.Text = "EMPTY";
            this.lblStageExistValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStageAlignTitle
            // 
            this.lblStageAlignTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblStageAlignTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageAlignTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAlignTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblStageAlignTitle.Location = new System.Drawing.Point(11, 47);
            this.lblStageAlignTitle.Name = "lblStageAlignTitle";
            this.lblStageAlignTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblStageAlignTitle.Size = new System.Drawing.Size(120, 35);
            this.lblStageAlignTitle.TabIndex = 2;
            this.lblStageAlignTitle.Text = "STAGE ALIGN";
            this.lblStageAlignTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageAlignValue
            // 
            this.lblStageAlignValue.BackColor = System.Drawing.Color.White;
            this.lblStageAlignValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageAlignValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAlignValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStageAlignValue.Location = new System.Drawing.Point(137, 47);
            this.lblStageAlignValue.Name = "lblStageAlignValue";
            this.lblStageAlignValue.Size = new System.Drawing.Size(128, 35);
            this.lblStageAlignValue.TabIndex = 3;
            this.lblStageAlignValue.Text = "INCOMPLETE";
            this.lblStageAlignValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStageAlignOffsetTitle
            // 
            this.lblStageAlignOffsetTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblStageAlignOffsetTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageAlignOffsetTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAlignOffsetTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblStageAlignOffsetTitle.Location = new System.Drawing.Point(11, 82);
            this.lblStageAlignOffsetTitle.Name = "lblStageAlignOffsetTitle";
            this.lblStageAlignOffsetTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblStageAlignOffsetTitle.Size = new System.Drawing.Size(120, 35);
            this.lblStageAlignOffsetTitle.TabIndex = 4;
            this.lblStageAlignOffsetTitle.Text = "ALIGN OFFSET";
            this.lblStageAlignOffsetTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageAlignOffsetValue
            // 
            this.lblStageAlignOffsetValue.BackColor = System.Drawing.Color.White;
            this.lblStageAlignOffsetValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageAlignOffsetValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAlignOffsetValue.Font = new System.Drawing.Font("Consolas", 7.5F);
            this.lblStageAlignOffsetValue.Location = new System.Drawing.Point(137, 82);
            this.lblStageAlignOffsetValue.Name = "lblStageAlignOffsetValue";
            this.lblStageAlignOffsetValue.Size = new System.Drawing.Size(128, 35);
            this.lblStageAlignOffsetValue.TabIndex = 5;
            this.lblStageAlignOffsetValue.Text = "X0/Y0";
            this.lblStageAlignOffsetValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStageBarcodeTitle
            // 
            this.lblStageBarcodeTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblStageBarcodeTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageBarcodeTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageBarcodeTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblStageBarcodeTitle.Location = new System.Drawing.Point(11, 117);
            this.lblStageBarcodeTitle.Name = "lblStageBarcodeTitle";
            this.lblStageBarcodeTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblStageBarcodeTitle.Size = new System.Drawing.Size(120, 35);
            this.lblStageBarcodeTitle.TabIndex = 6;
            this.lblStageBarcodeTitle.Text = "STAGE BARCODE";
            this.lblStageBarcodeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageBarcodeValue
            // 
            this.lblStageBarcodeValue.BackColor = System.Drawing.Color.White;
            this.lblStageBarcodeValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageBarcodeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageBarcodeValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStageBarcodeValue.Location = new System.Drawing.Point(137, 117);
            this.lblStageBarcodeValue.Name = "lblStageBarcodeValue";
            this.lblStageBarcodeValue.Size = new System.Drawing.Size(128, 35);
            this.lblStageBarcodeValue.TabIndex = 7;
            this.lblStageBarcodeValue.Text = "INCOMPLETE";
            this.lblStageBarcodeValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStageChipAlignTitle
            // 
            this.lblStageChipAlignTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblStageChipAlignTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageChipAlignTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageChipAlignTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblStageChipAlignTitle.Location = new System.Drawing.Point(11, 152);
            this.lblStageChipAlignTitle.Name = "lblStageChipAlignTitle";
            this.lblStageChipAlignTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblStageChipAlignTitle.Size = new System.Drawing.Size(120, 35);
            this.lblStageChipAlignTitle.TabIndex = 8;
            this.lblStageChipAlignTitle.Text = "STAGE CHIP ALIGN";
            this.lblStageChipAlignTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageChipAlignValue
            // 
            this.lblStageChipAlignValue.BackColor = System.Drawing.Color.White;
            this.lblStageChipAlignValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageChipAlignValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageChipAlignValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStageChipAlignValue.Location = new System.Drawing.Point(137, 152);
            this.lblStageChipAlignValue.Name = "lblStageChipAlignValue";
            this.lblStageChipAlignValue.Size = new System.Drawing.Size(128, 35);
            this.lblStageChipAlignValue.TabIndex = 9;
            this.lblStageChipAlignValue.Text = "INCOMPLETE";
            this.lblStageChipAlignValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStageChipAlignOffsetTitle
            // 
            this.lblStageChipAlignOffsetTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblStageChipAlignOffsetTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageChipAlignOffsetTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageChipAlignOffsetTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblStageChipAlignOffsetTitle.Location = new System.Drawing.Point(11, 187);
            this.lblStageChipAlignOffsetTitle.Name = "lblStageChipAlignOffsetTitle";
            this.lblStageChipAlignOffsetTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblStageChipAlignOffsetTitle.Size = new System.Drawing.Size(120, 35);
            this.lblStageChipAlignOffsetTitle.TabIndex = 10;
            this.lblStageChipAlignOffsetTitle.Text = "DIE MAP OFFSET";
            this.lblStageChipAlignOffsetTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageChipAlignOffsetValue
            // 
            this.lblStageChipAlignOffsetValue.BackColor = System.Drawing.Color.White;
            this.lblStageChipAlignOffsetValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageChipAlignOffsetValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageChipAlignOffsetValue.Font = new System.Drawing.Font("Consolas", 6F);
            this.lblStageChipAlignOffsetValue.Location = new System.Drawing.Point(137, 187);
            this.lblStageChipAlignOffsetValue.Name = "lblStageChipAlignOffsetValue";
            this.lblStageChipAlignOffsetValue.Size = new System.Drawing.Size(128, 35);
            this.lblStageChipAlignOffsetValue.TabIndex = 11;
            this.lblStageChipAlignOffsetValue.Text = "X0/Y0";
            this.lblStageChipAlignOffsetValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStageFinishTitle
            // 
            this.lblStageFinishTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblStageFinishTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageFinishTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageFinishTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblStageFinishTitle.Location = new System.Drawing.Point(11, 222);
            this.lblStageFinishTitle.Name = "lblStageFinishTitle";
            this.lblStageFinishTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblStageFinishTitle.Size = new System.Drawing.Size(120, 39);
            this.lblStageFinishTitle.TabIndex = 12;
            this.lblStageFinishTitle.Text = "STAGE FINISH";
            this.lblStageFinishTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageFinishValue
            // 
            this.lblStageFinishValue.BackColor = System.Drawing.Color.White;
            this.lblStageFinishValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageFinishValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageFinishValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStageFinishValue.Location = new System.Drawing.Point(137, 222);
            this.lblStageFinishValue.Name = "lblStageFinishValue";
            this.lblStageFinishValue.Size = new System.Drawing.Size(128, 39);
            this.lblStageFinishValue.TabIndex = 13;
            this.lblStageFinishValue.Text = "INCOMPLETE";
            this.lblStageFinishValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpCounters
            // 
            this.grpCounters.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpCounters.Controls.Add(this.counterLayout);
            this.grpCounters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCounters.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpCounters.Location = new System.Drawing.Point(288, 0);
            this.grpCounters.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.grpCounters.Name = "grpCounters";
            this.grpCounters.Size = new System.Drawing.Size(153, 147);
            this.grpCounters.TabIndex = 1;
            this.grpCounters.TabStop = false;
            this.grpCounters.Text = "COUNTER";
            // 
            // counterLayout
            // 
            this.counterLayout.ColumnCount = 1;
            this.counterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.counterLayout.Controls.Add(this.lblNeedleUsingTitle, 0, 0);
            this.counterLayout.Controls.Add(this.lblNeedleUsingValue, 0, 1);
            this.counterLayout.Controls.Add(this.lblJellPadUsingTitle, 0, 2);
            this.counterLayout.Controls.Add(this.lblJellPadUsingValue, 0, 3);
            this.counterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.counterLayout.Location = new System.Drawing.Point(3, 23);
            this.counterLayout.Name = "counterLayout";
            this.counterLayout.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.counterLayout.RowCount = 4;
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.counterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.counterLayout.Size = new System.Drawing.Size(147, 121);
            this.counterLayout.TabIndex = 0;
            // 
            // lblNeedleUsingTitle
            // 
            this.lblNeedleUsingTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblNeedleUsingTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleUsingTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleUsingTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblNeedleUsingTitle.Location = new System.Drawing.Point(11, 12);
            this.lblNeedleUsingTitle.Name = "lblNeedleUsingTitle";
            this.lblNeedleUsingTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNeedleUsingTitle.Size = new System.Drawing.Size(75, 49);
            this.lblNeedleUsingTitle.TabIndex = 0;
            this.lblNeedleUsingTitle.Text = "NEEDLE USING";
            this.lblNeedleUsingTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleUsingValue
            // 
            this.lblNeedleUsingValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleUsingValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleUsingValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleUsingValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNeedleUsingValue.Location = new System.Drawing.Point(92, 12);
            this.lblNeedleUsingValue.Name = "lblNeedleUsingValue";
            this.lblNeedleUsingValue.Size = new System.Drawing.Size(44, 49);
            this.lblNeedleUsingValue.TabIndex = 1;
            this.lblNeedleUsingValue.Text = "0 ea";
            this.lblNeedleUsingValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblJellPadUsingTitle
            // 
            this.lblJellPadUsingTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblJellPadUsingTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblJellPadUsingTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJellPadUsingTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblJellPadUsingTitle.Location = new System.Drawing.Point(11, 61);
            this.lblJellPadUsingTitle.Name = "lblJellPadUsingTitle";
            this.lblJellPadUsingTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblJellPadUsingTitle.Size = new System.Drawing.Size(75, 50);
            this.lblJellPadUsingTitle.TabIndex = 2;
            this.lblJellPadUsingTitle.Text = "JELL PAD USING";
            this.lblJellPadUsingTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblJellPadUsingValue
            // 
            this.lblJellPadUsingValue.BackColor = System.Drawing.Color.White;
            this.lblJellPadUsingValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblJellPadUsingValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJellPadUsingValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblJellPadUsingValue.Location = new System.Drawing.Point(92, 61);
            this.lblJellPadUsingValue.Name = "lblJellPadUsingValue";
            this.lblJellPadUsingValue.Size = new System.Drawing.Size(44, 50);
            this.lblJellPadUsingValue.TabIndex = 3;
            this.lblJellPadUsingValue.Text = "0 ea";
            this.lblJellPadUsingValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpInfo
            // 
            this.grpInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpInfo.Controls.Add(this.infoLayout);
            this.grpInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpInfo.Location = new System.Drawing.Point(447, 0);
            this.grpInfo.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.grpInfo.Name = "grpInfo";
            this.leftLayout.SetRowSpan(this.grpInfo, 2);
            this.grpInfo.Size = new System.Drawing.Size(389, 297);
            this.grpInfo.TabIndex = 3;
            this.grpInfo.TabStop = false;
            this.grpInfo.Text = "INFO";
            // 
            // infoLayout
            // 
            this.infoLayout.ColumnCount = 2;
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoLayout.Controls.Add(this.stageAxisYPanel, 0, 0);
            this.infoLayout.Controls.Add(this.stageAxisTPanel, 1, 0);
            this.infoLayout.Controls.Add(this.tableLayoutPanel1, 0, 1);
            this.infoLayout.Controls.Add(this.stageAxisXPanel, 1, 1);
            this.infoLayout.Controls.Add(this.tableLayoutPanel2, 0, 2);
            this.infoLayout.Controls.Add(this.needleAxisZPanel, 1, 2);
            this.infoLayout.Controls.Add(this.tableLayoutPanel3, 0, 3);
            this.infoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoLayout.Location = new System.Drawing.Point(3, 23);
            this.infoLayout.Name = "infoLayout";
            this.infoLayout.Padding = new System.Windows.Forms.Padding(8, 12, 8, 12);
            this.infoLayout.RowCount = 4;
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.infoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.infoLayout.Size = new System.Drawing.Size(383, 271);
            this.infoLayout.TabIndex = 0;
            // 
            // stageAxisYPanel
            // 
            this.stageAxisYPanel.ColumnCount = 1;
            this.stageAxisYPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.stageAxisYPanel.Controls.Add(this.lblStageAxisYTitle, 0, 0);
            this.stageAxisYPanel.Controls.Add(this.lblStageAxisYValue, 0, 1);
            this.stageAxisYPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stageAxisYPanel.Location = new System.Drawing.Point(12, 16);
            this.stageAxisYPanel.Margin = new System.Windows.Forms.Padding(4);
            this.stageAxisYPanel.Name = "stageAxisYPanel";
            this.stageAxisYPanel.RowCount = 2;
            this.stageAxisYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.stageAxisYPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.stageAxisYPanel.Size = new System.Drawing.Size(175, 50);
            this.stageAxisYPanel.TabIndex = 2;
            // 
            // lblStageAxisYTitle
            // 
            this.lblStageAxisYTitle.BackColor = System.Drawing.Color.Black;
            this.lblStageAxisYTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAxisYTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblStageAxisYTitle.ForeColor = System.Drawing.Color.White;
            this.lblStageAxisYTitle.Location = new System.Drawing.Point(3, 0);
            this.lblStageAxisYTitle.Name = "lblStageAxisYTitle";
            this.lblStageAxisYTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblStageAxisYTitle.Size = new System.Drawing.Size(169, 24);
            this.lblStageAxisYTitle.TabIndex = 0;
            this.lblStageAxisYTitle.Text = "STAGE AXIS Y";
            this.lblStageAxisYTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageAxisYValue
            // 
            this.lblStageAxisYValue.BackColor = System.Drawing.Color.White;
            this.lblStageAxisYValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageAxisYValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAxisYValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStageAxisYValue.Location = new System.Drawing.Point(3, 24);
            this.lblStageAxisYValue.Name = "lblStageAxisYValue";
            this.lblStageAxisYValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblStageAxisYValue.Size = new System.Drawing.Size(169, 26);
            this.lblStageAxisYValue.TabIndex = 1;
            this.lblStageAxisYValue.Text = "0 um";
            this.lblStageAxisYValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // stageAxisTPanel
            // 
            this.stageAxisTPanel.ColumnCount = 1;
            this.stageAxisTPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.stageAxisTPanel.Controls.Add(this.lblStageAxisTTitle, 0, 0);
            this.stageAxisTPanel.Controls.Add(this.lblStageAxisTValue, 0, 1);
            this.stageAxisTPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stageAxisTPanel.Location = new System.Drawing.Point(195, 16);
            this.stageAxisTPanel.Margin = new System.Windows.Forms.Padding(4);
            this.stageAxisTPanel.Name = "stageAxisTPanel";
            this.stageAxisTPanel.RowCount = 2;
            this.stageAxisTPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.stageAxisTPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.stageAxisTPanel.Size = new System.Drawing.Size(176, 50);
            this.stageAxisTPanel.TabIndex = 1;
            // 
            // lblStageAxisTTitle
            // 
            this.lblStageAxisTTitle.BackColor = System.Drawing.Color.Black;
            this.lblStageAxisTTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAxisTTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblStageAxisTTitle.ForeColor = System.Drawing.Color.White;
            this.lblStageAxisTTitle.Location = new System.Drawing.Point(3, 0);
            this.lblStageAxisTTitle.Name = "lblStageAxisTTitle";
            this.lblStageAxisTTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblStageAxisTTitle.Size = new System.Drawing.Size(170, 24);
            this.lblStageAxisTTitle.TabIndex = 0;
            this.lblStageAxisTTitle.Text = "STAGE AXIS T";
            this.lblStageAxisTTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageAxisTValue
            // 
            this.lblStageAxisTValue.BackColor = System.Drawing.Color.White;
            this.lblStageAxisTValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageAxisTValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAxisTValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStageAxisTValue.Location = new System.Drawing.Point(3, 24);
            this.lblStageAxisTValue.Name = "lblStageAxisTValue";
            this.lblStageAxisTValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblStageAxisTValue.Size = new System.Drawing.Size(170, 26);
            this.lblStageAxisTValue.TabIndex = 1;
            this.lblStageAxisTValue.Text = "0 um";
            this.lblStageAxisTValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lblStageAxisZTitle, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblStageAxisZValue, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 74);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(175, 50);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // lblStageAxisZTitle
            // 
            this.lblStageAxisZTitle.BackColor = System.Drawing.Color.Black;
            this.lblStageAxisZTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAxisZTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblStageAxisZTitle.ForeColor = System.Drawing.Color.White;
            this.lblStageAxisZTitle.Location = new System.Drawing.Point(3, 0);
            this.lblStageAxisZTitle.Name = "lblStageAxisZTitle";
            this.lblStageAxisZTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblStageAxisZTitle.Size = new System.Drawing.Size(169, 24);
            this.lblStageAxisZTitle.TabIndex = 0;
            this.lblStageAxisZTitle.Text = "STAGE AXIS Z";
            this.lblStageAxisZTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStageAxisZValue
            // 
            this.lblStageAxisZValue.BackColor = System.Drawing.Color.White;
            this.lblStageAxisZValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStageAxisZValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAxisZValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStageAxisZValue.Location = new System.Drawing.Point(3, 24);
            this.lblStageAxisZValue.Name = "lblStageAxisZValue";
            this.lblStageAxisZValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblStageAxisZValue.Size = new System.Drawing.Size(169, 26);
            this.lblStageAxisZValue.TabIndex = 1;
            this.lblStageAxisZValue.Text = "0 um";
            this.lblStageAxisZValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // stageAxisXPanel
            // 
            this.stageAxisXPanel.ColumnCount = 1;
            this.stageAxisXPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.stageAxisXPanel.Controls.Add(this.lblStageAxisXTitle, 0, 0);
            this.stageAxisXPanel.Controls.Add(this.lblVisionAxisXValue, 0, 1);
            this.stageAxisXPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stageAxisXPanel.Location = new System.Drawing.Point(195, 74);
            this.stageAxisXPanel.Margin = new System.Windows.Forms.Padding(4);
            this.stageAxisXPanel.Name = "stageAxisXPanel";
            this.stageAxisXPanel.RowCount = 2;
            this.stageAxisXPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.stageAxisXPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.stageAxisXPanel.Size = new System.Drawing.Size(176, 50);
            this.stageAxisXPanel.TabIndex = 0;
            // 
            // lblStageAxisXTitle
            // 
            this.lblStageAxisXTitle.BackColor = System.Drawing.Color.Black;
            this.lblStageAxisXTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStageAxisXTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblStageAxisXTitle.ForeColor = System.Drawing.Color.White;
            this.lblStageAxisXTitle.Location = new System.Drawing.Point(3, 0);
            this.lblStageAxisXTitle.Name = "lblStageAxisXTitle";
            this.lblStageAxisXTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblStageAxisXTitle.Size = new System.Drawing.Size(170, 24);
            this.lblStageAxisXTitle.TabIndex = 0;
            this.lblStageAxisXTitle.Text = "VISION AXIS X";
            this.lblStageAxisXTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVisionAxisXValue
            // 
            this.lblVisionAxisXValue.BackColor = System.Drawing.Color.White;
            this.lblVisionAxisXValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblVisionAxisXValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionAxisXValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblVisionAxisXValue.Location = new System.Drawing.Point(3, 24);
            this.lblVisionAxisXValue.Name = "lblVisionAxisXValue";
            this.lblVisionAxisXValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblVisionAxisXValue.Size = new System.Drawing.Size(170, 26);
            this.lblVisionAxisXValue.TabIndex = 1;
            this.lblVisionAxisXValue.Text = "0 um";
            this.lblVisionAxisXValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(12, 132);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(175, 50);
            this.tableLayoutPanel2.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Black;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.label1.Size = new System.Drawing.Size(169, 24);
            this.label1.TabIndex = 0;
            this.label1.Text = "NEEDLE AXIS X";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.White;
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Font = new System.Drawing.Font("Consolas", 10F);
            this.label2.Location = new System.Drawing.Point(3, 24);
            this.label2.Name = "label2";
            this.label2.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.label2.Size = new System.Drawing.Size(169, 26);
            this.label2.TabIndex = 1;
            this.label2.Text = "0 um";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // needleAxisZPanel
            // 
            this.needleAxisZPanel.ColumnCount = 1;
            this.needleAxisZPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.needleAxisZPanel.Controls.Add(this.lblNeedleAxisZTitle, 0, 0);
            this.needleAxisZPanel.Controls.Add(this.lblNeedleAxisZValue, 0, 1);
            this.needleAxisZPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.needleAxisZPanel.Location = new System.Drawing.Point(195, 132);
            this.needleAxisZPanel.Margin = new System.Windows.Forms.Padding(4);
            this.needleAxisZPanel.Name = "needleAxisZPanel";
            this.needleAxisZPanel.RowCount = 2;
            this.needleAxisZPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.needleAxisZPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.needleAxisZPanel.Size = new System.Drawing.Size(176, 50);
            this.needleAxisZPanel.TabIndex = 3;
            // 
            // lblNeedleAxisZTitle
            // 
            this.lblNeedleAxisZTitle.BackColor = System.Drawing.Color.Black;
            this.lblNeedleAxisZTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleAxisZTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblNeedleAxisZTitle.ForeColor = System.Drawing.Color.White;
            this.lblNeedleAxisZTitle.Location = new System.Drawing.Point(3, 0);
            this.lblNeedleAxisZTitle.Name = "lblNeedleAxisZTitle";
            this.lblNeedleAxisZTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblNeedleAxisZTitle.Size = new System.Drawing.Size(170, 24);
            this.lblNeedleAxisZTitle.TabIndex = 0;
            this.lblNeedleAxisZTitle.Text = "NEEDLE AXIS Z";
            this.lblNeedleAxisZTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleAxisZValue
            // 
            this.lblNeedleAxisZValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleAxisZValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleAxisZValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleAxisZValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNeedleAxisZValue.Location = new System.Drawing.Point(3, 24);
            this.lblNeedleAxisZValue.Name = "lblNeedleAxisZValue";
            this.lblNeedleAxisZValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblNeedleAxisZValue.Size = new System.Drawing.Size(170, 26);
            this.lblNeedleAxisZValue.TabIndex = 1;
            this.lblNeedleAxisZValue.Text = "0 um";
            this.lblNeedleAxisZValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.label4, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(12, 190);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(175, 65);
            this.tableLayoutPanel3.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.Black;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(3, 0);
            this.label3.Name = "label3";
            this.label3.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.label3.Size = new System.Drawing.Size(169, 24);
            this.label3.TabIndex = 0;
            this.label3.Text = "EJECT PIN AXIS Z";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.White;
            this.label4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Font = new System.Drawing.Font("Consolas", 10F);
            this.label4.Location = new System.Drawing.Point(3, 24);
            this.label4.Name = "label4";
            this.label4.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.label4.Size = new System.Drawing.Size(169, 41);
            this.label4.TabIndex = 1;
            this.label4.Text = "0 um";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // needleVacuumPanel
            // 
            this.needleVacuumPanel.ColumnCount = 2;
            this.needleVacuumPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.needleVacuumPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.needleVacuumPanel.Controls.Add(this.dotNeedleVacuum, 0, 0);
            this.needleVacuumPanel.Controls.Add(this.lblNeedleVacuum, 1, 0);
            this.needleVacuumPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.needleVacuumPanel.Location = new System.Drawing.Point(195, 190);
            this.needleVacuumPanel.Margin = new System.Windows.Forms.Padding(4);
            this.needleVacuumPanel.Name = "needleVacuumPanel";
            this.needleVacuumPanel.RowCount = 1;
            this.needleVacuumPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.needleVacuumPanel.Size = new System.Drawing.Size(176, 65);
            this.needleVacuumPanel.TabIndex = 6;
            // 
            // dotNeedleVacuum
            // 
            this.dotNeedleVacuum.BackColor = System.Drawing.Color.Transparent;
            this.dotNeedleVacuum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dotNeedleVacuum.Location = new System.Drawing.Point(6, 8);
            this.dotNeedleVacuum.Margin = new System.Windows.Forms.Padding(9, 7, 9, 7);
            this.dotNeedleVacuum.Name = "dotNeedleVacuum";
            this.dotNeedleVacuum.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(85)))), ((int)(((byte)(85)))), ((int)(((byte)(85)))));
            this.dotNeedleVacuum.OnColor = System.Drawing.Color.LimeGreen;
            this.dotNeedleVacuum.Size = new System.Drawing.Size(30, 30);
            this.dotNeedleVacuum.TabIndex = 0;
            // 
            // lblNeedleVacuum
            // 
            this.lblNeedleVacuum.BackColor = System.Drawing.SystemColors.Control;
            this.lblNeedleVacuum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleVacuum.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblNeedleVacuum.Location = new System.Drawing.Point(34, 0);
            this.lblNeedleVacuum.Name = "lblNeedleVacuum";
            this.lblNeedleVacuum.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblNeedleVacuum.Size = new System.Drawing.Size(139, 65);
            this.lblNeedleVacuum.TabIndex = 1;
            this.lblNeedleVacuum.Text = "NEEDLE VACUUM";
            this.lblNeedleVacuum.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpCylinder
            // 
            this.grpCylinder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpCylinder.Controls.Add(this.cylinderLayout);
            this.grpCylinder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCylinder.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpCylinder.Location = new System.Drawing.Point(288, 153);
            this.grpCylinder.Name = "grpCylinder";
            this.grpCylinder.Size = new System.Drawing.Size(153, 144);
            this.grpCylinder.TabIndex = 2;
            this.grpCylinder.TabStop = false;
            this.grpCylinder.Text = "NEEDLE INFO";
            // 
            // cylinderLayout
            // 
            this.cylinderLayout.ColumnCount = 1;
            this.cylinderLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cylinderLayout.Controls.Add(this.needleVacuumPanel, 0, 0);
            this.cylinderLayout.Controls.Add(this.lblExpendingTitle, 0, 1);
            this.cylinderLayout.Controls.Add(this.lblExpendingValue, 0, 2);
            this.cylinderLayout.Controls.Add(this.lblNeedleUpDownTitle, 0, 3);
            this.cylinderLayout.Controls.Add(this.lblNeedleUpDownValue, 0, 4);
            this.cylinderLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cylinderLayout.Location = new System.Drawing.Point(3, 23);
            this.cylinderLayout.Name = "cylinderLayout";
            this.cylinderLayout.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cylinderLayout.RowCount = 5;
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.cylinderLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.cylinderLayout.Size = new System.Drawing.Size(147, 118);
            this.cylinderLayout.TabIndex = 0;
            // 
            // lblExpendingTitle
            // 
            this.lblExpendingTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblExpendingTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblExpendingTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExpendingTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblExpendingTitle.Location = new System.Drawing.Point(11, 12);
            this.lblExpendingTitle.Name = "lblExpendingTitle";
            this.lblExpendingTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblExpendingTitle.Size = new System.Drawing.Size(75, 48);
            this.lblExpendingTitle.TabIndex = 0;
            this.lblExpendingTitle.Text = "EXPENDING";
            this.lblExpendingTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblExpendingValue
            // 
            this.lblExpendingValue.BackColor = System.Drawing.Color.White;
            this.lblExpendingValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblExpendingValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExpendingValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblExpendingValue.Location = new System.Drawing.Point(92, 12);
            this.lblExpendingValue.Name = "lblExpendingValue";
            this.lblExpendingValue.Size = new System.Drawing.Size(44, 48);
            this.lblExpendingValue.TabIndex = 1;
            this.lblExpendingValue.Text = "...";
            this.lblExpendingValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblNeedleUpDownTitle
            // 
            this.lblNeedleUpDownTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.lblNeedleUpDownTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleUpDownTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleUpDownTitle.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this.lblNeedleUpDownTitle.Location = new System.Drawing.Point(11, 60);
            this.lblNeedleUpDownTitle.Name = "lblNeedleUpDownTitle";
            this.lblNeedleUpDownTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblNeedleUpDownTitle.Size = new System.Drawing.Size(75, 48);
            this.lblNeedleUpDownTitle.TabIndex = 2;
            this.lblNeedleUpDownTitle.Text = "NEEDLE UP/DOWN";
            this.lblNeedleUpDownTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNeedleUpDownValue
            // 
            this.lblNeedleUpDownValue.BackColor = System.Drawing.Color.White;
            this.lblNeedleUpDownValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblNeedleUpDownValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNeedleUpDownValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblNeedleUpDownValue.Location = new System.Drawing.Point(92, 60);
            this.lblNeedleUpDownValue.Name = "lblNeedleUpDownValue";
            this.lblNeedleUpDownValue.Size = new System.Drawing.Size(44, 48);
            this.lblNeedleUpDownValue.TabIndex = 3;
            this.lblNeedleUpDownValue.Text = "...";
            this.lblNeedleUpDownValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpAction
            // 
            this.grpAction.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpAction.Controls.Add(this.actionBar);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.Location = new System.Drawing.Point(0, 303);
            this.grpAction.Margin = new System.Windows.Forms.Padding(0, 3, 3, 0);
            this.grpAction.Name = "grpAction";
            this.grpAction.Size = new System.Drawing.Size(836, 567);
            this.grpAction.TabIndex = 3;
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
            // 
            // actionBar
            // 
            this.actionBar.ColumnCount = 2;
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionBar.Controls.Add(this.btnWfAlign, 0, 0);
            this.actionBar.Controls.Add(this.btnWfBarcode, 1, 0);
            this.actionBar.Controls.Add(this.btnPrepareLoad, 0, 1);
            this.actionBar.Controls.Add(this.btnDieMapping, 1, 1);
            this.actionBar.Controls.Add(this.btnPrepareUnload, 0, 2);
            this.actionBar.Controls.Add(this.btnMoveAvoid, 1, 2);
            this.actionBar.Controls.Add(this.btnVisionWafer, 0, 3);
            this.actionBar.Controls.Add(this.btnStop, 1, 3);
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionBar.Location = new System.Drawing.Point(3, 23);
            this.actionBar.Margin = new System.Windows.Forms.Padding(0);
            this.actionBar.Name = "actionBar";
            this.actionBar.RowCount = 4;
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.Size = new System.Drawing.Size(830, 184);
            this.actionBar.TabIndex = 0;
            // 
            // btnWfAlign
            // 
            this.btnWfAlign.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnWfAlign.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnWfAlign.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWfAlign.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnWfAlign.ForeColor = System.Drawing.Color.White;
            this.btnWfAlign.Location = new System.Drawing.Point(3, 3);
            this.btnWfAlign.Name = "btnWfAlign";
            this.btnWfAlign.Size = new System.Drawing.Size(409, 40);
            this.btnWfAlign.TabIndex = 0;
            this.btnWfAlign.Tag = "i18n:wi.wfAlign";
            this.btnWfAlign.Text = "WAFER ALIGN";
            // 
            // btnWfBarcode
            // 
            this.btnWfBarcode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnWfBarcode.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnWfBarcode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnWfBarcode.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnWfBarcode.ForeColor = System.Drawing.Color.White;
            this.btnWfBarcode.Location = new System.Drawing.Point(418, 3);
            this.btnWfBarcode.Name = "btnWfBarcode";
            this.btnWfBarcode.Size = new System.Drawing.Size(409, 40);
            this.btnWfBarcode.TabIndex = 1;
            this.btnWfBarcode.Tag = "i18n:wi.wfBarcode";
            this.btnWfBarcode.Text = "WAFER BARCODE";
            // 
            // btnPrepareLoad
            // 
            this.btnPrepareLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPrepareLoad.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrepareLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrepareLoad.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnPrepareLoad.ForeColor = System.Drawing.Color.White;
            this.btnPrepareLoad.Location = new System.Drawing.Point(3, 49);
            this.btnPrepareLoad.Name = "btnPrepareLoad";
            this.btnPrepareLoad.Size = new System.Drawing.Size(409, 40);
            this.btnPrepareLoad.TabIndex = 2;
            this.btnPrepareLoad.Text = "PREP LOAD";
            // 
            // btnDieMapping
            // 
            this.btnDieMapping.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnDieMapping.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDieMapping.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDieMapping.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnDieMapping.ForeColor = System.Drawing.Color.White;
            this.btnDieMapping.Location = new System.Drawing.Point(418, 49);
            this.btnDieMapping.Name = "btnDieMapping";
            this.btnDieMapping.Size = new System.Drawing.Size(409, 40);
            this.btnDieMapping.TabIndex = 3;
            this.btnDieMapping.Text = "DIE MAPPING";
            // 
            // btnPrepareUnload
            // 
            this.btnPrepareUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPrepareUnload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrepareUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrepareUnload.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnPrepareUnload.ForeColor = System.Drawing.Color.White;
            this.btnPrepareUnload.Location = new System.Drawing.Point(3, 95);
            this.btnPrepareUnload.Name = "btnPrepareUnload";
            this.btnPrepareUnload.Size = new System.Drawing.Size(409, 40);
            this.btnPrepareUnload.TabIndex = 4;
            this.btnPrepareUnload.Text = "PREP UNLOAD";
            // 
            // btnMoveAvoid
            // 
            this.btnMoveAvoid.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnMoveAvoid.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnMoveAvoid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveAvoid.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnMoveAvoid.ForeColor = System.Drawing.Color.White;
            this.btnMoveAvoid.Location = new System.Drawing.Point(418, 95);
            this.btnMoveAvoid.Name = "btnMoveAvoid";
            this.btnMoveAvoid.Size = new System.Drawing.Size(409, 40);
            this.btnMoveAvoid.TabIndex = 5;
            this.btnMoveAvoid.Text = "AVOID";
            // 
            // btnVisionWafer
            // 
            this.btnVisionWafer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnVisionWafer.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnVisionWafer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVisionWafer.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnVisionWafer.ForeColor = System.Drawing.Color.White;
            this.btnVisionWafer.Location = new System.Drawing.Point(3, 141);
            this.btnVisionWafer.Name = "btnVisionWafer";
            this.btnVisionWafer.Size = new System.Drawing.Size(409, 40);
            this.btnVisionWafer.TabIndex = 6;
            this.btnVisionWafer.Text = "VISION: WAFER";
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(214)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.btnStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnStop.ForeColor = System.Drawing.Color.White;
            this.btnStop.Location = new System.Drawing.Point(418, 141);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(409, 40);
            this.btnStop.TabIndex = 7;
            this.btnStop.Text = "STOP";
            // 
            // materialDetailView
            // 
            this.materialDetailView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.materialDetailView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialDetailView.Location = new System.Drawing.Point(842, 0);
            this.materialDetailView.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.materialDetailView.Name = "materialDetailView";
            this.contentLayout.SetRowSpan(this.materialDetailView, 2);
            this.materialDetailView.ShowProcessTestDataButton = false;
            this.materialDetailView.Size = new System.Drawing.Size(836, 870);
            this.materialDetailView.TabIndex = 1;
            // 
            // InputStagePage
            // 
            this.Controls.Add(this.rootLayout);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "InputStagePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.leftLayout.ResumeLayout(false);
            this.grpState.ResumeLayout(false);
            this.stateLayout.ResumeLayout(false);
            this.grpCounters.ResumeLayout(false);
            this.counterLayout.ResumeLayout(false);
            this.grpInfo.ResumeLayout(false);
            this.infoLayout.ResumeLayout(false);
            this.stageAxisYPanel.ResumeLayout(false);
            this.stageAxisTPanel.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.stageAxisXPanel.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.needleAxisZPanel.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.needleVacuumPanel.ResumeLayout(false);
            this.grpCylinder.ResumeLayout(false);
            this.cylinderLayout.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        private TableLayoutPanel stageAxisXPanel;
        private Label lblVisionAxisXValue;
        private Label lblJellPadUsingTitle;
        private Label lblStageAxisXTitle;
        private TableLayoutPanel stageAxisTPanel;
        private Label lblStageAxisTTitle;
        private Label lblStageAxisTValue;
        private TableLayoutPanel stageAxisYPanel;
        private Label lblStageAxisYTitle;
        private Label lblStageAxisYValue;
        private TableLayoutPanel needleAxisZPanel;
        private Label lblNeedleAxisZTitle;
        private Label lblNeedleAxisZValue;
        private TableLayoutPanel needleVacuumPanel;
        private IndicatorDot dotNeedleVacuum;
        private Label lblNeedleVacuum;
        private TableLayoutPanel tableLayoutPanel1;
        private Label lblStageAxisZTitle;
        private Label lblStageAxisZValue;
        private TableLayoutPanel tableLayoutPanel2;
        private Label label1;
        private Label label2;
        private TableLayoutPanel tableLayoutPanel3;
        private Label label3;
        private Label label4;
    }
}

