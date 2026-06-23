namespace QMC.CDT_320.Ui.Controls
{
    partial class SideVisionViewerControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.TableLayoutPanel _grid;
        private VisionViewerPanel _front;
        private VisionViewerPanel _rear;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._grid = new System.Windows.Forms.TableLayoutPanel();
            this._front = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._rear = new QMC.CDT_320.Ui.Controls.VisionViewerPanel();
            this._grid.SuspendLayout();
            this.SuspendLayout();
            //
            // _grid
            //
            this._grid.ColumnCount = 1;
            this._grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._grid.RowCount = 2;
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Name = "_grid";
            this._grid.Controls.Add(this._front, 0, 0);
            this._grid.Controls.Add(this._rear, 0, 1);
            //
            // _front
            //
            this._front.Dock = System.Windows.Forms.DockStyle.Fill;
            this._front.Margin = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this._front.Name = "_front";
            //
            // _rear
            //
            this._rear.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rear.Margin = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this._rear.Name = "_rear";
            //
            // SideVisionViewerControl
            //
            this.Controls.Add(this._grid);
            this.Name = "SideVisionViewerControl";
            this._grid.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
