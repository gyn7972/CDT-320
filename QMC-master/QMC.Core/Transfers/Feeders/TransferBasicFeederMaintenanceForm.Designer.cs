namespace QMC.Transfers.Feeders
{
    partial class TransferBasicFeederMaintenanceForm
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
            this.relatedTransferredModulePortLocationSelector1 = new MechaSys.SoftBricks.Transfer.Controls.TransferredModulePortLocationSelector();
            this.tabPageXTeachingRelatedSecondary = new MechaSys.SoftBricks.Hmi.Controls.TabPageX();
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
            this.tabPageTeaching.Controls.Add(this.relatedTransferredModulePortLocationSelector1);
            this.tabPageTeaching.Controls.Add(this.tabControlTeaching);
            this.tabPageTeaching.Controls.Add(this.buttonReadySecondary);
            this.tabPageTeaching.Controls.Add(this.groupBoxTransferDirection);
            this.tabPageTeaching.Controls.Add(this.groupBoxPorts);
            this.tabPageTeaching.Controls.Add(this.groupBoxMacro);
            this.tabPageTeaching.Controls.Add(this.buttonLock);
            this.tabPageTeaching.Controls.Add(this.transferredModuleSelector);
            this.tabPageTeaching.Name = "tabPageTeaching";
            this.tabPageTeaching.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTeaching.TabIndex = 3;
            this.tabPageTeaching.Text = "Teaching";
            this.tabPageTeaching.Controls.SetChildIndex(this.transferredModuleSelector, 0);
            this.tabPageTeaching.Controls.SetChildIndex(this.buttonLock, 0);
            this.tabPageTeaching.Controls.SetChildIndex(this.groupBoxMacro, 0);
            this.tabPageTeaching.Controls.SetChildIndex(this.groupBoxPorts, 0);
            this.tabPageTeaching.Controls.SetChildIndex(this.groupBoxTransferDirection, 0);
            this.tabPageTeaching.Controls.SetChildIndex(this.buttonReadySecondary, 0);
            this.tabPageTeaching.Controls.SetChildIndex(this.tabControlTeaching, 0);
            this.tabPageTeaching.Controls.SetChildIndex(this.relatedTransferredModulePortLocationSelector1, 0);
            // 
            // buttonLock
            // 
            this.buttonLock.Location = new System.Drawing.Point(998, 588);
            // 
            // buttonReadySecondary
            // 
            this.buttonReadySecondary.Location = new System.Drawing.Point(998, 634);
            // 
            // tabControlTeaching
            // 
            this.tabControlTeaching.Controls.Add(this.tabPageTeachingPrimary);
            this.tabControlTeaching.Controls.Add(this.tabPageTeachingSecondary);
            this.tabControlTeaching.Controls.Add(this.tabPageXTeachingRelatedSecondary);
            this.tabControlTeaching.ItemSize = new System.Drawing.Size(75, 40);
            this.tabControlTeaching.Location = new System.Drawing.Point(139, 6);
            this.tabControlTeaching.Name = "tabControlTeaching";
            this.tabControlTeaching.Padding = new System.Windows.Forms.Padding(3);
            this.tabControlTeaching.Size = new System.Drawing.Size(853, 741);
            this.tabControlTeaching.SizeMode = MechaSys.SoftBricks.Hmi.Controls.TabControlX.TabSizeModes.Normal;
            this.tabControlTeaching.TabIndex = 46;
            this.tabControlTeaching.Controls.SetChildIndex(this.tabPageXTeachingRelatedSecondary, 0);
            this.tabControlTeaching.Controls.SetChildIndex(this.tabPageTeachingSecondary, 0);
            this.tabControlTeaching.Controls.SetChildIndex(this.tabPageTeachingPrimary, 0);
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
            // relatedTransferredModulePortLocationSelector1
            // 
            this.relatedTransferredModulePortLocationSelector1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.relatedTransferredModulePortLocationSelector1.Location = new System.Drawing.Point(998, 297);
            this.relatedTransferredModulePortLocationSelector1.Name = "relatedTransferredModulePortLocationSelector1";
            this.relatedTransferredModulePortLocationSelector1.Owner = null;
            this.relatedTransferredModulePortLocationSelector1.Padding = new System.Windows.Forms.Padding(3);
            this.relatedTransferredModulePortLocationSelector1.SelectedModule = null;
            this.relatedTransferredModulePortLocationSelector1.SelectedPortIndex = -1;
            this.relatedTransferredModulePortLocationSelector1.Size = new System.Drawing.Size(125, 285);
            this.relatedTransferredModulePortLocationSelector1.TabIndex = 47;
            this.relatedTransferredModulePortLocationSelector1.BeforeSelectedModuleChanged += new System.EventHandler(this.relatedTransferredModulePortLocationSelector1_BeforeSelectedModuleChanged);
            this.relatedTransferredModulePortLocationSelector1.SelectedModuleChanged += new System.EventHandler(this.relatedTransferredModulePortLocationSelector1_SelectedModuleChanged);
            this.relatedTransferredModulePortLocationSelector1.SelectedPortIndexChanged += new System.EventHandler(this.relatedTransferredModulePortLocationSelector1_SelectedPortIndexChanged);
            // 
            // tabPageXTeachingRelatedSecondary
            // 
            this.tabPageXTeachingRelatedSecondary.Name = "tabPageXTeachingRelatedSecondary";
            this.tabPageXTeachingRelatedSecondary.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageXTeachingRelatedSecondary.TabIndex = 2;
            this.tabPageXTeachingRelatedSecondary.Text = "Related secondary";
            // 
            // TransferBasicFeederMaintenanceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CaptionVisible = true;
            this.ClientSize = new System.Drawing.Size(1162, 840);
            this.Name = "TransferBasicFeederMaintenanceForm";
            this.Text = "BasicFeederMaintenanceForm";
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
        private MechaSys.SoftBricks.Hmi.Controls.TabPageX tabPageXTeachingRelatedSecondary;
        protected MechaSys.SoftBricks.Transfer.Controls.TransferredModulePortLocationSelector relatedTransferredModulePortLocationSelector1;
    }
}