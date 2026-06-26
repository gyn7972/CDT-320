using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class RoiNudgePad
    {
        private System.ComponentModel.IContainer components = null;

        private FlowLayoutPanel  _flow;
        private TableLayoutPanel _pad;   // 3×3 이동
        private TableLayoutPanel _rz;    // 2×3 크기 + Full
        private Button _btnUp;
        private Button _btnDown;
        private Button _btnLeft;
        private Button _btnRight;
        private Button _btnCenter;
        private Button _btnWPlus;
        private Button _btnWMinus;
        private Button _btnHPlus;
        private Button _btnHMinus;
        private Button _btnFull;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._flow = new FlowLayoutPanel();
            this._pad = new TableLayoutPanel();
            this._rz = new TableLayoutPanel();
            this._btnUp = new Button();
            this._btnDown = new Button();
            this._btnLeft = new Button();
            this._btnRight = new Button();
            this._btnCenter = new Button();
            this._btnWPlus = new Button();
            this._btnWMinus = new Button();
            this._btnHPlus = new Button();
            this._btnHMinus = new Button();
            this._btnFull = new Button();
            this.SuspendLayout();

            // _flow
            this._flow.Dock = DockStyle.Fill;
            this._flow.FlowDirection = FlowDirection.LeftToRight;
            this._flow.WrapContents = false;
            this._flow.Padding = new Padding(4);

            // _pad (3×3 이동)
            this._pad.ColumnCount = 3;
            this._pad.RowCount = 3;
            this._pad.Size = new Size(120, 96);
            this._pad.Margin = new Padding(2);
            this._pad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this._pad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this._pad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            this._pad.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            this._pad.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            this._pad.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

            // _rz (2×3 크기 + Full)
            this._rz.ColumnCount = 2;
            this._rz.RowCount = 3;
            this._rz.Size = new Size(140, 96);
            this._rz.Margin = new Padding(2);
            this._rz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._rz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this._rz.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            this._rz.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            this._rz.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

            // _btnUp
            this._btnUp.Text = "▲";
            this._btnUp.Dock = DockStyle.Fill;
            this._btnUp.Margin = new Padding(2);
            this._btnUp.FlatStyle = FlatStyle.Flat;
            this._btnUp.Font = UiTheme.ButtonFont;
            this._btnUp.Click += new System.EventHandler(this._btnUp_Click);

            // _btnLeft
            this._btnLeft.Text = "◀";
            this._btnLeft.Dock = DockStyle.Fill;
            this._btnLeft.Margin = new Padding(2);
            this._btnLeft.FlatStyle = FlatStyle.Flat;
            this._btnLeft.Font = UiTheme.ButtonFont;
            this._btnLeft.Click += new System.EventHandler(this._btnLeft_Click);

            // _btnCenter
            this._btnCenter.Text = "●";
            this._btnCenter.Dock = DockStyle.Fill;
            this._btnCenter.Margin = new Padding(2);
            this._btnCenter.FlatStyle = FlatStyle.Flat;
            this._btnCenter.Font = UiTheme.ButtonFont;
            this._btnCenter.Click += new System.EventHandler(this._btnCenter_Click);

            // _btnRight
            this._btnRight.Text = "▶";
            this._btnRight.Dock = DockStyle.Fill;
            this._btnRight.Margin = new Padding(2);
            this._btnRight.FlatStyle = FlatStyle.Flat;
            this._btnRight.Font = UiTheme.ButtonFont;
            this._btnRight.Click += new System.EventHandler(this._btnRight_Click);

            // _btnDown
            this._btnDown.Text = "▼";
            this._btnDown.Dock = DockStyle.Fill;
            this._btnDown.Margin = new Padding(2);
            this._btnDown.FlatStyle = FlatStyle.Flat;
            this._btnDown.Font = UiTheme.ButtonFont;
            this._btnDown.Click += new System.EventHandler(this._btnDown_Click);

            this._pad.Controls.Add(this._btnUp, 1, 0);
            this._pad.Controls.Add(this._btnLeft, 0, 1);
            this._pad.Controls.Add(this._btnCenter, 1, 1);
            this._pad.Controls.Add(this._btnRight, 2, 1);
            this._pad.Controls.Add(this._btnDown, 1, 2);

            // _btnWPlus
            this._btnWPlus.Text = "W +";
            this._btnWPlus.Dock = DockStyle.Fill;
            this._btnWPlus.Margin = new Padding(2);
            this._btnWPlus.FlatStyle = FlatStyle.Flat;
            this._btnWPlus.Font = UiTheme.ButtonFont;
            this._btnWPlus.Click += new System.EventHandler(this._btnWPlus_Click);

            // _btnWMinus
            this._btnWMinus.Text = "W -";
            this._btnWMinus.Dock = DockStyle.Fill;
            this._btnWMinus.Margin = new Padding(2);
            this._btnWMinus.FlatStyle = FlatStyle.Flat;
            this._btnWMinus.Font = UiTheme.ButtonFont;
            this._btnWMinus.Click += new System.EventHandler(this._btnWMinus_Click);

            // _btnHPlus
            this._btnHPlus.Text = "H +";
            this._btnHPlus.Dock = DockStyle.Fill;
            this._btnHPlus.Margin = new Padding(2);
            this._btnHPlus.FlatStyle = FlatStyle.Flat;
            this._btnHPlus.Font = UiTheme.ButtonFont;
            this._btnHPlus.Click += new System.EventHandler(this._btnHPlus_Click);

            // _btnHMinus
            this._btnHMinus.Text = "H -";
            this._btnHMinus.Dock = DockStyle.Fill;
            this._btnHMinus.Margin = new Padding(2);
            this._btnHMinus.FlatStyle = FlatStyle.Flat;
            this._btnHMinus.Font = UiTheme.ButtonFont;
            this._btnHMinus.Click += new System.EventHandler(this._btnHMinus_Click);

            // _btnFull
            this._btnFull.Text = "Full Size";
            this._btnFull.Dock = DockStyle.Fill;
            this._btnFull.Margin = new Padding(2);
            this._btnFull.FlatStyle = FlatStyle.Flat;
            this._btnFull.Font = UiTheme.ButtonFont;
            this._btnFull.Click += new System.EventHandler(this._btnFull_Click);

            this._rz.Controls.Add(this._btnWPlus, 0, 0);
            this._rz.Controls.Add(this._btnWMinus, 1, 0);
            this._rz.Controls.Add(this._btnHPlus, 0, 1);
            this._rz.Controls.Add(this._btnHMinus, 1, 1);
            this._rz.Controls.Add(this._btnFull, 0, 2);
            this._rz.SetColumnSpan(this._btnFull, 2);

            // _flow 조립
            this._flow.Controls.Add(this._pad);
            this._flow.Controls.Add(this._rz);

            // RoiNudgePad
            this.Controls.Add(this._flow);
            this.Name = "RoiNudgePad";
            this.Size = new Size(280, 104);
            this.ResumeLayout(false);
        }
    }
}
