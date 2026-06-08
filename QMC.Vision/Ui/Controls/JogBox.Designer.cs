using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class JogBox
    {
        private System.ComponentModel.IContainer components = null;

        private Label    _hdr;
        private Button   _btnUp;
        private Button   _btnDown;
        private Button   _btnLeft;
        private Button   _btnRight;
        private Button   _btnCw;
        private Button   _btnCcw;
        private ComboBox _cmbSpeed;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._hdr = new Label();
            this._btnUp = new Button();
            this._btnDown = new Button();
            this._btnLeft = new Button();
            this._btnRight = new Button();
            this._btnCw = new Button();
            this._btnCcw = new Button();
            this._cmbSpeed = new ComboBox();
            this.SuspendLayout();

            // _hdr
            this._hdr.Dock = DockStyle.Top;
            this._hdr.Height = 26;
            this._hdr.Text = "Jog Box";
            this._hdr.BackColor = UiTheme.StatusBarBg;
            this._hdr.ForeColor = Color.White;
            this._hdr.Font = UiTheme.SectionFont;
            this._hdr.TextAlign = ContentAlignment.MiddleLeft;
            this._hdr.Padding = new Padding(10, 0, 0, 0);

            // _btnUp
            this._btnUp.Location = new Point(100, 32);
            this._btnUp.Size = new Size(54, 54);
            this._btnUp.Text = "▲";
            this._btnUp.Font = new Font("맑은 고딕", 15F);
            this._btnUp.FlatStyle = FlatStyle.Flat;

            // _btnDown
            this._btnDown.Location = new Point(100, 150);
            this._btnDown.Size = new Size(54, 54);
            this._btnDown.Text = "▼";
            this._btnDown.Font = new Font("맑은 고딕", 15F);
            this._btnDown.FlatStyle = FlatStyle.Flat;

            // _btnLeft
            this._btnLeft.Location = new Point(30, 91);
            this._btnLeft.Size = new Size(54, 54);
            this._btnLeft.Text = "◀";
            this._btnLeft.Font = new Font("맑은 고딕", 15F);
            this._btnLeft.FlatStyle = FlatStyle.Flat;

            // _btnRight
            this._btnRight.Location = new Point(170, 91);
            this._btnRight.Size = new Size(54, 54);
            this._btnRight.Text = "▶";
            this._btnRight.Font = new Font("맑은 고딕", 15F);
            this._btnRight.FlatStyle = FlatStyle.Flat;

            // _btnCw
            this._btnCw.Location = new Point(30, 214);
            this._btnCw.Size = new Size(54, 34);
            this._btnCw.Text = "↻";
            this._btnCw.Font = new Font("맑은 고딕", 13F);
            this._btnCw.FlatStyle = FlatStyle.Flat;

            // _btnCcw
            this._btnCcw.Location = new Point(170, 214);
            this._btnCcw.Size = new Size(54, 34);
            this._btnCcw.Text = "↺";
            this._btnCcw.Font = new Font("맑은 고딕", 13F);
            this._btnCcw.FlatStyle = FlatStyle.Flat;

            // _cmbSpeed
            this._cmbSpeed.Location = new Point(100, 219);
            this._cmbSpeed.Size = new Size(54, 24);
            this._cmbSpeed.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbSpeed.Font = UiTheme.ValueFont;

            // JogBox
            this.BackColor = UiTheme.OptionPanelBg;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Size = new Size(260, 280);
            this.Controls.Add(this._hdr);
            this.Controls.Add(this._btnUp);
            this.Controls.Add(this._btnDown);
            this.Controls.Add(this._btnLeft);
            this.Controls.Add(this._btnRight);
            this.Controls.Add(this._btnCw);
            this.Controls.Add(this._btnCcw);
            this.Controls.Add(this._cmbSpeed);
            this.Name = "JogBox";
            this.ResumeLayout(false);
        }
    }
}
