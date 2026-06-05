namespace QMC.Hmi.Forms
{
    partial class GeneralBottomForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneralBottomForm));
            this.panelMechanicalDryRun = new MechaSys.SoftBricks.Hmi.Controls.PanelX();
            this.labelControlJobCompletionCount = new MechaSys.SoftBricks.Hmi.Controls.LabelX();
            this.labelControlJobCompletionCountTitle = new MechaSys.SoftBricks.Hmi.Controls.LabelX();
            this.textBoxControlJobRepetition = new System.Windows.Forms.TextBox();
            this.labelControlJobRepetitionCount = new MechaSys.SoftBricks.Hmi.Controls.LabelX();
            this.listBoxAppliedOptions = new MechaSys.SoftBricks.Hmi.Controls.ListBoxX();
            this.buttonLogOn = new MechaSys.SoftBricks.Hmi.Controls.GraphicButton();
            this.clock = new MechaSys.SoftBricks.Hmi.Controls.Clock();
            this.flowLayoutPanel = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.buttonOperation = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonConfiguration = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonMaintenance = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonRecipe = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonDataLog = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.buttonUtility = new MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton();
            this.timer = new MechaSys.SoftBricks.Hmi.Controls.TimerX(this.components);
            this.panelMechanicalDryRun.SuspendLayout();
            this.flowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelMechanicalDryRun
            // 
            this.panelMechanicalDryRun.Controls.Add(this.labelControlJobCompletionCount);
            this.panelMechanicalDryRun.Controls.Add(this.labelControlJobCompletionCountTitle);
            this.panelMechanicalDryRun.Controls.Add(this.textBoxControlJobRepetition);
            this.panelMechanicalDryRun.Controls.Add(this.labelControlJobRepetitionCount);
            this.panelMechanicalDryRun.Controls.Add(this.listBoxAppliedOptions);
            this.panelMechanicalDryRun.Location = new System.Drawing.Point(725, 3);
            this.panelMechanicalDryRun.Name = "panelMechanicalDryRun";
            this.panelMechanicalDryRun.Size = new System.Drawing.Size(353, 82);
            this.panelMechanicalDryRun.TabIndex = 40;
            this.panelMechanicalDryRun.Visible = false;
            // 
            // labelControlJobCompletionCount
            // 
            this.labelControlJobCompletionCount.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.labelControlJobCompletionCount.Location = new System.Drawing.Point(269, 47);
            this.labelControlJobCompletionCount.Name = "labelControlJobCompletionCount";
            this.labelControlJobCompletionCount.Size = new System.Drawing.Size(80, 22);
            this.labelControlJobCompletionCount.TabIndex = 36;
            this.labelControlJobCompletionCount.Text = "0";
            this.labelControlJobCompletionCount.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelControlJobCompletionCountTitle
            // 
            this.labelControlJobCompletionCountTitle.AutoSize = true;
            this.labelControlJobCompletionCountTitle.Location = new System.Drawing.Point(158, 48);
            this.labelControlJobCompletionCountTitle.Name = "labelControlJobCompletionCountTitle";
            this.labelControlJobCompletionCountTitle.Size = new System.Drawing.Size(105, 14);
            this.labelControlJobCompletionCountTitle.TabIndex = 39;
            this.labelControlJobCompletionCountTitle.Text = "Completion Count";
            // 
            // textBoxControlJobRepetition
            // 
            this.textBoxControlJobRepetition.Location = new System.Drawing.Point(269, 14);
            this.textBoxControlJobRepetition.Name = "textBoxControlJobRepetition";
            this.textBoxControlJobRepetition.ReadOnly = true;
            this.textBoxControlJobRepetition.Size = new System.Drawing.Size(80, 22);
            this.textBoxControlJobRepetition.TabIndex = 38;
            this.textBoxControlJobRepetition.Text = "0";
            this.textBoxControlJobRepetition.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelControlJobRepetitionCount
            // 
            this.labelControlJobRepetitionCount.AutoSize = true;
            this.labelControlJobRepetitionCount.Location = new System.Drawing.Point(158, 17);
            this.labelControlJobRepetitionCount.Name = "labelControlJobRepetitionCount";
            this.labelControlJobRepetitionCount.Size = new System.Drawing.Size(100, 14);
            this.labelControlJobRepetitionCount.TabIndex = 37;
            this.labelControlJobRepetitionCount.Text = "Repetition Count";
            // 
            // listBoxAppliedOptions
            // 
            this.listBoxAppliedOptions.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxAppliedOptions.FormattingEnabled = true;
            this.listBoxAppliedOptions.ItemHeight = 20;
            this.listBoxAppliedOptions.Items.AddRange(new object[] {
            "Without Substrate",
            "Without Vision"});
            this.listBoxAppliedOptions.Location = new System.Drawing.Point(3, 11);
            this.listBoxAppliedOptions.Name = "listBoxAppliedOptions";
            this.listBoxAppliedOptions.Size = new System.Drawing.Size(149, 64);
            this.listBoxAppliedOptions.TabIndex = 35;
            // 
            // buttonLogOn
            // 
            this.buttonLogOn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonLogOn.AssignKey = System.Windows.Forms.Keys.None;
            this.buttonLogOn.BackColor = System.Drawing.Color.Transparent;
            this.buttonLogOn.BottomLabelVisible = true;
            this.buttonLogOn.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonLogOn.DisableImage")));
            this.buttonLogOn.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonLogOn.EnableImage")));
            this.buttonLogOn.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonLogOn.FlatAppearance.BorderSize = 0;
            this.buttonLogOn.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonLogOn.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonLogOn.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonLogOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonLogOn.Image = ((System.Drawing.Image)(resources.GetObject("buttonLogOn.Image")));
            this.buttonLogOn.Location = new System.Drawing.Point(1084, 5);
            this.buttonLogOn.Name = "buttonLogOn";
            this.buttonLogOn.Polygon = false;
            this.buttonLogOn.SecondaryEnableImage = null;
            this.buttonLogOn.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonLogOn.SelectedImage")));
            this.buttonLogOn.Size = new System.Drawing.Size(80, 80);
            this.buttonLogOn.TabIndex = 39;
            this.buttonLogOn.Text = "Log On";
            this.buttonLogOn.UseVisualStyleBackColor = true;
            this.buttonLogOn.Click += new System.EventHandler(this.buttonLogOn_Click);
            // 
            // clock
            // 
            this.clock.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clock.BackColor = System.Drawing.SystemColors.Control;
            this.clock.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.clock.Format = "yyyy-MM-dd HH:mm:ss";
            this.clock.Location = new System.Drawing.Point(1170, 15);
            this.clock.Name = "clock";
            this.clock.Padding = new System.Windows.Forms.Padding(3);
            this.clock.Size = new System.Drawing.Size(90, 58);
            this.clock.TabIndex = 38;
            this.clock.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.clock.UpdateInterval = 1000;
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.BackColor = System.Drawing.SystemColors.Control;
            this.flowLayoutPanel.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanel.Controls.Add(this.buttonOperation);
            this.flowLayoutPanel.Controls.Add(this.buttonConfiguration);
            this.flowLayoutPanel.Controls.Add(this.buttonMaintenance);
            this.flowLayoutPanel.Controls.Add(this.buttonRecipe);
            this.flowLayoutPanel.Controls.Add(this.buttonDataLog);
            this.flowLayoutPanel.Controls.Add(this.buttonUtility);
            this.flowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowLayoutPanel.Gap = 110;
            this.flowLayoutPanel.Location = new System.Drawing.Point(56, 5);
            this.flowLayoutPanel.Name = "flowLayoutPanel";
            this.flowLayoutPanel.Size = new System.Drawing.Size(663, 80);
            this.flowLayoutPanel.TabIndex = 37;
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
            this.buttonOperation.Location = new System.Drawing.Point(10, 0);
            this.buttonOperation.Name = "buttonOperation";
            this.buttonOperation.Polygon = false;
            this.buttonOperation.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonOperation.SelectedImage")));
            this.buttonOperation.Size = new System.Drawing.Size(90, 80);
            this.buttonOperation.TabIndex = 15;
            this.buttonOperation.TabStop = true;
            this.buttonOperation.Text = "Operation";
            this.buttonOperation.UseVisualStyleBackColor = true;
            this.buttonOperation.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonConfiguration
            // 
            this.buttonConfiguration.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonConfiguration.BackColor = System.Drawing.Color.Transparent;
            this.buttonConfiguration.BottomLabelVisible = true;
            this.buttonConfiguration.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonConfiguration.DisableImage")));
            this.buttonConfiguration.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonConfiguration.EnableImage")));
            this.buttonConfiguration.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonConfiguration.FlatAppearance.BorderSize = 0;
            this.buttonConfiguration.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonConfiguration.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonConfiguration.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonConfiguration.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonConfiguration.Image = ((System.Drawing.Image)(resources.GetObject("buttonConfiguration.Image")));
            this.buttonConfiguration.Location = new System.Drawing.Point(120, 0);
            this.buttonConfiguration.Name = "buttonConfiguration";
            this.buttonConfiguration.Polygon = false;
            this.buttonConfiguration.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonConfiguration.SelectedImage")));
            this.buttonConfiguration.Size = new System.Drawing.Size(90, 80);
            this.buttonConfiguration.TabIndex = 16;
            this.buttonConfiguration.TabStop = true;
            this.buttonConfiguration.Text = "Configuration";
            this.buttonConfiguration.UseVisualStyleBackColor = true;
            this.buttonConfiguration.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonMaintenance
            // 
            this.buttonMaintenance.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonMaintenance.BackColor = System.Drawing.Color.Transparent;
            this.buttonMaintenance.BottomLabelVisible = true;
            this.buttonMaintenance.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonMaintenance.DisableImage")));
            this.buttonMaintenance.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonMaintenance.EnableImage")));
            this.buttonMaintenance.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonMaintenance.FlatAppearance.BorderSize = 0;
            this.buttonMaintenance.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonMaintenance.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonMaintenance.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonMaintenance.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMaintenance.Image = ((System.Drawing.Image)(resources.GetObject("buttonMaintenance.Image")));
            this.buttonMaintenance.Location = new System.Drawing.Point(230, 0);
            this.buttonMaintenance.Name = "buttonMaintenance";
            this.buttonMaintenance.Polygon = false;
            this.buttonMaintenance.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonMaintenance.SelectedImage")));
            this.buttonMaintenance.Size = new System.Drawing.Size(90, 80);
            this.buttonMaintenance.TabIndex = 17;
            this.buttonMaintenance.TabStop = true;
            this.buttonMaintenance.Text = "Maintenance";
            this.buttonMaintenance.UseVisualStyleBackColor = true;
            this.buttonMaintenance.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonRecipe
            // 
            this.buttonRecipe.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonRecipe.BackColor = System.Drawing.Color.Transparent;
            this.buttonRecipe.BottomLabelVisible = true;
            this.buttonRecipe.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonRecipe.DisableImage")));
            this.buttonRecipe.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonRecipe.EnableImage")));
            this.buttonRecipe.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonRecipe.FlatAppearance.BorderSize = 0;
            this.buttonRecipe.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonRecipe.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonRecipe.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonRecipe.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonRecipe.Image = ((System.Drawing.Image)(resources.GetObject("buttonRecipe.Image")));
            this.buttonRecipe.Location = new System.Drawing.Point(340, 0);
            this.buttonRecipe.Name = "buttonRecipe";
            this.buttonRecipe.Polygon = false;
            this.buttonRecipe.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonRecipe.SelectedImage")));
            this.buttonRecipe.Size = new System.Drawing.Size(90, 80);
            this.buttonRecipe.TabIndex = 18;
            this.buttonRecipe.TabStop = true;
            this.buttonRecipe.Text = "Recipe";
            this.buttonRecipe.UseVisualStyleBackColor = true;
            this.buttonRecipe.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonDataLog
            // 
            this.buttonDataLog.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonDataLog.BackColor = System.Drawing.Color.Transparent;
            this.buttonDataLog.BottomLabelVisible = true;
            this.buttonDataLog.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonDataLog.DisableImage")));
            this.buttonDataLog.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonDataLog.EnableImage")));
            this.buttonDataLog.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonDataLog.FlatAppearance.BorderSize = 0;
            this.buttonDataLog.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonDataLog.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonDataLog.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonDataLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonDataLog.Image = ((System.Drawing.Image)(resources.GetObject("buttonDataLog.Image")));
            this.buttonDataLog.Location = new System.Drawing.Point(450, 0);
            this.buttonDataLog.Name = "buttonDataLog";
            this.buttonDataLog.Polygon = false;
            this.buttonDataLog.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonDataLog.SelectedImage")));
            this.buttonDataLog.Size = new System.Drawing.Size(90, 80);
            this.buttonDataLog.TabIndex = 19;
            this.buttonDataLog.TabStop = true;
            this.buttonDataLog.Text = "Data Log";
            this.buttonDataLog.UseVisualStyleBackColor = true;
            this.buttonDataLog.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // buttonUtility
            // 
            this.buttonUtility.Appearance = System.Windows.Forms.Appearance.Button;
            this.buttonUtility.BackColor = System.Drawing.Color.Transparent;
            this.buttonUtility.BottomLabelVisible = true;
            this.buttonUtility.DisableImage = ((System.Drawing.Image)(resources.GetObject("buttonUtility.DisableImage")));
            this.buttonUtility.EnableImage = ((System.Drawing.Image)(resources.GetObject("buttonUtility.EnableImage")));
            this.buttonUtility.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.buttonUtility.FlatAppearance.BorderSize = 0;
            this.buttonUtility.FlatAppearance.CheckedBackColor = System.Drawing.SystemColors.Control;
            this.buttonUtility.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            this.buttonUtility.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            this.buttonUtility.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonUtility.Image = ((System.Drawing.Image)(resources.GetObject("buttonUtility.Image")));
            this.buttonUtility.Location = new System.Drawing.Point(560, 0);
            this.buttonUtility.Name = "buttonUtility";
            this.buttonUtility.Polygon = false;
            this.buttonUtility.SelectedImage = ((System.Drawing.Image)(resources.GetObject("buttonUtility.SelectedImage")));
            this.buttonUtility.Size = new System.Drawing.Size(90, 80);
            this.buttonUtility.TabIndex = 20;
            this.buttonUtility.TabStop = true;
            this.buttonUtility.Text = "Utility";
            this.buttonUtility.UseVisualStyleBackColor = true;
            this.buttonUtility.CheckedChanged += new System.EventHandler(this.buttons_CheckedChanged);
            // 
            // timer
            // 
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // GeneralBottomForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1272, 90);
            this.Controls.Add(this.panelMechanicalDryRun);
            this.Controls.Add(this.buttonLogOn);
            this.Controls.Add(this.clock);
            this.Controls.Add(this.flowLayoutPanel);
            this.Name = "GeneralBottomForm";
            this.Text = "GeneralBottomForm";
            this.panelMechanicalDryRun.ResumeLayout(false);
            this.panelMechanicalDryRun.PerformLayout();
            this.flowLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private MechaSys.SoftBricks.Hmi.Controls.PanelX panelMechanicalDryRun;
        private MechaSys.SoftBricks.Hmi.Controls.LabelX labelControlJobCompletionCount;
        private MechaSys.SoftBricks.Hmi.Controls.LabelX labelControlJobCompletionCountTitle;
        private System.Windows.Forms.TextBox textBoxControlJobRepetition;
        private MechaSys.SoftBricks.Hmi.Controls.LabelX labelControlJobRepetitionCount;
        protected MechaSys.SoftBricks.Hmi.Controls.ListBoxX listBoxAppliedOptions;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicButton buttonLogOn;
        protected MechaSys.SoftBricks.Hmi.Controls.Clock clock;
        protected MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanel;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonOperation;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonConfiguration;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonMaintenance;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonRecipe;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonDataLog;
        protected MechaSys.SoftBricks.Hmi.Controls.GraphicRadioButton buttonUtility;
        private MechaSys.SoftBricks.Hmi.Controls.TimerX timer;
    }
}