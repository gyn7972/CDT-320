namespace QMC.Hmi.Forms
{
    partial class GeneralSideForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.flowLayoutPanel = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.SuspendLayout();
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.BottomUp;
            this.flowLayoutPanel.Gap = 95;
            this.flowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel.Name = "flowLayoutPanel";
            this.flowLayoutPanel.Size = new System.Drawing.Size(110, 834);
            this.flowLayoutPanel.TabIndex = 1;
            // 
            // GeneralSideForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(110, 834);
            this.Controls.Add(this.flowLayoutPanel);
            this.Name = "GeneralSideForm";
            this.Text = "GeneralSideForm";
            this.ResumeLayout(false);

        }

        #endregion

        protected MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanel;
    }
}