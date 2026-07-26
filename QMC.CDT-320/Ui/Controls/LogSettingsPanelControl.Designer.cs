namespace QMC.CDT_320.Ui.Controls
{
    partial class LogSettingsPanelControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        // 로그 정리
        private System.Windows.Forms.GroupBox grpMaint;
        private System.Windows.Forms.TableLayoutPanel tlpMaint;
        private System.Windows.Forms.Label lblLogHistory;
        private System.Windows.Forms.ComboBox _cbLogHistory;
        private System.Windows.Forms.Label lblLogCompress;
        private System.Windows.Forms.ComboBox _cbLogCompress;
        private System.Windows.Forms.Label lblCompressDays;
        private System.Windows.Forms.NumericUpDown _nCompressDays;
        private System.Windows.Forms.Label lblLogDelete;
        private System.Windows.Forms.ComboBox _cbLogDelete;
        private System.Windows.Forms.Label lblDeleteDays;
        private System.Windows.Forms.NumericUpDown _nDeleteDays;
        // 로그 경로
        private System.Windows.Forms.GroupBox grpPaths;
        private System.Windows.Forms.Label lblPathMode;
        private System.Windows.Forms.ComboBox _cbPathMode;
        private System.Windows.Forms.DataGridView gridLogPaths;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLogPath;
        private System.Windows.Forms.DataGridViewButtonColumn colLogBrowse;
        // Material 스냅샷 저장 옵션
        private System.Windows.Forms.GroupBox grpSnapshot;
        private System.Windows.Forms.TableLayoutPanel tlpSnapshot;
        private System.Windows.Forms.CheckBox _chkInspectionDetail;
        private System.Windows.Forms.Label lblSnapshotHint;
        // Vision 이미지
        private System.Windows.Forms.GroupBox grpVision;
        private System.Windows.Forms.TableLayoutPanel tlpVision;
        private System.Windows.Forms.Label lblImageFormat;
        private System.Windows.Forms.ComboBox _cbImageFormat;
        private System.Windows.Forms.DataGridView gridVisionImages;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVisType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colVisPath;
        private System.Windows.Forms.DataGridViewButtonColumn colVisBrowse;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpMaint = new System.Windows.Forms.GroupBox();
            this.tlpMaint = new System.Windows.Forms.TableLayoutPanel();
            this.lblLogHistory = new System.Windows.Forms.Label();
            this._cbLogHistory = new System.Windows.Forms.ComboBox();
            this.lblLogCompress = new System.Windows.Forms.Label();
            this._cbLogCompress = new System.Windows.Forms.ComboBox();
            this.lblCompressDays = new System.Windows.Forms.Label();
            this._nCompressDays = new System.Windows.Forms.NumericUpDown();
            this.lblLogDelete = new System.Windows.Forms.Label();
            this._cbLogDelete = new System.Windows.Forms.ComboBox();
            this.lblDeleteDays = new System.Windows.Forms.Label();
            this._nDeleteDays = new System.Windows.Forms.NumericUpDown();
            this.grpPaths = new System.Windows.Forms.GroupBox();
            this.lblPathMode = new System.Windows.Forms.Label();
            this._cbPathMode = new System.Windows.Forms.ComboBox();
            this.gridLogPaths = new System.Windows.Forms.DataGridView();
            this.colLogType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLogBrowse = new System.Windows.Forms.DataGridViewButtonColumn();
            this.grpSnapshot = new System.Windows.Forms.GroupBox();
            this.tlpSnapshot = new System.Windows.Forms.TableLayoutPanel();
            this._chkInspectionDetail = new System.Windows.Forms.CheckBox();
            this.lblSnapshotHint = new System.Windows.Forms.Label();
            this.grpVision = new System.Windows.Forms.GroupBox();
            this.tlpVision = new System.Windows.Forms.TableLayoutPanel();
            this.lblImageFormat = new System.Windows.Forms.Label();
            this._cbImageFormat = new System.Windows.Forms.ComboBox();
            this.gridVisionImages = new System.Windows.Forms.DataGridView();
            this.colVisType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colVisPath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colVisBrowse = new System.Windows.Forms.DataGridViewButtonColumn();
            this.rootLayout.SuspendLayout();
            this.grpMaint.SuspendLayout();
            this.tlpMaint.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._nCompressDays)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDeleteDays)).BeginInit();
            this.grpPaths.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridLogPaths)).BeginInit();
            this.grpSnapshot.SuspendLayout();
            this.tlpSnapshot.SuspendLayout();
            this.grpVision.SuspendLayout();
            this.tlpVision.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridVisionImages)).BeginInit();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.grpMaint, 0, 0);
            this.rootLayout.Controls.Add(this.grpSnapshot, 0, 1);
            this.rootLayout.Controls.Add(this.grpPaths, 0, 2);
            this.rootLayout.Controls.Add(this.grpVision, 0, 3);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(1);
            // [레이아웃 정정 2026-07-27] 기존에는 4행이 전부 Absolute(130+336+150) + 빈 Percent 행이라
            // 합계 616px가 필요했는데 패널 높이가 그보다 작아 VISION IMAGE 그룹 하단이 잘렸다.
            // LOG FILE PATH 행만 Percent로 두어 남는 높이를 흡수하게 하고, 나머지는 내용에 맞춘 고정 높이로 둔다.
            this.rootLayout.RowCount = 4;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 78F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 176F));
            this.rootLayout.Size = new System.Drawing.Size(900, 700);
            this.rootLayout.TabIndex = 0;
            //
            // grpSnapshot
            //
            this.grpSnapshot.BackColor = System.Drawing.Color.White;
            this.grpSnapshot.Controls.Add(this.tlpSnapshot);
            this.grpSnapshot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSnapshot.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpSnapshot.Location = new System.Drawing.Point(1, 133);
            this.grpSnapshot.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.grpSnapshot.Name = "grpSnapshot";
            this.grpSnapshot.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.grpSnapshot.Size = new System.Drawing.Size(898, 76);
            this.grpSnapshot.TabIndex = 1;
            this.grpSnapshot.TabStop = false;
            this.grpSnapshot.Text = "MATERIAL SNAPSHOT (material_state.json)";
            //
            // tlpSnapshot
            //
            this.tlpSnapshot.ColumnCount = 2;
            this.tlpSnapshot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.tlpSnapshot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpSnapshot.Controls.Add(this._chkInspectionDetail, 0, 0);
            this.tlpSnapshot.Controls.Add(this.lblSnapshotHint, 1, 0);
            this.tlpSnapshot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpSnapshot.Location = new System.Drawing.Point(6, 23);
            this.tlpSnapshot.Name = "tlpSnapshot";
            this.tlpSnapshot.RowCount = 1;
            this.tlpSnapshot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpSnapshot.Size = new System.Drawing.Size(886, 49);
            this.tlpSnapshot.TabIndex = 0;
            //
            // _chkInspectionDetail
            //
            this._chkInspectionDetail.Appearance = System.Windows.Forms.Appearance.Button;
            this._chkInspectionDetail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(238)))), ((int)(((byte)(238)))));
            this._chkInspectionDetail.Cursor = System.Windows.Forms.Cursors.Hand;
            this._chkInspectionDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this._chkInspectionDetail.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._chkInspectionDetail.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this._chkInspectionDetail.Margin = new System.Windows.Forms.Padding(3, 6, 8, 6);
            this._chkInspectionDetail.Name = "_chkInspectionDetail";
            this._chkInspectionDetail.Size = new System.Drawing.Size(249, 37);
            this._chkInspectionDetail.TabIndex = 0;
            this._chkInspectionDetail.Text = "측정값 상세 저장  [OFF]";
            this._chkInspectionDetail.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._chkInspectionDetail.UseVisualStyleBackColor = false;
            //
            // lblSnapshotHint
            //
            this.lblSnapshotHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSnapshotHint.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSnapshotHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblSnapshotHint.Location = new System.Drawing.Point(263, 0);
            this.lblSnapshotHint.Name = "lblSnapshotHint";
            this.lblSnapshotHint.Size = new System.Drawing.Size(620, 49);
            this.lblSnapshotHint.TabIndex = 1;
            this.lblSnapshotHint.Text = "OFF 권장 — 검사 측정값 상세(Measurements/Alignments)를 스냅샷에서 제외합니다.\r\nON 하면 파일이 웨이퍼 1장당 약 5배로 커지고 저장 부하가 늘어납니다. 상세는 CSV에 별도 기록됩니다.";
            this.lblSnapshotHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grpMaint
            // 
            this.grpMaint.BackColor = System.Drawing.Color.White;
            this.grpMaint.Controls.Add(this.tlpMaint);
            this.grpMaint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMaint.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpMaint.Location = new System.Drawing.Point(1, 1);
            this.grpMaint.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.grpMaint.Name = "grpMaint";
            this.grpMaint.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpMaint.Size = new System.Drawing.Size(898, 128);
            this.grpMaint.TabIndex = 0;
            this.grpMaint.TabStop = false;
            this.grpMaint.Text = "LOG MAINTENANCE";
            // 
            // tlpMaint
            // 
            this.tlpMaint.BackColor = System.Drawing.Color.White;
            this.tlpMaint.ColumnCount = 5;
            this.tlpMaint.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tlpMaint.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpMaint.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.tlpMaint.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpMaint.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMaint.Controls.Add(this.lblPathMode, 0, 0);
            this.tlpMaint.Controls.Add(this._cbPathMode, 1, 0);
            this.tlpMaint.Controls.Add(this.lblLogHistory, 2, 0);
            this.tlpMaint.Controls.Add(this._cbLogHistory, 3, 0);
            this.tlpMaint.Controls.Add(this.lblLogCompress, 0, 1);
            this.tlpMaint.Controls.Add(this._cbLogCompress, 1, 1);
            this.tlpMaint.Controls.Add(this.lblCompressDays, 2, 1);
            this.tlpMaint.Controls.Add(this._nCompressDays, 3, 1);
            this.tlpMaint.Controls.Add(this.lblLogDelete, 0, 2);
            this.tlpMaint.Controls.Add(this._cbLogDelete, 1, 2);
            this.tlpMaint.Controls.Add(this.lblDeleteDays, 2, 2);
            this.tlpMaint.Controls.Add(this._nDeleteDays, 3, 2);
            this.tlpMaint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMaint.Location = new System.Drawing.Point(6, 20);
            this.tlpMaint.Margin = new System.Windows.Forms.Padding(0);
            this.tlpMaint.Name = "tlpMaint";
            this.tlpMaint.RowCount = 4;
            this.tlpMaint.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpMaint.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpMaint.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpMaint.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMaint.Size = new System.Drawing.Size(886, 102);
            this.tlpMaint.TabIndex = 0;
            // 
            // lblLogHistory
            // 
            this.lblLogHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLogHistory.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblLogHistory.Location = new System.Drawing.Point(3, 0);
            this.lblLogHistory.Name = "lblLogHistory";
            this.lblLogHistory.Size = new System.Drawing.Size(194, 34);
            this.lblLogHistory.TabIndex = 0;
            this.lblLogHistory.Text = "Log history view (Test)";
            this.lblLogHistory.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbLogHistory
            // 
            this._cbLogHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbLogHistory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbLogHistory.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbLogHistory.Items.AddRange(new object[] {
            "ENABLE",
            "DISABLE"});
            this._cbLogHistory.Location = new System.Drawing.Point(203, 5);
            this._cbLogHistory.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._cbLogHistory.Name = "_cbLogHistory";
            this._cbLogHistory.Size = new System.Drawing.Size(144, 23);
            this._cbLogHistory.TabIndex = 0;
            // 
            // lblLogCompress
            // 
            this.lblLogCompress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLogCompress.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblLogCompress.Location = new System.Drawing.Point(3, 34);
            this.lblLogCompress.Name = "lblLogCompress";
            this.lblLogCompress.Size = new System.Drawing.Size(194, 34);
            this.lblLogCompress.TabIndex = 1;
            this.lblLogCompress.Text = "Log compress";
            this.lblLogCompress.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbLogCompress
            // 
            this._cbLogCompress.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbLogCompress.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbLogCompress.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbLogCompress.Items.AddRange(new object[] {
            "ENABLE",
            "DISABLE"});
            this._cbLogCompress.Location = new System.Drawing.Point(203, 39);
            this._cbLogCompress.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._cbLogCompress.Name = "_cbLogCompress";
            this._cbLogCompress.Size = new System.Drawing.Size(144, 23);
            this._cbLogCompress.TabIndex = 1;
            // 
            // lblCompressDays
            // 
            this.lblCompressDays.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCompressDays.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCompressDays.Location = new System.Drawing.Point(353, 34);
            this.lblCompressDays.Name = "lblCompressDays";
            this.lblCompressDays.Size = new System.Drawing.Size(184, 34);
            this.lblCompressDays.TabIndex = 2;
            this.lblCompressDays.Text = "Compress after (days)";
            this.lblCompressDays.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nCompressDays
            // 
            this._nCompressDays.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nCompressDays.Font = new System.Drawing.Font("Consolas", 10F);
            this._nCompressDays.Location = new System.Drawing.Point(543, 39);
            this._nCompressDays.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._nCompressDays.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this._nCompressDays.Name = "_nCompressDays";
            this._nCompressDays.Size = new System.Drawing.Size(124, 23);
            this._nCompressDays.TabIndex = 2;
            // 
            // lblLogDelete
            // 
            this.lblLogDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLogDelete.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblLogDelete.Location = new System.Drawing.Point(3, 68);
            this.lblLogDelete.Name = "lblLogDelete";
            this.lblLogDelete.Size = new System.Drawing.Size(194, 34);
            this.lblLogDelete.TabIndex = 3;
            this.lblLogDelete.Text = "Delete archived log";
            this.lblLogDelete.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbLogDelete
            // 
            this._cbLogDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbLogDelete.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbLogDelete.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbLogDelete.Items.AddRange(new object[] {
            "ENABLE",
            "DISABLE"});
            this._cbLogDelete.Location = new System.Drawing.Point(203, 73);
            this._cbLogDelete.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._cbLogDelete.Name = "_cbLogDelete";
            this._cbLogDelete.Size = new System.Drawing.Size(144, 23);
            this._cbLogDelete.TabIndex = 3;
            // 
            // lblDeleteDays
            // 
            this.lblDeleteDays.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDeleteDays.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblDeleteDays.Location = new System.Drawing.Point(353, 68);
            this.lblDeleteDays.Name = "lblDeleteDays";
            this.lblDeleteDays.Size = new System.Drawing.Size(184, 34);
            this.lblDeleteDays.TabIndex = 4;
            this.lblDeleteDays.Text = "Delete after (days)";
            this.lblDeleteDays.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _nDeleteDays
            // 
            this._nDeleteDays.Dock = System.Windows.Forms.DockStyle.Fill;
            this._nDeleteDays.Font = new System.Drawing.Font("Consolas", 10F);
            this._nDeleteDays.Location = new System.Drawing.Point(543, 73);
            this._nDeleteDays.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._nDeleteDays.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this._nDeleteDays.Name = "_nDeleteDays";
            this._nDeleteDays.Size = new System.Drawing.Size(124, 23);
            this._nDeleteDays.TabIndex = 4;
            // 
            // grpPaths
            // 
            this.grpPaths.BackColor = System.Drawing.Color.White;
            this.grpPaths.Controls.Add(this.gridLogPaths);
            this.grpPaths.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPaths.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpPaths.Location = new System.Drawing.Point(1, 211);
            this.grpPaths.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.grpPaths.Name = "grpPaths";
            this.grpPaths.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpPaths.Size = new System.Drawing.Size(898, 311);
            this.grpPaths.TabIndex = 1;
            this.grpPaths.TabStop = false;
            this.grpPaths.Text = "LOG FILE PATH";
            //
            // lblPathMode  (LOG MAINTENANCE 최상단 행 좌측)
            //
            this.lblPathMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPathMode.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPathMode.Name = "lblPathMode";
            this.lblPathMode.TabIndex = 10;
            this.lblPathMode.Text = "Log save mode";
            this.lblPathMode.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _cbPathMode
            //
            this._cbPathMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbPathMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbPathMode.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbPathMode.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._cbPathMode.Name = "_cbPathMode";
            this._cbPathMode.TabIndex = 11;
            // 
            // gridLogPaths
            // 
            this.gridLogPaths.AllowUserToAddRows = false;
            this.gridLogPaths.AllowUserToDeleteRows = false;
            this.gridLogPaths.AllowUserToResizeRows = false;
            this.gridLogPaths.BackgroundColor = System.Drawing.Color.White;
            this.gridLogPaths.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            this.gridLogPaths.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.gridLogPaths.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridLogPaths.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colLogType,
            this.colLogPath,
            this.colLogBrowse});
            this.gridLogPaths.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridLogPaths.EnableHeadersVisualStyles = false;
            this.gridLogPaths.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.gridLogPaths.Location = new System.Drawing.Point(6, 20);
            this.gridLogPaths.Name = "gridLogPaths";
            this.gridLogPaths.RowHeadersVisible = false;
            this.gridLogPaths.RowTemplate.Height = 28;
            this.gridLogPaths.Size = new System.Drawing.Size(886, 308);
            this.gridLogPaths.TabIndex = 0;
            // 
            // colLogType
            // 
            this.colLogType.HeaderText = "LOG TYPE";
            this.colLogType.Name = "colLogType";
            this.colLogType.ReadOnly = true;
            this.colLogType.Width = 200;
            // 
            // colLogPath
            // 
            this.colLogPath.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLogPath.HeaderText = "FILE PATH";
            this.colLogPath.Name = "colLogPath";
            // 
            // colLogBrowse
            // 
            this.colLogBrowse.HeaderText = "";
            this.colLogBrowse.Name = "colLogBrowse";
            this.colLogBrowse.Text = "...";
            this.colLogBrowse.Width = 44;
            // 
            // grpVision
            // 
            this.grpVision.BackColor = System.Drawing.Color.White;
            this.grpVision.Controls.Add(this.tlpVision);
            this.grpVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVision.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.grpVision.Location = new System.Drawing.Point(1, 524);
            this.grpVision.Margin = new System.Windows.Forms.Padding(0);
            this.grpVision.Name = "grpVision";
            this.grpVision.Padding = new System.Windows.Forms.Padding(6, 2, 6, 6);
            this.grpVision.Size = new System.Drawing.Size(898, 175);
            this.grpVision.TabIndex = 2;
            this.grpVision.TabStop = false;
            this.grpVision.Text = "VISION IMAGE (OK / NG)";
            // 
            // tlpVision
            // 
            this.tlpVision.BackColor = System.Drawing.Color.White;
            this.tlpVision.ColumnCount = 3;
            this.tlpVision.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tlpVision.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpVision.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpVision.Controls.Add(this.lblImageFormat, 0, 0);
            this.tlpVision.Controls.Add(this._cbImageFormat, 1, 0);
            this.tlpVision.Controls.Add(this.gridVisionImages, 0, 1);
            this.tlpVision.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpVision.Location = new System.Drawing.Point(6, 20);
            this.tlpVision.Margin = new System.Windows.Forms.Padding(0);
            this.tlpVision.Name = "tlpVision";
            this.tlpVision.RowCount = 2;
            this.tlpVision.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpVision.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpVision.Size = new System.Drawing.Size(886, 124);
            this.tlpVision.TabIndex = 0;
            // 
            // lblImageFormat
            // 
            this.lblImageFormat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblImageFormat.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblImageFormat.Location = new System.Drawing.Point(3, 0);
            this.lblImageFormat.Name = "lblImageFormat";
            this.lblImageFormat.Size = new System.Drawing.Size(194, 34);
            this.lblImageFormat.TabIndex = 0;
            this.lblImageFormat.Text = "Image format";
            this.lblImageFormat.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbImageFormat
            // 
            this._cbImageFormat.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbImageFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbImageFormat.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbImageFormat.Items.AddRange(new object[] {
            "JPG",
            "BMP"});
            this._cbImageFormat.Location = new System.Drawing.Point(203, 5);
            this._cbImageFormat.Margin = new System.Windows.Forms.Padding(3, 5, 3, 4);
            this._cbImageFormat.Name = "_cbImageFormat";
            this._cbImageFormat.Size = new System.Drawing.Size(144, 23);
            this._cbImageFormat.TabIndex = 0;
            // 
            // gridVisionImages
            // 
            this.gridVisionImages.AllowUserToAddRows = false;
            this.gridVisionImages.AllowUserToDeleteRows = false;
            this.gridVisionImages.AllowUserToResizeRows = false;
            this.gridVisionImages.BackgroundColor = System.Drawing.Color.White;
            this.gridVisionImages.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("맑은 고딕", 9F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            this.gridVisionImages.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.gridVisionImages.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridVisionImages.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colVisType,
            this.colVisPath,
            this.colVisBrowse});
            this.tlpVision.SetColumnSpan(this.gridVisionImages, 3);
            this.gridVisionImages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridVisionImages.EnableHeadersVisualStyles = false;
            this.gridVisionImages.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.gridVisionImages.Location = new System.Drawing.Point(3, 36);
            this.gridVisionImages.Margin = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this.gridVisionImages.Name = "gridVisionImages";
            this.gridVisionImages.RowHeadersVisible = false;
            this.gridVisionImages.RowTemplate.Height = 28;
            this.gridVisionImages.Size = new System.Drawing.Size(880, 85);
            this.gridVisionImages.TabIndex = 1;
            // 
            // colVisType
            // 
            this.colVisType.HeaderText = "IMAGE";
            this.colVisType.Name = "colVisType";
            this.colVisType.ReadOnly = true;
            this.colVisType.Width = 200;
            // 
            // colVisPath
            // 
            this.colVisPath.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colVisPath.HeaderText = "IMAGE PATH";
            this.colVisPath.Name = "colVisPath";
            // 
            // colVisBrowse
            // 
            this.colVisBrowse.HeaderText = "";
            this.colVisBrowse.Name = "colVisBrowse";
            this.colVisBrowse.Text = "...";
            this.colVisBrowse.Width = 44;
            // 
            // LogSettingsPanelControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "LogSettingsPanelControl";
            this.Size = new System.Drawing.Size(900, 700);
            this.rootLayout.ResumeLayout(false);
            this.grpMaint.ResumeLayout(false);
            this.tlpMaint.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._nCompressDays)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._nDeleteDays)).EndInit();
            this.grpPaths.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridLogPaths)).EndInit();
            this.grpSnapshot.ResumeLayout(false);
            this.tlpSnapshot.ResumeLayout(false);
            this.grpVision.ResumeLayout(false);
            this.tlpVision.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridVisionImages)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
