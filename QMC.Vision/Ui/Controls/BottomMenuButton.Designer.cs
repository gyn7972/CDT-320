using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class BottomMenuButton
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // leaf 커스텀 페인트 컨트롤 — 자식 컨트롤 없음. 직렬화 가능 기본 속성만 설정(SetStyle/OnPaint 는 Code.cs).
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(110, 70);
            this.Cursor = Cursors.Hand;
            this.Font = UiTheme.BottomBtnFont;
            this.ForeColor = UiTheme.BottomBarFg;
            this.BackColor = UiTheme.BottomBarBg;
            this.Name = "BottomMenuButton";
            this.ResumeLayout(false);
        }
    }
}
