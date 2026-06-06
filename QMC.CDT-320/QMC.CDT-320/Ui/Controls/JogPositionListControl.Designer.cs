namespace QMC.CDT_320.Ui.Controls
{
    partial class JogPositionListControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAxis;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPosition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAxis2;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPosition2;

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && (components != null))
                    components.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void InitializeComponent()
        {
            this.grid = new System.Windows.Forms.DataGridView();
            this.colAxis = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPosition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAxis2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPosition2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeColumns = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(207)))), ((int)(((byte)(211)))), ((int)(((byte)(216)))));
            this.grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.grid.ColumnHeadersHeight = 29;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.grid.ColumnHeadersVisible = false;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colAxis,
            this.colPosition,
            this.colAxis2,
            this.colPosition2});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.EnableHeadersVisualStyles = false;
            this.grid.Location = new System.Drawing.Point(0, 0);
            this.grid.Margin = new System.Windows.Forms.Padding(0);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.RowHeadersWidth = 51;
            this.grid.RowTemplate.Height = 28;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(343, 40);
            this.grid.TabIndex = 0;
            // 
            // colAxis
            // 
            this.colAxis.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colAxis.FillWeight = 45F;
            this.colAxis.HeaderText = "AXIS";
            this.colAxis.MinimumWidth = 6;
            this.colAxis.Name = "colAxis";
            this.colAxis.ReadOnly = true;
            this.colAxis.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colPosition
            // 
            this.colPosition.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colPosition.FillWeight = 55F;
            this.colPosition.HeaderText = "POSITION";
            this.colPosition.MinimumWidth = 6;
            this.colPosition.Name = "colPosition";
            this.colPosition.ReadOnly = true;
            this.colPosition.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            //
            // colAxis2
            //
            this.colAxis2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colAxis2.FillWeight = 45F;
            this.colAxis2.HeaderText = "AXIS";
            this.colAxis2.MinimumWidth = 6;
            this.colAxis2.Name = "colAxis2";
            this.colAxis2.ReadOnly = true;
            this.colAxis2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colAxis2.Visible = false;
            //
            // colPosition2
            //
            this.colPosition2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colPosition2.FillWeight = 55F;
            this.colPosition2.HeaderText = "POSITION";
            this.colPosition2.MinimumWidth = 6;
            this.colPosition2.Name = "colPosition2";
            this.colPosition2.ReadOnly = true;
            this.colPosition2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colPosition2.Visible = false;
            // 
            // JogPositionListControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grid);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "JogPositionListControl";
            this.Size = new System.Drawing.Size(343, 40);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
