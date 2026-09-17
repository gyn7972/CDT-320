using System.Drawing;
using QMC.CDT320.DieMaps;
using QMC.CDT_320.Ui.Common.WaferMaps;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// 메인 화면과 Recipe 맵 편집 화면의 공통 표시입니다.
    /// 실시간 조회, 타이머, 맵 편집 및 저장은 호출 화면에서 담당합니다.
    /// </summary>
    public class WaferMapView : QMC.CDT320.Ui.Controls.DieMapView
    {
        protected override Color MapBorderColor => WaferMapPalette.WaferOutline;
        protected override float MapBorderWidth => 2f;
        protected override int MapBorderInset => 3;
        protected override string OverlayFontFamily => "맑은 고딕";
        protected override bool ShowTechnicalInfoLine => false;

        public WaferMapView()
        {
            BackColor = WaferMapPalette.ViewBackground;
            CompactUsedBounds = true;
            ShowWaferOutline = true;
            ShowEquipmentAxes = false;
            EntryVisibilityPredicate = entry => entry != null;
            // 등록 맵의 BIN은 검사 결과가 아니다. 대상/제외만 표시하고 원본 값은 보존한다.
            CellColorResolver = entry => WaferMapDisplayStyle.GetColor(ResolvePlanState(entry));
            CellStatusResolver = entry => WaferMapDisplayStyle.GetText(ResolvePlanState(entry));
            CellTextResolver = entry => "";
            LegendItemsResolver = WaferMapDisplayStyle.BuildLegend;
        }

        private static WaferMapCellState ResolvePlanState(DieMapEntry entry)
        {
            return entry == null ? WaferMapCellState.Unknown :
                entry.IsTarget ? WaferMapCellState.Wait : WaferMapCellState.Skip;
        }
    }
}
