using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    partial class VisionCameraCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private Label lblGuide;
        private DataGridView gridMeasurements;
        private DataGridViewTextBoxColumn colItem;
        private DataGridViewTextBoxColumn colPixel;
        private DataGridViewTextBoxColumn colMm;
        private DataGridViewTextBoxColumn colAxis;
        private DataGridViewTextBoxColumn colScore;
        private Label lblOffsets;
        private Label lblStatus;
        private FlowLayoutPanel buttonPanel;
        private Button btnCheck;
        private Button btnFindBottom;
        private Button btnFindInput;
        private Button btnFindOutput;
        private Button btnRunAll;
        private Button btnCalculateSave;
        private Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblGuide = new System.Windows.Forms.Label();
            this.gridMeasurements = new System.Windows.Forms.DataGridView();
            this.colItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPixel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMm = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAxis = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblOffsets = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCheck = new System.Windows.Forms.Button();
            this.btnFindBottom = new System.Windows.Forms.Button();
            this.btnFindInput = new System.Windows.Forms.Button();
            this.btnFindOutput = new System.Windows.Forms.Button();
            this.btnRunAll = new System.Windows.Forms.Button();
            this.btnCalculateSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridMeasurements)).BeginInit();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblTitle, 0, 0);
            this.rootLayout.Controls.Add(this.lblGuide, 0, 1);
            this.rootLayout.Controls.Add(this.gridMeasurements, 0, 2);
            this.rootLayout.Controls.Add(this.lblOffsets, 0, 3);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 4);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 5);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 6;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 62F));
            this.rootLayout.Size = new System.Drawing.Size(940, 560);
            this.rootLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(940, 45);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "VISION CAMERA CAL";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGuide
            // 
            this.lblGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGuide.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblGuide.Location = new System.Drawing.Point(12, 53);
            this.lblGuide.Margin = new System.Windows.Forms.Padding(12, 8, 12, 4);
            this.lblGuide.Name = "lblGuide";
            this.lblGuide.Size = new System.Drawing.Size(916, 42);
            this.lblGuide.TabIndex = 1;
            this.lblGuide.Text = "Bottom/Input/Output 카메라가 같은 Reticle을 찾은 좌표와 현재 모터 위치를 저장합니다. Reticle 실린더 방향은 자동으로 움직이지 않습니다.";
            this.lblGuide.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // gridMeasurements
            // 
            this.gridMeasurements.AllowUserToAddRows = false;
            this.gridMeasurements.AllowUserToDeleteRows = false;
            this.gridMeasurements.AllowUserToResizeRows = false;
            this.gridMeasurements.BackgroundColor = System.Drawing.Color.White;
            this.gridMeasurements.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridMeasurements.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colItem,
            this.colPixel,
            this.colMm,
            this.colAxis,
            this.colScore});
            this.gridMeasurements.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridMeasurements.Location = new System.Drawing.Point(12, 107);
            this.gridMeasurements.Margin = new System.Windows.Forms.Padding(12, 4, 12, 8);
            this.gridMeasurements.MultiSelect = false;
            this.gridMeasurements.Name = "gridMeasurements";
            this.gridMeasurements.ReadOnly = true;
            this.gridMeasurements.RowHeadersVisible = false;
            this.gridMeasurements.RowTemplate.Height = 26;
            this.gridMeasurements.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridMeasurements.Size = new System.Drawing.Size(916, 273);
            this.gridMeasurements.TabIndex = 2;
            // 
            // colItem
            // 
            this.colItem.HeaderText = "CAMERA";
            this.colItem.Name = "colItem";
            this.colItem.ReadOnly = true;
            this.colItem.Width = 150;
            // 
            // colPixel
            // 
            this.colPixel.HeaderText = "PIXEL X/Y";
            this.colPixel.Name = "colPixel";
            this.colPixel.ReadOnly = true;
            this.colPixel.Width = 170;
            // 
            // colMm
            // 
            this.colMm.HeaderText = "MM X/Y";
            this.colMm.Name = "colMm";
            this.colMm.ReadOnly = true;
            this.colMm.Width = 170;
            // 
            // colAxis
            // 
            this.colAxis.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colAxis.HeaderText = "AXIS POSITION";
            this.colAxis.Name = "colAxis";
            this.colAxis.ReadOnly = true;
            // 
            // colScore
            // 
            this.colScore.HeaderText = "SCORE";
            this.colScore.Name = "colScore";
            this.colScore.ReadOnly = true;
            this.colScore.Width = 90;
            // 
            // lblOffsets
            // 
            this.lblOffsets.BackColor = System.Drawing.Color.White;
            this.lblOffsets.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOffsets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOffsets.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblOffsets.Location = new System.Drawing.Point(12, 388);
            this.lblOffsets.Margin = new System.Windows.Forms.Padding(12, 0, 12, 8);
            this.lblOffsets.Name = "lblOffsets";
            this.lblOffsets.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblOffsets.Size = new System.Drawing.Size(916, 44);
            this.lblOffsets.TabIndex = 3;
            this.lblOffsets.Text = "Offset: -";
            this.lblOffsets.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStatus
            // 
            this.lblStatus.BackColor = System.Drawing.Color.White;
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblStatus.Location = new System.Drawing.Point(12, 440);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(12, 0, 12, 8);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(916, 50);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "대기 중입니다.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnCheck);
            this.buttonPanel.Controls.Add(this.btnFindBottom);
            this.buttonPanel.Controls.Add(this.btnFindInput);
            this.buttonPanel.Controls.Add(this.btnFindOutput);
            this.buttonPanel.Controls.Add(this.btnRunAll);
            this.buttonPanel.Controls.Add(this.btnCalculateSave);
            this.buttonPanel.Controls.Add(this.btnClose);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Location = new System.Drawing.Point(12, 498);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(12, 0, 12, 10);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(916, 52);
            this.buttonPanel.TabIndex = 5;
            // 
            // btnCheck
            // 
            this.btnCheck.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnCheck.Location = new System.Drawing.Point(786, 6);
            this.btnCheck.Margin = new System.Windows.Forms.Padding(6);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Size = new System.Drawing.Size(124, 38);
            this.btnCheck.TabIndex = 0;
            this.btnCheck.Text = "CHECK READY";
            this.btnCheck.UseVisualStyleBackColor = true;
            // 
            // btnFindBottom
            // 
            this.btnFindBottom.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnFindBottom.Location = new System.Drawing.Point(660, 6);
            this.btnFindBottom.Margin = new System.Windows.Forms.Padding(6);
            this.btnFindBottom.Name = "btnFindBottom";
            this.btnFindBottom.Size = new System.Drawing.Size(114, 38);
            this.btnFindBottom.TabIndex = 1;
            this.btnFindBottom.Text = "FIND BOTTOM";
            this.btnFindBottom.UseVisualStyleBackColor = true;
            // 
            // btnFindInput
            // 
            this.btnFindInput.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnFindInput.Location = new System.Drawing.Point(544, 6);
            this.btnFindInput.Margin = new System.Windows.Forms.Padding(6);
            this.btnFindInput.Name = "btnFindInput";
            this.btnFindInput.Size = new System.Drawing.Size(104, 38);
            this.btnFindInput.TabIndex = 2;
            this.btnFindInput.Text = "FIND INPUT";
            this.btnFindInput.UseVisualStyleBackColor = true;
            // 
            // btnFindOutput
            // 
            this.btnFindOutput.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnFindOutput.Location = new System.Drawing.Point(416, 6);
            this.btnFindOutput.Margin = new System.Windows.Forms.Padding(6);
            this.btnFindOutput.Name = "btnFindOutput";
            this.btnFindOutput.Size = new System.Drawing.Size(116, 38);
            this.btnFindOutput.TabIndex = 3;
            this.btnFindOutput.Text = "FIND OUTPUT";
            this.btnFindOutput.UseVisualStyleBackColor = true;
            // 
            // btnRunAll
            // 
            this.btnRunAll.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnRunAll.Location = new System.Drawing.Point(290, 6);
            this.btnRunAll.Margin = new System.Windows.Forms.Padding(6);
            this.btnRunAll.Name = "btnRunAll";
            this.btnRunAll.Size = new System.Drawing.Size(114, 38);
            this.btnRunAll.TabIndex = 4;
            this.btnRunAll.Text = "RUN CURRENT";
            this.btnRunAll.UseVisualStyleBackColor = true;
            // 
            // btnCalculateSave
            // 
            this.btnCalculateSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.btnCalculateSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalculateSave.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnCalculateSave.ForeColor = System.Drawing.Color.White;
            this.btnCalculateSave.Location = new System.Drawing.Point(138, 6);
            this.btnCalculateSave.Margin = new System.Windows.Forms.Padding(6);
            this.btnCalculateSave.Name = "btnCalculateSave";
            this.btnCalculateSave.Size = new System.Drawing.Size(140, 38);
            this.btnCalculateSave.TabIndex = 5;
            this.btnCalculateSave.Text = "CALC / SAVE";
            this.btnCalculateSave.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.Location = new System.Drawing.Point(26, 6);
            this.btnClose.Margin = new System.Windows.Forms.Padding(6);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 38);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // VisionCameraCalibrationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(210)))), ((int)(((byte)(214)))));
            this.ClientSize = new System.Drawing.Size(940, 560);
            this.Controls.Add(this.rootLayout);
            this.Name = "VisionCameraCalibrationDialog";
            this.Text = "Vision Camera Calibration";
            this.rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridMeasurements)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
