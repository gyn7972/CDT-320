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
        private RadioButton rbStandard;
        private RadioButton rbStartIndex;
        private RadioButton rbSelectPickStatus;
        private RadioButton rbDragPickStatus;
        private Button btnPickStatusSave;
        private Button btnReloadActiveMap;
        private TableLayoutPanel actionLayout;
        private ActionButton btnManualAlignComplete;
        private ActionButton btnNeedleBlockDown;
        private ActionButton btnThetaMatchMove;
        private ActionButton btnXyMatchMove;
        private Button btnClose;

        private static readonly Color TitleColor = Color.FromArgb(38, 50, 66);
        private static readonly Color CaptionBack = Color.FromArgb(236, 238, 241);
        private static readonly Color CaptionFore = Color.FromArgb(70, 70, 70);
        private static readonly Color PanelBack = Color.White;
        private static readonly Color ActionButtonBack = Color.FromArgb(128, 128, 128);

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
            this.btnReloadActiveMap = new System.Windows.Forms.Button();
            this.btnPickStatusSave = new System.Windows.Forms.Button();
            this.btnManualAlignComplete = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnNeedleBlockDown = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnThetaMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnXyMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
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
            this.grpReceiveMap.SuspendLayout();
            this.mapLayout.SuspendLayout();
            this.grpDieGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridDieList)).BeginInit();
            this.sideLayout.SuspendLayout();
            this.grpMapInfo.SuspendLayout();
            this.mapInfoLayout.SuspendLayout();
            this.grpMode.SuspendLayout();
            this.modeLayout.SuspendLayout();
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
            ConfigureMainGroup(this.grpReceiveMap, "OUTPUT GOOD RECEIVE MAP", 0);
            this.grpReceiveMap.Controls.Add(this.mapLayout);
            this.grpReceiveMap.Margin = new System.Windows.Forms.Padding(0, 0, 1, 1);
            this.grpReceiveMap.Location = new System.Drawing.Point(8, 8);
            this.grpReceiveMap.Size = new System.Drawing.Size(827, 442);
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
            this.mapView.Location = new System.Drawing.Point(0, 0);
            this.mapView.Margin = new System.Windows.Forms.Padding(0);
            this.mapView.Map = null;
            this.mapView.Name = "mapView";
            this.mapView.Size = new System.Drawing.Size(815, 412);
            this.mapView.TabIndex = 0;
            //
            // grpDieGrid
            //
            ConfigureMainGroup(this.grpDieGrid, "OUTPUT GOOD RECEIVE MAP DGV", 2);
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
            headerStyle.BackColor = CaptionBack;
            headerStyle.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            headerStyle.ForeColor = TitleColor;
            headerStyle.SelectionBackColor = CaptionBack;
            headerStyle.SelectionForeColor = TitleColor;
            headerStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.gridDieList.ColumnHeadersDefaultCellStyle = headerStyle;
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
            this.gridDieList.Margin = new System.Windows.Forms.Padding(0, 1, 1, 0);
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
            this.sideLayout.Margin = new System.Windows.Forms.Padding(1, 0, 0, 1);
            this.sideLayout.Name = "sideLayout";
            this.sideLayout.RowCount = 1;
            this.sideLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sideLayout.Size = new System.Drawing.Size(827, 442);
            this.sideLayout.TabIndex = 1;
            //
            // grpMapInfo
            //
            ConfigureMainGroup(this.grpMapInfo, "BIN / DIE INFO", 0);
            this.grpMapInfo.Controls.Add(this.mapInfoLayout);
            this.grpMapInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMapInfo.Margin = new System.Windows.Forms.Padding(0, 0, 1, 0);
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
            for (int i = 0; i < 12; i++)
                this.mapInfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8.333333F));
            this.mapInfoLayout.Size = new System.Drawing.Size(397, 412);
            this.mapInfoLayout.TabIndex = 0;
            //
            // info labels
            //
            ConfigureInfoCaption(this.lblProjectCaption, "Project Name", 0);
            ConfigureInfoValue(this.lblProjectValue, "--", 1);
            ConfigureInfoCaption(this.lblBarcodeCaption, "Source Wafer", 2);
            ConfigureInfoValue(this.lblBarcodeValue, "--", 3);
            ConfigureInfoCaption(this.lblBinCaption, "Side", 4);
            ConfigureInfoValue(this.lblBinValue, "--", 5);
            ConfigureInfoCaption(this.lblChipWCaption, "Grid X", 6);
            ConfigureInfoValue(this.lblChipW, "0", 7);
            ConfigureInfoCaption(this.lblChipHCaption, "Grid Y", 8);
            ConfigureInfoValue(this.lblChipH, "0", 9);
            ConfigureInfoCaption(this.lblPitchXCaption, "Pitch X", 10);
            ConfigureInfoValue(this.lblPitchX, "0", 11);
            ConfigureInfoCaption(this.lblPitchYCaption, "Pitch Y", 12);
            ConfigureInfoValue(this.lblPitchY, "0", 13);
            ConfigureInfoCaption(this.lblWaferDiaCaption, "Progress", 14);
            ConfigureInfoValue(this.lblWaferDia, "0/0", 15);
            ConfigureInfoCaption(this.lblAxisXCaption, "X (mm)", 16);
            ConfigureInfoValue(this.lblAxisX, "0", 17);
            ConfigureInfoCaption(this.lblAxisYCaption, "Y (mm)", 18);
            ConfigureInfoValue(this.lblAxisY, "0", 19);
            ConfigureInfoCaption(this.lblBinRankCaption, "Bin / State", 20);
            ConfigureInfoValue(this.lblBinRank, "0", 21);
            ConfigureInfoCaption(this.lblDieNumCaption, "Next Target", 22);
            ConfigureInfoValue(this.lblDieNum, "0/0", 23);
            //
            // grpMode
            //
            ConfigureMainGroup(this.grpMode, "OUTPUT STAGE", 1);
            this.grpMode.Controls.Add(this.modeLayout);
            this.grpMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.grpMode.Dock = System.Windows.Forms.DockStyle.None;
            this.grpMode.Margin = new System.Windows.Forms.Padding(1, 0, 0, 0);
            this.grpMode.Size = new System.Drawing.Size(410, 216);
            //
            // modeLayout
            //
            this.modeLayout.BackColor = System.Drawing.Color.White;
            this.modeLayout.ColumnCount = 2;
            this.modeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.modeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.modeLayout.Controls.Add(this.rbStandard, 0, 0);
            this.modeLayout.Controls.Add(this.rbStartIndex, 0, 1);
            this.modeLayout.Controls.Add(this.rbSelectPickStatus, 0, 2);
            this.modeLayout.Controls.Add(this.rbDragPickStatus, 0, 3);
            this.modeLayout.Controls.Add(this.btnReloadActiveMap, 0, 4);
            this.modeLayout.Controls.Add(this.btnPickStatusSave, 1, 4);
            this.modeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modeLayout.Location = new System.Drawing.Point(6, 24);
            this.modeLayout.Margin = new System.Windows.Forms.Padding(0);
            this.modeLayout.Name = "modeLayout";
            this.modeLayout.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.modeLayout.RowCount = 5;
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.modeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.modeLayout.Size = new System.Drawing.Size(398, 186);
            this.modeLayout.TabIndex = 0;
            ConfigureModeRadio(this.rbStandard, "GOOD STAGE", 0, true);
            ConfigureModeRadio(this.rbStartIndex, "NG STAGE", 1, false);
            ConfigureModeRadio(this.rbSelectPickStatus, "SOURCE ORDER", 2, false);
            ConfigureModeRadio(this.rbDragPickStatus, "RECEIVED STATUS", 3, false);
            this.modeLayout.SetColumnSpan(this.rbStandard, 2);
            this.modeLayout.SetColumnSpan(this.rbStartIndex, 2);
            this.modeLayout.SetColumnSpan(this.rbSelectPickStatus, 2);
            this.modeLayout.SetColumnSpan(this.rbDragPickStatus, 2);
            //
            // grpAction
            //
            ConfigureMainGroup(this.grpAction, "ACTION", 3);
            this.grpAction.Controls.Add(this.actionLayout);
            this.grpAction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.grpAction.Dock = System.Windows.Forms.DockStyle.None;
            this.grpAction.Margin = new System.Windows.Forms.Padding(1, 1, 0, 0);
            this.grpAction.Location = new System.Drawing.Point(843, 454);
            this.grpAction.Size = new System.Drawing.Size(827, 124);
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
            ConfigureStageCommandButton(this.btnReloadActiveMap, "RELOAD OUTPUT DIE MAP", 0);
            ConfigureStageCommandButton(this.btnPickStatusSave, "MOVE SELECTED SLOT", 1);
            ConfigureMapActionButton(this.btnManualAlignComplete, "GOOD PLAN INIT", 2);
            ConfigureMapActionButton(this.btnNeedleBlockDown, "NG PLAN INIT", 3);
            ConfigureMapActionButton(this.btnThetaMatchMove, "SAVE MATERIAL STATE", 4);
            ConfigureMapActionButton(this.btnXyMatchMove, "REFRESH DISPLAY", 5);
            AssignStableControlNames();
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
            this.modeLayout.PerformLayout();
            this.grpAction.ResumeLayout(false);
            this.actionLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private static void ConfigureMainGroup(GroupBox group, string text, int tabIndex)
        {
            group.BackColor = PanelBack;
            group.Dock = System.Windows.Forms.DockStyle.Fill;
            group.Font = new System.Drawing.Font("Malgun Gothic", 11F, System.Drawing.FontStyle.Bold);
            group.ForeColor = TitleColor;
            group.Margin = new System.Windows.Forms.Padding(0, 0, 1, 1);
            group.Padding = new System.Windows.Forms.Padding(3);
            group.TabIndex = tabIndex;
            group.TabStop = false;
            group.Text = text;
        }

        private static void ConfigureInfoCaption(Label label, string text, int tabIndex)
        {
            label.AutoEllipsis = true;
            label.BackColor = CaptionBack;
            label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            label.Dock = System.Windows.Forms.DockStyle.Fill;
            label.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            label.ForeColor = CaptionFore;
            label.Margin = new System.Windows.Forms.Padding(1);
            label.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            label.TabIndex = tabIndex;
            label.Text = text;
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        }

        private static void ConfigureInfoValue(Label label, string text, int tabIndex)
        {
            label.AutoEllipsis = true;
            label.BackColor = System.Drawing.Color.White;
            label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            label.Dock = System.Windows.Forms.DockStyle.Fill;
            label.Font = new System.Drawing.Font("Consolas", 9F);
            label.ForeColor = System.Drawing.Color.FromArgb(25, 29, 34);
            label.Margin = new System.Windows.Forms.Padding(1);
            label.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            label.TabIndex = tabIndex;
            label.Text = text;
            label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        }

        private static void ConfigureModeRadio(RadioButton radio, string text, int tabIndex, bool isChecked)
        {
            radio.AutoSize = false;
            radio.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft;
            radio.Checked = isChecked;
            radio.Dock = System.Windows.Forms.DockStyle.Fill;
            radio.Font = new System.Drawing.Font("맑은 고딕", 9F);
            radio.ForeColor = System.Drawing.Color.Black;
            radio.Margin = new System.Windows.Forms.Padding(3);
            radio.Padding = new System.Windows.Forms.Padding(0);
            radio.TabIndex = tabIndex;
            radio.TabStop = isChecked;
            radio.Text = text;
            radio.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            radio.UseVisualStyleBackColor = true;
        }

        private static void ConfigureMapActionButton(ActionButton button, string text, int tabIndex)
        {
            button.BackColor = ActionButtonBack;
            button.Cursor = System.Windows.Forms.Cursors.Hand;
            button.Dock = System.Windows.Forms.DockStyle.Fill;
            button.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Bold);
            button.ForeColor = System.Drawing.Color.White;
            button.Margin = new System.Windows.Forms.Padding(3);
            button.TabIndex = tabIndex;
            button.Text = text;
        }

        private static void ConfigureStageCommandButton(Button button, string text, int tabIndex)
        {
            button.BackColor = System.Drawing.Color.FromArgb(0xE9, 0xEE, 0xF4);
            button.Cursor = System.Windows.Forms.Cursors.Hand;
            button.Dock = System.Windows.Forms.DockStyle.Fill;
            button.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(0x8F, 0x9C, 0xAD);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0xC7, 0xD2, 0xE0);
            button.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(0xDA, 0xE2, 0xEC);
            button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            button.ForeColor = System.Drawing.Color.FromArgb(0x26, 0x32, 0x42);
            button.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            button.TabIndex = tabIndex;
            button.Text = text;
            button.UseVisualStyleBackColor = false;
        }

        private void AssignStableControlNames()
        {
            this.grpReceiveMap.Name = "grpReceiveMap";
            this.grpDieGrid.Name = "grpDieGrid";
            this.grpAction.Name = "grpAction";
            this.grpMapInfo.Name = "grpMapInfo";
            this.grpMode.Name = "grpMode";
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
            this.btnReloadActiveMap.Name = "btnReloadActiveMap";
            this.btnPickStatusSave.Name = "btnPickStatusSave";
            this.btnManualAlignComplete.Name = "btnManualAlignComplete";
            this.btnNeedleBlockDown.Name = "btnNeedleBlockDown";
            this.btnThetaMatchMove.Name = "btnThetaMatchMove";
            this.btnXyMatchMove.Name = "btnXyMatchMove";
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
