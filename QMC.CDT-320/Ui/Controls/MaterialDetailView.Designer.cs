using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    partial class MaterialDetailView
    {
        private System.ComponentModel.IContainer components = null;
        private GroupBox grpMaterialDetail;
        private DataGridView gridMaterial;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colValue;

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
            this.grpMaterialDetail = new System.Windows.Forms.GroupBox();
            this.gridMaterial = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpMaterialDetail.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridMaterial)).BeginInit();
            this.SuspendLayout();
            // 
            // grpMaterialDetail
            // 
            this.grpMaterialDetail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpMaterialDetail.Controls.Add(this.gridMaterial);
            this.grpMaterialDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpMaterialDetail.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.grpMaterialDetail.Location = new System.Drawing.Point(0, 0);
            this.grpMaterialDetail.Name = "grpMaterialDetail";
            this.grpMaterialDetail.Padding = new System.Windows.Forms.Padding(8);
            this.grpMaterialDetail.Size = new System.Drawing.Size(420, 520);
            this.grpMaterialDetail.TabIndex = 0;
            this.grpMaterialDetail.TabStop = false;
            this.grpMaterialDetail.Text = "MATERIAL";
            // 
            // gridMaterial
            // 
            this.gridMaterial.AllowUserToAddRows = false;
            this.gridMaterial.AllowUserToDeleteRows = false;
            this.gridMaterial.AllowUserToResizeRows = false;
            this.gridMaterial.BackgroundColor = System.Drawing.Color.White;
            this.gridMaterial.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.gridMaterial.ColumnHeadersHeight = 30;
            this.gridMaterial.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.gridMaterial.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colName,
            this.colValue});
            this.gridMaterial.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridMaterial.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.gridMaterial.EnableHeadersVisualStyles = false;
            this.gridMaterial.GridColor = System.Drawing.Color.Gainsboro;
            this.gridMaterial.Location = new System.Drawing.Point(8, 28);
            this.gridMaterial.MultiSelect = false;
            this.gridMaterial.Name = "gridMaterial";
            this.gridMaterial.ReadOnly = true;
            this.gridMaterial.RowHeadersVisible = false;
            this.gridMaterial.RowTemplate.Height = 30;
            this.gridMaterial.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridMaterial.Size = new System.Drawing.Size(404, 484);
            this.gridMaterial.TabIndex = 0;
            this.gridMaterial.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridMaterial_CellDoubleClick);
            // 
            // colName
            // 
            this.colName.HeaderText = "Property";
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            this.colName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colName.Width = 130;
            // 
            // colValue
            // 
            this.colValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colValue.HeaderText = "Value";
            this.colValue.Name = "colValue";
            this.colValue.ReadOnly = true;
            this.colValue.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // MaterialDetailView
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.Controls.Add(this.grpMaterialDetail);
            this.Name = "MaterialDetailView";
            this.Size = new System.Drawing.Size(420, 520);
            this.grpMaterialDetail.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridMaterial)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
