namespace QMC.CDT_320.Ui.Dialogs
{
    partial class WaferVisionTestDialog
    {
        private System.ComponentModel.IContainer components = null;
        private QMC.CDT_320.Ui.Controls.WaferVisionTestControl waferVisionTestControl;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.waferVisionTestControl = new QMC.CDT_320.Ui.Controls.WaferVisionTestControl();
            this.SuspendLayout();
            // 
            // waferVisionTestControl
            // 
            this.waferVisionTestControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.waferVisionTestControl.Location = new System.Drawing.Point(0, 0);
            this.waferVisionTestControl.Name = "waferVisionTestControl";
            this.waferVisionTestControl.Size = new System.Drawing.Size(1080, 600);
            this.waferVisionTestControl.TabIndex = 0;
            // 
            // WaferVisionTestDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1080, 600);
            this.Controls.Add(this.waferVisionTestControl);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 460);
            this.Name = "WaferVisionTestDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Wafer Vision Test";
            this.ResumeLayout(false);
        }
    }
}
