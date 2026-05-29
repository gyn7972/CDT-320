using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    partial class AxisJogControl
    {
        private TableLayoutPanel _layout;
        private GroupBox _axisGroup;
        private ComboBox _axisSelector;
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
        private TableLayoutPanel _padLayout;
        private Button _btnTMinus;
        private Button _btnYPlus;
        private Button _btnTPlus;
        private Button _btnZPlus;
        private Button _btnXMinus;
        private Button _btnStop;
        private Button _btnXPlus;
        private Button _btnZMinus;
        private Button _btnYMinus;

        /// <summary>디자이너에서 사용하는 리소스를 정리합니다.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._layout = new System.Windows.Forms.TableLayoutPanel();
            this._axisGroup = new System.Windows.Forms.GroupBox();
            this._axisSelector = new System.Windows.Forms.ComboBox();
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
            this._padLayout = new System.Windows.Forms.TableLayoutPanel();
            this._btnTMinus = new System.Windows.Forms.Button();
            this._btnYPlus = new System.Windows.Forms.Button();
            this._btnTPlus = new System.Windows.Forms.Button();
            this._btnZPlus = new System.Windows.Forms.Button();
            this._btnXMinus = new System.Windows.Forms.Button();
            this._btnStop = new System.Windows.Forms.Button();
            this._btnXPlus = new System.Windows.Forms.Button();
            this._btnZMinus = new System.Windows.Forms.Button();
            this._btnYMinus = new System.Windows.Forms.Button();
            this._layout.SuspendLayout();
            this._axisGroup.SuspendLayout();
            this._speedGroup.SuspendLayout();
            this._modeGroup.SuspendLayout();
            this._presetPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numStep)).BeginInit();
            this._padLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // _layout
            // 
            this._layout.ColumnCount = 1;
            this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._layout.Controls.Add(this._axisGroup, 0, 0);
            this._layout.Controls.Add(this._speedGroup, 0, 1);
            this._layout.Controls.Add(this._modeGroup, 0, 2);
            this._layout.Controls.Add(this._padLayout, 0, 3);
            this._layout.Location = new System.Drawing.Point(0, 0);
            this._layout.Margin = new System.Windows.Forms.Padding(0);
            this._layout.Name = "_layout";
            this._layout.RowCount = 4;
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 155F));
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._layout.Size = new System.Drawing.Size(325, 556);
            this._layout.TabIndex = 0;
            // 
            // _axisGroup
            // 
            this._axisGroup.Controls.Add(this._axisSelector);
            this._axisGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this._axisGroup.Location = new System.Drawing.Point(5, 5);
            this._axisGroup.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this._axisGroup.Name = "_axisGroup";
            this._axisGroup.Padding = new System.Windows.Forms.Padding(16, 22, 16, 10);
            this._axisGroup.Size = new System.Drawing.Size(315, 62);
            this._axisGroup.TabIndex = 0;
            this._axisGroup.TabStop = false;
            this._axisGroup.Text = "Axis";
            // 
            // _axisSelector
            // 
            this._axisSelector.Dock = System.Windows.Forms.DockStyle.Fill;
            this._axisSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._axisSelector.FormattingEnabled = true;
            this._axisSelector.Location = new System.Drawing.Point(16, 40);
            this._axisSelector.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._axisSelector.Name = "_axisSelector";
            this._axisSelector.Size = new System.Drawing.Size(283, 23);
            this._axisSelector.TabIndex = 0;
            // 
            // _speedGroup
            // 
            this._speedGroup.Controls.Add(this._rdoFine);
            this._speedGroup.Controls.Add(this._rdoCoarse);
            this._speedGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this._speedGroup.Location = new System.Drawing.Point(5, 77);
            this._speedGroup.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this._speedGroup.Name = "_speedGroup";
            this._speedGroup.Padding = new System.Windows.Forms.Padding(16, 12, 16, 10);
            this._speedGroup.Size = new System.Drawing.Size(315, 62);
            this._speedGroup.TabIndex = 1;
            this._speedGroup.TabStop = false;
            this._speedGroup.Text = "Move";
            // 
            // _rdoFine
            // 
            this._rdoFine.AutoSize = true;
            this._rdoFine.Checked = true;
            this._rdoFine.Location = new System.Drawing.Point(21, 38);
            this._rdoFine.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._rdoFine.Name = "_rdoFine";
            this._rdoFine.Size = new System.Drawing.Size(55, 19);
            this._rdoFine.TabIndex = 0;
            this._rdoFine.TabStop = true;
            this._rdoFine.Text = "Fine";
            this._rdoFine.UseVisualStyleBackColor = true;
            // 
            // _rdoCoarse
            // 
            this._rdoCoarse.AutoSize = true;
            this._rdoCoarse.Location = new System.Drawing.Point(101, 38);
            this._rdoCoarse.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._rdoCoarse.Name = "_rdoCoarse";
            this._rdoCoarse.Size = new System.Drawing.Size(75, 19);
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
            this._modeGroup.Location = new System.Drawing.Point(5, 149);
            this._modeGroup.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this._modeGroup.Name = "_modeGroup";
            this._modeGroup.Padding = new System.Windows.Forms.Padding(16, 12, 16, 10);
            this._modeGroup.Size = new System.Drawing.Size(315, 145);
            this._modeGroup.TabIndex = 2;
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
            this._presetPanel.Location = new System.Drawing.Point(16, 95);
            this._presetPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._presetPanel.Name = "_presetPanel";
            this._presetPanel.Size = new System.Drawing.Size(283, 42);
            this._presetPanel.TabIndex = 3;
            // 
            // _btnStep1
            // 
            this._btnStep1.Location = new System.Drawing.Point(2, 2);
            this._btnStep1.Margin = new System.Windows.Forms.Padding(2);
            this._btnStep1.Name = "_btnStep1";
            this._btnStep1.Size = new System.Drawing.Size(51, 35);
            this._btnStep1.TabIndex = 0;
            this._btnStep1.Text = "1";
            this._btnStep1.UseVisualStyleBackColor = true;
            // 
            // _btnStep01
            // 
            this._btnStep01.Location = new System.Drawing.Point(57, 2);
            this._btnStep01.Margin = new System.Windows.Forms.Padding(2);
            this._btnStep01.Name = "_btnStep01";
            this._btnStep01.Size = new System.Drawing.Size(51, 35);
            this._btnStep01.TabIndex = 1;
            this._btnStep01.Text = "0.1";
            this._btnStep01.UseVisualStyleBackColor = true;
            // 
            // _btnStep001
            // 
            this._btnStep001.Location = new System.Drawing.Point(112, 2);
            this._btnStep001.Margin = new System.Windows.Forms.Padding(2);
            this._btnStep001.Name = "_btnStep001";
            this._btnStep001.Size = new System.Drawing.Size(51, 35);
            this._btnStep001.TabIndex = 2;
            this._btnStep001.Text = "0.01";
            this._btnStep001.UseVisualStyleBackColor = true;
            // 
            // _btnStep0001
            // 
            this._btnStep0001.Location = new System.Drawing.Point(167, 2);
            this._btnStep0001.Margin = new System.Windows.Forms.Padding(2);
            this._btnStep0001.Name = "_btnStep0001";
            this._btnStep0001.Size = new System.Drawing.Size(57, 35);
            this._btnStep0001.TabIndex = 3;
            this._btnStep0001.Text = "0.001";
            this._btnStep0001.UseVisualStyleBackColor = true;
            // 
            // _btnStepZero
            // 
            this._btnStepZero.Location = new System.Drawing.Point(228, 2);
            this._btnStepZero.Margin = new System.Windows.Forms.Padding(2);
            this._btnStepZero.Name = "_btnStepZero";
            this._btnStepZero.Size = new System.Drawing.Size(46, 35);
            this._btnStepZero.TabIndex = 4;
            this._btnStepZero.Text = "0\'";
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
            this._numStep.Location = new System.Drawing.Point(19, 62);
            this._numStep.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
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
            this._numStep.Size = new System.Drawing.Size(171, 25);
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
            this._rdoContinuous.Location = new System.Drawing.Point(21, 32);
            this._rdoContinuous.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._rdoContinuous.Name = "_rdoContinuous";
            this._rdoContinuous.Size = new System.Drawing.Size(103, 19);
            this._rdoContinuous.TabIndex = 0;
            this._rdoContinuous.Text = "Continuous";
            this._rdoContinuous.UseVisualStyleBackColor = true;
            // 
            // _rdoStep
            // 
            this._rdoStep.AutoSize = true;
            this._rdoStep.Checked = true;
            this._rdoStep.Location = new System.Drawing.Point(162, 32);
            this._rdoStep.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._rdoStep.Name = "_rdoStep";
            this._rdoStep.Size = new System.Drawing.Size(58, 19);
            this._rdoStep.TabIndex = 1;
            this._rdoStep.TabStop = true;
            this._rdoStep.Text = "Step";
            this._rdoStep.UseVisualStyleBackColor = true;
            // 
            // _padLayout
            // 
            this._padLayout.ColumnCount = 4;
            this._padLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._padLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._padLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._padLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this._padLayout.Controls.Add(this._btnTMinus, 0, 0);
            this._padLayout.Controls.Add(this._btnYPlus, 1, 0);
            this._padLayout.Controls.Add(this._btnTPlus, 2, 0);
            this._padLayout.Controls.Add(this._btnZPlus, 3, 0);
            this._padLayout.Controls.Add(this._btnXMinus, 0, 1);
            this._padLayout.Controls.Add(this._btnStop, 1, 1);
            this._padLayout.Controls.Add(this._btnXPlus, 2, 1);
            this._padLayout.Controls.Add(this._btnZMinus, 3, 1);
            this._padLayout.Controls.Add(this._btnYMinus, 1, 2);
            this._padLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._padLayout.Location = new System.Drawing.Point(5, 304);
            this._padLayout.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this._padLayout.Name = "_padLayout";
            this._padLayout.RowCount = 3;
            this._padLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._padLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._padLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this._padLayout.Size = new System.Drawing.Size(315, 247);
            this._padLayout.TabIndex = 3;
            // 
            // _btnTMinus
            // 
            this._btnTMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnTMinus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnTMinus.Location = new System.Drawing.Point(9, 10);
            this._btnTMinus.Margin = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this._btnTMinus.Name = "_btnTMinus";
            this._btnTMinus.Size = new System.Drawing.Size(60, 62);
            this._btnTMinus.TabIndex = 0;
            this._btnTMinus.Text = "T-";
            this._btnTMinus.UseVisualStyleBackColor = true;
            // 
            // _btnYPlus
            // 
            this._btnYPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnYPlus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnYPlus.Location = new System.Drawing.Point(87, 10);
            this._btnYPlus.Margin = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this._btnYPlus.Name = "_btnYPlus";
            this._btnYPlus.Size = new System.Drawing.Size(60, 62);
            this._btnYPlus.TabIndex = 1;
            this._btnYPlus.Text = "Y+";
            this._btnYPlus.UseVisualStyleBackColor = true;
            // 
            // _btnTPlus
            // 
            this._btnTPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnTPlus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnTPlus.Location = new System.Drawing.Point(165, 10);
            this._btnTPlus.Margin = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this._btnTPlus.Name = "_btnTPlus";
            this._btnTPlus.Size = new System.Drawing.Size(60, 62);
            this._btnTPlus.TabIndex = 2;
            this._btnTPlus.Text = "T+";
            this._btnTPlus.UseVisualStyleBackColor = true;
            // 
            // _btnZPlus
            // 
            this._btnZPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnZPlus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnZPlus.Location = new System.Drawing.Point(243, 10);
            this._btnZPlus.Margin = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this._btnZPlus.Name = "_btnZPlus";
            this._btnZPlus.Size = new System.Drawing.Size(63, 62);
            this._btnZPlus.TabIndex = 3;
            this._btnZPlus.Text = "Z+";
            this._btnZPlus.UseVisualStyleBackColor = true;
            // 
            // _btnXMinus
            // 
            this._btnXMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnXMinus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnXMinus.Location = new System.Drawing.Point(9, 92);
            this._btnXMinus.Margin = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this._btnXMinus.Name = "_btnXMinus";
            this._btnXMinus.Size = new System.Drawing.Size(60, 62);
            this._btnXMinus.TabIndex = 4;
            this._btnXMinus.Text = "X-";
            this._btnXMinus.UseVisualStyleBackColor = true;
            // 
            // _btnStop
            // 
            this._btnStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this._btnStop.Location = new System.Drawing.Point(87, 92);
            this._btnStop.Margin = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this._btnStop.Name = "_btnStop";
            this._btnStop.Size = new System.Drawing.Size(60, 62);
            this._btnStop.TabIndex = 5;
            this._btnStop.Text = "STOP";
            this._btnStop.UseVisualStyleBackColor = true;
            // 
            // _btnXPlus
            // 
            this._btnXPlus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnXPlus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnXPlus.Location = new System.Drawing.Point(165, 92);
            this._btnXPlus.Margin = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this._btnXPlus.Name = "_btnXPlus";
            this._btnXPlus.Size = new System.Drawing.Size(60, 62);
            this._btnXPlus.TabIndex = 6;
            this._btnXPlus.Text = "X+";
            this._btnXPlus.UseVisualStyleBackColor = true;
            // 
            // _btnZMinus
            // 
            this._btnZMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnZMinus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnZMinus.Location = new System.Drawing.Point(243, 92);
            this._btnZMinus.Margin = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this._btnZMinus.Name = "_btnZMinus";
            this._btnZMinus.Size = new System.Drawing.Size(63, 62);
            this._btnZMinus.TabIndex = 7;
            this._btnZMinus.Text = "Z-";
            this._btnZMinus.UseVisualStyleBackColor = true;
            // 
            // _btnYMinus
            // 
            this._btnYMinus.Dock = System.Windows.Forms.DockStyle.Fill;
            this._btnYMinus.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this._btnYMinus.Location = new System.Drawing.Point(87, 174);
            this._btnYMinus.Margin = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this._btnYMinus.Name = "_btnYMinus";
            this._btnYMinus.Size = new System.Drawing.Size(60, 63);
            this._btnYMinus.TabIndex = 9;
            this._btnYMinus.Text = "Y-";
            this._btnYMinus.UseVisualStyleBackColor = true;
            // 
            // AxisJogControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.Controls.Add(this._layout);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "AxisJogControl";
            this.Size = new System.Drawing.Size(325, 561);
            this._layout.ResumeLayout(false);
            this._axisGroup.ResumeLayout(false);
            this._speedGroup.ResumeLayout(false);
            this._speedGroup.PerformLayout();
            this._modeGroup.ResumeLayout(false);
            this._modeGroup.PerformLayout();
            this._presetPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._numStep)).EndInit();
            this._padLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
