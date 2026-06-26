namespace QMC.CDT_320.Ui.Dialogs
{
    partial class VisionMonitorDialog
    {
        private System.ComponentModel.IContainer components = null;
        private QMC.CDT_320.Ui.Controls.VisionMonitorControl visionMonitorControl;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.visionMonitorControl = new QMC.CDT_320.Ui.Controls.VisionMonitorControl();
            this.SuspendLayout();
            // 
            // visionMonitorControl
            // 
            this.visionMonitorControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.visionMonitorControl.Location = new System.Drawing.Point(0, 0);
            this.visionMonitorControl.Name = "visionMonitorControl";
            this.visionMonitorControl.Size = new System.Drawing.Size(1000, 760);
            this.visionMonitorControl.TabIndex = 0;
            // 
            // VisionMonitorDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1000, 760);
            this.Controls.Add(this.visionMonitorControl);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimizeBox = false;
            this.Name = "VisionMonitorDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Vision Camera Monitor";
            this.ResumeLayout(false);
        }
    }
}
