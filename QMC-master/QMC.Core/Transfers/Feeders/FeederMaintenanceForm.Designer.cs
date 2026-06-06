namespace QMC.Transfers.Feeders
{
    partial class FeederMaintenanceForm
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
            this.tabPageTeaching.SuspendLayout();
            this.groupBoxMacro.SuspendLayout();
            this.tabControlTeaching.SuspendLayout();
            this.tabPageGeneral.SuspendLayout();
            this.tabPageDigitalIo.SuspendLayout();
            this.tabPageAnalogIo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.moduleBox)).BeginInit();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPageTeaching
            // 
            this.tabPageTeaching.Controls.Add(this.transferredModuleSelector);
            this.tabPageTeaching.Controls.Add(this.tabControlTeaching);
            this.tabPageTeaching.Controls.Add(this.buttonReadySecondary);
            this.tabPageTeaching.Controls.Add(this.groupBoxTransferDirection);
            this.tabPageTeaching.Controls.Add(this.groupBoxPorts);
            this.tabPageTeaching.Controls.Add(this.groupBoxMacro);
            this.tabPageTeaching.Controls.Add(this.buttonLock);
            this.tabPageTeaching.Name = "tabPageTeaching";
            this.tabPageTeaching.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTeaching.TabIndex = 3;
            this.tabPageTeaching.Text = "Teaching";
            // 
            // tabControlTeaching
            // 
            this.tabControlTeaching.Controls.Add(this.tabPageTeachingPrimary);
            this.tabControlTeaching.Controls.Add(this.tabPageTeachingSecondary);
            this.tabControlTeaching.ItemSize = new System.Drawing.Size(75, 40);
            this.tabControlTeaching.Location = new System.Drawing.Point(139, 6);
            this.tabControlTeaching.Name = "tabControlTeaching";
            this.tabControlTeaching.Padding = new System.Windows.Forms.Padding(3);
            this.tabControlTeaching.Size = new System.Drawing.Size(853, 741);
            this.tabControlTeaching.SizeMode = MechaSys.SoftBricks.Hmi.Controls.TabControlX.TabSizeModes.Normal;
            this.tabControlTeaching.TabIndex = 46;
            // 
            // tabPageTeachingPrimary
            // 
            this.tabPageTeachingPrimary.Name = "tabPageTeachingPrimary";
            this.tabPageTeachingPrimary.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTeachingPrimary.TabIndex = 0;
            this.tabPageTeachingPrimary.Text = "Primary";
            // 
            // tabPageTeachingSecondary
            // 
            this.tabPageTeachingSecondary.Name = "tabPageTeachingSecondary";
            this.tabPageTeachingSecondary.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTeachingSecondary.TabIndex = 1;
            this.tabPageTeachingSecondary.Text = "Secondary";
            // 
            // tabPageGeneral
            // 
            this.tabPageGeneral.Controls.Add(this.partStateGrid);
            this.tabPageGeneral.Controls.Add(this.partControlBox);
            this.tabPageGeneral.Controls.Add(this.moduleBox);
            this.tabPageGeneral.Name = "tabPageGeneral";
            this.tabPageGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageGeneral.TabIndex = 0;
            this.tabPageGeneral.Text = "General";
            // 
            // tabPageDigitalIo
            // 
            this.tabPageDigitalIo.Controls.Add(this.dioView);
            this.tabPageDigitalIo.Name = "tabPageDigitalIo";
            this.tabPageDigitalIo.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageDigitalIo.TabIndex = 1;
            this.tabPageDigitalIo.Text = "Digital IO";
            // 
            // tabPageAnalogIo
            // 
            this.tabPageAnalogIo.Controls.Add(this.aioView);
            this.tabPageAnalogIo.Name = "tabPageAnalogIo";
            this.tabPageAnalogIo.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAnalogIo.TabIndex = 2;
            this.tabPageAnalogIo.Text = "Analog IO";
            // 
            // aioView
            // 
            this.aioView.Size = new System.Drawing.Size(1150, 762);
            // 
            // partStateGrid
            // 
            this.partStateGrid.Location = new System.Drawing.Point(739, 3);
            this.partStateGrid.Size = new System.Drawing.Size(414, 762);
            // 
            // tabControl
            // 
            this.tabControl.BackColor = System.Drawing.SystemColors.Control;
            this.tabControl.Controls.Add(this.tabPageGeneral);
            this.tabControl.Controls.Add(this.tabPageDigitalIo);
            this.tabControl.Controls.Add(this.tabPageAnalogIo);
            this.tabControl.Controls.Add(this.tabPageTeaching);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.ItemSize = new System.Drawing.Size(75, 40);
            this.tabControl.Location = new System.Drawing.Point(0, 23);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Windows.Forms.Padding(3);
            this.tabControl.Size = new System.Drawing.Size(1162, 817);
            this.tabControl.SizeMode = MechaSys.SoftBricks.Hmi.Controls.TabControlX.TabSizeModes.Normal;
            this.tabControl.TabIndex = 5;
            // 
            // FeederMaintenanceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CaptionVisible = true;
            this.ClientSize = new System.Drawing.Size(1162, 840);
            this.Name = "FeederMaintenanceForm";
            this.Text = "FeederMaintenanceForm";
            this.tabPageTeaching.ResumeLayout(false);
            this.groupBoxMacro.ResumeLayout(false);
            this.tabControlTeaching.ResumeLayout(false);
            this.tabPageGeneral.ResumeLayout(false);
            this.tabPageDigitalIo.ResumeLayout(false);
            this.tabPageAnalogIo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.moduleBox)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}