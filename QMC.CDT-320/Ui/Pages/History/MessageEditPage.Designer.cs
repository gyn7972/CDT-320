namespace QMC.CDT_320.Ui.Pages.History
{
    partial class MessageEditPage
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.TableLayoutPanel actionLayout;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKo;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEn;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKind;
        private System.Windows.Forms.DataGridViewCellStyle gridHeaderStyle;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.colCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colKo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colKind = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridHeaderStyle = new System.Windows.Forms.DataGridViewCellStyle();
            this.actionLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();
            this.rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.actionLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // rootLayout
            //
            this.rootLayout.ColumnCount = 1;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.lblHeader, 0, 0);
            this.rootLayout.Controls.Add(this.grid, 0, 1);
            this.rootLayout.Controls.Add(this.actionLayout, 0, 2);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.rootLayout.RowCount = 3;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            //
            // lblHeader
            //
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblHeader.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this.lblHeader.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblHeader.Text = "MESSAGE EDIT";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // grid — 코드/KIND 는 읽기전용, KO/EN 만 인라인 편집
            //
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.ReadOnly = false;
            this.grid.MultiSelect = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.gridHeaderStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.gridHeaderStyle.BackColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.gridHeaderStyle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.gridHeaderStyle.ForeColor = System.Drawing.Color.White;
            this.grid.ColumnHeadersDefaultCellStyle = this.gridHeaderStyle;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.colCode, this.colKind, this.colKo, this.colEn });
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.grid.RowHeadersVisible = false;
            this.grid.RowTemplate.Height = 26;
            //
            // colCode (읽기전용)
            //
            this.colCode.HeaderText = "CODE";
            this.colCode.Name = "CODE";
            this.colCode.ReadOnly = true;
            this.colCode.FillWeight = 90F;
            this.colCode.MinimumWidth = 150;
            this.colCode.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(245, 247, 249);
            this.colCode.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(90, 90, 90);
            this.colCode.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            //
            // colKind (읽기전용)
            //
            this.colKind.HeaderText = "KIND";
            this.colKind.Name = "KIND";
            this.colKind.ReadOnly = true;
            this.colKind.FillWeight = 55F;
            this.colKind.MinimumWidth = 85;
            this.colKind.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.colKind.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(245, 247, 249);
            this.colKind.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(90, 90, 90);
            this.colKind.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            //
            // colKo (편집)
            //
            this.colKo.HeaderText = "DESCRIPTION (KO)";
            this.colKo.Name = "KO";
            this.colKo.ReadOnly = false;
            this.colKo.FillWeight = 400F;
            this.colKo.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            //
            // colEn (편집)
            //
            this.colEn.HeaderText = "DESCRIPTION (EN)";
            this.colEn.Name = "EN";
            this.colEn.ReadOnly = false;
            this.colEn.FillWeight = 400F;
            this.colEn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            //
            // actionLayout — [코드 동기화] [저장]
            //
            this.actionLayout.ColumnCount = 3;
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 168F));
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 128F));
            this.actionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.actionLayout.Controls.Add(this.btnImport, 0, 0);
            this.actionLayout.Controls.Add(this.btnSave, 1, 0);
            this.actionLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actionLayout.Padding = new System.Windows.Forms.Padding(10, 9, 10, 9);
            this.actionLayout.RowCount = 1;
            this.actionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            //
            // btnImport — 코드 동기화 (teal)
            //
            this.btnImport.BackColor = System.Drawing.Color.FromArgb(0, 150, 136);
            this.btnImport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnImport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnImport.FlatAppearance.BorderSize = 0;
            this.btnImport.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 121, 107);
            this.btnImport.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(38, 166, 154);
            this.btnImport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImport.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnImport.ForeColor = System.Drawing.Color.White;
            this.btnImport.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnImport.Name = "btnImport";
            this.btnImport.Text = "REFRESH";
            this.btnImport.UseVisualStyleBackColor = false;
            //
            // btnSave (green)
            //
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(39, 128, 87);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(30, 105, 71);
            this.btnSave.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(52, 148, 104);
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this.btnSave.Text = "SAVE";
            this.btnSave.UseVisualStyleBackColor = false;
            //
            // MessageEditPage
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.rootLayout);
            this.Name = "MessageEditPage";
            this.Size = new System.Drawing.Size(1678, 900);
            this.rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.actionLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
