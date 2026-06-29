using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Dialogs
{
    partial class ProcessedImageDialog
    {
        private IContainer components = null;

        private Button btnAll, btnP0, btnP1, btnP2, btnP3, btnRefresh;
        private Label lblTh;
        private NumericUpDown numTh;
        private Button btnApplyTh;
        private PictureBox picProc;
        private Label lblInfo;
        private Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { if (picProc != null && picProc.Image != null) picProc.Image.Dispose(); } catch { }
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            this.btnAll = new Button(); this.btnP0 = new Button(); this.btnP1 = new Button();
            this.btnP2 = new Button(); this.btnP3 = new Button(); this.btnRefresh = new Button();
            this.lblTh = new Label();
            this.numTh = new NumericUpDown();
            this.btnApplyTh = new Button();
            this.picProc = new PictureBox();
            this.lblInfo = new Label();
            this.btnClose = new Button();
            ((ISupportInitialize)(this.picProc)).BeginInit();
            ((ISupportInitialize)(this.numTh)).BeginInit();
            this.SuspendLayout();

            Tab(this.btnAll, "전체", 8, Color.Black);
            Tab(this.btnP0, "ROI1", 82, Color.Red);
            Tab(this.btnP1, "ROI2", 156, Color.Goldenrod);
            Tab(this.btnP2, "ROI3", 230, Color.RoyalBlue);
            Tab(this.btnP3, "ROI4", 304, Color.ForestGreen);

            this.btnRefresh.Location = new Point(382, 8);
            this.btnRefresh.Size = new Size(140, 28);
            this.btnRefresh.Text = "현재 프레임 처리";
            this.btnRefresh.FlatStyle = FlatStyle.Flat;
            this.btnRefresh.BackColor = Color.FromArgb(0xDD, 0xEE, 0xFF);

            // 임계값 입력 행
            this.lblTh.Location = new Point(8, 44);
            this.lblTh.Size = new Size(50, 24);
            this.lblTh.Text = "임계값";
            this.lblTh.TextAlign = ContentAlignment.MiddleLeft;
            this.numTh.Location = new Point(60, 42);
            this.numTh.Size = new Size(64, 24);
            this.numTh.Minimum = 0;
            this.numTh.Maximum = 255;
            this.btnApplyTh.Location = new Point(132, 40);
            this.btnApplyTh.Size = new Size(120, 26);
            this.btnApplyTh.Text = "적용 + 처리";
            this.btnApplyTh.FlatStyle = FlatStyle.Flat;
            this.btnApplyTh.BackColor = Color.FromArgb(0xEA, 0xDD, 0xFF);

            this.picProc.Location = new Point(8, 72);
            this.picProc.Size = new Size(514, 326);
            this.picProc.BackColor = Color.Black;
            this.picProc.SizeMode = PictureBoxSizeMode.Zoom;
            this.picProc.BorderStyle = BorderStyle.FixedSingle;

            this.lblInfo.Location = new Point(8, 406);
            this.lblInfo.Size = new Size(400, 22);
            this.lblInfo.Font = new Font("Consolas", 9.5F);
            this.lblInfo.TextAlign = ContentAlignment.MiddleLeft;

            this.btnClose.Location = new Point(422, 404);
            this.btnClose.Size = new Size(100, 26);
            this.btnClose.Text = "닫기";
            this.btnClose.FlatStyle = FlatStyle.Flat;

            this.Controls.Add(this.btnAll); this.Controls.Add(this.btnP0); this.Controls.Add(this.btnP1);
            this.Controls.Add(this.btnP2); this.Controls.Add(this.btnP3); this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.lblTh); this.Controls.Add(this.numTh); this.Controls.Add(this.btnApplyTh);
            this.Controls.Add(this.picProc);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnClose);

            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(530, 438);
            this.Text = "처리 이미지 (엣지 응답)";
            ((ISupportInitialize)(this.picProc)).EndInit();
            ((ISupportInitialize)(this.numTh)).EndInit();
            this.ResumeLayout(false);
        }

        private static void Tab(Button b, string text, int x, Color fg)
        {
            b.Text = text; b.Location = new Point(x, 8); b.Size = new Size(72, 28);
            b.FlatStyle = FlatStyle.Flat; b.ForeColor = fg; b.BackColor = Color.White;
            b.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        }
    }
}
