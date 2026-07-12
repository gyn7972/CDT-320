using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Ui.Controls;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    partial class InputStageMapTransferPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private Label lblProjectCaption;
        private Label lblProjectValue;
        private Label lblBarcodeCaption;
        private Label lblBarcodeValue;
        private Label lblBinCaption;
        private Label lblBinValue;
        private Label lblMapTitle;
        private GroupBox grpCreate;
        private DieMapView mapView;
        private DataGridView gridDieList;
        private TableLayoutPanel infoEditBody;
        private GroupBox grpMapInfo;
        private TableLayoutPanel mapInfoLayout;
        private Label lblChipWCaption;
        private Label lblChipW;
        private Label lblChipHCaption;
        private Label lblChipH;
        private Label lblPitchXCaption;
        private Label lblPitchX;
        private Label lblPitchYCaption;
        private Label lblPitchY;
        private Label lblWaferDiaCaption;
        private Label lblWaferDia;
        private Label lblAxisXCaption;
        private Label lblAxisX;
        private Label lblAxisYCaption;
        private Label lblAxisY;
        private Label lblBinRankCaption;
        private Label lblBinRank;
        private Label lblDieNumCaption;
        private Label lblDieNum;
        private TableLayoutPanel dieStateArea;
        private GroupBox grpDieState;
        private TableLayoutPanel dieStateLayout;
        private RadioButton rdoDieStateWait;
        private RadioButton rdoDieStateGood;
        private RadioButton rdoDieStateNg;
        private RadioButton rdoDieStateSkip;
        private System.Windows.Forms.Button btnApplyDieState;
        private TableLayoutPanel detachedButtonRow;
        private ActionButton btnManualAlignComplete;
        private ActionButton btnReloadActiveMap;
        private GroupBox grpAction;
        private TableLayoutPanel actionBar;
        private ActionButton btnThetaMatchMove;
        private ActionButton btnXyMatchMove;
        private ActionButton btnManualDieMapOffsetApply;
        private RadioButton rbStandard;
        private RadioButton rbStartIndex;
        private RadioButton rbSelectPickStatus;
        private RadioButton rbDragPickStatus;
        private ActionButton btnPickStatusSave;
        private ActionButton btnNeedleBlockDown;
        private Button btnClose;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblProjectCaption = new System.Windows.Forms.Label();
            this.lblProjectValue = new System.Windows.Forms.Label();
            this.lblBarcodeCaption = new System.Windows.Forms.Label();
            this.lblBarcodeValue = new System.Windows.Forms.Label();
            this.lblBinCaption = new System.Windows.Forms.Label();
            this.lblBinValue = new System.Windows.Forms.Label();
            this.lblMapTitle = new System.Windows.Forms.Label();
            this.grpCreate = new System.Windows.Forms.GroupBox();
            this.mapView = new QMC.CDT320.Ui.Controls.DieMapView();
            this.gridDieList = new System.Windows.Forms.DataGridView();
            this.infoEditBody = new System.Windows.Forms.TableLayoutPanel();
            this.grpMapInfo = new System.Windows.Forms.GroupBox();
            this.mapInfoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblChipWCaption = new System.Windows.Forms.Label();
            this.lblChipW = new System.Windows.Forms.Label();
            this.lblChipHCaption = new System.Windows.Forms.Label();
            this.lblChipH = new System.Windows.Forms.Label();
            this.lblPitchXCaption = new System.Windows.Forms.Label();
            this.lblPitchX = new System.Windows.Forms.Label();
            this.lblPitchYCaption = new System.Windows.Forms.Label();
            this.lblPitchY = new System.Windows.Forms.Label();
            this.lblWaferDiaCaption = new System.Windows.Forms.Label();
            this.lblWaferDia = new System.Windows.Forms.Label();
            this.lblAxisXCaption = new System.Windows.Forms.Label();
            this.lblAxisX = new System.Windows.Forms.Label();
            this.lblAxisYCaption = new System.Windows.Forms.Label();
            this.lblAxisY = new System.Windows.Forms.Label();
            this.lblBinRankCaption = new System.Windows.Forms.Label();
            this.lblBinRank = new System.Windows.Forms.Label();
            this.lblDieNumCaption = new System.Windows.Forms.Label();
            this.lblDieNum = new System.Windows.Forms.Label();
            this.dieStateArea = new System.Windows.Forms.TableLayoutPanel();
            this.grpDieState = new System.Windows.Forms.GroupBox();
            this.dieStateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rdoDieStateWait = new System.Windows.Forms.RadioButton();
            this.rdoDieStateGood = new System.Windows.Forms.RadioButton();
            this.rdoDieStateNg = new System.Windows.Forms.RadioButton();
            this.rdoDieStateSkip = new System.Windows.Forms.RadioButton();
            this.btnApplyDieState = new System.Windows.Forms.Button();
            this.detachedButtonRow = new System.Windows.Forms.TableLayoutPanel();
            this.btnManualAlignComplete = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnReloadActiveMap = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionBar = new System.Windows.Forms.TableLayoutPanel();
            this.btnThetaMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnXyMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnManualDieMapOffsetApply = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rbStandard = new System.Windows.Forms.RadioButton();
            this.rbStartIndex = new System.Windows.Forms.RadioButton();
            this.rbSelectPickStatus = new System.Windows.Forms.RadioButton();
            this.rbDragPickStatus = new System.Windows.Forms.RadioButton();
            this.btnPickStatusSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleBlockDown = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnClose = new System.Windows.Forms.Button();
            this.colIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGridX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGridY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAxisX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAxisY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDieUid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rootLayout.SuspendLayout();
            this.grpCreate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridDieList)).BeginInit();
            this.infoEditBody.SuspendLayout();
            this.grpMapInfo.SuspendLayout();
            this.mapInfoLayout.SuspendLayout();
            this.dieStateArea.SuspendLayout();
            this.grpDieState.SuspendLayout();
            this.dieStateLayout.SuspendLayout();
            this.detachedButtonRow.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionBar.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.Controls.Add(this.grpCreate, 0, 0);
            this.rootLayout.Controls.Add(this.gridDieList, 0, 1);
            this.rootLayout.Controls.Add(this.infoEditBody, 1, 0);
            this.rootLayout.Controls.Add(this.grpAction, 1, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(0);
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            //
            // grpCreate
            //
            this.grpCreate.BackColor = System.Drawing.Color.White;
            this.grpCreate.Controls.Add(this.mapView);
            this.grpCreate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCreate.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpCreate.ForeColor = System.Drawing.Color.Black;
            this.grpCreate.Margin = new System.Windows.Forms.Padding(3);
            this.grpCreate.Name = "grpCreate";
            this.grpCreate.Padding = new System.Windows.Forms.Padding(3);
            this.grpCreate.TabStop = false;
            this.grpCreate.Text = "INPUT DIE MAP CREATE";
            //
            // mapView
            //
            this.mapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.mapView.Caption = "Input Die Map";
            this.mapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapView.Location = new System.Drawing.Point(6, 24);
            this.mapView.Map = null;
            this.mapView.Margin = new System.Windows.Forms.Padding(0);
            this.mapView.Name = "mapView";
            this.mapView.Size = new System.Drawing.Size(815, 412);
            this.mapView.TabIndex = 1;
            //
            // gridDieList
            //
            this.gridDieList.AllowUserToAddRows = false;
            this.gridDieList.AllowUserToDeleteRows = false;
            this.gridDieList.AllowUserToResizeRows = false;
            this.gridDieList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridDieList.BackgroundColor = System.Drawing.Color.White;
            this.gridDieList.ColumnHeadersHeight = 32;
            this.gridDieList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridDieList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colIndex,
            this.colGridX,
            this.colGridY,
            this.colTarget,
            this.colResult,
            this.colBin,
            this.colAxisX,
            this.colAxisY,
            this.colDieUid});
            this.gridDieList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridDieList.Location = new System.Drawing.Point(3, 493);
            this.gridDieList.Margin = new System.Windows.Forms.Padding(3);
            this.gridDieList.MultiSelect = false;
            this.gridDieList.Name = "gridDieList";
            this.gridDieList.ReadOnly = true;
            this.gridDieList.RowHeadersVisible = false;
            this.gridDieList.RowTemplate.Height = 20;
            this.gridDieList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridDieList.Size = new System.Drawing.Size(1128, 332);
            this.gridDieList.TabIndex = 2;
            //
            // infoEditBody
            //
            this.infoEditBody.BackColor = System.Drawing.Color.White;
            this.infoEditBody.ColumnCount = 2;
            this.infoEditBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoEditBody.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.infoEditBody.Controls.Add(this.grpMapInfo, 0, 0);
            this.infoEditBody.Controls.Add(this.dieStateArea, 1, 0);
            this.infoEditBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoEditBody.Margin = new System.Windows.Forms.Padding(3);
            this.infoEditBody.Name = "infoEditBody";
            this.infoEditBody.Padding = new System.Windows.Forms.Padding(0);
            this.infoEditBody.RowCount = 1;
            this.infoEditBody.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.infoEditBody.TabIndex = 1;
            //
            // grpMapInfo
            //
            this.grpMapInfo.BackColor = System.Drawing.Color.White;
            this.grpMapInfo.Controls.Add(this.mapInfoLayout);
            this.grpMapInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMapInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpMapInfo.ForeColor = System.Drawing.Color.Black;
            this.grpMapInfo.Margin = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.grpMapInfo.Name = "grpMapInfo";
            this.grpMapInfo.Padding = new System.Windows.Forms.Padding(3);
            this.grpMapInfo.TabStop = false;
            this.grpMapInfo.Text = "DIE MAP INFO";
            //
            // mapInfoLayout
            //
            this.mapInfoLayout.ColumnCount = 2;
            this.mapInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.mapInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.mapInfoLayout.Controls.Add(this.lblChipWCaption, 0, 0);
            this.mapInfoLayout.Controls.Add(this.lblChipW, 1, 0);
            this.mapInfoLayout.Controls.Add(this.lblChipHCaption, 0, 1);
            this.mapInfoLayout.Controls.Add(this.lblChipH, 1, 1);
            this.mapInfoLayout.Controls.Add(this.lblPitchXCaption, 0, 2);
            this.mapInfoLayout.Controls.Add(this.lblPitchX, 1, 2);
            this.mapInfoLayout.Controls.Add(this.lblPitchYCaption, 0, 3);
            this.mapInfoLayout.Controls.Add(this.lblPitchY, 1, 3);
            this.mapInfoLayout.Controls.Add(this.lblWaferDiaCaption, 0, 4);
            this.mapInfoLayout.Controls.Add(this.lblWaferDia, 1, 4);
            this.mapInfoLayout.Controls.Add(this.lblAxisXCaption, 0, 5);
            this.mapInfoLayout.Controls.Add(this.lblAxisX, 1, 5);
            this.mapInfoLayout.Controls.Add(this.lblAxisYCaption, 0, 6);
            this.mapInfoLayout.Controls.Add(this.lblAxisY, 1, 6);
            this.mapInfoLayout.Controls.Add(this.lblBinRankCaption, 0, 7);
            this.mapInfoLayout.Controls.Add(this.lblBinRank, 1, 7);
            this.mapInfoLayout.Controls.Add(this.lblDieNumCaption, 0, 8);
            this.mapInfoLayout.Controls.Add(this.lblDieNum, 1, 8);
            this.mapInfoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapInfoLayout.Location = new System.Drawing.Point(3, 23);
            this.mapInfoLayout.Name = "mapInfoLayout";
            this.mapInfoLayout.Padding = new System.Windows.Forms.Padding(4, 6, 4, 4);
            this.mapInfoLayout.RowCount = 9;
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.mapInfoLayout.TabIndex = 0;
            //
            // lblChipWCaption
            //
            this.lblChipWCaption.AutoEllipsis = true;
            this.lblChipWCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblChipWCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipWCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipWCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblChipWCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblChipWCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipWCaption.Name = "lblChipWCaption";
            this.lblChipWCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblChipWCaption.TabIndex = 0;
            this.lblChipWCaption.Text = "Chip Width";
            this.lblChipWCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblChipW
            //
            this.lblChipW.AutoEllipsis = true;
            this.lblChipW.BackColor = System.Drawing.Color.White;
            this.lblChipW.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipW.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipW.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblChipW.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblChipW.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipW.Name = "lblChipW";
            this.lblChipW.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblChipW.TabIndex = 1;
            this.lblChipW.Text = "0";
            this.lblChipW.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblChipHCaption
            //
            this.lblChipHCaption.AutoEllipsis = true;
            this.lblChipHCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblChipHCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipHCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipHCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblChipHCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblChipHCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipHCaption.Name = "lblChipHCaption";
            this.lblChipHCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblChipHCaption.TabIndex = 2;
            this.lblChipHCaption.Text = "Chip Height";
            this.lblChipHCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblChipH
            //
            this.lblChipH.AutoEllipsis = true;
            this.lblChipH.BackColor = System.Drawing.Color.White;
            this.lblChipH.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipH.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipH.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblChipH.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblChipH.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipH.Name = "lblChipH";
            this.lblChipH.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblChipH.TabIndex = 3;
            this.lblChipH.Text = "0";
            this.lblChipH.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblPitchXCaption
            //
            this.lblPitchXCaption.AutoEllipsis = true;
            this.lblPitchXCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblPitchXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchXCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblPitchXCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblPitchXCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblPitchXCaption.Name = "lblPitchXCaption";
            this.lblPitchXCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPitchXCaption.TabIndex = 4;
            this.lblPitchXCaption.Text = "Center Step X";
            this.lblPitchXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblPitchX
            //
            this.lblPitchX.AutoEllipsis = true;
            this.lblPitchX.BackColor = System.Drawing.Color.White;
            this.lblPitchX.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchX.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblPitchX.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblPitchX.Margin = new System.Windows.Forms.Padding(1);
            this.lblPitchX.Name = "lblPitchX";
            this.lblPitchX.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblPitchX.TabIndex = 5;
            this.lblPitchX.Text = "0";
            this.lblPitchX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblPitchYCaption
            //
            this.lblPitchYCaption.AutoEllipsis = true;
            this.lblPitchYCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblPitchYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchYCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblPitchYCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblPitchYCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblPitchYCaption.Name = "lblPitchYCaption";
            this.lblPitchYCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPitchYCaption.TabIndex = 6;
            this.lblPitchYCaption.Text = "Center Step Y";
            this.lblPitchYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblPitchY
            //
            this.lblPitchY.AutoEllipsis = true;
            this.lblPitchY.BackColor = System.Drawing.Color.White;
            this.lblPitchY.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchY.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblPitchY.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblPitchY.Margin = new System.Windows.Forms.Padding(1);
            this.lblPitchY.Name = "lblPitchY";
            this.lblPitchY.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblPitchY.TabIndex = 7;
            this.lblPitchY.Text = "0";
            this.lblPitchY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblWaferDiaCaption
            //
            this.lblWaferDiaCaption.AutoEllipsis = true;
            this.lblWaferDiaCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblWaferDiaCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDiaCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDiaCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblWaferDiaCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblWaferDiaCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblWaferDiaCaption.Name = "lblWaferDiaCaption";
            this.lblWaferDiaCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblWaferDiaCaption.TabIndex = 8;
            this.lblWaferDiaCaption.Text = "Wafer Dia";
            this.lblWaferDiaCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblWaferDia
            //
            this.lblWaferDia.AutoEllipsis = true;
            this.lblWaferDia.BackColor = System.Drawing.Color.White;
            this.lblWaferDia.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDia.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDia.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblWaferDia.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblWaferDia.Margin = new System.Windows.Forms.Padding(1);
            this.lblWaferDia.Name = "lblWaferDia";
            this.lblWaferDia.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblWaferDia.TabIndex = 9;
            this.lblWaferDia.Text = "0";
            this.lblWaferDia.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblAxisXCaption
            //
            this.lblAxisXCaption.AutoEllipsis = true;
            this.lblAxisXCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblAxisXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisXCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblAxisXCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblAxisXCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisXCaption.Name = "lblAxisXCaption";
            this.lblAxisXCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxisXCaption.TabIndex = 10;
            this.lblAxisXCaption.Text = "Axis X";
            this.lblAxisXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblAxisX
            //
            this.lblAxisX.AutoEllipsis = true;
            this.lblAxisX.BackColor = System.Drawing.Color.White;
            this.lblAxisX.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisX.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblAxisX.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblAxisX.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisX.Name = "lblAxisX";
            this.lblAxisX.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxisX.TabIndex = 11;
            this.lblAxisX.Text = "0";
            this.lblAxisX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblAxisYCaption
            //
            this.lblAxisYCaption.AutoEllipsis = true;
            this.lblAxisYCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblAxisYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisYCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblAxisYCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblAxisYCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisYCaption.Name = "lblAxisYCaption";
            this.lblAxisYCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxisYCaption.TabIndex = 12;
            this.lblAxisYCaption.Text = "Axis Y";
            this.lblAxisYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblAxisY
            //
            this.lblAxisY.AutoEllipsis = true;
            this.lblAxisY.BackColor = System.Drawing.Color.White;
            this.lblAxisY.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisY.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblAxisY.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblAxisY.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisY.Name = "lblAxisY";
            this.lblAxisY.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxisY.TabIndex = 13;
            this.lblAxisY.Text = "0";
            this.lblAxisY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblBinRankCaption
            //
            this.lblBinRankCaption.AutoEllipsis = true;
            this.lblBinRankCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblBinRankCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBinRankCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinRankCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblBinRankCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblBinRankCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinRankCaption.Name = "lblBinRankCaption";
            this.lblBinRankCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBinRankCaption.TabIndex = 14;
            this.lblBinRankCaption.Text = "BIN RANK";
            this.lblBinRankCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblBinRank
            //
            this.lblBinRank.AutoEllipsis = true;
            this.lblBinRank.BackColor = System.Drawing.Color.White;
            this.lblBinRank.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBinRank.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinRank.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblBinRank.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblBinRank.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinRank.Name = "lblBinRank";
            this.lblBinRank.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblBinRank.TabIndex = 15;
            this.lblBinRank.Text = "0";
            this.lblBinRank.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // lblDieNumCaption
            //
            this.lblDieNumCaption.AutoEllipsis = true;
            this.lblDieNumCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblDieNumCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieNumCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieNumCaption.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblDieNumCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblDieNumCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblDieNumCaption.Name = "lblDieNumCaption";
            this.lblDieNumCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDieNumCaption.TabIndex = 16;
            this.lblDieNumCaption.Text = "Die Number";
            this.lblDieNumCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblDieNum
            //
            this.lblDieNum.AutoEllipsis = true;
            this.lblDieNum.BackColor = System.Drawing.Color.White;
            this.lblDieNum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieNum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieNum.Font = new System.Drawing.Font("Consolas", 8F);
            this.lblDieNum.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblDieNum.Margin = new System.Windows.Forms.Padding(1);
            this.lblDieNum.Name = "lblDieNum";
            this.lblDieNum.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblDieNum.TabIndex = 17;
            this.lblDieNum.Text = "0/0";
            this.lblDieNum.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // dieStateArea
            //
            this.dieStateArea.BackColor = System.Drawing.Color.White;
            this.dieStateArea.ColumnCount = 1;
            this.dieStateArea.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.dieStateArea.Controls.Add(this.grpDieState, 0, 0);
            this.dieStateArea.Controls.Add(this.detachedButtonRow, 0, 2);
            this.dieStateArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dieStateArea.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.dieStateArea.Name = "dieStateArea";
            this.dieStateArea.Padding = new System.Windows.Forms.Padding(0);
            this.dieStateArea.RowCount = 4;
            this.dieStateArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 216F));
            this.dieStateArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.dieStateArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.dieStateArea.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.dieStateArea.TabIndex = 2;
            //
            // grpDieState
            //
            this.grpDieState.BackColor = System.Drawing.Color.White;
            this.grpDieState.Controls.Add(this.dieStateLayout);
            this.grpDieState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDieState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpDieState.ForeColor = System.Drawing.Color.Black;
            this.grpDieState.Margin = new System.Windows.Forms.Padding(0);
            this.grpDieState.Name = "grpDieState";
            this.grpDieState.Padding = new System.Windows.Forms.Padding(3);
            this.grpDieState.TabStop = false;
            this.grpDieState.Text = "DIE STATE EDIT";
            //
            // dieStateLayout
            //
            this.dieStateLayout.ColumnCount = 1;
            this.dieStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.dieStateLayout.Controls.Add(this.rdoDieStateWait, 0, 0);
            this.dieStateLayout.Controls.Add(this.rdoDieStateGood, 0, 1);
            this.dieStateLayout.Controls.Add(this.rdoDieStateNg, 0, 2);
            this.dieStateLayout.Controls.Add(this.rdoDieStateSkip, 0, 3);
            this.dieStateLayout.Controls.Add(this.btnApplyDieState, 0, 4);
            this.dieStateLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dieStateLayout.Location = new System.Drawing.Point(3, 23);
            this.dieStateLayout.Name = "dieStateLayout";
            this.dieStateLayout.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.dieStateLayout.RowCount = 5;
            this.dieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.dieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.dieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.dieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.dieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.dieStateLayout.TabIndex = 0;
            //
            // rdoDieStateWait
            //
            this.rdoDieStateWait.Checked = true;
            this.rdoDieStateWait.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoDieStateWait.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rdoDieStateWait.Name = "rdoDieStateWait";
            this.rdoDieStateWait.TabIndex = 0;
            this.rdoDieStateWait.TabStop = true;
            this.rdoDieStateWait.Text = "WAIT / 검사 대기";
            this.rdoDieStateWait.UseVisualStyleBackColor = true;
            //
            // rdoDieStateGood
            //
            this.rdoDieStateGood.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoDieStateGood.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rdoDieStateGood.Name = "rdoDieStateGood";
            this.rdoDieStateGood.TabIndex = 1;
            this.rdoDieStateGood.Text = "GOOD / 검사 완료";
            this.rdoDieStateGood.UseVisualStyleBackColor = true;
            //
            // rdoDieStateNg
            //
            this.rdoDieStateNg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoDieStateNg.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rdoDieStateNg.Name = "rdoDieStateNg";
            this.rdoDieStateNg.TabIndex = 2;
            this.rdoDieStateNg.Text = "NG / 검사 불량";
            this.rdoDieStateNg.UseVisualStyleBackColor = true;
            //
            // rdoDieStateSkip
            //
            this.rdoDieStateSkip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoDieStateSkip.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rdoDieStateSkip.Name = "rdoDieStateSkip";
            this.rdoDieStateSkip.TabIndex = 3;
            this.rdoDieStateSkip.Text = "SKIP / 제외";
            this.rdoDieStateSkip.UseVisualStyleBackColor = true;
            //
            // btnApplyDieState
            //
            this.btnApplyDieState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(238)))), ((int)(((byte)(244)))));
            this.btnApplyDieState.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApplyDieState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyDieState.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(143)))), ((int)(((byte)(156)))), ((int)(((byte)(173)))));
            this.btnApplyDieState.FlatAppearance.BorderSize = 1;
            this.btnApplyDieState.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(199)))), ((int)(((byte)(210)))), ((int)(((byte)(224)))));
            this.btnApplyDieState.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(218)))), ((int)(((byte)(226)))), ((int)(((byte)(236)))));
            this.btnApplyDieState.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyDieState.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnApplyDieState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.btnApplyDieState.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.btnApplyDieState.Name = "btnApplyDieState";
            this.btnApplyDieState.Text = "APPLY SELECTED DIE";
            this.btnApplyDieState.UseVisualStyleBackColor = false;
            //
            // detachedButtonRow
            //
            this.detachedButtonRow.BackColor = System.Drawing.Color.White;
            this.detachedButtonRow.ColumnCount = 1;
            this.detachedButtonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.detachedButtonRow.Controls.Add(this.btnManualAlignComplete, 0, 0);
            this.detachedButtonRow.Controls.Add(this.btnReloadActiveMap, 0, 1);
            this.detachedButtonRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.detachedButtonRow.Margin = new System.Windows.Forms.Padding(0);
            this.detachedButtonRow.Name = "detachedButtonRow";
            this.detachedButtonRow.Padding = new System.Windows.Forms.Padding(0);
            this.detachedButtonRow.RowCount = 2;
            this.detachedButtonRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.detachedButtonRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.detachedButtonRow.TabIndex = 3;
            //
            // btnManualAlignComplete
            //
            this.btnManualAlignComplete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(244)))), ((int)(((byte)(247)))));
            this.btnManualAlignComplete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnManualAlignComplete.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnManualAlignComplete.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.btnManualAlignComplete.Margin = new System.Windows.Forms.Padding(3);
            this.btnManualAlignComplete.Name = "btnManualAlignComplete";
            this.btnManualAlignComplete.TabIndex = 0;
            this.btnManualAlignComplete.Text = "MANUAL ALIGN COMPLETE";
            //
            // btnReloadActiveMap
            //
            this.btnReloadActiveMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(244)))), ((int)(((byte)(247)))));
            this.btnReloadActiveMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReloadActiveMap.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnReloadActiveMap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.btnReloadActiveMap.Margin = new System.Windows.Forms.Padding(3);
            this.btnReloadActiveMap.Name = "btnReloadActiveMap";
            this.btnReloadActiveMap.TabIndex = 1;
            this.btnReloadActiveMap.Text = "RELOAD ACTIVE MAP";
            //
            // grpAction
            //
            this.grpAction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.grpAction.BackColor = System.Drawing.Color.White;
            this.grpAction.Controls.Add(this.actionBar);
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.ForeColor = System.Drawing.Color.Black;
            this.grpAction.Margin = new System.Windows.Forms.Padding(3);
            this.grpAction.Name = "grpAction";
            this.grpAction.Padding = new System.Windows.Forms.Padding(3);
            this.grpAction.Size = new System.Drawing.Size(410, 172);
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
            //
            // actionBar
            //
            this.actionBar.BackColor = System.Drawing.Color.White;
            this.actionBar.ColumnCount = 1;
            this.actionBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionBar.Controls.Add(this.btnThetaMatchMove, 0, 0);
            this.actionBar.Controls.Add(this.btnXyMatchMove, 0, 1);
            this.actionBar.Controls.Add(this.btnManualDieMapOffsetApply, 0, 2);
            this.actionBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionBar.Location = new System.Drawing.Point(6, 24);
            this.actionBar.Margin = new System.Windows.Forms.Padding(0);
            this.actionBar.Name = "actionBar";
            this.actionBar.Padding = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.actionBar.RowCount = 3;
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionBar.Size = new System.Drawing.Size(398, 142);
            this.actionBar.TabIndex = 0;
            //
            // btnThetaMatchMove
            //
            this.btnThetaMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnThetaMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnThetaMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnThetaMatchMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnThetaMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnThetaMatchMove.Margin = new System.Windows.Forms.Padding(3);
            this.btnThetaMatchMove.Name = "btnThetaMatchMove";
            this.btnThetaMatchMove.TabIndex = 0;
            this.btnThetaMatchMove.Text = "T 보정";
            //
            // btnXyMatchMove
            //
            this.btnXyMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnXyMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnXyMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXyMatchMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnXyMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnXyMatchMove.Margin = new System.Windows.Forms.Padding(3);
            this.btnXyMatchMove.Name = "btnXyMatchMove";
            this.btnXyMatchMove.TabIndex = 1;
            this.btnXyMatchMove.Text = "다이 검출";
            //
            // btnManualDieMapOffsetApply
            //
            this.btnManualDieMapOffsetApply.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnManualDieMapOffsetApply.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnManualDieMapOffsetApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnManualDieMapOffsetApply.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnManualDieMapOffsetApply.ForeColor = System.Drawing.Color.White;
            this.btnManualDieMapOffsetApply.Margin = new System.Windows.Forms.Padding(3);
            this.btnManualDieMapOffsetApply.Name = "btnManualDieMapOffsetApply";
            this.btnManualDieMapOffsetApply.TabIndex = 2;
            this.btnManualDieMapOffsetApply.Text = "Offset 적용";
            //
            // rbStandard
            //
            this.rbStandard.AutoSize = true;
            this.rbStandard.Checked = true;
            this.rbStandard.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbStandard.Name = "rbStandard";
            this.rbStandard.TabIndex = 0;
            this.rbStandard.TabStop = true;
            this.rbStandard.Text = "STANDARD";
            this.rbStandard.UseVisualStyleBackColor = true;
            this.rbStandard.Visible = false;
            //
            // rbStartIndex
            //
            this.rbStartIndex.AutoSize = true;
            this.rbStartIndex.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbStartIndex.Name = "rbStartIndex";
            this.rbStartIndex.TabIndex = 1;
            this.rbStartIndex.Text = "START INDEX";
            this.rbStartIndex.Visible = false;
            //
            // rbSelectPickStatus
            //
            this.rbSelectPickStatus.AutoSize = true;
            this.rbSelectPickStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbSelectPickStatus.Name = "rbSelectPickStatus";
            this.rbSelectPickStatus.TabIndex = 2;
            this.rbSelectPickStatus.Text = "SELECT PICK STATUS";
            this.rbSelectPickStatus.UseVisualStyleBackColor = true;
            this.rbSelectPickStatus.Visible = false;
            //
            // rbDragPickStatus
            //
            this.rbDragPickStatus.AutoSize = true;
            this.rbDragPickStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbDragPickStatus.Name = "rbDragPickStatus";
            this.rbDragPickStatus.TabIndex = 3;
            this.rbDragPickStatus.Text = "DRAG PICK STATUS";
            this.rbDragPickStatus.Visible = false;
            //
            // btnPickStatusSave
            //
            this.btnPickStatusSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPickStatusSave.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnPickStatusSave.ForeColor = System.Drawing.Color.White;
            this.btnPickStatusSave.Name = "btnPickStatusSave";
            this.btnPickStatusSave.TabIndex = 4;
            this.btnPickStatusSave.Text = "SELECT PICK STATUS SAVE";
            this.btnPickStatusSave.Visible = false;
            //
            // btnNeedleBlockDown
            //
            this.btnNeedleBlockDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleBlockDown.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnNeedleBlockDown.ForeColor = System.Drawing.Color.White;
            this.btnNeedleBlockDown.Name = "btnNeedleBlockDown";
            this.btnNeedleBlockDown.TabIndex = 1;
            this.btnNeedleBlockDown.Text = "NEEDLE BLOCK DOWN";
            this.btnNeedleBlockDown.Visible = false;
            //
            // lblHeader
            //
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(0, 0);
            this.lblHeader.TabIndex = 100;
            this.lblHeader.Tag = "i18n:work.page.inputMap";
            this.lblHeader.Text = "INPUT STAGE DIE MAP";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblHeader.Visible = false;
            //
            // lblMapTitle
            //
            this.lblMapTitle.Name = "lblMapTitle";
            this.lblMapTitle.Size = new System.Drawing.Size(0, 0);
            this.lblMapTitle.TabIndex = 101;
            this.lblMapTitle.Tag = "i18n:recipe.inputMapCreate";
            this.lblMapTitle.Text = "INPUT DIE MAP CREATE";
            this.lblMapTitle.Visible = false;
            //
            // lblProjectCaption
            //
            this.lblProjectCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblProjectCaption.ForeColor = System.Drawing.Color.White;
            this.lblProjectCaption.Name = "lblProjectCaption";
            this.lblProjectCaption.Size = new System.Drawing.Size(0, 0);
            this.lblProjectCaption.TabIndex = 102;
            this.lblProjectCaption.Text = "Project Name :";
            this.lblProjectCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblProjectCaption.Visible = false;
            //
            // lblProjectValue
            //
            this.lblProjectValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblProjectValue.ForeColor = System.Drawing.Color.White;
            this.lblProjectValue.Name = "lblProjectValue";
            this.lblProjectValue.Size = new System.Drawing.Size(0, 0);
            this.lblProjectValue.TabIndex = 103;
            this.lblProjectValue.Text = "--";
            this.lblProjectValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblProjectValue.Visible = false;
            //
            // lblBarcodeCaption
            //
            this.lblBarcodeCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBarcodeCaption.ForeColor = System.Drawing.Color.White;
            this.lblBarcodeCaption.Name = "lblBarcodeCaption";
            this.lblBarcodeCaption.Size = new System.Drawing.Size(0, 0);
            this.lblBarcodeCaption.TabIndex = 104;
            this.lblBarcodeCaption.Text = "Barcode Name :";
            this.lblBarcodeCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBarcodeCaption.Visible = false;
            //
            // lblBarcodeValue
            //
            this.lblBarcodeValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBarcodeValue.ForeColor = System.Drawing.Color.White;
            this.lblBarcodeValue.Name = "lblBarcodeValue";
            this.lblBarcodeValue.Size = new System.Drawing.Size(0, 0);
            this.lblBarcodeValue.TabIndex = 105;
            this.lblBarcodeValue.Text = "--";
            this.lblBarcodeValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBarcodeValue.Visible = false;
            //
            // lblBinCaption
            //
            this.lblBinCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBinCaption.ForeColor = System.Drawing.Color.White;
            this.lblBinCaption.Name = "lblBinCaption";
            this.lblBinCaption.Size = new System.Drawing.Size(0, 0);
            this.lblBinCaption.TabIndex = 106;
            this.lblBinCaption.Text = "1Bin :";
            this.lblBinCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBinCaption.Visible = false;
            //
            // lblBinValue
            //
            this.lblBinValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBinValue.ForeColor = System.Drawing.Color.White;
            this.lblBinValue.Name = "lblBinValue";
            this.lblBinValue.Size = new System.Drawing.Size(0, 0);
            this.lblBinValue.TabIndex = 107;
            this.lblBinValue.Text = "--";
            this.lblBinValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBinValue.Visible = false;
            //
            // btnClose
            //
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(0, 0);
            this.btnClose.TabIndex = 108;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Visible = false;
            //
            // colIndex
            //
            this.colIndex.FillWeight = 45F;
            this.colIndex.HeaderText = "Index";
            this.colIndex.Name = "colIndex";
            this.colIndex.ReadOnly = true;
            this.colIndex.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colGridX
            //
            this.colGridX.FillWeight = 55F;
            this.colGridX.HeaderText = "DieMapX";
            this.colGridX.Name = "colGridX";
            this.colGridX.ReadOnly = true;
            this.colGridX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colGridY
            //
            this.colGridY.FillWeight = 55F;
            this.colGridY.HeaderText = "DieMapY";
            this.colGridY.Name = "colGridY";
            this.colGridY.ReadOnly = true;
            this.colGridY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colTarget
            //
            this.colTarget.FillWeight = 65F;
            this.colTarget.HeaderText = "State";
            this.colTarget.Name = "colTarget";
            this.colTarget.ReadOnly = true;
            this.colTarget.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colResult
            //
            this.colResult.FillWeight = 80F;
            this.colResult.HeaderText = "Result";
            this.colResult.Name = "colResult";
            this.colResult.ReadOnly = true;
            this.colResult.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colBin
            //
            this.colBin.FillWeight = 55F;
            this.colBin.HeaderText = "Bin";
            this.colBin.Name = "colBin";
            this.colBin.ReadOnly = true;
            this.colBin.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colAxisX
            //
            this.colAxisX.FillWeight = 80F;
            this.colAxisX.HeaderText = "X(mm)";
            this.colAxisX.Name = "colAxisX";
            this.colAxisX.ReadOnly = true;
            this.colAxisX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colAxisY
            //
            this.colAxisY.FillWeight = 80F;
            this.colAxisY.HeaderText = "Y(mm)";
            this.colAxisY.Name = "colAxisY";
            this.colAxisY.ReadOnly = true;
            this.colAxisY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colDieUid
            //
            this.colDieUid.FillWeight = 180F;
            this.colDieUid.HeaderText = "Die UID";
            this.colDieUid.Name = "colDieUid";
            this.colDieUid.ReadOnly = true;
            this.colDieUid.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // events (디자이너 관리)
            //
            this.gridDieList.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridDieList_CellClick);
            this.gridDieList.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridDieList_CellDoubleClick);
            this.gridDieList.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.OnGridDieListCellMouseDown);
            this.btnReloadActiveMap.Click += new System.EventHandler(this.btnReloadActiveMap_Click);
            this.btnPickStatusSave.Click += new System.EventHandler(this.btnPickStatusSave_Click);
            this.btnApplyDieState.Click += new System.EventHandler(this.btnApplyDieState_Click);
            this.btnManualAlignComplete.Click += new System.EventHandler(this.btnManualAlignComplete_Click);
            this.btnNeedleBlockDown.Click += new System.EventHandler(this.btnNeedleBlockDown_Click);
            this.btnThetaMatchMove.Click += new System.EventHandler(this.btnThetaMatchMove_Click);
            this.btnXyMatchMove.Click += new System.EventHandler(this.btnXyMatchMove_Click);
            this.btnManualDieMapOffsetApply.Click += new System.EventHandler(this.btnManualDieMapOffsetApply_Click);
            //
            // InputStageMapTransferPage
            //
            this.AutoScroll = false;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "InputStageMapTransferPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.grpCreate.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridDieList)).EndInit();
            this.infoEditBody.ResumeLayout(false);
            this.grpMapInfo.ResumeLayout(false);
            this.mapInfoLayout.ResumeLayout(false);
            this.dieStateArea.ResumeLayout(false);
            this.grpDieState.ResumeLayout(false);
            this.dieStateLayout.ResumeLayout(false);
            this.detachedButtonRow.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private DataGridViewTextBoxColumn colIndex;
        private DataGridViewTextBoxColumn colGridX;
        private DataGridViewTextBoxColumn colGridY;
        private DataGridViewTextBoxColumn colTarget;
        private DataGridViewTextBoxColumn colResult;
        private DataGridViewTextBoxColumn colBin;
        private DataGridViewTextBoxColumn colAxisX;
        private DataGridViewTextBoxColumn colAxisY;
        private DataGridViewTextBoxColumn colDieUid;
    }
}
