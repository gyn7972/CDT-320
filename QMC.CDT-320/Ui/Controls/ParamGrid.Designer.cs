namespace QMC.CDT_320.Ui.Controls
{
    partial class ParamGrid
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel _layout;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._layout = new System.Windows.Forms.TableLayoutPanel();
            this.SuspendLayout();
            // 
            // _layout
            // 
            this._layout.BackColor = System.Drawing.Color.Transparent;
            this._layout.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.None;
            this._layout.ColumnCount = 1;
            this._layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._layout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._layout.Location = new System.Drawing.Point(6, 6);
            this._layout.Name = "_layout";
            this._layout.RowCount = 1;
            this._layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._layout.Size = new System.Drawing.Size(388, 188);
            this._layout.TabIndex = 0;
            // 
            // ParamGrid
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this._layout);
            this.Font = new System.Drawing.Font("?? ??", 9F);
            this.Name = "ParamGrid";
            this.Padding = new System.Windows.Forms.Padding(6);
            this.Size = new System.Drawing.Size(400, 200);
            this.ResumeLayout(false);
        }
    }
}
