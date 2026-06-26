namespace QMC.CDT_320.Ui.Dialogs
{
    partial class VisionCameraScaleDialog
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.DataGridView gridCameraScale;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCamera;
        private System.Windows.Forms.DataGridViewTextBoxColumn colWidth;
        private System.Windows.Forms.DataGridViewTextBoxColumn colHeight;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScaleX;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScaleY;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSource;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnCameraSettingReq;
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
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.gridCameraScale = new System.Windows.Forms.DataGridView();
            this.colCamera = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWidth = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colHeight = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScaleX = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScaleY = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblStatus = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnCameraSettingReq = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridCameraScale)).BeginInit();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.lblDescription, 0, 1);
            this.rootLayout.Controls.Add(this.gridCameraScale, 0, 2);
            this.rootLayout.Controls.Add(this.lblStatus, 0, 3);
            this.rootLayout.Controls.Add(this.buttonPanel, 0, 4);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 5;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.rootLayout.Size = new System.Drawing.Size(760, 420);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(18, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(760, 48);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "CAMERA PIXEL SCALE SETUP";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDescription
            // 
            this.lblDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDescription.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDescription.Location = new System.Drawing.Point(12, 48);
            this.lblDescription.Margin = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(736, 58);
            this.lblDescription.TabIndex = 1;
            this.lblDescription.Text = "VisionPC는 pixel 좌표를 전송합니다. Handler는 카메라별 Width/Height와 Scale(mm/px)을 저장하고, Center는 Width/2, Height/2로 자동 계산합니다.";
            this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // gridCameraScale
            // 
            this.gridCameraScale.AllowUserToAddRows = false;
            this.gridCameraScale.AllowUserToDeleteRows = false;
            this.gridCameraScale.AllowUserToResizeRows = false;
            this.gridCameraScale.BackgroundColor = System.Drawing.Color.White;
            this.gridCameraScale.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridCameraScale.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCamera,
            this.colWidth,
            this.colHeight,
            this.colScaleX,
            this.colScaleY,
            this.colSource});
            this.gridCameraScale.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridCameraScale.Location = new System.Drawing.Point(12, 106);
            this.gridCameraScale.Margin = new System.Windows.Forms.Padding(12, 0, 12, 8);
            this.gridCameraScale.MultiSelect = false;
            this.gridCameraScale.Name = "gridCameraScale";
            this.gridCameraScale.RowHeadersVisible = false;
            this.gridCameraScale.RowTemplate.Height = 24;
            this.gridCameraScale.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridCameraScale.Size = new System.Drawing.Size(736, 208);
            this.gridCameraScale.TabIndex = 2;
            this.gridCameraScale.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridCameraScale_CellValueChanged);
            this.gridCameraScale.CurrentCellDirtyStateChanged += new System.EventHandler(this.gridCameraScale_CurrentCellDirtyStateChanged);
            // 
            // colCamera
            // 
            this.colCamera.HeaderText = "CAMERA";
            this.colCamera.Name = "colCamera";
            this.colCamera.ReadOnly = true;
            this.colCamera.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colCamera.Width = 170;
            // 
            // colWidth
            // 
            this.colWidth.HeaderText = "WIDTH(px)";
            this.colWidth.Name = "colWidth";
            this.colWidth.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colWidth.Width = 105;
            // 
            // colHeight
            // 
            this.colHeight.HeaderText = "HEIGHT(px)";
            this.colHeight.Name = "colHeight";
            this.colHeight.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colHeight.Width = 105;
            // 
            // colScaleX
            // 
            this.colScaleX.HeaderText = "SCALE X(mm/px)";
            this.colScaleX.Name = "colScaleX";
            this.colScaleX.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colScaleX.Width = 140;
            // 
            // colScaleY
            // 
            this.colScaleY.HeaderText = "SCALE Y(mm/px)";
            this.colScaleY.Name = "colScaleY";
            this.colScaleY.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colScaleY.Width = 140;
            // 
            // colSource
            // 
            this.colSource.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colSource.HeaderText = "SOURCE";
            this.colSource.Name = "colSource";
            this.colSource.ReadOnly = true;
            this.colSource.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // lblStatus
            // 
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblStatus.Location = new System.Drawing.Point(12, 322);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(12, 0, 12, 8);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblStatus.Size = new System.Drawing.Size(736, 38);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "대기 중입니다.";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonPanel
            // 
            this.buttonPanel.ColumnCount = 5;
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.buttonPanel.Controls.Add(this.btnReload, 0, 0);
            this.buttonPanel.Controls.Add(this.btnCameraSettingReq, 1, 0);
            this.buttonPanel.Controls.Add(this.btnSave, 2, 0);
            this.buttonPanel.Controls.Add(this.btnClose, 4, 0);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Location = new System.Drawing.Point(12, 368);
            this.buttonPanel.Margin = new System.Windows.Forms.Padding(12, 0, 12, 10);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.Size = new System.Drawing.Size(736, 42);
            this.buttonPanel.TabIndex = 4;
            // 
            // btnReload
            // 
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReload.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnReload.Location = new System.Drawing.Point(0, 0);
            this.btnReload.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(112, 42);
            this.btnReload.TabIndex = 0;
            this.btnReload.Text = "RELOAD";
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);
            // 
            // btnCameraSettingReq
            // 
            this.btnCameraSettingReq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCameraSettingReq.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCameraSettingReq.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnCameraSettingReq.Location = new System.Drawing.Point(120, 0);
            this.btnCameraSettingReq.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnCameraSettingReq.Name = "btnCameraSettingReq";
            this.btnCameraSettingReq.Size = new System.Drawing.Size(162, 42);
            this.btnCameraSettingReq.TabIndex = 1;
            this.btnCameraSettingReq.Text = "CAMERA SETTING REQ";
            this.btnCameraSettingReq.Click += new System.EventHandler(this.btnCameraSettingReq_Click);
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(290, 0);
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(112, 42);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "SAVE";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.Location = new System.Drawing.Point(626, 0);
            this.btnClose.Margin = new System.Windows.Forms.Padding(0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(110, 42);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "CLOSE";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // VisionCameraScaleDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(760, 420);
            this.Controls.Add(this.rootLayout);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimumSize = new System.Drawing.Size(720, 360);
            this.Name = "VisionCameraScaleDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Camera Pixel Scale Setup";
            this.rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridCameraScale)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
