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
        private System.Windows.Forms.TableLayoutPanel rightLayout;
        private System.Windows.Forms.TableLayoutPanel settingSection;
        private System.Windows.Forms.Label lblSettingTitle;
        private System.Windows.Forms.Label lblChipCountXKey;
        private System.Windows.Forms.Label lblChipCountXVal;
        private System.Windows.Forms.Label lblChipCountYKey;
        private System.Windows.Forms.Label lblChipCountYVal;
        private System.Windows.Forms.Label lblChipPitchXKey;
        private System.Windows.Forms.Label lblChipPitchXVal;
        private System.Windows.Forms.Label lblChipPitchYKey;
        private System.Windows.Forms.Label lblChipPitchYVal;
        private System.Windows.Forms.Label lblWaferDiameterKey;
        private System.Windows.Forms.Label lblWaferDiameterVal;
        private System.Windows.Forms.Label lblAxisXKey;
        private System.Windows.Forms.Label lblAxisXVal;
        private System.Windows.Forms.Label lblAxisYKey;
        private System.Windows.Forms.Label lblAxisYVal;
        private System.Windows.Forms.TableLayoutPanel modeSection;
        private System.Windows.Forms.Label lblModeTitle;
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
        private System.Windows.Forms.TableLayoutPanel jogSection;
        private System.Windows.Forms.Label lblJogTitle;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogPlusY;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogMinusX;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogStop;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogPlusX;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogMinusY;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogMinusT;
        private QMC.CDT_320.Ui.Controls.ActionButton btnJogPlusT;

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
            this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
            this.settingSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblSettingTitle = new System.Windows.Forms.Label();
            this.lblChipCountXKey = new System.Windows.Forms.Label();
            this.lblChipCountXVal = new System.Windows.Forms.Label();
            this.lblChipCountYKey = new System.Windows.Forms.Label();
            this.lblChipCountYVal = new System.Windows.Forms.Label();
            this.lblChipPitchXKey = new System.Windows.Forms.Label();
            this.lblChipPitchXVal = new System.Windows.Forms.Label();
            this.lblChipPitchYKey = new System.Windows.Forms.Label();
            this.lblChipPitchYVal = new System.Windows.Forms.Label();
            this.lblWaferDiameterKey = new System.Windows.Forms.Label();
            this.lblWaferDiameterVal = new System.Windows.Forms.Label();
            this.lblAxisXKey = new System.Windows.Forms.Label();
            this.lblAxisXVal = new System.Windows.Forms.Label();
            this.lblAxisYKey = new System.Windows.Forms.Label();
            this.lblAxisYVal = new System.Windows.Forms.Label();
            this.modeSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblModeTitle = new System.Windows.Forms.Label();
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
            this.jogSection = new System.Windows.Forms.TableLayoutPanel();
            this.lblJogTitle = new System.Windows.Forms.Label();
            this.btnJogPlusY = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogMinusX = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogStop = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogPlusX = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogMinusY = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogMinusT = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnJogPlusT = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.mainLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
            this.mapSection.SuspendLayout();
            this.rightLayout.SuspendLayout();
            this.settingSection.SuspendLayout();
            this.modeSection.SuspendLayout();
            this.actionSection.SuspendLayout();
            this.jogSection.SuspendLayout();
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
            this.mainLayout.Size = new System.Drawing.Size(1400, 900);
            this.mainLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1400, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "MAP CREATE";
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
            this.contentLayout.Size = new System.Drawing.Size(1400, 870);
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
            this.mapSection.Size = new System.Drawing.Size(956, 854);
            this.mapSection.TabIndex = 0;
            // 
            // lblMapTitle
            // 
            this.lblMapTitle.BackColor = System.Drawing.Color.FromArgb(217, 119, 6);
            this.lblMapTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblMapTitle.ForeColor = System.Drawing.Color.White;
            this.lblMapTitle.Location = new System.Drawing.Point(0, 0);
            this.lblMapTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblMapTitle.Name = "lblMapTitle";
            this.lblMapTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblMapTitle.Size = new System.Drawing.Size(956, 26);
            this.lblMapTitle.TabIndex = 0;
            this.lblMapTitle.Text = "MAP";
            this.lblMapTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mapPanel
            // 
            this.mapPanel.BackColor = System.Drawing.Color.Black;
            this.mapPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mapPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapPanel.Location = new System.Drawing.Point(0, 30);
            this.mapPanel.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.mapPanel.Name = "mapPanel";
            this.mapPanel.Size = new System.Drawing.Size(956, 824);
            this.mapPanel.TabIndex = 1;
            // 
            // rightLayout
            // 
            this.rightLayout.ColumnCount = 1;
            this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightLayout.Controls.Add(this.settingSection, 0, 0);
            this.rightLayout.Controls.Add(this.modeSection, 0, 1);
            this.rightLayout.Controls.Add(this.actionSection, 0, 2);
            this.rightLayout.Controls.Add(this.jogSection, 0, 3);
            this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightLayout.Location = new System.Drawing.Point(972, 8);
            this.rightLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rightLayout.Name = "rightLayout";
            this.rightLayout.RowCount = 4;
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 210F));
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
            this.settingSection.Controls.Add(this.lblChipCountXVal, 1, 1);
            this.settingSection.Controls.Add(this.lblChipCountYKey, 0, 2);
            this.settingSection.Controls.Add(this.lblChipCountYVal, 1, 2);
            this.settingSection.Controls.Add(this.lblChipPitchXKey, 0, 3);
            this.settingSection.Controls.Add(this.lblChipPitchXVal, 1, 3);
            this.settingSection.Controls.Add(this.lblChipPitchYKey, 0, 4);
            this.settingSection.Controls.Add(this.lblChipPitchYVal, 1, 4);
            this.settingSection.Controls.Add(this.lblWaferDiameterKey, 0, 5);
            this.settingSection.Controls.Add(this.lblWaferDiameterVal, 1, 5);
            this.settingSection.Controls.Add(this.lblAxisXKey, 0, 6);
            this.settingSection.Controls.Add(this.lblAxisXVal, 1, 6);
            this.settingSection.Controls.Add(this.lblAxisYKey, 0, 7);
            this.settingSection.Controls.Add(this.lblAxisYVal, 1, 7);
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
            this.settingSection.Size = new System.Drawing.Size(420, 252);
            this.settingSection.TabIndex = 0;
            // 
            // lblSettingTitle
            // 
            this.lblSettingTitle.BackColor = System.Drawing.Color.FromArgb(217, 119, 6);
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
            this.lblSettingTitle.Text = "SETTING";
            this.lblSettingTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // setting rows
            // 
            this.lblChipCountXKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipCountXKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipCountXKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipCountXKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblChipCountXKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipCountXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipCountXKey.Text = "CHIP COUNT X";
            this.lblChipCountXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblChipCountXVal.BackColor = System.Drawing.Color.White;
            this.lblChipCountXVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipCountXVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipCountXVal.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblChipCountXVal.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipCountXVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipCountXVal.Text = "7435";
            this.lblChipCountXVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblChipCountYKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipCountYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipCountYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipCountYKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblChipCountYKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipCountYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipCountYKey.Text = "CHIP COUNT Y";
            this.lblChipCountYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblChipCountYVal.BackColor = System.Drawing.Color.White;
            this.lblChipCountYVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipCountYVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipCountYVal.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblChipCountYVal.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipCountYVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipCountYVal.Text = "5840";
            this.lblChipCountYVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblChipPitchXKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipPitchXKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipPitchXKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipPitchXKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblChipPitchXKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipPitchXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipPitchXKey.Text = "CHIP PITCH X";
            this.lblChipPitchXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblChipPitchXVal.BackColor = System.Drawing.Color.White;
            this.lblChipPitchXVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipPitchXVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipPitchXVal.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblChipPitchXVal.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipPitchXVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipPitchXVal.Text = "7437 um";
            this.lblChipPitchXVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblChipPitchYKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblChipPitchYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipPitchYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipPitchYKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblChipPitchYKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipPitchYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipPitchYKey.Text = "CHIP PITCH Y";
            this.lblChipPitchYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblChipPitchYVal.BackColor = System.Drawing.Color.White;
            this.lblChipPitchYVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblChipPitchYVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChipPitchYVal.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblChipPitchYVal.Margin = new System.Windows.Forms.Padding(1);
            this.lblChipPitchYVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChipPitchYVal.Text = "5842 um";
            this.lblChipPitchYVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblWaferDiameterKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblWaferDiameterKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDiameterKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDiameterKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblWaferDiameterKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblWaferDiameterKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblWaferDiameterKey.Text = "WAFER DIAMETER";
            this.lblWaferDiameterKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblWaferDiameterVal.BackColor = System.Drawing.Color.White;
            this.lblWaferDiameterVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWaferDiameterVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferDiameterVal.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblWaferDiameterVal.Margin = new System.Windows.Forms.Padding(1);
            this.lblWaferDiameterVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblWaferDiameterVal.Text = "300000 um";
            this.lblWaferDiameterVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblAxisXKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblAxisXKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisXKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisXKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAxisXKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisXKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAxisXKey.Text = "AXIS X";
            this.lblAxisXKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAxisXVal.BackColor = System.Drawing.Color.White;
            this.lblAxisXVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisXVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisXVal.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxisXVal.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisXVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAxisXVal.Text = "0 um";
            this.lblAxisXVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblAxisYKey.BackColor = System.Drawing.Color.Gainsboro;
            this.lblAxisYKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisYKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisYKey.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAxisYKey.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisYKey.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAxisYKey.Text = "AXIS Y";
            this.lblAxisYKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAxisYVal.BackColor = System.Drawing.Color.White;
            this.lblAxisYVal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblAxisYVal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAxisYVal.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblAxisYVal.Margin = new System.Windows.Forms.Padding(1);
            this.lblAxisYVal.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblAxisYVal.Text = "0 um";
            this.lblAxisYVal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // modeSection
            // 
            this.modeSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.modeSection.ColumnCount = 1;
            this.modeSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.modeSection.Controls.Add(this.lblModeTitle, 0, 0);
            this.modeSection.Controls.Add(this.rbStandard, 0, 1);
            this.modeSection.Controls.Add(this.rbStartIndex, 0, 2);
            this.modeSection.Controls.Add(this.rbReference1, 0, 3);
            this.modeSection.Controls.Add(this.rbReference2, 0, 4);
            this.modeSection.Controls.Add(this.rbManualSelectPick, 0, 5);
            this.modeSection.Controls.Add(this.rbAlignCheckIndex, 0, 6);
            this.modeSection.Controls.Add(this.rbDragSelectPick, 0, 7);
            this.modeSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modeSection.Location = new System.Drawing.Point(0, 260);
            this.modeSection.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.modeSection.Name = "modeSection";
            this.modeSection.RowCount = 8;
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.modeSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.285F));
            this.modeSection.Size = new System.Drawing.Size(420, 212);
            this.modeSection.TabIndex = 1;
            // 
            // lblModeTitle
            // 
            this.lblModeTitle.BackColor = System.Drawing.Color.FromArgb(217, 119, 6);
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
            // mode radio buttons
            // 
            this.rbStandard.Checked = true;
            this.rbStandard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStandard.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbStandard.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbStandard.Text = "STANDARD";
            this.rbStartIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbStartIndex.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbStartIndex.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbStartIndex.Text = "START INDEX";
            this.rbReference1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbReference1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbReference1.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbReference1.Text = "1 REFERENCE INDEX";
            this.rbReference2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbReference2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbReference2.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbReference2.Text = "2 REFERENCE INDEX";
            this.rbManualSelectPick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbManualSelectPick.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbManualSelectPick.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbManualSelectPick.Text = "MANUAL SELECT PICK";
            this.rbAlignCheckIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbAlignCheckIndex.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbAlignCheckIndex.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.rbAlignCheckIndex.Text = "ALIGN CHECK INDEX";
            this.rbDragSelectPick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rbDragSelectPick.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.rbDragSelectPick.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
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
            this.actionSection.Location = new System.Drawing.Point(0, 480);
            this.actionSection.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.actionSection.Name = "actionSection";
            this.actionSection.RowCount = 4;
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.actionSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.actionSection.Size = new System.Drawing.Size(420, 202);
            this.actionSection.TabIndex = 2;
            // 
            // lblActionTitle
            // 
            this.lblActionTitle.BackColor = System.Drawing.Color.FromArgb(217, 119, 6);
            this.actionSection.SetColumnSpan(this.lblActionTitle, 2);
            this.lblActionTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblActionTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblActionTitle.ForeColor = System.Drawing.Color.White;
            this.lblActionTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblActionTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblActionTitle.Text = "ACTION";
            this.lblActionTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // action buttons
            // 
            this.btnCreate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCreate.Margin = new System.Windows.Forms.Padding(4);
            this.btnCreate.Text = "CREATE";
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Text = "SAVE";
            this.btnFirstDieMoveComplete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFirstDieMoveComplete.Margin = new System.Windows.Forms.Padding(4);
            this.btnFirstDieMoveComplete.Text = "FIRST DIE MOVE COMPLETE";
            this.btnAutoMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAutoMatch.Margin = new System.Windows.Forms.Padding(4);
            this.btnAutoMatch.Text = "AUTO MATCH";
            this.btnThetaMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnThetaMatchMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnThetaMatchMove.Text = "THETA MATCH MOVE";
            this.btnXyMatchMove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXyMatchMove.Margin = new System.Windows.Forms.Padding(4);
            this.btnXyMatchMove.Text = "X/Y MATCH MOVE";
            // 
            // jogSection
            // 
            this.jogSection.BackColor = System.Drawing.Color.WhiteSmoke;
            this.jogSection.ColumnCount = 3;
            this.jogSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.jogSection.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogSection.Controls.Add(this.lblJogTitle, 0, 0);
            this.jogSection.Controls.Add(this.btnJogPlusY, 1, 1);
            this.jogSection.Controls.Add(this.btnJogMinusX, 0, 2);
            this.jogSection.Controls.Add(this.btnJogStop, 1, 2);
            this.jogSection.Controls.Add(this.btnJogPlusX, 2, 2);
            this.jogSection.Controls.Add(this.btnJogMinusT, 0, 3);
            this.jogSection.Controls.Add(this.btnJogMinusY, 1, 3);
            this.jogSection.Controls.Add(this.btnJogPlusT, 2, 3);
            this.jogSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jogSection.Location = new System.Drawing.Point(0, 690);
            this.jogSection.Margin = new System.Windows.Forms.Padding(0);
            this.jogSection.Name = "jogSection";
            this.jogSection.RowCount = 4;
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.jogSection.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.jogSection.Size = new System.Drawing.Size(420, 164);
            this.jogSection.TabIndex = 3;
            // 
            // lblJogTitle
            // 
            this.lblJogTitle.BackColor = System.Drawing.Color.FromArgb(217, 119, 6);
            this.jogSection.SetColumnSpan(this.lblJogTitle, 3);
            this.lblJogTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJogTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblJogTitle.ForeColor = System.Drawing.Color.White;
            this.lblJogTitle.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblJogTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblJogTitle.Text = "JOG";
            this.lblJogTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // jog buttons
            // 
            this.btnJogPlusY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogPlusY.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogPlusY.Text = "+Y";
            this.btnJogMinusX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogMinusX.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogMinusX.Text = "-X";
            this.btnJogStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogStop.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogStop.Text = "STOP";
            this.btnJogPlusX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogPlusX.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogPlusX.Text = "+X";
            this.btnJogMinusY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogMinusY.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogMinusY.Text = "-Y";
            this.btnJogMinusT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogMinusT.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogMinusT.Text = "-T";
            this.btnJogPlusT.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJogPlusT.Margin = new System.Windows.Forms.Padding(4);
            this.btnJogPlusT.Text = "+T";
            // 
            // MapCreatePage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(191, 191, 191);
            this.Controls.Add(this.mainLayout);
            this.Name = "MapCreatePage";
            this.Size = new System.Drawing.Size(1400, 900);
            this.mainLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            this.mapSection.ResumeLayout(false);
            this.rightLayout.ResumeLayout(false);
            this.settingSection.ResumeLayout(false);
            this.modeSection.ResumeLayout(false);
            this.actionSection.ResumeLayout(false);
            this.jogSection.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
