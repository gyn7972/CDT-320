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

            this.btnR0.Text = "ROI1";
            this.btnR0.Location = new Point(10, 32);
            this.btnR0.Size = new Size(72, 30);
            this.btnR0.FlatStyle = FlatStyle.Flat;
            this.btnR0.ForeColor = Color.Red;
            this.btnR0.BackColor = Color.White;
            this.btnR0.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.btnR1.Text = "ROI2";
            this.btnR1.Location = new Point(88, 32);
            this.btnR1.Size = new Size(72, 30);
            this.btnR1.FlatStyle = FlatStyle.Flat;
            this.btnR1.ForeColor = Color.Goldenrod;
            this.btnR1.BackColor = Color.White;
            this.btnR1.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.btnR2.Text = "ROI3";
            this.btnR2.Location = new Point(166, 32);
            this.btnR2.Size = new Size(72, 30);
            this.btnR2.FlatStyle = FlatStyle.Flat;
            this.btnR2.ForeColor = Color.RoyalBlue;
            this.btnR2.BackColor = Color.White;
            this.btnR2.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            this.btnR3.Text = "ROI4";
            this.btnR3.Location = new Point(244, 32);
            this.btnR3.Size = new Size(72, 30);
            this.btnR3.FlatStyle = FlatStyle.Flat;
            this.btnR3.ForeColor = Color.ForestGreen;
            this.btnR3.BackColor = Color.White;
            this.btnR3.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);

            this.lblInfo.Location = new Point(10, 70);
            this.lblInfo.Size = new Size(300, 20);
            this.lblInfo.Font = new Font("Consolas", 9.5F);

            this.lblStep.Location = new Point(10, 98); this.lblStep.Size = new Size(40, 24); this.lblStep.Text = "Step";
            this.lblStep.TextAlign = ContentAlignment.MiddleLeft;
            this.btnS1.Text = "1";
            this.btnS1.Location = new Point(52, 96);
            this.btnS1.Size = new Size(40, 24);
            this.btnS1.FlatStyle = FlatStyle.Flat;
            this.btnS1.BackColor = Color.White;
            this.btnS5.Text = "5";
            this.btnS5.Location = new Point(96, 96);
            this.btnS5.Size = new Size(40, 24);
            this.btnS5.FlatStyle = FlatStyle.Flat;
            this.btnS5.BackColor = Color.White;
            this.btnS10.Text = "10";
            this.btnS10.Location = new Point(140, 96);
            this.btnS10.Size = new Size(40, 24);
            this.btnS10.FlatStyle = FlatStyle.Flat;
            this.btnS10.BackColor = Color.White;

            this.lblPos.Location = new Point(10, 128); this.lblPos.Size = new Size(120, 18); this.lblPos.Text = "위치 (Center)";
            this.lblSize.Location = new Point(176, 128); this.lblSize.Size = new Size(120, 18); this.lblSize.Text = "크기 (W/H)";

            this.btnXm.Text = "◀ X-";
            this.btnXm.Location = new Point(10, 150);
            this.btnXm.Size = new Size(64, 32);
            this.btnXm.FlatStyle = FlatStyle.Flat;
            this.btnXm.BackColor = Color.White;
            this.btnXm.Font = new Font("맑은 고딕", 9F);
            this.btnXp.Text = "X+ ▶";
            this.btnXp.Location = new Point(78, 150);
            this.btnXp.Size = new Size(64, 32);
            this.btnXp.FlatStyle = FlatStyle.Flat;
            this.btnXp.BackColor = Color.White;
            this.btnXp.Font = new Font("맑은 고딕", 9F);
            this.btnYm.Text = "▲ Y-";
            this.btnYm.Location = new Point(10, 188);
            this.btnYm.Size = new Size(64, 32);
            this.btnYm.FlatStyle = FlatStyle.Flat;
            this.btnYm.BackColor = Color.White;
            this.btnYm.Font = new Font("맑은 고딕", 9F);
            this.btnYp.Text = "Y+ ▼";
            this.btnYp.Location = new Point(78, 188);
            this.btnYp.Size = new Size(64, 32);
            this.btnYp.FlatStyle = FlatStyle.Flat;
            this.btnYp.BackColor = Color.White;
            this.btnYp.Font = new Font("맑은 고딕", 9F);
            this.btnWm.Text = "W -";
            this.btnWm.Location = new Point(176, 150);
            this.btnWm.Size = new Size(64, 32);
            this.btnWm.FlatStyle = FlatStyle.Flat;
            this.btnWm.BackColor = Color.White;
            this.btnWm.Font = new Font("맑은 고딕", 9F);
            this.btnWp.Text = "W +";
            this.btnWp.Location = new Point(244, 150);
            this.btnWp.Size = new Size(64, 32);
            this.btnWp.FlatStyle = FlatStyle.Flat;
            this.btnWp.BackColor = Color.White;
            this.btnWp.Font = new Font("맑은 고딕", 9F);
            this.btnHm.Text = "H -";
            this.btnHm.Location = new Point(176, 188);
            this.btnHm.Size = new Size(64, 32);
            this.btnHm.FlatStyle = FlatStyle.Flat;
            this.btnHm.BackColor = Color.White;
            this.btnHm.Font = new Font("맑은 고딕", 9F);
            this.btnHp.Text = "H +";
            this.btnHp.Location = new Point(244, 188);
            this.btnHp.Size = new Size(64, 32);
            this.btnHp.FlatStyle = FlatStyle.Flat;
            this.btnHp.BackColor = Color.White;
            this.btnHp.Font = new Font("맑은 고딕", 9F);

            // 직접 입력 행 (X / Y / W / H)
            this.lblX.Text = "X";
            this.lblX.Location = new Point(12, 228);
            this.lblX.Size = new Size(16, 20);
            this.lblX.TextAlign = ContentAlignment.MiddleLeft;
            this.txtX.Location = new Point(28, 226);
            this.txtX.Size = new Size(52, 22);
            this.txtX.Font = new Font("Consolas", 9.5F);
            this.lblY.Text = "Y";
            this.lblY.Location = new Point(86, 228);
            this.lblY.Size = new Size(16, 20);
            this.lblY.TextAlign = ContentAlignment.MiddleLeft;
            this.txtY.Location = new Point(102, 226);
            this.txtY.Size = new Size(52, 22);
            this.txtY.Font = new Font("Consolas", 9.5F);
            this.lblW.Text = "W";
            this.lblW.Location = new Point(158, 228);
            this.lblW.Size = new Size(16, 20);
            this.lblW.TextAlign = ContentAlignment.MiddleLeft;
            this.txtW.Location = new Point(178, 226);
            this.txtW.Size = new Size(52, 22);
            this.txtW.Font = new Font("Consolas", 9.5F);
            this.lblH.Text = "H";
            this.lblH.Location = new Point(236, 228);
            this.lblH.Size = new Size(16, 20);
            this.lblH.TextAlign = ContentAlignment.MiddleLeft;
            this.txtH.Location = new Point(254, 226);
            this.txtH.Size = new Size(52, 22);
            this.txtH.Font = new Font("Consolas", 9.5F);

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

    }
}
