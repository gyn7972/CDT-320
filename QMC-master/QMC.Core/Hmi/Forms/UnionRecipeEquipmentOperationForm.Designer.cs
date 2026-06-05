namespace QMC.Hmi.Forms
{
    partial class UnionRecipeEquipmentOperationForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            MechaSys.SoftBricks.Hmi.Controls.FlexGridComboBoxCell flexGridComboBoxCell1 = new MechaSys.SoftBricks.Hmi.Controls.FlexGridComboBoxCell();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelInfo = new System.Windows.Forms.Panel();
            this.flexGridInfo = new MechaSys.SoftBricks.Hmi.Controls.FlexGrid();
            this.flexGridInfoColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flexGridInfoColumnValue = new MechaSys.SoftBricks.Hmi.Controls.FlexGridComboBoxColumn();
            this.panelRecipe = new System.Windows.Forms.Panel();
            this.flexGridRecipe = new MechaSys.SoftBricks.Hmi.Controls.FlexGrid();
            this.ColumnNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.buttonDeselect = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonSelect = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.slotSelector1 = new MechaSys.SoftBricks.Materials.Controls.SlotSelector();
            this.buttonShowRecipe = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.flowLayoutPanelLoaders = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.flowLayoutPanelControl = new MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX();
            this.buttonStart = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonResume = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonPause = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonStop = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.slotStateView1 = new MechaSys.SoftBricks.Materials.Controls.SlotStateView();
            this.timer = new MechaSys.SoftBricks.Hmi.Controls.TimerX(this.components);
            this.buttonPageUp = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.buttonPageDown = new MechaSys.SoftBricks.Hmi.Controls.ButtonX();
            this.titleLabelRecipeIdentifier = new MechaSys.SoftBricks.Hmi.Controls.TitleLabel();
            this.slotStateView2 = new MechaSys.SoftBricks.Materials.Controls.SlotStateView();
            this.slotSelector2 = new MechaSys.SoftBricks.Materials.Controls.SlotSelector();
            this.panelInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.flexGridInfo)).BeginInit();
            this.panelRecipe.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.flexGridRecipe)).BeginInit();
            this.flowLayoutPanelControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelText
            // 
            this.labelText.Size = new System.Drawing.Size(1156, 23);
            this.labelText.Text = "Job Operation";
            // 
            // panelInfo
            // 
            this.panelInfo.Controls.Add(this.flexGridInfo);
            this.panelInfo.Location = new System.Drawing.Point(624, 698);
            this.panelInfo.Name = "panelInfo";
            this.panelInfo.Size = new System.Drawing.Size(407, 127);
            this.panelInfo.TabIndex = 49;
            // 
            // flexGridInfo
            // 
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.flexGridInfo.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.flexGridInfo.ColumnHeadersHeight = 30;
            this.flexGridInfo.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.flexGridInfoColumnName,
            this.flexGridInfoColumnValue});
            this.flexGridInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flexGridInfo.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.flexGridInfo.EnableTouch = false;
            this.flexGridInfo.GridColor = System.Drawing.SystemColors.Control;
            this.flexGridInfo.IsContentChanged = true;
            this.flexGridInfo.Location = new System.Drawing.Point(0, 0);
            this.flexGridInfo.Name = "flexGridInfo";
            this.flexGridInfo.RowTemplate.Height = 40;
            this.flexGridInfo.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.flexGridInfo.Size = new System.Drawing.Size(407, 127);
            this.flexGridInfo.TabIndex = 26;
            // 
            // flexGridInfoColumnName
            // 
            this.flexGridInfoColumnName.HeaderText = "Name";
            this.flexGridInfoColumnName.Name = "flexGridInfoColumnName";
            this.flexGridInfoColumnName.ReadOnly = true;
            this.flexGridInfoColumnName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.flexGridInfoColumnName.Width = 150;
            // 
            // flexGridInfoColumnValue
            // 
            this.flexGridInfoColumnValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            flexGridComboBoxCell1.DataSource = null;
            flexGridComboBoxCell1.DisplayStyleForCurrentCellOnly = true;
            flexGridComboBoxCell1.ErrorText = "";
            flexGridComboBoxCell1.Style = dataGridViewCellStyle2;
            flexGridComboBoxCell1.Value = null;
            flexGridComboBoxCell1.ValueType = typeof(object);
            this.flexGridInfoColumnValue.CellTemplate = flexGridComboBoxCell1;
            this.flexGridInfoColumnValue.DisplayStyleForCurrentCellOnly = true;
            this.flexGridInfoColumnValue.HeaderText = "Value";
            this.flexGridInfoColumnValue.Name = "flexGridInfoColumnValue";
            this.flexGridInfoColumnValue.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // panelRecipe
            // 
            this.panelRecipe.Controls.Add(this.flexGridRecipe);
            this.panelRecipe.Location = new System.Drawing.Point(624, 62);
            this.panelRecipe.Name = "panelRecipe";
            this.panelRecipe.Size = new System.Drawing.Size(407, 584);
            this.panelRecipe.TabIndex = 48;
            // 
            // flexGridRecipe
            // 
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.flexGridRecipe.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.flexGridRecipe.ColumnHeadersHeight = 30;
            this.flexGridRecipe.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnNo,
            this.ColumnName});
            this.flexGridRecipe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flexGridRecipe.EnableTouch = false;
            this.flexGridRecipe.GridColor = System.Drawing.SystemColors.Control;
            this.flexGridRecipe.IsContentChanged = true;
            this.flexGridRecipe.Location = new System.Drawing.Point(0, 0);
            this.flexGridRecipe.Name = "flexGridRecipe";
            this.flexGridRecipe.RowTemplate.Height = 40;
            this.flexGridRecipe.Size = new System.Drawing.Size(407, 584);
            this.flexGridRecipe.TabIndex = 7;
            this.flexGridRecipe.CurrentCellChanged += new System.EventHandler(this.flexGridRecipe_CurrentCellChanged);
            // 
            // ColumnNo
            // 
            this.ColumnNo.HeaderText = "No";
            this.ColumnNo.Name = "ColumnNo";
            this.ColumnNo.ReadOnly = true;
            this.ColumnNo.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.ColumnNo.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.ColumnNo.Width = 40;
            // 
            // ColumnName
            // 
            this.ColumnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnName.HeaderText = "Name";
            this.ColumnName.Name = "ColumnName";
            this.ColumnName.ReadOnly = true;
            this.ColumnName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // buttonDeselect
            // 
            this.buttonDeselect.Location = new System.Drawing.Point(730, 652);
            this.buttonDeselect.Name = "buttonDeselect";
            this.buttonDeselect.Size = new System.Drawing.Size(100, 40);
            this.buttonDeselect.TabIndex = 40;
            this.buttonDeselect.Text = "Nothing";
            this.buttonDeselect.UseVisualStyleBackColor = true;
            this.buttonDeselect.Click += new System.EventHandler(this.buttonSlotSelect_Click);
            // 
            // buttonSelect
            // 
            this.buttonSelect.Location = new System.Drawing.Point(624, 652);
            this.buttonSelect.Name = "buttonSelect";
            this.buttonSelect.Size = new System.Drawing.Size(100, 40);
            this.buttonSelect.TabIndex = 39;
            this.buttonSelect.Text = "All";
            this.buttonSelect.UseVisualStyleBackColor = true;
            this.buttonSelect.Click += new System.EventHandler(this.buttonSlotSelect_Click);
            // 
            // slotSelector1
            // 
            this.slotSelector1.BackColor = System.Drawing.SystemColors.Control;
            this.slotSelector1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotSelector1.Location = new System.Drawing.Point(12, 26);
            this.slotSelector1.Name = "slotSelector1";
            this.slotSelector1.SelectedText = null;
            this.slotSelector1.Size = new System.Drawing.Size(300, 802);
            this.slotSelector1.SlotFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotSelector1.SlotForeColor = System.Drawing.SystemColors.ControlText;
            this.slotSelector1.SlotNoBackColor = System.Drawing.SystemColors.Control;
            this.slotSelector1.SlotNoFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotSelector1.SlotNoForeColor = System.Drawing.SystemColors.ControlText;
            this.slotSelector1.SlotPadding = new System.Windows.Forms.Padding(1);
            this.slotSelector1.TabIndex = 34;
            // 
            // buttonShowRecipe
            // 
            this.buttonShowRecipe.Location = new System.Drawing.Point(934, 652);
            this.buttonShowRecipe.Name = "buttonShowRecipe";
            this.buttonShowRecipe.Size = new System.Drawing.Size(100, 40);
            this.buttonShowRecipe.TabIndex = 38;
            this.buttonShowRecipe.Text = "Show Recipe";
            this.buttonShowRecipe.UseVisualStyleBackColor = true;
            this.buttonShowRecipe.Click += new System.EventHandler(this.buttonShowRecipe_Click);
            // 
            // flowLayoutPanelLoaders
            // 
            this.flowLayoutPanelLoaders.BackColor = System.Drawing.SystemColors.Control;
            this.flowLayoutPanelLoaders.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanelLoaders.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelLoaders.Gap = 43;
            this.flowLayoutPanelLoaders.Location = new System.Drawing.Point(1037, 26);
            this.flowLayoutPanelLoaders.Name = "flowLayoutPanelLoaders";
            this.flowLayoutPanelLoaders.Size = new System.Drawing.Size(113, 430);
            this.flowLayoutPanelLoaders.TabIndex = 37;
            // 
            // flowLayoutPanelControl
            // 
            this.flowLayoutPanelControl.BackColor = System.Drawing.SystemColors.Control;
            this.flowLayoutPanelControl.ContentAlign = System.Drawing.ContentAlignment.TopCenter;
            this.flowLayoutPanelControl.Controls.Add(this.buttonStart);
            this.flowLayoutPanelControl.Controls.Add(this.buttonResume);
            this.flowLayoutPanelControl.Controls.Add(this.buttonPause);
            this.flowLayoutPanelControl.Controls.Add(this.buttonStop);
            this.flowLayoutPanelControl.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelControl.Gap = 43;
            this.flowLayoutPanelControl.Location = new System.Drawing.Point(1037, 655);
            this.flowLayoutPanelControl.Name = "flowLayoutPanelControl";
            this.flowLayoutPanelControl.Size = new System.Drawing.Size(113, 173);
            this.flowLayoutPanelControl.TabIndex = 36;
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(1, 0);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(110, 40);
            this.buttonStart.TabIndex = 0;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonOperation_Click);
            // 
            // buttonResume
            // 
            this.buttonResume.Location = new System.Drawing.Point(1, 43);
            this.buttonResume.Name = "buttonResume";
            this.buttonResume.Size = new System.Drawing.Size(110, 40);
            this.buttonResume.TabIndex = 1;
            this.buttonResume.Text = "Resume";
            this.buttonResume.UseVisualStyleBackColor = true;
            this.buttonResume.Click += new System.EventHandler(this.buttonOperation_Click);
            // 
            // buttonPause
            // 
            this.buttonPause.Location = new System.Drawing.Point(1, 86);
            this.buttonPause.Name = "buttonPause";
            this.buttonPause.Size = new System.Drawing.Size(110, 40);
            this.buttonPause.TabIndex = 2;
            this.buttonPause.Text = "Pause";
            this.buttonPause.UseVisualStyleBackColor = true;
            this.buttonPause.Click += new System.EventHandler(this.buttonOperation_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(1, 129);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(110, 40);
            this.buttonStop.TabIndex = 3;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonOperation_Click);
            // 
            // slotStateView1
            // 
            this.slotStateView1.BackColor = System.Drawing.SystemColors.Control;
            this.slotStateView1.Carrier = null;
            this.slotStateView1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView1.Location = new System.Drawing.Point(12, 26);
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
            this.slotStateView1.Size = new System.Drawing.Size(300, 802);
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
            this.slotStateView1.SlotPadding = new System.Windows.Forms.Padding(1);
            this.slotStateView1.SlotUndefined = System.Drawing.Color.Chocolate;
            this.slotStateView1.TabIndex = 35;
            // 
            // timer
            // 
            this.timer.Interval = 500;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // buttonPageUp
            // 
            this.buttonPageUp.Location = new System.Drawing.Point(1038, 478);
            this.buttonPageUp.Name = "buttonPageUp";
            this.buttonPageUp.Size = new System.Drawing.Size(110, 50);
            this.buttonPageUp.TabIndex = 50;
            this.buttonPageUp.Text = "Page Up";
            this.buttonPageUp.UseVisualStyleBackColor = true;
            this.buttonPageUp.Visible = false;
            this.buttonPageUp.Click += new System.EventHandler(this.buttonPages_Click);
            // 
            // buttonPageDown
            // 
            this.buttonPageDown.Location = new System.Drawing.Point(1037, 534);
            this.buttonPageDown.Name = "buttonPageDown";
            this.buttonPageDown.Size = new System.Drawing.Size(110, 50);
            this.buttonPageDown.TabIndex = 51;
            this.buttonPageDown.Text = "Page Down";
            this.buttonPageDown.UseVisualStyleBackColor = true;
            this.buttonPageDown.Visible = false;
            this.buttonPageDown.Click += new System.EventHandler(this.buttonPages_Click);
            // 
            // titleLabelRecipeIdentifier
            // 
            this.titleLabelRecipeIdentifier.Location = new System.Drawing.Point(624, 26);
            this.titleLabelRecipeIdentifier.Name = "titleLabelRecipeIdentifier";
            this.titleLabelRecipeIdentifier.Size = new System.Drawing.Size(407, 30);
            this.titleLabelRecipeIdentifier.TabIndex = 52;
            this.titleLabelRecipeIdentifier.Text = null;
            this.titleLabelRecipeIdentifier.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.titleLabelRecipeIdentifier.TextBackColor = System.Drawing.SystemColors.Window;
            this.titleLabelRecipeIdentifier.TextFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.titleLabelRecipeIdentifier.TextForeColor = System.Drawing.SystemColors.WindowText;
            this.titleLabelRecipeIdentifier.Title = "Identifier";
            this.titleLabelRecipeIdentifier.TitleAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.titleLabelRecipeIdentifier.TitleBackColor = System.Drawing.SystemColors.Control;
            this.titleLabelRecipeIdentifier.TitleFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.titleLabelRecipeIdentifier.TitleForeColor = System.Drawing.SystemColors.ControlText;
            this.titleLabelRecipeIdentifier.TitleWidth = 75;
            this.titleLabelRecipeIdentifier.UnitSymbol = null;
            this.titleLabelRecipeIdentifier.ValueFormat = "";
            this.titleLabelRecipeIdentifier.ValueWidth = 330;
            // 
            // slotStateView2
            // 
            this.slotStateView2.BackColor = System.Drawing.SystemColors.Control;
            this.slotStateView2.Carrier = null;
            this.slotStateView2.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotStateView2.Location = new System.Drawing.Point(318, 26);
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
            this.slotStateView2.Size = new System.Drawing.Size(300, 802);
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
            this.slotStateView2.SlotPadding = new System.Windows.Forms.Padding(1);
            this.slotStateView2.SlotUndefined = System.Drawing.Color.Chocolate;
            this.slotStateView2.TabIndex = 53;
            // 
            // slotSelector2
            // 
            this.slotSelector2.BackColor = System.Drawing.SystemColors.Control;
            this.slotSelector2.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotSelector2.Location = new System.Drawing.Point(318, 26);
            this.slotSelector2.Name = "slotSelector2";
            this.slotSelector2.SelectedText = null;
            this.slotSelector2.Size = new System.Drawing.Size(300, 802);
            this.slotSelector2.SlotFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotSelector2.SlotForeColor = System.Drawing.SystemColors.ControlText;
            this.slotSelector2.SlotNoBackColor = System.Drawing.SystemColors.Control;
            this.slotSelector2.SlotNoFont = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.slotSelector2.SlotNoForeColor = System.Drawing.SystemColors.ControlText;
            this.slotSelector2.SlotPadding = new System.Windows.Forms.Padding(1);
            this.slotSelector2.TabIndex = 35;
            // 
            // UnionRecipeEquipmentOperationForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Caption = "Job Operation";
            this.CaptionVisible = true;
            this.ClientSize = new System.Drawing.Size(1156, 834);
            this.Controls.Add(this.slotSelector2);
            this.Controls.Add(this.slotStateView2);
            this.Controls.Add(this.titleLabelRecipeIdentifier);
            this.Controls.Add(this.buttonPageDown);
            this.Controls.Add(this.buttonPageUp);
            this.Controls.Add(this.panelInfo);
            this.Controls.Add(this.panelRecipe);
            this.Controls.Add(this.buttonDeselect);
            this.Controls.Add(this.buttonSelect);
            this.Controls.Add(this.slotSelector1);
            this.Controls.Add(this.buttonShowRecipe);
            this.Controls.Add(this.flowLayoutPanelLoaders);
            this.Controls.Add(this.flowLayoutPanelControl);
            this.Controls.Add(this.slotStateView1);
            this.Name = "UnionRecipeEquipmentOperationForm";
            this.Controls.SetChildIndex(this.labelText, 0);
            this.Controls.SetChildIndex(this.slotStateView1, 0);
            this.Controls.SetChildIndex(this.flowLayoutPanelControl, 0);
            this.Controls.SetChildIndex(this.flowLayoutPanelLoaders, 0);
            this.Controls.SetChildIndex(this.buttonShowRecipe, 0);
            this.Controls.SetChildIndex(this.slotSelector1, 0);
            this.Controls.SetChildIndex(this.buttonSelect, 0);
            this.Controls.SetChildIndex(this.buttonDeselect, 0);
            this.Controls.SetChildIndex(this.panelRecipe, 0);
            this.Controls.SetChildIndex(this.panelInfo, 0);
            this.Controls.SetChildIndex(this.buttonPageUp, 0);
            this.Controls.SetChildIndex(this.buttonPageDown, 0);
            this.Controls.SetChildIndex(this.titleLabelRecipeIdentifier, 0);
            this.Controls.SetChildIndex(this.slotStateView2, 0);
            this.Controls.SetChildIndex(this.slotSelector2, 0);
            this.panelInfo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.flexGridInfo)).EndInit();
            this.panelRecipe.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.flexGridRecipe)).EndInit();
            this.flowLayoutPanelControl.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.Panel panelInfo;
        protected MechaSys.SoftBricks.Hmi.Controls.FlexGrid flexGridInfo;
        private System.Windows.Forms.DataGridViewTextBoxColumn flexGridInfoColumnName;
        private MechaSys.SoftBricks.Hmi.Controls.FlexGridComboBoxColumn flexGridInfoColumnValue;
        protected System.Windows.Forms.Panel panelRecipe;
        protected MechaSys.SoftBricks.Hmi.Controls.FlexGrid flexGridRecipe;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonDeselect;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonSelect;
        protected MechaSys.SoftBricks.Materials.Controls.SlotSelector slotSelector1;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonShowRecipe;
        protected MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelLoaders;
        protected MechaSys.SoftBricks.Hmi.Controls.FlowLayoutPanelX flowLayoutPanelControl;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonStart;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonResume;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonPause;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonStop;
        protected MechaSys.SoftBricks.Materials.Controls.SlotStateView slotStateView1;
        protected MechaSys.SoftBricks.Hmi.Controls.TimerX timer;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonPageUp;
        protected MechaSys.SoftBricks.Hmi.Controls.ButtonX buttonPageDown;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnName;
        protected MechaSys.SoftBricks.Materials.Controls.SlotStateView slotStateView2;
        protected MechaSys.SoftBricks.Materials.Controls.SlotSelector slotSelector2;
        protected MechaSys.SoftBricks.Hmi.Controls.TitleLabel titleLabelRecipeIdentifier;
    }
}