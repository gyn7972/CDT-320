namespace QMC.CDT_320.Ui.Controls
{
    partial class VisionViewerPanel
    {
        private System.ComponentModel.IContainer components = null;

        private QMC.Common.Ui.Controls.CameraViewBase _cam;
        private System.Windows.Forms.FlowLayoutPanel _bar;
        private System.Windows.Forms.CheckBox _chkViewer;
        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblStat;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._cam = new QMC.Common.Ui.Controls.CameraViewBase();
            this._bar = new System.Windows.Forms.FlowLayoutPanel();
            this._chkViewer = new System.Windows.Forms.CheckBox();
            this._lblTitle = new System.Windows.Forms.Label();
            this._lblStat = new System.Windows.Forms.Label();
            this._bar.SuspendLayout();
            this.SuspendLayout();
            //
            // _cam
            //
            this._cam.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cam.Name = "_cam";
            this._cam.ShowToolbar = true;
            //
            // _bar
            //
            this._bar.Controls.Add(this._lblTitle);
            this._bar.Controls.Add(this._chkViewer);
            this._bar.Controls.Add(this._lblStat);
            this._bar.Dock = System.Windows.Forms.DockStyle.Top;
            this._bar.Height = 30;
            this._bar.Name = "_bar";
            this._bar.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this._bar.WrapContents = false;
            //
            // _chkViewer — 뷰어(라이브 스트림) ON/OFF 토글. 버튼 모양 체크박스.
            //
            this._chkViewer.Appearance = System.Windows.Forms.Appearance.Button;
            this._chkViewer.AutoSize = true;
            this._chkViewer.Margin = new System.Windows.Forms.Padding(10, 3, 4, 3);
            this._chkViewer.Name = "_chkViewer";
            this._chkViewer.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this._chkViewer.Text = "뷰어 OFF";
            this._chkViewer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._chkViewer.UseVisualStyleBackColor = true;
            this._chkViewer.CheckedChanged += new System.EventHandler(this.chkViewer_CheckedChanged);
            //
            // _lblTitle
            //
            this._lblTitle.AutoSize = true;
            this._lblTitle.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this._lblTitle.Margin = new System.Windows.Forms.Padding(6, 7, 8, 0);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Text = "이미지";
            //
            // _lblStat
            //
            this._lblStat.AutoSize = true;
            this._lblStat.ForeColor = System.Drawing.Color.DimGray;
            this._lblStat.Margin = new System.Windows.Forms.Padding(8, 7, 0, 0);
            this._lblStat.Name = "_lblStat";
            this._lblStat.Text = "idle";
            //
            // VisionViewerPanel — Fill(_cam) 을 먼저(뒤쪽 z-order), Top(_bar) 을 나중에.
            //
            this.Controls.Add(this._cam);
            this.Controls.Add(this._bar);
            this.Name = "VisionViewerPanel";
            this._bar.ResumeLayout(false);
            this._bar.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
