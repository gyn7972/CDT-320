using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class PickerView
    {
        private System.ComponentModel.IContainer components = null;

        private VisionImageView _single;
        private TableLayoutPanel _channelHost;
        private Panel _cell0, _cell1, _cell2, _cell3;
        private Label _lbl0, _lbl1, _lbl2, _lbl3;
        private VisionImageView _ch0, _ch1, _ch2, _ch3;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._single = new VisionImageView();
            this._channelHost = new TableLayoutPanel();
            this._cell0 = new Panel();
            this._cell1 = new Panel();
            this._cell2 = new Panel();
            this._cell3 = new Panel();
            this._lbl0 = new Label();
            this._lbl1 = new Label();
            this._lbl2 = new Label();
            this._lbl3 = new Label();
            this._ch0 = new VisionImageView();
            this._ch1 = new VisionImageView();
            this._ch2 = new VisionImageView();
            this._ch3 = new VisionImageView();
            this.SuspendLayout();

            // _single
            this._single.Dock = DockStyle.Fill;

            // _channelHost
            this._channelHost.Dock = DockStyle.Fill;
            this._channelHost.BackColor = Color.FromArgb(0x1A, 0x1A, 0x1E);
            this._channelHost.ColumnCount = 1;
            this._channelHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this._channelHost.RowCount = 4;
            this._channelHost.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            this._channelHost.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            this._channelHost.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            this._channelHost.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            this._channelHost.Visible = false;

            // _cell0 (Front channel 1)
            this._cell0.Dock = DockStyle.Fill;
            this._cell0.Margin = new Padding(0, 0, 0, 2);
            this._cell0.BackColor = Color.FromArgb(0x14, 0x14, 0x17);
            this._lbl0.Dock = DockStyle.Top;
            this._lbl0.Height = 14;
            this._lbl0.Text = "Front channel 1";
            this._lbl0.ForeColor = Color.Gainsboro;
            this._lbl0.Font = new Font("Segoe UI", 7.5F);
            this._lbl0.Padding = new Padding(4, 1, 0, 0);
            this._ch0.Dock = DockStyle.Fill;
            this._cell0.Controls.Add(this._ch0);
            this._cell0.Controls.Add(this._lbl0);
            this._channelHost.Controls.Add(this._cell0, 0, 0);

            // _cell1 (Front channel 2)
            this._cell1.Dock = DockStyle.Fill;
            this._cell1.Margin = new Padding(0, 0, 0, 2);
            this._cell1.BackColor = Color.FromArgb(0x14, 0x14, 0x17);
            this._lbl1.Dock = DockStyle.Top;
            this._lbl1.Height = 14;
            this._lbl1.Text = "Front channel 2";
            this._lbl1.ForeColor = Color.Gainsboro;
            this._lbl1.Font = new Font("Segoe UI", 7.5F);
            this._lbl1.Padding = new Padding(4, 1, 0, 0);
            this._ch1.Dock = DockStyle.Fill;
            this._cell1.Controls.Add(this._ch1);
            this._cell1.Controls.Add(this._lbl1);
            this._channelHost.Controls.Add(this._cell1, 0, 1);

            // _cell2 (Back channel 1)
            this._cell2.Dock = DockStyle.Fill;
            this._cell2.Margin = new Padding(0, 0, 0, 2);
            this._cell2.BackColor = Color.FromArgb(0x14, 0x14, 0x17);
            this._lbl2.Dock = DockStyle.Top;
            this._lbl2.Height = 14;
            this._lbl2.Text = "Back channel 1";
            this._lbl2.ForeColor = Color.Gainsboro;
            this._lbl2.Font = new Font("Segoe UI", 7.5F);
            this._lbl2.Padding = new Padding(4, 1, 0, 0);
            this._ch2.Dock = DockStyle.Fill;
            this._cell2.Controls.Add(this._ch2);
            this._cell2.Controls.Add(this._lbl2);
            this._channelHost.Controls.Add(this._cell2, 0, 2);

            // _cell3 (Back channel 2)
            this._cell3.Dock = DockStyle.Fill;
            this._cell3.Margin = new Padding(0, 0, 0, 2);
            this._cell3.BackColor = Color.FromArgb(0x14, 0x14, 0x17);
            this._lbl3.Dock = DockStyle.Top;
            this._lbl3.Height = 14;
            this._lbl3.Text = "Back channel 2";
            this._lbl3.ForeColor = Color.Gainsboro;
            this._lbl3.Font = new Font("Segoe UI", 7.5F);
            this._lbl3.Padding = new Padding(4, 1, 0, 0);
            this._ch3.Dock = DockStyle.Fill;
            this._cell3.Controls.Add(this._ch3);
            this._cell3.Controls.Add(this._lbl3);
            this._channelHost.Controls.Add(this._cell3, 0, 3);

            // PickerView
            this.BackColor = Color.FromArgb(0x1A, 0x1A, 0x1E);
            this.Controls.Add(this._single);
            this.Controls.Add(this._channelHost);
            this.Name = "PickerView";
            this.ResumeLayout(false);
        }
    }
}
