using QMC.CDT320.Ui.Controls;

namespace QMC.CDT_320.Ui.Dialogs
{
    /// <summary>공용 맵 조작을 재사용하되 파일 뷰어의 정보는 다이얼로그에서 표시한다.</summary>
    internal sealed class WaferMapViewerView : DieMapView
    {
        protected override bool ShowTechnicalInfoLine => false;
        protected override string OverlayFontFamily => "맑은 고딕";
    }
}
