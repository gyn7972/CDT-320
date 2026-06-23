namespace QMC.CDT_320.Ui.Dialogs
{
    partial class VisionViewDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TabControl _tabs;
        private System.Windows.Forms.TabPage _tabWafer;
        private System.Windows.Forms.TabPage _tabBottom;
        private System.Windows.Forms.TabPage _tabBin;
        private System.Windows.Forms.TabPage _tabSide;
        private QMC.CDT_320.Ui.Controls.VisionViewerPanel _pnWafer;
        private QMC.CDT_320.Ui.Controls.VisionViewerPanel _pnBottom;
        private QMC.CDT_320.Ui.Controls.VisionViewerPanel _pnBin;
        private QMC.CDT_320.Ui.Controls.SideVisionViewerControl _pnSide;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._tabs = new System.Windows.Forms.TabControl();
            this._tabWafer = new System.Windows.Forms.TabPage();
            this._tabBottom = new System.Windows.Forms.TabPage();
            this._tabBin = new System.Windows.Forms.TabPage();
            this._tabSide = new System.Windows.Forms.TabPage();
            this._pnWafer = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._pnBottom = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._pnBin = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._pnSide = new QMC.CDT_320.Ui.Controls.SideVisionViewerControl();
            this._tabs.SuspendLayout();
            this._tabWafer.SuspendLayout();
            this._tabBottom.SuspendLayout();
            this._tabBin.SuspendLayout();
            this._tabSide.SuspendLayout();
            this.SuspendLayout();
            //
            // _tabs
            //
            this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabs.Name = "_tabs";
            this._tabs.Controls.Add(this._tabWafer);
            this._tabs.Controls.Add(this._tabBottom);
            this._tabs.Controls.Add(this._tabBin);
            this._tabs.Controls.Add(this._tabSide);
            //
            // _tabWafer
            //
            this._tabWafer.Name = "_tabWafer";
            this._tabWafer.Text = "Wafer Vision";
            this._tabWafer.UseVisualStyleBackColor = true;
            this._tabWafer.Controls.Add(this._pnWafer);
            //
            // _tabBottom
            //
            this._tabBottom.Name = "_tabBottom";
            this._tabBottom.Text = "Bottom Insp";
            this._tabBottom.UseVisualStyleBackColor = true;
            this._tabBottom.Controls.Add(this._pnBottom);
            //
            // _tabBin
            //
            this._tabBin.Name = "_tabBin";
            this._tabBin.Text = "Bin Vision";
            this._tabBin.UseVisualStyleBackColor = true;
            this._tabBin.Controls.Add(this._pnBin);
            //
            // _tabSide
            //
            this._tabSide.Name = "_tabSide";
            this._tabSide.Text = "Side (Front+Rear)";
            this._tabSide.UseVisualStyleBackColor = true;
            this._tabSide.Controls.Add(this._pnSide);
            //
            // panels
            //
            this._pnWafer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnWafer.Name = "_pnWafer";
            this._pnBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnBottom.Name = "_pnBottom";
            this._pnBin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnBin.Name = "_pnBin";
            this._pnSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnSide.Name = "_pnSide";
            //
            // VisionViewDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1100, 760);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 560);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Name = "VisionViewDialog";
            this.Text = "Vision View — 통합 카메라 뷰";
            this.Controls.Add(this._tabs);
            this._tabWafer.ResumeLayout(false);
            this._tabBottom.ResumeLayout(false);
            this._tabBin.ResumeLayout(false);
            this._tabSide.ResumeLayout(false);
            this._tabs.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
