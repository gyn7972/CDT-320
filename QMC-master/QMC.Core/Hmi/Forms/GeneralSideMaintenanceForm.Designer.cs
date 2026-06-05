namespace QMC.Hmi.Forms
{
    partial class GeneralSideMaintenanceForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralSideMaintenanceForm));
            this.buttonEquipment = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonModule = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.flowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.Controls.Add(this.buttonEquipment);
            this.flowLayoutPanel.Controls.Add(this.buttonModule);
            // 
            // buttonEquipment
            // 
            this.buttonEquipment.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonEquipment.BackColor = System.Drawing.Color.Transparent;
            this.buttonEquipment.BottomLabelVisible = true;
            this.buttonEquipment.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonEquipment.DisableImage")));
            this.buttonEquipment.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonEquipment.EnableImage")));
            this.buttonEquipment.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonEquipment.FlatAppearance.BorderSize = 0;
            this.buttonEquipment.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonEquipment.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonEquipment.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonEquipment.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonEquipment.Image = ((System.Drawing.Image)(resources.GetObject("buttonEquipment.Image")));
            this.buttonEquipment.Location = new System.Drawing.Point(10, 739);
            this.buttonEquipment.Name = "buttonEquipment";
            this.buttonEquipment.Polygon = false;
            this.buttonEquipment.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonEquipment.SelectedImage")));
            this.buttonEquipment.Size = new System.Drawing.Size(90, 90);
            this.buttonEquipment.TabIndex = 10;
            this.buttonEquipment.TabStop = true;
            this.buttonEquipment.Text = "Equipment";
            this.buttonEquipment.UseVisualStyleBackColor = true;
            this.buttonEquipment.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonModule
            // 
            this.buttonModule.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonModule.BackColor = System.Drawing.Color.Transparent;
            this.buttonModule.BottomLabelVisible = true;
            this.buttonModule.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonModule.DisableImage")));
            this.buttonModule.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonModule.EnableImage")));
            this.buttonModule.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonModule.FlatAppearance.BorderSize = 0;
            this.buttonModule.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonModule.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonModule.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonModule.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonModule.Image = ((System.Drawing.Image)(resources.GetObject("buttonModule.Image")));
            this.buttonModule.Location = new System.Drawing.Point(10, 644);
            this.buttonModule.Name = "buttonModule";
            this.buttonModule.Polygon = false;
            this.buttonModule.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonModule.SelectedImage")));
            this.buttonModule.Size = new System.Drawing.Size(90, 90);
            this.buttonModule.TabIndex = 11;
            this.buttonModule.TabStop = true;
            this.buttonModule.Text = "Module";
            this.buttonModule.UseVisualStyleBackColor = true;
            this.buttonModule.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // GeneralSideMaintenanceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(110, 834);
            this.Name = "GeneralSideMaintenanceForm";
            this.Text = "GeneralSideMaintenanceForm";
            this.flowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonEquipment;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonModule;
    }
}