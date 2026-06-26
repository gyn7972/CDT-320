using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Windows
{
    partial class BottomInspectionWindow
    {
        private System.ComponentModel.IContainer components = null;

        // 단일 모니터에서 화면을 모두 가릴 때 임시로 숨길 수 있는 최소화 버튼.
        private System.Windows.Forms.Button btnMinimize;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // btnMinimize — 우상단 고정 최소화 버튼(테두리 없는 풀스크린이라 시스템 최소화 버튼이 없음).
            btnMinimize = new Button();
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Text = "—";
            btnMinimize.Size = new Size(48, 32);
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.BackColor = Color.FromArgb(0x3A, 0x40, 0x4C);
            btnMinimize.ForeColor = Color.White;
            btnMinimize.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnMinimize.TabStop = false;
            btnMinimize.Cursor = Cursors.Hand;
            btnMinimize.Click += btnMinimize_Click;

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;   // 테두리 없음 → 이동 핸들 없음
            StartPosition = FormStartPosition.Manual; // 보조 모니터 좌표로 직접 배치
            ShowInTaskbar = false;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;
            BackColor = Color.FromArgb(0x2C, 0x30, 0x3A);
            Name = "BottomInspectionWindow";
            Text = "Bottom Inspection";

            Controls.Add(btnMinimize);

            ResumeLayout(false);
        }
    }
}
