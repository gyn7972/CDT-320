namespace QMC.LoadPorts
{
    partial class PlateTransferLoadPortMaintenanceForm
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
            this.tabControlSlotState = new MechaSys.SoftBricks.Hmi.Controls.TabControlX();
            this.tabPagePlate = new MechaSys.SoftBricks.Hmi.Controls.TabPageX();
            this.slotStateView2 = new MechaSys.SoftBricks.Materials.Controls.SlotStateView();
            this.slotStateView1 = new MechaSys.SoftBricks.Materials.Controls.SlotStateView();
            this.tabPageGeneral.SuspendLayout();
            this.tabPageDigitalIo.SuspendLayout();
            this.tabPageAnalogIo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.moduleBox)).BeginInit();
            this.tabControl.SuspendLayout();
            this.tabControlSlotState.SuspendLayout();
            this.tabPagePlate.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPageGeneral
            // 
            this.tabPageGeneral.Controls.Add(this.tabControlSlotState);
            this.tabPageGeneral.Controls.Add(this.partStateGrid);
            this.tabPageGeneral.Controls.Add(this.partControlBox);
            this.tabPageGeneral.Controls.Add(this.moduleBox);
            this.tabPageGeneral.Name = "tabPageGeneral";
            this.tabPageGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageGeneral.TabIndex = 0;
            this.tabPageGeneral.Text = "General";
            this.tabPageGeneral.Controls.SetChildIndex(this.moduleBox, 0);
            this.tabPageGeneral.Controls.SetChildIndex(this.partControlBox, 0);
            this.tabPageGeneral.Controls.SetChildIndex(this.partStateGrid, 0);
            this.tabPageGeneral.Controls.SetChildIndex(this.tabControlSlotState, 0);
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
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.ItemSize = new System.Drawing.Size(75, 40);
            this.tabControl.Location = new System.Drawing.Point(0, 23);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Windows.Forms.Padding(3);
            this.tabControl.Size = new System.Drawing.Size(1162, 817);
            this.tabControl.SizeMode = MechaSys.SoftBricks.Hmi.Controls.TabControlX.TabSizeModes.Normal;
            this.tabControl.TabIndex = 5;
            // 
            // tabControlSlotState
            // 
            this.tabControlSlotState.Controls.Add(this.tabPagePlate);
            this.tabControlSlotState.ItemSize = new System.Drawing.Size(100, 45);
            this.tabControlSlotState.Location = new System.Drawing.Point(137, 6);
            this.tabControlSlotState.Name = "tabControlSlotState";
            this.tabControlSlotState.Padding = new System.Windows.Forms.Padding(3);
            this.tabControlSlotState.Size = new System.Drawing.Size(583, 741);
            this.tabControlSlotState.TabIndex = 10;
            // 
            // tabPagePlate
            // 
            this.tabPagePlate.Controls.Add(this.slotStateView2);
            this.tabPagePlate.Controls.Add(this.slotStateView1);
            this.tabPagePlate.Name = "tabPagePlate";
            this.tabPagePlate.Padding = new System.Windows.Forms.Padding(3);
            this.tabPagePlate.TabIndex = 0;
            this.tabPagePlate.Text = "Plate";
            // 
            // slotStateView2
            // 
            this.slotStateView2.BackColor = System.Drawing.SystemColors.Control;
            this.slotStateView2.Carrier = null;
            this.slotStateView2.DisplayProcessingStateText = null;
            this.slotStateView2.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView2.Location = new System.Drawing.Point(296, 6);
            this.slotStateView2.Name = "slotStateView2";
            this.slotStateView2.ProcessAborted = System.Drawing.Color.HotPink;
            this.slotStateView2.ProcessInProcess = System.Drawing.Color.LimeGreen;
            this.slotStateView2.ProcessLost = System.Drawing.Color.DarkKhaki;
            this.slotStateView2.ProcessNeedsProcessing = System.Drawing.Color.LightGreen;
            this.slotStateView2.ProcessNone = System.Drawing.Color.DarkGray;
            this.slotStateView2.ProcessNotExist = System.Drawing.Color.Gray;
            this.slotStateView2.ProcessProcessed = System.Drawing.Color.SeaGreen;
            this.slotStateView2.ProcessRejected = System.Drawing.Color.Thistle;
            this.slotStateView2.ProcessSkipped = System.Drawing.Color.Plum;
            this.slotStateView2.ProcessStopped = System.Drawing.Color.LightPink;
            this.slotStateView2.Size = new System.Drawing.Size(275, 675);
            this.slotStateView2.SlotCrossSlotted = System.Drawing.Color.DarkOrange;
            this.slotStateView2.SlotDoubleSlotted = System.Drawing.Color.HotPink;
            this.slotStateView2.SlotEmpty = System.Drawing.Color.Gray;
            this.slotStateView2.SlotFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView2.SlotForeColor = System.Drawing.SystemColors.ControlText;
            this.slotStateView2.SlotNoBackColor = System.Drawing.SystemColors.Control;
            this.slotStateView2.SlotNoFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView2.SlotNoForeColor = System.Drawing.SystemColors.ControlText;
            this.slotStateView2.SlotNotEmpty = System.Drawing.Color.Sienna;
            this.slotStateView2.SlotOccupied = System.Drawing.Color.LimeGreen;
            this.slotStateView2.SlotPadding = new System.Windows.Forms.Padding(2);
            this.slotStateView2.SlotUndefined = System.Drawing.Color.Chocolate;
            this.slotStateView2.TabIndex = 1;
            // 
            // slotStateView1
            // 
            this.slotStateView1.BackColor = System.Drawing.SystemColors.Control;
            this.slotStateView1.Carrier = null;
            this.slotStateView1.DisplayProcessingStateText = null;
            this.slotStateView1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView1.Location = new System.Drawing.Point(6, 6);
            this.slotStateView1.Name = "slotStateView1";
            this.slotStateView1.ProcessAborted = System.Drawing.Color.HotPink;
            this.slotStateView1.ProcessInProcess = System.Drawing.Color.LimeGreen;
            this.slotStateView1.ProcessLost = System.Drawing.Color.DarkKhaki;
            this.slotStateView1.ProcessNeedsProcessing = System.Drawing.Color.LightGreen;
            this.slotStateView1.ProcessNone = System.Drawing.Color.DarkGray;
            this.slotStateView1.ProcessNotExist = System.Drawing.Color.Gray;
            this.slotStateView1.ProcessProcessed = System.Drawing.Color.SeaGreen;
            this.slotStateView1.ProcessRejected = System.Drawing.Color.Thistle;
            this.slotStateView1.ProcessSkipped = System.Drawing.Color.Plum;
            this.slotStateView1.ProcessStopped = System.Drawing.Color.LightPink;
            this.slotStateView1.Size = new System.Drawing.Size(274, 675);
            this.slotStateView1.SlotCrossSlotted = System.Drawing.Color.DarkOrange;
            this.slotStateView1.SlotDoubleSlotted = System.Drawing.Color.HotPink;
            this.slotStateView1.SlotEmpty = System.Drawing.Color.Gray;
            this.slotStateView1.SlotFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView1.SlotForeColor = System.Drawing.SystemColors.ControlText;
            this.slotStateView1.SlotNoBackColor = System.Drawing.SystemColors.Control;
            this.slotStateView1.SlotNoFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView1.SlotNoForeColor = System.Drawing.SystemColors.ControlText;
            this.slotStateView1.SlotNotEmpty = System.Drawing.Color.Sienna;
            this.slotStateView1.SlotOccupied = System.Drawing.Color.LimeGreen;
            this.slotStateView1.SlotPadding = new System.Windows.Forms.Padding(2);
            this.slotStateView1.SlotUndefined = System.Drawing.Color.Chocolate;
            this.slotStateView1.TabIndex = 0;
            // 
            // PlateTransferLoadPortMaintenanceForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CaptionVisible = true;
            this.ClientSize = new System.Drawing.Size(1162, 840);
            this.Name = "PlateTransferLoadPortMaintenanceForm";
            this.Text = "PlateTransferLoadPortMaintenanceForm";
            this.tabPageGeneral.ResumeLayout(false);
            this.tabPageDigitalIo.ResumeLayout(false);
            this.tabPageAnalogIo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.moduleBox)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.tabControlSlotState.ResumeLayout(false);
            this.tabPagePlate.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.TabControlX tabControlSlotState;
        private MechaSys.SoftBricks.Hmi.Controls.TabPageX tabPagePlate;
        private MechaSys.SoftBricks.Materials.Controls.SlotStateView slotStateView1;
        private MechaSys.SoftBricks.Materials.Controls.SlotStateView slotStateView2;
    }
}