namespace QMC.Vision.Ui.Pages
{
    partial class WaferSpecPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel _panelLeft;
        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblSpecLib;
        private System.Windows.Forms.ComboBox _cbSpecLibrary;
        private System.Windows.Forms.Button _btnLoadSpec;
        private System.Windows.Forms.Button _btnSaveSpec;
        private System.Windows.Forms.Label _lblName;
        private System.Windows.Forms.TextBox _tbName;
        private System.Windows.Forms.Label _lblGridX;
        private System.Windows.Forms.NumericUpDown _nGridX;
        private System.Windows.Forms.Label _lblGridY;
        private System.Windows.Forms.NumericUpDown _nGridY;
        private System.Windows.Forms.Label _lblPitchX;
        private System.Windows.Forms.NumericUpDown _nPitchX;
        private System.Windows.Forms.Label _lblPitchY;
        private System.Windows.Forms.NumericUpDown _nPitchY;
        private System.Windows.Forms.Label _lblDiameter;
        private System.Windows.Forms.NumericUpDown _nDiameter;
        private System.Windows.Forms.Label _lblRotate;
        private System.Windows.Forms.ComboBox _cbRotate;
        private System.Windows.Forms.Button _btnPreview;
        private System.Windows.Forms.Button _btnApply;
        private System.Windows.Forms.Label _lblInfo;
        private QMC.Vision.Ui.Controls.DieMapView _mapView;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._panelLeft = new System.Windows.Forms.Panel();
            this._lblTitle = new System.Windows.Forms.Label();
            this._lblSpecLib = new System.Windows.Forms.Label();
            this._cbSpecLibrary = new System.Windows.Forms.ComboBox();
            this._btnLoadSpec = new System.Windows.Forms.Button();
            this._btnSaveSpec = new System.Windows.Forms.Button();
            this._lblName = new System.Windows.Forms.Label();
            this._tbName = new System.Windows.Forms.TextBox();
            this._lblGridX = new System.Windows.Forms.Label();
            this._nGridX = new System.Windows.Forms.NumericUpDown();
            this._lblGridY = new System.Windows.Forms.Label();
            this._nGridY = new System.Windows.Forms.NumericUpDown();
            this._lblPitchX = new System.Windows.Forms.Label();
            this._nPitchX = new System.Windows.Forms.NumericUpDown();
            this._lblPitchY = new System.Windows.Forms.Label();
            this._nPitchY = new System.Windows.Forms.NumericUpDown();
            this._lblDiameter = new System.Windows.Forms.Label();
            this._nDiameter = new System.Windows.Forms.NumericUpDown();
            this._lblRotate = new System.Windows.Forms.Label();
            this._cbRotate = new System.Windows.Forms.ComboBox();
            this._btnPreview = new System.Windows.Forms.Button();
            this._btnApply = new System.Windows.Forms.Button();
            this._lblInfo = new System.Windows.Forms.Label();
            this._mapView = new QMC.Vision.Ui.Controls.DieMapView();
            this._panelLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).BeginInit();
            this.SuspendLayout();
            //
            // _mapView
            //
            this._mapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this._mapView.Caption = "Wafer Spec Preview";
            this._mapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mapView.Location = new System.Drawing.Point(300, 0);
            this._mapView.Name = "_mapView";
            this._mapView.ShowWaferOutline = true;
            this._mapView.Size = new System.Drawing.Size(600, 700);
            this._mapView.TabIndex = 1;
            //
            // _panelLeft
            //
            this._panelLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this._panelLeft.Controls.Add(this._lblTitle);
            this._panelLeft.Controls.Add(this._lblSpecLib);
            this._panelLeft.Controls.Add(this._cbSpecLibrary);
            this._panelLeft.Controls.Add(this._btnLoadSpec);
            this._panelLeft.Controls.Add(this._btnSaveSpec);
            this._panelLeft.Controls.Add(this._lblName);
            this._panelLeft.Controls.Add(this._tbName);
            this._panelLeft.Controls.Add(this._lblGridX);
            this._panelLeft.Controls.Add(this._nGridX);
            this._panelLeft.Controls.Add(this._lblGridY);
            this._panelLeft.Controls.Add(this._nGridY);
            this._panelLeft.Controls.Add(this._lblPitchX);
            this._panelLeft.Controls.Add(this._nPitchX);
            this._panelLeft.Controls.Add(this._lblPitchY);
            this._panelLeft.Controls.Add(this._nPitchY);
            this._panelLeft.Controls.Add(this._lblDiameter);
            this._panelLeft.Controls.Add(this._nDiameter);
            this._panelLeft.Controls.Add(this._lblRotate);
            this._panelLeft.Controls.Add(this._cbRotate);
            this._panelLeft.Controls.Add(this._btnPreview);
            this._panelLeft.Controls.Add(this._btnApply);
            this._panelLeft.Controls.Add(this._lblInfo);
            this._panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this._panelLeft.Location = new System.Drawing.Point(0, 0);
            this._panelLeft.Name = "_panelLeft";
            this._panelLeft.Padding = new System.Windows.Forms.Padding(12);
            this._panelLeft.Size = new System.Drawing.Size(300, 700);
            this._panelLeft.TabIndex = 0;
            //
            // _lblTitle
            //
            this._lblTitle.AutoSize = true;
            this._lblTitle.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._lblTitle.Location = new System.Drawing.Point(12, 12);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(120, 21);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "웨이퍼 사양";
            //
            // _lblSpecLib
            //
            this._lblSpecLib.AutoSize = true;
            this._lblSpecLib.Location = new System.Drawing.Point(12, 44);
            this._lblSpecLib.Name = "_lblSpecLib";
            this._lblSpecLib.Size = new System.Drawing.Size(70, 15);
            this._lblSpecLib.TabIndex = 1;
            this._lblSpecLib.Text = "저장 사양";
            //
            // _cbSpecLibrary
            //
            this._cbSpecLibrary.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSpecLibrary.Location = new System.Drawing.Point(15, 62);
            this._cbSpecLibrary.Name = "_cbSpecLibrary";
            this._cbSpecLibrary.Size = new System.Drawing.Size(270, 23);
            this._cbSpecLibrary.TabIndex = 2;
            //
            // _btnLoadSpec
            //
            this._btnLoadSpec.Location = new System.Drawing.Point(15, 90);
            this._btnLoadSpec.Name = "_btnLoadSpec";
            this._btnLoadSpec.Size = new System.Drawing.Size(130, 30);
            this._btnLoadSpec.TabIndex = 3;
            this._btnLoadSpec.Text = "LOAD SPEC";
            this._btnLoadSpec.UseVisualStyleBackColor = true;
            this._btnLoadSpec.Click += new System.EventHandler(this.btnLoadSpec_Click);
            //
            // _btnSaveSpec
            //
            this._btnSaveSpec.Location = new System.Drawing.Point(155, 90);
            this._btnSaveSpec.Name = "_btnSaveSpec";
            this._btnSaveSpec.Size = new System.Drawing.Size(130, 30);
            this._btnSaveSpec.TabIndex = 4;
            this._btnSaveSpec.Text = "SAVE SPEC";
            this._btnSaveSpec.UseVisualStyleBackColor = true;
            this._btnSaveSpec.Click += new System.EventHandler(this.btnSaveSpec_Click);
            //
            // _lblName
            //
            this._lblName.AutoSize = true;
            this._lblName.Location = new System.Drawing.Point(12, 132);
            this._lblName.Name = "_lblName";
            this._lblName.Size = new System.Drawing.Size(75, 15);
            this._lblName.TabIndex = 5;
            this._lblName.Text = "Spec name";
            //
            // _tbName
            //
            this._tbName.Location = new System.Drawing.Point(120, 129);
            this._tbName.Name = "_tbName";
            this._tbName.Size = new System.Drawing.Size(165, 23);
            this._tbName.TabIndex = 6;
            //
            // _lblGridX
            //
            this._lblGridX.AutoSize = true;
            this._lblGridX.Location = new System.Drawing.Point(12, 166);
            this._lblGridX.Name = "_lblGridX";
            this._lblGridX.Size = new System.Drawing.Size(95, 15);
            this._lblGridX.TabIndex = 7;
            this._lblGridX.Text = "Grid X (count)";
            //
            // _nGridX
            //
            this._nGridX.Location = new System.Drawing.Point(180, 164);
            this._nGridX.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            this._nGridX.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this._nGridX.Name = "_nGridX";
            this._nGridX.Size = new System.Drawing.Size(105, 23);
            this._nGridX.TabIndex = 8;
            this._nGridX.Value = new decimal(new int[] { 45, 0, 0, 0 });
            //
            // _lblGridY
            //
            this._lblGridY.AutoSize = true;
            this._lblGridY.Location = new System.Drawing.Point(12, 198);
            this._lblGridY.Name = "_lblGridY";
            this._lblGridY.Size = new System.Drawing.Size(95, 15);
            this._lblGridY.TabIndex = 9;
            this._lblGridY.Text = "Grid Y (count)";
            //
            // _nGridY
            //
            this._nGridY.Location = new System.Drawing.Point(180, 196);
            this._nGridY.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
            this._nGridY.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this._nGridY.Name = "_nGridY";
            this._nGridY.Size = new System.Drawing.Size(105, 23);
            this._nGridY.TabIndex = 10;
            this._nGridY.Value = new decimal(new int[] { 45, 0, 0, 0 });
            //
            // _lblPitchX
            //
            this._lblPitchX.AutoSize = true;
            this._lblPitchX.Location = new System.Drawing.Point(12, 230);
            this._lblPitchX.Name = "_lblPitchX";
            this._lblPitchX.Size = new System.Drawing.Size(85, 15);
            this._lblPitchX.TabIndex = 11;
            this._lblPitchX.Text = "Pitch X (mm)";
            //
            // _nPitchX
            //
            this._nPitchX.DecimalPlaces = 3;
            this._nPitchX.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this._nPitchX.Location = new System.Drawing.Point(180, 228);
            this._nPitchX.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this._nPitchX.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this._nPitchX.Name = "_nPitchX";
            this._nPitchX.Size = new System.Drawing.Size(105, 23);
            this._nPitchX.TabIndex = 12;
            this._nPitchX.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // _lblPitchY
            //
            this._lblPitchY.AutoSize = true;
            this._lblPitchY.Location = new System.Drawing.Point(12, 262);
            this._lblPitchY.Name = "_lblPitchY";
            this._lblPitchY.Size = new System.Drawing.Size(85, 15);
            this._lblPitchY.TabIndex = 13;
            this._lblPitchY.Text = "Pitch Y (mm)";
            //
            // _nPitchY
            //
            this._nPitchY.DecimalPlaces = 3;
            this._nPitchY.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this._nPitchY.Location = new System.Drawing.Point(180, 260);
            this._nPitchY.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this._nPitchY.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this._nPitchY.Name = "_nPitchY";
            this._nPitchY.Size = new System.Drawing.Size(105, 23);
            this._nPitchY.TabIndex = 14;
            this._nPitchY.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // _lblDiameter
            //
            this._lblDiameter.AutoSize = true;
            this._lblDiameter.Location = new System.Drawing.Point(12, 294);
            this._lblDiameter.Name = "_lblDiameter";
            this._lblDiameter.Size = new System.Drawing.Size(140, 15);
            this._lblDiameter.TabIndex = 15;
            this._lblDiameter.Text = "Outer diameter (mm)";
            //
            // _nDiameter
            //
            this._nDiameter.DecimalPlaces = 1;
            this._nDiameter.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            this._nDiameter.Location = new System.Drawing.Point(180, 292);
            this._nDiameter.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            this._nDiameter.Name = "_nDiameter";
            this._nDiameter.Size = new System.Drawing.Size(105, 23);
            this._nDiameter.TabIndex = 16;
            this._nDiameter.Value = new decimal(new int[] { 290, 0, 0, 0 });
            //
            // _lblRotate
            //
            this._lblRotate.AutoSize = true;
            this._lblRotate.Location = new System.Drawing.Point(12, 326);
            this._lblRotate.Name = "_lblRotate";
            this._lblRotate.Size = new System.Drawing.Size(50, 15);
            this._lblRotate.TabIndex = 17;
            this._lblRotate.Text = "Rotate";
            //
            // _cbRotate
            //
            this._cbRotate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbRotate.Items.AddRange(new object[] { "None", "R90", "R180", "R270" });
            this._cbRotate.Location = new System.Drawing.Point(180, 323);
            this._cbRotate.Name = "_cbRotate";
            this._cbRotate.Size = new System.Drawing.Size(105, 23);
            this._cbRotate.TabIndex = 18;
            //
            // _btnPreview
            //
            this._btnPreview.Location = new System.Drawing.Point(15, 362);
            this._btnPreview.Name = "_btnPreview";
            this._btnPreview.Size = new System.Drawing.Size(130, 34);
            this._btnPreview.TabIndex = 19;
            this._btnPreview.Text = "미리보기";
            this._btnPreview.UseVisualStyleBackColor = true;
            this._btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            //
            // _btnApply
            //
            this._btnApply.Location = new System.Drawing.Point(155, 362);
            this._btnApply.Name = "_btnApply";
            this._btnApply.Size = new System.Drawing.Size(130, 34);
            this._btnApply.TabIndex = 20;
            this._btnApply.Text = "적용/저장";
            this._btnApply.UseVisualStyleBackColor = true;
            this._btnApply.Click += new System.EventHandler(this.btnApply_Click);
            //
            // _lblInfo
            //
            this._lblInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._lblInfo.Location = new System.Drawing.Point(12, 408);
            this._lblInfo.Name = "_lblInfo";
            this._lblInfo.Size = new System.Drawing.Size(273, 60);
            this._lblInfo.TabIndex = 21;
            this._lblInfo.Text = "";
            //
            // WaferSpecPage
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this._mapView);
            this.Controls.Add(this._panelLeft);
            this.Name = "WaferSpecPage";
            this.Size = new System.Drawing.Size(900, 700);
            this.Load += new System.EventHandler(this.OnPageLoad);
            this._panelLeft.ResumeLayout(false);
            this._panelLeft.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nGridX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nGridY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nPitchY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDiameter)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
