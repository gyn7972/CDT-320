namespace QMC.Vision.Ui.Pages
{
    partial class InputDieMapPage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel _panelLeft;
        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblHelp;
        private System.Windows.Forms.Label _lblSpec;
        private System.Windows.Forms.Label _lblSideSkip;
        private System.Windows.Forms.NumericUpDown _nSideSkip;
        private System.Windows.Forms.Label _lblTbSkip;
        private System.Windows.Forms.NumericUpDown _nTbSkip;
        private System.Windows.Forms.Button _btnGenerate;
        private System.Windows.Forms.Button _btnApply;
        private System.Windows.Forms.Button _btnInvert;
        private System.Windows.Forms.Button _btnExport;
        private System.Windows.Forms.Button _btnImport;
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
            this._lblHelp = new System.Windows.Forms.Label();
            this._lblSpec = new System.Windows.Forms.Label();
            this._lblSideSkip = new System.Windows.Forms.Label();
            this._nSideSkip = new System.Windows.Forms.NumericUpDown();
            this._lblTbSkip = new System.Windows.Forms.Label();
            this._nTbSkip = new System.Windows.Forms.NumericUpDown();
            this._btnGenerate = new System.Windows.Forms.Button();
            this._btnApply = new System.Windows.Forms.Button();
            this._btnInvert = new System.Windows.Forms.Button();
            this._btnExport = new System.Windows.Forms.Button();
            this._btnImport = new System.Windows.Forms.Button();
            this._lblInfo = new System.Windows.Forms.Label();
            this._mapView = new QMC.Vision.Ui.Controls.DieMapView();
            this._panelLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nSideSkip)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTbSkip)).BeginInit();
            this.SuspendLayout();
            //
            // _mapView
            //
            this._mapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this._mapView.Caption = "Input Die Map";
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
            this._panelLeft.Controls.Add(this._lblHelp);
            this._panelLeft.Controls.Add(this._lblSpec);
            this._panelLeft.Controls.Add(this._lblSideSkip);
            this._panelLeft.Controls.Add(this._nSideSkip);
            this._panelLeft.Controls.Add(this._lblTbSkip);
            this._panelLeft.Controls.Add(this._nTbSkip);
            this._panelLeft.Controls.Add(this._btnGenerate);
            this._panelLeft.Controls.Add(this._btnApply);
            this._panelLeft.Controls.Add(this._btnInvert);
            this._panelLeft.Controls.Add(this._btnExport);
            this._panelLeft.Controls.Add(this._btnImport);
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
            this._lblTitle.Size = new System.Drawing.Size(95, 21);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "INPUT DIE";
            //
            // _lblHelp
            //
            this._lblHelp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this._lblHelp.Location = new System.Drawing.Point(12, 40);
            this._lblHelp.Name = "_lblHelp";
            this._lblHelp.Size = new System.Drawing.Size(275, 36);
            this._lblHelp.TabIndex = 1;
            this._lblHelp.Text = "셀 클릭=대상 토글, 휠=확대, 드래그=이동. 사양은 ‘웨이퍼 사양’ 페이지에서 변경.";
            //
            // _lblSpec
            //
            this._lblSpec.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._lblSpec.Location = new System.Drawing.Point(15, 82);
            this._lblSpec.Name = "_lblSpec";
            this._lblSpec.Padding = new System.Windows.Forms.Padding(6);
            this._lblSpec.Size = new System.Drawing.Size(270, 56);
            this._lblSpec.TabIndex = 2;
            this._lblSpec.Text = "";
            //
            // _lblSideSkip
            //
            this._lblSideSkip.AutoSize = true;
            this._lblSideSkip.Location = new System.Drawing.Point(12, 152);
            this._lblSideSkip.Name = "_lblSideSkip";
            this._lblSideSkip.Size = new System.Drawing.Size(120, 15);
            this._lblSideSkip.TabIndex = 3;
            this._lblSideSkip.Text = "Edge skip L/R (열)";
            //
            // _nSideSkip
            //
            this._nSideSkip.Location = new System.Drawing.Point(180, 150);
            this._nSideSkip.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this._nSideSkip.Name = "_nSideSkip";
            this._nSideSkip.Size = new System.Drawing.Size(105, 23);
            this._nSideSkip.TabIndex = 4;
            //
            // _lblTbSkip
            //
            this._lblTbSkip.AutoSize = true;
            this._lblTbSkip.Location = new System.Drawing.Point(12, 184);
            this._lblTbSkip.Name = "_lblTbSkip";
            this._lblTbSkip.Size = new System.Drawing.Size(120, 15);
            this._lblTbSkip.TabIndex = 5;
            this._lblTbSkip.Text = "Edge skip T/B (행)";
            //
            // _nTbSkip
            //
            this._nTbSkip.Location = new System.Drawing.Point(180, 182);
            this._nTbSkip.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this._nTbSkip.Name = "_nTbSkip";
            this._nTbSkip.Size = new System.Drawing.Size(105, 23);
            this._nTbSkip.TabIndex = 6;
            //
            // _btnGenerate
            //
            this._btnGenerate.Location = new System.Drawing.Point(15, 216);
            this._btnGenerate.Name = "_btnGenerate";
            this._btnGenerate.Size = new System.Drawing.Size(270, 32);
            this._btnGenerate.TabIndex = 7;
            this._btnGenerate.Text = "웨이퍼 사양으로 생성 (CREATE)";
            this._btnGenerate.UseVisualStyleBackColor = true;
            this._btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            //
            // _btnApply
            //
            this._btnApply.Location = new System.Drawing.Point(15, 252);
            this._btnApply.Name = "_btnApply";
            this._btnApply.Size = new System.Drawing.Size(270, 32);
            this._btnApply.TabIndex = 8;
            this._btnApply.Text = "적용/저장 (SAVE)";
            this._btnApply.UseVisualStyleBackColor = true;
            this._btnApply.Click += new System.EventHandler(this.btnApply_Click);
            //
            // _btnInvert
            //
            this._btnInvert.Location = new System.Drawing.Point(15, 288);
            this._btnInvert.Name = "_btnInvert";
            this._btnInvert.Size = new System.Drawing.Size(270, 30);
            this._btnInvert.TabIndex = 9;
            this._btnInvert.Text = "대상 반전 (INVERT TARGET)";
            this._btnInvert.UseVisualStyleBackColor = true;
            this._btnInvert.Click += new System.EventHandler(this.btnInvert_Click);
            //
            // _btnExport
            //
            this._btnExport.Location = new System.Drawing.Point(15, 324);
            this._btnExport.Name = "_btnExport";
            this._btnExport.Size = new System.Drawing.Size(130, 30);
            this._btnExport.TabIndex = 10;
            this._btnExport.Text = "내보내기 (EXPORT)";
            this._btnExport.UseVisualStyleBackColor = true;
            this._btnExport.Click += new System.EventHandler(this.btnExport_Click);
            //
            // _btnImport
            //
            this._btnImport.Location = new System.Drawing.Point(155, 324);
            this._btnImport.Name = "_btnImport";
            this._btnImport.Size = new System.Drawing.Size(130, 30);
            this._btnImport.TabIndex = 11;
            this._btnImport.Text = "불러오기 (IMPORT)";
            this._btnImport.UseVisualStyleBackColor = true;
            this._btnImport.Click += new System.EventHandler(this.btnImport_Click);
            //
            // _lblInfo
            //
            this._lblInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._lblInfo.Location = new System.Drawing.Point(12, 366);
            this._lblInfo.Name = "_lblInfo";
            this._lblInfo.Size = new System.Drawing.Size(275, 70);
            this._lblInfo.TabIndex = 12;
            this._lblInfo.Text = "";
            //
            // InputDieMapPage
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this._mapView);
            this.Controls.Add(this._panelLeft);
            this.Name = "InputDieMapPage";
            this.Size = new System.Drawing.Size(900, 700);
            this.Load += new System.EventHandler(this.OnPageLoad);
            this._panelLeft.ResumeLayout(false);
            this._panelLeft.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nSideSkip)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nTbSkip)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
