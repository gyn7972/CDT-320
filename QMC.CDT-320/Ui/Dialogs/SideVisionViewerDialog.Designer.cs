namespace QMC.CDT_320.Ui.Dialogs
{
    partial class SideVisionViewerDialog
    {
        private System.ComponentModel.IContainer components = null;
        private QMC.CDT_320.Ui.Controls.SideVisionViewerControl sideViewer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.sideViewer = new QMC.CDT_320.Ui.Controls.SideVisionViewerControl();
            this.SuspendLayout();
            // 
            // sideViewer
            // 
            this.sideViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sideViewer.Location = new System.Drawing.Point(0, 0);
            this.sideViewer.Margin = new System.Windows.Forms.Padding(0);
            this.sideViewer.Name = "sideViewer";
            this.sideViewer.Size = new System.Drawing.Size(900, 820);
            this.sideViewer.TabIndex = 0;
            // 
            // SideVisionViewerDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 820);
            this.Controls.Add(this.sideViewer);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(700, 560);
            this.Name = "SideVisionViewerDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Side Vision Viewer - Front / Rear";
            this.ResumeLayout(false);
        }
    }
}
