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
            this.rdoCoarse = new System.Windows.Forms.RadioButton();
            this.rdoFine = new System.Windows.Forms.RadioButton();
            this.rdoCurrent = new System.Windows.Forms.RadioButton();
            this.grpMoveMode = new System.Windows.Forms.GroupBox();
            this.moveModeLayout = new System.Windows.Forms.TableLayoutPanel();
            this.rdoContinuous = new System.Windows.Forms.RadioButton();
            this.rdoStep = new System.Windows.Forms.RadioButton();
            this.numStepDistance = new System.Windows.Forms.NumericUpDown();
            this.cboStepPreset = new System.Windows.Forms.ComboBox();
            this.lblStepUnit = new System.Windows.Forms.Label();
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
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66F));
            this.rootLayout.Controls.Add(this.grpSpeedMode, 0, 0);
            this.rootLayout.Controls.Add(this.grpMoveMode, 1, 0);
            this.rootLayout.Controls.Add(this.axisHost, 0, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 84F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 136F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(614, 316);
            this.rootLayout.TabIndex = 0;
            // 
            // grpSpeedMode
            // 
            this.grpSpeedMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(211)))), ((int)(((byte)(216)))));
            this.grpSpeedMode.Controls.Add(this.speedModeLayout);
            this.grpSpeedMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSpeedMode.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpSpeedMode.Location = new System.Drawing.Point(3, 4);
            this.grpSpeedMode.Margin = new System.Windows.Forms.Padding(0, 0, 4, 8);
            this.grpSpeedMode.Name = "grpSpeedMode";
            this.grpSpeedMode.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpSpeedMode.Size = new System.Drawing.Size(253, 95);
            this.grpSpeedMode.TabIndex = 0;
            this.grpSpeedMode.TabStop = false;
            this.grpSpeedMode.Text = "Speed Mode";
            // 
            // speedModeLayout
            // 
            this.speedModeLayout.ColumnCount = 2;
            this.speedModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.speedModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.speedModeLayout.Controls.Add(this.rdoCoarse, 0, 1);
            this.speedModeLayout.Controls.Add(this.rdoFine, 0, 0);
            this.speedModeLayout.Controls.Add(this.rdoCurrent, 1, 0);
            this.speedModeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.speedModeLayout.Location = new System.Drawing.Point(3, 24);
            this.speedModeLayout.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.speedModeLayout.Name = "speedModeLayout";
            this.speedModeLayout.RowCount = 2;
            this.speedModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 34.78261F));
            this.speedModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 31.15942F));
            this.speedModeLayout.Size = new System.Drawing.Size(247, 67);
            this.speedModeLayout.TabIndex = 0;
            // 
            // rdoCoarse
            // 
            this.rdoCoarse.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoCoarse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoCoarse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoCoarse.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rdoCoarse.Location = new System.Drawing.Point(3, 35);
            this.rdoCoarse.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoCoarse.MinimumSize = new System.Drawing.Size(52, 24);
            this.rdoCoarse.Name = "rdoCoarse";
            this.rdoCoarse.Size = new System.Drawing.Size(117, 32);
            this.rdoCoarse.TabIndex = 1;
            this.rdoCoarse.Text = "Coarse";
            this.rdoCoarse.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoCoarse.UseVisualStyleBackColor = false;
            this.rdoCoarse.CheckedChanged += new System.EventHandler(this.ModeRadio_CheckedChanged);
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
            this.rdoFine.MinimumSize = new System.Drawing.Size(52, 24);
            this.rdoFine.Name = "rdoFine";
            this.rdoFine.Size = new System.Drawing.Size(117, 35);
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
            this.rdoCurrent.Location = new System.Drawing.Point(126, 0);
            this.rdoCurrent.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoCurrent.MinimumSize = new System.Drawing.Size(52, 24);
            this.rdoCurrent.Name = "rdoCurrent";
            this.rdoCurrent.Size = new System.Drawing.Size(118, 35);
            this.rdoCurrent.TabIndex = 2;
            this.rdoCurrent.Text = "Current";
            this.rdoCurrent.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoCurrent.UseVisualStyleBackColor = false;
            this.rdoCurrent.CheckedChanged += new System.EventHandler(this.ModeRadio_CheckedChanged);
            // 
            // grpMoveMode
            // 
            this.grpMoveMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(211)))), ((int)(((byte)(216)))));
            this.grpMoveMode.Controls.Add(this.moveModeLayout);
            this.grpMoveMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMoveMode.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpMoveMode.Location = new System.Drawing.Point(266, 5);
            this.grpMoveMode.Margin = new System.Windows.Forms.Padding(4, 0, 0, 8);
            this.grpMoveMode.Name = "grpMoveMode";
            this.grpMoveMode.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.grpMoveMode.Size = new System.Drawing.Size(498, 95);
            this.grpMoveMode.TabIndex = 1;
            this.grpMoveMode.TabStop = false;
            this.grpMoveMode.Text = "Move Mode";
            // 
            // moveModeLayout
            // 
            this.moveModeLayout.ColumnCount = 4;
            this.moveModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.27881F));
            this.moveModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.27881F));
            this.moveModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15.24163F));
            this.moveModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34.20074F));
            this.moveModeLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.moveModeLayout.Controls.Add(this.rdoContinuous, 0, 0);
            this.moveModeLayout.Controls.Add(this.rdoStep, 2, 0);
            this.moveModeLayout.Controls.Add(this.numStepDistance, 0, 1);
            this.moveModeLayout.Controls.Add(this.lblStepUnit, 2, 1);
            this.moveModeLayout.Controls.Add(this.cboStepPreset, 3, 1);
            this.moveModeLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.moveModeLayout.Location = new System.Drawing.Point(3, 24);
            this.moveModeLayout.Margin = new System.Windows.Forms.Padding(0);
            this.moveModeLayout.Name = "moveModeLayout";
            this.moveModeLayout.RowCount = 2;
            this.moveModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.moveModeLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.moveModeLayout.Size = new System.Drawing.Size(492, 67);
            this.moveModeLayout.TabIndex = 0;
            // 
            // rdoContinuous
            // 
            this.rdoContinuous.Appearance = System.Windows.Forms.Appearance.Button;
            this.moveModeLayout.SetColumnSpan(this.rdoContinuous, 2);
            this.rdoContinuous.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoContinuous.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoContinuous.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rdoContinuous.Location = new System.Drawing.Point(3, 0);
            this.rdoContinuous.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoContinuous.Name = "rdoContinuous";
            this.rdoContinuous.Size = new System.Drawing.Size(242, 33);
            this.rdoContinuous.TabIndex = 0;
            this.rdoContinuous.Text = "Conti";
            this.rdoContinuous.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoContinuous.UseVisualStyleBackColor = false;
            this.rdoContinuous.CheckedChanged += new System.EventHandler(this.ModeRadio_CheckedChanged);
            // 
            // rdoStep
            // 
            this.rdoStep.Appearance = System.Windows.Forms.Appearance.Button;
            this.rdoStep.Checked = true;
            this.moveModeLayout.SetColumnSpan(this.rdoStep, 2);
            this.rdoStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rdoStep.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rdoStep.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rdoStep.Location = new System.Drawing.Point(251, 0);
            this.rdoStep.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.rdoStep.Name = "rdoStep";
            this.rdoStep.Size = new System.Drawing.Size(238, 33);
            this.rdoStep.TabIndex = 1;
            this.rdoStep.TabStop = true;
            this.rdoStep.Text = "Step";
            this.rdoStep.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.rdoStep.UseVisualStyleBackColor = false;
            this.rdoStep.CheckedChanged += new System.EventHandler(this.ModeRadio_CheckedChanged);
            // 
            // numStepDistance
            // 
            this.moveModeLayout.SetColumnSpan(this.numStepDistance, 2);
            this.numStepDistance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numStepDistance.Location = new System.Drawing.Point(3, 36);
            this.numStepDistance.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
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
            this.numStepDistance.Size = new System.Drawing.Size(242, 27);
            this.numStepDistance.TabIndex = 2;
            this.numStepDistance.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            //
            // cboStepPreset
            //
            this.cboStepPreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStepPreset.Font = new System.Drawing.Font("맑은 고딕", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.cboStepPreset.FormattingEnabled = true;
            this.cboStepPreset.Items.AddRange(new object[] {
            "1000",
            "100",
            "10",
            "1",
            "0"});
            this.cboStepPreset.Location = new System.Drawing.Point(325, 36);
            this.cboStepPreset.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.cboStepPreset.Name = "cboStepPreset";
            this.cboStepPreset.Size = new System.Drawing.Size(86, 25);
            this.cboStepPreset.TabIndex = 3;
            this.cboStepPreset.SelectedIndexChanged += new System.EventHandler(this.cboStepPreset_SelectedIndexChanged);
            this.cboStepPreset.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.cboStepPreset_MouseWheel);
            //
            // lblStepUnit
            //
            this.lblStepUnit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStepUnit.Font = new System.Drawing.Font("Consolas", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblStepUnit.Location = new System.Drawing.Point(251, 33);
            this.lblStepUnit.Name = "lblStepUnit";
            this.lblStepUnit.Size = new System.Drawing.Size(68, 34);
            this.lblStepUnit.TabIndex = 8;
            this.lblStepUnit.Text = "um";
            this.lblStepUnit.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // axisHost
            // 
            this.axisHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(211)))), ((int)(((byte)(216)))));
            this.axisHost.ColumnCount = 3;
            this.rootLayout.SetColumnSpan(this.axisHost, 2);
            this.axisHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.axisHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 132F));
            this.axisHost.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.axisHost.Controls.Add(this.axisButtonLayout, 1, 1);
            this.axisHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axisHost.Location = new System.Drawing.Point(3, 88);
            this.axisHost.Margin = new System.Windows.Forms.Padding(0);
            this.axisHost.Name = "axisHost";
            this.axisHost.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.axisHost.RowCount = 3;
            this.axisHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.axisHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.axisHost.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.axisHost.Size = new System.Drawing.Size(760, 170);
            this.axisHost.TabIndex = 2;
            // 
            // axisButtonLayout
            // 
            this.axisButtonLayout.ColumnCount = 1;
            this.axisButtonLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axisButtonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.axisButtonLayout.Location = new System.Drawing.Point(314, 39);
            this.axisButtonLayout.Margin = new System.Windows.Forms.Padding(0);
            this.axisButtonLayout.Name = "axisButtonLayout";
            this.axisButtonLayout.RowCount = 1;
            this.axisButtonLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.axisButtonLayout.Size = new System.Drawing.Size(132, 92);
            this.axisButtonLayout.TabIndex = 0;
            // 
            // JogAxisMoveControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.Controls.Add(this.rootLayout);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "JogAxisMoveControl";
            this.Size = new System.Drawing.Size(491, 253);
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
