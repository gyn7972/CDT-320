using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Dialogs
{
    partial class ZoomDialog
    {
        private System.ComponentModel.IContainer components = null;

        private Label  _statusBar;
        private Panel  _bar;
        private Button _btnReset;
        private Button _btnSave;
        private Button _btnClose;
        private Panel  _canvas;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._statusBar = new Label();
            this._bar = new Panel();
            this._btnReset = new Button();
            this._btnSave = new Button();
            this._btnClose = new Button();
            this._canvas = new Panel();
            this._bar.SuspendLayout();
            this.SuspendLayout();

            // _statusBar (Text 는 런타임에 이미지 크기로 설정)
            this._statusBar.Dock = DockStyle.Top;
            this._statusBar.Height = 28;
            this._statusBar.BackColor = Color.FromArgb(0xD9, 0x77, 0x06);
            this._statusBar.ForeColor = Color.White;
            this._statusBar.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this._statusBar.TextAlign = ContentAlignment.MiddleLeft;
            this._statusBar.Padding = new Padding(10, 0, 0, 0);

            // _bar
            this._bar.Dock = DockStyle.Bottom;
            this._bar.Height = 44;
            this._bar.BackColor = Color.FromArgb(45, 45, 48);

            // _btnReset
            this._btnReset.Location = new Point(10, 6);
            this._btnReset.Size = new Size(100, 32);
            this._btnReset.Text = "1:1";
            this._btnReset.FlatStyle = FlatStyle.Flat;
            this._btnReset.BackColor = Color.White;
            this._btnReset.Font = new Font("맑은 고딕", 10F);
            this._btnReset.Click += new System.EventHandler(this.OnResetClick);

            // _btnSave
            this._btnSave.Location = new Point(116, 6);
            this._btnSave.Size = new Size(100, 32);
            this._btnSave.Text = "SAVE";
            this._btnSave.FlatStyle = FlatStyle.Flat;
            this._btnSave.BackColor = Color.White;
            this._btnSave.Font = new Font("맑은 고딕", 10F);
            this._btnSave.Click += new System.EventHandler(this.OnSaveClick);

            // _btnClose
            this._btnClose.Location = new Point(222, 6);
            this._btnClose.Size = new Size(100, 32);
            this._btnClose.Text = "CLOSE";
            this._btnClose.FlatStyle = FlatStyle.Flat;
            this._btnClose.BackColor = Color.White;
            this._btnClose.Font = new Font("맑은 고딕", 10F);
            this._btnClose.DialogResult = DialogResult.OK;

            this._bar.Controls.Add(this._btnReset);
            this._bar.Controls.Add(this._btnSave);
            this._bar.Controls.Add(this._btnClose);

            // _canvas (Paint/Wheel/Mouse 이벤트 named 연결; SetStyle 더블버퍼는 Code.cs 생성자에서)
            this._canvas.Dock = DockStyle.Fill;
            this._canvas.BackColor = Color.Black;
            this._canvas.Paint += new PaintEventHandler(this.OnPaintCanvas);
            this._canvas.MouseWheel += new MouseEventHandler(this.OnWheel);
            this._canvas.MouseDown += new MouseEventHandler(this.OnDown);
            this._canvas.MouseMove += new MouseEventHandler(this.OnMove);
            this._canvas.MouseUp += new MouseEventHandler(this.OnUp);
            this._canvas.DoubleClick += new System.EventHandler(this.OnCanvasDoubleClick);
            this._canvas.MouseEnter += new System.EventHandler(this.OnCanvasMouseEnter);

            // ZoomDialog (폼 속성; Text 는 런타임 title 로 설정)
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(640, 480);
            this.ClientSize = new Size(1024, 768);
            this.BackColor = Color.FromArgb(40, 40, 40);
            this.ShowIcon = false;
            this.Text = "Zoom";
            this.Name = "ZoomDialog";
            this.Controls.Add(this._statusBar);
            this.Controls.Add(this._bar);
            this.Controls.Add(this._canvas);
            this.Controls.SetChildIndex(this._canvas, 0);
            this._bar.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
