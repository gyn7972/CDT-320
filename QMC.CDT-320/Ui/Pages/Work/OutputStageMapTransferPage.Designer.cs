using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Ui.Controls;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    partial class OutputStageMapTransferPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel mapLayout;
        private GroupBox grpReceiveMap;
        private GroupBox grpDieGrid;
        private GroupBox grpAction;
        private TableLayoutPanel sideLayout;
        private Label lblProjectCaption;
        private Label lblProjectValue;
        private Label lblBarcodeCaption;
        private Label lblBarcodeValue;
        private Label lblBinCaption;
        private Label lblBinValue;
        private Label lblMapTitle;
        private DieMapView mapView;
        private DataGridView gridDieList;
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
        private GroupBox grpMode;
        private TableLayoutPanel modeLayout;
        private GroupBox grpOutputDieState;
        private TableLayoutPanel outputDieStateLayout;
        private RadioButton rbStandard;
        private RadioButton rbStartIndex;
        private RadioButton rbSelectPickStatus;
        private RadioButton rbDragPickStatus;
        private RadioButton rdoOutputStateGood;
        private RadioButton rdoOutputStateNg;
        private Button btnPickStatusSave;
        private Button btnReloadActiveMap;
        private Button btnApplyOutputDieState;
        private TableLayoutPanel actionLayout;
        private ActionButton btnManualAlignComplete;
        private ActionButton btnNeedleBlockDown;
        private ActionButton btnThetaMatchMove;
        private ActionButton btnXyMatchMove;
        private Button btnClose;

        private void InitializeComponent()
        {
            DataGridViewCellStyle headerStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpReceiveMap = new System.Windows.Forms.GroupBox();
            this.mapLayout = new System.Windows.Forms.TableLayoutPanel();
            this.mapView = new QMC.CDT320.Ui.Controls.DieMapView();
            this.grpDieGrid = new System.Windows.Forms.GroupBox();
            this.gridDieList = new System.Windows.Forms.DataGridView();
            this.sideLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpMapInfo = new System.Windows.Forms.GroupBox();
            this.mapInfoLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpMode = new System.Windows.Forms.GroupBox();
            this.modeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpOutputDieState = new System.Windows.Forms.GroupBox();
            this.outputDieStateLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpAction = new System.Windows.Forms.GroupBox();
            this.actionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblProjectCaption = new System.Windows.Forms.Label();
            this.lblProjectValue = new System.Windows.Forms.Label();
            this.lblBarcodeCaption = new System.Windows.Forms.Label();
            this.lblBarcodeValue = new System.Windows.Forms.Label();
            this.lblBinCaption = new System.Windows.Forms.Label();
            this.lblBinValue = new System.Windows.Forms.Label();
            this.lblMapTitle = new System.Windows.Forms.Label();
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
            this.rbStandard = new System.Windows.Forms.RadioButton();
            this.rbStartIndex = new System.Windows.Forms.RadioButton();
            this.rbSelectPickStatus = new System.Windows.Forms.RadioButton();
            this.rbDragPickStatus = new System.Windows.Forms.RadioButton();
            this.rdoOutputStateGood = new System.Windows.Forms.RadioButton();
            this.rdoOutputStateNg = new System.Windows.Forms.RadioButton();
            this.btnReloadActiveMap = new System.Windows.Forms.Button();
            this.btnPickStatusSave = new System.Windows.Forms.Button();
            this.btnApplyOutputDieState = new System.Windows.Forms.Button();
            this.btnManualAlignComplete = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleBlockDown = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnThetaMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnXyMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnClose = new System.Windows.Forms.Button();
            this.colIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGridX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGridY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEquipmentGridX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEquipmentGridY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAxisX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAxisY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDieUid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rootLayout.SuspendLayout();
            this.grpReceiveMap.SuspendLayout();
            this.mapLayout.SuspendLayout();
            this.grpDieGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridDieList)).BeginInit();
            this.sideLayout.SuspendLayout();
            this.grpMapInfo.SuspendLayout();
            this.mapInfoLayout.SuspendLayout();
            this.grpMode.SuspendLayout();
            this.modeLayout.SuspendLayout();
            this.grpOutputDieState.SuspendLayout();
            this.outputDieStateLayout.SuspendLayout();
            this.grpAction.SuspendLayout();
            this.actionLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.Controls.Add(this.grpReceiveMap, 0, 0);
            this.rootLayout.Controls.Add(this.sideLayout, 1, 0);
            this.rootLayout.Controls.Add(this.gridDieList, 0, 1);
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
            // grpReceiveMap
            //
            this.grpReceiveMap.BackColor = System.Drawing.Color.White;
            this.grpReceiveMap.Controls.Add(this.mapLayout);
            this.grpReceiveMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpReceiveMap.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpReceiveMap.ForeColor = System.Drawing.Color.Black;
            this.grpReceiveMap.Margin = new System.Windows.Forms.Padding(3);
            this.grpReceiveMap.Padding = new System.Windows.Forms.Padding(3);
            this.grpReceiveMap.TabStop = false;
            this.grpReceiveMap.Text = "OUTPUT GOOD RECEIVE MAP";
            //
            // mapLayout
            //
            this.mapLayout.BackColor = System.Drawing.Color.White;
            this.mapLayout.ColumnCount = 1;
            this.mapLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapLayout.Controls.Add(this.mapView, 0, 0);
            this.mapLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapLayout.Location = new System.Drawing.Point(6, 24);
            this.mapLayout.Margin = new System.Windows.Forms.Padding(0);
            this.mapLayout.Name = "mapLayout";
            this.mapLayout.RowCount = 1;
            this.mapLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapLayout.Size = new System.Drawing.Size(815, 412);
            this.mapLayout.TabIndex = 0;
            //
            // mapView
            //
            this.mapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(221)))), ((int)(((byte)(221)))));
            this.mapView.Caption = "OUTPUT GOOD RECEIVE MAP";
            this.mapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapView.CompactUsedBounds = true;
            this.mapView.ShowWaferOutline = true;
            this.mapView.ShowEquipmentAxes = false;
            this.mapView.EnableRectangleSelection = true;
            this.mapView.Location = new System.Drawing.Point(0, 0);
            this.mapView.Margin = new System.Windows.Forms.Padding(0);
            this.mapView.Map = null;
            this.mapView.Name = "mapView";
            this.mapView.Size = new System.Drawing.Size(815, 412);
            this.mapView.TabIndex = 0;
            //
            // grpDieGrid
            //
            this.grpDieGrid.Margin = new System.Windows.Forms.Padding(0, 1, 1, 0);
            this.grpDieGrid.Location = new System.Drawing.Point(8, 454);
            this.grpDieGrid.Size = new System.Drawing.Size(827, 438);
            //
            // gridDieList
            //
            this.gridDieList.AllowUserToAddRows = false;
            this.gridDieList.AllowUserToDeleteRows = false;
            this.gridDieList.AllowUserToResizeRows = false;
            this.gridDieList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridDieList.BackgroundColor = System.Drawing.Color.White;
            this.gridDieList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            headerStyle.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            headerStyle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            headerStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            headerStyle.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            headerStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridDieList.ColumnHeadersDefaultCellStyle = headerStyle;
            this.gridDieList.ColumnHeadersHeight = 32;
            this.gridDieList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridDieList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colIndex,
            this.colGridX,
            this.colGridY,
            this.colEquipmentGridX,
            this.colEquipmentGridY,
            this.colTarget,
            this.colResult,
            this.colBin,
            this.colAxisX,
            this.colAxisY,
            this.colDieUid});
            cellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            cellStyle.BackColor = System.Drawing.Color.White;
            cellStyle.Font = new System.Drawing.Font("Consolas", 9F);
            cellStyle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            cellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(221)))), ((int)(((byte)(235)))), ((int)(((byte)(255)))));
            cellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            cellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.gridDieList.DefaultCellStyle = cellStyle;
            this.gridDieList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridDieList.EnableHeadersVisualStyles = false;
            this.gridDieList.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.gridDieList.Location = new System.Drawing.Point(0, 451);
            this.gridDieList.Margin = new System.Windows.Forms.Padding(3);
            this.gridDieList.MultiSelect = false;
            this.gridDieList.Name = "gridDieList";
            this.gridDieList.ReadOnly = true;
            this.gridDieList.RowHeadersVisible = false;
            this.gridDieList.RowTemplate.Height = 22;
            this.gridDieList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridDieList.Size = new System.Drawing.Size(838, 449);
            this.gridDieList.TabIndex = 0;
            //
            // sideLayout
            //
            this.sideLayout.BackColor = System.Drawing.Color.White;
            this.sideLayout.ColumnCount = 2;
            this.sideLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sideLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sideLayout.Controls.Add(this.grpMapInfo, 0, 0);
            this.sideLayout.Controls.Add(this.grpMode, 1, 0);
            this.sideLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sideLayout.Location = new System.Drawing.Point(843, 8);
            this.sideLayout.Margin = new System.Windows.Forms.Padding(3);
            this.sideLayout.Padding = new System.Windows.Forms.Padding(0);
            this.sideLayout.Name = "sideLayout";
            this.sideLayout.RowCount = 1;
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sideLayout.Size = new System.Drawing.Size(827, 442);
            this.sideLayout.TabIndex = 1;
            //
            // grpMapInfo
            //
            this.grpMapInfo.BackColor = System.Drawing.Color.White;
            this.grpMapInfo.Controls.Add(this.mapInfoLayout);
            this.grpMapInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMapInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpMapInfo.ForeColor = System.Drawing.Color.Black;
            this.grpMapInfo.Margin = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.grpMapInfo.Padding = new System.Windows.Forms.Padding(3);
            this.grpMapInfo.TabStop = false;
            this.grpMapInfo.Text = "BIN / DIE INFO";
            this.grpMapInfo.Size = new System.Drawing.Size(412, 449);
            //
            // mapInfoLayout
            //
            this.mapInfoLayout.BackColor = System.Drawing.Color.White;
            this.mapInfoLayout.ColumnCount = 2;
            this.mapInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.mapInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 56F));
            this.mapInfoLayout.Controls.Add(this.lblProjectCaption, 0, 0);
            this.mapInfoLayout.Controls.Add(this.lblProjectValue, 1, 0);
            this.mapInfoLayout.Controls.Add(this.lblBarcodeCaption, 0, 1);
            this.mapInfoLayout.Controls.Add(this.lblBarcodeValue, 1, 1);
            this.mapInfoLayout.Controls.Add(this.lblBinCaption, 0, 2);
            this.mapInfoLayout.Controls.Add(this.lblBinValue, 1, 2);
            this.mapInfoLayout.Controls.Add(this.lblChipWCaption, 0, 3);
            this.mapInfoLayout.Controls.Add(this.lblChipW, 1, 3);
            this.mapInfoLayout.Controls.Add(this.lblChipHCaption, 0, 4);
            this.mapInfoLayout.Controls.Add(this.lblChipH, 1, 4);
            this.mapInfoLayout.Controls.Add(this.lblPitchXCaption, 0, 5);
            this.mapInfoLayout.Controls.Add(this.lblPitchX, 1, 5);
            this.mapInfoLayout.Controls.Add(this.lblPitchYCaption, 0, 6);
            this.mapInfoLayout.Controls.Add(this.lblPitchY, 1, 6);
            this.mapInfoLayout.Controls.Add(this.lblWaferDiaCaption, 0, 7);
            this.mapInfoLayout.Controls.Add(this.lblWaferDia, 1, 7);
            this.mapInfoLayout.Controls.Add(this.lblAxisXCaption, 0, 8);
            this.mapInfoLayout.Controls.Add(this.lblAxisX, 1, 8);
            this.mapInfoLayout.Controls.Add(this.lblAxisYCaption, 0, 9);
            this.mapInfoLayout.Controls.Add(this.lblAxisY, 1, 9);
            this.mapInfoLayout.Controls.Add(this.lblBinRankCaption, 0, 10);
            this.mapInfoLayout.Controls.Add(this.lblBinRank, 1, 10);
            this.mapInfoLayout.Controls.Add(this.lblDieNumCaption, 0, 11);
            this.mapInfoLayout.Controls.Add(this.lblDieNum, 1, 11);
            this.mapInfoLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapInfoLayout.Location = new System.Drawing.Point(6, 24);
            this.mapInfoLayout.Margin = new System.Windows.Forms.Padding(0);
            this.mapInfoLayout.Name = "mapInfoLayout";
            this.mapInfoLayout.Padding = new System.Windows.Forms.Padding(1);
            this.mapInfoLayout.RowCount = 12;
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.Size = new System.Drawing.Size(397, 412);
            this.mapInfoLayout.TabIndex = 0;
            //
            // info labels (caption/value)
            //
            this.lblProjectCaption.AutoEllipsis = true;
            this.lblProjectCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblProjectCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblProjectCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProjectCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblProjectCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblProjectCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblProjectCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblProjectCaption.TabIndex = 0;
            this.lblProjectCaption.Text = "Project Name";
            this.lblProjectCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblProjectValue.AutoEllipsis = true;
            this.lblProjectValue.BackColor = System.Drawing.Color.White;
            this.lblProjectValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblProjectValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProjectValue.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblProjectValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblProjectValue.Margin = new System.Windows.Forms.Padding(1);
            this.lblProjectValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblProjectValue.TabIndex = 1;
            this.lblProjectValue.Text = "--";
            this.lblProjectValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblBarcodeCaption.AutoEllipsis = true;
            this.lblBarcodeCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblBarcodeCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBarcodeCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBarcodeCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblBarcodeCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblBarcodeCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblBarcodeCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBarcodeCaption.TabIndex = 2;
            this.lblBarcodeCaption.Text = "Source Wafer :";
            this.lblBarcodeCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBarcodeValue.AutoEllipsis = true;
            this.lblBarcodeValue.BackColor = System.Drawing.Color.White;
            this.lblBarcodeValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBarcodeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBarcodeValue.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblBarcodeValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblBarcodeValue.Margin = new System.Windows.Forms.Padding(1);
            this.lblBarcodeValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblBarcodeValue.TabIndex = 3;
            this.lblBarcodeValue.Text = "--";
            this.lblBarcodeValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblBinCaption.AutoEllipsis = true;
            this.lblBinCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblBinCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBinCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblBinCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblBinCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBinCaption.TabIndex = 4;
            this.lblBinCaption.Text = "Side :";
            this.lblBinCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBinValue.AutoEllipsis = true;
            this.lblBinValue.BackColor = System.Drawing.Color.White;
            this.lblBinValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBinValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinValue.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblBinValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblBinValue.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblBinValue.TabIndex = 5;
            this.lblBinValue.Text = "--";
            this.lblBinValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblChipWCaption.AutoEllipsis = true;
            this.lblChipWCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblChipWCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipWCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipWCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblChipWCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblChipWCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipWCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblChipWCaption.TabIndex = 6;
            this.lblChipWCaption.Text = "Grid X";
            this.lblChipWCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblChipW.AutoEllipsis = true;
            this.lblChipW.BackColor = System.Drawing.Color.White;
            this.lblChipW.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipW.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipW.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblChipW.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblChipW.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipW.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblChipW.TabIndex = 7;
            this.lblChipW.Text = "0";
            this.lblChipW.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblChipHCaption.AutoEllipsis = true;
            this.lblChipHCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblChipHCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipHCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipHCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblChipHCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblChipHCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipHCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblChipHCaption.TabIndex = 8;
            this.lblChipHCaption.Text = "Grid Y";
            this.lblChipHCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblChipH.AutoEllipsis = true;
            this.lblChipH.BackColor = System.Drawing.Color.White;
            this.lblChipH.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipH.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipH.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblChipH.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblChipH.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipH.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblChipH.TabIndex = 9;
            this.lblChipH.Text = "0";
            this.lblChipH.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblPitchXCaption.AutoEllipsis = true;
            this.lblPitchXCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblPitchXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchXCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblPitchXCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblPitchXCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblPitchXCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPitchXCaption.TabIndex = 10;
            this.lblPitchXCaption.Text = "Center Step X";
            this.lblPitchXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblPitchX.AutoEllipsis = true;
            this.lblPitchX.BackColor = System.Drawing.Color.White;
            this.lblPitchX.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchX.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblPitchX.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblPitchX.Margin = new System.Windows.Forms.Padding(1);
            this.lblPitchX.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblPitchX.TabIndex = 11;
            this.lblPitchX.Text = "0";
            this.lblPitchX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblPitchYCaption.AutoEllipsis = true;
            this.lblPitchYCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblPitchYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchYCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblPitchYCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblPitchYCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblPitchYCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPitchYCaption.TabIndex = 12;
            this.lblPitchYCaption.Text = "Center Step Y";
            this.lblPitchYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblPitchY.AutoEllipsis = true;
            this.lblPitchY.BackColor = System.Drawing.Color.White;
            this.lblPitchY.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchY.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblPitchY.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblPitchY.Margin = new System.Windows.Forms.Padding(1);
            this.lblPitchY.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblPitchY.TabIndex = 13;
            this.lblPitchY.Text = "0";
            this.lblPitchY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblWaferDiaCaption.AutoEllipsis = true;
            this.lblWaferDiaCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblWaferDiaCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDiaCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDiaCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblWaferDiaCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblWaferDiaCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblWaferDiaCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblWaferDiaCaption.TabIndex = 14;
            this.lblWaferDiaCaption.Text = "Progress";
            this.lblWaferDiaCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblWaferDia.AutoEllipsis = true;
            this.lblWaferDia.BackColor = System.Drawing.Color.White;
            this.lblWaferDia.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDia.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDia.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblWaferDia.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblWaferDia.Margin = new System.Windows.Forms.Padding(1);
            this.lblWaferDia.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblWaferDia.TabIndex = 15;
            this.lblWaferDia.Text = "0/0";
            this.lblWaferDia.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblAxisXCaption.AutoEllipsis = true;
            this.lblAxisXCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblAxisXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisXCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblAxisXCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblAxisXCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisXCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxisXCaption.TabIndex = 16;
            this.lblAxisXCaption.Text = "X (mm)";
            this.lblAxisXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAxisX.AutoEllipsis = true;
            this.lblAxisX.BackColor = System.Drawing.Color.White;
            this.lblAxisX.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisX.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblAxisX.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblAxisX.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisX.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxisX.TabIndex = 17;
            this.lblAxisX.Text = "0";
            this.lblAxisX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblAxisYCaption.AutoEllipsis = true;
            this.lblAxisYCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblAxisYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisYCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblAxisYCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblAxisYCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisYCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxisYCaption.TabIndex = 18;
            this.lblAxisYCaption.Text = "Y (mm)";
            this.lblAxisYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAxisY.AutoEllipsis = true;
            this.lblAxisY.BackColor = System.Drawing.Color.White;
            this.lblAxisY.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisY.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblAxisY.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblAxisY.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisY.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxisY.TabIndex = 19;
            this.lblAxisY.Text = "0";
            this.lblAxisY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblBinRankCaption.AutoEllipsis = true;
            this.lblBinRankCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblBinRankCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBinRankCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinRankCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblBinRankCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblBinRankCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinRankCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBinRankCaption.TabIndex = 20;
            this.lblBinRankCaption.Text = "Bin / State";
            this.lblBinRankCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblBinRank.AutoEllipsis = true;
            this.lblBinRank.BackColor = System.Drawing.Color.White;
            this.lblBinRank.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBinRank.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinRank.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblBinRank.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblBinRank.Margin = new System.Windows.Forms.Padding(1);
            this.lblBinRank.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblBinRank.TabIndex = 21;
            this.lblBinRank.Text = "0";
            this.lblBinRank.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblDieNumCaption.AutoEllipsis = true;
            this.lblDieNumCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(238)))), ((int)(((byte)(241)))));
            this.lblDieNumCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieNumCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieNumCaption.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            this.lblDieNumCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.lblDieNumCaption.Margin = new System.Windows.Forms.Padding(1);
            this.lblDieNumCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDieNumCaption.TabIndex = 22;
            this.lblDieNumCaption.Text = "Next Target";
            this.lblDieNumCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblDieNum.AutoEllipsis = true;
            this.lblDieNum.BackColor = System.Drawing.Color.White;
            this.lblDieNum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieNum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieNum.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblDieNum.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(29)))), ((int)(((byte)(34)))));
            this.lblDieNum.Margin = new System.Windows.Forms.Padding(1);
            this.lblDieNum.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblDieNum.TabIndex = 23;
            this.lblDieNum.Text = "0/0";
            this.lblDieNum.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // grpMode
            //
            this.grpMode.BackColor = System.Drawing.Color.White;
            this.grpMode.Controls.Add(this.modeLayout);
            this.grpMode.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpMode.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpMode.ForeColor = System.Drawing.Color.Black;
            this.grpMode.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.grpMode.Padding = new System.Windows.Forms.Padding(3);
            this.grpMode.TabStop = false;
            this.grpMode.Text = "OUTPUT STAGE";
            this.grpMode.Size = new System.Drawing.Size(410, 368);
            //
            // modeLayout
            //
            this.modeLayout.BackColor = System.Drawing.Color.White;
            this.modeLayout.ColumnCount = 4;
            this.modeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.modeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.modeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.modeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.modeLayout.Controls.Add(this.rbStandard, 2, 0);
            this.modeLayout.Controls.Add(this.rbStartIndex, 3, 0);
            this.modeLayout.Controls.Add(this.grpOutputDieState, 0, 1);
            this.modeLayout.Controls.Add(this.btnReloadActiveMap, 0, 2);
            this.modeLayout.Controls.Add(this.btnPickStatusSave, 2, 2);
            this.modeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modeLayout.Location = new System.Drawing.Point(6, 24);
            this.modeLayout.Margin = new System.Windows.Forms.Padding(0);
            this.modeLayout.Name = "modeLayout";
            this.modeLayout.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.modeLayout.RowCount = 3;
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 216F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.modeLayout.Size = new System.Drawing.Size(398, 338);
            this.modeLayout.TabIndex = 0;
            this.modeLayout.SetColumnSpan(this.rbStandard, 1);
            this.modeLayout.SetColumnSpan(this.rbStartIndex, 1);
            this.modeLayout.SetColumnSpan(this.grpOutputDieState, 4);
            this.modeLayout.SetColumnSpan(this.btnReloadActiveMap, 2);
            this.modeLayout.SetColumnSpan(this.btnPickStatusSave, 2);
            //
            // rbStandard (GOOD toggle)
            //
            this.rbStandard.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbStandard.AutoSize = false;
            this.rbStandard.BackColor = System.Drawing.Color.White;
            this.rbStandard.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbStandard.Checked = true;
            this.rbStandard.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rbStandard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStandard.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.rbStandard.FlatAppearance.BorderSize = 1;
            this.rbStandard.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(228)))), ((int)(((byte)(236)))), ((int)(((byte)(246)))));
            this.rbStandard.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(247)))), ((int)(((byte)(251)))));
            this.rbStandard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbStandard.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.rbStandard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(102)))), ((int)(((byte)(179)))));
            this.rbStandard.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.rbStandard.Padding = new System.Windows.Forms.Padding(0);
            this.rbStandard.TabIndex = 0;
            this.rbStandard.TabStop = true;
            this.rbStandard.Text = "GOOD";
            this.rbStandard.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbStandard.UseVisualStyleBackColor = false;
            //
            // rbStartIndex (NG toggle)
            //
            this.rbStartIndex.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbStartIndex.AutoSize = false;
            this.rbStartIndex.BackColor = System.Drawing.Color.White;
            this.rbStartIndex.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbStartIndex.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rbStartIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStartIndex.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(201)))), ((int)(((byte)(207)))), ((int)(((byte)(216)))));
            this.rbStartIndex.FlatAppearance.BorderSize = 1;
            this.rbStartIndex.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(228)))), ((int)(((byte)(236)))), ((int)(((byte)(246)))));
            this.rbStartIndex.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(247)))), ((int)(((byte)(251)))));
            this.rbStartIndex.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbStartIndex.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold);
            this.rbStartIndex.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(46)))));
            this.rbStartIndex.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this.rbStartIndex.Padding = new System.Windows.Forms.Padding(0);
            this.rbStartIndex.TabIndex = 1;
            this.rbStartIndex.TabStop = true;
            this.rbStartIndex.Text = "NG";
            this.rbStartIndex.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbStartIndex.UseVisualStyleBackColor = false;
            //
            // rbSelectPickStatus
            //
            this.rbSelectPickStatus.AutoSize = false;
            this.rbSelectPickStatus.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rbSelectPickStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbSelectPickStatus.Checked = true;
            this.rbSelectPickStatus.Enabled = true;
            this.rbSelectPickStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbSelectPickStatus.ForeColor = System.Drawing.Color.Black;
            this.rbSelectPickStatus.Margin = new System.Windows.Forms.Padding(3);
            this.rbSelectPickStatus.Padding = new System.Windows.Forms.Padding(0);
            this.rbSelectPickStatus.TabIndex = 2;
            this.rbSelectPickStatus.TabStop = true;
            this.rbSelectPickStatus.Text = "WAIT / 대기";
            this.rbSelectPickStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rbSelectPickStatus.UseVisualStyleBackColor = true;
            //
            // rbDragPickStatus
            //
            this.rbDragPickStatus.AutoSize = false;
            this.rbDragPickStatus.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rbDragPickStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbDragPickStatus.Enabled = true;
            this.rbDragPickStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbDragPickStatus.ForeColor = System.Drawing.Color.Black;
            this.rbDragPickStatus.Margin = new System.Windows.Forms.Padding(3);
            this.rbDragPickStatus.Padding = new System.Windows.Forms.Padding(0);
            this.rbDragPickStatus.TabIndex = 3;
            this.rbDragPickStatus.TabStop = true;
            this.rbDragPickStatus.Text = "SKIP / 제외";
            this.rbDragPickStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rbDragPickStatus.UseVisualStyleBackColor = true;
            //
            // rdoOutputStateGood
            //
            this.rdoOutputStateGood.AutoSize = false;
            this.rdoOutputStateGood.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rdoOutputStateGood.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoOutputStateGood.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rdoOutputStateGood.ForeColor = System.Drawing.Color.Black;
            this.rdoOutputStateGood.Margin = new System.Windows.Forms.Padding(3);
            this.rdoOutputStateGood.Padding = new System.Windows.Forms.Padding(0);
            this.rdoOutputStateGood.TabIndex = 4;
            this.rdoOutputStateGood.TabStop = true;
            this.rdoOutputStateGood.Text = "GOOD / 완료";
            this.rdoOutputStateGood.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rdoOutputStateGood.UseVisualStyleBackColor = true;
            //
            // rdoOutputStateNg
            //
            this.rdoOutputStateNg.AutoSize = false;
            this.rdoOutputStateNg.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rdoOutputStateNg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoOutputStateNg.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rdoOutputStateNg.ForeColor = System.Drawing.Color.Black;
            this.rdoOutputStateNg.Margin = new System.Windows.Forms.Padding(3);
            this.rdoOutputStateNg.Padding = new System.Windows.Forms.Padding(0);
            this.rdoOutputStateNg.TabIndex = 5;
            this.rdoOutputStateNg.TabStop = true;
            this.rdoOutputStateNg.Text = "NG / 불량";
            this.rdoOutputStateNg.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.rdoOutputStateNg.UseVisualStyleBackColor = true;
            //
            // grpOutputDieState
            //
            this.grpOutputDieState.BackColor = System.Drawing.Color.White;
            this.grpOutputDieState.Controls.Add(this.outputDieStateLayout);
            this.grpOutputDieState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOutputDieState.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpOutputDieState.ForeColor = System.Drawing.Color.Black;
            this.grpOutputDieState.Margin = new System.Windows.Forms.Padding(0);
            this.grpOutputDieState.Padding = new System.Windows.Forms.Padding(3);
            this.grpOutputDieState.TabStop = false;
            this.grpOutputDieState.Text = "DIE STATE EDIT";
            //
            // outputDieStateLayout
            //
            this.outputDieStateLayout.ColumnCount = 1;
            this.outputDieStateLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.outputDieStateLayout.Controls.Add(this.rbSelectPickStatus, 0, 0);
            this.outputDieStateLayout.Controls.Add(this.rdoOutputStateGood, 0, 1);
            this.outputDieStateLayout.Controls.Add(this.rdoOutputStateNg, 0, 2);
            this.outputDieStateLayout.Controls.Add(this.rbDragPickStatus, 0, 3);
            this.outputDieStateLayout.Controls.Add(this.btnApplyOutputDieState, 0, 4);
            this.outputDieStateLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputDieStateLayout.Location = new System.Drawing.Point(3, 23);
            this.outputDieStateLayout.Margin = new System.Windows.Forms.Padding(0);
            this.outputDieStateLayout.Name = "outputDieStateLayout";
            this.outputDieStateLayout.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.outputDieStateLayout.RowCount = 5;
            this.outputDieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.outputDieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.outputDieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.outputDieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.outputDieStateLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.outputDieStateLayout.Size = new System.Drawing.Size(382, 190);
            this.outputDieStateLayout.TabIndex = 0;
            // btnReloadActiveMap
            //
            this.btnReloadActiveMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(238)))), ((int)(((byte)(244)))));
            this.btnReloadActiveMap.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReloadActiveMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReloadActiveMap.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(143)))), ((int)(((byte)(156)))), ((int)(((byte)(173)))));
            this.btnReloadActiveMap.FlatAppearance.BorderSize = 1;
            this.btnReloadActiveMap.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(199)))), ((int)(((byte)(210)))), ((int)(((byte)(224)))));
            this.btnReloadActiveMap.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(218)))), ((int)(((byte)(226)))), ((int)(((byte)(236)))));
            this.btnReloadActiveMap.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReloadActiveMap.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnReloadActiveMap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.btnReloadActiveMap.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.btnReloadActiveMap.TabIndex = 0;
            this.btnReloadActiveMap.Text = "RELOAD OUTPUT DIE MAP";
            this.btnReloadActiveMap.UseVisualStyleBackColor = false;
            //
            // btnPickStatusSave
            //
            this.btnPickStatusSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(238)))), ((int)(((byte)(244)))));
            this.btnPickStatusSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickStatusSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickStatusSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(143)))), ((int)(((byte)(156)))), ((int)(((byte)(173)))));
            this.btnPickStatusSave.FlatAppearance.BorderSize = 1;
            this.btnPickStatusSave.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(199)))), ((int)(((byte)(210)))), ((int)(((byte)(224)))));
            this.btnPickStatusSave.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(218)))), ((int)(((byte)(226)))), ((int)(((byte)(236)))));
            this.btnPickStatusSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPickStatusSave.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnPickStatusSave.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.btnPickStatusSave.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.btnPickStatusSave.TabIndex = 1;
            this.btnPickStatusSave.Text = "MOVE SELECTED SLOT";
            this.btnPickStatusSave.UseVisualStyleBackColor = false;
            //
            // btnApplyOutputDieState
            //
            this.btnApplyOutputDieState.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(238)))), ((int)(((byte)(244)))));
            this.btnApplyOutputDieState.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApplyOutputDieState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApplyOutputDieState.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(143)))), ((int)(((byte)(156)))), ((int)(((byte)(173)))));
            this.btnApplyOutputDieState.FlatAppearance.BorderSize = 1;
            this.btnApplyOutputDieState.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(199)))), ((int)(((byte)(210)))), ((int)(((byte)(224)))));
            this.btnApplyOutputDieState.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(218)))), ((int)(((byte)(226)))), ((int)(((byte)(236)))));
            this.btnApplyOutputDieState.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApplyOutputDieState.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnApplyOutputDieState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(38)))), ((int)(((byte)(50)))), ((int)(((byte)(66)))));
            this.btnApplyOutputDieState.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.btnApplyOutputDieState.TabIndex = 6;
            this.btnApplyOutputDieState.Text = "APPLY SELECTED STATE";
            this.btnApplyOutputDieState.UseVisualStyleBackColor = false;
            //
            // grpAction
            //
            this.grpAction.BackColor = System.Drawing.Color.White;
            this.grpAction.Controls.Add(this.actionLayout);
            this.grpAction.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpAction.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpAction.ForeColor = System.Drawing.Color.Black;
            this.grpAction.Margin = new System.Windows.Forms.Padding(3);
            this.grpAction.Padding = new System.Windows.Forms.Padding(3);
            this.grpAction.TabStop = false;
            this.grpAction.Text = "ACTION";
            this.grpAction.Size = new System.Drawing.Size(827, 126);
            //
            // actionLayout
            //
            this.actionLayout.BackColor = System.Drawing.Color.White;
            this.actionLayout.ColumnCount = 2;
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionLayout.Controls.Add(this.btnManualAlignComplete, 0, 0);
            this.actionLayout.Controls.Add(this.btnNeedleBlockDown, 1, 0);
            this.actionLayout.Controls.Add(this.btnThetaMatchMove, 0, 1);
            this.actionLayout.Controls.Add(this.btnXyMatchMove, 1, 1);
            this.actionLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.actionLayout.Location = new System.Drawing.Point(6, 24);
            this.actionLayout.Margin = new System.Windows.Forms.Padding(0);
            this.actionLayout.Name = "actionLayout";
            this.actionLayout.Padding = new System.Windows.Forms.Padding(3, 1, 3, 0);
            this.actionLayout.RowCount = 2;
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.actionLayout.Size = new System.Drawing.Size(815, 93);
            this.actionLayout.TabIndex = 0;
            //
            // action buttons
            //
            this.btnManualAlignComplete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnManualAlignComplete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnManualAlignComplete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnManualAlignComplete.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnManualAlignComplete.ForeColor = System.Drawing.Color.White;
            this.btnManualAlignComplete.Margin = new System.Windows.Forms.Padding(3);
            this.btnManualAlignComplete.TabIndex = 2;
            this.btnManualAlignComplete.Text = "GOOD PLAN INIT";
            this.btnNeedleBlockDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleBlockDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleBlockDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleBlockDown.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNeedleBlockDown.ForeColor = System.Drawing.Color.White;
            this.btnNeedleBlockDown.Margin = new System.Windows.Forms.Padding(3);
            this.btnNeedleBlockDown.TabIndex = 3;
            this.btnNeedleBlockDown.Text = "NG PLAN INIT";
            this.btnThetaMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnThetaMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnThetaMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnThetaMatchMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnThetaMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnThetaMatchMove.Margin = new System.Windows.Forms.Padding(3);
            this.btnThetaMatchMove.TabIndex = 4;
            this.btnThetaMatchMove.Text = "SAVE MATERIAL STATE";
            this.btnXyMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnXyMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnXyMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXyMatchMove.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnXyMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnXyMatchMove.Margin = new System.Windows.Forms.Padding(3);
            this.btnXyMatchMove.TabIndex = 5;
            this.btnXyMatchMove.Text = "REFRESH DISPLAY";
            //
            // lblHeader
            //
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Font = new System.Drawing.Font("Malgun Gothic", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(0, 0);
            this.lblHeader.TabIndex = 100;
            this.lblHeader.Tag = "i18n:work.page.outputMap";
            this.lblHeader.Text = "OUTPUT GOOD RECEIVE MAP";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblHeader.Visible = false;
            //
            // lblMapTitle
            //
            this.lblMapTitle.Name = "lblMapTitle";
            this.lblMapTitle.Size = new System.Drawing.Size(0, 0);
            this.lblMapTitle.TabIndex = 101;
            this.lblMapTitle.Tag = "i18n:work.page.outputMap";
            this.lblMapTitle.Text = "OUTPUT GOOD RECEIVE MAP";
            this.lblMapTitle.Visible = false;
            //
            // btnClose
            //
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(0, 0);
            this.btnClose.TabIndex = 102;
            this.btnClose.Text = "CLOSE";
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
            this.colGridX.FillWeight = 65F;
            this.colGridX.HeaderText = "맵 X";
            this.colGridX.Name = "colGridX";
            this.colGridX.ReadOnly = true;
            this.colGridX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colGridY
            //
            this.colGridY.FillWeight = 65F;
            this.colGridY.HeaderText = "맵 Y";
            this.colGridY.Name = "colGridY";
            this.colGridY.ReadOnly = true;
            this.colGridY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colEquipmentGridX
            //
            this.colEquipmentGridX.FillWeight = 55F;
            this.colEquipmentGridX.HeaderText = "Grid X";
            this.colEquipmentGridX.Name = "colEquipmentGridX";
            this.colEquipmentGridX.Visible = false;
            this.colEquipmentGridX.ReadOnly = true;
            this.colEquipmentGridX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colEquipmentGridY
            //
            this.colEquipmentGridY.FillWeight = 55F;
            this.colEquipmentGridY.HeaderText = "Grid Y";
            this.colEquipmentGridY.Name = "colEquipmentGridY";
            this.colEquipmentGridY.Visible = false;
            this.colEquipmentGridY.ReadOnly = true;
            this.colEquipmentGridY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
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
            this.colAxisX.FillWeight = 90F;
            this.colAxisX.HeaderText = "Process X(mm)";
            this.colAxisX.Name = "colAxisX";
            this.colAxisX.Visible = false;
            this.colAxisX.ReadOnly = true;
            this.colAxisX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colAxisY
            //
            this.colAxisY.FillWeight = 90F;
            this.colAxisY.HeaderText = "Process Y(mm)";
            this.colAxisY.Name = "colAxisY";
            this.colAxisY.Visible = false;
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
            this.gridDieList.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.OnGridDieListCellMouseDown);
            this.rbStandard.CheckedChanged += new System.EventHandler(this.rbStandard_CheckedChanged);
            this.rbStartIndex.CheckedChanged += new System.EventHandler(this.rbStartIndex_CheckedChanged);
            this.btnReloadActiveMap.Click += new System.EventHandler(this.btnReloadActiveMap_Click);
            this.btnPickStatusSave.Click += new System.EventHandler(this.btnPickStatusSave_Click);
            this.btnApplyOutputDieState.Click += new System.EventHandler(this.btnApplyOutputDieState_Click);
            this.btnManualAlignComplete.Click += new System.EventHandler(this.btnManualAlignComplete_Click);
            this.btnNeedleBlockDown.Click += new System.EventHandler(this.btnNeedleBlockDown_Click);
            this.btnThetaMatchMove.Click += new System.EventHandler(this.btnThetaMatchMove_Click);
            this.btnXyMatchMove.Click += new System.EventHandler(this.btnXyMatchMove_Click);
            //
            // OutputStageMapTransferPage
            //
            this.AutoScroll = false;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "OutputStageMapTransferPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.grpReceiveMap.ResumeLayout(false);
            this.mapLayout.ResumeLayout(false);
            this.grpDieGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridDieList)).EndInit();
            this.sideLayout.ResumeLayout(false);
            this.grpMapInfo.ResumeLayout(false);
            this.mapInfoLayout.ResumeLayout(false);
            this.grpMode.ResumeLayout(false);
            this.modeLayout.ResumeLayout(false);
            this.grpOutputDieState.ResumeLayout(false);
            this.outputDieStateLayout.ResumeLayout(false);
            this.grpAction.ResumeLayout(false);
            this.actionLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private void AssignStableControlNames()
        {
            this.grpReceiveMap.Name = "grpReceiveMap";
            this.grpDieGrid.Name = "grpDieGrid";
            this.grpAction.Name = "grpAction";
            this.grpMapInfo.Name = "grpMapInfo";
            this.grpMode.Name = "grpMode";
            this.grpOutputDieState.Name = "grpOutputDieState";
            this.outputDieStateLayout.Name = "outputDieStateLayout";
            this.lblProjectCaption.Name = "lblProjectCaption";
            this.lblProjectValue.Name = "lblProjectValue";
            this.lblBarcodeCaption.Name = "lblBarcodeCaption";
            this.lblBarcodeValue.Name = "lblBarcodeValue";
            this.lblBinCaption.Name = "lblBinCaption";
            this.lblBinValue.Name = "lblBinValue";
            this.lblChipWCaption.Name = "lblChipWCaption";
            this.lblChipW.Name = "lblChipW";
            this.lblChipHCaption.Name = "lblChipHCaption";
            this.lblChipH.Name = "lblChipH";
            this.lblPitchXCaption.Name = "lblPitchXCaption";
            this.lblPitchX.Name = "lblPitchX";
            this.lblPitchYCaption.Name = "lblPitchYCaption";
            this.lblPitchY.Name = "lblPitchY";
            this.lblWaferDiaCaption.Name = "lblWaferDiaCaption";
            this.lblWaferDia.Name = "lblWaferDia";
            this.lblAxisXCaption.Name = "lblAxisXCaption";
            this.lblAxisX.Name = "lblAxisX";
            this.lblAxisYCaption.Name = "lblAxisYCaption";
            this.lblAxisY.Name = "lblAxisY";
            this.lblBinRankCaption.Name = "lblBinRankCaption";
            this.lblBinRank.Name = "lblBinRank";
            this.lblDieNumCaption.Name = "lblDieNumCaption";
            this.lblDieNum.Name = "lblDieNum";
            this.rbStandard.Name = "rbStandard";
            this.rbStartIndex.Name = "rbStartIndex";
            this.rbSelectPickStatus.Name = "rbSelectPickStatus";
            this.rbDragPickStatus.Name = "rbDragPickStatus";
            this.rdoOutputStateGood.Name = "rdoOutputStateGood";
            this.rdoOutputStateNg.Name = "rdoOutputStateNg";
            this.btnReloadActiveMap.Name = "btnReloadActiveMap";
            this.btnPickStatusSave.Name = "btnPickStatusSave";
            this.btnApplyOutputDieState.Name = "btnApplyOutputDieState";
            this.btnManualAlignComplete.Name = "btnManualAlignComplete";
            this.btnNeedleBlockDown.Name = "btnNeedleBlockDown";
            this.btnThetaMatchMove.Name = "btnThetaMatchMove";
            this.btnXyMatchMove.Name = "btnXyMatchMove";
        }

        private DataGridViewTextBoxColumn colIndex;
        private DataGridViewTextBoxColumn colGridX;
        private DataGridViewTextBoxColumn colGridY;
        private DataGridViewTextBoxColumn colEquipmentGridX;
        private DataGridViewTextBoxColumn colEquipmentGridY;
        private DataGridViewTextBoxColumn colTarget;
        private DataGridViewTextBoxColumn colResult;
        private DataGridViewTextBoxColumn colBin;
        private DataGridViewTextBoxColumn colAxisX;
        private DataGridViewTextBoxColumn colAxisY;
        private DataGridViewTextBoxColumn colDieUid;
    }
}
