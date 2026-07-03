namespace QMC.CDT_320.Ui.Controls
{
    partial class JogAxisMoveControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.GroupBox grpSpeedMode;
        private System.Windows.Forms.TableLayoutPanel speedModeLayout;
        private System.Windows.Forms.RadioButton rdoFine;
        private System.Windows.Forms.RadioButton rdoCoarse;
        private System.Windows.Forms.RadioButton rdoCurrent;
        private System.Windows.Forms.GroupBox grpMoveMode;
        private System.Windows.Forms.TableLayoutPanel moveModeLayout;
        private System.Windows.Forms.RadioButton rdoContinuous;
        private System.Windows.Forms.RadioButton rdoStep;
        private System.Windows.Forms.NumericUpDown numStepDistance;
        private System.Windows.Forms.Label lblStepUnit;
        private System.Windows.Forms.ComboBox cboStepPreset;
        private System.Windows.Forms.TableLayoutPanel axisHost;
        private System.Windows.Forms.TableLayoutPanel axisButtonLayout;

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && (components != null))
                    components.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpSpeedMode = new System.Windows.Forms.GroupBox();
            this.speedModeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rdoFine = new System.Windows.Forms.RadioButton();
            this.rdoCurrent = new System.Windows.Forms.RadioButton();
            this.rdoCoarse = new System.Windows.Forms.RadioButton();
            this.grpMoveMode = new System.Windows.Forms.GroupBox();
            this.moveModeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rdoContinuous = new System.Windows.Forms.RadioButton();
            this.rdoStep = new System.Windows.Forms.RadioButton();
            this.numStepDistance = new System.Windows.Forms.NumericUpDown();
            this.lblStepUnit = new System.Windows.Forms.Label();
            this.cboStepPreset = new System.Windows.Forms.ComboBox();
            this.axisHost = new System.Windows.Forms.TableLayoutPanel();
            this.axisButtonLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rootLayout.SuspendLayout();
            this.grpSpeedMode.SuspendLayout();
            this.speedModeLayout.SuspendLayout();
            this.grpMoveMode.SuspendLayout();
            this.moveModeLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numStepDistance)).BeginInit();
            this.axisHost.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.rootLayout.Controls.Add(this.grpSpeedMode, 0, 0);
            this.rootLayout.Controls.Add(this.grpMoveMode, 1, 0);
            this.rootLayout.Controls.Add(this.axisHost, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(3);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 84F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 255F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(389, 353);
            this.rootLayout.TabIndex = 0;
            // 
            // grpSpeedMode
            // 
            this.grpSpeedMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.grpSpeedMode.Controls.Add(this.speedModeLayout);
            this.grpSpeedMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSpeedMode.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpSpeedMode.Location = new System.Drawing.Point(3, 3);
            this.grpSpeedMode.Margin = new System.Windows.Forms.Padding(0, 0, 4, 6);
            this.grpSpeedMode.Name = "grpSpeedMode";
            this.grpSpeedMode.Size = new System.Drawing.Size(149, 78);
            this.grpSpeedMode.TabIndex = 0;
            this.grpSpeedMode.TabStop = false;
            this.grpSpeedMode.Text = "Speed Mode";
            // 
            // speedModeLayout
            // 
            this.speedModeLayout.ColumnCount = 1;
            this.speedModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.speedModeLayout.Controls.Add(this.rdoFine, 0, 0);
            this.speedModeLayout.Controls.Add(this.rdoCurrent, 0, 1);
            this.speedModeLayout.Controls.Add(this.rdoCoarse, 0, 2);
            this.speedModeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedModeLayout.Location = new System.Drawing.Point(3, 19);
            this.speedModeLayout.Name = "speedModeLayout";
            this.speedModeLayout.RowCount = 3;
            this.speedModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.speedModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            this.speedModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            this.speedModeLayout.Size = new System.Drawing.Size(143, 56);
            this.speedModeLayout.TabIndex = 0;
            // 
            // rdoFine
            // 
            this.rdoFine.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoFine.Checked = true;
            this.rdoFine.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoFine.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoFine.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rdoFine.Location = new System.Drawing.Point(3, 0);
            this.rdoFine.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoFine.MinimumSize = new System.Drawing.Size(46, 19);
            this.rdoFine.Name = "rdoFine";
            this.rdoFine.Size = new System.Drawing.Size(137, 19);
            this.rdoFine.TabIndex = 0;
            this.rdoFine.TabStop = true;
            this.rdoFine.Text = "Fine";
            this.rdoFine.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoFine.UseVisualStyleBackColor = false;
            this.rdoFine.CheckedChanged += new System.EventHandler(this.ModeRadio_CheckedChanged);
            // 
            // rdoCurrent
            // 
            this.rdoCurrent.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoCurrent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoCurrent.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rdoCurrent.Location = new System.Drawing.Point(3, 18);
            this.rdoCurrent.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoCurrent.MinimumSize = new System.Drawing.Size(46, 19);
            this.rdoCurrent.Name = "rdoCurrent";
            this.rdoCurrent.Size = new System.Drawing.Size(137, 19);
            this.rdoCurrent.TabIndex = 2;
            this.rdoCurrent.Text = "Current";
            this.rdoCurrent.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoCurrent.UseVisualStyleBackColor = false;
            this.rdoCurrent.CheckedChanged += new System.EventHandler(this.ModeRadio_CheckedChanged);
            // 
            // rdoCoarse
            // 
            this.rdoCoarse.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoCoarse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoCoarse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoCoarse.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rdoCoarse.Location = new System.Drawing.Point(3, 36);
            this.rdoCoarse.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoCoarse.MinimumSize = new System.Drawing.Size(46, 19);
            this.rdoCoarse.Name = "rdoCoarse";
            this.rdoCoarse.Size = new System.Drawing.Size(137, 20);
            this.rdoCoarse.TabIndex = 1;
            this.rdoCoarse.Text = "Coarse";
            this.rdoCoarse.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoCoarse.UseVisualStyleBackColor = false;
            this.rdoCoarse.CheckedChanged += new System.EventHandler(this.ModeRadio_CheckedChanged);
            // 
            // grpMoveMode
            // 
            this.grpMoveMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.grpMoveMode.Controls.Add(this.moveModeLayout);
            this.grpMoveMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMoveMode.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpMoveMode.Location = new System.Drawing.Point(160, 3);
            this.grpMoveMode.Margin = new System.Windows.Forms.Padding(4, 0, 0, 6);
            this.grpMoveMode.Name = "grpMoveMode";
            this.grpMoveMode.Size = new System.Drawing.Size(226, 78);
            this.grpMoveMode.TabIndex = 1;
            this.grpMoveMode.TabStop = false;
            this.grpMoveMode.Text = "Move Mode";
            // 
            // moveModeLayout
            // 
            this.moveModeLayout.ColumnCount = 3;
            this.moveModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.90909F));
            this.moveModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.63636F));
            this.moveModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.moveModeLayout.Controls.Add(this.rdoContinuous, 0, 0);
            this.moveModeLayout.Controls.Add(this.rdoStep, 0, 1);
            this.moveModeLayout.Controls.Add(this.numStepDistance, 1, 0);
            this.moveModeLayout.Controls.Add(this.lblStepUnit, 2, 0);
            this.moveModeLayout.Controls.Add(this.cboStepPreset, 1, 1);
            this.moveModeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.moveModeLayout.Location = new System.Drawing.Point(3, 19);
            this.moveModeLayout.Margin = new System.Windows.Forms.Padding(0);
            this.moveModeLayout.Name = "moveModeLayout";
            this.moveModeLayout.RowCount = 2;
            this.moveModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.moveModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.moveModeLayout.Size = new System.Drawing.Size(220, 56);
            this.moveModeLayout.TabIndex = 0;
            // 
            // rdoContinuous
            // 
            this.rdoContinuous.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoContinuous.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoContinuous.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoContinuous.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rdoContinuous.Location = new System.Drawing.Point(3, 0);
            this.rdoContinuous.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoContinuous.Name = "rdoContinuous";
            this.rdoContinuous.Size = new System.Drawing.Size(106, 28);
            this.rdoContinuous.TabIndex = 0;
            this.rdoContinuous.Text = "Continuous";
            this.rdoContinuous.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoContinuous.UseVisualStyleBackColor = false;
            this.rdoContinuous.CheckedChanged += new System.EventHandler(this.ModeRadio_CheckedChanged);
            // 
            // rdoStep
            // 
            this.rdoStep.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoStep.Checked = true;
            this.rdoStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoStep.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoStep.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rdoStep.Location = new System.Drawing.Point(3, 28);
            this.rdoStep.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoStep.Name = "rdoStep";
            this.rdoStep.Size = new System.Drawing.Size(106, 28);
            this.rdoStep.TabIndex = 1;
            this.rdoStep.TabStop = true;
            this.rdoStep.Text = "Step";
            this.rdoStep.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoStep.UseVisualStyleBackColor = false;
            this.rdoStep.CheckedChanged += new System.EventHandler(this.ModeRadio_CheckedChanged);
            // 
            // numStepDistance
            // 
            this.numStepDistance.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.numStepDistance.Location = new System.Drawing.Point(115, 5);
            this.numStepDistance.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this.numStepDistance.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.numStepDistance.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numStepDistance.Name = "numStepDistance";
            this.numStepDistance.Size = new System.Drawing.Size(68, 23);
            this.numStepDistance.TabIndex = 2;
            this.numStepDistance.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // lblStepUnit
            // 
            this.lblStepUnit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStepUnit.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblStepUnit.Location = new System.Drawing.Point(189, 0);
            this.lblStepUnit.Name = "lblStepUnit";
            this.lblStepUnit.Size = new System.Drawing.Size(28, 28);
            this.lblStepUnit.TabIndex = 8;
            this.lblStepUnit.Text = "um";
            this.lblStepUnit.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // cboStepPreset
            // 
            this.cboStepPreset.Dock = System.Windows.Forms.DockStyle.Top;
            this.cboStepPreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStepPreset.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.cboStepPreset.FormattingEnabled = true;
            this.cboStepPreset.Items.AddRange(new object[] {
            "1000",
            "100",
            "10",
            "1",
            "0"});
            this.cboStepPreset.Location = new System.Drawing.Point(115, 30);
            this.cboStepPreset.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this.cboStepPreset.Name = "cboStepPreset";
            this.cboStepPreset.Size = new System.Drawing.Size(68, 21);
            this.cboStepPreset.TabIndex = 3;
            this.cboStepPreset.SelectedIndexChanged += new System.EventHandler(this.cboStepPreset_SelectedIndexChanged);
            this.cboStepPreset.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.cboStepPreset_MouseWheel);
            // 
            // axisHost
            // 
            this.axisHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(211)))), ((int)(((byte)(216)))));
            this.axisHost.ColumnCount = 3;
            this.rootLayout.SetColumnSpan(this.axisHost, 2);
            this.axisHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.axisHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 135F));
            this.axisHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.axisHost.Controls.Add(this.axisButtonLayout, 1, 1);
            this.axisHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axisHost.Location = new System.Drawing.Point(3, 87);
            this.axisHost.Margin = new System.Windows.Forms.Padding(0);
            this.axisHost.Name = "axisHost";
            this.axisHost.Padding = new System.Windows.Forms.Padding(4);
            this.axisHost.RowCount = 3;
            this.axisHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.axisHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 74F));
            this.axisHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.axisHost.Size = new System.Drawing.Size(383, 255);
            this.axisHost.TabIndex = 2;
            // 
            // axisButtonLayout
            // 
            this.axisButtonLayout.ColumnCount = 1;
            this.axisButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axisButtonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axisButtonLayout.Location = new System.Drawing.Point(124, 90);
            this.axisButtonLayout.Margin = new System.Windows.Forms.Padding(0);
            this.axisButtonLayout.Name = "axisButtonLayout";
            this.axisButtonLayout.RowCount = 1;
            this.axisButtonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axisButtonLayout.Size = new System.Drawing.Size(135, 74);
            this.axisButtonLayout.TabIndex = 0;
            // 
            // JogAxisMoveControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.Controls.Add(this.rootLayout);
            this.Name = "JogAxisMoveControl";
            this.Size = new System.Drawing.Size(389, 353);
            this.rootLayout.ResumeLayout(false);
            this.grpSpeedMode.ResumeLayout(false);
            this.speedModeLayout.ResumeLayout(false);
            this.grpMoveMode.ResumeLayout(false);
            this.moveModeLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numStepDistance)).EndInit();
            this.axisHost.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
