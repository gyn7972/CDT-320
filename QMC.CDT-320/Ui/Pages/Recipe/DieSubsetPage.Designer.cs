using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class DieSubsetPage
    {
        private TableLayoutPanel dieEditorLayout;
        private Label lblBasicHeader;
        private Label lblSpecLibrary;
        private ComboBox _cbSpecLibrary;
        private Button btnLoadSpec;
        private Button btnSaveSpec;
        private Label lblName;
        private Label lblWidth;
        private Label lblHeight;
        private Label lblThickness;
        private Label lblToleranceHeader;
        private Label lblWidthLower;
        private Label lblWidthUpper;
        private Label lblHeightLower;
        private Label lblHeightUpper;
        private Label lblVisionHeader;
        private Label lblChippingDepth;
        private Label lblChippingLength;
        private Label lblForeignSize;
        private TextBox _tbName;
        private NumericUpDown _nW;
        private NumericUpDown _nH;
        private NumericUpDown _nT;
        private NumericUpDown _nWLow;
        private NumericUpDown _nWUp;
        private NumericUpDown _nHLow;
        private NumericUpDown _nHUp;
        private NumericUpDown _nChipDepth;
        private NumericUpDown _nChipLen;
        private NumericUpDown _nForeign;

        private void InitializeComponent()
        {
            this.dieEditorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblBasicHeader = new System.Windows.Forms.Label();
            this.lblSpecLibrary = new System.Windows.Forms.Label();
            this._cbSpecLibrary = new System.Windows.Forms.ComboBox();
            this.btnLoadSpec = new System.Windows.Forms.Button();
            this.btnSaveSpec = new System.Windows.Forms.Button();
            this.lblName = new System.Windows.Forms.Label();
            this._tbName = new System.Windows.Forms.TextBox();
            this.lblWidth = new System.Windows.Forms.Label();
            this._nW = new System.Windows.Forms.NumericUpDown();
            this.lblHeight = new System.Windows.Forms.Label();
            this._nH = new System.Windows.Forms.NumericUpDown();
            this.lblThickness = new System.Windows.Forms.Label();
            this._nT = new System.Windows.Forms.NumericUpDown();
            this.lblToleranceHeader = new System.Windows.Forms.Label();
            this.lblWidthLower = new System.Windows.Forms.Label();
            this._nWLow = new System.Windows.Forms.NumericUpDown();
            this.lblWidthUpper = new System.Windows.Forms.Label();
            this._nWUp = new System.Windows.Forms.NumericUpDown();
            this.lblHeightLower = new System.Windows.Forms.Label();
            this._nHLow = new System.Windows.Forms.NumericUpDown();
            this.lblHeightUpper = new System.Windows.Forms.Label();
            this._nHUp = new System.Windows.Forms.NumericUpDown();
            this.lblVisionHeader = new System.Windows.Forms.Label();
            this.lblChippingDepth = new System.Windows.Forms.Label();
            this._nChipDepth = new System.Windows.Forms.NumericUpDown();
            this.lblChippingLength = new System.Windows.Forms.Label();
            this._nChipLen = new System.Windows.Forms.NumericUpDown();
            this.lblForeignSize = new System.Windows.Forms.Label();
            this._nForeign = new System.Windows.Forms.NumericUpDown();
            this._editorPanel.SuspendLayout();
            this.dieEditorLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nT)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nWLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nWUp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nHLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nHUp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nChipDepth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nChipLen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nForeign)).BeginInit();
            this.SuspendLayout();
            // 
            // _editorPanel
            // 
            this._editorPanel.Controls.Add(this.dieEditorLayout);
            this._editorPanel.Size = new System.Drawing.Size(1094, 676);
            // 
            // _lblProject
            // 
            this._lblProject.Size = new System.Drawing.Size(794, 36);
            // 
            // dieEditorLayout
            // 
            this.dieEditorLayout.ColumnCount = 4;
            this.dieEditorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.dieEditorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.dieEditorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.dieEditorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.dieEditorLayout.Controls.Add(this.lblBasicHeader, 0, 0);
            this.dieEditorLayout.Controls.Add(this.lblSpecLibrary, 0, 1);
            this.dieEditorLayout.Controls.Add(this._cbSpecLibrary, 1, 1);
            this.dieEditorLayout.Controls.Add(this.btnLoadSpec, 2, 1);
            this.dieEditorLayout.Controls.Add(this.btnSaveSpec, 3, 1);
            this.dieEditorLayout.Controls.Add(this.lblName, 0, 2);
            this.dieEditorLayout.Controls.Add(this._tbName, 1, 2);
            this.dieEditorLayout.Controls.Add(this.lblWidth, 0, 3);
            this.dieEditorLayout.Controls.Add(this._nW, 1, 3);
            this.dieEditorLayout.Controls.Add(this.lblHeight, 0, 4);
            this.dieEditorLayout.Controls.Add(this._nH, 1, 4);
            this.dieEditorLayout.Controls.Add(this.lblThickness, 0, 5);
            this.dieEditorLayout.Controls.Add(this._nT, 1, 5);
            this.dieEditorLayout.Controls.Add(this.lblToleranceHeader, 0, 6);
            this.dieEditorLayout.Controls.Add(this.lblWidthLower, 0, 7);
            this.dieEditorLayout.Controls.Add(this._nWLow, 1, 7);
            this.dieEditorLayout.Controls.Add(this.lblWidthUpper, 0, 8);
            this.dieEditorLayout.Controls.Add(this._nWUp, 1, 8);
            this.dieEditorLayout.Controls.Add(this.lblHeightLower, 0, 9);
            this.dieEditorLayout.Controls.Add(this._nHLow, 1, 9);
            this.dieEditorLayout.Controls.Add(this.lblHeightUpper, 0, 10);
            this.dieEditorLayout.Controls.Add(this._nHUp, 1, 10);
            this.dieEditorLayout.Controls.Add(this.lblVisionHeader, 0, 11);
            this.dieEditorLayout.Controls.Add(this.lblChippingDepth, 0, 12);
            this.dieEditorLayout.Controls.Add(this._nChipDepth, 1, 12);
            this.dieEditorLayout.Controls.Add(this.lblChippingLength, 0, 13);
            this.dieEditorLayout.Controls.Add(this._nChipLen, 1, 13);
            this.dieEditorLayout.Controls.Add(this.lblForeignSize, 0, 14);
            this.dieEditorLayout.Controls.Add(this._nForeign, 1, 14);
            this.dieEditorLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.dieEditorLayout.Location = new System.Drawing.Point(8, 12);
            this.dieEditorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.dieEditorLayout.Name = "dieEditorLayout";
            this.dieEditorLayout.Padding = new System.Windows.Forms.Padding(10);
            this.dieEditorLayout.RowCount = 15;
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.dieEditorLayout.Size = new System.Drawing.Size(1078, 517);
            this.dieEditorLayout.TabIndex = 0;
            // 
            // lblBasicHeader
            // 
            this.lblBasicHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblBasicHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.dieEditorLayout.SetColumnSpan(this.lblBasicHeader, 4);
            this.lblBasicHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBasicHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblBasicHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblBasicHeader.Location = new System.Drawing.Point(13, 10);
            this.lblBasicHeader.Name = "lblBasicHeader";
            this.lblBasicHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblBasicHeader.Size = new System.Drawing.Size(1052, 30);
            this.lblBasicHeader.TabIndex = 0;
            this.lblBasicHeader.Text = "Die specification";
            this.lblBasicHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSpecLibrary
            // 
            this.lblSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpecLibrary.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSpecLibrary.Location = new System.Drawing.Point(13, 40);
            this.lblSpecLibrary.Name = "lblSpecLibrary";
            this.lblSpecLibrary.Size = new System.Drawing.Size(214, 33);
            this.lblSpecLibrary.TabIndex = 25;
            this.lblSpecLibrary.Text = "Spec library";
            this.lblSpecLibrary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbSpecLibrary
            // 
            this._cbSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbSpecLibrary.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSpecLibrary.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._cbSpecLibrary.FormattingEnabled = true;
            this._cbSpecLibrary.Location = new System.Drawing.Point(233, 43);
            this._cbSpecLibrary.Name = "_cbSpecLibrary";
            this._cbSpecLibrary.Size = new System.Drawing.Size(592, 25);
            this._cbSpecLibrary.TabIndex = 26;
            // 
            // btnLoadSpec
            // 
            this.btnLoadSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadSpec.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnLoadSpec.Location = new System.Drawing.Point(831, 43);
            this.btnLoadSpec.Name = "btnLoadSpec";
            this.btnLoadSpec.Size = new System.Drawing.Size(114, 27);
            this.btnLoadSpec.TabIndex = 27;
            this.btnLoadSpec.Text = "Load Spec";
            this.btnLoadSpec.UseVisualStyleBackColor = true;
            this.btnLoadSpec.Click += new System.EventHandler(this.btnLoadSpec_Click);
            // 
            // btnSaveSpec
            // 
            this.btnSaveSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveSpec.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnSaveSpec.Location = new System.Drawing.Point(951, 43);
            this.btnSaveSpec.Name = "btnSaveSpec";
            this.btnSaveSpec.Size = new System.Drawing.Size(114, 27);
            this.btnSaveSpec.TabIndex = 28;
            this.btnSaveSpec.Text = "Save Spec";
            this.btnSaveSpec.UseVisualStyleBackColor = true;
            this.btnSaveSpec.Click += new System.EventHandler(this.btnSaveSpec_Click);
            // 
            // lblName
            // 
            this.lblName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblName.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblName.Location = new System.Drawing.Point(13, 73);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(214, 35);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "Spec name";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbName
            // 
            this.dieEditorLayout.SetColumnSpan(this._tbName, 3);
            this._tbName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbName.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbName.Location = new System.Drawing.Point(233, 76);
            this._tbName.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this._tbName.Name = "_tbName";
            this._tbName.Size = new System.Drawing.Size(832, 23);
            this._tbName.TabIndex = 2;
            // 
            // lblWidth
            // 
            this.lblWidth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWidth.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblWidth.Location = new System.Drawing.Point(13, 108);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(214, 34);
            this.lblWidth.TabIndex = 3;
            this.lblWidth.Text = "Width (mm)";
            this.lblWidth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nW
            // 
            this.dieEditorLayout.SetColumnSpan(this._nW, 3);
            this._nW.DecimalPlaces = 4;
            this._nW.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nW.Font = new System.Drawing.Font("Consolas", 10F);
            this._nW.Location = new System.Drawing.Point(233, 111);
            this._nW.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this._nW.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nW.Name = "_nW";
            this._nW.Size = new System.Drawing.Size(832, 23);
            this._nW.TabIndex = 4;
            this._nW.Value = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            // 
            // lblHeight
            // 
            this.lblHeight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeight.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeight.Location = new System.Drawing.Point(13, 142);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(214, 34);
            this.lblHeight.TabIndex = 5;
            this.lblHeight.Text = "Height (mm)";
            this.lblHeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nH
            // 
            this.dieEditorLayout.SetColumnSpan(this._nH, 3);
            this._nH.DecimalPlaces = 4;
            this._nH.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nH.Font = new System.Drawing.Font("Consolas", 10F);
            this._nH.Location = new System.Drawing.Point(233, 145);
            this._nH.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this._nH.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nH.Name = "_nH";
            this._nH.Size = new System.Drawing.Size(832, 23);
            this._nH.TabIndex = 6;
            this._nH.Value = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            // 
            // lblThickness
            // 
            this.lblThickness.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThickness.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblThickness.Location = new System.Drawing.Point(13, 176);
            this.lblThickness.Name = "lblThickness";
            this.lblThickness.Size = new System.Drawing.Size(214, 30);
            this.lblThickness.TabIndex = 7;
            this.lblThickness.Text = "Thickness (mm)";
            this.lblThickness.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nT
            // 
            this.dieEditorLayout.SetColumnSpan(this._nT, 3);
            this._nT.DecimalPlaces = 4;
            this._nT.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nT.Font = new System.Drawing.Font("Consolas", 10F);
            this._nT.Location = new System.Drawing.Point(233, 179);
            this._nT.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this._nT.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nT.Name = "_nT";
            this._nT.Size = new System.Drawing.Size(832, 23);
            this._nT.TabIndex = 8;
            this._nT.Value = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            // 
            // lblToleranceHeader
            // 
            this.lblToleranceHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblToleranceHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.dieEditorLayout.SetColumnSpan(this.lblToleranceHeader, 4);
            this.lblToleranceHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblToleranceHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblToleranceHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblToleranceHeader.Location = new System.Drawing.Point(13, 206);
            this.lblToleranceHeader.Name = "lblToleranceHeader";
            this.lblToleranceHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblToleranceHeader.Size = new System.Drawing.Size(1052, 34);
            this.lblToleranceHeader.TabIndex = 9;
            this.lblToleranceHeader.Text = "Tolerances (mm)";
            this.lblToleranceHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWidthLower
            // 
            this.lblWidthLower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWidthLower.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblWidthLower.Location = new System.Drawing.Point(13, 240);
            this.lblWidthLower.Name = "lblWidthLower";
            this.lblWidthLower.Size = new System.Drawing.Size(214, 34);
            this.lblWidthLower.TabIndex = 10;
            this.lblWidthLower.Text = "Width lower";
            this.lblWidthLower.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nWLow
            // 
            this.dieEditorLayout.SetColumnSpan(this._nWLow, 3);
            this._nWLow.DecimalPlaces = 4;
            this._nWLow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nWLow.Font = new System.Drawing.Font("Consolas", 10F);
            this._nWLow.Location = new System.Drawing.Point(233, 243);
            this._nWLow.Maximum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this._nWLow.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            -2147483648});
            this._nWLow.Name = "_nWLow";
            this._nWLow.Size = new System.Drawing.Size(832, 23);
            this._nWLow.TabIndex = 11;
            // 
            // lblWidthUpper
            // 
            this.lblWidthUpper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWidthUpper.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblWidthUpper.Location = new System.Drawing.Point(13, 274);
            this.lblWidthUpper.Name = "lblWidthUpper";
            this.lblWidthUpper.Size = new System.Drawing.Size(214, 34);
            this.lblWidthUpper.TabIndex = 12;
            this.lblWidthUpper.Text = "Width upper";
            this.lblWidthUpper.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nWUp
            // 
            this.dieEditorLayout.SetColumnSpan(this._nWUp, 3);
            this._nWUp.DecimalPlaces = 4;
            this._nWUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nWUp.Font = new System.Drawing.Font("Consolas", 10F);
            this._nWUp.Location = new System.Drawing.Point(233, 277);
            this._nWUp.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._nWUp.Name = "_nWUp";
            this._nWUp.Size = new System.Drawing.Size(832, 23);
            this._nWUp.TabIndex = 13;
            // 
            // lblHeightLower
            // 
            this.lblHeightLower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeightLower.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeightLower.Location = new System.Drawing.Point(13, 308);
            this.lblHeightLower.Name = "lblHeightLower";
            this.lblHeightLower.Size = new System.Drawing.Size(214, 34);
            this.lblHeightLower.TabIndex = 14;
            this.lblHeightLower.Text = "Height lower";
            this.lblHeightLower.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nHLow
            // 
            this.dieEditorLayout.SetColumnSpan(this._nHLow, 3);
            this._nHLow.DecimalPlaces = 4;
            this._nHLow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nHLow.Font = new System.Drawing.Font("Consolas", 10F);
            this._nHLow.Location = new System.Drawing.Point(233, 311);
            this._nHLow.Maximum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this._nHLow.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            -2147483648});
            this._nHLow.Name = "_nHLow";
            this._nHLow.Size = new System.Drawing.Size(832, 23);
            this._nHLow.TabIndex = 15;
            // 
            // lblHeightUpper
            // 
            this.lblHeightUpper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeightUpper.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeightUpper.Location = new System.Drawing.Point(13, 342);
            this.lblHeightUpper.Name = "lblHeightUpper";
            this.lblHeightUpper.Size = new System.Drawing.Size(214, 30);
            this.lblHeightUpper.TabIndex = 16;
            this.lblHeightUpper.Text = "Height upper";
            this.lblHeightUpper.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nHUp
            // 
            this.dieEditorLayout.SetColumnSpan(this._nHUp, 3);
            this._nHUp.DecimalPlaces = 4;
            this._nHUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nHUp.Font = new System.Drawing.Font("Consolas", 10F);
            this._nHUp.Location = new System.Drawing.Point(233, 345);
            this._nHUp.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._nHUp.Name = "_nHUp";
            this._nHUp.Size = new System.Drawing.Size(832, 23);
            this._nHUp.TabIndex = 17;
            // 
            // lblVisionHeader
            // 
            this.lblVisionHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblVisionHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.dieEditorLayout.SetColumnSpan(this.lblVisionHeader, 4);
            this.lblVisionHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblVisionHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblVisionHeader.Location = new System.Drawing.Point(13, 372);
            this.lblVisionHeader.Name = "lblVisionHeader";
            this.lblVisionHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblVisionHeader.Size = new System.Drawing.Size(1052, 34);
            this.lblVisionHeader.TabIndex = 18;
            this.lblVisionHeader.Text = "Vision inspection thresholds (mm)";
            this.lblVisionHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblChippingDepth
            // 
            this.lblChippingDepth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChippingDepth.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblChippingDepth.Location = new System.Drawing.Point(13, 406);
            this.lblChippingDepth.Name = "lblChippingDepth";
            this.lblChippingDepth.Size = new System.Drawing.Size(214, 34);
            this.lblChippingDepth.TabIndex = 19;
            this.lblChippingDepth.Text = "Chipping depth max";
            this.lblChippingDepth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nChipDepth
            // 
            this.dieEditorLayout.SetColumnSpan(this._nChipDepth, 3);
            this._nChipDepth.DecimalPlaces = 4;
            this._nChipDepth.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nChipDepth.Font = new System.Drawing.Font("Consolas", 10F);
            this._nChipDepth.Location = new System.Drawing.Point(233, 409);
            this._nChipDepth.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this._nChipDepth.Name = "_nChipDepth";
            this._nChipDepth.Size = new System.Drawing.Size(832, 23);
            this._nChipDepth.TabIndex = 20;
            // 
            // lblChippingLength
            // 
            this.lblChippingLength.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChippingLength.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblChippingLength.Location = new System.Drawing.Point(13, 440);
            this.lblChippingLength.Name = "lblChippingLength";
            this.lblChippingLength.Size = new System.Drawing.Size(214, 34);
            this.lblChippingLength.TabIndex = 21;
            this.lblChippingLength.Text = "Chipping length max";
            this.lblChippingLength.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nChipLen
            // 
            this.dieEditorLayout.SetColumnSpan(this._nChipLen, 3);
            this._nChipLen.DecimalPlaces = 4;
            this._nChipLen.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nChipLen.Font = new System.Drawing.Font("Consolas", 10F);
            this._nChipLen.Location = new System.Drawing.Point(233, 443);
            this._nChipLen.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this._nChipLen.Name = "_nChipLen";
            this._nChipLen.Size = new System.Drawing.Size(832, 23);
            this._nChipLen.TabIndex = 22;
            // 
            // lblForeignSize
            // 
            this.lblForeignSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblForeignSize.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblForeignSize.Location = new System.Drawing.Point(13, 474);
            this.lblForeignSize.Name = "lblForeignSize";
            this.lblForeignSize.Size = new System.Drawing.Size(214, 34);
            this.lblForeignSize.TabIndex = 23;
            this.lblForeignSize.Text = "Foreign size max";
            this.lblForeignSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nForeign
            // 
            this.dieEditorLayout.SetColumnSpan(this._nForeign, 3);
            this._nForeign.DecimalPlaces = 5;
            this._nForeign.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nForeign.Font = new System.Drawing.Font("Consolas", 10F);
            this._nForeign.Location = new System.Drawing.Point(233, 477);
            this._nForeign.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nForeign.Name = "_nForeign";
            this._nForeign.Size = new System.Drawing.Size(832, 23);
            this._nForeign.TabIndex = 24;
            // 
            // DieSubsetPage
            // 
            this.Name = "DieSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.dieEditorLayout.ResumeLayout(false);
            this.dieEditorLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nT)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nWLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nWUp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nHLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nHUp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nChipDepth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nChipLen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nForeign)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
