namespace QMC.CDT_320.Ui.Dialogs
{
    partial class InputStageDieMapSetupDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel optionLayout;
        private System.Windows.Forms.Label lblVisionTarget;
        private System.Windows.Forms.Label lblRetry;
        private System.Windows.Forms.TextBox _targetId;
        private System.Windows.Forms.NumericUpDown _retryCount;
        private System.Windows.Forms.TableLayoutPanel bodyLayout;
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.Panel _view;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStageY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVisionX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOffsetX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOffsetY;
        private System.Windows.Forms.TableLayoutPanel bottomLayout;
        private System.Windows.Forms.Label _status;
        private System.Windows.Forms.FlowLayoutPanel bottomButtons;
        private System.Windows.Forms.Button btnTeach;
        private System.Windows.Forms.Button btnMove;
        private System.Windows.Forms.Button btnVisionTest;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle headerStyle = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle numberStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.optionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblVisionTarget = new System.Windows.Forms.Label();
            this.lblRetry = new System.Windows.Forms.Label();
            this._targetId = new System.Windows.Forms.TextBox();
            this._retryCount = new System.Windows.Forms.NumericUpDown();
            this.bodyLayout = new System.Windows.Forms.TableLayoutPanel();
            this._grid = new System.Windows.Forms.DataGridView();
            this._view = new System.Windows.Forms.Panel();
            this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStageY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colVisionX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOffsetX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOffsetY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bottomLayout = new System.Windows.Forms.TableLayoutPanel();
            this._status = new System.Windows.Forms.Label();
            this.bottomButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnTeach = new System.Windows.Forms.Button();
            this.btnMove = new System.Windows.Forms.Button();
            this.btnVisionTest = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            this.optionLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._retryCount)).BeginInit();
            this.bodyLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.bottomLayout.SuspendLayout();
            this.bottomButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.optionLayout, 0, 0);
            this.rootLayout.Controls.Add(this.bodyLayout, 0, 1);
            this.rootLayout.Controls.Add(this.bottomLayout, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(12, 12);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.rootLayout.Size = new System.Drawing.Size(820, 397);
            this.rootLayout.TabIndex = 0;
            // 
            // bodyLayout
            // 
            this.bodyLayout.ColumnCount = 2;
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 68F));
            this.bodyLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.bodyLayout.Controls.Add(this._grid, 0, 0);
            this.bodyLayout.Controls.Add(this._view, 1, 0);
            this.bodyLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bodyLayout.Location = new System.Drawing.Point(0, 44);
            this.bodyLayout.Margin = new System.Windows.Forms.Padding(0);
            this.bodyLayout.Name = "bodyLayout";
            this.bodyLayout.RowCount = 1;
            this.bodyLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bodyLayout.Size = new System.Drawing.Size(820, 295);
            this.bodyLayout.TabIndex = 1;
            // 
            // optionLayout
            // 
            this.optionLayout.ColumnCount = 4;
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.optionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.optionLayout.Controls.Add(this.lblVisionTarget, 0, 0);
            this.optionLayout.Controls.Add(this._targetId, 1, 0);
            this.optionLayout.Controls.Add(this.lblRetry, 2, 0);
            this.optionLayout.Controls.Add(this._retryCount, 3, 0);
            this.optionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.optionLayout.Location = new System.Drawing.Point(0, 0);
            this.optionLayout.Margin = new System.Windows.Forms.Padding(0);
            this.optionLayout.Name = "optionLayout";
            this.optionLayout.RowCount = 1;
            this.optionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.optionLayout.Size = new System.Drawing.Size(820, 44);
            this.optionLayout.TabIndex = 0;
            // 
            // lblVisionTarget
            // 
            this.lblVisionTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVisionTarget.Location = new System.Drawing.Point(3, 0);
            this.lblVisionTarget.Name = "lblVisionTarget";
            this.lblVisionTarget.Size = new System.Drawing.Size(99, 44);
            this.lblVisionTarget.TabIndex = 0;
            this.lblVisionTarget.Text = "Vision Target";
            this.lblVisionTarget.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRetry
            // 
            this.lblRetry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRetry.Location = new System.Drawing.Point(663, 0);
            this.lblRetry.Name = "lblRetry";
            this.lblRetry.Size = new System.Drawing.Size(64, 44);
            this.lblRetry.TabIndex = 2;
            this.lblRetry.Text = "Retry";
            this.lblRetry.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _targetId
            // 
            this._targetId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._targetId.Location = new System.Drawing.Point(108, 10);
            this._targetId.Name = "_targetId";
            this._targetId.Size = new System.Drawing.Size(549, 25);
            this._targetId.TabIndex = 1;
            // 
            // _retryCount
            // 
            this._retryCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._retryCount.Location = new System.Drawing.Point(733, 10);
            this._retryCount.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._retryCount.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._retryCount.Name = "_retryCount";
            this._retryCount.Size = new System.Drawing.Size(84, 25);
            this._retryCount.TabIndex = 3;
            this._retryCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._retryCount.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // _grid
            // 
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.AllowUserToResizeRows = false;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            this._grid.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = System.Drawing.SystemColors.Control;
            headerStyle.Font = new System.Drawing.Font("Malgun Gothic", 9F);
            headerStyle.ForeColor = System.Drawing.SystemColors.WindowText;
            headerStyle.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            headerStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            headerStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._grid.ColumnHeadersDefaultCellStyle = headerStyle;
            this._grid.ColumnHeadersHeight = 32;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colEnabled,
            this.colName,
            this.colStageY,
            this.colVisionX,
            this.colOffsetX,
            this.colOffsetY});
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Location = new System.Drawing.Point(3, 3);
            this._grid.MultiSelect = false;
            this._grid.Name = "_grid";
            this._grid.RowHeadersVisible = false;
            this._grid.RowTemplate.Height = 28;
            this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._grid.Size = new System.Drawing.Size(551, 289);
            this._grid.TabIndex = 1;
            this._grid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.Grid_CellEndEdit);
            this._grid.SelectionChanged += new System.EventHandler(this.Grid_SelectionChanged);
            // 
            // _view
            // 
            this._view.BackColor = System.Drawing.Color.White;
            this._view.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._view.Dock = System.Windows.Forms.DockStyle.Fill;
            this._view.Location = new System.Drawing.Point(560, 3);
            this._view.Name = "_view";
            this._view.Size = new System.Drawing.Size(257, 289);
            this._view.TabIndex = 2;
            this._view.Paint += new System.Windows.Forms.PaintEventHandler(this.View_Paint);
            // 
            // colEnabled
            // 
            this.colEnabled.HeaderText = "Use";
            this.colEnabled.Name = "Enabled";
            this.colEnabled.Width = 48;
            // 
            // colName
            // 
            this.colName.HeaderText = "Point";
            this.colName.Name = "Name";
            this.colName.ReadOnly = true;
            this.colName.Width = 80;
            // 
            // colStageY
            // 
            numberStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.colStageY.DefaultCellStyle = numberStyle;
            this.colStageY.HeaderText = "StageY";
            this.colStageY.Name = "StageY";
            this.colStageY.Width = 110;
            // 
            // colVisionX
            // 
            this.colVisionX.DefaultCellStyle = numberStyle;
            this.colVisionX.HeaderText = "VisionX";
            this.colVisionX.Name = "VisionX";
            this.colVisionX.Width = 100;
            // 
            // colOffsetX
            // 
            this.colOffsetX.DefaultCellStyle = numberStyle;
            this.colOffsetX.HeaderText = "OffsetX";
            this.colOffsetX.Name = "OffsetX";
            this.colOffsetX.Width = 90;
            // 
            // colOffsetY
            // 
            this.colOffsetY.DefaultCellStyle = numberStyle;
            this.colOffsetY.HeaderText = "OffsetY";
            this.colOffsetY.Name = "OffsetY";
            this.colOffsetY.Width = 90;
            // 
            // bottomLayout
            // 
            this.bottomLayout.ColumnCount = 2;
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 580F));
            this.bottomLayout.Controls.Add(this._status, 0, 0);
            this.bottomLayout.Controls.Add(this.bottomButtons, 1, 0);
            this.bottomLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomLayout.Location = new System.Drawing.Point(0, 339);
            this.bottomLayout.Margin = new System.Windows.Forms.Padding(0);
            this.bottomLayout.Name = "bottomLayout";
            this.bottomLayout.RowCount = 1;
            this.bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomLayout.Size = new System.Drawing.Size(820, 58);
            this.bottomLayout.TabIndex = 2;
            // 
            // _status
            // 
            this._status.Dock = System.Windows.Forms.DockStyle.Fill;
            this._status.Location = new System.Drawing.Point(3, 0);
            this._status.Name = "_status";
            this._status.Size = new System.Drawing.Size(234, 58);
            this._status.TabIndex = 0;
            this._status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // bottomButtons
            // 
            this.bottomButtons.Controls.Add(this.btnClose);
            this.bottomButtons.Controls.Add(this.btnSave);
            this.bottomButtons.Controls.Add(this.btnVisionTest);
            this.bottomButtons.Controls.Add(this.btnMove);
            this.bottomButtons.Controls.Add(this.btnTeach);
            this.bottomButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.bottomButtons.Location = new System.Drawing.Point(243, 3);
            this.bottomButtons.Name = "bottomButtons";
            this.bottomButtons.Size = new System.Drawing.Size(574, 52);
            this.bottomButtons.TabIndex = 1;
            // 
            // btnTeach
            // 
            this.btnTeach.Location = new System.Drawing.Point(10, 7);
            this.btnTeach.Margin = new System.Windows.Forms.Padding(6, 7, 6, 6);
            this.btnTeach.Name = "btnTeach";
            this.btnTeach.Size = new System.Drawing.Size(96, 36);
            this.btnTeach.TabIndex = 3;
            this.btnTeach.Text = "TEACH";
            this.btnTeach.UseVisualStyleBackColor = true;
            this.btnTeach.Click += new System.EventHandler(this.BtnTeach_Click);
            // 
            // btnMove
            // 
            this.btnMove.Location = new System.Drawing.Point(118, 7);
            this.btnMove.Margin = new System.Windows.Forms.Padding(6, 7, 6, 6);
            this.btnMove.Name = "btnMove";
            this.btnMove.Size = new System.Drawing.Size(96, 36);
            this.btnMove.TabIndex = 4;
            this.btnMove.Text = "MOVE";
            this.btnMove.UseVisualStyleBackColor = true;
            this.btnMove.Click += new System.EventHandler(this.BtnMove_Click);
            // 
            // btnVisionTest
            // 
            this.btnVisionTest.Location = new System.Drawing.Point(226, 7);
            this.btnVisionTest.Margin = new System.Windows.Forms.Padding(6, 7, 6, 6);
            this.btnVisionTest.Name = "btnVisionTest";
            this.btnVisionTest.Size = new System.Drawing.Size(104, 36);
            this.btnVisionTest.TabIndex = 3;
            this.btnVisionTest.Text = "VISION TEST";
            this.btnVisionTest.UseVisualStyleBackColor = true;
            this.btnVisionTest.Click += new System.EventHandler(this.BtnVisionTest_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(342, 7);
            this.btnSave.Margin = new System.Windows.Forms.Padding(6, 7, 6, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(88, 36);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "SAVE";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(442, 7);
            this.btnClose.Margin = new System.Windows.Forms.Padding(6, 7, 6, 6);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(94, 36);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "CLOSE";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // InputStageDieMapSetupDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(844, 421);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("Malgun Gothic", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputStageDieMapSetupDialog";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Input Stage Die Map Setup";
            this.rootLayout.ResumeLayout(false);
            this.optionLayout.ResumeLayout(false);
            this.optionLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._retryCount)).EndInit();
            this.bodyLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this.bottomLayout.ResumeLayout(false);
            this.bottomButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
