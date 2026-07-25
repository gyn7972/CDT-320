namespace QMC.CDT_320.Ui.Dialogs
{
    partial class ManualSequenceDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Button btnInputLoad;
        private System.Windows.Forms.Button btnInputUnload;
        private System.Windows.Forms.Button btnOutputLoad;
        private System.Windows.Forms.Button btnOutputUnload;
        private System.Windows.Forms.Panel pickerSelectPanel;
        private System.Windows.Forms.RadioButton rbFrontPicker;
        private System.Windows.Forms.RadioButton rbRearPicker;
        private System.Windows.Forms.Label lblPickerNo;
        private System.Windows.Forms.ComboBox cmbPickerNo;
        private System.Windows.Forms.Button btnPickUp;
        private System.Windows.Forms.Button btnBottom;
        private System.Windows.Forms.Button btnSide;
        private System.Windows.Forms.Button btnPlace;
        private System.Windows.Forms.Button btnPickUpZTest;
        private System.Windows.Forms.Button btnAllStep;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Panel outputSidePanel;
        private System.Windows.Forms.Label lblOutputSide;
        private System.Windows.Forms.RadioButton rbOutputGood;
        private System.Windows.Forms.RadioButton rbOutputNg;
        private System.Windows.Forms.Panel speedPanel;
        private System.Windows.Forms.Label lblSpeedPercent;
        private System.Windows.Forms.NumericUpDown numSpeedPercent;
        private System.Windows.Forms.Label lblReadySpeedPercent;
        private System.Windows.Forms.NumericUpDown numReadySpeedPercent;
        private System.Windows.Forms.Panel loadTargetPanel;
        private System.Windows.Forms.Label lblInputLoadTarget;
        private System.Windows.Forms.ComboBox cmbInputLoadTarget;
        private System.Windows.Forms.Label lblOutputLoadTarget;
        private System.Windows.Forms.ComboBox cmbOutputLoadTarget;
        private System.Windows.Forms.Button btnRefreshLoadTargets;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnInputLoad = new System.Windows.Forms.Button();
            this.btnInputUnload = new System.Windows.Forms.Button();
            this.btnOutputLoad = new System.Windows.Forms.Button();
            this.btnOutputUnload = new System.Windows.Forms.Button();
            this.pickerSelectPanel = new System.Windows.Forms.Panel();
            this.outputSidePanel = new System.Windows.Forms.Panel();
            this.rbOutputNg = new System.Windows.Forms.RadioButton();
            this.rbOutputGood = new System.Windows.Forms.RadioButton();
            this.lblOutputSide = new System.Windows.Forms.Label();
            this.cmbPickerNo = new System.Windows.Forms.ComboBox();
            this.lblPickerNo = new System.Windows.Forms.Label();
            this.rbRearPicker = new System.Windows.Forms.RadioButton();
            this.rbFrontPicker = new System.Windows.Forms.RadioButton();
            this.btnPickUp = new System.Windows.Forms.Button();
            this.btnBottom = new System.Windows.Forms.Button();
            this.btnSide = new System.Windows.Forms.Button();
            this.btnPlace = new System.Windows.Forms.Button();
            this.btnPickUpZTest = new System.Windows.Forms.Button();
            this.btnAllStep = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.speedPanel = new System.Windows.Forms.Panel();
            this.numSpeedPercent = new System.Windows.Forms.NumericUpDown();
            this.lblSpeedPercent = new System.Windows.Forms.Label();
            this.numReadySpeedPercent = new System.Windows.Forms.NumericUpDown();
            this.lblReadySpeedPercent = new System.Windows.Forms.Label();
            this.loadTargetPanel = new System.Windows.Forms.Panel();
            this.lblInputLoadTarget = new System.Windows.Forms.Label();
            this.cmbInputLoadTarget = new System.Windows.Forms.ComboBox();
            this.lblOutputLoadTarget = new System.Windows.Forms.Label();
            this.cmbOutputLoadTarget = new System.Windows.Forms.ComboBox();
            this.btnRefreshLoadTargets = new System.Windows.Forms.Button();
            this.mainLayout.SuspendLayout();
            this.pickerSelectPanel.SuspendLayout();
            this.outputSidePanel.SuspendLayout();
            this.speedPanel.SuspendLayout();
            this.loadTargetPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSpeedPercent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numReadySpeedPercent)).BeginInit();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(115)))), ((int)(((byte)(0)))));
            this.titleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.titleLabel.Font = new System.Drawing.Font("맑은 고딕", 13F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(0, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.titleLabel.Size = new System.Drawing.Size(846, 42);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "MANUAL SEQUENCE";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 4;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainLayout.Controls.Add(this.btnInputLoad, 0, 0);
            this.mainLayout.Controls.Add(this.btnInputUnload, 1, 0);
            this.mainLayout.Controls.Add(this.btnOutputLoad, 2, 0);
            this.mainLayout.Controls.Add(this.btnOutputUnload, 3, 0);
            this.mainLayout.Controls.Add(this.pickerSelectPanel, 0, 1);
            this.mainLayout.Controls.Add(this.btnPickUp, 0, 2);
            this.mainLayout.Controls.Add(this.btnBottom, 1, 2);
            this.mainLayout.Controls.Add(this.btnSide, 2, 2);
            this.mainLayout.Controls.Add(this.btnPlace, 3, 2);
            this.mainLayout.Controls.Add(this.btnPickUpZTest, 0, 3);
            this.mainLayout.Controls.Add(this.btnAllStep, 0, 4);
            this.mainLayout.Controls.Add(this.btnClose, 2, 4);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.mainLayout.Location = new System.Drawing.Point(0, 42);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(16);
            this.mainLayout.RowCount = 5;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.mainLayout.Size = new System.Drawing.Size(846, 317);
            this.mainLayout.TabIndex = 1;
            // 
            // btnInputLoad
            // 
            this.btnInputLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInputLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInputLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInputLoad.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnInputLoad.ForeColor = System.Drawing.Color.White;
            this.btnInputLoad.Location = new System.Drawing.Point(21, 21);
            this.btnInputLoad.Margin = new System.Windows.Forms.Padding(5);
            this.btnInputLoad.Name = "btnInputLoad";
            this.btnInputLoad.Size = new System.Drawing.Size(193, 47);
            this.btnInputLoad.TabIndex = 0;
            this.btnInputLoad.Text = "INPUT LOAD";
            this.btnInputLoad.UseVisualStyleBackColor = false;
            // 
            // btnInputUnload
            // 
            this.btnInputUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInputUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInputUnload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInputUnload.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnInputUnload.ForeColor = System.Drawing.Color.White;
            this.btnInputUnload.Location = new System.Drawing.Point(224, 21);
            this.btnInputUnload.Margin = new System.Windows.Forms.Padding(5);
            this.btnInputUnload.Name = "btnInputUnload";
            this.btnInputUnload.Size = new System.Drawing.Size(193, 47);
            this.btnInputUnload.TabIndex = 1;
            this.btnInputUnload.Text = "INPUT UNLOAD";
            this.btnInputUnload.UseVisualStyleBackColor = false;
            // 
            // btnOutputLoad
            // 
            this.btnOutputLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnOutputLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOutputLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOutputLoad.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOutputLoad.ForeColor = System.Drawing.Color.White;
            this.btnOutputLoad.Location = new System.Drawing.Point(427, 21);
            this.btnOutputLoad.Margin = new System.Windows.Forms.Padding(5);
            this.btnOutputLoad.Name = "btnOutputLoad";
            this.btnOutputLoad.Size = new System.Drawing.Size(193, 47);
            this.btnOutputLoad.TabIndex = 2;
            this.btnOutputLoad.Text = "OUTPUT LOAD";
            this.btnOutputLoad.UseVisualStyleBackColor = false;
            // 
            // btnOutputUnload
            // 
            this.btnOutputUnload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnOutputUnload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOutputUnload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOutputUnload.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOutputUnload.ForeColor = System.Drawing.Color.White;
            this.btnOutputUnload.Location = new System.Drawing.Point(630, 21);
            this.btnOutputUnload.Margin = new System.Windows.Forms.Padding(5);
            this.btnOutputUnload.Name = "btnOutputUnload";
            this.btnOutputUnload.Size = new System.Drawing.Size(195, 47);
            this.btnOutputUnload.TabIndex = 3;
            this.btnOutputUnload.Text = "OUTPUT UNLOAD";
            this.btnOutputUnload.UseVisualStyleBackColor = false;
            // 
            // pickerSelectPanel
            // 
            this.mainLayout.SetColumnSpan(this.pickerSelectPanel, 4);
            this.pickerSelectPanel.Controls.Add(this.outputSidePanel);
            this.pickerSelectPanel.Controls.Add(this.cmbPickerNo);
            this.pickerSelectPanel.Controls.Add(this.lblPickerNo);
            this.pickerSelectPanel.Controls.Add(this.rbRearPicker);
            this.pickerSelectPanel.Controls.Add(this.rbFrontPicker);
            this.pickerSelectPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pickerSelectPanel.Location = new System.Drawing.Point(21, 78);
            this.pickerSelectPanel.Margin = new System.Windows.Forms.Padding(5);
            this.pickerSelectPanel.Name = "pickerSelectPanel";
            this.pickerSelectPanel.Size = new System.Drawing.Size(804, 47);
            this.pickerSelectPanel.TabIndex = 4;
            // 
            // outputSidePanel
            // 
            this.outputSidePanel.Controls.Add(this.rbOutputNg);
            this.outputSidePanel.Controls.Add(this.rbOutputGood);
            this.outputSidePanel.Controls.Add(this.lblOutputSide);
            this.outputSidePanel.Location = new System.Drawing.Point(520, 0);
            this.outputSidePanel.Name = "outputSidePanel";
            this.outputSidePanel.Size = new System.Drawing.Size(284, 47);
            this.outputSidePanel.TabIndex = 4;
            // 
            // rbOutputNg
            // 
            // 라디오 대신 토글 버튼 형태로 표시한다(상호배타 동작은 RadioButton 그대로 유지).
            this.rbOutputNg.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbOutputNg.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.rbOutputNg.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rbOutputNg.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.rbOutputNg.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.rbOutputNg.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbOutputNg.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.rbOutputNg.ForeColor = System.Drawing.Color.Black;
            this.rbOutputNg.Location = new System.Drawing.Point(186, 6);
            this.rbOutputNg.Name = "rbOutputNg";
            this.rbOutputNg.Size = new System.Drawing.Size(88, 35);
            this.rbOutputNg.TabIndex = 2;
            this.rbOutputNg.Text = "NG";
            this.rbOutputNg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbOutputNg.UseVisualStyleBackColor = false;
            // 
            // rbOutputGood
            // 
            // 라디오 대신 토글 버튼 형태로 표시한다(상호배타 동작은 RadioButton 그대로 유지).
            this.rbOutputGood.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbOutputGood.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.rbOutputGood.Checked = true;
            this.rbOutputGood.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rbOutputGood.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(160)))), ((int)(((byte)(160)))));
            this.rbOutputGood.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(140)))), ((int)(((byte)(70)))));
            this.rbOutputGood.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbOutputGood.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.rbOutputGood.ForeColor = System.Drawing.Color.Black;
            this.rbOutputGood.Location = new System.Drawing.Point(92, 6);
            this.rbOutputGood.Name = "rbOutputGood";
            this.rbOutputGood.Size = new System.Drawing.Size(88, 35);
            this.rbOutputGood.TabIndex = 1;
            this.rbOutputGood.TabStop = true;
            this.rbOutputGood.Text = "GOOD";
            this.rbOutputGood.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rbOutputGood.UseVisualStyleBackColor = false;
            // 
            // lblOutputSide
            // 
            this.lblOutputSide.ForeColor = System.Drawing.Color.Black;
            this.lblOutputSide.Location = new System.Drawing.Point(0, 0);
            this.lblOutputSide.Name = "lblOutputSide";
            this.lblOutputSide.Size = new System.Drawing.Size(80, 47);
            this.lblOutputSide.TabIndex = 0;
            this.lblOutputSide.Text = "Output";
            this.lblOutputSide.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbPickerNo
            // 
            this.cmbPickerNo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPickerNo.Enabled = false;
            this.cmbPickerNo.FormattingEnabled = true;
            this.cmbPickerNo.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4"});
            this.cmbPickerNo.Location = new System.Drawing.Point(398, 10);
            this.cmbPickerNo.Name = "cmbPickerNo";
            this.cmbPickerNo.Size = new System.Drawing.Size(84, 25);
            this.cmbPickerNo.TabIndex = 3;
            this.cmbPickerNo.Visible = false;
            // 
            // lblPickerNo
            // 
            this.lblPickerNo.Enabled = false;
            this.lblPickerNo.ForeColor = System.Drawing.Color.Black;
            this.lblPickerNo.Location = new System.Drawing.Point(302, 0);
            this.lblPickerNo.Name = "lblPickerNo";
            this.lblPickerNo.Size = new System.Drawing.Size(90, 47);
            this.lblPickerNo.TabIndex = 2;
            this.lblPickerNo.Text = "Picker No";
            this.lblPickerNo.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblPickerNo.Visible = false;
            // 
            // rbRearPicker
            // 
            this.rbRearPicker.Dock = System.Windows.Forms.DockStyle.Left;
            this.rbRearPicker.Enabled = false;
            this.rbRearPicker.ForeColor = System.Drawing.Color.Black;
            this.rbRearPicker.Location = new System.Drawing.Point(140, 0);
            this.rbRearPicker.Name = "rbRearPicker";
            this.rbRearPicker.Size = new System.Drawing.Size(140, 47);
            this.rbRearPicker.TabIndex = 1;
            this.rbRearPicker.Text = "RearPicker";
            this.rbRearPicker.UseVisualStyleBackColor = true;
            this.rbRearPicker.Visible = false;
            // 
            // rbFrontPicker
            // 
            this.rbFrontPicker.Checked = true;
            this.rbFrontPicker.Dock = System.Windows.Forms.DockStyle.Left;
            this.rbFrontPicker.Enabled = false;
            this.rbFrontPicker.ForeColor = System.Drawing.Color.Black;
            this.rbFrontPicker.Location = new System.Drawing.Point(0, 0);
            this.rbFrontPicker.Name = "rbFrontPicker";
            this.rbFrontPicker.Size = new System.Drawing.Size(140, 47);
            this.rbFrontPicker.TabIndex = 0;
            this.rbFrontPicker.TabStop = true;
            this.rbFrontPicker.Text = "FrontPicker";
            this.rbFrontPicker.UseVisualStyleBackColor = true;
            this.rbFrontPicker.Visible = false;
            // 
            // btnPickUp
            // 
            this.btnPickUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPickUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickUp.Enabled = false;
            this.btnPickUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPickUp.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPickUp.ForeColor = System.Drawing.Color.White;
            this.btnPickUp.Location = new System.Drawing.Point(21, 135);
            this.btnPickUp.Margin = new System.Windows.Forms.Padding(5);
            this.btnPickUp.Name = "btnPickUp";
            this.btnPickUp.Size = new System.Drawing.Size(193, 47);
            this.btnPickUp.TabIndex = 5;
            this.btnPickUp.Text = "PICK UP";
            this.btnPickUp.UseVisualStyleBackColor = false;
            this.btnPickUp.Visible = false;
            // 
            // btnBottom
            // 
            this.btnBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBottom.Enabled = false;
            this.btnBottom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBottom.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnBottom.ForeColor = System.Drawing.Color.White;
            this.btnBottom.Location = new System.Drawing.Point(224, 135);
            this.btnBottom.Margin = new System.Windows.Forms.Padding(5);
            this.btnBottom.Name = "btnBottom";
            this.btnBottom.Size = new System.Drawing.Size(193, 47);
            this.btnBottom.TabIndex = 6;
            this.btnBottom.Text = "BOTTOM";
            this.btnBottom.UseVisualStyleBackColor = false;
            this.btnBottom.Visible = false;
            // 
            // btnSide
            // 
            this.btnSide.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSide.Enabled = false;
            this.btnSide.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSide.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSide.ForeColor = System.Drawing.Color.White;
            this.btnSide.Location = new System.Drawing.Point(427, 135);
            this.btnSide.Margin = new System.Windows.Forms.Padding(5);
            this.btnSide.Name = "btnSide";
            this.btnSide.Size = new System.Drawing.Size(193, 47);
            this.btnSide.TabIndex = 7;
            this.btnSide.Text = "SIDE";
            this.btnSide.UseVisualStyleBackColor = false;
            this.btnSide.Visible = false;
            // 
            // btnPlace
            // 
            this.btnPlace.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPlace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlace.Enabled = false;
            this.btnPlace.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlace.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPlace.ForeColor = System.Drawing.Color.White;
            this.btnPlace.Location = new System.Drawing.Point(630, 135);
            this.btnPlace.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlace.Name = "btnPlace";
            this.btnPlace.Size = new System.Drawing.Size(195, 47);
            this.btnPlace.TabIndex = 8;
            this.btnPlace.Text = "PLACE";
            this.btnPlace.UseVisualStyleBackColor = false;
            this.btnPlace.Visible = false;
            // 
            // btnPickUpZTest
            // 
            this.btnPickUpZTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.mainLayout.SetColumnSpan(this.btnPickUpZTest, 4);
            this.btnPickUpZTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPickUpZTest.Enabled = false;
            this.btnPickUpZTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPickUpZTest.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPickUpZTest.ForeColor = System.Drawing.Color.White;
            this.btnPickUpZTest.Location = new System.Drawing.Point(21, 192);
            this.btnPickUpZTest.Margin = new System.Windows.Forms.Padding(5);
            this.btnPickUpZTest.Name = "btnPickUpZTest";
            this.btnPickUpZTest.Size = new System.Drawing.Size(804, 47);
            this.btnPickUpZTest.TabIndex = 9;
            this.btnPickUpZTest.Text = "PICK Z TEST";
            this.btnPickUpZTest.UseVisualStyleBackColor = false;
            this.btnPickUpZTest.Visible = false;
            // 
            // btnAllStep
            // 
            this.btnAllStep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.mainLayout.SetColumnSpan(this.btnAllStep, 2);
            this.btnAllStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAllStep.Enabled = false;
            this.btnAllStep.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAllStep.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnAllStep.ForeColor = System.Drawing.Color.White;
            this.btnAllStep.Location = new System.Drawing.Point(21, 249);
            this.btnAllStep.Margin = new System.Windows.Forms.Padding(5);
            this.btnAllStep.Name = "btnAllStep";
            this.btnAllStep.Size = new System.Drawing.Size(396, 47);
            this.btnAllStep.TabIndex = 10;
            this.btnAllStep.Text = "ALL STEP";
            this.btnAllStep.UseVisualStyleBackColor = false;
            this.btnAllStep.Visible = false;
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.mainLayout.SetColumnSpan(this.btnClose, 2);
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(427, 249);
            this.btnClose.Margin = new System.Windows.Forms.Padding(5);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(398, 47);
            this.btnClose.TabIndex = 11;
            this.btnClose.Text = "닫기";
            this.btnClose.UseVisualStyleBackColor = false;
            //
            // loadTargetPanel  (LOAD 대상 Wafer/Bin 선택 — UNLOAD는 원본 슬롯 고정이라 선택 대상이 없다)
            //
            this.loadTargetPanel.Controls.Add(this.btnRefreshLoadTargets);
            this.loadTargetPanel.Controls.Add(this.cmbOutputLoadTarget);
            this.loadTargetPanel.Controls.Add(this.lblOutputLoadTarget);
            this.loadTargetPanel.Controls.Add(this.cmbInputLoadTarget);
            this.loadTargetPanel.Controls.Add(this.lblInputLoadTarget);
            this.loadTargetPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.loadTargetPanel.Location = new System.Drawing.Point(0, 359);
            this.loadTargetPanel.Name = "loadTargetPanel";
            this.loadTargetPanel.Size = new System.Drawing.Size(846, 46);
            this.loadTargetPanel.TabIndex = 2;
            //
            // lblInputLoadTarget
            //
            this.lblInputLoadTarget.ForeColor = System.Drawing.Color.Black;
            this.lblInputLoadTarget.Location = new System.Drawing.Point(18, 0);
            this.lblInputLoadTarget.Name = "lblInputLoadTarget";
            this.lblInputLoadTarget.Size = new System.Drawing.Size(120, 46);
            this.lblInputLoadTarget.TabIndex = 0;
            this.lblInputLoadTarget.Text = "INPUT LOAD 대상";
            this.lblInputLoadTarget.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbInputLoadTarget
            //
            this.cmbInputLoadTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbInputLoadTarget.FormattingEnabled = true;
            this.cmbInputLoadTarget.Location = new System.Drawing.Point(140, 10);
            this.cmbInputLoadTarget.Name = "cmbInputLoadTarget";
            this.cmbInputLoadTarget.Size = new System.Drawing.Size(240, 25);
            this.cmbInputLoadTarget.TabIndex = 1;
            //
            // lblOutputLoadTarget
            //
            this.lblOutputLoadTarget.ForeColor = System.Drawing.Color.Black;
            this.lblOutputLoadTarget.Location = new System.Drawing.Point(396, 0);
            this.lblOutputLoadTarget.Name = "lblOutputLoadTarget";
            this.lblOutputLoadTarget.Size = new System.Drawing.Size(130, 46);
            this.lblOutputLoadTarget.TabIndex = 2;
            this.lblOutputLoadTarget.Text = "OUTPUT LOAD 대상";
            this.lblOutputLoadTarget.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbOutputLoadTarget
            //
            this.cmbOutputLoadTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOutputLoadTarget.FormattingEnabled = true;
            this.cmbOutputLoadTarget.Location = new System.Drawing.Point(528, 10);
            this.cmbOutputLoadTarget.Name = "cmbOutputLoadTarget";
            this.cmbOutputLoadTarget.Size = new System.Drawing.Size(240, 25);
            this.cmbOutputLoadTarget.TabIndex = 3;
            //
            // btnRefreshLoadTargets
            //
            this.btnRefreshLoadTargets.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnRefreshLoadTargets.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefreshLoadTargets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefreshLoadTargets.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnRefreshLoadTargets.ForeColor = System.Drawing.Color.White;
            this.btnRefreshLoadTargets.Location = new System.Drawing.Point(778, 9);
            this.btnRefreshLoadTargets.Name = "btnRefreshLoadTargets";
            this.btnRefreshLoadTargets.Size = new System.Drawing.Size(50, 28);
            this.btnRefreshLoadTargets.TabIndex = 4;
            this.btnRefreshLoadTargets.Text = "갱신";
            this.btnRefreshLoadTargets.UseVisualStyleBackColor = false;
            //
            // statusLabel
            //
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLabel.Location = new System.Drawing.Point(0, 359);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Padding = new System.Windows.Forms.Padding(18, 10, 18, 0);
            this.statusLabel.Size = new System.Drawing.Size(846, 54);
            this.statusLabel.TabIndex = 2;
            this.statusLabel.Text = "Auto와 동일한 Material/Die Map/Picker 상태를 사용합니다.";
            // 
            // speedPanel
            // 
            this.speedPanel.Controls.Add(this.numReadySpeedPercent);
            this.speedPanel.Controls.Add(this.lblReadySpeedPercent);
            this.speedPanel.Controls.Add(this.numSpeedPercent);
            this.speedPanel.Controls.Add(this.lblSpeedPercent);
            this.speedPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.speedPanel.Location = new System.Drawing.Point(0, 413);
            this.speedPanel.Name = "speedPanel";
            this.speedPanel.Size = new System.Drawing.Size(846, 46);
            this.speedPanel.TabIndex = 3;
            // 
            // numSpeedPercent
            // 
            this.numSpeedPercent.Location = new System.Drawing.Point(248, 10);
            this.numSpeedPercent.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numSpeedPercent.Name = "numSpeedPercent";
            this.numSpeedPercent.Size = new System.Drawing.Size(90, 25);
            this.numSpeedPercent.TabIndex = 1;
            this.numSpeedPercent.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numSpeedPercent.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lblSpeedPercent
            // 
            this.lblSpeedPercent.ForeColor = System.Drawing.Color.Black;
            this.lblSpeedPercent.Location = new System.Drawing.Point(18, 0);
            this.lblSpeedPercent.Name = "lblSpeedPercent";
            this.lblSpeedPercent.Size = new System.Drawing.Size(230, 46);
            this.lblSpeedPercent.TabIndex = 0;
            this.lblSpeedPercent.Text = "Manual 속도 (%)";
            this.lblSpeedPercent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblReadySpeedPercent
            //
            this.lblReadySpeedPercent.ForeColor = System.Drawing.Color.Black;
            this.lblReadySpeedPercent.Location = new System.Drawing.Point(452, 0);
            this.lblReadySpeedPercent.Name = "lblReadySpeedPercent";
            this.lblReadySpeedPercent.Size = new System.Drawing.Size(160, 46);
            this.lblReadySpeedPercent.TabIndex = 2;
            this.lblReadySpeedPercent.Text = "Ready 속도 (%)";
            this.lblReadySpeedPercent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numReadySpeedPercent
            //
            this.numReadySpeedPercent.Location = new System.Drawing.Point(612, 10);
            this.numReadySpeedPercent.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numReadySpeedPercent.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numReadySpeedPercent.Name = "numReadySpeedPercent";
            this.numReadySpeedPercent.Size = new System.Drawing.Size(90, 25);
            this.numReadySpeedPercent.TabIndex = 3;
            this.numReadySpeedPercent.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numReadySpeedPercent.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // ManualSequenceDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this.ClientSize = new System.Drawing.Size(846, 505);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.speedPanel);
            this.Controls.Add(this.loadTargetPanel);
            this.Controls.Add(this.mainLayout);
            this.Controls.Add(this.titleLabel);
            this.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManualSequenceDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manual Sequence";
            this.mainLayout.ResumeLayout(false);
            this.pickerSelectPanel.ResumeLayout(false);
            this.outputSidePanel.ResumeLayout(false);
            this.speedPanel.ResumeLayout(false);
            this.loadTargetPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numSpeedPercent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numReadySpeedPercent)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
