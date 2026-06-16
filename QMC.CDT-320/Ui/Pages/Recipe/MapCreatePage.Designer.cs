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
        private System.Windows.Forms.Label lblAxisXKey;
        private System.Windows.Forms.NumericUpDown _nDiameter;
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
        private System.Windows.Forms.TableLayoutPanel actionSection;
        private System.Windows.Forms.Label lblActionTitle;
        private QMC.CDT_320.Ui.Controls.ActionButton btnCreate;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSave;
        private QMC.CDT_320.Ui.Controls.ActionButton btnFirstDieMoveComplete;
        private QMC.CDT_320.Ui.Controls.ActionButton btnAutoMatch;
        private QMC.CDT_320.Ui.Controls.ActionButton btnThetaMatchMove;
        private QMC.CDT_320.Ui.Controls.ActionButton btnXyMatchMove;

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
            this.mapSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblMapTitle = new System.Windows.Forms.Label();
            this.mapPanel = new System.Windows.Forms.Panel();
            this.mapEditorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.mapLibraryBar = new System.Windows.Forms.TableLayoutPanel();
            this._cbMapLibrary = new System.Windows.Forms.ComboBox();
            this._btnMapLoad = new System.Windows.Forms.Button();
            this._btnMapNew = new System.Windows.Forms.Button();
            this._btnMapRename = new System.Windows.Forms.Button();
            this._btnMapDelete = new System.Windows.Forms.Button();
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
            this.lblAxisXKey = new System.Windows.Forms.Label();
            this._nDiameter = new System.Windows.Forms.NumericUpDown();
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
            this.actionSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblActionTitle = new System.Windows.Forms.Label();
            this.btnCreate = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSave = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnFirstDieMoveComplete = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnAutoMatch = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnThetaMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnXyMatchMove = new QMC.CDT_320.Ui.Controls.ActionButton();
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
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).BeginInit();
            this.edgeSkipPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nSideEdgeSkip)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTopBottomEdgeSkip)).BeginInit();
            this.modeSection.SuspendLayout();
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
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
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
            this.contentLayout.Padding = new System.Windows.Forms.Padding(8);
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
            this.mapSection.Location = new System.Drawing.Point(8, 8);
            this.mapSection.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.mapSection.Name = "mapSection";
            this.mapSection.RowCount = 2;
            this.mapSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.mapSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapSection.Size = new System.Drawing.Size(1234, 854);
            this.mapSection.TabIndex = 0;
            // 
            // lblMapTitle
            // 
            this.lblMapTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblMapTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblMapTitle.ForeColor = System.Drawing.Color.White;
            this.lblMapTitle.Location = new System.Drawing.Point(0, 0);
            this.lblMapTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblMapTitle.Name = "lblMapTitle";
            this.lblMapTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblMapTitle.Size = new System.Drawing.Size(1234, 26);
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
            this.mapPanel.Location = new System.Drawing.Point(0, 30);
            this.mapPanel.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.mapPanel.Name = "mapPanel";
            this.mapPanel.Size = new System.Drawing.Size(1234, 824);
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
            this.mapEditorLayout.Size = new System.Drawing.Size(1232, 822);
            this.mapEditorLayout.TabIndex = 0;
            // 
            // mapLibraryBar
            // 
            this.mapLibraryBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.mapLibraryBar.ColumnCount = 5;
            this.mapLibraryBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapLibraryBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.mapLibraryBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.mapLibraryBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.mapLibraryBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.mapLibraryBar.Controls.Add(this._cbMapLibrary, 0, 0);
            this.mapLibraryBar.Controls.Add(this._btnMapLoad, 1, 0);
            this.mapLibraryBar.Controls.Add(this._btnMapNew, 2, 0);
            this.mapLibraryBar.Controls.Add(this._btnMapRename, 3, 0);
            this.mapLibraryBar.Controls.Add(this._btnMapDelete, 4, 0);
            this.mapLibraryBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapLibraryBar.Location = new System.Drawing.Point(0, 0);
            this.mapLibraryBar.Margin = new System.Windows.Forms.Padding(0);
            this.mapLibraryBar.Name = "mapLibraryBar";
            this.mapLibraryBar.Padding = new System.Windows.Forms.Padding(4);
            this.mapLibraryBar.RowCount = 1;
            this.mapLibraryBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mapLibraryBar.Size = new System.Drawing.Size(1232, 34);
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
            this._cbMapLibrary.Size = new System.Drawing.Size(930, 23);
            this._cbMapLibrary.TabIndex = 0;
            // 
            // _btnMapLoad
            // 
            this._btnMapLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._btnMapLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMapLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMapLoad.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this._btnMapLoad.ForeColor = System.Drawing.Color.Black;
            this._btnMapLoad.Location = new System.Drawing.Point(940, 4);
            this._btnMapLoad.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._btnMapLoad.Name = "_btnMapLoad";
            this._btnMapLoad.Size = new System.Drawing.Size(68, 26);
            this._btnMapLoad.TabIndex = 1;
            this._btnMapLoad.Text = "LOAD";
            this._btnMapLoad.UseVisualStyleBackColor = false;
            // 
            // _btnMapNew
            // 
            this._btnMapNew.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._btnMapNew.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMapNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMapNew.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this._btnMapNew.ForeColor = System.Drawing.Color.Black;
            this._btnMapNew.Location = new System.Drawing.Point(1010, 4);
            this._btnMapNew.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._btnMapNew.Name = "_btnMapNew";
            this._btnMapNew.Size = new System.Drawing.Size(68, 26);
            this._btnMapNew.TabIndex = 2;
            this._btnMapNew.Text = "NEW";
            this._btnMapNew.UseVisualStyleBackColor = false;
            // 
            // _btnMapRename
            // 
            this._btnMapRename.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._btnMapRename.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnMapRename.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMapRename.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
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
            this._btnMapDelete.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this._btnMapDelete.ForeColor = System.Drawing.Color.Black;
            this._btnMapDelete.Location = new System.Drawing.Point(1160, 4);
            this._btnMapDelete.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this._btnMapDelete.Name = "_btnMapDelete";
            this._btnMapDelete.Size = new System.Drawing.Size(68, 26);
            this._btnMapDelete.TabIndex = 4;
            this._btnMapDelete.Text = "DELETE";
            this._btnMapDelete.UseVisualStyleBackColor = false;
            // 
            // mapViewPanel
            // 
            this.mapViewPanel.BackColor = System.Drawing.Color.Black;
            this.mapViewPanel.Controls.Add(this._mapView);
            this.mapViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapViewPanel.Location = new System.Drawing.Point(0, 34);
            this.mapViewPanel.Margin = new System.Windows.Forms.Padding(0);
            this.mapViewPanel.Name = "mapViewPanel";
            this.mapViewPanel.Size = new System.Drawing.Size(1232, 788);
            this.mapViewPanel.TabIndex = 1;
            // 
            // _mapView
            // 
            this._mapView.BackColor = System.Drawing.Color.Black;
            this._mapView.Caption = "Recipe Die Map";
            this._mapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mapView.Location = new System.Drawing.Point(0, 0);
            this._mapView.Map = null;
            this._mapView.Name = "_mapView";
            this._mapView.Size = new System.Drawing.Size(1232, 788);
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
            this.rightLayout.Location = new System.Drawing.Point(1250, 8);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 3;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 248F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 323F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Size = new System.Drawing.Size(420, 854);
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
            this.settingSection.Controls.Add(this.lblAxisXKey, 0, 6);
            this.settingSection.Controls.Add(this._nDiameter, 1, 6);
            this.settingSection.Controls.Add(this.lblAxisYKey, 0, 7);
            this.settingSection.Controls.Add(this.edgeSkipPanel, 1, 7);
            this.settingSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingSection.Location = new System.Drawing.Point(0, 0);
            this.settingSection.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.settingSection.Name = "settingSection";
            this.settingSection.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.settingSection.RowCount = 8;
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.settingSection.Size = new System.Drawing.Size(420, 240);
            this.settingSection.TabIndex = 0;
            // 
            // lblSettingTitle
            // 
            this.lblSettingTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.settingSection.SetColumnSpan(this.lblSettingTitle, 2);
            this.lblSettingTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSettingTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSettingTitle.ForeColor = System.Drawing.Color.White;
            this.lblSettingTitle.Location = new System.Drawing.Point(0, 0);
            this.lblSettingTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblSettingTitle.Name = "lblSettingTitle";
            this.lblSettingTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblSettingTitle.Size = new System.Drawing.Size(420, 22);
            this.lblSettingTitle.TabIndex = 0;
            this.lblSettingTitle.Text = "DIE MAP SETTING";
            this.lblSettingTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblChipCountXKey
            // 
            this.lblChipCountXKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipCountXKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipCountXKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipCountXKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblChipCountXKey.Location = new System.Drawing.Point(1, 27);
            this.lblChipCountXKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipCountXKey.Name = "lblChipCountXKey";
            this.lblChipCountXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipCountXKey.Size = new System.Drawing.Size(174, 28);
            this.lblChipCountXKey.TabIndex = 1;
            this.lblChipCountXKey.Text = "FRAME SPEC NAME";
            this.lblChipCountXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbFrameSpecName
            // 
            this._tbFrameSpecName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbFrameSpecName.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbFrameSpecName.Location = new System.Drawing.Point(177, 27);
            this._tbFrameSpecName.Margin = new System.Windows.Forms.Padding(1);
            this._tbFrameSpecName.Name = "_tbFrameSpecName";
            this._tbFrameSpecName.Size = new System.Drawing.Size(242, 23);
            this._tbFrameSpecName.TabIndex = 2;
            this._tbFrameSpecName.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // lblChipCountYKey
            // 
            this.lblChipCountYKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipCountYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipCountYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipCountYKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblChipCountYKey.Location = new System.Drawing.Point(1, 57);
            this.lblChipCountYKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipCountYKey.Name = "lblChipCountYKey";
            this.lblChipCountYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipCountYKey.Size = new System.Drawing.Size(174, 28);
            this.lblChipCountYKey.TabIndex = 3;
            this.lblChipCountYKey.Text = "GRID X";
            this.lblChipCountYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nGridX
            // 
            this._nGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridX.Font = new System.Drawing.Font("Consolas", 10F);
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
            this._nGridX.Size = new System.Drawing.Size(242, 23);
            this._nGridX.TabIndex = 4;
            this._nGridX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.lblChipPitchXKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblChipPitchXKey.Location = new System.Drawing.Point(1, 87);
            this.lblChipPitchXKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipPitchXKey.Name = "lblChipPitchXKey";
            this.lblChipPitchXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipPitchXKey.Size = new System.Drawing.Size(174, 28);
            this.lblChipPitchXKey.TabIndex = 5;
            this.lblChipPitchXKey.Text = "GRID Y";
            this.lblChipPitchXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nGridY
            // 
            this._nGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridY.Font = new System.Drawing.Font("Consolas", 10F);
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
            this._nGridY.Size = new System.Drawing.Size(242, 23);
            this._nGridY.TabIndex = 6;
            this._nGridY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.lblChipPitchYKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblChipPitchYKey.Location = new System.Drawing.Point(1, 117);
            this.lblChipPitchYKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipPitchYKey.Name = "lblChipPitchYKey";
            this.lblChipPitchYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipPitchYKey.Size = new System.Drawing.Size(174, 28);
            this.lblChipPitchYKey.TabIndex = 7;
            this.lblChipPitchYKey.Text = "PITCH X";
            this.lblChipPitchYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPitchX
            // 
            this._nPitchX.DecimalPlaces = 3;
            this._nPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchX.Location = new System.Drawing.Point(177, 117);
            this._nPitchX.Margin = new System.Windows.Forms.Padding(1);
            this._nPitchX.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nPitchX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchX.Name = "_nPitchX";
            this._nPitchX.Size = new System.Drawing.Size(242, 23);
            this._nPitchX.TabIndex = 8;
            this._nPitchX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.lblWaferDiameterKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblWaferDiameterKey.Location = new System.Drawing.Point(1, 147);
            this.lblWaferDiameterKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblWaferDiameterKey.Name = "lblWaferDiameterKey";
            this.lblWaferDiameterKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblWaferDiameterKey.Size = new System.Drawing.Size(174, 28);
            this.lblWaferDiameterKey.TabIndex = 9;
            this.lblWaferDiameterKey.Text = "PITCH Y";
            this.lblWaferDiameterKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPitchY
            // 
            this._nPitchY.DecimalPlaces = 3;
            this._nPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchY.Location = new System.Drawing.Point(177, 147);
            this._nPitchY.Margin = new System.Windows.Forms.Padding(1);
            this._nPitchY.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._nPitchY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchY.Name = "_nPitchY";
            this._nPitchY.Size = new System.Drawing.Size(242, 23);
            this._nPitchY.TabIndex = 10;
            this._nPitchY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._nPitchY.Value = new decimal(new int[] {
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
            this.lblAxisXKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAxisXKey.Location = new System.Drawing.Point(1, 177);
            this.lblAxisXKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisXKey.Name = "lblAxisXKey";
            this.lblAxisXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAxisXKey.Size = new System.Drawing.Size(174, 28);
            this.lblAxisXKey.TabIndex = 11;
            this.lblAxisXKey.Text = "WAFER DIAMETER";
            this.lblAxisXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nDiameter
            // 
            this._nDiameter.DecimalPlaces = 1;
            this._nDiameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDiameter.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDiameter.Location = new System.Drawing.Point(177, 177);
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
            this._nDiameter.Size = new System.Drawing.Size(242, 23);
            this._nDiameter.TabIndex = 12;
            this._nDiameter.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._nDiameter.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            // 
            // lblAxisYKey
            // 
            this.lblAxisYKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblAxisYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisYKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAxisYKey.Location = new System.Drawing.Point(1, 207);
            this.lblAxisYKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisYKey.Name = "lblAxisYKey";
            this.lblAxisYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAxisYKey.Size = new System.Drawing.Size(174, 28);
            this.lblAxisYKey.TabIndex = 13;
            this.lblAxisYKey.Text = "EDGE SKIP L/R, T/B";
            this.lblAxisYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // edgeSkipPanel
            // 
            this.edgeSkipPanel.ColumnCount = 2;
            this.edgeSkipPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.edgeSkipPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.edgeSkipPanel.Controls.Add(this._nSideEdgeSkip, 0, 0);
            this.edgeSkipPanel.Controls.Add(this._nTopBottomEdgeSkip, 1, 0);
            this.edgeSkipPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.edgeSkipPanel.Location = new System.Drawing.Point(176, 206);
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
            this._nSideEdgeSkip.Font = new System.Drawing.Font("Consolas", 10F);
            this._nSideEdgeSkip.Location = new System.Drawing.Point(1, 1);
            this._nSideEdgeSkip.Margin = new System.Windows.Forms.Padding(1);
            this._nSideEdgeSkip.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._nSideEdgeSkip.Name = "_nSideEdgeSkip";
            this._nSideEdgeSkip.Size = new System.Drawing.Size(120, 23);
            this._nSideEdgeSkip.TabIndex = 0;
            this._nSideEdgeSkip.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // _nTopBottomEdgeSkip
            // 
            this._nTopBottomEdgeSkip.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nTopBottomEdgeSkip.Font = new System.Drawing.Font("Consolas", 10F);
            this._nTopBottomEdgeSkip.Location = new System.Drawing.Point(123, 1);
            this._nTopBottomEdgeSkip.Margin = new System.Windows.Forms.Padding(1);
            this._nTopBottomEdgeSkip.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._nTopBottomEdgeSkip.Name = "_nTopBottomEdgeSkip";
            this._nTopBottomEdgeSkip.Size = new System.Drawing.Size(120, 23);
            this._nTopBottomEdgeSkip.TabIndex = 1;
            this._nTopBottomEdgeSkip.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            this.modeSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modeSection.Location = new System.Drawing.Point(0, 248);
            this.modeSection.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.modeSection.Name = "modeSection";
            this.modeSection.RowCount = 9;
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.modeSection.Size = new System.Drawing.Size(420, 315);
            this.modeSection.TabIndex = 1;
            // 
            // lblModeTitle
            // 
            this.lblModeTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblModeTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblModeTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblModeTitle.ForeColor = System.Drawing.Color.White;
            this.lblModeTitle.Location = new System.Drawing.Point(0, 0);
            this.lblModeTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblModeTitle.Name = "lblModeTitle";
            this.lblModeTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblModeTitle.Size = new System.Drawing.Size(420, 22);
            this.lblModeTitle.TabIndex = 0;
            this.lblModeTitle.Text = "MODE";
            this.lblModeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // chkCircularMap
            // 
            this.chkCircularMap.Checked = true;
            this.chkCircularMap.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCircularMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkCircularMap.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.chkCircularMap.Location = new System.Drawing.Point(12, 26);
            this.chkCircularMap.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.chkCircularMap.Name = "chkCircularMap";
            this.chkCircularMap.Size = new System.Drawing.Size(408, 36);
            this.chkCircularMap.TabIndex = 1;
            this.chkCircularMap.Text = "CIRCLE DIE MAP";
            this.chkCircularMap.UseVisualStyleBackColor = true;
            // 
            // rbStandard
            // 
            this.rbStandard.Checked = true;
            this.rbStandard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStandard.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbStandard.Location = new System.Drawing.Point(12, 62);
            this.rbStandard.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbStandard.Name = "rbStandard";
            this.rbStandard.Size = new System.Drawing.Size(408, 36);
            this.rbStandard.TabIndex = 2;
            this.rbStandard.TabStop = true;
            this.rbStandard.Text = "STANDARD";
            // 
            // rbStartIndex
            // 
            this.rbStartIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStartIndex.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbStartIndex.Location = new System.Drawing.Point(12, 98);
            this.rbStartIndex.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbStartIndex.Name = "rbStartIndex";
            this.rbStartIndex.Size = new System.Drawing.Size(408, 36);
            this.rbStartIndex.TabIndex = 3;
            this.rbStartIndex.Text = "START INDEX";
            // 
            // rbReference1
            // 
            this.rbReference1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbReference1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbReference1.Location = new System.Drawing.Point(12, 134);
            this.rbReference1.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbReference1.Name = "rbReference1";
            this.rbReference1.Size = new System.Drawing.Size(408, 36);
            this.rbReference1.TabIndex = 4;
            this.rbReference1.Text = "1 REFERENCE INDEX";
            // 
            // rbReference2
            // 
            this.rbReference2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbReference2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbReference2.Location = new System.Drawing.Point(12, 170);
            this.rbReference2.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbReference2.Name = "rbReference2";
            this.rbReference2.Size = new System.Drawing.Size(408, 36);
            this.rbReference2.TabIndex = 5;
            this.rbReference2.Text = "2 REFERENCE INDEX";
            // 
            // rbManualSelectPick
            // 
            this.rbManualSelectPick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbManualSelectPick.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbManualSelectPick.Location = new System.Drawing.Point(12, 206);
            this.rbManualSelectPick.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbManualSelectPick.Name = "rbManualSelectPick";
            this.rbManualSelectPick.Size = new System.Drawing.Size(408, 36);
            this.rbManualSelectPick.TabIndex = 6;
            this.rbManualSelectPick.Text = "MANUAL SELECT PICK";
            // 
            // rbAlignCheckIndex
            // 
            this.rbAlignCheckIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAlignCheckIndex.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbAlignCheckIndex.Location = new System.Drawing.Point(12, 242);
            this.rbAlignCheckIndex.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbAlignCheckIndex.Name = "rbAlignCheckIndex";
            this.rbAlignCheckIndex.Size = new System.Drawing.Size(408, 36);
            this.rbAlignCheckIndex.TabIndex = 7;
            this.rbAlignCheckIndex.Text = "ALIGN CHECK INDEX";
            // 
            // rbDragSelectPick
            // 
            this.rbDragSelectPick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbDragSelectPick.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbDragSelectPick.Location = new System.Drawing.Point(12, 278);
            this.rbDragSelectPick.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbDragSelectPick.Name = "rbDragSelectPick";
            this.rbDragSelectPick.Size = new System.Drawing.Size(408, 37);
            this.rbDragSelectPick.TabIndex = 8;
            this.rbDragSelectPick.Text = "DRAG SELECT PICK";
            // 
            // actionSection
            // 
            this.actionSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.actionSection.ColumnCount = 2;
            this.actionSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.actionSection.Controls.Add(this.lblActionTitle, 0, 0);
            this.actionSection.Controls.Add(this.btnCreate, 0, 1);
            this.actionSection.Controls.Add(this.btnSave, 1, 1);
            this.actionSection.Controls.Add(this.btnFirstDieMoveComplete, 0, 2);
            this.actionSection.Controls.Add(this.btnAutoMatch, 1, 2);
            this.actionSection.Controls.Add(this.btnThetaMatchMove, 0, 3);
            this.actionSection.Controls.Add(this.btnXyMatchMove, 1, 3);
            this.actionSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionSection.Location = new System.Drawing.Point(0, 571);
            this.actionSection.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.actionSection.Name = "actionSection";
            this.actionSection.RowCount = 4;
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.actionSection.Size = new System.Drawing.Size(420, 275);
            this.actionSection.TabIndex = 2;
            // 
            // lblActionTitle
            // 
            this.lblActionTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.actionSection.SetColumnSpan(this.lblActionTitle, 2);
            this.lblActionTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActionTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblActionTitle.ForeColor = System.Drawing.Color.White;
            this.lblActionTitle.Location = new System.Drawing.Point(0, 0);
            this.lblActionTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblActionTitle.Name = "lblActionTitle";
            this.lblActionTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblActionTitle.Size = new System.Drawing.Size(420, 22);
            this.lblActionTitle.TabIndex = 0;
            this.lblActionTitle.Text = "ACTION";
            this.lblActionTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnCreate
            // 
            this.btnCreate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnCreate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCreate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCreate.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnCreate.ForeColor = System.Drawing.Color.White;
            this.btnCreate.Location = new System.Drawing.Point(4, 30);
            this.btnCreate.Margin = new System.Windows.Forms.Padding(4);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(202, 74);
            this.btnCreate.TabIndex = 1;
            this.btnCreate.Text = "CREATE";
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(214, 30);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(202, 74);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "SAVE";
            // 
            // btnFirstDieMoveComplete
            // 
            this.btnFirstDieMoveComplete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnFirstDieMoveComplete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFirstDieMoveComplete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFirstDieMoveComplete.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnFirstDieMoveComplete.ForeColor = System.Drawing.Color.White;
            this.btnFirstDieMoveComplete.Location = new System.Drawing.Point(4, 112);
            this.btnFirstDieMoveComplete.Margin = new System.Windows.Forms.Padding(4);
            this.btnFirstDieMoveComplete.Name = "btnFirstDieMoveComplete";
            this.btnFirstDieMoveComplete.Size = new System.Drawing.Size(202, 75);
            this.btnFirstDieMoveComplete.TabIndex = 3;
            this.btnFirstDieMoveComplete.Text = "FIRST DIE MOVE COMPLETE";
            // 
            // btnAutoMatch
            // 
            this.btnAutoMatch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnAutoMatch.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAutoMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAutoMatch.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnAutoMatch.ForeColor = System.Drawing.Color.White;
            this.btnAutoMatch.Location = new System.Drawing.Point(214, 112);
            this.btnAutoMatch.Margin = new System.Windows.Forms.Padding(4);
            this.btnAutoMatch.Name = "btnAutoMatch";
            this.btnAutoMatch.Size = new System.Drawing.Size(202, 75);
            this.btnAutoMatch.TabIndex = 4;
            this.btnAutoMatch.Text = "AUTO MATCH";
            // 
            // btnThetaMatchMove
            // 
            this.btnThetaMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnThetaMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnThetaMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnThetaMatchMove.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnThetaMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnThetaMatchMove.Location = new System.Drawing.Point(4, 195);
            this.btnThetaMatchMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnThetaMatchMove.Name = "btnThetaMatchMove";
            this.btnThetaMatchMove.Size = new System.Drawing.Size(202, 76);
            this.btnThetaMatchMove.TabIndex = 5;
            this.btnThetaMatchMove.Text = "THETA MATCH MOVE";
            // 
            // btnXyMatchMove
            // 
            this.btnXyMatchMove.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnXyMatchMove.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnXyMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXyMatchMove.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnXyMatchMove.ForeColor = System.Drawing.Color.White;
            this.btnXyMatchMove.Location = new System.Drawing.Point(214, 195);
            this.btnXyMatchMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnXyMatchMove.Name = "btnXyMatchMove";
            this.btnXyMatchMove.Size = new System.Drawing.Size(202, 76);
            this.btnXyMatchMove.TabIndex = 6;
            this.btnXyMatchMove.Text = "X/Y MATCH MOVE";
            // 
            // MapCreatePage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
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
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).EndInit();
            this.edgeSkipPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nSideEdgeSkip)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTopBottomEdgeSkip)).EndInit();
            this.modeSection.ResumeLayout(false);
            this.actionSection.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

