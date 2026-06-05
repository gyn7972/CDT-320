namespace QMC.Hmi.Forms
{
    partial class GeneralSemiEquipmentMaintenanceForm
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
            this.layoutBox = new MechaSys.SoftBricks.Hmi.Controls.LayoutBox();
            this.groupBoxSelection = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.buttonDeselectAll = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonSelectAll = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.groupBoxModules = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.buttonUserSelectModules = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonStopModules = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonInServiceModules = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonInitializeModules = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.groupBoxEquipment = new MechaSys.SoftBricks.Hmi.Controls.GroupBoxX();
            this.buttonStopAfterWorkEquipment = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonStartEquipment = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonCleanUp = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonClearJobs = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonInitializeEquipment = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonStopEquipment = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.panelX1 = new MechaSys.SoftBricks.Hmi.Controls.PanelX();
            this.tabPageXImageSensor = new MechaSys.SoftBricks.Hmi.Controls.TabPageX();
            this.cameraView1 = new MechaSys.SoftBricks.Visions.ImageSensorView();
            this.tabPageGeneral.SuspendLayout();
            this.tabControlX1.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.groupBoxSelection.SuspendLayout();
            this.groupBoxModules.SuspendLayout();
            this.groupBoxEquipment.SuspendLayout();
            this.panelX1.SuspendLayout();
            this.tabPageXImageSensor.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPageGeneral
            // 
            this.tabPageGeneral.Controls.Add(this.panelX1);
            this.tabPageGeneral.Controls.Add(this.layoutBox);
            this.tabPageGeneral.Name = "tabPageGeneral";
            this.tabPageGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageGeneral.TabIndex = 0;
            this.tabPageGeneral.Text = "General";
            // 
            // tabControlX1
            // 
            this.tabControlX1.Controls.Add(this.tabPageXImageSensor);
            this.tabControlX1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlX1.ItemSize = new System.Drawing.Size(75, 30);
            this.tabControlX1.Location = new System.Drawing.Point(3, 3);
            this.tabControlX1.Name = "tabControlX1";
            this.tabControlX1.Padding = new System.Windows.Forms.Padding(3);
            this.tabControlX1.Size = new System.Drawing.Size(1150, 762);
            this.tabControlX1.SizeMode = MechaSys.SoftBricks.Hmi.Controls.TabControlX.TabSizeModes.Normal;
            this.tabControlX1.TabIndex = 0;
            this.tabControlX1.Controls.SetChildIndex(this.tabPageXImageSensor, 0);
            // 
            // tabControl
            // 
            this.tabControl.BackColor = System.Drawing.SystemColors.Control;
            this.tabControl.Controls.Add(this.tabPageGeneral);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.ItemSize = new System.Drawing.Size(75, 40);
            this.tabControl.Location = new System.Drawing.Point(0, 23);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Windows.Forms.Padding(3);
            this.tabControl.Size = new System.Drawing.Size(1162, 817);
            this.tabControl.SizeMode = MechaSys.SoftBricks.Hmi.Controls.TabControlX.TabSizeModes.Normal;
            this.tabControl.TabIndex = 5;
            // 
            // layoutBox
            // 
            this.layoutBox.BackColor = System.Drawing.SystemColors.Control;
            this.layoutBox.Editable = false;
            this.layoutBox.FixedSize = false;
            this.layoutBox.Grid = new System.Drawing.Point(4, 4);
            this.layoutBox.Location = new System.Drawing.Point(6, 6);
            this.layoutBox.Name = "layoutBox";
            this.layoutBox.SelectionMode = MechaSys.SoftBricks.Hmi.Controls.LayoutBox.SelectionMethod.Multi;
            this.layoutBox.Size = new System.Drawing.Size(800, 732);
            this.layoutBox.StayRatio = true;
            this.layoutBox.TabIndex = 4;
            // 
            // groupBoxSelection
            // 
            this.groupBoxSelection.BorderStyle = MechaSys.SoftBricks.Hmi.Controls.GroupBoxX.BorderStyles.None;
            this.groupBoxSelection.Controls.Add(this.buttonDeselectAll);
            this.groupBoxSelection.Controls.Add(this.buttonSelectAll);
            this.groupBoxSelection.Location = new System.Drawing.Point(3, 639);
            this.groupBoxSelection.Name = "groupBoxSelection";
            this.groupBoxSelection.Size = new System.Drawing.Size(106, 117);
            this.groupBoxSelection.TabIndex = 14;
            this.groupBoxSelection.TabStop = false;
            this.groupBoxSelection.Text = "Selection";
            // 
            // buttonDeselectAll
            // 
            this.buttonDeselectAll.Location = new System.Drawing.Point(2, 67);
            this.buttonDeselectAll.Name = "buttonDeselectAll";
            this.buttonDeselectAll.Size = new System.Drawing.Size(100, 40);
            this.buttonDeselectAll.TabIndex = 2;
            this.buttonDeselectAll.Text = "Deselect All";
            this.buttonDeselectAll.UseVisualStyleBackColor = true;
            this.buttonDeselectAll.Click += new System.EventHandler(this.buttonSelection_Click);
            // 
            // buttonSelectAll
            // 
            this.buttonSelectAll.Location = new System.Drawing.Point(2, 21);
            this.buttonSelectAll.Name = "buttonSelectAll";
            this.buttonSelectAll.Size = new System.Drawing.Size(100, 40);
            this.buttonSelectAll.TabIndex = 0;
            this.buttonSelectAll.Text = "Select All";
            this.buttonSelectAll.UseVisualStyleBackColor = true;
            this.buttonSelectAll.Click += new System.EventHandler(this.buttonSelection_Click);
            // 
            // groupBoxModules
            // 
            this.groupBoxModules.BorderStyle = MechaSys.SoftBricks.Hmi.Controls.GroupBoxX.BorderStyles.None;
            this.groupBoxModules.Controls.Add(this.buttonUserSelectModules);
            this.groupBoxModules.Controls.Add(this.buttonStopModules);
            this.groupBoxModules.Controls.Add(this.buttonInServiceModules);
            this.groupBoxModules.Controls.Add(this.buttonInitializeModules);
            this.groupBoxModules.Location = new System.Drawing.Point(3, 349);
            this.groupBoxModules.Name = "groupBoxModules";
            this.groupBoxModules.Size = new System.Drawing.Size(106, 228);
            this.groupBoxModules.TabIndex = 13;
            this.groupBoxModules.TabStop = false;
            this.groupBoxModules.Text = "Modules";
            // 
            // buttonUserSelectModules
            // 
            this.buttonUserSelectModules.Enabled = false;
            this.buttonUserSelectModules.Location = new System.Drawing.Point(2, 180);
            this.buttonUserSelectModules.Name = "buttonUserSelectModules";
            this.buttonUserSelectModules.Size = new System.Drawing.Size(100, 40);
            this.buttonUserSelectModules.TabIndex = 3;
            this.buttonUserSelectModules.Text = "UserSelected";
            this.buttonUserSelectModules.UseVisualStyleBackColor = true;
            this.buttonUserSelectModules.Click += new System.EventHandler(this.buttonModules_Click);
            // 
            // buttonStopModules
            // 
            this.buttonStopModules.Enabled = false;
            this.buttonStopModules.Location = new System.Drawing.Point(2, 21);
            this.buttonStopModules.Name = "buttonStopModules";
            this.buttonStopModules.Size = new System.Drawing.Size(100, 40);
            this.buttonStopModules.TabIndex = 0;
            this.buttonStopModules.Text = "Stop";
            this.buttonStopModules.UseVisualStyleBackColor = true;
            this.buttonStopModules.Click += new System.EventHandler(this.buttonModules_Click);
            // 
            // buttonInServiceModules
            // 
            this.buttonInServiceModules.Enabled = false;
            this.buttonInServiceModules.Location = new System.Drawing.Point(2, 134);
            this.buttonInServiceModules.Name = "buttonInServiceModules";
            this.buttonInServiceModules.Size = new System.Drawing.Size(100, 40);
            this.buttonInServiceModules.TabIndex = 2;
            this.buttonInServiceModules.Text = "InService";
            this.buttonInServiceModules.UseVisualStyleBackColor = true;
            this.buttonInServiceModules.Click += new System.EventHandler(this.buttonModules_Click);
            // 
            // buttonInitializeModules
            // 
            this.buttonInitializeModules.Enabled = false;
            this.buttonInitializeModules.Location = new System.Drawing.Point(2, 67);
            this.buttonInitializeModules.Name = "buttonInitializeModules";
            this.buttonInitializeModules.Size = new System.Drawing.Size(100, 40);
            this.buttonInitializeModules.TabIndex = 1;
            this.buttonInitializeModules.Text = "Initialize";
            this.buttonInitializeModules.UseVisualStyleBackColor = true;
            this.buttonInitializeModules.Click += new System.EventHandler(this.buttonModules_Click);
            // 
            // groupBoxEquipment
            // 
            this.groupBoxEquipment.BorderStyle = MechaSys.SoftBricks.Hmi.Controls.GroupBoxX.BorderStyles.None;
            this.groupBoxEquipment.Controls.Add(this.buttonStopAfterWorkEquipment);
            this.groupBoxEquipment.Controls.Add(this.buttonStartEquipment);
            this.groupBoxEquipment.Controls.Add(this.buttonCleanUp);
            this.groupBoxEquipment.Controls.Add(this.buttonClearJobs);
            this.groupBoxEquipment.Controls.Add(this.buttonInitializeEquipment);
            this.groupBoxEquipment.Controls.Add(this.buttonStopEquipment);
            this.groupBoxEquipment.Location = new System.Drawing.Point(3, 3);
            this.groupBoxEquipment.Name = "groupBoxEquipment";
            this.groupBoxEquipment.Size = new System.Drawing.Size(106, 340);
            this.groupBoxEquipment.TabIndex = 12;
            this.groupBoxEquipment.TabStop = false;
            this.groupBoxEquipment.Text = "Equipment";
            // 
            // buttonStopAfterWorkEquipment
            // 
            this.buttonStopAfterWorkEquipment.Location = new System.Drawing.Point(2, 294);
            this.buttonStopAfterWorkEquipment.Name = "buttonStopAfterWorkEquipment";
            this.buttonStopAfterWorkEquipment.Size = new System.Drawing.Size(100, 40);
            this.buttonStopAfterWorkEquipment.TabIndex = 5;
            this.buttonStopAfterWorkEquipment.Text = "Cycle Stop";
            this.buttonStopAfterWorkEquipment.UseVisualStyleBackColor = true;
            this.buttonStopAfterWorkEquipment.Click += new System.EventHandler(this.buttonEquipment_Click);
            // 
            // buttonStartEquipment
            // 
            this.buttonStartEquipment.Location = new System.Drawing.Point(2, 248);
            this.buttonStartEquipment.Name = "buttonStartEquipment";
            this.buttonStartEquipment.Size = new System.Drawing.Size(100, 40);
            this.buttonStartEquipment.TabIndex = 4;
            this.buttonStartEquipment.Text = "Auto";
            this.buttonStartEquipment.UseVisualStyleBackColor = true;
            this.buttonStartEquipment.Click += new System.EventHandler(this.buttonEquipment_Click);
            // 
            // buttonCleanUp
            // 
            this.buttonCleanUp.Location = new System.Drawing.Point(2, 180);
            this.buttonCleanUp.Name = "buttonCleanUp";
            this.buttonCleanUp.Size = new System.Drawing.Size(100, 40);
            this.buttonCleanUp.TabIndex = 3;
            this.buttonCleanUp.Text = "Clean Up";
            this.buttonCleanUp.UseVisualStyleBackColor = true;
            this.buttonCleanUp.Click += new System.EventHandler(this.buttonEquipment_Click);
            // 
            // buttonClearJobs
            // 
            this.buttonClearJobs.Location = new System.Drawing.Point(2, 134);
            this.buttonClearJobs.Name = "buttonClearJobs";
            this.buttonClearJobs.Size = new System.Drawing.Size(100, 40);
            this.buttonClearJobs.TabIndex = 2;
            this.buttonClearJobs.Text = "Clear Jobs";
            this.buttonClearJobs.UseVisualStyleBackColor = true;
            this.buttonClearJobs.Click += new System.EventHandler(this.buttonEquipment_Click);
            // 
            // buttonInitializeEquipment
            // 
            this.buttonInitializeEquipment.Location = new System.Drawing.Point(2, 67);
            this.buttonInitializeEquipment.Name = "buttonInitializeEquipment";
            this.buttonInitializeEquipment.Size = new System.Drawing.Size(100, 40);
            this.buttonInitializeEquipment.TabIndex = 1;
            this.buttonInitializeEquipment.Text = "Initialize";
            this.buttonInitializeEquipment.UseVisualStyleBackColor = true;
            this.buttonInitializeEquipment.Click += new System.EventHandler(this.buttonEquipment_Click);
            // 
            // buttonStopEquipment
            // 
            this.buttonStopEquipment.Location = new System.Drawing.Point(2, 21);
            this.buttonStopEquipment.Name = "buttonStopEquipment";
            this.buttonStopEquipment.Size = new System.Drawing.Size(100, 40);
            this.buttonStopEquipment.TabIndex = 0;
            this.buttonStopEquipment.Text = "Stop";
            this.buttonStopEquipment.UseVisualStyleBackColor = true;
            this.buttonStopEquipment.Click += new System.EventHandler(this.buttonEquipment_Click);
            // 
            // panelX1
            // 
            this.panelX1.Controls.Add(this.groupBoxEquipment);
            this.panelX1.Controls.Add(this.groupBoxSelection);
            this.panelX1.Controls.Add(this.groupBoxModules);
            this.panelX1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelX1.Location = new System.Drawing.Point(1044, 3);
            this.panelX1.Name = "panelX1";
            this.panelX1.Size = new System.Drawing.Size(109, 762);
            this.panelX1.TabIndex = 15;
            // 
            // tabPageXImageSensor
            // 
            this.tabPageXImageSensor.Controls.Add(this.cameraView1);
            this.tabPageXImageSensor.Name = "tabPageXImageSensor";
            this.tabPageXImageSensor.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageXImageSensor.TabIndex = 4;
            this.tabPageXImageSensor.Text = "Image sensor";
            // 
            // cameraView1
            // 
            this.cameraView1.ColorFirst = System.Drawing.Color.Lavender;
            this.cameraView1.ColorSecond = System.Drawing.Color.Khaki;
            this.cameraView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cameraView1.EnabledMonitoring = false;
            this.cameraView1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cameraView1.Location = new System.Drawing.Point(3, 3);
            this.cameraView1.Name = "cameraView1";
            this.cameraView1.Padding = new System.Windows.Forms.Padding(3);
            this.cameraView1.Size = new System.Drawing.Size(1138, 717);
            this.cameraView1.TabIndex = 7;
            // 
            // GeneralSemiEquipmentMaintenanceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CaptionVisible = true;
            this.ClientSize = new System.Drawing.Size(1162, 840);
            this.Name = "GeneralSemiEquipmentMaintenanceForm";
            this.Text = "GeneralSemiEquipmentMaintenanceForm";
            this.tabPageGeneral.ResumeLayout(false);
            this.tabControlX1.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.groupBoxSelection.ResumeLayout(false);
            this.groupBoxModules.ResumeLayout(false);
            this.groupBoxEquipment.ResumeLayout(false);
            this.panelX1.ResumeLayout(false);
            this.tabPageXImageSensor.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        protected MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxSelection;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonDeselectAll;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonSelectAll;
        protected MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxModules;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonUserSelectModules;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonStopModules;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonInServiceModules;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonInitializeModules;
        protected MechaSys.SoftBricks.Hmi.Controls.GroupBoxX groupBoxEquipment;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonStopAfterWorkEquipment;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonStartEquipment;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonCleanUp;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonClearJobs;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonInitializeEquipment;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonStopEquipment;
        protected MechaSys.SoftBricks.Hmi.Controls.LayoutBox layoutBox;
        private MechaSys.SoftBricks.Hmi.Controls.PanelX panelX1;
        private MechaSys.SoftBricks.Hmi.Controls.TabPageX tabPageXImageSensor;
        private MechaSys.SoftBricks.Visions.ImageSensorView cameraView1;
    }
}