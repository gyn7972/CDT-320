using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Dialogs
{
    partial class RoiJogDialog
    {
        private IContainer components = null;

        private Label lblTitle;
        private Button btnR0, btnR1, btnR2, btnR3;
        private Label lblInfo;
        private Label lblStep;
        private Button btnS1, btnS5, btnS10;
        private Label lblPos, lblSize;
        private Button btnXm, btnXp, btnYm, btnYp;
        private Button btnWm, btnWp, btnHm, btnHp;
        private Label lblX, lblY, lblW, lblH;
        private TextBox txtX, txtY, txtW, txtH;
        private Button btnApply;
        private Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            this.lblTitle = new Label();
            this.btnR0 = new Button(); this.btnR1 = new Button(); this.btnR2 = new Button(); this.btnR3 = new Button();
            this.lblInfo = new Label();
            this.lblStep = new Label();
            this.btnS1 = new Button(); this.btnS5 = new Button(); this.btnS10 = new Button();
            this.lblPos = new Label(); this.lblSize = new Label();
            this.btnXm = new Button(); this.btnXp = new Button(); this.btnYm = new Button(); this.btnYp = new Button();
            this.btnWm = new Button(); this.btnWp = new Button(); this.btnHm = new Button(); this.btnHp = new Button();
            this.lblX = new Label(); this.lblY = new Label(); this.lblW = new Label(); this.lblH = new Label();
            this.txtX = new TextBox(); this.txtY = new TextBox(); this.txtW = new TextBox(); this.txtH = new TextBox();
            this.btnApply = new Button();
            this.btnClose = new Button();
            this.SuspendLayout();

            this.lblTitle.Location = new Point(10, 8);
            this.lblTitle.Size = new Size(300, 20);
            this.lblTitle.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);

            Sel(this.btnR0, "ROI1", 10, Color.Red);
            Sel(this.btnR1, "ROI2", 88, Color.Goldenrod);
            Sel(this.btnR2, "ROI3", 166, Color.RoyalBlue);
            Sel(this.btnR3, "ROI4", 244, Color.ForestGreen);

            this.lblInfo.Location = new Point(10, 70);
            this.lblInfo.Size = new Size(300, 20);
            this.lblInfo.Font = new Font("Consolas", 9.5F);

            this.lblStep.Location = new Point(10, 98); this.lblStep.Size = new Size(40, 24); this.lblStep.Text = "Step";
            this.lblStep.TextAlign = ContentAlignment.MiddleLeft;
            Mini(this.btnS1, "1", 52, 96); Mini(this.btnS5, "5", 96, 96); Mini(this.btnS10, "10", 140, 96);

            this.lblPos.Location = new Point(10, 128); this.lblPos.Size = new Size(120, 18); this.lblPos.Text = "위치 (Center)";
            this.lblSize.Location = new Point(176, 128); this.lblSize.Size = new Size(120, 18); this.lblSize.Text = "크기 (W/H)";

            Jog(this.btnXm, "◀ X-", 10, 150); Jog(this.btnXp, "X+ ▶", 78, 150);
            Jog(this.btnYm, "▲ Y-", 10, 188); Jog(this.btnYp, "Y+ ▼", 78, 188);
            Jog(this.btnWm, "W -", 176, 150); Jog(this.btnWp, "W +", 244, 150);
            Jog(this.btnHm, "H -", 176, 188); Jog(this.btnHp, "H +", 244, 188);

            // 직접 입력 행 (X / Y / W / H)
            InLbl(this.lblX, "X", 12); In(this.txtX, 28);
            InLbl(this.lblY, "Y", 86); In(this.txtY, 102);
            InLbl(this.lblW, "W", 158); In(this.txtW, 178);
            InLbl(this.lblH, "H", 236); In(this.txtH, 254);

            this.btnApply.Location = new Point(10, 258);
            this.btnApply.Size = new Size(148, 30);
            this.btnApply.Text = "입력값 적용";
            this.btnApply.FlatStyle = FlatStyle.Flat;
            this.btnApply.BackColor = Color.FromArgb(0xDD, 0xEE, 0xFF);

            this.btnClose.Location = new Point(164, 258);
            this.btnClose.Size = new Size(148, 30);
            this.btnClose.Text = "닫기";
            this.btnClose.FlatStyle = FlatStyle.Flat;

            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.btnR0); this.Controls.Add(this.btnR1); this.Controls.Add(this.btnR2); this.Controls.Add(this.btnR3);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblStep); this.Controls.Add(this.btnS1); this.Controls.Add(this.btnS5); this.Controls.Add(this.btnS10);
            this.Controls.Add(this.lblPos); this.Controls.Add(this.lblSize);
            this.Controls.Add(this.btnXm); this.Controls.Add(this.btnXp); this.Controls.Add(this.btnYm); this.Controls.Add(this.btnYp);
            this.Controls.Add(this.btnWm); this.Controls.Add(this.btnWp); this.Controls.Add(this.btnHm); this.Controls.Add(this.btnHp);
            this.Controls.Add(this.lblX); this.Controls.Add(this.txtX);
            this.Controls.Add(this.lblY); this.Controls.Add(this.txtY);
            this.Controls.Add(this.lblW); this.Controls.Add(this.txtW);
            this.Controls.Add(this.lblH); this.Controls.Add(this.txtH);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnClose);

            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(322, 300);
            this.Text = "ROI 조그";
            this.ResumeLayout(false);
        }

        private static void Sel(Button b, string text, int x, Color fg)
        {
            b.Text = text; b.Location = new Point(x, 32); b.Size = new Size(72, 30);
            b.FlatStyle = FlatStyle.Flat; b.ForeColor = fg; b.BackColor = Color.White;
            b.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        }
        private static void Mini(Button b, string text, int x, int y)
        {
            b.Text = text; b.Location = new Point(x, y); b.Size = new Size(40, 24);
            b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.White;
        }
        private static void Jog(Button b, string text, int x, int y)
        {
            b.Text = text; b.Location = new Point(x, y); b.Size = new Size(64, 32);
            b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.White;
            b.Font = new Font("맑은 고딕", 9F);
        }
        private static void InLbl(Label l, string text, int x)
        {
            l.Text = text; l.Location = new Point(x, 228); l.Size = new Size(16, 20);
            l.TextAlign = ContentAlignment.MiddleLeft;
        }
        private static void In(TextBox t, int x)
        {
            t.Location = new Point(x, 226); t.Size = new Size(52, 22);
            t.Font = new Font("Consolas", 9.5F);
        }
    }
}
