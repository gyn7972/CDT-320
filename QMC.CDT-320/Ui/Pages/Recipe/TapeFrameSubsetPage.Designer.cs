using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class TapeFrameSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private GroupBox grpFrame;
        private TableLayoutPanel tlpFrame;
        private Label lblSpecLibrary;
        private ComboBox _cbSpecLibrary;
        private Button btnLoadSpec;
        private Button btnSaveSpec;
        private Label lblName;
        private TextBox _tbName;
        private Label lblGridX;
        private NumericUpDown _nGridX;
        private Label lblGridY;
        private NumericUpDown _nGridY;
        private Label lblPitchX;
        private NumericUpDown _nPitchX;
        private Label lblPitchY;
        private NumericUpDown _nPitchY;
        private Label lblDiameter;
        private NumericUpDown _nDiameter;
        private Label lblRotate;
        private ComboBox _cbRotate;
        private GroupBox grpWafer;
        private TableLayoutPanel tlpWafer;
        private Label lblWaferRole;
        private ComboBox _cbWaferRole;
        private Button _btnImportWaferMap;
        private Label lblMapFile;
        private Label _lblMapFileValue;
        private Label lblDieSizeX;
        private NumericUpDown _nDieSizeX;
        private Label lblDieSizeY;
        private NumericUpDown _nDieSizeY;
        private Label lblEdgeMode;
        private ComboBox _cbEdgeSkipMode;
        private Label lblEdgeLR;
        private NumericUpDown _nSideEdgeSkip;
        private Label lblEdgeTB;
        private NumericUpDown _nTopBottomEdgeSkip;

        private void InitializeComponent()
        {
            this.editorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpFrame = new System.Windows.Forms.GroupBox();
            this.tlpFrame = new System.Windows.Forms.TableLayoutPanel();
            this.lblSpecLibrary = new System.Windows.Forms.Label();
            this._cbSpecLibrary = new System.Windows.Forms.ComboBox();
            this.btnLoadSpec = new System.Windows.Forms.Button();
            this.btnSaveSpec = new System.Windows.Forms.Button();
            this.lblName = new System.Windows.Forms.Label();
            this._tbName = new System.Windows.Forms.TextBox();
            this.lblGridX = new System.Windows.Forms.Label();
            this._nGridX = new System.Windows.Forms.NumericUpDown();
            this.lblGridY = new System.Windows.Forms.Label();
            this._nGridY = new System.Windows.Forms.NumericUpDown();
            this.lblPitchX = new System.Windows.Forms.Label();
            this._nPitchX = new System.Windows.Forms.NumericUpDown();
            this.lblPitchY = new System.Windows.Forms.Label();
            this._nPitchY = new System.Windows.Forms.NumericUpDown();
            this.lblDiameter = new System.Windows.Forms.Label();
            this._nDiameter = new System.Windows.Forms.NumericUpDown();
            this.lblRotate = new System.Windows.Forms.Label();
            this._cbRotate = new System.Windows.Forms.ComboBox();
            this.grpWafer = new System.Windows.Forms.GroupBox();
            this.tlpWafer = new System.Windows.Forms.TableLayoutPanel();
            this.lblWaferRole = new System.Windows.Forms.Label();
            this._cbWaferRole = new System.Windows.Forms.ComboBox();
            this._btnImportWaferMap = new System.Windows.Forms.Button();
            this.lblMapFile = new System.Windows.Forms.Label();
            this._lblMapFileValue = new System.Windows.Forms.Label();
            this.lblDieSizeX = new System.Windows.Forms.Label();
            this._nDieSizeX = new System.Windows.Forms.NumericUpDown();
            this.lblDieSizeY = new System.Windows.Forms.Label();
            this._nDieSizeY = new System.Windows.Forms.NumericUpDown();
            this.lblEdgeMode = new System.Windows.Forms.Label();
            this._cbEdgeSkipMode = new System.Windows.Forms.ComboBox();
            this.lblEdgeLR = new System.Windows.Forms.Label();
            this._nSideEdgeSkip = new System.Windows.Forms.NumericUpDown();
            this.lblEdgeTB = new System.Windows.Forms.Label();
            this._nTopBottomEdgeSkip = new System.Windows.Forms.NumericUpDown();
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            this.grpFrame.SuspendLayout();
            this.tlpFrame.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).BeginInit();
            this.grpWafer.SuspendLayout();
            this.tlpWafer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nSideEdgeSkip)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTopBottomEdgeSkip)).BeginInit();
            this.SuspendLayout();
            // 
            // _editorPanel
            // 
            this._editorPanel.BackColor = System.Drawing.Color.White;
            this._editorPanel.Controls.Add(this.editorLayout);
            this._editorPanel.Size = new System.Drawing.Size(1094, 676);
            // 
            // _lblProject
            // 
            this._lblProject.Size = new System.Drawing.Size(794, 36);
            // 
            // editorLayout
            // 
            this.editorLayout.BackColor = System.Drawing.Color.White;
            this.editorLayout.ColumnCount = 2;
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.Controls.Add(this.grpFrame, 0, 0);
            this.editorLayout.Controls.Add(this.grpWafer, 0, 1);
            this.editorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorLayout.Location = new System.Drawing.Point(8, 12);
            this.editorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.editorLayout.Name = "editorLayout";
            this.editorLayout.RowCount = 2;
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.Size = new System.Drawing.Size(1078, 656);
            this.editorLayout.TabIndex = 0;
            // 
            // grpFrame
            // 
            this.grpFrame.BackColor = System.Drawing.Color.White;
            this.grpFrame.Controls.Add(this.tlpFrame);
            this.grpFrame.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpFrame.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpFrame.Location = new System.Drawing.Point(0, 0);
            this.grpFrame.Margin = new System.Windows.Forms.Padding(0);
            this.grpFrame.Name = "grpFrame";
            this.grpFrame.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpFrame.Size = new System.Drawing.Size(539, 297);
            this.grpFrame.TabIndex = 0;
            this.grpFrame.TabStop = false;
            this.grpFrame.Text = "Tape frame specification";
            // 
            // tlpFrame
            // 
            this.tlpFrame.BackColor = System.Drawing.Color.White;
            this.tlpFrame.ColumnCount = 4;
            this.tlpFrame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpFrame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpFrame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tlpFrame.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tlpFrame.Controls.Add(this.lblSpecLibrary, 0, 0);
            this.tlpFrame.Controls.Add(this._cbSpecLibrary, 1, 0);
            this.tlpFrame.Controls.Add(this.btnLoadSpec, 2, 0);
            this.tlpFrame.Controls.Add(this.btnSaveSpec, 3, 0);
            this.tlpFrame.Controls.Add(this.lblName, 0, 1);
            this.tlpFrame.Controls.Add(this._tbName, 1, 1);
            this.tlpFrame.Controls.Add(this.lblGridX, 0, 2);
            this.tlpFrame.Controls.Add(this._nGridX, 1, 2);
            this.tlpFrame.Controls.Add(this.lblGridY, 0, 3);
            this.tlpFrame.Controls.Add(this._nGridY, 1, 3);
            this.tlpFrame.Controls.Add(this.lblPitchX, 0, 4);
            this.tlpFrame.Controls.Add(this._nPitchX, 1, 4);
            this.tlpFrame.Controls.Add(this.lblPitchY, 0, 5);
            this.tlpFrame.Controls.Add(this._nPitchY, 1, 5);
            this.tlpFrame.Controls.Add(this.lblDiameter, 0, 6);
            this.tlpFrame.Controls.Add(this._nDiameter, 1, 6);
            this.tlpFrame.Controls.Add(this.lblRotate, 0, 7);
            this.tlpFrame.Controls.Add(this._cbRotate, 1, 7);
            this.tlpFrame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpFrame.Location = new System.Drawing.Point(6, 20);
            this.tlpFrame.Margin = new System.Windows.Forms.Padding(0);
            this.tlpFrame.Name = "tlpFrame";
            this.tlpFrame.RowCount = 9;
            this.tlpFrame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpFrame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpFrame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpFrame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpFrame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpFrame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpFrame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpFrame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpFrame.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpFrame.Size = new System.Drawing.Size(527, 271);
            this.tlpFrame.TabIndex = 0;
            // 
            // lblSpecLibrary
            // 
            this.lblSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpecLibrary.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSpecLibrary.Location = new System.Drawing.Point(3, 0);
            this.lblSpecLibrary.Name = "lblSpecLibrary";
            this.lblSpecLibrary.Size = new System.Drawing.Size(214, 33);
            this.lblSpecLibrary.TabIndex = 1;
            this.lblSpecLibrary.Text = "Saved spec";
            this.lblSpecLibrary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbSpecLibrary
            // 
            this._cbSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbSpecLibrary.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSpecLibrary.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbSpecLibrary.Location = new System.Drawing.Point(223, 3);
            this._cbSpecLibrary.Name = "_cbSpecLibrary";
            this._cbSpecLibrary.Size = new System.Drawing.Size(61, 23);
            this._cbSpecLibrary.TabIndex = 2;
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
            this.btnLoadSpec.TabIndex = 3;
            this.btnLoadSpec.Text = "LOAD SPEC";
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
            this.btnSaveSpec.TabIndex = 4;
            this.btnSaveSpec.Text = "SAVE SPEC";
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
            this.lblName.TabIndex = 5;
            this.lblName.Text = "Spec name";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbName
            // 
            this.tlpFrame.SetColumnSpan(this._tbName, 3);
            this._tbName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbName.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbName.Location = new System.Drawing.Point(223, 36);
            this._tbName.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this._tbName.Name = "_tbName";
            this._tbName.Size = new System.Drawing.Size(301, 23);
            this._tbName.TabIndex = 6;
            // 
            // lblGridX
            // 
            this.lblGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGridX.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGridX.Location = new System.Drawing.Point(3, 67);
            this.lblGridX.Name = "lblGridX";
            this.lblGridX.Size = new System.Drawing.Size(214, 34);
            this.lblGridX.TabIndex = 7;
            this.lblGridX.Text = "Grid X (count)";
            this.lblGridX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nGridX
            // 
            this.tlpFrame.SetColumnSpan(this._nGridX, 3);
            this._nGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nGridX.Location = new System.Drawing.Point(223, 70);
            this._nGridX.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this._nGridX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nGridX.Name = "_nGridX";
            this._nGridX.Size = new System.Drawing.Size(301, 23);
            this._nGridX.TabIndex = 8;
            this._nGridX.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblGridY
            // 
            this.lblGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGridY.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGridY.Location = new System.Drawing.Point(3, 101);
            this.lblGridY.Name = "lblGridY";
            this.lblGridY.Size = new System.Drawing.Size(214, 34);
            this.lblGridY.TabIndex = 9;
            this.lblGridY.Text = "Grid Y (count)";
            this.lblGridY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nGridY
            // 
            this.tlpFrame.SetColumnSpan(this._nGridY, 3);
            this._nGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nGridY.Location = new System.Drawing.Point(223, 104);
            this._nGridY.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this._nGridY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._nGridY.Name = "_nGridY";
            this._nGridY.Size = new System.Drawing.Size(301, 23);
            this._nGridY.TabIndex = 10;
            this._nGridY.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblPitchX
            // 
            this.lblPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchX.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPitchX.Location = new System.Drawing.Point(3, 135);
            this.lblPitchX.Name = "lblPitchX";
            this.lblPitchX.Size = new System.Drawing.Size(214, 34);
            this.lblPitchX.TabIndex = 11;
            this.lblPitchX.Text = "Pitch X (mm)";
            this.lblPitchX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPitchX
            // 
            this.tlpFrame.SetColumnSpan(this._nPitchX, 3);
            this._nPitchX.DecimalPlaces = 3;
            this._nPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchX.Location = new System.Drawing.Point(223, 138);
            this._nPitchX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchX.Name = "_nPitchX";
            this._nPitchX.Size = new System.Drawing.Size(301, 23);
            this._nPitchX.TabIndex = 12;
            this._nPitchX.Value = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            // 
            // lblPitchY
            // 
            this.lblPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPitchY.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPitchY.Location = new System.Drawing.Point(3, 169);
            this.lblPitchY.Name = "lblPitchY";
            this.lblPitchY.Size = new System.Drawing.Size(214, 34);
            this.lblPitchY.TabIndex = 13;
            this.lblPitchY.Text = "Pitch Y (mm)";
            this.lblPitchY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPitchY
            // 
            this.tlpFrame.SetColumnSpan(this._nPitchY, 3);
            this._nPitchY.DecimalPlaces = 3;
            this._nPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchY.Location = new System.Drawing.Point(223, 172);
            this._nPitchY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchY.Name = "_nPitchY";
            this._nPitchY.Size = new System.Drawing.Size(301, 23);
            this._nPitchY.TabIndex = 14;
            this._nPitchY.Value = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            // 
            // lblDiameter
            // 
            this.lblDiameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDiameter.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblDiameter.Location = new System.Drawing.Point(3, 203);
            this.lblDiameter.Name = "lblDiameter";
            this.lblDiameter.Size = new System.Drawing.Size(214, 34);
            this.lblDiameter.TabIndex = 15;
            this.lblDiameter.Text = "Outer diameter (mm)";
            this.lblDiameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nDiameter
            // 
            this.tlpFrame.SetColumnSpan(this._nDiameter, 3);
            this._nDiameter.DecimalPlaces = 1;
            this._nDiameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDiameter.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDiameter.Location = new System.Drawing.Point(223, 206);
            this._nDiameter.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._nDiameter.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this._nDiameter.Name = "_nDiameter";
            this._nDiameter.Size = new System.Drawing.Size(301, 23);
            this._nDiameter.TabIndex = 16;
            this._nDiameter.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // lblRotate
            // 
            this.lblRotate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRotate.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblRotate.Location = new System.Drawing.Point(3, 237);
            this.lblRotate.Name = "lblRotate";
            this.lblRotate.Size = new System.Drawing.Size(214, 34);
            this.lblRotate.TabIndex = 17;
            this.lblRotate.Text = "Rotate";
            this.lblRotate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbRotate
            // 
            this.tlpFrame.SetColumnSpan(this._cbRotate, 3);
            this._cbRotate.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbRotate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbRotate.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbRotate.Items.AddRange(new object[] {
            "None",
            "R90",
            "R180",
            "R270"});
            this._cbRotate.Location = new System.Drawing.Point(223, 240);
            this._cbRotate.Name = "_cbRotate";
            this._cbRotate.Size = new System.Drawing.Size(301, 23);
            this._cbRotate.TabIndex = 18;
            // 
            // grpWafer
            // 
            this.grpWafer.BackColor = System.Drawing.Color.White;
            this.grpWafer.Controls.Add(this.tlpWafer);
            this.grpWafer.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.grpWafer.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpWafer.Location = new System.Drawing.Point(0, 366);
            this.grpWafer.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.grpWafer.Name = "grpWafer";
            this.grpWafer.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpWafer.Size = new System.Drawing.Size(539, 290);
            this.grpWafer.TabIndex = 1;
            this.grpWafer.TabStop = false;
            this.grpWafer.Text = "Wafer map / die size";
            // 
            // tlpWafer
            // 
            this.tlpWafer.BackColor = System.Drawing.Color.White;
            this.tlpWafer.ColumnCount = 2;
            this.tlpWafer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.tlpWafer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpWafer.Controls.Add(this.lblWaferRole, 0, 0);
            this.tlpWafer.Controls.Add(this._cbWaferRole, 1, 0);
            this.tlpWafer.Controls.Add(this._btnImportWaferMap, 0, 1);
            this.tlpWafer.Controls.Add(this.lblMapFile, 0, 2);
            this.tlpWafer.Controls.Add(this._lblMapFileValue, 1, 2);
            this.tlpWafer.Controls.Add(this.lblDieSizeX, 0, 3);
            this.tlpWafer.Controls.Add(this._nDieSizeX, 1, 3);
            this.tlpWafer.Controls.Add(this.lblDieSizeY, 0, 4);
            this.tlpWafer.Controls.Add(this._nDieSizeY, 1, 4);
            this.tlpWafer.Controls.Add(this.lblEdgeMode, 0, 5);
            this.tlpWafer.Controls.Add(this._cbEdgeSkipMode, 1, 5);
            this.tlpWafer.Controls.Add(this.lblEdgeLR, 0, 6);
            this.tlpWafer.Controls.Add(this._nSideEdgeSkip, 1, 6);
            this.tlpWafer.Controls.Add(this.lblEdgeTB, 0, 7);
            this.tlpWafer.Controls.Add(this._nTopBottomEdgeSkip, 1, 7);
            this.tlpWafer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpWafer.Location = new System.Drawing.Point(6, 20);
            this.tlpWafer.Margin = new System.Windows.Forms.Padding(0);
            this.tlpWafer.Name = "tlpWafer";
            this.tlpWafer.RowCount = 9;
            this.tlpWafer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpWafer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpWafer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpWafer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpWafer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpWafer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpWafer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpWafer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tlpWafer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpWafer.Size = new System.Drawing.Size(527, 264);
            this.tlpWafer.TabIndex = 0;
            // 
            // lblWaferRole
            // 
            this.lblWaferRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWaferRole.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblWaferRole.Location = new System.Drawing.Point(3, 0);
            this.lblWaferRole.Name = "lblWaferRole";
            this.lblWaferRole.Size = new System.Drawing.Size(214, 33);
            this.lblWaferRole.TabIndex = 0;
            this.lblWaferRole.Text = "Wafer role";
            this.lblWaferRole.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbWaferRole
            // 
            this._cbWaferRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbWaferRole.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbWaferRole.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbWaferRole.Items.AddRange(new object[] {
            "INPUT WAFER",
            "OUTPUT WAFER"});
            this._cbWaferRole.Location = new System.Drawing.Point(223, 3);
            this._cbWaferRole.Name = "_cbWaferRole";
            this._cbWaferRole.Size = new System.Drawing.Size(301, 23);
            this._cbWaferRole.TabIndex = 1;
            this._cbWaferRole.SelectedIndexChanged += new System.EventHandler(this.OnWaferRoleChanged);
            // 
            // _btnImportWaferMap
            // 
            this.tlpWafer.SetColumnSpan(this._btnImportWaferMap, 2);
            this._btnImportWaferMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnImportWaferMap.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnImportWaferMap.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this._btnImportWaferMap.Location = new System.Drawing.Point(3, 36);
            this._btnImportWaferMap.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this._btnImportWaferMap.Name = "_btnImportWaferMap";
            this._btnImportWaferMap.Size = new System.Drawing.Size(521, 26);
            this._btnImportWaferMap.TabIndex = 2;
            this._btnImportWaferMap.Text = "LOAD WAFER MAP";
            this._btnImportWaferMap.UseVisualStyleBackColor = true;
            this._btnImportWaferMap.Click += new System.EventHandler(this.btnImportWaferMap_Click);
            // 
            // lblMapFile
            // 
            this.lblMapFile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMapFile.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblMapFile.Location = new System.Drawing.Point(3, 66);
            this.lblMapFile.Name = "lblMapFile";
            this.lblMapFile.Size = new System.Drawing.Size(214, 33);
            this.lblMapFile.TabIndex = 3;
            this.lblMapFile.Text = "Recipe map file";
            this.lblMapFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lblMapFileValue
            // 
            this._lblMapFileValue.AutoEllipsis = true;
            this._lblMapFileValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblMapFileValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblMapFileValue.Font = new System.Drawing.Font("Consolas", 9F);
            this._lblMapFileValue.Location = new System.Drawing.Point(223, 66);
            this._lblMapFileValue.Name = "_lblMapFileValue";
            this._lblMapFileValue.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this._lblMapFileValue.Size = new System.Drawing.Size(301, 33);
            this._lblMapFileValue.TabIndex = 4;
            this._lblMapFileValue.Text = "-";
            this._lblMapFileValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDieSizeX
            // 
            this.lblDieSizeX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieSizeX.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblDieSizeX.Location = new System.Drawing.Point(3, 99);
            this.lblDieSizeX.Name = "lblDieSizeX";
            this.lblDieSizeX.Size = new System.Drawing.Size(214, 33);
            this.lblDieSizeX.TabIndex = 5;
            this.lblDieSizeX.Text = "Die size X (mm)";
            this.lblDieSizeX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nDieSizeX
            // 
            this._nDieSizeX.DecimalPlaces = 3;
            this._nDieSizeX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDieSizeX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDieSizeX.Location = new System.Drawing.Point(223, 102);
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
            this._nDieSizeX.Size = new System.Drawing.Size(301, 23);
            this._nDieSizeX.TabIndex = 6;
            this._nDieSizeX.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblDieSizeY
            // 
            this.lblDieSizeY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDieSizeY.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblDieSizeY.Location = new System.Drawing.Point(3, 132);
            this.lblDieSizeY.Name = "lblDieSizeY";
            this.lblDieSizeY.Size = new System.Drawing.Size(214, 33);
            this.lblDieSizeY.TabIndex = 7;
            this.lblDieSizeY.Text = "Die size Y (mm)";
            this.lblDieSizeY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nDieSizeY
            // 
            this._nDieSizeY.DecimalPlaces = 3;
            this._nDieSizeY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDieSizeY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDieSizeY.Location = new System.Drawing.Point(223, 135);
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
            this._nDieSizeY.Size = new System.Drawing.Size(301, 23);
            this._nDieSizeY.TabIndex = 8;
            this._nDieSizeY.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblEdgeMode
            // 
            this.lblEdgeMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEdgeMode.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblEdgeMode.Location = new System.Drawing.Point(3, 165);
            this.lblEdgeMode.Name = "lblEdgeMode";
            this.lblEdgeMode.Size = new System.Drawing.Size(214, 33);
            this.lblEdgeMode.TabIndex = 9;
            this.lblEdgeMode.Text = "Edge skip mode";
            this.lblEdgeMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbEdgeSkipMode
            // 
            this._cbEdgeSkipMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbEdgeSkipMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbEdgeSkipMode.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbEdgeSkipMode.Items.AddRange(new object[] {
            "GRID COUNT",
            "MM",
            "EXTERNAL MAP"});
            this._cbEdgeSkipMode.Location = new System.Drawing.Point(223, 168);
            this._cbEdgeSkipMode.Name = "_cbEdgeSkipMode";
            this._cbEdgeSkipMode.Size = new System.Drawing.Size(301, 23);
            this._cbEdgeSkipMode.TabIndex = 10;
            this._cbEdgeSkipMode.SelectedIndexChanged += new System.EventHandler(this._cbEdgeSkipMode_SelectedIndexChanged);
            // 
            // lblEdgeLR
            // 
            this.lblEdgeLR.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEdgeLR.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblEdgeLR.Location = new System.Drawing.Point(3, 198);
            this.lblEdgeLR.Name = "lblEdgeLR";
            this.lblEdgeLR.Size = new System.Drawing.Size(214, 33);
            this.lblEdgeLR.TabIndex = 11;
            this.lblEdgeLR.Text = "Edge skip L/R";
            this.lblEdgeLR.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nSideEdgeSkip
            // 
            this._nSideEdgeSkip.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nSideEdgeSkip.Font = new System.Drawing.Font("Consolas", 10F);
            this._nSideEdgeSkip.Location = new System.Drawing.Point(223, 201);
            this._nSideEdgeSkip.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._nSideEdgeSkip.Name = "_nSideEdgeSkip";
            this._nSideEdgeSkip.Size = new System.Drawing.Size(301, 23);
            this._nSideEdgeSkip.TabIndex = 12;
            // 
            // lblEdgeTB
            // 
            this.lblEdgeTB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEdgeTB.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblEdgeTB.Location = new System.Drawing.Point(3, 231);
            this.lblEdgeTB.Name = "lblEdgeTB";
            this.lblEdgeTB.Size = new System.Drawing.Size(214, 33);
            this.lblEdgeTB.TabIndex = 13;
            this.lblEdgeTB.Text = "Edge skip T/B";
            this.lblEdgeTB.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nTopBottomEdgeSkip
            // 
            this._nTopBottomEdgeSkip.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nTopBottomEdgeSkip.Font = new System.Drawing.Font("Consolas", 10F);
            this._nTopBottomEdgeSkip.Location = new System.Drawing.Point(223, 234);
            this._nTopBottomEdgeSkip.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this._nTopBottomEdgeSkip.Name = "_nTopBottomEdgeSkip";
            this._nTopBottomEdgeSkip.Size = new System.Drawing.Size(301, 23);
            this._nTopBottomEdgeSkip.TabIndex = 14;
            // 
            // TapeFrameSubsetPage
            // 
            this.Name = "TapeFrameSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            this.grpFrame.ResumeLayout(false);
            this.tlpFrame.ResumeLayout(false);
            this.tlpFrame.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).EndInit();
            this.grpWafer.ResumeLayout(false);
            this.tlpWafer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDieSizeY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nSideEdgeSkip)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTopBottomEdgeSkip)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
