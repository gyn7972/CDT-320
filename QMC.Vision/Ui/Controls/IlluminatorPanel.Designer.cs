using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class IlluminatorPanel
    {
        private System.ComponentModel.IContainer components = null;

        private Label    _hdr;
        private Label    _lblCh1, _lblCh2, _lblCh3, _lblCh4;
        private TrackBar _tb1, _tb2, _tb3, _tb4;
        private Label    _val1, _val2, _val3, _val4;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._hdr = new Label();
            this._lblCh1 = new Label(); this._tb1 = new TrackBar(); this._val1 = new Label();
            this._lblCh2 = new Label(); this._tb2 = new TrackBar(); this._val2 = new Label();
            this._lblCh3 = new Label(); this._tb3 = new TrackBar(); this._val3 = new Label();
            this._lblCh4 = new Label(); this._tb4 = new TrackBar(); this._val4 = new Label();
            ((ISupportInitialize)this._tb1).BeginInit();
            ((ISupportInitialize)this._tb2).BeginInit();
            ((ISupportInitialize)this._tb3).BeginInit();
            ((ISupportInitialize)this._tb4).BeginInit();
            this.SuspendLayout();

            // _hdr
            this._hdr.Dock = DockStyle.Top;
            this._hdr.Height = 26;
            this._hdr.Text = "Illuminator";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // CH 1 (y=30)
            this._lblCh1.Location = new Point(6, 34); this._lblCh1.Size = new Size(40, 20); this._lblCh1.Text = "CH 1"; this._lblCh1.Font = UiTheme.ButtonFont;
            this._tb1.Location = new Point(50, 26); this._tb1.Size = new Size(180, 30); this._tb1.Minimum = 0; this._tb1.Maximum = 255; this._tb1.Value = 128; this._tb1.TickFrequency = 32;
            this._tb1.ValueChanged += new System.EventHandler(this.OnTrackValueChanged);
            this._val1.Location = new Point(234, 34); this._val1.Size = new Size(40, 20); this._val1.Text = "128"; this._val1.Font = UiTheme.ValueFont; this._val1.TextAlign = ContentAlignment.MiddleRight;

            // CH 2 (y=62)
            this._lblCh2.Location = new Point(6, 66); this._lblCh2.Size = new Size(40, 20); this._lblCh2.Text = "CH 2"; this._lblCh2.Font = UiTheme.ButtonFont;
            this._tb2.Location = new Point(50, 58); this._tb2.Size = new Size(180, 30); this._tb2.Minimum = 0; this._tb2.Maximum = 255; this._tb2.Value = 128; this._tb2.TickFrequency = 32;
            this._tb2.ValueChanged += new System.EventHandler(this.OnTrackValueChanged);
            this._val2.Location = new Point(234, 66); this._val2.Size = new Size(40, 20); this._val2.Text = "128"; this._val2.Font = UiTheme.ValueFont; this._val2.TextAlign = ContentAlignment.MiddleRight;

            // CH 3 (y=94)
            this._lblCh3.Location = new Point(6, 98); this._lblCh3.Size = new Size(40, 20); this._lblCh3.Text = "CH 3"; this._lblCh3.Font = UiTheme.ButtonFont;
            this._tb3.Location = new Point(50, 90); this._tb3.Size = new Size(180, 30); this._tb3.Minimum = 0; this._tb3.Maximum = 255; this._tb3.Value = 128; this._tb3.TickFrequency = 32;
            this._tb3.ValueChanged += new System.EventHandler(this.OnTrackValueChanged);
            this._val3.Location = new Point(234, 98); this._val3.Size = new Size(40, 20); this._val3.Text = "128"; this._val3.Font = UiTheme.ValueFont; this._val3.TextAlign = ContentAlignment.MiddleRight;

            // CH 4 (y=126)
            this._lblCh4.Location = new Point(6, 130); this._lblCh4.Size = new Size(40, 20); this._lblCh4.Text = "CH 4"; this._lblCh4.Font = UiTheme.ButtonFont;
            this._tb4.Location = new Point(50, 122); this._tb4.Size = new Size(180, 30); this._tb4.Minimum = 0; this._tb4.Maximum = 255; this._tb4.Value = 128; this._tb4.TickFrequency = 32;
            this._tb4.ValueChanged += new System.EventHandler(this.OnTrackValueChanged);
            this._val4.Location = new Point(234, 130); this._val4.Size = new Size(40, 20); this._val4.Text = "128"; this._val4.Font = UiTheme.ValueFont; this._val4.TextAlign = ContentAlignment.MiddleRight;

            // IlluminatorPanel
            this.BackColor = UiTheme.OptionPanelBg;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Height = 180;
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._lblCh1); this.Controls.Add(this._tb1); this.Controls.Add(this._val1);
            this.Controls.Add(this._lblCh2); this.Controls.Add(this._tb2); this.Controls.Add(this._val2);
            this.Controls.Add(this._lblCh3); this.Controls.Add(this._tb3); this.Controls.Add(this._val3);
            this.Controls.Add(this._lblCh4); this.Controls.Add(this._tb4); this.Controls.Add(this._val4);
            this.Name = "IlluminatorPanel";
            ((ISupportInitialize)this._tb1).EndInit();
            ((ISupportInitialize)this._tb2).EndInit();
            ((ISupportInitialize)this._tb3).EndInit();
            ((ISupportInitialize)this._tb4).EndInit();
            this.ResumeLayout(false);
        }
    }
}
