using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    partial class JogMoveOptionsControl
    {
        private GroupBox _speedGroup;
        private RadioButton _rdoFine;
        private RadioButton _rdoCoarse;
        private GroupBox _modeGroup;
        private RadioButton _rdoContinuous;
        private RadioButton _rdoStep;
        private NumericUpDown _numStep;
        private FlowLayoutPanel _presetPanel;
        private Button _btnStep1;
        private Button _btnStep01;
        private Button _btnStep001;
        private Button _btnStep0001;
        private Button _btnStepZero;

        /// <summary>디자이너 리소스를 정리합니다.</summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._speedGroup = new System.Windows.Forms.GroupBox();
            this._rdoFine = new System.Windows.Forms.RadioButton();
            this._rdoCoarse = new System.Windows.Forms.RadioButton();
            this._modeGroup = new System.Windows.Forms.GroupBox();
            this._presetPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._btnStep1 = new System.Windows.Forms.Button();
            this._btnStep01 = new System.Windows.Forms.Button();
            this._btnStep001 = new System.Windows.Forms.Button();
            this._btnStep0001 = new System.Windows.Forms.Button();
            this._btnStepZero = new System.Windows.Forms.Button();
            this._numStep = new System.Windows.Forms.NumericUpDown();
            this._rdoContinuous = new System.Windows.Forms.RadioButton();
            this._rdoStep = new System.Windows.Forms.RadioButton();
            this._speedGroup.SuspendLayout();
            this._modeGroup.SuspendLayout();
            this._presetPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numStep)).BeginInit();
            this.SuspendLayout();
            // 
            // _speedGroup
            // 
            this._speedGroup.Controls.Add(this._rdoFine);
            this._speedGroup.Controls.Add(this._rdoCoarse);
            this._speedGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this._speedGroup.Location = new System.Drawing.Point(0, 0);
            this._speedGroup.Name = "_speedGroup";
            this._speedGroup.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this._speedGroup.Size = new System.Drawing.Size(300, 64);
            this._speedGroup.TabIndex = 0;
            this._speedGroup.TabStop = false;
            this._speedGroup.Text = "Move";
            // 
            // _rdoFine
            // 
            this._rdoFine.AutoSize = true;
            this._rdoFine.Checked = true;
            this._rdoFine.Location = new System.Drawing.Point(24, 28);
            this._rdoFine.Name = "_rdoFine";
            this._rdoFine.Size = new System.Drawing.Size(47, 16);
            this._rdoFine.TabIndex = 0;
            this._rdoFine.TabStop = true;
            this._rdoFine.Text = "Fine";
            this._rdoFine.UseVisualStyleBackColor = true;
            // 
            // _rdoCoarse
            // 
            this._rdoCoarse.AutoSize = true;
            this._rdoCoarse.Location = new System.Drawing.Point(98, 28);
            this._rdoCoarse.Name = "_rdoCoarse";
            this._rdoCoarse.Size = new System.Drawing.Size(61, 16);
            this._rdoCoarse.TabIndex = 1;
            this._rdoCoarse.Text = "Coarse";
            this._rdoCoarse.UseVisualStyleBackColor = true;
            // 
            // _modeGroup
            // 
            this._modeGroup.Controls.Add(this._presetPanel);
            this._modeGroup.Controls.Add(this._numStep);
            this._modeGroup.Controls.Add(this._rdoContinuous);
            this._modeGroup.Controls.Add(this._rdoStep);
            this._modeGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this._modeGroup.Location = new System.Drawing.Point(0, 64);
            this._modeGroup.Name = "_modeGroup";
            this._modeGroup.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this._modeGroup.Size = new System.Drawing.Size(300, 136);
            this._modeGroup.TabIndex = 1;
            this._modeGroup.TabStop = false;
            this._modeGroup.Text = "Move Mode";
            // 
            // _presetPanel
            // 
            this._presetPanel.Controls.Add(this._btnStep1);
            this._presetPanel.Controls.Add(this._btnStep01);
            this._presetPanel.Controls.Add(this._btnStep001);
            this._presetPanel.Controls.Add(this._btnStep0001);
            this._presetPanel.Controls.Add(this._btnStepZero);
            this._presetPanel.Location = new System.Drawing.Point(16, 88);
            this._presetPanel.Name = "_presetPanel";
            this._presetPanel.Size = new System.Drawing.Size(240, 36);
            this._presetPanel.TabIndex = 3;
            // 
            // _btnStep1
            // 
            this._btnStep1.Location = new System.Drawing.Point(2, 2);
            this._btnStep1.Margin = new System.Windows.Forms.Padding(2);
            this._btnStep1.Name = "_btnStep1";
            this._btnStep1.Size = new System.Drawing.Size(42, 30);
            this._btnStep1.TabIndex = 0;
            this._btnStep1.Text = "1";
            this._btnStep1.UseVisualStyleBackColor = true;
            // 
            // _btnStep01
            // 
            this._btnStep01.Location = new System.Drawing.Point(48, 2);
            this._btnStep01.Margin = new System.Windows.Forms.Padding(2);
            this._btnStep01.Name = "_btnStep01";
            this._btnStep01.Size = new System.Drawing.Size(42, 30);
            this._btnStep01.TabIndex = 1;
            this._btnStep01.Text = "0.1";
            this._btnStep01.UseVisualStyleBackColor = true;
            // 
            // _btnStep001
            // 
            this._btnStep001.Location = new System.Drawing.Point(94, 2);
            this._btnStep001.Margin = new System.Windows.Forms.Padding(2);
            this._btnStep001.Name = "_btnStep001";
            this._btnStep001.Size = new System.Drawing.Size(44, 30);
            this._btnStep001.TabIndex = 2;
            this._btnStep001.Text = "0.01";
            this._btnStep001.UseVisualStyleBackColor = true;
            // 
            // _btnStep0001
            // 
            this._btnStep0001.Location = new System.Drawing.Point(142, 2);
            this._btnStep0001.Margin = new System.Windows.Forms.Padding(2);
            this._btnStep0001.Name = "_btnStep0001";
            this._btnStep0001.Size = new System.Drawing.Size(50, 30);
            this._btnStep0001.TabIndex = 3;
            this._btnStep0001.Text = "0.001";
            this._btnStep0001.UseVisualStyleBackColor = true;
            // 
            // _btnStepZero
            // 
            this._btnStepZero.Location = new System.Drawing.Point(196, 2);
            this._btnStepZero.Margin = new System.Windows.Forms.Padding(2);
            this._btnStepZero.Name = "_btnStepZero";
            this._btnStepZero.Size = new System.Drawing.Size(40, 30);
            this._btnStepZero.TabIndex = 4;
            this._btnStepZero.Text = "0'";
            this._btnStepZero.UseVisualStyleBackColor = true;
            // 
            // _numStep
            // 
            this._numStep.DecimalPlaces = 3;
            this._numStep.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._numStep.Location = new System.Drawing.Point(16, 54);
            this._numStep.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._numStep.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this._numStep.Name = "_numStep";
            this._numStep.Size = new System.Drawing.Size(150, 21);
            this._numStep.TabIndex = 2;
            this._numStep.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // _rdoContinuous
            // 
            this._rdoContinuous.AutoSize = true;
            this._rdoContinuous.Location = new System.Drawing.Point(24, 26);
            this._rdoContinuous.Name = "_rdoContinuous";
            this._rdoContinuous.Size = new System.Drawing.Size(86, 16);
            this._rdoContinuous.TabIndex = 0;
            this._rdoContinuous.Text = "Continuous";
            this._rdoContinuous.UseVisualStyleBackColor = true;
            // 
            // _rdoStep
            // 
            this._rdoStep.AutoSize = true;
            this._rdoStep.Checked = true;
            this._rdoStep.Location = new System.Drawing.Point(142, 26);
            this._rdoStep.Name = "_rdoStep";
            this._rdoStep.Size = new System.Drawing.Size(48, 16);
            this._rdoStep.TabIndex = 1;
            this._rdoStep.TabStop = true;
            this._rdoStep.Text = "Step";
            this._rdoStep.UseVisualStyleBackColor = true;
            // 
            // JogMoveOptionsControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            this.Controls.Add(this._modeGroup);
            this.Controls.Add(this._speedGroup);
            this.Name = "JogMoveOptionsControl";
            this.Size = new System.Drawing.Size(300, 200);
            this._speedGroup.ResumeLayout(false);
            this._speedGroup.PerformLayout();
            this._modeGroup.ResumeLayout(false);
            this._modeGroup.PerformLayout();
            this._presetPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._numStep)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
