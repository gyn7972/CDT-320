namespace QMC.CDT_320.Ui.Controls
{
    partial class JogSpeedControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel speedTrackLayout;
        private System.Windows.Forms.Panel pnlSpeedSlider;
        private System.Windows.Forms.Label lblSpeedHigh;
        private System.Windows.Forms.Label lblSpeedMid;
        private System.Windows.Forms.Label lblSpeedLow;
        private System.Windows.Forms.Label lblSpeedValue;

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && (components != null))
                    components.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.speedTrackLayout = new System.Windows.Forms.TableLayoutPanel();
            this.pnlSpeedSlider = new System.Windows.Forms.Panel();
            this.lblSpeedHigh = new System.Windows.Forms.Label();
            this.lblSpeedMid = new System.Windows.Forms.Label();
            this.lblSpeedLow = new System.Windows.Forms.Label();
            this.lblSpeedValue = new System.Windows.Forms.Label();
            this.rootLayout.SuspendLayout();
            this.speedTrackLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.speedTrackLayout, 0, 0);
            this.rootLayout.Controls.Add(this.lblSpeedValue, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 10F));
            this.rootLayout.Size = new System.Drawing.Size(110, 275);
            this.rootLayout.TabIndex = 0;
            // 
            // speedTrackLayout
            // 
            this.speedTrackLayout.ColumnCount = 2;
            this.speedTrackLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedTrackLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.speedTrackLayout.Controls.Add(this.pnlSpeedSlider, 0, 0);
            this.speedTrackLayout.Controls.Add(this.lblSpeedHigh, 1, 0);
            this.speedTrackLayout.Controls.Add(this.lblSpeedMid, 1, 1);
            this.speedTrackLayout.Controls.Add(this.lblSpeedLow, 1, 2);
            this.speedTrackLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedTrackLayout.Location = new System.Drawing.Point(9, 10);
            this.speedTrackLayout.Margin = new System.Windows.Forms.Padding(0);
            this.speedTrackLayout.Name = "speedTrackLayout";
            this.speedTrackLayout.RowCount = 3;
            this.speedTrackLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.speedTrackLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.speedTrackLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.speedTrackLayout.Size = new System.Drawing.Size(92, 193);
            this.speedTrackLayout.TabIndex = 0;
            // 
            // pnlSpeedSlider
            // 
            this.pnlSpeedSlider.BackColor = System.Drawing.Color.Transparent;
            this.pnlSpeedSlider.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnlSpeedSlider.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSpeedSlider.Location = new System.Drawing.Point(0, 0);
            this.pnlSpeedSlider.Margin = new System.Windows.Forms.Padding(0);
            this.pnlSpeedSlider.Name = "pnlSpeedSlider";
            this.speedTrackLayout.SetRowSpan(this.pnlSpeedSlider, 3);
            this.pnlSpeedSlider.Size = new System.Drawing.Size(44, 193);
            this.pnlSpeedSlider.TabIndex = 0;
            this.pnlSpeedSlider.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlSpeedSlider_Paint);
            this.pnlSpeedSlider.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pnlSpeedSlider_MouseDown);
            this.pnlSpeedSlider.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pnlSpeedSlider_MouseMove);
            this.pnlSpeedSlider.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pnlSpeedSlider_MouseUp);
            this.pnlSpeedSlider.Resize += new System.EventHandler(this.pnlSpeedSlider_Resize);
            // 
            // lblSpeedHigh
            // 
            this.lblSpeedHigh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpeedHigh.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblSpeedHigh.Location = new System.Drawing.Point(47, 0);
            this.lblSpeedHigh.Name = "lblSpeedHigh";
            this.lblSpeedHigh.Size = new System.Drawing.Size(42, 64);
            this.lblSpeedHigh.TabIndex = 1;
            this.lblSpeedHigh.Text = "100%";
            // 
            // lblSpeedMid
            // 
            this.lblSpeedMid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpeedMid.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblSpeedMid.Location = new System.Drawing.Point(47, 64);
            this.lblSpeedMid.Name = "lblSpeedMid";
            this.lblSpeedMid.Size = new System.Drawing.Size(42, 64);
            this.lblSpeedMid.TabIndex = 2;
            this.lblSpeedMid.Text = "50%";
            this.lblSpeedMid.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSpeedLow
            // 
            this.lblSpeedLow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSpeedLow.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblSpeedLow.Location = new System.Drawing.Point(47, 128);
            this.lblSpeedLow.Name = "lblSpeedLow";
            this.lblSpeedLow.Size = new System.Drawing.Size(42, 65);
            this.lblSpeedLow.TabIndex = 3;
            this.lblSpeedLow.Text = "0%";
            this.lblSpeedLow.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // lblSpeedValue
            // 
            this.lblSpeedValue.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.lblSpeedValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSpeedValue.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblSpeedValue.Location = new System.Drawing.Point(12, 203);
            this.lblSpeedValue.Name = "lblSpeedValue";
            this.lblSpeedValue.Size = new System.Drawing.Size(86, 52);
            this.lblSpeedValue.TabIndex = 1;
            this.lblSpeedValue.Text = "50%";
            this.lblSpeedValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // JogSpeedControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.Controls.Add(this.rootLayout);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "JogSpeedControl";
            this.Size = new System.Drawing.Size(110, 275);
            this.rootLayout.ResumeLayout(false);
            this.speedTrackLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
