using System.Drawing;
using System.Windows.Forms;
using QMC.CDT320.Ui.Controls;
using QMC.CDT_320.Ui.Controls;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.Work
{
    partial class MapTransferPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel statusLayout;
        private Label lblProjectCaption;
        private Label lblProjectValue;
        private Label lblBarcodeCaption;
        private Label lblBarcodeValue;
        private Label lblBinCaption;
        private Label lblBinValue;
        private TableLayoutPanel bodyLayout;
        private TableLayoutPanel mapLayout;
        private Label lblMapTitle;
        private DieMapView mapView;
        private DataGridView gridDieList;
        private DataGridViewTextBoxColumn colIndex;
        private DataGridViewTextBoxColumn colGridX;
        private DataGridViewTextBoxColumn colGridY;
        private DataGridViewTextBoxColumn colTarget;
        private DataGridViewTextBoxColumn colResult;
        private DataGridViewTextBoxColumn colBin;
        private DataGridViewTextBoxColumn colAxisX;
        private DataGridViewTextBoxColumn colAxisY;
        private DataGridViewTextBoxColumn colDieUid;
        private TableLayoutPanel sideLayout;
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
        private RadioButton rbStandard;
        private RadioButton rbStartIndex;
        private RadioButton rbSelectPickStatus;
        private RadioButton rbDragPickStatus;
        private ActionButton btnPickStatusSave;
        private ActionButton btnReloadActiveMap;
        private TableLayoutPanel actionLayout;
        private ActionButton btnManualAlignComplete;
        private ActionButton btnNeedleBlockDown;
        private ActionButton btnThetaMatchMove;
        private ActionButton btnXyMatchMove;
        private Button btnClose;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.statusLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblProjectCaption = new System.Windows.Forms.Label();
            this.lblProjectValue = new System.Windows.Forms.Label();
            this.lblBarcodeCaption = new System.Windows.Forms.Label();
            this.lblBarcodeValue = new System.Windows.Forms.Label();
            this.lblBinCaption = new System.Windows.Forms.Label();
            this.lblBinValue = new System.Windows.Forms.Label();
            this.bodyLayout = new System.Windows.Forms.TableLayoutPanel();
            this.mapLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblMapTitle = new System.Windows.Forms.Label();
            this.mapView = new QMC.CDT320.Ui.Controls.DieMapView();
            this.gridDieList = new System.Windows.Forms.DataGridView();
            this.colIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGridX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGridY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAxisX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAxisY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDieUid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sideLayout = new System.Windows.Forms.TableLayoutPanel();
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
            this.grpMode = new System.Windows.Forms.GroupBox();
            this.modeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rbStandard = new System.Windows.Forms.RadioButton();
            this.rbStartIndex = new System.Windows.Forms.RadioButton();
            this.rbSelectPickStatus = new System.Windows.Forms.RadioButton();
            this.rbDragPickStatus = new System.Windows.Forms.RadioButton();
            this.btnReloadActiveMap = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnPickStatusSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.actionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnManualAlignComplete = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleBlockDown = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnThetaMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnXyMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.statusLayout.SuspendLayout();
            this.bodyLayout.SuspendLayout();
            this.mapLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridDieList)).BeginInit();
            this.sideLayout.SuspendLayout();
            this.grpMapInfo.SuspendLayout();
            this.mapInfoLayout.SuspendLayout();
            this.grpMode.SuspendLayout();
            this.modeLayout.SuspendLayout();
            this.actionLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.statusLayout, 0, 1);
            this.rootLayout.Controls.Add(this.bodyLayout, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1675, 897);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(3, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1669, 26);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:work.page.inputMap";
            this.lblHeader.Text = "INPUT 맵";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // statusLayout
            // 
            this.statusLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.statusLayout.ColumnCount = 8;
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.statusLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.statusLayout.Controls.Add(this.lblProjectCaption, 0, 0);
            this.statusLayout.Controls.Add(this.lblProjectValue, 1, 0);
            this.statusLayout.Controls.Add(this.lblBarcodeCaption, 2, 0);
            this.statusLayout.Controls.Add(this.lblBarcodeValue, 3, 0);
            this.statusLayout.Controls.Add(this.lblBinCaption, 4, 0);
            this.statusLayout.Controls.Add(this.lblBinValue, 5, 0);
            this.statusLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLayout.Location = new System.Drawing.Point(3, 29);
            this.statusLayout.Name = "statusLayout";
            this.statusLayout.RowCount = 1;
            this.statusLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.statusLayout.Size = new System.Drawing.Size(1669, 16);
            this.statusLayout.TabIndex = 1;
            // 
            // lblProjectCaption
            // 
            this.lblProjectCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProjectCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblProjectCaption.ForeColor = System.Drawing.Color.White;
            this.lblProjectCaption.Location = new System.Drawing.Point(3, 0);
            this.lblProjectCaption.Name = "lblProjectCaption";
            this.lblProjectCaption.Size = new System.Drawing.Size(74, 20);
            this.lblProjectCaption.TabIndex = 0;
            this.lblProjectCaption.Text = "Project Name :";
            this.lblProjectCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblProjectValue
            // 
            this.lblProjectValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProjectValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblProjectValue.ForeColor = System.Drawing.Color.White;
            this.lblProjectValue.Location = new System.Drawing.Point(83, 0);
            this.lblProjectValue.Name = "lblProjectValue";
            this.lblProjectValue.Size = new System.Drawing.Size(254, 20);
            this.lblProjectValue.TabIndex = 1;
            this.lblProjectValue.Text = "--";
            this.lblProjectValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBarcodeCaption
            // 
            this.lblBarcodeCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBarcodeCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBarcodeCaption.ForeColor = System.Drawing.Color.White;
            this.lblBarcodeCaption.Location = new System.Drawing.Point(343, 0);
            this.lblBarcodeCaption.Name = "lblBarcodeCaption";
            this.lblBarcodeCaption.Size = new System.Drawing.Size(84, 20);
            this.lblBarcodeCaption.TabIndex = 2;
            this.lblBarcodeCaption.Text = "Barcode Name :";
            this.lblBarcodeCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBarcodeValue
            // 
            this.lblBarcodeValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBarcodeValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBarcodeValue.ForeColor = System.Drawing.Color.White;
            this.lblBarcodeValue.Location = new System.Drawing.Point(433, 0);
            this.lblBarcodeValue.Name = "lblBarcodeValue";
            this.lblBarcodeValue.Size = new System.Drawing.Size(214, 20);
            this.lblBarcodeValue.TabIndex = 3;
            this.lblBarcodeValue.Text = "--";
            this.lblBarcodeValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinCaption
            // 
            this.lblBinCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinCaption.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBinCaption.ForeColor = System.Drawing.Color.White;
            this.lblBinCaption.Location = new System.Drawing.Point(653, 0);
            this.lblBinCaption.Name = "lblBinCaption";
            this.lblBinCaption.Size = new System.Drawing.Size(54, 20);
            this.lblBinCaption.TabIndex = 4;
            this.lblBinCaption.Text = "1Bin :";
            this.lblBinCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinValue
            // 
            this.lblBinValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinValue.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblBinValue.ForeColor = System.Drawing.Color.White;
            this.lblBinValue.Location = new System.Drawing.Point(713, 0);
            this.lblBinValue.Name = "lblBinValue";
            this.lblBinValue.Size = new System.Drawing.Size(114, 20);
            this.lblBinValue.TabIndex = 5;
            this.lblBinValue.Text = "--";
            this.lblBinValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // bodyLayout
            // 
            this.bodyLayout.ColumnCount = 2;
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 520F));
            this.bodyLayout.Controls.Add(this.mapLayout, 0, 0);
            this.bodyLayout.Controls.Add(this.sideLayout, 1, 0);
            this.bodyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bodyLayout.Location = new System.Drawing.Point(3, 51);
            this.bodyLayout.Name = "bodyLayout";
            this.bodyLayout.Padding = new System.Windows.Forms.Padding(6);
            this.bodyLayout.RowCount = 1;
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.Size = new System.Drawing.Size(1669, 843);
            this.bodyLayout.TabIndex = 2;
            // 
            // mapLayout
            // 
            this.mapLayout.ColumnCount = 1;
            this.mapLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapLayout.Controls.Add(this.lblMapTitle, 0, 0);
            this.mapLayout.Controls.Add(this.mapView, 0, 1);
            this.mapLayout.Controls.Add(this.gridDieList, 0, 2);
            this.mapLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapLayout.Location = new System.Drawing.Point(11, 11);
            this.mapLayout.Name = "mapLayout";
            this.mapLayout.RowCount = 3;
            this.mapLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.mapLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.mapLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.mapLayout.Size = new System.Drawing.Size(1131, 825);
            this.mapLayout.TabIndex = 0;
            // 
            // lblMapTitle
            // 
            this.lblMapTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblMapTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblMapTitle.ForeColor = System.Drawing.Color.White;
            this.lblMapTitle.Location = new System.Drawing.Point(3, 0);
            this.lblMapTitle.Name = "lblMapTitle";
            this.lblMapTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblMapTitle.Size = new System.Drawing.Size(1125, 24);
            this.lblMapTitle.TabIndex = 0;
            this.lblMapTitle.Tag = "i18n:recipe.inputMapCreate";
            this.lblMapTitle.Text = "INPUT MAP CREATE";
            this.lblMapTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mapView
            // 
            this.mapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.mapView.Caption = "Map Transfer";
            this.mapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapView.Location = new System.Drawing.Point(3, 27);
            this.mapView.Map = null;
            this.mapView.Name = "mapView";
            this.mapView.Size = new System.Drawing.Size(1125, 458);
            this.mapView.TabIndex = 1;
            // 
            // gridDieList
            // 
            this.gridDieList.AllowUserToAddRows = false;
            this.gridDieList.AllowUserToDeleteRows = false;
            this.gridDieList.AllowUserToResizeRows = false;
            this.gridDieList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridDieList.BackgroundColor = System.Drawing.Color.White;
            this.gridDieList.ColumnHeadersHeight = 22;
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
            this.gridDieList.Location = new System.Drawing.Point(3, 491);
            this.gridDieList.MultiSelect = false;
            this.gridDieList.Name = "gridDieList";
            this.gridDieList.ReadOnly = true;
            this.gridDieList.RowHeadersVisible = false;
            this.gridDieList.RowTemplate.Height = 20;
            this.gridDieList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridDieList.Size = new System.Drawing.Size(1125, 331);
            this.gridDieList.TabIndex = 2;
            // 
            // colIndex
            // 
            this.colIndex.FillWeight = 45F;
            this.colIndex.HeaderText = "No";
            this.colIndex.Name = "colIndex";
            this.colIndex.ReadOnly = true;
            this.colIndex.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colGridX
            // 
            this.colGridX.FillWeight = 55F;
            this.colGridX.HeaderText = "GridX";
            this.colGridX.Name = "colGridX";
            this.colGridX.ReadOnly = true;
            this.colGridX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colGridY
            // 
            this.colGridY.FillWeight = 55F;
            this.colGridY.HeaderText = "GridY";
            this.colGridY.Name = "colGridY";
            this.colGridY.ReadOnly = true;
            this.colGridY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colTarget
            // 
            this.colTarget.FillWeight = 65F;
            this.colTarget.HeaderText = "Target";
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
            // sideLayout
            // 
            this.sideLayout.ColumnCount = 2;
            this.sideLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 230F));
            this.sideLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sideLayout.Controls.Add(this.grpMapInfo, 0, 0);
            this.sideLayout.Controls.Add(this.grpMode, 1, 0);
            this.sideLayout.Controls.Add(this.actionLayout, 1, 1);
            this.sideLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sideLayout.Location = new System.Drawing.Point(1142, 9);
            this.sideLayout.Name = "sideLayout";
            this.sideLayout.RowCount = 3;
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sideLayout.Size = new System.Drawing.Size(520, 825);
            this.sideLayout.TabIndex = 1;
            // 
            // grpMapInfo
            // 
            this.grpMapInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpMapInfo.Controls.Add(this.mapInfoLayout);
            this.grpMapInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMapInfo.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpMapInfo.Location = new System.Drawing.Point(4, 4);
            this.grpMapInfo.Margin = new System.Windows.Forms.Padding(4);
            this.grpMapInfo.Name = "grpMapInfo";
            this.grpMapInfo.Size = new System.Drawing.Size(222, 252);
            this.grpMapInfo.TabIndex = 0;
            this.grpMapInfo.TabStop = false;
            this.grpMapInfo.Text = "MAP INFO";
            // 
            // mapInfoLayout
            // 
            this.mapInfoLayout.ColumnCount = 2;
            this.mapInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.mapInfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52F));
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
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.mapInfoLayout.Size = new System.Drawing.Size(216, 226);
            this.mapInfoLayout.TabIndex = 0;
            // 
            // lblChipWCaption
            // 
            this.lblChipWCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblChipWCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipWCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipWCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblChipWCaption.Location = new System.Drawing.Point(7, 8);
            this.lblChipWCaption.Name = "lblChipWCaption";
            this.lblChipWCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblChipWCaption.Size = new System.Drawing.Size(93, 24);
            this.lblChipWCaption.TabIndex = 0;
            this.lblChipWCaption.Text = "Chip Width";
            this.lblChipWCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblChipW
            // 
            this.lblChipW.BackColor = System.Drawing.Color.White;
            this.lblChipW.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipW.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipW.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblChipW.Location = new System.Drawing.Point(106, 8);
            this.lblChipW.Name = "lblChipW";
            this.lblChipW.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblChipW.Size = new System.Drawing.Size(103, 24);
            this.lblChipW.TabIndex = 1;
            this.lblChipW.Text = "0";
            this.lblChipW.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblChipHCaption
            // 
            this.lblChipHCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblChipHCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipHCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipHCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblChipHCaption.Location = new System.Drawing.Point(7, 32);
            this.lblChipHCaption.Name = "lblChipHCaption";
            this.lblChipHCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblChipHCaption.Size = new System.Drawing.Size(93, 24);
            this.lblChipHCaption.TabIndex = 2;
            this.lblChipHCaption.Text = "Chip Height";
            this.lblChipHCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblChipH
            // 
            this.lblChipH.BackColor = System.Drawing.Color.White;
            this.lblChipH.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipH.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipH.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblChipH.Location = new System.Drawing.Point(106, 32);
            this.lblChipH.Name = "lblChipH";
            this.lblChipH.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblChipH.Size = new System.Drawing.Size(103, 24);
            this.lblChipH.TabIndex = 3;
            this.lblChipH.Text = "0";
            this.lblChipH.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPitchXCaption
            // 
            this.lblPitchXCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblPitchXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchXCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPitchXCaption.Location = new System.Drawing.Point(7, 56);
            this.lblPitchXCaption.Name = "lblPitchXCaption";
            this.lblPitchXCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPitchXCaption.Size = new System.Drawing.Size(93, 24);
            this.lblPitchXCaption.TabIndex = 4;
            this.lblPitchXCaption.Text = "Pitch X";
            this.lblPitchXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPitchX
            // 
            this.lblPitchX.BackColor = System.Drawing.Color.White;
            this.lblPitchX.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchX.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblPitchX.Location = new System.Drawing.Point(106, 56);
            this.lblPitchX.Name = "lblPitchX";
            this.lblPitchX.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblPitchX.Size = new System.Drawing.Size(103, 24);
            this.lblPitchX.TabIndex = 5;
            this.lblPitchX.Text = "0";
            this.lblPitchX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPitchYCaption
            // 
            this.lblPitchYCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblPitchYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchYCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPitchYCaption.Location = new System.Drawing.Point(7, 80);
            this.lblPitchYCaption.Name = "lblPitchYCaption";
            this.lblPitchYCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPitchYCaption.Size = new System.Drawing.Size(93, 24);
            this.lblPitchYCaption.TabIndex = 6;
            this.lblPitchYCaption.Text = "Pitch Y";
            this.lblPitchYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPitchY
            // 
            this.lblPitchY.BackColor = System.Drawing.Color.White;
            this.lblPitchY.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchY.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblPitchY.Location = new System.Drawing.Point(106, 80);
            this.lblPitchY.Name = "lblPitchY";
            this.lblPitchY.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblPitchY.Size = new System.Drawing.Size(103, 24);
            this.lblPitchY.TabIndex = 7;
            this.lblPitchY.Text = "0";
            this.lblPitchY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblWaferDiaCaption
            // 
            this.lblWaferDiaCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblWaferDiaCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDiaCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDiaCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblWaferDiaCaption.Location = new System.Drawing.Point(7, 104);
            this.lblWaferDiaCaption.Name = "lblWaferDiaCaption";
            this.lblWaferDiaCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblWaferDiaCaption.Size = new System.Drawing.Size(93, 24);
            this.lblWaferDiaCaption.TabIndex = 8;
            this.lblWaferDiaCaption.Text = "Wafer Dia";
            this.lblWaferDiaCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaferDia
            // 
            this.lblWaferDia.BackColor = System.Drawing.Color.White;
            this.lblWaferDia.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDia.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDia.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblWaferDia.Location = new System.Drawing.Point(106, 104);
            this.lblWaferDia.Name = "lblWaferDia";
            this.lblWaferDia.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblWaferDia.Size = new System.Drawing.Size(103, 24);
            this.lblWaferDia.TabIndex = 9;
            this.lblWaferDia.Text = "0";
            this.lblWaferDia.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblAxisXCaption
            // 
            this.lblAxisXCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblAxisXCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisXCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisXCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblAxisXCaption.Location = new System.Drawing.Point(7, 128);
            this.lblAxisXCaption.Name = "lblAxisXCaption";
            this.lblAxisXCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxisXCaption.Size = new System.Drawing.Size(93, 24);
            this.lblAxisXCaption.TabIndex = 10;
            this.lblAxisXCaption.Text = "Axis X";
            this.lblAxisXCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxisX
            // 
            this.lblAxisX.BackColor = System.Drawing.Color.White;
            this.lblAxisX.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisX.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblAxisX.Location = new System.Drawing.Point(106, 128);
            this.lblAxisX.Name = "lblAxisX";
            this.lblAxisX.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxisX.Size = new System.Drawing.Size(103, 24);
            this.lblAxisX.TabIndex = 11;
            this.lblAxisX.Text = "0";
            this.lblAxisX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblAxisYCaption
            // 
            this.lblAxisYCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblAxisYCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisYCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisYCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblAxisYCaption.Location = new System.Drawing.Point(7, 152);
            this.lblAxisYCaption.Name = "lblAxisYCaption";
            this.lblAxisYCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblAxisYCaption.Size = new System.Drawing.Size(93, 24);
            this.lblAxisYCaption.TabIndex = 12;
            this.lblAxisYCaption.Text = "Axis Y";
            this.lblAxisYCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAxisY
            // 
            this.lblAxisY.BackColor = System.Drawing.Color.White;
            this.lblAxisY.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisY.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblAxisY.Location = new System.Drawing.Point(106, 152);
            this.lblAxisY.Name = "lblAxisY";
            this.lblAxisY.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblAxisY.Size = new System.Drawing.Size(103, 24);
            this.lblAxisY.TabIndex = 13;
            this.lblAxisY.Text = "0";
            this.lblAxisY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblBinRankCaption
            // 
            this.lblBinRankCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblBinRankCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBinRankCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinRankCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblBinRankCaption.Location = new System.Drawing.Point(7, 176);
            this.lblBinRankCaption.Name = "lblBinRankCaption";
            this.lblBinRankCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBinRankCaption.Size = new System.Drawing.Size(93, 24);
            this.lblBinRankCaption.TabIndex = 14;
            this.lblBinRankCaption.Text = "BIN RANK";
            this.lblBinRankCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinRank
            // 
            this.lblBinRank.BackColor = System.Drawing.Color.White;
            this.lblBinRank.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBinRank.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinRank.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblBinRank.Location = new System.Drawing.Point(106, 176);
            this.lblBinRank.Name = "lblBinRank";
            this.lblBinRank.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblBinRank.Size = new System.Drawing.Size(103, 24);
            this.lblBinRank.TabIndex = 15;
            this.lblBinRank.Text = "0";
            this.lblBinRank.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblDieNumCaption
            // 
            this.lblDieNumCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblDieNumCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieNumCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieNumCaption.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDieNumCaption.Location = new System.Drawing.Point(7, 200);
            this.lblDieNumCaption.Name = "lblDieNumCaption";
            this.lblDieNumCaption.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDieNumCaption.Size = new System.Drawing.Size(93, 48);
            this.lblDieNumCaption.TabIndex = 16;
            this.lblDieNumCaption.Text = "Die Number";
            this.lblDieNumCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDieNum
            // 
            this.lblDieNum.BackColor = System.Drawing.Color.White;
            this.lblDieNum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDieNum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieNum.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblDieNum.Location = new System.Drawing.Point(106, 200);
            this.lblDieNum.Name = "lblDieNum";
            this.lblDieNum.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblDieNum.Size = new System.Drawing.Size(103, 48);
            this.lblDieNum.TabIndex = 17;
            this.lblDieNum.Text = "0/0";
            this.lblDieNum.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // grpMode
            // 
            this.grpMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpMode.Controls.Add(this.modeLayout);
            this.grpMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMode.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpMode.Location = new System.Drawing.Point(234, 4);
            this.grpMode.Margin = new System.Windows.Forms.Padding(4);
            this.grpMode.Name = "grpMode";
            this.grpMode.Size = new System.Drawing.Size(286, 252);
            this.grpMode.TabIndex = 1;
            this.grpMode.TabStop = false;
            this.grpMode.Text = "MODE";
            // 
            // modeLayout
            // 
            this.modeLayout.ColumnCount = 1;
            this.modeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.modeLayout.Controls.Add(this.rbStandard, 0, 0);
            this.modeLayout.Controls.Add(this.rbStartIndex, 0, 1);
            this.modeLayout.Controls.Add(this.rbSelectPickStatus, 0, 2);
            this.modeLayout.Controls.Add(this.rbDragPickStatus, 0, 3);
            this.modeLayout.Controls.Add(this.btnReloadActiveMap, 0, 4);
            this.modeLayout.Controls.Add(this.btnPickStatusSave, 0, 5);
            this.modeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modeLayout.Location = new System.Drawing.Point(3, 23);
            this.modeLayout.Name = "modeLayout";
            this.modeLayout.Padding = new System.Windows.Forms.Padding(10, 8, 10, 4);
            this.modeLayout.RowCount = 6;
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.modeLayout.Size = new System.Drawing.Size(280, 226);
            this.modeLayout.TabIndex = 0;
            // 
            // rbStandard
            // 
            this.rbStandard.AutoSize = true;
            this.rbStandard.Checked = true;
            this.rbStandard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStandard.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbStandard.Location = new System.Drawing.Point(13, 13);
            this.rbStandard.Name = "rbStandard";
            this.rbStandard.Size = new System.Drawing.Size(244, 21);
            this.rbStandard.TabIndex = 0;
            this.rbStandard.TabStop = true;
            this.rbStandard.Text = "STANDARD";
            this.rbStandard.UseVisualStyleBackColor = true;
            // 
            // rbStartIndex
            // 
            this.rbStartIndex.AutoSize = true;
            this.rbStartIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStartIndex.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbStartIndex.Location = new System.Drawing.Point(13, 40);
            this.rbStartIndex.Name = "rbStartIndex";
            this.rbStartIndex.Size = new System.Drawing.Size(244, 21);
            this.rbStartIndex.TabIndex = 1;
            this.rbStartIndex.Text = "START INDEX";
            // 
            // rbSelectPickStatus
            // 
            this.rbSelectPickStatus.AutoSize = true;
            this.rbSelectPickStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbSelectPickStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbSelectPickStatus.Location = new System.Drawing.Point(13, 67);
            this.rbSelectPickStatus.Name = "rbSelectPickStatus";
            this.rbSelectPickStatus.Size = new System.Drawing.Size(244, 21);
            this.rbSelectPickStatus.TabIndex = 2;
            this.rbSelectPickStatus.Text = "SELECT PICK STATUS";
            this.rbSelectPickStatus.UseVisualStyleBackColor = true;
            // 
            // rbDragPickStatus
            // 
            this.rbDragPickStatus.AutoSize = true;
            this.rbDragPickStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbDragPickStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbDragPickStatus.Location = new System.Drawing.Point(13, 94);
            this.rbDragPickStatus.Name = "rbDragPickStatus";
            this.rbDragPickStatus.Size = new System.Drawing.Size(244, 21);
            this.rbDragPickStatus.TabIndex = 3;
            this.rbDragPickStatus.Text = "DRAG PICK STATUS";
            // 
            // btnReloadActiveMap
            // 
            this.btnReloadActiveMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnReloadActiveMap.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReloadActiveMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReloadActiveMap.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnReloadActiveMap.ForeColor = System.Drawing.Color.White;
            this.btnReloadActiveMap.Location = new System.Drawing.Point(14, 122);
            this.btnReloadActiveMap.Margin = new System.Windows.Forms.Padding(4);
            this.btnReloadActiveMap.Name = "btnReloadActiveMap";
            this.btnReloadActiveMap.Size = new System.Drawing.Size(242, 32);
            this.btnReloadActiveMap.TabIndex = 5;
            this.btnReloadActiveMap.Text = "RELOAD ACTIVE MAP";
            // 
            // btnPickStatusSave
            // 
            this.btnPickStatusSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPickStatusSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPickStatusSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickStatusSave.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnPickStatusSave.ForeColor = System.Drawing.Color.White;
            this.btnPickStatusSave.Location = new System.Drawing.Point(14, 162);
            this.btnPickStatusSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnPickStatusSave.Name = "btnPickStatusSave";
            this.btnPickStatusSave.Size = new System.Drawing.Size(242, 80);
            this.btnPickStatusSave.TabIndex = 4;
            this.btnPickStatusSave.Text = "SELECT PICK STATUS SAVE";
            // 
            // actionLayout
            // 
            this.actionLayout.ColumnCount = 1;
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionLayout.Controls.Add(this.btnManualAlignComplete, 0, 0);
            this.actionLayout.Controls.Add(this.btnNeedleBlockDown, 0, 1);
            this.actionLayout.Controls.Add(this.btnThetaMatchMove, 0, 2);
            this.actionLayout.Controls.Add(this.btnXyMatchMove, 0, 3);
            this.actionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionLayout.Location = new System.Drawing.Point(233, 263);
            this.actionLayout.Name = "actionLayout";
            this.actionLayout.Padding = new System.Windows.Forms.Padding(4, 6, 4, 4);
            this.actionLayout.RowCount = 4;
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.actionLayout.Size = new System.Drawing.Size(284, 184);
            this.actionLayout.TabIndex = 3;
            // 
            // btnManualAlignComplete
            // 
            this.btnManualAlignComplete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnManualAlignComplete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnManualAlignComplete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnManualAlignComplete.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnManualAlignComplete.ForeColor = System.Drawing.Color.White;
            this.btnManualAlignComplete.Location = new System.Drawing.Point(8, 10);
            this.btnManualAlignComplete.Margin = new System.Windows.Forms.Padding(4);
            this.btnManualAlignComplete.Name = "btnManualAlignComplete";
            this.btnManualAlignComplete.Size = new System.Drawing.Size(262, 41);
            this.btnManualAlignComplete.TabIndex = 0;
            this.btnManualAlignComplete.Text = "MANUAL ALIGN COMPLETE";
            // 
            // btnNeedleBlockDown
            // 
            this.btnNeedleBlockDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnNeedleBlockDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNeedleBlockDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNeedleBlockDown.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnNeedleBlockDown.ForeColor = System.Drawing.Color.White;
            this.btnNeedleBlockDown.Location = new System.Drawing.Point(8, 59);
            this.btnNeedleBlockDown.Margin = new System.Windows.Forms.Padding(4);
            this.btnNeedleBlockDown.Name = "btnNeedleBlockDown";
            this.btnNeedleBlockDown.Size = new System.Drawing.Size(262, 41);
            this.btnNeedleBlockDown.TabIndex = 1;
            this.btnNeedleBlockDown.Text = "NEEDLE BLOCK DOWN";
            // 
            // btnThetaMatchMove
            // 
            this.btnThetaMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnThetaMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnThetaMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnThetaMatchMove.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnThetaMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnThetaMatchMove.Location = new System.Drawing.Point(8, 108);
            this.btnThetaMatchMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnThetaMatchMove.Name = "btnThetaMatchMove";
            this.btnThetaMatchMove.Size = new System.Drawing.Size(262, 41);
            this.btnThetaMatchMove.TabIndex = 2;
            this.btnThetaMatchMove.Text = "THETA MATCH MOVE";
            // 
            // btnXyMatchMove
            // 
            this.btnXyMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnXyMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnXyMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXyMatchMove.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnXyMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnXyMatchMove.Location = new System.Drawing.Point(8, 157);
            this.btnXyMatchMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnXyMatchMove.Name = "btnXyMatchMove";
            this.btnXyMatchMove.Size = new System.Drawing.Size(262, 43);
            this.btnXyMatchMove.TabIndex = 3;
            this.btnXyMatchMove.Text = "X_Y_MATCH MOVE";
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnClose.Location = new System.Drawing.Point(4, 208);
            this.btnClose.Margin = new System.Windows.Forms.Padding(4);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(279, 44);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = false;
            // 
            // MapTransferPage
            // 
            this.AutoScroll = false;
            this.Controls.Add(this.rootLayout);
            this.Name = "MapTransferPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.statusLayout.ResumeLayout(false);
            this.bodyLayout.ResumeLayout(false);
            this.mapLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridDieList)).EndInit();
            this.sideLayout.ResumeLayout(false);
            this.grpMapInfo.ResumeLayout(false);
            this.mapInfoLayout.ResumeLayout(false);
            this.grpMode.ResumeLayout(false);
            this.modeLayout.ResumeLayout(false);
            this.modeLayout.PerformLayout();
            this.actionLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

