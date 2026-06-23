namespace QMC.CDT_320.Ui.Dialogs
{
    partial class SideVisionViewerDialog
    {
        private System.ComponentModel.IContainer components = null;

        private QMC.CDT_320.Ui.Controls.SideVisionViewerControl _side;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._side = new QMC.CDT_320.Ui.Controls.SideVisionViewerControl();
            this.SuspendLayout();
            //
            // _side
            //
            this._side.Dock = System.Windows.Forms.DockStyle.Fill;
            this._side.Name = "_side";
            //
            // SideVisionViewerDialog
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 820);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(700, 560);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Name = "SideVisionViewerDialog";
            this.Text = "VISION 측면 뷰어 — Front + Rear 동시";
            this.Controls.Add(this._side);
            this.ResumeLayout(false);
        }
    }
}
