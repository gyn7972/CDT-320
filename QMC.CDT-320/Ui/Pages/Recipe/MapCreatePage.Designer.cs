namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class MapCreatePage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel contentLayout;
        private System.Windows.Forms.TableLayoutPanel mapSection;
        private System.Windows.Forms.Label lblMapTitle;
        private System.Windows.Forms.Panel mapPanel;
        private System.Windows.Forms.TableLayoutPanel mapEditorLayout;
        private System.Windows.Forms.TableLayoutPanel mapLibraryBar;
        private System.Windows.Forms.ComboBox _cbMapLibrary;
        private System.Windows.Forms.Button _btnMapLoad;
        private System.Windows.Forms.Button _btnMapNew;
        private System.Windows.Forms.Button _btnMapRename;
        private System.Windows.Forms.Button _btnMapDelete;
        private System.Windows.Forms.Panel mapViewPanel;
        private QMC.CDT320.Ui.Controls.DieMapView _mapView;
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.TableLayoutPanel settingSection;
        private System.Windows.Forms.Label lblSettingTitle;
        private System.Windows.Forms.Label lblChipCountXKey;
        private System.Windows.Forms.TextBox _tbFrameSpecName;
        private System.Windows.Forms.Label lblChipCountYKey;
        private System.Windows.Forms.NumericUpDown _nGridX;
        private System.Windows.Forms.Label lblChipPitchXKey;
        private System.Windows.Forms.NumericUpDown _nGridY;
        private System.Windows.Forms.Label lblChipPitchYKey;
        private System.Windows.Forms.NumericUpDown _nPitchX;
        private System.Windows.Forms.Label lblWaferDiameterKey;
        private System.Windows.Forms.NumericUpDown _nPitchY;
        private System.Windows.Forms.Label _lblDieSizeXKey;
        private System.Windows.Forms.NumericUpDown _nDieSizeX;
        private System.Windows.Forms.Label _lblDieSizeYKey;
        private System.Windows.Forms.NumericUpDown _nDieSizeY;
        private System.Windows.Forms.Label lblAxisXKey;
        private System.Windows.Forms.NumericUpDown _nDiameter;
        private System.Windows.Forms.Label _lblEdgeSkipModeKey;
        private System.Windows.Forms.ComboBox _cbEdgeSkipMode;
        private System.Windows.Forms.Label lblAxisYKey;
        private System.Windows.Forms.TableLayoutPanel edgeSkipPanel;
        private System.Windows.Forms.NumericUpDown _nSideEdgeSkip;
        private System.Windows.Forms.NumericUpDown _nTopBottomEdgeSkip;
        private System.Windows.Forms.TableLayoutPanel modeSection;
        private System.Windows.Forms.Label lblModeTitle;
        private System.Windows.Forms.CheckBox chkCircularMap;
        private System.Windows.Forms.RadioButton rbStandard;
        private System.Windows.Forms.RadioButton rbStartIndex;
        private System.Windows.Forms.RadioButton rbReference1;
        private System.Windows.Forms.RadioButton rbReference2;
        private System.Windows.Forms.RadioButton rbManualSelectPick;
        private System.Windows.Forms.RadioButton rbAlignCheckIndex;
        private System.Windows.Forms.RadioButton rbDragSelectPick;
        private System.Windows.Forms.Panel binSidePanel;
        private System.Windows.Forms.RadioButton rbBinGood;
        private System.Windows.Forms.RadioButton rbBinNg;
        private System.Windows.Forms.TableLayoutPanel actionSection;
        private System.Windows.Forms.Label lblActionTitle;
        private System.Windows.Forms.TextBox _tbMapApplyInfo;
        private QMC.CDT_320.Ui.Controls.ActionButton btnCreate;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnFirstDieMoveComplete;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAutoMatch;
        private QMC.CDT_320.Ui.Controls.ActionButton btnThetaMatchMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnXyMatchMove;
        private System.Windows.Forms.ToolTip _recipeLocationToolTip;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.mapSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblMapTitle = new System.Windows.Forms.Label();
            this.mapPanel = new System.Windows.Forms.Panel();
            this.mapEditorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.mapLibraryBar = new System.Windows.Forms.TableLayoutPanel();
            this._cbMapLibrary = new System.Windows.Forms.ComboBox();
            this._btnMapLoad = new System.Windows.Forms.Button();
            this.mapViewPanel = new System.Windows.Forms.Panel();
            this._mapView = new QMC.CDT320.Ui.Controls.DieMapView();
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.settingSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblSettingTitle = new System.Windows.Forms.Label();
            this.lblChipCountXKey = new System.Windows.Forms.Label();
            this._tbFrameSpecName = new System.Windows.Forms.TextBox();
            this.lblChipCountYKey = new System.Windows.Forms.Label();
            this._nGridX = new System.Windows.Forms.NumericUpDown();
            this.lblChipPitchXKey = new System.Windows.Forms.Label();
            this._nGridY = new System.Windows.Forms.NumericUpDown();
            this.lblChipPitchYKey = new System.Windows.Forms.Label();
            this._nPitchX = new System.Windows.Forms.NumericUpDown();
            this.lblWaferDiameterKey = new System.Windows.Forms.Label();
            this._nPitchY = new System.Windows.Forms.NumericUpDown();
            this._lblDieSizeXKey = new System.Windows.Forms.Label();
            this._nDieSizeX = new System.Windows.Forms.NumericUpDown();
            this._lblDieSizeYKey = new System.Windows.Forms.Label();
            this._nDieSizeY = new System.Windows.Forms.NumericUpDown();
            this.lblAxisXKey = new System.Windows.Forms.Label();
            this._nDiameter = new System.Windows.Forms.NumericUpDown();
            this._lblEdgeSkipModeKey = new System.Windows.Forms.Label();
            this._cbEdgeSkipMode = new System.Windows.Forms.ComboBox();
            this.lblAxisYKey = new System.Windows.Forms.Label();
            this.edgeSkipPanel = new System.Windows.Forms.TableLayoutPanel();
            this._nSideEdgeSkip = new System.Windows.Forms.NumericUpDown();
            this._nTopBottomEdgeSkip = new System.Windows.Forms.NumericUpDown();
            this.modeSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblModeTitle = new System.Windows.Forms.Label();
            this.chkCircularMap = new System.Windows.Forms.CheckBox();
            this.rbStandard = new System.Windows.Forms.RadioButton();
            this.rbStartIndex = new System.Windows.Forms.RadioButton();
            this.rbReference1 = new System.Windows.Forms.RadioButton();
            this.rbReference2 = new System.Windows.Forms.RadioButton();
            this.rbManualSelectPick = new System.Windows.Forms.RadioButton();
            this.rbAlignCheckIndex = new System.Windows.Forms.RadioButton();
            this.rbDragSelectPick = new System.Windows.Forms.RadioButton();
            this.binSidePanel = new System.Windows.Forms.Panel();
            this.rbBinGood = new System.Windows.Forms.RadioButton();
            this.rbBinNg = new System.Windows.Forms.RadioButton();
            this.actionSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblActionTitle = new System.Windows.Forms.Label();
            this._tbMapApplyInfo = new System.Windows.Forms.TextBox();
            this.btnCreate = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnFirstDieMoveComplete = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAutoMatch = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnThetaMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnXyMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this._recipeLocationToolTip = new System.Windows.Forms.ToolTip(this.components);
            this._btnMapNew = new System.Windows.Forms.Button();
            this._btnMapRename = new System.Windows.Forms.Button();
            this._btnMapDelete = new System.Windows.Forms.Button();
            this.mainLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.mapSection.SuspendLayout();
            this.mapPanel.SuspendLayout();
            this.mapEditorLayout.SuspendLayout();
            this.mapLibraryBar.SuspendLayout();
            this.mapViewPanel.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.settingSection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).BeginInit();
            this.edgeSkipPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nSideEdgeSkip)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTopBottomEdgeSkip)).BeginInit();
            this.modeSection.SuspendLayout();
            this.binSidePanel.SuspendLayout();
            this.actionSection.SuspendLayout();
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
            this.mainLayout.Size = new System.Drawing.Size(1678, 900);
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
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1678, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "DIE MAP CREATE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 420F));
            this.contentLayout.Controls.Add(this.mapSection, 0, 0);
            this.contentLayout.Controls.Add(this.rightLayout, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(0, 30);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(0);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1678, 870);
            this.contentLayout.TabIndex = 1;
            // 
            // mapSection
            // 
            this.mapSection.ColumnCount = 1;
            this.mapSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapSection.Controls.Add(this.lblMapTitle, 0, 0);
            this.mapSection.Controls.Add(this.mapPanel, 0, 1);
            this.mapSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapSection.Location = new System.Drawing.Point(0, 0);
            this.mapSection.Margin = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.mapSection.Name = "mapSection";
            this.mapSection.RowCount = 2;
            this.mapSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.mapSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapSection.Size = new System.Drawing.Size(1257, 870);
            this.mapSection.TabIndex = 0;
            // 
            // lblMapTitle
            // 
            this.lblMapTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblMapTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblMapTitle.ForeColor = System.Drawing.Color.White;
            this.lblMapTitle.Location = new System.Drawing.Point(0, 0);
            this.lblMapTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblMapTitle.Name = "lblMapTitle";
            this.lblMapTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblMapTitle.Size = new System.Drawing.Size(1257, 26);
            this.lblMapTitle.TabIndex = 0;
            this.lblMapTitle.Text = "DIE MAP";
            this.lblMapTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mapPanel
            // 
            this.mapPanel.BackColor = System.Drawing.Color.Black;
            this.mapPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mapPanel.Controls.Add(this.mapEditorLayout);
            this.mapPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapPanel.Location = new System.Drawing.Point(0, 27);
            this.mapPanel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.mapPanel.Name = "mapPanel";
            this.mapPanel.Size = new System.Drawing.Size(1257, 843);
            this.mapPanel.TabIndex = 1;
            // 
            // mapEditorLayout
            // 
            this.mapEditorLayout.ColumnCount = 1;
            this.mapEditorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapEditorLayout.Controls.Add(this.mapLibraryBar, 0, 0);
            this.mapEditorLayout.Controls.Add(this.mapViewPanel, 0, 1);
            this.mapEditorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapEditorLayout.Location = new System.Drawing.Point(0, 0);
            this.mapEditorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.mapEditorLayout.Name = "mapEditorLayout";
            this.mapEditorLayout.RowCount = 2;
            this.mapEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.mapEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapEditorLayout.Size = new System.Drawing.Size(1255, 841);
            this.mapEditorLayout.TabIndex = 0;
            // 
            // mapLibraryBar
            // 
            this.mapLibraryBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.mapLibraryBar.ColumnCount = 2;
            this.mapLibraryBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapLibraryBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.mapLibraryBar.Controls.Add(this._cbMapLibrary, 0, 0);
            this.mapLibraryBar.Controls.Add(this._btnMapLoad, 1, 0);
            this.mapLibraryBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapLibraryBar.Location = new System.Drawing.Point(0, 0);
            this.mapLibraryBar.Margin = new System.Windows.Forms.Padding(0);
            this.mapLibraryBar.Name = "mapLibraryBar";
            this.mapLibraryBar.Padding = new System.Windows.Forms.Padding(4);
            this.mapLibraryBar.RowCount = 1;
            this.mapLibraryBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapLibraryBar.Size = new System.Drawing.Size(1255, 34);
            this.mapLibraryBar.TabIndex = 0;
            // 
            // _cbMapLibrary
            // 
            this._cbMapLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbMapLibrary.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbMapLibrary.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbMapLibrary.FormattingEnabled = true;
            this._cbMapLibrary.Location = new System.Drawing.Point(4, 5);
            this._cbMapLibrary.Margin = new System.Windows.Forms.Padding(0, 1, 4, 1);
            this._cbMapLibrary.Name = "_cbMapLibrary";
            this._cbMapLibrary.Size = new System.Drawing.Size(1123, 23);
            this._cbMapLibrary.TabIndex = 0;
            // 
            // _btnMapLoad
            // 
            this._btnMapLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._btnMapLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMapLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMapLoad.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this._btnMapLoad.ForeColor = System.Drawing.Color.Black;
            this._btnMapLoad.Location = new System.Drawing.Point(1133, 4);
            this._btnMapLoad.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._btnMapLoad.Name = "_btnMapLoad";
            this._btnMapLoad.Size = new System.Drawing.Size(118, 26);
            this._btnMapLoad.TabIndex = 1;
            this._btnMapLoad.Text = "LOAD SPEC";
            this._btnMapLoad.UseVisualStyleBackColor = false;
            this._btnMapLoad.Click += new System.EventHandler(this._btnMapLoad_Click);
            // 
            // mapViewPanel
            // 
            this.mapViewPanel.BackColor = System.Drawing.Color.Black;
            this.mapViewPanel.Controls.Add(this._mapView);
            this.mapViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapViewPanel.Location = new System.Drawing.Point(0, 34);
            this.mapViewPanel.Margin = new System.Windows.Forms.Padding(0);
            this.mapViewPanel.Name = "mapViewPanel";
            this.mapViewPanel.Size = new System.Drawing.Size(1255, 807);
            this.mapViewPanel.TabIndex = 1;
            // 
            // _mapView
            // 
            this._mapView.BackColor = System.Drawing.Color.Black;
            this._mapView.Caption = "Recipe Die Map";
            this._mapView.CompactUsedBounds = false;
            this._mapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mapView.EnableRectangleSelection = false;
            this._mapView.Location = new System.Drawing.Point(0, 0);
            this._mapView.Map = null;
            this._mapView.Name = "_mapView";
            this._mapView.SelectedEntry = null;
            this._mapView.ShowEquipmentAxes = false;
            this._mapView.ShowWaferOutline = false;
            this._mapView.Size = new System.Drawing.Size(1255, 807);
            this._mapView.TabIndex = 0;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.settingSection, 0, 0);
            this.rightLayout.Controls.Add(this.modeSection, 0, 1);
            this.rightLayout.Controls.Add(this.actionSection, 0, 2);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(1258, 0);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 3;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 248F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 323F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(420, 870);
            this.rightLayout.TabIndex = 1;
            // 
            // settingSection
            // 
            this.settingSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.settingSection.ColumnCount = 2;
            this.settingSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.settingSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.settingSection.Controls.Add(this.lblSettingTitle, 0, 0);
            this.settingSection.Controls.Add(this.lblChipCountXKey, 0, 1);
            this.settingSection.Controls.Add(this._tbFrameSpecName, 1, 1);
            this.settingSection.Controls.Add(this.lblChipCountYKey, 0, 2);
            this.settingSection.Controls.Add(this._nGridX, 1, 2);
            this.settingSection.Controls.Add(this.lblChipPitchXKey, 0, 3);
            this.settingSection.Controls.Add(this._nGridY, 1, 3);
            this.settingSection.Controls.Add(this.lblChipPitchYKey, 0, 4);
            this.settingSection.Controls.Add(this._nPitchX, 1, 4);
            this.settingSection.Controls.Add(this.lblWaferDiameterKey, 0, 5);
            this.settingSection.Controls.Add(this._nPitchY, 1, 5);
            this.settingSection.Controls.Add(this._lblDieSizeXKey, 0, 6);
            this.settingSection.Controls.Add(this._nDieSizeX, 1, 6);
            this.settingSection.Controls.Add(this._lblDieSizeYKey, 0, 7);
            this.settingSection.Controls.Add(this._nDieSizeY, 1, 7);
            this.settingSection.Controls.Add(this.lblAxisXKey, 0, 8);
            this.settingSection.Controls.Add(this._nDiameter, 1, 8);
            this.settingSection.Controls.Add(this._lblEdgeSkipModeKey, 0, 9);
            this.settingSection.Controls.Add(this._cbEdgeSkipMode, 1, 9);
            this.settingSection.Controls.Add(this.lblAxisYKey, 0, 10);
            this.settingSection.Controls.Add(this.edgeSkipPanel, 1, 10);
            this.settingSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingSection.Location = new System.Drawing.Point(0, 0);
            this.settingSection.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.settingSection.Name = "settingSection";
            this.settingSection.Padding = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.settingSection.RowCount = 11;
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.Size = new System.Drawing.Size(420, 247);
            this.settingSection.TabIndex = 0;
            this._recipeLocationToolTip.SetToolTip(this.settingSection, "Die는 Recipe → 다이 사양, Wafer/Pitch는 Recipe → 웨이퍼 사양에서 변경합니다.");
            // 
            // lblSettingTitle
            // 
            this.lblSettingTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.settingSection.SetColumnSpan(this.lblSettingTitle, 2);
            this.lblSettingTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSettingTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblSettingTitle.ForeColor = System.Drawing.Color.White;
            this.lblSettingTitle.Location = new System.Drawing.Point(0, 0);
            this.lblSettingTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.lblSettingTitle.Name = "lblSettingTitle";
            this.lblSettingTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblSettingTitle.Size = new System.Drawing.Size(420, 25);
            this.lblSettingTitle.TabIndex = 0;
            this.lblSettingTitle.Text = "DIE MAP SETTING";
            this.lblSettingTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblChipCountXKey
            // 
            this.lblChipCountXKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipCountXKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipCountXKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipCountXKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblChipCountXKey.Location = new System.Drawing.Point(1, 27);
            this.lblChipCountXKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipCountXKey.Name = "lblChipCountXKey";
            this.lblChipCountXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipCountXKey.Size = new System.Drawing.Size(174, 28);
            this.lblChipCountXKey.TabIndex = 1;
            this.lblChipCountXKey.Text = "FRAME SPEC NAME";
            this.lblChipCountXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this.lblChipCountXKey, "Recipe → 웨이퍼 사양 → Spec name에서 설정합니다.");
            // 
            // _tbFrameSpecName
            // 
            this._tbFrameSpecName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbFrameSpecName.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbFrameSpecName.Location = new System.Drawing.Point(177, 27);
            this._tbFrameSpecName.Margin = new System.Windows.Forms.Padding(1);
            this._tbFrameSpecName.Name = "_tbFrameSpecName";
            this._tbFrameSpecName.ReadOnly = true;
            this._tbFrameSpecName.Size = new System.Drawing.Size(242, 23);
            this._tbFrameSpecName.TabIndex = 2;
            this._tbFrameSpecName.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._tbFrameSpecName, "Recipe → 웨이퍼 사양 → Spec name에서 설정합니다.");
            // 
            // lblChipCountYKey
            // 
            this.lblChipCountYKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipCountYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipCountYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipCountYKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblChipCountYKey.Location = new System.Drawing.Point(1, 57);
            this.lblChipCountYKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipCountYKey.Name = "lblChipCountYKey";
            this.lblChipCountYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipCountYKey.Size = new System.Drawing.Size(174, 28);
            this.lblChipCountYKey.TabIndex = 3;
            this.lblChipCountYKey.Text = "GRID X";
            this.lblChipCountYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this.lblChipCountYKey, "Recipe → 웨이퍼 사양 → LOAD WAFER MAP에서 결정됩니다.");
            // 
            // _nGridX
            // 
            this._nGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridX.Enabled = false;
            this._nGridX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nGridX.InterceptArrowKeys = false;
            this._nGridX.Location = new System.Drawing.Point(177, 57);
            this._nGridX.Margin = new System.Windows.Forms.Padding(1);
            this._nGridX.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._nGridX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nGridX.Name = "_nGridX";
            this._nGridX.ReadOnly = true;
            this._nGridX.Size = new System.Drawing.Size(242, 23);
            this._nGridX.TabIndex = 4;
            this._nGridX.TabStop = false;
            this._nGridX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._nGridX, "Recipe → 웨이퍼 사양 → LOAD WAFER MAP에서 결정됩니다.");
            this._nGridX.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // lblChipPitchXKey
            // 
            this.lblChipPitchXKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipPitchXKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipPitchXKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipPitchXKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblChipPitchXKey.Location = new System.Drawing.Point(1, 87);
            this.lblChipPitchXKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipPitchXKey.Name = "lblChipPitchXKey";
            this.lblChipPitchXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipPitchXKey.Size = new System.Drawing.Size(174, 28);
            this.lblChipPitchXKey.TabIndex = 5;
            this.lblChipPitchXKey.Text = "GRID Y";
            this.lblChipPitchXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this.lblChipPitchXKey, "Recipe → 웨이퍼 사양 → LOAD WAFER MAP에서 결정됩니다.");
            // 
            // _nGridY
            // 
            this._nGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridY.Enabled = false;
            this._nGridY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nGridY.InterceptArrowKeys = false;
            this._nGridY.Location = new System.Drawing.Point(177, 87);
            this._nGridY.Margin = new System.Windows.Forms.Padding(1);
            this._nGridY.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._nGridY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nGridY.Name = "_nGridY";
            this._nGridY.ReadOnly = true;
            this._nGridY.Size = new System.Drawing.Size(242, 23);
            this._nGridY.TabIndex = 6;
            this._nGridY.TabStop = false;
            this._nGridY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._nGridY, "Recipe → 웨이퍼 사양 → LOAD WAFER MAP에서 결정됩니다.");
            this._nGridY.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // lblChipPitchYKey
            // 
            this.lblChipPitchYKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipPitchYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipPitchYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipPitchYKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblChipPitchYKey.Location = new System.Drawing.Point(1, 117);
            this.lblChipPitchYKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipPitchYKey.Name = "lblChipPitchYKey";
            this.lblChipPitchYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipPitchYKey.Size = new System.Drawing.Size(174, 28);
            this.lblChipPitchYKey.TabIndex = 7;
            this.lblChipPitchYKey.Text = "PITCH GAP X";
            this.lblChipPitchYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this.lblChipPitchYKey, "Recipe → 웨이퍼 사양 → Pitch X에서 설정합니다.");
            // 
            // _nPitchX
            // 
            this._nPitchX.DecimalPlaces = 3;
            this._nPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchX.Enabled = false;
            this._nPitchX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchX.InterceptArrowKeys = false;
            this._nPitchX.Location = new System.Drawing.Point(177, 117);
            this._nPitchX.Margin = new System.Windows.Forms.Padding(1);
            this._nPitchX.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nPitchX.Name = "_nPitchX";
            this._nPitchX.ReadOnly = true;
            this._nPitchX.Size = new System.Drawing.Size(242, 23);
            this._nPitchX.TabIndex = 8;
            this._nPitchX.TabStop = false;
            this._nPitchX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._nPitchX, "Recipe → 웨이퍼 사양 → Pitch X에서 설정합니다.");
            this._nPitchX.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblWaferDiameterKey
            // 
            this.lblWaferDiameterKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblWaferDiameterKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDiameterKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDiameterKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblWaferDiameterKey.Location = new System.Drawing.Point(1, 147);
            this.lblWaferDiameterKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblWaferDiameterKey.Name = "lblWaferDiameterKey";
            this.lblWaferDiameterKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblWaferDiameterKey.Size = new System.Drawing.Size(174, 28);
            this.lblWaferDiameterKey.TabIndex = 9;
            this.lblWaferDiameterKey.Text = "PITCH GAP Y";
            this.lblWaferDiameterKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this.lblWaferDiameterKey, "Recipe → 웨이퍼 사양 → Pitch Y에서 설정합니다.");
            // 
            // _nPitchY
            // 
            this._nPitchY.DecimalPlaces = 3;
            this._nPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchY.Enabled = false;
            this._nPitchY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchY.InterceptArrowKeys = false;
            this._nPitchY.Location = new System.Drawing.Point(177, 147);
            this._nPitchY.Margin = new System.Windows.Forms.Padding(1);
            this._nPitchY.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nPitchY.Name = "_nPitchY";
            this._nPitchY.ReadOnly = true;
            this._nPitchY.Size = new System.Drawing.Size(242, 23);
            this._nPitchY.TabIndex = 10;
            this._nPitchY.TabStop = false;
            this._nPitchY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._nPitchY, "Recipe → 웨이퍼 사양 → Pitch Y에서 설정합니다.");
            this._nPitchY.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // _lblDieSizeXKey
            // 
            this._lblDieSizeXKey.BackColor = System.Drawing.Color.Gainsboro;
            this._lblDieSizeXKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblDieSizeXKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblDieSizeXKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._lblDieSizeXKey.Location = new System.Drawing.Point(1, 177);
            this._lblDieSizeXKey.Margin = new System.Windows.Forms.Padding(1);
            this._lblDieSizeXKey.Name = "_lblDieSizeXKey";
            this._lblDieSizeXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._lblDieSizeXKey.Size = new System.Drawing.Size(174, 28);
            this._lblDieSizeXKey.TabIndex = 11;
            this._lblDieSizeXKey.Text = "DIE SIZE X";
            this._lblDieSizeXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this._lblDieSizeXKey, "Recipe → 다이 사양 → Width에서 설정합니다.");
            // 
            // _nDieSizeX
            // 
            this._nDieSizeX.DecimalPlaces = 4;
            this._nDieSizeX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDieSizeX.Enabled = false;
            this._nDieSizeX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDieSizeX.InterceptArrowKeys = false;
            this._nDieSizeX.Location = new System.Drawing.Point(177, 177);
            this._nDieSizeX.Margin = new System.Windows.Forms.Padding(1);
            this._nDieSizeX.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nDieSizeX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nDieSizeX.Name = "_nDieSizeX";
            this._nDieSizeX.ReadOnly = true;
            this._nDieSizeX.Size = new System.Drawing.Size(242, 23);
            this._nDieSizeX.TabIndex = 12;
            this._nDieSizeX.TabStop = false;
            this._nDieSizeX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._nDieSizeX, "Recipe → 다이 사양 → Width에서 설정합니다.");
            this._nDieSizeX.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // _lblDieSizeYKey
            // 
            this._lblDieSizeYKey.BackColor = System.Drawing.Color.Gainsboro;
            this._lblDieSizeYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblDieSizeYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblDieSizeYKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._lblDieSizeYKey.Location = new System.Drawing.Point(1, 207);
            this._lblDieSizeYKey.Margin = new System.Windows.Forms.Padding(1);
            this._lblDieSizeYKey.Name = "_lblDieSizeYKey";
            this._lblDieSizeYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._lblDieSizeYKey.Size = new System.Drawing.Size(174, 28);
            this._lblDieSizeYKey.TabIndex = 13;
            this._lblDieSizeYKey.Text = "DIE SIZE Y";
            this._lblDieSizeYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this._lblDieSizeYKey, "Recipe → 다이 사양 → Height에서 설정합니다.");
            // 
            // _nDieSizeY
            // 
            this._nDieSizeY.DecimalPlaces = 4;
            this._nDieSizeY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDieSizeY.Enabled = false;
            this._nDieSizeY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDieSizeY.InterceptArrowKeys = false;
            this._nDieSizeY.Location = new System.Drawing.Point(177, 207);
            this._nDieSizeY.Margin = new System.Windows.Forms.Padding(1);
            this._nDieSizeY.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nDieSizeY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nDieSizeY.Name = "_nDieSizeY";
            this._nDieSizeY.ReadOnly = true;
            this._nDieSizeY.Size = new System.Drawing.Size(242, 23);
            this._nDieSizeY.TabIndex = 14;
            this._nDieSizeY.TabStop = false;
            this._nDieSizeY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._nDieSizeY, "Recipe → 다이 사양 → Height에서 설정합니다.");
            this._nDieSizeY.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblAxisXKey
            // 
            this.lblAxisXKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblAxisXKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisXKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisXKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblAxisXKey.Location = new System.Drawing.Point(1, 237);
            this.lblAxisXKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisXKey.Name = "lblAxisXKey";
            this.lblAxisXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAxisXKey.Size = new System.Drawing.Size(174, 28);
            this.lblAxisXKey.TabIndex = 11;
            this.lblAxisXKey.Text = "WAFER DIAMETER";
            this.lblAxisXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this.lblAxisXKey, "Recipe → 웨이퍼 사양 → Outer diameter에서 설정합니다.");
            // 
            // _nDiameter
            // 
            this._nDiameter.DecimalPlaces = 1;
            this._nDiameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDiameter.Enabled = false;
            this._nDiameter.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDiameter.InterceptArrowKeys = false;
            this._nDiameter.Location = new System.Drawing.Point(177, 237);
            this._nDiameter.Margin = new System.Windows.Forms.Padding(1);
            this._nDiameter.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nDiameter.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nDiameter.Name = "_nDiameter";
            this._nDiameter.ReadOnly = true;
            this._nDiameter.Size = new System.Drawing.Size(242, 23);
            this._nDiameter.TabIndex = 12;
            this._nDiameter.TabStop = false;
            this._nDiameter.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._nDiameter, "Recipe → 웨이퍼 사양 → Outer diameter에서 설정합니다.");
            this._nDiameter.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            // 
            // _lblEdgeSkipModeKey
            // 
            this._lblEdgeSkipModeKey.BackColor = System.Drawing.Color.Gainsboro;
            this._lblEdgeSkipModeKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblEdgeSkipModeKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblEdgeSkipModeKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._lblEdgeSkipModeKey.Location = new System.Drawing.Point(1, 267);
            this._lblEdgeSkipModeKey.Margin = new System.Windows.Forms.Padding(1);
            this._lblEdgeSkipModeKey.Name = "_lblEdgeSkipModeKey";
            this._lblEdgeSkipModeKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._lblEdgeSkipModeKey.Size = new System.Drawing.Size(174, 28);
            this._lblEdgeSkipModeKey.TabIndex = 17;
            this._lblEdgeSkipModeKey.Text = "EDGE SKIP MODE";
            this._lblEdgeSkipModeKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this._lblEdgeSkipModeKey, "Recipe → 웨이퍼 사양 → Edge skip mode에서 설정합니다.");
            // 
            // _cbEdgeSkipMode
            // 
            this._cbEdgeSkipMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbEdgeSkipMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbEdgeSkipMode.Enabled = false;
            this._cbEdgeSkipMode.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbEdgeSkipMode.FormattingEnabled = true;
            this._cbEdgeSkipMode.Items.AddRange(new object[] {
            "GRID COUNT",
            "MM",
            "EXTERNAL MAP"});
            this._cbEdgeSkipMode.Location = new System.Drawing.Point(177, 267);
            this._cbEdgeSkipMode.Margin = new System.Windows.Forms.Padding(1);
            this._cbEdgeSkipMode.Name = "_cbEdgeSkipMode";
            this._cbEdgeSkipMode.Size = new System.Drawing.Size(242, 23);
            this._cbEdgeSkipMode.TabIndex = 18;
            this._recipeLocationToolTip.SetToolTip(this._cbEdgeSkipMode, "Recipe → 웨이퍼 사양 → Edge skip mode에서 설정합니다.");
            // 
            // lblAxisYKey
            // 
            this.lblAxisYKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblAxisYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisYKey.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblAxisYKey.Location = new System.Drawing.Point(1, 297);
            this.lblAxisYKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisYKey.Name = "lblAxisYKey";
            this.lblAxisYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAxisYKey.Size = new System.Drawing.Size(174, 28);
            this.lblAxisYKey.TabIndex = 13;
            this.lblAxisYKey.Text = "EDGE SKIP L/R, T/B";
            this.lblAxisYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._recipeLocationToolTip.SetToolTip(this.lblAxisYKey, "Recipe → 웨이퍼 사양 → Edge skip에서 설정합니다.");
            // 
            // edgeSkipPanel
            // 
            this.edgeSkipPanel.ColumnCount = 2;
            this.edgeSkipPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.edgeSkipPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.edgeSkipPanel.Controls.Add(this._nSideEdgeSkip, 0, 0);
            this.edgeSkipPanel.Controls.Add(this._nTopBottomEdgeSkip, 1, 0);
            this.edgeSkipPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.edgeSkipPanel.Location = new System.Drawing.Point(176, 296);
            this.edgeSkipPanel.Margin = new System.Windows.Forms.Padding(0);
            this.edgeSkipPanel.Name = "edgeSkipPanel";
            this.edgeSkipPanel.RowCount = 1;
            this.edgeSkipPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.edgeSkipPanel.Size = new System.Drawing.Size(244, 30);
            this.edgeSkipPanel.TabIndex = 14;
            // 
            // _nSideEdgeSkip
            // 
            this._nSideEdgeSkip.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nSideEdgeSkip.Enabled = false;
            this._nSideEdgeSkip.Font = new System.Drawing.Font("Consolas", 10F);
            this._nSideEdgeSkip.InterceptArrowKeys = false;
            this._nSideEdgeSkip.Location = new System.Drawing.Point(1, 1);
            this._nSideEdgeSkip.Margin = new System.Windows.Forms.Padding(1);
            this._nSideEdgeSkip.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._nSideEdgeSkip.Name = "_nSideEdgeSkip";
            this._nSideEdgeSkip.ReadOnly = true;
            this._nSideEdgeSkip.Size = new System.Drawing.Size(120, 23);
            this._nSideEdgeSkip.TabIndex = 0;
            this._nSideEdgeSkip.TabStop = false;
            this._nSideEdgeSkip.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._nSideEdgeSkip, "Recipe → 웨이퍼 사양 → Edge skip에서 설정합니다.");
            // 
            // _nTopBottomEdgeSkip
            // 
            this._nTopBottomEdgeSkip.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nTopBottomEdgeSkip.Enabled = false;
            this._nTopBottomEdgeSkip.Font = new System.Drawing.Font("Consolas", 10F);
            this._nTopBottomEdgeSkip.InterceptArrowKeys = false;
            this._nTopBottomEdgeSkip.Location = new System.Drawing.Point(123, 1);
            this._nTopBottomEdgeSkip.Margin = new System.Windows.Forms.Padding(1);
            this._nTopBottomEdgeSkip.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._nTopBottomEdgeSkip.Name = "_nTopBottomEdgeSkip";
            this._nTopBottomEdgeSkip.ReadOnly = true;
            this._nTopBottomEdgeSkip.Size = new System.Drawing.Size(120, 23);
            this._nTopBottomEdgeSkip.TabIndex = 1;
            this._nTopBottomEdgeSkip.TabStop = false;
            this._nTopBottomEdgeSkip.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._recipeLocationToolTip.SetToolTip(this._nTopBottomEdgeSkip, "Recipe → 웨이퍼 사양 → Edge skip에서 설정합니다.");
            // 
            // modeSection
            // 
            this.modeSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.modeSection.ColumnCount = 1;
            this.modeSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.modeSection.Controls.Add(this.lblModeTitle, 0, 0);
            this.modeSection.Controls.Add(this.chkCircularMap, 0, 1);
            this.modeSection.Controls.Add(this.rbStandard, 0, 2);
            this.modeSection.Controls.Add(this.rbStartIndex, 0, 3);
            this.modeSection.Controls.Add(this.rbReference1, 0, 4);
            this.modeSection.Controls.Add(this.rbReference2, 0, 5);
            this.modeSection.Controls.Add(this.rbManualSelectPick, 0, 6);
            this.modeSection.Controls.Add(this.rbAlignCheckIndex, 0, 7);
            this.modeSection.Controls.Add(this.rbDragSelectPick, 0, 8);
            this.modeSection.Controls.Add(this.binSidePanel, 0, 9);
            this.modeSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modeSection.Location = new System.Drawing.Point(0, 248);
            this.modeSection.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.modeSection.Name = "modeSection";
            this.modeSection.RowCount = 10;
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.Size = new System.Drawing.Size(420, 322);
            this.modeSection.TabIndex = 1;
            // 
            // lblModeTitle
            // 
            this.lblModeTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblModeTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblModeTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblModeTitle.ForeColor = System.Drawing.Color.White;
            this.lblModeTitle.Location = new System.Drawing.Point(0, 0);
            this.lblModeTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.lblModeTitle.Name = "lblModeTitle";
            this.lblModeTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblModeTitle.Size = new System.Drawing.Size(420, 25);
            this.lblModeTitle.TabIndex = 0;
            this.lblModeTitle.Text = "MODE";
            this.lblModeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkCircularMap
            // 
            this.chkCircularMap.Checked = true;
            this.chkCircularMap.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCircularMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkCircularMap.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.chkCircularMap.Location = new System.Drawing.Point(12, 26);
            this.chkCircularMap.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.chkCircularMap.Name = "chkCircularMap";
            this.chkCircularMap.Size = new System.Drawing.Size(408, 32);
            this.chkCircularMap.TabIndex = 1;
            this.chkCircularMap.Text = "CIRCLE DIE MAP";
            this.chkCircularMap.UseVisualStyleBackColor = true;
            // 
            // rbStandard
            // 
            this.rbStandard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStandard.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbStandard.Location = new System.Drawing.Point(12, 58);
            this.rbStandard.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbStandard.Name = "rbStandard";
            this.rbStandard.Size = new System.Drawing.Size(408, 32);
            this.rbStandard.TabIndex = 2;
            this.rbStandard.TabStop = true;
            this.rbStandard.Text = "STANDARD";
            // 
            // rbStartIndex
            // 
            this.rbStartIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStartIndex.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbStartIndex.Location = new System.Drawing.Point(12, 90);
            this.rbStartIndex.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbStartIndex.Name = "rbStartIndex";
            this.rbStartIndex.Size = new System.Drawing.Size(408, 32);
            this.rbStartIndex.TabIndex = 3;
            this.rbStartIndex.Text = "START INDEX";
            // 
            // rbReference1
            // 
            this.rbReference1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbReference1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbReference1.Location = new System.Drawing.Point(12, 122);
            this.rbReference1.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbReference1.Name = "rbReference1";
            this.rbReference1.Size = new System.Drawing.Size(408, 32);
            this.rbReference1.TabIndex = 4;
            this.rbReference1.Text = "1 REFERENCE INDEX";
            // 
            // rbReference2
            // 
            this.rbReference2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbReference2.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbReference2.Location = new System.Drawing.Point(12, 154);
            this.rbReference2.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbReference2.Name = "rbReference2";
            this.rbReference2.Size = new System.Drawing.Size(408, 32);
            this.rbReference2.TabIndex = 5;
            this.rbReference2.Text = "2 REFERENCE INDEX";
            // 
            // rbManualSelectPick
            // 
            this.rbManualSelectPick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbManualSelectPick.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbManualSelectPick.Location = new System.Drawing.Point(12, 186);
            this.rbManualSelectPick.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbManualSelectPick.Name = "rbManualSelectPick";
            this.rbManualSelectPick.Size = new System.Drawing.Size(408, 32);
            this.rbManualSelectPick.TabIndex = 6;
            this.rbManualSelectPick.Text = "MANUAL SELECT PICK";
            // 
            // rbAlignCheckIndex
            // 
            this.rbAlignCheckIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAlignCheckIndex.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbAlignCheckIndex.Location = new System.Drawing.Point(12, 218);
            this.rbAlignCheckIndex.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbAlignCheckIndex.Name = "rbAlignCheckIndex";
            this.rbAlignCheckIndex.Size = new System.Drawing.Size(408, 32);
            this.rbAlignCheckIndex.TabIndex = 7;
            this.rbAlignCheckIndex.Text = "ALIGN CHECK INDEX";
            // 
            // rbDragSelectPick
            // 
            this.rbDragSelectPick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbDragSelectPick.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.rbDragSelectPick.Location = new System.Drawing.Point(12, 250);
            this.rbDragSelectPick.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbDragSelectPick.Name = "rbDragSelectPick";
            this.rbDragSelectPick.Size = new System.Drawing.Size(408, 32);
            this.rbDragSelectPick.TabIndex = 8;
            this.rbDragSelectPick.Text = "DRAG SELECT PICK";
            // 
            // binSidePanel
            // 
            this.binSidePanel.Controls.Add(this.rbBinGood);
            this.binSidePanel.Controls.Add(this.rbBinNg);
            this.binSidePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.binSidePanel.Location = new System.Drawing.Point(12, 282);
            this.binSidePanel.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.binSidePanel.Name = "binSidePanel";
            this.binSidePanel.Size = new System.Drawing.Size(408, 40);
            this.binSidePanel.TabIndex = 9;
            this.binSidePanel.Visible = false;
            // 
            // rbBinGood
            // 
            this.rbBinGood.Dock = System.Windows.Forms.DockStyle.Left;
            this.rbBinGood.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.rbBinGood.Location = new System.Drawing.Point(0, 0);
            this.rbBinGood.Name = "rbBinGood";
            this.rbBinGood.Size = new System.Drawing.Size(204, 40);
            this.rbBinGood.TabIndex = 0;
            this.rbBinGood.TabStop = true;
            this.rbBinGood.Text = "GOOD BIN MAP";
            // 
            // rbBinNg
            // 
            this.rbBinNg.Dock = System.Windows.Forms.DockStyle.Right;
            this.rbBinNg.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.rbBinNg.Location = new System.Drawing.Point(204, 0);
            this.rbBinNg.Name = "rbBinNg";
            this.rbBinNg.Size = new System.Drawing.Size(204, 40);
            this.rbBinNg.TabIndex = 1;
            this.rbBinNg.TabStop = true;
            this.rbBinNg.Text = "NG BIN MAP";
            // 
            // actionSection
            // 
            this.actionSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.actionSection.ColumnCount = 2;
            this.actionSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionSection.Controls.Add(this.lblActionTitle, 0, 0);
            this.actionSection.Controls.Add(this._tbMapApplyInfo, 0, 1);
            this.actionSection.Controls.Add(this.btnCreate, 0, 2);
            this.actionSection.Controls.Add(this.btnSave, 1, 2);
            this.actionSection.Controls.Add(this.btnFirstDieMoveComplete, 0, 3);
            this.actionSection.Controls.Add(this.btnAutoMatch, 1, 3);
            this.actionSection.Controls.Add(this.btnThetaMatchMove, 0, 4);
            this.actionSection.Controls.Add(this.btnXyMatchMove, 1, 4);
            this.actionSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionSection.Location = new System.Drawing.Point(0, 571);
            this.actionSection.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.actionSection.Name = "actionSection";
            this.actionSection.RowCount = 5;
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.actionSection.Size = new System.Drawing.Size(420, 298);
            this.actionSection.TabIndex = 2;
            // 
            // lblActionTitle
            // 
            this.lblActionTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.actionSection.SetColumnSpan(this.lblActionTitle, 2);
            this.lblActionTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActionTitle.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblActionTitle.ForeColor = System.Drawing.Color.White;
            this.lblActionTitle.Location = new System.Drawing.Point(0, 0);
            this.lblActionTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.lblActionTitle.Name = "lblActionTitle";
            this.lblActionTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblActionTitle.Size = new System.Drawing.Size(420, 25);
            this.lblActionTitle.TabIndex = 0;
            this.lblActionTitle.Text = "ACTION";
            this.lblActionTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbMapApplyInfo
            // 
            this._tbMapApplyInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(251)))));
            this._tbMapApplyInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.actionSection.SetColumnSpan(this._tbMapApplyInfo, 2);
            this._tbMapApplyInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbMapApplyInfo.Font = new System.Drawing.Font("Consolas", 8.75F);
            this._tbMapApplyInfo.Location = new System.Drawing.Point(4, 30);
            this._tbMapApplyInfo.Margin = new System.Windows.Forms.Padding(4);
            this._tbMapApplyInfo.Multiline = true;
            this._tbMapApplyInfo.Name = "_tbMapApplyInfo";
            this._tbMapApplyInfo.ReadOnly = true;
            this._tbMapApplyInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._tbMapApplyInfo.Size = new System.Drawing.Size(412, 112);
            this._tbMapApplyInfo.TabIndex = 1;
            this._tbMapApplyInfo.TabStop = false;
            this._tbMapApplyInfo.Text = "No Recipe map loaded.";
            // 
            // btnCreate
            // 
            this.btnCreate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnCreate.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnCreate.BadgeText = "ACTION";
            this.btnCreate.BorderColor = System.Drawing.Color.Empty;
            this.btnCreate.BorderWidth = 0;
            this.btnCreate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCreate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCreate.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnCreate.ForeColor = System.Drawing.Color.White;
            this.btnCreate.Location = new System.Drawing.Point(4, 150);
            this.btnCreate.Margin = new System.Windows.Forms.Padding(4);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(202, 42);
            this.btnCreate.TabIndex = 1;
            this.btnCreate.Text = "CREATE";
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSave.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnSave.BadgeText = "ACTION";
            this.btnSave.BorderColor = System.Drawing.Color.Empty;
            this.btnSave.BorderWidth = 0;
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(214, 150);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(202, 42);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "SAVE";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnFirstDieMoveComplete
            // 
            this.btnFirstDieMoveComplete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnFirstDieMoveComplete.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnFirstDieMoveComplete.BadgeText = "ACTION";
            this.btnFirstDieMoveComplete.BorderColor = System.Drawing.Color.Empty;
            this.btnFirstDieMoveComplete.BorderWidth = 0;
            this.btnFirstDieMoveComplete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFirstDieMoveComplete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFirstDieMoveComplete.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnFirstDieMoveComplete.ForeColor = System.Drawing.Color.White;
            this.btnFirstDieMoveComplete.Location = new System.Drawing.Point(4, 200);
            this.btnFirstDieMoveComplete.Margin = new System.Windows.Forms.Padding(4);
            this.btnFirstDieMoveComplete.Name = "btnFirstDieMoveComplete";
            this.btnFirstDieMoveComplete.Size = new System.Drawing.Size(202, 42);
            this.btnFirstDieMoveComplete.TabIndex = 3;
            this.btnFirstDieMoveComplete.Text = "FIRST DIE MOVE COMPLETE";
            // 
            // btnAutoMatch
            // 
            this.btnAutoMatch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAutoMatch.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnAutoMatch.BadgeText = "ACTION";
            this.btnAutoMatch.BorderColor = System.Drawing.Color.Empty;
            this.btnAutoMatch.BorderWidth = 0;
            this.btnAutoMatch.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAutoMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAutoMatch.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnAutoMatch.ForeColor = System.Drawing.Color.White;
            this.btnAutoMatch.Location = new System.Drawing.Point(214, 200);
            this.btnAutoMatch.Margin = new System.Windows.Forms.Padding(4);
            this.btnAutoMatch.Name = "btnAutoMatch";
            this.btnAutoMatch.Size = new System.Drawing.Size(202, 42);
            this.btnAutoMatch.TabIndex = 4;
            this.btnAutoMatch.Text = "AUTO MATCH";
            // 
            // btnThetaMatchMove
            // 
            this.btnThetaMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnThetaMatchMove.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnThetaMatchMove.BadgeText = "ACTION";
            this.btnThetaMatchMove.BorderColor = System.Drawing.Color.Empty;
            this.btnThetaMatchMove.BorderWidth = 0;
            this.btnThetaMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnThetaMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnThetaMatchMove.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnThetaMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnThetaMatchMove.Location = new System.Drawing.Point(4, 250);
            this.btnThetaMatchMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnThetaMatchMove.Name = "btnThetaMatchMove";
            this.btnThetaMatchMove.Size = new System.Drawing.Size(202, 44);
            this.btnThetaMatchMove.TabIndex = 5;
            this.btnThetaMatchMove.Text = "THETA MATCH MOVE";
            // 
            // btnXyMatchMove
            // 
            this.btnXyMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnXyMatchMove.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnXyMatchMove.BadgeText = "ACTION";
            this.btnXyMatchMove.BorderColor = System.Drawing.Color.Empty;
            this.btnXyMatchMove.BorderWidth = 0;
            this.btnXyMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnXyMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXyMatchMove.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnXyMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnXyMatchMove.Location = new System.Drawing.Point(214, 250);
            this.btnXyMatchMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnXyMatchMove.Name = "btnXyMatchMove";
            this.btnXyMatchMove.Size = new System.Drawing.Size(202, 44);
            this.btnXyMatchMove.TabIndex = 6;
            this.btnXyMatchMove.Text = "X/Y MATCH MOVE";
            // 
            // _btnMapNew
            // 
            this._btnMapNew.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._btnMapNew.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMapNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMapNew.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this._btnMapNew.ForeColor = System.Drawing.Color.Black;
            this._btnMapNew.Location = new System.Drawing.Point(1010, 4);
            this._btnMapNew.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._btnMapNew.Name = "_btnMapNew";
            this._btnMapNew.Size = new System.Drawing.Size(68, 26);
            this._btnMapNew.TabIndex = 2;
            this._btnMapNew.Text = "SAVE AS";
            this._btnMapNew.UseVisualStyleBackColor = false;
            // 
            // _btnMapRename
            // 
            this._btnMapRename.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._btnMapRename.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMapRename.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMapRename.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this._btnMapRename.ForeColor = System.Drawing.Color.Black;
            this._btnMapRename.Location = new System.Drawing.Point(1080, 4);
            this._btnMapRename.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._btnMapRename.Name = "_btnMapRename";
            this._btnMapRename.Size = new System.Drawing.Size(78, 26);
            this._btnMapRename.TabIndex = 3;
            this._btnMapRename.Text = "RENAME";
            this._btnMapRename.UseVisualStyleBackColor = false;
            // 
            // _btnMapDelete
            // 
            this._btnMapDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._btnMapDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMapDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMapDelete.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold);
            this._btnMapDelete.ForeColor = System.Drawing.Color.Black;
            this._btnMapDelete.Location = new System.Drawing.Point(1160, 4);
            this._btnMapDelete.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._btnMapDelete.Name = "_btnMapDelete";
            this._btnMapDelete.Size = new System.Drawing.Size(68, 26);
            this._btnMapDelete.TabIndex = 4;
            this._btnMapDelete.Text = "DELETE";
            this._btnMapDelete.UseVisualStyleBackColor = false;
            // 
            // MapCreatePage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.mainLayout);
            this.Name = "MapCreatePage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.mainLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.mapSection.ResumeLayout(false);
            this.mapPanel.ResumeLayout(false);
            this.mapEditorLayout.ResumeLayout(false);
            this.mapLibraryBar.ResumeLayout(false);
            this.mapViewPanel.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.settingSection.ResumeLayout(false);
            this.settingSection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).EndInit();
            this.edgeSkipPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nSideEdgeSkip)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTopBottomEdgeSkip)).EndInit();
            this.modeSection.ResumeLayout(false);
            this.binSidePanel.ResumeLayout(false);
            this.actionSection.ResumeLayout(false);
            this.actionSection.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}

