namespace QMC.BottomInspectTest
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Button btnFolder;
        private System.Windows.Forms.TextBox txtFolder;
        private System.Windows.Forms.Label lblCount;

        private System.Windows.Forms.ListBox lstFiles;
        private System.Windows.Forms.PictureBox picView;
        private System.Windows.Forms.PropertyGrid grdParams;

        private System.Windows.Forms.ListView lvResults;
        private System.Windows.Forms.ColumnHeader colFile;
        private System.Windows.Forms.ColumnHeader colResult;
        private System.Windows.Forms.ColumnHeader colWidth;
        private System.Windows.Forms.ColumnHeader colHeight;
        private System.Windows.Forms.ColumnHeader colAngle;
        private System.Windows.Forms.ColumnHeader colChip;
        private System.Windows.Forms.ColumnHeader colForeign;
        private System.Windows.Forms.ColumnHeader colMs;
        private System.Windows.Forms.ColumnHeader colThread;

        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Button btnPrev;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Label lblThreads;
        private System.Windows.Forms.NumericUpDown numThreads;
        private System.Windows.Forms.Label lblStatus;

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
            this.pnlTop = new System.Windows.Forms.Panel();
            this.btnFolder = new System.Windows.Forms.Button();
            this.txtFolder = new System.Windows.Forms.TextBox();
            this.lblCount = new System.Windows.Forms.Label();
            this.lstFiles = new System.Windows.Forms.ListBox();
            this.picView = new System.Windows.Forms.PictureBox();
            this.grdParams = new System.Windows.Forms.PropertyGrid();
            this.lvResults = new System.Windows.Forms.ListView();
            this.colFile = new System.Windows.Forms.ColumnHeader();
            this.colResult = new System.Windows.Forms.ColumnHeader();
            this.colWidth = new System.Windows.Forms.ColumnHeader();
            this.colHeight = new System.Windows.Forms.ColumnHeader();
            this.colAngle = new System.Windows.Forms.ColumnHeader();
            this.colChip = new System.Windows.Forms.ColumnHeader();
            this.colForeign = new System.Windows.Forms.ColumnHeader();
            this.colMs = new System.Windows.Forms.ColumnHeader();
            this.colThread = new System.Windows.Forms.ColumnHeader();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnPlay = new System.Windows.Forms.Button();
            this.lblThreads = new System.Windows.Forms.Label();
            this.numThreads = new System.Windows.Forms.NumericUpDown();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlTop.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThreads)).BeginInit();
            this.SuspendLayout();
            //
            // pnlTop
            //
            this.pnlTop.Controls.Add(this.btnFolder);
            this.pnlTop.Controls.Add(this.txtFolder);
            this.pnlTop.Controls.Add(this.lblCount);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Height = 40;
            this.pnlTop.Padding = new System.Windows.Forms.Padding(6);
            //
            // btnFolder
            //
            this.btnFolder.Location = new System.Drawing.Point(8, 7);
            this.btnFolder.Size = new System.Drawing.Size(100, 27);
            this.btnFolder.Text = "폴더 선택";
            this.btnFolder.Click += new System.EventHandler(this.btnFolder_Click);
            //
            // txtFolder
            //
            this.txtFolder.Location = new System.Drawing.Point(116, 10);
            this.txtFolder.Size = new System.Drawing.Size(760, 21);
            this.txtFolder.ReadOnly = true;
            this.txtFolder.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            //
            // lblCount
            //
            this.lblCount.Location = new System.Drawing.Point(884, 12);
            this.lblCount.Size = new System.Drawing.Size(200, 18);
            this.lblCount.Text = "이미지 0개";
            this.lblCount.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            //
            // lstFiles
            //
            this.lstFiles.Dock = System.Windows.Forms.DockStyle.Left;
            this.lstFiles.Width = 260;
            this.lstFiles.IntegralHeight = false;
            this.lstFiles.HorizontalScrollbar = true;
            this.lstFiles.SelectedIndexChanged += new System.EventHandler(this.lstFiles_SelectedIndexChanged);
            //
            // picView
            //
            this.picView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picView.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picView.BackColor = System.Drawing.Color.Black;
            //
            // grdParams
            //
            this.grdParams.Dock = System.Windows.Forms.DockStyle.Right;
            this.grdParams.Width = 340;
            this.grdParams.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.grdParams.ToolbarVisible = false;
            //
            // lvResults
            //
            this.lvResults.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lvResults.Height = 170;
            this.lvResults.View = System.Windows.Forms.View.Details;
            this.lvResults.FullRowSelect = true;
            this.lvResults.HideSelection = false;
            this.lvResults.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colFile, this.colResult, this.colWidth, this.colHeight, this.colAngle, this.colChip, this.colForeign, this.colMs, this.colThread });
            this.colFile.Text = "파일"; this.colFile.Width = 240;
            this.colResult.Text = "결과"; this.colResult.Width = 90;
            this.colWidth.Text = "W(mm)"; this.colWidth.Width = 70;
            this.colHeight.Text = "H(mm)"; this.colHeight.Width = 70;
            this.colAngle.Text = "Angle"; this.colAngle.Width = 60;
            this.colChip.Text = "Chip max"; this.colChip.Width = 75;
            this.colForeign.Text = "Foreign"; this.colForeign.Width = 70;
            this.colMs.Text = "시간(ms)"; this.colMs.Width = 70;
            this.colThread.Text = "Thread"; this.colThread.Width = 60;
            this.lvResults.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.lvResults_ItemSelectionChanged);
            //
            // pnlBottom
            //
            this.pnlBottom.Controls.Add(this.btnPrev);
            this.pnlBottom.Controls.Add(this.btnNext);
            this.pnlBottom.Controls.Add(this.btnPlay);
            this.pnlBottom.Controls.Add(this.lblThreads);
            this.pnlBottom.Controls.Add(this.numThreads);
            this.pnlBottom.Controls.Add(this.lblStatus);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Height = 42;
            //
            // btnPrev
            //
            this.btnPrev.Location = new System.Drawing.Point(8, 7);
            this.btnPrev.Size = new System.Drawing.Size(90, 28);
            this.btnPrev.Text = "◀ 이전";
            this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);
            //
            // btnNext
            //
            this.btnNext.Location = new System.Drawing.Point(104, 7);
            this.btnNext.Size = new System.Drawing.Size(90, 28);
            this.btnNext.Text = "다음 ▶";
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            //
            // btnPlay
            //
            this.btnPlay.Location = new System.Drawing.Point(200, 7);
            this.btnPlay.Size = new System.Drawing.Size(110, 28);
            this.btnPlay.Text = "▶ 재생";
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            //
            // lblThreads
            //
            this.lblThreads.Location = new System.Drawing.Point(324, 13);
            this.lblThreads.Size = new System.Drawing.Size(70, 18);
            this.lblThreads.Text = "쓰레드 수";
            //
            // numThreads
            //
            this.numThreads.Location = new System.Drawing.Point(394, 10);
            this.numThreads.Size = new System.Drawing.Size(60, 21);
            this.numThreads.Minimum = 1;
            this.numThreads.Maximum = 32;
            this.numThreads.Value = 8;
            //
            // lblStatus
            //
            this.lblStatus.Location = new System.Drawing.Point(470, 13);
            this.lblStatus.Size = new System.Drawing.Size(700, 18);
            this.lblStatus.Text = "Ready.";
            this.lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1480, 900);
            this.Controls.Add(this.picView);
            this.Controls.Add(this.lvResults);
            this.Controls.Add(this.lstFiles);
            this.Controls.Add(this.grdParams);
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.pnlBottom);
            this.Text = "Bottom 표면 검사 테스트";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.pnlBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThreads)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
