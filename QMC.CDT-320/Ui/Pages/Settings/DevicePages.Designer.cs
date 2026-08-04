namespace QMC.CDT_320.Ui.Pages.Settings
{
    partial class BarcodeReaderPage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel optionLayout;
        private System.Windows.Forms.Label lblPortTitle;
        private System.Windows.Forms.Label lblPortValue;
        private System.Windows.Forms.Label lblBaudTitle;
        private System.Windows.Forms.Label lblBaudValue;
        private System.Windows.Forms.Label lblDataBitsTitle;
        private System.Windows.Forms.Label lblDataBitsValue;
        private System.Windows.Forms.Label lblParityTitle;
        private System.Windows.Forms.Label lblParityValue;
        private System.Windows.Forms.Label lblStopBitsTitle;
        private System.Windows.Forms.Label lblStopBitsValue;
        private System.Windows.Forms.Label lblHeadCharTitle;
        private System.Windows.Forms.Label lblHeadCharValue;
        private System.Windows.Forms.Label lblTailCharTitle;
        private System.Windows.Forms.Label lblTailCharValue;
        private System.Windows.Forms.Label lblTimeoutTitle;
        private System.Windows.Forms.Label lblTimeoutValue;
        private System.Windows.Forms.Label lblRetryTitle;
        private System.Windows.Forms.Label lblRetryValue;
        private QMC.CDT_320.Ui.Controls.ActionButton btnConnect;
        private QMC.CDT_320.Ui.Controls.ActionButton btnTestRead;
        private System.Windows.Forms.Label lblLastResult;
        private System.Windows.Forms.TableLayoutPanel channelLayout;
        private System.Windows.Forms.GroupBox grpInputBarcode;
        private System.Windows.Forms.GroupBox grpOutputBarcode;
        private System.Windows.Forms.TableLayoutPanel inputBarcodeLayout;
        private System.Windows.Forms.TableLayoutPanel outputBarcodeLayout;
        private System.Windows.Forms.TableLayoutPanel inputButtonLayout;
        private System.Windows.Forms.TableLayoutPanel outputButtonLayout;
        private System.Windows.Forms.CheckBox chkInputUse;
        private System.Windows.Forms.CheckBox chkOutputUse;
        private System.Windows.Forms.Label lblInputPort;
        private System.Windows.Forms.Label lblOutputPort;
        private System.Windows.Forms.Label lblInputBaud;
        private System.Windows.Forms.Label lblOutputBaud;
        private System.Windows.Forms.Label lblInputTimeout;
        private System.Windows.Forms.Label lblOutputTimeout;
        private System.Windows.Forms.Label lblInputRetry;
        private System.Windows.Forms.Label lblOutputRetry;
        private System.Windows.Forms.Label lblInputRetryStep;
        private System.Windows.Forms.Label lblOutputRetryStep;
        private System.Windows.Forms.Label lblInputTrigger;
        private System.Windows.Forms.Label lblOutputTrigger;
        private System.Windows.Forms.NumericUpDown numInputPort;
        private System.Windows.Forms.NumericUpDown numOutputPort;
        private System.Windows.Forms.NumericUpDown numInputBaud;
        private System.Windows.Forms.NumericUpDown numOutputBaud;
        private System.Windows.Forms.NumericUpDown numInputTimeout;
        private System.Windows.Forms.NumericUpDown numOutputTimeout;
        private System.Windows.Forms.NumericUpDown numInputRetry;
        private System.Windows.Forms.NumericUpDown numOutputRetry;
        private System.Windows.Forms.NumericUpDown numInputRetryStep;
        private System.Windows.Forms.NumericUpDown numOutputRetryStep;
        private System.Windows.Forms.TextBox txtInputTrigger;
        private System.Windows.Forms.TextBox txtOutputTrigger;
        private QMC.CDT_320.Ui.Controls.ActionButton btnInputConnect;
        private QMC.CDT_320.Ui.Controls.ActionButton btnOutputConnect;
        private QMC.CDT_320.Ui.Controls.ActionButton btnInputTest;
        private QMC.CDT_320.Ui.Controls.ActionButton btnOutputTest;
        private QMC.CDT_320.Ui.Controls.ActionButton btnSaveBarcode;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.channelLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpInputBarcode = new System.Windows.Forms.GroupBox();
            this.inputBarcodeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.chkInputUse = new System.Windows.Forms.CheckBox();
            this.lblInputPort = new System.Windows.Forms.Label();
            this.numInputPort = new System.Windows.Forms.NumericUpDown();
            this.lblInputBaud = new System.Windows.Forms.Label();
            this.numInputBaud = new System.Windows.Forms.NumericUpDown();
            this.lblInputTimeout = new System.Windows.Forms.Label();
            this.numInputTimeout = new System.Windows.Forms.NumericUpDown();
            this.lblInputRetry = new System.Windows.Forms.Label();
            this.numInputRetry = new System.Windows.Forms.NumericUpDown();
            this.lblInputRetryStep = new System.Windows.Forms.Label();
            this.numInputRetryStep = new System.Windows.Forms.NumericUpDown();
            this.lblInputTrigger = new System.Windows.Forms.Label();
            this.txtInputTrigger = new System.Windows.Forms.TextBox();
            this.inputButtonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnInputConnect = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnInputTest = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.grpOutputBarcode = new System.Windows.Forms.GroupBox();
            this.outputBarcodeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.chkOutputUse = new System.Windows.Forms.CheckBox();
            this.lblOutputPort = new System.Windows.Forms.Label();
            this.numOutputPort = new System.Windows.Forms.NumericUpDown();
            this.lblOutputBaud = new System.Windows.Forms.Label();
            this.numOutputBaud = new System.Windows.Forms.NumericUpDown();
            this.lblOutputTimeout = new System.Windows.Forms.Label();
            this.numOutputTimeout = new System.Windows.Forms.NumericUpDown();
            this.lblOutputRetry = new System.Windows.Forms.Label();
            this.numOutputRetry = new System.Windows.Forms.NumericUpDown();
            this.lblOutputRetryStep = new System.Windows.Forms.Label();
            this.numOutputRetryStep = new System.Windows.Forms.NumericUpDown();
            this.lblOutputTrigger = new System.Windows.Forms.Label();
            this.txtOutputTrigger = new System.Windows.Forms.TextBox();
            this.outputButtonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnOutputConnect = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnOutputTest = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnSaveBarcode = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.lblLastResult = new System.Windows.Forms.Label();
            this.optionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPortTitle = new System.Windows.Forms.Label();
            this.lblPortValue = new System.Windows.Forms.Label();
            this.lblBaudTitle = new System.Windows.Forms.Label();
            this.lblBaudValue = new System.Windows.Forms.Label();
            this.lblDataBitsTitle = new System.Windows.Forms.Label();
            this.lblDataBitsValue = new System.Windows.Forms.Label();
            this.lblParityTitle = new System.Windows.Forms.Label();
            this.lblParityValue = new System.Windows.Forms.Label();
            this.lblStopBitsTitle = new System.Windows.Forms.Label();
            this.lblStopBitsValue = new System.Windows.Forms.Label();
            this.lblHeadCharTitle = new System.Windows.Forms.Label();
            this.lblHeadCharValue = new System.Windows.Forms.Label();
            this.lblTailCharTitle = new System.Windows.Forms.Label();
            this.lblTailCharValue = new System.Windows.Forms.Label();
            this.lblTimeoutTitle = new System.Windows.Forms.Label();
            this.lblTimeoutValue = new System.Windows.Forms.Label();
            this.lblRetryTitle = new System.Windows.Forms.Label();
            this.lblRetryValue = new System.Windows.Forms.Label();
            this.btnConnect = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.btnTestRead = new QMC.CDT_320.Ui.Controls.ActionButton();
            this.rootLayout.SuspendLayout();
            this.channelLayout.SuspendLayout();
            this.grpInputBarcode.SuspendLayout();
            this.inputBarcodeLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numInputPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInputBaud)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInputTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInputRetry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInputRetryStep)).BeginInit();
            this.inputButtonLayout.SuspendLayout();
            this.grpOutputBarcode.SuspendLayout();
            this.outputBarcodeLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputBaud)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputRetry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputRetryStep)).BeginInit();
            this.outputButtonLayout.SuspendLayout();
            this.optionLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.channelLayout, 0, 1);
            this.rootLayout.Controls.Add(this.btnSaveBarcode, 0, 2);
            this.rootLayout.Controls.Add(this.lblLastResult, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 390F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
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
            this.lblHeader.Location = new System.Drawing.Point(8, 8);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1662, 26);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "BARCODE";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // channelLayout
            // 
            this.channelLayout.ColumnCount = 2;
            this.channelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.channelLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.channelLayout.Controls.Add(this.grpInputBarcode, 0, 0);
            this.channelLayout.Controls.Add(this.grpOutputBarcode, 1, 0);
            this.channelLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.channelLayout.Location = new System.Drawing.Point(11, 41);
            this.channelLayout.Name = "channelLayout";
            this.channelLayout.Padding = new System.Windows.Forms.Padding(0, 8, 0, 4);
            this.channelLayout.Size = new System.Drawing.Size(1656, 384);
            this.channelLayout.TabIndex = 1;
            // 
            // grpInputBarcode
            // 
            this.grpInputBarcode.Controls.Add(this.inputBarcodeLayout);
            this.grpInputBarcode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInputBarcode.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpInputBarcode.Location = new System.Drawing.Point(0, 8);
            this.grpInputBarcode.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.grpInputBarcode.Name = "grpInputBarcode";
            this.grpInputBarcode.Size = new System.Drawing.Size(824, 372);
            this.grpInputBarcode.TabIndex = 0;
            this.grpInputBarcode.TabStop = false;
            this.grpInputBarcode.Text = "INPUT WAFER BARCODE";
            // 
            // inputBarcodeLayout
            // 
            this.inputBarcodeLayout.ColumnCount = 2;
            this.inputBarcodeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.inputBarcodeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.inputBarcodeLayout.Controls.Add(this.chkInputUse, 0, 0);
            this.inputBarcodeLayout.Controls.Add(this.lblInputPort, 0, 1);
            this.inputBarcodeLayout.Controls.Add(this.numInputPort, 1, 1);
            this.inputBarcodeLayout.Controls.Add(this.lblInputBaud, 0, 2);
            this.inputBarcodeLayout.Controls.Add(this.numInputBaud, 1, 2);
            this.inputBarcodeLayout.Controls.Add(this.lblInputTimeout, 0, 3);
            this.inputBarcodeLayout.Controls.Add(this.numInputTimeout, 1, 3);
            this.inputBarcodeLayout.Controls.Add(this.lblInputRetry, 0, 4);
            this.inputBarcodeLayout.Controls.Add(this.numInputRetry, 1, 4);
            this.inputBarcodeLayout.Controls.Add(this.lblInputRetryStep, 0, 5);
            this.inputBarcodeLayout.Controls.Add(this.numInputRetryStep, 1, 5);
            this.inputBarcodeLayout.Controls.Add(this.lblInputTrigger, 0, 6);
            this.inputBarcodeLayout.Controls.Add(this.txtInputTrigger, 1, 6);
            this.inputBarcodeLayout.Controls.Add(this.inputButtonLayout, 0, 7);
            this.inputBarcodeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inputBarcodeLayout.Location = new System.Drawing.Point(3, 21);
            this.inputBarcodeLayout.Name = "inputBarcodeLayout";
            this.inputBarcodeLayout.Padding = new System.Windows.Forms.Padding(10);
            this.inputBarcodeLayout.RowCount = 8;
            this.inputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.inputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.inputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.inputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.inputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.inputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.inputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.inputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.inputBarcodeLayout.Size = new System.Drawing.Size(818, 348);
            this.inputBarcodeLayout.TabIndex = 0;
            // 
            // chkInputUse
            // 
            this.inputBarcodeLayout.SetColumnSpan(this.chkInputUse, 2);
            this.chkInputUse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkInputUse.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.chkInputUse.Location = new System.Drawing.Point(13, 13);
            this.chkInputUse.Name = "chkInputUse";
            this.chkInputUse.Size = new System.Drawing.Size(792, 32);
            this.chkInputUse.TabIndex = 0;
            this.chkInputUse.Text = "USE INPUT WAFER BARCODE";
            // 
            // lblInputPort
            // 
            this.lblInputPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputPort.Location = new System.Drawing.Point(13, 48);
            this.lblInputPort.Name = "lblInputPort";
            this.lblInputPort.Size = new System.Drawing.Size(184, 38);
            this.lblInputPort.TabIndex = 1;
            this.lblInputPort.Text = "SERIAL PORT (COM)";
            this.lblInputPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numInputPort
            // 
            this.numInputPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numInputPort.Location = new System.Drawing.Point(203, 51);
            this.numInputPort.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.numInputPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numInputPort.Name = "numInputPort";
            this.numInputPort.Size = new System.Drawing.Size(602, 25);
            this.numInputPort.TabIndex = 2;
            this.numInputPort.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblInputBaud
            // 
            this.lblInputBaud.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputBaud.Location = new System.Drawing.Point(13, 86);
            this.lblInputBaud.Name = "lblInputBaud";
            this.lblInputBaud.Size = new System.Drawing.Size(184, 38);
            this.lblInputBaud.TabIndex = 3;
            this.lblInputBaud.Text = "BAUD RATE";
            this.lblInputBaud.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numInputBaud
            // 
            this.numInputBaud.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numInputBaud.Location = new System.Drawing.Point(203, 89);
            this.numInputBaud.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numInputBaud.Minimum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numInputBaud.Name = "numInputBaud";
            this.numInputBaud.Size = new System.Drawing.Size(602, 25);
            this.numInputBaud.TabIndex = 4;
            this.numInputBaud.Value = new decimal(new int[] {
            300,
            0,
            0,
            0});
            // 
            // lblInputTimeout
            // 
            this.lblInputTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputTimeout.Location = new System.Drawing.Point(13, 124);
            this.lblInputTimeout.Name = "lblInputTimeout";
            this.lblInputTimeout.Size = new System.Drawing.Size(184, 38);
            this.lblInputTimeout.TabIndex = 5;
            this.lblInputTimeout.Text = "READ TIMEOUT (ms)";
            this.lblInputTimeout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numInputTimeout
            // 
            this.numInputTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numInputTimeout.Location = new System.Drawing.Point(203, 127);
            this.numInputTimeout.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.numInputTimeout.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numInputTimeout.Name = "numInputTimeout";
            this.numInputTimeout.Size = new System.Drawing.Size(602, 25);
            this.numInputTimeout.TabIndex = 6;
            this.numInputTimeout.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // lblInputRetry
            // 
            this.lblInputRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputRetry.Location = new System.Drawing.Point(13, 162);
            this.lblInputRetry.Name = "lblInputRetry";
            this.lblInputRetry.Size = new System.Drawing.Size(184, 38);
            this.lblInputRetry.TabIndex = 7;
            this.lblInputRetry.Text = "RETRY COUNT";
            this.lblInputRetry.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numInputRetry
            // 
            this.numInputRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numInputRetry.Location = new System.Drawing.Point(203, 165);
            this.numInputRetry.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numInputRetry.Name = "numInputRetry";
            this.numInputRetry.Size = new System.Drawing.Size(602, 25);
            this.numInputRetry.TabIndex = 8;
            // 
            // lblInputRetryStep
            // 
            this.lblInputRetryStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputRetryStep.Location = new System.Drawing.Point(13, 200);
            this.lblInputRetryStep.Name = "lblInputRetryStep";
            this.lblInputRetryStep.Size = new System.Drawing.Size(184, 38);
            this.lblInputRetryStep.TabIndex = 9;
            this.lblInputRetryStep.Text = "Y RETRY STEP (mm)";
            this.lblInputRetryStep.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numInputRetryStep
            // 
            this.numInputRetryStep.DecimalPlaces = 3;
            this.numInputRetryStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numInputRetryStep.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numInputRetryStep.Location = new System.Drawing.Point(203, 203);
            this.numInputRetryStep.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numInputRetryStep.Name = "numInputRetryStep";
            this.numInputRetryStep.Size = new System.Drawing.Size(602, 25);
            this.numInputRetryStep.TabIndex = 10;
            this.numInputRetryStep.Value = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            // 
            // lblInputTrigger
            // 
            this.lblInputTrigger.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInputTrigger.Location = new System.Drawing.Point(13, 238);
            this.lblInputTrigger.Name = "lblInputTrigger";
            this.lblInputTrigger.Size = new System.Drawing.Size(184, 38);
            this.lblInputTrigger.TabIndex = 11;
            this.lblInputTrigger.Text = "TRIGGER (blank=listen)";
            this.lblInputTrigger.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtInputTrigger
            // 
            this.txtInputTrigger.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInputTrigger.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtInputTrigger.Location = new System.Drawing.Point(203, 241);
            this.txtInputTrigger.MaxLength = 128;
            this.txtInputTrigger.Name = "txtInputTrigger";
            this.txtInputTrigger.Size = new System.Drawing.Size(602, 22);
            this.txtInputTrigger.TabIndex = 12;
            // 
            // inputButtonLayout
            // 
            this.inputButtonLayout.ColumnCount = 2;
            this.inputBarcodeLayout.SetColumnSpan(this.inputButtonLayout, 2);
            this.inputButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.inputButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.inputButtonLayout.Controls.Add(this.btnInputConnect, 0, 0);
            this.inputButtonLayout.Controls.Add(this.btnInputTest, 1, 0);
            this.inputButtonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inputButtonLayout.Location = new System.Drawing.Point(13, 279);
            this.inputButtonLayout.Name = "inputButtonLayout";
            this.inputButtonLayout.Size = new System.Drawing.Size(792, 56);
            this.inputButtonLayout.TabIndex = 13;
            // 
            // btnInputConnect
            // 
            this.btnInputConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInputConnect.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnInputConnect.BadgeText = "ACTION";
            this.btnInputConnect.BorderColor = System.Drawing.Color.Empty;
            this.btnInputConnect.BorderWidth = 0;
            this.btnInputConnect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInputConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInputConnect.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnInputConnect.ForeColor = System.Drawing.Color.White;
            this.btnInputConnect.Location = new System.Drawing.Point(3, 3);
            this.btnInputConnect.Name = "btnInputConnect";
            this.btnInputConnect.Size = new System.Drawing.Size(390, 50);
            this.btnInputConnect.TabIndex = 0;
            this.btnInputConnect.Text = "CONNECT";
            // 
            // btnInputTest
            // 
            this.btnInputTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnInputTest.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnInputTest.BadgeText = "ACTION";
            this.btnInputTest.BorderColor = System.Drawing.Color.Empty;
            this.btnInputTest.BorderWidth = 0;
            this.btnInputTest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInputTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInputTest.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnInputTest.ForeColor = System.Drawing.Color.White;
            this.btnInputTest.Location = new System.Drawing.Point(399, 3);
            this.btnInputTest.Name = "btnInputTest";
            this.btnInputTest.Size = new System.Drawing.Size(390, 50);
            this.btnInputTest.TabIndex = 1;
            this.btnInputTest.Text = "TEST READ";
            // 
            // grpOutputBarcode
            // 
            this.grpOutputBarcode.Controls.Add(this.outputBarcodeLayout);
            this.grpOutputBarcode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOutputBarcode.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpOutputBarcode.Location = new System.Drawing.Point(832, 8);
            this.grpOutputBarcode.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.grpOutputBarcode.Name = "grpOutputBarcode";
            this.grpOutputBarcode.Size = new System.Drawing.Size(824, 372);
            this.grpOutputBarcode.TabIndex = 1;
            this.grpOutputBarcode.TabStop = false;
            this.grpOutputBarcode.Text = "OUTPUT BIN BARCODE";
            // 
            // outputBarcodeLayout
            // 
            this.outputBarcodeLayout.ColumnCount = 2;
            this.outputBarcodeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.outputBarcodeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.outputBarcodeLayout.Controls.Add(this.chkOutputUse, 0, 0);
            this.outputBarcodeLayout.Controls.Add(this.lblOutputPort, 0, 1);
            this.outputBarcodeLayout.Controls.Add(this.numOutputPort, 1, 1);
            this.outputBarcodeLayout.Controls.Add(this.lblOutputBaud, 0, 2);
            this.outputBarcodeLayout.Controls.Add(this.numOutputBaud, 1, 2);
            this.outputBarcodeLayout.Controls.Add(this.lblOutputTimeout, 0, 3);
            this.outputBarcodeLayout.Controls.Add(this.numOutputTimeout, 1, 3);
            this.outputBarcodeLayout.Controls.Add(this.lblOutputRetry, 0, 4);
            this.outputBarcodeLayout.Controls.Add(this.numOutputRetry, 1, 4);
            this.outputBarcodeLayout.Controls.Add(this.lblOutputRetryStep, 0, 5);
            this.outputBarcodeLayout.Controls.Add(this.numOutputRetryStep, 1, 5);
            this.outputBarcodeLayout.Controls.Add(this.lblOutputTrigger, 0, 6);
            this.outputBarcodeLayout.Controls.Add(this.txtOutputTrigger, 1, 6);
            this.outputBarcodeLayout.Controls.Add(this.outputButtonLayout, 0, 7);
            this.outputBarcodeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputBarcodeLayout.Location = new System.Drawing.Point(3, 21);
            this.outputBarcodeLayout.Name = "outputBarcodeLayout";
            this.outputBarcodeLayout.Padding = new System.Windows.Forms.Padding(10);
            this.outputBarcodeLayout.RowCount = 8;
            this.outputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.outputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.outputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.outputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.outputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.outputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.outputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.outputBarcodeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.outputBarcodeLayout.Size = new System.Drawing.Size(818, 348);
            this.outputBarcodeLayout.TabIndex = 0;
            // 
            // chkOutputUse
            // 
            this.outputBarcodeLayout.SetColumnSpan(this.chkOutputUse, 2);
            this.chkOutputUse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkOutputUse.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.chkOutputUse.Location = new System.Drawing.Point(13, 13);
            this.chkOutputUse.Name = "chkOutputUse";
            this.chkOutputUse.Size = new System.Drawing.Size(792, 32);
            this.chkOutputUse.TabIndex = 0;
            this.chkOutputUse.Text = "USE OUTPUT BIN BARCODE";
            // 
            // lblOutputPort
            // 
            this.lblOutputPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOutputPort.Location = new System.Drawing.Point(13, 48);
            this.lblOutputPort.Name = "lblOutputPort";
            this.lblOutputPort.Size = new System.Drawing.Size(184, 38);
            this.lblOutputPort.TabIndex = 1;
            this.lblOutputPort.Text = "SERIAL PORT (COM)";
            this.lblOutputPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numOutputPort
            // 
            this.numOutputPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numOutputPort.Location = new System.Drawing.Point(203, 51);
            this.numOutputPort.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.numOutputPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numOutputPort.Name = "numOutputPort";
            this.numOutputPort.Size = new System.Drawing.Size(602, 25);
            this.numOutputPort.TabIndex = 2;
            this.numOutputPort.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblOutputBaud
            // 
            this.lblOutputBaud.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOutputBaud.Location = new System.Drawing.Point(13, 86);
            this.lblOutputBaud.Name = "lblOutputBaud";
            this.lblOutputBaud.Size = new System.Drawing.Size(184, 38);
            this.lblOutputBaud.TabIndex = 3;
            this.lblOutputBaud.Text = "BAUD RATE";
            this.lblOutputBaud.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numOutputBaud
            // 
            this.numOutputBaud.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numOutputBaud.Location = new System.Drawing.Point(203, 89);
            this.numOutputBaud.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numOutputBaud.Minimum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numOutputBaud.Name = "numOutputBaud";
            this.numOutputBaud.Size = new System.Drawing.Size(602, 25);
            this.numOutputBaud.TabIndex = 4;
            this.numOutputBaud.Value = new decimal(new int[] {
            300,
            0,
            0,
            0});
            // 
            // lblOutputTimeout
            // 
            this.lblOutputTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOutputTimeout.Location = new System.Drawing.Point(13, 124);
            this.lblOutputTimeout.Name = "lblOutputTimeout";
            this.lblOutputTimeout.Size = new System.Drawing.Size(184, 38);
            this.lblOutputTimeout.TabIndex = 5;
            this.lblOutputTimeout.Text = "READ TIMEOUT (ms)";
            this.lblOutputTimeout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numOutputTimeout
            // 
            this.numOutputTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numOutputTimeout.Location = new System.Drawing.Point(203, 127);
            this.numOutputTimeout.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.numOutputTimeout.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numOutputTimeout.Name = "numOutputTimeout";
            this.numOutputTimeout.Size = new System.Drawing.Size(602, 25);
            this.numOutputTimeout.TabIndex = 6;
            this.numOutputTimeout.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // lblOutputRetry
            // 
            this.lblOutputRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOutputRetry.Location = new System.Drawing.Point(13, 162);
            this.lblOutputRetry.Name = "lblOutputRetry";
            this.lblOutputRetry.Size = new System.Drawing.Size(184, 38);
            this.lblOutputRetry.TabIndex = 7;
            this.lblOutputRetry.Text = "RETRY COUNT";
            this.lblOutputRetry.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numOutputRetry
            // 
            this.numOutputRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numOutputRetry.Location = new System.Drawing.Point(203, 165);
            this.numOutputRetry.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numOutputRetry.Name = "numOutputRetry";
            this.numOutputRetry.Size = new System.Drawing.Size(602, 25);
            this.numOutputRetry.TabIndex = 8;
            // 
            // lblOutputRetryStep
            // 
            this.lblOutputRetryStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOutputRetryStep.Location = new System.Drawing.Point(13, 200);
            this.lblOutputRetryStep.Name = "lblOutputRetryStep";
            this.lblOutputRetryStep.Size = new System.Drawing.Size(184, 38);
            this.lblOutputRetryStep.TabIndex = 9;
            this.lblOutputRetryStep.Text = "Y RETRY STEP (mm)";
            this.lblOutputRetryStep.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // numOutputRetryStep
            // 
            this.numOutputRetryStep.DecimalPlaces = 3;
            this.numOutputRetryStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numOutputRetryStep.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numOutputRetryStep.Location = new System.Drawing.Point(203, 203);
            this.numOutputRetryStep.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numOutputRetryStep.Name = "numOutputRetryStep";
            this.numOutputRetryStep.Size = new System.Drawing.Size(602, 25);
            this.numOutputRetryStep.TabIndex = 10;
            this.numOutputRetryStep.Value = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            // 
            // lblOutputTrigger
            // 
            this.lblOutputTrigger.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOutputTrigger.Location = new System.Drawing.Point(13, 238);
            this.lblOutputTrigger.Name = "lblOutputTrigger";
            this.lblOutputTrigger.Size = new System.Drawing.Size(184, 38);
            this.lblOutputTrigger.TabIndex = 11;
            this.lblOutputTrigger.Text = "TRIGGER (blank=listen)";
            this.lblOutputTrigger.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtOutputTrigger
            // 
            this.txtOutputTrigger.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOutputTrigger.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtOutputTrigger.Location = new System.Drawing.Point(203, 241);
            this.txtOutputTrigger.MaxLength = 128;
            this.txtOutputTrigger.Name = "txtOutputTrigger";
            this.txtOutputTrigger.Size = new System.Drawing.Size(602, 22);
            this.txtOutputTrigger.TabIndex = 12;
            // 
            // outputButtonLayout
            // 
            this.outputButtonLayout.ColumnCount = 2;
            this.outputBarcodeLayout.SetColumnSpan(this.outputButtonLayout, 2);
            this.outputButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.outputButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.outputButtonLayout.Controls.Add(this.btnOutputConnect, 0, 0);
            this.outputButtonLayout.Controls.Add(this.btnOutputTest, 1, 0);
            this.outputButtonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputButtonLayout.Location = new System.Drawing.Point(13, 279);
            this.outputButtonLayout.Name = "outputButtonLayout";
            this.outputButtonLayout.Size = new System.Drawing.Size(792, 56);
            this.outputButtonLayout.TabIndex = 13;
            // 
            // btnOutputConnect
            // 
            this.btnOutputConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnOutputConnect.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnOutputConnect.BadgeText = "ACTION";
            this.btnOutputConnect.BorderColor = System.Drawing.Color.Empty;
            this.btnOutputConnect.BorderWidth = 0;
            this.btnOutputConnect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOutputConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOutputConnect.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOutputConnect.ForeColor = System.Drawing.Color.White;
            this.btnOutputConnect.Location = new System.Drawing.Point(3, 3);
            this.btnOutputConnect.Name = "btnOutputConnect";
            this.btnOutputConnect.Size = new System.Drawing.Size(390, 50);
            this.btnOutputConnect.TabIndex = 0;
            this.btnOutputConnect.Text = "CONNECT";
            // 
            // btnOutputTest
            // 
            this.btnOutputTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnOutputTest.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnOutputTest.BadgeText = "ACTION";
            this.btnOutputTest.BorderColor = System.Drawing.Color.Empty;
            this.btnOutputTest.BorderWidth = 0;
            this.btnOutputTest.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOutputTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOutputTest.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOutputTest.ForeColor = System.Drawing.Color.White;
            this.btnOutputTest.Location = new System.Drawing.Point(399, 3);
            this.btnOutputTest.Name = "btnOutputTest";
            this.btnOutputTest.Size = new System.Drawing.Size(390, 50);
            this.btnOutputTest.TabIndex = 1;
            this.btnOutputTest.Text = "TEST READ";
            // 
            // btnSaveBarcode
            // 
            this.btnSaveBarcode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnSaveBarcode.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnSaveBarcode.BadgeText = "ACTION";
            this.btnSaveBarcode.BorderColor = System.Drawing.Color.Empty;
            this.btnSaveBarcode.BorderWidth = 0;
            this.btnSaveBarcode.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSaveBarcode.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSaveBarcode.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSaveBarcode.ForeColor = System.Drawing.Color.White;
            this.btnSaveBarcode.Location = new System.Drawing.Point(1410, 431);
            this.btnSaveBarcode.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.btnSaveBarcode.Name = "btnSaveBarcode";
            this.btnSaveBarcode.Size = new System.Drawing.Size(260, 36);
            this.btnSaveBarcode.TabIndex = 2;
            this.btnSaveBarcode.Text = "SAVE BARCODE SETTINGS";
            // 
            // lblLastResult
            // 
            this.lblLastResult.BackColor = System.Drawing.Color.Black;
            this.lblLastResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLastResult.Font = new System.Drawing.Font("Consolas", 12F);
            this.lblLastResult.ForeColor = System.Drawing.Color.LimeGreen;
            this.lblLastResult.Location = new System.Drawing.Point(8, 470);
            this.lblLastResult.Margin = new System.Windows.Forms.Padding(0);
            this.lblLastResult.Name = "lblLastResult";
            this.lblLastResult.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblLastResult.Size = new System.Drawing.Size(1662, 40);
            this.lblLastResult.TabIndex = 3;
            this.lblLastResult.Text = "Last Result : ";
            this.lblLastResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // optionLayout
            // 
            this.optionLayout.ColumnCount = 2;
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 210F));
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 280F));
            this.optionLayout.Controls.Add(this.lblPortTitle, 0, 0);
            this.optionLayout.Controls.Add(this.lblPortValue, 1, 0);
            this.optionLayout.Controls.Add(this.lblBaudTitle, 0, 1);
            this.optionLayout.Controls.Add(this.lblBaudValue, 1, 1);
            this.optionLayout.Controls.Add(this.lblDataBitsTitle, 0, 2);
            this.optionLayout.Controls.Add(this.lblDataBitsValue, 1, 2);
            this.optionLayout.Controls.Add(this.lblParityTitle, 0, 3);
            this.optionLayout.Controls.Add(this.lblParityValue, 1, 3);
            this.optionLayout.Controls.Add(this.lblStopBitsTitle, 0, 4);
            this.optionLayout.Controls.Add(this.lblStopBitsValue, 1, 4);
            this.optionLayout.Controls.Add(this.lblHeadCharTitle, 0, 5);
            this.optionLayout.Controls.Add(this.lblHeadCharValue, 1, 5);
            this.optionLayout.Controls.Add(this.lblTailCharTitle, 0, 6);
            this.optionLayout.Controls.Add(this.lblTailCharValue, 1, 6);
            this.optionLayout.Controls.Add(this.lblTimeoutTitle, 0, 7);
            this.optionLayout.Controls.Add(this.lblTimeoutValue, 1, 7);
            this.optionLayout.Controls.Add(this.lblRetryTitle, 0, 8);
            this.optionLayout.Controls.Add(this.lblRetryValue, 1, 8);
            this.optionLayout.Controls.Add(this.btnConnect, 0, 9);
            this.optionLayout.Controls.Add(this.btnTestRead, 1, 9);
            this.optionLayout.Dock = System.Windows.Forms.DockStyle.Left;
            this.optionLayout.Location = new System.Drawing.Point(0, 0);
            this.optionLayout.Name = "optionLayout";
            this.optionLayout.RowCount = 11;
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.optionLayout.Size = new System.Drawing.Size(520, 100);
            this.optionLayout.TabIndex = 0;
            // 
            // lblPortTitle
            // 
            this.lblPortTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblPortTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPortTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPortTitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblPortTitle.Location = new System.Drawing.Point(2, 2);
            this.lblPortTitle.Margin = new System.Windows.Forms.Padding(2);
            this.lblPortTitle.Name = "lblPortTitle";
            this.lblPortTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblPortTitle.Size = new System.Drawing.Size(206, 26);
            this.lblPortTitle.TabIndex = 0;
            this.lblPortTitle.Text = "PORT";
            this.lblPortTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPortValue
            // 
            this.lblPortValue.BackColor = System.Drawing.Color.White;
            this.lblPortValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPortValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPortValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblPortValue.Location = new System.Drawing.Point(212, 2);
            this.lblPortValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblPortValue.Name = "lblPortValue";
            this.lblPortValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblPortValue.Size = new System.Drawing.Size(306, 26);
            this.lblPortValue.TabIndex = 1;
            this.lblPortValue.Text = "COM3";
            this.lblPortValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblBaudTitle
            // 
            this.lblBaudTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblBaudTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBaudTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBaudTitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblBaudTitle.Location = new System.Drawing.Point(2, 32);
            this.lblBaudTitle.Margin = new System.Windows.Forms.Padding(2);
            this.lblBaudTitle.Name = "lblBaudTitle";
            this.lblBaudTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblBaudTitle.Size = new System.Drawing.Size(206, 26);
            this.lblBaudTitle.TabIndex = 2;
            this.lblBaudTitle.Text = "BAUD RATE";
            this.lblBaudTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblBaudValue
            // 
            this.lblBaudValue.BackColor = System.Drawing.Color.White;
            this.lblBaudValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBaudValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBaudValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblBaudValue.Location = new System.Drawing.Point(212, 32);
            this.lblBaudValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblBaudValue.Name = "lblBaudValue";
            this.lblBaudValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblBaudValue.Size = new System.Drawing.Size(306, 26);
            this.lblBaudValue.TabIndex = 3;
            this.lblBaudValue.Text = "9600";
            this.lblBaudValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblDataBitsTitle
            // 
            this.lblDataBitsTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblDataBitsTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDataBitsTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDataBitsTitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDataBitsTitle.Location = new System.Drawing.Point(2, 62);
            this.lblDataBitsTitle.Margin = new System.Windows.Forms.Padding(2);
            this.lblDataBitsTitle.Name = "lblDataBitsTitle";
            this.lblDataBitsTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblDataBitsTitle.Size = new System.Drawing.Size(206, 26);
            this.lblDataBitsTitle.TabIndex = 4;
            this.lblDataBitsTitle.Text = "DATA BITS";
            this.lblDataBitsTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDataBitsValue
            // 
            this.lblDataBitsValue.BackColor = System.Drawing.Color.White;
            this.lblDataBitsValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDataBitsValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDataBitsValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblDataBitsValue.Location = new System.Drawing.Point(212, 62);
            this.lblDataBitsValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblDataBitsValue.Name = "lblDataBitsValue";
            this.lblDataBitsValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblDataBitsValue.Size = new System.Drawing.Size(306, 26);
            this.lblDataBitsValue.TabIndex = 5;
            this.lblDataBitsValue.Text = "8";
            this.lblDataBitsValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblParityTitle
            // 
            this.lblParityTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblParityTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblParityTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblParityTitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblParityTitle.Location = new System.Drawing.Point(2, 92);
            this.lblParityTitle.Margin = new System.Windows.Forms.Padding(2);
            this.lblParityTitle.Name = "lblParityTitle";
            this.lblParityTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblParityTitle.Size = new System.Drawing.Size(206, 26);
            this.lblParityTitle.TabIndex = 6;
            this.lblParityTitle.Text = "PARITY";
            this.lblParityTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblParityValue
            // 
            this.lblParityValue.BackColor = System.Drawing.Color.White;
            this.lblParityValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblParityValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblParityValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblParityValue.Location = new System.Drawing.Point(212, 92);
            this.lblParityValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblParityValue.Name = "lblParityValue";
            this.lblParityValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblParityValue.Size = new System.Drawing.Size(306, 26);
            this.lblParityValue.TabIndex = 7;
            this.lblParityValue.Text = "NONE";
            this.lblParityValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblStopBitsTitle
            // 
            this.lblStopBitsTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblStopBitsTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStopBitsTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStopBitsTitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblStopBitsTitle.Location = new System.Drawing.Point(2, 122);
            this.lblStopBitsTitle.Margin = new System.Windows.Forms.Padding(2);
            this.lblStopBitsTitle.Name = "lblStopBitsTitle";
            this.lblStopBitsTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblStopBitsTitle.Size = new System.Drawing.Size(206, 26);
            this.lblStopBitsTitle.TabIndex = 8;
            this.lblStopBitsTitle.Text = "STOP BITS";
            this.lblStopBitsTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStopBitsValue
            // 
            this.lblStopBitsValue.BackColor = System.Drawing.Color.White;
            this.lblStopBitsValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStopBitsValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStopBitsValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblStopBitsValue.Location = new System.Drawing.Point(212, 122);
            this.lblStopBitsValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblStopBitsValue.Name = "lblStopBitsValue";
            this.lblStopBitsValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblStopBitsValue.Size = new System.Drawing.Size(306, 26);
            this.lblStopBitsValue.TabIndex = 9;
            this.lblStopBitsValue.Text = "1";
            this.lblStopBitsValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblHeadCharTitle
            // 
            this.lblHeadCharTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblHeadCharTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadCharTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadCharTitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblHeadCharTitle.Location = new System.Drawing.Point(2, 152);
            this.lblHeadCharTitle.Margin = new System.Windows.Forms.Padding(2);
            this.lblHeadCharTitle.Name = "lblHeadCharTitle";
            this.lblHeadCharTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblHeadCharTitle.Size = new System.Drawing.Size(206, 26);
            this.lblHeadCharTitle.TabIndex = 10;
            this.lblHeadCharTitle.Text = "HEAD CHAR";
            this.lblHeadCharTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblHeadCharValue
            // 
            this.lblHeadCharValue.BackColor = System.Drawing.Color.White;
            this.lblHeadCharValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblHeadCharValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeadCharValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblHeadCharValue.Location = new System.Drawing.Point(212, 152);
            this.lblHeadCharValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblHeadCharValue.Name = "lblHeadCharValue";
            this.lblHeadCharValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblHeadCharValue.Size = new System.Drawing.Size(306, 26);
            this.lblHeadCharValue.TabIndex = 11;
            this.lblHeadCharValue.Text = "STX";
            this.lblHeadCharValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTailCharTitle
            // 
            this.lblTailCharTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblTailCharTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTailCharTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTailCharTitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblTailCharTitle.Location = new System.Drawing.Point(2, 182);
            this.lblTailCharTitle.Margin = new System.Windows.Forms.Padding(2);
            this.lblTailCharTitle.Name = "lblTailCharTitle";
            this.lblTailCharTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblTailCharTitle.Size = new System.Drawing.Size(206, 26);
            this.lblTailCharTitle.TabIndex = 12;
            this.lblTailCharTitle.Text = "TAIL CHAR";
            this.lblTailCharTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTailCharValue
            // 
            this.lblTailCharValue.BackColor = System.Drawing.Color.White;
            this.lblTailCharValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTailCharValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTailCharValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblTailCharValue.Location = new System.Drawing.Point(212, 182);
            this.lblTailCharValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblTailCharValue.Name = "lblTailCharValue";
            this.lblTailCharValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblTailCharValue.Size = new System.Drawing.Size(306, 26);
            this.lblTailCharValue.TabIndex = 13;
            this.lblTailCharValue.Text = "ETX";
            this.lblTailCharValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTimeoutTitle
            // 
            this.lblTimeoutTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblTimeoutTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTimeoutTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeoutTitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblTimeoutTitle.Location = new System.Drawing.Point(2, 212);
            this.lblTimeoutTitle.Margin = new System.Windows.Forms.Padding(2);
            this.lblTimeoutTitle.Name = "lblTimeoutTitle";
            this.lblTimeoutTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblTimeoutTitle.Size = new System.Drawing.Size(206, 26);
            this.lblTimeoutTitle.TabIndex = 14;
            this.lblTimeoutTitle.Text = "READ TIMEOUT";
            this.lblTimeoutTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTimeoutValue
            // 
            this.lblTimeoutValue.BackColor = System.Drawing.Color.White;
            this.lblTimeoutValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTimeoutValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeoutValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblTimeoutValue.Location = new System.Drawing.Point(212, 212);
            this.lblTimeoutValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblTimeoutValue.Name = "lblTimeoutValue";
            this.lblTimeoutValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblTimeoutValue.Size = new System.Drawing.Size(306, 26);
            this.lblTimeoutValue.TabIndex = 15;
            this.lblTimeoutValue.Text = "3000 ms";
            this.lblTimeoutValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblRetryTitle
            // 
            this.lblRetryTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblRetryTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRetryTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRetryTitle.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblRetryTitle.Location = new System.Drawing.Point(2, 242);
            this.lblRetryTitle.Margin = new System.Windows.Forms.Padding(2);
            this.lblRetryTitle.Name = "lblRetryTitle";
            this.lblRetryTitle.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblRetryTitle.Size = new System.Drawing.Size(206, 26);
            this.lblRetryTitle.TabIndex = 16;
            this.lblRetryTitle.Text = "RETRY COUNT";
            this.lblRetryTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRetryValue
            // 
            this.lblRetryValue.BackColor = System.Drawing.Color.White;
            this.lblRetryValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRetryValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRetryValue.Font = new System.Drawing.Font("Consolas", 10F);
            this.lblRetryValue.Location = new System.Drawing.Point(212, 242);
            this.lblRetryValue.Margin = new System.Windows.Forms.Padding(2);
            this.lblRetryValue.Name = "lblRetryValue";
            this.lblRetryValue.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.lblRetryValue.Size = new System.Drawing.Size(306, 26);
            this.lblRetryValue.TabIndex = 17;
            this.lblRetryValue.Text = "3";
            this.lblRetryValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnConnect
            // 
            this.btnConnect.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnConnect.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnConnect.BadgeText = "ACTION";
            this.btnConnect.BorderColor = System.Drawing.Color.Empty;
            this.btnConnect.BorderWidth = 0;
            this.btnConnect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnConnect.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnConnect.ForeColor = System.Drawing.Color.White;
            this.btnConnect.Location = new System.Drawing.Point(2, 272);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(2);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(206, 30);
            this.btnConnect.TabIndex = 18;
            this.btnConnect.Text = "CONNECT";
            // 
            // btnTestRead
            // 
            this.btnTestRead.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnTestRead.BadgeColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(166)))), ((int)(((byte)(35)))));
            this.btnTestRead.BadgeText = "ACTION";
            this.btnTestRead.BorderColor = System.Drawing.Color.Empty;
            this.btnTestRead.BorderWidth = 0;
            this.btnTestRead.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestRead.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTestRead.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnTestRead.ForeColor = System.Drawing.Color.White;
            this.btnTestRead.Location = new System.Drawing.Point(212, 272);
            this.btnTestRead.Margin = new System.Windows.Forms.Padding(2);
            this.btnTestRead.Name = "btnTestRead";
            this.btnTestRead.Size = new System.Drawing.Size(306, 30);
            this.btnTestRead.TabIndex = 19;
            this.btnTestRead.Text = "TEST READ";
            // 
            // BarcodeReaderPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "BarcodeReaderPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            this.channelLayout.ResumeLayout(false);
            this.grpInputBarcode.ResumeLayout(false);
            this.inputBarcodeLayout.ResumeLayout(false);
            this.inputBarcodeLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numInputPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInputBaud)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInputTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInputRetry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInputRetryStep)).EndInit();
            this.inputButtonLayout.ResumeLayout(false);
            this.grpOutputBarcode.ResumeLayout(false);
            this.outputBarcodeLayout.ResumeLayout(false);
            this.outputBarcodeLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputBaud)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputRetry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOutputRetryStep)).EndInit();
            this.outputButtonLayout.ResumeLayout(false);
            this.optionLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }

    partial class ZoomLensPage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel lensLayout;
        private System.Windows.Forms.GroupBox grpInputVision;
        private System.Windows.Forms.GroupBox grpOutputVision;
        private System.Windows.Forms.GroupBox grpLowerVision;
        private System.Windows.Forms.GroupBox grpBottomVision;
        private System.Windows.Forms.GroupBox grpSideVisionFront;
        private System.Windows.Forms.GroupBox grpSideVisionRear;
        private System.Windows.Forms.TableLayoutPanel inputVisionLayout;
        private System.Windows.Forms.TableLayoutPanel outputVisionLayout;
        private System.Windows.Forms.TableLayoutPanel lowerVisionLayout;
        private System.Windows.Forms.TableLayoutPanel bottomVisionLayout;
        private System.Windows.Forms.TableLayoutPanel sideVisionFrontLayout;
        private System.Windows.Forms.TableLayoutPanel sideVisionRearLayout;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lensLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpInputVision = new System.Windows.Forms.GroupBox();
            this.grpOutputVision = new System.Windows.Forms.GroupBox();
            this.grpLowerVision = new System.Windows.Forms.GroupBox();
            this.grpBottomVision = new System.Windows.Forms.GroupBox();
            this.grpSideVisionFront = new System.Windows.Forms.GroupBox();
            this.grpSideVisionRear = new System.Windows.Forms.GroupBox();
            this.inputVisionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.outputVisionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lowerVisionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.bottomVisionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.sideVisionFrontLayout = new System.Windows.Forms.TableLayoutPanel();
            this.sideVisionRearLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rootLayout.SuspendLayout();
            this.lensLayout.SuspendLayout();
            this.grpInputVision.SuspendLayout();
            this.grpOutputVision.SuspendLayout();
            this.grpLowerVision.SuspendLayout();
            this.grpBottomVision.SuspendLayout();
            this.grpSideVisionFront.SuspendLayout();
            this.grpSideVisionRear.SuspendLayout();
            this.SuspendLayout();
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.lensLayout, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 500F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Text = "ZOOM LENS";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lensLayout.ColumnCount = 1;
            this.lensLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lensLayout.Controls.Add(this.grpInputVision, 0, 0);
            this.lensLayout.Controls.Add(this.grpOutputVision, 0, 1);
            this.lensLayout.Controls.Add(this.grpLowerVision, 0, 2);
            this.lensLayout.Controls.Add(this.grpBottomVision, 0, 3);
            this.lensLayout.Controls.Add(this.grpSideVisionFront, 0, 4);
            this.lensLayout.Controls.Add(this.grpSideVisionRear, 0, 5);
            this.lensLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lensLayout.RowCount = 6;
            this.lensLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.lensLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.lensLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.lensLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.lensLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.lensLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.grpInputVision.Controls.Add(this.inputVisionLayout);
            this.grpInputVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInputVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpInputVision.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpInputVision.Text = "INPUT VISION";
            this.grpOutputVision.Controls.Add(this.outputVisionLayout);
            this.grpOutputVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOutputVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpOutputVision.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpOutputVision.Text = "OUTPUT VISION";
            this.grpLowerVision.Controls.Add(this.lowerVisionLayout);
            this.grpLowerVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpLowerVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpLowerVision.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpLowerVision.Text = "LOWER VISION";
            this.grpBottomVision.Controls.Add(this.bottomVisionLayout);
            this.grpBottomVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpBottomVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpBottomVision.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpBottomVision.Text = "BOTTOM VISION";
            this.grpSideVisionFront.Controls.Add(this.sideVisionFrontLayout);
            this.grpSideVisionFront.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSideVisionFront.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSideVisionFront.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpSideVisionFront.Text = "SIDE VISION (FRONT)";
            this.grpSideVisionRear.Controls.Add(this.sideVisionRearLayout);
            this.grpSideVisionRear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSideVisionRear.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSideVisionRear.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpSideVisionRear.Text = "SIDE VISION (REAR)";
            this.inputVisionLayout.ColumnCount = 7;
            this.inputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.inputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.inputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.inputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.inputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.inputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.inputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.inputVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "PORT", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            this.inputVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "COM4", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 0);
            this.inputVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "CHANNEL", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 0);
            this.inputVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "1", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 0);
            this.inputVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "ZOOM", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 0);
            this.inputVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "50", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 0);
            this.inputVisionLayout.Controls.Add(new System.Windows.Forms.Button() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "APPLY", FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold) }, 6, 0);
            this.inputVisionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.inputVisionLayout.Padding = new System.Windows.Forms.Padding(8);
            this.outputVisionLayout.ColumnCount = 7;
            this.outputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.outputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.outputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.outputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.outputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.outputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.outputVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.outputVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "PORT", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            this.outputVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "COM4", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 0);
            this.outputVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "CHANNEL", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 0);
            this.outputVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "1", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 0);
            this.outputVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "ZOOM", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 0);
            this.outputVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "50", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 0);
            this.outputVisionLayout.Controls.Add(new System.Windows.Forms.Button() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "APPLY", FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold) }, 6, 0);
            this.outputVisionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputVisionLayout.Padding = new System.Windows.Forms.Padding(8);
            this.lowerVisionLayout.ColumnCount = 7;
            this.lowerVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.lowerVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.lowerVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.lowerVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.lowerVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.lowerVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.lowerVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.lowerVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "PORT", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            this.lowerVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "COM4", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 0);
            this.lowerVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "CHANNEL", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 0);
            this.lowerVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "1", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 0);
            this.lowerVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "ZOOM", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 0);
            this.lowerVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "50", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 0);
            this.lowerVisionLayout.Controls.Add(new System.Windows.Forms.Button() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "APPLY", FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold) }, 6, 0);
            this.lowerVisionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lowerVisionLayout.Padding = new System.Windows.Forms.Padding(8);
            this.bottomVisionLayout.ColumnCount = 7;
            this.bottomVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.bottomVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.bottomVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.bottomVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.bottomVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.bottomVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.bottomVisionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.bottomVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "PORT", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            this.bottomVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "COM4", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 0);
            this.bottomVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "CHANNEL", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 0);
            this.bottomVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "1", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 0);
            this.bottomVisionLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "ZOOM", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 0);
            this.bottomVisionLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "50", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 0);
            this.bottomVisionLayout.Controls.Add(new System.Windows.Forms.Button() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "APPLY", FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold) }, 6, 0);
            this.bottomVisionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomVisionLayout.Padding = new System.Windows.Forms.Padding(8);
            this.sideVisionFrontLayout.ColumnCount = 7;
            this.sideVisionFrontLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.sideVisionFrontLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.sideVisionFrontLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.sideVisionFrontLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.sideVisionFrontLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.sideVisionFrontLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.sideVisionFrontLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.sideVisionFrontLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "PORT", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            this.sideVisionFrontLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "COM4", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 0);
            this.sideVisionFrontLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "CHANNEL", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 0);
            this.sideVisionFrontLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "1", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 0);
            this.sideVisionFrontLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "ZOOM", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 0);
            this.sideVisionFrontLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "50", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 0);
            this.sideVisionFrontLayout.Controls.Add(new System.Windows.Forms.Button() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "APPLY", FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold) }, 6, 0);
            this.sideVisionFrontLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sideVisionFrontLayout.Padding = new System.Windows.Forms.Padding(8);
            this.sideVisionRearLayout.ColumnCount = 7;
            this.sideVisionRearLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.sideVisionRearLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.sideVisionRearLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.sideVisionRearLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.sideVisionRearLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.sideVisionRearLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.sideVisionRearLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.sideVisionRearLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "PORT", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            this.sideVisionRearLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "COM4", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 0);
            this.sideVisionRearLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "CHANNEL", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 0);
            this.sideVisionRearLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "1", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 0);
            this.sideVisionRearLayout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "ZOOM", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 0);
            this.sideVisionRearLayout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "50", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 0);
            this.sideVisionRearLayout.Controls.Add(new System.Windows.Forms.Button() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "APPLY", FlatStyle = System.Windows.Forms.FlatStyle.Flat, Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold) }, 6, 0);
            this.sideVisionRearLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sideVisionRearLayout.Padding = new System.Windows.Forms.Padding(8);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "ZoomLensPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.grpSideVisionRear.ResumeLayout(false);
            this.grpSideVisionFront.ResumeLayout(false);
            this.grpBottomVision.ResumeLayout(false);
            this.grpLowerVision.ResumeLayout(false);
            this.grpOutputVision.ResumeLayout(false);
            this.grpInputVision.ResumeLayout(false);
            this.lensLayout.ResumeLayout(false);
            this.rootLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }

    partial class HeightSensorPage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TableLayoutPanel sensorLayout;
        private System.Windows.Forms.GroupBox grpSensor1;
        private System.Windows.Forms.GroupBox grpSensor2;
        private System.Windows.Forms.TableLayoutPanel sensor1Layout;
        private System.Windows.Forms.TableLayoutPanel sensor2Layout;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.sensorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpSensor1 = new System.Windows.Forms.GroupBox();
            this.grpSensor2 = new System.Windows.Forms.GroupBox();
            this.sensor1Layout = new System.Windows.Forms.TableLayoutPanel();
            this.sensor2Layout = new System.Windows.Forms.TableLayoutPanel();
            this.rootLayout.SuspendLayout();
            this.sensorLayout.SuspendLayout();
            this.grpSensor1.SuspendLayout();
            this.grpSensor2.SuspendLayout();
            this.SuspendLayout();
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.sensorLayout, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 210F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.lblHeader.BackColor = UiTheme.StatusBarBg;
            this.lblHeader.ForeColor = UiTheme.StatusBarFg;
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Text = "HEIGHT SENSOR";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.sensorLayout.ColumnCount = 1;
            this.sensorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sensorLayout.Controls.Add(this.grpSensor1, 0, 0);
            this.sensorLayout.Controls.Add(this.grpSensor2, 0, 1);
            this.sensorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensorLayout.RowCount = 2;
            this.sensorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sensorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.grpSensor1.Controls.Add(this.sensor1Layout);
            this.grpSensor1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSensor1.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSensor1.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpSensor1.Text = "SENSOR #1";
            this.grpSensor2.Controls.Add(this.sensor2Layout);
            this.grpSensor2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSensor2.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSensor2.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.grpSensor2.Text = "SENSOR #2";
            this.sensor1Layout.ColumnCount = 6;
            this.sensor1Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor1Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor1Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor1Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor1Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor1Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "PORT", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "COM5", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 0);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "CHANNEL", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 0);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "1", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 0);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "ZERO", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 0);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "0.00", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 0);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "LIMIT MIN", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 1);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "-10.00", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 1);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "LIMIT MAX", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 1);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "10.00", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 1);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "VALUE", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 1);
            this.sensor1Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "0.000 mm", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 1);
            this.sensor1Layout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensor1Layout.Padding = new System.Windows.Forms.Padding(8);
            this.sensor1Layout.RowCount = 2;
            this.sensor1Layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sensor1Layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sensor2Layout.ColumnCount = 6;
            this.sensor2Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor2Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor2Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor2Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor2Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor2Layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "PORT", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "COM5", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 0);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "CHANNEL", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 0);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "1", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 0);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "ZERO", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 0);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "0.00", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 0);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "LIMIT MIN", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 1);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "-10.00", Font = new System.Drawing.Font("Consolas", 10F) }, 1, 1);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "LIMIT MAX", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 1);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "10.00", Font = new System.Drawing.Font("Consolas", 10F) }, 3, 1);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.Label() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "VALUE", TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 4, 1);
            this.sensor2Layout.Controls.Add(new System.Windows.Forms.TextBox() { Dock = System.Windows.Forms.DockStyle.Fill, Text = "0.000 mm", Font = new System.Drawing.Font("Consolas", 10F) }, 5, 1);
            this.sensor2Layout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sensor2Layout.Padding = new System.Windows.Forms.Padding(8);
            this.sensor2Layout.RowCount = 2;
            this.sensor2Layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sensor2Layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "HeightSensorPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.grpSensor2.ResumeLayout(false);
            this.grpSensor1.ResumeLayout(false);
            this.sensorLayout.ResumeLayout(false);
            this.rootLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
