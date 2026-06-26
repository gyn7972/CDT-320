namespace QMC.Vision.Ui.Pages
{
    partial class PickupSequencePage
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel _panelLeft;
        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.GroupBox _grpTarget;
        private System.Windows.Forms.RadioButton _rbInput;
        private System.Windows.Forms.RadioButton _rbOutput;
        private System.Windows.Forms.GroupBox _grpCorner;
        private System.Windows.Forms.RadioButton _rbTL;
        private System.Windows.Forms.RadioButton _rbTR;
        private System.Windows.Forms.RadioButton _rbBL;
        private System.Windows.Forms.RadioButton _rbBR;
        private System.Windows.Forms.GroupBox _grpDir;
        private System.Windows.Forms.RadioButton _rbHoriz;
        private System.Windows.Forms.RadioButton _rbVert;
        private System.Windows.Forms.GroupBox _grpPattern;
        private System.Windows.Forms.RadioButton _rbStraight;
        private System.Windows.Forms.RadioButton _rbZigZag;
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
            this._grpTarget = new System.Windows.Forms.GroupBox();
            this._rbInput = new System.Windows.Forms.RadioButton();
            this._rbOutput = new System.Windows.Forms.RadioButton();
            this._grpCorner = new System.Windows.Forms.GroupBox();
            this._rbTL = new System.Windows.Forms.RadioButton();
            this._rbTR = new System.Windows.Forms.RadioButton();
            this._rbBL = new System.Windows.Forms.RadioButton();
            this._rbBR = new System.Windows.Forms.RadioButton();
            this._grpDir = new System.Windows.Forms.GroupBox();
            this._rbHoriz = new System.Windows.Forms.RadioButton();
            this._rbVert = new System.Windows.Forms.RadioButton();
            this._grpPattern = new System.Windows.Forms.GroupBox();
            this._rbStraight = new System.Windows.Forms.RadioButton();
            this._rbZigZag = new System.Windows.Forms.RadioButton();
            this._btnApply = new System.Windows.Forms.Button();
            this._lblInfo = new System.Windows.Forms.Label();
            this._mapView = new QMC.Vision.Ui.Controls.DieMapView();
            this._panelLeft.SuspendLayout();
            this._grpTarget.SuspendLayout();
            this._grpCorner.SuspendLayout();
            this._grpDir.SuspendLayout();
            this._grpPattern.SuspendLayout();
            this.SuspendLayout();
            //
            // _mapView
            //
            this._mapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this._mapView.Caption = "Pickup Order";
            this._mapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mapView.Location = new System.Drawing.Point(280, 0);
            this._mapView.Name = "_mapView";
            this._mapView.ShowWaferOutline = true;
            this._mapView.Size = new System.Drawing.Size(620, 700);
            this._mapView.TabIndex = 1;
            //
            // _panelLeft
            //
            this._panelLeft.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this._panelLeft.Controls.Add(this._lblTitle);
            this._panelLeft.Controls.Add(this._grpTarget);
            this._panelLeft.Controls.Add(this._grpCorner);
            this._panelLeft.Controls.Add(this._grpDir);
            this._panelLeft.Controls.Add(this._grpPattern);
            this._panelLeft.Controls.Add(this._btnApply);
            this._panelLeft.Controls.Add(this._lblInfo);
            this._panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this._panelLeft.Location = new System.Drawing.Point(0, 0);
            this._panelLeft.Name = "_panelLeft";
            this._panelLeft.Padding = new System.Windows.Forms.Padding(12);
            this._panelLeft.Size = new System.Drawing.Size(280, 700);
            this._panelLeft.TabIndex = 0;
            //
            // _lblTitle
            //
            this._lblTitle.AutoSize = true;
            this._lblTitle.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._lblTitle.Location = new System.Drawing.Point(12, 12);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(110, 21);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "픽업 순서";
            //
            // _grpTarget
            //
            this._grpTarget.Controls.Add(this._rbInput);
            this._grpTarget.Controls.Add(this._rbOutput);
            this._grpTarget.Location = new System.Drawing.Point(15, 40);
            this._grpTarget.Name = "_grpTarget";
            this._grpTarget.Size = new System.Drawing.Size(240, 60);
            this._grpTarget.TabIndex = 1;
            this._grpTarget.TabStop = false;
            this._grpTarget.Text = "Pickup target";
            //
            // _rbInput
            //
            this._rbInput.AutoSize = true;
            this._rbInput.Location = new System.Drawing.Point(16, 26);
            this._rbInput.Name = "_rbInput";
            this._rbInput.Size = new System.Drawing.Size(105, 19);
            this._rbInput.TabIndex = 0;
            this._rbInput.Text = "WAFER INPUT";
            this._rbInput.UseVisualStyleBackColor = true;
            this._rbInput.CheckedChanged += new System.EventHandler(this.OnTargetChanged);
            //
            // _rbOutput
            //
            this._rbOutput.AutoSize = true;
            this._rbOutput.Location = new System.Drawing.Point(130, 26);
            this._rbOutput.Name = "_rbOutput";
            this._rbOutput.Size = new System.Drawing.Size(100, 19);
            this._rbOutput.TabIndex = 1;
            this._rbOutput.Text = "BIN OUTPUT";
            this._rbOutput.UseVisualStyleBackColor = true;
            this._rbOutput.CheckedChanged += new System.EventHandler(this.OnTargetChanged);
            //
            // _grpCorner
            //
            this._grpCorner.Controls.Add(this._rbTL);
            this._grpCorner.Controls.Add(this._rbTR);
            this._grpCorner.Controls.Add(this._rbBL);
            this._grpCorner.Controls.Add(this._rbBR);
            this._grpCorner.Location = new System.Drawing.Point(15, 108);
            this._grpCorner.Name = "_grpCorner";
            this._grpCorner.Size = new System.Drawing.Size(240, 96);
            this._grpCorner.TabIndex = 2;
            this._grpCorner.TabStop = false;
            this._grpCorner.Text = "시작 코너 (Start corner)";
            //
            // _rbTL
            //
            this._rbTL.AutoSize = true;
            this._rbTL.Location = new System.Drawing.Point(16, 26);
            this._rbTL.Name = "_rbTL";
            this._rbTL.Size = new System.Drawing.Size(90, 19);
            this._rbTL.TabIndex = 0;
            this._rbTL.Text = "Top-Left";
            this._rbTL.UseVisualStyleBackColor = true;
            this._rbTL.CheckedChanged += new System.EventHandler(this.OnOptionChanged);
            //
            // _rbTR
            //
            this._rbTR.AutoSize = true;
            this._rbTR.Location = new System.Drawing.Point(126, 26);
            this._rbTR.Name = "_rbTR";
            this._rbTR.Size = new System.Drawing.Size(95, 19);
            this._rbTR.TabIndex = 1;
            this._rbTR.Text = "Top-Right";
            this._rbTR.UseVisualStyleBackColor = true;
            this._rbTR.CheckedChanged += new System.EventHandler(this.OnOptionChanged);
            //
            // _rbBL
            //
            this._rbBL.AutoSize = true;
            this._rbBL.Location = new System.Drawing.Point(16, 58);
            this._rbBL.Name = "_rbBL";
            this._rbBL.Size = new System.Drawing.Size(110, 19);
            this._rbBL.TabIndex = 2;
            this._rbBL.Text = "Bottom-Left";
            this._rbBL.UseVisualStyleBackColor = true;
            this._rbBL.CheckedChanged += new System.EventHandler(this.OnOptionChanged);
            //
            // _rbBR
            //
            this._rbBR.AutoSize = true;
            this._rbBR.Location = new System.Drawing.Point(126, 58);
            this._rbBR.Name = "_rbBR";
            this._rbBR.Size = new System.Drawing.Size(115, 19);
            this._rbBR.TabIndex = 3;
            this._rbBR.Text = "Bottom-Right";
            this._rbBR.UseVisualStyleBackColor = true;
            this._rbBR.CheckedChanged += new System.EventHandler(this.OnOptionChanged);
            //
            // _grpDir
            //
            this._grpDir.Controls.Add(this._rbHoriz);
            this._grpDir.Controls.Add(this._rbVert);
            this._grpDir.Location = new System.Drawing.Point(15, 212);
            this._grpDir.Name = "_grpDir";
            this._grpDir.Size = new System.Drawing.Size(240, 60);
            this._grpDir.TabIndex = 3;
            this._grpDir.TabStop = false;
            this._grpDir.Text = "진행 방향 (Direction)";
            //
            // _rbHoriz
            //
            this._rbHoriz.AutoSize = true;
            this._rbHoriz.Location = new System.Drawing.Point(16, 26);
            this._rbHoriz.Name = "_rbHoriz";
            this._rbHoriz.Size = new System.Drawing.Size(100, 19);
            this._rbHoriz.TabIndex = 0;
            this._rbHoriz.Text = "Horizontal";
            this._rbHoriz.UseVisualStyleBackColor = true;
            this._rbHoriz.CheckedChanged += new System.EventHandler(this.OnOptionChanged);
            //
            // _rbVert
            //
            this._rbVert.AutoSize = true;
            this._rbVert.Location = new System.Drawing.Point(126, 26);
            this._rbVert.Name = "_rbVert";
            this._rbVert.Size = new System.Drawing.Size(90, 19);
            this._rbVert.TabIndex = 1;
            this._rbVert.Text = "Vertical";
            this._rbVert.UseVisualStyleBackColor = true;
            this._rbVert.CheckedChanged += new System.EventHandler(this.OnOptionChanged);
            //
            // _grpPattern
            //
            this._grpPattern.Controls.Add(this._rbStraight);
            this._grpPattern.Controls.Add(this._rbZigZag);
            this._grpPattern.Location = new System.Drawing.Point(15, 280);
            this._grpPattern.Name = "_grpPattern";
            this._grpPattern.Size = new System.Drawing.Size(240, 60);
            this._grpPattern.TabIndex = 4;
            this._grpPattern.TabStop = false;
            this._grpPattern.Text = "패턴 (Pattern)";
            //
            // _rbStraight
            //
            this._rbStraight.AutoSize = true;
            this._rbStraight.Location = new System.Drawing.Point(16, 26);
            this._rbStraight.Name = "_rbStraight";
            this._rbStraight.Size = new System.Drawing.Size(90, 19);
            this._rbStraight.TabIndex = 0;
            this._rbStraight.Text = "Straight";
            this._rbStraight.UseVisualStyleBackColor = true;
            this._rbStraight.CheckedChanged += new System.EventHandler(this.OnOptionChanged);
            //
            // _rbZigZag
            //
            this._rbZigZag.AutoSize = true;
            this._rbZigZag.Location = new System.Drawing.Point(126, 26);
            this._rbZigZag.Name = "_rbZigZag";
            this._rbZigZag.Size = new System.Drawing.Size(90, 19);
            this._rbZigZag.TabIndex = 1;
            this._rbZigZag.Text = "ZigZag";
            this._rbZigZag.UseVisualStyleBackColor = true;
            this._rbZigZag.CheckedChanged += new System.EventHandler(this.OnOptionChanged);
            //
            // _btnApply
            //
            this._btnApply.Location = new System.Drawing.Point(15, 350);
            this._btnApply.Name = "_btnApply";
            this._btnApply.Size = new System.Drawing.Size(240, 34);
            this._btnApply.TabIndex = 5;
            this._btnApply.Text = "적용/저장";
            this._btnApply.UseVisualStyleBackColor = true;
            this._btnApply.Click += new System.EventHandler(this.btnApply_Click);
            //
            // _lblInfo
            //
            this._lblInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this._lblInfo.Location = new System.Drawing.Point(12, 398);
            this._lblInfo.Name = "_lblInfo";
            this._lblInfo.Size = new System.Drawing.Size(255, 70);
            this._lblInfo.TabIndex = 6;
            this._lblInfo.Text = "";
            //
            // PickupSequencePage
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this._mapView);
            this.Controls.Add(this._panelLeft);
            this.Name = "PickupSequencePage";
            this.Size = new System.Drawing.Size(900, 700);
            this.Load += new System.EventHandler(this.OnPageLoad);
            this._panelLeft.ResumeLayout(false);
            this._panelLeft.PerformLayout();
            this._grpTarget.ResumeLayout(false);
            this._grpTarget.PerformLayout();
            this._grpCorner.ResumeLayout(false);
            this._grpCorner.PerformLayout();
            this._grpDir.ResumeLayout(false);
            this._grpDir.PerformLayout();
            this._grpPattern.ResumeLayout(false);
            this._grpPattern.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
