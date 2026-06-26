using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Windows
{
    partial class BottomInspectionWindow
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

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

            ResumeLayout(false);
        }
    }
}
