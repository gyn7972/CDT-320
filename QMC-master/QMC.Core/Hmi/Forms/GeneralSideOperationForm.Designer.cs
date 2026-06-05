namespace QMC.Hmi.Forms
{
    partial class GeneralSideOperationForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralSideOperationForm));
            this.buttonMonitoring = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonOperation = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.flowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.Controls.Add(this.buttonMonitoring);
            this.flowLayoutPanel.Controls.Add(this.buttonOperation);
            // 
            // buttonMonitoring
            // 
            this.buttonMonitoring.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonMonitoring.BackColor = System.Drawing.Color.Transparent;
            this.buttonMonitoring.BottomLabelVisible = true;
            this.buttonMonitoring.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonMonitoring.DisableImage")));
            this.buttonMonitoring.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonMonitoring.EnableImage")));
            this.buttonMonitoring.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonMonitoring.FlatAppearance.BorderSize = 0;
            this.buttonMonitoring.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonMonitoring.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonMonitoring.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonMonitoring.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMonitoring.Image = ((System.Drawing.Image)(resources.GetObject("buttonMonitoring.Image")));
            this.buttonMonitoring.Location = new System.Drawing.Point(10, 739);
            this.buttonMonitoring.Name = "buttonMonitoring";
            this.buttonMonitoring.Polygon = false;
            this.buttonMonitoring.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonMonitoring.SelectedImage")));
            this.buttonMonitoring.Size = new System.Drawing.Size(90, 90);
            this.buttonMonitoring.TabIndex = 5;
            this.buttonMonitoring.TabStop = true;
            this.buttonMonitoring.Text = "Monitoring";
            this.buttonMonitoring.UseVisualStyleBackColor = true;
            this.buttonMonitoring.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonOperation
            // 
            this.buttonOperation.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonOperation.BackColor = System.Drawing.Color.Transparent;
            this.buttonOperation.BottomLabelVisible = true;
            this.buttonOperation.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonOperation.DisableImage")));
            this.buttonOperation.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonOperation.EnableImage")));
            this.buttonOperation.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonOperation.FlatAppearance.BorderSize = 0;
            this.buttonOperation.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonOperation.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonOperation.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonOperation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonOperation.Image = ((System.Drawing.Image)(resources.GetObject("buttonOperation.Image")));
            this.buttonOperation.Location = new System.Drawing.Point(10, 644);
            this.buttonOperation.Name = "buttonOperation";
            this.buttonOperation.Polygon = false;
            this.buttonOperation.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonOperation.SelectedImage")));
            this.buttonOperation.Size = new System.Drawing.Size(90, 90);
            this.buttonOperation.TabIndex = 6;
            this.buttonOperation.TabStop = true;
            this.buttonOperation.Text = "Operation";
            this.buttonOperation.UseVisualStyleBackColor = true;
            this.buttonOperation.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // GeneralSideOperationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(110, 834);
            this.Name = "GeneralSideOperationForm";
            this.Text = "GeneralSideOperationForm";
            this.flowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonMonitoring;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonOperation;
    }
}