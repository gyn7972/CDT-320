using System;
using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    partial class EventLogPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel filterLayout;
        private Label lblDate;
        private DateTimePicker _dp;
        private Label lblRunId;
        private TextBox txtRunId;
        private Label lblSource;
        private TextBox txtSource;
        private Label lblSearch;
        private TextBox txtSearch;
        private CheckBox chkRecentHour;
        private ComboBox cmbLimit;
        private QMC.Common.Ui.Controls.UiStandardButton btnRefresh;
        private QMC.Common.Ui.Controls.UiStandardButton btnOpenFile;
        private QMC.Common.Ui.Controls.UiStandardButton btnLive;
        private DataGridView _grid;

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.filterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblDate = new System.Windows.Forms.Label();
            this._dp = new System.Windows.Forms.DateTimePicker();
            this.lblRunId = new System.Windows.Forms.Label();
            this.txtRunId = new System.Windows.Forms.TextBox();
            this.lblSource = new System.Windows.Forms.Label();
            this.txtSource = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.chkRecentHour = new System.Windows.Forms.CheckBox();
            this.cmbLimit = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new QMC.Common.Ui.Controls.UiStandardButton();
            this.btnOpenFile = new QMC.Common.Ui.Controls.UiStandardButton();
            this.btnLive = new QMC.Common.Ui.Controls.UiStandardButton();
            this._grid = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rootLayout.SuspendLayout();
            this.filterLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.filterLayout, 0, 1);
            this.rootLayout.Controls.Add(this._grid, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Location = new System.Drawing.Point(0, 0);
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1678, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:hist.event";
            this.lblHeader.Text = "이벤트";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblHeader.TextChanged += new System.EventHandler(this.lblHeader_TextChanged);
            // 
            // filterLayout
            // 
            this.filterLayout.BackColor = System.Drawing.Color.White;
            this.filterLayout.ColumnCount = 13;
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 104F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 122F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 136F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.filterLayout.Controls.Add(this.lblDate, 0, 0);
            this.filterLayout.Controls.Add(this._dp, 1, 0);
            this.filterLayout.Controls.Add(this.lblRunId, 2, 0);
            this.filterLayout.Controls.Add(this.txtRunId, 3, 0);
            this.filterLayout.Controls.Add(this.lblSource, 4, 0);
            this.filterLayout.Controls.Add(this.txtSource, 5, 0);
            this.filterLayout.Controls.Add(this.lblSearch, 6, 0);
            this.filterLayout.Controls.Add(this.txtSearch, 7, 0);
            this.filterLayout.Controls.Add(this.chkRecentHour, 8, 0);
            this.filterLayout.Controls.Add(this.cmbLimit, 9, 0);
            this.filterLayout.Controls.Add(this.btnRefresh, 10, 0);
            this.filterLayout.Controls.Add(this.btnOpenFile, 11, 0);
            this.filterLayout.Controls.Add(this.btnLive, 12, 0);
            this.filterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filterLayout.Location = new System.Drawing.Point(0, 30);
            this.filterLayout.Margin = new System.Windows.Forms.Padding(0);
            this.filterLayout.Name = "filterLayout";
            this.filterLayout.Padding = new System.Windows.Forms.Padding(8, 3, 8, 3);
            this.filterLayout.RowCount = 1;
            this.filterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.filterLayout.Size = new System.Drawing.Size(1678, 40);
            this.filterLayout.TabIndex = 1;
            // 
            // lblDate
            // 
            this.lblDate.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(249)))));
            this.lblDate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDate.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.lblDate.Location = new System.Drawing.Point(8, 8);
            this.lblDate.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.lblDate.Name = "lblDate";
            this.lblDate.Padding = new System.Windows.Forms.Padding(5, 0, 3, 0);
            this.lblDate.Size = new System.Drawing.Size(52, 24);
            this.lblDate.TabIndex = 0;
            this.lblDate.Text = "DATE";
            this.lblDate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dp
            // 
            this._dp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this._dp.Font = new System.Drawing.Font("Consolas", 10F);
            this._dp.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._dp.Location = new System.Drawing.Point(77, 8);
            this._dp.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._dp.Name = "_dp";
            this._dp.Size = new System.Drawing.Size(164, 23);
            this._dp.TabIndex = 1;
            this._dp.ValueChanged += new System.EventHandler(this.dp_ValueChanged);
            // 
            // lblRunId
            // 
            this.lblRunId.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblRunId.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(249)))));
            this.lblRunId.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblRunId.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblRunId.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.lblRunId.Location = new System.Drawing.Point(244, 8);
            this.lblRunId.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.lblRunId.Name = "lblRunId";
            this.lblRunId.Padding = new System.Windows.Forms.Padding(5, 0, 3, 0);
            this.lblRunId.Size = new System.Drawing.Size(56, 24);
            this.lblRunId.TabIndex = 4;
            this.lblRunId.Text = "RunId";
            this.lblRunId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtRunId
            // 
            this.txtRunId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRunId.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtRunId.Location = new System.Drawing.Point(323, 8);
            this.txtRunId.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.txtRunId.Name = "txtRunId";
            this.txtRunId.Size = new System.Drawing.Size(214, 23);
            this.txtRunId.TabIndex = 5;
            this.txtRunId.TextChanged += new System.EventHandler(this.FilterTextBox_TextChanged);
            // 
            // lblSource
            // 
            this.lblSource.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSource.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(249)))));
            this.lblSource.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSource.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblSource.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.lblSource.Location = new System.Drawing.Point(540, 8);
            this.lblSource.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.lblSource.Name = "lblSource";
            this.lblSource.Padding = new System.Windows.Forms.Padding(5, 0, 3, 0);
            this.lblSource.Size = new System.Drawing.Size(64, 24);
            this.lblSource.TabIndex = 6;
            this.lblSource.Text = "Source";
            this.lblSource.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtSource
            // 
            this.txtSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSource.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtSource.Location = new System.Drawing.Point(619, 8);
            this.txtSource.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.txtSource.Name = "txtSource";
            this.txtSource.Size = new System.Drawing.Size(144, 23);
            this.txtSource.TabIndex = 7;
            this.txtSource.TextChanged += new System.EventHandler(this.FilterTextBox_TextChanged);
            // 
            // lblSearch
            // 
            this.lblSearch.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(249)))));
            this.lblSearch.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblSearch.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.lblSearch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(45)))), ((int)(((byte)(57)))));
            this.lblSearch.Location = new System.Drawing.Point(766, 8);
            this.lblSearch.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Padding = new System.Windows.Forms.Padding(5, 0, 3, 0);
            this.lblSearch.Size = new System.Drawing.Size(66, 24);
            this.lblSearch.TabIndex = 8;
            this.lblSearch.Text = "Search";
            this.lblSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtSearch.Location = new System.Drawing.Point(851, 8);
            this.txtSearch.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(282, 23);
            this.txtSearch.TabIndex = 9;
            this.txtSearch.TextChanged += new System.EventHandler(this.FilterTextBox_TextChanged);
            // 
            // chkRecentHour
            // 
            this.chkRecentHour.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkRecentHour.AutoSize = true;
            this.chkRecentHour.BackColor = System.Drawing.Color.White;
            this.chkRecentHour.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.chkRecentHour.Location = new System.Drawing.Point(1139, 10);
            this.chkRecentHour.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.chkRecentHour.Name = "chkRecentHour";
            this.chkRecentHour.Size = new System.Drawing.Size(87, 19);
            this.chkRecentHour.TabIndex = 10;
            this.chkRecentHour.Text = "Last 1 hour";
            this.chkRecentHour.UseVisualStyleBackColor = true;
            this.chkRecentHour.CheckedChanged += new System.EventHandler(this.chkRecentHour_CheckedChanged);
            // 
            // cmbLimit
            // 
            this.cmbLimit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbLimit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLimit.Font = new System.Drawing.Font("Consolas", 10F);
            this.cmbLimit.FormattingEnabled = true;
            this.cmbLimit.Items.AddRange(new object[] {
            "500",
            "2000",
            "ALL"});
            this.cmbLimit.Location = new System.Drawing.Point(1243, 8);
            this.cmbLimit.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.cmbLimit.Name = "cmbLimit";
            this.cmbLimit.Size = new System.Drawing.Size(70, 23);
            this.cmbLimit.TabIndex = 11;
            this.cmbLimit.SelectedIndexChanged += new System.EventHandler(this.cmbLimit_SelectedIndexChanged);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.Location = new System.Drawing.Point(1320, 8);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnRefresh.MinimumSize = new System.Drawing.Size(116, 24);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnRefresh.Role = QMC.Common.Ui.Controls.UiStandardButtonRole.Primary;
            this.btnRefresh.Size = new System.Drawing.Size(116, 24);
            this.btnRefresh.TabIndex = 12;
            this.btnRefresh.Text = "REFRESH";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnOpenFile
            // 
            this.btnOpenFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenFile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOpenFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenFile.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnOpenFile.Location = new System.Drawing.Point(1442, 8);
            this.btnOpenFile.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnOpenFile.MinimumSize = new System.Drawing.Size(128, 24);
            this.btnOpenFile.Name = "btnOpenFile";
            this.btnOpenFile.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnOpenFile.Size = new System.Drawing.Size(128, 24);
            this.btnOpenFile.TabIndex = 13;
            this.btnOpenFile.Text = "OPEN FILE";
            this.btnOpenFile.UseVisualStyleBackColor = false;
            this.btnOpenFile.Click += new System.EventHandler(this.btnOpenFile_Click);
            // 
            // btnLive
            // 
            this.btnLive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLive.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLive.FlatAppearance.BorderSize = 0;
            this.btnLive.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLive.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnLive.Location = new System.Drawing.Point(1578, 8);
            this.btnLive.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.btnLive.Name = "btnLive";
            this.btnLive.Role = QMC.Common.Ui.Controls.UiStandardButtonRole.Dark;
            this.btnLive.Size = new System.Drawing.Size(88, 24);
            this.btnLive.TabIndex = 14;
            this.btnLive.Text = "실시간";
            this.btnLive.UseVisualStyleBackColor = false;
            this.btnLive.Click += new System.EventHandler(this.btnLive_Click);
            // 
            // _grid
            // 
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToResizeRows = false;
            this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Consolas", 10F);
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this._grid.ColumnHeadersHeight = 29;
            this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewTextBoxColumn6,
            this.dataGridViewTextBoxColumn5});
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.Font = new System.Drawing.Font("Consolas", 10F);
            this._grid.Location = new System.Drawing.Point(3, 73);
            this._grid.Name = "_grid";
            this._grid.ReadOnly = true;
            this._grid.RowHeadersVisible = false;
            this._grid.RowHeadersWidth = 51;
            this._grid.RowTemplate.Height = 26;
            this._grid.Size = new System.Drawing.Size(1672, 824);
            this._grid.TabIndex = 2;
            this._grid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Grid_CellClick);
            this._grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Grid_CellDoubleClick);
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle8;
            this.dataGridViewTextBoxColumn1.FillWeight = 175F;
            this.dataGridViewTextBoxColumn1.HeaderText = "DATE";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 200;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle9;
            this.dataGridViewTextBoxColumn2.FillWeight = 75F;
            this.dataGridViewTextBoxColumn2.HeaderText = "KIND";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle10;
            this.dataGridViewTextBoxColumn3.FillWeight = 75F;
            this.dataGridViewTextBoxColumn3.HeaderText = "ACTOR";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle11;
            this.dataGridViewTextBoxColumn4.FillWeight = 190F;
            this.dataGridViewTextBoxColumn4.HeaderText = "CODE";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle12;
            this.dataGridViewTextBoxColumn6.FillWeight = 170F;
            this.dataGridViewTextBoxColumn6.HeaderText = "SOURCE";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.FillWeight = 460F;
            this.dataGridViewTextBoxColumn5.HeaderText = "DESCRIPTION";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // EventLogPage
            // 
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "EventLogPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.Load += new System.EventHandler(this.EventLogPage_Load);
            this.rootLayout.ResumeLayout(false);
            this.filterLayout.ResumeLayout(false);
            this.filterLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this.ResumeLayout(false);

        }

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
    }
}

