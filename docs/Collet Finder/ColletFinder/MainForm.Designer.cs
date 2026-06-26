namespace ColletFinder
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

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
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnLoad = new System.Windows.Forms.Button();
            this.lblBlock = new System.Windows.Forms.Label();
            this.numBlock = new System.Windows.Forms.NumericUpDown();
            this.lblThreshold = new System.Windows.Forms.Label();
            this.numThreshold = new System.Windows.Forms.NumericUpDown();
            this.btnProcess = new System.Windows.Forms.Button();
            this.btnClearRoi = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.chkCuda = new System.Windows.Forms.CheckBox();
            this.chkFast = new System.Windows.Forms.CheckBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.split = new System.Windows.Forms.SplitContainer();
            this.picSource = new System.Windows.Forms.PictureBox();
            this.lblSrcHeader = new System.Windows.Forms.Label();
            this.picResult = new System.Windows.Forms.PictureBox();
            this.lblResHeader = new System.Windows.Forms.Label();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBlock)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picResult)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.btnLoad);
            this.panelTop.Controls.Add(this.lblBlock);
            this.panelTop.Controls.Add(this.numBlock);
            this.panelTop.Controls.Add(this.lblThreshold);
            this.panelTop.Controls.Add(this.numThreshold);
            this.panelTop.Controls.Add(this.btnProcess);
            this.panelTop.Controls.Add(this.btnClearRoi);
            this.panelTop.Controls.Add(this.btnSave);
            this.panelTop.Controls.Add(this.chkCuda);
            this.panelTop.Controls.Add(this.chkFast);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1200, 58);
            this.panelTop.TabIndex = 0;
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(12, 16);
            this.btnLoad.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(110, 26);
            this.btnLoad.TabIndex = 0;
            this.btnLoad.Text = "이미지 열기";
            this.btnLoad.UseVisualStyleBackColor = true;
            // 
            // lblBlock
            // 
            this.lblBlock.AutoSize = true;
            this.lblBlock.Location = new System.Drawing.Point(140, 22);
            this.lblBlock.Name = "lblBlock";
            this.lblBlock.Size = new System.Drawing.Size(81, 12);
            this.lblBlock.TabIndex = 1;
            this.lblBlock.Text = "블럭 크기(px)";
            // 
            // numBlock
            // 
            this.numBlock.Location = new System.Drawing.Point(220, 19);
            this.numBlock.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numBlock.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.numBlock.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numBlock.Name = "numBlock";
            this.numBlock.Size = new System.Drawing.Size(70, 21);
            this.numBlock.TabIndex = 2;
            this.numBlock.Value = new decimal(new int[] {
            9,
            0,
            0,
            0});
            // 
            // lblThreshold
            // 
            this.lblThreshold.AutoSize = true;
            this.lblThreshold.Location = new System.Drawing.Point(305, 22);
            this.lblThreshold.Name = "lblThreshold";
            this.lblThreshold.Size = new System.Drawing.Size(99, 12);
            this.lblThreshold.TabIndex = 3;
            this.lblThreshold.Text = "임계값(표준편차)";
            // 
            // numThreshold
            // 
            this.numThreshold.DecimalPlaces = 1;
            this.numThreshold.Location = new System.Drawing.Point(410, 19);
            this.numThreshold.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numThreshold.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numThreshold.Name = "numThreshold";
            this.numThreshold.Size = new System.Drawing.Size(70, 21);
            this.numThreshold.TabIndex = 4;
            this.numThreshold.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // btnProcess
            // 
            this.btnProcess.Location = new System.Drawing.Point(505, 16);
            this.btnProcess.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(110, 26);
            this.btnProcess.TabIndex = 5;
            this.btnProcess.Text = "처리 실행";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click_1);
            // 
            // btnClearRoi
            // 
            this.btnClearRoi.Location = new System.Drawing.Point(625, 16);
            this.btnClearRoi.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnClearRoi.Name = "btnClearRoi";
            this.btnClearRoi.Size = new System.Drawing.Size(100, 26);
            this.btnClearRoi.TabIndex = 6;
            this.btnClearRoi.Text = "ROI 초기화";
            this.btnClearRoi.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(735, 16);
            this.btnSave.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(110, 26);
            this.btnSave.TabIndex = 7;
            this.btnSave.Text = "결과 저장";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // chkCuda
            // 
            this.chkCuda.AutoSize = true;
            this.chkCuda.Location = new System.Drawing.Point(865, 22);
            this.chkCuda.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkCuda.Name = "chkCuda";
            this.chkCuda.Size = new System.Drawing.Size(85, 16);
            this.chkCuda.TabIndex = 8;
            this.chkCuda.Text = "CUDA 사용";
            this.chkCuda.UseVisualStyleBackColor = true;
            //
            // chkFast
            //
            this.chkFast.AutoSize = true;
            this.chkFast.Checked = false;
            this.chkFast.CheckState = System.Windows.Forms.CheckState.Unchecked;
            this.chkFast.Location = new System.Drawing.Point(965, 22);
            this.chkFast.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkFast.Name = "chkFast";
            this.chkFast.Size = new System.Drawing.Size(180, 16);
            this.chkFast.TabIndex = 9;
            this.chkFast.Text = "빠른 검출(모멘트·노이즈 적을 때)";
            this.chkFast.UseVisualStyleBackColor = true;
            //
            // lblStatus
            //
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Location = new System.Drawing.Point(0, 591);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1200, 18);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "  준비";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // split
            // 
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Location = new System.Drawing.Point(0, 58);
            this.split.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.split.Name = "split";
            // 
            // split.Panel1
            // 
            this.split.Panel1.Controls.Add(this.picSource);
            this.split.Panel1.Controls.Add(this.lblSrcHeader);
            // 
            // split.Panel2
            // 
            this.split.Panel2.Controls.Add(this.picResult);
            this.split.Panel2.Controls.Add(this.lblResHeader);
            this.split.Size = new System.Drawing.Size(1200, 533);
            this.split.SplitterDistance = 595;
            this.split.SplitterWidth = 6;
            this.split.TabIndex = 1;
            // 
            // picSource
            // 
            this.picSource.BackColor = System.Drawing.Color.Black;
            this.picSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picSource.Location = new System.Drawing.Point(0, 19);
            this.picSource.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.picSource.Name = "picSource";
            this.picSource.Size = new System.Drawing.Size(595, 514);
            this.picSource.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picSource.TabIndex = 0;
            this.picSource.TabStop = false;
            // 
            // lblSrcHeader
            // 
            this.lblSrcHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSrcHeader.Location = new System.Drawing.Point(0, 0);
            this.lblSrcHeader.Name = "lblSrcHeader";
            this.lblSrcHeader.Size = new System.Drawing.Size(595, 19);
            this.lblSrcHeader.TabIndex = 1;
            this.lblSrcHeader.Text = "  원본 / ROI (마우스로 드래그하여 ROI 지정)";
            this.lblSrcHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // picResult
            // 
            this.picResult.BackColor = System.Drawing.Color.Black;
            this.picResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picResult.Location = new System.Drawing.Point(0, 19);
            this.picResult.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.picResult.Name = "picResult";
            this.picResult.Size = new System.Drawing.Size(599, 514);
            this.picResult.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picResult.TabIndex = 0;
            this.picResult.TabStop = false;
            // 
            // lblResHeader
            // 
            this.lblResHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblResHeader.Location = new System.Drawing.Point(0, 0);
            this.lblResHeader.Name = "lblResHeader";
            this.lblResHeader.Size = new System.Drawing.Size(599, 19);
            this.lblResHeader.TabIndex = 1;
            this.lblResHeader.Text = "  결과 (표준편차 ≥ 임계값 → 흰색)";
            this.lblResHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 609);
            this.Controls.Add(this.split);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.panelTop);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MinimumSize = new System.Drawing.Size(900, 456);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Collet Finder — ROI 표준편차 텍스처 필터";
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBlock)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThreshold)).EndInit();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picResult)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Label lblBlock;
        private System.Windows.Forms.NumericUpDown numBlock;
        private System.Windows.Forms.Label lblThreshold;
        private System.Windows.Forms.NumericUpDown numThreshold;
        private System.Windows.Forms.Button btnProcess;
        private System.Windows.Forms.Button btnClearRoi;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.CheckBox chkCuda;
        private System.Windows.Forms.CheckBox chkFast;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.PictureBox picSource;
        private System.Windows.Forms.Label lblSrcHeader;
        private System.Windows.Forms.PictureBox picResult;
        private System.Windows.Forms.Label lblResHeader;
    }
}
