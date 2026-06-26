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
            this._pnWafer = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._tabBottom = new System.Windows.Forms.TabPage();
            this._pnBottom = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._tabBin = new System.Windows.Forms.TabPage();
            this._pnBin = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._tabSide = new System.Windows.Forms.TabPage();
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
            this._tabs.Controls.Add(this._tabWafer);
            this._tabs.Controls.Add(this._tabBottom);
            this._tabs.Controls.Add(this._tabBin);
            this._tabs.Controls.Add(this._tabSide);
            this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabs.Location = new System.Drawing.Point(0, 0);
            this._tabs.Name = "_tabs";
            this._tabs.SelectedIndex = 0;
            this._tabs.Size = new System.Drawing.Size(1100, 760);
            this._tabs.TabIndex = 0;
            // 
            // _tabWafer
            // 
            this._tabWafer.Controls.Add(this._pnWafer);
            this._tabWafer.Location = new System.Drawing.Point(4, 24);
            this._tabWafer.Name = "_tabWafer";
            this._tabWafer.Size = new System.Drawing.Size(1092, 732);
            this._tabWafer.TabIndex = 0;
            this._tabWafer.Text = "Wafer Vision";
            this._tabWafer.UseVisualStyleBackColor = true;
            // 
            // _pnWafer
            // 
            this._pnWafer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnWafer.Location = new System.Drawing.Point(0, 0);
            this._pnWafer.Name = "_pnWafer";
            this._pnWafer.Size = new System.Drawing.Size(1092, 732);
            this._pnWafer.TabIndex = 0;
            // 
            // _tabBottom
            // 
            this._tabBottom.Controls.Add(this._pnBottom);
            this._tabBottom.Location = new System.Drawing.Point(4, 24);
            this._tabBottom.Name = "_tabBottom";
            this._tabBottom.Size = new System.Drawing.Size(1092, 732);
            this._tabBottom.TabIndex = 1;
            this._tabBottom.Text = "Bottom Insp";
            this._tabBottom.UseVisualStyleBackColor = true;
            // 
            // _pnBottom
            // 
            this._pnBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnBottom.Location = new System.Drawing.Point(0, 0);
            this._pnBottom.Name = "_pnBottom";
            this._pnBottom.Size = new System.Drawing.Size(1092, 732);
            this._pnBottom.TabIndex = 0;
            // 
            // _tabBin
            // 
            this._tabBin.Controls.Add(this._pnBin);
            this._tabBin.Location = new System.Drawing.Point(4, 24);
            this._tabBin.Name = "_tabBin";
            this._tabBin.Size = new System.Drawing.Size(1092, 732);
            this._tabBin.TabIndex = 2;
            this._tabBin.Text = "Bin Vision";
            this._tabBin.UseVisualStyleBackColor = true;
            // 
            // _pnBin
            // 
            this._pnBin.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnBin.Location = new System.Drawing.Point(0, 0);
            this._pnBin.Name = "_pnBin";
            this._pnBin.Size = new System.Drawing.Size(1092, 732);
            this._pnBin.TabIndex = 0;
            // 
            // _tabSide
            // 
            this._tabSide.Controls.Add(this._pnSide);
            this._tabSide.Location = new System.Drawing.Point(4, 24);
            this._tabSide.Name = "_tabSide";
            this._tabSide.Size = new System.Drawing.Size(1092, 732);
            this._tabSide.TabIndex = 3;
            this._tabSide.Text = "Side (Front+Rear)";
            this._tabSide.UseVisualStyleBackColor = true;
            // 
            // _pnSide
            // 
            this._pnSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnSide.Location = new System.Drawing.Point(0, 0);
            this._pnSide.Name = "_pnSide";
            this._pnSide.Size = new System.Drawing.Size(1092, 732);
            this._pnSide.TabIndex = 0;
            // VisionViewDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1100, 760);
            this.Controls.Add(this._tabs);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 560);
            this.Name = "VisionViewDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Vision View — 통합 카메라 뷰";
            this._tabs.ResumeLayout(false);
            this._tabWafer.ResumeLayout(false);
            this._tabBottom.ResumeLayout(false);
            this._tabBin.ResumeLayout(false);
            this._tabSide.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
