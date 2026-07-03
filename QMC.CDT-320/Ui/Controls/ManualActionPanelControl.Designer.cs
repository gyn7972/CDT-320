namespace QMC.CDT_320.Ui.Controls
{
    partial class ManualActionPanelControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel buttonsHost;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.buttonsHost = new System.Windows.Forms.TableLayoutPanel();
            this.SuspendLayout();
            //
            // buttonsHost
            //
            this.buttonsHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.buttonsHost.ColumnCount = 2;
            this.buttonsHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.buttonsHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.buttonsHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonsHost.Location = new System.Drawing.Point(0, 0);
            this.buttonsHost.Margin = new System.Windows.Forms.Padding(0);
            this.buttonsHost.Name = "buttonsHost";
            this.buttonsHost.Padding = new System.Windows.Forms.Padding(3);
            this.buttonsHost.RowCount = 1;
            this.buttonsHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.buttonsHost.Size = new System.Drawing.Size(553, 231);
            this.buttonsHost.TabIndex = 0;
            //
            // ManualActionPanelControl
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.Controls.Add(this.buttonsHost);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "ManualActionPanelControl";
            this.Size = new System.Drawing.Size(553, 231);
            this.ResumeLayout(false);
        }
    }
}
