using System;
using System.Windows.Forms;

namespace QMC.Vision.Ui.Controls
{
    /// <summary>레시피 타깃 페이지 공용 ROI 제어 패드 — 이동(3×3) + 크기(W/H ±) + Recenter + Full Size.
    /// VisionTargetPage / InspectorTargetPage 공유. 레이아웃은 RoiNudgePad.Designer.cs.
    /// 버튼은 페이지 로직을 모르고 "의미"만 이벤트로 알린다 → 핸들러에서 구독자로 1스텝 디버깅.</summary>
    public partial class RoiNudgePad : Panel
    {
        /// <summary>이동 요청 — (dx, dy) 스텝 수(오른쪽/아래 = +).</summary>
        public event Action<int, int> MoveRequested;

        /// <summary>크기 변경 요청 — (dw, dh) 스텝 수(확대 = +).</summary>
        public event Action<int, int> ResizeRequested;

        /// <summary>중앙 정렬 요청.</summary>
        public event Action RecenterRequested;

        /// <summary>전체 크기 요청.</summary>
        public event Action FullSizeRequested;

        public RoiNudgePad()
        {
            InitializeComponent();
        }

        private void _btnUp_Click(object sender, EventArgs e)     { MoveRequested?.Invoke(0, -1); }
        private void _btnDown_Click(object sender, EventArgs e)   { MoveRequested?.Invoke(0, 1); }
        private void _btnLeft_Click(object sender, EventArgs e)   { MoveRequested?.Invoke(-1, 0); }
        private void _btnRight_Click(object sender, EventArgs e)  { MoveRequested?.Invoke(1, 0); }
        private void _btnCenter_Click(object sender, EventArgs e) { RecenterRequested?.Invoke(); }
        private void _btnWPlus_Click(object sender, EventArgs e)  { ResizeRequested?.Invoke(1, 0); }
        private void _btnWMinus_Click(object sender, EventArgs e) { ResizeRequested?.Invoke(-1, 0); }
        private void _btnHPlus_Click(object sender, EventArgs e)  { ResizeRequested?.Invoke(0, 1); }
        private void _btnHMinus_Click(object sender, EventArgs e) { ResizeRequested?.Invoke(0, -1); }
        private void _btnFull_Click(object sender, EventArgs e)   { FullSizeRequested?.Invoke(); }
    }
}
