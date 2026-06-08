using System;
using System.Drawing;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    partial class CameraView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // leaf 커스텀 페인트 컨트롤 — 자식 컨트롤 없음. 직렬화 가능 속성 + 이벤트 named 핸들러 연결만.
        // (SetStyle / OnPaint / 비즈니스 로직은 CameraView.cs)
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.Black;
            this.MouseDown += new MouseEventHandler(this.OnMouseDownEditing);
            this.MouseMove += new MouseEventHandler(this.OnMouseMoveEditing);
            this.MouseUp += new MouseEventHandler(this.OnMouseUpEditing);
            this.DoubleClick += new EventHandler(this.OnViewDoubleClick);
            this.Name = "CameraView";
            this.ResumeLayout(false);
        }
    }
}
