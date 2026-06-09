using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.Recipe
{
    partial class TapeFrameSubsetPage
    {
        private TableLayoutPanel editorLayout;
        private Label lblFrameHeader;
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

        private void InitializeComponent()
        {
            this.editorLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblFrameHeader = new System.Windows.Forms.Label();
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
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).BeginInit();
            this.SuspendLayout();
            // 
            // _editorPanel
            // 
            this._editorPanel.Controls.Add(this.editorLayout);
            this._editorPanel.Size = new System.Drawing.Size(1094, 676);
            // 
            // _lblProject
            // 
            this._lblProject.Size = new System.Drawing.Size(794, 36);
            // 
            // editorLayout
            // 
            this.editorLayout.ColumnCount = 4;
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.editorLayout.Controls.Add(this.lblFrameHeader, 0, 0);
            this.editorLayout.Controls.Add(this.lblSpecLibrary, 0, 1);
            this.editorLayout.Controls.Add(this._cbSpecLibrary, 1, 1);
            this.editorLayout.Controls.Add(this.btnLoadSpec, 2, 1);
            this.editorLayout.Controls.Add(this.btnSaveSpec, 3, 1);
            this.editorLayout.Controls.Add(this.lblName, 0, 2);
            this.editorLayout.Controls.Add(this._tbName, 1, 2);
            this.editorLayout.Controls.Add(this.lblGridX, 0, 3);
            this.editorLayout.Controls.Add(this._nGridX, 1, 3);
            this.editorLayout.Controls.Add(this.lblGridY, 0, 4);
            this.editorLayout.Controls.Add(this._nGridY, 1, 4);
            this.editorLayout.Controls.Add(this.lblPitchX, 0, 5);
            this.editorLayout.Controls.Add(this._nPitchX, 1, 5);
            this.editorLayout.Controls.Add(this.lblPitchY, 0, 6);
            this.editorLayout.Controls.Add(this._nPitchY, 1, 6);
            this.editorLayout.Controls.Add(this.lblDiameter, 0, 7);
            this.editorLayout.Controls.Add(this._nDiameter, 1, 7);
            this.editorLayout.Controls.Add(this.lblRotate, 0, 8);
            this.editorLayout.Controls.Add(this._cbRotate, 1, 8);
            this.editorLayout.SetColumnSpan(this._tbName, 3);
            this.editorLayout.SetColumnSpan(this._nGridX, 3);
            this.editorLayout.SetColumnSpan(this._nGridY, 3);
            this.editorLayout.SetColumnSpan(this._nPitchX, 3);
            this.editorLayout.SetColumnSpan(this._nPitchY, 3);
            this.editorLayout.SetColumnSpan(this._nDiameter, 3);
            this.editorLayout.SetColumnSpan(this._cbRotate, 3);
            this.editorLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.editorLayout.Location = new System.Drawing.Point(8, 12);
            this.editorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.editorLayout.Name = "editorLayout";
            this.editorLayout.Padding = new System.Windows.Forms.Padding(10);
            this.editorLayout.RowCount = 9;
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.editorLayout.Size = new System.Drawing.Size(1078, 326);
            this.editorLayout.TabIndex = 0;
            // 
            // lblFrameHeader
            // 
            this.lblFrameHeader.BackColor = System.Drawing.Color.LightYellow;
            this.lblFrameHeader.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.editorLayout.SetColumnSpan(this.lblFrameHeader, 4);
            this.lblFrameHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFrameHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblFrameHeader.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lblFrameHeader.Location = new System.Drawing.Point(13, 10);
            this.lblFrameHeader.Name = "lblFrameHeader";
            this.lblFrameHeader.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblFrameHeader.Size = new System.Drawing.Size(1052, 30);
            this.lblFrameHeader.TabIndex = 0;
            this.lblFrameHeader.Text = "Tape frame specification";
            this.lblFrameHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSpecLibrary
            // 
            this.lblSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpecLibrary.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSpecLibrary.Location = new System.Drawing.Point(13, 40);
            this.lblSpecLibrary.Name = "lblSpecLibrary";
            this.lblSpecLibrary.Size = new System.Drawing.Size(214, 34);
            this.lblSpecLibrary.TabIndex = 1;
            this.lblSpecLibrary.Text = "Saved spec";
            this.lblSpecLibrary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbSpecLibrary
            // 
            this._cbSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbSpecLibrary.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSpecLibrary.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbSpecLibrary.Location = new System.Drawing.Point(233, 43);
            this._cbSpecLibrary.Name = "_cbSpecLibrary";
            this._cbSpecLibrary.Size = new System.Drawing.Size(592, 23);
            this._cbSpecLibrary.TabIndex = 2;
            // 
            // btnLoadSpec
            // 
            this.btnLoadSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadSpec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadSpec.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnLoadSpec.Location = new System.Drawing.Point(831, 43);
            this.btnLoadSpec.Name = "btnLoadSpec";
            this.btnLoadSpec.Size = new System.Drawing.Size(114, 28);
            this.btnLoadSpec.TabIndex = 3;
            this.btnLoadSpec.Text = "LOAD SPEC";
            this.btnLoadSpec.UseVisualStyleBackColor = true;
            this.btnLoadSpec.Click += new System.EventHandler(this.btnLoadSpec_Click);
            // 
            // btnSaveSpec
            // 
            this.btnSaveSpec.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(88)))), ((int)(((byte)(31)))));
            this.btnSaveSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveSpec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveSpec.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnSaveSpec.ForeColor = System.Drawing.Color.White;
            this.btnSaveSpec.Location = new System.Drawing.Point(951, 43);
            this.btnSaveSpec.Name = "btnSaveSpec";
            this.btnSaveSpec.Size = new System.Drawing.Size(114, 28);
            this.btnSaveSpec.TabIndex = 4;
            this.btnSaveSpec.Text = "SAVE SPEC";
            this.btnSaveSpec.UseVisualStyleBackColor = false;
            this.btnSaveSpec.Click += new System.EventHandler(this.btnSaveSpec_Click);
            // 
            // lblName
            // 
            this.lblName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblName.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblName.Location = new System.Drawing.Point(13, 40);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(214, 34);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "Spec name";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbName
            // 
            this._tbName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbName.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbName.Location = new System.Drawing.Point(233, 43);
            this._tbName.Name = "_tbName";
            this._tbName.Size = new System.Drawing.Size(832, 23);
            this._tbName.TabIndex = 2;
            // 
            // lblGridX
            // 
            this.lblGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGridX.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGridX.Location = new System.Drawing.Point(13, 74);
            this.lblGridX.Name = "lblGridX";
            this.lblGridX.Size = new System.Drawing.Size(214, 34);
            this.lblGridX.TabIndex = 3;
            this.lblGridX.Text = "Grid X (count)";
            this.lblGridX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nGridX
            // 
            this._nGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nGridX.Location = new System.Drawing.Point(233, 77);
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
            this._nGridX.Size = new System.Drawing.Size(832, 23);
            this._nGridX.TabIndex = 4;
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
            this.lblGridY.Location = new System.Drawing.Point(13, 108);
            this.lblGridY.Name = "lblGridY";
            this.lblGridY.Size = new System.Drawing.Size(214, 34);
            this.lblGridY.TabIndex = 5;
            this.lblGridY.Text = "Grid Y (count)";
            this.lblGridY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nGridY
            // 
            this._nGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nGridY.Location = new System.Drawing.Point(233, 111);
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
            this._nGridY.Size = new System.Drawing.Size(832, 23);
            this._nGridY.TabIndex = 6;
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
            this.lblPitchX.Location = new System.Drawing.Point(13, 142);
            this.lblPitchX.Name = "lblPitchX";
            this.lblPitchX.Size = new System.Drawing.Size(214, 34);
            this.lblPitchX.TabIndex = 7;
            this.lblPitchX.Text = "Pitch X (mm)";
            this.lblPitchX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPitchX
            // 
            this._nPitchX.DecimalPlaces = 3;
            this._nPitchX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchX.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchX.Location = new System.Drawing.Point(233, 145);
            this._nPitchX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchX.Name = "_nPitchX";
            this._nPitchX.Size = new System.Drawing.Size(832, 23);
            this._nPitchX.TabIndex = 8;
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
            this.lblPitchY.Location = new System.Drawing.Point(13, 176);
            this.lblPitchY.Name = "lblPitchY";
            this.lblPitchY.Size = new System.Drawing.Size(214, 34);
            this.lblPitchY.TabIndex = 9;
            this.lblPitchY.Text = "Pitch Y (mm)";
            this.lblPitchY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nPitchY
            // 
            this._nPitchY.DecimalPlaces = 3;
            this._nPitchY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nPitchY.Font = new System.Drawing.Font("Consolas", 10F);
            this._nPitchY.Location = new System.Drawing.Point(233, 179);
            this._nPitchY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchY.Name = "_nPitchY";
            this._nPitchY.Size = new System.Drawing.Size(832, 23);
            this._nPitchY.TabIndex = 10;
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
            this.lblDiameter.Location = new System.Drawing.Point(13, 210);
            this.lblDiameter.Name = "lblDiameter";
            this.lblDiameter.Size = new System.Drawing.Size(214, 34);
            this.lblDiameter.TabIndex = 11;
            this.lblDiameter.Text = "Outer diameter (mm)";
            this.lblDiameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nDiameter
            // 
            this._nDiameter.DecimalPlaces = 1;
            this._nDiameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDiameter.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDiameter.Location = new System.Drawing.Point(233, 213);
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
            this._nDiameter.Size = new System.Drawing.Size(832, 23);
            this._nDiameter.TabIndex = 12;
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
            this.lblRotate.Location = new System.Drawing.Point(13, 244);
            this.lblRotate.Name = "lblRotate";
            this.lblRotate.Size = new System.Drawing.Size(214, 38);
            this.lblRotate.TabIndex = 13;
            this.lblRotate.Text = "Rotate";
            this.lblRotate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbRotate
            // 
            this._cbRotate.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbRotate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbRotate.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbRotate.Items.AddRange(new object[] {
            "None",
            "R90",
            "R180",
            "R270"});
            this._cbRotate.Location = new System.Drawing.Point(233, 247);
            this._cbRotate.Name = "_cbRotate";
            this._cbRotate.Size = new System.Drawing.Size(832, 23);
            this._cbRotate.TabIndex = 14;
            // 
            // TapeFrameSubsetPage
            // 
            this.Name = "TapeFrameSubsetPage";
            this.Size = new System.Drawing.Size(1094, 742);
            this.Controls.SetChildIndex(this._editorPanel, 0);
            this._editorPanel.ResumeLayout(false);
            this.editorLayout.ResumeLayout(false);
            this.editorLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
