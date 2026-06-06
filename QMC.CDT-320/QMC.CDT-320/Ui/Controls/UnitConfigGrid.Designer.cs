namespace QMC.CDT_320.Ui.Controls
{
    partial class UnitConfigGrid
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel layout;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.TableLayoutPanel buttonRow;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnReload;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.layout = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.grid = new System.Windows.Forms.DataGridView();
            this.buttonRow = new System.Windows.Forms.TableLayoutPanel();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnReload = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.layout.SuspendLayout();
            this.buttonRow.SuspendLayout();
            this.SuspendLayout();

            // layout
            this.layout.ColumnCount = 1;
            this.layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layout.Controls.Add(this.lblTitle, 0, 0);
            this.layout.Controls.Add(this.grid, 0, 1);
            this.layout.Controls.Add(this.buttonRow, 0, 2);
            this.layout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layout.RowCount = 3;
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.layout.Margin = new System.Windows.Forms.Padding(0);

            // lblTitle
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.lblTitle.Text = "UNIT CONFIG";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0);

            // grid
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.grid.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);

            var colKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colKey.HeaderText = "Property";
            colKey.FillWeight = 55F;
            colKey.ReadOnly = true;
            var colVal = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colVal.HeaderText = "Value";
            colVal.FillWeight = 45F;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colKey, colVal });

            // buttonRow
            this.buttonRow.ColumnCount = 2;
            this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.buttonRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.buttonRow.Controls.Add(this.btnApply, 0, 0);
            this.buttonRow.Controls.Add(this.btnReload, 1, 0);
            this.buttonRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonRow.RowCount = 1;
            this.buttonRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonRow.Margin = new System.Windows.Forms.Padding(4, 0, 4, 4);

            // btnApply
            this.btnApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApply.Text = "Apply && Save";
            this.btnApply.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);

            // btnReload
            this.btnReload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReload.Text = "Reload";
            this.btnReload.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.btnReload.Click += new System.EventHandler(this.btnReload_Click);

            // UnitConfigGrid
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.layout);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "UnitConfigGrid";
            this.Size = new System.Drawing.Size(320, 700);

            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.layout.ResumeLayout(false);
            this.buttonRow.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}