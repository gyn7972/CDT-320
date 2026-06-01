namespace QMC.Recipes
{
    partial class GeneralUnionRecipeEditorForm
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
            if (disposing && (components != null))
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
            this.buttonAssign = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.panelX1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.BackColor = System.Drawing.SystemColors.Control;
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.ItemSize = new System.Drawing.Size(75, 40);
            this.tabControl.Location = new System.Drawing.Point(0, 69);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Windows.Forms.Padding(3);
            this.tabControl.Size = new System.Drawing.Size(1162, 771);
            this.tabControl.SizeMode = MechaSys.SoftBricks.Hmi.Controls.TabControlX.TabSizeModes.Normal;
            this.tabControl.TabIndex = 17;
            // 
            // panelX1
            // 
            this.panelX1.Controls.Add(this.buttonAssign);
            this.panelX1.Controls.SetChildIndex(this.buttonList, 0);
            this.panelX1.Controls.SetChildIndex(this.labelIndentifierTitle, 0);
            this.panelX1.Controls.SetChildIndex(this.buttonSave, 0);
            this.panelX1.Controls.SetChildIndex(this.labelVersion, 0);
            this.panelX1.Controls.SetChildIndex(this.buttonReload, 0);
            this.panelX1.Controls.SetChildIndex(this.buttonDelete, 0);
            this.panelX1.Controls.SetChildIndex(this.labelIdentifier, 0);
            this.panelX1.Controls.SetChildIndex(this.buttonAssign, 0);
            // 
            // buttonAssign
            // 
            this.buttonAssign.Location = new System.Drawing.Point(751, 3);
            this.buttonAssign.Name = "buttonAssign";
            this.buttonAssign.Size = new System.Drawing.Size(75, 40);
            this.buttonAssign.TabIndex = 25;
            this.buttonAssign.Text = "Assign";
            this.buttonAssign.UseVisualStyleBackColor = true;
            this.buttonAssign.Click += new System.EventHandler(this.buttonAssign_Click);
            // 
            // GeneralUnionRecipeEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CaptionVisible = true;
            this.ClientSize = new System.Drawing.Size(1162, 840);
            this.Name = "GeneralUnionRecipeEditorForm";
            this.Text = "GeneralUnionRecipeEditorForm";
            this.panelX1.ResumeLayout(false);
            this.panelX1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonAssign;
    }
}