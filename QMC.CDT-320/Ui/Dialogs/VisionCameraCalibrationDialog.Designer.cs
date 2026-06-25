using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    partial class VisionCameraCalibrationDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private Label lblGuide;
        private TableLayoutPanel contentLayout;
        private DataGridView gridMeasurements;
        private DataGridViewTextBoxColumn colItem;
        private DataGridViewTextBoxColumn colPixel;
        private DataGridViewTextBoxColumn colMm;
        private DataGridViewTextBoxColumn colAngle;
        private DataGridViewTextBoxColumn colAxis;
        private DataGridViewTextBoxColumn colScore;
        private Label lblSequenceGuide;
        private Label lblOffsets;
        private Label lblStatus;
        private FlowLayoutPanel buttonPanel;
        private Button btnCheck;
        private Button btnFindBottom;
        private Button btnFindInput;
        private Button btnFindOutput;
        private Button btnRunAll;
        private Button btnRetractReticle;
        private Button btnCalculateSave;
        private Button btnClose;
        private ToolTip toolTip;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblGuide = new System.Windows.Forms.Label();
            this.contentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.gridMeasurements = new System.Windows.Forms.DataGridView();
            this.colItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPixel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMm = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAngle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAxis = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblSequenceGuide = new System.Windows.Forms.Label();
            this.lblOffsets = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCheck = new System.Windows.Forms.Button();
            this.btnFindBottom = new System.Windows.Forms.Button();
            this.btnFindInput = new System.Windows.Forms.Button();
            this.btnFindOutput = new System.Windows.Forms.Button();
            this.btnRunAll = new System.Windows.Forms.Button();
            this.btnRetractReticle = new System.Windows.Forms.Button();
            this.btnCalculateSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.rootLayout.SuspendLayout();
            this.contentLayout.SuspendLayout();
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
            this.rootLayout.Controls.Add(this.contentLayout, 0, 2);
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
            this.rootLayout.Size = new System.Drawing.Size(1040, 600);
            this.rootLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(1040, 45);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "VISION CAMERA CAL";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGuide
            // 
            this.lblGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGuide.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.lblGuide.Location = new System.Drawing.Point(12, 53);
            this.lblGuide.Margin = new System.Windows.Forms.Padding(12, 8, 12, 4);
            this.lblGuide.Name = "lblGuide";
            this.lblGuide.Size = new System.Drawing.Size(1016, 42);
            this.lblGuide.TabIndex = 1;
            this.lblGuide.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contentLayout
            // 
            this.contentLayout.ColumnCount = 2;
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 72F));
            this.contentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28F));
            this.contentLayout.Controls.Add(this.gridMeasurements, 0, 0);
            this.contentLayout.Controls.Add(this.lblSequenceGuide, 1, 0);
            this.contentLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentLayout.Location = new System.Drawing.Point(12, 107);
            this.contentLayout.Margin = new System.Windows.Forms.Padding(12, 4, 12, 8);
            this.contentLayout.Name = "contentLayout";
            this.contentLayout.RowCount = 1;
            this.contentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.contentLayout.Size = new System.Drawing.Size(1016, 313);
            this.contentLayout.TabIndex = 2;
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
            this.colAngle,
            this.colAxis,
            this.colScore});
            this.gridMeasurements.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridMeasurements.Location = new System.Drawing.Point(0, 0);
            this.gridMeasurements.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.gridMeasurements.MultiSelect = false;
            this.gridMeasurements.Name = "gridMeasurements";
            this.gridMeasurements.ReadOnly = true;
            this.gridMeasurements.RowHeadersVisible = false;
            this.gridMeasurements.RowTemplate.Height = 26;
            this.gridMeasurements.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridMeasurements.Size = new System.Drawing.Size(723, 313);
            this.gridMeasurements.TabIndex = 0;
            // 
            // colItem
            // 
            this.colItem.HeaderText = "CAMERA";
            this.colItem.Name = "colItem";
            this.colItem.ReadOnly = true;
            this.colItem.Width = 120;
            // 
            // colPixel
            // 
            this.colPixel.HeaderText = "PIXEL X/Y";
            this.colPixel.Name = "colPixel";
            this.colPixel.ReadOnly = true;
            this.colPixel.Width = 140;
            // 
            // colMm
            // 
            this.colMm.HeaderText = "MM X/Y";
            this.colMm.Name = "colMm";
            this.colMm.ReadOnly = true;
            this.colMm.Width = 140;
            // 
            // colAngle
            // 
            this.colAngle.HeaderText = "ANGLE T";
            this.colAngle.Name = "colAngle";
            this.colAngle.ReadOnly = true;
            this.colAngle.Width = 80;
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
            this.colScore.Width = 80;
            // 
            // lblSequenceGuide
            // 
            this.lblSequenceGuide.BackColor = System.Drawing.Color.White;
            this.lblSequenceGuide.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSequenceGuide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSequenceGuide.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.lblSequenceGuide.Location = new System.Drawing.Point(739, 0);
            this.lblSequenceGuide.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblSequenceGuide.Name = "lblSequenceGuide";
            this.lblSequenceGuide.Padding = new System.Windows.Forms.Padding(12);
            this.lblSequenceGuide.Size = new System.Drawing.Size(277, 313);
            this.lblSequenceGuide.TabIndex = 1;
            // 
            // lblOffsets
            // 
            this.lblOffsets.BackColor = System.Drawing.Color.White;
            this.lblOffsets.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblOffsets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOffsets.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.lblOffsets.Location = new System.Drawing.Point(12, 428);
            this.lblOffsets.Margin = new System.Windows.Forms.Padding(12, 0, 12, 8);
            this.lblOffsets.Name = "lblOffsets";
            this.lblOffsets.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblOffsets.Size = new System.Drawing.Size(1016, 44);
            this.lblOffsets.TabIndex = 3;
            this.lblOffsets.Text = "Offset: -";
            this.lblOffsets.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStatus
            // 
            this.lblStatus.BackColor = System.Drawing.Color.White;
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.lblStatus.Location = new System.Drawing.Point(12, 480);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(12, 0, 12, 8);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(1016, 50);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnCheck);
            this.buttonPanel.Controls.Add(this.btnRunAll);
            this.buttonPanel.Controls.Add(this.btnFindBottom);
            this.buttonPanel.Controls.Add(this.btnFindInput);
            this.buttonPanel.Controls.Add(this.btnFindOutput);
            this.buttonPanel.Controls.Add(this.btnRetractReticle);
            this.buttonPanel.Controls.Add(this.btnCalculateSave);
            this.buttonPanel.Controls.Add(this.btnClose);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.buttonPanel.Location = new System.Drawing.Point(12, 538);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(12, 0, 12, 10);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(1016, 52);
            this.buttonPanel.TabIndex = 5;
            this.buttonPanel.WrapContents = false;
            // 
            // btnCheck
            // 
            this.btnCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnCheck.Location = new System.Drawing.Point(6, 6);
            this.btnCheck.Margin = new System.Windows.Forms.Padding(6);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Size = new System.Drawing.Size(124, 38);
            this.btnCheck.TabIndex = 0;
            this.btnCheck.Text = "CHECK READY";
            this.btnCheck.UseVisualStyleBackColor = true;
            // 
            // btnFindBottom
            // 
            this.btnFindBottom.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnFindBottom.Location = new System.Drawing.Point(272, 6);
            this.btnFindBottom.Margin = new System.Windows.Forms.Padding(6);
            this.btnFindBottom.Name = "btnFindBottom";
            this.btnFindBottom.Size = new System.Drawing.Size(118, 38);
            this.btnFindBottom.TabIndex = 2;
            this.btnFindBottom.Text = "FIND BOTTOM";
            this.btnFindBottom.UseVisualStyleBackColor = true;
            // 
            // btnFindInput
            // 
            this.btnFindInput.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnFindInput.Location = new System.Drawing.Point(402, 6);
            this.btnFindInput.Margin = new System.Windows.Forms.Padding(6);
            this.btnFindInput.Name = "btnFindInput";
            this.btnFindInput.Size = new System.Drawing.Size(108, 38);
            this.btnFindInput.TabIndex = 3;
            this.btnFindInput.Text = "FIND INPUT";
            this.btnFindInput.UseVisualStyleBackColor = true;
            // 
            // btnFindOutput
            // 
            this.btnFindOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnFindOutput.Location = new System.Drawing.Point(522, 6);
            this.btnFindOutput.Margin = new System.Windows.Forms.Padding(6);
            this.btnFindOutput.Name = "btnFindOutput";
            this.btnFindOutput.Size = new System.Drawing.Size(118, 38);
            this.btnFindOutput.TabIndex = 4;
            this.btnFindOutput.Text = "FIND OUTPUT";
            this.btnFindOutput.UseVisualStyleBackColor = true;
            // 
            // btnRetractReticle
            // 
            this.btnRetractReticle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnRetractReticle.Location = new System.Drawing.Point(652, 6);
            this.btnRetractReticle.Margin = new System.Windows.Forms.Padding(6);
            this.btnRetractReticle.Name = "btnRetractReticle";
            this.btnRetractReticle.Size = new System.Drawing.Size(124, 38);
            this.btnRetractReticle.TabIndex = 5;
            this.btnRetractReticle.Text = "RETICLE BACK";
            this.btnRetractReticle.UseVisualStyleBackColor = true;
            // 
            // btnRunAll
            // 
            this.btnRunAll.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnRunAll.Location = new System.Drawing.Point(142, 6);
            this.btnRunAll.Margin = new System.Windows.Forms.Padding(6);
            this.btnRunAll.Name = "btnRunAll";
            this.btnRunAll.Size = new System.Drawing.Size(118, 38);
            this.btnRunAll.TabIndex = 1;
            this.btnRunAll.Text = "RUN CURRENT";
            this.btnRunAll.UseVisualStyleBackColor = true;
            // 
            // btnCalculateSave
            // 
            this.btnCalculateSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.btnCalculateSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCalculateSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnCalculateSave.ForeColor = System.Drawing.Color.White;
            this.btnCalculateSave.Location = new System.Drawing.Point(788, 6);
            this.btnCalculateSave.Margin = new System.Windows.Forms.Padding(6);
            this.btnCalculateSave.Name = "btnCalculateSave";
            this.btnCalculateSave.Size = new System.Drawing.Size(130, 38);
            this.btnCalculateSave.TabIndex = 6;
            this.btnCalculateSave.Text = "CALC / SAVE";
            this.btnCalculateSave.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.Location = new System.Drawing.Point(930, 6);
            this.btnClose.Margin = new System.Windows.Forms.Padding(6);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(74, 38);
            this.btnClose.TabIndex = 7;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // VisionCameraCalibrationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(210)))), ((int)(((byte)(214)))));
            this.ClientSize = new System.Drawing.Size(1040, 600);
            this.Controls.Add(this.rootLayout);
            this.Name = "VisionCameraCalibrationDialog";
            this.Text = "Vision Camera Calibration";
            this.rootLayout.ResumeLayout(false);
            this.contentLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridMeasurements)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
