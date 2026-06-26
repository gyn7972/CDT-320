namespace QMC.Vision.Ui.Controls
{
    /// <summary>
    /// 검사 결과 뷰어 표시 모드. 공통 뷰어 컨트롤(InspectionViewerControl)이
    /// 이 값에 따라 라벨/그래프/Map/결과 그리드 컬럼을 스왑한다.
    /// </summary>
    public enum InspectionMode
    {
        /// <summary>Bottom 검사 — 사이즈(너비·높이)·칩핑·이물.</summary>
        Bottom,

        /// <summary>Side 검사 — 측면 칩핑(Front/Back max chipping depth).</summary>
        Side,

        /// <summary>Bin(Output) 검사 — DieGap(Top/Right/Bottom gap) + 위치.</summary>
        Bin
    }
}
