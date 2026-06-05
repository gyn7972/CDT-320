namespace QMC.Hmi.Forms
{
    partial class GeneralSideDataLogForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralSideDataLogForm));
            this.buttonAlarm = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonTracking = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonMaterial = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonControlJob = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonJob = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonLog = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.flowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.Controls.Add(this.buttonAlarm);
            this.flowLayoutPanel.Controls.Add(this.buttonTracking);
            this.flowLayoutPanel.Controls.Add(this.buttonMaterial);
            this.flowLayoutPanel.Controls.Add(this.buttonControlJob);
            this.flowLayoutPanel.Controls.Add(this.buttonJob);
            this.flowLayoutPanel.Controls.Add(this.buttonLog);
            // 
            // buttonAlarm
            // 
            this.buttonAlarm.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonAlarm.BackColor = System.Drawing.Color.Transparent;
            this.buttonAlarm.BottomLabelVisible = true;
            this.buttonAlarm.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonAlarm.DisableImage")));
            this.buttonAlarm.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonAlarm.EnableImage")));
            this.buttonAlarm.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonAlarm.FlatAppearance.BorderSize = 0;
            this.buttonAlarm.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonAlarm.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonAlarm.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonAlarm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonAlarm.Image = ((System.Drawing.Image)(resources.GetObject("buttonAlarm.Image")));
            this.buttonAlarm.Location = new System.Drawing.Point(10, 739);
            this.buttonAlarm.Name = "buttonAlarm";
            this.buttonAlarm.Polygon = false;
            this.buttonAlarm.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonAlarm.SelectedImage")));
            this.buttonAlarm.Size = new System.Drawing.Size(90, 90);
            this.buttonAlarm.TabIndex = 23;
            this.buttonAlarm.TabStop = true;
            this.buttonAlarm.Text = "Alarm";
            this.buttonAlarm.UseVisualStyleBackColor = true;
            this.buttonAlarm.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonTracking
            // 
            this.buttonTracking.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonTracking.BackColor = System.Drawing.Color.Transparent;
            this.buttonTracking.BottomLabelVisible = true;
            this.buttonTracking.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonTracking.DisableImage")));
            this.buttonTracking.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonTracking.EnableImage")));
            this.buttonTracking.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonTracking.FlatAppearance.BorderSize = 0;
            this.buttonTracking.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonTracking.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonTracking.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonTracking.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonTracking.Image = ((System.Drawing.Image)(resources.GetObject("buttonTracking.Image")));
            this.buttonTracking.Location = new System.Drawing.Point(10, 644);
            this.buttonTracking.Name = "buttonTracking";
            this.buttonTracking.Polygon = false;
            this.buttonTracking.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonTracking.SelectedImage")));
            this.buttonTracking.Size = new System.Drawing.Size(90, 90);
            this.buttonTracking.TabIndex = 25;
            this.buttonTracking.TabStop = true;
            this.buttonTracking.Text = "Tracking";
            this.buttonTracking.UseVisualStyleBackColor = true;
            this.buttonTracking.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonMaterial
            // 
            this.buttonMaterial.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonMaterial.BackColor = System.Drawing.Color.Transparent;
            this.buttonMaterial.BottomLabelVisible = true;
            this.buttonMaterial.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonMaterial.DisableImage")));
            this.buttonMaterial.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonMaterial.EnableImage")));
            this.buttonMaterial.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonMaterial.FlatAppearance.BorderSize = 0;
            this.buttonMaterial.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonMaterial.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonMaterial.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonMaterial.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMaterial.Image = ((System.Drawing.Image)(resources.GetObject("buttonMaterial.Image")));
            this.buttonMaterial.Location = new System.Drawing.Point(10, 549);
            this.buttonMaterial.Name = "buttonMaterial";
            this.buttonMaterial.Polygon = false;
            this.buttonMaterial.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonMaterial.SelectedImage")));
            this.buttonMaterial.Size = new System.Drawing.Size(90, 90);
            this.buttonMaterial.TabIndex = 24;
            this.buttonMaterial.TabStop = true;
            this.buttonMaterial.Text = "Material";
            this.buttonMaterial.UseVisualStyleBackColor = true;
            this.buttonMaterial.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonControlJob
            // 
            this.buttonControlJob.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonControlJob.BackColor = System.Drawing.Color.Transparent;
            this.buttonControlJob.BottomLabelVisible = true;
            this.buttonControlJob.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonControlJob.DisableImage")));
            this.buttonControlJob.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonControlJob.EnableImage")));
            this.buttonControlJob.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonControlJob.FlatAppearance.BorderSize = 0;
            this.buttonControlJob.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonControlJob.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonControlJob.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonControlJob.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonControlJob.Image = ((System.Drawing.Image)(resources.GetObject("buttonControlJob.Image")));
            this.buttonControlJob.Location = new System.Drawing.Point(10, 454);
            this.buttonControlJob.Name = "buttonControlJob";
            this.buttonControlJob.Polygon = false;
            this.buttonControlJob.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonControlJob.SelectedImage")));
            this.buttonControlJob.Size = new System.Drawing.Size(90, 90);
            this.buttonControlJob.TabIndex = 26;
            this.buttonControlJob.TabStop = true;
            this.buttonControlJob.Text = "CJ, PJ";
            this.buttonControlJob.UseVisualStyleBackColor = true;
            this.buttonControlJob.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonJob
            // 
            this.buttonJob.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonJob.BackColor = System.Drawing.Color.Transparent;
            this.buttonJob.BottomLabelVisible = true;
            this.buttonJob.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonJob.DisableImage")));
            this.buttonJob.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonJob.EnableImage")));
            this.buttonJob.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonJob.FlatAppearance.BorderSize = 0;
            this.buttonJob.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonJob.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonJob.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonJob.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonJob.Image = ((System.Drawing.Image)(resources.GetObject("buttonJob.Image")));
            this.buttonJob.Location = new System.Drawing.Point(10, 359);
            this.buttonJob.Name = "buttonJob";
            this.buttonJob.Polygon = false;
            this.buttonJob.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonJob.SelectedImage")));
            this.buttonJob.Size = new System.Drawing.Size(90, 90);
            this.buttonJob.TabIndex = 27;
            this.buttonJob.TabStop = true;
            this.buttonJob.Text = "Job";
            this.buttonJob.UseVisualStyleBackColor = true;
            this.buttonJob.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonLog
            // 
            this.buttonLog.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonLog.BackColor = System.Drawing.Color.Transparent;
            this.buttonLog.BottomLabelVisible = true;
            this.buttonLog.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonLog.DisableImage")));
            this.buttonLog.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonLog.EnableImage")));
            this.buttonLog.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonLog.FlatAppearance.BorderSize = 0;
            this.buttonLog.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonLog.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonLog.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonLog.Image = ((System.Drawing.Image)(resources.GetObject("buttonLog.Image")));
            this.buttonLog.Location = new System.Drawing.Point(10, 264);
            this.buttonLog.Name = "buttonLog";
            this.buttonLog.Polygon = false;
            this.buttonLog.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonLog.SelectedImage")));
            this.buttonLog.Size = new System.Drawing.Size(90, 90);
            this.buttonLog.TabIndex = 28;
            this.buttonLog.TabStop = true;
            this.buttonLog.Text = "Log";
            this.buttonLog.UseVisualStyleBackColor = true;
            this.buttonLog.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // GeneralSideDataLogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(110, 834);
            this.Name = "GeneralSideDataLogForm";
            this.Text = "GeneralSideDataLogForm";
            this.flowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonAlarm;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonTracking;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonMaterial;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonControlJob;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonJob;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonLog;
    }
}