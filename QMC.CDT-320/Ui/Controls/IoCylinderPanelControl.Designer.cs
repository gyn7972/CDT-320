namespace QMC.CDT_320.Ui.Controls
{
    partial class IoCylinderPanelControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FlowLayoutPanel rowsHost;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rowsHost = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // rowsHost
            // 
            this.rowsHost.AutoScroll = true;
            this.rowsHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(205)))), ((int)(((byte)(205)))));
            this.rowsHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rowsHost.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.rowsHost.Location = new System.Drawing.Point(0, 0);
            this.rowsHost.Margin = new System.Windows.Forms.Padding(0);
            this.rowsHost.Name = "rowsHost";
            this.rowsHost.Padding = new System.Windows.Forms.Padding(0);
            this.rowsHost.Size = new System.Drawing.Size(190, 300);
            this.rowsHost.TabIndex = 0;
            this.rowsHost.WrapContents = false;
            // 
            // IoCylinderPanelControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(205)))), ((int)(((byte)(205)))));
            this.Controls.Add(this.rowsHost);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "IoCylinderPanelControl";
            this.Size = new System.Drawing.Size(190, 300);
            this.Resize += new System.EventHandler(this.IoCylinderPanelControl_Resize);
            this.ResumeLayout(false);
        }
    }
}
