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
            this._editorPanel.SuspendLayout();
            this.editorLayout.SuspendLayout();
            this.grpFrame.SuspendLayout();
            this.tlpFrame.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).BeginInit();
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
            // editorLayout  (좌측 50%만 사용, 그룹박스 세로 배치)
            //
            this.editorLayout.BackColor = System.Drawing.Color.White;
            this.editorLayout.ColumnCount = 2;
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.editorLayout.Controls.Add(this.grpFrame, 0, 0);
            this.editorLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editorLayout.Location = new System.Drawing.Point(8, 12);
            this.editorLayout.Margin = new System.Windows.Forms.Padding(0);
            this.editorLayout.Name = "editorLayout";
            this.editorLayout.Padding = new System.Windows.Forms.Padding(0);
            this.editorLayout.RowCount = 2;
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 306F));
            this.editorLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.editorLayout.Size = new System.Drawing.Size(1078, 656);
            this.editorLayout.TabIndex = 0;
            //
            // grpFrame
            //
            this.grpFrame.BackColor = System.Drawing.Color.White;
            this.grpFrame.Controls.Add(this.tlpFrame);
            this.grpFrame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpFrame.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpFrame.Margin = new System.Windows.Forms.Padding(0);
            this.grpFrame.Name = "grpFrame";
            this.grpFrame.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
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
            this.tlpFrame.TabIndex = 0;
            //
            // lblSpecLibrary
            //
            this.lblSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpecLibrary.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSpecLibrary.Name = "lblSpecLibrary";
            this.lblSpecLibrary.TabIndex = 1;
            this.lblSpecLibrary.Text = "Saved spec";
            this.lblSpecLibrary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbSpecLibrary
            //
            this._cbSpecLibrary.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbSpecLibrary.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSpecLibrary.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbSpecLibrary.Name = "_cbSpecLibrary";
            this._cbSpecLibrary.TabIndex = 2;
            //
            // btnLoadSpec
            //
            this.btnLoadSpec.BackColor = System.Drawing.Color.White;
            this.btnLoadSpec.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadSpec.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadSpec.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(205)))), ((int)(((byte)(212)))));
            this.btnLoadSpec.FlatAppearance.BorderSize = 1;
            this.btnLoadSpec.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(230)))), ((int)(((byte)(236)))));
            this.btnLoadSpec.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            this.btnLoadSpec.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadSpec.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnLoadSpec.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(80)))));
            this.btnLoadSpec.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this.btnLoadSpec.Name = "btnLoadSpec";
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
            this.btnSaveSpec.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this.btnSaveSpec.Name = "btnSaveSpec";
            this.btnSaveSpec.TabIndex = 4;
            this.btnSaveSpec.Text = "SAVE SPEC";
            this.btnSaveSpec.UseVisualStyleBackColor = false;
            this.btnSaveSpec.Click += new System.EventHandler(this.btnSaveSpec_Click);
            //
            // lblName
            //
            this.lblName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblName.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblName.Name = "lblName";
            this.lblName.TabIndex = 5;
            this.lblName.Text = "Spec name";
            this.lblName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _tbName
            //
            this.tlpFrame.SetColumnSpan(this._tbName, 3);
            this._tbName.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbName.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbName.Margin = new System.Windows.Forms.Padding(3, 3, 3, 4);
            this._tbName.Name = "_tbName";
            this._tbName.TabIndex = 6;
            //
            // lblGridX
            //
            this.lblGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGridX.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblGridX.Name = "lblGridX";
            this.lblGridX.TabIndex = 7;
            this.lblGridX.Text = "Grid X (count)";
            this.lblGridX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nGridX
            //
            this.tlpFrame.SetColumnSpan(this._nGridX, 3);
            this._nGridX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridX.Font = new System.Drawing.Font("Consolas", 10F);
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
            this.lblGridY.Name = "lblGridY";
            this.lblGridY.TabIndex = 9;
            this.lblGridY.Text = "Grid Y (count)";
            this.lblGridY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _nGridY
            //
            this.tlpFrame.SetColumnSpan(this._nGridY, 3);
            this._nGridY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nGridY.Font = new System.Drawing.Font("Consolas", 10F);
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
            this.lblPitchX.Name = "lblPitchX";
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
            this._nPitchX.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchX.Name = "_nPitchX";
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
            this.lblPitchY.Name = "lblPitchY";
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
            this._nPitchY.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._nPitchY.Name = "_nPitchY";
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
            this.lblDiameter.Name = "lblDiameter";
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
            this.lblRotate.Name = "lblRotate";
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
            this._cbRotate.Name = "_cbRotate";
            this._cbRotate.TabIndex = 18;
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
            this.ResumeLayout(false);

        }
    }
}
