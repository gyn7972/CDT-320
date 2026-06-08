using System.Drawing;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    partial class MaterialDetailView
    {
        private System.ComponentModel.IContainer components = null;
        private GroupBox grpMaterialDetail;
        private TableLayoutPanel materialLayout;
        private DataGridView gridMaterial;
        private FlowLayoutPanel materialButtonLayout;
        private Button btnCreateData;
        private Button btnClearData;
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
            this.materialLayout = new System.Windows.Forms.TableLayoutPanel();
            this.gridMaterial = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.materialButtonLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCreateData = new System.Windows.Forms.Button();
            this.btnClearData = new System.Windows.Forms.Button();
            this.grpMaterialDetail.SuspendLayout();
            this.materialLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridMaterial)).BeginInit();
            this.materialButtonLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpMaterialDetail
            // 
            this.grpMaterialDetail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.grpMaterialDetail.Controls.Add(this.materialLayout);
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
            // materialLayout
            // 
            this.materialLayout.ColumnCount = 1;
            this.materialLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialLayout.Controls.Add(this.gridMaterial, 0, 0);
            this.materialLayout.Controls.Add(this.materialButtonLayout, 0, 1);
            this.materialLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialLayout.Location = new System.Drawing.Point(8, 28);
            this.materialLayout.Name = "materialLayout";
            this.materialLayout.RowCount = 2;
            this.materialLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.materialLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.materialLayout.Size = new System.Drawing.Size(404, 484);
            this.materialLayout.TabIndex = 0;
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
            this.gridMaterial.Location = new System.Drawing.Point(3, 3);
            this.gridMaterial.MultiSelect = false;
            this.gridMaterial.Name = "gridMaterial";
            this.gridMaterial.ReadOnly = true;
            this.gridMaterial.RowHeadersVisible = false;
            this.gridMaterial.RowTemplate.Height = 30;
            this.gridMaterial.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridMaterial.Size = new System.Drawing.Size(398, 434);
            this.gridMaterial.TabIndex = 0;
            this.gridMaterial.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridMaterial_CellDoubleClick);
            // 
            // materialButtonLayout
            // 
            this.materialButtonLayout.Controls.Add(this.btnCreateData);
            this.materialButtonLayout.Controls.Add(this.btnClearData);
            this.materialButtonLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.materialButtonLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.materialButtonLayout.Location = new System.Drawing.Point(3, 443);
            this.materialButtonLayout.Name = "materialButtonLayout";
            this.materialButtonLayout.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.materialButtonLayout.Size = new System.Drawing.Size(398, 38);
            this.materialButtonLayout.TabIndex = 1;
            this.materialButtonLayout.WrapContents = false;
            // 
            // btnCreateData
            // 
            this.btnCreateData.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.btnCreateData.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCreateData.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCreateData.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCreateData.ForeColor = System.Drawing.Color.White;
            this.btnCreateData.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnCreateData.Name = "btnCreateData";
            this.btnCreateData.Size = new System.Drawing.Size(110, 30);
            this.btnCreateData.TabIndex = 0;
            this.btnCreateData.Text = "DATA CREATE";
            this.btnCreateData.UseVisualStyleBackColor = false;
            this.btnCreateData.Click += new System.EventHandler(this.btnCreateData_Click);
            // 
            // btnClearData
            // 
            this.btnClearData.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.btnClearData.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClearData.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearData.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnClearData.ForeColor = System.Drawing.Color.White;
            this.btnClearData.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnClearData.Name = "btnClearData";
            this.btnClearData.Size = new System.Drawing.Size(110, 30);
            this.btnClearData.TabIndex = 1;
            this.btnClearData.Text = "DATA CLEAR";
            this.btnClearData.UseVisualStyleBackColor = false;
            this.btnClearData.Click += new System.EventHandler(this.btnClearData_Click);
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
            this.materialLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridMaterial)).EndInit();
            this.materialButtonLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
