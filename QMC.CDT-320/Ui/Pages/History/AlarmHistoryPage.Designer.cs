using System.Drawing;
using System.Windows.Forms;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Pages.History
{
    partial class AlarmHistoryPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblHeader;
        private TableLayoutPanel filterLayout;
        private Label lblSeverity;
        private ComboBox _cbSeverity;
        private Label lblSearch;
        private TextBox _tbFilter;
        private Label _lblCount;
        private Button btnClear;
        private DataGridView _grid;

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.filterLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblSeverity = new System.Windows.Forms.Label();
            this._cbSeverity = new System.Windows.Forms.ComboBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this._tbFilter = new System.Windows.Forms.TextBox();
            this._lblCount = new System.Windows.Forms.Label();
            this.btnClear = new System.Windows.Forms.Button();
            this._grid = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.rootLayout.SuspendLayout();
            this.filterLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.filterLayout, 0, 1);
            this.rootLayout.Controls.Add(this._grid, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
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
            this.lblHeader.Location = new System.Drawing.Point(3, 0);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblHeader.Size = new System.Drawing.Size(1672, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "i18n:hist.alarm";
            this.lblHeader.Text = "알람";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // filterLayout
            // 
            this.filterLayout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.filterLayout.ColumnCount = 7;
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 66F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 290F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.filterLayout.Controls.Add(this.lblSeverity, 0, 0);
            this.filterLayout.Controls.Add(this._cbSeverity, 1, 0);
            this.filterLayout.Controls.Add(this.lblSearch, 2, 0);
            this.filterLayout.Controls.Add(this._tbFilter, 3, 0);
            this.filterLayout.Controls.Add(this._lblCount, 4, 0);
            this.filterLayout.Controls.Add(this.btnClear, 5, 0);
            this.filterLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filterLayout.Location = new System.Drawing.Point(3, 33);
            this.filterLayout.Name = "filterLayout";
            this.filterLayout.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.filterLayout.RowCount = 1;
            this.filterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.filterLayout.Size = new System.Drawing.Size(1672, 44);
            this.filterLayout.TabIndex = 1;
            // 
            // lblSeverity
            // 
            this.lblSeverity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSeverity.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSeverity.Location = new System.Drawing.Point(11, 6);
            this.lblSeverity.Name = "lblSeverity";
            this.lblSeverity.Size = new System.Drawing.Size(70, 32);
            this.lblSeverity.TabIndex = 0;
            this.lblSeverity.Text = "Severity:";
            this.lblSeverity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cbSeverity
            // 
            this._cbSeverity.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cbSeverity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbSeverity.Font = new System.Drawing.Font("Consolas", 10F);
            this._cbSeverity.Location = new System.Drawing.Point(87, 9);
            this._cbSeverity.Name = "_cbSeverity";
            this._cbSeverity.Size = new System.Drawing.Size(144, 23);
            this._cbSeverity.TabIndex = 1;
            // 
            // lblSearch
            // 
            this.lblSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSearch.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblSearch.Location = new System.Drawing.Point(237, 6);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(60, 32);
            this.lblSearch.TabIndex = 2;
            this.lblSearch.Text = "Search:";
            this.lblSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tbFilter
            // 
            this._tbFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tbFilter.Font = new System.Drawing.Font("Consolas", 10F);
            this._tbFilter.Location = new System.Drawing.Point(303, 9);
            this._tbFilter.Name = "_tbFilter";
            this._tbFilter.Size = new System.Drawing.Size(284, 23);
            this._tbFilter.TabIndex = 3;
            // 
            // _lblCount
            // 
            this._lblCount.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblCount.Font = new System.Drawing.Font("Consolas", 10F);
            this._lblCount.Location = new System.Drawing.Point(593, 6);
            this._lblCount.Name = "_lblCount";
            this._lblCount.Size = new System.Drawing.Size(94, 32);
            this._lblCount.TabIndex = 4;
            this._lblCount.Text = "(0)";
            this._lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(93)))), ((int)(((byte)(26)))));
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(76)))), ((int)(((byte)(18)))));
            this.btnClear.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(120)))), ((int)(((byte)(55)))));
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Location = new System.Drawing.Point(693, 9);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(154, 28);
            this.btnClear.TabIndex = 5;
            this.btnClear.Text = "Clear active alarms";
            this.btnClear.UseVisualStyleBackColor = false;
            // 
            // _grid
            // 
            this._grid.AllowUserToAddRows = false;
            this._grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._grid.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Consolas", 10F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this._grid.ColumnHeadersHeight = 29;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewTextBoxColumn5,
            this.dataGridViewTextBoxColumn6,
            this.dataGridViewTextBoxColumn7});
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.EnableHeadersVisualStyles = false;
            this._grid.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
            this._grid.Location = new System.Drawing.Point(3, 83);
            this._grid.Name = "_grid";
            this._grid.ReadOnly = true;
            this._grid.RowHeadersVisible = false;
            this._grid.RowHeadersWidth = 51;
            this._grid.Size = new System.Drawing.Size(1672, 814);
            this._grid.TabIndex = 2;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewTextBoxColumn1.FillWeight = 95F;
            this.dataGridViewTextBoxColumn1.HeaderText = "Time";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 110;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // dataGridViewTextBoxColumn2
            //
            this.dataGridViewTextBoxColumn2.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewTextBoxColumn2.FillWeight = 85F;
            this.dataGridViewTextBoxColumn2.HeaderText = "Severity";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 75;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // dataGridViewTextBoxColumn3
            //
            this.dataGridViewTextBoxColumn3.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewTextBoxColumn3.FillWeight = 150F;
            this.dataGridViewTextBoxColumn3.HeaderText = "Code";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 90;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // dataGridViewTextBoxColumn4
            //
            this.dataGridViewTextBoxColumn4.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dataGridViewTextBoxColumn4.FillWeight = 150F;
            this.dataGridViewTextBoxColumn4.HeaderText = "Source";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 90;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // dataGridViewTextBoxColumn5
            //
            this.dataGridViewTextBoxColumn5.FillWeight = 280F;
            this.dataGridViewTextBoxColumn5.HeaderText = "Message";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // dataGridViewTextBoxColumn6
            //
            this.dataGridViewTextBoxColumn6.FillWeight = 190F;
            this.dataGridViewTextBoxColumn6.HeaderText = "Cause";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // dataGridViewTextBoxColumn7
            //
            this.dataGridViewTextBoxColumn7.FillWeight = 190F;
            this.dataGridViewTextBoxColumn7.HeaderText = "Action";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // AlarmHistoryPage
            // 
            this.Controls.Add(this.rootLayout);
            this.Name = "AlarmHistoryPage";
            this.Size = new System.Drawing.Size(1678, 900);
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
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
    }
}

