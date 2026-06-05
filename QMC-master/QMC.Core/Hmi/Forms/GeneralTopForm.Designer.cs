namespace QMC.Hmi.Forms
{
    partial class GeneralTopForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralTopForm));
            this.buttonExit = new MechaSys.SoftBricks.Hmi.Controls.GraphicButton();
            this.panelSecs = new System.Windows.Forms.Panel();
            this.gemStateViewer = new MechaSys.SoftBricks.Secs.Controls.GemStateViewer();
            this.buttonTerminalService = new MechaSys.SoftBricks.Hmi.Controls.GraphicButton();
            this.sbFlowLayoutPanel = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.buttonAlarm = new MechaSys.SoftBricks.Hmi.Controls.GraphicButton();
            this.buttonBuzzer = new MechaSys.SoftBricks.Hmi.Controls.GraphicButton();
            this.buttonModule = new MechaSys.SoftBricks.Hmi.Controls.GraphicButton();
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.lampBox = new MechaSys.SoftBricks.IO.Parts.TowerLampBox();
            this.timer = new MechaSys.SoftBricks.Hmi.Controls.TimerX(this.components);
            this.panelSecs.SuspendLayout();
            this.sbFlowLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonExit
            // 
            this.buttonExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonExit.AssignKey = System.Windows.Forms.Keys.None;
            this.buttonExit.BackColor = System.Drawing.Color.Transparent;
            this.buttonExit.BottomLabelVisible = true;
            this.buttonExit.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonExit.DisableImage")));
            this.buttonExit.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonExit.EnableImage")));
            this.buttonExit.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonExit.FlatAppearance.BorderSize = 0;
            this.buttonExit.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonExit.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonExit.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonExit.Image = ((System.Drawing.Image)(resources.GetObject("buttonExit.Image")));
            this.buttonExit.Location = new System.Drawing.Point(1112, 5);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Polygon = false;
            this.buttonExit.SecondaryEnableImage = null;
            this.buttonExit.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonExit.SelectedImage")));
            this.buttonExit.Size = new System.Drawing.Size(90, 80);
            this.buttonExit.TabIndex = 53;
            this.buttonExit.Text = "Exit";
            this.buttonExit.UseVisualStyleBackColor = true;
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // panelSecs
            // 
            this.panelSecs.BackColor = System.Drawing.SystemColors.Control;
            this.panelSecs.Controls.Add(this.gemStateViewer);
            this.panelSecs.Controls.Add(this.buttonTerminalService);
            this.panelSecs.Location = new System.Drawing.Point(775, 5);
            this.panelSecs.Name = "panelSecs";
            this.panelSecs.Size = new System.Drawing.Size(331, 90);
            this.panelSecs.TabIndex = 52;
            // 
            // gemStateViewer
            // 
            this.gemStateViewer.BackColor = System.Drawing.SystemColors.Control;
            this.gemStateViewer.CommStateVisible = true;
            this.gemStateViewer.ControlStateVisible = true;
            this.gemStateViewer.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gemStateViewer.GemService = null;
            this.gemStateViewer.Location = new System.Drawing.Point(3, 18);
            this.gemStateViewer.Name = "gemStateViewer";
            this.gemStateViewer.Padding = new System.Windows.Forms.Padding(3);
            this.gemStateViewer.Size = new System.Drawing.Size(221, 55);
            this.gemStateViewer.TabIndex = 32;
            // 
            // buttonTerminalService
            // 
            this.buttonTerminalService.AssignKey = System.Windows.Forms.Keys.None;
            this.buttonTerminalService.BackColor = System.Drawing.Color.Transparent;
            this.buttonTerminalService.BottomLabelVisible = true;
            this.buttonTerminalService.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonTerminalService.DisableImage")));
            this.buttonTerminalService.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonTerminalService.EnableImage")));
            this.buttonTerminalService.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonTerminalService.FlatAppearance.BorderSize = 0;
            this.buttonTerminalService.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonTerminalService.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonTerminalService.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonTerminalService.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonTerminalService.Image = ((System.Drawing.Image)(resources.GetObject("buttonTerminalService.Image")));
            this.buttonTerminalService.Location = new System.Drawing.Point(224, 0);
            this.buttonTerminalService.Name = "buttonTerminalService";
            this.buttonTerminalService.Polygon = false;
            this.buttonTerminalService.SecondaryEnableImage = null;
            this.buttonTerminalService.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonTerminalService.SelectedImage")));
            this.buttonTerminalService.Size = new System.Drawing.Size(104, 80);
            this.buttonTerminalService.TabIndex = 33;
            this.buttonTerminalService.Text = "Terminal Service";
            this.buttonTerminalService.UseVisualStyleBackColor = true;
            this.buttonTerminalService.Click += new System.EventHandler(this.buttonTerminalService_Click);
            // 
            // sbFlowLayoutPanel
            // 
            this.sbFlowLayoutPanel.BackColor = System.Drawing.SystemColors.Control;
            this.sbFlowLayoutPanel.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.sbFlowLayoutPanel.Controls.Add(this.buttonAlarm);
            this.sbFlowLayoutPanel.Controls.Add(this.buttonBuzzer);
            this.sbFlowLayoutPanel.Controls.Add(this.buttonModule);
            this.sbFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.sbFlowLayoutPanel.Gap = 110;
            this.sbFlowLayoutPanel.Location = new System.Drawing.Point(234, 5);
            this.sbFlowLayoutPanel.Name = "sbFlowLayoutPanel";
            this.sbFlowLayoutPanel.Size = new System.Drawing.Size(535, 80);
            this.sbFlowLayoutPanel.TabIndex = 51;
            // 
            // buttonAlarm
            // 
            this.buttonAlarm.AssignKey = System.Windows.Forms.Keys.None;
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
            this.buttonAlarm.Location = new System.Drawing.Point(10, 0);
            this.buttonAlarm.Name = "buttonAlarm";
            this.buttonAlarm.Polygon = false;
            this.buttonAlarm.SecondaryEnableImage = ((System.Drawing.Image)(resources.GetObject("buttonAlarm.SecondaryEnableImage")));
            this.buttonAlarm.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonAlarm.SelectedImage")));
            this.buttonAlarm.Size = new System.Drawing.Size(90, 80);
            this.buttonAlarm.TabIndex = 45;
            this.buttonAlarm.Text = "Alarm";
            this.buttonAlarm.UseVisualStyleBackColor = true;
            this.buttonAlarm.Click += new System.EventHandler(this.buttonAlarm_Click);
            // 
            // buttonBuzzer
            // 
            this.buttonBuzzer.AssignKey = System.Windows.Forms.Keys.None;
            this.buttonBuzzer.BackColor = System.Drawing.Color.Transparent;
            this.buttonBuzzer.BottomLabelVisible = true;
            this.buttonBuzzer.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonBuzzer.DisableImage")));
            this.buttonBuzzer.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonBuzzer.EnableImage")));
            this.buttonBuzzer.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonBuzzer.FlatAppearance.BorderSize = 0;
            this.buttonBuzzer.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonBuzzer.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonBuzzer.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonBuzzer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonBuzzer.Image = ((System.Drawing.Image)(resources.GetObject("buttonBuzzer.Image")));
            this.buttonBuzzer.Location = new System.Drawing.Point(120, 0);
            this.buttonBuzzer.Name = "buttonBuzzer";
            this.buttonBuzzer.Polygon = false;
            this.buttonBuzzer.SecondaryEnableImage = null;
            this.buttonBuzzer.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonBuzzer.SelectedImage")));
            this.buttonBuzzer.Size = new System.Drawing.Size(90, 80);
            this.buttonBuzzer.TabIndex = 44;
            this.buttonBuzzer.Text = "Buzzer";
            this.buttonBuzzer.UseVisualStyleBackColor = true;
            this.buttonBuzzer.Click += new System.EventHandler(this.buttonBuzzer_Click);
            // 
            // buttonModule
            // 
            this.buttonModule.AssignKey = System.Windows.Forms.Keys.None;
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
            this.buttonModule.Location = new System.Drawing.Point(230, 0);
            this.buttonModule.Name = "buttonModule";
            this.buttonModule.Polygon = false;
            this.buttonModule.SecondaryEnableImage = null;
            this.buttonModule.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonModule.SelectedImage")));
            this.buttonModule.Size = new System.Drawing.Size(90, 80);
            this.buttonModule.TabIndex = 43;
            this.buttonModule.Text = "Module";
            this.buttonModule.UseVisualStyleBackColor = true;
            this.buttonModule.Click += new System.EventHandler(this.buttonModule_Click);
            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pictureBoxLogo.BackColor = System.Drawing.SystemColors.Control;
            this.pictureBoxLogo.Location = new System.Drawing.Point(12, 5);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(216, 80);
            this.pictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBoxLogo.TabIndex = 50;
            this.pictureBoxLogo.TabStop = false;
            // 
            // lampBox
            // 
            this.lampBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lampBox.BackColor = System.Drawing.SystemColors.Control;
            this.lampBox.BlueOffImage = null;
            this.lampBox.BlueOnImage = null;
            this.lampBox.BottomImage = null;
            this.lampBox.Enabled = true;
            this.lampBox.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lampBox.GreenOffImage = null;
            this.lampBox.GreenOnImage = null;
            this.lampBox.Location = new System.Drawing.Point(1228, 12);
            this.lampBox.Name = "lampBox";
            this.lampBox.Padding = new System.Windows.Forms.Padding(3);
            this.lampBox.RedOffImage = null;
            this.lampBox.RedOnImage = null;
            this.lampBox.Size = new System.Drawing.Size(32, 3);
            this.lampBox.TabIndex = 54;
            this.lampBox.TopImage = null;
            this.lampBox.WhiteOffImage = null;
            this.lampBox.WhiteOnImage = null;
            this.lampBox.YellowOffImage = null;
            this.lampBox.YellowOnImage = null;
            // 
            // timer
            // 
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // GeneralTopForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1272, 90);
            this.Controls.Add(this.lampBox);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.panelSecs);
            this.Controls.Add(this.sbFlowLayoutPanel);
            this.Controls.Add(this.pictureBoxLogo);
            this.Name = "GeneralTopForm";
            this.Text = "GeneralTopForm";
            this.panelSecs.ResumeLayout(false);
            this.sbFlowLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.GraphicButton buttonExit;
        private System.Windows.Forms.Panel panelSecs;
        private MechaSys.SoftBricks.Secs.Controls.GemStateViewer gemStateViewer;
        private MechaSys.SoftBricks.Hmi.Controls.GraphicButton buttonTerminalService;
        protected MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX sbFlowLayoutPanel;
        private MechaSys.SoftBricks.Hmi.Controls.GraphicButton buttonAlarm;
        private MechaSys.SoftBricks.Hmi.Controls.GraphicButton buttonBuzzer;
        private MechaSys.SoftBricks.Hmi.Controls.GraphicButton buttonModule;
        protected System.Windows.Forms.PictureBox pictureBoxLogo;
        protected MechaSys.SoftBricks.IO.Parts.TowerLampBox lampBox;
        private MechaSys.SoftBricks.Hmi.Controls.TimerX timer;
    }
}