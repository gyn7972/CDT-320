namespace QMC.Hmi.Forms
{
    partial class GeneralSideUtilityForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralSideUtilityForm));
            this.buttonAccount = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.flowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.Controls.Add(this.buttonAccount);
            // 
            // buttonAccount
            // 
            this.buttonAccount.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonAccount.BackColor = System.Drawing.Color.Transparent;
            this.buttonAccount.BottomLabelVisible = true;
            this.buttonAccount.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonAccount.DisableImage")));
            this.buttonAccount.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonAccount.EnableImage")));
            this.buttonAccount.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonAccount.FlatAppearance.BorderSize = 0;
            this.buttonAccount.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonAccount.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonAccount.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonAccount.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonAccount.Image = ((System.Drawing.Image)(resources.GetObject("buttonAccount.Image")));
            this.buttonAccount.Location = new System.Drawing.Point(10, 739);
            this.buttonAccount.Name = "buttonAccount";
            this.buttonAccount.Polygon = false;
            this.buttonAccount.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonAccount.SelectedImage")));
            this.buttonAccount.Size = new System.Drawing.Size(90, 90);
            this.buttonAccount.TabIndex = 1;
            this.buttonAccount.TabStop = true;
            this.buttonAccount.Text = "Account";
            this.buttonAccount.UseVisualStyleBackColor = true;
            this.buttonAccount.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // GeneralSideUtilityForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(110, 834);
            this.Name = "GeneralSideUtilityForm";
            this.Text = "GeneralSideUtilityForm";
            this.flowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonAccount;
    }
}