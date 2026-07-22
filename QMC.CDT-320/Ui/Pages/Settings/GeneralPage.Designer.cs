namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class GeneralPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.GroupBox grpSetting;
        private System.Windows.Forms.TableLayoutPanel settingLayout;
        private System.Windows.Forms.TableLayoutPanel bodyLayout;
        private System.Windows.Forms.Label lblLanguage;
        private System.Windows.Forms.Label lblBinArray;
        private System.Windows.Forms.Label lblVisionMatch;
        private System.Windows.Forms.Label lblSimulationMode;
        private System.Windows.Forms.Label lblDryRunMode;
        private System.Windows.Forms.Label lblDeveloperMode;
        private System.Windows.Forms.Label lblPickerMotionOnlyTestMode;
        private System.Windows.Forms.Label lblUseVision;
        private System.Windows.Forms.Label lblUseRealVisionInSimulation;
        private System.Windows.Forms.Label lblWaferCompleteRunMode;
        private System.Windows.Forms.Label lblPickRuntimeOffset;
        private System.Windows.Forms.Label lblPlaceRuntimeOffset;
        private System.Windows.Forms.TableLayoutPanel pickRuntimeOffsetLayout;
        private System.Windows.Forms.TableLayoutPanel placeRuntimeOffsetLayout;
        private System.Windows.Forms.ComboBox _cbPickRuntimeOffset;
        private System.Windows.Forms.ComboBox _cbPlaceRuntimeOffset;
        private System.Windows.Forms.Button btnResetPickRuntimeOffset;
        private System.Windows.Forms.Button btnResetPlaceRuntimeOffset;
        private System.Windows.Forms.ComboBox _cbLang;
        private System.Windows.Forms.ComboBox _cbBinArr;
        private System.Windows.Forms.ComboBox _cbVisionMatch;
        private System.Windows.Forms.ComboBox _cbSimulationMode;
        private System.Windows.Forms.ComboBox _cbDryRunMode;
        private System.Windows.Forms.ComboBox _cbDeveloperMode;
        private System.Windows.Forms.ComboBox _cbPickerMotionOnlyTestMode;
        private System.Windows.Forms.ComboBox _cbUseVision;
        private System.Windows.Forms.ComboBox _cbUseRealVisionInSimulation;
        private System.Windows.Forms.ComboBox _cbWaferCompleteRunMode;
        private System.Windows.Forms.TableLayoutPanel logBtnLayout;
        private System.Windows.Forms.Button btnLogSettings;
        private System.Windows.Forms.GroupBox grpAjin;
        private System.Windows.Forms.TableLayoutPanel ajinLayout;
        private System.Windows.Forms.CheckBox _cbAjin;
        private System.Windows.Forms.Label lblIrq;
        private System.Windows.Forms.TextBox _tbIrq;

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
            this.grpSetting = new System.Windows.Forms.GroupBox();
            this.settingLayout = new System.Windows.Forms.TableLayoutPanel();
            this.bodyLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.lblBinArray = new System.Windows.Forms.Label();
            this.lblVisionMatch = new System.Windows.Forms.Label();
            this.lblSimulationMode = new System.Windows.Forms.Label();
            this.lblDryRunMode = new System.Windows.Forms.Label();
            this.lblDeveloperMode = new System.Windows.Forms.Label();
            this.lblPickerMotionOnlyTestMode = new System.Windows.Forms.Label();
            this.lblUseVision = new System.Windows.Forms.Label();
            this.lblUseRealVisionInSimulation = new System.Windows.Forms.Label();
            this.lblWaferCompleteRunMode = new System.Windows.Forms.Label();
            this._cbLang = new System.Windows.Forms.ComboBox();
            this._cbBinArr = new System.Windows.Forms.ComboBox();
            this._cbVisionMatch = new System.Windows.Forms.ComboBox();
            this._cbSimulationMode = new System.Windows.Forms.ComboBox();
            this._cbDryRunMode = new System.Windows.Forms.ComboBox();
            this._cbDeveloperMode = new System.Windows.Forms.ComboBox();
            this._cbPickerMotionOnlyTestMode = new System.Windows.Forms.ComboBox();
            this._cbUseVision = new System.Windows.Forms.ComboBox();
            this._cbUseRealVisionInSimulation = new System.Windows.Forms.ComboBox();
            this._cbWaferCompleteRunMode = new System.Windows.Forms.ComboBox();
            this.lblPickRuntimeOffset = new System.Windows.Forms.Label();
            this.lblPlaceRuntimeOffset = new System.Windows.Forms.Label();
            this.pickRuntimeOffsetLayout = new System.Windows.Forms.TableLayoutPanel();
            this.placeRuntimeOffsetLayout = new System.Windows.Forms.TableLayoutPanel();
            this._cbPickRuntimeOffset = new System.Windows.Forms.ComboBox();
            this._cbPlaceRuntimeOffset = new System.Windows.Forms.ComboBox();
            this.btnResetPickRuntimeOffset = new System.Windows.Forms.Button();
            this.btnResetPlaceRuntimeOffset = new System.Windows.Forms.Button();
            this.grpAjin = new System.Windows.Forms.GroupBox();
            this.ajinLayout = new System.Windows.Forms.TableLayoutPanel();
            this._cbAjin = new System.Windows.Forms.CheckBox();
            this.lblIrq = new System.Windows.Forms.Label();
            this._tbIrq = new System.Windows.Forms.TextBox();
            this.logBtnLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnLogSettings = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.grpSetting.SuspendLayout();
            this.settingLayout.SuspendLayout();
            this.bodyLayout.SuspendLayout();
            this.pickRuntimeOffsetLayout.SuspendLayout();
            this.placeRuntimeOffsetLayout.SuspendLayout();
            this.grpAjin.SuspendLayout();
            this.ajinLayout.SuspendLayout();
            this.logBtnLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.grpSetting, 0, 1);
            this.rootLayout.Controls.Add(this.grpAjin, 0, 2);
            this.rootLayout.Controls.Add(this.logBtnLayout, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 440F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 78F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.rootLayout.SetColumnSpan(this.lblHeader, 2);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(8, 8);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1662, 26);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "SETTING";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpSetting
            // 
            this.grpSetting.Controls.Add(this.settingLayout);
            this.grpSetting.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSetting.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.grpSetting.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.grpSetting.Location = new System.Drawing.Point(8, 38);
            this.grpSetting.Margin = new System.Windows.Forms.Padding(0, 0, 0, 1);
            this.grpSetting.Name = "grpSetting";
            this.grpSetting.Padding = new System.Windows.Forms.Padding(1, 10, 1, 2);
            this.grpSetting.Size = new System.Drawing.Size(831, 439);
            this.grpSetting.TabIndex = 1;
            this.grpSetting.TabStop = false;
            this.grpSetting.Text = "SETTING";
            // 
            // settingLayout
            // 
            this.settingLayout.ColumnCount = 1;
            this.settingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingLayout.Controls.Add(this.bodyLayout, 0, 0);
            this.settingLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingLayout.Location = new System.Drawing.Point(1, 30);
            this.settingLayout.Margin = new System.Windows.Forms.Padding(0);
            this.settingLayout.Name = "settingLayout";
            this.settingLayout.RowCount = 1;
            this.settingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 408F));
            this.settingLayout.Size = new System.Drawing.Size(829, 407);
            this.settingLayout.TabIndex = 0;
            // 
            // bodyLayout
            // 
            this.bodyLayout.ColumnCount = 2;
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.Controls.Add(this.lblLanguage, 0, 0);
            this.bodyLayout.Controls.Add(this.lblBinArray, 0, 1);
            this.bodyLayout.Controls.Add(this.lblVisionMatch, 0, 2);
            this.bodyLayout.Controls.Add(this.lblSimulationMode, 0, 3);
            this.bodyLayout.Controls.Add(this.lblDryRunMode, 0, 4);
            this.bodyLayout.Controls.Add(this.lblDeveloperMode, 0, 5);
            this.bodyLayout.Controls.Add(this.lblPickerMotionOnlyTestMode, 0, 6);
            this.bodyLayout.Controls.Add(this.lblUseVision, 0, 7);
            this.bodyLayout.Controls.Add(this.lblUseRealVisionInSimulation, 0, 8);
            this.bodyLayout.Controls.Add(this.lblWaferCompleteRunMode, 0, 9);
            this.bodyLayout.Controls.Add(this._cbLang, 1, 0);
            this.bodyLayout.Controls.Add(this._cbBinArr, 1, 1);
            this.bodyLayout.Controls.Add(this._cbVisionMatch, 1, 2);
            this.bodyLayout.Controls.Add(this._cbSimulationMode, 1, 3);
            this.bodyLayout.Controls.Add(this._cbDryRunMode, 1, 4);
            this.bodyLayout.Controls.Add(this._cbDeveloperMode, 1, 5);
            this.bodyLayout.Controls.Add(this._cbPickerMotionOnlyTestMode, 1, 6);
            this.bodyLayout.Controls.Add(this._cbUseVision, 1, 7);
            this.bodyLayout.Controls.Add(this._cbUseRealVisionInSimulation, 1, 8);
            this.bodyLayout.Controls.Add(this._cbWaferCompleteRunMode, 1, 9);
            this.bodyLayout.Controls.Add(this.lblPickRuntimeOffset, 0, 10);
            this.bodyLayout.Controls.Add(this.pickRuntimeOffsetLayout, 1, 10);
            this.bodyLayout.Controls.Add(this.lblPlaceRuntimeOffset, 0, 11);
            this.bodyLayout.Controls.Add(this.placeRuntimeOffsetLayout, 1, 11);
            this.bodyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bodyLayout.Location = new System.Drawing.Point(0, 0);
            this.bodyLayout.Margin = new System.Windows.Forms.Padding(0);
            this.bodyLayout.Name = "bodyLayout";
            this.bodyLayout.RowCount = 12;
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.bodyLayout.Size = new System.Drawing.Size(829, 408);
            this.bodyLayout.TabIndex = 1;
            // 
            // lblLanguage
            // 
            this.lblLanguage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblLanguage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLanguage.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblLanguage.Location = new System.Drawing.Point(2, 2);
            this.lblLanguage.Margin = new System.Windows.Forms.Padding(2);
            this.lblLanguage.Name = "lblLanguage";
            this.lblLanguage.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblLanguage.Size = new System.Drawing.Size(176, 30);
            this.lblLanguage.TabIndex = 0;
            this.lblLanguage.Text = "Language";
            this.lblLanguage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBinArray
            // 
            this.lblBinArray.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblBinArray.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBinArray.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblBinArray.Location = new System.Drawing.Point(2, 36);
            this.lblBinArray.Margin = new System.Windows.Forms.Padding(2);
            this.lblBinArray.Name = "lblBinArray";
            this.lblBinArray.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBinArray.Size = new System.Drawing.Size(176, 30);
            this.lblBinArray.TabIndex = 1;
            this.lblBinArray.Text = "Bin Array File";
            this.lblBinArray.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVisionMatch
            // 
            this.lblVisionMatch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblVisionMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionMatch.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblVisionMatch.Location = new System.Drawing.Point(2, 70);
            this.lblVisionMatch.Margin = new System.Windows.Forms.Padding(2);
            this.lblVisionMatch.Name = "lblVisionMatch";
            this.lblVisionMatch.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblVisionMatch.Size = new System.Drawing.Size(176, 30);
            this.lblVisionMatch.TabIndex = 2;
            this.lblVisionMatch.Text = "Vision Match Error";
            this.lblVisionMatch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSimulationMode
            // 
            this.lblSimulationMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblSimulationMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSimulationMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSimulationMode.Location = new System.Drawing.Point(2, 104);
            this.lblSimulationMode.Margin = new System.Windows.Forms.Padding(2);
            this.lblSimulationMode.Name = "lblSimulationMode";
            this.lblSimulationMode.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblSimulationMode.Size = new System.Drawing.Size(176, 30);
            this.lblSimulationMode.TabIndex = 6;
            this.lblSimulationMode.Text = "SIMULATION MODE";
            this.lblSimulationMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDryRunMode
            // 
            this.lblDryRunMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblDryRunMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDryRunMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDryRunMode.Location = new System.Drawing.Point(2, 138);
            this.lblDryRunMode.Margin = new System.Windows.Forms.Padding(2);
            this.lblDryRunMode.Name = "lblDryRunMode";
            this.lblDryRunMode.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDryRunMode.Size = new System.Drawing.Size(176, 30);
            this.lblDryRunMode.TabIndex = 7;
            this.lblDryRunMode.Text = "DRY RUN MODE";
            this.lblDryRunMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDeveloperMode
            // 
            this.lblDeveloperMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblDeveloperMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeveloperMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDeveloperMode.Location = new System.Drawing.Point(2, 172);
            this.lblDeveloperMode.Margin = new System.Windows.Forms.Padding(2);
            this.lblDeveloperMode.Name = "lblDeveloperMode";
            this.lblDeveloperMode.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDeveloperMode.Size = new System.Drawing.Size(176, 30);
            this.lblDeveloperMode.TabIndex = 10;
            this.lblDeveloperMode.Text = "DEVELOPER MODE";
            this.lblDeveloperMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPickerMotionOnlyTestMode
            // 
            this.lblPickerMotionOnlyTestMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblPickerMotionOnlyTestMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickerMotionOnlyTestMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPickerMotionOnlyTestMode.Location = new System.Drawing.Point(2, 206);
            this.lblPickerMotionOnlyTestMode.Margin = new System.Windows.Forms.Padding(2);
            this.lblPickerMotionOnlyTestMode.Name = "lblPickerMotionOnlyTestMode";
            this.lblPickerMotionOnlyTestMode.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPickerMotionOnlyTestMode.Size = new System.Drawing.Size(176, 30);
            this.lblPickerMotionOnlyTestMode.TabIndex = 12;
            this.lblPickerMotionOnlyTestMode.Text = "PICKER MOTION ONLY TEST";
            this.lblPickerMotionOnlyTestMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblUseVision
            // 
            this.lblUseVision.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblUseVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUseVision.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblUseVision.Location = new System.Drawing.Point(2, 240);
            this.lblUseVision.Margin = new System.Windows.Forms.Padding(2);
            this.lblUseVision.Name = "lblUseVision";
            this.lblUseVision.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblUseVision.Size = new System.Drawing.Size(176, 30);
            this.lblUseVision.TabIndex = 14;
            this.lblUseVision.Text = "VISION USE";
            this.lblUseVision.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblUseRealVisionInSimulation
            // 
            this.lblUseRealVisionInSimulation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblUseRealVisionInSimulation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUseRealVisionInSimulation.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblUseRealVisionInSimulation.Location = new System.Drawing.Point(2, 274);
            this.lblUseRealVisionInSimulation.Margin = new System.Windows.Forms.Padding(2);
            this.lblUseRealVisionInSimulation.Name = "lblUseRealVisionInSimulation";
            this.lblUseRealVisionInSimulation.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblUseRealVisionInSimulation.Size = new System.Drawing.Size(176, 30);
            this.lblUseRealVisionInSimulation.TabIndex = 16;
            this.lblUseRealVisionInSimulation.Text = "REAL VISION IN SIMULATION";
            this.lblUseRealVisionInSimulation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWaferCompleteRunMode
            // 
            this.lblWaferCompleteRunMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblWaferCompleteRunMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferCompleteRunMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblWaferCompleteRunMode.Location = new System.Drawing.Point(2, 308);
            this.lblWaferCompleteRunMode.Margin = new System.Windows.Forms.Padding(2);
            this.lblWaferCompleteRunMode.Name = "lblWaferCompleteRunMode";
            this.lblWaferCompleteRunMode.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblWaferCompleteRunMode.Size = new System.Drawing.Size(176, 30);
            this.lblWaferCompleteRunMode.TabIndex = 18;
            this.lblWaferCompleteRunMode.Text = "WAFER COMPLETE RUN MODE";
            this.lblWaferCompleteRunMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbLang
            // 
            this._cbLang.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbLang.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbLang.Location = new System.Drawing.Point(182, 2);
            this._cbLang.Margin = new System.Windows.Forms.Padding(2);
            this._cbLang.Name = "_cbLang";
            this._cbLang.Size = new System.Drawing.Size(645, 23);
            this._cbLang.TabIndex = 3;
            this._cbLang.SelectedIndexChanged += new System.EventHandler(this._cbLang_SelectedIndexChanged);
            // 
            // _cbBinArr
            // 
            this._cbBinArr.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbBinArr.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbBinArr.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbBinArr.Location = new System.Drawing.Point(182, 36);
            this._cbBinArr.Margin = new System.Windows.Forms.Padding(2);
            this._cbBinArr.Name = "_cbBinArr";
            this._cbBinArr.Size = new System.Drawing.Size(645, 23);
            this._cbBinArr.TabIndex = 4;
            this._cbBinArr.SelectedIndexChanged += new System.EventHandler(this._cbBinArr_SelectedIndexChanged);
            // 
            // _cbVisionMatch
            // 
            this._cbVisionMatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbVisionMatch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbVisionMatch.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbVisionMatch.Location = new System.Drawing.Point(182, 70);
            this._cbVisionMatch.Margin = new System.Windows.Forms.Padding(2);
            this._cbVisionMatch.Name = "_cbVisionMatch";
            this._cbVisionMatch.Size = new System.Drawing.Size(645, 23);
            this._cbVisionMatch.TabIndex = 5;
            this._cbVisionMatch.SelectedIndexChanged += new System.EventHandler(this._cbVisionMatch_SelectedIndexChanged);
            // 
            // _cbSimulationMode
            // 
            this._cbSimulationMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbSimulationMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSimulationMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbSimulationMode.Location = new System.Drawing.Point(182, 104);
            this._cbSimulationMode.Margin = new System.Windows.Forms.Padding(2);
            this._cbSimulationMode.Name = "_cbSimulationMode";
            this._cbSimulationMode.Size = new System.Drawing.Size(645, 23);
            this._cbSimulationMode.TabIndex = 8;
            this._cbSimulationMode.SelectedIndexChanged += new System.EventHandler(this._cbSimulationMode_SelectedIndexChanged);
            // 
            // _cbDryRunMode
            // 
            this._cbDryRunMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbDryRunMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbDryRunMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbDryRunMode.Location = new System.Drawing.Point(182, 138);
            this._cbDryRunMode.Margin = new System.Windows.Forms.Padding(2);
            this._cbDryRunMode.Name = "_cbDryRunMode";
            this._cbDryRunMode.Size = new System.Drawing.Size(645, 23);
            this._cbDryRunMode.TabIndex = 9;
            this._cbDryRunMode.SelectedIndexChanged += new System.EventHandler(this._cbDryRunMode_SelectedIndexChanged);
            // 
            // _cbDeveloperMode
            // 
            this._cbDeveloperMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbDeveloperMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbDeveloperMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbDeveloperMode.Location = new System.Drawing.Point(182, 172);
            this._cbDeveloperMode.Margin = new System.Windows.Forms.Padding(2);
            this._cbDeveloperMode.Name = "_cbDeveloperMode";
            this._cbDeveloperMode.Size = new System.Drawing.Size(645, 23);
            this._cbDeveloperMode.TabIndex = 11;
            this._cbDeveloperMode.SelectedIndexChanged += new System.EventHandler(this._cbDeveloperMode_SelectedIndexChanged);
            // 
            // _cbPickerMotionOnlyTestMode
            // 
            this._cbPickerMotionOnlyTestMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbPickerMotionOnlyTestMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbPickerMotionOnlyTestMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbPickerMotionOnlyTestMode.Location = new System.Drawing.Point(182, 206);
            this._cbPickerMotionOnlyTestMode.Margin = new System.Windows.Forms.Padding(2);
            this._cbPickerMotionOnlyTestMode.Name = "_cbPickerMotionOnlyTestMode";
            this._cbPickerMotionOnlyTestMode.Size = new System.Drawing.Size(645, 23);
            this._cbPickerMotionOnlyTestMode.TabIndex = 13;
            this._cbPickerMotionOnlyTestMode.SelectedIndexChanged += new System.EventHandler(this._cbPickerMotionOnlyTestMode_SelectedIndexChanged);
            // 
            // _cbUseVision
            // 
            this._cbUseVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbUseVision.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbUseVision.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbUseVision.Location = new System.Drawing.Point(182, 240);
            this._cbUseVision.Margin = new System.Windows.Forms.Padding(2);
            this._cbUseVision.Name = "_cbUseVision";
            this._cbUseVision.Size = new System.Drawing.Size(645, 23);
            this._cbUseVision.TabIndex = 15;
            this._cbUseVision.SelectedIndexChanged += new System.EventHandler(this._cbUseVision_SelectedIndexChanged);
            // 
            // _cbUseRealVisionInSimulation
            // 
            this._cbUseRealVisionInSimulation.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbUseRealVisionInSimulation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbUseRealVisionInSimulation.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbUseRealVisionInSimulation.Location = new System.Drawing.Point(182, 274);
            this._cbUseRealVisionInSimulation.Margin = new System.Windows.Forms.Padding(2);
            this._cbUseRealVisionInSimulation.Name = "_cbUseRealVisionInSimulation";
            this._cbUseRealVisionInSimulation.Size = new System.Drawing.Size(645, 23);
            this._cbUseRealVisionInSimulation.TabIndex = 17;
            this._cbUseRealVisionInSimulation.SelectedIndexChanged += new System.EventHandler(this._cbUseRealVisionInSimulation_SelectedIndexChanged);
            // 
            // _cbWaferCompleteRunMode
            // 
            this._cbWaferCompleteRunMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbWaferCompleteRunMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbWaferCompleteRunMode.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbWaferCompleteRunMode.Location = new System.Drawing.Point(182, 308);
            this._cbWaferCompleteRunMode.Margin = new System.Windows.Forms.Padding(2);
            this._cbWaferCompleteRunMode.Name = "_cbWaferCompleteRunMode";
            this._cbWaferCompleteRunMode.Size = new System.Drawing.Size(645, 23);
            this._cbWaferCompleteRunMode.TabIndex = 19;
            this._cbWaferCompleteRunMode.SelectedIndexChanged += new System.EventHandler(this._cbWaferCompleteRunMode_SelectedIndexChanged);
            //
            // lblPickRuntimeOffset
            //
            this.lblPickRuntimeOffset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblPickRuntimeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickRuntimeOffset.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPickRuntimeOffset.Location = new System.Drawing.Point(2, 342);
            this.lblPickRuntimeOffset.Margin = new System.Windows.Forms.Padding(2);
            this.lblPickRuntimeOffset.Name = "lblPickRuntimeOffset";
            this.lblPickRuntimeOffset.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPickRuntimeOffset.Size = new System.Drawing.Size(176, 30);
            this.lblPickRuntimeOffset.TabIndex = 20;
            this.lblPickRuntimeOffset.Text = "PICK RUNTIME OFFSET";
            this.lblPickRuntimeOffset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // pickRuntimeOffsetLayout
            //
            this.pickRuntimeOffsetLayout.ColumnCount = 2;
            this.pickRuntimeOffsetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pickRuntimeOffsetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.pickRuntimeOffsetLayout.Controls.Add(this._cbPickRuntimeOffset, 0, 0);
            this.pickRuntimeOffsetLayout.Controls.Add(this.btnResetPickRuntimeOffset, 1, 0);
            this.pickRuntimeOffsetLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pickRuntimeOffsetLayout.Location = new System.Drawing.Point(180, 340);
            this.pickRuntimeOffsetLayout.Margin = new System.Windows.Forms.Padding(0);
            this.pickRuntimeOffsetLayout.Name = "pickRuntimeOffsetLayout";
            this.pickRuntimeOffsetLayout.RowCount = 1;
            this.pickRuntimeOffsetLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pickRuntimeOffsetLayout.Size = new System.Drawing.Size(649, 34);
            this.pickRuntimeOffsetLayout.TabIndex = 21;
            //
            // _cbPickRuntimeOffset
            //
            this._cbPickRuntimeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbPickRuntimeOffset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbPickRuntimeOffset.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbPickRuntimeOffset.Location = new System.Drawing.Point(2, 2);
            this._cbPickRuntimeOffset.Margin = new System.Windows.Forms.Padding(2);
            this._cbPickRuntimeOffset.Name = "_cbPickRuntimeOffset";
            this._cbPickRuntimeOffset.Size = new System.Drawing.Size(535, 23);
            this._cbPickRuntimeOffset.TabIndex = 0;
            this._cbPickRuntimeOffset.SelectedIndexChanged += new System.EventHandler(this._cbPickRuntimeOffset_SelectedIndexChanged);
            //
            // btnResetPickRuntimeOffset
            //
            this.btnResetPickRuntimeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnResetPickRuntimeOffset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetPickRuntimeOffset.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnResetPickRuntimeOffset.Location = new System.Drawing.Point(541, 2);
            this.btnResetPickRuntimeOffset.Margin = new System.Windows.Forms.Padding(2);
            this.btnResetPickRuntimeOffset.Name = "btnResetPickRuntimeOffset";
            this.btnResetPickRuntimeOffset.Size = new System.Drawing.Size(106, 30);
            this.btnResetPickRuntimeOffset.TabIndex = 1;
            this.btnResetPickRuntimeOffset.Text = "RESET";
            this.btnResetPickRuntimeOffset.UseVisualStyleBackColor = true;
            this.btnResetPickRuntimeOffset.Click += new System.EventHandler(this.btnResetPickRuntimeOffset_Click);
            //
            // lblPlaceRuntimeOffset
            //
            this.lblPlaceRuntimeOffset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblPlaceRuntimeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceRuntimeOffset.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPlaceRuntimeOffset.Location = new System.Drawing.Point(2, 376);
            this.lblPlaceRuntimeOffset.Margin = new System.Windows.Forms.Padding(2);
            this.lblPlaceRuntimeOffset.Name = "lblPlaceRuntimeOffset";
            this.lblPlaceRuntimeOffset.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPlaceRuntimeOffset.Size = new System.Drawing.Size(176, 30);
            this.lblPlaceRuntimeOffset.TabIndex = 22;
            this.lblPlaceRuntimeOffset.Text = "PLACE RUNTIME OFFSET";
            this.lblPlaceRuntimeOffset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // placeRuntimeOffsetLayout
            //
            this.placeRuntimeOffsetLayout.ColumnCount = 2;
            this.placeRuntimeOffsetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.placeRuntimeOffsetLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.placeRuntimeOffsetLayout.Controls.Add(this._cbPlaceRuntimeOffset, 0, 0);
            this.placeRuntimeOffsetLayout.Controls.Add(this.btnResetPlaceRuntimeOffset, 1, 0);
            this.placeRuntimeOffsetLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.placeRuntimeOffsetLayout.Location = new System.Drawing.Point(180, 374);
            this.placeRuntimeOffsetLayout.Margin = new System.Windows.Forms.Padding(0);
            this.placeRuntimeOffsetLayout.Name = "placeRuntimeOffsetLayout";
            this.placeRuntimeOffsetLayout.RowCount = 1;
            this.placeRuntimeOffsetLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.placeRuntimeOffsetLayout.Size = new System.Drawing.Size(649, 34);
            this.placeRuntimeOffsetLayout.TabIndex = 23;
            //
            // _cbPlaceRuntimeOffset
            //
            this._cbPlaceRuntimeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbPlaceRuntimeOffset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbPlaceRuntimeOffset.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._cbPlaceRuntimeOffset.Location = new System.Drawing.Point(2, 2);
            this._cbPlaceRuntimeOffset.Margin = new System.Windows.Forms.Padding(2);
            this._cbPlaceRuntimeOffset.Name = "_cbPlaceRuntimeOffset";
            this._cbPlaceRuntimeOffset.Size = new System.Drawing.Size(535, 23);
            this._cbPlaceRuntimeOffset.TabIndex = 0;
            this._cbPlaceRuntimeOffset.SelectedIndexChanged += new System.EventHandler(this._cbPlaceRuntimeOffset_SelectedIndexChanged);
            //
            // btnResetPlaceRuntimeOffset
            //
            this.btnResetPlaceRuntimeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnResetPlaceRuntimeOffset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetPlaceRuntimeOffset.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnResetPlaceRuntimeOffset.Location = new System.Drawing.Point(541, 2);
            this.btnResetPlaceRuntimeOffset.Margin = new System.Windows.Forms.Padding(2);
            this.btnResetPlaceRuntimeOffset.Name = "btnResetPlaceRuntimeOffset";
            this.btnResetPlaceRuntimeOffset.Size = new System.Drawing.Size(106, 30);
            this.btnResetPlaceRuntimeOffset.TabIndex = 1;
            this.btnResetPlaceRuntimeOffset.Text = "RESET";
            this.btnResetPlaceRuntimeOffset.UseVisualStyleBackColor = true;
            this.btnResetPlaceRuntimeOffset.Click += new System.EventHandler(this.btnResetPlaceRuntimeOffset_Click);
            //
            // grpAjin
            //
            this.grpAjin.Controls.Add(this.ajinLayout);
            this.grpAjin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpAjin.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpAjin.Location = new System.Drawing.Point(8, 478);
            this.grpAjin.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpAjin.Name = "grpAjin";
            this.grpAjin.Padding = new System.Windows.Forms.Padding(8);
            this.grpAjin.Size = new System.Drawing.Size(831, 70);
            this.grpAjin.TabIndex = 2;
            this.grpAjin.TabStop = false;
            this.grpAjin.Text = "AJINEXTEK";
            // 
            // ajinLayout
            // 
            this.ajinLayout.ColumnCount = 3;
            this.ajinLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.ajinLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.ajinLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.ajinLayout.Controls.Add(this._cbAjin, 0, 0);
            this.ajinLayout.Controls.Add(this.lblIrq, 1, 0);
            this.ajinLayout.Controls.Add(this._tbIrq, 2, 0);
            this.ajinLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.ajinLayout.Location = new System.Drawing.Point(8, 26);
            this.ajinLayout.Name = "ajinLayout";
            this.ajinLayout.RowCount = 1;
            this.ajinLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.ajinLayout.Size = new System.Drawing.Size(815, 36);
            this.ajinLayout.TabIndex = 0;
            // 
            // _cbAjin
            // 
            this._cbAjin.AutoSize = true;
            this._cbAjin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbAjin.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._cbAjin.Location = new System.Drawing.Point(3, 3);
            this._cbAjin.Name = "_cbAjin";
            this._cbAjin.Size = new System.Drawing.Size(174, 30);
            this._cbAjin.TabIndex = 0;
            this._cbAjin.Text = "UseAjin (AXL.dll)";
            this._cbAjin.UseVisualStyleBackColor = true;
            this._cbAjin.CheckedChanged += new System.EventHandler(this._cbAjin_CheckedChanged);
            // 
            // lblIrq
            // 
            this.lblIrq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIrq.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblIrq.Location = new System.Drawing.Point(183, 0);
            this.lblIrq.Name = "lblIrq";
            this.lblIrq.Size = new System.Drawing.Size(74, 36);
            this.lblIrq.TabIndex = 1;
            this.lblIrq.Text = "IRQ NO.";
            this.lblIrq.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbIrq
            // 
            this._tbIrq.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbIrq.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._tbIrq.Location = new System.Drawing.Point(263, 3);
            this._tbIrq.Name = "_tbIrq";
            this._tbIrq.Size = new System.Drawing.Size(549, 23);
            this._tbIrq.TabIndex = 2;
            this._tbIrq.TextChanged += new System.EventHandler(this._tbIrq_TextChanged);
            // 
            // logBtnLayout
            // 
            this.logBtnLayout.ColumnCount = 2;
            this.logBtnLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.logBtnLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.logBtnLayout.Controls.Add(this.btnLogSettings, 1, 0);
            this.logBtnLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logBtnLayout.Location = new System.Drawing.Point(8, 556);
            this.logBtnLayout.Margin = new System.Windows.Forms.Padding(0);
            this.logBtnLayout.Name = "logBtnLayout";
            this.logBtnLayout.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.logBtnLayout.RowCount = 1;
            this.logBtnLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.logBtnLayout.Size = new System.Drawing.Size(831, 40);
            this.logBtnLayout.TabIndex = 3;
            // 
            // btnLogSettings
            // 
            this.btnLogSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLogSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogSettings.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnLogSettings.Location = new System.Drawing.Point(415, 3);
            this.btnLogSettings.Margin = new System.Windows.Forms.Padding(0);
            this.btnLogSettings.Name = "btnLogSettings";
            this.btnLogSettings.Size = new System.Drawing.Size(416, 37);
            this.btnLogSettings.TabIndex = 0;
            this.btnLogSettings.Text = "LOG SETTINGS";
            this.btnLogSettings.UseVisualStyleBackColor = true;
            this.btnLogSettings.Click += new System.EventHandler(this.btnLogSettings_Click);
            // 
            // GeneralPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "GeneralPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.grpSetting.ResumeLayout(false);
            this.settingLayout.ResumeLayout(false);
            this.bodyLayout.ResumeLayout(false);
            this.pickRuntimeOffsetLayout.ResumeLayout(false);
            this.placeRuntimeOffsetLayout.ResumeLayout(false);
            this.grpAjin.ResumeLayout(false);
            this.ajinLayout.ResumeLayout(false);
            this.ajinLayout.PerformLayout();
            this.logBtnLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
