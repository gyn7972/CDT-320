namespace QMC.CDT_320.Ui.Dialogs
{
    partial class AxisPositionPopup
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView listViewAxis;
        private System.Windows.Forms.ColumnHeader colAxisNo;
        private System.Windows.Forms.ColumnHeader colAxisName;
        private System.Windows.Forms.ColumnHeader colPosition;
        private System.Windows.Forms.ColumnHeader colUnit;
        private System.Windows.Forms.ImageList imageListAxisRows;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.listViewAxis = new System.Windows.Forms.ListView();
            this.colAxisNo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colAxisName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPosition = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUnit = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageListAxisRows = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // listViewAxis
            // 
            this.listViewAxis.BackColor = System.Drawing.Color.Black;
            this.listViewAxis.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colAxisNo,
            this.colAxisName,
            this.colPosition,
            this.colUnit});
            this.listViewAxis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewAxis.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.listViewAxis.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(220)))), ((int)(((byte)(130)))));
            this.listViewAxis.FullRowSelect = true;
            this.listViewAxis.HideSelection = false;
            this.listViewAxis.Location = new System.Drawing.Point(0, 0);
            this.listViewAxis.Name = "listViewAxis";
            this.listViewAxis.OwnerDraw = true;
            this.listViewAxis.Size = new System.Drawing.Size(407, 523);
            this.listViewAxis.SmallImageList = this.imageListAxisRows;
            this.listViewAxis.TabIndex = 0;
            this.listViewAxis.UseCompatibleStateImageBehavior = false;
            this.listViewAxis.View = System.Windows.Forms.View.Details;
            // 
            // colAxisNo
            // 
            this.colAxisNo.Text = "NO";
            this.colAxisNo.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colAxisNo.Width = 44;
            // 
            // colAxisName
            // 
            this.colAxisName.Text = "Axis Name";
            this.colAxisName.Width = 178;
            // 
            // colPosition
            // 
            this.colPosition.Text = "Position";
            this.colPosition.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colPosition.Width = 110;
            // 
            // colUnit
            // 
            this.colUnit.Text = "Unit";
            this.colUnit.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.colUnit.Width = 48;
            // 
            // imageListAxisRows
            // 
            this.imageListAxisRows.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageListAxisRows.ImageSize = new System.Drawing.Size(1, 20);
            this.imageListAxisRows.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // AxisPositionPopup
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(407, 523);
            this.Controls.Add(this.listViewAxis);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.MinimumSize = new System.Drawing.Size(360, 380);
            this.Name = "AxisPositionPopup";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Axis Position Monitor";
            this.ResumeLayout(false);

        }
    }
}
