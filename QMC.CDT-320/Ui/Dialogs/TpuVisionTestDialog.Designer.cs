namespace QMC.CDT_320.Ui.Dialogs
{
    partial class TpuVisionTestDialog
    {
        private System.ComponentModel.IContainer components = null;
        private QMC.CDT_320.Ui.Controls.TpuVisionTestControl tpuVisionTestControl;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tpuVisionTestControl = new QMC.CDT_320.Ui.Controls.TpuVisionTestControl();
            this.SuspendLayout();
            // 
            // tpuVisionTestControl
            // 
            this.tpuVisionTestControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tpuVisionTestControl.Location = new System.Drawing.Point(0, 0);
            this.tpuVisionTestControl.Name = "tpuVisionTestControl";
            this.tpuVisionTestControl.Size = new System.Drawing.Size(1080, 560);
            this.tpuVisionTestControl.TabIndex = 0;
            // 
            // TpuVisionTestDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1080, 560);
            this.Controls.Add(this.tpuVisionTestControl);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 460);
            this.Name = "TpuVisionTestDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Vision Test";
            this.ResumeLayout(false);
        }
    }
}
