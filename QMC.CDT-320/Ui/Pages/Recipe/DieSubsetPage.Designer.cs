using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class DieSubsetPage
    {
        private TableLayoutPanel dieEditorLayout;
        private GroupBox grpDieSpec;
        private TableLayoutPanel tlpDie;
        private GroupBox grpTolerance;
        private TableLayoutPanel tlpTol;
        private GroupBox grpVision;
        private TableLayoutPanel tlpVis;
        private GroupBox grpSaveGuide;
        private TableLayoutPanel tlpSaveGuide;
        private Label _lblCurrentRecipeInfo;
        private Label lblSaveSequence;
        private Label lblButtonMeaning;
        private GroupBox grpOperationStatus;
        private TextBox _txtOperationStatus;
        private Label lblSpecLibrary;
        private ComboBox _cbSpecLibrary;
        private Button btnLoadSpec;
        private Button btnSaveSpec;
        private Label lblName;
        private Label lblWidth;
        private Label lblHeight;
        private Label lblThickness;
        private Label lblWidthLower;
        private Label lblWidthUpper;
        private Label lblHeightLower;
        private Label lblHeightUpper;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DieSubsetPage));
            this.dieEditorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpDieSpec = new System.Windows.Forms.GroupBox();
            this.tlpDie = new System.Windows.Forms.TableLayoutPanel();
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
            this.grpTolerance = new System.Windows.Forms.GroupBox();
            this.tlpTol = new System.Windows.Forms.TableLayoutPanel();
            this.lblWidthLower = new System.Windows.Forms.Label();
            this._nWLow = new System.Windows.Forms.NumericUpDown();
            this.lblWidthUpper = new System.Windows.Forms.Label();
            this._nWUp = new System.Windows.Forms.NumericUpDown();
            this.lblHeightLower = new System.Windows.Forms.Label();
            this._nHLow = new System.Windows.Forms.NumericUpDown();
            this.lblHeightUpper = new System.Windows.Forms.Label();
            this._nHUp = new System.Windows.Forms.NumericUpDown();
            this.grpVision = new System.Windows.Forms.GroupBox();
            this.tlpVis = new System.Windows.Forms.TableLayoutPanel();
            this.lblChippingDepth = new System.Windows.Forms.Label();
            this._nChipDepth = new System.Windows.Forms.NumericUpDown();
            this.lblChippingLength = new System.Windows.Forms.Label();
            this._nChipLen = new System.Windows.Forms.NumericUpDown();
            this.lblForeignSize = new System.Windows.Forms.Label();
            this._nForeign = new System.Windows.Forms.NumericUpDown();
            this.grpSaveGuide = new System.Windows.Forms.GroupBox();
            this.tlpSaveGuide = new System.Windows.Forms.TableLayoutPanel();
            this._lblCurrentRecipeInfo = new System.Windows.Forms.Label();
            this.lblSaveSequence = new System.Windows.Forms.Label();
            this.lblButtonMeaning = new System.Windows.Forms.Label();
            this.grpOperationStatus = new System.Windows.Forms.GroupBox();
            this._txtOperationStatus = new System.Windows.Forms.TextBox();
            this._editorPanel.SuspendLayout();
            this.dieEditorLayout.SuspendLayout();
            this.grpDieSpec.SuspendLayout();
            this.tlpDie.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nT)).BeginInit();
            this.grpTolerance.SuspendLayout();
            this.tlpTol.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nWLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nWUp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nHLow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nHUp)).BeginInit();
            this.grpVision.SuspendLayout();
            this.tlpVis.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nChipDepth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nChipLen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nForeign)).BeginInit();
            this.grpSaveGuide.SuspendLayout();
            this.tlpSaveGuide.SuspendLayout();
            this.grpOperationStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // _editorPanel
            // 
            this._editorPanel.BackColor = System.Drawing.Color.White;
            this._editorPanel.Controls.Add(this.dieEditorLayout);
            this._editorPanel.Size = new System.Drawing.Size(1094, 676);
            // 
            // _lblProject
            // 
            this._lblProject.Size = new System.Drawing.Size(794, 36);
            // 
            // dieEditorLayout
            // 
            this.dieEditorLayout.BackColor = System.Drawing.Color.White;
            this.dieEditorLayout.ColumnCount = 2;
            this.dieEditorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dieEditorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dieEditorLayout.Controls.Add(this.grpDieSpec, 0, 0);
            this.dieEditorLayout.Controls.Add(this.grpTolerance, 0, 2);
            this.dieEditorLayout.Controls.Add(this.grpVision, 0, 4);
            this.dieEditorLayout.Controls.Add(this.grpSaveGuide, 1, 0);
            this.dieEditorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dieEditorLayout.Location = new System.Drawing.Point(8, 12);
            this.dieEditorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.dieEditorLayout.Name = "dieEditorLayout";
            this.dieEditorLayout.RowCount = 5;
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 167F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.dieEditorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 133F));
            this.dieEditorLayout.Size = new System.Drawing.Size(1078, 656);
            this.dieEditorLayout.TabIndex = 0;
            // 
            // grpDieSpec
            // 
            this.grpDieSpec.BackColor = System.Drawing.Color.White;
            this.grpDieSpec.Controls.Add(this.tlpDie);
            this.grpDieSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDieSpec.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpDieSpec.Location = new System.Drawing.Point(0, 0);
            this.grpDieSpec.Margin = new System.Windows.Forms.Padding(0);
            this.grpDieSpec.Name = "grpDieSpec";
            this.grpDieSpec.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpDieSpec.Size = new System.Drawing.Size(539, 200);
            this.grpDieSpec.TabIndex = 0;
            this.grpDieSpec.TabStop = false;
            this.grpDieSpec.Text = "다이 사양";
            // 
            // tlpDie
            // 
            this.tlpDie.BackColor = System.Drawing.Color.White;
            this.tlpDie.ColumnCount = 4;
            this.tlpDie.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpDie.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpDie.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tlpDie.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tlpDie.Controls.Add(this.lblSpecLibrary, 0, 0);
            this.tlpDie.Controls.Add(this._cbSpecLibrary, 1, 0);
            this.tlpDie.Controls.Add(this.btnLoadSpec, 2, 0);
            this.tlpDie.Controls.Add(this.btnSaveSpec, 3, 0);
            this.tlpDie.Controls.Add(this.lblName, 0, 1);
            this.tlpDie.Controls.Add(this._tbName, 1, 1);
            this.tlpDie.Controls.Add(this.lblWidth, 0, 2);
            this.tlpDie.Controls.Add(this._nW, 1, 2);
            this.tlpDie.Controls.Add(this.lblHeight, 0, 3);
            this.tlpDie.Controls.Add(this._nH, 1, 3);
            this.tlpDie.Controls.Add(this.lblThickness, 0, 4);
            this.tlpDie.Controls.Add(this._nT, 1, 4);
            this.tlpDie.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpDie.Location = new System.Drawing.Point(6, 20);
            this.tlpDie.Margin = new System.Windows.Forms.Padding(0);
            this.tlpDie.Name = "tlpDie";
            this.tlpDie.RowCount = 5;
            this.tlpDie.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpDie.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpDie.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpDie.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpDie.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpDie.Size = new System.Drawing.Size(527, 174);
            this.tlpDie.TabIndex = 0;
            // 
            // lblSpecLibrary
            // 
            this.lblSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpecLibrary.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSpecLibrary.Location = new System.Drawing.Point(3, 0);
            this.lblSpecLibrary.Name = "lblSpecLibrary";
            this.lblSpecLibrary.Size = new System.Drawing.Size(214, 33);
            this.lblSpecLibrary.TabIndex = 25;
            this.lblSpecLibrary.Text = "저장 사양";
            this.lblSpecLibrary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbSpecLibrary
            // 
            this._cbSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbSpecLibrary.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSpecLibrary.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this._cbSpecLibrary.FormattingEnabled = true;
            this._cbSpecLibrary.Location = new System.Drawing.Point(223, 3);
            this._cbSpecLibrary.Name = "_cbSpecLibrary";
            this._cbSpecLibrary.Size = new System.Drawing.Size(61, 25);
            this._cbSpecLibrary.TabIndex = 26;
            // 
            // btnLoadSpec
            // 
            this.btnLoadSpec.BackColor = System.Drawing.Color.White;
            this.btnLoadSpec.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadSpec.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(205)))), ((int)(((byte)(212)))));
            this.btnLoadSpec.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(230)))), ((int)(((byte)(236)))));
            this.btnLoadSpec.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            this.btnLoadSpec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadSpec.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnLoadSpec.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(80)))));
            this.btnLoadSpec.Location = new System.Drawing.Point(290, 3);
            this.btnLoadSpec.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this.btnLoadSpec.Name = "btnLoadSpec";
            this.btnLoadSpec.Size = new System.Drawing.Size(114, 26);
            this.btnLoadSpec.TabIndex = 27;
            this.btnLoadSpec.Text = "사양 불러오기";
            this.btnLoadSpec.UseVisualStyleBackColor = false;
            this.btnLoadSpec.Click += new System.EventHandler(this.btnLoadSpec_Click);
            // 
            // btnSaveSpec
            // 
            this.btnSaveSpec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(88)))), ((int)(((byte)(31)))));
            this.btnSaveSpec.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSaveSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveSpec.FlatAppearance.BorderSize = 0;
            this.btnSaveSpec.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(72)))), ((int)(((byte)(22)))));
            this.btnSaveSpec.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(110)))), ((int)(((byte)(55)))));
            this.btnSaveSpec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveSpec.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnSaveSpec.ForeColor = System.Drawing.Color.White;
            this.btnSaveSpec.Location = new System.Drawing.Point(410, 3);
            this.btnSaveSpec.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this.btnSaveSpec.Name = "btnSaveSpec";
            this.btnSaveSpec.Size = new System.Drawing.Size(114, 26);
            this.btnSaveSpec.TabIndex = 28;
            this.btnSaveSpec.Text = "사양 저장·적용";
            this.btnSaveSpec.UseVisualStyleBackColor = false;
            this.btnSaveSpec.Click += new System.EventHandler(this.btnSaveSpec_Click);
            // 
            // lblName
            // 
            this.lblName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblName.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblName.Location = new System.Drawing.Point(3, 33);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(214, 34);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "사양 이름";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbName
            // 
            this.tlpDie.SetColumnSpan(this._tbName, 3);
            this._tbName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbName.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbName.Location = new System.Drawing.Point(223, 36);
            this._tbName.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this._tbName.Name = "_tbName";
            this._tbName.Size = new System.Drawing.Size(301, 23);
            this._tbName.TabIndex = 2;
            // 
            // lblWidth
            // 
            this.lblWidth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWidth.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblWidth.Location = new System.Drawing.Point(3, 67);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(214, 34);
            this.lblWidth.TabIndex = 3;
            this.lblWidth.Text = "가로 (mm)";
            this.lblWidth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nW
            // 
            this.tlpDie.SetColumnSpan(this._nW, 3);
            this._nW.DecimalPlaces = 4;
            this._nW.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nW.Font = new System.Drawing.Font("Consolas", 10F);
            this._nW.Location = new System.Drawing.Point(223, 70);
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
            this._nW.Size = new System.Drawing.Size(301, 23);
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
            this.lblHeight.Location = new System.Drawing.Point(3, 101);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(214, 34);
            this.lblHeight.TabIndex = 5;
            this.lblHeight.Text = "세로 (mm)";
            this.lblHeight.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nH
            // 
            this.tlpDie.SetColumnSpan(this._nH, 3);
            this._nH.DecimalPlaces = 4;
            this._nH.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nH.Font = new System.Drawing.Font("Consolas", 10F);
            this._nH.Location = new System.Drawing.Point(223, 104);
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
            this._nH.Size = new System.Drawing.Size(301, 23);
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
            this.lblThickness.Location = new System.Drawing.Point(3, 135);
            this.lblThickness.Name = "lblThickness";
            this.lblThickness.Size = new System.Drawing.Size(214, 39);
            this.lblThickness.TabIndex = 7;
            this.lblThickness.Text = "두께 (mm)";
            this.lblThickness.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nT
            // 
            this.tlpDie.SetColumnSpan(this._nT, 3);
            this._nT.DecimalPlaces = 4;
            this._nT.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nT.Font = new System.Drawing.Font("Consolas", 10F);
            this._nT.Location = new System.Drawing.Point(223, 138);
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
            this._nT.Size = new System.Drawing.Size(301, 23);
            this._nT.TabIndex = 8;
            this._nT.Value = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            // 
            // grpTolerance
            // 
            this.grpTolerance.BackColor = System.Drawing.Color.White;
            this.grpTolerance.Controls.Add(this.tlpTol);
            this.grpTolerance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpTolerance.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpTolerance.Location = new System.Drawing.Point(0, 278);
            this.grpTolerance.Margin = new System.Windows.Forms.Padding(0);
            this.grpTolerance.Name = "grpTolerance";
            this.grpTolerance.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpTolerance.Size = new System.Drawing.Size(539, 167);
            this.grpTolerance.TabIndex = 1;
            this.grpTolerance.TabStop = false;
            this.grpTolerance.Text = "허용 오차 (mm)";
            // 
            // tlpTol
            // 
            this.tlpTol.BackColor = System.Drawing.Color.White;
            this.tlpTol.ColumnCount = 2;
            this.tlpTol.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpTol.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpTol.Controls.Add(this.lblWidthLower, 0, 0);
            this.tlpTol.Controls.Add(this._nWLow, 1, 0);
            this.tlpTol.Controls.Add(this.lblWidthUpper, 0, 1);
            this.tlpTol.Controls.Add(this._nWUp, 1, 1);
            this.tlpTol.Controls.Add(this.lblHeightLower, 0, 2);
            this.tlpTol.Controls.Add(this._nHLow, 1, 2);
            this.tlpTol.Controls.Add(this.lblHeightUpper, 0, 3);
            this.tlpTol.Controls.Add(this._nHUp, 1, 3);
            this.tlpTol.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpTol.Location = new System.Drawing.Point(6, 20);
            this.tlpTol.Margin = new System.Windows.Forms.Padding(0);
            this.tlpTol.Name = "tlpTol";
            this.tlpTol.RowCount = 4;
            this.tlpTol.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpTol.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpTol.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpTol.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpTol.Size = new System.Drawing.Size(527, 141);
            this.tlpTol.TabIndex = 0;
            // 
            // lblWidthLower
            // 
            this.lblWidthLower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWidthLower.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblWidthLower.Location = new System.Drawing.Point(3, 0);
            this.lblWidthLower.Name = "lblWidthLower";
            this.lblWidthLower.Size = new System.Drawing.Size(214, 34);
            this.lblWidthLower.TabIndex = 10;
            this.lblWidthLower.Text = "가로 하한";
            this.lblWidthLower.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nWLow
            // 
            this._nWLow.DecimalPlaces = 4;
            this._nWLow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nWLow.Font = new System.Drawing.Font("Consolas", 10F);
            this._nWLow.Location = new System.Drawing.Point(223, 3);
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
            this._nWLow.Size = new System.Drawing.Size(301, 23);
            this._nWLow.TabIndex = 11;
            // 
            // lblWidthUpper
            // 
            this.lblWidthUpper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWidthUpper.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblWidthUpper.Location = new System.Drawing.Point(3, 34);
            this.lblWidthUpper.Name = "lblWidthUpper";
            this.lblWidthUpper.Size = new System.Drawing.Size(214, 34);
            this.lblWidthUpper.TabIndex = 12;
            this.lblWidthUpper.Text = "가로 상한";
            this.lblWidthUpper.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nWUp
            // 
            this._nWUp.DecimalPlaces = 4;
            this._nWUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nWUp.Font = new System.Drawing.Font("Consolas", 10F);
            this._nWUp.Location = new System.Drawing.Point(223, 37);
            this._nWUp.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._nWUp.Name = "_nWUp";
            this._nWUp.Size = new System.Drawing.Size(301, 23);
            this._nWUp.TabIndex = 13;
            // 
            // lblHeightLower
            // 
            this.lblHeightLower.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeightLower.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeightLower.Location = new System.Drawing.Point(3, 68);
            this.lblHeightLower.Name = "lblHeightLower";
            this.lblHeightLower.Size = new System.Drawing.Size(214, 34);
            this.lblHeightLower.TabIndex = 14;
            this.lblHeightLower.Text = "세로 하한";
            this.lblHeightLower.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nHLow
            // 
            this._nHLow.DecimalPlaces = 4;
            this._nHLow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nHLow.Font = new System.Drawing.Font("Consolas", 10F);
            this._nHLow.Location = new System.Drawing.Point(223, 71);
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
            this._nHLow.Size = new System.Drawing.Size(301, 23);
            this._nHLow.TabIndex = 15;
            // 
            // lblHeightUpper
            // 
            this.lblHeightUpper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeightUpper.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblHeightUpper.Location = new System.Drawing.Point(3, 102);
            this.lblHeightUpper.Name = "lblHeightUpper";
            this.lblHeightUpper.Size = new System.Drawing.Size(214, 39);
            this.lblHeightUpper.TabIndex = 16;
            this.lblHeightUpper.Text = "세로 상한";
            this.lblHeightUpper.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nHUp
            // 
            this._nHUp.DecimalPlaces = 4;
            this._nHUp.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nHUp.Font = new System.Drawing.Font("Consolas", 10F);
            this._nHUp.Location = new System.Drawing.Point(223, 105);
            this._nHUp.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._nHUp.Name = "_nHUp";
            this._nHUp.Size = new System.Drawing.Size(301, 23);
            this._nHUp.TabIndex = 17;
            // 
            // grpVision
            // 
            this.grpVision.BackColor = System.Drawing.Color.White;
            this.grpVision.Controls.Add(this.tlpVis);
            this.grpVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpVision.Location = new System.Drawing.Point(0, 523);
            this.grpVision.Margin = new System.Windows.Forms.Padding(0);
            this.grpVision.Name = "grpVision";
            this.grpVision.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpVision.Size = new System.Drawing.Size(539, 133);
            this.grpVision.TabIndex = 2;
            this.grpVision.TabStop = false;
            this.grpVision.Text = "비전 검사 기준 (mm)";
            // 
            // tlpVis
            // 
            this.tlpVis.BackColor = System.Drawing.Color.White;
            this.tlpVis.ColumnCount = 2;
            this.tlpVis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpVis.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpVis.Controls.Add(this.lblChippingDepth, 0, 0);
            this.tlpVis.Controls.Add(this._nChipDepth, 1, 0);
            this.tlpVis.Controls.Add(this.lblChippingLength, 0, 1);
            this.tlpVis.Controls.Add(this._nChipLen, 1, 1);
            this.tlpVis.Controls.Add(this.lblForeignSize, 0, 2);
            this.tlpVis.Controls.Add(this._nForeign, 1, 2);
            this.tlpVis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpVis.Location = new System.Drawing.Point(6, 20);
            this.tlpVis.Margin = new System.Windows.Forms.Padding(0);
            this.tlpVis.Name = "tlpVis";
            this.tlpVis.RowCount = 3;
            this.tlpVis.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpVis.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpVis.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpVis.Size = new System.Drawing.Size(527, 107);
            this.tlpVis.TabIndex = 0;
            // 
            // lblChippingDepth
            // 
            this.lblChippingDepth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChippingDepth.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblChippingDepth.Location = new System.Drawing.Point(3, 0);
            this.lblChippingDepth.Name = "lblChippingDepth";
            this.lblChippingDepth.Size = new System.Drawing.Size(214, 34);
            this.lblChippingDepth.TabIndex = 19;
            this.lblChippingDepth.Text = "치핑 깊이 최대";
            this.lblChippingDepth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nChipDepth
            // 
            this._nChipDepth.DecimalPlaces = 4;
            this._nChipDepth.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nChipDepth.Font = new System.Drawing.Font("Consolas", 10F);
            this._nChipDepth.Location = new System.Drawing.Point(223, 3);
            this._nChipDepth.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this._nChipDepth.Name = "_nChipDepth";
            this._nChipDepth.Size = new System.Drawing.Size(301, 23);
            this._nChipDepth.TabIndex = 20;
            // 
            // lblChippingLength
            // 
            this.lblChippingLength.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblChippingLength.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblChippingLength.Location = new System.Drawing.Point(3, 34);
            this.lblChippingLength.Name = "lblChippingLength";
            this.lblChippingLength.Size = new System.Drawing.Size(214, 34);
            this.lblChippingLength.TabIndex = 21;
            this.lblChippingLength.Text = "치핑 길이 최대";
            this.lblChippingLength.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nChipLen
            // 
            this._nChipLen.DecimalPlaces = 4;
            this._nChipLen.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nChipLen.Font = new System.Drawing.Font("Consolas", 10F);
            this._nChipLen.Location = new System.Drawing.Point(223, 37);
            this._nChipLen.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this._nChipLen.Name = "_nChipLen";
            this._nChipLen.Size = new System.Drawing.Size(301, 23);
            this._nChipLen.TabIndex = 22;
            // 
            // lblForeignSize
            // 
            this.lblForeignSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblForeignSize.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblForeignSize.Location = new System.Drawing.Point(3, 68);
            this.lblForeignSize.Name = "lblForeignSize";
            this.lblForeignSize.Size = new System.Drawing.Size(214, 39);
            this.lblForeignSize.TabIndex = 23;
            this.lblForeignSize.Text = "이물 크기 최대";
            this.lblForeignSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nForeign
            // 
            this._nForeign.DecimalPlaces = 5;
            this._nForeign.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nForeign.Font = new System.Drawing.Font("Consolas", 10F);
            this._nForeign.Location = new System.Drawing.Point(223, 71);
            this._nForeign.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nForeign.Name = "_nForeign";
            this._nForeign.Size = new System.Drawing.Size(301, 23);
            this._nForeign.TabIndex = 24;
            // 
            // grpSaveGuide
            // 
            this.grpSaveGuide.BackColor = System.Drawing.Color.White;
            this.grpSaveGuide.Controls.Add(this.tlpSaveGuide);
            this.grpSaveGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSaveGuide.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSaveGuide.Location = new System.Drawing.Point(559, 0);
            this.grpSaveGuide.Margin = new System.Windows.Forms.Padding(20, 0, 4, 0);
            this.grpSaveGuide.Name = "grpSaveGuide";
            this.grpSaveGuide.Padding = new System.Windows.Forms.Padding(12, 8, 12, 12);
            this.dieEditorLayout.SetRowSpan(this.grpSaveGuide, 5);
            this.grpSaveGuide.Size = new System.Drawing.Size(515, 656);
            this.grpSaveGuide.TabIndex = 3;
            this.grpSaveGuide.TabStop = false;
            this.grpSaveGuide.Text = "저장 / 불러오기 순서";
            // 
            // tlpSaveGuide
            // 
            this.tlpSaveGuide.BackColor = System.Drawing.Color.White;
            this.tlpSaveGuide.ColumnCount = 1;
            this.tlpSaveGuide.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpSaveGuide.Controls.Add(this._lblCurrentRecipeInfo, 0, 0);
            this.tlpSaveGuide.Controls.Add(this.lblSaveSequence, 0, 1);
            this.tlpSaveGuide.Controls.Add(this.lblButtonMeaning, 0, 2);
            this.tlpSaveGuide.Controls.Add(this.grpOperationStatus, 0, 3);
            this.tlpSaveGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpSaveGuide.Location = new System.Drawing.Point(12, 26);
            this.tlpSaveGuide.Name = "tlpSaveGuide";
            this.tlpSaveGuide.Padding = new System.Windows.Forms.Padding(2);
            this.tlpSaveGuide.RowCount = 4;
            this.tlpSaveGuide.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tlpSaveGuide.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 215F));
            this.tlpSaveGuide.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 135F));
            this.tlpSaveGuide.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpSaveGuide.Size = new System.Drawing.Size(491, 618);
            this.tlpSaveGuide.TabIndex = 0;
            // 
            // _lblCurrentRecipeInfo
            // 
            this._lblCurrentRecipeInfo.AutoEllipsis = true;
            this._lblCurrentRecipeInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(245)))), ((int)(((byte)(248)))));
            this._lblCurrentRecipeInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblCurrentRecipeInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblCurrentRecipeInfo.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this._lblCurrentRecipeInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(70)))));
            this._lblCurrentRecipeInfo.Location = new System.Drawing.Point(2, 5);
            this._lblCurrentRecipeInfo.Margin = new System.Windows.Forms.Padding(0, 3, 0, 8);
            this._lblCurrentRecipeInfo.Name = "_lblCurrentRecipeInfo";
            this._lblCurrentRecipeInfo.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this._lblCurrentRecipeInfo.Size = new System.Drawing.Size(487, 89);
            this._lblCurrentRecipeInfo.TabIndex = 0;
            this._lblCurrentRecipeInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSaveSequence
            // 
            this.lblSaveSequence.BackColor = System.Drawing.Color.White;
            this.lblSaveSequence.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSaveSequence.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSaveSequence.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.lblSaveSequence.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.lblSaveSequence.Location = new System.Drawing.Point(2, 102);
            this.lblSaveSequence.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblSaveSequence.Name = "lblSaveSequence";
            this.lblSaveSequence.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.lblSaveSequence.Size = new System.Drawing.Size(487, 207);
            this.lblSaveSequence.TabIndex = 1;
            this.lblSaveSequence.Text = resources.GetString("lblSaveSequence.Text");
            // 
            // lblButtonMeaning
            // 
            this.lblButtonMeaning.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(253)))), ((int)(((byte)(248)))), ((int)(((byte)(238)))));
            this.lblButtonMeaning.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblButtonMeaning.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblButtonMeaning.Font = new System.Drawing.Font("맑은 고딕", 9.25F);
            this.lblButtonMeaning.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(55)))), ((int)(((byte)(35)))));
            this.lblButtonMeaning.Location = new System.Drawing.Point(2, 317);
            this.lblButtonMeaning.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblButtonMeaning.Name = "lblButtonMeaning";
            this.lblButtonMeaning.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.lblButtonMeaning.Size = new System.Drawing.Size(487, 127);
            this.lblButtonMeaning.TabIndex = 2;
            this.lblButtonMeaning.Text = "버튼 의미\r\n• 사양 불러오기 : 저장 사양 → 화면 (아직 미적용)\r\n• 상단 저장 : 화면 → 현재 레시피 + 연결 맵 재생성\r\n• 사양 저장" +
    "·적용 : library 저장 + 현재 Recipe 적용\r\n• Reload : 저장하지 않은 변경을 버리고 Recipe 재로드";
            // 
            // grpOperationStatus
            // 
            this.grpOperationStatus.Controls.Add(this._txtOperationStatus);
            this.grpOperationStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOperationStatus.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.grpOperationStatus.Location = new System.Drawing.Point(2, 452);
            this.grpOperationStatus.Margin = new System.Windows.Forms.Padding(0);
            this.grpOperationStatus.Name = "grpOperationStatus";
            this.grpOperationStatus.Padding = new System.Windows.Forms.Padding(8, 4, 8, 8);
            this.grpOperationStatus.Size = new System.Drawing.Size(487, 164);
            this.grpOperationStatus.TabIndex = 3;
            this.grpOperationStatus.TabStop = false;
            this.grpOperationStatus.Text = "현재 상태 / 저장 결과";
            // 
            // _txtOperationStatus
            // 
            this._txtOperationStatus.BackColor = System.Drawing.Color.White;
            this._txtOperationStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._txtOperationStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtOperationStatus.Font = new System.Drawing.Font("맑은 고딕", 9.25F);
            this._txtOperationStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(70)))));
            this._txtOperationStatus.Location = new System.Drawing.Point(8, 21);
            this._txtOperationStatus.Multiline = true;
            this._txtOperationStatus.Name = "_txtOperationStatus";
            this._txtOperationStatus.ReadOnly = true;
            this._txtOperationStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtOperationStatus.Size = new System.Drawing.Size(471, 135);
            this._txtOperationStatus.TabIndex = 0;
            // 
            // DieSubsetPage
            // 
            this.Name = "DieSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.dieEditorLayout.ResumeLayout(false);
            this.grpDieSpec.ResumeLayout(false);
            this.tlpDie.ResumeLayout(false);
            this.tlpDie.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nT)).EndInit();
            this.grpTolerance.ResumeLayout(false);
            this.tlpTol.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nWLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nWUp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nHLow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nHUp)).EndInit();
            this.grpVision.ResumeLayout(false);
            this.tlpVis.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nChipDepth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nChipLen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nForeign)).EndInit();
            this.grpSaveGuide.ResumeLayout(false);
            this.tlpSaveGuide.ResumeLayout(false);
            this.grpOperationStatus.ResumeLayout(false);
            this.grpOperationStatus.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
