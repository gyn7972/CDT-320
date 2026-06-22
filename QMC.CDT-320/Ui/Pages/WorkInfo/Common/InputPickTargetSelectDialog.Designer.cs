using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Pages.WorkInfo
{
    partial class InputPickTargetSelectDialog
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel optionLayout;
        private Label lblPickerNo;
        private ComboBox cmbPickerNo;
        private Button btnRefresh;
        private SplitContainer splitTarget;
        private QMC.CDT320.Ui.Controls.DieMapView mapView;
        private DataGridView gridTargets;
        private DataGridViewTextBoxColumn colOrder;
        private DataGridViewTextBoxColumn colDieId;
        private DataGridViewTextBoxColumn colMap;
        private DataGridViewTextBoxColumn colTarget;
        private Label lblPrepared;
        private Label lblStatus;
        private TableLayoutPanel stepLayout;
        private Label lblStep;
        private ComboBox cmbPickZStep;
        private FlowLayoutPanel buttonPanel;
        private Button btnRunStep;
        private Button btnNextStep;
        private Button btnPrepare;
        private Button btnPickZTest;
        private Button btnClose;

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.optionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblPickerNo = new System.Windows.Forms.Label();
            this.cmbPickerNo = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.splitTarget = new System.Windows.Forms.SplitContainer();
            this.mapView = new QMC.CDT320.Ui.Controls.DieMapView();
            this.gridTargets = new System.Windows.Forms.DataGridView();
            this.colOrder = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDieId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMap = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTarget = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblPrepared = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.stepLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblStep = new System.Windows.Forms.Label();
            this.cmbPickZStep = new System.Windows.Forms.ComboBox();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnPickZTest = new System.Windows.Forms.Button();
            this.btnNextStep = new System.Windows.Forms.Button();
            this.btnRunStep = new System.Windows.Forms.Button();
            this.btnPrepare = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.optionLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitTarget)).BeginInit();
            this.splitTarget.Panel1.SuspendLayout();
            this.splitTarget.Panel2.SuspendLayout();
            this.splitTarget.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridTargets)).BeginInit();
            this.stepLayout.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.optionLayout, 0, 1);
            this.rootLayout.Controls.Add(this.splitTarget, 0, 2);
            this.rootLayout.Controls.Add(this.lblPrepared, 0, 3);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 4);
            this.rootLayout.Controls.Add(this.stepLayout, 0, 5);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 6);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 7;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.rootLayout.Size = new System.Drawing.Size(860, 560);
            this.rootLayout.TabIndex = 0;
            //
            // lblHeader
            //
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(860, 42);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "INPUT DIE PICKUP TEST";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // optionLayout
            //
            this.optionLayout.ColumnCount = 4;
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.optionLayout.Controls.Add(this.lblPickerNo, 0, 0);
            this.optionLayout.Controls.Add(this.cmbPickerNo, 1, 0);
            this.optionLayout.Controls.Add(this.btnRefresh, 2, 0);
            this.optionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.optionLayout.Location = new System.Drawing.Point(8, 48);
            this.optionLayout.Margin = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.optionLayout.Name = "optionLayout";
            this.optionLayout.RowCount = 1;
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.optionLayout.Size = new System.Drawing.Size(844, 32);
            this.optionLayout.TabIndex = 1;
            //
            // lblPickerNo
            //
            this.lblPickerNo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPickerNo.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblPickerNo.Location = new System.Drawing.Point(3, 0);
            this.lblPickerNo.Name = "lblPickerNo";
            this.lblPickerNo.Size = new System.Drawing.Size(104, 32);
            this.lblPickerNo.TabIndex = 0;
            this.lblPickerNo.Text = "Picker No";
            this.lblPickerNo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbPickerNo
            //
            this.cmbPickerNo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbPickerNo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPickerNo.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.cmbPickerNo.FormattingEnabled = true;
            this.cmbPickerNo.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4"});
            this.cmbPickerNo.Location = new System.Drawing.Point(113, 3);
            this.cmbPickerNo.Name = "cmbPickerNo";
            this.cmbPickerNo.Size = new System.Drawing.Size(104, 25);
            this.cmbPickerNo.TabIndex = 1;
            //
            // btnRefresh
            //
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnRefresh.Location = new System.Drawing.Point(223, 3);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(104, 26);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "REFRESH";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            //
            // splitTarget
            //
            this.splitTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitTarget.Location = new System.Drawing.Point(8, 90);
            this.splitTarget.Margin = new System.Windows.Forms.Padding(8, 4, 8, 4);
            this.splitTarget.Name = "splitTarget";
            //
            // splitTarget.Panel1
            //
            this.splitTarget.Panel1.Controls.Add(this.mapView);
            //
            // splitTarget.Panel2
            //
            this.splitTarget.Panel2.Controls.Add(this.gridTargets);
            this.splitTarget.Size = new System.Drawing.Size(844, 374);
            this.splitTarget.SplitterDistance = 560;
            this.splitTarget.TabIndex = 2;
            //
            // mapView
            //
            this.mapView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.mapView.Caption = "InputStage Wafer";
            this.mapView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapView.Location = new System.Drawing.Point(0, 0);
            this.mapView.Name = "mapView";
            this.mapView.ShowWaferOutline = true;
            this.mapView.Size = new System.Drawing.Size(560, 374);
            this.mapView.TabIndex = 0;
            //
            // gridTargets
            //
            this.gridTargets.AllowUserToAddRows = false;
            this.gridTargets.AllowUserToDeleteRows = false;
            this.gridTargets.AllowUserToResizeRows = false;
            this.gridTargets.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridTargets.BackgroundColor = System.Drawing.Color.White;
            this.gridTargets.ColumnHeadersHeight = 30;
            this.gridTargets.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridTargets.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colOrder,
            this.colDieId,
            this.colMap,
            this.colTarget});
            this.gridTargets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridTargets.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.gridTargets.EnableHeadersVisualStyles = false;
            this.gridTargets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridTargets.Location = new System.Drawing.Point(0, 0);
            this.gridTargets.Margin = new System.Windows.Forms.Padding(0);
            this.gridTargets.MultiSelect = false;
            this.gridTargets.Name = "gridTargets";
            this.gridTargets.ReadOnly = true;
            this.gridTargets.RowHeadersVisible = false;
            this.gridTargets.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridTargets.Size = new System.Drawing.Size(280, 374);
            this.gridTargets.TabIndex = 0;
            //
            // colOrder
            //
            this.colOrder.FillWeight = 45F;
            this.colOrder.HeaderText = "Order";
            this.colOrder.Name = "colOrder";
            this.colOrder.ReadOnly = true;
            this.colOrder.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colDieId
            //
            this.colDieId.FillWeight = 220F;
            this.colDieId.HeaderText = "Die ID";
            this.colDieId.Name = "colDieId";
            this.colDieId.ReadOnly = true;
            this.colDieId.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colMap
            //
            this.colMap.FillWeight = 70F;
            this.colMap.HeaderText = "Map X/Y";
            this.colMap.Name = "colMap";
            this.colMap.ReadOnly = true;
            this.colMap.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colTarget
            //
            this.colTarget.FillWeight = 130F;
            this.colTarget.HeaderText = "Target X/Y";
            this.colTarget.Name = "colTarget";
            this.colTarget.ReadOnly = true;
            this.colTarget.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // lblPrepared
            //
            this.lblPrepared.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPrepared.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblPrepared.Location = new System.Drawing.Point(8, 468);
            this.lblPrepared.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblPrepared.Name = "lblPrepared";
            this.lblPrepared.Size = new System.Drawing.Size(844, 30);
            this.lblPrepared.TabIndex = 3;
            this.lblPrepared.Text = "Prepared: -";
            this.lblPrepared.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblStatus
            //
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblStatus.Location = new System.Drawing.Point(8, 498);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(844, 34);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "-";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // stepLayout
            //
            this.stepLayout.ColumnCount = 3;
            this.stepLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.stepLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 320F));
            this.stepLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.stepLayout.Controls.Add(this.lblStep, 0, 0);
            this.stepLayout.Controls.Add(this.cmbPickZStep, 1, 0);
            this.stepLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.stepLayout.Location = new System.Drawing.Point(8, 532);
            this.stepLayout.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.stepLayout.Name = "stepLayout";
            this.stepLayout.RowCount = 1;
            this.stepLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.stepLayout.Size = new System.Drawing.Size(964, 42);
            this.stepLayout.TabIndex = 5;
            //
            // lblStep
            //
            this.lblStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStep.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.lblStep.Location = new System.Drawing.Point(3, 0);
            this.lblStep.Name = "lblStep";
            this.lblStep.Size = new System.Drawing.Size(84, 42);
            this.lblStep.TabIndex = 0;
            this.lblStep.Text = "Pick Z Step";
            this.lblStep.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbPickZStep
            //
            this.cmbPickZStep.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbPickZStep.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPickZStep.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.cmbPickZStep.FormattingEnabled = true;
            this.cmbPickZStep.Location = new System.Drawing.Point(93, 8);
            this.cmbPickZStep.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.cmbPickZStep.Name = "cmbPickZStep";
            this.cmbPickZStep.Size = new System.Drawing.Size(314, 25);
            this.cmbPickZStep.TabIndex = 1;
            //
            // buttonPanel
            //
            this.buttonPanel.Controls.Add(this.btnClose);
            this.buttonPanel.Controls.Add(this.btnPickZTest);
            this.buttonPanel.Controls.Add(this.btnNextStep);
            this.buttonPanel.Controls.Add(this.btnRunStep);
            this.buttonPanel.Controls.Add(this.btnPrepare);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonPanel.Location = new System.Drawing.Point(8, 578);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(8, 4, 8, 8);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.buttonPanel.Size = new System.Drawing.Size(964, 74);
            this.buttonPanel.TabIndex = 6;
            //
            // btnClose
            //
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.btnClose.Location = new System.Drawing.Point(722, 11);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(116, 34);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // btnPickZTest
            //
            this.btnPickZTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnPickZTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPickZTest.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPickZTest.ForeColor = System.Drawing.Color.White;
            this.btnPickZTest.Location = new System.Drawing.Point(586, 11);
            this.btnPickZTest.Name = "btnPickZTest";
            this.btnPickZTest.Size = new System.Drawing.Size(130, 34);
            this.btnPickZTest.TabIndex = 1;
            this.btnPickZTest.Text = "PICK Z TEST";
            this.btnPickZTest.UseVisualStyleBackColor = false;
            this.btnPickZTest.Click += new System.EventHandler(this.btnPickZTest_Click);
            //
            // btnNextStep
            //
            this.btnNextStep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.btnNextStep.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNextStep.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnNextStep.ForeColor = System.Drawing.Color.White;
            this.btnNextStep.Location = new System.Drawing.Point(450, 11);
            this.btnNextStep.Name = "btnNextStep";
            this.btnNextStep.Size = new System.Drawing.Size(130, 34);
            this.btnNextStep.TabIndex = 2;
            this.btnNextStep.Text = "NEXT STEP";
            this.btnNextStep.UseVisualStyleBackColor = false;
            this.btnNextStep.Click += new System.EventHandler(this.btnNextStep_Click);
            //
            // btnRunStep
            //
            this.btnRunStep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(96)))), ((int)(((byte)(96)))), ((int)(((byte)(96)))));
            this.btnRunStep.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRunStep.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnRunStep.ForeColor = System.Drawing.Color.White;
            this.btnRunStep.Location = new System.Drawing.Point(314, 11);
            this.btnRunStep.Name = "btnRunStep";
            this.btnRunStep.Size = new System.Drawing.Size(130, 34);
            this.btnRunStep.TabIndex = 3;
            this.btnRunStep.Text = "RUN STEP";
            this.btnRunStep.UseVisualStyleBackColor = false;
            this.btnRunStep.Click += new System.EventHandler(this.btnRunStep_Click);
            //
            // btnPrepare
            //
            this.btnPrepare.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(119)))), ((int)(((byte)(6)))));
            this.btnPrepare.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrepare.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnPrepare.ForeColor = System.Drawing.Color.White;
            this.btnPrepare.Location = new System.Drawing.Point(430, 11);
            this.btnPrepare.Name = "btnPrepare";
            this.btnPrepare.Size = new System.Drawing.Size(150, 34);
            this.btnPrepare.TabIndex = 0;
            this.btnPrepare.Text = "INSPECT / MOVE";
            this.btnPrepare.UseVisualStyleBackColor = false;
            this.btnPrepare.Click += new System.EventHandler(this.btnPrepare_Click);
            //
            // InputPickTargetSelectDialog
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(980, 660);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputPickTargetSelectDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Input Die PickUp Test";
            this.rootLayout.ResumeLayout(false);
            this.optionLayout.ResumeLayout(false);
            this.splitTarget.Panel1.ResumeLayout(false);
            this.splitTarget.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitTarget)).EndInit();
            this.splitTarget.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridTargets)).EndInit();
            this.stepLayout.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
